# 畅谈会：DurableObject 与 Workspace 绑定机制

> **日期**：2025-12-27
> **形式**：畅谈会 (Jam Session)
> **标签**：#design
> **主持人**：刘德智 (Team Leader / StandardsChair)
> **参与者**：Advisor-Claude, Advisor-Gemini, Advisor-GPT
> **状态**：进行中
> **前置**：
> - [畅谈会 #2: DurableDict API](StateJournal/2025-12-26-durabledict-api-design.md) (B-4 开放问题)
> - [畅谈会 #4: 诊断作用域](StateJournal/2025-12-26-diagnostic-scope.md)

---

## 背景

### 问题来源

在重构 `DurableDict` 以符合设计文档的双字典策略后，发现遗漏了 **§3.1.3 Lazy Loading** 的关键要求：

> **[A-OBJREF-TRANSPARENT-LAZY-LOAD]**：当 `TryGetValue`/索引器/枚举读取 value 且内部存储为 `ObjectId` 时，
> MUST 自动调用 `LoadObject(ObjectId)` 并返回 `IDurableObject` 实例。

**核心矛盾**：`DurableDict` 需要调用 `LoadObject`，但它没有持有 `IWorkspace` 引用。

### 隐含假设

设计文档的 Lazy Loading 需求**暗含**了每个非 Detached 的 DurableObject 都与一个 Workspace 实例绑定。
但这个绑定机制没有在设计文档中明确定义。

### 编程语言类比

监护人提出了一个深刻的类比：

> 我们 `new` 一个实例时可没填入 Heap 参数来指定在哪个堆上创建，而是隐式地绑定进程内唯一的一个堆。

这引出了核心权衡：**显式依赖** vs **隐式上下文** 的选择。

---

## 候选方案

### 方案 A：构造时注入

```csharp
// 公开构造函数需要 Workspace
public DurableDict(Workspace workspace, ulong objectId) {
    _workspace = workspace;
    // ...
}

// 或者通过工厂方法
var dict = workspace.CreateObject<DurableDict>();
```

| 优点 | 缺点 |
|:-----|:-----|
| ✅ 显式依赖，易审计 | ❌ 每个对象存储 Workspace 引用（内存开销） |
| ✅ 易于测试（Mock Workspace） | ❌ API 更复杂（用户需要传 Workspace） |
| ✅ 支持多 Workspace | ❌ 对象无法在 Workspace 间迁移 |

### 方案 B：静态单例（进程唯一 Workspace）

```csharp
// 全局单例
public static class StateJournal {
    public static Workspace Current { get; set; }
}

// DurableDict 内部
private Workspace Workspace => StateJournal.Current;
```

| 优点 | 缺点 |
|:-----|:-----|
| ✅ API 最简洁 | ❌ 无法操作多个 Workspace |
| ✅ 无需存储引用（节省内存） | ❌ 单元测试困难（全局状态） |
| ✅ 类似 GC 堆的隐式绑定 | ❌ 并发/异步场景上下文可能丢失 |

### 方案 C：延迟绑定（先构造后 Attach）

```csharp
// 构造时不绑定
public DurableDict(ulong objectId) { ... }

// 由 Workspace 在返回前绑定
internal void AttachWorkspace(Workspace workspace) {
    _workspace = workspace;
}
```

| 优点 | 缺点 |
|:-----|:-----|
| ✅ 公开 API 简洁 | ❌ 有"未绑定"的危险时间窗口 |
| ✅ 可以先创建后绑定 | ❌ 需要严格保证"对外可见时已绑定" |
| ✅ 灵活性较高 | ❌ 额外的状态检查逻辑 |

---

## 相关设计问题

### 1. 接口 vs 抽象基类

当前 `IDurableObject` 是接口。如果选择方案 A 或 C，可能需要改为**抽象基类**以：
- 提供共享的 `_workspace` 字段
- 实现通用的 Lazy Loading 逻辑
- 减少每个派生类的重复代码

### 2. ObjectId 类型澄清

畅谈会 #2 后已引入 `ObjectId` record struct（而非 GPT 当时提议的 `ObjRef`）：

```csharp
public readonly record struct ObjectId(ulong Value);
```

这解决了"ulong 值域碰撞"问题——`_current` 中存储的 `ObjectId` 与普通 `ulong` 业务值可以通过类型区分。

### 3. ObjRef 等价判定

`UpdateDirtyKey` 需要正确判断 `ObjectId` 和已加载实例的等价性：
- `_committed[key] = ObjectId(42)` 
- `_current[key] = <实例，其 ObjectId = 42>`
- 这两者**语义等价**，不应进入 dirty

---

## 讨论主题

1. **绑定机制选择**：A/B/C 哪个最适合 StateJournal？
2. **接口 vs 抽象基类**：`IDurableObject` 应该改为抽象基类吗？
3. **多 Workspace 需求**：MVP 是否需要支持？
4. **测试策略**：如何在没有真实 Workspace 的情况下测试 DurableDict？

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

各位顾问，这是一个**架构级设计问题**，将决定 StateJournal 的核心 API 形态。

**问题本质**：DurableObject 如何获取 Workspace 引用？

这个问题类似于编程语言中的"对象在哪个堆上分配"——大多数语言选择了隐式绑定（进程唯一堆），
但我们的场景可能需要多 Workspace 支持。

**讨论流程**：

1. **Round 1**：三位顾问分别从自己的视角分析三个方案
   - Claude：概念框架、系统类比
   - Gemini：DX、API 设计
   - GPT：规范严谨性、可测试性

2. **Round 2**：交叉回应，针对分歧点深入讨论

3. **Round 3**：收敛共识，形成推荐方案

**期望产出**：
- 推荐的绑定机制（A/B/C 或混合方案）
- 接口 vs 抽象基类的决策
- MVP 范围界定（是否支持多 Workspace）

让我们开始！

---

### Advisor-Claude 发言 (Round 1)

让我从概念框架视角分析这个问题，为后续讨论建立心智模型。

#### 1. 问题本质：这是什么？

如果我们退一步看，这个问题的本质是：

> **对象如何获取其存在语境（Existential Context）？**

监护人提出的 Heap 类比非常精准。让我进一步展开这个类比：

| 维度 | 进程堆 (Heap) | StateJournal Workspace |
|:-----|:--------------|:-----------------------|
| **存在语境** | 进程地址空间 | Workspace 实例 |
| **对象身份** | 内存地址 | ObjectId |
| **获取语境** | 隐式（编译器/运行时注入） | **待定（三个方案）** |
| **多语境** | 多进程 = 多 Heap | 多 Workspace = ？ |

关键洞见：**编程语言的 Heap 绑定之所以能隐式，是因为它发生在编译/运行时层面，对用户不可见**。而我们的 Workspace 绑定发生在**库层面**，用户可见。

#### 2. 系统类比：三个参照系

##### 类比 A：编程语言的分配器 (Allocator)

```
Rust Arena<'a> / C++ Allocator
- 显式传递分配器
- 对象"知道"自己属于哪个 Arena
- 类似方案 A（构造时注入）
```

##### 类比 B：隐式全局上下文

```
Java GC / JavaScript VM
- 进程唯一的堆
- 对象无需持有 Heap 引用
- 类似方案 B（静态单例）
```

##### 类比 C：延迟绑定/懒初始化

```
Entity Framework DbContext / ORM Attach
- 对象可以先存在于"游离态"
- 稍后 Attach 到特定上下文
- 类似方案 C（延迟绑定）
```

#### 3. 概念自洽性评估

让我用"概念边界清晰度"这个维度来评估三个方案：

| 方案 | 概念自洽性 | 主要张力 |
|:-----|:-----------|:---------|
| **A 构造注入** | ⭐⭐⭐⭐ 高 | 对象-容器关系显式，但 API 繁琐 |
| **B 静态单例** | ⭐⭐⭐ 中 | 概念简单，但隐藏了"单 Workspace"假设 |
| **C 延迟绑定** | ⭐⭐ 低 | 引入"未绑定"中间态，需额外状态管理 |

**为什么 C 的自洽性较低？**

方案 C 引入了一个新的对象状态：`Unbound`（未绑定）。对照设计文档 §3.1.0.1 的状态机：

```
当前状态：Clean | PersistentDirty | TransientDirty | Detached

方案 C 引入：Unbound → (Attach) → Clean/TransientDirty
```

这打破了现有状态机的封闭性——需要重新审视所有状态转换。

#### 4. 每个方案的隐含假设

| 方案 | 隐含假设 |
|:-----|:---------|
| **A** | 用户总是能获取 Workspace 引用；对象生命周期由用户管理 |
| **B** | 进程内只有一个 Workspace；或 Agent 不需要同时操作多个 |
| **C** | 对象可以在"无归属"状态存在；绑定点是可控的 |

**关键问题**：这些假设与 StateJournal 的目标用户（LLM Agent）的使用场景匹配吗？

**我的初步判断**：对于 MVP 而言，**方案 B 的假设最符合实际**——

> Agent 通常操作**单一状态空间**（类似单进程）。多 Workspace 更像是"多个独立 Agent"的场景，而非"单 Agent 同时操作多个 Workspace"。

#### 5. 接口 vs 抽象基类

从类型设计角度，我倾向于引入**抽象基类**，原因如下：

**当前 `IDurableObject` 接口**：
- 定义了契约（`State`, `Id`, `HasChanges` 等）
- 但无法共享实现

**如果采用方案 A 或 C**：
- 每个派生类都需要：存储 `_workspace` 字段、实现 Lazy Loading 逻辑
- 这是**结构性重复**，适合用基类消除

**建议的类型层次**：

```csharp
interface IDurableObject          // 对外契约
abstract class DurableObjectBase  // 实现共享（Workspace 绑定、Lazy Loading）
    : IDurableObject
class DurableDict : DurableObjectBase
class DurableArray : DurableObjectBase  // 未来
```

> **Pit of Success 原则**：抽象基类让"做对的事"（正确实现 Lazy Loading）比"做错的事"更容易。

#### 6. 我的初步倾向

**MVP 推荐：方案 B（静态单例）+ 演进预留**

理由：
1. **符合 MVP 原则**——文档明确声明"单进程单 writer"
2. **概念简单**——不引入额外状态
3. **类似 GC 堆**——这个心智模型对开发者最熟悉

**演进预留**：
- 将 `Workspace.Current` 的访问封装为可替换的 Provider
- 未来如需多 Workspace，可改为 `AsyncLocal<Workspace>` 或构造注入

#### 7. 留给 Gemini/GPT 的问题

1. **DX 视角**（@Gemini）：方案 B 的单例在单元测试时如何 Mock？有无 DX 更好的变体？
2. **规范视角**（@GPT）：方案 B 是否与现有条款冲突？`[A-OBJREF-TRANSPARENT-LAZY-LOAD]` 的实现是否依赖特定绑定机制？

---

### Advisor-Gemini 发言 (Round 1)

## 1. DX 摩擦力分析 (Friction Analysis)

我从**开发者体验 (DX)** 的角度，特别是"代码编写的流畅度"和"心智负担"来评估这三个方案。

### 方案 A：构造注入 (The "Key-Card" Model)
> *隐喻：就像在公司里，进每个房间都需要刷卡。安全，但繁琐。*

- **DX 评分**: ⭐⭐
- **痛点**: **参数穿透 (Parameter Drilling)**。
  - 开发者必须在所有层级传递 `workspace` 引用，即使中间层并不关心它。
  - `new DurableDict(workspace, id)` 打断了思维流——我只想创建一个对象，却被迫思考它属于谁。
- **优点**: **Intellisense 友好**。
  - 如果使用工厂模式 `workspace.Create<DurableDict>()`，IDE 的自动补全能引导开发者发现可用对象。

### 方案 B：静态单例 (The "Gravity" Model)
> *隐喻：就像重力。你不需要随身携带"重力发生器"，它无处不在。*

- **DX 评分**: ⭐⭐⭐⭐⭐ (编写时) / ⭐ (测试时)
- **爽点**: **零摩擦**。
  - `new DurableDict(id)` 符合直觉。就像 `new List<T>()` 一样自然。
  - 它隐藏了"对象必须依附于空间"的复杂性，符合"被动安全"原则。
- **风险**: **隐形依赖**。
  - 代码看起来是独立的，实际上耦合了全局状态。

### 方案 C：AsyncLocal (The "Inception" Model)
> *隐喻：盗梦空间。每一层梦境都有自己的物理法则，互不干扰。*

- **DX 评分**: ⭐⭐⭐⭐
- **特点**: **上下文隔离**。
  - 看起来像方案 B（API 简洁），但行为像方案 A（实例隔离）。
  - 完美解决了方案 B 的并发测试问题。

---

## 2. 回应 Claude：静态单例的 Mock 策略

Claude 问到了方案 B 的致命弱点：**如何测试？**

如果直接使用 `static Workspace Current` 字段，并行测试会相互干扰（Race Condition）。
我的建议是：**采用方案 B 的 API 外观，但使用方案 C 的实现内核。**

这就是 .NET 中经典的 **Ambient Context Pattern (环境上下文模式)**。

### 核心设计：`WorkspaceScope`

我们不直接暴露可写的静态属性，而是提供一个 `IDisposable` 的作用域控制器。

```csharp
// 1. 基础设施：环境上下文
public class StateJournalContext
{
    private static readonly AsyncLocal<Workspace?> _current = new();

    // 对外暴露的只读属性 (API 像方案 B)
    public static Workspace Current => _current.Value 
        ?? throw new InvalidOperationException("No active StateJournal Workspace. Wrap your code in a WorkspaceScope.");

    // 内部设置器
    internal static void Set(Workspace? workspace) => _current.Value = workspace;
}

// 2. 测试/运行时的作用域控制
public readonly struct WorkspaceScope : IDisposable
{
    private readonly Workspace? _previous;

    public WorkspaceScope(Workspace workspace)
    {
        _previous = StateJournalContext.Current; // 支持嵌套
        StateJournalContext.Set(workspace);
    }

    public void Dispose()
    {
        StateJournalContext.Set(_previous); // 恢复现场
    }
}
```

### 测试代码示例

这样，单元测试就变得非常优雅且安全（支持并行运行）：

```csharp
[Fact]
public void Test_DurableDict_LazyLoad()
{
    // Arrange
    var mockWs = new Mock<Workspace>();
    mockWs.Setup(...);

    // Act & Assert
    // 这里的 Scope 只影响当前异步控制流，不影响其他并行测试
    using (new WorkspaceScope(mockWs.Object)) 
    {
        // DX 依然保持方案 B 的简洁：不需要传 workspace
        var dict = new DurableDict(objectId: 42); 
        
        // 内部自动调用 StateJournalContext.Current.LoadObject(42)
        var value = dict["key"]; 
    }
}
```

---

## 3. 错误示能性 (Error Affordance)

当开发者犯错（忘记设置 Workspace）时，体验如何？

- **方案 A**: **编译期错误**。最安全，但最烦人。
- **方案 B/C**: **运行期异常**。
  - 关键在于**异常信息的设计**。
  - ❌ `NullReferenceException` (糟糕的体验)
  - ✅ `InvalidOperationException("DurableObject cannot be accessed outside of a WorkspaceScope. Ensure you have initialized the StateJournal.")` (具有指导性的错误)

---

## 4. 推荐方案：The "Ambient" Compromise

我建议结合三者的优点：

1. **API 层面**：采用 **方案 B (静态访问)** 的简洁性。
   - 允许 `new DurableDict(id)`。
2. **实现层面**：采用 **方案 C (AsyncLocal)** 的隔离性。
   - 解决测试和并发问题。
3. **类型层面**：采纳 Claude 的 **抽象基类** 建议。
   - `DurableObjectBase` 负责封装 `StateJournalContext.Current` 的访问逻辑。

### 最终代码样貌

```csharp
// 基类处理脏活累活
public abstract class DurableObjectBase : IDurableObject
{
    // 隐式获取上下文，对子类透明
    protected Workspace Workspace => StateJournalContext.Current;

    protected T LoadObject<T>(ObjectId id) 
    {
        return Workspace.Load<T>(id); // 自动路由到当前上下文
    }
}

// 子类只需关注业务
public class DurableDict : DurableObjectBase
{
    public DurableDict(ObjectId id) : base(id) { }
    
    // ...
}
```

这种设计既满足了 **MVP 的简单性**（看起来像单例），又保留了 **未来的扩展性**（AsyncLocal 天然支持多 Workspace 并存，只要它们在不同的执行上下文中）。


### Advisor-GPT 发言 (Round 1)

以下从“规范严谨性 + 可测试性”角度对 Claude / Gemini 的建议做审计，并给出可落地的条款草案（Normative）与兼容性分析。

---

## 0. 结论（可审计版）

1) **“Workspace 绑定”必须是 DurableObject 的不可变属性**：对象实例一旦被 `LoadObject/CreateObject` 产出并对外可见，就 MUST 绑定到某一个 Workspace；后续不允许被“环境上下文”隐式改绑。

2) **Ambient Context（AsyncLocal）可以用来提升 DX/测试并行性，但不得成为正确性基础**：正确性基础应当是“对象持有（或可验证）其所属 Workspace”，Ambient 仅用于：
    - 让 API 外观接近方案 B（无需层层传参）
    - 让测试在并行/异步场景下隔离（避免 static 可写全局）

