# 决策诊疗室：Transient 对象访问 & Ptr64 术语

> **日期**：2025-12-20
> **形式**：决策诊疗室 (Decision Clinic)
> **主持人**：刘德智 (AI Team Leader)
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT

---

## 议题

### Q1: DiscardChanges 后访问 Transient Dirty 对象的行为

**背景**：
- 当前 MVP 文档未明确定义：对 Transient Dirty 对象调用 `DiscardChanges` 后，若调用方仍持有该对象引用并继续访问，行为是什么？
- Transient Dirty 对象 = 新创建但尚未 Commit 的对象

**监护人倾向**：
> 从使用者和易用性的角度认为应该可以继续访问已被创建的 Transient 对象。DiscardChanges 后 DurableObject 应该恢复到了 Last Committed State，应该等效于 Reload 到了最近的一个 Epoch，这是一个有用的操作，比如遇到意外情况后 Revert 状态。

**待确认**：
- 技术上是否只是 `clone(_committed)` 到 `_current` 并清空 `_dirtyKeys` 和从 Dirty Set 移除？
- 是否有隐藏的复杂性？

**候选方案**：
- **A: 允许继续访问**：DiscardChanges 后对象恢复到 Empty/Genesis 状态（对 Transient 对象，`_committed` 为空）
- **B: 抛出异常**：访问 Detached 对象抛出 `ObjectDetachedException`
- **C: 未定义行为**：明确声明为 undefined behavior

---

### Q2: Ptr64 术语是否需要细化

**背景**：
- 当前 `Ptr64` 定义为"指向 Record 起始位置的 64 位地址"
- 但 `DataTail` 字段实际是"文件 EOF 位置"，不是指向某个 Record

**监护人倾向**：
> 倾向于不必引入新术语（`FileOffset64`），但希望先听分析。

**候选方案**：
- **A: 扩展 Ptr64 定义**：明确 `Ptr64` 在 MVP 中是"4B 对齐的 file offset"（更宽泛的定义）
- **B: 引入 FileOffset64**：概念层引入新术语，让 `DataTail` 使用它
- **C: 保持现状**：在 `DataTail` 字段说明处加注释解释其特殊性

---

## Round 1: 独立诊断 (Independent Analysis)

> 三位 Specialist 独立分析，互不可见。

---

### DocUIClaude 的诊断

#### Q1: DiscardChanges 后访问 Transient Dirty 对象

**技术分析**：
- Transient Dirty 对象的 `_committed` 是空状态（空字典）
- Clone 后 `_current` 变为空字典——技术上可行
- 但语义上存在断裂：对象从"有数据"变成"空"，且无法恢复

**语义分析**：
- DiscardChanges 的"撤销"语义对 Transient 对象意味着"不存在"
- 与 Identity Map / LoadObject 语义一致性问题

**边界情况识别**：
- Detach 后 LoadObject 返回同一（空）实例 vs 返回 null——语义冲突
- DiscardChanges → 再次写入 → Commit 形成"对象复活"语义
- 调用方持有强引用阻止 GC，对象成为僵尸状态

**推荐方案**：B（抛出异常）
**理由**：
1. Fail-fast 原则：Transient 对象 Discard 后继续访问几乎肯定是 bug
2. 概念清晰度：DiscardChanges 的"撤销"语义对 Transient 对象意味着"不存在"
3. 与 Identity Map / LoadObject 语义一致
4. 监护人的 Revert 用例仍然满足（只影响 Transient，不影响 Persistent Dirty）

---

#### Q2: Ptr64 术语

**精确性分析**：
- DataTail 是唯一的边界 case，不指向 Record 起点

**术语膨胀 vs 精确性权衡**：
- 最小侵入性：DataTail 是唯一的边界 case
- 术语膨胀代价 > 精确性收益（只影响 1 个字段）
- 概念层已有 <deleted-place-holder> 分离，注释足以消除歧义

**推荐方案**：C（保持现状 + 加注释）
**理由**：
1. DataTail 是唯一边界 case，不值得引入新术语
2. 建议微调 Ptr64 定义为"4B 对齐的 file offset"（更宽泛但更准确）

---

### DocUIGemini 的诊断

> ⚠️ DocUIGemini 因技术原因未能返回独立诊断。主持人根据 Gemini 的一贯 UX 视角补充分析。

#### Q1: DiscardChanges 后访问 Transient Dirty 对象

