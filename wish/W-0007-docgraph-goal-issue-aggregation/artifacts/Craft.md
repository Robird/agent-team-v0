---
docId: "W-0007-Craft"
title: "W-0007 Craft-Tier"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/wish.md"
goals:
  - id: "C-PHASE1"
    description: "实现 Phase 1 接口扩展"
  - id: "C-PHASE2"
    description: "实现 Phase 2 Issue 扩展"
  - id: "C-PHASE3"
    description: "实现 Phase 3 Goals 聚合"
---

# W-0007 Craft-Tier: 实现记录

## 1. Phase 1: 接口扩展

### 1.1 任务清单

| 任务 | 状态 | 执行者 |
|:-----|:-----|:-------|
| 实现 `GenerateMultiple()` 接口扩展 | ✅ 完成 | Implementer |
| Code Review | ✅ LGTM | Craftsman |
| 测试验证 | ✅ 99 tests passed | — |

### 1.2 实现记录

**改动文件**：
- `Visitors/IDocumentGraphVisitor.cs` — 新增 `GenerateMultiple()` 默认方法 + XML doc
- `Commands/RunCommand.cs` — 新增 `OutputPreflight` 类，支持多输出 + 安全校验

**关键特性**：
- 路径冲突检测（HashSet 去重）
- 输出路径安全校验（拒绝绝对路径、路径穿越）
- 空 Dictionary 等价 null（回退单输出）
- Key/OutputPath 空白校验

---

## 2. Phase 2: Issue 扩展

### 2.1 任务清单

| 任务 | 状态 | 执行者 |
|:-----|:-----|:-------|
| 扩展 Issue 类和解析逻辑 | ✅ 完成 | Implementer |
| 实现两层输出 | ✅ 完成 | Implementer |
| Code Review | ✅ LGTM | Craftsman |
| 测试验证 | ✅ 99 tests passed | — |

### 2.2 实现记录

**改动文件**：
- `Visitors/IssueAggregator.cs` — 全面重写

**关键特性**：
- Issue 类新增 `Id`, `SourceNode` 字段
- 双格式解析：字符串 `"X-ID: 描述"` + 对象 `{description, status, id?}`
- 两层输出：`docs/issues.gen.md` + `wish/W-XXXX/project-status/issues.md`
- 支持 `resolved_issues` 字段归档
- Wish 归属：ProducedBy 优先，路径推导兜底
- Tier 识别：R/S/U/P/C 前缀映射

---

## 3. Phase 3: Goals 聚合

### 3.1 任务清单

| 任务 | 状态 | 执行者 |
|:-----|:-----|:-------|
| 新建 GoalAggregator | ✅ 完成 | Implementer |
| Code Review | ✅ LGTM | Craftsman |
| 测试验证 | ✅ 99 tests passed | — |

### 3.2 实现记录

**新建文件**：
- `Visitors/GoalAggregator.cs` — Goals 聚合器

**修改文件**：
- `Commands/RunCommand.cs` — 注册新 Visitor
- `Visitors/GlossaryVisitor.cs` — `KnownFrontmatterFields` 新增 `Goals`
- `Visitors/IssueAggregator.cs` — 全局分组标题加 Wish title + 字段转义

**关键特性**：
- Goal 类：`Id`, `Description`, `SourceNode`
- 字符串格式解析：`"X-ID: 描述"`
- 两层输出：`docs/goals.gen.md` + `wish/W-XXXX/project-status/goals.md`
- 支持 `resolved_goals` 字段归档
- Markdown 表格安全转义

---

## 4. Phase 4: 架构重构

### 4.1 任务清单

| 任务 | 状态 | 执行者 |
|:-----|:-----|:-------|
| 抽取 TwoTierAggregatorBase 基类 | ✅ 完成 | Implementer |
| 统一输出格式（heading + 子弹列表） | ✅ 完成 | Implementer |
| Issue ID 必填 | ✅ 完成 | Implementer |
| 迁移旧 issues 条目 | ✅ 完成 | TeamLeader |
| 更新 Shape.md | ✅ 完成 | TeamLeader |
| 测试验证 | ✅ 349 tests passed | — |

### 4.2 实现记录

**新建文件**：
- `Visitors/TwoTierAggregatorBase.cs` — 泛型基类

**重构文件**：
- `Visitors/IssueAggregator.cs` — 继承基类，ID 必填
- `Visitors/GoalAggregator.cs` — 继承基类，统一格式

**设计亮点**：
- 共享的两级输出逻辑（全局 + Wish 级别）
- 统一的 heading + 子弹列表格式
- ID 必填，简化代码分支
- 基类处理 Wish 归属判断、输出生成

---

## 5. Phase 5: 格式简化与状态管理

### 5.1 任务清单

| 任务 | 状态 | 执行者 |
|:-----|:-----|:-------|
| 移除字符串格式支持 | ✅ 完成 | Implementer |
| 统一对象格式 | ✅ 完成 | Implementer |
| 迁移到 status 属性设计 | ✅ 完成 | Implementer |
| 全局聚合只显示 Active | ✅ 完成 | Implementer |
| 定义 produce 语义约束 | ✅ 完成 | Craftsman + TeamLeader |
| 从 produce 中移除 .gen.md | ✅ 完成 | Implementer |
| 更新文档 | ✅ 完成 | TeamLeader |

### 5.2 实现记录

**格式简化**：
- 决策理由：刚起步，数据量少，窗口期最佳；双格式维护复杂，已引发问题
- 变更：移除字符串格式，只保留对象格式 `{id, description}`

**状态管理重构**：
- **之前**：`issues` + `resolved_issues` 两个独立字段
- **之后**：单一 `issues` 字段 + `status` 属性（默认 "open"）
- **理由**：
  - 易用性：修改状态只改一个属性，不用搬运整个条目
  - 可扩展性：天然支持 `blocked`/`in-progress` 等其他状态
  - 聚焦：全局聚合默认只显示 active，消除干扰

**聚合策略**：
- 全局聚合：只显示 Active 条目（`status != "resolved"`）
- Wish 级别：保留 Active/Resolved 分区（用于项目回顾）

**produce 语义约束**（[R-PRODUCE-001]）：
- **决策**：派生视图文件（`.gen.md`）不应记录在 Wish 的 `produce` 中
- **理由**：
  - 可再生，不是设计/实现本体
  - 避免验证噪声和空文件创建副作用
  - `produce` 应表达"需要人工维护"的产物
- **实施**：从 7 个 Wish 移除了 `.gen.md` 的 produce 声明
- **固化**：在 Rule.md 中记录条款 [R-PRODUCE-001]

---

**状态**：🟢 全部完成
**更新**：2026-01-05（Phase 5 格式简化、状态管理与 produce 语义约束）
