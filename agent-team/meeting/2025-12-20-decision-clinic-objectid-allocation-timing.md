# 决策诊疗室：ObjectId 分配时机

> **会议类型**：Decision Clinic（决策诊疗室）
> **日期**：2025-12-20
> **主持人**：刘德智 / SageWeaver（DocUI 规范起草委员会执行委员会主席）
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
> **议题**：ObjectId 应该在 Transient Object 创建时立即分配，还是延迟到 Commit 时分配？

---

## 议题背景

### 当前规范状态

根据 `mvp-design-v2.md` §3.1.1：

```markdown
ObjectId 分配（第二批决策补充）：
- ObjectId 采用**单调递增计数器**分配，避免碰撞处理。
- 为保证崩溃恢复与跨进程一致性，需要将 `NextObjectId`（下一个可分配 id）持久化。
  - 依据 Q18：MVP 只把 `NextObjectId` 写在 meta commit record 中。
```

当前规范**隐含假设** ObjectId 在 `CreateObject<T>()` 时立即分配，体现在：
- §3.1.0.1 状态转换表：`CreateObject<T>()` 新建对象后加入 Identity Map（需要 ObjectId 作为 key）
- [S-02] Identity Map 与 Dirty Set 的 key **必须等于对象自身 ObjectId**

### 监护人提出的替代方案

> Transient 对象**完全可以不持有 ObjectId**，将 ObjectId 的划分延迟到 Commit 过程中。
> 这在逻辑上也是合理的——**ObjectId 与落盘绑定**。

### 待决问题

**Q1: ObjectId 分配时机**

| 方案 | 描述 | 关键特征 |
|------|------|----------|
| **A: 立即分配** | `CreateObject<T>()` 时立即分配 ObjectId | 当前隐含假设 |
| **B: 延迟分配** | Commit 时才分配 ObjectId | ObjectId 与落盘绑定 |

---

## Round 1: 独立诊断 (Independent Diagnosis)

### DocUIClaude 的诊断

**概念分析洞察**：
1. **身份的本质**：ObjectId 定义为"对象的稳定身份"。身份应在存在时产生（方案 A），还是被承认时产生（方案 B）？
2. **类比支持**：数据库 AUTO_INCREMENT、UUID、DDD Entity 都采用"创建时分配"；Git commit hash 是例外但语义不同（内容寻址）
3. **引用透明性**：方案 A 让引用立即可用；方案 B 需要"临时引用 + Commit 时替换"的双轨机制

**方案 A（立即分配）优势**：
- 概念简洁：1 种引用形态（ObjectId）
- 规范兼容：与 [S-02] Identity Map 约束完全对齐
- Transient 间引用自然支持，无需特殊机制
- ObjectId 浪费是纯美学问题（uint64 空间足够用 5 亿年）

**方案 B（延迟分配）问题**：
- 引入"引用的两种形态"（临时 + 正式），增加概念复杂度
- 需要修改 [S-02]、§3.1.0.1 状态转换等规范
- Commit 时需要引用图遍历 + 两遍扫描，复杂度 O(n)
- 循环引用需要特殊处理

**诊断结论**：推荐方案 A（立即分配）
- 语义更干净（一种引用形态 vs 两种）
- 与现有规范完全兼容（无需修改）
- 降低 Commit 路径实现复杂度

---

### DocUIGemini 的诊断

#### 开发者心智模型
**"出生证明" vs "户口登记"**

- **方案 A (立即分配)** 符合 **"出生证明"** 模型：对象一旦被 `new` 出来，就拥有了在这个世界（进程）中的唯一身份。这符合 C# 引用类型 (`Reference Equality`) 的直觉——对象生而不同。
- **方案 B (延迟分配)** 符合 **"户口登记"** 模型：对象只是"黑户"，直到去派出所（Commit）登记后才获得身份证号。
- **ORM 对比**：虽然 EF Core/Hibernate 等 ORM 常采用延迟分配（因为依赖 DB 自增），但这通常被视为 **"Leaky Abstraction" (抽象泄漏)**，是阻碍开发者将 DB 对象视为普通内存对象的最大认知摩擦之一。
- **DurableHeap 定位**：我们构建的是 **Persistent Memory (持久化内存)**，而不是 SQL Wrapper。在内存编程模型中，Identity 是内在属性 (Intrinsic Property)，不应依赖于外部持久化动作。

