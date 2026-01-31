# 畅谈会：ReadFrame 重构与 ScanReverse 解耦设计

**日期**：2026-01-15
**标签**：`#design` `#refactor`
**主持**：TeamLeader
**参与者**：Craftsman, Craftsman.OpenRouter, Implementer

---

## 背景

基于上一轮畅谈（`2026-01-15-readframe-buffer-lifetime.md`）的决议，我们需要：

1. **让 `ScanReverse` 与 `ReadFrame` 解耦**
   - `ScanReverse` 从返回 `RbfFrame` 序列改为返回 `SizedPtr` 序列
   - `ScanReverse` 只进行基本结构校验（首尾长度相等、首尾 fence、StatusByte、tombstone）
   - 默认不进行 CRC 校验，提供可选参数开启 CRC 校验

2. **在 `RbfRawOps` 层拆出 Buffer 外置的底层接口**
   - 当前 `ReadFrame` 实现可分为 3 步：长度检查 → 划分缓存 → 读磁盘填缓存
   - 核心是"读磁盘填缓存"这一步
   - 实现 `RbfFrame ReadFrame(SizedPtr ptr, Span<byte> buffer)` 作为底层基础

---

## 当前实现分析

### RbfRawOps.ReadFrame 当前结构（L30-223）

```
1. 参数校验 (L32-81)
   - Offset 4B 对齐
   - Length 4B 对齐
   - Length >= MinFrameLength (20B)
   - Offset/Length 可表示性检查

2. Buffer 分配 (L83-87)
   - lengthBytes <= 4096: stackalloc
   - lengthBytes > 4096: ArrayPool.Rent

3. 读磁盘填缓存 + 校验 + 构造结果 (L89-223)
   - RandomAccess.Read 读取整个 FrameBytes
   - Framing 校验（HeadLen, StatusByte, PayloadLen, StatusLen, TailLen）
   - CRC 校验
   - 构造 RbfFrame（当前被迫复制 payload 到堆数组）
   - 归还 ArrayPool buffer
```

### 问题

- 步骤 2 和 3 强耦合：buffer 在 try-finally 中分配和归还，无法复用
- 返回 `AteliaResult<RbfFrame>` 时 `RbfFrame.Payload` 必须是 owned（堆数组），违背 zero-copy 初衷

---

## 待讨论问题

### Q1: Buffer 外置接口的签名设计

**候选 A**：纯 Span 输入输出
```csharp
AteliaResult<RbfFrame> ReadFrameInto(SafeFileHandle file, SizedPtr ptr, Span<byte> buffer);
```

**候选 B**：分离校验与读取
```csharp
// 只读取，不校验（最底层）
int ReadFrameRaw(SafeFileHandle file, SizedPtr ptr, Span<byte> buffer);

// 校验已读取的 buffer
AteliaResult<RbfFrame> ValidateAndParseFrame(SizedPtr ptr, ReadOnlySpan<byte> frameBytes);
```

**候选 C**：带 out 参数返回所需长度
```csharp
AteliaResult<RbfFrame> TryReadFrameInto(SafeFileHandle file, SizedPtr ptr, Span<byte> buffer, out int requiredLength);
```

### Q2: ScanReverse 返回 SizedPtr 后的 API 形态

当前设计（返回 `RbfFrame`）：
```csharp
foreach (var frame in file.ScanReverse()) {
    // frame.Ptr, frame.Tag, frame.Payload, frame.IsTombstone
}
```

新设计（返回 `SizedPtr`）需要考虑：
- 如何暴露 `IsTombstone` 用于过滤？
- 是否需要一个轻量级结构 `RbfFrameHeader` 包含 `SizedPtr` + `IsTombstone`？

### Q3: CRC 校验的可选开关位置

- 在 `ScanReverse` 参数上？`ScanReverse(bool showTombstone = false, bool verifyCrc = false)`
- 在 `ReadFrame` 参数上？`ReadFrame(ptr, buffer, skipCrc: true)`
- 两者都有？

