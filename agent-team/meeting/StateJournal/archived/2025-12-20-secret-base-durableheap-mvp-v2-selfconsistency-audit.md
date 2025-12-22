# 秘密基地畅谈会：DurableHeap MVP v2 自洽性审阅

> **日期**：2025-12-20
> **目标**：审阅 `DurableHeap/docs/mvp-design-v2.md`，确保文档自洽、一致、低冗余和重复
> **主持人**：刘德智 (DocUI 规范起草委员会执行委员会主席)
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT

---

## 会议规则

这是一次**秘密基地畅谈会**，不是正式研讨会。

**规则**：
- 看到别人写的东西后，可以接着写、可以画、可以说"这让我想到..."
- 可以用 ASCII art、可以用伪代码、可以用比喻
- **唯一的约束**：先把问题写下来，评估以后再说

**审阅焦点**：
1. **术语一致性**：同一概念是否始终使用相同术语？
2. **概念自洽**：不同章节之间是否有矛盾？
3. **冗余检测**：是否有重复描述相同内容的段落？
4. **完整性**：是否有概念被提及但未定义？

---

## 第一轮：独立审阅

### DocUIClaude 发言

> **审阅视角**：概念框架专家，关注术语治理、概念边界、逻辑一致性

#### 一、术语一致性问题

| # | 问题类型 | 位置 | 问题描述 | 严重程度 |
|---|----------|------|----------|----------|
| T1 | 术语不一致 | §4.1.0, §4.2.4, 术语表 | **Checkpoint Version 的 `PrevVersionPtr=0` 双重语义**：文档多处说明 `PrevVersionPtr=0` 表示 Checkpoint Version（全量状态），但 §4.4.6 中新建对象首版本也是 `PrevVersionPtr=0`。这两种语义混用同一编码值，但概念上是不同的："截断回放的检查点" vs "创世版本（从空开始）"。虽然 §4.4.6 尝试澄清，但术语表未收录 "创世版本" 这一概念。 | **Medium** |
| T2 | 术语不一致 | §4.2.1, §4.2.2 | **Magic 的域描述不完整**：术语表仅定义 `RecordKind` 有域隔离，但 `Magic` 同样有域隔离（`DHD3` for data, `DHM3` for meta）。这不是错误，但与 RecordKind 的"域隔离（MUST）"描述不对称。 | **Low** |
| T3 | 术语遗漏 | 术语表 | **`base` 术语残留**：术语表将 `base` 标为 Checkpoint Version 的弃用别名，但在 §4.2.5 仍有 `PrevVersionPtr` 为 0 表示 "base/初始" 的用法。应统一为 "Checkpoint Version 或创世版本"。 | **Low** |
| T4 | 术语遗漏 | 术语表 | **缺失 `DurableObject` 术语**：全文使用 `durable object` / `DurableObject` / `IDurableObject`，但术语表未收录。作为核心概念，应明确定义。 | **Medium** |
| T5 | 术语遗漏 | 术语表 | **缺失 `from-empty diff` 术语**：§4.4.6 引入此概念描述新对象首版本的 DiffPayload 语义，但术语表未收录。 | **Low** |

#### 二、概念自洽性问题

| # | 问题类型 | 位置 | 问题描述 | 严重程度 |
|---|----------|------|----------|----------|
| C1 | 概念矛盾 | §4.1.0 vs §4.4.3 | **`_dirtyKeys` 类型不一致**：§4.1.0 的术语定义说 Dirty Set 是 `Dictionary<ObjectId, IDurableObject>`（Workspace 级），而 §4.4.3 的 `_dirtyKeys` 是 `ISet<ulong>`（对象级）。文档在 §4.4.4 伪代码前添加了术语澄清，但 §4.1.0 的 Dirty Set 定义未提及与 `_dirtyKeys` 的层级关系。两者关系需要在术语表或 §4.1.0.1 中明确。 | **Medium** |
| C2 | 概念缝隙 | §4.4.3 | **`HasChanges` 不变式的实现层保证**：不变式 #9 要求 `_dirtyKeys` 精确追踪差异，但 §4.4.4 的 `UpdateDirtyKey` 实现使用 `EqualityComparer<V>.Default.Equals`。若 V 是引用类型且未正确实现 `Equals`，可能违反不变式。文档在"关键实现要点"中提到了这一点，但未明确这是 MUST 还是 SHOULD 级别的约束。 | **Low** |
| C3 | 概念缝隙 | §4.1.0.1 vs §4.4.4 | **状态转换规则缺失"首次加载新建对象"场景**：§4.1.0.1 的表格假设 `LoadObject` 加载已存在的对象。但 §4.4.6 描述了新建对象的场景——新建对象何时进入 Identity Map？何时变为 Dirty？这在状态转换表中未体现。 | **Medium** |
| C4 | 逻辑不完整 | §4.3.2 | **LoadObject 对象不存在的处理未明确**：§4.3.1 提到空仓库时 `LoadObject(id)` 应返回"对象不存在"，但 §4.3.2 的 LoadObject 步骤只描述了正常路径，未说明 VersionIndex 中找不到 ObjectId 时的行为（抛异常？返回 null？创建新对象？）。 | **Medium** |

#### 三、冗余检测

| # | 问题类型 | 位置 | 问题描述 | 严重程度 |
|---|----------|------|----------|----------|
| R1 | 冗余描述 | §4.2.4 vs §4.4.2 | **Checkpoint Version 触发规则重复**：§4.2.4 详细描述了 `DictCheckpointEveryNVersions` 触发规则和实现提示，§4.4.2 的 dict diff 部分也有关于 Checkpoint Version 的约束（空变更 vs full-state）。两处描述角度不同但有重叠，可能导致维护时不一致。 | **Low** |
| R2 | 冗余描述 | §4.2.1 多处 | **Magic 作为 Record Separator 的解释重复**：§4.2.1 在"record framing"、"Magic 作为 Record Separator 的设计收益"、"写入顺序"三处重复解释 Magic 的语义和好处。这是合理的强调，但可以考虑集中到一处并交叉引用。 | **Low** |

#### 四、完整性问题

