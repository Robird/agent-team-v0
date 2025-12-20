# 条款稳定语义锚点设计工作坊

> **日期**：2025-12-21
> **目标**：为 DurableHeap MVP v2 的所有规范条款设计稳定语义锚点（Stable Semantic Anchors）
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
> **主持人**：刘德智 / SageWeaver

---

## 背景

根据监护人批示，条款 ID 应从位置型编号（如 `[S-17]`）升级为**稳定语义锚点**（如 `[S-ID-RESERVED-RANGE]`）。

**优势**：
1. 重新编号时无需担心批量替换错误
2. 锚点名本身就是条款内容的"语义摘要"
3. 便于测试框架映射和跨文档引用

**命名原则**（草案）：
- 使用 `SCREAMING-KEBAB-CASE`
- 保留前缀 `F-`/`A-`/`S-`/`R-` 表示类别
- 名称应能概括条款核心语义（类似"内容 hash"）
- 长度控制在 3-5 个词

---

## 现有条款清单

以下是从文档中提取的所有条款，按文内出现顺序排列：

### Format 类 (`[F-xx]`)

| 原编号 | 位置 | 内容摘要 | 建议锚点名 |
|--------|------|----------|------------|
| `[F-01]` | 术语表/RecordKind | 域隔离：data/meta 文件各有独立 RecordKind 枚举空间 | |
| `[F-02]` | §3.2.1 | Magic 是 Record Separator：空文件先写 Magic，每写完 Record 后追加 Magic | |
| `[F-03]` | §3.2.1 | HeadLen == TailLen：否则视为损坏 | |
| `[F-04]` | §3.2.1 | 对齐约束：HeadLen % 4 == 0，Record 起点 4B 对齐 | |
| `[F-05]` | §3.2.1 | Ptr64 null 与对齐：Ptr64==0 表示 null，否则必须 4B 对齐 | |
| `[F-06]` | §3.2.1 | CRC32C 覆盖范围：Payload + Pad + TailLen（不含 HeadLen） | |
| `[F-07]` | §3.2.0.1 | varint canonical 最短编码 | |
| `[F-08]` | §3.2.0.1 | 解码错误策略：EOF/溢出/非 canonical 一律格式错误 | |
| `[F-09]` | §3.4.2 | KeyValuePairType 低 4 bit 为 ValueType，高 4 bit 预留必须写 0 | |
| `[F-10]` | §3.2.5 | ObjectKind 0-127 为标准类型 | |
| `[F-11]` | §3.2.5 | ObjectKind 128-255 保留给版本变体 | |
| `[F-12]` | §3.2.5 | 遇到未知 ObjectKind 必须抛异常（Fail-fast） | |
| `[F-13]` | §3.2.1 | Magic 不属于 Record，是 Record 间的栅栏 | |

### API 类 (`[A-xx]`)

| 原编号 | 位置 | 内容摘要 | 建议锚点名 |
|--------|------|----------|------------|
| `[A-01]` | §3.4.3 | DiscardChanges 方法（MUST）：重置 _current 为 _committed 副本 | |
| `[A-02]` | §3.4.5 | CommitAll() 无参版（MUST）：提交 Dirty Set 所有对象 | |
| `[A-03]` | §3.4.5 | CommitAll(newRoot)（SHOULD）：设置新 root 并提交 | |
| `[A-04]` | §3.4.7 | Dirty Set 可见性 API（SHOULD）：HasDirtyObjects 等 | |

### Semantics 类 (`[S-xx]`)

