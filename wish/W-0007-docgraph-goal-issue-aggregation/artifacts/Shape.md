---
docId: "W-0007-Shape"
title: "W-0007 Shape-Tier"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/wish.md"
goals:
  - id: "S-SCHEMA"
    description: "定义 goals/issues 的 frontmatter schema"
  - id: "S-OUTPUT"
    description: "定义两层输出的格式"
  - id: "S-ID"
    description: "定义 ID 命名规范"
issues:
  - id: "S-FORMAT"
    description: "输出格式从表格改为 heading + 子弹列表"
    status: "resolved"
  - id: "S-PATH"
    description: "全局聚合路径统一到 wish-panels/"
    status: "resolved"
  - id: "S-EXT"
    description: "生成文件统一 .gen.md 扩展名"
    status: "resolved"
  - id: "S-DUAL-FORMAT"
    description: "移除字符串格式支持，统一对象格式"
    status: "resolved"
---

# W-0007 Shape-Tier: API 外观与输出格式

## 1. frontmatter Schema

### 1.1 goals 字段

**格式**：对象数组（**ID 必填**）

```yaml
goals:
  - id: "R-MOTIVATION"
    description: "阐明动机和价值"
  - id: "S-SCHEMA"
    description: "定义 frontmatter 格式"
```

**字段说明**：

| 字段 | 格式 | 必填 | 示例 |
|:-----|:-----|:-----|:-----|
| `id` | `{Tier前缀}-{关键词}` | ✅ | `R-MOTIVATION`, `S-SCHEMA` |
| `description` | 自然语言，≤50 字 | ✅ | "阐明动机和价值" |

### 1.2 issues 字段

**格式**：对象数组（**ID 必填**）

```yaml
issues:
  - id: "R-DECISION"
    description: "需要决定聚合粒度"
    status: "open"  # 可选，默认 "open"
  - id: "S-FORMAT"
    description: "输出格式已统一"
    status: "resolved"
```

**字段说明**：

| 字段 | 格式 | 必填 | 默认值 | 示例 |
|:-----|:-----|:-----|:-------|:-----|
| `id` | `{Tier前缀}-{关键词}` | ✅ | — | `R-MOTIVATION` |
| `description` | 自然语言，≤50 字 | ✅ | — | "阐明动机和价值" |
| `status` | `"open"` \| `"resolved"` | ❌ | `"open"` | `"resolved"` |

> **注**：goals 字段格式相同，也支持 status 属性。

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

### 2.1 统一格式：Heading + 子弹列表

所有聚合输出（Goals、Issues）使用统一格式：

- **按源文件分组**：用 heading 显示文件路径
- **子弹列表**：`- ID: 描述`

这种格式的优点：
1. **统一**：全局和 Wish 级别使用相同模式
2. **灵活**：列表项可增删，不像表格要求列数一致
3. **直观**：路径作为 heading，一眼看到条目来源

### 2.2 Wish 级别输出

**路径**：`wish/W-XXXX-slug/project-status/goals.gen.md` 和 `issues.gen.md`

> **注**：使用 `.gen.md` 扩展名，表明由工具生成，防止误编辑。

**格式示例**：

```markdown
<!-- 本文档由 DocGraph 工具自动生成，手动编辑无效 -->
<!-- 再生成命令：docgraph -->

# W-XXXX Goals

## Active Goals

### `artifacts/Resolve.md`

- R-MOTIVATION: 阐明动机和价值

### `artifacts/Shape.md`

- S-SCHEMA: 定义 frontmatter 格式
- S-OUTPUT: 定义两层输出的格式

## Resolved Goals

### `artifacts/Resolve.md`

- R-DECISION: 聚合粒度已决策
```

### 2.3 全局级别输出

**路径**：`wish-panels/goals.gen.md` 和 `wish-panels/issues.gen.md`

> **注**：全局聚合统一输出到 `wish-panels/` 目录，与 `reachable-documents.gen.md` 等全局视图文件保持一致。

**格式示例**：

```markdown
<!-- 本文档由 DocGraph 工具自动生成，手动编辑无效 -->
<!-- 再生成命令：docgraph -->

# 目标汇总

## 统计概览

- 总目标数：N
- Active：X
- Resolved：Y

## `wish/W-0007-docgraph/artifacts/Shape.md`

- S-SCHEMA: 定义 frontmatter 格式
- S-OUTPUT: 定义两层输出的格式

## `wish/W-0007-docgraph/artifacts/Plan.md`

- P-PHASE1: 规划 Phase 1 接口扩展
```

---

## 3. 状态追踪与聚合策略

### 3.1 状态值

使用 `status` 字段标记条目状态：

| 值 | 含义 | 默认 |
|:---|:-----|:-----|
| `"open"` | 尚未完成/解决 | ✅ |
| `"resolved"` | 已完成/已解决 | — |

**状态迁移**：完成时修改 `status: "open"` → `status: "resolved"`。

### 3.2 聚合策略

**全局聚合** (`wish-panels/*.gen.md`)：
- **只显示 Active**：默认只渲染 `status != "resolved"` 的条目
- **统计概览**：显示 Active 和 Resolved 数量
- **目的**：快速知道还有哪些事情要做，获得聚焦

**Wish 级别聚合** (`project-status/*.gen.md`)：
- **分区显示**：Active 和 Resolved 分别列出
- **目的**：项目内部回顾，查看进展历史

---

## 4. 实现架构

### 4.1 TwoTierAggregatorBase 基类

所有两级聚合器继承自 `TwoTierAggregatorBase<TItem>`，共享：
- 两级输出逻辑（全局 + Wish 级别）
- 按源文件分组的输出格式
- Active/Resolved 分区

### 4.2 继承关系

```
TwoTierAggregatorBase<TItem>
├── IssueAggregator (TItem = Issue)
└── GoalAggregator (TItem = Goal)
```

---

## 5. 验收标准

- [x] frontmatter 解析支持 goals 字段（ID 必填）
- [x] frontmatter 解析支持 issues 字段（ID 必填）
- [x] 生成 Wish 级别的 goals.md 和 issues.md
- [x] 生成全局级别的 goals.gen.md 和 issues.gen.md
- [x] resolved_goals / resolved_issues 归档到 Resolved 区域
- [x] 输出格式统一为 heading + 子弹列表

---

**状态**：🟢 完成
**更新**：2026-01-05（格式重构）
