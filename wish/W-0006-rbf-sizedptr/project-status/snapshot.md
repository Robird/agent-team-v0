---
docId: "W-0006-snapshot"
title: "W-0006 Snapshot"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
---

# W-0006 Snapshot — 执行寄存器

> **最终状态**：❌ **Abandoned**（2026-01-06）  
> **技术交付**：✅ 完成（2026-01-05）  
> **质量审阅**：❌ 失败（方法论问题）

---

## 放弃总结 (Abandonment Summary)

### 技术成果（已完成）

**文档修订**：
- `rbf-interface.md` v0.17 → v0.18
- `rbf-format.md` v0.16 → v0.17
- SizedPtr 完全替代 Address64
- 2 个文档修订耗时 0.5 小时

**产物清单**：
- 4 个 Artifact-Tiers 文档（Resolve/Shape/Rule/Plan）
- 3 个调查报告（Resolve Brief、Address64 价值检查、执行记录）
- 2 个修订后的 RBF 文档

**时间线**：
- 2026-01-05 18:00 — 畅谈会（Scope/Approach）
- 2026-01-05 21:00 — Resolve-Tier 完成
- 2026-01-05 22:30 — Shape/Rule-Tier 完成
- 2026-01-05 23:30 — Plan-Tier 完成
- 2026-01-06 00:00 — Craft-Tier 完成（文档修订）
- **总耗时：~4.5 小时**

### 审阅失败（方法论问题）

**审阅过程**：
- 2026-01-06 01:00 — Craftsman 5 轮审阅（Resolve/Shape/Rule/Plan/跨层）
- **产生问题**：36 个（18 文档/措辞 + 18 设计/工程）
- 2026-01-06 02:00 — Curator 分类处理
- 2026-01-06 03:00 — Curator 修订文档/措辞类问题（15 个）
- **总耗时：~3 小时**

### 失败原因分析

#### 核心问题：审阅模式错配

| 维度 | Craftsman 的做法 | 应该的做法 | 后果 |
|:-----|:-----------------|:-----------|:-----|
| **目标** | 学术论文审阅 | 工程交付审阅 | 关注点偏离 |
| **焦点** | 证据链、可追溯性、历史可复核 | 当前状态是否正确 | 产生大量伪问题 |
| **标准** | 文档形式完美、逻辑闭环 | 代码可编译、文档可用 | 过度要求 |
| **结果** | 36 个问题，12+ 个伪问题 | 应该 < 10 个真问题 | 审阅成本 >> 价值 |

#### 典型伪问题示例

**E1（Sev1）**："Resolve 引用的 rbf-interface.md 已被修订为 SizedPtr，证据不可复核"
- **荒谬之处**：文档被修订**正是 Wish 成功的证据**，却被当成"证据缺失"
- **倒果为因**：Resolve 说"要改 Address64→SizedPtr"，我们改了，Craftsman 说"现在看不到 Address64 了，证据缺失"

**E2（Sev2）**："256MB 足够需要追溯之前在哪里分析过"
- **过度要求**：监护人说了就是了，还要考古找"分析过程"的出处？
- **本末倒置**：关注"谁说的"而非"说得对不对"

**大量文档/措辞问题**（18 个）：
- "链接路径 `meeting/` 应改为 `../meeting/`"（D1）
- "§1 表述与 §6 结论程度差异"（D2）— 认知演进被当成"矛盾"
- "Value vs Offset 术语表达"（D4）— 纯粹抠字眼

#### 伪问题增长机制

1. **审阅产生问题** → Craftsman 找出 36 个问题
2. **分类处理** → Curator 分为文档/设计两类
3. **修订文档** → 修订后又产生新的"可挑刺点"
4. **无限循环** → 永远有新字眼可抠

**结论**：抠字眼是没有尽头的，这种审阅方式不可持续。

### 监护人决策

**放弃 W-0006**（2026-01-06）：
- RBF 文档修订成果**保留**（技术上正确，已达成原定目标）
- 5 个 Tier 文档**保留**（作为方法论实验记录）
- 审阅报告**归档**（作为失败案例）

**教训总结**：
1. **审阅目标**：应关注"当前状态是否正确"，而非"历史记录是否完美"
2. **审阅标准**：应以"代码可编译、文档可用"为准，而非"形式完美、逻辑闭环"
3. **伪问题识别**：如果修复一个问题不影响交付质量，那可能是伪问题
4. **成本控制**：审阅时间不应超过实现时间（本次 3h 审阅 vs 4.5h 实现）

**下一步**：另寻更有效的质量保证方法（待定）。

---

## 遗留产物清单

### 保留（有价值）

- ✅ `atelia/docs/Rbf/rbf-interface.md` v0.18
- ✅ `atelia/docs/Rbf/rbf-format.md` v0.17
- ✅ `wish/W-0006-rbf-sizedptr/artifacts/*.md`（方法论实验记录）