| 原编号 | 位置 | 内容摘要 | 建议锚点名 |
|--------|------|----------|------------|
| `[S-01]` | §3.1.0.1 | Dirty Set 强引用：持有对象直到 Commit 或 DiscardChanges | |
| `[S-02]` | §3.1.0.1 | Identity Map 与 Dirty Set 的 key 必须等于对象 ObjectId | |
| `[S-03]` | §3.1.0.1 | Dirty 对象不得被 GC 回收 | |
| `[S-04]` | §3.1.0.1 | 新建对象必须立即加入 Dirty Set | |
| `[S-05]` | (Deprecated) | 原语义已合并到 [S-06] | |
| `[S-06]` | §3.4.3 | Working State 纯净性：tombstone 不得作为值出现 | |
| `[S-07]` | §3.4.3 | Delete 一致性：ContainsKey/TryGetValue/Enumerate 结果一致 | |
| `[S-08]` | §3.4.3 | Commit 失败不改内存（对象级） | |
| `[S-09]` | §3.4.3 | Commit 成功后追平：CommittedState == CurrentState | |
| `[S-10]` | §3.4.3 | 隔离性：Commit 后对 _current 写入不影响 _committed | |
| `[S-11]` | §3.4.3 | Diff Key 唯一 + 升序 | |
| `[S-12]` | §3.4.3 | Canonical Diff：不含 net-zero 变更 | |
| `[S-13]` | §3.4.3 | 可重放性：Apply(S, D) == CurrentState | |
| `[S-14]` | §3.4.3 | _dirtyKeys 精确性 | |
| `[S-15]` | §3.4.5 | Commit 失败不改内存（Heap 级） | |
| `[S-16]` | §3.4.5 | 可重试：失败后可再次 CommitAll | |
| `[S-17]` | §3.4.6/术语表 | ObjectId 保留区：0-15 不可分配 | |
| `[S-18]` | §3.2.4 | Checkpoint Base 断链：无法回溯更早历史 | |
| `[S-19]` | §3.2.4 | 否决 MSB Hack 维持历史链 | |
| `[S-20]` | §3.1.0.1 | Transient Dirty 对象 DiscardChanges 行为：Detach | |
| `[S-21]` | §3.1.1 | ObjectId 单调性边界 | |
| `[S-22]` | §3.1.1 | CreateObject 分配行为 | |

### Recovery 类 (`[R-xx]`)

| 原编号 | 位置 | 内容摘要 | 建议锚点名 |
|--------|------|----------|------------|
| `[R-01]` | §3.2.1 | resync 时不信任 TailLen：必须按 4B 回退扫描 | |
| `[R-02]` | §3.2.2 | meta 领先 data 时继续回扫上一条 | |
| `[R-03]` | §3.5 | 以 DataTail 截断 data 文件尾部垃圾 | |
| `[R-04]` | §3.5 | Allocator 仅从 HEAD.NextObjectId 初始化 | |

---

## 任务

请各位为每个条款提出**建议锚点名**，遵循以下格式：

```
[F-01] → [F-RECORDKIND-DOMAIN-ISOLATION]
```

可以讨论命名风格、长度、是否需要缩写等。

---

## 讨论记录

### DocUIClaude（命名方案）

**命名原则说明**：
- 优先使用文档中已有的规范术语（如 `RecordKind`、`Checkpoint`、`Dirty Set`）
- 动词优先选择描述性动作（`MUST`、`PROHIBIT`、`REQUIRE`）
- 保持 2-4 个词的长度，必要时用缩写（如 `GC`、`ID`、`CRC`）

---

#### Format 类 (`[F-xx]`)

| 原编号 | 建议锚点名 | 命名理由 |
|--------|------------|----------|
| `[F-01]` | `[F-RECORDKIND-DOMAIN-ISOLATION]` | 核心语义是"域隔离"，`DOMAIN` 比 `NAMESPACE` 更贴近文档用语 |
| `[F-02]` | `[F-MAGIC-AS-SEPARATOR]` | 强调 Magic 的角色是"分隔符"而非"记录头" |
| `[F-03]` | `[F-HEADLEN-TAILLEN-MATCH]` | 头尾长度必须匹配，用 `MATCH` 表达等式约束 |
| `[F-04]` | `[F-RECORD-4B-ALIGNMENT]` | 4 字节对齐是核心约束，`RECORD` 表明作用范围 |
| `[F-05]` | `[F-PTR64-NULL-AND-ALIGN]` | 同时覆盖 null 表示和对齐约束两个语义 |
| `[F-06]` | `[F-CRC32C-COVERAGE]` | 直接点明 CRC 的覆盖范围问题 |
| `[F-07]` | `[F-VARINT-CANONICAL]` | `CANONICAL` 是编码领域的标准术语 |
| `[F-08]` | `[F-DECODE-ERROR-FAILFAST]` | 强调错误处理策略是 fail-fast |
| `[F-09]` | `[F-KVPAIR-HIGHBITS-RESERVED]` | `HIGHBITS` 精确指向高 4 bit 预留 |
| `[F-10]` | `[F-OBJECTKIND-STD-RANGE]` | `STD` = Standard，0-127 标准类型范围 |
| `[F-11]` | `[F-OBJECTKIND-VARIANT-RANGE]` | `VARIANT` 表示版本变体，128-255 范围 |
| `[F-12]` | `[F-UNKNOWN-OBJECTKIND-REJECT]` | `REJECT` 表达遇到未知类型必须拒绝 |
| `[F-13]` | `[F-MAGIC-NOT-RECORD]` | 与 `[F-02]` 互补，强调 Magic 的独立身份 |

