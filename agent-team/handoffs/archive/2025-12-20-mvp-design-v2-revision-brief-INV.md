# DurableHeap MVP v2 修订清单

> **Investigator Brief**
> **日期**：2025-12-20
> **任务**：基于 2025-12-19 畅谈会第四轮共识，识别 mvp-design-v2.md 需要修改的具体位置
> **依据**：`agent-team/meeting/2025-12-19-durableheap-mvp-review.md`（第四轮共识）

---

## 概述

根据 2025-12-19 畅谈会共识，需要修订 7 个 P0 问题。以下是每个问题在目标文档中的具体修改位置。

---

## P0-1: `_isDirty` → `_dirtyKeys`

### 修改点 1.1：术语表 ChangeSet 行

- **位置**：[mvp-design-v2.md#L18-L20](../../DurableHeap/docs/mvp-design-v2.md#L18-L20)（术语表"状态与差分"部分）
- **当前内容**：
  ```markdown
  | **ChangeSet** | 自上次 Commit 以来的累积变更（逻辑概念，可为隐式） | 机制描述: Write-Tracking | 方案 C: `ComputeDiff()` + `_isDirty` |
  ```
- **建议修改**：
  ```markdown
  | **ChangeSet** | 自上次 Commit 以来的累积变更（逻辑概念，可为隐式） | 机制描述: Write-Tracking | 方案 C: `ComputeDiff()` + `_dirtyKeys` |
  ```

### 修改点 1.2：术语表新增 `HasChanges` 定义

- **位置**：术语表"状态与差分"部分末尾（约 L21 后）
- **建议新增行**：
  ```markdown
  | **HasChanges** | 只读属性，表示当前状态与 Committed State 是否存在语义差异 | — | `_dirtyKeys.Count > 0` |
  ```

### 修改点 1.3：术语表 Dirty Set 定义补充

- **位置**：[mvp-design-v2.md#L38](../../DurableHeap/docs/mvp-design-v2.md#L38)（"载入与缓存"部分）
- **当前内容**：
  ```markdown
  | **Dirty Set** | Workspace 级别的 dirty 对象强引用集合，记录所有具有未提交修改的对象 | — | `_dirtySet` |
  ```
- **建议修改**（更新实现映射，与 `_dirtyKeys` 概念区分）：
  ```markdown
  | **Dirty Set** | Workspace 级别的 dirty 对象强引用集合，记录所有 `HasChanges=true` 的对象 | — | `HashSet<ObjectId>` |
  ```

### 修改点 1.4：对象级 API 表

- **位置**：[mvp-design-v2.md#L47-L49](../../DurableHeap/docs/mvp-design-v2.md#L47-L49)（"对象级 API（二阶段提交）"部分）
- **当前内容**：
  ```markdown
  | **OnCommitSucceeded** | Finalize 阶段：追平内存状态（`_committed = _current`，`_isDirty = false`） | — | `DurableDict.OnCommitSucceeded()` |
  ```
- **建议修改**：
  ```markdown
  | **OnCommitSucceeded** | Finalize 阶段：追平内存状态（`_committed = _current`，`_dirtyKeys.Clear()`） | — | `DurableDict.OnCommitSucceeded()` |
  ```

### 修改点 1.5：4.4.1 ChangeSet 语义 - 实现方案描述

- **位置**：[mvp-design-v2.md#L306-L311](../../DurableHeap/docs/mvp-design-v2.md#L306-L311)（"DurableDict 实现方案：双字典（方案 C）"小节）
- **当前内容**：
  ```markdown
  ##### DurableDict 实现方案：双字典（方案 C）

  依据畅谈会决策（2025-12-19），MVP 采用 **方案 C（双字典）**：

  - `_committed`：上次 Commit 成功时的状态快照。
  - `_current`：当前的完整工作状态（Working State）。
  - `_isDirty`：标记自上次 Commit 以来是否有写操作。

  Commit 时通过比较 `_committed` 与 `_current` 生成 diff，而不是维护显式的 ChangeSet 数据结构。
  ```
- **建议修改**：
  ```markdown
  ##### DurableDict 实现方案：双字典（方案 C）

  依据畅谈会决策（2025-12-19），MVP 采用 **方案 C（双字典）**：

  - `_committed`：上次 Commit 成功时的状态快照。
  - `_current`：当前的完整工作状态（Working State）。
  - `_dirtyKeys`：`ISet<ulong>`，记录所有与 committed 存在语义差异的 key。

  **不变式**：`key ∈ _dirtyKeys` 当且仅当 `CurrentValue(key) ≠ CommittedValue(key)`（包含新增/删除/改值）。

  **`HasChanges` 属性**：`_dirtyKeys.Count > 0`。

  Commit 时只需遍历 `_dirtyKeys` 生成 diff，复杂度为 O(dirtyKeys) 而非 O(n)。
  ```

### 修改点 1.6：4.4.3 核心不变式表

- **位置**：[mvp-design-v2.md#L340-L343](../../DurableHeap/docs/mvp-design-v2.md#L340-L343)（"核心不变式（MUST）"小节中的 Commit 语义不变式）
- **当前内容**：
  ```markdown
  4. **Commit 成功后追平**：Commit 成功返回后，必须满足 `CommittedState == CurrentState`（语义相等），并清除 `HasChanges`。
  ```
- **建议修改**：
  ```markdown
  4. **Commit 成功后追平**：Commit 成功返回后，必须满足 `CommittedState == CurrentState`（语义相等），并满足 `_dirtyKeys.Count == 0`（即 `HasChanges == false`）。
  ```

### 修改点 1.7：4.4.3 实现建议表

- **位置**：[mvp-design-v2.md#L352-L354](../../DurableHeap/docs/mvp-design-v2.md#L352-L354)（"实现建议（SHOULD）"小节）
- **当前内容**：
  ```markdown
  1. **Fast Path**：若自上次成功 Commit/Discard 以来没有任何写入操作（Set/Delete），则 `Commit()` 应为 $O(1)$ 并且不执行全量 diff。
     - 建议：维护 `_isDirty` flag，暴露为只读属性 `HasChanges`。
  ```
- **建议修改**：
  ```markdown
  1. **Fast Path**：若 `_dirtyKeys` 为空，则 `Commit()` 应为 $O(1)$ 并跳过 diff 计算。
     - 实现：`HasChanges == (_dirtyKeys.Count > 0)`。
  ```

### 修改点 1.8：4.4.4 伪代码骨架

- **位置**：[mvp-design-v2.md#L368-L420](../../DurableHeap/docs/mvp-design-v2.md#L368-L420)（完整伪代码块）
- **当前内容**（摘录关键部分）：
  ```csharp
  class DurableDict<K, V> : IDurableObject {
      private Dictionary<K, V> _committed;  // 上次 commit 时的状态
      private Dictionary<K, V> _current;    // 当前工作状态
      private bool _isDirty;                // 自上次 commit 后是否有写操作
      
      // ... 读 API ...
      public bool HasChanges => _isDirty;
      
      // ... 写 API ...
      public void Set(K key, V value) {
          _current[key] = value;
          _isDirty = true;
      }
      
      public bool Delete(K key) {
          var removed = _current.Remove(key);
          if (removed) _isDirty = true;
          return removed;
      }
      
      public bool WritePendingDiff(IRecordWriter writer) {
          if (!_isDirty) return false;  // Fast path: O(1)
          
          var diff = ComputeDiff(_committed, _current);
          if (diff.Count == 0) {
              // 实际没变化，但不在此处清标记——等待 OnCommitSucceeded()
              return false;
          }
          // ...
      }
      
      public void OnCommitSucceeded() {
          if (!_isDirty) return;
          _committed = Clone(_current);
          _isDirty = false;
      }
      
      public void DiscardChanges() {
          _current = Clone(_committed);
          _isDirty = false;
      }
  ```
- **建议修改**（完整替换伪代码）：
  ```csharp
  class DurableDict<K, V> : IDurableObject where K : IEquatable<K> {
      private Dictionary<K, V> _committed;   // 上次 commit 时的状态
      private Dictionary<K, V> _current;     // 当前工作状态
      private HashSet<K> _dirtyKeys = new(); // 与 committed 存在差异的 key 集合
      
      // ===== 读 API =====
      public V this[K key] => _current[key];
      public bool ContainsKey(K key) => _current.ContainsKey(key);
      public bool TryGetValue(K key, out V value) => _current.TryGetValue(key, out value);
      public int Count => _current.Count;
      public IEnumerable<KeyValuePair<K,V>> Enumerate() => _current;
      public bool HasChanges => _dirtyKeys.Count > 0;
      
      // ===== 写 API =====
      public void Set(K key, V value) {
          _current[key] = value;
          UpdateDirtyKey(key);
      }
      
      public bool Delete(K key) {
          var removed = _current.Remove(key);
          UpdateDirtyKey(key);
          return removed;
      }
      
      // 维护 _dirtyKeys 不变式
      private void UpdateDirtyKey(K key) {
          var currentValue = _current.TryGetValue(key, out var cv) ? (true, cv) : (false, default);
          var committedValue = _committed.TryGetValue(key, out var cmv) ? (true, cmv) : (false, default);
          
          bool isSame = currentValue.Item1 == committedValue.Item1 
                     && EqualityComparer<V>.Default.Equals(currentValue.cv, committedValue.cmv);
          
          if (isSame) {
              _dirtyKeys.Remove(key);  // 恢复原状，不再 dirty
          } else {
              _dirtyKeys.Add(key);     // 存在差异
          }
      }
      
      // ===== 生命周期 API（二阶段提交） =====
      
      public bool WritePendingDiff(IRecordWriter writer) {
          if (_dirtyKeys.Count == 0) return false;  // Fast path: O(1)
          
          var diff = ComputeDiffFromDirtyKeys();
          if (diff.Count == 0) {
              // 不应发生（_dirtyKeys 不变式应保证 diff 非空）
              return false;
          }
          
          WriteDiffTo(writer, diff);
          return true;
      }
      
      public void OnCommitSucceeded() {
          _committed = Clone(_current);
          _dirtyKeys.Clear();
      }
      
      public void DiscardChanges() {
          _current = Clone(_committed);
          _dirtyKeys.Clear();
      }
      
      // ===== 内部方法 =====
      private List<DiffEntry<K,V>> ComputeDiffFromDirtyKeys() {
          var result = new List<DiffEntry<K,V>>();
          
          foreach (var key in _dirtyKeys) {
              var inCurrent = _current.TryGetValue(key, out var currentVal);
              var inCommitted = _committed.TryGetValue(key, out var committedVal);
              
              if (inCurrent && !inCommitted) {
                  result.Add(DiffEntry<K,V>.Set(key, currentVal));  // 新增
              } else if (!inCurrent && inCommitted) {
                  result.Add(DiffEntry<K,V>.Delete(key));           // 删除
              } else if (inCurrent && inCommitted) {
                  result.Add(DiffEntry<K,V>.Set(key, currentVal));  // 改值
              }
              // else: 既不在 current 也不在 committed，不应发生
          }
          
          result.Sort((a, b) => Comparer<K>.Default.Compare(a.Key, b.Key));
          return result;
      }
      
      private Dictionary<K,V> Clone(Dictionary<K,V> source) {
          return new Dictionary<K,V>(source);
      }
  }
  ```

### 修改点 1.9：4.4.4 关键实现要点

- **位置**：[mvp-design-v2.md#L422-L430](../../DurableHeap/docs/mvp-design-v2.md#L422-L430)
- **当前内容**（要点 2）：
  ```markdown
  2. **值相等性判断**：`ComputeDiff` 依赖值的 `Equals` 方法。若 `V` 是引用类型且未正确实现 `Equals`，可能产生冗余 Set 记录。建议文档要求 `V` 实现 `IEquatable<V>`，或 MVP 使用 `ReferenceEquals`。
  ```
- **建议修改**：
  ```markdown
  2. **值相等性判断**：`UpdateDirtyKey` 依赖值的 `Equals` 方法判断当前值与 committed 值是否相同。MVP 类型收敛到 `null/varint/ObjRef/Ptr64` 后，相等性定义为"类型相同且数值相同"；`ObjRef` 以 `ObjectId` 相等。
  ```

---

## P0-2: Magic 结构定义

### 修改点 2.1：4.2.1 data file 的 record framing 描述

- **位置**：[mvp-design-v2.md#L152-L160](../../DurableHeap/docs/mvp-design-v2.md#L152-L160)（"record framing（Q20=A；data/meta 统一）"小节）
- **当前内容**：
  ```markdown
  record framing（Q20=A；data/meta 统一）：

  - data 与 meta 统一采用 `RBF` framing：
  	- `[Magic(4)] [Len(u32 LE)] [Payload bytes] [Pad(0..3)] [Len(u32 LE)] [CRC32C(u32 LE)]`
  	- `Len`（也称 `HeadLen`/`TailLen`）表示从 `Magic` 开始到 `CRC32C` 结束的 **整条 record 总字节长度**。
  	- ...

  并且引入文件级不变量：`Magic` 作为 record 分隔符（separator），文件开头与结尾都必须出现 `Magic`。

  - 逻辑结构：`[Magic][Record1][Magic][Record2][Magic]...`。
  	- 这里的最后一个 `Magic` 为"尾部哨兵"，它不属于任何 record（其后没有 `Len`）。
  ```
- **建议修改**（Magic 与 Record 并列，Record 不包含 Magic）：
  ```markdown
  record framing（Q20=A；data/meta 统一）：

  **Magic 是 Record Separator（记录分隔符），不属于任何 Record。**

  - **文件结构**：
    - 空文件：`[Magic]`（仅分隔符）
    - 含 N 条 Record：`[Magic][Record1][Magic][Record2]...[Magic]`
  - **写入规则**：
    1. 空文件先写一个 `Magic`
    2. 每写完一条 Record 后追加一个 `Magic`
  - **Record 格式**（Record 本身不包含 Magic）：
    - `[Len(u32 LE)] [Payload bytes] [Pad(0..3)] [Len(u32 LE)] [CRC32C(u32 LE)]`
  - `Len`（也称 `HeadLen`/`TailLen`）表示本条 Record 的总字节长度（从 `HeadLen` 开始到 `CRC32C` 结束）。
  - `Pad` 用 0 填充，使得下一个 `Magic` 的起点满足 4B 对齐。
  - `CRC32C` 覆盖范围：`Payload + Pad + TailLen`。
  ```

### 修改点 2.2：关于 `Len` 的精确定义

- **位置**：[mvp-design-v2.md#L164-L170](../../DurableHeap/docs/mvp-design-v2.md#L164-L170)
- **当前内容**：
  ```markdown
  关于 `Len` 的精确定义（MVP 固定，避免实现分歧）：

  - 头部的 `Len` 记为 `HeadLen`，尾部的 `Len` 记为 `TailLen`；两者必须满足 `HeadLen == TailLen`，否则视为损坏。
  - `HeadLen/TailLen` 的数值等于本条 record 的总字节数：
  	- `HeadLen = 4(Magic) + 4(HeadLen) + PayloadLen + PadLen + 4(TailLen) + 4(CRC32C)`。
  - 由于固定头尾总计 16 字节且要求 4B 对齐，`PadLen` 的 writer 计算方式可固定为：
  	- `PadLen = (4 - (PayloadLen % 4)) % 4`。
  - 最小长度：`HeadLen >= 16`；对 data/meta 的 MVP 记录，payload 至少包含 1 字节 kind，因此通常 `HeadLen >= 17`。
  ```
- **建议修改**（由于 Magic 不属于 Record，Len 不包含 Magic）：
  ```markdown
  关于 `Len` 的精确定义（MVP 固定，避免实现分歧）：

  - 头部的 `Len` 记为 `HeadLen`，尾部的 `Len` 记为 `TailLen`；两者必须满足 `HeadLen == TailLen`，否则视为损坏。
  - `HeadLen/TailLen` 的数值等于本条 Record 的总字节数（**不包含分隔符 Magic**）：
    - `HeadLen = 4(HeadLen) + PayloadLen + PadLen + 4(TailLen) + 4(CRC32C)`
  - 由于固定头尾总计 12 字节且要求 4B 对齐，`PadLen` 的 writer 计算方式可固定为：
    - `PadLen = (4 - (PayloadLen % 4)) % 4`
  - 最小长度：`HeadLen >= 12`；对 data/meta 的 MVP 记录，payload 至少包含 1 字节 kind，因此通常 `HeadLen >= 13`（对齐后 `HeadLen >= 16`）。
  ```

### 修改点 2.3：反向扫尾算法

- **位置**：[mvp-design-v2.md#L174-L190](../../DurableHeap/docs/mvp-design-v2.md#L174-L190)（"反向扫尾（reverse scan）所需不变量"小节）
- **当前内容**：
  ```markdown
  - 定义 `End = MagicPos`。
  	- 直观含义：`End` 是"上一条 record 的末尾"...
  - 若 `End == 0`：表示"没有任何 record"（空文件仅哨兵），扫描结束。
  - ...
  - 校验 `CRC32C`（覆盖 `Payload + Pad + TailLen`）。通过后，本条记录有效；下一轮令 `MagicPos = Start - 4`（上一条 record 的分隔符位置）继续。
  ```
- **建议修改**（基于 Magic-as-Separator 的算法）：
  ```markdown
  反向扫尾（reverse scan）所需不变量（MVP 固定）：

  - 初始 `MagicPos = FileLength - 4`（尾部分隔符位置）。
  - 定义 `RecordEnd = MagicPos`（当前分隔符 Magic 的起始位置 = 上一条 Record 的末尾）。
  - 若 `RecordEnd == 4`：表示"没有任何 Record"（文件仅 `[Magic]`），扫描结束。
  - 否则：
    - 从 `RecordEnd` 向前读取 `TailLen`（位于 `RecordEnd-8..RecordEnd-5`）与 `CRC32C`（位于 `RecordEnd-4..RecordEnd-1`）。
    - 计算 `RecordStart = RecordEnd - TailLen`。
    - 向前 4 字节定位前一个 `Magic`：`PrevMagicPos = RecordStart - 4`。
    - 验证：`PrevMagicPos` 处 4 字节等于 `Magic`。
    - 读取 `HeadLen`，验证 `HeadLen == TailLen`。
    - 校验 `CRC32C`（覆盖 `Payload + Pad + TailLen`）。
    - 通过后，本条 Record 有效；下一轮令 `MagicPos = PrevMagicPos` 继续。
  ```

### 修改点 2.4：写入顺序描述

- **位置**：[mvp-design-v2.md#L204-L218](../../DurableHeap/docs/mvp-design-v2.md#L204-L218)（"写入顺序（MVP 固定）"小节）
- **当前内容**：
  ```markdown
  - 规范化步骤（单条 record）：
  	0) 确保文件以 `Magic` 结束（新建空文件时先写入 4 bytes `Magic` 作为哨兵）。
  	1) 从当前文件尾部哨兵位置开始写入 record：写入 `Magic(4)`（可覆盖原哨兵 `Magic`，值相同）。
  	2) 写入 `HeadLen(u32)` 占位（先写 0）。
  	3) 顺序写入 `Payload`。
  	...
  ```
- **建议修改**（基于 Magic-as-Separator，Record 不包含 Magic）：
  ```markdown
  - 规范化步骤（单条 Record）：
    0) 确保文件以 `Magic` 结束（新建空文件时先写入 4 bytes `Magic`）。
    1) 在最后一个 `Magic` 之后开始写入 Record：写入 `HeadLen(u32)` 占位（先写 0）。
    2) 顺序写入 `Payload`。
    3) 写入 `Pad(0..3)`（全 0），使得 Record 结尾满足 4B 对齐。
    4) 写入 `TailLen(u32)`（此时已知总长度）。
    5) 计算 `CRC32C(Payload + Pad + TailLen)`，写入 `CRC32C(u32)`。
    6) 回填 `HeadLen(u32) = TailLen`。
    7) 追加写入 4 bytes `Magic` 作为新的分隔符。
  ```

### 修改点 2.5："关于尾部 Magic 哨兵"段落

- **位置**：[mvp-design-v2.md#L222-L226](../../DurableHeap/docs/mvp-design-v2.md#L222-L226)
- **当前内容**：
  ```markdown
  关于"尾部 `Magic` 哨兵"（MVP 采纳）：

  - 不改变单条 record 的 RBF framing（`Len/CRC32C/Pad` 规则不变），只是文件级额外要求"以 `Magic` 结束"。
  - 收益：fast-path 可 O(1) 命中 `MagicPos = FileLength-4`；尾部损坏时 resync 与 fast-path 复用同一套"命中 `Magic` → 校验 `Len/CRC32C`"流程。
  - 代价：每条 record 额外 4 bytes；writer 需维护哨兵不变量（对 MVP 单 writer + append-only 可接受）。
  ```
- **建议修改**：
  ```markdown
  **Magic 作为 Record Separator 的设计收益**（MVP 采纳）：

  - **概念简洁**：所有 Magic 的语义相同（分隔符），无需区分"Record 内的 Magic"和"尾部哨兵 Magic"。
  - **fast-path**：reverse scan 初始 `MagicPos = FileLength-4` 可 O(1) 命中。
  - **resync 统一**：尾部损坏时的 resync 与 fast-path 复用同一套"命中 Magic → 校验 Len/CRC32C"流程。
  - **代价**：每条 Record 前后各有一个 Magic（首 Record 前的 Magic 是文件头，后续 Record 共享前一条 Record 后的 Magic）。
  ```

---

## P0-3: `DataTail` 定义

### 修改点 3.1：4.2.2 Meta 文件 `DataTail` 描述

- **位置**：[mvp-design-v2.md#L245](../../DurableHeap/docs/mvp-design-v2.md#L245)（meta payload 字段描述）
- **当前内容**：
  ```markdown
  - `DataTail`（Ptr64，定长 u64 LE：data 文件逻辑尾；byte offset = `DataTail`）
  ```
- **建议修改**（明确 DataTail = EOF，包含尾部 Magic）：
  ```markdown
  - `DataTail`（Ptr64，定长 u64 LE：data 文件逻辑尾部；`DataTail = EOF`，**包含尾部分隔符 Magic**）
  ```

### 修改点 3.2：4.5 崩溃恢复

- **位置**：[mvp-design-v2.md#L459-L462](../../DurableHeap/docs/mvp-design-v2.md#L459-L462)
- **当前内容**：
  ```markdown
  - 以该 record 的 `DataTail` 截断 data 文件尾部垃圾（必要时）。
  ```
- **建议修改**（明确截断后文件仍满足不变量）：
  ```markdown
  - 以该 record 的 `DataTail` 截断 data 文件尾部垃圾（必要时）。截断后文件仍以 Magic 分隔符结尾。
  ```

---

## P0-4: Value 类型收敛

### 修改点 4.1：4.1.4 类型约束表

- **位置**：[mvp-design-v2.md#L128-L133](../../DurableHeap/docs/mvp-design-v2.md#L128-L133)
- **当前内容**：
  ```markdown
  | 类别 | 支持 | 不支持 |
  |------|------|--------|
  | **值类型** | 基元类型：`int`, `long`, `ulong`, `float`, `double`, `bool`, `null` | 任意 struct、用户自定义值类型 |
  | **引用类型** | `DurableObject` 派生类型（内置集合：`DurableDict`；未来：`DurableArray`） | 任意 class、`List<T>`、`Dictionary<K,V>` 等 |
  ```
- **建议修改**：
  ```markdown
  | 类别 | 支持（MVP） | 未来扩展 | 不支持 |
  |------|-------------|----------|--------|
  | **值类型** | `null`, 整数（varint）, `ObjRef(ObjectId)`, `Ptr64` | `float`, `double`, `bool` | 任意 struct、用户自定义值类型 |
  | **引用类型** | `DurableObject` 派生类型（内置集合：`DurableDict`） | `DurableArray` | 任意 class、`List<T>`、`Dictionary<K,V>` 等 |
  ```

### 修改点 4.2：4.4.2 ValueType 枚举

- **位置**：[mvp-design-v2.md#L393-L398](../../DurableHeap/docs/mvp-design-v2.md#L393-L398)
- **当前内容**：
  ```markdown
  ValueType（低 4 bit，MVP 固定）：

  - `Val_Null = 0x0`
  - `Val_Tombstone = 0x1`
  - `Val_ObjRef = 0x2`
  - `Val_VarInt = 0x3`
  - `Val_Ptr64 = 0x4`
  ```
- **建议新增说明**：
  ```markdown
  ValueType（低 4 bit，MVP 固定）：

  - `Val_Null = 0x0`
  - `Val_Tombstone = 0x1`
  - `Val_ObjRef = 0x2`
  - `Val_VarInt = 0x3`（有符号整数，ZigZag 编码）
  - `Val_Ptr64 = 0x4`

  **MVP 类型收敛说明**：
  - MVP 仅支持上述 5 种 ValueType
  - `float`/`double`/`bool` 移至"未来扩展"，不在 MVP 实现
  - 若需扩展，可增加 `Val_Bool = 0x5`（1 byte）、`Val_F32 = 0x6`（IEEE754 u32 LE）、`Val_F64 = 0x7`（IEEE754 u64 LE）
  ```

---

## P0-5: Dirty Set 卡住问题

**此问题由 P0-1 的 `_dirtyKeys` 方案解决。**

当采用 `_dirtyKeys` 机制后：
- 每次 `Set`/`Delete` 操作时，会立即更新 `_dirtyKeys`（添加或移除 key）
- 如果一系列操作（如 `Set(k,v); Delete(k)`）的净效果为零，`_dirtyKeys` 会自动清空该 key
- `HasChanges = _dirtyKeys.Count > 0`，因此净零变更后 `HasChanges = false`
- 对象不会卡在 Dirty Set 中

无需额外修改点。

---

## P0-6: Commit API 命名

### 修改点 6.1：4.4.5 标题

- **位置**：[mvp-design-v2.md#L433](../../DurableHeap/docs/mvp-design-v2.md#L433)
- **当前内容**：
  ```markdown
  #### 4.4.5 Commit(rootId)
  ```
- **建议修改**：
  ```markdown
  #### 4.4.5 CommitAll(newRootId)
  ```

### 修改点 6.2：4.4.5 方法签名描述

- **位置**：[mvp-design-v2.md#L435](../../DurableHeap/docs/mvp-design-v2.md#L435)
- **当前内容**：
  ```markdown
  输入：rootId（一般为常驻 workspace 的 root 对象 id）。
  ```
- **建议修改**：
  ```markdown
  **签名**：`CommitAll(newRootId: ObjectId)`

  **参数语义**：
  - `newRootId`：本次 commit 后的 root 对象 id
  - 参数名 `newRootId` 明确表达这是一个"更新 root 指针"的副作用，**不是** commit 范围限定符

  **⚠️ 重要**：`CommitAll` 是**全局检查点**，会持久化 Dirty Set 中所有未提交的修改，而不仅仅是 `newRootId` 可达的对象。
  ```

### 修改点 6.3：文档中其他 `Commit(rootId)` 引用

需要全文搜索并替换以下位置：

- [mvp-design-v2.md#L6](../../DurableHeap/docs/mvp-design-v2.md#L6)：术语表 Commit 行（如有）
- 决策表和正文中对 `Commit(...)` 的描述

---

## P0-7: 首次 commit 语义

### 修改点 7.1：4.3.1 Open 空仓库行为

- **位置**：[mvp-design-v2.md#L269-L272](../../DurableHeap/docs/mvp-design-v2.md#L269-L272)
- **当前内容**：
  ```markdown
  #### 4.3.1 Open

  1) 扫描 meta 文件尾部，找到最后一个 CRC32C 有效且"data tail 与指针可验证"的 `MetaCommitRecord`。
  2) 得到 HEAD `EpochSeq`、`RootObjectId`、`VersionIndexPtr`、`DataTail`、`NextObjectId`。
  3) 初始化 ObjectId allocator：`next = NextObjectId`。
  ```
- **建议修改**：
  ```markdown
  #### 4.3.1 Open

  1) 扫描 meta 文件尾部，找到最后一个 CRC32C 有效且"data tail 与指针可验证"的 `MetaCommitRecord`。
  2) 若找到有效 record：得到 HEAD `EpochSeq`、`RootObjectId`、`VersionIndexPtr`、`DataTail`、`NextObjectId`。
  3) **若未找到有效 record（空仓库）**：
     - `EpochSeq = 0`（隐式空状态）
     - `RootObjectId = 0`（无 root）
     - `VersionIndexPtr = 0`（空映射）
     - `DataTail = 4`（仅包含初始 Magic 分隔符）
     - `NextObjectId = 1`（`ObjectId = 0` 保留给 well-known 对象，如 VersionIndex）
  4) 初始化 ObjectId allocator：`next = NextObjectId`。
  ```

### 修改点 7.2：新建对象首次版本

- **位置**：建议在 4.2.5 节末尾新增段落
- **建议新增**：
  ```markdown
  ##### 新建对象的首次版本（First Version of New Object）

  - 新对象的**首个版本**其 `PrevVersionPtr = 0`
  - `DiffPayload` 语义上为"from-empty diff"（即所有 key-value 都是 Upserts）
  - 与 Checkpoint Version 的 wire format 相同（都是 `PrevVersionPtr=0` + Upserts），但概念上是"创世版本"而非"压缩快照"
  - 读取时，`PrevVersionPtr = 0` 表示版本链终止，无需继续回溯

  ##### 首次 Commit 语义（First Commit on Empty Heap）

  - `Open()` 空仓库时，返回 `Epoch = 0`（隐式空状态）
  - 首次 `CommitAll()` 创建 `Epoch = 1` 的 MetaCommitRecord
  - `VersionIndexPtr` 指向首个 VersionIndex 版本（`PrevVersionPtr = 0` 的全量 state）
  - `DataTail` 更新为 data 文件新末尾（包含尾部 Magic）
  ```

---

## 附录：术语表遗漏补充（P1）

建议在术语表中补充以下条目：

| 术语 | 定义 | 别名/弃用 | 实现映射 |
|------|------|----------|---------|
| **Checkpoint Version** | 版本链中 `PrevVersionPtr=0` 的全量状态版本，用于封顶回放成本 | — | `PrevVersionPtr=0` + 全量 DiffPayload |
| **Materialize** | 从版本链中间表示合成 Committed State 的过程 | — | 4.1.0 定义 |
| **Deserialize** | 从文件字节解码为版本中间表示的过程 | — | 4.1.0 定义 |
| **HasChanges** | 只读属性，表示当前状态与 Committed State 是否存在语义差异 | — | `_dirtyKeys.Count > 0` |

---

## Implementer 实现建议

1. **优先实现 `_dirtyKeys` 机制**：这是保证 Dirty Set 不卡住的关键
2. **统一 Magic-as-Separator 视角**：所有 framing 代码按"Magic 是分隔符"理解
3. **类型收敛**：MVP 只实现 `null/varint/ObjRef/Ptr64`，float/double/bool 放 TODO
4. **API 命名**：对外 API 使用 `CommitAll(newRootId:)` 避免 Scoped Commit 误解
5. **空仓库边界**：实现 Open 时处理"无有效 MetaCommitRecord"的情况

## QA 测试建议

1. **`_dirtyKeys` 不变式测试**：
   - `Set(k,v); Delete(k)` 后 `HasChanges == false`
   - `Set(k, newVal); Set(k, committedVal)` 后 `HasChanges == false`
2. **Magic Separator framing 测试**：
   - 空文件 `[Magic]` → reverse scan 返回"无 record"
   - payload 包含 Magic 字节 → CRC 校验不通过则 resync
3. **首次 Commit 测试**：
   - 空仓库 Open 后 `CurrentEpoch == 0`
   - 首次 CommitAll 后 `CurrentEpoch == 1`

---

*Brief 由 Investigator 产出，2025-12-20*
