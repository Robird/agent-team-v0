---
wishId: "W-0005"
title: "重构 Wish 目录与 DocGraph：每个 Wish 一个实例目录"
status: Active
owner: "AI Team"
created: 2026-01-04
updated: 2026-01-04
tags: [meta, infrastructure, docgraph]
produce:
  - "wishes/specs/wish-instance-directory-spec.md"
  - "atelia/docs/DocGraph/v0.2/wish-instance-layout.md"
  - "atelia/docs/DocGraph/v0.2/migration-plan.md"
---

# Wish: 重构 Wish 目录与 DocGraph：每个 Wish 一个实例目录

> **一句话动机**: 让每个 Wish 自带一套可被 DocGraph 汇总的“状态寄存器 + 分层产物”，为 LLM Agent 的连续推进提供可靠的外部记忆载体。

## 目标与边界

**目标 (Goals)**:
- [ ] 将 Wish 的文件形态从“单文件 + 分目录 active/completed/…”升级为“每个 Wish 一个实例目录”。
- [ ] 新布局在 `./wish/`（注意：与当前 `./wishes/` 并存一段时间，迁移后可统一）。
- [ ] 每个 Wish 实例目录内，提供标准化骨架（模板化、便于冷启动扫描）：
  - [ ] `wish.md`（意图载体；frontmatter 承载 status/links）
  - [ ] `project-status/`（状态寄存器：goals/issues/snapshot）
  - [ ] `artifacts/Resolve|Shape|Rule|Plan|Craft/`（按 Artifact-Tiers 分层的内部产物组）
  - [ ] `meeting/`（工作记忆/讨论记录，可分段滚动）
  - [ ] `experiments/`（PoC/Spike/临时脚本/bench）
- [ ] 将 DocGraph 的汇总与校验能力扩展到“Wish 实例目录”：
  - [ ] 能扫描所有 `wish/**/wish.md` 并生成索引/状态总览
  - [ ] 能校验 `produce` / `produce_by` 双向链接在新布局下仍成立
  - [ ] 能提供 `validate` / `fix` / `generate` 针对新布局的默认入口
- [ ] 提供一份迁移方案：把当前 `wishes/active/wish-000X-*.md` 迁移为 `wish/W-000X-<slug>/wish.md`，并保留可追溯性。

**非目标 (Non-Goals)**:
- 不在本 Wish 中实现完整工作流引擎（CI/CD/自动派工等）。
- 不强制一次性迁移所有旧 Wish（允许渐进迁移）。
- 不解决所有历史文档命名不一致问题（优先解决结构与可聚合性）。

## 验收标准 (Acceptance Criteria)

- [ ] 新目录规范文档完成：`wishes/specs/wish-instance-directory-spec.md`
- [ ] DocGraph v0.2 文档完成（布局说明 + 迁移方案）：`atelia/docs/DocGraph/v0.2/*`
- [ ] DocGraph 能在本仓库对至少 2 个 Wish 实例目录执行：
  - [ ] `validate` 成功（或输出可理解的失败报告）
  - [ ] `generate` 输出 Wish 索引/汇总（可导航）
- [ ] 至少迁移 1 个已完成的 Wish 作为样例（建议 Wish-0004 SizedPtr）。

## 层级进度 (Layer Progress)

> **术语参考**：本文档使用 [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md#artifact-tiers产物层级)（产物层级）框架组织产物层级。具体层级定义见 [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md)。

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟡 进行中 | — | 来自模拟执行反馈：分层文档组缺失导致落笔困难 |
| Shape-Tier | ⚪ 未开始 | — | 目录结构外观、命名约定、模板 |
| Rule-Tier | ⚪ 未开始 | — | frontmatter/链接/聚合规则条款 |
| Plan-Tier | ⚪ 未开始 | — | 迁移策略、工具改造路径 |
| Craft-Tier | ⚪ 未开始 | — | DocGraph 实现与测试 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

| IssueId | 层级 | 状态 | 描述 | 阻塞原因 |
|:--------|:-----|:-----|:-----|:---------|
| — | — | — | — | — |

## 背景

在 Wish-0004 的模拟执行中暴露出关键问题：
- 如果不在一开始就创建“按 Artifact-Tiers 分层的内部文档组”，讨论产物会无处落笔，最终导致层级混写。
- LLM Agent 的上下文会被压缩，真正可靠的是“可被扫描/可被聚合”的文件结构。

因此需要把 Wish 从“单文件意图”升级为“一个自带状态寄存器与分层产物的实例目录”。

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-04 | AI Team | 创建 | 来自 Wish-0004 模拟执行反馈 |
