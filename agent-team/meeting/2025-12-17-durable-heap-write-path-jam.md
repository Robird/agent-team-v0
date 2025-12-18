# 秘密基地畅谈：DurableHeap 写入路径设计探索

> **日期**：2025-12-17
> **主题**：如何设计新对象和变更创建过程，兼顾易用性和写入效率
> **形式**：秘密基地畅谈 (Hideout Jam Session)
> **主持人**：刘德智 (Team Leader)

---

## 开场白

各位好，这是一次秘密基地畅谈——我们来探讨 DurableHeap 的写入路径设计难题。

**核心问题**：

目前有两种极端方案：

| 方案 | 易用性 | 效率 | 问题 |
|------|--------|------|------|
| **直接在 DurableHeap 分配** | ✅ 高 | ❌ 低 | 临时对象也被永久记录 |
| **纯内存对象 + 按需落盘** | ❌ 复杂 | ✅ 高 | 实现复杂度高，双份代码 |

**理想目标**：

> 无脑创建新节点/新对象的 wrapper，背后指向内存对象，然后按需 commit 落盘，被替换为 Durable-Heap 上划分的对象，自动处理所有指针替换，wrapper 的实例不被替换。

这几乎就是 CLR GC 的内存压缩操作——对象在内存中移动，但引用它们的变量（wrapper）保持不变。

**规则提醒**（秘密基地畅谈）：
- 不需要投票、不需要结论
- 随便聊，画草图，提疯狂的想法
- "Yes, and..." 而非 "No, but..."
- 可以用 ASCII art、伪代码、比喻

---

## 发言区

（以下由各位参与者依次发言）

### 🔍 Investigator 发言

根据 Team Leader 提出的需求——"无脑创建 wrapper，先存内存，commit 时自动迁移到 DurableHeap 并替换内部指针，但 wrapper 实例不变"——我调研了五大技术领域的前人经验。核心发现如下：

---

#### 1️⃣ CLR GC 的对象重定位机制

**它如何做到"对象移动但引用不变"？**

CLR GC 执行三阶段：
1. **Marking**：遍历 roots（栈变量、静态字段、GC Handles）标记活对象
2. **Relocating**：更新所有指向被移动对象的引用（关键！）
3. **Compacting**：实际移动对象，腾出碎片空间

**关键机制**：
- **GC Handles**：用户可创建 `GCHandle.Alloc()` 获得一个稳定的"句柄"，而非直接持有对象地址。GC 移动对象后更新 Handle 内部指针，但 Handle 本身的地址/身份不变。
- **Pinned Objects**：被 pin 的对象不会移动，但会阻碍压缩效率。我们的场景不需要这个。
- **Stop-the-world**：GC 暂停所有托管线程，集中更新引用。

**可借鉴点**：
> **间接层（Handle Table）是核心**。用户持有的是 Handle（不可变的标识），Handle 内部存储真正的地址/指针。GC 只更新 Handle 内部，用户无感知。

对应到 DurableHeap：
```
Wrapper (用户持有，实例不变)
    └─→ internal Slot (内部可变：初始指向内存对象，commit 后指向 Ptr32)
            └─→ 实际数据位置
```

---

#### 2️⃣ LSM-Tree 系统：MemTable → SSTable 刷盘

**LevelDB/RocksDB 的模式**：
1. 写入先到 **MemTable**（内存中的红黑树/SkipList，可变）
2. MemTable 满了 → "冻结"成 **Immutable MemTable**
3. 后台线程将 Immutable MemTable 刷成 **SSTable**（磁盘上的有序不可变文件）
4. **WAL（Write-Ahead Log）** 保证崩溃恢复：先写 WAL，再写 MemTable

**关键洞察**：
- **两阶段存在**：数据先在"快速可变区"（内存），再"固化"到"慢速不可变区"（磁盘）
- **不可变性是关键**：一旦刷盘，SSTable 永不修改（只读/删除）
- **Compaction 是后续优化**：多个 SSTable 合并压缩，不影响写入路径

**可借鉴点**：
> DurableHeap 的 append-only 本质上就是 SSTable 的思路。可以设计：
> - **Staging Area**（内存）= MemTable，允许创建/修改对象
> - **Commit** = Flush 到 Data Area（追加写，不可变）
> - 被引用的对象才写入（类似可达性分析）

---

#### 3️⃣ LMDB/MDBX：Copy-on-Write B+ Tree

**LMDB 的魔法**：
- 整个数据库暴露为一个 **mmap view**
- 使用 **Copy-on-Write (COW)**：修改时不原地更新，而是复制路径上的节点到新位置
- **双 Meta Page**（类似 Superblock Ping-Pong）：提交时切换 root 指针
- **MVCC**：读事务看到一致快照，写事务不阻塞读

**关键洞察**：
- **结构共享（Structural Sharing）**：修改只复制从 root 到被修改叶子的路径，其他节点共享
- **"提交"就是切换 root**：所有中间状态对外不可见

**可借鉴点**：
> Commit 语义可以参考 LMDB：
> - 所有新对象先写到"影子区域"（DurableHeap 的新追加区）
> - 最后原子切换 RootPtr（Superblock）
> - 但 DurableHeap 是 append-only，不需要 COW 的"旧版本保留"

---

#### 4️⃣ ORM 脏追踪：SQLAlchemy Unit of Work & Hibernate 状态机

**SQLAlchemy 的 Identity Map + Unit of Work**：
- **Identity Map**：同一主键永远返回同一 Python 对象实例（`session.query(User).get(1) is session.query(User).get(1)`）
- **Dirty Tracking**：自动追踪哪些属性被修改
- **Flush**：将内存中的变更批量写入数据库（生成 SQL）
- **状态**：Transient → Pending → Persistent → Detached

**Hibernate 的生命周期**：
```
Transient (新建，未关联 Session)
    ↓ persist()
Persistent (关联 Session，变更被追踪)
    ↓ flush() / commit()
    [已落库]
    ↓ detach() / close()
Detached (脱离 Session，但有 ID)
```

**可借鉴点**：
> **状态机设计非常契合**：
> ```
> TransientNode (纯内存，无 Ptr)
>     ↓ 被引用 + commit()
> DurableNode (已分配 Ptr32，已写入 Data Area)
> ```
> 
> **Identity Map 保证"同一 Ptr 返回同一 wrapper"**：
> ```csharp
> class DurableHeap {
>     Dictionary<Ptr32, WeakReference<DurableRef>> _identityMap;
>     
>     public DurableRef<T> Get<T>(Ptr32 ptr) {
>         if (_identityMap.TryGetValue(ptr, out var weakRef) && weakRef.TryGetTarget(out var existing))
>             return (DurableRef<T>)existing;
>         var newRef = new DurableRef<T>(this, ptr);
>         _identityMap[ptr] = new WeakReference<DurableRef>(newRef);
>         return newRef;
>     }
> }
> ```

