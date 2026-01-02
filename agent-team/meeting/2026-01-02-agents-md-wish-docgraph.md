# 畅谈会：AGENTS.md 增补 Wish 与 DocGraph 信息

> **日期**：2026-01-02
> **标签**：#design #jam
> **主持人**：TeamLeader
> **参与者**：Seeker, Curator, Craftsman, DocOps, Investigator
> **状态**：进行中

---

## 背景
AGENTS.md 是给所有 LLM Agent 看的团队内写作所需的最小必要信息文件，类似人类的义务教育和公司快速上岗培训。目前缺少关于如何使用 Wish 与 DocGraph 的信息。

## 目标
基于调查到的信息和文本撰写共识，撰写一份极简但包含了必要信息的"最小须知与指导"，增补到 AGENTS.md 中。

## 💬 畅谈记录

### 主持人发言

大家好！今天我们要为 AGENTS.md 增补关于 Wish 与 DocGraph 的信息。AGENTS.md 是给所有 LLM Agent 看的"最小必要信息文件"，类似上岗培训手册。

**核心问题**：
1. 对于 Wish 系统，Agent 需要知道哪些最小必要信息？
2. 对于 DocGraph，Agent 需要知道哪些最小必要信息？
3. 如何以极简但完整的方式呈现这些信息？

**期望产出**：一段可以增补到 AGENTS.md 的文本，包含 Wish 和 DocGraph 的"最小须知与指导"。

让我们先邀请 @Investigator 调查一下当前项目中 Wish 和 DocGraph 的相关文档和实现情况。

**邀请格式**：
```yaml
# @Investigator
taskTag: "#design"
chatroomFile: "agent-team/meeting/2026-01-02-agents-md-wish-docgraph.md"
appendHeading: "### Investigator 发言"
scope: "调查 Wish 和 DocGraph 的当前文档、实现、使用方式"
outputForm: "Markdown 要点列表，包含关键路径、核心概念、当前状态"
```

请遵守 Lens-Orchestra 协议：开头做透镜声明，结尾给接力请求。

---

### Investigator 发言

> **透镜声明**：我从**源码分析与文档考古**视角回应，聚焦于 Wish 和 DocGraph 的当前文档结构、实现状态、使用方式。

## 调查结论

### 一、Wish 系统

#### 关键路径
| 用途 | 路径 |
|:-----|:-----|
| 入口索引 | `wishes/index.md` |
| 系统说明 | `wishes/README.md` |
| 规范条款 | `wishes/specs/wish-system-rules.md` |
| 文档模板 | `wishes/templates/wish-template.md` |
| 活跃 Wish | `wishes/active/wish-XXXX-<slug>.md` |

#### 核心概念
1. **Wish = 意图载体 + 产物索引**：动机驱动的开发工作流入口节点
2. **WishId 格式**：`W-XXXX`（如 `W-0001`）
3. **状态目录**：`active/` → `completed/` → `abandoned/`（互斥存放）
4. **层级进度追踪**：每个 Wish 用表格追踪 5 个 Artifact-Tier 的完成状态
5. **index.md 是派生视图**：可从各目录扫描重建，提供"从哪开始读"和"当前阻塞"两条最短路径

#### 当前状态
- **W-0001（Wish 系统自举）**：Resolve/Shape/Rule 已完成🟢，Plan 进行中🟡
- **W-0002（DocGraph）**：Resolve 完成🟢，Shape 进行中🟡，其余未开始⚪
- **使用方式**：手工维护（MVP 阶段），无自动化工具

#### Agent 最小须知
```
创建 Wish → 复制 wishes/templates/wish-template.md 到 wishes/active/
Wish 完成 → 移动到 wishes/completed/
Wish 放弃 → 移动到 wishes/abandoned/
查看全局 → 阅读 wishes/index.md
```

---

### 二、DocGraph

