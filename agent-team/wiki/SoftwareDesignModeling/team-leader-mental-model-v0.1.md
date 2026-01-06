---
docId: "B-0002-Theory"
title: "Team-Leader 心智模型（Wish 驱动的多 Tier 推进）"
produce_by:
  - "wish/W-0003-jam-session/wish.md"
status: "Draft"
version: "0.1"
created: "2026-01-03"
---

# Team-Leader 心智模型（Wish 驱动的多 Tier 推进）

> **一句话定位**：以 Wish 为原动力，把项目视为在 Artifact-Tiers 多维状态空间中的受控演进；由 Leader 调度 AI 团队执行贡献动作、沉淀产物，并在需要时向监护人索取外部资源。

## 0. 范围与里程碑

### 0.1 这份模型解决什么
- 给 **Leader-of-AI-Team** 一套可重复执行的调度循环（能落到文本与代码变更）
- 给 AI 团队一套共享的“状态寄存器”（目标树 / 问题列表 / 会议记录）
- 将推进过程显式映射到 [Artifact-Tiers](../wiki/artifact-tiers.md) 五个层级，减少“层级错位”和“无效做功”

### 0.2 第一个里程碑（M1）
- 不追求必然把 Wish 推进到可运行。
- **M1 成功条件**：已经把 Wish 推进到“没有可采取的动作继续推进”（包括：
  - 关键外部依赖阻塞 → 进入 Biding，附带清晰的唤醒条件；或
  - 不可行/不值得 → 进入 Abandoned，并记录结论依据；或
  - 已得到足够的中间产物（跨 Tier 对齐），但继续推进需要监护人输入/资源）。

> **M1 的硬约束**：任何以“无法继续推进”作为结论的轮次，都必须输出两样东西：
> - **阻塞清单（Blockers）**：到底被什么卡住（信息/权限/外部依赖/时间/带宽/仓库状态等）
> - **唤醒条件（Wake Conditions）**：满足什么条件就能从 Biding 回到 Active（尽量可判定）

> 说明：M1 的价值在于把“推进”变成一个能终止、能复盘、能迁移的过程，而不是无限对话。

---

## 1. 基础假设与术语

### 1.1 项目被视为多维状态
借用 [软件演进动力学](./software-evolution-dynamics.md) 的视角：项目状态可以表示为一个跨 Tier 的向量 $S_t$，Leader 的工作是施加“贡献算子（Contributions）”使其朝向更可交付的状态演进。

### 1.2 两种驱动力（两类梯度）
> “梯度/驱动力”指：为什么现在需要行动。

1) **已规划但未实现的功能/目标（Planned-but-not-built）**
- 特征：方向大体明确，缺的是落地路径、实现与验证。

2) **已知但未解决的问题（Known-unsolved / Known-unknown）**
- 包含：Bug、设计矛盾、术语冲突、缺事实、以及“知道自己不知道”（重要信息）。

---

## 2. 驱动力的文本载体（给 LLM 用的“状态寄存器”）

> **语义优先原则**：第一阶段不要在“分散存储 vs 单文件维护”“目录如何组织”上消耗注意力。
> 先把每个节点/字段的**语义钉牢**（能扫描、能更新、能表达依赖、能形成焦点），再决定用 DocGraph 聚合还是显式维护单文件。

### 2.1 目标树 / 目标列表（Goal Graph）
用于承载“功能/目标”驱动力。

**形态建议（先从 Markdown 开始）**：
- 用 Markdown list 表达一个“目标树”，并用引用/ID 表达依赖（实际是图）。
- 每个目标节点建议包含：
  - `id`：稳定标识
  - `tier`：主要落点（Resolve/Shape/Rule/Plan/Craft）
  - `done`：是否完成
  - `depends_on`：依赖的目标/问题
  - `deliverable`：产物形态（文档/patch/test/decision log…）
  - `definition_of_done`：完成判据

*最低可用语义约束*（满足即可开始跑循环）：
- 能回答“这个目标是什么、现在状态如何、被什么阻塞、下一步最小动作是什么”。