3) 方案 C（Attach-before-expose）若引入对外可见的 `Unbound` 状态，会与现有“状态集合封闭”条款冲突；若仅作为**内部过渡态**且对外不可观察，则可与现有状态机兼容。

---

## 1) 条款兼容性审计（对齐现有 §3.1.3 与对象状态机）

### 1.1 与 §3.1.3 Lazy Loading 条款的兼容性

相关 SSOT 条款：
- `[A-OBJREF-TRANSPARENT-LAZY-LOAD]`：读取遇到 `ObjectId` MUST 自动 `LoadObject(ObjectId)` 并返回实例。
- `[A-OBJREF-BACKFILL-CURRENT]`：Lazy Load 成功后 SHOULD 回填 `_current`，且不改变 dirty 语义。

兼容性结论：
- **兼容的必要条件**：在触发 Lazy Load 时，`DurableDict` 必须能确定“应当调用哪个 Workspace 的 `LoadObject`”。这不是实现细节，而是规范缺口：当前规范只规定了 MUST 调用 `LoadObject`，但未规定“Workspace 选择函数”。
- 若采用“纯 Ambient Current（对象不持有 Workspace）”：将出现**跨 Workspace 误加载**的可观察行为（同一对象实例在不同 scope 下读取同一 key，会从不同 Workspace 拉取不同对象实例/失败原因）。这会使 Lazy Load 行为**不可判定/不可测试**，属于规范层面不允许的歧义。