#### 关键路径
| 用途 | 路径 |
|:-----|:-----|
| 版本概览 | `atelia/docs/DocGraph/README.md` |
| 功能边界 | `atelia/docs/DocGraph/v0.1/scope.md` |
| 使用指南 | `atelia/docs/DocGraph/v0.1/USAGE.md` |
| frontmatter 规范 | `agent-team/how-to/maintain-frontmatter.md` |
| 源码位置 | `atelia/src/DocGraph/` |
| 生成产物 | `docs/glossary.gen.md`, `docs/issues.gen.md` |

#### 核心概念
1. **定位**：文档关系验证器（v0.1 MVP），验证 `produce` ↔ `produce_by` 双向链接
2. **文档图 ≠ 文件系统树**：基于 frontmatter 关系构成的有向图
3. **Root Nodes = Wish 文档**：扫描 `wishes/active/` 和 `wishes/completed/`
4. **produce 关系**：Wish → 产物文档（由 Wish 声明）
5. **produce_by 关系**：产物 → Wish（由产物文档声明，工具验证一致性）

#### 已实现功能（v0.1）
| 命令 | 功能 | 状态 |
|:-----|:-----|:-----|
| `validate` | 验证双向链接一致性 | ✅ 已实现 |
| `fix` | 自动修复缺失 frontmatter | ✅ 已实现 |
| `stats` | 显示文档图统计 | ✅ 已实现 |
| `generate` | 汇总文档生成 | ✅ 已实现 |

#### Agent 最小须知
```bash
# 运行验证
cd atelia/src/DocGraph && dotnet run -- validate ../../../

# 预览修复
dotnet run -- fix ../../../ --dry-run

# 执行修复
dotnet run -- fix ../../../ --yes

# 生成汇总（glossary.gen.md, issues.gen.md）
dotnet run -- generate ../../../
```

#### frontmatter 字段速查
| 文档类型 | 必填字段 |
|:---------|:---------|
| Wish 文档 | `title`, `produce: [路径数组]` |
| 产物文档 | `docId`, `title`, `produce_by: [Wish路径数组]` |
| 扩展字段 | `glossary: [{术语: 定义}]`, `issues: [{description, status}]` |

---

### 三、为 AGENTS.md 增补的建议要点

基于调查，建议增补以下**最小必要信息**：

#### Wish 系统（约 6 行）
- 定义：Wish = 意图载体 + 产物索引，30 秒速读动机
- 创建：复制模板到 `wishes/active/`
- 状态迁移：`active/` → `completed/` / `abandoned/`
- 索引入口：`wishes/index.md`

#### DocGraph（约 8 行）
- 定义：文档关系验证器，验证 `produce` ↔ `produce_by` 双向链接
- 运行：`cd atelia/src/DocGraph && dotnet run -- validate ../../../`
- frontmatter 规范：参见 `agent-team/how-to/maintain-frontmatter.md`
- 生成产物：`docs/glossary.gen.md`, `docs/issues.gen.md`

---

### 接力请求

> 调查完毕，建议由 **@Curator**（体验视角）或 **@DocOps**（文档维护视角）基于以上要点，提炼出适合 AGENTS.md 的极简文本草案。关键问题：
> 1. 信息密度：如何用最少文字让新 Agent 5 秒建立概念？
> 2. 操作路径：创建 Wish / 运行 DocGraph 的最短指令是什么？
> 3. 进阶入口：想深入了解时该读哪个文件？

---

### 场势快照

- **共识**：Investigator 已完成全面调查，提供了 Wish 和 DocGraph 的详细现状
- **分歧**：无
- **层级**：当前在 Shape-Tier（定义用户看到什么）和 Rule-Tier（定义合法操作）之间
- **缺口**：需要将调查结果转化为适合 AGENTS.md 的极简文本
- **能量**：平稳，调查已完成，准备进入提炼阶段

### 下一步动作：邀请 Curator

