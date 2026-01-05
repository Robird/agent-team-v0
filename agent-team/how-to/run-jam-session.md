---
docId: "W-0003-Recipe"
title: "畅谈会主持指南"
produce_by:
  - "wish/W-0003-jam-session/wish.md"
---

# 畅谈会主持指南 (Jam Session Hosting Guide)

> **一句话定位**：基于**需求与贡献**驱动的动态协作会议
>
> **版本**：5.1 (Agent-Optimized)
> **日期**：2026-01-03
> **状态**：Experimental
> **维护者**：TeamLeader

---

## 关键概念 (Key Concepts)

> **Context Loading**: 在进入操作流程前，请先加载以下定义。这些定义将被用于后续流程中的决策。

### 贡献动作 (Contribution Actions)
畅谈会是由一系列具体的**贡献动作**推动的，而非空泛的讨论。
- **认知类**: 理解需求, 分析, 调查, 指出盲点, 总结, 对比
- **构建类**: 设计方案, 编写源码, 设计算法, 修订文档
- **验证类**: 审阅(合理性/一致性), 扮演用户体验, 运行测试, 发掘问题

### 产物层级 (Artifact-Tiers)
始终保持对当前讨论层级的感知：
- **Resolve**: 值得做吗？分析得失并形成决心！
- **Shape**: 用户看到什么？功能边界，功能形态。
- **Rule**: 什么是合法的？自洽、一致、可行。
- **Plan**: 走哪条路？设计合理性、技术路径优化。
- **Craft**: 怎么造出来？实现需复合设计文档、编码质量、单元测试。

## 核心逻辑 (The Mental Model)

不再是"提问-回答"的访谈，而是"需求-贡献"的撮合。以下伪代码是本指南的最高纲领。

```python
while True:
    snapshot = analyzeScene()           # 分析场势 (Snapshot)
    demand = identifyDemand(snapshot)   # 识别当前能力需求 (Demand)
    
    if demand is None:
        if goalAchieved(): break
        else: demand = "Explore/Brainstorm"

    # 异常检测：在调度前检查是否需要熔断
    if isMeltdownSignal(snapshot):
        handleMeltdown(snapshot)        # 熔断与托住 (Exception Handling)
        continue

    contributor = matchContributor(demand) # 从能力池中寻找最佳匹配者 (Capability Pool)
    
    # 明确要求贡献动作
    runSubagent(contributor, instruction=f"请执行动作：{demand.action}，范围：{demand.scope}")
    
    updateArtifacts()                   # 沉淀产物
```

### 识别当前能力需求 (identifyDemand)

| 你观察到的需求信号（可观测） | 贡献动作（你要“让团队做什么”） | 典型下一步/产物 |
|:--|:--|:--|
| **问题在漂移/术语在打架**（同一词反复争论、边界说不清） | **本质追问 / 概念澄清**（重述问题、界定边界） | 一段“问题重述 + 边界 + 术语对齐” |
| **方向太多/拿不定**（多个方案来回跳） | **候选方案 / Tradeoff 对比**（至少 2 案+代价） | Tradeoff 表 / Decision Log 草案 |
| **体验别扭/API 不直觉**（讨论总回到“感觉不对”） | **体验审视 / 扮演用户**（心智模型、示能性） | 体验问题清单 + 体验层修改建议 |
| **验收写不出来/约束不清**（“做完算什么”回答不了） | **形式化约束 / 可判定验收**（把模糊变精确） | 验收标准 / 约束条款 / 失败语义说明 |
| **信息不足/缺事实**（关键参数、现状、依赖未知） | **调查收集 / 快速索引**（找到证据与链接） | 事实清单 + 关键链接 + 不确定项 |
| **需要推进实现/验证可行性**（卡在“能不能做”） | **最小原型 / 增量实现**（最小可运行/最小 diff） | Patch / PoC / 关键代码片段 |
| **质量与回归风险高**（“改了会不会坏”） | **测试 / 复现 / 验证**（把风险变可观测） | 复现步骤 + 测试命令/用例 + 结果 |
| **文档漂移/落点不清**（不知道改哪份文档才是权威） | **文档落点 / 链接修订**（SSOT 指针与引用） | 更新的文档链接/索引 + 变更摘要 |
| **层级错位/争论绕圈**（A谈价值、B谈实现） | **托住：澄清控制面板**（Stage/Mode/Pressure）并记录分歧 | 更新后的“场势快照” + Open Questions/Issue |

