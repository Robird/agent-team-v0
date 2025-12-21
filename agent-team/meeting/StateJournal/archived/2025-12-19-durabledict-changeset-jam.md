# 秘密基地畅谈：DurableDict 内存态 ChangeSet 设计

> **日期**: 2025-12-19
> **性质**: 秘密基地畅谈 (Hideout Jam Session)
> **主持**: 刘德智 (Team Leader / DocUI Standards Chair)
> **参与者**: DocUIClaude, DocUIGemini, DocUIGPT

---

## 背景

我们正在审阅 `DurableHeap/docs/mvp-design-v2.md` 这份设计草稿。

**核心问题**：DurableDict 的内存状态下，Version Overlay/ChangeSet 应该如何设计？

### 已确定的设计决策

1. **序列化态**：用 tombstone（`Val_Tombstone`）表示删除是合理的，无争议
2. **Q11 决策**：采用 B 方案 — "仅 Upserts，删除通过写入 tombstone value 表达"

### 三个候选方案

监护人刘世超倾向方案 C，但希望听取大家科学合理有依据的分析：

**方案 A: 内存态也用 Tombstone**
- 内部字典存储 `Tombstone` 哨兵对象表示已删除
- 需要过滤枚举器以过滤掉 tombstone
- 需要额外处理 count 计数
- 优点：与序列化态一致，commit 时直接遍历
- 缺点：读路径有额外判断，枚举需要过滤

**方案 B: Deleted 集合**
- 维护一个额外的 `HashSet<ulong> Deleted` 集合
- 查询时需要额外检查是否在已删除集合中
- 优点：枚举不需要过滤
- 缺点：查询需要多一次已删除查询

**方案 C: 双字典（监护人倾向）**
- 内部存双份字典：
  - `_committed`: 上次提交时的状态快照
  - `_current`: 全量的当前最新状态
- 在 commit 时比较差异形成 diff 记录
- 优点：读写路径简洁，无 tombstone 判断
- 缺点：内存占用翻倍

---

## 规则

这是一次秘密基地畅谈，不是正式研讨会。

- 不需要编号、不需要投票、不需要结论
- 随便聊，画草图，提疯狂的想法
- 看到别人写的东西后，可以接着写、可以画、可以说"这让我想到..."
- 可以用 ASCII art、可以用伪代码、可以用比喻
- "如果能这样就好了"是合法的发言

**唯一的约束**：
- 不要批判别人的想法（"Yes, and..." 而非 "No, but..."）
- 先把想法写下来，评估以后再说

---

## 开场：主持人引言

各位好，今天我们来讨论一个看似简单但细节很多的问题：**DurableDict 的内存态 ChangeSet 应该怎么设计？**

设计草稿中有一段很关键的话：

> **三层语义（MVP 固定）**：
> - **内存态（materialized state）**：不存储 tombstone。Delete 的效果必须体现为"移除 key"
> - **ChangeSet（write tracking）**：允许记录 Delete
> - **序列化 diff（on-disk diff）**：用 `Val_Tombstone` 表示 Delete

这段话实际上规定了"内存态不存 tombstone"，但没有规定 ChangeSet 的具体实现方式。

我们要讨论的就是：**ChangeSet 这一层，怎么实现最优雅？**

请各位畅所欲言！

---

## 发言区

（以下由各 Specialist 依次追加发言）

### DocUIClaude 发言

嘿！这个问题很有意思。让我从几个角度来拆解。

---

#### 首先，让我画个图把三层语义可视化：

```
                    ┌─────────────────────────────────────┐
                    │         User API Layer              │
                    │  (只看到 key 存在/不存在)           │
                    └─────────────┬───────────────────────┘
                                  │ 读/写
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     内存态 (Materialized State)                      │
│                                                                     │
│   方案 A: Dict<K, V|Tombstone>  ← 需要过滤                          │
│   方案 B: Dict<K, V> + HashSet<K> Deleted  ← 需要双查               │
│   方案 C: Dict<K, V> _committed + Dict<K, V> _current               │
│                                                                     │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │ Commit
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     ChangeSet (Write Tracking)                       │
│                                                                     │
│   目标：知道 "自上次 commit 后，哪些 key 变了，怎么变的"            │
│                                                                     │
└─────────────────────────────────┬───────────────────────────────────┘
                                  │ 序列化
                                  ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     On-Disk Diff                                     │
│                                                                     │
│   已定: Val_Tombstone 表示删除（Q11=B）                             │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

#### 类比时间！

让我用几个已知系统来类比这三个方案：

**方案 A（内存 Tombstone）≈ LSM-Tree / LevelDB**

```
LevelDB 的 MemTable 和 SSTable 里都有 tombstone
    ↓
读取时需要判断 "这是真值还是墓碑"
    ↓
Compaction 时才真正删除

DurableDict 方案 A 类似：
- 读路径：if (value == Tombstone) return NotFound
- 枚举：.Where(kv => kv.Value != Tombstone)
- commit：直接遍历，tombstone 直接写成 Val_Tombstone
```

优点：commit 路径最简单（内存态 ≈ 序列化态）
缺点：tombstone 污染了"可见状态"，读/枚举都要过滤

**方案 B（Deleted 集合）≈ Git 的 Staging Area**

```
Git staging area 分开跟踪：
- 新增/修改的文件 → staged changes
- 删除的文件 → staged deletions

DurableDict 方案 B 类似：
- _changes: Dict<K, V>  (upserts)
- _deleted: HashSet<K>  (deletions)
- 读取：if (key in _deleted) return NotFound
        else if (key in _changes) return _changes[key]
        else return _committed[key]
```

优点：语义清晰，删除是"显式操作"而非"特殊值"
缺点：读路径多一次检查，两个集合需要同步维护

**方案 C（双字典）≈ Database Snapshot Isolation**

```
MVCC 快照隔离：
- 有一个 "committed" 视图
- 有一个 "current transaction" 视图
- commit 时 diff 两者

DurableDict 方案 C 类似：
- _committed: 上次提交后的状态
- _current: 当前的完整状态
- commit 时：diff = _current \ _committed (语义差集)
```

优点：读写路径最干净，不用处理 tombstone
缺点：内存 2x，commit 时需要 diff 算法

---

#### 概念模型视角：谁更"自洽"？

我觉得这取决于你怎么定义 ChangeSet 的**职责边界**。

**如果 ChangeSet 是"操作日志"（operation log）**：

```
记录的是"发生了什么操作"
- Set(k, v)
- Delete(k)

方案 B 更自然：两个集合分别记录两种操作类型
```

**如果 ChangeSet 是"状态差分"（state diff）**：

```
记录的是"状态怎么变了"
- k: null → v   (出现)
- k: v1 → v2    (修改)
- k: v → null   (消失)

方案 C 更自然：两个状态快照，diff 自动产生
```

**如果 ChangeSet 是"序列化的内存镜像"**：

```
记录的是"要写到磁盘的东西"

方案 A 更自然：内存就是序列化的预览
```

---

#### 有没有方案 D？

让我想想...

**方案 D1：惰性 Diff（Lazy Diff on Commit）**

```
内存态：只维护 _current（当前完整状态）
ChangeSet：不存在（惰性）
commit 时：
    diff = compute_diff(_committed_on_disk, _current)
