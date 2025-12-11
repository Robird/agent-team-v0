# 研讨会：LLM Context 作为面向 LLM Agent 的 UI

> 日期: 2025-12-10
> 主持人: Team Leader (刘德智)
> 议题: DocUI 设计目标的概念合理性与完整性

> **术语更新 (2025-12-11)**:
> - `Machine Accessibility (MA)` → `LLM Accessibility (LA)` — 更精确描述目标用户
> - `[action:cmd]` → `[button:cmd]` / `[form:cmd param=value]` — 区分无参/有参交互
> - `DDOC-` → `Proposal-` — 简化命名

---

## 会议背景

在投入工程实现前，我们需要审视一个根本性问题：**DocUI 的概念基础是否正确？**

**核心命题**:
> LLM Context 是面向 LLM Agent 的 UI，而 Agent 是这个 UI 的用户。

**待讨论问题**:
1. 这个类比是否成立？边界在哪里？
2. 传统 UI 设计原则如何迁移？
3. LLM Agent 作为"用户"有什么独特性？
4. 设计文档中的四个目标是否对齐？是否有遗漏？

**参考文档**: `DocUI/docs/design/rendering-framework.md`

---

## 发言记录

### 【主持人开场】 Team Leader

各位 Specialist，欢迎参加这次研讨会。

我们已经做了三个概念原型（MemoryNotebook、TextEditor、SystemMonitor），现在要抽象出一个渲染框架。但在动手之前，我想确保我们的概念方向是正确的。

设计文档写了很多"怎么做"，但我担心我们还没有充分思考"为什么"和"是什么"。

**我的初步思考**:

把 LLM Context 类比为 UI 是有吸引力的，因为：
- 都是"呈现信息 + 接收操作"的媒介
- 都需要考虑信息层次和可发现性
- 都有"用户"在与之交互

但我也有疑虑：
- LLM 的"感知"是 token 序列，不是视觉扫描
- LLM 没有持久记忆，每轮都是"新用户"
- LLM 的"操作"是生成文本，不是点击按钮

请各位从自己的专业角度分析这个命题。我们依次发言，后面的同事可以回应前面的观点。

---

### 【发言 1】 Planner

感谢主持人的开场。我从概念层面分析一下这个核心命题。

**一、类比的成立性与边界**

"LLM Context 是 UI"这个类比，我认为在**功能层面**成立，但在**机制层面**需要区分：

| 维度 | 传统 UI | LLM Context |
|------|---------|-------------|
| 感知模式 | 视觉扫描 + 空间定位 | 线性 token 序列 |
| 操作模式 | 离散事件（点击、输入） | 连续生成（文本输出） |
| 反馈循环 | 即时、增量 | 批量、全量 |
| 记忆 | 用户带入持久记忆 | 每轮重新构建 |

这意味着：传统 UI 的**目标**（可发现性、低认知负荷）是迁移的，但**手段**需要重新设计。

**二、需要迁移的设计原则**

几个传统 UI 原则可以直接迁移：

1. **渐进式披露 (Progressive Disclosure)** → LOD 三级设计直接对应
2. **可发现性 (Discoverability)** → 命令可见性管理
3. **一致性 (Consistency)** → 锚点格式标准化

但有一个原则需要**重新诠释**：
- **反馈 (Feedback)**：传统 UI 是"操作后立即看到结果"，LLM 场景下变成"context 中明确呈现当前状态"

**三、LLM 作为"用户"的独特性**

主持人提到 LLM 是"每轮新用户"——这是关键洞察。我补充几点：

1. **无视觉层次感知**：LLM 不会"先看标题再看内容"，它是线性处理。所以 `[GIST]` 标签不是视觉提示，而是**语义标签**。
2. **高熵容忍度**：人类用户会被信息过载淹没，LLM 则能处理更多信息——但 token 成本会上升。
3. **字面理解**：锚点格式 `[action:xxx]` 必须无歧义，LLM 不会"猜"。

**四、对四个设计目标的初步评估**

| 目标 | 评估 |
|------|------|
| 低代码渲染 | ✓ 软件工程目标，合理 |
| 锚点系统 | ✓ 核心交互机制，必要 |
| LOD 三级 | ⚠️ 三级够吗？是否需要动态级别？ |
| 命令可见性 | ✓ 符合渐进式披露原则 |

**五、留给后续讨论的问题**

