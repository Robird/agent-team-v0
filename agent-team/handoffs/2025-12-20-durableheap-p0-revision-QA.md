# DurableHeap MVP 文档第四轮共识修订验证报告

> **日期**：2025-12-20  
> **验证员**：QA (Claude Opus 4.5)  
> **验证对象**：
> - 设计文档：`DurableHeap/docs/mvp-design-v2.md`
> - 测试向量：`DurableHeap/docs/mvp-test-vectors.md`
> - 会议共识：`agent-team/meeting/2025-12-19-durableheap-mvp-review.md`（第四轮共识）

---

## 验证结果

| P0 | 问题 | 状态 | 备注 |
|----|------|------|------|
| 1 | `_isDirty` 实现改为 `ISet<ulong> _dirtyKeys` | ✅ | 术语表 L27, L76；4.4.1 L732-748；4.4.3 不变式 L888-891；4.4.4 伪代码 L919-963 |
| 2 | Magic 结构改为 Magic-as-Separator | ✅ | 4.2.1 L403 明确声明；L485 设计收益说明 |
| 3 | `DataTail` 定义为 `DataTail = EOF`（含尾部 Magic） | ✅ | 4.2.2 L538 明确定义 |
| 4 | Value 类型收敛到 null/varint/ObjRef/Ptr64 | ✅ | 4.1.4 L343 支持类型表；L346 MVP 限制说明；4.4.2 L816-820 ValueType 枚举 |
| 5 | Dirty Set 卡住问题由 #1 解决 | ✅ | `_dirtyKeys` 机制在 diff 为空时自动为空，不会卡住 |
| 6 | Commit API 改名为 `CommitAll(newRootId)` | ✅ | 4.4.5 章节标题 L1056；L1060 命名说明 |
| 7 | 首次 commit 空仓库 Epoch=0, NextObjectId=1 | ✅ | 4.1.1 L299-302；4.3.1 L658-659；新增 4.4.6 L1100-1118 |

---

## 详细验证记录

### P0-1: `_dirtyKeys` 实现

**检查点**：术语表、4.4.1、4.4.3、4.4.4 伪代码

**验证结果**：

1. **术语表**（L27）：
   ```
   | **ChangeSet** | ... | 方案 C: `ComputeDiff()` + `_dirtyKeys` |
   ```

2. **术语表**（L76）：
   ```
   | **OnCommitSucceeded** | Finalize 阶段：追平内存状态（`_committed = _current`，`_dirtyKeys.Clear()`） |
   ```

3. **4.4.2 章节**（L732）：
   ```
   - `_dirtyKeys`：记录自上次 Commit 以来发生变更的 key 集合（`ISet<ulong>`）。
   ```

4. **4.4.2 `_dirtyKeys` 维护规则**（L736-748）：完整的维护规则，包括：
   - `HasChanges = _dirtyKeys.Count > 0`
   - Upsert 时的 Add/Remove 逻辑
   - Delete 时的 Add/Remove 逻辑

5. **4.4.3 `_dirtyKeys` 不变式**（L888-891）：
   ```
   9. **_dirtyKeys 精确性**：`_dirtyKeys` 必须精确追踪所有与 `_committed` 存在语义差异的 key。
      - 对任意 key k：`k ∈ _dirtyKeys` ⟺ `CurrentValue(k) ≠ CommittedValue(k)`
   ```

6. **4.4.4 伪代码**（L919-963）：完整的 `UpdateDirtyKey()` 方法实现

---

### P0-2: Magic-as-Separator

**检查点**：4.2.1、4.2.2、Len 定义、reverse scan

**验证结果**：

1. **4.2.1 定义**（L403）：
   ```
   **Magic 是 Record Separator（记录分隔符），不属于任何 Record。**
   ```

2. **文件结构**（L405-407）：
   - 空文件：`[Magic]`（仅分隔符）
   - 含 N 条 Record：`[Magic][Record1][Magic][Record2]...[RecordN][Magic]`

3. **写入规则**（L408-410）：
   - 空文件先写一个 `Magic`
   - 每写完一条 Record 后追加一个 `Magic`

4. **Record 格式**（L411）：
   - `[Len(u32 LE)] [Payload bytes] [Pad(0..3)] [Len(u32 LE)] [CRC32C(u32 LE)]`
   - **Record 本身不包含 Magic**

5. **Len 定义**（L415-420）：明确 `HeadLen = 4 + PayloadLen + PadLen + 4 + 4`（不包含分隔符 Magic）

6. **reverse scan 算法**（L429-441）：已更新为 `MagicPos = PrevMagicPos` 继续

7. **设计收益说明**（L485-492）：列出 4 点收益

---

### P0-3: `DataTail` 定义

**检查点**：4.2.1、4.2.2

**验证结果**：

**4.2.2 meta payload 字段**（L538）：
```
- `DataTail`（Ptr64，定长 u64 LE：data 文件逻辑尾部；`DataTail = EOF`，**包含尾部分隔符 Magic**）
```

