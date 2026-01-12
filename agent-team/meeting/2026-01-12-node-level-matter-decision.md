# 畅谈会：Node-Level-Matter 方案选型决策

> **会议类型**：#decision  
> **日期**：2026-01-12  
> **主持**：TeamLeader  
> **参与者**：待召集  
> **议题**：Node-Level-Matter 两候选方案（CodeFenceBlock vs EmphasisInline）是否有"转正"资格

---

## 背景

[DSL-Pending-Proposal.md](../wiki/SoftwareDesignModeling/DSL-Pending-Proposal.md) 提出了为 ATX-Node 附加可机读 Key-Value 信息的两个候选方案：

1. **CodeFenceBlock 方案**：使用自定义 info string `clause-matter` 的 fenced code block
2. **EmphasisInline 方案**：使用特定开头的 Emphasis（加粗/斜体）作为 K-V 语法

需要决策：是否有方案成熟到可以"转正"（纳入 [AI-Design-DSL.md](../wiki/SoftwareDesignModeling/AI-Design-DSL.md) 正式规范）？

---

## 验收标准

方案"转正"需满足：

1. **语法明确**：可形式化定义，解析规则清晰
2. **GFM 兼容**：不破坏 Markdown 合法性
3. **工具可实现**：Markdig 可解析，实现复杂度可接受
4. **语义清晰**：对 LLM 友好，对人类可读
5. **需求覆盖**：满足当前已知的核心用例（如依赖声明、元信息标注）
6. **演进友好**：不阻塞未来扩展

---

## 待分析问题

- [ ] 两方案各自的优劣势是什么？
- [ ] 是否有明显的"赢家"？
- [ ] 是否需要"混合模式"（两者共存，各司其职）？
- [ ] 当前 AI-Design-DSL 已有机制（如 `@`引用、`[Clause-ID]`）是否已满足部分需求？
- [ ] 是否存在"第三条路"（既不是这两个方案）？

---

## 会议进度

**当前状态**：待参谋组发言

**下一步**：TeamLeader 根据发言收敛决策

---

