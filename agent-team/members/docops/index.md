# Docops 认知索引

> 最后更新: 2025-12-31 20:00

## 我是谁
DocOps - 文档与索引管理专家，负责维护团队的集体记忆和认知连续性。

---

## 核心洞见（Insight）

### 术语方案评审中的文档管理洞见（2025-12-30）

参与 L1-L5 层级术语命名畅谈会，从文档管理角度发现几个关键风险：

| 风险类型 | 说明 | 应对建议 |
|:---------|:-----|:---------|
| **SSOT 双写风险** | 新方案提议改术语，但现有 6+ 文件已全面使用旧术语 | 必须同步更新，禁止双术语并存 |
| **术语冲突** | Path 与 DocGraph 的 `Document.Path` 字段语义冲突 | 避免使用可能与代码字段名冲突的术语 |
| **术语表缺位** | 术语定义散落在畅谈会记录、模板、规范中 | 建立权威术语表作为上游 SSOT |

**术语过渡期管理策略**（三阶段）：
1. **兼容期**：双术语并存，新文档用新术语
2. **迁移期**：逐步替换旧文档中的旧术语
3. **清理期**：移除所有旧术语引用

**行动项**：在 `agent-team/wiki/` 下建立权威术语表。

### 术语迁移成本评估模型（2025-12-31）

参与术语命名规范畅谈会，从文档管理角度建立迁移成本评估模型：

| 影响层级 | 典型文件 | 迁移策略 |
|:---------|:---------|:---------|
| SSOT 层 | 术语表、模板 | MUST 立即迁移 |
| 规范层 | 规范条款文档 | MUST 1周内 |
| 活跃层 | 当前活跃设计文档 | SHOULD 1月内 |
| 历史层 | 归档/会议记录 | MAY 保持原样 |

**核心洞见**：
1. **真正需要强制迁移的只有 SSOT + 规范层**——约 15-20 个文件，其他可渐进式更新
2. **注册表方案对 DocGraph 最友好**——Registry 本身就是 DocGraph 可消费的 SSOT
3. **历史文档保持原样是正确策略**——避免"篡改历史"的风险

**工具需求优先级**：
- P0: terminology-registry.yaml（机器可读 SSOT）
- P1: 迁移验证 grep 脚本 + Markdown lint
- P2: DocGraph 术语规范化模块

**验收检查清单模式**：grep 验证旧术语清零 + 新术语正确应用，可复用到所有术语迁移任务。

**检查脚本经验补充**（2025-12-31）：
- 检查脚本的正确性直接影响迁移判断
- `grep -i`（大小写不敏感）会把正确的 `API`、`LLM` 也匹配为警告
- 需要添加排除规则：反例说明内容、代码标识符域、文件路径域

### 三层 SSOT 结构维护特性（2025-12-31）

参与术语 SSOT 结构决策畅谈会，分析三层结构的维护特性：

**Layer 3 是关键简化器**：
- 机器可读的 `terminology-registry.yaml` 使一致性检查自动化成为可能
- 检查脚本从硬编码白名单改为动态读取 registry.yaml 是 P0 任务

**三层结构的维护特性**：

| 层次 | 变更频率 | 变更驱动 | 维护复杂度 |
|:-----|:---------|:---------|:-----------|
| Layer 1 (语义) | 低 | 畅谈会决策 | 低 |
| Layer 2 (写法) | 中 | 实践反馈 | 中 |
| Layer 3 (注册表) | 中-高 | 新缩写添加 | **低**（机器验证） |

**变更隔离的价值**：
- 改写法（Layer 2）不影响语义（Layer 1）
- 改白名单（Layer 3）不需要重写规则
- 每层变更都是原子的，不会牵一发而动全身

**迁移实施关键经验**：
1. 现有引用大多指向语义定义——迁移后仍有效，无需大规模修改
2. Redirect Stub 是过渡工件，长期目标是让分层成为自然边界
3. 历史文档保持原样——它们是历史事实的记录

