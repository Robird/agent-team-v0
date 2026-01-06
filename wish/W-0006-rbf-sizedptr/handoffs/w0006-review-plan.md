---
docId: "handoff-w0006-review-plan"
title: "W-0006 Plan.md 审阅（可执行性 & 上游对齐）"
date: "2026-01-06"
reviewer: "Craftsman"
source:
  - "/repos/focus/wish/W-0006-rbf-sizedptr/artifacts/Plan.md"
---

# W-0006 Plan.md 审阅（可执行性 & 上游对齐）

> **文档/措辞类问题已提取**
>
> 本报告中的文档/措辞类问题（Sev2-1 行号漂移、Sev2-2 验收标准表达、Sev3-1 <deleted-place-holder>.cs 引用、Sev3-2 条款索引模板）已移至 [w0006-review-doc-issues.md](w0006-review-doc-issues.md) 编号 D12-D15。
>
> 本报告仅保留 **设计/工程类问题**（3 项）。

## 审阅摘要

`Plan.md` 的整体结构清晰：以 Phase 1（文档修订）/Phase 2（未来代码实现）拆分范围，并且在 §4 提供了"语义增强清单 + 隐性知识记录"，这部分与 `Resolve.md` §6 的监护人原话基本一致，能帮助读者理解 *为什么 SizedPtr 不只是改名*。

主要问题集中在两类：
1) **条款 ID 对齐**：Rule.md 对 NullPtr 条款 ID 与异常类型的约束，与 Plan.md（以及实际上游文档中的实现形态）存在不一致，可能造成条款索引与后续跨文档引用混乱。
2) **范围闭合**：`rbf-test-vectors.md` 作为 `rbf-format.md` 的关联文档，仍使用旧术语，但未纳入 Phase 1 修订范围，导致"文档层修订闭环"不完整。

结论：**Plan.md 的"方向"和"语义说明"质量较高，但需要解决条款 ID 统一问题，并明确 `rbf-test-vectors.md` 的处置策略。**

---

## 问题清单

### Sev1（必须修）

1. **Rule.md 与 Plan.md 的 NullPtr 条款 ID 不一致（且会外溢到上游文档引用）**
   - 证据：
     - `Rule.md` §1/§3.2 使用条款 ID **`[R-RBF-NULLPTR]`**。
     - `Plan.md`（以及当前 `rbf-interface.md` §2.3）使用 **`[F-RBF-NULLPTR]`**。
   - 影响：
     - 条款 ID 是"跨文档可引用"的稳定锚点；此处不一致会导致条款索引、引用、后续审阅/实现跟踪出现"到底哪个才是规范条款"的歧义。
   - 建议修复方向（二选一，但必须统一）：
     - **方案 A（推荐）**：把 Rule.md 中的 `[R-RBF-NULLPTR]` 统一改为 `[F-RBF-NULLPTR]`（因为它目前放在 interface 文档的术语表 §2，且已经以 F- 前缀落地）。
     - 方案 B：把 Plan.md 与 `rbf-interface.md` 的 `[F-RBF-NULLPTR]` 统一改为 `[R-RBF-NULLPTR]`，并更新条款索引与变更日志引用。

2. **Rule.md 的异常类型描述与 `Atelia.Data.SizedPtr` 实际实现不一致，Plan.md 新 §2.3 的措辞也受影响**
   - 证据：
     - `Rule.md` §4.1 写道：对齐违规"构造时抛出 `ArgumentException`"。
     - `atelia/src/Data/SizedPtr.cs` 的 `SizedPtr.Create(...)`：对齐/范围/溢出均抛出 **`ArgumentOutOfRangeException`**。
     - `Plan.md` 新 §2.3 仅明确"超出范围抛 `ArgumentOutOfRangeException`"，对对齐违规的异常类型未明确。
   - 影响：
     - Rule-Tier 的约束若与真实 SSOT（代码/基础类型）不一致，会在后续实现/测试阶段产生"按 Rule 还是按代码"的冲突。
   - 建议：
     - 明确"RBF 文档引用 Atelia.Data 时，以 Atelia.Data 的行为为准"；并把 Rule.md 的异常类型更新为与 `SizedPtr.Create` 一致（或在 Rule.md 中改成"不规定异常类型，只规定失败必须可观测"，但要统一口径）。

---

### Sev2（重要，建议修）

1. **Plan.md 未把 `rbf-test-vectors.md` 的 Ptr64/<deleted-place-holder> 迁移纳入 Phase 1，但它是 rbf-format.md 显式引用的关联文档**
   - 证据：
     - `rbf-format.md` 头部链接了测试向量：`[rbf-test-vectors.md](rbf-test-vectors.md)`。
     - 当前 `rbf-test-vectors.md` 仍大量使用 "<deleted-place-holder>/Ptr64 / `[F-PTR64-WIRE-FORMAT]`"。
   - 影响：
     - 上游规范（format）已进入 SizedPtr 口径，但测试向量仍停留在 Ptr64/<deleted-place-holder> 口径，读者会遇到"规范说 SizedPtr，但测试向量说 Ptr64"的断裂。
   - 建议：
     - 若本 Wish 的 Phase 1 目标是"文档层修订闭环"，应将 `rbf-test-vectors.md` 纳入修订清单（至少：术语、条款 ID、章节标题/用例名）。
     - 若坚持 out-of-scope，建议在 `rbf-format.md` 或 Plan.md 中明确"测试向量将在后续 Wish 更新"，避免当前版本形成不一致。

---

## 优点记录（Good Practices）

1. **Phase 分层清晰，范围边界明确**：把"文档层修订"和"代码实现"分成两个阶段，有利于控制本 Wish 的交付面。
2. **Migration Notes 不止改类型名，能捕捉语义增益**：§4.1 的"一次 IO / 预分配 / 自包含"等描述，与监护人对 SizedPtr 价值的阐述一致，属于高质量迁移说明。
3. **隐性知识（K1-K4）记录到位**：尤其 K2（Null 语义是业务约定）能避免未来把 Null 语义塞回 `Atelia.Data.SizedPtr` 的回归风险。
4. **验收包含自动化检查倾向**：如"grep 清除 <deleted-place-holder>"这种验证方法具备可操作性（尽管需要修正 editor exclude/范围问题）。

---

## 总体建议

- **建议结论**：Plan.md 在"设计意图与语义解释"层面已达到可交付质量；设计/工程类问题需要在执行前解决。

- **建议优先动作（按收益排序）**：
  1) 先解决 **条款 ID 统一**（`[R-RBF-NULLPTR]` vs `[F-RBF-NULLPTR]`）以及 **异常类型口径** 的不一致（Rule ↔ Data.SizedPtr）。
  2) 明确 `rbf-test-vectors.md` 是否属于 Phase 1 的"文档闭环"范围；如果是，补入修订清单并给出条款 ID 迁移映射。
