# RBF 写入路径优化任务简报审阅报告

> **审阅者**：Craftsman
> **审阅日期**：2026-02-01
> **审阅对象**：Phase 0 / Phase 2 / Phase 3 三个任务简报

---

## 1. 整体评估

| 任务 | 质量分 | 可行性 | 说明 |
|:-----|:-------|:-------|:-----|
| Phase 0（缓存 Delegate） | 4/5 | ✅ 可立即执行 | 方案清晰，但简报路径与实际源码路径不一致 |
| Phase 2（Writer/Sink Reset） | 3/5 | ⚠️ 需要调整 | 方案可行，但忽略了 `_sink` 是 `readonly` 的限制以及 CRC 状态 |
| Phase 3（Builder 复用） | 3/5 | ⚠️ 需要调整 | 整体架构合理，但有多处细节需要修正 |

**综合评价**：三个任务简报的整体思路正确，分解方式合理。但存在以下共性问题：
1. 文件路径引用不一致（`RbfFileImpl.cs` 实际位于 `Internal/` 子目录）
2. 对现有代码状态的认知偏差（`_sink` 已是不可变字段）
3. 部分状态重置逻辑不完整

---

## 2. 逐任务审阅

### 2.1 Phase 0：缓存 BeginAppend 的 Delegate

**✅ 可行，小修正后可执行**

#### 问题清单

| ID | 严重度 | 问题 | 修正建议 |
|:---|:-------|:-----|:---------|
| P0-1 | P2 | 文件路径错误 | 简报中写 `atelia/src/Rbf/RbfFileImpl.cs`，实际为 `atelia/src/Rbf/Internal/RbfFileImpl.cs` |
| P0-2 | P3 | 初始化时机 | 建议在字段声明处直接初始化，而非构造函数内，更符合 C# idiom |

#### 验证结果（对照现有代码）

```csharp
// 现有实现 (RbfFileImpl.cs#L66-L71)
var builder = new RbfFrameBuilder(
    _handle,
    tailOffset,
    onCommitCallback: (endOffset) => _tailOffset = endOffset,  // ← 每次分配
    clearBuilderFlag: () => _hasActiveBuilder = false          // ← 每次分配
);
```

**确认**：简报描述与现有代码一致，每次 `BeginAppend` 确实会分配两个 delegate。

#### 建议修正

```csharp
// 字段声明处直接初始化（推荐）
private readonly Action<long> _onCommitCallback;
private readonly Action _clearBuilderFlag;

internal RbfFileImpl(SafeFileHandle handle, long tailOffset) {
    _handle = handle ?? throw new ArgumentNullException(nameof(handle));
    _tailOffset = tailOffset;
    _onCommitCallback = (endOffset) => _tailOffset = endOffset;
    _clearBuilderFlag = () => _hasActiveBuilder = false;
}
```

---

### 2.2 Phase 2：Writer 和 Sink 的 Reset 支持

**⚠️ 需要调整，方案可行但细节不完整**

#### 问题清单

