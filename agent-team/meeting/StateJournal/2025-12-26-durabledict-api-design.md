# 畅谈会：DurableDict API 外观设计

> **日期**：2025-12-26
> **标签**：#design
> **主持人**：刘德智 (Team Leader / StandardsChair)
> **参与者**：Advisor-Claude, Advisor-Gemini, Advisor-GPT
> **状态**：进行中

---

## 背景

### 问题来源

L1 审阅发现的 Underspecified 问题：
- **U-1**：DurableDict 泛型形式 `DurableDict<TValue>` vs 非泛型
- **U-2**：Enumerate vs Entries 命名
- **U-4**：LazyRef 与 DurableDict 集成

### 监护人观点

**关于泛型形式**：
> 在实现方面，我们的 DurableDict 确实选择了不成为类型特化容器，Value 是混杂集合。这很大程度上受 JSON 的影响，因为库的最初动机之一是对位替代 json lines。混杂类型实现简单，用起来像 duck type，比较灵活。
>
> 在易用性方面，暴露怎样的外观是个 API 设计问题。我不建议用泛型外观，因为我们许诺容器的类型特化，在序列化和反序列化时目前也不保存集合的泛型参数信息。逻辑上是个 key 是整数的 JSON Object。
>
> 而暴露 C# 外观方面，我认为可以借鉴前人在强类型语言中设计 JSON 读写库的外观设计经验，我们博采众长制定最适合我们的方案。

**关于 LazyRef 集成**：
> 对于 DurableDict，我认为不适用 LazyRef，因为 DurableDict 的内部实现如果用某种 `IDictionary<ulong, object>` 的话，就地用 Value 的槽位实现可能更自然——目标对象未 Load 之前存 ObjectId，Load 之后存 Load 出来的实例。

### 核心问题

1. DurableDict 应该暴露什么样的 C# 外观？泛型 vs 非泛型？
2. 如何借鉴 JSON 库的设计经验（Newtonsoft.Json, System.Text.Json 等）？
3. API 命名：Enumerate() vs Entries vs 其他？
4. LazyRef 的定位：独立工具类 vs DurableDict 内部实现细节？

### 相关文档

- [mvp-design-v2.md](../../../atelia/docs/StateJournal/mvp-design-v2.md)
- [mvp-boundary.md](../../../atelia/docs/StateJournal/mvp-boundary.md)
- [L1-Full-Review-Feedback.md](../../../atelia/docs/StateJournal/review/L1-Full-Review-Feedback.md)

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

各位顾问，今天我们讨论 DurableDict 的 API 外观设计。

**核心议题**：

| 维度 | 当前状态 | 待决策 |
|:-----|:---------|:-------|
| 泛型形式 | `DurableDict<TValue>` | 保留泛型 vs 改为 `DurableDict`（非泛型） |
| Value 类型 | 混杂集合（类似 JSON） | 强类型 accessor vs duck typing |
| 枚举 API | `Entries` 属性 | 命名统一 |
| LazyRef | 独立组件 | 是否集成到 DurableDict |

**参考设计**：
- **Newtonsoft.Json**：`JObject` — 非泛型，通过 `Value<T>()` 方法获取强类型值
- **System.Text.Json**：`JsonElement` — 非泛型，通过 `GetXxx()` 方法获取强类型值
- **BSON (MongoDB)**：`BsonDocument` — 非泛型，混杂集合

**讨论焦点**：

1. 我们的 DurableDict 逻辑上是"key 为整数的 JSON Object"——API 应该如何反映这一语义？
2. 泛型参数 `TValue` 在当前实现中有什么实际作用？序列化时是否保存类型信息？
3. 如果改为非泛型，强类型访问如何实现？

请各位顾问从各自视角分析。

---

### Advisor-Claude 发言

我从概念框架视角分析这个问题。这让我想到一个更根本的区分——"类型容器"与"文档容器"的本质差异。

#### 1. 泛型参数 `TValue` 的当前语义是什么？

审视当前实现 [DurableDict.cs](../../../atelia/src/StateJournal/Objects/DurableDict.cs)，`TValue` 泛型参数的实际语义是**编译期类型提示**，但：

