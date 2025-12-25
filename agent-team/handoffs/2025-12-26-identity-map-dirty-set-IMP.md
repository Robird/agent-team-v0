# [T-P4-01/02] Identity Map + Dirty Set Implementation Result

## 实现摘要

实现了 Workspace 的两个核心基础设施组件：
- **IdentityMap**：使用 `WeakReference<IDurableObject>` 缓存对象，允许 Clean 对象被 GC 回收
- **DirtySet**：使用强引用持有脏对象，防止 GC 回收

## 文件变更

- `atelia/src/StateJournal/Workspace/IdentityMap.cs` — 对象身份映射实现
- `atelia/src/StateJournal/Workspace/DirtySet.cs` — 脏对象集合实现
- `atelia/tests/StateJournal.Tests/Workspace/IdentityMapTests.cs` — 19 个测试用例
- `atelia/tests/StateJournal.Tests/Workspace/DirtySetTests.cs` — 17 个测试用例

## 规范对齐说明

| 条款 | 实现 | 备注 |
|------|------|------|
| `[S-IDENTITY-MAP-KEY-COHERENCE]` | ✅ `Add()` 使用 `obj.ObjectId` 作为 key | 两个类都遵守 |
| `[S-DIRTYSET-OBJECT-PINNING]` | ✅ `Dictionary<ulong, IDurableObject>` 强引用 | 经 GC 测试验证 |
| `[S-DIRTY-OBJECT-GC-PROHIBIT]` | ✅ 强引用阻止 GC | 测试：`Add_PreventsGC` |
| `[S-NEW-OBJECT-AUTO-DIRTY]` | 📝 接口已就绪，待 Workspace 调用 | Workspace 负责调用 `dirtySet.Add()` |

## 设计要点

### IdentityMap

1. **WeakReference 语义**：允许 Clean 对象被 GC 回收，保持内存效率
2. **TryGet 自动清理**：访问失效的 WeakReference 时自动移除条目
3. **幂等添加**：同一对象重复 `Add()` 是安全的（no-op）
4. **重复检测**：不同对象使用相同 ObjectId 时抛 `InvalidOperationException`
5. **Cleanup 方法**：可选的批量清理失效引用（定期调用优化内存）

### DirtySet

1. **强引用语义**：防止 Dirty 对象被 GC 回收
2. **幂等添加**：重复 `Add()` 是安全的（覆盖）
3. **GetAll**：返回所有脏对象用于 CommitAll
4. **Clear**：Commit 成功后清空集合

## 测试结果

- **Targeted**: `dotnet test --filter "FullyQualifiedName~Workspace"` → **36/36 ✅**
- **Full**: `dotnet test tests/StateJournal.Tests/` → **451/451 ✅**

## 已知差异

无。实现完全符合任务规范。

## 遗留问题

1. **Workspace 集成**：`[S-NEW-OBJECT-AUTO-DIRTY]` 需要在 Workspace.CreateObject() 中调用 `dirtySet.Add()`
2. **Cleanup 策略**：IdentityMap.Cleanup() 的调用时机待定（可在 Commit 后或定期调用）

## Changefeed Anchor

`#delta-2025-12-26-identity-map-dirty-set`