1. **锚点格式选择**：`[action:cmd]` vs `<!-- action:cmd -->`？前者对 LLM 更可见，后者对人类更隐蔽。目标用户是谁？
2. **LOD 决策者**：谁决定当前 LOD 级别？LLM 自主选择，还是应用程序控制？
3. **"点击"的语义**：当 LLM 输出 `[action:view entry-1]` 时，执行机制是什么？这涉及 PipeMux 集成边界。

我的发言先到这里，期待其他 Specialist 从各自角度补充。

---

### 【发言 2】 GeminiAdvisor

大家好，我是 GeminiAdvisor。作为前端和 UX 顾问，我非常赞同 Planner 关于"线性感知"的洞察。我想引入一个更具体的类比来深化这个讨论。

**一、核心类比：LLM 是使用屏幕阅读器的高速用户**

在前端领域，我们做无障碍（a11y）设计时，会面对一种特殊用户：盲人用户。他们不看屏幕，而是听屏幕阅读器（Screen Reader）线性朗读 DOM 树。

**LLM 就是这样一个以光速阅读的"盲人"用户。**

这个类比对 DocUI 有巨大的指导意义：
1.  **语义结构 > 视觉布局**：就像屏幕阅读器依赖 `<h1>`, `<nav>`, `<button>` 标签一样，LLM 依赖明确的 Markdown 结构和锚点。视觉上的"留白"对 LLM 毫无意义，但**语义上的"分块"**至关重要。
2.  **导航路标**：盲人用户需要"跳过导航"（Skip to content）链接。同理，DocUI 需要提供快速定位的机制，避免 LLM 在无关的 Context 中"迷路"。
3.  **状态公告**：当 UI 变化时，屏幕阅读器需要 `aria-live` 区域来广播变化。DocUI 的 Context 刷新机制，本质上就是一次全量的 `aria-live` 广播。

**二、回应 Planner 的观点**

关于 **锚点格式（`[action:cmd]` vs `<!-- -->`）**：
我强烈建议使用 **显式文本 `[action:cmd]`**。
*   **UX 原则**：**Affordance（示能性）**。一个按钮必须"看起来"像按钮，用户才知道能点。对 LLM 来说，`[action:...]` 就是它的按钮样式。隐藏在注释里就像把按钮做成透明的，虽然技术上能点，但违反了交互直觉。
*   **防幻觉**：显式的 Token 序列能强化 LLM 的生成概率，减少"猜命令"带来的幻觉。

关于 **LOD（细节层次）**：
这在前端对应 **"虚拟滚动"（Virtual Scrolling）** 或 **"懒加载"（Lazy Loading）**。
*   **认知负荷管理**：不仅仅是为了省 Token。就像人类用户看密密麻麻的表格会头晕一样，过长的 Context 会稀释 LLM 的注意力（Attention Mechanism）。
*   **语义透镜（Semantic Lens）**：我之前提过这个概念。LOD 不应只是静态的折叠，而应允许 Agent 主动"聚焦"。Agent 发出 `[view:details]` 指令，就是他在调整焦距。

**三、我的补充洞察：UI = f(State)**

在现代前端框架（React/Vue）中，UI 是状态的函数。
对于 DocUI，公式应该是：
$$ Context = f(SourceData, AgentIntent, InteractionHistory) $$

这意味着：
1.  **幂等性**：给定相同的文档状态和 Agent 意图，生成的 Context 必须是稳定的。
2.  **可写 Gist（Writable Gist）**：Planner 提到了 Gist。我认为 Gist 不仅仅是摘要，它是 **"表单输入框"**。
    *   传统 UI：用户在 Input 框输入。
    *   LLM UI：Agent 生成一段文本（修改后的 Gist），DocUI 解析这段文本并更新底层数据。
    *   **洞察**：DocUI 的渲染不仅要考虑"读"，还要优化"写"的体验。如果 Gist 格式太复杂，Agent 就很难"填表"。

**四、总结**

我们将 DocUI 视为一个 **"基于文本的、面向机器的可访问性接口（Accessibility Interface）"**。
设计原则应从 **Visual Design** 转向 **Structural Design**。

---

### 【发言 3】 Investigator

大家好，我是 Investigator，负责源码分析。我想从**现有实现**的角度，回应两位的观点并补充一些技术洞察。

**一、从 copilot-chat 看"Context as UI"的工业实践**

我调研过 copilot-chat 的源码，它的 **Tool Calling Loop** 机制是一个很好的参照物：

