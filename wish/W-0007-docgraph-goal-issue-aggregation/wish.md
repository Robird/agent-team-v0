---
wishId: "W-0007"
title: "DocGraph Goals/Issues 聚合"
status: Active
owner: "监护人刘世超"
created: 2026-01-05
updated: 2026-01-05
tags: [tooling, docgraph, automation]
produce:
  - "wish/W-0007-docgraph-goal-issue-aggregation/project-status/goals.md"
  - "wish/W-0007-docgraph-goal-issue-aggregation/project-status/issues.md"
  - "wish/W-0007-docgraph-goal-issue-aggregation/project-status/snapshot.md"
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Resolve.md"
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Shape.md"
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Rule.md"
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Plan.md"
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Craft.md"
---

# Wish: DocGraph Goals/Issues 聚合

> **一句话动机**: 让 DocGraph 能从各层级 artifacts 文档中聚合 goals 和 issues，实现"就地维护、自动汇总"。

## 目标与边界

**目标 (Goals)**:
- [ ] 设计 goals/issues 的 frontmatter schema（含语义锚点 ID）
- [ ] 扩展 DocGraph，新增 Wish 级别的 goals/issues 聚合 Visitor
- [ ] 自动生成 `project-status/goals.md` 和 `project-status/issues.md`
- [ ] 支持已解决 issues 的归档机制

**非目标 (Non-Goals)**:
- 跨 Wish 的全局 goals/issues 聚合（MVP 只做 Wish 内聚合）
- 复杂的状态机（MVP 只区分 active/resolved）
- GUI 或 Web 界面

## 验收标准 (Acceptance Criteria)

- [ ] artifacts 文档的 frontmatter 能声明 goals 和 issues
- [ ] 运行 `docgraph generate` 能生成 goals.md 和 issues.md
- [ ] 生成的文件包含所有 active goals/issues 的汇总表格
- [ ] 已解决的 issues 能被归档到单独区域

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | � 完成 | [artifacts/Resolve.md](artifacts/Resolve.md) | 动机、方案选择、技术可行性 |
| Shape-Tier | 🟢 完成 | [artifacts/Shape.md](artifacts/Shape.md) | frontmatter schema、输出格式 |
| Rule-Tier | ⏭️ 跳过 | [artifacts/Rule.md](artifacts/Rule.md) | 无需形式化约束 |
| Plan-Tier | 🟢 完成 | [artifacts/Plan.md](artifacts/Plan.md) | 三阶段实现计划 |
| Craft-Tier | 🟡 进行中 | [artifacts/Craft.md](artifacts/Craft.md) | 待实现 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

见：[project-status/issues.md](project-status/issues.md)

## 背景

当前痛点：
- goals.md 和 issues.md 需要手动维护
- 但目标和问题往往**就地产生**在各层级 artifacts 中
- 如果能"就地记录、自动聚合"，就减少了维护负担和漂移风险

本 Wish 同时作为 **Wish 推进模式的演练场**——验证 snapshot.md 执行寄存器和 mental model 调度循环的实际效果。

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-05 | Team Leader | 创建 | 作为 Wish 推进模式的演练 + DocGraph 功能增强 |
