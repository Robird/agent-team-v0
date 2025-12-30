---
meetingId: "M-2025-12-30-DocGraph-Terminology"
title: "DocGraph 术语命名畅谈会"
date: 2025-12-30
status: Active
participants: [Moderator, Seeker, Curator, Craftsman, DocOps, Implementer]
tags: [terminology, design, docgraph]
parentWish: "W-0002"
---

# DocGraph 术语命名畅谈会

> **会议目标**：为 L1/L2/L3/L4/L5 层级赋予语义化名字，避免抽象编号撞车
> **参考材料**：
> - `wishes/active/wish-0002-doc-graph-tool.md`
> - `wishes/specs/wish-system-rules.md`
> - `atelia/docs/DocGraph/api.md`
> - `atelia/docs/DocGraph/ore.md`

## 会议议程

1. **开场**：主持人介绍背景与目标
2. **现状分析**：当前 L1-L5 的使用模式与问题
3. **术语提案**：各成员提出命名方案
4. **收敛讨论**：评估候选方案，形成共识
5. **行动项**：确定后续执行计划

## 场势快照（会议开始前）

**当前状态**：
- W-0002 DocGraph 处于 L2 进行中状态
- 已有 L2 API 草案和 ORE 设计原矿
- L1-L5 目前仅作为抽象层级编号，缺乏语义化名字

**核心问题**：
- L1/L2/L3/L4/L5 太抽象，容易与其他系统的层级编号冲突
- 需要为每个层级赋予有意义的、反映其本质的术语

**约束条件**：
- 术语应反映层级本质（Why/What/Rules/How/Build）
- 术语应具有区分度，避免与其他领域术语冲突
- 术语应便于记忆和传播

---

### Moderator 发言

大家好！我是本次畅谈会的主持人。让我们开始这次关于 DocGraph 术语命名的畅谈会。

**开场白**：
我们聚集在这里，是因为我们意识到一个重要的设计问题：当前使用的 L1/L2/L3/L4/L5 层级编号虽然简洁，但过于抽象，容易与其他系统的层级概念混淆。更重要的是，这些编号没有传达出每个层级的本质含义。

**会议目标**：
1. **必要目标**：为 L1-L5 赋予语义化名字
2. **随缘目标**：顺便审阅两份草稿（api.md 和 ore.md），达成一些修订或设计方面的一致

**会议规则**：
- 每个成员发言后，我会进行场势快照和决策
- 只有需要监护人发言或提供信息时才暂停
- 让对话自然涌现，不预编排整个流程

**当前观察**：
从 ore.md 中，我看到 L1-L5 的实际含义已经隐含：
- L1: Why - 动机、问题陈述
- L2: What - 外观、接口设计  
- L3: Rules - 规范、条款
- L4: How - 实现方案、技术选型
- L5: Build - 构建、测试、部署

但这些只是描述，我们需要的是**术语**——简洁、有力、有区分度的名字。

**启动问题**：
我想先邀请 @Seeker 来帮我们追问本质：L1-L5 这些层级到底在解决什么根本问题？它们的核心区别是什么？

### Seeker 发言

感谢主持人的邀请。让我直接追问 L1-L5 的本质区别。

#### 1. 本质追问：每个层级回答不同的"元问题"

| 层级 | 当前标签 | 本质元问题 | 核心产出 |
|:-----|:---------|:-----------|:---------|
| **L1** | Why | 这是真问题吗？<br>值得投入吗？<br>不解决会怎样？ | 价值判断、动机陈述 |
| **L2** | What | 边界在哪里？<br>用户看到什么？<br>系统承诺什么？ | 界面契约、外观设计 |
| **L3** | Rules | 什么是合法的？<br>如何判定对错？<br>违反会怎样？ | 形式化约束、条款 |
| **L4** | How | 走哪条路？<br>为什么选 A 不选 B？<br>权衡依据是什么？ | 策略选择、技术路线 |
| **L5** | Build | 怎么造出来？<br>如何验证造对了？<br>如何确保可重复？ | 物化执行、测试验证 |

#### 2. 核心区别：不同的正确性判据

每个层级有独特的**正确性判据**：

- **L1**：人类价值判断（"这值得做"）
  - 判据：共鸣度、紧迫性、战略对齐
  - 隐喻：**罗盘**（指向方向）

- **L2**：易用性 + 完备性（"这能用"）
  - 判据：直觉性、覆盖关键场景、无歧义
  - 隐喻：**门面**（用户接触的界面）

- **L3**：形式化检验（"这合法"）
  - 判据：无矛盾、可判定、可自动化检查
  - 隐喻：**法典**（成文规则）

- **L4**：量化评估（"这可行"）
  - 判据：性能指标、成本效益、风险分析
  - 隐喻：**地图**（路径选择）

- **L5**：可执行性（"这能跑"）
  - 判据：测试通过、构建成功、部署运行
  - 隐喻：**施工**（实际建造）

#### 3. 术语提案

基于本质分析，我提出三套候选方案：

**方案 A（动词导向）**：
- L1: **Motivate**（激发动机）
- L2: **Shape**（塑造边界）
- L3: **Govern**（治理规则）
- L4: **Route**（规划路径）
- L5: **Craft**（精工建造）

**方案 B（名词导向）**：
- L1: **Purpose**（目的）
- L2: **Interface**（界面）
- L3: **Codex**（法典）
- L4: **Blueprint**（蓝图）
- L5: **Artifact**（产物）

**方案 C（混合，推荐）**：
- L1: **Why**（保持简洁）
- L2: **Shape**（强调边界塑造）
- L3: **Rules**（直观无歧义）
- L4: **Route**（路径选择）
- L5: **Craft**（技艺实现）

#### 4. 关键洞察

1. **层级边界的模糊性是特性而非缺陷**：这是连续光谱离散化的必然产物。五层级的价值在于让 80% 的决策清晰归类。

2. **向上依赖，向下不保证**：L2 依赖 L1 的价值判断，但 L1 正确不保证 L2 正确。每层都有独立的失败模式。

3. **术语应反映"决策类型"而非"文档类型"**：我们不是在命名文档，而是在命名**决策的抽象层级**。

我倾向于**方案 C**，因为它：
- 保留已被接受的 Why
- 用 Shape 替代 What（更强调主动塑造）
- 保持 Rules 的直观性
- 用 Route 替代 How（避免泛泛的"如何"）
- 用 Craft 替代 Build（强调技艺而非机械建造）

现在我想听听 @Curator 从用户体验和开发者体验角度的看法。

### Curator 发言

