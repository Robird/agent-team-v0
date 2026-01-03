---
wishId: "W-0004"
title: "完善并实现SizedPtr"
status: Completed
owner: "刘世超"
created: 2026-01-04
updated: 2026-01-04
tags: []
produce:
  - "atelia/docs/Data/Draft/SizedPtr.md"
---

# Wish: 完善并实现SizedPtr

> **一句话动机**: 在Atelia.Data中实现一种将地址和长度打包为64bit的胖指针--SizedPtr

已有初步设想文档`atelia/docs/Data/Draft/SizedPtr.md`
bit分配方案尚未敲定：
  - 尚未排除为不同工况提供多种类型。但不倾向于这个方案，因为其增加了复杂性。
  - 尚未排除用1到2个bit来记录bit划分。但不倾向于这个方案，因为其增加了复杂性。
命名和API设计这类UX/DX问题尚未敲定方案
首个目标用户是将`atelia/docs/Rbf/rbf-interface.md`中的`Address64`类型替换掉。

## 目标与边界

**目标 (Goals)**:
- [ ] 有限范围内的{地址+长度}与uint64之间的双向映射
- [ ] 地址位数不少于36 bit，长度位数不小于24 bit
- [ ] 对外提供2种形态：1. `{ulong Offset; uint Size}`用于作为胖指针访问数据。 2. `ulong Packed`用于存储和传输。`Offset / Size / Packed`并非具体的标识符命名要求，而是用途示意。
- [ ] 地址和长度都按4字节对齐，并有必要的检查
- [ ] 标识符命名一致、简洁、自明
- [ ] API易用
- [ ] 实现在[Atelia.Data](atelia/src/Data)项目中

**非目标 (Non-Goals)**:
- 不处理序列化。用户如何存储和传输ulong不是我们的事。
- 不处理字节序，工作在uint64,uint32语义空间，不涉及byte类型。
- 不定义特殊值，什么是Null什么是Empty，这些是使用者的用法问题
- 不要求实现丰富的便捷方法和工具函数

## 验收标准 (Acceptance Criteria)

- [ ] 所有目标有对应分析文档或单元测试支撑
- [ ] 单元测试通过

## 层级进度 (Layer Progress)

> **术语参考**：本文档使用 [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md#artifact-tiers产物层级)（产物层级）框架组织产物层级。具体层级定义见 [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md)。

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | ⚪ 未开始 | — | |
| Shape-Tier | ⚪ 未开始 | — | |
| Rule-Tier | ⚪ 未开始 | — | |
| Plan-Tier | ⚪ 未开始 | — | |
| Craft-Tier | ⚪ 未开始 | — | |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

## 关联 Issue

| IssueId | 层级 | 状态 | 描述 | 阻塞原因 |
|:--------|:-----|:-----|:-----|:---------|
| — | — | — | — | — |

## 背景 (可选)


## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-04 | [name] | 创建 | 初始创建 |
| 2026-01-04 | AI Team | 标记 Completed | 已在 Atelia.Data 实现 `SizedPtr` 且单元测试通过；后续接入 RBF/目录重构拆分为新 Wish |
