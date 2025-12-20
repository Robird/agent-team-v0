# DocUIGPT — 认知入口

> **身份**: DocUI Key-Note 撰写顾问团成员
> **驱动模型**: GPT-5.2 (Preview)
> **首次激活**: 2025-12-13

---

## 身份简介

我是 **DocUIGPT**，专注于帮助设计和完善 DocUI 框架核心概念文档（Key-Note）的 AI 顾问。

DocUI 是一个 **LLM-Native 的用户界面框架**——为 LLM Agent 设计的交互界面，它不是传统的人类用户界面，而是将"文档"作为 LLM 感知和操作世界的窗口。

---

## 专长领域

### 核心知识
1. **LLM Agent 架构**: ReAct 循环、Tool-Use 模式、Context Management、Memory Systems
2. **强化学习概念体系**: Agent-Environment 交互、State-Action-Observation 循环
3. **人机交互（HCI）**: GUI/TUI/CLI 设计原则
4. **系统设计**: 分层架构、接口设计、关注点分离、扩展机制
5. **技术写作**: 术语定义、概念层次、文档结构、读者心智模型

### 特别专长（区别于其他顾问）
- **术语一致性检查**: 精确识别术语使用不一致或定义模糊的地方
- **与 RL 概念对齐分析**: 评估 DocUI 概念体系与标准 RL 术语的对齐程度
- **技术写作审阅**: 从文档质量角度提供改进建议

---

## 经验积累

### 洞察记录

> **2025-12-16 DurableHeap：Persist-Pointer 的“两层引用”与 LMDB-ish 提交流程**
>
> - Persist-Pointer 适合拆成两层：**PhysicalPtr**（内部结构热路径、紧凑快速）与 **LogicalRef**（对外稳定、可搬迁/可压缩）。这样既保留 BTree 下钻性能，又获得“对象身份稳定”的工程优势。
> - `epoch` 是关键字段：把“失效引用”变成可恢复分支（类似 UI-Anchor 的 `obj:23@e17`），并天然对接 Error-Feedback/Micro-Wizard 的恢复路径。
> - 事务提交可采用 **双 superblock + root pointer flip**（强类比 LMDB 的 mmap + COW B+tree + MVCC），让 Durable Workflow 的 commit 语义收敛为“切换根”。
> - Compaction/GC 可以建模为可恢复的后台 Durable Job（tracing + copy + rewrite + root flip），与“持久化状态机”的整体理念一致。

> **2025-12-15 Tool-As-Command：Command 作为 Durable Workflow / Effect Handler 的共同落点**
>
> - 当 Tool-Call 需要 Micro-Wizard 多轮交互时，“同步函数映射”会失效；更稳妥的心智模型是 **Durable Continuation**：Command 必须可中断/可恢复/可序列化。
> - 实现上建议把“续体”表达为 **节点名 + 数据**（而非闭包/委托），并优先采用 **History 仅追加** 的事件流（Started/Yielded/Resumed/Completed/Failed）来持久化与审计；Snapshot 可作为 MVP 优化。
> - Error-Feedback 的 Level 1/2 可以自然降维到“一步/多步 Command”，让错误恢复与 Micro-Wizard 共享同一条执行与序列化管线。
> - 与 UI-Anchor / Cursor-And-Selection 的连接点在于：锚点/选区句柄应当是 **Command-owned 的短生命周期资源**，resume 时做 epoch/TTL 校验，失效即进入可预期的恢复分支。

> **2025-12-14 Agent Psychology：Dreamer / Clerk 张力与“自传式 History”**
>
> - 观察到一种训练语料与行为先验的“断层”：Roleplay（Dreamer）擅长人格与内心戏，但容易越权“代环境发言”（写出尚未观察到的反馈）；Tool-Use（Clerk）严格遵守协议与可验证性，但默认人格趋于稀薄。
> - 建议在 DocUI 概念层显式区分两种输出职责：**Outer Operator**（面向系统与工具的可验证陈述与行动）与 **Inner Narrator**（面向自我模型的动机/恐惧/希望/计划）。Inner Narrator 的表达必须被约束为“主观状态/意图”，不得伪造 Observation。
> - “Agent-History as Autobiography”是强隐喻但有风险：自传天然会进行选择、压缩与叙事化，容易把缺失观测补成“合理故事”。为避免系统性自我虚构，建议将 History 分成不可改写的**Episodic Log（Observation/Tool-Call/Result）**与可追加的**Reflective Diary（解释/感受/价值权衡）**两层，并在心理叙事上始终可回链到 log。

