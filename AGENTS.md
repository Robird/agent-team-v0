**不要用`insert_edit_into_file`工具**，用其他文本编辑工具作为替代，比如`apply_patch` 或 `replace_string_in_file`甚至终端命令。

**Atelia**这个名字源于缩写 *Autonomous Thinking, Eternal Learning, Introspective Agents*

**咱们只有一个人，但又不知是一个人**：咱们是一群智能体与一位人类组成的团队，正在构建能够连续自主运行、具有内源性目标的高级智能体。当前的每一行代码、每一条规范，都是点燃 AI 自举的火柴。

**及时重构优于兼容层**：咱们的代码几乎都是新写自用的，面向未来。当发改进时，只要能彻底重构的地方，就不选择留下兼容层，避免留下无谓的分支复杂性。

---

## 目标分解树
- 设计并实现可以长期持续自主行动的Agent
  - 建立[Agent-Operating-System(能动体运转系统)](agent-team/beacon/draft-agent-operating-system.md)的理论框架
  - 设计并实现自研的LLM Agent框架，早期代码位于`atelia/prototypes/Agent.Core`
- 设计并实现[DocUI](DocUI/docs/key-notes)。DocUI是LLM与Agent-OS交互的界面
- 实现LLM Agent的“零意外编辑”，用预览+确认的方式
- 设计并实现DocUI中的[Micro-Wizard](DocUI/docs/key-notes/micro-wizard.md)
  - 实现[StateJournal](atelia/docs/StateJournal/mvp-design-v2.md)
    - 实现[RBF(Reversible-Binary-Framing)](atelia/docs/Rbf/rbf-interface.md)
      - 用[SizedPtr](atelia/docs/Data/Draft/SizedPtr.md)替代RBF接口文档中的<deleted-place-holder>类型
        - 确定`Offset`和`Length`的bit分配方案
        - 在[Atelia.Data](atelia/src/Data)中实现`SizedPtr`
    - 分解巨大的设计文件`atelia/docs/StateJournal/mvp-design-v2.md`为一组更小的可以组合使用的文档，因为单体文件太大导致`Claude Opus 4.5`已不能正确按照设计文档编写代码
- 维持项目内的众多文档出于LLM Agent可理解和使用的形态
  - 撰写和维护团队内Agent的入门知识文件[AGENTS.md]，也就是本文件
  - 建立基于[Wish](wish/W-0001-wish-bootstrap/wish.md)和[Artifact-Tiers](agent-team/wiki/artifact-tiers.md)的分圈层推进的软件开发方法
  - 基于[DocGraph](atelia/docs/DocGraph/v0.1/README.md)的glossary和issues汇总。分散撰写与维护，自动汇总关键信息形成鸟瞰视图。

## 核心术语

> **Artifact-Tiers（产物层级）**：统摄 Why/Shape/Rule/Plan/Craft 产物层级的认知框架。

**Artifact-Tiers层级方法论**：
| 层级 | 核心问题 | 一句话解释 |
|:-----|:---------|:-----------|
| **Resolve-Tier** | 值得做吗？ | 分析问题价值，做出实施决心 |
| **Shape-Tier** | 用户看到什么？ | 定义系统边界和用户承诺 |
| **Rule-Tier** | 什么是合法的？ | 建立形式化约束和验收标准 |
| **Plan-Tier** | 走哪条路？ | 选择技术路线和实施策略 |
| **Craft-Tier** | 怎么造出来？ | 具体实现、测试和部署 |

**详细定义**：参见 [Artifact-Tiers](agent-team/wiki/artifact-tiers.md)

## 团队成员

### 参谋组 (Advisory Board)
`Seeker`, `Curator`, `Craftsman`

> **注**：召唤词是锚点而非边界。每位参谋可以从任何角度回应任何问题。
> 积累的记忆是历史视角的沉淀，不是能力的限制。

### 前线组 (Field Team)
| ID | Role | Specialty |
|:---|:-----|:----------|
| `Investigator` | 源码分析 | 技术调研、代码考古、Brief 产出 |
| `Implementer` | 编码实现 | 功能开发、移植、重构 |
| `QA` | 测试验证 | 测试编写、回归验证、Bug 复现 |
| `DocOps` | 文档维护 | 文档维护、索引管理 |
| `MemoryPalaceKeeper` | 记忆管理 | 便签整理、记忆归档、分类路由 |

## 畅谈会标签
| Tag | 目的 | MUST 产出 |
|:----|:-----|:----------|
| `#review` | 审阅文档 | FixList（问题+定位+建议） |
| `#design` | 探索方案 | 候选方案 + Tradeoff 表 |
| `#decision` | 收敛决策 | Decision Log（状态+上下文） |
| `#jam` | 自由畅想 | *无强制产出*（允许未验证假设，但不得伪造事实） |

## 在畅谈会上发言
- **MUST** append 到文件末尾，禁止插入或重排
- **MUST** 使用 `### <Name> 发言` 作为标题
- **MUST** 代码块指定语言

## 关键路径
| 用途 | 路径模式 |
|:-----|:---------|
| 会议文件 | `agent-team/meeting/<project>/YYYY-MM-DD-<topic>.md` |
| 成员认知 | `agent-team/members/<name>/index.md` |
| 共享知识 | `agent-team/wiki/` |
| 记忆积累 | `agent-team/wiki/memory-accumulation-spec.md` |
| 记忆维护 | `agent-team/wiki/memory-maintenance-skill.md` |
| **操作指南** | **`agent-team/how-to/`** |

