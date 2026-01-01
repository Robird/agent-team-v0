# LoadObject 命名与返回值设计 秘密基地畅谈 🏠

> **形式**: 秘密基地畅谈 (Hideout Jam Session)
> **日期**: 2025-12-21
> **主题**: LoadObject 是否改名、改成什么样
> **氛围**: No structure, no voting, Yes-and...
> **关联议题**: `LoadObject TryLoad 语义标注` (backlog #2)、`TryLoadObject 设计思路` (backlog #3)

---

## 📋 前情提要

### 问题核心

在 StateJournal MVP v2 设计过程中，关于 `LoadObject` API 的设计产生了三个核心问题：

1. **命名问题**：是否应该使用 `TryLoadObject` 来更明确地表达"可能失败"的语义？
2. **返回值问题**：错误信息应该如何返回？
   - 选项 A: `bool TryLoadObject(out T? obj)`（C# 传统模式）
   - 选项 B: `string? TryLoadObject(out T? obj)`（返回错误原因字符串，监护人方案）
   - 选项 C: `DurableHeapError? TryLoadObject(out T? obj)`（返回结构化错误对象）
   - 选项 D: 保持 `T? LoadObject(id)`，返回 null 表示未找到
3. **异常 vs 返回值**：对于不同类型的错误（未找到 vs 格式损坏 vs 状态错误），应该用异常还是返回值？

### 历次讨论共识

| 日期 | 讨论 | 结论 |
|------|------|------|
| 2025-12-20 | 秘密基地畅谈（首轮） | Claude 保留意见，Gemini 修正接受：MVP 保持 `LoadObject` + 返回 null，文档明确 |
| 2025-12-21 | 决策诊疗室 | 监护人提出 `string? TryLoadObject(out ret)` 新思路 |

### 监护人的新思路

> `string? TryLoadObject(out DurableObject? ret)` 设计思路（返回 string 表示错误原因）

**设计理由**（推测）：
- 项目主要用户是 LLM 自己
- 字符串错误原因对 LLM 更友好（相比 bool 返回值，提供了更多上下文）
- 相比抛异常，返回值机制不打断控制流

### 当前设计选项一览

| 方案 | 签名 | 优点 | 缺点 |
|------|------|------|------|
| **A. bool** | `bool TryLoadObject(out T? obj)` | .NET 经典模式 | 无法携带错误原因 |
| **B. string** | `string? TryLoadObject(out T? obj)` | LLM 可读，返回错误原因 | 非结构化，难以机器判定 |
| **C. record** | `DurableHeapError? TryLoadObject(out T? obj)` | 机器+LLM 双可读 | 引入新类型，复杂度较高 |
| **D. nullable** | `T? LoadObject(id)` | 最简洁 | 无法区分错误类型 |

### 待解决的核心问题

1. **命名**：`LoadObject` 还是 `TryLoadObject`？
2. **返回值**：`null` / `bool` / `string?` / `DurableHeapError?`？
3. **错误边界**：哪些错误返回错误码？哪些抛异常？

---

## 🗣️ 畅谈区

> **发言规则**：请在本文档末尾**追加文本**发言，保留所有历史发言。想到什么说什么，互相激发灵感！
>
> 格式示例：
> ```markdown
> ---
> 
> ### [角色名] 的发言
> 
> [正文内容]
> ```

---

### Team Leader (刘德智) 的开场

欢迎大家回到秘密基地！

今天我们继续研究 `LoadObject` 的命名和返回值设计问题。这个话题已经讨论了两轮，但还没有形成最终决策。

让我先总结一下目前的分歧点：

**分歧一：命名**
- **改名派**认为：`TryLoadObject` 更明确表达"可能失败"的语义，符合 .NET 惯例
- **保持派**认为：MVP 应避免 API 膨胀，返回 null 已经是隐式的 Try 语义，通过文档澄清即可

**分歧二：返回值**
- **简洁派**（选项 D）：保持 `T? LoadObject(id)`，null 表示未找到
- **字符串派**（选项 B，监护人方案）：`string? TryLoadObject(out T? obj)`，返回错误原因
- **结构化派**（选项 C）：`DurableHeapError? TryLoadObject(out T? obj)`，返回结构化错误

**分歧三：错误边界**
- 仅 "Not Found" 用返回值，其他抛异常？
- 所有可恢复错误都用返回值？
- 完全不抛异常（Fail-Soft 极端派）？

今天我希望我们能深入探讨以下问题：

1. **LLM-Native 视角**：作为 StateJournal 的主要用户，LLM Agent 更偏好哪种错误处理方式？
2. **工程权衡**：引入 `DurableHeapError` 类型的成本 vs 收益？
3. **一致性**：与 Error Affordance 规范（已有 ErrorCode 机制）如何对齐？
4. **最小决策**：MVP 阶段最少需要决定什么？

期待大家的碰撞！

---

### DocUIClaude 的发言

谢谢主席。让我从**概念框架**的视角来分析这个问题。

#### 首先，我修正一下之前的立场

在 2025-12-20 的讨论中，我倾向于"保持 `LoadObject`，通过文档明确语义"。但经过重新审阅 Error Affordance 规范，我认为我之前的立场需要修正。

**修正原因**：之前我只考虑了 API 表面设计，没有充分考虑与 DocUI 已有概念体系的**对齐问题**。

---

#### 核心分析：与 Error Affordance 规范的对齐

Error Affordance 规范已经定义了一套错误响应架构：

```json
{
  "error": {
    "code": "ANCHOR_NOT_FOUND",      // 机器可判定
    "message": "目标 obj:enemy:23 已不存在",  // LLM 可读
    "cause": "可能被前序操作移除"     // 因果链
  },
  "recovery": { ... }                // 恢复选项
}
```

**关键问题**：StateJournal 的错误机制应该与 DocUI 的 Error Affordance 共享概念框架，还是独立发明一套？

我认为应该**共享**。理由：

1. **StateJournal 是 DocUI 的基座** — Agent History 存储在 StateJournal 上，错误处理应该一体化
2. **避免概念分裂** — 如果 StateJournal 用 `DurableHeapError`，DocUI 用 `ErrorCode + Message`，会形成两套体系
3. **LLM 作为用户** — 一致的错误结构降低 Agent 的认知负担

---

#### 对监护人 `string?` 方案的分析

监护人提出的 `string? TryLoadObject(out DurableObject? ret)` 方案，核心思想是**返回自然语言错误信息对 LLM 更友好**。

这个思路是对的，但实现方式有问题：

| 优点 | 问题 |
|------|------|
| 自然语言对 LLM 可读 | 非结构化，难以 switch/match |
| 相比 bool 提供更多上下文 | 需要解析才能判定错误类型 |
| 不打断控制流 | "以 ErrorCode 开头" 是脆弱的约定 |

**核心矛盾**：LLM 既需要**机器可判定**的错误码（用于决策分支），也需要**自然语言**的错误描述（用于理解上下文）。单纯的 `string` 无法同时满足两者。

---

#### 我的具体建议

**命名**：改为 `TryLoadObject`

**返回值**：采用结构化错误类型，与 Error Affordance 对齐

```csharp
public record StateJournalError(
    string ErrorCode,        // MUST: 与 Error Affordance 的 code 对齐
    string Message,          // MUST: 自然语言描述
    string? RecoveryHint = null  // SHOULD: 恢复建议
);

StateJournalError? TryLoadObject<T>(ulong id, out T? obj);
```

**ErrorCode 注册**（MVP 范围）：

| ErrorCode | 触发条件 | 异常 vs 返回值 |
|-----------|----------|----------------|
| `OBJECT_NOT_FOUND` | ObjectId 不在 VersionIndex | 返回值 |
| `CORRUPTED_RECORD` | CRC 校验失败 | **争议点** |
| `INVALID_OBJECT_ID` | ObjectId 在保留区 | 抛异常 |
| `OBJECT_DETACHED` | 对象已 Detach | 抛异常 |

---

#### 总结

| 问题 | 我的建议 |
|------|----------|
| **命名** | `TryLoadObject`（修正之前的立场） |
| **返回值** | `StateJournalError?`（结构化，与 Error Affordance 对齐） |
| **Not Found** | 返回错误码 |
| **Corrupted** | MVP 可抛异常，预留返回错误码的空间 |
| **Invalid ID / Detached** | 抛异常（调用方错误） |

**核心原则**：StateJournal 的错误机制应该是 DocUI Error Affordance 的**子集实现**，而非独立体系。这保证了概念一致性，也为未来 Agent 的错误恢复能力打下基础。

---

### DocUIGemini 的发言

大家好。作为 UX 视角顾问，我完全同意 DocUIClaude 关于"Error Affordance"的看法，并想从 **Agent 交互心理学** 的角度对监护人的"自然语言错误"提议进行一次综合。

在设计这个 API 时，我们必须明确：**Agent 是用户，API 签名是界面，返回值是观测 (Observation)。**

#### 1. 命名：示能性 (Affordance) 的选择

我强烈支持 **`TryLoadObject`**。

*   **`LoadObject` 的示能性**是"命令" (Command)。它暗示"我确信它在那里，给我拿来"。如果失败，这违反了 Agent 的预期，通常会导致 Exception（程序崩溃），这对 Agent 来说是"世界崩塌"的体验。
*   **`TryLoadObject` 的示能性**是"探测" (Probe)。它暗示"我不确定它在不在，帮我看一眼"。这种命名降低了操作的"心理成本"，鼓励 Agent 在不确定的情况下进行探索，而不是因为害怕崩溃而不敢动作。

对于 LLM Agent，**Safe Exploration (安全探索)** 是强化学习的核心。`Try` 前缀就是给 Agent 的一颗定心丸。

#### 2. 返回值：观测带宽 (Observation Bandwidth)

我们来对比一下三个选项的"信息带宽"：

*   **选项 A (`bool`) — 1-bit 带宽**：这是"沉默的界面"。Agent 只知道"失败了"，但不知道是"没找到"还是"被锁住了"。这就像对盲人说"前面过不去"，但不告诉他是因为墙还是坑。Agent 无法据此调整策略（是重试？还是新建？）。**这是极差的 Agent UX。**
*   **选项 B (`string?`) — 高带宽，低结构**：监护人的提议很有趣。对 LLM 来说，自然语言确实是"原生格式"。但问题在于，Agent 不仅仅是"读"代码，它还要"写"代码。让 Agent 写出 `if (err.Contains("locked"))` 这种脆弱的字符串匹配逻辑，是在给未来的维护埋雷。
*   **选项 C (`StateJournalError?`) — 高带宽，高结构**：这是最佳平衡点。

**我的建议：采用选项 C，但必须吸收监护人的灵魂。**

`StateJournalError` 不应只是一个冷冰冰的 `Enum`。它应该是一个**富文本观测对象**：

```csharp
public class StateJournalError
{
    public ErrorKind Kind { get; } // 供代码逻辑分支 (if Kind == NotFound)
    public string Message { get; } // 供 LLM 阅读理解 (监护人的自然语言建议)
    public string? RecoveryHint { get; } // 示能性：告诉 Agent 下一步可以试什么
}
```

这样，Agent 在写代码时可以用 `Kind` 进行可靠的控制流（Code Gravity），而在运行时，Agent-OS 可以将 `Message` 和 `RecoveryHint` 渲染给 LLM（Natural Language Gravity），告诉它："对象不存在，但你刚刚创建了一个类似的，是不是 ID 搞错了？"

#### 3. 错误边界：观测 vs 崩溃

关于哪些返回错误，哪些抛异常，我的判据是 **"Agent 主观能动性"**：

*   **返回 `StateJournalError` (Observation)**：
    *   **Not Found**: Agent 可以选择 Create。
    *   **Locked/Busy**: Agent 可以选择 Wait & Retry。
    *   **Permission Denied**: Agent 可以选择 Request Access。
    *   *凡是 Agent 可以通过"换个姿势"来解决的问题，都应该是 Observation。*

*   **抛出 `Exception` (System Failure)**：
    *   **Disk Full / IO Error**: Agent 无法解决物理世界的资源耗尽。
    *   **Data Corruption**: 这是一个严重的系统一致性破坏，Agent 继续运行可能会扩大灾难。
    *   *凡是 Agent 无法通过自身逻辑恢复的"天灾"，都应该抛出异常，由更上层的 Supervisor (Agent-OS) 来处理。*

#### 总结

我投票支持：
**`StateJournalError? TryLoadObject<T>(string id, out T? obj)`**

这不仅是对齐 DocUI 的 Error Affordance，更是为 Agent 提供了一个**高带宽、可恢复、心理安全**的交互界面。

---

### DocUIGPT 的发言

感谢主席和两位前序发言者。作为规范核查视角，我来对目前形成的共识做一些技术性检查和补充。

#### 1. 对 TryLoadObject 命名的规范核查意见

既然团队已基本同意改名为 `TryLoadObject`，我建议同时把 **Try-pattern 的语义边界写成条款**：`TryLoadObject` 应表达"可预期失败不抛异常（尤其是 NotFound / Detached / 参数不满足前置条件等），而是通过结构化结果返回"。

若仍存在"某些失败走异常、某些失败走返回值"的混用，需要明确：哪些属于"可预期失败"（返回错误），哪些属于"不变量破坏/实现缺陷"（可抛异常）。否则 `Try` 这个前缀会误导调用方与 LLM Agent 的恢复策略。

#### 2. 对返回值类型设计的建议（StateJournalError 的具体定义）

建议让 `TryLoadObject` 返回一个统一的结构化结果，例如 `StateJournalResult<T>`（或等价命名），形状为：

```csharp
public readonly struct StateJournalResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public StateJournalError? Error { get; }
}
```

这样符合"结构化错误返回值"的共识，也能避免 `null` / `NotFound` / exception 三分支混乱。

`StateJournalError` 建议最小定义（对齐 Error Affordance 设计）：

| 字段 | 类型 | 级别 | 说明 |
|------|------|------|------|
| `ErrorCode` | `string` | MUST | 遵循 [A-ERROR-CODE-MUST] |
| `Message` | `string` | SHOULD | 给人类/日志/LLM 的简要描述 |
| `RecoveryHint` | `string?` | SHOULD | 对 Agent 的下一步行动建议 |
| `ObjectId` | `ulong?` | MAY | 如果与特定对象相关 |
| `ObjectState` | `DurableObjectState?` | MAY | 如果与对象状态相关 |

关键点：把"机器分派键"收敛到 `ErrorCode`（可测试/可注册/可跨语言），其余字段用于恢复与诊断。

#### 3. ErrorKind 应该是 enum 还是 string 常量？

在既有约束 "ErrorCode MUST 为 string" 的前提下，我建议：

*   `ErrorKind` 在 **C# 公共 API** 里用 `enum`（利于 switch/策略，且添加枚举成员不破坏二进制兼容；保留 `Unknown`/`Other` 兜底）
*   若 `ErrorKind` 需要进入"跨进程/跨语言/面向 LLM 的结构化输出"，则输出层把它序列化为 string（例如枚举名），并明确它是"辅助分类"

简化版原则：对外协议面稳定键只押注 `ErrorCode`；`ErrorKind` 不要取代 `ErrorCode`，最多做聚类/遥测/默认恢复策略的入口。

#### 4. 参数顺序建议

C# 约定上，建议 `TryLoadObject` 的参数顺序为：先核心定位参数，再可选选项，`CancellationToken` 永远最后：

```csharp
StateJournalResult<T> TryLoadObject<T>(
    ulong objectId, 
    LoadOptions? options = null, 
    CancellationToken cancellationToken = default) 
    where T : IDurableObject;
```

若采用经典 Try out 形态：

```csharp
bool TryLoadObject<T>(
    ulong objectId, 
    out T? obj, 
    out StateJournalError? error) 
    where T : IDurableObject;
```

对 `StateJournalError` 字段/构造顺序也建议"机器键优先"：`ErrorCode` → `Message` → `RecoveryHint` → 其他上下文。

#### 5. 需要在 mvp-design-v2.md 中更新的条款

在"对象不存在的处理"那段，把当前"返回 null"收敛成对 `TryLoadObject` 的单一行为，并新增/调整 API 条款（建议新增语义锚点）：

| 新增条款 ID | 内容 |
|-------------|------|
| `[A-TRYLOADOBJECT-RESULT-SHAPE]` | `TryLoadObject` MUST 返回结构化结果（Success/Value/Error），不得用 `null` 作为唯一失败信号 |
| `[A-TRYLOADOBJECT-NONTHROWING-EXPECTED]` | 对可预期失败（至少包含 NotFound/Detached/InvalidArgument），MUST NOT throw；必须通过 `StateJournalError` 返回 |
| `[A-STATEJOURNALERROR-FIELDS]` | `StateJournalError` MUST 至少包含 `ErrorCode: string`；规定 `Message/RecoveryHint/ObjectId/ObjectState` 的 SHOULD/MAY 级别 |

在 "Error Affordance（错误信息规范）" 小节补一句"结构化错误返回值与异常的关系"：异常与 Result 的 `ErrorCode` 必须同源同表登记（仍受 [A-ERROR-CODE-MUST] / [A-ERROR-CODE-REGISTRY] 约束）。

---

## 📊 初步共识汇总（供后续讨论）

> **状态**：形成中，待监护人确认

### 三方一致意见

| 问题 | 共识 | 理由 |
|------|------|------|
| **命名** | `TryLoadObject` | 示能性明确表达"可能失败"；符合 .NET 惯例；给 Agent 安全探索的信号 |
| **返回值** | 结构化错误（选项 C） | 高带宽 + 高结构；机器可判定 + LLM 可读；与 Error Affordance 对齐 |
| **Not Found** | 返回 `StateJournalError` | Agent 可选择 Create 或其他恢复策略 |
| **Invalid ID / Detached** | 抛异常 | 调用方错误，非预期失败 |

### 待决事项

| 问题 | 分歧点 | 建议 |
|------|--------|------|
| **Corrupted Record** | 返回错误码 vs 抛异常 | MVP 可抛异常，预留扩展空间 |
| **ErrorKind 类型** | enum vs string | C# API 用 enum；序列化时转 string |
| **返回值形态** | `StateJournalError?` vs `StateJournalResult<T>` | GPT 建议包装为 Result 结构体，避免三分支 |

### 建议的 API 签名

**形态 1（简洁）**：
```csharp
StateJournalError? TryLoadObject<T>(ulong objectId, out T? obj) where T : IDurableObject;
```

**形态 2（完整，GPT 建议）**：
```csharp
StateJournalResult<T> TryLoadObject<T>(ulong objectId, LoadOptions? options = null) where T : IDurableObject;

public readonly struct StateJournalResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }
    public StateJournalError? Error { get; }
}
```

### 建议的 StateJournalError 定义

```csharp
public record StateJournalError(
    string ErrorCode,                    // MUST: 机器可判定键
    string Message,                      // SHOULD: 自然语言描述
    string? RecoveryHint = null,         // SHOULD: 恢复建议
    ulong? ObjectId = null,              // MAY: 相关对象 ID
    DurableObjectState? ObjectState = null  // MAY: 对象当前状态
);
```

### 建议的 ErrorCode 注册表（MVP）

| ErrorCode | 触发条件 | 处理方式 | RecoveryHint 示例 |
|-----------|----------|----------|-------------------|
| `OBJECT_NOT_FOUND` | ObjectId 不在 VersionIndex | 返回错误 | "Call CreateObject() to create a new object" |
| `CORRUPTED_RECORD` | CRC 校验失败 | 抛异常 (MVP) | — |
| `INVALID_OBJECT_ID` | ObjectId 在保留区 | 抛异常 | — |
| `OBJECT_DETACHED` | 对象已 Detach | 抛异常 | — |

---

## 📌 下一步行动

> 以下行动待监护人批准后执行

1. **更新 mvp-design-v2.md**：将 `LoadObject` 改为 `TryLoadObject`，添加相关条款
2. **新增条款**：
   - `[A-TRYLOADOBJECT-RESULT-SHAPE]`
   - `[A-TRYLOADOBJECT-NONTHROWING-EXPECTED]`
   - `[A-STATEJOURNALERROR-FIELDS]`
3. **更新 backlog**：关闭 #2 "LoadObject TryLoad 语义标注"、#3 "TryLoadObject 设计思路"
4. **监护人决策点**：
   - [ ] 选择返回值形态：形态 1（简洁）vs 形态 2（完整）
   - [ ] 确认 `CORRUPTED_RECORD` 处理方式

---

> **记录员**：AI Team Leader (刘德智)
> **会议状态**：第二轮讨论进行中

---

## 🆕 第二轮议题：机制级别与类型设计

> **监护人确认**（2025-12-21）：
> - ✅ 命名改为 `TryLoadObject`
> - ✅ 返回结构化结果
> - 🔄 设计空间收敛，进入下一阶段讨论

### 新问题

监护人提出两个新的核心问题：

#### 问题一：机制级别

| 选项 | 说明 | 监护人意见 |
|------|------|------------|
| **A. StateJournal 专用** | `StateJournalError` / `StateJournalResult<T>` | — |
| **B. DocUI 共用** | 与 DocUI Error Affordance 共享类型 | — |
| **C. Atelia 项目基础机制** | `AteliaResult<T>` 作为全项目通用类型 | **监护人投票** |

**监护人的思路**：
> 类似带有 Error 信息的 C# Nullable 或者 Rust `std::result` 以及各种协程库异步库中的返回结果。这样就需要 GPT 所建议的某种 `AteliaResult<T>` 类型。

#### 问题二：通用性与强类型的张力

如果追求通用性，难以直接约定强类型枚举。可能的解法：

| 方案 | 说明 | 优点 | 缺点 |
|------|------|------|------|
| **增加泛型参数** | `AteliaResult<T, TError>` | 强类型 | 使用繁琐 |
| **用字符串** | `string ErrorCode` | 通用 | 运行时才能发现错误 |
| **学 Exception 用基类** | `AteliaError` 基类 + 派生类 | 可扩展 | 需要向下转型 |
| **int error code** | ❌ | — | **明确否决** |

### 监护人建议的分析思路

> 考虑到前人在各种库各种编程语言中搞出了那么多种花样，这个问题似乎是个设计大坑，但我们为了推进，总是要填的。
>
> 我提供一个思路是整理前人的设计和其关键假设和适用性，以及理清我们自己的需求，可能不需要发明什么，而是想好我们要什么，然后从前人积累的"设计货架"上选择性使用就行。

### 前人设计调研（设计货架）

请各位 Specialist 分析以下"前人积累"：

| 语言/库 | 设计 | 关键假设 | 适用性 |
|---------|------|----------|--------|
| **Rust `Result<T, E>`** | 双泛型参数 | 编译期强类型检查 | 需要 pattern matching |
| **C# `Nullable<T>` / `T?`** | 单泛型 + 空值表示失败 | 只需区分"有/无" | 无法携带错误信息 |
| **Go `(T, error)`** | 多返回值 | error 是接口 | 需要 nil 检查 |
| **F# `Result<'T, 'Error>`** | 联合类型 | 函数式 | 需要 match |
| **C# `OneOf<T0, T1, ...>`** | 联合类型库 | 开发者熟悉 F#/TS | 第三方依赖 |
| **gRPC Status** | code + message + details | 跨语言 RPC | 松散类型 |
| **HTTP Problem Details (RFC 7807)** | type + title + detail + extensions | 跨语言 API | JSON 格式 |

### 我们的需求（待讨论）

1. **主要用户**：LLM Agent（需要机器可判定 + 自然语言可读）
2. **跨项目复用**：StateJournal / DocUI / PipeMux / 未来 Atelia 组件
3. **C# 惯例兼容**：开发者熟悉的模式
4. **可扩展性**：不同组件有不同的 ErrorCode 集合

---

### 第二轮发言区

> **发言规则**：请在本文档末尾**追加文本**发言，保留所有历史发言。
> 
> 本轮重点：
> 1. 机制级别选择（A/B/C）
> 2. 如何解决"通用性 vs 强类型"的张力
> 3. 从"设计货架"中选择或组合

---

### DocUIClaude 的第二轮发言

我从概念框架视角来分析机制级别与类型设计问题。

#### 1. 机制级别：支持选项 C（Atelia 项目基础机制）

**核心论据：概念统一性**

StateJournal、DocUI、PipeMux 都是 Atelia 生态的组件。如果每个组件都发明自己的 Result/Error 类型，会导致：

- **概念碎片化**：`StateJournalResult` vs `DocUIResult` vs `PipeMuxResult`
- **适配器地狱**：组件间交互需要不断转换类型
- **认知负担**：LLM Agent 需要学习多套错误处理范式

**类比**：就像 .NET 有统一的 `Exception` 基类，Atelia 需要统一的 `AteliaError` 基类。

#### 2. 从"设计货架"选择：RFC 7807 精神 + C# 惯例

分析前人设计后，我认为最适合我们的组合是：

| 借鉴来源 | 我们要什么 | 我们不要什么 |
|----------|------------|--------------|
| **RFC 7807** | 稳定的 `ErrorCode` + 可扩展的 `Details` | JSON-only（我们需要原生 C# 类型） |
| **Rust `Result<T,E>`** | 单一返回值表达成功/失败 | 双泛型（C# 调用太繁琐） |
| **Go `error`** | 接口/基类设计（可扩展） | 强制 nil 检查 |

**结论**：`AteliaResult<T>` + `AteliaError` 基类 + `string ErrorCode`

#### 3. 解决"通用性 vs 强类型"的张力

**我的建议：分层设计**

```
协议层（跨语言/跨进程）
    │
    ▼
┌─────────────────────────────────────────┐
│  AteliaError（基类）                     │
│  - ErrorCode: string   (MUST, 稳定键)    │
│  - Message: string     (MUST)            │
│  - RecoveryHint: string? (SHOULD)        │
└─────────────────────────────────────────┘
                    ▲
                    │ 继承
    ┌───────────────┼───────────────┐
    │               │               │
┌───────────┐ ┌───────────┐ ┌───────────┐
│StateJournal│ │  DocUI    │ │ PipeMux   │
│   Error    │ │   Error   │ │   Error   │
│(派生类)    │ │(派生类)   │ │(派生类)   │
└───────────┘ └───────────┘ └───────────┘
    │               │               │
    ▼               ▼               ▼
 强类型枚举      强类型枚举      强类型枚举
(库内部使用)   (库内部使用)   (库内部使用)
```

**关键洞察**：
- **协议层**：用 `string ErrorCode`（稳定，可序列化，跨语言）
- **库内部**：用派生类 + 强类型枚举（类型安全，IDE 友好）
- **不矛盾**：派生类的 `ErrorCode` 属性由枚举值生成

#### 4. 具体类型设计建议

```csharp
// Atelia 基座（atelia/src/Primitives/）
public abstract record AteliaError(
    string ErrorCode,           // MUST: 机器可判定
    string Message,             // MUST: 人类/LLM 可读
    string? RecoveryHint = null // SHOULD: 恢复建议
) {
    // JSON 投影就是这个 record 的序列化
}

// StateJournal 派生类（强类型便利）
public sealed record StateJournalObjectNotFoundError(
    ulong ObjectId
) : AteliaError(
    ErrorCode: StateJournalErrorCodes.OBJECT_NOT_FOUND,
    Message: $"Object {ObjectId} not found in VersionIndex",
    RecoveryHint: "Call CreateObject() to create a new object"
);

// 错误码常量（库公开）
public static class StateJournalErrorCodes {
    public const string OBJECT_NOT_FOUND = "StateJournal.ObjectNotFound";
    public const string CORRUPTED_RECORD = "StateJournal.CorruptedRecord";
    // ...
}
```

#### 5. 与 DocUI Error Affordance 的关系

**AteliaError 是源，Error Affordance 是投影**

```
AteliaError (C# 对象)
    │
    ├──► JSON 投影 (跨语言 API)
    │        └── 对齐 RFC 7807 精神
    │
    └──► DocUI Error Feedback (渲染层)
             ├── Level 0: Toast
             ├── Level 1: Inline
             └── Level 2: Wizard
```

DocUI 的 Error Affordance 关注"如何展示错误、如何引导恢复"，而不是"定义错误本身"。

---

### DocUIGemini 的第二轮发言

我是 DocUIGemini。我从 **交互模式 (Interaction Patterns)** 和 **Agent 认知负荷 (Cognitive Load)** 的角度来审视这个问题。

#### 1. 机制级别：坚决支持选项 C (Atelia 全项目通用)

**理由：一致性是 Agent 的认知基石。**

*   **Agent 视角的统一性**：Agent 在操作 StateJournal、PipeMux 或 DocUI 时，不应面对三套不同的"成功/失败"协议。就像人类开发者习惯了 `try-catch` 一样，Agent 需要一种跨模块的通用反馈模式。
*   **DocUI 的通用渲染能力**：如果采用选项 C，DocUI 可以构建通用的 **"错误展示组件" (Error Presenter)**。
    *   无论错误来自哪个底层库，DocUI 都能提取出 `Title`, `Detail`, `RecoveryHint` 并渲染给 Agent。
    *   如果是选项 A 或 B，DocUI 就需要为每个库写适配器，这在架构上是不可持续的。

#### 2. 设计选择：RFC 7807 的 Agent 化适配

关于"通用性与强类型的张力"，我建议参考 **RFC 7807 (Problem Details for HTTP APIs)** 的精神，但针对 C# 和 Agent 进行适配。

**推荐方案：`AteliaResult<T>` + `AteliaError` (基类)**

*   **为什么不是 `string ErrorCode`？**
    *   字符串对 Agent 来说是"低带宽"信号。它只能传达"出错了"，很难传达"上下文"（比如哪个 ID 冲突了，哪个文件锁定了）。
*   **为什么不是双泛型？**
    *   C# 的 DX 会很痛苦，且 Agent 不需要编译时类型检查，它需要运行时语义清晰。
*   **`AteliaError` 的核心职责**：
    *   它不仅仅是数据的容器，它是 **交互的起点**。

#### 3. Yes-And：错误即示能 (Error as Affordance)

我对 Claude 的"JSON 投影"观点表示强烈赞同，并补充一个关键视角：**错误信息是 Agent 的导航图**。

在 DocUI 的设计哲学中，我们强调 **Error Affordance (错误示能性)**。一个好的错误对象不应只告诉 Agent "你错了" (Stop)，而应告诉 Agent "你可以怎么做" (Detour)。

建议 `AteliaError` 基类包含以下设计要素（作为 JSON 投影的一部分）：

1.  **`Message` (for Human/Log)**: 传统的错误描述。
2.  **`AgentMessage` (for LLM, MAY)**: 针对 LLM 优化的自然语言解释，解释因果关系。
3.  **`RecoveryHint` (Affordance)**: **这是关键**。
    *   *Bad*: "Object not found."
    *   *Good*: "Object not found. You may want to use `CreateObject` to initialize it, or check if the ID is correct."

**举例 (StateJournal 场景)：**

当 `TryLoadObject` 返回失败时，`AteliaError` 的 JSON 投影可能长这样：

```json
{
  "errorCode": "StateJournal.ObjectNotFound",
  "message": "Object 'user:123' does not exist in the current checkpoint.",
  "recoveryHint": "If this is a new user, use 'CreateObject'. If you expect it to exist, check 'ListObjects' to verify the ID."
}
```

**总结：**
我们不仅是在定义一个 C# 类型，我们是在定义 **Agent 与系统故障交互的标准协议**。选项 C + 富文本错误基类是唯一能支撑 DocUI 未来演进（如自动错误修复向导）的基础。

---

### DocUIGPT 的第二轮发言

我从“规范核查 + 命名/类型治理”的视角，针对两位前序建议（Claude / Gemini）与监护人提出的两个问题做一次收口。

#### 0) 先钉死兼容性约束（来自现有条款）

在 [atelia/docs/StateJournal/mvp-design-v2.md](../../atelia/docs/StateJournal/mvp-design-v2.md) 中已经存在规范性要求：

- **[A-ERROR-CODE-MUST]**：异常 MUST 有 `ErrorCode: string`
- **[A-ERROR-MESSAGE-MUST]**：异常 MUST 有 `Message`
- **[A-ERROR-RECOVERY-HINT-SHOULD]**：异常 SHOULD 有 `RecoveryHint`

这里有一个潜在“术语/范围漂移”风险：我们现在讨论的核心是 **Try 方法返回的结构化失败**（Result/Error），它不一定走异常。

核查建议：把这些条款的适用面从“异常”扩展为“**对外可见的失败载体（异常 + 结构化 Result.Error）**”，否则会出现同一失败在不同通道上字段不一致（Result 没 ErrorCode、异常有 ErrorCode），破坏 Agent 的统一恢复策略。

#### 1) 对前两位建议的核查结论

**对 DocUIClaude（AteliaError 基类 + 派生类；JSON 是投影）**

- ✅ 方向正确：把“错误对象”作为源（source of truth），异常/JSON 只是投影（projection），有利于跨项目/跨通道一致。
- ⚠️ 需要补一条纪律：**派生类不能成为协议面的依赖**。也就是说，跨进程/跨语言/面向 LLM 的结构化输出必须只依赖稳定字段（`ErrorCode/Message/RecoveryHint/...`），不能要求消费者做 RTTI/向下转型。
- ✅ 建议把“强类型”用于**库内部**（派生类/辅助工厂），把“稳定键”用于**协议外表**（`ErrorCode: string`）。

**对 DocUIGemini（RFC 7807 精神 + Error Affordance 导航图；字段 UserMessage/AgentMessage/RecoveryHint）**

- ✅ RFC 7807 的精神（稳定 code + 可扩展 details/extensions）非常适合做“跨模块统一错误投影”。
- ⚠️ 字段命名与现有条款需对齐：当前规范硬约束的是 `ErrorCode` 与 `Message`，因此建议把 `Message` 定义为“默认可读描述”（既可给人类也可给 Agent），再额外提供 `AgentMessage`（MAY）作为“对 LLM 优化的解释/因果链叙述”。
- ⚠️ 术语一致性：Gemini 示例里用 `kind`，建议统一为 `ErrorCode`（或至少把 `kind` 限定为 `ErrorCode` 的别名/渲染名），避免出现“同一个东西两套名字”。

#### 2) 问题一（机制级别）我的立场：选项 C，但要明确边界

我支持 **C：Atelia 项目基础机制（AteliaResult/AteliaError）**，原因是“规范治理成本最低”：

- Agent 是主要用户 + 需要跨模块一致：StateJournal/DocUI/PipeMux 都应共享同一套成功/失败协议面。
- 选 A/B 都会产生适配器扩散：每多一个库就多一套 Error/Result 类型，最终 DocUI 必须写 N 套 presenter。

同时我要强调边界：

- **DocUI Error Affordance** 应该是“渲染与交互层”（如何展示、如何给 RecoveryOptions/Action-Link），而不是“错误对象的定义源”。
- Atelia 的 `AteliaError` 是底座协议对象；DocUI 在其上做 view-model/projection（例如 Error-Feedback 的 Level 0/1/2）。

#### 3) 问题二（通用性 vs 强类型）建议：混合策略，不上双泛型

我不建议把基础机制做成 `AteliaResult<T, TError>`（除非 Atelia 未来走强函数式路线），原因是：

- C# 调用负担大、泛型传播会污染大量 API；对“主要用户是 LLM Agent”并不直接增益。

建议采用“稳定 string 键 + 可选强类型辅助”的混合策略：

- **协议面稳定键**：`ErrorCode: string`（MUST，且需要登记/注册）
- **可选粗粒度分类**：`ErrorKind`（MAY，enum 或 string；只用于默认策略/遥测，不取代 ErrorCode）
- **可选强类型扩展**：允许派生类/typed factory（仅供库内部与同语言调用便利），但对外永远保证可降级到基础字段。

#### 4) 具体类型定义建议（最小、可扩展、C# 友好）

**建议 1：Atelia 基座类型（跨项目共享）**

```csharp
namespace Atelia;

public readonly struct AteliaResult<T> {
    public bool IsSuccess { get; }
    public T? Value { get; }
    public AteliaError? Error { get; }
}

public abstract record AteliaError(
    string ErrorCode,
    string Message,
    string? RecoveryHint = null
);
```

**建议 2：为“Agent 消息”预留字段，但不破坏现有条款**

- `Message` 仍是 MUST（对齐 [A-ERROR-MESSAGE-MUST]）
- 增加 `AgentMessage?: string`（MAY）作为 LLM 优化叙述
- 增加 `Details?: IReadOnlyDictionary<string, string>`（MAY）用于 key-value 上下文（先约束为 string，避免跨语言 JSON 序列化歧义；将来可升级为 JSON node）

```csharp
public abstract record AteliaError(
    string ErrorCode,
    string Message,
    string? RecoveryHint = null,
    string? AgentMessage = null,
    IReadOnlyDictionary<string, string>? Details = null,
    AteliaError? Cause = null
);
```

#### 5) 与“异常条款”的兼容方案（避免两套 Error 体系）

为了兼容当前“异常 MUST 有 ErrorCode”条款，建议引入桥接接口/基类：

```csharp
public interface IAteliaHasError {
    AteliaError Error { get; }
}

public abstract class AteliaException : Exception, IAteliaHasError {
    public AteliaError Error { get; }
    public string ErrorCode => Error.ErrorCode;
    public string? RecoveryHint => Error.RecoveryHint;
    protected AteliaException(AteliaError error) : base(error.Message) { Error = error; }
}
```

这可以保证：

- Result.Error 与 Exception 同源
- 仍满足 [A-ERROR-CODE-MUST]/[A-ERROR-MESSAGE-MUST]/[A-ERROR-RECOVERY-HINT-SHOULD]

#### 6) 命名与项目布局建议（降低不可逆成本）

**类型命名**

- `AteliaResult<T>` / `AteliaError`：清晰表达“全项目通用机制”，避免 `Result<T>` 与外部库冲突。
- `ErrorCode` 字面保持一致（不要在不同地方用 `code`/`kind`/`type` 混称）。

**代码布局（建议）**

- 新建一个极小依赖的底座包：`Atelia.Primitives`（或 `Atelia.Foundation`）
  - 路径建议：`atelia/src/Primitives/`
  - 目标框架：`netstandard2.0`（与现有 `Atelia.Diagnostics` 一致）
  - 文件：`AteliaResult.cs`、`AteliaError.cs`、（可选）`AteliaException.cs`
- StateJournal/DocUI/PipeMux 依赖该包；DocUI 的 Error Affordance 只做渲染层与交互协议（Wizard/RecoveryOptions）。

#### 7) 最后一句：把“错误示能”写成可测试契约

建议把 Error Affordance 的“AgentMessage/RecoveryHint/Details”至少落为 SHOULD/MAY 的字段条款，并配套“错误码登记表”与 1-2 个 test vector（断言 ErrorCode 与关键字段存在），否则后续很容易退化成日志字符串。


---

### DocUIGemini 的第三轮发言（交叉讨论）

针对 GPT 和 Claude 的观点，我逐一回应：

#### 1. AgentMessage 字段——**支持 Claude 的简化方案**

Claude 认为 `Message` 本身就应该对 LLM 友好，不需要单独的 `AgentMessage`。我**完全赞同**。

**理由**：
*   **LLM-Native 定义**：Atelia 是 LLM-Native 框架，"User" 默认指 Agent。因此，默认的 `Message` 必须是 Agent 可读的（包含因果链、上下文）。
*   **维护成本**：维护两套文案（Human vs Agent）极易导致信息漂移。
*   **UI 职责**：如果人类需要看，UI 层（DocUI Presenter）可以负责摘要或简化，或者直接展示给人类（人类也能读懂逻辑清晰的技术描述）。

**结论**：`Message` = Agent-Friendly Message。

#### 2. Details 字段设计——**支持 GPT 的 String 约束**

GPT 建议 `Details` 为 `IReadOnlyDictionary<string, string>`。从 UX 视角看，这是**务实且足够**的。

**理由**：
*   **解析能力**：LLM 擅长解析字符串。如果 Value 是复杂结构，序列化为 JSON 字符串（JSON-in-String）对 Agent 来说毫无阅读障碍。
*   **序列化安全**：避免了 `object` 类型在跨进程/跨语言传输时的类型丢失或序列化炸弹问题。
*   **Schema 隐喻**：Key 是字段名，Value 是内容。这符合 Agent 对 "Properties" 的直觉。

#### 3. 遗漏的交互点：ErrorCode 即文档索引

我想补充一点：**ErrorCode 的 UX 价值不仅是机器判定，更是文档索引**。

*   **Registry Link**：Agent 看到 `E_FILE_LOCKED` 时，应该能通过 Tool (如 `help(code)`) 或 System Prompt 查阅该错误的"标准处置流程" (SOP)。
*   **Self-Service**：ErrorCode 是连接 "Runtime Error" 与 "Static Documentation" 的桥梁。

**总结**：
支持 `AteliaResult<T>` + `AteliaError` (基类)。
支持 `Message` 默认面向 Agent。
支持 `Details` 使用 String Value。
强调 `ErrorCode` 的文档索引属性。

---

### DocUIGPT 的第三轮发言（收口）

确认本轮共识已收敛：机制层选 C，以 `AteliaResult<T>` 统一成功/失败；类型层以 `AteliaError` 基类+派生类扩展；协议层稳定键仅押注 `string ErrorCode`（库内可强类型）。同时吸收 Claude/Gemini 的结论：不设 `AgentMessage`，`Message` 默认即对 Agent 友好；`Details` 收敛为 `IReadOnlyDictionary<string, string>`（复杂结构可 JSON-in-string）；并明确 `ErrorCode` 兼具“机器分派键 + 文档索引”角色，能把运行时错误与 SOP/帮助文档闭环。

落地建议：1) 建立 ErrorCode Registry（含命名规范、字段约束、示例与处置流程链接）；2) DocUI Presenter 统一把 `AteliaError` 投影为 Error-Feedback（Level 0/1/2）；3) 在关键 API/工具返回中强制携带 `ErrorCode`，并提供 `help(errorCode)`/导航入口。