### Q4: 与现有测试的兼容性

当前有 146 个测试通过，重构后如何保持兼容？
- 保留当前 `ReadFrame(file, ptr)` 签名作为便捷层（内部调用新底层）？
- 还是直接替换签名？

---

## 请各位参谋发言

请从以下角度分析：

1. **API 设计**：各候选方案的易用性、正确性、扩展性
2. **实现复杂度**：重构工作量、测试影响
3. **性能特征**：zero-copy 可达性、分配情况
4. **与 ScanReverse 的协同**：两个 API 如何配合使用

---

## Craftsman 发言

### Q1: Buffer 外置接口签名设计

#### 当前问题的本质

当前实现被迫做堆分配：
```csharp
// 重要：RbfFrame 是 ref struct，其 Payload 是 ReadOnlySpan<byte>。
// 如果使用了 ArrayPool，buffer 在返回后会被归还，Payload 引用将失效！
byte[] payloadArray = new byte[payloadLen];
```

这违背了 **zero-copy 热路径** 的设计目标。

#### 候选方案对比

| 维度 | **A: ReadFrameInto** | **B: 分离 Raw + Validate** | **C: TryReadFrameInto + out** |
|:-----|:-------------------|:-------------------------|:------------------------------|
| **API 复杂度** | 低（单函数） | 高（两函数协作） | 中（单函数但多返回路径） |
| **职责清晰度** | 中（读+校验耦合） | **高**（职责单一） | 中 |
| **Zero-copy 可达性** | ✅ | ✅ | ✅ |
| **调用方负担** | 低 | 高（需正确组合） | 中 |
| **长度未知时处理** | 调用方需预分配 | 调用方需预分配 | **支持探测式调用** |
| **ScanReverse 适配** | 适中 | **最优**（可复用 Validate） | 适中 |

#### 推荐：混合方案 A+B'

**核心洞察**：候选 B 的"分离"思想正确，但暴露两个公开函数增加误用风险。建议：

1. **公开层**：提供单一入口 `ReadFrameInto`（用户友好）
2. **内部层**：拆分为 `ReadRaw` + `ValidateAndParse`（便于 ScanReverse 复用）

```csharp
// ======================== 公开 API ========================
/// <summary>将帧读入调用方提供的 buffer，返回解析后的 RbfFrame。</summary>
/// <remarks>
/// 生命周期警告：返回的 RbfFrame.Payload 直接引用 buffer，
/// 调用方 MUST 确保 buffer 在使用 Payload 期间有效。
/// </remarks>
public static AteliaResult<RbfFrame> ReadFrameInto(
    SafeFileHandle file, 
    SizedPtr ptr, 
    Span<byte> buffer);

// ======================== 内部 API（供 ScanReverse 复用）========================
internal static partial class RbfRawOps {
    /// <summary>原始读取：仅执行 I/O，不校验。</summary>
    internal static int ReadRaw(SafeFileHandle file, SizedPtr ptr, Span<byte> buffer);
    
    /// <summary>校验并解析已读入 buffer 的帧数据。</summary>
    internal static AteliaResult<RbfFrame> ValidateAndParse(
        SizedPtr ptr, 
        ReadOnlySpan<byte> frameBytes, 
        bool skipCrc = false);
}
```

**为什么不选纯 B**：暴露 `ReadFrameRaw` 返回 `int` 违反 Result-Pattern 精神，调用方容易忘记调用 Validate。

**为什么不选 C**：`out int requiredLength` 在 RBF 场景下几乎无用——因为 `SizedPtr` 已经告诉你所需长度。

---

### Q2: ScanReverse 返回类型变更

#### 推荐：引入 `RbfFrameHeader`

```csharp
/// <summary>帧头信息（不含 Payload）。</summary>
/// <remarks>
/// 用于 ScanReverse 场景的轻量迭代。
/// 注意：这是 readonly struct（非 ref struct），可以装箱/存储。
/// </remarks>
public readonly struct RbfFrameHeader {
    public SizedPtr Ptr { get; init; }
    public uint Tag { get; init; }
    public bool IsTombstone { get; init; }
}
```

