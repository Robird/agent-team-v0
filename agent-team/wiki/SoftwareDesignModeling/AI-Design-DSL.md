条款格式系统DSL化

## 思路
规则代码解析部分：英文单词和ASCII符号表示术语和解析结构。
人类和LLM阅读文本：用中文和中文符号写语义解释。用中文的：引号、冒号、括号、顿号，降低解析器复杂性。需要表示ASCII符号时用转义序列表达。
**TODO:定义具体用哪种转义序列。**
英文冒号后尽量不跟空格，以显著与中文冒号视觉区分。
实现层解析时，先用NuGet Markdig解析得到`Markdig.Syntax.Block`序列。我们再组装各`Block`构建语义模型。
**TODO:改写为条款格式**

## 核心设计目标
- Markdown兼容：DSL必须是合法GFM Markdown，可被现有工具渲染
- 定义与引用区分：语法上显式区分，支持形式化解析
- 依赖关系显式化：支持构建条款依赖图
- 后续逐步工具链集成：与DocGraph深度集成
**TODO:改写为条款格式**

---

## term `TODO-Element` 待办事项
用于记录待办事项。

### design [F-TODO-FORMAT] 定义`TODO-Element`的格式
是EmphasisInline，是加粗（`IsDouble`），内容（外层星号的内部）精确以`TODO:`开头。不支持前导空白。不要求冒号后有空白。

---

## term `ATX-Node` 标题节点
用于将Markdown从块级元素序列，以ATX Heading为单位，构建为节点序列。
由一个Markdown ATX Heading自身及其后直至下一个ATX Heading(或EOF)之间的所有文本复合而成。
开头的ATX Heading作为`ATX-Node`的Heading。
`ATX-Node`具有深度`Depth`（取嵌套深度语义），定义为其ATX Heading中的井号`#`个数。
ATX Heading下的所有其他内容定义为`ATX-Node`的内容`Content`。

## term `ATX-Tree` 标题树
用于将`ATX-Node`按照各自的深度构建为一棵节点树。
对于任意一个`ATX-Node` X，沿着节点序列向前逐个检查节点Y，若Y的深度小于X的深度，则终止迭代，并且定义Y是X的父节点。
所有无显式父节点的节点，都是其所在文档的抽象根节点的子节点。
**TODO:研究要不要保留此概念。`ATX-Tree`这个概念目前还没啥用，考虑不正式使用**

---

## term `Term-Node` 术语概念
是一种`ATX-Node`，且具有特定的Heading文本模式。
用于承载一个设计概念。

### term `Term-ID` 术语名字
是`Term-Node`的文本形式标识符。

### design [F-TERM-ID-FORMAT] 定义`Term-ID`格式
最外层用ASCII反引号\`包裹，内部采用逐个单词首字母大写形式（Title-Kebab）英文单词，首字母缩写可全大写（如：`DNA`、`ID`等）。
多单词时 MUST 使用连字符（'-'）连接。
不支持下划线但支持数字。
**TODO:再想想办法，首字母缩写可全大写这个豁免条件其实很难做严格。不过这是英语的老大难问题了，暂时不追求那么精确，又不耽误使用。**

### design [F-TERM-DEFINITION-FORMAT]
`Term-Node`的Heading正文中以`term`关键字开头，空白后紧跟`Term-ID`，(可选)空白后再跟Title。

---

## term `Clause-Node` 条款
是一种`Term-Node`，且具有特定的Heading文本模式。
用于承载一个设计约束。
按职能不同，其具有3种亚型：`Decision-Clause`(决策)、`Design-Clause`(服务于`Decision-Clause`的关键设计)、`Hint`(可由`Decision-Clause`和`Design-Clause`推导得出的信息)。

### term `Clause-ID` 条款编号
是`Clause-Node`的文本形式标识符。

### design [F-CLAUSE-ID-FORMAT] 定义`Clause-ID`的格式
最外层采用ASCII方括号`[]`包裹，内部采用全大写形式英文单词。
多单词时 MUST 使用连字符（'-'）连接。
不支持下划线但支持数字。

### term `Clause-Modifier` 职能修饰符
`Clause-Modifier`是一种关键字，不可省略，用于表述`Clause-Node`的亚型。
`Clause-Modifier`必须全部小写。必须是单个英文单词。
*`Clause-Modifier`目前有3种：`decision`、`design`、`hint`。*

### design [F-CLAUSE-DEFINITION-FORMAT] `Clause-Node`的定义格式
`Term-Node`的Heading正文中以`Clause-Modifier`开头，空白后紧跟`Clause-ID`，(可选)空白后再跟Title。

---

### term `Decision-Clause` 决策型`Clause-Node`
是`Clause-Node`，且`Clause-Modifier`为`decision`。
用于建模设计的目标。
是条款有向图的根节点，相当于公理。
*通常AI不应主动自己修改`Decision-Clause`。*

### term `Design-Clause` 设计型`Clause-Node`
是`Clause-Node`，且`Clause-Modifier`为`design`。
为了达成`Decision-Clause`而消除设计不确定性的关键约束（把软件设计看作规划过程、设计选择就是变量约束）。
当`Design-Clause`与`Decision-Clause`冲突时，应调整`Design-Clause`。
*修订各种`Design-Clause`是AI的主要工作领域。*

### term `SSOT-Clauses` SSOT集合
由`设计项目`内所有的`Decision-Clause`和`Design-Clause`构成的集合。
**TODO:研究要不要把`设计项目`也定义为`Term-Node`。设计文件多了去了，肯定要分成多个处理单元的。设计文件互相之间有引用，处理单元通常是一组文件。**

### term `Hint-Clause` 提示型`Clause-Node`
是`Clause-Node`，且`Clause-Modifier`为`hint`。
用于提示使用者那些确定但不够明显的设计信息。
`Hint-Clause`必须可以由`SSOT-Clauses`推导得出。是一种提示性信息，是可选和可重建的。

---

## term `Clause-Matter` 条款装饰元素
用于承载该 Clause 的修饰信息（attributes）。通常是对其他`Clause-Node`的依赖关系。
是可选信息。
内容是YAML。
*选Matter这个词纯粹是为了拉近与front-matter的关系，让LLM感到熟悉。*

### design [F-CLAUSE-MATTER-FORMAT] 定义Clause的元信息格式
是Markdown fenced code block, 且info string 必须是自定义标记`clause-matter`，且**紧跟**在`Clause-Node`的Heading下面。
**紧跟**指ATX Heading的下一行必须是fence起始行，两行之间只有一个`\n`，不允许空行、注释行、或其它块级元素插入。

### hint [H-CLAUSE-MATTER-EXAMPLE] 条款元信息示例
```clause-matter
note:注意这条Markdown fence紧跟在[H-CLAUSE-MATTER-EXAMPLE]条款的标题下。
```
