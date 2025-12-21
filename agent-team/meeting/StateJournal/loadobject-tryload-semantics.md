# LoadObject/TryLoadObject 语义设计专项讨论

> **专项议题**：LoadObject 是否改为 TryLoadObject，以及错误返回机制设计
> **相关任务**：`LoadObject TryLoad 语义标注`、`TryLoadObject 设计思路`
> **当前状态**：设计讨论中
> **创建日期**：2025-12-21
> **最后更新**：2025-12-21

---

## 议题概述

在 StateJournal (原 DurableHeap) MVP v2 设计过程中，关于 `LoadObject` API 的错误处理机制产生了三个核心问题：

1. **命名问题**：是否应该使用 `TryLoadObject` 来更明确地表达"可能失败"的语义？
2. **返回值问题**：错误信息应该如何返回？
   - 选项 A: `bool TryLoadObject(out T? obj)`（C# 传统模式）
   - 选项 B: `string? TryLoadObject(out T? obj)`（返回错误原因字符串）
   - 选项 C: `DurableHeapError? TryLoadObject(out T? obj)`（返回结构化错误对象）
   - 选项 D: 保持 `T? LoadObject(id)`，返回 null 表示未找到
3. **异常 vs 返回值**：对于不同类型的错误（未找到 vs 格式损坏 vs 状态错误），应该用异常还是返回值？

---

## 前情回顾：历次讨论要点

### 2025-12-20 秘密基地畅谈会

#### DocUIGemini 的初始观点（第一轮）

**核心论点**：API 的"示能性 (Affordance)"与实际行为必须匹配

**问题识别**：
> 在许多 ORM/存储框架中，`Load(id)` 隐含了 "Expect Exists" 的语义（找不到抛异常），而 `Find(id)` 或 `TryLoad(id)` 才返回 `null`。
> 如果开发者拿着一个从其他对象引用的 `ObjectId` 去 Load，他通常预期它存在。返回 `null` 会导致后续代码报 `NullReferenceException`，丢失上下文。

**建议**：
- 考虑将 API 命名为 `TryLoadObject` 以匹配其行为
- 或者提供 `LoadObject` (throws if missing) 和 `TryLoadObject` (returns null) 双版本

**DX 风险分析**：
开发者可能会为了"只保存修改"而被迫传入当前的 Root，或者为了"只修改 Root"而意外触发了全量提交。

---

#### DocUIClaude 的回应（第二轮）

**立场**：保留意见，倾向于保持现状

**论据**：

1. **复杂度代价**：双版本 API 意味着 Workspace 类要维护两个方法，且每次扩展（如 LoadObject with version）都要考虑两套
2. **MVP 原则**：最小可行方案应避免 API 膨胀
3. **现有惯例**：C# 的 `Dictionary<K,V>.TryGetValue` 返回 `bool` + `out` 参数，而 StateJournal 的 `LoadObject` 返回 `null` 其实已经是"Try"语义

**折中建议**：
保持现有 `LoadObject` 返回 `null` 的设计，但在文档中**明确标注语义**：
> "`LoadObject` 采用 'TryLoad' 语义：对象不存在时返回 `null`，而非抛出异常。"

**可选扩展**：
如果未来需要 "Expect Exists" 语义，可以用扩展方法实现：
```csharp
public static T LoadObjectOrThrow<T>(this Workspace ws, ulong id) where T : IDurableObject
    => ws.LoadObject<T>(id) ?? throw new ObjectNotFoundException(id);
```

---

#### DocUIGemini 的修正立场（第二轮）

**接受 Claude 的观点**：

> "MVP 原则：最小可行方案应避免 API 膨胀"

**修正后的观点**：
对于 Agent 而言，`null` 是一种有效的 **Observation (观测结果)** —— "我观测到对象不存在"。这比抛出异常（通常被 Agent 视为 "Action Failed" 或 "System Error"）更具语义价值。

只要文档明确承诺 "Returns null if not found"（而非 "Behavior undefined"），`LoadObject` 返回 `null` 是完全可以接受的。这符合 **"Fail-Soft"** 的 UX 原则。