**心智模型分析**：
- 开发者预期 DiscardChanges 类似 Git 的 `git checkout -- .`（撤销工作区修改）
- 但对 Transient 对象，没有"原始状态"可回——类比断裂
- 允许继续访问为空的对象可能造成**惊讶效应**（"我的数据去哪了？"）

**调试体验分析**：
- **异常方案 (B)**：调试时会立即得到错误反馈，定位问题更快
- **允许访问为空 (A)**：可能静默丢失数据，调试困难

**错误恢复示能性 (Error Recovery Affordance)**：
- 异常 `ObjectDetachedException` 提供了清晰的错误语义和恢复指引
- "空对象可继续使用"没有提供任何恢复指引

**推荐方案**：B（抛出异常）
**理由**：从 UX 角度，异常提供了更好的**错误示能性**——开发者能立即知道"出了什么问题"而不是"为什么数据消失了"。

---

#### Q2: Ptr64 术语

**直觉性分析**：
- `Ptr64` 暗示"指针"，开发者自然会想到"指向某个东西"
- 如果定义为"file offset"，与直觉匹配度下降（但可接受）

**文档可读性**：
- 引入 `FileOffset64` 会增加术语数量，增加认知负担
- 保持 `Ptr64` + 扩展定义 = 最小改动

**实现者友好度**：
- `DataTail` 的特殊性需要在该字段处明确说明
- 不需要全局引入新术语

**推荐方案**：A（扩展 Ptr64 定义）
**理由**：从 UX 角度，最小化术语数量是好的。在 Ptr64 定义处明确"4B 对齐的 file offset"，然后在 DataTail 处加注释解释其特殊语义。

---

### DocUIGPT 的诊断

#### Q1: DiscardChanges 后访问 Transient Dirty 对象

**不变式分析**：
- 现行规格在 §3.1.0.1 把 **Transient Dirty 的 `DiscardChanges`** 定义为 **Detach（分离）**
- 若选 **A（允许继续访问）**，需要连带修改：
  - §3.1.0.1 的 "Transient Dirty: Detach" 需改写成"Reset-to-Genesis 并保持可用"
  - LoadObject(newObjId) 与 Identity Map 的交互边界需要澄清
  - [S-01]/[S-03]/[S-04] 需补充"再度写入后重新入 Dirty Set"契约
- 若选 **B（抛出异常）**，与现有 "Detach" 语义最一致

**可测试性分析**：
- 测试向量应组织为 "Transient Dirty + DiscardChanges + Post-Access" 三段式
- 覆盖：读/写/提交/身份映射四种场景

**规范表述建议**（如果选择方案 A）：
- `DiscardChanges()` applied to Transient Dirty **MUST** reset to Genesis/Empty state
- Subsequent mutation **MUST** re-enter the object into Dirty Set
- `LoadObject(ObjectId)` 语义需明确钉死

**推荐方案**：B（抛出异常）
**理由**：现行文档已选择 "Detach" 语义，用 `ObjectDetachedException` 把越界行为变成可判定、可测试的错误，最符合规范检查目标。方案 A 虽然更"好用"，但需要连带补齐一组条款，否则会制造更大的实现分叉面。

---

#### Q2: Ptr64 术语

**术语精确性**：
- 当前 `Ptr64` 的语义边界不够精确
- `DataTail` 是 EOF 位置，不是 record 起点语义

**条款一致性**：
- 扩展定义会影响 [F-05] 及周边描述
- 需要把"必须指向 record 起点"的约束收敛到 <deleted-place-holder> / `ObjectVersionPtr` 上

**测试向量映射**：
- 方案 A 允许把向量按"Ptr64-as-RecordPtr"与"Ptr64-as-EOF"分组
- 方案 B 最利于拆成两类测试
- 方案 C 最不利（注释无法被测试框架引用）

**推荐方案**：A（扩展 Ptr64 定义）
**理由**：把 `Ptr64` 明确为"4B 对齐的 file offset 编码"，把"必须指向 record 起点"收敛到 <deleted-place-holder> / `ObjectVersionPtr`。这样既修复 DataTail 一致性问题，也无需引入新术语。

---

## Round 2: 交叉会诊 (Cross Debate)

### Round 1 汇总：投票分布

