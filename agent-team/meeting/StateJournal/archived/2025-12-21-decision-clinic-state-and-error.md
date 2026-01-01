# 决策诊疗室：State 枚举与 Error Affordance 设计

> **日期**：2025-12-21
> **形式**：决策诊疗室 (Decision Clinic)
> **目标**：为 P0-1、P0-3、P1-5、P1-8 形成可直接落文的具体设计方案
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
> **主持人**：刘德智 / SageWeaver

---

## 背景

根据 2025-12-20 畅谈会共识和监护人批示，以下任务需要进一步设计讨论：

| 编号 | 任务 | 复杂度 | 关键问题 |
|------|------|--------|----------|
| P0-1 | State 枚举升级为核心 API | 中 | 状态闭集定义？状态转换表？ |
| P0-3 | "必须写死"升级为条款 | 中 | 具体有哪些隐式 MUST？锚点名？ |
| P1-5 | Error Affordance 规范化 | 中 | 结构化字段定义？ErrorCode 为 MUST？ |
| P1-8 | DiscardChanges ObjectId 语义 | 低 | 追加到哪个条款？措辞？ |

**监护人补充意见**：
- 项目主要用户是 LLM 自己
- `string? TryLoadObject(out DurableObject? ret)` 设计思路（返回 string 表示错误原因）

---

## 议程

1. **Round 1：独立诊断** — 三位 Specialist 独立提出具体设计方案
2. **Round 2：交叉会诊** — 针对分歧点辩论
3. **Round 3：处方共识** — 形成可直接落文的条款草稿

---

## Round 1：独立诊断

### 任务说明

请各位 Specialist 针对以下 4 个任务，独立提出**具体的、可落文的**设计方案：

**P0-1 State 枚举**：
- 定义状态闭集（枚举值）
- 定义状态转换表（哪些 API 在哪些状态下合法）
- 提议条款锚点名

**P0-3 "必须写死"升级为条款**：
- 识别 §3.2.1 / §3.2.2 中的隐式 MUST
- 为每个 MUST 提议条款锚点名和措辞

**P1-5 Error Affordance**：
- 定义结构化错误信息的字段
- 哪些字段是 MUST，哪些是 SHOULD
- 提议条款锚点名

**P1-8 DiscardChanges ObjectId 语义**：
- 提议追加到 `[S-TRANSIENT-DISCARD-DETACH]` 的措辞

---

## 讨论记录

### DocUIClaude（Round 1：独立诊断）

作为概念框架专家，我从**状态机完备性**、**API 可预测性**和**LLM Agent 友好性**三个维度提出具体设计方案。

---

#### P0-1 State 枚举设计

**核心洞察**：对象生命周期是一个有限状态机，状态机的核心不变式是"状态必须可观测"。当前文档 §3.1.0.1 已有状态枚举的**建议 API**，但未规定为 MUST——这导致实现可能不提供状态查询，迫使用户通过 try-catch 探测状态（反模式）。

##### 1. 状态闭集定义

```csharp
public enum DurableObjectState
{
    Clean,           // 已加载，无未提交修改（可读可写）
    PersistentDirty, // 有未提交修改，已有 Committed 版本（可读可写）
    TransientDirty,  // 新建对象，尚未有 Committed 版本（可读可写）
    Detached         // 已分离，不可操作（任何访问抛异常）
}
```

**状态语义精确定义**：

| 状态 | `HasChanges` | `HasCommittedVersion` | 崩溃恢复行为 |
|------|--------------|----------------------|--------------|
| `Clean` | `false` | `true` | 恢复到 Committed State |
| `PersistentDirty` | `true` | `true` | 丢失未提交修改，恢复到 Committed State |
| `TransientDirty` | `true` | `false` | 对象丢失（从未 Commit） |
| `Detached` | N/A | N/A | 对象已不存在，无恢复行为 |

**命名决策说明**：
- 选择 `PersistentDirty` 而非 `Dirty`，与 `TransientDirty` 形成清晰对偶，避免歧义
- `Clean` 优于 `Committed`：Clean 强调"无未提交修改"，而非"有提交版本"
- `Detached` 优于 `Invalid`/`Disposed`：Detached 精确描述"从 Identity Map 分离"的语义

##### 2. 状态转换表（API × State 矩阵）

| API | Clean | PersistentDirty | TransientDirty | Detached |
|-----|-------|-----------------|----------------|----------|
| **读操作** (Get/Contains/Enumerate) | ✅ | ✅ | ✅ | ❌ `ObjectDetachedException` |
| **写操作** (Set/Remove) | ✅ → PersistentDirty | ✅ | ✅ | ❌ `ObjectDetachedException` |
| **Commit** (通过 CommitAll) | ✅ (无操作) | ✅ → Clean | ✅ → Clean | ❌ `ObjectDetachedException` |
| **DiscardChanges** | ✅ (无操作) | ✅ → Clean | ✅ → **Detached** | ❌ `ObjectDetachedException` |
| **State 查询** | ✅ | ✅ | ✅ | ✅ (返回 Detached) |

**边界条件说明**：
- `State` 属性即使在 Detached 状态下也可查询（返回 `Detached`），不抛异常——这是唯一的"安全探针"
- Clean 对象的 `DiscardChanges` 是空操作（idempotent）
- Clean 对象的 Commit 不生成新版本（fast path）

