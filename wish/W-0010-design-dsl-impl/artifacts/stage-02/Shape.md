---
docId: "W-0010-Stage-02-Shape"
title: "Stage-02 Shape: TermNode/ClauseNode 实现 + 测试完善"
stage: 02
status: Frozen
inputs: [stage-01]
produce_by:
  - "wish/W-0010-design-dsl-impl/wish.md"
---

# Stage-02 Shape: TermNode/ClauseNode 实现 + 测试完善

> **前情提要**: 请先阅读 [prologue.md](./prologue.md) 了解 Stage-01 的完成状态和可依赖接口。

---

## 概览

**阶段目标**: 实现 TermNodeBuilder 和 ClauseNodeBuilder，完成 INodeBuilder 职责链的语义识别能力。

**当前阶段任务**:
```
Task-S-004 (TermNodeBuilder) ← 依赖 Stage-01 的 INodeBuilder 框架
Task-S-005 (ClauseNodeBuilder) ← 依赖 Stage-01 的 INodeBuilder 框架
Task-S-006 (测试完善) ← 依赖 S-004, S-005
```

**核心挑战**: 
- 从 HeadingBlock 标题文本中识别 Term/Clause 模式
- 使用 `HeadingTextExtractor.ExtractText()` 获取原始文本（含反引号）

**设计决策**:
- **匹配策略**: 采用正则 + 二次 Identifier 校验（理由：实现简单、HeadingTextExtractor 已保真）
- **Heading Level**: 允许任意 Level（不约束必须 `##` 或 `###`，但测试覆盖多种 Level）
- **尾随空白**: 允许尾随空白，提取后 `Trim()`

---

## Task-S-004: 实现 TermNodeBuilder

**目标**: 实现 INodeBuilder，识别 Term 定义模式，构造 TermNode。

**DSL 规范参考**: 根据 @`Term-Node` 和 @[F-TERM-DEFINITION-FORMAT]：
- **格式**: `term \`Term-ID\` 可选标题`
- **Term-ID**: 符合 Identifier 格式（`^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$`）
- **关键字 `term`**: 大小写不敏感

**示例**:
```markdown
## term `User-Account` 用户账号
## Term `DNA-Sequence`
## TERM `simple`
```

**产出**:
- `atelia/src/DesignDsl/TermNode.cs` — TermNode 类
- `atelia/src/DesignDsl/TermNodeBuilder.cs` — INodeBuilder 实现

**数据结构**:
```csharp
/// <summary>术语定义节点</summary>
public sealed class TermNode : AxtNode
{
    /// <summary>Term ID（不含反引号，保留原始大小写）</summary>
    public string TermId { get; }
    
    /// <summary>可选标题（已 Trim，空字符串转为 null）</summary>
    public string? Title { get; }
    
    // 推荐构造签名：
    // public TermNode(HeadingBlock heading, IReadOnlyList<Block> content, string termId, string? title)
    //     : base(heading, content) { ... }
}
```

**验收标准**:
1. `TermNode` 继承 `AxtNode`，有 `TermId` 和 `Title` 属性
2. `TermNodeBuilder` 实现 `INodeBuilder`
3. `TryBuild()` 对匹配 Term 模式的 HeadingBlock 返回 `TermNode`，否则返回 null
4. 关键字 `term` 大小写不敏感
5. Term-ID 必须符合 Identifier 格式（否则返回 null）
6. 关键字与反引号之间必须有空白
7. `dotnet build atelia/src/DesignDsl/` 无错误

**SubAgent 提示**:
- 使用 `HeadingTextExtractor.ExtractText(heading, originalMarkdown)` 获取原始标题文本
- 推荐正则：`` ^\s*term\s+`([^`]+)`(?:\s+(.+?))?\s*$ ``（忽略大小写，允许尾随空白）
- Identifier 验证正则：`^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$`
- 使用 `[GeneratedRegex]` 属性优化正则性能
- Title 提取后 `Trim()`，空字符串转为 null

**依赖**: Stage-01（INodeBuilder 框架、HeadingTextExtractor）

---

## Task-S-005: 实现 ClauseNodeBuilder

**目标**: 实现 INodeBuilder，识别 Clause 定义模式，构造 ClauseNode。

**DSL 规范参考**: 根据 @`Clause-Node` 和 @[F-CLAUSE-DEFINITION-FORMAT]：
- **格式**: `<modifier> [Clause-ID] 可选标题`
- **Modifier**: `decision` | `spec` | `derived`（大小写不敏感）
- **Clause-ID**: 符合 Identifier 格式

**示例**:
```markdown
### decision [S-MARKDOWN-COMPATIBLE] 兼容GFM
### spec [F-TERM-REFERENCE-FORMAT] 定义引用格式
### derived [F-CLAUSE-REFERENCE-LINK] 可以是链接
```

**产出**:
- `atelia/src/DesignDsl/ClauseNode.cs` — ClauseNode 类
- `atelia/src/DesignDsl/ClauseModifier.cs` — ClauseModifier 枚举
- `atelia/src/DesignDsl/ClauseNodeBuilder.cs` — INodeBuilder 实现

**数据结构**:
```csharp
/// <summary>条款修饰符</summary>
public enum ClauseModifier { Decision, Spec, Derived }