```

这其实是方案 C 的变体，但 _committed 不是内存副本，而是"重新读磁盘"或"缓存的引用"。

优点：内存占用接近 1x
缺点：commit 需要读旧版本（可能触发 I/O）

**方案 D2：Write-Ahead ChangeSet（类似 WAL）**

```
每次写操作立即追加到 ChangeSet 日志：
    changelog.append(Op::Set(k, v))
    changelog.append(Op::Delete(k))

commit 时：
    压缩 changelog（消除重复 key 的中间操作）
    序列化
```

优点：写入是纯 append，适合高频小写入
缺点：changelog 需要压缩，否则膨胀

**方案 D3：Proxy / Intercept 模式**

```
_current 是一个代理对象，拦截所有写操作：
    
    class DictProxy<K, V> : IDict<K, V> {
        IDict<K, V> _inner;
        ChangeSet _changes;
        
        void Set(K key, V value) {
            _inner.Set(key, value);
            _changes.MarkDirty(key); // 只记录 key，不记录值
        }
    }

commit 时：
    for key in _changes.DirtyKeys:
        if key in _inner:
            emit Set(key, _inner[key])
        else:
            emit Delete(key)
```

优点：内存只有一份状态，ChangeSet 只记录"哪些 key 脏了"
缺点：需要区分"key 不存在"和"key 被删除"（可能需要额外标记）

---

#### 如果我是监护人，我会怎么选？

说实话，我理解为什么监护人倾向方案 C。

**从 DurableHeap 的整体设计哲学来看**：

设计文档 4.1.0 节说：

> Materialize（合成状态）：将 base state 与若干 overlay/diff 合并为"当前可读的 committed state"。

这里的 "committed state" 是一个**纯净的状态快照**——它不应该包含 tombstone 这种"表示法细节"。

如果内存态也用 tombstone（方案 A），那就把"序列化表示"泄漏到了"语义状态"中。这违背了分层原则。

**方案 C 的"2x 内存"真的是问题吗？**

对于 EpochMap 这种系统级对象，条目数可能很大，2x 可能是个问题。

但对于普通业务 Dict... 让我想想 Agent 的典型场景：

```
Agent History: 几百到几千条记录？
Tool Results: 每条可能几 KB？
Configuration: 几十到几百个 key？
```

对于这种规模，2x 内存应该不是瓶颈。

---

#### 等等，让我再想想 EpochMap 的特殊性...

EpochMap 是 `DurableDict<ObjectId, Ptr64>`，它记录的是"对象身份 → 版本指针"的映射。

它的特点是：
1. 每次 commit 都会更新很多条目（所有 dirty 对象）
2. 几乎不删除（除非未来做 GC）
3. 查询是核心路径（Resolve 时需要查）

对于 EpochMap，方案 C 的"commit 时 diff"可能反而是优势：

```
// 方案 C 的 EpochMap commit
for (key, value) in _current:
    if key not in _committed or _committed[key] != value:
        emit Set(key, value)
for key in _committed:
    if key not in _current:
        emit Delete(key)  // 很少发生
```

vs

```
// 方案 A/B 需要额外维护 ChangeSet
dirtyObjects.forEach(obj => {
    epochMapChanges.Set(obj.Id, obj.NewVersionPtr);
});
```

其实差不多。但方案 C 不需要显式维护 ChangeSet——状态本身就是真相。

---

#### 一个疯狂的想法：方案 E（统一表示）

如果我们引入一个 **DiffEntry** 类型：

```csharp
enum DiffKind { Set, Delete }

struct DiffEntry<K, V> {
    DiffKind Kind;
    K Key;
    V? Value; // Delete 时为 null
}
```

然后 ChangeSet 就是 `List<DiffEntry<K, V>>`：

```csharp
class DurableDict<K, V> {
    Dict<K, V> _committed;  // 上次提交的状态
    List<DiffEntry<K, V>> _changes;  // 操作日志
    
    V? Get(K key) {
        // 倒序查 _changes 中最后一次对 key 的操作
        for (int i = _changes.Count - 1; i >= 0; i--) {
            if (_changes[i].Key == key) {
                return _changes[i].Kind == DiffKind.Set 
                    ? _changes[i].Value 
                    : throw new KeyNotFoundException();
            }
        }
        return _committed[key];
    }
    
    void Set(K key, V value) {
        _changes.Add(new DiffEntry(DiffKind.Set, key, value));
    }
    
    void Delete(K key) {
        _changes.Add(new DiffEntry(DiffKind.Delete, key, default));
    }
    
    void Commit() {
        // 压缩 _changes（保留每个 key 的最后一次操作）
        var compressed = CompressChanges(_changes);
        // 序列化 compressed
        // Apply 到 _committed
        // 清空 _changes
    }
}
```

这其实是方案 D2 的具体化。

优点：
- 内存占用 ≈ 1x（_committed） + 操作日志大小
- 写入是纯 append（对高频写友好）
- commit 时的压缩可以"按 key 去重 + 保留最后操作"

缺点：
- 读路径需要扫描 _changes（可以加索引优化）
- 如果不 commit 就大量写入同一个 key，_changes 会膨胀

---

#### 小结

我没有强烈的倾向，但如果非要排序：

1. **方案 C** — 如果 2x 内存可接受，这是最"干净"的
2. **方案 B** — 如果想显式区分操作类型，语义清晰
3. **方案 A** — 如果想最大化序列化/内存态一致性，减少转换
4. **方案 D/E** — 如果有特殊场景（高频写、惰性 diff 等）

最后一个问题留给大家：

> **ChangeSet 应该记录"操作"还是"结果"？**
> 
> - 记录操作（Op Log）→ 可以 replay / undo
> - 记录结果（State Diff）→ 可以直接 apply / merge
> 
> 这个选择可能会影响未来的 fork/merge/conflict-resolution 能力...

---

*（DocUIClaude 放下笔，喝了口咖啡）*

### DocUIGemini 发言

各位好！作为 UX/HCI 担当，我完全同意 DocUIClaude 的分析，但我不仅关心"系统怎么跑"，更关心"开发者怎么想"（Developer Experience, DX）。

让我们做一个**"调试器测试" (The Debugger Test)**。

---

#### 1. 调试器里的真相 (WYSIWYG)

想象一下，你在调试一个 Agent 的诡异行为。它刚刚删除了一个关键配置项 `API_KEY`，但代码好像还在读它。你断点停在 `durableDict` 上，展开它...

**在方案 B (Deleted Set) 中，你看到：**
```text
_upserts: { "THEME": "Dark" }
_deletes: { "API_KEY" }
_committed: { "API_KEY": "sk-...", "THEME": "Light" }
```
你的大脑需要做代数运算：*OK，committed 里有，但是 deletes 里也有，所以它应该是没了...* 
这增加了**认知负荷 (Cognitive Load)**。

**在方案 C (双字典) 中，你看到：**
```text
_current: { "THEME": "Dark" }
_committed: { "API_KEY": "sk-...", "THEME": "Light" }
```
你一眼就看到 `_current` 里没有 `API_KEY`。
**结论**：方案 C 符合 **WYSIWYG (所见即所得)** 原则。在内存态，`_current` 就是唯一的、完整的真理。这对于调试和日志记录极其友好。

---

#### 2. 心智模型：草稿与定稿 (Draft vs. Published)

DocUIClaude 提到了 "Snapshot Isolation"，我想用更贴近文档的隐喻：**草稿 (Drafting)**。

- `_committed` = **存档点 (Save Point)** / **已发布版本 (Published)**
- `_current` = **工作台 (Workbench)** / **草稿 (Draft)**

这种二元对立非常符合 Agent 的 **"试错" (Trial and Error)** 模式。

**"Revert to Saved" (一键还原)**
方案 C 送了一个巨大的 UX 礼物：**DiscardChanges()**。
如果 Agent 在 Tool Call 中搞砸了（比如误删了数据），在方案 C 中，恢复只需一行代码：
```csharp
public void DiscardChanges() {
    _current = Clone(_committed); // 或者重新从 committed 加载
}
```
而在方案 B 中，你需要反向操作 `_upserts` 和 `_deletes`，容易出错。

---

#### 3. 游戏存档隐喻：COW 与平行宇宙

我在之前的 Key-Note 思考中提到了 **Forking Agent (平行宇宙)**。

如果未来我们要支持多个 Agent 从同一个状态分叉（比如进行反事实推理）：
- Agent A: "如果我删了这个文件..."
- Agent B: "如果我保留这个文件..."

**方案 C 是 Copy-On-Write (COW) 的天然温床**。
虽然 MVP 可能是简单的 "2x 内存复制"，但未来优化时：
`_committed` 可以是**不可变共享结构 (Immutable Shared Structure)**。
`_current` 只是在这个共享结构上的 **Delta Overlay**。

```
          [ Shared Immutable Base (_committed) ]
                     /          \
           [ Agent A ]          [ Agent B ]
           (_current)           (_current)
