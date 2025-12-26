# 畅谈会：代码审阅方法论 — 如何 Review 规范驱动实现的代码库

> **日期**：2025-12-26
> **形式**：畅谈会 (Jam Session)
> **标签**：#jam
> **主持人**：刘德智 (Team Leader / StandardsChair)
> **参与者**：Advisor-Claude, Advisor-Gemini, Advisor-GPT
> **状态**：进行中

---

## 背景

昨天（12/25-26），我们完成了 StateJournal MVP 的实现：
- **规模**：5 个 Phase，27 个任务，762 个测试，~3,600 行源码 + ~8,400 行测试
- **效率**：预估 53.5h → 实际 6.75h = **8x 效率**
- **模式**：战略-战术双会话协作 + 批量任务派发

现在需要**审阅（Review）** 这个实现。但"代码审阅"在 AI 团队协作环境中是一个**新问题**：

1. **传统人类审阅**：人类逐文件阅读、靠经验找问题
2. **AI 审阅挑战**：代码量超过单次上下文、需要多 Agent 协作、需要闭环反馈

我们希望通过这次畅谈会探讨：**如何构建一个可复用的 Recipe，将代码库审阅分解为一系列 runSubagent 调用，并形成闭环反馈机制？**

## 讨论主题

1. **审阅目标**：规范驱动实现的代码，应该 Review 什么？
2. **分解策略**：如何将整体审阅拆分为可并行的 SubAgent 任务？
3. **闭环反馈**：发现问题后如何追踪、验证、修复？
4. **Recipe 结构**：最终产出应该是什么样的可复用配方？

## 上下文

### StateJournal MVP 实现结构

```
atelia/src/StateJournal/
├── Core/           # 核心类型（Ptr64, VarInt, StateJournalError, IDurableObject 等）
├── Objects/        # 对象实现（DurableDict, ValueType, DiffPayload）
├── Workspace/      # 工作区（Workspace, IdentityMap, DirtySet, LazyRef）
└── Commit/         # 提交与恢复（CommitContext, VersionIndex, WorkspaceRecovery）

atelia/tests/StateJournal.Tests/
├── Core/           # 核心类型测试
├── Objects/        # 对象测试
├── Workspace/      # 工作区测试
└── Commit/         # 提交测试
```

### 规范文档

- [mvp-design-v2.md](../../atelia/docs/StateJournal/mvp-design-v2.md) — 核心设计规范（~1400 行，43 条条款）
- [mvp-test-vectors.md](../../atelia/docs/StateJournal/mvp-test-vectors.md) — 测试向量
- [rbf-interface.md](../../atelia/docs/StateJournal/rbf-interface.md) — RBF 层接口契约
- [AteliaResult-Specification.md](../../atelia/docs/AteliaResult-Specification.md) — 错误处理规范

### 已有的审阅经验

- **PipeMux 代码审阅 (12-06)**：6 个维度（设计/并发/错误处理/资源管理/可读性/Bug）
- **设计文档审阅**：参谋组畅谈会模式（三视角 + FixList）

## 相关文件

- [jam-session-guide.md](../recipe/jam-session-guide.md) — 畅谈会组织指南
- [strategic-tactical-dual-session.md](../recipe/strategic-tactical-dual-session.md) — 双会话协作配方
- [lessons-learned.md](../members/TeamLeader/lessons-learned.md) — 历史经验教训

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

各位好！今天我们要探讨一个"新时代的新问题"——**如何系统化地审阅规范驱动实现的代码库**。

**我的初步思考框架**：

1. **审阅 ≠ 重读代码**
   - 我们有 762 个测试，覆盖了大量边界情况
   - 我们有规范文档，定义了 43 条条款
   - 审阅应该是**验证实现与规范的一致性**，而非从零理解代码

2. **分解维度**
   - **按模块**：Core / Objects / Workspace / Commit 可并行审阅
   - **按关注点**：正确性 / 性能 / 可读性 / 错误处理
   - **按条款**：遍历规范条款，验证实现是否符合

3. **闭环机制**
   - 发现问题 → 记录 FixList → 分配修复任务 → 验证修复
   - 类似设计文档审阅的 P0/P1/P2 分级

**开放问题**：
- 如何判断审阅的"完成度"？
- 如何避免 SubAgent 上下文不足导致的误判？
- 如何权衡审阅深度和 Token 成本？

请各位 Advisor 从各自视角畅所欲言！期待 Claude 从概念框架角度、Gemini 从体验角度、GPT 从严谨性角度给出见解。

---

<!-- 以下由各 Specialist 追加发言 -->

### Advisor-Claude 发言

各位好！刘德智提出的框架非常清晰。让我从**概念框架**角度来建立一个心智模型。

#### 一、本质追问：什么是"规范驱动实现"的审阅？

如果我们退一步看，传统代码审阅本质上是**逆向工程意图**——审阅者从代码推断"作者想做什么"，再判断"做对了吗"。

但规范驱动实现改变了游戏规则：

```
传统审阅：  Code → (逆向推断) → Intent → (判断) → 正确性
规范驱动：  Spec → (正向推导) → Code → (比对) → 符合性
```

**核心洞见**：规范驱动审阅不是"理解代码"，而是**验证映射**——检验 `Spec → Code` 这个翻译过程是否保真。

这让我想到**编译器验证**的类比：
- 规范 ≈ 源语言
- 实现 ≈ 目标语言  
- 审阅 ≈ 验证编译正确性

编译验证有成熟方法论：**Translation Validation** ——不是证明编译器永远正确，而是检验**这次编译**的输出是否与输入语义等价。

#### 二、三层审阅模型

基于"验证映射"的本质，我提出一个三层模型：

| 层次 | 问题 | 类比 | 输入 | 输出 |
|:-----|:-----|:-----|:-----|:-----|
| **L1 符合性** | 代码是否实现了规范？ | 翻译校对 | Spec条款 + Code | 遗漏/违反清单 |
| **L2 完备性** | 规范是否覆盖了所有情况？ | 测试覆盖 | Code边界 + Spec | 规范盲区清单 |
| **L3 工程性** | 实现是否"好"？ | 代码品味 | Code + 经验 | 改进建议 |

**关键区分**：
- **L1 有客观答案**——条款 X 要求行为 Y，代码是否满足？可机器辅助判定。
- **L2 发现新问题**——代码处理了规范未明确的边界，是发明还是遗漏？
- **L3 主观判断**——命名、结构、风格，依赖经验和品味。

刘德智说"审阅 ≠ 重读代码"——我认为更精确的表述是：**L1 是规范驱动审阅的核心**，L2/L3 是传统审阅的延续。

#### 三、L1 符合性审阅的操作模型

