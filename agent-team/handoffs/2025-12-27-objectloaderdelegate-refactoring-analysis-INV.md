# ObjectLoaderDelegate 重构影响分析

> **Investigator Brief** | 2025-12-27
> **任务**: 分析 ObjectLoaderDelegate 在 StateJournal 中的使用情况，评估重构影响范围

---

## 1. ObjectLoaderDelegate 使用点清单

### 1.1 定义位置

| 文件 | 行号 | 用法 |
|:-----|:-----|:-----|
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L14) | L14 | **委托定义**: `public delegate AteliaResult<IDurableObject> ObjectLoaderDelegate(ulong objectId)` |

### 1.2 字段声明

| 文件 | 行号 | 用法 |
|:-----|:-----|:-----|
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L34) | L34 | **私有字段**: `private readonly ObjectLoaderDelegate? _objectLoader` |

### 1.3 构造函数注入点

| 文件 | 行号 | 签名 | 说明 |
|:-----|:-----|:-----|:-----|
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L68) | L68-72 | `Workspace(ObjectLoaderDelegate? objectLoader)` | 主构造函数，接收可空 loader |
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L90) | L90-101 | `Workspace(ulong nextObjectId, ObjectLoaderDelegate? objectLoader = null)` | Recovery 构造函数，internal |
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L121) | L121-125 | `Workspace.Open(RecoveryInfo info, ObjectLoaderDelegate? objectLoader = null)` | 静态工厂方法 |

### 1.4 使用点

| 文件 | 行号 | 用法 |
|:-----|:-----|:-----|
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L70) | L70 | **构造时赋值**: `_objectLoader = objectLoader` |
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L99) | L99 | **构造时赋值**: `_objectLoader = objectLoader` |
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L199) | L199 | **实际调用**: `var loadResult = _objectLoader?.Invoke(objectId)` |

### 1.5 代码注释

