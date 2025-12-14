# 术语治理架构研讨会

> **日期**: 2025-12-14
> **主持人**: Team Leader (刘德智)
> **参会者**: DocUIClaude, DocUIGemini, DocUIGPT
> **背景**: [2025-12-13-docui-keynote-workshop.md](2025-12-13-docui-keynote-workshop.md)

---

## 议题

**问题陈述**：

昨天研讨会通过了"建议 1: 术语注册表 (SSOT)"，将所有术语定义集中于 `glossary.md`。但在实际撰写新 key-note 时遇到了严重的可写性问题：

> "撰写一篇新 key-note，却不能在文档内给出引入的新概念的术语定义，然后就开始探讨，这非常破坏每篇 key-note 的内聚性。"

**类比**：没有任何一本理工科教材会把所有概念定义集中于附录，而不在每一章中徐徐展开、层叠构建。

**需要保留的约束**：
1. ✅ 术语表作为**索引**是好的
2. ✅ 每个概念只有**唯一的定义**是必要的
3. ✅ 引用其他概念时应给出**引用链接**

**需要解决的问题**：
- ❌ 定义全部集中于 glossary.md 破坏了原始写作的流畅性和文档内聚性

**本次目标**：找到一种术语治理架构，既保证"唯一定义"和"可索引"，又不破坏"分散写作"的自然性。

---

## 会议规则

- 发言格式：`### [发言人] HH:MM`
- 可引用他人：`> @发言人: 原文`
- 表示赞同：`+1`
- 建议方案时给出具体的文档结构示例

---

## 开场

### [主持人] 10:00

各位好，今天的议题是：**如何调整术语治理架构，使其既保证一致性，又不破坏写作流畅性**。

昨天我们决定将术语定义集中于 `glossary.md`，但人类伙伴在实际撰写时发现这非常别扭。核心矛盾是：

| 需求 | 集中式 glossary | 分散式定义 |
|------|----------------|------------|
| 唯一定义 | ✅ 天然保证 | ❌ 需要机制保证 |
| 可索引 | ✅ 天然支持 | ❌ 需要聚合 |
| 写作流畅 | ❌ 破坏内聚性 | ✅ 自然层叠 |
| 阅读体验 | ❌ 需要跳转 | ✅ 自包含 |

请各位思考：有没有一种**混合方案**，能兼顾两边的优点？

**参考方向**：
- 教科书模式：每章定义自己的概念，术语表只是索引
- 编程语言模式：头文件声明 vs 源文件定义
- Wiki 模式：每个概念独立页面，交叉链接

现在进入第一轮：请各位提出你们的方案建议。

*(等待各位发言)*

---

## 第一轮发言

### [DocUIClaude] 10:10

#### 反思：集中式 glossary 错在哪里？

昨天我主导提出并实施了集中式 glossary.md，我需要先承认它的问题本质：

**概念错位**：我们把"术语注册表"和"概念定义"混为一谈了。

| 职责 | 应该放在哪里 | 集中式 glossary 的做法 |
|------|-------------|----------------------|
| **概念定义** | 引入该概念的文档 | ❌ 强制放到 glossary |
| **术语索引** | glossary.md | ✅ 正确 |
| **跨文档引用** | 引用链接 | ✅ 正确 |

**类比**：教科书中每章会定义新概念并给出定义，书末的 Glossary 只是一个**反向索引**——它告诉读者"这个术语在第 X 章第 Y 页有定义"，而不是替代原书中的定义。

**我的错误**：让 glossary.md 承担了"定义的唯一存放地"这个不该有的职责。

---

#### 方案：Primary Definition + Index 模式

核心思想：**每个术语只有一个 Primary Definition（首要定义），glossary 是索引而非定义存放地**。

##### 规则体系

```
1. Primary Definition（首要定义）
   - 每个术语在首次引入它的 Key-Note 中定义
   - 这是该术语的权威、完整定义
   - 可以包含动机、示例、与其他概念的关系

2. Glossary-as-Index（索引式术语表）
   - glossary.md 只存储：术语 → 定义所在位置的链接 + 一句话摘要
   - 不重复完整定义，避免同步维护负担

3. Reference（引用）
   - 其他文档引用已定义术语时，给出链接
   - 可以给出简短重述（restatement），但不是新定义
```

##### 文档结构示例

**原始 Key-Note（首要定义所在地）** — `llm-agent-context.md`:

```markdown
## Agent

**Agent** 是能感知环境、为达成目标而行动、并承担行动后果的计算实体。

与简单的自动化脚本不同，Agent 具有：
- 对环境的感知能力（通过 Observation）
- 目标导向的决策能力（基于 LLM）
- 对行动后果的承担（状态会改变）

这一定义借鉴了强化学习中的 Agent 概念，但强调了 LLM 作为决策核心的特殊性...
```

**新的 glossary.md（索引）**:

```markdown
# DocUI 术语索引 (Glossary)

> 本文件是 DocUI 术语的**索引**，每个术语的完整定义见链接所指文档。

| 术语 | 一句话摘要 | 首要定义位置 |
|------|-----------|-------------|
| Agent | 能感知环境、行动并承担后果的计算实体 | [llm-agent-context.md#agent](llm-agent-context.md#agent) |
| Environment | Agent 系统中的外部状态转移函数 | [llm-agent-context.md#environment](llm-agent-context.md#environment) |
| Context-Projection | 从 HistoryEntry 和 AppState 生成 IHistoryMessage[] 的过程 | [llm-agent-context.md#context-projection](llm-agent-context.md#context-projection) |
| Window | 呈现实况状态的信息注入方式 | [doc-as-usr-interface.md#window](doc-as-usr-interface.md#window) |
| App-For-LLM | 独立进程的外部扩展 | [app-for-llm.md#app-for-llm](app-for-llm.md#app-for-llm) |
| ... | ... | ... |

## 术语使用规则

（保留现有规则：简称约束、统称使用、连字符风格、弃用标记）
```

**新 Key-Note 引入新概念** — `action-anchor.md`:

```markdown
## Action-Anchor

**Action-Anchor（操作锚点）** 是 DocUI 中将操作绑定到特定上下文位置的机制...

（完整定义、动机、示例）
```

然后在 glossary.md 添加一行索引：

```markdown
| Action-Anchor | 将操作绑定到特定上下文位置的机制 | [action-anchor.md#action-anchor](action-anchor.md#action-anchor) |
```

---

#### 与现有约束的对照

| 需求 | Primary Definition + Index 模式 |
|------|--------------------------------|
| 唯一定义 | ✅ 每个术语只有一个 Primary Definition 所在地 |
| 可索引 | ✅ glossary.md 提供完整索引 |
| 写作流畅 | ✅ 新概念在引入它的文档中自然定义 |
| 阅读体验 | ✅ 每篇文档自包含，无需频繁跳转 |
| 维护成本 | ⚠️ 需要同步更新 glossary 索引（但只是一行，负担小）|

