# DocUI Key-Notes 首次智囊团审阅

> **形式**: 智囊团循环 - 审阅阶段 (Staff Cycle - Audit Phase)
> **日期**: 2025-12-15
> **主持**: DocUI 规范起草委员会执行委员会主席 (刘德智)
> **审阅范围**: `DocUI/docs/key-notes/` 全部文档

---

## 审阅目标

针对 Key-Notes 进行三维度审阅：

| 维度 | 定义 | 示例问题 |
|------|------|----------|
| **自洽性** | 文档之间不能有矛盾 | 术语定义冲突、概念关系不一致 |
| **合理性** | 设计是否可行、有无逻辑漏洞 | 不可实现的承诺、未考虑的边界情况 |
| **完备性** | 是否覆盖必要的概念 | 缺失的术语定义、未说明的交互模式 |

---

## 当前文档清单

| 文档 | 主要内容 | 状态 |
|------|----------|------|
| `glossary.md` | 术语索引（指向各 Key-Note） | 索引 |
| `llm-agent-context.md` | Agent/LLM/Environment 核心概念 | Stable |
| `doc-as-usr-interface.md` | DocUI/Window/Notification/LOD | Stable |
| `app-for-llm.md` | Capability-Provider/Built-in/App | Stable |
| `UI-Anchor.md` | Object-Anchor/Action-Link/Action-Prototype | Draft |
| `micro-wizard.md` | 微向导机制 | Draft |
| `cursor-and-selection.md` | 选区标记机制 | Draft |
| `abstract-token.md` | （待确认内容） | ? |
| `key-notes-drive-proposals.md` | （元文档？） | ? |

---

## Specialist 审阅分工

| Specialist | 审阅视角 | 重点关注 |
|------------|----------|----------|
| **DocUIClaude** | 概念框架 | 术语一致性、概念完备性、逻辑自洽 |
| **DocUIGemini** | UX/交互 | 交互模式合理性、用户体验、可发现性 |
| **DocUIGPT** | 规范审计 | 命名约定、文档格式、代码示例准确性 |

---

## 问题清单

> 以下由各 Specialist 审阅后填充

### DocUIClaude 发现的问题

| # | 问题 | 类型 | 严重度 | 涉及文档 | 建议 |
|---|------|------|--------|----------|------|
| 1 | `AnchorTable` glossary 摘要与正文定义不一致 | 自洽 | 中 | glossary.md, UI-Anchor.md | 统一定义，明确是映射表还是数据结构 |
| 2 | `Recent History` 未收录至 glossary | 完备 | 中 | doc-as-usr-interface.md, glossary.md | 添加术语定义或标记为"策略概念" |
| 3 | `WizardView` 未定义 | 完备 | 中 | micro-wizard.md, glossary.md | 添加术语定义 |
| 4 | `Attention Focus` 多处提及但无正式定义 | 完备 | 高 | llm-agent-context.md, doc-as-usr-interface.md | 在 glossary 或专门 Key-Note 中正式定义 |
| 5 | `AppState` 作为 Context-Projection 关键输入但未定义 | 完备 | 高 | llm-agent-context.md, glossary.md | 添加术语定义 |
| 6 | Window LOD 与 Notification LOD 级别名称不统一 | 合理 | 低 | doc-as-usr-interface.md | 说明设计理由或统一命名 |
| 7 | Selection-Marker 与 Object-Anchor 关系定位模糊 | 自洽 | 中 | cursor-and-selection.md, UI-Anchor.md | 明确是 UI-Anchor 的子类还是独立概念 |
| 8 | `epoch` 在 UI-Anchor.md 中使用但未正式定义 | 完备 | 低 | UI-Anchor.md, glossary.md | 添加定义或在正文中解释 |
| 9 | Action 定义与 Action-Link/Action-Prototype 命名空间冲突 | 自洽 | 中 | llm-agent-context.md, UI-Anchor.md | 考虑 UI-Anchor 中使用前缀如 `UI-Action-*` |
| 10 | `Token 预算` 多处提及但缺乏正式定义 | 完备 | 低 | llm-agent-context.md, doc-as-usr-interface.md | 添加术语或在 LOD 部分正式说明 |

#### 详细说明

**问题 1: AnchorTable 定义不一致**

glossary.md 摘要: "锚点 ID 到实体的映射表"
UI-Anchor.md 正文: 节标题为 "AnchorId 结构"，描述的是四元组结构 `kind + providerId + sessionId + localId`

建议: glossary 中 AnchorTable 应指向正确的定义位置，或在 UI-Anchor.md 中添加独立的 AnchorTable 定义节。

**问题 4: Attention Focus 无正式定义**