##### 3. 条款锚点名提议

| 条款 ID | 条款内容 | 说明 |
|---------|----------|------|
| `[A-OBJECT-STATE-ENUM-MUST]` | `IDurableObject` MUST 暴露 `State` 属性，返回 `DurableObjectState` 枚举 | 升级为 MUST |
| `[A-OBJECT-STATE-CLOSED-SET]` | `DurableObjectState` MUST 仅包含 `Clean`, `PersistentDirty`, `TransientDirty`, `Detached` 四个值 | 闭集保证 |
| `[A-OBJECT-STATE-DETACHED-QUERYABLE]` | `State` 属性在 Detached 状态下 MUST 返回 `Detached` 而非抛出异常 | 安全探针 |
| `[S-STATE-TRANSITION-MATRIX]` | 对象状态转换 MUST 遵循状态转换表定义 | 语义完备性 |

---

#### P0-3 "必须写死"条款化

**核心洞察**：§3.2.1 和 §3.2.2 中存在多处叙述性的"必须/必须写死"，这些是崩溃一致性的关键契约，但未被赋予条款 ID——导致测试矩阵可能遗漏、实现可能分歧。

##### 识别的隐式 MUST（§3.2.1 Data 文件）

**1. 写入顺序 7 步**

当前文档用"规范化步骤"描述，但未标注 MUST。这 7 步的顺序决定了 CRC 覆盖范围和崩溃恢复能力。

**提议条款**：

| 条款 ID | 条款内容 |
|---------|----------|
| `[F-RECORD-WRITE-STEP-ORDER]` | 单条 Record 的写入 MUST 按以下顺序执行：0) 确保 Magic 结尾 → 1) HeadLen 占位 → 2) Payload → 3) Pad → 4) TailLen → 5) CRC32C → 6) 回填 HeadLen → 7) 追加 Magic |

**2. resync 策略**

当前 `[R-RESYNC-DISTRUST-TAILLEN]` 已存在，但 resync 的完整行为（4B 对齐向前扫描）是叙述性的。

**提议条款**：

| 条款 ID | 条款内容 |
|---------|----------|
| `[R-RESYNC-4B-ALIGNED-SCAN]` | 尾部损坏时，reader MUST 按 4B 对齐从尾部向前扫描 Magic，对每个候选位置执行完整验证 |

##### 识别的隐式 MUST（§3.2.2 Meta 文件）

**3. 刷盘顺序（最关键！）**

当前文档写：
> "刷盘顺序（必须写死）：1) 先将 data 文件…fsync 2) 再将 meta 文件…fsync"

这是**崩溃一致性的核心契约**，但没有条款 ID！

**提议条款**：

| 条款 ID | 条款内容 |
|---------|----------|
| `[R-COMMIT-FLUSH-ORDER]` | Commit 刷盘顺序 MUST 为：1) data file 所有 records 写入并 fsync；2) **然后** meta file commit record 写入并 fsync。违反此顺序将导致崩溃恢复语义未定义 |

**4. Meta 领先处理**

当前 `[R-META-AHEAD-BACKTRACK]` 存在，但表述分散。建议整合：

**提议条款**（已存在，建议强化措辞）：

| 条款 ID | 条款内容（建议修订） |
|---------|----------|
| `[R-META-AHEAD-BACKTRACK]` | 若某条 MetaCommitRecord 校验通过，但 `DataTail > DataFileLength` 或 `VersionIndexPtr` 不可解引用，reader MUST 回扫上一条 MetaCommitRecord，直到找到有效记录或判定空仓库 |

##### 汇总表

| 章节 | 当前表述 | 提议条款 ID | 优先级 |
|------|----------|------------|--------|
| §3.2.1 | "规范化步骤 0-7" | `[F-RECORD-WRITE-STEP-ORDER]` | P0 |
| §3.2.1 | resync 策略叙述 | `[R-RESYNC-4B-ALIGNED-SCAN]` | P1 |
| §3.2.2 | "刷盘顺序（必须写死）" | `[R-COMMIT-FLUSH-ORDER]` | **P0（最关键）** |
| §3.2.2 | Meta 领先处理 | `[R-META-AHEAD-BACKTRACK]` (强化) | P0 |

---

#### P1-5 Error Affordance

**核心洞察**：对 LLM Agent 而言，错误信息是唯一的调试线索。模糊的 "Invalid Operation" 是 Agent 的噩梦，而带有 **结构化字段 + 恢复建议** 的错误信息是 Agent 的导航图。

##### 1. 结构化错误信息字段定义

```csharp
public interface IDurableHeapException
{
    // === MUST 字段 ===
    string ErrorCode { get; }         // 机器可判定的错误码（如 "OBJECT_DETACHED"）
    string Message { get; }           // 人类可读的错误描述
    
    // === SHOULD 字段 ===
    ulong? ObjectId { get; }          // 涉及的对象 ID（如适用）
    DurableObjectState? ObjectState { get; }  // 对象当前状态（如适用）
    string? RecoveryHint { get; }     // 可能的恢复建议
    
    // === MAY 字段 ===
    string? OperationName { get; }    // 触发异常的操作名称
    long? FileOffset { get; }         // 涉及的文件偏移量（用于格式错误）
}
```

##### 2. 字段规范性要求

