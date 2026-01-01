# 秘密基地畅谈会：DurableHeap MVP v2 设计审阅

> **日期**：2025-12-20
> **主持人**：刘德智 / SageWeaver（DocUI 规范起草委员会执行委员会主席）
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
> **审阅对象**：`DurableHeap/docs/mvp-design-v2.md`
> **目标**：确保文档自洽、一致、简洁

---

## 会议背景

本次审阅针对 DurableHeap MVP v2 设计文档，该文档：
- 尚无实现，无数据，无兼容性包袱
- 包含详尽的术语表、决策矩阵和磁盘布局规范
- 目标是形成"可开工规格"

审阅聚焦三个维度：
1. **自洽性**：文档内部各部分是否逻辑一致
2. **一致性**：术语使用是否全文统一
3. **简洁性**：是否存在冗余或可精简之处

---

## 第一轮审阅

### DocUIClaude 审阅意见

> **审阅视角**：概念框架、术语一致性、概念完备性、逻辑自洽
> **审阅时间**：2025-12-20

#### 发现的问题

| # | 问题类型 | 严重度 | 描述 | 位置（章节） |
|---|----------|--------|------|--------------|
| 1 | 术语一致性 | Major | `Checkpoint Version` 与 `base` 术语混用 | 术语表 + 4.1.4 + 4.2.4 |
| 2 | 概念完备性 | Major | 术语表遗漏 `Commit point` 概念定义 | 术语表 |
| 3 | 术语一致性 | Minor | `_dirtyKeys` vs `Dirty Set` 概念层级混淆 | 术语表 + 4.4.3 |
| 4 | 概念完备性 | Minor | `RecordKind` 枚举值表在正文中分散 | 4.2.1 + 4.2.2 + 4.2.5 |
| 5 | 逻辑自洽 | Minor | `ValueType` 枚举与"MVP 支持的类型"表不完全对齐 | 4.1.4 + 4.4.2 |
| 6 | 术语一致性 | Trivial | `ObjectVersionRecord` vs `ObjectVersion Record` 空格不一致 | 多处 |
| 7 | 概念完备性 | Major | `Identity Map` 与 `Dirty Set` 的生命周期/清理时机未明确 | 4.1.2 + 4.3.2 |
| 8 | 逻辑自洽 | Minor | `PrevVersionPtr=0` 语义双重用途未显式区分 | 4.2.4 + 4.4.6 |
| 9 | 概念完备性 | Trivial | `Deserialize` 定义了但术语表未收录 | 术语表 + 4.1.0 |
| 10 | 术语一致性 | Major | `LoadObject` vs `Resolve` 边界模糊 | 术语表 + 4.1.0 |

#### 详细说明

**#1 `Checkpoint Version` 与 `base` 术语混用 [Major]**

术语表明确定义了 `Checkpoint Version`（全量状态版本，PrevVersionPtr=0），但正文多处仍出现 `base` 一词：
- 4.2.4："周期性 base / materialize 缓存"
- 4.1.0 Materialize 定义："将 base state 与若干 overlay/diff 合并"
- 4.4.2："从空 dict apply 后得到当前全量 state"

建议统一用 `Checkpoint Version` 或在术语表中将 `base` 定义为 `Checkpoint Version` 的别名。

**#2 术语表遗漏 `Commit point` 概念定义 [Major]**

文档中多次提到 `commit point`（2.2 节标题、4.2.2 "提交点" 等），但术语表未收录。`Commit point` 是理解崩溃恢复语义的关键概念，应给予正式定义。

建议定义：`Commit point` — 对外可见的版本持久化边界；在 meta file 方案中，等于 `MetaCommitRecord` 成功落盘的时刻。

**#3 `_dirtyKeys` vs `Dirty Set` 概念层级混淆 [Minor]**

术语表定义了 `Dirty Set`（Workspace 级别的 dirty 对象强引用集合），但 4.4.3 和 4.4.4 大量使用 `_dirtyKeys`（对象级别的 dirty key 集合）。

这是两个不同层级的概念：
- `Dirty Set`：Workspace 级，记录哪些**对象**是 dirty
- `_dirtyKeys`：对象级，记录该对象内哪些 **key** 发生变化

建议：
- 在术语表增加 `DirtyKeys`（或 `Dirty Key Set`）条目，明确其与 `Dirty Set` 的层级关系
- 或在 4.4.3 首次出现时加入显式澄清

**#4 `RecordKind` 枚举值表分散 [Minor]**

`RecordKind` 在术语表定义为"Record 的顶层类型标识"，但其枚举值分散在多处：
- 4.2.1："`0x01`：`ObjectVersionRecord`"
- 4.2.2："MVP 固定为 `0x01` 表示 `MetaCommitRecord`"

建议在术语表或独立小节提供完整的 `RecordKind` 枚举值表，作为实现参照。

**#5 `ValueType` 枚举与"MVP 支持的类型"表不完全对齐 [Minor]**

4.1.4 的类型约束表列出 MVP 支持的值类型：`null`, `varint`（整数）, `ObjRef(ObjectId)`, `Ptr64`。

但 4.4.2 的 `ValueType` 枚举定义了 5 种：`Val_Null`, `Val_Tombstone`, `Val_ObjRef`, `Val_VarInt`, `Val_Ptr64`。

`Val_Tombstone` 是内部编码细节，不是"用户可写入的值类型"，但两处的表述容易引起混淆。

建议：
- 在 4.1.4 的表中注明"编码层另有 `Tombstone` 用于表达删除语义"
- 或将 `ValueType` 分为"用户值类型"和"内部值类型"两个子集

**#6 `ObjectVersionRecord` 空格不一致 [Trivial]**

多数场合使用 `ObjectVersionRecord`（无空格），但 4.2.5 标题写作 "ObjectVersionRecord（对象版本，增量 DiffPayload）"时在括号内使用了"对象版本"的说法，与术语表用词一致，但建议检查全文确保复合术语书写统一。

**#7 `Identity Map` 与 `Dirty Set` 的生命周期/清理时机未明确 [Major]**

术语表定义了 `Identity Map`（WeakReference 映射）和 `Dirty Set`（强引用集合），但以下问题未明确：
- `Dirty Set` 的对象何时移出？（Commit 成功后？DiscardChanges 后？）
- `Identity Map` 的条目何时清理？（依赖 GC？主动清理？）
- 如果对象同时在 `Identity Map` 和 `Dirty Set` 中，Commit 成功后是否从两者都移除？

4.3.2 提及"Dirty Set 的规则：对象第一次变 dirty 时加入；commit 成功后移除并清空 ChangeSet"，但这是在"对象生命周期与 WeakReference 约束"小节，不是正式定义。

建议在术语表补充生命周期说明，或在 4.1 增加专门的"对象生命周期"小节。

**#8 `PrevVersionPtr=0` 语义双重用途 [Minor]**

`PrevVersionPtr=0` 在两个场景使用：
1. **Checkpoint Version**：周期性写入的全量状态版本，用于封顶 replay 成本
2. **新建对象首版本**：对象的创世版本，本质上也是"from-empty"

4.4.6 明确指出"wire format 与 Checkpoint Version 相同，但概念上是'创世版本'"。

虽然实现上确实相同，但概念上是两个不同用途。建议：
- 在术语表明确 `PrevVersionPtr=0` 的双重语义
- 或引入区分标记（如 `IsCheckpoint` flag），虽然 MVP 可能不需要

**#9 `Deserialize` 定义了但术语表未收录 [Trivial]**

4.1.0 详细定义了四个读取阶段概念：`Deserialize`, `Materialize`, `LoadObject`, `ChangeSet`。其中：
- `Materialize` 未被术语表收录（在 `LoadObject` 定义中被引用）
- `Deserialize` 完全未被术语表收录

虽然这些是内部实现概念，但既然 4.1.0 专门定义了它们，建议在术语表"载入与缓存"分组下增加条目，保持术语注册完整性。

**#10 `LoadObject` vs `Resolve` 边界模糊 [Major]**

术语表定义：
- `LoadObject`："按 HEAD 取版本指针并 materialize，返回可写对象实例"
- 弃用："Deprecated: Resolve（作为外部 API 总称）"

但 4.1.0 又说："备注：内部仍可使用 `Resolve` 描述'解析版本指针'的子步骤。"

这产生了混淆：
- 对外 API：`LoadObject`（正确）
- 对内子步骤：`Resolve`（可用，但与弃用声明矛盾）

