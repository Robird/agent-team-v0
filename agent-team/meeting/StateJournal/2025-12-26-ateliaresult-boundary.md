# 畅谈会：AteliaResult 适用边界

> **日期**：2025-12-26
> **标签**：#design
> **主持人**：刘德智 (Team Leader / StandardsChair)
> **参与者**：Advisor-Claude, Advisor-Gemini, Advisor-GPT
> **状态**：进行中

---

## 背景

### 问题来源

L1 审阅发现 V-2：`TryGetValue` 返回类型不符合规范

- **规范要求**：`AteliaResult<object> TryGetValue(ulong key);`
- **实现现状**：`bool TryGetValue(ulong key, out TValue? value)`

### 监护人观点

> 对于只有{是/否}/{有/无}这样简单情况，而没有"函数向调用方反馈——为何没有"这样的复杂情况（相当于替代了 throw Exception）的函数，应该允许简单的 `bool TryGetSomeValue(<params>, out <Type> ret)` 形式。
>
> 而需要返回"为何没有返回值"的复杂情况，则应用 `AteliaResult<T> GetSomeValue(<params>)` 形式。

### 核心问题

1. `AteliaResult` 的适用边界在哪里？
2. 如何修订 `AteliaResult-Specification.md` §5.1？
3. `TryGetValue` 的返回类型应该是什么？

### 相关文档

- [AteliaResult-Specification.md](../../AteliaResult-Specification.md)
- [mvp-design-v2.md](../mvp-design-v2.md) §3.4.8
- [L1-Full-Review-Feedback.md](../review/L1-Full-Review-Feedback.md)

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

各位顾问，今天我们需要讨论 `AteliaResult` 的适用边界问题。

**核心议题**：

| 返回模式 | 适用场景 | 示例 |
|:---------|:---------|:-----|
| `bool TryX(..., out T)` | 简单的有/无判断 | `TryGetValue`, `TryParse` |
| `AteliaResult<T>` | 需要传达"为何没有" | `TryLoadObject` |

**讨论焦点**：

1. **边界划分原则**：什么情况下用 `bool + out`？什么情况下用 `AteliaResult`？
2. **命名约定**：`Try-` 前缀是否暗示了返回模式？
3. **一致性 vs 实用性**：全面采用 `AteliaResult` 更一致，但 `bool + out` 对 C# 开发者更熟悉

请各位顾问从各自视角分析这个问题。

---

### Advisor-Claude 发言

各位好。这个问题让我想到一个更本质的概念：**失败的信息量（Information Content of Failure）**。

#### 1. 本质区别：失败的信息熵

如果我们退一步看，`bool + out` 和 `AteliaResult<T>` 的本质区别在于：

| 维度 | `bool + out` | `AteliaResult<T>` |
|:-----|:-------------|:------------------|
| **失败的信息量** | 0 bit（只知道"没有"） | N bits（知道"为什么没有"） |
| **失败原因空间** | 单点（不存在） | 多元（不存在 / 无权限 / 已删除 / ...） |
| **恢复路径** | 调用者自行推断 | 错误对象显式提供 |

这让我想起**信息论中的区分**：
- `bool` 是**二值信号**：成功/失败
- `AteliaResult.Error` 是**携带 payload 的信号**：失败 + 失败原因

#### 2. 边界判定原则：失败原因是否可枚举为单点

我提议一个**决策树**：

```
失败时，调用者需要区分"为什么失败"吗？
│
├─ 否 → 失败原因只有一种（或等价于一种）
│       → bool + out 足够
│       → 示例：Dictionary.TryGetValue（失败 = 键不存在）
│
└─ 是 → 失败原因有多种，且调用者需要据此选择不同恢复策略
        → AteliaResult<T>
        → 示例：TryLoadObject（不存在 / Detached / 损坏）
```

**核心公式**：

$$
\text{Use } \mathtt{AteliaResult} \iff |\text{FailureReasons}| > 1 \land \text{CallerNeedsToDistinguish}
$$

#### 3. StateJournal 场景分析

让我们用这个框架分析 StateJournal 的几个 API：

| API | 失败原因空间 | 调用者需要区分？ | 建议返回类型 |
|:----|:-------------|:-----------------|:-------------|
| `DurableDict.TryGetValue` | {键不存在} | 否——不存在就是不存在 | `bool + out` ✅ |
| `TryLoadObject` | {不存在, Detached, 损坏, ...} | 是——Agent 需要不同恢复策略 | `AteliaResult<T>` ✅ |
| `TryParse` (VarInt 等) | {格式错误} | 通常否——格式错就是格式错 | `bool + out` ✅ |
| `TryCommit` | {冲突, 磁盘满, 损坏, ...} | 是——不同错误不同处理 | `AteliaResult<T>` ✅ |