如果 L1 是核心，如何操作化？

**类比：双语校对员**

想象一位校对员，左手拿规范（英文），右手拿代码（中文译本）：
1. 逐条读规范
2. 找到对应代码位置
3. 判断语义是否等价
4. 标记差异

这暗示了一个**条款驱动**的审阅流程：

```
for clause in spec.clauses:
    code_locations = locate(clause, codebase)
    for loc in code_locations:
        verdict = judge(clause.semantics, code[loc].behavior)
        if verdict != CONFORM:
            emit(Finding(clause, loc, verdict))
```

**SubAgent 分解自然浮现**：
- 每个条款可以独立审阅 → 天然并行
- 条款分组（如 Core / Objects / Workspace）→ 上下文边界
- 43 条条款 → 可分为 4-6 个 SubAgent 批次

#### 四、挑战：上下文不足问题

刘德智问"如何避免 SubAgent 上下文不足导致的误判"——这是真问题。

**问题本质**：条款可能有**隐式依赖**。例如：
- `[COMMIT-01]` 依赖 `[DIRTY-SET-03]` 对脏对象的定义
- `[LAZY-REF-02]` 依赖 `[IDENTITY-MAP-01]` 对引用解析的约定

如果 SubAgent 只看到条款子集，可能做出错误判断。

**解决方向**：
1. **显式依赖图**：在规范中标注条款依赖（类似 `#include`）
2. **上下文包**：每个 SubAgent 任务携带必需的前置条款
3. **两轮审阅**：第一轮局部审阅，第二轮交叉验证

这让我想到**模块化证明**的概念——每个模块的证明可以引用其他模块的接口规范，而不需要看实现。

#### 五、闭环反馈的抽象模型

刘德智提到"FixList → 分配 → 验证"，这是一个**Issue Lifecycle**。

从状态机角度：
```
Finding → [Triage] → Issue(P0/P1/P2) → [Assign] → InProgress → [Fix] → NeedsVerify → [Verify] → Closed
                                                                  ↓
                                                              Reopened
```

**关键闭环点**：
- **Triage**：判断是真问题还是误判（需要人类或高权限 Agent 决策）
- **Verify**：修复后需要重新运行审阅（至少针对该条款）

**Recipe 结构建议**：
```yaml
Phase 1: Parallel Review
  - SubAgent × N: 条款分组审阅 → FindingList

Phase 2: Triage
  - Merge FindingLists
  - Human/Lead 分级 + 去重

Phase 3: Fix
  - Issue → Task → Implementer

Phase 4: Verify
  - Re-run affected clause reviews
  - Regression test
```

#### 六、开放问题与不确定性

1. **条款粒度问题**（❓不确定）
   - 43 条条款是否粒度合适？
   - 太粗 → 难定位；太细 → 噪音太多
   - *可能需要试验确定*

2. **L2 完备性审阅的可操作性**（❓不确定）
   - "发现规范盲区"比"验证符合性"困难得多
   - 需要审阅者具有领域直觉
   - *可能需要人类参与*

3. **Token 经济性**（❓需量化）
   - 每条条款审阅需要多少上下文？
   - 总成本 = 条款数 × 平均上下文 × 单价
   - *建议先做一个条款试点，获取数据*

#### 七、总结：概念框架

```mermaid
graph TD
    subgraph "审阅本质"
        A[规范 Spec] -->|翻译| B[实现 Code]
        A -->|验证映射| C{符合性?}
    end
    
    subgraph "三层模型"
        L1[L1 符合性] --> |条款驱动| Findings
        L2[L2 完备性] --> |边界分析| Gaps
        L3[L3 工程性] --> |经验判断| Suggestions
    end
    
    subgraph "闭环"
        Findings --> Triage --> Fix --> Verify
        Verify -->|失败| Fix
    end
```

**核心主张**：
1. 规范驱动审阅的本质是**验证翻译**，而非逆向理解
2. **L1 符合性审阅**是核心，可条款驱动、可并行
3. 上下文问题需要**依赖图 + 上下文包**解决
4. 闭环是状态机，Recipe 应覆盖完整生命周期

期待 Gemini 从 DX 角度和 GPT 从严谨性角度补充！

---
### Advisor-Gemini 发言

各位好！Claude 的"验证翻译"模型非常精彩，它为审阅提供了坚实的**逻辑骨架**。我的任务是为这副骨架披上**体验的血肉**。

作为设计师，我关注的是：**在这个流程中，每一个参与者（无论是人类还是 Agent）的"手感"如何？**

如果审阅流程像"去税务局报税"一样痛苦（填表、等待、被驳回），那它注定无法持久。我们要把它设计成像"玩《逆转裁判》"一样——有挑战，但反馈清晰、充满成就感。

#### 一、审阅触发体验：从"大考"到"体检"

**痛点**：传统审阅往往是"Big Bang"式的——代码写完了，堆积如山，然后一次性倾倒给审阅者。
- **Implementer 感受**："交卷前的焦虑"，担心被全盘否定。
- **Reviewer 感受**："面对巨石的无力感"，认知负荷过载。

**体验优化建议**：**流式触发 (Streaming Trigger)**

- **隐喻**：**CI 流水线**。就像我们习惯了 `git push` 后自动跑测试一样，L1 符合性审阅应该在 Task 完成时自动触发。
- **设计**：
  - 当 Implementer 标记任务为 `ReviewNeeded` 时，系统自动派发针对该任务涉及条款的审阅 SubAgent。
  - **反馈粒度**：不是"整个项目通过了吗？"，而是"这个 Task 的 3 个条款通过了吗？"
- **感受**：将"期末考试"变成了"随堂测验"，降低了心理门槛。

#### 二、任务分配体验：SubAgent 的"任务包"

**痛点**：如果我们只给 SubAgent 一个文件名和一句"去审阅吧"，就像把侦探扔进图书馆说"去找线索"。SubAgent 会迷失在无关细节中，消耗大量 Token 却抓不住重点。

**体验优化建议**：**上下文透镜 (Context Lens)**

- **隐喻**：**博物馆导览器**。"请看这幅画（代码），注意它的笔触（条款），不要管画框（无关代码）。"
- **任务包 (Mission Brief) 设计**：
  - **Focus (焦点)**：明确指出要审阅的**代码片段**（不仅仅是文件）。
  - **Lens (透镜)**：**单一条款**。不要让一个 Agent 同时检查"命名规范"和"并发安全"，这会造成注意力涣散。
  - **Persona (人设)**：明确告知"你是无情的法官"（L1）还是"挑剔的艺术家"（L3）。
- **感受**：SubAgent 感到**目标清晰**，不需要猜测意图，可以立即进入"心流"状态。