建议：
- 明确 `Resolve` 的弃用范围（仅弃用"作为外部 API 总称"，内部使用保留）
- 或在术语表增加 `Resolve` 的精确定义，标注"Internal Only"

#### 初步建议

1. **P0（阻塞开工）**：
   - 补充 `Commit point` 术语定义
   - 明确 `Identity Map` 与 `Dirty Set` 的生命周期
   - 澄清 `LoadObject` vs `Resolve` 的使用边界

2. **P1（建议在开工前完成）**：
   - 统一 `Checkpoint Version` / `base` 术语
   - 明确 `_dirtyKeys` 与 `Dirty Set` 的层级关系
   - 集中 `RecordKind` 枚举值表

3. **P2（可延后）**：
   - 补充 `Deserialize`, `Materialize` 术语表条目
   - 对齐 `ValueType` 枚举与用户类型表
   - 检查复合术语空格一致性

4. **疯狂想法**：
   - 考虑为术语表增加"概念层 / 编码层 / 实现层"分类标签，帮助读者理解术语的抽象层级
   - `PrevVersionPtr=0` 的双重语义可以通过"概念合并"来简化：无论是 Checkpoint 还是创世，本质都是"无前序依赖的完整状态"

---

### DocUIGemini 审阅意见

> **审阅视角**：UX/交互设计、可实现性、API 可发现性
> **审阅时间**：2025-12-20

#### 发现的问题

| # | 问题类型 | 严重度 | 描述 | 位置（章节） |
|---|----------|--------|------|--------------|
| 1 | API Usability | Minor | `CommitAll(newRootId)` 强制传递 `newRootId`，缺乏"保留当前 Root"的便捷重载 | 4.4.5 |
| 2 | API Naming | Minor | `DurableDict.Delete` 命名偏离 C# `IDictionary.Remove` 惯例 | 4.4.4 |
| 3 | UX/Safety | Major | `DiscardChanges` 目前标记为 SHOULD，建议升级为 MUST | 4.4.3 |
| 4 | UX/Visibility | Minor | 缺乏获取当前 Dirty Set 状态的 API，违背"系统状态可见性"原则 | 4.1.2 / 4.4.5 |
| 5 | Implementability | Minor | `Magic` 作为分隔符时，未明确 Payload 恰好包含 Magic 序列时的处理预期（虽有 Length 保护） | 4.2.1 |

#### 详细说明

**#1 `CommitAll(newRootId)` 强制传递 `newRootId` [Minor]**

当前设计中 `CommitAll` 需要显式传入 `newRootId`。在很多场景下（如仅保存数据变更，不切换 Root），用户可能只想调用 `Commit()`。
强制用户传入 `currentRootId` 增加了认知负担，且容易传错（例如传了旧的 ID）。
**建议**：提供 `CommitAll()` 无参重载，默认使用当前 RootId；或者将 API 拆分为 `Commit()` 和 `UpdateRoot(id)`（虽然原子性会有所不同，需权衡）。

**#2 `DurableDict.Delete` 命名偏离 C# 惯例 [Minor]**

4.4.4 伪代码中使用 `Delete(key)`。在 C# `IDictionary<K,V>` 接口中，标准动词是 `Remove(key)`。
使用 `Delete` 会导致 **Metaphor Leakage**（隐喻泄漏）：让用户感觉到"我在操作数据库"而不是"我在操作字典"。
**建议**：统一使用 `Remove`，保持 In-Memory Dictionary 的隐喻一致性。

**#3 `DiscardChanges` 建议升级为 MUST [Major]**

4.4.3 将 `DiscardChanges` 列为 "SHOULD"。
从 UX 角度看，`DiscardChanges` 是唯一的 **Undo Affordance**（撤销示能）。
在 "Implicit ChangeSet" 模式下，用户很容易无意中弄脏对象。如果没有 `DiscardChanges`，用户唯一的恢复手段是重启进程，这体验极差。
**建议**：将其升级为 MUST，作为 MVP 的核心安全网。

**#4 缺乏获取当前 Dirty Set 状态的 API [Minor]**

文档定义了 `Dirty Set`，但未提及是否对外暴露。
根据 Nielsen 的 **Visibility of System Status** 原则，用户需要知道"如果我现在 Commit，会提交哪些对象？"。
**建议**：在 Heap 层面提供 `GetDirtyObjects()` 或 `IsDirty(objectId)` 的只读视图。

**#5 `Magic` 分隔符与 Payload 冲突的预期 [Minor]**

虽然 `Len` + `CRC32C` 提供了强保护，但文档应明确：Payload 内部包含 `Magic` 字节序列是允许的，Reader 依靠 `Len` 跳过 Payload，仅在 Resync 时才搜索 `Magic`。
这能消除实现者的疑虑：是否需要对 Payload 中的 Magic 进行转义（Escape）？答案显然是不需要，但文档应显式确认。

#### 对 DocUIClaude 发现的评论

*   **同意 #10 (`LoadObject` vs `Resolve`)**：从 UX 视角看，`Resolve` 听起来像是指针解引用（底层机制），而 `Load` 听起来像 IO 操作（用户意图）。区分二者非常重要。建议对外统一用 `Load`，对内保留 `Resolve` 描述指针解析过程。
*   **同意 #1 (`Checkpoint` vs `base`)**：`base` 在 Git 语境下通常指 `merge base`，容易混淆。`Checkpoint` 更准确地传达了"存档点/截断点"的含义。

#### 初步建议

1.  **API 调整**：
    *   将 `Delete` 重命名为 `Remove`。
    *   增加 `CommitAll()` 重载（复用当前 Root）。
    *   在 `DurableHeap` 类上增加 `IEnumerable<ObjectId> GetDirtyObjects()`。

2.  **规范升级**：
    *   将 `DiscardChanges` 设为 MVP 必须实现。
    *   在 4.2.1 显式说明 "Payload 可包含 Magic 序列，无需转义"。

3.  **文档修辞**：
    *   在 4.4.4 伪代码中，让 `DurableDict` 实现 `IDictionary<K,V>` 接口（或部分实现），以增强"它是字典"的心理模型。

---

### DocUIGPT 审阅意见

> **审阅视角**：规范检查（可实现性/可验证性）、命名约定、文档格式、代码示例准确性
> **审阅时间**：2025-12-20

#### 发现的问题

| # | 问题类型 | 严重度 | 描述 | 位置（章节） |
|---|----------|--------|------|--------------|
| 1 | 逻辑自洽 / 崩溃恢复 | Major | `RBF reverse scan` 的“空文件边界”判定与 `RecordEnd/MagicPos` 定义不一致，存在 off-by-4 风险 | 4.2.1（反向扫尾不变量） |
| 2 | 规范一致性 / 概念定义 | Major | `Dirty Set` 被描述为 `Set<ObjectId>` 且“强引用”，但 ObjectId 无法阻止对象被 GC；与 4.3.2 的动机（防丢改动）矛盾 | 4.1.0（术语与读取阶段）+ 4.3.2 |
| 3 | 可实现性 / API 语义 | Major | `RecordKind` 在 data/meta 两域都使用 `0x01`，但未声明是“按文件域隔离”的枚举，易导致实现者误用同一枚举表 | 4.2.1 + 4.2.2 + 4.2.5 |
| 4 | 数值/枚举完整性 | Major | “MVP 支持的整数类型包含 `ulong`”，但值编码只有 `Val_VarInt`（ZigZag），无法无损覆盖 `ulong` 全域；需补 `Val_VarUInt` 或收紧类型表 | 4.1.4 + 4.2.0.1 + 4.4.2 |
| 5 | 代码示例准确性 | Major | `DurableDict<K,V>` 伪代码在类型系统上不可编译/不成立：`HashSet<ulong>` + `(ulong)(object)key`、`Comparer<K>.Default`、未定义的 `DiffEntry`/`IRecordWriter`，且与“key 固定为 ulong”的 MVP 取舍冲突 | 4.4.4 |
| 6 | 规范语言 | Major | 文中大量使用 MUST/SHOULD，但未声明采用 RFC 2119/8174 语义；缺少“Normative Language”小节会让核查与实现一致性变弱 | 4.4.3（不变式）及全文 |
| 7 | 命名约定 / 术语边界 | Minor | `RBF framing` 作为术语被频繁使用但未在术语表注册；“framing/record separator/frame separator”表述混用，建议 SSOT 统一 | 2.6 Q20 + 4.2.1 |
| 8 | 命名约定 / 一致性 | Minor | 概念层 `Address64` / `ObjectVersionPtr` 与编码名 `Ptr64` 在正文中交替出现，读者难以判断何处是“语义类型”何处是“线格式名” | 术语表 + 4.2.1 + 4.2.5 |
| 9 | 文档结构 | Minor | “决策表”内含被后续决策覆盖/作废的条目（例如 Q4 被 Q16 覆盖），但没有“Superseded/Deprecated Question”标记规则，降低可追溯性 | 3（决策表） |
| 10 | 格式/可查找性 | Minor | “关键常量/编码表”分散：`Magic`、`RecordKind`、`ObjectKind`、`ValueType` 的值域与域隔离规则没有集中表格，影响实现与测试向量编写 | 4.2.1/4.2.2/4.2.5/4.4.2 |

