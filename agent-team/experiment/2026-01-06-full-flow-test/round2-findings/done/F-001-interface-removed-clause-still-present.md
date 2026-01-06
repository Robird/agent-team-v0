# F-001: rbf-interface 声称已移除条款但正文仍保留

**位置**: atelia/docs/Rbf/rbf-interface.md §2.2 Tombstone（含 `[S-STATEJOURNAL-TOMBSTONE-SKIP]`）与 §6 条款索引后的“已移除的条款”说明
**描述**: 文档在 §2.2 明确写了 `[S-STATEJOURNAL-TOMBSTONE-SKIP]`（上层必须跳过 Tombstone），但在后文又声明 `[S-STATEJOURNAL-TOMBSTONE-SKIP]` 已移至 `mvp-design-v2.md`，形成自相矛盾：读者无法判断该条款是否仍属于接口契约。
**建议**: 二选一并保持全文一致：
- 若该条款应归属上层：从 `rbf-interface.md` 正文移除该条款（或降级为 Informative），并在上层文档给出 SSOT；
- 若该条款应保留在接口契约：删除“已移除的条款”中的对应声明，并在条款索引保留其条目。
