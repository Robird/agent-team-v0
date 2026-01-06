# F-002: SizedPtr 4B 粒度 vs 字节粒度歧义

**位置**: rbf-interface.md §2.3 `[F-SIZEDPTR-DEFINITION]` 与 rbf-format.md §7.1 `[F-SIZEDPTR-WIRE-FORMAT]`
**类型**: Ambiguity
**描述**: 接口文档的属性名 `OffsetBytes`/`LengthBytes` 暗示单位是字节，但格式文档说"4B 粒度"。不清楚属性返回的是实际字节偏移还是原始编码值。
**风险**: 实现者可能把 `OffsetBytes = 100` 当成"偏移 100 字节"去读，实际应该是"偏移 400 字节"，导致读取错位。
**建议**: 明确 `OffsetBytes` / `LengthBytes` 是解码后的字节值（调用者无需乘 4），还是原始编码值（调用者需乘 4）。
