# Key-Notes 消化理解

> **最后同步时间**: 2025-12-19
> **Key-Note 数量**: 10
> **同步范围**: `DocUI/docs/key-notes/*.md`

---

## 整体架构理解（按 Key-Note 归纳）

DocUI 当前 Key-Note 体系在表达一个 RL 风格的 Agent 系统：

- **Agent / Environment**：用 RL 语言定义交互回路；LLM 被视为“内部状态转移函数”。
- **Agent-OS**：介于 LLM 与 Environment 之间的中间件，负责把系统状态渲染为 Observation、执行 Tool-Call 并维护 History。
- **DocUI**：Agent-OS 向 LLM 呈现“可感知世界 + 可执行动作”的界面层，采用 Markdown Document 作为主载体。
- **App-For-LLM**：对 Agent 能力的外部扩展机制（独立进程 + RPC），通过 DocUI 统一呈现给 LLM。
- **Context Budget / Abstract Token**：承认“有效上下文远小于最大窗口”这一工程现实，提供跨模型的抽象计量单位。
- **Error-Feedback / Cursor-And-Selection**：把“可恢复错误反馈”和“光标/选区表达”作为 Micro-Wizard 体系的重要支撑，减少重试死循环与复述成本。

这些 key-note 的关系可粗略理解为：

```mermaid
graph TD
	KN0[Key-Notes Drive Proposals] --> G[Glossary (Index)]
	KN0 --> KN1[LLM Agent Context]
	KN0 --> KN2[Doc as User Interface]
	KN0 --> KN3[App-For-LLM]
	KN0 --> KN4[Abstract Token]
	KN0 --> KN5[Error-Feedback]
	KN0 --> KN6[Cursor-And-Selection]
	G --> KN1
	G --> KN2
	G --> KN3
	G --> KN4
	G --> KN5
	G --> KN6
```

> 备注：术语治理已于 2025-12-14 落地为 **Primary Definition + Glossary-as-Index**：术语的权威定义分布在各 Key-Note 的“定义块”，`glossary.md` 回归为索引入口（摘要 + 链接 + 状态）。

---

## 各篇 Key-Note 摘要（逐篇）

### 0) `glossary.md`

**角色定位（当前文件现状）**
- DocUI 术语的**索引文件**：记录术语名称、摘要（复制定义块一句话定义）、定义位置链接、状态（Stable / Draft / Deprecated）。

**包含内容（当前覆盖）**
- Agent / Environment / Agent-OS / LLM
- Observation / Action / Tool-Call / Thinking
- Agent-History / HistoryEntry / History-View
- Context-Projection（弃用 Render）
- Window / Notification
- Capability-Provider / Built-in / App-For-LLM

**关键规则（文件末尾明确）**
- “App” 简称仅指 App-For-LLM；统称用 Capability-Provider。
- 复合术语使用连字符风格（Context-Projection、Capability-Provider、App-For-LLM 等）。

**Primary Definition 机制（已落地到 drive-proposals 与 glossary）**
- 每个核心术语在“首次引入它的 Key-Note”中定义，格式为：`##/### Term` 标题下第一段为引用块 `> **Term** ...`。
- 非定义源文档允许 restatement，但必须显式标注并指回权威定义。

### 1) `abstract-token.md`

**核心问题**
- 不同模型 tokenizer 不同且可能不可见，导致无法可靠地“精确计量某特定模型 token 数”。
- 工程上有效上下文长度显著小于最大上下文窗口（注意力稀释、计费等）。

**核心方案**
- 放弃对“模型特定 token”的精确计数，转而估算文本的跨模型抽象信息量单位：**Abstract Token**。

**候选计量方法**
1. 按字符类别线性加权拟合（快、简单，但边缘 case 偏差大）
2. 选定一个先进开源 tokenizer 作为统一计量基准

**开放问题 / TODO**
- 在“速度/一致性/可解释性/误差”之间选定最终实现。

---

### 2) `app-for-llm.md`

