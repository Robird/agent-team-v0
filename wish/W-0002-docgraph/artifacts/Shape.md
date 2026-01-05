---
docId: "W-0002-Shape"
title: "W-0002 Shape"
produce_by:
  - "wish/W-0002-docgraph/wish.md"
---

# Shape (W-0002)

## 核心概念

- **文档图**：frontmatter 关系构成的有向图，不是文件系统树
- **Root-Nodes**：Wish 文档，是文档图的入口点
- **produce 关系**：Wish 文档到产物文档的单向链接
- **produce_by 关系**：产物文档到 Wish 文档的反向链接

## 用户价值

- **对文档撰写者**：只需在 frontmatter 中维护 `produce` 关系，工具自动验证完整性
- **对项目管理者**：自动获得文档关系图，发现断裂的链接
- **对团队协作**：分散工作，集中验证，确保文档关系一致性

## 功能边界文档

- [atelia/docs/DocGraph/v0.1/scope.md](../../../atelia/docs/DocGraph/v0.1/scope.md)