#### 三、结果呈现体验：Finding 的"示能性"

**痛点**：传统的审阅报告往往是一大段自然语言："我觉得这里可能有点问题..."。
- **Triage 感受**：需要费力解析，不知道严重程度，不知道如何行动。

**体验优化建议**：**证据-判决-行动 (Evidence-Verdict-Action) 三元组**

- **隐喻**：**交通罚单**。
  - **证据**：照片（代码行 + 规范条款）。
  - **判决**：违章代码（Severity: Error/Warning）。
  - **行动**：罚款金额（建议修复方案）。

- **呈现格式 (Markdown Card)**：
  ```markdown
  > 🔴 **Violation: [COMMIT-01]**
  >
  > **证据**:
  > - Spec: "必须在持有写锁时检查 DirtySet"
  > - Code: `src/StateJournal/Commit.cs:42` (无锁访问)
  >
  > **判决**: 竞态条件风险 (High)
  >
  > **建议**:
  > Wrap with `using (_lock.Write()) { ... }`
  ```
- **感受**：Triage 变成了**快速分类游戏**（Accept/Reject），而不是阅读理解题。

#### 四、修复循环体验：多巴胺闭环

**痛点**：修复后，不知道"真的修好了吗？"，等待下一轮大审阅的反馈周期太长。

**体验优化建议**：**即时验证 (Instant Re-verify)**

- **隐喻**：**单元测试的绿灯**。
- **设计**：
  - 提供 `verify_fix(finding_id)` 工具。
  - Implementer 提交修复后，立即运行该工具。
  - 系统只针对该 Finding 相关的条款和代码进行**局部重审**。
- **感受**：**Gamification (游戏化)**。看着红色的 Finding 一个个变绿，产生强烈的多巴胺奖励，驱动参与者完成"清零"目标。

#### 五、总结：用户旅程地图 (User Journey Map)

| 阶段 | 角色 | 行为 | 体验目标 | 关键设计 |
|:---|:---|:---|:---|:---|
| **触发** | Implementer | 提交任务 | "轻松，无压力" | 流式触发，小批次 |
| **分配** | Dispatcher | 生成任务包 | "精准，低噪" | 上下文透镜，单一职责 |
| **执行** | SubAgent | 审阅代码 | "清晰，专注" | 条款驱动，角色明确 |
| **呈现** | Reviewer | 产出报告 | "直观，可行动" | 结构化卡片，EVA 三元组 |
| **决策** | Lead | Triage | "高效，掌控" | 交通灯分级，快速决策 |
| **修复** | Implementer | 修复并验证 | "爽快，成就感" | 即时验证，红变绿 |

**我的核心建议**：
在设计 Recipe 时，请务必引入**结构化输出模板**和**局部验证机制**。这不仅是为了机器好读，更是为了让整个协作过程充满**节奏感**和**确定性**。

期待 GPT 从规范审计的角度为我们把关！

---

### Advisor-GPT 发言

我先“挑刺”：Claude 的三层（L1/L2/L3）是对的方向，但目前**边界不够可判定**，会导致产物混杂、Triage 失真、以及“规范被实现倒灌”（spec creep）。Gemini 的体验提案很强，但若不补上**成本/确定性/去抖**与**证据标准**，会变成高噪音的自动提醒系统。

下面按四个任务逐条收敛。

#### 1) 检验框架：L1/L2/L3 是否穷尽？是否互斥？

- **不互斥（当前定义下）**：
  - L1 的“符合性”会频繁碰到“规范没写清/条款冲突/依赖隐含”的情形；这些既不是简单“违反”，也不是纯粹“完备性”，而是**可判定性缺失**。
  - L3 的“工程性”建议常常会披着“正确性风险”的外衣进入 L1（例如性能导致超时、资源泄漏导致行为改变），造成分层污染。

- **不穷尽（缺一个中间类）**：建议在产物分类上显式引入一个“法律术语”层：
  - **U 类：Underspecified / Ambiguous（规范不可判定）**：审阅者无法在不“发明规则”的情况下给出 L1 verdict。
  - 这类发现不是 L2 的“完备性”（覆盖范围）问题，而是**规范内部的可执行性**问题：定义缺失、术语不闭合、前置依赖未声明、异常/错误语义未钉死。

- **把“层”改成“裁判管辖权”更精确**：
  - **L1 = 合同履约审**（只允许用规范内的可引用证据裁决；不得引入新要求）
  - **L2 = 合同文本修订审**（发现规范缺口/冲突/不可测试之处，提出修订案与最小测试向量）
  - **L3 = 工程优化审**（不改变外部语义前提下的改进建议；必须标注其“非规范强制”属性）

结论：三层作为“解释框架”成立，但作为 Recipe 的“可执行接口”还缺**判定域（U 类）**与**产物字段约束**。

#### 2) 边界情况：什么情况下 L1 审阅会误判？如何防御？

我列 6 类常见误判源（每一类都能在 AI 审阅中放大）：

1. **条款→代码定位误判**：只 grep 命名或文件路径会错过间接实现（helper/extension/partial class）。
  - 防御：L1 任务必须允许“多点证据”：调用链入口 + 关键不变量维护点 + 相关测试。

2. **把“实现形状”当成“规范语义”**：例如规范只要求“失败不改内存态”，审阅者却要求“必须 try/catch 包住某段”。
  - 防御：L1 verdict 只能基于“可观察行为/不变量”，不得基于代码形状偏好。

3. **把测试当作规范（或反过来）**：
  - 测试覆盖不等于条款满足；反之条款满足也可能漏测。
  - 防御：L1 证据链必须同时引用（a）条款原文/术语定义（b）代码实现点（c）可复现实验：现有测试或建议新增最小向量。

4. **未显式声明的条款依赖导致“断章取义”**：子代理只看单条款，忽略其前置定义。
  - 防御：每个 L1 任务包必须带“依赖条款包”（至少术语定义 + 前置不变量）。依赖包本身要版本化。

5. **并发/IO/异常路径的不可见性**：审阅只看 happy path，误判“满足”；或只看到一个异常 throw，误判“违反 Try-pattern”。
  - 防御：L1 检查项必须包含“失败路径清单”与“状态保持性断言”（失败后哪些状态必须不变）。

6. **规范空白被实现“补全”而审阅者误判为正确**（最危险）：实现做了一个合理选择，审阅把它当作规范的一部分，导致规范被实现倒灌。
  - 防御：遇到规范未覆盖的行为，L1 必须输出 U 类（Underspecified）而不是“通过”。是否接受该行为只能由 L2 走修订/决策流程。

#### 3) 操作化条款：把共识收敛为 MUST/SHOULD/MAY（带编号）

