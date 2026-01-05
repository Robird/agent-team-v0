---
docId: "snapshot-template"
title: "Snapshot 文档模板"
produce_by:
  - "wish/W-0001-wish-bootstrap/wish.md"
---

# Snapshot 文档模板

> **定位**：snapshot.md 是"执行寄存器"，存储当前执行状态，让 Team Leader 每次"醒来"能直接续上。
>
> **使用方法**：复制下面的模板内容到 `wish/W-XXXX-<slug>/project-status/snapshot.md`。

## 模板内容

```yaml
---
docId: "W-XXXX-snapshot"
title: "W-XXXX Snapshot"
produce_by:
  - "wish/W-XXXX-<slug>/wish.md"

# === 执行寄存器（YAML frontmatter）===
snapshotVersion: "0.1"
updated: "YYYY-MM-DD"

# 当前焦点（chooseFocus 的结果）
focus:
  kind: "Goal"  # Goal | Issue | Meta
  id: "G-001"   # 目标或问题的稳定标识（或文件锚点链接）
  tier: "Plan"  # Resolve | Shape | Rule | Plan | Craft

# 下一步需求（identifyDemand 的结果）
next:
  action: "Implement"           # Investigate | Test | Clarify | Propose | Implement | Review | DocOps
  deliverable: "Patch"          # FixList | Patch | DecisionLog | TradeoffTable | TestResult | Brief
  definitionOfDone: "[完成判据，1-3 行]"
  stopCondition: "[停止条件，避免无限扩写]"

# 下一位贡献者（matchContributor 的结果）
assignee: "Implementer"  # Investigator | Implementer | QA | DocOps | Craftsman | Curator

# 阻塞状态（若有）
blockers: []
# blockers:
#   - reason: "[阻塞原因]"
#     wake: "[唤醒条件，可判定]"
needsGuardian: false
---

# W-XXXX Snapshot

## Context (人类可读的上下文)

- 当前状态：[简要描述]
- 最近进展：[最近一次循环发生了什么]

## Pointers (指向 SSOT)

- Goals: [./goals.md](./goals.md)
- Issues: [./issues.md](./issues.md)
- Last jam log: [若有畅谈会记录，链接到此]

## Recent Outputs (最近产出，可选)

- [产物链接 1]
- [产物链接 2]
```

## 字段说明

| 字段 | 用途 | 更新时机 |
|:-----|:-----|:---------|
| `focus` | 本轮唯一焦点 | chooseFocus 变化时 |
| `next` | 下一步需求（可执行且可终止） | identifyDemand 变化时 |
| `assignee` | 下一位贡献者 | matchContributor 变化时 |
| `blockers` | 阻塞原因与唤醒条件 | 进入/退出阻塞时 |
| `needsGuardian` | 是否需要监护人输入 | 需要外部决策时 |

## 更新规则

**MUST 更新的时机**（任一命中就更新）：
- 焦点变化：`focus` 变了
- 下一步指令变化：`next` 的任一字段变化
- 分派变化：`assignee` 变化
- 进入阻塞：出现 `blockers` 或 `needsGuardian==true`
- 畅谈会/评审会结束

**SHOULD 更新**：
- 产物落地后，把链接补到 "Recent Outputs" 小节

## 与 goals.md / issues.md 的关系

- **goals.md**：目标的 SSOT（结构与完成判据）
- **issues.md**：问题的 SSOT（阻塞与解决方案）
- **snapshot.md**：执行寄存器（只保存"当前指针 + 下一条指令"），用 ID/链接指回 SSOT