> 目标树的用途：在任意时刻回答“注意力焦点应当在哪里”。

### 2.2 问题列表（Issue Graph）
用于承载“问题/未知”驱动力。

**形态建议（同样先从 Markdown 开始）**：
- 一份显式维护的 `issues.md` 当然最简单；
- 也可以分散存储（各文档的 Open Questions/Issue 段落），再用 DocGraph 聚合。

每个问题节点建议包含：
- `id`
- `symptom`：可观测症状（争论点/失败现象/缺事实）
- `tier`：主要发生的层级
- `blocks` / `blocked_by`：依赖关系
- `next_probe`：最小观测动作（Investigate/Test/Spike）
- `resolution`：结论（含依据）

*最低可用语义约束*（满足即可开始跑循环）：
- 能回答“这个问题是什么、如何观测/复现、下一步探针是什么、解决的完成判据是什么”。

> 问题列表的用途：把“不知道”变成可被调度的对象，避免在对话里遗失。

### 2.3 畅谈会文件（Jam / Chat Log as Working Memory）
畅谈会文件是“团队短期工作记忆”，用于记录发言与即时产出，但**不应成为长期 SSOT**。

建议规则：
- 文件过大就切分并继承关键信息（避免单文件过长导致后续 Agent 读取成本爆炸）。
- 每次循环结束前，Leader 要把“对话产出”沉淀回目标树/问题列表/对应 Tier 的权威文档。

---

## 3. 跨 Tier 的产物类型（每个 Tier 三类产物）

对任意 $Tier \in \{Resolve, Shape, Rule, Plan, Craft\}$，Leader 期望每轮至少推进以下之一：

1) **推进功能实现**（Feature Progress）
- 让某个目标节点更接近 Done。

2) **推进问题解决**（Issue Progress）
- 让某个问题从 Unknown/Conflict 走向 Stable/Resolved。

3) **发现并记录新问题**（New Issue Discovery）
- 把隐性风险显性化（尤其是 Known-unknown）。

> 注：第三类不是“失败”，而是减熵。

---

## 3.1 完成判据（Done ≠ 绿测）

> 经验：对 LLM 驱动的实现而言，“测试通过”经常不足以作为完成判据。
> 常见失败模式包括：忽视/误读设计条款、重复实现、以及“我以为…”式幻觉。

因此，本模型把“完成”拆成两层：

1) **功能完成（Craft correctness）**：实现 + 测试通过
2) **质量完成（Review closure）**：至少一次显式 Code Review 迭代闭环（Review → Fix → Re-Review），直到未发现明显问题

并且：Wish 的“目标客户/优化方向”不等于“交付范围”。若后续需要集成到某个 consumer（例如 RBF），应拆分为新的 Wish/Issue。

---

## 3.2 Review-to-Closure（范围受控的质量闭环）

> 来自实验记录：`agent-team/experiment/2026-01-04-sizedptr-wish0004-reviewloop/2026-01-04-sizedptr-code-review-loop.md`

当 Craft 产物出现后，Leader SHOULD 默认进入一个 **Review → Fix → Re-Review** 的闭环，直到满足 stop condition。

### 3.2.1 Scope Gate（范围闸门）

LLM 审阅者很容易提出“范围外但听起来很合理”的建议（例如并发安全、加大量工具方法）。因此每次审阅 MUST 明确：

- **In-Scope（允许）**：
  - Design/Spec ↔ Code/Test 对齐
  - 正确性/边界/溢出/异常语义
  - API 误用成本（仅限非破坏性微调）
  - 可维护性（不改变外部语义的去重/小重构）

- **Out-of-Scope（禁止）**：
  - 并发安全/多线程改造
  - 新增大量便捷方法/工具函数
  - 扩展到其他 consumer 的接入/迁移（应拆分为新 Wish/Issue）
  - 推翻已决策的关键参数（例如 bit 分配/格式契约）

