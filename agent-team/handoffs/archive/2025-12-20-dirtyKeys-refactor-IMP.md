# [DurableHeap MVP v2] _isDirty → _dirtyKeys 重构 Implementation Result

## 实现摘要

根据 2025-12-19 畅谈会第四轮共识，将 DurableDict 的 dirty tracking 从 `bool _isDirty` 修改为 `ISet<ulong> _dirtyKeys` 集合，实现精确的 key 级变更追踪。

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 11 处修改（术语表、语义章节、不变式、伪代码）

## 源码对齐说明

| 修订前 | 修订后 | 备注 |
|--------|--------|------|
| `bool _isDirty` | `HashSet<ulong> _dirtyKeys` | 字段类型变更 |
| `_isDirty = true` | `UpdateDirtyKey(key)` | 精确维护 dirty 状态 |
| `_isDirty = false` | `_dirtyKeys.Clear()` | 清空操作 |
| `!_isDirty` fast path | `_dirtyKeys.Count == 0` | 判空逻辑 |
| `ComputeDiff(old, new)` | `ComputeDiff(old, new, dirtyKeys)` | 只遍历 dirty keys |

## 修订详情

### 1. 术语表更新（2 处）

- ChangeSet 行：实现映射从 `_isDirty` 改为 `_dirtyKeys`
- OnCommitSucceeded 行：`_isDirty = false` → `_dirtyKeys.Clear()`

### 2. 4.4.1 ChangeSet 语义（1 处）

新增 `_dirtyKeys` 维护规则：

```
概念层语义（不变）：
- HasChanges ⟺ ∃ key: CurrentValue(key) ≠ CommittedValue(key)

实现层规则：
- HasChanges = _dirtyKeys.Count > 0
- Upsert(key, value)：比较 value 与 _committed[key]
  - 若相等：_dirtyKeys.Remove(key)
  - 若不等：_dirtyKeys.Add(key)
- Delete(key)：
  - 若 _committed.ContainsKey(key)：_dirtyKeys.Add(key)
  - 若 !_committed.ContainsKey(key)：_dirtyKeys.Remove(key)
```

### 3. 4.4.3 不变式（1 处）

新增第 9 条不变式：

```
9. **_dirtyKeys 精确性**：_dirtyKeys 必须精确追踪所有与 _committed 存在语义差异的 key。
   - 对任意 key k：k ∈ _dirtyKeys ⟺ CurrentValue(k) ≠ CommittedValue(k)
```

### 4. 4.4.4 伪代码骨架（重构）

- 字段定义：`private HashSet<ulong> _dirtyKeys = new();`
- 新增 `UpdateDirtyKey(K key)` 方法
- `Set()` 和 `Delete()` 调用 `UpdateDirtyKey`
- `ComputeDiff` 增加 `dirtyKeys` 参数，复杂度从 O(n+m) 降为 O(|dirtyKeys|)
- 二阶段表格和关键实现要点更新

### 5. 4.4.5 Commit 步骤（2 处）

- 规范约束中 `_isDirty` → `_dirtyKeys`
- 步骤 5：`_isDirty=false` → `清空 _dirtyKeys`

## 测试结果

- 文档编译：✅ Markdown 语法正确
- 伪代码：✅ C# 语法正确，可编译

## 已知差异

无（这是文档修订，不涉及实际代码变更）

## 遗留问题

无

## 设计收益

1. **性能优化**：`ComputeDiff` 只需遍历 `_dirtyKeys` 而非全量扫描两个字典
2. **语义精确**：`HasChanges` 只有真正存在差异的 key 才会被计入
3. **消除困惑**：避免"set-then-delete 回到原状态但 `_isDirty` 仍为 true"的问题

## Changefeed Anchor

`#delta-2025-12-20-dirtyKeys-refactor`