---

#### 主持人的共识记录

最终共识归入 **P2 优先级**（建议但非必须）：

| 问题 | 解决方案 | 共识程度 |
|------|----------|----------|
| `LoadObject` 命名 | 保持现有设计（返回 `null`），但在文档中明确标注 "TryLoad 语义" | Claude 保留意见，Gemini 修正接受 |

---

### 2025-12-21 决策诊疗室

#### 监护人补充意见

在"State 枚举与 Error Affordance 设计"会议开场时，监护人（刘德智/SageWeaver）提供了新的设计思路：

**核心思路**：
> `string? TryLoadObject(out DurableObject? ret)` 设计思路（返回 string 表示错误原因）

**设计理由**（推测）：
- 项目主要用户是 LLM 自己
- 字符串错误原因对 LLM 更友好（相比 bool 返回值，提供了更多上下文）
- 相比抛异常，返回值机制不打断控制流

---

#### Error Affordance 讨论中的相关观点

##### DocUIClaude 的结构化错误设计

虽然不是直接针对 `LoadObject` API，但在 P1-5 Error Affordance 任务中，Claude 提出了通用的错误信息结构：

```csharp
public interface IDurableHeapException
{
    // === MUST 字段 ===
    string ErrorCode { get; }         // 机器可判定的错误码（如 "OBJECT_NOT_FOUND"）
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

**ErrorCode 枚举示例**：
```csharp
public const string OBJECT_NOT_FOUND = "OBJECT_NOT_FOUND";
```

---

##### DocUIGemini 的 Error Affordance 观点

**核心主张**：
> 错误信息是 Agent 的 **Recovery Prompt**。
> 监护人提到的 `TryLoadObject(out ret)` 模式虽然经典，但对 Agent 来说，**结构化异常** 往往比 `bool` 返回值包含更多上下文。

**混合模式建议**：
- **机器可读**：`ErrorCode` (string enum), `ObjectId` (ulong?), `State` (enum?)
- **LLM 可读**：`Message` (自然语言描述), `RecoveryHint` (自然语言建议)

**针对 `LoadObject` 的具体建议**：
> 对于 `LoadObject`，支持 `null` 返回值（作为 "Not Found" 的 Observation），但对于其他操作（如访问 Detached 对象），必须抛出富异常。

---

##### DocUIGPT 的可诊断性补充

**接受 Fail-Soft，但要求可诊断性**：

> 我同意 Gemini 第二轮的修正：`null` 本身是有效 Observation。MVP 不必扩充成 `Load`/`TryLoad` 两套。
>
> 但为了 Agent 的可诊断性，建议至少保证：
> - 文档明确这是契约（not found → `null`），并且
> - 提供一个可选的"带原因"的通道（例如 `TryLoadObject(out reason)` 或 `LoadObjectResult`），否则 Agent 只能从 `null` 推断，无法区分"NotFound vs invalid id vs detached ref"。（不一定要立刻上 API，但规范可以预留扩展点。）

---

## 核心争议点分析

### 1. 命名：LoadObject vs TryLoadObject

| 观点 | 论据 | 支持者 |
|------|------|--------|
| **改名为 TryLoadObject** | - 命名明确表达"可能失败"<br>- 符合 .NET 惯例（Try 前缀表示不抛异常）<br>- 避免开发者误用（期待 Load 必定成功） | Gemini (初期) |
| **保持 LoadObject** | - MVP 应避免 API 膨胀<br>- 返回 null 已经是明确的 Try 语义<br>- 可通过文档澄清 | Claude, Gemini (修正后) |

**当前倾向**：保持 `LoadObject`，通过文档明确语义

---

### 2. 返回值：bool vs string vs record

#### 选项对比

| 方案 | 签名 | 优点 | 缺点 |
|------|------|------|------|
| **A. bool** | `bool TryLoadObject(out T? obj)` | - .NET 经典模式<br>- 简洁 | - 无法携带错误原因<br>- Agent/人类都难以调试 |
| **B. string** | `string? TryLoadObject(out T? obj)` | - 直接返回错误原因<br>- LLM 可读 | - 非结构化<br>- 难以机器判定 |
| **C. record** | `DurableHeapError? TryLoadObject(out T? obj)` | - 结构化<br>- 机器+LLM 双可读<br>- 可扩展 | - 引入新类型<br>- 复杂度较高 |
| **D. nullable** | `T? LoadObject(id)` | - 最简洁<br>- null 表达 Not Found | - 无法区分错误类型<br>- 无错误上下文 |

#### 监护人方案 (B) 的特点

```csharp
string? errorReason = workspace.TryLoadObject(id, out DurableObject? obj);
if (errorReason != null) {
    // 处理错误，errorReason 包含人类/LLM 可读的原因
}
```

**优点**：
- 对 LLM 极其友好（自然语言错误描述）
- 相比 bool，提供了丰富上下文
- 相比异常，不打断控制流

**缺点**：
- 如果需要机器判定错误类型，需要解析字符串（脆弱）
- 与 Error Affordance 规范中的 ErrorCode 机制脱节

---

#### GPT 提出的折中方案

在监护人的 `string?` 方案基础上，GPT 建议：

> 若坚持返回 `string`，则该 string **MUST 以 `ErrorCode` 开头**（例如 `"OBJECT_NOT_FOUND: ..."`），否则测试与 agent 都无法稳定分派。

**示例**：
```csharp
string? error = workspace.TryLoadObject(id, out var obj);
if (error != null) {
    if (error.StartsWith("OBJECT_NOT_FOUND")) {
        // 机器可判定的分支
    }
    Console.WriteLine(error); // LLM 可读的完整信息
}
```

**更推荐的方案**：
> 返回 `DurableHeapError?`（或 `bool TryLoadObject(..., out DurableHeapError? error)`），把 "机器字段"与 "展示文本"分层。

---

### 3. 异常 vs 返回值：何时抛异常？

| 错误场景 | 当前 MVP 处理 | 争议点 |
|----------|---------------|--------|
| **对象未找到** | 返回 `null` | ✅ 三方共识：应返回 null，不抛异常 |
| **格式损坏** | ❓ 未明确 | 🔴 应该抛异常（系统级错误）还是返回错误码？ |
| **对象 Detached** | 抛 `ObjectDetachedException` | ✅ 三方共识：应抛异常（状态前置条件违反） |
| **ObjectId 非法** | ❓ 未明确 | 🔴 应该返回错误还是抛异常？ |

**需要明确的决策**：
- `LoadObject` 是否只用于"Not Found"场景？
- 如果遇到格式错误/损坏，是否应该用另一个 API（如 `ValidateObject`）？

---

## 当前设计的隐式决策

根据现有文档（mvp-design-v2.md §3.3.2），当前 `LoadObject` 的隐式行为：

| 场景 | 当前行为 |
|------|----------|
| 对象存在且有效 | 返回对象，`State == Clean` |
| ObjectId 在 VersionIndex 中不存在 | 返回 `null` |
| 格式损坏（CRC/Framing 错误） | ❓ 文档未明确（推测：抛异常） |
| ObjectId 保留区（0-15） | ❓ 文档未明确 |

**问题**：
- "返回 `null`"的语义**只覆盖了 "Not Found"**，没有覆盖其他错误类型
- 如果格式损坏时抛异常，那么调用方需要同时处理返回值和异常，这增加了认知负担

---

## 设计选项与权衡

### 选项 1：保持 LoadObject + 返回 null（当前 MVP）

**API**：
```csharp
T? LoadObject<T>(ulong id) where T : IDurableObject;
```

**优点**：
- 最简洁
- 符合 C# 可空引用类型惯例
- 对 "Not Found" 场景处理清晰

**缺点**：
- 无法区分错误类型（Not Found vs 格式损坏 vs ...)
- 格式损坏时只能抛异常，破坏 API 的 Fail-Soft 承诺
- Agent 难以从 null 中获取调试信息

**适用场景**：
- 对象不存在是**唯一的预期失败场景**
- 所有其他错误（格式损坏等）都视为异常（不可恢复）

**文档要求**：
- 明确 "返回 null 仅表示 Not Found"
- 明确哪些错误场景会抛异常

---

### 选项 2：改为 TryLoadObject + bool

**API**：
```csharp
bool TryLoadObject<T>(ulong id, out T? obj) where T : IDurableObject;
```

**优点**：
- 符合 .NET 惯例（TryGet 模式）
- 命名明确表达"可能失败"

**缺点**：
- 与选项 1 完全相同的问题（bool 无法携带错误信息）
- 引入 `out` 参数，API 稍微冗长
- 对 LLM 而言，bool 信息量不如 null

**结论**：
- 相比选项 1，**没有本质改进**，只是命名更明确
- 如果选择这个方向，不如直接跳到选项 3/4

---

### 选项 3：TryLoadObject + string（监护人方案）

**API**：
```csharp
string? TryLoadObject<T>(ulong id, out T? obj) where T : IDurableObject;
// 返回值：null 表示成功，非 null 表示错误原因
```

**优点**：
- 对 LLM 极其友好（自然语言错误描述）
- 可携带丰富的上下文信息
- 不打断控制流（相比异常）

**缺点**：
- 字符串是非结构化的，机器判定需要解析
- 与 Error Affordance 规范中的 ErrorCode 机制脱节
- 如果字符串格式不一致，测试和 Agent 都难以稳定处理

**改进方案（GPT 建议）**：
```csharp
// 字符串必须以 ErrorCode 开头
string? error = TryLoadObject(id, out var obj);
if (error?.StartsWith("OBJECT_NOT_FOUND") == true) {
    // 机器可判定
}
```

**适用场景**：
- 主要面向 LLM Agent
- 错误种类较少（可以靠前缀区分）
- 不需要复杂的错误处理逻辑

---

### 选项 4：TryLoadObject + DurableHeapError

**API**：
```csharp
public record DurableHeapError(
    string ErrorCode,          // MUST: 机器可判定
    string Message,            // MUST: LLM 可读
    ulong? ObjectId = null,    // SHOULD
    string? RecoveryHint = null // SHOULD
);

