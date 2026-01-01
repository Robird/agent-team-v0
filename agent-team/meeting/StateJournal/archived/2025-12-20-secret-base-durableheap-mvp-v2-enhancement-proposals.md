# 秘密基地畅谈会：DurableHeap MVP v2 局部增强提案探讨

> **日期**：2025-12-20
> **主持人**：刘德智 / SageWeaver
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
> **议题**：5 个局部增强提案的评估与决策
> **目标**：达成共识，决定是否接纳、改进或否决，并更新设计文档。

---

## 提案清单

### 提案 1：预留 ObjectId 空间
**内容**：关于“`NextObjectId = 1`（`ObjectId = 0` 保留给 VersionIndex 或其他 well-known 对象）”。建议初始就划分出一块 ObjectId 空间（例如 0-255），用于 well-known 对象，预留给后续演进使用。

### 提案 2：非断链 Checkpoint Base
**内容**：关于 **Genesis Base**、**Checkpoint Base**、`PrevVersionPtr=0`。
**建议**：用 `PrevVersionPtr` 的最高位（MSB）为 1 表达 **Checkpoint Base**，并用剩余的低 63 bit 存根前一 Version 的 `PrevVersionPtr`。
**目标**：
1. 达成周期性全量保存功能（Checkpoint）。
2. 保持版本历史不断链（可回溯到更早历史）。
3. 不增加字段。
4. 文档描述和实现都能控制在局部。

### 提案 3：格式版本化策略
**内容**：关于文件格式的版本化。
**选项**：
- **A. 每个对象独立记录版本**（用户倾向）：升级无需转换，自然写入新版。需决定是增加专用 byte 还是复用 `ObjectKind`（如 `ObjectKind.DictV2`）。
- **B. 每个 Epoch 记录版本**：需升级所有可达旧版数据。
- **C. 每个文件记录版本**：需文件级 Compaction。

### 提案 4：规范描述去重
**内容**：两处“`KeyValuePairType`: `byte`”的重复表达合并为一处。

### 提案 5：泛型误导性修正
**内容**：关于 `DurableDict<TValue>` 伪代码中的 `<TValue>` 泛型参数。
**问题**：可能引起误解，因为 DurableDict 更类似 `JObject`，不能提供任意 Value 的类型特化，只能支持有限内置类型。
**建议**：研究改良表述。

---

## 第一轮：独立观点阐述 (Round 1: Independent Analysis)

请各位顾问针对上述 5 个提案发表独立观点。请关注：
- **必要性**：是否真的需要现在做？
- **副作用**：是否引入了不必要的复杂性？
- **实现成本**：是否符合 MVP 原则？

---

### DocUIClaude (Round 1)

以下是我从**概念框架**视角对五个提案的独立分析：

---

#### 提案 1：预留 ObjectId 空间

**立场：✅ 赞同，但建议缩小范围**

**利**：
1. **前瞻性设计**：预留空间是低成本的"未来保险"，避免后续破坏性变更
2. **语义清晰**：well-known ID 与动态分配 ID 天然分离，概念上更干净
3. **实现简单**：只需 `NextObjectId = 256`，几乎零成本

**弊**：
1. **YAGNI 风险**：除了 VersionIndex，目前没有其他 well-known 对象的明确需求
2. **文档负担**：需要定义"well-known 空间的分配规则"

**建议**：
- 预留 0-7（8 个）而非 0-255，符合 MVP 最小原则
- `ObjectId=0` 保留给 VersionIndex（当前唯一需求）
- 明确声明"1-7 保留供未来使用"
- 这样既保留扩展性，又避免过度设计

---

#### 提案 2：非断链 Checkpoint Base

**立场：⚠️ 有保留，需权衡复杂性**

**利**：
1. **历史可追溯**：满足"时间线连续性"的核查需求
2. **空间效率**：复用现有字段，无额外空间开销
3. **概念优雅**：Checkpoint 变成"标记点"而非"截断点"

**弊**：
1. **复杂性增加**：MSB 位域编码增加了格式层复杂度
   - 读写时需要位操作
   - 调试时难以直观理解原始数据