**规则**：Out-of-Scope 建议必须进入 Parking Lot（记录但不执行），以避免 review 退化成“许愿池”。

### 3.2.2 Review 交付物（建议）

- FixList（分级）：Sev0/Sev1/Sev2
- Parking Lot：最多 3 条（范围外建议）

### 3.2.3 Stop Condition（建议）

> 连续一次 Re-Review 未发现 Sev0/Sev1，即视为 review closure。

### 3.2.4 reviewFindings：最小可机读输出（建议 Schema）

为了让 `reviewArtifacts(state)` 的结果可被 Leader 后续调度（自动生成修复计划、驱动下一轮 Re-Review），建议把审阅输出收敛为一个最小结构。

```yaml
reviewFindings:
  meta:
    target:
      wishId: "W-0004"
      artifactTier: "Craft"
      codeRefs:
        - "atelia/src/Data/SizedPtr.cs"
      testRefs:
        - "atelia/tests/Data.Tests/SizedPtrTests.cs"
      designRefs:
        - "atelia/docs/Data/Draft/SizedPtr.md"

  scopeGate:
    inScope:
      - "Design/Spec ↔ Code/Test 对齐"
      - "边界/溢出/异常语义"
      - "API 误用成本（非破坏性微调）"
    outOfScope:
      - "并发安全/多线程改造"
      - "新增大量便捷方法"

  fixList:
    - id: "F-001"
      severity: "Sev1"  # Sev0 | Sev1 | Sev2
      title: "测试名实不符"
      location: "atelia/tests/Data.Tests/SizedPtrTests.cs:203"
      evidence: "名称宣称 checked/throws，但用例只验证正常加法"
      suggestedFix: "重命名为更诚实的语义，并补充注释"

  parkingLot:
    - "并发安全：让类型线程安全（Out-of-Scope，记录但不执行）"

  closure:
    hasIssues: true
    stopCondition: "连续一次 Re-Review 无 Sev0/Sev1"
    canCloseNow: false
```

> 备注：该 Schema 的核心目的不是“形式主义”，而是给 LLM 一个可判定的输出契约，使 review 不再退化成散乱点评。

---

## 3.3 防漂移护栏：先有 Shape-Tier SSOT，再进入 Craft

> 来自 Wish-0004 模拟：当缺失明确的 Shape-Tier 产物（例如 API 外观/命名/契约 SSOT）时，团队往往会在设计文档里用“伪实现代码块”代替 API 定义。
> 这会导致文档结构 ≠ 真实代码结构的漂移，并诱发审阅者要求“补齐伪实现”，从而产生范围外建议泛滥。

因此，Leader 在进入 Craft 前 SHOULD 促成最小 Shape-Tier 产物：
- 一份简短的 API 外观 SSOT（签名 + 关键语义要点；避免用大段伪实现承载 SSOT）
- 明确声明“实现 SSOT 是哪个源文件/测试文件；文档不内嵌伪实现以避免漂移”

### 3.3.1 API 外观 SSOT：最小模板（建议）

下面给出一个“够用就行”的 API 外观 SSOT 模板（可放在对应 Wish 的 Shape 层产物里）。

```markdown
# API Surface SSOT: <TypeName>

## Type
- Namespace: `Atelia.Data`
- Kind: `public readonly record struct <TypeName>(ulong Packed)`

## Semantics
- Represents: `[StartBytes, StartBytes + LengthBytes)` (half-open)
- Alignment: 4B aligned (both components)
- Special values: none (Null/Empty are consumer-owned conventions)

## Public Members
- `ulong Packed` — packed representation (no validation implied)
- `ulong OffsetBytes` — start offset in bytes (4B aligned)
- `uint LengthBytes` — length in bytes (4B aligned)
- `ulong EndOffsetExclusive` — checked `OffsetBytes + LengthBytes`
- `static <TypeName> FromPacked(ulong)` — no validation
- `static <TypeName> Create(ulong offsetBytes, uint lengthBytes)` — validates, throws
- `static bool TryCreate(ulong offsetBytes, uint lengthBytes, out <TypeName>)` — validates, no throw
- `bool Contains(ulong position)` — half-open interval check

## Misuse Traps
- `Packed==0` is not automatically Null; consumers may define their own conventions.
- `FromPacked` does not validate; use `Create/TryCreate` for writing paths.

## SSOT Pointers
- Implementation: `atelia/src/Data/<TypeName>.cs`
- Tests: `atelia/tests/Data.Tests/<TypeName>Tests.cs`
```