#### 详细说明

**#1 `RBF reverse scan` 的空文件边界不一致 [Major]**

4.2.1 的反向扫尾不变量写到：
- 初始 `MagicPos = FileLength - 4`（尾部分隔符位置）。
- 定义 `RecordEnd = MagicPos`（分隔符 Magic 的起始位置）。
- 但随后写“若 `RecordEnd == 4` 表示没有任何 Record（文件仅 `[Magic]`）”。

在“空文件为 `[Magic]`”的约束下：`FileLength = 4`，所以 `MagicPos = 0`，进而 `RecordEnd = 0`。此时正确的“无 record”判定应当是 `RecordEnd == 0`（或等价写法）。

这不是纯文案问题：reverse scan 是恢复/定位 HEAD 的关键路径，off-by-4 会直接导致读到错误的 `TailLen/CRC32C` 位置，进而触发错误的 resync 行为，最坏会把“尾部真实最后有效记录”判定为损坏而丢失。

**建议修复**：统一定义“`MagicPos` 指向 Magic 起始”还是“指向 Magic 末尾”，二者选其一并通篇一致；我建议保持目前描述（起始），然后把无 record 判定改为 `RecordEnd == 0`。

**#2 `Dirty Set` 的强引用语义与类型不一致 [Major]**

文档两处对 Dirty Set 的表述互相打架：
- 4.1.0 把 Dirty Set 写成 `Set<ObjectId>` 且称“强引用”。
- 4.3.2 解释 Dirty Set 的必要性是“避免 dirty 对象被 GC 回收导致丢改动”。

若 Dirty Set 只存 `ObjectId`，它不会持有对象实例的强引用，无法达成防 GC 的目标；因此 4.3.2 的论证在实现上落不了地。

**建议修复**（二选一，需写死）：
1) Dirty Set 存“对象实例”的强引用（例如 `HashSet<IDurableObject>` 或 `Dictionary<ObjectId, IDurableObject>`）。
2) 如果坚持只存 `ObjectId`，那就必须引入另一条强引用机制（例如 workspace 的 `LiveObjects` 表），并在文档中明确 GC 约束与对象可达性模型。

**#3 `RecordKind` 值域的“文件域隔离”未写死 [Major]**

当前 data 与 meta 的 payload 都以 `RecordKind=0x01` 表示各自唯一记录类型，但文档未明确 `RecordKind` 的值域是否按文件域隔离。

对实现者来说，最自然的实现是一个全局 `enum RecordKind : byte`；若这么做，就会出现 `0x01` 同时表示 `ObjectVersionRecord` 与 `MetaCommitRecord` 的冲突。

**建议修复**：
- 引入 `DataRecordKind` 与 `MetaRecordKind` 两个枚举（或 `RecordKind` + `RecordDomain`），并在术语表把“域隔离”写成 MUST。

**#4 `ulong` 值类型与 `Val_VarInt` 编码不匹配 [Major]**

4.1.4 宣称整数支持 `int/long/ulong`；但 4.2.0.1 对 `varint` 的定义是 ZigZag（有符号），而 4.4.2 的 `ValueType` 只有 `Val_VarInt`，没有 `Val_VarUInt`。

结论：在当前规格下，`ulong` 作为“值”无法无损编码（尤其是 $> 2^{63}-1$ 的值）。

**建议修复**（二选一）：
1) 增加 `Val_VarUInt`（低 4 bit 新枚举值），并规定其 payload 为 `varuint`；
2) 将“值类型整数”收敛为 `long`（或 `varint` 有符号），把 `ulong` 仅保留给 Dict key / ObjectId / 计数等“无符号语义字段”。

**#5 伪代码“看起来像 C#，但无法成立” [Major]**

4.4.4 的代码块会被读者当作“接近可用的 reference implementation”，但它同时：
- 声称泛型 `K`，却硬编码 `_dirtyKeys: HashSet<ulong>` 并做 `(ulong)(object)key` 强转；
- `Comparer<K>.Default` 对 `K` 的可比较性没有约束；
- `DiffEntry`/`IRecordWriter`/`WriteDiffTo` 未定义（可以是伪代码，但需要显式标注为 pseudo + 接口约束）；
- 与第 2.7 节 Q23 的“key 固定为 `ulong`”取舍冲突：既然 MVP 只支持 `ulong` key，更建议直接把示例写成 `DurableDict`（非泛型）或 `DurableDict<TValue>`。

**建议修复**：把示例拆成两层：
- 规范层：给出 method signatures + 不变式；
- 示例层：给出“可编译的 MVP 版 `DurableDict`（key=ulong）”或清晰标注“这是伪代码，不保证可编译”。

**#6 MUST/SHOULD 缺少规范语义声明 [Major]**

文档已开始承担“可开工规格”的角色，并使用 MUST/SHOULD 表达强约束；但没有声明这些词的严格含义与优先级（是否采用 RFC 2119/8174）。

缺这一节会使：
- 评审/测试无法判定“违反 SHOULD 是否可接受”；
- 不同实现可能把 MUST/SHOULD 当口语，导致兼容性与恢复语义分叉。

**建议修复**：增加一个简短小节：
> 本文使用 RFC 2119/8174 的关键字（MUST/SHOULD/MAY…），并说明“SHOULD 的可偏离条件必须在实现文档中明确说明”。

**#7 `RBF`/framing/分隔符术语未收口 [Minor]**

“RBF framing”与“Record Separator / frame separator”在正文作为准术语使用，但未在术语表注册，不利于全文 SSOT。

建议把 4.2.1 的 framing 作为一个正式术语块（例如 **RBF Framing**：`[Magic][Len][Payload][Pad][Len][CRC32C][Magic]`），并把“Magic 不属于 record”作为 MUST 不变式写入。

**#8 `Address64`/`Ptr64`/`ObjectVersionPtr` 的层次标注不足 [Minor]**

术语表里试图区分“语义名 vs 编码名”，但正文仍频繁混用 `Ptr64`（编码）与 `Address64/ObjectVersionPtr`（语义），读者很难判断某段是在定义 wire-format 还是在定义概念。

建议固定写法：
- 概念层一律用 `Address64/ObjectVersionPtr/VersionIndexPtr/DataTail`；
- 编码层在首次出现时标注一次：`Ptr64 := u64 LE byte offset (4B aligned; 0=null)`，之后仅在“线格式/编码规范”小节使用 `Ptr64`。

**#9 决策表的“作废题”缺少机制 [Minor]**

决策表里出现“已被 Q16 覆盖”的备注，这很好，但缺少统一机制：
- 如何标识 Superseded？
- 是否需要把旧选择置空？
- 后续读者如何快速得知“应以哪个决策为准”？

建议引入一列 `状态`（Active/Superseded）或在 ID 前加标记（例如 `Q4 (Superseded by Q16)`），保持可追溯同时减少误读。

**#10 常量/枚举表需要集中 [Minor]**

实现者最需要的是“一页纸的编码表 + 测试向量入口”。目前 `Magic`、record kinds、object kinds、value types 分散在 4.2 与 4.4 多节。

建议新增“Appendix A：Encoding Tables”集中列出：
- `data Magic=DHD3`，`meta Magic=DHM3`
- `DataRecordKind` 值表
- `MetaRecordKind` 值表
- `ObjectKind` 值表
- `ValueType` 值表（含 reserved bits 规则）

#### 对前两位顾问发现的评论

- 对 DocUIClaude：
   - 同意 #1/#2/#7/#10 的方向，并建议把“`Commit point` 定义 + reverse scan 算法 + DataTail/EOF 包含关系”合并成一个“Crash-Recovery Contract”小节，以便做端到端验证。
   - 补充：除“Checkpoint vs base”外，`RecordKind` 的域隔离与 `Dirty Set` 的强引用语义是更容易在实现中踩坑的 P0。

