# 决策摘要：DurableDict 内存态 ChangeSet 设计

## 背景（2-3句）

DurableHeap MVP 设计草稿 (`DurableHeap/docs/mvp-design-v2.md`) 中，DurableDict 的序列化态用 tombstone 表示删除已确定，但内存态的 ChangeSet 实现方式未固化。三个候选方案（A: 内存 tombstone, B: Deleted 集合, C: 双字典）需要选择，以确保符合既有的"三层语义"约束并具备良好的开发者体验。

## 讨论过程

通过 runSubAgent 邀请三位 DocUI Specialist 进行两轮秘密基地畅谈：

- **DocUIClaude** (Claude Opus 4.5)：系统类比（LSM-Tree/Git/MVCC）、概念模型分析
- **DocUIGemini** (Gemini 3 Pro)：UX/DX 视角、WYSIWYG 调试原则（第二轮由 Claude 代为补充）
- **DocUIGPT** (GPT-5.2)：规范核查、不变式清单、措辞修正建议

完整讨论记录：[2025-12-19-durabledict-changeset-jam.md](../meeting/2025-12-19-durabledict-changeset-jam.md)

## 问题清单

| # | 问题 | 严重度 | 建议方案 |
|---|------|--------|----------|
| 1 | 方案 A（内存 tombstone）违反三层语义约束 | 🔴 高 | 出局 |
| 2 | 4.4.2 "内存态"措辞与三层语义冲突 | 🟡 中 | 修正为三层术语 |
| 3 | 缺少 dirty tracking 优化 | 🟢 低 | 添加 `_isDirty` flag |

## 推荐行动

### 确认的设计决策

- [x] **选择方案 C（双字典）**：`_committed` + `_current`，commit 时 diff
- [x] **Q1**: Commit 成功后 `_committed = Clone(_current)`（深拷贝）
- [x] **Q2**: 使用 `_isDirty` flag，暴露 `HasChanges` 属性
- [x] **Q3**: "新增后删除"不写记录（Canonical Diff）

### 需要写入设计文档的不变式

1. Working State 纯净性：tombstone 不得出现在可枚举视图
2. Commit 失败不改内存
3. Commit 成功后 CommittedState == CurrentState
4. 隔离性：Commit 后对 _current 的写入不影响 _committed
5. Canonical Diff：key 唯一 + 升序；不含 net-zero 变更

### 文档修订

- [ ] 修改 4.4.1/4.4.2：用 Working State / ChangeSet / On-Disk Diff 三层术语替换歧义的"内存态"
- [ ] 新增不变式小节
- [ ] 添加 DurableDict 伪代码骨架（参考 Claude 第二轮发言）

## 备选方案（如有分歧）

无分歧。三位 Specialist 一致推荐方案 C。

## 决策选项

- [x] 全部批准 ✅
- [ ] 部分批准（请标注）
- [ ] 需要更多信息
- [ ] 否决

---

**监护人批示**：已批准，安排修订文档并质检迭代。

**日期**：2025-12-19

**决策生效后**：由 Implementer 根据规范实现 DurableDict，由 QA 基于不变式设计 property tests。
