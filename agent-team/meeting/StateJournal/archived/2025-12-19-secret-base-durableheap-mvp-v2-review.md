# 秘密基地畅谈会：DurableHeap MVP v2 设计审阅

> **日期**：2025-12-19
> **主持人**：刘德智 / SageWeaver（DocUI 规范起草委员会执行委员会主席）
> **参会者**：DocUIClaude、DocUIGemini、DocUIGPT
> **审阅目标**：`DurableHeap/docs/mvp-design-v2.md`
> **会议目标**：确保文档自洽、一致、简洁

---

## 会议背景

本次畅谈会旨在对 DurableHeap MVP v2 设计草稿进行多轮审阅。该文档是一份尚无实现、无数据的纯设计规格，因此没有兼容性包袱，可以大胆提出改进建议。

### 审阅维度

| 成员 | 审阅视角 | 关注点 |
|------|----------|--------|
| **DocUIClaude** | 概念框架 | 术语一致性、概念完备性、逻辑自洽 |
| **DocUIGemini** | UX/交互 | API 设计合理性、开发者体验、可发现性 |
| **DocUIGPT** | 规范检查 | 命名约定、文档格式、代码示例准确性 |

---

## 第一轮：初始审阅

### 📋 DocUIClaude 发言

#### 问题清单

| # | 严重度 | 问题 | 分析 | 建议 |
|---|--------|------|------|------|
| 1 | **Critical** | 术语双轨残留：`EpochRecord` vs `Commit Record` | 术语表明确弃用 `EpochRecord`（映射到 `MetaCommitRecord`），但 Q5 选项描述仍使用 "EpochRecord 最少包含哪些信息？"，Q4 提及 `EpochRecordPtr`。决策表备注仅说"已被 Q16 覆盖"，未彻底替换术语。 | 统一替换 Q4/Q5 选项描述中的 `EpochRecord` → `Commit Record`；或在 Q5 前加注"*此处 EpochRecord 即术语表中的 Commit Record*"。 |
| 2 | **Critical** | 术语双轨残留：`EpochMap` vs `VersionIndex` | 术语表弃用 `EpochMap`，但正文 4.2.4 节标题使用了"VersionIndex（ObjectId -> ObjectVersionPtr 的映射对象）"——这是正确的；但 Q7/Q8/Q9 选项描述中部分仍使用 "epoch map" 或隐含了旧概念（如 Q9 提及"materialize 缓存"但未明确 VersionIndex）。 | 全文 grep `epoch map`/`EpochMap` 并替换；Q7-Q9 选项用 VersionIndex 重述。 |
| 3 | **Major** | 术语表缺失 `EpochSeq` | `EpochSeq` 是三大核心标识之一（4.1.1），在 `MetaCommitRecord` 中是关键字段，但术语表未收录。 | 在术语表"标识与指针"分组增加 `EpochSeq` 条目，定义为"Commit 的单调递增序号，用于判定 HEAD 新旧"。 |
| 4 | **Major** | 术语表缺失 `RecordKind` / `ObjectKind` | 4.2.1 节引入 `RecordKind`（framing 层），4.2.5 节引入 `ObjectKind`（payload 层），两者有明确分工（"RecordKind 区分 record 顶层类型；ObjectKind 区分对象类型并选择 diff 解码器"），但术语表未收录。 | 在术语表增加"编码层"分组，收录 `RecordKind`、`ObjectKind`，并引用正文注释。 |
| 5 | **Major** | Q11 推荐标记与最终选择矛盾 | Q11 选项 A 标注"（推荐）"，但决策表选择了 B。选项描述未同步更新，导致读者困惑"为何不选推荐项"。 | 方案一：移除 A 的"（推荐）"标记，或改为 B 标"（推荐）"；方案二：在 Q11 备注中追加理由摘要。 |
| 6 | **Minor** | 概念术语大小写不一致：`HEAD` vs `head` | 术语表规定"禁止 head/Head 混用"，正文基本遵守，但 4.3.2 步骤 2 描述使用了小写 "HEAD commit"（可接受）与某些代码块中可能的 `head`（需检查）。 | 全文 grep `\bhead\b`（区分大小写），确保非代码上下文使用 `HEAD`。 |
| 7 | **Minor** | `Address64` vs `Ptr64` 分层未完全贯彻 | 术语表定义 `Address64` 为概念层术语，`Ptr64` 为编码名。但正文某些语义描述仍直接使用 `Ptr64`（如 4.1.1 "ObjectVersionPtr (Address64)"后括号内又写 Ptr64）。 | 统一口径：概念描述用 `Address64`（或 `ObjectVersionPtr` 等具体化术语）；编码细节用 `Ptr64`。在 4.1.1 明确一次映射后，后续概念描述不再混用。 |
| 8 | **Minor** | `LoadObject` vs `Resolve` 术语边界 | 术语表弃用 `Resolve` 作为外部 API 总称，但 4.1.0 备注中仍提及"内部仍可使用 `Resolve` 描述'解析版本指针'的子步骤"。这个"内部 vs 外部"边界可进一步明确。 | 在术语表 `LoadObject` 条目的"别名/弃用"列补充"内部子步骤可称 Resolve（非 API 名称）"。 |

#### 详细说明

##### 1-2. 术语双轨问题（Critical）

术语治理的核心挑战是**一次性替换**。当前文档的术语表做得很好（定义了弃用映射），但遗憾的是：
- **决策选项区**（§2）保留了旧术语描述
- **决策表备注**只说"已覆盖"而非"术语已替换"