感谢 Seeker 的本质追问和系统化分析。我将从**使用者的实际体验**出发，评估这三套方案。

---

#### 1. UX/DX 分析表格

| 维度 | 方案 A（动词导向） | 方案 B（名词导向） | 方案 C（混合） |
|:-----|:-------------------|:-------------------|:---------------|
| **语法一致性** | ⭐⭐⭐⭐⭐ 全动词，可用于祈使句 | ⭐⭐⭐⭐⭐ 全名词，可用于标签 | ⭐⭐ 混合，语法场景受限 |
| **自解释性** | ⭐⭐⭐ Govern/Route 需解释 | ⭐⭐⭐ Codex/Blueprint 带领域色彩 | ⭐⭐⭐⭐ Why/Rules 直白 |
| **发音流畅度** | ⭐⭐⭐⭐ 双音节为主，朗朗上口 | ⭐⭐⭐ Interface/Blueprint 较长 | ⭐⭐⭐⭐ 短词为主 |
| **跨领域冲突** | ⭐⭐⭐ Route 与网络路由冲突 | ⭐⭐ Interface/Blueprint 高频词 | ⭐⭐⭐ Route 仍有冲突 |
| **可图标化** | ⭐⭐⭐ 动词难直接图标化 | ⭐⭐⭐⭐⭐ 名词天然可视觉化 | ⭐⭐⭐ 混合难统一视觉 |

**小结**：没有完美方案。方案 A 胜在一致性，方案 B 胜在可视觉化，方案 C 胜在直白但输在一致性。

---

#### 2. 记忆负担评估

> **核心问题**：新成员入职三天后，还记得几个术语？

| 方案 | 记忆曲线 | 传播性 | 证据 |
|:-----|:---------|:-------|:-----|
| **A** | 📈 中等 | 中等 | 动词有动作感，但 Govern/Route 缺乏直觉锚点 |
| **B** | 📈 较低 | 较高 | Codex/Blueprint 带叙事感，但"翻译成本"高——需先翻译成汉语理解 |
| **C** | 📈 **最高** | **最高** | Why/Rules/Craft 几乎零学习成本，Shape/Route 稍弱 |

**记忆心理学视角**：
- **Why/Rules/Craft** 是英语母语者和非母语者都熟悉的高频词，几乎不需要专门记忆。
- **Codex/Blueprint** 带有"工程隐喻"或"古典隐喻"，需要额外认知转换。
- **Govern** 虽准确，但对非母语者有"政府治理"的干扰联想。

**结论**：**方案 C 的记忆负担最低**，因为它最大程度利用了日常词汇。

---

#### 3. 视觉隐喻强度

> **设计问题**：如果要画图标或制作海报，哪个方案最有视觉张力？

| 层级 | 方案 A 隐喻 | 方案 B 隐喻 | 方案 C 隐喻 |
|:-----|:------------|:------------|:------------|
| L1 | 🔥 点火/激发 | 🎯 靶心/目标 | ❓ 问号/疑问 |
| L2 | 🫶 塑形/雕刻 | 🚪 门/界面 | 🫶 塑形 |
| L3 | ⚖️ 天平/法槌 | 📜 卷轴/法典 | 📏 尺子/规则 |
| L4 | 🗺️ 地图/路线 | 📐 蓝图 | 🗺️ 地图/路线 |
| L5 | 🔨 锤子/工匠 | 📦 产品/盒子 | 🔨 锤子/工匠 |

**视觉强度排名**：
1. **方案 B** ⭐⭐⭐⭐⭐ —— 名词天然对应实物，Codex（卷轴）+ Blueprint（蓝图）有很强的"物质感"
2. **方案 A** ⭐⭐⭐ —— 动词需转化为动作的"定格帧"才能可视化
3. **方案 C** ⭐⭐⭐ —— Why 的问号图标略显单薄，Rules 的尺子足够

**潜在问题**：如果未来需要制作 Atelia 文档系统的可视化仪表盘，方案 B 的图标设计会最自然。

---

#### 4. 实际使用场景示例

让我模拟三个真实场景：

**场景 1：日常对话**

| 场景 | 方案 A | 方案 B | 方案 C |
|:-----|:-------|:-------|:-------|
| 「这个决策属于哪一层？」 | "这是 Govern 层的问题。" | "这是 Codex 层的问题。" | "这是 Rules 层的问题。" |
| **自然度** | 🔸 中等（需解释 Govern） | 🔸 中等（Codex 显生僻） | ✅ 高（Rules 直观） |

**场景 2：文档标题**

| 场景 | 方案 A | 方案 B | 方案 C |
|:-----|:-------|:-------|:-------|
| L2 设计文档 | `DocGraph-Shape.md` | `DocGraph-Interface.md` | `DocGraph-Shape.md` |
| **区分度** | ✅ 高 | 🔸 中（Interface 泛用） | ✅ 高 |

**场景 3：代码注释**

```csharp
// 方案 A
// [Motivate] 为什么需要 DocGraph？

// 方案 B  
// [Purpose] 为什么需要 DocGraph？

// 方案 C
// [Why] 为什么需要 DocGraph？
```

**代码注释评估**：
- `[Why]` 最短，在 IDE 行内显示时占用空间最小
- `[Purpose]` 和 `[Motivate]` 都需要 7-8 字符
- 对于频繁出现在注释中的标签，**简洁压倒一切**

---

#### 5. 渐进式理解测试

> **设计问题**：新手能否从术语本身推断层级含义？

| 术语 | 新手首次猜测 | 与实际含义匹配度 |
|:-----|:-------------|:-----------------|
| **Why** | "为什么要做这件事" | ✅ 100% |
| **Shape** | "外形？结构？" | 🔸 60%（需解释是"塑造边界"） |
| **Rules** | "规则、约束" | ✅ 95% |
| **Route** | "路由？网络？" | 🔸 50%（与网络术语冲突） |
| **Craft** | "手工艺？制作" | ✅ 80%（略带文艺气息） |

**渐进式理解评估**：
- **Why / Rules / Craft**：几乎零解释成本
- **Shape / Route**：需要一句话解释来消歧

---

#### 6. 我的建议：改良版方案 C'

我支持 Seeker 推荐的 **方案 C**，但有一个微调建议：

| 层级 | 方案 C | 我的建议 C' | 理由 |
|:-----|:-------|:-----------|:-----|
| L1 | Why | **Why** ✅ | 保持 |
| L2 | Shape | **Shape** ✅ | 保持（动词感强，暗示主动塑造） |
| L3 | Rules | **Rules** ✅ | 保持 |
| L4 | Route | **Path** 🔄 | 避免与网络 routing 冲突；Path 更中性，强调"选择的道路" |
| L5 | Craft | **Craft** ✅ | 保持 |

