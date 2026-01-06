# StateJournal Workspace/ObjectLoader/RBF 设计意图调查报告

> **日期**：2025-12-27
> **调查者**：Investigator
> **源文档**：
> - `atelia/docs/StateJournal/mvp-design-v2.md` (v3.8)
> - `atelia/docs/StateJournal/rbf-interface.md` (v0.10)
> - `atelia/docs/StateJournal/workspace-binding-spec.md`
> - `atelia/docs/StateJournal/backlog.md`

---

## 1. 当前设计意图：Workspace 应该如何使用？

### 1.1 核心定位

Workspace 是 StateJournal 的**核心协调器**，类比 Git 的 working tree：

> "MVP 将一次进程内的 StateJournal 视为唯一的 **workspace（Git working tree 类比）**"
> — mvp-design-v2.md §3.1.2

### 1.2 Workspace 的职责

| 职责 | 说明 | 相关条款 |
|------|------|----------|
| **对象工厂** | 创建/加载 DurableObject 实例 | `[A-WORKSPACE-FACTORY-CREATE]`, `[A-WORKSPACE-FACTORY-LOAD]` |
| **Identity Map 管理** | ObjectId → WeakReference 去重 | §3.1.2 LoadObject 语义 |
| **Dirty Set 管理** | 持有 dirty 对象的强引用，防止 GC | `[S-DIRTYSET-OBJECT-PINNING]` |
| **HEAD 追踪** | 维护当前 MetaCommitRecord | §3.3.1 Open |
| **Commit 协调** | 二阶段提交的顶层协调 | §3.4.5 CommitAll |

### 1.3 用户期望的使用方式（API 外观）

```csharp
// 打开 Workspace（唯一入口）
using var workspace = Workspace.Open("path/to/journal");

// 创建对象（通过工厂，不是 new）
var dict = workspace.CreateObject<DurableDict>();

// 加载对象
var result = workspace.LoadObject<DurableDict>(existingId);
if (result.IsSuccess) { var loaded = result.Value; }

// 修改对象（in-place API）
dict.Set(key, value);

// 提交所有 dirty 对象
workspace.CommitAll();
```

### 1.4 Workspace 绑定机制（关键设计点）

**护照模式**：每个 DurableObject MUST 绑定到且仅绑定到一个 Owning Workspace。

| 条款 | 要求 |
|------|------|
| `[S-WORKSPACE-OWNING-EXACTLY-ONE]` | 每个对象 MUST 绑定一个 Workspace |
| `[S-WORKSPACE-OWNING-IMMUTABLE]` | 绑定在生命周期内不可变 |
| `[S-WORKSPACE-CTOR-REQUIRES-WORKSPACE]` | 构造函数 MUST 接收 Workspace 参数 |
| `[S-LAZYLOAD-DISPATCH-BY-OWNER]` | Lazy Load 按 Owning Workspace 分派 |

---

## 2. ObjectLoader 的预期工作方式

### 2.1 结论：内部集成而非外部传入

**ObjectLoader 是 Workspace 的内部实现**，不是独立组件。LoadObject 的完整流程定义在 Workspace 内部：

> "LoadObject 的完整流程 = 查 Identity Map →（miss 时）按 HEAD 从 VersionIndex 找 ObjectVersionPtr → Deserialize → Materialize → 创建内存对象并挂接 ChangeSet → 放入 Identity Map"
> — mvp-design-v2.md §3.1.0

### 2.2 四阶段读取模型

规范定义了清晰的四阶段读取模型（§3.1.0）：

| 阶段 | 输入 | 输出 | 说明 |
|------|------|------|------|
| **Deserialize** | ObjectVersionPtr | 版本中间表示 | 解码字节，不创建对象 |
| **Materialize** | 版本链 | Committed State | **Shallow**：不递归加载引用对象 |
| **LoadObject** | ObjectId | DurableObject 实例 | Identity Map 去重 + 创建带 ChangeSet 的对象 |
| **ChangeSet** | — | 变更跟踪 | 隐式（双字典策略的 diff 算法） |

### 2.3 Shallow Materialization 原则

> "**Materialize is shallow**（MVP 固定）——只合成'本对象自身'的状态；遇到 `Val_ObjRef(ObjectId)` 时只保留 `ObjectId`，不创建/加载被引用对象。"
> — mvp-design-v2.md §3.1.0

