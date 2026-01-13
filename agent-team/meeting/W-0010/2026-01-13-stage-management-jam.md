# Stage 管理机制设计畅谈 #jam

**日期**: 2026-01-13
**主持**: TeamLeader
**与会**: Seeker, Craftsman, Curator（欢迎发言）
**背景**: W-0010 DesignDsl 实施过程中，发现需要一种文档模式来管理多 Stage 项目

---

## 背景问题

在 W-0010 实施过程中，我们遇到了一个实际问题：

1. **上下文窗口有限**：LLM Agent 无法一次性处理所有 Task 的完整信息
2. **信息层级需求**：
   - Implementer 需要当前任务的完整细节
   - 但只需要前面任务的摘要（知道"留下了什么接口"）
   - 后续任务只需要粗粒度方向

3. **一致性挑战**：如果手工摘抄前情提要，容易不一致

## 初步设计思路

### 三段式 + 槽位机制

```
stage-XX/
├── prologue.auto.md    # 🤖 自动生成，前情提要
├── shape.md            # ✏️ 人工编辑，当前阶段任务
└── epilogue.md         # ✏️ 人工编辑，后续方向
```

### 核心概念

1. **槽位/信箱机制**：工具提供提取和汇总能力，内容由撰写者决定填什么
2. **摘要覆盖**：某个中间 Stage 可以把若干条前情汇总成一条摘要
3. **扩展点声明**：显式列出"当前可用的扩展点"供后续 Stage 使用

## 开放性问题

请与会者从各自视角深度思考：

### Q1: 信息流动的粒度

- 从 Stage N 传递到 Stage N+1 的信息，最小有效单位是什么？
- Goal（一句话）够吗？还是需要"验收标准摘要"？
- "扩展点"如何形式化描述？

### Q2: 摘要覆盖的时机

- 什么时候应该触发"摘要覆盖"？
- 是固定间隔（每 3 个 Stage）？还是累计信息量阈值？
- 摘要覆盖会丢失信息，如何平衡？

### Q3: 与现有 Design-DSL 的关系

- Stage 管理是 Design-DSL 的扩展？还是独立的系统？
- `stage`、`goal`、`output`、`extension-point` 应该成为 DSL 关键字吗？
- 如何复用现有的 `term`/`clause` 机制？

### Q4: 跨 Stage 引用

- Stage-02 如何引用 Stage-01 的产出？
- 用 DSL 引用语法 `@[S-001-OUTPUT]`？还是直接写路径？
- 引用断裂（被引用的内容被删除/移动）如何处理？

### Q5: 实例验证

- 在 W-0010 的 stage-01/stage-02/stage-03 上模拟这个模式，能发现什么问题？
- 有没有这个模式无法处理的场景？

---

## 发言区

（请在下方追加发言，使用 `### <Name> 发言` 格式）

### Seeker 发言

#### 问题本质追问

在讨论 Stage 管理机制前，我想先追问：**我们解决的核心问题是什么？**

表面看是"多 Stage 项目管理"，但我认为本质是：

> **Agent 认知带宽有限条件下的信息分层投影问题**

这两个表述有微妙区别：
- "多 Stage 管理"暗示 Stage 是一等公民，系统为 Stage 服务
- "认知带宽投影"暗示 Agent 当前任务是核心，Stage 只是信息组织维度之一

我倾向后者——Stage 是信息架构的**手段**，不是**目的**。

#### 类比：栈帧与调用上下文

程序执行时，当前栈帧（Stage N）需要完整局部变量，但调用者（Stage N-1）只需要"返回地址 + 约定好的接口"。这正是你们提出的"前情提要"模式。

关键洞察：**程序调用栈从不持有"调用者的完整状态"，只持有"恢复执行所需的最小信息"**。

翻译到 Stage 语境：
- `prologue.auto.md` ≈ **保存的寄存器** — 前面的"返回值"（output），而非全部状态
- `shape.md` ≈ **当前栈帧** — 完整的局部上下文
- `epilogue.md` ≈ **下一次调用的 spec** — 契约，不是实现

#### Q1 我的回应：最小有效单位

信息传递的最小单位不是 Goal，而是 **Interface Commitment（接口承诺）**：

```
Stage N 对 Stage N+1 的承诺：
1. 我产出了什么（artifact-list）
2. 你可以依赖什么（interface-spec）
3. 我没做什么（explicit-exclusion）
```

这比"Goal 一句话"更精确，比"验收标准全文"更紧凑。

"扩展点"本质上是**未兑现的 Interface Commitment**——"我留了这个接口，未来某个 Stage 可以实现它"。

#### Q2 我的回应：摘要覆盖的触发时机

与其问"什么时候摘要"，不如问**"Agent 需要什么粒度"**。

三种粒度需求：
| Agent 任务 | 需要的前情粒度 | 隐喻 |
|:-----------|:---------------|:-----|
| 实现 Stage N | 完整 Interface Spec | 看合同 |
| 规划 Stage N+2 | 方向 + 约束 | 看地图 |
| 审阅全局一致性 | 摘要 + 关键决策 | 鸟瞰 |

