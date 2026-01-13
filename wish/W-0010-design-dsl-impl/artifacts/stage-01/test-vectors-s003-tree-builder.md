# Task-S-003 测试向量（AxtTreeBuilder / RootNode / AxtTree）

## 概述
验证 `AxtTreeBuilder.Build(AtxSectionResult, NodeBuilderPipeline)` 的树构建语义：
- `RootNode` 是隐式根，Depth=0，Heading=null，Content 来自 `AtxSectionResult.Preface`
- 对每个 `AtxSection`：`pipeline.Build(section.Heading, section.Content)` 产生一个 `AxtNode`
- 父子关系由“向上首个 Depth 更小的节点”决定（用栈算法实现）
- `AxtTree.AllNodes` 必须按文档出现顺序收集（RootNode 在首位）

说明：本文件的期望输出聚焦于“树结构/顺序/双向引用一致性”，并默认使用 `DefaultNodeBuilder`（即所有 heading 都是普通 AxtNode）。

## 测试用例

### Case 1: 空文档
**输入**:
```markdown

```

**期望输出**:
- Root:
  - Depth = 0
  - Heading = null
  - Content.Count = 0
  - Children.Count = 0
- AllNodes:
  - Count = 1
  - AllNodes[0] 为 Root

**验证点**:
- [ ] Build 不抛异常
- [ ] `tree.Root` 存在且 AllNodes 首位是 Root

---

### Case 2: 仅 RootNode 内容（无 Heading）
**输入**:
```markdown
preface-1

- a
- b
```

**期望输出**:
- Root.Content:
  - Count = 2（ParagraphBlock + ListBlock）
- Root.Children.Count = 0
- AllNodes.Count = 1

**验证点**:
- [ ] 无 Heading 时不产生 AxtNode
- [ ] Root.Content 等于 Splitter 的 Preface（全部 blocks 都在 Root.Content）

---

### Case 3: 单层 Heading（两个同级节点）
**输入**:
```markdown
# A

A-1

# B
```

**期望输出**:
- Root.Children:
  - Count = 2，顺序为 A, B
- A:
  - Depth = 1
  - Parent = Root
  - Content.Count = 1（ParagraphBlock: “A-1”）
- B:
  - Depth = 1
  - Parent = Root
  - Content.Count = 0
- AllNodes 顺序：Root, A, B

**验证点**:
- [ ] 同级节点都挂在 Root 下
- [ ] AllNodes 按文档出现顺序

---

### Case 4: 多层嵌套（逐级加深）
**输入**:
```markdown
# A

## B

### C

C-1
```

**期望输出**:
- 结构：Root -> A -> B -> C
- Parent 关系：C.Parent=B，B.Parent=A，A.Parent=Root
- Children 关系：A.Children=[B]，B.Children=[C]
- AllNodes 顺序：Root, A, B, C

**验证点**:
- [ ] 深度递增时形成链式嵌套
- [ ] Parent/Children 双向一致

---

### Case 5: 跳跃 Depth（# 直接跟 ###）
**输入**:
```markdown
# A

### B
```

**期望输出**:
- A.Depth = 1
- B.Depth = 3
- B.Parent = A（因为 A 是上方首个 Depth 更小的节点）
- AllNodes 顺序：Root, A, B

**验证点**:
- [ ] 不要求 depth 连续，仍按“首个更小 depth”挂载

---

### Case 6: 同级多节点（## B 与 ## C）
**输入**:
```markdown
# A

## B

## C

## D
```

**期望输出**:
- A.Children 顺序：B, C, D
- B/C/D 的 Parent 都是 A
- AllNodes 顺序：Root, A, B, C, D

**验证点**:
- [ ] 相同 depth 的节点互为兄弟
- [ ] 兄弟节点顺序与文档一致

---

### Case 7: 带 YAML front-matter（Root.Content 不含 YAML）
**输入**:
```markdown
---
title: demo
---

preface

# A

A-1
```

**期望输出**:
- 前置条件：使用启用 `UseYamlFrontMatter()` 的 Markdig pipeline 解析输入
- Splitter 期望：
  - `AtxSectionResult.FrontMatter != null`
  - `AtxSectionResult.Preface` 仅包含“preface”的 ParagraphBlock
- Tree 期望：
  - Root.Content.Count = 1（不包含 YAML block）
  - Root.Children[0] 为 A

**验证点**:
- [ ] Root.Content 与 Preface 完全一致
- [ ] YAML block 不应出现在 Root.Content

---

### Case 8: AllNodes 顺序验证（混合层级）
**输入**:
```markdown
# A

## B

# C

## D

### E
```

**期望输出**:
- AllNodes 顺序：Root, A, B, C, D, E
- 结构：
  - Root.Children: A, C
  - A.Children: B
  - C.Children: D
  - D.Children: E

**验证点**:
- [ ] AllNodes 严格按遇到 heading 的顺序追加
- [ ] 当从 `##` 回到 `#` 时，父节点回溯到 Root

---

### Case 9: Parent/Children 双向引用一致性验证（系统性检查）
**输入**:
```markdown
# A

## B

## C

### D
```

**期望输出**:
- 结构：Root -> A -> {B, C}; 且 C -> D
- 双向一致性：
  - 对每个节点 N（除 Root）：N.Parent.Children 必须包含 N
  - 对每个节点 N：N.Children 中每个 child 的 Parent 必须等于 N

**验证点**:
- [ ] Parent 与 Children 的双向引用一致
- [ ] Root 的 Parent 应为 null（注意 RootNode 通过 new Parent 隐藏基类属性）

---

### Case 10: 边界：Setext Heading（实现行为对齐检查）
**输入**:
```markdown
Title
====

para
```

**期望输出（按当前实现）**:
- 如果 Markdig 将该语法解析为 HeadingBlock：
  - Splitter 会把它当作 section heading（因为 Splitter 只检查 `is HeadingBlock`）
  - Tree 会产生一个 AxtNode，Depth = heading.Level（通常为 1）

**验证点**:
- [ ] 明确当前实现是否“仅处理 ATX”还是“处理所有 HeadingBlock”
- [ ] 若未来要严格 ATX，这个用例应作为回归保护（期望变更时需同步修改 Splitter/Builder）

