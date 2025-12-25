# [T-P3-04] _dirtyKeys 精确追踪机制 - Implementation Result

## 实现摘要

实现了 DurableDict 的 _dirtyKeys 精确追踪机制。当值恢复到 committed 状态时，自动移除 dirtyKey，使 `HasChanges` 能够准确反映实际变更状态。

## 文件变更

- [DurableDict.cs](../../atelia/src/StateJournal/Objects/DurableDict.cs) — 实现精确追踪逻辑
- [DurableDictTests.cs](../../atelia/tests/StateJournal.Tests/Objects/DurableDictTests.cs) — 添加 14 个精确追踪测试

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|---------|------|------|
| `[S-DIRTYKEYS-TRACKING-EXACT]` | `UpdateDirtyKeyForSet()` / `UpdateDirtyKeyForRemove()` | 精确追踪变更 |
| `[S-WORKING-STATE-TOMBSTONE-FREE]` | Working State 不存储 tombstone | 保持不变 |

### 实现细节

**UpdateDirtyKeyForSet(key, value)**：
```csharp
bool hasCommitted = _committed.TryGetValue(key, out var committedValue);
bool isEqual = hasCommitted 
    ? EqualityComparer<TValue>.Default.Equals(newValue, committedValue)
    : false;

if (isEqual) _dirtyKeys.Remove(key);
else _dirtyKeys.Add(key);
```

**UpdateDirtyKeyForRemove(key, hadInWorking, hasInCommitted)**：
- 删除 committed key → `_dirtyKeys.Add(key)`
- 删除新 key（未提交）→ `_dirtyKeys.Remove(key)`

## 测试结果

- **Targeted**: `dotnet test --filter DirtyKeys` → 14/14 ✓
- **Full**: `dotnet test StateJournal.Tests` → 407/407 ✓

### 新增测试用例

| 测试名 | 场景 |
|--------|------|
| `DirtyKeys_SetBackToOriginalValue_HasChangesBecomeFalse` | Set 回原值不脏 |
| `DirtyKeys_RemoveNewKey_HasChangesBecomeFalse` | Remove 新 key 不脏 |
| `DirtyKeys_RemoveCommittedKey_HasChangesIsTrue` | Remove committed key 是脏 |
| `DirtyKeys_RemoveThenSetOriginal_HasChangesBecomeFalse` | Remove + Set 恢复 |
| `DirtyKeys_GetOperations_DoNotPollute` | Get 不污染 |
| `DirtyKeys_RoundTrip_SetSameValueAfterCommit_HasChangesIsFalse` | 往返测试 |
| `DirtyKeys_MultipleKeys_MixedTracking` | 多 key 混合追踪 |
| `DirtyKeys_RemoveNonExistent_DoesNotChangeHasChanges` | Remove 不存在的 key |
| `DirtyKeys_SetNull_TracksCorrectly` | null 值追踪 |
| `DirtyKeys_TransientDirty_SetAndRemove` | TransientDirty 状态追踪 |
| `DirtyKeys_ComplexScenario_SameKeyMultipleOperations` | 同 key 多次操作 |
| `DirtyKeys_WritePendingDiff_ConsistentWithHasChanges` | Diff 输出一致性 |

## 已知差异

无。实现完全符合 mvp-design-v2.md 规范。

## 遗留问题

无。

## Changefeed Anchor

`#delta-2025-12-26-dirtykeys-exact`
