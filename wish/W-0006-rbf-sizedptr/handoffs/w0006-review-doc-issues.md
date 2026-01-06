# W-0006 审阅报告 — 文档/措辞类问题汇总

> 从 `w0006-review-resolve.md` 提取的文档/措辞类问题。
> 提取日期：2026-01-06

---

## 来源：Resolve.md 审阅

### D1: meeting 链接路径缺少 `../`

- **原报告位置**：w0006-review-resolve.md / P1
- **原严重性**：Sev1
- **问题类型**：链接路径
- **问题描述**：正文多处使用相对路径 `meeting/2026-01-05-scope-and-approach.md`，但 `Resolve.md` 位于 `wish/W-0006-rbf-sizedptr/artifacts/`，meeting 文件实际在 `wish/W-0006-rbf-sizedptr/meeting/`。路径少了 `../`，导致链接不可跳转。
- **证据**：
  - Resolve.md 位于 `wish/W-0006-rbf-sizedptr/artifacts/`
  - meeting 文件位于 `wish/W-0006-rbf-sizedptr/meeting/`
- **建议修复**：将所有 `meeting/` 改为 `../meeting/`

---

### D2: §1 "共存分工" vs §6 "直接替代" 的叙事不一致

- **原报告位置**：w0006-review-resolve.md / P2
- **原严重性**：Sev1
- **问题类型**：表述演进未对齐
- **问题描述**：§1 把 <deleted-place-holder> 与 SizedPtr 定义为"共存分工"（point-to vs range），但 §6/§7 给出最终决策是"SizedPtr 直接替代 <deleted-place-holder>"。这是设计**演进**的记录问题（§1 是初始理解，§6/§7 是最终结论），不是逻辑矛盾。
- **分类依据**：Resolve-Tier 文档记录了"从问题识别到决策形成"的过程，§1 代表初始视角，§6/§7 代表澄清后的结论。这种演进是合理的，只需统一叙事风格。
- **建议修复**：
  - 在 §1 或 §6 添加一句说明设计认知的演进
  - 或回写 §1 使其与最终结论一致（以"事后交付稿"风格）

---

### D3: §3 Scope 与 §7 结论的目标不一致

- **原报告位置**：w0006-review-resolve.md / P3
- **原严重性**：Sev2
- **问题类型**：叙事未同步
- **问题描述**：§3 Scope 明确"语义边界定义：明确 <deleted-place-holder> vs SizedPtr 的使用场景和判断依据"，而 §7 说"对外全部用 SizedPtr"，不再需要"共存策略"。Scope 未根据最终决策同步收敛。
- **分类依据**：这是设计演进后未更新 Scope 的叙事问题，不是逻辑错误。决策清晰，只是描述未对齐。
- **建议修复**：将 Scope 的"语义边界定义"改写为"对外替代策略 + 对内是否保留 <deleted-place-holder> 的决策点"

---

### D4: frontmatter 与正文的术语表达不一致（Value vs Offset）

- **原报告位置**：w0006-review-resolve.md / P7
- **原严重性**：Sev2
- **问题类型**：措辞一致性
- **问题描述**：frontmatter 描述风险为 "<deleted-place-holder>.Value 与 SizedPtr.OffsetBytes 术语混淆"，正文 P4 描述为 "RBF 使用 Offset（<deleted-place-holder> 的文件偏移）"。两处用了不同表达方式：frontmatter 用字段名 `Value`，正文用概念名 `Offset`。
- **分类依据**：<deleted-place-holder> 的字段名确实是 `Value`（已验证），正文的 `Offset` 是概念名。两者都正确，但表达方式不一致。
- **建议修复**：统一为一种表述：
  - 若强调字段名：正文也改用 `Value`
  - 若强调概念：frontmatter 改为"<deleted-place-holder> 的文件偏移概念与 SizedPtr.OffsetBytes 混淆风险"

---

### D5: 一句话判定语气与最终结论不一致

- **原报告位置**：w0006-review-resolve.md / P8
- **原严重性**：Sev2
- **问题类型**：措辞精炼
- **问题描述**：开头一句话判定为"值得引入"（偏"可选增强"语气），但 §6/§7 明确指出 SizedPtr 是接口层"核心类型"，不是可选增强。
- **建议修复**：将一句话判定改为直接反映最终结论，例如：
  > "SizedPtr 是 RBF Interface 层对外 Frame 句柄的核心类型，应替代 <deleted-place-holder>。"

---

## 统计

| 类型 | 数量 |
|:-----|:-----|
| 链接路径 | 1 |
| 表述演进未对齐 | 1 |
| 叙事未同步 | 1 |
| 措辞一致性 | 1 |
| 措辞精炼 | 1 |
| **合计** | **5** |