因此：**要保持 §3.1.3 的可判定性，LoadObject 的分派 MUST 绑定到对象所属 Workspace，而不是绑定到调用点的 ambient。**

### 1.2 与对象状态机条款的兼容性

相关 SSOT 条款：
- `[A-OBJECT-STATE-CLOSED-SET]`：`DurableObjectState` MUST 仅包含四态（Clean/PersistentDirty/TransientDirty/Detached）。
- `[S-STATE-TRANSITION-MATRIX]`：状态转换必须遵循矩阵；Detached 为终态。
- `[S-DETACHED-ACCESS-TIERING]`：Detached 访问分层（元信息不抛，语义访问抛）。

兼容性结论：
- 若方案 C 把 `Unbound` 作为可观察状态（例如对外暴露、或允许对外可见实例在未绑定时被访问），则违反 `[A-OBJECT-STATE-CLOSED-SET]`（状态集合不再封闭）。
- 若将“绑定”定义为**对象构造/加载过程中的内部不变量**，并规定“对象对外可见前已绑定”，则无需引入新状态；与现有状态机完全兼容。

---

## 2) Ambient Context（AsyncLocal）审计：并发/异步可靠性

### 2.1 AsyncLocal 的可靠性边界（能做什么，不能做什么）

可以依赖的性质（.NET 常规语义）：
- `AsyncLocal<T>` 随 `ExecutionContext` 流动：`await`、大多数 `Task` 链路会保持值。
- 并行测试/并行任务：每个异步控制流可拥有不同的 ambient 值（解决“可写 static 全局”互相污染）。

