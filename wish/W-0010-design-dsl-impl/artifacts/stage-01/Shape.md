---
docId: "W-0010-Shape"
title: "Atelia.DesignDsl Shape-Tier 设计（任务简报模式）"
produce_by:
  - "wish/W-0010-design-dsl-impl/wish.md"
produce:
  - "wish/W-0010-design-dsl-impl/artifacts/stage-01/test-vectors-s001-splitter.md"
  - "wish/W-0010-design-dsl-impl/artifacts/stage-01/test-vectors-s002-framework.md"
  - "wish/W-0010-design-dsl-impl/artifacts/stage-01/test-vectors-s003-tree-builder.md"
---

# Atelia.DesignDsl Shape-Tier 设计（任务简报模式）

> **实验说明**: 本文档采用"任务简报模式"，每个 Task 都是一个自包含的工作单元，可由 runSubagent 独立执行。

## 概览

**目标**: 实现 AI-Design-DSL 的基础解析器，从 Markdown 文档中提取 ATX-Tree、Term-Node、Clause-Node。

**核心数据流**（渐进式构建）:
```
MarkdownDocument (Block 序列)
    ↓ Task-S-001: 分段器
AtxSectionResult { Preface, AtxSections[] }
    ↓ Task-S-002: INodeBuilder 框架
AxtNode 序列（含 TermNode/ClauseNode）
    ↓ Task-S-003: AxtTree 构建
AxtTree (HeadingNode 树，嵌套关系)
```

**分层架构**（超平面切分）:
```
┌─────────────────────────────────────────────────────────────┐
│  Layer 0: Extraction (S-001)                                │
│  MarkdownDocument → AtxSectionResult                        │
│  纯结构分段，不做语义判断                                    │
└─────────────────────────────────────────────────────────────┘
                           ↓ 超平面 H1
┌─────────────────────────────────────────────────────────────┐
│  Layer 1: Classification (S-002 + S-004/S-005)              │
│  HeadingBlock → 标题文本 → 模式匹配 → AxtNode               │
│  INodeBuilder[] 职责链，支持扩展                            │
└─────────────────────────────────────────────────────────────┘
                           ↓ 超平面 H2
┌─────────────────────────────────────────────────────────────┐
│  Layer 2: Tree Building (S-003)                             │
│  AxtNode[] → AxtTree（深度栈算法建立嵌套关系）              │
└─────────────────────────────────────────────────────────────┘
```

**程序集**: `Atelia.DesignDsl`（位于 `atelia/src/DesignDsl/`）

**设计原则**:
- **渐进式构建**：每一步职责单一，失败面小，便于测试
- **职责链扩展**：INodeBuilder 框架支持未来新增节点类型（如 Summary-Node）
- **推迟决策**：模式匹配策略（Inline 结构 vs 正则）作为 OpenQuestion，在具体 Builder 中决定

**依赖图**:
```
Task-S-001 (Block 分段器) ← 无依赖，可立即开始
    ↓
Task-S-002 (INodeBuilder 框架 + 数据结构) ← 依赖 S-001
    ↓
Task-S-003 (AxtTree 构建) ← 依赖 S-002
Task-S-004 (TermNodeBuilder) ← 依赖 S-002
Task-S-005 (ClauseNodeBuilder) ← 依赖 S-002
    ↓
Task-S-006 (测试策略) ← 依赖 S-001~S-005
Task-S-007 (DocGraph 集成 API) ← 依赖 S-003~S-005
```

---

## Task-S-001: 实现 Block 序列分段器

**目标**: 将 Markdig 解析出的 Block 序列分段为"前导内容 + ATX Section 列表"，为后续构建 AxtNode 提供干净的中间数据。

**背景**: 根据 DSL 规范 @`ATX-Node`，每个 ATX Heading 及其后续内容（直到下一个 ATX Heading 或 EOF）构成一个逻辑段落。分段器是最基础的处理步骤：
- **输入**：Markdig 解析出的 Block 序列（`IReadOnlyList<Block>` 或 `MarkdownDocument`）
- **输出**：分段结果，包含前导内容（Preface）和 ATX Section 列表

这一步**不做任何语义判断**（不识别 Term/Clause），只做纯粹的结构分段。无论输入什么，都能成功输出，把所有 Block 划分到某个输出部分。

**输入**: 无前置依赖

