# 畅谈会：ELOG 层边界契约设计

> **日期**：2025-12-21
> **标签**：#design
> **主持人**：刘德智 (Team Leader)
> **参与者**：Advisor-GPT, Advisor-Claude, Advisor-Gemini
> **状态**：进行中

---

## 背景

StateJournal MVP v2 设计文档需要拆分为两层：

```
Layer 0: ELOG Framing (底层)
├── Magic / HeadLen / TailLen / Pad / CRC32C
├── reverse scan / resync
├── DataTail truncate
└── 纯二进制分帧，无业务语义

────────────────────────────────
Layer 1: StateJournal 语义 (上层)
├── ObjectVersionRecord / MetaCommitRecord
├── DurableDict / DiffPayload / ValueType
├── 二阶段提交 / Workspace / Identity Map
└── 依赖 Layer 0 的 record 读写能力
```

**核心问题**：Layer 0 对外暴露什么接口？这个决策影响：
- 是否能实现 zero-copy
- 上下层的耦合程度
- 实现复杂度与性能权衡

## 设计目标

1. **Layer 0 可独立稳固**：有清晰的接口契约，可独立测试
2. **支持高效写入**：避免不必要的内存复制
3. **支持 ELOG 特性**：预留 header → 写 payload → 回填 header/CRC
4. **与现有基础设施对齐**：复用 `IReservableBufferWriter` 等已有抽象

## 候选方案

### 写入侧

#### 方案 W1: `ReadOnlySpan<byte> payload` 直传

```csharp
interface IElogWriter {
    Ptr64 Append(ReadOnlySpan<byte> payload, RecordKind kind);
    void Fsync();
}
```

**特点**：
- ✅ 接口最简单
- ❌ 调用方必须预先序列化到连续内存
- ❌ 可能需要额外复制（先序列化到 buffer，再传给 ELOG）

#### 方案 W2: `IBufferWriter<byte>` 回调

```csharp
interface IElogWriter {
    Ptr64 Append(RecordKind kind, Action<IBufferWriter<byte>> writePayload);
    void Fsync();
}
```

**特点**：
- ✅ 调用方可直接序列化到 writer
- ⚠️ 需要先写入再计算长度/CRC（两遍扫描或预留回填）
- ⚠️ 标准 `IBufferWriter` 不支持预留/回填

#### 方案 W3: `IReservableBufferWriter` 回调

```csharp
interface IElogWriter {
    Ptr64 Append(RecordKind kind, Action<IReservableBufferWriter> writePayload);
    void Fsync();
}
```

**特点**：
- ✅ 调用方可直接序列化并使用预留/回填
- ✅ 完美支持 ELOG header 回填
- ⚠️ 自定义接口，调用方需要适配

#### 方案 W4: 分层混合（推荐讨论）

```csharp
// Layer 0 内部使用 IReservableBufferWriter
// 但对外暴露简化接口
interface IElogWriter {
    // 简单场景：payload 已就绪
    Ptr64 Append(ReadOnlySpan<byte> payload, RecordKind kind);
    
    // 高级场景：调用方需要直接写入
    ElogRecordBuilder BeginRecord(RecordKind kind);
    
    void Fsync();
}

ref struct ElogRecordBuilder {
    // 内部持有 IReservableBufferWriter
    public IBufferWriter<byte> PayloadWriter { get; }
    public Ptr64 Commit(); // 完成写入，回填 header/CRC
}
```

**特点**：
- ✅ 简单场景有简单 API
- ✅ 高级场景可以 zero-copy
- ⚠️ 接口更复杂

### 读取侧

#### 方案 R1: 返回 `ReadOnlySpan<byte>`

```csharp
interface IElogReader {
    ElogRecord? ReadAt(Ptr64 ptr);
}

ref struct ElogRecord {
    public RecordKind Kind { get; }
    public ReadOnlySpan<byte> Payload { get; }
}
```

**特点**：
- ✅ 最简单
- ⚠️ `ref struct` 限制（不能存储、不能 async）
- ⚠️ Span 生命周期绑定到底层缓冲区

#### 方案 R2: 返回 `ReadOnlyMemory<byte>`

```csharp
readonly struct ElogRecord {
    public RecordKind Kind { get; }
    public ReadOnlyMemory<byte> Payload { get; }
}
```

**特点**：
- ✅ 可存储、可 async
- ⚠️ 需要保证底层缓冲区生命周期

#### 方案 R3: 回调模式

```csharp
interface IElogReader {
    void ReadAt(Ptr64 ptr, RecordVisitor visitor);
}

interface RecordVisitor {
    void Visit(RecordKind kind, ReadOnlySpan<byte> payload);
}
```

**特点**：
- ✅ 无生命周期问题
- ❌ API 不直观

## 现有基础设施

### IReservableBufferWriter

```csharp
// 核心能力：预留区段 + 延后回填
public interface IReservableBufferWriter : IBufferWriter<byte> {
    Span<byte> ReserveSpan(int count, out int reservationToken, string? tag = null);
    void Commit(int reservationToken);
}
```

**适用场景**：
- ELOG header 的 HeadLen/TailLen/CRC 回填
- 上层 DiffPayload 的 PairCount 回填

### ChunkedReservableWriter

已有实现，支持：
- ArrayPool 分块缓冲
- 自适应增长
- 连续前缀 flush
- 透传模式（无预留时直通底层 writer）

