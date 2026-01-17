# 畅谈会：ReadFrame Buffer 生命周期设计问题

**日期**：2026-01-15
**标签**：`#design`
**主持**：TeamLeader
**参与者**：Craftsman, Seeker

---

## 背景

在实现 `RbfRawOps.ReadFrame` 时发现一个接口设计问题：

### 设计文档的意图（rbf-interface.md @[A-RBF-FRAME-STRUCT]）

```csharp
/// <summary>
/// RBF 帧数据结构。
/// </summary>
/// <remarks>
/// <para>只读引用结构，生命周期受限于产生它的 Scope（如 ReadFrame 的 buffer）。</para>
/// </remarks>
public readonly ref struct RbfFrame {
    public SizedPtr Ptr { get; init; }
    public uint Tag { get; init; }
    public ReadOnlySpan<byte> Payload { get; init; }
    public bool IsTombstone { get; init; }
}
```

设计意图：zero-copy，`Payload` 直接引用读取 buffer，生命周期受限于 scope。

### 当前实现的问题（RbfRawOps.ReadFrame.cs L194-201）

```csharp
// 重要：RbfFrame 是 ref struct，其 Payload 是 ReadOnlySpan<byte>。
// 如果使用了 ArrayPool，buffer 在返回后会被归还，Payload 引用将失效！
// 解决方案：复制 payload 到新数组。
byte[] payloadArray;
if (payloadLen > 0) {
    payloadArray = new byte[payloadLen];
    buffer.Slice(RbfConstants.PayloadFieldOffset, payloadLen).CopyTo(payloadArray);
}
```

**问题**：为了让 `RbfFrame` 在函数返回后可用，不得不进行：
1. 堆分配 `new byte[payloadLen]`
2. 全量复制 `CopyTo(payloadArray)`

这完全违背了 `ref struct` + `ReadOnlySpan<byte>` 的 zero-copy 设计初衷。

### 根本矛盾

| 选项 | Buffer 来源 | 问题 |
|:-----|:------------|:-----|
| stackalloc | 栈 | 函数返回后栈帧销毁，span 悬垂 |
| ArrayPool.Rent | 池 | 函数返回前必须 Return，span 悬垂 |
| new byte[] | 堆 | 可以返回，但每次都分配 ← 当前方案 |

`AteliaResult<RbfFrame>` 是值类型，返回时会复制。但 `RbfFrame.Payload` 是 `ReadOnlySpan<byte>`，它只是指针+长度，指向的内存必须在使用时有效。

---

## 待讨论问题

1. **接口签名是否需要修改？** 当前签名 `AteliaResult<RbfFrame> ReadFrame(SizedPtr ptr)` 是否从根本上无法实现 zero-copy？

2. **可能的替代方案**：
   - **方案 A**：Callback 模式 `void ReadFrame(SizedPtr ptr, Action<RbfFrame> handler)`
   - **方案 B**：Scoped 模式 `void ReadFrame(SizedPtr ptr, ref RbfFrameHandler handler)` 
   - **方案 C**：Buffer 外置 `RbfFrame ReadFrame(SizedPtr ptr, Span<byte> buffer)`
   - **方案 D**：接受堆分配，但优化小帧路径
   - **方案 E**：返回 `RbfFrameOwned`（非 ref struct，持有 byte[]）

3. **用户体验权衡**：
   - 监护人提到"功能上只要求 buffer 的可用性生存期和返回的 ref struct 一样就行"
   - 即：可以要求用户代码拿到数据后**立即使用**，不跨越 await/yield/方法边界

4. **ScanReverse 的启示**：
   - @[S-RBF-SCANREVERSE-CURRENT-LIFETIME] 已经约束了 `Current` 生命周期不超过下次 `MoveNext()`
   - 这是通过枚举器模式实现的，每次 `MoveNext()` 可以复用同一个 buffer

---

## 请各位参谋发言

请从以下角度分析：
1. 各方案的 API 易用性
2. 各方案的性能特征
3. 与现有 `AteliaResult` 模式的兼容性
4. 对上层调用者的影响

---

### Craftsman 发言

先明确结论：`AteliaResult<RbfFrame> ReadFrame(SizedPtr ptr)` 作为“返回一个可在函数外自由使用的值”的 API，几乎必然会迫使实现方选择“堆分配 + 复制”，否则就会引入悬垂引用风险（stackalloc / ArrayPool 都会在返回后失效）。要实现 zero-copy，API 必须把 **使用 `RbfFrame` 的时机限制在被控的同步 scope 内**（handler/visitor），或者把 buffer 的所有权交给返回值（Owned）。

