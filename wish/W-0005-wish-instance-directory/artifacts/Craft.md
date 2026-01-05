---
docId: "W-0005-Craft"
title: "W-0005 Craft"
produce_by:
  - "wish/W-0005-wish-instance-directory/wish.md"
---

# Craft (W-0005)

## 代码改造范围（目标：只改 Root Nodes 来源）

- DocGraph Root Nodes 扫描：从 `wish/**/wish.md` 建立 Root Nodes
- 其余：produce 闭包构建、双向链接验证、fix、visitors 保持不变（先保持）

## 提案：新增 Visitor 输出“全可达文档目录”到 `wish-panels/`

- 新增 visitor：`ReachableDocumentsVisitor`
  - 输入：DocumentGraph 的 `AllNodes`
  - 输出：`wish-panels/reachable-documents.gen.md`
  - 内容：按路径字典序列出所有可达文档（即可视化迁移闭包，帮助人工迁移）

该 Visitor 等价于你说的“顺藤摸瓜”的机器化版本：从 Root Nodes 出发得到闭包，再把闭包列表落盘。