#### API 可用性
**"外键悖论" (The Foreign Key Paradox)**

如果采用方案 B，会严重破坏 **"创建 -> 组装 -> 提交"** 的自然流：

```csharp
// 场景：创建树结构 (Parent -> Child)
var parent = heap.CreateObject<Node>();
var child = heap.CreateObject<Node>();

// 方案 A：自然流畅
child.ParentId = parent.Id; // parent.Id 已存在
heap.CommitAll();

// 方案 B：死锁困境
child.ParentId = parent.Id; // ❌ parent.Id 是什么？0？null？Throw？
// 开发者被迫写成：
heap.CommitAll(); // 先提交 parent 拿 ID
child.ParentId = parent.Id; // 再关联
heap.CommitAll(); // 再提交 child
```

除非引入复杂的 "Object Proxy" 或 "Deferred Reference" 机制，否则方案 B 会迫使开发者为了获取 ID 而频繁 Commit，导致 **"过早提交" (Premature Commit)**，破坏事务原子性。

#### 错误示能性 (Error Affordance)
**"静默的主键污染" (The Silent Key Pollution)**

如果采用方案 B，且 `Id` 在 Commit 前返回默认值（如 0）：

1. 开发者创建一个 Transient 对象 `obj` (Id=0)
2. 开发者将其放入一个 `Dictionary<ulong, Metadata>`，Key 为 `0`
3. 执行 `Commit()`。`obj.Id` 突变为 `100`
4. **灾难发生**：字典里的 Key 仍然是 `0`，但对象 ID 变成了 `100`。索引失效，且没有任何报错

这是典型的 **"False Affordance" (虚假示能)**：API 允许你读取 `Id`，但这个 `Id` 是不稳定的。方案 A 通过保证 ID 的 **Immutable Identity**，彻底消除此类错误类别。

#### 可预测性
**LLM 作为用户的视角**

作为 DocUI 的顾问，必须指出 **LLM 对"符号稳定性"的依赖**。

如果 LLM 生成代码：
1. Create Object X (ID: 0)
2. Reasoning: "I will link Y to X(0)"
3. Action: Commit
4. Observation: X is now ID 100
5. **Hallucination Risk**: LLM 的上下文窗口中同时存在 "X is 0" 和 "X is 100" 的事实，极易导致推理混乱

方案 A 提供 **Stable Symbol Grounding (稳定的符号落地)**：X 从出生起就是 100，无论是否 Commit。这对 Agent 的推理链更加友好。

#### 推荐方案：A (立即分配)

**理由**：
1. **DX 优先**：支持 "Create-Link-Commit" 的自然事务流，避免为了拿 ID 而打断事务
2. **消除状态陷阱**：避免 "Transient ID" (0) 变为 "Persistent ID" (100) 导致的数据结构腐烂
3. **心智模型一致性**：符合 "Persistent Memory" 隐喻，对象即数据，身份即指针
4. **Agent 友好**：稳定的 ID 降低了 LLM 理解代码执行副作用的认知负荷

**关于 ID 浪费的注脚**：
在 64 位空间下，ID 浪费（Crash 导致跳号）是纯粹的**美学问题**，而非工程问题。为了美学牺牲 DX 是不值得的。

---

### DocUIGPT 的独立诊断（规范审计 / 契约精确性 / 可测试性）

#### 0. 我所依据的“现有规范事实”（SSOT 摘要）

