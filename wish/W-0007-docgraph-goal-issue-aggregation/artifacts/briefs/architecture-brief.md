---
docId: "W-0007-Brief-Architecture"
title: "W-0007 DocGraph 架构调查 Brief"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Resolve.md"
author: "Investigator"
created: "2026-01-05"
---

# W-0007 DocGraph 架构调查 Brief

> **调查者**：Investigator
> **日期**：2026-01-05
> **任务**：分析 DocGraph 现有架构，评估 goals/issues 聚合功能的扩展可行性

---

## 1. 现有架构概述

### 1.1 目录结构

- `atelia/src/DocGraph/`
  - `Program.cs` — CLI 入口
  - `Commands/` — 命令实现
    - `RunCommand.cs` — 默认全流程命令（validate + fix + generate）
    - `ValidateCommand.cs`
    - `FixCommand.cs`
    - `StatsCommand.cs`
  - `Core/` — 核心模型
    - `DocumentGraph.cs` — 文档图容器
    - `DocumentGraphBuilder.cs` — 构建器（扫描、验证、修复）
    - `DocumentNode.cs` — 节点模型
    - `ValidationResult.cs`
    - `Fix/` — 修复动作
  - `Visitors/` — Visitor 实现
    - `IDocumentGraphVisitor.cs` — 接口定义
    - `GlossaryVisitor.cs` — 术语表生成器
    - `IssueAggregator.cs` — ⚠️ 问题聚合器（已存在！）
    - `ReachableDocumentsVisitor.cs` — 可达文档列表
  - `Utils/`
    - `FrontmatterParser.cs` — YAML 解析
    - `PathNormalizer.cs` — 路径规范化

### 1.2 Visitor 模式工作原理

**接口定义**（`IDocumentGraphVisitor.cs`）：

| 成员 | 类型 | 用途 |
|:-----|:-----|:-----|
| `Name` | string | 用于命名 |
| `OutputPath` | string | 输出路径 |
| `RequiredFields` | IReadOnlyList\<string\> | 依赖的 frontmatter 字段 |
| `Generate()` | string | 生成逻辑 |

**执行流程**：
1. `RunCommand.ExecuteAsync()` 调用 `GetVisitors()` 获取所有 Visitor
2. 对每个 Visitor 调用 `Generate(graph)` 生成内容
3. 将内容写入 `visitor.OutputPath`

### 1.3 frontmatter 解析

**支持的核心字段**：

| 字段 | 类型 | 用途 |
|:-----|:-----|:-----|
| `wishId` | string | Wish 实例标识 |
| `docId` | string | 产物文档标识 |
| `title` | string | 文档标题 |
| `status` | string | Wish 状态 |
| `produce` | string[] | 产出文档路径列表 |
| `produce_by` | string[] | 来源文档路径列表 |

---

## 2. 关键发现：IssueAggregator 已存在

**当前支持的 issues 格式**：

```yaml
issues:
  - description: "问题描述"
    status: "open"        # 可选，默认 "open"
    assignee: "负责人"     # 可选
```

**输出位置**：`docs/issues.gen.md`

### 格式对比

| 维度 | 现有 IssueAggregator | W-0007 初始设计 |
|:-----|:---------------------|:----------------|
| **格式** | 对象数组 `[{description, status, assignee}]` | 字符串数组 `["I-ID: 描述"]` |
| **ID 机制** | 无 ID | 支持 `I-KEYWORD` 前缀 |
| **输出路径** | `docs/issues.gen.md` | `project-status/issues.md` |

---

## 3. 扩展点分析

### 3.1 新增 GoalAggregator Visitor

**最小改动方案**：
1. 在 `Visitors/` 下新建 `GoalAggregator.cs`
2. 实现 `IDocumentGraphVisitor` 接口
3. 在 `RunCommand.GetVisitors()` 中注册

### 3.2 扩展 KnownFrontmatterFields

在 `GlossaryVisitor.cs` 底部的 `KnownFrontmatterFields` 类中添加 `Goals` 常量。

### 3.3 双格式支持

修改 `ExtractIssues()` 方法同时支持字符串和对象格式。

---

## 4. 建议

| 建议 | 理由 |
|:-----|:-----|
| 双格式支持 | 渐进式迁移，不破坏现有文档 |
| 先 MVP 后优化 | 架构扩展点清晰，风险低 |

---

## 附录：关键代码锚点

| 概念 | 位置 |
|:-----|:-----|
| Visitor 接口 | `Visitors/IDocumentGraphVisitor.cs` |
| Visitor 注册 | `Commands/RunCommand.cs#L95-L101` |
| IssueAggregator 实现 | `Visitors/IssueAggregator.cs` |
| KnownFrontmatterFields | `Visitors/GlossaryVisitor.cs#L93-L103` |