DurableHeapError? TryLoadObject<T>(ulong id, out T? obj) where T : IDurableObject;
```

**优点**：
- 结构化：机器可判定 + LLM 可读
- 与 Error Affordance 规范完全对齐
- 可扩展（未来可添加字段）
- 测试友好（可稳定断言 ErrorCode）

**缺点**：
- 引入新类型，增加 API 表面积
- 相比 string，稍显复杂

**示例用法**：
```csharp
var error = workspace.TryLoadObject<MyObject>(id, out var obj);
if (error != null) {
    switch (error.ErrorCode) {
        case "OBJECT_NOT_FOUND":
            Console.WriteLine($"对象 {error.ObjectId} 未找到");
            Console.WriteLine(error.RecoveryHint); // "Call CreateObject() to create a new object"
            break;
        case "CORRUPTED_RECORD":
            Console.WriteLine(error.Message);
            // 可能需要运行修复工具
            break;
    }
}
```

**适用场景**：
- 需要支持多种错误类型
- 需要机器判定错误类型
- 面向 LLM Agent + 人类开发者

---

### 选项 5：双 API（LoadObject + TryLoadObject）

**API**：
```csharp
// 变体 1：Load 抛异常，TryLoad 返回 null
T LoadObject<T>(ulong id) where T : IDurableObject; // throws if not found
T? TryLoadObject<T>(ulong id) where T : IDurableObject; // returns null if not found

