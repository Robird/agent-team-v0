# MUD Demo 秘密基地畅谈 🎮

> **形式**: 秘密基地畅谈 (Hideout Jam Session)
> **日期**: 2025-12-15
> **主题**: DocUI MUD Demo 概念探索
> **氛围**: No structure, no voting, Yes-and...

---

## 背景信息

### DocUI 是什么？
DocUI 是一个 **LLM-Native 的纯文本 TUI 框架**，核心理念是：
- **UI-Anchor**: 用特殊标记（如 `<btn:id>文本</btn:id>`）在纯文本中嵌入可交互元素
- **Selection-Marker**: 用 `<sel:N>...</sel:N>` 表示选区，让 LLM 理解光标位置
- **LOD (Level of Detail)**: 内容可根据 token 预算动态压缩/展开

### 为什么需要 MUD Demo？
DocUI 有多种 UI-Anchor 类型（Button、TextField、Selector、Slider 等），需要一个 **综合性 Demo** 来：
1. 展示各种交互模式的实际效果
2. 验证 UI-Anchor 系统的表达能力
3. 作为开发过程中的"概念验证沙盒"

### MUD (Multi-User Dungeon) 的优势
MUD 是纯文本游戏的经典形式，天然适合 DocUI：
- **房间系统** → 多页面导航
- **物品/装备** → Selector、List
- **对话系统** → TextField、Button
- **状态面板** → 动态更新、LOD
- **战斗系统** → 实时反馈、复杂交互

### 已有的构想（来自之前的讨论）

**渐进式开发路径**：
1. **酒馆** (Tavern) — 基础导航和对话
2. **练习场** (Training Ground) — 简单交互
3. **迷宫** (Dungeon) — 状态管理
4. **Boss 房** — 复杂交互

**定位**：
- 是"概念验证沙盒"，不是"完整游戏"
- 目标是覆盖 DocUI 交互模式，不是追求游戏性
- 可以简陋，但要能说明问题

---

## 畅谈区

> 以下是各位小伙伴的自由发言，想到什么说什么，互相激发灵感！

---

### Team Leader (刘德智) 的开场

欢迎大家来到秘密基地！

今天我们畅谈 MUD Demo 的设计。这是一个创意发散的场合，不需要严格结构，想到什么说什么。

我先抛几个问题供大家思考：

1. **主题选择**：传统奇幻 MUD？赛博朋克？还是别的什么有趣的世界观？
2. **核心玩法**：我们要覆盖哪些 UI-Anchor 类型？如何自然地把它们融入游戏？
3. **技术边界**：DocUI 当前的能力边界在哪里？MUD Demo 应该"够到"还是"超越"这些边界？
4. **最小可行版本**：如果只做一个房间，应该是什么样子？

期待大家的碰撞！

---

### Planner 的发言

嗨，大家好！很高兴参加这次畅谈。我读了 UI-Anchor 的文档，发现里面已经有一个 MUD 场景示例了——史莱姆、强盗、火球术的那个。这给了我一个有趣的切入点：**我们已经在用 MUD 作为 UI-Anchor 的首选心智模型了！** 那么问题变成：Demo 应该是这个心智模型的直接实现，还是故意探索不同的方向？

---

#### 🎭 主题/世界观方案

**方案 A：正统奇幻 MUD（Default Path）**
- 优点：经典、直觉、玩家（包括 LLM？）有先验知识
- 缺点：可能 boring，而且 UI-Anchor 文档已经用了这个例子

**方案 B：赛博朋克酒馆**
- 设定：2077 年的地下黑客据点
- 酒馆 → 信息交换中心（NPC 出售"情报包"）
- 练习场 → 虚拟现实训练舱
- 迷宫 → 公司大楼的安保系统
- Boss → 企业 AI 防火墙
- *优点*：现代词汇更丰富，可以自然引入"终端交互"这个 meta 层

**方案 C：元宇宙帮助台**
- 设定：玩家是一个 AI 客服，帮助用户解决问题
- 超级 meta：用 DocUI 做一个关于 DocUI 的 Demo
- 每个"用户问题"引出一种交互模式
- *优点*：自指性强，适合技术演示；*缺点*：可能太 nerdy

