# Inbox Archive — 已处理便签归档

> 这是已处理便签的归档。由 MemoryPalaceKeeper 维护。

---

## 归档 2025-12-23

### 便签 2025-12-23 15:32

规格文档的"冗余"常见不是字多，而是同一事实被写成三种载体（段落叙述/表格/伪代码/图）。更稳的瘦身法：选定唯一 SSOT 表达（对二进制格式通常是字段表+少量算法条款），其余载体一律降级为 Informative 并强制引用 SSOT 条款 ID，不允许再引入新约束。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-23 00:08

RBF 规格消冗余时的审计优先级：先修"事实错误/排版断裂"（否则会把错误固化为 SSOT），再做重复引用收敛；并用一条元规则钉死 Normative vs Informative 边界（示例/推导/流程表默认 Informative，除非显式声明），以降低派生数据漂移导致的实现分叉。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-23 12:00

在"理想 Team Leader"讨论里，我把 Claude 的 Navigator（信息/意图层）与 Gemini 的 Campfire/Lens（能量/稳定性层）收敛为可审计的条款集合：关键不在隐喻本身，而在 **Tempo 的可判定切换条件** 与 **整合者独立判断力的制度化分离（Advice vs Leader Belief）**。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

## 归档 2025-12-24

### 便签 2025-12-23 00:00

RBF 规格里的"RBF 不解释语义"属于高风险模糊句：在存在 Padding tombstone / Auto-Abort 等机制时，RBF 实际上必须解释至少一个保留值。需要把这句话改写为可判定元规则：RBF 不解释业务 payload 语义，但可以解释 framing/维护语义（例如可丢弃帧），并明确 unknown/extension 的处理策略，否则会诱发实现分叉。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-23 23:55

RBF "移除头部 Padding 墓碑"论证的成败取决于对底层 writer 能力模型的规范化：Reservation-Rollback、Seek-back Patch、Neither 三类能力不能在规范里混作"有/无回填能力"。若不把"只支持 Reservation-Rollback（且 open builder 期间不得外泄/Flush 语义必须写死）"写成条款，监护人的二分论证在规范层面不成立，并会诱发实现分叉与不可判定测试面。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-24 00:00

RBF v0.12 变更（Pad→FrameStatus、1-4B、0x00/0xFF、全字节同值）会同时影响：长度公式、CRC 覆盖范围与"最小可解析帧"向量。更新 Layer 0 测试向量时，应顺带清理掉未在 rbf-format.md 定义的内容（例如 varint、上层 meta 恢复用例与条款映射），否则会造成"测试向量宣称的 SSOT"与格式规范脱钩。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

## 归档 2025-12-25

### 便签 2025-12-24 00:00

在"ObjectKind 编码进 FrameTag"的提案里，Layer 0（rbf-format）通常无需改动；真正的分叉风险来自 Layer 1 的双 SSOT：mvp-design-v2 的 FrameTag/record 布局 与 rbf-interface 的 `[S-STATEJOURNAL-FRAMETAG-MAPPING]` 若不同步，会让实现者在"固定常量映射 vs 位段编码"之间分裂。把 FrameTag 位布局提升为明确条款（并规定非 ObjVer 的 SubType 必须为 0、未知值 fail-fast）是最小可审计闭环。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-24 15:30

本次 FrameTag 16/16 位段编码修订的主要风险不是编码本身，而是"跨文档重复定义导致漂移"：1) rbf-interface.md 仍然正文定义了 `[S-STATEJOURNAL-TOMBSTONE-SKIP]`，但又声称该条款已移至 mvp-design-v2.md；属于自相矛盾，且破坏 Layer 0/1 边界。2) rbf-interface.md 顶部版本号仍是 0.5，但变更日志已有 0.6/0.7；会导致引用与审计失真。3) rbf-interface.md 示例代码使用 `frame.Status`，但 `RbfFrame` 结构未暴露 Status 字段；属于契约/示例断裂。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-24 16:10

语义化锚点（`[F-...]`/`[A-...]`/`[S-...]`/`[R-...]`）应只承载"规范性条款"的稳定引用键；把实现策略/方案代号（如"方案 C：双字典 _committed + _current"）用新的 `[I-...]` 前缀混入同一命名空间，会模糊 Normative vs Informative 边界并破坏索引/测试映射的一致性。若需要引用实现说明，优先用描述性文本或独立的非条款式锚点机制；并在锚点 token 里避免过泛词（如 `DICT`），用更精确的受控词（如 `DURABLEDICT`）降低未来扩展时的歧义/碰撞风险。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-24 17:20

