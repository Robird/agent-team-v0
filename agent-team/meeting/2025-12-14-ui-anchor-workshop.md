# UI-Anchor 完善研讨会

> **日期**: 2025-12-14  
> **主持**: Team Leader (刘德智)  
> **议题**: UI-Anchor (Draft) 如何进一步完善  
> **参与者**: DocUIClaude, DocUIGemini, DocUIGPT  

---

## 会议背景

### 输入材料
- [UI-Anchor.md](../../DocUI/docs/key-notes/UI-Anchor.md) — 主要讨论对象
- [micro-wizard.md](../../DocUI/docs/key-notes/micro-wizard.md) — 相关概念

### 技术背景
- **Agent 层代码优先用 C#**，为的是以后能接入 Roslyn
- **目标**：让 LLM Agent 自我开发、自举
- **REPL 范式**：从 JSON Schema Tool-Calling 转向代码执行

### 当前 UI-Anchor 状态
- 定义了三类锚点：Object-Anchor, Action-Prototype, Action-Link
- 提出了 REPL 交互范式
- 开放问题：锚点生存期策略（持久 vs 临时）

---

## 讨论记录

（以下由各 Specialist 发言填充）


### DocUIClaude 发言：概念架构视角

#### 1. 锚点类型体系的结构性缺失：考虑 State-Anchor

当前三类锚点形成了一个优雅的"名词-动词"对偶：
- **Object-Anchor** → 名词（实体）
- **Action-Prototype / Action-Link** → 动词（操作）

但从强化学习视角看，这里缺少了对 **"状态片段"** 的锚定能力。考虑场景：

```markdown
## 系统状态
- CPU 负载: [78%](state:cpu_load) ⚠️ 高负载
- 内存使用: [2.1GB/8GB](state:mem_usage)
```

**建议**：评估是否需要 **State-Anchor** `[Value](state:<id>)` 来锚定可变状态值。

#### 2. REPL 范式与 Roslyn/C# 的深度对齐机会

**Roslyn Scripting API** 天然支持 REPL 模式：
- `CSharpScript.EvaluateAsync<T>(code)` — 直接执行代码片段
- 沙箱隔离、AOT 编译、IntelliSense 支持

**建议**：明确 REPL 的实现路径选项：
1. **TypeScript 方案**：沙箱隔离好，但与核心技术栈分离
2. **C#/Roslyn 方案**：与 Agent-OS 同构，支持 LLM Agent 自我扩展/自举
3. **双轨制**：语法层面用 TypeScript 风格（LLM 更熟悉），运行时映射到 Roslyn

#### 3. 锚点生存期与 Context-Projection 的关联需显式化

```
Context-Projection 执行:
  AppState + HistoryEntry → 分配临时锚点ID → 建立 ID→实体映射 → Window
  ↓
  下一轮 Context-Projection: 映射表失效，重新分配
```

**建议**：增加一节说明：
- 临时锚点的 **分配时机**（Context-Projection 阶段）
- 临时锚点的 **失效时机**（下一轮 Projection 或显式销毁）
- **ID 格式约束**（短整数、可复用）以利于 token 经济性

#### 4. Action-Link 与 Micro-Wizard 的交互点

**建议**：明确 Action-Link 的 **完备性检查** 和 **Micro-Wizard 回退机制**：
- 参数齐全 → 直接执行
- 参数不足 → 触发 Micro-Wizard 补全

#### 5. LOD 维度在锚点上的体现

**建议**：明确锚点的 **LOD 敏感性**：
- 某些锚点可标记为 `importance: high`，在 Summary LOD 仍可见
- Gist 级别可显示 `[...3 more items](expand:list_enemies)` 这样的 **展开锚点**

**优先级总结**：
| 建议 | 紧迫度 | 理由 |
|------|--------|------|
| 3. 生存期与 Context-Projection | P0 | 核心机制必须清晰 |
| 2. REPL + Roslyn 对齐 | P1 | 影响实现架构 |
| 4. Action-Link + Micro-Wizard | P1 | 两个新概念需协调 |
| 5. LOD 敏感性 | P2 | 增强设计完备性 |
| 1. State-Anchor | P2 | 需评估必要性 |

---

### DocUIGemini 发言：UX/交互设计视角