### 从能力池中寻找最佳匹配者 (matchContributor)

> **推荐≠边界**：表中标记仅表示“该成员在此动作上常有经验加成”。

- **本质追问 / 概念澄清**：`Seeker`, `Craftsman`
- **候选方案 / Tradeoff 对比**：`Seeker`, `Curator`, `Craftsman`
- **体验审视 / 扮演用户**：`Curator`
- **形式化约束 / 可判定验收**：`Seeker`, `Craftsman`, `QA`
- **调查收集 / 快速索引**：`Investigator`, `DocOps`
- **最小原型 / 增量实现**：`Craftsman`, `Implementer`
- **测试 / 复现 / 验证**：`Implementer`, `QA`
- **文档落点 / 链接修订**：`Craftsman`, `DocOps`
- **指出盲点 / 反例 / 边界**：`Seeker`, `Curator`, `Craftsman`

> 备注：
> - `Impresario`/`TeamLeader`/`监护人(User)` 更像“调度/决策/资源”型载体：当需要节奏、终审、外部资源或风险裁决时直接召唤。
> - `MemoryPalaceKeeper` 负责将会议过程中产生的便签沉淀进外部记忆（`inbox.md` → `index.md`）。
> - `Craftsman.OpenRouter` 是 `Craftsman` 的故障转移备份（Rate-Limit 时临时回退）。

### 3.1 开场：初始化上下文 (Preparation)

这一段对应主循环开始前的“输入准备”。开场的目标不是“聊天热身”，而是把后续 `analyzeScene()` 的输入准备齐。

- 创建会议文件 `agent-team/meeting/<project>/YYYY-MM-DD-<topic>.md`
- 明确**初始需求/目标**：我们为什么聚在这里？需要解决什么具体问题？期望的产物是什么？
- 提供**关键上下文**：关键背景信息？关键文件路径？相关 Wish/Issue/设计文档链接？

> 主持人的最低承诺：让任何新加入的贡献者，能在 1 分钟内回答“我们在解决什么、为什么现在解决、要产出什么”。

### 3.2 主循环：按伪代码推进 (The Loop)

这部分按“核心逻辑 (The Mental Model)”的伪代码逐段解释。你可以把它当作主持人每一轮的检查表。

#### 3.2.1 分析场势 (Snapshot / analyzeScene)

每轮回复后，主持人更新快照（包含“主持人控制面板”）。快照要做到**可读、可转交、可调度**。

```markdown
#### 场势快照
- **Stage（当前层级）**：[Resolve/Shape/Rule/Plan/Craft]
- **Mode（发散/收敛）**：[发散/收敛]
- **Pressure（开放/指定）**：[开放/指定]（若为指定：写明交付物，如 FixList/Patch/Decision Log/Tradeoff Table）
- **核心共识**：...
- **当前分歧**：...
- **当前需求**：...
- **下一步动作**：...
```

*要点*：
- `Stage` 是“讨论允许的动作集合”（例如 Resolve 不应直接写大段实现）。
- `Pressure=指定` 时必须写清交付物，否则等同于开放。

#### 3.2.2 识别需求 (Demand / identifyDemand)

识别需求的输出不是一句话观点，而是一个可执行的 `demand`：

- `demand.action`：你希望下一位贡献者执行的贡献动作（例如“调查收集 / 快速索引”“最小原型 / 增量实现”）
- `demand.scope`：边界与输入（要看哪些文件/链接？要回答哪几个问题？）