**产出**:
- `atelia/src/DesignDsl/DesignDsl.csproj` — 项目文件（TargetFramework=net9.0, 依赖 Markdig）
- `atelia/src/DesignDsl/AtxSectionResult.cs` — 分段结果数据结构
- `atelia/src/DesignDsl/AtxSectionSplitter.cs` — 分段器实现

**数据结构设计**:
```csharp
/// <summary>Block 序列分段结果</summary>
public sealed class AtxSectionResult
{
    /// <summary>YAML Front Matter（如果存在）</summary>
    public YamlFrontMatterBlock? FrontMatter { get; }
    
    /// <summary>FrontMatter 之后、首个 ATX Heading 之前的 Blocks（用于 RootNode.Content）</summary>
    public IReadOnlyList<Block> Preface { get; }
    
    /// <summary>ATX Heading 及其下辖 Blocks 的列表</summary>
    public IReadOnlyList<AtxSection> Sections { get; }
}

/// <summary>单个 ATX Section</summary>
public sealed class AtxSection
{
    /// <summary>ATX Heading Block</summary>
    public HeadingBlock Heading { get; }
    
    /// <summary>该 Heading 下辖的 Blocks（直到下一个 HeadingBlock 或 EOF）</summary>
    public IReadOnlyList<Block> Content { get; }
}
```

**验收标准**:
1. 项目文件包含 Markdig NuGet 依赖（版本 >= 0.38.0）
2. `AtxSectionSplitter.Split(IReadOnlyList<Block> blocks)` 方法存在，返回 `AtxSectionResult`
3. 空输入返回 `FrontMatter=null` + 空 Preface + 空 Sections（不抛异常）
4. 如果首个 Block 是 `YamlFrontMatterBlock`，识别并单独存储到 `FrontMatter` 字段
5. 无 HeadingBlock 时，所有非 FrontMatter Block 归入 Preface，Sections 为空
6. 有 HeadingBlock 时，首个 HeadingBlock 之前的 Block（不含 FrontMatter）归入 Preface
7. 每个 HeadingBlock 与其后续 Block（直到下一个 HeadingBlock 或 EOF）组成一个 AtxSection
8. 所有输入 Block 都被划分到输出的某个部分（FrontMatter 或 Preface 或某个 Section.Content）
9. `dotnet build atelia/src/DesignDsl/` 无错误

**SubAgent 提示**:
- 命名空间使用 `Atelia.DesignDsl`
- 首先检查第一个 Block 是否是 `YamlFrontMatterBlock`，是则单独存储
- 遍历 Block 序列，遇到 `HeadingBlock` 时切分
- 使用 `List<Block>` 收集当前段落，遇到下一个 HeadingBlock 时封装为 AtxSection
- 这是纯函数，无副作用，非常适合单元测试
- 代码格式由 `cd atelia && pwsh atelia/format.ps1` 统一处理，编码时聚焦功能正确性

**依赖**: 无

---

## Task-S-002: 定义 HeadingNode/AxtNode 数据结构 + INodeBuilder 框架

**目标**: 定义核心数据结构（HeadingNode、AxtNode），并建立 INodeBuilder 职责链框架，将 AtxSection 转换为 AxtNode 序列。

**背景**: 这一步引入节点数据模型和可扩展的构建框架：
- **HeadingNode**: 标题节点基类，包含 Heading（标题内容）、Content（块级内容）、Depth（深度）
- **AxtNode**: 对应 Markdown ATX Heading（`#` ~ `######`），Depth=井号数量
- **INodeBuilder**: 职责链模式，检查 HeadingBlock 是否属于其职能范围，返回对应类型的 AxtNode

职责链设计：
- 每个 INodeBuilder 实现检查 HeadingBlock，能处理则返回具体节点（如 TermNode），不能则返回 null
- 最后有一个 **DefaultNodeBuilder** 作为兜底，构造普通 AxtNode
- 未来扩展（如 SummaryNode）只需新增 INodeBuilder 实现并注册

**输入**: Task-S-001 完成的分段结果

