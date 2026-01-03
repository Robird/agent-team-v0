---
docId: "B-0002-Theory"
title: "Team-Leader 心智模型（Wish 驱动的多 Tier 推进）"
produce_by:
  - "wishes/active/wish-0003-exp-demand-contribute-jam.md"
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

---

## 5. 主循环（Leader Iteration Loop）

### 5.1 初始化（拿到 Wish 之后）
拿到 Wish 文件后，Leader 做初始化：
- 建立/选择一个工作目录与基础结构（会议文件、状态寄存器、关键链接）
- 从 Wish 抽取：目标、范围、约束、外部依赖、成功/失败判据
- 创建或更新：
  - 目标树（Goal Graph）初稿
  - 问题列表（Issue Graph）初稿（至少包含 Known-unknown）

### 5.2 迭代伪代码

```python
while True:
    wish = loadWish()  # 外部输入原动力

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

    if noFurtherAction(state):
        break  # M1: 推进到“无可行动”也算阶段性完成
```

### 5.3 关键函数的语义约束

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

#### needGuardianInput(demand, state)
- 触发条件示例：
  - Wish 的边界/优先级需要外部决策
  - 缺关键上下文（私有信息、业务目标、现实资源约束）
  - 需要授权（合并策略、发布节奏、对外承诺）

#### updateArtifacts(...)
- 强制把“对话结论”写回 SSOT：
  - 目标节点状态变化（done/blocked/next）
  - 问题节点状态变化（open/resolved/new）
  - 对应 Tier 文档的增量（决策、定义、约束、方案、实现）

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
