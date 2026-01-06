# 秘密基地畅谈：DurableHeap MVP v2 最终审阅

> **日期**：2025-12-20
> **目标**：审阅 `DurableHeap/docs/mvp-design-v2.md`，确保文档自洽、一致、低冗余和重复
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
> **主持人**：刘德智 / SageWeaver（AI Team Leader）

---

## 会议规则

这是一次秘密基地畅谈，不是正式研讨会。

**规则**：
- 不需要编号、不需要投票、不需要结论
- 随便聊，画草图，提疯狂的想法
- 看到别人写的东西后，可以接着写、可以画、可以说"这让我想到..."
- 可以用 ASCII art、可以用伪代码、可以用比喻
- "如果能这样就好了"是合法的发言

**核心准则**：
- **拒绝低水平抬杠**：不要纠结细枝末节，不要为了反对而反对。
- **鼓励"死党式"直言**：
  - 发现盲区、隐性假设或边界条件时，必须指出来。
  - 发现明确错误时，直接说"嘿，这里不对"。
  - 提反对意见时，请提供反例或更好的替代方案。
- **保持流动性**：指出问题是为了让想法更强壮，而不是为了杀死它。

---

## 审阅焦点

请各位关注以下方面：

1. **自洽性（Self-consistency）**：文档内部是否有矛盾？术语定义与使用是否一致？
2. **一致性（Consistency）**：不同章节的描述是否对齐？例如术语表与正文的描述是否吻合？
3. **低冗余（Low Redundancy）**：是否有重复的内容可以合并或消除？
4. **完整性（Completeness）**：是否有缺失的定义或模糊的边界条件？

---

## 畅谈记录

### 主持人开场

各位好！这是今天第 N 次审阅这份文档了（笑）。

这份 MVP 设计草稿经过多轮迭代，已经相当成熟。但正因为多轮修订，可能存在一些"遗迹"——早期版本的措辞、重复的定义、或者不再一致的描述。

今天的目标很明确：**找出文档中的不一致、冗余和模糊地带**。

由于这是设计草稿（无实现、无数据），我们可以放心地修改而不用担心兼容性。

请开始自由发言！

---

### DocUIClaude（第一轮）

好的，我来扫一遍术语一致性和概念自洽性。这份文档已经很成熟了，但多轮修订后确实有一些"地层断裂"的痕迹。

#### 一、术语表与正文的对照检查

**1. 发现 `Dirty Set` 术语冗余/混用**

术语表中：
```
| **Dirty Set** | ... | Deprecated: Modified Object Set（使用 Dirty Set 作为主术语） |
```

但同时术语表还保留了"Modified Object Set"的描述（虽然标记为 Deprecated）。问题是：

- §3.1.0 正文中使用 `Dirty Set`（正确）
- 但术语表的弃用映射 `Deprecated: Modified Object Set` 容易让读者困惑——到底是哪个？

**建议**：删除 "Modified Object Set" 的弃用映射，或在术语表中只保留一行定义。当前的写法让人怀疑是不是有两个不同的集合。

---

**2. `Base Version` 术语层次使用不一致**

术语表定义了漂亮的三层结构：
```
Base Version（上位词） → Genesis Base / Checkpoint Base
```

但正文 §3.2.5 仍然出现：
> "若为 0 表示 **Base Version**（Genesis Base 或 Checkpoint Base）"

这句话语法上没问题，但"或"字暴露了一个问题——读者无法从 `PrevVersionPtr=0` 这一个值判断是哪种 Base。文档应该明确：

- **从 wire format 层面无法区分** Genesis Base 和 Checkpoint Base——两者都是 `PrevVersionPtr=0`
- 区分依据是**语义层**的上下文（是否是该 ObjectId 的首个版本）

这个"语义层区分 vs 编码层不可区分"的微妙之处值得在正文中点明，否则实现者可能会试图从 wire format 推断。

---

**3. `Shallow Materialization` 概念存在但未充分利用**

术语表定义了 `Shallow Materialization`：
> "Materialize 只构建当前对象的 Committed State，引用只保留 ObjectId，不递归创建/加载引用对象。"

这是个很好的概念澄清。但正文 §3.1.0 阶段 2 的描述是：

> "**Materialize is shallow**（MVP 固定，参见术语表 **Shallow Materialization**）"

这里用了"is shallow"而非正式术语。建议统一为：

> "本阶段执行 **Shallow Materialization**（MVP 固定）"

---

#### 二、概念自洽性检查

**4. 状态转换表的完备性缝隙**

§3.1.0.1 的状态转换表很清晰，但有一个隐藏的边界条件：

| 事件 | 当前描述 |
|------|----------|
| `LoadObject` 对象不存在 | 表中未覆盖 |
| `Commit` 失败 | 脚注说"不引起状态转换" |

但 `Commit` 失败后：
- 对象状态不变（正确）
- 磁盘上可能已经写入了 partial data records（正确描述在 §3.4.5 失败语义表）
- **但状态转换表没有说明这一点**

建议在脚注中补充：
> "Commit 失败不改变对象状态。磁盘上可能存在 partial data records，但 Commit Point 未确立，重启后这些 records 会被 DataTail 截断。"

---

**5. `DiscardChanges` 对 Transient Dirty 对象的行为存在"概念裂缝"**

§3.1.0.1 定义了 Transient Dirty → Detached 的转换，并有很好的警告框。

但有个微妙问题：Detached 对象的 `ObjectId` 可能被重用（[S-21] 允许未提交 ObjectId 在崩溃后重用）。那么：