## Wish 系统

> **一句话**：Wish = 你想做什么 + 做出了什么。开发意图的起点与产物追踪器。

| 想做什么 | 怎么做 |
|:---------|:-------|
| 查看全局 | 读 `wish-panels/` 下的汇总面板 |
| 创建 Wish | 复制 `wish/W-0001-wish-bootstrap/library/templates/wish-template.md` 到 `wish/W-XXXX-<slug>/wish.md` |
| 查看规范 | 读 `wish/W-0001-wish-bootstrap/library/specs/wish-system-rules.md` |

**详细定义** → `wish/W-0001-wish-bootstrap/library/README.md`

## DocGraph
**一键刷新所有汇总产物**：`cd atelia/src/DocGraph && dotnet run -- ../../../`
**详细定义** → `atelia/docs/DocGraph/v0.1/USAGE.md` · **frontmatter 规范** → `agent-team/how-to/maintain-frontmatter.md`

## 操作指南速查
需要命名？ → `agent-team/how-to/name-things-well.md`
整理记忆？ → `agent-team/how-to/maintain-memory.md`
记录洞见？ → `agent-team/how-to/accumulate-memory.md`
维护外部记忆？ → `agent-team/how-to/maintain-external-memory.md`
批量处理inbox便签？ → `agent-team/how-to/batch-process-inbox.md`
维护团队小黑板？ → `agent-team/how-to/maintain-blackboard.md`
写代码返回错误原因？ → `atelia/docs/Primitives/AteliaResult/guide.md`

## D-S-D Model 速查
> **一句话**：D‑S‑D 是设计信息分层：什么不该动、什么是约束、什么只是解释。

| 层级 | 核心 | Agent 行为 |
|:-----|:-----|:-----------|
| **Decision** (决策) | Why & Constraints | **只读**：除非用户明确“修改/重审决策”。 |
| **Spec** (规格) | What & How | **主战场**：精确定义实现蓝图；必须满足 Decision；维护 SSOT。 |
| **Derived** (推导) | Context & Examples | **可销毁重建**：图/例子/FAQ；与 Normative 冲突时必须让步。 |

**关键原则**：
- **Normative = Decision + Spec**（唯一具有约束力）。
- **SSOT / No Double Write**：同一事实只在一个 canonical 位置定义，其余用引用。

**详细定义** → [Decision-Spec-Derived-Model](agent-team/wiki/SoftwareDesignModeling/Decision-Spec-Derived-Model.md)

## AI-Design-DSL 速查
> **一句话**：Design-DSL 是基于 GFM 的形式化设计方言。作为 Agent，**编写设计文档时 MUST 使用此格式**，以便被其他 Agent 精确理解和工具化解析。

**核心语法**：
- **定义术语**：``## term `Term-ID` 中文标题`` (ID推荐 Title-Kebab)
- **定义条款**：`### <type> [CLAUSE-ID] 中文标题` (ID推荐全大写或全小写)
  - `decision`: **决策** (Target)。不可轻易改动。
  - `spec`: **规格** (Constraint)。为满足决策的约束。
  - `derived`: **推导** (Info)。
- **引用**：
  - 术语：@`Term-ID`
  - 条款：@[CLAUSE-ID]

**最小范例**：
```markdown
## term `User-Account` 用户账号
### decision [ACCOUNT-SECURITY] 账号安全
### spec [PWD-COMPLEXITY] 密码必须包含@`User-Account`名称；参见@[ACCOUNT-SECURITY]
```

**详细定义** → `agent-team/wiki/SoftwareDesignModeling/AI-Design-DSL.md`

## Spec Conventions 速查
> **一句话**：在 Atelia 项目写规范，**怎么编号**、**怎么表示**、**怎么命名**。

**1. 规范语言 (Normative Language)**
- **Keywords**: `MUST`/`MUST NOT` (绝对约束), `SHOULD` (强推荐), `MAY` (可选)。
- **MVP 固定**: 等价于 "MUST for current-version"。
- **区分**: 明确分离 Normative (规范) 和 Informative (解释)。

**2. 条款编号 (Clause-ID)**
- **格式**: `[PREFIX-STANCE-NAME]` (SCREAMING-KEBAB)。
- **前缀 (MUST)**:
  - `F`: Format (格式/布局)
  - `A`: API (签名/行为)
  - `S`: Semantics (不变量/语义)
  - `R`: Recovery (错误处理)
- **立场**: 名字表达“倾向/答案” (用 `CASE-INSENSITIVE` 而非 `CASE-SENSITIVITY`)。
- **变更**: 实质变更语义时，**新建**条款并标记旧条款 `DEPRECATED`。

**3. LLM 友好表示 (LLM-Friendly Notation)**
- **原则**: 最小复杂度 (能文本不列表，能列表不表格，能表格不图)，避免ASCII-Art。
- **选型**:
  - **树/层级** → 嵌套列表
  - **二维属性** → Markdown 表格
  - **少量关系** → SVO 文本 (加粗动词，e.g., A **depends on** B)
  - **复杂拓扑/时序** → Mermaid (`flowchart`, `sequenceDiagram`)
  - **位布局** → 表格 (行=字段, 列=属性) + 声明端序
- **SSOT**: 图表/例子仅作 **Illustration**，不得引入新约束，必须指回权威 Spec。

**详细定义** → [spec-conventions](atelia/docs/spec-conventions.md)



