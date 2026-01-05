---
docId: "W-0007-Shape"
title: "W-0007 Shape-Tier"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/wish.md"
goals:
  - "S-SCHEMA: 定义 goals/issues 的 frontmatter schema"
  - "S-OUTPUT: 定义两层输出的格式"
  - "S-ID: 定义 ID 命名规范"
---

# W-0007 Shape-Tier: API 外观与输出格式

## 1. frontmatter Schema

### 1.1 goals 字段

**格式**：字符串数组，每项为 `"ID: 描述"`

```yaml
goals:
  - "R-MOTIVATION: 阐明动机和价值"
  - "S-SCHEMA: 定义 frontmatter 格式"
  - "P-VISITOR: 实现 Visitor 扩展"
```

**字段说明**：

| 组成部分 | 格式 | 示例 |
|:---------|:-----|:-----|
| **ID** | `{Tier前缀}-{关键词}` | `R-MOTIVATION`, `S-SCHEMA` |
| **分隔符** | `: `（冒号+空格） | — |
| **描述** | 自然语言，≤50 字 | "阐明动机和价值" |

### 1.2 issues 字段

**格式**：对象数组（兼容现有）或字符串数组（新格式）

**对象格式（现有，保持兼容）**：
```yaml
issues:
  - description: "问题描述"
    status: "open"
    assignee: "负责人"
    id: "I-KEYWORD"  # 可选，新增字段
```

**字符串格式（新增）**：
```yaml
issues:
  - "R-DECISION: 需要决定聚合粒度"
  - "S-COMPAT: 现有格式兼容性"
```

**双格式支持规则**：
- 字符串格式：默认 `status: active`，无 `assignee`
- 对象格式：完整控制所有字段
- 两种格式可混用

### 1.3 ID 命名规范

**Tier 前缀**：

| 前缀 | Tier | 示例 |
|:-----|:-----|:-----|
| `R-` | Resolve | `R-MOTIVATION`, `R-DECISION` |
| `S-` | Shape | `S-SCHEMA`, `S-OUTPUT` |
| `U-` | Rule | `U-MUST`, `U-SHOULD` |
| `P-` | Plan | `P-VISITOR`, `P-PHASE1` |
| `C-` | Craft | `C-IMPL`, `C-TEST` |

> **注**：Rule 用 `U-` 避免与 Resolve 的 `R-` 冲突（U 代表 rUle）。

**关键词规范**：
- `SCREAMING-KEBAB-CASE`（全大写 + 连字符）
- 长度 1-3 个词
- 能概括核心语义

**唯一性约束**：
- **Wish 内**：active goals/issues 的 ID 不得重复
- **跨 Wish**：允许重复（不同 Wish 可有相同 ID）

---

## 2. 输出格式

### 2.1 Wish 级别输出

**路径**：`wish/W-XXXX-slug/project-status/goals.md` 和 `issues.md`

**goals.md 格式**：

```markdown
<!-- 本文档由 DocGraph 工具自动生成，手动编辑无效 -->
<!-- 再生成命令：docgraph -->

# W-XXXX Goals

## Active Goals

| ID | Tier | 描述 | 来源 |
|:---|:-----|:-----|:-----|
| R-MOTIVATION | Resolve | 阐明动机和价值 | [Resolve.md](../artifacts/Resolve.md) |
| S-SCHEMA | Shape | 定义 frontmatter 格式 | [Shape.md](../artifacts/Shape.md) |

## Resolved Goals

（已完成的目标，从 artifacts 的 resolved_goals 字段收集）
```

**issues.md 格式**：

```markdown
<!-- 本文档由 DocGraph 工具自动生成，手动编辑无效 -->

# W-XXXX Issues

## Active Issues

| ID | Tier | 描述 | 状态 | 来源 |
|:---|:-----|:-----|:-----|:-----|
| R-DECISION | Resolve | 需要决定聚合粒度 | open | [Resolve.md](../artifacts/Resolve.md) |

## Resolved Issues

（已解决的问题，从 artifacts 的 resolved_issues 字段收集）
```

### 2.2 全局级别输出

**路径**：`docs/goals.gen.md` 和 `docs/issues.gen.md`

**格式**：按 Wish 分组的汇总表

```markdown
<!-- 本文档由 DocGraph 工具自动生成，手动编辑无效 -->

# 全局 Goals 汇总

## 统计概览

- 总目标数：N
- Active：X
- Resolved：Y

## W-0001 Wish 系统自举

| ID | Tier | 描述 | 来源 |
|:---|:-----|:-----|:-----|
| ... | ... | ... | ... |

## W-0007 DocGraph Goals/Issues 聚合

| ID | Tier | 描述 | 来源 |
|:---|:-----|:-----|:-----|
| ... | ... | ... | ... |
```

---

## 3. 状态追踪

### 3.1 Active vs Resolved

**区分方式**：使用独立字段

```yaml
# Active goals/issues
goals:
  - "S-SCHEMA: 定义 frontmatter 格式"
issues:
  - "R-DECISION: 需要决定聚合粒度"

# Resolved（可选，用于归档）
resolved_goals:
  - "R-MOTIVATION: 阐明动机和价值"
resolved_issues:
  - "R-SCOPE: 聚合粒度已决策"
```

**规则**：
- `goals` / `issues` 字段：当前 active 的条目
- `resolved_goals` / `resolved_issues`：已完成/已解决的条目
- 生成时分别聚合到 Active 和 Resolved 区域

### 3.2 状态迁移

手动维护：完成时从 `goals` 移动到 `resolved_goals`。

---

## 4. 与 snapshot.md 的集成

**snapshot.md 的 focus 字段**：

```yaml
focus:
  kind: "Goal"  # Goal | Issue
  id: "S-SCHEMA"  # 引用 goals/issues 中的 ID
  tier: "Shape"
```

**约束**：`focus.id` 必须存在于当前 Wish 的 active goals 或 issues 中。

---

## 5. 验收标准

- [ ] frontmatter 解析支持新的 goals 字段
- [ ] frontmatter 解析支持 issues 的字符串格式（双格式兼容）
- [ ] 生成 Wish 级别的 goals.md 和 issues.md
- [ ] 生成全局级别的 goals.gen.md 和 issues.gen.md
- [ ] resolved_goals / resolved_issues 归档到 Resolved 区域

---

**状态**：🟢 完成
**更新**：2026-01-05