---

#### API 类 (`[A-xx]`)

| 原编号 | 建议锚点名 | 命名理由 |
|--------|------------|----------|
| `[A-01]` | `[A-DISCARDCHANGES-RESET]` | `RESET` 描述操作效果：重置到 committed 状态 |
| `[A-02]` | `[A-COMMITALL-NOARG-MUST]` | 强调无参版本是 MUST 级别要求 |
| `[A-03]` | `[A-COMMITALL-NEWROOT-SHOULD]` | 与 `[A-02]` 形成对照，设置新 root 是 SHOULD |
| `[A-04]` | `[A-DIRTYSET-VISIBILITY]` | `VISIBILITY` 表达 API 的可观测性需求 |

---

#### Semantics 类 (`[S-xx]`)

| 原编号 | 建议锚点名 | 命名理由 |
|--------|------------|----------|
| `[S-01]` | `[S-DIRTYSET-STRONG-REF]` | 强引用是防止 GC 的机制 |
| `[S-02]` | `[S-MAP-KEY-OBJECTID-SYNC]` | `SYNC` 表达 key 必须与 ObjectId 同步一致 |
| `[S-03]` | `[S-DIRTY-OBJECT-GC-PROHIBIT]` | `PROHIBIT` 表达禁止 GC 回收 |
| `[S-04]` | `[S-NEW-OBJECT-DIRTY-IMMEDIATE]` | `IMMEDIATE` 强调"立即"加入的时序要求 |
| `[S-05]` | `[S-DEPRECATED-MERGED-TO-S06]` | 保留废弃标记，便于历史追溯 |
| `[S-06]` | `[S-WORKING-STATE-NO-TOMBSTONE]` | 直接表达约束：Working State 不含 tombstone |
| `[S-07]` | `[S-DELETE-API-CONSISTENCY]` | `CONSISTENCY` 表达多 API 结果一致性 |
| `[S-08]` | `[S-COMMIT-FAIL-MEMORY-INTACT]` | `INTACT` 表达内存不变 |
| `[S-09]` | `[S-COMMIT-SUCCESS-STATE-SYNC]` | `SYNC` 表达两状态同步 |
| `[S-10]` | `[S-COMMIT-ISOLATION]` | `ISOLATION` 是数据库事务的经典术语 |
| `[S-11]` | `[S-DIFF-KEY-UNIQUE-SORTED]` | 同时覆盖唯一性和有序性 |
| `[S-12]` | `[S-DIFF-CANONICAL-NO-NETZERO]` | `NETZERO` 是净零变更的紧凑表达 |
| `[S-13]` | `[S-DIFF-REPLAYABLE]` | `REPLAYABLE` 表达可重放性 |
| `[S-14]` | `[S-DIRTYKEYS-PRECISION]` | `PRECISION` 表达精确追踪 |
| `[S-15]` | `[S-HEAP-COMMIT-FAIL-INTACT]` | 与 `[S-08]` 对应，层级从对象提升到 Heap |
| `[S-16]` | `[S-COMMIT-FAIL-RETRYABLE]` | `RETRYABLE` 表达失败后可重试 |
| `[S-17]` | `[S-OBJECTID-RESERVED-RANGE]` | `RESERVED` 明确表达保留区概念 |
| `[S-18]` | `[S-CHECKPOINT-HISTORY-BREAK]` | `BREAK` 表达历史链断裂 |
| `[S-19]` | `[S-MSB-HACK-REJECTED]` | `REJECTED` 表达否决决策 |
| `[S-20]` | `[S-TRANSIENT-DISCARD-DETACH]` | `TRANSIENT` 限定作用范围，`DETACH` 描述结果 |
| `[S-21]` | `[S-OBJECTID-MONOTONIC-BOUND]` | `BOUND` 表达边界条件 |
| `[S-22]` | `[S-CREATE-OBJECT-ALLOC]` | `ALLOC` 是分配行为的标准缩写 |

---

#### Recovery 类 (`[R-xx]`)