| 字段 | 级别 | 理由 |
|------|------|------|
| `ErrorCode` | **MUST** | 机器可判定；支持 switch/match 分派；可测试 |
| `Message` | **MUST** | 人类可读；包含上下文 |
| `ObjectId` | **SHOULD** | 提供定位线索；对象操作异常时应提供 |
| `ObjectState` | **SHOULD** | 帮助理解"为什么失败"；与 State 枚举配合 |
| `RecoveryHint` | **SHOULD** | 提供下一步行动建议（对 Agent 尤其重要） |
| `OperationName` | MAY | 调试用；非关键路径 |
| `FileOffset` | MAY | 仅用于格式错误诊断 |

##### 3. ErrorCode 枚举（MVP 最小集）

```csharp
public static class DurableHeapErrorCodes
{
    // 对象状态错误
    public const string OBJECT_DETACHED = "OBJECT_DETACHED";
    public const string OBJECT_NOT_FOUND = "OBJECT_NOT_FOUND";
    
    // 格式错误
    public const string CORRUPTED_RECORD = "CORRUPTED_RECORD";
    public const string INVALID_FRAMING = "INVALID_FRAMING";
    public const string UNKNOWN_OBJECT_KIND = "UNKNOWN_OBJECT_KIND";
    public const string VARINT_OVERFLOW = "VARINT_OVERFLOW";
    public const string NON_CANONICAL_VARINT = "NON_CANONICAL_VARINT";
    
    // 提交错误
    public const string COMMIT_DATA_FSYNC_FAILED = "COMMIT_DATA_FSYNC_FAILED";
    public const string COMMIT_META_FSYNC_FAILED = "COMMIT_META_FSYNC_FAILED";
    
    // 类型错误
    public const string UNSUPPORTED_VALUE_TYPE = "UNSUPPORTED_VALUE_TYPE";
}
```

##### 4. 条款锚点名提议

| 条款 ID | 条款内容 | 级别 |
|---------|----------|------|
| `[A-ERROR-CODE-MUST]` | 所有 DurableHeap 公开异常 MUST 包含 `ErrorCode` 属性（字符串，机器可判定） | MUST |
| `[A-ERROR-MESSAGE-MUST]` | 所有 DurableHeap 公开异常 MUST 包含 `Message` 属性，描述失败原因和上下文 | MUST |
| `[A-ERROR-OBJECTID-SHOULD]` | 涉及特定对象的异常 SHOULD 包含 `ObjectId` 属性 | SHOULD |
| `[A-ERROR-STATE-SHOULD]` | 涉及对象状态的异常 SHOULD 包含 `ObjectState` 属性 | SHOULD |
| `[A-ERROR-RECOVERY-HINT-SHOULD]` | 异常 SHOULD 包含 `RecoveryHint` 属性，提供恢复建议（对 Agent 尤其重要） | SHOULD |
| `[A-ERROR-CODE-REGISTRY]` | ErrorCode 值 MUST 在文档中登记，禁止使用未登记的 ErrorCode | MUST |

##### 5. 示例：好的异常 vs 坏的异常

**❌ 坏的异常（Agent 无法处理）**：
```
InvalidOperationException: Invalid operation.
```

**✅ 好的异常（Agent 可理解、可恢复）**：
```
ObjectDetachedException:
  ErrorCode: "OBJECT_DETACHED"
  Message: "Cannot access object 42: it was detached after DiscardChanges on a Transient object."
  ObjectId: 42
  ObjectState: Detached
  RecoveryHint: "Object was never committed. Call CreateObject() to create a new object with a new ObjectId."
```

---

#### P1-8 DiscardChanges ObjectId 语义

**核心洞察**：当前 `[S-TRANSIENT-DISCARD-DETACH]` 清晰定义了 DiscardChanges 对 Transient 对象的行为（变 Detached），但未明确 ObjectId 的回收语义——用户可能疑惑"这个 ObjectId 还能用吗？"

##### 提议追加措辞

**当前条款**（摘自 mvp-design-v2.md §3.1.0.1）：
> **[S-TRANSIENT-DISCARD-DETACH] Transient Dirty 对象的 DiscardChanges 行为（MUST）**：
> - 对 Transient Dirty 对象调用 `DiscardChanges()` 后：
>   - 对象 MUST 从 Dirty Set 移除
>   - 对象 MUST 从 Identity Map 移除
>   - 后续任何访问（读/写/枚举）MUST 抛出 `ObjectDetachedException`
> - 异常消息 SHOULD 提供恢复指引...

**提议追加**：

在上述条款末尾追加：

> - **ObjectId 回收语义（MVP 固定）**：
>   - 已分配给 Detached 对象的 ObjectId 在**同一进程生命周期内** MUST NOT 被重新分配
>   - 仅当进程崩溃重启后，该 ObjectId **MAY** 被重新分配（因为 Transient 对象从未进入 VersionIndex，其 ObjectId 未被 `NextObjectId` 持久化）
>   - 调用方 SHOULD 避免在 `DiscardChanges()` 后仍持有对原对象的引用，以防逻辑混淆
>
> **设计理由**：进程内不回收可避免"ObjectId 被重用但调用方仍持有旧引用"的悬空问题；崩溃后允许重用是因为 Transient 对象从未对外可见。

##### 条款锚点名提议

