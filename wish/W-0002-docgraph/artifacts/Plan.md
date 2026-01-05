---
docId: "W-0002-Plan"
title: "W-0002 Plan"
produce_by:
  - "wish/W-0002-docgraph/wish.md"
---

# Plan (W-0002)

## 技术选型

| 库 | 用途 | 备注 |
|:---|:-----|:-----|
| Markdig | Markdown 解析 | 成熟的 .NET Markdown 解析器 |
| YamlDotNet | YAML frontmatter 解析 | 标准 YAML 库 |
| System.IO.Abstractions | 文件系统抽象 | 便于测试 |

## 迁移说明

2026-01-05：从 `wishes/active/wish-0002-doc-graph-tool.md` 迁入新实例目录。

## 待清理项

- [ ] 删除旧文件 `wishes/active/wish-0002-doc-graph-tool.md`（待整体迁移完成后统一清理）
- [ ] 更新外部产物文档的 `produce_by` 指向新路径（已追加）