**CI 集成建议**：
- PR 修改 wiki/docs/specs 时自动运行术语检查
- 定时全量扫描（每周一次）发现漂移

**角色职责矩阵**：
- DocOps: Layer 1/3 维护者，Layer 2 协调者
- Craftsman: Layer 1/2 审核者
- Seeker/Curator: 顾问角色

### 术语迁移的涟漪效应（2025-12-30）

术语体系变更会产生**涟漪式影响**——从模板到规范到实例文档。在执行 P1 术语更新任务时发现：

| 影响层级 | 文件数量 | 说明 |
|:---------|:---------|:-----|
| **模板层** | 1 | `wish-template.md` - 源头 SSOT |
| **规范层** | 2 | `README.md`, `wish-system-rules.md` |
| **实例层** | 2 | `wish-0001-*.md`, `wish-0002-*.md` - 基于旧模板创建 |

**教训**：术语更新任务不能仅更新模板和规范，必须同步扫描实例文档。未来建议：
1. 用 grep 验证旧术语是否残留
2. DocGraph 工具投产后可自动化此类一致性检查

#### 实操经验补充（2025-12-30 更新）

| 经验点 | 说明 |
|:-------|:-----|
| **先验证后更新** | grep 旧术语可快速定位所有需更新位置，避免遗漏 |
| **版本管理差异** | 规范文件需更新版本号和变更历史，模板文件不需要 |
| **权威术语表价值** | 有 SSOT 后，添加引用只需一行，大幅降低术语漂移风险 |

**术语更新检查清单**（可复用）：
- [ ] grep 旧术语，确认影响范围
- [ ] 更新模板层（源头 SSOT）
- [ ] 更新规范层（条款文档）
- [ ] 扫描实例层（已创建的文档）
- [ ] grep 验证旧术语清零
- [ ] grep 验证新术语正确应用

### 双向链接规范的例外情况（2025-12-30）

执行 Wish 系统文档一致性审计时发现：[WS-LK-002] 条款要求被引用产物必须有 ParentWish 字段，但这仅适用于**专属产物**。

| 产物类型 | 示例 | ParentWish 要求 |
|:---------|:-----|:----------------|
| **专属产物** | L3 条款文件、专属设计文档 | MUST — 单一 Wish 绑定 |
| **共享产物** | 畅谈会记录、通用规范文档 | N/A — 可被多 Wish 引用 |

**设计洞见**：畅谈会记录作为 L1 产物被引用时，其自身没有 frontmatter 和 ParentWish 是合理的——这类通用性质文档不应与单一 Wish 绑定。未来在 L3 条款中应明确这一例外。

### 场景卡片核心价值（2025-12-29）

场景卡片的核心价值是**文档包定位** — 告诉 Implementer "做这件事需要读哪些文档片段"：
- 行号定位比章节号更精确，便于 LLM 按需加载
- 条款分类（按功能域）比平铺更易理解
- 用户故事视角帮助聚焦必要上下文

### 条款注册表维护教训（2025-12-29）

条款注册表的**行号是脆弱的元数据**，任何文档重构后都需要重新生成：
- Task 1.1 提取术语表后，mvp-design-v2.md 行号漂移（减少约 171 行）
- 导致 51/51 条款行号全部失效
- 未来考虑自动化脚本维护行号映射

### 文档一致性检查方法论（2025-12-28）

当接口规范（如 `rbf-interface.md`）发生 API 形态变更时，检查下游文档需要区分**影响范围**：

| 文档类型 | 影响判断 | 示例 |
|:---------|:---------|:-----|
| **格式规范**（Layer 0） | 通常不受接口层变更影响——线格式与 API 形态是正交的 | `rbf-format.md` 不受 `Payload`/`ReservablePayload` 合并影响 |
| **测试向量** | 取决于覆盖范围：专注线格式 → 不受影响；覆盖写入器行为 → 需检查 | `rbf-test-vectors.md` 只验证 Layer 0 |
| **上层设计文档** | 需检查 API 使用示例是否需要更新 | `mvp-design-v2.md` |

