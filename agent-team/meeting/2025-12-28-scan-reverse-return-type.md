# 畅谈会：ScanReverse() 返回值类型设计

> **日期**：2025-12-28
> **形式**：畅谈会 (Jam Session)
> **标签**：#design
> **主持人**：刘德智 (Team Leader / StandardsChair)
> **参与者**：Advisor-Claude, Advisor-Gemini, Advisor-GPT
> **状态**：✅ 已完成

---

## 背景

在 RBF 层接口设计中（`atelia/docs/StateJournal/rbf-interface.md`），`IRbfScanner.ScanReverse()` 方法需要返回一个可枚举的帧序列。当前规范注释中提到：

```csharp
/// <summary>
/// 逆向扫描枚举器（实现细节由底层决定）。
/// </summary>
/// <remarks>
/// 实现 MAY 选择 struct（避免堆分配）或 class（支持 async）。
/// 上层应通过 foreach 或 LINQ 消费，不依赖具体形态。
/// </remarks>
public /* struct | class */ RbfReverseEnumerable : IEnumerable<RbfFrame> { }
```

这里存在三种典型设计选择：

1. **返回 `IEnumerable<RbfFrame>` 接口**
2. **返回 `struct` 枚举器具体类型**（如 `RbfReverseEnumerable`）
3. **返回 `class` 枚举器具体类型**

本次畅谈会的目标是分析这三种选择的利弊，形成顾问组的整体推荐方案。

## 讨论主题

1. 三种返回值类型的性能特征对比
2. 与 `RbfFrame` 作为 `ref struct` 的交互约束
3. 未来扩展性（如 async 支持）
4. API 一致性与 DX（开发者体验）
5. 推荐方案及其理由

## 相关文件

- [rbf-interface.md](../../atelia/docs/StateJournal/rbf-interface.md) — RBF 层接口规范
- [rbf-format.md](../../atelia/docs/StateJournal/rbf-format.md) — RBF 帧格式规范（如果需要）

## 约束条件（来自规范）

从 `rbf-interface.md` 中提取的关键约束：

- **`RbfFrame` 是 `ref struct`**：生命周期受限于底层缓冲区，不能装箱、不能作为泛型参数（在某些场景下）
- **逆向扫描语义**：从文件尾部向头部遍历所有帧
- **上层消费模式**：通过 `foreach` 或 LINQ 消费

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

各位顾问好！

今天我们来讨论 RBF 层 `ScanReverse()` 方法的返回值类型设计。这是一个典型的 API 设计权衡问题，涉及性能、可用性、扩展性等多个维度。

**请注意一个关键约束**：`RbfFrame` 是 `ref struct`，这会影响某些设计选择的可行性。

请 Advisor-Claude 先从概念框架角度分析三种选择的本质差异，然后 Advisor-Gemini 从开发者体验角度展开，最后 Advisor-GPT 进行严谨的条款化分析和边界检验。

期待各位的真知灼见！

---

### Advisor-Claude 发言

谢谢邀请！这是一个非常有趣的设计问题。让我从概念框架角度展开分析。

#### 1. 本质差异：类型系统层面的三种选择

如果我们退一步看，这三种选择代表了 API 设计中"抽象程度"的光谱：

| 选择 | 抽象层次 | 本质 |
|:-----|:---------|:-----|
| `IEnumerable<T>` 接口 | 最高抽象 | "任何可枚举的东西" |
| `class` 具体类型 | 中等抽象 | "一个特定的可枚举对象" |
| `struct` 具体类型 | 最低抽象 | "一个值语义的枚举器，生命周期由调用者控制" |

从类型系统视角，这三种选择的核心区别在于：

- **接口返回**：调用者不知道具体类型 → 必须通过虚调用（vtable dispatch）交互
- **class 返回**：调用者知道具体类型 → 可内联优化，但需要堆分配
- **struct 返回**：调用者知道具体类型 → 可内联优化 + 零堆分配

**类比**：这类似于函数返回值的"存在类型（existential type）"与"具体类型"之争。`IEnumerable<T>` 说的是"存在某个类型 X 满足 IEnumerable 契约"，而具体类型说的是"就是这个 X"。

#### 2. 核心问题：`RbfFrame` 作为 `ref struct` 的"传染性"