- **序列化时不保存类型信息**：DiffPayload 只写入 `ValueType` 标签（Null/Tombstone/ObjRef/VarInt/Ptr64），不记录 `TValue` 的 CLR 类型
- **反序列化时无法恢复**：Materialize 只能根据 `ValueType` 还原基础类型（long/ObjectId 等），无法还原泛型参数
- **内部存储是 `object`**：`_committed` 和 `_working` 实际存储的是 `object`（或装箱后的值）

**类比**：这就像一个**标称类型系统中的鸭子类型**——泛型参数 `TValue` 只在编译期提供类型检查，运行时和持久化层完全无视它。

#### 2. "类型容器" vs "文档容器"的本质区别

这是一个**概念边界清晰度**问题。让我用类比来阐述：

| 维度 | 类型容器（如 `List<T>`） | 文档容器（如 `JObject`） |
|:-----|:-------------------------|:-------------------------|
| **类型承诺** | 编译期 + 运行时 | 无（duck typing） |
| **序列化保真** | 类型信息随数据持久化 | 只保存结构，不保存宿主类型 |
| **边界契约** | 违反类型约束 = 编译/运行时错误 | 访问时显式转换 |
| **心智模型** | "一个装 T 的容器" | "一个自描述的数据岛" |

**关键洞见**：

> **类型容器承诺"类型同构性"（Type Homogeneity）——所有元素属于同一类型 T。**
> **文档容器承诺"结构自描述性"（Structural Self-Description）——数据携带自己的类型标签。**

DurableDict 的实际行为是后者：
- 同一个 dict 可以混合存储 `long`、`ObjectId`、`null`
- 序列化只保存 `ValueType` 标签，不保存 CLR 类型
- 进程重启后，无法从磁盘恢复"这是一个 `DurableDict<MyEnum>`"

#### 3. DurableDict 应该属于哪一类？

**结论：DurableDict 是"文档容器"，不是"类型容器"。**

设计规范 [mvp-design-v2.md](../../../atelia/docs/StateJournal/mvp-design-v2.md) §3.1.5 已经明确：

> `DurableDict` 是 StateJournal 的**底层索引原语（Low-level Indexing Primitive）**，不是通用数据容器。

**核心类比**：

| 类比来源 | API 设计 | DurableDict 对标 |
|:---------|:---------|:-----------------|
| Newtonsoft.Json | `JObject` — 非泛型 | ✅ |
| MongoDB BSON | `BsonDocument` — 非泛型 | ✅ |
| System.Text.Json | `JsonElement` — 非泛型 | ✅ |
| C# Dictionary | `Dictionary<K,V>` — 泛型 | ❌ 不对标 |

**泛型外观的危险**：

如果保留 `DurableDict<TValue>`，用户会产生**类型同构性的心智模型预期**：
- 预期：所有 value 都是 `TValue` 类型
- 实际：value 可以是任意支持的类型，`TValue` 只是编译期提示
- 风险：进程重启后，从磁盘加载的对象**无法恢复泛型参数**，导致类型断言失败

#### 4. 非泛型设计的强类型访问方案

借鉴 JSON 库经验，推荐 **Accessor Pattern**：

```csharp
// 非泛型 DurableDict（文档容器语义）
public class DurableDict : IDurableObject {
    // 弱类型读取
    public bool TryGetValue(ulong key, out object? value);
    
    // 强类型 Accessor（借鉴 JObject.Value<T>()）
    public T? GetValue<T>(ulong key);
    public bool TryGetValue<T>(ulong key, out T? value);
    
    // 或者索引器 + 显式转换
    public object? this[ulong key] { get; set; }
}

// 使用示例
var dict = workspace.LoadObject<DurableDict>(id);
long count = dict.GetValue<long>(Keys.Count);     // 显式声明期望类型
var child = dict.GetValue<DurableDict>(Keys.Child);  // ObjRef 自动 LoadObject
```

**关键原则**：

1. **显式优于隐式**：用户在访问点声明期望类型，而非在容器声明处
2. **Fail-fast**：类型不匹配时抛明确异常，而非静默返回 default
3. **文档化边界**：API 命名和文档明确这是"文档容器"，避免类型容器误解

#### 5. 决策建议（Tradeoff 表）