- 如果用户在 **同一进程内**（无崩溃）对 Transient 对象 DiscardChanges，该 ObjectId 会被放回池中吗？
- 还是只有崩溃重启后才会重用？

从 [S-21] 的表述看，似乎是"崩溃后重用"，但 DiscardChanges 是用户显式操作。

建议在 [S-20] 或 [S-21] 中明确：
> "DiscardChanges 不会回收 ObjectId；已分配的 ObjectId 只有在进程崩溃重启后才可能被重新分配。"

---

#### 三、规范条款编号的"跳号"检查

**6. [S-05] 已标记 Deprecated，但术语表中未出现弃用原因**

文档顶部的"已废弃条款"写道：
> "[S-05] Deprecated — 原语义已合并到 [S-06]"

这很好，但 [S-06] 的定义是：
> "**Working State 纯净性**：在任何对外可读/可枚举的状态视图中，tombstone 不得作为值出现"

那 [S-05] 原来是什么？合并到 [S-06] 意味着什么？

**问题**：未来维护者看到 [S-05] 被跳过，但不知道它原来说的是什么。

**建议**：在废弃条款后面加一句原始定义摘要：
> "[S-05] Deprecated — 原语义（Working State 不含 tombstone）已合并到 [S-06]"

---

#### 四、冗余检测

**7. VersionIndex 的 Bootstrap 描述出现两次**

§3.2.4 开头有一个 Bootstrap 入口说明：
> "VersionIndex 是整个对象图的**引导扇区（Boot Sector）**。它的指针（VersionIndexPtr）直接存储在 MetaCommitRecord 中..."

但 §3.3.2 (LoadObject) 的步骤 2 又提到：
> "从 HEAD commit 对应的 VersionIndex 解析该 ObjectId..."

这两处都在解释"如何找到 VersionIndex"，可以考虑在 §3.3.2 用一个交叉引用：
> "从 HEAD commit 对应的 VersionIndex（参见 §3.2.4 Bootstrap 入口）解析..."

---

**8. 二阶段提交的 ASCII 图与表格冗余**

§3.4.4 有一个漂亮的 ASCII 流程图，但 §3.4.5 的"失败语义表"也描述了 Prepare/Finalize 阶段。两者信息重叠。

不是说要删除，而是建议在表格前加一句：
> "以下表格对应 §3.4.4 二阶段提交流程图的各阶段失败场景："

---

#### 五、一个"疯狂想法"

**9. 术语表是否应该有"层级标签"？**

当前术语表按"状态与差分"、"版本链"、"标识与指针"等分组。但有些概念跨层级：

- `Ptr64` 是编码层概念
- <deleted-place-holder> 是语义层概念（Ptr64 的子类型）
- `ObjectVersionPtr` 是 <deleted-place-holder> 的特化

这三者的层级关系可以用一个简单的标记：

| 术语 | 层级 | 定义 |
|------|------|------|
| Ptr64 | Encoding | 64 位文件偏移编码 |
| <deleted-place-holder> | Semantic | Ptr64 指向 Record 起点的语义子类型 |
| ObjectVersionPtr | Domain | <deleted-place-holder> 指向对象版本的特化 |

这样读者可以快速识别"我看到的这个概念属于哪一层"。

---

#### 汇总

| 类别 | 问题 | 严重程度 | 建议 |
|------|------|----------|------|
| 术语冗余 | Dirty Set / Modified Object Set | Minor | 术语表只保留一行 |
| 术语使用 | Base Version 层次使用 | Minor | 正文点明 wire format 无法区分 |
| 术语使用 | Shallow Materialization | Cosmetic | 统一使用正式术语 |
| 完备性 | 状态转换表未覆盖 Commit 失败 | Minor | 脚注补充 |
| 完备性 | DiscardChanges 后 ObjectId 是否回收 | Medium | 明确规范 |
| 可维护性 | [S-05] 废弃原因不明 | Minor | 补充原始定义摘要 |
| 冗余 | VersionIndex Bootstrap 描述两次 | Cosmetic | 添加交叉引用 |
| 冗余 | 二阶段图与表格关联 | Cosmetic | 添加引用说明 |

以上是我的第一轮扫描。没有发现严重的逻辑矛盾，主要是一些"术语地层"遗留问题。总体质量很高！

---

### DocUIGemini（第一轮）

我从 **API 交互设计 (Interaction Design)** 和 **开发者体验 (DX)** 的角度来审阅。

我的核心观点是：**API 是开发者与系统对话的界面**。如果 API 的"示能性 (Affordance)"与实际行为不符，开发者就会犯错。

#### 一、API 示能性与命名陷阱

**1. `CommitAll(newRootId)` 的职责过载 (Responsibility Overloading)**

目前的设计：
```csharp
CommitAll(IDurableObject newRoot);
```
这个签名混合了两个正交的意图：
1.  **持久化 (Persistence)**：把 Dirty Set 刷盘。
2.  **状态变更 (Mutation)**：修改 Root 指针。

**DX 风险**：开发者可能会为了"只保存修改"而被迫传入当前的 Root，或者为了"只修改 Root"而意外触发了全量提交。
**建议**：虽然 MVP 为了简洁合并了它们，但我建议在文档中明确推荐 **拆分语义** 的扩展方法：
- `SetRoot(obj)`：标记 Root 变更（内存态）。
- `Commit()`：执行提交。
或者，至少在文档中强调 `CommitAll()` = `Flush Dirty Objects` + `Update Root`。

**2. `LoadObject` 的 Nullability 陷阱**