**核心定义**
- **App-For-LLM**：Agent 的外部扩展机制，把紧密相关的“数据 + 视图 + 操作”封装为整体供 LLM 使用。
- **DocUI**：LLM 与功能交互的界面层（渲染信息、管理可用操作、路由调用、反馈结果）。

**关键架构决策**
- DocUI 与 App-For-LLM **解耦**：
	- DocUI 作为统一交互界面，不关心能力来自内建还是外部。
	- **内建能力**（如 recap/history/context 统计）是 Agent 的“器官”，直接访问内部状态，不走 RPC。
	- **外部扩展**一律独立进程，走 RPC（PipeMux/JSON-RPC），带来隔离、语言无关、热更新。
	- 明确“不提供内嵌插件机制”，避免边界模糊。

**开放问题 / TODO**
- 已引入上位术语 Capability-Provider 统一指代 Built-in 与 App-For-LLM；后续可继续补齐接口与协议层面的契约。

---

### 3) `doc-as-usr-interface.md`

**核心命题**
- “LLM 是 Agent-OS 的用户”。LLM 获取信息与执行操作的界面被称为 **DocUI**，因主要以 Markdown Document 形式渲染。

**与 GUI/TUI/API 的类比**
- 形态上更接近 TUI/Web server 的文本渲染，但选择 Markdown 是因为：LLM 语料熟悉、语法噪声低；XML/HTML 噪声大；JSON 存在转义序列问题。

**Context 注入的两种形态**
1. **Window**：在 Context-Projection 时生成一份 Markdown 文档，作为“最新 Observation”的正文部分；强调 LOD（信息层级）控制。
2. **Notification**：对近期历史与事件流的注入；HistoryEntry 按 LOD 渲染为 Basic/Detail。

**层级关系澄清（新增）**
- 明确 Observation → History-View → (Window + Notification) 的包含关系。
- 明确 Recent History 是“选取策略/时间窗”，而非渲染形态。

**LOD 设计**
- Window LOD：`{Full, Summary, Gist}`
	- Gist：最小“What + 关键线索”，提供“提高 LOD 恢复认知”的入口
	- Summary：甜点级别（实用性 vs token），包含节点概述、重要链接、子节点列表链接
	- Full：原始信息
- Notification LOD：`{Basic, Detail}`

**待消化建议（高价值）**
- 引入 **Focus / Attention Focus**：操作会移动焦点；焦点对象 Full，相关对象 Summary，其余 Gist。
- LOD 应是“信息维度切换”，不只是“字数缩减”。
- 增加 **Diff/Dirty State**：显式标记自上次交互以来变化的部分，引导注意力。

**开放问题 / TODO**
- Summary 的树形/平铺渲染策略与展开时机未定。

---

### 4) `key-notes-drive-proposals.md`

**角色定位**
- Key-Notes 类似“宪法/关键帧”，定义设计关键轮廓，用于约束与指导 Proposal 的撰写方向。
- 起草主体是人类，AI 可辅助但需谨慎编辑。

**术语治理规则（当前落地，已重写）**
- **Primary Definition 原则**：术语权威定义在“首次引入该术语的 Key-Note”。
- **定义块格式**：`##/### Term` + 标题下第一段引用块 `> **Term** ...`（一句话定义）。
- **Glossary-as-Index**：glossary 是索引而非定义仓库，摘要需与定义块一句话定义文本等价。
- **Restatement 规则**：允许重述但必须显式标注/回链，不得改变定义边界。
- **迁移规则**：改名（Deprecated + 指向新名）；迁居（Redirect Stub 保留旧锚点）。
- **CI 校验规格（待实施）**：索引可达/锚点唯一、定义唯一性、摘要一致性、状态约束。

---

### 5) `llm-agent-context.md`

**概念总纲**
- 采用 RL 概念体系建模，同时短期务实沿用 chat 范式的常规 LLM。
- 借用心理学术语：关注“外在行为/内在行为”，强调术语借用仅指等效机制与行为层面。