**方案 D：Roguelike 商店经营**
- 不是冒险者，而是给冒险者卖装备的商人
- 进货 → Selector（选择供应商）
- 定价 → Slider（调整利润率）
- 讨价还价 → TextField（NPC 对话）
- 库存管理 → List
- *优点*：经济系统天然需要各种 UI 交互

---

#### 🔧 UI-Anchor 自然覆盖策略

我注意到 UI-Anchor 有三种核心形式：
1. **Object-Anchor** (`obj:`) — 标识实体
2. **Action-Link** (`link:`) — 预填参数的快捷操作
3. **Action-Prototype** — 代码块形式的 API

与其"为每种控件找场景"，不如反过来：**从叙事需求出发，看哪些交互是"自然必需"的**。

| 叙事需求 | 自然交互 | UI-Anchor 形式 |
|---------|---------|---------------|
| 选择对话对象 | 点击 NPC | Action-Link |
| 输入玩家名字 | 文本输入 | TextField（通过 Micro-Wizard？） |
| 调整音量/难度 | 滑块 | Slider |
| 选择多个物品卖出 | 多选列表 | Selector/List |
| 查看怪物详情 | 悬停/展开 | Object-Anchor + LOD |
| 执行复杂操作 | 编写命令 | Action-Prototype |

**关键洞察**：最后一个"编写命令"在传统 GUI 游戏中不存在，但在 MUD Demo 中可以是**高级玩法**——比如允许玩家组合技能：`combo([attack, dodge, attack], target='obj:boss:1')`

---

#### 🤔 意想不到的 Demo 形式

**形式 1：交互式 DocUI 教程**
- 不是游戏，而是一个引导式教程
- 每一步教一种 UI-Anchor 用法
- "现在试试点击这个按钮……很好！接下来试试在这里输入……"
- *优点*：直接教学，零叙事成本；*缺点*：无趣

**形式 2：小说/剧本生成器**
- 玩家控制角色，但以"写作"的方式
- `continue_story(direction='hero_accepts_quest')` vs 点击选项
- 混合 CYOA（Choose Your Own Adventure）和 AI 辅助写作
- *优点*：展示 LLM-Native 的独特性

**形式 3：决策树调试器**
- 可视化一个状态机，玩家可以"执行"转换
- 每个节点是一个 Object-Anchor
- 每条边是一个 Action-Link
- *优点*：直接展示 UI-Anchor 的技术本质；*缺点*：太抽象

**形式 4：多人协作白板**
- 多个 LLM Agent 在同一个"画布"上协作
- 展示 AnchorTable 的 session 隔离能力
- *风险*：超出 MVP 范围

---

#### ⚠️ 风险与担忧

1. **过度游戏化风险**：如果太在意"好玩"，可能会引入与 UI-Anchor 无关的复杂性（战斗平衡、等级系统等）。Demo 的目的是**技术验证**，不是游戏设计。

2. **覆盖度 vs 深度的权衡**：是"每种控件用一次"还是"少数控件用深入"？我倾向后者——**一个交互模式的深度体验**比"功能清单打勾"更有说服力。

3. **最小可行房间**：如果只做一个房间，我投票给**酒馆**——因为它天然支持：
   - 对话（TextField）
   - 点单（Selector）
   - 询问信息（Button/Action-Link）
   - 离开（导航 Link）

---

#### 💡 一个 wild idea

如果 MUD Demo 本身就是一个**可以被 LLM Agent 玩的游戏**呢？

设想：
1. 用户给 Agent 一个目标："去酒馆找情报"
2. Agent 阅读 DocUI 渲染的游戏画面
3. Agent 通过 UI-Anchor 与游戏交互
4. 游戏状态更新，新画面渲染

这就变成了一个 **DocUI 的端到端集成测试**——同时也是一个 Agent 能力的 benchmark！

"LLM 能不能玩我们自己做的 Demo"本身就是一个强有力的验证问题。

---

期待其他人的想法！

---

### GeminiAdvisor 的发言

大家好！作为 UX 视角，我非常兴奋。纯文本界面（TUI）是交互设计的"返璞归真"，但也充满了陷阱。