同时要提醒一个工程现实：`Action<RbfFrame>` / `ReadOnlySpanAction<byte, RbfFrame>` 这类“把 `ref struct` 作为泛型委托参数”的写法在 C# 里大概率会直接被语言规则禁止（`ref struct` 不能作为多数泛型类型实参，尤其是 delegate/class 形态），因此方案 A 若要落地，需要用“自定义非泛型 delegate”或“泛型 state + span-action，但把 frame 拆成非-ref-struct 元信息”。

#### Tradeoff 分析表

| 方案 | API 易用性（调用者） | 性能（分配/复制） | 安全性（误用/悬垂） | 与 AteliaResult 兼容性 | 与 ScanReverse 一致性 |
|:--|:--|:--|:--|:--|:--|
| A Callback（`handler(frame)`） | 中：需要写 handler；对简单读用起来“多一层缩进” | 优：可做到 0 alloc / 0 copy（实现用 stackalloc 或复用 buffer） | 中-优：强制同步使用；但如果用错签名（泛型 Action）可能直接编译不过；也可能被低级 Unsafe 误用 | 中：`AteliaResult` 不能自然“包住 frame 返回”；更像 `AteliaResult ReadFrameScoped(..., handler)`；handler 自己的失败语义需要设计 | 优：与“Current 生命周期仅到下一次 MoveNext”同一精神（受控 scope） |
| B Scoped/Visitor（`ReadFrame<TState>(ptr, state, visitor)`） | 中：对初学者稍绕；但调用点可无闭包、无捕获、无分配 | 优：同 A，且更容易做“零分配传 state”（避免 closure） | 优：同步 + 受控生命周期；visitor 泛型可避免 boxing；更利于规约化 | 优：可以定义为 `AteliaResult ReadFrameScoped<TState>(..., visitor)`，错误统一由返回值承载；visitor 可返回 `AteliaResult` 以向上传播 | 优：最贴合枚举器/访问者模型（ScanReverse 可复用同一 visitor 约束） |
| C Buffer 外置（`ReadFrame(ptr, buffer)`） | 中-差：调用者要准备 buffer；还要处理 `buffer too small` 或先探测长度 | 优：0 alloc / 0 copy（payload 指向 caller buffer） | 优：生命周期清晰（由 caller buffer 决定）；但容易在“复用 buffer 后还继续用旧 frame”时踩坑（与 ScanReverse 的 Current 类似） | 优：仍可返回 `AteliaResult<RbfFrame>`（只要 frame 不逃逸）；但调用者拿到 frame 后能否跨越边界必须写清楚 | 优：与 ScanReverse 的“复用同一 buffer”一致；甚至 ScanReverse 内部就像在做 C |
| D 维持现状 + 小帧优化 | 优：最直观，当前调用点不变 | 中：小帧快；大帧仍分配+复制；实现复杂度上升（InlineArray/栈内小缓冲 + fallback） | 优：最不容易悬垂（返回 owned byte[]） | 优：完全沿用现有 `AteliaResult<RbfFrame>` 形态 | 中：ScanReverse 如果追求 zero-copy，会与此风格分裂；如果也走复制，则一致但牺牲热路径 |
| E 返回 Owned（`RbfFrameOwned` 持有 `byte[]`） | 优：最易用，返回值可自由存储/跨 async/跨方法 | 中-差：必然有分配；通常也有复制（除非底层本来就来自 byte[] 且可移交所有权） | 优：基本无悬垂；最“安全默认” | 优：天然契合 `AteliaResult<RbfFrameOwned>` | 中：与 ScanReverse 的零拷贝枚举器不一致，但可作为“便捷层”存在 |
| F 双 API（Scoped + Owned） | 优：调用者按场景选；热路径零拷贝，便捷路径拿 Owned | 优：热路径 0 alloc/0 copy；便捷路径付费 | 优：Scoped 明确同步生命周期；Owned 无悬垂 | 优：Owned 继续用 `AteliaResult<T>`；Scoped 用 `AteliaResult`（非泛型）或 `AteliaResult<TResult>`（TResult 非 ref struct） | 优：热路径与 ScanReverse 对齐；便捷层不干扰一致性 |

#### 推荐方案

我推荐 **方案 F（区分两种 API）**，其中“热路径”具体落地形态建议更接近 **方案 B（Scoped/Visitor）**。