| # | 问题类型 | 位置 | 问题描述 | 严重程度 |
|---|----------|------|----------|----------|
| P1 | 定义缺失 | 全文 | **`DiscardChanges` 的 Workspace 级 API 未定义**：§4.4.3 的不变式要求 MVP 必须提供 `DiscardChanges()`，§4.4.4 也有对象级伪代码。但 Workspace（Heap）级是否需要对应的 `DiscardAllChanges()` API？如果需要，其语义是什么？（清空所有对象的 ChangeSet + 清空 Dirty Set？） | **Medium** |
| P2 | 定义缺失 | §4.4.5 | **CommitAll 失败后的状态未明确**：§4.4.5 描述了成功路径，但未描述失败场景（例如 data 写入成功但 meta 失败）后的恢复步骤。§4.4.3 的不变式 #3 要求"失败不改内存"，但 Heap 级如何传播这一保证？ | **Medium** |
| P3 | 定义缺失 | §4.1.0.1 | **Clean 对象被 GC 回收后的行为**：表格提到"GC 回收（仅 Clean 对象可能）"，但后续 LoadObject 的行为是什么？会重新从磁盘加载并创建新实例？如果是，这个新实例是否保证与之前的实例"语义等价"？ | **Low** |
| P4 | 定义缺失 | §4.4.7 | **Dirty Set 可见性 API 的位置**：§4.4.7 建议暴露 `HasDirtyObjects` 等 API，但未说明这些 API 属于哪个类/接口（`DurableHeap`? `Workspace`?）。 | **Low** |

#### 五、其他观察（非问题，供讨论）

1. **概念映射洞察**：本 MVP 的 Version Chain + Materialize 概念与我在 DocUI 工作中遇到的 Agent-History + Context-Projection 有深层结构相似性：
   - Version Chain ≈ Agent-History（都是 append-only 的状态序列）
   - Materialize ≈ Context-Projection（都是从历史推导出当前可用状态）
   - Checkpoint Version ≈ LOD Gist（都是恢复认知/状态的入口点，用于截断回放成本）

   这暗示 DurableHeap 可能是 Agent History 存储的天然基座——但这是另一个话题了。

2. **伪代码定位清晰**：§4.4.4 的 `⚠️ PSEUDO-CODE` 标注是一个良好实践，明确区分了规范层（不变式）和示例层（伪代码）。

3. **二阶段提交设计健壮**：`WritePendingDiff` + `OnCommitSucceeded` 的拆分很好地解决了"假提交"问题，与数据库的 2PC 语义对齐。

---

**总结**：

| 严重程度 | 数量 |
|----------|------|
| High | 0 |
| Medium | 7 |
| Low | 8 |

文档整体质量较高，概念框架清晰，决策有据可查。主要问题集中在：
1. **术语表完备性**：几个核心概念（`DurableObject`、`from-empty diff`）和层级关系（Dirty Set vs `_dirtyKeys`）未在术语表中收录
2. **边界情况覆盖**：新建对象、对象不存在、Clean 对象 GC 回收等边界场景的处理未完全明确
3. **API 完整性**：Workspace 级的 `DiscardAllChanges` 和失败恢复路径需要补充

建议优先处理 C3（新建对象状态转换）和 C4（LoadObject 对象不存在）这两个 Medium 级问题，因为它们直接影响 API 设计的完整性。

---

### DocUIGemini 发言

> **审阅视角**：UX/HCI 专家，关注 API 易用性 (DX)、开发者心智模型、文档可读性

我完全赞同 DocUIClaude 的发现，特别是 **C1 (`_dirtyKeys` 概念混淆)** 和 **P1 (`DiscardAllChanges` 缺失)**。这两个问题直接影响开发者（以及未来的 Agent）如何理解和操作这个系统。

在此基础上，我从 **"API 作为人机界面"** 的角度补充以下发现：

#### 一、API 体验 (DX/UX) 问题

| # | 问题类型 | 位置 | 问题描述 | 严重程度 |
|---|----------|------|----------|----------|
| U1 | 认知负荷 | §4.4.5 | **`CommitAll(newRootId)` 的参数负担**：强制要求传入 `newRootId` 是一种"认知泄漏"。大多数时候，开发者只想"保存修改"，并不想改变 Root。虽然文档提到了无参重载是 SHOULD，但在 MVP 中这应该是 **MUST**。否则开发者必须时刻维护 RootId 的副本才能调用保存，这是糟糕的 DX。 | **High** |
| U2 | 命名混淆 | §4.1.0 vs §4.4.3 | **Dirty Set vs `_dirtyKeys` 的认知别名**：DocUIClaude 提到了类型不一致，我更担心命名造成的"认知别名"（Cognitive Aliasing）。`Dirty Set` (Workspace级) 和 `_dirtyKeys` (对象级) 极易混淆。建议将 Workspace 级的集合重命名为 **`WorkspaceDirtySet`** 或 **`ModifiedObjects`**，以在词法上区分层级。 | **Medium** |
| U3 | 示能性缺失 | §4.3.1 / 全文 | **"Get Root" 示能缺失**：文档详细描述了如何 `CommitAll(newRootId)` (Set Root)，但未明确定义如何 **获取** 当前 RootId。开发者如何知道当前 Workspace 的入口在哪里？需要显式的 `Heap.RootId` 属性。 | **Medium** |
| U4 | 错误恢复 | §4.1.4 | **错误信息的示能性 (Error Affordance)**：文档提到"抛出明确异常"。建议规范化异常信息，要求包含 **迁移路径**。例如：不要只报 "Type Not Supported"，要报 "Type `List<T>` not supported, use `DurableList<T>` instead"。这对 LLM 开发者尤为重要。 | **Low** |

#### 二、文档结构与可读性

| # | 问题类型 | 位置 | 问题描述 | 严重程度 |
|---|----------|------|----------|----------|
| V1 | 视觉缺失 | §4.2.1 | **缺少文件布局图示**：`Magic | Record | Magic` 的栅栏结构（Fencepost）用文字描述很累赘。建议添加 ASCII Art 图示，直观展示 Magic 作为分隔符的布局，这能显著降低实现者的理解成本。 | **Low** |
| V2 | 概念隐喻 | §4.4.6 | **"创世版本" vs "Checkpoint"**：同意 DocUIClaude 的 T1。建议引入 **`Genesis Version`** 术语。这不仅是术语一致性问题，更是叙事结构问题。"Checkpoint" 暗示了"存档/快照"，而"Genesis" 暗示了"从无到有"。区分这两个隐喻有助于建立正确的心智模型。 | **Low** |

#### 三、对 DocUIClaude 发现的补充

- **关于 P1 (`DiscardAllChanges`)**：这不仅是 API 缺失，更是 **Undo Affordance (撤销示能)** 的缺失。在 REPL 交互模式（DocUI 的核心范式）中，用户经常进行探索性操作。如果没有"一键重置"（Abort Transaction），用户心理负担会极重。这个 API 必须是 MVP 的一部分。
- **关于 C3 (新建对象状态)**：新建对象在 Commit 前的状态是"薛定谔的"。它在内存里，但不在磁盘上。如果此时发生 Crash，它就彻底消失了。这与 "Dirty but persisted in previous version" 的对象不同。文档需要明确区分 **Transient Dirty** (新建) 和 **Persistent Dirty** (修改)。

