# W-0006 跨层一致性审阅报告

> **分类说明**：本报告原包含 5 个问题。经分类，3 个文档/措辞类问题（Sev2-3, Sev2-4, Sev3-5）已提取至 [w0006-review-doc-issues.md](w0006-review-doc-issues.md)（D16-D18）。本报告保留 2 个设计/工程类问题。

## 审阅摘要
- 总体评价：需改进
- 逻辑链条完整性：有断点
- 术语一致性：有漂移
- 发现问题数：5 个 → 保留设计/工程类 2 个

## 逻辑链条分析

### Resolve → Shape
- **4 个问题的承接情况**：
  - P1（Null 语义冲突）：Shape 已吸收为 `RbfInterface.NullPtr = default(SizedPtr)` 的业务约定，并明确推荐判等方式（`ptr == default`）。
  - P2（缺显式区间类型）：Shape 将 SizedPtr 定位为 RBF Interface 核心 Frame 句柄类型，并解释了写/读/持久化三类用途。
  - P3（256MB 上限适用性）：Shape §4.1 明确引用监护人决策并给出约束视角（RBF 承诺/StateJournal 假设）。
  - P4（术语漂移风险）：Shape 通过 Glossary Alignment 给出了旧→新映射，并提出“跨文档 `Offset` 专指 `SizedPtr.OffsetBytes`”。
- **监护人决策（Resolve §6）的传递**：
  - D1/D2/D4 传递完整：Shape 明确“SizedPtr 完全替代 <deleted-place-holder>”并突出其为核心类型。
  - D3（Null 语义移至 RBF 层）也已传递，但后续 Tier（Rule/Plan）在条款 ID 形态上出现不一致（见问题清单）。

### Shape → Rule
- **Glossary Alignment → 条款化**：
  - Rule 以条款形式给出了 NullPtr 约定与 <deleted-place-holder> 移除规则，并提供迁移映射表。
  - 但 Rule 仍把 NullPtr 条款记为 `[R-RBF-NULLPTR]`，而 Shape/实际文档使用的是 `[F-RBF-NULLPTR]`（ID 漂移，且 Tier 间没有一致的映射说明）。
- **Interface Contract → 约束定义**：
  - Rule 覆盖了接口签名替换点（Append/Commit/TryReadAt/RbfFrame），与 Shape §5 基本一致。
  - 但 Rule 的“完全移除 <deleted-place-holder>”与 Shape “不再出现 <deleted-place-holder> 术语”的承诺，在“实际文档集”层面还未完全兑现（rbf-test-vectors.md 仍存在 <deleted-place-holder>/Ptr64 文本与条款索引，见后文）。

### Rule → Plan
- **Rule 的条款变更清单 → Plan 的修订步骤**：
  - Plan 基本覆盖 Rule 的“删除/新增/更新”清单，并给出行号/章节级操作。
  - 但 Plan 在 rbf-interface.md 的 NullPtr 新增条款 ID 写成了 `[F-RBF-NULLPTR]`（与实际 rbf-interface.md 一致），而 Plan 同时又在描述中写“被 `[R-RBF-NULLPTR]` 替代/新增”与 Rule 不一致，形成“Rule→Plan 传递断点”。
- **Rule 的 NullPtr 定义 → Plan 的实施指导**：
  - Plan 提供了示例代码、判等方式与 TryReadAt 失败语义（接收 NullPtr 返回 false），与 Rule §1/§4.2 一致。

### Plan → 实际文档
- **与 rbf-interface.md / rbf-format.md 的一致性**：
  - rbf-interface.md 已完成：<deleted-place-holder> 移除、SizedPtr §2.3、`[F-SIZEDPTR-DEFINITION]`、`[F-RBF-NULLPTR]`、接口签名与示例更新、条款索引/变更日志更新。
  - rbf-format.md 已完成：§7 重写为 SizedPtr wire format，并使用 `[F-SIZEDPTR-WIRE-FORMAT]`。
- **Plan 的修订清单与实际修订差异**：
  - Plan 要求“grep 验证 <deleted-place-holder> 清除（atelia/docs/Rbf）”，但实际仓库仍有 `atelia/docs/Rbf/rbf-test-vectors.md` 中 <deleted-place-holder>/Ptr64 相关表述与条款索引（且其 frontmatter 仍引用旧版本关系）。这使 Phase 1 的“文档层修订完成”在“文档集”意义上并未闭合。

## 问题清单

### Sev1（必须修）
1. **条款 ID 跨 Tier 不一致，且缺少映射解释**
   - 现象：
     - Shape/实际 `rbf-interface.md` 使用 `[F-RBF-NULLPTR]`。
     - Rule 使用 `[R-RBF-NULLPTR]`。
     - Plan 的清单部分又写成 `[F-RBF-NULLPTR]` 和 `[F-RBF-NULLPTR]`（同时在其它段落沿用“被 `[R-RBF-NULLPTR]` 替代”的叙述）。
   - 影响：下游引用无法确定应引用哪个 ID；审阅与验收会出现“看起来完成但引用断裂”。
   - 建议：选定唯一 ID（建议与实际接口文档一致：`[F-RBF-NULLPTR]`），并在 Rule/Plan 中统一；若坚持 R/F 分层，则需给出明确映射规则（例如：Rule 用 R-*，但落地到具体规范文档时映射为 F-*），否则不要混用。

2. **Plan → 实际文档的验收标准与现状矛盾（<deleted-place-holder> 未在文档集清零）**
   - 现象：Plan §5.1 写“grep -r \"<deleted-place-holder>\" atelia/docs/Rbf/ 返回空”。
     但实际 `atelia/docs/Rbf/rbf-test-vectors.md` 仍包含 “<deleted-place-holder>/Ptr64” 文本与 `[F-PTR64-WIRE-FORMAT]` 条目。
   - 影响：Phase 1 结束标准不可达；跨文档读者仍会看到旧术语与旧条款 ID，造成“Shape 承诺（不再出现 <deleted-place-holder>）”被事实否定。
   - 建议：把 rbf-test-vectors.md 纳入 Phase 1 修订范围（更新为 SizedPtr，并同步条款索引），或修改 Plan 的验收标准明确“仅 rbf-interface.md 与 rbf-format.md 清零”。

## 优点记录
- Resolve → Shape 的决策传递非常直接：D2（核心用途）与 D4（直接替代）让 Shape 明确、可执行，减少了不必要的“共存策略”复杂度。
- rbf-interface.md 与 rbf-format.md 的落地质量高：
  - SizedPtr/NullPtr 定义清晰；
  - 接口签名、示例、条款索引、变更日志均同步更新；
  - rbf-format.md 以 SSOT 为中心的组织方式保持稳定。
- Rule 对“Null 是业务约定”这一点写得比较到位，避免污染 `Atelia.Data.SizedPtr` 的几何语义。

## 总体建议
- **先修 Sev1-1**：统一 `[F-RBF-NULLPTR]` vs `[R-RBF-NULLPTR]` 的条款 ID 策略。否则跨层链条在"条款引用"上存在硬断点。
- **先修 Sev1-2**：把 `rbf-test-vectors.md` 纳入 W-0006 Phase 1（或调整验收口径）。否则"文档集闭合"不可达。

> **已提取至文档/措辞类**：术语约束条款化（D16）、NullPtr 收束话术（D17）、Rule/Plan 重复表格优化（D18）。见 [w0006-review-doc-issues.md](w0006-review-doc-issues.md)。

