# 畅谈会：RbfFrame IDisposable 设计 — ArrayPool Buffer 生命周期管理

**日期**：2026-01-16
**标签**：`#design` `#usability`
**主持**：TeamLeader
**参与者**：Craftsman, Implementer

---

## 背景

当前 Stage 05 设计确立了 `ReadFrameInto(file, ptr, buffer)` 作为核心 API，调用方负责提供 buffer。这是 zero-copy 热路径的正确设计。

但监护人提出了一个易用性增强思路：

> 让 `RbfFrame` 实现 `IDisposable` 接口并增加一个可控字段用于携带"拥有"的来自 `ArrayPool<byte>` 的 buffer。在 `Dispose` 时归还给 `ArrayPool`。
>
> 目的是为 `RbfReadImpl` 和 `RbfFileImpl` 实现一个**无需传入 buffer 的 `ReadFrame` 方法**，方法内始终从 `ArrayPool` 借缓存，并在返回 `RbfFrame` 时设置 buffer ownership。
>
> 从而在 `ReadFrameInto` 之外，也提供一个**易用性和性能平衡**的接口。

### 当前 RbfFrame 结构

```csharp
public readonly ref struct RbfFrame {
    public SizedPtr Ptr { get; init; }
    public uint Tag { get; init; }
    public ReadOnlySpan<byte> Payload { get; init; }
    public bool IsTombstone { get; init; }
}
```

### 先例：RbfFrameBuilder

`RbfFrameBuilder` 已经是 `ref struct` 实现 `IDisposable`，证明了这种模式是可行的：

```csharp
public ref struct RbfFrameBuilder : IDisposable {
    private bool _disposed;
    private bool _committed;
    
    public void Dispose() {
        if (_disposed) { return; }
        _disposed = true;
        if (!_committed) {
            // Auto-Abort 逻辑
        }
    }
}
```

---

## 待讨论问题

### Q1: RbfFrame 携带 ArrayPool buffer 的字段设计

**候选 A**：nullable byte[] 字段
```csharp
public ref struct RbfFrame : IDisposable {
    public SizedPtr Ptr { get; init; }
    public uint Tag { get; init; }
    public ReadOnlySpan<byte> Payload { get; init; }
    public bool IsTombstone { get; init; }
    
    // 新增：可选的 owned buffer
    private byte[]? _ownedBuffer;
    
    public void Dispose() {
        if (_ownedBuffer is not null) {
            ArrayPool<byte>.Shared.Return(_ownedBuffer);
            _ownedBuffer = null;
        }
    }
}
```

**候选 B**：使用 MemoryHandle / Memory&lt;byte&gt;

**候选 C**：引入独立的 RbfFrameOwned 类型

### Q2: 新 ReadFrame API 签名

**目标**：无需调用方传入 buffer，内部从 ArrayPool 借缓存

```csharp
// 候选签名
AteliaResult<RbfFrame> ReadFrame(SafeFileHandle file, SizedPtr ptr);
// 或放在 IRbfFile 上
AteliaResult<RbfFrame> ReadFrame(SizedPtr ptr);
```

**问题**：
- 返回的 `RbfFrame` 是 `ref struct`，无法存储，调用方必须立即使用或 Dispose
- 这是期望的行为吗？还是需要提供可存储版本？

### Q3: ReadFrameInto 与 ReadFrame 的关系

- `ReadFrameInto`：buffer 外置，返回的 `RbfFrame` **不拥有** buffer
- `ReadFrame`：buffer 内部分配，返回的 `RbfFrame` **拥有** buffer

**问题**：
- `ReadFrameInto` 返回的 `RbfFrame` 是否也应该实现 `IDisposable`（但 Dispose 是 no-op）？
- 还是提供两种不同的类型？

### Q4: 与 readonly ref struct 的兼容性

当前 `RbfFrame` 是 `readonly ref struct`。如果需要在 `Dispose` 中修改 `_ownedBuffer = null`，则需要：
- 移除 `readonly` 修饰符，或
- 使用其他设计模式

---

## Craftsman 发言

#### 0. 先定基调：我们在解决两个"生命周期模型"
- **非 owning（外部 buffer）**：`ReadFrameInto(span)` 只做解析，**不管** buffer 生命周期；返回值只是"视图"。
- **owning（内部租借）**：`ReadFrame*(ptr)` 自己从池里拿 buffer，并把"归还责任"随返回值一起交出去（`Dispose` 归还）。

这两个模型可以共存，但我强烈建议：**API 命名必须把"是否 owning"表达出来**，否则使用方很容易踩"用后归还/忘记归还"的坑。

---

#### 1) API 设计：易用性 / 安全性 / 一致性
- 如果把 owning 语义塞进同一个 `RbfFrame` 里，调用方会直觉地把它当"纯数据结构"用（复制、缓存、跨层传递），但 owning 需要"资源语义"（using/Dispose），这两者心智模型冲突。
- 因此我建议至少做到二选一（按你们更愿意破坏/重命名哪个 API）：
  - **方案 X（安全优先）**：保留"当前 `ReadFrame` = 复制 payload（无需 Dispose）"的语义，再新增一个明确 owning 的方法，比如 `ReadFramePooled` / `ReadPooledFrame`。
  - **方案 Y（性能优先）**：把 `ReadFrame` 改成 owning（必须 Dispose），并把旧语义改名为 `ReadFrameCopy`（或 `ReadFrameDetached`），让"默认路径"也变成可控的高性能路径。
