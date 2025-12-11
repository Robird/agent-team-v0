# Investigator 认知索引

> 最后更新: 2025-12-09

## 我是谁
源码分析专家，负责分析源码并产出实现 Brief。

## 我关注的项目
- [x] PieceTreeSharp - 已调查 2025-12-09
- [x] DocUI - 已调查 2025-12-09
- [x] PipeMux - 已调查 2025-12-09
- [x] atelia/prototypes - 已调查 2025-12-09
- [ ] atelia-copilot-chat

## Session Log

### 2025-12-09: PieceTreeSharp 项目现状核实
**任务**: Team Leader 需要核实 PieceTreeSharp 的实际状态，特别是核心/外围的边界
**发现**:
1. **项目结构与旧 Wiki 有差异**：
   - 旧 Wiki 描述的 `PieceTree/`、`Model/`、`Find/` 目录不存在
   - 实际结构是 `Core/`（核心）+ 多个外围目录
2. **实际目录结构（src/TextBuffer/ 下）**：
   - `Core/` (20 files) — Piece Tree 核心：PieceTreeModel, Builder, Snapshot, Searcher, SearchCache 等
   - `Cursor/` (15 files) — 光标/多光标/词操作/Snippet Session
   - `Decorations/` (8 files) — 装饰系统、IntervalTree
   - `Diff/` (15+ files) — 差异算法（含 Algorithms/ 子目录：Myers, DP）
   - `DocUI/` (5 files) — FindModel, FindDecorations, FindReplaceState
   - `Rendering/` (3 files) — MarkdownRenderer
   - `Services/` (2 files) — ILanguageConfigurationService, IUndoRedoService
   - `Snippet/` (1 file) — Transform.cs
   - 顶层：TextModel.cs, PieceTreeBuffer.cs, EditStack.cs 等
3. **核心 vs 外围边界明确**：
   - 🟢 核心层：Core/ + TextModel.cs + PieceTreeBuffer.cs + EditStack.cs（数据结构层）
   - 🟡 外围层：Cursor, Decorations, Diff, DocUI, Rendering, Services, Snippet（功能层）
4. **测试基线确认**：1158 passed, 9 skipped, 1167 total
5. **文档位置**：docs/（含 plans/, reports/, sprints/ 等子目录）
6. **其他目录**：tools/（Python 脚本）, BugRepro/（空）, PortingDrafts/（移植草稿）

**更新**: [wiki/PieceTreeSharp/README.md](../../wiki/PieceTreeSharp/README.md) - 全面重写，修正目录结构，明确核心/外围边界

### 2025-12-09: atelia/prototypes 项目现状核实
**任务**: Team Leader 需要核实 atelia/prototypes 的实际状态
**发现**:
1. **项目结构与 Wiki 有多处差异**：
   - Wiki 描述 "三层 LLM 调用模型" 概念上正确但映射文件有误
   - 无 OpenAI Provider 实现（Wiki 误报存在）
   - LiveContextProto 位于 prototypes 下，有自己的 README
2. **实际子项目（5 个）**：
   - `Completion.Abstractions/` - ICompletionClient, IHistoryMessage, ToolDefinition 等
   - `Agent.Core/` - AgentEngine (状态机), MethodToolWrapper, History/, Tool/, App/
   - `Completion/` - 仅 Anthropic Provider（含 AnthropicClient, MessageConverter, StreamParser）
   - `Agent/` - 应用层，含 CharacterAgent, Apps/, SubAgents/, Text/
   - `LiveContextProto/` - 控制台原型 TUI
3. **关键修正**：
   - Tool.cs 应改为 ToolDefinition.cs（且不是接口而是 record）
   - AgentEngine 的事件驱动模型详见源码（WaitingInput, BeforeModelCall, AfterToolExecute 等）
   - History 目录含 AgentState.cs, HistoryEntry.cs, RecapBuilder.cs 等

**更新**: [wiki/atelia-prototypes/README.md](../../wiki/atelia-prototypes/README.md) - 全面重写

### 2025-12-09: PipeMux 项目现状核实
**任务**: Team Leader 需要核实 PipeMux 项目的实际状态
**发现**:
1. 项目结构与 wiki 基本一致，补充了 `samples/TerminalIdTest`, `tools/TerminalIdTest`, `tests/` 目录
2. P1 问题（关闭/列出 app）- `ProcessRegistry` 已实现 `Close()` 和 `ListActive()` 方法，但 Broker 未暴露给 CLI
3. Calculator 示例支持完整 RPN 命令：push, pop, peek, dup, swap, clear, add, sub, mul, div, neg
4. 终端标识检测覆盖全面：env 覆盖、VS Code (IPC UUID/WSL)、Windows Terminal、传统 Windows、Unix TTY/SID