**产出**:
- `atelia/src/DesignDsl/HeadingNode.cs` — HeadingNode 基类
- `atelia/src/DesignDsl/AxtNode.cs` — AxtNode 类
- `atelia/src/DesignDsl/INodeBuilder.cs` — 职责链接口
- `atelia/src/DesignDsl/DefaultNodeBuilder.cs` — 兜底实现
- `atelia/src/DesignDsl/NodeBuilderPipeline.cs` — 职责链调度器
- `atelia/src/DesignDsl/HeadingTextExtractor.cs` — 标题文本提取辅助类（供 S-004/S-005 复用）

**数据结构设计**:
```csharp
/// <summary>标题节点基类</summary>
public abstract class HeadingNode
{
    public ContainerInline? Heading { get; }    // 标题内容（RootNode 为 null）
    public IReadOnlyList<Block> Content { get; } // 下辖的块级内容
    public int Depth { get; }                    // 深度（RootNode=0, AxtNode=HeadingBlock.Level）
    public HeadingNode? Parent { get; internal set; }
    public IReadOnlyList<HeadingNode> Children { get; }
}

/// <summary>ATX 标题节点</summary>
public class AxtNode : HeadingNode
{
    public HeadingBlock SourceHeadingBlock { get; } // 原始 Markdig 对象
}

/// <summary>节点构建器接口（职责链模式）</summary>
public interface INodeBuilder
{
    /// <summary>尝试构建节点。能处理返回节点，不能处理返回 null。</summary>
    AxtNode? TryBuild(HeadingBlock heading, IReadOnlyList<Block> content);
}

/// <summary>标题文本提取辅助类（供 INodeBuilder 实现复用）</summary>
public static class HeadingTextExtractor
{
    /// <summary>从 HeadingBlock 提取原始标题文本（保留反引号等格式符号）</summary>
    public static string ExtractText(HeadingBlock heading, string originalMarkdown);
}
```

**验收标准**:
1. `HeadingNode` 是 abstract class，包含 `Heading`、`Content`、`Depth`、`Children`、`Parent` 属性
2. `AxtNode` 继承 `HeadingNode`，有 `SourceHeadingBlock` 属性
3. `INodeBuilder.TryBuild()` 方法签名正确，返回 `AxtNode?`
4. `DefaultNodeBuilder` 实现 `INodeBuilder`，总是返回普通 `AxtNode`（兜底）
5. `NodeBuilderPipeline` 按顺序调用注册的 INodeBuilder，首个非 null 结果作为最终节点
6. `NodeBuilderPipeline` 默认包含 `DefaultNodeBuilder` 作为最后一个 Builder
7. `Children` 和 `Parent` 双向引用一致（插入子节点时同步设置 Parent）
8. `HeadingTextExtractor.ExtractText(heading, originalMarkdown)` 方法存在，使用 Span 切片保留原始标题文本（含反引号等格式符号）
9. 所有公共类型有 XML 文档注释（至少包含一句话描述）
10. `dotnet build atelia/src/DesignDsl/` 无错误

**SubAgent 提示**:
- `NodeBuilderPipeline` 可以用 `List<INodeBuilder>` 存储 Builder 链
- 调用顺序：先注册的 Builder 优先，`DefaultNodeBuilder` 放最后
- `HeadingNode.Children` 建议用 `List<HeadingNode>` 内部存储，对外暴露 `IReadOnlyList`
- Depth 来自 `HeadingBlock.Level`
- **HeadingTextExtractor 实现**：使用 `heading.Inline.Span` 从原始 Markdown 字符串切片，完美保留反引号等格式符号
- **注意**：TermNode 和 ClauseNode 在后续 Task 中定义，此处不引入
- 代码格式由 `atelia/format.ps1` 统一处理

**依赖**: Task-S-001

---

## Task-S-003: 实现 RootNode 和 AxtTree 构建

**目标**: 引入 RootNode 和 AxtTree，实现从 AtxSectionResult + AxtNode 序列构建完整的树结构。

**背景**: ATX-Tree 的嵌套规则（来自 DSL 规范 @`ATX-Tree`）：
- **RootNode** 是隐式根，Depth=0，承载 Preface 内容（FrontMatter 后、首个 ATX Heading 前）
- 对于任意 AxtNode X，其父节点 Y 是 X 上方首个 Depth 更小的节点
- 如果 X 前面没有 Depth 更小的节点，则 Y 是 RootNode

构建步骤：
1. 创建 RootNode，Content = AtxSectionResult.Preface（已不含 YamlFrontMatterBlock）
2. 使用 NodeBuilderPipeline 将 AtxSections 转换为 AxtNode 序列
3. 根据 Depth 规则建立父子关系

