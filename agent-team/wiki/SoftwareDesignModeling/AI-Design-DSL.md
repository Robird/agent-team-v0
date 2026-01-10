# AI-Design-DSL

> 本文档定义 **Design-DSL**：一种保持 GFM Markdown 兼容的、可被工具形式化解析的设计文档 DSL，用于把规范文本中的**术语、条款与引用**建模为结构化数据。
> 
> 它固化三类条款层级（`decision` / `spec` / `derived`），并规定 `@Term-ID`、`@Clause-ID` 等可机读引用与依赖表达，以支持构建条款依赖图/文件依赖图并对接 DocGraph 等工具链。
> 
> 本文关注**语法与解析规则（machine-readable / tooling）**；在满足 DSL 形式约束前提下的写作规范与表示约定由 [spec-conventions.md](../../../atelia/docs/spec-conventions.md) 负责。

---

## term `Design-DSL` 设计DSL
用于将AI辅助的软件设计文档形式化的领域特定语言。

### decision [S-MARKDOWN-COMPATIBLE] 兼容GFM
@`Design-DSL` MUST 是合法 GFM Markdown，可被 Markdig 解析。

### decision [S-FORMAL-PARSING] 形式化解析支持
@`Design-DSL` MUST 在语法上显式区分定义与引用，支持形式化解析。

### decision [S-DEPENDENCY-GRAPH] 依赖关系显式化
@`Design-DSL` MUST 支持构建条款依赖图。

### decision [S-DOC-GRAPH-COMPATIBLE] 工具链集成
@`Design-DSL` MUST 支持后续逐步与 DocGraph 工具链深度集成。
目前主要是不能侵占DocGraph所依赖的front-matter。

### decision [S-LANGUAGE-SEPARATION] 语言分离原则
面向规则程序解析的部分：使用英文单词和ASCII符号表示术语和解析结构。
面向人类和LLM语义解释：优先使用中文和中文符号（例如中文的：引号、冒号、括号、顿号）。

### spec [F-PARSING-STRATEGY] 解析策略
实现层解析时，先用NuGet Markdig解析得到`Markdig.Syntax.Block`序列，再组装各`Block`构建语义模型。

---

## term `TODO-Element` 待办事项
用于记录待办事项。

### spec [F-TODO-FORMAT] 定义@`TODO-Element`的格式
是EmphasisInline，是加粗（`IsDouble`），内容（外层星号的内部）精确以`TODO:`开头。

@`TODO-Element` MUST NOT 支持前导空白。
@`TODO-Element` MUST NOT 要求冒号后有空白。

---

## term `Heading-Node` 标题节点
一个@`Heading-Node`由以下部分复合而成：
- `Heading`属性，其内容为Markdown Inline元素组成的序列。
- `Content`属性，其内容为Markdown Block元素组成的序列。
- `Depth`属性，应与嵌套深度正相关。

## term `Root-Node` 根节点
是一种@`Heading-Node`，以文档为单位，由解析器隐式创建，作为整个文档的根节点。它位于任何其他@`Heading-Node`之前。
- `Heading`属性 SHOULD 为文档URI。
- `Content`属性 MUST 来源于文档front-matter之后，首个有效ATX Heading（或EOF）之前的全部块级内容。
- `Depth`属性 MUST 为0。

## term `ATX-Node` ATX节点
是一种@`Heading-Node`，由解析器为每一个**源ATX Heading**创建。
- `Heading`属性 MUST 等效于对应**源ATX Heading**的文本按以下步骤导出得到：
  1. 去除开头 ATX opening sequence（正则表达式`^#+`）。
  2. 去除 ATX closing sequence（正则表达式`#+$`）。
  3. 去除中间结果的首尾空白。
  4. 将结果按 GFM inline 规则解析为行内元素序列。
- `Content`属性 MUST 等效于对应**源ATX Heading**之后，下一个有效ATX Heading（或EOF）之前的全部块级内容。
- `Depth`属性 MUST 为**源ATX Heading**中开头 ATX opening sequence的井号`#`个数。*注意！为了简单性，此Depth并非有效嵌套层级，而是仅看字面量。*

## term `ATX-Tree` 标题树
将文档包含的所有@`Heading-Node`按照嵌套关系构建而成的节点树。
对于任意一个@`ATX-Node` X，定义其上方首个`Depth`属性更小的节点Y，为X的父节点。*由于我们定义了@`Root-Node`位于最前端，所以若X前面没有任何显式节点，则Y就是@`Root-Node`*

---

## term `Term-Node` 术语概念
是一种@`ATX-Node`，且具有特定的Heading文本模式。
用于承载一个设计概念。

### term `Term-ID` 术语名字
是@`Term-Node`的文本形式标识符。