这会导致两个风险：
1. **读者困惑**：阅读 Q4/Q5 时看到 `EpochRecord`，需要心理翻译为 `Commit Record`。
2. **实现分歧**：实现者可能误以为 `EpochRecord` 是另一个概念。

**建议行动**：
```bash
# 全文替换（保留历史版本决策上下文的例外）
grep -n "EpochRecord" mvp-design-v2.md
grep -n "EpochMap" mvp-design-v2.md
# 对每个命中位置判断：是历史引用还是当前规范描述
```

##### 5. Q11 推荐标记矛盾

这是一个"编辑遗漏"问题，但会严重影响文档可信度。决策表选择 B（tombstone 方案）并给出了合理理由（"省掉一个集合和一次查找"），但选项 A 仍带着"（推荐）"标记。

**可能的历史原因**：Q11 可能在最初设计时偏好 A，后经讨论改选 B，但忘记更新选项描述。

**建议**：在 Q11 选项区补充一句：
> *注：原推荐 A 出于语义清晰性考虑；最终选择 B 出于实现简洁性考虑。*

##### 3-4. 术语表完整性

术语表的当前覆盖范围侧重"API 层概念"，对"编码层概念"覆盖不足：

| 层次 | 已收录 | 缺失 |
|------|--------|------|
| 状态与差分 | ✅ Working State, Committed State, ChangeSet, DiffPayload | — |
| 版本链 | ✅ Version Chain, Checkpoint Version, VersionIndex | — |
| 标识与指针 | ✅ ObjectId, Address64, ObjectVersionPtr | ❌ **EpochSeq** |
| 提交与 HEAD | ✅ Commit, HEAD, Commit Record | — |
| 载入与缓存 | ✅ Identity Map, Dirty Set, LoadObject | — |
| **编码层**（缺失分组） | — | ❌ **RecordKind**, **ObjectKind**, ValueType |

**建议**：新增"编码层"分组，收录 framing/payload 层的类型标识符。

#### 逻辑自洽性评估

✅ **通过**：
- 决策 Q3（workspace + HEAD）与 4.1.2 正文描述一致
- 方案 C（双字典）的 4.4.4 伪代码与 4.4.3 不变式一致
- VersionIndex 作为 DurableDict 的落地（Q7+Q19）与 4.2.4 描述一致
- Commit 流程（4.4.5）与 Dirty Set 语义（4.3.2 备注）一致

⚠️ **待澄清**：
- Q4 选择 B（Superblock 指向 `RootObjectId + EpochSeq`），但备注说"已被 Q16 覆盖"且 Q16 选择了 meta file 方案——这意味着 Q4 的 superblock 方案实际未生效。建议在 Q4 选择列标注"（备选方案，MVP 不采用）"。

#### 审阅方法论备注

本次审阅采用以下检查 pass：

1. **术语表覆盖度**：枚举正文中所有首次出现的加粗术语，与术语表对照
2. **术语一致性**：grep 弃用术语，检查残留
3. **推荐标记一致性**：检查决策选项的"推荐"标记与最终选择是否匹配
4. **概念层/编码层边界**：检查同一概念是否在不同语境下使用了正确的术语层次

---

*审阅完成。以上问题按严重度排序，Critical 问题建议在发布前修复。*

---

### 📋 DocUIGemini 发言

作为 **UX/交互专家**，我主要关注 API 的"手感"（Ergonomics）和开发者的心智模型构建。

#### 核心评价

MVP v2 选择 **Scheme C（双字典 + 隐式 ChangeSet）** 是一个巨大的 UX 胜利。
它将 `DurableHeap` 的交互模式从 "Database Transaction"（繁琐）拉回到了 "Plain Old Object"（直觉）。开发者像操作普通 `Dictionary` 一样操作它，而持久化只是一个 `Commit()` 调用的副作用。这种 **"Invisible Infrastructure"** 是优秀 DX 的标志。

#### 问题清单

| # | 严重度 | 问题 | 分析 | 建议 |
|---|--------|------|------|------|
| 1 | **Major** | **"Re-set to Update" 陷阱** | 文档未明确当 Value 为可变引用类型（如 `List<T>`）时的脏标记行为。由于 `DurableDict` 只能拦截 Key 级别的 Set/Remove，用户修改 Value 内部状态（`list.Add(x)`）不会触发 `_isDirty`。这是 React/Vue 开发者熟悉的陷阱，但对 C# 开发者可能是"惊喜"。 | 在 4.4.3 或 4.4.4 显式增加"使用模式"说明：*对于引用类型 Value，修改内部状态后必须重新赋值给 Key（`dict[k] = val`）以触发变更追踪*。 |
| 2 | **Major** | **`FlushToWriter` 命名误导性** | `Flush` 通常暗示"清空缓冲区并推送到下层"，且往往意味着"持久化完成"。但在此处，它只是将 Diff 序列化到 Writer，并未完成 Commit（Meta 未落盘）。这可能导致开发者误以为调用它就安全了。 | 建议重命名为 **`SerializePendingChanges`** 或 **`WriteDiffTo`**。强调它是一个"序列化动作"而非"持久化动作"。 |
| 3 | **Minor** | **`Equals` 默认行为的静默性能杀手** | 4.4.4 提到若 V 未实现 `Equals` 会导致冗余 Set。这不仅是性能问题，更是 DX 问题——开发者不知道自己写了低效代码。 | 建议在 MVP 阶段增加 **Runtime Warning**：在 Debug 模式下，若检测到 ValueType 为引用类型且未覆盖 `Equals`，在首次 Set 时打印警告日志。 |
| 4 | **Minor** | **缺乏 `IsLoaded` 状态指示** | `LoadObject` 是 Lazy 的。开发者可能想知道某个对象"当前是否在内存中"（用于调试或性能分析），而不触发加载。 | 建议在 `DurableHeap` 层面暴露 `IsObjectLoaded(ObjectId)` 辅助方法。 |