---

## 原 Sev 分布

| 严重性 | 数量 |
|:-------|:-----|
| Sev1 | 2 |
| Sev2 | 3 |
| **合计** | **5** |

---

## 来源：Shape.md 审阅

### D6: §2 术语合同的"Offset 专指 OffsetBytes"表述缺乏可执行约束

- **原报告位置**：w0006-review-shape.md / P2
- **原严重性**：Sev2
- **问题类型**：措辞精炼/可执行性
- **问题描述**：Resolve 中的 I-TERM-DRIFT-RISK 是"<deleted-place-holder>.Value vs SizedPtr.OffsetBytes/Offset 术语混用"的风险。Shape 目前采用"Offset 一词专指 OffsetBytes"的约定，但存在两个问题：
  1. 文档/代码里仍可能出现"Offset"（未带 Bytes），读者不确定它是"文件偏移"还是"区间 offset"。
  2. `rbf-format.md` 和其他文档会天然使用"offset"作为通用英文词，单靠"专指"难以形成可执行的写作约束。
- **分类依据**：这是术语书写规范的可执行性问题，不涉及设计逻辑或工程可行性。
- **建议修复**：将"术语合同"升级为更强、可执行的约束：
  - "在 W-0006 产物中，除非在引用外部文档原文，否则一律使用 `OffsetBytes`/`LengthBytes`；避免裸写 `Offset`/`Length`。"
  - 如果确需使用"offset"作为通用词，必须写作"file offset（=OffsetBytes）"。

---

### D7: §2 Glossary Alignment 表格布局可能误导读者（新旧约束混淆）

- **原报告位置**：w0006-review-shape.md / P3
- **原严重性**：Sev2
- **问题类型**：格式/表述清晰度
- **问题描述**：`SizedPtr.OffsetBytes` ↔ `<deleted-place-holder>.Value` 行的备注写作"4B 对齐，38-bit（~1TB 范围）"，这对 `SizedPtr.OffsetBytes` 是正确的，但放在"新旧对照"行里，读者可能误读为"<deleted-place-holder>.Value 也只有 ~1TB"。
- **分类依据**：这是表格格式与措辞布局问题，<deleted-place-holder> 的实际能力不受影响，只是呈现方式容易引起误读。
- **建议修复**：将该行拆成更清晰的两段信息：
  - 映射关系：`<deleted-place-holder>.Value (file offset)` → `SizedPtr.OffsetBytes (file offset)`
  - 新约束：`SizedPtr` 受 38:26 与 4B 对齐约束（并把范围信息放到"新约束"列/脚注里），避免把旧类型也"顺带限幅"。

---

### D8: §3 Interface Contract 未显式链接到 `rbf-interface.md` 的具体 API 点

- **原报告位置**：w0006-review-shape.md / P6
- **原严重性**：Sev3
- **问题类型**：可追溯性/链接补充
- **问题描述**：Shape 的叙述与监护人决策一致，覆盖了写/读/持久化三用途，但"用途→API"的对应关系主要靠读者自行推断。
- **分类依据**：API 已存在且设计正确，只是文档中缺少显式引用链接，属于可追溯性改进。
- **建议修复**：每个用途补一个括号引用：
  - 写路径：`IRbfFramer.Append` / `RbfFrameBuilder.Commit`
  - 读路径：`IRbfScanner.TryReadAt`
  - 持久化：`SizedPtr.Packed`（以及 `RbfFrame.Ptr` 在 ScanReverse/ReadAt 的产出）

---

## 统计（Shape.md 来源）

| 类型 | 数量 |
|:-----|:-----|
| 措辞精炼/可执行性 | 1 |
| 格式/表述清晰度 | 1 |
| 可追溯性/链接补充 | 1 |
| **合计** | **3** |

---

## 原 Sev 分布（Shape.md 来源）

| 严重性 | 数量 |
|:-------|:-----|
| Sev2 | 2 |
| Sev3 | 1 |
| **合计** | **3** |

---

## 来源：Rule.md 审阅

### D9: §1 NullPtr 代码示例缺少 `using Atelia.Data;`

- **原报告位置**：w0006-review-rule.md / P2
- **原严重性**：Sev1
- **问题类型**：代码示例完整性
- **问题描述**：示例在 `namespace Atelia.Rbf` 中直接使用 `SizedPtr`，但 `SizedPtr` 定义在 `Atelia.Data` 命名空间。读者复制示例会出现类型无法解析的编译错误。
- **分类依据**：示例意图清晰（展示 NullPtr 的定义方式），缺少的只是 using 声明。读者能理解需要引用 `Atelia.Data`，不会误导实现方向。
- **建议修复**：任选其一：
  - 增加 `using Atelia.Data;`
  - 使用全限定名 `Atelia.Data.SizedPtr`