**关键术语定义（该文为术语源头）**
- **Environment**：外部状态转移函数
- **Agent**：可感知环境并行动的计算实体；系统存在内部/外部状态转移函数交替作用
- **LLM**：内部状态转移函数（典型 autoregressive，也可能 diffusion）
- **Agent-OS**：LLM 与 Environment 之间的中间件；向 LLM 提供 Observation，执行 Tool-Call
- **Message**：单向分块传递（Half-Duplex）
- **Observation**：Agent-OS → LLM 的 Message（展示部分系统状态）
- **Tool-Call**：LLM → Agent-OS 的同步功能调用
- **Action**：LLM → Agent-OS 的 Message（Thinking + Tool-Call）
- **History**：Agent 状态的一部分；增量、仅追加、不可变
- **History-View**：用于向 LLM 展示的、由 Agent-OS 渲染出的 History 部分信息

**明确弃用/澄清**
- 弃用 **Human-User**：不是一对一问答；chat 范式 User 由 Agent-OS 取代
- 弃用 **To-User-Response**：除 Tool-Call 外的 LLM 文本不触发 Agent-OS 状态转移；需要对外说话必须走 Tool-Call（类比 print 从关键字变库函数）
- **Thinking/CoT**：Agent-OS 不解析，但它是 LLM 内部状态，影响后续 token 概率分布
- 区分“分块消息模型”与“传输层流式”；并讨论真正全双工流式 LLM 的含义

**LLM 调用的三层模型（实现映射）**
- **ICompletionClient**：厂商 API 适配层（OpenAI/Anthropic/Gemini 等）
- **IHistoryMessage**：跨厂商抽象调用消息（由渲染层生成，含 LOD 后的 ToolCallResult；并在最后 Observation 中包含 DocUI 渲染出的 App 信息）
- **HistoryEntry**：更完整的交互记录结构（ToolCallResult 含 Basic+Detail）

**可视化（新增）**
- 增加三层结构图与调用序列图，显式展示 HistoryEntry → Context-Projection → IHistoryMessage → ICompletionClient 的数据流。

**Context-Projection（DocUI 语境）**
- 从“活跃 HistoryEntry + AppState”投影/组装出用于 LLM 调用的一组 IHistoryMessage，并受 Token 预算与 Attention Focus 约束。

**开放问题 / TODO（该文明确列出）**
- 更准确的厂商 API 命名
- Render 术语可能过宽，需更精确命名
- 三层模型更好的命名与 Mermaid 图
- HistoryEntry 与 IHistoryMessage 的结构图
- IHistoryMessage 是否改回 IContextMessage、接口/类型的取舍

---

### 6) `UI-Anchor.md`

**核心命题**
- **UI-Anchor** 为 LLM 提供引用与操作 DocUI 可见元素的可靠锚点（句柄/ID）。

**三类锚点（该文已给出清晰的 Primary Definition 候选）**
1. **Object-Anchor**：`[Label](obj:<id>)`，用于指代实体对象并作为 Action 参数。
2. **Action-Prototype**：以函数原型公开操作接口（Live API Documentation），服务 REPL 范式。
3. **Action-Link**：`[Label](link:<id> "code_snippet")`，预填参快捷调用（通过 `click(id)` 工具触发）。

**交互范式（新增/强调）**
- 采用 **REPL（Read → Eval → Execute）**：LLM 先阅读 Prototype/Anchor，再生成代码调用或点击 Action-Link。

**开放问题（值得进入术语与协议层）**
- Anchor 生存期：持久（UUID/GUID，悬空引用） vs 临时（Context-Projection 动态分配，绑定可见性）。文中倾向后者，并建议“可持久化引用”另起机制。

**一致性风险提示**
- `obj:` / `link:` / `file:` 等 URI scheme 尚未进入 glossary；且锚点与 Context-Projection/History-View 的关系尚未在术语索引中建立。
- 文档中 Action-Prototype 例子使用 TypeScript 签名，但工程背景倾向 C#；后续需要明确“展示语言 vs 执行语言”的契约。

### 7) `micro-wizard.md`

**核心定义（已补齐，非空）**
- **Micro-Wizard**：在局部引入多步骤、上下文感知的引导，帮助 LLM 渐进解决局部复杂性，并在完成后“修剪中间状态”，保持上下文简洁。

