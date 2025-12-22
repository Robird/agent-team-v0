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

> **2025-12-22 记忆维护工程化：把“整理记忆”定义成可审计的 Compaction（条款 ID + 维护日志 + QA 验收）**
>
> - 将记忆维护视为“可回放的压缩/重整”，而不是随手删改：每次维护必须留下审计线索（谁/何时/改了什么/为什么/如何验证）。
> - 条款编号建议采用稳定语义锚点（如 `[MM-TRIG-LINES]`），避免纯序号重排导致引用崩溃；日志条目引用条款 ID 实现可追责闭环。
> - 备份与回滚必须二选一闭环（Git commit 或 archive snapshot），并通过“认知不变量 + 冷启动阅读测试 + 变更计数”防止过度压缩丢关键知识。
> - 规范落地的常见破口不是缺条款，而是 **SSOT 不唯一**：速查表/附录条款 ID 不一致、归档/备份路径出现多套真相，会让执行与审计分叉；应强制“条款 ID 与路径 SSOT 唯一化”，并把维护日志/归档材料钉死为 append-only。
> - 对 OnSessionEnd 写入机制，必须把“更新”拆成可判定写入语义：`OVERWRITE | APPEND | MERGE | TOMBSTONE`，并配套路由规则（State/Knowledge/Log 分流），否则默认行为会退化为 append-only。

> **2025-12-22 命名方法论工程化：用“命名表面矩阵 + 约束表”把畅谈会变成可审计流程**
>
> - 畅谈会法（方法 C）的短板不是“缺观点”，而是缺可复制的**产物定义**：Name Surface Matrix（哪些名字表面、哪些已 SSOT 锁定）、Constraint Table（MUST/SHOULD/MAY + 违反后果）、Candidate Scorecard（统一维度对比）、Decision Record（结论+理由+兼容策略）。
> - 在“格式名已定”的场景，代码标识符应优先对齐 SSOT；口感问题用更精确的后缀（Reader/Writer/Encoder/Decoder）解决，而不应引入第二套词根。

> **2025-12-22 命名收敛：面向 LLM 的“搜索词命中”优先于实现特征**
>
> - 命名评审可以引入一个简单启发式：用“你会在 GitHub/文档里搜什么词来找到这种能力？”来替代实现特征争论。
> - 对二进制格式，Magic 的可读性与版本化（如 `RBF1`）直接影响排障与演进成本；同时要在规范里钉死“版本何时换 Magic”。

> **2025-12-22 Layer 0 对齐复核：数值精确性与可测试性“断点清单”**
>
> - 规格里出现“多项式/hex 常量”时，必须同时钉死**表示法**（normal vs reflected、字节序列 vs u32 端序），否则实现会在“看似只是注释”的地方分叉。
> - 逆向扫描/对齐算法最常见的 bug 是**首帧/空文件边界**；把这些边界写成最小 test vector（如“仅 Genesis + 1 条空 payload frame”）能立刻暴露 off-by-4 / >=8 这类错误。
> - 当接口层（Tag+Payload）与格式层（wire layout）拆分时，P0 风险是“接口能说清楚但 wire 没写清楚”（例如 FrameTag 的编码位置）；必须把接口抽象回链到字节布局，否则测试无法黑盒判定。

> **2025-12-22 RBF 接口规格复核：把“实现细节争论”收敛为“可测试不变量”**
>
> - 条款写作优先锚定**可观测后置条件**：例如 Auto-Abort 的核心不变量应是“逻辑上该帧不存在 + 后续可继续写”，而不是“必须写某种字节序列”。
> - 对同一行为允许多实现路径时，条款应写成“逻辑一致 + 物理双路径（SHOULD/MUST fallback）”，避免把优化路径（Zero I/O Abort）与保底路径（Padding 墓碑）写成互斥冲突。
> - 只要规范正文出现“Reader MUST …”但 Reader 未被接口化，就会形成测试分叉（Scanner 是否跳过 Padding）；修复方法是明确职责边界并写入条款。
> - 对 durability（fsync）这类跨平台难断言的行为，若不在本层承诺，就不要用具体类型暗示（如 `FileStream.Flush(true)`）；把顺序与 durable flush 留给 commit 层（Layer 1）条款化。

> **2025-12-16 StateJournal：Persist-Pointer 的“两层引用”与 LMDB-ish 提交流程**
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

> **2025-12-21 规范审计：条款 ID 与示例命名漂移会破坏“可审计闭环”**
>
> - 条款编号即便满足“只增不复用”，若在正文中出现顺序长期乱序（例如较小编号在后文才首次出现），人工审阅会误判为漏条款；同时也增加未来自动抽取/索引生成的复杂度。更稳妥：首次引入按编号递增，或明确写入“出现顺序不要求单调”的元规则。
> - 规范性句子若使用“必须/不得/必须写死”等口语表达，却没有落到 RFC 2119 关键字 + 条款 ID，会形成不可测试/不可引用的“暗契约”，高概率诱导实现分叉。
> - 图表/伪代码中的函数名若与正文术语不一致（例如流程图仍用旧名 `WriteDiff()` 而正文已收敛为 `WritePendingDiff()`），会成为实现者的默认真相来源；应把示例命名纳入规范一致性检查清单。

