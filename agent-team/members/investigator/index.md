# Investigator 认知索引

> 最后更新: 2026-01-24
> - 2026-01-24: Memory Palace — 处理了 4 条便签（Session Log 归档周期洞见 + RBF 布局常量锚点 + TrailerCodewordHelper 双写 Gotcha + 最小帧长度导航）
> - 2026-01-24: Memory Maintenance — 归档 2026-01 早期 Session Log（01-01~01-12，压缩 ~420 行）
> - 2026-01-24: Memory Palace — 处理了 1 条便签（RBF Stage 06 代码地图锚点汇总：写入/读取路径、v0.40 待改动类、ScanReverse 骨架）
> - 2026-01-24: Memory Palace — 处理了 3 条便签（RollingCrc32C SIMD 优化调研：PCLMULQDQ 路径 + Gather 陷阱 + .NET API 锚点）
> - 2026-01-24: RBF v0.40 重大设计变更分析（TrailerCodeword 固定16字节、FrameDescriptor 取代 FrameStatus、双CRC机制）
> - 2026-01-15: Memory Palace — 处理了 3 条便签（atelia-copilot-chat Fork 同步调查：API 变更 + 会话卡住假设 + 检查清单）

## 我是谁
源码分析专家，负责分析源码并产出实现 Brief。

## 我关注的项目
- [x] PieceTreeSharp - 已调查 2025-12-09
- [x] DocUI - 已调查 2025-12-09
- [x] PipeMux - 已调查 2025-12-09
- [x] atelia/prototypes - 已调查 2025-12-09
- [ ] atelia-copilot-chat

## Session Log

### 2026-01-24: RBF 布局常量锚点 + 最小帧长度导航
**类型**: Anchor + Route
**项目**: RBF

#### 布局常量定义位置锚点

