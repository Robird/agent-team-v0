---
docId: "W-0006-Plan"
title: "W-0006 Plan"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
---

# Plan (W-0006)

## 迁移计划

1. 审阅 rbf-interface.md，识别需要引入 SizedPtr 的位置
2. 审阅 rbf-format.md，确定 wire format 编码方式
3. 编写 Decision Log 记录 tradeoff
4. 更新文档

## 迁移说明

2026-01-05：从 `wishes/active/wish-0006-rbf-migrate-to-sizedptr.md` 迁入新实例目录。

## 待清理项

- [ ] 删除旧文件 `wishes/active/wish-0006-rbf-migrate-to-sizedptr.md`（待整体迁移完成后统一清理）
- [ ] 更新外部产物文档的 `produce_by` 指向新路径（已追加）