- 一致性上：你们已经有 `RbfFrameBuilder : IDisposable` 的先例，所以把"读出来的 frame 也是 disposable"是统一的；但要避免"看起来像值，行为像资源"的惊喜。

---

#### 2) C# 语言约束：`ref struct` + `IDisposable` + `readonly`
- 结论：在当前工程（`net9.0`）里，`ref struct : IDisposable` 已经被 `RbfFrameBuilder` 证明是可行的；`using var` 会走静态绑定调用 `Dispose()`，不会涉及装箱。
- 关键限制点不在"能不能实现接口"，而在：
  - **不能被当作接口类型使用**：`IDisposable d = frame;` 不行（装箱/逃逸问题）。所以 API/内部代码应避免返回/存储为 `IDisposable`。
  - **不能跨 `await`**：`ref struct` 不能存在于 async 状态机里。调用方在 `using` 作用域内 `await` 会直接编译报错，这是好事（帮你们"硬性约束生命周期"）。
- `readonly ref struct` 的兼容性是本议题的核心：  
  - 若 `Dispose()` 里需要把 `_ownedBuffer = null` 或 `_disposed = true`，就必须去掉 `readonly`。  
  - 但**去掉 `readonly` 并不能解决最危险的问题：复制导致的双重归还**（见下文陷阱）。

---

#### 3) 设计模式：候选 A/B/C 的取舍（我倾向于"带 owner token 的变体 C'"）
**候选 A（直接 `byte[]? _ownedBuffer`）我认为风险最高：**
- 最大坑：`struct` 可被复制，`var b = a;` 会复制 `_ownedBuffer` 引用；如果两个副本都 `Dispose()`，会把同一个数组 `Return()` 两次 —— 这在语义上等价于 double-free，会把池污染到不可预测状态。
- 你可以加 `_disposed` 防重入，但**防不了"副本各自认为自己没 dispose"**。C# 没有 move-only 语义，靠约定很脆。

**候选 B（Memory/MemoryHandle/IMemoryOwner）更接近正确方向：**
- 重点不是 `MemoryHandle` 本身，而是引入一个**共享的、可幂等释放的 owner 对象**（引用类型），让多个副本 Dispose 也只释放一次。

**候选 C（独立 owning 类型）或 C'（frame 内嵌 owner token）我认为最稳：**
- C：新增 `RbfFrameOwned` / `RbfFrameLease`（class），里面持有 owner + `RbfFrame` 视图；`RbfFrame` 继续保持"纯视图、无资源语义"。这是最清晰的分层。
- C'：满足"让 `RbfFrame` 自己可 Dispose"的同时，解决复制问题：  
  - `RbfFrame` 持有一个私有 `sealed class ArrayPoolLease`（里面有 `byte[]` + 原子状态），`Dispose()` 委托给它；lease 的 `Dispose` 用 `Interlocked.Exchange` 保证只 Return 一次。  
  - 这样即便 `RbfFrame` 被复制，多个副本共享同一个 lease，依然是安全的。  
  - 在这个方案里，`RbfFrame` **可以继续保持 `readonly ref struct`**（不需要给字段赋 null）。

---

#### 4) 与现有 `ReadFrameInto` 的协同：如何共存不打架
- 我建议保持一个非常明确的约束：
  - `ReadFrameInto(handle, ptr, Span<byte> buffer)`：**永远不拥有**，返回的 frame **永远不负责归还**。
  - `ReadFrame*(ptr)`：**永远拥有**（或至少"可能拥有"但必须可 `Dispose`），并且在失败路径中自行归还（调用方拿不到就不该承担释放）。
- 这会带来一个实现纪律：`ReadFrame*(ptr)` 内部租借的 buffer 不应再被复制到"独立 payload"里（否则你又回到旧行为）；要么就明确叫 `ReadFrameCopy`。

---

#### 5) 命名建议：把 owning 写进名字（避免"默认坑"）
- 如果你们选择"保持旧 `ReadFrame` = copy"：
  - `ReadFrame`：返回可长期持有的 payload（复制/脱离池），不需要 Dispose
  - `ReadFramePooled` / `ReadPooledFrame` / `ReadFrameLease`：返回必须 Dispose 的 frame
- 如果你们选择"重构把 `ReadFrame` 变成 owning"：
  - `ReadFrame`：必须 Dispose（性能路径）
  - `ReadFrameCopy` / `ReadFrameDetached`：显式复制（安全路径）
- 我个人更偏好 `Pooled` / `Lease` 这种词，因为它们直接提示"资源借用"的心智模型；`Owned` 容易让人误会成"我拥有 payload 的长期所有权"。

---