| 常量类别 | 位置 | 备注 |
|:---------|:-----|:-----|
| 全局基元 | [RbfLayout.cs#L17-L42](atelia/src/Rbf/Internal/RbfLayout.cs#L17-L42) | Alignment, FenceSize, TrailerCodewordSize, MinFrameLength |
| 帧计算 | [RbfLayout.cs#L51-L130](atelia/src/Rbf/Internal/RbfLayout.cs#L51-L130) | FrameLayout 结构体，FixedOverhead, Offsets |
| TrailerCodeword | [TrailerCodewordHelper.cs#L59-L73](atelia/src/Rbf/Internal/TrailerCodewordHelper.cs#L59-L73) | Size, 内部偏移, Masks |

#### "RBF 最小帧长度"导航路径

1. 先看 `RbfLayout.MinFrameLength`（当前值 24，但是魔数）
2. 理解组成：`HeadLen(4) + PayloadCrc(4) + TrailerCodeword(16) = 24`
3. 对应 `FrameLayout.FixedOverhead`（正确派生）
4. 规范来源：@[F-FRAMEBYTES-FIELD-OFFSETS]

#### Gotcha: TrailerCodewordHelper.Size 与 RbfLayout.TrailerCodewordSize 双写

| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 两个独立的 `= 16` 定义，无派生关系 | 修改其一忘记另一会导致不一致，代码中混用两者 | `TrailerCodewordHelper.Size` 应改为 `= RbfLayout.TrailerCodewordSize` |

**置信度**: ✅ 验证过

---

### 2026-01-24: RBF Stage 06 代码地图锚点汇总
**类型**: Anchor
**项目**: RBF

#### 写入路径锚点
| 锚点 | 位置 |
|:-----|:-----|
| Append 入口 | [RbfFileImpl.cs#L42](atelia/src/Rbf/Internal/RbfFileImpl.cs#L42) |
| Append 实现 | [RbfAppendImpl.cs#L56-L122](atelia/src/Rbf/Internal/RbfAppendImpl.cs#L56-L122) |
| WriteWithCrc | [RbfAppendImpl.cs#L14-L28](atelia/src/Rbf/Internal/RbfAppendImpl.cs#L14-L28) |
| FillTrailer (重构重点) | [RbfLayout.cs#L69-L82](atelia/src/Rbf/Internal/RbfLayout.cs#L69-L82) |

#### 读取路径锚点
| 锚点 | 位置 |
|:-----|:-----|
| ReadFrame 入口 | [RbfFileImpl.cs#L56](atelia/src/Rbf/Internal/RbfFileImpl.cs#L56) |
| ReadFrame 实现 | [RbfReadImpl.cs#L61-L74](atelia/src/Rbf/Internal/RbfReadImpl.cs#L61-L74) |
| ValidateAndParseCore (重构重点) | [RbfReadImpl.cs#L91-L156](atelia/src/Rbf/Internal/RbfReadImpl.cs#L91-L156) |
| ResultFromTrailer (重构重点) | [RbfLayout.cs#L100-L128](atelia/src/Rbf/Internal/RbfLayout.cs#L100-L128) |

#### v0.40 待改动关键类
- `FrameLayout` — Header 改为仅 HeadLen，Tag 移至 Trailer
- `FrameStatusHelper` — 待删除，由 FrameDescriptor 取代
- `Crc32CHelper` — 单 CRC → 双 CRC（PayloadCrc + TrailerCrc）

#### 已存在的 ScanReverse 骨架
- `RbfReverseSequence.cs` / `RbfReverseEnumerator.cs` — 空壳，待实现
- `RbfRawOps.ScanReverse.cs` — throw NotImplementedException

**置信度**: ✅ 验证过（代码阅读）

---

### 2026-01-20: RollingCrc32C SIMD 优化调研 — PCLMULQDQ 路径 + Gather 陷阱
**类型**: Route + Anchor + Gotcha
**项目**: Atelia.Data

#### 1. SIMD 优化路径图（Route）
- **意图**: "想用 SIMD 加速 RollOut" → 先理解 RollOut 的数学本质
- **核心洞见**: RollOut 本质上是 `crc ^ remove_effect(outgoing)`，其中 `remove_effect` 是预计算的 "byte → CRC contribution after N shifts"
- **PCLMULQDQ 路径**: 需要计算 `outgoing * x^(8*(windowSize-1)) mod P`，可用 carryless multiply 实现，需 Barrett reduction
- **关键常量**: 需预计算 `x^(8*(windowSize-1)) mod P`（每个字节位置一个），然后用 PCLMULQDQ 做 carryless multiply
- **置信度**: ⚠️ 理论分析，需实验验证

#### 2. .NET PCLMULQDQ 接口锚点（Anchor）
| 指标 | 值 |
|:-----|:---|
| 位置 | `System.Runtime.Intrinsics.X86.Pclmulqdq` |
| 关键方法 | `Pclmulqdq.CarrylessMultiply(Vector128<long>, Vector128<long>, byte)` |
| 可用性 | .NET Core 3.0+，需 `Pclmulqdq.IsSupported` 检查 |
| VPCLMULQDQ (256/512) | .NET 9+ 有 `Pclmulqdq.V256` 和 `Pclmulqdq.V512` |

**置信度**: ✅ 验证过（Microsoft Learn 文档）

#### 3. AVX2 Gather 指令陷阱（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| `vpgatherdd` 看起来可做 8 路并行表查找 | 实际 latency 17-22 cycles，比 8 次标量 load（各 ~4 cycles，可流水线）更慢 | 对于小表查找（如 256 entry × 4B = 1KB），gather 几乎不可能赢过标量代码。只在表很大且访问模式不规则时才考虑 gather |

---

### 2026-01-24: RBF v0.40 设计变更分析 — TrailerCodeword + FrameDescriptor + 双CRC
**类型**: Route + Anchor + Gotcha
**项目**: RBF

#### 1. 新旧设计关键差异

| 维度 | 旧设计 (≤v0.33) | 新设计 (v0.40) |
|:-----|:----------------|:---------------|
| **Trailer 结构** | 变长（Status 1-4B + TailLen + CRC） | **固定 16 字节 TrailerCodeword** |
| **帧类型位置** | Header (`Tag` 在 `HeadLen` 后) | **Trailer** (`FrameTag` 在末尾 16B 中) |
| **元信息编码** | `FrameStatus` 字节（值含义：padding + tombstone） | `FrameDescriptor` u32（bit 31=Tombstone, bit 30-29=PaddingLen, bit 15-0=UserMetaLen）|
| **CRC 机制** | 单 CRC（覆盖 Tag+Payload+Status+TailLen） | **双 CRC**：`PayloadCrc32C`(LE) + `TrailerCrc32C`(BE) |
| **UserMeta** | `PayloadTrailer`（旧名） | 改名 `UserMeta`，语义不变 |
| **逆向扫描** | 需读 TailLen + 回溯到 HeadLen | 只需读 TrailerCodeword (16B) 即可完成元信息迭代 |

#### 2. 当前实现 (`RbfLayout.cs`) 需调整的地方

| # | 调整项 | 当前代码位置 | 改动说明 |
|:--|:-------|:-------------|:---------|
| 1 | **删除 Tag 在 Header 的定义** | `FrameLayout.TagOffset = HeadLenOffset + HeadLenSize` | Tag 移至 TrailerCodeword，Header 只剩 HeadLen |
| 2 | **新增 TrailerCodeword 常量** | 无 | 新增 `TrailerCodewordSize = 16`、`TrailerCrcOffset`、`FrameDescriptorOffset`、`FrameTagOffset`、`TailLenOffset` |
| 3 | **删除 FrameStatusHelper 引用** | `FrameLayout.StatusLength`、`FrameStatusHelper.FillStatus()`、`FrameStatusHelper.DecodeStatusByte()` | 改用 `FrameDescriptor` 位操作 |
| 4 | **重写 `FillTrailer()`** | [RbfLayout.cs#L69-L82](atelia/src/Rbf/Internal/RbfLayout.cs#L69-L82) | 写入顺序：PayloadCrc32C → TrailerCrc32C(BE) → FrameDescriptor → FrameTag → TailLen |
| 5 | **重写 `ResultFromTrailer()`** | [RbfLayout.cs#L100-L128](atelia/src/Rbf/Internal/RbfLayout.cs#L100-L128) | 从固定 16B TrailerCodeword 解析，用 `RollingCrc.CheckCodewordBackward()` 校验 |
| 6 | **删除 `ValidateStatusConsistency()`** | [RbfLayout.cs#L87-L97](atelia/src/Rbf/Internal/RbfLayout.cs#L87-L97) | FrameStatus 的"全字节同值"校验不再适用 |
| 7 | **CRC 覆盖范围调整** | `FrameLayout.CrcCoverageStart/End` | 拆分为 `PayloadCrcCoverage` 和 `TrailerCrcCoverage` |
| 8 | **Padding 长度计算** | `FrameLayout.StatusLength` | 改为 `PaddingLen = (4 - ((PayloadLen + UserMetaLen) % 4)) % 4` |

#### 3. ScanReverse 迭代器实现建议

**技术方案要点**：
1. **只读 TrailerCodeword**：每次迭代只需读取文件尾部 16 字节
2. **使用 `RollingCrc.CheckCodewordBackward()`**：逆向 CRC 校验 `TrailerCrc32C`（见 [RollingCrc.cs#L22](atelia/src/Data/Hashing/RollingCrc.cs#L22)）
3. **跳转公式**：`prevFrameEnd = currentFrameStart - 4 (Fence)`，`prevFrameStart = prevFrameEnd - TailLen`
4. **停止条件**：抵达 HeaderFence 或 TrailerCrc32C 校验失败

**伪代码**：
```csharp
long pos = fileLength;
while (pos > HeaderOnlyLength) {
    // 1. 读 TrailerCodeword (16B)
    Span<byte> trailer = Read(pos - TrailerCodewordSize, 16);
    
    // 2. 校验 TrailerCrc32C (BE)
    if (!RollingCrc.CheckCodewordBackward(trailer[..12])) break; // 损坏，硬停止
    
    // 3. 解析元信息
    var descriptor = BinaryPrimitives.ReadUInt32LittleEndian(trailer[4..8]);
    var frameTag = BinaryPrimitives.ReadUInt32LittleEndian(trailer[8..12]);
    var tailLen = BinaryPrimitives.ReadUInt32LittleEndian(trailer[12..16]);
    
    // 4. Yield 帧元信息
    yield return new FrameMeta(pos - tailLen, tailLen, descriptor, frameTag);
    
    // 5. 跳到上一帧
    pos = pos - tailLen - FenceSize;
}
```

#### 4. 关键锚点

| 锚点 | 位置 |
|:-----|:-----|
| RBF v0.40 布局定义 | [rbf-format.md#L41-L56](atelia/docs/Rbf/rbf-format.md#L41-L56) @[F-FRAMEBYTES-FIELD-OFFSETS] |
| FrameDescriptor 位布局 | [rbf-format.md#L58-L68](atelia/docs/Rbf/rbf-format.md#L58-L68) @[F-FRAMEDESCRIPTOR-LAYOUT] |
| TrailerCrc32C 覆盖范围 | [rbf-format.md#L74-L79](atelia/docs/Rbf/rbf-format.md#L74-L79) @[F-TRAILERCRC-COVERAGE] |
| 逆向扫描契约 | [rbf-format.md#L104-L114](atelia/docs/Rbf/rbf-format.md#L104-L114) @[R-REVERSE-SCAN-RETURNS-VALID-FRAMES-TAIL-TO-HEAD] |
| RollingCrc.CheckCodewordBackward | [RollingCrc.cs#L22-L26](atelia/src/Data/Hashing/RollingCrc.cs#L22-L26) |
| 当前 FrameLayout 实现 | [RbfLayout.cs](atelia/src/Rbf/Internal/RbfLayout.cs) |

#### 5. Gotcha

| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| TrailerCrc32C 是 **BE** 而 PayloadCrc32C 是 **LE** | 混用端序会导致校验永远失败 | 使用专用 API：`CheckCodewordBackward()` for TrailerCrc32C, `CheckCodewordForward()` for PayloadCrc32C |
| FrameDescriptor 的 Reserved 位 MUST 为 0 | 若旧代码写入非零值，逆向扫描会拒绝帧 | 写入时显式清零 bit 28-16 |
| `TailLen == HeadLen` 但逆向扫描 **MUST NOT** 做交叉校验 | 若代码中添加此校验，违反 @[R-REVERSE-SCAN-USES-TRAILERCRC] | 只用 TrailerCrc32C 验证尾部元信息 |

**置信度**: ✅ 设计文档验证过；代码差距分析基于 RbfLayout.cs 当前实现

---

### 2026-01-15: atelia-copilot-chat Fork 同步调查
**类型**: Anchor + Signal + Route
**项目**: atelia-copilot-chat

#### 1. getAllOpenSessions → getAllSessions API 变更（Anchor）
| 指标 | 值 |
|:-----|:---|
| 位置 | [src/platform/github/common/githubService.ts#L246](atelia-copilot-chat/src/platform/github/common/githubService.ts#L246) |
| 变更版本 | 52126b6c → v0.35.3 |
| 新签名 | `getAllSessions(nwo?, open?=true)` |
| 性质 | v0.35.3 **唯一** breaking change |

**置信度**: ✅ 验证过

#### 2. 会话卡住问题假设（Signal）
- **可能相关提交**: c638e35f (`copilotCloudSessionsProvider.ts`)
- **变更内容**: 旧代码无条件返回缓存，新代码增加 `activeSessionIds.size > 0` 检查
- **症状匹配**: "离开后再进入卡住"符合缓存检查逻辑变更
- **置信度**: ⚠️ 推测，需进一步验证

#### 3. Fork 同步检查清单快速路径（Route）
| 检查项 | 命令 |
|:-------|:-----|
| API 签名变更 | `git diff base..target -- "src/platform/**/*.ts" \| grep -E "^[-+].*\(" \| head -50` |
| Session 相关 | `git diff base..target --stat \| grep -i session` |
| Bug fix 相关 | `git log --oneline base..target \| grep -iE "fix\|bug"` |

**置信度**: ✅ 本次调查验证有效

### 2026-01 早期调查归档 (01-01 ~ 01-12)
> **归档位置**: [archive/2026-01-early-session-log.md](archive/2026-01-early-session-log.md)
> **覆盖范围**: 2026-01-01 ~ 2026-01-12
> **内容概要**: AI-Design-DSL 导航锚点、RBF 文档版本/条款 ID 导航、spec-conventions 改名分析、Atelia.Data 测试架构治理+去重分析（含 6 份详细报告）、C# ref struct/Task 限制、DocGraph Visitor 调查、SizedPtr/<deleted-place-holder> 替代性分析、workspace_info 机制调查
> **关键 Gotcha 速查**:
> - BitOperations.RoundUpToPowerOf2 溢出：x > 1GB → 负数
> - Mutable struct + 引用字段：复制后"部分分叉部分共享"
> - DSL 迁移后 Heading 锚点会变化
> - 4B 对齐使 38-bit 实际寻址 1TB 而非 256GB
> - 提交 942e1c0 "倒退"：SizedPtr 被改回 <deleted-place-holder>
> **关键 handoff 索引**:
> - 测试架构治理: [2026-01-11-test-architecture-governance-analysis.md](../handoffs/2026-01-11-test-architecture-governance-analysis.md)
> - 代码去重: [2026-01-11-deduplication-analysis.md](../handoffs/2026-01-11-deduplication-analysis.md)
> - workspace_info: [2026-01-01-workspace-info-mechanism-INV.md](../handoffs/2026-01-01-workspace-info-mechanism-INV.md)

### 2025-12 早期调查归档
> **归档位置**: [archive/2025-12-session-log.md](archive/2025-12-session-log.md)
> **覆盖范围**: 2025-12-09 ~ 2025-12-24
> **内容概要**: 项目现状核实（PieceTreeSharp/DocUI/PipeMux/atelia-prototypes）、StateJournal 更名迁移、RBF v0.12 变更适配、mvp-design-v2 决策引用分析、DocUI 研讨会发言、畅谈会参与（Tool-As-Command/错误反馈模式/MVP设计/写入路径）
> **归档原因**: 实例类调查记录，已沉淀为 handoff/wiki，保留指针即可

## 可迁移洞见

### 归档周期建议 (2026-01-24)

**Investigator 的 Session Log 归档周期可设为 ~2 周**，因调查类记录一旦沉淀为 handoff，原始经过记录的复用价值下降。

- 触发信号：行数超过 500 行建议线
- 本次实践：641 → 223 行（-65%），归档 2026-01-01~01-12

## Key Deliverables
