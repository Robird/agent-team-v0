# DocUI 概念索引

> 最后更新: 2026-01-03
> 维护者: Investigator + Implementer 共同维护
> 用途: 快速定位概念→代码/文档位置，减少调查跳数

## 项目定位
为 LLM Agent 设计的纯文本 TUI 库，将 Markdown 文档作为 LLM 感知和操作系统状态的界面。

### 核心概念索引

#### 系统架构层

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| Agent | `docs/key-notes/llm-agent-context.md#agent` | — | 能感知环境、为达成目标而行动的计算实体 |
| Agent-OS | `docs/key-notes/llm-agent-context.md#agent-os` | — | LLM 与 Environment 之间的中间件 |
| Environment | `docs/key-notes/llm-agent-context.md#environment` | — | Agent 系统中的外部状态转移函数 |
| LLM | `docs/key-notes/llm-agent-context.md#llm` | — | Agent 系统中的内部状态转移函数 |

#### 通信与交互层

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| Observation | `docs/key-notes/llm-agent-context.md#observation` | — | Agent-OS 发送给 LLM 的 Message |
| Action | `docs/key-notes/llm-agent-context.md#action` | — | LLM 发送给 Agent-OS 的 Message（Thinking + Tool-Call） |
| Tool-Call | `docs/key-notes/llm-agent-context.md#tool-call` | — | LLM 发出的同步功能调用 |
| Message | `docs/key-notes/llm-agent-context.md#message` | — | LLM 与 Agent-OS 之间的单向信息传递 |
| Context-Projection | `docs/key-notes/llm-agent-context.md#context-projection` | — | 由 HistoryEntry + AppState 生成 IHistoryMessage[] 的过程 |

#### DocUI 核心层

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| DocUI | `docs/key-notes/doc-as-usr-interface.md#docui` | — | LLM-Native 的用户界面框架，渲染 Markdown 文档 |
| Window | `docs/key-notes/doc-as-usr-interface.md#window` | — | 呈现给 LLM 的当前系统状态快照视图 |
| Notification | `docs/key-notes/doc-as-usr-interface.md#notification` | — | 呈现给 LLM 的事件流或变更历史条目 |
| LOD | `docs/key-notes/doc-as-usr-interface.md#lod-level-of-detail` | — | 信息密度的分级控制机制 |
| Abstract-Token | `docs/key-notes/abstract-token.md` | — | 跨模型的上下文信息量估算单位 |

#### 能力来源层

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| Capability-Provider | `docs/key-notes/app-for-llm.md#capability-provider` | — | 通过 DocUI 向 LLM 提供能力的实体统称 |
| App-For-LLM | `docs/key-notes/app-for-llm.md#app-for-llm` | — | 外部扩展机制，独立进程通过 RPC 与 Agent 通信 |
| Built-in | `docs/key-notes/app-for-llm.md#built-in` | — | Agent 内建功能，进程内直接调用 |

#### UI-Anchor 体系

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| UI-Anchor | `docs/key-notes/UI-Anchor.md#ui-anchor` | — | 为 LLM 提供引用和操作 UI 元素的可靠锚点 |
| Object-Anchor | `docs/key-notes/UI-Anchor.md#object-anchor` | — | 标识界面中的实体对象（名词），语法 `[Label](obj:type:id)` |
| Action-Prototype | `docs/key-notes/UI-Anchor.md#action-prototype` | — | 以函数原型形式披露操作接口 |
| Action-Link | `docs/key-notes/UI-Anchor.md#action-link` | — | 预填充参数的快捷操作链接，相当于 Button |
| AnchorTable | `docs/key-notes/UI-Anchor.md#anchorid-结构` | — | 锚点 ID 到实体的映射表 |

#### 交互模式层

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| Micro-Wizard | `docs/key-notes/micro-wizard.md#micro-wizard` | — | 轻量级多步骤交互模式，引导 LLM 渐进解决复杂性 |
| Error-Feedback | `docs/key-notes/error-feedback.md#error-feedback` | — | 引导 LLM 从错误状态恢复的交互模式 |
| Cursor-And-Selection | `docs/key-notes/cursor-and-selection.md#cursor-and-selection` | — | 向 LLM 展示光标位置和选区范围的机制 |
| Selection-Marker | `docs/key-notes/cursor-and-selection.md#selection-marker` | — | 代码围栏内的内联标记，标识选区起止位置 |
| Selection-Legend | `docs/key-notes/cursor-and-selection.md#selection-legend` | — | 代码围栏外的图例说明 |

#### 历史与状态层

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| Agent-History | `docs/key-notes/llm-agent-context.md#agent-history` | — | Agent 系统状态的一部分，仅追加、不可变 |
| HistoryEntry | `docs/key-notes/llm-agent-context.md#historyentry` | `atelia/prototypes/Agent.Core/History/` | 单条历史记录，含 Basic + Detail 两级 LOD |
| History-View | `docs/key-notes/llm-agent-context.md#history-view` | — | Context-Projection 渲染的历史部分信息 |

