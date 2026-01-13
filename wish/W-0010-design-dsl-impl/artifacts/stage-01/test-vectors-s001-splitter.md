# Task-S-001 测试向量（AtxSectionSplitter）

## 概述
验证 `AtxSectionSplitter.Split(IReadOnlyList<Block>)` 的纯结构分段行为：
- 识别首个 `YamlFrontMatterBlock` 并单独存储到 `AtxSectionResult.FrontMatter`
- 将 FrontMatter 之后、首个 `HeadingBlock` 之前的 blocks 收集为 `Preface`
- 将每个 `HeadingBlock` 与其后续 blocks（直到下一个 `HeadingBlock` 或 EOF）封装为 `AtxSection`

说明：以下“期望输出”以 `AtxSectionResult` 的可观察属性为准（`FrontMatter`、`Preface`、`Sections`），并用“Block 类型/数量/关键文本”描述验证点。

## 测试用例

### Case 1: 空输入（空字符串 Markdown）
**输入**:
```markdown

```

**期望输出**:
- FrontMatter: null
- Preface: 空（Count = 0）
- Sections: 空（Count = 0）

**验证点**:
- [ ] 不抛异常
- [ ] 返回对象不为 null
- [ ] `FrontMatter == null` 且 `Preface.Count == 0` 且 `Sections.Count == 0`

---

### Case 2: 无 HeadingBlock（仅段落 + 列表）
**输入**:
```markdown
这是一个段落。

- a
- b
```

**期望输出**:
- FrontMatter: null
- Preface:
  - Count = 2
  - Block[0] 类型 = ParagraphBlock（文本包含“这是一个段落”）
  - Block[1] 类型 = ListBlock（包含 2 个 ListItem）
- Sections: 空（Count = 0）

**验证点**:
- [ ] 所有非 YAML blocks 都进入 Preface
- [ ] Sections 为空

---

### Case 3: 仅有 Preface（无 Heading），且带 YAML Front-Matter
**输入**:
```markdown
---
title: demo
---

preface text
```

**期望输出**:
- FrontMatter: 非 null（类型为 YamlFrontMatterBlock）
- Preface:
  - Count = 1
  - Block[0] 类型 = ParagraphBlock（文本包含“preface text”）
- Sections: 空（Count = 0）

**验证点**:
- [ ] `FrontMatter` 单独存储，且 `Preface` 不包含 YamlFrontMatterBlock
- [ ] 无 Heading 时 Sections 为空

---

### Case 4: Preface + 单个 Section
**输入**:
```markdown
preface paragraph

# A

A-1

A-2
```

**期望输出**:
- FrontMatter: null
- Preface:
  - Count = 1（ParagraphBlock，包含“preface paragraph”）
- Sections:
  - Count = 1
  - Sections[0].Heading:
    - 类型 = HeadingBlock
    - Level = 1
  - Sections[0].Content:
    - Count = 2
    - 都是 ParagraphBlock（分别包含“A-1”“A-2”）

**验证点**:
- [ ] Preface 收集到首个 HeadingBlock 之前
- [ ] 一个 HeadingBlock 对应一个 Section
- [ ] Heading 下的 blocks 全部进入对应 Section.Content

---

### Case 5: 多个 Section（含“空内容”的 Section）
**输入**:
```markdown
# A

A-1

## B

### C

C-1
```

**期望输出**:
- FrontMatter: null
- Preface: 空（Count = 0）
- Sections:
  - Count = 3
  - Section A: Heading.Level=1，Content.Count=1（ParagraphBlock: “A-1”）
  - Section B: Heading.Level=2，Content.Count=0（因为紧跟下一个 HeadingBlock）
  - Section C: Heading.Level=3，Content.Count=1（ParagraphBlock: “C-1”）

**验证点**:
- [ ] 相邻 HeadingBlock 之间没有 blocks 时，对应 Content 为空列表
- [ ] 分段不依赖 heading level（Level 变化不影响“遇到 Heading 即切分”）

---

### Case 6: YAML Front-Matter + Preface + 多个 Section
**输入**:
```markdown
---
a: 1
b: 2
---

preface

# A

A-1

# B

B-1
```

**期望输出**:
- FrontMatter: 非 null（YamlFrontMatterBlock）
- Preface:
  - Count = 1（ParagraphBlock: “preface”）
- Sections:
  - Count = 2
  - A: Heading.Level=1，Content.Count=1（ParagraphBlock: “A-1”）
  - B: Heading.Level=1，Content.Count=1（ParagraphBlock: “B-1”）

**验证点**:
- [ ] FrontMatter 必须只在“首个 block”时被识别
- [ ] Preface 不包含 YAML block
- [ ] 每个 HeadingBlock 都产生一个 Section

---

### Case 7: 无 YAML Front-Matter（FrontMatter 应为 null）
**输入**:
```markdown
not yaml
---
still not yaml front matter

# A
```

**期望输出**:
- FrontMatter: null（因为首个 block 不是 YamlFrontMatterBlock）
- Preface:
  - Count >= 1（包含“not yaml --- still not yaml front matter”的段落/块）
- Sections:
  - Count = 1（A）

**验证点**:
- [ ] 仅当 blocks[0] 是 YamlFrontMatterBlock 才设置 FrontMatter
- [ ] 其它位置出现的 `---` 不应被当成 FrontMatter（取决于 Markdig 的解析结果，但 Splitter 不做额外判断）

---

### Case 8: YAML block 不在首位（边界：不被当作 FrontMatter）
**输入**:
```markdown
preface first

---
a: 1
---

# A
```

**期望输出**:
- FrontMatter: null（因为首个 block 不是 YamlFrontMatterBlock）
- Preface:
  - 包含“preface first”对应的 ParagraphBlock
  - 其后如果 Markdig 仍产生 YamlFrontMatterBlock（在某些 pipeline/config 下可能不会），Splitter 会把它当作普通 Preface block 或 Section.Content block（因为它只检查 blocks[0]）
- Sections:
  - Count = 1（A）

**验证点**:
- [ ] Splitter 当前实现只识别文档首块 YAML
- [ ] 即使出现 YamlFrontMatterBlock，但不在 blocks[0]，也不会进入 FrontMatter 字段

