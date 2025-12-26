# Investigation Brief: `_removedFromCommitted` 集合的必要性分析

> **Investigator**: 2025-12-26
> **任务**: 调查 `DurableDict._removedFromCommitted` 集合是否多余
> **结论**: ⚠️ **设计缺陷确认** — 该集合在概念上可以消除，但当前实现架构下有其存在理由

---

## 1. 问题背景

监护人指出：

> 根本就不应该有 `_removedFromCommitted` 这个集合。在 Load 的 Materialize 阶段就应该处理掉 Val_Tombstone 的语义，进入最终暴露给外界的 DurableDict 实例里面的 `_committed` 集合应该是磁盘上的最终逻辑状态才对。

## 2. 当前实现分析

### 2.1 `_removedFromCommitted` 的使用场景

| 场景 | 代码位置 | 用途 |
|-----|---------|-----|
| **读取** | `TryGetValue`, `ContainsKey`, `this[key]` | 判断 key 是否已从 committed 删除 |
| **枚举** | `Count`, `Keys`, `Entries` | 过滤已删除的 committed key |
| **写入** | `Remove` | 标记 committed key 为已删除 |
| **恢复** | `Set` | 恢复被删除的 key（从集合中移除） |
| **序列化** | `WritePendingDiff` | 为已删除的 committed key 写入 tombstone |
| **提交** | `OnCommitSucceeded` | 从 `_committed` 中真正删除 key，然后清空集合 |
| **丢弃** | `DiscardChanges` | 清空集合（重置为 committed 状态） |

### 2.2 问题本质：双字典策略的副产品

当前实现采用**双字典策略**：
- `_committed`: 上次 Commit 成功时的快照
- `_working`: 当前工作状态

**关键设计决策**：`_committed` 字典在 Commit 之前**不被修改**。

这导致了一个问题：当用户调用 `Remove(key)` 时：
- 如果 key 在 `_working` 中：直接删除 ✅
- 如果 key 在 `_committed` 中：不能修改 `_committed`，只能**记录**删除意图 ❌

`_removedFromCommitted` 就是为了记录这个删除意图而存在的。

## 3. 规范条款分析

### 3.1 `[S-WORKING-STATE-TOMBSTONE-FREE]` 条款

> Working State 纯净性：在任何对外可读/可枚举的状态视图中，tombstone 不得作为值出现；Delete 的语义是"key 不存在"。

当前实现**符合**此条款：
- `TryGetValue` 返回 `false`（不返回 tombstone）
- `ContainsKey` 返回 `false`
- 枚举时跳过被删除的 key

### 3.2 加载流程中的 Tombstone 处理

从 [mvp-design-v2.md](../../atelia/docs/StateJournal/mvp-design-v2.md) §3.1.0：

> **Materialize（合成状态）**：将 Checkpoint Base 状态与若干 overlay diff 合并为当前可读的 **Committed State**。

规范意图是：Materialize 阶段合成的是**最终逻辑状态**，tombstone 应该被"消化"掉。

### 3.3 当前实现的 Load 逻辑

从代码来看，`DurableDict` 有一个 internal 构造函数：

```csharp
internal DurableDict(ulong objectId, Dictionary<ulong, object?> committed) {
    ObjectId = objectId;
    _committed = committed ?? throw new ArgumentNullException(nameof(committed));
    _working = new Dictionary<ulong, object?>();
    _removedFromCommitted = new HashSet<ulong>();  // 初始为空！
    _dirtyKeys = new HashSet<ulong>();
    _state = DurableObjectState.Clean;
}
```

**观察**：加载时 `_removedFromCommitted` 初始化为空集合。

这意味着：
1. **如果 Materialize 正确处理了 tombstone**：传入的 `committed` 字典已经是最终状态（不含被删除的 key），加载后 `_removedFromCommitted` 保持为空 ✅
2. **`_removedFromCommitted` 只在运行时 Remove 操作中被填充**：用于追踪"本次会话中删除的 committed key" ✅

## 4. 核心问题定位

### 4.1 为什么需要 `_removedFromCommitted`？

答案：**因为 `_committed` 在 Commit 之前是只读的**。

当前架构：
```
_committed (只读快照)  ─┬─► 读取路径需要合并两者
_working  (可写覆盖)   ─┘
_removedFromCommitted  ─► 记录"从 committed 删除"的意图
```

### 4.2 替代设计：合并 Working State

如果 Working State 是一个**合并视图**而非独立字典：

```
_committed (基底状态)
↓ Apply changes
_current (最终可读状态，包含所有变更)
```

这种设计下：
- `Remove(key)` 直接从 `_current` 删除
- 不需要 `_removedFromCommitted`
- `ComputeDiff` 比较 `_committed` 和 `_current` 即可