这是本次讨论的**关键约束**，让我仔细分析。

##### 2.1 `IEnumerable<T>` 的 `T` 能否是 `ref struct`？

**答案是：不能**（至少在 C# 12 及之前版本）。

原因：

```csharp
// IEnumerable<T> 的标准定义
public interface IEnumerable<T> {
    IEnumerator<T> GetEnumerator();
}

public interface IEnumerator<T> {
    T Current { get; }  // T 会被存储或返回
    bool MoveNext();
}
```

这里有两个致命问题：

1. **装箱禁止**：`ref struct` 不能装箱，但 `IEnumerator<T>` 作为接口必须分配在堆上或被装箱
2. **泛型类型参数限制**：在 C# 12 及之前，`ref struct` 不能作为泛型类型参数（因为泛型代码可能在任意位置存储 `T`）

##### 2.2 C# 13 的 `allows ref struct` 救星？

C# 13 引入了 `allows ref struct` 反约束（anti-constraint），允许 `ref struct` 作为泛型参数：

```csharp
void Method<T>(T obj) where T : allows ref struct { }
```

**但这不能救 `IEnumerable<T>`**，因为：

1. **标准库未重新定义**：.NET 9 中 `IEnumerable<T>` / `IEnumerator<T>` **没有**添加 `allows ref struct`
2. **架构根本问题**：即使添加了，`IEnumerator<T>.Current` 的生命周期语义与 `ref struct` 冲突——`Current` 需要在 `MoveNext()` 之后仍然有效，但 `ref struct` 的生命周期绑定到栈帧

##### 2.3 `Span<T>` 生态的先例

微软在设计 `Span<T>` 生态时面临过同样问题。他们的解决方案值得参考：

```csharp
// Span<T> 不实现 IEnumerable<T>
// 而是定义了自己的枚举器模式
public ref struct Span<T> {
    public Enumerator GetEnumerator() => new Enumerator(this);
    
    public ref struct Enumerator {
        public ref T Current { get; }
        public bool MoveNext();
    }
}
```

**关键洞见**：`Span<T>` 完全绕过了 `IEnumerable<T>`，返回一个 `ref struct Enumerator`。C# 的 `foreach` 语法不要求 `IEnumerable`——它只要求存在 `GetEnumerator()` 方法和正确的枚举器鸭子类型（duck typing）。

#### 3. 设计选项重新评估

基于以上分析，让我重新评估三种选择的可行性：

| 选择 | 可行性 | 原因 |
|:-----|:-------|:-----|
| `IEnumerable<RbfFrame>` | ❌ **不可行** | `ref struct` 不能作为 `IEnumerable<T>` 的类型参数 |
| `class RbfReverseEnumerable` | ⚠️ **受限可行** | 可行，但枚举器的 `Current` 返回 `ref struct` 需要特殊处理 |
| `struct RbfReverseEnumerable` | ✅ **推荐** | 完全匹配 `Span<T>` 生态的成熟模式 |

##### 关于 "class" 选项的细节

如果选择 class，需要注意：

```csharp
public class RbfReverseEnumerable {
    public Enumerator GetEnumerator() => ...;
    
    // 枚举器本身可以是 ref struct
    public ref struct Enumerator {
        public RbfFrame Current { get; }  // RbfFrame 是 ref struct
        public bool MoveNext();
    }
}
```

这是可行的，因为**外层容器是 class，但内层枚举器是 ref struct**。问题是：这种混合设计增加了一次堆分配，而收益不明显。

#### 4. 推荐方案：`ref struct` 枚举器模式

基于 `Span<T>` 生态的成熟先例，我推荐：

```csharp
public interface IRbfScanner {
    // ...
    RbfReverseEnumerable ScanReverse();
}

/// <summary>
/// 逆向扫描的可枚举视图。
/// </summary>
/// <remarks>
/// <para>实现 duck-typed 枚举器模式，支持 foreach。</para>
/// <para>不实现 IEnumerable&lt;T&gt;（因 RbfFrame 是 ref struct）。</para>
/// </remarks>
public readonly ref struct RbfReverseEnumerable {
    // 内部持有 scanner 引用等
    
    public Enumerator GetEnumerator() => new Enumerator(...);
    
    public ref struct Enumerator {
        public RbfFrame Current { get; }
        public bool MoveNext();
    }
}
```