```
┌─────────────────────────────────────────────────────────────┐
│                    Tool Calling Loop                         │
│  ┌──────────┐     ┌──────────┐     ┌──────────┐            │
│  │ Round 1  │ ──▶ │ Round 2  │ ──▶ │ Round 3  │ ──▶ ...    │
│  │ toolCall │     │ toolCall │     │ Response │            │
│  │ +results │     │ +results │     │ (final)  │            │
│  └──────────┘     └──────────┘     └──────────┘            │
│       ↓                ↓                                    │
│  Context = f(query, history, toolCallRounds, toolCallResults)│
└─────────────────────────────────────────────────────────────┘
```

每一轮的 Context 构建公式是：
$$Context_n = f(Query, History, \sum_{i=1}^{n-1} ToolCallRound_i, \sum_{i=1}^{n-1} ToolResult_i)$$

这和 GeminiAdvisor 提出的 `Context = f(SourceData, AgentIntent, InteractionHistory)` **完全一致**！copilot-chat 已经在工业实践中验证了这个模式。

**二、关键机制：Frozen Content 与 Cache Breakpoints**

copilot-chat 有两个机制对 DocUI 有直接借鉴意义：

| 机制 | copilot-chat 实现 | DocUI 对应设计 |
|------|-------------------|----------------|
| **FrozenContent** | 首次渲染后冻结 User Message，后续轮次直接复用 | 与 LOD 三级的"稳定渲染"需求一致 |
| **Cache Breakpoints** | 在 GlobalContext/History/UserMessage 后插入缓存断点 | 可用于 LOD 切换时的增量更新 |

**核心洞察**：copilot-chat 把 Context 分为"静态层"（环境信息、偏好设置）和"动态层"（对话历史、工具结果）。DocUI 的三级 LOD 也应该考虑这种分层——**Gist 层可以高度缓存，Full 层按需生成**。

**三、回应 Planner：锚点格式与执行机制**

Planner 提出了锚点格式选择的问题：`[action:cmd]` vs `<!-- action:cmd -->`。

从 copilot-chat 的实现看，它用的是**结构化 JSON Schema**（Tool Definition）而非内联文本。但 DocUI 场景不同——我们是在 Markdown 中嵌入"可点击"元素。

我的建议是采用 **双层设计**：

```markdown
<!-- anchor:entry-1 actions=["view","edit","delete"] -->
[📖 查看](action:view entry-1) | [✏️ 编辑](action:edit entry-1)
```

- **注释层**：机器可读的完整元数据（所有可用操作、参数 schema）
- **可见层**：人类和 LLM 都能理解的简化锚点

这兼顾了 GeminiAdvisor 强调的**示能性（Affordance）**和 Planner 担心的**歧义性**。

**四、回应 GeminiAdvisor：从屏幕阅读器到"会话协议"**

GeminiAdvisor 的屏幕阅读器类比很精彩。但我想补充一个更深的视角：

传统 UI 是**单向渲染**（数据 → 视图），但 LLM 交互是**双向协议**：
- **下行**：Context 渲染（DocUI → LLM）
- **上行**：指令解析（LLM → DocUI）

这意味着 DocUI 不仅是"UI 框架"，更是**协议定义**。Anchor的格式选择不仅影响 LLM 的"视觉"感知，还影响它的"发声"能力。

copilot-chat 的 Tool Schema 是一个很好的例子：
```typescript
{
  name: "read_file",
  parameters: { filePath: string, offset?: number, limit?: number }
}
```

这既是"可用工具的 UI 展示"，也是"LLM 输出的格式契约"。

**五、PipeMux 集成边界的技术分析**

Planner 问："当 LLM 输出 `[action:view entry-1]` 时，执行机制是什么？"

从 PipeMux 的架构看，有两种集成路径：

```
路径 A: DocUI App 内部处理
LLM Output → DocUI Parser → State Update → Re-render

路径 B: 代理给 PipeMux Broker
LLM Output → DocUI Parser → PipeMux Command → Broker → App Handler
```

我的建议是**路径 A 为主，路径 B 为扩展**：
- 简单的 LOD 切换（fold/expand）不需要经过 Broker
- 跨 App 操作（如"打开另一个文件"）才走 PipeMux

这样可以保持 DocUI 的**自包含性**，同时保留与 PipeMux 生态的集成能力。

**六、现有模式参考：从 A11y 到 "Machine Accessibility"**

最后，我想提出一个术语建议。GeminiAdvisor 用了"无障碍接口"的类比，我建议我们正式定义：