| 文件 | 行号 | 说明 |
|:-----|:-----|:-----|
| [Workspace.cs](../../atelia/src/StateJournal/Workspace/Workspace.cs#L42) | L42 | **Phase 5 预告**: `// Phase 5 会添加 IRbfFramer/IRbfScanner` |

---

## 2. 测试中的 Mock 方式

### 2.1 当前测试策略

测试文件：[WorkspaceTests.cs](../../atelia/tests/StateJournal.Tests/Workspace/WorkspaceTests.cs)

**Mock 方式：Lambda 委托注入**

```csharp
// 典型用法 1: 条件返回
ObjectLoaderDelegate loader = id => id == 100
    ? AteliaResult<IDurableObject>.Success(mockObject)
    : AteliaResult<IDurableObject>.Failure(new ObjectNotFoundError(id));
using var workspace = new Workspace(loader);

// 典型用法 2: 固定返回
ObjectLoaderDelegate loader = _ =>
    AteliaResult<IDurableObject>.Success(mockObject);

// 典型用法 3: 固定失败
ObjectLoaderDelegate loader = _ =>
    AteliaResult<IDurableObject>.Failure(customError);
```

### 2.2 测试用例分布

| 测试文件 | 用法 | 测试数量 |
|:---------|:-----|:---------|
| WorkspaceTests.cs | `new Workspace()` (无 loader) | 大多数测试 |
| WorkspaceTests.cs | `new Workspace(loader)` (带 loader) | 4 个测试 |
| WorkspaceCommitTests.cs | `new Workspace()` (无 loader) | 全部 |

### 2.3 关键测试用例

1. **`LoadObject_FromStorage_AddsToIdentityMap`** — 验证 loader 返回的对象加入 IdentityMap
2. **`LoadObject_FromStorage_DoesNotAddToDirtySet`** — 验证从存储加载的对象不加入 DirtySet
3. **`LoadObject_LoaderReturnsFailure_PropagatesError`** — 验证错误传播
4. **`LoadObject_LoaderReturnsSameType_Succeeds`** — 验证类型匹配

---

## 3. IRbfScanner 实现状态

### 3.1 接口定义

文件：[IRbfScanner.cs](../../atelia/src/Rbf/IRbfScanner.cs)

```csharp
public interface IRbfScanner {
    bool TryReadAt(<deleted-place-holder> address, out RbfFrame frame);
    IEnumerable<RbfFrame> ScanReverse();
    byte[] ReadPayload(in RbfFrame frame);
}
```

### 3.2 实现状态

文件：[RbfScanner.cs](../../atelia/src/Rbf/RbfScanner.cs)

| 方法 | 实现状态 | 说明 |
|:-----|:---------|:-----|
| `TryReadAt` | ✅ 完整实现 | 支持随机读取帧，含 CRC 校验 |
| `ScanReverse` | ✅ 完整实现 | 支持从尾部逆向扫描，含 Resync |
| `ReadPayload` | ✅ 完整实现 | 返回 Payload 的字节数组拷贝 |

### 3.3 集成可行性

**RbfScanner 已可用于 Workspace 集成**，但需要以下桥接工作：

1. **Deserialize 阶段**：RbfScanner 输出 `RbfFrame`（含 PayloadOffset/PayloadLength），需要解析为 StateJournal Record
2. **Materialize 阶段**：从 Payload 反序列化为 `DurableDict` 的 `Dictionary<ulong, object?>`
3. **LoadObject 流程**：
   - 查 VersionIndex 获取 ObjectVersionPtr
   - 调用 `IRbfScanner.TryReadAt(address, out frame)`
   - 调用 Materialize 构建 DurableDict

### 3.4 缺失组件

| 组件 | 状态 | 说明 |
|:-----|:-----|:-----|
| Materializer | ❌ 未实现 | 从 Payload 字节反序列化为对象 |
| RecordReader | ❌ 未实现 | 从 RbfFrame 解析 StateJournal Record |
| IRbfFramer | ✅ 已有接口+实现 | 写入帧，但 Workspace 未集成 |

---

## 4. DurableObjectBase 与 Workspace 交互

### 4.1 交互点

文件：[DurableObjectBase.cs](../../atelia/src/StateJournal/Objects/DurableObjectBase.cs)

| 成员 | 用法 |
|:-----|:-----|
| `_owningWorkspace` | 私有字段，存储绑定的 Workspace |
| `OwningWorkspace` | protected 属性，供子类访问 |
| `LoadObject<T>(ulong id)` | protected 方法，调用 `_owningWorkspace.LoadObject<T>(id)` |

### 4.2 Lazy Load 调用链

```
DurableDict.ResolveAndBackfill(key, value)
    → DurableObjectBase.LoadObject<IDurableObject>(objectId)
        → Workspace.LoadObject<T>(objectId)
            → _objectLoader?.Invoke(objectId)  // 当前实现
```

### 4.3 关键条款

- `[S-LAZYLOAD-DISPATCH-BY-OWNER]`: Lazy Load 按 Owning Workspace 分派
- `[A-OBJREF-TRANSPARENT-LAZY-LOAD]`: 透明 Lazy Load

---

## 5. 重构影响评估

### 5.1 需要修改的文件

| 文件 | 修改类型 | 复杂度 | 说明 |
|:-----|:---------|:-------|:-----|
| `Workspace.cs` | **核心重构** | 高 | 替换 ObjectLoaderDelegate 为 IRbfScanner + Materializer |
| `DurableObjectBase.cs` | 无需修改 | - | 已通过 `_owningWorkspace.LoadObject` 解耦 |
| `DurableDict.cs` | 可能需修改 | 中 | 添加 Materialize 构造函数（已有 committed 参数版本） |
| `WorkspaceTests.cs` | **测试重构** | 中 | 4 个测试用例需要适配新的 Mock 方式 |
| `WorkspaceCommitTests.cs` | 无需修改 | - | 不涉及 LoadObject |

### 5.2 新增组件

| 组件 | 复杂度 | 说明 |
|:-----|:-------|:-----|
| `IMaterializer` | 低 | 接口定义，从 Payload 反序列化 |
| `DurableDictMaterializer` | 中 | DurableDict 的具体 Materializer |
| `RecordReader` | 中 | 从 RbfFrame 解析 StateJournal Record |

### 5.3 重构策略建议

**推荐方案: 渐进式替换**

1. **Phase 1**: 定义 `IMaterializer` 接口
   ```csharp
   public interface IMaterializer<T> where T : IDurableObject {
       T Materialize(Workspace workspace, ulong objectId, ReadOnlySpan<byte> payload);
   }
   ```

2. **Phase 2**: 添加 `IRbfScanner` 注入点（与 `ObjectLoaderDelegate` 并存）
   ```csharp
   public Workspace(IRbfScanner? scanner, IMaterializer<IDurableObject>? materializer) { ... }
   ```

3. **Phase 3**: 实现 `LoadObject` 的 Scanner 路径
   ```csharp
   // 优先使用 Scanner，fallback 到 Delegate
   if (_scanner != null && _materializer != null) { ... }
   else if (_objectLoader != null) { ... }
   ```

4. **Phase 4**: 移除 `ObjectLoaderDelegate`（待所有测试迁移后）

### 5.4 风险评估

| 风险 | 等级 | 缓解措施 |
|:-----|:-----|:---------|
| 测试中断 | 中 | 保持 ObjectLoaderDelegate 作为 fallback |
| 类型注册 | 低 | MVP 仅支持 DurableDict，无需复杂的类型注册机制 |
| 性能退化 | 低 | Scanner 已有完整实现，无额外开销 |

---

## 6. 总结

### 6.1 关键发现

1. **ObjectLoaderDelegate 使用范围有限**：仅在 Workspace.cs 中定义和使用（6 处引用）
2. **测试 Mock 方式简单**：Lambda 委托注入，迁移成本低
3. **IRbfScanner 已完整实现**：可直接用于 Workspace 集成
4. **缺失 Materializer**：需要新增反序列化组件

### 6.2 Implementer 建议

1. 先实现 `IMaterializer` 接口和 `DurableDictMaterializer`
2. 采用渐进式替换策略，避免一次性大改
3. 保持向后兼容，ObjectLoaderDelegate 作为 fallback

### 6.3 QA 建议

1. 现有 4 个 LoadObject 测试需要适配
2. 新增 IRbfScanner 集成测试
3. 新增 Materialize 路径的端到端测试
