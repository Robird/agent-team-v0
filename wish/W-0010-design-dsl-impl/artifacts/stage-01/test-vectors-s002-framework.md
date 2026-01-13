# Task-S-002 测试向量（INodeBuilder 框架 + HeadingTextExtractor）

## 概述
验证两块内容：
1) `NodeBuilderPipeline` 的职责链行为（默认兜底、顺序优先、InsertBefore 语义）
2) `HeadingTextExtractor.ExtractText(HeadingBlock)` 对 Heading inline 的“纯文本拼接”规则（LiteralInline / CodeInline / ContainerInline 递归）

说明：当前实现的 `HeadingTextExtractor` 会将 `CodeInline` 的 `.Content` 拼接到输出中（不含反引号），并对容器类 inline（如 Emphasis/Strong）递归遍历子节点。

## 测试用例

### Case 1: DefaultNodeBuilder 兜底（无自定义 Builder）
**输入**:
```markdown
# A

content
```

**期望输出**:
- NodeBuilderPipeline:
  - 使用 `new NodeBuilderPipeline()` 构造时，`Builders.Count == 1`
  - `Builders[0]` 类型为 DefaultNodeBuilder
- `pipeline.Build(heading, content)`:
  - 返回类型：AxtNode（不为 null）
  - `node.Depth == heading.Level`（此处为 1）
  - `node.SourceHeadingBlock` 引用等于输入 heading
  - `node.Content` 引用等于输入 content 列表

**验证点**:
- [ ] 默认 pipeline 必须始终能构建出节点（不返回 null）
- [ ] 未注册任何其它 builder 时，必然走 DefaultNodeBuilder

---

### Case 2: HeadingTextExtractor 基本提取（纯文本）
**输入**:
```markdown
## Hello World
```

**期望输出**:
- `HeadingTextExtractor.ExtractText(heading)` 返回：`Hello World`

**验证点**:
- [ ] LiteralInline 的 `.Content` 按原样拼接
- [ ] 不额外 trim（除非 Markdig 自身已经归一化 inline 内容）

---

### Case 3: HeadingTextExtractor 含 CodeInline（反引号）
**输入**:
```markdown
## term `User-Account` 用户账号
```

**期望输出**:
- `HeadingTextExtractor.ExtractText(heading, originalMarkdown)` 返回：``term `User-Account` 用户账号``
  - 注意：输出中**包含反引号**（使用 Span 切片方案）

**验证点**:
- [ ] 反引号被完整保留
- [ ] 关键字与 ID/Title 之间的空白在输出中保留

---

### Case 4: HeadingTextExtractor 含嵌套 Inline（如加粗/斜体）
**输入**:
```markdown
## Hello **Bold** and *Italic*
```

**期望输出**:
- `HeadingTextExtractor.ExtractText(heading)` 返回：`Hello Bold and Italic`

**验证点**:
- [ ] Strong/Emphasis 作为 ContainerInline 时递归遍历其 FirstChild..NextSibling
- [ ] 子节点 LiteralInline 内容按顺序拼接

---

### Case 5: Pipeline 顺序优先（首个非 null 结果获胜）
**输入**:
```markdown
# A
```

**期望输出**:
- 给定两个自定义 builder：
  - Builder1.TryBuild(...) 返回 null
  - Builder2.TryBuild(...) 返回某个 AxtNode 实例（例如用传入 heading/content new AxtNode）
- 使用 `new NodeBuilderPipeline(new[]{ Builder1, Builder2 })` 构造：
  - pipeline.Build(...) 返回 Builder2 的节点（不进入 DefaultNodeBuilder）

**验证点**:
- [ ] 按注册顺序调用 builder
- [ ] 第一个返回非 null 的结果立即返回

---

### Case 6: InsertBefore 语义（插入到 DefaultNodeBuilder 之前）
**输入**:
```markdown
# A
```

**期望输出**:
- 给定 `pipeline = new NodeBuilderPipeline()`，然后 `pipeline.InsertBefore(customBuilder)`：
  - `Builders.Count == 2`
  - `Builders[0] == customBuilder`
  - `Builders[1]` 类型为 DefaultNodeBuilder
- 若 customBuilder 返回 null，则仍应由 DefaultNodeBuilder 兜底返回 AxtNode

**验证点**:
- [ ] DefaultNodeBuilder 始终保持在最后
- [ ] InsertBefore 不改变 DefaultNodeBuilder 的“兜底”语义