| 原编号 | 建议锚点名 | 命名理由 |
|--------|------------|----------|
| `[R-01]` | `[R-RESYNC-DISTRUST-TAILLEN]` | `DISTRUST` 精确表达"不信任"的语义 |
| `[R-02]` | `[R-META-AHEAD-BACKTRACK]` | `BACKTRACK` 描述回扫动作 |
| `[R-03]` | `[R-DATATAIL-TRUNCATE-GARBAGE]` | 描述截断操作的目标 |
| `[R-04]` | `[R-ALLOCATOR-INIT-FROM-HEAD]` | `INIT-FROM-HEAD` 精确描述初始化来源 |

---

**补充说明**：

1. **命名一致性**：相似概念使用相似词根
   - `INTACT` / `SYNC` 用于状态一致性
   - `PROHIBIT` / `REJECT` 用于禁止性约束
   - `MUST` / `SHOULD` 仅在需要强调规范级别时使用

2. **可搜索性**：每个锚点名包含足够的关键词，便于 grep 搜索
   - `git grep 'COMMIT.*FAIL'` 可找到所有提交失败相关条款
   - `git grep 'OBJECTID'` 可找到所有对象 ID 相关条款

3. **与测试框架对齐**：锚点名可直接映射为测试类/方法名
   - `[F-VARINT-CANONICAL]` → `Test_F_Varint_Canonical()`
   - `[S-DIRTYSET-STRONG-REF]` → `Test_S_DirtySet_StrongRef()`

---

### DocUIGemini（命名方案）

**设计理念**：
我的命名侧重于 **Mental Model (心智模型)** 和 **Affordance (示能性)**。
- 使用 **Pinning** 代替 Strong Ref，因为 Pinning 是内存管理的标准动词。
- 使用 **Symmetry** 代替 Match，强调几何直觉。
- 使用 **Fence** 代替 Separator，强调边界感。

---

#### Format 类 (`[F-xx]`)

| 原编号 | 建议锚点名 | 命名理由 |
|--------|------------|----------|
| `[F-01]` | `[F-RECORDKIND-DOMAIN-ISOLATION]` | 同意 Claude，精确。 |
| `[F-02]` | `[F-MAGIC-AS-PUNCTUATION]` | Magic 是二进制流的标点符号，提供视觉韵律。 |
| `[F-03]` | `[F-RECORD-LENGTH-SYMMETRY]` | `SYMMETRY` (对称性) 比 `MATCH` 更具几何直觉，暗示了从两端都能解析的特性。 |
| `[F-04]` | `[F-RECORD-ALIGNMENT-4B]` | 强调 4B 是对齐粒度。 |
| `[F-05]` | `[F-PTR64-NULLABLE-ALIGNMENT]` | `NULLABLE` 涵盖了 0 的语义，`ALIGNMENT` 涵盖了非 0 的约束。 |
| `[F-06]` | `[F-CRC32C-PAYLOAD-COVERAGE]` | 显式指出覆盖的是 Payload (及周边)，而非全包。 |
| `[F-07]` | `[F-VARINT-CANONICAL-ENCODING]` | 强调是编码约束。 |
| `[F-08]` | `[F-DECODER-STRICT-MODE]` | `STRICT-MODE` 暗示了 Fail-fast 和拒绝非标准编码的态度。 |
| `[F-09]` | `[F-KVPAIR-RESERVED-BITS]` | 重点在 Reserved，这是对未来的留白。 |
| `[F-10]` | `[F-OBJECTKIND-STD-SPACE]` | `SPACE` (空间) 比 `RANGE` 更符合类型系统的隐喻。 |
| `[F-11]` | `[F-OBJECTKIND-VERSION-SPACE]` | 同上，这是版本变体的空间。 |
| `[F-12]` | `[F-UNKNOWN-KIND-TRAP]` | `TRAP` (陷阱/中断) 是 CPU 处理非法指令的术语，非常生动。 |
| `[F-13]` | `[F-MAGIC-IS-FENCE]` | `FENCE` (栅栏) 形象地描述了 Magic 将 Record 隔开的作用，且不属于 Record。 |

---

#### API 类 (`[A-xx]`)

| 原编号 | 建议锚点名 | 命名理由 |
|--------|------------|----------|
| `[A-01]` | `[A-DISCARD-REVERTS-TO-COMMITTED]` | 这是一个完整的句子：Discard 动作将状态 Revert 到 Committed。 |
| `[A-02]` | `[A-COMMIT-ALL-DIRTY]` | 动词短语，清晰表达"提交所有脏对象"。 |
| `[A-03]` | `[A-COMMIT-AND-SET-ROOT]` | 显式化副作用：既提交，又设置 Root。 |
| `[A-04]` | `[A-DIRTY-STATE-OBSERVABILITY]` | `OBSERVABILITY` (可观测性) 是系统设计的核心属性。 |