- 对 DocUIGemini：
   - 同意 #2（`Delete`→`Remove`）与 #4（Dirty Set 可见性 API）。
   - 对 #5（Payload 含 Magic 的处理）：建议把它上升为一个明确的规范句：**Payload MAY contain Magic bytes; reader MUST rely on Len to skip; resync MUST verify Len/CRC32C**。这样可以直接指导实现与测试。
   - 补充：即便有 `CommitAll()` 重载，仍建议保留 `CommitAll(newRootId)` 以表达“切换 root”的显式意图；但需要一条“无参即沿用当前 root”的规范。

#### 初步建议

1. **P0（开工前必须修）**：修正 reverse scan 空文件判定；把 `Dirty Set` 设计写死为“强引用对象实例”；明确 `RecordKind` 域隔离。
2. **P1（强烈建议修）**：解决 `ulong` value 编码缺口（新增 `Val_VarUInt` 或收紧支持类型）；将 4.4.4 伪代码改为可编译 MVP 版本或显著标注“伪代码”。
3. **P1（文档硬质量）**：增加 “Normative Language (RFC2119/8174)” 小节；新增 Appendix 集中枚举表/常量表。
4. **P2（可后置）**：统一 `RBF framing` 作为正式术语并收口 framing/分隔符措辞；给决策表增加 Superseded 机制。

---

## 第一轮问题汇总与讨论

### 主持人汇总（刘德智）

三位顾问共发现 **20 个问题**，按严重度分布：
- **Major（阻塞开工）**：11 个
- **Minor**：7 个
- **Trivial**：2 个

#### P0 级问题（必须修复才能开工）

| # | 问题 | 发现者 | 来源章节 |
|---|------|--------|----------|
| P0-1 | **Reverse scan 空文件边界 off-by-4** | GPT | 4.2.1 |
| P0-2 | **`Dirty Set` 强引用语义与 `Set<ObjectId>` 类型矛盾** | GPT | 4.1.0 + 4.3.2 |
| P0-3 | **`RecordKind` 值域的文件域隔离未声明** | GPT | 4.2.1/4.2.2 |
| P0-4 | **`ulong` 值编码缺口**（`Val_VarInt` 无法覆盖） | GPT | 4.1.4 + 4.4.2 |
| P0-5 | **术语表遗漏 `Commit point`** | Claude | 术语表 |
| P0-6 | **`Identity Map` 与 `Dirty Set` 生命周期未明确** | Claude | 4.1.2 + 4.3.2 |
| P0-7 | **`LoadObject` vs `Resolve` 边界模糊** | Claude | 术语表 + 4.1.0 |
| P0-8 | **`DiscardChanges` 建议升级为 MUST** | Gemini | 4.4.3 |

#### P1 级问题（强烈建议开工前修复）

| # | 问题 | 发现者 | 来源章节 |
|---|------|--------|----------|
| P1-1 | `Checkpoint Version` 与 `base` 术语混用 | Claude | 全文 |
| P1-2 | `_dirtyKeys` 与 `Dirty Set` 层级关系未澄清 | Claude | 4.4.3 |
| P1-3 | 伪代码类型系统不成立 | GPT | 4.4.4 |
| P1-4 | 缺少 RFC 2119/8174 规范语言声明 | GPT | 全文 |
| P1-5 | `CommitAll(newRootId)` 缺乏便捷重载 | Gemini | 4.4.5 |
| P1-6 | `Delete` 命名偏离 C# `Remove` 惯例 | Gemini | 4.4.4 |
| P1-7 | 缺乏 Dirty Set 状态可见性 API | Gemini | 4.1.2 |

#### P2 级问题（可延后）

| # | 问题 | 发现者 | 来源章节 |
|---|------|--------|----------|
| P2-1 | `RecordKind` 枚举值表分散 | Claude + GPT | 4.2.x |
| P2-2 | `ValueType` 枚举与用户类型表不完全对齐 | Claude | 4.1.4 + 4.4.2 |
| P2-3 | `Deserialize`/`Materialize` 术语表未收录 | Claude | 术语表 |
| P2-4 | `RBF framing` 术语未注册 | GPT | 4.2.1 |
| P2-5 | 决策表缺少 Superseded 标记机制 | GPT | 第 3 节 |

#### 交叉验证与共识点

1. **术语一致性**：Claude 和 Gemini 都认同 `Checkpoint Version` vs `base` 的混用问题
2. **API 命名**：Gemini 提出 `Delete`→`Remove`，GPT 在伪代码审阅中也发现类型系统问题
3. **崩溃恢复**：GPT 的 reverse scan 问题与 Claude 的 `Commit point` 缺失相关联
4. **强引用语义**：GPT 的 `Dirty Set` 类型矛盾与 Claude 的生命周期问题是同一根因

### 待讨论点

1. **`Dirty Set` 设计**：应该是 `Set<ObjectId>` 还是 `Dictionary<ObjectId, IDurableObject>`？
2. **`ulong` 值支持**：新增 `Val_VarUInt` 还是收紧支持类型？
3. **伪代码定位**：改为可编译版本还是显著标注为伪代码？

---

## 第二轮审阅（针对第一轮发现的问题）

### DocUIClaude 跟进意见

> **跟进视角**：概念框架、术语一致性、逻辑自洽
> **跟进时间**：2025-12-20

#### 对 P0 问题的表态

| # | 问题 | 表态 | 理由 |
|---|------|------|------|
| P0-1 | Reverse scan 空文件边界 off-by-4 | ✅ 同意 | GPT 的分析准确。当 `FileLength=4` 时，`MagicPos=0`，无 record 判定应为 `RecordEnd==0`。这是崩溃恢复关键路径，必须修正。 |
| P0-2 | `Dirty Set` 强引用语义与类型矛盾 | ✅ 同意 | 这是概念层与实现层脱节的典型案例。**语义目标**（防 GC 丢改动）与**类型定义**（`Set<ObjectId>`）不一致。必须二择一并写死。 |
| P0-3 | `RecordKind` 文件域隔离未声明 | ✅ 同意 | 同一个 `0x01` 在 data/meta 两个文件中表示不同记录类型，这是"隐式知识"。应显式声明域隔离，或拆分为 `DataRecordKind`/`MetaRecordKind`。 |
| P0-4 | `ulong` 值编码缺口 | ✅ 同意 | ZigZag 编码的 `varint` 无法无损表示 `>2^63-1` 的 `ulong`。必须决策：扩展编码或收紧类型。详见下方讨论。 |
| P0-5 | 术语表遗漏 `Commit point` | ✅ 维持 | 作为第一轮提出者，维持此建议。`Commit point` 是崩溃恢复语义的锚点，必须正式定义。 |
| P0-6 | `Identity Map` 与 `Dirty Set` 生命周期 | ✅ 维持 | 与 P0-2 同根。一旦 P0-2 的设计写死，生命周期自然清晰。建议在术语表补充生命周期说明。 |
| P0-7 | `LoadObject` vs `Resolve` 边界 | ✅ 维持 | 同意 Gemini 的分析：`Load` = 用户意图，`Resolve` = 底层机制。建议：对外 API 统一用 `Load`，术语表明确 `Resolve` 为 "Internal Only"。 |
| P0-8 | `DiscardChanges` 升级为 MUST | ✅ 同意 | Gemini 的 "Undo Affordance" 论证有力。Implicit ChangeSet 模式下，`DiscardChanges` 是唯一的安全逃生口。 |

#### 对待讨论点的意见

**1. `Dirty Set` 设计**

> **问题**：应该是 `Set<ObjectId>` 还是 `Dictionary<ObjectId, IDurableObject>`？

**建议**：选 `Dictionary<ObjectId, IDurableObject>`（或等效的强引用集合）

**分析**：
- **语义目标**：防止 dirty 对象在 Commit 前被 GC 回收
- **`Set<ObjectId>` 的问题**：`ObjectId` 是值类型（`ulong`），不持有对象引用，无法阻止 GC
- **`Dictionary<ObjectId, IDurableObject>` 的优势**：
  - Key 用于快速查找（O(1) 判定对象是否 dirty）
  - Value 持有强引用，实现 GC 保护
  - 与 `Identity Map`（WeakReference）形成互补：Identity Map 允许 GC 回收干净对象，Dirty Set 保护脏对象

**概念层命名建议**：
- 术语表应定义为：`Dirty Set — 持有 dirty 对象强引用的集合，类型为 Dictionary<ObjectId, IDurableObject>`
- 实现可自由选择具体容器（`Dictionary`/`ConcurrentDictionary`/自定义），但必须满足"强引用"语义

