---
wishId: "W-0001"
title: "Wish 系统自举"
status: Active
owner: "监护人刘世超"
created: 2025-12-30
updated: 2025-12-30
tags: [meta, infrastructure]
produce:
  - "wishes/specs/wish-system-spec.md"
---

# Wish: Wish 系统自举

> **一句话动机**: 建立 Wish 驱动的开发工作流，让动机成为导航起点而非文档终点。

## 目标与边界

**目标 (Goals)**:
- [x] 定义 Wish 文档的标准格式和必填字段
- [x] 建立 `wishes/` 目录结构（active/completed/abandoned）
- [x] 创建 index.md 作为单体入口（派生视图）
- [x] 定义 L3 条款文件（MUST/SHOULD/MAY 规范）
- [ ] 团队成员熟悉并能 5 秒内判断问题层级

**非目标 (Non-Goals)**:
- 自动化工具（MVP 阶段手工维护）
- 复杂的状态机和工作流引擎
- 与 Git 深度集成

## 验收标准 (Acceptance Criteria)

- [x] `wishes/` 目录已创建，结构符合设计
[x] 至少 2 个 Wish 文档已创建（本 Wish + DocGraph）
- [x] index.md 可用于导航
- [x] Rule-Tier 条款文件已定义核心 MUST 条款
- [ ] 用 Wish 系统成功追踪一个完整 Wish 生命周期

## 层级进度 (Layer Progress)

> **术语参考**：本文档使用 [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md#artifact-tiers产物层级)（产物层级）框架组织产物层级。具体层级定义见 [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md)。

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟢 完成 | [畅谈会记录](../../agent-team/meeting/Meta/2025-12-30-layers-of-dev.md) | 动机已充分表达 |
| Shape-Tier | 🟢 完成 | [模板文件](../templates/wish-template.md) | 文档结构设计 |
| Rule-Tier | 🟢 完成 | [wish-system-rules.md](../specs/wish-system-rules.md) | MUST/SHOULD/MAY 条款 |
| Plan-Tier | 🟡 进行中 | — | 手工维护方案，暂无需文档 |
| Craft-Tier | ➖ N/A | — | MVP 无自动化代码 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

| IssueId | 层级 | 状态 | 描述 | 阻塞原因 |
|:--------|:-----|:-----|:-----|:---------|
| — | — | — | 暂无阻塞 Issue | — |

## 背景

Wish 系统源于监护人的核心洞见：**动机是最易丢失的信息**。代码会保留（能执行），规范会保留（被引用），但动机常常消失在会议记录和聊天历史中。

Wish 系统将动机从"过程副产品"提升为"一等公民"，通过以下设计实现：
- **语义缩放**：最短的文本承载最高的信息密度（30 秒速读）
- **入口即地图**：单体 index.md 提供全局概览
- **Token 经济性**：LLM Agent 可用最少 token 建立整体概念

这是一个**自举的** Wish——用 Wish 系统来定义 Wish 系统。这种自指性验证了系统的通用性。

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2025-12-30 | Implementer | 创建 | 基于畅谈会共识初始化 Wish 系统 |