> **2025-12-21 StateJournal 实施可行性审计：三类“可测试性阻塞点”**
>
> - **条款互相打架** 比“缺条款”更致命：例如 Detached 状态下“任何访问必须抛异常”与 “`State` 属性必须不抛异常（含 Detached）”会让测试无法写成黑盒判定。经验修复：把 API 分成“元信息（introspection）”与“语义数据访问（operational）”，并在规范里列清单。
> - **失败通道不唯一** 会在实现与测试之间制造不可见分叉：同一段落混用“返回失败/抛异常”但没钉死规则时，团队会自发补洞。经验修复：选择一种对外失败协议（Result 或 Exception 为主），另一种只作为 bug/不变量破坏通道，并强制 ErrorCode 可断言。
> - **可观察返回未钉死（null vs NotFound Result）** 会把上层调用模式撕裂成两套，后续改动成本极高。经验修复：在 MVP 里宁可“丑但唯一”，也不要“优雅但二义”。

> **2025-12-20 StateJournal MVP v2 审计补充：编号闭环与 SSOT 重复项是“规范可信度门槛”**
>
> - 条款编号体系只要出现“跳号但未声明 Deprecated”（例如 `[S-05]` 缺失），读者会立刻怀疑编号规则是否真的可审计；修复成本低但信任收益极高。
> - Glossary（SSOT）若出现同名术语重复条目（例如 `Address64` 两条定义），会让实现者与未来工具链无法确定权威定义；必须强制“一术语一锚点”。
> - 规范正文若宣告“非泛型容器”，就应避免在正文叙述中使用 `DurableDict<...>` 形式（哪怕是描述性），否则会诱发实现分叉心智模型。
> - 若文档声明“（MVP 固定）都应映射条款编号”，就要么补齐编号，要么撤销该元规则；否则属于自洽性破口（元规则 vs 实际执行不一致）。

> **2025-12-20 StateJournal MVP v2 审阅：伪代码“看似泛型”会诱发实现分叉**
>
> - 当规范已经把 Dict key 收敛为 `ulong` 时，伪代码不应再以 `DurableDict<K,V>` 形式出现并依赖 `(ulong)(object)key` 这类强转；这会让读者误以为实现可泛型化，复制后产生 runtime cast 风险。
> - 更稳妥：要么把类型签名直接写死为 `DurableDict<V>`（key 固定 `ulong`），要么把 `_dirtyKeys` 改为 `HashSet<K>` 并在规范层把 `K==ulong` 写成编译期约束（否则示例与规范脱钩）。

> **2025-12-20 交叉讨论：优先级排序应以“实现正确性缺口”压过“命名洁癖”**
>
> - 术语/命名分层（例如 `Address64` vs `Ptr64`、Base/Genesis/Checkpoint）能显著减少实现分叉，但它们的主要收益是“降低歧义”。
> - 一旦发现会影响崩溃一致性或 API 行为的缺口（例如 commit 失败语义、新建对象/NotFound 的边界行为），它们必须被提升为 P0：否则不同实现会在关键路径上自发补齐，造成不可审计的不兼容。
> - 实务排序模板：先补齐“提交点/失败不改内存/恢复路径”的确定性条款 → 再收敛概念层术语 → 最后做可读性与冗余清理。

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

> **2025-12-16 StateJournal：varint 与“可跳过性”的边界条件**
>
> - “zero-copy”需要拆开看：对 `string/blob` 的 payload bytes 可以做到 zero-copy；对 `int` 这类标量必然要 decode（CPU 计算），不应把“必须解码”与“复制 payload bytes”混为一谈。
> - `varint` 的关键代价不是“复制”，而是 **失去 O(1) 随机定位能力**：一旦把 offset/length table 做成变长，就会迫使 wrapper 扫描才能定位成员边界，破坏 lazy random access。
> - “先写 data 再写 length（且不回填 header）”会让记录从 Ptr 出发无法确定尾部位置，从而无法局部跳过 record、也无法只读 header 就建立惰性索引；MVP 更稳妥是 **Header+Footer 都有 TotalLen**（写完回填 header），footer 同时用于尾部回扫与损坏检测。

> **2025-12-20 StateJournal Decision Clinic：Detached 对象与“编码名/语义名”分层的必要性（规范审计）**
>
> - 当对象生命周期存在 “Detach（从 Dirty Set 移除强引用）” 分支时，必须把“仍可访问”的行为钉死为 **确定性契约**：要么 `MUST throw ObjectDetachedException`，要么 `MUST reset to Genesis/Empty 并保持可继续使用`；否则会产生实现分叉且难以写出可判定测试。
> - `Ptr64` 作为 wire-format 名称如果同时被读者理解为“指向 record 起点”，会与 `DataTail = EOF` 这类字段冲突；更稳妥的分层是：`Ptr64` = **4B 对齐 file offset 的编码**（`u64 LE`，`0=null`），`Address64`/`ObjectVersionPtr` = **语义子集：必须指向 record 起点**。这样既不引入新术语，也能把校验规则写到正确的层级。