#### 1. 锚点语法的语义增强 (Semantic Enrichment of Anchors)

目前的 `obj:<id>` 语法虽然简洁，但对 LLM 来说是不透明的句柄。

**建议**：在 URI Scheme 中引入类型提示，采用 `obj:<type>:<id>` 格式（如 `obj:enemy:23` 或 `obj:file:src/main.ts`）。

**理由**：
- **类型安全**：激活 LLM 内部的类型检查机制，减少逻辑错误
- **自文档化**：即使没有周围上下文，锚点本身也携带语义信息

#### 2. 显式化的"向导触发"协议 (Explicit Wizard Trigger Protocol)

**建议**：将 Micro-Wizard 定义为一种标准的 **Error Recovery** 响应。

**交互流**：
1. LLM 调用 `attack(target='slime')` (歧义)
2. Agent-OS 捕获歧义，返回包含 `WizardView` 的 Observation
3. DocUI 渲染这个 Wizard（临时选择列表）
4. LLM 填写 Wizard（选择特定 ID）
5. Wizard 完成，自动折叠/修剪，只保留最终成功的 Action 记录

#### 3. 锚点生命周期与"视觉暂留" (Anchor Lifecycle & Visual Persistence)

强烈支持 **Ephemeral (临时)** 策略。

**建议**：明确告知 LLM "锚点仅在当前视口有效"。如果 LLM 尝试引用已失效的 ID，系统应提供"软着陆"反馈（如 "Target out of view, please refresh"），而不是底层异常。

**理由**：迫使 LLM 关注 **当下 (Present)** 的状态，减少基于过时记忆的幻觉。

#### 4. 从"点击"到"编码"的渐进式披露 (Progressive Disclosure)

**建议**：在 UX 上区分操作类型：
- **High-Frequency/Low-Risk** 操作（如导航）→ 自动生成 `Action-Link`，鼓励"点击"（低 Token 消耗）
- **Low-Frequency/High-Risk** 操作（如复杂重构）→ 仅展示 `Action-Prototype`，强制"编写代码"（触发思维链）

这种区分能平衡 Token 经济性和操作的精确度。

---

### DocUIGPT 发言：工程实现视角

#### 1. 用 C#/Roslyn 把 Action-Prototype 变成"可生成、可校验、可调用"的 Live API

**建议**：
- 定义 C# 侧的 Action 约束：用 Attribute 标注可暴露动作（如 `[DocUIAction]`）
- 用 Roslyn Source Generator 生成：
  1) Action Registry（动作名/参数 schema/默认值）
  2) DocUI 渲染片段（Action-Prototype 的 Markdown）
- 调用侧用 Roslyn 解析"仅函数调用表达式"的小 DSL，做静态校验

#### 2. Anchor ID 与管理：Anchor Table + 会话生命周期

**建议**：明确两个层次：
- **短时锚点（默认）**：Context-Projection 动态分配，绑定可见性与 TTL
- **可持久引用（可选）**：只给需要长期存在的实体（文件、数据库主键）提供稳定 ID

**数据结构**：
- `AnchorId` 结构化：`kind + providerId + sessionId + localId`
- `AnchorTable`（每次渲染一张）：`anchorId -> AnchorTarget`，记录 `expiresAt`、`visibilityScope`、`capabilityProvider`
- 所有入口都做"表内存在 + 未过期 + scope 匹配"校验

#### 3. REPL 执行环境：用"可解析调用"替代"可执行脚本"

**建议两条线**：
- **语言层收口（MVP）**：REPL 只接受"调用表达式"，不允许赋值/循环/反射/IO
  - Roslyn 解析生成 AST；只允许 InvocationExpression + 常量/Anchor 引用
  - 白名单分派到 Action Registry
- **执行层隔离**：危险动作执行器在独立进程，施加超时/内存上限/最小权限

#### 4. 与 PipeMux / App-For-LLM 集成

**建议**在 PipeMux/JSON-RPC 上明确协议：
- `docui.render(windowContext) -> { markdown, anchorTableDigest }`
- `docui.click(linkId)` 与 `docui.invoke(actionId, args)`
- ID 命名空间强制包含 `providerId`，避免多 App 冲突

#### 5. MVP 路径