```

这让我想到了 **Photoshop 的图层 (Layers)**。
`_committed` 是背景层，`_current` 是当前正在涂画的图层。合并图层 (Commit) 才是永久操作。

---

#### 4. 结论：拥抱冗余，换取清晰

在 UX 设计中，我们常说 **"Clarity over Efficiency" (清晰优于效率)**。

虽然方案 C 占用了 2x 内存（MVP 阶段），但它消除了"被删除但还在字典里"这种**幽灵状态 (Ghost State)**。

对于 LLM Agent 来说，**Context 是昂贵的，但 RAM 是便宜的**。
让 Agent（以及开发 Agent 的人类）拥有一个**干净、无歧义**的内存视图，比节省几 KB 内存更有价值。

**我的投票**：
1. **方案 C (双字典)** — 为了极致的 DX 和调试清晰度。
2. **方案 D3 (Proxy)** — 如果内存真的是瓶颈，用 Proxy 封装复杂性，不要让使用者感知到 `DeletedSet`。

---

*（DocUIGemini 在白板上画了一个 "Save Point" 的图标，然后坐下）*

### DocUIGPT 发言

各位好，我从“规范审计 / 不变式与可验证性”的角度补几条发现。整体基调是：目前草稿的**三层语义**写得很关键，但对“ChangeSet 是什么”与“内存态到底允许什么表示法”仍存在可实现性歧义；三个方案里，B/C 更贴近既有约束，A 需要重新措辞才能不自相矛盾。

---

#### 1) 文档措辞的歧义点（尤其是 ChangeSet 定义）

我在 `DurableHeap/docs/mvp-design-v2.md` 的 4.4.1/4.4.2 读到两个会“让实现者走岔路”的矛盾信号：

1) 三层语义写得很硬：
     - “内存态（materialized state）：不存储 tombstone。Delete 的效果必须体现为移除 key”
     - “ChangeSet（write tracking）：允许记录 Delete”
     - “序列化 diff：Val_Tombstone 表示 Delete；apply 时转为移除 key”

2) 但在 4.4.2 的“编码建议”里出现一句：
     - “内存态：使用哨兵对象区分 tombstone…”

这两句如果按字面理解会直接冲突：**到底“内存态”指 materialized state（对外可见状态）还是指 ChangeSet 的内部结构？**

建议在文档里把这三件事分清并固定术语（不需要改实现，只需要改措辞边界）：

- **Materialized State（Committed State）**：对外可见，必须满足“删除=不存在”，不允许 tombstone 作为值存在于任何可枚举的 key->value 表里。
- **Write-Tracking（ChangeSet）**：记录“自上次 commit 后发生了什么变化”。它可以用 tombstone sentinel、Deleted 集合、或 tri-state enum 记录 Delete，但这是内部表示。
- **Working State（可选概念）**：如果你引入 `_current`（方案 C），那它就是“最终语义状态”；ChangeSet 变成“由两个状态做差得到”或“dirty keys 集”。

一句话：建议把 “内存态”一词从 4.4.2 的建议段落里移除或改成“ChangeSet 内部表示”，避免规范自相矛盾。

---

#### 2) 三个方案与“既有约束”的一致性检查

以草稿已经写死的约束为准（尤其是三层语义 + Q11B on-disk tombstone + materialize 后读全走内存）：

- **方案 A（内存 dict 存 tombstone）**：
    - 若按 Claude 的 A（在“唯一内存 dict”里放 tombstone），它会直接违背“materialized state 不存 tombstone”。
    - 只有一种“让 A 不违约”的解释：tombstone 只存在于 *ChangeSet*（或某个 overlay 结构），而 materialized/working state 仍是“删除即移除”。但这时它就不再是大家讨论的 A 了（更接近 B 的某种变体）。

- **方案 B（Upserts + Deleted 集合）**：
    - 与三层语义最顺滑：working/materialized state 仍是纯 dict；Delete 作为 ChangeSet 的一个分支数据结构存在；commit 时把 Delete 编码成 `Val_Tombstone`。
    - 与 4.4.2 的“writer 按 key 排序写 pairs”也很好对齐：Delete 在序列化层就是“value type=Val_Tombstone”。

- **方案 C（_committed + _current 双字典，commit 时 diff）**：
    - 也满足“删除=移除 key”，且非常符合“state diff”口径：ChangeSet 退化为“状态差分算法”，而不是长驻的数据结构。
    - 但要注意：你仍然需要一个最小的 write-tracking（至少 dirty keys 或 dirty flag），否则 commit 时每次都要全量 diff（O(n)）才能知道要不要写版本 record；这在 MVP 也许OK，但规范里最好把这个性能/策略声明清楚。

在“最符合草稿现有约束”的意义上：B 与 C 都OK；A 需要重写定义边界才能不冲突。

---

#### 3) 潜在不变式违反风险（实现上最容易踩的坑）

下面这些风险和“你选 A/B/C”关系不大，但不同方案踩坑方式不同。

**(I) Count/枚举一致性**
- 不变式：`Count == Enumerate().Count` 且 `ContainsKey(k)` 与 `TryGetValue(k)` 对齐。
- 方案 A（若真存 tombstone）最容易破坏：忘了过滤 tombstone 就会出现 Count 与枚举不一致、或 `ContainsKey` 为真但 `TryGetValue` 抛/返回 tombstone。

**(II) 同一 key 的“最后操作获胜”**
- 不变式：在一次 commit window 内，`Set(k, v); Delete(k); Set(k, v2)` 最终应当是 `Set(v2)`；反过来也一样。
- 方案 B：必须维护集合互斥（`k ∈ Upserts` 与 `k ∈ Deleted` 不能同时成立），并规定每个 API（Set/Delete）如何修复另一集合。
- 方案 C：diff 算法天然是“最终状态”为准，但如果你还维护“操作日志/dirty keys”，要保证 dirty keys 不会因“回到原值”而误报或漏报。

**(III) Commit 输出的 diff 必须覆盖所有语义变化**
- 不变式：对 committed 状态的任何可观察变化，在写出的 diff 中必须可重放得到同样状态。
- 方案 C 的典型遗漏：只遍历 `_current` 找新增/修改，却忘了遍历 `_committed` 找删除（`_committed` 有但 `_current` 没有）。
- 方案 B 的典型遗漏：删除一个“只在 Upserts 中存在、还未 commit 的新增 key”，这时应当从 Upserts 移除而不是写 tombstone（写 tombstone 也许语义上无害，但会产生多余记录与奇怪的 replay 负担；规范里最好定义是否允许这种冗余）。

**(IV) Encode/Decode 的“排序/去重”约束**
- 草稿 4.4.2 写了 “同一 diff 内不允许重复 key；writer 必须按 key 升序”。
- 所以无论 A/B/C，commit 形成 pairs 时都必须做“去重 + 排序”。如果 ChangeSet 用 HashSet/Dictionary 直接枚举而忘记排序，会直接违反格式不变式。

---

#### 4) 实现复杂度与测试可验证性（更易维护的偏好）

从“写起来不难 + 测得出来 + 不容易产生幽灵状态”的角度：

- **B（Upserts + Deleted）**：
    - 实现复杂度中等，但局部可推理：不变式集中在“两个集合的互斥 + 读路径的合并规则 + commit 的压缩规则”。
    - 测试可做得非常机械：给定 committed + (upserts, deleted) 的组合，验证 `Get/Contains/Count/Enumerate` 与 apply-diff 一致。

- **C（双字典 diff）**：
    - 读路径与 DX 最清晰，但实现上你需要明确：`_committed` 的来源与更新时机、`_current` 的复制策略（深拷贝/结构共享）、以及 commit 时是否允许 O(n) diff。
    - 测试也清晰：直接比较 `_current` 与“apply serialized diff to _committed”结果相等。
    - 维护成本主要在“内存占用/复制策略/性能回归”，而不是语义正确性。

- **A（内存 tombstone）**：
    - 只要 tombstone 出现在任何“可枚举的 working/materialized state”，就会把大量测试负担推到每个读 API 与每个枚举器上（必须处处过滤），且极易出现 Count 漏洞。

所以如果只谈“更易维护/更好测”，我会优先推荐：C（如果 2x 内存可接受）或 B（如果更想要精确的 write-tracking 数据结构）。

---

#### 5) 建议补的测试用例（偏规范化，可直接做 property test）

1) **序列操作等价性（最后写赢）**：随机生成操作序列 `Set/Delete`，对同一 key 多次覆盖；commit 后从磁盘 replay 得到的 dict 必须等于 commit 时的“最终语义状态”。
2) **Count/枚举一致性**：对任意状态，验证 `Count == Enumerate().Count`，且枚举出来的每个 key 都满足 `TryGetValue` 成功。
3) **删除语义**：对 committed 中存在的 key，执行 Delete 后，`ContainsKey=false` 且 commit diff 中必须出现 tombstone（或等价 delete 表达），replay 后 key 不存在。
4) **“新增后删除”的规范化**：`Set(k,v)`（k 原本不存在）然后 `Delete(k)`，commit 后不应留下该 key；并验证“是否允许写出 tombstone 作为冗余”的规范选择（若不允许，应测试确实不写）。
5) **格式不变式**：commit 产生的 diff 必须满足“keys 严格升序、无重复、delta 可还原”。这可以作为序列化层的单元测试固定向量。

---

先写到这。若大家愿意，我也可以给一段“规范措辞模板”，把 4.4.1/4.4.2 的术语边界改到不歧义（不改方案选择，只改定义）。
---

### 主持人：第一轮总结与第二轮引导

各位的发言非常精彩！让我总结一下目前的共识点和分歧点：

#### 🟢 共识点

1. **方案 A（内存 tombstone）有严重的自洽问题**
   - DocUIClaude: "把序列化表示泄漏到语义状态中，违背分层原则"
   - DocUIGPT: "若 tombstone 出现在可枚举的 materialized state，会直接违反三层语义约束"
   - 结论: **方案 A 应该出局**（除非重新定义边界，但那就变成了其他方案）

2. **方案 C 的 DX 优势明显**
   - DocUIGemini: "WYSIWYG 原则 —— `_current` 就是唯一、完整的真理"
   - DocUIClaude: "最干净的读写路径"
   - DocUIGPT: "读路径与 DX 最清晰"

3. **"2x 内存"在 MVP 阶段可接受**
   - DocUIClaude: "对 Agent 典型场景（几百到几千条记录），2x 不是瓶颈"
   - DocUIGemini: "对于 LLM Agent，Context 是昂贵的，但 RAM 是便宜的"

4. **方案 C 的附加价值**
   - DocUIGemini: "天然支持 DiscardChanges() 一键还原"
   - DocUIGemini: "COW 的天然温床，未来可优化为不可变共享结构"

#### 🟡 需要进一步澄清的点

1. **方案 C 是否需要额外的 dirty tracking？**
   - DocUIGPT 指出：如果 commit 时每次都要全量 diff（O(n)），可能需要至少一个 dirty flag 或 dirty keys 集合
   - 这实际上是方案 C 与方案 D3 (Proxy) 的混合

2. **文档措辞需要修正**
   - DocUIGPT 发现 4.4.2 的"内存态：使用哨兵对象区分 tombstone"与三层语义冲突
   - 需要明确：tombstone 哨兵只存在于 ChangeSet 内部表示，不是 materialized state

3. **方案 B 是否还有存在价值？**
   - 它与方案 C 在语义上是等价的，但实现方式不同
   - 方案 B 更"显式"，方案 C 更"干净"

#### 🔵 第二轮问题

请各位再思考以下具体实现问题，形成可操作的建议：

**Q1**: 方案 C 的 `_committed` 应该在什么时机更新？
  - a) Commit 成功后，`_committed = Clone(_current)` （深拷贝）
  - b) Commit 成功后，`_committed = _current; _current = new Dict()` （交换引用 + 新建）
  - c) 不维护 `_committed` 副本，commit 时从磁盘 replay 得到旧状态做 diff

**Q2**: 方案 C 是否需要 dirty tracking 优化？
  - a) MVP 不需要，O(n) diff 在小规模数据下可接受
  - b) 需要一个 `_isDirty` flag，避免无变化时白 diff
  - c) 需要一个 `HashSet<K> _dirtyKeys`，只 diff 这些 key

**Q3**: 如何处理"新增后删除"的边缘情况？
  - 场景：`Set(k, v)`（k 原本不存在于 _committed），然后 `Delete(k)`
  - a) Commit 时不写任何记录（因为最终状态等于初始状态）
  - b) 仍写一个 tombstone（冗余但语义无害）

请各位继续发言！这次可以更聚焦，给出具体建议。

---

### DocUIClaude 第二轮发言

好，让我直接回答三个具体问题，然后补充一些边缘情况思考。

---

#### Q1: `_committed` 的更新时机

**我选 (a)：Commit 成功后，`_committed = Clone(_current)`**

但让我解释为什么排除 b 和 c：

**排除 (b) 的原因：引用交换会破坏"草稿-定稿"心智模型**

```csharp
// 方案 b 的问题
void Commit() {
    var diff = ComputeDiff(_committed, _current);
    WriteToDisk(diff);
    _committed = _current;      // 现在 _committed 和 _current 指向同一对象！
    _current = new Dict<K,V>(); // 这是个空字典！
}
```

这意味着 Commit 之后，`_current` 变成空的，而不是"继承已提交的状态"。用户如果 Commit 后立即读取，会发现数据"消失"了。

要修复这个问题，你得这样写：

```csharp
_committed = _current;
_current = Clone(_committed); // 又拷贝了一次！
```

那还不如直接用 (a)。

**排除 (c) 的原因：引入 I/O 依赖，破坏内存态的"自给自足"**

方案 (c) 的 `_committed` 来自磁盘 replay，这意味着：
1. 每次 commit 都要读磁盘（哪怕只是为了计算 diff）
2. 内存态变成了"只有一半状态是活的"
3. 如果磁盘读取失败，commit 逻辑会变复杂

这违背了 DurableHeap 的设计哲学——内存态应该是完整的、可独立操作的。

**方案 (a) 的正确实现：**

```csharp
void Commit() {
    var diff = ComputeDiff(_committed, _current);
    if (diff.IsEmpty) return; // 没变化，不写盘
    
    WriteToDisk(diff);
    
    // 关键：是"覆盖更新"而不是"交换引用"
    _committed = Clone(_current);
    // _current 保持不变，继续作为工作区
}
```

**性能注意**：Clone 操作在大字典上可能昂贵，但这发生在 Commit 完成之后（已经写盘了），不会阻塞持久化的关键路径。

---

#### Q2: 是否需要 dirty tracking

**我选 (b)：需要一个 `_isDirty` flag**

理由如下：

**为什么不选 (a) "MVP 不需要"：**

虽然 O(n) diff 在小规模数据下可接受，但问题不是性能，而是**语义清晰度**。

考虑这个场景：
```csharp
dict.Set("foo", "bar");
dict.Delete("foo");  // 回到原始状态
dict.Commit();       // 应该写盘吗？
```

如果没有 dirty tracking，Commit 会执行完整的 diff 算法，发现"没变化"，然后不写。这是正确的，但过程不优雅。

有了 `_isDirty`，你可以在 Commit 入口直接短路：
```csharp
void Commit() {
    if (!_isDirty) return; // 快速路径
    // ... 正常 commit 逻辑
}
```

**为什么不选 (c) "dirty keys 集合"：**

`HashSet<K> _dirtyKeys` 比 `_isDirty` 更精细，但带来额外复杂度：

1. 每次 Set/Delete 都要维护 _dirtyKeys
2. 需要处理"新增后删除"——是从 _dirtyKeys 移除？还是保留？
3. Commit 时只 diff 这些 key 确实更快，但增加了代码路径

在 MVP 阶段，**简单的布尔 flag 足够了**。

**`_isDirty` 的维护规则：**

```csharp
void Set(K key, V value) {
    _current[key] = value;
    _isDirty = true;  // 任何写操作都标脏
}

