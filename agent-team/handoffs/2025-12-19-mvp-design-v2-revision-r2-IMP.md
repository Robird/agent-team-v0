# DurableHeap MVP Design v2 — 第二轮修订

## 实现摘要

根据 DocUIGPT 质检反馈，对 `DurableHeap/docs/mvp-design-v2.md` 进行第二轮修订，修正了 4 个术语精确性问题：消除歧义的"内存态"措辞、修正术语定义中的碰撞风险、统一 ChangeSet 语义描述、补充 NoChange 编码说明。

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 术语精确化修订

## 质检问题与修正对照

| # | 问题位置 | 问题描述 | 修正内容 |
|---|----------|----------|----------|
| 1 | 文档头/4.1.2/4.1.3/4.3.2 | 多处"内存态"措辞导致 Working State / Committed State / 对象实例混淆 | 全局替换为精确术语：`committed state`、`Working State（_current）`、`进程内对象实例` |
| 2 | 4.4.1 | "Materialized State"括注与 Materialize 输出称为 committed state 产生碰撞 | 移除"Materialized State"改为"Current State"；新增独立的"Committed State（已提交状态 / Baseline）"定义 |
| 3 | 4.4.1 | "每个内存对象维护一个 ChangeSet"与方案 C 的隐式 diff 算法描述不一致 | 改为"具有 ChangeSet 语义（可为显式结构或隐式 diff 算法）" |
| 4 | 4.4.2 | "三个状态：NoChange/Set/Delete"容易误导实现者在 payload 中编码 NoChange | 补充说明"`NoChange` 通过 diff 中缺失该 key 表达，不在 payload 中编码" |

## 具体修改明细

### 1. "内存态"全局替换

**Line 11（文档头）**：
```diff
- > - 反序列化后的内存态：**全量 materialize（但不级联 materialize 引用对象）**。
- > - 写入跟踪：每对象维护 **ChangeSet**（用于产生增量写入）。
- > - Dict 增量：内存形态与序列化形态都选 **state diff**（为了查询快、实现简单）。
+ > - 反序列化后的 committed state：**全量 materialize（但不级联 materialize 引用对象）**。
+ > - 写入跟踪：每对象具有 **ChangeSet 语义**（用于产生增量写入）。
+ > - Dict 增量：Working State（`_current`）与序列化形态都选 **state diff**（为了查询快、实现简单）。
```

**Line 237（4.1.2）**：
```diff
- - 对已 materialize 的对象：Resolve 固定返回同一个内存实例，不从磁盘覆盖内存态。
+ - 对已 materialize 的对象：Resolve 固定返回同一个内存实例，不从磁盘覆盖 Working State（`_current`）。
```

**Line 248（4.1.3）**：
```diff
- - **内存态表示（MVP 固定）**：对 `Val_ObjRef(ObjectId)`，materialize 的 committed state 中只存 `ObjectId`...
+ - **进程内对象实例表示（MVP 固定）**：对 `Val_ObjRef(ObjectId)`，materialize 的 committed state（`_committed`）中只存 `ObjectId`...
```

**Line 641（4.4.2）**：
```diff
- > 说明：此前"内存态：使用哨兵对象区分 tombstone"的建议...
+ > 说明：此前"ChangeSet 内部使用哨兵对象区分 tombstone"的建议，仅适用于显式 ChangeSet 结构的实现（如方案 A/B）。在方案 C 中，Working State（`_current`）使用标准字典...
```

### 2. 术语定义修正（4.4.1）

将三层术语扩展为四层，消除术语碰撞：

```markdown
1. **Working State（工作状态 / Current State）**
   - 定义：对外可读/可枚举的语义状态视图。
   - 在方案 C（双字典）中，体现为 `_current` 字典的内容。

2. **Committed State（已提交状态 / Baseline）**  [新增]
   - 定义：上次 Commit 成功时的状态快照；也是 Materialize（合成状态）的输出。
   - 在方案 C（双字典）中，体现为 `_committed` 字典的内容。

3. **ChangeSet（变更跟踪 / Write-Tracking）**
   - 在方案 C 中，ChangeSet 退化为"由 `_committed` 与 `_current` 两个状态做差得到"的隐式 diff 算法，不需要显式的数据结构。

4. **On-Disk Diff（序列化差分）**
```

术语映射更新：
```diff
- > 术语映射：本文档中提到的"内存态"若指对外可见状态，应理解为 Working State；若指写入跟踪结构，应理解为 ChangeSet。
+ > 术语映射：本文档中"Working State"指 `_current`，"Committed State"指 `_committed`（Materialize 的结果）。
```

### 3. ChangeSet 描述修正（4.4.1）

```diff
- - 每个内存对象维护一个 ChangeSet（Q13/Q14）：
+ - 每个内存对象具有 ChangeSet 语义（可为显式结构或隐式 diff 算法）（Q13/Q14）：
```

### 4. NoChange 编码说明（4.4.2）

```diff
- - 因此 value 编码至少需要三个状态：
- - `NoChange`：不出现在 diff 里
+ - 因此 value 在逻辑上至少需要三个状态：
+   - `NoChange`：不出现在 diff 里（**通过 diff 中缺失该 key 表达，不在 payload 中编码**）
```

## 验证结果

- `grep -n "内存态"` → 0 matches（全部替换完毕）
- 术语定义符合 4 层分层（Working State / Committed State / ChangeSet / On-Disk Diff）
- NoChange 编码说明与 Canonical Diff 约束一致

## 遗留问题

无。本轮修订仅涉及术语精确化，不影响技术规格。

## Changefeed Anchor

`#delta-2025-12-19-mvp-design-v2-r2`
