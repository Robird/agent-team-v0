# Stage 06: 帧格式重构 + ScanReverse 设计草案

> **主笔**：TeamLeader  
> **审阅**：Implementer, Craftsman  
> **状态**：第四轮（v0.40 帧格式重构后修订）

> **重要变更（2026-01-24）**：本文档已根据 RBF v0.40 Wire Format Breaking Change 全面修订：
> - TrailerCodeword（固定 16 字节）取代旧的变长 Trailer（Status + TailLen + CRC）
> - FrameTag 从头部移至尾部（在 TrailerCodeword 中）
> - FrameDescriptor (u32) 取代 FrameStatus 字节（统一管理 IsTombstone/PaddingLen/TailMetaLen）
> - 双 CRC 机制：PayloadCrc32C (LE) + TrailerCrc32C (BE)
> - TailMeta 取代 PayloadTrailer

---

## 0. 设计演进

### 第三轮 → 第四轮（v0.40 帧格式重构）
- **核心变更**：帧布局从 `[HeadLen][Tag][Payload][Status][TailLen][CRC]` 改为 `[HeadLen][Payload][TailMeta][Padding][PayloadCrc][TrailerCodeword]`
- **ScanReverse 大幅简化**：只需读取 TrailerCodeword (16B) + Fence (4B) 即可迭代
- **Stage 06 扩展**：除 ScanReverse 外，还需先重构现有 RbfLayout 实现

---

## 1. 新帧布局（v0.40）

```
文件布局：[Fence][Frame1][Fence][Frame2][Fence]...[FrameN][Fence]
            4B              4B              4B            4B

单帧布局（FrameBytes，不含两侧 Fence）：

PayloadCodeword:
┌─────────┬─────────┬──────────┬─────────┬───────────────┐
│ HeadLen │ Payload │ TailMeta │ Padding │ PayloadCrc32C │
│  4B LE  │   N B   │   M B    │  0-3 B  │    4B LE      │
└─────────┴─────────┴──────────┴─────────┴───────────────┘

TrailerCodeword (固定 16 字节):
┌───────────────┬─────────────────┬──────────┬─────────┐
│ TrailerCrc32C │ FrameDescriptor │ FrameTag │ TailLen │
│   4B **BE**   │     4B LE       │  4B LE   │  4B LE  │
└───────────────┴─────────────────┴──────────┴─────────┘
```

**FrameDescriptor 位布局** (u32 LE)：
| Bit | 字段 | 说明 |
|-----|------|------|
| 31 | IsTombstone | 1=墓碑帧 |
| 30-29 | PaddingLen | 0-3 |
| 28-16 | Reserved | MUST 为 0 |
| 15-0 | TailMetaLen | 0-65535 |

**双 CRC 机制**：
- `PayloadCrc32C = CRC32C(Payload + TailMeta + Padding)` — **LE** 存储
- `TrailerCrc32C = CRC32C(FrameDescriptor + FrameTag + TailLen)` — **BE** 存储

---

## 2. 核心设计约束

| 约束 | 说明 |
|:-----|:-----|
| **返回 RbfFrameInfo** | `{Ticket, Tag, PayloadLength, TailMetaLength, IsTombstone}` |
| **CRC 职责分离** | ScanReverse 只校验 `TrailerCrc32C`；ReadFrame 校验双 CRC（`PayloadCrc32C` + `TrailerCrc32C`） |
| **尾部导向** | ScanReverse 只读 TrailerCodeword (16B)，不读 HeadLen，不做 `HeadLen == TailLen` 交叉校验 |
| **MVP 硬停止** | 遇到损坏时停止迭代，通过 `TerminationError` 报告 |

**HeadLen vs TailLen 的 SSOT 选择**：
- **ScanReverse**：以 `TailLen` 为长度 SSOT（尾部导向，不读 HeadLen）
- **ReadFrame**：仍必须验证 `HeadLen == TailLen`（完整 framing 校验）

---

## 3. 新增/修改类型

### 3.1 RbfFrameInfo（帧元信息）

