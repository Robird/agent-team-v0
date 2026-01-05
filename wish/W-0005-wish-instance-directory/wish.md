---
wishId: "W-0005"
title: "重构 Wish 目录与 DocGraph：每个 Wish 一个实例目录"
status: Active
owner: "AI Team"
created: 2026-01-04
updated: 2026-01-05
tags: [meta, infrastructure, docgraph]
produce:
  # v0.2 规范（迁移到 W-0001 的 library）
  - "wish/W-0001-wish-bootstrap/library/specs/wish-instance-directory-spec.md"
  - "atelia/docs/DocGraph/v0.2/wish-instance-layout.md"
  - "atelia/docs/DocGraph/v0.2/migration-plan.md"

  # 本 Wish 实例的状态寄存器
  - "wish/W-0005-wish-instance-directory/project-status/goals.md"
  - "wish/W-0005-wish-instance-directory/project-status/issues.md"
  - "wish/W-0005-wish-instance-directory/project-status/snapshot.md"

  # 本 Wish 实例的分层产物（每层一个文档）
  - "wish/W-0005-wish-instance-directory/artifacts/Resolve.md"
  - "wish/W-0005-wish-instance-directory/artifacts/Shape.md"
  - "wish/W-0005-wish-instance-directory/artifacts/Rule.md"
  - "wish/W-0005-wish-instance-directory/artifacts/Plan.md"
  - "wish/W-0005-wish-instance-directory/artifacts/Craft.md"
---

# Wish: 重构 Wish 目录与 DocGraph：每个 Wish 一个实例目录

> **一句话动机**: 让每个 Wish 自带一套可被 DocGraph 汇总的“状态寄存器 + 分层产物”，为 LLM Agent 的连续推进提供可靠的外部记忆载体。

## 目标与边界

**目标 (Goals)**:
- [x] 将 Wish 的文件形态从“单文件 + 分目录 active/completed/…”升级为“每个 Wish 一个实例目录”。
  - 现状：W-0001~W-0006 均已迁入 `wish/W-XXXX-<slug>/wish.md`。
- [x] 新布局在 `./wish/`，完成迁移后删除旧 `wishes/`。
  - 现状：旧 `wishes/` 已删除（仓库根下不再存在该目录）。
- [x] 每个 Wish 实例目录内，提供标准化骨架：
  - [x] `wish.md`
  - [x] `project-status/`（goals/issues/snapshot）
  - [x] `artifacts/{Resolve,Shape,Rule,Plan,Craft}.md`
  - [ ] `meeting/`（工作记忆，按需；本次试点未创建）
  - [ ] `experiments/`（PoC/Spike，按需；本次试点未创建）
- [x] DocGraph 扫描入口迁移：Root Nodes = `wish/**/wish.md`
  - 现状：DocGraph 默认仅扫描 `wish/` 且严格只把 `wish/<instance>/wish.md` 识别为 Root。

**非目标 (Non-Goals)**:
- 不实现完整工作流引擎（CI/CD/自动派工）。
- 不强制一次性迁移所有历史文档命名不一致问题（优先结构与可聚合性）。

## 验收标准 (Acceptance Criteria)

- [x] `wish/W-0001-wish-bootstrap/library/specs/wish-instance-directory-spec.md` 完成（后续迁入 W-0001）。
- [x] `atelia/docs/DocGraph/v0.2/*` 完成（布局说明 + 迁移方案）。
- [x] 在“新世界（仅 wish/）”下：DocGraph `validate` 通过。
  - 证据：`docgraph` 全流程运行验证通过（6 个 Wish Roots / 72 文件闭包 / 0 issues）。
- [x] DocGraph 生成 `wish-panels/` 下的汇总产物（可导航）。
  - 证据：`wish-panels/reachable-documents.gen.md` 已生成并列出闭包。
- [x] 至少迁移 1 个已完成 Wish 作为样例（W-0004 已有样例）。
  - 证据：`wish/W-0004-sizedptr/wish.md` 为 `status: Completed` 且标注 sample/migrated。

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟡 进行中 | `artifacts/Resolve.md` | |
| Shape-Tier | 🟡 进行中 | `artifacts/Shape.md` | 目录外观与命名 |
| Rule-Tier | 🟡 进行中 | `artifacts/Rule.md` | 规则/约束条款 |
| Plan-Tier | 🟡 进行中 | `artifacts/Plan.md` | 迁移步骤与 stop conditions |
| Craft-Tier | 🟡 进行中 | `artifacts/Craft.md` | DocGraph 改造与工具产物 |

## 关联 Issue

见：`project-status/issues.md`

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-04 | AI Team | 创建（旧版 wishes/active） | 来自 Wish-0004 模拟执行反馈 |
| 2026-01-05 | AI Team | 迁移为实例目录 | W-0005 作为试点/试验田 |