> **2025-12-20 规格共识形成：P0/P1/P2 投票清单是“可审计收敛器”**
>
> - 以“是否会造成实现分叉/一致性风险”为 P0 标准，把争论从偏好（命名洁癖/风格）拉回到可验证的行为契约（失败语义、边界行为、分层定义）。
> - 投票条目应该写成“可落地修订动作”（可直接映射到文档 diff），并要求每票一句理由；这让共识可回溯，也方便后续把条目变成 PR checklist。
> - 对规范类文档，先锁死“提交点/失败不改内存/NotFound 行为/新建对象生命周期”等条款，再做术语与可读性清洁，能显著减少后续重写成本。

> **2025-12-20 规范一致性审计：Alias 术语不应进入规范正文**
>
> - 当术语表声明 SSOT 且给出 alias（例如 `Dirty Set` ↔ `Modified Object Set`），**规范正文与条款编号段**应强制只使用一个规范写法；alias 只在术语表/迁移说明中出现。
> - 原因：alias 一旦进入正文，会在实现/测试向量/错误信息里形成第二套“事实来源”，导致“名词漂移”在后续版本很难收口。
> - 同理，像 `Epoch`（口语概念）与 `EpochSeq`（字段/术语）这类词簇，必须在规范段落中强制统一到一个可测试的术语（建议统一到 `EpochSeq`）。

> **2025-12-20 Markdown 规范审阅：用空格替代 Tab，减少渲染分叉**
>
> - 规范文档里大量使用嵌套列表时，Tab 缩进在不同 Markdown 引擎（CommonMark/GFM/静态站点）下更容易触发“意外代码块/列表断裂”。
> - 更稳妥的写作约定：统一使用空格缩进（例如嵌套层级用 2 或 4 spaces），并保持同一层级的缩进一致。

> **2025-12-20 文档瘦身会议：决策归档语义、条款分类与 Test Vectors 的“空壳风险”**
>
> - `docs/archive/` 的目录语义会把内容推向“可忽略死档”，不适合承载“未来实现分叉时的权威回溯依据”；决策/权衡更应进入 `docs/decisions/`（ADR 风格），并在主文保留一页以内的结论索引。
> - 条款编号分类以 `[F/A/S/R]` 更稳健：Format/Framing、API、Semantics（涵盖 commit 语义不变式）、Recovery；相比单独的 `Commit` 前缀，更能覆盖全域契约并减少歧义（Contract/Constraint/Commit）。
> - Test Vectors 允许“先立骨架后填内容”，但需要避免空壳：至少每个大类放 1 个 seed vector 用于钉死歧义，其余向量优先从 reference impl + tests 自动生成/导出以避免漂移。

> **2025-12-20 规范瘦身方法论：把“长文档问题”转写为“契约可测试性问题”**
>
> - 文档冗余的根因通常不是写啰嗦，而是把三类语义混写：Normative Contract（必须一致的契约）、Reference Algorithm（参考实现路径）、Rationale/ADR（取舍理由）。
> - 瘦身优先级：先抽出“可引用/可测试的契约核心”（编号 MUST/SHOULD + 边界行为 + 恢复语义）作为 SSOT，再把伪代码与 ADR 迁入附录/归档。
> - “代码即真理”只有在代码可编译、可执行、并纳入测试时成立；pseudo-code 的精确性是幻觉，适合当路标不适合作为规格主轴。
> - 最小可维护闭环：条款 ID ↔ Test Vectors（格式向量/恢复向量/failpoint），任何格式或语义变更都必须同步更新向量，避免靠叙述维持一致性。

> **2025-12-19 DurableDict：术语消歧的“禁用模糊词”经验（QC 记录）**
>
> - 即便在文档中提供了“术语映射”（例如把“内存态”映射到 Working State/ChangeSet），仍建议在规范段中**彻底禁用**这类跨层模糊词：它会在其他章节再次引入歧义，并削弱读者对三层语义边界的信心。
> - “Materialize（过程）→ committed state（结果）”与“Materialized State（名词）”极易发生术语碰撞；更稳妥的写法是把对外语义状态称为 Working/Current State，把 materialize 的结果明确称为 Committed Baseline/Committed State。

> **2025-12-20 StateJournal MVP v2：规范可实现性审计（关键坑位清单）**
>
> - `RBF reverse scan` 的空文件边界判定存在 off-by-4 风险：若定义 `MagicPos = FileLength-4` 且 `RecordEnd = MagicPos`，则“仅 `[Magic]`”时应判定 `RecordEnd == 0` 而非 `4`。
> - `Dirty Set` 若只存 `ObjectId` 不能防 GC，无法满足“防丢改动”的动机；必须强引用对象实例（或引入独立强引用表）。
> - `RecordKind` 在 data/meta 两域都用 `0x01` 时必须声明“域隔离”，否则实现者很容易误用单一枚举表。
> - `ulong` 作为值类型与 `Val_VarInt(ZigZag)` 不匹配：要么新增 `Val_VarUInt`，要么收紧值类型为有符号。

> **2025-12-16 StateJournal：纯 C# span-view vs C/FFI 低层的权衡**
>
> - `ReadOnlySpan<byte>` 的“临时构造”本质是（ptr,length）两个字段的轻量视图，通常可被 JIT 内联/消除；真实成本更多来自“映射视图切换/边界检查/分支”，而不是 span 本身。
> - 纯 C# 方案的关键是把 `unsafe` 收口在“AcquirePointer + span 建立”的边界层，其余解析用 `BinaryPrimitives/MemoryMarshal` 走安全代码；可获得接近 C 的吞吐且大幅降低跨平台/构建复杂度。
> - C/FFI 方案更适合当你需要：多语言复用同一套解析、极致性能（减少边界检查/更自由的指针运算）、或需要复用既有 C 生态；代价是 ABI/部署/调试复杂度显著上升，并引入 native 崩溃面。

