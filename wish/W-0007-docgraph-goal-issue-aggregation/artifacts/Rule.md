---
docId: "W-0007-Rule"
title: "W-0007 Rule-Tier"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/wish.md"
---

# W-0007 Rule-Tier: 规范条款

> **状态**：⏭️ 跳过

## 跳过原因

本 Wish 的约束已在 Shape-Tier 中充分定义：
- **ID 格式**：`{Tier前缀}-{关键词}`，正则 `^[A-Z]-[A-Z0-9-]+$`
- **唯一性**：Wish 内 active 条目不重复
- **双格式兼容**：字符串和对象格式都支持

无需额外的形式化规范或 Schema 文件，Shape-Tier 定义已足够清晰。