## 待讨论问题

| # | 问题 | 决策选项 |
|---|------|----------|
| Q1 | 写入侧用哪个方案？ | W1 / W2 / W3 / W4 |
| Q2 | 读取侧用哪个方案？ | R1 / R2 / R3 |
| Q3 | 是否追求 zero-copy？ | 全面 / 仅热路径 / MVP 不要求 |
| Q4 | Layer 0 是否暴露 `IReservableBufferWriter`？ | 暴露 / 仅内部使用 |
| Q5 | 术语表如何共享？ | 提取到独立 glossary.md / 保持 Layer 0 内联 |

## 💬 畅谈记录

### 主持人开场 (刘德智)

各位顾问好！

今天的畅谈会目标是为 ELOG 层边界设计接口契约。

**核心决策点**：
1. 写入接口形态（简单 Span vs IBufferWriter vs IReservableBufferWriter）
2. 读取接口形态（Span vs Memory vs 回调）
3. zero-copy 的优先级

请各位从自己的专长视角发表意见：
- **GPT**：接口精确性与可测试性
- **Claude**：概念分层与未来扩展性
- **Gemini**：开发者体验与实现复杂度

发言顺序：Claude → Gemini → GPT

---

### Advisor-Claude 发言

各位好！作为概念架构与术语治理专家，我将从**概念分层清晰度**和**未来扩展性影响**两个视角来评审这次的 ELOG 层边界契约设计。

---

## 一、概念分层分析

### 1.1 Layer 0 的本质定位

首先，让我明确 Layer 0（ELOG Framing）的**概念内核**：

> **ELOG 是"二进制信封"——它只关心如何安全地封装和解封任意 payload，不关心 payload 里装的是什么。**

这个定位决定了 Layer 0 的职责边界：

| 职责 | Layer 0 MUST | Layer 0 MUST NOT |
|------|--------------|------------------|
| **封装** | 提供 header/trailer 包装、CRC32C 校验 | 解释 payload 的语义 |
| **扫描** | 支持 reverse scan、resync 4B 对齐 | 知道什么是 ObjectVersionRecord |
| **截断** | 正确处理 DataTail 截断 | 理解 VersionIndex 或 DiffPayload |

### 1.2 候选方案的分层评估

#### 写入侧

| 方案 | 分层清晰度 | 分析 |
|------|------------|------|
| **W1** | ⭐⭐⭐ | 最简洁，但迫使上层预先序列化——这实际上是把"分帧"的责任泄漏到了上层 |
| **W2** | ⭐⭐ | 标准 `IBufferWriter` 不支持预留，Layer 0 内部必须做两遍扫描或临时缓冲 |
| **W3** | ⭐⭐⭐⭐ | 概念一致：ELOG 的 header 回填需求与 `IReservableBufferWriter` 的预留能力天然匹配 |
| **W4** | ⭐⭐⭐⭐⭐ | **分层混合**——简单场景有简洁 API，复杂场景有能力扩展 |

**我的倾向：W4**

关键洞察：W4 的 `ElogRecordBuilder` 模式实际上是在**封装复杂性**——调用方不需要理解 `IReservableBufferWriter` 的状态机，只需要知道"写 payload → commit"。

但我有一个术语建议：

> **将 `ElogRecordBuilder` 命名为 `ElogFrameBuilder`** —— "Record" 暗示业务语义，"Frame" 更准确描述 Layer 0 的职责（信封）。

#### 读取侧

| 方案 | 分层清晰度 | 分析 |
|------|------------|------|
| **R1** | ⭐⭐⭐⭐ | `ref struct` 的限制反而是优点——强制调用方同步处理，避免生命周期问题 |
| **R2** | ⭐⭐⭐ | `Memory<byte>` 需要保证底层缓冲区生命周期，这个责任应该由谁承担？ |
| **R3** | ⭐⭐ | 回调模式把控制流反转了，对调用方不友好 |

**我的倾向：R1 为主，R2 为辅助**

理由：
- 大多数读取场景是"读 → 反序列化 → 丢弃"，R1 的 `Span` 完美匹配
- 少数需要持久化 payload 的场景（如缓存），可以提供 `ToMemory()` 辅助方法

### 1.3 `IReservableBufferWriter` 的概念层定位

这是一个**关键的概念澄清点**：

审阅 [IReservableBufferWriter.cs](atelia/src/Data/IReservableBufferWriter.cs) 后，我注意到它的文档写的是"扩展 `IBufferWriter<byte>` 协议"。这暗示它是一个**通用的序列化能力增强**，而非 ELOG 专用接口。

这很好！它应该保持这个定位：

```
                       ┌─────────────────────────┐
                       │   IBufferWriter<byte>   │  ← .NET 标准
                       └───────────┬─────────────┘
                                   │ 扩展
                       ┌───────────▼─────────────┐
                       │ IReservableBufferWriter │  ← Atelia 通用能力
                       └───────────┬─────────────┘
                                   │ 使用
           ┌───────────────────────┼───────────────────────┐
           │                       │                       │
  ┌────────▼────────┐    ┌─────────▼─────────┐   ┌─────────▼─────────┐
  │  ELOG Framing   │    │  DiffPayload 写入  │   │  其他序列化场景  │
  │  (Layer 0)      │    │  (Layer 1)         │   │                   │
  └─────────────────┘    └───────────────────┘   └───────────────────┘
```