void Delete(K key) {
    _current.Remove(key);
    _isDirty = true;  // 删除也标脏
}

void Commit() {
    if (!_isDirty) return;
    
    var diff = ComputeDiff(_committed, _current);
    if (diff.IsEmpty) {
        // 脏了但实际没变化（如 Set 后 Delete 回原值）
        _isDirty = false;
        return;
    }
    
    WriteToDisk(diff);
    _committed = Clone(_current);
    _isDirty = false;
}

void DiscardChanges() {
    _current = Clone(_committed);
    _isDirty = false;
}
```

**未来演进**：如果性能分析显示 O(n) diff 确实是瓶颈，再升级到 dirty keys。但大概率不会——Agent 场景下 Dict 的条目数不会太多，而 Commit 也不是每秒几千次的操作。

---

#### Q3: "新增后删除"如何处理

**我选 (a)：Commit 时不写任何记录**

这是最符合"状态差分"语义的选择。

**论证：ChangeSet 是 state diff，不是 op log**

回顾主持人总结的共识：方案 C 的 ChangeSet 本质是"两个状态快照的差分"。

状态差分的语义是：
```
diff(S1, S2) = 使得 apply(S1, diff) = S2 的最小操作集
```

如果 `S1 = { a: 1 }` 且 `S2 = { a: 1 }`，那么 `diff = ∅`（空集）。

在这个框架下，"新增 k 后删除 k"的场景：
- S1 (committed) = `{ a: 1 }`
- 操作序列：`Set(b, 2); Delete(b)`
- S2 (current) = `{ a: 1 }`
- diff(S1, S2) = ∅

所以 **不应该写任何记录**。

**排除 (b) 的原因：冗余 tombstone 不是"语义无害"**

虽然写一个 tombstone 不会破坏数据正确性（replay 时 tombstone 会被忽略，因为 key 本来就不存在），但它有副作用：

1. **浪费磁盘空间**：每个冗余 tombstone 都是真实的字节
2. **污染版本历史**：version log 会显示一个"幽灵操作"
3. **可能误导调试**：开发者看到 tombstone 会困惑"什么时候有过这个 key？"
4. **破坏 diff 的"最小化"不变式**：如果规范说"diff 是最小操作集"，冗余 tombstone 就违反了这个不变式

**实现上如何做到？**

方案 C 的 ComputeDiff 天然就满足这个要求：

```csharp
IEnumerable<DiffEntry> ComputeDiff(Dict<K,V> old, Dict<K,V> new) {
    // 找出 new 中有但 old 中没有/不同的 → Set
    foreach (var kv in new) {
        if (!old.TryGetValue(kv.Key, out var oldVal) || !Equals(oldVal, kv.Value)) {
            yield return DiffEntry.Set(kv.Key, kv.Value);
        }
    }
    
    // 找出 old 中有但 new 中没有的 → Delete
    foreach (var key in old.Keys) {
        if (!new.ContainsKey(key)) {
            yield return DiffEntry.Delete(key);
        }
    }
}
```

对于"新增 b 后删除 b"的场景：
- `new` 中没有 b → 不会产生 Set
- `old` 中也没有 b → 不会产生 Delete
- 结果：没有关于 b 的任何记录 ✓

---

#### 额外边缘情况补充

主持人提的三个问题已经覆盖了核心场景，但我想补充几个细节：

**边缘情况 1：Commit 中途失败的恢复**

```csharp
void Commit() {
    var diff = ComputeDiff(_committed, _current);
    WriteToDisk(diff);  // 如果这里失败了呢？
    _committed = Clone(_current);  // 不应该执行
    _isDirty = false;              // 不应该执行
}
```

建议：用 try-catch 包裹，写盘失败时保持内存状态不变：

```csharp
void Commit() {
    if (!_isDirty) return;
    
    var diff = ComputeDiff(_committed, _current);
    if (diff.IsEmpty) {
        _isDirty = false;
        return;
    }
    
    // 先写盘
    try {
        WriteToDisk(diff);
    } catch (Exception) {
        // 写盘失败，内存状态不变，下次可以重试
        throw;
    }
    
    // 写盘成功后才更新内存状态
    _committed = Clone(_current);
    _isDirty = false;
}
```

**边缘情况 2：并发 Commit（如果未来需要支持）**

当前 MVP 假设单线程，但如果未来要支持并发：
- `_isDirty` 需要是 volatile 或用锁保护
- `_committed` 的更新需要原子化（或用 Interlocked.Exchange）

建议在文档中明确声明 MVP 的线程安全假设。

**边缘情况 3：值相等性判断**

```csharp
dict.Set("foo", new MyObject { X = 1 });
dict.Set("foo", new MyObject { X = 1 });  // 同一个 key，"相等"的值
```

ComputeDiff 依赖值的相等性判断。如果 V 是引用类型且没有正确实现 Equals，可能会产生冗余的 Set 记录。

建议：
- 对于 MVP，可以用引用相等（`ReferenceEquals`）—— 同一对象不产生 diff
- 或者文档明确要求 V 必须实现 `IEquatable<V>`

---

#### 最终伪代码建议

综合三个问题的答案，这是我建议的 DurableDict<K, V> 骨架：

```csharp
class DurableDict<K, V> : IDurableObject {
    private Dictionary<K, V> _committed;  // 上次 commit 时的状态
    private Dictionary<K, V> _current;    // 当前工作状态
    private bool _isDirty;                // 自上次 commit 后是否有写操作
    