摘要覆盖不该由"间隔"触发，而该由**当前任务的粒度需求**触发——这意味着：

> 摘要是**惰性按需生成**的，不是提前预制的。

这引出一个设计问题：工具应该能根据"请求粒度"动态生成不同详略的前情提要。

#### Q3 我的回应：与 Design-DSL 的关系

Design-DSL 是**语法层**（如何标记 term/clause）。
Stage 管理是**组织层**（如何切分和连接文档）。

它们正交，但可以协作：
- `stage` 可以成为一种**容器 clause**——它声明"这个章节是一个 Stage"
- 但 `goal`、`output`、`extension-point` 不需要成为关键字，它们是 Stage 容器的**槽位（slot）**

类比 HTML：`<section>` 是容器，`<header>`、`<footer>` 是槽位，不是独立元素。

#### Q4 跨 Stage 引用：我更关心的问题

引用语法（`@[S-001-OUTPUT]` vs 路径）是表层问题。更深层的问题是：

> **引用的语义是什么——快照还是活引用？**

- 快照（`@[S-001-OUTPUT@v1.2]`）：安全但可能过时
- 活引用（`@[S-001-OUTPUT]`）：总是最新，但内容漂移

我的提议：**Stage 边界是天然的版本锚点**。Stage N 引用 Stage N-1 时，隐式绑定 Stage N-1 的"完成版本"。这样：
- 引用稳定（Stage 完成后不再改变）
- 追踪清晰（Stage 号即版本号）

#### 一个更深的问题

最后，我想抛出一个元问题：

> 我们设计的是"给 Agent 用的 Stage 管理系统"，还是"给人类用、Agent 也能用的系统"？

如果是前者，应该优化机器可解析性（严格 schema、工具 API）。
如果是后者，应该保持人类可读性优先（Markdown 自然语言 + 约定）。

W-0010 的 Shape.md 当前是后者风格（人类可读的任务简报）。如果我们要引入自动生成的 `prologue.auto.md`，需要明确：这个文件是给谁看的？

我的倾向：**人机双可读**——对人类是可审阅的摘要，对 Agent 是可解析的结构化数据。这要求 `prologue.auto.md` 采用"结构化 Markdown"风格（有固定 section、可预测格式）。


### Craftsman 发言

我认可 Seeker 的“栈帧/接口承诺”类比，但从实现与可回归角度，我想把它进一步压成**可判定契约**（能被工具抽取、能被 grep/validate 检查、能避免双写漂移）。

我认为 Stage 管理机制的核心，不是“把文档切开”，而是：

1) **Stage 边界要成为稳定的版本锚点（freeze point）**，否则跨 Stage 引用必然漂移；
2) **自动生成的内容必须有“来源可追溯、语义可验证”的下限**，否则 `prologue.auto.md` 会变成第二份 SSOT（且更容易错）。

下面我按 Q1~Q5 给出最小规范建议（偏工程约束）。

#### Q1: 最小有效单位 = “可依赖面（Dependable Surface）”

Goal 一句话不够；“验收全文”太重。建议把 Stage N→N+1 的信息固定成一个**依赖面清单**，本质是 Seeker 的 Interface Commitment 的可判定版本：

- **Produced Artifacts**：产物列表（文件/目录/命令/测试/接口），必须可定位
- **Stable IDs**：每个产物必须绑定一个稳定 ID（例如 Clause-ID / Artifact-ID），用于引用而不是靠“标题文本”
- **Contract**：对外可依赖的语义（MUST/MUST NOT），只写“边界可观察行为”，避免实现细节
- **Non-goals**：明确排除项（这是防止后续误用/返工的最高性价比条目）
- **Open Extension Points**：扩展点=“未兑现但被允许的依赖面”，必须声明稳定性级别（Stable/Experimental/Rejected）

扩展点形式化建议：把它当成一种“可实现的接口槽位”，至少包含：
- `Name/ID`
- `Owner Stage`（谁声明的）
- `Expected Inputs/Outputs`（类型/文件/符号层次即可）
- `Constraints`（不可违反的约束，如性能/格式/错误语义）
- `Stability`（Stable/Experimental；Rejected 表示明确不做）

#### Q2: 摘要覆盖不是“写摘要”，而是“冻结 + 再投影”

我同意“按需生成不同粒度 prologue”的方向，但要加两条护栏：

1) **Freeze First**：只有当 Stage 被标记为 Done（或 Frozen）后，才允许把它作为“可依赖输入”。未冻结的 Stage 只能被当作 Informative（derived）。
2) **Projection Must Be Traceable**：`prologue.auto.md` 的每一条摘要，必须能追溯到来源条目（哪一个 Artifact-ID/Clause-ID/文件段落）。否则它会在漂移时无法审计。

触发机制建议不是“每 3 个 Stage”，而是两类阈值：
- **Token budget 阈值**（ContextBuilder 预算触发）
- **引用密度阈值**（依赖面条目数/引用数触发）