---

### D10: §3.4 字段名表述"SizedPtr Ptr"读起来像类型名+字段名重复

- **原报告位置**：w0006-review-rule.md / P7
- **原严重性**：Sev3
- **问题类型**：表述清晰度
- **问题描述**：当前写作 `RbfFrame.Address` → `SizedPtr Ptr`，读起来像"类型名+字段名重复"。实际表达是"属性名 Address 改为 Ptr，类型为 SizedPtr"。
- **分类依据**：表述不够清晰，但意图可理解，不影响实施。
- **建议修复**：改为"`RbfFrame.Address`（<deleted-place-holder>）→ `RbfFrame.Ptr`（`SizedPtr`）"

---

### D11: §2 条款 ID `[R-RBF-<deleted-place-holder>-DEPRECATED]` 与"完全移除"措辞不一致

- **原报告位置**：w0006-review-rule.md / P9
- **原严重性**：Sev3
- **问题类型**：术语一致性
- **问题描述**：小节与正文规则要求"完全移除"，但条款 ID 使用 `DEPRECATED`（暗示"仍存在但不推荐使用"）。这会降低读者对迁移强制性的理解。
- **分类依据**：术语选择问题，但条款正文已明确是"完全移除"，不会导致实施偏差。
- **建议修复**：将条款 ID 改为 `[R-RBF-<deleted-place-holder>-REMOVED]` 或 `[R-RBF-NO-<deleted-place-holder>]`

---

## 来源：Plan.md 审阅

### D12: 修订清单行号漂移，降低直接可执行性

- **原报告位置**：w0006-review-plan.md / Sev2-1
- **原严重性**：Sev2
- **问题类型**：定位方式/可维护性
- **问题描述**：§2-3 修订清单大量依赖行号（L82-97、L106-107、L134 等），但当前上游文档已发生位移（如 `SizedPtr` 章节起始约在 L84，`Append` 签名在 L153 左右）。
- **分类依据**：行号不准确但位置可通过章节标题/符号/条款 ID 定位，执行者仍能找到目标位置，只是需要额外搜索步骤。不会导致修订完全不可执行。
- **建议修复**：
  - 用章节标题/条款 ID/符号签名替代行号（如"`rbf-interface.md` §3.1 `IRbfFramer.Append` 的返回类型"）
  - 若保留行号，标注文档版本/commit，并提供可搜索的文本锚点

---

### D13: 验收标准与目标改动的表达方式有错位

- **原报告位置**：w0006-review-plan.md / Sev2-2
- **原严重性**：Sev2
- **问题类型**：验收项表述精确度
- **问题描述**：§5.1 最后一条写"文档头部增加设计演进锚点；验证：frontmatter 含 `produce_by` 指向本 wish"。但当前上游文档的"设计演进"链接是正文 blockquote，而 `produce_by` 是另一类元数据。验收方法与目标改动不是同一件事。
- **分类依据**：目标改动清晰（增加设计演进锚点），只是验收方法的表述需要细化，不影响执行方向。
- **建议修复**：拆成两个可判定验收点：
  1. frontmatter `produce_by` 正确指向 wish
  2. 文档正文头部存在 `设计演进` 链接指向 W-0006 artifacts

---

### D14: Phase 2 引用已不存在的 <deleted-place-holder>.cs 文件

- **原报告位置**：w0006-review-plan.md / Sev3-1
- **原严重性**：Sev3
- **问题类型**：计划条目准确性
- **问题描述**：Phase 2 提到"删除 `<deleted-place-holder>.cs`（已归档在 archive/）"，但工作区内已无该文件（源码已归档到 `atelia/archive/2025-12-29-rbf-statejournal-v1/Rbf/<deleted-place-holder>.cs`）。
- **分类依据**：不阻塞 Phase 1 执行，只是 Phase 2 计划的准确性问题。
- **建议修复**：
  - 补充归档路径 `archive/2025-12-29-rbf-statejournal-v1/Rbf/<deleted-place-holder>.cs`
  - 或改为"清理 <deleted-place-holder> 残留实现（如存在）"

---

### D15: 条款索引新增表项的文本模板未明确

- **原报告位置**：w0006-review-plan.md / Sev3-2
- **原严重性**：Sev3
- **问题类型**：清单细节补充
- **问题描述**：§2.2 写"§6 条款索引 新条款条目"，但未明确条款索引的具体表项文本（类似 §5.2 变更日志条目那样的模板）。
- **分类依据**：清单有参照模板（§5.2），执行者能推断格式，只是细节不够明确。
- **建议修复**：补齐新增索引项的文本模板