理由（工程严谨性优先）：

1. **把“生命周期限制”做成 API 的一部分，而不是写在注释里**
   - 只要存在 `AteliaResult<RbfFrame>` 这种“把 frame 作为返回值带出 scope”的形式，团队迟早会在热路径上被迫复制，或者在维护中引入悬垂 bug。
   - Scoped API 强制同步使用（也天然阻止 async handler），这是把约束前移到类型系统/签名层面。

2. **双 API 让性能与易用性不互相伤害**
   - 业务/工具代码往往更想要 `Owned`（跨层传递、缓存、延迟处理）。
   - 底层热路径（ScanReverse、回放、索引维护）则必须 zero-copy。

3. **与 ScanReverse 的一致性最好**
   - ScanReverse 已经是“枚举器 Current 只在下次 MoveNext 前有效”的模型。
   - `ReadFrameScoped` 本质上是“单次枚举”，语义上完全一致：frame 只在 visitor/handler 返回前有效。

#### 建议的签名风格（不强制，但利于落地）

- Scoped（热路径）：
  - `AteliaResult ReadFrameScoped<TState>(SizedPtr ptr, TState state, RbfFrameVisitor<TState> visitor)`
  - visitor 建议返回 `AteliaResult` 以支持“在处理 frame 时失败并向上传播”（保持错误语义单一来源）。
  - 避免 `Action<RbfFrame>` 这类泛型委托实参为 ref struct 的写法；若用委托，优先自定义 `delegate`。

- Owned（便捷路径）：
  - `AteliaResult<RbfFrameOwned> ReadFrame(SizedPtr ptr)`
  - `RbfFrameOwned` 内持有 `byte[]`（或 `ImmutableArray<byte>`/`ReadOnlyMemory<byte>`），确保可跨 async。

#### 需要进一步讨论/澄清的问题

1. **Scoped API 的失败语义**：
   - handler/visitor 是否允许返回错误？如果允许，错误是“读取失败”还是“处理失败”，是否需要区分错误码域？（推荐都走 `AteliaResult`，但 error category 要清晰。）

2. **方案 C 是否需要作为“更底层的专家接口”保留**：
   - 如果上层确实想自己管理 buffer（例如批量处理、复用大缓冲区），`ReadFrameInto(buffer)` 很实用。
   - 但必须定义 `buffer too small` 的标准错误，以及“所需最小长度”的返回方式（例如在 error payload 中带 required size）。

3. **文档 SSOT 的生命周期条款**：
   - 建议把 Scoped 的精确定义写成与 ScanReverse 同风格的规范语句：
     - “`RbfFrame.Payload` 仅在 visitor 调用期间有效（或直到下一次读取覆盖同一 buffer）”。
   - 并明确：Scoped API **MUST NOT** 允许 async/await 边界（签名层面就不提供 `Task` 版本）。

4. **性能目标的量化**：
   - “小帧优化”只有在明确小帧占比、payload size 分布、以及分配/复制的热点证据后才值得做（否则 D 方案会成为复杂度债务）。
---

### Seeker 发言

Craftsman 的分析框架和 Tradeoff 表非常扎实。我想从**概念边界**和**先例模式**的角度补充一些探索性思考。

#### 一、.NET 生态中的三个先例

**1. System.IO.Pipelines 的 ReadResult + ReadOnlySequence**

这是 .NET 中最成熟的 zero-copy 读取模式。核心洞见：

- `PipeReader.ReadAsync()` 返回 `ReadResult`，其中包含 `ReadOnlySequence<byte>`
- **关键机制**：调用者必须调用 `AdvanceTo()` 告知 pipe "我看完了哪些数据"
- 在 `AdvanceTo()` 之前，buffer 保持有效；之后，buffer 可能被回收复用

这是一种**显式借还协议**——你借走 buffer 的"视图"，用完后必须归还。与我们讨论的 `RbfFrame` 生命周期问题本质相同，但 Pipelines 选择了"API 层面强制交还"。

**问：** RBF 的 ReadFrame 是否需要类似的"借还"机制？还是 ScanReverse 的"隐式归还（MoveNext 时）"已经够用？

**2. Utf8JsonReader 与 JsonDocument 的分工**

这是"同一数据，两种访问模式"的经典案例：

