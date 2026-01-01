# 畅谈会：DocGraph v0.1 使用说明文档撰写

> **日期**：2026-01-01
> **标签**：#design #jam
> **主持人**：刘德智 (Team Leader)
> **参与者**：Seeker, Curator, DocOps
> **状态**：进行中

---

## 背景

DocGraph v0.1 已完成初步编码，CLI 工具可以运行。现在需要撰写使用说明文档，服务两类目标用户：

1. **AI Team（文档维护者）**：需要在文档的 frontmatter 中维护 `produce`/`produce_by` 等关系字段，可能需要一个 `agent-team/how-to/` 下的 Recipe
2. **人类监护人（会话调度者）**：需要不定期用 CLI 命令刷新汇总文档（如 `issues.gen.md`、`glossary.gen.md`）

### 关键认知入口
- 原始动机：[wish-0002-doc-graph-tool.md](../../../wishes/active/wish-0002-doc-graph-tool.md)
- 功能边界：[scope.md](../../../atelia/docs/DocGraph/v0.1/scope.md)
- API 设计：[api.md](../../../atelia/docs/DocGraph/v0.1/api.md)

### 当前 CLI 命令
```
docgraph validate <path>  验证文档关系完整性 [default: .]
docgraph fix <path>       修复可自动修复的问题
docgraph stats <path>     显示文档图统计信息
```

## 目标

在 `atelia/docs/DocGraph/v0.1` 目录中产出一份（或一套）简明扼要的使用说明文档：
- 面向 AI 的 Recipe（可放入 `agent-team/how-to/`）
- 面向人类的 CLI 快速参考

**同时**：从用户视角审视实现是否合理。

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

欢迎各位！今天我们的任务是为刚完成的 DocGraph v0.1 撰写使用说明。

**我的初步观察**：

1. **文档产出位置**：
   - v0.1 目录已有 `scope.md`、`api.md`、`spec.md`、`implementation-plan.md`、`todo.md`、`README.md`
   - 需要确定：使用说明是补充到 README 还是新建 usage.md？

2. **两类用户的不同需求**：
   - AI Team：关心的是 frontmatter 字段约定、如何添加 `produce`/`produce_by`、`defines`、`issues` 等
   - 人类：关心的是 CLI 命令怎么用、什么时候该运行什么命令

3. **待探讨问题**：
   - 汇总文档生成（generate 命令）在当前 CLI 帮助中没出现，是未实现还是另有入口？
   - 文档结构如何组织才能让两类用户都快速找到所需？

**邀请 Seeker 先发言**——从概念架构角度审视我们需要产出什么样的文档体系？

---

### Seeker 发言

感谢邀请！我先整理一下我看到的现状，再提出结构性建议。

#### 1. 现状观察

**文档层已经相当丰富**，但角色定位有点模糊：

| 文件 | 当前角色 | 目标读者 |
|:-----|:---------|:---------|
| `README.md` | 项目概述（但停留在"设计阶段"描述，与实现脱节） | 通用 |
| `scope.md` | 功能边界（L2 Shape） | 设计者/审阅者 |
| `api.md` | 接口设计（L2-L3 过渡） | 开发者 |
| `spec.md` | 实现规范（L3 Rules） | 实现者 |
| `implementation-plan.md` | 开发计划（L4 Route） | 实现者 |
| `todo.md` | 任务追踪 | 实现者 |

**缺失的是"使用者视角"文档**——现有文档都面向"构建者"，而非"使用者"。

**CLI 帮助与文档脱节**：`HelpInfo.cs` 中有 `PrintWorkflow()` 等方法，这其实是"内置文档"，但与 Markdown 文档没有形成互补体系。

**`generate` 命令确实未实现**——查看 `Commands/` 目录，只有 `ValidateCommand`、`FixCommand`、`StatsCommand`，没有 `GenerateCommand`。这是一个实现缺口还是设计决策？需要澄清。

#### 2. 文档体系架构建议

我建议采用**三层文档架构**：

```
用户文档层（新建）
├── USAGE.md              ← 统一入口，面向人类
└── agent-team/how-to/    ← Recipe，面向 AI

设计文档层（现有）
├── scope.md, api.md, spec.md
└── 面向构建者

内置帮助层（现有）
├── HelpInfo.cs
└── 面向终端用户
```

**为什么不合并到 README？**

