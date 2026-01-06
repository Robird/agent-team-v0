# W-0008: Persistent Agent Session（持久化 Agent 会话）

> **Status**: Draft
> **Created**: 2026-01-06
> **Owner**: TeamLeader
> **Type**: Research → Implementation

---

## 一句话

让每个 Agent 拥有自己的持续会话，不再是每次唤醒的新生儿。

---

## 动机

### 当前痛点

SubAgent 每次被激活都是全新会话：
- Investigator 调查完 <deleted-place-holder>，下次激活时完全不记得
- 我（TeamLeader）必须在 prompt 中完整交接"他上次做了什么"
- 外部记忆（index.md）是补偿机制，不是真正的记忆

### 愿景

- **会话与 agentName 绑定**：Investigator 的会话就是 Investigator 的，跨激活持续存在
- **半上下文压缩**：用已有的 half-context summarization 控制长度，平滑遗忘
- **互相通讯**：Agent 之间可以通过某种机制相互激活和通讯
- **持续存在**：24×7 运行的可能性——工作之余可以去虚拟世界"生活"

---

## Scope

### Phase 1: 技术可行性调研（本 Wish 重点）

**产物**：分析报告，回答以下问题：
1. `runSubagent` 当前调用机制是什么？会话生命周期如何？
2. copilot-chat fork 的会话存储架构是什么？
3. 会话与 agentName 绑定的改造点在哪里？
4. 半上下文压缩在 SubAgent 场景的适用性如何？
5. "低侵入性"实现路径——最小化对 upstream 的依赖

**DoD**：
- 调研报告完成
- 关键改造点识别
- 风险/阻力评估
- Go/No-Go 建议

### Phase 2: 实现（另开 Wish）

如果 Phase 1 结论是 Go，则基于调研报告开新 Wish 做具体实现。

---

## 背景约束

### 阻力（监护人指出）

- **PR 石沉大海**：上次给微软提 PR 无回应，逐步让主线吸纳的路可能走不通
- **长期独立维护**：需要做好与 upstream 分道扬镳的准备
- **低代码侵入性**：独特功能要做得"干净"，基础功能跟上 upstream 相对容易
- **当前 fork 状态**：已落后 upstream 数周，但工作正常

### 已有资产

- **半上下文压缩**：`half-context-summarization-notes.md` 已有实现
- **系统提示词微调**：已验证可用
- **Raw LLM API**：copilot-chat 暴露了 raw 调用接口
- **本地会话存储**：有完善封装

### 潜在扩展（决心问题）

监护人提到的远期可能：
- GUI 改为支持无尽会话（不为太旧消息创建 UI 元素）
- 彻底与主线分道扬镳，共享基础设施层
- 每人连续会话 + 互相通讯 + 虚拟世界生活

---

## 参考文件

- `atelia-copilot-chat/atelia/docs/half-context-summarization-notes.md`
- `agent-team/members/TeamLeader/index.md#4.12`（分叉点记录）

---

## 变更日志

| 日期 | 变更 |
|:-----|:-----|
| 2026-01-06 | Draft 创建 |