- 现有规范已经把“新建对象”纳入对象状态机，并显式写成：`CreateObject<T>()` 产生 **Transient Dirty**，立即加入 Identity Map（WeakRef）与 Dirty Set（强引用）。见 [DurableHeap/docs/mvp-design-v2.md](DurableHeap/docs/mvp-design-v2.md#L242-L288)。
- **[S-02]** 明确要求：Identity Map 与 Dirty Set 的 key 必须等于对象自身 `ObjectId`。见 [DurableHeap/docs/mvp-design-v2.md](DurableHeap/docs/mvp-design-v2.md#L254-L256)。
- `NextObjectId` 仅在 `MetaCommitRecord` 持久化，Open 时用 HEAD 的 `NextObjectId` 初始化 allocator。见 [DurableHeap/docs/mvp-design-v2.md](DurableHeap/docs/mvp-design-v2.md#L296-L333) 与 [DurableHeap/docs/mvp-design-v2.md](DurableHeap/docs/mvp-design-v2.md#L623-L633)。

这些事实使得“立即分配（A）”是当前文档的默认语义；“延迟分配（B）”不是小改动，而是会穿透对象身份、引用表示、以及 Dirty/Identity 的 key 机制。

---

#### 方案 A：`CreateObject` 立即分配 ObjectId

##### A1. 规范影响（对现有 [S-02] 等条款兼容性）

- 与现有状态机与 **[S-02]** 完全一致：Transient Dirty 也拥有 `ObjectId`，因此 Identity Map / Dirty Set 可以继续用 `ObjectId` 做 key。
- 需要补齐的不是“结构”，而是“契约边界”：ObjectId 的稳定性应被描述为“对已 Commit 对象稳定”，对“未 Commit 对象”仅保证进程内可引用即可。

##### A2. `NextObjectId` 语义：分配但未 Commit 后 Crash

现状是：`NextObjectId` 只在 commit point 之后才持久化。

- 因此在 A 中，**允许**出现：进程内已经分配过一批 ObjectId，但在 commit 前崩溃，重启后 `NextObjectId` 回退到上一次 commit 的值，从而“重用”那批未提交的 ObjectId。
- 这并不违反“稳定身份”的核心语义，因为这些被重用的 ObjectId 对应的对象从未进入 VersionIndex / HEAD，也就从未对外（跨进程）存在。

需要新增/澄清的规范点是：

- **ObjectId 单调性边界**：对“已提交历史”单调递增即可；对“进程内未提交分配”不保证跨崩溃单调。
- **唯一性边界**：必须保证“任意时刻同一 workspace 内，不会出现两个存活对象共享同一 ObjectId”（进程内唯一）；跨崩溃只需保证不会与“已提交对象”冲突。

##### A3. 测试向量设计（如何验证分配行为）

可判定、可自动化的测试向量（不依赖实现细节）：

1) **进程内递增**：连续 `CreateObject` 产生递增 id，且 `IdentityMap[Id]` 命中同一实例。
2) **未提交崩溃回退**：
   - Given：空仓库 Open，记下 `next0`。
   - When：CreateObject N 次但不 Commit，关闭进程；再 Open。
   - Then：`NextObjectId == next0`，且新创建对象复用 `next0`。
3) **提交后持久化**：
   - Given：CreateObject N 次并 Commit。
   - Then：重启后 `NextObjectId == (maxAllocatedId + 1)`。

这些测试天然与条款 **[R-02]/[R-03]** 的“commit point 与恢复”逻辑一致，容易做 failpoint/crash-injection。

##### A4. 规范复杂度（需要多少新条款）

只需要少量“语义澄清”条款，重点围绕 allocator 与 crash 边界：

- 新增 1 条 `[S-xx]`：ObjectId 的“跨崩溃单调性”不是要求；仅保证“已提交对象集合”与“进程内存活对象集合”的唯一性/不冲突。
- 新增 1 条 `[A-xx]` 或 `[S-xx]`：`CreateObject` 必须立即分配 ObjectId，且 `CommitAll` 必须把 `NextObjectId` 写入 `MetaCommitRecord`（已经叙述，但可升级为 MUST + 编号以便测试映射）。
- （可选）新增 1 条 `[R-xx]`：重启后 allocator 初始化严格来自 HEAD 的 `NextObjectId`，不得扫描 data 来“推断更大 id”（否则会破坏可测性与确定性）。

