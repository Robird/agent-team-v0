# [A9] 合并 Appendix B 到独立 test vectors 文件 — Implementation Result

## 实现摘要

根据畅谈会共识执行 A9 任务，将主文档的 Appendix B（Test Vectors 骨架）整合到独立的 `mvp-test-vectors.md` 文件，实现避免重复、统一维护的目标。同时修复了术语对齐问题（EpochMap → VersionIndex）。

## 文件变更

- `DurableHeap/docs/mvp-test-vectors.md` — 新增条款编号映射表，修复术语对齐
- `DurableHeap/docs/mvp-design-v2.md` — Appendix B 替换为引用说明

## 源码对齐说明

| 任务要求 | 实现 | 备注 |
|---------|---------|------|
| 添加条款编号映射表 | ✅ 7 条映射规则 | 包含 F-01/F-02/F-03/R-01/R-02/S-01/A-01 |
| 从主文档删除 Appendix B 骨架 | ✅ 替换为引用 | 保留章节标题，内容指向独立文件 |
| 验证术语对齐 | ✅ 2 处修复 | EpochMapVersionPtr → VersionIndexPtr |

## 测试结果

- 无需测试（纯文档重构）

## 已知差异

- test vectors 文件原有内容比 Appendix B 骨架更完善，因此采用"保留原有内容 + 添加映射表"的策略
- 条款编号（如 `[F-01]`）目前为占位符，待 A4 任务完成后正式对齐

## 遗留问题

- A4 任务（给现有 MUST/SHOULD 条款编号）尚未完成，映射表中的条款 ID 待确认
- A7（Wire Format ASCII 图）和 A8（核心洞察）仍为后续任务

## Changefeed Anchor

`#delta-2025-12-20-a9-test-vectors-merge`
