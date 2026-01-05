---
docId: "W-0007-Plan"
title: "W-0007 Plan-Tier"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/wish.md"
goals:
  - id: "P-PHASE1"
    description: "规划 Phase 1 接口扩展"
  - id: "P-PHASE2"
    description: "规划 Phase 2 Issue 扩展"
  - id: "P-PHASE3"
    description: "规划 Phase 3 Goals 聚合"
---

# W-0007 Plan-Tier: 实现计划

## 1. 总体规划

### 1.1 阶段划分

| 阶段 | 内容 | 预估工时 | 依赖 |
|:-----|:-----|:---------|:-----|
| **Phase 1** | 接口扩展（`GenerateMultiple()`） | 0.5h | 无 |
| **Phase 2** | Issue 扩展（id 字段 + 两层输出） | 1h | Phase 1 |
| **Phase 3** | Goals 聚合（新建 GoalAggregator） | 1h | Phase 1 |

**总预估**：2.5h

### 1.2 风险评估

| 风险 | 概率 | 影响 | 缓解措施 |
|:-----|:-----|:-----|:---------|
| 接口扩展破坏现有 Visitor | 低 | 高 | 默认实现返回 null，向后兼容 |
| 双格式解析遗漏边界情况 | 中 | 中 | 补充测试用例覆盖空数组、null 等 |
| Wish 归属判断错误 | 低 | 中 | 使用 ProducedBy 关系而非路径推导 |

---

## 2. Phase 1: 接口扩展

**目标**：让 Visitor 支持生成多个文件

### 2.1 改动清单

| 文件 | 改动 | 行数 |
|:-----|:-----|:-----|
| `Visitors/IDocumentGraphVisitor.cs` | 新增 `GenerateMultiple()` 默认方法 | +5 |
| `Commands/RunCommand.cs` | 修改执行逻辑支持多输出 | +20 |

### 2.2 接口设计

```csharp
public interface IDocumentGraphVisitor
{
    // 原有接口保持不变
    string Name { get; }
    string OutputPath { get; }
    IReadOnlyList<string> RequiredFields { get; }
    string Generate(DocumentGraph graph);
    
    // 新增：多输出支持（默认返回 null，表示使用单输出）
    IReadOnlyDictionary<string, string>? GenerateMultiple(DocumentGraph graph) => null;
}
```

### 2.3 RunCommand 修改

```csharp
foreach (var visitor in visitors)
{
    var multiOutput = visitor.GenerateMultiple(graph);
    if (multiOutput != null)
    {
        foreach (var (path, content) in multiOutput)
        {
            await WriteFileAsync(path, content);
        }
    }
    else
    {
        await WriteFileAsync(visitor.OutputPath, visitor.Generate(graph));
    }
}
```

### 2.4 验收标准

- [ ] 现有 Visitor 无需修改即可继续工作
- [ ] 新接口默认返回 null
- [ ] 多输出路径冲突时报错

---

## 3. Phase 2: Issue 扩展

**目标**：支持 id 字段 + 两层输出

### 3.1 改动清单

| 文件 | 改动 | 行数 |
|:-----|:-----|:-----|
| `Visitors/IssueAggregator.cs` | `Issue` 类新增 `Id` 字段 | +3 |
| `Visitors/IssueAggregator.cs` | `ExtractIssues()` 解析字符串格式 | +15 |
| `Visitors/IssueAggregator.cs` | 实现 `GenerateMultiple()` | +30 |
| `Visitors/GlossaryVisitor.cs` | `KnownFrontmatterFields` 新增 `Goals` | +1 |

### 3.2 Issue 类扩展

```csharp
private class Issue
{
    public string? Id { get; set; }  // 新增，可选
    public string Description { get; set; } = "";
    public string Status { get; set; } = "open";
    public string? Assignee { get; set; }
    public string SourceDocument { get; set; } = "";
    public DocumentNode? SourceNode { get; set; }  // 新增，用于 Wish 归属
}
```

### 3.3 双格式解析