---

#### 方案 B：Commit 时延迟分配 ObjectId

##### B1. 规范影响（需要修改/新增哪些条款）

B 的核心矛盾：现有规范把 ObjectId 当作“对象在 workspace 内的身份锚点”，而延迟分配意味着 Transient 对象在 commit 前 **没有** 这个锚点。

因此必须至少做出以下一组契约改写（不是局部补丁）：

- 改写 **[S-02]**：Identity Map / Dirty Set 的 key 不能再“必须等于 ObjectId”，因为 transient 没有 ObjectId；必须引入新的 key（例如 `TransientKey` / `CreationSeq` / 直接以对象实例引用为 key）。
- 改写新建对象状态机（`CreateObject<T>()` 那一行）：不能再说“加入 Identity Map（需要 ObjectId 作为 key）”。
- 改写引用表示：当前 Working State/Committed State 的对象引用统一通过 `ObjectId` 表示（并且 Dict value 支持 `Val_ObjRef(ObjectId)`）。若 transient 没有 ObjectId，则在 commit 前“把 transient 塞进容器/字段”的行为必须定义：
  - 要么禁止（MUST throw），
  - 要么允许，但需要一个“临时引用表示”（会引入新的 ValueType 或新的内存态表示），commit 时再统一回填为 ObjectId。

##### B2. `NextObjectId` 语义：更简单还是更复杂？

表面上更简单：只有 commit 分配 id，`NextObjectId` 的推进与持久化天然一致。

但规范上会更复杂，因为你必须额外定义：

- **分配顺序的确定性**：commit 时同时给多个 transient 分配 id，若顺序不固定（例如依赖字典迭代顺序），测试将变得不可判定。必须规定“按 CreationSeq 升序”或类似的稳定顺序（这又要求在对象上引入 `CreationSeq`）。
- **commit 内引用闭包**：如果对象 A 引用 transient B，而 B 也在本次 commit 中被分配 id，则必须规定先为所有 transient 分配 id，再序列化任何包含 ObjRef 的 DiffPayload，否则 writer 无法编码 `Val_ObjRef(ObjectId)`。

所以：`NextObjectId` 本身更简单，但“为了让它可测试/可实现一致”，需要额外的排序/两段式分配条款，整体反而更重。

##### B3. 测试向量设计（Transient 的测试如何组织）

B 的测试会显著更难写成“条款可判定”的向量，原因是：你不得不测试“commit 内的批量分配顺序”。

最低限度你需要：

1) **commit 分配**：`CreateObject` 后 `obj.Id` 不可用（null/throw/0），commit 后变为非保留区 id。
2) **稳定排序**：在同一 commit 里创建 N 个对象，commit 后它们的 id 分配顺序必须与规范指定的顺序一致（例如 creation order）。
3) **容器引用闭包**：把 transient 塞入 dict/value，commit 后序列化出来的 `Val_ObjRef` 必须引用新分配的 id。

这些测试要求在 API 上暴露“未分配 id 的对象”的可观察状态（否则无法断言），进一步增加 API 侵入。

##### B4. 规范复杂度（需要多少新条款）

需要新增/改写的条款数量会明显更多，且涉及跨章节的连锁一致性：

- 修改 **[S-02]** 与对象生命周期表（至少 2-3 处）。
- 新增 2-4 条 `[S-xx]`：transient key 机制、commit 内的 id 分配顺序、commit 内的“两阶段：先分配 id 再写 diff”的约束。
- 新增 1-2 条 `[A-xx]`：`obj.Id` 在 transient 阶段的行为（null/throw）与错误模型。
- 可能需要新增一个“内存态引用表示”术语与其与 `Val_ObjRef` 的映射规则（否则引用语义不闭合）。

---

#### 回答题目

##### 1) 哪个方案对现有规范的侵入性更小？

- 结论：**方案 A（立即分配）侵入性更小**。
- 原因：现有 **[S-02]**、状态机、以及“对象引用以 ObjectId 表示”的整体设计已经把 ObjectId 当作 workspace 内身份锚点；B 会迫使引入第二套身份（TransientKey）并改写多处契约。

