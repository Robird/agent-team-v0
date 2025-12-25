# T-P3-01/02 Implementation Result

## 实现摘要

实现了 StateJournal Phase 3 的基础序列化设施：ValueType 枚举、DiffPayloadWriter 和 DiffPayloadReader。这是 DurableDict 持久化的核心编码层，支持 Null/Tombstone/ObjRef/VarInt/Ptr64 五种值类型。

## 文件变更

### 源码
- [atelia/src/StateJournal/Objects/ValueType.cs](../../../atelia/src/StateJournal/Objects/ValueType.cs) — ValueType 枚举（5 种值类型）+ 扩展方法（验证、提取等）
- [atelia/src/StateJournal/Objects/DiffPayload.cs](../../../atelia/src/StateJournal/Objects/DiffPayload.cs) — DiffPayloadWriter（ref struct）和 DiffPayloadReader（ref struct）
- [atelia/src/StateJournal/Core/StateJournalError.cs](../../../atelia/src/StateJournal/Core/StateJournalError.cs) — 新增 DiffPayloadFormatError、DiffPayloadEofError 错误类型

### 测试
- [atelia/tests/StateJournal.Tests/Objects/ValueTypeTests.cs](../../../atelia/tests/StateJournal.Tests/Objects/ValueTypeTests.cs) — 31 个测试用例
- [atelia/tests/StateJournal.Tests/Objects/DiffPayloadTests.cs](../../../atelia/tests/StateJournal.Tests/Objects/DiffPayloadTests.cs) — 34 个测试用例

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|---------|------|------|
| `[F-KVPAIR-HIGHBITS-RESERVED]` | `ValueTypeExtensions.AreHighBitsZero()` / `ValidateKeyValuePairType()` | 高 4 bit 必须为 0 |
| `[F-UNKNOWN-VALUETYPE-REJECT]` | `ValueType.IsKnown()` + `ValidateKeyValuePairType()` | 未知值类型 fail-fast |
| `[S-DIFF-KEY-SORTED-UNIQUE]` | Writer: `ValidateKeyOrder()` / Reader: delta=0 检测 | 严格升序 + 唯一性 |
| `[S-PAIRCOUNT-ZERO-LEGALITY]` | 支持 PairCount=0 编解码 | Base Version 空字典 |

## 测试结果

- **Targeted**: `dotnet test --filter "FullyQualifiedName~Objects"` → 129/129 ✅
- **Full**: `dotnet test tests/StateJournal.Tests/` → 339/339 ✅

## 设计决策

### DiffPayloadWriter 采用两阶段策略
- Phase 1: 收集所有 pairs 到 `List<...>`（因为需要先写 PairCount）
- Phase 2: `Complete()` 时序列化所有数据到 IBufferWriter

### ref struct 限制处理
- `DiffPayloadReader.TryReadValuePayload()` 使用 `out` 参数 + 返回 `AteliaError?`（而非 `AteliaResult<ReadOnlySpan<byte>>`）
- 测试中异常检测使用 try-catch（而非 FluentAssertions 的 lambda）

### 错误类型新增
- `DiffPayloadFormatError`: 格式错误（高 4 bit 非零等）
- `DiffPayloadEofError`: EOF 错误（payload 截断等）

## 遗留问题

无。

## Changefeed Anchor
`#delta-2025-12-26-diffpayload`