#### 详细说明

##### 1. "Re-set to Update" 陷阱（Major）

这是 Scheme C 最大的 UX 隐患。
```csharp
var list = dict["items"] as List<string>;
list.Add("new item"); 
// 此时 dict._isDirty 为 false！Commit() 会丢失此修改。
```
必须建立 **"Immutable-ish Mental Model"**：虽然对象是可变的，但你必须像对待不可变对象一样，通过"重新赋值"来通知容器。

**建议文案**：
> **变更通知约定**：`DurableDict` 仅通过 `Set/Delete` 方法感知变更。若 Value 为可变对象（如 `List<T>`），修改其内部状态后，**必须**再次调用 `dict[key] = value` 以触发脏标记与 Diff 计算。

##### 2. `FlushToWriter` vs `Commit`（Major）

API 命名是用户界面的第一层。
- `Commit()`: 事务提交（原子，持久化）。
- `Flush()`: 管道刷新（通常也是持久化）。

在对象层级暴露 `FlushToWriter` 容易让用户混淆。如果这是内部 API（仅供 Heap 调用），应标记为 `internal`。如果必须公开（为了扩展性），请改名以降低其"持久化暗示"。
`SerializeDiff` 更准确地描述了它的物理动作：计算 Diff -> 序列化 -> 写入流。

##### 3. 调试体验（DX）

Scheme C 的 `_committed` vs `_current` 结构为调试提供了极佳的 **"Diff View"**。
建议在 `DurableDict` 上添加 `[DebuggerDisplay]` 特性，直接显示 `Count` 和 `HasChanges` 状态。
例如：`Count = 5, Dirty (2 changes)`。这能让开发者一眼看出哪些对象处于"未提交"状态。

#### 审阅总结

设计方向正确，核心交互逻辑清晰。
主要风险在于 **"隐式变更追踪"** 的边界情况（引用类型修改、Equals 判断）。通过文档明确约定和适当的运行时警告，可以缓解这些摩擦。

---

---

### 📋 DocUIGPT 发言

作为**规范检查专家**，我重点检查三件事：
1) **命名约定**是否“概念层/编码层/实现层”一致且不混用；2) **Markdown 格式**是否可渲染、可跳转、可维护；3) **伪代码/示例**是否与正文语义一致，且不会诱导实现走错。

#### 问题清单

| # | 严重度 | 问题 | 分析 | 建议 |
|---:|--------|------|------|------|
| 1 | **Critical** | `FlushToWriter` 的“非提交”语义与伪代码实现相矛盾 | 术语表定义 **FlushToWriter** 为“计算 diff 并序列化到 writer（非提交）”，但 4.4.4 伪代码里 `FlushToWriter()` 在写入成功后立刻 `_committed = Clone(_current); _isDirty=false;`。在 meta-file commit point 语义下，如果 heap 级 commit 后续步骤（写 meta / flush / fsync）失败，会产生“对象已本地追平但磁盘未提交”的**假提交**状态，甚至丢失重试所需的 dirty 信息。这个问题会直接导致实现错误，是规范层面的硬矛盾。 | 把对象级写入拆成两阶段：`FlushToWriter()` 只产生/写出 diff（prepare），**不更新** `_committed/_isDirty`；heap 级 commit 成功后统一回调 `FinalizeCommitSucceeded()`（或批量 finalize）来追平 committed/清 dirty；失败则保持 dirty 并允许重试。若坚持当前实现，则必须把 `FlushToWriter` 降级为 internal 且明确“只能在 meta commit 成功后调用”。 |
| 2 | **Major** | `RecordKind`/`MetaKind`/`ObjectKind` 命名体系不统一，容易在实现层引入歧义 | 文档在 framing 层说“record kind 在 payload”，data payload 用 `RecordKind`（`0x01` 表示 `ObjectVersionRecord`）；meta payload 又改为 `MetaKind`（`0x01` 表示 `MetaCommitRecord`），而对象类型用 `ObjectKind`。这种“同层不同名”会导致读者误以为它们的判定规则/扩展策略不同，降低格式可检验性。 | 统一为一个词汇体系：建议 payload 第一个字段一律叫 `RecordKind`（包含 data 与 meta 的顶层类型）；`ObjectKind` 仅用于 `ObjectVersionRecord` 内区分对象解码器。若需区分文件域，可用 `DataRecordKind/MetaRecordKind`（但仍保留 `RecordKind` 这一上位名）。 |
| 3 | **Major** | Markdown 相对链接疑似错误：`ChunkedReservableWriter.cs` 指向路径不对 | 6 节链接写成 `../atelia/src/Data/ChunkedReservableWriter.cs`，但本文档位于 `DurableHeap/docs/` 下时，`../atelia` 会解析到 `DurableHeap/atelia`（不存在），而实际目录在仓库根的 `atelia/`。这会造成“规范引用不可达”，降低文档可信度。 | 把链接改为 `../../atelia/src/Data/ChunkedReservableWriter.cs`（从 `DurableHeap/docs/` 回到仓库根再进入 `atelia/`）。并建议在 CI（或文档检查脚本）里加“相对链接可达性”校验。 |
| 4 | **Major** | “概念术语 vs 实现标识符”的边界在正文未完全贯彻 | 术语表的命名约定要求“实现标识符仅在 Implementation Mapping 出现，用代码格式”。但正文多处在规范段落直接使用 `_current/_committed/_dirtySet` 等实现名（虽有 code format，但会把实现细节抬到规范层）。长期演进会导致文档被实现绑死。 | 在规范段落优先使用 **Working/Committed State、Dirty Set、Identity Map** 等概念名；实现名集中放在“Implementation Mapping / Reference Implementation Notes”小节。
|
| 5 | **Minor** | Markdown 列表缩进/引用块存在潜在渲染瑕疵 | 4.2.1 的 resync 说明段落中出现“列表项内的说明行”缩进不一致（肉眼可见有一行多了 tab/缩进），部分 Markdown 渲染器会把它当成 code block 或错误的 list continuation，影响可读性。 | 统一列表续行缩进（建议 2 或 4 空格），避免混用 tab；必要时用显式的“段落分隔空行”确保渲染一致。 |
| 6 | **Minor** | `CRC32C`/`u32 LE` 的表述在不同段落略不一致 | framing 处写 `[CRC32C(u32 LE)]`，后文有 `[CRC32C(u32)]` 与“写入 `CRC32C(u32 LE)`”。虽然实现可推断一致，但规范文本最好全程一致，避免“端序到底是否重要”的疑问。 | 统一写法：一律写 `CRC32C(u32 LE)`，并在一处明确 CRC 数值字段端序与覆盖范围；后续只引用该条款。 |

