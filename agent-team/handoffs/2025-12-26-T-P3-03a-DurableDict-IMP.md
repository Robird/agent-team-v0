# [T-P3-03a] DurableDict 基础结构 - Implementation Result

## 实现摘要

实现了 `DurableDict<TValue>` 的双字典模型基础结构，包含 Committed State 和 Working State 的分离存储，以及完整的读写 API。关键设计：使用 `_removedFromCommitted` 集合跟踪已删除的 Committed 键，避免在 Working State 中存储 tombstone。

## 文件变更

- `atelia/src/StateJournal/Objects/DurableDict.cs` — DurableDict 实现 + ObjectDetachedException
- `atelia/tests/StateJournal.Tests/Objects/DurableDictTests.cs` — 40 个测试用例

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|---------|------|------|
| `[A-DURABLEDICT-API-SIGNATURES]` | ✅ 完全匹配 | TryGetValue, ContainsKey, this[], Count, Keys, Entries, Set, Remove |
| `[S-DURABLEDICT-KEY-ULONG-ONLY]` | ✅ Key 类型 = ulong | 泛型参数只用于 TValue |
| `[S-WORKING-STATE-TOMBSTONE-FREE]` | ✅ 使用 _removedFromCommitted | Remove 不存 tombstone，用单独集合追踪 |

## 实现要点

### 双字典模型内部状态

```csharp
private readonly Dictionary<ulong, TValue?> _committed;       // Committed State
private readonly Dictionary<ulong, TValue?> _working;         // Working State
private readonly HashSet<ulong> _removedFromCommitted;        // 跟踪已删除的 Committed 键
private readonly HashSet<ulong> _dirtyKeys;                   // 变更追踪
```

### 读取优先级

1. 先查 `_working`
2. 检查 `_removedFromCommitted`（已删除则返回不存在）
3. 再查 `_committed`

### Remove 逻辑

- 从 `_working` 移除（如存在）
- 若键在 `_committed` 中，加入 `_removedFromCommitted`
- 不存储 tombstone

### Set 恢复逻辑

- 若键在 `_removedFromCommitted` 中，移除该标记
- 这允许 Remove 后再 Set 同一个键

## 测试结果

- Targeted: `dotnet test --filter DurableDictTests` → 40/40 ✅
- Full: `dotnet test StateJournal.Tests` → 379/379 ✅

## IDurableObject 骨架方法

以下方法为骨架，后续任务实现：

| 方法 | 任务 |
|-----|------|
| `WritePendingDiff()` | T-P3-03b |
| `OnCommitSucceeded()` | T-P3-03b |
| `DiscardChanges()` | T-P3-05 |

## 已知差异

无（完全按规范实现）。

## 遗留问题

1. `_dirtyKeys` 更新为简化版（所有 Set/Remove 直接加入），T-P3-04 会完善为精确追踪
2. Count 计算复杂度为 O(n)，可优化为增量维护

## Changefeed Anchor

`#delta-2025-12-26-T-P3-03a-DurableDict`
