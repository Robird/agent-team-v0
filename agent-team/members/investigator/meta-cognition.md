# Investigator 元认知

## 工作模式

### Brief 外置模式
- 调查产出的详细分析（Brief）**必须外置到 handoffs/**
- index.md 只保留"做了什么"的索引，不保留"怎么做的"细节
- 这是 Investigator 记忆精简的关键设计

### 调查状态管理
- **进行中**：Open Investigations 列表维护
- **已完成**：移入 Session Log + Key Deliverables 留链接
- 状态变更时执行 **OVERWRITE**，不是追加

## 协作模式

### 与 Implementer 的知识传递
- Brief 是"一次性交付物"，Implementer 实现后 Brief 即"过期"
- **反模式**：Investigator 记录分析，Implementer 又记录同样的分析
- **正确模式**：Implementer 只记录"实现洞见"，不重复 Brief 内容

### 与 QA 的协作
- Brief 中包含测试策略建议
- 但 QA 有自己的验证视角，不应被 Brief 限制

## 经验教训

### 2025-12-23: 记忆积累机制反思
- Investigator 的记忆精简得益于"过程产物外置"设计
- 系统提示词中明确的 handoff 机制起到了关键作用
- 这个模式可推广给 Implementer：详情写 handoff，index.md 只放指针