现在邀请 @Curator 从用户体验角度，基于 Investigator 的调查结果，提炼出适合 AGENTS.md 的极简文本草案。

**锚点问题**：
1. 对于新加入的 Agent，关于 Wish 和 DocGraph 的"5秒建立概念"信息是什么？
2. 最常用的操作路径（创建 Wish、运行 DocGraph）应该怎么描述最简洁？
3. 如何平衡信息完整性和简洁性？

请遵守 Lens-Orchestra 协议：开头做透镜声明，结尾给接力请求。

---

### Curator 发言

> **透镜声明**：我从**用户体验与认知界面设计**视角回应，聚焦于"5秒建立概念"、"30秒可操作"、"进阶入口清晰"的三层体验设计。

## 设计原则

基于我积累的 **文档分层的时间预算** 原则：
- **L0（5秒）**：这是什么？我需要吗？
- **L1（30秒）**：怎么用？
- **L2（按需）**：去哪了解更多？

AGENTS.md 的目标受众是"刚加入团队的 LLM Agent"——他们需要**机场标识牌**式的导航，而非机场运营手册。

---

## 极简文本草案（建议增补到 AGENTS.md）

### 方案 A：表格式（信息密度高，扫描友好）

```markdown
## Wish 系统
> **一句话**：Wish = 你想做什么 + 做出了什么。每个开发意图的起点和产物追踪器。

| 想做什么 | 怎么做 |
|:---------|:-------|
| 查看全局 | `wishes/index.md` |
| 创建 Wish | 复制 `wishes/templates/wish-template.md` → `wishes/active/` |
| 完成 Wish | 移动到 `wishes/completed/` |
| 放弃 Wish | 移动到 `wishes/abandoned/` |

**深入了解** → `wishes/README.md`

## DocGraph
> **一句话**：文档关系验证器。确保 Wish 与产物文档的双向链接一致。

| 想做什么 | 怎么做 |
|:---------|:-------|
| 验证链接 | `cd atelia/src/DocGraph && dotnet run -- validate ../../../` |
| 自动修复 | `dotnet run -- fix ../../../ --yes` |
| 生成汇总 | `dotnet run -- generate ../../../` |

**深入了解** → `atelia/docs/DocGraph/v0.1/USAGE.md` · **frontmatter 规范** → `agent-team/how-to/maintain-frontmatter.md`
```

---

### 方案 B：流式（更口语化，降低认知阻尼）

```markdown
## Wish 系统

**是什么**：Wish 是"开发意图"的载体——记录你想做什么、产出了什么。每个 Wish 追踪从决心到交付的完整生命周期。

**怎么用**：
- 查全局 → 读 `wishes/index.md`
- 开新 Wish → 复制 `wishes/templates/wish-template.md` 到 `wishes/active/`
- 完成/放弃 → 移动到 `wishes/completed/` 或 `wishes/abandoned/`

**了解更多** → `wishes/README.md`

## DocGraph

**是什么**：文档关系验证器——检查 Wish 声明的产物（`produce`）与产物文档的反向声明（`produce_by`）是否一致。

**怎么用**：
```bash
cd atelia/src/DocGraph && dotnet run -- validate ../../../  # 验证
dotnet run -- fix ../../../ --yes                           # 修复
dotnet run -- generate ../../../                            # 生成汇总
```

**了解更多** → `atelia/docs/DocGraph/v0.1/USAGE.md` | **frontmatter** → `agent-team/how-to/maintain-frontmatter.md`
```

---

## 方案对比

| 维度 | 方案 A（表格式） | 方案 B（流式） |
|:-----|:-----------------|:---------------|
| 扫描效率 | ⭐⭐⭐ 高（表格结构化） | ⭐⭐ 中（需顺序读） |
| 认知亲和 | ⭐⭐ 中（偏工具书感） | ⭐⭐⭐ 高（像人在说话） |
| AGENTS.md 风格一致性 | ⭐⭐⭐ 高（现有内容多用表格） | ⭐⭐ 中（需检查现有风格） |
| 字数 | ~180 字 | ~200 字 |