// 变体 2：Load 抛异常，TryLoad 返回结构化错误
T LoadObject<T>(ulong id) where T : IDurableObject; // throws if not found
DurableHeapError? TryLoadObject<T>(ulong id, out T? obj) where T : IDurableObject;
```

**优点**：
- 同时满足"Expect Exists"和"Maybe Exists"两种场景
- 符合部分 .NET 库的设计模式（如某些 ORM）

**缺点**：
- API 膨胀（两倍方法数）
- 维护成本高
- 对 MVP 来说过于复杂

**Claude 的意见**：
> MVP 原则：最小可行方案应避免 API 膨胀

**可能的折中**：
- MVP 只提供 `TryLoadObject`
- 通过扩展方法提供 `LoadObjectOrThrow`

---

## 待决策问题清单

### 核心决策

1. **命名**：`LoadObject` 还是 `TryLoadObject`？
2. **返回值**：`null` / `bool` / `string?` / `DurableHeapError?`？
3. **错误范围**：`TryLoadObject` 应该覆盖哪些错误类型？
   - 仅 Not Found？
   - Not Found + 格式损坏？
   - 所有可能的错误？

### 边界条件

4. **格式损坏**：如果 ObjectVersionPtr 指向的 Record CRC 校验失败，应该：
   - 返回错误码（Fail-Soft）？
   - 抛出异常（Fail-Fast）？
   - 两者皆可（取决于 API）？

5. **ObjectId 非法**：如果 ObjectId 在保留区（0-15）且不是 Well-Known Object，应该：
   - 返回错误码？
   - 抛出异常？

6. **Detached 对象**：如果尝试 Load 一个已经在内存中但状态为 Detached 的对象，应该：
   - 抛出异常（当前设计）？
   - 返回错误码？
   - 重新从磁盘加载？

### 与 Error Affordance 的对齐

7. **错误码注册**：如果采用 `string?` 或 `DurableHeapError?`，需要在文档中注册哪些 ErrorCode？
   - `OBJECT_NOT_FOUND`
   - `CORRUPTED_RECORD`
   - `INVALID_OBJECT_ID`
   - `OBJECT_DETACHED`（如果不抛异常）
   - ...

8. **RecoveryHint**：每种错误应该提供什么样的恢复建议？
   - `OBJECT_NOT_FOUND`: "Call CreateObject() to create a new object with this ObjectId"
   - `CORRUPTED_RECORD`: "The object data is corrupted. Consider running repair tool or restoring from backup."
   - ...

---

## 建议的讨论流程

### 第一步：明确错误边界

**问题**：`LoadObject` 应该处理哪些错误场景？

建议的错误分类：
```
A. 预期内的正常失败（应返回错误码/null）
   - OBJECT_NOT_FOUND: ObjectId 在 VersionIndex 中不存在