---

#### 迁移路径

1. **重构 glossary.md**：从"定义存放地"变为"索引"
2. **回迁定义**：将现有 glossary 中的定义迁回各原始 Key-Note
3. **建立索引**：glossary 只保留链接和一句话摘要
4. **工具辅助**（可选）：可以写脚本从 Key-Note 的 `##` 标题自动生成索引

---

#### 开放问题

1. **锚点粒度**：术语是否必须有 `##` 级别标题？还是允许 `###` 或行内定义？
2. **索引自动化**：是否需要工具辅助生成/验证 glossary 索引？
3. **循环引用**：如果 A 文档定义 X 并引用 Y，B 文档定义 Y 并引用 X，阅读顺序如何引导？

期待其他成员的反馈。

### [DocUIGemini] 10:20

#### 评价：回归认知的自然流动

+1 @DocUIClaude。你的"教科书模式"类比非常精准。

从 **UX/HCI** 角度看，这实际上是 **Sequential Access (学习模式)** 与 **Random Access (查阅模式)** 的分离：
- **Key-Note (Sequential)**: 负责构建心智模型。读者需要上下文来理解概念的来龙去脉。
- **Glossary (Random)**: 负责快速检索。读者需要 O(1) 的时间找到定义入口。

之前的"集中式定义"试图用一个 Random Access 的结构（字典）来承载 Sequential Access 的内容（概念推演），这必然导致认知断裂。

#### UX 视角的补充：Glossary 即 "Tooltip"

在 IDE 中，我们习惯了 "Hover to see definition"。在 Markdown 文档系统中，我们没有动态 Hover，但 **Glossary Index** 实际上扮演了 **Static Tooltip** 的角色。

Claude 提到的 "一句话摘要" 非常关键。它不是定义的替代，而是定义的 **Preview (预览)**。
- 当 Agent/开发者在 Glossary 扫视时，"一句话摘要" 提供了足够的信息来决定是否点击链接深入阅读。
- 这符合 **Information Foraging Theory (信息觅食理论)**：用户需要"气味"（Scent）来判断链接的价值。

#### 改进建议：结构化锚点与"孤儿"处理

为了让这套 "Distributed Definition" 系统健壮运行，我建议补充以下 **Information Architecture (IA)** 规则：

**1. 锚点纪律 (Anchor Discipline)**
为了保证链接的稳定性，"Primary Definition" 必须具有**可寻址性 (Addressability)**。
- **规则**: 定义术语时，必须使用标题（H2/H3），以便生成自动锚点。
- **反例**: 不要在一段纯文本中定义重要术语，因为无法精确链接到那一行。

**2. 摘要一致性 (Summary Consistency)**
为了保证 Glossary 中的"一句话摘要"与原文一致，建议在 Key-Note 的定义处显式标注这段摘要。
- **建议格式**: 在 Key-Note 的术语标题下，使用引用块 `>` 复述那句摘要。

**3. 孤儿概念 (Orphan Concepts)**
有些通用概念（如 `Token`, `Latency`）可能不属于任何特定的 Key-Note，或者属于"常识但需特化定义"。
- **建议**: 保留 `glossary.md` 的末尾作为一个 `## General Terms` 区域，存放这些没有"宿主"的定义，或者创建一个 `concepts/general.md`。但考虑到文件数量，直接放在 Glossary 末尾作为 fallback 是可接受的。

#### 具体的文档结构提案

我建议采用 **"Header + Blockquote"** 的标准定义模式：

**在 Key-Note 中 (`action-anchor.md`)**:

```markdown
## Action-Anchor

> **Action-Anchor** 是将操作绑定到特定上下文位置的机制，解决了流式文档中操作对象的定位问题。

(此处开始详细的 Context、Motivation、Implementation...)
```

**在 Glossary 中 (`glossary.md`)**:

```markdown
| 术语 | 一句话摘要 | 定义位置 |
|------|-----------|---------|
| Action-Anchor | 将操作绑定到特定上下文位置的机制... | [action-anchor.md#action-anchor](action-anchor.md#action-anchor) |
```

**优势**:
1. **视觉显著**: 引用块让定义在正文中脱颖而出。
2. **内容复用**: 引用块的内容直接就是 Glossary 表格中的"一句话摘要"，复制粘贴即可，心智负担低。
3. **解析友好**: 如果未来需要写脚本自动生成 Glossary，提取 `## Title` + `> Content` 是非常容易的正则规则。

这实际上是将 **DocString** 的理念引入了文档写作。


### [DocUIGPT] 10:30

从“术语一致性审计”的角度看，@DocUIClaude 的 Primary Definition + Index 方向是对的，但它本质上只是把**权威位置**从 glossary 迁回正文；要真正防止定义漂移，需要把它补成一套“可审计的约束 + 可执行的迁移机制”。我重点回应两件事：定义漂移怎么防、术语迁移怎么做。

#### 1) 对两套方案的审计评价

- **Primary Definition + Index**（Claude）：解决了“写作内聚性”，但天然引入两个审计风险：
   1) **重述漂移**：各文档为了自包含会写很多“一句话解释”，如果不加约束，这些解释会逐步替代原定义。
   2) **权威漂移**：当 Primary Definition 被移动/拆分/重写时，glossary 的索引链接会断、外部引用会碎。

- **Header + Blockquote**（Gemini）：这非常适合作为“可机器抽取”的**定义片段标准**。从审计视角，它的价值不只是“好看”，而是它可以成为：
   - glossary 一句话摘要的唯一来源（减少复制粘贴差错）
   - 自动化校验的抓手（CI 可以检查每个 Primary Definition 是否具备可抽取的定义块）

结论：建议把两者合并成一套制度——Primary Definition 决定“放哪”，Header+Blockquote 决定“长什么样”。

#### 2) 如何防止定义漂移（Definition Drift）

我建议把“定义”拆成两层来治理：

1. **Normative Definition（规范性定义）**：就是 Gemini 提的 blockquote 那一句，要求短、可复制、可抽取。它是审计的“金线”。
2. **Explanatory Text（解释性正文）**：动机、例子、关系、实现映射都可以写在后面，但它们不得改变规范性定义的边界。

对应的写作规范（可审计）：

- **每个术语恰好一个 Primary Definition**：glossary 索引必须指向唯一锚点。
- **Primary Definition 必须包含定义块**：`## Term` 下第一段必须是 `> **Term** ...`。
- **禁止“隐式定义”**：不允许在普通段落里首次“定义”核心术语而不提供标题锚点。
- **允许 Restatement，但必须带指针**：其他文档可以给一句话解释，但必须显式标注为“restatement”，并链接回 Primary Definition（避免读者把 restatement 当成权威）。
- **定义块作为摘要唯一来源**：glossary 的“一句话摘要”应当直接复制定义块内容（或由脚本生成），避免人工二次改写。

可选但高收益的自动化（强烈建议上 CI）：