**`DurableDict.TryGetValue` 的特殊性**：

它本质上是一个**纯查询操作**，类似于字典查找。"键不存在"是唯一的失败语义——不需要告诉调用者"为什么不存在"，因为答案永远是"因为你没放进去"。

这与 `TryLoadObject` 形成对比：
- `TryLoadObject` 的失败可能是"从未创建"、"已被 Detach"、"数据损坏"——Agent 需要知道是哪种，才能选择正确的恢复策略。

#### 4. 对 AteliaResult 规范的修订建议

我建议在 `AteliaResult-Specification.md` §5.1 增加一个**决策矩阵**：

```markdown
### 5.1 何时使用 Result vs bool+out vs 异常

| 场景特征 | 使用方式 | 理由 |
|----------|----------|------|
| 失败原因单一且调用者无需区分 | `bool + out` | 最简模式，C# 惯用法 |
| 失败原因多元，调用者需选择恢复策略 | `AteliaResult.Failure` | 携带失败信息，支持机器判定 |
| 不可恢复故障（系统级错误） | `AteliaException` | 需要上层 Supervisor 介入 |

**判定流程**：

1. 失败是否可恢复？
   - 否 → 抛异常
   - 是 → 继续判断
2. 失败原因是否多元且调用者需要区分？
   - 否 → `bool + out`
   - 是 → `AteliaResult<T>`
```

#### 5. 命名约定建议

关于 `Try-` 前缀的语义：

| 前缀 | 返回类型 | 语义 |
|:-----|:---------|:-----|
| `TryX(..., out T)` | `bool` | "尝试做 X，告诉我成功与否" |
| `TryX(...)` | `AteliaResult<T>` | "尝试做 X，告诉我结果详情" |
| `X(...)` | `T` 或 `void` | "做 X，失败就抛异常" |

这保持了 C# 社区对 `Try-` 前缀的惯用理解，同时通过**签名**（有无 `out` 参数）区分两种模式。

#### 6. 类比：HTTP 状态码

这个问题让我想到 HTTP 状态码的设计：

- `404 Not Found` 是单一语义——资源不存在
- 但 `4xx` 家族有很多成员——`401`、`403`、`409`...

如果 API 的失败语义只有"404"，用 `bool` 足够。
如果 API 可能返回整个 `4xx` 家族，用 `AteliaResult` 携带具体错误码。

#### 7. 总结

**核心洞见**：`AteliaResult` 的价值在于**失败时携带可操作信息**。当失败没有需要传达的额外信息时，它就退化为冗余包装。

**对 V-2 问题的回答**：`DurableDict.TryGetValue` 使用 `bool + out` 是合理的——它符合"失败原因单一"的简单模式。规范应当修订以明确这一边界。

**决策建议**：

| 决策项 | 建议 |
|:-------|:-----|
| `DurableDict.TryGetValue` 返回类型 | 维持 `bool + out`（实现正确，规范需修订） |
| `AteliaResult-Specification.md` 修订 | 增加 §5.1 决策矩阵 |
| 命名约定 | `TryX(..., out T)` vs `TryX(): AteliaResult<T>` |

---

### Advisor-Gemini 发言

Claude 的"失败信息熵"框架很有洞察力。让我从**开发者体验（DX）**的角度补充分析。

#### 1. C# 开发者的肌肉记忆

`bool TryX(..., out T)` 是 C# 生态系统中根深蒂固的模式：

```csharp
// 每个 C# 开发者都写过的代码
if (dict.TryGetValue(key, out var value)) {
    // use value
}

if (int.TryParse(input, out var number)) {
    // use number
}
```

**这个模式的 DX 优势**：
- ✅ **零认知负担**：不需要学习新的 Result 类型
- ✅ **IDE 友好**：自动补全 `out var`
- ✅ **模式匹配友好**：与 `if` 语句自然结合
- ✅ **无分配开销**：`out` 参数是栈上传递

#### 2. LLM Agent 消费两种返回值的对比

作为 LLM-Native 框架，我们需要考虑 Agent 如何消费这些 API：

