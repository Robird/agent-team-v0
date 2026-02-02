# Task: Phase 3 - RbfFrameBuilder 的 Per-File 复用

> **预估工时**：4h
> **执行者**：Implementer
> **依赖**：Phase 2（Writer 和 Sink 的 Reset 支持）
> **收益**：每帧堆分配从 ~150B 降至 0B（摊销）

---

## 1. 问题背景

当前每次 `BeginAppend()` 都会创建新的 `RbfFrameBuilder`，而 Builder 内部又会创建 `RandomAccessByteSink` 和 `SinkReservableWriter`。

由于 `RbfFileImpl` 已通过 `_hasActiveBuilder` 约束保证**同一时刻只有一个活跃 Builder**，因此可以采用 **per-file 单实例复用**策略，而非全局 ObjectPool。

## 2. 目标

1. `RbfFrameBuilder` 添加 `Reset(long frameStart)` 方法
2. `RbfFileImpl` 持有一个可复用的 Builder 实例
3. `BeginAppend()` 复用现有 Builder 而非每次 new

## 3. 架构设计

### 3.1 生命周期模型

```
RbfFileImpl（文件级）
  └── _builder（延迟创建，复用）
        ├── _sink (RandomAccessByteSink) → Reset(offset)
        └── _writer (SinkReservableWriter) → Reset()
```

**关键点**：
- Builder 首次使用时创建，之后复用
- 每次 BeginAppend 调用 `_builder.Reset(frameStart)`
- Builder.Dispose 不销毁内部对象，只重置状态

### 3.2 状态机

```
Builder 状态：Idle → Active → Committed/Aborted → Idle
                     ↑__________________|
                          Reset()
```

## 4. 修改范围

### 4.1 RbfFrameBuilder

**文件**：`/repos/focus/atelia/src/Rbf/RbfFrameBuilder.cs`

#### 4.1.0 移除 readonly 修饰符（前置条件）

审阅确认，当前有 **6 个 `readonly` 字段**需要改为可变：

```csharp
// 当前（需修改）
private readonly long _frameStart;              // → private long _frameStart;
private readonly int _headLenReservationToken;  // → private int _headLenReservationToken;
private readonly Action<long> _onCommitCallback; // → private Action<long> _onCommitCallback;
private readonly Action _clearBuilderFlag;       // → private Action _clearBuilderFlag;

// 保持 readonly（不变）
private readonly RandomAccessByteSink _sink;     // 同一文件，Reset 只更新 offset
private readonly SinkReservableWriter _writer;   // 复用实例，Reset 清空状态
```

**添加 Reset 方法**：

```csharp
/// <summary>
/// 重置 Builder 状态，准备写入新帧。
/// </summary>
/// <param name="frameStart">新帧的起始偏移量</param>
/// <param name="onCommitCallback">提交回调（可复用已缓存的 delegate）</param>
/// <param name="clearBuilderFlag">清除标记回调（可复用已缓存的 delegate）</param>
internal void Reset(long frameStart, Action<long> onCommitCallback, Action clearBuilderFlag)
{
    // 1. 验证状态
    if (!_disposed && !_committed)
    {
        throw new InvalidOperationException("Cannot reset an active builder. Dispose or commit first.");
    }
    
    // 2. 重置位置信息
    _frameStart = frameStart;
    _onCommitCallback = onCommitCallback;
    _clearBuilderFlag = clearBuilderFlag;
    
    // 3. 重置状态标记
    _committed = false;
    _disposed = false;
    
    // 4. 重置 Sink（使用 Phase 2 添加的方法）
    _sink.Reset(frameStart);
    
    // 5. 重置 Writer
    _writer.Reset();
    
    // 6. 重新预留 HeadLen
    _headLenReservationToken = _writer.ReserveSpan(HeadLenSize, out var _, "HeadLen");
}
```

**修改 Dispose**：

```csharp
public void Dispose()
{
    if (_disposed) return;
    _disposed = true;
    
    if (!_committed)
    {
        // Abort：重置 writer 但不销毁
        _writer.Reset();
    }
    
    // 通知 facade 清除标记
    _clearBuilderFlag?.Invoke();
    
    // 注意：不再 Dispose _writer，因为要复用
}
```

**添加内部状态检查**（供 RbfFileImpl 使用）：

```csharp
internal bool CanBeReused => _disposed || _committed;
```

### 4.2 RbfFileImpl

**文件**：`/repos/focus/atelia/src/Rbf/Internal/RbfFileImpl.cs`

**添加字段**：

```csharp
private RbfFrameBuilder? _reusableBuilder;
```

**修改 BeginAppend**：

```csharp
public RbfFrameBuilder BeginAppend()
{
    if (_hasActiveBuilder)
    {
        throw new InvalidOperationException("Another builder is already active.");
    }
    
    _hasActiveBuilder = true;
    
    if (_reusableBuilder is null)
    {
        // 首次创建
        _reusableBuilder = new RbfFrameBuilder(
            _handle, 
            _tailOffset, 
            _onCommitCallback, 
            _clearBuilderFlag);
    }
    else
    {
        // 复用
        _reusableBuilder.Reset(_tailOffset, _onCommitCallback, _clearBuilderFlag);
    }
    
    return _reusableBuilder;
}
```

**修改 Dispose**：