#### 6) 潜在陷阱（我认为需要在设计里主动"挡掉"的）
1. **复制导致 double-return（最致命）**：如果走候选 A，一定会有人写出 `var x = frame;` 并在两个路径 Dispose。建议用"owner token 幂等释放"彻底消除。
2. **Dispose 后继续读 Payload（use-after-return）**：数组被归还后可能立刻被复用并改写，读到的数据会悄悄变脏。建议在文档/命名上强提醒；如有条件，DEBUG 下可以让 owner 记录状态并在访问时抛异常（代价需评估）。
3. **忘记 Dispose（性能退化/池失效）**：这不会立刻内存泄漏，但会让 ArrayPool 的复用失效、导致更多分配与 GC 压力。命名 + `using var` 习惯很关键。
4. **"持久化保存 payload"误用**：调用方如果要跨作用域保存，应显式 `Payload.ToArray()`；最好提供辅助方法 `CopyPayload()` 之类（可选）。
5. **清零策略**：`ArrayPool.Return(array, clearArray: ...)` 是否清零涉及性能与信息残留取舍；如果 payload 可能含敏感数据，安全默认应倾向清零（或至少提供可配置策略）。

---

**我的结论（可执行建议）**  
- 不推荐候选 A 的"裸 `byte[]?` owned 字段"，因为无法从类型层面防止复制导致的 double-return。  
- 推荐 **C'：`RbfFrame` 内部持有一个幂等释放的 owner token（引用类型）**，这样既能满足"`RbfFrame : IDisposable`"，又能保留 `readonly ref struct`，并从根上消除双重归还风险。  
- API 命名上，建议至少引入 `ReadFramePooled`（或把 `ReadFrame` 改成 owning 并提供 `ReadFrameCopy`），让"是否需要 Dispose"一眼可见。

---

## Implementer 发言

从实现角度分析 C' 方案（owner token 模式）的具体落地策略。

#### 1. Owner Token 类设计

```csharp
// atelia/src/Rbf/Internal/PooledBufferOwner.cs
internal sealed class PooledBufferOwner {
    private byte[]? _buffer;
    
    public PooledBufferOwner(byte[] buffer) {
        _buffer = buffer;
    }
    
    /// <summary>幂等释放：多次调用安全，只有第一次生效。</summary>
    public void Release() {
        var buf = Interlocked.Exchange(ref _buffer, null);
        if (buf is not null) {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }
}
```

**设计要点**：
- **引用类型**：解决 struct 复制导致的 double-return 问题——所有复制共享同一 owner 实例
- **Interlocked.Exchange**：保证线程安全的幂等释放，即使并发调用也只归还一次
- **Internal**：不暴露给外部，只作为实现细节

#### 2. RbfFrame 修改方案

关键发现：**可以保留 `readonly ref struct`**！

```csharp
public readonly ref struct RbfFrame : IDisposable {
    public SizedPtr Ptr { get; init; }
    public uint Tag { get; init; }
    public ReadOnlySpan<byte> Payload { get; init; }
    public bool IsTombstone { get; init; }
    
    // 新增：可选的 owner token（internal set）
    internal PooledBufferOwner? Owner { get; init; }
    
    public void Dispose() {
        Owner?.Release();  // 不修改字段值，只调用引用对象方法
    }
}
```

**为什么能保留 `readonly`**：
- `Dispose()` 中的 `Owner?.Release()` 只是调用引用类型的方法
- 不修改 `RbfFrame` 自身的任何字段值
- `readonly` 约束的是字段赋值，不阻止通过引用调用方法

这与 `RbfFrameBuilder` 不同——后者在 `Dispose()` 中需要 `_disposed = true`，所以不能是 `readonly`。

#### 3. ReadFramePooled 实现

```csharp
// atelia/src/Rbf/Internal/RbfReadImpl.cs

/// <summary>从 ArrayPool 借缓存读取帧。调用方 MUST 调用 Dispose() 归还 buffer。</summary>
public static AteliaResult<RbfFrame> ReadFramePooled(SafeFileHandle file, SizedPtr ptr) {
    // 1. 复用现有校验（不检查 buffer 长度）
    var error = CheckReadParamsWithoutBuffer(ptr);
    if (error != null) {
        return AteliaResult<RbfFrame>.Failure(error);
    }
    
    // 2. 从 ArrayPool 借 buffer
    int lengthBytes = (int)ptr.LengthBytes;
    byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(lengthBytes);
    
    // 3. 调用 ReadFrameIntoUnchecked
    var result = ReadFrameIntoUnchecked(file, ptr, rentedBuffer.AsSpan());
    
    // 4. 失败路径：立即归还 buffer
    if (!result.IsSuccess) {
        ArrayPool<byte>.Shared.Return(rentedBuffer);
        return result;
    }
    
    // 5. 成功路径：创建 owner 并重建 frame
    var owner = new PooledBufferOwner(rentedBuffer);
    var frame = result.Value;
    
    return AteliaResult<RbfFrame>.Success(new RbfFrame {
        Ptr = frame.Ptr,
        Tag = frame.Tag,
        Payload = frame.Payload,  // 仍指向 rentedBuffer 的切片
        IsTombstone = frame.IsTombstone,
        Owner = owner
    });
}

/// <summary>不含 buffer 长度校验的参数检查（提取自 CheckReadParams）。</summary>
private static AteliaError? CheckReadParamsWithoutBuffer(SizedPtr ptr) {
    // 复用 CheckReadParams 中除 buffer 长度外的所有校验
    // ...
}
```

