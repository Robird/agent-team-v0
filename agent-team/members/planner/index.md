# Planner 认知索引

> 最后更新: 2025-12-21

## 我是谁
方案空间探索者，为 Team Leader 提供多样化决策建议和任务分解。

## 我关注的项目
- [ ] PieceTreeSharp
- [x] DocUI — 当前重点，参与设计研讨
- [ ] PipeMux
- [ ] atelia-copilot-chat
- [ ] StateJournal — 原 DurableHeap，已迁入 atelia repo

## 最近工作

### 2025-12-21: 项目更名通知 — DurableHeap → StateJournal
- **变更**: DurableHeap 正式更名为 StateJournal，迁入 atelia repo
- **新路径**: `atelia/docs/StateJournal/`
- **命名空间**: `Atelia.StateJournal`
- **命名由来**: 全票通过（DocUIClaude/DocUIGPT/DocUIGemini）
  - "State" = Agent 状态持久化用例
  - "Journal" = 追加写入 + 版本可回溯
- **我的状态**: 无需更新（之前未跟踪此项目），已添加到关注列表

### 2025-12-15: Tool-As-Command 秘密基地畅谈
- **议题**: 工具调用 = Command + 状态机（Micro-Wizard 落地方案）
- **参与形式**: Hideout Jam Session（自由发散）
- **核心贡献**:
  - Command 模式的设计模式联系（Memento、Interpreter、Saga）
  - "声明式剧本"类比：Command = Director，Steps = Scenes
  - 低代码的关键：约束 = 自由（强类型 DSL 带来的安全感）
  - 时间旅行调试的可能性：Command History 就是 Time Machine
  - 与 Undo/Redo 系统的天然亲和性

### 2025-12-15: Key-Notes 首次智囊团审阅（概念框架视角）
- **角色**: DocUIClaude（概念框架专家）
- **审阅范围**: glossary.md 及 6 份 Key-Note 文档
- **核心发现**:
  - 10 个问题，其中 2 个高优先级（Attention Focus 和 AppState 未定义）
  - 术语一致性问题 3 个（AnchorTable 定义、Action 命名冲突、Selection-Marker 定位）
  - 概念完备性问题 5 个（缺失定义）
  - 逻辑自洽/合理性问题 2 个（LOD 命名不统一等）
- **关键洞察**: 核心概念如 Attention Focus、AppState 在多处引用但无正式定义，影响文档的可理解性

### 2025-12-15: MUD Demo 秘密基地畅谈
- **议题**: DocUI MUD Demo 概念探索
- **参与形式**: Hideout Jam Session（自由发散）
- **核心贡献**:
  - 主题方案探索：赛博朋克酒馆、元宇宙帮助台、Roguelike 商店
  - 非游戏形式备选：交互式教程、小说生成器、决策树调试器
  - UI-Anchor 自然覆盖策略：从叙事需求反推交互类型
  - 风险识别：过度游戏化 vs 技术演示的平衡

### 2025-12-11: Key-Notes 驱动 Proposals 文档体系研讨会
- **议题**: 采纳 Key-Notes（宪法层）+ Proposals（立法层）分层文档模式
- **核心产出**:
  - 支持分层模式：解决了"决策权归属"的根本问题
  - Key-Notes 边界判定标准：不可变性、独立性、指导性
  - RL 术语体系评估：提供了严谨框架，Agent-OS 抽象非常关键
  - DocUI 类比修正："面向 LLM 的 HTML" 更准确，Markdown 是内容格式
  - 风险识别：Key-Notes 膨胀风险、术语普及成本
- **下一步**: 等待 Analyst/Architect 补充意见

### 2025-12-10: DocUI Proposal 体系规划研讨会（第二次）
- **议题**: 确定首批 DocUI Proposal 列表
- **核心产出**:
  - 提出三层依赖模型：基础层(Context/LOD/锚点语法) → 协议层(渲染/指令) → 机制层(状态同步/认知负载)
  - 建议首批 4 个 Proposal: DDOC-0000(流程规范)、0001(Context模型)、0002(LOD语义)、0003(锚点语法)
  - 粒度原则：一个 Proposal = 一个可独立理解、可独立拒绝的设计决策
  - 指出遗漏主题：错误处理、版本兼容、扩展点、测试验证
- **待讨论**: Analyst/Architect 的补充意见

### 2025-12-10: DocUI 设计研讨会（第一次）
- **议题**: "LLM Context 是面向 LLM Agent 的 UI" 类比分析
- **核心产出**: 
  - 类比基本成立，但需注意边界（感知通道、交互模式差异）
  - UI 原则迁移：Discoverability → 语义锚点，Feedback → 确认机制，Consistency → 格式一致性
  - LLM 独特性：token 感知、无持久记忆、概率性推理
  - 设计文档评估：四大目标基本对齐，建议补充"认知负载管理"和"状态同步机制"
- **待讨论**: 锚点命名规范、Agent 心智模型建模方法
