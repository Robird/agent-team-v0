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
  4. 输出处理报告

  请开始处理。
```

> **MemoryPalaceKeeper 工作流程已优化**（v2，2025-12-27）：
> - 伪代码风格定义流程，执行更高效
> - **Git-as-Archive**：Git 历史即归档，无需 inbox-archive.md
> - 每条便签处理后输出 checkpoint，便于追踪进度
> - 终端命令一次性完成 inbox 重置和 Git 提交

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
| 2025-12-27 | v2: 更新 prompt 适配伪代码工作流程；移除 inbox-archive.md 引用（Git-as-Archive）|
| 2025-12-26 | v1.1: 添加 MemoryPalaceKeeper 到成员列表 |
| 2025-12-26 | v1.0: 初版 |