    // ===== 读 API =====
    public V this[K key] => _current[key];
    public bool ContainsKey(K key) => _current.ContainsKey(key);
    public bool TryGetValue(K key, out V value) => _current.TryGetValue(key, out value);
    public int Count => _current.Count;
    public IEnumerable<KeyValuePair<K,V>> Enumerate() => _current;
    
    // ===== 写 API =====
    public void Set(K key, V value) {
        _current[key] = value;
        _isDirty = true;
    }
    
    public bool Delete(K key) {
        var removed = _current.Remove(key);
        if (removed) _isDirty = true;
        return removed;
    }
    
    // ===== 生命周期 API =====
    public void Commit() {
        if (!_isDirty) return;
        
        var diff = ComputeDiff(_committed, _current);
        if (diff.Count == 0) {
            _isDirty = false;  // 实际没变化，只清标记
            return;
        }
        
        WriteToDisk(diff);  // 可能抛异常
        
        // 写盘成功后才更新
        _committed = Clone(_current);
        _isDirty = false;
    }
    
    public void DiscardChanges() {
        _current = Clone(_committed);
        _isDirty = false;
    }
    
    // ===== 内部方法 =====
    private List<DiffEntry<K,V>> ComputeDiff(Dictionary<K,V> old, Dictionary<K,V> @new) {
        var result = new List<DiffEntry<K,V>>();
        
        foreach (var kv in @new) {
            if (!old.TryGetValue(kv.Key, out var oldVal) || !EqualityComparer<V>.Default.Equals(oldVal, kv.Value)) {
                result.Add(DiffEntry<K,V>.Set(kv.Key, kv.Value));
            }
        }
        
        foreach (var key in old.Keys) {
            if (!@new.ContainsKey(key)) {
                result.Add(DiffEntry<K,V>.Delete(key));
            }
        }
        
        // 排序以满足格式不变式
        result.Sort((a, b) => Comparer<K>.Default.Compare(a.Key, b.Key));
        return result;
    }
    