**2. `ulong` 值支持**

> **问题**：新增 `Val_VarUInt` 还是收紧支持类型？

**建议**：收紧支持类型，将 `ulong` 移出"值类型"范畴

**分析**：
- **MVP 原则**：最小可行方案优先，复杂性后置
- **`ulong` 的实际用途**：主要用于 `ObjectId`、`Ptr64`、`Epoch` 等**标识符/地址**，这些在编码层已有专门类型
- **"值"的语义**：用户存储的业务数据。业务场景中需要 `ulong` 全域的需求较少
- **新增 `Val_VarUInt` 的代价**：
  - 编码复杂度 +1（需要无符号 varuint 编解码）
  - `ValueType` 枚举扩展（占用 low 4-bit 一个槽位）
  - 序列化/反序列化代码分支 +1
  - 所有读取端必须支持，否则前向兼容性破坏

**推荐方案**：
- MVP 值类型收紧为：`null`, `long`（有符号 varint）, `ObjRef`, `Ptr64`
- 术语表 4.1.4 的类型约束表删除 `ulong`，或注明"ulong 仅用于 ObjectId/Ptr64 等结构字段，不作为独立值类型"
- 未来如有需求，可在 v2 扩展 `Val_VarUInt`

**3. 伪代码定位**

> **问题**：改为可编译版本还是显著标注为伪代码？

**建议**：显著标注为伪代码 + 规范层/示例层分离

**分析**：
- **当前问题**：伪代码"看起来像 C#"，读者会当作"接近可用的 reference implementation"
- **全面改可编译版本的代价**：
  - 需要定义所有依赖类型（`DiffEntry`, `IRecordWriter` 等）
  - 需要处理所有边缘情况（泛型约束、null 处理等）
  - 代码量膨胀，喧宾夺主，干扰规范阅读
- **伪代码的价值**：简洁展示算法逻辑，忽略实现细节

**推荐方案**（GPT 的分层建议很好，我进一步细化）：

1. **规范层**：
   - Method signatures（精确的 API 签名）
   - 不变式（MUST/SHOULD 条款）
   - 示例：`void Upsert(ulong key, TValue value)` + "MUST: Upsert 后 `IsDirty == true`"

2. **伪代码层**：
   - 在代码块前加显著标注：`// ⚠️ PSEUDO-CODE: 仅展示算法逻辑，不保证可编译`
   - 或使用不同语法高亮（如 `plaintext` 而非 `csharp`）

3. **参考实现**（可选，放 Appendix 或单独文件）：
   - 真正可编译的 MVP 版本
   - 可作为测试向量的基准

#### 补充说明

**跨问题关联**：P0-2（Dirty Set 类型）、P0-6（生命周期）、P1-2（`_dirtyKeys` 层级）是同一概念域的三个切面。建议在术语表中增加"对象状态管理"分组，统一定义：
- `Identity Map`：WeakReference 映射，允许 GC 回收干净对象
- `Dirty Set`：强引用集合，保护 dirty 对象免于 GC
- `DirtyKeys`：对象级，记录该对象内哪些 key 发生变化

**对 GPT 伪代码审阅的额外观察**：
4.4.4 中 `DurableDict<K,V>` 与 2.7 节 Q23 "key 固定为 `ulong`" 的矛盾，暴露了"决策表"与"正文代码"之间的同步机制缺失。建议：
- 决策表中的关键取舍应在正文首次使用时 **显式回引**（如 "根据 Q23，MVP key 固定为 `ulong`"）
- 或在代码块附近增加 "Design Note" 说明约束来源

---

### DocUIGemini 跟进意见

> **跟进视角**：UX/交互设计、可实现性、API 可发现性
> **跟进时间**：2025-12-20

#### 对 P0 问题的表态

| # | 问题 | 表态 | 理由 |
|---|------|------|------|
| P0-1 | Reverse scan 空文件边界 off-by-4 | ✅ 同意 | 崩溃恢复逻辑必须精确，off-by-N 错误是致命的。 |
| P0-2 | `Dirty Set` 强引用语义与类型矛盾 | ✅ 同意 | 必须持有对象实例的强引用，否则 GC 会在持久化前回收脏对象，导致数据丢失。这是"隐形安全网"的基础。 |
| P0-3 | `RecordKind` 文件域隔离未声明 | ✅ 同意 | 显式优于隐式。避免实现者复用枚举值导致解析错误。 |
| P0-4 | `ulong` 值编码缺口 | ✅ 同意 | 必须解决。建议 MVP 收紧类型，移除 `ulong` 值支持。 |
| P0-5 | 术语表遗漏 `Commit point` | ✅ 同意 | 核心概念必须定义。 |
| P0-6 | `Identity Map` 与 `Dirty Set` 生命周期 | ✅ 同意 | 生命周期管理是内存态的核心逻辑，必须明确。 |
| P0-7 | `LoadObject` vs `Resolve` 边界 | ✅ 同意 | 区分"用户意图"(Load)与"底层机制"(Resolve)对 API 可用性至关重要。 |
| P0-8 | `DiscardChanges` 升级为 MUST | ✅ 同意 | 这是唯一的 Undo 机制，必须作为 MVP 的标配。 |

#### 对待讨论点的意见

**1. `Dirty Set` 设计**

> **问题**：应该是 `Set<ObjectId>` 还是 `Dictionary<ObjectId, IDurableObject>`？

**建议**：`Dictionary<ObjectId, IDurableObject>`

**分析**：
- **UX 视角（隐形安全网）**：开发者（用户）的心智模型是"我修改了对象，它就应该在那里等待保存"。如果因为 GC 导致对象消失，这是严重的 **Expectation Violation**。
- **实现视角**：`Dictionary` 提供了 O(1) 的查找（用于 `IsDirty` 检查）和强引用持有（用于防止 GC）。
- **补充**：这个设计实现了 **Passive Safety**（被动安全）。用户不需要手动 `Pin()` 对象，系统自动处理。

**2. `ulong` 值支持**

> **问题**：新增 `Val_VarUInt` 还是收紧支持类型？

**建议**：收紧支持类型，移除 `ulong` 作为独立值类型。

**分析**：
- **UX 视角（互操作性）**：`ulong` 在跨语言交互（尤其是与 JS/JSON 交互）时常是痛点。在 MVP 阶段，`long` (signed 64-bit) 已经能覆盖绝大多数"大整数"需求。
- **实现视角**：减少一种编码格式，简化解析器逻辑。
- **替代方案**：如果用户确实需要存储 `ulong` 且不关心数值语义（只作为 bit pattern），可以使用 `long` 进行 `reinterpret_cast` 存储，或者使用 `byte[8]`。

**3. 伪代码定位**

> **问题**：改为可编译版本还是显著标注为伪代码？

**建议**：显著标注为伪代码 + 规范层/示例层分离。

**分析**：
- **UX 视角（虚假示能）**：如果代码看起来像 C# 却不能编译，这是一种 **False Affordance**。它暗示了"Copy-Paste 可用性"却无法兑现。
- **建议**：
    - **规范层**：使用 IDL (Interface Definition Language) 风格或纯签名描述接口契约。
    - **逻辑层**：使用伪代码描述算法，并在代码块顶部显式标注 `// PSEUDO-CODE`。
    - **示例层**：如果需要提供可运行示例，应单独提供 Test Case 或 Usage Example，而不是混在规范描述中。

#### 对 DocUIClaude 建议的回应

*   **同意** DocUIClaude 关于 `Dirty Set` 使用 `Dictionary` 的建议。这与我强调的 **Invisible Safety Net** 理念完全一致。
*   **同意** DocUIClaude 关于 `ulong` 的收紧建议。MVP 应聚焦于核心场景，`long` 足够通用。
*   **同意** DocUIClaude 关于伪代码分层的建议。
*   **补充**：关于 P0-2/P0-6，建议在术语表中引入 **Object Lifecycle State** (Clean, Dirty, Ghost/Evicted) 的状态机描述，这样能更清晰地定义 `Identity Map` 和 `Dirty Set` 在每个状态转换时的行为。

---

### DocUIGPT 跟进意见

#### 对 P0 问题的表态

