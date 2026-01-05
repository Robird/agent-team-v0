---
docId: "W-0007-Resolve"
title: "W-0007 Resolve-Tier"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/wish.md"
produce:
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/briefs/architecture-brief.md"
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/briefs/feasibility-report.md"
goals:
  - "G-SCHEMA: 设计 goals/issues 的 frontmatter schema"
issues:
  - "I-ID-DESIGN: ID 机制设计待决策"
  - "I-EXISTING-ISSUES: 现有 issues 字段格式兼容性"
---

# W-0007 Resolve-Tier: 动机与方案探索

## 1. 动机（Why）

### 1.1 当前痛点

- **手动维护成本**：goals.md 和 issues.md 需要手动维护，容易与实际产物脱节
- **就地产生，分散记录**：目标和问题往往在各层级 artifacts 中产生（Resolve 里的"为什么要做"、Plan 里的"技术阻塞"、Craft 里的"实现问题"）
- **漂移风险**：多处记录同一事实，SSOT 不明确

### 1.2 期望状态

- **就地记录**：在 artifacts 文档的 frontmatter 中声明 goals/issues
- **自动聚合**：DocGraph 扫描并生成 project-status/goals.md 和 issues.md
- **单一来源**：artifacts 是 SSOT，project-status 是派生视图

### 1.3 附加价值

本 Wish 同时作为 **Wish 推进模式的演练场**——验证 snapshot.md 执行寄存器的实际效果。

## 2. 候选方案

### 2.1 方案 A：扩展现有 Visitor（推荐）

借鉴现有的 `produce`/`produce_by` 聚合逻辑，新增 `goals`/`issues` 字段的 Visitor。

**优点**：
- 与现有架构一致
- 复用已有的 frontmatter 解析逻辑
- 渐进式扩展，风险低

**缺点**：
- 需要理解现有 Visitor 模式

### 2.2 方案 B：新增独立命令

为 goals/issues 聚合创建完全独立的命令和代码路径。

**优点**：
- 隔离性好，不影响现有功能

**缺点**：
- 代码重复
- 维护两套聚合逻辑

### 2.3 方案选择

**倾向方案 A**：扩展现有 Visitor。理由：一致性、复用、渐进式。

## 3. ID 机制设计

### 3.1 设计原则

借鉴 `atelia/docs/spec-conventions.md` 中的**稳定语义锚点（Stable Semantic Anchors）**思想：

- **语义哈希**：ID 名能概括核心语义，作为"内容摘要"
- **稳定性**：ID 一旦创建，不因位置移动而改变
- **可重用**：已解决的 issue 归档后，ID 可被新内容重用

### 3.2 ID 格式

| 类型 | 前缀 | 格式 | 示例 |
|:-----|:-----|:-----|:-----|
| Goal | `G-` | `G-KEYWORD` | `G-SCHEMA`, `G-VISITOR` |
| Issue | `I-` | `I-KEYWORD` | `I-ID-DESIGN`, `I-COMPAT` |

**命名规则**：
- 使用 `SCREAMING-KEBAB-CASE`（全大写 + 连字符）
- 长度控制在 2-4 个词
- 能概括核心语义

### 3.3 生命周期

```
Active → Resolved → Archived
```

- **Active**：显示在 goals.md / issues.md 主区域
- **Resolved**：标记为完成，移入 "Resolved" 区域
- **Archived**：从生成文件中移除（但源文件中保留历史）

## 4. frontmatter Schema 草案

### 4.1 goals 字段

```yaml
goals:
  - "G-KEYWORD: 一句话描述"
  - "G-ANOTHER: 另一个目标"
```

### 4.2 issues 字段（兼容现有）

现有格式（保持兼容）：
```yaml
issues:
  - "I-KEYWORD: 一句话描述"
```

扩展格式（可选，用于状态标记）：
```yaml
issues:
  - id: "I-KEYWORD"
    summary: "一句话描述"
    status: "active"  # active | resolved
```

**决策**：MVP 先用简单字符串格式，后续按需扩展。

## 5. 架构调研结果（2026-01-05 Investigator Brief）

> 详见：[W-0007-docgraph-architecture-brief.md](../../../agent-team/handoffs/W-0007-docgraph-architecture-brief.md)

### 5.1 关键发现

