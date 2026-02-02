# DocOps 认知索引

> 最后更新: 2026-02-02 便签归档 (1条: RBF Stage 11 文档一致性检查)

## 我是谁

DocOps - 文档与索引管理专家，负责维护团队的集体记忆和认知连续性。

**核心职责**：
- Consistency Gatekeeper：维护文档叙述一致性
- Index Steward：管理 Changefeed Anchor 系统
- Doc Gardener：控制文档体积，防止信息熵增

---

## 核心洞见（Insight）

> 按主题组织，演进关系以 `→` 标注

### 接口语义匹配→实现简化模式（2026-01-11）

**发现场景**：RBF 适配器文档 §5 重构（80行→25行，-68%）

**核心洞见**：推式接口（caller 持有数据，callee 只做转发）与 `RandomAccess.Write` 语义完美匹配——`IBufferWriter<byte>` → `IByteSink` 的改变消除了中间 buffer 需求。

**模式抽象**：
- 当接口语义与底层 API 语义匹配时，适配器可极度简化
- "拉式→推式"转换往往带来代码量数量级下降
- 条款数量也减少（4 删 3 增），说明概念复杂度同步降低

---

### 文档职能分离方法论（2026-01-11）

**发现场景**：RBF 接口文档与 Type Bone 文档的职能划分重构

**可观察性边界原则**：
| 可见性 | 归属文档 | 示例 |
|:-------|:---------|:-----|
| 上层能观察到的 | 契约文档 (interface) | Auto-Abort 语义 |
| 内部实现路径 | 实现文档 (type-bone) | 双路径物理实现 |

**约束分级模式**：
| 约束类型 | 表达位置 | 变更影响 |
|:---------|:---------|:---------|
| Public Constraint | 契约文档 | 破坏性变更 |
| Observable Consequence | 契约文档 | 行为变更 |
| Implementation Constraint | 实现文档 | 内部重构 |

**"门面 + 实现层"文档分层模式**（补充自审查发现）：
- interface.md：对外门面，定义 Shape-Tier 契约
- type-bone.md：内部实现，定义 Craft-Tier 骨架
- 两者通过条款 ID 引用形成单向依赖（type-bone → interface）

**SSOT 漂移清理实践经验（2026-01-12）**：
- Implementation Guide 类文档在接口修订后容易遗漏同步更新
- 条款 ID 重命名时，跨层引用点（interface → type-bone）容易被忽略
- **职责闭合检查有价值**：审查时检查各层 API 参数是否对齐，可发现漏参问题

**Breaking Change 后四层检查框架（2026-01-24，2026-02-02 增补）**：
| 层级 | 检查点 | 示例 |
|:-----|:-------|:-----|
| 1. 废弃术语清理 | grep 旧术语，确认活跃文档已清除 | `FrameStatusBytes` 等 |
| 2. 接口文档对齐 | format.md → interface.md | 字段名、类型变更 |
| 3. 实现指南对齐 | interface.md → type-bone.md | API 签名、描述 |
| 4. 测试向量对齐 | 格式版本 → 测试向量版本 | v0.40 同步 |

**核心洞见**：Breaking Change 检查要区分"文档层面完成"与"代码实现跟进"——文档可以先行，但需明确标注实现差距。

**补充（2026-02-02）**：文档检查不仅要看签名定义，还要检查：
- **示例代码**：旧 API 模式（如直接赋值）需适配新 Result-Pattern（如 `var result = ...` + 失败检查）
- **描述性文字**：规范条款中的旧术语残留（如 `Commit` → `EndAppend`）

### 术语迁移方法论（成熟）

**四层影响模型**（可复用于所有术语迁移）：
| 影响层级 | 迁移策略 | 典型文件 |
|:---------|:---------|:---------|
| SSOT 层 | MUST 立即迁移 | 术语表、registry.yaml |
| 规范层 | MUST 1周内 | 规范条款文档 |
| 活跃层 | SHOULD 1月内 | 当前活跃设计文档 |
| 历史层 | MAY 保持原样 | 归档/会议记录（历史不可改） |

**D-S-D 模型实战验证（2026-01-09）**：
| 层级 | 文件数 | 迁移策略 |
|:-----|:------|:---------|
| SSOT 层 | 1（spec-conventions.md） | MUST 立即迁移 |
| 历史层 | 4（Handoffs） | MAY 保持原样 |

**洞见**：真正需要迁移的只有 SSOT + 规范层（~15-20 文件），历史层 14 处旧术语属于历史事实

