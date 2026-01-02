# 畅谈会主持指南 (Jam Session Hosting Guide)

> **一句话定位**：基于**需求与贡献**驱动的动态协作会议
>
> **版本**：5.0 (Demand-Contribution Exp)
> **日期**：2026-01-02
> **状态**：Experimental
> **维护者**：TeamLeader

---

## 🚀 核心逻辑：需求-贡献循环

不再是"提问-回答"的访谈，而是"需求-贡献"的撮合。

```python
while True:
    snapshot = analyzeScene()           # 分析场势
    demand = identifyDemand(snapshot)   # 识别当前能力需求 (e.g., 需要代码实现? 需要逻辑审阅?)
    
    if demand is None:
        if goalAchieved(): break
        else: demand = "Explore/Brainstorm"

    contributor = matchContributor(demand) # 从能力池中寻找最佳匹配者
    
    # 明确要求贡献动作
    runSubagent(contributor, instruction=f"请执行动作：{demand.action}，范围：{demand.scope}")
    
    updateArtifacts()                   # 沉淀产物
```

## 🔑 关键概念

### 1. 贡献动作 (Contribution Actions)
畅谈会是由一系列具体的**贡献动作**推动的，而非空泛的讨论。
- **认知类**: 理解需求, 分析, 调查, 指出盲点, 总结, 对比
- **构建类**: 设计方案, 编写源码, 设计算法, 修订文档
- **验证类**: 审阅(合理性/一致性), 扮演用户体验, 运行测试, 发掘问题

### 2. 能力池 (Capability Pool)
主持人需根据成员优势进行调度。

**参谋组**:
- `Seeker`: 本质追问, 建立分析框架;快速设计方案;体验分析
- `Curator`: 扮演用户体验;快速设计方案;快速分析

**救火队**:
- `Craftsman`: 审阅代码;技术知识;审阅设计文档的合理性、一致性、自洽性;编写关键代码;严谨设计方案;指出盲点和误区;严谨分析
- `Craftsman.OpenRouter`: `Craftsman`的故障转移备份，遭遇服务器Rate-Limit时临时回退。

**前线组**:
- `Investigator`: 缓存多级信息索引，能快速调查信息
- `Implementer`: 缓存实现要点记忆，善于快速撰写代码增量
- `QA`: 缓存各种测试命令和脚本知识
- `DocOps`: 缓存关键文档链接和文档关系知识

**基础结构**:
- `MemoryPalaceKeeper`: 帮助大家将`inbox.md`中的便签以最合适的方式合并入`index.md`中
- `Moderator`: 畅谈会主持;调度者
- `TeamLeader`: 对项目目标和需求的深度理解;项目二把手，常规事务决策！
- `监护人/User`: 文档和互联网知识之外的项目理解;外部资源获取(LLM访问，硬件，软件服务);动机生成;项目一把手，风险决策！

### 3. 产物层级 (Artifact-Tiers)
始终保持对当前讨论层级的感知：
- **Resolve**: 值得做吗？分析得失并形成决心！
- **Shape**: 用户看到什么？功能边界，功能形态。
- **Rule**: 什么是合法的？自洽、一致、可行。
- **Plan**: 走哪条路？设计合理性、技术路径优化。
- **Craft**: 怎么造出来？实现需复合设计文档、编码质量、单元测试。

---

## 📋 操作流程

### 1. 开场 (Preparation)
- 创建会议文件 `agent-team/meeting/<project>/YYYY-MM-DD-<topic>.md`
- 明确**初始需求**：我们为什么聚在这里？需要解决什么具体问题？
- 提供**关键上下文**：关键背景信息？关键文件路径？

### 2. 进行中 (The Loop)

#### 场势快照 (Snapshot)
每轮回复后，主持人更新快照：
```markdown
#### 场势快照
- **当前层级**：[Resolve/Shape/Rule/Plan/Craft]
- **核心共识**：...
- **当前分歧**：...
- **当前需求**：[例如：需要对方案A进行代码可行性验证]
- **下一步动作**：[例如：邀请 Implementer 写一段原型代码；开放式询问“对前面的发言有哪些看法？”]
```

#### 调度策略 (Dispatch)
- **识别需求**：当前缺什么？是缺思路（Brainstorm），还是缺事实（Investigate），还是缺代码（Implement）？
- **匹配成员**：谁最擅长这个？
- **明确指令**：不要问"你怎么看"，要问"请基于你的专业视角，对X进行Y操作"。

### 3. 收尾 (Closing)
- **结论同步块**：
  - ✅ 已定 (Decided)
  - ❓ 未定 (Open Questions)
  - 📋 行动项 (Action Items) -> 转化为 Wish 或 Issue
- **沉淀**：将共识写入对应 Tier 的文档中。

---

## 🛑 熔断与托住

| 信号 | 动作 |
|:-----|:-----|
| **层级错位** (A谈代码, B谈价值) | **托住**: "我们先暂停一下，确认当前是在解决 Craft 问题还是 Resolve 问题？" |
| **缺乏事实** (空对空争论) | **托住**: "看来我们需要更多信息。@Investigator，请去调查一下..." |
| **僵局** (无法达成共识) | **记录**: 将分歧点记录为 Issue，结束当前话题，避免无休止争论。 |

---

## 📝 检查清单 (Checklist)

- [ ] 是否明确了当前的**能力需求**？
- [ ] 是否邀请了**最适合**的成员进行贡献？
- [ ] 是否产出了**实质性**的内容（代码、文档修订、明确的结论）？
- [ ] 是否更新了**场势快照**？
- [ ] 结束时是否有**结论同步块**？

---

## 🔗 相关资源
- [Artifact-Tiers](../../agent-team/wiki/artifact-tiers.md)