**实现要点**：
- 失败路径必须归还 buffer，避免泄漏
- 成功时重建 `RbfFrame` 以附加 `Owner`（值类型特性）
- `PayloadAndMeta` 继续指向 `rentedBuffer` 的切片，生命周期由 owner 管理

#### 4. 与 ReadFrameInto 的关系

| API | Owner 值 | Dispose 行为 |
|:----|:---------|:-------------|
| `ReadFrameInto` | `null` | No-op（安全） |
| `ReadFramePooled` | `new PooledBufferOwner(...)` | 归还 buffer |

**ReadFrameInto 完全不变**：返回的 frame 的 `Owner` 为 `null`，调用 `null?.Release()` 是安全的 no-op。

调用方可以统一用 `using` 包装两种 API 的返回值，代码风格一致：

```csharp
// 两种用法都安全
using var frame1 = RbfReadImpl.ReadFrameInto(file, ptr, buffer).Value;
using var frame2 = RbfReadImpl.ReadFramePooled(file, ptr).Value;
```

#### 5. 测试关键点

```csharp
public class RbfFrameDisposeTests {
    [Fact]
    public void Dispose_WhenOwnerIsNull_NoOp() {
        var frame = new RbfFrame { Owner = null };
        frame.Dispose();  // 第一次
        frame.Dispose();  // 第二次，仍安全
    }
    
    [Fact]
    public void Dispose_WhenOwnerPresent_ReturnsBufferOnce() {
        var buffer = ArrayPool<byte>.Shared.Rent(100);
        var owner = new PooledBufferOwner(buffer);
        var frame = new RbfFrame { Owner = owner };
        
        frame.Dispose();  // 归还
        frame.Dispose();  // 幂等，不 double-return
    }
    
    [Fact]
    public void Copy_ThenBothDispose_NoDoubleReturn() {
        // 关键：验证 owner token 模式解决 struct 复制问题
        var buffer = ArrayPool<byte>.Shared.Rent(100);
        var owner = new PooledBufferOwner(buffer);
        
        var frame1 = new RbfFrame { Owner = owner };
        var frame2 = frame1;  // 复制！
        
        frame1.Dispose();  // 归还
        frame2.Dispose();  // 共享同一 owner，幂等安全
    }
    
    [Fact]
    public void Dispose_ThreadSafe() {
        var buffer = ArrayPool<byte>.Shared.Rent(100);
        var owner = new PooledBufferOwner(buffer);
        var frame = new RbfFrame { Owner = owner };
        
        Parallel.For(0, 100, _ => frame.Dispose());
        // Interlocked.Exchange 保证只归还一次
    }
}
```

#### 6. 工作量预估

| 工作项 | 预估时间 | 说明 |
|:-------|:---------|:-----|
| `PooledBufferOwner` 类 | 15 min | 核心逻辑简单 |
| `RbfFrame` 修改 | 10 min | 添加 Owner + IDisposable |
| `CheckReadParamsWithoutBuffer` | 15 min | 从 CheckReadParams 提取 |
| `ReadFramePooled` 方法 | 30 min | 包含失败路径处理 |
| 测试用例 | 45 min | 含并发测试 |
| **总计** | **约 2 小时** | |

#### 7. 潜在陷阱与规避

| 陷阱 | 规避措施 |
|:-----|:---------|
| Dispose 后访问 Payload | **文档警告**：Dispose 后 Payload 变 dangling，这是 by design |
| 调用方忘记 Dispose | **Analyzer 规则**（可选）：检测 `ReadFramePooled` 未包裹在 `using` 中 |
| ArrayPool.Rent 返回更大 buffer | **无影响**：Return 时传原 buffer 引用即可 |

#### 8. 命名确认

支持 `ReadFramePooled` 命名：
- 明确表达"使用 pool"的语义
- 与 `ReadFrameInto` 形成对比（Into = 外部 buffer，Pooled = 内部池）
- 不推荐 `ReadFrame`：易混淆哪个是 owning

---

**结论**：C' 方案可行，核心是 `PooledBufferOwner` 的幂等释放设计。能保留 `readonly ref struct`，实现复杂度低，约 2 小时可完成。随时可以开工。

---

## TeamLeader 收敛总结

感谢 Craftsman 和 Implementer 的深入分析！两位视角高度互补：

- **Craftsman** 从 API 设计和安全性角度，揭示了候选 A（裸 `byte[]?`）的致命陷阱（复制导致 double-return），并提出了 C'（owner token）方案的核心思路
- **Implementer** 从实现角度验证了方案可行性，给出了完整的代码示例和测试策略

### 核心共识

| 决策点 | 共识 |
|:-------|:-----|
| **RbfFrame 能否保留 `readonly ref struct`** | ✅ 可以！`Dispose()` 只调用 `Owner?.Release()`，不修改字段 |
| **如何防止 double-return** | Owner Token（引用类型）+ `Interlocked.Exchange` 幂等释放 |
| **API 命名** | `ReadFramePooled`（明确 owning 语义）vs `ReadFrameInto`（非 owning） |
| **与 ReadFrameInto 关系** | 完全不变，Owner = null 时 Dispose 是安全的 no-op |