/// <summary>条款定义节点</summary>
public sealed class ClauseNode : AxtNode
{
    public ClauseModifier Modifier { get; }
    public string ClauseId { get; }
    /// <summary>可选标题（已 Trim，空字符串转为 null）</summary>
    public string? Title { get; }
    
    // 推荐构造签名：
    // public ClauseNode(HeadingBlock heading, IReadOnlyList<Block> content, 
    //                   ClauseModifier modifier, string clauseId, string? title)
    //     : base(heading, content) { ... }
}
```

**验收标准**:
1. `ClauseModifier` 枚举包含 `Decision`、`Spec`、`Derived`
2. `ClauseNode` 继承 `AxtNode`，有 `Modifier`、`ClauseId`、`Title` 属性
3. `ClauseNodeBuilder` 实现 `INodeBuilder`
4. `TryBuild()` 对匹配 Clause 模式的 HeadingBlock 返回 `ClauseNode`，否则返回 null
5. Modifier 关键字大小写不敏感
6. Clause-ID 必须符合 Identifier 格式（否则返回 null）
7. Modifier 与方括号之间必须有空白
8. `dotnet build atelia/src/DesignDsl/` 无错误

**SubAgent 提示**:
- 使用 `HeadingTextExtractor.ExtractText(heading, originalMarkdown)` 获取原始标题文本
- 推荐正则：`^\s*(decision|spec|derived)\s+\[([^\]]+)\](?:\s+(.+?))?\s*$`（忽略大小写，允许尾随空白）
- 使用 `Enum.TryParse<ClauseModifier>(str, ignoreCase: true, out var m)`
- 与 S-004 保持一致的匹配策略
- Title 提取后 `Trim()`，空字符串转为 null

**依赖**: Stage-01（INodeBuilder 框架、HeadingTextExtractor）

---

## Task-S-006: 测试完善

**目标**: 为 S-004/S-005 编写单元测试，并完善端到端测试。

**产出**:
- `atelia/tests/DesignDsl.Tests/TermNodeBuilderTests.cs`
- `atelia/tests/DesignDsl.Tests/ClauseNodeBuilderTests.cs`
- `atelia/tests/DesignDsl.Tests/EndToEndTests.cs`

**验收标准**:

1. **TermNodeBuilderTests** 至少包含 8 个测试用例：
   - 正例：标准格式、仅 ID 无标题、大小写变体（`term`/`Term`/`TERM`）
   - 反例：缺少关键字、ID 无反引号、ID 格式非法、关键字与 ID 无空白

2. **ClauseNodeBuilderTests** 至少包含 10 个测试用例：
   - 正例：三种 Modifier 各一个、仅 ID 无标题、大小写变体
   - 反例：未知 Modifier、ID 无方括号、ID 格式非法

3. **EndToEndTests** 解析固化的测试文档：
   - 在 `atelia/test-data/DesignDsl/` 创建 `dsl-sample.md`（稳定的测试 fixture）
   - 验证关键节点存在（而非数量阈值）：
     - 至少包含一个 TermNode（如 `term \`Sample-Term\``）
     - 至少包含三种 ClauseModifier 各一个
   - 验证不同 Heading Level 的处理（`#`/`##`/`###`/`####` 都测试）

4. 所有测试在 `dotnet test` 下通过

**SubAgent 提示**:
- 使用 `[Theory]` + `[InlineData]` 组织参数化测试
- 参考已有的 `AtxSectionSplitterTests.cs` 风格
- 在测试中配置 Pipeline：先注册 TermNodeBuilder 和 ClauseNodeBuilder，再调用 Build

**依赖**: Task-S-004, Task-S-005

---

## Epilogue（后续方向）

### Stage-03 预告：DocGraph 集成 API

**目标**: 设计 DesignDsl 模块对外暴露的公共 API，供 DocGraph 调用。

**预计产出**:
- `DesignDslParser.cs` — 公共入口类
- `DesignDslParseResult` — 返回类型

**开放问题**:
- [ ] 是否需要 `ParseAsync` 异步版本？
- [ ] 错误处理策略：返回 Result 还是抛异常？
- [ ] Pipeline 配置是否应该可定制？

### 不在当前范围

以下内容明确不在 Stage-02 范围内：

1. **DocGraph 集成 API**（Stage-03）
2. **跨文档引用解析**（`@[CLAUSE-ID]` 链接）
3. **增量解析**（文档变更时局部更新）
4. **Summary-Node 等其他节点类型**

---

## 变更日志

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-13 | TeamLeader | 创建 Stage-02 Shape（三段式改造） | Stage 管理机制实验 |
| 2026-01-14 | Craftsman | 审阅设计，修复 6 个问题 | 正则尾随空白、E2E fixture、Level 约束等 |
| 2026-01-14 | Implementer | 完成 S-004/S-005/S-006 实现 | 67 个测试全部通过 |
| 2026-01-14 | TeamLeader | 标记为 Frozen | Stage-02 完成 |