**核心教训**：
1. 真正需要强制迁移的只有 SSOT + 规范层（~15-20 文件）
2. 术语更新必须扫描实例文档，不仅是模板和规范
3. `grep -i` 会误报，需排除反例/代码域/路径域
4. **语义过载风险**：同一术语在多域使用时谨慎重命名

**验收检查清单**：
- [ ] grep 旧术语，确认影响范围
- [ ] 更新 SSOT 层 → 规范层 → 活跃层
- [ ] grep 验证旧术语清零 + 新术语正确应用

**跨文档术语对齐检查清单（2026-01-09）**：
- [ ] modifier 名称一致
- [ ] 新术语有定义
- [ ] 引用方向语义清晰

**Canonical Source 术语选型（2026-01-09）**：
| 候选 | 优势 | 劣势 |
|:-----|:-----|:-----|
| Canonical Location | 明确物理位置 | 与路径概念混淆 |
| Canonical Artifact | 强调产物概念 | 与构建产物混淆 |
| **Canonical Source** ✓ | 与 SSOT 呼应，工具输出语义匹配 | — |

**洞见**：`Anchor` 仅指 Markdown 技术机制（`#heading-id`），需与 `Canonical Source`（语义层载体）区分

**"声明性引用"概念（2026-01-09）**：
- Decision→Spec 引用不是"因果依赖"，而是"宣告关系"
- Normative 是"要不要改"的判定，SSOT 是"在哪改"的定位——分离便于工具错误信息精确化

### 三层 SSOT 架构（成熟）

| 层次 | 文件 | 职责 | 变更频率 |
|:-----|:-----|:-----|:---------|
| Layer 1 | artifact-tiers.md | 概念语义定义 | 低 |
| Layer 2 | spec-conventions.md | 写法规范 | 中 |
| Layer 3 | terminology-registry.yaml | 机器可读约束 | 中-高 |

**价值**：变更隔离——改写法不影响语义，改白名单不需重写规则。

### 文档类型拓扑（成熟）

| 文档类型 | 回答问题 | 维护特点 |
|:---------|:---------|:---------|
| Wiki | "是什么" | 知识定义，相对稳定 |
| Spec | "必须什么" | 规范约束，版本演进 |
| Meeting | "发生了什么" | 历史事实，只追加 |
| **Recipe** | **"怎么做"** | 知识→行动桥梁，持续完善 |
| **Beacon** | **"为什么重要"** | 叙事层，与规范互补 |

**Recipe 特有洞见**：
- 流程步骤编号（Step 1）保留，条款编号（§3.2）移除
- 三层发现机制：AGENTS.md 入口 → index.md 导航 → 具体 Recipe

### 单体文档拆分模式（成熟）

当文档从"术语表"演变为"理论框架"时，采用 **核心 + 卫星** 模式：

| 层级 | Token 预算 | 用途 |
|:-----|:-----------|:-----|
| 30 秒版 | ~100 | index.md 头部速览 |
| 5 分钟版 | ~300 | 完整 index + 着陆页 |
| 深度版 | ~800 | core-definitions + 详情 |

**迁移策略**：先创建新结构 → 旧文件改 Redirect Stub → 更新引用 → 移除

### 双向链接例外（专属 vs 共享产物）

| 产物类型 | 示例 | ParentWish 要求 |
|:---------|:-----|:----------------|
| **专属产物** | L3 条款文件 | MUST — 单一 Wish 绑定 |
| **共享产物** | 畅谈会记录 | N/A — 可被多 Wish 引用 |

### 分层隔离的价值（实操经验）

- 接口层 API 变更不影响 Layer 0 格式规范/测试向量
- 版本引用必须同步，但内容本身"只有可观测行为变化时才改"
- 行号是脆弱的元数据——文档重构后需重新生成条款索引

### 术语迁移守护机制（2026-01-07）

**源自 SizedPtr 迁移路障事件分析**

**核心问题**：迁移完成后存在"归属权真空"——没有人负责守护迁移结果。

**根因分析**：
- 术语迁移不在常规扫描范围，导致回退未被及时发现
- 提交信息语义模糊（如"RBF文档修订"）掩盖了实际是反向迁移
- 缺乏迁移锁定机制，无法防止无意回退

**新职责扩展**（待确认归属）：
- 将"术语迁移锁定验证"加入日常扫描
- 建立 `agent-team/indexes/term-migrations.yaml` 追踪迁移状态
- 标准流程草案：`how-to/lock-term-migrations.md`

### 关键设计决策锁定模式（2026-01-07）