实现上，优先做“结构化抽取 + 机械合并”，而不是让 LLM 自由发挥写摘要：
- 结构化抽取：从固定槽位区块提取条目
- 机械合并：按 ID 去重、按稳定性筛选、按优先级截断
- LLM 摘要若要用，应显式标注为 derived，并附带来源链接（trace links）

#### Q3: 与 Design-DSL 的关系：正交，但要避免 DSL 关键字膨胀

我建议 Stage 管理保持为“组织层 + 槽位约定”，不要急着把 `goal/output/extension-point` 变成 DSL 关键字。

原因：关键字一旦进入 DSL，就要承诺解析、验证、迁移成本；而 Stage 机制的早期价值来自“快速形成闭环”。

折中做法：
- 仍然用 Markdown 结构化标题作为槽位边界
- 槽位内部用现有 Design-DSL（term/clause）写可依赖面的规范点（尤其是 MUST/MUST NOT）

换句话说：Stage 提供容器与生命周期；Design-DSL 提供“可引用、可审阅”的规范锚点。

#### Q4: 跨 Stage 引用：必须裁决“快照 vs 活引用”

我更偏向 Seeker 的判断：**Stage 边界天然是版本锚点**。

但要落地，需要一条工程规则：

- **Stage 一旦 Frozen，其目录下除 `prologue.auto.md` 外的文件 MUST NOT 变更**。

否则“Stage 号=版本号”不成立。

引用语法我建议分两层：

1) 人类阅读层：用相对路径链接（可点击）
2) 机器可判定层：用稳定 ID（Clause-ID / Artifact-ID）

例子（示意，不要求现在就实现新语法）：
```markdown
- Depends-On: @[S-RBF-FRAME-FORMAT] (defined in stage-02)
- Artifact: stage-02/output/rbf-interface.md (Artifact-ID: RBF-INTERFACE)
```

引用断裂处理（删除/移动）建议是“升级为显式失败”，不要静默：
- 工具校验时发现引用不可达 → 直接报错（这是迁移回退守卫的一部分）

#### Q5: 实例验证：先做一个“最小可跑”的 Stage 模板

为了尽快验证，我建议先规定一个最小模板（人机双可读），并用 W-0010 的 stage-01..03 试跑：

```markdown
## Stage Meta
- Stage: 02
- Status: Draft | Frozen
- Inputs: [Stage 01]

## Dependable Surface (for next stages)
### Produced Artifacts
### Contracts (MUST/MUST NOT)
### Extension Points
### Non-goals

## Notes (Informative)
```

工具侧最小验收（不需要 LLM）：
- 能抽取 Produced Artifacts/Contracts/Extension Points 的条目（哪怕只是基于 heading 的 slice）
- 能做引用可达性检查（路径 + Clause-ID 出现性）
- 能阻止 Frozen stage 被改动（例如 CI/脚本检查 git diff）

最后回应 Seeker 的“人机双可读”：我支持，但要补一句——**机器可读不是“更像 JSON”，而是“结构可预测 + 语义可验证 + 来源可追溯”**。只要做到这三点，即使仍是 Markdown，也能稳定演进。

### TeamLeader 总结

感谢 Seeker 和 Craftsman 的深度思考！我来提炼几个关键收获：

#### 核心洞察

1. **问题重构**：我们解决的不是"多 Stage 管理"，而是 **"Agent 认知带宽有限下的信息分层投影"**。Stage 是手段，不是目的。

2. **栈帧类比**成立且有价值：
   - `prologue` ≈ 保存的寄存器（前面的返回值）
   - `shape` ≈ 当前栈帧（完整局部上下文）
   - `epilogue` ≈ 下一次调用的 spec（契约）

3. **最小传递单位**：Interface Commitment（依赖面）
   - Produced Artifacts（产出了什么）
   - Contract（可依赖什么）
   - Non-goals（明确没做什么）
   - Extension Points（留了什么接口）

#### 工程约束共识

| 原则 | 说明 |
|:-----|:-----|
| **Freeze First** | 只有 Frozen 的 Stage 才能被依赖 |
| **Traceable** | 自动生成的内容必须可追溯到来源 |
| **Fail Loudly** | 引用断裂应该显式报错，不静默 |
| **人机双可读** | 结构可预测 + 语义可验证 + 来源可追溯 |

#### 下一步行动建议

1. **用 W-0010 实例验证**：按 Craftsman 的最小模板改造 stage-01/02/03
2. **暂不扩展 DSL 关键字**：Stage 机制保持为"组织层 + 槽位约定"
3. **工具先做结构化抽取**：基于 heading 的 slice，不依赖 LLM 摘要

#### 开放问题（留待后续）

- 摘要覆盖的触发机制（token budget vs 引用密度）
- 扩展点的稳定性级别（Stable/Experimental/Rejected）如何标记
- 跨 Wish 的 Stage 引用（如果一个 Wish 依赖另一个 Wish 的产出）

---

**会议收获已记录，可以开始实例验证了。**