这正是**监护人建议的设计**。

### 4.3 当前设计的合理性

当前设计也有其优势：
1. **Commit 失败时恢复简单**：`_committed` 未被修改，直接清空 `_working` 和 `_removedFromCommitted` 即可
2. **Diff 计算高效**：只需遍历 `_dirtyKeys`，不需要全量比较
3. **内存占用可预测**：`_committed` 和 `_working` 都是完整字典，易于理解

但代价是：
1. **读取路径复杂**：每次读取都要检查 3 个数据结构
2. **语义分裂**：Working State 被分散到 `_working` + `_removedFromCommitted`
3. **违反直觉**：`_committed` 不是真正的 "committed state"，因为需要排除 `_removedFromCommitted` 中的 key

## 5. 结论与建议

### 5.1 设计评估

| 维度 | 当前设计 | 替代设计（监护人建议） |
|-----|---------|---------------------|
| 概念清晰度 | ⚠️ 中等（需要理解 3 个数据结构的关系） | ✅ 高（2 个状态快照） |
| 读取性能 | ⚠️ 需检查 3 处 | ✅ 直接读取 |
| 写入性能 | ✅ O(1) | ✅ O(1) |
| Commit 失败恢复 | ✅ 简单（清空 working/removed） | ⚠️ 需要恢复 _current |
| 内存占用 | ⚠️ 可能有冗余（_working 存储覆盖值） | ✅ 更紧凑 |

### 5.2 是否移除 `_removedFromCommitted`？

**推荐方案**：保持现有设计，但重命名以提高清晰度。

原因：
1. 当前设计**符合规范条款**（Working State 无 tombstone）
2. 改变设计需要重构整个读写路径
3. 当前测试已通过，改动风险大

如果要移除，需要：
1. 将 `_working` 改为 `_current`（合并视图）
2. Remove 操作直接修改 `_current`
3. `ComputeDiff` 通过全量比较计算
4. Commit 失败时需要恢复 `_current` 到 `_committed` 的副本

### 5.3 建议的改进

1. **重命名**：`_removedFromCommitted` → `_deletedFromBase` 或 `_pendingDeletes`
2. **文档补充**：在代码注释中解释为什么需要这个集合
3. **长期优化**：考虑在 Phase 6+ 重构为单一 Working State 模型

---

## 6. 附录：关键代码路径

### 6.1 读取路径（TryGetValue）

```csharp
public bool TryGetValue(ulong key, out object? value) {
    ThrowIfDetached();
    // 1. 检查是否已从 _committed 删除
    if (_removedFromCommitted.Contains(key) && !_working.ContainsKey(key)) {
        value = default;
        return false;  // 被删除且未被重新设置
    }
    // 2. 优先查 _working
    if (_working.TryGetValue(key, out value)) { return true; }
    // 3. 回落到 _committed
    return _committed.TryGetValue(key, out value);
}
```

### 6.2 写入路径（Remove）

```csharp
public bool Remove(ulong key) {
    ThrowIfDetached();
    var hadInWorking = _working.Remove(key);
    var hasInCommitted = _committed.ContainsKey(key);

    // 标记 _committed 中的 key 为已删除
    if (hasInCommitted) {
        _removedFromCommitted.Add(key);  // ← 这就是需要这个集合的原因
    }
    // ...
}
```

### 6.3 序列化路径（WritePendingDiff）

```csharp
public void WritePendingDiff(IBufferWriter<byte> writer) {
    // ...
    foreach (var key in sortedDirtyKeys) {
        bool inWorking = _working.TryGetValue(key, out var workingValue);
        bool isRemoved = _removedFromCommitted.Contains(key);

        if (inWorking) {
            WriteValue(ref payloadWriter, key, workingValue);  // Upsert
        }
        else if (isRemoved) {
            payloadWriter.WriteTombstone(key);  // Delete → 写入 tombstone
        }
    }
}
```

---

## 7. 与监护人意见的对照

监护人说：
> 在 Load 的 Materialize 阶段就应该处理掉 Val_Tombstone 的语义

**当前实现符合这一点**：
- 加载时 `_committed` 是 Materialize 后的最终状态（假设 Materialize 正确实现）
- `_removedFromCommitted` 初始为空

监护人说：
> 进入最终暴露给外界的 DurableDict 实例里面的 `_committed` 集合应该是磁盘上的最终逻辑状态才对

**问题在于**：
- `_committed` 确实是加载时的最终状态
- 但运行时 Remove 操作不能修改它（设计约束）
- 所以需要 `_removedFromCommitted` 来追踪运行时的删除

**结论**：这不是 Load/Materialize 的问题，而是运行时状态管理策略的副产品。
