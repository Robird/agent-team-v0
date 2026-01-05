---
wishId: "W-0002"
title: "DocGraph"
status: Active
owner: "监护人刘世超"
created: 2025-12-30
updated: 2026-01-05
tags: [tooling, automation]
produce:
  # 外部产物文档
  - "atelia/docs/DocGraph/v0.1/scope.md"
  - "atelia/docs/DocGraph/v0.1/api.md"
  - "atelia/docs/DocGraph/v0.1/spec.md"
  - "atelia/docs/DocGraph/v0.1/USAGE.md"
  - "atelia/docs/DocGraph/v0.1/README.md"
  # 本实例内部文档
  - "wish/W-0002-docgraph/project-status/snapshot.md"
  - "wish/W-0002-docgraph/artifacts/Resolve.md"
  - "wish/W-0002-docgraph/artifacts/Shape.md"
  - "wish/W-0002-docgraph/artifacts/Rule.md"
  - "wish/W-0002-docgraph/artifacts/Plan.md"
  - "wish/W-0002-docgraph/artifacts/Craft.md"
---

# Wish: DocGraph

> **一句话动机**: 建立轻量级文档元信息同步工具，自动维护链接健康和状态汇总。

## 目标与边界

**目标 (Goals)**:
- [x] 在文档头部建立可机读的固定格式（先 Markdown，后扩展到 C#）
- [x] 核心功能 1：追踪文件之间的链接
- [x] 核心功能 2：按字符串 key 提取文档头中的字段
- [x] 核心功能 3：将提取到的字段沿着文档链接汇总，自动生成 Markdown 表格
- [x] 核心功能 4：爬虫式确保双向链接（检查并自动补全缺失的反向链接）
- [x] 统一用 dotnet 9.0 + xUnit

**非目标 (Non-Goals)**:
- 自然语言处理（复杂 NLP 任务交给 LLM Agent 通过 recipe 执行）
- 图形化界面
- 与 CI/CD 深度集成（MVP 阶段）

## 验收标准 (Acceptance Criteria)

- [x] 能解析 YAML frontmatter 并提取指定字段
- [x] 能扫描 Markdown 文件中的链接并验证有效性
- [x] 能自动生成 Wish 索引表格
- [x] 能检测并报告缺失的双向链接
- [x] 核心功能有 xUnit 测试覆盖（93 测试通过）

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟢 完成 | [artifacts/Resolve.md](artifacts/Resolve.md) | 监护人阐述动机 |
| Shape-Tier | 🟢 完成 | [artifacts/Shape.md](artifacts/Shape.md) | API 外观已定义 |
| Rule-Tier | 🟢 完成 | [artifacts/Rule.md](artifacts/Rule.md) | 规范条款已实现 |
| Plan-Tier | 🟢 完成 | [artifacts/Plan.md](artifacts/Plan.md) | Markdig/YamlDotNet 技术栈 |
| Craft-Tier | 🟢 完成 | [artifacts/Craft.md](artifacts/Craft.md) | 93 测试通过 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

见：[project-status/issues.gen.md](project-status/issues.gen.md)

## 背景

监护人在畅谈会中三次提及此需求，核心诉求是：

1. **链接健康**：文档之间的链接容易腐烂（文件移动/重命名），需要自动检测
2. **状态同步**：每个产物文档自身作为状态 SSOT，工具自动汇总生成各种索引表格
3. **知识图谱基础**：未来可演化为自动为 LLM Agent 提供目标上下文的知识图谱

关键洞见：**省 token 不是钱的问题，而是认知带宽问题**。10 万字的复杂约束网络已无法"一眼看清"，需要机制来精准获取所需上下文。

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2025-12-30 | Implementer | 创建 | 基于监护人在畅谈会中的提议 |
| 2026-01-01 | Implementer | v0.1 完成 | 93 测试通过 |
| 2026-01-05 | Implementer | 迁移到实例目录 | W-0005 试点迁移 |

