---
wishId: "W-0006"
title: "修订 RBF 设计稿：引入/改用 SizedPtr"
status: Active
owner: "AI Team"
created: 2026-01-04
updated: 2026-01-05
tags: [rbf, design, migration]
produce:
  # 外部产物文档
  - "atelia/docs/Rbf/rbf-interface.md"
  - "atelia/docs/Rbf/rbf-format.md"
  - "agent-team/experiment/2026-01-04-sizedptr-wish0004-reviewloop/2026-01-04-wish-0004-sizedptr-simulation.md"
  # 本实例内部文档
  - "wish/W-0006-rbf-sizedptr/project-status/goals.md"
  - "wish/W-0006-rbf-sizedptr/project-status/issues.md"
  - "wish/W-0006-rbf-sizedptr/project-status/snapshot.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Resolve.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Shape.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Rule.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Plan.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Craft.md"
---

# Wish: 修订 RBF 设计稿：引入/改用 SizedPtr

> **一句话动机**: 让 RBF/StateJournal 在需要表达"区间（offset+length）"时使用 SizedPtr，从而减少重复设计与类型漂移，并提升跨层复用。

## 目标与边界

**目标 (Goals)**:
- [ ] 审阅并修订 RBF 相关设计文档，使其能引用 `Atelia.Data.SizedPtr` 表达"4B 对齐的 byte range"
- [ ] 明确区分两类语义：
  - **Address64 / Ptr64**：指向某个 Frame 起始位置的 file offset（RBF 层既有语义）
  - **SizedPtr**：表达一个 span/range（offset+length）的紧凑表示（通用产品）
- [ ] 在 `rbf-interface.md` 与 `rbf-format.md` 中明确哪些字段/接口应继续使用 Address64，哪些应改用 SizedPtr
- [ ] 为后续实际实现迁移提供清晰的 Decision Log

**非目标 (Non-Goals)**:
- 不在本 Wish 中实现 RBF 的代码（当前 active code 里尚无完整 RBF 实现）
- 不在本 Wish 中强制把 Address64 完全删除或替换为 SizedPtr
- 不强制为 SizedPtr 引入 Null/Empty 等语义

## 验收标准 (Acceptance Criteria)

- [ ] `atelia/docs/Rbf/rbf-interface.md` 修订完成
- [ ] `atelia/docs/Rbf/rbf-format.md` 修订完成
- [ ] 新增 Decision Log 记录"为什么/如何引入 SizedPtr"的 tradeoff

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟡 进行中 | [artifacts/Resolve.md](artifacts/Resolve.md) | 来自 Wish-0004 的后续演化方向 |
| Shape-Tier | ⚪ 未开始 | [artifacts/Shape.md](artifacts/Shape.md) | 概念边界与术语对齐 |
| Rule-Tier | ⚪ 未开始 | [artifacts/Rule.md](artifacts/Rule.md) | 条款层的类型/字段约束 |
| Plan-Tier | ⚪ 未开始 | [artifacts/Plan.md](artifacts/Plan.md) | 迁移步骤（文档层）|
| Craft-Tier | ➖ N/A | [artifacts/Craft.md](artifacts/Craft.md) | 本 Wish 不做实现 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

见：[project-status/issues.md](project-status/issues.md)

## 背景

Wish-0004 已在 `Atelia.Data` 中实现 `SizedPtr` 并通过单元测试。在模拟执行过程中也暴露出"文档层术语混用"的风险：
- `Address64/Ptr64` 是 file offset（指向 Frame 起点）
- `SizedPtr` 是 span（offset+length）

RBF 作为目标用户之一，应在其设计稿中复用 SizedPtr，而不是在 RBF 层重复发明另一套 span 表达。

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-04 | AI Team | 创建 | 来自 Wish-0004 的后续工作拆分 |
| 2026-01-05 | Implementer | 迁移到实例目录 | W-0005 试点迁移 |