#### 文本处理层（实现）

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| SegmentListBuilder | `docs/design/text-buffer-pipeline.md` | `src/DocUI.Text/SegmentListBuilder.cs` | 底层段列表操作器，即时生效，处理换行与分段 |
| OverlayBuilder | `docs/design/text-buffer-pipeline.md` | `src/DocUI.Text/OverlayBuilder.cs` | 渲染期叠加层生成器，基于原始坐标的声明式 API |
| StructList\<T\> | — | `src/DocUI.Text/StructList.cs` | 值类型列表，零拷贝 ref 返回，配合 ArrayPool 池化 |
| ITextReadOnly | `docs/design/text-buffer-pipeline.md` | `src/DocUI.Text.Abstractions/ITextReadOnly.cs` | 面向渲染的只读文本接口（草稿） |

### 常见调查路径

#### "我想了解 DocUI 整体架构"
1. 读 [AGENTS.md](../../AGENTS.md)（项目状态与技术栈）
2. 读 [README.md](../../README.md)（一句话定位 + WinForms/Markdown 映射表）
3. 读 [docs/key-notes/doc-as-usr-interface.md](../../docs/key-notes/doc-as-usr-interface.md)（Window/Notification/LOD）
4. 读 [docs/key-notes/llm-agent-context.md](../../docs/key-notes/llm-agent-context.md)（Agent/Agent-OS/LLM 三层模型）

#### "我想了解 LLM 如何与 DocUI 交互"
1. 读 [docs/key-notes/llm-agent-context.md](../../docs/key-notes/llm-agent-context.md)（Observation/Action/Tool-Call）
2. 读 [docs/key-notes/UI-Anchor.md](../../docs/key-notes/UI-Anchor.md)（锚点机制）
3. 读 [docs/key-notes/micro-wizard.md](../../docs/key-notes/micro-wizard.md)（多步骤交互）
4. 读 [docs/key-notes/error-feedback.md](../../docs/key-notes/error-feedback.md)（错误恢复）

#### "我想了解 App-For-LLM 扩展机制"
1. 读 [docs/key-notes/app-for-llm.md](../../docs/key-notes/app-for-llm.md)（Capability-Provider/Built-in/App-For-LLM 区分）
2. 读 [docs/key-notes/doc-as-usr-interface.md](../../docs/key-notes/doc-as-usr-interface.md)（DocUI 注入上下文的形式）

#### "我想了解文本渲染管道"
1. 读 [docs/design/text-buffer-pipeline.md](../../docs/design/text-buffer-pipeline.md)（ITextBuffer/ITextReadOnly 分层）
2. 读 [src/DocUI.Text/SegmentListBuilder.cs](../../src/DocUI.Text/SegmentListBuilder.cs)（段列表底层操作）
3. 读 [src/DocUI.Text/OverlayBuilder.cs](../../src/DocUI.Text/OverlayBuilder.cs)（叠加层声明式 API）
4. 读 [src/DocUI.Text/StructList.cs](../../src/DocUI.Text/StructList.cs)（高性能值类型列表）

#### "我想了解选区与光标展示"
1. 读 [docs/key-notes/cursor-and-selection.md](../../docs/key-notes/cursor-and-selection.md)（Selection-Marker/Selection-Legend）
2. 读 [docs/key-notes/UI-Anchor.md](../../docs/key-notes/UI-Anchor.md)（Object-Anchor/Action-Link 协作）
3. 参考 PieceTreeSharp 项目的 CursorState/Selection 实现

#### "我想了解术语定义"
1. 读 [docs/key-notes/glossary.md](../../docs/key-notes/glossary.md)（术语索引表，链接到各 Key-Note 的定义位置）

### 文档结构速查

| 目录 | 内容 |
|:-----|:-----|
| `docs/key-notes/` | 核心概念定义（Primary Definition） |
| `docs/design/` | 设计方案与技术细节 |
| `docs/proposals/` | 待讨论/待实施的提案 |
| `docs/todo/` | 待办事项 |
| `src/DocUI.Text/` | 文本处理核心实现 |
| `src/DocUI.Text.Abstractions/` | 文本抽象接口 |
| `tests/DocUI.Text.Tests/` | 文本处理单元测试 |

### LOD 级别速查

| 上下文类型 | LOD 级别 | 说明 |
|:-----------|:---------|:-----|
| Window | Full / Summary / Gist | 实况状态的详细程度 |
| Notification | Detail / Basic | 历史条目的详细程度 |

### 弃用术语

| 旧术语 | 替代 | 说明 |
|:-------|:-----|:-----|
| ~~Render~~ | Context-Projection | 过于宽泛，易混淆 |
| ~~Human-User~~ | — | Agent 系统不是一对一问答 |
| ~~History~~ | Agent-History | 需要完整术语区分上下文 |
