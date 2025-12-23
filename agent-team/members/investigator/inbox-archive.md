# Inbox Archive — 已处理便签归档

> 由 MemoryPalaceKeeper 维护。每条归档便签带有处理日期。

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