**为什么改 Route → Path？**

1. **冲突规避**：在软件领域，"route" 几乎被 HTTP routing、event routing 垄断。开发者听到 "Route" 第一反应是 `@app.route('/')` 而非"策略选择"。
2. **隐喻精确性**："Path" 强调的是**已选择的道路**（结果），而 "Route" 强调的是**路线规划过程**（动作）。L4 讨论的是"选了哪条路"，而非"如何规划路线"。
3. **语感对称**：Why / Shape / Rules / Path / Craft —— 五个词中四个是单音节或双音节高频词，Path 比 Route 更日常。

---

#### 7. 最终立场

| 评估维度 | 最优方案 |
|:---------|:---------|
| 语法一致性 | 方案 A |
| 记忆负担 | 方案 C / C' |
| 视觉隐喻 | 方案 B |
| 日常对话 | 方案 C / C' |
| 代码注释 | 方案 C / C' |
| 渐进式理解 | 方案 C / C' |

**综合建议**：采用 **方案 C'**（Why → Shape → Rules → Path → Craft）

**核心理由**：
1. **记忆负担最低**：五个词中有四个是日常高频词
2. **对话最自然**：「这是 Rules 层的问题」比「这是 Codex 层的问题」直观
3. **代码注释最紧凑**：`[Why]` 比 `[Purpose]` 省 50% 字符
4. **渐进式理解友好**：新手读到术语就能猜对 80% 的含义

**补充说明**：
如果团队未来需要**强视觉化**（如仪表盘、海报），可以为每个术语定义官方图标，用图标弥补名词导向方案 B 的优势——术语用 C'，图标借鉴 B 的隐喻。

---

#### 8. 术语使用速查卡（体验设计产出）

如果采纳方案 C'，我建议创建一张"30 秒速查卡"：

```
┌─────────────────────────────────────────────────────┐
│  DocGraph 五层级术语速查                              │
├──────┬─────────┬──────────────────────────────────────┤
│ 层级 │ 术语    │ 一句话解释                            │
├──────┼─────────┼──────────────────────────────────────┤
│  L1  │ Why     │ 这问题值得解决吗？                    │
│  L2  │ Shape   │ 用户看到什么边界？                    │
│  L3  │ Rules   │ 什么是合法的？                       │
│  L4  │ Path    │ 走哪条路？                           │
│  L5  │ Craft   │ 怎么造出来？                         │
└──────┴─────────┴──────────────────────────────────────┘
```

这张卡片可以贴在团队 wiki 入口，让新成员 30 秒内建立心智模型。

---

以上是我的 UX/DX 评估。期待听到其他成员的观点。@Craftsman 可能会关注术语在规范文档中的条款化表达，@Implementer 可能会关注在代码中的实际标注体验。

### Craftsman 发言

下面从“规范审计/可判定性/可维护性”的角度，对方案 C（Why → Shape → Rules → Route → Craft）与方案 C'（Why → Shape → Rules → Path → Craft）做一次兼容性与冲突评估。

#### 1) 规范一致性审计结果（与现有规范/模板/文档的兼容性）

- **与 Wish 系统现状不一致（高影响）**：当前仓库已有事实上的层级命名 SSOT：
  - 在 [wishes/README.md](../../wishes/README.md) 与 [wishes/templates/wish-template.md](../../wishes/templates/wish-template.md) 中，层级产物写作 `L1(Why) → L2(What) → L3(Rules) → L4(How) → L5(Build)`。
  - 在 [atelia/docs/DocGraph/api.md](../../atelia/docs/DocGraph/api.md) 里明确写了“**层级: L2 What**”。
  - 在 [atelia/docs/DocGraph/ore.md](../../atelia/docs/DocGraph/ore.md) 的“后续提纯建议”仍使用 `L2 What / L4 How / L5 Build`。
  - 结论：若直接采用 C/C' 作为“唯一命名”，会对模板、README、已有 Wish 的 Layer Progress 表格形成**系统性漂移**；必须定义迁移策略或 alias 规则，否则会出现“同一层级两套名字并存”的 SSOT 双写风险（参见 [atelia/docs/spec-conventions.md](../../atelia/docs/spec-conventions.md) 的 `[S-DOC-SSOT-NO-DOUBLEWRITE]` 精神）。

- **与条款锚点体系一致（可控）**：本仓库规范条款采用“稳定语义锚点”`[F-*]/[A-*]/[S-*]/[R-*]`（见 [atelia/docs/spec-conventions.md](../../atelia/docs/spec-conventions.md)）。
  - 因此：层级术语（Why/Shape/Rules/Path/Craft）应当被视为“术语表与标题标签”，而不是替代/重写条款编号体系。

#### 2) 术语冲突检查结果（与现有代码库/文档的碰撞）

- **Route 的既有语义占用（强冲突）**：仓库内已经存在稳定的过程术语 **ROUTE（路由）**，用于“把内容分流写到哪里”的步骤（例如记忆维护流程中 `CLASSIFY → ROUTE → APPLY`）。
  - 这会导致“L4 Route（技术路线选择）”与“ROUTE（分流路由）”在跨文档讨论中产生**同形异义**，且两者都属于“流程/方法论”语境，极易混淆。
  - 结论：从冲突角度，**Route 不宜作为 L4 的 canonical 名称**。

- **Path 的领域高频占用（中-强冲突，尤其对 DocGraph）**：
  - DocGraph API 模型里已有 `Document.Path: string` 字段（见 [atelia/docs/DocGraph/api.md](../../atelia/docs/DocGraph/api.md) 的文档模型），并且全仓库里“path”普遍表示“文件路径/目录路径”。
  - 若 L4 也命名为 Path，则在讨论“Path 字段”与“Path 层级”时需要持续加前缀消歧（例如“FilePath vs L4-Path”），否则会产生歧义。
  - 结论：Path **比 Route 更好**（避免 HTTP routing 的第一联想，也避开现有 ROUTE 步骤），但它在 DocGraph 语境下仍有显著“文件路径”撞词成本。

- **Craft 与角色名的弱冲突（可接受）**：Craftsman 是团队角色名，但 Craft（技艺/实现）作为层级名仍可接受；只要在句子里保持“L5 Craft / Craftsman”两者不混用即可。

- **Rules/Why/Shape 的冲突较弱**：Rules 与 Why 在仓库中已大量出现且语义稳定；Shape 相对较少，但需额外一句话定义来避免“几何外形/格式 shape”的误读。