```csharp
/// <summary>帧元信息（不含 Payload）。</summary>
/// <remarks>
/// 用于 ScanReverse 产出，支持不读取 payload 的元信息迭代。
/// PayloadLength 与 TailMetaLength 从 TrailerCodeword 解码得出。
/// </remarks>
public readonly record struct RbfFrameInfo(
    SizedPtr Ticket,
    uint Tag,
    int PayloadLength,
    int TailMetaLength,
    bool IsTombstone
);
```

### 3.2 TrailerCodeword 解析辅助

```csharp
/// <summary>TrailerCodeword 解析结果（值类型，统一解码一次）。</summary>
/// <remarks>
/// 调用 <c>Parse()</c> 一次即可获得所有字段，无需再调用 DecodeDescriptor。
/// FrameDescriptor 的位字段已展开为只读属性，避免重复解码。
/// </remarks>
internal readonly struct TrailerCodewordData {
    public uint TrailerCrc32C { get; init; }  // 已从 BE 解码
    public uint FrameDescriptor { get; init; }
    public uint FrameTag { get; init; }
    public uint TailLen { get; init; }
    
    // 从 FrameDescriptor 解码的字段（一次解码，多次使用）
    public bool IsTombstone => (FrameDescriptor >> 31) != 0;
    public int PaddingLen => (int)((FrameDescriptor >> 29) & 0x3);
    public int TailMetaLen => (int)(FrameDescriptor & 0xFFFF);
}

internal static class TrailerCodewordHelper {
    public const int Size = 16;  // 固定 16 字节
    
    /// <summary>从完整 16 字节 buffer 解析 TrailerCodeword。</summary>
    /// <param name="buffer">MUST 为完整 16 字节 TrailerCodeword</param>
    /// <returns>解析后的结构体（字段已从各自端序解码，FrameDescriptor 已展开为属性）</returns>
    public static TrailerCodewordData Parse(ReadOnlySpan<byte> buffer);
    
    /// <summary>构建 FrameDescriptor。</summary>
    public static uint BuildDescriptor(bool isTombstone, int paddingLen, int tailMetaLen);
    
    /// <summary>序列化 TrailerCodeword（不含 CRC，CRC 由 SealTrailerCrc 写入）。</summary>
    public static void SerializeWithoutCrc(Span<byte> buffer, uint descriptor, uint tag, uint tailLen);
    
    /// <summary>计算并写入 TrailerCrc32C（使用 RollingCrc.SealCodewordBackward）。</summary>
    public static uint SealTrailerCrc(Span<byte> trailerCodeword);
}
```

**设计说明**：
- `TrailerCodewordHelper` 只负责字节编解码 + Seal/Check trailer CRC
- Payload CRC 的计算/写入由 `RbfWriteImpl` 负责（职责分离）

### 3.3 接口层（IRbfFile）

```csharp
public interface IRbfFile : IDisposable {
    // 已有方法...
    
    /// <summary>逆向扫描，返回帧元信息序列。</summary>
    /// <param name="showTombstone">是否包含墓碑帧。默认 false。</param>
    /// <remarks>
    /// CRC 职责分离：ScanReverse 只校验 TrailerCrc32C，不校验 PayloadCrc32C。
    /// 如需完整校验，请对返回的 Ticket 调用 ReadFrame/ReadPooledFrame。
    /// </remarks>
    RbfReverseSequence ScanReverse(bool showTombstone = false);
}
```

---

## 4. 核心算法：ReadTrailerBefore

### 4.1 设计思路

**核心洞察**：v0.40 的 TrailerCodeword 固定 16 字节，ScanReverse 只需读取这 16 字节 + 验证 Fence 即可。

```csharp
/// <summary>读取指定位置之前的帧元信息（只读 TrailerCodeword）。</summary>
/// <param name="file">RBF 文件句柄</param>
/// <param name="fenceEndOffset">帧尾 Fence 的 EndOffsetExclusive</param>
/// <returns>成功时返回 RbfFrameInfo，失败时返回错误</returns>
internal static AteliaResult<RbfFrameInfo> ReadTrailerBefore(
    SafeFileHandle file,
    long fenceEndOffset);
```

### 4.2 算法步骤（大幅简化）