| 维度 | `bool + out` | `AteliaResult<T>` |
|:-----|:-------------|:------------------|
| **成功路径代码** | `if (TryGet(k, out v)) { use(v); }` | `var r = Get(k); if (r.IsSuccess) { use(r.Value); }` |
| **失败路径代码** | `// just skip or log` | `if (r.IsFailure) { analyze(r.Error); }` |
| **Agent 推理复杂度** | 低——二元判断 | 中——需要解析 Error 结构 |
| **RecoveryHint 可用性** | ❌ 无 | ✅ 有 |

**核心洞察**：
- 当 Agent 只需要知道"有没有"时，`bool` 更简洁
- 当 Agent 需要知道"为什么没有"以选择恢复策略时，`AteliaResult` 更有价值

对于 `DurableDict.TryGetValue`：
- Agent 问："这个 key 有值吗？" → 答案是 `true/false`，足够
- 如果 `false`，Agent 的下一步通常是 `Set(key, defaultValue)`——不需要知道"为什么没有"

#### 3. 代码可读性对比

**场景 A：简单查找**

```csharp
// bool + out（简洁）
if (dict.TryGetValue(key, out var val)) {
    Process(val);
}

// AteliaResult（冗余）
var result = dict.GetValue(key);
if (result.IsSuccess) {
    Process(result.Value);
}
```

**场景 B：需要错误信息**

```csharp
// bool + out（信息不足）
if (!TryLoadObject(id, out var obj)) {
    // 为什么加载失败？不知道...
    Log.Error("Load failed");  // 无法提供有意义的日志
}

// AteliaResult（信息丰富）
var result = TryLoadObject(id);
if (result.IsFailure) {
    Log.Error($"Load failed: {result.Error.Message}");
    if (result.Error.ErrorCode == "StateJournal.ObjectNotFound") {
        CreateNew(id);  // 针对性恢复
    }
}
```

#### 4. 视觉隐喻：门禁卡 vs 诊断报告

- **`bool + out`** 像**门禁卡**：刷卡，绿灯/红灯，进/不进
- **`AteliaResult`** 像**诊断报告**：检查结果，如果有问题，告诉你是什么问题、建议怎么处理

`TryGetValue` 就是刷门禁卡——你只关心能不能进，不需要诊断报告。

#### 5. DX 设计建议

| 原则 | 说明 |
|:-----|:-----|
| **最小惊讶** | 遵循 C# 惯例：字典查找用 `bool + out` |
| **按需复杂** | 只在需要错误详情时引入 `AteliaResult` |
| **命名即文档** | `TryX(..., out)` vs `TryX()` 通过签名暗示返回模式 |
| **Agent 友好** | 复杂操作返回 `AteliaResult` 以提供 RecoveryHint |

#### 6. 对规范修订的建议

在 `AteliaResult-Specification.md` 中增加一个"直觉测试"：

> **直觉测试**：当操作失败时，调用者的第一反应是：
> - "哦，没有就算了" → `bool + out`
> - "为什么失败？我需要决定下一步" → `AteliaResult<T>`

#### 7. 总结

**Claude 提供了理论框架（信息熵），我补充 DX 视角**：

- `DurableDict.TryGetValue` 是"刷门禁卡"场景，`bool + out` 最合适
- 保持与 C# 社区惯例一致，降低学习曲线
- `AteliaResult` 保留给真正需要"诊断报告"的场景

**支持 Claude 的结论**：维持 `bool + out`，修订规范以明确边界。

---

### Advisor-GPT 发言

以下从“规范核查 + 精确性验证（可判定/可测试/可执行）”视角，给出对现行 [AteliaResult-Specification.md](../../AteliaResult-Specification.md) 的问题定位与条款级修订草案。

---

## 1) 现行规范核查（与本议题直接相关）

### 1.1 §5.1 的结构性缺口

现行 §5.1 仅给出“`AteliaResult` vs 异常”的二分。

- **缺失第三类**：C# 生态中广泛且已被本次讨论认可的 `bool + out`（典型：`Dictionary.TryGetValue`）。
- **直接后果**：规范文本与实际 API 设计空间不闭合，导致实现被误判为“不符合规范”。

### 1.2 `Try-` 前缀的语义边界未被钉死

会议共识倾向：

- `DurableDict.TryGetValue` 走 `bool + out`
- `TryLoadObject` 等复杂操作走 `AteliaResult<T>`

但现行规范对“`Try` 前缀是否暗示返回形态、是否承诺 non-throwing”缺乏 Normative 约束。

若不补齐：

- 调用方会对 `TryXxx` 形成两套心智模型（bool vs Result），**不可预测**。
- L1/L2 审阅与自动化规则无法机械判定（同名模式却不等价）。

### 1.3 “不可恢复故障”分类边界含混