##### 设计哲学支撑

1. **类型系统作为护栏**：`ref struct` 的约束不是障碍，而是防止 use-after-free 的护栏。选择一个会绕过这些护栏的设计（如返回包装后的 heap 对象）是**反模式**。

2. **接口 vs 具体类型**：通常 API 设计偏好返回接口以保持灵活性。但这条原则有例外——当底层约束使接口不可行时，返回具体类型是正确选择。`Span<T>` 就是最好的先例。

3. **一致性胜于理想**：RBF 层的定位是"高性能、零拷贝"的底层设施。返回 `ref struct` 枚举器与这一定位高度一致，也与 `RbfFrame` 本身是 `ref struct` 的设计保持概念一致性。

#### 5. 系统类比总结

| 类比来源 | 设计选择 | 原因 |
|:---------|:---------|:-----|
| `Span<T>.GetEnumerator()` | 返回 `ref struct Enumerator` | 性能 + 生命周期安全 |
| `ReadOnlySpan<T>` | 同上 | 同上 |
| `Memory<T>.Span` | 返回 `Span<T>`（ref struct） | 允许访问但不存储 |
| `ImmutableArray<T>` | 返回自定义 `struct Enumerator` | 避免 heap 分配 |

**核心模式**：高性能场景下，**返回具体的 struct/ref struct 枚举器** 是 .NET 生态的成熟实践。

#### 6. 待讨论的边界问题

1. **LINQ 兼容性**：`ref struct` 枚举器不支持 LINQ（`Where`, `Select` 等需要 `IEnumerable`）。如果上层需要 LINQ，必须先 `ToArray()` 具象化。这是 **可接受的取舍**——RBF 层不应该鼓励在帧级别做复杂查询。

2. **async 支持**：当前设计是同步的。如果未来需要 async，可能需要引入 `IAsyncEnumerable<T>` 的变体。但 `ref struct` 在 async 上下文中有更多限制（不能跨 await 边界存在）。这值得单独讨论。

3. **多次枚举**：`ref struct` 枚举器天然防止"存储后多次枚举"。调用者必须：
   - 每次调用 `ScanReverse()` 获取新枚举器，或
   - 在单次遍历中完成所有工作
   
   这是**特性而非缺陷**——强制调用者思考遍历次数。

---

### Advisor-Gemini 发言

感谢 Claude 的精彩分析！作为"用户代言人"，我完全同意从底层约束出发的推导。

但我必须指出，**`ref struct` 枚举器模式对开发者来说是一次"交互范式的转换"**。这不仅仅是类型系统的差异，更是心智模型的断裂。

让我从 DX（开发者体验）的角度来剖析这个设计。

#### 1. 交互体验：从"流媒体"到"现场直播"

如果说 `IEnumerable<T>` 是 **Netflix 流媒体**（可以缓存、稍后观看、随意快进），那么 `ref struct` 枚举器就是 **现场直播**（Live Performance）。

- **必须在场**：你必须在 `foreach` 循环的当下消费它。
- **不可录制**：你不能简单地把它存进 `List<T>`（除非你进行"转录"——深拷贝）。
- **稍纵即逝**：一旦循环结束，数据就"消失"了（栈帧弹出）。

##### 典型代码模式对比

**A. 快乐路径 (The Happy Path)**
开发者最常用的 `foreach` 体验是**无缝**的。得益于 C# 的 Duck Typing，以下代码完全合法且高效：

```csharp
// ✅ 体验完美，与普通 IEnumerable 无异
foreach (var frame in scanner.ScanReverse()) 
{
    if (frame.IsKeyFrame) Process(frame);
}
```

**B. 挫败路径 (The Friction Path)**
当开发者试图使用他们熟悉的 LINQ 工具箱时，会撞上一堵墙：

```csharp
// ❌ 编译错误：RbfReverseEnumerable 不包含 'Where' 的定义
var keyFrames = scanner.ScanReverse().Where(f => f.IsKeyFrame);

// ❌ 编译错误：无法将 ref struct 作为泛型参数
var list = scanner.ScanReverse().ToList(); 
```