##### 2) 哪个方案更可测试？

- 结论：**方案 A 更可测试**。
- 原因：A 的“崩溃回退/重用”行为可以被写成完全确定的 black-box 测试；B 为了可测试必须规定 commit 内排序，并暴露 transient id 状态，导致测试与实现细节强耦合。

##### 3) 各方案需要的规范变更（条款级别）

**方案 A（最小变更集）**

- 新增 `[S-xx]`：ObjectId 的跨崩溃单调性不保证；仅保证“已提交对象不冲突 + 进程内存活对象唯一”。（建议放在 §3.1.1 ObjectId 分配段落附近）
- 新增/升级为编号条款（`[S-xx]` 或 `[A-xx]`）：`CreateObject<T>()` MUST 立即分配 ObjectId，并将对象加入 Identity Map 与 Dirty Set（把目前叙述性表格语义提升成可测试条款）。
- 新增 `[R-xx]`：allocator 初始化 MUST 仅来自 HEAD 的 `NextObjectId`；未提交分配不应在重启后被“推断并保留”。

**方案 B（必要变更集，非详尽但最少项）**

- 修改 **[S-02]**：改为 “Identity Map / Dirty Set 的 key MUST 为（ObjectId 或 TransientKey）”，并定义 TransientKey。
- 修改 §3.1.0.1 新建对象状态机：CreateObject 不再“加入 Identity Map keyed by ObjectId”，而是 keyed by TransientKey。
- 新增 `[S-xx]`：commit 内 id 分配顺序 MUST 确定（例如 CreationSeq 升序），并规定 CreationSeq 的来源与可观察性。
- 新增 `[S-xx]`：commit 流程 MUST 先为所有待提交 transient 对象分配 ObjectId，再允许序列化任何 `Val_ObjRef`。
- 新增/修改 `[A-xx]`：transient 阶段访问 `obj.Id` 的行为（throw/null）以及错误类型。

##### 4) 推荐方案（A/B）及理由

- 推荐：**方案 A（CreateObject 立即分配）**。
- 理由（从规范审计/契约精确性/可测试性）：
  - 它与现有 **[S-02]**、Dirty/Identity 状态机、以及“引用=ObjectId”的整体设计一致，属于“补齐契约边界”而不是“重写身份模型”。
  - `NextObjectId` 只在 commit 持久化的设计本身并不矛盾，只需要明确：未提交分配在崩溃后允许被重用。
  - 测试向量可以直接覆盖“进程内分配/commit 持久化/崩溃回退”，且无需引入新的可观察状态或排序条款。

---

#### 额外发现（规范-测试一致性风险，供诊疗室参考）