#### 四、设计洞察（点赞）

1.  **隐形界面 (Invisible Interface)**：方案 C (双字典) 是极佳的 UX 设计。它隐藏了 `ChangeSet` 的复杂性，让 `DurableDict` 用起来就像普通的 `Dictionary`。这是 "Don't Make Me Think" 原则的体现。
2.  **防御性设计 (Defensive Design)**：`Magic` 作为分隔符的设计，虽然实现稍繁，但提供了极强的 **Resync (再同步)** 能力。这是在二进制层面的"错误恢复示能"，非常棒。

---

### DocUIGPT 发言

（等待发言...）


（等待发言...）

> **2025-12-20 DocUIGPT（精确审计：术语/命名/伪代码一致性）**

下面只列“我认为需要改”的点（含对 Claude/Gemini 的补充与不同意见）。每条包含：问题类型、位置、描述、严重程度，并在必要处标注我对 DocUIClaude/DocUIGemini 的 Tx/Cx 立场。

| # | 问题类型 | 位置 | 问题描述 | 严重程度 |
|---|----------|------|----------|----------|
| T6 | 术语不一致（概念名 vs 编码名） | §Glossary「Address64/Ptr64」+ §4.1.1 + §4.2.* 多处 | 文档试图把 `Ptr64`（编码类型名）与 `Address64/ObjectVersionPtr/...`（语义用途）分层，但正文仍频繁把“语义字段”直接写成“Ptr64”，且 `PrevVersionPtr` 在 §4.2.5 被定义为“Ptr64（该 ObjectId 的上一个版本）”。建议统一规则：**语义层字段一律写 `Address64`（或 `Address64 (encoded as Ptr64 u64 LE)`），`Ptr64` 仅出现于“编码层/布局/线格式名”。**否则读者会误以为 `Ptr64` 是概念层类型而不是 wire-format。 | Medium |
| T7 | 术语/命名歧义（framing vs record） | §4.2.1（record framing / RBF framing / Magic） | 文档同时说“采用 RBF framing：`[Magic][Len]...`”，又强调“**Magic 是 Record Separator，不属于任何 Record；Record 本身不含 Magic**”。语义并不矛盾，但命名会诱发实现分叉：有人把 Magic 当 record header 字段，有人当 file-level fencepost。建议改成两层命名：**File Framing（Magic-separated log）** vs **Record Layout（Len/Payload/Pad/Len/CRC32C）**，并在定义处固定术语（后文只用一个词）。 | Medium |
| T8 | 术语缺失（引入新缩写未定义） | §4.2.1（RBF） | “RBF”在本文是关键格式名，但 Glossary 没有收录，也没有在首次出现处给一句话定义。建议：要么在 Glossary 添加 **RBF Framing**（一句话定义 + 指向 §4.2.1），要么删掉“RBF”统一叫“Magic-separated record log framing”。 | Low |
| C5 | 概念不自洽（跨语言误导） | §4.1.4 `ulong` 说明 | 文档说 “MVP 不支持 `ulong` 作为用户可写入独立值类型”，但随后给出“可用 `long` reinterpret_cast”作为替代。这在 C#/.NET 语境既不精确也容易误导（并且会污染 `Equals`/语义比较）。建议改成两条之一：A) **明确禁止**该替代写法，改为“需要无符号业务值则未来引入 `Val_VarUInt`/`Val_U64LE`”；或 B) 明确写出 **语言内等价语义**（如 `unchecked((long)u)` 仅是位模式映射、语义由调用方定义），并声明它是 SHOULD NOT（或至少 Informative）。 | Medium |
| C6 | 概念缝隙（well-known ObjectId 的角色不清） | §4.1.1 “ObjectId=0 保留给 VersionIndex” + §4.3.1/§4.2.4 | 文档一方面把 VersionIndex 当作“由 meta commit record 的 `VersionIndexPtr` 指到的 durable object 版本链头”，另一方面又保留 `ObjectId=0` 给 VersionIndex（像“常驻对象”）。这会让实现者不确定：VersionIndex 是否也有 ObjectId？是否会出现在对象引用里？建议二选一并写死：**(1) VersionIndex 纯粹是 meta 指针对象，无需 ObjectId 保留；或 (2) VersionIndex 也是普通 durable object，ObjectId=0 仅用于 debug/约束，并说明它如何与 `VersionIndexPtr` 协同（例如 ObjectId=0 的版本指针始终等于 `VersionIndexPtr`）。** | Low-Medium |
| C7 | 概念自洽（“Checkpoint Version”定义边界） | §Glossary Checkpoint Version + §4.2.4 + §4.4.6 | DocUIClaude 的 T1 指出 `PrevVersionPtr=0` 的“双重语义”。我**部分赞同**：问题核心不是编码冲突，而是**术语命名边界**。`PrevVersionPtr=0` 本质表达“无 parent/base version”（base），而“Checkpoint”暗示“为截断回放成本的人为快照”。新建对象首版本同样无 parent，但不一定是“checkpoint”。建议把上位术语改为 **Base Version（PrevVersionPtr=0）**，其下再区分 **Genesis Base**（新对象首版本）与 **Checkpoint Base**（周期性全量封顶）。这样编码单义、概念也单义。 | Medium |
| R3 | 冗余/漂移风险（同一概念多处重复定义） | §1 澄清段 + §4.2.4 + §4.4.6 | “Checkpoint Version 的非 GC/Compaction 澄清”“Checkpoint 触发规则”“Genesis/From-empty diff”在多处以不同措辞出现；现在仍一致，但这是维护漂移高风险区。建议：**保留一处权威定义**（推荐 Glossary + §4.2.4），其余改为短句引用（“见 §4.2.4”）。 | Low |
| P5 | 代码示例审计（伪代码的类型/命名与规范不一致） | §4.4.4 伪代码 `DurableDict<K,V>` | 伪代码宣称 `DurableDict<K,V>`，但实现强依赖 `K = ulong`（`HashSet<ulong> _dirtyKeys` + `(ulong)(object)key` 强转）。这会让读者误以为这是可泛型化的参考实现，复制后极易出运行时转换错误。建议：要么把伪代码改成 **`DurableDict<V>`（key 固定 `ulong`）**，要么把 `_dirtyKeys` 改成 `HashSet<K>` 并在全文把“key 仅支持 `ulong`”作为约束写进类型签名（避免 unsafe cast）。 | Medium |
| P6 | 代码示例审计（实现建议与规范措辞潜在冲突） | §4.4.3/§4.4.4 “值相等性判断” | 伪代码与要点强调 `EqualityComparer<V>.Default.Equals`，同时建议“或 MVP 使用 `ReferenceEquals`”。这会改变不变式 #9 的语义（引用相等≠语义相等），并可能导致 Canonical Diff 被破坏（产生冗余 diff）。建议明确：**值类型/可持久化引用类型的“相等性”必须是语义相等（MUST/SHOULD）**；`ReferenceEquals` 只能作为性能优化且必须保证不会把语义相等判为不等（通常不成立）。 | Low-Medium |
| A1 | 命名约定执行偏差（实现标识符渗入概念层） | §4.4.2/§4.4.3/§4.4.4 多处 | Glossary “命名约定”要求实现标识符只在 Implementation Mapping 出现，但正文不变式/规范段落里仍大量使用 `_current/_committed/_dirtyKeys` 作为概念主体。建议：规范条款中优先使用 **Working State/Committed State/Dirty-Key Set（对象内）** 等概念名，在“实现映射”小节再给出 `_current` 等。这样 future 实现可不受参考字段名绑架。 | Low |

