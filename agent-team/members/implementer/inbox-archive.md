# Inbox Archive — 已处理便签归档

> 由 MemoryPalaceKeeper 归档的历史便签。

---

## 归档 2025-12-24

### 便签 2025-12-24 10:30

**文档精简技巧**：处理规范文档中的重复内容时的策略：
1. **识别权威来源**：确定哪个章节是规则的"规范性定义"（SSOT）
2. **引用替代重复**：其他位置只保留条款声明 + 引用链接，不重述规则
3. **代码注释精简**：当规范文档已有详细描述时，代码中的注释可精简为"见 §X.Y"
4. **switch 表达式简化**：C# 的 switch 表达式可以用元组模式 `(a, b) switch` 替代多层 if-else

**处理结果**：APPEND 到 index.md 核心洞见/方法论 #6

---

### 便签 2025-12-24 13:45

**表格合并策略（StateJournal mvp-design-v2.md §3.1.0.1）**：

将对象状态转换的两个独立表格合并为一个完整表格时的做法：
1. **添加"备注"列**：原来两个表格的差异信息（如 Crash 行为、状态转换来源）放入备注列
2. **统一状态名称**：使用枚举值名称（TransientDirty/PersistentDirty）而非描述性短语（"Dirty (Transient)"）
3. **脚注解释通配符**：表格中使用 `*Dirty` 表示多种状态，在脚注中说明
4. **代码注释补充定义**：将独立的"状态对比表"合并到状态枚举的代码注释中，减少文档碎片化
5. **保持状态机图完整**：Mermaid 图是最清晰的可视化，保留且与合并后的表格对应

**处理结果**：APPEND 到 index.md 核心洞见/方法论 #7

---

## 归档 2025-12-25

### 便签 2025-12-25 19:00

**RBF StatusLen 边界确定问题——根因澄清**

之前便签中"固有歧义"的表述不够准确。经分析根因：HeadLen/TailLen 记录的是 FrameBytes 总长度，而非 PayloadLen。由于 StatusLen 公式基于取模运算，给定 `(PayloadLen + StatusLen)` 的和，无法反推出具体的 PayloadLen——丢失了低 2 位信息。

候选改进方案：A) HeadLen 改记 PayloadLen; B) FrameStatus 编码 StatusLen（推荐）; C) StatusLen 固定为 4; D) 保持现状。

**处理结果**：APPEND 到 index.md 方法论 #8，详情写入 meta-cognition.md

---

### 便签 2025-12-25 18:30

**T-P1-05 IRbfScanner/逆向扫描 实现要点**

- PayloadLen 边界确定：枚举 StatusLen + CRC 消歧
- 测试设计：非零 Payload 避免歧义，非法 FrameStatus 全解释失败
- Span 与 yield 不兼容：改用 List<T> 收集

**处理结果**：APPEND 到 index.md 方法论 #9，详情写入 meta-cognition.md

---

### 便签 2025-12-25 17:25

**T-P1-04 IRbfFramer/Builder 实现要点**

- ref struct + lambda 约束解决方案
- CRC 覆盖范围精确实现
- Auto-Abort 语义实现
- Genesis Fence 可选参数

**处理结果**：APPEND 到 index.md 方法论 #10，详情写入 meta-cognition.md

---

### 便签 2025-12-25 10:30

**ASCII Art 修订规范合规（spec-conventions v0.3）**

- VarInt 图：加 Informative 标注
- FrameTag：改为 Visual Table
- Two-Phase Commit：改为 Mermaid sequenceDiagram

**处理结果**：APPEND 到 index.md 方法论 #11，详情写入 meta-cognition.md

---
