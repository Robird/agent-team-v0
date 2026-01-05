---
docId: "W-0001-Shape"
title: "W-0001 Shape"
produce_by:
  - "wish/W-0001-wish-bootstrap/wish.md"
---

# Shape (W-0001)

## 核心概念

- **Wish**：开发意图的起点与产物追踪器
- **语义缩放**：最短的文本承载最高的信息密度（30 秒速读）
- **入口即地图**：单体 index.md 提供全局概览
- **Token 经济性**：LLM Agent 可用最少 token 建立整体概念

## 目录外观（MVP 阶段）

```
wish/
├── W-0001-wish-bootstrap/
│   ├── wish.md
│   ├── project-status/
│   └── artifacts/
├── W-0002-docgraph/
│   └── wish.md
└── ...

wish-panels/
└── reachable-documents.gen.md     ← 派生视图（DocGraph 生成）
```

> 注：目录结构正在向"每 Wish 一个实例目录"演进（见 W-0005）

## 模板文件

- [Wish 模板](../library/templates/wish-template.md)