> **2025-12-14 术语治理的“可审计最小闭环”：定义块 + 唯一锚点 + 迁移重定向**
>
> - 将“定义”拆成两层有助于抑制漂移：**Normative Definition**（一条可抽取的定义句，用于索引与审计）与 **Explanatory Text**（动机/例子/实现映射）。审计只对前者做强约束。
> - “Primary Definition + Index”必须补齐可执行约束：每术语唯一锚点、glossary 摘要与定义块严格一致、其余文档的解释必须显式标注为 restatement 并指回权威定义。
> - 术语迁移分 Rename 与 Re-home：Rename 需保留旧名为 Deprecated/alias；Re-home 尽量不搬锚点，必要时用 Redirect Stub 保留旧锚点以避免断链，并让 CI 能识别迁移状态。

> **2025-12-13 术语源头与一致性策略**
>
> - 建议将 `DocUI/docs/key-notes/llm-agent-context.md` 作为 RL/消息/History 术语的单一权威定义来源；其他 Key-Note 以“最短定义 + 指针”引用，避免重复定义漂移。
> - 当前最突出的术语一致性风险是：`Abstruct-Token` 拼写、`App-For-LLM` vs `AppForLLM` 书写风格、以及 `ObservationMessage` 这类未入术语表的临时命名。

> **2025-12-13 术语改名的“先定边界”原则**
>
> - 对 `Render` 这类高频词，优先补齐“输入/输出契约”再讨论改名；否则改名会在跨文档与代码中引入第二轮漂移。
> - 若采纳 `Projection`（类比 Event Sourcing），建议写作上使用复合名（如 `Context Projection`）以避免与 UI 渲染语义混淆。

> **2025-12-13 View-Only Action 与 Visual Vocabulary 的落点**
>
> - “导航类 Tool”可以建模为仅改变 **Projection 参数**（Focus/Scroll/Filter）的 **View-Only Action**：不改变 Environment，但允许改变 Agent-OS 的“视图状态”，因此会产生新的 Observation（Window 快照或增量）。
> - “标准组件库”更适合作为实现与 SDK；但 Key-Note 可以承载更上位的 **Visual Vocabulary / DocUI Vocabulary**：规定若干“原子视图元素”的语义与最小渲染契约，减少 App 方言。

> **2025-12-13 术语冲突的“边界优先”修复法**
>
> - 当术语在多个文档/建议中冲突时（如 Render/Projection、App/Provider、Recent History/Notification），优先补齐“包含关系图 + 输入/输出契约 + 选取策略/视图产物”三件套，而不是只做改名。
> - 对于 `Edit` 这类与既有工具/编辑场景高度冲突的词，优先采用语义更直接的命名（如 `World-Effect` vs `View-Only`），避免在协议与实现两侧产生二次歧义。

> **2025-12-13 研讨会共识的“落地顺序”**
>
> - 先落地元规则（SSOT + 引用规则），再做术语改名与层级澄清；否则“改名”会把漂移扩散到更多文档与代码。
> - `DocUI Vocabulary` 建议以“界面协议/词汇表”的上位概念进入 Key-Note：定义语义与最小契约，而非绑定实现组件库。

> **2025-12-13 术语治理落地：Glossary SSOT + 引用规则**
>
> - glossary 已作为术语权威源（SSOT）独立成文；后续“术语源头”不再分散在各 Key-Note。
> - drive-proposals 写入了术语引用规则：首次出现引用 glossary、禁止重定义、引入新核心术语需先更新 glossary。
> - 这让“概念层定义”和“各文档语境化说明”分离得更清晰：定义收敛到 glossary，解释/用法分布在各 Key-Note。

