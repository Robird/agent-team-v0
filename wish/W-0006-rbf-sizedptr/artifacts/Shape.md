---
docId: "W-0006-Shape"
title: "W-0006 Shape"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
---

# Shape (W-0006)

## 两类语义

| 类型 | 语义 | 使用场景 |
|:-----|:-----|:---------|
| Address64 / Ptr64 | file offset（指向 Frame 起点）| RBF 层既有语义 |
| SizedPtr | span/range（offset+length）| 通用产品，4B 对齐 |

## 术语边界

- RBF 层继续使用 Address64/Ptr64 表达 Frame 起始位置
- 需要表达区间时使用 SizedPtr

