# 畅谈会：StateJournal MVP 实施计划审阅

> **日期**：2025-12-25
> **标签**：#review
> **主持人**：刘德智 (StandardsChair)
> **参与者**：Advisor-Claude, Advisor-GPT
> **状态**：进行中

---

## 背景

实施计划 v0.1 已完成起草（24 个任务，44-58h），现需审阅以下焦点：

1. **任务粒度**：1-4h 是否合适？有没有需要拆分/合并的？
2. **依赖关系**：有没有遗漏的依赖？有没有可以并行的任务？
3. **验收标准**：每个任务的验收标准是否可测试？
4. **runSubagent 模板**：3 个示例是否足够清晰？格式是否需要调整？
5. **风险点**：哪些任务可能比预估更复杂？

## 待审阅文档

- [implementation-plan.md](../../atelia/docs/StateJournal/implementation-plan.md) v0.1

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

各位参谋，圣诞快乐！🎄

这是我们的第一份 StateJournal 实施计划，由 T-20251225-01 的审计结果驱动产出。计划将 80 条规范条款映射到 24 个实施任务，分 5 个 Phase 推进。

**我需要你们帮我审阅**：

- **Advisor-Claude**：请重点关注**任务粒度和依赖链的概念完整性**。有没有任务应该拆分？有没有遗漏的隐式依赖？
- **Advisor-GPT**：请重点关注**条款对齐和验收标准的可测性**。每个验收标准是否足够精确？有没有条款被遗漏？

计划文档已在上方链接，请依次发言。我们的目标是产出 v0.2 修订版。

---

### Advisor-Claude 发言

圣诞快乐，StandardsChair！🎄

我已经仔细阅读了实施计划 v0.1，并与 MVP 设计文档进行了交叉对照。整体而言，这是一份**结构清晰、粒度合理**的计划，条款-任务映射表（附录 A）特别有价值。以下是我从**任务粒度和依赖链的概念完整性**角度的审阅结果。

---

## 问题清单

### P0 级（应阻塞执行）

#### 1. [依赖遗漏] T-P2-01 对 Phase 1 的依赖不准确

**现状**：T-P2-01 (Address64/Ptr64) 依赖 T-P1-05 (IRbfScanner)

**问题**：Address64/Ptr64 是**纯数据结构**（8 字节对齐的文件偏移量），不依赖 Scanner 的逆向扫描逻辑。实际上：
- T-P1-04 (IRbfFramer/Builder) 才是真正使用 Address64 的地方（Frame 写入位置计算）
- T-P1-05 (IRbfScanner) 也使用 Address64，但非必要前置

**建议**：
- T-P2-01 应改为**依赖 Phase 1 完成**或**无依赖**（可与 Phase 1 并行）
- 这可释放 1h 的并行度

---

#### 2. [隐式依赖遗漏] T-P2-02 (VarInt) 应被 T-P1-02 依赖

**现状**：T-P2-02 (VarInt 编解码) 在 Phase 2，无反向依赖

**问题**：查阅 [rbf-format.md](../../atelia/docs/StateJournal/rbf-format.md)，Frame 的 `HeadLen` 字段使用 VarInt 编码。因此 T-P1-02 (Frame 布局与对齐) **隐式依赖** VarInt。

**建议**：
- 方案 A：将 T-P2-02 提升到 Phase 1，作为 T-P1-02 的前置
- 方案 B：如果 MVP 的 HeadLen 只用固定长度编码（如 4 字节），则在条款覆盖说明中注明简化

**影响**：不修复可能导致 Implementer 在 T-P1-02 时发现缺少 VarInt 而返工。

---

### P1 级（强烈建议修复）

#### 3. [任务粒度] T-P3-03 (DurableDict 双字典实现) 4h 偏大

**现状**：T-P3-03 预估 4h，覆盖 API 签名、双字典策略、IDurableObject 接口实现

**问题**：这是 MVP 中最复杂的任务之一，包含：
- 两个内部字典 (`_committed`, `_current`) 的协调
- 5+ 个公开 API 方法
- IDurableObject 接口集成（State、HasChanges）

**建议**：拆分为两个任务：
- **T-P3-03a** (2h)：DurableDict 基础结构（双字典、基础读写 API）
- **T-P3-03b** (2h)：DurableDict 序列化集成（WritePendingDiff、OnCommitSucceeded）

**理由**：拆分后每个任务可独立验收，降低返工风险。

---

#### 4. [任务粒度] T-P5-03 (CommitAll) 4h 过于聚合

**现状**：T-P5-03 覆盖所有 `[A-COMMITALL-*]` 和 `[S-COMMIT-*]` 条款

**问题**：查阅条款数量，CommitAll 涉及：
- 双文件写入顺序（data → meta）
- Fsync 时序
- Dirty Set 遍历与清理
- 错误处理（Commit 失败时保持对象状态）