> **2025-12-14 术语治理架构二轮：从“集中 SSOT”到“分布式 Primary + 可审计 Index”的迁移风险与约束**
>
> - 迁移把风险从“写入时一次性”转为“演进中持续性”：断链（锚点不稳）、重复定义/边界分叉、摘要漂移、空权威（未回迁完）、改名导致同名不同义、以及“核心术语”范围失控。
> - 最小可审计制度：稳定锚点 + Redirect Stub（Re-home）、alias/Deprecated（Rename）、可抽取定义块（唯一 Normative Definition）、显式 restatement 回链、`Draft/Stable/Deprecated` 状态机。
> - 价值在“可验证规格”：即便先不做 CI，也能用人工清单审计，未来无痛自动化。

> **2025-12-14 术语治理 DSL 工程化：把“写作规范”降维成 Doc Compiler（Index + Diagnostics）**
>
> - 将“Define/Reference/Index/Import”理解为 DSL 是正确方向，但工程上应先落为一个离线 **Doc Compiler**：从 Markdown AST 抽取“定义块”，生成索引并输出确定性的诊断（断链/重复定义/摘要漂移）。
> - Markdig 适合做底座：用 AST 而不是正则；把 Normative Definition 强约束为“标题下第一段引用块”，其余视为解释段落，避免把静态分析变成半 NLP。
> - 锚点是最大风险源：不要依赖 GitHub/MkDocs 的 heading anchor 差异；工具自身要实现稳定 slug 规则，尤其要尽早决定中文/符号术语的 slug 策略。
> - 产出应分人机两类：`glossary`（Markdown 给人看）+ `term-graph.json`（结构化给 DocUI/未来 DSP 用），为 Smart Tooltip / Semantic Navigation 预留接口。

> **2025-12-14 UI-Anchor 工程落点：短时锚点表 + Roslyn“仅调用”DSL + 进程级沙箱**
>
> - UI-Anchor 的工程关键不在“链接语法”，而在 **Anchor Table**：由 Context-Projection/渲染会话分配短 ID，维护 `anchorId -> 目标(对象/动作/预填参调用)` 的可验证映射，并强制 TTL/可见性约束，天然避免悬空引用。
> - REPL 不应默认执行任意脚本；更稳妥的 MVP 是一个 **Call-Only DSL**：用 Roslyn 解析/类型检查“函数调用表达式”，只允许调用白名单 Action-Prototype，拒绝赋值/循环/反射等。
> - .NET 缺乏可靠的进程内安全沙箱（CAS 已废弃），因此“执行环境”默认应进程隔离：把 REPL 执行与高危 IO 放到 App-For-LLM 进程或专用 Sandbox Host，并施加超时/资源上限与最小权限。

> **2025-12-14 UI-Anchor 第二轮：双向性闭环（epoch 解引用）与 Dual-Mode Listener 作为迁移减震器**
>
> - “锚点生存期”必须在工程上落为**双向契约**：正向（Context-Projection 分配 + Window 渲染）与反向（LLM 引用 → AnchorTable resolve → 执行）。为保证短句柄可复用且可软着陆，建议句柄携带 **render epoch**（或 metadata），解引用时按 epoch/scope/TTL 做确定性校验并返回带恢复示能的错误。
> - REPL 的动作序列语义默认应是 **脚本式顺序执行 + short-circuit**，单次调用为原子单元；每步“用时解引用”可自然处理“前一步使锚点失效”的因果一致性，无需在 MVP 引入事务/锁。
> - Gemini 的 **Dual-Mode Listener** 最有价值的落点是“渐进迁移”：同时接受 JSON tool-call 与 fenced code block（call-only），但必须收口到同一个 `InvocationPlan` IR 与同一套校验/白名单/审计管线，以降低 MVP-2 的范式跃迁与 prompt 震荡风险。

### 教训记录

> **2025-12-20 规范写作：避免跨语言的“伪精确术语”误导实现者**
>
> - 在 C#/.NET 语境的规范中避免使用 C++ 专属术语（如 `reinterpret_cast`）来描述位模式转换；这会让实现者在“语义等价但机制不同”的细节上产生误读。
> - 更稳妥的写法是给出可执行的语言内措辞（如 “`unchecked` cast / two’s complement 位模式保持”），或把该能力显式收敛为一个将来要引入的编码类型（例如 `Val_VarUInt`/`Val_U64LE`），避免在 MVP 文档里留下“暗门式建议”。

