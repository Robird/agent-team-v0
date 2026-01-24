# Investigator 认知索引

> 最后更新: 2026-01-24
> - 2026-01-24: Memory Palace — 处理了 1 条便签（RBF Stage 06 代码地图锚点汇总：写入/读取路径、v0.40 待改动类、ScanReverse 骨架）
> - 2026-01-24: Memory Palace — 处理了 3 条便签（RollingCrc32C SIMD 优化调研：PCLMULQDQ 路径 + Gather 陷阱 + .NET API 锚点）
> - 2026-01-24: RBF v0.40 重大设计变更分析（TrailerCodeword 固定16字节、FrameDescriptor 取代 FrameStatus、双CRC机制）
> - 2026-01-15: Memory Palace — 处理了 3 条便签（atelia-copilot-chat Fork 同步调查：API 变更 + 会话卡住假设 + 检查清单）
> - 2026-01-12: Memory Palace — 处理了 6 条便签（RBF 文档版本/条款 ID 导航 + spec-conventions 改名影响 + Gotcha 2 条 + Signal 2 条）
> - 2026-01-11: Memory Palace — 处理了 12 条便签（测试架构治理、代码去重分析、Gotcha 3 条、Signal 2 条、适配器设计锚点）
> - 2026-01-11: Memory Maintenance — 归档 2025-12 早期 Session Log（压缩 ~290 行）
> - 2026-01-09: Memory Palace — 处理了 9 条便签（SizedPtr 迁移验证、RBF 条款统计、AI-Design-DSL 锚点、atelia-copilot-chat 调查、Gotcha 2 条）
> - 2026-01-08: Memory Palace — 处理了 1 条便签（SizedPtr 迁移 Gotcha: 提交 942e1c0 倒退）
> - 2026-01-06: Memory Palace — 处理了 6 条便签（C# ref struct 限制、AteliaResult 双类型、Task 派生限制）
> - 2026-01-06: Memory Palace — 处理了 4 条便签（<deleted-place-holder> 替代性分析续、W-0006/W-0007 锚点汇总）
> - 2026-01-05: DocGraph 代码调查（Visitor 扩展机制、produce 验证路径、7 条便签）
> - 2026-01-04: Memory Palace — 处理了 3 条便签（SizedPtr/RBF/<deleted-place-holder> 调查锚点）
> - 2026-01-01: workspace_info 机制调查（Copilot Chat Agent Prompt System）

## 我是谁
源码分析专家，负责分析源码并产出实现 Brief。

## 我关注的项目
- [x] PieceTreeSharp - 已调查 2025-12-09
- [x] DocUI - 已调查 2025-12-09
- [x] PipeMux - 已调查 2025-12-09
- [x] atelia/prototypes - 已调查 2025-12-09
- [ ] atelia-copilot-chat

## Session Log
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

### 2026-01-12: AI-Design-DSL/DesignDsl 导航锚点汇总
**类型**: Route + Anchor + Signal
**项目**: DesignDsl, Atelia

#### 1. AI-Design-DSL 规范导航（Route）
| 意图 | 位置 | 关键锚点 |
|:-----|:-----|:---------|
| 术语定义语法 | [AI-Design-DSL.md](agent-team/wiki/SoftwareDesignModeling/AI-Design-DSL.md) | `[F-TERM-DEFINITION-FORMAT]`, `` term `ATX-Tree` `` |
| 条款定义语法 | 同上 | `[F-CLAUSE-DEFINITION-FORMAT]` |
| Identifier 格式 | 同上 | `[S-IDENTIFIER-ALNUM-HYPHENATED]` |

**置信度**: ✅ 验证过

#### 2. DocGraph 现有代码位置（Anchor）
| 意图 | 位置 |
|:-----|:-----|
| DocGraph Markdown 处理 | [DocumentGraphBuilder.cs](atelia/src/DocGraph/Core/DocumentGraphBuilder.cs) |

**备注**: DocGraph 当前只使用 FrontmatterParser 处理 YAML front-matter，不解析 Markdown 内容。DesignDsl 将填补这个空白。
**置信度**: ✅ 验证过