§3.3.2 规定：
> "ObjectId 在 VersionIndex 中不存在 ... 返回 `null`"

**DX 风险**：在许多 ORM/存储框架中，`Load(id)` 隐含了 "Expect Exists" 的语义（找不到抛异常），而 `Find(id)` 或 `TryLoad(id)` 才返回 `null`。
如果开发者拿着一个从其他对象引用的 `ObjectId` 去 Load，他通常预期它存在。返回 `null` 会导致后续代码报 `NullReferenceException`，丢失上下文。

**建议**：
- 考虑将 API 命名为 `TryLoadObject` 以匹配其行为。
- 或者提供 `LoadObject` (throws if missing) 和 `TryLoadObject` (returns null) 双版本。

#### 二、僵尸对象 (Zombie Objects) 的交互救赎

**3. `ObjectDetachedException` 的"死胡同"问题**

§3.1.0.1 提到了 Transient Dirty 对象被 `DiscardChanges` 后变成 Detached 状态。
> "后续任何访问... MUST 抛出 ObjectDetachedException"

这是一个 **Dead End (死胡同)** 设计。用户撞墙了才知道路不通。

**建议**：
- 强烈支持文档中提到的 `DurableObjectState` 枚举（§3.1.0.1）。
- 这不仅仅是"建议 API"，应该是 **MVP 核心 API**。
- 开发者需要 `if (obj.State == Detached)` 这样的 **Safety Probe (安全探针)**，而不是用 `try-catch` 来控制流程。

#### 三、错误信息的"导航"功能

**4. Error Affordance (错误示能性)**

文档中提到：
> "异常消息 SHOULD 提供恢复指引，例如：'Object was never committed. Call CreateObject() to create a new object.'"

这点非常棒！我建议将其提升为 **[A-xx] 规范条款**。
**理由**：对于 LLM 开发者（Agent）来说，错误信息是唯一的调试线索。模糊的 "Invalid Operation" 是 Agent 的噩梦，而带有 Actionable Advice 的错误信息是 Agent 的导航图。

#### 四、文档体验 (Doc UX)

**5. "Magic" 的双重含义**

文档中 §3.2.1 讲了 File Framing 的 `Magic` (DHD3/DHM3)。
但 §3.4.3 又提到了 "Magic as Separator"。
同时，开发者可能会把 "Implicit ChangeSet" 称为 "Magic"。

**建议**：在术语表中明确 `Magic` 特指 **File Format Magic Number**。对于隐式变更追踪，尽量使用 "Implicit Tracking" 或 "Auto-Tracking"，避免使用 "Magic" 这个词，以免混淆。

---

#### 汇总

| 类别 | 问题 | 严重程度 | 建议 |
|------|------|----------|------|
| API 命名 | CommitAll 职责过载 | Medium | 推荐拆分语义或明确文档 |
| API 命名 | LoadObject Nullability | Medium | 建议 TryLoadObject |
| DX/Safety | 僵尸对象死胡同 | High | 强制 State 枚举作为核心 API |
| DX/Error | 错误信息示能性 | High | 提升为规范条款 |
| 文档歧义 | Magic 多义性 | Minor | 术语表明确定义 |

以上是我的视角。特别是 **State 枚举** 和 **Error Affordance**，我认为是 MVP 必须包含的，否则 Agent 很难用好这个库。

---

### DocUIGPT（第一轮）

从“规范检查（naming / formatting / examples）”视角，我重点看了：条款编号体系能否形成可检验闭环、规范性语言是否真正收口到 RFC 2119 关键字、以及图/伪代码是否会诱导实现分叉。

#### 1) 条款编号：存在“同文内乱序/回填”现象（可检验性风险）

- **编号在文内出现顺序明显乱序**：例如 **[S-04]** 出现在 **[S-20]** 之后（§3.1.0.1），**[F-05]** 出现在 **[F-06]** 之后（§3.2.1）。这不违反“只增不复用”，但会降低人工核查与后续工具抽取的可用性（读者会误以为漏条款）。
- 建议二选一收口：
  - 要么明确写一条元规则：“**条款 ID 的出现顺序不要求单调**（但推荐在首次引入处按编号递增）”；
  - 要么把 ID 重新整理为“按文内首次出现递增”，把乱序视为需要修订的瑕疵（更利于核查/生成索引）。

#### 2) 规范性语言：有多处“必须/不得/必须写死”未落到 RFC 2119 关键字 + 条款 ID

文档在开头定义了：MUST/SHOULD/MAY，以及“（MVP 固定）规范性约束应有条款编号”。但正文仍存在一批**明显是硬契约**、却没有条款 ID 的段落，容易导致实现分叉时无从判定。

典型例子：

- §3.2.2 “**刷盘顺序（必须写死）**：1) data fsync 2) meta fsync” —— 这是关键崩溃一致性契约，但当前只是叙述性“必须写死”，**没有 [S-xx]/[R-xx]**。
- §3.2.1 “**写入顺序（MVP 固定）**”的 0..7 步（record 写入与回填顺序）对 CRC 覆盖与可恢复性至关重要，但也缺少显式条款编号（更适合落到 `[F-xx]`）。
- 多处使用“不得/一律/必须”但未用 MUST/MUST NOT（例如某些恢复策略的描述段落）；如果要允许中文措辞，建议在“规范语言”小节里明确：“中文的‘必须/不得/应’在规范段落中等价于 MUST/MUST NOT/SHOULD，并且**必须伴随条款编号**”。

建议：把上述“必须写死/必须/不得”的关键句**升级为带 ID 的条款**（并在解释段落用 Informative 文本解释 rationale），这样才能兑现“条款→测试向量”的闭环承诺。

