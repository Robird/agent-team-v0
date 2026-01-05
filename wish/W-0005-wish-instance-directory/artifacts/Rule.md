---
docId: "W-0005-Rule"
title: "W-0005 Rule"
produce_by:
  - "wish/W-0005-wish-instance-directory/wish.md"
---

# Rule (W-0005)

- 目录规范 SSOT（v0.2 草案）：`wish/W-0001-wish-bootstrap/library/specs/wish-instance-directory-spec.md`

## 当前待裁决规则（会影响 DocGraph 行为）

- 产物 `produce_by` 是否允许“间接 produce”（不直指 `wish.md`）？
  - 你已明确：允许。
  - 但你也倾向：本次重构 DocGraph 只改 Root Nodes 来源，其余逻辑保持。
  - 需要在 `artifacts/Plan.md` 明确迁移阶段的折中策略（例如：过渡期仍直指 wish.md，或先不校验该条）。