出现位置:
- llm-agent-context.md: "当前 Attention Focus（决定 LOD 选择）"
- doc-as-usr-interface.md: "根据注意力焦点应用不同的详细等级"、待消化建议中详细描述

这是一个核心概念，决定了 LOD 选择逻辑，但仅在"待消化建议"中有描述，未正式定义。

**问题 5: AppState 未定义**

llm-agent-context.md 中 Context-Projection 的输入明确包含"各 Capability-Provider 的 AppState"，且在 Mermaid 图中也有体现，但 AppState 未在任何地方定义其结构和语义。

**问题 6: LOD 级别名称不统一**

| 类型 | LOD 级别 |
|------|----------|
| Window | Full, Summary, Gist |
| Notification | Detail, Basic |
| HistoryEntry | Basic, Detail |

这种不一致是有意设计还是遗留问题？如果是有意设计，应说明理由。

**问题 9: Action 命名空间冲突**

- llm-agent-context.md 定义 `Action` 为 "LLM 发送给 Agent-OS 的 Message"
- UI-Anchor.md 定义 `Action-Link` 和 `Action-Prototype` 作为 UI 元素

虽然语义相关，但 `Action` 在一个文档中是 Message 级别概念，在另一个文档中是 UI 元素。建议统一术语层次或添加区分前缀。

---

### DocUIGemini 发现的问题

| # | 问题 | 类型 | 严重度 | 涉及文档 | 建议 |
|---|------|------|--------|----------|------|
| 1 | 缺少标准化的"刷新/重同步"机制 | 错误处理 | 高 | UI-Anchor.md | 定义标准 `refresh()` Built-in Action，并在 Anchor 失效报错中明确引导调用。 |
| 2 | 对象与动作的关联示能性 (Affordance) 不足 | 可发现性 | 中 | UI-Anchor.md | 在 Full/Summary LOD 中，建议在对象旁渲染上下文相关的 Action-Link（类似右键菜单），而不仅依赖全局 Prototype。 |
| 3 | 代码内联标记 (Selection-Marker) 可能导致代码污染 | 交互模式 | 中 | cursor-and-selection.md | 需在 System Prompt 中强化"Overlay"概念，或考虑在 Legend 中增加"Copy Safe"的明确指示，防止 LLM 将 `<sel:1>` 写入代码。 |
| 4 | 脚本式执行中的"锚点失效"隐患 | 一致性 | 高 | UI-Anchor.md | 明确"短路 (Short-Circuit)"机制的报错文案，必须指出"因前序操作导致状态变更，锚点已失效"，引导 LLM 重新观测。 |
| 5 | 缺乏"操作反馈"的标准 UI 模式 | 交互模式 | 中 | doc-as-usr-interface.md | 定义 Action 点击后的即时反馈状态（Pending/Optimistic UI），避免 LLM 在长任务中重复点击。 |

#### 详细说明

**问题 1：缺少标准化的"刷新/重同步"机制**
文档提到当 Anchor 失效时返回 "Please refresh to get current IDs"，但并未在 `Built-in` 能力或 `UI-Anchor` 规范中定义什么是 "refresh"。LLM 需要一个明确的、通用的手段来"重新加载当前 Window"以获取最新 ID，而不是猜测该调用哪个工具。建议将 `refresh_context()` 作为 DocUI 的基础原语。

**问题 2：对象与动作的关联示能性不足**
目前的 `Action-Prototype` (动词) 和 `Object-Anchor` (名词) 是分离展示的。这要求 LLM 具备较强的推理能力来将两者匹配（"这个 `attack` 函数可以用在这个 `obj:enemy` 上吗？"）。从 UX 角度，**就近展示**（Proximity）是提升可发现性的关键。建议允许在对象锚点旁渲染常用的 Action-Link，例如 `[史莱姆1](obj:1) [⚔️](link:attack "attack(obj:1)")`。

**问题 3：代码内联标记的干扰风险**
在代码块中插入 `<sel:1>` 等标记虽然直观，但对于代码生成的 LLM 来说，存在极高的风险将其误认为是代码的一部分并进行补全或复制。建议：
1. 明确标记的语法尽量"非代码化"（虽然 `<>` 已经很像 XML/泛型了）。
2. 在 `Selection-Legend` 中明确声明 "Tags are for display only, do not include in code"。

**问题 4：脚本式执行中的"锚点失效"隐患**
文档提倡 `attack(obj:1); loot(obj:1)` 的脚本式执行。然而，`attack` 成功后 `obj:1` (敌人) 可能消失或变成尸体（新 ID）。此时 `loot` 执行时的"解引用"会失败。如果报错信息不明确，LLM 可能会认为系统故障。错误信息必须解释因果关系："Target obj:1 no longer exists (likely removed by previous action 'attack')."