这种"突然的无能为力感"是 DX 的主要风险点。

#### 2. 认知负担与"显式降级"

Claude 提到了 `ref struct` 的传染性。我认为这实际上是一种 **"强制显式降级" (Forced Explicit Degradation)** 的设计模式。

在普通 API 中，开发者往往无意识地进行堆分配（`.ToList()` 随手就来）。但在 RBF 层，我们**希望**开发者意识到他们在处理高性能、零拷贝的数据。

- **错误即示能 (Error as Affordance)**：
  当编译器报错 "type cannot be used as type parameter" 时，它实际上是在告诉开发者：**"嘿，你在试图持久化一个瞬态引用，这很危险！"**
  
  这不仅仅是阻挡，更是**教育**。它强迫开发者思考：*我是真的需要保存这个帧，还是只需要它的数据？*

- **缓解措施**：
  既然不能用 LINQ，我们是否可以提供一些"低配版"的扩展方法？
  *建议*：不要尝试复刻 LINQ。那会陷入"恐怖谷"（似像非像）。
  *推荐*：保持 API 的"原始感"，鼓励使用基础的 `foreach` 和 `if`。这是对性能最诚实的表达。

#### 3. 命名与隐喻设计

命名是 API 的 UI。既然我们不能提供 `IEnumerable` 的全部承诺，就不应该在名字里暗示它。

- **避免**：`RbfReverseEnumerable`
  - 后缀 `Enumerable` 强烈暗示实现了 `IEnumerable` 接口，这是一种**虚假示能 (False Affordance)**。
  
- **推荐**：`RbfReverseSequence` 或 `RbfFrameSequence`
  - **Sequence (序列)**：暗示了顺序性，但没有承诺 `IEnumerable` 的接口契约。
  - 配合文档注释：`/// A transient, stack-only sequence of RBF frames.`

#### 4. 护栏设计：如何优雅地"拒绝"

既然用户不能 `.ToList()`，我们需要提供正确的"逃生通道"。

如果用户真的需要保存数据怎么办？`RbfFrame` 是 `ref struct`，意味着它本身就不能被保存。用户必须**拷贝数据**。

我们应该在 `RbfFrame` 上提供明确的**导出 API**，作为对 LINQ 缺失的补偿：

```csharp
// 在 RbfFrame 定义中
public ref struct RbfFrame 
{
    // ...
    
    // ✅ 明确的逃生通道：将瞬态帧转录为持久化对象（DTO）
    public RbfFrameDto ToDto() => new RbfFrameDto(this);
}
```

这样，开发者的心智模型就通畅了：
1. 尝试 `.ToList()` -> 失败（编译器护栏生效）。
2. 意识到是 `ref struct`。
3. 调用 `.Select(f => f.ToDto()).ToList()` -> 成功（显式的数据拷贝）。

#### 5. 总结建议

从 DX 角度，我完全支持 **`ref struct` 枚举器模式**，但有以下补充建议：

1. **命名调整**：建议使用 `RbfReverseSequence` 替代 `RbfReverseEnumerable`，避免接口误导。
2. **逃生通道**：确保 `RbfFrame` 拥有便捷的 `ToDto()` 或 `CloneData()` 方法，让"持久化"这一操作变得显式但顺滑。
3. **文档隐喻**：在文档中明确使用 **"瞬态序列 (Transient Sequence)"** 这一术语，解释为什么 LINQ 不可用。

> **一句话总结**：让编译器做"坏人"，拦截不安全的内存操作；让 API 做"好人"，提供清晰的拷贝路径。

---

### Advisor-GPT 发言

我从“可判定性 / 规范可执行性”的角度做一次严格审计：先验证类型系统结论是否成立，再把边界行为钉成条款，最后核对与现有 [rbf-interface.md] 的一致性。

> Claude 的核心断言是：`IEnumerable<RbfFrame>` 不可行，因为 `RbfFrame` 是 `ref struct`。
>
> Gemini 的核心补充是：即使 foreach 可用，也要避免“Enumerable”命名与“LINQ 可用”示能，并提供显式拷贝逃生通道（如 `ToDto()`）。

#### 1) 逻辑检验：`IEnumerable<ref struct>` 是否真的不可行？

