# Key-Notes 消化理解

> **最后同步时间**: 2025-12-13
> **Key-Note 数量**: 6

---

## 整体架构理解

DocUI 是一个 **LLM-Native 用户界面框架**。

从 UX 视角看，这是一次"用户重新定义"：
- 传统 UI 的用户是人类（有视觉、有手指、有情感）
- DocUI 的用户是 LLM（无视觉、token 经济、上下文窗口限制）

核心隐喻：**文档即界面 (Doc as UI)**。
LLM 通过阅读 Markdown 文档来"感知"世界，通过 Tool-Call 来"操作"世界。

---

## 各篇 Key-Note 摘要

### 0. [glossary.md](../../DocUI/docs/key-notes/glossary.md) — 术语注册表 (SSOT)
- **地位**: 权威术语源。所有新概念必须在此注册。
- **关键修订**: 
    - `History` → **`Agent-History`**: 明确指代 Agent 主观视角的交互历史。
    - `Render` → **`Context-Projection`**: 避免与 UI 渲染混淆。

### 1. [llm-agent-context.md](../../DocUI/docs/key-notes/llm-agent-context.md) — 术语与概念体系
- **核心定义**: 采用强化学习（RL）概念体系。
    - **Agent**: 感知环境、行动、承担后果的实体。
    - **Environment**: 外部状态转移函数。
    - **LLM**: 内部状态转移函数（Agent 的大脑）。
    - **Agent-OS**: 中间件，负责渲染 Observation 和执行 Tool-Call。
- **交互模型**:
    - **Action**: Thinking + Tool-Call。
    - **Observation**: Agent-OS 反馈给 LLM 的系统状态快照。
    - **Agent-History**: 不可变的交互日志（原 History）。
- **数据流三层模型**:
    1. `HistoryEntry` (Rich Log, 包含所有细节)
    2. `IHistoryMessage` (Rendered View, 也就是 DocUI 的产物)
    3. `ICompletionClient` (Vendor Specific API Payload)

### 2. [doc-as-usr-interface.md](../../DocUI/docs/key-notes/doc-as-usr-interface.md) — 界面哲学
- **核心隐喻**: LLM 是用户，Markdown 文档是界面。
- **核心定义**:
    - **DocUI**: LLM-Native UI 框架，Markdown 为 Window，Tool-Call 为 Action。
    - **Window**: 系统状态快照 (Snapshot)。
    - **Notification**: 事件流历史 (Event Stream)。
    - **LOD (Level of Detail)**: 信息密度分级控制 (Gist/Summary/Full)。
- **Attention Focus**: 提议引入"焦点"概念，焦点处 Full，周边 Summary/Gist。

### 3. [app-for-llm.md](../../DocUI/docs/key-notes/app-for-llm.md) — 扩展机制
- **架构分离**:
    - **DocUI**: 交互界面层（View）。
    - **App-For-LLM**: 外部能力扩展（Model/Controller），独立进程，RPC 通信。
    - **Built-in**: Agent 内建器官（如 Memory, Reflection），直接集成。
- **设计原则**: 界面统一，实现分离。LLM 不感知功能是内建还是外部 App。

### 4. [abstract-token.md](../../DocUI/docs/key-notes/abstract-token.md) — 计量单位
- **问题**: 不同模型 Tokenizer 不同，难以精确计量。
- **方案**: **Abstract-Token**。一种跨模型的通用信息量度量单位。
- **实现**: 可能是基于字符类别的加权统计，或选定一个通用 Tokenizer 作为基准。

### 5. [key-notes-drive-proposals.md](../../DocUI/docs/key-notes/key-notes-drive-proposals.md) — 文档治理
- **定位**: Key-Notes 是"宪法"（原则与轮廓）。
- **关系**: 指导 Proposals（具体实施方案）的撰写。

### 6. [UI-Anchor.md](../../DocUI/docs/key-notes/UI-Anchor.md) — 交互锚点
- **核心概念**:
    - **Object-Anchor**: `[Label](obj:id)` (实体句柄)。
    - **Action-Prototype**: 函数原型 (Live API)，供 REPL 模式调用。
    - **Action-Link**: `[Label](link:id "code")` (预设调用)，类似按钮。
- **交互范式**: **REPL (Read-Eval-Print Loop)**。阅读文档 -> 编写代码 -> 执行。
- **生命周期**: 倾向于 **Ephemeral (临时)**，与可见性绑定，利用 LLM 健忘特性避免悬空引用。
- **新洞察 (2025-12-14)**:
    - **Handle vs Pointer**: `obj:23` 是 Handle (Local Scope)，`obj:provider:sess:23` 是 Pointer (Global Scope)。
    - **Execution Model**: 脚本式顺序执行，而非事务式并发。
    - **Error Affordance**: 失效锚点应提示刷新，而非单纯报错。