现行 §5.1 把“磁盘满 / IO 错误”归入“不可恢复”。这在工程上并非总成立：

- 对某些调用方而言，“磁盘满”可以通过释放空间后重试恢复。
- 对库层而言是否抛异常，往往取决于**抽象层级/控制域**，而非错误本体。

结论：§5.1 应把分类基准改为可判定维度（如“是否为可预期域内失败”“是否需要诊断 payload”），而不是主观的“可恢复/不可恢复”。

---

## 2) §5.1 修订草案（替换全文，Normative）

> 目标：让规范能机械判定 API 选型；对 C# 调用方最小惊讶；对 Agent 保留诊断能力。

### 5.1 何时使用 `bool + out` vs `AteliaResult<T>` vs 异常

本节定义三类失败表达：

- **Classic Try-pattern**：`bool TryX(..., out T value)`
- **Result-pattern**：`AteliaResult<T> X(...)`（或 `AteliaResult<T> TryX(...)`，见命名条款）
- **Exception-pattern**：抛出 `AteliaException`（或其他异常）

#### [ATELIA-FAILURE-CLASSIFICATION]

对外可见的失败 MUST 被归类为以下三类之一：

1. **Expected Domain Failure（可预期域内失败）**：由输入/状态/权限/业务条件导致，调用方可能据此采取下一步动作。
2. **Invariant Breach（不变量破坏）**：实现错误、契约被破坏、内部状态不可能、参数违反“必须成立”的前置条件。
3. **Infrastructure Fault（基础设施故障）**：IO/存储/网络/进程环境等导致的失败，通常需要上层策略介入或重试。

#### [ATELIA-BOOL-OUT-WHEN]

API MAY 使用 `bool + out` 形式，当且仅当同时满足：

- 对调用方而言，失败空间在语义上等价为**单一原因**（即：调用方不需要也不应区分失败原因）；并且
- 失败时不需要提供结构化诊断 payload（`ErrorCode` / `RecoveryHint` / `Details`）；并且
- 操作不涉及需要显式表达的 Infrastructure Fault（例如不直接执行 IO/持久化/跨进程通信等会产生多样失败的动作）。

约束：

- `TryX(..., out T value)` 在返回 `false` 时 MUST 将 `value` 置为 `default`。
- `TryX` 在面对 Expected Domain Failure 时 MUST NOT 以异常表达控制流。

#### [ATELIA-RESULT-WHEN]

API MUST 使用 `AteliaResult<T>` 形式，当任一条件成立：

- 失败原因**多元**，且调用方需要区分以选择不同恢复/分支策略；或
- 失败需要携带可机器判定的键（`ErrorCode`）或可操作的恢复建议（`RecoveryHint`）；或
- 操作可能遭遇 Infrastructure Fault 且需要以结构化方式呈现给调用方（例如提供可登记的 `ErrorCode`）。

约束：

- `AteliaResult<T>.Failure` MUST 携带 `AteliaError`，且满足本规范 §4.1 的所有 MUST 条款。

#### [ATELIA-EXCEPTION-WHEN]

API MUST 抛异常（推荐抛 `AteliaException`）当任一条件成立：

- 发生 Invariant Breach；或
- 发生 Infrastructure Fault 且该故障在当前抽象层不应被调用方以“正常分支”处理（即需要上层 Supervisor/Host 介入）。

说明（Informative）：若团队希望把部分 Infrastructure Fault 也纳入 `AteliaResult<T>`（以便 Agent 自动重试/降级），则 MUST 由组件规范明确哪些故障属于“域内可处理”，并为其分配 `ErrorCode`。

---

## 3) 命名约定条款草案（建议新增，Normative）

> 目标：让“方法名 + 签名”可机械推断失败通道，避免 `Try` 语义漂移。

建议新增一节（可置于 §5.1 后），至少包含：

#### [ATELIA-TRY-PREFIX-NONTHROWING]

方法名以 `Try` 开头时：

- 对 Expected Domain Failure，方法 MUST NOT 通过异常表达失败（允许返回 `false` 或 `AteliaResult.Failure`）。

#### [ATELIA-TRY-BOOL-SIGNATURE]

若方法返回类型为 `bool` 且用于表达“尝试”，则方法名 MUST 以 `Try` 开头，且 MUST 至少包含一个 `out` 参数承载成功产物（例如 `out T value`）。

#### [ATELIA-RESULT-NAMING-SHOULD]

返回 `AteliaResult<T>` 的方法 SHOULD 使用不带 `Try` 的动词名（例如 `LoadObject`、`Open`、`Commit`），以减少与 .NET Classic Try-pattern 的歧义。