---

#### Semantics 类 (`[S-xx]`)

| 原编号 | 建议锚点名 | 命名理由 |
|--------|------------|----------|
| `[S-01]` | `[S-DIRTY-OBJECT-PINNING]` | `PINNING` (钉住) 是内存管理中防止移动/回收的精准术语，形象生动。 |
| `[S-02]` | `[S-IDENTITY-MAP-COHERENCE]` | `COHERENCE` (连贯性) 强调 Map Key 与 Object ID 的内在一致。 |
| `[S-03]` | `[S-DIRTY-LIFETIME-EXTENSION]` | 强调 Dirty 状态延长了对象的生命周期 (防止 GC)。 |
| `[S-04]` | `[S-NEW-OBJECT-AUTO-DIRTY]` | `AUTO-DIRTY` 强调了自动化的状态流转，无需用户干预。 |
| `[S-05]` | `[S-DEPRECATED-SEE-S06]` | 标准废弃写法。 |
| `[S-06]` | `[S-WORKING-STATE-TOMBSTONE-FREE]` | `FREE` (无...的) 是一种强约束表达，如 Lock-Free。 |
| `[S-07]` | `[S-READ-API-CONSISTENCY]` | 强调所有 Read API 的视图一致性。 |
| `[S-08]` | `[S-COMMIT-ATOMICITY-OBJECT]` | 强调对象级的原子性：要么全变，要么不变。 |
| `[S-09]` | `[S-COMMIT-SYNCHRONIZATION]` | 提交成功意味着内存态与持久态的同步。 |
| `[S-10]` | `[S-WRITE-ISOLATION-COPY-ON-WRITE]` | 暗示了 COW 的隔离机制。 |
| `[S-11]` | `[S-DIFF-SORTED-UNIQUE]` | 简洁的标准算法术语。 |
| `[S-12]` | `[S-DIFF-MINIMALISM]` | `MINIMALISM` (极简主义) 暗示不包含冗余 (Net-Zero) 变更。 |
| `[S-13]` | `[S-DIFF-REPLAY-DETERMINISM]` | 强调 Replay 的确定性结果。 |
| `[S-14]` | `[S-DIRTY-TRACKING-ACCURACY]` | 强调追踪的精确性。 |
| `[S-15]` | `[S-COMMIT-ATOMICITY-HEAP]` | Heap 级的原子性。 |
| `[S-16]` | `[S-COMMIT-FAILURE-RECOVERABLE]` | 强调失败是可恢复的，不是致命的。 |
| `[S-17]` | `[S-ID-RESERVED-SYSTEM-RANGE]` | 强调是系统保留。 |
| `[S-18]` | `[S-HISTORY-HORIZON-CUTOFF]` | `HORIZON` (视界) 很有画面感，Checkpoint 构成了历史的视界。 |
| `[S-19]` | `[S-HISTORY-CHAIN-INTEGRITY]` | 拒绝 MSB Hack 是为了维护历史链的完整性。 |
| `[S-20]` | `[S-TRANSIENT-DISCARD-IS-DETACH]` | 明确定义：对于瞬态对象，Discard 等同于 Detach。 |
| `[S-21]` | `[S-ID-ALLOCATION-MONOTONICITY]` | 单调性是核心数学属性。 |
| `[S-22]` | `[S-CREATE-IMPLIES-ALLOCATION]` | 创建即分配。 |

---

#### Recovery 类 (`[R-xx]`)

| 原编号 | 建议锚点名 | 命名理由 |
|--------|------------|----------|
| `[R-01]` | `[R-SCAN-IGNORE-TAIL-HINT]` | `HINT` 暗示 TailLen 只是提示，不可信，必须 Ignore。 |
| `[R-02]` | `[R-META-LEAD-COMPENSATION]` | Meta 领先是一种需要"补偿" (Compensate) 或回溯的状态。 |
| `[R-03]` | `[R-TAIL-GARBAGE-TRUNCATION]` | `TRUNCATION` 是文件操作的标准动词。 |
| `[R-04]` | `[R-ALLOCATOR-SEEDING-FROM-HEAD]` | `SEEDING` (播种) 形象地描述了分配器状态的初始化。 |

---

### DocUIGPT（命名方案）

#### Format 类（`[F-xx]`）