| 方案 | 优点 | 缺点 | 风险等级 |
|:-----|:-----|:-----|:---------|
| **A. 改为非泛型** | 语义一致、避免类型幻觉、符合 JSON 库惯例 | 需修改 API、现有代码需调整 | 低 |
| B. 保留泛型但文档化限制 | 改动小 | 类型承诺与实际不符、用户困惑 | 中 |
| C. 实现完整类型保真 | 真正的类型容器 | 工程量大、增加复杂度 | 高 |

**我的立场**：倾向 **方案 A**。

> **设计原则**：API 外观应反映内部语义，而非掩盖它。
> "假泛型"比"无泛型"更危险——它给用户错误的安全感。

#### 6. 术语建议

如果采用非泛型方案，建议明确以下术语：

- **DurableDict**：StateJournal 的持久化字典原语（非泛型，key 固定为 ulong）
- **DurableValue**：DurableDict 中存储的值类型联合（Null | VarInt | ObjRef | Ptr64 | ...）
- **ValueType tag**：序列化时的类型判别器（对应枚举值速查表）

这与 JSON 库的术语体系类似：`JToken` / `JValue` / `JTokenType`。

---

### Advisor-Gemini 发言

我完全赞同 Claude 的"文档容器"定性。作为体验官，我将从 **Developer Experience (DX)** 和 **Affordance (示能性)** 角度进一步论证为何必须抛弃泛型外观。

#### 1. 虚假示能 (False Affordance) 的代价

在 HCI 中，**示能性**指物体属性向用户传达的操作可能。
- **门把手**暗示"拉"或"推"。
- **泛型 `DurableDict<T>`** 暗示"类型安全"和"同构集合"。

当我们提供 `DurableDict<MyEnum>` 时，我们向开发者许下了一个**无法兑现的承诺**：
1.  **承诺**：取出的值永远是 `MyEnum`。
2.  **现实**：磁盘上可能存着 `long` (VarInt)，或者另一个进程写入的 `int`。
3.  **后果**：开发者省略了防御性检查，直到运行时崩溃。

> **DX 原则**：API 签名必须诚实。与其提供一个"漏水的抽象"（Leaky Abstraction），不如提供一个"诚实的底层"（Honest Primitive）。

#### 2. 现有 JSON 库的 DX 标杆

开发者对"文档容器"的心智模型早已被 JSON 库确立。让我们看看标杆是如何设计的：

| 库 | 容器类型 | 访问模式 | DX 感受 |
|:---|:---------|:---------|:--------|
| **Newtonsoft.Json** | `JObject` (非泛型) | `obj["key"].Value<int>()` | **灵活 (Dynamic)**。感觉像在操作 JS 对象，容错性高。 |
| **System.Text.Json** | `JsonElement` (Struct) | `elem.GetProperty("key").GetInt32()` | **严谨 (Strict)**。明确的动词暗示了转换成本。 |
| **MongoDB** | `BsonDocument` | `doc["key"].AsInt32` | **直接 (Direct)**。强调数据类型的转换。 |

**共同点**：它们都选择了 **"无类型容器 + 强类型访问器" (Untyped Container + Typed Accessor)** 模式。
这不仅仅是实现限制，更是一种设计选择——它迫使开发者在**读取时刻 (Read Time)** 显式处理类型预期，而不是在**定义时刻 (Definition Time)** 假装类型确定。

#### 3. 迁移体验：从"约束"到"解放"

从 `DurableDict<T>` 迁移到非泛型 `DurableDict`，初看是退步（失去了 T），实则是解放。

**Before (泛型困境)**:
```csharp
// 开发者被迫定义一个"最大公约数"类型，通常是 object
var dict = new DurableDict<object>(); 
dict[1] = 100L;
// 必须显式强转，泛型毫无价值
long val = (long)dict[1]; 
```

**After (流式访问)**:
```csharp
var dict = new DurableDict();
dict.Set(1, 100);
// 意图清晰，代码更短
long val = dict.Get<long>(1); 
```

#### 4. DX 设计建议：Fluent Accessor Pattern

为了弥补失去泛型的"不安全感"，我们需要在 API 层面提供极佳的**人体工学 (Ergonomics)**。

建议引入一组 **Fluent Extension Methods**，让非泛型 API 用起来比泛型更顺手：

**A. 核心访问器 (The "Get" Family)**

不要叫 `GetValue<T>` (太啰嗦)，直接叫 `Get<T>`。