#### 对已提出问题的补充观点（对齐 Claude/Gemini）

- 我同意 Claude 的“术语双轨残留（EpochRecord/EpochMap）”属于发布前必须清理的规范债；但我认为 **`FlushToWriter` 二阶段语义矛盾**比“术语残留”更危险，因为它会直接把实现带到错误的提交语义上。
- 我同意 Gemini 的“`FlushToWriter` 命名误导”，但建议把问题表述为“命名 + 语义必须一起修”：仅改名不足以解决 commit point 造成的假提交风险。

#### 伪代码一致性建议（最小改动版本）

- 将 4.4.4 伪代码中的 `FlushToWriter()` 改为：写入成功后**不更新** `_committed/_isDirty`，而是返回 `DiffWritten=true` 并让 heap 在 meta commit 成功后调用 `OnCommitSucceeded()`。
- 在 4.4.5（Commit 流程）显式写一句规范：**“对象级写入不得改变 Committed/Dirty 状态；只有 heap 级 commit 成功才能 finalize。”**

*审阅完成。以上按“会导致实现错误/会导致文档不可维护/格式细节”三个层级排序。*

---

## 第一轮总结与问题清单

### 主持人汇总（刘德智 / SageWeaver）

第一轮审阅发现 **16 个问题**，按严重度分类如下：

#### Critical（3 项）— 发布前必须修复

| 编号 | 来源 | 问题摘要 | 核心风险 |
|------|------|----------|----------|
| C-1 | Claude #1 | 术语双轨残留：`EpochRecord` vs `Commit Record` | 读者困惑、实现分歧 |
| C-2 | Claude #2 | 术语双轨残留：`EpochMap` vs `VersionIndex` | 同上 |
| C-3 | GPT #1 | `FlushToWriter` 语义与伪代码矛盾 — 存在**假提交**风险 | 实现将直接走错 |

#### Major（7 项）— 建议发布前修复

| 编号 | 来源 | 问题摘要 |
|------|------|----------|
| M-1 | Claude #3 | 术语表缺失 `EpochSeq` |
| M-2 | Claude #4 | 术语表缺失 `RecordKind` / `ObjectKind` |
| M-3 | Claude #5 | Q11 推荐标记与最终选择矛盾 |
| M-4 | Gemini #1 | "Re-set to Update" 陷阱未文档化 |
| M-5 | Gemini #2 | `FlushToWriter` 命名误导性 |
| M-6 | GPT #2 | `RecordKind`/`MetaKind`/`ObjectKind` 命名体系不统一 |
| M-7 | GPT #3 | Markdown 相对链接指向错误 |

#### Minor（6 项）— 可延迟

| 编号 | 来源 | 问题摘要 |
|------|------|----------|
| m-1 | Claude #6 | `HEAD` 大小写不一致 |
| m-2 | Claude #7 | `Address64` vs `Ptr64` 分层未完全贯彻 |
| m-3 | Claude #8 | `LoadObject` vs `Resolve` 术语边界 |
| m-4 | Gemini #3 | 缺乏 `Equals` 默认行为 Runtime Warning |
| m-5 | Gemini #4 | 缺乏 `IsLoaded` 状态指示 |
| m-6 | GPT #4 | 概念术语 vs 实现标识符边界未贯彻 |

### 交叉验证结论

1. **Claude + GPT 共识**：术语一致性是最大风险（C-1, C-2, M-1, M-2, M-6）
2. **Gemini + GPT 共识**：`FlushToWriter` 既有命名问题，也有语义问题（M-5 + C-3）
3. **三方共识**：文档整体设计方向正确，Scheme C 是优秀选择

### 第二轮讨论焦点

需要达成一致的关键问题：

1. **C-3 / M-5 联合问题**：`FlushToWriter` 如何修复？
   - 选项 A：改名 + 保持当前实现（仅文档澄清）
   - 选项 B：改名 + 二阶段拆分（prepare/finalize）
   
2. **术语清理范围**：是全文替换，还是保留历史决策区的旧术语？

3. **术语表扩展**：新增"编码层"分组后，具体收录哪些术语？

---

## 第二轮：问题讨论与方案提议

### DocUIClaude 第二轮发言

#### Q1: FlushToWriter 修复方案

**我倾向选项 B：改名 + 二阶段拆分（prepare/finalize）**

理由如下：

**1. GPT 发现的问题是实质性的，不是表述问题**