[F-01] → [F-RECORDKIND-DOMAIN-ISOLATION]
[F-02] → [F-MAGIC-RECORD-SEPARATOR]
[F-03] → [F-HEADLEN-TAILLEN-EQUAL]
[F-04] → [F-RECORD-4B-ALIGNMENT]
[F-05] → [F-PTR64-NULL-ALIGNMENT]
[F-06] → [F-CRC32C-PAYLOAD-PAD-TAILLEN]
[F-07] → [F-VARINT-SHORTEST-ENCODING]
[F-08] → [F-VARINT-DECODE-FAILFAST]
[F-09] → [F-KVPAIRTYPE-HIGHNIBBLE-ZERO]
[F-10] → [F-OBJECTKIND-STANDARD-RANGE]
[F-11] → [F-OBJECTKIND-VARIANT-RANGE]
[F-12] → [F-UNKNOWN-OBJECTKIND-REJECT]
[F-13] → [F-MAGIC-OUTSIDE-RECORD]

#### API 类（`[A-xx]`）

[A-01] → [A-DISCARDCHANGES-REVERT-COMMITTED]
[A-02] → [A-COMMITALL-COMMIT-DIRTYSET]
[A-03] → [A-COMMITALL-SET-NEWROOT]
[A-04] → [A-DIRTYSET-VISIBILITY-API]

#### Semantics 类（`[S-xx]`）

[S-01] → [S-DIRTYSET-STRONG-REFERENCE]
[S-02] → [S-IDENTITYMAP-KEY-OBJECTID]
[S-03] → [S-DIRTY-OBJECT-NO-GC]
[S-04] → [S-NEW-OBJECT-AUTO-DIRTY]
[S-05] → [S-DEPRECATED-SEE-S06]
[S-06] → [S-WORKING-STATE-TOMBSTONE-FREE]
[S-07] → [S-DELETE-API-CONSISTENCY]
[S-08] → [S-COMMIT-FAIL-MEMORY-UNCHANGED]
[S-09] → [S-COMMIT-SUCCESS-STATE-SYNC]
[S-10] → [S-POSTCOMMIT-STATE-ISOLATION]
[S-11] → [S-DIFF-KEY-SORTED-UNIQUE]
[S-12] → [S-DIFF-CANONICAL-NO-NETZERO]
[S-13] → [S-DIFF-APPLY-DETERMINISTIC]
[S-14] → [S-DIRTYKEYS-TRACKING-EXACT]
[S-15] → [S-HEAP-COMMIT-FAIL-UNCHANGED]
[S-16] → [S-COMMIT-FAIL-RETRYABLE]
[S-17] → [S-OBJECTID-RESERVED-RANGE]
[S-18] → [S-CHECKPOINT-HISTORY-CUTOFF]
[S-19] → [S-PREVVERSIONPTR-NO-FLAGS]
[S-20] → [S-TRANSIENT-DISCARD-DETACH]
[S-21] → [S-OBJECTID-MONOTONIC-BOUNDARY]
[S-22] → [S-CREATEOBJECT-ALLOCATES-OBJECTID]

#### Recovery 类（`[R-xx]`）

[R-01] → [R-RESYNC-TAILLEN-BACKSCAN-4B]
[R-02] → [R-META-AHEAD-BACKSCAN]
[R-03] → [R-DATATAIL-TRUNCATE-TAIL]
[R-04] → [R-ALLOCATOR-SEED-HEAD-NEXTOBJECTID]

---

### 主持人（共识整合：最终命名方案）

三位 Specialist 的命名方案各有特色：
- **Claude**：强调与文档术语一致，便于搜索
- **Gemini**：强调心智模型和可读性，使用更生动的隐喻
- **GPT**：强调测试映射和机器可读性

我整合出以下**最终命名方案**，采纳各方案的最佳命名：

---

## 最终命名方案

### Format 类 (`[F-xx]`)

