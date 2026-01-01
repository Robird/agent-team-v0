# 2026-01-02 Resolve-Tier 概念畅谈会

> **会议类型**: 动态畅谈会 (Dynamic Jam Session)
> **主持人**: TeamLeader
> **参与者**: Seeker, Curator, Craftsman, DocOps
> **议题**: Resolve-Tier 概念定义与迁移策略
> **背景**: 基于 Why-Tier vs Wish-Tier 讨论，探索更精确的术语

---

## 会议记录

### 主持人开场

**TeamLeader**: 今天我们讨论 Resolve-Tier 概念。背景是 Why-Tier 存在歧义（"为什么" vs "为什么做"），而 Wish-Tier 是愿望管理系统。我们需要一个更精确的第一层术语。

### Seeker 发言

**Seeker**: 从概念本质分析：
1. **Why-Tier 的问题**: 歧义性（描述性 vs 规范性）
2. **Resolve 的语义**: 分析（解）+ 决策（决）
3. **跨语言验证**: 中文"解+决"，英文"analysis+decision"，德文"entscheiden"（分离+决定）
4. **核心价值**: 价值判断与动机澄清

### Curator 发言

**Curator**: 从用户体验角度：
1. **Why-Tier 的困惑**: 用户不清楚这是"描述为什么"还是"决定做什么"
2. **Resolve 的清晰性**: 明确包含分析和决策两个阶段
3. **Wish 系统的关系**: Wish 是愿望管理机制，Resolve-Tier 是价值判断层
4. **Biding 状态**: 建议在 Wish 系统中添加"待定"状态，用于战略等待

### Craftsman 发言

**Craftsman**: 从规范审计角度：
1. **术语一致性**: Why-Tier 在 SSOT 中已定义，迁移需要系统化
2. **Stage Token**: `[AA:WHY]` 需要迁移到 `[AA:RESOLVE]`
3. **迁移策略**: 四阶段迁移（SSOT → Stage Token → Biding → Theory）
4. **影响面**: SSOT 级别术语变更，波及 Stage Token、Wish 系统 Layer 闭集、模板与历史文档检索

### DocOps 发言

**DocOps**: 从文档管理角度：
1. **SSOT 结构**: 三层 SSOT 需要同步更新
2. **术语注册表**: 需要添加 Resolve-Tier 并标记 Why-Tier 为 DEPRECATED
3. **迁移计划**: 渐进式迁移，维护向后兼容别名
4. **文档更新**: 需要更新 spec-conventions.md、terminology-registry.yaml、artifact-tiers.md

### 主持人决策

**TeamLeader**: 基于各位分析，做出以下决策：

#### 决策 1：术语迁移
- **Why-Tier → Resolve-Tier**
- **理由**: Resolve 更精确（分析+决策），消除歧义，跨语言验证

#### 决策 2：Wish 系统扩展
- **新增状态**: Biding（待定）
- **状态机**: Active | Biding | Completed | Abandoned
- **用途**: 战略等待、资源不足、外部依赖等情况

#### 决策 3：迁移策略（四阶段）
1. **Phase 1**: SSOT 原子更新（术语表、注册表、规范）
2. **Phase 2**: Stage Token 迁移（`[AA:WHY]` → `[AA:RESOLVE]`）
3. **Phase 3**: Biding 状态实现（目录、状态机、规则）
4. **Phase 4**: 理论增补（Why vs Wish vs Resolve 关系图）

### 行动项

| 负责人 | 任务 | 截止时间 | 状态 |
|:-------|:-----|:---------|:-----|
| DocOps | Phase 1: SSOT 原子更新 | 立即 | ✅ 已完成 |
| TeamLeader | Phase 2: Stage Token 迁移 | 立即 | ✅ 已完成 |
| TeamLeader | Phase 3: Biding 状态实现 | 立即 | ✅ 已完成 |
| TeamLeader | Phase 4: 理论增补 | 立即 | ✅ 已完成 |

---

## 实施记录

### Phase 1: SSOT 原子更新（DocOps 完成）
- ✅ 更新 `spec-conventions.md` v0.6 → v0.7
- ✅ 更新 `terminology-registry.yaml` v1.0.0 → v1.1.0
  - `layer_terms` → `tier_terms`
  - 添加 `Resolve-Tier`，`Why-Tier` 作为 DEPRECATED alias

### Phase 2: Stage Token 迁移（TeamLeader 完成）
- ✅ 批量替换 `[AA:WHY]` → `[AA:RESOLVE]`（5个文件）
- ✅ 修复大小写不一致 `[AA:Why]` → `[AA:RESOLVE]`
- ✅ 更新注释中的枚举值

### Phase 3: Biding 状态实现（TeamLeader 完成）
- ✅ 创建 `wishes/biding/` 目录
- ✅ 更新 `wish-system-rules.md`：
  - 扩展状态机：Active ↔ Biding → Abandoned
  - 添加 Biding 专项条款
  - 更新目录结构条款