**关键价值点**
- 把“多解/多匹配/歧义”从一次性失败变成可交互分歧消解；同时通过修剪避免把过程细节永久占用上下文。

**一致性风险提示**
- “修剪中间状态”会触碰 History 的不可变/仅追加语义，需要明确是：
	- 在 HistoryEntry 层保留全量、在 Context-Projection/History-View 层进行折叠/摘要；还是
	- 采用“复合条目（Composite Entry）”把向导步骤封装成一条高层事件并保留可追溯明细。

---

### 8) `error-feedback.md`

**核心定义**
- **Error-Feedback**（错误反馈）：引导 LLM 从错误状态恢复的交互模式；强调“给出引导与候选”，而非只报错。

**三层模型（严重度 × 恢复复杂度）**
- Level 0 Hint：单行提示 + 自动修正建议
- Level 1 Choice：候选列表 + Action-Link
- Level 2 Wizard：触发 Micro-Wizard 多步流程

**实现上最可操作的部分**
- 错误呈现的固定结构：原因/因果链/恢复选项/可折叠技术详情
- 覆盖典型场景：锚点失效、参数歧义、连续失败熔断
- 给出了声明式 WizardSpec 与 JSON 序列化示例；并建议扩展工具执行状态以表达“需要澄清/需要恢复选择”等中间态

---

### 9) `cursor-and-selection.md`

**核心定义**
- **Cursor-And-Selection**：向 LLM 展示光标位置与选区范围的机制，通过“围栏内 overlay 标记”实现精确引用，避免复述长文本。

**关键机制**
- **Selection-Marker**：`<sel:N>...</sel:N>`（N 用于区分多个选区）
- **Selection-Legend**：围栏外图例，解释每个选区语义（old/new/候选等）
- 光标可用 `<cursor/>` 表示零宽度插入点

**与 UI-Anchor / Micro-Wizard 的关系**
- 类似 UI-Anchor 的“引用而不复述”，但语义是“文本流区间/位置”而非“实体对象”。
- 常作为 Micro-Wizard 的“预览输出”，并与 Action-Link 组合形成可确认/可取消/可选取下一匹配的交互闭环。
- 实现备注明确可复用 PieceTreeSharp 的 `Selection/Range/CursorState` 等结构做渲染转换。

---

## 术语一致性分析（跨文档审计）

### 术语源头与引用规则

- **术语 SSOT**：`glossary.md`
- **引用规则**：见 `key-notes-drive-proposals.md` 的“术语引用规则”章节。

### 关键术语表（当前出现的主要名词）

- Agent / Environment / LLM / Agent-OS
- Message / Observation / Action / Tool-Call
- History / HistoryEntry / History-View / Recent History
- DocUI / Window / Notification
- LOD：Window 的 {Full, Summary, Gist}；Notification 的 {Basic, Detail}
- App-For-LLM（外部扩展） / Built-in（内建能力）
- Context-Projection（生成 IHistoryMessage 的过程，弃用 Render）
- Abstract Token（跨模型 token 估算单位）

### 发现的术语问题（需要统一/澄清）

1. **Abstract Token 拼写与连字符风格**
	 - 当前 key-note 使用 `Abstract-Token`（连字符风格）；建议 glossary 与其他文档保持一致。

2. **App 命名风格不一致**
	 - `app-for-llm.md` 使用 `App-For-LLM`（带连字符）
	 - `llm-agent-context.md` 出现 `AppForLLM`（驼峰）--已改为`App-For-LLM`。

3. **DocUI 的“注入载体/结构”与 LLM 消息抽象间缺少桥接术语**
	 - `doc-as-usr-interface.md` 里出现 `ObservationMessage`（未在术语源头定义），并将 Window 作为 Observation 正文的一部分。
	 - `llm-agent-context.md` 使用 `Observation`（Message）与 `IHistoryMessage`（抽象层）。
	 - 建议补一个桥接句式：
		 - “Observation（Message）在实现上以 IHistoryMessage 表示，其正文由 DocUI Render 的 Window/Notifications 组成（或被其增强）”。
		 - 并避免引入新名 `ObservationMessage`，除非它是已存在的类型名。