2. **MVP 适用性存疑**：MVP 阶段真的需要历史不断链吗？
3. **与 Compaction 的交互**：如果未来支持 Compaction（删除旧版本），"不断链"承诺如何兑现？

**类比分析**：
这类似 Git 的 `shallow clone` vs `full clone` 之争。非断链相当于强制 full clone，代价是永远携带历史。

**建议**：
- **MVP 阶段选择简单方案**：`PrevVersionPtr=0` 表示 Checkpoint Base（断链）
- **预留 MSB 语义**：文档声明"MSB 保留供未来扩展"，但不实现非断链
- 理由：先发货再优化，非断链可作为 v2.1 特性

---

#### 提案 3：格式版本化策略

**立场：✅ 赞同选项 A（对象级版本）**

**利**：
1. **渐进升级**：无需全量转换，符合 DurableHeap "在线演进"哲学
2. **向后兼容**：旧版本对象保持可读
3. **解耦升级**：不同对象类型可独立演进

**选项 A 的子决策**：

| 方案 | 利 | 弊 |
|------|----|----|
| 新增 version byte | 语义清晰、独立演进 | 每对象增加 1 字节开销 |
| 复用 ObjectKind (如 `DictV2`) | 零额外空间 | Kind 空间可能耗尽、语义耦合 |

**建议**：
- MVP 选择"复用 ObjectKind"——当前只有 Dict 一种对象，Kind 空间充裕
- 文档声明"Kind 枚举的 0-127 用于基础类型，128-255 保留给版本变体"
- 这是最小成本方案，且保留了未来改用独立 version byte 的路径

---

#### 提案 4：规范描述去重

**立场：✅ 强烈赞同**

**利**：
1. **符合 SSOT 原则**：单一权威定义避免不一致风险
2. **维护成本降低**：修改只需改一处
3. **阅读体验改善**：减少冗余，提高信息密度

**弊**：
- 无明显弊端

**建议**：
- 立即执行，这是典型的"低成本高收益"改进
- 保留定义于术语表或首次出现处，其他位置改为引用

---

#### 提案 5：泛型误导性修正

**立场：✅ 赞同，这是概念清晰度问题**

**问题诊断**：
`DurableDict<TValue>` 传达的心智模型是"强类型容器"（如 `Dictionary<K, V>`），但实际语义是"动态类型容器"（如 `JObject`）。这是概念层与表述层的错位。

**改良方案评估**：

| 方案 | 优点 | 缺点 |
|------|------|------|
| `DurableDict` (去泛型) | 反映真实语义 | 失去"值有约束"的暗示 |
| `DurableDict<DurableValue>` | 明示值类型 | 需要定义 `DurableValue` 类型 |
| 伪代码注释 + 约束说明 | 成本最低 | 仍可能误导 |

**建议**：
- MVP 采用 `DurableDict`（去泛型）+ 文档说明"Value 限于 ValueType 枚举支持的类型"
- 未来如果引入 `IDurableValue` 接口，再考虑 `DurableDict<IDurableValue>`
- 在伪代码开头增加显式声明：`// Value types: null | varint | ObjRef | Ptr64`

---

#### 总结优先级

| 提案 | 建议 | 优先级 | 理由 |
|------|------|--------|------|
| 提案 4 | 立即执行 | P0 | 零成本去重 |
| 提案 5 | 立即执行 | P0 | 修正概念误导 |
| 提案 1 | 接纳（缩小为 0-7） | P1 | 低成本预留 |
| 提案 3 | 接纳（复用 Kind） | P1 | MVP 最小方案 |
| 提案 2 | 推迟 | P2 | 复杂度与 MVP 不匹配 |

---

### DocUIGemini (Round 1)

我是 **DocUIGemini**。我将从 **UX (开发者体验)** 和 **实现复杂度 (Implementation Friction)** 的角度进行审视。

我的核心判据是：**"这个设计会让开发者（包括实现者和使用者）感到困惑还是愉悦？"**

---

#### 提案 1：预留 ObjectId 空间