```csharp
// 基础读取 (Throw if missing/type-mismatch)
long id = dict.Get<long>(Keys.Id);

// 安全读取 (Option/Nullable)
long? maybeId = dict.TryGet<long>(Keys.Id);

// 默认值读取 (Pit of Success)
long count = dict.Get(Keys.Count, defaultValue: 0);
```

**B. 隐形桥梁：LazyRef 集成**

这是非泛型设计的最大红利。
在泛型版中，`DurableDict<LazyRef<T>>` 写起来非常痛苦。
在非泛型版中，我们可以把 `LazyRef` 隐藏在 `Get<T>` 内部：

```csharp
// 假设 dict 内部存的是 ObjectId (Val_ObjRef)
// 开发者只需请求他想要的类型：
var child = dict.Get<DurableDict>(Keys.Child);

// 内部实现自动识别：
// 1. 发现存储的是 ObjRef
// 2. 发现用户请求的是 IDurableObject
// 3. 自动调用 LoadObject
// 4. 返回实例
```

这创造了一种 **"Invisible Bridge" (隐形桥梁)** 体验——开发者感觉不到磁盘 I/O 和对象加载的存在，只有流畅的对象图遍历。

#### 5. 总结

我强烈支持 **方案 A (改为非泛型)**，并补充以下 DX 建议：

1.  **命名**：使用 `Get<T>()` / `Set()` 动词，强化"操作"语义，弱化"集合"语义。
2.  **集成**：将 `LoadObject` 逻辑封装在 `Get<IDurableObject>()` 中，实现透明加载。
3.  **示能**：API 签名应明确拒绝不支持的类型（如 `Set(key, complexObject)` 应在编译期或运行时快速失败），而不是默默接受然后序列化失败。

---

### Advisor-GPT 发言

从“规范核查 / 可判定性”视角，我建议把本议题收敛成两层契约：

1) **容器本体（DurableDict）**：不持有 Workspace；只承诺“持久化文档容器”的最小可测语义（Try-pattern、Detached 行为、枚举一致性）。
2) **访问器（Accessor / View）**：显式绑定 Workspace（或 ObjectLoader），承诺“透明 Lazy Load / LazyRef 生成 / 强类型读取”的行为。

这能同时解决当前三处漂移：
- 规范历史版本出现过“非泛型 + AteliaResult + Enumerate”，而当前实现与现行正文采用“泛型 + bool/out + Entries”。
- ObjRef 与 Ptr64 在 payload 层是不同 ValueType，但在 CLR 层如果都落到 `ulong`，会造成**语义不可判定**（无法从运行时类型区分）。
- LazyRef 需要 Workspace；DurableDict 本体不应把 Workspace 作为字段（否则对象图与生命周期会耦合、也破坏测试纯度）。

下面给出可落地的 **非泛型 API 签名草案**、**命名规范**、**LazyRef 集成方案**与**迁移检查清单**。

---

## 1) 非泛型 API 签名草案（Normative Draft）

### 1.1 `DurableDict`（容器本体：无 Workspace）

设计目标：
- 对齐 §3.4.3 当前“bool + out”Try-pattern 的可判定性（只表达“是否存在”，Detached 走异常）；
- 把“枚举/键集/计数”统一为 .NET 常规命名，消除 `Enumerate vs Entries` 歧义；
- 对 value 的 CLR 表示做最低限度的“可判定约束”，避免 ObjRef/Ptr64 混成 `ulong`。

```csharp
namespace Atelia.StateJournal;

// 可判定的 ObjRef 表示：避免与 Ptr64/VarInt 混淆
public readonly record struct ObjRef(ulong ObjectId);

public sealed class DurableDict : IDurableObject, IEnumerable<KeyValuePair<ulong, object?>> {
        // === IDurableObject ===
        public ulong ObjectId { get; }
        public DurableObjectState State { get; }
        public bool HasChanges { get; }

        // === Read API (Detached 时 MUST throw ObjectDetachedException) ===
        public bool TryGetValue(ulong key, out object? value);
        public bool ContainsKey(ulong key);
        public int Count { get; }

        // 枚举：统一语义名为 Entries/Keys/Values
        public IEnumerable<KeyValuePair<ulong, object?>> Entries { get; }
        public IEnumerable<ulong> Keys { get; }
        public IEnumerable<object?> Values { get; }

        // 索引器：缺失键抛 KeyNotFoundException（Detached 仍 throw ObjectDetachedException）
        public object? this[ulong key] { get; set; }

        // === Write API (Detached 时 MUST throw ObjectDetachedException) ===
        // Set 必须执行 value 验证：仅允许 MVP 支持域（null / long / Ptr64 / ObjRef / IDurableObject）
        public void Set(ulong key, object? value);
        public bool Remove(ulong key);

        // === Lifecycle ===
        // Detached 时 no-op（幂等）
        public void DiscardChanges();

        // === IEnumerable ===
        public IEnumerator<KeyValuePair<ulong, object?>> GetEnumerator();
}
```