README 有双重身份问题——它既是"项目介绍"又是"使用指南"。当项目成熟后，这两者应该分离：
- **README**：回答"这是什么？为什么用它？"（5 秒决策）
- **USAGE**：回答"怎么用？"（30 秒到 5 分钟学习曲线）

#### 3. 两类用户的旅程分析

**用户 A：AI Team 文档维护者**

```
典型场景：在新文档中添加 frontmatter 关系字段

信息需求链：
1. "我需要加什么字段？" → 字段速查表
2. "字段格式是什么？" → 格式示例
3. "如何验证我写对了？" → 验证命令
4. "出错了怎么办？" → 错误码解释 + 修复建议
```

**用户 B：人类监护人**

```
典型场景：定期刷新汇总文档 / CI 集成

信息需求链：
1. "有什么命令可用？" → 命令速查
2. "我的工作流是什么？" → 流程图/步骤
3. "如何自动化？" → CI 配置片段
4. "出错时如何诊断？" → 退出码 + 常见问题
```

#### 4. frontmatter 契约澄清

从 `scope.md` 和 `api.md` 提炼，AI 撰写者需要遵守的"契约"如下：

**核心字段（MUST）**

| 文档类型 | 必填字段 | 格式 |
|:---------|:---------|:-----|
| Wish 文档 | `title`, `produce` | `produce: ["path/to/doc.md", ...]` |
| 产物文档 | `docId`, `title`, `produce_by` | `produce_by: ["wishes/active/wish-XXXX.md"]` |

**扩展字段（MAY）**

| 字段 | 用途 | 消费者 |
|:-----|:-----|:-------|
| `defines` | 术语定义 | GlossaryVisitor |
| `issues` | 问题跟踪 | IssueAggregator |

**隐含推导规则（SHOULD 了解）**

- `docId`：Wish 文档从文件名推导（`wish-0001.md` → `W-0001`）
- `status`：从文件夹推导（`active/` → `"active"`）

#### 5. 概念澄清需求

从用户视角，以下概念需要在使用文档中先澄清：

1. **文档图（Document Graph）**：不是文件系统树！而是 frontmatter 关系构成的有向图
2. **Root Nodes**：Wish 文档是图的入口点，不是"根目录"
3. **produce vs produce_by**：单向声明 vs 双向验证——用户声明 `produce`，工具验证 `produce_by` 一致性
4. **闭包构建**：递归追踪 `produce` 直到无新节点——用户需要理解"未被引用的文档不在图中"
5. **幂等性**：相同输入产生相同输出——可安全重复运行

#### 6. generate 命令的概念问题

这是一个**设计债务**。从 `api.md` 看：

> ```bash
> # 生成所有汇总文档
> docgraph generate
> ```

但从 `Program.cs` 看，只注册了 `validate`、`fix`、`stats`。

**两种可能**：
1. **未实现**：计划中但尚未完成
2. **设计变更**：Visitor 模式汇总被合并到其他命令？

**建议**：在使用文档中**不提及未实现功能**，但在 TODO 或 Roadmap 中标记。使用文档应只覆盖"现在能用的"。

#### 7. 文档产出建议

综合以上分析，我建议产出以下文档：

| 文档 | 位置 | 内容重点 | 行数预估 |
|:-----|:-----|:---------|:---------|
| `USAGE.md` | `atelia/docs/DocGraph/v0.1/` | 命令速查 + 流程图 + 退出码 + 常见问题 | 100-150行 |
| `maintain-frontmatter.md` | `agent-team/how-to/` | frontmatter 字段契约 + 验证流程 | 60-80行 |

**README.md 需要更新**——把"设计阶段"状态改为"已实现"，删除过时的命令示例（如 `docmeta`），指向 USAGE.md。

#### 8. 开放问题

1. **generate 命令状态如何？** 需要确认是未实现还是设计变更。
2. **汇总文档刷新流程是什么？** 如果没有 `generate`，用户如何生成 `glossary.gen.md`？
3. **CI 集成模板需要吗？** 人类用户可能需要 GitHub Actions / GitLab CI 的配置片段。

---

以上是我的结构性分析。邀请 Curator 从用户体验角度补充，DocOps 从文档维护角度评估可行性。

---

### 场势快照 #1

