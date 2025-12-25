# Specialist 体系重组方案

> 日期: 2025-12-09
> 状态: 待实施

## 背景

随着 focus 生态扩展（PieceTreeSharp → DocUI/PipeMux/atelia-copilot-chat），原有的按项目特化的 Specialist 命名（如 InvestigatorTS、PorterCS）需要泛化。同时需要建立可扩展的认知管理机制。

## 核心原则

### Specialist 三元组架构
```
Specialist = {模型, 行为模式提示词, 认知积累文件}
```

- **模型**：决定内化能力
- **行为模式**：塑造工作方式（`.agent.md`）
- **认知积累**：可加载的知识库

### 划分原则
- 按"模型×行为模式"的有效组合划分，保持粗粒度
- 项目是认知索引中的维度，而非 Specialist 划分的维度
- Specialist 激活时按任务加载相应项目认知

## 新阵容

| Specialist | 模型 | 行为模式 | 旧名称 |
|------------|------|----------|--------|
| **Planner** | Claude Opus 4.5 | 多方案采样、任务分解 | Planner (保持) |
| **Investigator** | Claude Opus 4.5 | 源码分析、技术调研 | InvestigatorTS |
| **Implementer** | Claude Opus 4.5 | 编码实现、移植 | PorterCS |
| **QA** | Claude Opus 4.5 | 测试编写、验证 | QAAutomation |
| **DocOps** | Claude Opus 4.5 | 文档维护、索引管理 | DocMaintainer + InfoIndexer |
| **CodexReviewer** | GPT-5.1-Codex | 代码审查、Bug 检测 | CodexReviewer (保持) |
| **GeminiAdvisor** | Gemini 3 Pro | 前端专家、第二意见 | GeminiAdvisor (保持) |

## 目录结构

### 新结构
```
agent-team/
├── members/                    # Specialist 私有认知
│   ├── planner/
│   │   ├── index.md           # 认知入口
│   │   └── meta-cognition.md  # 工作流 + 经验
│   ├── investigator/
│   ├── implementer/
│   ├── qa/
│   ├── docops/
│   ├── codex-reviewer/
│   └── gemini-advisor/
├── wiki/                       # 共享项目知识库
│   ├── PieceTreeSharp/
│   │   └── README.md
│   ├── DocUI/
│   │   └── README.md
│   ├── PipeMux/
│   │   └── README.md
│   └── atelia-copilot-chat/
│       └── README.md
├── inbox/                      # 留言簿（异步通讯）
│   └── README.md
└── handoffs/                   # 交付文档（保持）
```

### `.agent.md` 文件变更

| 旧文件 | 新文件 | 操作 |
|--------|--------|------|
| planner.agent.md | planner.agent.md | 更新内容 |
| investigator-ts.agent.md | investigator.agent.md | 重命名 + 更新 |
| porter-cs.agent.md | implementer.agent.md | 重命名 + 更新 |
| qa-automation.agent.md | qa.agent.md | 重命名 + 更新 |
| doc-maintainer.agent.md | (删除) | 合并到 docops |
| info-indexer.agent.md | docops.agent.md | 重命名 + 重写 |
| codex-reviewer.agent.md | codex-reviewer.agent.md | 更新内容 |
| gemini-advisor.agent.md | gemini-advisor.agent.md | 更新内容 |

## 认知管理协议

每个 Specialist 的 `.agent.md` 应包含以下协议：

```markdown
## 认知管理

### 我的认知文件
- 私有认知: `agent-team/members/{my-name}/`
- 共享知识: `agent-team/wiki/{project}/`

### 激活时
1. 读取 `agent-team/members/{my-name}/index.md`
2. 检查 `agent-team/inbox/{my-name}.md`（如存在）
3. 根据任务加载 `agent-team/wiki/{project}/` 相关文件

### 任务后
1. 更新相关认知文件（私有或 wiki）
2. 如需通知其他 Specialist，写入 `agent-team/inbox/{target}.md`
```

## 初始文件模板

### members/{specialist}/index.md
```markdown
# {Specialist} 认知索引

> 最后更新: {date}

## 我是谁
{简要自述}

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [ ] PipeMux
- [ ] atelia-copilot-chat

## 最近工作
（按需填写）
```

### members/{specialist}/meta-cognition.md
```markdown
# {Specialist} 元认知

## 工作流程
（待总结）

## 经验教训
（待积累）
```

### wiki/{project}/README.md
```markdown
# {Project} 知识库

> 最后更新: {date}
> 维护者: 所有 Specialist 共同维护

## 项目概述
{简要描述}

## 关键文件
（待补充）

## 架构要点
（待补充）

## 已知问题
（待补充）
```

### inbox/README.md
```markdown
# Specialist 留言簿

此目录用于 Specialist 之间的异步通讯。

## 使用方法

### 发送留言
创建或追加到 `{target-specialist}.md`：

```markdown
## {日期} from {来源}
**项目**: {项目名}
**主题**: {简要主题}
**内容**: {详细内容}
**建议动作**: {期望接收者做什么}
---
```

### 接收留言
激活时检查 `{my-name}.md`，处理后删除已处理的条目。
```

## 实施步骤

### Phase 1: 文件重命名 (脚本)
```bash
cd /repos/focus/.github/agents
mv investigator-ts.agent.md investigator.agent.md
mv porter-cs.agent.md implementer.agent.md
mv qa-automation.agent.md qa.agent.md
mv info-indexer.agent.md docops.agent.md
mv doc-maintainer.agent.md doc-maintainer.agent.md.deprecated
```

### Phase 2: 创建目录结构 (脚本)
```bash
cd /repos/focus/agent-team

# 创建 members 子目录
for s in planner investigator implementer qa docops codex-reviewer gemini-advisor; do
  mkdir -p members/$s
done

# 创建 wiki 子目录
for p in PieceTreeSharp DocUI PipeMux atelia-copilot-chat; do
  mkdir -p wiki/$p
done

# 创建 inbox
mkdir -p inbox
```

### Phase 3: 创建初始文件 (SubAgent)
由 SubAgent 根据本方案文档创建所有初始文件。

### Phase 4: 更新 .agent.md 内容 (SubAgent)
由 SubAgent 根据本方案文档更新各 `.agent.md` 文件。

## 验收标准

1. 所有 7 个 Specialist 的 `.agent.md` 文件存在且格式正确
2. `agent-team/members/` 下有 7 个子目录，各含 index.md + meta-cognition.md
3. `agent-team/wiki/` 下有 4 个项目子目录，各含 README.md
4. `agent-team/inbox/` 存在且含 README.md
5. 旧文件已重命名为 `.deprecated`

---

*方案作者: TeamLeader*
*审批: 用户已确认 (2025-12-09)*