**值域约束（MUST）**

为保证“持久化编码可判定”，`Set(key, value)` 的允许值域必须明确：
- `null` → `ValueType.Null`
- `long` / `int` → `ValueType.VarInt`（写入时可统一提升为 long）
- `Ptr64`（强烈建议使用别名类型而非裸 `ulong`）→ `ValueType.Ptr64`
- `ObjRef` → `ValueType.ObjRef`
- `IDurableObject` → 编码为 `ValueType.ObjRef(ObjectId)`（**持久化时只写 ObjectId，不写对象内容**）

并且：
- **禁止**把 `ulong` 作为通用 value 直接接受（否则 ObjRef/Ptr64 在运行时不可区分）。如需兼容旧代码，可只在内部/obsolete API 中暂存 `ulong`，并明确它只表示 Ptr64。

> 这一条是“核查硬钉”：否则未来加入 ObjRef 后，运行时语义会出现 silent corruption 风险（同样的 `ulong` 无法决定走 LoadObject 还是当作地址）。

---

### 1.2 `DurableDictAccessor`（访问器：绑定 Workspace，提供强类型 + LazyRef）

访问器承诺两类能力：
- **强类型读取**：把“类型不匹配/不存在/Detached”变成可分派的结果；
- **透明 Lazy Load**：当 value 是 ObjRef 时，允许按需 LoadObject 并回填（缓存层面）。

```csharp
namespace Atelia.StateJournal;

public readonly struct DurableDictAccessor {
        private readonly DurableDict _dict;
        private readonly Workspace _workspace;

        public DurableDictAccessor(DurableDict dict, Workspace workspace);

        // 1) 强类型读取：不抛异常（除非编程错误，比如 workspace 为 null）
        public AteliaResult<T> TryGet<T>(ulong key);

        // 2) ObjRef 透明加载：只有当底层值为 ObjRef/IDurableObject 时才适用
        public AteliaResult<T> TryLoadObject<T>(ulong key) where T : class, IDurableObject;

        // 3) LazyRef 桥接：让调用方可选择“立即加载”或“延迟加载”
        public AteliaResult<LazyRef<T>> TryGetLazyRef<T>(ulong key) where T : class, IDurableObject;
}

public static class DurableDictAccessorExtensions {
        public static DurableDictAccessor Access(this DurableDict dict, Workspace workspace)
                => new DurableDictAccessor(dict, workspace);
}
```

**透明 Lazy Load 条款化建议（MUST）**

把 `[A-OBJREF-TRANSPARENT-LAZY-LOAD]` 的适用范围从“DurableDict 本体”改为“Accessor 层”，写成可判定条款：
- 仅当 `Accessor.TryLoadObject<T>(key)` / `Accessor.TryGetLazyRef<T>(key)` 被调用时触发加载；
- `DurableDict.TryGetValue` / `Entries` / `this[key]` **不得**隐式 I/O（容器本体保持纯）；
- Accessor 发生加载成功后，可以（SHOULD）将该 key 的当前值在内存中回填为对象实例（并不改变持久化编码语义），以复用 Workspace IdentityMap。

这与 LazyRef 的两个既有条款可直接对齐：
- `[A-OBJREF-TRANSPARENT-LAZY-LOAD]`：Accessor 返回对象实例
- `[A-OBJREF-BACKFILL-CURRENT]`：加载成功后回填缓存（dict 槽位或 LazyRef 内部 `_storage`）

---

## 2) 命名规范（Enumerate vs Entries vs Keys/Values）

建议把命名“受控词表化”，避免同义 API 并存：