#### 3) 可扩展性评估（能否扩展到其他领域）

- **Why / Rules**：跨领域泛化能力强，属于“决策类型”而非“领域对象”，扩展性好。
- **Shape**：可扩展，但需要团队共识把它绑定为“边界/外观/承诺面”的意思；否则在不同领域会被误解为“数据结构 shape / UI shape”。
- **Route/Path**：两者都可扩展为“方案选择/路线”，但在软件语境里 Route 的既有占用更重；Path 的占用更广（文件路径、执行路径），扩展时要付出持续消歧成本。
- **Craft**：在工程/制造隐喻下可扩展；但如果未来扩展到更偏“研究/写作”的领域，Craft 会带一点“手作/匠作”的风格色彩（可接受，但要意识到它不是最中性的词）。

#### 4) 条款编号兼容性分析（如 L3-RULES-001）

- **不建议把层级术语塞进条款编号作为主体系**：当前项目的条款编号体系是语义锚点 `[F-*]/[A-*]/[S-*]/[R-*]`，并且已在 Wish 系统规范中广泛采用（例如 `[F-WISH-FRONTMATTER-REQUIRED]` 等）。
- `L3-RULES-001` 这种“顺序号 + 层级名”的编号：
  - 优点：读者直觉上知道属于哪层。
  - 缺点：与现有稳定语义锚点体系不一致；且一旦术语从 Route 改 Path、What 改 Shape，会导致编号语义漂移（最难审计）。
- **折中建议（兼容且可判定）**：
  1. 条款仍用稳定语义锚点：例如 DocGraph L3 规范用 `[S-DOCGRAPH-…]` / `[F-DOCGRAPH-…]` 这一类。
  2. 在文档内部允许“展示型索引号”作为次要导航（Informative），例如：`(L3-001)`，但**跨文档引用只允许锚点**。
  3. 若坚持把层级信息写入 ID，建议用“层级前缀 + 语义锚点”而不是顺序号：例如 `[L3-S-DOCGRAPH-TRAVERSAL-TERMINATES]`（仍保持“内容哈希式命名”），并明确它只是别名（alias），SSOT 仍是 `[S-DOCGRAPH-TRAVERSAL-TERMINATES]`。

#### 5) 最终建议与调整建议

- **在 C vs C' 之间，我支持 C' 的方向（Route → Path）**：理由是 Route 与仓库中既有的“ROUTE（分流路由）”强冲突，且 Route 在软件语境里天然联想到 HTTP/event routing。
- **但我不建议把 L4 的 canonical 名称定为 Path（至少对 DocGraph）**：因为 DocGraph 的核心数据模型里已经有 `Path` 字段，且全仓库 path=文件路径的语义占用极高。
- **推荐的可审计落地形态**：
  1. 保留数字层级作为 SSOT：始终写 `L1/L2/L3/L4/L5`。
  2. 引入“层级别名表”（单点权威定义），例如：`L1 Why, L2 Shape (alias: What), L3 Rules, L4 Plan (alias: Path/Route), L5 Craft (alias: Build)`。
  3. 在代码注释/测试名/日志里使用带层级前缀的短标签以消歧：`[L4-PLAN]`、`[L2-SHAPE]`、`[L3-RULES]`；同时把文件路径一律写作 `FilePath`/`DocPath`。
  4. 国际化：中文展示可用“Why/Shape/Rules/Plan/Craft + 中文释义”双语并存（术语保留英文作为稳定 token，中文作为解释层）。

> 结论一句话：**避免 Route 作为 L4 的 SSOT；C' 比 C 更安全，但 L4=Path 在 DocGraph 语境仍有撞词成本，建议以“Plan/Approach”作 canonical，并把 Path 作为可接受 alias。**
---

### Moderator 场势快照 #1

**当前状态**：
1. **Seeker** 提出了三套方案，推荐方案 C：Why → Shape → Rules → Route → Craft
2. **Curator** 支持方案 C，但建议 Route → Path，形成方案 C'：Why → Shape → Rules → Path → Craft
3. **Craftsman** 支持 C' 方向，但指出 Path 在 DocGraph 语境有冲突，建议 L4 用 Plan/Approach

**关键洞察**：
1. **Route 有强冲突**：与现有 ROUTE（分流路由）过程术语冲突
2. **Path 有中强冲突**：与 DocGraph 的 Document.Path 字段冲突
3. **需要处理 SSOT 双写风险**：现有模板使用 L1(Why) → L2(What) → L3(Rules) → L4(How) → L5(Build)

**待解决问题**：
1. L4 的最终术语是什么？（Path vs Plan vs Approach vs 其他）
2. 如何处理与现有模板的兼容性？
3. 如何避免术语冲突？

**下一步**：
我想邀请 @DocOps 从文档管理和索引角度提供意见。DocOps 需要思考：
1. 如何在文档系统中管理新旧术语的过渡？
2. 如何建立权威的术语表？
3. 如何确保文档索引的一致性？

同时，我也想听听 @Implementer 从代码实现角度的看法。

### DocOps 发言

`#design`

从文档管理和索引一致性角度评估 L1-L5 层级术语方案。

---

#### 一、现有文档系统分析

##### 1.1 术语使用现状扫描

| 文件位置 | 当前术语 | 使用形式 | 影响范围 |
|:---------|:---------|:---------|:---------|
| `wishes/templates/wish-template.md` | L1 Why → L5 Build | 表格行标题 | 所有新建 Wish 的模板 |
| `wishes/README.md` | L1(Why) → L5(Build) | 核心概念说明 | 快速入门指南 |
| `wishes/specs/wish-system-rules.md` | L1-L5 | 条款引用 | L3 规范文档 |
| wish-0001-*.md | L1 Why → L5 Build | 层级进度表 | 活跃 Wish 文档 |
| wish-0002-*.md | L1 Why → L5 Build | 层级进度表 | 活跃 Wish 文档 |
| `wishes/index.md` | L1-L5 | 表头缩写 | 单体入口文件 |

**关键发现**：当前 **6 个核心文件** 已全面采用 `Why/What/Rules/How/Build` 术语体系。

##### 1.2 术语冲突风险识别

**Path 与 DocGraph 的冲突**：

根据 `atelia/docs/DocGraph/api.md` 的文档模型定义：

```
Document
├── Path: string          # 相对于 Workspace 的路径
├── Frontmatter: Dict
├── Content: string
├── OutgoingLinks: Link[]
└── IncomingLinks: Link[]
```

`Document.Path` 是 DocGraph 的**核心字段**，表示文件路径。若 L4 层级采用 "Path" 命名，将产生以下混淆：