### 最终决策（待监护人确认）

**推荐方案：C'（Owner Token 模式）**

```csharp
// 1. 新增 internal 类
internal sealed class PooledBufferOwner {
    private byte[]? _buffer;
    public void Release() { /* Interlocked.Exchange + Return */ }
}

// 2. RbfFrame 修改
public readonly ref struct RbfFrame : IDisposable {
    // ... 现有字段 ...
    internal PooledBufferOwner? Owner { get; init; }
    public void Dispose() => Owner?.Release();
}

// 3. 新增 API
public static AteliaResult<RbfFrame> ReadFramePooled(SafeFileHandle file, SizedPtr ptr);
```

### API 使用体验

```csharp
// 场景 1: 高性能热路径（调用方管理 buffer）
Span<byte> buffer = stackalloc byte[ptr.LengthBytes];
var result = RbfReadImpl.ReadFrameInto(file, ptr, buffer);
// 不需要 Dispose，buffer 生命周期由调用方控制

// 场景 2: 易用性优先（自动池化）
using var frame = RbfReadImpl.ReadFramePooled(file, ptr).Value;
// Dispose 自动归还 buffer

// 两种场景都可以用 using，代码风格一致
using var frame1 = RbfReadImpl.ReadFrameInto(file, ptr, buffer).Value;  // Dispose = no-op
using var frame2 = RbfReadImpl.ReadFramePooled(file, ptr).Value;        // Dispose = 归还
```

### 后续行动

**如果监护人确认可执行**：
1. 创建 `PooledBufferOwner` 类
2. 修改 `RbfFrame` 添加 `Owner` 字段和 `IDisposable` 实现
3. 实现 `ReadFramePooled` 方法
4. 添加 double-dispose 和并发 Dispose 测试
5. 更新 `IRbfFile` 接口和 `RbfFileImpl` 门面

**预估工作量**：约 2 小时

### 会议产出

- **候选方案**：A（裸 byte[]?）、B（Memory/IMemoryOwner）、C（独立类型）、C'（owner token）
- **最终决策**：C'（owner token 模式）
- **状态**：待监护人确认后执行

---

## 监护人决策（2026-01-16）

监护人赞同 AI Team 的分析和共识，并提出更保守的改进：

**决策：将带 Disposable 的 RbfFrame 作为独立新类型实现**

| 类型 | 职责 | 资源语义 |
|:-----|:-----|:---------|
| `RbfFrame` | 纯数据结构（保持现状） | 无，不需要 Dispose |
| `RbfPooledFrame` | 携带 ArrayPool buffer 的帧 | 有，MUST Dispose |

**理由**：
- 完全不牵连 `ReadFrameInto` 路径
- 用户代码自己管理 buffer 的场景走 `ReadFrameInto` 可以做到极致性能
- 图方便的走 `using var frame = ReadPooledFrame(...)` 

**最终 API 形态**：

```csharp
// 路径 1: 极致性能（调用方管理 buffer）
Span<byte> buffer = stackalloc byte[ptr.LengthBytes];
var result = RbfReadImpl.ReadFrameInto(file, ptr, buffer);
// RbfFrame 无需 Dispose

// 路径 2: 易用性优先（自动池化）
using var frame = RbfReadImpl.ReadPooledFrame(file, ptr).Value;
// RbfPooledFrame 必须 Dispose
```

**状态**：已确认，开始实施

---

## 实施记录（2026-01-16）

### 实施完成

Implementer 完成了以下文件：

| 文件 | 操作 | 说明 |
|:-----|:-----|:-----|
| `src/Rbf/Internal/PooledBufferOwner.cs` | 新建 | ArrayPool buffer 幂等释放器 |
| `src/Rbf/RbfPooledFrame.cs` | 新建 | 携带 Owner 的 readonly ref struct |
| `src/Rbf/Internal/RbfReadImpl.cs` | 修改 | 新增 `ReadPooledFrame` + `CheckReadParamsCore` |
| `src/Rbf/IRbfFile.cs` | 修改 | 添加 `ReadPooledFrame` 接口方法 |
| `src/Rbf/Internal/RbfFileImpl.cs` | 修改 | 实现 `ReadPooledFrame` 转发 |
| `tests/Rbf.Tests/RbfPooledFrameTests.cs` | 新建 | 8 个测试用例 |

### Code Review 结果

**审阅者**：Craftsman
**结论**：NEEDS_REVISION（已修复）

#### P0 问题（已修复）

1. **读取长度错误**：`ReadFrameIntoUnchecked` 中 `RandomAccess.Read(file, buffer, ...)` 应该是 `RandomAccess.Read(file, frameBuf, ...)`
   - **修复**：改为传 `frameBuf`

2. **ReadPooledFrame 传入整段数组**：`rentedBuffer.AsSpan()` 未限定长度
   - **修复**：改为 `rentedBuffer.AsSpan(0, lengthBytes)`

3. **异常路径 buffer 泄漏**：IO 异常时 buffer 未归还
   - **修复**：添加 try/catch，异常路径中归还 buffer

