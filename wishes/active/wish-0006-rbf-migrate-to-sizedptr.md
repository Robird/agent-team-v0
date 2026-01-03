---
wishId: "W-0006"
title: "修订 RBF 设计稿：引入/改用 SizedPtr（替代 Address64 的部分场景）"
status: Active
owner: "AI Team"
created: 2026-01-04
updated: 2026-01-04
tags: [rbf, design, migration]
produce:
  - "atelia/docs/Rbf/rbf-interface.md"
  - "atelia/docs/Rbf/rbf-format.md"
  - "agent-team/meeting/2026-01-04-wish-0004-sizedptr-simulation.md"
---

# Wish: 修订 RBF 设计稿：引入/改用 SizedPtr（替代 Address64 的部分场景）

> **一句话动机**: 让 RBF/StateJournal 在需要表达“区间（offset+length）”时使用 SizedPtr，从而减少重复设计与类型漂移，并提升跨层复用。

## 目标与边界

**目标 (Goals)**:
- [ ] 审阅并修订 RBF 相关设计文档，使其能引用 `Atelia.Data.SizedPtr` 表达“4B 对齐的 byte range”。
- [ ] 明确区分两类语义：
  - [ ] **Address64 / Ptr64**：指向某个 Frame 起始位置的 file offset（RBF 层既有语义）
  - [ ] **SizedPtr**：表达一个 span/range（offset+length）的紧凑表示（通用产品）
- [ ] 在 `rbf-interface.md` 与 `rbf-format.md` 中明确哪些字段/接口应继续使用 Address64，哪些应改用 SizedPtr，避免术语混用。
- [ ] 为后续实际实现迁移提供清晰的 Decision Log（不要求本 Wish 完成实现迁移）。

**非目标 (Non-Goals)**:
- 不在本 Wish 中实现 RBF 的代码（当前 active code 里尚无完整 RBF 实现）。
- 不在本 Wish 中强制把 Address64 完全删除或替换为 SizedPtr（是否替换属于进一步范围）。
- 不强制为 SizedPtr 引入 Null/Empty 等语义（由 RBF 层自行约定）。

## 验收标准 (Acceptance Criteria)

- [ ] `atelia/docs/Rbf/rbf-interface.md` 修订完成：
  - [ ] 术语表中新增 SizedPtr 概念引用与其层边界
  - [ ] 明确哪些 API/字段用 Address64，哪些用 SizedPtr
- [ ] `atelia/docs/Rbf/rbf-format.md` 修订完成：
  - [ ] 若 wire format 需要携带 span 信息，明确编码方式与对齐约束
  - [ ] 保持现有 Ptr64(Address64) 的条款稳定，不被 SizedPtr 混淆
- [ ] 新增一份 Decision Log（可在文档内或独立文件）记录“为什么/如何引入 SizedPtr”的 tradeoff。

## 层级进度 (Layer Progress)

> **术语参考**：本文档使用 [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md#artifact-tiers产物层级)（产物层级）框架组织产物层级。具体层级定义见 [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md)。

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟡 进行中 | — | 来自 Wish-0004 的后续演化方向 |
| Shape-Tier | ⚪ 未开始 | — | 概念边界与术语对齐 |
| Rule-Tier | ⚪ 未开始 | — | 条款层的类型/字段约束 |
| Plan-Tier | ⚪ 未开始 | — | 迁移步骤（文档层），实现迁移另开 Wish |
| Craft-Tier | ➖ N/A | — | 本 Wish 不做实现 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

| IssueId | 层级 | 状态 | 描述 | 阻塞原因 |
|:--------|:-----|:-----|:-----|:---------|
| — | — | — | — | — |

## 背景

Wish-0004 已在 `Atelia.Data` 中实现 `SizedPtr` 并通过单元测试。
在模拟执行过程中也暴露出“文档层术语混用”的风险：
- `Address64/Ptr64` 是 file offset（指向 Frame 起点）
- `SizedPtr` 是 span（offset+length）

RBF 作为目标用户之一，应在其设计稿中复用 SizedPtr，而不是在 RBF 层重复发明另一套 span 表达。

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-04 | AI Team | 创建 | 来自 Wish-0004 的后续工作拆分 |
