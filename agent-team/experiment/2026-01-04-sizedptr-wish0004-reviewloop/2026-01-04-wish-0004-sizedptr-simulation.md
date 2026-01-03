---
docId: "M-2026-01-04-W0004-SizedPtr-Simulation"
title: "模拟运行：Wish-0004 SizedPtr（用于校验 Team-Leader 心智模型）"
created: "2026-01-04"
status: "Draft"
links:
  wish: "wishes/active/wish-0004-SizedPtr.md"
  design: "atelia/docs/Data/Draft/SizedPtr.md"
  leader_model: "agent-team/beacon/team-leader-mental-model-v0.1.md"
---

# 模拟运行：Wish-0004 SizedPtr（用于校验 Team-Leader 心智模型）

> 目的：按 `team-leader-mental-model-v0.1.md` 的循环模拟推进一次真实 Wish，持续记录关键决策、阻塞、与模型不足。

## 初始化（Leader 读取 Wish）

**输入**：
- Wish: `wishes/active/wish-0004-SizedPtr.md`
- 设计草案: `atelia/docs/Data/Draft/SizedPtr.md`

**初始观察（未裁决）**：
- Wish frontmatter `wishId` 仍为 `W-XXXX`，与文件名 `wish-0004-*` 不一致（可能影响可追溯性与 DocGraph 校验）。
- Wish 正文存在重复段落（同一段话出现两次），提示“输入源”尚未去噪。
- `atelia/docs/Data/Draft/SizedPtr.md` 写明首个目标用户为 `atelia/docs/Rbf/rbf-interface.md`。
  - ✅ 该路径实际存在：`atelia/docs/Rbf/rbf-interface.md`
  - ⚠️ 但 active code 里目前未发现 RBF/Address64 的实现（推测仍停留在文档/归档代码阶段）。
- Wish 的 Layer Progress 表尚未开始；尚未建立与具体产物/测试的对应关系。

## 迭代日志（按 Leader Loop）

### Iteration 1

#### Snapshot
- Stage: Resolve/Shape/Rule/Plan/Craft（待判定）
- Mode: 发散（先补事实/对齐落点）
- Pressure: 开放

#### Focus（候选）
- F1: 澄清 “首个目标用户/替换对象” 的真实落点（rbf-interface/Address64 的权威位置与语义）
- F2: 校验 Wish 输入本身是否可被系统消费（wishId、重复内容、链接正确性）

#### Demand（候选）
- action: Investigate（调查/定位）
- scope: 找到 Address64 的权威定义与现行代码位置；确认是否需要 null 语义；确认 `rbf-interface.md` 的实际路径
- deliverable: 事实清单 + 链接修复建议 + 初步问题列表
- definition_of_done: 能明确“替换谁/在哪里替换/必须保持哪些语义不变”
- stop_condition: 找到权威文档位置并提取关键约束条款（不再继续扩展到实现）

(后续迭代待补)

### Iteration 1（已执行：调查/定位）— 结果沉淀

#### Facts（可引用证据）
- `Address64` 的权威语义定义在：`atelia/docs/Rbf/rbf-interface.md` §2.3
  - `[F-ADDRESS64-DEFINITION]`：8B LE 文件偏移，指向 Frame 起始位置
  - `[F-ADDRESS64-ALIGNMENT]`：非零地址 MUST 4B 对齐（`Value % 4 == 0`）
  - `[F-ADDRESS64-NULL]`：`Value == 0` 表示 null（无效地址）
- wire format 对应条款在：`atelia/docs/Rbf/rbf-format.md` §7 (`[F-PTR64-WIRE-FORMAT]`)
  - 同样声明 `0=null` 且 `4B aligned`
- `SizedPtr` 草案提供了默认实现设想（38:26 + 4B 对齐压缩），见：`atelia/docs/Data/Draft/SizedPtr.md`

#### 当前冲突/张力（需要 Rule/Shape 决策）
- Wish 的 Non-Goals 写明“不定义特殊值（Null/Empty 是使用者用法问题）”，但 `rbf-interface.md` 明确规定 `Address64.Null = 0`。
  - 这意味着“替换 Address64”时，要么：
    - (A) 允许 `SizedPtr` 也承载 `0=null` 的约定；要么
    - (B) 保持 `SizedPtr` 纯净，但在 RBF/StateJournal 层引入 wrapper/约定（例如 `Address64` 继续存在为语义封装，只是内部用 `SizedPtr` 表示 `Offset/Length`）。