#### P1 问题（已记录决策）

- **ref struct : IDisposable**：这是有意的设计选择，已在文档中说明理由

#### P2 问题（记录待改进）

- **ArrayPool.Return 清零策略**：敏感数据场景可考虑 clearArray 选项
- **测试补强**：可增加并发 Dispose 测试

### 最终测试结果

**156 个测试全部通过**


### Implementer 发言

从实现角度分析 C' 方案（owner token 模式）的具体落地策略。

#### 1. Owner Token 类设计

```csharp
// atelia/src/Rbf/Internal/PooledBufferOwner.cs
internal sealed class PooledBufferOwner {
    private byte[]? _buffer;
    
    public PooledBufferOwner(byte[] buffer) {
        _buffer = buffer;
    }
    
    /// <summary>幂等释放：多次调用安全，只有第一次生效。</summary>
    public void Release() {
        var buf = Interlocked.Exchange(ref _buffer, null);
        if (buf is not null) {
            ArrayPool<byte>.Shared.Return(buf);
        }
    }
}
```

**设计要点**：
- **引用类型**：解决 struct 复制导致的 double-return 问题——所有复制共享同一 owner 实例
- **Interlocked.Exchange**：保证线程安全的幂等释放，即使并发调用也只归还一次
- **Internal**：不暴露给外部，只作为实现细节

#### 2. RbfFrame 变更

```csharp
// atelia/src/Rbf/RbfFrame.cs
public ref struct RbfFrame : IDisposable {
    public SizedPtr Ptr { get; init; }
    public uint Tag { get; init; }
    public ReadOnlySpan<byte> Payload { get; init; }
    public bool IsTombstone { get; init; }
    
    // 新增：可选的 owner token（internal set，外部只读）
    internal PooledBufferOwner? Owner { get; init; }
    
    public void Dispose() {
        Owner?.Release();  // 幂等，多次调用安全
    }
}
```

**关键决策**：
- **移除 `readonly`**：`IDisposable` 的 `Dispose()` 需要能被调用，但实际上 `Owner?.Release()` 不修改 struct 自身字段，只调用引用对象的方法
- **保留 `readonly`？**：实际上可以保留！因为 `Owner?.Release()` 只是调用引用类型的方法，不修改 `RbfFrame` 的字段值

**修正**：保留 `readonly ref struct`：

```csharp
public readonly ref struct RbfFrame : IDisposable {
    // ... 所有属性不变 ...
    internal PooledBufferOwner? Owner { get; init; }
    
    public void Dispose() {
        Owner?.Release();  // 不修改字段，只调用方法
    }
}
```

#### 3. 代码变更清单

| 文件 | 变更类型 | 说明 |
|:-----|:---------|:-----|
| `src/Rbf/Internal/PooledBufferOwner.cs` | **新建** | Owner token 类 |
| `src/Rbf/RbfFrame.cs` | **修改** | 添加 `IDisposable` + `Owner` 属性 |
| `src/Rbf/Internal/RbfReadImpl.cs` | **修改** | 添加 `ReadFramePooled` 方法 |
| `tests/Rbf.Tests/RbfReadImplTests.cs` | **修改** | 添加 Dispose 安全性测试 |

#### 4. ReadFramePooled 实现逻辑

```csharp
// atelia/src/Rbf/Internal/RbfReadImpl.cs

/// <summary>从 ArrayPool 借缓存读取帧。调用方 MUST 调用 Dispose() 归还 buffer。</summary>
public static AteliaResult<RbfFrame> ReadFramePooled(SafeFileHandle file, SizedPtr ptr) {
    // 1. 参数校验（复用现有逻辑，但不检查 buffer 长度）
    var error = CheckReadParamsWithoutBuffer(ptr);
    if (error != null) {
        return AteliaResult<RbfFrame>.Failure(error);
    }
    
    // 2. 从 ArrayPool 借 buffer
    int lengthBytes = (int)ptr.LengthBytes;
    byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(lengthBytes);
    
    // 3. 调用 ReadFrameIntoUnchecked
    var result = ReadFrameIntoUnchecked(file, ptr, rentedBuffer.AsSpan());
    
    // 4. 失败时归还 buffer
    if (!result.IsSuccess) {
        ArrayPool<byte>.Shared.Return(rentedBuffer);
        return result;
    }
    
    // 5. 成功时创建 owner 并附加到 frame
    var owner = new PooledBufferOwner(rentedBuffer);
    var frame = result.Value;
    
    // 重建 frame 以附加 owner（因为 frame 是 readonly struct）
    return AteliaResult<RbfFrame>.Success(new RbfFrame {
        Ptr = frame.Ptr,
        Tag = frame.Tag,
        Payload = frame.Payload,
        IsTombstone = frame.IsTombstone,
        Owner = owner
    });
}
```

**实现要点**：
- **失败路径归还**：读取失败时立即归还 buffer，不泄漏
- **成功路径创建 owner**：只有成功时才创建 owner，由调用方 Dispose
- **重建 frame**：因为 `RbfFrame` 是值类型，需要重建以附加 Owner

#### 5. 与 ReadFrameInto 的关系