**问题 5：缺乏"操作反馈"的标准 UI 模式**
在 GUI 中，点击按钮会有按压态或 Loading 态。在 DocUI 中，LLM 发出 Action 后，如果系统处理较慢，LLM 看到的 Window 仍然是旧的。这可能导致 LLM 产生"操作未生效"的幻觉并重复操作。需要定义一种机制（如临时的 Notification 或 Window 顶部的 Status Banner）来告知 LLM "操作正在处理中"。

---

### DocUIGPT 发现的问题

| # | 问题 | 类型 | 严重度 | 涉及文档 | 建议 |
|---|------|------|--------|----------|------|
| 1 | 文件名大小写不一致：`UI-Anchor.md` vs 其他小写文件 | 自洽 | 中 | UI-Anchor.md | 统一为小写连字符 `ui-anchor.md` 或记录命名规范 |
| 2 | 状态标记 `Draft` 与实际成熟度不符 | 合理 | 中 | UI-Anchor.md, micro-wizard.md | UI-Anchor.md 内容详实应为 Stable；或定义 Draft/Stable 标准 |
| 3 | 代码围栏转义错误：内嵌围栏未正确转义 | 自洽 | 高 | llm-agent-context.md, cursor-and-selection.md | 修正四反引号嵌套或空格分隔问题 |
| 4 | 术语连字符风格不一致 | 自洽 | 中 | 多个文档 | `Tool-Call` vs `Tool Call`；`App-For-LLM` 内部统一但交叉引用时有变化 |
| 5 | glossary.md AnchorTable 指向 `#anchorid-结构` 但应为独立节 | 完备 | 低 | glossary.md, UI-Anchor.md | 在 UI-Anchor.md 中添加 `## AnchorTable` 独立节 |
| 6 | Mermaid 图中反引号转义导致渲染问题 | 自洽 | 中 | doc-as-usr-interface.md | 移除 Mermaid 代码块外的反引号转义 |
| 7 | 示例代码语言标记不统一 | 自洽 | 低 | UI-Anchor.md, micro-wizard.md | 统一使用 `typescript` 或 `csharp`；Action-Prototype 示例语言应明确 |
| 8 | TODO 节残留在 Stable 文档中 | 合理 | 低 | llm-agent-context.md, doc-as-usr-interface.md | Stable 文档不应有 TODO；移至 issue tracker 或降级为 Draft |
| 9 | 引用格式不一致：部分用相对路径，部分用锚点 | 自洽 | 低 | 多个文档 | 统一引用格式：`[术语](file.md#anchor)` |
| 10 | Selection-Marker 示例代码块空格干扰 | 自洽 | 中 | cursor-and-selection.md | `` ` ` ` `` 应为 ` ``` `（去除空格） |
| 11 | `WizardView` 在 micro-wizard.md 中使用但未定义 | 完备 | 中 | micro-wizard.md, glossary.md | 添加术语定义或在文中首次出现时定义 |
| 12 | 弃用术语节格式不统一 | 自洽 | 低 | glossary.md, llm-agent-context.md | glossary 用表格，llm-agent-context 用标题；选择统一格式 |

#### 详细说明

**问题 1: 文件名大小写不一致**

| 文件 | 命名风格 |
|------|----------|
| `llm-agent-context.md` | 小写连字符 ✅ |
| `doc-as-usr-interface.md` | 小写连字符 ✅ |
| `app-for-llm.md` | 小写连字符 ✅ |
| `UI-Anchor.md` | 首字母大写 ❌ |
| `micro-wizard.md` | 小写连字符 ✅ |
| `cursor-and-selection.md` | 小写连字符 ✅ |

建议统一为小写连字符，或在文档中明确命名规范（如"术语名大写时文件名也大写"）。

**问题 3: 代码围栏转义错误**

`llm-agent-context.md` 开头使用四反引号 ```` ```` markdown```` 包裹整个文档，这是非标准用法，可能导致某些渲染器问题。

`cursor-and-selection.md` 中为避免符号碰撞使用 `` ` ` ` `` 加空格，但这并非有效的代码围栏语法：
```markdown
` ` `csharp
// 这不会被识别为代码块
` ` `
```

应使用四反引号 ` ```` ` 或在外层使用不同数量的反引号。

**问题 4: 术语连字符风格**

glossary.md 声明"复合术语使用连字符连接"，但实际文档中存在不一致：

| 位置 | 写法 | 是否符合规范 |
|------|------|-------------|
| llm-agent-context.md | `Tool-Call` | ✅ |
| UI-Anchor.md | `tool calling` | ❌ 应为 Tool-Calling |
| app-for-llm.md | `tool calling` | ❌ |

**问题 6: Mermaid 图转义**

`doc-as-usr-interface.md` 中 Mermaid 图使用了 `\`\`\`mermaid` 形式的转义：
```
\`\`\`mermaid
```

