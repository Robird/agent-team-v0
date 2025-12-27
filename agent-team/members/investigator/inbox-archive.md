# Inbox Archive — 已处理便签归档

> 由 MemoryPalaceKeeper 维护。每条归档便签带有处理日期。

---

## 归档 2025-12-26 (处理于 2025-12-27)

### 原便签: 2025-12-26 17:30 — `_removedFromCommitted` 调查洞见

调查 `_removedFromCommitted` 时的发现：

1. **设计权衡的洞见**：双字典策略（`_committed` + `_working`）的核心约束是"_committed 在 Commit 前只读"。这个约束带来了 Commit 失败时恢复简单的好处，但代价是需要额外的 `_removedFromCommitted` 集合来追踪删除意图。

2. **规范与实现的契合点**：`[S-WORKING-STATE-TOMBSTONE-FREE]` 条款要求 Working State 不存储 tombstone。当前实现用 `_removedFromCommitted` 集合（而非 tombstone 值）满足了这个约束——这是一个巧妙但隐晦的设计。

3. **监护人意见的精确定位**：监护人的意见针对的是 Load/Materialize 阶段，但实际问题在运行时状态管理。加载时 `_committed` 确实是最终状态，`_removedFromCommitted` 只在运行时填充。

4. **替代设计的可行性**：如果改为单一 `_current` 字典（合并视图），可以消除 `_removedFromCommitted`，但需要重构读写路径和 Commit 失败恢复逻辑。

**处理结果**: → MERGE 到 index.md Session Log 2025-12-26 条目（深层洞见补充）

---

## 归档 2025-12-24 (处理于 2025-12-25)

### 原便签: 2025-12-24 15:30 — mvp-design-v2.md 历史决策引用分析

完成 mvp-design-v2.md 历史决策引用分析。

**关键发现**：
- Qxx 引用共 15 处，涉及 13 个不同决策（Q3/Q7-Q11/Q13-Q19/Q22-Q23）
- "方案 X" 引用全部是"方案 C"（双字典），共 8 处
- 方案 C 出现在术语表、实现描述、伪代码三个层面

**潜在问题**：
- 部分引用没有 "=Y" 后缀（如 Q15、Q17），需要查 decisions 才知道选了什么
- 方案 C 没有对应的 Qxx 编号，是独立的实现方案选择

**交付**: handoffs/2025-12-24-mvp-design-v2-decision-refs-INV.md

**处理结果**: → APPEND 到 index.md Session Log + Key Deliverables

---

## 归档 2025-12-24 (处理于 2025-12-24)

### 原便签: 2025-12-24 09:30 — RBF v0.12 格式变更对上层文档的影响

完成了 mvp-design-v2.md 对 RBF v0.12 变更的适配。关键发现：

1. **墓碑机制分离**：旧设计中 `FrameTag=0x00000000 (Padding)` 承担墓碑语义，现在墓碑完全由 `FrameStatus=0xFF (Tombstone)` 承载。这是职责分离的体现——FrameStatus 管 Layer 0 帧有效性，FrameTag 管 Layer 1 业务分类。

2. **StateJournal 处理顺序变更**：上层 Reader 现在 MUST 先检查 `FrameStatus`，再解释 `FrameTag`。这比之前"检查 FrameTag=0 就跳过"更清晰。

3. **mvp-test-vectors.md 无需更新**：它正确地将 Layer 0 测试委托给 rbf-test-vectors.md，Layer 1 测试不涉及帧格式细节。

4. **术语演化轨迹**：Magic → Fence (v0.10) → Pad → FrameStatus (v0.12)。每次重命名都反映了更精确的语义理解。

**处理结果**: → APPEND 到 index.md Session Log

---

## 归档 2025-12-24 (处理于 2025-12-24)

### 原便签: 2024-12-24 10:30 — 术语别名调研任务完成

术语别名调研任务完成。StateJournal 目录下的弃用/别名术语使用情况良好：
- 所有出现均在术语表定义或正式术语旁的注释说明中
- 没有发现需要替换的"误用"情况
- archived 文件夹中的历史文档包含相同的定义，无需处理

**处理结果**: → APPEND 到 index.md Session Log（State-Update）

---