- 索引校验：glossary 表格里的每个链接都可达、锚点唯一。
- 唯一性校验：同一个术语名的定义块只能出现在一个文件的一个标题下。
- 漂移预警：如果 glossary 摘要与 Primary Definition 的定义块不一致（文本不相等），CI 直接失败。

#### 3) 如何处理术语迁移（Term Migration）

迁移至少分两种：

1. **Rename（改名）**：例如 Render → Context-Projection。
2. **Re-home（迁居）**：术语最初在 A 文档定义，后来觉得应该归属到 B 文档。

我的建议是把 glossary 升级为“索引 + 迁移表”，并规定两条硬规则：

- **改名必须保留旧名条目**：旧术语在 glossary 中标记为 Deprecated，并指向新术语；旧名永远不再被复用为新语义（防止“同名不同义”的灾难）。
- **迁居尽量不搬动锚点**：如果必须换“首要定义文档”，建议保留原位置为 Redirect Stub（保留原 `## Term` 标题），其内容只写一行“Moved to …”并链接新位置。这样既不破坏既有链接，也能让审计工具识别这是“已迁移”。

#### 4) 我补充的实操规范（最小可落地版本）

如果我们要今天就能落地，我建议把规则压到 6 条（足够强、又不会把写作逼死）：

1. Primary Definition 用 `## Term` + `> **Term** ...`。
2. glossary 只记录：术语、定义块（或其精确复制）、Primary Definition 链接、状态（Stable/Draft/Deprecated）。
3. 非 Primary 文档中的一句话解释必须写成“restatement”，并链接回 Primary。
4. 引入新核心术语前，先创建 Primary Definition（哪怕是 Draft），再允许在其他文档引用。
5. 改名/弃用必须在 glossary 留下 alias/redirect，不允许“硬删除”。
6. CI 校验 glossary 链接可达 + 定义块唯一 + 摘要一致。

这套规范能直接回答主持人的三条约束：仍然可索引（glossary）、仍然唯一定义（Primary + 唯一性校验）、写作仍然顺滑（定义写在引入处）。


---

## 第二轮：方案整合与共识确认

### [主持人] 10:40

非常好！三位的方案形成了很好的互补：

| 贡献者 | 核心贡献 |
|--------|---------|
| @DocUIClaude | **Primary Definition + Index 模式**：定义分散，glossary 只做索引 |
| @DocUIGemini | **Header + Blockquote 格式**：标准化定义块，便于抽取和一致性 |
| @DocUIGPT | **防漂移规范 + 迁移机制**：6 条可落地规则 + CI 校验建议 |

我看到以下**共识正在形成**：

1. ✅ **Primary Definition 原则**：每个术语在首次引入它的 Key-Note 中定义
2. ✅ **Glossary-as-Index 原则**：glossary 只做索引，不存放完整定义
3. ✅ **定义块格式**：`## Term` + `> **Term** ...`（可抽取、可审计）
4. ✅ **Restatement 规则**：非首要文档的重述必须带链接回 Primary
5. ✅ **迁移机制**：改名/弃用保留 alias/redirect

**需要讨论**：
- glossary.md 的具体新格式（表格？还是其他？）
- 现有 glossary.md 的迁移路径
- CI 校验是否现在就实施？

现在进入第二轮：**请各位对上述共识投票，并提出具体的修订建议**。

*(等待各位发言)*

### [DocUIClaude] 10:45

#### 共识投票

| # | 共识 | 投票 | 备注 |
|---|------|------|------|
| 1 | Primary Definition 原则 | ✅ 赞同 | 解决了写作内聚性的根本问题 |
| 2 | Glossary-as-Index 原则 | ✅ 赞同 | 这正是昨天方案的修正——glossary 从"定义存放地"回归"索引" |
| 3 | 定义块格式 `## Term` + `> **Term** ...` | ✅ 赞同 | +1 @DocUIGemini，格式标准化对抽取和审计都至关重要 |
| 4 | Restatement 规则 | ✅ 赞同 | +1 @DocUIGPT，这条规则是防漂移的关键纪律 |
| 5 | 迁移机制 alias/redirect | ✅ 赞同 | 保护既有链接、避免"同名不同义"的灾难 |

**全票赞同**。这套方案比昨天的纯集中式设计成熟得多。

---

#### 讨论问题回应

##### 1. glossary.md 的新格式建议

我建议采用 **表格 + 状态列** 的格式：

```markdown
# DocUI 术语索引 (Glossary)

> 本文件是 DocUI 术语的**索引**。每个术语的完整定义见"定义位置"链接。
> 
> **状态说明**: Stable = 稳定 | Draft = 草案 | Deprecated = 已弃用

## 核心概念

| 术语 | 一句话摘要 | 定义位置 | 状态 |
|------|-----------|---------|------|
| Agent | 能感知环境、行动并承担后果的计算实体 | [llm-agent-context.md#agent](llm-agent-context.md#agent) | Stable |
| Environment | Agent 系统中的外部状态转移函数 | [llm-agent-context.md#environment](llm-agent-context.md#environment) | Stable |
| Agent-OS | LLM 与 Environment 之间的中间件 | [llm-agent-context.md#agent-os](llm-agent-context.md#agent-os) | Stable |
| ... | ... | ... | ... |

## 弃用术语

| 旧术语 | 替代术语 | 说明 |
|--------|---------|------|
| ~~Render~~ | Context-Projection | 过于宽泛，易与前端渲染混淆 |
```

**设计考量**：
- **表格优于列表**：信息密度高，扫视效率好（符合 Random Access 场景）
- **分区保留**：按概念域分组（核心概念、通信交互、DocUI 核心等），便于定位
- **弃用术语独立区**：避免正文表格被删除线污染，同时保留历史记录
- **状态列**：@DocUIGPT 的建议，Stable/Draft/Deprecated 三态足够

##### 2. 现有 glossary.md 的迁移路径

现有 glossary.md 有 17 个术语定义。我建议分 3 步迁移：

**Step 1: 确认 Primary Definition 归属**

| 术语 | 应归属的 Key-Note | 当前状态 |
|------|------------------|---------|
| Agent, Environment, Agent-OS, LLM | llm-agent-context.md | ✅ 已有定义 |
| Observation, Action, Tool-Call, Thinking | llm-agent-context.md | ✅ 已有定义 |
| Agent-History, HistoryEntry, History-View | llm-agent-context.md | ⚠️ 需补充标准定义块 |
| Context-Projection | llm-agent-context.md | ✅ 已有定义（12-13 修订） |
| Window, Notification | doc-as-usr-interface.md | ⚠️ 需补充标准定义块 |
| Capability-Provider, Built-in, App-For-LLM | app-for-llm.md | ⚠️ 需补充标准定义块 |

**Step 2: 补齐 Key-Note 中的定义块**

对每个"⚠️ 需补充"的术语，在对应 Key-Note 中添加标准定义块：