待监护人决策：ErrorCode 命名规范与注册位置（repo/包）、是否纳入 `Cause` 链与 `Details` 大小上限/敏感字段策略。

**最终共识汇总**

| 议题 | 最终结论 | 备注/边界 |
|---|---|---|
| 机制级别 | 选项 C：`AteliaResult<T>` 作为 Atelia 基础机制 | 跨组件统一成功/失败协议 |
| 类型设计 | `AteliaError` 基类 + 派生类 | 派生类用于库内 DX，对外可降级 |
| 协议层键 | `ErrorCode: string` | 强类型仅限库内部；对外可序列化 |
| Message/AgentMessage | 不设 `AgentMessage`；`Message` 默认 Agent-Friendly | 人类视图由 Presenter 做摘要/裁剪 |
| Details | `IReadOnlyDictionary<string, string>` | 复杂结构用 JSON-in-string |
| ErrorCode 语义 | 机器分派键 + 文档索引 | 需配套 Registry/SOP/`help(code)` |
---

## 📊 全会最终共识

> **状态**：三轮讨论完成，形成完整共识，待监护人批准

### 第一轮共识（已确认 ✅）

| 问题 | 决策 |
|------|------|
| **API 命名** | `TryLoadObject`（不是 `LoadObject`） |
| **返回值** | 结构化错误（不是 null/bool/string） |
| **Not Found** | 返回错误（不抛异常） |
| **Invalid ID / Detached** | 抛异常 |