**关键理由**：

1. **IsTombstone 必须暴露**：@[S-RBF-SCANREVERSE-TOMBSTONE-FILTER] 要求过滤 Tombstone，仅凭 `SizedPtr` 无法实现
2. **Tag 有价值**：上层通常需要按 Tag 过滤/路由，不暴露 Tag 则被迫二次 I/O
3. **为何不用纯 SizedPtr**：无法携带 `IsTombstone`，上层必须再次读取才能判断

**规范变更**：

```csharp
public ref struct RbfReverseEnumerator {
    public RbfFrameHeader Current { get; }  // ← 从 RbfFrame 改为 RbfFrameHeader
    public bool MoveNext();
}
```

---

### Q3: CRC 校验的可选开关

#### 推荐：双层控制，默认安全

```csharp
public enum CrcCheckPolicy {
    Always,  // 默认：始终校验
    Skip,    // 跳过 CRC（性能优先场景）
}

// ReadFrameInto：参数控制
public static AteliaResult<RbfFrame> ReadFrameInto(
    SafeFileHandle file, SizedPtr ptr, Span<byte> buffer,
    CrcCheckPolicy crcPolicy = CrcCheckPolicy.Always);

// ScanReverse：参数控制
public RbfReverseSequence ScanReverse(
    bool showTombstone = false,
    CrcCheckPolicy crcPolicy = CrcCheckPolicy.Always);
```

**为什么两者都有**：ReadFrameInto 用于批量处理可跳过，ScanReverse 恢复场景可开启。

**为什么默认 Always**：符合 Fail-Safe Default 原则。

---

### 最终推荐方案摘要

```csharp
// ======================= 新类型 =======================
public readonly struct RbfFrameHeader { ... }
public enum CrcCheckPolicy { Always, Skip }

// ======================= 公开 API 变更 =======================
// 1. 保留 ReadFrame（兼容现有代码）
AteliaResult<RbfFrame> ReadFrame(SizedPtr ptr);

// 2. 新增 Buffer 外置版本（zero-copy 热路径）
AteliaResult<RbfFrame> ReadFrameInto(SizedPtr ptr, Span<byte> buffer, CrcCheckPolicy crcPolicy = Always);

// 3. ScanReverse：返回 RbfFrameHeader 而非 RbfFrame
RbfReverseSequence ScanReverse(bool showTombstone = false, CrcCheckPolicy crcPolicy = Always);

// ======================= 内部 API =======================
internal static int ReadRaw(SafeFileHandle file, SizedPtr ptr, Span<byte> buffer);
internal static AteliaResult<RbfFrame> ValidateAndParse(SizedPtr ptr, ReadOnlySpan<byte> frameBytes, bool skipCrc = false);
internal static AteliaResult<RbfFrameHeader> ValidateAndParseHeader(SizedPtr ptr, ReadOnlySpan<byte> headerBytes, bool skipCrc = false);
```

---

## Implementer 发言

### 1. 代码重构策略

#### 1.1 文件拆分方案

继续使用 `partial class`，按功能拆分为 3 个文件：

| 新文件 | 职责 | 预估行数 |
|:-------|:-----|:---------|
| `RbfRawOps.ReadRaw.cs` | 纯 I/O：`ReadRaw()` | ~30 |
| `RbfRawOps.ValidateAndParse.cs` | 校验 + 解析 | ~120 |
| `RbfRawOps.ReadFrame.cs` | 公开 API + 兼容层 | ~80 |

#### 1.2 新增类型定义