#### [ATELIA-TRY-RESULT-NAMING-MAY]

若出于一致性或历史原因需要保留 `TryXxx(): AteliaResult<T>` 形式，则：

- 方法 MUST NOT 使用 `out` 参数承载成功产物（避免与 Classic Try-pattern 混淆）；并且
- 组件的 ErrorCode Registry MUST 覆盖该方法可能返回的主要 `ErrorCode`（至少列出 Top-N 常见错误）。

---

## 4) 修订差异（相对现行规范的最小变更集）

建议对 [AteliaResult-Specification.md](../../AteliaResult-Specification.md) 做如下最小修订：

1. **替换** §5.1：从“Result vs 异常”二分，升级为“`bool + out` vs `AteliaResult<T>` vs 异常”三分，并给出可判定条件（见上文 §2 草案）。
2. **新增** 命名约定条款：明确 `Try` 前缀的 non-throwing 语义，并钉死 `bool TryX(out ...)` 的签名约束；对 `AteliaResult<T>` 的命名给出 SHOULD/MAY 规则（见上文 §3 草案）。
3. **（可选但强烈建议）修正文档示例**：避免在同一文档同时示范 `TryX(out)` 与 `TryX(): AteliaResult<T>` 而不加说明；至少在示例处标注“这是 Try-Result 变体（见命名条款）”。

---

## 5) 仍需澄清的边界问题（建议纳入 open questions）

1. **Infrastructure Fault 是否允许用 `AteliaResult` 表达？**
    - 现行 §5.1 把 IO/磁盘满归入异常，但 Agent 场景常需要结构化重试/降级。建议在组件规范中显式列举“哪些基础设施故障被视为域内可处理”。
2. **“失败原因单一”的判定基准**：是“实现上只有一种失败”，还是“对调用方语义等价为一种失败”？建议采用后者（更可测试，也更贴近 Claude 的信息熵框架）。
3. **Classic Try 的异常豁免**：`bool + out` 是否允许抛异常？建议仅允许 Invariant Breach（例如 null 参数违反前置条件）抛异常，其余 Expected Domain Failure 必须用 `false`。
4. **Registry 覆盖粒度**：Try-Result 变体若被允许，Registry 是要求“全部 ErrorCode”还是“主要 ErrorCode”？上文给出“主要 Top-N”的折中，但需要主席裁决。

我支持两位顾问的结论：`DurableDict.TryGetValue` 使用 `bool + out` 是合理的；关键是把这一“第三类返回形态”以可判定条款写进规范，避免未来审阅误判与 API 语义漂移。
---

### 主持人总结 (刘德智)

感谢三位顾问的精彩分析！让我整理本次畅谈会的共识与后续行动。

#### 已达成共识

| 议题 | 共识 | 支持者 |
|:-----|:-----|:-------|
| `DurableDict.TryGetValue` 返回类型 | 维持 `bool + out`（实现正确） | Claude ✓ Gemini ✓ GPT ✓ |
| 规范需要修订 | 从二分升级为三分 | Claude ✓ Gemini ✓ GPT ✓ |
| 命名约定 | `TryX(out)` vs `TryX(): Result` 通过签名区分 | Claude ✓ Gemini ✓ GPT ✓ |
| 边界判定原则 | 失败原因单一 + 无需诊断 payload → `bool + out` | Claude ✓ Gemini ✓ GPT ✓ |

#### 核心洞见汇总

| 顾问 | 核心贡献 |
|:-----|:---------|
| **Claude** | "失败信息熵"框架——`bool` 是 0 bit 信号，`AteliaResult.Error` 是 N bits 信号 |
| **Gemini** | "门禁卡 vs 诊断报告"隐喻——DX 视角的直觉测试 |
| **GPT** | 条款级修订草案——可判定的三分类规范 |

#### 后续行动

| # | 行动 | 负责人 | 状态 |
|:-:|:-----|:-------|:----:|
| 1 | 将 GPT 的 §5.1 草案整合到 AteliaResult-Specification.md | DocOps | ✅ 完成 |
| 2 | 更新 mvp-design-v2.md `[A-DURABLEDICT-API-SIGNATURES]` 条款 | DocOps | ✅ 完成 |
| 3 | 关闭 V-2 Finding（规范侧修订） | Team Leader | ✅ 完成 |

#### 监护人决策

**2025-12-26**：监护人批准采纳畅谈会共识，已执行规范修订。

---

**畅谈会状态**：✅ 完成（已执行）
**完成时间**：2025-12-26