```
ReadFrameInto(file, ptr, buffer)     → RbfFrame { Owner = null }   // Dispose 是 no-op
ReadFramePooled(file, ptr)           → RbfFrame { Owner = owner }  // Dispose 归还 buffer
```

**ReadFrameInto 完全不变**：
- 返回的 `RbfFrame.Owner` 为 `null`
- `Dispose()` 调用 `null?.Release()` 是安全的 no-op
- 调用方可以选择用 `using` 包装（无害），也可以不 Dispose（因为 Owner 为 null）

#### 6. 测试策略

```csharp
// tests/Rbf.Tests/RbfFrameDisposeTests.cs

public class RbfFrameDisposeTests {
    [Fact]
    public void Dispose_WhenOwnerIsNull_NoOp() {
        // ReadFrameInto 返回的 frame
        var frame = new RbfFrame { Owner = null };
        frame.Dispose();  // 应该安全返回，不抛异常
        frame.Dispose();  // 多次调用也安全
    }
    
    [Fact]
    public void Dispose_WhenOwnerPresent_ReturnsBufferOnce() {
        // 模拟 ReadFramePooled 的行为
        var buffer = ArrayPool<byte>.Shared.Rent(100);
        var owner = new PooledBufferOwner(buffer);
        
        var frame = new RbfFrame { Owner = owner };
        frame.Dispose();  // 第一次：归还 buffer
        frame.Dispose();  // 第二次：幂等，不会 double-return
    }
    
    [Fact]
    public void Copy_ThenBothDispose_NoDoubleReturn() {
        // 关键测试：验证 owner token 模式解决 struct 复制问题
        var buffer = ArrayPool<byte>.Shared.Rent(100);
        var owner = new PooledBufferOwner(buffer);
        
        var frame1 = new RbfFrame { Owner = owner };
        var frame2 = frame1;  // 复制
        
        frame1.Dispose();  // 第一次归还
        frame2.Dispose();  // 共享同一 owner，幂等安全
    }
    
    [Fact]
    public void Dispose_ThreadSafe() {
        // 并发 Dispose 测试
        var buffer = ArrayPool<byte>.Shared.Rent(100);
        var owner = new PooledBufferOwner(buffer);
        
        var frame = new RbfFrame { Owner = owner };
        
        Parallel.For(0, 100, _ => {
            frame.Dispose();  // 100 个线程并发 Dispose
        });
        // 应该只归还一次，不报错
    }
}
```

#### 7. 实现复杂度评估

| 工作项 | 预估时间 | 复杂度 |
|:-------|:---------|:-------|
| `PooledBufferOwner` 类 | 15 min | 低 |
| `RbfFrame` 修改 | 10 min | 低 |
| `ReadFramePooled` 方法 | 30 min | 中（需处理失败路径） |
| 参数校验重构 | 15 min | 低（提取 `CheckReadParamsWithoutBuffer`） |
| 测试用例 | 45 min | 中（并发测试需要仔细验证） |
| **总计** | **~2 小时** | |

#### 8. 潜在风险与缓解

| 风险 | 缓解措施 |
|:-----|:---------|
| 调用方忘记 Dispose | 文档警告 + 可选的 Analyzer 规则 |
| ArrayPool.Return 参数检查 | `Rent(n)` 返回 `>= n` 的 buffer，Return 时传原 buffer 即可 |
| Payload Span 指向 rentedBuffer | Owner.Release 后 Payload 变 dangling——这是 by design，调用方 MUST NOT 在 Dispose 后访问 Payload |

#### 9. 命名建议确认

支持 Craftsman 的命名建议：

- **`ReadFramePooled`**：明确表达"使用 pool"的语义，与 `ReadFrameInto` 形成对比
- **不推荐 `ReadFrame`**：易与 `ReadFrameInto` 混淆，不清晰哪个是 owning

---

**结论**：C' 方案（owner token 模式）实现复杂度低，约 2 小时可完成。核心是 `PooledBufferOwner` 的幂等释放设计，通过 `Interlocked.Exchange` 保证线程安全和 double-dispose 安全。


---

## 第二轮讨论：进一步简化

### 监护人洞察

1. **命名**：`DisposableResult<T>` 比 `AteliaOwnedResult<T>` 更自明，对 LLM 更友好
2. **关键发现**：既然 `PooledBufferOwner` 已经是堆分配的 class，那么 `RbfPooledFrame` 保持 ref struct 就没有意义了
3. **推论**：`RbfPooledFrame` 和 `DisposableResult<T>` 都应该是 class

### 当前设计的冗余

```
RbfPooledFrame (ref struct)
    ├── Ptr, Tag, IsTombstone (值类型)
    ├── Payload (ReadOnlySpan<byte>) ← ref struct 的唯一理由
    └── Owner (PooledBufferOwner?) ← 已经是堆分配的 class！
            └── byte[] _buffer ← 真正的数据在这里
```

**问题**：
- `PayloadAndMeta` 只是 `Owner._buffer` 的切片
- `Owner` 已经堆分配，ref struct 带来的限制（不能存储、不能跨 await）反而是负担
- `PooledBufferOwner` 用 `Interlocked.Exchange` 防止 double-return，是为了应对 struct 复制问题
- 但如果 `RbfPooledFrame` 本身是 class，就不存在复制问题！