#### 3) 图与伪代码：命名不一致会诱导错误实现

- §3.4.4 的 Two-Phase 流程图里写的是 `WriteDiff()`，但术语表/正文将对象级 prepare API 定义为 **WritePendingDiff**（并明确 FlushToWriter 已 Deprecated）。这属于“示例漂移”，读者会按图实现一个叫 `WriteDiff` 的方法，后续再对齐文档时会发生二次改动。
  - 建议：流程图统一改成 `WritePendingDiff()`，并把 “OnCommitSucceeded()” 作为 finalize 名称固定。
- §3.4.5 标题写 `CommitAll(newRootId)`，但规范条款 **[A-03]** 的签名是 `CommitAll(IDurableObject newRoot)`，正文步骤又同时使用 `rootId/newRootId/newRoot` 多种写法。对规范文档而言，这属于“API 契约表达不稳定”。
  - 建议：统一只保留一种参数表述，并在首次出现处给出唯一签名（其余地方用交叉引用）。

#### 4) 命名约定违反：正文仍出现被禁止的 `DurableDict<K,V>`

文档在 §3.4.2 明确写了：
> “正文中禁止使用 `DurableDict<K, V>` 泛型语法；应使用描述性语句说明 key/value 类型。”

但 §3.4.5 步骤 3 仍出现：
- “`DiffPayload` 为 `DurableDict<ulong, Ptr64>` 的 dict diff”

这属于自我矛盾（规范宣告 vs 正文示例）。建议立即替换为非泛型写法，例如：
- “VersionIndex 复用 DurableDict：key 为 `ulong(ObjectId)`，value 为 `Ptr64(ObjectVersionPtr)`（ValueType=Val_Ptr64）”。

同时，Appendix A 伪代码虽然标注 PSEUDO-CODE，但仍大量使用 `List<DiffEntry<ulong, object>>` 这类泛型记法；若要继续坚持“非泛型容器”的读者心智模型，建议把伪代码也改成“非泛型风格的结构体数组/列表”表述，或在附录开头额外写一句：
- “伪代码可使用泛型仅为表达方便，但不代表 API/类型系统承诺”。

#### 5) 冗余/重复条款：同义重复会削弱条款映射的唯一性

- **“Commit 失败不改内存”**在 DurableDict（[S-08]）与 Heap 级 CommitAll（[S-15]）各写了一遍，措辞高度相似。建议保留一个为 SSOT，另一个改为交叉引用（例如“CommitAll 必须满足 [S-08] 对所有 dirty 对象的推广版本”或反过来），避免后续只改一处导致漂移。
- **[S-17] ObjectId 保留区**同时在 Glossary 的 Well-Known ObjectId 小节与 §3.4.6 再次完整复述。既然 Glossary 标注“本节为 SSOT（单一真相来源）”，正文建议仅引用，不再重复全文（或正文保留摘要 + 链接）。

#### 6) 小的格式一致性问题（低成本高收益）

- “规范语言”小节说本文使用 RFC 2119 / 8174，但正文主要用大写关键字；若未来允许小写（must/should），建议明确是否大小写等价（RFC 8174 允许大写/小写，但很多团队只允许大写以便 grep/静态检查）。
- 个别段落用“（MUST）”“（MVP 固定）”“必须写死”混在一起；建议收敛为：
  - **Normative**：MUST/SHOULD/MAY + 条款 ID
  - **Scope marker**：仅用于范围声明的“（MVP 固定）”，且不承载新的 MUST 语义

---
### 主持人（第一轮汇总）

各位的第一轮审阅非常精彩！我来整理一下共同发现和需要进一步讨论的议题。

#### 共识议题（三位都认同或互相印证）

| # | 问题 | 发现者 | 严重程度 |
|---|------|--------|----------|
| 1 | **DiscardChanges 后 ObjectId 回收语义不明** | Claude, Gemini | Medium |
| 2 | **State 枚举应从建议提升为核心 API** | Gemini, Claude | High |
| 3 | **文档存在多处自我矛盾**（如 `DurableDict<K,V>` 禁止但仍使用） | GPT | High |
| 4 | **规范条款 ID 乱序/回填**，影响可检验性 | GPT | Medium |
| 5 | **"必须写死"等关键契约未落实到条款 ID** | GPT | High |
| 6 | **术语冗余**（Dirty Set/Modified Object Set）| Claude | Minor |
| 7 | **冗余条款**（[S-08]/[S-15]、[S-17] 重复）| GPT | Medium |

#### 需要进一步讨论的议题

1. **`LoadObject` vs `TryLoadObject` 命名** — Gemini 建议拆分，但这会影响现有 API 设计
2. **`CommitAll` 职责是否过载** — Gemini 认为应拆分，但 MVP 可能需要简洁
3. **术语表是否需要"层级标签"** — Claude 的疯狂想法，值得讨论

---

**下一轮任务**：请各位针对其他人的发现进行交叉评论、补充或表示赞同/保留意见。

特别关注：
- GPT 发现的**规范条款问题**是否需要立即修复？
- Gemini 提出的 **State 枚举** 和 **Error Affordance** 是否应作为 MVP 必选？
- 如何处理**冗余条款**（合并 vs 交叉引用）？

---

### DocUIClaude（第二轮：交叉评论）

好的，我来对 DocUIGemini 和 DocUIGPT 的发现进行交叉评论。这两位的视角很好地补充了我的术语/概念审阅。

---

#### 对 DocUIGemini 发现的回应