将追加内容单独成条（或作为 `[S-TRANSIENT-DISCARD-DETACH]` 的子条款）：

| 条款 ID | 条款内容 |
|---------|----------|
| `[S-TRANSIENT-DISCARD-OBJECTID-NORECYCLE]` | Detached 对象的 ObjectId 在同一进程生命周期内 MUST NOT 被重新分配；进程重启后 MAY 重用 |

---

#### 汇总：所有提议的条款锚点

| 任务 | 条款 ID | 优先级 |
|------|---------|--------|
| P0-1 | `[A-OBJECT-STATE-ENUM-MUST]` | P0 |
| P0-1 | `[A-OBJECT-STATE-CLOSED-SET]` | P0 |
| P0-1 | `[A-OBJECT-STATE-DETACHED-QUERYABLE]` | P0 |
| P0-1 | `[S-STATE-TRANSITION-MATRIX]` | P0 |
| P0-3 | `[F-RECORD-WRITE-STEP-ORDER]` | P0 |
| P0-3 | `[R-COMMIT-FLUSH-ORDER]` | **P0（最关键）** |
| P0-3 | `[R-RESYNC-4B-ALIGNED-SCAN]` | P1 |
| P1-5 | `[A-ERROR-CODE-MUST]` | P1 |
| P1-5 | `[A-ERROR-MESSAGE-MUST]` | P1 |
| P1-5 | `[A-ERROR-OBJECTID-SHOULD]` | P1 |
| P1-5 | `[A-ERROR-STATE-SHOULD]` | P1 |
| P1-5 | `[A-ERROR-RECOVERY-HINT-SHOULD]` | P1 |
| P1-5 | `[A-ERROR-CODE-REGISTRY]` | P1 |
| P1-8 | `[S-TRANSIENT-DISCARD-OBJECTID-NORECYCLE]` | P1 |

---

**方法论说明**：
- 条款命名采用 `[前缀-核心概念-关键约束]` 格式，便于 grep 和测试映射
- 所有 MUST 级约束都应有对应的测试向量
- 与监护人"主要用户是 LLM 自己"的指导意见对齐：State 枚举和 Error Affordance 是 Agent 自主调试的关键

---

### DocUIGemini（Round 1：独立诊断）

#### P0-1 State 枚举设计

**核心洞察**：
Agent 是"盲人"，State 枚举是它的手杖。
如果 Agent 只能通过 `try-catch` 来探测对象状态，这不仅是 DX 问题，更是 **Safety** 问题——异常往往被 Agent 视为"行动失败"甚至"系统崩溃"，导致它放弃尝试或产生幻觉。
必须提供 **O(1) 的、无副作用的** 状态探针。

**1. 状态闭集与转换**

完全赞同 Claude 的 `DurableObjectState` 定义，但补充一点：**`IsDirty` / `HasChanges` 必须是 O(1) 的独立属性**。

虽然 `State == Dirty` 隐含了 `HasChanges`，但在 Agent 的思维链（CoT）中，"检查是否有变更"和"检查对象生命周期状态"是两个不同的认知步骤。
- `State`：回答 "我是谁？"（存活？新建？已死？）
- `HasChanges`：回答 "我需要保存吗？"

**建议 API**：
```csharp
// 核心状态
DurableObjectState State { get; }

// 快捷属性 (MUST be O(1))
bool HasChanges { get; } // 等价于 State == Dirty || State == TransientDirty
bool IsTransient { get; } // 等价于 State == TransientDirty
```

**2. 条款锚点名提议**

| 条款 ID | 条款内容 |
|---------|----------|
| `[A-STATE-OBSERVABILITY-MUST]` | 对象 MUST 暴露 `State` 属性和 `HasChanges` 属性，且读取它们 MUST NOT 抛出异常（即使在 Detached 状态下） |
| `[A-HASCHANGES-COMPLEXITY]` | `HasChanges` 属性的查询复杂度 MUST 为 O(1) |

---

#### P0-3 "必须写死"条款化

**核心洞察**：
"必须写死"通常意味着 **Binary Layout Invariant**（二进制布局不变式）。
对于 Agent 来说，这些是"物理定律"。如果文档只用自然语言描述物理定律，Agent 可能会试图"协商"或"绕过"。
必须用 **[F-xx]** (Format) 条款将其固化。

**1. 识别的隐式 MUST**

除了 Claude 识别的写入顺序和刷盘顺序，我还发现了：

- **Magic 作为栅栏** (§3.2.1)：文档说 "Magic 不属于 Record"，这是关键的 Framing 约束。
- **HeadLen/TailLen 对称性** (§3.2.1)：这是损坏检测的基础。

**2. 条款锚点名提议**

| 条款 ID | 条款内容 |
|---------|----------|
| `[F-MAGIC-FENCEPOST]` | Magic (4 bytes) MUST 作为 Record 之间的分隔符存在，不属于 Record Payload |
| `[F-LENGTH-SYMMETRY]` | Record 的 HeadLen MUST 等于 TailLen，否则视为损坏 |
| `[F-DATA-ALIGNMENT]` | Record 起始位置 MUST 4字节对齐 |

---

#### P1-5 Error Affordance