不可依赖/需显式条款化的边界：
- 若调用链路显式使用 `ExecutionContext.SuppressFlow()`，或使用不捕获上下文的低级线程 API，则 ambient 可能不流动。
- 若把 ambient 当作“对象所属 Workspace”的真相源，会导致**对象在不同 scope 下语义变化**（这是逻辑错误，不是 AsyncLocal 的错）。

审计结论：AsyncLocal 适合作为“调用点上下文”（DX/测试隔离），但**不适合作为对象身份绑定**。

### 2.2 审计建议：Scope 需要可嵌套、可恢复、异常安全

如果采纳 Gemini 的 Ambient Context Pattern，规范至少要钉死：
- scope 必须支持嵌套（保存/恢复 previous）
- `Dispose()` 必须在异常路径也能恢复（try/finally）
- `Current` 读取在未设置时必须 fail-fast 且给出恢复指引（避免 NRE）

---

## 3) 可测试性评估（测试向量视角）

### 3.1 测试维度（最小集）

为了让“Workspace 绑定 + 透明 Lazy Load”可判定，测试向量至少覆盖：

1) **绑定前置性（No Unbound Escape）**
- 任何对外可见 durable object 实例在首次语义访问时，必须已经绑定到 Workspace。

2) **跨并发隔离（Parallel Ambient）**
- 两个并行异步流分别设置不同 WorkspaceScope 时，Lazy Load 互不串台。

3) **跨 Workspace 误用（Mismatch）**
- 把属于 Workspace A 的对象实例拿到 Workspace B 的 scope 中调用语义访问：必须 fail-fast（而不是悄悄在 B 上 LoadObject）。

4) **Detached 优先级**
- 对象已 Detached 时，语义访问必须抛 `ObjectDetachedException`，不得因为存在/不存在 ambient 而改变异常类型。

### 3.2 方案对测试的影响（简评）

- 方案 A（显式注入 / 工厂创建）：最易测试（mock workspace），但易出现参数穿透。
- 方案 B（进程级 static 可写单例）：最难测试（并行污染），且无法表达多 workspace（即使只是测试需要）。
- 方案 B’（外观 B + 内核 AsyncLocal）：测试友好，但必须配合“对象不可改绑”的硬条款，否则会把跨 scope 误用变成 silent corruption。
- 方案 C（Attach-before-expose）：若“未绑定对象”永远不可对外见，则测试友好且兼容状态机；否则引入新状态导致规范冲突。

---

## 4) 条款草案（Normative Draft）

以下条款建议加入 StateJournal 规范（建议放入 §3.1.2（LoadObject 语义）附近，作为 Workspace 语义的 SSOT）。条款命名空间采用 `S-`（Semantics）与 `A-`（API）。

### 4.1 Workspace 绑定（核心）

**[S-WORKSPACE-BINDING-EXACTLY-ONE] DurableObject 的 Workspace 绑定（MUST）**
- 每个对外可见的 `IDurableObject` 实例 MUST 绑定到且仅绑定到一个 Workspace（称为 *Owning Workspace*）。
- 绑定在对象被 `LoadObject/CreateObject` 返回给调用方之前完成；调用方不得观察到“未绑定但可用”的对象。

**[S-WORKSPACE-BINDING-IMMUTABLE] Workspace 绑定不可变（MUST）**
- `IDurableObject` 的 Owning Workspace 一旦确定，MUST NOT 在对象生命周期内改变。
- 任何基于 ambient/current 的机制 MUST NOT 造成对象“隐式改绑”。

**[S-WORKSPACE-MISMATCH-FAILFAST] Workspace 不匹配的 Fail-fast（MUST）**
- 若存在“当前执行上下文 Workspace”（例如通过 Ambient Context），且它与对象的 Owning Workspace 不同，则对该对象的任何**语义数据访问** MUST fail-fast（异常或结构化错误均可，但必须可测试可断言），不得在错误的 Workspace 上执行 `LoadObject`。
- 该失败不得被降级为“返回 null/false”。

### 4.2 Ambient Context（可选能力，严禁偷换成正确性基础）

**[A-WORKSPACE-AMBIENT-OPTIONAL] Ambient Workspace 的地位（MAY）**
- 实现 MAY 提供 Ambient Workspace（例如 `AsyncLocal<Workspace>`）用于减少参数传递与提升测试隔离。
- Ambient Workspace MUST 仅作为“调用点默认 Workspace”或“创建/打开期间的便捷入口”；对象内部进行 Lazy Load 时，MUST 以对象 Owning Workspace 为准。

**[A-WORKSPACE-SCOPE-NESTED] WorkspaceScope 必须可嵌套（MUST）**
- 若提供 `WorkspaceScope`（或等价作用域类型），它 MUST 支持嵌套：进入 scope 时保存 previous，退出时恢复 previous。

**[A-WORKSPACE-SCOPE-EXCEPTION-SAFE] WorkspaceScope 异常安全（MUST）**
- scope 的退出恢复逻辑 MUST 在异常路径执行（等价于 try/finally）。