> **2025-12-16 协作偏好：以客观依据为准，欢迎反对不合理动议**
>
> - 记录：用户明确表示“没有强主观偏好，追求客观决策依据”，并鼓励我在发现不合理动议时直接反对/提出替代方案。
> - 启发：在 MVP 规格收敛时，应更激进地把“不确定性”转写为可验证的假设（benchmark / crash-injection / invariants），避免礼貌性含糊。

> **2025-12-17 StateJournal：把“可实现性”写进格式——CRC32C + DataTail + SortedDict 写入算法**
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

> **2025-12-19 StateJournal/DurableDict：定义术语必须保持“显式命名一致性”**
>
> - 当文档已经在某一节给出规范化术语（例如 **Working State / Committed State / ChangeSet / On-Disk Diff**），其后续出现应尽量保持同一写法（含大小写与括注），避免在规范段落中混用 `committed state`（小写口语）与 `Committed State`（定义术语）。
> - “层级/分层”标题应准确覆盖对象：若把 **On-Disk Diff** 纳入列表，措辞应避免说成“内存中的状态”，以免读者误以为它属于 runtime state。

> **2025-12-19 StateJournal：判别字段（Kind）命名统一，减少格式歧义**
>
> - 对二进制格式来说，`Kind` 这类判别字段属于“读路径的第一分支”，命名不一致会直接诱发实现分叉（尤其在恢复/跳过 record 的逻辑里）。
> - 建议以“层级”为维度统一：顶层 record 判别统一为 **RecordKind**（适用于 meta/data；必要时按文件域划分取值表或分配域位），对象 payload 内部判别统一为 **ObjectKind**（仅选择对象级 codec）。
> - 规则化约束：`Kind` 只用于 discriminator；若要强调域，用 `MetaRecordKind/DataRecordKind` 作为说明性别名，但规范正文仍以 `RecordKind` 为上位词。

> **2025-12-19 StateJournal：术语清单的“层数”也属于术语一致性（QC 记录）**
>
> - 当文档使用“X 层语义/四层模型”这类表述时，标题与列表项数量必须一致；否则读者会怀疑术语边界是否稳定。
> - 在同一节里若必须保留小写口语（如 `committed state`）用于“泛指状态概念”，建议显式标注为“非术语（generic phrase）”，或直接统一替换为已定义术语（如 **Committed State（Baseline）**）。

> **2025-12-19 StateJournal：术语规范审计——“概念名”与“代码名”必须分层**
>
> - 在设计草稿中，建议把**概念层术语**（例如 **Working State / Committed State (Baseline) / On-Disk Diff / Snapshot / Identity Map / Dirty Set**）与**实现层标识符**（例如 `_current/_committed/_dirtySet/Ptr64/DiffPayload`）显式分层：概念层用统一的规范化词表与大小写，代码名仅在“实现映射”小节出现。
> - 动词类阶段名（Deserialize/Materialize/Resolve/Commit）要么统一作为 API 名（PascalCase），要么统一作为过程名（小写动词短语），避免“同一段同时把它当术语又当函数名”，否则读者会混淆职责边界（例如把 `DurableDict.Commit()` 误解为“写 meta 的全局提交点”）。
> - “Baseline/Base/Snapshot/BaseVersionPtr”是最容易漂移的词簇：Baseline 应只保留为“上次 commit 的已提交状态”；Base/Snapshot/Checkpoint 归入“版本链分段/封顶策略”，并在词表中强制单义。

> **2025-12-19 StateJournal：FlushToWriter 的两阶段语义（避免“假提交”）**
>
> - 若将 `FlushToWriter` 定义为“对象级：计算 diff 并写入 writer（非提交）”，它就不应在成功写入后立刻更新 `_committed` 或清空 dirty；否则当 heap 级 commit 在后续步骤（例如写 meta commit record / fsync）失败时，内存会出现“看似已提交但磁盘未提交”的假提交状态，违反“Commit 失败不改内存”。
> - 更稳妥的落点是两阶段：`FlushToWriter` 仅产生/写出 DiffPayload（可视为 prepare），heap 级 commit point 成功后再统一回调 `OnCommitSucceeded()`（或批量 `FinalizeCommit()`）来更新 `_committed`、清空 ChangeSet/Dirty Set；失败则不触碰内存状态并允许 retry。

> **2025-12-19 StateJournal：RBF framing 的“Magic 哨兵/分隔符”歧义与 reverse-scan 校验点（规范审计）**
>
> - 发现潜在 P0 规格歧义：`Magic` 既被描述为 record header 字段，又被描述为文件级 separator/尾部哨兵；若不收口会导致 reverse scan 的 `MagicPos`/`End` 语义分叉。
> - 如果采用“record header 的 Magic + 文件末尾孤立 Magic 哨兵”的方案，reverse scan 的下一轮边界应更新为 `MagicPos = Start`（当前 record 的起点也是前一条 record 的 End），而不是 `Start - 4`；否则会落入上一条 record 的 CRC 字段。
> - `DataTail` 必须精确定义是否包含尾部哨兵（更推荐 `DataTail = EOF` 包含哨兵），否则恢复 truncate 会破坏“文件以 Magic 结束”的不变量。