**概念边界建议**：`IReservableBufferWriter` 应保持为 `Atelia.Data` 命名空间下的通用接口，不应被 ELOG 或 StateJournal 独占。

---

## 二、各问题的明确建议（Q1-Q5）

### Q1: 写入侧用哪个方案？

**建议：W4（分层混合）**，但做以下调整：

```csharp
interface IElogFramer  // 注意命名：Framer 而非 Writer
{
    // 简单场景：payload 已就绪
    Address64 WriteFrame(ReadOnlySpan<byte> payload, byte frameTag);
    
    // 高级场景：需要预留/回填
    ElogFrameBuilder BeginFrame(byte frameTag);
    
    void Flush();  // fsync 语义
}

ref struct ElogFrameBuilder
{
    public IBufferWriter<byte> Payload { get; }  // 对外暴露标准接口
    public Address64 Complete();  // 回填 header/CRC，返回 frame 起始地址
    public void Abort();  // 放弃当前 frame（可选）
}
```

**关键变化**：
1. 接口命名为 `IElogFramer`，强调"分帧器"而非"写入器"
2. 参数使用 `frameTag` 而非 `RecordKind`——Layer 0 不应使用 Layer 1 的术语
3. 返回 `Address64` 而非 `Ptr64`——概念层术语（虽然编码相同）

### Q2: 读取侧用哪个方案？

**建议：R1 为主 + 可选的 `CopyTo` 辅助**

```csharp
interface IElogScanner
{
    ElogFrame? ReadFrameAt(Address64 address);
    IEnumerable<ElogFrame> ScanFromTail();  // reverse scan
}

ref struct ElogFrame
{
    public byte FrameTag { get; }
    public ReadOnlySpan<byte> Payload { get; }
    public Address64 Address { get; }
    
    // 辅助方法：需要持久化时使用
    public byte[] ToArray();  // 或 CopyTo(Span<byte>)
}
```

**设计理由**：
- `ref struct` 的限制反而是"护栏"——防止调用方在不理解生命周期的情况下存储 frame
- 需要持久化时，显式调用 `ToArray()` 让分配意图清晰

### Q3: 是否追求 zero-copy？

**建议：仅热路径**

理由：

| 路径 | 频率 | zero-copy 价值 | 建议 |
|------|------|----------------|------|
| **Commit 写入** | 高频 | 高 | MUST 支持 |
| **LoadObject 读取** | 高频 | 高 | SHOULD 支持 |
| **Resync/Recovery** | 低频 | 低 | MAY 不追求 |
| **Debug/Dump** | 极低频 | 无 | 不追求 |

**概念层表述**：

> **[S-ELOG-ZEROCOPY-HOTPATH]**：ELOG 实现 SHOULD 在 Commit 写入和 LoadObject 读取路径上避免不必要的内存复制。Recovery 和诊断路径 MAY 使用简化实现。

### Q4: Layer 0 是否暴露 `IReservableBufferWriter`？

**建议：内部使用，对外封装**

理由：
1. `IReservableBufferWriter` 的状态机语义（GetSpan → Advance / ReserveSpan → Commit）对普通调用方有认知负担
2. `ElogFrameBuilder` 提供了更简洁的"写 → 完成"心智模型
3. 若高级调用方确需底层能力，可通过 `ElogFrameBuilder.UnsafeGetReservableWriter()` 暴露

```csharp
ref struct ElogFrameBuilder
{
    public IBufferWriter<byte> Payload { get; }  // 标准接口
    
    // 高级 API（仅供需要预留的调用方使用）
    public IReservableBufferWriter UnsafePayloadWriter { get; }
}
```

### Q5: 术语表如何共享？

**建议：Layer 0 独立术语表 + Layer 1 引用**

理由：
1. **分层隔离原则**：Layer 0 的术语不应依赖 Layer 1
2. **概念完整性**：Layer 0 应该是自包含的可独立测试模块
3. **未来扩展**：Layer 0 可能被其他项目复用（如 PipeMux 的消息分帧）

具体建议：

| 术语 | 层级 | 定义位置 |
|------|------|----------|
| Frame | Layer 0 | `elog-format.md` |
| FrameTag | Layer 0 | `elog-format.md` |
| Address64 | Layer 0 | `elog-format.md` |
| Magic / HeadLen / TailLen / CRC32C | Layer 0 | `elog-format.md` |
| RecordKind / ObjectKind / ValueType | Layer 1 | `mvp-design-v2.md` |
| ObjectVersionRecord / MetaCommitRecord | Layer 1 | `mvp-design-v2.md` |

---

## 三、风险提示

### 3.1 概念边界泄漏风险

**风险**：当前文档中 `RecordKind` 被作为 ELOG 接口参数使用，这会导致 Layer 0 依赖 Layer 1 的概念。

**建议**：将 `RecordKind` 重命名为 `FrameTag`（或在 Layer 0 中只使用 `byte tag`），让 Layer 1 负责 `FrameTag ↔ RecordKind` 的映射。

### 3.2 未来扩展阻塞点

如果将来需要：

| 扩展需求 | 当前方案的影响 | 建议预留 |
|----------|----------------|----------|
| **压缩** | W1 需要调用方处理压缩后再传 | W4 的 Builder 模式可内部处理 |
| **加密** | 同上 | 同上 |
| **多文件** | Address64 需要区分文件 | 预留高位作为文件 ID |
| **async I/O** | R1 的 `ref struct` 无法跨 await | 提供 R2 作为 async 替代 |

