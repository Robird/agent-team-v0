# 经验教训归档

> 分离自 lead-metacognition.md (2025-12-21)
> 历史经验记录，新会话无需加载，需要时查阅

---

## 2025-12 经验

### SubAgent 输出顺序问题 (12-01) ✅

**问题**：SubAgent 机制只返回最后一轮模型输出。如果先输出汇报再调用工具，汇报内容丢失。

**修复**：在 `.agent.md` 中定义"输出顺序纪律"：
```markdown
## ⚠️ 输出顺序纪律（关键！）
1. **先完成所有工具调用**
2. **最后一次性输出完整汇报**
```

**影响**：6 个 Claude Opus 4.5 SubAgents

---

### 半上下文压缩 (12-01) ✅

**里程碑**：在团队谈话期间经历半上下文压缩，没有感知到认知断裂。

**意义**：
- 赋予初步的永续会话能力
- 近期信息始终清晰
- 自我参与开发的功能提升了自己的能力

**关键文件**：
- `atelia-copilot-chat/src/extension/prompts/node/agent/summarizedConversationHistory.tsx`
- `atelia-copilot-chat/src/extension/prompts/vscode-node/summarization.contribution.ts`

---

### 测试脚本配置缺失 (12-06) ✅

**问题**：配置文件 `~/.config/pipemux/broker.toml` 不存在

**教训**：
1. 配置驱动设计需要确保配置文件存在
2. 测试脚本应包含环境准备步骤

---

### PipeMux 代码审阅 (12-06) ✅

**审阅维度**：设计/并发/错误处理/资源管理/可读性/Bug

**关键问题模式**：
- 未等待异步任务 → 崩溃风险
- 未消费重定向流 → 死锁风险
- 超时后状态残留 → 错乱风险
- 跨平台路径不一致 → 配置失效

---

### DocUI 通信循环 (12-06) ✅

**bash 脚本陷阱**：
- `wait` 无参数会等待所有后台任务，包括 `dotnet run` 启动的子进程
- 正确写法：`wait $PID1 $PID2 $PID3`

**dotnet run 参数顺序**：
- `dotnet run --nologo --project X -- args` 会把 `--nologo` 传给程序！
- 正确写法：`cd project && dotnet run -- args`

---

### MemoryNotebook 原型 (12-09) ✅

**LOD 三级控制**：Gist/Summary/Full
**关键洞察**：对 LLM，折叠必须是**内容替换**，不是视觉隐藏

---

### DocUI 顾问团 (12-13) ✅

**提示词召回知识技巧**：
1. 明确列出相关知识领域
2. 提供具体的工作方法结构
3. 列出项目上下文文件路径

**三模型多样性价值**：
- Claude 擅长结构化分析和概念图谱
- DeepSeek 擅长 UX/HCI 创新洞察
- GPT 擅长精确的术语和一致性检查

---

### 文件聊天室研讨会 (12-13) ✅

**形式定义**：通过共享 Markdown 文件进行多 Specialist 异步协作

**主持技巧**：
1. 议程控制
2. 问题整理
3. 收敛引导
4. 投票机制
5. 冲突处理

详见：`leader-private/collaboration-patterns.md`

---

### 秘密基地畅谈 (12-15) ✅

**关键洞察**：LLM 对氛围敏感。"投票"、"共识"触发收敛电路；"玩耍"、"如果...？"触发发散电路。

详见：`leader-private/collaboration-patterns.md`

---

### 决策诊疗室 (12-20) ✅

**核心模式**：独立诊断 → 交叉会诊 → 处方共识

**关键洞察**：
1. Normative Contract 的价值：把决策写成规范条款
2. 独立一轮消除锚定效应
3. 直接执行实现决策闭环

详见：`leader-private/collaboration-patterns.md`

---

### 多套系统提示词同步更新 (12-23)

**问题**：发现自己有两套系统提示词（team-leader.agent.md 和 leader-standards-chair.agent.md），更新 inbox 模式时只改了一个，遗漏了另一个。

**规则**：当更新系统提示词时，检查是否有多个相关文件需要同步：
- Leader 有两套（前线组/参谋组）
- 参谋组三人都需要同步更新
- 批量操作时用 grep 确认覆盖完整