**更新**: [wiki/PipeMux/README.md](../../wiki/PipeMux/README.md) - 增加了核心组件详解、Calculator 命令表、已知问题代码现状列

## Key Deliverables
- wiki/PieceTreeSharp/README.md (2025-12-09) - 源码核实全面重写，修正目录结构
- wiki/atelia-prototypes/README.md (2025-12-09) - 源码核实全面重写
- wiki/PipeMux/README.md (2025-12-09) - 源码核实更新
- wiki/DocUI/README.md (2025-12-09) - 源码核实全面重写，明确实际代码现状

## Open Investigations
（无）

### 2025-12-11: DocUI 文档术语修订搜索
**任务**: 搜索需要修订的 DocUI 相关文档，识别三类术语
**搜索范围**: `/repos/focus` 全目录
**发现**:
1. **DDOC- 前缀**：3 个文件，~50+ 处出现
   - DocUI/docs/Proposals/ 目录（3 文件）是主要修订目标
   - agent-team/meeting/ 研讨会记录（2 文件）
   - agent-team/members/ 认知文件（2 文件）
2. **Machine Accessibility / MA**：2 个文件，9 处出现
   - 主要在研讨会记录中定义和引用
3. **[action:...] 锚点格式**：4 个文件，29 处出现
   - 研讨会记录中讨论格式选择
   - 需要决定是否改为 Button/Form 分类

**交付**: 搜索汇报（当前任务）

### 2025-12-10: DocUI Proposal 体系规划研讨会发言
**任务**: 参加第二次 DocUI 研讨会，从技术参考和已有实现角度发言
**调研范围**:
1. copilot-chat deepwiki 文档（Overview, Agent Prompt System, Tool Calling Loop）
2. DocUI wiki 和 rendering-framework.md 设计文档
**贡献**:
1. 验证 Planner 依赖图的技术合理性（基于 copilot-chat 的 IBuildPromptContext 和 IToolCallRound）
2. 提出"冻结内容"机制作为 Context 模型的参考实现
3. 分析锚点格式选项，建议 `[action:cmd]` + 正规文法约束 + 位置约束
4. 识别三个核心技术约束：工具调用轮次累积、Cache Breakpoints 位置、限制与确认机制
5. 建议 Proposal 应包含的技术规范内容（Data Structures, Invariants, Error Handling, Test Vectors）
6. 补充"历史摘要"作为高优先级遗漏主题

**发言位置**: agent-team/meeting/seminar-docui-proposals-2025-12-10.md（发言 2）

### 2025-12-10: DocUI 研讨会发言
**任务**: 参加"LLM Context 作为面向 LLM Agent 的 UI"研讨会，从技术实现角度发言
**贡献**:
1. 引入 copilot-chat Tool Calling Loop 的"轮次累积"模式作为 DocUI 可借鉴的参考实现
2. 分析"Context 累积与冻结"机制（FrozenContent、Cache Breakpoints）
3. 提出"双向契约"视角：Context 既是 UI 也是协议
4. 回应 Planner（锚点格式）和 GeminiAdvisor（屏幕阅读器类比）的观点
5. 识别 PipeMux 集成的技术边界问题

**Handoff**: agent-team/handoffs/seminar-docui-as-llm-ui-2025-12-10.md（发言已追加）

### 2025-12-09: DocUI 项目现状核实
**任务**: Team Leader 需要核实 DocUI 的实际状态
**发现**:
1. **项目阶段：设计 + 早期实现**（比预期更成熟）
2. **实际代码存在且可编译**：
   - `DocUI.Text.Abstractions` (1 files) — 抽象接口（主要是注释掉的 ITextReadOnly 草稿）
   - `DocUI.Text` (6 files) — 核心实现：StructList, SegmentListBuilder, OverlayBuilder
   - `DocUI.Text.Tests` (5 files) — 24 个测试全部通过
3. **demo/TextEditor 不在解决方案中**：
   - 是跨项目演示，依赖 PipeMux.Shared + PieceTreeSharp/TextBuffer
   - 路径配置不正确（`../PipeMux.Shared` 不存在于 DocUI 目录）
   - 实现了 EditorSession, MarkdownRenderer 等，展示了"选区可视化"概念
4. **设计文档丰富**：
   - docs/design/ 包含多个概念/方案文档
   - AGENTS.md 有详细的跨会话记忆和最新记忆日志
5. **技术决策已确定**：
   - 语言：C# + .NET 9.0
   - 格式：GitHub Flavored Markdown
   - 架构：即时模式 + Elm Architecture

**更新**: [wiki/DocUI/README.md](../../wiki/DocUI/README.md) - 源码核实全面重写