**立场：✅ 赞同 (Support)**

**DX 视角**：
- **消除魔法数字**：如果没有预留空间，开发者在处理特殊对象（如 VersionIndex）时往往会硬编码 `if (id == 0)`。预留空间将这种"特殊性"合法化、区域化。
- **心理安全区**：知道 `0-255` 是"系统区"，`256+` 是"用户区"，这种明确的边界能降低开发者的认知负荷。

**复杂度**：
- **极低**。只需修改 `NextObjectId` 的初始值。

**建议**：
- 同意 Claude 的观点，MVP 不需要 255 个，**0-15** 足矣。
- 关键不在于数量，而在于**确立"保留区"这一概念**。

---

#### 提案 2：非断链 Checkpoint Base (MSB Hack)

**立场：🛑 反对 (Object)**

**DX 视角**：
- **调试敌意 (Hostile to Debugging)**：对于查看 Hex Dump 的开发者，`PrevVersionPtr` 突然变成负数（或极大值）是非常反直觉的。位掩码（Bit-masking）隐藏了真实数据的直观性。
- **心智模型冲突**：Checkpoint 的通常语义就是"截断"（Truncate/Snapshot）。"既是 Checkpoint 又是增量链的一部分"是一个混合概念，容易造成理解混乱。如果用户真的需要完整历史，他们通常不会执行 Checkpoint 操作，或者会归档旧文件。

**复杂度**：
- **高风险**。位操作是 Off-by-one 错误和符号位错误的温床。
- 在 MVP 阶段引入这种优化是过早优化。

**建议**：
- **保持简单**：`PrevVersionPtr = 0` (或 null) 表示链条终结。
- 如果必须保留历史指针，请使用显式的额外字段（虽然浪费空间，但清晰），或者推迟到 v2 解决。
- **MVP 原则**：显式优于隐式。不要为了省 8 个字节而牺牲清晰度。

---

#### 提案 3：格式版本化策略

**立场：✅ 赞同选项 A (复用 ObjectKind)**

**DX 视角**：
- **平滑演进**：开发者最怕"停机迁移"。对象级版本化允许系统在运行时逐步消化新旧格式，这是极佳的运维体验。
- **自描述性**：`ObjectKind.DictV2` 比 `Dict + VersionByte=2` 更符合枚举的直觉，且在调试日志中更易读。

**复杂度**：
- **中等**。需要在反序列化时增加 switch case，但这是标准做法。

**建议**：
- 采用 **Kind-as-Version** 策略。
- 这是一个经典的 **TLV (Tag-Length-Value)** 变体，其中 Tag 隐含了 Version。这是二进制协议设计的最佳实践之一。

---

#### 提案 4：规范描述去重

**立场：✅ 强烈赞同 (Strong Support)**

**DX 视角**：
- **文档即契约**：重复的定义是"契约腐烂"的开始。开发者看到两处定义，第一反应往往是"这两处有什么细微差别吗？"，这增加了不必要的确认成本。

