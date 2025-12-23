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
