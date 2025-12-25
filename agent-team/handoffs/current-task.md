# 任务: 起草 StateJournal MVP 实施计划 v0.1

## 元信息
- **任务 ID**: T-20251225-02
- **类型**: 起草
- **优先级**: P0
- **预计时长**: 45-60 分钟
- **状态**: ✅ 已完成
- **完成时间**: 2025-12-25
- **结果文件**: [task-result.md](task-result.md)

---

## 背景

T-20251225-01 审计完成，确认规范质量高（80 条款，5 阶段依赖图）。现在需要将审计结果转化为可执行的实施计划。

---

## 目标

创建 `atelia/docs/StateJournal/implementation-plan.md`，包含：

1. **实施阶段定义**：基于审计的 Phase 0-5 划分
2. **任务清单**：每个 Phase 的具体任务，粒度为 1-4 小时
3. **runSubagent 调用模板**：每个任务的 Implementer 调用参数
4. **验收标准**：每个任务对应的测试/验收点

---

## 输入文件

- `agent-team/handoffs/task-result.md` — 审计结果（条款清单 + 依赖图）
- `atelia/docs/StateJournal/mvp-design-v2.md` — 主规范
- `atelia/docs/StateJournal/mvp-test-vectors.md` — 测试向量（如存在）
- `atelia/docs/StateJournal/rbf-format.md` — RBF 格式规范
- `atelia/docs/StateJournal/rbf-interface.md` — RBF 接口规范

---

## 输出格式

```markdown
# StateJournal MVP 实施计划

> 版本: 0.1
> 创建: 2025-12-25
> 状态: 草案

## 1. 概述
<简要说明实施目标和方法>

## 2. 实施阶段

### Phase 1: RBF Layer 0
- 目标: ...
- 预估工时: ...
- 前置依赖: ...

### Phase 2: ...
（以此类推）

## 3. 任务清单

| 任务 ID | Phase | 名称 | 预估 | 依赖 | 条款覆盖 | 验收标准 |
|---------|-------|------|------|------|----------|----------|
| T-01 | 1 | ... | 2h | — | [F-FENCE-*] | 测试通过 |

## 4. runSubagent 调用模板

### T-01: <任务名>

（YAML 格式的调用参数）

## 5. 质量门禁

| 阶段 | 门禁条件 |
|------|----------|
| Phase 1 完成 | RBF 读写测试 100% 通过 |
| ... | ... |

## 附录: 条款-任务映射
（可选：反向索引，便于追踪）
```

---

## 约束

- **篇幅控制**：如果某个 Phase 内容过多，可以拆分到独立文件（如 `implementation-plan-phase1.md`）
- **粒度**：每个任务 1-4 小时，可独立验收
- **测试优先**：每个任务必须有对应的验收标准

---

## 执行方式

建议：
1. 先根据审计结果的 Phase 划分，列出每个 Phase 的任务
2. 为每个任务填写条款覆盖和验收标准
3. 编写 runSubagent 调用模板（可以先写 2-3 个示例，后续补充）

如果发现规范有遗漏，可以在报告中记录，不必立即修复。

---

## 完成标准

- [ ] 创建 `implementation-plan.md` 文件
- [ ] 包含 Phase 1-5 的任务划分
- [ ] 至少有 10 个具体任务定义
- [ ] 至少有 3 个 runSubagent 调用模板示例
- [ ] 结果写入 `task-result.md`

---

## 备注

这是实施计划的 v0.1 草案，后续会通过畅谈会审阅和迭代。不必追求完美，重点是：

1. **结构完整**：覆盖所有 Phase
2. **示例充分**：让 Implementer 看懂怎么用
3. **可迭代**：留出修订空间