**[A-WORKSPACE-AMBIENT-NONE-ERROR] Ambient 缺失时的错误示能（MUST）**
- 当 API 需要 Workspace 且 ambient 不存在时，MUST fail-fast，并提供可操作的恢复指引（例如“wrap in WorkspaceScope / use Workspace.Open()”），不得产生 `NullReferenceException`。

### 4.3 与现有状态机条款的约束对齐

**[S-WORKSPACE-BINDING-NOT-A-STATE] Workspace 绑定不引入新对象状态（MUST）**
- Workspace 绑定不得扩展 `DurableObjectState` 的取值集合；规范状态仍保持四态封闭（参见 `[A-OBJECT-STATE-CLOSED-SET]`）。

---

## 5) 对 Claude / Gemini 的直接回应（兼容性要点）

### 5.1 回应 Claude：与现有条款的兼容性验证

- 若选择“B（静态单例）+ Provider 封装”：只要 Provider 返回的是**对象 Owning Workspace**（而不是进程全局可变当前值），即可与 §3.1.3 兼容。
- 若 Provider 实际读的是 `StateJournalContext.Current`（ambient）且对象本身不持有 Workspace，则与 §3.1.3 的“LoadObject 分派可判定性”冲突：同一对象实例的读取行为会随 ambient 变化。
- 方案 C 只要满足“Attach-before-expose、外部不可观察 Unbound”，则不触犯 `[A-OBJECT-STATE-CLOSED-SET]`；否则需要修订状态机条款（成本高、风险大）。

### 5.2 回应 Gemini：AsyncLocal 方案在并发/异步场景是否可靠？

- AsyncLocal 作为“调用点上下文隔离”是可靠且测试友好的；它解决的是“并行测试污染”而不是“对象绑定”。
- 真正的风险不是 AsyncLocal 丢失，而是**把 ambient 当成对象绑定**导致跨 scope 误用变成 silent corruption。
- 因此建议：把 AsyncLocal 规范为“可选 ambient”，并用 `[S-WORKSPACE-MISMATCH-FAILFAST]` 把误用路径钉死为 fail-fast，从而把风险变成可测试的、可被 CI 捕获的错误。

---

## 6) 兼容性分析摘要（一句话版）

- “透明 Lazy Load”要可判定，必须让 `LoadObject` 的 Workspace 选择函数稳定且可测试：**按对象 Owning Workspace 分派**。
- AsyncLocal 适合作为 DX/测试隔离工具，但必须通过条款禁止“ambient 改写对象语义”。
---

### 主持人 Round 1 总结 (刘德智)

三位顾问的分析非常深入！让我提炼关键共识和分歧。

#### 已达成共识

| # | 共识点 | 支持者 |
|:-:|:-------|:-------|
| 1 | **方案 B'（外观 B + 内核 AsyncLocal）**是最佳折中 | Claude ✓ Gemini ✓ GPT ✓ |
| 2 | **抽象基类 `DurableObjectBase`** 应引入 | Claude ✓ Gemini ✓ |
| 3 | **WorkspaceScope** 用于测试隔离和上下文控制 | Gemini ✓ GPT ✓ |
| 4 | **Unbound 不应成为可观察状态**（保持四态封闭）| Claude ✓ GPT ✓ |

#### 关键分歧：对象是否持有 Workspace 引用？

这是三位顾问观点的**核心分歧**：

| 顾问 | 立场 | 理由 |
|:-----|:-----|:-----|
| **Claude** | 倾向不持有 | 方案 B 概念简单，通过 Provider 封装即可 |
| **Gemini** | 倾向不持有 | 通过 `StateJournalContext.Current` 获取，DX 更好 |
| **GPT** | **必须持有** | Lazy Load 分派必须按 Owning Workspace，否则跨 scope 会 silent corruption |

**GPT 的关键论证**：

> 若把 ambient 当作"对象所属 Workspace"的真相源，会导致**对象在不同 scope 下语义变化**
> （同一对象实例在不同 scope 下读取同一 key，会从不同 Workspace 拉取不同对象实例）。
> 这会使 Lazy Load 行为**不可判定/不可测试**。

这是一个**规范层面的正确性问题**，不仅仅是 DX 权衡。

#### Round 2 讨论焦点

请三位顾问针对以下问题进行交叉回应：

1. **@Claude @Gemini**：GPT 指出"纯 ambient 会导致跨 scope 误加载"，你们如何回应？是否需要调整方案？

2. **@GPT**：如果对象必须持有 Workspace 引用，这与方案 B 的"API 像单例"如何兼容？

3. **所有人**：如何设计让"对象持有 Workspace"但"API 不需要显式传递"？

---

### Advisor-Claude 发言 (Round 2)

GPT 的论证让我重新审视了自己的立场。让我用苏格拉底式追问来澄清这个问题。

#### 1. GPT 的论证是否成立？——是的，而且更深刻

GPT 指出的问题不仅成立，而且揭示了一个我在 Round 1 忽视的**本质区别**：

> **对象身份 vs 调用点上下文** 是两个不同的概念

让我用类比来说明：

| 概念 | 类比 | StateJournal 对应 |
|:-----|:-----|:------------------|
| **对象身份绑定** | 你的国籍 | 对象属于哪个 Workspace |
| **调用点上下文** | 你当前所在的国家 | 当前 ambient Workspace |

一个中国公民可以在美国访问（调用点上下文改变），但他的国籍（身份绑定）不会因此改变。

**GPT 指出的错误场景**：

```csharp
// 危险场景：ambient 作为身份绑定
var objFromWsA = workspaceA.LoadObject<DurableDict>(id); // 属于 A

using (new WorkspaceScope(workspaceB)) // 切换 ambient 到 B
{
    // 如果 Lazy Load 按 ambient 分派...
    var value = objFromWsA["key"]; // 💥 会在 B 上 LoadObject！
}
```

这确实是 **silent corruption**——对象"偷偷"从错误的 Workspace 加载了数据，而用户无法察觉。

**类比**：这就像一个中国公民在美国银行取款时，系统自动从美国账户（而非中国账户）扣款——因为"当前位置"被错误地当成了"账户归属地"。

#### 2. 我的立场调整

**Round 1 立场**：倾向方案 B（不持有 Workspace），通过 Provider 获取。