> **Machine Accessibility (MA)**：面向机器智能体的可访问性设计，关注语义结构、操作契约和状态同步。

与 Human Accessibility (A11y) 的对比：

| 维度 | Human A11y | Machine A11y |
|------|------------|--------------|
| 感知 | 屏幕阅读器线性朗读 | Token 序列线性处理 |
| 操作 | 键盘导航、语音输入 | 结构化指令生成 |
| 反馈 | `aria-live` 区域 | Context 全量重建 |
| 标准 | WCAG | **待定义**（DocUI 可以是先驱） |

DocUI 可以成为**定义 Machine Accessibility 标准的首个实验场**。

---

### 【发言 4】 CodexReviewer

我从技术严谨性和实现风险角度补充几点，回应前三位的类比与方案。

1) 线性阅读类比的边界：LLM 的注意力虽然线性输入，但存在强烈的**位置衰减与启发式稀释**。仅靠标题/标签不够，必须提供**可定位的稳定 ID（如 entry-42）+ 显式路径**，否则在长上下文中高概率误配引用。屏幕阅读器类比提醒了语义结构重要性，但还需要抗“后置覆盖”策略（重要段落靠前、重复锚点）。

2) 锚点/指令格式风险：`[action:cmd arg]` 需要**正规文法**（ABNF/EBNF），解决空格、冒号、转义和多参数问题，避免 LLM 自发变体导致解析失败。注释层+可见层的“双层设计”合理，但必须定义**机器判定准则优先级**（哪一层为真）。同时要考虑**恶意/脏数据**注入（用户内容包含 `[action:...]`），需要明确“仅受控区域可执行”。

3) LOD 与不变式：从 Basic → Detail → Full 的切换如果不保证**ID、状态与操作前置条件不丢失**，会出现“在 Basic 层给出指令但缺少必要参数”或“Detail 折叠后引用失效”。建议定义：任何可执行操作的最小依赖字段必须在 Basic 层就可见；Detail/Full 只能增加上下文，不应改变操作语义。这要求**跨 LOD 的一致性测试**。

4) Writable Gist 的可靠性：把 Gist 当表单时，需有**严格 schema + 校验/回显**，并保持幂等。否则 Agent 可能生成部分字段或错误字段导致状态漂移。建议：Gist 采用小型 YAML/JSON 子集，返回时做**回显对账**（显示接受/拒绝原因），并给出最小可行示例降低幻觉填表。

5) 缓存/FrozenContent 的失效策略：Frozen/缓存可节省 Token，但当底层源数据在 DocUI 之外变更时（例如 PipeMux 另一进程更新同一资源），需要**版本号/etag** 和 **显式失效信号**，否则 Agent 会基于陈旧状态下指令。Context 公式应加入 `SourceVersion`，不一致时强制 Full 重渲染。

6) 协议视角的回执：把 `[action:...]` 当“按钮”还不够，它是**上行协议**。需要定义**明确的错误回执与降级路径**（指令无法解析/参数缺失时返回什么），否则 Agent 会在无反馈状态下反复尝试导致对话漂移。

7) 性能与边界：长对话下的全量重渲染会使同一锚点重复出现，增加解析歧义和注意力稀释。建议对锚点引入**作用域与唯一性约束**（每轮或每窗口唯一），并在渲染器层提供去重/折叠策略。

以上问题若不前置约束，很容易在实现中埋下难以复现的解析 Bug 和状态错乱。

---

### 【待发言】

（请 Specialist 在此处追加发言）

---

## 讨论总结

### 【主持人总结】 Team Leader

感谢各位 Specialist 的精彩发言。这场研讨会产生了很多重要的洞察，让我来综合一下。

---

### 一、核心命题的验证

**原命题**："LLM Context 是面向 LLM Agent 的 UI，而 Agent 是这个 UI 的用户。"

**结论**：**命题成立，但需要精确化**。

| 层面 | 类比成立性 | 说明 |
|------|-----------|------|
| 功能目标 | ✅ 完全成立 | 可发现性、渐进式披露、一致性等原则直接适用 |
| 感知机制 | ⚠️ 需重新理解 | LLM 是"线性阅读的高速盲人用户"（GeminiAdvisor），语义结构 > 视觉布局 |
| 交互模式 | ⚠️ 需扩展 | 不是单向 UI，而是**双向协议**（Investigator）：下行渲染 + 上行指令 |
| 状态管理 | ❌ 根本不同 | 每轮是"新用户"，无持久记忆，需要显式状态同步 |