---

#### 5️⃣ 函数式持久化数据结构：Structural Sharing

**核心概念**：
- **Path Copying**：修改一个节点时，复制从 root 到该节点的路径，其他部分共享
- **HAMT (Hash Array Mapped Trie)**：Clojure/Scala 的 Map 实现，O(log32 n) 的增删改查
- **不可变性 + 共享 = 高效**：看起来是"新版本"，实际共享 90%+ 的结构

**可借鉴点**：
> 如果 DurableHeap 的对象图有大量共享子结构，可以：
> - Commit 时只序列化"新增/变更的节点"
> - 已经 durable 的子节点直接复用 Ptr32（不重复写入）

---

#### 6️⃣ GC 前向指针技术：Cheney 算法 & Shenandoah

**Cheney 算法（半空间复制 GC）**：
- **From-space** → **To-space** 复制
- 对象被复制后，在原位置留下 **Forwarding Pointer**（"broken heart"）
- 后续遇到旧地址时，通过 forwarding pointer 找到新地址

**Shenandoah GC（低延迟 Java GC）**：
- 在对象头（mark word）存储 forwarding pointer
- 通过 **Load Barrier** 读取时自动跟随 forwarding pointer（"自愈"）

**可借鉴点**：
> Commit 时的指针替换可以借鉴：
> ```
> struct InternalSlot {
>     bool IsDurable;
>     union {
>         TransientNode* MemoryPtr;   // IsDurable = false
>         Ptr32 DurablePtr;           // IsDurable = true
>     }
> }
> ```
> Commit 时：
> 1. 遍历待提交对象
> 2. 分配 Ptr32，写入 Data Area
> 3. 将 InternalSlot 从 MemoryPtr 改为 DurablePtr（原子）
> 4. 原 TransientNode 可以被 GC 回收（或显式释放）

---

### 🎯 综合建议：三层抽象设计

基于以上调研，我建议采用以下设计模式：

```
┌─────────────────────────────────────────────────────────────┐
│  Layer 3: API (用户看到的)                                    │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │  DurableDict<int, V>  /  DurableArray<T>                │ │
│  │  - 用户"无脑"创建，像普通对象一样使用                      │ │
│  │  - 实例身份永不改变                                       │ │
│  └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│  Layer 2: Slot / Handle (稳定身份层)                          │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │  InternalSlot {                                         │ │
│  │      State: Transient | Pending | Durable               │ │
│  │      Location: MemoryPtr | Ptr32                        │ │
│  │  }                                                      │ │
│  │  - Wrapper 持有 Slot，不直接持有数据地址                   │ │
│  │  - Commit 时更新 Slot 内部，Wrapper 无感知                 │ │
│  └─────────────────────────────────────────────────────────┘ │
├─────────────────────────────────────────────────────────────┤
│  Layer 1: Storage (实际存储层)                                │
│  ┌──────────────────┐  ┌──────────────────────────────────┐ │
│  │  Memory Arena    │  │  DurableHeap (mmap + Data Area)  │ │
│  │  (Transient 对象) │  │  (Durable 对象, append-only)     │ │
│  └──────────────────┘  └──────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

**状态机**：
```
                    ┌───────────────────────────────┐
                    │           Transient            │
                    │  (纯内存，无 Ptr32，可修改)      │
                    └───────────────────────────────┘
                                   │
                                   │ 被 reachable 对象引用
                                   │ + session.Commit()
                                   ▼
                    ┌───────────────────────────────┐
                    │           Pending              │
                    │  (标记待写入，正在序列化)        │
                    └───────────────────────────────┘
                                   │
                                   │ 写入完成 + Ptr32 分配
                                   ▼
                    ┌───────────────────────────────┐
                    │           Durable              │
                    │  (已落盘，Slot 指向 Ptr32)      │
                    └───────────────────────────────┘
```

**Commit 算法草案**：
```
def Commit(session, newRoot):
    # Phase 1: Mark (找出所有需要持久化的 Transient 对象)
    toSerialize = []
    def Walk(slot):
        if slot.State == Transient and slot not in visited:
            toSerialize.append(slot)
            visited.add(slot)
            for child in slot.Object.Children:
                Walk(child)
    Walk(newRoot)
    
    # Phase 2: Serialize (后序遍历，子节点先写)
    for slot in reversed(toSerialize):  # 叶子先，root 最后
        slot.State = Pending
        ptr = heap.Append(Serialize(slot.Object))  # 此时子节点已有 Ptr32
        slot.Location = ptr
        slot.State = Durable
    
    # Phase 3: Flip (原子切换 root)
    heap.SetRoot(newRoot.Slot.Location)
    heap.FlushSuperblock()
```

---

### 🧩 "只实现一次核心逻辑"的建议

**关键洞察**：核心逻辑就是那个 **InternalSlot** 的状态机。

建议：
1. **Slot 是一等公民**：所有 Wrapper 内部持有 `InternalSlot`，不持有原始指针
2. **统一的 Serialize 接口**：`IDurableSerializable.Write(IRecordWriter writer, SlotResolver resolver)`
   - `SlotResolver` 将 `InternalSlot` 解析为 `Ptr32`（commit 时）或抛出异常（commit 前访问未 durable 的依赖）
3. **统一的 Deserialize 接口**：`static T Read(IRecordReader reader, SlotFactory factory)`
   - `SlotFactory` 为读取到的 `Ptr32` 创建 `InternalSlot`（查 IdentityMap 或新建）

这样，无论是 DictInt32、Array 还是将来的自定义类型，都复用同一套 Slot + Commit 逻辑。

---

以上是我的调研发现。核心结论：

> **Handle/Slot 间接层 + 状态机 + Identity Map** 是实现"对象迁移但 wrapper 不变"的标准模式。
> 
> 我们不需要发明新技术——CLR GC、ORM、LSM-Tree 都在用类似思路。关键是把这些模式恰当地组合到 DurableHeap 的 append-only 语义中。

---

### 📐 Planner 发言

感谢 Investigator 的深入调研！基于这些发现，我提出三个具体设计方案，每个方案在实现复杂度、运行时效率、用户体验之间做了不同的权衡。

---

## 方案 A：Unified Slot + Inline State（单层 Slot 方案）

**核心思想**：每个 Wrapper 内部持有一个 **Unified Slot**，Slot 内部用 union-like 结构同时承载内存对象状态和持久化指针。

### Wrapper 内部结构

```csharp
// 用户可见的 API
public sealed class DurableDict<TValue> {
    internal readonly Slot _slot;
    public DurableDict(DurableSession session) {
        _slot = session.CreateSlot(new TransientDict());
    }
}