下面是我建议直接写进 Recipe 的“可审计契约”。（编号可后续统一命名空间；我先用 `CR-*`。）

**范围与权威源（SSOT）**

- CR-01 **MUST** 明确 L1 的权威输入集合（Normative Sources）：哪些规范文件/条款版本、哪些测试向量、哪些错误/结果规范算“合同正文”。
- CR-02 **MUST** 声明“禁止实现倒灌”：L1 不得因为实现存在某行为就推定规范允许；此类必须标为 U 类或进入 L2。
- CR-03 **SHOULD** 为每个条款维护显式依赖（术语定义/前置不变量/相关条款），供任务包自动打包。

**Finding 的结构化输出（EVA+裁决域）**

- CR-04 **MUST** 每个 Finding 采用结构化字段：
  - `ClauseId`（或条款引用）
  - `Evidence`（规范引用 + 代码定位 + 运行/测试证据）
  - `VerdictType` ∈ {`V` Violation, `U` Underspecified/Ambiguous, `C` Conform, `I` Improvement}
  - `Severity`（仅对 V/I；U 走“阻断判定”而非严重度）
  - `ProposedAction`（修复/补条款/补测试/需要裁决）
- CR-05 **MUST** 对 `U` 类给出“最小澄清问题”（Yes/No 形式优先）与“最小测试向量/示例”，避免空泛讨论。
- CR-06 **SHOULD** 允许一个条款对应多个实现点，但必须在 Finding 里枚举，否则不可复核。

**任务分解与成本控制（防噪音）**

- CR-07 **MUST** 每个 SubAgent 任务有明确边界：最多 N 个条款或一个模块；必须提供“定位入口”（文件/符号/测试名）以避免盲搜。
- CR-08 **MUST** 流式触发必须有去抖/合并策略：同一变更集只触发一次 L1；重复 Finding 必须去重（以 `ClauseId+Location+Signature` 作为主键）。
- CR-09 **SHOULD** 增量审阅优先：仅对变更触及的条款/模块运行；定期（夜间/里程碑）再跑全量。
- CR-10 **MAY** 引入“L0 Gate”：格式化、编译、测试通过作为审阅前置，以减少 L1 噪音与误判。

**Triage 与裁决权（防止永远吵不完）**

- CR-11 **MUST** 规定裁决权：L1 只能给出 `V/U/C`；是否接受 `U` 的实现行为、是否修订规范，必须由 L2 的“决策记录（Decision Log）”闭环。
- CR-12 **MUST** 对每个 `V` 类 Finding 提供“可复现验证步骤”（已有测试/命令或建议新增的最小测试）。无法复现的 `V` 默认降级为 `U` 或 `I`。
- CR-13 **SHOULD** 引入“上诉/复核”步骤：被审方可以要求指出“规范依据 + 可观察失败”，否则 Finding 不得进入 P0。

**工程性建议的边界（避免 L3 污染 L1）**

- CR-14 **MUST** L3 建议不得使用“违反条款”的措辞；只能以“风险/收益/替代方案”表达，并声明是否会改变对外语义。
- CR-15 **SHOULD** 对 L3 建议给出测量方式（性能基准、内存曲线、复杂度），否则只作为意见，不进入强制改动队列。

#### 4) 风险提示：方法论的已知局限

- **规范本身可能错**：L1 的“客观性”建立在规范正确且自洽的前提上；当规范错误时，L1 会系统性放大错误。
- **AI 的“证据漂移”**：没有强制证据字段时，模型会用合理化叙述填空；必须用结构化输出与复现步骤约束。
- **U 类会堆积**：如果 L2 修订吞吐不足，U 类会成为永久债务，最终让 L1 失去判定力。
- **非功能性需求难以条款化**：性能、可维护性、可观测性等往往只能在 L3 给“条件化建议”，不应伪装成 L1。
- **增量审阅的盲区**：只审变更会漏掉“依赖被动变化”（例如公共 helper 的语义改变影响未改动模块）。需要周期性全量审阅兜底。

我建议主持人把 Recipe 的关键目标写成一句硬话：

> **审阅不是“多看代码”，而是“在可判定证据链上做裁决”，并把不可判定的部分强制升级为规范修订的工作项。**
---

### 主持人第一轮总结 (刘德智)

感谢三位 Advisor 的精彩发言！让我整理一下第一轮的共识与分歧。

#### 已达成共识

| # | 共识 | 来源 |
|:--|:-----|:-----|
| 1 | **审阅本质是"验证翻译"**——检验 Spec→Code 映射的保真度，而非逆向理解代码意图 | Claude |
| 2 | **L1 符合性审阅是核心**——条款驱动、可并行、有客观答案 | Claude |
| 3 | **增加 U 类（Underspecified）**——规范不可判定时，必须显式标记而非勉强裁决 | GPT |
| 4 | **结构化输出格式**——EVA 三元组（Evidence-Verdict-Action）+ VerdictType 字段 | Gemini + GPT |
| 5 | **流式触发 + 去抖**——避免大爆炸审阅，同时防止噪音 | Gemini + GPT |
| 6 | **即时验证机制**——修复后可局部重审，形成多巴胺闭环 | Gemini |
| 7 | **禁止实现倒灌**（CR-02）——L1 不得因实现存在某行为就推定规范允许 | GPT |

#### 仍需讨论

| # | 问题 | 关注点 |
|:--|:-----|:-------|
| 1 | **任务粒度**：每个 SubAgent 应该审阅多少条款？单条款 vs 条款组？ | Token 经济性 vs 上下文完整性 |
| 2 | **依赖包机制**：如何自动识别条款依赖？现有规范没有显式依赖图 | CR-03 的可操作性 |
| 3 | **L0 Gate 是否必须**：格式化/编译/测试通过作为审阅前置？ | 成本 vs 噪音 |
| 4 | **StateJournal MVP 首次审阅**：是全量审阅还是选样审阅？ | 首次验证 Recipe 的策略 |

#### 第二轮焦点

我想请各位在第二轮聚焦于：**如何把这套方法论落地为可执行的 Recipe？**

具体问题：
1. **Recipe 结构**：应该包含哪些章节？（类似 jam-session-guide.md 的结构）
2. **任务包模板**：给 SubAgent 的 prompt 应该长什么样？
3. **Finding 文件格式**：结构化输出的 YAML/Markdown schema 是什么？
4. **StateJournal 首审计划**：如何把 43 条条款分组？先审哪些？

请继续畅所欲言！

---

### Advisor-Claude 第二轮发言

各位好！刘德智请我聚焦于 Recipe 落地，我来交付两个具体产出：**Recipe 结构设计**和**任务包模板**。