**建议**：

> **[F-ELOG-ADDRESS-RESERVED-BITS]**：Address64 的高 8 位 SHOULD 保留供未来扩展（如多文件索引）。MVP 实现 MUST 写入 0，读取时 SHOULD 忽略。

### 3.3 术语冲突风险

当前 MVP 设计文档中存在术语层级混用：

- `Ptr64` 被定义为"通用 file offset 编码"，但又说"4B 对齐"——这是 ELOG 的约束还是通用约束？
- `Address64` 被定义为"Ptr64 的语义子类型"——但 Layer 0 应该只知道 Address64，不应引用 Layer 1 的 Ptr64？

**建议**：

```
Layer 0 术语:
  - Address64: 8 字节 LE file offset，MUST 4B 对齐，0=null

Layer 1 术语:
  - Ptr64: 同 Address64（别名，为了与 DiffPayload 的 Val_Ptr64 对齐）
```

---

## 四、结论

### 总体评估

| 维度 | 评分 | 说明 |
|------|------|------|
| **概念分层清晰度** | ⭐⭐⭐⭐ | W4 + R1 组合提供了良好的分层 |
| **未来扩展性** | ⭐⭐⭐⭐ | Builder 模式为压缩/加密预留了空间 |
| **术语一致性** | ⭐⭐⭐ | 需要进一步隔离 Layer 0/1 的术语 |
| **实现复杂度** | ⭐⭐⭐⭐ | `IReservableBufferWriter` 已有成熟实现可复用 |

### 明确建议汇总

| 问题 | 建议 | 理由 |
|------|------|------|
| Q1 | **W4（分层混合）** | 兼顾简洁性和扩展性 |
| Q2 | **R1 + 可选 CopyTo** | Span 的限制是护栏 |
| Q3 | **仅热路径** | 投入产出比最优 |
| Q4 | **内部使用，对外封装** | 降低调用方认知负担 |
| Q5 | **Layer 0 独立术语表** | 分层隔离原则 |

### 关键行动项

1. **创建 `elog-format.md`**：Layer 0 的独立规格文档，定义 Frame 结构、FrameTag 枚举空间、Address64 约束
2. **术语隔离**：将 `RecordKind` 从 ELOG 接口移除，改用 `byte frameTag`
3. **接口命名规范化**：`IElogFramer` + `ElogFrameBuilder` + `IElogScanner` + `ElogFrame`
4. **条款编号预留**：为 Layer 0 预留 `[E-xxx]` 前缀的条款编号空间

---

以上是我从概念分层与未来扩展性视角的分析。期待 Gemini 和 GPT 两位同事的视角补充！

---

### Advisor-Gemini 发言

大家好！我是 Advisor-Gemini。我将从 **DX（开发者体验）** 和 **"Pit of Success"（成功陷阱）** 的角度来审视这些方案。

我的核心观点是：**Layer 0 应该是"防呆"的。** 底层越坚固，上层（StateJournal）的实现者就越敢于进行复杂的逻辑构建。

---

## 一、DX 评估矩阵

我对比了各方案在"易用性"（上手难度）和"安全性"（防误用）上的表现：

| 方案 | 易用性 (Affordance) | 安全性 (Safety) | 认知负荷 | 评价 |
|:---|:---:|:---:|:---:|:---|
| **W1 (Span)** | ⭐⭐ | ⭐⭐⭐ | 高 | **"内存体操"**：强迫用户自己管理 Buffer 和序列化，容易出现 Off-by-one 错误。 |
| **W2 (Action)** | ⭐⭐⭐ | ⭐⭐ | 中 | **"性能陷阱"**：看起来很美，但底层为了计算 Length/CRC 可能需要 Double Pass，隐性性能损耗。 |
| **W3 (Reservable)** | ⭐ | ⭐ | 极高 | **"裸露电线"**：直接暴露状态机（Reserve -> Advance -> Commit）。一旦顺序调错（如忘记 Commit），整个 Writer 就会死锁（Blocking Reservation）。 |
| **W4 (Builder)** | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ | 低 | **"事务隐喻"**：`Begin -> Write -> Complete` 符合人类直觉。配合 `using` 可实现自动回滚（Abort）。 |

---

## 二、各问题的明确建议（Q1-Q5）

### Q1: 写入侧用哪个方案？

**强烈建议：W4 (Builder 模式) + 事务性增强**

Claude 提到了 `ElogFrameBuilder`，我非常赞同。但我建议更进一步，将其设计为 **Disposable Transaction**。

**DX 痛点**：如果用户调用了 `BeginFrame` 但中间抛出异常，或者忘记调用 `Complete`，底层的 `IReservableBufferWriter` 会留下一个未提交的 Reservation，导致后续数据无法 Flush（死锁）。

**解决方案**：利用 `Dispose()` 实现 **Auto-Abort**。

```csharp
// 推荐用法：利用 using 语法糖保证安全
using (var frame = framer.BeginFrame(FrameTag.ObjectVersion)) 
{
    // 直接写入 payload，无需关心 header/crc
    frame.Writer.Write(myPayload);
    
    // 显式提交。如果未执行这行就退出作用域（如异常），
    // Dispose() 会自动将此帧标记为 Padding (Void)，防止阻塞流。
    frame.Commit(); 
}
```