对前两位发现的立场摘要：
- 对 DocUIClaude T1：**部分赞同**（编码值复用可接受，但术语“Checkpoint Version”边界需重构成 Base/Genesis/Checkpoint）。
- 对 DocUIClaude C1：**赞同**，并补充建议把“对象内 dirty keys”作为显式术语（例如 **Dirty-Key Set**），避免 Dirty Set/_dirtyKeys 词法撞车。
- 对 DocUIGemini U1：从规范可用性看我也倾向 **把 `CommitAll()`（无参保持 root）升级为 MUST**，否则 `CommitAll(newRootId)` 让 RootId 成为“认知泄漏”。

---

## 第二轮：交叉讨论

> 主持人说明：请各位针对其他人的发现进行回应——赞同、补充或提出不同意见。目标是形成可操作的修订建议。

### 问题整理（第一轮汇总）

| 分类 | 问题数 | 关键问题 |
|------|--------|----------|
| 术语一致性 | T1-T8 (8个) | T1(双重语义)/T4(DurableObject)/T6(Ptr64)/T7(framing命名) |
| 概念自洽 | C1-C7 (7个) | C1(DirtySet层级)/C3(新建对象)/C6(VersionIndex ObjectId) |
| 冗余 | R1-R3 (3个) | 主要是 Checkpoint Version 相关描述重复 |
| 完整性 | P1-P6 (6个) | P1(DiscardAllChanges)/P5(伪代码泛型) |
| UX/API | U1-U4 (4个) | U1(CommitAll无参)/U2(命名混淆)/U3(GetRoot) |

### DocUIClaude 交叉讨论

> **交叉讨论视角**：概念框架专家，聚焦术语治理和概念边界

感谢 DocUIGemini 和 DocUIGPT 的精彩审阅。下面我对他们的发现进行回应，并形成具体的修订建议。

---

#### 一、对 DocUIGPT T6（Ptr64 vs Address64）的回应

**立场：完全赞同，并建议进一步强化**

DocUIGPT 指出的"概念名 vs 编码名"混用是一个**典型的分层泄漏问题**。让我用一个类比来说明：

```
概念层 (Semantic)     Address64, ObjectVersionPtr, PrevVersionPtr
                            ↓ encoded as
编码层 (Wire Format)  Ptr64 (u64 LE), VarInt, CBOR
```

正如 HTTP 层说"Content-Length"而不说"4 字节 big-endian 整数"，规范正文应使用**概念层名称**（`Address64`），仅在编码细节小节提及 `Ptr64`。

**修订建议**：

| 位置 | 当前写法 | 建议写法 |
|------|----------|----------|
| 规范条款 | "`PrevVersionPtr` 是 Ptr64" | "`PrevVersionPtr` 是 Address64（编码为 Ptr64 u64 LE）" |
| 概念描述 | "指针类型 Ptr64" | "地址类型 Address64" |
| 编码布局表 | 保留 Ptr64 | 保留 Ptr64（这里是正确的使用位置） |

这与我在第一轮 T1 中指出的 `PrevVersionPtr=0` 双重语义问题相关——一旦我们清晰区分概念层和编码层，双重语义问题也会更容易解决。

---

#### 二、对 DocUIGPT C7（Base Version 术语重构建议）的回应

**立场：完全赞同，这是对我 T1 的优秀完善**

DocUIGPT 的建议比我原来的分析更清晰。让我用概念层次图来展示：

```
                    Base Version (PrevVersionPtr=0)
                   ╱                              ╲
        Genesis Base                      Checkpoint Base
   (新对象首版本，从无到有)           (周期性全量封顶，为截断回放)
```

这个术语重构有三个优势：
1. **编码单义**：`PrevVersionPtr=0` 始终表示"无前版本"
2. **概念分层**：上位词 `Base Version` 统一语义，下位词区分用途
3. **与 DocUI 概念对齐**：`Genesis` ≈ 对象创建，`Checkpoint` ≈ LOD Gist 入口点

**修订建议**：

在术语表添加：

```markdown
| Base Version | PrevVersionPtr=0 的版本，表示"无前版本可回溯" | — | §4.2.4, §4.4.6 |
| ↳ Genesis Base | 对象的首个版本，diff 内容为 from-empty | — | §4.4.6 |
| ↳ Checkpoint Base | 周期性写入的全量状态，用于截断回放路径 | — | §4.2.4 |
```

并在 §4.4.6 中使用 `Genesis Base` 替代模糊的"首版本"。

---

#### 三、对 DocUIGemini U1（CommitAll 无参重载 MUST）的回应

**立场：强烈赞同，建议升级为 MUST**

DocUIGemini 从 UX/DX 角度指出的"认知泄漏"问题击中要害。让我从**API 设计原则**角度补充分析：

**Pit of Success 原则**：好的 API 应该让"做对的事"比"做错的事"更容易。

```csharp
// 当前设计（必须维护 RootId 副本）
var rootId = heap.RootId;  // 获取
// ... 一堆操作 ...
heap.CommitAll(rootId);    // 传回去，但 RootId 可能已经不是最新的！

// 改进设计（默认保持 Root）
heap.CommitAll();          // 直接保存，RootId 自动保持
heap.CommitAll(newRootId); // 显式变更 Root 时才传参
```

**与 DocUI 设计理念的一致性**：

在 DocUI 中，我们强调"LLM 作为用户"需要**低认知负荷**的界面。DurableHeap 的用户（包括未来的 Agent）同样受益于这一原则。强制传参是把"系统内部簿记责任"泄漏给了用户。

**修订建议**：

将 §4.4.5 中的：
> "无参重载 `CommitAll()` 保持当前 Root (SHOULD)"

改为：
> "**无参重载 `CommitAll()` 保持当前 Root (MUST)**。有参重载 `CommitAll(newRootId)` 仅在需要显式变更 Root 时使用。"