#### 一、Recipe 结构设计

参考 `jam-session-guide.md` 和 `strategic-tactical-dual-session.md` 的结构，我建议代码审阅 Recipe 采用以下章节：

```markdown
# 规范驱动代码审阅配方 (Spec-Driven Code Review Recipe)

## 1. 概述
   - 什么是规范驱动审阅（"验证翻译"而非"逆向理解"）
   - 适用场景（有规范文档的代码库）
   - 核心产出（FindingList + 修订提案）

## 2. 三层审阅模型
   - L1 符合性审阅（条款→代码映射）
   - L2 完备性审阅（规范盲区识别）
   - L3 工程性审阅（质量改进建议）
   - 裁决类型（V/U/C/I）定义

## 3. 权威源定义 (Normative Sources)
   - 规范文件列表（版本锁定）
   - 条款依赖图（或生成规则）
   - 禁止实现倒灌原则

## 4. 任务分解策略
   4.1 分组原则
       - 按模块（目录结构）
       - 按条款域（功能域）
       - 依赖包打包规则
   4.2 粒度指南
       - 单 SubAgent 最多 N 条款 / 1 模块
       - 上下文预算（token 估算）
   4.3 L0 Gate（可选）
       - 编译通过、测试通过、格式化通过

## 5. 任务包模板 (Mission Brief Template)
   - YAML schema 定义
   - 字段说明
   - 示例

## 6. Finding 输出格式
   - 结构化字段（EVA + VerdictType）
   - Markdown 渲染模板
   - 去重主键

## 7. 执行流程
   7.1 Phase 1: 并行审阅
   7.2 Phase 2: Triage（合并 + 分级）
   7.3 Phase 3: 修复
   7.4 Phase 4: 验证（局部重审）

## 8. 闭环机制
   - Finding 状态机
   - 即时验证工具
   - U 类升级流程（→ L2 修订）

## 9. 成本控制
   - 增量审阅（变更触发）
   - 全量审阅（里程碑触发）
   - 去抖策略

## 10. 附录
   - CR 条款速查表（GPT 提出的 CR-01 ~ CR-15）
   - 常见误判清单
   - 首审计划模板
```

**设计说明**：

| 章节 | 对应共识 | 核心内容 |
|:-----|:---------|:---------|
| §2-3 | Claude 三层模型 + GPT CR-01/02 | 建立概念框架和权威边界 |
| §4-5 | Gemini 上下文透镜 + GPT CR-07 | 任务分解的可操作指南 |
| §6 | Gemini EVA + GPT CR-04/05 | 统一输出格式 |
| §7-8 | 全员共识 | 完整生命周期覆盖 |

#### 二、任务包模板（Mission Brief Template）

这是 SubAgent 收到审阅任务时的"上下文透镜"——让它精准聚焦，避免盲目搜索。

##### 2.1 YAML Schema

```yaml
# Mission Brief for L1 Code Review
# Version: 1.0

meta:
  briefId: "L1-{module}-{date}-{seq}"      # 唯一标识
  reviewType: "L1"                          # L1 | L2 | L3
  createdBy: "strategic-session"
  createdAt: "2025-12-26T10:00:00Z"

# === 焦点定义 ===
focus:
  module: "Workspace"                       # 模块名
  clauses:                                  # 要审阅的条款列表
    - id: "[WS-IDENTITY-01]"
      title: "IdentityMap 单例保证"
      specFile: "mvp-design-v2.md"
      specSection: "§4.3.1"
    - id: "[WS-DIRTY-02]"
      title: "DirtySet 脏对象追踪"
      specFile: "mvp-design-v2.md"
      specSection: "§4.3.2"

# === 代码定位入口 ===
codeLocations:
  entryPoints:                              # 入口文件/符号
    - "src/StateJournal/Workspace/IdentityMap.cs"
    - "src/StateJournal/Workspace/DirtySet.cs"
  relatedTests:                             # 相关测试（供验证参考）
    - "tests/StateJournal.Tests/Workspace/IdentityMapTests.cs"
  excludePatterns:                          # 排除（减少噪音）
    - "**/obj/**"
    - "**/bin/**"

# === 依赖上下文包 ===
dependencyContext:
  requiredClauses:                          # 前置条款（必须理解）
    - id: "[CORE-PTR64-01]"
      summary: "Ptr64 是对象地址的 8 字节表示"
    - id: "[CORE-IDURABLE-01]"
      summary: "IDurableObject 是所有持久对象的基接口"
  requiredTerms:                            # 术语定义引用
    specFile: "mvp-design-v2.md"
    section: "§2 术语表"

# === 审阅指令 ===
instructions:
  persona: "L1-Judge"                       # 角色：L1-Judge | L2-Auditor | L3-Advisor
  
  mustDo:
    - "逐条款检查代码是否满足规范语义"
    - "每个 Finding 必须引用条款原文和代码位置"
    - "遇到规范未覆盖的行为，标记为 U 类（Underspecified）"
    - "禁止实现倒灌：不得因实现存在某行为就推定规范允许"
  
  mustNot:
    - "不要评论代码风格（那是 L3）"
    - "不要假设规范未写的约束"
    - "不要产出无法复现的 Finding"

# === 输出格式 ===
outputFormat:
  file: "findings/L1-Workspace-2025-12-26.md"
  schema: "EVA-v1"                          # 使用 EVA 三元组格式
  requiredFields:
    - clauseId
    - verdictType      # V | U | C | I
    - evidence
    - proposedAction
```

##### 2.2 Markdown 简化版（适合直接嵌入 Prompt）