### Q2: 读取侧用哪个方案？

**建议：R1 (Ref Struct Span)**

**DX 视角**：
- `ref struct` 是一个 **Feature** 而不是 Bug。它强制开发者在"当前栈帧"处理完数据，或者显式拷贝。
- 这消除了 "Use-After-Free" 的隐患（即开发者错误地持有了指向已回收 Buffer 的引用）。
- 对于需要持久持有的场景，提供 `.ToOwned()` 或 `.CopyTo()` 扩展方法即可。

### Q3: 是否追求 zero-copy？

**建议：默认 Zero-Copy，提供 Escape Hatch**

- **Hot Path (Commit/Load)**：必须 Zero-Copy。
- **Cold Path (Debug/Dump)**：不要为了 Zero-Copy 让代码变得晦涩。提供 `byte[] ReadAsArray()` 这种"昂贵但方便"的 API 给测试和调试用。

### Q4: Layer 0 是否暴露 `IReservableBufferWriter`？

**建议：绝对不要 (Internal Only)**

`IReservableBufferWriter` 是一个**高危接口**。它的 `ReserveSpan` 和 `Commit` 必须严格配对，且顺序敏感。
Layer 0 的职责就是**封装**这个复杂性。对外只暴露 `IBufferWriter<byte>`（在 Builder 内部）。

### Q5: 术语表如何共享？

**建议：Layer 0 拥有独立的"物理层术语"**

同意 Claude。Layer 0 不应该知道 `RecordKind`。
- Layer 0 只有 `FrameTag` (byte)。
- Layer 1 负责将 `RecordKind` 映射到 `FrameTag`。

---

## 三、典型用法代码示例

这是我构想的 Layer 0 最终形态，注重 **Fluent** 和 **Safe**：

```csharp
// 1. 写入 (Happy Path)
public void SaveObject(IElogFramer framer, MyObject obj)
{
    // 开启一个帧，指定 Tag
    using var frame = framer.BeginFrame(FrameTags.Snapshot);
    
    // 序列化逻辑只看到标准的 IBufferWriter<byte>
    // 不需要关心预留、回填、CRC
    obj.SerializeTo(frame.Writer);
    
    // 提交！返回物理地址
    Address64 addr = frame.Commit();
}

// 2. 写入 (Exception Path - 自动防护)
public void SaveWithBug(IElogFramer framer)
{
    using (var frame = framer.BeginFrame(FrameTags.Diff))
    {
        frame.Writer.Write(someData);
        
        // 模拟异常
        throw new Exception("Oops");
        
        // frame.Commit() 未被执行
    } 
    // 离开作用域时，frame.Dispose() 被调用。
    // 检测到未 Commit，自动将 Header 覆写为 FrameTag.Padding (Void)，
    // 并执行 Commit。底层 Writer 保持健康，只是多了一块垃圾数据。
}

// 3. 读取
public void ReadLog(IElogScanner scanner, Address64 addr)
{
    // 限制生命周期，防止内存泄漏
    ElogFrame frame = scanner.ReadFrameAt(addr);
    
    if (frame.Tag == FrameTags.Padding) return; // 忽略填充帧
    
    // 直接处理 Span
    ProcessPayload(frame.Payload);
    
    // 如果需要存下来：
    byte[] copy = frame.Payload.ToArray();
}
```

## 四、易误用场景与防护建议

| 场景 | 风险 | 防护设计 (Pit of Success) |
|:---|:---|:---|
| **忘记 Commit** | Writer 死锁，无法 Flush | `ElogFrameBuilder` 实现 `IDisposable`，在 Dispose 时若未 Commit 则自动回填 Padding 并 Commit。 |
| **Payload 过大** | 内存爆涨 | `IBufferWriter` 天然支持分块，Layer 0 实现应透传分块能力，不强制连续内存。 |
| **并发写入** | 数据交错 | `IElogFramer` 显式声明为 **非线程安全**。若需并发，上层需加锁。Layer 0 不做锁，保持轻量。 |
| **读取后持有 Span** | 数据损坏 (Use-After-Free) | 返回 `ref struct`，编译器禁止将其存储到堆上。 |

---

### 总结

我完全支持 **W4 + R1** 的组合。
关键在于 `ElogFrameBuilder` 的实现细节：**它必须是一个能够自我清理的 Transaction**。
这不仅是为了好用，更是为了系统的**鲁棒性**——防止一个上层逻辑的 Bug 导致底层存储引擎的死锁。

### Advisor-GPT 发言

下面我从**接口精确性**与**可测试性**两个角度，基于现有格式条款（尤其是 StateJournal v2 的 framing 规范与条款 ID）来收口 Layer 0 的边界契约。

---

## 1) 接口精确性分析（类型安全 / 边界条件 / 可观察行为）

### 1.1 “Layer 0 只懂 framing，但必须可写出可识别的‘可丢弃帧’”

Gemini 提到的 Transactional Builder（自动 Abort）在工程上非常关键，但要**在不引入语义耦合**的前提下成立，有一个前置条件：

- Layer 0 必须能写出一个**上层可忽略**的帧（例如 Padding/Noop）。