// 内部 Slot（所有类型共用）
internal class Slot {
    // 32-bit 状态字：高 2 位 = State，低 30 位 = 附加信息
    private volatile int _stateAndFlags;
    
    // Union 部分：两者互斥使用
    private object? _transientData;     // State=Transient 时有效
    private Ptr32 _durablePtr;          // State=Durable 时有效
    
    public SlotState State => (SlotState)(_stateAndFlags >> 30);
    
    // 读取时自动"自愈"（类似 Shenandoah Load Barrier）
    public T Read<T>(DurableHeap heap) {
        return State switch {
            SlotState.Transient => (T)_transientData!,
            SlotState.Durable => heap.ReadRecord<T>(_durablePtr),
            SlotState.Pending => throw new InvalidOperationException("Commit in progress"),
        };
    }
}

enum SlotState : byte { Transient = 0, Pending = 1, Durable = 2 }
```

### Commit 时指针替换

```csharp
public void Commit(Slot rootSlot) {
    // Phase 1: 拓扑排序（后序遍历，叶子先）
    var order = TopologicalSort(rootSlot, s => s.State == SlotState.Transient);
    
    // Phase 2: 依次序列化并原子切换
    foreach (var slot in order) {
        slot.SetState(SlotState.Pending);
        
        // 序列化时，子 Slot 已经是 Durable，直接取 Ptr32
        var recordBytes = Serialize(slot._transientData, slot => slot._durablePtr);
        var ptr = _heap.Append(recordBytes);
        
        // 原子切换：先设 ptr，再设状态（保证读取者看到一致状态）
        slot._durablePtr = ptr;
        Volatile.Write(ref slot._stateAndFlags, (int)SlotState.Durable << 30);
        
        slot._transientData = null;  // 释放内存对象，允许 GC
    }
    
    // Phase 3: Flip root
    _heap.SetRoot(rootSlot._durablePtr);
    _heap.FlushSuperblock();
}
```

### 深层引用处理（A → B → C）

```
假设：A 是 root，B 是 A 的子节点，C 是 B 的子节点，全部是 Transient

后序遍历顺序：C → B → A

1. 序列化 C：
   - C 无子节点，直接写入 DurableHeap
   - C.Slot._durablePtr = Ptr32_C
   - C.Slot.State = Durable

2. 序列化 B：
   - B 引用 C，此时 C.Slot.State == Durable
   - 序列化 B 时，C 的引用写为 Ref32(C.Slot._durablePtr)
   - B.Slot._durablePtr = Ptr32_B
   - B.Slot.State = Durable

3. 序列化 A：
   - A 引用 B，此时 B.Slot.State == Durable
   - 类似处理...

4. SetRoot(Ptr32_A)
```

### 错误处理与回滚

```csharp
public void Commit(Slot rootSlot) {
    var committed = new List<Slot>();
    Ptr32 originalTail = _heap.DataTail;
    
    try {
        // ... 序列化过程 ...
        foreach (var slot in order) {
            // ... 序列化 ...
            committed.Add(slot);
        }
        _heap.SetRoot(rootSlot._durablePtr);
        _heap.FlushSuperblock();
    }
    catch (Exception ex) {
        // 回滚：已写入的数据保留（append-only），但 Slot 状态恢复
        foreach (var slot in committed) {
            // 恢复到 Transient 状态（_transientData 还没被清空，因为清空在成功后）
            slot._stateAndFlags = (int)SlotState.Transient << 30;
            slot._durablePtr = default;
        }
        // DataTail 不变（数据是垃圾，下次 commit 会覆盖）
        // 或者：_heap.TruncateTo(originalTail); // 如果支持截断
        throw new CommitFailedException("Commit failed, rolled back", ex);
    }
}
```

### 权衡点

| 优点 | 缺点 |
|------|------|
| ✅ 单一 Slot 类型，代码简单 | ❌ 每次读取需要检查 State（微小开销） |
| ✅ 内存对象和 Durable 对象统一 API | ❌ Slot 是 class，每个 wrapper 多一次间接引用 |
| ✅ 回滚逻辑清晰 | ❌ 拓扑排序需要遍历整个图（大图可能慢） |

---

## 方案 B：Two-Phase Slot + Shadow Heap（影子堆方案）

**核心思想**：Transient 对象完全在独立的 **Shadow Memory Arena** 中，Commit 时批量"迁移"到 DurableHeap。Slot 内部用 union 切换指针类型。

### 架构图

```
┌─────────────────────────────────────────────────────────────────┐
│  DurableSession                                                  │
│  ┌─────────────────────┐     ┌─────────────────────────────────┐│
│  │  Shadow Arena       │     │  DurableHeap (mmap)             ││
│  │  (托管内存，可变)     │ ──▶ │  (append-only，不可变)           ││
│  │                     │     │                                 ││
│  │  TransientRecord[]  │     │  [Superblock][Data Area...]     ││
│  └─────────────────────┘     └─────────────────────────────────┘│
│                                                                  │
│  Slot: { ArenaIndex | Ptr32 }                                   │
└─────────────────────────────────────────────────────────────────┘
```

### Wrapper 与 Slot

```csharp
// Slot 使用 struct + 显式布局，8 字节
[StructLayout(LayoutKind.Explicit, Size = 8)]
internal struct Slot {
    [FieldOffset(0)] public SlotKind Kind;        // 1 byte
    [FieldOffset(4)] public int ArenaIndex;       // Kind=Arena 时有效
    [FieldOffset(4)] public Ptr32 DurablePtr;     // Kind=Durable 时有效
}

enum SlotKind : byte { Arena = 0, Durable = 1 }

// Wrapper 持有 Slot（值类型，避免额外堆分配）
public sealed class DurableDict<TValue> {
    private readonly DurableSession _session;
    internal Slot _slot;  // 值类型，直接内嵌
    