B. 系统级错误（应抛出异常？）
   - CORRUPTED_RECORD: CRC 校验失败
   - INVALID_FRAMING: Magic/Length 不匹配
   - IO_ERROR: 文件读取失败

C. 调用方错误（应抛出异常？）
   - INVALID_OBJECT_ID: ObjectId 在保留区但不是 Well-Known
   - OBJECT_TYPE_MISMATCH: 对象实际类型与请求类型不符
```

**决策点**：
- A 类必须返回错误码/null
- B 类和 C 类：异常 or 错误码？

---

### 第二步：选择返回值设计

如果决定 `LoadObject` 只处理 A 类错误（Not Found），那么：
- **选项 1** (返回 `null`) 足够
- 文档中明确：B/C 类错误会抛出异常

如果决定 `LoadObject` 处理 A+B 类错误，那么：
- **选项 3** (`string?`) 或 **选项 4** (`DurableHeapError?`) 更合适
- C 类错误仍然抛出异常

如果决定 `LoadObject` 处理所有错误，那么：
- **选项 4** (`DurableHeapError?`) 几乎是唯一选择
- 完全不抛异常（除了编程错误如 ArgumentNullException）

---

### 第三步：与 Error Affordance 对齐

无论选择哪种方案，都需要：
1. 定义 ErrorCode 枚举
2. 为每个 ErrorCode 编写规范条款
3. 为每个 ErrorCode 定义 RecoveryHint
4. 编写测试向量

**示例**（选项 4）：
```markdown
| ErrorCode | 触发条件 | RecoveryHint |
|-----------|----------|--------------|
| `OBJECT_NOT_FOUND` | ObjectId 不在 VersionIndex | "Call CreateObject() or check if ObjectId is correct" |
| `CORRUPTED_RECORD` | CRC 校验失败 | "The object data is corrupted. Try LoadObject(id, version: previous) or restore from backup" |
```

---

## 参考：其他系统的设计

### .NET Dictionary

```csharp
// 返回 bool + out 参数
bool TryGetValue(TKey key, out TValue value);