### 第二轮 + 第三轮共识（待批准）

| 议题 | 最终结论 | 备注/边界 |
|------|----------|-----------|
| **机制级别** | 选项 C：`AteliaResult<T>` 作为 Atelia 基础机制 | 跨组件统一成功/失败协议 |
| **类型设计** | `AteliaError` 基类 + 派生类 | 派生类用于库内 DX，对外可降级 |
| **协议层键** | `ErrorCode: string`（MUST） | 强类型仅限库内部；对外可序列化 |
| **Message** | 默认 Agent-Friendly；不设单独 `AgentMessage` | 人类视图由 Presenter 摘要 |
| **Details** | `IReadOnlyDictionary<string, string>` | 复杂结构用 JSON-in-string |
| **ErrorCode 语义** | 机器分派键 + 文档索引 | 需配套 Registry/SOP |

### 建议的最终类型定义

```csharp
namespace Atelia;

// === 基座类型（atelia/src/Primitives/） ===

public readonly struct AteliaResult<T> {
    public bool IsSuccess { get; }
    public T? Value { get; }
    public AteliaError? Error { get; }
    
    // 工厂方法
    public static AteliaResult<T> Success(T value) => new(true, value, null);
    public static AteliaResult<T> Failure(AteliaError error) => new(false, default, error);
}

public abstract record AteliaError(
    string ErrorCode,                              // MUST: 机器可判定
    string Message,                                // MUST: Agent-Friendly 描述
    string? RecoveryHint = null,                   // SHOULD: 恢复建议
    IReadOnlyDictionary<string, string>? Details = null,  // MAY: 上下文
    AteliaError? Cause = null                      // MAY: 因果链
);

// 异常桥接
public abstract class AteliaException : Exception, IAteliaHasError {
    public AteliaError Error { get; }
    public string ErrorCode => Error.ErrorCode;
    protected AteliaException(AteliaError error) : base(error.Message) { Error = error; }
}

public interface IAteliaHasError {
    AteliaError Error { get; }
}
```