**透明 Lazy Loading** (`[A-OBJREF-TRANSPARENT-LAZY-LOAD]`)：
- 读取时自动 `LoadObject(ObjectId)` 并返回实例
- 回填到 `_current` 避免重复触发

### 2.4 LoadObject 返回类型

**`[A-LOADOBJECT-RETURN-RESULT]`**：统一返回 `AteliaResult<T>` 而非 `null` 或抛异常。

| 情况 | 返回值 | ErrorCode |
|------|--------|-----------|
| ObjectId 不存在 | `Failure` | `StateJournal.ObjectNotFound` |
| 版本链解析失败 | `Failure` | `StateJournal.CorruptedRecord` |
| 加载成功 | `Success(instance)` | — |

---

## 3. RBF 的角色：Workspace 应该如何与 RBF 交互？

### 3.1 层级关系

RBF 是 StateJournal 的**下层（Layer 0）**，提供二进制帧的封装/解封装：

| 文档 | 层级 | 职责 |
|------|------|------|
| `mvp-design-v2.md` | Layer 1 (StateJournal) | Record 语义、对象模型 |
| `rbf-interface.md` | Layer 0/1 边界 | 接口契约 |
| `rbf-format.md` | Layer 0 (RBF) | 二进制帧格式 |

### 3.2 RBF 接口契约

StateJournal 通过以下接口与 RBF 交互：

**写入接口（IRbfFramer）**：
```csharp
public interface IRbfFramer
{
    <deleted-place-holder> Append(FrameTag tag, ReadOnlySpan<byte> payload);
    RbfFrameBuilder BeginFrame(FrameTag tag);
    void Flush();  // 不含 fsync
}
```

**读取接口（IRbfScanner）**：
```csharp
public interface IRbfScanner
{
    bool TryReadAt(<deleted-place-holder> address, out RbfFrame frame);
    RbfReverseEnumerable ScanReverse();
}
```

### 3.3 关键交互点

| 操作 | StateJournal 行为 | RBF 职责 |
|------|-------------------|----------|
| **写入 ObjectVersionRecord** | 构建 DiffPayload，设置 FrameTag | 封装为 Frame，追加写入 |
| **写入 MetaCommitRecord** | 构建 meta payload | 封装为 Frame，追加写入 |
| **读取对象版本** | 提供 <deleted-place-holder>（ObjectVersionPtr） | 返回 RbfFrame，包含 Payload |
| **逆向扫描 Meta** | 请求 ScanReverse() | 返回帧枚举（从尾到头） |

### 3.4 FrameTag 映射（StateJournal 定义）

RBF 不解释 FrameTag 语义。StateJournal 定义如下取值：

| FrameTag 值 | RecordType | ObjectKind | 说明 |
|-------------|------------|------------|------|
| `0x00010001` | ObjectVersion | Dict | DurableDict 版本记录 |
| `0x00000002` | MetaCommit | — | 提交元数据记录 |

### 3.5 墓碑帧处理

**`[S-STATEJOURNAL-TOMBSTONE-SKIP]`**：StateJournal Reader MUST 先检查 `FrameStatus`，遇到 `Tombstone (0xFF)` 状态 MUST 跳过该帧。

---

## 4. 相关 Backlog 条目

### 4.1 已完成的相关任务

| 任务 | 状态 | 说明 |
|------|------|------|
| B-4: DurableDict 与 Workspace 绑定方式 | ✅ | 畅谈会 #5 解决，护照模式 |
| B-6: Detached 对象成员访问语义 | ✅ | 畅谈会 #3+#4 解决 |
| L1 全量审阅 | ✅ | 4 模块 60 条款 |

### 4.2 开放待办（P1）

| 任务 | 复杂度 | 说明 |
|------|--------|------|
| **B-5** | 中 | DurableDict API 成员正式命名 |
| **B-8** | 低 | LoadObject<T> 泛型/非泛型分层——考虑拆分为非泛型底层 + 泛型转换包装 |

### 4.3 已设计待实施（P2）

| 任务 | 说明 |
|------|------|
| **B-7** | DiagnosticScope (O6)：诊断作用域，允许读取 Detached 对象最后已知值 |

---

## 5. 关键条款引用

### 5.1 Workspace 相关条款