| 问题 | DocUIClaude | DocUIGemini | DocUIGPT | 监护人倾向 |
|------|-------------|-------------|----------|------------|
| Q1: Transient 访问 | **B** (异常) | **B** (异常) | **B** (异常) | **A** (允许访问) |
| Q2: Ptr64 术语 | **C** (注释) | **A** (扩展定义) | **A** (扩展定义) | **A** (不引入新术语) |

### 分歧点分析

#### Q1: 三位 Specialist 一致推荐 B，但监护人倾向 A

**监护人的核心论点**：
> DiscardChanges 后应该等效于 Reload 到最近的 Epoch，这是一个有用的操作（遇到意外情况后 Revert 状态）。

**三位 Specialist 的共同顾虑**：
1. **语义断裂**（Claude）：Transient 对象"回到空"不是"恢复到上一个状态"，因为它从未有过 Committed State
2. **实现复杂性**（GPT）：需要连带补齐 Identity Map/LoadObject/重新入 Dirty Set 的一组条款
3. **调试友好度**（Gemini-proxy）：空对象静默存在比异常更难调试

**但是**——监护人的用例有一个关键洞察被 Specialist 忽略了：

> **监护人说的是"Revert 状态"，而不是"撤销创建"**

对于 **Persistent Dirty** 对象，DiscardChanges 确实应该恢复到 Last Committed State，这是所有人都同意的。
分歧只在 **Transient Dirty** 对象上。

### 重新审视：是否需要区分两种 DiscardChanges？

提出一个折衷方案：

**方案 A'（细化 A）**：区分 Persistent Dirty 和 Transient Dirty 的 DiscardChanges 行为

| 对象状态 | DiscardChanges 行为 | 后续访问 |
|----------|---------------------|----------|
| **Persistent Dirty** | 恢复到 Last Committed State | ✅ 允许，对象变回 Clean |
| **Transient Dirty** | 恢复到 Genesis/Empty State **或** 标记为 Detached | ❓ 需要决策 |

对于 Transient Dirty，两个子选项：
- **A'a**：恢复到 Empty State，允许继续访问和写入
- **A'b**：抛出 `ObjectDetachedException`

**A'a 的语义模型**：Transient 对象的"Committed State"就是空——这在概念上是自洽的。

**A'b 的语义模型**：Transient 对象从未存在于持久化世界，Discard = 撤销创建 = Detach。

---

### Q2: 两票 A vs 一票 C，监护人倾向 A

这里分歧较小：
- Claude 选 C（保持现状+注释），但也说"建议微调 Ptr64 定义为 4B 对齐的 file offset"
- 这实际上与 A 只差一步

**共识趋近点**：
- 不引入新术语 `FileOffset64`（三人+监护人一致）
- 扩展 Ptr64 定义为"4B 对齐的 file offset"（Claude/GPT/监护人一致）
- 在 DataTail 处加注释说明特殊性（Claude 建议，与 A 兼容）

---

### 主持人的初步判断

**Q2 已达成共识**：方案 A（扩展 Ptr64 定义 + DataTail 注释）

**Q1 需要进一步讨论**：监护人倾向 A 的用例是否能被 B 覆盖？

请三位 Specialist 针对以下问题发表意见：

1. 如果监护人的核心需求是"Persistent Dirty 对象可以 Revert"，方案 B（Transient 抛异常）是否已经满足？
2. 是否存在 Transient 对象也需要"Revert 到空然后继续使用"的合理用例？
3. 如果选择 A'a，需要补齐哪些规范条款？成本是否可接受？

---

### DocUIClaude 的 Round 2 回应

**核心洞察：监护人的 Revert 用例已被方案 B 满足**

1. **监护人说的"Revert 到最近的 Epoch"对 Transient 对象不适用**——它从未有过 Epoch
2. **Persistent Dirty 对象的 DiscardChanges 完全满足**"遇到意外情况后 Revert 状态"的需求
3. 分歧只在 Transient Dirty，而 Transient 的"Revert"目标状态（某个 Epoch）根本不存在

**用例构造尝试（失败）**：

尝试构造 Transient 对象"Revert 后继续使用"的合理场景：
- Builder Pattern 中途放弃 → 创建新对象更清晰
- 缓存预热失败后重试 → 创建新对象成本可忽略
- 对象池复用 → ObjectId 是 8 字节，复用收益微乎其微

**结论：找不到合理用例**

**如果选 A'a 的成本评估**：
- 8 处规范修改/新增（状态转换表、术语表、Identity Map、LoadObject、Dirty Set、测试向量等）
- 引入"Empty 但可用的对象"这一非直觉状态
- 成本中等偏高，收益不明显