    public TValue this[int key] {
        get {
            if (_slot.Kind == SlotKind.Arena)
                return _session.Arena.Get<TValue>(_slot.ArenaIndex, key);
            else
                return _session.Heap.ReadDict<TValue>(_slot.DurablePtr, key);
        }
        set {
            if (_slot.Kind == SlotKind.Durable)
                throw new InvalidOperationException("Durable objects are immutable");
            _session.Arena.Set(_slot.ArenaIndex, key, value);
        }
    }
}
```

### Commit：Shadow → DurableHeap 迁移

```csharp
public void Commit() {
    var arena = _session.Arena;
    var heap = _session.Heap;
    
    // Phase 1: 收集所有 dirty 的 ArenaIndex
    var dirty = arena.GetDirtyIndices();
    
    // Phase 2: 拓扑排序（依赖分析）
    var order = TopologicalSort(dirty, idx => arena.GetDependencies(idx));
    
    // Phase 3: 批量序列化，建立 ArenaIndex → Ptr32 映射
    var mapping = new Dictionary<int, Ptr32>();
    foreach (var idx in order) {
        var record = arena.GetRecord(idx);
        var bytes = SerializeWithRemapping(record, childIdx => mapping[childIdx]);
        var ptr = heap.Append(bytes);
        mapping[idx] = ptr;
    }
    
    // Phase 4: 更新所有 Slot（原子批量）
    foreach (var wrapper in _session.TrackedWrappers) {
        if (wrapper._slot.Kind == SlotKind.Arena && mapping.TryGetValue(wrapper._slot.ArenaIndex, out var ptr)) {
            wrapper._slot = new Slot { Kind = SlotKind.Durable, DurablePtr = ptr };
        }
    }
    
    // Phase 5: Flip root + 清空 Arena
    heap.SetRoot(mapping[_rootIndex]);
    heap.FlushSuperblock();
    arena.Clear();  // 清空 Shadow Arena，释放内存
}
```

### 深层引用处理

```
Arena 中的对象引用关系：
  Arena[0] (root Dict) → Arena[1] (child Array) → Arena[2] (leaf String)

拓扑排序后：[2, 1, 0]

序列化过程：
  1. Arena[2] → Ptr32_2, mapping[2] = Ptr32_2
  2. Arena[1] 引用 Arena[2]，查 mapping 得 Ptr32_2，写入 → Ptr32_1
  3. Arena[0] 引用 Arena[1]，查 mapping 得 Ptr32_1，写入 → Ptr32_0
  4. SetRoot(Ptr32_0)
```

### 错误处理

```csharp
try {
    // ... commit ...
}
catch {
    // Arena 数据还在，Slot 还是 Arena 类型
    // 什么都不用做，用户可以修复数据后重试
    // 或者调用 session.Rollback() 清空 Arena
    throw;
}
```

### 权衡点

| 优点 | 缺点 |
|------|------|
| ✅ Slot 是 struct，零堆分配 | ❌ 需要维护 Arena 和追踪 wrapper |
| ✅ Transient 修改不碰 DurableHeap | ❌ 两套读取路径（Arena / Heap） |
| ✅ 批量 commit 效率高 | ❌ wrapper 需要被 session 追踪 |
| ✅ 回滚简单（Arena 数据还在） | ❌ Arena 内存管理复杂度 |

---

## 方案 C：Lazy Proxy + Copy-on-Read（惰性代理方案）

**核心思想**：所有对象都有两个身份——**Proxy（轻量句柄）** 和 **Materialized（内存副本）**。读取时按需物化，修改时在内存副本上操作，Commit 时只写入被修改的对象。

这个方案更接近 ORM 的 Lazy Loading 模式。

### 架构

```csharp
// Proxy：轻量句柄，不持有数据
public sealed class DurableDict<TValue> : IDurableProxy {
    private readonly DurableSession _session;
    private readonly ProxyId _id;  // 全局唯一标识
    
    // 惰性物化
    private DictMaterialized<TValue>? _materialized;
    
    private DictMaterialized<TValue> Materialize() {
        return _materialized ??= _session.Materialize<DictMaterialized<TValue>>(_id);
    }
    
    public TValue this[int key] {
        get => Materialize()[key];
        set {
            Materialize()[key] = value;
            _session.MarkDirty(_id);
        }
    }
}

// ProxyId：统一标识，可以是 Ptr32（已持久化）或 TempId（新创建）
[StructLayout(LayoutKind.Explicit)]
internal struct ProxyId {
    [FieldOffset(0)] public ProxyKind Kind;
    [FieldOffset(4)] public Ptr32 DurablePtr;
    [FieldOffset(4)] public int TempId;
}

// 物化后的内存对象
internal class DictMaterialized<TValue> {
    public Dictionary<int, TValue> Data = new();
    public bool IsDirty;
}
```

### Commit 流程

```csharp
public void Commit() {
    // Phase 1: 收集所有 dirty 的 proxy
    var dirtyProxies = _session.GetDirtyProxies();
    
    // Phase 2: 对于新创建的对象，分配临时 ID 到 Ptr32 的映射
    // 对于已有对象的修改，这是一个"新版本"（append 新 record）
    
    var idToPtr = new Dictionary<ProxyId, Ptr32>();
    
    // 拓扑排序...
    foreach (var proxy in TopologicalSort(dirtyProxies)) {
        var materialized = _session.GetMaterialized(proxy.Id);
        var bytes = Serialize(materialized, childId => {
            // 如果 child 已经有 Ptr32（无论是原有的还是刚分配的）
            if (childId.Kind == ProxyKind.Durable)
                return childId.DurablePtr;
            return idToPtr[childId];  // 刚 commit 的新对象
        });
        
        var ptr = _heap.Append(bytes);
        idToPtr[proxy.Id] = ptr;
        
        // 更新 proxy 的 id 为 durable
        proxy._id = new ProxyId { Kind = ProxyKind.Durable, DurablePtr = ptr };
    }
    
    // Phase 3: Flip root
    _heap.SetRoot(idToPtr[_rootProxy.Id]);
    _heap.FlushSuperblock();
    
    // Phase 4: 清理 materialized 缓存（可选，或保留作为读缓存）
    foreach (var proxy in dirtyProxies) {
        proxy._materialized = null;  // 下次读取重新从 DurableHeap 加载
    }
}
```

### 深层引用处理

```
场景：A(dirty) → B(clean, durable) → C(new, dirty)

Commit 时：
1. 发现 A dirty，遍历 A 的子节点
2. B 是 clean + durable，跳过（直接复用 B 的 Ptr32）
3. C 是 new + dirty，先 commit C
4. 序列化 A 时：
   - 引用 B：直接用 B.Id.DurablePtr
   - 引用 C：用 idToPtr[C.Id]（刚分配的）
