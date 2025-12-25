# T-P3-03b DurableDict 序列化集成 Implementation Result

## 实现摘要

实现了 DurableDict 的 `WritePendingDiff` 和 `OnCommitSucceeded` 方法，完成二阶段提交 API 的 Prepare/Finalize 语义。这是 Phase 3 序列化任务的关键里程碑，使 DurableDict 具备了持久化能力。

## 文件变更

### 源码
- [atelia/src/StateJournal/Objects/DurableDict.cs](../../../atelia/src/StateJournal/Objects/DurableDict.cs) — 实现 `WritePendingDiff`、`OnCommitSucceeded` 和 `WriteValue` 辅助方法

### 测试
- [atelia/tests/StateJournal.Tests/Objects/DurableDictTests.cs](../../../atelia/tests/StateJournal.Tests/Objects/DurableDictTests.cs) — 新增 18 个序列化测试用例

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|---------|------|------|
| `[S-POSTCOMMIT-WRITE-ISOLATION]` | `WritePendingDiff` 不修改 `_committed`、`_working`、`_dirtyKeys` | 测试验证：调用后 `HasChanges` 仍为 true |
| `[S-DIFF-KEY-SORTED-UNIQUE]` | `sortedDirtyKeys = _dirtyKeys.OrderBy(k => k)` | key 按升序排列后传给 DiffPayloadWriter |

## 测试结果

- **Targeted**: `dotnet test --filter "FullyQualifiedName~DurableDict"` → 56/56 ✅
- **Full**: `dotnet test tests/StateJournal.Tests/` → 395/395 ✅

## 新增测试用例

### WritePendingDiff 测试 (10 个)
| 测试 | 验证点 |
|------|--------|
| `WritePendingDiff_SingleValue_SerializesCorrectly` | 单值序列化往返 |
| `WritePendingDiff_MultipleValues_KeysAreSorted` | 乱序 Set 后输出 key 升序 |
| `WritePendingDiff_NullValue_WritesNull` | null 值序列化为 ValueType.Null |
| `WritePendingDiff_Remove_WritesTombstone` | 删除操作序列化为 Tombstone |
| `WritePendingDiff_DoesNotUpdateState` | 调用后 HasChanges 仍为 true |
| `WritePendingDiff_NoChanges_WritesEmptyPayload` | 无变更时 PairCount=0 |
| `WritePendingDiff_IntValue_SerializesAsVarInt` | int 自动转为 VarInt |
| `WritePendingDiff_MixedOperations_SerializesCorrectly` | 混合 Set/Remove |
| `WritePendingDiff_UnsupportedType_ThrowsNotSupportedException` | 不支持类型抛异常 |
| `WritePendingDiff_Detached_ThrowsObjectDetachedException` | Detached 状态检查 |

### OnCommitSucceeded 测试 (8 个)
| 测试 | 验证点 |
|------|--------|
| `OnCommitSucceeded_ClearsHasChanges` | HasChanges 变 false |
| `OnCommitSucceeded_StateBecomesClean` | State 变 Clean |
| `OnCommitSucceeded_ValuesStillAccessible` | 值从 _committed 可读 |
| `OnCommitSucceeded_MergesToCommitted` | 合并 _working 到 _committed |
| `OnCommitSucceeded_HandlesRemove` | 删除合并到 _committed |
| `OnCommitSucceeded_ThenModify_BecomesPersistentDirty` | 再次修改变 PersistentDirty |
| `OnCommitSucceeded_Detached_ThrowsObjectDetachedException` | Detached 状态检查 |
| `TwoPhaseCommit_RoundTrip` | 完整二阶段提交流程 |

## 设计决策

### MVP 值类型限制
仅支持 `null`、`long`、`int` 三种类型：
- `null` → `ValueType.Null`
- `long`/`int` → `ValueType.VarInt`（int 自动转为 long）

其他类型抛 `NotSupportedException`，后续可扩展支持 `ObjRef`、`Ptr64` 等。

### WriteValue 为静态方法
`WriteValue` 不依赖实例状态，定义为 `private static`，符合 C# 静态分析最佳实践。

## 已知差异

无。

## 遗留问题

- `DiscardChanges` 尚未实现（T-P3-05）
- 后续需要支持更多值类型（ObjRef、Ptr64）

## Changefeed Anchor
`#delta-2025-12-26-durable-dict-serialization`