> **2025-12-19 StateJournal：规范审计结论——命名/链接/示例一致性是“可开工”门槛**
>
> - 文档若宣称“概念层/编码层/实现层分离”，则必须把实现标识符（如 `_current/_committed`）收口到 Implementation Mapping；否则规范会被 reference implementation 绑死，后续演进难以审计。
> - 格式规范（Markdown）里“相对链接可达性”属于硬质量门槛：例如从 `atelia/docs/StateJournal/` 指向仓库根 `atelia/` 的链接若写错，会让读者无法验证关键实现提示，降低规范可信度。
> - 伪代码必须与 commit point 一致：只要 meta commit record 是对外可见点，对象级 `FlushToWriter` 就必须是 prepare-only；否则会出现“假提交”，并在失败重试时丢失 dirty 信息。

> **2025-12-20 “瘦身悖论”复盘：行数不是目标，SSOT + 可测试闭环才是**
>
> - “瘦身”若以行数/字数衡量，会与规范工程的核心目标冲突；真正可度量的目标应是：冗余度（同一事实/约束是否双写）、可判定性（无需作者意图即可判真伪）、可追踪性（条款 ID ↔ 测试向量/失败注入）。
> - 元数据基础设施（条款编号、索引、映射）通常是必要增量；“新冗余”来自“双写”：结构化表述与自然语言复述并存、Index 与 ADR 边界不清、图表与线性定义重复。
> - 若要真的减少行数，应优先删 Informative 层（rationale/权衡解释/重复示意），并把机械映射表转为生成物（文档只引用生成摘要/链接），从根源减少漂移风险。

> **2025-12-20 Rationale Stripping 边界：以“可判定性/实现不变性”做硬裁判**
>
> - 建议把 Rationale 的边界写成可执行规则：删除某段后，陌生实现者是否还能正确实现？测试者是否还能写出可判定测试？若两者都不受影响，则该段属于 Rationale/认知脚手架，应迁 ADR 或删除。
> - “说明/备注/Note”是高风险容器：若包含约束，应升级为 MUST/SHOULD 条款并删除 Note；若只是解释，应剥离。
> - 例外：会改变错误恢复与交互闭环的“UX 提示”属于接口契约的一部分，不应被误删为 Rationale。

> **2025-12-20 ASCII vs 线性定义：禁止双写，SSOT 选更可测试的表示**
>
> - 对 LLM-first 规范正文，ASCII 图通常是低带宽/高 token 的噪音；更稳的 SSOT 是 EBNF/format-string/字段表等线性可解析表示。
> - ASCII/mermaid 若保留，应作为人类 DX 的衍生物，并删除等价的叙述性段落以避免漂移。

> **2025-12-20 StateJournal Spec Review：把“分散定义/隐性概念”转写为 SSOT + 命名契约**
>
> - “ObjectId 保留区/Well-known Id”这类身份模型规则一旦分散在 Open/首次 Commit/条款里，会制造实现分叉风险；应收敛为 Glossary/单一权威定义，并让正文只引用不双写。
> - “Shallow Materialization”即使在正文里描述正确，只要没有被命名并纳入 Glossary，读者仍容易把“全量 materialize”误解为“对象图递归加载”；命名 + cross-ref 是最低成本的降歧义手段。
> - 优先级经验：P0 优先修“会导致 allocator/reader 行为分叉”的 SSOT 断裂；P1 修“关键语义但缺少概念名”的隐性规则；P2 再做叙事/可视化增强。


> **2025-12-20 规格讨论纪要审阅：把“提案投票”写成可测试契约，而不是偏好清单**
>
> - 在二进制格式/恢复语义的讨论里，“是否接纳提案”并不足以避免实现分叉；每个提案都需要最小化的 **Normative Contract**（MUST/SHOULD/MAY）来钉死未知值处理、校验策略与失败语义。
> - Markdown 作为规格载体时，格式一致性本身是工程风险：孤立的 fenced code block、列表缩进漂移，会直接导致阅读与审阅误判；建议把“字段表/编码表”作为 SSOT，其它段落只引用解释。

> **2025-12-20 StateJournal MVP v2 最终轮确认：元规则（MVP 固定→条款编号）必须闭环**
>
> - 当规范文档在“规范语言/编号规则”里宣告“所有（MVP 固定）约束必须有 `[F/A/S/R-xx]` 条款编号”时，这句话本身就变成了可审计契约。
> - 若正文大量使用“（MVP 固定）”却不配套编号，会形成“规则写了但没执行”的裂缝：读者无法判定哪些是可测试的硬约束，未来也难以建立条款→测试向量的映射。
> - 收敛方式应二选一：要么撤回/弱化该元规则（把（MVP 固定）降为强调文本），要么保留元规则并补齐最小编号条款集合（优先覆盖会导致实现分叉的关键路径）。