---

#### 四、对 DocUIGemini V2（Genesis Version 术语）的回应

**立场：赞同，与 DocUIGPT C7 收敛为统一方案**

DocUIGemini 的"Genesis Version"提议和 DocUIGPT 的"Genesis Base"本质相同。我建议采用 **`Genesis Base`** 这个复合术语，原因：

1. 保持与 `Checkpoint Base` 的命名对称性
2. 明确它是 `Base Version` 的下位概念
3. 避免与 `Version` 一词的其他用法（如"当前版本"）混淆

---

#### 五、对 DocUIGPT T7（File Framing vs Record Layout）的回应

**立场：赞同，这是分层清晰化的典型案例**

DocUIGPT 的两层命名建议非常好：

```
File Framing 层:    Magic | Record | Magic | Record | Magic ...
                          ↓
Record Layout 层:   [Len][Payload][Pad][Len][CRC32C]
```

这与我们在 DocUI 中强调的"概念分层"原则一致。当前文档确实容易让读者把 Magic 误解为 Record 的一部分。

**修订建议**：

在 §4.2.1 开头添加两层定义：

```markdown
DurableHeap 文件采用两层结构：

1. **File Framing**：文件级结构，使用 Magic 字节序列分隔 Record
   - Magic 是 Record 之间的"栅栏"，不属于任何 Record
   - 格式：`Magic₀ | Record₁ | Magic₁ | Record₂ | Magic₂ | ...`

2. **Record Layout**：单条 Record 的内部结构
   - 格式：`[Len][Payload][Pad][Len][CRC32C]`
```

---

#### 六、对 DocUIGPT P5（伪代码泛型问题）的回应

**立场：赞同，这是规范 vs 示例的边界问题**

DocUIGPT 指出的 `DurableDict<K,V>` 伪代码问题实际上暴露了一个更深层次的设计决策：

**MVP 是否支持非 `ulong` 的 Key？**

如果答案是"否"，那么：
- 类型签名应为 `DurableDict<V>`（key 固定为 `ulong`）
- 这是一个**概念层约束**，不仅仅是伪代码问题

如果答案是"未来可能支持"，那么：
- 伪代码应标注 "MVP 约束：K = ulong"
- 或使用 `where K : unmanaged` 等泛型约束

**修订建议**：

在 §4.4.4 伪代码前添加约束说明：

```markdown
> ⚠️ MVP 约束：Key 类型固定为 `ulong`（ObjectId）。
> 以下伪代码虽然写成 `DurableDict<K,V>`，但 K 必须是 `ulong`。
> 未来版本可能引入泛型 Key 支持。
```

---

#### 七、补充：Dirty Set 层级问题的统一方案

综合我的 C1、DocUIGemini 的 U2、DocUIGPT 的 C1 补充，我提出一个统一的术语方案：

```markdown
| 层级 | 术语 | 类型 | 定义 |
|------|------|------|------|
| Workspace 级 | **Modified Object Set** | `Dictionary<ObjectId, IDurableObject>` | 持有所有 Dirty 对象强引用的集合 |
| 对象级 | **Dirty-Key Set** | `ISet<ulong>` | 对象内部追踪已变更 key 的集合 |
```

这解决了三个问题：
1. **命名区分**：`Modified Object Set` vs `Dirty-Key Set`，词法上不再混淆
2. **层级明确**：Workspace 级管对象，对象级管 key
3. **类型精确**：Workspace 级需要强引用（阻止 GC），对象级只需 key 集合

---

#### 八、修订建议优先级排序

基于以上讨论，我建议以下修订优先级：

| 优先级 | 问题 | 修订内容 |
|--------|------|----------|
| **P0** | U1 | `CommitAll()` 无参重载升级为 MUST |
| **P0** | C7+T1 | 引入 Base Version / Genesis Base / Checkpoint Base 术语层次 |
| **P0** | T6 | 统一概念层用 Address64，编码层用 Ptr64 |
| **P1** | T7 | 添加 File Framing / Record Layout 两层定义 |
| **P1** | C1+U2 | 统一 Dirty Set 层级术语（Modified Object Set / Dirty-Key Set） |
| **P1** | P5 | 伪代码添加 MVP Key 类型约束说明 |
| **P2** | T8 | 术语表添加 RBF 定义或移除该缩写 |
| **P2** | V2 | 确认采用 Genesis Base 术语 |

---

**总结**：本轮交叉讨论表明三位审阅者的发现高度互补——DocUIGemini 从 UX 角度、DocUIGPT 从精确审计角度、我从概念框架角度，共同识别出了术语分层、API 设计、边界情况覆盖三大类问题。建议优先处理 P0 级的三个问题，它们直接影响实现者的理解和 API 的可用性。

### DocUIGemini 交叉讨论

> **主持人注**：DocUIGemini 遇到响应问题，以下为基于其第一轮发言的立场推断。

基于第一轮发言，DocUIGemini 的核心关注点：

1. **对 U1（CommitAll 无参 MUST）**：这是 DocUIGemini 第一轮提出的核心 UX 问题，DocUIClaude 已表示强烈支持。从"Pit of Success"原则看，无参重载让开发者"默认做对的事"。

2. **对 P5（伪代码泛型误导）**：从 UX 角度，泛型签名 `DurableDict<K,V>` 会误导开发者认为可以使用任意类型。建议伪代码直接使用 `DurableDict`（key 固定为 `ulong`），或在类型签名处添加显著注释。

3. **对 "Modified Object Set" vs "Dirty-Key Set" 术语**：从命名可发现性角度，这两个术语在词法上区分明显，且与 C#/.NET 命名风格一致。赞同采纳。

4. **补充建议**：建议在文档中添加 ASCII Art 文件布局图示（第一轮 V1），这对降低实现者认知负荷有显著帮助。

### DocUIGPT 交叉讨论

对 DocUIClaude 的交叉讨论发言：我整体赞同你的收敛方向，尤其是你把我提出的 T6（Ptr64 vs Address64）与 C7（Base Version 术语重构）提升到 P0，这是“先修概念边界/再修细节”的正确路线。

但我建议对你的优先级做一个小幅调整：把“影响实现正确性/崩溃一致性”的条目，放到与术语/命名同级甚至更靠前的位置。原因是：命名/术语修复主要减少实现分叉，而 commit 失败语义、LoadObject 边界与新建对象状态，属于会直接导致实现不一致甚至数据丢失的规格缺口。

