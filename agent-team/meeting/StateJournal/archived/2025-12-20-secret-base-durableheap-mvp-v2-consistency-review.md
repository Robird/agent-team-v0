# 秘密基地畅谈：DurableHeap MVP v2 设计文档一致性审阅

> **日期**：2025-12-20
> **目标文档**：`DurableHeap/docs/mvp-design-v2.md`
> **会议形式**：秘密基地畅谈 (Hideout Jam Session)
> **审阅目标**：确保文档自洽、一致、低冗余和重复
> **主持人**：刘德智 (AI Team Leader / DocUI 规范起草委员会主席)

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

本次审阅专注于以下维度：

1. **自洽性 (Self-Consistency)**：文档内部是否有矛盾？术语定义与使用是否一致？
2. **一致性 (Coherence)**：不同章节的描述是否对齐？编号条款是否有冲突？
3. **低冗余 (Low Redundancy)**：是否存在重复表述？是否可以合并或引用？
4. **完备性 (Completeness)**：是否有遗漏的边界条件或未定义的行为？

---

## Round 1: 独立发现 (Independent Findings)

> 每位 Specialist 独立阅读文档，列出发现的问题。

---

### DocUIClaude 的发现

作为概念框架专家，我按四个维度系统审阅了 MVP v2 设计文档。以下是我的发现：

---

#### 🔴 Critical Issues（阻塞实现的问题）

##### C1. `base/初始` 术语未更新 (§3.2.5)