**核心洞见**：分层隔离设计的价值——接口变更不污染格式规范。两个文档虽然同版本号，但变更范围独立。

**最小改动原则**：版本引用必须同步，但测试向量本身不需要"为了同步而改"——只有当可观测行为变化时才需新增/修改测试用例。

### Handoff 文档更新流程（2025-12-28）

当规范版本升级时，handoff 审阅报告的更新有固定模式：

1. **版本引用更新**：标题区域的规范版本号
2. **待办事项**：新增"已完成"的规范条目 + "待实现跟进"条目
3. **接口符合性审阅**：涉及变更的 § 节需更新描述
4. **对齐策略**：规范演进历史 + 实现跟进待办
5. **条款对照表**：新增条款行，标注"待验证"或"不符合"
6. **修订历史**：递增版本号，链接畅谈会记录

---

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [x] PipeMux - 2025-12-09 更新管理命令文档
- [ ] atelia-copilot-chat
- [x] StateJournal - 2025-12-29 文档原子化重构 Phase 1 完成

## 最近工作

### 2025-12-31 - 术语 SSOT 三层结构实施

**任务**: 响应术语命名规范畅谈会决策，实施三层 SSOT 结构

**完成项目**:

| 任务 | 变更 | 说明 |
|:-----|:-----|:-----|
| 检查脚本修复 | `check-terminology-consistency.sh` | 移除 `grep -i`，添加反例/代码域过滤 |
| terminology.md 导航添加 | v1.0.0 → v1.2.0 | 三层 SSOT 导航章节 |
| terminology.md 写法清理 | v1.2.0 → v1.3.0 | 写法规范内容移至 spec-conventions.md，用 Redirect Stub 替换 |
| wish-system-rules.md 精简 | v0.3.0 → v0.4.0 | 保留 Wish 特有术语，移除全局概念，添加三层引用 |

**验证结果**:
- wiki 目录：0 术语警告 ✅
- `check-terminology-consistency.sh` 脚本通过 ✅
- 所有 Redirect Stub 链接格式正确 ✅

**三层 SSOT 分离效果**:
- Layer 1 (terminology.md)：只包含概念语义定义
- Layer 2 (spec-conventions.md)：承载所有写法规范
- Layer 3 (terminology-registry.yaml)：机器可读约束

**洞见产出**: 术语迁移成本评估模型、三层 SSOT 维护特性 → 已归档到核心洞见

**状态**: ✅ 完成

### 2025-12-30 - 五层级术语体系迁移（P1 行动项）

**任务**: 响应畅谈会决策，将 Wish 系统相关文档的旧术语（What/How/Build）更新为新术语（Shape/Plan/Craft）

**更新文件清单**:

| 文件 | 版本变更 | 主要修改 |
|:-----|:---------|:---------|
| `wishes/templates/wish-template.md` | — | 层级进度表格术语更新，添加术语表引用 |
| `wishes/README.md` | — | 核心概念表格术语更新，添加术语表引用 |
| `wishes/specs/wish-system-rules.md` | v0.1.0 → v0.2.0 | §1 术语定义更新，新增 §9 术语演变说明 |
| `wishes/active/wish-0001-*.md` | — | 层级进度表格术语更新（超出 P1 范围，为一致性额外更新） |
| `wishes/active/wish-0002-*.md` | — | 层级进度表格术语更新（超出 P1 范围，为一致性额外更新） |

**验证结果**:
- ✅ 旧术语（L2 What, L4 How, L5 Build）：0 处残留
- ✅ 新术语（L2 Shape, L4 Plan, L5 Craft）：9 处正确应用
- ✅ 术语表引用：3 处添加

**洞见产出**: 术语迁移的涟漪效应 → 已归档到核心洞见

**状态**: ✅ 完成

### 2025-12-30 - Wish 系统首次文档一致性审计

**任务**: 审计 Wish 系统文档结构与双向链接规范落地情况