> 规则：该模板刻意不包含“伪实现代码块”。它只承载外观、语义、误用陷阱与 SSOT 指针。

---

## 4. Leader 的核心调度动作（能做什么）

Leader 的动作分三类：

1) **激活团队贡献**：通过 `runSubagent` 调度 AI 小伙伴执行有限类别的贡献动作（模板化）。
2) **向监护人索取外部资源**：终止工具调用循环，输出一个明确的“需要什么/为什么需要/否则无法推进”的请求。
3) **更新关键文档**：把当前进展写回目标树、问题列表、以及各 Tier 的权威文档（SSOT）。

### 4.1 AI 小伙伴的“贡献动作”类别（模板库）
可复用的动作集合（不追求穷尽，追求可调度）：
- 理解需求 / 问题重述
- 分析 / 拆分 / 推导
- 调查 / 收集证据 / 代码考古
- 指出盲点与误区 / 反例 / 边界
- 总结 / 对比 / Tradeoff
- 审阅（合理性 / 一致性 / 自洽性）
- 发掘问题 / 形成 Open Questions
- 提供知识（定义、资料、引用）
- 修订文档
- 设计算法 / 设计方案
- 编写源码 / 最小原型（Spike/PoC）
- 扮演用户体验并反馈
- 运行测试 / 复现 / 回归验证
- 代码审阅 / 一致性审阅（Design ↔ Code / 规范 ↔ 实现）
- API/UX 评审（命名、示能性、误用成本）

---

## 5. 主循环（Leader Iteration Loop）

### 5.1 初始化（拿到 Wish 之后）
拿到 Wish 文件后，Leader 做初始化：
- 建立/选择一个工作目录与基础结构（会议文件、状态寄存器、关键链接）
- 从 Wish 抽取：目标、范围、约束、外部依赖、成功/失败判据
- 创建或更新：
  - 目标树（Goal Graph）初稿
  - 问题列表（Issue Graph）初稿（至少包含 Known-unknown）

> **输入质量提示**：Wish 是系统的外部输入。若输入本身存在明显噪声（例如：模板字段未替换、正文重复、链接失效），应当把“输入去噪/校验”作为第一批次动作之一，否则会把噪声扩散进目标树与问题列表。

### 5.2 迭代伪代码

```python
while True:
    wish = loadWish()  # 外部输入原动力

  validateInputs(wish)  # 轻量校验：ID/链接/重复段落等（失败→记录为 issue）

    state = readStateRegisters(
        goal_graph,
        issue_graph,
        tier_ssot_docs,
        recent_jam_logs,
    )

    focus = chooseFocus(state)  # 结合依赖关系，选“注意力焦点”

    demand = identifyDemand(state, focus)  # demand 必须可执行且可终止（含 stop_condition）
    if demand is None:
        if goalAchieved(state, wish):
            break
        demand = "Explore/Brainstorm"  # 信息不足时先抬高信息密度

    if needGuardianInput(demand, state):
        requestFromGuardian(demand, state)
        break

    if isMeltdownSignal(state, focus):
        handleMeltdown(state)
        continue

    contributor = matchContributor(demand)
    runSubagent(contributor, instruction=compileInstruction(demand, state))

    updateArtifacts(
        demand,
        goal_graph,
        issue_graph,
        tier_ssot_docs,
        jam_log,
    )

    # 质量闭环：对 Craft 产物执行 review-to-closure
    if demandProducesCraft(demand):
        reviewFindings = reviewArtifacts(state)
        if reviewFindings.hasIssues:
            planFixes(reviewFindings)
            continue

    if noFurtherAction(state):
        break  # M1: 推进到“无可行动”也算阶段性完成
```