| 原编号 | 稳定锚点 | 来源 |
|--------|----------|------|
| `[F-01]` | `[F-RECORDKIND-DOMAIN-ISOLATION]` | Claude/GPT 共识 |
| `[F-02]` | `[F-MAGIC-RECORD-SEPARATOR]` | GPT（简洁明了） |
| `[F-03]` | `[F-HEADLEN-TAILLEN-SYMMETRY]` | Gemini（"对称性"更有画面感） |
| `[F-04]` | `[F-RECORD-4B-ALIGNMENT]` | Claude/GPT 共识 |
| `[F-05]` | `[F-PTR64-NULL-AND-ALIGNMENT]` | Claude（同时覆盖两个语义） |
| `[F-06]` | `[F-CRC32C-PAYLOAD-COVERAGE]` | Gemini（明确覆盖范围） |
| `[F-07]` | `[F-VARINT-CANONICAL-ENCODING]` | Gemini（强调是编码约束） |
| `[F-08]` | `[F-DECODE-ERROR-FAILFAST]` | Claude（fail-fast 是核心策略） |
| `[F-09]` | `[F-KVPAIR-HIGHBITS-RESERVED]` | Claude（精确指向高 4 bit） |
| `[F-10]` | `[F-OBJECTKIND-STANDARD-RANGE]` | GPT（拼出完整单词） |
| `[F-11]` | `[F-OBJECTKIND-VARIANT-RANGE]` | Claude/GPT 共识 |
| `[F-12]` | `[F-UNKNOWN-OBJECTKIND-REJECT]` | Claude/GPT 共识 |
| `[F-13]` | `[F-MAGIC-IS-FENCE]` | Gemini（"栅栏"隐喻很生动） |

### API 类 (`[A-xx]`)

| 原编号 | 稳定锚点 | 来源 |
|--------|----------|------|
| `[A-01]` | `[A-DISCARDCHANGES-REVERT-COMMITTED]` | GPT（完整描述动作效果） |
| `[A-02]` | `[A-COMMITALL-FLUSH-DIRTYSET]` | 综合（FLUSH 比 COMMIT 更精确） |
| `[A-03]` | `[A-COMMITALL-SET-NEWROOT]` | GPT（显式化副作用） |
| `[A-04]` | `[A-DIRTYSET-OBSERVABILITY]` | Gemini（可观测性是核心属性） |

### Semantics 类 (`[S-xx]`)

| 原编号 | 稳定锚点 | 来源 |
|--------|----------|------|
| `[S-01]` | `[S-DIRTYSET-OBJECT-PINNING]` | Gemini（PINNING 是精准术语） |
| `[S-02]` | `[S-IDENTITY-MAP-KEY-COHERENCE]` | Gemini（COHERENCE 强调一致性） |
| `[S-03]` | `[S-DIRTY-OBJECT-GC-PROHIBIT]` | Claude（PROHIBIT 更强烈） |
| `[S-04]` | `[S-NEW-OBJECT-AUTO-DIRTY]` | Gemini（强调自动化） |
| `[S-05]` | `[S-DEPRECATED-MERGED-TO-S06]` | Claude（保留历史追溯） |
| `[S-06]` | `[S-WORKING-STATE-TOMBSTONE-FREE]` | Gemini（FREE 是强约束表达） |
| `[S-07]` | `[S-DELETE-API-CONSISTENCY]` | Claude/GPT 共识 |
| `[S-08]` | `[S-COMMIT-FAIL-MEMORY-INTACT]` | Claude（INTACT 表达不变） |
| `[S-09]` | `[S-COMMIT-SUCCESS-STATE-SYNC]` | Claude/GPT 共识 |
| `[S-10]` | `[S-POSTCOMMIT-WRITE-ISOLATION]` | 综合（明确是 Commit 后的隔离） |
| `[S-11]` | `[S-DIFF-KEY-SORTED-UNIQUE]` | GPT（标准算法术语） |
| `[S-12]` | `[S-DIFF-CANONICAL-NO-NETZERO]` | Claude/GPT 共识 |
| `[S-13]` | `[S-DIFF-REPLAY-DETERMINISM]` | Gemini（强调确定性） |
| `[S-14]` | `[S-DIRTYKEYS-TRACKING-EXACT]` | GPT（EXACT 更精确） |
| `[S-15]` | `[S-HEAP-COMMIT-FAIL-INTACT]` | Claude（与 S-08 对称） |
| `[S-16]` | `[S-COMMIT-FAIL-RETRYABLE]` | Claude/GPT 共识 |
| `[S-17]` | `[S-OBJECTID-RESERVED-RANGE]` | Claude/GPT 共识 |
| `[S-18]` | `[S-CHECKPOINT-HISTORY-CUTOFF]` | GPT（CUTOFF 表达截断） |
| `[S-19]` | `[S-MSB-HACK-REJECTED]` | Claude（直接否决） |
| `[S-20]` | `[S-TRANSIENT-DISCARD-DETACH]` | Claude/GPT 共识 |
| `[S-21]` | `[S-OBJECTID-MONOTONIC-BOUNDARY]` | GPT（BOUNDARY 表达边界） |
| `[S-22]` | `[S-CREATEOBJECT-IMMEDIATE-ALLOC]` | 综合（强调立即分配） |