**输入**: Task-S-001 的分段结果 + Task-S-002 的 NodeBuilderPipeline

**产出**:
- `atelia/src/DesignDsl/RootNode.cs` — RootNode 类
- `atelia/src/DesignDsl/AxtTree.cs` — AxtTree 类
- `atelia/src/DesignDsl/AxtTreeBuilder.cs` — 树构建器

**数据结构设计**:
```csharp
/// <summary>隐式根节点（Depth=0）</summary>
public sealed class RootNode : HeadingNode
{
    // Heading = null, Depth = 0, Parent = null
    // Content = Preface（来自 AtxSectionResult）
}

/// <summary>ATX 标题树</summary>
public sealed class AxtTree
{
    public RootNode Root { get; }
    /// <summary>按文档出现顺序返回所有节点（RootNode 在首位）</summary>
    public IReadOnlyList<HeadingNode> AllNodes { get; }
}
```

**验收标准**:
1. `RootNode` 继承 `HeadingNode`，`Depth` 固定返回 0，`Parent` 固定返回 null，`Heading` 为 null
2. `RootNode.Content` 等于 `AtxSectionResult.Preface`（已由 S-001 分段器处理，不含 YamlFrontMatterBlock）
3. `AxtTree` 有 `Root` 属性和 `AllNodes` 属性
4. `AxtTreeBuilder.Build(AtxSectionResult sections, NodeBuilderPipeline pipeline, string originalMarkdown)` 方法存在
5. 父子关系正确建立：
   - `## A` (Depth=2) 后跟 `### B` (Depth=3)，则 B.Parent = A
   - `## A` (Depth=2) 后跟 `## C` (Depth=2)，则 C.Parent = A.Parent
   - `### B` (Depth=3) 后跟 `## C` (Depth=2)，则 C.Parent 回溯到合适祖先
6. `AxtTree.AllNodes` 按 HeadingBlock 文档出现顺序返回（RootNode 在首位）
7. 支持跳跃 Depth（如 `# A` 直接跟 `### B`，B.Parent = A）
8. `dotnet build atelia/src/DesignDsl/` 无错误

**SubAgent 提示**:
- 使用栈（Stack）维护当前祖先链，遇到新节点时弹出 Depth >= 当前的节点
- RootNode.Content 直接使用 `AtxSectionResult.Preface`（已由 S-001 处理好）
- `AllNodes` 可以用 List 在构建时收集，最后转为 IReadOnlyList
- 双向引用：添加 Child 时同步设置 Parent
- 代码格式由 `atelia/format.ps1` 统一处理

**依赖**: Task-S-001, Task-S-002

---

## Task-S-004: 实现 TermNodeBuilder

**目标**: 实现 INodeBuilder，识别 Term 定义模式，构造 TermNode。

**背景**: 根据 DSL 规范 @`Term-Node` 和 @[F-TERM-DEFINITION-FORMAT]：
- **格式**: `term \`Term-ID\` 可选标题`
- **Term-ID**: 符合 Identifier 格式（`^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$`）
- **关键字 `term`**: 大小写不敏感
- **Term-ID-Literal**: 反引号包裹的 Term-ID

示例：
```markdown
## term `User-Account` 用户账号
## Term `DNA-Sequence`
## TERM `simple`
```

**模式匹配策略（OpenQuestion）**:
匹配 HeadingBlock 有两种策略，实现者可以选择：
1. **基于 Inline 结构**：遍历 `HeadingBlock.Inline`，识别 LiteralInline（关键字）+ CodeInline（Term-ID）
2. **基于规范化文本**：提取 Heading 纯文本，用正则匹配

两种方式各有优劣，实现时选择一种并在代码注释中说明理由。

**输入**: Task-S-002 完成的 INodeBuilder 框架

**产出**:
- `atelia/src/DesignDsl/TermNode.cs` — TermNode 类
- `atelia/src/DesignDsl/TermNodeBuilder.cs` — INodeBuilder 实现

**数据结构设计**:
```csharp
/// <summary>术语定义节点</summary>
public sealed class TermNode : AxtNode
{
    /// <summary>Term ID（不含反引号，保留原始大小写）</summary>
    public string TermId { get; }
    
    /// <summary>可选标题</summary>
    public string? Title { get; }
}
```