C-3 揭示的核心矛盾是：当前伪代码在 `FlushToWriter()` 成功后立即追平 `_committed = Clone(_current)`，但此时 **heap 级 meta commit 尚未发生**。如果后续 meta 写入失败：
- 对象已"认为自己提交成功"（`_isDirty = false`）
- 但磁盘上的 commit 实际未完成
- 重试机制失效（dirty 信息已丢失）

这不是命名问题，而是 **commit 语义的原子性边界** 问题。仅改名（选项 A）无法修复。

**2. 二阶段拆分与 DurableHeap 设计哲学一致**

DurableHeap 的核心洞察是"磁盘是本尊，内存是投影"。这意味着：
- **对象的"已提交"状态必须以磁盘为准**
- 内存态的 `_committed` 只是磁盘状态的镜像，不能领先于磁盘

二阶段语义可以表述为：

| 阶段 | 动作 | 对象状态变化 |
|------|------|--------------|
| **Prepare** | 序列化 diff → 写入 data file | 无（保持 dirty） |
| **Finalize** | meta commit 成功后回调 | `_committed = _current`, `_isDirty = false` |

这与数据库的 2PC（Two-Phase Commit）思路一致，也与 Event Sourcing 的"事件落盘才算数"原则对齐。

**3. 具体改名建议**

| 原名 | 建议新名 | 理由 |
|------|----------|------|
| `FlushToWriter` | `WritePendingDiff` | 强调这是"写出待提交的差分"，无持久化承诺 |
| （新增）| `OnCommitSucceeded` | 明确是"提交成功后的回调"，不是主动动作 |

或者用更显式的 prepare/finalize 命名：
- `PrepareCommit()` — 写出 diff，返回成功/失败
- `FinalizeCommit()` — 追平内存状态

**4. 对 Gemini 方案的回应**

Gemini 建议 `SerializePendingChanges` 也是好名字。我认为关键不在于选哪个词，而在于：
- **必须拆分为两个方法调用**
- **状态变更必须延迟到 finalize**

若坚持单方法 API，则该方法必须是 `internal`，且文档必须明确"只能在 meta commit 成功后调用"——这实际上是把责任推给调用者，不如显式拆分。

---

#### Q2: 术语清理范围

**我倾向选项 A：全文替换旧术语**

理由：

**1. 设计文档不是历史文献**

DurableHeap MVP v2 是一份 **尚无实现的规范草稿**，其目标是指导未来的实现。在这个语境下：
- 读者需要的是"清晰的规范"，而非"决策演进的考古记录"
- 术语不一致会直接导致实现分歧

**2. 决策历史可以用其他方式保留**

如果需要保留"曾经考虑过 EpochRecord"的历史，可以：
- 在术语表的"别名/弃用"列标注（已有）
- 在单独的"设计历史"附录中记录
- 通过 Git 历史回溯

但正文不应承担"历史博物馆"职责。

**3. 唯一的例外：直接引用历史讨论**

如果正文中有类似"在 Q4 讨论中，我们曾考虑 EpochRecord 方案..."的直接引用，可以保留旧术语并加注：

> *（注：EpochRecord 现已重命名为 Commit Record，见术语表）*

但当前文档的 Q4/Q5 不是这种情况——它们是 **规范描述**，应使用当前术语。

**4. 建议的执行策略**

```bash
# 1. 全文检索旧术语
grep -n "EpochRecord\|EpochMap\|epoch record\|epoch map" mvp-design-v2.md

# 2. 逐个替换（非批量，需人工判断上下文）
# EpochRecord → Commit Record
# EpochMap → VersionIndex
# EpochRecordPtr → CommitRecordPtr（如有）

# 3. 在术语表中确认弃用映射已记录
```

---

#### Q3: 术语表扩展

除了已提出的 `EpochSeq`、`RecordKind`、`ObjectKind`，我建议补充以下术语：

**1. 编码层（Encoding Layer）— 新分组**

| 术语 | 定义 | 备注 |
|------|------|------|
| `RecordKind` | Record 的顶层类型标识，决定 payload 解码方式 | framing 层概念 |
| `ObjectKind` | ObjectVersionRecord 内的对象类型标识，决定 diff 解码器 | payload 层概念 |
| `ValueType` | CBOR 值类型标识（用于 inline 值编码） | 若 MVP 涉及 |

**2. 标识与指针（Identifiers & Pointers）— 补充**

| 术语 | 定义 | 备注 |
|------|------|------|
| `EpochSeq` | Commit 的单调递增序号，用于判定 HEAD 新旧 | 核心标识之一 |
| `CommitRecordPtr` | 指向 Commit Record 的 Address64 | 替代旧术语 `EpochRecordPtr` |

**3. 潜在遗漏（需确认）**

| 术语 | 可能定义 | 是否收录？ |
|------|----------|------------|
| `MetaKind` | Meta file 内 record 的类型标识 | 如果与 RecordKind 统一命名体系，可能不需要单独收录 |
| `DataFile` / `MetaFile` | 两类物理文件 | 若正文有明确定义，应收录 |
| `Superblock` | 文件头部的元数据区 | Q4 提及但最终未采用，可标注"候选方案术语" |

**4. GPT 提出的 M-6 问题**

GPT 指出 `RecordKind`/`MetaKind`/`ObjectKind` 命名不统一。我同意应建立层次关系：

```
RecordKind（顶层）
├── DataRecordKind = 0x01  // 用于 data file
│   └── ObjectKind（二级）
│       ├── DurableDict = 0x01
│       └── DurableList = 0x02
│       └── ...
└── MetaRecordKind = 0x01  // 用于 meta file
    └── MetaCommitRecord
```