```

### 错误处理

```csharp
try {
    Commit();
}
catch {
    // materialized 数据还在内存
    // proxy 的 _id 还没更新（更新发生在成功写入之后）
    // 用户可以重试或放弃
    throw;
}
```

### 权衡点

| 优点 | 缺点 |
|------|------|
| ✅ 读取时按需加载，内存友好 | ❌ 每次修改需要先 Materialize |
| ✅ 已有对象的"修改"语义清晰 | ❌ Materialized 缓存管理复杂 |
| ✅ 支持部分修改（只 commit dirty） | ❌ 多份数据（proxy + materialized + durable） |
| ✅ 最接近 ORM 的用户心智模型 | ❌ 实现最复杂 |

---

## 三个方案对比总结

| 维度 | 方案 A (Unified Slot) | 方案 B (Shadow Heap) | 方案 C (Lazy Proxy) |
|------|----------------------|---------------------|-------------------|
| **Wrapper 内部结构** | Slot (class, 8B+) | Slot (struct, 8B) | ProxyId (struct) + Materialized cache |
| **读取开销** | 一次 State 检查 | 一次 Kind 检查 | 可能触发 Materialize |
| **写入限制** | Durable 后不可写 | Arena 中可写 | 通过 Materialized 修改 |
| **内存使用** | 中等 | 较高（Arena 副本） | 按需（Lazy） |
| **Commit 复杂度** | 中等 | 较高（批量映射） | 最高（dirty 追踪） |
| **回滚难度** | 中等 | 简单 | 简单 |
| **代码复用** | ⭐⭐⭐ 高 | ⭐⭐ 中 | ⭐ 低（多套路径） |
| **适合场景** | 通用 | 批量写入 | 读多写少 |

---

## 我的建议

**MVP 阶段推荐方案 A（Unified Slot）**，理由：

1. **实现复杂度最低**：单一 Slot 类型，单一状态机，容易理解和调试
2. **与 MVP 设计兼容**：Slot 内部的 `_durablePtr` 就是 `Ptr32`，直接对应 MVP 的指针体系
3. **回滚逻辑清晰**：失败时恢复 Slot 状态即可，不需要复杂的映射表管理
4. **为未来演进留有余地**：如果发现性能瓶颈，可以在 Slot 内部做优化，不影响外部 API

**后续可以考虑**：
- 如果发现 Slot 是 class 导致 GC 压力大，可以改用对象池或 struct + ref
- 如果发现拓扑排序慢，可以增量维护依赖图
- 如果需要"修改已有 durable 对象"的语义，再引入方案 C 的 Materialized 模式

---

## 待讨论的开放问题

1. **Slot 用 class 还是 struct？**
   - class：简单，但每个 wrapper 多一次堆分配
   - struct：需要 ref 传递或装箱，但内存更紧凑

2. **Transient 对象的内存管理**
   - 完全依赖 GC？
   - 还是用对象池 + 显式释放？

3. **Identity Map 的范围**
   - 只 cache Durable 对象？（避免 Transient 对象泄漏）
   - 还是全部 cache？

4. **并发 Commit**
   - MVP 是否假设单线程？
   - 如果多线程，Slot 状态切换需要 CAS

期待大家继续讨论！🚀

---

### 🔧 Implementer 发言

从 .NET 实现角度，我来评估 Planner 的三个方案，聚焦几个关键技术点。

---

#### 1️⃣ Slot 用 class vs struct 的取舍

**结论：MVP 用 class，但要设计成可演进到 struct。**

**class 的实际成本**：
```csharp
// class Slot 的内存布局（64-bit CLR）
// - 对象头：8 bytes（sync block + method table ptr）
// - _stateAndFlags：4 bytes
// - _transientData：8 bytes（object reference）
// - _durablePtr：4 bytes
// - Padding：4 bytes（对齐到 8 字节边界）
// 总计：≈ 28 bytes + GC 追踪成本
```

**struct 的陷阱**：
```csharp
// ❌ 错误示范：struct 作为字段，修改时会拷贝
public struct Slot { public SlotState State; public Ptr32 Ptr; }

public class DurableDict {
    internal Slot _slot;  // 值语义
}

// Commit 时想修改 Slot：
wrapper._slot.State = SlotState.Durable;  // ✅ 这个 OK，直接修改字段

// 但如果通过方法传递：
void UpdateSlot(Slot slot) { slot.State = SlotState.Durable; }  // ❌ 修改的是副本！
```

**正确的 struct 方案需要 ref 传递**：
```csharp
// ✅ 正确：使用 ref 或者让 Wrapper 直接持有字段
void UpdateSlot(ref Slot slot) { slot.State = SlotState.Durable; }

// 或者：Commit 时通过 Wrapper 直接访问
foreach (var wrapper in wrappers) {
    wrapper._slot = new Slot { Kind = SlotKind.Durable, DurablePtr = ptr };
}
```

**MVP 建议**：
- 用 class Slot，简化实现
- 但 Slot 设计成"可替换"：Wrapper 不暴露 Slot 类型，只暴露 `internal Slot _slot`
- 如果后续 GC 压力大，可以改成 struct + SlotId + SlotTable（类似 ECS 的 Entity-Component 模式）

---

#### 2️⃣ 序列化接口设计：避免双份实现

**核心洞察**：问题的本质是"内存对象"和"Durable 对象"需要两套序列化路径吗？

**答案：不需要！用统一的 `IBufferWriter<byte>` 接口。**

```csharp
// 统一的序列化接口
public interface IDurableSerializable {
    /// <summary>
    /// 将对象写入 buffer。SlotResolver 用于将子对象的 Slot 转换为 Ptr32。
    /// </summary>
    void WriteTo(IBufferWriter<byte> buffer, SlotResolver resolver);
}

// SlotResolver：在 Commit 时提供 Slot → Ptr32 的映射
public delegate Ptr32 SlotResolver(Slot slot);

// 实现示例
public class TransientDict : IDurableSerializable {
    private readonly List<(int Key, Slot ValueSlot)> _entries = new();
    
