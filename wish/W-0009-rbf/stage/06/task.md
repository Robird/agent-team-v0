# Stage 06: 帧格式重构 + ScanReverse 实现

> **目标**：将 RBF 实现升级到 v0.40 帧格式，并实现 ScanReverse 迭代器。
> **前置**：Stage 05 完成（ReadFrame 重构 + Buffer 外置）

---

## 设计决策

### Decision 6.A: Wire Format Breaking Change (v0.40)

**结论**：采用新帧布局，FrameTag 移至尾部，引入 TrailerCodeword。

**变更摘要**：
- 旧：`[HeadLen][Tag][Payload][Status(1-4)][TailLen][CRC]`
- 新：`[HeadLen][Payload][TailMeta][Padding][PayloadCrc][TrailerCodeword(16B)]`

**TrailerCodeword 布局** (固定 16 字节)：
```
[TrailerCrc32C(BE)][FrameDescriptor(LE)][FrameTag(LE)][TailLen(LE)]
```

### Decision 6.B: 双 CRC 机制

**结论**：
- `PayloadCrc32C`：覆盖 Payload + TailMeta + Padding（LE 存储）
- `TrailerCrc32C`：覆盖 FrameDescriptor + FrameTag + TailLen（**BE 存储**，支持逆向 CRC 扫描）

### Decision 6.C: ScanReverse 只校验 TrailerCrc32C

**结论**：`ScanReverse` 只读取 TrailerCodeword (16B)，**只校验 `TrailerCrc32C`**，**不校验 `PayloadCrc32C`**。

**理由**：
1. 逆向扫描的目的是快速迭代帧元信息，不需要读取 Payload
2. TrailerCrc32C 已足够保证尾部元信息完整性
3. 如需完整校验，上层调用 `ReadFrame`（始终校验双 CRC）

**HeadLen vs TailLen 的 SSOT 选择**：
- **ScanReverse**：以 `TailLen` 为长度 SSOT（尾部导向，不读 HeadLen，不做 `HeadLen == TailLen` 交叉校验）
- **ReadFrame**：仍必须验证 `HeadLen == TailLen`（完整 framing 校验）

---

## 实现任务

### Part A: 布局重构 (v0.40 Breaking Change)

#### Task 6.1: 重构 RbfLayout.cs

**执行者**：Implementer
**依赖**：无

**任务简报**：
更新 `atelia/src/Rbf/Internal/RbfLayout.cs` 以适配 v0.40 布局。

**修改清单**：
1. 删除 `TagOffset`、`TagSize` 常量（Tag 不再在头部）
2. 新增 `TrailerCodewordSize = 16`
3. 新增 `PayloadCrcSize = 4`
4. 更新 `MinFrameLength = 24`（= HeadLen(4) + PayloadCrc(4) + TrailerCodeword(16)，参见 @[F-FRAMEBYTES-LAYOUT]）
5. 更新 `FrameLayout` 结构：
   - 删除 `TagOffset`
   - 新增 `PayloadOffset = HeadLenSize` (4)
   - 新增 `PayloadCrcOffset(int payloadLen, int tailMetaLen, int paddingLen)`
   - 新增 `TrailerCodewordOffset(int payloadLen, int tailMetaLen, int paddingLen)`

**验收标准**：
- [ ] 编译通过
- [ ] 常量值与 `rbf-format.md` @[F-FRAMEBYTES-LAYOUT] 对齐
- [ ] `MinFrameLength = 24` 与 SSOT 一致

---

#### Task 6.2: 实现 TrailerCodewordHelper

**执行者**：Implementer
**依赖**：Task 6.1

**任务简报**：
创建 `atelia/src/Rbf/Internal/TrailerCodewordHelper.cs`。