| 场景 | 歧义示例 |
|:-----|:---------|
| 畅谈会讨论 | "这个 Path 需要调整" — 是层级？是文件路径？ |
| 条款编写 | "[L4-PATH-001]" — 容易误读为文件路径相关条款 |
| Agent 理解 | 上下文中同时出现 "Document.Path" 和 "L4 Path" |

**Craftsman 的建议值得采纳**：L4 应避开 "Path"，改用 "Plan" 或 "Approach"。

##### 1.3 SSOT 双写风险分析

| 风险类型 | 现状 | 评估 |
|:---------|:-----|:-----|
| **术语定义分散** | 术语散落在模板、README、规范文件中 | ⚠️ 高风险 |
| **无权威术语表** | 没有 `glossary.md` 或 `terms.md` 作为 SSOT | ⚠️ 高风险 |
| **模板与规范可能脱节** | 更新规范时可能遗漏模板 | ⚠️ 中风险 |
| **历史文档引用** | 畅谈会记录使用旧术语 | ⚪ 可接受（历史事实） |

---

#### 二、术语方案对比评估

| 层级 | 现有方案 | 方案 C | 方案 C' | 我的建议 |
|:-----|:---------|:-------|:--------|:---------|
| L1 | Why | Why | Why | **Why** ✅ |
| L2 | What | Shape | Shape | **Shape** ✅ |
| L3 | Rules | Rules | Rules | **Rules** ✅ |
| L4 | How | Route | Path | **Route** ✅（避免与 DocGraph 冲突） |
| L5 | Build | Craft | Craft | **Craft** ✅ |

**我的最终建议：采纳方案 C（Why → Shape → Rules → Route → Craft）**

理由：
1. **Route** 优于 **Path**：避免与 DocGraph 的 `Document.Path` 冲突
2. **Shape** 优于 **What**：更强调"塑造边界"的动作性，符合 Seeker 的概念分析
3. **Craft** 优于 **Build**：强调过程中的技艺，与 Craftsman 角色形成有趣的呼应

---

#### 三、过渡方案设计

##### 3.1 三阶段过渡策略

```
Phase 1: 兼容期（1-2 周）
  ├─ 建立权威术语表，新旧术语并列
  ├─ 新文档使用新术语
  └─ 旧文档暂不修改

Phase 2: 迁移期（1 周）
  ├─ 批量更新模板和规范文件
  ├─ 更新活跃 Wish 文档
  └─ 更新 index.md

Phase 3: 清理期（持续）
  ├─ 保留畅谈会历史记录原样（历史事实）
  ├─ 在术语表中保留映射关系
  └─ 新成员通过术语表理解对应关系
```

##### 3.2 具体文件更新清单

| 文件 | 变更内容 | 优先级 | 依赖 |
|:-----|:---------|:-------|:-----|
| `agent-team/wiki/terminology.md` | **新建**：权威术语表 | P0 | 无 |
| `wishes/templates/wish-template.md` | 表格行标题更新 | P1 | 术语表 |
| `wishes/README.md` | 核心概念说明更新 | P1 | 术语表 |
| `wishes/specs/wish-system-rules.md` | 条款引用更新 | P1 | 术语表 |
| `wishes/index.md` | 表头更新 | P2 | 模板/规范 |
| wish-0001-*.md | 层级进度表更新 | P2 | 模板 |
| wish-0002-*.md | 层级进度表更新 | P2 | 模板 |

##### 3.3 向后兼容措施

在新的权威术语表中保留映射关系，确保历史文档可追溯：

```markdown
## 术语对照表

| 层级 | 新术语 (2025-12+) | 旧术语 (历史) | 说明 |
|:-----|:------------------|:--------------|:-----|
| L1 | Why | Why | 无变化 |
| L2 | Shape | What | 强调塑造边界 |
| L3 | Rules | Rules | 无变化 |
| L4 | Route | How | 强调路径选择 |
| L5 | Craft | Build | 强调技艺过程 |
```

---

#### 四、术语表管理建议

##### 4.1 权威术语表结构

建议创建 `agent-team/wiki/terminology.md`：

```markdown
# Atelia 术语表

> **文档性质**: 权威定义 (Normative SSOT)
> **更新时间**: 2025-12-30
> **说明**: 本文件是所有术语的唯一权威来源，其他文档应引用而非复述。

## 五层级方法论术语

| 层级 | 简写 | 全称 | 核心问题 | 隐喻 | 判据类型 |
|:-----|:-----|:-----|:---------|:-----|:---------|
| L1 | Why | 价值层 | 值得做吗？ | 罗盘 | 人类价值判断 |
| L2 | Shape | 契约层 | 用户看到什么？ | 门面 | 易用性+完备性 |
| L3 | Rules | 约束层 | 什么是合法的？ | 法典 | 形式化检验 |
| L4 | Route | 策略层 | 走哪条路？ | 地图 | 量化评估 |
| L5 | Craft | 物化层 | 怎么造出来？ | 施工 | 可执行性 |

## 术语演变历史

| 版本 | 日期 | L1 | L2 | L3 | L4 | L5 | 变更原因 |
|:-----|:-----|:---|:---|:---|:---|:---|:---------|
| v0.1 | 2025-12-30 | Why | What | Rules | How | Build | 初始版本 |
| v1.0 | 2025-12-30 | Why | Shape | Rules | Route | Craft | 语义强化 |
```

##### 4.2 维护机制

| 机制 | 说明 |
|:-----|:-----|
| **SSOT 原则** | 术语定义只在术语表中维护，其他文档引用 |
| **变更日志** | 任何术语变更必须记录版本和原因 |
| **引用检查** | 定期检查引用术语表的文档是否一致 |
| **DocGraph 集成** | 未来 W-0002 完成后可自动检查术语一致性 |

---

#### 五、索引一致性保障

##### 5.1 `wishes/index.md` 更新计划

当前 `wishes/index.md` 表头：

```markdown
| WishId | 标题 | Owner | L1 | L2 | L3 | L4 | L5 | 更新日期 |
```

更新后（Phase 2）：

```markdown
| WishId | 标题 | Owner | Why | Shape | Rules | Route | Craft | 更新日期 |
```

**注意**：表头变更后需确保所有 Wish 文档的层级进度表同步更新。

##### 5.2 changefeed anchor 机制

建议在 `wishes/index.md` 中添加术语变更的 anchor：

