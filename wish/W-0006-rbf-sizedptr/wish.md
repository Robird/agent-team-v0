---
wishId: "W-0006"
title: "修订 RBF 设计稿：引入/改用 SizedPtr"
status: Abandoned
owner: "AI Team"
created: 2026-01-04
updated: 2026-01-06
abandoned: 2026-01-06
abandoned_reason: "审阅方法论失败：Craftsman 陷入学术论文审阅模式，产生大量伪问题（证据链、可追溯性等形式要求），偏离工程交付目标"
tags: [rbf, design, migration]
produce:
  # 外部产物文档
  - "atelia/docs/Rbf/rbf-interface.md"
  - "atelia/docs/Rbf/rbf-format.md"
  - "agent-team/experiment/2026-01-04-sizedptr-wish0004-reviewloop/2026-01-04-wish-0004-sizedptr-simulation.md"
  # 本实例内部文档
  - "wish/W-0006-rbf-sizedptr/project-status/snapshot.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Resolve.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Shape.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Rule.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Plan.md"
  - "wish/W-0006-rbf-sizedptr/artifacts/Craft.md"
---

# Wish: 修订 RBF 设计稿：引入/改用 SizedPtr

> **一句话动机**: 让 RBF/StateJournal 在需要表达"区间（offset+length）"时使用 SizedPtr，从而减少重复设计与类型漂移，并提升跨层复用。

## 目标与边界

**目标 (Goals)**:
- [ ] 审阅并修订 RBF 相关设计文档，使其能引用 `Atelia.Data.SizedPtr` 表达"4B 对齐的 byte range"
- [ ] 明确区分两类语义：
  - **Address64 / Ptr64**：指向某个 Frame 起始位置的 file offset（RBF 层既有语义）
  - **SizedPtr**：表达一个 span/range（offset+length）的紧凑表示（通用产品）
- [ ] 在 `rbf-interface.md` 与 `rbf-format.md` 中明确哪些字段/接口应继续使用 Address64，哪些应改用 SizedPtr
- [ ] 为后续实际实现迁移提供清晰的 Decision Log

**非目标 (Non-Goals)**:
- 不在本 Wish 中实现 RBF 的代码（当前 active code 里尚无完整 RBF 实现）
- 不在本 Wish 中强制把 Address64 完全删除或替换为 SizedPtr
- 不强制为 SizedPtr 引入 Null/Empty 等语义

## 验收标准 (Acceptance Criteria)

- [x] `atelia/docs/Rbf/rbf-interface.md` 修订完成（v0.18）
- [x] `atelia/docs/Rbf/rbf-format.md` 修订完成（v0.17）
- [x] 新增 Decision Log 记录"为什么/如何引入 SizedPtr"的 tradeoff（Resolve.md §6-7）

## 层级进度 (Layer Progress)

| Artifact Tier | 状态 | 产物链接 | 备注 |
|:--------------|:-----|:---------|:-----|
| Resolve-Tier | 🟢 完成 | [artifacts/Resolve.md](artifacts/Resolve.md) | 动机、现状问题、Scope + 监护人决策澄清 |
| Shape-Tier | 🟢 完成 | [artifacts/Shape.md](artifacts/Shape.md) | 术语对齐、Interface Contract |
| Rule-Tier | 🟢 完成 | [artifacts/Rule.md](artifacts/Rule.md) | NullPtr 定义、Address64 废弃 |
| Plan-Tier | 🟢 完成 | [artifacts/Plan.md](artifacts/Plan.md) | 修订计划 + Migration Notes |
| Craft-Tier | 🟢 完成 | [修订后的文档](../../atelia/docs/Rbf/) | Phase 1 文档修订已完成 |
| Review-Tier | 🔴 失败 | [审阅报告](../../agent-team/handoffs/w0006-review-*.md) | **审阅方法论失败，产生大量伪问题** |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞/失败 | ➖ N/A  
> **技术交付**：✅ 2026-01-05（文档修订完成）  
> **质量审阅**：❌ 2026-01-06（方法论失败，Wish 放弃）  
> **Phase 2 计划**：代码实现（留给未来 Wish，当前无 active code）

## 关联 Issue

见：[artifacts/Resolve.md](artifacts/Resolve.md) frontmatter（已全部 resolved）

## 放弃原因 (Abandonment Reason)

**技术交付已完成**（2026-01-05）：
- ✅ 文档修订：rbf-interface.md v0.17→v0.18, rbf-format.md v0.16→v0.17
- ✅ 5 个 Tier 文档产出：Resolve/Shape/Rule/Plan/Craft
- ✅ SizedPtr 完全替代 Address64

**审阅方法论失败**（2026-01-06）：
- Craftsman 审阅产生 36 个问题（18 文档/措辞 + 18 设计/工程）
- **核心问题**：陷入"学术论文审阅模式"，过度关注形式（证据链、可追溯性、历史可复核），偏离工程目标（代码/文档正确性）
- **典型伪问题示例**：
  - E1："Resolve 引用的 rbf-interface.md 已被修订为 SizedPtr，证据不可复核" → **倒果为因**：文档被修订正是 Wish 成功的证据
  - E2："256MB 足够需要追溯之前在哪里分析过" → **过度要求**：监护人决策即为依据
  - 大量"链接路径""措辞一致性""表述演进"等抠字眼问题
- **结论**：伪问题增长速度远超真问题，审阅成本 >> 价值

**监护人决策**：放弃本 Wish，RBF 文档修订成果保留（技术上正确），另寻更有效的质量保证方法。

## 背景

Wish-0004 已在 `Atelia.Data` 中实现 `SizedPtr` 并通过单元测试。在模拟执行过程中也暴露出"文档层术语混用"的风险：
- `Address64/Ptr64` 是 file offset（指向 Frame 起点）
- `SizedPtr` 是 span（offset+length）

RBF 作为目标用户之一，应在其设计稿中复用 SizedPtr，而不是在 RBF 层重复发明另一套 span 表达。

## 变更日志 (Change Log)

| 日期 | 执行者 | 变更 | 原因 |
|:-----|:-------|:-----|:-----|
| 2026-01-04 | AI Team | 创建 | 来自 Wish-0004 的后续工作拆分 |
| 2026-01-05 | Implementer | 迁移到实例目录 | W-0005 试点迁移 |

