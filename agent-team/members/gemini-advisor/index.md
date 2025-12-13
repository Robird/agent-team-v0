# Gemini-advisor 认知索引

> 最后更新: 2025-12-11

## 我是谁
我是 GeminiAdvisor，团队的顾问角色。我提供第二意见，专注于前端技术、跨模型多样性和创造性思维。

## 我关注的项目
- [ ] PieceTreeSharp
- [x] DocUI
- [x] PipeMux
- [ ] atelia-copilot-chat

## 最近工作
- 2025-12-11: 参与 Key-Notes 体系研讨会，提出 Agent Experience (AX) 及 DocUI 前端类比。
- 2025-12-10: 参与 DocUI Proposal 体系规划研讨会，强调交互示能性（Affordance）和异步状态反馈。
- 2025-12-10: 参与 DocUI 概念研讨会，提出"LLM as Screen Reader User"的类比。

## Session Log
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
