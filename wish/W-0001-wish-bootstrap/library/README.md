---
docId: "wish-library-readme"
title: "Wish 系统库说明"
produce_by:
  - "wish/W-0001-wish-bootstrap/wish.md"
---

# Wish 系统库

> **Wish** = 意图载体 + 产物索引
>
> 动机驱动的开发工作流，让最短的文本承载最高的信息密度。

## 快速开始

1. **阅读规范**: 阅读 [specs/wish-system-rules.md](specs/wish-system-rules.md) 了解 MUST/SHOULD/MAY 条款
2. **创建 Wish**: 复制 [templates/wish-template.md](templates/wish-template.md) 的模板内容到 `wish/W-XXXX-<slug>/wish.md`
3. **验证链接**: 运行 `cd atelia/src/DocGraph && dotnet run -- validate ../../../`

## 目录结构

```
wish/W-0001-wish-bootstrap/library/
├── README.md             # 本文件
├── specs/                # 规范文档
│   ├── wish-system-rules.md         # MUST/SHOULD/MAY 条款
│   ├── wish-system-spec.md          # 系统规范（占位）
│   └── wish-instance-directory-spec.md  # 实例目录规范
└── templates/            # 文档模板
    ├── wish-template.md
    └── issue-template.md
```

## 核心概念

> **术语参考**：五层级术语定义见 [Artifact-Tiers](../../../agent-team/wiki/artifact-tiers.md)。

| 概念 | 说明 |
|:-----|:-----|
| **Wish** | 监护人的意图，30 秒可读懂，链接到各层级产物 |
| **Wish 实例目录** | `wish/W-XXXX-<slug>/`，包含 wish.md、project-status/、artifacts/ |
| **层级产物** | Resolve → Shape → Rule → Plan → Craft |
| **Issue** | 问题/阻塞记录，必须关联层级和 Wish |

## 状态符号

| 符号 | 含义 |
|:-----|:-----|
| ⚪ | 未开始 |
| 🟡 | 进行中 |
| 🟢 | 完成 |
| 🔴 | 阻塞 |
| ➖ | N/A (不适用) |

## 相关文档

- [Artifact-Tiers 方法论](../../../agent-team/wiki/artifact-tiers.md)
- [DocGraph 使用指南](../../../atelia/docs/DocGraph/v0.1/USAGE.md)