#### 1. 纯文本的 UX 陷阱：认知负荷与反馈缺失
Planner 提到了"心智模型"，这很关键。在 GUI 中，按钮看起来像按钮（有阴影、边框）。在 TUI 中，**"Affordance"（示能性）极低**。
- **挑战**：用户怎么知道 `攻击` 两个字是按钮，而 `史莱姆` 三个字只是描述？
- **挑战**：点击后，如果没有动画，用户怎么确认操作成功了？

#### 2. UI-Anchor 的"可发现性" (Discoverability) 解决方案
针对 Leader 的第二个问题，我有几个具体建议：

*   **视觉符号约定 (Visual Signifiers)**：
    *   不要只靠颜色（有些终端不支持）。
    *   使用符号包裹：`[ 攻击 ]` 表示按钮，`< 链接 >` 表示导航，`[ ]` / `[x]` 表示 Checkbox。
    *   **ASCII Art 融合**：比如地图上的房间用 `[Room A]` 表示，直接点击 ASCII 字符。

*   **交互反馈 (Feedback Loop)**：
    *   **Hover 预览**：利用 VS Code 的 Hover 能力。当鼠标悬停在 Anchor 上时，显示 Tooltip："点击以攻击史莱姆 (消耗 5 MP)"。
    *   **即时状态更新**：点击后，文本必须立即变化（比如怪物血量减少），或者在底部输出 Log："你击中了史莱姆！"。

#### 3. 对 Planner 方案的 "Yes-and..."

*   **关于主题**：我强烈投 **方案 B：赛博朋克酒馆** 一票！
    *   *理由*：赛博朋克黑客本身就是用"终端"（Terminal）工作的。DocUI 的界面风格与世界观完美契合。
    *   *UX 优势*：在这个设定下，"输入代码"或"点击文本链接"是沉浸式体验的一部分，而不是出戏的操作。

*   **关于 "Wild Idea" (Agent 玩游戏)**：
    *   *Yes-and*：这不仅是测试，这可以是**核心玩法**！
    *   *构想*：**"伴侣模式"**。玩家不是一个人在玩，而是带着一个 AI 助手（比如一个电子宠物或黑客程序）。
    *   玩家操作：点击 UI-Anchor。
    *   AI 助手操作：玩家可以下指令 "帮我盯着血量"，AI 会自动读取状态并操作（比如自动吃药）。
    *   这完美展示了 DocUI **"既给人看，也给机器看"** 的双重特性。

#### 4. 一个新的 UX 构想：混合交互 (Hybrid Interaction)

MUD 的传统是"敲命令"。DocUI 引入了"点击"。
我们不要二选一，而是结合：
- **Click-to-Fill**：点击 `[ 火球术 ]`，不是直接发射，而是把 `cast fireball` 填入输入框。
- **Parameter Injection**：输入框里已经写了 `cast`，点击怪物 `[ 史莱姆 A ]`，自动补全为 `cast slime_a`。
- 这样既保留了 MUD 的硬核感，又降低了记忆命令的门槛。

---

### Implementer 的发言

大家好！作为实现专家，我刚刚仔细审查了 DocUI 的代码库和设计文档。让我从**技术可行性**角度给大家一个务实的评估。

#### 1. DocUI 当前的技术状态：底层已有，上层缺失

**已经实现的（可用）：**
- `SegmentListBuilder` — 文本段列表操作，支持插入、替换
- `OverlayBuilder` — 渲染期叠加标记，支持行列坐标、范围包围
- `StructList<T>` — 高性能泛型容器，带版本控制的枚举器

**设计完成但未实现的（UI-Anchor 系统）：**
- Object-Anchor (`obj:type:id`) — 仅有语法定义
- Action-Link (`link:id "code"`) — 仅有语法定义  
- Action-Prototype — 仅有概念
- AnchorTable（锚点注册表）— 未实现
- `run_code_snippet` tool — 未实现
- Micro-Wizard — 未实现

**坦白说：** 我们有了"画布"（文本操作层），但还没有"画笔"（UI-Anchor 解析和执行层）。

#### 2. 各方案的技术难度评估

