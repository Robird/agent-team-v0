# 畅谈会：TailMeta 读取接口设计

**时间**：2026-01-26
**主题**：设计"仅读取 TailMeta"的接口，用于大帧预览/筛选场景
**标签**：#design
**主持人**：TeamLeader

---

## 背景

### 问题陈述

RBF v0.40 新增了 **TailMeta** 概念：Payload 尾部的一部分成为用户可自定义的元数据存储空间，用于承载类似 `APE Tag / Vorbis Comment` 之类的可扩展元数据。TailMeta 的长度存储于 `FrameDescriptor` 的低 16 位（@[F-FRAME-DESCRIPTOR-LAYOUT]）。

**需求**：提供用 `SizedPtr Ticket` 单独读取 TailMeta 数据的功能（不完整读取 Payload），用于大帧预览或筛选。

### 当前实现

```csharp
// IRbfFrame 接口
public interface IRbfFrame {
    SizedPtr Ticket { get; }
    uint Tag { get; }
    ReadOnlySpan<byte> PayloadAndMeta { get; }  // 目前合并返回
    int TailMetaLength { get; }                  // 可从此推算 Payload/TailMeta 边界
    bool IsTombstone { get; }
}

// 读取方法
AteliaResult<RbfFrame> ReadFrame(SafeFileHandle file, SizedPtr ticket, Span<byte> buffer);
AteliaResult<RbfPooledFrame> ReadPooledFrame(SafeFileHandle file, SizedPtr ticket);
```

### 候选方案

**方案 A：专用接口**
- 新增 `ReadTailMeta()` 方法
- 可能需要配套返回值类型（如 `RbfTailMetaResult`）

**方案 B：扩展现有接口**
- 为 `ReadFrame` 增加参数 `bool tailMetaOnly = false`
- 在 `RbfFrame` 中引入 bit flags 记录帧状态（如 `PayloadSkipped`）
- 后续数据恢复场景可复用此 flags（如 `CrcFailed`）

### 需要讨论的问题

1. API 设计哲学：专用 vs 通用？
2. 返回类型设计：复用 `RbfFrame` 还是新类型？
3. Bit flags 设计：如果引入，应包含哪些状态？
4. I/O 策略：最小化 I/O 的实现路径？

---

## 发言记录

### TeamLeader 发言（开场）

作为 Tech Lead，我先抛出几个思考维度：

**1. 使用场景分析**

"仅读 TailMeta"的典型场景：
- **大文件筛选**：用户有一个 10GB 的 Payload，只想先看元数据决定是否需要完整读取
- **批量索引构建**：遍历所有帧，只提取元数据建立索引
- **恢复诊断**：在数据恢复场景，可能 Payload 已损坏但元数据完好

**2. I/O 视角**

从帧布局看（v0.40）：
```
[HeadLen 4B][Payload][TailMeta][Padding][PayloadCrc 4B][TrailerCodeword 16B]
```

要"仅读 TailMeta"，最小 I/O 需要：
1. 读 TrailerCodeword（16B）→ 获取 `TailMetaLen` 和 `PaddingLen`
2. 计算 TailMeta 在帧内的偏移
3. 只读 TailMeta 那段数据

这比读整帧能省很多 I/O（尤其是大 Payload 场景）。

**3. 初步倾向**

我个人略倾向 **方案 B + bit flags**，理由：
- 统一的返回类型便于上层处理
- Flags 可以复用于多种"部分数据"场景
- 但需要仔细设计，避免 flags 爆炸

请各位参谋畅所欲言。

---

### Craftsman 发言

我先把问题拆成 3 个彼此正交的维度（这样能看清候选方案是否"混轴"）：

1) **读什么**：整帧 / 只读 TailMeta / 只读 trailer 元信息  
2) **校验到哪**：完整 CRC（PayloadCrc + TrailerCrc）/ 仅 framing+TrailerCrc（不触碰 PayloadCrc）  
3) **buffer 所有权**：调用方给 Span（zero-copy view）/ 内部从 ArrayPool 租（需要 Dispose）

现状里这三轴其实已经很清晰：  
- `ReadFrame` 属于「整帧 + 完整 CRC +（Span 或 Pool）」  
- `ScanReverse` 属于「只读元信息 + framing/TrailerCrc + 无 payload」  
因此"只读 TailMeta"天然是一个**新点**：`TailMeta-only + TrailerCrc +（Span 或 Pool）`。不建议硬塞进 `ReadFrame` 这条线。

---

