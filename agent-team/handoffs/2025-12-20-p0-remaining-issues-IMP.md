# [DurableHeap MVP v2] P0 剩余问题修订 Implementation Result

> **Implementer Handoff**
> **日期**：2025-12-20
> **任务**：实施 DurableHeap MVP 文档中剩余的 P0 问题修订
> **依据**：用户请求（P0-4, P0-6, P0-7）

---

## 实现摘要

完成了 DurableHeap MVP 设计文档 v2 中三个剩余 P0 问题的修订：Value 类型收敛、Commit API 命名、首次 commit 语义。共修改 6 处内容并新增 1 个章节。

---

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 6 处修改 + 1 个新章节

---

## 详细修订内容

### P0-4: Value 类型收敛

**修订要点**：
- MVP 仅支持：`null`, `varint`（整数）, `ObjRef(ObjectId)`, `Ptr64`
- 移出 MVP：`float`, `double`, `bool`

**修改位置**：

1. **4.1.4 类型约束章节**（Line 341）：
   - 原表格：`int`, `long`, `ulong`, `float`, `double`, `bool`, `null`
   - 新表格：`null`, `varint`（整数：`int`, `long`, `ulong`）, `ObjRef(ObjectId)`, `Ptr64`
   - 添加 MVP 限制说明

2. **4.4.2 ValueType 枚举**（Line 822）：
   - 添加 MVP 范围说明，确认只保留 5 种类型

### P0-6: Commit API 命名

**修订要点**：
- `Commit(rootId)` 改为 `CommitAll(newRootId)`
- 消除 Scoped Commit 的误解

**修改位置**：

1. **4.4.5 章节标题**（Line 1056）：
   - `#### 4.4.5 Commit(rootId)` → `#### 4.4.5 CommitAll(newRootId)`
   
2. **参数说明**（Line 1058-1060）：
   - 更新参数名为 `newRootId`
   - 添加命名说明：解释为何使用 `CommitAll` 而非 `Commit`

### P0-7: 首次 commit 语义

**修订要点**：
- 空仓库：`Epoch=0`, `NextObjectId=1`
- 新建对象首版本：`PrevVersionPtr=0`

**修改位置**：

1. **4.1.1 ObjectId 分配**（Line 299）：
   - 添加"空仓库初始状态（MVP 固定）"段落

2. **4.3.1 Open 章节**（Line 655）：
   - 添加"空仓库边界（MVP 固定）"段落

3. **新增 4.4.6 章节**（Line 1100-1117）：
   - "首次 Commit 与新建对象"
   - 包含三个子节：空仓库初始状态、首次 Commit、新建对象首版本

---

## 新增章节内容

### 4.4.6 首次 Commit 与新建对象

```markdown
##### 空仓库初始状态

- `Open()` 空仓库时：
  - `Epoch = 0`（隐式空状态，无 HEAD commit record）
  - `NextObjectId = 1`（`ObjectId = 0` 保留给 VersionIndex 或其他 well-known 对象）
  - `RootObjectId = null`（无 root）

##### 首次 Commit

- 首次 `CommitAll(newRootId)` 创建 `Epoch = 1` 的 MetaCommitRecord
- 此时 VersionIndex 写入第一个版本（`PrevVersionPtr = 0`）

##### 新建对象首版本

- 新对象的**首个版本**其 `PrevVersionPtr = 0`
- `DiffPayload` 语义上为 "from-empty diff"（所有 key-value 都是 Upserts）
- wire format 与 Checkpoint Version 相同，但概念上是"创世版本"
```

---

## 测试结果

- **语法检查**：文档 Markdown 格式正确
- **一致性验证**：
  - ✅ 4.1.4 类型约束与 4.4.2 ValueType 枚举一致
  - ✅ 4.1.1 空仓库初始状态与 4.3.1 空仓库边界一致
  - ✅ 4.4.5 CommitAll 与 4.4.6 首次 Commit 语义一致
  - ✅ 新建对象 PrevVersionPtr=0 与 Checkpoint Version 规格兼容

---

## 遗留问题

无。P0-4、P0-6、P0-7 修订均已完成。

---

## QA 关注点

1. **Value 类型边界**：实现时需确保只支持 5 种 ValueType，其他类型应抛出异常
2. **空仓库边界**：`LoadObject(id)` 在空仓库时应返回"对象不存在"
3. **首次 Commit**：确保 `Epoch` 从 0 跳到 1，`NextObjectId` 从 1 开始分配
4. **新建对象**：首版本必须 `PrevVersionPtr = 0`

---

## Changefeed Anchor

`#delta-2025-12-20-p0-remaining`

---

*Handoff 由 Implementer 产出，2025-12-20*