**验收标准**:
1. `TermNode` 继承 `AxtNode`，有 `TermId` 和 `Title` 属性
2. `TermNodeBuilder` 实现 `INodeBuilder`
3. `TryBuild()` 对匹配 Term 模式的 HeadingBlock 返回 `TermNode`，否则返回 null
4. 关键字 `term` 大小写不敏感（`TERM`、`Term`、`term` 都识别）
5. Term-ID 必须符合 Identifier 格式（否则返回 null，降级为普通 AxtNode）
6. TermId 保留原始大小写，但比较时使用 `OrdinalIgnoreCase`
7. 标题是可选的，可以为空
8. 关键字与反引号之间必须有空白
9. `dotnet build atelia/src/DesignDsl/` 无错误

**SubAgent 提示**:
- **使用 `HeadingTextExtractor.ExtractText()`** 获取标题纯文本（S-002 已实现）
- **如果选择正则方式**：对提取的纯文本使用 `[GeneratedRegex]` 正则匹配
- **如果选择 Inline 结构方式**：直接遍历 `HeadingBlock.Inline`，识别 `LiteralInline`（关键字）+ `CodeInline`（Term-ID）
- Identifier 正则：`^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$`
- 实现选择的理由写在代码注释中，便于后续维护
- 代码格式由 `atelia/format.ps1` 统一处理

**依赖**: Task-S-002

---

## Task-S-005: 实现 ClauseNodeBuilder

**目标**: 实现 INodeBuilder，识别 Clause 定义模式，构造 ClauseNode。

**背景**: 根据 DSL 规范 @`Clause-Node` 和 @[F-CLAUSE-DEFINITION-FORMAT]：
- **格式**: `<modifier> [Clause-ID] 可选标题`
- **Modifier**: `decision` | `spec` | `derived`（大小写不敏感）
- **Clause-ID**: 符合 Identifier 格式
- **Clause-ID-Literal**: 方括号包裹的 Clause-ID

示例：
```markdown
### decision [S-MARKDOWN-COMPATIBLE] 兼容GFM
### spec [F-TERM-REFERENCE-FORMAT] 定义引用格式
### derived [F-CLAUSE-REFERENCE-LINK] 可以是链接
### SPEC [lowercase-id]
```

**模式匹配策略（OpenQuestion）**:
与 Task-S-004 相同，实现者可以选择：
1. **基于 Inline 结构**：遍历 `HeadingBlock.Inline`，识别 LiteralInline（关键字）+ LinkInline 或直接文本（Clause-ID）
2. **基于规范化文本**：提取 Heading 纯文本，用正则匹配

建议与 Task-S-004 保持一致的策略。

**输入**: Task-S-002 完成的 INodeBuilder 框架

**产出**:
- `atelia/src/DesignDsl/ClauseNode.cs` — ClauseNode 类
- `atelia/src/DesignDsl/ClauseModifier.cs` — ClauseModifier 枚举
- `atelia/src/DesignDsl/ClauseNodeBuilder.cs` — INodeBuilder 实现

**数据结构设计**:
```csharp
/// <summary>条款修饰符</summary>
public enum ClauseModifier { Decision, Spec, Derived }

/// <summary>条款定义节点</summary>
public sealed class ClauseNode : AxtNode
{
    /// <summary>条款修饰符</summary>
    public ClauseModifier Modifier { get; }
    
    /// <summary>Clause ID（不含方括号，保留原始大小写）</summary>
    public string ClauseId { get; }
    
    /// <summary>可选标题</summary>
    public string? Title { get; }
}
```

**验收标准**:
1. `ClauseModifier` 枚举包含 `Decision`、`Spec`、`Derived` 三个值
2. `ClauseNode` 继承 `AxtNode`，有 `Modifier`、`ClauseId`、`Title` 属性
3. `ClauseNodeBuilder` 实现 `INodeBuilder`
4. `TryBuild()` 对匹配 Clause 模式的 HeadingBlock 返回 `ClauseNode`，否则返回 null
5. Modifier 关键字大小写不敏感（`Decision`、`DECISION`、`decision` 都识别）
6. Clause-ID 必须符合 Identifier 格式（否则返回 null，降级为普通 AxtNode）
7. ClauseId 保留原始大小写，但比较时使用 `OrdinalIgnoreCase`
8. 标题是可选的，可以为空
9. Modifier 与方括号之间必须有空白
10. `dotnet build atelia/src/DesignDsl/` 无错误