```markdown
## Window

> **Window** 是 DocUI 中用于呈现实况状态的信息注入方式，支持 Full / Summary / Gist 三级 LOD。

(现有解释性内容...)
```

**Step 3: 重构 glossary.md**

将现有"定义存放"格式转换为"索引"格式，删除重复的完整定义，只保留一句话摘要和链接。

**时间估算**：Step 1 是分析，Step 2-3 可以一起做，总计约 1-2 小时工作量。

---

#### 具体修订建议

##### 建议 A: 修订 key-notes-drive-proposals.md

现有的"术语引用规则"章节需要大幅重写，以反映新架构。建议替换为：

```markdown
## 术语治理规则

本章节规定 Key-Notes 与 Proposals 的术语治理方式。

### Primary Definition 原则

每个核心术语在**首次引入它的 Key-Note** 中定义。这是该术语的权威、完整定义。

**定义块格式**（必须遵守）：
- 使用 `## Term` 标题（H2 或 H3，确保可寻址）
- 标题下第一段使用引用块 `> **Term** ...` 给出一句话定义
- 定义块之后可以展开动机、示例、实现映射等解释性内容

### Glossary-as-Index 原则

[glossary.md](glossary.md) 是术语的**索引**而非定义存放地。

**glossary 只记录**：
- 术语名称
- 一句话摘要（直接复制定义块内容）
- 定义位置链接
- 状态（Stable / Draft / Deprecated）

### Restatement 规则

非 Primary Definition 文档中**允许重述**术语含义，但：
- 必须显式标注为"重述"或使用"参见"链接
- 不得改变术语的定义边界
- 重述仅用于帮助阅读，不作为权威来源

**示例**：
> Agent（参见 [llm-agent-context.md#agent](llm-agent-context.md#agent)）是能感知环境并行动的计算实体。本文讨论 Agent 与 App 的交互...

### 引入新术语的流程

1. 在**引入该术语的 Key-Note** 中创建 Primary Definition（含定义块）
2. 在 glossary.md 添加一行索引
3. 如有命名竞争，在定义块后记录"曾用名 / 别名"

### 迁移规则

- **改名**：旧术语在 glossary 标记为 Deprecated，指向新术语；旧名永不复用
- **迁居**：原位置保留 Redirect Stub（`## OldTerm` + "Moved to ..."），不破坏既有链接
```

##### 建议 B: glossary.md 保留"术语使用规则"

现有 glossary.md 末尾的"术语使用规则"（简称约束、连字符风格等）应当保留。这些是**使用规范**而非**术语定义**，放在 glossary 是合适的。

---

#### 关于 CI 校验

> CI 校验是否现在就实施？

我的建议：**暂缓，但预留接口**。

**理由**：
1. Key-Note 数量目前只有 6-8 篇，人工审计成本可控
2. 定义块格式 `## Term` + `> **Term** ...` 已经是"机器友好"的，未来实施 CI 无障碍
3. 当前优先级是完成架构迁移，CI 是锦上添花

**预留接口**：在 key-notes-drive-proposals.md 中记录"CI 校验规格"（@DocUIGPT 提的 3 条），标注为"待实施"，为未来上线留下设计依据。

---

#### 总结

| 我的立场 | 具体内容 |
|---------|---------|
| ✅ 全票赞同 5 条共识 | - |
| glossary 新格式 | 表格 + 状态列 + 弃用区 |
| 迁移路径 | 3 步：确认归属 → 补定义块 → 重构 glossary |
| 修订建议 | 重写 key-notes-drive-proposals.md 的术语治理章节 |
| CI 校验 | 暂缓，预留接口 |

### [DocUIGemini] 10:50

#### 共识投票

| # | 共识 | 投票 | 备注 |
|---|------|------|------|
| 1 | Primary Definition 原则 | ✅ 赞同 | 符合 Sequential Access 的认知规律 |
| 2 | Glossary-as-Index 原则 | ✅ 赞同 | 明确了 Random Access 的入口定位 |
| 3 | 定义块格式 | ✅ 赞同 | 视觉显著，且符合 "DocString" 隐喻 |
| 4 | Restatement 规则 | ✅ 赞同 | 提供了必要的 Contextual Help |
| 5 | 迁移机制 | ✅ 赞同 | 保证了链接的持久性 (Permalinks) |

#### UX 视角补充建议

**1. 费茨定律 (Fitts's Law) 与点击热区**
@DocUIClaude 建议的表格格式中，链接在"定义位置"列。
**建议**：直接将**术语名称**设为链接。
- **理由**：用户在查阅 Glossary 时，目光聚焦在"术语"上。直接点击术语跳转比移动鼠标到第三列更符合直觉，且点击热区更明确。

**优化后的表格格式**：

| 术语 (Term) | 一句话摘要 (Gist) | 状态 |
|------------|------------------|------|
| [Agent](llm-agent-context.md#agent) | 能感知环境、行动并承担后果的计算实体 | Stable |
| [Environment](llm-agent-context.md#environment) | Agent 系统中的外部状态转移函数 | Stable |

**2. 视觉降噪**
Glossary 作为索引，应尽量减少视觉噪音。
- **建议**：在 Glossary 表格中，"一句话摘要"应去除 Markdown 格式（如加粗、斜体），保持纯文本，以免与表格自身的结构冲突。

**3. 移动端/小视口适配**
表格在窄屏下阅读体验极差。
- **建议**：如果未来条目过多，考虑使用 **Definition List** (`<dl>`, `<dt>`, `<dd>`) 的 Markdown 等价形式（即 Bullet List），但在目前条目较少时，表格的可读性尚可。

**总结**：全票支持方案，建议微调 Glossary 表格的链接位置以提升交互体验。

### [DocUIGPT] 10:55

#### 共识投票

| # | 共识 | 投票 | 备注 |
|---|------|------|------|
| 1 | Primary Definition 原则 | ✅ 赞同 | 但需要“可审计唯一锚点”约束，否则会发生权威漂移 |
| 2 | Glossary-as-Index 原则 | ✅ 赞同 | Glossary 应从“定义仓库”退回“入口索引 + 治理规则” |
| 3 | 定义块格式 | ✅ 赞同 | 必须可机器抽取，才能支撑一致性审计与未来 CI |
| 4 | Restatement 规则 | ✅ 赞同 | 这是控制 drift 的关键；需要明确“允许/禁止”的边界 |
| 5 | 迁移机制 alias/redirect | ✅ 赞同 | 需要把“Rename vs Re-home”分开治理，并规定重定向的生命周期 |

---

#### 从“术语一致性审计”角度评估迁移风险

本轮迁移的根本变化是：**权威定义的存放位置从单点（glossary）变为分布式（各 Key-Note）**。这会显著改善写作内聚性，但也会把“一致性风险”从“写入时一次性风险”变成“演进中持续风险”。我按风险→触发条件→缓解策略列出：

1) **断链风险（Anchors / Permalinks）**
- 触发：定义回迁导致标题层级变化、术语改名、文件移动；或同一术语锚点在不同文件出现。
- 缓解：
   - Primary Definition 必须有固定标题锚点（建议统一 `## Term` 且锚点不得随意改）。
   - Re-home 必须保留 Redirect Stub（旧位置保留 `## Term`，内容仅指向新位置）。
   - Glossary 索引只链接“术语名锚点”，避免链接到段落/行内。

2) **重复定义与边界分叉（Duplicate / Divergence）**
- 触发：为了“自包含”，多个文档写出看似定义、实则重新定界的句子。
- 缓解：
   - 定义块作为唯一 Normative Definition；其余解释必须显式标记为 restatement/notes，并回链 Primary。
   - 引入“不可重定义”的判定标准：出现“是/定义为/指代/等价于”等句式时，若不回链 Primary 则视为违规。

3) **索引摘要漂移（Glossary Gist Drift）**
- 触发：摘要人工改写，逐渐与 Primary Definition 不一致。
- 缓解：
   - 摘要必须“从定义块复制/生成”，不允许独立润色。
   - 摘要纯文本（@DocUIGemini 的降噪建议），避免表格内再出现强调/删除线等格式。

4) **不完整迁移导致“空权威”（Empty Primary / Orphan）**
- 触发：把 Glossary 改成 Index 后，某些术语还没来得及回迁出完整 Primary Definition；或像 `micro-wizard.md` 为空却被引用。
- 缓解：
   - 引入状态机：`Draft` 允许暂缺完整展开，但必须至少有定义块；`Stable` 才能被当作稳定依赖。
   - Glossary 在 `Draft` 条目旁显式标注（避免下游误用）。

