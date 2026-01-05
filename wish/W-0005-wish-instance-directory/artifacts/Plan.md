---
docId: "W-0005-Plan"
title: "W-0005 Plan"
produce_by:
  - "wish/W-0005-wish-instance-directory/wish.md"
---

# Plan (W-0005)

## P0：先建试点工作台（Done）

- 已创建 `wish/W-0005-wish-instance-directory/` 并写入 goals/issues/snapshot + tier artifacts。

## P1：迁移策略（仅改 Root Nodes；闭包与双向链接逻辑先不改）

### 核心假设（来自你的 Open Q1 结论）
- DocGraph 的闭包构建（从 Root Nodes 的 `produce` 出发递归追踪）与双向链接验证逻辑先不动。
- 本次重构主要改动：Root Nodes 的来源
  - 旧：`wishes/{active,biding,completed}/**/*.md`
  - 新：`wish/W-*/wish.md`（或等价的 `wish/**/wish.md`，由目录名约束为 W- 前缀）

### 目录边界共识（2026-01-05）

- `wish/` MUST 仅承载 Wish 实例目录（例如 `W-0005-xxx/`）。
- 面板/派生视图统一输出到仓库根下的 `wish-panels/`（不与实例目录混放）。

### 迁移步骤（建议）
1) 为现存每个 Wish 建立实例目录并迁入：W-0001, W-0002, W-0003, W-0005, W-0006（W-0004 已有样例）
2) 统一更新这些 `wish.md` 的 produce 指向（只需要读写 frontmatter）
3) DocGraph 改造 Root Nodes 扫描范围到新结构
4) 用 DocGraph validate 验证闭包与 backlink
5) 删除旧 `wishes/`（最后一步）

### 迁移进展（2026-01-05 试点）

| Wish | 状态 | 新路径 |
|:-----|:-----|:-------|
| W-0001 | ✅ 已迁移 | `wish/W-0001-wish-bootstrap/` |
| W-0002 | ✅ 已迁移 | `wish/W-0002-docgraph/` |
| W-0003 | ✅ 已迁移 | `wish/W-0003-jam-session/` |
| W-0004 | ✅ 已有样例 | `wish/W-0004-sizedptr/` |
| W-0005 | ✅ 已迁移 | `wish/W-0005-wish-instance-directory/` |
| W-0006 | ✅ 已迁移 | `wish/W-0006-rbf-sizedptr/` |

**DocGraph validate 通过**：2026-01-05，12 Wish 文档 + 62 产物文档，76 关系。

### 待清理项

- [ ] 删除旧 `wishes/active/` 下的迁移过的 Wish 文件（需确保旧路径不再被引用）
- [ ] 清理外部产物文档中的旧 `produce_by` 路径（当前保留双指向以保证 validate 通过）

### Stop Conditions
- SC-1：所有 Wish Root Nodes 已迁入 `wish/` 且可被 DocGraph 扫描 ✅
- SC-2：DocGraph `validate` 在并存状态通过 ✅
- SC-3：删除 `wishes/` 后仍可通过 validate（待执行）

## P2：关于“间接 produce_by”与“先不改验证逻辑”的折中

- 过渡建议：迁移期仍让产物 `produce_by` 直接指向 `wish.md`（最省改动、最少惊喜）。
- 若需要间接 produce_by：作为 v0.3 再做（那会触及 DocGraph 的验证语义）。