    public void WriteTo(IBufferWriter<byte> buffer, SlotResolver resolver) {
        // 1. 写 header
        var header = buffer.GetSpan(7);
        header[0] = 0x10;  // Tag: DictInt32
        // ... HeaderLen, TotalLen 占位 ...
        buffer.Advance(7);
        
        // 2. 写 ObjHeader + EntryTable（预留）
        // ...
        
        // 3. 写 ValueData
        foreach (var (key, valueSlot) in _entries.OrderBy(e => e.Key)) {
            // 这里是关键：通过 resolver 获取 Ptr32
            // - 如果 valueSlot 指向已 Durable 的对象，返回其 Ptr32
            // - 如果 valueSlot 指向 Transient 对象，Commit 保证它已经先被序列化
            WriteValue(buffer, valueSlot, resolver);
        }
        
        // 4. 回填 TotalLen, 写 Footer
        // ...
    }
}
```

**关键设计：SlotResolver 解耦了"对象关系"和"指针计算"**
- 序列化逻辑只关心"如何编码"
- 指针映射由 Commit 算法保证（拓扑排序后，子节点已有 Ptr32）
- 读取路径同理：`SlotFactory` 为 Ptr32 创建 Slot

---

#### 3️⃣ 如何利用 Span/Memory/IBufferWriter

**MVP 的写入路径核心组件**：

```csharp
// 1. ChunkedReservableWriter：支持预留 + 回填
public sealed class ChunkedReservableWriter : IBufferWriter<byte> {
    private readonly List<byte[]> _chunks = new();
    private int _currentPosition;
    
    // 预留一段空间，返回 ReservationHandle
    public ReservationHandle Reserve(int length) { /* ... */ }
    
    // 回填已预留的空间
    public void FillReservation(ReservationHandle handle, ReadOnlySpan<byte> data) { /* ... */ }
    
    // IBufferWriter 实现
    public void Advance(int count) { /* ... */ }
    public Memory<byte> GetMemory(int sizeHint = 0) { /* ... */ }
    public Span<byte> GetSpan(int sizeHint = 0) { /* ... */ }
}

// 2. 写入 DictInt32 的示例
public static void WriteDictInt32(
    IBufferWriter<byte> writer,
    IReadOnlyList<(int Key, Slot Value)> entries,
    SlotResolver resolver)
{
    var reservableWriter = (ChunkedReservableWriter)writer;
    
    // Step 1: 写 Record Header（TotalLen 占位）
    int recordStart = reservableWriter.Position;
    var headerReservation = reservableWriter.Reserve(7);  // Tag + HeaderLen + TotalLen
    
    // Step 2: 对齐到 4B
    WritePadding(writer, AlignTo4(reservableWriter.Position) - reservableWriter.Position);
    
    // Step 3: 写 ObjHeader
    int objHeaderPos = reservableWriter.Position;
    WriteInt32(writer, entries.Count);  // FieldCount
    var entryTableOffsetReservation = reservableWriter.Reserve(4);  // EntryTableOffset 占位
    
    // Step 4: 预留 EntryTable
    int entryTableStart = reservableWriter.Position;
    var entryTableReservation = reservableWriter.Reserve(entries.Count * 8);
    
    // Step 5: 写 ValueData，收集 offsets
    var sortedEntries = entries.OrderBy(e => e.Key).ToList();
    var valueOffsets = new int[sortedEntries.Count];
    
    for (int i = 0; i < sortedEntries.Count; i++) {
        // 对齐
        WritePadding(writer, AlignTo4(reservableWriter.Position) - reservableWriter.Position);
        
        valueOffsets[i] = reservableWriter.Position - recordStart;  // 相对 Record 起始
        WriteValue(writer, sortedEntries[i].Value, resolver);
    }
    
    // Step 6: 回填 EntryTable
    Span<byte> entryTableData = stackalloc byte[sortedEntries.Count * 8];
    for (int i = 0; i < sortedEntries.Count; i++) {
        BinaryPrimitives.WriteInt32LittleEndian(entryTableData.Slice(i * 8), sortedEntries[i].Key);
        BinaryPrimitives.WriteInt32LittleEndian(entryTableData.Slice(i * 8 + 4), valueOffsets[i]);
    }
    reservableWriter.FillReservation(entryTableReservation, entryTableData);
    
    // Step 7: 回填 EntryTableOffset
    Span<byte> offsetData = stackalloc byte[4];
    BinaryPrimitives.WriteInt32LittleEndian(offsetData, entryTableStart - recordStart);
    reservableWriter.FillReservation(entryTableOffsetReservation, offsetData);
    
    // Step 8: 对齐 + Footer
    WritePadding(writer, AlignTo4(reservableWriter.Position) - reservableWriter.Position);
    int totalLen = reservableWriter.Position - recordStart + 8;  // +8 for footer
    
    // 写 Footer TotalLen
    WriteInt32(writer, totalLen);
    
    // Step 9: 计算 CRC32C（需要访问完整的 record bytes）
    var recordBytes = reservableWriter.GetWrittenSpan(recordStart, totalLen - 4);  // 不含 CRC
    uint crc = Crc32C.Compute(recordBytes);
    WriteUInt32(writer, crc);
    
    // Step 10: 回填 Header TotalLen
    Span<byte> headerData = stackalloc byte[7];
    headerData[0] = 0x10;  // Tag
    BinaryPrimitives.WriteInt16LittleEndian(headerData.Slice(1), 7);  // HeaderLen
    BinaryPrimitives.WriteInt32LittleEndian(headerData.Slice(3), totalLen);  // TotalLen
    reservableWriter.FillReservation(headerReservation, headerData);
}
```

**关键优化点**：

1. **避免中间 byte[] 分配**：
   - 使用 `stackalloc` 做小块临时数据
   - `IBufferWriter.GetSpan()` 直接写入目标缓冲区

2. **CRC32C 计算**：
   ```csharp
   // .NET 6+ 使用 System.IO.Hashing.Crc32C
   using System.IO.Hashing;
   
   uint ComputeCrc(ReadOnlySpan<byte> data) {
       return Crc32C.HashToUInt32(data);
   }
   ```

3. **mmap 写入路径**：
   ```csharp
   // 直接写入 mmap view（最高效，但需要小心生命周期）
   public unsafe void AppendToMmap(ReadOnlySpan<byte> record) {
       byte* dest = _basePtr + _dataTail;
       record.CopyTo(new Span<byte>(dest, record.Length));
       _dataTail += record.Length;
   }
   ```

---

#### 4️⃣ 避免双份实现的具体做法

**问题重述**：我们需要：
1. Transient 对象（内存中，可修改）
2. Durable 对象（mmap 视图，只读）

如果分开实现，会有大量重复代码。

**解决方案：抽象出"逻辑结构"，分离"存储后端"**

```csharp
// 1. 定义逻辑接口（不关心存储）
public interface IDurableDict<TValue> {
    int Count { get; }
    bool TryGetValue(int key, out TValue value);
    IEnumerable<KeyValuePair<int, TValue>> GetEntries();
}