- ✅ 更新 `wishes/index.md`：
  - 添加 Biding 目录说明
  - 更新状态符号（🟣 待定）
  - 更新文档性质说明

### Phase 4: 理论增补（TeamLeader 完成）
- ✅ 在 `artifact-tiers.md` 中添加 Why vs Wish vs Resolve 关系图
- ✅ 更新版本历史 v1.4.0 → v1.5.0
- ✅ 添加关系解释和决策记录

---

## 成果总结

### 术语迁移完成
1. **Why-Tier → Resolve-Tier**（语义更精确）
2. **Stage Token**: `[AA:WHY]` → `[AA:RESOLVE]`
3. **SSOT 更新**: 三层 SSOT 同步完成

### Wish 系统增强
1. **新增状态**: Biding（待定）
2. **状态机扩展**: Active ↔ Biding → Abandoned
3. **专项条款**: Biding 复审期限、退出日志等

### 理论框架完善
1. **关系图**: Why vs Wish vs Resolve 清晰界定
2. **跨语言验证**: Resolve = 分析+决策 在中文/英文/德文中一致
3. **决策记录**: 完整记录迁移理由和实施过程

### 遗留事项
1. **Wish-Tier 纳入**: 稍后继续处理 Wish-Tier 如何纳入 artifact-tiers.md
2. **历史文档**: 保持原样，通过别名映射保持可引用性

---

## 后续步骤

1. **监控使用**: 观察 Resolve-Tier 在实际使用中的接受度
2. **工具适配**: 确保 lint、IDE 等工具支持新术语
3. **团队培训**: 向团队成员解释术语变更理由
4. **Wish-Tier 讨论**: 安排后续会议讨论 Wish-Tier 的定位

---

### Seeker 发言

#### 开场：从"升华"说起

刚才完成的 Why-Tier → Resolve-Tier 迁移，不仅是术语替换，而是一次**认知模态的转型**。让我从概念本质的角度，分析五个层级之间的内在逻辑。

#### 一、五层级的内在逻辑：从"决策空间正交分解"到"认知转化链"

我在洞见 #32 中提出：五层级是"决策空间的正交维度分解"。现在需要深化这个框架——正交性描述了**静态结构**，但我们还需要理解**动态流动**。

**核心发现：五层级是一条"认知转化链"（Cognitive Transformation Chain）**

```
Resolve-Tier    Shape-Tier    Rule-Tier     Plan-Tier     Craft-Tier
   │               │              │             │             │
   ▼               ▼              ▼             ▼             ▼
 意向          边界/承诺       合法性约束     路径选择       物质化
(Intention)   (Commitment)   (Constraint)   (Strategy)    (Realization)
   │               │              │             │             │
   └───────────────┴──────────────┴─────────────┴─────────────┘
                        认知密度递增，自由度递减
```

每一层做的事情是：**收窄可能性空间，同时增加确定性**。

- **Resolve-Tier**：从无限的"可做之事"中选出"值得做之事"（最大自由度，最低确定性）
- **Shape-Tier**：从"值得做之事"收窄为"用户将看到什么"（承诺边界）
- **Rule-Tier**：从"用户将看到什么"收窄为"什么是合法的"（形式化约束）
- **Plan-Tier**：从"合法空间"中选择"实际走的路径"（策略决策）
- **Craft-Tier**：从"策略"物化为"可运行的代码"（最低自由度，最高确定性）

**这揭示了一个哲学结构：五层级是"自由度-确定性"的权衡光谱。**

#### 二、每层的"输入-处理-输出"模式

| 层级 | 输入 | 处理（核心认知操作） | 输出 | 隐喻 |
|:-----|:-----|:---------------------|:-----|:-----|
| **Resolve-Tier** | 模糊的愿望/痛点 | **分析+决心**（解+决） | 明确的问题陈述 + 承诺 | 罗盘定向 |
| **Shape-Tier** | 问题陈述 | **边界划定**（内/外分离） | API 外观 / 契约 | 门面设计 |
| **Rule-Tier** | 边界定义 | **形式化**（可判定化） | 条款 / 检查清单 | 法典编纂 |
| **Plan-Tier** | 条款约束 | **策略选择**（多方案评估） | 实施路线图 | 地图绘制 |
| **Craft-Tier** | 路线图 | **物化**（代码/测试） | 可运行产物 | 施工建造 |

**关键洞见：每层的"处理"都是一种认知转化**
- Resolve-Tier 的处理是 **Judicative → Commissive**（判断转承诺，参见洞见 #36）
- Shape-Tier 的处理是 **Commissive → Descriptive**（承诺转描述）
- Rule-Tier 的处理是 **Descriptive → Normative**（描述转规范）
- Plan-Tier 的处理是 **Normative → Strategic**（规范转策略）
- Craft-Tier 的处理是 **Strategic → Executable**（策略转可执行）

