# workspace_info 机制调查报告

> **调查日期**: 2026-01-01
> **调查员**: Investigator
> **任务来源**: 监护人 via TeamLeader

## 调查摘要

`workspace_info` 是 VS Code Copilot Chat 扩展在 Agent 模式下自动注入的上下文组件，用于向 LLM 提供工作区结构信息。该机制通过 `GlobalAgentContext` 组件在对话首轮渲染，并缓存以避免重复计算。

## 源码位置

| 组件 | 文件路径 |
|:-----|:---------|
| GlobalAgentContext | `atelia-copilot-chat/src/extension/prompts/node/agent/agentPrompt.tsx` |
| MultirootWorkspaceStructure | `atelia-copilot-chat/src/extension/prompts/node/panel/workspace/workspaceStructure.tsx` |
| visualFileTree | `atelia-copilot-chat/src/extension/prompts/node/panel/workspace/visualFileTree.ts` |
| WorkspaceFoldersHint | `atelia-copilot-chat/src/extension/prompts/node/agent/agentPrompt.tsx` |

## 核心发现

### 1. workspace_info 的组成

`workspace_info` 是一个 XML Tag，包含三个子组件：

```tsx
<Tag name='workspace_info'>
  <AgentTasksInstructions availableTools={...} />
  <WorkspaceFoldersHint />
  <AgentMultirootWorkspaceStructure maxSize={2000} excludeDotFiles={true} availableTools={...} />
</Tag>
```

- **AgentTasksInstructions**: 列出可执行的 VS Code Tasks
- **WorkspaceFoldersHint**: 列出工作区根文件夹列表
- **AgentMultirootWorkspaceStructure**: 生成目录树结构

### 2. 目录树生成算法 (visualFileTree.ts)

**核心参数**：
- `maxSize`: 最大字符数限制（默认 **2000**）
- `excludeDotFiles`: 是否排除点文件（默认 **true**）

**算法特点**：
1. **广度优先展开**（BFS）：先展示顶层，再逐层深入
2. **智能截断**：当空间不足时自动添加 `...` 占位符
3. **排序规则**：文件在前，目录在后；同类型按名称排序
4. **过滤规则**：
   - 遵循 `.gitignore` 和 Copilot Ignore 规则
   - 排除以 `.` 开头的文件/目录（可配置）
   - 排除特定始终忽略的文件

**关键代码逻辑**：
```typescript
// 排序：文件在前，目录在后
rootNodes.sort((a, b) => {
  if (a[1] === b[1]) {
    return a[0].localeCompare(b[0]);
  }
  return a[1] === FileType.Directory ? 1 : -1;
});
```

### 3. 深度控制机制

**没有显式的深度限制**！深度由 `maxSize` 参数间接控制：

- `maxSize=2000` 表示最多 2000 个**字符**
- 广度优先意味着先填满顶层，空间足够才深入子目录
- 当剩余空间不足以显示下一项时，添加 `...` 并停止

**实际效果**：
- 小型项目：可能展示完整结构
- 中型项目：展示 2-3 级深度
- 大型项目：只展示顶层目录和部分文件

### 4. 输出格式

```
FOLDER_NAME/
	file1.ext
	file2.ext
	subdir/
		...
	...
```

- 目录以 `/` 结尾
- 子级缩进使用 `\t`（tab）
- 截断处显示 `...`

### 5. 缓存机制

workspace_info 作为 `GlobalAgentContext` 的一部分：
- 在对话首轮渲染后缓存到 `GlobalContextMessageMetadata`
- 后续轮次直接使用缓存，不重新计算
- 缓存 key 包含工作区文件夹列表、用户偏好等

### 6. 条件渲染

`AgentMultirootWorkspaceStructure` 只在 `list_dir` 工具可用时渲染：

```typescript
override async prepare(...) {
  if (!this.props.availableTools?.find(tool => tool.name === ToolName.ListDirectory)) {
    return [];
  }
  return super.prepare(sizing, progress, token);
}
```

## 监护人问题回答

### Q1: workspace_info 是如何生成的？何时包含在上下文中？

**生成方式**：
1. `GlobalAgentContext` 组件在对话首轮调用
2. `AgentMultirootWorkspaceStructure` 调用 `workspaceVisualFileTree()` 生成目录树
3. 使用文件系统服务遍历工作区，应用过滤规则，广度优先截断

**包含条件**：
- Agent 模式对话（非 Ask 模式）
- `list_dir` 工具可用
- 有打开的工作区文件夹

### Q2: 当前展示多少级目录结构？是否包含文件列表？

**深度**：没有固定深度限制，由 2000 字符预算动态决定

**包含内容**：
- ✅ 文件列表（非点文件）
- ✅ 目录列表
- ❌ 点文件（默认排除）
- ❌ .gitignore 中的文件
- ❌ Copilot Ignore 的文件

### Q3: 如果 recipe 移到根目录，workspace_info 如何展示？

由于广度优先算法：
- **recipe/** 目录名称会出现在顶层（如果空间足够）
- recipe 的子目录/文件是否展开取决于剩余空间
- 与 `agent-team/how-to/` 相比，根目录的 `recipe/` 更可能被展开（因为路径更短，节省字符）

### Q4: 有没有办法控制 workspace_info 的内容或深度？

**当前可配置项**：
- 无直接用户配置
- `maxSize` 硬编码为 2000
- `excludeDotFiles` 硬编码为 true

**扩展可能性**：
1. 修改 `AgentMultirootWorkspaceStructure` 的 `maxSize` 参数（需改源码）
2. 使用 `.copilotignore` 文件排除特定目录
3. 利用 `.gitignore` 排除不需要的内容

**建议**：如果需要让 recipe 目录更显眼：
1. 将其移到根目录（更短的路径 = 更高优先级）
2. 或在 `.copilotignore` 中排除其他大目录

## 实现建议

### 对于 recipe 可见性问题

1. **短期**：将 recipe 移到根目录可以提高其在 workspace_info 中的可见性
2. **中期**：考虑创建 `.copilotignore` 文件，排除大型但不重要的目录
3. **长期**：如果需要更精细控制，可能需要 fork 或配置 Copilot 扩展

### workspace_info 的限制

1. **无法指定必须包含的目录**
2. **无法调整深度/宽度优先级**
3. **无法自定义排序**

## 关键条款引用

| 条款 | 描述 |
|:-----|:-----|
| `maxSize=2000` | 字符预算上限 |
| `excludeDotFiles=true` | 默认排除点文件 |
| BFS 展开 | 广度优先决定展示内容 |
| 缓存首轮 | 对话中不会更新 |

---

**交付日期**: 2026-01-01
**状态**: 完成
