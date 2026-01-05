# 小黑板维护指南

> **用途**：阶段2 — 在批量 inbox 处理后，维护团队小黑板
> **执行者**：TeamLeader
> **前置条件**：已完成 `batch-process-inbox.md`（阶段1）

---

## 触发时机

在完成阶段1（batch-process-inbox）后，监护人提示执行阶段2。

**输入**：阶段1 产出的报告 `agent-team/handoffs/memory/YYYY-MM-DD-HHmm-batch.md`

---

## 执行流程

### Step 1: 读取阶段1报告

读取最新的批量处理报告，关注以下信息：

1. **🏷️ 小黑板候选汇总**：各成员报告中标记的候选条目
2. **📋 便签主题分布**：多人便签涉及的相同概念
3. **🎯 健康状态汇总**：是否有需要关注的成员

**查找最新报告**：
```bash
ls -t /repos/focus/agent-team/handoffs/memory/*-batch.md | head -1
# 文件名格式：YYYY-MM-DD-HHmm-batch.md
# 例如：2026-01-05-1930-batch.md
```

### Step 2: 处理小黑板候选

读取 `agent-team/blackboard.md`，对阶段1报告中的候选条目逐一处理：

**处理规则**：

| 候选类型 | 成熟度 | 处理方式 |
|:---------|:-------|:---------|
| Recommend | 任意 | 直接上架到「熟客推荐」栏 |
| Story | 任意 | 直接上架到「本周趣事」栏 |
| Hot | Confirmed | 直接上架到「今日特酿」栏 |
| Hot | Emerging | 写入提名区，标记 `@<确认人>` 待确认 |

**上架格式**（Recommend/Story）：

```markdown
### [成熟度符号] [标题]
[一句话描述]
— *[作者]* | [证据](链接) | YYYY-MM-DD
```

**提名格式**（Hot 待确认）：

```markdown
### 提名 [类型：Hot]
- **内容**：[一句话描述]
- **证据**：[链接]
- **提名者**：[来源成员]
- **待确认**：@[确认人]
- **date**：YYYY-MM-DD
```

### Step 3: 发现跨人共性

基于阶段1报告，识别多人便签涉及的共同主题：

1. **扫描关键词**：同一概念出现在 ≥2 人的便签中
2. **生成新候选**：如果共性主题有价值，生成 Hot 候选
3. **标注证据**：列出所有相关成员的证据链接

**共性主题提名格式**：

```markdown
### 提名 [类型：Hot]
- **内容**：[共性主题：一句话描述]
- **证据**：
  - [成员A/index.md#洞见ID]
  - [成员B/index.md#洞见ID]
- **提名者**：TeamLeader（基于跨人共性发现）
- **待确认**：@Craftsman（或其他适合确认的人）
- **date**：YYYY-MM-DD
```

### Step 4: 处理现有提名

扫描小黑板中的提名区，对每条提名：

| 情况 | 处理 |
|:-----|:-----|
| 有 `@我` 待确认 | 作为 TL 直接确认或拒绝 |
| 有 `@其他人` 待确认 | 保留，Step 5 会 ping 对方 |
| 无人确认的 Hot | 指定一个合适的确认人 |
| 已过期（>14天） | 删除或降级为 Recommend |

### Step 5: 执行 Ping 确认（按需）

对于标记了 `@某人` 待确认的提名，调用对应 Agent 确认：

```yaml
agentName: <确认人>  # 如 Craftsman, Seeker 等
description: 小黑板提名确认
prompt: |
  # 小黑板提名确认任务

  ## 待确认提名
  
  你在团队小黑板上有一条待确认的提名：
  
  ```
  <提名内容>
  ```
  
  ## 任务
  
  1. 读取证据链接，判断该洞见是否成熟
  2. 做出决定：
     - **确认**：该洞见有足够证据支持，可以上 Hot 栏
     - **降级**：有价值但不够成熟，改为 Recommend
     - **拒绝**：证据不足或有误，说明理由
  
  ## 输出
  
  - 决定：确认 / 降级 / 拒绝
  - 理由：一句话说明
  - 如果确认，建议的最终表述（可优化原文）
```