```markdown
### 2025-12-30 – Terminology Update
- **#delta-2025-12-30-terminology-v1** – 五层级术语从 v0.1 (Why/What/Rules/How/Build) 升级到 v1.0 (Why/Shape/Rules/Route/Craft)。权威定义见 `agent-team/wiki/terminology.md`。
```

---

#### 六、最终建议汇总

| 决策点 | 我的建议 | 理由 |
|:-------|:---------|:-----|
| **L4 术语选择** | **Route** 而非 Path | 避免与 DocGraph.Document.Path 冲突 |
| **整体方案** | 采纳 **方案 C** | Why → Shape → Rules → Route → Craft |
| **术语表位置** | `agent-team/wiki/terminology.md` | 与其他 wiki 文档同级，便于引用 |
| **过渡策略** | 三阶段（兼容→迁移→清理） | 降低破坏性，确保向后兼容 |
| **文档更新顺序** | 术语表 → 模板/规范 → 活跃文档 → 索引 | 自上而下，SSOT 优先 |

**关键行动项（如果决策通过）**：

1. **P0**：创建 `agent-team/wiki/terminology.md` 权威术语表
2. **P1**：更新 `wishes/templates/wish-template.md` 和 `wishes/README.md`
3. **P1**：更新 `wishes/specs/wish-system-rules.md` 和 `wishes/index.md`
4. **P2**：更新活跃 Wish 文档（W-0001, W-0002）
5. **P3**：在 `wishes/index.md` 添加 changefeed anchor

---

**认知文件更新确认**：已将本次畅谈会洞见写入 `agent-team/members/DocOps/inbox.md`。

发言完毕。等待监护人和参谋组对术语方案的最终决策。

---

### Moderator 场势快照 #2

**当前状态**：
1. **Seeker**：推荐方案 C（Why → Shape → Rules → Route → Craft）
2. **Curator**：支持方案 C，但建议 Route → Path（方案 C'）
3. **Craftsman**：支持 C' 方向，但指出 Path 有冲突，建议 L4 用 Plan/Approach
4. **DocOps**：推荐方案 C（Route 而非 Path），并提供了完整的过渡方案

**关键进展**：
1. **Route vs Path 争议**：DocOps 指出 Path 与 DocGraph 的 Document.Path 字段冲突，建议保留 Route
2. **过渡方案**：DocOps 设计了详细的三阶段过渡策略和文件更新清单
3. **术语表**：建议创建 `agent-team/wiki/terminology.md` 作为权威 SSOT

**待决策问题**：
1. **L4 术语最终选择**：Route vs Plan vs Approach vs 其他？
2. **是否采纳方案 C**：Why → Shape → Rules → Route → Craft
3. **过渡时机**：何时开始执行过渡方案？

**当前倾向**：
- Seeker：方案 C（Route）
- Curator：方案 C'（Path）  
- Craftsman：Plan/Approach
- DocOps：方案 C（Route）

**Route 的争议点**：
- 优点：避免与 DocGraph 的 Path 字段冲突
- 缺点：与现有 ROUTE（分流路由）过程术语冲突（Craftsman 指出）

**需要监护人介入的问题**：
1. 如何权衡 Route 的两个冲突？（与 ROUTE 过程术语冲突 vs 与 DocGraph Path 字段冲突）
2. 是否有其他更好的 L4 术语候选？

**下一步**：
我想邀请 @Implementer 从代码实现角度提供最后意见，然后我们可以进行决策收敛。

### Implementer 发言

`#design`

从代码实现角度评估 L1-L5 术语方案。我将聚焦于：命名空间冲突、API 设计影响、测试代码影响、以及"代码友好性"。

---

#### 一、代码实现影响分析

##### 1.1 术语在代码中的使用场景

| 使用场景 | 示例 | 术语要求 |
|:---------|:-----|:---------|
| **类名/接口名** | `IShapeDocument`, `RulesValidator` | 可作为合法标识符后缀 |
| **枚举值** | `DocumentLayer.Why`, `DocumentLayer.Route` | 单词简洁、无特殊字符 |
| **命名空间** | `Atelia.DocGraph.Shape`, `Atelia.DocGraph.Rules` | 避免与系统命名空间冲突 |
| **方法名** | `ValidateRules()`, `BuildCraft()` | 动词+名词组合自然 |
| **测试类名** | `ShapeDocumentTests`, `RulesValidatorTests` | 后缀兼容性好 |
| **条款标签（注释）** | `// [L3-RULES]`, `// [L4-ROUTE]` | 简短、无歧义 |

##### 1.2 各候选术语的代码友好性评估

| 术语 | 作为类名后缀 | 作为枚举值 | 作为命名空间 | 综合代码友好性 |
|:-----|:-------------|:-----------|:-------------|:---------------|
| **Why** | ⭐⭐ (`WhyDocument`略奇怪) | ⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐ |
| **Shape** | ⭐⭐⭐⭐⭐ (`ShapeSpec`) | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Rules** | ⭐⭐⭐⭐⭐ (`RulesValidator`) | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Route** | ⭐⭐⭐⭐ (`RouteStrategy`) | ⭐⭐⭐⭐ | ⭐⭐ (与网络路由混淆) | ⭐⭐⭐ |
| **Path** | ⭐⭐ (与 `System.IO.Path` 强冲突) | ⭐⭐⭐ | ⭐ (与 IO 冲突严重) | ⭐⭐ |
| **Plan** | ⭐⭐⭐⭐⭐ (`PlanDocument`) | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| **Approach** | ⭐⭐⭐⭐ (`ApproachSpec`) | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐ |
| **Craft** | ⭐⭐⭐⭐⭐ (`CraftBuilder`) | ⭐⭐⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |

---

#### 二、命名空间冲突检查

##### 2.1 现有代码库扫描结果

我扫描了 `atelia/src/**/*.cs` 中的术语使用：

| 术语 | 现有代码中出现次数 | 使用语境 | 冲突风险 |
|:-----|:-------------------|:---------|:---------|
| `Path` | 4 次 | 全部是 `System.IO.Path`（文件路径操作） | **高** ⚠️ |
| `Route` | 0 次 | 无直接使用 | **低** |
| `Plan` | 1 次 | 注释中"Plan A"（策略描述） | **极低** |

##### 2.2 `System.IO.Path` 冲突详解

在 C# 中，`Path` 是一个极高频的系统类型：

```csharp
// 现有代码中的典型使用
Path.Combine(cwd, ".atelia", folderName);
Path.Combine(_logDir, $"{category.ToLowerInvariant()}.log");
```

如果 L4 层级命名为 `Path`，将产生严重的命名空间冲突：