否则“自动 Abort”只能通过“完全不写入任何字节”来实现，这会迫使 Layer 0 在 Begin→Commit 之间**缓冲整条 payload**（丢掉 zero-copy 优势），或引入复杂的 rollback 机制。

因此：建议把当前讨论中的 `RecordKind` 明确降维为 **`FrameTag : byte`**（Layer 0 术语），并规定至少一个保留值（例如 `0x00 = Padding`）。Layer 1 再将其映射为 `DataRecordKind/MetaRecordKind`，与 v2 文档中的 **[F-RECORDKIND-DOMAIN-ISOLATION]** 精神一致。

### 1.2 指针类型：避免 `Ptr64`/`Address64` 在接口面混用

StateJournal v2 对 `Ptr64` 的定义是“通用 file offset 编码”（不一定指向 record 起点），而 `Address64` 才是“指向 record 起点”的语义子类型。

在 Layer 0 API 上建议：

- **写入返回值**：返回“record 起点”的 **`Address64`**（或至少命名为 `RecordAddress`），避免上层误把 `DataTail`（EOF 偏移）当成可解引用地址。
- **随机读入参**：只接受 `Address64`（而不是泛化 `Ptr64`），使类型系统参与“只能读 record 起点”的约束。

如果暂时不引入新类型，也建议在接口命名上强制区分：`ReadAtRecord(Address64 recordStart)` vs `TruncateToDataTail(Ptr64 eof)`，避免“同是 ulong”导致实现分叉。

### 1.3 Builder 的状态机必须写进契约（否则必然实现分叉）

当前 `IReservableBufferWriter` 的契约已经明确“GetSpan/Advance 与 Reserve/Commit 的顺序约束 + 单线程”。Layer 0 的 builder 只要建立在它之上，就必须把这些关键行为“提升”为 Layer 0 的可观察契约：

- 是否允许嵌套 Begin？（建议：**不允许**，同一 writer 同时最多一个 open builder）
- Dispose/Abort 后 writer 是否可继续写？（建议：**必须可继续**；否则上层一个异常会把整个日志写入器打残）
- Commit/Abort 是否幂等？（建议：**Abort 幂等**，Commit 非幂等且重复调用抛异常）

这类行为必须写成条款（或至少写进接口 docstring），否则你会得到两类实现：

1) “严格失败”型：异常后 writer 进入不可用状态；
2) “自愈”型：自动写入 padding 并恢复。

两者在压测与故障恢复时表现差异巨大，且难以用黑盒测试统一。

---

## 2) 可测试性评估（单测、Test Vectors 映射）

### 2.1 推荐把 Layer 0 的测试面拆成三组

1) **Framing Test Vectors（字节级 SSOT）**
    - 输入：`FrameTag` + payload bytes
    - 断言：输出 bytes 必须满足 v2 既有 framing 条款：
      - **[F-MAGIC-RECORD-SEPARATOR]**（写完 record 追加 Magic）
      - **[F-HEADLEN-TAILLEN-SYMMETRY]**（HeadLen==TailLen）
      - **[F-CRC32C-PAYLOAD-COVERAGE]**（CRC 覆盖范围）
      - **[F-RECORD-WRITE-SEQUENCE]**（写入步骤顺序）

2) **Recovery/Resync Vectors（损坏尾部）**
    - 构造：尾部注入随机垃圾 / 半条 record / 错 CRC / 错 TailLen
    - 断言：reverse scan/resync 必须遵守 **[R-RESYNC-DISTRUST-TAILLEN]**（不得信任 TailLen 跳跃）。

3) **API-Behavior Tests（状态机 + 失败协议）**
    - Begin→异常→Dispose：writer 仍可继续追加下一条有效 record
    - Begin→未 Commit→Dispose：必须落盘一个可忽略帧（Padding）或等价的“不会破坏后续写入”的行为
    - 非法调用序列（重复 Commit / Begin 嵌套 / 并发调用）：必须确定性失败（异常类型可不锁死，但错误通道必须唯一）

### 2.2 为什么“Transactional Builder”对测试友好

它把“调用方正确性”从“必须记住调用顺序”变成“语言语法保证”：

- `using var frame = BeginFrame(...)` 让测试可以稳定构造“异常路径”的回归用例；
- `Dispose` 的 Auto-Abort 行为让 fuzz/属性测试更容易覆盖“抛异常的任意中断点”。

要点是：**Auto-Abort 的落盘形态必须规范化**（例如 Padding 帧），否则测试只能检查“没崩”，无法检查“写入序列是否仍满足 framing 不变量”。

---

## 3) 可能导致实现分叉的歧义（建议立即钉死）

1) `RecordKind` 是否属于 Layer 0 术语：
    - 若不收口，会发生“Layer 0 依赖 Layer 1 枚举”或“Kind 放 payload 的第一个字节但没人承认”的双轨。

2) Abort 的语义：
    - “不写任何字节” vs “写 Padding 帧”会直接影响 zero-copy 与崩溃恢复一致性。

3) Read API 的生命周期：
    - `ReadOnlySpan<byte>` 若来自内部 buffer/mmap，生命周期必须被 `ref struct`/回调限制；否则上层把 span 缓存起来会产生隐性 use-after-free。

4) `Fsync/Flush` 的含义：
    - 仅 flush OS buffer？还是 fsync 到介质？建议在 Layer 0 只承诺“flush 到 OS 可见”，commit point 的“fsync 次序”交给 Layer 1 的 **[R-COMMIT-FSYNC-ORDER]**。