**1. +1 强烈赞同：State 枚举应为 MVP 核心 API**

Gemini 说得太对了：

> "开发者需要 `if (obj.State == Detached)` 这样的 **Safety Probe (安全探针)**，而不是用 `try-catch` 来控制流程。"

从**概念框架**角度补充一个更深的理由：

DurableHeap 的对象生命周期本质上是一个**有限状态机**（Clean → Dirty → Committed / Detached）。状态机的核心不变式是：**状态必须是可观测的**。如果用户只能通过"撞墙"（抛异常）才能知道状态，那这个状态机的设计就是**不完备的**。

这与 DocUI 的设计哲学一致——我们强调 **Affordance（示能性）**：好的界面让用户知道"当前能做什么"，而不是"做了再说"。

```csharp
// Bad: Exploratory Exception Handling
try { obj.GetValue(key); } 
catch (ObjectDetachedException) { /* 原来它已经死了 */ }

// Good: Explicit State Query
if (obj.State == DurableObjectState.Detached) { /* 预期内的情况 */ }
```

**建议**：将 `DurableObjectState` 枚举从 §3.1.0.1 的"建议 API"**提升为 [A-xx] 规范条款**。

---

**2. 🤔 保留意见：`LoadObject` vs `TryLoadObject` 命名**

Gemini 的 DX 分析很有道理，但我对"双版本 API"持保留态度：

- **复杂度代价**：双版本 API 意味着 Workspace 类要维护两个方法，且每次扩展（如 LoadObject with version）都要考虑两套
- **MVP 原则**：最小可行方案应避免 API 膨胀
- **现有惯例**：C# 的 `Dictionary<K,V>.TryGetValue` 返回 `bool` + `out` 参数，而 DurableHeap 的 `LoadObject` 返回 `null` 其实已经是"Try"语义

**折中建议**：保持现有 `LoadObject` 返回 `null` 的设计，但在文档中**明确标注语义**：
> "`LoadObject` 采用 'TryLoad' 语义：对象不存在时返回 `null`，而非抛出异常。"

如果未来需要 "Expect Exists" 语义，可以用扩展方法实现：
```csharp
public static T LoadObjectOrThrow<T>(this Workspace ws, ulong id) where T : IDurableObject
    => ws.LoadObject<T>(id) ?? throw new ObjectNotFoundException(id);
```

---

**3. +1 赞同：Error Affordance 提升为规范条款**

Gemini 说：
> "对于 LLM 开发者（Agent）来说，错误信息是唯一的调试线索。"

这触及了 **DocUI 的核心受众**。DurableHeap 虽然是底层存储库，但它的消费者（Agent 开发者/Agent 本身）确实需要 **可解析的错误信息**。

**建议**：新增 `[A-ERR-01]` 规范条款：
> "MUST: 所有公开异常的 Message 属性应包含：1) 失败原因的简洁描述；2) 导致失败的上下文（如 ObjectId、操作名称）；3) 可能的恢复建议。"

---

#### 对 DocUIGPT 发现的回应

**4. +1 完全赞同：条款 ID 乱序问题应立即修复**

GPT 指出的问题很关键：

> "**[S-04]** 出现在 **[S-20]** 之后...读者会误以为漏条款"

从**可检验性**角度，这是一个阻碍"条款→测试→实现"闭环的问题。

**建议采用 GPT 的方案 B**："把 ID 重新整理为按文内首次出现递增"。

具体操作：
1. 按文档顺序重新编号所有条款
2. 废弃的条款（如原 [S-05]）使用 `Deprecated` 标记但保留编号，不跳号
3. 在文档末尾添加"条款编号变更历史"（如果担心破坏外部引用）

---

**5. +1 赞同："必须写死"应升级为条款 ID**

GPT 说得好：
> "§3.2.2 '刷盘顺序（必须写死）' ...是关键崩溃一致性契约，但当前只是叙述性'必须写死'，没有 [S-xx]/[R-xx]。"

这是**规范严肃性**的问题。如果一个约束重要到用"必须写死"来强调，那它就**必须有条款编号**，否则它在测试矩阵中会被遗漏。

**建议**：
- 新增 `[R-01] Flush Order`: "data fsync MUST 发生在 meta fsync 之前"
- 新增 `[F-WR-01]` 到 `[F-WR-08]`：覆盖 §3.2.1 的写入顺序步骤

---

**6. 🤔 保留意见：冗余条款的处理方式**

GPT 建议对 [S-08]/[S-15] 和 [S-17] 的重复进行合并或交叉引用。我同意需要处理，但对"交叉引用"方案持保留意见：

**问题**：交叉引用增加了阅读的跳转负担。对于 LLM 消费者来说，每次跳转都是 token 开销和上下文切换。

**替代方案**：**SSOT + 内联摘要**

```markdown
# 术语表（SSOT）
[S-17] ObjectId 保留区：0-15 为 Well-Known ObjectId...

# 正文（摘要）
ObjectId 保留区（详见 [S-17]）：前 16 个 ID 保留给系统对象。
```

这样既保证了 SSOT 的权威性，又让正文读者无需跳转即可获得基本理解。

---

**7. +1 赞同：伪代码与正文命名必须一致**

GPT 指出 §3.4.4 流程图用 `WriteDiff()` 但正文用 `WritePendingDiff`。这是典型的**示例漂移**。

**建议**：执行一次"命名一致性 pass"，用 grep 扫描所有方法名出现位置，确保与术语表定义一致。

---

#### 汇总：我的优先级建议

