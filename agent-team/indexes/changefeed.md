# Changefeed 索引

> **文档性质**: 变更记录索引
> **维护者**: DocOps
> **说明**: 本文件记录重要的理论、框架、流程变更，便于追溯和引用。

---

## 变更记录

### #delta-2026-01-02-artifact-tiers-theory-upgrade
**日期**: 2026-01-02  
**类型**: 理论框架升级  
**状态**: ✅ 已完成  
**影响面**: 高（所有使用 Artifact-Tiers 的文档和工具）

#### 变更概述
基于 [2026-01-02 畅谈会](../meeting/2026-01-02-artifact-tiers-wish-integration-jam.md)，Artifact-Tiers 框架进行了重大理论升级和文档重构。

#### 核心升级内容

**1. 理论框架升级**
- **认知转化链**: 五层级从静态结构升级为动态认知流动
- **旅途隐喻**: 引入可重入、可折返的旅程理解
- **二维模型**: Tier（认知深度）× Wish（时间状态）交叉定位
- **跨层不变量**: 意图保真、可追溯、正交性维护

**2. 文档结构重构**
- 从单体文档 (`artifact-tiers.md`) 重构为文档群 (`artifact-tiers/`)
- 创建渐进式阅读路径：30秒/5分钟/深度版
- 按角色拆分用户指南：开发者/设计师/产品经理

**3. Wish 系统集成**
- Wish 明确为**生命周期轴**，不是第六个 Tier
- 建立 Tier × Wish 二维定位系统
- 每个 Wish 在特定时间处于特定 Tier 的交叉点

#### 文档变更

**新建文件**:
- `artifact-tiers/README.md` - 快速入口
- `artifact-tiers/core-definitions.md` (v2.0.0) - 核心定义 SSOT
- `artifact-tiers/user-guides/` - 角色指南目录
- `artifact-tiers/tools/` - 思维工具目录
- `artifact-tiers/integrations/` - 集成指南目录

**更新文件**:
- `artifact-tiers.md` → 重定向页面 (v2.0.0)
- `spec-conventions.md` (v0.7) - 术语写法规范
- `terminology-registry.yaml` (v1.1.0) - 机器可读注册表

**会议记录**:
- [2026-01-02 Artifact-Tiers 与 Wish 集成畅谈会](../meeting/2026-01-02-artifact-tiers-wish-integration-jam.md)
- [2026-01-02 Resolve-Tier 概念畅谈会](../meeting/2026-01-02-resolve-tier-concept-jam.md)

#### 引用更新需求

需要更新引用的文件：
- [ ] 所有引用 `artifact-tiers.md` 的文档
- [ ] 代码中的注释引用
- [ ] 工具配置和模板
- [ ] 培训材料和演示文稿

#### 迁移计划

**阶段 1 (2026-01-02)**:
- ✅ 创建文档群结构
- ✅ 撰写核心定义 (v2.0.0)
- ✅ 创建重定向页面
- ✅ 记录 Changefeed Anchor

**阶段 2 (2026-01-16 前)**:
- [ ] 更新所有内部引用
- [ ] 完成用户指南撰写
- [ ] 开发思维工具模板

**阶段 3 (2026-02-01)**:
- [ ] 完全归档原文件
- [ ] 验证所有引用更新
- [ ] 团队培训和新成员引导

#### 相关决策
- [2026-01-02 畅谈会决策](../meeting/2026-01-02-artifact-tiers-wish-integration-jam.md#主持人决策)
- Resolve-Tier 术语迁移完成确认
- Wish 作为生命周期轴的定位确认

---

### #delta-2026-01-02-resolve-tier-migration
**日期**: 2026-01-02  
**类型**: 术语迁移  
**状态**: ✅ 已完成  
**影响面**: 中（术语一致性）

#### 变更概述
Why-Tier → Resolve-Tier 术语迁移完成。

#### 变更内容
- 术语定义更新：Resolve = 分析（解）+ 决策（决）
- Stage Token 迁移：`[AA:WHY]` → `[AA:RESOLVE]`
- SSOT 更新：术语表、注册表、规范文档
- Wish 系统扩展：新增 Biding 状态

#### 相关记录
- [Resolve-Tier 迁移总结](../meeting/2026-01-02-resolve-tier-concept-jam.md)
- [语言学验证记录](../members/TeamLeader/inbox.md#便签-2026-01-02-resolve-tier-迁移完成总结)

---

## 变更分类

### 理论框架变更
- `#delta-2026-01-02-artifact-tiers-theory-upgrade` - 重大理论升级

### 术语变更  
- `#delta-2026-01-02-resolve-tier-migration` - Why-Tier → Resolve-Tier

### 流程变更
*(暂无)*

### 工具变更
*(暂无)*

---

## 变更模板

```markdown
### #delta-YYYY-MM-DD-简短描述
**日期**: YYYY-MM-DD  
**类型**: 理论框架/术语/流程/工具  
**状态**: ⏳ 进行中 / ✅ 已完成 / 🚫 已取消  
**影响面**: 低/中/高

#### 变更概述
简要描述变更内容。

#### 变更内容
- 具体变更项1
- 具体变更项2

#### 文档变更
**新建文件**:
- `path/to/new-file.md`

**更新文件**:
- `path/to/updated-file.md`

**会议记录**:
- [会议标题](../meeting/YYYY-MM-DD-topic.md)

#### 引用更新需求
- [ ] 需要更新的引用1
- [ ] 需要更新的引用2

#### 迁移计划
**阶段 1 (日期)**:
- [ ] 任务1
- [ ] 任务2

**阶段 2 (日期)**:
- [ ] 任务3
- [ ] 任务4

#### 相关决策
- [决策记录链接]
```

---

## 维护说明

### 添加新变更
1. 在"变更记录"部分添加新条目
2. 使用模板格式
3. 更新"变更分类"
4. 通知相关维护者

### 变更状态更新
- **进行中**: 变更正在实施
- **已完成**: 变更实施完成，文档更新完毕
- **已取消**: 变更被取消，需说明原因

### 引用管理
- 所有重要变更都应在此记录
- 相关文档应引用对应的 `#delta-` 标签
- 定期检查引用更新状态

---

**最后更新**: 2026-01-02  
**维护者**: DocOps  
**审核者**: Craftsman