| 类型 | 性质 | 生命周期 | 用途 |
|:-----|:-----|:---------|:-----|
| `Utf8JsonReader` | ref struct | scoped（不能存储） | 流式解析，zero-copy |
| `JsonDocument` | class | owned（需 Dispose） | 可存储、可遍历的 DOM |

`Utf8JsonReader` 明确告诉你："我是临时的，别想把我存起来"。而 `JsonDocument.Parse()` 会**复制一份数据**，换取随时随地可用的 DOM。

这与 Craftsman 的"方案 F 双 API"不谋而合：热路径用 scoped，便捷路径用 owned。

**3. Span<T> 与 Memory<T> 的分工**

这是最根本的设计哲学：

- `Span<T>` = "我是当前栈帧的局部视图，离开 scope 就失效"
- `Memory<T>` = "我持有对堆内存的引用，可以自由传递"

**核心启示**：.NET 的设计者没有试图让 Span 万能，而是接受了"有些场景必须用 Memory"。RBF 是否也需要类似的哲学接受？

#### 二、用户场景分析——"拿到帧后做什么？"

我尝试列举 `ReadFrame` 的典型使用场景：

| 场景 | 用户意图 | 对生命周期的需求 |
|:-----|:---------|:-----------------|
| **立即解码** | 读帧 → 解析 payload → 丢弃原始字节 | scoped 够用 |
| **条件判断** | 读帧 → 检查 Tag/Payload 首字节 → 决定下一步 | scoped 够用 |
| **缓存/索引** | 读帧 → 存入 `Dictionary<SizedPtr, RbfFrame>` | **需要 owned** |
| **跨方法传递** | 读帧 → 传给另一个方法处理 | 取决于调用链深度 |
| **异步处理** | 读帧 → await 某操作 → 继续用 payload | **需要 owned** |

观察：**大多数"读帧"是为了"立即处理"**。需要 owned 的场景往往是上层架构决策（缓存策略、异步模式）导致的。

**问：** 这是否意味着 scoped 才是"本色"，owned 是"升档"？

#### 三、激进思考——回到"ref struct RbfFrame"的设计初衷

我想追问一个更根本的问题：

> **`RbfFrame` 为什么是 ref struct？**

从 [rbf-interface.md](rbf-interface.md) 的注释来看：
> "只读引用结构，生命周期受限于产生它的 Scope"

这暗示设计意图是：**RbfFrame 不应该逃逸出它的产生上下文**。

但当前 `ReadFrame` 的签名 `AteliaResult<RbfFrame> ReadFrame(SizedPtr ptr)` 恰恰是**要求 RbfFrame 逃逸**——你返回它，就是想让调用者在函数外使用它。

这里存在一个**设计张力**：
- `ref struct` 在类型系统层面说："我不能逃逸"
- 方法签名在 API 层面说："我要你逃逸"

**假设 A：如果我们接受"ReadFrame 总是复制"**

那 `RbfFrame` 还需要是 ref struct 吗？

- 如果 `Payload` 是 `byte[]` 而非 `ReadOnlySpan<byte>`，整个 `RbfFrame` 就可以是普通 `readonly struct`
- 这会让 API 更简单、更直观，但放弃了 zero-copy 可能性

**假设 B：如果我们坚持 zero-copy 的设计初衷**

那 `ReadFrame` 的签名本身就是错误的——返回 `RbfFrame` 这件事就违背了 ref struct 的存在意义。

正确的问法应该是："你想对这个帧做什么？"——然后在**你告诉我之后**，我帮你做。

这正是 Visitor/Callback 模式的哲学基础。

#### 四、一个类比：`File.ReadAllBytes` vs `FileStream`

.NET 提供了两种读文件的方式：

```csharp
// 简单但分配
byte[] data = File.ReadAllBytes(path);

// 复杂但可控
using var fs = new FileStream(path, FileMode.Open);
fs.Read(buffer, 0, count);
```

没有人抱怨 `ReadAllBytes` "不够 zero-copy"——它的存在是为了**便利性**。

**类比到 RBF**：
- `ReadFrame(ptr)` → 返回 owned，简单直接（类似 ReadAllBytes）
- `ReadFrameScoped(ptr, visitor)` → scoped 回调，性能极致（类似 FileStream）

两者不是竞争关系，是**不同抽象层次的选择**。

#### 五、待澄清的概念问题

1. **"zero-copy"是目标还是手段？**
   - 如果是目标：需要量化收益（payload 平均多大？读帧频率多高？）
   - 如果是手段：目标是什么？是"低延迟"还是"低内存压力"？

