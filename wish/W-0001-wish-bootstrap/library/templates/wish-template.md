---
docId: "wish-template"
title: "Wish 文档模板"
produce_by:
  - "wish/W-0001-wish-bootstrap/wish.md"
---

# Wish 文档模板

> **使用方法**：复制下面的模板内容到 `wish/W-XXXX-<slug>/wish.md`，并填写相应字段。

## 模板内容

```yaml
---
wishId: "W-XXXX"
title: "[简短标题，≤80字]"
status: Active  # Active | Biding | Completed | Abandoned
owner: "[责任人/角色]"
created: YYYY-MM-DD
updated: YYYY-MM-DD
tags: []
produce:
  - "wish/W-XXXX-<slug>/project-status/goals.md"
  - "wish/W-XXXX-<slug>/project-status/issues.md"
  - "wish/W-XXXX-<slug>/project-status/snapshot.md"
  - "wish/W-XXXX-<slug>/artifacts/Resolve.md"
  - "wish/W-XXXX-<slug>/artifacts/Shape.md"
  - "wish/W-XXXX-<slug>/artifacts/Rule.md"
  - "wish/W-XXXX-<slug>/artifacts/Plan.md"
  - "wish/W-XXXX-<slug>/artifacts/Craft.md"
---

# Wish: [标题]

> **一句话动机**: [监护人的原始意图，不超过 50 字]

## 目标与边界

**目标 (Goals)**:
- [ ] [可验收的目标 1]
- [ ] [可验收的目标 2]

**非目标 (Non-Goals)**:
- [明确排除的范围]

## 验收标准 (Acceptance Criteria)

- [ ] [可判定的验收条件 1]
- [ ] [可判定的验收条件 2]

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | ⚪ 未开始 | [artifacts/Resolve.md](artifacts/Resolve.md) | |
| Shape-Tier | ⚪ 未开始 | [artifacts/Shape.md](artifacts/Shape.md) | |
| Rule-Tier | ⚪ 未开始 | [artifacts/Rule.md](artifacts/Rule.md) | |
| Plan-Tier | ⚪ 未开始 | [artifacts/Plan.md](artifacts/Plan.md) | |
| Craft-Tier | ⚪ 未开始 | [artifacts/Craft.md](artifacts/Craft.md) | |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

见：[project-status/issues.md](project-status/issues.md)

## 背景 (可选)

[2-3 段背景说明，供新成员快速了解]

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| YYYY-MM-DD | [name] | 创建 | 初始创建 |
```

## 术语参考

- **Artifact-Tiers**：见 [artifact-tiers.md](../../../../agent-team/wiki/artifact-tiers.md)
