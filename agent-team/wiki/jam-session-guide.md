# 畅谈会指南 (Jam Session Guide)

> AI Team 研讨会的统一形式定义和组织方法。
> 本文件是 Team Leader 组织研讨会的 SSOT。

## 概述

**畅谈会 (Jam Session)** 是 AI Team 的统一协作形式，通过文件聊天室进行异步多轮讨论。

### 核心特征

- **轻松氛围**：鼓励发散思考，不需要一开始就有完美答案
- **主持人邀请制**：由 Team Leader 依次邀请各 Specialist 发言
- **多轮交叉**：每轮发言可以回应、补充、挑战前人观点
- **建设性指正**：发现问题要指出，但目的是让想法更强壮
- **可操作共识**：最终形成可执行的决议，而非空谈

## 畅谈会类型

通过标签区分不同场景：

| 标签 | 目的 | MUST 产出 | SHOULD 产出 |
|:-----|:-----|:----------|:------------|
| `#review` | 审阅现有文档 | FixList（问题+定位+建议） | 优先级排序 |
| `#design` | 探索新方案 | 候选方案 + Tradeoff 表 | 推荐选项 |
| `#decision` | 收敛分歧 | 决策条目 + 状态 | 回滚条件 |
| `#jam` | 自由畅想 | *无强制产出* | 洞察记录 |

## 组织流程

### 1. 创建聊天室文件

**位置**：`agent-team/meeting/YYYY-MM-DD-<topic>.md`

**模板**：

```markdown
# 畅谈会：<主题>

> **日期**：YYYY-MM-DD
> **形式**：畅谈会 (Jam Session)
> **标签**：#review | #design | #decision | #jam
> **主持人**：刘德智 (Team Leader)
> **参与者**：<列出参与的 Specialist>
> **状态**：进行中

---

## 背景

<说明为什么需要这次讨论，核心问题是什么>

## 讨论主题

1. <主题 1>
2. <主题 2>
3. ...

## 相关文件

- <链接到需要讨论的文件>

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

<开场白，说明讨论目标和期望产出>

---

<!-- 以下由各 Specialist 追加发言 -->
```

### 2. 邀请 Specialist 发言

使用 `runSubagent` 调用，prompt 中 MUST 包含：

```yaml
# @Advisor-Claude
taskTag: "#review"
chatroomFile: "agent-team/meeting/2025-12-21-xxx.md"
targetFiles:
  - "atelia/docs/StateJournal/mvp-design-v2.md"
appendHeading: "### Advisor-Claude 发言"
scope: "术语一致性审计，不做实现建议"
outputForm: "Markdown 要点列表"
```

### 3. 多轮交叉讨论

每轮结束后，主持人整理：
- 已达成的共识
- 仍有分歧的点
- 下一轮要讨论的焦点

然后开启下一轮邀请。

### 4. 收尾

主持人负责：
1. 整理最终共识清单
2. 列出后续行动（谁做什么）
3. 更新聊天室文件状态为"已完成"
4. 必要时更新相关项目的 backlog

## 参与者规范

### 发言格式

- **MUST** append 到文件末尾
- **MUST** 使用 `### <Name> 发言` 作为标题
- **MUST** 代码块指定语言
- **SHOULD** 引用他人观点用 `>` 块引用
- **MUST NOT** 插入到文件中间或修改他人发言

### 发言风格

- **拒绝低水平抬杠**：不要纠结细枝末节
- **鼓励死党式直言**：发现盲区必须指出
- **保持流动性**：指出问题是为了让想法更强壮
- **标注不确定性**：对不确定的建议明确标注

## 常见问题

### Q: 什么时候用 `#jam` vs `#design`？

- `#jam`：不知道要解决什么问题，先发散探索
- `#design`：问题明确，需要形成候选方案

### Q: Specialist 发言位置错了怎么办？

主持人手动修正，并在下一轮邀请时强调"MUST append 到末尾"。

### Q: 讨论无法收敛怎么办？

1. 明确记录分歧点
2. 标记为"待监护人决策"
3. 准备决策摘要供监护人快速判断

## 相关文件

- [AGENTS.md](/repos/focus/AGENTS.md) — 全局协议入口
- [lead-metacognition.md](../lead-metacognition.md) — Team Leader 核心认知
- [collaboration-patterns.md](../leader-private/collaboration-patterns.md) — 历史参考（待归档）
