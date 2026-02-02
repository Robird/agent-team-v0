# Task: Phase 2 - Writer 和 Sink 的 Reset 支持

> **预估工时**：3h
> **执行者**：Implementer
> **依赖**：Phase 0（但可并行开发）
> **收益**：为 Phase 3 的 per-file Builder 复用奠定基础

---

## 1. 问题背景

当前 `SinkReservableWriter` 已有 `Reset()` 方法，但只能重置内部状态，无法切换底层 sink。
`RandomAccessByteSink` 没有 Reset 方法，每次都需要 new。

为实现 Phase 3 的 per-file Builder 复用，需要让 Writer 和 Sink 都支持"带参数的 Reset"。

## 2. 目标

1. `RandomAccessByteSink` 添加 `Reset(long writeOffset)` 方法
2. `SinkReservableWriter` 添加 `Reset(IByteSink sink)` 重载（或修改现有 Reset）

## 3. 修改范围

### 3.1 RandomAccessByteSink

**文件**：`/repos/focus/atelia/src/Rbf/Internal/RandomAccessByteSink.cs`

**添加方法**：

```csharp
/// <summary>
/// 重置写入偏移量，复用同一个 Sink 实例。
/// </summary>
/// <param name="writeOffset">新的起始写入偏移量</param>
public void Reset(long writeOffset)
{
    _writeOffset = writeOffset;
}
```

**注意**：
- `_file` (SafeFileHandle) 不变，因为是同一个文件
- 只需重置 `_writeOffset`

### 3.2 SinkReservableWriter

**文件**：`/repos/focus/atelia/src/Data/SinkReservableWriter.cs`

**方案 A（推荐）：修改现有 Reset 方法签名**

```csharp
/// <summary>
/// 重置 Writer 状态，可选切换底层 Sink。
/// </summary>
/// <param name="newSink">新的 Sink，若为 null 则保持当前 Sink</param>
public void Reset(IByteSink? newSink = null)
{
    // 现有 Reset 逻辑
    FlushAndRecycleAllChunks();
    _tracker.Reset();
    _committedEnd = 0;
    // ...
    
    // 新增：切换 sink
    if (newSink is not null)
    {
        _sink = newSink;
    }
}
```

**方案 B（备选）：添加 Reset 重载**

```csharp
public void Reset() { /* 现有逻辑 */ }

public void Reset(IByteSink newSink)
{
    Reset();
    _sink = newSink;
}
```

**决策建议**：方案 A 更简洁，但需要将 `_sink` 从 `readonly` 改为可变。请在实现前确认当前 `_sink` 字段的修饰符。

### 3.3 字段可变性调整（必须）

经审阅确认，`_sink` 当前**是** `readonly`（见 [SinkReservableWriter.cs#L24](../../../atelia/src/Data/SinkReservableWriter.cs#L24)），**必须**移除 `readonly` 修饰符才能支持 Reset 切换 sink：

```csharp
// 修改前
private readonly IByteSink _sink;

// 修改后
private IByteSink _sink;
```

### 3.4 ChunkSizingStrategy 状态考量

`ChunkSizingStrategy` 有内部状态（累计 chunk 数、增长计数），多次 Reset 后可能导致 chunk 尺寸持续增长。**当前阶段暂不重置**，但需在测试中观察内存占用情况。

## 4. 验收标准

1. **编译通过**
2. **现有测试通过**：`dotnet test atelia/tests/Rbf.Tests/ && dotnet test atelia/tests/Data.Tests/`
3. **新增单元测试**：
   - `RandomAccessByteSink.Reset()` 后写入位置正确
   - `SinkReservableWriter.Reset(newSink)` 后使用新 sink 写入

### 4.1 建议的测试用例
**CRC 计算正确性测试**（审阅补充 - 关键）：
```csharp
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
    
    // CRC 应基于新数据计算，不受旧数据影响
    Assert.NotEqual(crc1, crc2);
}
```
**RandomAccessByteSink Reset 测试**：
```csharp
[Fact]
public void Reset_UpdatesWriteOffset()
{
    using var file = CreateTempFile();
    var sink = new RandomAccessByteSink(file, startOffset: 0);
    
    sink.Push(new byte[] { 1, 2, 3 });
    // 此时内部 offset 应为 3
    
    sink.Reset(100);
    sink.Push(new byte[] { 4, 5 });
    // 应写入到 offset 100-101
    
    // 验证文件内容
}
```

**SinkReservableWriter Reset 测试**：
```csharp
[Fact]
public void Reset_WithNewSink_UseNewSink()
{
    var sink1 = new MockByteSink();
    var sink2 = new MockByteSink();
    var writer = new SinkReservableWriter(sink1);
    
    writer.Write(new byte[] { 1, 2, 3 });
    writer.Commit();
    Assert.True(sink1.ReceivedData);
    
    writer.Reset(sink2);
    writer.Write(new byte[] { 4, 5, 6 });
    writer.Commit();
    Assert.True(sink2.ReceivedData);
}
```

## 5. 风险评估

| 风险 | 概率 | 影响 | 缓解 |
|:-----|:-----|:-----|:-----|
| Reset 不完整导致脏状态 | 中 | 高 | 全面的 Reset 测试覆盖 |
| 移除 readonly 降低安全性 | 低 | 低 | Reset 只在明确场景调用 |
| 与现有 Reset 调用者冲突 | 低 | 中 | 使用可选参数保持兼容 |

## 6. 注意事项

- 确保 `FlushAndRecycleAllChunks()` 在切换 sink 前被调用（避免数据写入错误 sink）
- 考虑 `Reset(newSink)` 是否应该重置 `_committedEnd`（应该重置，因为是新的写入会话）
- 如果 Writer 处于"有未提交数据"状态时调用 Reset，当前行为是丢弃——确认这是期望行为

---

## 执行指令（供 runSubagent 使用）

```
你是 Implementer，请执行以下任务：

## 第一步：分析现有实现

1. 阅读 `/repos/focus/atelia/src/Rbf/Internal/RandomAccessByteSink.cs`
   - 确认字段列表和构造函数
   - 确认是否有现有 Reset 方法

2. 阅读 `/repos/focus/atelia/src/Data/SinkReservableWriter.cs`
   - 确认 `_sink` 字段的修饰符（是否 readonly）
   - 阅读现有 `Reset()` 方法的实现
   - 理解 `FlushAndRecycleAllChunks()` 的行为

## 第二步：实现修改

1. 在 RandomAccessByteSink 中添加 `Reset(long writeOffset)` 方法
2. 修改 SinkReservableWriter：
   - 如果 `_sink` 是 readonly，移除 readonly
   - 修改 Reset 方法支持可选的 newSink 参数

## 第三步：添加测试

在以下位置添加测试：
- `/repos/focus/atelia/tests/Rbf.Tests/` - RandomAccessByteSink 测试
- `/repos/focus/atelia/tests/Data.Tests/` - SinkReservableWriter 测试

## 第四步：验证

```bash
cd /repos/focus && dotnet build atelia/src/Rbf/ && dotnet build atelia/src/Data/
cd /repos/focus && dotnet test atelia/tests/Rbf.Tests/ && dotnet test atelia/tests/Data.Tests/
```

## 输出要求

完成后报告：
1. 修改的文件和具体变更
2. 新增的测试用例
3. 测试运行结果
4. 任何设计决策或发现的问题
```