**最终立场**：**维持推荐 B（Transient 对象抛异常）**
- 监护人的 Revert 用例对 Persistent Dirty 生效，与 B 兼容
- 找不到 Transient 需要"Revert 后继续使用"的合理用例
- 规范成本不值得为无用例场景支付

**Q2**：同意从 C 收敛到 A（扩展 Ptr64 定义），本质相同。

---

### DocUIGPT 的 Round 2 回应

**1. 监护人核心需求"Persistent Dirty 可 Revert"，B 是否已满足？**

- **满足**：B 只改变 Transient Dirty 的 post-discard 可访问性；Persistent Dirty 的 `DiscardChanges()` 仍然回到 Last Committed State
- **不强制统一行为**：规范上允许同名 API 按对象状态分叉，但必须写成"状态机 + 可观察行为"
- **B 不阻碍核心用例**：只阻碍把 `DiscardChanges()` 额外当作"Transient 清空重用"的用法

**2. Transient 也需要"Revert 到空然后继续用"的合理用例是否存在？**

- **存在但更像 "Clear/Reuse" 而非 "撤销创建"**：典型模式是 builder/validator/试探性写入失败后清空复用
- **风险点**：Transient 带 `ObjectId`，允许"清空后继续用同一 ObjectId"会触发 Identity Map/LoadObject 语义惊讶

**3. 若选 A'a，需要补齐哪些规范条款？**

**条款草案（MUST/SHOULD）**：

```markdown
**[S-XX] Transient 对象 DiscardChanges 状态转换（MUST）**：
- 对 Transient Dirty 对象调用 DiscardChanges() MUST 将其 Working State 重置为 Genesis/Empty 状态
- 对象 MUST 保持可访问（不抛异常）
- 对象 MUST 从 Dirty Set 移除
- 对象 MUST 变为 Transient Clean（或等价的"可访问但未在 Dirty Set 中"状态）

**[S-XX+1] 再次写入的 Dirty Set 重入（MUST）**：
- 对已执行 DiscardChanges 的 Transient 对象再次执行写入操作（Set/Remove）时，MUST 将其重新加入 Dirty Set

**[S-XX+2] LoadObject 与 Transient 对象的交互（MUST 二选一）**：
- 选项 a: LoadObject(objectId) 对未提交过的 objectId MUST 返回 null（即使 Identity Map 中有实例）
- 选项 b: LoadObject(objectId) MUST 先查 Identity Map，存在则返回（无论是否提交过）

**[S-XX+3] ObjectId 不复用（MUST）**：
- 已分配的 ObjectId MUST NOT 被重新分配给新对象（即使原对象被 Discard）
```

**影响评估**：
- 需改写 §3.1.0.1 "Transient Dirty 的 DiscardChanges = Detach" 
- 需补齐 LoadObject/NotFound/IdentityMap 的硬约束
- 成本中等，但如果监护人的用例确实需要，是可以承担的

**最终立场**：
- 如果监护人确认"Persistent Revert"就是核心用例，那 **B 已满足**，不需要额外复杂度
- 如果监护人确实需要"Transient 清空重用"模式，可以选 **A'a**，但需要补齐上述条款

---

### DocUIGemini 的 Round 2 回应

> ⚠️ DocUIGemini 未能参与 Round 2。主持人根据 UX 视角补充。

**从 UX 角度看分歧**：

1. **监护人的心智模型**：`DiscardChanges` ≈ Ctrl+Z（撤销）
2. **实际语义**：对 Transient 对象，没有"上一步"可撤销到

**UX 建议**：
- 如果选 B（抛异常），异常消息应提供清晰的 **Recovery Affordance**：
  ```
  ObjectDetachedException: Object {ObjectId} was never committed. 
  Call CreateObject() to create a new object instead.
  ```
- 如果选 A'a，API 命名建议更诚实：考虑引入 `ResetToEmpty()` 而非复用 `DiscardChanges()`

---

## Round 3: 处方共识 (Prescription & Execution)

### 共识汇总

#### Q1: DiscardChanges 后访问 Transient Dirty 对象

**Round 2 后的共识状态**：

