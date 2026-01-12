# 记忆宫殿批量维护指南（阶段1）

**用途**：阶段1 — 批量处理各成员 inbox，产出汇总报告
**执行者**：TeamLeader
**后续**：完成后执行 `maintain-blackboard.md`（阶段2）

---

## 触发时机

监护人提示执行记忆维护时，按以下流程执行。

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
  5. 输出处理报告，包含：
     - 🎯 健康指标速览（顶部）
     - 🏷️ 小黑板候选（供阶段2汇总）
     - 📝 处理详情（折叠）

  请开始处理。
```

**MemoryPalaceKeeper 工作流程已优化**（v2，2025-12-27）：
- 伪代码风格定义流程，执行更高效
- **Git-as-Archive**：Git 历史即归档，无需 inbox-archive.md
- 每条便签处理后输出 checkpoint，便于追踪进度
- 终端命令一次性完成 inbox 重置和 Git 提交

### Step 3: 汇总并输出报告

从各 MemoryPalaceKeeper 报告中提取数据，汇总为阶段1报告。

**输出文件**：`agent-team/handoffs/memory/YYYY-MM-DD-HHmm-batch.md`
- 日期精确到分钟（一天可能多批）
- 例如：`2026-01-05-1930-batch.md`

**报告模板**：

```markdown
# 记忆宫殿批量维护报告（阶段1）

**执行时间**：YYYY-MM-DD HH:MM
**处理成员**：N 人
**调度者**：TeamLeader

---

## 🎯 健康状态汇总

| 成员 | 便签数 | 行数 | 密度 | 状态 | 趋势 |
|:-----|:-------|:-----|:-----|:-----|:-----|
| Seeker | 3 | 245 | 4.2% | ✅ | → |
| TeamLeader | 5 | 612 | 1.8% | ⚠️ | ↑ |
| ... | ... | ... | ... | ... | ... |

**整体评估**：X/Y 人健康，Z 人需要关注

---

## 🏷️ 小黑板候选汇总

从各成员报告收集的候选，供阶段2处理

| 成员 | 内容 | 类型 | 成熟度 | 证据 |
|:-----|:-----|:-----|:-------|:-----|
| Seeker | [摘要] | Recommend | Emerging | [链接] |
| Craftsman | [摘要] | Hot | Confirmed | [链接] |

*如无候选，写"（无）"*

---

## 📋 便签主题分布

用于阶段2发现跨人共性

| 主题关键词 | 涉及成员 | 便签数 |
|:-----------|:---------|:-------|
| DocGraph | Implementer, Investigator | 13 |
| Wish推进 | TeamLeader | 3 |

---

## 🔍 调度者观察

### 需要深度维护的成员

| 成员 | 触发原因 | 状态 |
|:-----|:---------|:-----|
| TeamLeader | 行数 > 500 | ⚠️ 建议维护 |

### 发现的问题与需求

- [能力差距、记忆健康风险、知识孤岛等观察]

### 协作机会

- [值得组织畅谈会的主题、流程改进建议]

---

## 📊 统计

- **处理便签**：共 X 条
- **小黑板候选**：Y 条
- **需深度维护**：Z 人
```

**汇总技巧**：
- 健康状态：从各 MPK 报告的「🎯 健康指标速览」表复制
- 小黑板候选：从各 MPK 报告的「🏷️ 小黑板候选」章节汇总
- 主题分布：从各成员的便签摘要提取关键词，合并统计
- 趋势：与上次报告对比（首次填 →）。

### Step 4: 触发深度记忆维护

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

**设计理念**：
- ⚠️ 是"建议"而非"必须"，尊重成员自主判断
- 立即激活让成员有机会自主决定，而非被动等待
- 无论是否维护，都可以收集对 Recipe 的反馈

### Step 5: 完成阶段1

1. **保存报告**到 `agent-team/handoffs/memory/YYYY-MM-DD-HHmm-batch.md`
2. **通知监护人**阶段1完成，可触发阶段2（`maintain-blackboard.md`）

**阶段衔接**：阶段2会读取本报告的「小黑板候选汇总」和「便签主题分布」章节。

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