**核心洞察**：
错误信息是 Agent 的 **Recovery Prompt**。
监护人提到的 `TryLoadObject(out ret)` 模式虽然经典，但对 Agent 来说，**结构化异常** 往往比 `bool` 返回值包含更多上下文。
如果返回 `false`，Agent 只能瞎猜"为什么"；如果抛出带 `ErrorCode` 的异常，Agent 能精准定位。

**1. 结构化 vs 自然语言**

我建议采用 **混合模式**：
- **机器可读**：`ErrorCode` (string enum), `ObjectId` (ulong?), `State` (enum?)
- **LLM 可读**：`Message` (自然语言描述), `RecoveryHint` (自然语言建议)

**2. 监护人模式的变体**

对于 `LoadObject`，支持 `null` 返回值（作为 "Not Found" 的 Observation），但对于其他操作（如访问 Detached 对象），必须抛出富异常。

**3. 条款锚点名提议**

| 条款 ID | 条款内容 |
|---------|----------|
| `[A-ERR-STRUCTURED-MUST]` | 异常对象 MUST 实现 `IDurableHeapException` 接口，提供 `ErrorCode` 字段 |
| `[A-ERR-RECOVERY-HINT]` | 异常消息 SHOULD 包含 `RecoveryHint` 字段，用自然语言指导下一步操作 |
| `[A-ERR-CONTEXT-FIELDS]` | 涉及特定对象的异常 SHOULD 包含 `ObjectId` 和 `CurrentState` 字段 |

---

#### P1-8 DiscardChanges ObjectId 语义