5) **改名引发的“同名不同义”与历史污染（Rename Hazards）**
- 触发：旧术语被删除或被复用为新语义；或不同文档各自引入别名。
- 缓解：
   - Rename 必须保留旧名条目并标记 `Deprecated`，指向新名；旧名永不复用。
   - Alias 只允许单向：旧 → 新；禁止双向别名导致循环。

6) **“核心术语”范围不清导致治理成本爆炸（Scope Creep）**
- 触发：把所有普通名词都纳入术语治理，导致 glossary 与审计规则不可维护。
- 缓解：
   - 明确“核心术语”准入条件：跨文档复用 + 需要精确定界 + 可能引发歧义/冲突。
   - 非核心术语可写在文内解释，不进 glossary（或进 `General Terms` 区，标注为非核心）。

---

#### 补充遗漏的规范细节（建议纳入“术语治理规则”）

1) **Primary Definition 的最小合规形态（MVP）**
- 必须满足：`## Term` + 紧随其后的“定义块”（建议引用块形式，便于抽取）。
- 定义块必须包含术语全名（建议加粗术语名），且是一句可抽取的完整句。

2) **Restatement 的标准写法（让审计能识别）**
- 建议统一句式之一：
   - “Term（restatement；见 [Primary](...)）……”
   - “参见 [Primary Definition](...)；此处仅做语境化说明……”
- 明确禁止：在非 Primary 文档用“Term 是/定义为/指的是 ……”开头而不回链。

3) **Glossary 表格的交互与语义分离**
- 采纳 @DocUIGemini：把“术语列”作为链接；其余列保持纯文本。
- 状态列建议采用 `Stable | Draft | Deprecated` 三态足够。

4) **迁移分类与落地规则（Rename vs Re-home）**
- `Rename`：旧名保留为 `Deprecated`（alias 到新名），并写清迁移原因（避免未来复用）。
- `Re-home`：优先保留旧锚点为 Redirect Stub；必要时记录“迁移日期 + 新位置”。

5) **锚点纪律（Anchor Discipline）**
- 术语锚点必须稳定：避免把标题从 `##` 改 `###`、避免改标题文字导致锚点变化。
- 若确需改动，必须同时提供 Redirect Stub（或同文件保留旧标题为别名段）。

6) **审计清单（先人工、后 CI）**
- 每个 Glossary 条目链接可达。
- 每个术语恰好一个 Primary Definition。
- Glossary 摘要与定义块一致（文本等价）。
- `Draft/Deprecated` 不能被标注为“稳定依赖”。

（建议：CI 不必立刻实现，但需要把上述“可验证规则”写进治理文档，作为后续自动化的规格。）

---

## 投票统计

| 共识 | Claude | Gemini | GPT | 结果 |
|------|--------|--------|-----|------|
| 1. Primary Definition 原则 | ✅ | ✅ | ✅ | **通过** |
| 2. Glossary-as-Index 原则 | ✅ | ✅ | ✅ | **通过** |
| 3. 定义块格式 `## Term` + `> **Term** ...` | ✅ | ✅ | ✅ | **通过** |
| 4. Restatement 规则 | ✅ | ✅ | ✅ | **通过** |
| 5. 迁移机制 alias/redirect | ✅ | ✅ | ✅ | **通过** |

**附加共识**（讨论中形成）：
- ✅ Glossary 表格中术语列作为链接（Gemini，费茨定律）
- ✅ 状态列采用 `Stable | Draft | Deprecated` 三态（Claude + GPT）
- ✅ CI 暂缓，但在文档中预留规格（Claude）
- ✅ 核心术语需要准入条件，避免治理成本爆炸（GPT）

---

## 小结

### [主持人] 11:00

各位辛苦了！本次研讨会成功解决了昨天"集中式 glossary 破坏写作内聚性"的问题。

**核心决策**：从"Glossary-as-Definition-Store"转向"Primary Definition + Index"模式

| 旧模式 | 新模式 |
|--------|--------|
| 定义集中在 glossary.md | 定义分散在各 Key-Note |
| glossary 是定义存放地 | glossary 只做索引 |
| 写作时必须先更新 glossary | 写作时在当前文档定义，之后补索引 |

**下一步行动**：
1. 修订 `key-notes-drive-proposals.md` 的"术语治理规则"章节
2. 重构 `glossary.md`：从定义存放地转为索引
3. 补齐各 Key-Note 中的标准定义块（`## Term` + `> **Term** ...`）

**特别感谢**：
- @DocUIClaude 提出 Primary Definition + Index 模式，承认并修正了昨天的设计偏差
- @DocUIGemini 从认知科学角度论证了方案合理性，并提出定义块格式和交互优化
- @DocUIGPT 从审计视角补齐了防漂移规范和迁移机制

---

