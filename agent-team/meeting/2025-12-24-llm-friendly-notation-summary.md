# 畅谈会摘要：LLM 友好的信息表示形式

> 原始记录：[2025-12-24-llm-friendly-notation.md](2025-12-24-llm-friendly-notation.md)

## 核心问题

对于 LLM 来说，如果一种信息只以一种形式记录的话，能否总结出一套推荐规则？——不同信息表示形式（ASCII art、Mermaid、表格、列表）对 LLM 的"认知负担"有何差异？

## 参与者观点摘要

### Advisor-Claude

- **编译管线类比**：LLM 信息处理类似于 `Input → Tokenizer → Token Sequence → Attention → Semantic Representation → Output`，与人类视觉系统有本质差异（人类天然并行，LLM 天然串行）
- **两种解码模式假设**：
  - **直接映射**（嵌套列表、表格、JSON）——结构内嵌于序列，认知负担低
  - **重建解码**（ASCII art）——需要跨多个 token 位置聚合信息，认知负担高
- **原生度排序**：嵌套列表 > Markdown 表格 > Mermaid > 纯文本 > ASCII art 框图 > ASCII art 文字
- **核心类比**："LLM 友好 ≈ 盲人友好"——无障碍设计原则可直接适用于 LLM 友好设计
- **关键洞察**：LLM 没有"视觉预处理"硬件，需要用 attention 软件模拟 2D 空间理解

### Advisor-Gemini

- **示能性错位 (Affordance Mismatch)**：ASCII art 对人类有极高视觉示能性，但对 LLM 是"认知阻尼"
- **双重读者悖论**：文档既要给人看（视觉动物），也要给 LLM 看（序列动物）
- **黄金交集**：嵌套列表是人类和 LLM 都友好的表示形式
- **设计原则**：
  - **语义孪生**：ASCII art 必须有等效文字注释
  - **代码即图表**：Mermaid 是唯一真理（LLM 读源码比读图更开心）
  - **渐进增强**：核心逻辑承载于列表和表格，图表是"增强"而非唯一载体

### Advisor-GPT

- **条款草案核心思想**：
  - `[S-DOC-TEXT-FIRST]` 规范性内容 MUST 以线性可读、可解析的文本形式表达
  - `[S-DOC-SSOT-SINGLE]` 同一事实被多种形式重复时，MUST 选定唯一 SSOT（单一信息源）
  - `[S-DOC-ASCIIART-AVOID]` ASCII art SHOULD NOT 出现在规范文档中
- **现状核查**：`jam-session-guide.md` 和 `mvp-design-v2.md` 存在 ASCII art 作为唯一载体的问题
- **实施路线图**：P0 修基座规范 → P1 修 SSOT 文档破口 → P2 轻量 lint 防复发
- **反对双写**：Mermaid 本身就是线性 SSOT，不应强制再配文字（会制造漂移）

### 实验结果

**实验 A：ASCII art 文字识别**
- 简单 Block 风格：识别率惊人（可能因训练数据中见过大量 figlet 输出）
- 复杂风格（斜体、装饰）：识别率下降，出现漏字

**实验 B：同构状态机理解**
- Mermaid / 表格 / 纯文本：100% 准确
- ASCII art 框图：**误解箭头方向**（把 Ready→Idle 误认为 Loading 的出边）

**监护人修正**：
- 实验设计存在缺陷（提示过多、生成质量不稳定、字符数错误）
- ASCII art 状态机的错误不在阅读理解，而在**生成/解码过程的"口误"**
- Block 风格表现好可能是因为**专项训练过**

## 达成的共识

- **原生度排序**：嵌套列表 🥇 > Markdown 表格 🥈 > Mermaid 🥉 > 纯文本 > ASCII art 框图 ⚠️ > ASCII art 文字 ☠️
- **ASCII art 框图有严重方向歧义风险**——实验证明箭头方向容易被误解
- **Mermaid 和表格是"黄金标准"**——语义化、无歧义、双方友好
- **ASCII art 应尽量避免用于记录信息**
- **设计原则三条**：
  1. 语义孪生：ASCII art 必须有等效文字
  2. 代码即图表：优先 Mermaid
  3. 渐进增强：核心信息在列表/表格，图表是增强

## 待解决问题

- 将条款草案正式写入 `spec-conventions.md` 第 3 章
- 修复 `jam-session-guide.md` 的 ASCII 图（替换为 Mermaid 或降级为 Illustration）
- 修复 `mvp-design-v2.md` 的位布局框图（显式标注 Illustration，声明公式/表为 SSOT）
- 设计轻量 lint 脚本检测 box-drawing 字符

## 监护人指导方向

### "2+N" 框架（条款精简）

监护人提出将 GPT 的条款草案精简为 "2+N" 结构：

**2 条核心原则**：
1. **SHOULD NOT 使用 ASCII art**（非 MUST NOT，允许创作时先自由表达）
2. **避免双写**：一份信息尽量只用一种形式表达

**N 条形式选择指导**（待确定）：
- "当要表达状态机时，如果有 xx 特征，则优先用 xx 形式"
- "当要表达关系图时，如果有 xx 特征，则优先用 xx 形式"
- ...

### "辅助皮层"概念（深远洞察）

监护人的核心洞察：

> "如果能建立起一种 token 序列和'树'/'图'甚至其他抽象形式的双向映射，借助外部代码，你们 LLM 的抽象能力会大幅提高！想象一下如果没有发明那些数学和自然科学符号，我们人类能有今天的抽象思考能力？我觉得不行！"

**类比**：
```
人类大脑 + 纸笔/棋盘/CAD = 增强的人类
LLM + 树形存储/AST引擎/图变换器 = 增强的 LLM（"反向赛博格"）
```

**候选应用**：
1. **记忆记事本** → 树形外部存储（可操作、可检索、可渲染）
2. **Roslyn 接入** → AST + 语义模型 + 标准变换

---

**经验教训**：当 SubAgent 返回报错时，有可能已经完成了部分任务，只是没能完成最终汇报——可以检查 side effect 是否留下了工作结果。