#### 3. DSL 关键字快速识别（Signal）
| 特征 | 含义 |
|:-----|:-----|
| Heading 以 `term`/`decision`/`spec`/`derived` 开头 | DSL 节点 |
| 反引号包裹 ID | Term-Node |
| 方括号包裹 ID | Clause-Node |

**置信度**: ✅ 验证过

### 2026-01-12: RBF 文档版本/条款 ID 导航锚点汇总
**类型**: Route + Signal + Gotcha
**项目**: RBF

#### 1. RBF 文档版本信息速查（Route）
| 文档 | 版本号位置 |
|:-----|:-----------|
| interface | `## 6. 最近变更` 表格第一行 |
| format | `## 10. 变更日志` 表格第一行 |
| test-vectors | `## 变更日志` 表格第一行 |
| decisions | 无版本号（AI-Design-DSL 保护的根文档）|

- **关联版本声明位置**: test-vectors.md L7 frontmatter 后的第一个 blockquote
- **置信度**: ✅ 验证过

#### 2. 条款 ID 引用分析速查（Route）
| 资源 | 位置 |
|:-----|:-----|
| 完整清单 | `agent-team/handoffs/rbf-clause-id-inventory.csv` |
| 影响分析 | `agent-team/handoffs/rbf-rename-impact-analysis.md` |
| 搜索命令 | `grep -rn "CLAUSE-ID" --include="*.md" --include="*.cs" atelia/ agent-team/` |

**高引用 TOP5**: R-REVERSE-SCAN-ALGORITHM(47), F-HEADLEN-FORMULA(47), F-FRAMESTATUS-VALUES(41), F-FRAME-LAYOUT(35), F-STATUSLEN-FORMULA(34)
**置信度**: ✅ 验证过

#### 3. 测试向量条款引用存在性判断（Signal）
- **验证方式**: grep 规范文档中的条款 ID
- **已知缺失**: `[S-RBF-SCANREVERSE-CONCURRENT-MUTATION]`、`[S-RBF-SCANREVERSE-MULTI-GETENUM]` 可能是测试向量自创的期望条款
- **置信度**: ⚠️ 推测，需确认是"规范缺失"还是"测试向量自行定义"

#### 4. 版本声明与实际版本的"假漂移"（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| test-vectors.md 声明对齐 format v0.28 / interface v0.20，实际已是 v0.32 / v0.26 | 看起来漂移 4-6 个版本，实际都是格式/表述优化，语义无变更 | 先看变更日志判断语义是否变化，再决定是否更新测试向量；版本号漂移 ≠ 内容漂移 |

#### 5. 条款 ID 改名时 archive/ 可跳过（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| `atelia/archive/2025-12-29-rbf-statejournal-v1/` 下有大量 .cs 文件引用旧条款 ID | 若全量替换会修改历史快照，破坏考古价值 | 改名脚本应排除 `archive/` 目录；历史代码引用保留旧 ID 是合理的 |

### 2026-01-12: spec-conventions 条款 ID 改名影响分析
**类型**: Route + Signal
**项目**: spec-conventions

#### 1. 影响范围导航（Route）
| 指标 | 数值 |
|:-----|:-----|
| 涉及条款 | 8 个条款 ID 改名 |
| 核心更新文件 | 4 个（spec-conventions.md + 2 meeting + 1 wiki）|
| 总更新点 | 15 处 |
| 高影响条款 | `S-DOC-FORMAT-MINIMALISM`（6 处非 SSOT 引用）|
| 无影响范围 | DocUI/PieceTreeSharp/PipeMux 均无引用 |

**详细报告**: [spec-conventions-rename-impact.md](../handoffs/spec-conventions-rename-impact.md)
**置信度**: ✅ 验证过（grep 全仓库搜索）

#### 2. 条款引用密度判断信号（Signal）
- **特征**: `S-DOC-FORMAT-MINIMALISM` 是唯一被 wiki 和多个 meeting 引用的条款
- **原因推测**: 该条款定义了"表示形式选择原则"，是最早定义且最常被引用的写作指导条款
- **迁移启示**: 基础性条款改名需优先处理，因其下游引用更广
- **置信度**: ✅ 数据支持

### 2026-01-11: Atelia.Data 测试架构治理 + 代码去重分析
**类型**: Route + Anchor + Gotcha
**项目**: Atelia.Data