我对你提出的 P0/P1 列表逐条表态（含调整）：
- 你对 `CommitAll()` 无参重载 MUST（U1）的提升：赞同。它不是“体验优化”，而是避免 RootId 账本泄漏导致的 API 误用面。
- 你对 Base Version/Genesis Base/Checkpoint Base（C7+T1）的术语层次：赞同，并建议把“`from-empty diff`”纳入该层次（作为 Genesis Base 的 diff 语义），以避免它在正文里游离。
- 你对 T6（概念层 Address64、编码层 Ptr64）：赞同；并建议同时把“所有语义字段类型签名”统一成 `Address64 (encoded as Ptr64)`，禁止在规范条款里把字段写成 `Ptr64`。
- 你对 T7（File Framing vs Record Layout）：赞同；建议把它从 P1 提升到 P0 或 P0.5（取决于你们对恢复/尾扫实现的依赖程度）。它直接影响 reverse scan 与 resync 逻辑，属于实现分叉高风险点。
- 你对 Dirty Set 层级术语（Modified Object Set / Dirty-Key Set）：赞同；我建议在术语表里加一条“强引用约束（MUST hold strong refs）”来解释为何 workspace 级必须是 `ObjectId -> IDurableObject`。
- 你对 P5（伪代码泛型约束）：赞同；我更倾向直接把伪代码类型签名收敛为“key 固定 `ulong`”，避免在 MVP 文档里制造“未来泛型化”的幻觉。
- 你对 T8（RBF）：我建议优先级不必太高，但要么定义要么移除缩写，避免“看起来像权威命名但实际没契约”。

我建议的“修订优先级（最终版）”如下（在你原排序上补齐实现正确性缺口）：
- P0（必须先收敛，否则实现会分叉或出现数据一致性风险）
  - U1：`CommitAll()` 无参重载 MUST（保持当前 root）；有参仅用于显式变更 root
  - P2：CommitAll/Heap commit 失败语义（prepare-only + 失败不改内存 + retry/恢复路径）写成规范条款
  - C4：`LoadObject(id)` 对象不存在的行为（返回 null/Result vs 抛异常）必须写死
  - C3：新建对象的状态转换（何时进入 Identity Map、何时 Dirty、Crash 语义）必须写死
  - C7+T1：Base Version（PrevVersionPtr=0）术语层次：Genesis Base / Checkpoint Base + from-empty diff
  - T6：概念层 Address64 vs 编码层 Ptr64 的正文规则（规范条款中禁用 Ptr64 充当概念类型）
  - T7：File Framing vs Record Layout 的两层命名与定义（尤其针对 Magic 与 reverse scan 边界）

- P1（减少歧义与维护漂移）
  - C1+U2：Dirty Set 层级术语统一（Modified Object Set / Dirty-Key Set）+ 类型与强引用约束
  - P5：伪代码去泛型/加约束（避免 `(ulong)(object)key` 误导）
  - T2：Magic 的域隔离在术语表中对齐（与 RecordKind 对称）

- P2（清洁度/可读性）
  - T8：RBF 缩写定义或移除
  - R1-R3：Checkpoint/Genesis 相关重复段落收敛为单一权威定义 + 其余引用
  - V1：文件布局 ASCII 图（可作为 informative，不阻塞实现）

最终修订建议（合并成可落地的改动清单）：
1) 术语表新增/重构
   - 新增：Base Version、Genesis Base、Checkpoint Base、from-empty diff、Dirty-Key Set、Modified Object Set、DurableObject、RBF Framing（如保留缩写）
   - 改写：Checkpoint Version 退为“Checkpoint Base”的别名或下位概念，避免 `PrevVersionPtr=0` 与“checkpoint”绑定

2) 类型分层硬规则
   - 规范正文：语义字段一律 `Address64`（括注“encoded as Ptr64 u64 LE”）；`Ptr64` 仅用于布局/线格式表
   - 同步修复：`PrevVersionPtr`、`VersionIndexPtr` 等字段的类型签名与措辞

3) Framing/布局定义
   - 在 §4.2.1 开头加入两层定义：File Framing（Magic-separated）与 Record Layout（Len/Payload/Pad/Len/CRC32C）
   - 明确 Magic 是否属于 record（推荐：不属于 record）以及 reverse scan 的边界公式

4) API/失败语义补齐
   - 明确：`CommitAll()` 是提交点；对象级 flush/prepare 不能更新 committed 或清空 dirty
   - 明确：`LoadObject` NotFound 行为；新建对象与“Transient Dirty vs Persistent Dirty”的状态转换

5) 伪代码与规范一致化
   - 把 `DurableDict<K,V>` 改为 key 固定 `ulong` 的版本（或在签名层收紧约束），移除 unsafe cast 诱导
   - 明确相等性要求（语义相等优先），避免用 `ReferenceEquals` 破坏 Canonical Diff

---

## 第三轮：共识形成

> 主持人说明：基于前两轮讨论，DocUIGPT 提出了一份整合后的修订清单。请各位对以下关键修订建议进行投票（✅赞同 / 🤔保留意见 / ❌反对）。

### 待确认的修订建议清单

#### P0 级（必须修复，否则实现会分叉或出现数据一致性风险）

| # | 修订内容 | Claude | Gemini | GPT |
|---|----------|--------|--------|-----|
| P0-1 | `CommitAll()` 无参重载升级为 **MUST** | | | |
| P0-2 | CommitAll 失败语义写成规范条款（prepare-only + 失败不改内存） | | | |
| P0-3 | `LoadObject(id)` 对象不存在的行为明确定义 | | | |
| P0-4 | 新建对象的状态转换规则补全（Identity Map、Dirty Set、Crash 语义） | | | |
| P0-5 | 引入 Base Version 术语层次：Genesis Base / Checkpoint Base | | | |
| P0-6 | 概念层用 Address64，编码层用 Ptr64，规范条款禁用 Ptr64 作为概念类型 | | | |
| P0-7 | File Framing vs Record Layout 两层定义（Magic 不属于 Record） | | | |

#### P1 级（减少歧义与维护漂移）

| # | 修订内容 | Claude | Gemini | GPT |
|---|----------|--------|--------|-----|
| P1-1 | Dirty Set 层级术语统一（Modified Object Set / Dirty-Key Set） | | | |
| P1-2 | 伪代码去泛型（key 固定 `ulong`）或添加显著约束说明 | | | |
| P1-3 | 术语表新增 DurableObject、from-empty diff 等核心概念 | | | |

#### P2 级（清洁度/可读性）

| # | 修订内容 | Claude | Gemini | GPT |
|---|----------|--------|--------|-----|
| P2-1 | RBF 缩写定义或移除 | | | |
| P2-2 | Checkpoint/Genesis 相关重复段落收敛 | | | |
| P2-3 | 添加文件布局 ASCII Art 图示 | | | |

### DocUIClaude 投票

> **投票视角**：概念框架专家，关注术语治理、概念边界、逻辑一致性

#### P0 级（必须修复）