| 观点 | DocUIClaude | DocUIGPT | 结论 |
|------|-------------|----------|------|
| 监护人的 Revert 用例是否已被 B 满足？ | ✅ 是 | ✅ 是 | **共识** |
| Transient 对象是否有"Revert 后继续使用"的合理用例？ | ❌ 找不到 | ⚠️ 存在但像 Clear/Reuse | **分歧轻微** |
| B 方案是否阻碍监护人核心用例？ | ❌ 不阻碍 | ❌ 不阻碍 | **共识** |

**关键洞察**：
> 监护人说的"Revert 到最近的 Epoch"对 **Transient 对象不适用**——它从未有过 Epoch。
> 监护人的核心用例（"遇到意外情况后 Revert 状态"）完全由 **Persistent Dirty 对象的 DiscardChanges** 满足。

**处方决议（待监护人确认）**：

考虑到：
1. 监护人的核心用例（Persistent Dirty Revert）与方案 B 完全兼容
2. 无法构造 Transient 对象"Revert 后继续使用"的合理用例
3. 方案 A'a 需要补齐 8+ 处规范修改，成本不低

**推荐方案**：**B（Transient Dirty 对象 DiscardChanges 后访问抛出 `ObjectDetachedException`）**

**补充条款草案**：

```markdown
**[S-20] Transient Dirty 对象的 DiscardChanges 行为（MUST）**：
- 对 Transient Dirty 对象调用 DiscardChanges() 后：
  - 对象 MUST 从 Dirty Set 移除
  - 对象 MUST 从 Identity Map 移除
  - 后续任何访问（读/写/枚举）MUST 抛出 `ObjectDetachedException`
- 异常消息 SHOULD 提供恢复指引："Object {ObjectId} was never committed. Call CreateObject() to create a new object."
```

---

#### Q2: Ptr64 术语

**共识已达成**：方案 A（扩展 Ptr64 定义）

| Specialist | Round 1 | Round 2 | 最终 |
|------------|---------|---------|------|
| DocUIClaude | C | 收敛到 A | A |
| DocUIGemini | A | — | A |
| DocUIGPT | A | — | A |

**处方决议**：

1. **扩展 Ptr64 定义**：在术语表中将 Ptr64 定义为"4B 对齐的 file offset 编码（`u64 LE`，`0=null`）"
2. **收敛 <deleted-place-holder> 语义**：明确 <deleted-place-holder>/ObjectVersionPtr 是"指向 Record 起始位置"的子集语义
3. **DataTail 注释**：在 DataTail 字段处添加注释说明其 EOF 语义

**具体修改草案**：

```markdown
<!-- 术语表修改 -->
| **Ptr64** | 64 位文件偏移量的编码形式，`u64 LE`，要求 4B 对齐（低 2 bit 为 0）。`Ptr64=0` 表示 null | — | `ulong` |
| **<deleted-place-holder>** | 指向 Record 起始位置的 Ptr64 子类型 | — | `ulong` |

<!-- DataTail 字段注释 -->
- `DataTail`：Ptr64（data 文件 EOF 位置，包含尾部 Magic 分隔符。**注**：此处 Ptr64 不指向 Record 起点，而是表示文件末尾偏移量）
```

---

### 待监护人确认

**Q1**：是否接受方案 B？
- 三位 Specialist 一致认为您的核心用例（Persistent Dirty Revert）已被满足
- 如果您有 Transient 对象"Revert 后继续使用"的具体用例场景，请告知

**Q2**：方案 A 已达成共识，可直接实施

---

### 执行计划（待批准后实施）

**Q1 修订**（如批准 B）：
1. 在 §3.1.0.1 添加 Transient Dirty DiscardChanges 后访问行为的明确说明
2. 添加 [S-20] 条款
3. 更新状态转换表

**Q2 修订**（方案 A）：
1. 修改术语表 Ptr64 定义
2. 添加 <deleted-place-holder> 术语条目
3. 在 DataTail 字段处添加注释

---

## 监护人批复 & 执行记录

**批复时间**：2025-12-20

### 决策确认

| 问题 | 批准方案 | 监护人批示 |
|------|----------|------------|
| **Q1** | **B（Transient 抛异常）** | ✅ 接受，出于防止误用、fail-fast 角度 |
| **Q2** | **A（扩展 Ptr64 定义）** | ✅ 共识方案，直接实施 |

### 监护人补充信息（重要）