#### 1. 测试泛化陷阱（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 测试断言 `IsPassthrough` 或中间状态 `inner.Data()` | 泛化到 `IReservableBufferWriter` 接口后 `SinkReservableWriter` 测试失败 | 只验证最终结果；`IsPassthrough`（Chunked）与 `IsIdle`（Sink）语义不同，需拆分测试 |

#### 2. 接口契约 vs 实现细节判定锚点
| 问题 | 答案 |
|:----|:----|
| 最终数据顺序 | ✅ 接口契约 |
| Flush 时机 | ❌ 实现细节 |
| 中间状态可观察性 | ❌ 实现细节 |
| Reservation 阻塞语义 | ⚠️ 契约推论（不乱序 flush）|

**位置**: [IReservableBufferWriter.cs](atelia/src/Data/IReservableBufferWriter.cs) 接口注释

#### 3. 测试架构治理导航（Route）
**快速判定三问**:
1. 只用 `IReservableBufferWriter` 方法？ → Yes=可接口化
2. 只验证最终数据？ → Yes=可接口化
3. 使用 `IsPassthrough`/`FlushedLength`/`BlockingReservationToken`？ → Yes=实现级

**文件改造速查**: NegativeTests ✅ Done, Tests P0, P1Tests/OptionsTests P1, P2Tests/StatsTests P3
**详细分析**: [2026-01-11-test-architecture-governance-analysis.md](agent-team/handoffs/2026-01-11-test-architecture-governance-analysis.md)

#### 4. CRW/SRW 去重分析锚点
**代码重复统计**: 完全相同 ~36%, 高度相似 ~19%, 语义相同 ~10%, 可提取总量 ~65%
**关键可提取项**: `Bijection` P0, `AllocReservationToken`/Chunk尺寸计算/`TryRecycleFlushedChunks` P1, `Reservation`类 P2
**统一差异**: `unchecked`→统一为 unchecked, `DataBegin=0`→统一为显式
**详细分析**: [2026-01-11-deduplication-analysis.md](agent-team/handoffs/2026-01-11-deduplication-analysis.md)