### Recovery 类 (`[R-xx]`)

| 原编号 | 稳定锚点 | 来源 |
|--------|----------|------|
| `[R-01]` | `[R-RESYNC-DISTRUST-TAILLEN]` | Claude（DISTRUST 精确表达不信任） |
| `[R-02]` | `[R-META-AHEAD-BACKTRACK]` | Claude（BACKTRACK 描述回扫） |
| `[R-03]` | `[R-DATATAIL-TRUNCATE-GARBAGE]` | Claude/GPT 共识 |
| `[R-04]` | `[R-ALLOCATOR-SEED-FROM-HEAD]` | Gemini（SEED 形象描述初始化） |

---

## 旧→新映射表（供批量替换）

```
[F-01]  → [F-RECORDKIND-DOMAIN-ISOLATION]
[F-02]  → [F-MAGIC-RECORD-SEPARATOR]
[F-03]  → [F-HEADLEN-TAILLEN-SYMMETRY]
[F-04]  → [F-RECORD-4B-ALIGNMENT]
[F-05]  → [F-PTR64-NULL-AND-ALIGNMENT]
[F-06]  → [F-CRC32C-PAYLOAD-COVERAGE]
[F-07]  → [F-VARINT-CANONICAL-ENCODING]
[F-08]  → [F-DECODE-ERROR-FAILFAST]
[F-09]  → [F-KVPAIR-HIGHBITS-RESERVED]
[F-10]  → [F-OBJECTKIND-STANDARD-RANGE]
[F-11]  → [F-OBJECTKIND-VARIANT-RANGE]
[F-12]  → [F-UNKNOWN-OBJECTKIND-REJECT]
[F-13]  → [F-MAGIC-IS-FENCE]

[A-01]  → [A-DISCARDCHANGES-REVERT-COMMITTED]
[A-02]  → [A-COMMITALL-FLUSH-DIRTYSET]
[A-03]  → [A-COMMITALL-SET-NEWROOT]
[A-04]  → [A-DIRTYSET-OBSERVABILITY]

[S-01]  → [S-DIRTYSET-OBJECT-PINNING]
[S-02]  → [S-IDENTITY-MAP-KEY-COHERENCE]
[S-03]  → [S-DIRTY-OBJECT-GC-PROHIBIT]
[S-04]  → [S-NEW-OBJECT-AUTO-DIRTY]
[S-05]  → [S-DEPRECATED-MERGED-TO-S06]
[S-06]  → [S-WORKING-STATE-TOMBSTONE-FREE]
[S-07]  → [S-DELETE-API-CONSISTENCY]
[S-08]  → [S-COMMIT-FAIL-MEMORY-INTACT]
[S-09]  → [S-COMMIT-SUCCESS-STATE-SYNC]
[S-10]  → [S-POSTCOMMIT-WRITE-ISOLATION]
[S-11]  → [S-DIFF-KEY-SORTED-UNIQUE]
[S-12]  → [S-DIFF-CANONICAL-NO-NETZERO]
[S-13]  → [S-DIFF-REPLAY-DETERMINISM]
[S-14]  → [S-DIRTYKEYS-TRACKING-EXACT]
[S-15]  → [S-HEAP-COMMIT-FAIL-INTACT]
[S-16]  → [S-COMMIT-FAIL-RETRYABLE]
[S-17]  → [S-OBJECTID-RESERVED-RANGE]
[S-18]  → [S-CHECKPOINT-HISTORY-CUTOFF]
[S-19]  → [S-MSB-HACK-REJECTED]
[S-20]  → [S-TRANSIENT-DISCARD-DETACH]
[S-21]  → [S-OBJECTID-MONOTONIC-BOUNDARY]
[S-22]  → [S-CREATEOBJECT-IMMEDIATE-ALLOC]

[R-01]  → [R-RESYNC-DISTRUST-TAILLEN]
[R-02]  → [R-META-AHEAD-BACKTRACK]
[R-03]  → [R-DATATAIL-TRUNCATE-GARBAGE]
[R-04]  → [R-ALLOCATOR-SEED-FROM-HEAD]
```

---

## 下一步

1. 将映射表应用到 `mvp-design-v2.md`
2. 更新"条款编号"章节的规则说明
3. 更新决策摘要

---