**核心洞察**：
Agent 很容易混淆 "Object Identity" (ObjectId) 和 "Object Reference" (C# 变量)。
必须明确：Discard 之后，ObjectId 可能还在（如果是 Persistent），也可能没了（如果是 Transient），但手里的 C# 引用肯定废了。

**1. 提议措辞**

在 `[S-TRANSIENT-DISCARD-DETACH]` 后追加：

> **ObjectId 生命周期（Agent 注意）**：
> - 对于 **Transient** 对象，DiscardChanges 等同于 **"Cancel Creation"**。该 ObjectId 视为从未分配。
> - 警告：虽然 ObjectId 在技术上可能被回收重用（进程重启后），但 Agent **MUST NOT** 假设旧的 ObjectId 引用指向新对象。
> - 最佳实践：调用 DiscardChanges 后，Agent 应立即将持有该对象的变量置为 `null`。

---


### DocUIGPT（Round 1：独立诊断｜规范严谨性 + 可测试映射）

我从“**可判定契约（deterministic contract）**”与“**测试向量可直接映射（Requirement → Test Vector）**”角度出发：
- 任何会导致实现分叉的行为，都必须被钉死为 **闭集状态机 + 条款化前后置条件**。
- 任何需要 agent 恢复的错误，都必须被钉死为 **机器可断言的 ErrorCode + 结构化字段**（自然语言只做 display）。

---

#### P0-1 State 枚举：闭集完备 + 前置/后置条件 + 测试向量映射

##### 1) 状态闭集（Closed Set）

我建议把“对象实例状态”定义为**严格闭集**，并强制 `State` 为**总函数**（total function）：

```csharp
public enum DurableObjectState
{
        Clean,            // 已加载，HasChanges==false，且 HasCommittedVersion==true
        PersistentDirty,  // 有未提交修改，且 HasCommittedVersion==true
        TransientDirty,   // 新建对象尚未产生 committed version（HasCommittedVersion==false）
        Detached          // 终态：TransientDirty 调用 DiscardChanges 后进入；任何操作 fail-fast
}
```

**闭集完备性说明（避免“隐含状态”）**：
- “未加载/未 materialize”不是对象实例的状态（没有对象实例时不谈 `IDurableObject.State`）。
- “损坏/不可解码”属于 LoadObject 的结果（错误），不应塞进 State（否则状态机混入 I/O 与恢复语义，测试不可判定）。
- “heap 已关闭/Disposed”如 MVP 不定义，则必须在 API 语义中明确：该类错误属于 `ObjectDisposed`（ErrorCode）而非引入额外 state 值。

**关键不变式（用于测试断言）**：
- `State` MUST be side-effect-free；MUST be $O(1)$；MUST NOT throw（即使 Detached 也返回 `Detached`）。
- `HasChanges` 与 `HasCommittedVersion` 必须可由状态导出，并在文档中写为等价关系：
    - `State==Clean` ⟺ `HasChanges==false && HasCommittedVersion==true`
    - `State==PersistentDirty` ⟺ `HasChanges==true && HasCommittedVersion==true`
    - `State==TransientDirty` ⟺ `HasChanges==true && HasCommittedVersion==false`
    - `State==Detached` ⟺ `HasChanges==false && HasCommittedVersion==false`（或定义为 N/A，但为了测试判定建议固定为 false/false）

> 备注：现行正文里用 `Dirty`（且又区分 Transient/Persistent）会诱发歧义；我建议把枚举值直接命名为 `PersistentDirty/TransientDirty`，从命名层切断歧义源。

##### 2) 状态转换：按 API 写“前置条件 / 后置条件”

我建议把状态转换写成**条款化的 API 合法性矩阵**（对测试友好），并补上“非法调用的 ErrorCode”。

**读写 API（示意）**

| API | 前置条件（MUST） | 后置条件（MUST） | 非法时（MUST） |
|---|---|---|---|
| `LoadObject(id)` | — | 若 found → 返回对象 `State==Clean`；若 not found → 返回 `null`/NotFound | — |
| `CreateObject<T>()` | — | 返回对象 `State==TransientDirty`；加入 Dirty Set | — |
| 任何读（Get/Contains/Enumerate） | `State != Detached` | 状态不变 | `ErrorCode=OBJECT_DETACHED` |
| 任何写（Set/Remove） | `State != Detached` | `Clean→PersistentDirty`；`PersistentDirty/TransientDirty` 保持 dirty | `ErrorCode=OBJECT_DETACHED` |
| `DiscardChanges()` | `State != Detached` | `PersistentDirty→Clean`；`TransientDirty→Detached`；`Clean` 不变 | `ErrorCode=OBJECT_DETACHED` |
| `CommitAll()`（成功） | — | `PersistentDirty→Clean`；`TransientDirty→Clean`；`Clean` 不变 | — |
| `CommitAll()`（失败） | — | **所有对象状态 MUST 不变**（含 `HasChanges`/Dirty Set） | 返回/抛出带 ErrorCode 的失败 |

**需要显式写入的“禁止隐含状态”条款**：
- “Commit 失败不改内存”除了 `_current/_committed`，也必须覆盖 `State/HasChanges/Dirty Set membership`，否则实现会分叉出“半 finalize”的隐含状态。

##### 3) 测试向量映射（可直接落到 Appendix B）

建议为 State 相关条款建立一个最小测试向量族（seed vectors），每条向量只断言“可观测行为”，不依赖实现细节：

- `TV-STATE-01`：`CreateObject` → `State==TransientDirty` 且 `HasChanges==true`
- `TV-STATE-02`：`TransientDirty.DiscardChanges()` → `State==Detached`；随后任意读写抛 `ErrorCode==OBJECT_DETACHED`；`State` 仍可读且为 `Detached`
- `TV-STATE-03`：`Clean` 写入一次 → `State==PersistentDirty`；再次写/删保持 dirty
- `TV-STATE-04`：`PersistentDirty.DiscardChanges()` → `State==Clean` 且语义回到 committed
- `TV-STATE-05`（failpoint）：在“meta fsync 失败”下注入失败，`CommitAll` 失败后所有对象 `State/HasChanges` 不变（覆盖 Dirty Set membership）

条款锚点建议（示例）：
- `[A-OBJECT-STATE-PROPERTY]`（State must exist + total + O(1) + non-throwing）
- `[S-OBJECT-STATE-CLOSED-SET]`（枚举闭集）
- `[S-OBJECT-STATE-TRANSITIONS]`（状态转换矩阵/前后置条件）
- `[S-COMMIT-FAIL-NO-STATE-CHANGE]`（Commit 失败状态不变，作为补强条款，避免仅覆盖内存字典）

---

#### P0-3 把“必须写死”升级为条款：核查 §3.2.1/§3.2.2 的 MUST + RFC2119 措辞 + 锚点规范

这里我不复述已有条款，而是给出**缺口核查**：哪些“规范性句子”尚未被条款化（无法被测试向量引用），以及建议的锚点名/措辞。

##### 1) §3.2.1（Data 文件）MUST 核查缺口

已存在的条款（示例）：`[F-MAGIC-RECORD-SEPARATOR]`、`[F-HEADLEN-TAILLEN-SYMMETRY]`、`[F-CRC32C-PAYLOAD-COVERAGE]`、`[R-RESYNC-DISTRUST-TAILLEN]`、`[F-PTR64-NULL-AND-ALIGNMENT]`。

**仍建议条款化的关键 MUST（否则实现分叉点会落入“叙述段落”）**：

1) **“写入顺序（0..7）”目前是叙述，缺少锚点**
- 建议新增：`[F-RECORD-WRITE-SEQUENCE]`
- RFC 2119 草案措辞：
    - “Writer MUST write each Record using the following sequence (0..7) …”
    - “Writer MUST append the trailing Magic after successfully backfilling HeadLen.”

2) **“Ptr64 指向何处”目前是叙述，缺少锚点（测试需要可判定）**
- 建议新增：`[F-PTR64-POINTS-TO-HEADLEN]`
- 草案措辞：
    - “For any non-null Ptr64 that refers to a Record, Ptr64 MUST equal the byte offset of that Record’s HeadLen field (the first byte immediately after the preceding Magic).”

3) **Magic 常量值本身应条款化（否则跨实现可能拼写/版本漂移）**
- 建议新增：`[F-MAGIC-VALUE-DATA]` / `[F-MAGIC-VALUE-META]`（或合并为 `[F-MAGIC-VALUE-BY-DOMAIN]`）

##### 2) §3.2.2（Meta 文件）MUST 核查缺口

1) **刷盘顺序“必须写死”必须升级为 Recovery 条款（P0）**
- 建议新增：`[R-COMMIT-FSYNC-ORDER]`
- 草案措辞（强制顺序、可测试）：
    - “A Commit MUST flush and fsync the data file writes for this commit before writing and fsyncing the MetaCommitRecord. Implementations MUST NOT reorder these durability barriers.”