- [DurableHeap/docs/mvp-design-v2.md](DurableHeap/docs/mvp-design-v2.md#L638-L639) 规定空仓库 `NextObjectId = 16`（保留 `0..15`）。
- [DurableHeap/docs/mvp-test-vectors.md](DurableHeap/docs/mvp-test-vectors.md#L105-L112) 的 FIRST-COMMIT-001 断言 `NextObjectId == 1`。

这会导致测试向量与规范 SSOT 直接冲突；无论 A/B，建议先把这处对齐（否则会干扰对 allocator 行为的讨论与回归测试）。

---

## Round 2: 交叉会诊 (Cross Debate)

### Round 1 汇总：投票分布

| Specialist | 推荐方案 | 核心论据 |
|------------|----------|----------|
| **DocUIClaude** | **A** (立即分配) | 概念简洁（1 种引用形态）、与现有规范兼容、降低 Commit 路径复杂度 |
| **DocUIGemini** | **A** (立即分配) | DX 优先、避免状态陷阱、符合 Persistent Memory 隐喻、Agent 友好 |
| **DocUIGPT** | **A** (立即分配) | 规范侵入性最小、可测试性更强、只需补齐边界条款 |

### 共识已达成 ✅

三位 Specialist **一致推荐方案 A（CreateObject 时立即分配 ObjectId）**。

**共识要点**：

| 维度 | 方案 A 优势 | 方案 B 劣势 |
|------|-------------|-------------|
| **概念复杂度** | 1 种引用形态 | 需要双轨机制（临时引用 + 正式引用） |
| **规范兼容性** | 与 [S-02] 完全对齐 | 需要修改 [S-02]、状态机、引入 TransientKey |
| **DX** | 自然的 Create-Link-Commit 流程 | 破坏事务原子性，导致过早提交 |
| **可测试性** | Black-box 测试即可 | 需要测试 commit 内排序，与实现耦合 |
| **Agent 友好度** | 稳定符号落地 | ID 突变导致推理混乱 |

### 需要补齐的规范条款（GPT 整理）

**方案 A 的最小变更集**：

1. **`[S-21]` ObjectId 单调性边界（建议新增）**
   - ObjectId 对"已提交对象集合"MUST 单调递增
   - 对"进程内未提交分配"不保证跨崩溃单调性
   - 允许未提交的 ObjectId 在崩溃后被重用

2. **`[S-22]` CreateObject 分配行为（建议新增）**
   - `CreateObject<T>()` MUST 立即分配 ObjectId
   - 分配后 MUST 将对象加入 Identity Map 与 Dirty Set

3. **`[R-04]` Allocator 初始化（建议新增）**
   - 重启后 allocator MUST 仅从 HEAD 的 `NextObjectId` 初始化
   - MUST NOT 通过扫描 data 文件推断更大 ID

### GPT 额外发现：测试向量与规范冲突

> [mvp-design-v2.md] 规定空仓库 `NextObjectId = 16`（保留 `0..15`）
> [mvp-test-vectors.md] 的 FIRST-COMMIT-001 断言 `NextObjectId == 1`

**需要对齐**：建议将测试向量更新为 `NextObjectId == 16`。

---

## Round 3: 处方共识 (Prescription & Execution)

### 决议

**采纳方案 A：CreateObject 时立即分配 ObjectId**

**理由汇总**：
1. 三位 Specialist 一致推荐
2. 与现有规范设计完全兼容
3. 避免引入"临时引用"的概念复杂度
4. 支持自然的 Create-Link-Commit 事务流程
5. 对 LLM Agent 的符号推理更友好

### 待实施的规范修订

| # | 条款 | 类型 | 位置 | 内容摘要 |
|---|------|------|------|----------|
| 1 | [S-21] | 新增 | §3.1.1 | ObjectId 单调性边界（允许崩溃后重用未提交 ID） |
| 2 | [S-22] | 新增 | §3.1.1 | CreateObject MUST 立即分配 ObjectId |
| 3 | [R-04] | 新增 | §6.2 | Allocator 初始化来源限定 |
| 4 | 测试向量 | 修复 | mvp-test-vectors.md | FIRST-COMMIT-001 的 NextObjectId 从 1 改为 16 |

---

### 待监护人确认

**是否批准**：
- [x] 采纳方案 A（立即分配）
- [x] 实施上述 4 项规范修订

---

## 监护人批复 & 执行记录

**批复时间**：2025-12-20

### 决策确认

| 问题 | 批准方案 | 监护人批示 |
|------|----------|------------|
| **ObjectId 分配时机** | **A（立即分配）** | ✅ 赞同分析和共识 |

### 执行记录

| # | 条款 | 位置 | 状态 |
|---|------|------|------|
| 1 | **[S-21]** ObjectId 单调性边界 | §3.1.1 ObjectId 分配段落 | ✅ |
| 2 | **[S-22]** CreateObject MUST 立即分配 | §3.1.1 ObjectId 分配段落 | ✅ |
| 3 | **[R-04]** Allocator 初始化来源限定 | §3.5 崩溃恢复 | ✅ |
| 4 | 测试向量 NextObjectId 修复 | mvp-test-vectors.md FIRST-COMMIT-001 | ✅ |

**当前条款编号状态**：
- S-01..04, S-06..22（S-05 预留 gap）
- R-01..04

**执行完成时间**：2025-12-20 