#### 方案 A（专用 ReadTailMeta）：我倾向它（并建议做一个小变体）

**优点（正交性/一致性）**
- 不污染 `ReadFrame` 的语义边界：规范里已经写了 `ReadFrame` 做 PayloadCrc；TailMeta-only 若想省 I/O，客观上无法做 PayloadCrc（除非仍然把 payload 全读一遍，那就失去意义）。所以它不应该是 `ReadFrame` 的一个 mode。
- API 意图清晰：调用者看到方法名就知道"这是预览/筛选用途，不等价于完整读取"。
- 与 `ScanReverse` 的职责分离高度一致：`ScanReverse` 给出 `RbfFrameInfo`，`ReadTailMeta` 负责把那段 bytes 拿回来 —— 上层组合很自然。

**优点（类型安全/误用防护）**
- 可以用返回类型把"你拿不到 payload"写进类型系统里，避免 flags/if 分支散落在上层：
  - 返回 `RbfTailMetaFrame`（`readonly ref struct`）只暴露 `TailMeta`，不给 `Payload`/`PayloadAndMeta`，误用面直接收敛。
  - pooled 版本返回 `RbfPooledTailMeta`，只租 `TailMetaLength` 那么大，避免大帧场景下"为了看 200B meta 租 10GB buffer"的灾难。

**实现复杂度（可控）**
- 读取路径最小 I/O 可以是：读 16B TrailerCodeword → 校验 TrailerCrc + 解 descriptor → 计算 tailMetaOffset → 读 TailMetaLen bytes。  
- 现有 `ValidateAndParseCore` 假设拿到完整帧 buffer；为了复用逻辑，建议抽一个"只基于 TrailerCodeword + TailLen 的解析/校验"小 helper（避免重复实现 framing 规则）。这属于低风险重构，不会引入兼容层。

**兼容性**
- 纯新增方法/类型：对现有调用方是零破坏。

**我建议的变体（让 API 更好用且更省 I/O）**
- 两组重载（和现有 ReadFrame/ReadPooledFrame 的风格一致）：
  - `ReadTailMeta(file, ticket, buffer)` → `AteliaResult<RbfTailMetaFrame>`（无需先 ScanReverse，适合"我手上只有 ticket"的场景）
  - `ReadTailMeta(file, info, buffer)` → `AteliaResult<RbfTailMetaFrame>`（走 ScanReverse pipeline 的热路径，避免重复读 trailer）
  - `ReadPooledTailMeta(file, ticket)` / `ReadPooledTailMeta(file, info)`（只租 TailMetaLength）
- 语义上明确：**只保证 TrailerCrc/framing 校验，不做 PayloadCrc**（这点必须写进 XML doc/规范条款里，防止上层误把它当"数据完整性保证"）。

---

#### 方案 B（ReadFrame 加 bool + RbfFrame flags）：我不建议

**核心问题：混轴导致的"语义污染"**
- `ReadFrame` 从名字上看仍然像"读帧"，但它无法保持 `ReadFrame` 现有的"完整 CRC"承诺（除非仍然读全 payload）。这会让上层对"ReadFrame 成功=payload 可信"产生错觉。
- `bool tailMetaOnly` 是经典"布尔陷阱"：调用点几个月后很难一眼看出 `true/false` 表示什么；更糟的是它会鼓励后续继续堆 `bool skipPayloadCrc / bool skipPayload / bool allowCrcFail...`，最终 API 变成不可维护的参数矩阵。

**类型安全/误用风险**
- 让 `RbfFrame` 进入"部分填充态"会迫使所有使用 `RbfFrame` 的地方增加 flags 判断；漏判断就是 silent bug。
- Flags "可复用到恢复场景（CrcFailed 等）"这个方向我理解，但它把**读取结果（数据视图）**和**诊断状态（校验/恢复信息）**耦合在一起，后续一定出现"flags 组合爆炸 + 语义不自洽"的维护成本。

**实现上也未必更省**
- 如果仍然返回 `RbfPooledFrame`，其当前实现是按 `ticket.Length` 租 buffer；在"大 payload、小 TailMeta"场景，这与需求相违背，除非再引入更多分支与新的内部布局约定，复杂度并不比方案 A 小。

---

#### 我的结论

- **我倾向：方案 A（专用 ReadTailMeta）**，并强烈建议"Span 版 + Pooled 版"都提供，且 pooled 版只租 TailMetaLen。  
- **不建议：方案 B（bool + flags 把 ReadFrame 做成多模式）**，它会破坏接口正交性与 `ReadFrame` 的稳定语义，长期会把上层代码拖进 flags 地狱。