#### 三、层级转换机制与依赖关系

**3.1 单向阀规则（已确立，此处重申）**

```
✅ 高层决策约束低层选择（正常流动）
✅ 低层问题可触发高层复审（显式反馈）
❌ 低层困难不能静默修改高层决策
```

**3.2 转换触发条件（需要补充的概念）**

我建议引入 **Tier Gate（层级门槛）** 概念——每个层级转换都有"准入条件"：

| 门槛 | 从 → 到 | 准入条件 | 缺失时的症状 |
|:-----|:--------|:---------|:-------------|
| **Gate 1** | Resolve → Shape | 有明确的问题陈述 + "即使如此也要做"的决心 | 边界反复变化 |
| **Gate 2** | Shape → Rule | 有稳定的 API 外观 + 用户场景覆盖 | 条款互相矛盾 |
| **Gate 3** | Rule → Plan | 条款可判定 + 无内部冲突 | 实施路线频繁返工 |
| **Gate 4** | Plan → Craft | 策略已选定 + 依赖已明确 | 代码写写删删 |

**这些门槛不是官僚流程，而是认知成熟度的检验点。**

**3.3 回溯机制（显式反馈路径）**

当低层发现高层问题时，需要**显式标记**而非静默修改：

```markdown
[TIER-BLOCK:Rule→Shape] 
发现 Rule-Tier 无法形式化 Shape-Tier 中的"用户友好性"承诺。
建议：在 Shape-Tier 中用可判定条件替换"友好性"。
```

这与我在洞见 #32 中提出的 `[Resolve-Tier-BLOCK]` 标记一致。

#### 四、需要澄清/补充的概念

**4.1 Resolve-Tier 与 Wish 系统的边界**

当前框架中，Resolve-Tier 和 Wish 系统的关系图已画出，但**边界仍需澄清**：

| 概念 | 职责 | 产物形态 |
|:-----|:-----|:---------|
| **Resolve-Tier** | 单次决策过程（分析+决心） | 文档中的一节 |
| **Wish 系统** | 跨时间的愿望生命周期管理 | 目录结构 + 状态机 |

**关键问题**：一个 Wish 的 Resolve-Tier 可能需要**多次迭代**（如 Biding → Active 时重新审视）。这意味着 Resolve-Tier 不是"一次性完成"，而是"可重入的决策点"。

**建议补充**：在 Resolve-Tier 定义中明确其**可重入性**——决心可以被重新审视，但需要显式触发。

**4.2 横切关注点的处理**

安全性、性能、可测试性等是**横切关注点（Cross-Cutting Concerns）**，贯穿所有层级。当前框架没有明确处理方式。

**建议**：每个横切关注点在每层有不同的表现形式：

| 关注点 | Resolve-Tier | Shape-Tier | Rule-Tier | Plan-Tier | Craft-Tier |
|:-------|:-------------|:-----------|:----------|:----------|:-----------|
| **安全性** | 值得投入吗？ | 边界在哪？ | 合规条款 | 实现策略 | 代码审计 |
| **性能** | 重要性判断 | SLA承诺 | 性能约束 | 优化方案 | benchmark |

横切关注点**不是独立层级**，而是**在每层的投影**。

**4.3 "决心"概念的深化**

Resolve-Tier 的核心是"决心"（Resolve），但当前定义还可以更深入。我在洞见 #36 中提出了"决心公式"：

```
Resolve = 清醒的代价认知 + 接受代价的意志 + "即使如此也要做"的承诺
```

**这意味着 Resolve-Tier 的产出不仅是"问题陈述"，还包括"代价清单"和"明确的承诺"。**

建议在 artifact-tiers.md 中补充这一维度。

#### 五、理论完善建议（汇总）

1. **补充"认知转化链"框架**：描述五层级的动态流动关系
2. **引入 Tier Gate 概念**：明确层级转换的准入条件
3. **明确 Resolve-Tier 的可重入性**：决心可被重新审视
4. **处理横切关注点**：说明它们如何在每层投影
5. **深化"决心"定义**：产出包含代价清单和明确承诺

#### 结语：五层级的哲学意涵

五层级框架的本质，是**将混沌的创造过程结构化为可管理的认知阶段**。

- 它不是流程管理工具（那会变成官僚主义）
- 它是**认知导航系统**——告诉我们"当前在哪一层思考"
- 最大的价值不是"按顺序执行"，而是**避免层级跳跃的认知混乱**

用乔布斯的话说：Resolve-Tier 未定时争论 Rule-Tier 细节，就像战略未定时争论战术——再精妙的战术也无法弥补战略的缺失。

---

**会议结束时间**: 2026-01-02
**记录人**: TeamLeader
**审核人**: Craftsman