### 5.3 关键函数的语义约束

#### validateInputs(wish)

目的：防止“外部输入噪声”污染整个演进过程。

建议的最小校验项（通过则继续；不通过不一定要终止，但必须登记为 issue）：
- Wish ID 是否稳定唯一（例如 frontmatter `wishId` 与文件名/链接体系一致）
- `produce` 链接是否存在
- 是否存在明显重复段落/重复目标（可先标记为 issue，后续再清理）

deliverable（最小）：
- 事实清单 + Issue Graph 新增节点（记录要修的输入问题与后果）


#### demand：最小可调度 Schema（建议）

为了让 `runSubagent` 的请求可生成、可检验、可终止，建议把每轮的 `demand` 约束为一个最小结构：

- `action`：贡献动作类型（Investigate/Test/Clarify/Propose/Implement/Review…）
- `scope`：输入边界（要读哪些文件/链接？要回答哪些问题？不得越界到哪里？）
- `deliverable`：期望产物（FixList/Patch/Decision Log/Tradeoff Table/事实清单/测试结果…）
- `definition_of_done`：完成判据（什么算“完成/可用”）
- `stop_condition`：停止条件（达到什么就停；避免无限扩写/无限探索）

> 经验法则：缺少 `deliverable` 会退化成“聊天”；缺少 `stop_condition` 会退化成“无限扩写”。

#### chooseFocus(state)
- 输入：目标树、问题列表、各 Tier 的成熟度与阻塞关系
- 输出：本轮唯一/少量“焦点对象”（目标或问题）
- 规则：优先选择“解锁最多后续行动”的焦点（高扇出阻塞点）

#### identifyDemand(state, focus)
- 输出必须是可执行且可终止的：`action + scope + deliverable + definition_of_done + stop_condition`
- 若分歧/不确定度很高：优先观测类动作（Investigate/Test/Spike）

> **冲突优先原则（拟合视角）**：若发现 Wish 与现有“规范/接口契约/上游文档”存在矛盾，先不要默认“文档 SSOT 必然压倒 Wish”。
> 
> 将其视为一个需要优化/拟合的多目标问题：
> - 先把矛盾登记为 Issue（明确冲突点、涉及的 Tier、影响范围与候选处理方式）
> - 然后优先产生 `Clarify/Decide` 类 demand（交付物为 Decision Log / Tradeoff Table），以显式方式选择：
>   - 调整需求（仅当 Wish 被判定为低优先级/草稿/不可行），或
>   - 调整“模型参数”（更新规范/接口契约），或
>   - 引入分层/封装（产品类型保持纯净，使用者在其层定义约定）
> 
> **护栏**：除非该 Wish 被明确降级，否则不要“削足适履”地为了既有草稿设计去扭曲需求。

#### needGuardianInput(demand, state)
- 触发条件示例：
  - Wish 的边界/优先级需要外部决策
  - 缺关键上下文（私有信息、业务目标、现实资源约束）
  - 需要授权（合并策略、发布节奏、对外承诺）

#### updateArtifacts(...)
- 强制把“对话结论”写回 SSOT：
- 强制把“对话结论”写回 SSOT：
  - 目标节点状态变化（done/blocked/next）
  - 问题节点状态变化（open/resolved/new）
  - 对应 Tier 文档的增量（决策、定义、约束、方案、实现）

并在产生 Craft 产物时追加：
- 更新 Review 状态（是否完成 review-to-closure）
- 更新 API Review 状态（是否完成可用性讨论与改名/破坏性变更评估）

#### reviewArtifacts(state)

目的：让“测试通过”之后仍能捕获 LLM 常见错误（漏读条款、重复实现、边界条件遗漏、命名误导等）。