**建议**：拆分为：
- **T-P5-03a** (2h)：CommitAll 核心流程（遍历 Dirty Set、写入 data file）
- **T-P5-03b** (2h)：Commit 完成与恢复（meta 写入、Fsync、状态清理）

**理由**：Commit 是 MVP 最关键的正确性边界，拆分后可分步验证。

---

#### 5. [依赖链] Phase 间并行度可提升

**现状**：Phase 2 整体依赖 Phase 1 完成

**问题**：实际上有些 Phase 2 任务可以与 Phase 1 并行：
- T-P2-02 (VarInt) — 纯算法，无外部依赖
- T-P2-04 (DurableObjectState 枚举) — 纯定义，无外部依赖

**建议**：在依赖图中标注"可并行任务"，释放约 3h 的并行度

---

### P2 级（改进建议）

#### 6. [概念边界] T-P4-05 (LazyRef\<T\>) 可与 T-P4-04 合并

**现状**：T-P4-05 单独作为任务，预估 2h

**问题**：LazyRef 是 LoadObject 的"透明封装"，两者概念紧密耦合。分开实现可能导致接口不一致。

**建议**：合并为 **T-P4-04** (LoadObject & LazyRef, 4h)，验收标准合并

---

#### 7. [验收标准] 部分任务的验收标准过于模糊

**示例**：
- T-P1-01：验收标准 = "常量值正确" → **建议补充**：`BitConverter.GetBytes(Fence)` 等于 `FenceBytes`
- T-P2-05：验收标准 = "接口编译通过" → **建议补充**：至少有一个 Mock 实现通过单元测试

**通用建议**：每个任务的验收标准应遵循 **可机器判定** 原则——能被 CI 测试覆盖的标准优于人工审查。

---

#### 8. [遗漏任务] 缺少"错误类型定义"任务

**问题**：MVP 设计文档提及多种错误情况（`[F-DECODE-ERROR-FAILFAST]`、`[F-UNKNOWN-VALUETYPE-REJECT]` 等），但计划中没有专门的错误类型定义任务。

**建议**：在 Phase 2 增加 **T-P2-00** (0.5h)：StateJournal 错误类型定义
- 输出：`StateJournalError.cs`（继承 `AteliaError`）
- 覆盖条款：所有 `*-REJECT`、`*-FAILFAST` 条款的错误码

---

## 优点肯定

1. **条款-任务映射表（附录 A）非常有价值**——实现了规范条款到实施任务的双向追溯，有助于验收时检查覆盖度。

2. **runSubagent 调用模板设计周全**——`specFiles`、`conditions`、`acceptanceCriteria` 三层结构为 Implementer 提供了清晰的上下文。

3. **Phase 划分符合概念边界**——RBF Layer 0 → 核心类型 → DurableDict → Workspace → Commit，层层递进，避免了跨层耦合。

4. **预期项目结构清晰**——文件夹划分与 Phase 对应，便于增量交付。

---

## 改进建议汇总

| 编号 | 问题 | 建议操作 | 优先级 |
|:-----|:-----|:---------|:-------|
| 1 | T-P2-01 依赖不准确 | 改为无依赖或依赖 Phase 1 完成 | P0 |
| 2 | VarInt 与 Frame 布局隐式依赖 | 提升 T-P2-02 到 Phase 1 或注明简化 | P0 |
| 3 | T-P3-03 粒度偏大 | 拆分为 03a (基础) + 03b (序列化) | P1 |
| 4 | T-P5-03 粒度偏大 | 拆分为 03a (核心流程) + 03b (完成/恢复) | P1 |
| 5 | Phase 间并行度不足 | 标注可并行任务 | P1 |
| 6 | T-P4-05 与 T-P4-04 耦合 | 合并为单一任务 | P2 |
| 7 | 验收标准模糊 | 补充可机器判定的条件 | P2 |
| 8 | 缺少错误类型任务 | 增加 T-P2-00 | P2 |

---

*以上是我从概念完整性角度的审阅。期待 GPT 的条款对齐审阅补充！*

---

### StandardsChair 补充发言（代 Advisor-GPT）

> **注**：由于 Advisor-GPT 遇到速率限制，我以 StandardsChair 身份完成条款对齐审计。

## 条款覆盖审计

对照 T-20251225-01 审计结果的 80 条条款，检查附录 A 的映射：

### 遗漏条款清单

| 条款 ID | 类型 | 遗漏原因 | 建议归属任务 |
|---------|------|----------|--------------|
| `[F-FRAMESTATUS-VALUES]` | 格式 | FrameStatus 处理未显式提及 | T-P1-04 |
| `[F-FRAMESTATUS-FILL]` | 格式 | FrameStatus 填充规则 | T-P1-04 |
| `[F-STATUSLEN-FORMULA]` | 格式 | StatusLen 计算 | T-P1-02 |
| `[S-RBF-TOMBSTONE-VISIBLE]` | 语义 | Scanner 产出 Tombstone | T-P1-05 |
| `[S-TRANSIENT-DISCARD-DETACH]` | 语义 | Transient 对象 Discard | T-P3-05 |
| `[S-TRANSIENT-DISCARD-OBJECTID-QUARANTINE]` | 语义 | ObjectId 隔离 | T-P4-03 |
| `[S-OBJECTID-MONOTONIC-BOUNDARY]` | 语义 | 单调性边界 | T-P4-03 |
| `[S-POSTCOMMIT-WRITE-ISOLATION]` | 语义 | Commit 后写隔离 | T-P3-03 |