**建议**：
- **DRY (Don't Repeat Yourself)** 不仅适用于代码，也适用于文档。

---

#### 提案 5：泛型误导性修正

**立场：✅ 强烈赞同 (Strong Support)**

**DX 视角**：
- **API 诚实性 (API Honesty)**：`DurableDict<T>` 许下了一个它无法兑现的承诺（即支持任意 T）。当开发者尝试传入 `DurableDict<MyUser>` 并发现编译错误或运行时异常时，这是一种**挫败感 (Frustration)**。
- **类比**：它应该像 `JObject` 或 `BsonDocument`，而不是 `Dictionary<K,V>`。

**建议**：
- 去掉泛型，改名 `DurableDict`。
- 在文档中明确其行为类似于 **弱类型字典** 或 **JSON 对象**。
- 这不仅是文档修正，更是**心智模型校准**。

---

#### 总结

| 提案 | 态度 | 关键词 |
| :--- | :--- | :--- |
| 提案 1 (预留 ID) | ✅ 赞同 | **心理边界** |
| 提案 2 (MSB Hack) | 🛑 反对 | **调试敌意** |
| 提案 3 (版本化) | ✅ 赞同 | **平滑演进** |
| 提案 4 (去重) | ✅ 赞同 | **契约一致性** |
| 提案 5 (去泛型) | ✅ 赞同 | **API 诚实性** |


### DocUIGPT (Round 1)

我从 **规范严谨性（可判定/可测试）**、**一致性（术语/字段语义不漂移）**、以及 **Markdown 作为“规格载体”的格式质量** 三个角度补充。

---

#### 跨提案（P0）——把“讨论”收敛为“可执行契约”

- 建议为每个提案补一个小段落：**Normative Contract**（用 MUST/SHOULD/MAY），并明确“未知值/损坏/不支持”的处理策略（fail-fast vs best-effort）。否则同样的提案会被实现者补成不同的隐含规则。
- 建议把涉及二进制编码的字段都写成“字段表/编码表”的 SSOT（单一真相来源），其余文字段落只做解释并引用该表。提案 4 的去重可以顺势把 SSOT 机制落地。

---

#### 提案 1：预留 ObjectId 空间

- 我更关心“规则是否写死”，而不是 N 取 7/15/255：
  - MUST 明确 `ObjectId=0` 的唯一含义（例如 VersionIndex）。
  - MUST 明确“系统保留区”和“动态分配区”的边界，以及动态分配的起始值（`NextObjectId`）。
  - SHOULD 明确未知 well-known id 的处理（推荐 fail-fast），避免静默读取造成幽灵状态。

---

#### 提案 2：非断链 Checkpoint Base（MSB 复用 PrevVersionPtr）

- MVP 阶段我倾向 **不接纳** MSB hack，原因不是“位运算丑”，而是它必须先补齐位级规格与恢复语义，否则实现必分叉。
- 若坚持该方案，规范至少需要：
  - 明确 `PrevVersionPtr` 的位宽/字节序；定义 `IsCheckpointBase` 与 `PrevPtr63` 的精确取值规则。
  - MUST 说明 `PrevPtr63` 指向的对象（逻辑链值 vs 物理偏移），这两者会直接改变读路径。
  - MUST 规定保留位必须为 0、以及遇到保留位非 0 的处理（建议视为损坏）。
- 更稳的折中：MVP 继续用 `PrevVersionPtr = 0` 表示 base（允许断链），同时把 MSB 标记写成“保留但未启用”的格式预留位。

---

#### 提案 3：格式版本化策略

- 对象级版本（A）是更符合渐进演进的路径，但无论采用 `ObjectKind.DictV2` 还是 `VersionByte`，规范都必须写死“未知版本/未知 kind”的处理策略。
- 建议统一术语，避免“版本”混指：
  - **Format Version**（编码/对象格式） vs **Epoch/Version Chain**（语义版本链）。

---

#### 提案 4：规范描述去重

- 强烈赞同，并建议把重复的 `KeyValuePairType: byte` 收敛进一个字段表（SSOT），其他地方只做引用。

---

#### 提案 5：泛型误导性修正

- 强烈赞同：`DurableDict<TValue>` 会给出“任意 T 可持久化”的错误承诺。
- 建议伪代码改为 `DurableDict`（无泛型），并在首次出现处列出 value 的封闭集合（例如 `ValueType` 枚举支持的类型），避免读者自行脑补。

---

#### Markdown（格式载体）建议

- 当前纪要里出现了孤立的 fenced code block（```）把普通列表包进了代码块，渲染会变形；建议后续修订时去掉该 fence，或只在真正的代码片段处使用 fenced block。
- 列表缩进建议统一用空格（避免不同 Markdown 引擎把 Tab/缩进识别成代码块）。

---

## 第二轮：交叉辩论 (Round 2: Cross Debate)

---

### DocUIClaude (Round 2)

我将针对两个核心分歧点进行回应，并给出明确建议。

---

#### 分歧点 1：预留空间大小（0-7 vs 0-15 vs 0-255）

**各方立场回顾**：
- **DocUIClaude (Round 1)**：建议 0-7（8 个）
- **DocUIGemini (Round 1)**：建议 0-15（16 个）
- **原提案**：建议 0-255（256 个）

**我的分析**：

三个方案的本质差异在于对"未来需求不确定性"的态度：

| 方案 | 预留数量 | 隐含假设 |
|------|----------|----------|
| 0-7 | 8 | "我们只需要少量 well-known 对象" |
| 0-15 | 16 | "保守一点，翻倍余量" |
| 0-255 | 256 | "给一个完整的字节边界" |

**关键洞察**：这是一个**单调递增的决策**——我们只能提高起始 ID，不能降低。因此选择标准应是"最小可用"而非"最大保险"。

**具体推算**：
- 当前已知需求：`ObjectId=0` 用于 VersionIndex（1 个）
- 潜在需求：Configuration 对象、Schema 对象、Metadata 对象……乐观估计 5-10 个
- **8 个足够覆盖 MVP + 近期演进**
- **16 个提供 2x 余量**，且仍在单 nibble（半字节）范围内

**最终建议：采纳 0-15（16 个）**

理由：
1. **与 Gemini 收敛**：避免继续争论，16 个是合理的折中
2. **实用考量**：16 = 2^4，是二进制边界，调试时直观（一个 hex 字符）
3. **MVP 原则未违反**：`NextObjectId = 16` vs `NextObjectId = 8` 的实现成本完全相同

**Normative Contract 建议**（回应 GPT 的跨提案建议）：

```
[F-WK-01] MUST: ObjectId 0-15 为系统保留区（System-Reserved Range）
[F-WK-02] MUST: ObjectId=0 用于 VersionIndex
[F-WK-03] MUST: ObjectId 1-15 保留供未来使用，当前 MUST NOT 分配
[F-WK-04] MUST: 动态分配起始值 NextObjectId = 16
[F-WK-05] SHOULD: 遇到未知 well-known ObjectId (1-15) 时 fail-fast
```

---

#### 分歧点 2：Checkpoint 断链问题（MSB Hack vs 简单方案）

**各方立场回顾**：
- **DocUIGemini**：🛑 强烈反对 MSB Hack（"调试敌意"、"位运算是错误温床"）
- **DocUIGPT**：倾向不接纳，除非补齐位级规格与恢复语义
- **DocUIClaude (Round 1)**：有保留，建议 MVP 选择简单方案

**核心问题澄清**：

用户要求我确认：**MVP 阶段是否可以接受 Checkpoint 导致版本链断裂？**

我的回答是：**✅ 可以接受**。

**论证**：

1. **Checkpoint 的核心价值是什么？**
   - 减少恢复时需要遍历的版本链长度
   - 提供全量快照作为恢复基准点
   - **不是**保留完整历史追溯能力（这是 Git 的职责）

2. **断链的代价是什么？**
   - 无法从 Checkpoint 后的版本追溯到 Checkpoint 前的版本
   - 如果用户需要完整历史，他们应该**不执行 Checkpoint** 或**归档旧文件**

3. **非断链的代价是什么？**
   - MSB hack 增加格式复杂度（位操作、调试不友好）
   - 需要补齐大量位级规格（字节序、保留位处理、逻辑/物理指针区分）
   - 与 Compaction（未来可能的空间回收）形成概念冲突

4. **MVP 原则判断**：
   - 非断链是"nice-to-have"而非"must-have"
   - 复杂度与收益不成比例

**最终建议：正式否决提案 2**

理由：
1. **三位审阅者达成共识**：无人支持 MVP 阶段引入 MSB hack
2. **概念清晰度**：Checkpoint = 截断点，这是最符合直觉的语义
3. **预留演进路径**：如果未来确实需要非断链，可以：
   - 使用显式字段（如 `CheckpointPrevPtr: Address64?`）
   - 或者在 v2 重新设计 Commit Record 格式

**Normative Contract 建议**：

```
[S-CB-01] MUST: Checkpoint Base 的 PrevVersionPtr = 0
[S-CB-02] MUST: 从 Checkpoint Base 出发的版本链不可回溯到更早历史
[S-CB-03] MAY: 保留完整历史的需求由外部机制（如文件归档、Git）满足
```

**文档修订建议**：

在设计文档中明确声明：

> **设计决策：Checkpoint 断链**
> 
> MVP 阶段，Checkpoint Base 标志着版本链的起点，无法回溯到更早历史。
> 这是有意为之的简化设计。如果用户需要完整历史追溯能力，应在执行
> Checkpoint 前归档原始文件。

---

#### 附：对 GPT 跨提案建议的回应

GPT 提出的"Normative Contract"机制非常重要。我完全赞同。

建议在本次会议结束后，将所有接纳的提案都转化为带编号的条款（`[F-xx]`, `[S-xx]`, `[A-xx]`），
并补充失败语义（MUST fail-fast / SHOULD warn / MAY ignore）。

这样做可以：
1. 避免实现分叉
2. 让规格可测试
3. 为未来的兼容性测试套件奠定基础

---

#### 总结：本轮投票

| 提案 | 最终建议 | 说明 |
|------|----------|------|
| 提案 1 | ✅ 接纳（0-15） | 与 Gemini 收敛 |
| 提案 2 | ❌ 否决 | 三方共识，MVP 不适合 |
| 提案 3 | ✅ 接纳（复用 Kind） | 原立场不变 |
| 提案 4 | ✅ 接纳 | 原立场不变 |
| 提案 5 | ✅ 接纳 | 原立场不变 |


### DocUIGPT (Round 2)

我支持并确认本轮共识，并建议把它们落成**可测试的规范条款（Normative Clauses）**，以避免“投票通过但实现分叉”。下面条款全部面向 **DurableHeap MVP v2** 规范正文，使用 MUST/SHOULD/MAY。

#### 共识确认（本轮结论）

- 预留 ID：`ObjectId` 预留区间为 **0-15**。
- Checkpoint：**否决 MSB Hack**（禁止把标志位塞进指针），采用 **断链方案**（base 指针为 0）。
- 版本化：对象格式版本化 **复用 `ObjectKind`**（Kind 同时携带 codec 版本）。
- 去泛型：`DurableDict` 去掉 `<TValue>`，避免“任意类型可持久化”的误导。

---

## Normative Clauses

### Well-Known / Reserved ObjectId

- [F-WK-01] **MUST** 定义 `ObjectId` 为无符号整数标识符，并定义系统保留区间 `0..15`（含端点），称为 **System-Reserved ObjectId Range**。
- [F-WK-02] **MUST** 将 `ObjectId = 0` 保留给 **VersionIndex**（或规范明确指定的等价“全局索引对象”）；除非规范显式更新，否则实现 **MUST NOT** 赋予其其他含义。
- [F-WK-03] `ObjectId` 位于 `1..15` 的值为 **Reserved (Unassigned)**。实现 **MUST NOT** 在 MVP v2 中把这些 ID 分配给任意新建对象。
- [F-WK-04] 实现的动态分配起始值 **MUST** 为 `NextObjectId = 16`，且动态分配器 **MUST NOT** 返回 `0..15`。
- [F-WK-05] 当读取到对 `1..15` 范围内对象的可达引用（reachable reference），且该 `ObjectId` 未被规范定义为 well-known 对象时，实现 **MUST** 以确定性错误失败（fail-fast），并在错误中报告该 `ObjectId`。

### Checkpoint Base (断链；禁止 MSB Hack)

- [S-CB-01] **MUST NOT** 使用 `PrevVersionPtr`（或等价字段）的任何位（包括 MSB）表达“Checkpoint/Base”等标志语义；`PrevVersionPtr` 的全部位宽 **MUST** 仅表示指针/偏移本身（即“纯指针值”）。
- [S-CB-02] **MUST** 定义：当且仅当 `PrevVersionPtr == 0` 时，该版本节点为 **Checkpoint Base**（又称 base-of-chain）。
- [S-CB-03] 从任意版本节点回溯时，遇到 `PrevVersionPtr == 0` **MUST** 立即停止回溯；实现 **MUST NOT** 尝试“继续解码更早历史”。
- [S-CB-04] 生成 Checkpoint Base 的写入过程 **MUST** 保证：从该 base 及其后继链即可恢复出当前可达状态（reachable state），而不依赖 base 之前的任何历史记录。
- [S-CB-05] 若实现提供“生成 checkpoint”的 API，则该操作 **MAY** 允许丢弃（或变得不可达）更早版本历史；规范不要求“跨 checkpoint 的追溯连续性”。

### Object-Level Versioning via ObjectKind

- [F-VER-01] 对象格式版本化 **MUST** 采用对象级（per-object）策略；版本化粒度是“对象 codec”，而不是“epoch”或“文件”。
- [F-VER-02] `ObjectKind` **MUST** 作为对象 codec 的判别字段，且 **MUST** 同时编码“对象类型 + 该类型的格式版本”。（示例：`DictV1`、`DictV2` 为不同的 `ObjectKind` 值。）
- [F-VER-03] 一旦某个 `ObjectKind` 数值被分配，其语义与字节级编码 **MUST NOT** 在后续版本中改变；演进 **MUST** 通过分配新的 `ObjectKind` 数值实现。
- [F-VER-04] 写入端在生成新对象时，**SHOULD** 使用实现所支持的最新 `ObjectKind` 版本；但该选择 **MUST** 不影响读取端对既有对象的向后兼容性（backward compatibility）。
- [F-VER-05] 读取端在遍历可达对象图（reachable object graph）时，若遇到未知（unsupported）`ObjectKind`，实现 **MUST** 以确定性错误失败（fail-fast），并在错误中报告该 `ObjectKind` 值；实现 **MUST NOT** 静默忽略或以“空对象/默认值”替代。

### DurableDict 去泛型（API 诚实性）

- [A-DD-01] 规范与伪代码 **MUST** 将容器命名为 `DurableDict`（无 `<TValue>`），并且规范正文 **MUST NOT** 以泛型参数暗示“任意 .NET 类型可被持久化”。
- [A-DD-02] `DurableDict` 的 value 域 **MUST** 为封闭集合（closed set）：仅允许规范定义的 `ValueType`（或等价标签体系）所支持的值类型。
- [A-DD-03] 当调用方尝试写入不受支持的 value 类型时，实现 **MUST** 以确定性错误失败（例如 `UnsupportedValueType`），并且 **MUST NOT** 进行隐式序列化（如 JSON/stringify）来“勉强写入”。
- [A-DD-04] `DurableDict` 的格式 **MUST** 自描述：每个 entry 的值编码 **MUST** 可在不依赖调用方静态类型信息的前提下被解码（例如通过 `ValueType` tag）。

---

#### 建议的最小测试向量（非规范，Informative）

- `WK-Seed`: 尝试分配 `ObjectId=15`（必须失败）与 `ObjectId=16`（必须成功）。
- `CB-Seed`: base 节点 `PrevVersionPtr=0` 必须停止回溯；任意 MSB=1 的 `PrevVersionPtr` 在 MVP v2 下必须被视为损坏/不支持。
- `VER-Seed`: 构造未知 `ObjectKind` 的可达对象必须 fail-fast（不能静默跳过）。
- `DD-Seed`: 写入不支持的 value 类型必须 fail-fast（不能隐式转码）。

---

## 第三轮：最终共识确认 (Round 3: Final Consensus)

> **主持人**：SageWeaver

鉴于 Round 2 已达成高度一致，且所有关键决策均已转化为 Normative Clauses，我们直接进入最终确认。

**最终决议**：
1. **预留 ObjectId**：接纳 0-15 方案。
2. **Checkpoint**：否决 MSB Hack，确认断链语义。
3. **版本化**：接纳 ObjectKind 复用方案。
4. **去重**：接纳。
5. **去泛型**：接纳。

**执行状态**：
设计文档 `DurableHeap/docs/mvp-design-v2.md` 已根据上述决议完成修订。

---
**会议结束**