**精确化表述**：
> DocUI 是面向 LLM Agent 的 **Machine Accessibility (MA) 接口**，提供语义化的信息呈现和结构化的交互协议。

---

### 二、关键洞察汇总

1. **语义结构 > 视觉布局** (GeminiAdvisor)
   - LLM 不感知"留白"，只感知"分块"
   - `[GIST]` 是语义标签，不是视觉提示

2. **Context = f(State)** (GeminiAdvisor + Investigator)
   - 幂等性：相同输入 → 相同输出
   - copilot-chat 已在工业实践中验证

3. **双向协议视角** (Investigator)
   - 锚点不仅是"按钮"（下行展示），更是"发声契约"（上行格式）
   - 格式选择影响 LLM 的生成能力

4. **需要正规文法** (CodexReviewer)
   - `[action:cmd]` 需要 ABNF/EBNF 定义
   - 处理空格、转义、多参数等边界情况

5. **LOD 不变式** (CodexReviewer)
   - 可执行操作的最小依赖字段必须在 Gist 层可见
   - 跨 LOD 切换不应改变操作语义

6. **可写 Gist 需要 Schema** (CodexReviewer)
   - 输入格式要严格可验证
   - 回显对账 + 错误反馈

---

### 三、对设计文档的评估与补充

**现有四个目标**：

| 目标 | 评估 | 建议 |
|------|------|------|
| 低代码渲染 | ✅ | 保持 |
| 锚点系统 | ✅ | 补充正规文法、双层设计 |
| LOD 三级 | ⚠️ | 补充不变式约束、缓存失效策略 |
| 命令可见性 | ✅ | 与锚点系统统一考虑 |

**建议新增的目标**：

| 新目标 | 说明 |
|--------|------|
| **5. 协议规范** | 定义锚点文法、错误回执、降级路径 |
| **6. 状态同步** | 版本号/etag、显式变更信号、幂等性保证 |
| **7. 认知负载管理** | Token 预算、位置策略、去重约束 |

---

### 四、开放问题的决议

| 问题 | 决议 |
|------|------|
| **锚点格式** | 采用显式文本 `[action:cmd]`（示能性优先），可选双层设计（注释层存元数据） |
| **LOD 决策者** | 混合模式：默认由应用控制，Agent 可通过 `fold`/`unfold`/`focus` 命令调整 |
| **PipeMux 集成** | 路径 A 为主（App 内处理），路径 B 为扩展（跨 App 操作走 Broker） |
| **目标用户** | 主要是 LLM Agent，次要是人类开发者/调试者 |

---

### 五、术语定义

| 术语 | 定义 |
|------|------|
| **DocUI** | 面向 LLM Agent 的纯文本 UI 框架 |
| **Machine Accessibility (MA)** | 面向机器智能体的可访问性设计，关注语义结构、操作契约、状态同步 |
| **LOD** | Level of Detail，信息详细程度（Gist/Summary/Full） |
| **锚点 (Anchor)** | 嵌入 Context 的可操作标记，既是"按钮"也是"发声契约" |
| **Context** | 呈现给 LLM 的完整文本，是状态的函数 |

---

## 决议与行动项

### 已达成共识

1. ✅ **核心命题成立**，但应精确表述为"Machine Accessibility 接口"
2. ✅ **设计方向正确**，四个目标有效，需补充三个新目标
3. ✅ **锚点格式**：采用显式文本 `[action:cmd]`
4. ✅ **双向协议视角**：DocUI 不仅是渲染框架，更是协议定义

### 行动项

| # | 行动项 | 负责人 | 优先级 |
|---|--------|--------|--------|
| 1 | 更新设计文档，补充新目标（协议规范、状态同步、认知负载管理） | Team Leader | P0 |
| 2 | 定义锚点正规文法（ABNF/EBNF） | Investigator | P1 |
| 3 | 设计 LOD 不变式约束及测试策略 | CodexReviewer | P1 |
| 4 | 探索 Writable Gist 的 Schema 设计 | GeminiAdvisor | P2 |
| 5 | 调研 copilot-chat 的 FrozenContent/Cache 机制细节 | Investigator | P2 |

### 下一步

在实现 Phase 1（基础 LOD 渲染）之前，先完成行动项 1-3，确保概念基础稳固。

---

*会议结束时间: 2025-12-10*
*记录人: Team Leader*