**术语约定**（与 SSOT 对齐）：
- `FenceSize` = 4B（@[F-FENCE-RBF1-ASCII-4B]）
- `TrailerCodewordSize` = 16B
- `MinFrameLength` = 24B = HeadLen(4) + PayloadCrc(4) + TrailerCodeword(16)（@[F-FRAMEBYTES-LAYOUT]）
- `MaxFrameLength` = `SizedPtr.MaxLength`（`atelia/src/Data/SizedPtr.cs`常量，内部编码限制）
- `HeaderOnlyLength` = `FenceSize`（文件开头的 Fence）

```
输入：fenceEndOffset（帧尾 Fence 的 EndOffsetExclusive）

1. 边界检查：fenceEndOffset >= FenceSize + MinFrameLength + FenceSize
2. 读取 fenceEndOffset - FenceSize - TrailerCodewordSize 位置的 (TrailerCodeword + Fence) = 20 字节
3. 验证末尾 4 字节是 Fence ("RBF1")
4. 解析 TrailerCodeword (16B)：TrailerCrc32C(BE), FrameDescriptor(LE), FrameTag(LE), TailLen(LE)
5. 使用 RollingCrc.CheckCodewordBackward 验证 TrailerCrc32C
6. 验证 FrameDescriptor 保留位 (bit 28-16) 为 0
7. 验证 TailLen：
   - TailLen >= MinFrameLength
   - TailLen <= int.MaxValue（SizedPtr.Length 可表示）
   - TailLen % 4 == 0（4B 对齐）
8. 计算 frameStart = fenceEndOffset - FenceSize - TailLen
9. 验证 frameStart >= FenceSize（帧起始不能越过 HeaderFence）
10. 计算 PayloadLength = TailLen - FixedOverhead - TailMetaLen - PaddingLen
    （FixedOverhead = 24 = HeadLen(4) + PayloadCrc(4) + TrailerCodeword(16)）
11. 验证 PayloadLength >= 0
12. 构造 RbfFrameInfo { Ticket = (frameStart, TailLen), Tag, PayloadLength, TailMetaLength, IsTombstone }
```

**注意**：Step 7 中 `TailLen <= int.MaxValue` 是关键边界检查，防止 `u32 → int` 溢出。

### 4.3 伪代码