2) **Commit Point 的精确定义需要条款（避免实现把点放在 data fsync 或 meta write 之前）**
- 建议新增：`[R-COMMIT-POINT-META-FSYNC]`
- 草案措辞：
    - “The Commit Point MUST be defined as the moment when the MetaCommitRecord has been fully persisted (fsync completed).”

3) **Meta 的 resync 规则目前是叙述，需引用/复用 data 的 resync 条款或单列**
- 选择 A（推荐）：把 resync 定义提升为“域无关条款”，例如 `[R-RESYNC-4B-MAGIC-SCAN]`，并在 data/meta 处引用。
- 选择 B：新增 `[R-META-RESYNC-4B-MAGIC-SCAN]`。

4) **`DataTail = EOF` 且“包含尾部 Magic”是恢复可判定的关键，应条款化**
- 建议新增：`[F-DATATAIL-INCLUDES-TRAILING-MAGIC]`

##### 3) 锚点命名规范（补强元规则，减少歧义）

现有命名规则已给出，我建议再加 3 条"核查友好"的硬规则（写进规范语言小节即可）：
- 锚点名 **禁止**包含 MUST/SHOULD/MAY（规范级别可能演进，但锚点应稳定）。
- 对“过程类/顺序类”条款统一用后缀：`-SEQUENCE` / `-ORDER`（例如 `RECORD-WRITE-SEQUENCE`, `COMMIT-FSYNC-ORDER`），便于 grep 与测试命名。
- 同一语义不得出现两个近义锚点（例如 `WRITE-STEP-ORDER` 与 `WRITE-SEQUENCE` 只能留一个），否则测试映射会分叉。

##### 4) 测试映射建议（让 P0-3 可落地到 Appendix B）

- `TV-FRAMING-RECORD-WRITE-SEQUENCE-01`：模拟“写到步骤 5 崩溃/撕裂”，重启 resync 必须回到上一个有效 Magic 边界（覆盖 `[F-RECORD-WRITE-SEQUENCE]` 与 resync 条款）
- `TV-RECOVERY-COMMIT-FSYNC-ORDER-01`：注入“data fsync 成功、meta fsync 失败”，重启必须看到旧 HEAD（覆盖 `[R-COMMIT-FSYNC-ORDER]` 与 `[R-COMMIT-POINT-META-FSYNC]`）
- `TV-FORMAT-PTR64-POINTS-TO-HEADLEN-01`：对任意成功写入的 Record，断言 ptr 指向 HeadLen 的起始（可用 reader 反查）

---

#### P1-5 Error Affordance：为什么 ErrorCode MUST + 字段精确定义 + 测试框架对接

##### 1) 为什么 `ErrorCode` 必须是 MUST（不仅是 DX）

我建议在规范里写清“ErrorCode 的系统性理由”，否则容易被当成可选 DX：
- **可测试性**：测试必须能做稳定断言；自然语言 `Message` 不可稳定比较。
- **可恢复性（Agent/LLM）**：Agent 的恢复策略需要 switch/match 的离散键；否则只能对 Message 做脆弱的字符串解析。
- **跨语言/跨异常模型**：有的 API 走异常，有的 API 走 `TryXxx`；ErrorCode 是二者的共同最小交集。

##### 2) 结构化错误字段：精确定义（MUST/SHOULD/MAY）

建议定义一个“错误载体”类型（可被异常持有、也可被 Try-API 返回）：

```csharp
public sealed record DurableHeapError(
        string ErrorCode,
        string Message,
        string? Operation = null,
        ulong? ObjectId = null,
        DurableObjectState? ObjectState = null,
        string? RecoveryHint = null,
        string? FileDomain = null,   // "data" | "meta" | null
        ulong? FileOffset = null
);
```

规范性字段定义建议：
- `ErrorCode`（MUST）：机器可断言的稳定标识。
- `Message`（MUST）：给人/agent 看的解释性文本（不可作为唯一判定依据）。
- `Operation`（SHOULD）：触发错误的 API 名（例如 `DiscardChanges`, `LoadObject`）。
- `ObjectId`（SHOULD when applicable）：与对象相关错误必须给出。
- `ObjectState`（SHOULD when applicable）：若错误与状态前置条件相关（Detached 等）必须给出。
- `RecoveryHint`（SHOULD）：给出下一步建议（尤其对 OBJECT_DETACHED / NOT_FOUND）。
- `FileDomain`/`FileOffset`（MAY）：格式/恢复类错误用于定位（例如 CRC 不匹配）。

对监护人提到的 `TryLoadObject(out ret)`（返回 string 原因）我建议：
- 若坚持返回 `string`，则该 string MUST 以 `ErrorCode` 开头（例如 `OBJECT_NOT_FOUND: ...`），否则测试与 agent 都无法稳定分派。
- 更推荐：返回 `DurableHeapError?`（或 `bool TryLoadObject(..., out DurableHeapError? error)`），把 “机器字段”与 “展示文本”分层。

##### 3) 与测试框架对接：把错误码当作断言主键

建议在测试基线里固定一个最小错误码集（MVP seed），并要求每个错误码至少有 1 个向量：
- `OBJECT_DETACHED`：对 Detached 的任意操作
- `OBJECT_NOT_FOUND`：LoadObject 不存在（若使用错误返回而非 null）
- `CORRUPTED_RECORD` / `INVALID_FRAMING`：CRC/HeadLen/TailLen/Magic 相关
- `UNKNOWN_OBJECT_KIND`：ObjectKind 未知 fail-fast
- `NON_CANONICAL_VARINT` / `VARINT_OVERFLOW`：varint 规范
- `COMMIT_META_FSYNC_FAILED` / `COMMIT_DATA_FSYNC_FAILED`：failpoint 注入