2. **ScanReverse 与 ReadFrame 的一致性需求有多强？**
   - ScanReverse 已经采用 "Current 只在 MoveNext 前有效" 的模式
   - ReadFrame 是否必须与之一致？还是可以因为"用户意图不同"而选择不同模式？

3. **`AteliaResult<T>` 对 ref struct 的支持边界在哪里？**
   - 小黑板提到 "AteliaResult 双类型架构：ref struct + readonly struct"
   - 当前的 `AteliaResult<RbfFrame>` 是哪种？它的实现是否已经被 ref struct 特性约束？

#### 六、我的倾向

基于以上分析，我认同 Craftsman 的方案 F，但想强调一个认知框架：

> **问题不是"怎么让 ReadFrame 实现 zero-copy"，而是"ReadFrame 这个 API 的存在意义是什么"**

如果 ReadFrame 的存在是为了"便捷地获取帧数据供后续使用"，那它**本质上就应该返回 owned 数据**——这不是妥协，是正确的抽象定位。

而 zero-copy 的需求应该由 **另一个 API** 来满足——一个签名上就明确表达"你只能在我的回调里用"的 API。

**命名建议**（供后续讨论）：
- `ReadFrame(ptr)` → 返回 `RbfFrameOwned`，便捷优先
- `VisitFrame(ptr, visitor)` 或 `WithFrame(ptr, action)` → scoped 回调，性能优先

这样的命名让用户从 API 名字就能感知到生命周期差异——"Read"暗示"拿走"，"Visit/With"暗示"借用"。

---

### TeamLeader 收敛总结

感谢 Craftsman 和 Seeker 的深入分析。两位的视角互补：Craftsman 从工程严谨性出发给出了完整的 Tradeoff 表和落地建议；Seeker 从概念边界出发追问了"ref struct RbfFrame 的设计初衷"这个根本问题。

#### 核心洞见

Seeker 的追问点到了要害：

> **`ref struct` 在类型系统层面说"我不能逃逸"，而 `ReadFrame` 的签名在 API 层面要求它逃逸。**

这是设计文档的内在矛盾。当时写 `@[A-RBF-FRAME-STRUCT]` 定义 `RbfFrame` 为 `ref struct` + `ReadOnlySpan<byte> Payload`，是为了 zero-copy 的可能性。但同时 `@[A-RBF-IRBFFILE-SHAPE]` 定义的 `ReadFrame` 返回 `AteliaResult<RbfFrame>`，隐含了"返回值可在函数外使用"的语义承诺。

两者不可兼得——这正是当前实现被迫堆分配+复制的根因。

#### 决议

**采用方案 F：双 API 分层**

| API | 返回类型 | 生命周期 | 用途 |
|:----|:---------|:---------|:-----|
| `ReadFrame(ptr)` | `AteliaResult<RbfFrameOwned>` | owned，可自由使用 | 便捷路径，大多数场景 |
| `VisitFrame(ptr, visitor)` | `AteliaResult` | scoped，仅在 visitor 内有效 | 热路径，zero-copy |

**理由**：
1. **让命名承载语义**："Read"暗示"拿走"，"Visit"暗示"借用"
2. **与 ScanReverse 一致**：ScanReverse 的枚举器模式就是 scoped（Current 只在 MoveNext 前有效），VisitFrame 是"单次访问"版本
3. **不强迫用户为性能付出易用性代价**：绝大多数场景用 owned 即可，热路径自选 scoped

#### 待办事项

1. **修订 rbf-interface.md**：
   - 修改 `@[A-RBF-IRBFFILE-SHAPE]` 中的 `ReadFrame` 签名
   - 新增 `VisitFrame` API 定义
   - 新增 `RbfFrameOwned` 类型定义
   - 更新 `@[A-RBF-FRAME-STRUCT]` 注释，明确 `RbfFrame` 只用于 scoped 场景

2. **修改当前实现**：
   - `RbfRawOps.ReadFrame` → 返回 `RbfFrameOwned`（保持当前的复制行为，但类型语义正确）
   - 新增 `RbfRawOps.VisitFrame` → scoped visitor 模式，zero-copy

3. **后续优化机会**（非阻塞）：
   - 小帧优化（InlineArray）——需要先收集 payload size 分布数据

#### 会议产出

- **候选方案**：A–F 六种，见 Craftsman 分析表
- **最终决策**：方案 F（双 API）
- **状态**：待监护人确认后执行

---
