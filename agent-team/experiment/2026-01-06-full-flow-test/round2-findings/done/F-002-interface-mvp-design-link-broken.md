# F-002: rbf-interface 对 mvp-design-v2.md 的相对链接疑似错误

**位置**: atelia/docs/Rbf/rbf-interface.md §5 “使用示例（Informative）”段落中的 `[mvp-design-v2.md](mvp-design-v2.md)` 链接（以及 §1 文档关系对该文件的引用）
**描述**: 仓库中 `mvp-design-v2.md` 实际位于 `atelia/docs/StateJournal/mvp-design-v2.md`，而 `rbf-interface.md` 在 `atelia/docs/Rbf/` 下使用相对链接 `mvp-design-v2.md` 会指向不存在的文件路径，导致“文档关系/上层语义来源”在阅读时断链。
**建议**: 将链接修正为正确相对路径（例如 `../StateJournal/mvp-design-v2.md`），并顺便检查文档中所有对 `mvp-design-v2.md` 的引用是否一致使用同一路径。