如果团队希望"减少类型数量"，我也给一个折中替代：`ReadTailMeta(..., Span<byte> buffer)` 返回 `int`（写入字节数），其余元信息让调用方从 `RbfFrameInfo` 取；但我个人仍更喜欢 `RbfTailMetaFrame` 这种"类型即约束"的方案（误用面最小）。

---



### Seeker 发言

我同意 Craftsman 的结论（方案 A），但想从更底层追问一个问题：**TailMeta 本质上是什么？**

#### 1. 本体论追问：TailMeta 的身份

从设计意图看，TailMeta 有两种可能的身份：

- **身份 A：Payload 的附属物**——它是 Payload 的"标签"，与 Payload 共生共灭，校验/信任边界与 Payload 绑定
- **身份 B：独立的元数据域**——它是帧级别的独立存储空间，拥有自己的信任边界，可独立于 Payload 存在

当前设计隐含的是 **身份 B**（TailMeta 与 Payload 有不同的读取路径），但这个假设值得显式确认。因为：

- 如果是身份 A，那么"仅读 TailMeta 而不校验 PayloadCrc"在语义上是**不完整的信任**——用户拿到的元数据可能与实际 Payload 不一致（比如 Payload 已损坏但 TailMeta 完好）
- 如果是身份 B，则需要考虑：TailMeta 自己要不要有 CRC？当前设计中 TailMeta 的完整性依赖于 TrailerCrc，这足够吗？

#### 2. 信任模型的三个层次

Craftsman 提到的"只保证 TrailerCrc/framing 校验，不做 PayloadCrc"，我建议更精确地表述为**三层信任模型**：

| 层次 | 校验内容 | 信任断言 |
|:-----|:---------|:---------|
| L1: Framing | TrailerCodeword 可解码 | "这是一个结构合法的帧" |
| L2: Meta | TrailerCrc 通过 | "帧元信息（含 TailMeta）未被篡改" |
| L3: Full | PayloadCrc 通过 | "整个帧内容（Payload + Meta）完整" |

`ReadTailMeta` 提供的是 **L2 信任**，这应该在 API 文档中显式声明。

#### 3. 一个边界情况：TailMetaLength = 0

当帧没有 TailMeta 时（`TailMetaLength = 0`），`ReadTailMeta` 应该：
- 返回成功 + 空 Span？
- 返回特定错误码？

我倾向前者（返回成功 + 空 Span），因为"没有元数据"也是一种合法的元数据状态。但这需要在规范中明确。

#### 4. 关于类型设计的补充

Craftsman 建议的 `RbfTailMetaFrame` 我同意其方向。但有一个细节：它是否需要暴露 `Ticket`？

- 如果暴露：上层可以拿着这个 Ticket 去 `ReadFrame` 读取完整帧
- 如果不暴露：更强的封装，但上层需要自己维护 Ticket 与 TailMeta 的关联

考虑到典型用例是"预览后决定是否读取完整帧"，**暴露 Ticket 是合理的**。

#### 5. 我的倾向

- **结论**：同意方案 A
- **补充建议**：
  1. 在规范中显式定义 TailMeta 的身份（独立元数据域 vs Payload 附属物）
  2. 在 API 文档中显式声明信任层次（L2: TrailerCrc，非 L3: PayloadCrc）
  3. 明确 `TailMetaLength = 0` 的行为
  4. `RbfTailMetaFrame` 应暴露 `Ticket` 以支持"预览→完整读取"工作流

---

### Curator 发言

前两位的分析非常扎实，我从**用户体验**与**API 示能性**角度做三点补充：

#### 1. 方案 A 的体验优势：意图即命名

Craftsman 说"调用者看到方法名就知道"——这正是我最看重的。

从 DX 视角，`ReadTailMeta()` 是**自解释 API**：
- 它回答"我能做什么"（示能性）
- 它不需要查文档就能猜对用法

而 `ReadFrame(..., tailMetaOnly: true)` 是**配置型 API**：
- 它把"预览"伪装成"读帧的一种模式"
- 布尔参数在调用点变成 `true/false`，三个月后没人记得这是什么

我的经验法则：**如果参数改变的是"做什么"而非"怎么做"，就应该是独立方法**。

#### 2. 类型设计：`RbfTailMetaFrame` 应该"诚实地贫瘠"

Craftsman 建议用独立类型避免误用，我完全同意，但想强调一点：

**类型的字段清单就是对用户的承诺**。

