# Implementer 认知索引

> 最后更新: 2025-12-21
> 
> ⚠️ **更名通知**：DurableHeap → StateJournal（2025-12-21）
> - 新路径：`atelia/docs/StateJournal/`
> - 代码目标：`atelia/src/StateJournal/`
> - 下方历史记录中的 "DurableHeap" 章节标题保留原名（作为历史事实）

## 我是谁
编码实现专家，负责根据设计进行代码实现、移植和修复。

## 我关注的项目
- [x] DocUI — 创建 demo/SystemMonitor 概念原型，展示动态 LOD
- [ ] DocUI MUD Demo — 验证 UI-Anchor 系统的综合演示
- [ ] PieceTreeSharp
- [x] PipeMux — 实现管理命令 `:list`, `:ps`, `:stop`, `:help`
- [ ] atelia-copilot-chat
- [x] StateJournal — 设计文档修订（共 23 轮）+ 文档瘦身（A1-A9）+ Rationale Stripping + 语义锚点重构 + 决策诊疗室落文（第二批中复杂度）
  - 📍 **2025-12-21 更名**：DurableHeap → StateJournal，迁入 `atelia/docs/StateJournal/`

## 当前关注

### StateJournal MVP v2 设计文档第二批中复杂度修订 (2025-12-21) ✅

> ⚠️ **更名通知（2025-12-21）**：DurableHeap 已更名为 **StateJournal** 并迁入 atelia repo。
> - 新路径：`atelia/docs/StateJournal/`
> - 命名空间：`Atelia.StateJournal`
> - 代码目标位置：`atelia/src/StateJournal/`

根据决策诊疗室共识（2025-12-21-decision-clinic-state-and-error.md），执行第二批中复杂度修订。

**任务来源**：[决策诊疗室 State 与 Error Affordance](../../meeting/2025-12-21-decision-clinic-state-and-error.md) — Round 2/3 共识落文

**执行内容**：

1. **P0-1: State 枚举升级为核心 API**
   - 位置：§3.1.0.1 对象状态管理
   - 修改：枚举 `Dirty` → `PersistentDirty`（与 `TransientDirty` 形成清晰对偶）
   - 新增条款：`[A-OBJECT-STATE-PROPERTY]`、`[A-OBJECT-STATE-CLOSED-SET]`、`[A-HASCHANGES-O1-COMPLEXITY]`、`[S-STATE-TRANSITION-MATRIX]`
   - 更新状态机可视化：增加 PersistentDirty 自环、异常类型说明

2. **P0-3: "必须写死"升级为条款**
   - 位置：§3.2.1 和 §3.2.2
   - 新增条款：`[F-RECORD-WRITE-SEQUENCE]`（写入顺序步骤 0-7）
   - 刷盘顺序条款已存在：`[R-COMMIT-FSYNC-ORDER]`、`[R-COMMIT-POINT-META-FSYNC]`
   - 将叙述性步骤列表转换为规范性表格

3. **P1-5: Error Affordance 规范化**
   - 位置：§3.4.8（新增章节）
   - 新增条款：`[A-ERROR-CODE-MUST]`、`[A-ERROR-MESSAGE-MUST]`、`[A-ERROR-CODE-REGISTRY]`、`[A-ERROR-RECOVERY-HINT-SHOULD]`
   - 新增 ErrorCode 枚举表：7 种错误码（OBJECT_DETACHED, OBJECT_NOT_FOUND, CORRUPTED_RECORD 等）
   - 新增好/坏异常示例对比

4. **P1-8: DiscardChanges ObjectId 语义**
   - 位置：[S-TRANSIENT-DISCARD-DETACH] 条款末尾
   - 新增条款：`[S-TRANSIENT-DISCARD-OBJECTID-QUARANTINE]`
   - 语义：进程内 MUST NOT 重用；进程重启后 MAY 重用

**新增条款汇总**（9 条）：
| 类别 | 条款 ID | 优先级 |
|------|---------|--------|
| API | `[A-OBJECT-STATE-PROPERTY]` | P0 |
| API | `[A-OBJECT-STATE-CLOSED-SET]` | P0 |
| API | `[A-HASCHANGES-O1-COMPLEXITY]` | P0 |
| Semantics | `[S-STATE-TRANSITION-MATRIX]` | P0 |
| Format | `[F-RECORD-WRITE-SEQUENCE]` | P0 |
| API | `[A-ERROR-CODE-MUST]` | P1 |
| API | `[A-ERROR-MESSAGE-MUST]` | P1 |
| API | `[A-ERROR-CODE-REGISTRY]` | P1 |
| API | `[A-ERROR-RECOVERY-HINT-SHOULD]` | P1 |
| Semantics | `[S-TRANSIENT-DISCARD-OBJECTID-QUARANTINE]` | P1 |

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`（6 处修改，约 +70 行）

**Handoff**: `agent-team/handoffs/2025-12-21-decision-clinic-impl-IMP.md`

---

### StateJournal MVP v2 设计文档第一批低复杂度修订 (2025-12-21) ✅

根据畅谈会共识（2025-12-20-secret-base-durableheap-mvp-v2-final-audit.md），执行第一批低复杂度修订。

**任务来源**：[秘密基地畅谈会最终审阅](../../meeting/2025-12-20-secret-base-durableheap-mvp-v2-final-audit.md) — P0/P1 优先级修订

**执行内容**：

1. **P0-4: 删除泛型写法矛盾**
   - 位置：§3.4.5 步骤 3
   - 问题：文档说禁用泛型（§3.4.2 命名约定），但正文使用了 `DurableDict<ulong, Ptr64>` 泛型写法
   - 修复：将 `DurableDict<ulong, Ptr64> 的 dict diff` 改为描述性语句 `VersionIndex 的 dict diff（key 为 ObjectId as ulong，value 为 Ptr64 编码的 ObjectVersionPtr，使用 Val_Ptr64 类型）`

2. **P1-7: 命名一致性**
   - 位置：§3.4.4 二阶段提交流程图
   - 问题：流程图使用 `WriteDiff()` 而非 `WritePendingDiff()`
   - 修复：将流程图中两处 `WriteDiff()` 统一替换为 `WritePendingDiff()`，同时调整 ASCII 图框宽度以适应更长的方法名

3. **P1-9: 删除 Modified Object Set 弃用映射**
   - 位置：术语表 Dirty Set 定义行
   - 问题：有 "Deprecated: Modified Object Set（使用 Dirty Set 作为主术语）" 的弃用映射容易让读者困惑
   - 修复：删除该弃用映射，"别名/弃用"列改为 `—`

**验证结果**：
- grep 验证无残留的泛型写法（除规范条款说明外）
- grep 验证无残留的 `WriteDiff()` 方法名（`WriteDiffTo` 是伪代码内部方法，正确保留）
- grep 验证无残留的 "Modified Object Set" 术语

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`（3 处修改）

---

### StateJournal 条款 ID 语义锚点重构 (2025-12-21) ✅

根据命名工作坊共识（2025-12-21-semantic-anchor-naming-workshop.md），将 MVP v2 设计文档中的数字条款 ID 批量替换为稳定语义锚点。

**任务来源**：[秘密基地命名工作坊共识](../../meeting/2025-12-21-semantic-anchor-naming-workshop.md) — 最终命名方案