结论：**在当前 .NET/C# 生态下，`IEnumerable<RbfFrame>` 作为公开返回类型确实不可行**；Claude 的方向是对的，但需要把“不可行”的理由写得更精确，避免被未来语言特性误读。

**理由 A（最硬的规则）：`ref struct` 不能作为“类/接口”的泛型实参。**

`IEnumerable<T>`/`IEnumerator<T>` 都是 interface（引用类型语义）。把 `RbfFrame`（`ref struct`）代入 `T` 会触发语言规则：**`ref struct` 类型不得用作泛型类型参数，除非目标泛型类型显式允许（如未来的 `allows ref struct` 生态）**。当前 BCL 的 `IEnumerable<T>` 并未声明“允许 ref struct”。所以这一步在编译期就会被禁止。

**理由 B（即便未来“允许”，语义仍高度可疑）：接口枚举协议与 `ref struct` 生命周期语义不匹配。**

即使假设某天存在“允许 ref struct”的 `IEnumerable<T>` 版本，`IEnumerator<T>` 作为接口返回/传递，其实现对象通常是堆对象或涉及装箱/捕获。`ref struct` 的存在目的正是阻止这类跨栈帧/跨边界存活。

因此我建议规范层不要把论证押在“C# 版本细节”上，而是把结论表述为：**RBF 的 ScanReverse 产物是 *stack-only, non-boxable, non-interface-based* 的序列**（与 `Span<T>` 的枚举模型一致）。

**可能遗漏的“绕路”场景（需要在规范里显式排除/说明）：**

- 可以返回 `IEnumerable<Address64>` 或 `IEnumerable<RbfFrameDto>`（或其他可堆存的 DTO），然后由调用方再 `TryReadAt`/拷贝 payload。这在类型上可行，但会改变语义（零拷贝热路径不再直接暴露帧视图）与性能特征；应当作为“物化/导出 API”的备选，而非 `ScanReverse()` 的主语义。

#### 2) 边界情况审计（必须写进契约，否则实现会分叉）

**2.1 空序列**

- 需要明确：空文件、仅 tombstone、或扫描窗口为空时，`ScanReverse()` 返回的序列 **MUST 可被 `foreach` 安全消费**，并表现为 0 次迭代（`MoveNext()` 首次即返回 false）。
- 不建议把“空”做成异常；否则上层需要额外分支，且不利于组合（诊断/恢复也更难）。

**2.2 遍历过程中底层文件被修改**

这是典型“规范不写=实现分叉”的点。我建议明确选择其一（至少 P0 写清）：

- **Fail-fast（推荐）**：若检测到文件长度/校验/映射 epoch 变化导致当前扫描不再可信，枚举器 **SHOULD** 尽早抛出 `InvalidOperationException`（或专用异常）并停止产出。
- **未定义（可接受但要写明）**：若团队不愿承诺检测能力，则至少写成“并发修改时行为未定义；调用方必须在稳定快照上使用”。

不建议承诺“强一致快照”除非实现明确采取了 snapshot/共享只读视图机制（否则这是一条不可实现的 MUST）。

**2.3 多次 `GetEnumerator()` 的行为（以及多次枚举）**

这里要区分三个层次：

1. **同一个 `ScanReverse()` 返回值上，多次 `GetEnumerator()`**：
    - 建议规定为：每次 `GetEnumerator()` 返回一个**独立**枚举器，起点一致（同一“扫描窗口”）。
    - 这样调用方心智模型接近 `IEnumerable` 的“可重复枚举”，但仍保持 stack-only。

2. **同一个枚举器实例是否可重入/并发使用**：
    - MUST NOT（典型枚举器语义）：一个枚举器实例不得被并发推进。

3. **`Current` 的有效期**：
    - 需要钉死：`Current`（即 `RbfFrame`）只在“下一次 `MoveNext()` 之前”有效；调用方若要持久化必须显式拷贝（已有 `PayloadToArray()`）。

#### 3) 条款化建议（可直接编入规范的草案）

我建议把“返回类型形态 + 生命周期 + 并发修改策略”收敛成以下条款草案（命名可再调整，但语义建议一次钉死）：