| ID | 严重度 | 问题 | 修正建议 |
|:---|:-------|:-----|:---------|
| P2-1 | P1 | `_sink` 已是非 `readonly` | 简报假设需要移除 `readonly`，但实际代码中 `_sink` 本来就不是 `readonly`（见 [SinkReservableWriter.cs#L24](atelia/src/Data/SinkReservableWriter.cs#L24)：`private readonly IByteSink _sink;`），**更正**：实际代码确实是 `readonly`，但简报中的方案 A 需要移除 `readonly` |
| P2-2 | P1 | **遗漏 CRC 状态** | `SinkReservableWriter.GetCrcSinceReservationEnd()` 依赖 chunk 遍历计算 CRC。若 Reset 后数据残留在 chunk 中，CRC 计算会出错 |
| P2-3 | P2 | **遗漏 `_sizingStrategy` 重置** | `ChunkSizingStrategy` 有状态（`_cumulativeCount`、`_sizeGrowthCount`），Reset 时应考虑是否重置 |
| P2-4 | P2 | Sink 同一文件无需 Reset | 简报中 `RandomAccessByteSink.Reset(long writeOffset)` 正确，但 per-file 复用场景下文件句柄不变，只需更新 offset |

#### 验证结果（对照现有代码）

```csharp
// SinkReservableWriter.Reset() 现有实现 (L232-L249)
public void Reset() {
    ObjectDisposedException.ThrowIf(_disposed, this);

    foreach (var c in _chunks) {
        if (c.IsRented) {
            _pool.Return(c.Buffer);
        }
    }
    _chunks.Clear();

    _reservations.Clear();

    _writtenLength = 0;
    _pushedLength = 0;

    _hasLastSpan = false;
    _lastSpanLength = 0;
    // Do not reset _reservationSerial to avoid token reuse hazards.
}
```

**观察**：现有 `Reset()` 不重置 `_sink`，符合"保持当前 Sink"的语义。但需要确认 `_reservations.Clear()` 是否完整清理。

#### 建议修正

**方案 A 改进版**：

```csharp
// SinkReservableWriter.cs
private IByteSink _sink;  // 移除 readonly

/// <summary>
/// 重置 Writer 状态，可选切换底层 Sink。
/// </summary>
/// <param name="newSink">新的 Sink，若为 null 则保持当前 Sink</param>
/// <param name="resetSizingStrategy">是否重置 ChunkSizingStrategy（默认 false，保留增长策略）</param>
public void Reset(IByteSink? newSink = null, bool resetSizingStrategy = false)
{
    ObjectDisposedException.ThrowIf(_disposed, this);

    // 现有清理逻辑...
    foreach (var c in _chunks) { if (c.IsRented) { _pool.Return(c.Buffer); } }
    _chunks.Clear();
    _reservations.Clear();
    _writtenLength = 0;
    _pushedLength = 0;
    _hasLastSpan = false;
    _lastSpanLength = 0;

    // 新增：切换 sink
    if (newSink is not null) { _sink = newSink; }

    // 可选：重置 sizing 策略
    if (resetSizingStrategy) {
        _sizingStrategy = new ChunkSizingStrategy(options.MinChunkSize, options.MaxChunkSize);
    }
}
```

**RandomAccessByteSink.Reset() 需要补充**：

```csharp
// RandomAccessByteSink.cs - 新增方法
public void Reset(long writeOffset)
{
    _writeOffset = writeOffset;
}
```

---

### 2.3 Phase 3：RbfFrameBuilder 的 Per-File 复用

**⚠️ 需要调整，核心架构正确但有多处细节问题**

#### 问题清单

| ID | 严重度 | 问题 | 修正建议 |
|:---|:-------|:-----|:---------|
| P3-1 | P1 | **Reset 签名不匹配** | 简报中 `Reset(long frameStart, Action<long>, Action)` 需要传 3 个参数，但 Phase 0 已缓存 delegate 在 `RbfFileImpl`，不应每次传入 |
| P3-2 | P1 | **`_headLenReservationToken` 是 `readonly`** | 现有代码声明为 `private readonly int _headLenReservationToken`，Reset 时无法重新赋值 |
| P3-3 | P1 | **Dispose/DisposeInternal 责任混淆** | 简报让 `Dispose()` 不释放 `_writer`，但 `_writer.Reset()` 已在 Auto-Abort 分支调用，逻辑重复 |
| P3-4 | P2 | **`_frameStart` 是 `readonly`** | 现有代码 `private readonly long _frameStart`，Reset 时无法重新赋值 |
| P3-5 | P2 | **`_onCommitCallback`/`_clearBuilderFlag` 是 `readonly`** | 同上，需要移除 `readonly` 或改为缓存在 RbfFileImpl 而非 Builder |
| P3-6 | P2 | **状态机检查不完整** | `CanBeReused` 检查 `_disposed || _committed`，但未检查 "部分写入后 Reset" 的边界情况 |

#### 验证结果（对照现有代码）

```csharp
// RbfFrameBuilder.cs 当前字段 (L16-L25)
public sealed class RbfFrameBuilder : IDisposable {
    private readonly long _frameStart;                    // ← readonly
    private readonly RandomAccessByteSink _sink;          // ← readonly
    private readonly SinkReservableWriter _writer;        // ← readonly
    private readonly int _headLenReservationToken;        // ← readonly
    private readonly Action<long> _onCommitCallback;      // ← readonly
    private readonly Action _clearBuilderFlag;            // ← readonly
    private bool _committed;
    private bool _disposed;
```

**关键发现**：6 个 `readonly` 字段需要变为可变才能支持 Reset。这是**架构层面的变更**，比简报描述的范围更大。

#### 建议修正

**方案调整**：简化 Reset 签名，回调复用 Phase 0 缓存

```csharp
// RbfFrameBuilder.cs
public sealed class RbfFrameBuilder : IDisposable {
    private long _frameStart;                    // 移除 readonly
    private readonly RandomAccessByteSink _sink; // 保持 readonly（同一文件）
    private readonly SinkReservableWriter _writer; // 保持 readonly
    private int _headLenReservationToken;        // 移除 readonly
    private Action<long> _onCommitCallback;      // 移除 readonly
    private Action _clearBuilderFlag;            // 移除 readonly
    private bool _committed;
    private bool _disposed;

    /// <summary>重置 Builder，准备写入新帧。</summary>
    internal void Reset(long frameStart, Action<long> onCommitCallback, Action clearBuilderFlag)
    {
        // 前置检查
        if (!_disposed && !_committed) {
            throw new InvalidOperationException(
                "Cannot reset an active builder. Dispose or commit first.");
        }

        _frameStart = frameStart;
        _onCommitCallback = onCommitCallback;
        _clearBuilderFlag = clearBuilderFlag;
        _committed = false;
        _disposed = false;

        // 重置 Sink offset
        _sink.Reset(frameStart);

        // 重置 Writer（清空 buffer，释放旧 chunks）
        _writer.Reset();

        // 重新预留 HeadLen
        _ = _writer.ReserveSpan(FrameLayout.HeadLenSize, out _headLenReservationToken, tag: "HeadLen");
    }
}
```

**Dispose 逻辑调整**：

```csharp
// 复用场景下的 Dispose：只重置状态，不释放资源
public void Dispose() {
    if (_disposed) { return; }
    _disposed = true;

    if (!_committed) {
        // Auto-Abort: 重置 writer 但不 Dispose
        _writer.Reset();
    }

    // 通知 Facade
    _clearBuilderFlag?.Invoke();
    
    // 注意：不调用 _writer.Dispose()，因为要复用
}

/// <summary>彻底释放（仅 RbfFileImpl.Dispose 调用）</summary>
internal void DisposeInternal() {
    if (!_disposed) { Dispose(); }  // 确保先走正常 Dispose 流程
    _writer.Dispose();
}
```

---

## 3. 遗漏风险

### 3.1 未识别的风险点

| ID | 风险 | 影响 | 建议 |
|:---|:-----|:-----|:-----|
| R-1 | **CRC 计算依赖 chunk 遍历** | Reset 后若有残留数据，`GetCrcSinceReservationEnd()` 可能读到旧数据 | 确保 `_writer.Reset()` 彻底清空所有 chunks |
| R-2 | **Reservation token 重用** | `SinkReservableWriter` 内部 `_reservationSerial` 不重置（故意设计），但跨 Reset 的 token 可能冲突 | 验证 token 生成逻辑，考虑 generation ID |
| R-3 | **并发 Dispose 风险** | 若 RbfFileImpl.Dispose 和 Builder.Dispose 并发调用，可能 double-invoke callback | 使用 Interlocked 原子操作保护 `_hasActiveBuilder` |
| R-4 | **ChunkSizingStrategy 状态累积** | 多次 Reset 后，chunk size 策略会持续增长，可能导致内存占用上升 | 考虑在 Reset 时可选重置 sizing 策略 |

### 3.2 测试覆盖盲区

| 场景 | 风险 | 当前覆盖 |
|:-----|:-----|:---------|
| Reset 后立即 Dispose（无写入） | 状态机边界 | ❌ 未提及 |
| 连续 Abort 多次（Reset → 部分写入 → Dispose × N） | 脏状态累积 | ❌ 未提及 |
| Reset 后 ReserveSpan 超过当前 chunk 容量 | chunk 创建时机 | ❌ 未提及 |
| 异常恢复：Reset 过程中抛异常 | Builder 状态一致性 | ❌ 未提及 |

---

## 4. 测试建议

### 4.1 Phase 0 补充测试

```csharp
[Fact]
public void BeginAppend_ReusesDelegate_SameInstance()
{
    using var file = RbfFile.Create(tempPath);
    
    var builder1 = file.BeginAppend();
    // 通过反射或内部 API 获取 callback 引用
    var callback1 = GetOnCommitCallback(builder1);
    builder1.Dispose();
    
    var builder2 = file.BeginAppend();
    var callback2 = GetOnCommitCallback(builder2);
    builder2.Dispose();
    
    // 验证是同一个 delegate 实例
    Assert.Same(callback1, callback2);
}
```

### 4.2 Phase 2 补充测试

```csharp
[Fact]
public void Reset_WithNewSink_ClearsAllState()
{
    var sink1 = new TestByteSink();
    var writer = new SinkReservableWriter(sink1);
    
    // 写入并预留
    writer.Write([1, 2, 3]);
    _ = writer.ReserveSpan(4, out var token, "test");
    
    // Reset 到新 sink
    var sink2 = new TestByteSink();
    writer.Reset(sink2);
    
    // 验证状态清空
    Assert.Equal(0, writer.WrittenLength);
    Assert.Equal(0, writer.PendingReservationCount);
    Assert.False(writer.TryGetReservationSpan(token, out _)); // 旧 token 失效
}

[Fact]
public void Reset_ClearsChunks_CrcComputationCorrect()
{
    var sink = new TestByteSink();
    var writer = new SinkReservableWriter(sink);
    
    // 第一轮写入
    _ = writer.ReserveSpan(4, out var token1, "head");
    writer.Write([1, 2, 3, 4, 5, 6, 7, 8]);
    var crc1 = writer.GetCrcSinceReservationEnd(token1);
    
    writer.Reset();
    
    // 第二轮写入（不同数据）
    _ = writer.ReserveSpan(4, out var token2, "head");
    writer.Write([9, 9, 9, 9]);
    var crc2 = writer.GetCrcSinceReservationEnd(token2);
    
    // CRC 应基于新数据计算
    Assert.NotEqual(crc1, crc2);
}
```

### 4.3 Phase 3 补充测试

```csharp
[Fact]
public void Reset_AfterPartialWrite_Abort_NoLeakage()
{
    using var file = RbfFile.Create(tempPath);
    
    // 第一帧：部分写入后 abort
    using (var builder = file.BeginAppend())
    {
        builder.PayloadAndMeta.Write("PartialData"u8);
        // 不调用 EndAppend，Dispose 触发 abort
    }
    
    // 验证文件未被修改
    Assert.Equal(0, file.TailOffset);
    
    // 第二帧：正常写入
    using (var builder = file.BeginAppend())
    {
        builder.PayloadAndMeta.Write("RealData"u8);
        builder.EndAppend(tag: 1);
    }
    
    // 验证只有一帧，且内容正确
    var frames = file.ReadAllFrames();
    Assert.Single(frames);
    Assert.Equal("RealData"u8.ToArray(), frames[0].Payload.ToArray());
}

[Fact]
public void Reset_DuringActiveBuilder_ThrowsInvalidOperation()
{
    using var file = RbfFile.Create(tempPath);
    var builder = file.BeginAppend();
    
    // 尝试在 builder 活跃时调用 Reset（应失败）
    builder.PayloadAndMeta.Write([1, 2, 3]);
    
    Assert.Throws<InvalidOperationException>(() => 
        builder.Reset(100, _ => {}, () => {}));
    
    builder.Dispose();
}

[Fact]
public void RbfFileImpl_Dispose_DisposesReusableBuilder()
{
    var file = RbfFile.Create(tempPath);
    var builder = file.BeginAppend();
    builder.PayloadAndMeta.Write([1, 2, 3]);
    builder.EndAppend(tag: 1);
    
    // Dispose file（应级联 dispose builder 内部资源）
    file.Dispose();
    
    // 验证 builder 的 writer 已释放（通过行为验证）
    // 此处需要内部 API 或间接验证
}
```

---

## 5. 审阅结论

### 5.1 执行建议

| 任务 | 结论 | 前置条件 |
|:-----|:-----|:---------|
| **Phase 0** | ✅ **可立即执行** | 修正文件路径 |
| **Phase 2** | ⚠️ **需要修正后执行** | 确认 `readonly` 移除；补充 CRC 相关测试 |
| **Phase 3** | ⚠️ **需要修正后执行** | 移除多个 `readonly` 修饰符；调整 Dispose 逻辑 |

### 5.2 必须修正项

1. **[P3-2] 移除 `_headLenReservationToken` 的 `readonly`**：否则 Reset 无法重新赋值
2. **[P3-4/P3-5] 移除多个 `readonly`**：`_frameStart`、`_onCommitCallback`、`_clearBuilderFlag`
3. **[R-1] 验证 CRC 计算在 Reset 后的正确性**：添加专门测试

### 5.3 建议修正项

1. **[P0-2] 字段声明处初始化**：更符合 C# 惯用法
2. **[P2-3] 考虑 `_sizingStrategy` 重置**：防止内存占用持续增长
3. **[R-2] 验证 token 重用安全性**：跨 Reset 的 token 不应有效

### 5.4 最终判定

> **可以开始执行**，但需要先完成以下修正：
> 1. 更新 Phase 3 简报中的 `readonly` 字段列表
> 2. 补充 CRC 计算正确性测试用例
> 3. 确认 Dispose/DisposeInternal 的责任边界

---

*审阅完成 — Craftsman*