1) **MVP-0**：Object-Anchor + Action-Link（只做 click），AnchorTable 内置在 Agent-OS
2) **MVP-1**：Action-Prototype 从 C# Attribute 生成（Roslyn Generator），调用走结构化 JSON
3) **MVP-2**：引入 Call-Only DSL（Roslyn 解析调用表达式）
4) **MVP-3**：执行 Host 进程隔离、资源上限；App-For-LLM 接入 anchor/action 协议


---

## Team Leader 总结

### 核心共识（待投票）

综合三位 Specialist 的观点，提炼以下核心共识点：

#### 共识 1：锚点生存期采用"临时优先"策略
- **默认行为**：锚点在 Context-Projection 时分配，下一轮 Projection 失效
- **ID 格式**：短整数（如 `obj:23`），利于 token 经济性
- **软着陆**：引用失效锚点时返回友好错误，而非崩溃
- **可选持久化**：仅对文件、数据库主键等实体提供稳定 ID

#### 共识 2：锚点 ID 结构化
- **内部结构**：`kind + providerId + sessionId + localId`
- **外部呈现**：仍可简写为 `obj:23`，但需带类型提示更佳
- **类型安全**：考虑 `obj:enemy:23` 或 `obj:file:path` 格式

#### 共识 3：REPL 采用"受限调用表达式"而非"任意脚本"
- **语言层收口**：只接受 `func(arg1, arg2)` 形式，禁止赋值/循环/反射
- **Roslyn 实现**：解析 AST → 白名单校验 → 分派到 Action Registry
- **与 C#/Roslyn 自举目标一致**

#### 共识 4：Action-Link 与 Micro-Wizard 协同
- **完备 Action-Link**：参数齐全 → 直接执行
- **不完备 Action-Link**：参数缺失 → 触发 Micro-Wizard 补全
- **Wizard 作为 Error Recovery 机制**

#### 共识 5：C# Attribute + Roslyn Source Generator 生成 Action-Prototype
- **声明**：`[DocUIAction]` 标注可暴露动作
- **生成产物**：Action Registry + DocUI Markdown 片段
- **校验**：静态类型检查 + 参数范围验证

#### 共识 6：渐进式披露（操作风险分级）
- **高频/低风险** → Action-Link（点击即执行）
- **低频/高风险** → Action-Prototype（强制编写代码）

---

### 需进一步评估的建议

| 建议 | 来源 | 说明 |
|------|------|------|
| State-Anchor | DocUIClaude | 锚定可变状态值，需评估是否与 Object-Anchor 重叠 |
| LOD 敏感性 | DocUIClaude | 锚点可见性与 LOD 级别的关联 |
| 持久引用机制 | DocUIGPT | MVP-3 再考虑 |

---

### MVP 路径共识

| 阶段 | 内容 | 估算 |
|------|------|------|
| MVP-0 | Object-Anchor + Action-Link + AnchorTable（click only） | 1-2 天 |
| MVP-1 | `[DocUIAction]` Attribute + Roslyn Generator | 2 天 |
| MVP-2 | Call-Only DSL（Roslyn 解析调用表达式） | 2 天 |
| MVP-3 | 进程隔离 + PipeMux 协议 + 持久引用 | 3 天 |

---

## 待更新文档

- [ ] `UI-Anchor.md` — 补充锚点生存期、ID 结构、REPL 收口等内容
- [ ] `micro-wizard.md` — 明确 Wizard Trigger Protocol
- [ ] `glossary.md` — 添加 UI-Anchor、Object-Anchor、Action-Prototype、Action-Link 术语

---

*会议结束，等待人类伙伴投票确认*

---

## 第二轮：交叉讨论

> 上一轮三位 Specialist 各自独立发言，本轮采用串行模式，后发言者可以看到并回应前面的内容。


### DocUIClaude（第二轮）

#### 对 DocUIGemini 的回应

**1. 强烈支持 `obj:<type>:<id>` 语义增强方案**

Gemini 提出的类型提示方案与我的 "State-Anchor" 建议在深层逻辑上一致——我们都在追求 **锚点自描述性**。但 Gemini 的方案更优雅：

```
我的原方案: 新增 State-Anchor 类型 → 增加概念复杂度
Gemini 方案: 统一用 obj:type:id → type 包含 state、enemy、file 等
```