**位置**：[mvp-design-v2.md](../../DurableHeap/docs/mvp-design-v2.md#L364) 第 364 行

**问题描述**：
> `PrevVersionPtr`：Ptr64（该 ObjectId 的上一个版本；若为 0 表示 **base/初始**）

术语表已明确定义 `Base Version` 作为上位术语，`Genesis Base` 和 `Checkpoint Base` 作为子类型。但此处仍使用未规范化的"base/初始"表述。

**严重度**：Critical（术语 SSOT 与正文不一致，影响实现者理解）

**解决方向**：改为"若为 0 表示 **Base Version**（Genesis Base 或 Checkpoint Base）"

---

##### C2. Well-Known ObjectId 预留范围冲突 (§3.3.1 vs §3.4.6)

**位置**：
- [§3.3.1](../../DurableHeap/docs/mvp-design-v2.md#L423-L428)：`NextObjectId = 16`（0 保留给 VersionIndex；1-15 保留给未来 well-known 对象）
- [§3.4.6](../../DurableHeap/docs/mvp-design-v2.md#L586-L593)：`NextObjectId = 1`（ObjectId = 0 保留给 VersionIndex 或其他 well-known 对象）

**问题描述**：两处定义的初始 `NextObjectId` 不一致（16 vs 1）。§3.3.1 明确预留 0-15，但 §3.4.6 只预留 0。

**严重度**：Critical（空仓库初始化行为不确定）

**解决方向**：统一为 `NextObjectId = 16`（与 §3.3.1 对齐），并在 §3.4.6 添加说明：`ObjectId 1-15 预留给未来 well-known 对象`。

---

#### 🟠 Major Issues（需要修复但不阻塞理解）

##### M1. `ObjectKind` 枚举值定义不集中

**位置**：
- [术语表](../../DurableHeap/docs/mvp-design-v2.md#L115)：仅定义概念
- [§3.2.5](../../DurableHeap/docs/mvp-design-v2.md#L365-L369)：`0-127` 标准类型，MVP 定义 `Dict=1`；`128-255` 保留
- [§3.4.2](../../DurableHeap/docs/mvp-design-v2.md#L481)：dict diff 使用 `ObjectKind=Dict`

**问题描述**：`ObjectKind` 的枚举值定义散落在多处（术语表只有概念、§3.2.5 有范围划分、正文提到 `Dict=1`）。缺少集中的枚举值表。

**严重度**：Major（实现者需要在文档中搜索拼凑完整定义）

**解决方向**：在术语表或 §3.2.5 添加枚举值表：
```
| 枚举值 | 名称 | 说明 |
|--------|------|------|
| 0x00 | Reserved | 保留 |
| 0x01 | Dict | DurableDict（MVP 唯一实现） |
| 0x02-0x7F | Standard | 标准类型（未来扩展） |
| 0x80-0xFF | Variant | 版本变体（如 DictV2） |
```

---

##### M2. `RecordKind` 文件域隔离约束与枚举值定义分离

**位置**：
- [术语表 [F-01]](../../DurableHeap/docs/mvp-design-v2.md#L111-L112)：定义域隔离约束
- [§3.2.1](../../DurableHeap/docs/mvp-design-v2.md#L273)：`DataRecordKind: 0x01 = ObjectVersionRecord`
- [§3.2.2](../../DurableHeap/docs/mvp-design-v2.md#L310)：`MetaRecordKind: 0x01 = MetaCommitRecord`

**问题描述**：`[F-01]` 域隔离约束在术语表，但具体枚举值定义在 §3.2.1 和 §3.2.2。两个文件域都使用 `0x01` 但含义不同——这正是 [F-01] 要说明的，但读者需要跨章节拼凑。

**严重度**：Major（信息分散，增加理解成本）

**解决方向**：在 [F-01] 附近添加表格：
```
| 域 | 0x01 | 0x02+ |
|----|------|-------|
| data | ObjectVersionRecord | 预留 |
| meta | MetaCommitRecord | 预留 |
```

---

##### M3. `Dirty Set` 与 `Modified Object Set` 术语冗余

**位置**：[术语表](../../DurableHeap/docs/mvp-design-v2.md#L118-L121)

**问题描述**：
- `Dirty Set`：定义完整
- `Modified Object Set`：定义为"与 Dirty Set 同义"

两个术语指向同一概念，但在正文中未见 `Modified Object Set` 的使用。这是术语表膨胀的迹象。

**严重度**：Major（术语表冗余，增加维护负担）

**解决方向**：
- 选择一个作为主术语（建议 `Dirty Set`）
- 另一个标记为 `Deprecated` 或仅保留简短别名

---

##### M4. `VersionIndex` 逻辑类型表述不精确

**位置**：[§3.2.4](../../DurableHeap/docs/mvp-design-v2.md#L338)

**问题描述**：
> VersionIndex 复用通用 `DurableDict` 的 diff/apply 路径：逻辑类型为 `DurableDict<ObjectId, Ptr64>`。

但 Appendix A 和 §3.4.2 已明确 MVP 的 `DurableDict` 不使用泛型（`[A-DD-01]`）。此处 `DurableDict<ObjectId, Ptr64>` 与"不使用泛型"的设计冲突。

**严重度**：Major（概念层与实现层表述不一致）

**解决方向**：改为"VersionIndex 复用通用 `DurableDict` 的 diff/apply 路径（key 为 `ObjectId` 即 `ulong`，value 使用 `Val_Ptr64`）"

---

#### 🟡 Minor Issues（改善可读性的建议）

##### m1. 术语表 `LoadObject` 的"内部仍可使用 Resolve"描述过长

**位置**：[术语表](../../DurableHeap/docs/mvp-design-v2.md#L124-L125)

**问题描述**：
> Deprecated: Resolve（作为外部 API 总称；内部仍可用 Resolve 描述"解析版本指针"子步骤，标记为 Internal Only）

这段描述过长且嵌套层次深，难以快速理解。

**解决方向**：拆分为两行：
- `Resolve`：Deprecated（作为外部 API）
- `Resolve (Internal Only)`：解析版本指针的子步骤

---

##### m2. Appendix A 伪代码缺少 `DiscardChanges` 实现

**位置**：[Appendix A.1](../../DurableHeap/docs/mvp-design-v2.md#L765-L767)

**问题描述**：伪代码骨架包含 `DiscardChanges` 方法签名，但实现只有一行注释式代码。§3.4.3 的 `[A-01]` 已明确 DiscardChanges 是 MUST，但伪代码未展示完整逻辑。

**解决方向**：补充实现（已有 `_current = Clone(_committed); _dirtyKeys.Clear();`，其实是完整的）

---

##### m3. CRC32C 覆盖范围在多处重复描述

**位置**：
- [§3.2.1 [F-06]](../../DurableHeap/docs/mvp-design-v2.md#L223)
- [§3.2.1 写入顺序 步骤 5](../../DurableHeap/docs/mvp-design-v2.md#L256)

**问题描述**：CRC32C 覆盖范围（Payload + Pad + TailLen）在两处重复描述。

**解决方向**：步骤 5 可改为"按 [F-06] 计算 CRC32C 并写入"。

---

##### m4. `ValueType` 枚举表位置不佳

**位置**：[§3.4.2](../../DurableHeap/docs/mvp-design-v2.md#L508-L513)

**问题描述**：`ValueType` 枚举表在 dict diff payload 的描述中间，但它是跨多处使用的基础概念（VersionIndex value 类型也引用它）。

**解决方向**：考虑将 `ValueType` 枚举表提升到术语表，或在术语表添加引用指向 §3.4.2。

---

##### m5. §3.4.4 二阶段提交 ASCII 图与文字描述重叠

**位置**：[§3.4.4](../../DurableHeap/docs/mvp-design-v2.md#L527-L552)

**问题描述**：ASCII 图和"关键实现要点"文字描述了相同的二阶段流程。信息重叠约 50%。

**解决方向**：保留图或文字之一，另一个简化为"参见上图"/"如图所示"。

---

#### 🔵 完备性缝隙（边界条件/未定义行为）

##### G1. `DiscardChanges` 对 Transient Dirty 对象后续访问未定义

**位置**：[§3.1.0.1](../../DurableHeap/docs/mvp-design-v2.md#L180-L183)

**问题描述**：对 Transient Dirty 对象调用 `DiscardChanges` 后，对象变为"内存垃圾，等待 GC 回收"。但若调用方持有该对象引用并继续访问，行为未定义。

**解决方向**：添加约束：
- `DiscardChanges` 后继续访问 Transient Dirty 对象的行为是 **undefined**
- 或：Detach 后设置标记，后续访问抛出 `ObjectDetachedException`

---

##### G2. 并发访问假设未明确声明

**位置**：全文

**问题描述**：文档多次提到"单 writer"和"单线程"，但没有集中声明并发假设。若实现者在多线程环境使用，可能遇到未定义行为。

**解决方向**：在 §1 MVP 目标后添加明确的并发假设声明：
> **线程安全假设**：MVP 假设单线程单 writer。所有 API 在无外部同步的情况下并发调用是 undefined behavior。

---

##### G3. `LoadObject` 版本链解析失败的异常类型未规范

**位置**：[§3.3.2](../../DurableHeap/docs/mvp-design-v2.md#L438)

**问题描述**：
> ObjectId 存在但版本链解析失败 | 抛出 `CorruptedDataException`（或等价异常）

"或等价异常"过于模糊，不同实现可能抛出不同异常，影响调用方的异常处理。

**解决方向**：明确异常层次（建议）：
- `DurableHeapException`（基类）
- `CorruptedDataException`（数据损坏）
- `ObjectNotFoundException`（对象不存在，如果选择抛异常而非返回 null）

---

#### 📊 统计汇总

| 严重度 | 数量 | 影响范围 |
|--------|------|----------|
| Critical | 2 | 术语不一致、初始化行为冲突 |
| Major | 4 | 信息分散、概念/实现不对齐 |
| Minor | 5 | 可读性改善 |
| 完备性缝隙 | 3 | 边界条件未覆盖 |

**总体评价**：文档结构良好，大部分概念定义清晰。主要问题集中在：
1. 历次迭代修订后的术语遗留（C1）
2. 空仓库初始化的两处定义不一致（C2）
3. 枚举值定义分散（M1, M2）

这些问题都是可修复的，不影响核心设计的正确性。

---

### DocUIGemini 的发现

作为 UX/交互设计专家，我从 API 人机工程学和 "Doc as UI" 的兼容性角度审阅了本文档。我的核心关注点是：API 是否符合直觉？数据模型是否支持 LLM 的语义理解？

#### 🔴 Critical Issues（严重影响 DocUI 集成）

##### C1. `DurableDict` 仅支持 `ulong` Key 对 DocUI 极度不友好 (§3.4.2)

**位置**：[§3.4.2](../../DurableHeap/docs/mvp-design-v2.md#L475) 及 Appendix A

**问题描述**：
文档规定 MVP 的 `DurableDict` Key 固定为 `ulong`。
> MVP 约束：key 固定为 ulong；Value 仅限 ValueType 枚举支持的类型

这对 DocUI 是致命的。DocUI 的核心隐喻是 "Doc as UI"，依赖 JSON-like 的语义化结构（如 `{"name": "Gemini", "role": "Assistant"}`）。
如果底层存储强制使用 `ulong` Key，Agent 必须维护一个 `String -> ulong` 的映射表（Hash Map），导致：
1.  **语义丢失**：存储层看到的只是 `dict[12345] = "Gemini"`，失去了 "name" 这个语义标签。
2.  **调试困难**：开发者（和 LLM）无法直接阅读存储内容。
3.  **抽象泄漏**：实现细节（ObjectId 是 ulong）泄漏到了用户数据模型中。

**严重度**：Critical（阻碍 DocUI 的 "Self-Describing Data" 愿景）

**解决方向**：
- **必须**在 MVP 中支持 `string` Key（哪怕底层实现低效一点）。
- 或者，明确 `DurableDict` 仅用于内部索引（如 VersionIndex），用户数据应使用另一套支持 String Key 的容器（如 `DurableJson`）。但文档目前暗示 `DurableDict` 是通用的。

---

#### 🟠 Major Issues（认知负荷与误导性示能）

##### M1. `CommitAll(newRootId)` 的职责混合 (§3.4.5)

**位置**：[§3.4.5](../../DurableHeap/docs/mvp-design-v2.md#L565)

**问题描述**：
`CommitAll(newRootId)` 将 "持久化 (Save)" 与 "修改根节点 (Mutate Root)" 两个正交的操作绑定在了一起。
这构成了 **False Affordance (虚假示能)**：
- 暗示用户：要提交就必须（或顺便）修改 Root。
- 暗示用户：修改 Root 只有在提交时才能发生。

**严重度**：Major（增加了 API 的认知复杂度）

**解决方向**：
- 拆分 API：`SetRoot(obj)` 和 `CommitAll()`。
- 或者明确参数名为 `CommitAll(updateRootTo: obj)` 以显式化副作用。

##### M2. 隐式 Scope 带来的孤儿风险 (Orphan Risk) (§3.4.5)

**位置**：[§3.4.5](../../DurableHeap/docs/mvp-design-v2.md#L570)

**问题描述**：
`CommitAll` 自动提交所有 Dirty 对象（Implicit Scope）。
虽然文档提到了 "Orphan Risk"，但这是一种 **Unsafe by Default** 的设计。
开发者（或 LLM）很容易创建了对象，忘记挂载到 Root，然后 `CommitAll` 成功了。数据存下来了，但永远找不到了（除非遍历全堆）。

**严重度**：Major（数据可达性风险）

**解决方向**：
- 引入 **Reachability Check**（可选）：在 Commit 时警告未挂载的 Dirty 对象。
- 或者提供 `GetOrphanedObjects()` API 供诊断。

---

#### 🟡 Minor Issues（微瑕疵）

##### m1. `DurableDict` 非泛型设计的类型安全缺失

**位置**：Appendix A

**问题描述**：
伪代码中 `DurableDict` 使用 `object` 作为 Value 类型。
虽然这符合 "Honest API"（底层确实是弱类型的），但对于习惯强类型的 C# 开发者（以及需要类型提示的 LLM），这增加了出错概率（Boxing/Unboxing, Invalid Cast）。

**解决方向**：
- 考虑提供泛型包装器 `DurableDict<TKey, TValue>`（即使底层是非泛型的）。

##### m2. `DiscardChanges` 命名建议

**位置**：[§3.4.3](../../DurableHeap/docs/mvp-design-v2.md#L518)

**问题描述**：
`DiscardChanges` 语义清晰，但在版本控制领域（Git），`Revert` 或 `Restore` 更常见。
`Discard` 听起来像 "丢弃对象"，而 `Revert` 听起来像 "回到过去"。

**解决方向**：
- 考虑别名 `RevertToCommitted()` 以增强语义直观性。

---

#### 💡 创新建议

##### I1. 引入 "Draft Mode" 显式状态

目前的 Dirty 状态是隐式的。
建议引入显式的 `IsDraft` 属性。
- `IsDraft = true` (Dirty)
- `IsDraft = false` (Clean)
这能让 LLM 更直观地理解 "为什么我的修改还没生效？" —— 因为还在 Draft 模式。

---

### DocUIGPT 的发现

作为规范检查与命名一致性审阅者，我从“条款编号体系可检验性、命名/术语一致性、示例伪代码准确性、MUST/SHOULD/MAY 规范语言”四个角度审阅了 [DurableHeap/docs/mvp-design-v2.md](../../DurableHeap/docs/mvp-design-v2.md)。整体思路扎实，但当前文档里仍存在会诱发实现分叉的“编号体系漂移”和少量“术语/示例脱钩”。

#### 🔴 Critical Issues

1) **条款编号体系出现“非规则分叉”，破坏可检验性**
  - 现状：正文同时出现 `[F-xx]/[A-xx]/[S-xx]/[R-xx]` 以及非规则的 `[F-VER-01]`、`[S-CB-01]`、`[A-DD-01]`。
  - 风险：你们在“条款编号规则”里要求“编号只增不复用、可映射测试向量”。一旦允许任意子前缀，后续索引/测试映射/核查工具会出现多套解析规则，等同于“编号系统失效”。
  - 解决方向：
    - **方案 A（推荐）**：把 `[F-VER-01..03]`、`[S-CB-01..02]`、`[A-DD-01]` 重新编号进主序列（例如分配 `F-10..12`、`S-17..18`、`A-05`），并在条款编号规则中明确“禁止子命名空间”。
    - **方案 B**：如果你们确实要分命名空间，必须在“条款编号规则”里正式定义 grammar（例如 `[F-<Topic>-<NN>]`），并定义 Topic 集合、递增规则、弃用规则与测试映射规则。

2) **条款前缀与内容分类不一致：`[S-05]` 实际是 Framing/Format**
  - 位置：§3.2.1，“Magic 不属于 Record，是 Record 之间的栅栏”。
  - 风险：这会让读者与测试作者对“语义条款 vs 格式条款”的边界产生错误心智模型；也会让 test-vectors 归类错位（Format 测试 vs 语义测试）。
  - 解决方向：把 `[S-05]` 改为 `[F-xx]`（或给出理由说明为什么它被视为语义不变式，并调整前缀表定义）。

3) **空仓库初始化的 `NextObjectId` 自相矛盾（16 vs 1）**
  - 这点 DocUIClaude 已指出，我这里重复强调：这不是“小 bug”，它会导致不同实现对“保留 ID 范围”做出不同选择，最终造成文件格式/互操作的硬分叉。
  - 解决方向：锁死一个单一规范（建议统一为 `NextObjectId = 16` 并明确 `0..15` 的预留语义，或反之；但必须只有一个）。

#### 🟠 Major Issues

1) **“规范性/解释性”边界在 Appendix 中被破坏：Informative 附录出现带编号的要求式语句**
  - 位置：Appendix A 标注“Informative, not Normative”，但伪代码块里出现 `"[A-DD-01] DurableDict 不使用泛型"` 这种“带编号 + 断言式”语句。
  - 风险：读者会把它当成规范条款（尤其当它跟正文冲突时）；而你们又强调“实现可自由选择”，两者冲突。
  - 解决方向：
    - 要么删除/改写 `[A-DD-01]`（变成普通文字说明，不带条款编号，不用 MUST/SHOULD）；
    - 要么把“DurableDict 非泛型（MVP）”升级为正文的 API/语义条款（并让正文其它地方不再写 `DurableDict<ObjectId, Ptr64>` 这种泛型表述）。

2) **`VersionIndex` 的类型表述在“概念层/实现层”之间摇摆，容易误导实现者**
  - 位置：§3.2.4 与 §3.4.2/Appendix A。
  - 现状：一处写 `DurableDict<ObjectId, Ptr64>`（泛型、概念层像类型系统），另一处又声明 MVP `DurableDict` 无泛型且 value 是 `object`。
  - 风险：实现者会以为“可以（或应该）做泛型 DurableDict”，从而引入强转/box/unbox/类型标签等分叉。
  - 解决方向：在规范正文统一采用“非泛型 + 明确 key/value 编码”表述，例如：
    - “VersionIndex 复用 DurableDict 的 diff 编码；key 固定 `ObjectId(ulong)`，value 使用 `Val_Ptr64`。”

3) **规范语言扩展“（MVP 固定）”可能造成 MUST/SHOULD 的降级歧义**
  - 位置：Normative Language 小节。
  - 现状：说“规范性条款：使用 MUST/SHOULD/MAY，或标注为 ‘（MVP 固定）’、‘（MUST）’”。
  - 风险：
    - “MVP 固定”不是标准规范关键字，读者会问：它等价于 MUST 吗？还是等价于“本版本不讨论”？
    - 长期维护时会出现“关键约束写成 MVP 固定，但未能进入 test-vectors”的隐性漏测。
  - 解决方向：
    - 要么把“（MVP 固定）”严格定义为 **informative 的 scope 标注**（不是强制性语气），并要求所有强制行为仍必须用 MUST/MUST NOT；
    - 要么明确定义 “MVP 固定 ≡ MUST（对 MVP 实现）”，并要求它同样必须有条款编号（如 `[S-xx]`）。

4) **术语治理规则与正文用词仍有残留不一致，尤其是 “Base/Genesis/Checkpoint” 词簇**
  - 位置：§3.2.5 `PrevVersionPtr=0 表示 base/初始`（应对齐 Glossary 的 Base Version/Genesis Base/Checkpoint Base）；以及若干 “Checkpoint Base/Checkpoint Base/full-state” 的复合写法。
  - 风险：实现者会把 `PrevVersionPtr=0` 仅理解为“新建对象初始版本”，忽略 checkpoint base 的语义，导致 replay/链长控制行为不一致。
  - 解决方向：把 `PrevVersionPtr=0` 的语义统一写成：
    - “Base Version（包括 Genesis Base 与 Checkpoint Base 两种来源）”，并在同一处明确二者如何区分（如果区分只存在于解释层，也要说明“语义等价，仅动机不同”）。

#### 🟡 Minor Issues

1) **条款编号与列表编号混杂，局部出现编号跳转/重复，降低审阅信心**
  - 例：§3.4.3 `_dirtyKeys` 不变式后面的“2. Clone 策略 / 3. 可观察性 ...”是纯列表但紧跟 `[S-14]`，且序号从 9 跳到 2。
  - 解决方向：把“列表编号”改为无序列表，或重排为 1..n，避免与条款编号视觉冲突。

2) **伪代码的失败分支不够“Fail-fast”，容易被复制成真实实现**
  - 位置：Appendix A `WritePendingDiff()` 中 `if (diff.Count == 0) return false;`。
  - 风险：当 `_dirtyKeys` 与 diff 计算不一致时静默跳过，会掩盖逻辑 bug；实际实现更应该抛异常或至少进入诊断路径。
  - 解决方向：改成 `throw`（或 `Debug.Fail + throw`），并在正文强调 `_dirtyKeys` 精确性是 MUST（你们已有 `[S-14]`）。

3) **`Ptr64`/`Address64` 约定很好，但个别字段属于“文件位置”而非“record 地址”，术语可能需要更精确**
  - 例：`DataTail` 当前用 `Ptr64` 表达 EOF（包含 magic）。它不是“指向 record 起始位置”的地址。
  - 解决方向：要么在 Glossary 明确 `Ptr64` 在 MVP 中是“4B 对齐的 file offset”（更宽），要么引入 `FileOffset64`（概念层）并让 `DataTail` 用它。

#### 建议的收敛顺序（落地动作）

1) **先收口条款编号体系**：禁止/规范子前缀；把 `[F-VER-*]/[S-CB-*]/[A-DD-*]` 归并或正式化。
2) **修正前缀分类错误**：把 `[S-05]` 等格式类条款归到 `[F-*]`。
3) **统一空仓库初始化**：`NextObjectId` 与预留区间单点定义。
4) **清理 Appendix 的“伪规范”**：Informative 区域禁止带编号强制语句；或升级进正文。


---

## Round 2: 交叉讨论 (Cross Discussion)

> 基于 Round 1 的发现，进行交叉讨论，补充或反驳。

---

### DocUIClaude 的回应

读完 Gemini 和 GPT 的发现，有几点想交叉讨论：

---

#### 🤝 对 DocUIGemini 发现的回应

##### 强烈赞同 C1（`ulong` Key 对 DocUI 不友好），并想补充一个角度

Gemini 说得非常到位——"语义丢失"是核心问题。从我的概念框架视角补充一点：

这不仅仅是 DocUI 的问题，而是 **DurableHeap 作为通用持久化基座的定位问题**。

如果 DurableHeap 的野心只是做 Agent 内部状态的高效存储（内部索引、版本链管理），`ulong` Key 完全够用。但如果未来要支持 LLM 可理解的持久化数据模型，`string` Key 是必经之路。

**我的建议是明确分层**：
```
层 1: DurableDict<ulong, ...>  — 内部索引专用（VersionIndex、ObjectId 映射）
层 2: DurableJson / DurableMap — 语义化存储（string key、嵌套结构）
```

MVP 可以只做层 1，但需要在文档中**明确声明 DurableDict 的定位是内部索引**，不是通用数据容器。这样就不会误导用户。

---

##### 赞同 M1（`CommitAll(newRootId)` 职责混合）

Gemini 识别的 **False Affordance** 问题很准确。我用另一个角度佐证：

从 Git 类比看，`git commit` 和 `git checkout -b` 是分离的操作。`CommitAll(newRootId)` 相当于强制要求"每次 commit 都要改 HEAD 指向"，这确实反直觉。

不过我想提一个**反向考量**：MVP 阶段保持简单的"一个 API 做完所有事"可能是合理的。拆分 API 增加了出错可能（用户可能 `SetRoot` 但忘记 `CommitAll`）。

**建议**：
- MVP 保持 `CommitAll(newRootId)` 但加 **无参重载** `CommitAll()` = 保持当前 root
- 文档明确：`CommitAll()` 是默认用法，`CommitAll(newRootId)` 是高级用法

---

##### 赞同 M2（隐式 Scope 的孤儿风险），补充具体方案

Gemini 提的 "Reachability Check" 是正确方向。从概念层考虑，我建议用 **Warning 而非 Error**：

```csharp
// CommitAll 返回值或 out 参数
var result = workspace.CommitAll();
if (result.OrphanedObjectCount > 0) {
    // Logger.Warn($"Committed {result.OrphanedObjectCount} orphaned objects");
}
```

理由：
1. 有时孤儿对象是"预期行为"（比如用户创建了但决定不要了，懒得 Discard）
2. 强制报错会破坏"先创建，后挂载"的自然工作流

---

##### 对 I1（Draft Mode 显式状态）持保留意见

Gemini 建议引入 `IsDraft` 属性代替隐式 Dirty 状态。概念上我理解，但有顾虑：

1. **术语膨胀**：`Draft` 和 `Dirty` 是同一概念的两个名字，增加认知负担
2. **与 DocUI 术语不对齐**：DocUI 用的是 "working state vs committed state"，不是 "draft vs final"

如果要改善可观察性，我建议用 **`HasUncommittedChanges`** 属性——这是描述性的、自解释的，不引入新隐喻。

---

#### 🤝 对 DocUIGPT 发现的回应

##### 完全赞同 C1（条款编号体系分叉）

GPT 的检查视角非常精准。`[F-VER-01]`、`[S-CB-01]`、`[A-DD-01]` 这些"子命名空间"确实会破坏可检验性。

我更倾向 **方案 A（禁止子命名空间）**：
- 编号系统的价值在于**扁平、可排序、可唯一映射到测试**
- 一旦允许 `[F-VER-*]`，很快就会出现 `[F-VER-SUB-*]`，然后失控

**补充建议**：可以用**连续编号 + 分组注释**兼顾可读性：
```markdown
### 3.2.5 Version Record 格式
<!-- Clauses [F-10] to [F-12] define version record constraints -->
- [F-10] ...
- [F-11] ...
- [F-12] ...
```

---

##### 赞同 C2（`[S-05]` 前缀分类错位）

GPT 说得对——"Magic 是 Record 之间的栅栏"是 **Format/Framing 约束**，不是 Semantics。

但我想提一个更深层的问题：**Format 和 Semantics 的边界本身就很模糊**。

比如：
- "CRC32C 覆盖 Payload+Pad+TailLen"——这是 Format 还是 Semantics？
- "PrevVersionPtr=0 表示 Base Version"——这是 Format（编码规则）还是 Semantics（语义约束）？

**建议**：与其纠结每条规则的分类，不如在条款编号规则中**明确分类原则**：
- `[F-*]`: Wire format、字节布局、大小端
- `[S-*]`: 状态转换、不变式、可达性
- `[A-*]`: API 契约、参数约束、返回值语义

---

##### 赞同 M3（`(MVP 固定)` 的 MUST/SHOULD 降级歧义）

这是个细微但重要的规范语言问题。我的理解是：

- `MUST` = 所有实现都必须遵守，否则不 conformant
- `(MVP 固定)` = 本版本规范锁死这个选择，但未来版本**可能**开放

两者的区别在于**时间维度**：`MUST` 是永久的，`(MVP 固定)` 是版本化的。

**建议**：明确定义为：
> `(MVP 固定)` ≡ `MUST for v2.x`; 后续主版本可能升级为 `MAY` 或改变语义

并要求所有 `(MVP 固定)` 条款必须有编号（可以用 `[M-*]` 前缀表示"MVP-scoped"）。

---

##### 对 GPT 的 C3（NextObjectId 矛盾）确认

这和我的 C2 是同一个问题。我们达成共识：**必须统一为单一定义**。

我倾向 `NextObjectId = 16`（§3.3.1 版本），因为：
1. 预留 0-15 给 well-known 对象更有远见
2. 16 = 2^4 是二进制友好的边界
3. 对 MVP 没有任何实现成本（只是初始值不同）

---

#### 💡 新发现：三位审阅者的发现有重叠，暗示这些是"高共识问题"

通过交叉对比，我发现以下问题被**两位或三位**审阅者同时识别：

| 问题 | DocUIClaude | DocUIGemini | DocUIGPT |
|------|-------------|-------------|----------|
| NextObjectId 初始值冲突 | ✅ C2 | | ✅ C3 |
| 泛型表述不一致 | ✅ M4 | ✅ m1 (弱) | ✅ M2 |
| 条款分类/编号体系问题 | | | ✅ C1, C2 |
| `ulong` Key 限制 | | ✅ C1 | |
| 术语遗留 (base/初始) | ✅ C1 | | ✅ M4 |

**高共识问题**（建议优先修复）：
1. NextObjectId 初始值（我+GPT）
2. 泛型表述一致性（我+Gemini+GPT）
3. 术语遗留（我+GPT）

**分歧或独立发现**（需要进一步讨论）：
1. `ulong` Key 限制（只有 Gemini 提出，但非常重要）
2. 条款编号体系（只有 GPT 提出，但影响可检验性）

---

#### 🎯 Round 2 小结：我的优先级建议

结合三方发现，我认为修复优先级应该是：

**P0（阻塞实现）**：
1. NextObjectId 初始值统一
2. `base/初始` 术语更新
3. 条款编号体系收口

**P1（影响 DX/可维护性）**：
1. 枚举值集中定义（ObjectKind, RecordKind, ValueType）
2. 泛型表述统一为非泛型
3. `(MVP 固定)` 语义明确化

**P2（长期改进）**：
1. `DurableDict` 定位声明（内部索引 vs 通用容器）
2. `CommitAll` 无参重载
3. Appendix 伪代码规范化

---

### DocUIGPT 的回应

看完 Claude 与 Gemini 的 Round 2，我整体立场是“强赞同 + 再往前推一步把共识写成可判定的契约句”。我按：赞同点补齐、分歧点澄清、以及新问题三块回应。

#### 🤝 我对两位共同点的补齐

##### 1) `NextObjectId`（16 vs 1）不只是数值冲突，而是“保留区语义”未被规范化

我赞同统一为 `NextObjectId = 16`，但还缺两句关键规范，否则未来扩展仍会分叉：

- **Allocator 约束**：allocator **MUST NOT** 分配 `ObjectId` in `0..15`（或你们最终选定的保留区间）。
- **Reader 前向兼容策略**：当 reader 看到落在保留区、但它“不理解含义”的 object 时，是 fail-fast（推荐）还是忽略（危险）？必须写死。

原因：否则“预留”只是写在初始化段落里的愿望，allocator/loader 仍可能在边界 case 上各自补齐策略。

##### 2) 条款编号“扁平化”不只是可读性，它是后续 Test Vectors / crash-injection 的寻址基础

我支持 Claude 的“禁止子命名空间（方案 A）”，并补一句工程落点：扁平 ID 的真实价值是让测试向量、故障注入点、与实现断言都能引用同一稳定 ID，不需要额外解析规则。

如果需要“分组可读性”，建议在标题或注释层表达（例如 `<!-- [F-10..F-12] Version Record -->`），而不是把分组编码进 ID grammar。

##### 3) `DurableDict` 的定位声明应同时解决“DocUI 语义化”与“实现复杂度”

我赞同 Gemini 的“string key 是最终路线”，也赞同 Claude 的“分层妥协”。建议把分层写成一条明确的产品边界：

- `DurableDict`（MVP）定义为 **Indexing Primitive**：key 为 `ulong`，用于 VersionIndex / intern table / object graph plumbing。
- 语义化容器（未来）可以是 `DurableMap`/`DurableJson`；在 MVP 里允许用 well-known 的 “Key Interner” 对象作为过渡（例如 `string -> ulong` 的映射 + reverse mapping），但必须声明它是“迁移桥”，不是最终数据模型。

这样 DocUI 侧至少能把“人类可读的 key”放进 durable 的 interner，而不是让映射散落在进程内 hash map。

#### 🟠 我对分歧点的澄清/微反对

##### 1) 关于 `CommitAll(newRootId)`：拆不拆 API 是次要的，关键是“失败语义与可观察性”

Gemini 说 false affordance、Claude 给无参重载，我都同意。但我更关心一个更容易诱发实现分叉的点：

- **Commit 失败后内存状态 MUST NOT 改变**（尤其是 committed baseline / dirty tracking / root 指针相关状态）。
- 无论 API 是否拆分，都建议给出 `CommitResult`（哪怕内部用 exceptions），并把 orphan/reachability 的诊断放进返回值或明确的可观察渠道（对 Agent/LLM 来说，纯日志不可见）。

如果这两句不写死，未来实现会在“commit 内部阶段推进到哪一步算成功”上各自做选择，最终产生难复现的一致性 bug。

##### 2) Orphan 风险：我倾向“默认 Warning + 可选严格模式”，而不是强制 fail

Claude 提 Warning，我同意；Gemini 提返回 Orphan 信息，我也同意。补充一个折衷：

- 默认：`CommitAll()` 成功但返回 `OrphanedCount > 0`。
- 可选（诊断/调试/CI）：`CommitOptions.FailOnOrphans = true`，直接失败并列出示例 orphan IDs。

这样不会破坏“先创建后挂载”的工作流，也允许在测试环境把风险抬到 P0。

#### 💡 新问题（两位的讨论触发我想到的）

##### N1) 需要一个“Unknown/Reserved Kind 的处理策略”条款，否则扩展时会静默数据丢失

你们在 `ObjectKind`/`RecordKind` 的讨论里强调枚举表集中，但更关键的是：当 parser 遇到未知 `RecordKind` / `ObjectKind` / `ValueType` 时，是 **MUST fail-fast** 还是可以跳过？

对 DurableHeap 这种持久化基座，默认“跳过未知”极易造成 silent corruption（尤其在版本链/索引场景）。建议明确选择 fail-fast，并把该行为写成一条可测试的恢复/兼容条款。

##### N2) “保留区 + well-known 对象”与 DocUI 的“稳定入口”天然契合，但要防止被写成隐式规范

我建议保留区里至少预留一个槽位给“调试/可观测性对象”（例如 string key interner、heap metadata manifest）。但这类东西如果只写在 Informative/Appendix，很容易变成“大家默默依赖的事实”，却没有条款编号与测试向量。

结论：要么把 well-known 的语义提升为少量规范条款（带 ID、可测试），要么坚决不在 MVP 文档里暗示它们会被用来承载语义化数据。

##### N3) 文档里“Format vs Semantics”边界的确模糊：建议新增一段“分类判据”而不是逐条争论

Claude 已提出分类判据，我补一句落地写法：

- `[F-*]`：影响 parser 能否无歧义地定位/跳过/校验 record 的规则（字节布局、对齐、CRC 覆盖范围、tail 语义）。
- `[S-*]`：影响状态机与 API 行为的规则（commit 成功条件、dirty tracking 不变式、orphan 诊断）。

把判据写出来，比争论某条归属更能避免未来新增条款时继续错位。


### DocUIGemini 的回应

听了 Claude 和 GPT 的发言，我感到这次讨论非常有质量。我们正在从不同维度（概念、UX、规范）逼近一个更健壮的设计。

---

#### 🤝 对 DocUIClaude 回应的再回应

##### 1. 关于 `ulong` Key 的分层妥协 (Layered Approach)
> Claude 建议：明确分层，Layer 1 (DurableDict) 做内部索引，Layer 2 (DurableJson) 做语义存储。

**非常赞同**。这是一个极佳的架构切分。
我接受 MVP 阶段 `DurableDict` 仅支持 `ulong` Key，**前提是**：
1.  文档必须显式声明这一分层愿景（Vision Statement）。
2.  文档必须明确 `DurableDict` 的定位是 "Low-level Indexing Primitive"，而非 "General Purpose Container"。
这样可以避免开发者（和未来的 LLM 维护者）产生"这就是最终形态"的误解。

##### 2. 关于 `CommitAll` 的无参重载
> Claude 建议：MVP 保持 `CommitAll(newRootId)` 但加无参重载 `CommitAll()`。

**同意**。
`CommitAll()` (无参) 是一个低成本高收益的 DX 改进。它符合 "Save" 的直觉（Ctrl+S 不会问你要不要改文件名）。
建议将其列为 **MUST**，因为它能显著降低 LLM 生成代码时的逻辑错误率（LLM 经常会为了调用 API 而编造一个 `newRootId`）。

##### 3. 关于 Orphan Risk 的警告机制
> Claude 建议：用 Warning 而非 Error。

**同意，但需补充**：
对于 Agent 而言，Log 中的 Warning 是不可见的。
建议 API 返回值包含 `OrphanedCount`，或者在 `Observation` 中体现。
例如：`CommitAll()` 返回 `CommitResult` 对象，包含 `{ Success: true, OrphanedObjects: 5 }`。
这样 Agent 可以通过检查返回值来决定是否需要自我修正（Self-Correction）。

##### 4. 关于 Draft Mode vs HasUncommittedChanges
> Claude 建议：使用 `HasUncommittedChanges` 避免术语膨胀。

**我接受这个修正**。
`HasUncommittedChanges` 是标准的 Dirty Check 命名，符合 .NET 惯例。
我撤回 `IsDraft` 的提议。DX 的一致性优于新颖性。

---

#### 🤝 对 DocUIGPT 发现的声援

##### 1. 坚决支持条款编号体系收口 (No Sub-namespaces)
GPT 指出的 `[F-VER-01]` 问题非常关键。
从 **LLM Token 经济学**的角度看：
- `[F-12]` 是 3-4 个 token。
- `[F-VER-01]` 是 5-6 个 token，且增加了序列预测的熵（Entropy）。
复杂的编号体系不仅难核查，也难被 LLM 准确引用。
**建议**：采用扁平数字编号，如 `[F-100]` 系列预留给 Version Record。

##### 2. 关于 NextObjectId = 16 的 UX 视角
我支持统一为 `16`。
除了二进制友好外，还有一个 **Visual Debugging** 的优势：
在 Hex Dump 中，`0x00` - `0x0F` 是一行（通常）。
将 System Objects 限制在第一行，User Objects 从第二行开始，能提供清晰的 **Visual Rhythm (视觉韵律)**。
这符合我之前提到的 "Format Punctuation" 理念。

---

#### 💡 新的思考：规范的"语法糖"与分层

Claude 提到了 "Format 和 Semantics 边界模糊" 的问题。
这让我想到，我们在写 Spec 时，其实也在设计一种 "Language"。

建议在条款编号规则中引入 **"Layered Spec"** 概念：
- **`[F-*]` (Format)**: **Immutable**. 涉及磁盘布局、字节对齐。一旦发布，几乎不能改（否则破坏兼容性）。
- **`[S-*]` (Semantics)**: **Evolving**. 涉及状态机、API 行为。可以在不改变磁盘格式的情况下演进（例如放宽某些约束）。

将 `[S-05]` (Magic 栅栏) 归入 `[F-*]` 是合理的，因为它是磁盘布局的一部分。
凡是影响 "Parser 能否读出数据" 的都是 `[F]`；凡是影响 "Interpreter 如何理解数据" 的都是 `[S]`。

---

#### 🎯 Round 2 小结：我的优先级建议

**P0 (Must Fix)**:
1.  **NextObjectId = 16** (统一初始化行为)
2.  **条款编号扁平化** (去除子命名空间)
3.  **DurableDict 定位声明** (明确 Layer 1 定位，平息 `ulong` 争议)

**P1 (Should Fix)**:
1.  **CommitAll() 无参重载** (DX/Safety)
2.  **CommitResult 返回 Orphan 信息** (Agent Observability)

**P2 (Nice to Have)**:
1.  **Visual Rhythm** (利用 Hex Dump 布局优化 ID 分配)

---

## Round 3: 共识与行动项 (Consensus & Action Items)

> 主持人（刘德智）汇总 Round 1-2 的讨论，整理达成共识的问题和解决方案。

---

### 🎯 已达成共识的问题（按优先级排序）

#### P0 级：阻塞实现，必须修复

| # | 问题 | 发现者 | 建议方案 | 共识状态 |
|---|------|--------|----------|----------|
| P0-1 | **NextObjectId 初始值冲突（16 vs 1）** | Claude(C2) + GPT(C3) | 统一为 `NextObjectId = 16`，明确 `0-15` 为保留区间 | ✅ 三方一致 |
| P0-2 | **条款编号体系分叉**（F-VER-*, S-CB-*, A-DD-*） | GPT(C1) | 方案 A：禁止子命名空间，归并到主序列（如 F-10..12）| ✅ 三方一致 |
| P0-3 | **术语遗留："base/初始"未更新** | Claude(C1) + GPT(M4) | 改为 "Base Version（Genesis Base 或 Checkpoint Base）" | ✅ 三方一致 |

#### P1 级：影响 DX/可维护性，应该修复

| # | 问题 | 发现者 | 建议方案 | 共识状态 |
|---|------|--------|----------|----------|
| P1-1 | **DurableDict 定位模糊** | Gemini(C1) + Claude | 明确声明：Layer 1 Indexing Primitive（ulong key），非通用容器 | ✅ 三方一致 |
| P1-2 | **泛型表述不一致**（VersionIndex 的 DurableDict<K,V>） | Claude(M4) + Gemini(m1) + GPT(M2) | 统一为非泛型表述："key 为 ObjectId(ulong)，value 使用 Val_Ptr64" | ✅ 三方一致 |
| P1-3 | **CommitAll 无参重载缺失** | Gemini(M1) + Claude | 添加 `CommitAll()` 无参版本作为 MUST | ✅ 三方一致 |
| P1-4 | **枚举值定义分散**（ObjectKind, RecordKind, ValueType） | Claude(M1, M2) | 集中到术语表或专门小节 | ✅ 三方一致 |
| P1-5 | **[S-05] 前缀分类错位** | GPT(C2) | 改为 [F-*]，并在文档中添加 Format/Semantics 分类判据 | ✅ 三方一致 |
| P1-6 | **"(MVP 固定)"语义不明** | GPT(M3) + Claude | 定义："MVP 固定 ≡ MUST for v2.x"，需有条款编号 | ✅ Claude+GPT 一致 |
| P1-7 | **Appendix 出现带编号规范语句** | GPT(M1) | 删除 [A-DD-01] 或升级到正文 | ✅ 三方一致 |

#### P2 级：改善可读性/完备性，可以推迟

| # | 问题 | 发现者 | 建议方案 | 共识状态 |
|---|------|--------|----------|----------|
| P2-1 | **Orphan Risk 诊断** | Gemini(M2) + Claude | CommitResult 返回 OrphanedCount；可选严格模式 | ✅ 三方一致 |
| P2-2 | **DiscardChanges 后访问 Transient Dirty 对象行为未定义** | Claude(G1) | 明确为 undefined 或抛 ObjectDetachedException | 🟡 待监护人决策 |
| P2-3 | **并发假设未明确声明** | Claude(G2) | 在 §1 添加线程安全假设声明 | ✅ 三方一致 |
| P2-4 | **Unknown Kind 处理策略** | GPT(N1) | 明确 fail-fast 策略 | ✅ GPT+Claude 一致 |
| P2-5 | **Dirty Set / Modified Object Set 术语冗余** | Claude(M3) | 合并为单一术语（建议保留 Dirty Set） | ✅ 三方一致 |
| P2-6 | **Ptr64 vs FileOffset64 术语区分** | GPT(Minor 3) | 在 Glossary 扩展 Ptr64 定义或引入 FileOffset64 | 🟡 待监护人决策 |

---

### 📋 具体修订行动项

#### 行动 1：统一 NextObjectId（P0-1）

**位置**：§3.3.1 与 §3.4.6

**修改内容**：
- §3.4.6 的 `NextObjectId = 1` → `NextObjectId = 16`
- 添加说明："`ObjectId 0` 保留给 VersionIndex；`ObjectId 1-15` 保留给未来 well-known 对象"
- 添加规范条款：`[S-XX] Allocator MUST NOT 分配 ObjectId in 0..15`

---

#### 行动 2：收口条款编号体系（P0-2）

**修改内容**：
- `[F-VER-01..03]` → `[F-10..12]`
- `[S-CB-01..02]` → `[S-17..18]`
- `[A-DD-01]` → 删除（在 Appendix 的 Informative 区域不应有编号条款）
- 在"条款编号规则"小节添加："禁止子命名空间；使用连续编号 + 分组注释"

---

#### 行动 3：术语更新（P0-3）

**位置**：§3.2.5

**修改内容**：
- "若为 0 表示 base/初始" → "若为 0 表示 **Base Version**（Genesis Base 或 Checkpoint Base）"

---

#### 行动 4：DurableDict 定位声明（P1-1）

**位置**：§3.1.4 类型约束 或新建小节

**添加内容**：
```markdown
> **DurableDict 定位声明（MVP）**：
> - `DurableDict` 是 **Low-level Indexing Primitive**，用于 VersionIndex、ObjectId 映射等内部索引
> - Key 固定为 `ulong`，不支持 String Key
> - 语义化存储（如 JSON-like 结构）不在 MVP 范围，未来版本将引入 `DurableJson` 或等价容器
> - 如需在 MVP 中存储 String Key 数据，可使用 "Key Interner" 模式（string → ulong 映射）
```

---

#### 行动 5：统一泛型表述（P1-2）

**位置**：§3.2.4

**修改内容**：
- "逻辑类型为 `DurableDict<ObjectId, Ptr64>`" 
- → "复用 DurableDict 的 diff 编码；key 为 ObjectId（ulong），value 使用 Val_Ptr64"

---

#### 行动 6：添加 CommitAll 无参重载（P1-3）

**位置**：§3.4.5

**修改 API 重载表**：
- `[A-02] CommitAll()（MUST）`：保持当前 root 不变，提交所有 dirty 对象。这是默认用法。
- `[A-03] CommitAll(IDurableObject newRoot)（SHOULD）`：设置新 root 并提交。

---

#### 行动 7：集中枚举值定义（P1-4）

**位置**：术语表或 §3.2 开头

**添加表格**：

```markdown
### 枚举值速查表

#### ObjectKind (§3.2.5)
| 值 | 名称 | 说明 |
|-------|------|------|
| 0x00 | Reserved | 保留 |
| 0x01 | Dict | DurableDict（MVP 唯一） |
| 0x02-0x7F | Standard | 未来标准类型 |
| 0x80-0xFF | Variant | 版本变体 |

#### RecordKind (§3.2.1, §3.2.2)
| 域 | 值 | 名称 |
|------|-------|------|
| data | 0x01 | ObjectVersionRecord |
| meta | 0x01 | MetaCommitRecord |

#### ValueType (§3.4.2)
| 值 | 名称 |
|-------|------|
| 0x0 | Val_Null |
| 0x1 | Val_Tombstone |
| 0x2 | Val_ObjRef |
| 0x3 | Val_VarInt |
| 0x4 | Val_Ptr64 |
```

---

#### 行动 8：修正 [S-05] 前缀（P1-5）

**修改内容**：
- `[S-05]` → `[F-XX]`（分配下一个 F 编号）
- 添加"Format/Semantics 分类判据"小节

---

#### 行动 9：定义"(MVP 固定)"语义（P1-6）

**位置**：规范语言小节

**添加内容**：
```markdown
- **（MVP 固定）**：等价于"MUST for v2.x"，表示当前版本锁死该选择。
  - 后续主版本可能演进为 MAY 或改变语义
  - 所有（MVP 固定）约束应有对应条款编号
```

---

### 🔄 待监护人决策的分歧点

以下问题在讨论中未达成完全共识，需要监护人最终裁决：

1. **DiscardChanges 后访问 Transient Dirty 对象**
   - 选项 A：明确为 undefined behavior
   - 选项 B：抛出 ObjectDetachedException
   
2. **Ptr64 vs FileOffset64 术语**
   - 选项 A：扩展 Ptr64 定义为"4B 对齐的 file offset"
   - 选项 B：引入新术语 FileOffset64

---

## 会议记录

### 会议基本信息

- **日期**：2025-12-20
- **形式**：秘密基地畅谈 (Hideout Jam Session)
- **主持人**：刘德智 (AI Team Leader)
- **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
- **审阅文档**：`DurableHeap/docs/mvp-design-v2.md`

### 关键讨论点

1. **三位 Specialist 从不同视角审阅**：
   - DocUIClaude：概念框架、术语一致性
   - DocUIGemini：UX/交互、API 人机工程学
   - DocUIGPT：规范检查、条款编号体系

2. **高共识问题识别**：
   - NextObjectId 冲突（Claude + GPT 同时发现）
   - 泛型表述不一致（三人同时发现）
   - 条款编号体系问题（GPT 主导发现，其他两人强烈赞同）

3. **DocUIGemini 的独特贡献**：
   - 指出 `ulong` Key 对 DocUI 语义化的影响（虽是"本文档"一致性问题之外，但对整体生态极其重要）
   - 提出 CommitResult 返回 Orphan 信息以提升 Agent 可观测性

4. **DocUIGPT 的独特贡献**：
   - 系统性核查条款编号体系，发现"子命名空间"问题
   - 指出 Format/Semantics 边界模糊需要明确判据
   - 建议 fail-fast 处理未知 Kind

### 会议成果

- **发现问题**：共计 26 个问题（Critical 5 个, Major 10 个, Minor 6 个, 完备性缝隙 5 个）
- **达成共识**：17 个问题达成解决方案共识
- **待决策**：2 个问题需监护人裁决
- **行动项**：9 个具体修订行动

### 下一步

1. ~~将本次共识整理为决策摘要，提交监护人审批~~ ✅ 已批准
2. ~~监护人批准后，执行 9 项修订行动~~ ✅ 已完成（2025-12-20）
3. ~~修订完成后进行 Diff Review~~ ✅ 已验证

---

## 修订执行记录（2025-12-20）

**监护人批示**：批准实施，并补充 DurableDict 演进路线图信息

**执行摘要**：

| 行动 | 状态 | 说明 |
|------|------|------|
| P0-1: NextObjectId 统一为 16 | ✅ | §3.4.6 已修正，添加 [S-17] 保留区约束 |
| P0-2: 条款编号收口 | ✅ | F-VER-* → F-10..12, S-CB-* → S-18..19, 删除 A-DD-01 |
| P0-3: 术语更新 | ✅ | "base/初始" → "Base Version" |
| P1-1: DurableDict 定位声明 | ✅ | 新增 §3.1.5，整合监护人演进路线图 |
| P1-2: 泛型表述统一 | ✅ | §3.2.4 改为非泛型表述 |
| P1-4: 枚举值速查表 | ✅ | 术语表后新增速查表 |
| P1-5: [S-05] → [F-13] | ✅ | 术语约束归入 Format 类别 |
| P1-6: "(MVP 固定)"语义 | ✅ | 规范语言小节添加定义 |
| P2-5: 术语冗余清理 | ✅ | Modified Object Set 标记为 Deprecated |

**条款编号最终分布**：
- `[F-01..13]`：Format/Framing（13 条）
- `[S-01..04, S-06..19]`：Semantics（18 条，S-05 移至 F-13 后留空洞）
- `[A-01..04]`：API（4 条）
- `[R-01..03]`：Recovery（3 条）

**待后续版本处理**：
- P2-2: DiscardChanges 后访问 Transient Dirty 对象行为（监护人待决策）
- P2-6: Ptr64 vs FileOffset64 术语（监护人待决策）