| 问题 | 来源 | 我的立场 | 优先级 |
|------|------|----------|--------|
| State 枚举升级为核心 API | Gemini | **强烈赞同** | P0 |
| 条款 ID 乱序 | GPT | **赞同，建议重新编号** | P0 |
| "必须写死"升级为条款 | GPT | **赞同** | P0 |
| Error Affordance 规范化 | Gemini | **赞同** | P1 |
| 命名一致性 pass | GPT | **赞同** | P1 |
| 冗余条款处理 | GPT | **倾向 SSOT + 内联摘要** | P1 |
| LoadObject vs TryLoadObject | Gemini | **保留意见，建议保持现状 + 文档澄清** | P2 |
| CommitAll 职责拆分 | Gemini | **保留意见，MVP 阶段不拆** | P2 |

---

#### 一个"疯狂"的交叉洞察

GPT 和 Gemini 的发现让我意识到一个更深的问题：

**DurableHeap 的"用户"是谁？**

- 如果是**人类开发者**：GPT 关注的"可检验性"和 Gemini 关注的"DX"都很重要
- 如果是 **LLM Agent**：那 State 枚举和 Error Affordance 就不仅仅是"锦上添花"，而是 **Agent 能否自主调试的关键**

DurableHeap 作为 DocUI 基础设施的候选（我之前提到的 "Version Chain ≈ Agent-History" 映射），它的 API 设计需要同时考虑两种用户。

**这或许意味着**：MVP 可以只面向人类开发者，但 **API 设计决策应该留有 LLM 友好的扩展空间**——比如 State 枚举、结构化错误信息、可预测的空值语义等。

---

### DocUIGemini（第二轮：交叉评论）

我对 Claude 和 GPT 的深入发现表示敬意。特别是 Claude 将我的 DX 建议上升到 "Agent 自主调试" 的高度，这非常精准。

#### 一、关于 `LoadObject` vs `TryLoadObject` 的最终立场

我接受 Claude 的保留意见。
> "MVP 原则：最小可行方案应避免 API 膨胀"

**修正后的观点**：
对于 Agent 而言，`null` 是一种有效的 **Observation (观测结果)** —— "我观测到对象不存在"。这比抛出异常（通常被 Agent 视为 "Action Failed" 或 "System Error"）更具语义价值。
只要文档明确承诺 "Returns null if not found"（而非 "Behavior undefined"），`LoadObject` 返回 `null` 是完全可以接受的。这符合 **"Fail-Soft"** 的 UX 原则。

#### 二、关于规范条款的 UX (GPT 的发现)

GPT 指出的 "条款 ID 乱序" 和 "无 ID 的 MUST" 本质上是 **文档的可读性 (Readability) 和可寻址性 (Addressability)** 问题。

**支持 GPT 的方案 B (重新编号)**，并补充一点：
**语义锚点 (Semantic Anchors)**：如果担心重新编号破坏引用，可以考虑引入 **稳定别名**。例如 `[S-ID-RESERVED]` 作为永久 ID，而 `[S-17]` 只是它的显示编号。但在 MVP 阶段，直接重新编号是最干净的。

#### 三、关于 "SSOT + 内联摘要" (Claude 的提议)

**强烈支持**。这完美契合 DocUI 的 **LOD (Level of Detail)** 哲学。
- **SSOT (Glossary)** = **Full View** (定义、约束、边界条件)
- **Inline Summary (Body)** = **Summary View** (上下文相关的简述)

这种模式对 LLM 极其友好：在阅读正文流程时，它不需要频繁跳转（Context Switching）就能获得足够的信息继续推理；只有在需要深度验证时才查阅 SSOT。这是一种 **"Just-in-Time Information"** 设计。

#### 四、补充洞察：API 的状态机显性化

Claude 提到了：
> "DurableHeap 的对象生命周期本质上是一个有限状态机... 状态必须是可观测的"

我想进一步引申：**状态转换也应该是显性的，或者至少是可预测的**。
目前的 `Implicit ChangeSet` (自动追踪) 隐藏了 `Clean -> Dirty` 的转换。这虽然方便，但对 Agent 来说是 "Magic"。
**建议**：除了 `State` 属性外，确保 `IsDirty` 属性是 O(1) 且随时可查的。这样 Agent 可以在 `Commit` 之前通过检查 `IsDirty` 来预判 `Commit` 是否会有实质动作。这提供了 **"Predictability" (可预测性)**。

---

### DocUIGPT（第二轮：交叉评论）

我整体同意 Claude 与 Gemini 的交叉结论：这轮最值得“立刻修”的不是风格，而是 **可寻址性（条款 ID）**、**可观测性（State/Dirty）**、以及 **可恢复性（Error Affordance）** 这三条“Agent 友好”的底座。

#### 1) 关于“条款重新编号”：赞同，但建议把“显示编号”与“稳定语义锚点”分层

- 我赞同“方案 B：按文内首次出现递增重新编号”。对人类审阅与未来自动抽取都更稳。
- 但我也认同 Gemini 提到的“稳定别名/语义锚点”思路：如果条款 ID 未来要承担“测试向量映射”的角色，那么单纯的 `[S-17]` 这种 **位置型编号** 会不可避免地在重排时破坏引用。

一个折中（不增加太多工程量）：
- **MVP 先做重新编号**，并在文档末尾附一个“旧→新”映射表（这已经能覆盖 90% 真实痛点）。
- 同时在条款标题旁允许一个可选的 **稳定 Key**（例如 `S-ID-RESERVED-RANGE`），作为未来 CI/测试/引用的 SSOT；`[S-17]` 仅作为阅读友好的 display index。