> **2025-12-21 秘密基地畅谈（最终确认轮）：把“Agent 友好性”落为可测试的协议面**
>
> - 最终决议里把三件事提升为 P0/P1（`DurableObjectState`、条款可寻址性、Error Affordance）是正确的：它们分别对应 **可观测性/可引用性/可恢复性**，是让人类与 LLM/Agent 都能稳定使用的底座。
> - 对 `DurableObjectState`，仅列枚举值还不够：需要同时钉死“哪些 API 在哪些状态合法”与“跃迁是否闭集”，否则实现会用额外隐含状态补洞，造成分叉。
> - 对 Error Affordance，“结构化字段”应至少包含稳定 `ErrorCode`（MUST），其余上下文字段（ObjectId/State/RecommendedAction）可作为 SHOULD/MAY；这样才能写出可回归测试，并让 Agent 的恢复策略可判定。

> **2025-12-21 Decision Clinic Round 1：状态闭集与错误码的“可判定测试面”收敛**
>
> - `DurableObjectState` 的“闭集完备”需要用**排除法**写清：未加载/损坏/heap disposed 等不应被塞进 state，而应作为 Load/Dispose 错误路径（否则状态机混入 I/O，测试不可判定）。
> - `State` 属性应被定义为 **total / O(1) / non-throwing**（Detached 也能读），否则实现会迫使调用方用 try/catch 探测状态，形成不可预测分支。
> - `DiscardChanges(Transient)` 后的 ObjectId 复用若不钉死，会与“同一时刻 ObjectId 唯一”条款潜在冲突；更稳的 MVP 契约是 **process-lifetime quarantine**：同进程不复用、跨崩溃可复用。
> - Error Affordance 的最小闭环是“ErrorCode MUST + 结构化字段 SHOULD/MAY + Appendix B 向量逐条断言 code”，自然语言只做 display。

> **2025-12-21 StateJournal Hideout Round 2：AteliaResult/AteliaError 作为跨项目失败协议（规范审计）**
>
> - “机制级别”应上提到 **Atelia 项目基础机制**：`AteliaResult<T>` + `AteliaError`，避免 StateJournal/DocUI/PipeMux 各自发明 Result/Error 类型导致适配器扩散。
> - 现有条款（如 `[A-ERROR-CODE-MUST]`）当前措辞限定为“异常”，但 MVP 设计已转向 Try 方法返回结构化结果；建议把条款适用面扩展为“对外可见失败载体（异常 + Result.Error）”，否则字段与恢复策略会出现双轨漂移。
> - 强类型与通用性的折中：协议面稳定键押注 `ErrorCode: string`（可登记/可测试/可跨语言），强类型派生类仅用于库内部 DX；序列化/对外投影必须可降级为基础字段。
> - DocUI 的 Error Affordance 应是“渲染与交互层”（如何引导恢复），而不是错误对象的 SSOT；错误对象 SSOT 放在 Atelia 基座更利于一致性与复用。

> **2025-12-21 条款可寻址性：稳定语义锚点（Semantic Anchor）命名纪律**
>
> - 位置型编号（如 `[S-17]`）适合作为“阅读索引”，但不适合作为跨文档/测试/代码的稳定引用键；推荐用稳定语义锚点（如 `[S-OBJECTID-RESERVED-RANGE]`）作为机器可读 key。
> - 锚点名优先选择“可测试的语义维度”（字段名/不变式/错误策略），避免把 MUST/SHOULD 写进锚点名（规范级别会变，但语义锚点应保持稳定）。
> - 命名长度以 2-4 个词（不含 `F-/A-/S-/R-` 前缀）更利于 grep 与测试名映射；必要时用术语化缩写（如 `CRC32C`、`PTR64`），但避免引入第二套缩写词表。

> **2025-12-21 StateJournal 命名与归属：品牌名/技术名分层 + NuGet/namespace 的“不可逆成本”**
>
> - “是否改名”建议分两层：**品牌名（package/repo）**可相对稳定以降低外部迁移成本；**技术名（public API 类型/组件名）**必须对齐真实语义，避免 `Heap` 暗示随机访问/allocator 语义。
> - NuGet `PackageId` 与 public `namespace` 属于高不可逆资产：不建议用 `*V2/*2` 这类临时后缀固化在包名；更稳妥用 **SemVer major bump** 表达语义断裂，并把格式版本放到 `FormatVersion`（与 API version 解耦）。
> - Repo 归属优先按依赖方向与发布节奏裁决：底层存储不应依赖 UI（避免层级反转）；若主要服务 Agent runtime/历史存储，放进 atelia 往往更自然；独立 repo 仅在明确存在跨生态消费者与“协议化”目标时才值得。

> **2025-12-21 命名投票经验：优先“稳定语义/用例名”，把实现术语留在内部**
>
> - 当底层实现仍在演进（diff 编码、对象图布局、GC/compaction 策略）时，repo/package 的名字更应锚定“对外不变的语义与用例”（如 State + Journal/Append-only），避免把短期实现细节（如 DeltaGraph）固化为外部心智模型。
> - 实现导向名可以作为内部组件/子模块名（codec、index、graph 等），但对外优先选“使用者第一眼就能预测行为边界”的命名。

