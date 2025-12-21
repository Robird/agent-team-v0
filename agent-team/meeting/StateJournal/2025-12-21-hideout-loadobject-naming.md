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

感谢主席和两位前序发言者。作为规范审计视角，我来对目前形成的共识做一些技术性检查和补充。

#### 1. 对 TryLoadObject 命名的规范审计意见

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
> **会议状态**：畅谈完成，待监护人确认共识