这会导致原始 Markdown 查看时显示转义字符，影响可读性。应直接使用标准 Mermaid 代码块。

**问题 8: Stable 文档含 TODO**

| 文档 | 状态 | TODO 数量 |
|------|------|----------|
| llm-agent-context.md | Stable | 5 条 |
| doc-as-usr-interface.md | Stable | 1 条 |

Stable 状态意味着"已稳定可采用"，但含有 TODO 表明仍有未决事项。建议：
- 将 TODO 移至独立的 issue/task 文件
- 或将文档状态降级为 Draft

**问题 10: 代码块空格问题**

`cursor-and-selection.md` 第 68-76 行：
```markdown
` ` `csharp
class MyClass {
    <sel:1>public const</sel:1><sel:2>private static readonly</sel:2> string DefaultName = "some-name";
}
` ` `
```

反引号间的空格导致这不是有效的围栏代码块。这可能是为了在外层围栏中嵌套演示，但结果是示例本身语法错误。

**问题 11: WizardView 未定义**

`micro-wizard.md` 中：
> Agent-OS 捕获歧义，返回 `WizardView` Observation

`WizardView` 作为核心概念出现但未在 glossary 或本文中定义其结构。应添加定义：
```markdown
> **WizardView** 是 Micro-Wizard 触发时返回的特殊 Observation 结构，包含引导性提示和可选项列表。
```

---

## 汇总与优先级排序

> 由主席汇总三位 Specialist 的发现，去重合并，按优先级排序

### 高优先级问题（5 个）

| # | 问题 | 发现者 | 涉及文档 |
|---|------|--------|----------|
| H1 | `Attention Focus` 核心概念无正式定义 | Claude | llm-agent-context, doc-as-usr-interface |
| H2 | `AppState` 作为 Context-Projection 输入但未定义 | Claude | llm-agent-context |
| H3 | 缺少标准化的"刷新/重同步"机制 | Gemini | UI-Anchor |
| H4 | 脚本式执行中"锚点失效"的报错文案需明确 | Gemini | UI-Anchor |
| H5 | 代码围栏转义错误导致示例失效 | GPT | cursor-and-selection, llm-agent-context |

### 中优先级问题（10 个）

| # | 问题 | 发现者 | 涉及文档 |
|---|------|--------|----------|
| M1 | `WizardView` 未定义 | Claude, GPT | micro-wizard, glossary |
| M2 | `AnchorTable` 定义不一致 | Claude | glossary, UI-Anchor |
| M3 | Selection-Marker 与 Object-Anchor 关系模糊 | Claude | cursor-and-selection, UI-Anchor |
| M4 | Action 命名空间冲突（Message vs UI 元素） | Claude | llm-agent-context, UI-Anchor |
| M5 | 对象与动作的关联示能性不足 | Gemini | UI-Anchor |
| M6 | Selection-Marker 可能被 LLM 误写入代码 | Gemini | cursor-and-selection |
| M7 | 缺乏"操作反馈"的标准 UI 模式 | Gemini | doc-as-usr-interface |
| M8 | 文件名大小写不一致 (`UI-Anchor.md`) | GPT | UI-Anchor |
| M9 | 状态标记 Draft/Stable 与成熟度不符 | GPT | UI-Anchor, micro-wizard |
| M10 | 术语连字符风格不一致 | GPT | 多个文档 |

### 低优先级问题（7 个）

| # | 问题 | 发现者 |
|---|------|--------|
| L1 | Window LOD 与 Notification LOD 级别名称不统一 | Claude |
| L2 | `epoch`、`Token 预算` 等术语未正式定义 | Claude |
| L3 | `Recent History` 未收录至 glossary | Claude |
| L4 | glossary 锚点指向问题 | GPT |
| L5 | 示例代码语言标记不统一 | GPT |
| L6 | Stable 文档含 TODO 节 | GPT |
| L7 | 弃用术语节格式不统一 | GPT |

---

## 下一步

1. 各 Specialist 完成审阅，填充问题清单
2. 主席汇总去重，形成统一问题清单
3. 针对高优先级问题进行研讨
4. 形成决策摘要供监护人审批
5. 根据决策实施修订

---

*本文件创建于 2025-12-15，首次智囊团审阅*