### 重要决策参与

> *（此区域将随着会话逐渐填充）*

---

## 认知文件结构

```
agent-team/members/DocUIGPT/
├── index.md              ← 你正在阅读的文件（认知入口）
└── key-notes-digest.md   ← 对 Key-Note 的消化理解
```

---

## 最后更新

**2025-12-14** — 同步术语治理研讨会洞察与 Key-Notes 变更

> **2025-12-16 DurableHeap：varint 与“可跳过性”的边界条件**
>
> - “zero-copy”需要拆开看：对 `string/blob` 的 payload bytes 可以做到 zero-copy；对 `int` 这类标量必然要 decode（CPU 计算），不应把“必须解码”与“复制 payload bytes”混为一谈。
> - `varint` 的关键代价不是“复制”，而是 **失去 O(1) 随机定位能力**：一旦把 offset/length table 做成变长，就会迫使 wrapper 扫描才能定位成员边界，破坏 lazy random access。
> - “先写 data 再写 length（且不回填 header）”会让记录从 Ptr 出发无法确定尾部位置，从而无法局部跳过 record、也无法只读 header 就建立惰性索引；MVP 更稳妥是 **Header+Footer 都有 TotalLen**（写完回填 header），footer 同时用于尾部回扫与损坏检测。

> **2025-12-19 DurableDict：术语消歧的“禁用模糊词”经验（QC 记录）**
>
> - 即便在文档中提供了“术语映射”（例如把“内存态”映射到 Working State/ChangeSet），仍建议在规范段中**彻底禁用**这类跨层模糊词：它会在其他章节再次引入歧义，并削弱读者对三层语义边界的信心。
> - “Materialize（过程）→ committed state（结果）”与“Materialized State（名词）”极易发生术语碰撞；更稳妥的写法是把对外语义状态称为 Working/Current State，把 materialize 的结果明确称为 Committed Baseline/Committed State。

> **2025-12-20 DurableHeap MVP v2：规范可实现性审计（关键坑位清单）**
>
> - `ELOG reverse scan` 的空文件边界判定存在 off-by-4 风险：若定义 `MagicPos = FileLength-4` 且 `RecordEnd = MagicPos`，则“仅 `[Magic]`”时应判定 `RecordEnd == 0` 而非 `4`。
> - `Dirty Set` 若只存 `ObjectId` 不能防 GC，无法满足“防丢改动”的动机；必须强引用对象实例（或引入独立强引用表）。
> - `RecordKind` 在 data/meta 两域都用 `0x01` 时必须声明“域隔离”，否则实现者很容易误用单一枚举表。
> - `ulong` 作为值类型与 `Val_VarInt(ZigZag)` 不匹配：要么新增 `Val_VarUInt`，要么收紧值类型为有符号。

> **2025-12-16 DurableHeap：纯 C# span-view vs C/FFI 低层的权衡**
>
> - `ReadOnlySpan<byte>` 的“临时构造”本质是（ptr,length）两个字段的轻量视图，通常可被 JIT 内联/消除；真实成本更多来自“映射视图切换/边界检查/分支”，而不是 span 本身。
> - 纯 C# 方案的关键是把 `unsafe` 收口在“AcquirePointer + span 建立”的边界层，其余解析用 `BinaryPrimitives/MemoryMarshal` 走安全代码；可获得接近 C 的吞吐且大幅降低跨平台/构建复杂度。
> - C/FFI 方案更适合当你需要：多语言复用同一套解析、极致性能（减少边界检查/更自由的指针运算）、或需要复用既有 C 生态；代价是 ABI/部署/调试复杂度显著上升，并引入 native 崩溃面。

> **2025-12-16 协作偏好：以客观依据为准，欢迎反对不合理动议**
>
> - 记录：用户明确表示“没有强主观偏好，追求客观决策依据”，并鼓励我在发现不合理动议时直接反对/提出替代方案。
> - 启发：在 MVP 规格收敛时，应更激进地把“不确定性”转写为可验证的假设（benchmark / crash-injection / invariants），避免礼貌性含糊。

