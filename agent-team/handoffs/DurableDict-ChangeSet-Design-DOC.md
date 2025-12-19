# [DurableDict ChangeSet Design] Documentation Update

## 实现摘要

根据畅谈会决策（2025-12-19），修订了 `DurableHeap/docs/mvp-design-v2.md` 设计文档，落实 DurableDict 内存态 ChangeSet 的方案 C（双字典）设计。

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 设计文档修订

## 具体修改内容

### 1. 修改 4.4.1 节（ChangeSet 语义）

新增 **三层语义术语定义** 小节，明确区分：
- **Working State（工作状态）**：对外可读/可枚举的语义状态视图
- **ChangeSet（变更跟踪）**：记录变更的内部结构或算法
- **On-Disk Diff（序列化差分）**：Commit 时写入磁盘的增量记录

添加术语映射说明，避免"内存态"一词的歧义。

### 2. 修改 4.4.2 节（Dict 的 state diff）

- 新增 **DurableDict 实现方案：双字典（方案 C）** 小节
  - 描述 `_committed`、`_current`、`_isDirty` 三个核心字段
  - 说明 Commit 时通过比较两个字典生成 diff

- 修改 **三层语义与表示法约束** 小节
  - 用三层术语替换原有的"内存态"措辞
  - 澄清 Working State 使用标准字典，tombstone 仅在 diff 计算与序列化阶段出现

- 更新编码建议
  - 移除对 `DictBase.cs Tombstone` 单例的引用（该建议仅适用于方案 A/B）

### 3. 新增 4.4.3 节（DurableDict 不变式与实现规范）

**核心不变式（MUST）**：
1. Working State 纯净性
2. Delete 一致性
3. Commit 失败不改内存
4. Commit 成功后追平
5. 隔离性
6. Key 唯一 + 升序
7. Canonical Diff（规范化）
8. 可重放性

**实现建议（SHOULD）**：
1. Fast Path（O(1) 无变更检测）
2. Clone 策略（深拷贝 → COW 演进路径）
3. 可观察性（HasChanges 属性）
4. DiscardChanges 支持

### 4. 新增 4.4.4 节（DurableDict 伪代码骨架）

包含完整的 C# 伪代码：
- 读 API：索引器、ContainsKey、TryGetValue、Count、Enumerate、HasChanges
- 写 API：Set、Delete
- 生命周期 API：Commit、DiscardChanges
- 内部方法：ComputeDiff、Clone

关键实现要点：
- Commit 中途失败的恢复策略
- 值相等性判断注意事项
- 线程安全声明（MVP 假设单线程）

### 5. 章节重编号

- 原 4.4.3 Commit(rootId) → 重命名为 **4.4.5**

## 源码对齐说明

| 畅谈会决策 | 文档落地 | 位置 |
|-----------|---------|------|
| 方案 C（双字典） | 4.4.2 DurableDict 实现方案 | 第 614-622 行 |
| Q1: Clone | 4.4.4 伪代码 Commit() | 第 810-815 行 |
| Q2: _isDirty flag | 4.4.3 实现建议 #1 + 4.4.4 伪代码 | 第 770-772 行 |
| Q3: 不写记录 | 4.4.3 不变式 #7 | 第 754-756 行 |
| 三层术语 | 4.4.1 三层语义术语定义 | 第 590-606 行 |
| 不变式列表 | 4.4.3 核心不变式 | 第 725-759 行 |

## 测试结果

- 文档结构验证：✅ 5 个 4.4.x 子节正确编号
- 术语一致性：✅ "内存态"歧义已消除
- 伪代码语法：✅ C# 语法正确

## 已知差异

无。本次修订严格遵循畅谈会决策。

## 遗留问题

1. **DictBase.cs 代码清理**：原 4.4.2 中引用的 `Tombstone` 单例（用于方案 A/B）可能需要在实现时移除或重构
2. **EpochMap 适配**：4.4.4 伪代码针对通用 DurableDict，EpochMap 作为特化可能需要额外处理

## Changefeed Anchor

`#delta-2025-12-19-durabledict-changeset-design`