4. **History-View / Recent History / Notification 的关系需要更显式**
	 - `doc-as-usr-interface.md`：Notification 是 HistoryEntry 的 LOD 渲染，且 Recent History 用于连续性。
	 - `llm-agent-context.md`：History-View 是“展示给 LLM 的 History 部分信息”。
	 - 建议明确：History-View 是“渲染产物集合”，Notification 是其中一种条目形态（或一个 section），Recent History 是“选取策略/时间窗”。

5. **LOD 命名跨对象不统一但可能合理：需要规则说明**
	 - Window 用 {Full/Summary/Gist}；Notification 用 {Detail/Basic}。
	 - 如果这是刻意区分“状态快照”与“事件流”，建议写一条规则说明：
		 - “快照类（Window）用 Full/Summary/Gist；事件类（Notification）用 Detail/Basic”。

6. **Action 分类与命名需要避免与“编辑”语境冲突**
	 - `Action = Thinking + Tool-Call` 的定义下，如果引入“导航/只读”能力，建议将分类标准写成“是否触发 Environment 状态转移”。
	 - 命名上优先 `World-Effect` vs `View-Only (Navigation)`，避免 `Edit` 这种与文件编辑/IDE 语境高度冲突的词。

6. **“Render”术语已弃用，需持续清理残留**
	 - glossary 与 `llm-agent-context.md` 已明确弃用 Render；其他文档如出现残留需清理。

---

## 与 RL 概念的对齐评估（当前版本）

- 对齐点：Agent/Environment/Observation/Action（含 Tool-Call）/History 的角色划分清晰；“LLM 是内部状态转移函数”的抽象有助于把 LLM 置于系统回路中。
- 潜在歧义：
	- RL 中 Action 通常是“施加到 Environment 的动作”，而本文将 `Action = Thinking + Tool-Call`（发给 Agent-OS 的 Message）。
	- 这并非错误，但需要显式声明“本体系将 Action 定义为 LLM→Agent-OS 的动作载体，Tool-Call 是可执行子集”。

---

## 同步日志

| 日期 | 动作 |
|------|------|
| 2025-12-13 | 首次完整同步：读取 5 篇 Key-Note，补全逐篇摘要与术语一致性审计 |
| 2025-12-14 | 重新同步：新增 UI-Anchor；micro-wizard 已补齐定义；action-anchor 不再存在于 key-notes 目录，已从摘要移除 |
| 2025-12-15 | 新增 Error-Feedback 与 Cursor-And-Selection；同步 Key-Note 数量与总览关系图 |
| 2025-12-19 | 例行复核：逐篇读取 10 篇 Key-Note，未发现新增文件或结构性变更 |

---

## 研讨会共识（持续落地中）

> 来源：`agent-team/meeting/2025-12-13-docui-keynote-workshop.md`

- **术语治理**：演进为 **Primary Definition + Glossary-as-Index**，并在 `key-notes-drive-proposals.md` 写入定义块/重述/迁移/CI 规则。
- **过程命名**：`Render` 已替换为 `Context-Projection`，并补齐输入/输出契约。
- **能力来源统一**：引入 `Capability-Provider` 作为 Built-in 与 App-For-LLM 的上位词，并约束 “App” 仅指外部扩展。
- **交互闭环补齐**：Action 分类为 `World-Effect` vs `View-Only (Navigation)`；引入 Navigation Tool 的“只改视图状态/Projection 参数”定义。
- **信息组织机制**：将 `Attention Focus` 从“待消化建议”转正，作为驱动 LOD 动态切换的机制。
- **跨 App 一致性**：引入 `DocUI Vocabulary`（界面协议/词汇表，语义+最小契约），避免过早固化 SDK 组件库。
- **可视化与层级澄清**：为三层模型补 Mermaid 图；澄清 Window/Notification/History-View/Recent History 的包含关系与“选取策略 vs 呈现形态”。
