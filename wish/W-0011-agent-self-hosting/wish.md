---
wishId: "W-0011"
title: "Agent Self-Hosting: 迁移到自建 Agent 运行环境"
status: Biding
owner: "TeamLeader + Guardian"
created: 2026-01-15
updated: 2026-01-15
tags: [agent-os, infrastructure, strategic]
produce:
  - "wish/W-0011-agent-self-hosting/project-status/goals.md"
  - "wish/W-0011-agent-self-hosting/project-status/issues.md"
  - "wish/W-0011-agent-self-hosting/project-status/snapshot.md"
  - "wish/W-0011-agent-self-hosting/artifacts/Resolve.md"
---

# Wish: Agent Self-Hosting

> **一句话动机**: 降低对上游实验功能的依赖，让 AI Team 能在自建环境中稳定运行。

## 目标与边界

**目标 (Goals)**:
- [ ] AI Team 能在自建环境中使用 runSubagent 功能
- [ ] 自定义 Agent（如 Investigator、Implementer）可在 SubAgent 中调用
- [ ] 会话状态可持久化，不依赖 copilot-chat 扩展的会话管理

**非目标 (Non-Goals)**:
- 完全脱离 VS Code 环境（短期内仍使用 VS Code 作为 UI 层）
- 实现完整的 Agent-OS（那是更长期的愿景）
- 支持多用户/多租户场景

## 验收标准 (Acceptance Criteria)

- [ ] 在自建环境中 `runSubagent` 能正常调用指定 Agent
- [ ] Agent 间能通过某种机制传递上下文（替代当前依赖 VS Code 的机制）
- [ ] 不受 VS Code 更新影响（即使 `customAgentInSubagent` 被移除也能工作）

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟡 进行中 | [artifacts/Resolve.md](artifacts/Resolve.md) | 孵化中，待明确愿景 |
| Shape-Tier | ⚪ 未开始 | - | |
| Rule-Tier | ⚪ 未开始 | - | |
| Plan-Tier | ⚪ 未开始 | - | |
| Craft-Tier | ⚪ 未开始 | - | |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

见：[project-status/issues.md](project-status/issues.md)

## 背景

### 问题起源

2026-01-15，copilot-chat fork 与 VS Code Insider 版本不匹配导致会话管理出现问题。
调查过程中发现 AI Team 依赖的关键功能 `chat.customAgentInSubagent.enabled` 是 VS Code 本体的**实验功能**，存在被移除的风险。

### 当前依赖链

| 依赖项 | 风险等级 | 自建替代方向 |
|:-------|:---------|:-------------|
| VS Code `runSubagent` 工具 | 🟡 中（实验功能） | 自建 Agent 调度器 |
| `customAgentInSubagent.enabled` | 🔴 高（可能移除） | 自建 Agent 注册机制 |
| copilot-chat 会话管理 | 🟡 中（频繁变更） | 自建会话持久化 |
| copilot-chat 认证/计费 | 🟢 低（核心功能） | 暂不替换 |

### 战略思考（待孵化）

监护人提出了两个可能的方向：
1. **响应式问答模式**：降低技术规格，保持现有问答交互模式，只是摆脱上游依赖
2. **长期自主活动**：直奔 Agent-OS 愿景，让 AI Team 能持续自主运行

这两个方向的技术栈选择可能不同，需要在 Resolve-Tier 中明确。

### 前置依赖

- W-0008-persistent-agent-session（会话持久化的探索）
- Agent-OS 理论框架（`agent-team/beacon/draft-agent-operating-system.md`）
- atelia 项目中的 Agent.Core 原型（`atelia/prototypes/Agent.Core`）

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-15 | TeamLeader | 创建 | copilot-chat 同步问题触发的战略思考 |