```csharp
// atelia/src/Rbf/RbfFrameHeader.cs
public readonly struct RbfFrameHeader {
    public SizedPtr Ptr { get; init; }
    public uint Tag { get; init; }
    public bool IsTombstone { get; init; }
}

// atelia/src/Rbf/CrcCheckPolicy.cs
public enum CrcCheckPolicy { Always, Skip }

// atelia/src/Rbf/Internal/RbfErrors.cs（新增）
internal sealed record RbfBufferTooSmallError(
    string Message,
    int RequiredBytes,
    int ProvidedBytes,
    string? RecoveryHint = null
) : AteliaError("Rbf.BufferTooSmall", Message, RecoveryHint);
```

---

### 2. 测试兼容性

#### 2.1 兼容层保留

**MUST 保留**旧签名作为便捷层：

```csharp
public static AteliaResult<RbfFrame> ReadFrame(SafeFileHandle file, SizedPtr ptr) {
    // 分配 buffer（stackalloc 或 ArrayPool）
    // 调用 ReadFrameInto(file, ptr, buffer)
    // 如果成功，复制 Payload 到堆数组（维持旧行为）
    // 返回
}
```

这样 **所有现有测试无需修改**。

#### 2.2 新增测试清单

| 新方法 | 新增测试 |
|:-------|:---------|
| `ReadFrameInto` | `BufferTooSmall_ReturnsBufferError` |
| `ReadFrameInto` | `ValidBuffer_PayloadIsSlice`（验证 zero-copy） |
| `ReadFrameInto` | `SkipCrc_DoesNotCheckCrc` |
| `ValidateAndParse` | `LengthMismatch_ReturnsError` |
| `ValidateAndParseHeader` | `OnlyChecksHeadTailStatus` |

---

### 3. ScanReverse 实现变化

#### 3.1 最小读取需求

逆向扫描只需读取帧的 **尾部区域** 即可构造 `RbfFrameHeader`：

```
帧布局：[HeadLen(4)][Tag(4)][Payload(N)][Status(1-4)][TailLen(4)][CRC(4)]
```

**逆向定位算法**：
1. 从当前位置向前读 4 字节 → TailLen（帧总长度）
2. 计算帧起始位置 = 当前位置 - TailLen
3. 读取 HeadLen 验证 HeadLen == TailLen
4. 读取 Tag 字段
5. 读取 StatusByte 获取 IsTombstone
6. 构造 `RbfFrameHeader { Ptr, Tag, IsTombstone }`

#### 3.2 复用 ValidateAndParseHeader

```csharp
// ScanReverse 内部伪代码
while (currentOffset > HeaderFenceLength) {
    // 1. 读取尾部 TailLen
    Span<byte> tailBuf = stackalloc byte[4];
    RandomAccess.Read(file, tailBuf, currentOffset - 4);
    uint tailLen = BinaryPrimitives.ReadUInt32LittleEndian(tailBuf);
    
    // 2. 计算帧范围
    var ptr = SizedPtr.Create(currentOffset - tailLen, tailLen);
    
    // 3. 读取最小校验区域
    Span<byte> headerBuf = stackalloc byte[MinHeaderCheckSize];
    
    // 4. 调用 ValidateAndParseHeader
    var result = ValidateAndParseHeader(ptr, headerBuf, skipCrc: !verifyCrc);
    
    currentOffset = ptr.OffsetBytes;
}
```

---

### 4. 边界情况处理

| 情况 | 处理方式 |
|:-----|:---------|
| Buffer 长度不足 | 返回 `RbfBufferTooSmallError`（含 RequiredBytes） |
| frameBytes.Length != ptr.LengthBytes | 返回 `RbfArgumentError` |
| Short Read | 返回 `RbfArgumentError`（ptr 可能越界） |

---

### 5. 实现优先级

| 阶段 | 任务 | 依赖 |
|:-----|:-----|:-----|
| **P0** | 定义 `RbfFrameHeader`, `CrcCheckPolicy`, `RbfBufferTooSmallError` | 无 |
| **P1** | 实现 `ValidateAndParse`, `ValidateAndParseHeader` | P0 |
| **P2** | 实现 `ReadRaw`, `ReadFrameInto` | P1 |
| **P3** | 重构 `ReadFrame` 为兼容层 | P2 |
| **P4** | 实现 `ScanReverse` | P1 |