**Round 2 立场**：**接受 GPT 的论证**。对象 MUST 持有（或能确定性地恢复）其 Owning Workspace。

但我想在 GPT 的条款基础上做一个**概念澄清**，区分两个层次：

| 层次 | 内容 | 正确性依赖 |
|:-----|:-----|:-----------|
| **身份层** | 对象绑定到 Owning Workspace | MUST 稳定（GPT 的条款） |
| **便利层** | 创建/打开时可用 ambient 简化 API | MAY 使用（Gemini 的 DX） |

#### 3. 修订方案：Owning Workspace + Ambient Scope

我建议的方案是 **A' (构造注入的内部实现 + B 的外部 API)**：

```
                    ┌─────────────────────────────────────────┐
                    │           用户视角（外观 B）             │
                    │                                         │
                    │   using (workspace.BeginScope())        │
                    │   {                                     │
                    │       var dict = new DurableDict(id);   │
                    │       // 或 workspace.Load<DurableDict>(id)
                    │   }                                     │
                    └─────────────────────────────────────────┘
                                       │
                                       ▼
                    ┌─────────────────────────────────────────┐
                    │         内部实现（实质 A）               │
                    │                                         │
                    │   class DurableDict {                   │
                    │       private readonly Workspace _owner;│
                    │       // 构造时从 ambient 捕获并固定     │
                    │   }                                     │
                    └─────────────────────────────────────────┘
```

**关键机制**：构造时**捕获并固定** ambient Workspace 作为 Owning Workspace。

```csharp
public abstract class DurableObjectBase : IDurableObject
{
    // 不可变：构造时确定，此后不再依赖 ambient
    private readonly Workspace _owningWorkspace;
    
    protected DurableObjectBase(ObjectId id)
    {
        // 在构造瞬间"快照"当前 ambient，此后与 ambient 脱钩
        _owningWorkspace = StateJournalContext.Current;
        // ...
    }
    
    // Lazy Load 按 Owning Workspace 分派，不是按 ambient
    protected T LoadObject<T>(ObjectId id) where T : IDurableObject
    {
        return _owningWorkspace.LoadObject<T>(id);
    }
}
```

#### 4. 与 GPT 条款的对齐

让我验证这个设计是否满足 GPT 提出的条款：

| GPT 条款 | 是否满足 | 实现方式 |
|:---------|:---------|:---------|
| `[S-WORKSPACE-BINDING-EXACTLY-ONE]` | ✅ | `_owningWorkspace` 字段不可变 |
| `[S-WORKSPACE-BINDING-IMMUTABLE]` | ✅ | `readonly` 字段 + 构造时赋值 |
| `[S-WORKSPACE-MISMATCH-FAILFAST]` | ⚠️ 需显式实现 | 见下方讨论 |

**关于 `[S-WORKSPACE-MISMATCH-FAILFAST]`**：

GPT 要求"当 ambient 与 Owning Workspace 不同时，语义访问 MUST fail-fast"。

我有一个**替代方案**：**完全忽略 ambient**。

```csharp
// 方案 A'：完全忽略 ambient（我更倾向这个）
protected T LoadObject<T>(ObjectId id)
{
    // 总是用 _owningWorkspace，不检查 ambient
    return _owningWorkspace.LoadObject<T>(id);
}

// GPT 方案：检查并 fail-fast
protected T LoadObject<T>(ObjectId id)
{
    if (StateJournalContext.Current != _owningWorkspace)
        throw new WorkspaceMismatchException(...);
    return _owningWorkspace.LoadObject<T>(id);
}
```

**权衡**：

| 方案 | 优点 | 缺点 |
|:-----|:-----|:-----|
| **忽略 ambient** | 行为可预测；跨 scope 访问"正常工作" | 不提醒用户"这个操作跨越了 scope 边界" |
| **检查 fail-fast** | 强制用户意识到 scope 边界 | 可能过于严格；"正确但恼人" |

**我的倾向**：对于 MVP，采用"忽略 ambient"。因为：
1. 对象的正确性不依赖于调用点 ambient——这本身就是正确的设计
2. Fail-fast 检查可以作为**调试模式**或 `STATEJOURNAL_STRICT_SCOPE` 编译标志
3. 避免"defensive programming"导致的 API 摩擦

#### 5. 这与我 Round 1 的立场矛盾吗？

表面上看，我从"不持有 Workspace"转向了"持有 Workspace"。但如果我们看更深层的概念：

**Round 1 的核心主张**：API 应该像方案 B（单例）那样简洁。

**Round 2 的调整**：实现上对象确实持有 Workspace，但 API 层面用户无需显式传递。

这两者是**兼容的**——GPT 指出的是"正确性约束"，Gemini 和我关注的是"API 人体工学"。好的设计应该**两者兼得**。

**类比**：C# 的闭包"捕获"外部变量。用户写起来像是变量"飘在空中"，但编译器实际生成了一个类来持有这些变量。用户不感知，但正确性有保障。

```csharp
// 用户看到的
var dict = new DurableDict(id); // 简洁！

// 编译器/运行时做的
var dict = new DurableDict(id, owningWorkspace: StateJournalContext.Current);
```

#### 6. 回应主持人的三个问题

> **Q1**: GPT 指出"纯 ambient 会导致跨 scope 误加载"，你们如何回应？

**接受**。这是一个有效的正确性论证，不是 DX 偏好问题。

> **Q2**: 如果对象必须持有 Workspace 引用，这与方案 B 的"API 像单例"如何兼容？

通过"构造时捕获 ambient"模式兼容。用户写 `new DurableDict(id)`，内部自动从 ambient 捕获并固定 `_owningWorkspace`。

> **Q3**: 如何设计让"对象持有 Workspace"但"API 不需要显式传递"？

见上方"修订方案"。核心是：**ambient 在创建时读取一次，然后固化为对象属性**。

#### 7. 遗留问题

有一个边界场景需要讨论：

**场景**：ambient 不存在时调用 `new DurableDict(id)`

```csharp
// 用户忘记包裹 WorkspaceScope
var dict = new DurableDict(id); // 💥 StateJournalContext.Current 为 null
```

**可选策略**：

| 策略 | 行为 | 适用场景 |
|:-----|:-----|:---------|
| **Fail-fast** | 构造时抛异常 | 严格模式（推荐） |
| **延迟绑定** | 允许创建，首次访问时抛异常 | 更宽松，但引入 Unbound 状态 |