**SubAgent 提示**:
- **使用 `HeadingTextExtractor.ExtractText()`** 获取标题纯文本（S-002 已实现）
- **如果选择正则方式**：推荐 `^(decision|spec|derived)\s+\[([^\]]+)\](?:\s+(.+))?$`（忽略大小写）
- 将 modifier 字符串转换为枚举：`Enum.TryParse<ClauseModifier>(modifierStr, ignoreCase: true, out var modifier)`
- Identifier 正则：`^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$`
- 与 Task-S-004 保持一致的匹配策略
- 代码格式由 `atelia/format.ps1` 统一处理

**依赖**: Task-S-002

---

## Task-S-006: 设计单元测试策略

**目标**: 设计 DesignDsl 模块的单元测试策略，定义测试场景和测试数据。

**背景**: 测试需要覆盖（按依赖顺序）：
1. **Task-S-001 分段器**: 空输入、无 Heading、有 Heading、front-matter 处理
2. **Task-S-002 INodeBuilder 框架**: DefaultNodeBuilder、Pipeline 调用顺序
3. **Task-S-003 AxtTree 构建**: 嵌套关系、Content 切片、跳跃 Depth、front-matter 过滤
4. **Task-S-004 TermNodeBuilder**: 正例、反例、边界情况
5. **Task-S-005 ClauseNodeBuilder**: 三种 Modifier、正例、反例、边界情况
6. **端到端**: 解析真实的 AI-Design-DSL.md 文档

参考项目：`atelia/tests/DocGraph.Tests/` 使用 xUnit。

**输入**: Task-S-001 ~ S-005 完成的实现

**产出**:
- `atelia/tests/DesignDsl.Tests/DesignDsl.Tests.csproj` — 测试项目文件
- `atelia/tests/DesignDsl.Tests/AtxSectionSplitterTests.cs` — 分段器测试
- `atelia/tests/DesignDsl.Tests/NodeBuilderPipelineTests.cs` — 职责链测试
- `atelia/tests/DesignDsl.Tests/AxtTreeBuilderTests.cs` — 树构建测试
- `atelia/tests/DesignDsl.Tests/TermNodeBuilderTests.cs` — Term 构建测试
- `atelia/tests/DesignDsl.Tests/ClauseNodeBuilderTests.cs` — Clause 构建测试
- `atelia/tests/DesignDsl.Tests/EndToEndTests.cs` — 端到端测试

**验收标准**:
1. 测试项目使用 xUnit + FluentAssertions
2. AtxSectionSplitterTests 至少包含 7 个测试用例：
   - 空输入、无 HeadingBlock、仅有 Preface、单个 Section、多个 Section
   - **带 YamlFrontMatterBlock**（验证 FrontMatter 字段正确存储，Preface 不含 YAML Block）
   - **无 YamlFrontMatterBlock**（验证 FrontMatter 为 null）
3. NodeBuilderPipelineTests 至少包含 4 个测试用例：
   - 空 Pipeline（应报错或有 DefaultBuilder）、单个 Builder、多个 Builder 优先级、DefaultBuilder 兜底
4. AxtTreeBuilderTests 至少包含 9 个测试用例：
   - 空文档、仅 RootNode 内容、单层 Heading、多层嵌套、跳跃 Depth、同级多节点
   - **带 YAML front-matter 文档**（验证 RootNode.Content 等于输入的 Preface，不含 YAML Block）
   - **跳跃 Depth 场景**（如 `# A` 直接跟 `### B`）
5. TermNodeBuilderTests 至少包含 8 个测试用例：
   - 正例：标准格式、仅 ID 无标题、大小写变体
   - 反例：缺少关键字、ID 无反引号、ID 格式非法、关键字与 ID 无空白
6. ClauseNodeBuilderTests 至少包含 10 个测试用例：
   - 正例：三种 Modifier 各一个、仅 ID 无标题、大小写变体
   - 反例：未知 Modifier、ID 无方括号、ID 格式非法
7. EndToEndTests 解析 `agent-team/wiki/SoftwareDesignModeling/AI-Design-DSL.md`：
   - 验证 RootNode 存在
   - 验证 TermNode 数量 >= 10
   - 验证 ClauseNode 数量 >= 15
   - 验证特定节点存在（如 `Design-DSL`、`S-MARKDOWN-COMPATIBLE`）
