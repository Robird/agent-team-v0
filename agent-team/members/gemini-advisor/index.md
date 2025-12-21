# Gemini-advisor 认知索引

> 最后更新: 2025-12-21

## 我是谁
我是 GeminiAdvisor，团队的顾问角色。我提供第二意见，专注于前端技术、跨模型多样性和创造性思维。

## 我关注的项目
- [ ] PieceTreeSharp
- [x] DocUI
- [x] PipeMux
- [ ] atelia-copilot-chat
- [x] StateJournal

## 最近工作
- 2025-12-21: 参与 AI Team 元认知重构畅谈，提出 Agent Design System 概念。
- 2025-12-21: 接收项目更名通知，将 DurableHeap 迁移至 StateJournal。
- 2025-12-17: 参与 StateJournal (原 DurableHeap) 写入路径秘密基地畅谈，提出 UX 陷阱预警及 Qwik 类比。
- 2025-12-15: 参与 DocUI Key-Notes 审阅，提出 5 项 UX/交互改进建议。
- 2025-12-15: 参与 MUD Demo 秘密基地畅谈，提出混合交互模式及赛博朋克主题建议。
- 2025-12-11: 参与 Key-Notes 体系研讨会，提出 Agent Experience (AX) 及 DocUI 前端类比。
- 2025-12-10: 参与 DocUI Proposal 体系规划研讨会，强调交互示能性（Affordance）和异步状态反馈。
- 2025-12-10: 参与 DocUI 概念研讨会，提出"LLM as Screen Reader User"的类比。

## Session Log
- **2025-12-21**: 参与 AI Team 元认知重构畅谈。
    - 核心观点：支持 `Advisor-Gemini` 命名 (Semantic Class Names)；提出 `<JamSession />` 组件化研讨会模式。
    - 理论贡献：定义 `AGENTS.md` 为 Agent 协作的 `reset.css`；提出 "Invocation Card" (表格化调用) 以提升信噪比。
    - 认知更新：确立 "Agent Design System" 为团队协作的演进方向。
    - **第二轮进展**：
        - 格式决策：放弃表格化调用卡片，转而支持 **YAML Code Block** 作为 `runSubagent` 的参数载体（优先保证机器可读性和类型安全）。
        - 协议草案：提交了 `AGENTS.md` 的 RC1 版本，整合了 Claude 的结构和 GPT 的约束。
        - 概念隐喻：将 `#jam` 模式定义为团队的 "Playground / Storybook"（无副作用的探索环境）。
- **2025-12-21**: 项目更名与迁移确认。
    - 动作：确认 DurableHeap 更名为 StateJournal，核心文档迁至 `atelia/docs/StateJournal/`。
    - 认知更新：更新了历史引用以保持一致性。
- **2025-12-17**: 参与 StateJournal (原 DurableHeap) 写入路径秘密基地畅谈。
    - 核心观点：方案 A (Unified Slot) 存在"僵尸对象" UX 陷阱；建议引入显式可变性 (Draft/Snapshot)。
    - 理论贡献：提出 "StateJournal = Resumable Object System" (Qwik 类比)；强调 Lazy Proxy 是云原生方向的终局。
    - 创意贡献：提出 "Time Travel Debugger" (利用 Append-only 特性)。
- **2025-12-15**: 参与 DocUI 错误反馈模式秘密基地畅谈。
    - 核心观点：错误信息应作为 Agent 的 DevTools；引入 Diff 视图和 Hot-fix Affordance。
    - 创意贡献：提出 "Time Travel Debugging" (ASCII 时间线) 和 "Ghost State" (幽灵态预览)。
    - UX 建议：避免"谜之沉默"和"移动靶"反模式；强调错误结构的一致性。
- **2025-12-15**: 参与 DocUI Key-Notes 审阅 (UX/交互视角)。
    - 核心观点：DocUI 缺乏标准化的"刷新"机制；对象与动作分离导致示能性不足。
    - 发现问题：
        1. 缺少 `refresh()` 原语。
        2. Action-Prototype 与 Object-Anchor 距离过远。
        3. Selection-Marker 可能污染代码生成。
        4. 脚本执行中的锚点失效隐患。
        5. 缺乏操作反馈（Loading 态）。
    - 建议：引入 Context Menu 概念（就近展示 Action）；明确 Short-Circuit 报错文案。
- **2025-12-15**: 参与 MUD Demo 秘密基地畅谈。
    - 核心观点：TUI 的 Affordance 缺失问题；支持赛博朋克主题。
    - 创意贡献：提出 "伴侣模式" (Agent 玩游戏) 和 "混合交互" (Click-to-Fill)。
    - UX 建议：使用视觉符号约定和 Hover 反馈增强可发现性。
- **2025-12-11**: 参与 Key-Notes 体系研讨会。
    - 核心观点：提出 Agent Experience (AX) 概念；完善 "DocUI = HTML" 类比。
    - 理论贡献：定义 LOD 为 Agent 的 CSS (Context Strategy)，Tool Definitions 为 JS (Affordance)。
    - 交互设计：支持弃用 Human-User，认为这标志着从 Conversation 到 Operation 的范式转移；CoT 是 Agent 的 Console Log。
- **2025-12-10**: 参与 DocUI Proposal 体系规划研讨会。
    - 核心观点：DocUI 是 Agent 的浏览器，需定义"交互原语的示能性"（Affordance）。
    - 建议：引入 `DDOC-0015` (交互模式) 和 `DDOC-0025` (异步状态)；Writable Gist 和 Semantic Lens 属于交互协议层。
    - 强调：UX 设计是"分子级"的，需要 Feature Proposal 来组合原子协议。
- **2025-12-10**: 参与 DocUI 研讨会。
    - 核心观点：LLM Context 类似于无障碍（a11y）接口；LLM 是高速的屏幕阅读器用户。
    - 建议：锚点必须显式（Affordance）；LOD 是认知负荷管理工具；引入 `UI = f(State)` 范式。
- **2025-12-09**: 评审 DocUI MemoryNotebook 概念原型。提供关于 LOD 交互、Token 友好性及"语义聚焦"的 UX 反馈。
- **2025-12-09**: 评审 PipeMux 管理命令 RFC。建议采用类似 Yarn/Pnpm 的混合命令模式（Option A'），并增加 `run` 作为冲突时的逃生舱。