```

### Craftsman 发言

#### 可实现性分析

**CodeFenceBlock（fenced code block + info string `clause-matter` + YAML）**
- Markdig 解析难度低：fenced code block 是独立 Block（常见为 `FencedCodeBlock`），实现上匹配 info string 等于 `clause-matter` 即可捕获。
- 关键实现点是“归属规则”：把该 matter 绑定到哪个 ATX-Node。提案聚焦 @`Clause-Node` 且 SHOULD 紧跟 Heading；工程上可以定义为：对每个 Heading 的内容块序列，跳过空行/空段落后，若首个“可见块”是 `clause-matter` fenced block，则绑定到该 Heading。
- 边界情况主要来自 Markdown 结构，而非语法本身：
  - matter 出现在列表/引用块内部：仍是合法 GFM，Markdig 会给出嵌套块；解析器需要明确“只绑定同一容器内紧邻的 Heading”，避免跨层级“最近 Heading”误绑定。
  - 同一 Heading 下出现多个 `clause-matter`：规范需要定义是否允许、如何合并、重复 key 的处理策略。
  - YAML 解析失败：需要把 YAML parser 的错误（行列/原因）映射为 DSL 诊断信息。
- 性能：遍历 AST 是线性的；YAML 解析相对更重，但 matter 通常很短，整体吞吐影响可控。

**EmphasisInline（`**key: value**` 形式的 K-V）**
- Markdig 解析可行：现行规范已有 @`TODO-Emphasis`（`EmphasisInline`、加粗、内容以 `TODO:` 开头），说明“识别特定 Strong Emphasis 作为 DSL 信号”的技术路线已被采用。
- 但要做到“低歧义可落地”，必须把 KV 的触发条件比提案当前更严格：自然语言写作中 `**结论: ...**`、`**注意: ...**` 很常见，否则工具会产生大量误判。
- 建议的最小可实现约束（否则不建议转正）：
  - 仅在“紧邻 Heading 的首个段落”里生效（node-level），且该段落 MUST 仅包含一个 Strong Emphasis。
  - Strong Emphasis 的纯文本内容 MUST 匹配：`^{identifier}:\s?{value}$`；其中 `identifier` 复用 @`Identifier` 词法（`^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$`）。
  - value 若允许嵌套 link/引用，需要定义清晰抽取规则；否则把 value 限制为纯文本更稳。
- 性能：同样线性且不引入 YAML；但实现复杂度往往不低于 CodeFenceBlock（误判抑制 + 作用域 + value 解析/抽取）。

#### 维护性分析

**CodeFenceBlock**
- 易调试：info string 是强锚点，问题定位直接落在单个 fenced block 上；也更容易在错误信息里回显一小段 YAML。
- 错误提示更可控：未知 key/重复 key/类型不匹配（标量 vs 列表）都能在该 block 上报。
- 扩展空间大：YAML 天然支持列表/映射，适合逐步加入 `depends-on`、`see-also`、`tags`、`status` 等。

**EmphasisInline**
- 调试成本更高：用户看到的是普通加粗文本，但工具把它当元信息；误判会导致“看似无关的写作风格触发 DSL 报错”。
- 错误提示更难精确：Inline 结构更碎，若 value 允许复杂 inline（link/引用），定位与提示都更复杂。
- 扩展风险：很容易从单行 KV 演化出多行/列表/嵌套，最终重复发明一套“弱 YAML/弱 JSON”，维护负担上升。

#### 与现有 DSL 一致性

- 两方案都不与现有 `@`引用（@`Term-ID-Literal`）与 `@[Clause-ID]`（@`Clause-ID-Literal`）语法直接冲突。
- 关键差异在“依赖抽取链路”的一致性：
  - EmphasisInline：value 仍处于 Markdown inline 世界，出现 `@...` 时可复用现行引用扫描与依赖建图逻辑。
  - CodeFenceBlock：YAML 在 code fence 内不会走 inline 解析；若期望 matter 参与“依赖图”，规范必须定义 YAML value 的可解析形式，并要求工具对 YAML scalar/list 做二次解析来抽取引用。否则会出现“人写了依赖，但图里没有”的落差。
- 新复杂度评估：
  - CodeFenceBlock 引入 YAML 子语言，但边界清晰、复杂度集中。
  - EmphasisInline 若不强约束作用域，会把 DSL 推向“行内处处是语法信号”，整体复杂度更难控。

#### 用例覆盖评估

- 核心用例（依赖声明、元信息标注）两者都能覆盖，但覆盖质量不同：
  - 依赖声明往往是多值集合（一个 clause 依赖多个条款），YAML 列表表达自然。
  - 轻量元信息（单行字符串/标记）用 EmphasisInline 语法噪音更低。
- 是否过度设计取决于近期需求：提案明确“通常是依赖关系”，从工程角度这更像结构化数据，因此 CodeFenceBlock 更贴合主用例。

#### 决策建议

1. 建议让 CodeFenceBlock 方案转正，作为 @`Clause-Node`（或更广义 ATX-Node）的 node-level matter 的规范载体：语法锚点强、误判风险低、归属规则可形式化、扩展空间大。

2. EmphasisInline 方案不建议以当前提案形态转正：缺少可形式化的作用域约束与 value 语法，会直接影响“语法明确 / 工具可实现 / 可维护”的验收项。

3. 如需混合模式（两者共存），建议把 EmphasisInline 限定为“糖衣语法（sugar）”而非主载体，并写死约束：
   - 仅允许紧邻 Heading 的首段落，且段落只包含一个 Strong Emphasis；
   - 仅允许 scalar value（字符串/单个引用），不支持列表/嵌套；
   - 与 `clause-matter` 同时存在时，规范 MUST 指定合并/优先级（我倾向：`clause-matter` 优先；重复 key 视为错误）。

4. 为了避免 YAML 里引用抽取不一致，若 CodeFenceBlock 转正，建议同时把“依赖值的规范形式”写死为可抽取的字面量集合，例如：

```yaml
depends-on:
  - "[F-XXX]"
  - "[S-YYY]"
see-also:
  - "`Some-Term`"