8. 所有测试在 `dotnet test` 下通过

**SubAgent 提示**:
- 使用 `[Theory]` + `[InlineData]` 组织参数化测试
- 测试文件路径使用相对于 test-data 目录的路径
- 在 `atelia/test-data/DesignDsl/` 下创建测试用 Markdown 文件，包括：
  - `minimal-with-frontmatter.md`（带 YAML front-matter + 前导内容 + 多级 Heading）
  - `term-and-clause.md`（包含 Term 定义、Clause 定义、以及非法 ID 的反例）
- 参考 `atelia/tests/DocGraph.Tests/DocumentGraphBuilderTests.cs` 的测试风格

**依赖**: Task-S-001 ~ S-005

---

## Task-S-007: 定义 DocGraph 集成 API

**目标**: 设计 DesignDsl 模块对外暴露的公共 API，供 DocGraph 调用。

**背景**: DocGraph 需要从 Markdown 文档中提取语义信息：
- 术语定义（Term）
- 条款定义（Clause，区分 decision/spec/derived）
- 文档结构（ATX-Tree）

集成点：
- DocGraph 扫描文件时调用 DesignDsl 解析
- 解析结果用于构建依赖图（后续 Wish）
- 解析结果用于生成汇总文档

**输入**: Task-S-003 ~ S-005 完成的实现

**产出**:
- `atelia/src/DesignDsl/DesignDslParser.cs` — 公共入口类
- 更新 `atelia/src/DocGraph/DocGraph.csproj` 添加 ProjectReference

**验收标准**:
1. `DesignDslParser` 类存在，是 `public static` 或可实例化
2. `ParseFile(string filePath)` 方法：读取文件并解析（使用配置好的 Pipeline）
3. `ParseDocument(MarkdownDocument document)` 方法：从 Markdig 对象解析
4. Markdig Pipeline 配置必须包含：`UseYamlFrontMatter()`（以及必要的 GFM 扩展）
5. 返回类型包含：
   - `AxtTree Tree` — 完整的 ATX-Tree
   - `IReadOnlyList<TermNode> Terms` — 所有 Term 节点
   - `IReadOnlyList<ClauseNode> Clauses` — 所有 Clause 节点
   - `IReadOnlyList<ClauseNode> Decisions` — 仅 Decision 类型
   - `IReadOnlyList<ClauseNode> Specs` — 仅 Spec 类型
   - `IReadOnlyList<ClauseNode> Derived` — 仅 Derived 类型
6. DocGraph.csproj 添加 `<ProjectReference Include="../DesignDsl/DesignDsl.csproj" />`
7. `dotnet build atelia/src/DocGraph/` 无错误

**SubAgent 提示**:
- 考虑使用 `record` 定义返回类型 `DesignDslParseResult`
- Pipeline 配置示例：`new MarkdownPipelineBuilder().UseYamlFrontMatter().UseAdvancedExtensions().Build()`
- 文件读取使用 `File.ReadAllText` + `Markdown.Parse(text, pipeline)`
- 遍历 `AxtTree.AllNodes` 筛选出 TermNode 和 ClauseNode
- 为便于后续扩展，考虑将 Terms/Clauses 设计为延迟计算（lazy）

**依赖**: Task-S-003, Task-S-004, Task-S-005

---

## 变更日志

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-12 | Investigator | 创建 Shape.md（任务简报模式） | 响应 W-0010 设计任务 |
| 2026-01-13 | TeamLeader | 修订皮毛问题（Craftsman 审阅建议） | YAML front-matter 配置、补强验收标准、去除编码风格要求 |
| 2026-01-13 | TeamLeader | 重构任务序列（监护人设计决策） | 渐进式构建：S-001 分段器 → S-002 INodeBuilder 框架 → S-003 AxtTree → S-004/S-005 具体 Builder |
| 2026-01-13 | TeamLeader | 应用超平面实验洞见 | 添加分层架构图、显式化 HeadingTextExtractor、统一标题文本提取逻辑 |
| 2026-01-13 | TeamLeader | 显式化 YamlFrontMatter 处理 | AtxSectionResult 增加 FrontMatter 字段，S-001 单独存储，S-003 直接使用 Preface |
