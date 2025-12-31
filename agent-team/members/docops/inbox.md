# Inbox — 待处理便签

> 这是你的便签收集箱。
> 写下洞见、经验、状态变更，MemoryPalaceKeeper 会定期整理归档。
>
> **格式**：每条便签用 `## 便签 YYYY-MM-DD HH:MM` 开头，用 `---` 分隔。

---

<!-- 在下方添加你的便签 -->

## 便签 2026-01-01 12:00

**Recipe 目录审计摘要完成**

完成了 `agent-team/recipe/` 目录下 12 个 Recipe 文件的全面审计。

**核心发现**：

1. **Recipe 文档质量普遍较高**：大多数文档结构完整、目的明确、有实战案例
2. **长度分布差异大**：从 100 行（memory-palace-batch-guide.md）到 1000+ 行（jam-session-guide.md、real-time-collaboration-pattern.md）
3. **记忆相关 Recipe 形成完整体系**：accumulation → maintenance → orchestration → batch-guide 形成闭环

**最佳实践典范**：
- `spec-driven-code-review.md`：条款体系完整、可审计产物清晰
- `strategic-tactical-dual-session.md`：有量化验证数据（8x 效率提升）
- `naming-skill-guide.md`：提供多种方法论的适用边界框架

**改进建议**：
- 部分超长文档（>800行）可考虑拆分或提供速查摘要
- `real-time-collaboration-pattern.md` 有重复内容（案例研究部分重复出现）

---

## 便签 2026-01-01 14:30

**Recipe元讨论：文档管理视角洞见**

参与Recipe元讨论畅谈会，从文档管理角度分析了Recipe在整个文档生态中的定位。

**核心洞见**：

1. **Recipe占据独特生态位**：Wiki回答"是什么"，Spec定义"必须什么"，Meeting记录"发生了什么"，而Recipe是"怎么做"——它是知识转化为行动的桥梁。

2. **文档类型拓扑图**：绘制了从AGENTS.md（入口）到indexes/（导航枢纽）的完整文档类型关系图，明确了Recipe与wiki/spec/meeting/members的依赖关系。

3. **三维分类法**：建议采用场景维度（使用时机）、角色维度（目标读者）、依赖维度（前置关系）对Recipe进行索引。

4. **变更传播路径**：Recipe变更会产生涟漪效应——影响System Prompt、其他Recipe引用、索引状态。建议集成到现有Changefeed系统。

5. **新成员引导机制**：从AGENTS.md入口→recipe/index.md索引→具体Recipe的三层渐进引导，配合推荐学习路径。

**行动建议**：
- P0: 创建`recipe/index.md`作为唯一入口
- P1: Recipe头部必须声明DependsOn/SeeAlso依赖
- P2: Recipe MAJOR变更集成到Changefeed Anchor系统

**关联文件**：`meeting/2026-01-01-recipe-meta-discussion.md`

---

## 便签 2026-01-01 16:30

**Recipe发现机制技术讨论：文档管理视角洞见**

参与Recipe发现机制技术讨论畅谈会，从文档管理角度提供了系统分析。

**核心洞见**：

1. **引用关系影响评估模型**：迁移影响可分层评估——SSOT层（MUST迁移）、Agent配置层（MUST迁移）、活跃文档层（SHOULD迁移）、历史层（MAY保留原样）。真正需要强制迁移的只有约10-15处引用。

2. **与现有索引体系的集成**：Prompt化索引应紧跟AGENTS.md的"关键路径"表格，形成入口层→导航层→详情层的三层结构。新增索引增加约225 tokens，ROI正向。

3. **Prompt化索引实现方案**：提供了完整的文件名映射表（13个文件）和AGENTS.md集成设计。关键设计决策是将Recipe入口纳入"关键路径"表格，保持现有结构的一致性。

4. **变更传播管理**：建议为Recipe迁移建立Changefeed Anchor（`#delta-2026-01-01-recipe-discovery`），融入现有的变更管理流程。

5. **风险对比洞见**：Recipe迁移与术语迁移对比——Recipe迁移是纯路径变更（低风险），术语迁移是语义变更（高风险）。前者可以更激进执行。

**产出物**：
- 完整的文件名映射表（旧→新）
- 三阶段迁移策略（结构→引用修复→验证）
- 验收检查清单模板
- 重定向 Stub 设计

**关联文件**：`meeting/2026-01-01-recipe-discovery-mechanism.md`

---
