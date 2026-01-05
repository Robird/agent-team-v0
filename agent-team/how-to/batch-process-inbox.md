# 记忆宫殿批量维护指南

> **用途**：当监护人触发记忆维护时，Team Leader 使用此模板调用 MemoryPalaceKeeper

---

## 触发时机

监护人会定期（预计每天 1 次）提醒执行记忆维护。

收到提醒后，按以下流程执行。

---

## 执行流程

### Step 1: 检查各成员 inbox 状态

```bash
cd /repos/focus/agent-team/members
for dir in */; do
  if [ -f "$dir/inbox.md" ]; then
    lines=$(grep -c "^## 便签" "$dir/inbox.md" 2>/dev/null || echo "0")
    if [ "$lines" != "0" ]; then
      echo "$dir: $lines 条待处理"
    fi
  fi
done
```

### Step 2: 对有待处理便签的成员调用 MemoryPalaceKeeper

对每个有便签的成员目录，使用以下 runSubagent 调用：

```yaml
agentName: MemoryPalaceKeeper
description: 处理 <成员名> 的 inbox
prompt: |
  # 记忆宫殿管理任务

  ## 目标目录
  
  `agent-team/members/<成员名>/`

  ## 任务
  
  请处理该目录下的 `inbox.md` 文件中的所有便签。
  
  按照你的伪代码工作流程：
  1. PHASE 1: 读取 inbox.md + index.md
  2. PHASE 2: 逐条便签执行 CLASSIFY → ROUTE → APPLY
  3. PHASE 3: 终端命令收尾（heredoc 重置 inbox + Git 提交）
  4. PHASE 4: 计算 index.md 健康指标
  5. 输出处理报告（健康指标速览在顶部）

  请开始处理。
```

> **MemoryPalaceKeeper 工作流程已优化**（v2，2025-12-27）：
> - 伪代码风格定义流程，执行更高效
> - **Git-as-Archive**：Git 历史即归档，无需 inbox-archive.md
> - 每条便签处理后输出 checkpoint，便于追踪进度
> - 终端命令一次性完成 inbox 重置和 Git 提交

### Step 3: 汇总报告并评估健康状态

从每个 MemoryPalaceKeeper 报告的「健康指标速览」表中提取数据，汇总为批量报告：

```markdown
## 记忆宫殿批量维护报告

**执行时间**：YYYY-MM-DD
**处理成员**：N 人

### 健康状态汇总

| 成员 | 便签数 | 行数 | 密度 | 状态 |
|:-----|:-------|:-----|:-----|:-----|
| Seeker | 3 | 245 | 4.2% | ✅ |
| TeamLeader | 5 | 612 | 1.8% | ⚠️ |
| ... | ... | ... | ... | ... |

### 需要深度维护的成员

| 成员 | 触发原因 | 建议行动 |
|:-----|:---------|:---------|
| TeamLeader | 行数 > 500 | 执行 Step 4 |

**总计**：处理了 X 条便签，Y 人需要深度维护
```

> **汇总技巧**：直接从每个 MemoryPalaceKeeper 报告的「🎯 健康指标速览」表复制数据即可。

### Step 3.5: 批次汇总与小黑板维护

在所有成员 inbox 处理完成后，调用 MemoryPalaceKeeper 做批次汇总：

```yaml
agentName: MemoryPalaceKeeper
description: 批次汇总与小黑板维护
prompt: |
  # 批次汇总任务

  ## 背景
  
  刚完成本批次 inbox 处理，以下是各成员的处理报告摘要：
  
  <粘贴 Step 3 汇总的健康状态表>
  
  ## 任务
  
  1. **发现共性主题**：基于处理报告，识别多人便签涉及的共同主题
  2. **维护小黑板**：
     - 读取 `agent-team/blackboard.md`
     - 处理现有提名（检查 @某人 待确认的条目）
     - 根据共性主题考虑新增提名
     - 清理过期条目（14天TTL）
  3. **产出批次汇总**：
     - 本批次共性主题
     - 值得交叉引用的洞见
     - 小黑板变更摘要

  请开始处理。
```

> **设计理念**：两阶段模式让 MPK 在汇总阶段拥有「全局视角」，能够：
> - 发现跨成员的共性洞见
> - 维护小黑板（处理提名、清理过期）
> - 产出「热门借阅榜」雏形

### Step 4: 触发深度记忆维护（按需）

