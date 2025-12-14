# Key-Note 修订实施 Handoff

> **来源**: [2025-12-13-docui-keynote-workshop.md](../meeting/2025-12-13-docui-keynote-workshop.md)
> **日期**: 2025-12-13
> **调度者**: Team Leader
> **执行者**: DocUIClaude, DocUIGPT
> **状态**: ✅ 已完成

---

## 实施结果

| # | 建议 | 执行者 | 状态 |
|---|------|--------|------|
| 1 | 术语注册表 (SSOT) | DocUIClaude | ✅ 完成 |
| 2 | Render → Context-Projection | DocUIClaude | ✅ 完成 |
| 3 | 引入 Capability-Provider | DocUIClaude | ✅ 完成 |
| 9 | 统一简称风格 | DocUIGPT | ✅ 完成 |
| 10 | 术语引用规则 | DocUIGPT | ✅ 完成 |
| 11 | 3 层模型 Mermaid 图 | DocUIGPT | ✅ 完成 |
| 12 | 层级关系澄清 | DocUIGPT | ✅ 完成 |

**暂缓**（4, 5, 6, 7, 8）— 人类伙伴有额外信息待补充。

---

## DocUIClaude 任务

### 任务 1: 创建术语注册表 Key-Note

**文件**: `DocUI/docs/key-notes/glossary.md`（新建）

**格式要求**（人类指定）：
- **不用 Markdown 表格**
- 用 **术语标题 + 特性 list** 格式
- 结构更灵活，key 和 value 更近

**示例格式**:
```markdown
### Agent
- **定义**: 能感知环境、为达成目标而行动、并承担行动后果的计算实体
- **别名**: -
- **非目标**: 不是简单的自动化脚本
- **实现映射**: -
```

**术语列表**（至少包含）:
- Agent, Environment, Agent-OS, LLM
- Observation, Action, Tool-Call, Thinking
- History, HistoryEntry, History-View
- Context-Projection (原 Render)
- Capability-Provider (新引入)
- Window, Notification
- App-For-LLM, Built-in

### 任务 2: 重命名 Render → Context-Projection

**文件**: `DocUI/docs/key-notes/llm-agent-context.md`

**改动**:
- 将 `## Render` 章节重命名为 `## Context-Projection`
- 更新定义，添加输入/输出契约
- 标注 "Render" 为弃用术语

### 任务 3: 引入 Capability-Provider

**文件**: `DocUI/docs/key-notes/app-for-llm.md`

**改动**:
- 将 `## TODO` 替换为 `## Capability-Provider（能力提供者）`
- 定义 Built-in 和 App-For-LLM 的区别
- 明确 "App" 简称仅指外部扩展

---

## DocUIGPT 任务

### 任务 9: 统一简称风格

**文件**: `DocUI/docs/key-notes/app-for-llm.md`

**改动**:
- 添加术语约束规则
- 明确 App 仅指 App-For-LLM
- 统称使用 Capability-Provider

### 任务 10: 术语引用规则

**文件**: `DocUI/docs/key-notes/key-notes-drive-proposals.md`

**改动**:
- 添加 "术语引用规则" 章节
- 指定 glossary.md 为 SSOT
- 规定其他文档的引用方式

### 任务 11: 3 层模型 Mermaid 图

**文件**: `DocUI/docs/key-notes/llm-agent-context.md`

**改动**:
- 在 `## LLM调用的3层模型` 章节添加两张 Mermaid 图
- 结构视图 + 调用序列图
- 使用 Context-Projection 术语

### 任务 12: 层级关系澄清

**文件**: `DocUI/docs/key-notes/doc-as-usr-interface.md`

**改动**:
- 添加 Window/Notification/History-View 层级关系图
- 添加术语说明
- 先解决有无问题，细节后续迭代

---

## 执行顺序建议

1. **DocUIClaude 先执行**（建立 glossary.md 和核心术语改名）
2. **DocUIGPT 后执行**（依赖 glossary.md 和 Context-Projection 术语）

---

*创建时间: 2025-12-13*