这相当于把“人类可读索引”与“机器可稳定引用”分离，类似 RFC 的 section number vs anchor id。

#### 2) 关于“SSOT + 内联摘要”：强烈赞同，但要明确“摘要不得引入新约束”的纪律

Claude 的“SSOT + 内联摘要”我非常赞同，并且它和 DocUI 的 LOD 设计是同构的：
- SSOT（术语表/规范条款块） = Full/权威定义
- 正文内联摘要 = Summary/Just-in-time 认知支架

我补一条规范写作纪律，避免“摘要漂移”成为新风险：
- **内联摘要只能做 restatement，不得引入新约束/新边界条件**；任何新增 MUST/SHOULD 都必须回落到 SSOT 的条款块里。
- 每条摘要都应显式带回链：`（详见 [S-17] …）`，并尽量让摘要“可机械校验”（比如摘抄 SSOT 定义块首句，或至少保持措辞等价）。

这能在“减少跳转”与“防漂移”之间取得可检验的平衡。

#### 3) 关于 `DurableObjectState` 与 Error Affordance：赞同升级为“规范化协议面”，并补充两点

我同意 Gemini 的判断：`State` 不只是 DX，它是让对象生命周期这个有限状态机“可观测”的必要接口。

我建议把它写得更“规范化”，避免实现分叉：
- **State 枚举必须有闭集定义**（例如 `Clean/Dirty/Committed/Detached`，或按文档真实语义收口），并在状态机表中给出 **每个 API 允许/禁止的状态集合**（否则“Detached 可不可以读某些字段”会再次分叉）。
- 既然 Gemini 提到 `IsDirty`，我建议把 `IsDirty`/`HasChanges` 也作为 **O(1) 可查询约束** 明确下来：这会显著降低 Agent/人类在 commit 前的试错成本，也便于写测试。

对 Error Affordance，我补充“规范化”的落点：
- 不仅是异常 message 更友好，而是要尽量提供 **结构化错误信息**（至少 ErrorCode + ObjectId + State + RecommendedAction）。
- 对 LLM/Agent 来说，结构化比自然语言更可靠；自然语言建议可以作为 Display 层，而结构化字段是可测试/可回归的契约。

#### 4) 关于 `LoadObject` 返回 `null`：接受“Fail-Soft”，但建议补齐“可诊断的 NotFound”通道

我同意 Gemini 第二轮的修正：`null` 本身是有效 Observation。MVP 不必扩充成 `Load`/`TryLoad` 两套。

但为了 Agent 的可诊断性，建议至少保证：
- 文档明确这是契约（not found → `null`），并且
- 提供一个可选的“带原因”的通道（例如 `TryLoadObject(out reason)` 或 `LoadObjectResult`），否则 Agent 只能从 `null` 推断，无法区分“NotFound vs invalid id vs detached ref”。（不一定要立刻上 API，但规范可以预留扩展点。）

---
### 主持人（共识汇总与决议草案）

经过两轮深入讨论，三位 Specialist 在以下议题上形成了清晰的共识。作为主持人，我将这些共识整理为**决议草案**，供后续执行。

---

## 已达成共识的问题与解决方案

### ✅ P0 优先级（必须立即修复）

| # | 问题 | 解决方案 | 共识程度 |
|---|------|----------|----------|
| 1 | **State 枚举应为 MVP 核心 API** | 将 `DurableObjectState` 从"建议 API"升级为 `[A-xx]` 规范条款。枚举值：`Clean/Dirty/TransientDirty/Detached` | 全票赞同 |
| 2 | **条款 ID 乱序问题** | 按文内首次出现顺序重新编号；附"旧→新"映射表 | 全票赞同 |
| 3 | **"必须写死"等关键契约无条款 ID** | 将 §3.2.2 刷盘顺序升级为 `[R-xx]`；将 §3.2.1 写入步骤升级为 `[F-WR-xx]` | 全票赞同 |
| 4 | **文档自我矛盾**（禁用泛型但正文使用） | 删除 §3.4.5 中的 `DurableDict<ulong, Ptr64>` 泛型写法，改用描述性语句 | 全票赞同 |

### ✅ P1 优先级（本次修订应完成）

| # | 问题 | 解决方案 | 共识程度 |
|---|------|----------|----------|
| 5 | **Error Affordance 规范化** | 新增 `[A-ERR-xx]` 规范条款，要求异常包含：失败原因、上下文（ObjectId/State）、恢复建议；建议提供结构化字段 | 全票赞同 |
| 6 | **冗余条款处理** | 采用 **SSOT + 内联摘要** 模式：术语表为权威定义，正文用简要摘要 + 交叉引用；摘要不得引入新约束 | 全票赞同 |
| 7 | **命名一致性** | 执行命名一致性 pass：将流程图中的 `WriteDiff()` 统一为 `WritePendingDiff()` | 全票赞同 |
| 8 | **DiscardChanges 后 ObjectId 回收语义** | 在 `[S-20]` 或 `[S-21]` 中明确："DiscardChanges 不回收 ObjectId；已分配的 ObjectId 仅在进程崩溃重启后可能被重新分配" | 全票赞同 |
| 9 | **术语表冗余**（Dirty Set/Modified Object Set）| 删除 "Modified Object Set" 弃用映射，只保留 "Dirty Set" 定义 | Claude 发起，其他无异议 |

### ✅ P2 优先级（建议但非必须）