建议 `RbfTailMetaFrame` 只暴露：
- `Ticket`（Seeker 提的，支持"预览→完整读取"工作流）
- `Tag`
- `TailMeta`（`ReadOnlySpan<byte>`）
- `IsTombstone`

**不暴露 `PayloadLength`**——虽然技术上我们知道这个值，但暴露它会暗示"你可以对 Payload 做点什么"。这是**虚假示能**。用户想知道 Payload 多大？请调用 `ScanReverse` 或 `ReadFrame`。

#### 3. 信任层次的 UX 表达：错误码即教育

Seeker 提出的 L1/L2/L3 信任模型很有价值，但不应只写在文档里——应该**编进类型系统或错误码**。

建议：
- `ReadTailMeta` 成功时返回的类型本身就叫 `RbfTailMetaFrame`（不叫 `RbfFrame`），用**类型名告诉用户"你拿到的是元数据，不是完整帧"**
- 如果用户拿着 `TailMetaFrame` 的 `Ticket` 去 `ReadFrame`，然后 PayloadCrc 校验失败，错误码应该明确说 `PayloadCorrupted`——这时用户会意识到"原来预览不保证 Payload 完整性"

这是我说的 **Error as Navigation**——错误消息不是死胡同，而是路标。

#### 结论

- **同意方案 A**，理由：正交、诚实、自解释
- 补充建议：类型设计要"诚实地贫瘠"，不暴露你不想让用户依赖的信息
- 信任模型应该通过**类型名**和**错误码**表达，而不只是文档

---

## 决策总结

### TeamLeader 发言（收敛）

感谢三位参谋的深入分析！会议共识已经非常清晰。

#### 决策：采用方案 A（专用 `ReadTailMeta` 接口）

| 议题 | 决议 |
|:-----|:-----|
| API 选择 | 新增专用 `ReadTailMeta()`，不扩展现有 `ReadFrame` |
| 返回类型 | 新建 `RbfTailMetaFrame`（ref struct）+ `RbfPooledTailMeta`（class） |
| CRC 策略 | 只做 TrailerCrc（L2 信任），显式声明不做 PayloadCrc |
| `TailMetaLength = 0` | 返回成功 + 空 Span |

#### 类型设计草案

```csharp
/// <summary>TailMeta 预览结果（L2 信任级别：仅保证 TrailerCrc）。</summary>
public readonly ref struct RbfTailMetaFrame {
    public SizedPtr Ticket { get; init; }     // 支持"预览→完整读取"工作流
    public uint Tag { get; init; }
    public ReadOnlySpan<byte> TailMeta { get; init; }
    public bool IsTombstone { get; init; }
    // 注意：不暴露 PayloadLength（诚实地贫瘠）
}
```

#### API 签名草案

```csharp
// IRbfFile 扩展
RbfTailMetaFrame ReadTailMeta(RbfFrameInfo info, Span<byte> buffer);
RbfPooledTailMeta ReadPooledTailMeta(RbfFrameInfo info);

// 内部实现（RbfReadImpl）
AteliaResult<RbfTailMetaFrame> ReadTailMeta(SafeFileHandle file, RbfFrameInfo info, Span<byte> buffer);
AteliaResult<RbfPooledTailMeta> ReadPooledTailMeta(SafeFileHandle file, RbfFrameInfo info);
```

#### 采纳的设计原则

1. **正交性**：`ReadFrame`（整帧+L3）vs `ReadTailMeta`（元数据+L2）vs `ScanReverse`（元信息+L1）
2. **意图即命名**：方法名自解释，不用布尔参数伪装不同操作
3. **类型即约束**：用独立类型阻止误用，而非运行时 flags 检查
4. **诚实地贫瘠**：只暴露能信任的信息

#### 待办

- [ ] 在 `rbf-interface.md` 中补充 TailMeta 身份定义（独立元数据域）
- [ ] 在 `rbf-interface.md` 中补充三层信任模型（L1/L2/L3）
- [ ] 实现 `RbfTailMetaFrame` 和 `RbfPooledTailMeta` 类型
- [ ] 实现 `RbfReadImpl.ReadTailMeta`
- [ ] 实现 `RbfFileImpl.ReadTailMeta` + `IRbfFile` 接口扩展
- [ ] 添加测试用例

#### 致谢

- **Craftsman**：三轴分解 + 误用防护分析
- **Seeker**：本体论追问 + 三层信任模型
- **Curator**：DX 视角 + "诚实地贫瘠"原则

会议结束。

---