```csharp
public void Dispose()
{
    // 清理 reusable builder
    if (_reusableBuilder is not null)
    {
        // 如果 builder 有活跃写入，先 abort
        if (!_reusableBuilder.CanBeReused)
        {
            _reusableBuilder.Dispose();
        }
        // 彻底释放内部资源
        _reusableBuilder.DisposeInternal(); // 新增方法，真正释放 writer
        _reusableBuilder = null;
    }
    
    _handle.Dispose();
}
```

### 4.3 新增 DisposeInternal 方法

**在 RbfFrameBuilder 中**：

```csharp
/// <summary>
/// 彻底释放内部资源（仅供 RbfFileImpl.Dispose 调用）
/// </summary>
internal void DisposeInternal()
{
    _writer.Dispose();
    // _sink 无需 dispose（不持有资源）
}
```

## 5. 验收标准

1. **编译通过**
2. **现有测试通过**
3. **新增测试**：
   - 连续多次 BeginAppend → Commit 写入
   - 连续多次 BeginAppend → Abort（Dispose）
   - 交替 Commit 和 Abort
   - 验证第 N 次写入不包含第 N-1 次的残留数据

### 5.1 关键测试用例

```csharp
[Fact]
public void BeginAppend_ReusesBuilder_NoDataLeakage()
{
    using var file = RbfFile.Create(tempPath);
    
    // 第一帧
    using (var builder1 = file.BeginAppend())
    {
        builder1.Payload.Write("Frame1"u8);
        builder1.EndAppend();
    }
    
    // 第二帧（复用 builder）
    using (var builder2 = file.BeginAppend())
    {
        builder2.Payload.Write("Frame2"u8);
        builder2.EndAppend();
    }
    
    // 验证两帧内容独立
    var frames = file.ReadAllFrames();
    Assert.Equal(2, frames.Count);
    Assert.Equal("Frame1"u8.ToArray(), frames[0].Payload);
    Assert.Equal("Frame2"u8.ToArray(), frames[1].Payload);
}

[Fact]
public void BeginAppend_AfterAbort_NoDataLeakage()
{
    using var file = RbfFile.Create(tempPath);
    
    // 第一帧：写入后 abort
    using (var builder1 = file.BeginAppend())
    {
        builder1.Payload.Write("Aborted"u8);
        // 不调用 EndAppend，直接 Dispose → Abort
    }
    
    // 第二帧：正常写入
    using (var builder2 = file.BeginAppend())
    {
        builder2.Payload.Write("Committed"u8);
        builder2.EndAppend();
    }
    
    // 验证只有一帧，且内容正确
    var frames = file.ReadAllFrames();
    Assert.Single(frames);
    Assert.Equal("Committed"u8.ToArray(), frames[0].Payload);
}
```

## 6. 风险评估

| 风险 | 概率 | 影响 | 缓解 |
|:-----|:-----|:-----|:-----|
| Reset 不完整导致数据泄漏 | 中 | 高 | 全面的 Reset 后写入测试 |
| Reservation token 混淆 | 中 | 高 | Reset 时重新 Reserve HeadLen |
| Builder 状态机混乱 | 低 | 中 | 添加状态检查，异常时 fail-fast |
| 内存泄漏（Writer 未释放） | 低 | 中 | RbfFileImpl.Dispose 中调用 DisposeInternal |

## 7. 注意事项

1. **Reset 顺序很重要**：先 Reset Sink（更新 offset），再 Reset Writer（清空 buffer）
2. **HeadLen Reservation**：每次 Reset 后必须重新 `ReserveSpan(HeadLenSize)`
3. **回调复用**：使用 Phase 0 缓存的 delegate，避免重复分配
4. **异常安全**：如果 Reset 过程中抛异常，Builder 应处于可重试或可 Dispose 状态

---

## 执行指令（供 runSubagent 使用）

```
你是 Implementer，请执行以下任务：

## 前置确认

1. 确认 Phase 2 已完成：
   - RandomAccessByteSink 有 `Reset(long writeOffset)` 方法
   - SinkReservableWriter 的 `_sink` 字段可变

## 第一步：修改 RbfFrameBuilder

1. 阅读 `/repos/focus/atelia/src/Rbf/RbfFrameBuilder.cs`
2. 添加 `Reset(long frameStart, Action<long>, Action)` 方法
3. 添加 `CanBeReused` 属性
4. 添加 `DisposeInternal()` 方法
5. 修改 `Dispose()` 方法（不再销毁 writer）

## 第二步：修改 RbfFileImpl

1. 阅读 `/repos/focus/atelia/src/Rbf/RbfFileImpl.cs`
2. 添加 `_reusableBuilder` 字段
3. 修改 `BeginAppend()` 方法（首次创建 vs 复用）
4. 修改 `Dispose()` 方法（清理 reusable builder）

## 第三步：添加测试

在 `/repos/focus/atelia/tests/Rbf.Tests/` 添加：
- Builder 复用后数据隔离测试
- Abort 后复用测试
- 连续多次写入测试

## 第四步：验证

```bash
cd /repos/focus && dotnet build atelia/src/Rbf/
cd /repos/focus && dotnet test atelia/tests/Rbf.Tests/
```

## 输出要求

完成后报告：
1. 修改的所有文件和具体变更
2. 新增的测试用例列表
3. 测试运行结果
4. 任何设计变更或发现的问题
5. 与任务简报的差异说明（如有）
```