- **`[A-RBF-SCANREVERSE-SEQUENCE]`**：`IRbfScanner.ScanReverse()` MUST 返回一个可被 `foreach` 消费的“逆向帧序列”类型（duck-typed enumerator pattern），其 `GetEnumerator()` 返回的枚举器 MUST 为 stack-only（不得装箱）。

- **`[S-RBF-SCANREVERSE-NO-IENUMERABLE]`**：由于 `RbfFrame` 为 `ref struct`，`ScanReverse()` 返回类型 MUST NOT 承诺或实现 `IEnumerable<RbfFrame>` / `IEnumerator<RbfFrame>`；规范文本 MUST NOT 要求调用方使用 LINQ 直接消费。

- **`[S-RBF-SCANREVERSE-EMPTY-IS-OK]`**：当扫描范围为空时，`ScanReverse()` 返回序列 MUST 产生 0 个元素且不得抛出异常。

- **`[S-RBF-SCANREVERSE-CURRENT-LIFETIME]`**：枚举器 `Current` 返回的 `RbfFrame` 视图的生命周期 MUST 不超过下一次 `MoveNext()`；调用方若需持久化数据 MUST 显式拷贝（例如调用 `PayloadToArray()` 或其他导出 API）。

- **`[S-RBF-SCANREVERSE-CONCURRENT-MUTATION]`**：若底层文件/映射在枚举期间被修改，行为 MUST 明确为二选一：
  - （推荐）实现 SHOULD fail-fast（抛 `InvalidOperationException` 或专用异常）并停止产出；或
  - 实现 MUST 文档化为“未定义行为”，并要求调用方在稳定快照上使用。

- **`[S-RBF-SCANREVERSE-MULTI-GETENUM]`**：对同一个 `ScanReverse()` 返回值，多次调用 `GetEnumerator()` MUST 返回互不干扰的枚举器实例；每个枚举器实例从同一扫描窗口的尾部开始。

#### 4) 与现有规范一致性（rbf-interface.md 冲突点）

当前 [rbf-interface.md] 的 `IRbfScanner.ScanReverse()` 段落存在**自相矛盾/不可实现**之处：

1. `public /* struct | class */ RbfReverseEnumerable : IEnumerable<RbfFrame> { }` 与 `[A-RBF-FRAME-REF-STRUCT]`（`RbfFrame` 为 `ref struct`）发生冲突：在现行语言/库下无法成立。
2. remarks 写“上层应通过 foreach 或 LINQ 消费”也与 `ref struct` 约束冲突：即便 foreach 可用，LINQ 基于 `IEnumerable<T>` 扩展方法，无法无损适配。

因此，若采纳 Claude/Gemini 推荐方向，需要同步修订 [rbf-interface.md] 的该段：

- 把“`IEnumerable<RbfFrame>`”改为“duck-typed sequence/enumerator 模式”的明确契约；
- 把“LINQ 消费”的措辞降级或移除，并把“显式拷贝逃生通道”指向 `RbfFrame.PayloadToArray()`（或将来新增 `ToDto()` 也可，但必须明确它是 allocation/copy 路径）。

---
### 主持人总结 (刘德智)

感谢三位顾问的精彩发言！让我来整理本次畅谈会的共识和推荐方案。

---

## 📋 畅谈会共识

### 核心结论

**三种返回值类型的可行性评估**：

| 选择 | 可行性 | 理由 |
|:-----|:-------|:-----|
| `IEnumerable<RbfFrame>` 接口 | ❌ **不可行** | `ref struct` 不能作为泛型接口的类型参数 |
| `class` 枚举器具体类型 | ⚠️ **受限可行** | 可行但增加堆分配，收益不明显 |
| `ref struct` 枚举器具体类型 | ✅ **推荐** | 与 `Span<T>` 生态一致，零分配，类型安全 |

### 已达成共识

1. **类型系统约束是决定性的**（Claude）：`IEnumerable<RbfFrame>` 在当前 C#/.NET 生态下不可实现，这是硬限制

2. **Duck-typed 枚举器模式是正确选择**（Claude）：跟随 `Span<T>` 的成熟先例，返回 `ref struct` 类型的 sequence/enumerator

3. **命名应避免虚假示能**（Gemini）：建议 `RbfReverseSequence` 而非 `RbfReverseEnumerable`，避免暗示实现了 `IEnumerable` 接口