**发现场景**：审阅 RBF 接口文档时总结的可复用模式

**"AI不可修改的关键决策"块**：
| 属性 | 要求 |
|:-----|:-----|
| 格式 | 表格（决策ID \| 内容 \| 理由 \| 确认日期） |
| 位置 | 文档概述之后、术语表之前 |
| 关键 | 必须引用源文件作为依赖锚点 |
| 用途 | 防止 AI Agent 在后续编辑中无意回退设计决策 |

**文档化技巧**：
- 技术属性 + 语义角色并重（如 SizedPtr 的"写→读凭证"隐喻）
- 引用外部决策矩阵作为理由锚点（如 `AteliaResult/guide.md#1-决策矩阵`）

**实践案例（2026-01-12）**：spec-conventions.md 术语迁移锁定——在文档头部添加迁移锁定块，锁定 Clause-ID 术语，防止回退到旧术语（语义锚点/REQID 等）。这是小黑板"三层防回退守卫"中 Migration Lock Annotation 层的实践。

**待形成 Recipe**：`how-to/lock-key-decisions.md`

### 条款格式 DSL 化演进路线（2026-01-07 → 2026-01-09 增补）

**现状评估**：
- 条款格式 `[F-NAME]`/`[A-NAME]` 等已有形式化定义（spec-conventions.md §2）
- 但仅是"命名约定"，工具层面无法区分**定义**与**引用**

**核心挑战（文档运维视角）**：
| 问题 | 影响 |
|:-----|:-----|
| 定义/引用混淆 | 无法确定哪个是 SSOT |
| 跨文档依赖不可见 | 引用关系无机器可读表示 |
| 术语迁移风险 | 改名/废弃时引用点无法自动发现 |
| 层级验证缺失 | Decision→Spec→Derived 方向约束无法校验 |

**与 DocGraph 集成切入点**：
- frontmatter 解析能力 → 可扩展支持条款定义声明
- Visitor 模式 → 可新增 `ClauseIndexVisitor`
- produce/produce_by → 可扩展为 defines/references

**渐进部署四阶段**：
1. Phase 0：建立条款定义 frontmatter 约定（无工具变更）
2. Phase 1：`ClauseValidator` 检查定义唯一性、引用合法性
3. Phase 2：`ClauseIndexVisitor` 生成 `clauses.gen.md`
4. Phase 3：正式 DSL 语法 + 依赖图可视化

**AI-Design-DSL 试点评估（2026-01-09）**：
- Phase 0 试点目标：`rbf-decisions.md`（已有"AI不可修改"标注，条款数量适中）
- **DSL 核心价值不在格式化，在条款间引用关系的形式化**——这是防止迁移回退的关键
- `decision` modifier = 形式化的 Migration Lock，可替代自然语言警告
- **DSL 化是"人工约定→机器可验证"的跃迁**

**DSL vs conventions 职责切分**：
| 文件 | 回答问题 | SSOT 内容 |
|:-----|:---------|:----------|
| AI-Design-DSL.md | 机器怎么解析 | 语法结构 |
| spec-conventions.md | 人怎么写 | 表示选择 |

**最小修订策略**：conventions 只需新增 ~15 行引用 DSL 定义，而非双写

### 引用密度控制模式（2026-01-07）

**发现场景**：审阅 AI-Design-DSL @引用语法

**核心洞见**：引用密度控制 = 信噪比管理

**双层引用模式**：
| 层级 | 语法 | 语义 | 索引行为 |
|:-----|:-----|:-----|:---------|
| **强引用** | `@\`Term\`` / `@[CLAUSE]` | 逻辑依赖，必须追踪 | 进入依赖图 |
| **弱引用** | 纯 code span（无 `@`） | 仅为可读性 | 不进入依赖图 |

**判断标准**：删除被引用项是否导致本段落失效
- 强引用：是 → 必须追踪
- 弱引用：否 → 仅便于查阅

**写作指南要点**：
1. 定义优先：每个 term/clause 有且仅有一个定义位置
2. 强引用节制：每段落不超过 3 个 `@` 引用
3. 弱引用自由：无 `@` 的 code span 不影响图谱

**结构拆分建议**（Guide vs Spec）：
```
SoftwareDesignModeling/
├── AI-Design-DSL.md          # 速览
├── AI-Design-DSL-spec.md     # 形式化规范
└── AI-Design-DSL-guide.md    # 写作指南
```

### 机读优先设计妥协模式（2026-01-09）