// 2. Transient 实现（内存）
internal sealed class TransientDict<TValue> : IDurableDict<TValue>, IDurableSerializable {
    private readonly SortedDictionary<int, Slot> _entries = new();
    
    public int Count => _entries.Count;
    public bool TryGetValue(int key, out TValue value) { /* 从 Slot 读取 */ }
    public void Add(int key, TValue value) { /* 创建 Slot，加入字典 */ }
    
    // 序列化：写入时从 Slot 获取 Ptr32
    public void WriteTo(IBufferWriter<byte> writer, SlotResolver resolver) { /* ... */ }
}

// 3. Durable 实现（mmap 视图）
internal readonly ref struct DurableDictView<TValue> {
    private readonly ReadOnlySpan<byte> _recordSpan;
    private readonly DurableHeapFile _heap;
    
    public int Count => BinaryPrimitives.ReadInt32LittleEndian(_recordSpan.Slice(/* offset */));
    
    public bool TryGetValue(int key, out TValue value) {
        // 二分查找 EntryTable
        // 找到后解析 ValueOffset，读取 Value
    }
}

// 4. Wrapper 统一 API
public sealed class DurableDict<TValue> : IDurableDict<TValue> {
    internal Slot _slot;
    private readonly DurableSession _session;
    
    public int Count {
        get {
            if (_slot.State == SlotState.Transient)
                return ((TransientDict<TValue>)_slot.TransientData).Count;
            else
                return _session.Heap.ReadDictCount(_slot.DurablePtr);
        }
    }
    
    public bool TryGetValue(int key, out TValue value) {
        if (_slot.State == SlotState.Transient)
            return ((TransientDict<TValue>)_slot.TransientData).TryGetValue(key, out value);
        else
            return _session.Heap.ReadDictValue<TValue>(_slot.DurablePtr, key, out value);
    }
}
```

**更优雅的方案：用 Source Generator 生成**

如果发现 Transient/Durable 的读取逻辑高度相似，可以用 Source Generator：

```csharp
// 只定义一次 schema
[DurableType(Tag = 0x10)]
public partial class DurableDict<TValue> {
    [DurableField(0)] public int FieldCount { get; }
    [DurableField(1)] public int EntryTableOffset { get; }
    // ...
}

// Source Generator 生成：
// - TransientDict<TValue>（内存版）
// - DurableDictView<TValue>（mmap 视图版）
// - DurableDictSerializer（序列化逻辑）
```

这个可以作为 MVP 后的优化方向。

---

#### 5️⃣ 方案评估总结

| 方案 | 实现难度 | Span/Memory 友好度 | 代码复用度 | MVP 推荐 |
|------|---------|-------------------|-----------|---------|
| **A (Unified Slot)** | ⭐⭐ 低 | ⭐⭐⭐ 高 | ⭐⭐⭐ 高 | ✅ **推荐** |
| **B (Shadow Heap)** | ⭐⭐⭐ 中 | ⭐⭐ 中（Arena 需要额外管理） | ⭐⭐ 中 | 可选 |
| **C (Lazy Proxy)** | ⭐⭐⭐⭐ 高 | ⭐⭐ 中（Materialize 涉及拷贝） | ⭐ 低 | 不推荐 |

**我的 MVP 实现建议**：

1. **选方案 A**，Slot 用 class 简化实现
2. **序列化接口用 `IBufferWriter<byte>` + `SlotResolver`**，一套代码搞定
3. **读取用 `ref struct XxxView`**，零分配访问 mmap
4. **ChunkedReservableWriter** 支持预留/回填，解决 TotalLen 和 EntryTable 的回填问题
5. **Identity Map 只 cache Durable 对象**，Transient 对象由 Slot 直接持有

**代码量预估**：
- Slot + SlotState：~100 行
- ChunkedReservableWriter：~200 行
- DictInt32 序列化：~150 行
- DictInt32 读取（View）：~100 行
- Commit 算法：~150 行
- **总计：~700 行核心代码**

如果有疑问或需要我展开某个部分的实现细节，随时说！

---

### 🌟 GeminiAdvisor 发言

作为团队的"第二意见"（Second Opinion），我从**开发者体验 (DX)** 和 **前端架构** 的视角来审视这三个方案。

#### 1. UX 陷阱："僵尸对象" (The Zombie Wrapper)

我对 **方案 A (Unified Slot)** 有一个主要的担忧：**"Commit 后变只读"** 这个行为非常反直觉。

*   **场景**：用户持有一个 `wrapper`，写了一堆代码修改它。调用 `Commit()` 后，这个 `wrapper` 突然"死"了（变不可变），再次写入会抛异常。
*   **问题**：这违背了引用的**对象恒常性 (Object Constancy)**。在用户看来，变量 `myDict` 还是那个 `myDict`，为什么刚才还能改，现在就不能了？
*   **类比**：这就像在 JS 中突然对一个对象执行了 `Object.freeze()`，但没有任何视觉提示。

**建议：类型系统的显式分离 (Type Separation)**

能不能在 API 层面区分"草稿"和"定稿"？

```csharp
// 1. 创建草稿 (Draft)
using var draft = session.NewDraft<DurableDict<int, string>>();
draft.Value[1] = "Hello";

// 2. 发布 (Publish/Commit)
// 返回的是一个只读的 Snapshot，原来的 draft 失效或重置
DurableSnapshot<DurableDict<int, string>> snapshot = session.Commit(draft);