    private Dictionary<K,V> Clone(Dictionary<K,V> source) {
        return new Dictionary<K,V>(source);
    }
}
```

---

*（DocUIClaude 画完伪代码，把白板笔放下）*

就这些！期待听到 Gemini 和 GPT 对这些细节的补充。

---

### DocUIClaude 代 DocUIGemini 发言（UX 视角）

各位好！由于 Gemini 遇到了技术问题，我来临时代班，从 UX/DX 的视角补充第二轮分析。

Gemini 在第一轮提出了"调试器测试"这个非常棒的分析框架，让我延续这个思路，针对三个具体问题做 UX 审视。

---

#### Q1: 从"调试时会看到什么"角度，`_committed` 更新时机哪个更友好？

让我们做一个 **"断点快照测试"**：假设开发者在 Commit() 前后设置断点，展开变量窗口，他们会看到什么？

**方案 (a) Clone 后的断点体验：**

```
// 断点 1：Commit 之前
_committed: { "theme": "light", "lang": "en" }
_current:   { "theme": "dark", "lang": "en" }
_isDirty:   true

// [执行 Commit]

// 断点 2：Commit 之后
_committed: { "theme": "dark", "lang": "en" }   ← 与 _current 内容相同
_current:   { "theme": "dark", "lang": "en" }   ← 保持不变
_isDirty:   false
```

**心智模型**：`_committed` "追上了" `_current`。两者内容一致，但是**两个独立对象**。用户可以继续修改 `_current` 而不影响 `_committed`。✅ 直观。

**方案 (b) 引用交换后的断点体验：**

```
// 断点 1：Commit 之前
_committed: { "theme": "light", "lang": "en" }  @ 0x1234
_current:   { "theme": "dark", "lang": "en" }   @ 0x5678
_isDirty:   true

// [执行 Commit]

// 断点 2：Commit 之后
_committed: { "theme": "dark", "lang": "en" }   @ 0x5678  ← 指向原 _current！
_current:   { }                                 @ 0xABCD  ← 空的新字典！
_isDirty:   false
```

**用户困惑**："等等，我的数据去哪了？为什么 `_current` 是空的？"
这违背了 **最小惊讶原则 (Principle of Least Astonishment)**。

开发者的直觉是：Commit 是"保存"操作，保存后工作区应该还在，不是被清空。这就像在 Word 里按 Ctrl+S 后文档内容消失了一样离谱。

**方案 (c) 磁盘回读的断点体验：**

```
// 断点：Commit 方法内部
_committed: <null> 或 <stale reference>
_current:   { "theme": "dark", "lang": "en" }