| 条款 ID | 内容摘要 |
|---------|----------|
| `[S-WORKSPACE-OWNING-EXACTLY-ONE]` | 每个 IDurableObject MUST 绑定一个 Workspace |
| `[S-WORKSPACE-OWNING-IMMUTABLE]` | Owning Workspace 不可变 |
| `[S-WORKSPACE-CTOR-REQUIRES-WORKSPACE]` | 构造函数 MUST 接收 Workspace 参数 |
| `[A-WORKSPACE-FACTORY-CREATE]` | CreateObject<T>() 的 5 步流程 |
| `[A-WORKSPACE-FACTORY-LOAD]` | LoadObject<T>(ObjectId) 的 5 步流程 |
| `[A-WORKSPACE-AMBIENT-OPTIONAL]` | Ambient Context 是可选便利层 |

### 5.2 LoadObject/Materialize 相关条款

| 条款 ID | 内容摘要 |
|---------|----------|
| `[A-LOADOBJECT-RETURN-RESULT]` | LoadObject MUST 返回 AteliaResult<T> |
| `[A-OBJREF-TRANSPARENT-LAZY-LOAD]` | 读取时自动 LoadObject 并返回实例 |
| `[A-OBJREF-BACKFILL-CURRENT]` | Lazy Load 后回填到 _current |
| `[S-LAZYLOAD-DISPATCH-BY-OWNER]` | 按 Owning Workspace 分派 |

### 5.3 RBF 接口条款

| 条款 ID | 内容摘要 |
|---------|----------|
| `[A-RBF-FRAMER-INTERFACE]` | IRbfFramer 接口定义 |
| `[A-RBF-SCANNER-INTERFACE]` | IRbfScanner 接口定义 |
| `[S-RBF-TOMBSTONE-VISIBLE]` | Scanner MUST 产出所有帧包括 Tombstone |
| `[S-STATEJOURNAL-TOMBSTONE-SKIP]` | StateJournal MUST 忽略 Tombstone 帧 |
| `[F-FRAMETAG-DEFINITION]` | FrameTag 是 4 字节帧类型标识 |
| `[F-<deleted-place-holder>-DEFINITION]` | <deleted-place-holder> 是 8 字节文件偏移量 |

### 5.4 对象生命周期条款

| 条款 ID | 内容摘要 |
|---------|----------|
| `[S-DIRTYSET-OBJECT-PINNING]` | Dirty Set MUST 持有对象强引用 |
| `[S-NEW-OBJECT-AUTO-DIRTY]` | 新建对象 MUST 立即加入 Dirty Set |
| `[A-OBJECT-STATE-PROPERTY]` | IDurableObject MUST 暴露 State 属性 |
| `[S-STATE-TRANSITION-MATRIX]` | 状态转换 MUST 遵循矩阵 |

---

## 6. 实现建议

### 6.1 对 Implementer

1. **分层实现**：先实现 Layer 1（核心绑定），再 Layer 2（工厂），最后 Layer 3（可选 Ambient）
2. **构造函数保护**：DurableDict 构造函数 MUST 为 `internal`，用户只能通过工厂创建
3. **Identity Map**：使用 `ConditionalWeakTable` 或 `Dictionary<ObjectId, WeakReference<IDurableObject>>`
4. **Dirty Set**：使用 `Dictionary<ObjectId, IDurableObject>` 强引用

### 6.2 对 QA

1. **测试 Workspace 绑定**：验证对象无法脱离 Workspace 创建
2. **测试 Lazy Loading**：验证使用 Owning Workspace 而非 Ambient
3. **测试 Identity Map 去重**：多次 LoadObject 返回同一实例
4. **测试 Dirty Set 防 GC**：dirty 对象不会被回收

---

## 7. 待确认问题

| # | 问题 | 相关条款 | 备注 |
|---|------|----------|------|
| 1 | LoadObject<T> 是否应拆分为非泛型底层 + 泛型包装？ | B-8 | 监护人 2025-12-27 提出 |
| 2 | 多 Workspace 场景下的 ObjectId 唯一性如何保证？ | — | MVP 单进程单 Workspace |
| 3 | 是否需要 Workspace 级别的 DiscardAllChanges？ | — | 当前只有对象级 DiscardChanges |

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2025-12-27 | 初稿 |