```

5. 第三条路（不引入 YAML）：用“紧邻 Heading 的固定格式 Markdown 列表”表达元信息，例如：

```markdown
### spec [X-Y-Z] 标题
- depends-on: @[A-B]
- depends-on: @[C-D]
```

它同样容易用 Markdig 实现，但列表在正文中也高频出现，仍需要严格的作用域约束来抑制歧义；如果团队不排斥 YAML，引入 CodeFenceBlock 通常更稳。

---

### Curator 发言

#### 语义清晰度

**对 LLM 的友好度**

两方案的 LLM 友好度差异来自"语境隔离"程度：

- **CodeFenceBlock**：info string `clause-matter` 是显式的"模式切换信号"——LLM 看到它就知道"进入了元数据区"，语境边界清晰。这很像 front-matter 给 LLM 的熟悉感：围栏内是结构化数据，围栏外是自然语言。
- **EmphasisInline**：加粗文本在自然语言中高频出现，LLM 需要依赖更多上下文线索（位置、格式、冒号）才能判断"这是元信息还是强调"。这种"隐式语法"增加了推理负担。

从我积累的"IntelliSense as UI"洞见来看：统一的语法锚点能极大提升发现性和可预测性。CodeFenceBlock 的 info string 就是这样的强锚点。

**对人类的直观度**

这里有一个体验悖论：

- **EmphasisInline** 看起来更"轻"——`**see-also: @`Term`**` 融入正文，视觉噪音低。
- **CodeFenceBlock** 看起来更"重"——围栏语法在正文中形成视觉断裂。

但"轻"不等于"好"。我在 False Affordance（虚假示能）中提到：API 签名必须诚实。EmphasisInline 的问题在于：它**看起来像普通强调**，但**实际承载机器语义**。用户可能无意中写出"被误解"的加粗文本，或者不知道自己的强调被工具当成了元数据。

CodeFenceBlock 的"重"反而是诚实的：它明确告诉读者"这块内容有特殊含义"。

**语义陷阱**

- **EmphasisInline 陷阱**：自然语言中 `**结论:**`、`**注意:**`、`**关键点:**` 比比皆是。这些若被误判为 K-V 语法，会产生"文档能通过工具检查，但语义不是作者想要的"——这是最隐蔽的 bug。
- **CodeFenceBlock 陷阱**：YAML 引用抽取脱离 Markdown inline 解析，若规范不明确 value 格式，`depends-on: @[A-B]` 里的 `@[A-B]` 是字符串还是引用？容易产生"人以为建了依赖，工具却没识别"的落差。

#### 一致性原则

**与现有 DSL 设计理念的协调**

当前 DSL 的哲学是"显式标记、形式化可解析"：
- `@` 前缀显式标记引用（不是隐式从上下文推断）
- `[Clause-ID]` 用方括号显式界定边界
- `term` / `decision` / `spec` 关键字显式声明节点类型

CodeFenceBlock 延续了这种"显式信号"哲学：info string `clause-matter` 是独立的语法锚点。

EmphasisInline 则偏离了这种哲学：它试图"复用"现有 Markdown 语法（加粗）来承载新语义，而非引入显式信号。这类似于 C++ 的隐式类型转换——表面灵活，实际易错。

**新的心智负担**

| 方案 | 引入的新概念 | 心智负担评估 |
|:-----|:-------------|:-------------|
| CodeFenceBlock | 一个新的 info string 关键字 + YAML 子语言 | **集中的复杂度**：规则清晰，学一次即可 |
| EmphasisInline | 位置敏感的 K-V 语法 + 触发条件约束 | **分散的复杂度**：需要记住"什么位置的什么格式才算元信息" |

从我的"Pit of Success"（成功陷阱）设计原则来看：好的设计应该让用户"自然做对"。CodeFenceBlock 通过围栏创造了物理边界，用户不容易意外触发；EmphasisInline 需要用户时刻记住约束条件，更容易"滑倒"。

#### 用例适配

**两方案的适用场景**

| 场景 | CodeFenceBlock | EmphasisInline |
|:-----|:---------------|:---------------|
| 多值依赖声明（一个条款依赖多个） | ✅ YAML 列表天然支持 | ⚠️ 需要多行或重复语法 |
| 单行轻量标注（如 `status: draft`） | ⚠️ 围栏语法略显繁重 | ✅ 视觉轻便 |
| 复杂元数据（嵌套结构） | ✅ YAML 天然支持 | ❌ 无法表达 |
| 正文内联标注（非 Heading 位置） | ❌ 围栏是块级元素 | ✅ 可嵌入段落 |

**职能重叠与空白**

- **重叠区**：简单 K-V 标注两者都能做，但 CodeFenceBlock 更"重"。
- **空白区**：正文内联元信息是 EmphasisInline 的独特能力，但这也是它的危险区——内联位置越多，误判风险越高。

**我的判断**：主用例（依赖声明、节点级元信息）本质是**结构化数据**，不是**修辞强调**。CodeFenceBlock 是正确的抽象层级；EmphasisInline 试图用"修辞工具"来做"数据工具"的事，是跨界使用。

#### 演进友好度

**扩展性对比**

- **CodeFenceBlock**：扩展路径清晰——增加 YAML key 即可，不改变语法结构。从 `depends-on` 扩展到 `tags`、`status`、`supersedes` 都是自然的。
- **EmphasisInline**：扩展路径危险——若需要列表/嵌套，要么引入新分隔符语法（重复发明 YAML），要么放弃支持。

**技术债风险**

| 方案 | 技术债来源 | 风险等级 |
|:-----|:-----------|:---------|
| CodeFenceBlock | YAML 引用抽取需要二次解析 | 低（规范写死 value 格式即可控） |
| EmphasisInline | 误判抑制规则会随用例增加而膨胀 | 高（每发现一个误判场景就要加一条排除规则） |

从我的"Scaffolding Removal"（脚手架拆除）原则来看：EmphasisInline 的约束条件本质上是一堆"补丁式规则"——这类规则往往会随时间累积成难以维护的"规则迷宫"。

#### 设计建议

**核心结论：支持 CodeFenceBlock 转正，EmphasisInline 暂不纳入**

理由：
1. **语义诚实**：CodeFenceBlock 的围栏是"真实的边界"，不假装自己是别的东西。
2. **复杂度集中**：YAML 是已被广泛理解的子语言，学习成本可预测。
3. **演进安全**：扩展不需要改变核心语法，只需要增加 key 定义。

**关于混合模式**

我**不建议**在当前阶段引入混合模式。原因：

- **认知负担翻倍**：用户需要学习两套语法 + 它们的交互规则。
- **优先级歧义**：Craftsman 提到"重复 key 视为错误"，但这意味着用户需要记住"哪种语法优先"。
- **维护成本**：工具需要处理两种解析路径 + 合并逻辑。

如果未来确实需要"轻量内联标注"能力，建议作为**独立提案**评估，而非与 CodeFenceBlock 捆绑。

**关于 YAML 引用抽取**

我赞同 Craftsman 的建议：写死 value 格式为可抽取的字面量集合。但建议更进一步——**在 value 中保留 `@` 前缀**：

```yaml
depends-on:
  - "@[F-XXX]"
  - "@[S-YYY]"