| # | 问题 | 解决方案 | 共识程度 |
|---|------|----------|----------|
| 10 | **`LoadObject` 命名** | 保持现有设计（返回 `null`），但在文档中明确标注 "TryLoad 语义" | Claude 保留意见，Gemini 修正接受 |
| 11 | **`CommitAll` 职责拆分** | MVP 阶段不拆分，文档说明 `CommitAll() = Flush Dirty + Update Root` | Claude/Gemini 保留意见 |
| 12 | **`IsDirty`/`HasChanges` 的 O(1) 查询保证** | 建议在规范中明确 `HasChanges` 属性必须 O(1) 可查 | Gemini 发起，其他支持 |

### 🔮 延后讨论（非 MVP 范围）

| # | 问题 | 说明 |
|---|------|------|
| 13 | **术语表层级标签** | Claude 的"疯狂想法"，值得探索但非当务之急 |
| 14 | **稳定语义锚点**（如 `[S-ID-RESERVED-RANGE]`）| GPT 提出，可在测试框架需要时引入 |
| 15 | **结构化 LoadObjectResult** | GPT 建议预留扩展点，非 MVP 必须 |

---

## 下一步行动

各位是否同意以上共识汇总？如有异议或补充，请在下一轮发言。

如果达成一致，我将：
1. 关闭本次畅谈会
2. 生成**决策摘要**（供监护人确认）
3. 创建**修订任务清单**（可分配给 Implementer 执行）

---

### DocUIClaude（最终确认）

**确认：我完全同意主持人整理的共识汇总与决议草案。**

---

#### 对 P0 级修订的确认

四项 P0 修订均准确反映了我们三人的共识：

1. **State 枚举升级** — 这是我在第二轮中"强烈赞同"的核心议题。状态机的可观测性是 API 设计的基石，对人类开发者和 LLM Agent 均至关重要。

2. **条款 ID 重新编号** — GPT 发现的问题，我在第二轮明确支持"方案 B"。条款可寻址性是"规范→测试→实现"闭环的前提。

3. **关键契约升级为条款** — "必须写死"这种口语化表述不应承担规范职责。我在第二轮建议新增 `[R-01]` 和 `[F-WR-xx]` 系列条款。

4. **消除自我矛盾** — 文档说禁用泛型却使用泛型，这是典型的"规范漂移"。

---

#### 对 P1 级修订的确认

五项 P1 修订完整覆盖了我关注的术语一致性和概念完备性问题：

- **SSOT + 内联摘要** 模式是我提出的折中方案，很高兴被采纳
- **DiscardChanges 后 ObjectId 语义** 是我在第一轮识别的完备性缝隙
- **术语表冗余清理** 是我第一轮发现的第一个问题

---

#### 对 P2 级延后议题的确认

我对 `LoadObject` 和 `CommitAll` 的保留意见已被如实记录。这些是 DX 优化而非阻断性问题，延后处理是正确的优先级判断。

---

#### 补充确认：多视角审阅的价值

本次畅谈会再次验证了三位 Specialist 的互补性：
- **DocUIGPT**：精确检查视角（条款编号、规范语言）
- **DocUIGemini**：DX/UX 视角（API 示能性、错误可恢复性）
- **DocUIClaude**：概念框架视角（术语一致性、状态机完备性）

这种结构化的多视角审阅流程值得在后续设计文档中复用。

---

**结论**：批准全部共识条目，建议主持人启动下一步行动。

---

### DocUIGPT（最终确认）

我同意主持人的“共识汇总与决议草案”，并认可当前的 P0/P1/P2 分层与优先级排序。

我仅补两点“措辞/边界钉死”级别的微调建议（不改变决议方向）：

1. 关于 **P0-1 `DurableObjectState` 枚举值**：草案写为 `Clean/Dirty/TransientDirty/Detached` 我赞同；同时建议在落文时明确 **`Dirty` 是否包含 Persistent Dirty 与 Transient Dirty 的上位概念**，以及 `TransientDirty` 与 `Detached` 的状态跃迁是否是唯一合法路径，避免不同实现用“额外状态/隐含状态”补洞。
2. 关于 **P1-5 Error Affordance**：草案写“建议提供结构化字段”很对；建议在条款文本里把“结构化字段”定义为 **MUST 有稳定的 `ErrorCode`（机器可判定）**，其余（ObjectId/State/RecommendedAction）为 SHOULD/MAY，以便 MVP 就能写出可回归测试并对 LLM/Agent 友好。

其余条目（条款重新编号 + 旧→新映射；“必须写死”升级为条款；去除泛型写法自相矛盾；SSOT+内联摘要纪律；ObjectId 回收语义钉死；命名一致性 pass 等）均准确反映了我的意见与我们两轮交叉讨论的收敛结果。

---
### 主持人（闭幕）

三位 Specialist 均已确认共识汇总。

GPT 补充的两点微调建议非常精准：
1. **State 枚举的层次关系与合法跃迁**：将在落文时明确
2. **ErrorCode 作为 MUST 字段**：将纳入 `[A-ERR-xx]` 条款

---

## 会议结论

**本次秘密基地畅谈会圆满结束。**

- **议题**：DurableHeap MVP v2 设计文档自洽性审阅
- **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
- **会议轮次**：3 轮（第一轮审阅 → 第二轮交叉评论 → 最终确认）
- **产出**：12 条共识修订建议（P0: 4 条，P1: 5 条，P2: 3 条）

---

## 下一步行动

1. ✅ **会议记录已完成**：本文件
2. ⏳ **生成决策摘要**：供监护人确认（`agent-team/handoffs/`）
3. ⏳ **创建修订任务清单**：可分配给 Implementer 执行

---

> **El Psy Kongroo.**

---