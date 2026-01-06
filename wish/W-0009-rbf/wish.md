---
wishId: "W-0009"
title: "设计并实现RBF（Reversible Binary Framing）"
status: Active
owner: "监护人刘世超"
created: 2026-01-06
updated: 2026-01-06
tags: [rbf, design]
produce:
  # 外部产物文档
  - "atelia/docs/Rbf/rbf-interface.md"
  - "atelia/docs/Rbf/rbf-format.md"
  - "atelia/docs/Rbf/rbf-test-vectors.md"
---

# Wish: 修订 RBF 设计稿：引入/改用 SizedPtr

> **一句话动机**: 让 RBF/StateJournal 在需要表达"区间（offset+length）"时使用 SizedPtr，从而减少重复设计与类型漂移，并提升跨层复用。

## 目标与边界

**目标 (Goals)**:
- [ ] 审阅并修订 RBF 相关设计文档，使其能引用 `Atelia.Data.SizedPtr` 表达"4B 对齐的 byte range"
- [ ] 明确区分两类语义：
  - **<deleted-place-holder> / Ptr64**：指向某个 Frame 起始位置的 file offset（RBF 层既有语义）
  - **SizedPtr**：表达一个 span/range（offset+length）的紧凑表示（通用产品）
- [ ] 在 `rbf-interface.md` 与 `rbf-format.md` 中明确哪些字段/接口应继续使用 <deleted-place-holder>，哪些应改用 SizedPtr
- [ ] 为后续实际实现迁移提供清晰的 Decision Log

**非目标 (Non-Goals)**:
- 不在本 Wish 中实现 RBF 的代码（当前 active code 里尚无完整 RBF 实现）
- 不在本 Wish 中强制把 <deleted-place-holder> 完全删除或替换为 SizedPtr
- 不强制为 SizedPtr 引入 Null/Empty 等语义