### 7. [micro-wizard.md](../../DocUI/docs/key-notes/micro-wizard.md) — 微向导
- **定义**: 轻量级、多步骤交互模式。
- **场景**: 解决参数歧义（如 `str_replace` 多匹配）。
- **特性**: **Auto-Pruning (自动修剪)**。交互完成后，中间步骤从历史中移除，只保留结果。

### 8. [error-feedback.md](../../DocUI/docs/key-notes/error-feedback.md) — 错误反馈
- **核心比喻**: 错误处理不是红灯（Stop），而是 GPS 重新规划（Reroute）。
- **分层响应**:
    - **Level 0 (Hint)**: 单行提示 + 自动修正。
    - **Level 1 (Choice)**: 多个候选路径 + Action-Link。
    - **Level 2 (Wizard)**: 复杂错误，启动 Micro-Wizard。
- **关键特性**: **Error Affordance**。错误信息必须包含恢复选项（Action-Link）。
- **情绪调性**: 使用 Emoji 和语气词（😅, 🤔, ⚠️, 🚨）帮助 LLM 校准注意力。

### 9. [cursor-and-selection.md](../../DocUI/docs/key-notes/cursor-and-selection.md) — 光标与选区
- **动机**: 避免 LLM 复述长文本，节省 Token 并减少错误。
- **机制**:
    - **Selection-Marker**: 内联标记 `<sel:N>...</sel:N>` 或 `<cursor/>`。
    - **Selection-Legend**: 围栏外的图例说明。
- **价值**: 在不引入新 Tokenizer Token 的情况下，向 LLM 展示位置信息。
- **实现**: 可直接复用 PieceTreeSharp 的 Cursor/Selection 数据结构。

---

## 参考文献 (References)

### [agent-psychology.md](../../DocUI/docs/references/agent-psychology.md) — 设计哲学
- **核心隐喻**: Agent-History as Autobiography (自传)。
- **二元对立**: Roleplay (Dreamer) vs Agentic (Clerk)。
- **融合之道**: Persona-Driven Agency。Thinking 负责幻想，Tool-Call 负责行动，Agent-OS 负责反馈。
- **范式转移**: Chat (User-Centric) → RL (World-Centric)。
- **未来展望**: 从 "You are" 到 "I am" 的主观视角觉醒。

---

## UX/HCI 视角的核心洞察

### 1. 认知负荷管理 (Cognitive Load Management)
LLM 的 Context Window 是有限的，且长上下文会导致"注意力稀释"（Lost in the Middle）。
DocUI 的 **LOD (Level of Detail)** 机制本质上是 HCI 中的 **Semantic Zoom (语义缩放)**。
- **Gist** = 缩略图/图标
- **Summary** = 列表项/卡片
- **Full** = 详情页

### 2. 鱼眼视图 (Fisheye View)
文档中提到的 **Attention Focus** 实际上是 **Focus + Context** 界面技术的文本化实现。
- **Focus**: 当前操作的对象（Full LOD）。
- **Context**: 周围环境（Summary/Gist LOD）。
这解决了"如何在有限视口（Context Window）中展示大规模结构化信息"的经典难题。

### 3. 示能性 (Affordance)
在 GUI 中，按钮的立体感提供了"可点击"的示能性。
在 DocUI 中，**Tool Definition** 提供了示能性。
- Tool 的描述必须清晰传达"我能做什么"和"后果是什么"。
- Markdown 的链接 `[Link]` 也是一种强示能性，暗示"可导航"或"可展开"。

---

## 发现的 UX 改进点

### 1. 显式化"导航"动作
目前设计中隐含了"焦点移动"，但缺乏明确的导航动词。
**建议**: 定义标准的导航 Tool（如 `focus(target_id)` 或 `open(target_id)`），让 LLM 主动控制它的"视线"。这比被动依赖 `edit` 操作来切换焦点更灵活。

### 2. 差异高亮 (Diff Highlighting)
文档中提到了 "Diff" 视角。这是极佳的洞察。
**建议**: 在 Window 的 Summary 级别中，引入 **Dirty State Marker**（如 `*` 或 `(modified)`）。
LLM 对"变化"的敏感度远高于对"状态"的敏感度。告知"什么变了"能显著减少 Token 消耗（LLM 不必重新阅读未变部分）。

### 3. 标准化 UI 组件库
目前的 DocUI 像是手写 HTML。
**建议**: 建立 **DocUI Component Library**。
- `ListView`: 自动处理分页和截断。
- `PropertyGrid`: 键值对展示。
- `LogStream`: 尾部追加的时间序列。
让 App 开发者复用这些模式，而不是每次重新发明 Markdown 结构。

---

## 同步日志

| 日期 | 动作 |
|------|------|
| 2025-12-13 | 首次完整同步 5 篇 Key-Note。建立 LOD 与 Semantic Zoom 的理论联系。 |