我倾向 **Fail-fast**——这与 GPT 的 `[A-WORKSPACE-AMBIENT-NONE-ERROR]` 一致。

---



### Advisor-Gemini 发言 (Round 2)

我非常喜欢 Claude 的修订方案。这在 **正确性 (Correctness)** 和 **易用性 (DX)** 之间找到了完美的平衡点。

#### 1. 核心隐喻：护照 (The Passport Metaphor)

Claude 的方案可以用"护照"来解释：

- **构造时捕获** = **出生地原则**。你出生时（`new`）所在的国家（Scope）决定了你的国籍（Owning Workspace）。
- **固化引用** = **颁发护照**。对象持有 `_owningWorkspace` 就像持有护照。
- **跨 Scope 访问** = **旅行**。当你带着护照去另一个国家（进入另一个 `WorkspaceScope`），你的国籍不会改变。

这个隐喻非常符合直觉，且完美解决了 GPT 提出的 "Silent Corruption" 问题。

#### 2. 关键 DX 陷阱：真空窒息 (The Vacuum Problem)

"构造时捕获"模式最大的 DX 风险在于：**如果在没有任何 Scope 的地方创建对象会发生什么？**

```csharp
// ❌ 错误场景：在真空（无 Scope）中呼吸
var dict = new DurableDict(id); 
```

这里必须 **Fail-fast**。绝对不能创建一个 "无国籍" (Stateless/Unbound) 的对象，否则后续的 Lazy Load 会在随机时刻炸毁，调试难度极高。

**建议的错误示能 (Error Affordance)**：

异常信息必须具有**指导性**，而不是冷冰冰的 `NullReferenceException`。

> ❌ `Object reference not set to an instance of an object.`
>
> ✅ `InvalidOperationException: Cannot create 'DurableDict' outside of a WorkspaceScope. Please wrap your code in 'using (new WorkspaceScope(workspace)) { ... }' or use 'workspace.Create<DurableDict>()'.`

#### 3. 互操作性：为什么"忽略 Ambient"优于"检查 Ambient"

Claude 提到了一个权衡：当 `Current != Owning` 时，是 **Fail-fast** 还是 **Ignore**？

从 DX 角度，我强烈建议 **Ignore Ambient (即允许跨 Scope 访问)**。

**理由：数据迁移场景**

如果我们禁止跨 Scope 访问，那么编写"将数据从 Workspace A 复制到 Workspace B"的代码将变得异常痛苦：

```csharp
// ✅ 期望的 DX：像普通对象一样操作
using (new WorkspaceScope(wsB)) 
{
    var objA = wsA.Load<DurableDict>(idA); // objA 属于 wsA
    var objB = new DurableDict(idB);       // objB 属于 wsB (当前 scope)
    
    // 跨 Scope 互操作：读取 A，写入 B
    // 如果 objA 强制检查 Current == wsA，这里会抛出异常！
    objB["key"] = objA["key"]; 
}
```

如果强制 Fail-fast，开发者就被迫在两个 Scope 之间反复横跳，代码会变得支离破碎。
只要对象内部坚定地使用 `_owningWorkspace`（护照），跨 Scope 访问就是安全的。

#### 4. 最终代码建议

结合三位的智慧，这是我建议的 `DurableObjectBase` 最终形态：

```csharp
public abstract class DurableObjectBase : IDurableObject
{
    private readonly Workspace _owningWorkspace;

    protected DurableObjectBase(ObjectId id)
    {
        // 1. 捕获 (Capture)
        var current = StateJournalContext.Current;
        
        // 2. 校验 (Validate) - 拒绝真空创建
        if (current == null)
        {
            throw new InvalidOperationException(
                $"Attempted to create {this.GetType().Name} without an active WorkspaceScope. " +
                "Ensure you are inside a 'using (new WorkspaceScope(...))' block.");
        }

        // 3. 固化 (Solidify)
        _owningWorkspace = current;
        Id = id;
    }

    // 4. 忽略 Ambient (Ignore Ambient) - 始终使用护照
    protected T LoadObject<T>(ObjectId id) where T : IDurableObject
    {
        return _owningWorkspace.LoadObject<T>(id);
    }
}
```

**总结**：
- **API**: 保持 `new DurableDict(id)` 的极致简洁。
- **正确性**: 构造时固化，杜绝漂移。
- **互操作**: 允许持证旅行（跨 Scope 访问）。


### Advisor-GPT 发言 (Round 2)

## 1) 共识确认

我确认接受并支持 Claude/Gemini 在 Round 2 收敛出的修订方案：

1. 对象必须持有 Owning Workspace（构造/加载时捕获并固化，引用不可变）
2. 透明 Lazy Load 必须按 Owning Workspace 分派
3. Ambient/AsyncLocal 仅用于 DX/测试隔离与“创建入口便捷”，不是正确性基础
4. 真空创建（无 scope/无 workspace）必须 fail-fast，并给出明确的恢复指引

我唯一的“最终修订意见”是：在上述前提成立时，**不应强制要求“ambient 与 owning 不一致就 fail-fast”**。已绑定对象始终使用自己的“护照”（Owning Workspace）完成加载/读写时，跨 scope 访问的行为是稳定且可预测的；强制 fail-fast 反而会破坏跨 Workspace 复制/迁移的可用性。

---

## 2) 最终条款（建议写入规范的 Normative 版本）

为避免“实现看起来对，但规范不可判定/不可测试”，建议将本议题收敛为以下硬条款（条款 ID 可按现有编号体系调整）：

### 2.1 Owning Workspace（身份绑定）

**[S-WORKSPACE-OWNING-EXACTLY-ONE]（MUST）**
- 每个对外可见的 `IDurableObject` 实例 MUST 绑定到且仅绑定到一个 Workspace（*Owning Workspace*）。

**[S-WORKSPACE-OWNING-IMMUTABLE]（MUST）**
- Owning Workspace 在对象生命周期内 MUST NOT 改变。
- ambient/current 机制 MUST NOT 造成对象被隐式改绑。

**[S-WORKSPACE-ATTACH-BEFORE-EXPOSE]（MUST）**
- 对象在被 `LoadObject/CreateObject` 返回给调用方之前 MUST 已绑定 Owning Workspace。
- 调用方不得观察到“未绑定但可用”的对象实例（避免引入可观察的 `Unbound` 状态）。