**IssueAggregator 已存在！** 现有实现位于 `atelia/src/DocGraph/Visitors/IssueAggregator.cs`。

| 维度 | 现有 IssueAggregator | W-0007 初始设计 |
|:-----|:---------------------|:----------------|
| **格式** | 对象数组 `[{description, status, assignee}]` | 字符串数组 `["I-ID: 描述"]` |
| **ID 机制** | 无 ID | 支持 `I-KEYWORD` 前缀 |
| **输出路径** | `docs/issues.gen.md` | `project-status/issues.md` |

### 5.2 Visitor 扩展点

- **接口**：`IDocumentGraphVisitor`（Name, OutputPath, RequiredFields, Generate）
- **注册点**：`RunCommand.GetVisitors()` 返回 Visitor 列表
- **现有 Visitor**：GlossaryVisitor、IssueAggregator、ReachableDocumentsVisitor

### 5.3 设计调整选项

| 选项 | 描述 | 优缺点 |
|:-----|:-----|:-------|
| **A. 双格式支持** | 扩展 IssueAggregator 同时支持字符串和对象格式 | ✅ 渐进式，兼容现有 |
| **B. 并行聚合器** | 新建专门处理字符串格式的聚合器 | ✅ 隔离，❌ 代码重复 |
| **C. 统一迁移** | 全部迁移到字符串格式 | ❌ 破坏性变更 |

**倾向选项 A**：双格式支持，渐进式迁移。

### 5.4 输出路径决策（待监护人确认）

| 选项 | 路径 | 理由 |
|:-----|:-----|:-----|
| **保持现状** | `docs/goals.gen.md`, `docs/issues.gen.md` | 与现有 glossary 一致 |
| **迁移到 Wish 内** | `project-status/goals.md`, `project-status/issues.md` | 符合 Wish 实例目录设计 |

**问题**：Visitor 当前是全局聚合（docs/），而非 Wish 级别聚合。若要 Wish 内聚合，需要扩展 Visitor 接口或新增 Wish 级别生成命令。

## 6. 监护人决策（2026-01-05）

### 6.1 聚合粒度决策

**选择：C. 两层都做**

| 层级 | 输出位置 | 价值 |
|:-----|:---------|:-----|
| **Wish 级别** | `project-status/goals.md`, `issues.md` | 专注具体问题，不加载无关信息 |
| **全局级别** | `docs/goals.gen.md`, `issues.gen.md` | 跨 Wish 耦合时的高层视野 |

### 6.2 ID 机制决策

- **全局唯一性**：不要求（允许不同 Wish 有相同 ID）
- **Wish 内唯一性**：仅要求同一 Wish 内的 **active** goals/issues 不重复
- **降低碰撞**：用字母前缀区分 Tier（如 `R-xxx` Resolve, `S-xxx` Shape, `P-xxx` Plan, `C-xxx` Craft）

### 6.3 实现思路

1. **扩展现有 issue 格式**：增加 `id` 字段（可选，向后兼容）
2. **复用解析逻辑**：相同格式，不同聚合粒度
3. **分层输出**：用 LINQ GroupBy 按 Wish 分组，生成两级输出

### 6.4 价值分析（监护人原话）

> 按 wish 聚合使我们在处理具体问题时更专注，不用加载其他 wish 里的信息。
> 而全局聚合，使我们在遇到真的跨 wish 耦合时（比如一个阻塞了另一个），有一个全局视野，可以进行高层次分析。

## 7. 待讨论问题（更新后）

1. ~~**[I-ID-DESIGN]**~~ ✅ 语义锚点 + Tier 前缀
2. ~~**[I-OUTPUT-PATH]**~~ ✅ 两层都做
3. ~~**[I-WISH-SCOPE]**~~ ✅ 两层都做
4. **[I-EXISTING-ISSUES]** 🟡 现有 issue 格式如何扩展 id 字段

## 8. 下一步

- [x] 调研现有 DocGraph Visitor 架构 ✅
- [x] 监护人决策聚合粒度 ✅
- [ ] **技术可行性评估**：验证两层输出的实现复杂度
- [ ] 进入 Shape-Tier：定义 frontmatter schema 和输出格式

---

**状态**：🟡 进行中（技术评估中）
**更新**：2026-01-05