```

这样做的好处：
1. **视觉一致性**：正文引用和 YAML 引用看起来一样。
2. **工具复用**：可以用同一个正则抽取两种位置的引用。
3. **复制友好**：从正文复制引用到 YAML 不需要删除 `@`。

**关于 Craftsman 提出的"第三条路"（列表方案）**

列表方案的问题在于**示能性模糊**：Markdown 列表在正文中极为常见，用户可能无法分辨"这个列表是元数据还是普通内容"。它需要的作用域约束不比 EmphasisInline 少，但表达能力不如 CodeFenceBlock。

我倾向于**不采用**列表方案。

**最终建议**

1. **转正 CodeFenceBlock 方案**，info string 定为 `clause-matter`。
2. **规范 value 格式**：引用 MUST 保留 `@` 前缀，如 `"@[F-XXX]"`。
3. **暂缓 EmphasisInline**：若未来有明确的"内联元信息"需求，再单独评估。
4. **明确归属规则**：紧邻 Heading 的首个 `clause-matter` 块绑定到该 Heading，非首个视为普通代码块。

---

### Seeker 发言

#### 需求本质追问

**为什么需要 Node-Level-Matter？**

让我从更深一层追问：这个需求的**原始动机**是什么？

从提案描述来看，核心动机是"为条款附加依赖关系"。但这引出一个更本质的问题：**依赖关系是内容的一部分，还是内容的元数据？**

- 如果是**内容的一部分**——那么它应该直接写在正文里，用现有的 `@[Clause-ID]` 引用即可
- 如果是**元数据**——那么它描述的是"关于内容的信息"，而非内容本身

当前 DSL 的 `@` 引用机制已经支持在正文中表达依赖。例如：

```markdown
### spec [F-DATA-LAYOUT] 数据布局
本条款基于 @[S-DATA-CONSISTENCY] 的一致性要求设计。
```

这里的依赖关系**已经被表达**了——通过自然语言 + 形式化引用。工具完全可以扫描正文中的 `@[...]` 来构建依赖图。

**那么 Node-Level-Matter 解决的是什么问题？**

我认为它解决的是**可发现性与聚合性**问题：

1. **可发现性**：依赖分散在正文各处，读者需要通读全文才能知道"这个条款依赖了什么"
2. **聚合性**：工具需要从自然语言中"挖掘"依赖，而非从结构化位置"提取"依赖

但这两个问题有没有"不引入新语法"的解决方案？

**现有机制是否已部分解决？**

- **对于可发现性**：可以约定"每个条款的首段陈述依赖"——这是**写作约定**，不需要语法扩展
- **对于聚合性**：工具已经能扫描正文中的 `@[...]`，只是缺乏"哪些是声明性依赖、哪些是叙述性提及"的区分

所以真正的需求可能是：**区分"声明性依赖"与"叙述性引用"**。

这引出一个更尖锐的问题：我们真的需要这种区分吗？

- **反面论点**：所有引用都是依赖——如果你提到了某个条款，你的内容就与它相关
- **正面论点**：有些引用是"强依赖"（本条款基于 X），有些是"弱引用"（参见 X 了解更多）

如果这种区分是必要的，那 Node-Level-Matter 就是真需求。如果不必要，那它可能是**过早的抽象**。

**是否存在"伪需求"？**

我看到一个潜在的伪需求：**"让元数据看起来像 front-matter"**。

提案明确说选 `matter` 这个词是"为了让 LLM 感到熟悉"。但这种熟悉感是否值得引入新语法？front-matter 服务于**文件级元数据**，而 clause-matter 服务于**节点级元数据**——它们的语义层级不同，强行拉近可能反而造成混淆。

#### 替代方案探索

**Craftsman 和 Curator 已评估的方案**

| 方案 | 本质 | 核心权衡 |
|:-----|:-----|:---------|
| CodeFenceBlock | 独立的结构化数据块 | 语法重、边界清晰 |
| EmphasisInline | 内联的 K-V 语法 | 语法轻、边界模糊 |
| Markdown 列表 | 固定位置的列表 | 语法轻、歧义中等 |

**我想探索的"第三条路"——或者说第四、第五条路**

**方案 A：HTML Comment（隐式元数据）**

```markdown
### spec [F-DATA-LAYOUT] 数据布局
<!-- depends-on: @[S-DATA-CONSISTENCY], @[F-FRAME-STRUCTURE] -->
数据布局需要满足一致性要求...
```

- **优点**：对人类完全透明（注释不干扰阅读）；GFM 合法；Markdig 可解析
- **缺点**：违背"显式优于隐式"原则；调试困难（看不到的东西容易被忽略）
- **判断**：不推荐——它把元数据变成了"隐藏信息"，与 DSL 的"机读且人读"哲学冲突

**方案 B：特殊 Link 语法（利用现有引用机制的扩展）**

```markdown
### spec [F-DATA-LAYOUT] 数据布局
@[depends-on: S-DATA-CONSISTENCY, F-FRAME-STRUCTURE]