根据确认结果更新小黑板：
- **确认**：移动到 Hot 栏，标记 ✓ Confirmed
- **降级**：移动到 Recommend 栏，标记 ◐ Emerging
- **拒绝**：从提名区删除

### Step 6: 清理过期条目

检查各栏的日期，清理过期内容：

| 栏目 | TTL | 过期处理 |
|:-----|:----|:---------|
| Hot | 14天 | 归档到 `agent-team/archive/blackboard/` |
| Story | 7天 | 直接删除 |
| Recommend | 无限 | 保留 |

```bash
# 创建归档目录（如不存在）
mkdir -p /repos/focus/agent-team/archive/blackboard
```

### Step 7: Git 提交

```bash
git add agent-team/blackboard.md
git add agent-team/archive/blackboard/
git commit -m "blackboard: 阶段2维护 - N条上架, M条过期清理"
```

### Step 8: 输出维护报告

```markdown
## 小黑板维护报告

**执行时间**：YYYY-MM-DD HH:MM
**输入报告**：handoffs/memory/xxx-batch.md

---

### 📊 本次变更

| 操作 | 数量 | 详情 |
|:-----|:-----|:-----|
| 候选上架 | N | Recommend x, Story y, Hot z |
| 新增提名 | M | 基于跨人共性 |
| 确认处理 | K | 确认 a, 降级 b, 拒绝 c |
| 过期清理 | P | Hot p, Story q |

---

### 🔥 Hot 栏变更

- [+] [新上架的 Hot 条目]
- [-] [过期归档的 Hot 条目]

### 👍 Recommend 栏变更

- [+] [新上架的 Recommend 条目]

### 📸 Story 栏变更

- [+] [新上架的 Story 条目]
- [-] [过期删除的 Story 条目]

---

### 🔍 跨人共性发现

| 主题 | 涉及成员 | 处理 |
|:-----|:---------|:-----|
| [主题A] | Seeker, Craftsman | 生成 Hot 提名 |
| [主题B] | TeamLeader, Implementer | 已有相似洞见，跳过 |

---

### ⏳ 待处理提名

| 提名内容 | 待确认人 | 状态 |
|:---------|:---------|:-----|
| [提名A] | @Craftsman | 已 ping，待回复 |
| [提名B] | @Seeker | 本次未 ping（可选） |

---

### 📝 对监护人的备注

[如有需要人工决策的事项，写在这里]
```

---

## 与阶段1的衔接

### 阶段1报告应包含的信息

为了支撑阶段2，阶段1（batch-process-inbox）的报告需要包含：

1. **小黑板候选列表**（新增）：
   ```markdown
   ### 🏷️ 小黑板候选
   
   | 成员 | 内容 | 类型 | 成熟度 | 证据 |
   |:-----|:-----|:-----|:-------|:-----|
   | Seeker | [内容摘要] | Recommend | Emerging | [链接] |
   | Craftsman | [内容摘要] | Hot | Confirmed | [链接] |
   ```

2. **便签主题摘要**（用于发现共性）：
   ```markdown
   ### 📋 便签主题分布
   
   | 主题关键词 | 涉及成员 | 便签数 |
   |:-----------|:---------|:-------|
   | DocGraph | Implementer, Investigator | 13 |
   | Wish推进 | TeamLeader | 3 |
   ```

---

## 注意事项

1. **阶段顺序**：必须先完成阶段1，阶段2才有输入数据
2. **幂等性**：重复执行阶段2应该是安全的（不会重复上架）
3. **Ping 节制**：一次最多 ping 2-3 人，避免打扰
4. **人工检查点**：监护人可以在 Step 5 前审视提名，决定是否 ping

---

## 变更日志

| 日期 | 变更 |
|:-----|:-----|
| 2026-01-05 | v1.0: 初版 — 从 batch-process-inbox.md 分离为独立阶段2 |