---

### P0-4: Value 类型收敛

**检查点**：4.1.4、4.4.2 ValueType

**验证结果**：

1. **4.1.4 类型约束表**（L341-344）：
   | 类别 | MVP 支持 | MVP 不支持 |
   |------|----------|------------|
   | **值类型** | `null`, `varint`（整数：`int`, `long`, `ulong`）, `ObjRef(ObjectId)`, `Ptr64` | `float`, `double`, `bool`, 任意 struct、用户自定义值类型 |

2. **4.1.4 MVP 限制说明**（L346）：
   ```
   > **MVP 限制说明**：`float`, `double`, `bool` 类型将在后续版本支持，不属于 MVP 范围。
   ```

3. **4.4.2 ValueType 枚举**（L816-820）：
   ```
   - `Val_Null = 0x0`
   - `Val_Tombstone = 0x1`
   - `Val_ObjRef = 0x2`
   - `Val_VarInt = 0x3`
   - `Val_Ptr64 = 0x4`
   ```

4. **4.4.2 MVP 范围说明**（L822）：
   ```
   > **MVP 范围说明**：MVP 仅实现以上 5 种 ValueType。`float`/`double`/`bool` 等类型将在后续版本添加对应的 ValueType 枚举值。
   ```

---

### P0-5: Dirty Set 卡住问题

**检查点**：4.4.3 不变式

**验证结果**：

通过 `_dirtyKeys` 机制解决。当 `ComputeDiff()` 为空时，`_dirtyKeys.Count == 0`，`HasChanges == false`，对象自动从 Dirty Set 移除。

相关实现在 4.4.4 伪代码（L974-978）：
```csharp
public bool WritePendingDiff(IRecordWriter writer) {
    if (_dirtyKeys.Count == 0) return false;  // Fast path: O(1)
    ...
}
```

---

### P0-6: Commit API 命名

**检查点**：4.4.5 章节标题和内容

**验证结果**：

1. **章节标题**（L1056）：
   ```
   #### 4.4.5 CommitAll(newRootId)
   ```

2. **命名说明**（L1060）：
   ```
   > **命名说明**：使用 `CommitAll` 而非 `Commit` 以消除歧义——本 API 提交 Dirty Set 中的所有对象，而非仅提交 root 可达的对象（Scoped Commit）。
   ```

---

### P0-7: 首次 Commit 与空仓库

**检查点**：4.1.1、4.3.1、新增 4.4.6

**验证结果**：

1. **4.1.1 空仓库初始状态**（L299-303）：
   ```
   **空仓库初始状态**（MVP 固定）：

   - `Epoch = 0`：隐式空状态，表示无 HEAD commit record
   - `NextObjectId = 1`：`ObjectId = 0` 保留给 VersionIndex 或其他 well-known 对象
   - `RootObjectId = null`：无 root
   ```

2. **4.3.1 Open 空仓库边界**（L654-663）：
   ```
   **空仓库边界**（MVP 固定）：

   - 若 meta 文件为空（仅包含 Magic 分隔符）或不存在有效的 `MetaCommitRecord`：
     - `Epoch = 0`（隐式空状态）
     - `NextObjectId = 1`
     - `RootObjectId = null`
     - `VersionIndexPtr = null`（无 VersionIndex）
   - 此时 `LoadObject(id)` 对任意 id 都应返回"对象不存在"。
   ```

3. **新增 4.4.6 章节**（L1100-1120）：完整的"首次 Commit 与新建对象"章节，包括：
   - 空仓库初始状态
   - 首次 Commit 语义
   - 新建对象首版本

---

## 测试向量验证

| 测试用例组 | 状态 | 数量 |
|------------|------|------|
| `_dirtyKeys` 不变式测试（DIRTY-xxx） | ✅ 新增 | 5 个用例 |
| 首次 Commit 语义（FIRST-COMMIT-xxx） | ✅ 新增 | 3 个用例 |
| Value 类型边界（VALUE-xxx） | ✅ 新增 | 2 组用例（OK/BAD） |
| CommitAll API 语义（COMMIT-ALL-xxx） | ✅ 新增 | 2 个用例 |
| ELOG framing（ELOG-xxx） | ✅ 更新 | 多个用例（Magic-as-Separator 语义） |

---

## 内部一致性检查

| 检查项 | 状态 | 备注 |
|--------|------|------|
| 章节编号连续性 | ✅ | 4.4.1 → 4.4.2 → 4.4.3 → 4.4.4 → 4.4.5 → 4.4.6 |
| 术语表与正文一致 | ✅ | `_dirtyKeys`、`CommitAll`、Magic-as-Separator 全文一致 |
| 交叉引用有效性 | ✅ | 4.4.3 引用 4.4.4、4.4.5 引用 4.4.4 等 |

---

## 结论

**验证通过**：所有 7 个 P0 问题均已正确修订，文档内部一致性良好，测试向量已同步更新。

---

## Changefeed Anchor

`#delta-2025-12-20-p0-revision`
