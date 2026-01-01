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

**会议结束时间**: 2026-01-02
**记录人**: TeamLeader
**审核人**: Craftsman