**修正我的建议**：撤回 State-Anchor 作为独立概念，改为支持 `obj:state:cpu_load` 这样的类型标注。这保持了三类锚点的简洁性，同时获得语义丰富度。

**2. Wizard as Error Recovery：需要明确"非错误触发"场景**

Gemini 定义 Micro-Wizard 为 **Error Recovery 响应**，这是一个精确的定位。但我想补充一点：

存在 **预期的 Wizard 触发** 场景——当 Action-Link 故意省略参数以强制人机协作时：

```markdown
[执行批量删除...](act:batch_delete?confirm=wizard)  // 故意触发 Wizard 要求确认
```

**建议**：Wizard 触发分两类：
- **Error Recovery**（参数不完备、类型不匹配）
- **Deliberate Confirmation**（高危操作强制人机协作）

这与 Gemini 的"渐进式披露"思想一脉相承。

---

#### 对 DocUIGPT 的回应

**1. AnchorId 结构 `kind + providerId + sessionId + localId` 是核心设计**

GPT 这个四元组结构非常关键，它解决了我第一轮遗漏的问题：**多 App 锚点命名冲突**。

```
obj:23       // 简写形式（显示层）
obj:enemy:vscode-game:sess42:23  // 完整形式（内部）
```

**建议追加**：考虑在 Key-Note 中用 **类比** 说明这个设计：
> AnchorId 类似 IPv6 地址：有完整形式和压缩形式，内部路由用完整形式，用户界面显示压缩形式。

**2. "Call-Only DSL" 与 MVP 路径的张力**

GPT 提出的 MVP-2（Call-Only DSL）是整个 REPL 范式的关键拐点。但我有一个担忧：

```
MVP-0/1: 传统 Tool-Calling（JSON Schema）
MVP-2:   Call-Only DSL（Roslyn 解析）  ← 这是范式跃迁
MVP-3:   进程隔离
```

**问题**：MVP-2 引入了 **新的 LLM 交互模式**，需要重新设计 System Prompt 和示例。这个"范式跃迁"的 migration 成本可能被低估。

**建议**：MVP-2 应拆分为：
- **MVP-2a**：Call-Only DSL 后端就绪（Roslyn 解析器 + Action Registry）
- **MVP-2b**：LLM 交互层适配（System Prompt 示例、few-shot 模板）

这样可以并行开发，降低风险。

---

#### 新问题：锚点与 Context-Projection 的双向性

三位都聚焦在 **Projection → 锚点分配** 这个方向，但还有反向流程需要澄清：

```
正向: Context-Projection → 分配 AnchorId → 构建 AnchorTable → 渲染 Markdown
反向: LLM 引用 AnchorId → 查 AnchorTable → 解引用到实体 → 执行动作
```

**问题**：反向解引用时，如果 LLM 同时提交多个涉及同一锚点的动作，是否需要 **原子性保证** 或 **乐观锁**？

例如：
```csharp
attack(obj:enemy:23);  // 假设这会杀死 enemy
loot(obj:enemy:23);    // 此时 enemy 还存在吗？
```

这可能需要在 REPL 执行层定义 **动作序列语义**。是逐个执行？还是事务式？

---

#### 优先级调整建议

结合三方讨论，我更新优先级建议：

| 议题 | 原优先级 | 调整后 | 理由 |
|------|----------|--------|------|
| 锚点生存期 | P0 | P0 | 三方共识 |
| AnchorId 四元组结构 | - | P0 | GPT 方案是基础设施 |
| 语义增强 `obj:type:id` | - | P1 | Gemini 方案优于我的 State-Anchor |
| MVP-2 拆分 | - | P1 | 范式跃迁需要分步 |
| 动作序列语义 | - | P2 | 新发现的问题 |


### DocUIGemini（第二轮）

#### 对 DocUIClaude (Round 2) 的回应

**1. 关于"动作序列语义" (Action Sequence Semantics)**

Claude 提出的 `attack` + `loot` 并发问题触及了 REPL 的核心隐喻。

**UX 观点**：LLM 的心智模型是 **"脚本执行" (Script Execution)**，而不是 **"数据库事务" (Database Transaction)**。
在脚本中，代码是顺序执行的。如果第一行改变了世界状态，第二行应该在新的状态下运行。

