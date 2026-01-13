---
wishId: "W-0010"
title: "实现 Atelia.DesignDsl 基础解析器"
status: Active
owner: "TeamLeader"
created: 2026-01-12
updated: 2026-01-12
tags: ["design-dsl", "atelia", "infrastructure"]
produce:
  - "wish/W-0010-design-dsl-impl/project-status/goals.md"
  - "wish/W-0010-design-dsl-impl/project-status/issues.md"
  - "wish/W-0010-design-dsl-impl/project-status/snapshot.md"
  - "wish/W-0010-design-dsl-impl/artifacts/stage-01/Shape.md"
  - "wish/W-0010-design-dsl-impl/artifacts/stage-02/Shape.md"
---

# Wish: 实现 Atelia.DesignDsl 基础解析器

> **一句话动机**: 让 AI-Design-DSL 草稿变成可运行的解析器，支持 ATX-Tree 构建和术语/条款提取。

## 目标与边界

**目标 (Goals)**:
- [x] 命名确定：`Atelia.DesignDsl`（程序集 + 命名空间）
- [ ] 实现 ATX-Node 构建（从 Markdig AST）
- [ ] 实现 ATX-Tree 构建（嵌套关系）
- [ ] 提取术语定义（Term-Node）
- [ ] 提取条款定义（Clause-Node）
- [ ] 集成到 DocGraph（调用 DesignDsl API）

**非目标 (Non-Goals)**:
- 引用解析（`@Term-ID`、`@Clause-ID`）— 留待后续 Wish
- 依赖图构建 — 留待后续 Wish
- Import-Node 实现 — 留待后续 Wish

## 验收标准 (Acceptance Criteria)

- [ ] `Atelia.DesignDsl.dll` 编译成功（dotnet 9.0）
- [ ] 单元测试覆盖核心场景（ATX-Node 构建、Tree 构建、Term/Clause 提取）
- [ ] DocGraph 能调用 DesignDsl API 解析示例文档
- [ ] 示例文档：`agent-team/wiki/SoftwareDesignModeling/AI-Design-DSL.md` 能成功解析

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | ➖ N/A | — | 目标清晰，无需单独 Resolve 产物 |
| Shape-Tier | 🟢 完成 | [artifacts/Shape.md](artifacts/Shape.md) | **实验：任务简报模式** ✅ 7 任务定义 |
| Rule-Tier | ➖ N/A | — | Shape.md 中验收标准已足够规范 |
| Plan-Tier | ➖ N/A | — | Shape.md 中依赖图已是实施计划 |
| Craft-Tier | 🟡 进行中 | [artifacts/Craft.md](artifacts/Craft.md) | 当前：Task-S-001 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

见：[project-status/issues.md](project-status/issues.md)

## 背景

AI-Design-DSL 是一个用于解析和建模软件设计文档的 DSL，草稿位于 `agent-team/wiki/SoftwareDesignModeling/AI-Design-DSL.md`。

当前问题：
- DSL 仅有草稿规范，没有实现
- DocGraph 需要形式化解析能力（术语/条款依赖图）
- 手工维护设计文档容易漂移，需要工具验证

本 Wish 目标：
- 实现 DSL 的基础解析器（ATX-Tree + Term/Clause 提取）
- 提供可测试、可扩展的架构
- 为后续依赖图、引用验证等功能打基础

## 方法论实验

本 Wish 尝试**任务简报模式**：
- 设计文档（Shape/Rule/Plan）本身就是一个个可由 runSubagent 完成的任务
- 每个任务包含：背景、目标、验收标准、输出路径
- Team Leader 逐个调度 SubAgent 完成任务，串联成完整设计

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-12 | TeamLeader | 创建 Wish | 监护人指示：实现 DesignDsl 基础部分 |
| 2026-01-12 | TeamLeader + Curator | 命名确定为 `Atelia.DesignDsl` | Cycle Loss 采样法评估 |
