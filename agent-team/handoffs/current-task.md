# 任务: 完成 Phase 4 全部任务（Workspace 管理）

## 元信息
- **任务 ID**: T-20251226-02 (批量任务)
- **Phase**: 4 (Workspace 管理)
- **类型**: 批量实施
- **优先级**: P0
- **预计时长**: 1-2 小时（基于 Phase 3 的 13x 效率）

---

## 背景

Phase 1-3 已完成，572 个测试全部通过！累计效率 5.5x！

现在进入 Phase 4，实现对象生命周期管理（Workspace）。

---

## 目标

完成 Phase 4 全部 5 个任务，输出到 `atelia/src/StateJournal/Workspace/`。

---

## 任务清单

| 任务 ID | 名称 | 预估 | 条款覆盖 | 验收标准 |
|---------|------|------|----------|----------|
| T-P4-01 | Identity Map | 2h | `[S-IDENTITY-MAP-KEY-COHERENCE]` | 同一 ObjectId 加载两次返回相同实例 |
| T-P4-02 | Dirty Set | 2h | `[S-DIRTYSET-OBJECT-PINNING]`, `[S-DIRTY-OBJECT-GC-PROHIBIT]` | Dirty 对象 GC.Collect() 后仍可访问 |
| T-P4-03 | CreateObject | 2h | `[S-CREATEOBJECT-IMMEDIATE-ALLOC]`, `[S-NEW-OBJECT-AUTO-DIRTY]` | ObjectId >= 16; 自动标记 Dirty |
| T-P4-04 | LoadObject | 3h | `[A-LOADOBJECT-RETURN-RESULT]` | 返回 `AteliaResult<T>`; NotFound 返回 Failure |
| T-P4-05 | LazyRef<T> | 2h | `[A-OBJREF-BACKFILL-CURRENT]` | 透明加载; 回填后不重复加载 |

**总预估**：11h

---

## 规范参考

- `atelia/docs/StateJournal/mvp-design-v2.md` §5 Workspace
- `atelia/docs/StateJournal/implementation-plan.md` Phase 4 详情

---

## 核心概念

### Workspace 架构

```
┌─────────────────────────────────────────────────────────┐
│ Workspace                                               │
├─────────────────────────────────────────────────────────┤
│ _identityMap: Dictionary<ulong, WeakReference<IDO>>     │ ← 对象缓存
│ _dirtySet: HashSet<IDurableObject>                      │ ← 脏对象强引用
│ _nextObjectId: ulong                                    │ ← ID 分配器
├─────────────────────────────────────────────────────────┤
│ CreateObject<T>() → T (TransientDirty)                  │
│ LoadObject<T>(id) → AteliaResult<T>                     │
│ CommitAll() → 写入所有脏对象                             │
└─────────────────────────────────────────────────────────┘
```

### Identity Map + Dirty Set 协作

```
CreateObject:
  1. 分配 ObjectId（>= 16，单调递增）
  2. 创建对象（TransientDirty 状态）
  3. 加入 _identityMap（WeakRef）
  4. 加入 _dirtySet（强引用，防止 GC）

LoadObject:
  1. 查 _identityMap
     → 命中且 alive → 返回
     → 未命中或 dead → 从存储加载
  2. 加入 _identityMap（WeakRef）
  3. 不加入 _dirtySet（Clean 状态）

对象变 Dirty:
  → 自动加入 _dirtySet（对象内部回调）

CommitAll:
  → 遍历 _dirtySet，写入后清空
```

### LazyRef<T> 透明加载

```csharp
public struct LazyRef<T> where T : IDurableObject
{
    private ulong _objectId;
    private T? _cached;
    
    public T Value => _cached ??= Workspace.LoadObject<T>(_objectId);
}
```

---

## 输出目录

- 源码：`atelia/src/StateJournal/Workspace/`
- 测试：`atelia/tests/StateJournal.Tests/Workspace/`

---

## 依赖关系

T-P4-01 → T-P4-02 → T-P4-03 → T-P4-04 → T-P4-05

建议按顺序实现，但 T-P4-05 可以与 T-P4-04 并行。

---

## 验收标准

| 任务 | 关键测试 |
|------|----------|
| T-P4-01 | `ReferenceEquals(Load(id), Load(id)) == true` |
| T-P4-02 | `GC.Collect(); dirtyObject.State == Dirty` |
| T-P4-03 | `Create().ObjectId >= 16`; `Create().State == TransientDirty` |
| T-P4-04 | `Load(notExist).IsSuccess == false` |
| T-P4-05 | `lazyRef.Value` 首次访问触发加载，后续访问返回缓存 |

---

## 汇报要求

完成后请汇报：
1. 各任务完成情况
2. 新增测试数（累计应 > 600）
3. 遇到的问题或设计决策（如有）

---

## 备注

Phase 4 是 Workspace 骨架，为 Phase 5 (Commit & Recovery) 做准备。

保持惊人的效率！🚀