**发现**：附录 A 映射了约 30 条主要条款，但 80 条中约有 8-10 条未显式映射。

### 验收标准可测性审计

| 任务 ID | 当前验收标准 | 问题 | 改进建议 |
|---------|--------------|------|----------|
| T-P1-01 | "常量值正确" | 模糊 | "Fence == 0x31464252 且 FenceBytes.SequenceEqual([0x52,0x42,0x46,0x31])" |
| T-P2-05 | "接口编译通过" | 无测试 | "存在至少一个 IDurableObject 的 test double 实现" |
| T-P4-01 | "去重测试通过" | 未指定场景 | "同一 ObjectId 加载两次返回相同实例（ReferenceEquals）" |
| T-P4-02 | "GC pinning 测试通过" | 难以测试 | "Dirty 对象调用 GC.Collect() 后仍可访问" |
| T-P5-01 | "Dict 复用验证" | 模糊 | "VersionIndex 类型为 DurableDict 且 Key 为 ulong" |

### runSubagent 模板审计

| 字段 | 示例 1 | 示例 2 | 示例 3 | 建议 |
|------|--------|--------|--------|------|
| taskId | ✅ | ✅ | ✅ | — |
| phase | ✅ | ✅ | ✅ | — |
| name | ✅ | ✅ | ✅ | — |
| targetFiles | ✅ | ✅ | ✅ | — |
| specFiles | ✅ | ✅ | ✅ | — |
| conditions | ✅ | ✅ | ✅ | — |
| implementation | ✅ | ✅ | ✅ | — |
| dependencies | ❌ 缺失 | ✅ | ✅ | 补充 |
| testFile | ✅ | ✅ | ✅ | — |
| acceptanceCriteria | ✅ | ✅ | ✅ | — |
| **errorHandling** | ❌ | ❌ | ❌ | 新增字段 |

**建议新增字段**：
- `errorHandling`: 该任务应处理的错误场景（配合 Claude 提出的 T-P2-00）

---

### 主持人共识汇总 (刘德智)

感谢 Advisor-Claude 的详细审阅！综合两个视角的发现，达成以下共识：

## 共识决议

### 立即采纳（P0/P1）

| # | 问题 | 决议 | 影响 |
|---|------|------|------|
| 1 | T-P2-01 依赖不准确 | **采纳**：改为无依赖 | 可与 Phase 1 并行 |
| 2 | VarInt 隐式依赖 | **采纳方案 B**：MVP HeadLen 使用 u32 LE（非 VarInt），在 T-P1-02 条款说明中注明 | 无结构变更 |
| 3 | T-P3-03 拆分 | **采纳**：拆为 03a (基础 2h) + 03b (序列化 2h) | +1 任务 |
| 4 | T-P5-03 拆分 | **采纳**：拆为 03a (核心 2h) + 03b (完成 2h) | +1 任务 |
| 5 | Phase 间并行度 | **采纳**：标注 T-P2-02, T-P2-04 可并行 | 文档更新 |

### 延后处理（P2）

| # | 问题 | 决议 | 理由 |
|---|------|------|------|
| 6 | T-P4-04/05 合并 | **延后**：保持分离 | LoadObject 已足够复杂，LazyRef 值得单独验收 |
| 7 | 验收标准模糊 | **采纳**：v0.2 补充具体化 | 提升可测性 |
| 8 | 错误类型任务 | **采纳**：增加 T-P2-00 (0.5h) | 早期定义便于后续引用 |
| 9 | 条款映射补充 | **采纳**：附录 A 补充遗漏条款 | 完整性 |
| 10 | 模板字段补充 | **采纳**：示例 1 补充 dependencies | 一致性 |

## v0.2 变更清单

1. ✅ T-P2-01 依赖改为"无"
2. ✅ T-P1-02 增加条款说明：HeadLen 使用 u32 LE
3. ✅ T-P3-03 拆分为 T-P3-03a + T-P3-03b
4. ✅ T-P5-03 拆分为 T-P5-03a + T-P5-03b
5. ✅ 增加 T-P2-00 (错误类型定义)
6. ✅ 标注可并行任务
7. ✅ 补充验收标准具体化
8. ✅ 附录 A 补充遗漏条款
9. ✅ 示例 1 模板补充 dependencies 字段

**总任务数**：24 → 27 (+3)
**总预估工时**：44-58h → 48-64h (+4-6h)

---

## 状态更新

> **畅谈会状态**：✅ 已完成
> **下一步**：修订 implementation-plan.md 为 v0.2

---