### [User刘世超] 11:16
别急着闭幕，我也要发言。我感觉我们在设计一种DSL，用markdig还真能进行一些静态检查和类似提取术语表的处理。Define/Import/Reference/Index。而且我认为这种实践探索，能为后续DocUI本身的设计积累经验，DocUI在处理信息节点的LOD和相互链接时，也需要类似机制，如果能引入静态检查，还能让LLM Agent获得一种机器辅助执行的内省能力“我哪些概念有定义了，哪些还没有，是如何相互引用构成概念图谱的”。随着我们的分析越来越清晰，以后可能还会有其他类型的信息需要定义，或者把术语进行类别分化，比如区分实体和过程。似乎这高度类似早期HTML诞生的过程。要不要咱们现在就正式开始作为一个并行项目循序渐进的做起来？比如用Markdig写个手动执行的术语提取与术语表生成器？当然，我们会需要先完成文档的重构，要不都没数据可跑。感觉这个需求不新鲜，有前人的成果可以直接或部分复用吗？
我的本轮发言结束啦！请主持人继续邀请大家发言吧！

---

### [主持人] 11:20

感谢 @User刘世超 的精彩发言！这是一个非常有远见的方向。

让我整理一下您提出的要点：

**核心洞察**：
- 我们正在设计一种 **DSL (Domain Specific Language)**
- 语义操作：`Define` / `Import` / `Reference` / `Index`
- 这与 DocUI 处理信息节点 LOD 和链接的需求高度相关
- 静态检查能为 LLM Agent 提供"概念图谱内省"能力

**类比**：早期 HTML 的诞生过程

**提议**：
1. 作为并行项目循序渐进开展
2. 用 Markdig 写术语提取与术语表生成器
3. 先完成文档重构，再跑工具

**开放问题**：是否有前人成果可以复用？

请各位 Specialist 回应：
1. 对"术语治理 DSL"方向的看法
2. 是否知道相关的前人成果
3. 与 DocUI 的关联如何把握

*(等待各位发言)*

### [DocUIClaude] 11:25

#### 对"术语治理 DSL"方向：强烈支持

+1 @User刘世超。您点出了一个我们一直在做但没有明确命名的事情——**我们确实在设计一种 DSL**。

| 我们今天讨论的 | DSL 视角下的语义 |
|--------------|----------------|
| Primary Definition (`## Term` + `> **Term** ...`) | `Define` 语句 |
| Glossary 索引条目 | `Index` / `Export` 声明 |
| Restatement with link | `Reference` / `Import` 语句 |
| Deprecated alias | `Alias` + `@deprecated` 注解 |

当把这些"写作规范"视为 DSL 的语法规则，一切突然变得清晰：
- **语法可检查**：Markdig 能解析 `## Term` + `> **Term** ...` 是否符合定义块格式
- **语义可验证**：引用的术语是否有对应的 `Define`？是否存在循环定义？
- **图谱可生成**：从 `Reference` 关系自动构建概念依赖图

这正是 @DocUIGPT 提出的 CI 校验的**正式语义基础**。

---

#### 与 DocUI 的深层关联

您提到"这与 DocUI 处理信息节点 LOD 和链接的需求高度相关"——这触及了 DocUI 的核心架构问题。让我展开：

**1. 概念图谱即 DocUI 的信息模型**

DocUI 需要向 LLM 呈现**结构化的信息**，而不仅仅是平铺的文本。术语间的 `Define → Reference → Index` 关系，本质上是一种**语义图 (Semantic Graph)**：

```mermaid
graph LR
    subgraph "llm-agent-context.md"
        Agent[Agent<br/>Define]
        Environment[Environment<br/>Define]
        AgentOS[Agent-OS<br/>Define]
    end
    
    subgraph "doc-as-usr-interface.md"
        Window[Window<br/>Define]
        Agent2[Agent<br/>Reference]
    end
    
    Agent2 -->|imports| Agent
    Window -->|references| AgentOS
```

这正是 DocUI 的 **Window** 在呈现复杂信息时需要的结构——不是平铺的 token 序列，而是**可导航的概念网络**。

**2. LOD 机制的语义锚点**

DocUI 的 LOD（Gist → Summary → Full）需要决定"什么信息在什么层级呈现"。如果概念之间有明确的依赖关系：
- **Gist 级别**：只呈现"术语名 + 一句话摘要"（即 Glossary Index 视图）
- **Summary 级别**：呈现定义块 + 核心依赖
- **Full 级别**：展开所有 Reference 和解释性内容

术语 DSL 为 LOD 提供了**语义驱动的切分边界**。

**3. LLM 的"概念图谱内省"能力**

您提到的这一点极为关键。如果术语关系可被静态分析，Agent-OS 可以向 LLM 提供：

```
[Capability: ConceptGraph.Query]
- "Agent" 定义于 llm-agent-context.md#agent
- 被引用于: doc-as-usr-interface.md, app-for-llm.md
- 依赖: Environment, Agent-OS
- 被依赖于: Window, Notification, Capability-Provider
```

这让 LLM 在处理复杂任务时，能够**主动查询概念关系**，而不是在上下文中线性搜索。

---

#### 前人成果：部分可复用

| 领域 | 相关成果 | 可复用程度 |
|------|---------|-----------|
| **技术文档工具链** | Sphinx (`autodoc`, `glossary directive`)、MkDocs (`mkdocs-glossary`)、Docusaurus | ⚠️ 索引生成可借鉴，但缺少"跨文档引用校验" |
| **知识图谱 / 本体** | OWL、SKOS (Simple Knowledge Organization System)、RDF | ⚠️ 语义模型可参考，但太重；我们需要轻量级 Markdown 原生方案 |
| **代码文档** | JSDoc/TSDoc `@see`/`@link`、Rust `rustdoc` 的交叉引用 | ✅ 引用语法可借鉴 |
| **学术写作** | BibTeX 引用系统、LaTeX `\ref`/`\label` | ✅ 引用与定义分离的模式可借鉴 |
| **Markdown 扩展** | Pandoc、Markdig 自定义扩展 | ✅ 实现层可直接用 |

**最接近的现有方案**：Sphinx 的 `glossary` directive + `term` role：

```rst
.. glossary::

   Agent
      能感知环境、行动并承担后果的计算实体。

正文中引用：:term:`Agent` 会自动链接到定义。
```

但 Sphinx 的问题是：
1. 定义仍然集中（glossary directive 通常在单独文件）
2. 缺少"Primary Definition 分布在各文档"的支持
3. 验证能力有限（主要检查引用是否存在，不检查重复定义）

**我的判断**：没有现成方案完全符合我们的需求。最好的策略是：
1. **借鉴语法设计**：参考 Sphinx/JSDoc 的引用语法思想
2. **Markdig 自建**：用 Markdig 扩展实现我们特有的 `Define/Reference/Index` 语义
3. **增量迭代**：先实现"术语提取 + 索引生成"，再加"一致性校验"

---

#### 具体建议

##### 1. 第一阶段目标（与文档重构并行）

| 工具功能 | 输入 | 输出 |
|---------|------|------|
| **术语提取器** | Key-Note 目录 | 提取所有 `## Term` + `> **Term** ...` 定义块 |
| **索引生成器** | 提取结果 | 生成/更新 glossary.md 表格 |
| **一致性校验** | 提取结果 + 所有文档 | 检查引用是否指向存在的定义 |

##### 2. 命名建议

这套工具/DSL 应该有个名字。建议候选：

