# 任务: 组织实施计划审阅畅谈会

## 元信息
- **任务 ID**: T-20251225-03
- **类型**: 畅谈会
- **优先级**: P0
- **预计时长**: 30-45 分钟

---

## 背景

实施计划 v0.1 已起草完成（24 个任务，44-58h），现需组织参谋组审阅，发现潜在问题并产出 v0.2。

---

## 目标

组织一次畅谈会，邀请 Advisor-Claude 和 Advisor-GPT 审阅实施计划，产出：

1. **问题清单**：发现的任务划分、依赖、验收标准等问题
2. **改进建议**：针对每个问题的具体修改建议
3. **v0.2 修订**：根据共识直接更新 implementation-plan.md

---

## 输入文件

- `atelia/docs/StateJournal/implementation-plan.md` — 待审阅的实施计划 v0.1
- `agent-team/handoffs/task-result.md` — T-20251225-01 审计结果（参考）

---

## 畅谈会配置

```yaml
taskTag: "#review"
chatroomFile: "agent-team/meeting/2025-12-25-implementation-plan-review.md"
participants:
  - Advisor-Claude  # 任务粒度、依赖链、概念完整性
  - Advisor-GPT     # 条款对齐、验收标准可测性
```

### 审阅焦点

请参谋们重点关注：

1. **任务粒度**：1-4h 是否合适？有没有需要拆分/合并的？
2. **依赖关系**：有没有遗漏的依赖？有没有可以并行的任务？
3. **验收标准**：每个任务的验收标准是否可测试？
4. **runSubagent 模板**：3 个示例是否足够清晰？格式是否需要调整？
5. **风险点**：哪些任务可能比预估更复杂？

---

## 执行方式

按照 `agent-team/recipe/jam-session-guide.md` 组织畅谈会：

1. 创建聊天室文件
2. 写开场白
3. 依次邀请 Advisor-Claude 和 Advisor-GPT 发言
4. 汇总共识，修订 implementation-plan.md
5. 更新版本号为 v0.2

---

## 完成标准

- [ ] 创建畅谈会记录文件
- [ ] Advisor-Claude 完成发言
- [ ] Advisor-GPT 完成发言
- [ ] 汇总共识，更新 implementation-plan.md 为 v0.2
- [ ] 结果写入 `task-result.md`

---

## 备注

这是双会话自激振荡的第三次迭代！监护人扮演调度器角色，验证机制可行性。

完成后请在 Response 末尾使用标准格式请求转发：

```markdown
---
## 📤 请转发至战略层会话

{汇报内容}
```
