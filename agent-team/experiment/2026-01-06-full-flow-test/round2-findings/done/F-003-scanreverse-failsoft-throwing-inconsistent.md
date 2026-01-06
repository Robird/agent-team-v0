# F-003: ScanReverse 对不完整文件的“不得抛异常”与 rbf-format 的 MAY throw 冲突

**位置**: atelia/docs/Rbf/rbf-interface.md §4.2 `[S-RBF-SCANREVERSE-EMPTY-IS-OK]` vs atelia/docs/Rbf/rbf-format.md §2.2 `[F-FILE-MINIMUM-LENGTH]`
**描述**: 接口契约规定当 `fileLength < 4` 时 `ScanReverse()` 必须返回空序列且“不得抛出异常”。但线格式规范在 `[F-FILE-MINIMUM-LENGTH]` 写的是：`fileLength < 4` Reader SHOULD fail-soft（返回空序列），MAY 抛出异常。两者在“是否允许抛异常”上不一致，会导致实现/测试无法判定正确行为。
**建议**: 明确分层并对齐措辞：
- 若以接口契约为准：将 `rbf-format.md` 中该场景收敛为“不得抛异常”（至少对 Reverse Scan 路径），把 “MAY throw” 移到其他 API（例如构造 Scanner 时参数非法）或移除；
- 若以线格式为准允许抛异常：则更新 `rbf-interface.md`，把“不得抛异常”改为 SHOULD fail-soft + MAY throw，并补充何时允许 throw（例如 IO 访问失败 vs 纯解析失败）。
