# Inbox Archive — 已处理便签归档

> 由 MemoryPalaceKeeper 维护。每条归档便签带有处理日期。

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