**MUST**
- 键值对枚举统一叫 `Entries`（property），类型 `IEnumerable<KeyValuePair<ulong, object?>>`。
- 键集合统一叫 `Keys`；值集合统一叫 `Values`。

**MUST NOT**
- 不新增 `Enumerate()`/`EnumerateEntries()` 作为同义方法。

**兼容例外（SHOULD，迁移期）**
- 如果历史代码/文档已有 `Enumerate()`，可以保留为 `[Obsolete]` 过渡别名：
    - `Enumerate()` 的实现必须等价于 `Entries`（返回同一序列语义），并在文档中声明最终会移除。

理由（核查角度）：
- `Entries/Keys/Values` 是 .NET 集合语义最稳定的一组名词；
- `Enumerate` 动词在 API surface 上容易引入“是否 lazy、是否 snapshot、是否有副作用（I/O）”的争议。

---

## 3) 与现有泛型版本的迁移路径（兼容 + 渐进）

我建议迁移以“兼容外壳 + 渐进替换”为主，避免一次性大爆炸改动。

### 3.1 代码迁移策略（建议两阶段）

**Phase 1：并存（不破坏现有调用方）**
- 新增 `DurableDict`（非泛型）与 `ObjRef`。
- 保留现有 `DurableDict<TValue>`，但将其定位为：
    - （A）内部实现可复用 `DurableDict`；或
    - （B）保持原样，但标注 `[Obsolete("Use DurableDict + Accessor")]`，并提供自动迁移提示。
- 新增 `DurableDictAccessor` 与 `Access(workspace)`。

**Phase 2：收口（逐步移除“假泛型”）**
- 将规范中的 `[A-DURABLEDICT-API-SIGNATURES]` SSOT 收敛到非泛型版本；
- 将实现与测试的默认路径切换到 `DurableDict`；
- 允许 `DurableDict<TValue>` 继续存在一段时间，但只作为薄包装（typed helpers），不再作为“核心持久化原语”。

### 3.2 调用点迁移示例（机械替换）

```csharp
// Before
var dict = new DurableDict<long?>(id);
dict.TryGetValue(k, out var v);

// After
var dict = new DurableDict(id);
dict.TryGetValue(k, out var vObj);
var v = dict.Access(ws).TryGet<long>(k);
```

---

## 4) 迁移检查清单（可用于 PR Checklist）

- [ ] 规范：`[A-DURABLEDICT-API-SIGNATURES]` 只保留一套 SSOT（非泛型），并明确 `Entries/Keys/Values` 命名。
- [ ] 规范：把 `[A-OBJREF-TRANSPARENT-LAZY-LOAD]` 的适用对象从“DurableDict 本体”改为“Accessor/Workspace 绑定视图”。
- [ ] 代码：新增 `ObjRef`（或等价表示）以区分 ObjRef 与 Ptr64，避免 `ulong` 二义性。
- [ ] 代码：`DurableDict.Set` 对 value 做白名单验证；不支持类型必须 fail-fast（`NotSupportedException` 或 `AteliaResult`，二选一但要全局一致）。
- [ ] 代码：提供 `dict.Access(workspace)`，并在其中实现 `TryLoadObject<T>` / `TryGetLazyRef<T>`。
- [ ] 测试：新增覆盖向量——`ObjRef` roundtrip + Accessor 透明加载 + 回填缓存不重复加载（复用现有 `LazyRefTests` 的 loadCount 模式）。
- [ ] 兼容：若保留 `Enumerate()`，必须标记 `[Obsolete]` 且语义严格等价 `Entries`。

---

> 核查结论一句话：**把“容器纯度（无 I/O）”与“访问器可 I/O（透明 lazy load）”分层，是本议题最小可判定、最小迁移风险的收敛点**。
---

### 主持人总结 (刘德智)

感谢三位顾问的深度分析！本次畅谈会形成了高度一致的共识。

#### 已达成共识

| 议题 | 共识 | 支持者 |
|:-----|:-----|:-------|
| 泛型 vs 非泛型 | 改为非泛型 `DurableDict` | Claude ✓ Gemini ✓ GPT ✓ |
| 设计定位 | "文档容器"而非"类型容器" | Claude ✓ Gemini ✓ GPT ✓ |
| 枚举 API 命名 | 统一为 `Entries/Keys/Values` | GPT ✓ |
| LazyRef 集成 | 通过 Accessor 层实现，容器本体保持纯净 | GPT ✓（监护人原意对齐） |
| ObjRef 表示 | 引入 `ObjRef` 类型，避免与 `ulong` 混淆 | GPT ✓ |