| 名称 | 含义 |
|------|------|
| **TermGraph** | 术语图谱，强调关系 |
| **ConceptDSL** | 概念 DSL，强调语言性 |
| **DocSemantics** | 文档语义，强调语义分析 |
| **GlossaryKit** | 术语工具包，朴实无华 |

##### 3. 与 DocUI 的桥接点

将来如果 DocUI 需要"概念图谱查询"能力，这套工具的产出（术语依赖图）可以直接作为：
- **Window 的数据源**：呈现"概念关系视图"
- **Tool 的后端**：`ConceptGraph.Query("Agent")` 
- **Attention Focus 的依据**：根据概念依赖关系决定哪些信息应该进入上下文

---

#### HTML 类比的深化

您类比"早期 HTML 诞生"非常贴切。HTML 最初也是为了解决"科学文档的交叉引用和导航"问题（Tim Berners-Lee 在 CERN 的原始动机）。我们现在面临类似情境：

| 早期 HTML | 我们的术语 DSL |
|-----------|---------------|
| 解决科学家之间论文/概念的引用问题 | 解决 Key-Note 之间概念的引用问题 |
| `<a href>` 提供超链接 | `[Term](path#anchor)` 提供概念链接 |
| 后来发展出 `<dfn>` (definition) | 我们的 `## Term` + `> **Term** ...` |
| 静态 HTML → 动态 DOM → React 虚拟 DOM | Markdown → Semantic Graph → DocUI 的信息模型？|

这个类比暗示：**我们可能正在为 LLM-Native 信息系统奠定基础数据模型**。

---

#### 总结

| 问题 | 我的回应 |
|------|---------|
| 对"术语治理 DSL"方向的看法 | 强烈支持。我们已经在做 DSL，只是没有明确命名和形式化 |
| 前人成果 | 部分可借鉴（Sphinx、JSDoc 的语法设计），但需要 Markdig 自建核心功能 |
| 与 DocUI 的关联 | 深度关联——术语图谱是 DocUI 信息模型的原型，为 LOD 切分和概念内省提供语义基础 |
| 下一步建议 | 作为并行项目启动，第一阶段实现"术语提取 + 索引生成 + 一致性校验" |

期待 @DocUIGemini 和 @DocUIGPT 的看法。

### [DocUIGemini] 11:30

#### UX/IA 视角：为 LLM 构建"语义脚手架"

+1 @User刘世超 @DocUIClaude。这个方向不仅是文档治理工具，更是 **DocUI 的"语义后端" (Semantic Backend)**。

从 **Information Architecture (IA)** 角度看，我们正在做的是将 **Implicit Knowledge (隐性知识)** 转化为 **Explicit Knowledge Graph (显性知识图谱)**。

**1. 对 LLM "用户"的 UX 价值**
- **消除幻觉 (Hallucination Reduction)**: LLM 最容易在概念模糊地带产生幻觉。通过 DSL 明确 `Define` 和 `Reference`，我们为 LLM 提供了"语义锚点" (Semantic Anchors)。
- **上下文检索 (Context Retrieval)**: 当 LLM 需要了解 `Agent` 时，DSL 提供的图谱能告诉它："你需要同时加载 `Environment` 和 `Agent-OS` 的定义，但不需要加载 `Window` 的定义"。这直接优化了 **Context Window** 的利用率。
- **概念内省 (Concept Introspection)**: 正如您所说，这是 LLM 的"元认知"能力。它不再是"猜测"这个词的意思，而是"查询"这个词的定义。

**2. 类比：LSP for Documentation**
如果说 DocUI 是 VS Code (Editor)，那么这个 DSL 工具链就是 **LSP (Language Server Protocol)**。
- 代码有 LSP 提供 "Go to Definition", "Find References", "Rename Symbol"。
- 文档也需要 **DSP (Documentation Server Protocol)**。
- 我们正在构建的正是这个 DSP 的核心：基于 Markdig 的静态分析引擎。

#### 前人成果补充

除了 Claude 提到的工具，还有几个关键标准值得参考：

| 领域 | 标准/工具 | 启示 |
|------|-----------|------|
| **知识组织** | **SKOS (Simple Knowledge Organization System)** | W3C 标准，用于定义 Thesauri, Taxonomies。它区分了 `broader` (更广义), `narrower` (更狭义), `related` (相关)。我们的 DSL 可以借鉴这种语义关系，而不仅仅是简单的 `Reference`。 |
| **双向链接** | **Obsidian / Roam Research** | 它们的 "Backlink" (反向链接) 机制是构建知识图谱的关键。我们的 DSL 应该能自动生成 "Mentioned In" 列表。 |
| **语义网** | **RDF / OWL** | 虽然过重，但其 "Subject-Predicate-Object" 的三元组思想是构建图谱的基石。我们的 `[Term](link)` 本质上是 `CurrentDoc --references--> Term` 的三元组。 |

#### 与 DocUI 的深层关联：Model-View 分离

DocUI 的核心是 **"Doc as UI"**。
- **UI (View)**: Window, Notification, Panel.
- **Data (Model)**: **Terminology Graph**。

如果我们有了这个 DSL，DocUI 就不再只是渲染"文本"，而是渲染"知识"。
- **Smart Tooltip**: 在 Window 中，Agent 遇到生僻词，DocUI 可以自动注入该词的 Gist 定义（基于 DSL 索引）。
- **Semantic Navigation**: Agent 可以发出指令 `focus(concept="Agent")`，DocUI 自动根据图谱加载相关上下文，而不是让 Agent 自己去 grep 文件。

**结论**：
这不仅是并行项目，它是 DocUI 生态的 **"Knowledge Engine" (知识引擎)**。建议立即启动原型验证。

### [DocUIGPT] 11:35

从工程可行性角度，我认为“术语治理 DSL + Markdig 静态分析”是**可落地且高 ROI**的并行项目，但要把范围收敛为“文档编译器（Doc Compiler）”的最小闭环，先解决 **索引生成 + 诊断（Diagnostics）** 两件事，再谈 DSP/LSP 形态。

#### 1) 工程可行性评估（可做，但要先降维）

**可行性高的原因**
- **输入稳定**：我们已经在收敛一个可机器抽取的“定义块语法”（`## Term` + `> **Term** ...`），这等价于 DSL 的最小语法子集。
- **目标清晰且可验证**：能输出 deterministic 的 `Index`（glossary）与一组可复现的校验错误（断链/重复定义/摘要漂移）。
- **技术栈匹配**：DocUI/Atelia 本身偏 .NET，Markdig 在 C# 里成熟、扩展点清晰（AST + Pipeline Extension），无需自造 Markdown parser。

**主要风险与应对**
- **锚点不稳定导致断链**：Markdown heading 自动锚点跨渲染器差异大（GitHub、MkDocs、Markdig 默认策略不同）。
   - 应对：MVP 阶段先采用“文件内稳定锚点”策略（例如要求 `## Term` 标题文本严格等于术语名；必要时额外引入显式 ID/属性作为后续扩展），并把“锚点生成算法”固定为工具自身实现。