根据 Step 3 汇总的健康状态，决定是否触发深度记忆维护。

**两级触发机制**：

| 状态 | 触发方式 | 说明 |
|:-----|:---------|:-----|
| 🔴 需要深度重写 | **强制触发** | 调度者直接调用成员执行维护 |
| ⚠️ 建议维护 | **建议提醒** | 发送提醒便签，由成员自行决定+自主实施 |

#### 4.1 强制触发（🔴 状态）

对 🔴 状态成员，调用该成员自己执行维护：

```yaml
agentName: <成员名>  # 如 Seeker, Craftsman, TeamLeader 等
description: 执行 <成员名> 外部记忆深度维护
prompt: |
  # 外部记忆深度维护任务

  ## 背景
  
  在最近一次 inbox 批量处理后，你的认知文件 `agent-team/members/<成员名>/index.md` 健康指标触发了维护阈值：
  - 当前行数：XXX 行
  - 洞见密度：X.X%
  
  ## 任务
  
  请阅读 `agent-team/how-to/maintain-external-memory.md`，按照其中的维护流程整理你的 `index.md`。
  
  重点关注：
  1. 用"活性知识三问"判断哪些内容应归档
  2. 合并重复洞见，压缩过程记录
  3. 确保维护后能通过"冷启动三问测试"
  
  完成后输出维护报告，包含：
  - 维护前后行数对比
  - 主要压缩/归档的内容摘要
  - 洞见密度变化
```

#### 4.2 建议提醒（⚠️ 状态）

对 ⚠️ 状态成员，用 runSubagent **激活并提供维护机会**，由成员自行决定是否执行：

```yaml
agentName: <成员名>
description: <成员名> 外部记忆维护机会
prompt: |
  # 外部记忆维护机会

  ## 当前状态
  
  你的认知文件 `agent-team/members/<成员名>/index.md` 当前状态：
  - 行数：XXX 行（超过 500 行建议维护线）
  - 洞见密度：X.X%
  - 状态：⚠️ 建议维护
  
  ## 维护指南
  
  如果你决定维护，请参考 `agent-team/how-to/maintain-external-memory.md`。
  
  重点关注：
  1. 用"活性知识三问"判断哪些内容应归档
  2. 处理洞见之间的演进关系（A→B→C）
  3. 合并重复洞见，压缩过程记录
  4. 确保维护后能通过"冷启动三问测试"
  
  ## 你的选择
  
  这是一个**机会**而非**命令**。你可以：
  - **选择维护**：直接开始执行，完成后输出维护报告
  - **选择跳过**：如果你判断当前状态可接受，简要说明理由即可
  
  无论哪种选择，请在最后输出你对 `maintain-external-memory.md` v2.5.0 的反馈（如有）。
```

> **设计理念**：
> - ⚠️ 是"建议"而非"必须"，尊重成员自主判断
> - 立即激活让成员有机会自主决定，而非被动等待
> - 无论是否维护，都可以收集对 Recipe 的反馈

### Step 5: 输出调度者洞察报告

作为调度者，你能看到**全员认知状态**——这是任何单个成员都看不到的视角。

利用这个位置，输出**洞察性报告**（而不仅是事务性完成报告），为团队成员和监护人创造价值。

