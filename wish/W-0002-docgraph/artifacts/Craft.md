---
docId: "W-0002-Craft"
title: "W-0002 Craft"
produce_by:
  - "wish/W-0002-docgraph/wish.md"
---

# Craft (W-0002)

## 代码位置

- 源码：`atelia/src/DocGraph/`
- 测试：`atelia/tests/DocGraph.Tests/`

## 测试覆盖

- 93 测试通过
- 覆盖核心功能：frontmatter 解析、链接验证、双向链接检查、汇总生成

## 使用方法

```bash
cd atelia/src/DocGraph

# 验证文档关系
dotnet run -- validate ../../../

# 有问题？预览修复方案
dotnet run -- fix ../../../ --dry-run

# 确认后执行修复
dotnet run -- fix ../../../ --yes
```

## 文档

- API 设计：[atelia/docs/DocGraph/v0.1/api.md](../../../atelia/docs/DocGraph/v0.1/api.md)
- 使用指南：[atelia/docs/DocGraph/v0.1/USAGE.md](../../../atelia/docs/DocGraph/v0.1/USAGE.md)