### 归档（失败案例）

- 📦 `agent-team/handoffs/w0006-review-*.md`（36 个问题，12+ 伪问题）
- 📦 `agent-team/handoffs/w0006-review-doc-issues.md`（18 个文档/措辞问题）

### 废弃（无价值）

- ❌ Craftsman 审阅结果（方法论失败）
- ❌ 18 个"设计/工程问题"中的 12+ 伪问题

**W-0006 Phase 1（文档修订）已于 2026-01-05 完成。**

### 核心成果

1. **SizedPtr 完全替代 Address64**
   - Address64 类型及其 3 条相关条款完全移除
   - SizedPtr 成为 RBF Interface 层的核心 Frame 句柄类型

2. **NullPtr 约定定义**
   - RBF 层定义：`NullPtr = default(SizedPtr)`
   - 表示"无效的 Frame 引用"（业务语义）

3. **文档修订完成**
   - `rbf-interface.md` v0.17 → v0.18
   - `rbf-format.md` v0.16 → v0.17
   - 添加导航锚点指向 artifacts/

4. **产出物清单**
   - 4 个 Artifact-Tiers 文档（Resolve/Shape/Rule/Plan）
   - 3 个调查报告（Resolve Brief、Address64 价值检查、执行记录）
   - 2 个修订后的 RBF 文档

### 方法论验证

**team-leader-mental-model-v0.1.md 首次完整应用**：

| 原则 | 应用 | 结果 |
|:-----|:-----|:-----|
| **demand 结构化** | 每个 Tier 都有 action/scope/deliverable/DoD/stop_condition | ✅ 指令可执行、可终止 |
| **识别问题即产出** | Resolve识别4问题→监护人澄清→Shape/Rule简化 | ✅ 避免过度设计 |
| **review-to-closure** | Curator 审阅每个 Tier | ✅ 30秒可判性保证 |
| **snapshot 执行寄存器** | 实时更新焦点/下一步/阻塞 | ✅ 全程可追溯 |
| **监护人输入价值** | 4 条澄清直接解答所有疑问 | ✅ 跳过大量探索成本 |

### 进展类型统计

按方法论 §3 分类：

- ✅ **推进功能实现**：5 个 Tier 全部完成
- ✅ **发现并记录问题**：4 个 issues 识别并 resolved
- ✅ **问题解决**：Address64 存在价值调查完成

### Phase 2 计划

**代码实现**（留给未来 Wish）：
- 当前 atelia 仓库无 active RBF 代码
- Plan.md §6 已标注待确认项（StateJournal、测试向量）
- 实现时参考 Plan.md §4 Migration Notes

---

## 时间线 (Timeline)

| 时间 | Tier | 动作 | 产出 | 耗时 |
|:-----|:-----|:-----|:-----|:-----|
| 18:00 | 前置 | 畅谈会（Scope 与实施路径） | meeting/2026-01-05-scope-and-approach.md | 1h |
| 21:00 | Resolve | Investigator 调研 + Seeker 产出 | Resolve.md（LGTM） | 1h |
| 22:00 | Resolve | 监护人决策澄清 | Resolve.md §6-7 | 0.5h |
| 22:30 | Shape+Rule | Address64 调查 + 产出 | Shape.md + Rule.md（LGTM） | 1h |
| 23:30 | Plan | Seeker 产出修订计划 | Plan.md | 0.5h |
| 00:00 | Craft | Implementer 执行修订 | 2 个 RBF 文档 v0.18/v0.17 | 0.5h |

**总耗时**：~4.5 小时（从畅谈会到文档修订完成）

---

## 关键洞察

### K1: 监护人输入的指数级价值

4 条决策澄清节省了至少数天的探索时间：
- 避免了"Address64 vs SizedPtr 共存策略"的过度设计
- 直接确认了 SizedPtr 在 RBF 中的核心地位
- 明确了 Address64 已无存在价值（可完全移除）

### K2: "识别问题即产出"的威力

Resolve-Tier 识别的 4 个"问题"实际是"已有决策需要文档化"，这避免了：
- 重复发明轮子
- 猜测设计意图
- 过度分析瘫痪

### K3: demand 结构化使指令可终止

每个 Tier 的 `stop_condition` 避免了无限扩写/探索：
- Resolve: 问题已登记
- Shape/Rule: 核心产物已完成
- Plan: 修订清单可执行
- Craft: grep 验证通过

---

## 遗留事项 (Future Work)

1. **rbf-test-vectors.md** 待更新（Plan.md §6 标注）
2. **StateJournal** 文档待同步修订（Out-of-Scope，另一个 Wish）
3. **Phase 2 代码实现**（当有 active code 时启动）