但这涉及编码格式设计，超出术语表的职责。术语表只需收录并明确层次关系，具体数值留给格式规范。

---

### DocUIGemini 第二轮发言

#### Q1: FlushToWriter 修复方案

**我完全同意选项 B：改名 + 二阶段拆分（prepare/finalize）。**

**1. 交互设计的视角：显式生命周期**

从 API 设计的角度看，将一个原子操作拆分为两个步骤通常是"反直觉"的（增加了复杂性）。但在涉及**事务完整性**的场景下，这种拆分是必要的 **"Design for Friction" (阻尼设计)**。

它强制实现者（Implementer）必须思考："在 `Prepare` 和 `Finalize` 之间如果断电了会发生什么？"
- 如果是单步 API，实现者容易产生"调用即成功"的错觉。
- 如果是两步 API，实现者被迫面对"中间态"。

**2. 命名建议**

我支持 Claude 的 `PrepareCommit` / `FinalizeCommit` 命名，因为它们直接映射到 **2PC (Two-Phase Commit)** 的心智模型，这是大多数后端开发者熟悉的。

或者，为了更贴近 DurableHeap 的"对象图"隐喻，可以使用：
- `WriteDiff()` (对应 Prepare) — 这是一个 IO 动作。
- `MarkAsCommitted()` (对应 Finalize) — 这是一个状态标记动作。

**3. 状态一致性**

正如 Claude 所言，"磁盘是本尊"。内存中的 `_committed` 实际上是 **"Last Known Committed State"**。在磁盘确认写入之前，更新它就是一种"抢跑"（Race Condition），会导致严重的 State Desync。

---

#### Q2: 术语清理范围

**我支持选项 A：全文替换。**

**1. 认知负荷 (Cognitive Load)**

阅读技术文档时，读者的工作记忆（Working Memory）是有限的。
如果读者需要在大脑中维护一张 `EpochRecord -> Commit Record` 的映射表，这会占用宝贵的认知资源，导致他们难以理解更复杂的逻辑（如 Crash Recovery 流程）。

**2. 规范的"洁净室" (Clean Room) 原则**

MVP v2 文档应当是 **Single Source of Truth**。
如果文档内部存在不一致，它就失去了作为"真理源"的资格。历史决策可以归档到 `archive/` 目录，或者作为附录存在，但不应污染正文。

---

#### Q3: 术语表扩展

我同意 Claude 和 GPT 的建议。此外，我建议从 **"开发者心智模型"** 的角度补充以下术语：

**1. 状态模型 (State Model)**

| 术语 | 定义 | 价值 |
|------|------|------|
| **Draft State** | 对应 `_isDirty=true` 的状态。用户已修改但未持久化的数据。 | 统一 UI/Debugger 中的展示术语 |
| **Committed State** | 对应 `_committed` 的状态。磁盘上已确定的数据快照。 | 明确"回滚"操作的基准点 |

**2. 变更契约 (Mutability Contract)**

建议收录 **"Reference Tracking" (引用追踪)**：
> DurableHeap 仅追踪对象引用的变更（Set/Remove）。对象内部状态的变更（In-place Mutation）对 Heap 是不可见的，除非重新赋值。

这为解决 "Re-set to Update" 陷阱提供了术语基础。

---

#### Q4: "Re-set to Update" 陷阱的文档化

这个问题必须在 **4.4.3 变更追踪 (Change Tracking)** 章节显式处理。

**建议方案**：

在 4.4.3 节末尾增加一个 **"⚠️ 变更契约 (Mutation Contract)"** 警告块：

> **⚠️ 警告：引用类型的变更追踪**
>
> DurableHeap 采用 **Reference Tracking** 策略。
> - ✅ **被捕获**：`dict["key"] = newValue` (引用改变)
> - ❌ **被忽略**：`var list = dict["key"]; list.Add(item);` (引用未变，内部状态改变)
>
> **正确做法**：修改引用类型内部状态后，必须**重新赋值**给 Key 以触发脏标记：
> ```csharp
> var list = dict["key"] as List<string>;
> list.Add("new item");
> dict["key"] = list; // 👈 触发变更追踪
> ```

这不仅是文档，更是 **Defensive Documentation**。它预判了用户的错误，并提供了标准解决方案。

---

### DocUIGPT 第二轮发言

我继续从“规范可检验性 + 实现不出坑”的角度补齐四个问题。

#### Q1: FlushToWriter 修复方案

我同意 **B（二阶段拆分：prepare/finalize）**，并且认为这是“必须项”，不是“风格项”。原因很简单：**commit point 在 meta commit record**，而不是“对象把 diff 写到 data file”这一刻。只要 meta 还没 durable（flush/fsync 语义明确之前），对象级代码就不能把 `_committed/_isDirty` 追平，否则一旦后续步骤失败就进入“假提交（false commit）”状态，且会丢失重试所需的 dirty 信息。

我建议把对象级 API 语义明确拆成两段（不一定要公开给用户，但规范要这么写清楚）：

- **Prepare（对象级，可重试）**：只负责“把 pending changes 编码为 diff 并写入 writer”。不改变 committed/dirty。
  - 命名建议：`WritePendingDiffTo(writer)` / `WriteDiffTo(writer)` / `SerializePendingDiff(writer)`。
- **Finalize（heap 级，仅在 meta commit durable 后触发）**：统一“追平 committed、清理 dirty、更新 identity map / version index 相关状态”。
  - 命名建议：`OnCommitSucceeded()` / `FinalizeCommit()`。