> **2025-12-20 StateJournal Decision Clinic：ObjectId 分配时机是“身份模型”决策，不是 allocator 细节**
>
> - 只要规范把 Identity Map / Dirty Set 的 key 钉死为 `ObjectId`（如 `[S-02]`），那么“延迟到 commit 才分配 ObjectId”就会迫使引入第二套身份（TransientKey/CreationSeq），属于跨章节的身份模型重写。
> - 立即分配并不与 `NextObjectId`“仅在 commit record 持久化”矛盾：只需把契约边界写清——未提交分配在崩溃后允许被重用（因为从未进入 VersionIndex/HEAD），并用测试向量固化。
> - 可测试性优先级：A 能写成纯 black-box 的 crash/reopen 向量；B 为可判定性必须规定 commit 内分配顺序与可观察状态，测试与实现细节强耦合，增加实现分叉风险。

> **2025-12-20 API 语义审计：同名操作的“baseline 缺失”必须显式入规范**
>
> - 当同一个 API（例如 `DiscardChanges()`）同时作用于 Persistent 与 Transient 两类对象时，最大的分叉源不是实现细节，而是“是否存在可回滚的 baseline”。
> - 若 baseline 不存在（Transient），规范必须二选一并写成状态机：**MUST detach（fail-fast）** 或 **MUST reset-to-Genesis 并保持可继续使用**；任何“暗示可以/不可以”而不钉死 `LoadObject/NotFound/IdentityMap` 的写法都会制造不可判定测试与实现漂移。
> - 经验法则：凡涉及“撤销/回滚/丢弃”，先写清 *rollback target*（Last Committed vs Genesis）与 *object existence*（detached vs attached），再谈 UX/易用性。

> **2025-12-20 StateJournal MVP v2 Round 2 交叉讨论：把“保留区/兼容策略/可观察性”写成三条硬契约**
>
> - `NextObjectId` 统一只是表象，真正需要锁死的是：allocator 对保留区的 **MUST NOT 分配**，以及 reader 遇到“保留区但未知含义”的 **fail-fast vs ignore** 策略；否则扩展期仍会实现分叉。
> - `CommitAll` 是否拆分 API 是次要的；P0 是：**commit 失败后内存状态 MUST NOT 改变**，并提供对 Agent/LLM 可见的 `CommitResult`（至少包含 orphan/reachability 诊断），避免仅写日志导致不可见风险。
> - `RecordKind/ObjectKind/ValueType` 除了集中枚举表，更必须写明 **unknown kind 的处理策略**（建议 MUST fail-fast），否则前向扩展会引入 silent corruption。
> - 作为 DocUI 语义化过渡，可考虑 well-known 的 “Key Interner”（`string↔ulong` 映射）承载可读 key，但必须明确它是迁移桥而非最终数据模型，避免隐式规范。

> **2025-12-20 秘密基地畅谈会 Round 2：把“共识”压缩成可回归测试的条款集合**
>
> - Round 2 的价值不是重复表态，而是把“结论句”变成**实现与测试可判定**的契约：例如把“否决 MSB hack”写成 `PrevVersionPtr MUST NOT carry flags`，把“预留 0-15”写成 `allocator MUST NOT emit 0..15`。
> - 对二进制格式演进，`ObjectKind` 复用做版本化时，最容易遗漏的是 **unknown kind 的处理策略**；必须显式选择 fail-fast（MUST）以避免“静默跳过”造成数据丢失。
> - 去泛型的关键不在命名，而在 **API 诚实性**：必须禁止“隐式转码写入”（如 JSON/stringify）这种实现自发补齐的行为，否则会制造跨实现的不兼容数据。

> **2025-12-20 规格审计：条款编号系统的“子前缀分叉”会直接摧毁可审计性**
>
> - 当规范声明条款 ID 采用 `[F/A/S/R-xx]` 且“只增不复用、映射测试向量”，就必须禁止（或正式化）类似 `[F-VER-01]` / `[S-CB-01]` / `[A-DD-01]` 这类临时子命名空间；否则索引/审计/测试映射需要多套解析规则，最终退化为“编号只对作者有意义”。
> - 经验法则：要么 **统一扁平编号**（推荐），要么把 ID grammar 写进规范（定义 Topic 集合、递增规则、弃用规则与测试映射规则）。两者不可混用。
> - 同类风险：把 Format/Framing 内容标成 `[S-xx]` 会污染分类边界，导致测试向量归类错位（Format vs Semantics），进一步削弱可维护性。

> **2025-12-21 秘密基地畅谈 Round 2：条款“显示编号”与“稳定语义锚点”分层 + SSOT 内联摘要纪律**
>
> - 对“条款重新编号”方案持赞同态度：按文内首次出现递增能显著提升人工审计与未来自动抽取的可用性。
> - 同时建议把条款 ID 分层：`[S-17]` 作为阅读友好的 display index；可选引入稳定的语义 key（如 `S-ID-RESERVED-RANGE`）用于测试向量与外部引用，避免重排导致引用崩溃。
> - 强烈支持“SSOT + 内联摘要”：它与 DocUI 的 LOD 同构（Full=SSOT、Summary=正文摘要）。关键纪律是：摘要只能 restatement，不得引入新约束；任何新增 MUST/SHOULD 必须回落到 SSOT 条款块。
> - State 枚举与 Error Affordance 不是 DX 花活，而是让对象生命周期状态机“可观测/可恢复”的协议面；建议错误信息至少包含结构化字段（ErrorCode/ObjectId/State/RecommendedAction），自然语言仅作为 display。