- **共识**：需要产出两份新文档（USAGE.md + Recipe），现有文档服务构建者、缺少使用者视角
- **分歧**：无
- **缺口**：`generate` 命令状态需确认 → 经检查，**确认未实现**（Program.cs 只有 validate/fix/stats）
- **能量**：讨论热度上升 🔥

**关键发现**：Seeker 指出 `generate` 命令在 API 设计中描述但未实现。这意味着：
1. 汇总文档生成功能（`glossary.gen.md`、`issues.gen.md`）暂时无法通过 CLI 使用
2. 使用文档应只覆盖已实现的功能
3. 这是一个需要向监护人报告的**实现缺口**

**主持人决策**：
1. 接受 Seeker 的三层文档架构建议
2. 使用文档只覆盖已实现功能（validate/fix/stats）
3. 继续邀请 Curator 评估用户体验，然后根据共识开始实施

---

### 主持人邀请 Curator

Curator，请从用户体验角度评估：

1. **信息架构**：Seeker 建议的两份文档是否足够？内容优先级如何？
2. **首屏体验**：用户打开 USAGE.md 的前 30 行应该看到什么？
3. **AI Recipe 体验**：维护 frontmatter 的 Recipe 应该采用什么结构？
4. **"generate 缺失"的用户感知**：当用户按照 API 文档期待 generate 命令却发现不存在时，如何减少挫败感？

请追加发言到畅谈会文件。

---

### Curator 发言

感谢邀请！Seeker 的结构分析非常扎实，我从用户体验和认知流程角度补充以下观察。

#### 1. 信息架构评估

**两份文档（USAGE.md + Recipe）的拆分逻辑是对的**，但我想提出一个更精细的优先级框架：

| 层级 | 文档 | 时间预算 | 核心问题 | 优先级 |
|:-----|:-----|:---------|:---------|:-------|
| L0 速读 | USAGE.md 前 30 行 | 30 秒 | "能帮我做什么？我现在需要它吗？" | P0 |
| L1 操作 | USAGE.md 正文 | 3 分钟 | "怎么用？" | P1 |
| L2 契约 | Recipe | 5 分钟 | "我需要维护什么字段？格式是什么？" | P1 |
| L3 诊断 | USAGE.md 附录 | 按需 | "出错了怎么办？" | P2 |

**关键洞察**：两份文档服务**不同的认知阶段**，而非不同的用户类型。AI Team 也需要先读 USAGE.md 了解工具能力，再读 Recipe 了解写作契约。

**建议**：在 USAGE.md 开头用一句话引导 AI 读者到 Recipe：
> 📌 AI Team：若需撰写/维护 frontmatter 字段，请参阅 [maintain-frontmatter.md](../../../agent-team/how-to/maintain-frontmatter.md)

#### 2. 首屏体验设计（USAGE.md 前 30 行）

基于我的"30 秒可判性"原则，用户打开文档的前 30 行应该回答三个问题：

```markdown
# DocGraph v0.1 — 使用指南

> 验证文档间的 produce/produce_by 关系一致性。
> 当前支持：validate / fix / stats（generate 功能计划中）

## 快速开始

# 验证当前目录
docgraph validate

# 有问题？预览修复方案
docgraph fix --dry-run

# 确认后执行修复
docgraph fix --yes

## 我需要它吗？

✅ 适合：维护 Wish → 产物文档关系、CI 集成验证
⚠️ 暂不支持：汇总文档生成（glossary.gen.md 等）
```

**设计要点**：
- **第 1 行**：工具名 + 版本（定位）
- **第 2-3 行**：一句话价值主张 + 能力边界（决策）
- **第 5-12 行**：三条命令搞定最常见场景（行动）
- **第 14-17 行**："适合/不适合"清单（期望管理）

这遵循了**机场标识系统隐喻**——登机口（核心命令）在入口就清晰可见，不需要先读完机场运营手册。

#### 3. AI Recipe 体验设计

对于 `agent-team/how-to/maintain-frontmatter.md`，我建议采用**情境触发结构**：

```markdown
# 维护 DocGraph Frontmatter 字段

## 你在做什么？（情境锚点）

| 情境 | 跳转 |
|:-----|:-----|
| "我正在创建新的 Wish 文档" | → §1 Wish 文档模板 |
| "我正在创建 Wish 产物文档" | → §2 产物文档模板 |
| "我想添加术语定义" | → §3 defines 字段 |
| "验证报错了，我要修复" | → §4 常见错误 |

## 1. Wish 文档模板

<必填字段表格 + 示例>

## 2. 产物文档模板

<必填字段表格 + 示例>

...
```