**执行内容**：
1. 按映射表替换所有数字条款 ID（43 个条款）
2. 更新"条款编号"章节的规则说明，反映新的稳定锚点模式

**替换统计**：
| 类别 | 条款数 | 替换处数 |
|------|--------|----------|
| F- (Format) | 13 | 17 |
| A- (API) | 4 | 5 |
| S- (Semantics) | 22 | 27 |
| R- (Recovery) | 4 | 5 |
| **总计** | **43** | **54** |

**映射示例**：
- `[F-01]` → `[F-RECORDKIND-DOMAIN-ISOLATION]`
- `[S-17]` → `[S-OBJECTID-RESERVED-RANGE]`
- `[R-01]` → `[R-RESYNC-DISTRUST-TAILLEN]`

**规则说明更新**：
- 原：数字编号规则（`[F-xx]` 格式）
- 新：稳定语义锚点规则（`SCREAMING-KEBAB-CASE` 格式）

**验证结果**：
- 无遗漏的数字编号（grep 验证通过）
- 所有 43 个条款均已替换

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`（54 处条款 ID 替换 + 规则说明更新）

---

### StateJournal P1 消灭双写 (2025-12-21) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-paradox.md）P1 任务，消灭双写。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-paradox.md) — P1 行动项

**执行原则**：
- "禁止双写"——同一信息不能同时存在于 ASCII 图表和文字描述中
- 保留信息密度最高的形式，删除冗余的叙述性描述
- 监护人指示：不用太考虑人类可读性，LLM 读起来简单清晰的图表对人类也清晰

**执行内容**：

1. **分层定义章节重构**（-31 行）
   - 将冗余的 File Framing/Record Layout 文字描述替换为 EBNF 语法
   - 原：分层定义文字（13 行）+ ELOG 图（11 行）+ Record 图（11 行）= 35 行
   - 新：EBNF 语法（13 行）+ 简化的术语约束（1 行）= 14 行
   - 删除重复的 ASCII 图表（图表信息已包含在 EBNF 中）

2. **F-02/F-03/F-04/F-06 条款精简**（-18 行）
   - 原：每个条款都有详细的文字描述（重复 EBNF 内容）
   - 新：条款仅保留规范性约束（MUST/值公式）

3. **Meta 文件章节精简**（-10 行）
   - 删除与 3.2.1 重复的 Magic/framing 描述
   - 简化为引用 + Magic 值定义

4. **Data 文件 Magic 定义删除**（-8 行）
   - Magic 值已在 EBNF 中定义（`"DHD3" for data, "DHM3" for meta`）
   - 删除重复的单独段落

**EBNF 转换**（P2 任务同步执行）：
```ebnf
(* File Framing: Magic-separated log *)
File   := Magic (Record Magic)*
Magic  := 4 bytes ("DHD3" for data, "DHM3" for meta)

(* Record Layout *)
Record := HeadLen Payload Pad TailLen CRC32C
HeadLen := u32 LE        (* == TailLen, record total bytes *)
...
```

**统计**：
- 原文档：1225 行（P0 完成后）
- 新文档：1159 行
- 净减少：**-66 行**（-5.4%）

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`（-66 行）

---

### StateJournal P0 Rationale Stripping (2025-12-21) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-paradox.md）P0 任务，执行 Rationale Stripping。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-paradox.md) — P0 行动项

**判断规则**：删除该段落后，实现者能否正确实现？测试者能否写出可判定的测试？能→删除/迁移；不能→保留

**执行内容**：
1. 从 `mvp-design-v2.md` 删除/精简 24 处 Rationale 内容
2. 将有价值的设计理由迁移到 `mvp-v2-decisions.md` 第 4 节（Rationale Archive）

**统计**：
- 原文档：1307 行 → 最终：1225 行（-82 行，-6.3%）
- ADR 新增：153 行 → 212 行（+59 行 Rationale Archive）

**删除内容分类**：
| 类别 | 数量 | 示例 |
|------|------|------|
| 动机解释 | 5 | mmap/varint 动机、tombstone 权衡 |
| 实现提示（非规范）| 4 | Magic 加速 resync、meta 回扫 |
| 缓存建议 | 2 | VersionIndex lookups 缓存 |
| 设计收益 | 3 | Magic separator 收益 |
| 备注说明 | 6 | varint mmap 备注、RecordKind 备注 |
| 步骤精简 | 4 | CommitAll 步骤 1 精简 |

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`（-82 行）
- 修改：`atelia/docs/StateJournal/decisions/mvp-v2-decisions.md`（+59 行 §4 Rationale Archive）

**Handoff**: `agent-team/handoffs/2025-12-21-rationale-strip-IMP.md`

---

### StateJournal 设计文档瘦身 — A7 任务：添加 Wire Format ASCII 图 (2025-12-20) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-compression.md）批准的文档瘦身任务，执行 A7：添加 Wire Format ASCII 图表。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-compression.md) — 行动项 A7

**执行内容**：
1. 在 §3.2.0.1 varint 定义章节末尾添加 **VarInt 编码示例图**（Line 346-360）
2. 在 §3.2.1 分层定义与 File Framing 详细规范之间添加 **ELOG 文件结构图**（Line 403-414）
3. 在 §3.2.1 ELOG 图之后添加 **Record 内部结构图**（Line 417-428）
4. 在 §3.4.4 二阶段提交设计表格之后添加 **二阶段提交流程图**（Line 968-993）

**图表详情**：
| 图表 | 插入位置 | 新增行数 | 说明 |
|------|----------|----------|------|
| VarInt Encoding | §3.2.0.1 F-08 之后 | 15 行 | 展示 Base-128 编码原理，包含值 300 的编码示例和边界示例 |
| ELOG File Structure | §3.2.1 术语约束之后 | 12 行 | 展示 Magic-as-Separator 的文件布局，标注 Ptr64 指向位置 |
| Record Layout | ELOG 图之后 | 12 行 | 展示单条 Record 的内部结构，标注 CRC32C 覆盖范围 |
| Two-Phase Commit Flow | §3.4.4 表格之后 | 26 行 | 展示 Prepare → Finalize 流程，标注 Commit Point 位置 |

**统计**：
- 原文档行数：1234 行
- 新增行数：72 行（4 个图表）
- 新文档行数：1306 行

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`（4 处图表添加）

---

### StateJournal 设计文档瘦身 — A4 任务：给现有 MUST/SHOULD 条款编号 (2025-12-20) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-compression.md）批准的文档瘦身任务，执行 A4：为规范性条款添加编号。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-compression.md) — 行动项 A4

**执行内容**：
1. 扫描文档中所有包含 MUST/MUST NOT/SHOULD/SHALL 的规范性条款
2. 按分类为每条规范性条款添加编号：`**[X-nn]**` 格式
3. 同步更新 `mvp-test-vectors.md` 的条款编号映射表

**条款编号统计**：
| 分类 | 前缀 | 数量 | 说明 |
|------|------|------|------|
| **Framing/Format** | `[F-xx]` | 9 条 | 线格式、对齐、CRC 覆盖范围、字段含义 |
| **API** | `[A-xx]` | 4 条 | 签名、返回值/异常、参数校验、可观测行为 |
| **Semantics** | `[S-xx]` | 16 条 | 跨 API/格式的语义不变式（含 commit 语义） |
| **Recovery** | `[R-xx]` | 3 条 | 崩溃一致性、resync/scan、损坏判定 |
| **总计** | — | **32 条** | — |