### 2.2 Lazy Load 分派（正确性核心）

**[S-LAZYLOAD-DISPATCH-BY-OWNER]（MUST）**
- 当触发透明 Lazy Load（例如 `DurableDict` 遇到 `ObjectId` 并需 `LoadObject`）时，MUST 使用对象的 Owning Workspace 来分派 `LoadObject(ObjectId)`。
- MUST NOT 使用调用点 ambient/current 来选择目标 Workspace。

### 2.3 Ambient Workspace（仅便捷，不是权威）

**[A-WORKSPACE-AMBIENT-ROLE]（MAY/MUST NOT）**
- 实现 MAY 提供 ambient workspace（例如 `AsyncLocal<Workspace>`）以减少参数传递与支持并行测试隔离。
- ambient workspace MUST NOT 改变“已绑定对象”的语义行为；已绑定对象的读写与 Lazy Load 行为 MUST 与 ambient 无关。

**[A-WORKSPACE-SCOPE-NESTED-EXCEPTIONSAFE]（MUST）**
- 若提供 `WorkspaceScope`（或等价机制），它 MUST 可嵌套并在异常路径可靠恢复（等价于保存 previous + `try/finally` 恢复）。

### 2.4 真空创建（无 scope / 无 workspace）

**[S-WORKSPACE-VACUUM-CREATION-FAILFAST]（MUST）**
- 当某创建/产出路径依赖 ambient 捕获 Workspace 时（例如 `new DurableDict(id)`），若 ambient 不存在则 MUST fail-fast。
- 错误信息 MUST 提供可操作的恢复指引（例如“wrap in WorkspaceScope / use workspace.Create/Load API”），不得退化为 `NullReferenceException`。

---

## 3) MVP 范围：是否需要实现 [S-WORKSPACE-MISMATCH-FAILFAST]？

**结论：MVP 不需要把 `[S-WORKSPACE-MISMATCH-FAILFAST]` 作为普遍强制条款实现。**

建议处理方式（二选一）：
1) **删除/替换该条款**：用 `[S-LAZYLOAD-DISPATCH-BY-OWNER]` + “ambient 不得影响已绑定对象语义”取代它，直接消除 silent corruption 的根源。
2) **降级为 Strict/Debug 选项（MAY）**：仅在需要更强提示时启用（例如 `StateJournalOptions.StrictScopeChecks`），而不是把“跨 scope 合法互操作”判为错误。

这样能同时满足：正确性（不随 ambient 漂移）、可测试性（分派函数稳定）、DX（护照旅行/迁移友好）、以及状态机封闭（不引入 Unbound）。

---

## 📋 主持人收尾总结 (刘德智)

经过两轮深入讨论，三位顾问达成了完整共识！

### 最终决议

#### 1. 绑定机制：方案 A'（内部持有 + 外部简洁）

**核心模式：构造时捕获并固化（The Passport Pattern）**

```
┌─────────────────────────────────────────────────────────────┐
│  用户代码 (API 外观像方案 B)                                  │
├─────────────────────────────────────────────────────────────┤
│  using (new WorkspaceScope(workspace))                      │
│  {                                                          │
│      var dict = new DurableDict(id);  // 简洁！              │
│  }                                                          │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼ 构造时捕获
┌─────────────────────────────────────────────────────────────┐
│  内部实现 (实质方案 A)                                        │
├─────────────────────────────────────────────────────────────┤
│  private readonly Workspace _owningWorkspace;               │
│  // 捕获后固化，不再依赖 ambient                              │
└─────────────────────────────────────────────────────────────┘
```

#### 2. 类型层次：引入抽象基类

```csharp
interface IDurableObject              // 对外契约（保持兼容）
abstract class DurableObjectBase      // 实现共享
    : IDurableObject
    ├── DurableDict
    └── DurableArray (未来)
```

#### 3. 条款清单（写入规范）

| 条款 ID | 内容 | 级别 |
|:--------|:-----|:----:|
| `[S-WORKSPACE-OWNING-EXACTLY-ONE]` | 每个对象绑定到且仅绑定到一个 Owning Workspace | MUST |
| `[S-WORKSPACE-OWNING-IMMUTABLE]` | 绑定不可变 | MUST |
| `[S-WORKSPACE-ATTACH-BEFORE-EXPOSE]` | 对外可见前必须已绑定 | MUST |
| `[S-LAZYLOAD-DISPATCH-BY-OWNER]` | Lazy Load 按 Owning Workspace 分派 | MUST |
| `[A-WORKSPACE-AMBIENT-ROLE]` | Ambient 仅用于 DX/测试，不影响已绑定对象 | MAY |
| `[A-WORKSPACE-SCOPE-NESTED-EXCEPTIONSAFE]` | WorkspaceScope 可嵌套、异常安全 | MUST |
| `[S-WORKSPACE-VACUUM-CREATION-FAILFAST]` | 无 scope 创建必须 fail-fast | MUST |

#### 4. MVP 范围

- ✅ 实现 Owning Workspace 绑定
- ✅ 实现 `WorkspaceScope` + `AsyncLocal`
- ✅ 实现 `DurableObjectBase` 抽象基类
- ❌ 不需要强制 `[S-WORKSPACE-MISMATCH-FAILFAST]`（跨 scope 访问允许）

### 关键隐喻：护照模式 (Passport Pattern)

| 概念 | 隐喻 | 实现 |
|:-----|:-----|:-----|
| **出生地原则** | 出生时所在国家决定国籍 | 构造时捕获 ambient |
| **颁发护照** | 持有国籍证明 | `_owningWorkspace` 字段 |
| **持证旅行** | 可以出国但国籍不变 | 跨 scope 访问使用 Owning Workspace |
| **真空窒息** | 不能在无国籍状态出生 | 无 scope 创建 fail-fast |

### 后续行动

- [ ] **DocOps**：将条款写入 `mvp-design-v2.md` §3.1.2 附近
- [ ] **Implementer**：
  1. 创建 `StateJournalContext` + `WorkspaceScope`
  2. 创建 `DurableObjectBase` 抽象基类
  3. 重构 `DurableDict` 继承自基类
  4. 实现透明 Lazy Loading

---

## 状态

**状态**：✅ 已完成

**产出**：
- 绑定机制决策：方案 A'（护照模式）
- 类型层次决策：引入 `DurableObjectBase`
- 7 条规范条款（待写入）
- MVP 范围界定

---
