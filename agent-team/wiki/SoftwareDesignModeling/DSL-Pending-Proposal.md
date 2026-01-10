## term `ATX-Matter` 条款装饰元素
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