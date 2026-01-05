---
docId: "W-0007-Brief-Feasibility"
title: "W-0007 技术可行性评估报告"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Resolve.md"
author: "Investigator"
created: "2026-01-05"
---

# W-0007 技术可行性评估报告

> **调查人**：Investigator
> **日期**：2026-01-05
> **任务**：评估 DocGraph Goals/Issues 两层聚合方案的技术可行性

---

## 1. 现有实现分析

### 1.1 IssueAggregator 结构

**关键特征**：
- **单输出路径**：`OutputPath` 是属性而非方法，硬编码返回 `docs/issues.gen.md`
- **按文件分组**：已有 `GroupBy(i => i.SourceDocument)` 逻辑
- **Issue 数据结构**：私有 `Issue` 类，含 `Description`, `Status`, `Assignee`, `SourceDocument` 四字段
- **无 ID 字段**：当前 `Issue` 类没有 `id` 字段

### 1.2 接口约束

- `OutputPath` 是单值属性，不支持多输出
- `Generate()` 返回 `string`，不支持多文件输出

### 1.3 Wish 感知

| 属性 | 类型 | 说明 |
|:-----|:-----|:-----|
| `FilePath` | string | workspace 相对路径 |
| `Type` | DocumentType | Wish 或 Product |
| `ProducedBy` | List\<DocumentNode\> | 父 Wish 节点列表 |

**Wish 推导方式**：对于 Product，`node.ProducedBy[0]` 获取所属 Wish。

---

## 2. 实现路径对比

### 方案 A：单 Visitor 多输出（推荐）

**思路**：扩展接口支持多输出路径，新增 `GenerateMultiple()` 默认方法。

| 维度 | 评估 |
|:-----|:-----|
| **改动范围** | 接口扩展（Breaking Change 风险低，默认返回 null） |
| **复用性** | 高，复用现有 Issue 解析逻辑 |
| **复杂度** | 中，需要同时维护单输出和多输出逻辑 |
| **运行时开销** | 低，单次遍历生成所有输出 |

### 方案 B：拆分为两个 Visitor

**问题**：`WishIssueAggregator` 需要为每个 Wish 生成一个文件，但接口只支持单输出路径。

### 方案 C：生成后处理

**不推荐原因**：违反 DRY 原则，增加脆弱性。

---

## 3. 推荐方案：方案 A

**理由**：
1. **最小改动**：接口扩展向后兼容，现有 Visitor 无需修改
2. **单次遍历**：性能最优，一次 `ForEachDocument` 收集所有数据
3. **复用最大化**：现有 `ExtractIssues()` 完全复用

---

## 4. 代码改动清单

### Phase 1：接口扩展（~25 行，0.5h）

| 文件 | 改动点 |
|:-----|:-------|
| `IDocumentGraphVisitor.cs` | 新增 `GenerateMultiple()` 默认实现 |
| `RunCommand.cs` | 修改 Visitor 执行逻辑支持多输出 |

### Phase 2：Issue 扩展（~52 行，1h）

| 文件 | 改动点 |
|:-----|:-------|
| `IssueAggregator.cs` | `Issue` 类新增 `Id` 字段 |
| `IssueAggregator.cs` | `ExtractIssues()` 解析 `id` 字段 |
| `IssueAggregator.cs` | 实现 `GenerateMultiple()` |
| `GlossaryVisitor.cs` | `KnownFrontmatterFields` 新增 `Goals` |

### Phase 3：Goals 聚合（~81 行，1h）

| 文件 | 改动点 |
|:-----|:-------|
| `GoalAggregator.cs` | 新建，结构类似 IssueAggregator |
| `RunCommand.cs` | 注册 GoalAggregator |

### 总预估

| 阶段 | 行数 | 预估工时 |
|:-----|:-----|:---------|
| Phase 1 | ~25 | 0.5h |
| Phase 2 | ~52 | 1h |
| Phase 3 | ~81 | 1h |
| **合计** | **~158** | **2.5h** |

---

## 5. 风险与注意事项

| 风险 | 影响 | 缓解措施 |
|:-----|:-----|:---------|
| `GenerateMultiple()` 返回路径冲突 | 文件覆盖 | 在 RunCommand 中检测并报错 |
| 产物跨多个 Wish | 分类歧义 | 第一个 ProducedBy 为主 |
| 空 Wish（无 issue/goal） | 空文件 | 生成 "无记录" 占位内容 |

---

## 附录：关键代码锚点

| 概念 | 位置 |
|:-----|:-----|
| Visitor 注册入口 | `RunCommand.cs#L147-L153` |
| Issue 解析逻辑 | `IssueAggregator.cs#L78-L100` |
| Frontmatter 字段常量 | `GlossaryVisitor.cs#L93-L103` |
| DocumentNode 属性 | `DocumentNode.cs#L10-L50` |