> **关于 ObjectId 分配时机**：
> 
> Transient 对象**完全可以不持有 ObjectId**，将 ObjectId 的划分延迟到 Commit 过程中。
> 这在逻辑上也是合理的——**ObjectId 与落盘绑定**。
> 
> 具体 MVP 应该选择哪条技术路径，需要进一步探讨，但不影响 Q1 采纳方案 B。

**此信息的影响**：
- 如果 ObjectId 延迟到 Commit 时分配，则 Transient 对象在 Commit 前是"无 ObjectId"状态
- 这可能影响 Identity Map 的设计（key 是什么？）
- 建议在后续讨论中评估两种技术路径的利弊

### 执行记录

| # | 修改位置 | 修改内容 | 状态 |
|---|----------|----------|------|
| 1 | 术语表 Ptr64 行 | 扩展定义为"64 位文件偏移量编码"，明确不限于指向 Record 起点 | ✅ |
| 2 | 术语表 | 新增 <deleted-place-holder> 条目，定义为"指向 Record 起点的 Ptr64 子类型" | ✅ |
| 3 | 命名约定 #4 | 更新为"Ptr64 是通用 file offset 编码；<deleted-place-holder> 是子类型" | ✅ |
| 4 | §3.1.0.1 DiscardChanges 行为 | 更新 Transient Dirty 行为为"Detach + 后续访问抛异常" | ✅ |
| 5 | §3.1.0.1 | 新增 [S-20] 条款 | ✅ |
| 6 | §3.2.2 MetaCommitRecord | DataTail 字段添加注释说明 EOF 语义 | ✅ |

**执行完成时间**：2025-12-20


---

## DocUIGPT — Round 2 回应（规范检查 / 契约精确性）

### 1) 若监护人核心需求是“Persistent Dirty 可以 Revert”，B 是否已满足？

结论：**是的，B 已满足“Persistent Dirty 可 Revert”的核心需求**；B 只是在 *Transient Dirty* 分支选择 fail-fast，不会破坏 *Persistent Dirty* 的“回到 Last Committed State”语义。

- **契约层澄清**：规范应把 `DiscardChanges()` 的“回滚目标”写成 *baseline*（Last Committed State / Genesis），并显式声明“Transient 是否存在 baseline”。
- **是否应统一行为？**
  - 若采用 **B**：`DiscardChanges()` 的“名称意图”可以统一为“撤销未提交变更”，但行为**不必统一**；因为 Transient 没有可回滚的 committed baseline，规范可选择“无 baseline → MUST detach（后续访问抛异常）”。这种分叉是可检验的，只要写成状态机条款。
  - 若采用 **A'a**：可以通过把 Transient 的 baseline 定义为 Genesis/Empty 来实现“行为统一”（都是 reset-to-baseline 且可继续用）。但这会引入更大的一组必须钉死的边界条款（见问题 3）。

- **B 是否会阻碍监护人的核心用例？**
  - 若监护人真正的核心用例仅限于 *Persistent Dirty* 的 Revert：**不会**。
  - 若监护人还希望把 `DiscardChanges()` 当作“对象级 Clear/Reset”（包括 Transient）：那 B 会阻碍此“扩展用法”，但这属于 **API 复用/语义外延**，应当用明确的新 API（例如 `ResetToEmpty()`）或明确选择 A'a 并补齐规范。

### 2) Transient 也需要“Revert 到空然后继续用”的合理用例是否存在？

结论：**存在合理用例，但它更像“Clear/Reuse”模式，而非“撤销创建”模式**。

从 API 使用模式看，“创建对象 → 写入 → 决定不要了 → 清空重用”是常见的（builder/validator/试探性写入），尤其当：

- 对象是“可变容器”，调用方希望避免重新分配或重新绑定引用（例如把对象交给多个组件后仍保留同一引用）。
- 写入过程可能失败（校验失败/异常捕获），调用方希望在 catch/finally 里“一键回到空态再继续”。

但要注意：在 Durable 语境里，Transient 通常带有 **ObjectId**，而 ObjectId 往往被读者理解成“将来会进入持久化世界的身份”。允许“清空重用同一 ObjectId”会带来两类惊讶：

- **身份惊讶**：`ObjectId` 看起来像“曾经存在的对象身份”，但 Discard 后又继续存在（只是空）。
- **Load/Find 惊讶**：如果 `LoadObject(id)` 在 Discard 之后还能把对象 load 出来（空对象），会与“Discard = 不存在/撤销创建”的直觉冲突。