```csharp
private static List<Issue> ExtractIssues(DocumentNode node, object issuesObj)
{
    var result = new List<Issue>();
    
    if (issuesObj is not IEnumerable<object> issuesList)
        return result;
    
    foreach (var item in issuesList)
    {
        if (item is string issueString)
        {
            // 字符串格式: "I-ID: 描述"
            var match = Regex.Match(issueString, @"^([A-Z]-[A-Z0-9-]+):\s*(.+)$");
            if (match.Success)
            {
                result.Add(new Issue
                {
                    Id = match.Groups[1].Value,
                    Description = match.Groups[2].Value,
                    SourceDocument = node.FilePath,
                    SourceNode = node
                });
            }
        }
        else if (item is IDictionary<object, object> dict)
        {
            // 对象格式: {description, status, assignee, id?}
            result.Add(new Issue
            {
                Id = dict.TryGetValue("id", out var id) ? id?.ToString() : null,
                Description = dict.TryGetValue("description", out var desc) 
                    ? desc?.ToString() ?? "" : "",
                Status = dict.TryGetValue("status", out var status) 
                    ? status?.ToString() ?? "open" : "open",
                Assignee = dict.TryGetValue("assignee", out var assignee) 
                    ? assignee?.ToString() : null,
                SourceDocument = node.FilePath,
                SourceNode = node
            });
        }
    }
    return result;
}
```

### 3.4 两层输出实现

```csharp
public IReadOnlyDictionary<string, string>? GenerateMultiple(DocumentGraph graph)
{
    var allIssues = CollectAllIssues(graph);
    var outputs = new Dictionary<string, string>();
    
    // 全局输出
    outputs["docs/issues.gen.md"] = GenerateGlobalOutput(allIssues);
    
    // Wish 级别输出
    var issuesByWish = allIssues
        .Where(i => i.SourceNode != null)
        .GroupBy(i => GetOwningWishPath(i.SourceNode!))
        .Where(g => g.Key != null);
    
    foreach (var group in issuesByWish)
    {
        var wishPath = group.Key!;
        var outputPath = $"{wishPath}/project-status/issues.md";
        outputs[outputPath] = GenerateWishOutput(group.ToList(), wishPath);
    }
    
    return outputs;
}
```

### 3.5 验收标准

- [ ] 对象格式（现有）继续工作
- [ ] 字符串格式正确解析
- [ ] 全局输出生成到 `docs/issues.gen.md`
- [ ] Wish 级别输出生成到各 `project-status/issues.md`
- [ ] resolved_issues 正确归档到 Resolved 区域

---

## 4. Phase 3: Goals 聚合

**目标**：新建 GoalAggregator，结构类似 IssueAggregator

### 4.1 改动清单

| 文件 | 改动 | 行数 |
|:-----|:-----|:-----|
| `Visitors/GoalAggregator.cs` | 新建，完整实现 | +80 |
| `Commands/RunCommand.cs` | 注册新 Visitor | +1 |

### 4.2 GoalAggregator 结构

```csharp
public class GoalAggregator : IDocumentGraphVisitor
{
    public string Name => "goals";
    public string OutputPath => "docs/goals.gen.md";
    public IReadOnlyList<string> RequiredFields => [KnownFrontmatterFields.Goals];
    
    public string Generate(DocumentGraph graph) => GenerateGlobalOutput(CollectAllGoals(graph));
    
    public IReadOnlyDictionary<string, string>? GenerateMultiple(DocumentGraph graph)
    {
        // 与 IssueAggregator 类似的两层输出逻辑
    }
    
    private class Goal
    {
        public string? Id { get; set; }
        public string Description { get; set; } = "";
        public string SourceDocument { get; set; } = "";
        public DocumentNode? SourceNode { get; set; }
    }
}
```

### 4.3 验收标准

- [ ] goals 字符串格式正确解析
- [ ] 全局输出生成到 `docs/goals.gen.md`
- [ ] Wish 级别输出生成到各 `project-status/goals.md`
- [ ] resolved_goals 正确归档到 Resolved 区域

---

## 5. 测试计划

### 5.1 单元测试

| 测试场景 | 覆盖阶段 |
|:---------|:---------|
| `GenerateMultiple()` 返回 null 时走单输出 | Phase 1 |
| 多输出路径写入正确 | Phase 1 |
| Issue 字符串格式解析 | Phase 2 |
| Issue 对象格式向后兼容 | Phase 2 |
| Goal 字符串格式解析 | Phase 3 |
| Wish 归属判断正确 | Phase 2, 3 |

### 5.2 集成测试

- 运行 `docgraph` 命令验证完整流程
- 检查生成文件内容符合 Shape-Tier 定义的格式

---

## 6. 实施顺序

```
Phase 1 (0.5h)
    │
    ├──► Phase 2 (1h) ──► 测试 Issue 聚合
    │
    └──► Phase 3 (1h) ──► 测试 Goal 聚合
                              │
                              ▼
                      集成测试 + 文档更新
```

**建议**：Phase 2 和 Phase 3 可并行开发（由不同 Implementer），因为它们只依赖 Phase 1。

---

**状态**：🟢 完成
**更新**：2026-01-05