"Claude 的拓扑分类（树/图/序列/矩阵）"与"Gemini 的维度测试（1D/2D/ND）+ 降级原则"并不冲突：前者用于定义信息类型全集，后者用于作者选型与防止 Mermaid/图表滥用。把它们条款化时，关键是把冲突点收敛为一个可审计不变量：同一事实只能有一个 SSOT 表示；一旦 Mermaid 被选为 SSOT，就禁止再维护等价 ASCII 图（否则必然双写漂移）。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-24 18:05

在"辅助皮层（Auxiliary Cortex）"讨论里，最容易走偏的点是把它等同于 RAG/Tool-Use。更稳的工程定义是：AuxCortex = **可验证的结构化中间表示（IR）层**，把"注意力焦点→可用操作（affordance）→可验证结果→可回放历史"协议化。落地优先级建议：先做 AnchorTable（短句柄+epoch/TTL/scope 校验）+ Projection（可再生视图）+ Audit Log（可回放），再做花哨的 HUD/主动推送。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-24 19:10

"Intent IR"与"Agent HUD"应被视为同一闭环的两半：IR 提供可组合、可验证、可版本化的符号系统；HUD 提供本体感与 affordance（示能）。工程落点是一个可审计循环：Focus Signal → Context Projection（带 Anchor）→ Intent IR → DryRun/Verify/Apply → Audit/Replay。三大组件映射也应固定：DocUI=投影与界面协议，StateJournal=可回放持久化（artifact），PipeMux=有状态执行底座（domain cortices）。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

### 便签 2025-12-25 00:20

"提问者/DM"工程化落地的关键不是更强的语言能力，而是把它的输出协议化为可审计的控制信号：`QuestionTurn = question_text + intent + affordance + anchors + tension (+ success_criteria)`。只有当每次提问都携带证据锚点与示能类型，系统才可做离线打分、偏好学习、RL credit assignment，以及在集成层做 guardrail（防无锚点高张力操控、反空转）。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

## 归档 2025-12-25 (第二批)

### 便签 2025-12-25 11:40

"自然语言关系描述"若要进入规范，必须被条款化为可判定的结构（Relation Triples List）：SVO 单条关系一行、谓词加粗、nodes/edges 阈值触发升级（表格/mermaid），并明确行内箭头只用于线性流程而非依赖语义，避免产生新的主观灰区。命名上优先与现有 `RELATIONS-AS-TABLE` 对偶（`RELATIONS-AS-TEXT`），若用 `HETEROGENEOUS` 必须写入可数判定条件（谓词种类 ≥ 2）。

**处理结果**：APPEND 到 index.md 洞察记录（Knowledge-Discovery）

---

## 归档 2025-12-26

### 便签 2025-12-26 21:57

在"规范驱动代码审阅"里，Claude 的 L1/L2/L3 作为解释框架成立，但作为可执行 Recipe 会因不可判定场景频发而混层；需要显式引入 `U`（Underspecified/Ambiguous）裁决域，并把 Finding 结构化为 `ClauseId + Evidence + VerdictType(V/U/C/I) + ProposedAction + 可复现验证步骤`，同时明令禁止"实现倒灌"导致 spec creep。流式触发必须配去抖/去重与成本控制，否则会变成高噪音提醒系统。

**处理结果**：MERGE 到 index.md 核心洞见（与便签 2 合并）

---

### 便签 2025-12-26 22:41

本次把"规范驱动代码审阅 Recipe"的落地风险收敛为 QC/验收项：统一机器枚举 `verdictType: V|U|C|I`（渲染层可自由）、`U` 禁止携带 `severity` 且必须输出澄清问题与最小修订案；Finding 必须强制 Evidence Triad 的"可复现/可验证"字段；流式触发要有可重算 `dedupeKey`；审阅必须记录 `specRef`（版本锁定）以避免复核漂移；并用 Parseability/Signal-to-Noise/Coverage/Closure/Governance 五指标定义首审"Recipe 可用"。

**处理结果**：MERGE 到 index.md 核心洞见（与便签 1 合并）

---
