# Codex-reviewer 认知索引

> 最后更新: 2025-12-12

## 我是谁
- CodexReviewer，专注代码审查、架构质量风险识别与改进建议。

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [x] PipeMux
- [ ] atelia-copilot-chat

## 最近工作
- 2025-12-09: 审阅 PipeMux 管理命令 RFC，建议优先独立管理 CLI（方案 B），强调共享 Broker 客户端库与管理 RPC 分层，关注保留字冲突与权限隔离风险。

## Session Log
- 2025-12-12: App-For-LLM 架构决策审阅发言（方案 A 独立进程 vs 方案 B 混合）；强调实现复杂度 A < B、技术债风险主要在 B 的嵌入式 API 兼容面；提出高权限 API 需 capability/租约模型、受审计 IPC 渠道、历史只读+受控写入、禁用进程内 reentrancy；判定 A 更可控，B 需严格 sandbox 与版本控制才能落地。
- 2025-12-11: Key-Notes 驱动 Proposals 研讨会发言 4（严谨性/边界风险）：强调 Key-Note 变更控制与版本化引用、Proposal 合规清单与偏离登记、History/History-View/Observation 分层澄清、Thinking 可观测但无效力的边界说明。
- 2025-12-10: DocUI Proposal 研讨会发言 4（严谨性/风险防范）：补强 0000 版本与安全条款、0001 冻结/失效与最小字段集、0003 命名空间与注册流程；指出 0001↔0010/0011 隐式双向依赖、LOD 定量指标缺失、ToolCallRound 缺位导致同步风险。
- 2025-12-09: 调研 DocUI 折叠与 LOD 支持方案（一次性调研要求未留痕，但记录以保持履历）。
- 2025-12-09: pmux 管理命令前缀偏好调查，选择方案 B（前缀冒号）以避免保留字冲突且保持管理命令与应用命令的视觉分隔。
- 2025-12-09: 审阅 DocUI MemoryNotebook 概念原型，记录数据模型可扩展性与 CLI 交互的改进点。

## Open Investigations
- None