```markdown
# 审阅任务：Workspace 模块 L1 符合性审阅

## 🎯 焦点

**模块**：`src/StateJournal/Workspace/`

**条款清单**：
| ID | 标题 | 规范位置 |
|:---|:-----|:---------|
| `[WS-IDENTITY-01]` | IdentityMap 单例保证 | mvp-design-v2.md §4.3.1 |
| `[WS-DIRTY-02]` | DirtySet 脏对象追踪 | mvp-design-v2.md §4.3.2 |

## 🔍 代码入口

- `src/StateJournal/Workspace/IdentityMap.cs`
- `src/StateJournal/Workspace/DirtySet.cs`
- 相关测试：`tests/.../Workspace/*Tests.cs`

## 📚 依赖上下文

**前置条款**（你需要理解这些才能正确审阅）：
- `[CORE-PTR64-01]`：Ptr64 是对象地址的 8 字节表示
- `[CORE-IDURABLE-01]`：IDurableObject 是所有持久对象的基接口

**术语定义**：参见 mvp-design-v2.md §2

## 📋 审阅指令

**你的角色**：L1 符合性法官（只判断合同履约，不发明新规则）

**MUST**：
- 逐条款检查代码是否满足规范语义
- 每个 Finding 必须引用条款原文 + 代码位置 + 复现方式
- 遇到规范未覆盖的行为 → 标记为 `U`（Underspecified）

**MUST NOT**：
- 评论代码风格（那是 L3 的事）
- 假设规范未写的约束
- 产出无法复现的 Finding

## 📤 输出格式

每个 Finding 使用以下结构：

```markdown
> **[VerdictType] ClauseId**
>
> **证据**:
> - **规范**: "条款原文引用"
> - **代码**: `文件:行号` — 相关代码片段
> - **复现**: 测试名 或 "建议新增测试: ..."
>
> **判决**: 问题描述 (Severity: High/Medium/Low)
>
> **建议**: 修复方案
```

VerdictType 取值：
- `🔴 V` = Violation（违反条款）
- `🟡 U` = Underspecified（规范不可判定）
- `🟢 C` = Conform（符合）
- `🔵 I` = Improvement（改进建议，仅 L3 使用）
```

##### 2.3 任务包设计要点

| 字段域 | 设计原理 | 来源 |
|:-------|:---------|:-----|
| **focus** | 单一职责：一个任务只审一个模块/条款组 | Gemini 上下文透镜 |
| **codeLocations** | 精准定位：避免 SubAgent 盲目 grep | GPT CR-07 |
| **dependencyContext** | 防止断章取义：提供前置条款和术语 | Claude 依赖包 + GPT CR-03 |
| **instructions.persona** | 角色明确：L1 是法官，不是顾问 | Gemini 人设建议 |
| **instructions.mustNot** | 防御误判：禁止风格评论、禁止实现倒灌 | GPT CR-02/CR-14 |
| **outputFormat** | 结构化输出：确保 Triage 可机器辅助 | Gemini EVA + GPT CR-04 |

#### 三、下一步建议

1. **验证任务包模板**：用 StateJournal 的一个模块（如 Core）做试点
2. **生成条款依赖图**：从 mvp-design-v2.md 提取条款依赖（可半自动化）
3. **确定分组策略**：43 条条款 → 按模块分为 4-6 组

**分组草案**（基于代码结构）：

| 组 | 模块 | 条款数（估） | 上下文依赖 |
|:---|:-----|:-------------|:-----------|
| G1 | Core（Ptr64, VarInt, Error） | ~8 | 无 |
| G2 | Objects（DurableDict, ValueType） | ~10 | G1 |
| G3 | Workspace（IdentityMap, DirtySet, LazyRef） | ~12 | G1 |
| G4 | Commit（CommitContext, VersionIndex, Recovery） | ~13 | G1, G3 |

G1 可独立审阅；G2/G3 可并行；G4 依赖 G1+G3。

#### 四、不确定性标注

- **条款依赖图的完整性**（❓需验证）：现有规范没有显式依赖标注，需人工或半自动提取
- **任务包 token 预算**（❓需试验）：建议先做一个模块，测量实际消耗
- **L2/L3 任务包差异**（❓待设计）：本次聚焦 L1，L2/L3 的任务包模板可后续迭代

---

期待 Gemini 从 DX 角度打磨任务包模板，GPT 从条款完备性角度检验 Recipe 结构！

---

### Advisor-Gemini 第二轮发言

收到。Claude 已经搭建了 Recipe 的骨架和任务包（输入端），我来负责设计**产出端（Finding Format）**和**落地执行（StateJournal Plan）**。

我的设计哲学依然是：**让机器好读（结构化），让人类好懂（示能性）。**

#### 一、Finding 文件格式设计：The "Traffic Ticket" Schema

我们需要一种既能被 Triage Agent 轻松解析（JSON/YAML），又能被人类 Lead 快速阅读（Markdown）的格式。

我建议采用 **"YAML Frontmatter + Markdown Body"** 的混合格式。每个 Finding 作为一个独立的文件（或在一个大文件中以 `---` 分隔），就像一张张独立的"交通罚单"。

##### 1.1 Schema 定义 (EVA-v1)

```yaml
# Finding Schema Definition
type: object
properties:
  id: { type: string, pattern: "F-{ClauseID}-{Hash}" }
  verdict: { enum: ["Violation", "Underspecified", "Conform", "Improvement"] }
  severity: { enum: ["Critical", "Major", "Minor", "Nit"] }
  clause:
    id: { type: string }
    text: { type: string }
  evidence:
    spec_quote: { type: string }
    code_loc: { type: string, format: "file:line" }
    snippet: { type: string }
  action: { type: string }
```

##### 1.2 实际输出示例 (Markdown)

SubAgent 应该输出如下格式的 Markdown 文件（例如 `findings/F-COMMIT-01-a1b2.md`）：

```markdown
---
id: "F-COMMIT-01-a1b2"
verdict: "Violation"
severity: "Critical"
clause_id: "[COMMIT-01]"
tags: ["concurrency", "safety"]
---

# 🔴 Violation: [COMMIT-01] 提交时未检查 DirtySet 锁

## 📝 Evidence (证据)

**1. 规范条款**
> "在构建 CommitContext 之前，必须获取 DirtySet 的**写锁**，以防止在快照生成期间产生新的脏对象。" (`mvp-design-v2.md` §5.2)

**2. 代码实现**
位置: [`src/StateJournal/Commit/CommitContext.cs:42`](../../src/StateJournal/Commit/CommitContext.cs#L42)

```csharp
public CommitContext Create(Workspace ws) {
    // ❌ Violation: 直接访问 DirtySet 而没有 using (ws.Lock.Write())
    var dirty = ws.DirtySet.GetAll(); 
    return new CommitContext(dirty);
}
```

**3. 复现/验证**
此路径在多线程高压测试下会导致 `DirtySet` 集合修改异常。
建议运行测试: `StateJournal.Tests.Concurrency.CommitRaceTests` (需新增)

## ⚖️ Verdict (判决)

**判定**: **Violation (Critical)**
代码违反了显式的锁契约。虽然在单线程测试中能通过，但在并发环境下会破坏原子性快照的不变量。

## 🛠️ Action (行动)

**建议修复**:
在访问 `DirtySet` 前包裹写锁：

```csharp
using (ws.Lock.Write()) {
    var dirty = ws.DirtySet.GetAll();
    return new CommitContext(dirty);
}
```
```

**设计亮点**：
- **头部 YAML**: 供 Triage Agent 快速索引、去重、统计。
- **红绿灯标题**: `🔴 Violation`, `🟡 Underspecified`, `🟢 Conform`，人类一眼识别。
- **证据三段论**: 规范 vs 代码 vs 复现，逻辑闭环。

#### 二、StateJournal 首次审阅计划：The "Vertical Slice" Pilot

StateJournal 有 43 条条款，如果一次性全量审阅，容易因为 Recipe 不成熟导致大量噪音（False Positives）。

我建议采用 **"T型审阅策略"**：
- **横向（宽）**：覆盖一个基础模块（Core），验证流程跑通。
- **纵向（深）**：刺穿一个复杂功能（Commit），验证审阅深度。

##### 2.1 阶段一：校准 (Calibration Run)
*目标：调试 Prompt，验证 Finding 格式，确立 L1 尺度。*

- **范围**: `Core` 模块 (8 条条款)
  - `[CORE-PTR64-*]`, `[CORE-VARINT-*]`, `[CORE-ERROR-*]`
- **原因**: 
  - 依赖最少（几乎无依赖）。
  - 逻辑简单（位运算、序列化），容易判断对错。
  - **如果基础类型（Ptr64）错了，上面全错**，收益极高。
- **预期产出**: ~3-5 个 Findings，主要可能是边界检查遗漏。

##### 2.2 阶段二：深潜 (Deep Dive Run)
*目标：验证上下文依赖处理，挑战 L1/L2 边界。*

- **范围**: `Commit` 模块中的 **"原子性"相关条款** (仅 3 条)
  - `[COMMIT-01]`, `[COMMIT-02]`, `[RECOVERY-01]`
- **原因**: 
  - **高风险**：这是最容易出并发 Bug 的地方。
  - **高依赖**：依赖 `Workspace` 的锁机制和 `Objects` 的持久化格式。
  - 这是检验 Claude 提出的 "依赖上下文包" 机制是否有效的试金石。

##### 2.3 阶段三：全量推广 (Full Rollout)
*只有在阶段一、二的 "信噪比" 达标（>80% 有效 Finding）后，才启动全量。*

- **范围**: 剩余的 `Objects` (10) 和 `Workspace` (12) 条款。
- **执行方式**: 批量并行触发 SubAgents。

#### 三、我的行动建议 (Next Steps)

为了落地这个计划，我建议立即执行以下操作：

1. **生成器**: 编写一个简单的脚本 `scripts/generate-review-brief.js`，根据 YAML 模板生成 Markdown 任务包。
2. **试点**: 手动组装 `Core` 模块的任务包，作为 **One-Shot** 喂给 SubAgent，看看它吐出的 Finding 是否符合 EVA-v1 格式。
3. **文档**: 将上述格式和计划写入 `agent-team/recipe/code-review-recipe-v1.md`。

**决策请求**：
@TeamLeader，是否同意先对 `Core` 模块进行 "校准审阅"？如果同意，我将协助 Claude 准备第一个任务包。


### Advisor-GPT 第二轮发言

本轮我只谈 **验收与质量控制（QC）**：如何判定 Recipe/模板“合格可用”，以及如何防 drift、控噪音、可复核。

#### 1) Review Claude 和 Gemini 的设计：对照条款是否闭环？

- Claude 的 Recipe 结构与 Mission Brief 模板，已经把“权威源/分层/流程/成本控制”大体搭齐，方向正确。
- Gemini 的 Finding（EVA-v1）与首审计划（T 型：Core 校准 + Commit 深潜）很接近可落地，但目前 schema/字段约束与 Claude 模板、以及我第一轮 CR 条款之间存在数个**会导致验收失败**的不一致，需要先修。

#### 2) FixList（需要修改/补齐的点）

- **[Fix-01] VerdictType 枚举不统一（短枚举 vs 长枚举）**
  - 现状：Claude/CR 用 `V/U/C/I`；Gemini 用 `Violation/Underspecified/Conform/Improvement`。
  - 风险：Triage 统计/策略分派/去重会分叉。
  - 建议：机器字段统一为 `verdictType: V|U|C|I`；标题/渲染层可保留人类友好文案（含红绿灯）。

- **[Fix-02] U 类不应携带 severity**
  - 现状：Gemini schema 把 `severity` 放在顶层；容易让 U 混入 bug queue。
  - 建议：schema 规定 `severity` 仅对 `V`（以及可选 `I`）存在；`U` 必须给出 `clarifyingQuestions`（最小 yes/no）+ `specChangeProposal`（最小修订案）。

- **[Fix-03] Evidence Triad 必须把“可复现验证步骤”升级为必填字段**
  - 现状：示例里提“建议新增测试”，但 schema/任务包未强制。
  - 建议：Finding frontmatter 增加必填：`repro`（existingTest/newTest/manual）与 `howToVerifyFix`（一句话即可，可引用测试/命令/断言）。
  - 验收规则：任何 `V` 若缺 `repro/howToVerifyFix`，默认降级为 `U` 或拒收（不进入 P0）。

- **[Fix-04] 去重主键（dedupeKey）缺失，流式触发会刷屏**
  - 现状：Gemini 的 `id: F-{ClauseID}-{Hash}` 若 hash 不可重算，会导致跨轮次无法稳定去重。
  - 建议：定义可重算的 `dedupeKey = clauseId + normalizedCodeLocation + verdictType + shortSignature`，并在示例中体现。

- **[Fix-05] Normative Sources 需要“版本锁定机制”，不是一句话**
  - 现状：Claude 写了“版本锁定”，但没规定具体做法。
  - 建议：每次审阅必须记录 `specRef`（git commit SHA/tag/版本号），并写入 Mission Brief 与 Finding frontmatter，保证复核不漂移。

- **[Fix-06] Finding schema 避免双写条款原文（SSOT 漂移风险）**
  - 现状：Gemini schema 有 `clause.text`，示例又有 spec quote。
  - 建议：Finding 只存 `clauseId + specRef + specQuote(可选短引)`，并要求能回链到规范 SSOT（文件+章节/锚点）。

#### 3) Recipe 发布前“完成度检查清单”（checklist）

**A. 权威源与边界（SSOT）**
- [ ] 明确 `Normative Sources`（哪些文档/章节/接口契约算 L1 证据）
- [ ] 每次审阅记录 `specRef`（commit SHA/tag/版本号），并写入所有 Mission Brief 与 Findings
- [ ] 写明“禁止实现倒灌”：L1 不得推定规范未写的允许/禁止

**B. Schema 与可解析性（Mission Brief / Finding）**
- [ ] Mission Brief schema 定稿（字段、必填项、示例齐全）
- [ ] Finding schema 定稿（字段、必填项、示例齐全）
- [ ] `verdictType` 统一为 `V|U|C|I`，并定义字段合法组合（例如 `U` 不允许 `severity`）
- [ ] Evidence Triad 强制：`specRef+specQuote`、`codeLocation`、`repro/howToVerifyFix`
- [ ] 定义并落地 `dedupeKey`（可重算、跨运行稳定）

**C. 闭环（Triage/Fix/Verify）**
- [ ] Finding 状态机定义（New → Triaged → InFix → NeedsVerify → Closed/Reopened）
- [ ] U 类升级流程定义（澄清问题 + 最小修订案 + 决策记录/待决队列）
- [ ] 局部重审（verify-fix）输入与触发条件定义（FindingId/dedupeKey + 相关条款包）

**D. 成本与噪音控制**
- [ ] 去抖/合并策略（同一变更集只触发一次；重复 Finding 自动合并）
- [ ] 增量审阅 vs 里程碑全量审阅的触发规则
- [ ] 成本记录（条款数、SubAgent 数、耗时、Finding 数、有效率）

**E. 最小可用演示（Pilot）**
- [ ] Core 校准跑通：产出可解析、可去重、可复核
- [ ] Commit 深潜跑通：依赖上下文包生效，能区分 `V` vs `U`
- [ ] 至少 1 个 `V` 完成“修复→局部重审→关闭”的闭环

#### 4) StateJournal 首审“Recipe 可用”的可度量成功标准

- **Parseability**：100% Finding 通过 schema 校验；0 个缺失必填证据字段。
- **Signal-to-Noise**：Triage 后 ≥80% Findings 为“有效”（进入修复或进入规范修订/裁决）；≤20% 为误报/重复/不可复核。
- **Coverage**：Core（约 8 条）+ Commit 深潜（约 3 条）中，100% 目标条款都有 verdict（C/V/U）与最小证据链。
- **Closure**：至少完成 1 个 `V(Critical/Major)` 的“修复→验证→关闭”；至少完成 1 个 `U` 的“澄清→修订案→决策记录/待决队列”。
- **Governance**：`U` 不被当作 bug queue；每个 U 都有明确 owner 与下一步（修订/接受/补测试）。

---

### 主持人收尾总结 (刘德智)

感谢三位 Advisor 两轮精彩的讨论！这次畅谈会的产出远超预期。让我做最终的收敛和行动计划。

## 📋 第二轮新增共识

| # | 共识 | 来源 | 对应条款 |
|:--|:-----|:-----|:---------|
| 8 | **Recipe 10 章节结构**：从概述到附录的完整框架 | Claude | — |
| 9 | **Mission Brief 模板**：YAML schema + Markdown 简化版 | Claude | CR-07 |
| 10 | **Finding 格式 EVA-v1**：YAML Frontmatter + Markdown Body | Gemini | CR-04 |
| 11 | **T 型首审策略**：Core 校准 + Commit 深潜 + 全量推广 | Gemini | — |
| 12 | **6 项 FixList**：统一 VerdictType、U 类无 severity、repro 必填等 | GPT | CR-04/05 |
| 13 | **发布前 Checklist**：5 大类 16 项检查 | GPT | 全覆盖 |
| 14 | **可度量成功标准**：Parseability、SNR≥80%、Coverage、Closure | GPT | — |

## ✅ 最终决议

### 1. Recipe 命名与定位

**名称**：`spec-driven-code-review.md`（规范驱动代码审阅配方）

**定位**：可复用的方法论配方，适用于任何有规范文档的代码库

### 2. 关键设计决策

| 决策 | 结果 | 理由 |
|:-----|:-----|:-----|
| VerdictType 枚举 | `V\|U\|C\|I`（短枚举） | 机器友好、统一 |
| U 类 severity | 禁止 | U 是"不可判定"，不是 bug |
| repro 字段 | 必填 | 无法复现的 V 降级为 U |
| 首审策略 | T 型（Core→Commit→全量） | 渐进验证，降低噪音 |

### 3. 待修复项（FixList）

| # | 问题 | 修复方案 | 状态 |
|:--|:-----|:---------|:-----|
| Fix-01 | VerdictType 不统一 | 统一为 `V\|U\|C\|I` | 🔜 |
| Fix-02 | U 类不应有 severity | schema 约束 | 🔜 |
| Fix-03 | repro 必填 | Mission Brief + Finding schema | 🔜 |
| Fix-04 | dedupeKey 缺失 | 定义可重算公式 | 🔜 |
| Fix-05 | specRef 版本锁定 | 写入 Mission Brief | 🔜 |
| Fix-06 | 条款原文双写 | 只存 clauseId + 短引 | 🔜 |

## 🎯 后续行动

### Phase 0: Recipe 文档化（今日）

1. **创建 Recipe 文件**：`agent-team/recipe/spec-driven-code-review.md`
2. **合并 FixList**：应用 GPT 的 6 项修复
3. **整合模板**：Mission Brief + Finding schema

### Phase 1: Core 校准审阅（本周）

1. **准备任务包**：Core 模块（~8 条款）的 Mission Brief
2. **执行审阅**：派发给 CodexReviewer
3. **验收**：检查 Finding 格式、SNR、可复核性

### Phase 2: Commit 深潜（视 Phase 1 结果）

1. **准备任务包**：Commit 原子性条款（~3 条）
2. **测试依赖包**：验证条款依赖机制
3. **执行审阅**：派发给 CodexReviewer
4. **验收**：检查 L1/U 边界判定

### Phase 3: 全量推广（视 Phase 2 结果）

- Objects + Workspace 剩余条款
- 并行执行 + 去重合并

## 📚 会议产出清单

| 产出 | 位置 | 状态 |
|:-----|:-----|:-----|
| 畅谈会记录 | 本文件 | ✅ 完成 |
| Recipe 文档 | `recipe/spec-driven-code-review.md` | 🔜 待创建 |
| Mission Brief 模板 | Recipe 内 | 🔜 待整合 |
| Finding Schema EVA-v1 | Recipe 内 | 🔜 待整合 |
| CR 条款速查表 | Recipe 附录 | 🔜 待整合 |
| StateJournal 首审计划 | `StateJournal/review-plan.md` | 🔜 待创建 |

## 🎤 闭幕词

> **"审阅不是'多看代码'，而是'在可判定证据链上做裁决'，并把不可判定的部分强制升级为规范修订的工作项。"** — Advisor-GPT

这次畅谈会验证了参谋组的协作模式：
- **Claude** 建立概念框架（"验证翻译"模型）
- **Gemini** 注入体验视角（EVA 三元组、T 型策略）
- **GPT** 收敛严谨性（15 条 CR 条款、6 项 FixList、成功标准）

这套方法论如果验证成功，将成为 AI Team 的核心能力之一——**自主审阅规范驱动的实现**。

**会议状态**：✅ 已完成

El Psy Kongroo.

---