- `SizedPtr` 的“Length”是否等价于 RBF 的“FrameLength/RecordLength/SpanLength”？（尚未被文档绑定）
  - 目前草案只说 Length=长度（4B 对齐），但没指定“长度指向的对象是什么”。

#### 更新后的 Snapshot（本轮后）
- Stage: Rule/Shape（需要先做语义与命名决策，才能进入 Craft）
- Mode: 收敛（围绕两项冲突做决定）
- Pressure: 指定（交付物：两条决策记录 + 更新草案文档）

#### Next Demands（下一步可执行）
1) action: Decide (Rule)
   - scope: 明确 `SizedPtr` 是否承载 `0=null` 语义；如不承载，明确上层 wrapper 策略
   - deliverable: Decision Log（写入 `atelia/docs/Data/Draft/SizedPtr.md` 或新建 decisions 文档）
   - definition_of_done: 形成一句话规范 + 2-3 条必须保持的不变量
   - stop_condition: 一旦能写出“替换策略 + 兼容/迁移边界”，即停止扩展

2) action: Decide (Rule)
   - scope: 确定 bit 分配方案（40:24 / 38:26 / 36:28 之一）及其排他理由；明确长度含义（FrameLen? PayloadLen? 区间长度？）
   - deliverable: 决策段落 + 常量表（最大 Offset/Length 推导）
   - definition_of_done: 能从文本直接推导出边界值与对齐规则
   - stop_condition: 不再讨论“多类型/bit 标记位”等非目标

---

### Iteration 2（执行：收敛关键决策）

#### Snapshot（观察到的信号）
- **问题信号**：关键语义未钉死（Length 含义、bit 分配排他理由、特殊值分层策略）
  - 结果：无法进入 Craft（实现/测试），因为验收与边界无法稳定
- **层级判断**：Shape/Rule
- **Mode**：收敛
- **Pressure**：指定（交付物 = Decision Log，可直接粘贴进 `atelia/docs/Data/Draft/SizedPtr.md`）

#### Demand（identifyDemand 输出）
- action: Clarify/Decide
- scope:
  1) 定义 `Length` 的语义，使 SizedPtr 成为“通用产品”而非 RBF 专用
  2) 在 40:24 / 38:26 / 36:28 中选定默认 bit 分配，并写清排他理由与风险承认
  3) 明确“特殊值（Null/Empty）”的分层策略：SizedPtr 本体不定义，使用者可封装定义
- deliverable: Decision Log（含 tradeoff 与 stop_condition）
- definition_of_done: 读者可据此写出实现与单元测试，不再依赖口头补充
- stop_condition: 三项决策均有一句话结论 + 2-3 条理由 + 排除项，即停止扩展

#### matchContributor（能力池匹配）
- 贡献动作：本质追问 / 概念澄清 + Tradeoff 对比 + 形式化约束
- 推荐载体：`Seeker`（主）、`Craftsman`（复核：可实现性/边界）

#### 执行结果（来自 Seeker 的产出摘要）
- **Length 语义**：建议定义为纯几何语义的 byte range 长度，表达区间 `[offset, offset+length)`，不绑定 Frame/payload。
- **默认 bit 分配**：建议选 **38:26**；排除 36:28（offset 上限太低），谨慎对待 40:24（64MB length 天花板）。
- **特殊值分层**：SizedPtr 本体不定义 Null/Empty；上层（例如 RBF）可用 wrapper type 选择自己的约定（例如 `packed=0` 表示 null）。

#### Artifact Update（本轮落地动作）
- 将上述三项决策以 Decision Log 的形式写入：`atelia/docs/Data/Draft/SizedPtr.md`


---

## 对 Team-Leader 心智模型的“压力测试”发现（模型不足候选）

> 以下条目是在本 Wish 的模拟运行中自然暴露出来的缺口/模糊点：不是批评，而是下一版模型可增强之处。

### Gap 1：缺少“Wish 输入质量检查（lint/validate）”这一显式步骤
本次 Wish 一开始就出现：`wishId` 未从模板替换、正文重复段落。

如果不先做输入去噪：
- 目标树/问题列表会被污染（重复节点、错误 ID）
- DocGraph/追溯性可能在后期才爆炸（成本更高）