#### 5. ReservableWriter Chunk 统一方案锚点
- **CRW 定义**: [ChunkedReservableWriter.cs#L33-L44](atelia/src/Data/ChunkedReservableWriter.cs#L33-L44)
- **SRW 定义**: [SinkReservableWriter.cs#L28-L40](atelia/src/Data/SinkReservableWriter.cs#L28-L40)
- **差异**: 7/8 成员相同，1 属性 CRW 独有，1 方法 SRW 独有
- **结论**: 合并为 `internal sealed class ReservableWriterChunk`
- **分析报告**: [2026-01-11-unified-chunk-analysis.md](agent-team/handoffs/2026-01-11-unified-chunk-analysis.md)

#### 6. BitOperations.RoundUpToPowerOf2 溢出陷阱（Gotcha）
| 输入 | 返回 (uint) | 转 int |
|:-----|:------------|:-------|
| 1GB | 1GB | 1GB ✅ |
| 1GB+1 | 2GB (0x80000000) | -2147483648 ❌ |

**规避**: 仅对 `candidate ≤ (1 << 30)` 调用 RoundUp
**位置**: [ChunkSizingStrategy.cs#L38-L39](atelia/src/Data/ChunkSizingStrategy.cs#L38-L39)
**分析报告**: [2026-01-11-p0-overflow-fix-analysis.md](agent-team/handoffs/2026-01-11-p0-overflow-fix-analysis.md)

#### 7. P1 可变 struct 设计问题（Gotcha + Anchor）
| struct | 位置 | 问题 |
|:-------|:-----|:-----|
| `ReservationTracker` | [ReservationTracker.cs#L14](atelia/src/Data/ReservationTracker.cs#L14) | mutable struct + 引用字段 |
| `ChunkSizingStrategy` | [ChunkSizingStrategy.cs#L7](atelia/src/Data/ChunkSizingStrategy.cs#L7) | mutable struct + 值字段 |

**核心风险**: struct 复制后值字段独立、引用字段共享→"部分分叉部分共享"
**BCL 惯例**: List/Dictionary/Queue/Stack 全是 class，无 mutable struct 先例
**推荐方案**: 改为 `sealed class`（2 行改动）
**分析报告**: [2026-01-11-p1-mutable-struct-analysis.md](agent-team/handoffs/2026-01-11-p1-mutable-struct-analysis.md)

#### 8. IBufferWriter<byte> + IByteSink 双接口兼容性（Signal）
- Pull（IBufferWriter）与 Push（IByteSink）正交，可共存
- 两者共享同一个 `_pos` / `MemoryStream`

#### 9. RandomAccess → IByteSink 适配器设计锚点
- **设计报告**: [2026-01-11-randomaccess-bytesink-design.md](agent-team/handoffs/2026-01-11-randomaccess-bytesink-design.md)
- **旧方案**: `SequentialRandomAccessBufferWriter` (~80行) → **新方案**: `RandomAccessByteSink` (~25行, 68%减少)
- **关键约束**: `@[I-RBF-SEQWRITER-HEADLEN-GUARD]` 保留

#### 10. IByteSink 推式接口完美匹配 RandomAccess.Write（Signal）
- `IByteSink.Push(ReadOnlySpan<byte>)` 语义 = `RandomAccess.Write(handle, data, offset)` 语义
- 对比 `IBufferWriter<byte>` 需 ArrayPool 适配，`IByteSink` 可直接转发

**置信度**: ✅ 全部验证过

### 2026-01-09: RBF/AI-Design-DSL 验证锚点汇总
**类型**: Route + Anchor
**项目**: RBF, AI-Design-DSL

| 锚点 | 验证命令 | 状态 |
|:-----|:---------|:-----|
| SizedPtr 迁移完整性 | `grep -rn "deleted-place-holder" atelia/docs/Rbf/` | ✅ 0 匹配 |
| rbf-decisions.md 条款 | 9 条（6 Decision + 3 Format） | ✅ 验证过 |
| AI-Design-DSL 跨文档引用 | `grep -rn "S-RBF-DECISION-WRITEPATH\|S-RBF-DECISION-4B-ALIGNMENT" atelia/docs/Rbf/` | ✅ 验证过 |

**rbf-decisions.md 条款统计**：
- 位置: [atelia/docs/Rbf/rbf-decisions.md#L21-L89](atelia/docs/Rbf/rbf-decisions.md#L21-L89)
- 6 条 `[S-RBF-DECISION-*]`（决策性）+ 3 条 `[F-*]`（格式定义性）
- 条款 ID 格式符合 AI-Design-DSL 的 `[F-CLAUSE-ID-FORMAT]`

**跨文档引用位置**：
- `rbf-interface.md` L17 → `[S-RBF-DECISION-WRITEPATH-SINKRESERVABLEWRITER]`
- `rbf-format.md` L114 → `[S-RBF-DECISION-4B-ALIGNMENT-ROOT]`
- **Phase 1 TODO**: 将引用格式从 `**\`[ID]\`**` 迁移为 `@[ID]`

**置信度**: ✅ 验证过（2026-01-09）

### 2026-01-09: AI-Design-DSL 迁移 Gotcha — Heading 锚点会变化
**类型**: Gotcha
**项目**: AI-Design-DSL

| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| DSL 化后 Heading 格式从 `## 标题` 变为 `## modifier [ID] Title` | GitHub 自动生成的锚点会改变 | 1. 确认无外部链接依赖后再迁移；2. 内部引用优先使用 Clause ID；3. 如需稳定锚点，添加 HTML anchor |

**示例**：`#s-rbf-decision-xxx` → `#decision-s-rbf-decision-xxx-中文title`

**置信度**: ⚠️ 经验性观察

### 2026-01-09: atelia-copilot-chat Fork 现状调查
**任务**: 调研 Fork 版本差距、核心修改点、扩展机制
**关键发现**:

#### 1. Fork 版本状态
| 指标 | 数值 |
|:-----|:-----|
| 版本差距 | 6 commits ahead, 64 commits behind |
| 我们的改动 | 8 files, +1083/-5 lines |
| 新增功能 | `summarizedConversationHistory.tsx` (966 lines) — half-context 摘要 |

#### 2. 核心修改点锚点
- [copilotIdentity.tsx](atelia-copilot-chat/src/extension/prompts/node/base/copilotIdentity.tsx) — 身份定义
- [safetyRules.tsx](atelia-copilot-chat/src/extension/prompts/node/base/safetyRules.tsx) — 安全规则
- [agentPrompt.tsx#L98](atelia-copilot-chat/src/extension/prompts/node/agent/agentPrompt.tsx#L98) — 角色描述

#### 3. runSubagent API 稳定性（Signal）
- 代码特征: `ToolName.CoreRunSubagent = 'runSubagent'` + `ToolCategory.Core`
- 历史波动: 曾被删除（#1819）后恢复 — 非核心架构，可能再次变动
- 监控命令: `git log --all --oneline -- "**/runSubagent*" "**/subagent*"`

#### 4. PromptRegistry 扩展机制（推测）
- 入口: [promptRegistry.ts](atelia-copilot-chat/src/extension/prompts/node/agent/promptRegistry.ts)
- 扩展点: `IAgentPrompt` 接口 + `PromptRegistry.registerPrompt()`
- 可定制项: SystemPrompt, CopilotIdentityRules, SafetyRules, userQueryTagName, attachmentHint
- 潜在"第四条路": 通过注册自定义 AgentPrompt 实现低侵入定制（待深入调研）

**置信度**: ✅ 版本数据验证过；⚠️ 扩展机制为推测

### 2026-01-09: SizedPtr 范围估算陷阱 — 4B 对齐导致 ×4
**类型**: Gotcha
**项目**: RBF

| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 38-bit offset 范围容易被误算为 ~256GB | 因 4B 对齐实际为 ~1TB，文档中数值偏小 4 倍 | 1. 从 SSOT（`SizedPtr.cs`）读取 `MaxOffset`/`MaxLength` 常量；2. 避免硬编码估算值；3. 公式：`(2^bits - 1) × 4` |

**已知问题点**: [rbf-interface.md#L103](atelia/docs/Rbf/rbf-interface.md#L103)

**置信度**: ✅ 验证过

### 2026-01-09: "语义重复 vs 纯引用"快速检查路径
**类型**: Route
**项目**: RBF

- **判断标准**: 下层条款正文是否只含引用声明，还是重述了上层语义
- **验证命令**: 
  ```bash
  grep -A10 "^### design" atelia/docs/Rbf/rbf-interface.md | grep -E "depends:|>"
  ```
- **正确示范**: `rbf-format.md` §2.1 — "已上移至 Decision-Layer" + 纯引用列表

**置信度**: ✅ 验证过

### 2026-01-07: SizedPtr 迁移 Gotcha — 提交 942e1c0 是"倒退"
**类型**: Gotcha
**项目**: SizedPtr 迁移

| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 提交 942e1c0 "RBF文档修订"实际把 SizedPtr 改回 <deleted-place-holder> | 3 周工作被覆盖，SizedPtr=15→0，<deleted-place-holder>=1→21 | `git revert 942e1c0` 可直接恢复正确状态 |

**现象**: 提交信息听起来像"改进"，实际是"倒退"
**教训**: 代码审查时需验证提交实际改了什么，不能只看提交信息

**置信度**: ✅ 已验证

### 2026-01-06: C# ref struct / Task 派生限制调查
**任务**: AteliaResult 实现过程中发现的 C# 语言边界
**关键发现**:

#### 1. `allows ref struct` 限制（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 即使泛型参数声明 `allows ref struct`，也无法在 `Func<T, TResult>` 中使用 ref struct | `AteliaResult<T>.Map()` 无法支持 ref struct | 使用 `out` 参数模式或创建专用 `ref struct` 结果类型 |
| `readonly struct` 不能声明 `allows ref struct` | `AsyncAteliaResult<T>` 无法包含 ref struct 值 | 异步场景下 ref struct 必须"物化"为普通类型后传递 |

**何时使用 `allows ref struct`**：
- 在泛型约束中添加 `where T : allows ref struct`
- 参数声明为 `scoped T` 确保安全
- **不能使用** `Func<T>` / `Action<T>` 等委托
- 示例: .NET 9 的 `Dictionary.GetAlternateLookup<TAlternateKey>()` 支持 `ReadOnlySpan<char>`

**置信度**: ✅ 验证过

#### 2. AteliaResult 同步/异步双类型设计
| 类型 | 路径 | 形态 |
|:-----|:-----|:-----|
| 同步 | `atelia/src/Primitives/AteliaResult.cs` | `ref struct` |
| 异步 | 待实现 `atelia/src/Primitives/AsyncAteliaResult.cs` | `readonly struct` |

- **设计文档**: `agent-team/handoffs/atelia-async-result-design.md`
- **原因**: ref struct 不能跨 await 边界，故需两种类型
- **转换**: `ToAsync()` 方法提供显式转换

#### 3. 从 Task<T> 派生的根本限制（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 想通过 `AteliaTask<T> : Task<T>` 避免双重包装 | 无法工作——`TrySetResult`/`TrySetException`/promise 构造函数全是 `internal` | 使用 `Task<AsyncAteliaResult<T>>` 或 `ValueTask<AsyncAteliaResult<T>>` |

**Task 内部派生类锚点**（仅限 BCL 内部可用）：
- `WhenAllPromise` — 用于 `Task.WhenAll`
- `DelayPromise` — 用于 `Task.Delay`
- `TwoTaskWhenAnyPromise<T>` — 用于双 Task 的 `WhenAny`
- `UnwrapPromise<T>` — 用于 `Task.Run`/`Unwrap`

**置信度**: ✅ 从 dotnet/runtime 源码验证
### 2026-01-05: DocGraph 代码调查
**任务**: Wish-0007 相关的 DocGraph 源码调查，定位 Visitor 扩展点和 produce 验证机制
**关键发现**:

#### 1. Visitor 扩展机制
| 类型 | 位置 | 备注 |
|:-----|:-----|:-----|
| **扩展入口** | `RunCommand.cs#L95-L101` | `GetVisitors()` 硬编码列表 |
| **frontmatter 字段** | `GlossaryVisitor.cs#L93-L103` | `KnownFrontmatterFields` 静态类 |
| **Wish 归属推导** | `DocumentNode.ProducedBy` | 比路径正则更健壮 |

#### 2. produce 验证 → 空文件创建路径
- `DocumentGraphBuilder.cs#L402` — `ValidateProduceRelations()` 检测文件不存在
- `DocumentGraphBuilder.cs#L424` — 添加 `CreateMissingFileAction`
- `CreateMissingFileAction.cs#L89` — `Execute()` 写入模板内容
- **关键问题**: 不区分"手动维护"和"自动生成"的产物文件

#### 3. 多输出 Visitor 实现路径
1. `IDocumentGraphVisitor.cs` — 接口扩展点
2. `RunCommand.cs#L147` — `GetVisitors()` 注册入口
3. `RunCommand.cs#L104-L130` — Visitor 执行循环
- **建议**: 接口扩展 `GenerateMultiple()` 而非拆分 Visitor 类

#### 4. Gotcha 陷阱
| 陷阱 | 后果 | 规避 |
|:-----|:-----|:-----|
| IssueAggregator 已存在 | 重复造轮子 | W-0007 应改为"扩展"而非"新建" |
| produce 声明 vs Visitor 输出路径不一致 | fix 阶段用空模板覆盖手动文件 | produce 只声明 `.gen.md` 路径 |

**置信度**: ✅ 全部验证过

#### 5. 关键锚点汇总
**W-0006 <deleted-place-holder>/SizedPtr 锚点**：
- <deleted-place-holder> 权威定义: [rbf-interface.md#L111-L122](atelia/docs/Rbf/rbf-interface.md#L111-L122)
- SizedPtr 权威定义: [atelia/src/Data/SizedPtr.cs](atelia/src/Data/SizedPtr.cs)
- 关键冲突: <deleted-place-holder> 有 Null 语义，SizedPtr 无——需分层策略
- 代码状态: RBF 历史代码已归档到 `atelia/archive/2025-12-29-rbf-statejournal-v1/`

**W-0007 Issue 状态锚点**：
- I-ID-DESIGN: 已在 [Shape.md#L60-L80](wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Shape.md) 定义，代码实现于 `IssueAggregator.cs`
- Visitor 架构: `GlossaryVisitor`, `GoalAggregator`, `IssueAggregator`, `ReachableDocumentsVisitor`, `TwoTierAggregatorBase`
- C-MORE-VISITORS / C-MORE-TESTS 是长期演进目标，非阻塞性

### 2026-01-05: SizedPtr/<deleted-place-holder> 替代性分析（续）
**任务**: W-0006 相关的 <deleted-place-holder> 使用点分析，验证 SizedPtr 完全替代可行性
**关键发现**:
1. **<deleted-place-holder> 使用点定位**：
   - 类型定义: `rbf-interface.md#L82-L97`（§2.3）
   - 接口签名: 9 处（grep 验证）
   - Wire format: `rbf-format.md#L292-L302`（§7）
   - DataTail: `rbf-format.md#L310`（使用"地址"而非类型名）
2. **核心结论**：所有 9 处使用都是"定位 Frame"用途，SizedPtr 可完全替代
3. **Gotcha: DataTail 的"纯位置"假象**：
   - **现象**: DataTail 定义为"地址"，似乎只需位置不需长度
   - **后果**: 如果据此保留 <deleted-place-holder>，会造成 <deleted-place-holder>/SizedPtr 共存的复杂性
   - **规避**: DataTail 实际语义是"文件 EOF 位置"，`SizedPtr.Offset` 完全等价

**置信度**: ✅ 验证过

### 2026-01-04: SizedPtr/RBF/<deleted-place-holder> 现状调查
**任务**: Wish-0004 SizedPtr 设计调查，定位权威定义和代码现状
**关键发现**:
1. **<deleted-place-holder> 权威定义位置**：
   - 位置: `atelia/docs/Rbf/rbf-interface.md#2.3`
   - 条款: `[F-<deleted-place-holder>-DEFINITION]`, `[F-<deleted-place-holder>-ALIGNMENT]`, `[F-<deleted-place-holder>-NULL]`
   - 源码实现已归档: `atelia/archive/2025-12-29-rbf-statejournal-v1/Rbf/<deleted-place-holder>.cs`
2. **RBF 层代码状态**：
   - 搜索 `atelia/src/**` 无任何 <deleted-place-holder>/Rbf/SizedPtr 匹配
   - RBF 层实现已整体归档到 `atelia/archive/2025-12-29-rbf-statejournal-v1/`
   - **结论**: SizedPtr 需从零开始在 `atelia/src/Data/` 实现
3. **Null 语义冲突（Gotcha）**：
   - Wish-0004 非目标写"不定义特殊值"，但 <deleted-place-holder> 定义了 `Null=0`
   - **风险**: 若不澄清，上层 RBF 接口迁移时会卡住
   - **建议**: 在 Shape-Tier 明确立场——SizedPtr 保持纯净，Null 由 RBF 层自行包装

### 2026-01-01: workspace_info 机制调查
**任务**: 分析 VS Code Copilot Chat 中 workspace_info 的生成机制
**关键发现**:
1. **组成结构**：`workspace_info` 是 `GlobalAgentContext` 的子组件，包含 Tasks、FoldersHint、WorkspaceStructure 三部分
2. **深度控制**：无显式深度限制，由 `maxSize=2000` 字符预算和 BFS 算法共同决定
3. **生成算法**：`visualFileTree.ts` 实现广度优先展开，空间不足时添加 `...` 截断
4. **排序规则**：文件在前目录在后，同类型按名称排序
5. **过滤机制**：遵循 `.gitignore`、Copilot Ignore、排除点文件（默认）
6. **缓存策略**：首轮渲染后缓存到 Turn Metadata，后续轮次复用
7. **条件渲染**：仅在 `list_dir` 工具可用时渲染目录结构
**实际意义**: 将 recipe 移到根目录可提高其在 workspace_info 中的可见性（更短路径 = 更高优先级）
**交付**: [handoffs/2026-01-01-workspace-info-mechanism-INV.md](../handoffs/2026-01-01-workspace-info-mechanism-INV.md)

### 2025-12-27: Storage Engine M1 风险分析
**任务**: 调查 StateJournal + Rbf 现状，识别 M1 阶段高风险项
**关键发现**:
1. **RbfScanner 全量内存读取**：当前 `RbfScanner(ReadOnlyMemory<byte> data)` 把整个文件读入内存，对 GB 级仓库不可行。M1 必须重构为流式/分块读取
2. **Durable flush 抽象缺失**：`IRbfFramer.Flush()` 按设计只推缓冲到下层，fsync 由上层自理——但当前没有暴露底层句柄的途径
3. **建议方案**：引入 `FileBackedBufferWriter` 和 `FileBackedRbfScanner`，内部持有 `SafeFileHandle`，暴露 `FlushToDisk()` 方法
**深层洞见**: "接口设计正确但实现层缺失"的典型案例——接口预留了扩展点（`IBufferWriter<byte>` 注入），但 MVP 只实现了内存版本

### 2025-12-27: Workspace/ObjectLoader/RBF 设计意图调查
**任务**: 分析 StateJournal 设计文档，提取 Workspace、ObjectLoader、RBF 的设计意图
**关键发现**:
1. **Workspace 定位**：类比 Git working tree，是核心协调器（Identity Map、Dirty Set、HEAD 追踪、Commit 协调）
2. **ObjectLoader 是内部实现**：不是独立组件，LoadObject 流程定义在 Workspace 内部
3. **四阶段读取模型**：Deserialize → Materialize（Shallow）→ LoadObject → ChangeSet
4. **RBF 层级关系**：RBF 是 Layer 0，提供二进制帧封装；StateJournal 是 Layer 1，定义 Record 语义
5. **护照模式**：每个对象 MUST 绑定一个 Owning Workspace，绑定不可变
6. **分层 API 设计**：Layer 1（构造函数）→ Layer 2（工厂）→ Layer 3（可选 Ambient）
**交付**: [handoffs/2025-12-27-workspace-objectloader-rbf-investigation-INV.md](../handoffs/2025-12-27-workspace-objectloader-rbf-investigation-INV.md)
**待确认问题**:
- B-8: LoadObject<T> 是否应拆分为非泛型底层 + 泛型包装？

### 2025-12-26: `_removedFromCommitted` 集合必要性分析
**任务**: 调查 DurableDict 中 `_removedFromCommitted` 集合是否多余
**关键发现**:
1. **不是 Materialize 的问题**：加载时 `_committed` 是最终状态，`_removedFromCommitted` 初始为空
2. **运行时状态管理的副产品**：双字典策略要求 `_committed` 在 Commit 前只读，Remove 操作无法直接修改，只能用集合记录删除意图
3. **符合规范条款**：`[S-WORKING-STATE-TOMBSTONE-FREE]` 要求 Working State 无 tombstone，当前实现用集合而非 tombstone 值满足约束
4. **替代设计存在**：改为单一 `_current` 合并视图可消除该集合，但需要重构读写路径
**结论**: 设计上可以消除，但当前架构下有其存在理由。保持现有设计，考虑长期重构。
**交付**: [handoffs/2025-12-26-removedFromCommitted-analysis-INV.md](../handoffs/2025-12-26-removedFromCommitted-analysis-INV.md)
**深层洞见** (2025-12-26 补充):
- **设计权衡本质**：双字典策略的核心约束是"_committed 在 Commit 前只读"。带来 Commit 失败时恢复简单的好处，代价是需要 `_removedFromCommitted` 追踪删除意图
- **规范与实现的巧妙契合**：`[S-WORKING-STATE-TOMBSTONE-FREE]` 用集合（而非 tombstone 值）实现——隐晦但有效
- **监护人意见精确定位**：意见针对 Load/Materialize 阶段，但实际问题在运行时状态管理；加载时 `_committed` 确实是最终状态

### 2025-12 早期调查归档
> **归档位置**: [archive/2025-12-session-log.md](archive/2025-12-session-log.md)
> **覆盖范围**: 2025-12-09 ~ 2025-12-24
> **内容概要**: 项目现状核实（PieceTreeSharp/DocUI/PipeMux/atelia-prototypes）、StateJournal 更名迁移、RBF v0.12 变更适配、mvp-design-v2 决策引用分析、DocUI 研讨会发言、畅谈会参与（Tool-As-Command/错误反馈模式/MVP设计/写入路径）
> **归档原因**: 实例类调查记录，已沉淀为 handoff/wiki，保留指针即可

## Key Deliverables