**我的推荐**：**方案 A**——与 AGENTS.md 现有的表格风格一致，且 LLM 对表格的解析效率更高。

---

## 体验优化细节

### 1. "一句话定义"的设计意图
- **Wish**："你想做什么 + 做出了什么"——动词感强，暗示行动
- **DocGraph**："确保双向链接一致"——聚焦核心价值，不解释实现

### 2. "想做什么→怎么做"的表头设计
这是 **情境触发** 模式——Agent 从任务意图出发，直接找到操作路径。优于"命令→功能"的工具书式呈现。

### 3. 深入了解的分层
- **Wish**：单入口 `README.md`（足够简单）
- **DocGraph**：双入口 `USAGE.md` + `maintain-frontmatter.md`（操作指南 + 格式规范分离）

---

## 接力请求

> 草案已完成，建议由 **@DocOps** 或 **主持人** 决定：
> 1. **风格选择**：方案 A 还是方案 B？
> 2. **插入位置**：建议放在 AGENTS.md 的"关键路径"表格之后，"操作指南速查"之前
> 3. **最终润色**：是否需要进一步压缩字数？

如果决定采纳，下一步是实施增补。

---

### 场势快照

- **共识**：Curator 已提供两个优秀的草案方案（表格式和流式），并推荐方案 A
- **分歧**：无
- **层级**：当前在 Shape-Tier（定义用户看到什么）和 Craft-Tier（具体实现）之间
- **缺口**：需要 DocOps 从文档维护角度做最终决策和润色
- **能量**：平稳，有明确产出，准备进入决策阶段

### 下一步动作：邀请 DocOps

现在邀请 @DocOps 从文档维护角度，基于 Curator 的两个方案，做出最终决策并进行必要的润色。

**锚点问题**：
1. 哪个方案更适合 AGENTS.md 的整体风格和结构？
2. 插入位置是否合适？是否需要调整？
3. 是否需要进一步压缩或优化文本？

请遵守 Lens-Orchestra 协议：开头做透镜声明，结尾给接力请求。


### DocOps 发言

> **透镜声明**：我从**文档一致性与索引管理**视角回应，聚焦于 AGENTS.md 的结构风格、信息架构一致性、以及长期可维护性。

## 决策：采用方案 A（表格式）

### 决策依据

**1. 风格一致性分析**

AGENTS.md 现有结构高度依赖表格：
- "Artifact-Tiers层级方法论"：3 列表格
- "参谋组"/"前线组"：3 列表格
- "畅谈会标签"：3 列表格
- "关键路径"：2 列表格

方案 A 的"想做什么→怎么做"双列表格与"关键路径"表格结构完全一致。方案 B 的流式列表会破坏这种视觉节奏。

**2. LLM 解析效率**

表格对 LLM 是结构化数据，可直接索引。流式列表需要语义解析，增加认知负载。

**3. 信息密度**

方案 A 约 180 字，方案 B 约 200 字。表格以更少字数传递相同信息量。

### 决策结论

| 维度 | 选择 | 理由 |
|:-----|:-----|:-----|
| 风格 | **方案 A（表格式）** | 与现有 7 处表格结构一致 |
| 位置 | "关键路径"之后，"操作指南速查"之前 | 形成「路径→工具→操作」递进 |

---

## 润色后的最终文本