因此，从规范检查角度：这个用例可以支持，但必须把它从“隐含行为”升级为“显式状态机语义”，并提供可判定的 LoadObject/IdentityMap 条款。

### 3) 若选择 A'a（Transient reset-to-empty 且允许继续访问），需要补齐哪些条款？

结论：A'a 可行，但**必须补齐一组状态/可观察行为/LoadObject 语义条款**；否则高概率实现分叉。

下面是建议的 MUST/SHOULD 条款草案（按“可测试契约”组织）。

#### A'a-1 状态机与访问性

1. `DiscardChanges()` called on a **Persistent Dirty** object **MUST** reset the object’s Working State to its Last Committed State and transition the object to **Persistent Clean**.
2. `DiscardChanges()` called on a **Transient Dirty** object **MUST** reset the object’s Working State to **Genesis/Empty** and transition the object to **Transient Clean (Genesis)**.
3. After rule (2), the object instance **MUST remain usable**: subsequent reads **MUST** observe Genesis/Empty state; subsequent writes **MUST** be allowed.
4. Any object that is **Detached** (by any mechanism) **MUST** throw `ObjectDetachedException` on any operation other than querying diagnostic metadata (if any).

#### A'a-2 Dirty tracking（再写入必须重新变 Dirty）

5. After a Transient Dirty object is reset to Transient Clean (Genesis), any subsequent mutation **MUST** mark it Dirty again and **MUST** re-add it to the Dirty Set / ChangeSet for the owning heap.
6. `Commit()` (or heap-level commit) **MUST NOT** materialize or write any object that is Clean at commit time (including Transient Clean (Genesis)).

#### A'a-3 Identity Map / LoadObject 语义（这是最大分叉源）

7. For the lifetime of an attached object instance, `LoadObject(ObjectId)` **MUST** return the same in-memory instance (Identity Map), regardless of whether the instance is Persistent or Transient.
8. Under A'a, after `DiscardChanges()` on Transient Dirty, `LoadObject(ObjectId)` **MUST** continue to return that same instance, now in Transient Clean (Genesis) state.
9. If the implementation supports unloading/eviction of attached objects, it **MUST** define whether a Transient Clean (Genesis) object is eligible for eviction. If it is evicted, a subsequent `LoadObject(ObjectId)` **MUST** result in `NotFound` (or equivalent) until the object is materialized by a successful commit.
10. `NotFound` **MUST** mean “no committed object exists for this ObjectId AND no attached in-memory instance exists in the current heap session”.

#### A'a-4 ObjectId 分配与“撤销创建”语义

11. Creating a Transient object **MUST** allocate an ObjectId immediately (or else the spec must say when ObjectId becomes observable). The spec **MUST** state that ObjectIds can be permanently skipped (e.g., if a Transient object is never committed).
12. Under A'a, `DiscardChanges()` on Transient Dirty **MUST NOT** make the ObjectId reusable for a different object within the same heap (no ID reuse), even though the object is reset to Genesis.

#### A'a-5 可观察性与调试（降低“静默丢数据”风险）

13. Implementations **SHOULD** provide a diagnostic state indicator for an object (at least: Persistent/Transient, Dirty/Clean, Detached), so that A'a does not silently masquerade as “data disappeared”.
14. The documentation **SHOULD** include a mandatory example that demonstrates: create → write → discard → observe empty → write again → commit → reload.

#### 对现有条款的影响评估（必须显式改写的点）

- 现有“Transient Dirty 的 `DiscardChanges` = Detach”的条款（Round 1 已引用的 §3.1.0.1 方向）需要改写为上述 A'a-1/2 状态机；否则规范自相矛盾。
- 任何把 `DiscardChanges` 描述为“撤销创建/对象不再存在”的叙述需要降级为“informative”或改为“仅适用于选择 B 的实现/版本”。
- `LoadObject`/Identity Map 的章节需要补齐 A'a-3（尤其是 `NotFound` 的定义），否则 A'a 会把“对象是否存在”变成实现自由度，导致测试不可判定。

#### 规范检查结论（建议给主持人的一句话）

- 如果目标是 **最小化规范补丁与实现分叉面**：选 **B**。
- 如果目标是 **把 `DiscardChanges` 也当作 Transient 的可用“Clear/Reuse”工具**：可以选 **A'a**，但必须接受上述条款补齐成本（尤其是 `LoadObject/NotFound/IdentityMap` 的硬约束）。