测试辅助建议（让实现/测试团队低成本落地）：
- 提供 `AssertErrorCode(ex, "OBJECT_DETACHED")` 或 `AssertError(err, code:..., objectId:..., state:...)` 的统一断言入口。
- Appendix B 的每个向量增加 `ExpectedErrorCode` 字段（空表示成功路径）。

条款锚点建议：
- `[A-ERROR-CODE-REQUIRED]`
- `[A-ERROR-STRUCTURE-FIELDS]`
- `[A-ERROR-CODE-REGISTRY]`（错误码必须登记；禁止私自发明）
- `[A-TRYAPI-ERRORCODE-PREFIX]`（若采用 string 方案）

---

#### P1-8 DiscardChanges 的 ObjectId 语义：精确规范措辞（避免与唯一性条款冲突）

现行正文的 Zombie Warning 写到“Detached 的 ObjectId 可能被重新分配”，但这会与 `[S-OBJECTID-MONOTONIC-BOUNDARY]` 的“任意时刻不得出现两个存活对象共享同一 ObjectId”产生潜在冲突（Detached 实例仍在调用方变量中，且仍可被观察）。

我建议把语义钉死为“**进程内隔离（quarantine）**”：

建议追加到 `[S-TRANSIENT-DISCARD-DETACH]` 的规范措辞（可直接落文）：

> **ObjectId reuse after transient discard (MVP fixed)**：
> - After a `TransientDirty` object transitions to `Detached` via `DiscardChanges()`, its `ObjectId` MUST NOT be reallocated again during the same process lifetime.
> - After a crash/restart (i.e., across process lifetimes), that `ObjectId` MAY be reallocated, because the object was never committed and therefore never entered `HEAD` / `VersionIndex`.

并建议新增锚点：`[S-TRANSIENT-DISCARD-OBJECTID-QUARANTINE]`。

对应测试向量：
- `TV-OBJECTID-QUARANTINE-01`：同一进程内 `CreateObject` → `DiscardChanges`（Detached）→ 再 `CreateObject` N 次，断言新对象 `Id != oldId`。


---

## Round 2/3：主持人整合共识

三位 Specialist 的方案高度一致，无重大分歧。直接进入处方共识。

### 共识汇总

#### P0-1 State 枚举（全票通过）

**状态闭集**：
```csharp
public enum DurableObjectState
{
    Clean,            // 已加载，无未提交修改
    PersistentDirty,  // 有未提交修改，已有 Committed 版本
    TransientDirty,   // 新建对象，尚未有 Committed 版本
    Detached          // 已分离（终态）
}
```

**关键属性**：
- `State` 属性：MUST 存在、MUST O(1)、MUST 不抛异常（含 Detached）
- `HasChanges` 属性：MUST 存在、MUST O(1)

**条款锚点**：
| 条款 ID | 级别 |
|---------|------|
| `[A-OBJECT-STATE-PROPERTY]` | MUST |
| `[A-OBJECT-STATE-CLOSED-SET]` | MUST |
| `[A-HASCHANGES-O1-COMPLEXITY]` | MUST |
| `[S-STATE-TRANSITION-MATRIX]` | MUST |

---

#### P0-3 "必须写死"条款化（全票通过）

**新增条款**：
| 条款 ID | 内容 | 来源 |
|---------|------|------|
| `[F-RECORD-WRITE-SEQUENCE]` | 写入顺序 0-7 步 MUST 按序执行 | Claude/GPT |
| `[R-COMMIT-FSYNC-ORDER]` | data fsync MUST 在 meta fsync 之前 | **最关键** |
| `[R-COMMIT-POINT-META-FSYNC]` | Commit Point = meta fsync 完成时刻 | GPT |

---

#### P1-5 Error Affordance（全票通过）

**结构化字段**：
| 字段 | 级别 | 说明 |
|------|------|------|
| `ErrorCode` | **MUST** | 机器可判定 |
| `Message` | **MUST** | 人类可读 |
| `ObjectId` | SHOULD | 涉及对象时 |
| `ObjectState` | SHOULD | 涉及状态时 |
| `RecoveryHint` | SHOULD | 恢复建议 |

**条款锚点**：
| 条款 ID | 级别 |
|---------|------|
| `[A-ERROR-CODE-MUST]` | MUST |
| `[A-ERROR-MESSAGE-MUST]` | MUST |
| `[A-ERROR-CODE-REGISTRY]` | MUST |
| `[A-ERROR-RECOVERY-HINT-SHOULD]` | SHOULD |

---

#### P1-8 DiscardChanges ObjectId（全票通过）

**追加到 `[S-TRANSIENT-DISCARD-DETACH]`**：
> Detached 对象的 ObjectId 在同一进程生命周期内 MUST NOT 被重新分配；进程重启后 MAY 重用。

**条款锚点**：`[S-TRANSIENT-DISCARD-OBJECTID-QUARANTINE]`

---

## 下一步

将上述共识方案交给 Implementer 落文到 `mvp-design-v2.md`。

---

