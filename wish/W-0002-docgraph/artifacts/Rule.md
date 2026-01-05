---
docId: "W-0002-Rule"
title: "W-0002 Rule"
produce_by:
  - "wish/W-0002-docgraph/wish.md"
---

# Rule (W-0002)

## 规范文档 SSOT

- [atelia/docs/DocGraph/v0.1/spec.md](../../../atelia/docs/DocGraph/v0.1/spec.md)

## 条款编号规范

条款使用 `[类别-领域-编号]` 格式：
- **类别**：`S`(Structural), `A`(Algorithmic), `F`(Functional), `R`(Resource)
- **领域**：`DOCGRAPH`(核心), `FRONTMATTER`, `PATH`, `ERROR`
- **编号**：三位数字，按领域分组

## 关键设计决策

- 循环引用不检测不报告：核心目标是"收集信息"，环不影响统计
- 单文档问题是 Warning 不是 Error：不阻断对其他文档的收集