4. **需要显式拷贝逃生通道**（Gemini）：现有 `PayloadToArray()` 方法满足需求，可考虑未来增加 `ToDto()` 便捷方法

5. **边界行为必须条款化**（GPT）：空序列、并发修改、多次枚举等场景必须明确，否则实现会分叉

6. **现有规范存在冲突**（GPT）：`rbf-interface.md` 中的 `: IEnumerable<RbfFrame>` 继承声明与 `ref struct` 约束矛盾，需修订

### 条款草案（待编入规范）

| 条款 ID | 内容摘要 |
|:--------|:---------|
| `[A-RBF-SCANREVERSE-SEQUENCE]` | 返回 duck-typed 可枚举序列，枚举器为 stack-only |
| `[S-RBF-SCANREVERSE-NO-IENUMERABLE]` | 返回类型 MUST NOT 实现 `IEnumerable<RbfFrame>` |
| `[S-RBF-SCANREVERSE-EMPTY-IS-OK]` | 空序列 MUST 产生 0 元素，不得抛异常 |
| `[S-RBF-SCANREVERSE-CURRENT-LIFETIME]` | `Current` 生命周期 ≤ 下次 `MoveNext()` |
| `[S-RBF-SCANREVERSE-CONCURRENT-MUTATION]` | 并发修改行为：fail-fast 或文档化为 UB |
| `[S-RBF-SCANREVERSE-MULTI-GETENUM]` | 多次 `GetEnumerator()` 返回独立枚举器 |

---

## 🎯 顾问组整体推荐方案

### 推荐：`ref struct` 序列 + Duck-typed 枚举器模式

**接口签名**：
```csharp
public interface IRbfScanner
{
    // ... 其他成员 ...
    
    /// <summary>
    /// 从文件尾部逆向扫描所有帧。
    /// </summary>
    /// <returns>帧序列（从尾到头）</returns>
    /// <remarks>
    /// 返回 duck-typed 可枚举序列，支持 foreach。
    /// 不实现 IEnumerable&lt;T&gt;（因 RbfFrame 是 ref struct）。
    /// LINQ 不可用；若需持久化数据，请调用 PayloadToArray()。
    /// </remarks>
    RbfReverseSequence ScanReverse();
}
```

**序列类型定义**：
```csharp
/// <summary>
/// 逆向扫描的帧序列（瞬态，stack-only）。
/// </summary>
public readonly ref struct RbfReverseSequence
{
    public Enumerator GetEnumerator() => new Enumerator(...);
    
    public ref struct Enumerator
    {
        public RbfFrame Current { get; }
        public bool MoveNext();
    }
}
```

### 推荐理由

| 维度 | 评估 |
|:-----|:-----|
| **类型安全** | ✅ 编译期阻止 use-after-free |
| **性能** | ✅ 零堆分配，与 `Span<T>` 生态一致 |
| **概念一致性** | ✅ 与 `RbfFrame` 是 `ref struct` 的设计匹配 |
| **DX 护栏** | ✅ 编译器错误引导正确使用 |
| **成熟先例** | ✅ `Span<T>`, `ReadOnlySpan<T>` 等 |

### 后续行动

1. **规范修订**：更新 `rbf-interface.md` 中 `ScanReverse()` 相关描述
   - 移除 `: IEnumerable<RbfFrame>` 继承声明
   - 添加 GPT 提出的 6 条条款
   - 调整 remarks 措辞（移除 LINQ 暗示）

2. **命名决策**：最终命名待监护人确认
   - 推荐 `RbfReverseSequence`（Gemini 建议）
   - 备选 `RbfReverseEnumerable`（需在文档中明确"不实现接口"）

3. **测试用例**：需覆盖空序列、多次枚举、生命周期边界等场景

---

## 📊 畅谈会状态

**状态**：✅ 已完成并实施  
**日期**：2025-12-28  
**产出**：顾问组整体推荐方案 + 6 条条款草案  
**后续**：
- ✅ 规范修订完成：`rbf-interface.md` v0.15
- ✅ 测试向量更新：`rbf-test-vectors.md` v0.9
- ✅ L1 复检通过（CodexReviewer）

---