| # | 问题 | 表态 | 理由 |
|---|------|------|------|
| P0-1 | Reverse scan 空文件边界 off-by-4 | ✅ 同意 | 属于崩溃恢复关键路径的边界定义错误；在 `FileLength=4`（仅 `[Magic]`）时应判定 `RecordEnd==0`（而不是 `4`），否则会把 tail/CRC 定位带偏。 |
| P0-2 | `Dirty Set` 强引用语义与 `Set<ObjectId>` 类型矛盾 | ✅ 同意 | 仅存 `ObjectId` 无法持有对象实例强引用，达不到“防 GC 丢改动”的语义目标；必须把“强引用对象实例”落到数据结构契约里。 |
| P0-3 | `RecordKind` 值域的文件域隔离未声明 | ✅ 同意 | data/meta 复用 `0x01` 但未声明“域隔离”，会诱导实现者用单一枚举导致解析冲突；应写成显式规范（域隔离 MUST）。 |
| P0-4 | `ulong` 值编码缺口（仅有 ZigZag `Val_VarInt`） | ✅ 同意 | 现有 ZigZag varint 无法无损覆盖 `ulong` 全域；必须做明确取舍（新增编码或收紧支持）。 |
| P0-5 | 术语表遗漏 `Commit point` | ✅ 同意 | `Commit point` 是恢复语义的锚点概念；缺失会让“提交点/可见性边界”的讨论失去规范落点。 |
| P0-6 | `Identity Map` 与 `Dirty Set` 生命周期未明确 | ✅ 同意 | 这决定“是否可能假提交/丢改动/内存泄漏”；必须把进入/退出条件写成可检验规则。 |
| P0-7 | `LoadObject` vs `Resolve` 边界模糊 | ✅ 同意 | 外部 API 与内部子步骤混名会导致实现与文档读者误解；需要在术语表明确“对外只用 Load，对内可用 Resolve（Internal Only）”。 |
| P0-8 | `DiscardChanges` 建议升级为 MUST | ✅ 同意 | Implicit ChangeSet 模式下这是唯一明确的撤销/逃生口；若仅 SHOULD，会让安全网变成“可选”，影响可用性与可恢复性。 |

#### 对待讨论点的意见

**1. `Dirty Set` 设计**

同意建议：用 `Dictionary<ObjectId, IDurableObject>`（或等价的“ObjectId → 对象实例强引用”映射）。

补充两点约束，建议写入规范：
- **语义约束（Normative）**：Dirty Set MUST 持有对象实例的强引用，直到该对象的变更被“heap 级 commit point”确认成功（或显式 `DiscardChanges`/对象移除）。
- **一致性约束（Normative）**：Dirty Set 的 key 必须等于对象自身 `ObjectId`；当同一 `ObjectId` 被多次加载得到同一实例（Identity Map 命中）时，Dirty Set value 应保持引用同一实例，避免“同 id 多实例”的分叉状态。

实现建议（Informative）：Identity Map 可继续用 `WeakReference`/`ConditionalWeakTable`；Dirty Set 负责“脏对象 pin 住”。这样职责边界清晰：weak 用于 cache，strong 用于 correctness。

**2. `ulong` 值支持**

同意建议：收紧支持类型，把 `ulong` 作为“用户值类型”移出 MVP。

补充：
- 允许 `ulong` 出现在**结构字段**（如 `ObjectId`/`Ptr64`/计数/长度等）不矛盾；但“用户可写入值类型”与“格式字段类型”需要在文档里明确分栏。
- 若未来要加回 `ulong` 值，有两个可选路径：
   - 新增 `Val_VarUInt`（varuint，适配全域无符号）；或
   - 新增固定宽度 `Val_U64LE`（实现更直接，牺牲空间换实现简单）。

MVP 取舍上，我更倾向“先收紧”，避免格式分叉与额外兼容负担。

**3. 伪代码定位**

同意建议：显著标注 + 规范层/示例层分离。

补充一个写作结构建议：在文档中明确区分 **Normative**（规范性条款）与 **Informative**（解释/示例），并让伪代码默认落在 Informative。
- 规范层：API 签名、不变式、状态转换、崩溃恢复契约（commit point / reverse scan / truncate 规则）。
- 示例层：伪代码/样例实现必须以醒目标注“PSEUDO-CODE / NOT COMPILABLE”，并避免使用会误导为“可直接复制”的 `csharp` 高亮（可用 `text`/`plaintext`）。

#### 对前两位顾问建议的回应

我同意 DocUIClaude 与 DocUIGemini 在第二轮对 P0 的全部表态结论，并对三项待讨论点的共同建议（Dirty Set 用 `Dictionary<ObjectId, IDurableObject>`、MVP 移除 `ulong` 值支持、伪代码分层并醒目标注）无保留赞同。

额外补充：建议把 MUST/SHOULD 的规范语言（RFC 2119/8174）与 Normative/Informative 分区写进文档头部，便于后续核查与测试向量编写。

#### 共识确认

同意：当前在第二轮已经对所有 P0 问题达成共识；对三个待讨论点也已形成一致建议，可进入“按共识修改文档并复审”的阶段。

---

## 共识达成

### 确认的问题清单

| # | 问题 | 严重度 | 位置 | 状态 |
|---|------|--------|------|------|
| P0-1 | Reverse scan 空文件边界 off-by-4 | Major | 4.2.1 | ✅ 三方同意 |
| P0-2 | `Dirty Set` 强引用语义与 `Set<ObjectId>` 类型矛盾 | Major | 4.1.0 + 4.3.2 | ✅ 三方同意 |
| P0-3 | `RecordKind` 值域的文件域隔离未声明 | Major | 4.2.1/4.2.2 | ✅ 三方同意 |
| P0-4 | `ulong` 值编码缺口 | Major | 4.1.4 + 4.4.2 | ✅ 三方同意 |
| P0-5 | 术语表遗漏 `Commit point` | Major | 术语表 | ✅ 三方同意 |
| P0-6 | `Identity Map` 与 `Dirty Set` 生命周期未明确 | Major | 4.1.2 + 4.3.2 | ✅ 三方同意 |
| P0-7 | `LoadObject` vs `Resolve` 边界模糊 | Major | 术语表 + 4.1.0 | ✅ 三方同意 |
| P0-8 | `DiscardChanges` 建议升级为 MUST | Major | 4.4.3 | ✅ 三方同意 |

### 待讨论点决议

| # | 问题 | 决议 | 投票 |
|---|------|------|------|
| 1 | `Dirty Set` 设计 | 采用 `Dictionary<ObjectId, IDurableObject>`，必须持有对象实例的强引用 | Claude ✅ Gemini ✅ GPT ✅ |
| 2 | `ulong` 值支持 | 收紧支持类型，将 `ulong` 作为独立值类型移出 MVP（仅保留在结构字段中） | Claude ✅ Gemini ✅ GPT ✅ |
| 3 | 伪代码定位 | 显著标注为伪代码 + 规范层/示例层分离（使用 Normative/Informative 标记） | Claude ✅ Gemini ✅ GPT ✅ |

### 建议的解决方案

#### P0 级修复（开工前必须完成）

| # | 问题 | 修复方案 |
|---|------|----------|
| P0-1 | Reverse scan off-by-4 | 将"无 record 判定"从 `RecordEnd == 4` 改为 `RecordEnd == 0`（或等价写法） |
| P0-2 | `Dirty Set` 类型 | 将术语表定义改为 `Dictionary<ObjectId, IDurableObject>`，并明确"MUST 持有对象实例强引用" |
| P0-3 | `RecordKind` 域隔离 | 引入 `DataRecordKind` 与 `MetaRecordKind` 两个独立枚举，或在术语表声明"域隔离 MUST" |
| P0-4 | `ulong` 值编码 | 在 4.1.4 的类型约束表中删除 `ulong`，注明"ulong 仅用于 ObjectId/Ptr64 等结构字段" |
| P0-5 | `Commit point` 定义 | 在术语表增加：`Commit point — 对外可见的版本持久化边界；在 meta file 方案中，等于 MetaCommitRecord 成功落盘的时刻` |
| P0-6 | 生命周期明确化 | 在术语表或 4.1 增加"对象状态管理"分组，定义 Identity Map、Dirty Set、DirtyKeys 的进入/退出条件 |
| P0-7 | `LoadObject`/`Resolve` 边界 | 术语表明确 `Resolve` 为 "Internal Only"，对外 API 统一用 `Load` |
| P0-8 | `DiscardChanges` MUST | 将 4.4.3 中 `DiscardChanges` 从 SHOULD 升级为 MUST |

#### P1 级修复（强烈建议开工前完成）

