---
docId: "W-0007-snapshot"
title: "W-0007 Snapshot"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/wish.md"

# === 执行寄存器 ===
snapshotVersion: "0.1"
updated: "2026-01-05"

focus:
  kind: "Milestone"
  id: "COMPLETED"
  tier: "Craft"

next:
  action: "None"
  deliverable: "None"
  definitionOfDone: "W-0007 已完成"
  stopCondition: "N/A"

assignee: "None"

blockers: []
needsGuardian: false
---

# W-0007 Snapshot

## 🎉 Wish Completed!

**完成时间**：2026-01-05

## 实现摘要

### Phase 1: 接口扩展
- `IDocumentGraphVisitor.GenerateMultiple()` 默认方法
- `OutputPreflight` 路径安全校验

### Phase 2: Issue 扩展
- 双格式解析（字符串 + 对象）
- 两层输出（全局 + Wish 级别）
- `resolved_issues` 归档支持

### Phase 3: Goals 聚合
- 新建 `GoalAggregator`
- 两层输出 + `resolved_goals` 支持
- Markdown 表格安全转义

## 生成的文件

| 路径 | 类型 |
|:-----|:-----|
| `docs/goals.gen.md` | 全局 Goals |
| `docs/issues.gen.md` | 全局 Issues |
| `wish/W-XXXX/project-status/goals.md` | Wish 级别 Goals |
| `wish/W-XXXX/project-status/issues.md` | Wish 级别 Issues |

## Pointers

- Resolve: [../artifacts/Resolve.md](../artifacts/Resolve.md)
- Shape: [../artifacts/Shape.md](../artifacts/Shape.md)
- Plan: [../artifacts/Plan.md](../artifacts/Plan.md)
- Craft: [../artifacts/Craft.md](../artifacts/Craft.md)
