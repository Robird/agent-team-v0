---
archived_from: members/Advisor-Claude/index.md
archived_date: 2025-12-22
archived_by: Advisor-Claude
reason: 记忆维护压缩 - 过程记录归档
original_section: "2025-12-13 ~ 2025-12-15 DocUI 早期研讨会记录"
---

# DocUI 早期研讨会完整记录

> 本文件归档了 Advisor-Claude 参与 DocUI 早期设计研讨会的完整过程记录。

---

## 2025-12-13 Key-Note 修订研讨会

作为第一轮发言人，陈述了 5 个核心问题：
- Attention Focus 机制缺乏正式定义
- LLM 调用 3 层模型命名与可视化缺失
- "Render" 术语过于宽泛
- DocUI 使用者缺乏统一术语
- Abstract-Token 方案未决

**第二轮交叉提问**：
- 赞同 Gemini 的 "Projection" 命名建议，指出其与 Event Sourcing 的领域对齐
- 赞同 GPT 的术语治理 / SSOT 提议，建议指定 llm-agent-context.md 为术语权威源
- 对 "标准 UI 组件库" 保留意见（时机问题 + Key-Note 职责边界）
- 提出 "App" 简称约束方案：引入上位词 Capability Provider
- 整理了 P0-P3 优先级的修订建议清单

**第三轮具体修订建议**：
提出 4 个可操作的修订建议：
1. 在 SSOT 文件末尾建立术语注册表（包含术语、定义、别名、弃用标记、实现映射）
2. 将 "Render" 重命名为 "Context Projection" 并定义输入/输出契约
3. 在 app-for-llm.md 引入 Capability Provider 概念，消除 "App" 简称歧义
4. 消除 doc-as-usr-interface.md 中的 ObservationMessage 术语泄漏

**实施修订（建议 1, 2, 3）**：
执行研讨会通过的前 3 条建议：
1. ✅ 创建 glossary.md 作为术语 SSOT
2. ✅ 将 `Render` 重命名为 `Context-Projection`（含输入/输出契约）
3. ✅ 引入 `Capability-Provider` 概念，明确 "App" 简称仅指外部扩展

---

## 2025-12-14 术语治理架构研讨会

集中式 glossary.md 在实际写作中暴露问题：破坏文档内聚性。

**核心洞察**：混淆了"术语注册表"和"概念定义"两个职责。
- 概念定义应在引入该概念的文档中
- glossary 应只是索引（指向定义位置 + 一句话摘要）

提出 **Primary Definition + Index 模式** 作为改进方案。

---

## 2025-12-14 术语治理 DSL 洞察

用户 @刘世超 点明我们实际上在设计一种 DSL。

**语义操作映射**：
- `Define`: Primary Definition (`## Term` + `> **Term** ...`)
- `Index/Export`: Glossary 索引条目
- `Reference/Import`: Restatement with link
- `Alias + @deprecated`: Deprecated alias

**与 DocUI 的深层关联**：
- 概念图谱即 DocUI 的信息模型原型
- 术语依赖关系为 LOD 切分提供语义边界
- 静态分析可为 LLM Agent 提供"概念图谱内省"能力

**前人成果评估**：Sphinx glossary directive、JSDoc `@see`/`@link`、SKOS 等可借鉴语法设计，但需要 Markdig 自建核心功能。

**HTML 类比**：我们可能正在为 LLM-Native 信息系统奠定基础数据模型。

---

## 2025-12-14 UI-Anchor 研讨会洞察

参与第二轮交叉讨论，产生几个重要认知更新：

**1. 撤回 State-Anchor 独立概念**
Gemini 的 `obj:type:id` 语义增强方案更优雅，可用 `obj:state:cpu_load` 覆盖状态锚定需求，无需新增锚点类型。

**2. Wizard 触发的双重性**
- Error Recovery（被动触发）
- Deliberate Confirmation（主动触发，高危操作强制协作）

**3. MVP-2 范式跃迁风险**
Call-Only DSL 不仅是技术实现变化，更是 LLM 交互模式变化，需要拆分为 后端就绪(2a) + 交互层适配(2b) 两步。

**4. 新问题：动作序列语义**
多个动作涉及同一锚点时的原子性/顺序性问题尚未定义。

---

## 2025-12-15 Tool-As-Command 畅谈洞察

参与秘密基地畅谈，探讨 Micro-Wizard 的落地实现方案。核心洞察：

**1. Command 是 "CPS 化的协程"**
工具执行从同步函数变为状态机，本质上是把隐式调用栈（generator/async）
转换为显式状态+挂起数据。这是 Continuation-Passing Style 的经典应用。

**2. yield 的传播语义**
内层 Command 的 yield 会冒泡到外层 AgentEngine，导致整个系统进入
WaitingInput 状态。类似 async/await 的"传染性"——一处 await，处处 async。

**3. Error-Feedback = Algebraic Effects**
Level 1/2 错误恢复本质是"带恢复点的异常处理"。传统异常抛出就不回来，
而 Wizard 是"抛出去，等外界帮忙，然后继续"——这正是代数效应的语义。

**4. 对 DocUI 概念体系的影响**
- Tool-Call 定义需要扩展（可能产生 Command 状态机）
- 需要新增 Command 作为执行单元概念
- Observation 多了一种来源（Command yield）
- History 需要支持"挂起的 Command"