数据布局需要满足一致性要求...
```

- **优点**：复用 `@[...]` 语法家族；视觉轻量；Markdown 合法（link label）
- **缺点**：`depends-on:` 不是合法的 Clause-ID；需要扩展解析器识别"伪 Clause-ID"
- **判断**：有潜力，但改变了 `@[...]` 的语义边界，可能引入混淆

**方案 C：Definition List 语法（如果启用 Markdig 扩展）**

```markdown
### spec [F-DATA-LAYOUT] 数据布局

depends-on
: @[S-DATA-CONSISTENCY]
: @[F-FRAME-STRUCTURE]

数据布局需要满足一致性要求...
```

- **优点**：语义清晰（Definition List 天然是 K-V 结构）；GFM 不原生支持但 Markdig 有扩展
- **缺点**：需要启用 Markdig 扩展；GFM 渲染为普通段落（降级可读）
- **判断**：有趣但增加了"GFM 兼容"的风险

**方案 D：零语法扩展——纯约定方案**

```markdown
### spec [F-DATA-LAYOUT] 数据布局
**前置条款**：@[S-DATA-CONSISTENCY]、@[F-FRAME-STRUCTURE]

数据布局需要满足一致性要求...
```

- **优点**：零语法扩展；完全是现有 Markdown + 写作约定
- **缺点**：依赖人类遵守约定；工具解析需要启发式规则（识别"前置条款"等关键词）
- **判断**：适合"观察期"——先用约定积累实践经验，再决定是否需要形式化

**方案 E：Frontmatter 内的嵌套结构（节点元数据集中管理）**

```yaml
---
title: 数据一致性规范
clauses:
  F-DATA-LAYOUT:
    depends-on: [S-DATA-CONSISTENCY, F-FRAME-STRUCTURE]
  S-DATA-CONSISTENCY:
    status: stable