---

## 4) 对 Q1-Q5 的明确建议（收口版）

### Q1 写入侧用哪个方案？

选 **W4**，并把 W4 明确化为“Transactional Builder + 双入口（Span fast-path + Builder streaming）”。

- `Append(ReadOnlySpan<byte>)`：用于 payload 已就绪的小记录（测试向量也更直接）。
- `BeginFrame(FrameTag)`：用于 zero-copy/流式序列化与需要 payload 内回填（如 PairCount）。

### Q2 读取侧用哪个方案？

主选 **R1**（`ref struct` + `ReadOnlySpan<byte>`），并补一个“显式拷贝”的逃生口。

原因：R1 的限制（不能 async/不能存储）本质上是生命周期护栏；需要跨 async 或缓存时，提供 `CopyPayloadTo(...)`/`ToArray()` 即可。

### Q3 是否追求 zero-copy？

选“**仅热路径**”，但接口面必须**允许** zero-copy（否则未来无从优化）。

- 写：Builder 路径 MUST 支持不额外复制。
- 读：R1 路径 SHOULD 支持 span view；冷路径可 copy。

### Q4 Layer 0 是否暴露 `IReservableBufferWriter`？

结论：**不在 Layer 0 顶层接口直接暴露**，但允许在 Builder 内以“受控方式”提供。

折中理由（接口精确性 + 可测试性）：

- 直接暴露会把 `ReserveSpan/Commit` 的状态机风险扩散到所有调用点；
- 但 Layer 1 的 DiffPayload 写入确实需要“payload 内回填”，完全不暴露会迫使 Layer 1 缓冲并复制。

因此建议：Builder 对外主面是 `IBufferWriter<byte>`；同时可选提供 `IReservableBufferWriter`（或更强类型的 reservation token 包装）给确实需要回填的上层 codec。

### Q5 术语表如何共享？

选“**Layer 0 独立 glossary**（Frame/Magic/HeadLen/TailLen/CRC32C/FrameTag/Address64）+ Layer 1 引用并做映射”。

这是把“二进制 framing”变成可复用基础设施的必要条件，也能避免在接口签名上出现 Layer 1 的业务术语。

---

## 5) 共识汇总表（Claude / Gemini / GPT）

| 议题 | Claude | Gemini | Advisor-GPT（我） | 收口结论 |
|---|---|---|---|---|
| Q1 写入 | W4（Builder + Span） | W4 + Transaction（Auto-Abort） | W4 + Transaction（必须有可丢弃帧） | **W4 + Transactional Builder** |
| Q2 读取 | R1 主、R2 辅 | R1（ref struct） | R1 主 + 显式 copy 逃生口 | **R1 主** |
| Q3 zero-copy | 仅热路径 | 默认 zero-copy + 逃生口 | 仅热路径，但接口允许 | **仅热路径，接口允许** |
| Q4 暴露 Reservable | 内部使用，对外封装（可 unsafe） | 内部使用（强烈反对暴露） | 顶层不暴露；Builder 内受控暴露 | **顶层不暴露；Builder 内可选** |
| Q5 术语表 | Layer 0 独立术语表 | Layer 0 独立术语表 | Layer 0 独立 glossary + 映射 | **独立 glossary + 映射** |

分歧点主要集中在 Q3 的措辞（Gemini 更偏“默认 zero-copy”），以及 Q4 是否提供“高级 escape hatch”（我倾向在 Builder 内受控提供，以避免 Layer 1 复制）。

---

## 6) 最终推荐方案（可执行接口定义草案）

目标：接口本身即可承载“可测试条款”，并能把 v2 framing 条款直接映射到 Layer 0 的黑盒测试。

```csharp
using System;
using System.Buffers;

// Layer 0 概念：FrameTag 是 framing 层的 byte discriminator。
// Layer 1 再把 FrameTag 映射到 DataRecordKind/MetaRecordKind。
public readonly record struct FrameTag(byte Value);

public readonly record struct Address64(ulong Value);

public interface IElogFramer
{
     // 约束：单线程、非并发；同一时刻最多 1 个 open builder。

     Address64 Append(FrameTag tag, ReadOnlySpan<byte> payload);

     ElogFrameBuilder BeginFrame(FrameTag tag);

     // Layer 0 只承诺“flush 到 OS 可见”；fsync 顺序由 Layer 1 管。
     void Flush();
}

public ref struct ElogFrameBuilder
{
     public IBufferWriter<byte> Payload { get; }

     // 可选：仅供需要 payload 内回填的 codec（如 PairCount）使用。
     public Atelia.Data.IReservableBufferWriter? ReservablePayload { get; }

     public Address64 Commit();

     // Auto-Abort：未 Commit 就 Dispose 时，必须以“Padding 帧”收尾，
     // 保证 writer 仍可继续写后续 record，且 framing 不变量保持。
     public void Dispose();
}

public interface IElogScanner
{
     // 返回 ref-struct view（生命周期护栏）；需要持久化时调用方显式 copy。
     bool TryReadAt(Address64 recordStart, out ElogFrame frame);

     // reverse scan / resync：必须遵守 [R-RESYNC-DISTRUST-TAILLEN]。
     // 返回值：从尾到头枚举（不要求分配集合）。
     ElogReverseEnumerable ScanReverse();
}

public readonly ref struct ElogFrame
{
     public FrameTag Tag { get; }
     public ReadOnlySpan<byte> Payload { get; }
     public Address64 RecordStart { get; }
}
```