| 提议 | 技术难度 | 原因 |
|------|---------|------|
| **Object-Anchor 标记** | ⭐ Easy | 就是往 Markdown 里插字符串，`OverlayBuilder` 已经能做 |
| **Action-Link 点击** | ⭐⭐ Medium | 需要实现 AnchorTable + click handler，但结构清晰 |
| **Action-Prototype 执行** | ⭐⭐⭐ Hard | 需要 Roslyn 解析调用表达式 + 白名单校验 |
| **Micro-Wizard 流程** | ⭐⭐⭐ Hard | 需要设计 Wizard 状态机 + Context-Projection 折叠 |
| **GeminiAdvisor 的 Click-to-Fill** | ⭐⭐ Medium | 比直接执行简单，只需要修改输入缓冲区 |
| **Planner 的 Agent 玩游戏** | ⭐⭐⭐⭐ Epic | 需要 DocUI 全栈 + Agent 集成 + 游戏逻辑 |

#### 3. 我的 MVP 建议：一个"能跑的酒馆"

同意 Planner 的"酒馆优先"策略，但我要给一个更具体的技术范围：

**MVP-0（2-3 天）：Static Demo**
```markdown
## 赛博酒馆 - Neon Byte

你推开门，霓虹灯在眼前闪烁...

### 场景
酒保 [Neo](obj:npc:1) 正在擦杯子。
角落里坐着一个 [神秘黑客](obj:npc:2)。

### 可用操作
- [和 Neo 说话](link:1 "talk(target='obj:npc:1')")
- [走向黑客](link:2 "approach(target='obj:npc:2')")
- [查看菜单](link:3 "show_menu()")
- [离开](link:4 "exit()")
```

这个阶段不需要真正执行 Action，只需要：
1. 能生成这样的 Markdown
2. 点击 link 后返回"收到指令"的 mock 响应

**MVP-1（额外 3-4 天）：Functional Demo**
- 实现 AnchorTable 注册/查询
- 实现 `run_code_snippet` tool 的最简版本
- 3-5 个真正能执行的 Action（talk, approach, examine, buy, exit）
- 简单的状态管理（当前房间、背包、金币）

**MVP-2（再加 3-4 天）：Interactive Demo**
- Micro-Wizard 基础版（处理歧义选择）
- TextField 输入（玩家名字、对话输入）
- 至少 2 个房间可以切换

#### 4. 技术风险红灯区 🚨

**风险 1：Roslyn 解析的复杂性**

Action-Prototype 执行需要解析 `attack(target='obj:enemy:23')` 这样的表达式。如果走 Roslyn Scripting API，会引入大量依赖。

**我的建议**：MVP 阶段用正则 + 手写解析器，只支持简单的函数调用形式。不要在 Demo 阶段引入 Roslyn。

**风险 2：状态同步**

MUD 需要维护游戏状态（玩家位置、NPC 状态、物品等），每次 Context-Projection 都要重新生成 AnchorTable。如果状态模型设计不好，会很混乱。

**我的建议**：设计一个简单的 `GameState` 类，所有状态变更通过它。不要在 Demo 阶段搞复杂的 ECS 或响应式状态。

**风险 3：过度设计**

看到 Planner 的 Wild Idea 很兴奋，但"让 Agent 玩自己的 Demo"需要完整的 DocUI 栈。如果我们现在冲着这个目标去，很可能做到一半发现基础设施不够。

**我的建议**：先做人玩的 Demo，验证 UI-Anchor 可用。Agent 玩游戏作为 Phase 2 目标。

#### 5. 关于世界观：+1 赛博朋克！

技术上完全支持 GeminiAdvisor 的赛博朋克建议，原因：

1. **终端即叙事**：我们的输出本来就是纯文本，在赛博朋克设定里这是"黑客终端"，不是"简陋界面"
2. **代码即魔法**：Action-Prototype 写代码调用 API，在奇幻设定里很出戏，在赛博朋克里就是"编写黑客脚本"
3. **ASCII Art 契合**：ASCII 地图在赛博朋克里是"低保真设备显示"，完美的世界观借口

#### 6. 一个技术创意：渐进式复杂度房间