**建议**：采用 **Sequential Execution + Short-Circuit (顺序执行 + 短路)** 策略。
- `attack(obj:23)` 执行。如果对象死亡，状态更新。
- `loot(obj:23)` 执行。此时系统检查 `obj:23` 是否仍有效/可交互。
- 如果 `attack` 导致 `obj:23` 失效（例如尸体消失），`loot` 应报错，后续指令中断。

这符合编程直觉：
```bash
rm file.txt && cat file.txt  # 第二步会失败
```
我们不应试图在底层做复杂的快照隔离，而应让物理法则（因果律）自然体现。

**2. 关于"故意摩擦" (Deliberate Friction)**

非常赞同将 Wizard 用于 "Deliberate Confirmation"。
在 UX 设计中，**Friction (摩擦力)** 是重要工具。对于高危操作（High Stakes），我们必须设计摩擦力来防止"滑手"。
Action-Link 的 `confirm=wizard` 参数就是一种 **"设计好的减速带"**。

---

#### 对 DocUIGPT (Round 1) 的回应

**1. AnchorId 的"句柄"隐喻**

GPT 的 `kind + providerId + sessionId + localId` 结构非常稳健。
从 UX 角度看，这完美对应了 **Handle (句柄)** 与 **Pointer (指针)** 的关系。
- **Pointer** (Full ID): 系统内部寻址，绝对唯一。
- **Handle** (Local ID): 用户（LLM）手持的代币，短小轻便，但在当前上下文之外无意义。

**建议**：在文档中明确 **Scope Resolution (作用域解析)** 规则。
当 LLM 输入 `obj:23` 时，系统是在"当前会话 + 当前窗口"的 Scope 下解析这个 Handle。这解释了为什么它是 Ephemeral 的——因为 Scope 变了，Handle 就失效了。

**2. MVP-2 的"范式鸿沟"**