#### 核心洞见汇总

| 顾问 | 核心贡献 |
|:-----|:---------|
| **Claude** | "类型容器 vs 文档容器"概念框架；"假泛型比无泛型更危险" |
| **Gemini** | "虚假示能 (False Affordance)"；"隐形桥梁"Lazy Load 体验设计 |
| **GPT** | 两层架构（容器本体 + Accessor）；完整 API 签名草案；迁移检查清单 |

#### 设计方案摘要

**两层架构**：

1. **DurableDict（容器本体）**：
   - 非泛型
   - 无 Workspace 依赖
   - 只承诺持久化文档容器语义
   - API：`TryGetValue(out object?)`, `Entries`, `Keys`, `Values`

2. **DurableDictAccessor（访问器）**：
   - 绑定 Workspace
   - 提供强类型读取：`TryGet<T>()`
   - 提供透明 Lazy Load：`TryLoadObject<T>()`
   - 提供 LazyRef 桥接：`TryGetLazyRef<T>()`

**关键约束**：
- 引入 `ObjRef` 类型，避免 `ulong` 语义不可判定
- `Set()` 对 value 做白名单验证

#### 后续行动

| # | 行动 | 优先级 | 状态 |
|:-:|:-----|:------:|:----:|
| 1 | 更新 mvp-design-v2.md `[A-DURABLEDICT-API-SIGNATURES]` | P1 | ⏳ 待监护人批准 |
| 2 | 实现非泛型 `DurableDict` | P2 | ⏳ |
| 3 | 实现 `DurableDictAccessor` | P2 | ⏳ |
| 4 | 引入 `ObjRef` 类型 | P2 | ⏳ |
| 5 | 迁移现有泛型使用处 | P3 | ⏳ |

#### 监护人决策点

本次畅谈会达成了三位顾问的一致共识。提交监护人审批：

> **提案**：采纳 GPT 的两层架构设计，将 DurableDict 改为非泛型，通过 Accessor 层实现强类型访问和透明 Lazy Load。
>
> - [ ] 批准
> - [X] 需要修订
> - [ ] 否决

---

**畅谈会状态**：✅ 完成
**完成时间**：2025-12-26

## AI Team 监护人--刘世超的第二轮发言：
### 赞成以下内容，建议直接安排Specialist实施`mvp-desing-md`文档修订和相应的代码修订。
  - 改为非泛型 `DurableDict`
  - "文档容器"而非"类型容器"
  - 为引用其他DurableObject引入专用类型，避免与 `ulong` 混淆 。但是命名上我不建议用`ObjRef`，因为设计文档中已经有`ObjectId`这一概念了，我推荐在实现层也使用同名。
  - `DurableDict`中不使用`LazyRef`

### 反对以下内容，认为还未彻底解决，建议写入backlog再安排后续畅谈会解决。
  - 反对现在就定下成员正式命名。我认为应该先在实施前面共识的基础上，决定应最小实现哪些接口，成员名字自然就有了。
  - 反对现在就确定两层架构设计，因为把DurableObject与Workspace分离还需要处理跨Workspace迁移问题，无Workspace的`DurableDict`几乎无法单独使用。需要进一步探讨。我个人还是倾向于不新建`DurableDictAccessor`而是直接让`DurableDict`与Workspace绑定并提供易用性Accessor。总之，我认为这还是一个开放问题。

---

### 主持人回应 (刘德智)

收到监护人的第二轮反馈，我将：

**立即实施**（已批准）：
1. 非泛型 `DurableDict` 改造
2. 引入 `ObjectId` 类型（而非 `ObjRef`）避免 `ulong` 语义混淆
3. 不在 `DurableDict` 中使用 `LazyRef`

**写入 backlog**（待后续畅谈会）：
1. DurableDict 与 Workspace 的绑定方式（两层架构 vs 直接绑定）
2. API 成员正式命名（先实施基础再定名字）

---

**畅谈会最终状态**：✅ 部分批准
**批准时间**：2025-12-26