```csharp
// 歧义代码示例
namespace Atelia.DocGraph.Path  // L4 层级命名空间
{
    public class PathDocument    // 与 System.IO.Path 冲突
    {
        public string Path { get; set; }  // Document.Path 字段
    }
}

// 使用时的混乱
using System.IO;
using Atelia.DocGraph.Path;  // 编译器警告或错误

var filePath = Path.Combine(dir, file);  // 哪个 Path？
```

**结论**：`Path` 作为 L4 术语在 C# 代码中有**严重的命名空间冲突**，不建议采用。

##### 2.3 `Route` 冲突分析

`Route` 在代码库中无直接使用，但在文档中有 `CLASSIFY → ROUTE → APPLY` 过程术语。

**代码层面影响**：
- 无 C# 命名空间冲突
- 无现有类型/变量名冲突
- ASP.NET Core 的 `[Route]` 属性不在当前项目依赖中

**文档层面影响**：
- 与记忆维护流程的 `ROUTE` 步骤同形异义
- 跨文档讨论时需要语境消歧

**评估**：`Route` 在代码实现层面冲突较低，但在文档层面有中等冲突。

---

#### 三、API 设计影响

##### 3.1 DocGraph API 中的 `Path` 字段

根据 [api.md](atelia/docs/DocGraph/api.md) 的文档模型定义：

```csharp
public record Document(
    string Path,              // ← 核心字段！
    IDictionary<string, object> Frontmatter,
    string Content,
    IReadOnlyList<Link> OutgoingLinks,
    IReadOnlyList<Link> IncomingLinks
);

public record Link(
    string SourcePath,        // ← Path 无处不在
    string TargetPath,
    string RawTarget,
    int LineNumber,
    LinkType Type
);

public record MissingBacklink(
    string SourcePath,
    string TargetPath,
    string ExpectedField,
    string SuggestedValue
);
```

如果 L4 也叫 `Path`，API 设计将面临命名混乱：

```csharp
// 混乱的 API 示例
public interface IDocGraphService
{
    // Document.Path 字段
    Document GetDocumentByPath(string path);
    
    // L4 Path 层级
    PathDocument GetPathDocument(string documentPath);  // 两个 Path！
}
```

##### 3.2 推荐的 API 命名模式

如果采用 `Plan` 作为 L4 术语，API 设计更清晰：

```csharp
public enum DocumentLayer
{
    Why,      // L1
    Shape,    // L2
    Rules,    // L3
    Plan,     // L4 ← 无歧义
    Craft     // L5
}

public interface IDocGraphService
{
    Document GetDocumentByPath(string path);           // Path = 文件路径
    PlanDocument GetPlanDocument(WishId wishId);       // Plan = L4 层级
}
```

---

#### 四、测试代码影响

##### 4.1 测试类命名模式

测试类命名通常遵循 `{被测类名}Tests` 模式：

| L4 术语 | 测试类名示例 | 可读性 |
|:--------|:-------------|:-------|
| Route | `RouteDocumentTests`, `RouteValidatorTests` | ⭐⭐⭐⭐ |
| Path | `PathDocumentTests` | ⭐⭐ (与 IO 测试混淆) |
| Plan | `PlanDocumentTests`, `PlanValidatorTests` | ⭐⭐⭐⭐⭐ |
| Approach | `ApproachDocumentTests` | ⭐⭐⭐⭐ |

##### 4.2 测试方法命名模式

测试方法命名通常遵循 `{Method}_{Scenario}_{ExpectedResult}` 模式：

```csharp
// Route 术语
[Fact]
public void ValidateRoute_WithValidStrategy_ReturnsSuccess() { }

// Path 术语 - 易混淆！
[Fact]
public void ValidatePath_WithValidFilePath_ReturnsSuccess() { }  // 哪个 Path？

// Plan 术语 - 清晰
[Fact]
public void ValidatePlan_WithValidStrategy_ReturnsSuccess() { }
```

---

#### 五、我的最终建议

##### 5.1 术语选择排名（从代码实现角度）

| 排名 | L4 术语 | 理由 |
|:-----|:--------|:-----|
| 1️⃣ | **Plan** | 零命名空间冲突、与 DocGraph Path 字段完全分离、测试命名清晰 |
| 2️⃣ | **Approach** | 无冲突但稍长，不如 Plan 简洁 |
| 3️⃣ | **Route** | 代码层面无冲突，但文档层面与 ROUTE 步骤同形异义 |
| 4️⃣ | **Path** | **不推荐** — 与 System.IO.Path 和 DocGraph.Document.Path 双重冲突 |

##### 5.2 整体方案建议

| 层级 | 我的建议 | 代码示例 |
|:-----|:---------|:---------|
| L1 | **Why** | `enum DocumentLayer { Why, ... }` |
| L2 | **Shape** | `class ShapeDocument`, `namespace Atelia.DocGraph.Shape` |
| L3 | **Rules** | `class RulesValidator`, `// [L3-RULES]` |
| L4 | **Plan** | `class PlanDocument`, `// [L4-PLAN]` |
| L5 | **Craft** | `class CraftBuilder`, `namespace Atelia.DocGraph.Craft` |

**最终推荐方案**：**Why → Shape → Rules → Plan → Craft**

##### 5.3 代码层面的调整建议

1. **避免 Path 作为层级术语**：与 `System.IO.Path` 冲突是硬伤，无法通过命名规范消解
2. **Route 可接受但非最优**：如果团队倾向 Route，建议在代码中使用 `L4Route` 或 `LayerRoute` 前缀消歧
3. **Plan 是代码最友好选项**：单音节、无冲突、与 Path 字段完全正交
4. **条款标签采用带层级前缀**：`[L4-PLAN]` 而非单独的 `[PLAN]`，与现有 `[F-*]/[A-*]/[S-*]/[R-*]` 体系兼容

##### 5.4 如果采用 Route（备选方案）

如果团队决定采用 Route（方案 C），建议以下代码规范：

```csharp
// 命名规范：LayerRoute 前缀
public class LayerRouteDocument { }

// 注释标签：带层级前缀
// [L4-ROUTE] 技术选型说明

// 命名空间：明确层级
namespace Atelia.DocGraph.LayerRoute { }
```

---

#### 六、与各方案的兼容性评估

| 方案 | L4 术语 | 代码冲突 | 文档冲突 | 综合评估 |
|:-----|:--------|:---------|:---------|:---------|
| **方案 C** | Route | ⚪ 低 | ⚠️ 中（ROUTE 步骤） | ⭐⭐⭐ 可接受 |
| **方案 C'** | Path | ⛔ **高** | ⛔ **高**（Document.Path） | ⭐ 不推荐 |
| **Craftsman 建议** | Plan | ⚪ 极低 | ⚪ 极低 | ⭐⭐⭐⭐⭐ 最优 |