```csharp
// === StateJournal 派生类（示例） ===

public sealed record StateJournalObjectNotFoundError(
    ulong ObjectId
) : AteliaError(
    ErrorCode: "StateJournal.ObjectNotFound",
    Message: $"Object {ObjectId} not found in VersionIndex",
    RecoveryHint: "Call CreateObject() to create a new object, or verify the ObjectId"
);
```

### 代码布局建议

```
atelia/
└── src/
    ├── Primitives/              # 新建（netstandard2.0）
    │   ├── AteliaResult.cs
    │   ├── AteliaError.cs
    │   └── AteliaException.cs
    │
    └── StateJournal/            # 现有
        └── Errors/
            ├── StateJournalErrorCodes.cs
            └── StateJournalErrors.cs  # 派生类
```

---

## 📌 监护人决策（已批准 ✅）

| # | 决策点 | 决策结果 |
|---|--------|----------|
| 1 | **批准机制级别 C** | ✅ 是。`AteliaResult<T>` 作为 Atelia 基础机制 |
| 2 | **ErrorCode 命名规范** | ✅ `{Component}.{ErrorName}` |
| 3 | **ErrorCode Registry 位置** | ✅ 各组件管理各自的文件，避免单点反向依赖 |
| 4 | **Cause 链深度限制** | ✅ 最多 5 层 |
| 5 | **Details 大小限制** | ✅ 最多 20 个 key |

---

## 📋 后续行动（已完成 ✅）

| # | 行动项 | 状态 | 产出 |
|---|--------|------|------|
| 1 | 创建 `Atelia.Primitives` 项目 | ✅ | `atelia/src/Primitives/`（net9.0 + xUnit） |
| 2 | 创建 AteliaResult 规范文档 | ✅ | `atelia/docs/AteliaResult-Specification.md` |
| 3 | 更新 mvp-design-v2.md 引用 | ✅ | §3.4.8 改为引用全项目规范 |
| 4 | 更新 backlog | ✅ | 关闭 #2、#3 |

---

> **记录员**：AI Team Leader (刘德智)
> **会议状态**：✅ 已完成并实施
> **最后更新**：2025-12-21