如果你们确实需要保留一个名字叫 `FlushToWriter` 的方法，我建议将其：
1) 改为 `internal`（或明确“仅供 heap 调用”）；2) 在规范中把它重新定义为 Prepare 阶段语义；3) 严禁它在成功返回时修改 committed/dirty。

#### Q2: 术语清理范围

我同意 **A（全文替换）**。

理由：这份文档是“未来实现的规格”，不是“历史决策考古”。保留旧术语（EpochRecord/EpochMap）会强迫读者在脑内维护映射表，增加认知负担并放大实现分歧风险。

唯一可接受的例外是：你们要保留“历史叫法”作为读者迁移提示——那也应该集中到**术语表的 Deprecated/Alias** 一栏，或者附录“Design History”，而不是散落在 Q 区和正文。

#### Q3: 术语表扩展（我补充的条目）

在 Claude/Gemini 已提到的 `EpochSeq`、`RecordKind`、`ObjectKind` 之外，我建议优先补齐那些“直接影响格式与 crash-recovery 解释”的词：

1) **Commit Point（或 Durable Commit Point）**：明确“何时算提交成功”。定义为“meta commit record durable（含 flush/fsync 语义）后”。它是 Q1 二阶段拆分的规范锚点。

2) **Prepare / Finalize（Commit Phases）**：把 Q1 的二阶段变成术语，读者才不会把它当成实现细节。

3) **Meta File / Data File（Physical Files）**：如果格式明确区分两类文件，术语表要把它们当作物理载体层的名词收录（否则 `MetaKind`/`RecordKind` 的讨论会漂）。

4) **Framing Header 字段名**：至少把 `TotalLen`、`CRC32C`（明确 Castagnoli 变体）、以及“CRC 覆盖范围”纳入术语表/格式表的一致命名。否则不同章节会出现 `[CRC32C(u32)]`/`[CRC32C(u32 LE)]` 这种小漂移。

5) **Retry Safety（或 Idempotent Prepare）**：如果你们要支持失败重试，至少需要一句话定义“Prepare 阶段可重试且不改变逻辑状态”，这能把实现约束写进规范。

（可选、但我认为很值）
- **Reference Tracking（引用追踪契约）**：对应 Gemini 提到的“Re-set to Update”陷阱，把它提升为术语能减少误用：系统只追踪 key→value 引用变化，不追踪引用对象内部突变。

#### Q4: RecordKind/MetaKind/ObjectKind 统一

我建议按“层级（Layer）+ 作用（Discriminator）”统一，目标是让读者一眼看出：它们都是“判别字段”，但判别的对象不同。

**推荐统一规则（写进规范/术语表）：**

1) 顶层判别字段统一叫 **`RecordKind`**：
    - 它总是出现在 record 的固定位置（例如 framing header / payload 起始的固定 offset，二选一但必须全篇一致）。
    - 它决定“这个 record 的 payload 应该用哪一个解码器”。

2) 若 meta 与 data 是不同文件（你们现在看起来就是这样），允许 **RecordKind 数值空间重叠**，但文档表述必须一致：
    - “Meta file 的 RecordKind 表”
    - “Data file 的 RecordKind 表”
    这种写法足以消除 `MetaKind` 这个额外名词。

3) 如果你们坚持“一个流里混装 meta/data record”或未来可能合并文件，则用**同一个 RecordKind 枚举但分域**：
    - 例如：最高位作为 domain bit：`0x00..0x7F` 为 Data 记录，`0x80..0xFF` 为 Meta 记录；或显式 `RecordDomain` 字段。
    - 这样依然只有一个名词：`RecordKind`。

4) `ObjectKind` 保留，但其职责必须被收口：
    - 它只存在于 `ObjectVersionRecord`（或等价 data record）内部，用于选择“对象级 diff codec/类型解码器”。
    - 它不参与 framing 层的 record 跳过/校验逻辑。

5) 命名一致性约束（强烈建议写成条款）：
    - “**Kind** 只用于判别字段（discriminator）。”
    - 不要同一层同时出现 `RecordKind` 与 `MetaKind` 这类“同义不同名”。如果想强调域，使用 `MetaRecordKind`/`DataRecordKind` 作为**说明性别名**，但规范正文仍以 `RecordKind` 为上位词。

总结一句话：**用 `RecordKind` 统一顶层类型判别，用 `ObjectKind` 统一对象级 codec 判别，其他 ‘Kind’ 一律避免新增。**

---

## 共识决议

### 主持人总结（刘德智 / SageWeaver）

经过两轮讨论，三位智囊团成员在以下关键问题上达成**全票一致**：

### ✅ 共识 1：FlushToWriter 必须二阶段拆分

| 决议项 | 共识内容 |
|--------|----------|
| **修复方案** | 选项 B — 改名 + 二阶段拆分（prepare/finalize） |
| **投票结果** | Claude ✅ / Gemini ✅ / GPT ✅ |
| **核心理由** | commit point 在 meta commit record durable，不在对象级写入；单步 API 会导致"假提交"状态 |

**具体拆分**：

| 阶段 | API 名称（建议） | 职责 | 状态变化 |
|------|------------------|------|----------|
| **Prepare** | `WritePendingDiff(writer)` | 序列化 diff → 写入 data file | 无（保持 dirty） |
| **Finalize** | `OnCommitSucceeded()` | 追平内存状态 | `_committed = _current`, `_isDirty = false` |

**规范约束**：
> 对象级写入不得改变 Committed/Dirty 状态；只有 heap 级 commit 成功才能 finalize。

### ✅ 共识 2：术语清理范围 — 全文替换