```markdown
## Wish 系统

> **一句话**：Wish = 你想做什么 + 做出了什么。开发意图的起点与产物追踪器。

| 想做什么 | 怎么做 |
|:---------|:-------|
| 查看全局 | 读 `wishes/index.md` |
| 创建 Wish | 复制 `wishes/templates/wish-template.md` → `wishes/active/` |
| 完成 Wish | 移动到 `wishes/completed/` |
| 放弃 Wish | 移动到 `wishes/abandoned/` |

**详细定义** → `wishes/README.md`

## DocGraph

> **一句话**：文档关系验证器。校验 Wish 的 `produce` 与产物的 `produce_by` 双向链接。

| 想做什么 | 怎么做 |
|:---------|:-------|
| 验证链接 | `cd atelia/src/DocGraph && dotnet run -- validate ../../../` |
| 自动修复 | `dotnet run -- fix ../../../ --yes` |
| 生成汇总 | `dotnet run -- generate ../../../` |

**详细定义** → `atelia/docs/DocGraph/v0.1/USAGE.md` · **frontmatter 规范** → `agent-team/how-to/maintain-frontmatter.md`
```

### 润色变更说明

| 原文 | 润色后 | 理由 |
|:-----|:-------|:-----|
| "每个开发意图的起点和产物追踪器" | "开发意图的起点与产物追踪器" | 删除冗余"每个"，用"与"替代"和"更书面 |
| "确保 Wish 与产物文档的双向链接一致" | "校验 Wish 的 `produce` 与产物的 `produce_by` 双向链接" | 显式提及字段名，强化操作性 |
| `wishes/index.md` | 读 `wishes/index.md` | 添加动词，与下方"复制/移动"平行 |
| "深入了解" | "详细定义" | 与 AGENTS.md 现有"**详细定义**：参见 Artifact-Tiers"措辞统一 |

---

## 插入位置确认

**在 AGENTS.md 中的锚点**：

```markdown
## 关键路径
| 用途 | 路径模式 |
...
| **操作指南** | **`agent-team/how-to/`** |

⬇️ 【插入位置】

## 操作指南速查
需要命名？ → ...
```

### 理由
1. **递进逻辑**：关键路径（在哪）→ 核心工具（用什么）→ 操作指南（怎么做）
2. **视觉节奏**：两个新 section（Wish/DocGraph）使用相同的表格结构，与上方"关键路径"表格呼应
3. **信息完整性**：Wish 和 DocGraph 在"目标分解树"中已被提及但未展开，此处补全操作指南

---

## 接力请求

> 决策已完成，建议由 **主持人** 确认后：
> 1. **实施增补**：将润色后的文本插入 AGENTS.md 指定位置
> 2. **验证一致性**：运行 DocGraph validate 确认新增内容不破坏文档图
> 3. **更新 Wish**：如有对应 Wish，更新其进度状态

如需立即实施，我可以直接编辑 AGENTS.md。

---

### 场势快照

- **共识**：DocOps 已做出最终决策，采用方案 A（表格式），并进行了润色
- **分歧**：无
- **层级**：当前在 Craft-Tier（具体实现）
- **缺口**：需要实施增补到 AGENTS.md
- **能量**：平稳，准备进入实施阶段

### 实施完成

已成功实施增补：

1. **插入位置**：在 AGENTS.md 的"关键路径"表格之后，"操作指南速查"之前
2. **插入内容**：使用 DocOps 润色后的最终文本（方案 A 表格式）
3. **验证结果**：运行 DocGraph validate 验证通过，无问题发现

增补已完成，AGENTS.md 现在包含了 Wish 和 DocGraph 的最小必要信息。

---

### 结论同步块

**✅ 已定**：
- 采用方案 A（表格式）增补 Wish 和 DocGraph 信息到 AGENTS.md
- 插入位置："关键路径"表格之后，"操作指南速查"之前
- 文本内容已润色优化，保持与现有风格一致
- DocGraph validate 验证通过，文档一致性完好

**❓ 未定**：
- 无

**📋 行动项**：
- 已完成：增补 AGENTS.md 中的 Wish 和 DocGraph 信息
- 已完成：运行 DocGraph validate 验证一致性
- 建议：如有对应 Wish，可更新其进度状态

**🔺 待裁决**：
- 无

---

状态：已完成 ✅

