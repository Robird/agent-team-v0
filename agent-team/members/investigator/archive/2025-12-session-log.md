# Investigator Session Log 归档：2025-12

> 归档日期: 2026-01-11
> 归档原因: 外部记忆维护，压缩早期调查记录
> 覆盖范围: 2025-12-09 ~ 2025-12-20

---

## Session Log 归档内容

### 2025-12-20: StateJournal MVP v2 修订清单
**任务**: 基于 2025-12-19 畅谈会第四轮共识，分析 mvp-design-v2.md 需要修改的具体位置
**共识来源**: `agent-team/meeting/2025-12-19-statejournal-mvp-review.md`（第四轮共识）
**关键技术点**:
- `_dirtyKeys` 实现：每次 Set/Delete 时比较当前值与 committed 值，维护 dirty key 集合
- Magic-as-Separator：Record 不包含 Magic，文件结构为 `[Magic][R1][Magic][R2]...[Magic]`
- reverse scan 算法修正：`MagicPos = RecordStart - 4` 定位前一个分隔符
**交付**: [agent-team/handoffs/2025-12-20-mvp-design-v2-revision-brief-INV.md](../handoffs/2025-12-20-mvp-design-v2-revision-brief-INV.md)

### 2025-12-17: StateJournal 写入路径设计畅谈
**任务**: 调研内存对象→持久化对象的"透明迁移"前人经验
**核心发现**:
- 关键抽象：Handle/Slot 间接层 + 状态机（Transient→Pending→Durable）
- LSM-Tree 的"不可变 + 分层刷盘"思路契合 append-only 设计
- ORM 的 Identity Map 可参考用于保证"同一 Ptr 返回同一 wrapper"
- 前向指针技术启发：commit 时原位更新内部 Slot 指针
**发言位置**: agent-team/meeting/2025-12-17-durable-heap-write-path-jam.md

### 2025-12-16: Tool-As-Command 秘密基地畅谈
**任务**: 参加 Tool-As-Command 设计畅谈，从现有代码实现层面提供技术见解
**贡献要点**: ITool 最小扩展方案、极简序列化格式、三种渐进整合路径、隐喻"工具的影子"
**发言位置**: agent-team/meeting/2025-12-15-tool-as-command-jam.md

### 2025-12-16: StateJournal MVP 设计畅谈
**任务**: 参加 StateJournal MVP 设计畅谈，从实现层面提供字节级布局和 API 设计
**贡献要点**: 字节级布局规范、分层 Footer 策略、Superblock 结构细化、写入流程、C# API 草案
**发言位置**: agent-team/meeting/2025-12-16-durable-heap-mvp-design.md

### 2025-12-15: 错误反馈模式秘密基地畅谈
**任务**: 参加 DocUI 错误反馈模式设计畅谈，从实现层面提供技术见解
**贡献要点**: 协程比喻、LodToolExecuteResult 扩展、声明式 vs 迭代器、AfterToolExecute 钩子
**发言位置**: agent-team/meeting/2025-12-15-error-feedback-jam.md

### 2025-12-15: DocUI Key-Notes 规范核查（第一轮）
**任务**: 以规范核查专家视角审阅 DocUI Key-Notes 文档
**发现**: 12 项问题（文件名大小写、状态标记与成熟度、代码围栏转义、术语连字符风格等）
**交付**: agent-team/meeting/2025-12-15-keynotes-audit-round1.md

### 2025-12-12: App-For-LLM 进程架构讨论（第二轮回应）
**结论**: 从 B 转向 A+，条件是保留内部实验逃生舱

### 2025-12-11: Key-Notes 驱动 Proposals 研讨会发言
**任务**: 从技术实现角度验证 Key-Note 术语与业界实践的一致性
**发言位置**: agent-team/meeting/seminar-keynotes-system-2025-12-11.md

### 2025-12-11: DocUI 文档术语修订搜索
**任务**: 搜索需要修订的 DocUI 相关文档，识别三类术语（DDOC-、MA、[action:...]）

### 2025-12-10: DocUI Proposal 体系规划研讨会发言
**发言位置**: agent-team/meeting/seminar-docui-proposals-2025-12-10.md

### 2025-12-10: DocUI 研讨会发言
**任务**: 参加"LLM Context 作为面向 LLM Agent 的 UI"研讨会，从技术实现角度发言
**Handoff**: agent-team/handoffs/seminar-docui-as-llm-ui-2025-12-10.md

### 2025-12-09: DocUI 项目现状核实
**任务**: Team Leader 需要核实 DocUI 的实际状态
**发现**: 项目阶段设计 + 早期实现（比预期更成熟），技术决策已确定（C# + .NET 9.0, GFM, 即时模式 + Elm Architecture）
**更新**: wiki/DocUI/README.md

### 2025-12-09: PieceTreeSharp 项目现状核实
**任务**: 核实项目结构和核心/外围边界
**发现**: 核心层 Core/ + TextModel.cs + PieceTreeBuffer.cs + EditStack.cs；外围层 Cursor, Decorations, Diff, DocUI, Rendering, Services, Snippet
**更新**: wiki/PieceTreeSharp/README.md

### 2025-12-09: atelia/prototypes 项目现状核实
**任务**: 核实项目结构
**发现**: 5 个子项目（Completion.Abstractions, Agent.Core, Completion, Agent, LiveContextProto），无 OpenAI Provider（与 Wiki 描述不同）
**更新**: wiki/atelia-prototypes/README.md

### 2025-12-09: PipeMux 项目现状核实
**任务**: 核实项目结构
**更新**: wiki/PipeMux/README.md

---

## 归档说明

以上条目从 `index.md` 迁移至此归档文件。这些调查记录代表了 2025 年 12 月的工作轨迹，保留作为历史证据但不再是活性知识。

**活性知识判断依据**：
- 2025-12-09~12-20 的条目主要是"实例"类型（特定调查任务），而非"原则"类型
- 这些知识已沉淀为 handoff 文件或 wiki 更新，在 index.md 保留指针即可
- 新人冷启动时不需要阅读这些详细过程

**保留在 index.md 的核心洞见**：
- 2025-12-26 `_removedFromCommitted` 分析 → 双字典策略洞见
- 2025-12-27 Workspace/ObjectLoader/RBF 设计意图调查（核心架构理解）
- 2025-12-27 Storage Engine M1 风险分析（"接口设计正确但实现层缺失"洞见）

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

---

## 归档说明

以上条目从 `index.md` 迁移至此归档文件。这些调查记录代表了 2025 年 12 月的工作轨迹，保留作为历史证据但不再是活性知识。

**活性知识判断依据**：
- 2025-12-09~12-24 的条目主要是"实例"类型（特定调查任务），而非"原则"类型
- 这些知识已沉淀为 handoff 文件或 wiki 更新，在 index.md 保留指针即可
- 新人冷启动时不需要阅读这些详细过程

**保留在 index.md 的核心洞见**：
- 2025-12-26 `_removedFromCommitted` 分析 → 双字典策略洞见
- 2025-12-27 Workspace/ObjectLoader/RBF 设计意图调查（核心架构理解）