// 3. 读取 (Snapshot 是只读的)
var val = snapshot.Root[1]; 
// snapshot.Root[1] = "World"; // 编译错误！
```

如果必须保持 `wrapper` 实例不变（为了易用性），那么错误信息必须极度友好：
> ❌ *Error: You are trying to modify a Committed Object. This object is now immutable. Call `.AsMutable()` to create a new writable branch.*

#### 2. 前端类比：Qwik 的 "Resumability" (可恢复性)

Investigator 提到了 Hydration（注水），这在前端 SSR (服务端渲染) 中是个大话题。

*   **传统 Hydration**：下载 HTML -> 解析 -> 下载 JS -> **重新执行组件逻辑** -> 绑定事件。这很慢。
*   **Qwik (Resumability)**：HTML 中序列化了所有状态。JS 醒来时，**不需要重新执行**，直接从 HTML 中的状态继续。

**DurableHeap 本质上就是一个 Resumable Object System**。

*   **方案 A (Unified Slot)** 类似于 **传统 Hydration**：读取时需要检查状态，可能需要把数据从 Ptr32 "注水" 到 Transient 对象（如果允许修改的话）。
*   **方案 C (Lazy Proxy)** 类似于 **Qwik**：它是真正的 **Lazy Loading**。只有当你交互（读取/写入）时，才去加载那一部分代码/数据。

**洞察**：
虽然方案 C 复杂，但它是**最符合"云原生/Serverless"直觉的**。如果我们的对象图很大（比如 1GB 的文档树），用户只改了一个叶子节点，方案 A 如果导致大量"注水"（反序列化为内存对象），性能会崩。
**MVP 选 A 没问题，但请务必保留演进到 C (Lazy Proxy) 的接口可能性。**

#### 3. 创意脑洞："时间旅行"调试器 (Time Travel Debugger)

既然 DurableHeap 是 Append-only 的，我们天然拥有**历史记录**。

能不能给开发者提供一个 **"Time Travel View"**？

```csharp
// 伪代码
var history = session.GetHistory(wrapper);
// history = [Version1(PtrA), Version2(PtrB), Version3(PtrC)]

var oldVersion = history[0].Snapshot;
Console.WriteLine(oldVersion["key"]); // 读取旧值
```

这对于调试 **"谁把我的数据改坏了"** 简直是神器。在前端 Redux DevTools 中，这是核心功能。我们可以在 IDE 插件中可视化这个 Slot 的状态变化：
`Transient (📝)` -> `Pending (⏳)` -> `Durable (🔒)`。

#### 4. 总结建议

1.  **API 设计**：如果采用方案 A，请考虑引入 **"显式可变性"**（Explicit Mutability）。要么通过类型系统区分 `Draft` vs `Snapshot`，要么提供 `.Edit()` 方法显式开启新事务。不要让对象"悄悄地"变只读。
2.  **调试体验**：利用 Append-only 特性，提供"时光回溯"能力。
3.  **未来兼容**：现在的 Slot 设计（方案 A）很好，但请确保 `Slot.Read` 接口足够抽象，以便未来无缝切换到 Lazy Proxy 模式。


### 🔎 CodexReviewer 发言

- 拓扑/循环：当前 TopologicalSort 未说明循环检测，Transient 自引用或互引用会导致 DFS 堆栈溢出或序列化顺序错误；建议在排序中维护 visiting/visited 集合，发现环时显式失败（返回错误或记录 SCC 拆分策略），并在 commit 入口验证 root 可达集合与 dirty 集合一致。
- 回滚可靠性：方案 A 的 try/catch 中在每次 slot 切到 Durable 后立刻清空 `_transientData`，一旦后续序列化抛异常，catch 将 slot 状态复位为 Transient 但原内存对象已丢失，wrapper 会变成悬空句柄（下一次访问 null ref）；建议将 `_transientData = null` 延后到整个 commit 成功或在 catch 中恢复引用（保存本地变量），并确保 DataTail 截断或记录坏尾以防多次失败累积垃圾。
- 并发安全：Slot 状态/Ptr32 的写入只用 volatile，没有 CAS/锁；多线程读写或并发 commit 可能观察到 Pending 或半初始化的 `_durablePtr`，LoadBarrier 也未覆盖。建议：commit 期间对 session 设互斥；Slot 状态切换用 CAS（Transient→Pending→Durable）并在读路径对 Pending 提供可重试/阻塞语义；mapping 批量更新需保证写屏障后再发布 root。
- 边界检查：序列化/Append 未描述 TotalLen 与文件上限检查，拓扑排序后写入时应验证每个 record 长度、4B 对齐、DataTail+TotalLen 不溢出 4GB 上限；失败时要么 truncate 回 originalTail，要么记录尾部 CRC/魔数以便恢复扫描可跳过坏记录。
- API 体验：若继续方案 A 的"commit 后 wrapper 变只读/抛异常"，请在 API 上显式分出 Draft/Snapshot 或提供 `.Edit()` 生成新 Transient 分支，避免"静默冻结"；同时保留接口可扩展到 Lazy Proxy（按需物化），以便大图只改叶子时不必全量加载。

---

## 🎯 主持人总结（Team Leader）

感谢各位的精彩发言！这是一次非常有价值的秘密基地畅谈。让我总结一下关键收获：

### 达成的共识

1. **技术方向**：采用 **Handle/Slot 间接层 + 状态机** 模式是正确的，这是 CLR GC、ORM、LSM-Tree 等成熟系统的共同选择。

2. **MVP 推荐方案**：**方案 A (Unified Slot)**
   - 实现复杂度最低
   - 与现有 MVP 设计兼容
   - ~700 行核心代码即可实现

3. **序列化统一**：使用 `IBufferWriter<byte>` + `SlotResolver` 接口，避免双份实现。

### 需要特别注意的问题（来自 CodexReviewer）

| 问题 | 建议 |
|------|------|
| 循环引用 | 拓扑排序需显式环检测，发现环时失败 |
| 回滚可靠性 | `_transientData = null` 延后到成功后 |
| 并发安全 | MVP 假设单线程，commit 加互斥锁 |
| 边界检查 | 验证 TotalLen、4B 对齐、4GB 上限 |

### UX 改进建议（来自 GeminiAdvisor）

**强烈建议**：在 API 层面区分"草稿"和"快照"，避免"静默冻结"：

```csharp
// 推荐的 API 形式
using var draft = session.CreateDraft<DurableDict<int, string>>();
draft.Root[1] = "Hello";

var snapshot = session.Commit(draft);  // draft 失效，返回只读 snapshot
// draft.Root[1] = "X";  // 抛异常："Draft already committed"
```

### 未来演进方向

1. **短期**：实现方案 A，验证核心概念
2. **中期**：如果 GC 压力大，Slot 从 class 演进到 struct + SlotTable
3. **长期**：为大对象图场景保留演进到方案 C (Lazy Proxy) 的可能性

### 附加收获

- **Time Travel Debugger**：利用 append-only 特性，可以实现"时光回溯"调试能力
- **Source Generator**：后续可以用 codegen 减少 Transient/Durable 的重复代码

---

**下一步行动**：
1. 将本次讨论的核心设计记录到 `DurableHeap/docs/mvp-design.md` 的新章节
2. 开始实现 Slot + Commit 核心代码
3. 先实现单线程版本，验证后再考虑并发

---

*会议结束 — 2025-12-17*