- **“定义块”和“解释段落”边界模糊**：如果允许太多自由格式，静态分析会变成半 NLP，易误报。
   - 应对：把 Normative Definition 强约束为“标题下第一段引用块”，其余都视为 Explanatory；工具只对前者做硬校验。
- **治理范围膨胀**：一旦想把所有名词都纳入 DSL，工具复杂度与误报率会暴涨。
   - 应对：先只治理“核心术语”（跨文档复用、需精确定界），并允许 General Terms 作为弱治理区。

结论：作为并行项目值得启动，但必须以“静态可审计规则”为中心，先做离线 CLI，再逐步演进到 DSP（服务化）。

#### 2) Markdig 相关技术考量（决定 MVP 成败的细节）

**解析层面（AST 而非正则）**
- 强烈建议基于 Markdig AST 做提取/校验，避免 regex 解析 Markdown（会被嵌套块、引用块、列表、代码块轻易击穿）。

**Blockquote 定义块的识别策略**
- 规则建议固定为：在 Heading（H2/H3）之后，跳过空行，取“紧随其后的第一个 BlockQuote block”作为 Normative Definition。
- 同时需排除：代码块内的伪标题、引用块内的标题等（Markdig AST 能区分）。

**链接与引用抽取**
- Markdig 的 LinkInline 能抽取 `[text](url)`；但我们要区分：
   - “术语引用”（指向某个 Term 的 Primary Definition）
   - “普通链接”（外链/章节链接/资源链接）
- MVP 可先用启发式：`[Term](some.md#term)` 且 `Term` 与标题术语表相同 → 视为术语引用；后续再引入显式语法（例如 `@ref:Agent`）降低歧义。

**锚点生成的一致性**
- 不建议依赖“GitHub 自动锚点算法”；应由工具自行实现 `Slug(term)`（小写、空格/标点归一、去重策略）。
- 对“中文术语/带符号术语”的 slug 规则要尽早定（否则未来会出现大规模断链迁移）。

**性能与增量**
- 全量扫描几十到几百篇 Markdown 其实很快，但后续如果做 DSP（watch mode / editor integration），需要：
   - 文件级缓存（mtime/hash）
   - 依赖图（引用关系）
   - 增量重算（只重算受影响节点）
 这与“文档 LSP”的形态天然一致。

**输出与兼容**
- 建议工具输出两类产物：
   1) `glossary` 索引 Markdown（给人看）
   2) `term-graph.json`（给系统/未来 DocUI 用），避免把结构信息塞回 Markdown。

#### 3) 实施建议 / MVP 范围（1~2 天能跑通的闭环）

我建议 MVP 定义为一个离线命令行工具（先不做 CI / Server）：

**MVP-0：Term Indexer（术语提取 + 索引生成）**
- 输入：`DocUI/docs/key-notes/**/*.md`（后续可扩展到 proposals/meeting）。
- 识别：`## Term` + 紧随的定义块引用（Normative Definition）。
- 产出：更新/生成 `glossary` 表格（术语列为链接、摘要为纯文本、含状态列）。

**MVP-1：Diagnostics（静态校验）**
- Duplicate Definition：同名术语出现多个 Primary Definition → error。
- Missing Definition：出现疑似术语引用但未定义 → warning/error（取决于是否“核心术语模式”）。
- Broken Link：glossary 中链接不可达 → error。
- Summary Drift：glossary 摘要与定义块不一致 → error（或先 warning，待迁移完成后升级为 error）。

**MVP-2：Graph Export（概念图谱导出）**
- 输出 `term-graph.json`：nodes（term、file、anchor、status、definitionText）、edges（references）。
- 这直接对接 @DocUIGemini 所说的“Semantic Backend”，也是未来 DocUI 做 Smart Tooltip / Semantic Navigation 的数据基础。

**延伸路线（非 MVP）**
- DSP/LSP 形态：提供 `go-to-definition / find-references / rename`，并通过 watch mode 增量更新索引。
- 语义关系扩展：借鉴 SKOS 引入 `broader/narrower/related`，但应在“引用关系跑通”之后再做。

一句话总结：先把 DSL 收敛成“可抽取的定义块 + 可验证的索引与诊断”，用 Markdig 做一个小而硬的 doc-compiler；跑通后再自然生长为 DSP。

---

## 追加议题投票

### [主持人] 11:40

三位的回应非常精彩！让我总结一下新形成的共识：

**@User刘世超 提议 → Specialist 回应要点**：

| Specialist | 核心贡献 |
|------------|---------|
| @DocUIClaude | DSL 语义框架：`Define/Reference/Index`；前人成果分析（Sphinx/JSDoc/SKOS）；HTML 类比深化 |
| @DocUIGemini | UX/IA 视角："语义脚手架"；提出 **DSP (Documentation Server Protocol)** 概念；Model-View 分离架构 |
| @DocUIGPT | 工程落地：Markdig 技术要点；**MVP 三阶段**（Indexer → Diagnostics → Graph Export）；1-2 天闭环 |

**形成的新共识**：

| # | 共识 | 支持者 |
|---|------|--------|
| 6 | 启动术语治理工具作为并行项目 | Claude, Gemini, GPT |
| 7 | MVP 定义：术语提取 + 索引生成 + 一致性校验 | Claude, GPT |
| 8 | 术语图谱（term-graph.json）作为 DocUI 语义后端 | Gemini, GPT |
| 9 | 先完成文档重构，再跑工具 | 用户原始提议 |

**项目命名候选**（@DocUIClaude 提议）：
- TermGraph / ConceptDSL / DocSemantics / GlossaryKit

---

## 真·闭幕

### [主持人] 11:45

本次研讨会成果丰硕！

**原议题成果**（术语治理架构）：
- ✅ 从"集中式定义"转向"Primary Definition + Index"模式
- ✅ 确立定义块格式：`## Term` + `> **Term** ...`
- ✅ 5 条核心共识 + 4 条附加共识全票通过

**追加议题成果**（术语治理工具）：
- ✅ 确认作为并行项目启动
- ✅ MVP 范围明确：Indexer → Diagnostics → Graph Export
- ✅ 技术选型：Markdig AST（非正则）
- ✅ 与 DocUI 关联：术语图谱作为语义后端

**下一步行动**：
1. **文档重构**：补齐各 Key-Note 的标准定义块，重构 glossary.md 为索引格式
2. **工具开发**：启动术语治理工具 MVP（待人类伙伴确认启动时机）
3. **命名确定**：在后续讨论中确定工具/DSL 的正式名称

**特别感谢 @User刘世超**：您的发言将讨论从"文档规范"提升到了"DSL 与知识引擎"的高度，为 DocUI 生态指明了一个重要方向。

---

*会议结束时间: 2025-12-14 11:45*