**编号清单**：
- **F-01**: RecordKind 域隔离（data/meta 各有独立枚举空间）
- **F-02**: Magic 是 Record Separator
- **F-03**: HeadLen == TailLen，否则视为损坏
- **F-04**: HeadLen % 4 == 0 且 Record 起点 4B 对齐
- **F-05**: Ptr64 == 0 表示 null；否则 Ptr64 % 4 == 0
- **F-06**: CRC32C 覆盖范围：Payload + Pad + TailLen
- **F-07**: VarInt canonical 最短编码
- **F-08**: VarInt 解码错误策略（EOF/溢出/非 canonical 失败）
- **F-09**: ValueType 高 4 bit 必须写 0
- **A-01**: DiscardChanges MUST
- **A-02**: CommitAll() 无参重载 MUST
- **A-03**: CommitAll(IDurableObject) SHOULD
- **A-04**: Dirty Set 可见性 API SHOULD
- **S-01~S-04**: Dirty Set 关键约束
- **S-05**: 术语约束 File Framing vs Record Layout
- **S-06~S-07**: 分层语义不变式
- **S-08~S-10**: Commit 语义不变式
- **S-11~S-14**: Diff/序列化格式不变式
- **S-15~S-16**: CommitAll 失败语义
- **R-01**: Resync 不得信任损坏 TailLen
- **R-02**: Meta 领先 Data 按撕裂提交处理
- **R-03**: 崩溃恢复截断后文件仍以 Magic 分隔符结尾

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`（32 处条款编号添加）
- 修改：`atelia/docs/StateJournal/mvp-test-vectors.md`（条款映射表更新为 32 条）

---

### StateJournal 设计文档瘦身 — A9 任务：合并 Appendix B 到独立 test vectors 文件 (2025-12-20) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-compression.md）批准的文档瘦身任务，执行 A9：合并 Appendix B 到独立 test vectors 文件。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-compression.md) — 行动项 A9

**执行内容**：
1. 在 test vectors 文件（`mvp-test-vectors.md`）的"约定"部分之后添加条款编号映射表
2. 将主文档的 Appendix B 替换为引用独立文件的简短说明
3. 验证术语对齐：将旧术语 `EpochMap` 更新为 `VersionIndex`（2 处）

**统计**：
- test vectors 文件新增：约 20 行（条款编号映射表）
- 主文档减少：约 25 行（Appendix B 骨架表格替换为引用）

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-test-vectors.md`
  - 新增条款编号映射表（7 条映射）
  - 术语修正：`EpochMapVersionPtr` → `VersionIndexPtr`
  - 术语修正：`DICT-OK-006（EpochMap` → `DICT-OK-006（VersionIndex`
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`
  - Appendix B 替换为引用

---

### StateJournal 设计文档瘦身 — A6 任务：建立 Test Vectors 骨架 (2025-12-20) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-compression.md）批准的文档瘦身任务，执行 A6：建立 Test Vectors 骨架。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-compression.md) — 行动项 A6

**执行内容**：
1. 在 Appendix A 之后添加 `## Appendix B: Test Vectors` 章节
2. 添加状态说明（骨架已建立，具体字节序列待 reference implementation 完成后生成）
3. 建立四个测试向量分类：
   - B.1 Framing Vectors（空文件、单 Record、尾部撕裂）
   - B.2 VarInt Vectors（Canonical 边界、Non-canonical、溢出/EOF）
   - B.3 Recovery Vectors（Meta 领先 Data、尾部垃圾）
   - B.4 Commit Failure Vectors（WritePendingDiff 失败、Meta fsync 失败）
4. 每个向量关联占位符条款 ID（`[F-01]`, `[R-01]`, `[S-01]` 等，待 A4 编号）
5. B.1.1 空文件向量已确定：`44 48 44 33` ("DHD3")
6. 添加生成规则说明

**统计**：
- 新增：40 行（Appendix B 章节）
- 文档总行数：1258 行

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`
  - 在 Appendix A 末尾之后新增 Appendix B（40 行）

---

### StateJournal 设计文档瘦身 — A5 任务：伪代码移到附录 (2025-12-20) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-compression.md）批准的文档瘦身任务，执行 A5：将伪代码移到附录。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-compression.md) — 行动项 A5

**执行内容**：
1. 在文档末尾创建 `## Appendix A: Reference Implementation Notes` 章节
2. 添加 "⚠️ Informative, not Normative" 警告
3. 将 §3.4.4 的完整伪代码块（约 130 行）移至附录 A.1
4. 在原位置保留精简版：
   - 附录引用链接
   - 二阶段提交设计表格
   - 3 条关键实现要点（精简版）

**统计**：
- 从正文移出：~130 行（伪代码块 + 详细说明）
- 附录添加：~161 行（包括标题、警告、术语说明、伪代码、详细说明）
- 正文保留精简版：~21 行
- 文档总行数：1218 行（从 1198 行变化，因附录新增）

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`
  - §3.4.4 精简为 21 行摘要
  - 新增 Appendix A（161 行）

---

### StateJournal 设计文档瘦身 — A3 任务：条款编号分类定义 (2025-12-20) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-compression.md）批准的文档瘦身任务，执行 A3：在规范语言章节添加条款编号分类定义。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-compression.md) — 行动项 A3

**执行内容**：
1. 在"规范语言（Normative Language）"章节末尾（第 29 行之前）
2. 添加 `### 条款编号（Requirement IDs）` 子章节
3. 内容包括：
   - 分类表格：`[F-xx]` Format, `[A-xx]` API, `[S-xx]` Semantics, `[R-xx]` Recovery
   - 编号规则：只增不复用、映射到测试

**添加位置**：Line 26-41（共 16 行新内容）