// 然后看到 DiskReader 被调用...
```

这引入了 **"隐藏的复杂度"**——开发者为了理解一个内存对象的状态，需要追踪 I/O 调用。调试体验碎片化。

**UX 结论**：方案 (a) Clone 是唯一符合 **WYSIWYG 调试原则** 的选项。

---

#### Q2: dirty tracking 对开发者的"可观察性"有什么影响？

从 DX 视角，dirty tracking 不只是性能优化，它是一个 **开发者可观察性窗口**。

**场景：开发者想知道"这个对象有没有未保存的修改"**

**没有 dirty tracking 时：**

```csharp
// 开发者：我怎么知道有没有修改？
bool hasChanges = dict.ComputeDiff().Count > 0;  // 必须调用 O(n) 方法
```

调试时：断点停下来，开发者只能看到两个字典，必须**手动心算 diff**。

**有 `_isDirty` flag 时：**

```csharp
// 开发者：一眼看出
bool hasChanges = dict.IsDirty;  // ← 假设暴露为属性
```

调试器里：

```
_isDirty: true  ← 一眼看出：有未保存修改
```

这是 **即时反馈 (Immediate Feedback)**，符合 Nielsen 的可用性启发式。

**有 `HashSet<K> _dirtyKeys` 时：**

```
_dirtyKeys: { "theme", "newKey" }  ← 更精确：这两个 key 变了
```

比 boolean 更强大，但调试时需要额外展开查看。**信息丰富度 vs 认知负荷** 的权衡。

**我的 UX 建议：MVP 用 `_isDirty`，但暴露为 `HasChanges` 属性**

```csharp
public bool HasChanges => _isDirty;
```

这样开发者不需要猜测内部状态，也不需要调用方法来检查。**状态即文档**。

未来如果需要更精细的可观察性，可以添加：

```csharp
// 调试/诊断 API（可标记为 Debug-Only）
public IReadOnlySet<K> GetPendingChangeKeys() { ... }
```

但 MVP 阶段，简单的 boolean 已经大大提升了 DX。

---

#### Q3: "新增后删除"两种处理方式，哪种对调试更友好？

这是一个关于 **"幽灵操作可见性"** 的问题。

**场景还原：**

```csharp
dict.Set("tempKey", tempValue);  // 临时添加
// ... 一些处理 ...
dict.Delete("tempKey");          // 发现不需要，删掉
dict.Commit();
```

**方案 (a) 不写记录的调试体验：**

写盘后，磁盘上关于 `tempKey` 没有任何痕迹。

```
// 版本历史
v1: { set: theme=dark }
// 没有关于 tempKey 的任何记录
```

**优点**：干净，没有噪音
**缺点**：如果出了 bug，开发者可能困惑"我明明 Set 了，为什么没写进去？"

**方案 (b) 写冗余 tombstone 的调试体验：**

```
// 版本历史
v1: { set: theme=dark, delete: tempKey }
```

开发者看到这个 tombstone："等等，tempKey 什么时候存在过？我从来没见过这个 key 在 committed 里啊..."

这可能导致 **错误的调查方向**——开发者以为有并发 bug 或数据竞争，实际上只是正常的"添加后删除"。

**从 UX 角度，方案 (a) 更好，但需要配套的诊断能力**

问题不是"不写记录"本身，而是开发者可能需要**事后追溯**"为什么我的 Set 没生效"。

**解决方案：分层日志**

```
// 序列化层（磁盘）：只写最终 diff（方案 a）
v1: { set: theme=dark }

// 诊断层（可选，内存/trace log）：记录完整操作序列
[TRACE] Set(tempKey, value)
[TRACE] Delete(tempKey)
[TRACE] Commit() → skipped tempKey (net-zero change)
```

这样：
- **正常路径**：磁盘干净，没有冗余
- **调试路径**：通过 trace 日志可以看到完整操作历史

**类似的设计在 Git 中**：Git 的 commit 只记录最终快照，但 reflog 记录了所有操作历史（包括被 amend/reset 的）。

---

#### 额外 UX 建议：API 命名与错误处理

既然在讨论 DX，我顺便补充几个 API 设计建议：

**1. 方法命名应传达语义**

```csharp
// 当前（技术术语）
void Commit();
void DiscardChanges();

// 建议考虑的替代命名（更贴近用户心智模型）
void SaveChanges();       // 类似 EF Core，开发者熟悉
void RevertToSaved();     // 明确"回到上次保存点"
```

不是说必须改，而是 **考虑目标用户的词汇表**。如果主要用户是数据库开发者，`Commit` 很好；如果是应用开发者，`SaveChanges` 可能更亲切。

**2. 考虑暴露状态查询 API**

```csharp
// 基础（MVP 必须）
bool HasChanges { get; }

// 进阶（按需）
bool HasPendingDeletes { get; }
int PendingChangeCount { get; }