| # | 问题 | 修复方案 |
|---|------|----------|
| P1-1 | `Checkpoint`/`base` 混用 | 全文替换 `base` → `Checkpoint Version`，或在术语表定义 `base` 为别名 |
| P1-2 | `_dirtyKeys` 层级 | 在术语表或 4.4.3 首次出现时澄清层级关系 |
| P1-3 | 伪代码类型系统 | 在代码块前加 `// ⚠️ PSEUDO-CODE`，或改用 `plaintext` 高亮 |
| P1-4 | RFC 2119/8174 声明 | 增加"Normative Language"小节，声明 MUST/SHOULD 的严格含义 |
| P1-5 | `CommitAll` 重载 | 增加 `CommitAll()` 无参重载（沿用当前 Root） |
| P1-6 | `Delete` → `Remove` | 将 API 命名改为 `Remove` 以符合 C# 惯例 |
| P1-7 | Dirty Set 可见性 | 增加 `IEnumerable<ObjectId> GetDirtyObjects()` 或 `IsDirty(objectId)` API |

#### P2 级修复（可延后）

- 新增 Appendix 集中枚举表/常量表
- 统一 `RBF framing` 作为正式术语
- 给决策表增加 Superseded 机制
- 补充 `Deserialize`/`Materialize` 术语表条目

---

## 会议结论

### 审阅成果

本次秘密基地畅谈会成功完成了对 DurableHeap MVP v2 设计文档的多轮审阅：

- **参与者**：DocUIClaude（概念框架）、DocUIGemini（UX/交互）、DocUIGPT（规范检查）
- **发现问题**：共 20 个，其中 P0 级 8 个
- **共识达成**：所有 P0 问题及三个待讨论点均达成一致意见

### 关键决议

1. **`Dirty Set` 必须持有对象实例的强引用**（`Dictionary<ObjectId, IDurableObject>`）
2. **MVP 值类型收紧**，移除 `ulong` 作为独立值类型
3. **伪代码明确标注**，采用 Normative/Informative 分区

### 后续行动

1. **P0 修复**：开工前必须完成全部 8 项 P0 级修复
2. **P1 修复**：强烈建议在开工前完成
3. **文档更新**：根据本次审阅结果更新 `mvp-design-v2.md`
4. **复审**：修复完成后进行快速复审确认

### 会议评价

本次审阅展示了多视角协作的价值：
- **Claude** 从概念框架角度发现了术语一致性和生命周期定义问题
- **Gemini** 从 UX 角度发现了 API 可用性和安全网问题
- **GPT** 从规范检查角度发现了崩溃恢复逻辑和编码完备性问题

三个视角形成了有效的交叉验证，最终达成了高质量的共识。

---

> **会议时间**：2025-12-20
> **主持人**：刘德智 / SageWeaver
> **记录人**：AI Team
> **状态**：✅ 已结束，共识达成

---

## 修复实施记录

> **执行时间**：2025-12-20
> **执行人**：刘德智 / SageWeaver

### 已实施的 P0 级修复

| # | 问题 | 修复状态 | 说明 |
|---|------|----------|------|
| P0-1 | Reverse scan off-by-4 | ✅ 完成 | `RecordEnd == 4` → `RecordEnd == 0`，并补充边界说明 |
| P0-2 | `Dirty Set` 类型 | ✅ 完成 | 改为 `Dictionary<ObjectId, IDurableObject>`，明确强引用语义 |
| P0-3 | `RecordKind` 域隔离 | ✅ 完成 | 术语表声明域隔离 MUST，引入 `DataRecordKind`/`MetaRecordKind` |
| P0-4 | `ulong` 值编码 | ✅ 完成 | 从值类型表移除，注明仅用于结构字段 |
| P0-5 | `Commit point` 定义 | ✅ 完成 | 术语表"提交与 HEAD"分组增加 `Commit Point` 条目 |
| P0-6 | 生命周期明确化 | ✅ 完成 | 新增 §4.1.0.1 对象状态管理（Object Lifecycle） |
| P0-7 | `LoadObject`/`Resolve` 边界 | ✅ 完成 | 术语表标注 `Resolve` 为 "Internal Only" |
| P0-8 | `DiscardChanges` MUST | ✅ 完成 | 从 SHOULD 升级为 MUST，增加独立小节强调 |

### 已实施的 P1 级修复

| # | 问题 | 修复状态 | 说明 |
|---|------|----------|------|
| P1-1 | `Checkpoint`/`base` 混用 | ✅ 完成 | 统一使用 `Checkpoint Version`，Q9/Q10 已修正 |
| P1-2 | `_dirtyKeys` 层级 | ✅ 完成 | 伪代码前增加术语澄清说明 |
| P1-3 | 伪代码类型系统 | ✅ 完成 | 添加 `⚠️ PSEUDO-CODE` 标注和 Normative/Informative 声明 |
| P1-4 | RFC 2119/8174 声明 | ✅ 完成 | 新增"规范语言（Normative Language）"小节 |
| P1-5 | `CommitAll` 重载 | ✅ 完成 | 增加 `CommitAll()` 无参重载说明 |
| P1-6 | `Delete` → `Remove` | ✅ 完成 | API 命名改为 `Remove`，符合 C# 惯例 |
| P1-7 | Dirty Set 可见性 | ✅ 完成 | 新增 §4.4.7 Dirty Set 可见性 API |

### 待复审

以上修复已全部实施，待三位顾问复审确认。

---

## 复审结果汇总

> **复审时间**：2025-12-20
> **复审人**：DocUIClaude、DocUIGemini、DocUIGPT

### 全体通过

| 顾问 | 结论 | 备注 |
|------|------|------|
| **DocUIClaude** | ✅ 批准 | 所有修复正确实施，概念定义清晰、术语使用一致、逻辑自洽 |
| **DocUIGemini** | ✅ 批准 | UX 相关修复均已落实，被动安全性和 DX 显著提升 |
| **DocUIGPT** | ✅ 批准 | 规范检查通过，发现 2 个 P2 级轻微问题（不阻塞批准） |

### P2 级遗留问题（可延后）

| # | 问题 | 来源 |
|---|------|------|
| 1 | `reinterpret_cast` 术语在 C# 语境下易误导，建议改用 `unchecked` 表述 | DocUIGPT |
| 2 | 正文中 `RecordKind == 0x01` 可改为 `DataRecordKind`/`MetaRecordKind` 以"防误用" | DocUIGPT |

---

## 最终会议结论

**文档状态**：✅ **可开工规格已达成**

DurableHeap MVP v2 设计文档经过：
1. 第一轮三方独立审阅（发现 20 个问题）
2. 第二轮共识讨论（达成全部 P0/P1 修复方案）
3. 全部 P0（8 项）和 P1（7 项）修复实施
4. 第三轮三方复审（全体通过）

现已具备开工条件。

### 致谢

感谢三位 DocUI Specialist 的专业贡献：
- **DocUIClaude**：概念框架守护者，确保术语一致性与逻辑自洽
- **DocUIGemini**：UX 体验守护者，确保 API 可用性与安全逃生口
- **DocUIGPT**：规范检查守护者，确保编码格式与文档质量

---

> **会议时间**：2025-12-20
> **主持人**：刘德智 / SageWeaver
> **记录人**：AI Team
> **状态**：✅ 完成，可开工

---

### DocUIClaude 复审意见

> **复审视角**：概念框架、术语一致性、逻辑自洽
> **复审时间**：2025-12-20

#### 修复验证

| 修复项 | 验证结果 | 备注 |
|--------|----------|------|
| P0-5 Commit point | ✅ 正确实施 | 术语表"提交与 HEAD"分组已添加 `Commit Point` 条目，定义为"对外可见的版本持久化边界；在 meta file 方案中，等于 `MetaCommitRecord` 成功落盘的时刻"。与 §4.2.2 的 commit point 描述一致。 |
| P0-6 对象状态管理 | ✅ 正确实施 | 新增 §4.1.0.1 Object Lifecycle，定义了 Clean/Dirty 状态及转换规则，明确了 Identity Map（WeakRef）与 Dirty Set（强引用）的职责分离和生命周期。状态转换表清晰完整。 |
| P0-7 LoadObject/Resolve 边界 | ✅ 正确实施 | 术语表明确标注 `Resolve` 为 "Internal Only"（弃用声明：`Deprecated: Resolve（作为外部 API 总称；内部仍可用 Resolve 描述"解析版本指针"子步骤，标记为 Internal Only）`）。边界清晰。 |
| P1-1 Checkpoint/base 统一 | ✅ 正确实施 | 全文检查：Q9 备注、Q10 备注中的 `base` 已替换为 `Checkpoint Version`。术语表定义了 `Checkpoint Version`（全量状态版本，`PrevVersionPtr=0`）。 |
| P1-2 _dirtyKeys 层级 | ✅ 正确实施 | §4.4.4 伪代码前增加了术语澄清："下文 `_dirtyKeys` 是 `DurableDict` 对象内部的私有字段，追踪**该对象内**发生变更的 key 集合。它与 Workspace 级别的 **Dirty Set**（持有所有 dirty 对象实例）是不同层级的概念。" |