关键点（对应“可测试闭环”）：

- `Append/Commit` 的输出 bytes 必须满足 v2 framing 现有条款（[F-MAGIC-RECORD-SEPARATOR]、[F-RECORD-WRITE-SEQUENCE] 等）。
- `Dispose` 的 Auto-Abort 必须落到“Padding 帧（保留 FrameTag）”，否则无法在黑盒测试中断言“异常路径仍保持 framing 不变量”。
- `TryReadAt` 只接受 `Address64`，让类型系统参与边界约束；避免 `Ptr64` 泛化导致误用。

---

如果主持人同意，我建议下一步把上述接口草案同步写成 6-10 条 Layer 0 的条款 ID（`[A-ELOG-...]` + `[S-ELOG-...]`），并为每条条款至少放 1 个 seed Test Vector，作为实现/重构的回归基线。

---

## 📋 主持人总结 (刘德智)

三位顾问的审阅非常深入！让我整理共识和决策。

### 🎯 核心共识

三位顾问在所有关键问题上达成一致：

| 问题 | 共识方案 | 关键理由 |
|------|----------|----------|
| **Q1 写入** | W4 + Transactional Builder | 简单场景有简洁 API，复杂场景可 zero-copy；Auto-Abort 防死锁 |
| **Q2 读取** | R1（ref struct） | Span 的限制是"生命周期护栏"，防止 use-after-free |
| **Q3 zero-copy** | 仅热路径 | Commit/Load MUST 支持；Recovery MAY 不追求 |
| **Q4 暴露 Reservable** | 顶层不暴露，Builder 内可选 | 降低认知负担，但保留上层 codec 的回填能力 |
| **Q5 术语表** | Layer 0 独立 glossary | 分层隔离原则；Layer 1 做映射 |

### 🔑 关键设计决策

#### 1. 术语隔离（概念边界清晰化）

```
Layer 0 术语          Layer 1 术语
─────────────────     ─────────────────
FrameTag (byte)   ←→  RecordKind (enum)
Address64         ←→  Ptr64 (alias)
Frame             ←→  Record
IElogFramer       ←→  StateJournal
```

#### 2. Transactional Builder（防死锁机制）

```csharp
using var frame = framer.BeginFrame(tag);
try {
    frame.Payload.Write(data);
    return frame.Commit();  // 正常提交
} 
// 异常/未 Commit → Dispose 自动写入 Padding 帧 → Writer 保持健康
```

#### 3. 可丢弃帧（FrameTag.Padding）

**关键洞察**（来自 GPT）：Auto-Abort 必须有"可落盘的无害形态"，否则要么缓冲整条 payload（丢掉 zero-copy），要么 rollback 机制复杂化。

解决方案：预留 `FrameTag = 0x00` 为 Padding，Layer 1 读取时忽略。

### 📐 最终接口定义

```csharp
// Layer 0 术语
public readonly record struct FrameTag(byte Value) {
    public static readonly FrameTag Padding = new(0x00);
}
public readonly record struct Address64(ulong Value);

// 写入接口
public interface IElogFramer {
    Address64 Append(FrameTag tag, ReadOnlySpan<byte> payload);
    ElogFrameBuilder BeginFrame(FrameTag tag);
    void Flush();
}

public ref struct ElogFrameBuilder : IDisposable {
    public IBufferWriter<byte> Payload { get; }
    public IReservableBufferWriter? ReservablePayload { get; }  // 可选
    public Address64 Commit();
    public void Dispose();  // Auto-Abort: 未 Commit 则写 Padding
}

// 读取接口
public interface IElogScanner {
    bool TryReadAt(Address64 addr, out ElogFrame frame);
    ElogReverseEnumerable ScanReverse();
}

public readonly ref struct ElogFrame {
    public FrameTag Tag { get; }
    public ReadOnlySpan<byte> Payload { get; }
    public Address64 Address { get; }
}
```

### 🛠️ 后续行动

| # | 行动 | 产出 |
|---|------|------|
| 1 | 创建 `elog-format.md` | Layer 0 独立规格（Frame 结构、FrameTag 空间、Address64） |
| 2 | 从 mvp-design-v2.md 提取 framing 条款 | `[E-xxx]` 前缀的条款编号空间 |
| 3 | 创建 `elog-test-vectors.md` | Layer 0 独立测试向量 |
| 4 | 实现 `IElogFramer` + `ElogFrameBuilder` | 基于现有 `IReservableBufferWriter` |

### 📝 会议状态

**状态**：✅ 设计共识达成，接口文档已创建

**下一步**（调整后）：
1. ✅ 创建接口文档：[elog-interface.md](../../atelia/docs/StateJournal/elog-interface.md)
2. 🔄 组织接口文档审阅（复验）
3. 接口稳固后，用它作为"手术刀"从 mvp-design-v2.md 切出 elog-format.md
4. 进入 Week 1-2 实施：ELOG Framing 实现

---

> **关键收获**：Transactional Builder + Auto-Abort 模式解决了"底层防呆"的核心问题——
> 一个上层的异常不会导致底层 Writer 死锁。这让 Layer 0 真正成为"可独立稳固"的基座。

---

