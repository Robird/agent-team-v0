# 记忆维护组织指南

> **适用于**：Team Leader / StandardsChair 组织成员进行记忆维护
> **前置技能**：[memory-maintenance-skill.md](memory-maintenance-skill.md)（成员自身的维护技能）
>
> **状态**：v1.0（2025-12-22）
> **SSOT**：本文件是组织记忆维护的权威指南

---

## 概述

本文档定义 Team Leader 如何组织 AI Team 成员进行记忆维护。与 `memory-maintenance-skill.md`（成员自己执行维护）互补。

**角色分工**：
- **成员**：执行自己记忆文件的维护（使用 memory-maintenance-skill.md）
- **Leader**：组织、引导、质量把关（使用本文档）

---

## 维护前：状态评估

### 行数统计命令

```bash
# 统计所有成员记忆文件行数
for f in agent-team/members/*/index.md; do
  echo "$(wc -l < "$f") $f"
done | sort -rn
```

### 触发评估表

| 成员 | 行数 | 触发阈值 | 状态 |
|:-----|:-----|:---------|:-----|
| （填写） | | > 800 | 🔴/🟡/🟢 |

**状态说明**：
- 🔴 > 800 行：SHOULD 触发维护
- 🟡 600-800 行：趋势监控
- 🟢 < 600 行：健康

---

## 维护任务分派：runSubagent 提示词模板

### 模板结构

```markdown
# @<成员名> 记忆维护任务

## 任务背景

你被选为记忆维护技能书（v1.0）的执行者。你的记忆文件 `index.md` 当前有 **{LINE_COUNT} 行**，超过了 800 行的维护触发阈值。

**技能书位置**：`agent-team/wiki/memory-maintenance-skill.md`（请先阅读）

## 任务目标

按照技能书的 SOP，对你的记忆文件执行一次完整的记忆维护。

## 具体要求

### 1. 准备阶段
- 备份已由主持人完成：`git commit {BACKUP_COMMIT}`
- 阅读你的 `agent-team/members/{MEMBER_NAME}/index.md`
- 列出"不可删除清单"（Non-Negotiables）

### 2. 执行阶段
- 按四层框架分类：Identity / Insight / Index / Archive
- 执行洞见提纯：先提取再压缩
- 将过程记录压缩为索引条目
- 将详细讨论归档到 `agent-team/archive/members/{MEMBER_NAME}/{YYYY-MM}/`
- **归档按主题组织**（如 `statejournal-reviews.md`），不按日期

### 3. 目标
- 压缩后的 `index.md` 建议控制在 **450-600 行**
- 可根据实际洞见密度适当调整（允许弹性）
- 保留所有核心洞见（误解澄清、方法论、核心发现）
- 过程性讨论转为索引条目 + 归档链接

### 4. 验收阶段
- 执行冷启动测试：读完能理解"我是谁"
- 检查链接可达性
- 写入维护日志到 `agent-team/members/{MEMBER_NAME}/maintenance-log.md`

## 输出要求

完成后，请返回以下信息：

1. **维护摘要**：
   - 压缩前行数 / 压缩后行数
   - 提纯洞见数
   - 压缩块数
   - 归档块数

2. **维护日志内容**（简化版格式）

3. **反馈与建议**：
   - 技能书哪些部分很有用？
   - 哪些部分需要改进？
   - 执行中遇到的问题？

4. **冷启动测试结果**：通过/失败 + 简要说明

---

**重要**：这是实战执行，请真正修改 index.md、创建归档文件、创建维护日志，而不只是规划。
```

### 模板变量

| 变量 | 说明 | 示例 |
|:-----|:-----|:-----|
| `{MEMBER_NAME}` | 成员名称 | `Advisor-Claude` |
| `{LINE_COUNT}` | 当前行数 | `1281` |
| `{BACKUP_COMMIT}` | 备份 commit hash | `f2c99c2` |
| `{YYYY-MM}` | 归档月份 | `2025-12` |

---

## 维护流程 SOP

### 1. 评估阶段

```bash
# 1.1 统计行数
for f in agent-team/members/*/index.md; do
  echo "$(wc -l < "$f") $f"
done | sort -rn

# 1.2 确定维护范围
# 选择 > 800 行的成员
```

### 2. 备份阶段

```bash
# 2.1 Git 备份（MUST）
git add -A && git commit -m "chore: pre-maintenance snapshot for <members>"
# 记录 commit hash
```

### 3. 分派阶段

对每个需要维护的成员，使用上述模板调用 `runSubagent`。

**调用参数**：
```yaml
agentName: "<成员名>"
description: "记忆维护任务"
prompt: "<填充后的模板>"
```

### 4. 收集阶段

收集各成员的维护摘要和反馈，整理为维护报告。

### 5. 提交阶段

```bash
# 5.1 查看变更
git status

# 5.2 提交维护结果
git commit -m "feat: memory maintenance for <members>

Results:
- <member1>: X lines → Y lines (Z% compression)
- ...

QA: Cold-start tests PASSED"
```

---

## 质量把关

### Leader 复核清单

- [ ] 备份已创建
- [ ] 维护日志已写入
- [ ] 冷启动测试通过
- [ ] 归档文件按主题组织（非按日期）
- [ ] 压缩率合理（一般 40-70%）
- [ ] Git 提交信息清晰

### 常见问题处理

| 问题 | 处理 |
|:-----|:-----|
| 成员压缩过度（< 300 行） | 要求迭代补充洞见 |
| 成员压缩不足（> 700 行） | 检查是否有可归档的过程记录 |
| 归档文件命名不一致 | 统一为 `<topic>-<date-range>.md` |
| 维护日志缺失 | 要求补写 |

---

## 批量维护示例

### 场景：多成员同时维护

```bash
# 1. 评估
for f in agent-team/members/*/index.md; do
  lines=$(wc -l < "$f")
  name=$(basename $(dirname "$f"))
  if [ $lines -gt 800 ]; then
    echo "🔴 $name: $lines lines"
  elif [ $lines -gt 600 ]; then
    echo "🟡 $name: $lines lines"
  fi
done

# 2. 备份
git add -A && git commit -m "chore: pre-maintenance snapshot for batch maintenance"

# 3. 依次调用 runSubagent（不能并行，每个需要等待完成）
# ...

# 4. 提交
git add -A && git commit -m "feat: batch memory maintenance completed"
```

---

## 反馈收集模板

维护完成后，从各成员反馈中提取共性建议，用于改进技能书：

```markdown
## 维护反馈汇总（YYYY-MM-DD）

### 参与成员
- [ ] Advisor-Claude
- [ ] Advisor-GPT
- [ ] ...

### 技能书有效部分
1. ...

### 技能书改进建议
1. ...

### 待讨论问题
1. ...
```

---

## 变更日志

| 日期 | 版本 | 变更 |
|:-----|:-----|:-----|
| 2025-12-22 | v1.0 | 初版，基于 Advisor-Claude 试点维护经验 |
