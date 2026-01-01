---
wishId: "W-0002"
title: "DocGraph"
status: Active
owner: "监护人刘世超"
created: 2025-12-30
updated: 2025-12-30
tags: [tooling, automation]
produce:
  - "atelia/docs/DocGraph/v0.1/scope.md"
  - "atelia/docs/DocGraph/v0.1/api.md"
  - "atelia/docs/DocGraph/v0.1/spec.md"
  - "atelia/docs/DocGraph/v0.1/USAGE.md"
  - "atelia/docs/DocGraph/v0.1/README.md"
---

# Wish: DocGraph

> **一句话动机**: 建立轻量级文档元信息同步工具，自动维护链接健康和状态汇总。

## 目标与边界

**目标 (Goals)**:
- [ ] 在文档头部建立可机读的固定格式（先 Markdown，后扩展到 C#）
- [ ] 核心功能 1：追踪文件之间的链接
- [ ] 核心功能 2：按字符串 key 提取文档头中的字段
- [ ] 核心功能 3：将提取到的字段沿着文档链接汇总，自动生成 Markdown 表格
- [ ] 核心功能 4：爬虫式确保双向链接（检查并自动补全缺失的反向链接）
- [ ] 统一用 dotnet 9.0 + xUnit

**非目标 (Non-Goals)**:
- 自然语言处理（复杂 NLP 任务交给 LLM Agent 通过 recipe 执行）
- 图形化界面
- 与 CI/CD 深度集成（MVP 阶段）

## 验收标准 (Acceptance Criteria)

- [ ] 能解析 YAML frontmatter 并提取指定字段
- [ ] 能扫描 Markdown 文件中的链接并验证有效性
- [ ] 能自动生成 Wish 索引表格
- [ ] 能检测并报告缺失的双向链接
- [ ] 核心功能有 xUnit 测试覆盖

## 层级进度 (Layer Progress)

> **术语参考**：五层级术语定义见 [Atelia 术语表](../../agent-team/wiki/terminology.md)。

| 层级 | 状态 | 产物链接 | 备注 |
|:-----|:-----|:---------|:-----|
| Why-Layer | 🟢 完成 | [畅谈会记录](../../agent-team/meeting/Meta/2025-12-30-layers-of-dev.md) | 监护人阐述动机 |
| Shape-Layer | 🟡 进行中 | [API 设计草案](../../atelia/docs/DocGraph/api.md) | 初版 API 外观已定义 |
| Rule-Layer | ⚪ 未开始 | — | 待定义规范条款 |
| Plan-Layer | ⚪ 未开始 | — | 待选择技术方案（Markdig/YamlDotNet） |
| Craft-Layer | ⚪ 未开始 | — | 待实现 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

| IssueId | 层级 | 状态 | 描述 | 阻塞原因 |
|:--------|:-----|:-----|:-----|:---------|
| — | — | — | 暂无阻塞 Issue | — |

## 背景

监护人在畅谈会中三次提及此需求，核心诉求是：

1. **链接健康**：文档之间的链接容易腐烂（文件移动/重命名），需要自动检测
2. **状态同步**：每个产物文档自身作为状态 SSOT，工具自动汇总生成各种索引表格
3. **知识图谱基础**：未来可演化为自动为 LLM Agent 提供目标上下文的知识图谱

关键洞见：**省 token 不是钱的问题，而是认知带宽问题**。10 万字的复杂约束网络已无法"一眼看清"，需要机制来精准获取所需上下文。

## 技术选型参考

| 库 | 用途 | 备注 |
|:---|:-----|:-----|
| Markdig | Markdown 解析 | 成熟的 .NET Markdown 解析器 |
| YamlDotNet | YAML frontmatter 解析 | 标准 YAML 库 |
| System.IO.Abstractions | 文件系统抽象 | 便于测试 |

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2025-12-30 | Implementer | 创建 | 基于监护人在畅谈会中的提议 |
