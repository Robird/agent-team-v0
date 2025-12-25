# Inbox Archive — 已处理便签归档

> 由 MemoryPalaceKeeper 归档的历史便签。

---

## 归档 2025-12-26 (Phase 4)

### 便签 2025-12-26 14:30

**T-P4-01/02 完成 — IdentityMap + DirtySet**

实现了 Workspace 的两个核心基础设施：
1. IdentityMap 幂等添加技巧：同一对象重复 Add 时 no-op（`ReferenceEquals` 检查）
2. WeakReference GC 测试模式：`[MethodImpl(NoInlining)]` + 三连 GC + `GC.KeepAlive` 放 Assert 后
3. 测试用例设计：GC 相关测试覆盖有/无强引用、Remove/Clear 后

**处理结果**：APPEND 到 index.md 方法论 #18

---

### 便签 2025-12-26 16:00

**T-P4-03 完成 — Workspace.CreateObject**

实现了 Workspace 核心的 `CreateObject<T>()` 方法：
1. 保留区处理：ObjectId 0-15 保留给 Well-Known 对象
2. 命名空间冲突解决：测试文件使用 type alias
3. 对象创建流程：分配 ObjectId → Activator.CreateInstance → 加入 IdentityMap + DirtySet

**处理结果**：APPEND 到 index.md 方法论 #19

---

### 便签 2025-12-26 17:30

**T-P4-04 完成 — Workspace.LoadObject**

实现了 `LoadObject<T>()` 方法，符合 `[A-LOADOBJECT-RETURN-RESULT]` 条款：
1. ObjectLoaderDelegate 委托注入模式
2. AteliaResult<T> nullable 双重解包
3. 加载流程三步走：查 IdentityMap → 调用 loader → 加入 IdentityMap
4. 新增 `ObjectTypeMismatchError`

**处理结果**：APPEND 到 index.md 方法论 #20

---

### 便签 2025-12-26 18:10

**T-P4-05 完成 — LazyRef&lt;T&gt; 实现**

实现了延迟加载引用结构体：
1. struct 内部存储状态机：null/ulong/T 三态
2. 两种构造模式：延迟加载 / 立即可用
3. 回填缓存机制
4. 新增错误类型：LazyRefNotInitializedError / LazyRefNoWorkspaceError / LazyRefInvalidStorageError

**处理结果**：APPEND 到 index.md 方法论 #21 + 项目状态表 OVERWRITE（Phase 4 完成）

---

## 归档 2025-12-26

### 便签 2025-12-25 23:58

**T-P2-02 VarInt 实现洞见**

1. Canonical 校验：解码完成后统一验证 `bytesConsumed == GetVarUIntLength(result)`
2. 第 10 字节特殊处理：uint64 前 9 字节覆盖 63 bit，第 10 字节只能有 1 bit 有效
3. ref struct lambda 限制：测试异常需改用 try-catch

**处理结果**：APPEND 到 index.md 方法论 #12

---

### 便签 2025-12-25 11:00

**T-P2-01 Address64/Ptr64 实现洞见**

1. 复用优先：Rbf 层已有实现，StateJournal 层只需扩展
2. global using 限制：类型别名只在定义项目内生效
3. 跨层依赖：返回 Result 的方法放在 StateJournal 层

**处理结果**：APPEND 到 index.md 方法论 #13

---

### 便签 2025-12-25 17:30

**T-P2-03 FrameTag 位段编码实现洞见**

1. 解释器模式：StateJournal 层提供解释器扩展方法
2. 位段公式：`FrameTag = (SubType << 16) | RecordType`
3. 验证优先级：先检 RecordType，再检 SubType/ObjectKind

**处理结果**：APPEND 到 index.md 方法论 #14

---

### 便签 2025-12-25 22:30

**T-P2-05 IDurableObject 接口设计洞见**

1. HasChanges 语义：Detached 状态返回 false
2. DiscardChanges 幂等：Clean/Detached 状态是 No-op
3. test double 技巧：`_wasTransient` 字段追踪历史状态

**处理结果**：APPEND 到 index.md 方法论 #15 + 项目状态表 OVERWRITE（Phase 2 完成）

---

### 便签 2025-12-26 10:30

**T-P3-01/02 DiffPayload 实现洞见**

1. 两阶段 Writer：收集阶段 + 序列化阶段
2. ref struct 泛型限制：改用 `out` 参数
3. Key delta 唯一性：delta=0 拒绝
4. stackalloc 循环警告：buffer 声明移到循环外

**处理结果**：APPEND 到 index.md 方法论 #16

---

### 便签 2025-12-26 15:00

**T-P3-03a DurableDict 双字典模型洞见**

1. Remove 无 tombstone：用 `_removedFromCommitted` 集合追踪
2. Set 恢复语义：需从 `_removedFromCommitted` 移除
3. 延迟枚举检查：拆分为 getter + Core 方法
4. 泛型构造函数类型问题

**处理结果**：MERGE 到 index.md 方法论 #17（DurableDict 综合）

---

### 便签 2025-12-26 16:00

**T-P3-04 _dirtyKeys 精确追踪洞见**

1. 语义一致性：`HasChanges ⟺ _dirtyKeys.Count > 0`
2. EqualityComparer<T>.Default 值比较
3. Remove 两种情况处理
4. 状态转换时机控制

**处理结果**：MERGE 到 index.md 方法论 #17（DurableDict 综合）

---

### 便签 2025-12-26 17:30

**T-P3-05 DiscardChanges 实现洞见**

1. 状态机：四种状态四种行为
2. 资源清理差异
3. State 属性 Detached 后仍可读

**处理结果**：MERGE 到 index.md 方法论 #17（DurableDict 综合）+ 项目状态表 OVERWRITE（Phase 3 完成）

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