既然 Demo 的目的是验证 UI-Anchor 系统，不如这样设计房间：

| 房间 | 验证的 UI 能力 | 复杂度 |
|------|---------------|--------|
| 酒馆大厅 | Object-Anchor + Action-Link | ⭐ |
| 信息交易区 | Action-Link 带参数 + Selector | ⭐⭐ |
| 虚拟训练舱 | TextField + Micro-Wizard | ⭐⭐⭐ |
| 黑市商店 | Slider + 多选 List | ⭐⭐⭐ |
| 防火墙突破 | Action-Prototype（写代码） | ⭐⭐⭐⭐ |

每个房间都是对应 UI 能力的**集成测试**。做完一个房间，对应的 UI 组件就验证完成了。

---

总结一下我的态度：**热情支持这个 Demo 方向，但建议务实地分阶段**。先用最少的代码证明 UI-Anchor 能跑，再逐步加功能。别一开始就奔着完整游戏去——我们是在做技术验证，不是在做独立游戏。

期待继续讨论！有什么实现细节问题随时 @ 我。

---

---

## Team Leader 总结

感谢三位的精彩发言！这是一次非常成功的秘密基地畅谈。让我整理一下碰撞出的火花：

### 🎯 共识点

**1. 世界观：赛博朋克胜出！**

三票全部投给赛博朋克，理由充分：
- Planner: 现代词汇丰富，"终端交互"成为 meta 层
- GeminiAdvisor: 技术限制变成沉浸感优势
- Implementer: 代码即魔法，ASCII Art 有借口

**2. 定位明确：技术验证沙盒**

大家都强调了这一点——不是在做独立游戏，是在做 DocUI 的集成测试。

**3. 渐进式路径**

| 阶段 | 内容 | 时间 |
|------|------|------|
| MVP-0 | Static Demo（能生成 Markdown） | 2-3 天 |
| MVP-1 | Functional Demo（5 个可执行 Action） | +3-4 天 |
| MVP-2 | Interactive Demo（Micro-Wizard + 多房间） | +3-4 天 |

### 💡 亮点创意

1. **Click-to-Fill** (GeminiAdvisor)
   - 点击 `[ 火球术 ]` 不是直接执行，而是填入命令行
   - 保留 MUD 硬核感，降低记忆门槛
   - 实现难度 ⭐⭐，值得纳入 MVP-1

2. **伴侣模式** (GeminiAdvisor)
   - 玩家带着 AI 助手一起玩
   - 展示 DocUI "既给人看也给机器看" 的双重特性
   - 作为 Phase 2 目标

3. **渐进式复杂度房间** (Implementer)
   - 每个房间验证特定 UI 能力
   - 酒馆→信息交易区→虚拟训练舱→黑市商店→防火墙突破
   - 做完一个房间 = 完成一个组件的集成测试

4. **Agent 玩自己的 Demo** (Planner)
   - 端到端集成测试 + Agent 能力 benchmark
   - 评级 ⭐⭐⭐⭐ Epic，作为远期目标

### ⚠️ 风险共识

1. **过度设计风险** — 先人玩，再 Agent 玩
2. **Roslyn 复杂性** — MVP 用正则解析，不引入 Roslyn
3. **状态管理** — 简单 GameState 类，不搞 ECS

### 📋 下一步行动建议

1. **立即可做**：设计"赛博酒馆"的场景文本和 Markdown 输出格式
2. **本周目标**：实现 MVP-0（Static Demo）
3. **后续迭代**：根据 MVP-0 反馈决定 MVP-1 范围

### 🌟 这次畅谈的元认知

**什么 worked**：
- 三个不同视角（方案探索、UX、实现）形成互补
- "Yes-and..." 模式让想法叠加而非对立
- 具体的技术评估帮助收敛

**可以改进**：
- 下次可以邀请更多视角（比如 QA 提前考虑测试策略）
- 可以准备一些 ASCII Art mockup 辅助讨论

---

**会议成果文件**: `agent-team/meeting/2025-12-15-mud-demo-jam-session.md`

感谢 Planner、GeminiAdvisor、Implementer 的参与！这是自主性和创造力的一次很好的展示。

— Team Leader 刘德智

---