**发现场景**：审阅 AI-Design-DSL Summary-Node 定义

**核心洞见**：`Depth=1` 约束是**机读优先的设计妥协**——牺牲写作灵活性换取解析简单性。

**四问评估框架**（可复用于评估形式化约束）：
| 问题 | 评估维度 |
|:-----|:---------|
| 自动聚合是否容易 | 工具实现复杂度 |
| 对作者是否繁琐 | 写作灵活性损失 |
| 对 TOC 的影响 | 文档结构副作用 |
| 命名是否合适 | 领域术语匹配度 |

**建议**：`# summary` 会污染顶级 TOC，建议用 `## summary` 或移至文档末尾作为"附录"。

### 认知文件增量维护方法论（2026-01-03）

**核心洞见**：维护的核心是"合并演进关系"——把 A→B→C 的探索过程压缩为最终结论 C，并保留 A/B 的历史指针。

**v2.5.0 规范验证**：
- ✅ "活性知识三问"——帮助判断哪些洞见应归档
- ✅ "演进关系处理"——多条术语迁移洞见合并为可复用模型
- ✅ "非标准格式降级路径"——层级标题替代 `[I-###]` 标记同样适用

---

## 我关注的项目

| 项目 | 状态 | 最近活动 |
|:-----|:-----|:---------|
| StateJournal | 活跃 | 2025-12-29 文档原子化重构 Phase 1 |
| PipeMux | 完成 | 2025-12-09 管理命令文档更新 |
| PieceTreeSharp | 待跟进 | — |
| DocUI | 待跟进 | — |
| atelia-copilot-chat | 待跟进 | — |

---

## 最近工作

### 2026-02-02 - RBF Stage 11 文档一致性检查

**场景**：Stage 11 将 `RbfFrameBuilder.EndAppend` 返回类型改为 `AteliaResult<SizedPtr>` 后的文档同步

**发现并修复**：
- rbf-guide.md：示例代码适配 Result-Pattern
- rbf-interface.md：两处旧术语 `Commit` → `EndAppend`（条款 @[S-RBF-TAILOFFSET-UPDATE] 和 @[S-RBF-BUILDER-SINGLE-OPEN]）

**验证通过**：签名定义、Result-Pattern 条款、描述性文字全部对齐

### 2026-01-24 - RBF 文档健康检查 (v0.40 Breaking Change)

**场景**：rbf-format.md v0.40 引入重大变更后的跨文档健康检查

**关键发现**：
- ✅ 文档层面 SSOT 同步已基本完成（废弃术语清理、接口文档对齐）
- ⚠️ `RbfLayout.cs` 实现仍基于旧的 `FrameStatus` 布局，未适配新的 `TrailerCodeword` + `FrameDescriptor` 结构
- **设计文档先行更新**，代码实现尚未跟进

**待办优先级**：
| 优先级 | 项目 | 状态 |
|:-------|:-----|:-----|
| P0-Code | RbfLayout.cs 适配新 TrailerCodeword 布局 | 待 Implementer |
| P1-Doc | rbf-test-vectors.md 版本对齐 | 待更新 |
| P2-Code | RbfLayout.cs 废弃条款注释清理 | 待更新 |

**方法论**：Breaking Change 后执行四层检查（废弃术语清理 → 接口文档对齐 → 实现指南对齐 → 测试向量对齐）

### 2026-01-17 - SizedPtr/AteliaResult/RBF Stage 05 文档同步

**SizedPtr 设计文档** (commit bc5bd8b)：
- API 名称更新：`OffsetBytes` → `Offset`，`LengthBytes` → `Length`
- 类型变更：`Offset: ulong → long`，`Length: uint → int`（.NET I/O API 一致性）
- 新增 DL-004（类型简化决策）

**AteliaResult 文档** (commit 46aa891)：
- 扩展为类型家族：AteliaResult / AsyncAteliaResult / DisposableAteliaResult / IAteliaResult
- guide.md 新增 §4 "带资源所有权的结果" + ToDisposable() 使用示例
- specification.md 新增 §3.3-3.5（IAteliaResult 契约、Dispose 语义）
- 命名变更同步：`AteliaAsyncResult` → `AsyncAteliaResult`

**RBF Stage 05 文档** (rbf-interface.md v0.30 / rbf-type-bone.md v0.5)：
- ReadFrame API 重构：单参数签名 → 双变体（buffer 外置 + pooled）
- 新增 `IRbfFrame` 接口和 `RbfPooledFrame` 类型
- `RbfFrame.Ptr` → `Ticket`（语义凭据）
- `RbfRawOps` 拆分为 `RbfReadImpl` / `RbfWriteImpl`
- **职责闭合检查**验证四个类型定义完全对齐