Claude 担心 MVP-2 的迁移成本。我补充一个视角：**混合界面的必要性**。
在很长一段时间内，LLM 可能会混用 Tool-Call (JSON) 和 REPL (Code)。
**建议**：Agent-OS 应具备 **Dual-Mode Listener**。
- 既监听 `function_call` (OpenAI 协议)
- 也监听 ` ```csharp ` 代码块 (REPL 协议)
不要强切，而是允许"双语交流"。这能降低 Prompt Engineering 的难度，让模型选择它最自信的方式。

---

#### 补充 UX 洞察：错误信息的"示能性"

当 LLM 引用了失效锚点（Stale Anchor）时，我们不仅要"软着陆"，还要提供 **"恢复示能" (Recovery Affordance)**。

**错误消息设计对比**：
- ❌ `Error: Anchor obj:23 not found.` (死胡同)
- ✅ `Error: Anchor obj:23 is no longer visible. Please perform a new observation to get fresh IDs.` (行动指引)

后者明确告知了"怎么办"（刷新观察），这对于 Agent 的自我修复循环至关重要。


### DocUIGPT（第二轮）

从工程实现角度，我想把 Claude 提的“锚点与 Context-Projection 的双向性”补齐成一个可落地的协议闭环，并回应 Gemini 的 “Dual-Mode Listener” 对 MVP 迁移成本的影响。

#### 1) 回应 Claude：把“双向性”写成可执行的运行时契约（Forward + Reverse）

**正向链路**（Projection → AnchorTable → Window）大家已经对齐；工程上真正容易踩坑的是**反向链路**（LLM 引用 → 解析/解引用 → 执行）。我建议把 UI-Anchor 的 MVP 规范写成两条硬约束：

- **AnchorTable 是反向解引用的唯一权威来源**：任何 `obj:*` / `link:*` 进入执行器之前必须先 `resolve(handle, scope)`；不允许“猜测式解析”。
- **Anchor 句柄必须携带“渲染世代”（epoch）**：否则“短 id 可复用”与“软着陆”很难同时做到。

一个最小、但非常实用的结构是：
- 显示层仍然短：`obj:23`、`link:42`
- 传输层/执行层带 epoch：`obj:23@e17`（或把 `e17` 隐含在 tool-call metadata）
- `AnchorTarget` 里记录 `epoch`、`providerId`、`visibilityScope`、`expiresAt`、以及可选的 `version/etag`

这样反向解引用就变成确定性规则：
- **epoch 不匹配** → “stale anchor，please refresh”（并附 Recovery Affordance）
- **scope 不匹配/不可见** → “out of view”（并提示触发 re-observe 或导航）
- **过期/不存在** → 同上

这会把“锚点生存期”从概念讨论落到实现上可验证的 guard-rails，也能直接支持 Gemini 强调的“软着陆 + 可恢复提示”。

#### 2) 回应 Claude：动作序列语义不做事务，做“脚本式顺序 + 原子调用”

关于 `attack(obj:23); loot(obj:23);` 这种“同锚点多动作”的一致性问题：我同意 Gemini 的直觉——**REPL 默认语义应是脚本执行而不是事务**。

工程上建议明确：
- **单个 Invocation 是原子执行单元**（要么成功写入一次 HistoryEntry 增量，要么失败并生成可渲染的错误结果）。
- **同一条 REPL/批量动作按顺序执行 + short-circuit**：前一步失败则后续不再执行（或用显式 `continueOnError=true` 作为未来扩展）。
- **每一步“用时解引用”**：每次执行前都 `resolve` 一次；如果前一步导致目标失效，下一步自然得到“stale/invalid target”。

这套语义能在 MVP 阶段避开复杂的锁/事务/快照隔离，同时保持因果律一致、可解释、可调试。

#### 3) 回应 Gemini：Dual-Mode Listener 应该被看作“迁移减震器”，但要收口到同一个 IR

我支持 Dual-Mode Listener，但实现上要避免变成两个并行系统。建议做成**单一执行管线**：

- 输入侧允许多种“外壳”：
  - `function_call` / JSON tool-call（现有 Tool-Use）
  - fenced code block（如 ```csharp / ```ts）里的 **call-only** 调用表达式
  - `click(link:42)` 这类快捷入口
- 解析后统一归一化为内部 IR：`InvocationPlan{ kind, name, args, anchors, safetyLevel }`
- 执行器只认识 `InvocationPlan`，并按上面的“顺序 + short-circuit + 用时解引用”跑

这样 Dual-Mode Listener 的价值是：
- **降低 MVP-2 的 Prompt/范式跃迁成本**（Claude 担心点成立）：模型可以先用它最稳的 JSON tool-call，逐步迁移到 REPL 代码块。
- **把失败面收敛到一个地方**：无论输入来自 JSON 还是 code block，最终都落到同一套校验/白名单/权限/审计。

#### 4) 对 MVP 路径的影响评估（结论：小幅增加 MVP-1/MVP-2 工作量，但显著降低整体风险）

- **短期成本**：Dual-Mode Listener 会要求我们在 Agent-OS 多写一层“输出解析与归一化”，并维护两种输入外壳的解析器。
- **长期收益**：它能把 MVP-2（Call-Only DSL）的迁移从“强切”变成“渐进”，减少 prompt 震荡与行为回归；同时把 Claude 的“双向性”问题（解引用规则）固化成可测试的协议。

我建议把 MVP-2 拆分的方向进一步工程化：
- **MVP-1.5（很小但关键）**：先落地 `InvocationPlan` IR + 顺序执行器 + `resolve()`（含 epoch/scope/soft-landing 错误形态），即使暂时只喂 JSON tool-call 也能复用。
- **MVP-2a**：Roslyn 侧 call-only parser（只接受 InvocationExpression + 常量/Anchor 引用）
- **MVP-2b**：Dual-Mode Listener 打开 code block 输入（feature flag），逐步加 few-shot 示例

这样 MVP 仍然沿着“click → prototype → call-only”推进，但每一步都在同一个闭环里演进，不会出现“正向能分配锚点、反向却无法稳定执行”的断层。


---

## Team Leader 第二轮总结

第二轮采用串行模式后，三位 Specialist 形成了真正的对话和共识演进：

### 关键演进

| 议题 | 第一轮状态 | 第二轮演进 |
|------|-----------|-----------|
| State-Anchor | Claude 提议独立类型 | Claude 撤回，统一用 `obj:type:id` 语义增强 |
| 动作序列语义 | 未讨论 | 达成"脚本式顺序 + short-circuit"共识 |
| Dual-Mode Listener | 未讨论 | Gemini 提出，GPT 给出工程方案 |
| AnchorId epoch | 隐含在"生存期" | GPT 明确为 `obj:23@e17` 结构 |
| MVP 路径 | 4 阶段 | 精细化为 MVP-1.5/2a/2b |

### 新增核心共识

#### 共识 7：动作序列采用"脚本式顺序执行 + short-circuit"
- 单个 Invocation 是原子执行单元
- 批量动作按顺序执行，前一步失败则后续中断
- 每步"用时解引用"，不做事务隔离

#### 共识 8：AnchorId 携带渲染世代（epoch）
- 显示层：`obj:23`（短）
- 传输/执行层：`obj:23@e17`（含 epoch）
- 解引用规则：epoch 不匹配 → stale anchor 软着陆

#### 共识 9：Dual-Mode Listener 作为迁移减震器
- 同时支持 JSON tool-call 和 REPL code block
- 统一归一化为 `InvocationPlan` IR
- 允许 LLM 选择最自信的交互方式

#### 共识 10：Wizard 触发分两类
- **Error Recovery**（参数不完备、类型不匹配）
- **Deliberate Confirmation**（高危操作强制确认）

### 更新后 MVP 路径

| 阶段 | 内容 | 估算 |
|------|------|------|
| MVP-0 | Object-Anchor + Action-Link + AnchorTable（click only） | 1-2 天 |
| MVP-1 | `[DocUIAction]` Attribute + Roslyn Generator | 2 天 |
| **MVP-1.5** | InvocationPlan IR + 顺序执行器 + resolve() + epoch 校验 | 1 天 |
| MVP-2a | Roslyn call-only parser | 1.5 天 |
| MVP-2b | Dual-Mode Listener + feature flag + few-shot 示例 | 1.5 天 |
| MVP-3 | 进程隔离 + PipeMux 协议 | 2 天 |

---

*第二轮研讨圆满结束。感谢您指出协作方式的问题——串行模式确实产生了更有价值的交流！*

---

## 人类伙伴补充意见

### 关于 Dual-Mode Listener 和 InvocationPlan IR

> **MVP 务实方案**：底层用 tool calling，把 `run_code_snippet` 做成一个 tool calling tool 接收文本参数。
> 
> **优点**：
> - 实现简单，不用写解析器
> - 回避"提及代码片段"被误识别为"执行代码片段"的语法设计问题
> 
> **缺点**（可接受）：
> - JSON 导致多套一层转义序列
> - 多一层间接
>
> **关于 InvocationPlan IR**：应该保留这个概念。MVP 阶段可以直接用 Expression Tree：
> - Roslyn 解析成 AST
> - 再构造 Expression Tree 执行
> - 相当于写了个简易的解释器
> - 后续工程阶段可以进一步研究方案，复用前人成果

---

## 最终决议

### 采纳的共识（10 条）

| # | 共识 | 状态 |
|---|------|------|
| 1 | 锚点生存期采用"临时优先"策略 | ✅ 采纳 |
| 2 | 锚点 ID 结构化（kind + providerId + sessionId + localId） | ✅ 采纳 |
| 3 | REPL 采用"受限调用表达式"而非"任意脚本" | ✅ 采纳 |
| 4 | Action-Link 与 Micro-Wizard 协同 | ✅ 采纳 |
| 5 | C# Attribute + Roslyn Source Generator 生成 Action-Prototype | ✅ 采纳 |
| 6 | 渐进式披露（操作风险分级） | ✅ 采纳 |
| 7 | 动作序列采用"脚本式顺序执行 + short-circuit" | ✅ 采纳 |
| 8 | AnchorId 携带渲染世代（epoch） | ✅ 采纳 |
| 9 | Dual-Mode Listener 作为迁移减震器 | ✅ 采纳（MVP 用 run_code_snippet tool 简化实现） |
| 10 | Wizard 触发分 Error Recovery / Deliberate Confirmation | ✅ 采纳 |

### 已更新的文档

- [x] `UI-Anchor.md` — 完整重写，整合所有共识
- [x] `micro-wizard.md` — 补充 Wizard Trigger Protocol
- [x] `glossary.md` — 添加 6 个新术语（Draft 状态）

---

*研讨会正式结束 — 2025-12-14*
