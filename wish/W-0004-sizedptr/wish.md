---
wishId: "W-0004"
title: "完善并实现SizedPtr"
status: Completed
owner: "刘世超"
created: 2026-01-04
updated: 2026-01-05
tags: [sample, migrated]
produce:
  - "atelia/docs/Data/Draft/SizedPtr.md"
  - "wish/W-0004-sizedptr/project-status/snapshot.md"
  - "wish/W-0004-sizedptr/artifacts/Resolve.md"
  - "wish/W-0004-sizedptr/artifacts/Shape.md"
  - "wish/W-0004-sizedptr/artifacts/Rule.md"
  - "wish/W-0004-sizedptr/artifacts/Plan.md"
  - "wish/W-0004-sizedptr/artifacts/Craft.md"
---

# Wish: 完善并实现SizedPtr

> **一句话动机**: 在 Atelia.Data 中实现一种将地址和长度打包为 64bit 的胖指针——SizedPtr。

> 注：这是 W-0005 的迁移样例（把单文件 Wish 升级为实例目录）。

## 目标与边界

**目标 (Goals)**:
- [x] 在 `Atelia.Data` 中实现 `SizedPtr`
- [x] 单元测试通过

**非目标 (Non-Goals)**:
- 不处理序列化/字节序
- 不定义 Null/Empty 等特殊值语义（由上层约定）

## 验收标准 (Acceptance Criteria)

- [x] 目标实现完成且测试通过

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟢 完成 | `artifacts/Resolve.md` | |
| Shape-Tier | 🟢 完成 | `artifacts/Shape.md` | |
| Rule-Tier | 🟢 完成 | `artifacts/Rule.md` | |
| Plan-Tier | 🟡 进行中 | `artifacts/Plan.md` | |
| Craft-Tier | 🟢 完成 | `artifacts/Craft.md` | 实现已落地 |

## 关联 Issue

见：`project-status/issues.gen.md`

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-04 | AI Team | 标记 Completed | SizedPtr 已实现且测试通过 |
| 2026-01-05 | AI Team | 迁移为实例目录 | W-0005 样例迁移 |