**建议补充到 Leader Model**：在 `loadWish()/readStateRegisters()` 之后，增加一个轻量的 `validateInputs()`：
- 校验 wishId 与文件名一致性（或至少稳定唯一）
- 校验 produce 链接存在
- 去重/规范化（最少：标记重复段落为 issue）

### Gap 2：缺少“跨文档/跨层语义冲突”的显式处理位置
这里出现 Wish Non-Goals 与 rbf-interface 的 Null 语义冲突。

模型虽有 `isMeltdownSignal/handleMeltdown`，但更偏向会议协作熔断；而这种冲突属于 **Rule-Tier 的约束冲突**，需要一个明确的“冲突登记 → 决策产出”的路径。

**建议补充到 Leader Model**：在 `identifyDemand()` 中加入一个分支：
- 若发现 SSOT 间矛盾（Wish vs 规范/接口契约），优先产生 `Decide/Clarify` demand，并要求 deliverable 为 Decision Log。

### Gap 3：状态寄存器的“落点目录/命名约定”未定义
模型提出 `project-status/goals.md`、`project-status/issues.md`，但没规定：
- 这套状态寄存器是“每个 Wish 一套”，还是“每个项目一套”？
- 放在仓库哪里（agent-team 还是 atelia）？

这会导致 Leader 初始化阶段无法形成统一结构，进而影响聚合与复用。

**建议补充到 Leader Model**：给出一个最小约定（例如：
- `agent-team/projects/<wishId>/project-status/*` 作为跨 repo 的中立落点；
- 或者规定“状态寄存器跟随 Wish 所在 repo”。）

### Gap 4：工具型算子（如 DocGraph validate/fix）未纳入“贡献动作”集合
本次模拟里，DocGraph 很可能是最省 token 的“事实性工具动作”（链接/produce_by 对齐），但 Leader Model 的动作集合目前偏人工/LLM。

**建议补充到 Leader Model**：把“运行仓库工具（validate/generate）”纳入 Operators，并明确其 deliverable（例如：修复后的 frontmatter + 校验通过的报告）。

---

## 监护人反馈（方法论与本例取向）

### 方法论：把“冲突”视为拟合/优化问题
- Wish 可视为“目标数据点”；像 `rbf-interface.md` 这种文档更像当前“模型参数”。
- 真实世界经常多目标冲突，不存在单一 SSOT 能机械地压倒一切。
- 识别矛盾本身就是重要进展；接下来要做的是：把矛盾转写为可处理的问题（tradeoff/priority/minmax）。

### 本例：SizedPtr 是产品，RBF 是目标用户之一
- Wish-0004 以 SizedPtr 为准；RBF 当前设计稿是草稿，可在后续接入时调整。
- `Address64.Null = 0` 的语义 scope 属于 RBF 自己的接口设计；SizedPtr 类型本身不必承担“如何使用某个值域”的业务约定。
- 类比：`int` 不规定它是 0-based/1-based；约定属于使用者/协议。

### 工作台（未来演化方向）
- 倾向：每个 Wish 一个根目录，里面放状态寄存器与所有分析/实验/讨论文档；由 DocGraph 汇总与校验。

---

## 现场发现（待纳入后续 Wish/DocGraph 重构的需求）

### Finding A：缺少“按 Artifact-Tiers 分层的内部文档组”导致落笔困难与层级混写

现象（本次模拟中的直接症状）：
- 讨论产物一开始“无处写”，最后被迫写回到同一个 `atelia/docs/Data/Draft/SizedPtr.md`，导致层级混写。

监护人观点（重要）：
- 更合理的做法：把 `SizedPtr.md` 视为 Wish 的“外部附件/用户递来的照片”（初始意图），随后导入到内部形成一组按 Tier 分层的文档：
  - Resolve：用途/价值/工作量/风险分析
  - Shape：接口语义/命名/对外心智模型
  - Rule：功能边界/不变量/验收标准/值域与对齐约束
  - Plan：实现策略/伪代码/测试计划
  - Craft：代码与测试

结论：此问题先登记为后续重构任务（与“每个 Wish 一个目录”一起处理），当前继续推进 Wish-0004 到 M1。

---

### Iteration 3（执行：Plan/Craft 入口——最小实现与测试策略）