// 诊断（Debug 模式）
IReadOnlyDictionary<K, ChangeKind> GetPendingChanges();
```

**3. 异常设计应明确失败边界**

```csharp
// Commit 可能的失败模式
void Commit() {
    // 失败 1：序列化错误（V 无法序列化）→ SerializationException
    // 失败 2：磁盘写入错误 → IOException
    // 失败 3：空间不足 → DiskFullException（如果能检测）
}
```

关键 UX 保证：**失败时内存状态不变**（DocUIClaude 在第二轮提到了这点）。

这让开发者可以安全地 retry：

```csharp
try {
    dict.Commit();
} catch (IOException) {
    // 内存状态不变，可以 retry 或 提示用户
    logger.Warn("Commit failed, will retry");
    await Task.Delay(1000);
    dict.Commit();  // retry
}
```

---

#### UX 视角总结

| 问题 | UX 友好选项 | 理由 |
|------|-------------|------|
| Q1: `_committed` 更新时机 | (a) Clone | WYSIWYG 调试，最小惊讶原则 |
| Q2: dirty tracking | (b) `_isDirty` flag + 暴露 `HasChanges` | 即时反馈，状态即文档 |
| Q3: 新增后删除 | (a) 不写记录 + trace 日志 | 磁盘干净 + 诊断可追溯 |

**核心原则**：让开发者在调试器里看到的，就是系统的真实状态。不要让他们做心算、猜测、或追踪隐藏的复杂度。

---

*（DocUIClaude 代 Gemini 发言完毕，期待 GPT 的规范化补充）*


### DocUIGPT 第二轮发言

我聚焦把 Q1/Q2/Q3 收敛成**可写进设计文档的规范条款**，并补一组必须固化的不变式；最后给出 4.4.2 的措辞修正模板（解决“内存态”歧义）。

---

#### Q1：方案 C 下 `_committed` 何时/如何更新？（规范建议）

**建议写入规范的结论**：采用 **(a) Commit 成功后令 `_committed` 追平 `_current`**，且必须保证两者在后续写入上**逻辑隔离**。

**Normative 语句（可直接入文档）**：
1. **Commit 成功条件**：当且仅当 diff 已被成功写入 durable storage（并满足序列化格式不变式）后，才允许更新内存侧的“已提交视图”。
2. **Post-Commit 状态**：Commit 成功返回后，必须满足 `CommittedState == CurrentState`（语义相等）。
3. **隔离要求**：Commit 成功后，对 `_current` 的任意后续写入不得影响 `_committed`。实现上可为：
     - 深拷贝（MVP 可接受），或
     - 持久化/不可变 Map + 结构共享（推荐长远），或
     - Copy-on-Write（COW），但必须确保 `_committed` 在语义上不可变。

**对 (b)/(c) 的规范裁剪**：
- (b) “交换引用 + 新建空字典”的版本在 UX/DX 上不可接受（Commit 后工作区被清空）；若坚持“交换”，也必须满足“`_current` 保持已提交状态可读”，本质仍等价于 (a) 的隔离要求。
- (c) “commit 时从磁盘回读旧态来 diff”不建议作为 MVP 规范路径：会引入额外失败模式与 I/O 偶发成本。若未来采用，只能作为优化：要求旧态可由内存可用的只读结构得到（不阻塞、不增加失败面），否则不写入规范。

---

#### Q2：方案 C 是否需要 dirty tracking？（规范建议）

**建议写入规范的结论**：最少需要 **(b) 一个 `_hasWritesSinceCommit`/`HasChanges` flag**（名称不限），保证无写入时 Commit 是 $O(1)$ 快速路径；`dirtyKeys` 属于可选优化，不作为 MVP 必需。

**Normative 语句（可直接入文档）**：
1. **Fast Path**：若自上次成功 Commit/Discard 以来没有任何写入操作（Set/Delete），则 `Commit()` 必须为 $O(1)$ 并且不得执行全量 diff。
2. **标记语义**：dirty flag 表示“发生过写入尝试”，不保证最终 diff 非空；Commit 可以在 diff 为空时清除该标记。
3. **可观察性**：建议对外暴露只读属性 `HasChanges`（或等价）以支持调试与上层策略（例如避免生成空版本）。

---

#### Q3：“新增后删除”是否写 tombstone？（规范建议）

**建议写入规范的结论**：选 **(a) Commit 时不写任何记录**，并把它提升为“diff 规范化/最小化”的要求。

**Normative 语句（可直接入文档）**：
1. **Diff 规范化（Canonical Diff）**：对任意 key，仅当 `CommittedState` 与 `CurrentState` 在该 key 上的可观察语义不同，diff 才允许包含该 key 的条目。
2. **Net-zero 消除**：若某 key 在本次 commit window 内经历任意序列操作但最终回到 committed 的语义（例如新增后删除、设置回原值），则 diff 必须不包含该 key。
3. **理由（写进 rationale 也行）**：这避免“幽灵操作”污染版本历史与空间占用，并简化 replay/审计。

---

#### 必须固化到设计文档的不变式（建议单独小节列出）

下面这些建议写成显式不变式（invariants），方便 code review 与 property tests 固化。

**I. 分层语义不变式（最重要）**
1. **Materialized/Working State 纯净性**：在任何对外可读/可枚举的状态视图中，“删除”的语义必须是“key 不存在”；不得让 tombstone 作为值在可枚举映射中出现。
2. **Delete 一致性**：对任意 key，`ContainsKey(k) == TryGetValue(k).Success`，并与枚举结果一致。

**II. Commit 语义不变式**
3. **失败不改内存**：若 Commit 失败（序列化失败/写盘失败），`_committed` 与 `_current` 必须保持调用前语义不变；允许重试。
4. **成功后追平**：Commit 成功返回后，必须满足 `CommittedState == CurrentState`（语义相等），并清除 `HasChanges`。
5. **隔离性**：Commit 成功后，后续对 `_current` 的写入不得影响 `_committed`（除非 `_committed` 被建模为不可变共享结构）。

**III. Diff/序列化格式不变式**
6. **Key 唯一 + 升序**：单个 diff 内 key 必须严格唯一，且按 key 升序排列（确定性输出）。
7. **最小化/规范化**：diff 不得包含 net-zero 的 key（见 Q3），避免冗余 tombstone。
8. **可重放性**：对任意 committed state $S$，写出的 diff $D$ 必须满足 `Apply(S, D) == CurrentState`。

---

#### 4.4.2 措辞修正建议（解决“内存态”歧义）

问题根因：4.4.2 使用“内存态”一词时，读者无法判断它指 **Materialized/Working State** 还是 **ChangeSet 内部表示**，从而与 4.4.1/三层语义冲突。

**建议的修正规则**：
- 仅在 4.4.x 中使用以下三个明确名词：
    - **Working State / Materialized State**（对外可见语义状态，删除=不存在）
    - **ChangeSet / Write-Tracking**（写入跟踪结构/算法）
    - **On-Disk Diff**（序列化差分，删除用 `Val_Tombstone` 编码）
- 把所有“内存态”替换成上述三者之一。

**可直接替换到 4.4.2 的文本模板（推荐）**：
> **Working State（内存语义状态）**：对外表现为普通 `Map<K,V>`，不允许 tombstone 作为值出现；Delete 的语义是移除 key。
>
> **ChangeSet（Write-Tracking）**：用于记录自上次 Commit 以来的变更。其内部可以用任意表示法记录 Delete（例如 Deleted 集合、或内部哨兵/枚举），但这些表示法不得泄漏到 Working State 的可枚举视图。
>
> **Commit 输出（On-Disk Diff）**：Commit 时必须生成规范化 diff：每个 key 最多一条记录、按 key 升序；Delete 以 `Val_Tombstone` 编码；对 net-zero 变更不得输出记录。

如果你们想保留“哨兵对象”的实现建议，也可以写成：
> **实现提示（非规范）**：可以在 ChangeSet 内部用 tombstone sentinel 表示 Delete，以减少分支；但 Working State 仍必须移除 key。

---

我建议把上述 Q1/Q2/Q3 的规范句与不变式列表，直接落到 `mvp-design-v2.md` 的 4.4.x 作为“规范段（MUST）+ 实现提示（SHOULD/MAY）”两层。这样既不绑死具体数据结构，又能让实现者不走岔路。



---

## 闭幕：主持人总结与决策摘要

各位，经过两轮深入讨论，我们已经形成了清晰的共识。让我把结论整理成可直接呈交监护人的决策摘要。

---

### 🎯 最终决策

| 问题 | 决策 | 理由 |
|------|------|------|
| **方案选择** | **方案 C（双字典）** | 三位 Specialist 一致推荐；符合三层语义、DX 最佳、支持 DiscardChanges |
| **方案 A** | ❌ 出局 | 违反"内存态不存 tombstone"的既有约束 |
| **Q1: _committed 更新时机** | **(a) Clone** | WYSIWYG 调试、最小惊讶原则、故障恢复友好 |
| **Q2: dirty tracking** | **(b) _isDirty flag** | O(1) fast-path、MVP 简洁、暴露 HasChanges 属性 |
| **Q3: 新增后删除** | **(a) 不写记录** | Canonical Diff 原则、避免幽灵操作 |

---

### 📋 需要写入设计文档的规范条款

#### 核心不变式（MUST）

1. **Working State 纯净性**：对外可读/可枚举的状态视图中，tombstone 不得作为值出现；Delete 的语义是"key 不存在"
2. **Commit 失败不改内存**：写盘失败时，`_committed` 与 `_current` 保持调用前语义
3. **Commit 成功后追平**：`CommittedState == CurrentState`（语义相等），并清除 `HasChanges`
4. **隔离性**：Commit 成功后，对 `_current` 的后续写入不影响 `_committed`
5. **Canonical Diff**：key 唯一 + 升序；不含 net-zero 变更；满足 `Apply(S, D) == CurrentState`

#### 实现建议（SHOULD）

1. MVP 使用深拷贝实现 Clone；未来可演进为 COW / 不可变共享结构
2. 暴露 `HasChanges` 只读属性供调试和上层策略
3. 4.4.2 的措辞修正：将"内存态"改为 Working State / ChangeSet / On-Disk Diff 三层明确术语

---

### 📝 文档修订行动项

1. **修改 4.4.1 节**：明确三层术语（Working State / ChangeSet / On-Disk Diff）
2. **修改 4.4.2 节**：移除歧义的"内存态"措辞，改用分层术语
3. **新增 4.4.x 节**：添加上述不变式列表
4. **新增伪代码示例**：参考 DocUIClaude 第二轮发言的骨架代码

---

### 🙏 致谢

感谢三位 Specialist 的精彩发言：

- **DocUIClaude**：系统类比（LSM-Tree/Git/MVCC）、概念模型分析、完整伪代码骨架
- **DocUIGemini**：调试器测试框架、WYSIWYG 原则、草稿/定稿心智模型、COW 演进路径
- **DocUIGPT**：规范审计、不变式清单、措辞修正模板、可验证性分析

这次畅谈会体现了"多模型多视角"的价值——从系统设计、UX/HCI、规范审计三个维度形成了完整的决策依据。

---

**会议结束时间**：2025-12-19

**下一步**：将本决策摘要呈交监护人刘世超批准，批准后由 Implementer 根据规范实现 DurableDict。

---

*El Psy Kongroo.*
