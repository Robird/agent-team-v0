---
docId: "W-0005-issues"
title: "W-0005 Issues"
produce_by:
  - "wish/W-0005-wish-instance-directory/wish.md"
---

# W-0005 Issues

- I-0005-001 (Rule) DocGraph v0.2 是否仍强制产物 `produce_by` 直指 `wish.md`？
  - 当前倾向：本次重构只改 Root Nodes 来源；闭包与双向链接逻辑保持。
  - 风险：规范允许“间接 produce_by”会与现有校验发生冲突。
  - 状态：Open

- I-0005-002 (Plan) 从旧 `wishes/` 迁移到新 `wish/` 的顺序与回滚点
  - 状态：Resolved（2026-01-05 试点迁移完成）

- I-0005-003 (Craft) 迁移后待清理：旧 wishes/active 文件与双重 produce_by
  - 描述：试点迁移完成后，外部产物文档同时保留旧/新两个 produce_by 路径
  - 待清理文件：
    - `wishes/active/wish-0001-wish-system-bootstrap.md`
    - `wishes/active/wish-0002-doc-graph-tool.md`
    - `wishes/active/wish-0003-exp-demand-contribute-jam.md`
    - `wishes/active/wish-0006-rbf-migrate-to-sizedptr.md`
  - 状态：Open（最终迁移时统一清理）
