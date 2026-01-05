---
docId: "W-0006-Resolve"
title: "W-0006 Resolve"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
---

# Resolve (W-0006)

## 动机

Wish-0004 已在 `Atelia.Data` 中实现 `SizedPtr` 并通过单元测试。在模拟执行过程中暴露出"文档层术语混用"的风险：
- `Address64/Ptr64` 是 file offset（指向 Frame 起点）
- `SizedPtr` 是 span（offset+length）

## 决心

RBF 作为目标用户之一，应在其设计稿中复用 SizedPtr，而不是在 RBF 层重复发明另一套 span 表达。

## 来源

来自 Wish-0004 的后续工作拆分。