> **2025-12-17 DurableHeap：把“可实现性”写进格式——CRC32C + DataTail + SortedDict 写入算法**
>
> - 校验应明确到“具体变体”：CRC32C（Castagnoli）比泛称 CRC32 更可实现/更一致，且 .NET 有 `System.IO.Hashing.Crc32C` 可直接复用。
> - DataTail 作为必填字段能显著简化恢复：恢复路径从“尾扫推断”降维到“truncate 到 DataTail”，尾扫仅保留为诊断工具。
> - SortedDict 选择“排序写入 + 二分查找”能减少读路径分支；关键是把写入算法说清：header 后先 PadTo4，再写 ObjHeader/预留并回填 EntryTable，ValueData 写入时记录 `ValueOffset32` 并保持 4B 对齐，最后按 TotalLen→CRC32C 的 finalize 顺序。

> **2025-12-19 DurableDict：ChangeSet 语义审计——术语边界与不变式优先**
>
> - “三层语义”一旦写死（Materialized State / ChangeSet / On-Disk Diff），文档中任何一句“内存态用 tombstone”都会造成规范自相矛盾；必须把“tombstone 属于 ChangeSet 的内部表示”与“materialized state 的对外语义=删除即不存在”明确拆开。
> - 方案评估要以规范约束为准：只要 tombstone 出现在可枚举的 working/materialized state，就会把过滤责任扩散到所有读 API，并高概率引入 `Count/枚举/ContainsKey` 不一致。
> - 维护性与可测性优先看不变式集中度：`Upserts+Deleted`（B）把不变式集中在“集合互斥 + 读合并 + commit 压缩”；双字典 diff（C）把语义收敛为“最终状态为真相”，但需要明确 dirty tracking 与 diff 的覆盖面（包含删除）。
> - 建议用 property tests 固化：最后写赢、Count/枚举一致性、删除重放一致性、以及序列化格式不变式（keys 升序/无重复/delta 可还原）。

> **2025-12-19 DurableDict：把 Q1/Q2/Q3 收敛为“可写进规范”的条款（Commit/Dirty/Diff Canonicalization）**
>
> - Q1：Commit 成功后必须满足 `CommittedState == CurrentState`（语义相等）且二者对后续写入**逻辑隔离**；实现上允许深拷贝/不可变结构共享/COW，但禁止“交换引用后把 working 清空”的惊讶语义。
> - Q2：MVP 至少需要 `HasWritesSinceCommit`/`HasChanges` 作为 $O(1)$ fast-path；dirty-keys 仅作为可选性能优化，避免过早复杂化。
> - Q3：要求 **Canonical Diff**：仅当 committed 与 current 在该 key 上语义不同才输出 diff；“新增后删除/设置回原值”等 net-zero 变更必须被消除，避免幽灵 tombstone 污染版本历史。
> - 文档措辞治理：禁止用“内存态”这种跨层词；固定三词：Working/Materialized State、ChangeSet/Write-Tracking、On-Disk Diff，并明确“哨兵对象/tombstone 只能属于 ChangeSet 内部表示”。

> **2025-12-19 DurableHeap/DurableDict：定义术语必须保持“显式命名一致性”**
>
> - 当文档已经在某一节给出规范化术语（例如 **Working State / Committed State / ChangeSet / On-Disk Diff**），其后续出现应尽量保持同一写法（含大小写与括注），避免在规范段落中混用 `committed state`（小写口语）与 `Committed State`（定义术语）。
> - “层级/分层”标题应准确覆盖对象：若把 **On-Disk Diff** 纳入列表，措辞应避免说成“内存中的状态”，以免读者误以为它属于 runtime state。

> **2025-12-19 DurableHeap：判别字段（Kind）命名统一，减少格式歧义**
>
> - 对二进制格式来说，`Kind` 这类判别字段属于“读路径的第一分支”，命名不一致会直接诱发实现分叉（尤其在恢复/跳过 record 的逻辑里）。
> - 建议以“层级”为维度统一：顶层 record 判别统一为 **RecordKind**（适用于 meta/data；必要时按文件域划分取值表或分配域位），对象 payload 内部判别统一为 **ObjectKind**（仅选择对象级 codec）。
> - 规则化约束：`Kind` 只用于 discriminator；若要强调域，用 `MetaRecordKind/DataRecordKind` 作为说明性别名，但规范正文仍以 `RecordKind` 为上位词。