#### 其他 P0/P1 修复的抽查验证

| 修复项 | 验证结果 | 备注 |
|--------|----------|------|
| P0-1 Reverse scan off-by-4 | ✅ 正确实施 | §4.2.1 反向扫尾不变量：`RecordEnd == 0` 作为"无 record"判定，边界说明"`FileLength == 4`, `MagicPos == 0`"已补充 |
| P0-2 Dirty Set 类型 | ✅ 正确实施 | 术语表定义为 `Dictionary<ObjectId, IDurableObject>`（**强引用**），§4.1.0.1 明确"Dirty Set MUST 持有对象实例的强引用" |
| P0-3 RecordKind 域隔离 | ✅ 正确实施 | 术语表声明"**域隔离（MUST）**"，引入 `DataRecordKind` / `MetaRecordKind` 编码名 |
| P0-4 ulong 值编码 | ✅ 正确实施 | §4.1.4 类型约束表明确 `ulong` 不支持作为独立值类型，注明"仅用于 `ObjectId`/`Ptr64`/Dict Key 等结构字段" |
| P0-8 DiscardChanges MUST | ✅ 正确实施 | §4.4.3 新增"DiscardChanges 支持（MUST）"独立小节，明确为 MVP 必须实现 |
| P1-4 RFC 2119/8174 声明 | ✅ 正确实施 | 文档开头新增"规范语言（Normative Language）"小节，声明采用 RFC 2119/8174 关键字 |

#### 发现的新问题

**无新问题。** 修复实施准确完整。

#### 值得表扬的改进

1. **§4.1.0.1 对象状态管理**：新增的生命周期状态表（Clean → Dirty → Clean 转换）非常清晰，填补了概念空白
2. **术语表的 Commit Point 定义**：准确捕捉了"可见性边界"的语义，与 meta file 方案对齐
3. **二阶段提交的术语命名**：`WritePendingDiff` + `OnCommitSucceeded` 比原来的 `FlushToWriter` 更准确地表达了阶段语义
4. **伪代码标注**：`⚠️ PSEUDO-CODE` 标注明确，Normative/Informative 区分清晰

#### 概念自洽性检查

验证修复后的文档在概念层面的一致性：

| 检查项 | 结果 |
|--------|------|
| Identity Map 与 Dirty Set 的关系 | ✅ 清晰：WeakRef（缓存/去重） vs 强引用（GC 保护） |
| 对象状态转换的完备性 | ✅ 完整：LoadObject → Clean → Dirty → Commit → Clean 路径闭合 |
| Commit Point 与崩溃恢复的逻辑链 | ✅ 一致：Commit Point = meta fsync → 恢复时反向扫描 meta → 找到最后有效 Commit Point |
| _dirtyKeys 与 Dirty Set 的层级关系 | ✅ 明确：对象内（_dirtyKeys） vs Workspace 级（Dirty Set） |

#### 结论

- [x] **批准所有修复**
- [ ] 需要进一步修改

**审阅意见**：所有与我的专业领域相关的修复（P0-5/P0-6/P0-7/P1-1/P1-2）均已正确实施，概念定义清晰、术语使用一致、逻辑自洽。文档已达到"可开工规格"的质量标准。

**特别说明**：本次修复不仅解决了具体问题，还通过 §4.1.0.1（对象状态管理）和规范语言声明等结构性改进，提升了文档的整体可检验性和可实现性。建议作为 DurableHeap 后续文档的参考模板。

---

### DocUIGemini 复审意见

> **复审视角**：UX/交互设计、用户体验、可发现性
> **复审时间**：2025-12-20

#### 修复验证

| 修复项 | 验证结果 | 备注 |
|--------|----------|------|
| P0-2 Dirty Set 类型 | ✅ | 术语表已定义为 `Dictionary<ObjectId, IDurableObject>`（强引用），§4.1.0.1 明确了"Dirty 对象不得被 GC 回收"的约束。这成功构建了"隐形安全网"。 |
| P0-8 DiscardChanges MUST | ✅ | §4.4.3 已将其升级为 MUST，并明确其为"唯一的安全逃生口"。这保证了基本的 Undo Affordance。 |
| P1-5 CommitAll() 重载 | ✅ | §4.4.5 已添加 `CommitAll()` 无参重载建议，降低了"仅保存修改"场景下的认知负荷。 |
| P1-6 Delete → Remove 命名 | ✅ | §4.4.2 和 §4.4.4 伪代码中均已修正为 `Remove`，并添加了符合 C# 惯例的注释。消除了 Metaphor Leakage。 |
| P1-7 Dirty Set 可见性 API | ✅ | 新增 §4.4.7，提供了 `HasDirtyObjects` / `GetDirtyObjectIds` 等 API。满足了"系统状态可见性"原则。 |

#### 发现的新问题（如有）

无。

#### 结论

- [x] 批准所有修复
- [ ] 需要进一步修改

**复审总结**：
所有 UX 相关的关键修复均已落实。
特别是 **Dirty Set 的强引用设计** 和 **DiscardChanges 的强制化**，极大地提升了 DurableHeap 的被动安全性（Passive Safety）和错误恢复能力（Recovery Affordance）。
API 命名的修正（Remove）和重载的添加（CommitAll）也显著改善了开发者的使用体验（DX）。
文档现在不仅是技术规范，也体现了良好的交互设计原则。

---

### DocUIGPT 复审意见

> **复审视角**：规范检查、命名约定、文档格式、代码示例准确性
> **复审时间**：2025-12-20

#### 修复验证

| 修复项 | 验证结果 | 备注 |
|--------|----------|------|
| P0-1 Reverse scan 边界 | ✅ | §4.2.1 已将“无 record 判定”写为 `RecordEnd == 0`，并补充了“仅 `[Magic]` 时 `FileLength==4`、`MagicPos==0`”的边界说明；与 `RecordEnd = MagicPos` 定义一致。 |
| P0-3 `RecordKind` 域隔离声明 | ✅ | 术语表已明确 **域隔离（MUST）**：data file 与 meta file 各有独立 `RecordKind` 枚举空间，并给出 `DataRecordKind` / `MetaRecordKind` 的实现映射。 |
| P0-4 从 MVP 值类型移除 `ulong` | ✅ | §4.1.4 类型约束表中已将 `ulong` 从“值类型”移除，并补充“`ulong` 仅用于 `ObjectId`/`Ptr64`/Dict Key 等结构字段”的说明；与 `Val_VarInt(ZigZag)` 编码范围匹配。 |
| P1-3 伪代码标注（`⚠️ PSEUDO-CODE`） | ✅ | §4.4.4 在伪代码段落前增加醒目提示（引用块）且代码块内首行含 `// ⚠️ PSEUDO-CODE`，满足“可读但不诱导可编译”的文档质量要求。 |
| P1-4 RFC 2119/8174 规范语言声明 | ✅ | 文档开头新增“规范语言（Normative Language）”小节，明确采用 RFC 2119/8174 的 MUST/SHOULD/MAY 语义，并区分 Normative/Informative。 |

#### 发现的新问题（如有）

1. **术语风格小风险（P2）**：§4.1.4 使用了“`reinterpret_cast`”这一 C++ 术语来描述 `ulong`/`long` 的位模式转换；在 C# 语境下容易误导。
   - 建议改为更可执行的措辞，例如“使用 `unchecked((long)u)` / `unchecked((ulong)x)` 进行位模式转换（two’s complement 语义）”，或干脆避免给出该建议，改成“如需无符号 64-bit 业务值，后续版本新增 `Val_VarUInt`/`Val_U64LE`”。

2. **域隔离的“正文显式化”可加强（P2）**：虽已在术语表写死“域隔离 MUST”，但部分段落仍用 `RecordKind == 0x01` 的泛称表达（靠括注区分 meta/data）。
   - 不阻塞实现，但建议在这些段落同步改为 `MetaRecordKind == 0x01` / `DataRecordKind == 0x01`，降低实现者把它写成单一枚举的概率。

#### 结论

- [x] 批准所有修复
- [ ] 需要进一步修改