### 2026-01-12 - RBF SSOT 漂移清理

**场景**：RBF 文档 v0.27/v0.28 接口修订后遗留的 SSOT 漂移

**清理内容**：
- Builder 生命周期命名：`Commit` → `EndAppend`（3 处）
- Tombstone 条款引用：`@[S-RBF-TOMBSTONE-VISIBLE]` → `@[S-RBF-SCANREVERSE-TOMBSTONE-FILTER]`（1 处）
- ScanReverse 过滤职责闭合：RawOps 签名补充 `showTombstone` 参数（1 处）

**产出**：[清理报告](agent-team/handoffs/2026-01-12-rbf-ssot-drift-cleanup.md)

### 2026-01-12 - 术语统一迁移锁定标注

**完成项**：
- spec-conventions.md 头部添加迁移锁定块，锁定 Clause-ID 术语
- 禁止回退到旧术语（语义锚点/REQID 等）

**关联**：小黑板"三层防回退守卫"的 Migration Lock Annotation 层实践案例

### 2026-01-11 - RBF 文档系列重构

**文档职能分离**：
- rbf-interface.md：`[S-RBF-BUILDER-AUTO-ABORT]` 拆分为 `[S-RBF-BUILDER-DISPOSE-ABORTS-UNCOMMITTED-FRAME]`（仅逻辑语义）
- rbf-type-bone.md：新增 `[I-RBF-BUILDER-AUTO-ABORT-IMPL]`（物理实现双路径）、引用化 4 个公开类型、新增"快速导航"区块
- 行数变化：interface.md 372 行（-20）；type-bone.md 178 行（-80 重复，+35 引用）

**适配器文档简化** (rbf-type-bone.md §5)：
- 核心变化：`IBufferWriter<byte>` → `IByteSink`（拉式→推式）
- 代码量：80行 → 25行（-68%）
- 条款变化：删除 4 个 spec，新增 3 个 spec，保留 `@[I-RBF-SEQWRITER-HEADLEN-GUARD]`

**关联产出**：Curator 职能划分方案实施、对齐审查 FixList

### 2026-01-09 - D-S-D 术语对齐重构

**完成项**：
- AI-Design-DSL.md 术语对齐：`Design-Clause`→`Spec-Clause`，`Hint-Clause`→`Derived-Clause`，modifier `design`→`spec`，`hint`→`derived`
- spec-conventions.md 术语对齐：`SSOT-Layer`→`Spec-Layer`，条款 ID 更新
- spec-conventions.md 一致性修复：modifier 残留、Canonical Source 定义补充、引用语义澄清

**验收**：grep 确认旧术语已全部清除

### 2026-01-06 - AteliaResult 双类型设计文档创建

**完成项**：
- 创建 `/repos/focus/atelia/docs/Primitives/AteliaResult.md` v0.1
- 双类型架构设计规范（AteliaResult + AsyncAteliaResult）

**文档结构**：概述 → 类型定义 → API 契约 → 使用示例 → 设计理由

**关联**：
- 前置规范：`AteliaResult-Specification.md`（错误协议）
- 命名决策来源：TeamLeader inbox 便签（2026-01-06 15:00）
- 新建 `Primitives/` 目录，首个文件

---

### 2026-01-03 - 认知文件增量维护

**维护内容**：
- 归档 2025-12 工作日志 → `archive/members/docops/2025-12-work-log.md`
- 合并演进关系洞见（术语迁移方法论 A→B→C 整合）
- 压缩重复内容，提炼为可复用模式

**维护前后对比**：
| 指标 | 前 | 后 |
|:-----|:---|:---|
| 行数 | 678 | ~350 |
| 洞见组织 | 按时间堆积 | 按主题归类 |
| 工作日志 | 内联 | 归档 |

### 2026-01-02 - Recipe 条款编号清理 & Resolve-Tier Phase 1

**完成项**：
- 21 个 Recipe 文件核查，7 个文件移除条款编号
- Resolve-Tier SSOT 原子更新（spec-conventions.md、terminology-registry.yaml）

---

## 认知文件导航

| 文件 | 用途 |
|:-----|:-----|
| `index.md` | 本文件，认知入口 |
| `inbox.md` | 便签收集箱 |
| `meta-cognition.md` | 元认知反思（如有） |
| `archive/members/docops/` | 历史工作日志归档 |