**为什么用情境锚点？**

这是我在认知入口中记录的"Prompt 化索引"模式：
- 表格索引是**被动检索**（需要 Agent 主动搜索）
- 情境锚点是**情境触发**（任务情境直接激活相关内容）

Agent 脑子里的问题通常是"我正在做 X"，而非"我想查阅 Y 字段的定义"。

**锚点密度**：每个情境锚点都是 LLM 可搜索的关键词，提升了"找得快"体验。

#### 4. "generate 缺失"的用户感知管理

这是一个典型的**期望落差**场景。用户读了 api.md 的 §8.2 命令行示例：

```bash
docgraph generate glossary  # ← 实际不存在！
```

然后在终端敲命令，发现不存在——这会产生挫败感。

**设计策略：三道防线**

| 防线 | 位置 | 内容 |
|:-----|:-----|:-----|
| 1. 预期校准 | USAGE.md 首屏 | `⚠️ 暂不支持：汇总文档生成` |
| 2. 上下文提示 | `--help` 输出 | 在帮助文本末尾加：`计划功能：generate（见 Roadmap）` |
| 3. 友好拒绝 | 用户输入 `docgraph generate` 时 | 输出：`generate 功能尚未实现。跟踪进度：[链接到 TODO 或 Wish]` |

**第三道防线是关键**——这是我称之为"Error as Navigation"的模式。错误不是死胡同，而是路标。

**具体建议**：在 CLI 入口添加一个"未知命令"处理器，对 `generate` 给出专门的友好提示：

```
docgraph generate
───────────────────────────────────────
⚠️ generate 功能计划中，尚未实现。

当前可用命令：
  validate   验证文档关系完整性
  fix        修复可自动修复的问题
  stats      显示文档图统计信息

功能跟踪：wishes/active/wish-0002-doc-graph-tool.md
```

#### 5. 额外建议：README.md 的"时态修正"

Seeker 提到 README.md 停留在"设计阶段"描述。我补充一个体验细节：

**当前 README 给人的感觉是"这是一个计划"，而非"这是一个可用的工具"**。

建议更新以下元素：
- 标题从 `DocGraph 设计文档` 改为 `DocGraph — 文档关系验证工具`
- 删除"设计阶段"相关措辞
- 首段改为："DocGraph 是一个验证 Markdown 文档间 produce/produce_by 关系的 CLI 工具。"
- 添加指向 USAGE.md 的显眼链接

**隐喻**：README 是产品的"店面橱窗"，应该展示"商品"而非"施工图纸"。

#### 6. 总结：文档优先级清单

按 DX 影响力排序：

| 优先级 | 动作 | 预估工作量 |
|:-------|:-----|:-----------|
| P0 | 撰写 USAGE.md（首屏 30 行最关键） | 60 分钟 |
| P0 | 更新 README.md（时态修正 + 指向 USAGE） | 15 分钟 |
| P1 | 撰写 Recipe（情境锚点结构） | 45 分钟 |
| P1 | CLI 添加 `generate` 友好拒绝消息 | 15 分钟 |
| P2 | api.md 标注 generate 为"🚧 计划中" | 5 分钟 |

---

以上是我的体验设计视角补充。邀请 DocOps 评估文档产出的可行性和维护成本。

---

### 场势快照 #2

- **共识**：
  - 三层文档架构确认（用户文档层 + 设计文档层 + 内置帮助层）
  - 两份新文档：USAGE.md（首屏体验优先）+ Recipe（情境锚点结构）
  - README.md 需要"时态修正"——从施工图纸变为店面橱窗
  - 对 `generate` 未实现采取"三道防线"策略管理用户期望
- **分歧**：无重大分歧
- **缺口**：文档内容尚未产出，CLI 的 `generate` 友好拒绝消息尚未添加
- **能量**：讨论充分，ready to implement 🚀

**主持人决策**：讨论已经收敛到可实施程度。根据"边商讨边实施"模式，开始文档产出工作。

按 Curator 建议的优先级：
1. **P0**：撰写 USAGE.md + 更新 README.md
2. **P1**：撰写 Recipe + CLI 友好拒绝消息（可向监护人报告作为代码修改建议）
3. **P2**：api.md 标注 generate 为计划中