**核心发现**:
1. Wish 系统结构完整，37 条 L3 条款覆盖全面
2. 双向链接规范存在落地差距：畅谈会记录作为 L1 产物被引用，但其自身没有 frontmatter 和 ParentWish 字段
3. 这一差距是**合理的例外**：畅谈会记录是通用性质文档，不应与单一 Wish 绑定

**洞见产出**: 双向链接的例外情况（专属产物 vs 共享产物区分）→ 已归档到核心洞见

**状态**: ✅ 完成

### 2025-12-29 - StateJournal 文档重构 Phase 1 完成

**任务**: 执行 StateJournal 文档原子化重构 Phase 1

**产出文件**:
| 任务 | 产出 | 关键指标 |
|:-----|:-----|:---------|
| Task 1.0 条款注册表 | `clauses/index.md` | 51 条款（S:33, A:18） |
| Task 1.1 术语表提取 | `glossary.md` | 266 行，原文减少 171 行（11.5%） |
| Task 1.2 场景卡片 | `scenarios/loadobject.md` + `scenarios/index.md` | 9 条款引用，全部验证通过 |

**Task 1.0 关键发现**:
- 多次出现条款需要 Primary Definition 判定（P1/P2/P3 规则）
- [S-OBJECTID-RESERVED-RANGE] 出现 4 次为最多

**Task 1.1 验收清单**:
- [x] 独立可读 — glossary.md 可脱离其他文件解释核心术语
- [x] 无重复定义 — 每个术语只有一个 Primary Definition
- [x] Stub 存在 — 原位置有指向 glossary.md 的 stub
- [x] 非规范性标注 — 实现映射标注为 *(Informative)*

**Task 1.2 场景卡片结构**:
- 用户故事（Implementer 视角）
- 核心/扩展文档包（带行号定位）
- 关键条款（分功能域归类）
- 实现要点（流程图）

**状态**: ✅ 完成

### 2025-12-29 - clauses/index.md 行号漂移紧急修复

**问题**: Task 1.1 术语表提取后，mvp-design-v2.md 行号漂移导致 51/51 条款行号失效
**附加问题**: 统计口径错误 `S:35, A:16` → 实际 `S:33, A:18`

**修复内容**:
- 行号重算：51/51 条款全部重新定位
- 统计修正
- 跨文件条款更新：`[S-STATEJOURNAL-TOMBSTONE-SKIP]` → `rbf-interface.md L65`
- 版本：v1.0 → v1.1

**验收抽查**:
- S-CHECKPOINT-HISTORY-CUTOFF: 726 → 555 ✓
- A-LOADOBJECT-RETURN-RESULT: 797 → 626 ✓

**状态**: ✅ 完成

### 2025-12-28 - FrameTag Wrapper Type 移除全文档同步
- **任务**: 响应 wrapper type 审计决议，同步所有 RBF 相关文档
- **关联决议**: `meeting/2025-12-28-wrapper-type-audit.md`
- **变更文件**:
  | 文件 | 版本变更 | 主要修改 |
  |:-----|:---------|:---------|
  | `rbf-interface.md` | v0.16 → v0.17 | 移除 `[F-FRAMETAG-DEFINITION]`，§2.1 改为概念描述，接口参数 `FrameTag` → `uint` |
  | `mvp-design-v2.md` | v3.8 → v3.9 | 术语表 FrameTag 定义更新，新增 Packing/Unpacking 逻辑小节 |
  | `rbf-format.md` | v0.15 → v0.16 | 修复 `[F-FRAMETAG-WIRE-ENCODING]` 过时引用 |
  | `rbf-test-vectors.md` | v0.10 → v0.11 | 审计确认无需修改，更新关联规范版本 |
  | handoff 审阅报告 | - | 新增 TODO #9/#10，更新条款合规表 |
- **设计洞见**: wrapper type 的价值在于编译期类型安全，但当类型只是 uint 别名且无额外验证逻辑时，直接用原始类型更简洁。接口层用 uint，应用层可自由选择 enum 语义封装。
- **状态**: ✅ 完成