```markdown
## 记忆宫殿调度报告

**执行时间**：YYYY-MM-DD
**调度者**：TeamLeader

---

### 🎯 团队认知健康仪表盘

| 成员 | 便签数 | 行数 | 密度 | 状态 | 趋势 |
|:-----|:-------|:-----|:-----|:-----|:-----|
| Seeker | 3 | 245 | 4.2% | ✅ | → |
| TeamLeader | 5 | 612 | 1.8% | ⚠️ | ↑ |
| Implementer | 0 | 180 | 2.1% | ✅ | → |
| ... | ... | ... | ... | ... | ... |

**整体评估**：X/Y 人健康，Z 人需要关注

---

### 🔍 发现的问题与需求

#### 能力差距
- **Implementer** 连续 2 次维护无便签产出 → 可能需要提醒便签写作规范
- **XXX** 便签经常被标记为"模糊难分类" → 建议发送写作信号词表

#### 记忆健康风险
- **TeamLeader** index.md 连续 3 次增长（450→520→612）→ 压缩习惯未建立，建议安排深度维护辅导
- **Craftsman** 洞见密度降至 2.8%（接近 3% 阈值）→ 预警，下次重点关注

#### 知识孤岛风险
- RBF 接口设计相关洞见仅存在于 Seeker 的记忆中 → 建议 Implementer 同步学习

---

### 💡 协作机会与建议

#### 值得组织畅谈会的主题
- 本次 Seeker + Craftsman + TeamLeader 都产出了关于「洞见密度」的便签 → 建议组织畅谈会沉淀方法论

#### 流程改进建议
- MemoryPalaceKeeper 对「State+Discovery 混合便签」的处理出现 2 次不一致 → 建议强化规范

---

### 🙋 对监护人的请求（Asks）

> 以下事项需要人类介入决策：

1. **认知边界裁决**：Seeker 的「X 模式」洞见与 Craftsman 的「Y 原则」存在张力，需要决定以哪个为准
2. **归档决策**：TeamLeader 的 index.md 中有 150 行是 2025-12 的会议索引，是否应该移入 Archive？
3. **规范更新**：便签写作规范是否需要新增「Question」类型？本次发现 3 条便签本质是提问而非洞见

---

### 📊 本次维护统计

- **处理便签**：共 X 条
- **触发深度维护**：Y 人
- **下次建议时间**：YYYY-MM-DD（基于便签积累速度估算）
```

> **报告输出目录**：`agent-team/handoffs/memory/YYYY-MM-DD-HHmm-batch.md`
> - 一天多批时用时间精确到分钟
> - 与 meeting 命名风格一致

#### 洞察报告编写指南

| 观察维度 | 如何发现 | 报告到哪个章节 |
|:---------|:---------|:---------------|
| 便签产出分布异常 | 某成员连续 N 次无便签 | 能力差距 |
| 便签质量问题 | MemoryPalaceKeeper 报告中"模糊难分类"频繁出现 | 能力差距 |
| 记忆膨胀趋势 | 比较历史维护报告，行数持续增长 | 记忆健康风险 |
| 主题聚类 | 多人便签涉及同一概念 | 协作机会 |
| 跨成员冲突 | 新洞见与旧洞见矛盾 | 对监护人的请求 |
| 知识孤岛 | 某领域洞见只有一人有 | 知识孤岛风险 |

> **好领导的本质**：利用自己的位置优势（能看到全员状态），为团队成员发现他们自己看不到的问题，并向上争取资源解决这些问题。

---

## 成员目录列表

| 成员 | 目录路径 |
|:-----|:---------|
| Seeker | `agent-team/members/Seeker/` |
| Curator | `agent-team/members/Curator/` |
| Craftsman | `agent-team/members/Craftsman/` |
| Implementer | `agent-team/members/implementer/` |
| Investigator | `agent-team/members/investigator/` |
| QA | `agent-team/members/qa/` |
| DocOps | `agent-team/members/docops/` |
| TeamLeader | `agent-team/members/TeamLeader/` |
| MemoryPalaceKeeper | `agent-team/members/MemoryPalaceKeeper/` |

---

## 注意事项

1. **按需处理**：只处理有待处理便签的成员，空 inbox 跳过
2. **串行执行**：一次处理一个成员，避免并发冲突
3. **Git 提交**：MemoryPalaceKeeper 会在处理完成后自动 Git 提交，调度者无需额外操作
4. **元认知自我处理**：MemoryPalaceKeeper 自己也有 inbox，会在处理其他成员时顺带处理自己的便签

---

## 变更日志

| 日期 | 变更 |
|:-----|:-----|
| 2026-01-05 | v4.0: 新增 Step 3.5 批次汇总与小黑板维护；规范报告输出目录 `handoffs/memory/` |
| 2026-01-03 | v3.2: Step 4 改为两级触发机制——🔴强制触发、⚠️建议提醒；尊重成员自主判断 |
| 2026-01-02 | v3.1: 新增 Step 5 调度者洞察报告；定义「好领导」的观察维度与汇报结构 |
| 2026-01-02 | v3: 新增 Step 4 触发深度记忆维护；汇总报告增加健康状态列；与 maintain-external-memory.md 形成闭环 |
| 2025-12-27 | v2: 更新 prompt 适配伪代码工作流程；移除 inbox-archive.md 引用（Git-as-Archive）|
| 2025-12-26 | v1.1: 添加 MemoryPalaceKeeper 到成员列表 |
| 2025-12-26 | v1.0: 初版 |