---

## 统计（Plan.md 来源）

| 类型 | 数量 |
|:-----|:-----|
| 定位方式/可维护性 | 1 |
| 验收项表述精确度 | 1 |
| 计划条目准确性 | 1 |
| 清单细节补充 | 1 |
| **合计** | **4** |

---

## 原 Sev 分布（Plan.md 来源）

| 严重性 | 数量 |
|:-------|:-----|
| Sev2 | 2 |
| Sev3 | 2 |
| **合计** | **4** |

---

## 来源：跨层一致性审阅

### D16: Shape 的"术语合同"未在 Rule/Plan 中条款化/验收化

- **原报告位置**：w0006-review-cross-tier.md / Sev2-3
- **原严重性**：Sev2
- **问题类型**：可执行性/防护机制
- **问题描述**：Shape 提出"`Offset` 专指 `SizedPtr.OffsetBytes`"，但 Rule/Plan 没有把此作为可执行条款（例如：文档 MUST 使用 OffsetBytes 术语，禁止裸 Offset）。
- **分类依据**：术语约定本身清晰（Shape 已给出映射），只是缺少"书写约束→验收项"的闭环。不会导致当前实施困惑，属于防护性改进。D6 已有类似问题（Shape 层面的可执行性），此处是跨层传递的延续。
- **建议修复**：
  - 在 Rule 增补一个简短条款（例如 `[R-RBF-OFFSET-TERM]`）
  - 在 Plan 验收项加入 grep/人工检查要点

---

### D17: NullPtr 的"几何语义 vs 业务约定"在 Shape 与 Rule 的定位略有张力

- **原报告位置**：w0006-review-cross-tier.md / Sev2-4
- **原严重性**：Sev2
- **问题类型**：概念收束/措辞补充
- **问题描述**：Shape 将 `ptr == default` 视作"无效引用"；Rule 强调 `Packed=0` 数学上是空区间，但同时又将其作为 null。概念上可自洽，但需要一个"RBF 语义视角"的一句话收束。
- **分类依据**：两层的定位没有逻辑矛盾（Shape 说"无效引用"，Rule 说"业务约定"，都指向同一件事），只是读者可能困惑"(0,0) 到底是不是合法 range"。补一句收束话术即可。
- **建议修复**：在 Rule/接口文档补一句明确话术：
  > "在 RBF 语境中，`(0,0)` 保留为 NullPtr；有效 Frame 引用 MUST 满足 `LengthBytes > 0` 且 `OffsetBytes >= GenesisLen`。"

---

### D18: Rule 的"条款变更清单"与 Plan 的"修订清单"重复但不完全同构

- **原报告位置**：w0006-review-cross-tier.md / Sev3-5
- **原严重性**：Sev3
- **问题类型**：文档组织/维护方式
- **问题描述**：Rule 的 3.2/3.3/3.4 与 Plan 的 §2/§3 有重复，但条款 ID（如 NullPtr）在重复过程中引入漂移。维护成本增加，容易产生"一个更新了另一个没更新"。
- **分类依据**：重复本身不是逻辑错误，是文档组织方式的问题。条款 ID 漂移的**核心问题**（Sev1-1）已保留在设计/工程类，此处关注的是"重复导致维护负担"这一文档层面的改进。
- **建议修复**：
  - 把 Rule 的变更清单定位为"规范性目标"（What），Plan 定位为"执行步骤"（How）
  - 尽量通过引用而非复制表格；或在 Plan 中声明"以 Rule §3 为 SSOT"

---

## 统计（跨层一致性审阅来源）

| 类型 | 数量 |
|:-----|:-----|
| 可执行性/防护机制 | 1 |
| 概念收束/措辞补充 | 1 |
| 文档组织/维护方式 | 1 |
| **合计** | **3** |

---

## 原 Sev 分布（跨层一致性审阅来源）

| 严重性 | 数量 |
|:-------|:-----|
| Sev2 | 2 |
| Sev3 | 1 |
| **合计** | **3** |

---

## 汇总统计

| 来源 | 数量 |
|:-----|:-----|
| Resolve.md | 5 |
| Shape.md | 3 |
| Rule.md | 3 |
| Plan.md | 4 |
| 跨层一致性审阅 | 3 |
| **合计** | **18** |

---

## 汇总 Sev 分布

| 严重性 | 数量 |
|:-------|:-----|
| Sev1 | 3 |
| Sev2 | 9 |
| Sev3 | 6 |
| **合计** | **18** |