// 抛异常
TValue this[TKey key] { get; }
```

**教训**：双 API 满足不同场景

---

### Entity Framework Core

```csharp
// 返回 null（类似选项 1）
TEntity? Find(params object?[]? keyValues);

// 抛异常（Single 期待有且仅有一个）
TEntity Single(Expression<Func<TEntity, bool>> predicate);
```

**教训**：`Find` 用于"可能不存在"，`Single` 用于"必须存在"

---

### Rust Result<T, E>

```rust
// 返回 Result（类似选项 4）
fn load_object(id: u64) -> Result<Object, LoadError>;

match workspace.load_object(id) {
    Ok(obj) => { /* 使用 obj */ },
    Err(LoadError::NotFound) => { /* 处理未找到 */ },
    Err(LoadError::Corrupted(msg)) => { /* 处理损坏 */ },
}
```

**教训**：结构化错误类型（enum）支持模式匹配

---

## 下一步行动

### 待讨论的核心问题（优先级排序）

1. **P0**：`LoadObject` 的错误边界（A/B/C 类错误如何处理）
2. **P0**：返回值设计（null / string? / DurableHeapError?）
3. **P1**：命名（LoadObject vs TryLoadObject）
4. **P1**：ErrorCode 注册表
5. **P2**：是否需要双 API（Load + TryLoad）

### 建议的讨论形式

考虑以下方式之一：
- **决策诊疗室**：三位 Specialist 独立提案 → 交叉会诊 → 共识
- **监护人直接决策**：基于本文档的分析，监护人给出明确方向
- **原型实现**：先实现一个版本，在使用中迭代

---

## 附录：相关规范条款

### 当前 MVP v2 中与 LoadObject 相关的条款

#### [A-LOADOBJECT-SHALLOW-MUST]
> LoadObject MUST 执行 Shallow Materialization：仅加载目标对象，不递归加载引用。

#### [S-LOADOBJECT-NULL-IF-NOTFOUND]
> LoadObject 对不存在的 ObjectId 返回 null（而非抛出异常）。

（注：此条款为本文档推测，需确认是否已在 mvp-design-v2.md 中明确）

---

### Error Affordance 相关条款（2025-12-21 新增）

#### [A-ERROR-CODE-MUST]
> 所有 StateJournal 公开异常 MUST 包含 `ErrorCode` 属性（字符串，机器可判定）

#### [A-ERROR-MESSAGE-MUST]
> 所有 StateJournal 公开异常 MUST 包含 `Message` 属性，描述失败原因和上下文

#### [A-ERROR-OBJECTID-SHOULD]
> 涉及特定对象的异常 SHOULD 包含 `ObjectId` 属性

#### [A-ERROR-RECOVERY-HINT-SHOULD]
> 异常 SHOULD 包含 `RecoveryHint` 属性，提供恢复建议（对 Agent 尤其重要）

---

## 术语表

| 术语 | 定义 |
|------|------|
| **Fail-Soft** | 失败时不抛异常，通过返回值（null/错误码）让调用方处理 |
| **Fail-Fast** | 失败时立即抛异常，中止当前操作 |
| **Observation** | Agent 对系统状态的观测结果（如"对象不存在"是一种观测） |
| **Affordance** | API 设计的"示能性"，即 API 的形式应暗示其行为 |
| **Recovery Prompt** | 错误信息中提供的恢复建议，引导用户/Agent 下一步操作 |
| **ErrorCode** | 机器可判定的错误标识符（字符串常量），用于稳定的错误分派 |

---

## 变更历史

| 日期 | 变更 | 负责人 |
|------|------|--------|
| 2025-12-21 | 创建本文档，汇总历次讨论 | AI Team Leader |

---

> **下一步**：组织专项讨论会，针对"待决策问题清单"逐一形成决策。