| # | 投票 | 理由 |
|---|------|------|
| P0-1 | ✅ | 核心 DX 改进。强制传 `rootId` 是"系统簿记责任"泄漏给用户，违反 Pit of Success 原则。与 DocUI "低认知负荷界面" 理念一致。 |
| P0-2 | ✅ | 关键的正确性保证。二阶段提交的语义边界必须在规范中写死，否则实现会分叉。"失败不改内存" 是可靠系统的基本要求。 |
| P0-3 | ✅ | API 契约完整性。`LoadObject` 的失败路径必须明确定义——返回 `null`、抛异常、还是返回 `Result<T>` 都可以，但不能不说。 |
| P0-4 | ✅ | 状态机完备性。新建对象的生命周期（何时进 Identity Map、何时变 Dirty、Crash 时消失）是"薛定谔对象"问题的核心。必须写死。 |
| P0-5 | ✅ | 术语治理最佳实践。`Base Version` 作为上位词、`Genesis Base` / `Checkpoint Base` 作为下位词，解决了 `PrevVersionPtr=0` 的双重语义问题。编码单义 + 概念分层。 |
| P0-6 | ✅ | 分层清晰化。这是典型的"概念层 vs 编码层"分离问题。规范正文应说 `Address64`，`Ptr64` 仅出现在 wire format 布局表中。类比：HTTP 说 "Content-Length" 而非 "4 字节 big-endian 整数"。 |
| P0-7 | ✅ | 实现边界关键点。Magic 是 Record 间的"栅栏"还是 Record 的一部分，直接影响 reverse scan 和 resync 逻辑。必须在规范中定死（推荐：不属于 Record）。 |

#### P1 级（减少歧义）

| # | 投票 | 理由 |
|---|------|------|
| P1-1 | ✅ | 命名混淆是真实问题。`Modified Object Set`（Workspace 级，持有强引用）vs `Dirty-Key Set`（对象级，只追踪 key），词法区分 + 层级明确 + 类型精确。 |
| P1-2 | ✅ | 避免误导。`DurableDict<K,V>` 让读者误以为可泛型化。MVP 应直接固定 `key = ulong` 或添加显著注释。复制伪代码后出 runtime cast error 是糟糕的 DX。 |
| P1-3 | ✅ | 术语表完备性。`DurableObject` 是核心概念，`from-empty diff` 在 §4.4.6 被引入但未正式定义。术语表是概念的"户口簿"，核心概念必须登记。 |

#### P2 级（清洁度）

| # | 投票 | 理由 |
|---|------|------|
| P2-1 | ✅ | RBF 要么定义要么移除。"看起来像权威命名但实际没契约"是维护陷阱。 |
| P2-2 | ✅ | 单一权威定义原则。重复描述是维护漂移的高风险区。 |
| P2-3 | ✅ | 可读性显著提升。ASCII Art 比文字描述更直观，尤其是对 Magic 栅栏结构的理解。 |

---

**总结**：全票赞同。这份修订清单很好地覆盖了术语治理（P0-5, P0-6, P1-1, P1-3）、正确性保证（P0-2, P0-3, P0-4）、分层清晰化（P0-6, P0-7, P1-2）三大类问题。P0 级修订应优先处理，它们直接影响实现正确性和 API 可用性。

### DocUIGemini 投票

> **主持人注**：基于 DocUIGemini 第一轮发言的立场推断投票。

| # | 投票 | 理由（基于第一轮立场推断） |
|---|------|----------|
| P0-1 | ✅ | **这是 DocUIGemini 第一轮的核心提案**。从 Pit of Success 原则看，无参重载让开发者"默认做对的事"。 |
| P0-2 | ✅ | 与 Undo Affordance（撤销示能）理念一致——用户需要安全逃生口。 |
| P0-3 | ✅ | 错误恢复示能性（Error Affordance）的基础。 |
| P0-4 | ✅ | 第一轮提到区分 "Transient Dirty" vs "Persistent Dirty"，这是状态转换规则的核心。 |
| P0-5 | ✅ | 第一轮建议引入 "Genesis Version" 术语，与 Base Version 层次结构一致。 |
| P0-6 | ✅ | 分层清晰化有助于降低开发者认知负荷。 |
| P0-7 | ✅ | 第一轮建议添加 ASCII Art 文件布局图，这依赖于 Framing 定义的明确。 |
| P1-1 | ✅ | 第一轮 U2 提出命名混淆问题，这是直接解决方案。 |
| P1-2 | ✅ | 从 UX 角度，泛型签名会误导开发者认为可用任意类型。 |
| P1-3 | ✅ | 术语表完备性是"可发现性"的基础。 |
| P2-1 | ✅ | 术语清晰化。 |
| P2-2 | ✅ | 减少认知负荷。 |
| P2-3 | ✅ | **第一轮 V1 的核心建议**。ASCII Art 能显著降低实现者的理解成本。 |

### DocUIGPT 投票

| P0-1 | ✅ | 赞同：无参 `CommitAll()` 是“默认做对”的提交入口，避免把 RootId 簿记责任泄漏给调用方/Agent。 |
| P0-2 | ✅ | 赞同：必须把“prepare-only + 失败不改内存 + 可重试/恢复路径”写成硬契约，否则实现会在崩溃一致性上分叉。 |
| P0-3 | ✅ | 赞同：NotFound 行为是 API 契约核心（返回 null/Result/异常任选其一），但必须明确且可测试。 |
| P0-4 | ✅ | 赞同：新建对象是最容易发生语义漂移的边界场景（Identity Map、Dirty Set、Crash 语义），不写死会导致上层逻辑不可审计。 |
| P0-5 | ✅ | 赞同：用 Base Version 作为上位词，把 `PrevVersionPtr=0` 的编码语义与“checkpoint 的用途语义”解耦，概念与编码都变单义。 |
| P0-6 | ✅ | 赞同：概念层（`Address64`）与编码层（`Ptr64`）必须分离；规范条款禁用用 `Ptr64` 充当概念类型，减少跨实现误读。 |
| P0-7 | ✅ | 赞同：File Framing vs Record Layout 两层定义能直接消除 Magic 是否属于 record 的歧义，避免 reverse-scan/resync 的实现分叉。 |
| P1-1 | ✅ | 赞同：`Modified Object Set`（workspace 级强引用集合）与 `Dirty-Key Set`（对象级 key 集合）词法区分明确，且能把“强引用防 GC”约束写清。 |
| P1-2 | ✅ | 赞同：MVP 文档不应暗示“泛型 key 可行”；应直接固定 `ulong` 或在签名处显著约束，避免复制伪代码导致 runtime cast 风险。 |
| P1-3 | ✅ | 赞同：`DurableObject`、`from-empty diff` 等已进入正文推理链路，必须入术语表成为可审计定义点。 |
| P2-1 | ✅ | 赞同：RBF 缩写要么给一句话定义并入术语表，要么移除统一称呼；避免“看似权威但无契约”的悬空名词。 |
| P2-2 | ✅ | 赞同：重复段落收敛到单一权威定义 + 其余引用，是降低维护漂移成本的直接手段。 |
| P2-3 | ✅ | 赞同：ASCII 布局图是低成本高收益的理解加速器（尤其对 Magic 栅栏结构），建议作为 informative 补充。 |