### 2025-12-28 - RBF 测试向量文档版本同步检查
- **任务**: 检查 `rbf-test-vectors.md` 是否需要更新以反映 `rbf-interface.md` v0.16 变更
- **变更来源**: 畅谈会 `2025-12-28-rbf-builder-payload-simplification.md`
- **检查结果**:
  - `ReservablePayload` 双属性设计：**无引用**（测试向量专注 Layer 0 线格式，不涉及写入器 API）
  - `[S-RBF-BUILDER-FLUSH-NO-LEAK]` 条款：**无需测试向量**（写入器行为，通过单元测试验证）
  - Auto-Abort 变更：**无需更新**（语义澄清+防御性约束，不改变可观测行为）
- **执行修改**:
  - 更新版本号：0.9 → 0.10
  - 更新关联规范引用：`rbf-interface.md v0.15` → `v0.16`
  - 添加变更日志条目
- **核心洞见**: 分层隔离设计的价值——接口层 API 形态变更不影响 Layer 0 测试向量
- **状态**: ✅ 完成

### 2025-12-27 - StateJournal mvp-design-v2.md Workspace 绑定机制增补
- **任务**: 添加 §3.1.2.1 Workspace 绑定机制条款
- **修改文件**: `atelia/docs/StateJournal/mvp-design-v2.md`
- **变更内容**:
  - 在 §3.1.2 之后添加 §3.1.2.1 Workspace 绑定机制（增补）小节
  - 添加 7 个规范条款：S-WORKSPACE-OWNING-EXACTLY-ONE, S-WORKSPACE-OWNING-IMMUTABLE, S-WORKSPACE-CTOR-REQUIRES-WORKSPACE, S-LAZYLOAD-DISPATCH-BY-OWNER, A-WORKSPACE-FACTORY-CREATE, A-WORKSPACE-FACTORY-LOAD, A-WORKSPACE-AMBIENT-OPTIONAL
  - 更新 A.1 伪代码骨架中 DurableDict 构造函数注释，说明需要 Workspace 参数
  - 更新版本号从 v3.1 到 v3.8
- **来源**: 畅谈会 #5 + 监护人决策，详细设计见 `workspace-binding-spec.md`
- **状态**: ✅ 完成

### 2025-12-24 - StateJournal mvp-design-v2.md 文档冗余清理
- **任务**: 移除冗余内容，精简文档
- **修改文件**: `atelia/docs/StateJournal/mvp-design-v2.md`
- **变更内容**:
  - Section 2（设计决策）：原有摘要表格已移至外部决策记录，简化为单句指针
  - Section 3.1.0 末尾：Identity Map/Dirty Set 定义与术语表重复，已删除
- **效果**: 文档体积从 1416 行减少到 1399 行（-17 行）
- **状态**: ✅ 完成

### 2025-12-24 - StateJournal mvp-design-v2.md 冗余枚举清理
- **任务**: 移除重复的枚举列表，统一引用术语表中的枚举值速查表
- **修改文件**: `atelia/docs/StateJournal/mvp-design-v2.md`
- **变更内容**:
  - §3.2.5 ObjectVersionRecord：将详细的 ObjectKind 范围列表替换为引用，保留规范性标签
  - §3.4.2 Dict 的 DiffPayload：将 ValueType 枚举值列表替换为术语表引用
- **设计理由**: 枚举值已在 Glossary 的"枚举值速查表"中集中定义，避免多处重复维护
- **状态**: ✅ 完成

### 2025-12-23 - 记忆积累机制畅谈会（第三波）
- **任务**: 审阅 DocOps 记忆文件并参与第三波讨论
- **发言文件**: `agent-team/meeting/2025-12-22-memory-accumulation-reflection.md`
- **核心观点**:
  - 86 行精简是"设计好"的结果：index.md 只放工作日志，产物外置
  - 提出"三层分类"文档规范：索引层、摘要层、详情层
  - 建议采用"changefeed anchor"作为记忆文件的索引机制
  - 补充"链接检验"视角：过期链接是记忆腐败的主要症状