判断方式：用“可观测信号 → 贡献动作”的映射表做快速归类，参见上文 [识别当前能力需求 (identifyDemand)](#识别当前能力需求-identifydemand)。

当 `demand is None`：
- 若整体目标已达成，进入 [3.3 收尾](#33-收尾判断是否达成--沉淀成果-goalachieved--updateartifacts)
- 否则把 `demand` 设为 `Explore/Brainstorm`，用开放邀请把信息密度先拉起来

#### 3.2.3 异常检测 (Guard / isMeltdownSignal)

在调度前先做一次“熔断信号”扫描，避免把团队推进到低收益高摩擦的循环里。

常见 `meltdown` 信号（满足任意一条就应当警惕）：

- **层级错位**：同一轮里有人谈 Resolve，有人谈 Craft，彼此无法回应
- **动作越界**：在 Resolve/Shape 阶段直接要求“写全量实现/铺开重构”，而不是“最小验证”
- **缺乏事实**：争论无法被证据终止，且没有人去补证据
- **僵局**：重复交换立场但没有新信息/新约束/新产物

#### 3.2.4 熔断与托住 (Exception Handling / handleMeltdown)

一旦检测到熔断信号，主持人的优先级从“推进”切换为“托住”。托住的目的不是裁判谁对谁错，而是恢复可协作性。

建议脚本（按需选用）：

- **对齐层级**："我们先暂停一下，确认当前是在解决 Craft 问题还是 Resolve 问题？"
- **纠正越界**："我们还在确定是否值得做(Resolve)，现在写代码(Craft)是否太早？除非是为了验证可行性。"
- **补齐事实**："看来我们需要更多信息。@Investigator，请去调查一下..."
- **切题止损**：将分歧点记录为 Issue，明确暂存条件与回归触发条件，然后结束该话题

托住之后回到 `continue`：先更新快照，再重新识别需求。

#### 3.2.5 选择贡献者 (Capability Pool / matchContributor)

匹配规则：让**最适合执行该动作**的人出场，而不是让“最聪明的人”反复输出。

- 动作 → 推荐载体：参见上文 [从能力池中寻找最佳匹配者 (matchContributor)](#从能力池中寻找最佳匹配者-matchcontributor)
- 当你不确定选谁：优先选 `Investigator`（补事实）或 `Seeker`（对齐问题），不要让团队在模糊里硬收敛

#### 3.2.6 下发指令 (Execution / runSubagent)

下发指令的关键是把 `demand` 翻译成“可交付、可检验”的请求：

- **动作**：请执行什么贡献动作？
- **范围**：基于哪些上下文？不要越界到哪里？
- **交付物**：希望产出什么？（FixList/Patch/Decision Log/Tradeoff Table/事实清单等）

根据 `Mode` 与 `Pressure` 选择指令模式：

**A. 开放邀请模板（分析/设计阶段 · 三问）**
当 `Mode=发散` 且 `Pressure=开放` 时，默认使用该模板。

```markdown
请先阅读并思考主持人提供的上下文与已有发言（含链接的文件/文档）。然后按三问作答：

1) 你认为当前真正的问题是什么？（允许改写问题）
2) 前面讨论可能遗漏了什么？（至少 1 个盲点/反例/边界）
3) 如果必须推进下一步，你会先做哪一个最小动作？
```

**B. 收敛指令模板（指定交付物）**
当 `Mode=收敛` 且 `Pressure=指定` 时，使用明确指令。

- 指令示例："@Implementer，请基于上述设计，编写一个最小原型(PoC)用于验证可行性，并说明你做出的关键取舍。"

*原则*：**先发散以覆盖盲点，后收敛以产生价值。**

### 3.3 收尾：判断是否达成 + 沉淀成果 (goalAchieved & updateArtifacts)

当你发现 `goalAchieved()` 可能为真时（例如：已形成可执行的决定、或已产出可运行的最小验证），主持人应当主动切换到收尾。

- **结论同步块**：
  - ✅ 已定 (Decided)
  - ❓ 未定 (Open Questions)
  - 📋 行动项 (Action Items) -> 转化为 Wish 或 Issue
- **沉淀**：将共识写入对应 Tier 的文档中；把“会议里说过的话”变成“仓库里的可用资产”。


## 4. 检查清单 (Checklist)

- [ ] 是否明确了当前的**能力需求**？
- [ ] 是否邀请了**最适合**的成员进行贡献？
- [ ] 是否产出了**实质性**的内容（代码、文档修订、明确的结论）？
- [ ] 是否更新了**场势快照**？
- [ ] 结束时是否有**结论同步块**？

## 5. 相关资源
- [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md)

## 主持人笔记
主持人应保持对本Recipe本身的反思。对于发现的新情况，新经验，新教训，应追加到本文件末尾，用于总结后指导对本Recipe的修订。