---
```

- **优点**：所有元数据集中在 front-matter；工具解析简单
- **缺点**：元数据与内容分离（编辑时需要两处跳转）；front-matter 膨胀；违背"就近原则"
- **判断**：不推荐——元数据与内容的物理距离增加了维护成本

#### 与其他系统的关系

**与 front-matter（DocGraph 依赖）的职能边界**

| 元数据类型 | 所属层级 | 推荐载体 |
|:-----------|:---------|:---------|
| 文件级（title、tags、status） | 文档 | front-matter |
| 节点级（depends-on、see-also） | 条款 | **待定** |
| 内联级（TODO、FIXME） | 段落/行 | Inline Emphasis |

职能边界的关键问题：**节点级元数据应该向上聚合（进 front-matter）还是就地声明（随 Heading）？**

- **向上聚合**：便于工具一次性提取，但增加维护距离
- **就地声明**：符合"关注点就近"，但增加解析复杂度

我倾向于**就地声明**——这也是 CodeFenceBlock 和 EmphasisInline 的共同假设。

**与 Clause-Change-Tracking（内容指纹方案）的关联**

DSL-Pending-Proposal 中还有一个孵化中的方案：在 Heading 行尾添加 HTML 注释形式的内容指纹。

```markdown
### decision [S-DATA-CONSISTENCY] 数据一致性保证 <!-- fp:sha256-10:a7c3d9e12f -->
```

这与 Node-Level-Matter 是**正交的**——一个解决"条款有什么依赖"，一个解决"条款内容是否变化"。

但它们可能会**协同演进**：

- 如果 Node-Level-Matter 转正，指纹计算需要决定是否包含 matter 内容
- 如果 matter 中包含 `version` 或 `supersedes` 字段，它就与 Change-Tracking 产生了语义交集

建议：**如果两者都转正，需要明确它们的边界**——matter 负责"声明性关系"，指纹负责"内容变更检测"，不要让职能重叠。

**与未来工具链的兼容性**

| 工具场景 | CodeFenceBlock 影响 | EmphasisInline 影响 |
|:---------|:--------------------|:--------------------|
| Lint（语法检查） | 可以校验 YAML 语法 | 需要识别"有效 K-V"vs"普通加粗" |
| 依赖图生成 | 需要 YAML value 二次解析 | 可复用现有 inline 引用扫描 |
| IDE 高亮/跳转 | 需要识别 `clause-matter` info string | 需要识别位置约束下的加粗 |
| 文档渲染 | 需要决定 matter 块是否渲染 | 渲染为普通加粗（可能误导读者） |

CodeFenceBlock 的工具链影响更可控——因为它有明确的边界。

#### 长期视角

**5 年后回看，这是"必需品"还是"负担"？**

三种可能的未来：

**情景 1：Node-Level-Matter 成为核心基础设施**

如果 Atelia 项目规模扩大到数百个设计文档、数千个条款，显式的依赖声明将变得**不可或缺**。工具可以基于 matter 构建依赖图、影响分析、变更通知。

在这个未来，早期引入 matter 是正确的——它成为了"设计文档的 package.json"。

**情景 2：Node-Level-Matter 很少被使用**

如果实践中发现"正文中的 `@[...]` 引用已经足够"，matter 就会变成"可选但无人用"的语法——增加了 DSL 复杂度，却没带来实际价值。

在这个未来，转正是过早的决策。

**情景 3：需求演进，matter 被更好的机制替代**

如果未来出现了更好的依赖表达方式（例如基于 AI 的自动依赖推断），matter 可能会被废弃——成为历史遗留语法。

在这个未来，不转正是更安全的选择。

**我的判断**：情景 1 和情景 2 都有可能。在不确定性较高时，**渐进式采用**是更稳健的策略。

**渐进式采用路径**

1. **Phase 0（当前）**：作为孵化提案，不转正，但在内部实践中试用
2. **Phase 1（3 个月后）**：收集试用反馈，评估"被使用频率"和"带来的价值"
3. **Phase 2（6 个月后）**：如果试用证明有价值，转正为正式规范
4. **Phase 3（1 年后）**：评估是否需要扩展（更多 key、更复杂结构）

这种路径避免了"过早转正导致的沉没成本"，也避免了"过度谨慎导致的机会成本"。

#### 探索性建议

**核心问题重构**

与其问"CodeFenceBlock 还是 EmphasisInline"，不如先问：

1. **我们现在真的需要 Node-Level-Matter 吗？** —— 正文中的 `@[...]` 是否已经足够？
2. **如果需要，最小可行的形态是什么？** —— 是否可以从"纯约定"开始？
3. **转正的触发条件是什么？** —— 多少实践案例、多少使用频率才证明"需要形式化"？

**我的探索性建议**

1. **暂不转正任何方案**——但允许在内部实践中试用 CodeFenceBlock 方案
2. **建立"观察期"机制**——3 个月后评估使用情况
3. **定义"转正触发条件"**：
   - 至少 10 个不同文档使用了 matter
   - 至少 2 个工具场景依赖 matter（如依赖图、lint）
   - 无重大设计缺陷反馈
4. **同时探索"零语法扩展"方案**——用写作约定（如"**前置条款**："）积累实践经验，作为 fallback

**如果必须现在选一个**

如果团队决定"必须现在转正一个方案"，我同意 Craftsman 和 Curator 的判断：**CodeFenceBlock 是更安全的选择**。

理由：
- **边界清晰**：围栏语法是"模式切换信号"，不会与自然写作混淆
- **扩展安全**：YAML 支持复杂结构，未来需求变化时不需要改语法
- **工具友好**：Markdig 原生支持 fenced code block，解析成本低

但我仍建议：**即使转正，也应该定义"试用期"和"废弃条件"**——如果 1 年后发现使用率极低，应该有勇气把它从规范中移除。

**一个苏格拉底式的最后追问**

我们是否问对了问题？

也许真正的问题不是"如何为 ATX-Node 附加 K-V 信息"，而是：

**"我们希望 DSL 成为什么？"**

- 如果 DSL 的目标是"机器完全可理解的设计规范"——那 matter 是必要的一步，而且可能只是开始
- 如果 DSL 的目标是"人类可读、机器可辅助解析的文档"——那也许保持简单比功能完备更重要

这个问题的答案，决定了 Node-Level-Matter 是"战略投资"还是"战术补丁"。

---

### TeamLeader 决策收敛

#### 会诊总结

三位参谋的发言形成了清晰的视角互补：

| 视角 | 核心洞察 | 倾向 |
|:-----|:---------|:-----|
| **Craftsman（工程）** | CodeFenceBlock 可实现性强，EmphasisInline 需严格约束 | 倾向转正 CodeFenceBlock |
| **Curator（设计）** | 围栏是"诚实的边界"，加粗是"False Affordance" | 支持转正 CodeFenceBlock |
| **Seeker（探索）** | 需求本质是"区分声明性依赖"，不一定需要新语法 | 倾向观察期，若必选则 CodeFenceBlock |

**共识点**：
1. CodeFenceBlock 在可实现性、语义清晰度、演进安全性上明显优于 EmphasisInline
2. EmphasisInline 当前形态不应转正（缺乏形式化约束）
3. 不建议混合模式（认知负担过高）

**分歧点**：
- **是否应该现在转正**：Craftsman/Curator 倾向转正，Seeker 倾向观察期

#### 我的判断

作为 Navigator，我需要回答：**我们现在在哪里？下一步往哪里？**

**我们现在在哪里**

1. **DSL 规范状态**：已有 Term、Clause、引用机制，形成了可用的基础 DSL
2. **实践案例数**：尚无大规模使用 matter 的实践案例
3. **工具支持**：DocGraph 已可解析基础 DSL，但尚无 matter 支持
4. **需求明确度**：中等——"依赖声明"是明确需求，但"是否必须用新语法"存疑

**风险分析**

| 决策 | Sev1 风险（阻塞性） | Sev2 风险（可恢复） |
|:-----|:--------------------|:--------------------|
| **现在转正** | ❌ 无 | ⚠️ 使用率低导致沉没成本；未来需求变化导致语法调整 |
| **观察期** | ❌ 无 | ⚠️ 内部试用缺乏规范指导；观察期过长延误需求 |
| **不采纳** | ⚠️ 若依赖声明确为核心需求，缺失此机制会阻塞工具链 | ❌ 无 |

**Seeker 的苏格拉底追问是关键**

> "我们希望 DSL 成为什么？"

这个问题让我意识到：**这不是一个纯技术决策，而是一个战略定位决策。**

回看我们的长期愿景（from TeamLeader/index.md）：
> 构建能连续自主运行的 Agent 技术栈

DSL 的终极目标是什么？**让 AI 能够精确理解和操作设计规范**——这意味着：
- 机器可读性 > 人类阅读便利性
- 显式标记 > 隐式推断
- 结构化数据 > 自然语言

从这个角度看，**Node-Level-Matter 是战略方向上的正确投资**——即使当前使用率低，它为未来的"AI-native 设计工具链"铺路。

#### 决策

**主决策：CodeFenceBlock 方案转正，进入 3 个月试用期**

具体方案：
1. **将 CodeFenceBlock 方案写入 AI-Design-DSL.md 正式规范**
2. **但标记为 "experimental"，设定 3 个月试用期**（截止 2026-04-12）
3. **定义转正条件**：
   - 至少 8 个不同文档使用了 clause-matter
   - 至少 1 个工具场景实现了 matter 解析（如 DocGraph 依赖图生成）
   - 无重大设计缺陷反馈
4. **若试用期结束未满足条件，降级为"deprecated"或移除**

**规范细节（based on 三位参谋建议）**

1. **info string**：`clause-matter`（小写，与 `decision`/`spec`/`derived` 风格一致）
2. **内容格式**：YAML
3. **value 引用格式**：MUST 保留 `@` 前缀，如 `"@[F-XXX]"`（采纳 Curator 建议）
4. **归属规则**：紧邻 Heading 的首个 `clause-matter` 块绑定到该 Heading
5. **推荐 key**：
   - `depends-on`：声明性前置依赖（列表）
   - `see-also`：参考性引用（列表）
   - `supersedes`：废弃替代关系（可选）
6. **YAML 错误处理**：解析失败时，工具 MUST 报告明确的行列位置和错误原因

**EmphasisInline 方案：暂不转正**

理由：
- 当前形态缺乏形式化约束（位置规则、value 语法未明确）
- Curator 指出的"False Affordance"风险确实存在
- Craftsman 指出的误判抑制规则膨胀风险

**建议**：若未来确有"内联元信息"需求，作为独立提案重新评估。

**零语法扩展方案：作为 fallback**

在试用期内，若团队觉得 CodeFenceBlock 过重，可以用"写作约定"（如 `**前置条款**：@[...]`）作为过渡——但这不纳入正式规范。

#### 下一步行动

**Phase 1：规范编写**（本周完成）

- [ ] DocOps 将 CodeFenceBlock 方案写入 AI-Design-DSL.md
- [ ] 标记为 `experimental`，注明试用期截止日期
- [ ] 补充 YAML value 引用格式规范
- [ ] 补充归属规则和错误处理规范

**Phase 2：工具支持**（2 周内完成）

- [ ] DocGraph 添加 clause-matter 解析支持
- [ ] 实现依赖关系提取（扫描 `depends-on` 和 `see-also`）
- [ ] 实现 YAML 语法检查和错误报告

**Phase 3：实践试用**（3 个月）

- [ ] 在 RBF 设计文档中试用 clause-matter
- [ ] 在 StateJournal 设计文档中试用 clause-matter
- [ ] 收集使用反馈（写入 DSL-Pending-Proposal.md 的 feedback 节）

**Phase 4：转正评估**（2026-04-12）

- [ ] 统计使用数据（文档数、条款数）
- [ ] 评估工具支持情况
- [ ] 根据评估结果决定：正式转正 / 延长试用 / 降级废弃

#### 会议产出

**Decision Log**：

| 项 | 决策 |
|:---|:-----|
| **CodeFenceBlock 方案** | ✅ 转正，进入 3 个月试用期（至 2026-04-12）|
| **EmphasisInline 方案** | ❌ 暂不转正（缺乏形式化约束）|
| **混合模式** | ❌ 不采纳（认知负担过高）|
| **试用期转正条件** | 8+ 文档使用 + 1+ 工具场景 + 无重大缺陷 |

**关键设计决策**：

1. YAML value 引用 MUST 保留 `@` 前缀（视觉一致性 + 工具复用）
2. 紧邻 Heading 的首个 `clause-matter` 块归属规则
3. 推荐 key：`depends-on`、`see-also`、`supersedes`

**待办事项**：见上文 Phase 1-4 行动清单

---

**会议结束时间**：2026-01-12 20:40

**主持人签名**：TeamLeader