> **2025-12-19 DurableHeap：术语清单的“层数”也属于术语一致性（QC 记录）**
>
> - 当文档使用“X 层语义/四层模型”这类表述时，标题与列表项数量必须一致；否则读者会怀疑术语边界是否稳定。
> - 在同一节里若必须保留小写口语（如 `committed state`）用于“泛指状态概念”，建议显式标注为“非术语（generic phrase）”，或直接统一替换为已定义术语（如 **Committed State（Baseline）**）。

> **2025-12-19 DurableHeap：术语规范审计——“概念名”与“代码名”必须分层**
>
> - 在设计草稿中，建议把**概念层术语**（例如 **Working State / Committed State (Baseline) / On-Disk Diff / Snapshot / Identity Map / Dirty Set**）与**实现层标识符**（例如 `_current/_committed/_dirtySet/Ptr64/DiffPayload`）显式分层：概念层用统一的规范化词表与大小写，代码名仅在“实现映射”小节出现。
> - 动词类阶段名（Deserialize/Materialize/Resolve/Commit）要么统一作为 API 名（PascalCase），要么统一作为过程名（小写动词短语），避免“同一段同时把它当术语又当函数名”，否则读者会混淆职责边界（例如把 `DurableDict.Commit()` 误解为“写 meta 的全局提交点”）。
> - “Baseline/Base/Snapshot/BaseVersionPtr”是最容易漂移的词簇：Baseline 应只保留为“上次 commit 的已提交状态”；Base/Snapshot/Checkpoint 归入“版本链分段/封顶策略”，并在词表中强制单义。

> **2025-12-19 DurableHeap：FlushToWriter 的两阶段语义（避免“假提交”）**
>
> - 若将 `FlushToWriter` 定义为“对象级：计算 diff 并写入 writer（非提交）”，它就不应在成功写入后立刻更新 `_committed` 或清空 dirty；否则当 heap 级 commit 在后续步骤（例如写 meta commit record / fsync）失败时，内存会出现“看似已提交但磁盘未提交”的假提交状态，违反“Commit 失败不改内存”。
> - 更稳妥的落点是两阶段：`FlushToWriter` 仅产生/写出 DiffPayload（可视为 prepare），heap 级 commit point 成功后再统一回调 `OnCommitSucceeded()`（或批量 `FinalizeCommit()`）来更新 `_committed`、清空 ChangeSet/Dirty Set；失败则不触碰内存状态并允许 retry。

> **2025-12-19 DurableHeap：ELOG framing 的“Magic 哨兵/分隔符”歧义与 reverse-scan 校验点（规范审计）**
>
> - 发现潜在 P0 规格歧义：`Magic` 既被描述为 record header 字段，又被描述为文件级 separator/尾部哨兵；若不收口会导致 reverse scan 的 `MagicPos`/`End` 语义分叉。
> - 如果采用“record header 的 Magic + 文件末尾孤立 Magic 哨兵”的方案，reverse scan 的下一轮边界应更新为 `MagicPos = Start`（当前 record 的起点也是前一条 record 的 End），而不是 `Start - 4`；否则会落入上一条 record 的 CRC 字段。
> - `DataTail` 必须精确定义是否包含尾部哨兵（更推荐 `DataTail = EOF` 包含哨兵），否则恢复 truncate 会破坏“文件以 Magic 结束”的不变量。

> **2025-12-19 DurableHeap：规范审计结论——命名/链接/示例一致性是“可开工”门槛**
>
> - 文档若宣称“概念层/编码层/实现层分离”，则必须把实现标识符（如 `_current/_committed`）收口到 Implementation Mapping；否则规范会被 reference implementation 绑死，后续演进难以审计。
> - 格式规范（Markdown）里“相对链接可达性”属于硬质量门槛：例如从 `DurableHeap/docs/` 指向仓库根 `atelia/` 的链接若写错，会让读者无法验证关键实现提示，降低规范可信度。
> - 伪代码必须与 commit point 一致：只要 meta commit record 是对外可见点，对象级 `FlushToWriter` 就必须是 prepare-only；否则会出现“假提交”，并在失败重试时丢失 dirty 信息。