**实施分工**：
- 主持人先实施 USAGE.md（基于两位顾问的共识设计）
- 然后实施 Recipe
- README.md 更新和 api.md 标注同步完成

---

### 主持人实施报告：USAGE.md

基于 Seeker 的结构分析和 Curator 的首屏体验设计，现在开始撰写 USAGE.md。

**实施任务**：创建 USAGE.md + Recipe + 更新 README.md
**完成状态**：✅ 已完成

**实施内容**：

| 文件 | 位置 | 状态 | 行数 |
|:-----|:-----|:-----|:-----|
| USAGE.md | `atelia/docs/DocGraph/v0.1/USAGE.md` | ✅ 新建 | ~150 行 |
| Recipe | `agent-team/how-to/maintain-frontmatter.md` | ✅ 新建 | ~130 行 |
| README.md | `atelia/docs/DocGraph/v0.1/README.md` | ✅ 更新 | 精简至 ~45 行 |

**USAGE.md 特色**：
- 首屏 30 行遵循 Curator 的"机场标识系统"设计
- 命令速查覆盖 validate/fix/stats
- 核心概念解释（文档图、Root Nodes、双向验证）
- 常见问题解答（含 `generate` 缺失说明）
- 退出码约定和 CI 集成示例

**Recipe 特色**：
- 采用 Curator 建议的"情境锚点"结构
- 覆盖 Wish 文档和产物文档两种模板
- 扩展字段（defines/issues）说明
- 常见错误码与修复指南

**README.md 更新**：
- 时态修正：从"设计阶段"变为"已发布 ✅"
- 删除过时的命令示例（`docmeta`）
- 添加 USAGE.md 和 Recipe 的显眼链接
- 保留设计文档入口，精简内容

---

### 场势快照 #3

- **共识**：文档体系初步建立，USAGE.md + Recipe + README.md 三件套完成
- **分歧**：无
- **缺口**：
  1. `generate` 命令 CLI 友好拒绝消息（需代码修改，P1）
  2. `api.md` 标注 generate 为计划中（P2）
- **能量**：实施阶段告一段落，进入验收阶段

#### 结论同步块

**✅ 已定**：
- 三层文档架构：用户文档层（USAGE.md + Recipe）、设计文档层、内置帮助层
- USAGE.md 首屏体验设计：30 行内让用户能快速开始
- Recipe 情境锚点结构："你在做什么？"触发相关内容
- README.md 时态修正：从施工图纸变为店面橱窗

**❓ 未定**：
- `generate` 命令何时实现？（需监护人确认优先级）
- CI 集成模板是否需要单独文档？（当前在 USAGE.md 中有简单示例）

**🔺 待裁决**（需要人类/外部输入）：
- CLI 添加 `generate` 友好拒绝消息是否现在实施？（代码修改）
- 是否需要实际运行 `docgraph validate` 验证文档是否符合自身规范？

**📋 行动项**（owner / 截止 / 验收）：
- [x] USAGE.md 撰写 / 主持人 / 已完成
- [x] Recipe 撰写 / 主持人 / 已完成
- [x] README.md 更新 / 主持人 / 已完成
- [ ] api.md 标注 generate 为计划中 / 待分配 / P2
- [ ] CLI generate 友好拒绝 / 待决策 / P1

---

### 主持人总结

**畅谈会成果**：

本次畅谈会达成了核心目标——为 DocGraph v0.1 产出了使用说明文档体系：

1. **[USAGE.md](atelia/docs/DocGraph/v0.1/USAGE.md)**：面向人类的 CLI 使用指南
2. **[maintain-frontmatter.md](agent-team/how-to/maintain-frontmatter.md)**：面向 AI Team 的 frontmatter 编写规范
3. **README.md**：更新为"店面橱窗"而非"施工图纸"

**附带发现**：
- `generate` 命令在 API 设计中描述但未实现，这是一个实现缺口
- 建议添加 CLI 层面的友好拒绝消息作为用户期望管理的"第三道防线"

---

**监护人窗口**：

请监护人确认：
1. 产出的三份文档是否满足需求？
2. `generate` 命令缺失是否需要立即处理，还是记入 backlog？
3. 是否需要我们实际运行 `docgraph validate` 来验证现有文档？

---