**设计参考**：详见 [design-draft.md §3.2](design-draft.md#32-trailercodeword-解析辅助)

**FrameDescriptor 位布局**：参见 @[F-FRAME-DESCRIPTOR-LAYOUT]（bit31=IsTombstone, bit30-29=PaddingLen, bit28-16=Reserved(MUST=0), bit15-0=TailMetaLen）

**关键 API**：
- `TrailerCodewordData Parse(ReadOnlySpan<byte> buffer)` — 从 16B buffer 解析
- `uint BuildDescriptor(bool isTombstone, int paddingLen, int tailMetaLen)` — 构建 FrameDescriptor
- `void SerializeWithoutCrc(...)` — 序列化（不含 CRC）
- `uint SealTrailerCrc(Span<byte> trailerCodeword)` — 计算并写入 TrailerCrc32C

**验收标准**：
- [ ] 编译通过
- [ ] 单元测试覆盖 Parse/Build/Serialize 各路径
- [ ] CRC 使用 `RollingCrc.SealCodewordBackward` / `CheckCodewordBackward`
- [ ] **端序验证测试**：给定固定字节序列，断言 `TrailerCrc32C` 按 BE 读取、其他字段按 LE 读取
- [ ] **测试向量**：构造已知 (descriptor, tag, tailLen)，验证 `SealTrailerCrc` 写入的前 4 字节（BE）与 `RollingCrc.CheckCodewordBackward` 返回 true

---

#### Task 6.3: 重构 RbfAppendImpl + FrameLayout.FillTrailer

**执行者**：Implementer
**依赖**：Task 6.2

**任务简报**：
重写帧序列化和写入逻辑以适配 v0.40 布局。

**涉及文件**：
- `atelia/src/Rbf/Internal/RbfAppendImpl.cs` — `Append()` 方法、`WriteWithCrc()` 方法
- `atelia/src/Rbf/Internal/RbfLayout.cs` — `FrameLayout.FillTrailer()` 方法

**变更要点**：
1. 删除头部 Tag 写入
2. 计算 PaddingLen：`(4 - ((payloadLen + tailMetaLen) % 4)) % 4`（使 Payload+TailMeta+Padding 4B 对齐）
3. 写入顺序：HeadLen → Payload → TailMeta → Padding → PayloadCrc → TrailerCodeword
4. PayloadCrc 覆盖 Payload + TailMeta + Padding（@[F-CRC32C-COVERAGE]）
5. TrailerCrc 使用 `RollingCrc.SealCodewordBackward`

**验收标准**：
- [ ] 编译通过
- [ ] 写入的帧字节与 `rbf-format.md` @[F-FRAMEBYTES-LAYOUT] 规范一致
- [ ] **TailLen == HeadLen**（构造帧后读取 trailer 与 header 断言相等）
- [ ] TailMeta 参数支持（默认 `ReadOnlySpan<byte>.Empty`）
- [ ] **CRC 覆盖范围验证**：PayloadCrc 不覆盖 HeadLen/Trailer/Fence
- [ ] **端序验证**：PayloadCrc 为 LE，TrailerCrc 为 BE

---

#### Task 6.4: 重构 RbfReadCore + FrameLayout.ResultFromTrailer

**执行者**：Implementer
**依赖**：Task 6.2

**任务简报**：
重写帧解析和校验逻辑以适配 v0.40 布局。

**涉及文件**：
- `atelia/src/Rbf/Internal/RbfReadImpl.cs` — `ValidateAndParse()` 方法
- `atelia/src/Rbf/Internal/RbfLayout.cs` — `FrameLayout.ResultFromTrailer()` 方法

**变更要点**：
1. 删除头部 Tag 读取
2. 从 TrailerCodeword 读取 Tag
3. 校验双 CRC：PayloadCrc + TrailerCrc
4. 从 FrameDescriptor 解码 IsTombstone、PaddingLen、TailMetaLen
5. 验证 FrameDescriptor 保留位为 0

**Framing 校验清单**：
- [ ] HeadLen == TailLen
- [ ] FrameDescriptor 保留位 (bit 28-16) 为 0
- [ ] TailMetaLen <= 65535（16-bit 值域）
- [ ] PaddingLen <= 3（2-bit 值域）
- [ ] PayloadCrc32C 校验通过
- [ ] TrailerCrc32C 校验通过（使用 `RollingCrc.CheckCodewordBackward`）
- [ ] PayloadLength >= 0

**验收标准**：
- [ ] 编译通过
- [ ] 能正确读取 v0.40 格式的帧
- [ ] 返回的 `RbfFrame` 包含正确的 Tag、IsTombstone
- [ ] **双 CRC 都校验**（与 ScanReverse 不同）

---

#### Task 6.5: 修复既有测试

**执行者**：Implementer
**依赖**：Task 6.3, 6.4

**任务简报**：
更新所有既有测试以适配 v0.40 帧格式。

**变更要点**：
1. 更新测试中的帧字节构造（新布局）
2. 删除 `FrameStatusHelper.cs` 或标记为 DEPRECATED
3. 添加 TailMeta 参数（大多数测试传 empty）
4. 验证双 CRC 计算正确

**验收标准**：
- [x] 所有既有测试通过（`dotnet test tests/Rbf.Tests`）
- [x] 无警告
- [x] `FrameStatusHelper` 已删除或标记 DEPRECATED

---

### Part B: ScanReverse 实现

#### Task 6.6: 定义 RbfFrameInfo

**执行者**：Implementer
**依赖**：无

**任务简报**：
创建 `atelia/src/Rbf/RbfFrameInfo.cs`。

**设计参考**：详见 [design-draft.md §3.1](design-draft.md#31-rbfframeinfo帧元信息)

**类型签名**：
```csharp
public readonly record struct RbfFrameInfo(
    SizedPtr Ticket,
    uint Tag,
    int PayloadLength,
    int TailMetaLength,
    bool IsTombstone
);
```

**验收标准**：
- [ ] 编译通过
- [ ] 类型与 `rbf-interface.md` @[A-RBF-FRAME-INFO] 定义一致

---

#### Task 6.7: 实现 ReadTrailerBefore

**执行者**：Implementer
**依赖**：Task 6.2, 6.6

**任务简报**：
在 `RbfReadImpl.cs` 中添加 `ReadTrailerBefore` 方法。

**设计参考**：
- 算法步骤：详见 [design-draft.md §4.2](design-draft.md#42-算法步骤大幅简化)
- 完整伪代码：详见 [design-draft.md §4.3](design-draft.md#43-伪代码)

**算法摘要**（12 步）：
1. 边界检查 → 2. 读 20B (TrailerCodeword+Fence) → 3. 验 Fence → 4. 解析 TrailerCodeword
5. 验 TrailerCrc32C → 6. 验保留位 → 7. 验 TailLen → 8-9. 算 frameStart
10-11. 算 PayloadLength → 12. 构造 RbfFrameInfo

**I/O 特征**（对比旧方案）：
| 版本 | 读取字节 | I/O 次数 |
|------|----------|----------|
| v0.40 | **20B** | **1 次** |
| 旧方案 | 26-29B | 6 次 |

**验收标准**：
- [ ] 编译通过
- [ ] 单次 I/O 读取 20 字节（TrailerCodeword + Fence）
- [ ] 单元测试覆盖正常路径和各类错误路径
- [ ] **TailLen > int.MaxValue 必须失败**（边界检查）
- [ ] **每条 Framing 失败路径对应 1 个测试用例**：
  - Fence 不匹配
  - TrailerCrc32C 校验失败
  - Reserved bits 非零
  - TailLen 未对齐
  - TailLen < MinFrameLength
  - PayloadLength 负数

---

#### Task 6.8: 实现 RbfReverseEnumerator + RbfReverseSequence

**执行者**：Implementer
**依赖**：Task 6.7

**任务简报**：
实现 `atelia/src/Rbf/RbfReverseEnumerator.cs`（文件已存在但为 stub）。

**设计参考**：完整实现代码详见 [design-draft.md §5](design-draft.md#5-rbfreverseenumerator)

**类型签名**（接口契约）：
```csharp
namespace Atelia.Rbf;

/// <summary>逆向扫描序列（支持 foreach）。</summary>
public ref struct RbfReverseSequence {
    public RbfReverseEnumerator GetEnumerator();
}

/// <summary>逆向扫描枚举器。</summary>
public ref struct RbfReverseEnumerator {
    public RbfFrameInfo Current { get; }
    public bool MoveNext();
    public AteliaError? TerminationError { get; }
}
```

**验收标准**：
- [ ] 编译通过
- [ ] 支持 `foreach` 消费
- [ ] Tombstone 过滤正确（`showTombstone` 参数）
- [ ] 损坏时硬停止并设置 `TerminationError`

---

#### Task 6.9: 实现 IRbfFile.ScanReverse

**执行者**：Implementer
**依赖**：Task 6.8

**任务简报**：
在 `RbfFileImpl.cs` 中实现 `ScanReverse` 方法。

**验收标准**：
- [ ] 编译通过
- [ ] 接口签名与 `rbf-interface.md` @[A-RBF-IRBFFILE-SHAPE] 一致

---

#### Task 6.10: 实现便捷重载 ReadFrame(in RbfFrameInfo, ...)

**执行者**：Implementer
**依赖**：Task 6.9

**任务简报**：
在 `IRbfFile` 和 `RbfFileImpl` 中添加便捷重载。

**产出**：
```csharp
public interface IRbfFile {
    AteliaResult<RbfFrame> ReadFrame(in RbfFrameInfo info, Span<byte> buffer);
    AteliaResult<RbfPooledFrame> ReadPooledFrame(in RbfFrameInfo info);
}
```

**验收标准**：
- [ ] 编译通过
- [ ] 重载实现委托到 `ReadFrame(ticket, buffer)`

---

#### Task 6.11: ScanReverse 测试

**执行者**：Implementer
**依赖**：Task 6.10

**任务简报**：
创建 `tests/Rbf.Tests/RbfScanReverseTests.cs`。

**测试用例**：
1. **正常路径**：多帧文件逆向遍历，验证顺序（从尾到头）
2. **空文件**：只有 HeaderFence，返回空序列
3. **Tombstone 过滤**：`showTombstone=false` 时跳过 Tombstone
4. **Tombstone 可见**：`showTombstone=true` 时包含 Tombstone
5. **损坏停止**：TrailerCrc 损坏时硬停止，`TerminationError` 非空，**已产出的前序帧保持正确**
6. **边界值**：单帧、最小帧、最大帧

**验收标准**：
- [ ] 所有测试通过
- [ ] 覆盖各类边界和错误场景
- [ ] **硬停止语义验证**：损坏帧之前的帧仍可正确产出（不 Resync）

---

## 规范引用

| 条款 | 文档 | 要点 |
|------|------|------|
| @[F-FRAMEBYTES-LAYOUT] | rbf-format.md | FrameBytes 布局（v0.40） |
| @[F-FRAME-DESCRIPTOR-LAYOUT] | rbf-format.md | FrameDescriptor 位布局 |
| @[F-TRAILER-CRC-BIG-ENDIAN] | rbf-format.md | TrailerCrc32C BE 存储 |
| @[F-TRAILERCRC-COVERAGE] | rbf-format.md | TrailerCrc32C 覆盖范围 |
| @[F-CRC32C-COVERAGE] | rbf-format.md | PayloadCrc32C 覆盖范围 |
| @[F-PADDING-CALCULATION] | rbf-format.md | Padding 长度计算公式 |
| @[S-RBF-PAYLOADLENGTH-FORMULA] | **rbf-derived-notes.md** | PayloadLength 计算公式与上限 |
| @[R-REVERSE-SCAN-USES-TRAILER-CRC] | rbf-format.md | ScanReverse 使用 TrailerCrc |
| @[S-RBF-SCANREVERSE-NO-PAYLOADCRC] | rbf-interface.md | ScanReverse 不校验 PayloadCrc |
| @[A-RBF-FRAME-INFO] | rbf-interface.md | RbfFrameInfo 定义 |

---

## 相关文档

| 文档 | 职责 |
|:-----|:-----|
| **本文档（task.md）** | 执行蓝图：任务分解、验收标准、规范引用 |
| [design-draft.md](design-draft.md) | 设计参考：帧布局图、类型定义、算法伪代码 |
| [Stage 05](../05/task.md) | 前置 Stage |
| Stage 07 | 后续 Stage（BeginAppend/EndAppend）|

> **引用规范**：task.md 中的类型定义和算法通过语义锚点引用 design-draft.md，避免双写。

---