#### Snapshot（观察到的信号）
- **已收敛**：Length 语义、bit 分配、特殊值分层（已形成 Decision Log）
- **当前缺口**：缺少可运行的实现与单元测试；Wish 的 Acceptance Criteria 要求“单测支撑”
- **层级判断**：Plan → Craft
- **Mode**：收敛
- **Pressure**：指定（交付物 = `Atelia.Data` 源码 + `Atelia.Data.Tests` 单元测试 + 测试通过）

#### Demand（identifyDemand 输出）
- action: Implement + Test
- scope:
  1) 在 `atelia/src/Data` 增加 `SizedPtr` 实现（值类型、packed 打包/解包、Create/TryCreate、Contains、EndOffsetExclusive）
  2) 在 `atelia/tests/Data.Tests` 增加 `SizedPtrTests.cs`，覆盖 roundtrip、对齐、边界、FromPacked 不校验、Contains 半开区间
- deliverable: Patch（源码+测试）+ 测试运行结果
- definition_of_done:
  - `dotnet test` 通过（至少 Data.Tests）
  - P0/P1 边界测试覆盖就绪（见 QA 测试矩阵）
- stop_condition: 先做到“正确性与边界可验证”，不在本轮扩展更多便捷 API

#### matchContributor（能力池匹配）
- 贡献动作：最小原型/增量实现 + 测试/验证
- 推荐载体：`Implementer`（实现）、`QA`（测试矩阵与边界复核）、`Craftsman`（接口与不变量复核）

#### QA 提供的测试矩阵（摘要）
- Roundtrip（0,0 / 任意对齐 / MaxOffset / MaxLength）
- 对齐检查（TryCreate false + Create throws）
- MaxOffset/MaxLength +1 拒绝
- FromPacked 任意 ulong 可解包
- Contains 半开区间语义 + 0 length
- EndOffsetExclusive checked + 溢出拒绝

#### Blockers（本轮潜在阻塞点）
- “文档按 Tier 分组”的结构性改造暂缓（已登记 Finding A），不阻塞实现与测试。

#### 执行结果（Craft 落地）
- ✅ 已实现：`atelia/src/Data/SizedPtr.cs`
- ✅ 已测试：`atelia/tests/Data.Tests/SizedPtrTests.cs`
- ✅ 测试通过：`dotnet test tests/Data.Tests/Data.Tests.csproj -c Release`（total 86, failed 0）

#### Artifact Update（本轮沉淀）
- Craft-Tier：新增源码与测试（见上）
- Plan-Tier：QA 测试矩阵已写入 `atelia/docs/Data/Draft/SizedPtr.md` 的“测试计划（Plan-Tier 草案）”

#### 是否达成 M1（本 Wish 的阶段性终止）
- 当前对 Wish-0004 而言：核心实现与单测已具备，已可以继续推进“接入 RBF/替换 Address64”的更大范围工作。
- 若按“推进到无可行动”标准：尚未达成（仍有可行动的后续工作），但已完成一个清晰的内部里程碑：**从文档收敛到可运行代码 + 可验证测试**。

---

## 监护人对本次模拟的三点反馈（流程缺口）

### Feedback 1：Wish 目标边界
- Wish-0004 的目标是“在 Atelia.Data 实现 SizedPtr”。
- “接入 RBF / 替换 Address64”只是目标用户与优化方向之一，不属于本 Wish 的交付范围。

结论：可将 Wish-0004 判定为“已完成（Completed）”，后续接入/迁移应拆分为新的 Wish/Issue。

### Feedback 2：缺失 Code Review（质量闭环）

仅测试通过不足以作为 AI 编程的可靠完成判据，原因包括：
- 可能忽视/误读设计条款（上下文有限）
- 可能重复实现既有能力（没看见已有实现）
- 可能存在“我以为…”式幻觉（逻辑自洽但与项目约束不符）

需要显式的 Review→修订→再 Review 的迭代，直到 Review 未见明显问题。

### Feedback 3：缺失 API 易用性讨论（Shape-Tier 的盲区）

即使实现正确，API 的命名与易用性仍可能存在长期成本。
例如：
- 类型名 `SizedPtr` 下，属性 `Length` 是否应更贴近用户心智（如 `Size` / `LengthBytes` / `SpanLengthBytes`）？
- `Offset` 是否应叫 `Address`/`Ptr`/`OffsetBytes`？

结论：在进入 Craft 或完成前，应当有一次明确的 API Review（可由 Curator 主导）。