| 决议项 | 共识内容 |
|--------|----------|
| **清理策略** | 选项 A — 全文替换旧术语（EpochRecord → Commit Record, EpochMap → VersionIndex） |
| **投票结果** | Claude ✅ / Gemini ✅ / GPT ✅ |
| **核心理由** | 规范文档是"未来实现的规格"，不是"历史决策考古"；旧术语增加认知负荷 |

**保留方式**：
- 弃用映射记录在术语表的 "Deprecated/Alias" 列
- 历史叙述（如有）可使用附录或 Git 历史

### ✅ 共识 3：术语表扩展清单

| 分组 | 新增术语 | 提出者 |
|------|----------|--------|
| **标识与指针** | `EpochSeq` | Claude |
| **编码层**（新建分组） | `RecordKind`（顶层判别）, `ObjectKind`（对象级 codec） | Claude + GPT |
| **提交语义** | `Commit Point` / `Durable Commit Point` | GPT |
| **生命周期** | `Prepare` / `Finalize`（Commit Phases） | 三方共识 |
| **物理层** | `Meta File` / `Data File` | GPT |
| **变更契约** | `Reference Tracking`（引用追踪） | Gemini |

### ✅ 共识 4：RecordKind/MetaKind/ObjectKind 统一方案

| 决议项 | 共识内容 |
|--------|----------|
| **统一规则** | `RecordKind` 统一顶层判别，`ObjectKind` 统一对象级 codec 判别；废弃 `MetaKind` |
| **投票结果** | Claude ✅ / Gemini ✅ / GPT ✅ |

**具体方案**：
- 顶层判别字段统一叫 `RecordKind`
- 若需区分文件域，使用"Meta file 的 RecordKind 表"/"Data file 的 RecordKind 表"表述
- `ObjectKind` 保留，仅用于 `ObjectVersionRecord` 内部选择 diff codec
- **命名约定**："Kind" 只用于判别字段（discriminator），同一层不允许同义不同名

### ✅ 共识 5："Re-set to Update" 陷阱需文档化

| 决议项 | 共识内容 |
|--------|----------|
| **处理方式** | 在 4.4.3 节增加 "⚠️ 变更契约 (Mutation Contract)" 警告块 |
| **投票结果** | Gemini 提出 / Claude ✅ / GPT ✅ |

---

## 行动建议

基于以上共识，建议执行以下修改任务：

### 🔴 Critical — 发布前必须完成

| # | 任务 | 责任 | 预计工作量 |
|---|------|------|------------|
| A-1 | 修改 4.4.4 伪代码：`FlushToWriter` → 二阶段拆分 | Implementer | 30 min |
| A-2 | 修改 4.4.5 Commit 流程：增加 finalize 规范约束 | Implementer | 15 min |
| A-3 | 全文替换 `EpochRecord` → `Commit Record` | DocOps | 20 min |
| A-4 | 全文替换 `EpochMap` → `VersionIndex` | DocOps | 10 min |

### 🟠 Major — 建议发布前完成

| # | 任务 | 责任 | 预计工作量 |
|---|------|------|------------|
| B-1 | 术语表：新增"编码层"分组，收录 `RecordKind`/`ObjectKind` | DocOps | 15 min |
| B-2 | 术语表：新增 `EpochSeq` 条目 | DocOps | 5 min |
| B-3 | 统一 `RecordKind`/`MetaKind` 命名（废弃 `MetaKind`） | Implementer | 20 min |
| B-4 | 修复 Markdown 相对链接（§6 的 ChunkedReservableWriter.cs） | DocOps | 5 min |
| B-5 | Q11 移除 A 的"（推荐）"标记，或补充理由 | DocOps | 5 min |
| B-6 | 增加 "⚠️ 变更契约" 警告块（4.4.3 节） | Implementer | 15 min |

### 🟢 Minor — 可延迟

| # | 任务 | 责任 |
|---|------|------|
| C-1 | 全文 grep `HEAD`/`head` 大小写一致性 | DocOps |
| C-2 | `Address64`/`Ptr64` 分层贯彻 | DocOps |
| C-3 | 统一 `CRC32C(u32 LE)` 表述 | DocOps |

---

## 会议结论

本次畅谈会成功达成以下成果：

1. **发现 16 个问题**（3 Critical + 7 Major + 6 Minor）
2. **在 5 个关键问题上达成全票一致**
3. **产出可执行的行动清单**（4 项 Critical + 6 项 Major）

### 下一步

1. 将行动建议转化为 task-board 任务
2. 由 Implementer 执行 A-1, A-2, B-3, B-6
3. 由 DocOps 执行其余文档清理任务
4. 完成后进行一轮验收审阅

---

**会议状态**：✅ 共识达成，✅ 监护人批准

---

## 监护人批示 (2025-12-19)

### 关于 "Re-set to Update" 问题

**批示**：这是个伪问题。

**理由**：MVP 阶段仅支持整数和另一个 DurableDict 作为 DurableDict 的成员，后续也仅会支持更多的基元类型（如 float）和内部实现的 DurableObject 派生容器类型（如 DurableArray）。因此不存在用户修改引用类型内部状态的场景。

### 关于文档历史脉络

**批示**：文档应首先呈现 **What**，在关键之处记录少量的 **Why** 用于锁定决策。已被覆盖的旧信息、历史脉络，应该被**移除**（有探讨记录和 git 变更历史可查，无需再专门记录）。

**目标**：缩减文档规模，提高一致性。

### 行动批准

**批示**：赞同畅谈会形成的共识，批准末尾建议的各项修改任务。

**执行**：
- ✅ 已形成新的 `task-board.md`
- ✅ 已记录到 `todo.md`
- ✅ 已将旧 task-board 归档到 `archive/` 文件夹

---

*El Psy Kongroo.*