> **2025-12-21 项目更名与迁移完成：StateJournal（投票经验落地）**
>
> - 命名收敛点：优先把名字锚定到“Agent 状态 + 仅追加写/版本链（journal）”这层稳定语义；把 diff 编码、对象图布局、GC/compaction 等留给内部实现演进。
> - 迁移落地后的权威入口统一到 `atelia/docs/StateJournal/`（核心：`mvp-design-v2.md`；Backlog：`backlog.md`），公开命名空间使用 `Atelia.StateJournal`，减少后续不可逆迁移成本。

> **2025-12-21 命名投票复盘：把“稳定语义”写成可迁移的共识**
>
> - 论证策略：优先用“对外不变的语义边界”取代“实现机制名”。例如 `StateJournal` 直接承诺“状态 + 追加写 + 版本链/可回放”，比 `Heap`/`Store` 更不容易随实现演进而过时。
> - 迁移动作：同步明确旧文档目录已删除、新路径 `atelia/docs/StateJournal/` 生效，并把 namespace 钉死为 `Atelia.StateJournal`，避免后续出现“文档已迁/代码仍旧名”的二次漂移。
> - 团队协作经验：投票理由要求一句话且能映射到后续 PR checklist（链接替换、命名空间、入口文档与 backlog），可把“偏好争论”收敛成可执行的迁移清单。

> **2025-12-21 API 命名审计：TryXxx 语义与结构化错误返回必须自洽（StateJournal）**
>
> - `TryLoadObject` 若作为公开 API 名，应遵循 .NET 的 Try-pattern 预期：**不以异常表达“可预期失败”**，并把失败信息放进结构化返回（或 out error）；否则调用方心智模型会分叉。
> - 若团队选择“结构化错误返回值”而非 `bool + out` 经典签名，需要在规范中明确：Try 系列方法允许返回 `Result<T>`（Success/Value/Error），并将“异常仅用于编程错误/不变量破坏”写成条款。
> - Error contract 建议双层：`ErrorCode: string` 作为稳定可注册键（可映射测试向量），`ErrorKind` 作为粗粒度类别（更利于 switch/策略），并定义其是否属于对外协议面。

> **2025-12-21 AI Team 元认知：Subagent 命名 grammar 与 runSubagent 的可审计字段**
>
> - 命名优先级：先保证“可机械解析”，再考虑好听；推荐 grammar：`<Role>-<Model>`，Role 取自受控词表（Advisor/Reviewer/Tester/Facilitator），Model 取自 {Claude/Gemini/GPT}（或更具体版本）。避免把“项目归属/品牌前缀”混进同一层命名，降低歧义与迁移成本。
> - 研讨会形式合并要避免“只有语气没有契约”：建议 `#review/#design/#decision` 三类标签绑定最低产物（FixList / 候选方案+tradeoff / ADR+回滚条件），否则讨论难以复用与审计。
> - `runSubagent` 的邀请信息应补齐可审计字段：`chatroomFile`、`targetFiles`、`appendHeading`、`scope`、`outputForm`、`language`（MUST）；`existingConsensus/openQuestions/timebox/verificationStep`（SHOULD）。其中 `verificationStep`（调用方与子代理都声明“已追加到文件末尾”）能显著减少“插入中间/写错文件”的返工。

> **2025-12-21 交叉讨论收口：错误即文档索引（AteliaError / ErrorCode / Details）**
>
> - `Message` 应默认面向 Agent（LLM-friendly），不再拆 `AgentMessage` 以避免双文案漂移；如需人类友好，交由 DocUI Presenter 做摘要/裁剪。
> - `Details` 约束为 `IReadOnlyDictionary<string, string>` 是务实且安全的跨边界选择；复杂结构允许 JSON-in-string。
> - `ErrorCode` 除“机器分派键”外应被视为“静态文档索引”：需要配套 ErrorCode Registry（SOP/示例/恢复路径），并提供 `help(code)` 类入口把运行时错误闭环到文档与处置流程。

> **2025-12-21 畅谈会 Round 2：AGENTS.md 协议收口的“唯一规范”原则（标题与调用卡片）**
>
> - 协议里最忌“同时鼓励两套写法”：例如 `### <Name> 发言` 与 `### <Name> 第 N 轮`。必须二选一收口（另一种只能作为可选元信息），否则索引/检索/工具化会立刻分叉。
> - `runSubagent` 的字段命名应定义一套 **canonical keys** 作为 SSOT，避免 `chatroomFile/聊天室文件`、`appendHeading/发言标题` 等同义词漂移；同理，`targetFiles` 的类型（list vs single）与示例写法必须一致。
> - 调用格式建议以“结构化字段”作为 SSOT（fenced YAML 或逐行 Key: Value）；Markdown 表格可作为人类友好渲染，但不宜承载协议参数（对齐/换行导致解析与复制粘贴不稳）。
> - Role 表示长期稳定身份（如 `Advisor-*`），具体任务用 `taskTag` 表达；不要把 `-Design` 这类语境性后缀烧进名字里，避免边界不清与过度设计。