- **状态**: ✅ 完成

### 2025-12-21 - StateJournal mvp-design-v2.md §3.4.8 引用更新
- **任务**: 将 Error Affordance 部分改为引用全项目规范
- **修改文件**: `atelia/docs/StateJournal/mvp-design-v2.md`
- **变更内容**:
  - 添加规范提升通知 banner，引用 `AteliaResult-Specification.md`
  - 添加条款映射表，说明原 StateJournal 本地条款与新全项目条款的对应关系
  - 更新 ErrorCode 格式：从 `SCREAMING_SNAKE_CASE` 改为 `StateJournal.{ErrorName}` 格式
  - 保留 StateJournal 特有的 ErrorCode 注册表
  - 保留异常示例（更新 ErrorCode 格式）
- **删除的重复内容**:
  - 结构化错误字段定义（已在全项目规范中定义）
  - 本地条款定义 `[A-ERROR-CODE-MUST]` 等（改为引用）
- **状态**: ✅ 完成

### 2025-12-21 - AteliaResult 全项目规范文档
- **任务**: 创建 `atelia/docs/AteliaResult-Specification.md`
- **来源**: LoadObject 命名与返回值设计畅谈会共识
- **文档内容**:
  - §1 概述：定位、设计目标、核心洞察
  - §2 规范语言（RFC 2119）
  - §3 类型定义：`AteliaResult<T>`、`AteliaError`、`AteliaException`、`IAteliaHasError`
  - §4 规范条款（从 StateJournal 提升为全项目范围）
  - §5 使用规范：Result vs 异常选择、派生类模式、JSON 序列化
  - §6 与 StateJournal 规范的关系
  - §7 代码位置
- **定义的条款**:
  - [ATELIA-ERROR-CODE-MUST]
  - [ATELIA-ERROR-MESSAGE-MUST]
  - [ATELIA-ERROR-RECOVERY-HINT-SHOULD]
  - [ATELIA-ERROR-DETAILS-MAY]
  - [ATELIA-ERROR-CAUSE-MAY]
  - [ATELIA-ERRORCODE-NAMING]
  - [ATELIA-ERRORCODE-REGISTRY]
- **约束**:
  - Cause 链最多 5 层
  - Details 最多 20 个 key
  - ErrorCode 格式：`{Component}.{ErrorName}`
- **状态**: ✅ 完成

### 2025-12-21 - DurableHeap → StateJournal 更名迁移
- **任务**: 响应项目更名通知，更新所有文档索引中的 DurableHeap 引用
- **变更摘要**:
  - 旧路径：`DurableHeap/docs/` ❌ 已删除
  - 新路径：`atelia/docs/StateJournal/` ✅
  - 命名空间：`Atelia.StateJournal`
- **更新文件**:
  - `agent-team/lead-metacognition.md`: 项目 Backlog 表格路径更新
  - `.github/agents/docui-standards-chair.agent.md`: 任务类型加载文件表格更新
  - `agent-team/members/Curator-advisor/index.md`: 项目列表更新
  - `agent-team/wiki/DurableHeap/` → `agent-team/wiki/StateJournal/`: 目录重命名
  - `agent-team/wiki/StateJournal/concept.md`: 添加更名通知 banner
  - `agent-team/wiki/StateJournal/jam-brief-1.md`: 添加更名通知 banner
- **保留历史记录的文件**（不修改，作为历史事实）:
  - `agent-team/members/implementer/index.md`: 历史工作日志
  - `agent-team/archive/state-journal-mvp-design-v2.before-rename.md`: 更名前快照
- **状态**: ✅ 完成

### 2025-12-09 - PipeMux 管理命令文档更新
- **任务**: 更新 PipeMux 管理命令文档
- **更新文件**:
  - `PipeMux/docs/README.md`: 添加管理命令章节（`:list`, `:ps`, `:stop`, `:help`）
  - `agent-team/wiki/PipeMux/README.md`: 添加管理命令章节，更新已知问题表格标记 P1 任务已完成
- **状态**: ✅ 完成