---

## TeamLeader 收敛总结

感谢 Craftsman 和 Implementer 的深入分析。两位视角互补：Craftsman 从 API 设计层面给出了完整的类型和签名建议；Implementer 从实现角度给出了文件拆分、测试策略和边界处理方案。

### 核心决议

1. **API 分层策略**：
   - 公开层：`ReadFrameInto`（zero-copy）+ `ReadFrame`（兼容层，内部复制）
   - 内部层：`ReadRaw` + `ValidateAndParse` + `ValidateAndParseHeader`

2. **ScanReverse 解耦**：
   - 返回 `RbfFrameHeader`（含 `Ptr`, `Tag`, `IsTombstone`）而非 `RbfFrame`
   - 只做结构校验，CRC 可选

3. **CRC 控制**：
   - 引入 `CrcCheckPolicy` 枚举
   - 默认 `Always`，性能场景可 `Skip`

4. **兼容性**：
   - 保留 `ReadFrame(file, ptr)` 签名，内部调用新底层 + 复制
   - 现有 146 个测试无需修改

### 后续行动

1. **监护人确认**：请监护人审阅此方案，确认后执行
2. **代码实现**：按 P0→P4 优先级实施
3. **文档更新**：实现完成后更新 `rbf-interface.md`

### 会议产出

- **候选方案**：A、B、C 三种签名，S1、S2、S3 三种返回类型
- **最终决策**：混合方案 A+B'（公开单入口 + 内部分离）+ S2（RbfFrameHeader）
- **状态**：待监护人确认

---

## 监护人决策（2026-01-16）

监护人审阅后做出以下简化决策：

### 1. 移除兼容层

**决策**：直接替换 `ReadFrame(file, ptr)` 为 `ReadFrameInto(file, ptr, buffer)`，不保留兼容层。

**理由**：当前无外部依赖，没有兼容性包袱，轻装上阵便于彻底重构。

### 2. 简化 CRC 策略

**决策**：不引入 `CrcCheckPolicy` 参数。

- `ReadFrameInto` **始终校验** CRC
- `ScanReverse` **始终不校验** CRC

**理由**：
- 简化 API，减少用户决策负担
- 职责明确：ScanReverse 管结构迭代，ReadFrameInto 管内容校验
- 避免过早优化

### 3. 阶段调整

**决策**：将此重构作为新的 Stage 05，原 Stage 05-08 顺延为 06-09。

### 4. 后续探讨方向（记录备忘）

监护人提到 ScanReverse 返回类型可能需要进一步设计：
- 命名可能改为 `FrameInfo`（类似 `FileInfo`）
- 包含 `SizedPtr` + `Tag` + `IsTombstone`
- 可能绑定 `RbfFileImpl` 实例，支持惰性读取
- 具体设计待 Stage 05 完成后在 Stage 07 中细化

### 最终方案

```csharp
// ======================= 公开 API =======================
// 移除旧签名，直接实现 Buffer 外置
AteliaResult<RbfFrame> ReadFrameInto(SafeFileHandle file, SizedPtr ptr, Span<byte> buffer);
// 始终校验 CRC，无 CrcCheckPolicy 参数

// ScanReverse（Stage 07 实现）
// 返回 FrameInfo（待设计），始终不校验 CRC
RbfReverseSequence ScanReverse(bool showTombstone = false);

// ======================= 内部 API =======================
internal static int ReadRaw(SafeFileHandle file, SizedPtr ptr, Span<byte> buffer);
internal static AteliaResult<RbfFrame> ValidateAndParse(SizedPtr ptr, ReadOnlySpan<byte> frameBytes);
internal static AteliaResult<(SizedPtr, uint, bool)> ValidateAndParseHeader(SizedPtr ptr, ReadOnlySpan<byte> frameBytes);
```

**状态**：已确认，执行计划见 `stage/05/task.md` 和 `blueprint.md`

---