### term `Term-ID-Literal` 术语名字字面量
是@`Term-ID`的字面量写法：在@`Term-ID`外层包裹一对ASCII反引号\`\`。
@`Term-ID-Literal`主要用于@`Term-Node`的定义（`Heading`属性模式匹配）与引用（便于文本搜索与阅读）。

### spec [F-TERM-ID-FORMAT] 定义@`Term-ID`格式
采用逐个单词首字母大写形式（Title-Kebab）英文单词，首字母缩写可全大写（如：`DNA`、`ID`等）。
多单词时 MUST 使用连字符（'-'）连接。
@`Term-ID` MUST NOT 支持下划线。
@`Term-ID` MAY 包含数字。
*首字母缩写可全大写这个豁免条件其实很难做严格。这是英语的老大难问题了，暂时不追求精确，不耽误使用。*

@`Term-ID-Literal`的格式为：\` + @`Term-ID` + \`。

### spec [F-TERM-REFERENCE-FORMAT] 定义引用@`Term-Node`的格式
是一种Markdown Code Inline Element。

引用@`Term-Node`时，MUST 显式使用前导符号`@`。
其最小形式为：字符`@`后紧跟一个@`Term-ID-Literal`。

### spec [F-TERM-DEFINITION-FORMAT]
@`Term-Node`的`Heading`属性以`term`关键字开头，空白后紧跟@`Term-ID-Literal`，(可选)空白后再跟Title。

---

## term `Clause-Node` 条款
是一种@`Term-Node`，且具有特定的Heading文本模式。
用于承载一个设计约束。
按职能不同，其具有3种亚型：@`Decision-Clause`(决策)、@`Spec-Clause`(服务于@`Decision-Clause`的关键规格)、@`Derived-Clause`(可由@`Spec-Clauses`推导得出的信息)。

### term `Clause-ID` 条款编号
是@`Clause-Node`的文本形式标识符。

### term `Clause-ID-Literal` 条款编号字面量
是@`Clause-ID`的字面量写法：在@`Clause-ID`外层包裹一对ASCII方括号`[]`。
@`Clause-ID-Literal`主要用于@`Clause-Node`的定义（`Heading`属性模式匹配）与引用（便于文本搜索与阅读）。

### spec [F-CLAUSE-ID-FORMAT] 定义@`Clause-ID`的格式
采用英文单词。
多单词时 MUST 使用连字符（'-'）连接。
@`Clause-ID` MUST NOT 支持下划线。
@`Clause-ID` MAY 包含数字。
@`Clause-ID` 在匹配与引用时 SHOULD 不区分大小写（Case-Insensitive）。
*注：全大写通常是内容层的风格约定，并非解析器的硬性语法约束。*

@`Clause-ID-Literal`的格式为：`[` + @`Clause-ID` + `]`。

### spec [F-CLAUSE-REFERENCE-FORMAT] 定义引用@`Clause-Node`的格式
引用@`Clause-Node`时，MUST 显式使用前导符号`@`。
其最小形式为：字符`@`后紧跟一个@`Clause-ID-Literal`。

### derived [F-CLAUSE-REFERENCE-LINK] 对@`Clause-Node`的引用可以是LinkInline
对@`Clause-Node`的引用 MAY 写成合法的 GFM Markdown 链接，以携带 url（例如文件路径、锚点等）：
- inline link：`@` + @`Clause-ID-Literal` + `(` + url + `)`
- reference-style link：`@` + @`Clause-ID-Literal`，并在文档其他位置提供对应的Link Reference Definition。

### term `Clause-Modifier` 职能修饰符
@`Clause-Modifier`是一种关键字，用于表述@`Clause-Node`的亚型。采用全小写单个英文单词形式。
*@`Clause-Modifier`目前有3种：`decision`、`spec`、`derived`。*

### spec [F-CLAUSE-MODIFIER-CONSTRAINT] @`Clause-Modifier`格式约束
@`Clause-Modifier` MUST NOT 省略。
@`Clause-Modifier` MUST 全部小写。
@`Clause-Modifier` MUST 是单个英文单词。

### spec [F-CLAUSE-DEFINITION-FORMAT] @`Clause-Node`的定义格式
@`Term-Node`的`Heading`属性以@`Clause-Modifier`开头，空白后紧跟被ASCII方括号`[]`包裹的@`Clause-ID`（即@`Clause-ID-Literal`），(可选)空白后再跟Title。

---

### term `Decision-Clause` 决策型@`Clause-Node`
是@`Clause-Node`，且@`Clause-Modifier`为`decision`。
用于建模设计的目标。
是条款有向图的根节点，相当于公理。
*通常AI不应主动自己修改@`Decision-Clause`。*

### term `Spec-Clause` 规格型@`Clause-Node`
是@`Clause-Node`，且@`Clause-Modifier`为`spec`。
为了达成@`Decision-Clause`而消除设计不确定性的关键规格约束（把软件设计看作规划过程、规格选择就是变量约束）。
当@`Spec-Clause`与@`Decision-Clause`冲突时，应调整@`Spec-Clause`。
*修订各种@`Spec-Clause`是AI的主要工作领域。*

### term `Normative-Clauses` 规范性条款集合
由`设计项目`内所有的@`Decision-Clause`和@`Spec-Clause`构成的集合。
**TODO:研究要不要把`设计项目`也定义为@`Term-Node`。设计文件多了去了，肯定要分成多个处理单元的。设计文件互相之间有引用，处理单元通常是一组文件。**

### term `Derived-Clause` 推导型@`Clause-Node`
是@`Clause-Node`，且@`Clause-Modifier`为`derived`。
由@`Normative-Clauses`推导得出的设计信息，用于提示使用者那些确定但不够明显的约束。是可选和可重建的。

### spec [S-DERIVED-CLAUSE-DERIVABLE] @`Derived-Clause`可推导性约束
@`Derived-Clause` MUST 可以由 @`Normative-Clauses` 推导得出。是一种推导性信息，是可选和可重建的。

---

## term `Clause-Matter` 条款装饰元素
用于承载该 Clause 的修饰信息（attributes）。通常是对其他@`Clause-Node`的依赖关系。
是可选信息。
内容是YAML。
*选Matter这个词纯粹是为了拉近与front-matter的关系，让LLM感到熟悉。*

### spec [F-CLAUSE-MATTER-FORMAT] 定义Clause的元信息格式
是Markdown fenced code block, 且info string MUST 是自定义标记`clause-matter`，且 SHOULD 紧跟在@`Clause-Node`的Heading下面。
解析器 SHOULD 容忍在Heading和clause-matter之间出现空行，但为了最佳可读性，建议不要插入其他内容。

### derived [F-CLAUSE-MATTER-EXAMPLE] 条款元信息示例
```clause-matter
note:注意这条Markdown fence紧跟在@[F-CLAUSE-MATTER-EXAMPLE]条款的标题下。
```

---

## term `Summary-Node` 摘要节点
用于机读摘要。
是一种@`ATX-Node`，且 MUST 同时满足如下条件：
  - `Heading`属性 MUST 等于全小写的关键字`summary`或`gist`。
  - `Depth`属性为1。*当前严格是为了避免捕获父链Heading的复杂性。未来可能放宽。*
  - 是@`ATX-Tree`中的叶节点。*当前严格是为了避免子节点的复杂性。未来可能放宽。*
@`Summary-Node` MAY 出现在文档的任何位置。
@`Summary-Node` MAY 出现多个。机读时 MUST 保序聚合。
*机读摘要主要有2种目的：1. 自动多文件聚合；2. 让LLM可以在读取全文之前了解核心信息。*
关键字`gist`在此处是一种详略级别，比普通的摘要更短，仅保留最短的实用性信息。**

---

## term `Import-Node` 导入节点
是一种@`ATX-Node`，用于将外部文档的符号表引入当前文档上下文。
`Heading`属性以关键字`import`开头（建议全小写）。

### spec [F-IMPORT-SYNTAX] 定义导入语法
@`Import-Node` 的 `Heading` 属性 MUST 包含一个合法的 GFM Link 元素，形式为 `[Prefix](Url)`。
- **Prefix (前缀)**: 链接文本（Link Text）即为导入前缀。
- **Url (路径)**: 链接地址即为目标文档路径（通常是相对路径）。
*利用 Markdown Link 语法既保持了可读性/可跳转性，又提供了自然的结构化解析支持。*

### spec [F-IMPORT-SEMANTICS-PREFIX] 前缀导入语义
当导入语法中提供了非空的 **Prefix** 时，解析器 MUST 将目标文档中的所有 @`Term-ID` 和 @`Clause-ID` 加上前缀 `Prefix` 和连字符 `-` 后，合并入当前符号表。
例如：导入 `[Lib](lib.md)`，其中 `lib.md` 有 @`User`，则在当前文档中变为 @`Lib-User`。
*注意：由于 ID 允许大小写混合，Prefix 也支持大小写混合。建议 Prefix 采用与目标库风格一致的命名（如 Title-Case）。*

### spec [S-IMPORT-SCOPE] 导入非传递性（Non-transitive）
为了避免命名空间前缀过长及隐式依赖，导入关系 MUST NOT 具有传递性。
当处理 `import [Prefix](Target)` 时，解析器仅从 Target 文档中提取由 Target 文档**直接定义**的 @`Term-Node` 和 @`Clause-Node`。
Target 文档中通过导入指令引入的外部符号 MUST NOT 被二次导出到当前文档。若当前文档需要使用间接依赖的术语，MUST 显式导入对应的源文档。
*这使得任何文档的上下文依赖都是扁平且显式的，同时也消除了循环导入的解析风险。*

---
