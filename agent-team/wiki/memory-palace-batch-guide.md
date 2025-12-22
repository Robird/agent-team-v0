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
  
  按照你的工作流程：
  1. 读取 inbox.md
  2. 解析每条便签
  3. 对每条执行 CLASSIFY → ROUTE → APPLY
  4. 清空 inbox 并归档（可追加到 inbox-archive.md）
  5. 更新 index.md 的"最后更新"区块
  6. 输出处理报告

  请开始处理。
```

### Step 3: 汇总报告

所有成员处理完成后，汇总报告给监护人：

```markdown
## 记忆宫殿批量维护报告

**执行时间**：YYYY-MM-DD
**处理成员**：N 人

| 成员 | 便签数 | 处理结果 |
|:-----|:-------|:---------|
| ... | ... | ... |

**总计**：处理了 X 条便签
```

---

## 成员目录列表

| 成员 | 目录路径 |
|:-----|:---------|
| Advisor-Claude | `agent-team/members/Advisor-Claude/` |
| Advisor-Gemini | `agent-team/members/Advisor-Gemini/` |
| Advisor-GPT | `agent-team/members/Advisor-GPT/` |
| Implementer | `agent-team/members/implementer/` |
| Investigator | `agent-team/members/investigator/` |
| QA | `agent-team/members/qa/` |
| DocOps | `agent-team/members/docops/` |
| CodexReviewer | `agent-team/members/codex-reviewer/` |
| TeamLeader | `agent-team/members/TeamLeader/` |

---

## 注意事项

1. **按需处理**：只处理有待处理便签的成员，空 inbox 跳过
2. **串行执行**：一次处理一个成员，避免并发冲突
3. **Git 提交**：每批处理完成后，统一 git commit