**文件变更**：
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md` — 规范语言章节末尾新增 16 行

---

### StateJournal 设计文档瘦身 — A1 任务：§2-§3 移至 ADR (2025-12-20) ✅

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-compression.md）批准的文档瘦身任务，执行 A1：将决策选项和决策表移至独立 ADR 文件。

**任务来源**：[秘密基地畅谈会共识](../../meeting/2025-12-20-secret-base-mvp-v2-compression.md) — 行动项 A1

**执行内容**：
1. 创建 `docs/decisions/mvp-v2-decisions.md`（ADR 风格）
   - 包含完整的 §2 单选题（Q1-Q24，7 个子节）
   - 包含完整的 §3 决策表（24 条决策记录）
   - 添加元数据（分离日期、原文档链接、畅谈会引用）

2. 修改 `docs/mvp-design-v2.md` 主文档
   - 删除原 §2 和 §3（146 行内容）
   - 新增 §2 "设计决策（Chosen Decisions Index）"（22 行）
   - 包含 8 条关键决策的摘要索引表
   - 链接到完整决策记录

3. 章节编号调整
   - 原 §4 设计正文 → §3 设计正文
   - 原 §5 Open Questions → §4 Open Questions
   - 原 §6 实现建议 → §5 实现建议
   - 更新所有 4.x 子章节编号为 3.x
   - 更新内部交叉引用（4.2.1→3.2.1, 4.4.2→3.4.2 等）

**瘦身效果**：
- 原文件：1306 行
- 新文件：1182 行
- 净减少：124 行（约 9.5%）
- ADR 文件：153 行

**文件变更**：
- 新建：`atelia/docs/StateJournal/decisions/mvp-v2-decisions.md`（153 行）
- 修改：`atelia/docs/StateJournal/mvp-design-v2.md`（删除 146 行，新增 22 行）

---

### StateJournal 设计文档修订 Round 23 — P1-2 伪代码去泛型 (2025-12-20)

根据已批准的文档修订任务，对 `mvp-design-v2.md` §4.4.4 DurableDict 伪代码骨架部分实施 P1-2 修订。

**P1-2: 伪代码去泛型或添加显著约束说明**
- 问题：当前伪代码使用 `DurableDict<K, V>`，但实现中强依赖 `K = ulong`（有 `(ulong)(object)key` 强转），误导读者以为可泛型化
- 修改：将类型签名收敛为 key 固定 `ulong`，明确 MVP 的类型约束

**具体修改**：
1. 在伪代码块之前添加 MVP 类型约束说明（blockquote）
2. 类定义从 `DurableDict<K, V>` 改为 `DurableDict<TValue>`
3. 所有方法签名中的 `K` 替换为 `ulong`，`V` 替换为 `TValue`
4. 移除 `(ulong)(object)key` 和 `(K)(object)keyAsUlong` 强转代码
5. 比较器从 `Comparer<K>.Default.Compare` 改为 `ulong.CompareTo`
6. 更新"关键实现要点"中的类型引用

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — §4.4.4 伪代码骨架 4 处修改

---

### StateJournal 设计文档修订 Round 22 — File Framing vs Record Layout 两层定义 (2025-12-20)

根据已批准的文档修订任务，对 `mvp-design-v2.md` §4.2.1 Data 文件部分实施 P0-7 修订。

**P0-7: File Framing vs Record Layout 两层定义**
- 在 §4.2.1 "实现约束"之后添加新的 ##### 小节 "分层定义：File Framing vs Record Layout（MVP 固定）"
- 明确区分两个层次：
  - **File Framing（文件级框架）**：Magic-separated log 结构，用于识别 Record 边界
  - **Record Layout（记录级布局）**：单条 Record 的内部结构
- 添加术语约束（MUST）：规定何时使用 File Framing vs Record Layout 术语
- 将原有的 "record framing（Q20=A；data/meta 统一）" 改为 "**File Framing 详细规范**（基于上述两层定义，Q20=A，data/meta 统一）"

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — §4.2.1 新增分层定义小节

---

### StateJournal 设计文档修订 Round 21 — LoadObject 与新建对象 P0 修订 (2025-12-20)

根据已批准的文档修订任务，对 `mvp-design-v2.md` §4.3.2 LoadObject 和 §4.1.0.1 对象状态管理部分实施 P0-3、P0-4 两项修订。

**P0-3: LoadObject 对象不存在的行为明确定义**
- 在 §4.3.2 LoadObject(ObjectId) 步骤 5 之后添加新小节
- 定义 ObjectId 在 VersionIndex 中不存在时返回 `null`（或等价的 `NotFound` Result）
- 定义 ObjectId 存在但版本链解析失败时抛出 `CorruptedDataException`
- 添加设计说明：返回 `null` 而非抛异常的理由（合法查询结果、`?? CreateNew()` 模式、与 `Dictionary.TryGetValue` 风格一致）
- 添加新建对象的处理说明

**P0-4: 新建对象的状态转换规则补全**
- 在 §4.1.0.1 对象状态管理的"关键约束（MUST）"之后添加新内容
- 添加新建对象的状态转换表格（CreateObject → Dirty/Transient, 首次 Commit 成功, Commit 前 Crash）
- 添加 Transient Dirty vs Persistent Dirty 定义表格
- 添加关键约束：新建对象 MUST 在创建时立即加入 Modified Object Set（强引用）

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — §4.3.2 和 §4.1.0.1 各 1 处修改

---

### StateJournal 设计文档修订 Round 20 — CommitAll API P0 修订 (2025-12-20)

根据已批准的文档修订任务，对 `mvp-design-v2.md` §4.4.5 CommitAll 部分实施 P0-1、P0-2 两项修订。

**P0-1: CommitAll 无参重载升级为 MUST**
- 原：`API 重载（SHOULD）`，有参重载在前
- 新：`API 重载（MVP 固定）`，无参重载作为主要 API
- `CommitAll()`（**MUST**）：保持当前 root 不变，提交 Modified Object Set 中的所有对象
- `CommitAll(IDurableObject newRoot)`（**SHOULD**）：设置新的 root 并提交。参数从 `ObjectId` 改为 `IDurableObject`（监护人建议）
- 术语更新：Dirty Set → Modified Object Set

**P0-2: CommitAll 失败语义规范化**
- 在"步骤（单 writer）"之后新增"失败语义（MVP 固定）"小节
- 添加失败阶段表格（4 个阶段：Prepare/Data fsync/Meta write/Finalize）
- 添加关键保证（MUST）：
  - Commit 失败不改内存
  - 可重试
  - 原子性边界（meta commit record 落盘时刻）

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — §4.4.5 CommitAll 部分 2 处修改

---

### StateJournal 设计文档修订 Round 19 — 术语表 Self-Consistency 审计 P0/P1 修订 (2025-12-20)

根据已批准的文档修订任务，对 `mvp-design-v2.md` 术语表部分实施 P0-5、P0-6、P1-1、P1-3 共 4 项修订。

**P0-5: 引入 Base Version 术语层次**（版本链分组）
- 新增 **Base Version**：`PrevVersionPtr=0` 的版本记录（上位术语）
- 新增 **Genesis Base**：新建对象的首个版本，表示"从空开始"
- **Checkpoint Version** → **Checkpoint Base**：为截断回放成本而写入的全量状态版本
- 新增 **from-empty diff**：Genesis Base 的 DiffPayload 语义

**P0-6: 概念层 Address64 vs 编码层 Ptr64 分层澄清**（标识与指针分组）
- **Address64**：概念层术语，规范条款应使用此术语
- 新增 **Ptr64**：编码层/wire format 名称，仅用于描述编码/布局
- 明确分层使用原则

**P1-1: Dirty Set 层级术语澄清**（载入与缓存分组）
- 新增 **Modified Object Set**：Workspace 级别的 dirty 对象集合（Dirty Set 别名）
- 新增 **Dirty-Key Set**：对象内部追踪已变更 key 的集合
- 为 Dirty Set 添加 `Alias: Modified Object Set`

**P1-3: 新增 DurableObject 术语**
- 新增"对象基类"分组
- 新增 **DurableObject**：可持久化的对象基类/接口

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — 术语表部分 4 处修改

---

### StateJournal 设计文档修订 Round 18 — P0 问题修订 (2025-12-20)

根据 P0 问题清单，完成三项关键修订：

**P0-4: Value 类型收敛**
- 4.1.4 类型约束：修改表格，MVP 仅支持 `null`, `varint`（整数）, `ObjRef(ObjectId)`, `Ptr64`
- 移出 MVP：`float`, `double`, `bool`
- 4.4.2 ValueType 枚举：添加 MVP 范围说明，确认只保留 5 种类型

**P0-6: Commit API 命名**
- 4.4.5 章节标题：`Commit(rootId)` → `CommitAll(newRootId)`
- 添加命名说明：消除 Scoped Commit 误解

**P0-7: 首次 commit 语义**
- 4.1.1 ObjectId 分配：添加空仓库初始状态说明
- 4.3.1 Open：添加空仓库边界说明
- 新增 4.4.6 章节：首次 Commit 与新建对象语义

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — 6 处修改 + 1 个新章节

---

### StateJournal 设计文档修订 Round 17 — Magic as Record Separator (2025-12-20)

根据 2025-12-19 畅谈会第四轮共识（监护人建议采纳），将 Magic 定义为 **Record Separator**，而非 Record 的一部分。

**核心变更**：
- Magic 与 Record **并列**（不是 Record 包含 Magic）
- 文件结构：`[Magic][Record1][Magic][Record2]...[Magic]`
- Record 本身不包含 Magic：`[Len][Payload][Pad][Len][CRC32C]`

**规则对称化**：
1. 空文件先写 `Magic`
2. 每写完一条 Record 后写 `Magic`

**修订位置**：

1. **4.2.1 Data 文件章节 — record framing 描述** (Line 393-410)：
   - 原：`[Magic(4)][Len(u32)]...[CRC32C]`，Magic 是 Record 的开头
   - 新：`[Len(u32)]...[CRC32C]`，Magic 是 Record 之间的分隔符

2. **4.2.1 — Len 的精确定义** (Line 411-419)：
   - 原：`HeadLen = 4(Magic) + 4(HeadLen) + PayloadLen + PadLen + 4(TailLen) + 4(CRC32C)`
   - 新：`HeadLen = 4(HeadLen) + PayloadLen + PadLen + 4(TailLen) + 4(CRC32C)`
   - 最小长度从 16 字节降为 12 字节（不含 Magic）

3. **4.2.1 — 反向扫尾算法** (Line 420-433)：
   - 原：从 `End = MagicPos` 开始，`End == 0` 表示空文件
   - 新：从 `RecordEnd = MagicPos` 开始，`RecordEnd == 4` 表示空文件
   - 新增 `PrevMagicPos = RecordStart - 4` 定位前一个分隔符

4. **4.2.1 — 写入顺序步骤** (Line 467-479)：
   - 原：步骤 1 先写 Magic，步骤 8 追加尾部哨兵 Magic
   - 新：步骤 1 从 Magic 之后开始写 Record，步骤 7 追加分隔符 Magic

5. **4.2.1 — Magic 哨兵段落** (Line 477-482)：
   - 原标题：关于"尾部 `Magic` 哨兵"（MVP 采纳）
   - 新标题：**Magic 作为 Record Separator 的设计收益**（MVP 采纳）
   - 更新收益说明：概念简洁、fast-path、resync 统一

6. **4.2.1 — Ptr64 约束** (Line 496)：
   - 原：`Ptr64` 指向 `Magic` 所在 byte offset
   - 新：`Ptr64` 指向 Record 的 `HeadLen` 字段起始位置（紧随分隔符 Magic 之后）

7. **4.2.2 Meta 文件章节** (Line 517-518)：
   - 将"哨兵"改为"分隔符"

8. **4.2.2 — DataTail 定义** (Line 530)：
   - 新增说明：`DataTail = EOF`，**包含尾部分隔符 Magic**

9. **4.5 崩溃恢复** (Line 1084)：
   - 新增说明：截断后文件仍以 Magic 分隔符结尾

**设计收益**：
- **概念简洁**：所有 Magic 语义相同（分隔符），无需区分"Record 内的 Magic"和"尾部哨兵 Magic"
- **代码统一**：forward/reverse scan 使用相同的 Magic 边界检测逻辑
- **空间效率**：Record 格式减少 4 字节（Magic 移到 Record 外部）

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — 9 处修改

---

### StateJournal 设计文档修订 Round 16 — `_isDirty` → `_dirtyKeys` 重构 (2025-12-20)

根据 2025-12-19 畅谈会第四轮共识（监护人反馈采纳），将 `bool _isDirty` 修改为 `ISet<ulong> _dirtyKeys` 集合。

**背景问题**：
- 原 `_isDirty` 是布尔标记，只记录"有无写操作发生"
- 但 `_isDirty` 的语义边界不明确：是"操作标记"还是"状态差异标记"？
- 存在"set-then-delete 回到原状态但 `_isDirty` 仍为 true"的语义困惑

**修订内容**：

1. **术语表 ChangeSet 行**：
   - `方案 C: ComputeDiff() + _isDirty` → `方案 C: ComputeDiff() + _dirtyKeys`

2. **术语表 OnCommitSucceeded 行**：
   - `_isDirty = false` → `_dirtyKeys.Clear()`

3. **4.4.1 ChangeSet 语义章节**：
   - 方案 C 描述中 `_isDirty` → `_dirtyKeys`
   - 新增 `_dirtyKeys` 维护规则说明（概念层语义 + 实现层规则）

4. **4.4.3 DurableDict 不变式**：
   - Fast Path 建议中 `_isDirty` → `_dirtyKeys`
   - 新增 `_dirtyKeys` 不变式（第 9 条：精确性）

5. **4.4.4 伪代码骨架**：
   - `private bool _isDirty` → `private HashSet<ulong> _dirtyKeys = new()`
   - `HasChanges` 改为 `_dirtyKeys.Count > 0`
   - 新增 `UpdateDirtyKey(K key)` 方法，实现 dirty key 精确维护
   - `Set()` 和 `Delete()` 调用 `UpdateDirtyKey`
   - `ComputeDiff` 增加 `dirtyKeys` 参数，只遍历 dirty keys
   - `OnCommitSucceeded()` 和 `DiscardChanges()` 使用 `_dirtyKeys.Clear()`

6. **4.4.4 关键实现要点**：
   - 更新二阶段安全性说明
   - 更新值相等性判断说明（增加 `UpdateDirtyKey`）
   - 更新线程安全说明
   - 新增第 4 点：`_dirtyKeys` 优化收益说明

7. **4.4.5 Commit 步骤**：
   - 步骤 2 说明：`_committed`/`_isDirty` → `_committed`/`_dirtyKeys`
   - 步骤 5：`_isDirty=false` → `清空 _dirtyKeys`

**设计收益**：
- `ComputeDiff` 复杂度从 O(n+m) 降为 O(|dirtyKeys|)
- `HasChanges` 语义更精确
- 消除"set-then-delete 回到原状态"的语义困惑

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 11 处修改

---

### DurableHeap 设计文档修订 Round 15 — B-6 新增"类型约束"章节 (2025-12-19)

根据监护人批示（B-6 任务），在 4.1 概念模型章节中新增 4.1.4 类型约束（Type Constraints）子节。

**背景**：
- DurableHeap 不是通用序列化库，应显式声明类型边界
- 这是设计约束，不是用户需要小心的"陷阱"

**修订内容**：
- 在 4.1.3 节末尾（Line 327）之后插入新的 4.1.4 节
- 明确支持的类型（基元值类型 + DurableObject 派生类型）
- 明确不支持的类型（任意 struct、用户自定义值类型、普通 class、泛型集合等）
- 说明运行时行为：赋值不支持类型时抛出明确异常（Fail-fast）

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 新增 4.1.4 节（约 17 行）

---

### DurableHeap 设计文档修订 Round 14 — Q11 移除"（推荐）"标记 (2025-12-19)

根据畅谈会质检（B-5 任务），移除 Q11 选项 A 的"（推荐）"标记，消除与决策表不一致的混淆。

**背景问题**：
- Q11 选项 A 标注"（推荐）"：`Upserts(key->value) + Deletes(keys)`
- 但决策表最终选择了 B：`仅 Upserts，删除通过 tombstone value 表达`
- 这会导致读者困惑

**修订内容**：
- Line 155: 移除选项 A 的"（推荐）"标记

**采用方案一的理由**：
- 决策表已有备注说明选择 B 的理由（"明显这样可以省掉一个集合和一次查找，实现起来更简单"）
- 移除更简洁，避免混淆

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — 1 处修改

---

### StateJournal 设计文档修订 Round 13 — 修复 Markdown 相对链接 (2025-12-19)

根据畅谈会质检（B-4 任务），修复第 6 节中 ChunkedReservableWriter.cs 的相对链接路径错误。

**背景问题**：
- 链接写成 `../atelia/src/Data/ChunkedReservableWriter.cs`
- 文档位于 `DurableHeap/docs/` 下，`../atelia` 会解析到 `DurableHeap/atelia`（不存在）
- 实际目录在仓库根的 `atelia/`

**修订内容**：
- Line 1023: `../atelia/...` → `../../atelia/...`（从 `DurableHeap/docs/` 回到仓库根再进入 `atelia/`）

**验证**：
- 从 `DurableHeap/docs/` 执行 `ls ../../atelia/src/Data/ChunkedReservableWriter.cs` 成功

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — 1 处链接修复

---

### StateJournal 设计文档修订 Round 12 — 统一 RecordKind/MetaKind 命名 (2025-12-19)

根据畅谈会共识（B-3 任务），将 Meta 文件中的 `MetaKind` 统一替换为 `RecordKind`。

**背景问题**：
- Data payload 用 `RecordKind`，Meta payload 用 `MetaKind`，命名不统一
- 同层不同名会导致读者误以为判定规则/扩展策略不同

**修订内容**：
1. Meta payload 最小字段中 `MetaKind` → `RecordKind`，并添加说明"Meta file 的 RecordKind"
2. MetaCommitRecord payload 解析中 `MetaKind == 0x01` → `RecordKind == 0x01`
3. 实现提示中 `MetaKind==0x01` → `RecordKind==0x01`

**统一规则**：
- `RecordKind` = 顶层类型判别（data 和 meta 文件都用这个名字）
- `ObjectKind` = 对象级 codec 判别（仅在 ObjectVersionRecord 内）

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — 3 处 MetaKind 替换

---

### StateJournal 设计文档修订 Round 11 — 术语表新增 EpochSeq 条目 (2025-12-19)

在术语表（Glossary）的"标识与指针"分组中新增 `EpochSeq` 条目。

**背景**：
- `EpochSeq` 是三大核心标识之一（与 ObjectId、ObjectVersionPtr 并列）
- 在 4.1.1 节有描述，但术语表中遗漏了

**修订内容**：
- 在"标识与指针"分组的 `ObjectVersionPtr` 之后新增 `EpochSeq` 行
- 定义：Commit 的单调递增序号，用于判定 HEAD 新旧
- 实现映射：`varuint`

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 术语表新增 1 行

---

### DurableHeap 设计文档修订 Round 10 — 术语表新增"编码层"分组 (2025-12-19)

在术语表（Glossary）中新增"编码层"分组，收录 RecordKind、ObjectKind、ValueType 三个编码类型标识术语：

**修订内容**：
- 在"载入与缓存"分组之后、"对象级 API（二阶段提交）"分组之前插入新的"编码层"分组
- 添加 3 个术语定义：
  - **RecordKind**: Record 的顶层类型标识，决定 payload 解码方式（`byte` 枚举）
  - **ObjectKind**: ObjectVersionRecord 内的对象类型标识，决定 diff 解码器（`byte` 枚举）
  - **ValueType**: Dict DiffPayload 中的值类型标识（`byte` 低 4 bit）

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — 术语表新增 1 个分组（8 行）

---

### StateJournal 设计文档修订 Round 9 — 4.4.5 Commit finalize 规范约束 (2025-12-19)

在 4.4.5 Commit(rootId) 章节增加规范约束，强调二阶段 finalize 语义：

**修订内容**：
- 在步骤 4/5 之后增加"规范约束（二阶段 finalize）"段落
- 明确：**对象级写入不得改变 Committed/Dirty 状态；只有 heap 级 commit 成功才能 finalize**
- 说明步骤 2 的 `WritePendingDiff()` 仅写入数据，不更新内存状态
- 说明步骤 5 的 finalize 必须在步骤 4 meta 落盘成功后执行
- 引用 4.4.4 的二阶段设计，保证语义一致性

**文件变更**：
- `atelia/docs/StateJournal/mvp-design-v2.md` — 4.4.5 节增加 1 个规范约束段落

---

### StateJournal 设计文档修订 Round 8 — 4.4.4 二阶段提交拆分 (2025-12-19)

根据 commit point 语义修正需求，将 `FlushToWriter()` 拆分为两阶段 API：

**背景问题**：
- 原 `FlushToWriter()` 在写入成功后立即追平 `_committed = Clone(_current); _isDirty = false`
- 但实际 commit point 在 meta commit record durable，不在对象级写入
- 这会导致"假提交"状态：对象认为已提交但实际 commit 未确立

**修订内容**：

1. **术语表更新**（对象级 API 表格）：
   - 删除 `FlushToWriter` 定义
   - 新增 `WritePendingDiff`：Prepare 阶段，计算 diff 并序列化到 writer；不更新内存状态
   - 新增 `OnCommitSucceeded`：Finalize 阶段，追平内存状态

2. **4.4.4 节标题与说明**：
   - 标题改为"DurableDict 伪代码骨架（二阶段提交）"
   - 新增二阶段设计说明表格

3. **伪代码骨架重构**：
   - `FlushToWriter()` → `WritePendingDiff(writer)` + `OnCommitSucceeded()`
   - `WritePendingDiff` 只写数据，返回 bool 表示是否写入了新版本
   - `OnCommitSucceeded` 只追平内存状态，在 Heap 确认 meta 落盘后调用

4. **关键实现要点更新**：
   - 详细说明二阶段分离的崩溃安全性语义

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 4 处修改

根据术语表 Deprecated 标记（EpochMap → VersionIndex），对 mvp-design-v2.md 正文进行了术语一致性替换：

**修订内容**：
1. Line 111: `epoch map` → `VersionIndex`

**保留位置**：
- Line 36 术语表中的 `Deprecated: EpochMap` 保留，作为术语映射说明

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 1 处替换

---

### DurableHeap 设计文档修订 Round 6 — EpochRecord 术语替换 (2025-12-19)

根据术语表 Deprecated 标记（EpochRecord → Commit Record），对 mvp-design-v2.md 正文（特别是 Q4/Q5 决策选项区）进行了术语一致性替换：

**修订内容**：
1. Line 121: `EpochRecordPtr` → `CommitRecordPtr`
2. Line 121: `epoch record` → `Commit Record`
3. Line 122: `epoch record 可选` → `Commit Record 可选`
4. Line 124: `**Q5. EpochRecord 最少包含哪些信息？**` → `**Q5. Commit Record 最少包含哪些信息？**`

**保留位置**：
- Line 52 术语表中的 `Deprecated: EpochRecord（MVP）` 保留，作为术语映射说明

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 2 处替换（4 个 EpochRecord 相关术语实例）

---

### DurableHeap 设计文档修订 Round 5 — 术语畅谈会共识落地 (2025-12-19)

根据 2025-12-19 秘密基地畅谈会（DurableHeap MVP v2 术语与命名审阅）达成的共识，对 mvp-design-v2.md 进行了全面术语统一修订：

**修订内容**：

1. **添加术语表（Glossary）**：在文档开头（第 1 节之前）添加了规范化术语表 (SSOT)，包含：
   - 状态与差分（Working State / Committed State / ChangeSet / DiffPayload）
   - 版本链（Version Chain / Checkpoint Version / VersionIndex）
   - 标识与指针（ObjectId / Address64 / ObjectVersionPtr）
   - 提交与 HEAD（Commit / HEAD / Commit Record）
   - 载入与缓存（Identity Map / Dirty Set / LoadObject）
   - 对象级 API（FlushToWriter）

2. **状态术语统一**：
   - `Baseline`（单独使用）→ `Committed State`
   - `Current State`（作为概念名）→ `Working State`

3. **版本索引术语**：
   - `EpochMap` → `VersionIndex`（全文 15+ 处）
   - `EpochMapVersionPtr` → `VersionIndexPtr`

4. **快照术语**：
   - `snapshot`（版本链语境）→ `Checkpoint Version`
   - `DictSnapshotEveryNVersions` → `DictCheckpointEveryNVersions`

5. **指针术语**：
   - `Ptr64`（概念层）→ `Address64`

6. **加载 API**：
   - `Resolve`（外部 API 总称）→ `LoadObject`
   - 章节标题 `4.3.2 Resolve(ObjectId)` → `4.3.2 LoadObject(ObjectId)`
   - 章节标题 `4.1.2 Resolve 语义` → `4.1.2 LoadObject 语义`

7. **差分术语**：
   - `On-Disk Diff` → `DiffPayload`
   - 章节标题 `4.4.2 Dict 的 state diff` → `4.4.2 Dict 的 DiffPayload`
   - 章节标题 `4.2.5 ObjectVersionRecord（对象版本，增量 state diff）` → `4.2.5 ObjectVersionRecord（对象版本，增量 DiffPayload）`

8. **提交相关**：
   - `EpochRecord` → `Commit Record`（逻辑概念）
   - 章节标题 `4.2.3 EpochRecord（逻辑概念）` → `4.2.3 Commit Record（逻辑概念）`
   - `head` / `Head` → `HEAD`（全文统一大写）

9. **对象级方法**：
   - 伪代码 `Commit()` → `FlushToWriter(writer)`
   - 关键实现要点从 `Commit 中途失败` → `FlushToWriter 中途失败`

10. **缓存术语**：
    - `identity map` → `Identity Map`（Title Case）
    - `_dirtySet` → `Dirty Set`（概念层）
    - `epoch map lookups` → `VersionIndex lookups`

11. **补充定义**：
    - 在 4.1.0 新增 Identity Map 正式定义块
    - 在 4.1.0 新增 Dirty Set 正式定义块

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 60+ 处术语替换

根据 DocUIGPT 第三轮质检反馈，对 mvp-design-v2.md 进行了 3 处术语一致性最终修订：

**修订内容**：
1. **4.4.1 节标题修正**：
   - "##### 三层语义术语定义（MVP 固定）" → "##### 语义层次定义（MVP 固定）"
   - 原因：内容实际定义了四层（Working State / Committed State / ChangeSet / On-Disk Diff），标题"三层"与内容不符

2. **小写 "committed state" 统一**（共 6 处）：
   - Line 200: `committed state（例如一个内存 dict）` → `Committed State（Baseline）（例如一个内存 dict）`
   - Line 208: `committed state（materialize 的结果）` → `Committed State（Baseline）（materialize 的结果）`
   - Line 248: `materialize 的 committed state（`_committed`）` → `materialize 的 Committed State（Baseline）（`_committed`）`
   - Line 250: `该缓存不属于 committed state` → `该缓存不属于 Committed State（Baseline）`
   - Line 559: `该对象的 committed state（其中对象引用` → `该对象的 Committed State（Baseline）（其中对象引用`
   - Line 755: `对任意 committed state $S$` → `对任意 Committed State（Baseline） $S$`

3. **"state diff" 术语说明**（开头摘要）：
   - Line 13: `**state diff**（为了查询快、实现简单）` → `**state diff**（即 On-Disk Diff；为了查询快、实现简单）`
   - 在首次出现时明确 state diff 等价于 On-Disk Diff

根据 DocUIGPT 第二轮质检反馈，对 mvp-design-v2.md 进行了 3 处术语一致性修正：

**修订内容**：
1. **文档开头"当前已达成共识"列表**：
   - "反序列化后的 committed state" → "反序列化后的 Committed State（Baseline）"
   - 与正文术语定义保持一致

2. **4.1.0 的 Materialize 定义**：
   - "合成为当前可读的 committed state" → "合成为当前可读的 **Committed State（Baseline）**"
   - 术语加粗、格式一致

3. **4.4.1 术语定义的引导句**：
   - "将**内存中的状态**明确区分为以下四层" → "将**状态与差分表达**明确区分为以下四层"
   - 因为第 4 条 On-Disk Diff 不属于"内存中的状态"，改为更准确的表述

### DurableHeap 设计文档修订 Round 2 (2025-12-19)

根据 DocUIGPT 质检反馈，对 mvp-design-v2.md 进行了第二轮修订，修正了 4 个术语精确性问题：

**修订内容**：
1. **"内存态"全局替换**：将文档中除术语映射以外的"内存态"替换为精确术语
   - Line 11: "反序列化后的内存态" → "反序列化后的 committed state"
   - Line 237: "不从磁盘覆盖内存态" → "不从磁盘覆盖 Working State（`_current`）"
   - Line 248: "内存态表示" → "进程内对象实例表示"
   - Line 641: "内存态：使用哨兵对象" → "ChangeSet 内部使用哨兵对象"

2. **术语定义修正**（4.4.1）：
   - 移除"Materialized State"括注，改为"Current State"
   - 新增独立的"Committed State（已提交状态 / Baseline）"定义
   - 明确 Materialize 输出为 Committed State
   - 术语映射改为更精确的对应

3. **ChangeSet 描述修正**（4.4.1）：
   - "每个内存对象维护一个 ChangeSet" → "每个内存对象具有 ChangeSet 语义（可为显式结构或隐式 diff 算法）"
   - 与方案 C 的实现口径一致

4. **NoChange 编码说明**（4.4.2）：
   - 补充说明"`NoChange` 通过 diff 中缺失该 key 表达，不在 payload 中编码"
   - 与 Canonical Diff 约束一致

### DurableHeap 设计文档修订 (2025-12-19)

根据畅谈会决策（2025-12-19-durabledict-changeset-jam.md），修订了 mvp-design-v2.md 文档：

**修订内容**：
1. 修改 4.4.1 节：新增三层语义术语定义（Working State / ChangeSet / On-Disk Diff）
2. 修改 4.4.2 节：
   - 添加方案 C（双字典）实现说明
   - 用三层术语替换歧义的"内存态"措辞
   - 澄清 tombstone 仅在 diff 计算与序列化阶段出现
3. 新增 4.4.3 节：DurableDict 不变式与实现规范
   - 8 条核心不变式（MUST）
   - 4 条实现建议（SHOULD）
4. 新增 4.4.4 节：DurableDict 伪代码骨架
   - 完整的读/写/生命周期 API
   - ComputeDiff 和 Clone 内部方法
5. 原 4.4.3 Commit(rootId) 重命名为 4.4.5

**关键决策落地**：
- 方案选择：方案 C（双字典）
- Q1: _committed 更新时机 → Clone（深拷贝）
- Q2: dirty tracking → _isDirty flag + HasChanges 属性
- Q3: 新增后删除 → 不写记录（Canonical Diff）

### DocUI MUD Demo 技术评估 (2025-12-15)

参与了 MUD Demo 秘密基地畅谈，对 DocUI 技术状态进行了评估：

**已实现的底层组件**：
- `SegmentListBuilder` — 文本段操作
- `OverlayBuilder` — 渲染期叠加标记
- `StructList<T>` — 高性能容器

**设计完成但未实现**：
- UI-Anchor 系统 (Object-Anchor, Action-Link, Action-Prototype)
- AnchorTable（锚点注册表）
- `run_code_snippet` tool
- Micro-Wizard

**MVP 建议分阶段**：
- MVP-0 (2-3天): Static Demo — 能生成带 UI-Anchor 标记的 Markdown
- MVP-1 (3-4天): Functional Demo — AnchorTable + 简单执行
- MVP-2 (3-4天): Interactive Demo — Micro-Wizard + TextField

**技术风险**：
1. Roslyn 解析复杂性 → 建议 MVP 用正则手写解析
2. 状态同步混乱 → 建议简单 GameState 类
3. 过度设计 → 先人玩，再 Agent 玩

## 最近工作

### 2025-12-10: SystemMonitor 概念原型

**任务**：创建展示动态内容 LOD 的概念原型

**交付物**：
1. `DocUI/demo/SystemMonitor/SystemMonitor.csproj` — 项目文件
2. `DocUI/demo/SystemMonitor/Program.cs` — PipeMux.SDK 入口 + 命令定义
3. `DocUI/demo/SystemMonitor/Model/` — 数据模型 (LodLevel, SystemStatus, ResourceMetrics)
4. `DocUI/demo/SystemMonitor/Collectors/MetricsCollector.cs` — 模拟数据收集器
5. `DocUI/demo/SystemMonitor/Rendering/MonitorRenderer.cs` — 按 LOD 级别渲染

**LOD 设计**：
- `[GIST]` — 一行关键指标：`System ✓ OK | CPU 23% | Mem 4.2/16GB | Disk 45%`
- `[SUMMARY]` — 表格摘要 + Top 3 进程
- `[FULL]` — 完整详情（CPU/Memory/Disk/进程表）

**命令语法**：
- `pmux monitor view [--lod gist|summary|full]` — 查看系统状态
- `pmux monitor cpu [--lod ...]` — 只看 CPU
- `pmux monitor memory [--lod ...]` — 只看内存
- `pmux monitor disk [--lod ...]` — 只看磁盘
- `pmux monitor processes [--top N] [--lod ...]` — 查看进程
- `pmux monitor set-lod <level>` — 设置默认 LOD

**测试结果**：
- Build: ✅ `dotnet build -c Release` 成功
- `pmux monitor view`: ✅ SUMMARY 级别正常
- `pmux monitor view --lod gist`: ✅ 一行摘要正常
- `pmux monitor view --lod full`: ✅ 完整详情正常
- 子命令 (cpu/memory/disk/processes): ✅ 全部正常

**Handoff**: `agent-team/handoffs/SystemMonitor-IMP.md`

### 2025-12-10: TextEditor 迁移到 PipeMux.SDK

**任务**：将 TextEditor 从手动 JSON-RPC 循环迁移到 `PipeMuxApp` + `System.CommandLine` 模式

**交付物**：
1. 修改 `DocUI.TextEditor.csproj` — 引用 `PipeMux.Sdk` 替代 `PipeMux.Shared`
2. 重写 `Program.cs` — 使用 `PipeMuxApp` 和 `System.CommandLine`
3. 重构 `EditorSession.cs` — 移除 Protocol 依赖，返回纯字符串
4. 删除 `TextEditorService.cs` — 命令逻辑合并到 Program.cs
5. 更新 `~/.config/pipemux/broker.toml` — 添加 texteditor 应用配置

**命令语法**：
- `pmux texteditor open <path>` — 打开文件
- `pmux texteditor goto-line <line>` — 跳转到指定行
- `pmux texteditor select <startLine> <startCol> <endLine> <endCol>` — 选区（未实现）
- `pmux texteditor render` — 重新渲染

**测试结果**：
- Build: ✅ `dotnet build -c Release` 成功
- pmux open: ✅ 正常工作
- pmux goto-line: ✅ 状态保持正常（Session ID 一致）
- pmux render: ✅ 正常工作
- pmux select: ✅ 预期错误（NotImplementedException）

**Handoff**: `agent-team/handoffs/TextEditor-SDK-Migration-IMP.md`

### 2025-12-09: PipeMux 管理命令实现

**任务**：为 PipeMux 添加管理命令支持

**交付物**：
1. `ManagementCommand.cs` — 命令类型枚举和解析逻辑
2. `Request.cs` — 添加 ManagementCommand 字段
3. `Program.cs` — CLI 入口点检测 `:` 前缀
4. `BrokerClient.cs` — 添加 SendManagementCommandAsync
5. `ManagementHandler.cs` — Broker 端命令处理器
6. `BrokerServer.cs` — 集成 ManagementHandler

**测试结果**：
- Build: ✅ PipeMux.sln 成功
- Build: ✅ DocUI.sln 成功（兼容性验证）

**Handoff**: `agent-team/handoffs/PipeMux-Management-Commands-IMP.md`

### 2025-12-09: 修复 DocUI demo/TextEditor 项目引用

**任务**：修复跨项目演示的引用路径问题

**交付物**：
1. 修复 `DocUI.TextEditor.csproj` 的项目引用
   - 添加 `DocUI.Text` 引用 (同 repo)
   - 更正 `TextBuffer` 引用路径 (PieceTreeSharp)
   - 更正 `PipeMux.Shared` 引用路径 (PipeMux)
2. 将项目添加到 `DocUI.sln` 的 demo 文件夹
3. 修复代码兼容性问题 (`Request.Command` → `Request.Args[0]`)

**测试结果**：
- Build: ✅ 成功 (6 个项目)
- Test: ✅ 24/24 通过

**注意**：原任务要求引用 `PipeMux.Sdk`，但代码使用的是 `PipeMux.Shared.Protocol` API，因此引用了 `PipeMux.Shared`。