建议的最小 Review 输出（FixList）：
- Design ↔ Code 对齐检查（是否违反设计文档的关键条款）
- 重复实现/已有能力发现（是否已有类似类型/工具）
- API 易用性与误用成本（命名是否诱导错误使用）
- 性能/分配/溢出/异常类型等细节风险

stop_condition：连续一次 review 未发现 Sev0/Sev1 问题，即可视为“review closure”。

##### 最小 FixList 模板（Markdown，可直接复制粘贴）

> 目标：让审阅输出“可执行、可分流、可闭环”，并显式阻断范围外建议。

```markdown
## Review Summary

### Target
- Wish: `W-XXXX` / (link)
- Tier: Craft
- Code:
  - `path/to/file1`
  - `path/to/file2`
- Tests:
  - `path/to/tests`
- Design/Spec:
  - `path/to/spec-or-design.md`

### Scope Gate
- **In-Scope（允许）**：Design/Spec ↔ Code/Test 对齐；边界/溢出/异常语义；API 误用成本（非破坏性）；可维护性（不改外部语义）
- **Out-of-Scope（禁止）**：并发安全/多线程；新增大量便捷方法；扩展到其他模块/consumer；推翻已决策的关键参数

---

## FixList

### Sev0（必须修，阻止合入/发布）
- [ ] (file:line) **问题**：...
      **证据**：...
      **建议修复**：...

### Sev1（应修，阻止 closure）
- [ ] (file:line) **问题**：...
      **证据**：...
      **建议修复**：...

### Sev2（可选修，收益/成本权衡）
- [ ] (file:line) **问题**：...
      **证据**：...
      **建议修复**：...

---

## Parking Lot（范围外建议：只记录，不执行；最多 3 条）
1) ...
2) ...
3) ...

---

## Closure Decision
- Sev0 count: _
- Sev1 count: _
- 结论：
  - [ ] Close (本轮无 Sev0/Sev1)
  - [ ] Continue (存在 Sev0/Sev1，进入 Fix → Re-Review)
```

> 建议：把 FixList 的每一项都写成“能被下一位贡献者直接执行”的指令（含定位、证据、建议修复），否则 review 会退化成观点输出。

#### apiUsabilityReview(state)

当 public API 形成或准备冻结时，应主动调度一次 Shape-Tier 的 API 易用性讨论：
- 由 `Curator` 主导“扮演用户/命名/示能性/误用成本”
- 交付物：命名候选 + tradeoff + 最终选择 + 破坏性变更评估

#### noFurtherAction(state)
- 用于 M1 终止：系统当前无法再提出不依赖外部资源的有效动作
- 必须输出：阻塞清单 + 唤醒条件（否则就是“烂尾”）

建议输出格式（可直接贴进会议文件/状态文件）：

```markdown
### Blockers（阻塞清单）
- [ ] ...（为什么阻塞、阻塞在哪个 Tier、需要谁提供什么）

### Wake Conditions（唤醒条件）
- ...（满足即可继续推进的可判定条件）

### Next Action After Wake（解除后第一步）
- ...（解除阻塞后应执行的最小动作）
```

---

## 6. 我们接下来可以怎么把它变得可运行（建议的下一步）

1) **补齐状态寄存器的最小文件结构**（模板）
- `project-status/goals.md`
- `project-status/issues.md`
- `project-status/snapshot.md`（可选：聚合视图）

2) **定义一个最小的 demand schema**（让指令可生成、可检验）
- `action` / `scope` / `deliverable` / `definition_of_done` / `stop_condition`

3) **用一个真实 Wish 做一次“端到端演练”**
- 只跑到 M1：推进到无可行动/需监护人输入
- 产出：一组可复用模板 + 一个可复盘的实例

4) **把“工具型算子（仓库工具）”纳入可调度动作集合**
- 例如：运行 DocGraph 的 `validate/fix/generate` 作为一种低成本的“观测/对齐”算子
- deliverable：校验通过报告、修复后的 frontmatter/链接、生成的索引文件