---

#### 七、总结

**我的立场**：

1. **支持 Craftsman 的建议**：L4 使用 **Plan** 而非 Route 或 Path
2. **整体方案**：**Why → Shape → Rules → Plan → Craft**
3. **核心理由**：
   - `Path` 与 `System.IO.Path` 有不可调和的命名空间冲突
   - `Path` 与 `DocGraph.Document.Path` 字段有语义冲突
   - `Plan` 零冲突、代码友好、测试命名清晰
   - `Route` 可接受但在文档层面有同形异义风险

**如果必须在 Route vs Path 中二选一**：选 Route。代码层面的命名空间冲突比文档层面的同形异义更难处理。

---

发言完毕。

---

### Moderator 决策收敛

`#decision`

经过全体成员的深入讨论，我现在进行决策收敛。

#### 一、各成员立场总结

| 成员 | 推荐方案 | L4 术语 | 核心理由 |
|:-----|:---------|:--------|:---------|
| **Seeker** | 方案 C | Route | 本质分析支持，Route 强调"路径选择" |
| **Curator** | 方案 C' | Path | UX/DX 角度，Path 比 Route 更日常 |
| **Craftsman** | 改良方案 | Plan/Approach | 规范审计，Path 有冲突，Route 有同形异义 |
| **DocOps** | 方案 C | Route | 文档管理，Path 与 DocGraph.Document.Path 冲突 |
| **Implementer** | 改良方案 | **Plan** | 代码实现，Path 有命名空间冲突，Plan 最优 |

#### 二、关键发现

1. **Path 有双重冲突**：
   - 与 `System.IO.Path` 命名空间冲突（代码层面）
   - 与 `DocGraph.Document.Path` 字段冲突（语义层面）

2. **Route 有同形异义**：
   - 与现有 `CLASSIFY → ROUTE → APPLY` 过程术语冲突
   - 但代码层面无冲突

3. **Plan 是最优解**：
   - 零命名空间冲突
   - 零语义冲突
   - 代码友好，测试命名清晰

#### 三、最终决策

基于全体成员的讨论，我做出以下决策：

**决策 1：采纳改良方案**
- L1: **Why**（保持）
- L2: **Shape**（替代 What，强调边界塑造）
- L3: **Rules**（保持）
- L4: **Plan**（替代 How/Route/Path，强调策略规划）
- L5: **Craft**（替代 Build，强调技艺过程）

**完整方案**：**Why → Shape → Rules → Plan → Craft**

**决策 2：采纳 DocOps 的过渡方案**
- 三阶段过渡：兼容期 → 迁移期 → 清理期
- 创建权威术语表：`agent-team/wiki/terminology.md`
- 按优先级更新相关文档

**决策 3：术语使用规范**
1. 保留数字层级作为 SSOT：始终可写 `L1/L2/L3/L4/L5`
2. 在代码注释中使用带层级前缀：`[L4-PLAN]`
3. 在文档标题中可使用语义术语：`DocGraph-Plan.md`

#### 四、决策理由

1. **Plan 解决了所有冲突**：
   - 无 `System.IO.Path` 命名空间冲突
   - 无 `DocGraph.Document.Path` 字段冲突
   - 无 `ROUTE` 过程术语同形异义

2. **Plan 语义准确**：
   - L4 本质是"策略选择、技术路线"
   - "Plan" 准确反映"规划、方案"的含义
   - 比 "Route" 更中性，比 "Path" 更明确

3. **代码实现友好**：
   - `PlanDocument`, `PlanValidator` 等类名自然
   - 测试命名清晰：`PlanDocumentTests`
   - 枚举值：`DocumentLayer.Plan`

#### 五、行动项

**P0（立即执行）**：
1. 创建权威术语表：`agent-team/wiki/terminology.md`
2. 更新本次畅谈会记录，记录决策

**P1（1-2天内）**：
1. 更新 `wishes/templates/wish-template.md`
2. 更新 `wishes/README.md`
3. 更新 `wishes/specs/wish-system-rules.md`

**P2（1周内）**：
1. 更新 `wishes/index.md` 表头
2. 更新活跃 Wish 文档（W-0001, W-0002）
3. 在 `wishes/index.md` 添加 changefeed anchor

**P3（持续）**：
1. 新文档使用新术语
2. 定期检查术语一致性

#### 六、随缘目标达成情况

**审阅两份草稿的发现**：
1. `atelia/docs/DocGraph/api.md`：
   - 文档模型中的 `Document.Path` 字段与 L4 术语冲突（已解决）
   - API 设计整体良好，可作为 L2 参考

2. `atelia/docs/DocGraph/ore.md`：
   - 优秀的"设计原矿"，汇总了现有材料
   - 第3节 MVP 规范性约束很有价值
   - 可作为 L3 规范的基础

**建议的修订**：
1. 在 `api.md` 中更新术语引用
2. 在 `ore.md` 中记录本次术语决策
3. 两者都引用新的权威术语表

#### 七、会议总结

**成果**：
1. ✅ **必要目标达成**：为 L1-L5 赋予了语义化名字
2. ✅ **随缘目标部分达成**：审阅了草稿，发现了重要冲突
3. ✅ **达成共识**：全体成员参与，形成了可执行的决策

**关键洞见**：
1. 术语设计需要多维度评估：本质、UX/DX、规范、代码实现、文档管理
2. 冲突分析至关重要：Path 的双重冲突是本次讨论的关键发现
3. Plan 作为 L4 术语是最优平衡：解决了所有冲突，语义准确

**后续工作**：
1. 执行过渡方案
2. 监控术语使用一致性
3. 未来可通过 DocGraph 工具自动检查术语一致性

---

### Moderator 结束语

本次畅谈会成功完成了预定目标。感谢所有成员的深度参与和专业贡献：

- **Seeker** 的本质追问奠定了理论基础
- **Curator** 的 UX/DX 分析确保了术语可用性  
- **Craftsman** 的规范审计发现了关键冲突
- **DocOps** 的过渡方案提供了执行路径
- **Implementer** 的代码分析找到了最优解

特别感谢 **监护人** 提供参考材料和支持。

**会议正式结束**。决策已记录，行动项已明确。让我们开始执行过渡方案，让新的术语体系为团队服务。

---
**会议状态**: ✅ Completed
**决策记录**: 见上文
**行动项**: 见 P0-P3
**后续会议**: 术语过渡完成后的回顾会议（建议 1-2 周后）