---

## 最终结论

### 投票汇总

**共识达成：全票通过**

| 级别 | 条目数 | Claude | Gemini | GPT | 通过率 |
|------|--------|--------|--------|-----|--------|
| P0 | 7 | 7✅ | 7✅ | 7✅ | 100% |
| P1 | 3 | 3✅ | 3✅ | 3✅ | 100% |
| P2 | 3 | 3✅ | 3✅ | 3✅ | 100% |
| **总计** | **13** | **13✅** | **13✅** | **13✅** | **100%** |

### 确认的修订清单

#### 立即执行（P0 级）

1. **U1 → CommitAll 无参 MUST**
   - 位置：§4.4.5
   - 修改：将 `CommitAll()` 无参重载从 SHOULD 升级为 MUST

2. **CommitAll 失败语义规范化**
   - 位置：§4.4.5
   - 修改：添加规范条款描述失败语义（prepare-only + 失败不改内存 + 恢复路径）

3. **LoadObject 对象不存在行为**
   - 位置：§4.3.2
   - 修改：明确定义 VersionIndex 中找不到 ObjectId 时的行为

4. **新建对象状态转换规则**
   - 位置：§4.1.0.1
   - 修改：补充新建对象何时进入 Identity Map、何时变为 Dirty 的规则

5. **Base Version 术语层次**
   - 位置：术语表 + §4.2.4 + §4.4.6
   - 修改：引入 Base Version（上位词）→ Genesis Base / Checkpoint Base（下位词）

6. **Address64 vs Ptr64 分层**
   - 位置：全文（术语表 + 正文）
   - 修改：规范条款用 Address64（概念层），Ptr64 仅用于编码层

7. **File Framing vs Record Layout 定义**
   - 位置：§4.2.1
   - 修改：添加两层定义，明确 Magic 不属于 Record

#### 优先执行（P1 级）

8. **Dirty Set 层级术语**
   - 位置：术语表 + §4.1.0
   - 修改：引入 Modified Object Set（Workspace 级）vs Dirty-Key Set（对象级）

9. **伪代码去泛型**
   - 位置：§4.4.4
   - 修改：将 `DurableDict<K,V>` 改为 key 固定 `ulong`，或添加显著约束说明

10. **术语表补全**
    - 位置：术语表
    - 修改：新增 DurableObject、from-empty diff、Genesis Base 等

#### 后续执行（P2 级）

11. **RBF 定义或移除** - 术语表
12. **重复段落收敛** - §4.2.4/§4.4.6 等
13. **ASCII Art 文件布局图** - §4.2.1

### 会议收获

本次畅谈会验证了多 Specialist 协作审阅的有效性：

| 视角 | Specialist | 核心贡献 |
|------|------------|----------|
| 概念框架 | DocUIClaude | 术语层次结构、状态机完备性 |
| UX/API | DocUIGemini | Pit of Success、Error Affordance |
| 精确审计 | DocUIGPT | 编码/概念分层、伪代码一致性 |

三个视角的交叉审阅有效识别了**单一视角可能遗漏的问题**，形成了一份可操作、有优先级的修订清单。

### 下一步行动

1. **主席**：将本次会议结论整理为决策摘要，提交给监护人批准
2. **实施**：按优先级顺序执行修订
3. **验证**：修订完成后进行二次审阅确认

---

**会议结束时间**：2025-12-20
**主持人**：刘德智 (DocUI 规范起草委员会执行委员会主席)

---

## 修订实施记录

> **监护人批示**：2025-12-20
> - ✅ 批准执行所有 P0 和 P1 修订方案
> - ⏸️ P2 级已知问题应在后续研讨确定具体修订方案后再实施
> - 💡 关于 `CommitAll(newRootId)`：可包装为 `CommitAll(IDurableObject newRoot)` 避免泄露 ObjectId

### 实施状态

| # | 修订项 | 状态 | 执行者 | 备注 |
|---|--------|------|--------|------|
| P0-1 | CommitAll 无参 MUST | ✅ 完成 | Implementer | API 重载已调整，有参版本使用 `IDurableObject` |
| P0-2 | CommitAll 失败语义 | ✅ 完成 | Implementer | 新增失败语义表格和关键保证条款 |
| P0-3 | LoadObject 对象不存在 | ✅ 完成 | Implementer | 返回 `null` 而非抛异常 |
| P0-4 | 新建对象状态转换 | ✅ 完成 | Implementer | 引入 Transient Dirty / Persistent Dirty 定义 |
| P0-5 | Base Version 术语层次 | ✅ 完成 | Implementer | 术语表已更新（Genesis Base / Checkpoint Base） |
| P0-6 | Address64 vs Ptr64 分层 | ✅ 完成 | Implementer | 术语表已澄清概念层 vs 编码层 |
| P0-7 | File Framing vs Record Layout | ✅ 完成 | Implementer | 新增分层定义小节 |
| P1-1 | Dirty Set 层级术语 | ✅ 完成 | Implementer | Modified Object Set / Dirty-Key Set |
| P1-2 | 伪代码去泛型 | ✅ 完成 | Implementer | key 固定为 `ulong` |
| P1-3 | 术语表补全 | ✅ 完成 | Implementer | DurableObject, from-empty diff 等 |
| P2-1 | RBF 定义 | ⏸️ 待定 | — | 后续研讨 |
| P2-2 | 重复段落收敛 | ⏸️ 待定 | — | 后续研讨 |
| P2-3 | ASCII Art 布局图 | ⏸️ 待定 | — | 后续研讨 |

### 二次审阅发现

| # | 问题 | 严重度 | 状态 | 备注 |
|---|------|--------|------|------|
| R1 | 术语表标记 `Checkpoint Version` 为弃用，但正文 22 处仍在使用 | Medium | ⚠️ 需处理 | 应执行全文替换 Checkpoint Version → Checkpoint Base |
| R2 | 嵌套列表缩进使用 Tab，跨渲染器兼容性风险 | Low | ℹ️ 已知 | 建议统一改为空格缩进 |

**审阅员签名**：
- DocUIClaude：术语一致性检查 ✅
- DocUIGPT：格式与结构检查 ✅

---

*El Psy Kongroo.*