```csharp
internal static AteliaResult<RbfFrameInfo> ReadTrailerBefore(
    SafeFileHandle file,
    long fenceEndOffset)
{
    const int TrailerAndFenceSize = TrailerCodewordHelper.Size + RbfLayout.FenceSize;  // 20B
    
    // 1. 边界检查
    long minOffset = RbfLayout.HeaderOnlyLength + RbfLayout.MinFrameLength + RbfLayout.FenceSize;
    if (fenceEndOffset < minOffset) {
        return AteliaResult<RbfFrameInfo>.Failure(
            new RbfFramingError("No frame before this offset"));
    }
    
    // 2. 一次读取 TrailerCodeword + Fence (20B)
    Span<byte> buffer = stackalloc byte[TrailerAndFenceSize];
    long readOffset = fenceEndOffset - TrailerAndFenceSize;
    int bytesRead = RandomAccess.Read(file, buffer, readOffset);
    if (bytesRead < TrailerAndFenceSize) {
        return AteliaResult<RbfFrameInfo>.Failure(
            new RbfFramingError("Short read while reading TrailerCodeword+Fence"));
    }
    
    // 3. 验证 Fence
    if (!buffer[^RbfLayout.FenceSize..].SequenceEqual(RbfLayout.Fence)) {
        return AteliaResult<RbfFrameInfo>.Failure(
            new RbfFramingError("Expected Fence not found"));
    }
    
    // 4. 解析 TrailerCodeword
    var trailerSpan = buffer[..TrailerCodewordHelper.Size];
    var trailer = TrailerCodewordHelper.Parse(trailerSpan);
    
    // 5. 验证 TrailerCrc32C（使用 RollingCrc）
    if (!RollingCrc.CheckCodewordBackward(trailerSpan)) {
        return AteliaResult<RbfFrameInfo>.Failure(
            new RbfFramingError("TrailerCrc32C verification failed"));
    }
    
    // 6. 验证 FrameDescriptor 保留位
    if ((trailer.FrameDescriptor & 0x1FFF_0000) != 0) {
        return AteliaResult<RbfFrameInfo>.Failure(
            new RbfFramingError("FrameDescriptor reserved bits are not zero"));
    }
    
    // 7. 验证 TailLen（包含 int 可表示性检查）
    if (trailer.TailLen < RbfLayout.MinFrameLength || 
        trailer.TailLen > int.MaxValue ||  // 防止 u32 → int 溢出
        (trailer.TailLen & RbfLayout.AlignmentMask) != 0) {
        return AteliaResult<RbfFrameInfo>.Failure(
            new RbfFramingError($"Invalid TailLen: {trailer.TailLen}"));
    }
    
    // 8-9. 计算并验证 frameStart
    long frameStart = fenceEndOffset - RbfLayout.FenceSize - trailer.TailLen;
    if (frameStart < RbfLayout.HeaderOnlyLength) {
        return AteliaResult<RbfFrameInfo>.Failure(
            new RbfFramingError("Frame extends before HeaderFence"));
    }
    
    // 10-11. 计算 PayloadLength
    const int FixedOverhead = 4 + 4 + TrailerCodewordHelper.Size;  // HeadLen + PayloadCrc + Trailer
    int payloadLen = (int)trailer.TailLen - FixedOverhead - trailer.TailMetaLen - trailer.PaddingLen;
    if (payloadLen < 0) {
        return AteliaResult<RbfFrameInfo>.Failure(
            new RbfFramingError($"Computed PayloadLength is negative: {payloadLen}"));
    }
    
    // 12. 构造结果
    var ticket = SizedPtr.Create(frameStart, (int)trailer.TailLen);
    return AteliaResult<RbfFrameInfo>.Success(
        new RbfFrameInfo(ticket, trailer.FrameTag, payloadLen, trailer.TailMetaLen, trailer.IsTombstone));
}
```

### 4.4 I/O 特征

| 读取项 | 字节数 | 说明 |
|:-------|:-------|:-----|
| TrailerCodeword + Fence | 20B | **单次读取**，替代旧方案的 6 次读取 |

**对比旧方案**：从 26-29B / 6 次读取 → **20B / 1 次读取**

---

## 5. RbfReverseEnumerator

```csharp
public ref struct RbfReverseEnumerator {
    private readonly SafeFileHandle _handle;
    private readonly bool _showTombstone;
    private long _dataTail;
    private RbfFrameInfo _current;
    private AteliaError? _terminationError;
    
    public RbfFrameInfo Current => _current;
    public AteliaError? TerminationError => _terminationError;
    
    public bool MoveNext() {
        long minValidOffset = RbfLayout.HeaderOnlyLength
                            + RbfLayout.MinFrameLength
                            + RbfLayout.FenceSize;
        
        while (_dataTail >= minValidOffset) {
            var result = RbfReadImpl.ReadTrailerBefore(_handle, _dataTail);
            
            if (result.IsFailure) {
                _terminationError = result.Error;
                return false;
            }
            
            _current = result.Value;
            _dataTail = _current.Ticket.Offset;  // 下一次从当前帧起始位置继续
            
            if (!_showTombstone && _current.IsTombstone) {
                continue;
            }
            
            return true;
        }
        return false;
    }
}
```

---

## 6. 文档职责说明

> **本文档（design-draft.md）**：设计参考，提供帧布局图、类型定义、算法伪代码。
> **task.md**：执行蓝图，提供任务分解、验收标准、规范引用。
>
> 两文档通过**语义锚点**相互引用，避免双写。

---

## 7. 变更日志

| 版本 | 日期 | 变更 |
|------|------|------|
| 第四轮 | 2026-01-24 | 全面重写以适配 RBF v0.40：新帧布局、TrailerCodeword、双 CRC、ScanReverse 简化；合并布局重构任务 |
| 第三轮 | - | 监护人简化指导后修订 |
| 第二轮 | - | Implementer/Craftsman 审阅补充 |
| 第一轮 | - | 初始设计 |
