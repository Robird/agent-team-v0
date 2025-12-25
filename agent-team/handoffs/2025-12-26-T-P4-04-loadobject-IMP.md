# [T-P4-04] LoadObject 实现报告

## 实现摘要

完成了 `Workspace.LoadObject<T>()` 方法的骨架实现，实现了 Identity Map 查询逻辑和 `AteliaResult<T>` 返回类型。遵循 MVP 设计，预留了 `ObjectLoaderDelegate` 委托用于依赖注入存储层（Phase 5 实现）。

## 文件变更

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| [StateJournalError.cs](../../atelia/src/StateJournal/Core/StateJournalError.cs) | 修改 | 添加 `ObjectTypeMismatchError` |
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs) | 修改 | 添加 `ObjectLoaderDelegate`、`LoadObject<T>` 方法、新构造函数 |
| [WorkspaceTests.cs](../../atelia/tests/StateJournal.Tests/Workspace/WorkspaceTests.cs) | 修改 | 添加 8 个 LoadObject 测试用例 |

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|----------|------|------|
| `[A-LOADOBJECT-RETURN-RESULT]` | `AteliaResult<T>` 返回类型 | ✓ |
| Identity Map 命中 → 返回 | `_identityMap.TryGet()` | ✓ |
| NotFound → Failure | `ObjectNotFoundError` | ✓ |
| Type Mismatch → Failure | `ObjectTypeMismatchError` | ✓ |
| 从存储加载 → 加入 Identity Map | `_identityMap.Add(obj)` | ✓ |
| 从存储加载 → 不加入 DirtySet | Clean 状态，不调用 `_dirtySet.Add()` | ✓ |

## 测试结果

- **Targeted**: `dotnet test --filter "FullyQualifiedName~WorkspaceTests"` → 24/24 passed
- **Full**: `dotnet test tests/StateJournal.Tests` → 475/475 passed

## 新增测试用例

| 测试名称 | 覆盖场景 |
|----------|----------|
| `LoadObject_AfterCreate_ReturnsSameInstance` | Identity Map 命中 |
| `LoadObject_WrongType_ReturnsTypeMismatchError` | 类型不匹配 |
| `LoadObject_NotExists_ReturnsNotFoundError` | 对象不存在 |
| `LoadObject_FromStorage_AddsToIdentityMap` | 从存储加载 + Identity Map 缓存 |
| `LoadObject_FromStorage_DoesNotAddToDirtySet` | Clean 状态不进 DirtySet |
| `LoadObject_LoaderReturnsFailure_PropagatesError` | 错误传播 |
| `LoadObject_LoaderReturnsWrongType_ReturnsTypeMismatchError` | Loader 返回错误类型 |
| `LoadObject_MultipleLoads_ReturnsSameInstance` | 多次加载返回同一实例 |

## 设计要点

### ObjectLoaderDelegate

```csharp
public delegate AteliaResult<IDurableObject> ObjectLoaderDelegate(ulong objectId);
```

- MVP 阶段通过委托注入存储加载逻辑
- 无 loader 时（null）返回 `ObjectNotFoundError`
- Phase 5 会实现完整的 `IRbfScanner` + `VersionIndex` 查询

### 构造函数变更

```csharp
public Workspace() : this(objectLoader: null) { }
public Workspace(ObjectLoaderDelegate? objectLoader) { ... }
internal Workspace(ulong nextObjectId, ObjectLoaderDelegate? objectLoader = null) { ... }
```

- 保持无参构造函数兼容性
- 新增带 loader 的构造函数
- Recovery 构造函数扩展支持 loader

## 已知差异

无。

## 遗留问题

1. **Phase 5 待实现**：完整的存储层（`VersionIndex` → `ObjectVersionPtr` → Deserialize → Materialize）
2. **对象状态设置**：从存储加载的对象应处于 Clean/PersistentClean 状态（当前测试中 DurableDict 默认为 TransientDirty，但 LoadObject 不修改状态）

## Changefeed Anchor

`#delta-2025-12-26-T-P4-04-loadobject`