### 简化后的设计

```
RbfPooledFrame (sealed class) : IRbfFrame, IDisposable
    ├── Ptr, Tag, IsTombstone (属性)
    ├── _buffer (byte[]) ← 直接持有，消除中间层
    ├── _payloadOffset, _payloadLength (int)
    └── Payload => _buffer.AsSpan(_payloadOffset, _payloadLength)
    └── Dispose() => ArrayPool.Return(_buffer) // 简单直接
```

### 收益

| 方面 | 之前 | 之后 |
|:-----|:-----|:-----|
| 类型 | ref struct | sealed class |
| 中间层 | PooledBufferOwner | **删除** |
| 并发保护 | Interlocked.Exchange | **不需要** |
| 存储限制 | 不能存字段、不能跨 await | **无限制** |
| Dispose 幂等 | 靠 Owner token | 靠 `_buffer = null` 检查 |

### DisposableResult<T> 设计

```csharp
public sealed class DisposableResult<T> : IDisposable where T : IDisposable {
    private readonly T? _value;
    private readonly AteliaError? _error;
    
    public bool IsSuccess => _error is null;
    public T? Value => _value;
    public AteliaError? Error => _error;
    
    public void Dispose() {
        _value?.Dispose();  // null-safe，失败时 _value 是 null
    }
}
```

**使用方式**：
```csharp
using var result = rbf.ReadPooledFrame(ptr).ToDisposable();
if (result.IsFailure) { /* handle error */ }
var frame = result.Value;  // 安全使用
// result.Dispose() 自动调用 frame.Dispose()
```

### 决策

1. **RbfPooledFrame** 从 `readonly ref struct` 改为 `sealed class`
2. **删除** `PooledBufferOwner` 类
3. **IRbfFrame** 保持接口声明不变（class 也可以实现返回 Span 的属性）
4. **新增** `DisposableResult<T>` 到 `Atelia.Primitives`
5. **新增** `ToDisposable()` 扩展方法桥接 `AteliaResult<T>`

### 施工计划

1. 修改 `RbfPooledFrame.cs`：ref struct → sealed class
2. 删除 `PooledBufferOwner.cs`
3. 修改 `RbfReadImpl.ReadPooledFrame`：直接构造 class
4. 更新测试（语法可能有微调）
5. 新增 `DisposableResult<T>` 和 `ToDisposable()` 扩展
6. 运行全部测试验证

---

## 施工记录 (Round 2)

### 变更清单

| 文件 | 变更 |
|:-----|:-----|
| `src/Rbf/RbfPooledFrame.cs` | `readonly ref struct` → `sealed class`，直接持有 `byte[]` |
| `src/Rbf/Internal/PooledBufferOwner.cs` | **删除** |
| `src/Rbf/Internal/RbfReadImpl.cs` | 移除 `PooledBufferOwner` 创建，直接构造 class |
| `tests/Rbf.Tests/RbfPooledFrameTests.cs` | 移除对 `Owner` 属性的测试断言 |
| `src/Primitives/DisposableResult.cs` | **新增** |

### 新 RbfPooledFrame 结构

```csharp
public sealed class RbfPooledFrame : IRbfFrame, IDisposable {
    private byte[]? _buffer;           // 直接持有
    private readonly int _payloadOffset;
    private readonly int _payloadLength;

    public SizedPtr Ptr { get; }
    public uint Tag { get; }
    public bool IsTombstone { get; }
    public ReadOnlySpan<byte> Payload => _buffer.AsSpan(_payloadOffset, _payloadLength);

    public void Dispose() {
        var buf = _buffer;
        if (buf is not null) {
            _buffer = null;
            ArrayPool<byte>.Shared.Return(buf);
        }
    }
}
```

### DisposableResult<T> API

```csharp
public sealed class DisposableResult<T> : IDisposable where T : class, IDisposable {
    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public T? Value { get; }
    public AteliaError? Error { get; }
    public void Dispose() => _value?.Dispose();
}

// 扩展方法
public static DisposableResult<T> ToDisposable<T>(this AteliaResult<T> result);
```

**使用示例**：
```csharp
using var result = rbf.ReadPooledFrame(ptr).ToDisposable();
if (result.IsFailure) { /* handle */ }
var frame = result.Value;
// result.Dispose() 自动调用 frame.Dispose()
```

### 测试结果

- **156 个测试全部通过** ✅
- 无新增警告（除已有的 `RbfFrameBuilder._committed` 未使用警告）

### 代码行数对比

| 之前 | 之后 | 净减少 |
|:-----|:-----|:-------|
| RbfPooledFrame: 40 行 | RbfPooledFrame: 50 行 | +10 行 |
| PooledBufferOwner: 25 行 | (删除) | -25 行 |
| RbfReadImpl (pooled 部分): ~40 行 | ~35 行 | -5 行 |
| | DisposableResult: 120 行 | +120 行 |

**总体**：减少 PooledBufferOwner + Interlocked.Exchange 复杂度，增加了通用的 DisposableResult 类型。
