---
docId: "W-0010-Stage-02-Prologue"
title: "Stage-02 前情提要"
stage: 02
status: Active
inputs: [stage-01]
generated: false  # 未来可改为 true，由工具自动生成
---

# Stage-02 前情提要

> **说明**: 本文档汇总 Stage-01 的完成状态，为 Stage-02 实施提供上下文。
> 当前为手工编写，未来可由工具从 Stage-01 自动提取。

---

## Stage-01 完成状态

**状态**: ✅ Frozen（已冻结，不再变更）

**目标**: 实现 Block 序列分段 + 数据结构 + ATX 树构建的基础设施

### 已完成的任务

| Task | Goal（一句话） | 状态 |
|:-----|:--------------|:-----|
| S-001 | 将 Block 序列按 ATX Heading 分段为 AtxSectionResult | ✅ 完成 |
| S-002 | 定义 HeadingNode/AxtNode 数据结构 + INodeBuilder 职责链框架 | ✅ 完成 |
| S-003 | 实现 RootNode 和 AxtTree 构建（深度栈算法） | ✅ 完成 |

### Produced Artifacts（产出清单）

| Artifact-ID | 文件路径 | 说明 |
|:------------|:---------|:-----|
| `SPLITTER` | `atelia/src/DesignDsl/AtxSectionSplitter.cs` | Block 序列分段器 |
| `SECTION-RESULT` | `atelia/src/DesignDsl/AtxSectionResult.cs` | 分段结果数据结构 |
| `SECTION` | `atelia/src/DesignDsl/AtxSection.cs` | 单个 Section 数据结构 |
| `HEADING-NODE` | `atelia/src/DesignDsl/HeadingNode.cs` | 标题节点基类 |
| `AXT-NODE` | `atelia/src/DesignDsl/AxtNode.cs` | ATX 标题节点 |
| `ROOT-NODE` | `atelia/src/DesignDsl/RootNode.cs` | 隐式根节点 |
| `AXT-TREE` | `atelia/src/DesignDsl/AxtTree.cs` | ATX 标题树 |
| `TREE-BUILDER` | `atelia/src/DesignDsl/AxtTreeBuilder.cs` | 树构建器 |
| `NODE-BUILDER` | `atelia/src/DesignDsl/INodeBuilder.cs` | 节点构建器接口 |
| `DEFAULT-BUILDER` | `atelia/src/DesignDsl/DefaultNodeBuilder.cs` | 兜底构建器 |
| `PIPELINE` | `atelia/src/DesignDsl/NodeBuilderPipeline.cs` | 职责链调度器 |
| `TEXT-EXTRACTOR` | `atelia/src/DesignDsl/HeadingTextExtractor.cs` | 标题文本提取器 |
| `TESTS-S001-S003` | `atelia/tests/DesignDsl.Tests/` | 单元测试（33 个测试通过） |

---

## Contracts（可依赖的接口承诺）

Stage-02 可以依赖以下接口，它们在 Stage-01 已稳定：

### 1. AtxSectionSplitter

```csharp
public static class AtxSectionSplitter {
    /// <summary>将 Block 序列分段（只处理 ATX Heading，Setext 作为普通 Block）</summary>
    public static AtxSectionResult Split(IReadOnlyList<Block> blocks);
}
```

**Contract**:
- MUST: 空输入返回 FrontMatter=null + 空 Preface + 空 Sections
- MUST: 首个 Block 若是 YamlFrontMatterBlock，单独存储到 FrontMatter
- MUST: 只按 ATX Heading（`!heading.IsSetext`）分段
- MUST: 所有 Block 都被划分到某个输出部分

### 2. INodeBuilder 职责链

```csharp
public interface INodeBuilder {
    AxtNode? TryBuild(HeadingBlock heading, IReadOnlyList<Block> content, string originalMarkdown);
}

public class NodeBuilderPipeline {
    public void InsertBefore(INodeBuilder builder);  // 插入到 DefaultNodeBuilder 之前
    public AxtNode Build(HeadingBlock heading, IReadOnlyList<Block> content, string originalMarkdown);
}
```

**Contract**:
- MUST: Pipeline 按注册顺序调用 Builder，首个非 null 结果获胜
- MUST: DefaultNodeBuilder 始终在最后，保证永远返回非 null

### 3. HeadingTextExtractor

```csharp
public static class HeadingTextExtractor {
    /// <summary>从 HeadingBlock 提取原始标题文本（使用 Span 切片，保留反引号等格式符号）</summary>
    public static string ExtractText(HeadingBlock heading, string originalMarkdown);
}
```

**Contract**:
- MUST: 返回原始标题文本，包含反引号、方括号等格式符号
- MUST: 使用 `heading.Inline.Span` 从 originalMarkdown 切片

### 4. AxtTreeBuilder

```csharp
public static class AxtTreeBuilder {
    public static AxtTree Build(AtxSectionResult sections, NodeBuilderPipeline pipeline, string originalMarkdown);
}
```

**Contract**:
- MUST: 返回 AxtTree，Root 是 RootNode（Depth=0）
- MUST: AllNodes 按文档出现顺序排列（RootNode 在首位）
- MUST: 父子关系正确建立（深度栈算法）

---

## Extension Points（扩展点）

Stage-01 留下的扩展点，Stage-02 将实现：

### EXT-001: TermNodeBuilder

| 属性 | 值 |
|:-----|:---|
| Name | TermNodeBuilder |
| Owner Stage | Stage-01（声明），Stage-02（实现） |
| Stability | Stable |
| Expected Input | HeadingBlock + Content + originalMarkdown |
| Expected Output | TermNode（继承 AxtNode）或 null |
| Constraints | 匹配 `term \`Term-ID\` [Title]` 模式 |

### EXT-002: ClauseNodeBuilder

| 属性 | 值 |
|:-----|:---|
| Name | ClauseNodeBuilder |
| Owner Stage | Stage-01（声明），Stage-02（实现） |
| Stability | Stable |
| Expected Input | HeadingBlock + Content + originalMarkdown |
| Expected Output | ClauseNode（继承 AxtNode）或 null |
| Constraints | 匹配 `<modifier> [Clause-ID] [Title]` 模式 |

---

## Non-goals（Stage-01 明确不做的）

以下内容在 Stage-01 明确排除，Stage-02 或后续阶段可能处理：

1. **语义识别**：Stage-01 只做结构分段，不识别 Term/Clause（交给 Stage-02）
2. **跨文档引用解析**：`@[CLAUSE-ID]` 链接解析（交给后续 Stage）
3. **增量解析**：文档变更时局部更新（交给后续 Stage）
4. **DocGraph 集成 API**：公共入口类（交给 Stage-03）

---

## Resolved Issues（已解决的问题）

| Issue | 解决方案 | 详情 |
|:------|:---------|:-----|
| ISSUE-001: CodeInline 丢失反引号 | 使用 Span 切片 | HeadingTextExtractor 改用 `heading.Inline.Span` 从原始字符串切片 |
| ISSUE-002: Setext Heading 处理 | ATX-only | AtxSectionSplitter 只处理 ATX Heading，Setext 作为普通 Block |

---

## 详细设计参考

如需了解 Stage-01 的完整设计细节，请参阅：
- [stage-01/Shape.md](../stage-01/Shape.md) — 完整任务简报
- [stage-01/test-vectors-*.md](../stage-01/) — 测试向量文档
