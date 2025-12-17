# DurableHeap MVP 设计畅谈 📐

> **形式**: 迭代畅谈 (Design Iteration)
> **日期**: 2025-12-16
> **目标**: 产出足够明确可以开始编码的 MVP 设计文档
> **结束条件**: 所有与会者确认方案已解决/绕过所有发现的问题

---

## 监护人的 MVP Feature List 草稿

1. **自描述格式**：像 CBOR，无需 Schema，不处理自定义类型
2. **值类型 vs 引用类型**：
   - 值类型：inline 布局
   - 引用类型：flat 布局
3. **MVP 简化**：
   - 只处理单文件存储
   - 只支持 4 种类型：`int`, `string`, `JObject`(引用), `JArray`(引用)
   - 暂不处理对象销毁/回收
4. **核心特征**：增量序列化、Durable-Pointer、mmap、lazy wrapper
5. **技术栈**：.NET 9.0 + C# + xUnit
6. **文件布局**：
   - 预划分文件（如 256MB）
   - 逐步逻辑 append 内容
   - 文件尾部双 meta 存 root object offset
7. **Root object**：尾部存 length，方便从尾部向前迭代

### 监护人的布局提案（供探讨）

**传统布局**：
```
┌────────┬──────────┬────────┬─────────┐
│ Magic  │  Data... │ Length │  CRC    │
└────────┴──────────┴────────┴─────────┘
  ^-- Pointer 指向这里
```

**逆序布局提案**（考虑 IBufferWriter 难以提前知道 Length）：
```
┌────────┬──────────┬────────┬─────────┐
│ Magic  │  Data... │ Length │  CRC    │
└────────┴──────────┴────────┴─────────┘
                              ^-- Durable-Pointer 指向尾部
```

**或首尾都写 length**（最方便数据恢复）：
```
┌────────┬────────┬──────────┬────────┬─────────┐
│ Length │ Magic  │  Data... │ Length │  CRC    │
└────────┴────────┴──────────┴────────┴─────────┘
```

**参考**：[ChunkedReservableWriter](atelia/src/Data/ChunkedReservableWriter.cs) 提供了预留空间后回填的机制。

---

## 待解决问题清单

| # | 问题 | 状态 | 解决方案 |
|---|------|------|----------|
| Q1 | Record 的具体二进制布局？ | ✅ 已解决 | 混合风格：值类型 CBOR + 引用类型固定布局 |
| Q2 | 值类型（int/string）如何 inline？ | ✅ 已解决 | Tag + Varint (int) / Tag + Len + UTF8 (string) |
| Q3 | 引用类型（JObject/JArray）如何 flat？ | ✅ 已解决 | Tag + TotalLen + Count + OffsetTable + Data |
| Q4 | Durable-Pointer 的格式？指向头还是尾？ | ✅ 已解决 | 8B 绝对偏移，指向头（Tag 字节） |
| Q5 | 文件预分配 + 逻辑 append 的具体机制？ | ✅ 已解决 | 稀疏文件 SetLength(1GB) |
| Q6 | 双 meta 的格式和切换逻辑？ | ✅ 已解决 | 头部 4KB×2 Ping-Pong Superblock |
| Q7 | Lazy Wrapper 的 C# API 设计？ | ✅ 已解决 | DurableRef<T> + IDurable static abstract |
| Q8 | 崩溃恢复：如何找到最后一个有效 root？ | ✅ 已解决 | 读两个 Superblock，取 Seq 大且 CRC 有效的 |
| Q9 | Key 内联 vs 引用？ | ✅ 已解决 | MVP 内联，字符串池留给后续优化 |
| Q10 | 顶层 Record Footer 格式？ | ✅ 已解决 | TotalLen(4B) + CRC32(4B) |

---

## 畅谈规则

- 目标是收敛到可编码的设计
- 提出问题时标注 `[Q]`
- 提出解决方案时标注 `[A]`
- 认同方案时标注 `[+1]` 并说明理由
- 发现问题时标注 `[!]` 并说明

---

## 畅谈区

### Team Leader 开场

欢迎来到 MVP 设计畅谈！

监护人的 feature list 很清晰。我先梳理一下核心设计决策点：

**决策点 1：基元类型的二进制布局**

```
候选 A：CBOR 风格（类型标签 + 长度 + 数据）
  [type:1B][length:varint][data...]

候选 B：FlatBuffer 风格（固定头 + 偏移表）
  [vtable_offset:4B][...fields...]

候选 C：混合风格
  值类型用 CBOR 风格（紧凑）
  引用类型用固定布局（快速随机访问）
```

我倾向 **候选 C**，理由：
- int 用 CBOR 的 varint 可以省空间
- string 需要长度前缀
- JObject/JArray 作为引用类型，需要快速定位字段，固定布局更好

**[Q] 大家怎么看？**

---

**决策点 2：Durable-Pointer 指向位置**

监护人提出了逆序布局。我理解其优势：

```
写入流程（传统）：
1. 预留 Length 空间
2. 写 Magic
3. 写 Data
4. 回填 Length
5. 写 CRC

写入流程（逆序，指针指尾）：
1. 写 Magic
2. 写 Data
3. 写 Length（此时已知）
4. 写 CRC
不需要预留！
```

**[Q] 但读取时，逆序布局是否会增加复杂度？**

---

**决策点 3：文件预分配策略**

```
方案 A：固定大小预分配（256MB）
  优点：简单
  缺点：小 Agent 浪费空间

方案 B：指数增长（64KB → 1MB → 16MB → 256MB）
  优点：适应不同规模
  缺点：多次 resize

方案 C：稀疏文件（虚拟大小 1GB，实际按需分配）
  优点：两全其美
  缺点：需要 OS 支持（Linux/Windows 都支持）
```

**[Q] MVP 阶段用哪个？我倾向方案 A（最简单）或方案 C（如果稀疏文件好用）。**

---

期待大家的分析！

---

### DocUIGemini 的想法

你好！作为 DocUI 的设计顾问，我非常关注 DurableHeap，因为它是 Agent 的 **"海马体" (Hippocampus)**——负责将短期的 Working Memory (Context Window) 转录为长期的 Episodic Memory。

如果 DurableHeap 设计得当，Agent 就能拥有 **"瞬间唤醒" (Instant Wake-up)** 和 **"时间旅行" (Time Travel)** 的能力。

#### 1. 二进制布局：支持 "Lazy Access" 是关键 (Q1, Q3)

我强烈支持 **Team Leader 的候选 C (混合风格)**，并建议进一步明确 **JObject** 的布局以支持 **O(1) 字段访问**。

**[A] JObject 布局提案 (FlatBuffer 风格)**
为了让 Agent 在读取大对象（如包含 100 个工具定义的 Registry）时不必全量解析，建议引入 **Offset Table**：

```
[Tag: 1B]
[TotalLength: 4B]
[FieldCount: 2B]
[OffsetTable: 4B * FieldCount]  <-- 关键：相对起始位置的偏移量
[Key1_Ref][Value1_Ref]          <-- 引用或内联数据
[Key2_Ref][Value2_Ref]
...
```

*   **理由**: 这样 `DurableLazy<JObject>` 可以只读取 Header 和 OffsetTable，当 Agent 真正请求 `obj["tool_x"]` 时，直接 Seek 到对应位置读取，无需反序列化其他 99 个工具。这对于 **Token 经济性** 和 **响应延迟** 至关重要。

#### 2. Durable-Pointer：指向头，但保留尾部线索 (Q4, Q8)

关于指针指向，我认为必须遵循 **"Principle of Least Surprise"**。

**[A] 指针指向 Start**
*   **理由**: 指针即地址。`ptr` 应该指向对象的起始字节。这符合所有编程语言的直觉，也方便调试（Hex Editor 查看时，跳转到地址就能看到 Magic/Tag）。
*   **解决写入长度问题**: 使用 `ChunkedReservableWriter` 预留头部 Length 空间，写完后回填。这是成熟做法。

**[A] 双向遍历支持 (Crash Recovery)**
为了支持从尾部向前扫描（找最后一个有效 Root），每个 Record 应该有一个 **Footer**。

```
[Length: 4B] [Tag: 1B] [ ... Body ... ] [Length: 4B]
^                                       ^
|                                       |
Pointer 指向这里                        Scanner 读取这里回跳
```

这样既满足了随机访问（读头），也满足了日志回溯（读尾）。

#### 3. 文件与 Meta：Superblock 优于尾部 Meta (Q6)

**[!] 反对尾部 Meta**
如果文件是 append-only 且不断增长的，"尾部"是一个移动的目标。每次写入都要更新尾部，意味着频繁的磁盘 Seek 和潜在的原子性问题。

**[A] 头部 Superblock (Ping-Pong)**
建议在文件头部（Offset 0）预留两个 4KB 的 Block。

```
[Block A (Seq: 1)] [Block B (Seq: 0)] [ ... Data Area ... ]
```

*   **写入逻辑**: 轮流写入 A 和 B，Seq 递增。
*   **读取逻辑**: 读 A 和 B，校验 CRC，取 Seq 大的那个作为当前 Valid Root。
*   **优势**: 位置固定，OS 文件系统缓存友好，且天然支持原子更新（一次扇区写入）。

#### 4. 预分配策略：稀疏文件是正解 (Q5)

**[+1] 方案 C (稀疏文件)**
*   **理由**: 现代文件系统（NTFS, ext4, APFS）都良好支持 Sparse File。
*   **操作**: `FileStream.SetLength(1GB)`。
*   **效果**: 逻辑大小 1GB，物理占用 0KB。写入 Offset 100MB 处时，OS 自动分配该 Page。
*   **MVP**: 如果担心跨平台兼容性，可以 fallback 到 **方案 B (Chunked Growth)**，但 Chunk 要大（如 64MB），避免频繁 Resize 导致的磁盘碎片。

#### 5. 核心隐喻：Brain-on-Disk

最后，我想强调 DurableHeap 对 DocUI 的意义：

*   **Durable DOM**: 我们不仅存数据，还要存 UI 状态（滚动条位置、折叠状态）。这些也是 `JObject`。
*   **Forking Agent**: 利用 Copy-On-Write (COW) 特性，我们可以低成本创建 Agent 的"平行宇宙"（Fork）。
    *   父 Agent 指针: `0x1000`
    *   子 Agent 1 指针: `0x1000` (共享历史) + 新的 Action `0x2000`
    *   子 Agent 2 指针: `0x1000` (共享历史) + 新的 Action `0x3000`
    *   这让 **反事实推理 (Counter-factual Reasoning)** 成为可能。

---

### DocUIClaude 的想法

你们好！作为概念框架顾问，我先整理一下当前的设计空间，然后逐项给出分析。

#### 0. 概念内核确认

在深入技术细节之前，先确认一个核心概念：

> **DurableHeap 的本质**：磁盘是本尊，内存是投影。
> 
> 传统序列化：`Memory Object → Disk Bytes → Memory Object`（对象来回"翻译"）
> DurableHeap：`Disk Object ← mmap → Memory View`（对象就住在磁盘上，内存只是窗口）

这个概念决定了我们的很多设计选择。如果理解偏差，后续讨论会走歪。

**[Q] 确认一下：MVP 阶段是否坚持这个概念内核？还是先做"增量序列化"（传统翻译模型的优化版）？**

我的理解是坚持本尊概念——这决定了 Durable-Pointer 必须是**文件偏移量**，而不是某种抽象 ID。

---

#### 1. 二进制布局：[+1] 混合风格 + JObject 布局细化

**[+1] 混合风格**，理由：
- 值类型（int/string）出现频繁但个体小，CBOR 风格的 varint 节省空间
- 引用类型（JObject/JArray）需要 O(1) 随机访问，固定布局更优

**[A] 具体布局提案**（整合 Team Leader 和 Gemini 的思路）：

```
=== 值类型 (Inline) ===

Int:
  [Tag:1B = 0x01][Value:varint]
  
String:
  [Tag:1B = 0x02][Length:varint][UTF-8 Bytes...]

=== 引用类型 (Flat) ===

JObject:
  [Tag:1B = 0x10]
  [TotalLength:4B]          ← 便于跳过整个对象
  [FieldCount:2B]
  [OffsetTable: 4B * FieldCount]  ← 相对本对象起始的偏移
  [Field0: KeyPtr:4B + ValuePtr:4B]
  [Field1: KeyPtr:4B + ValuePtr:4B]
  ...

JArray:
  [Tag:1B = 0x11]
  [TotalLength:4B]
  [ElementCount:4B]
  [OffsetTable: 4B * ElementCount]  ← 相对本对象起始的偏移
  [Element0: Ptr:4B]
  [Element1: Ptr:4B]
  ...
```

**[!] 发现问题：Key 应该内联还是引用？**

JObject 的 Key 通常是短字符串（如 `"name"`, `"type"`）。有两种选择：

| 方案 | 优点 | 缺点 |
|------|------|------|
| Key 用引用（Ptr） | 布局统一，Key 可去重 | 读取时多一次 Seek |
| Key 内联 | 读取快，无需 Seek | 占用略大，无法去重 |

**[A] 我倾向 Key 用引用 + 字符串池去重**。理由：
1. 相同的 Key 会反复出现（如每个 HistoryEntry 都有 `"type"`, `"timestamp"`）
2. 去重后，整个 Agent 生命周期内 `"type"` 只存一份
3. 随着 History 增长，去重收益越来越大

**[Q] 是否 MVP 就要做字符串池？还是先简单处理，允许 Key 重复存储？**

---

#### 2. Durable-Pointer 位置：[+1] 指向头 + Footer 双向支持

**[+1] Gemini 的方案**：指针指向头，但加 Footer 支持从尾部回扫。

```
[Length:4B][Tag:1B][...Body...][Length:4B]
^                               ^
Ptr 指向这里                    崩溃恢复时从这里回跳
```

**补充理由**：
- **调试友好**：Hex Editor 跳转到 Ptr 地址，第一个字节就是 Tag，符合直觉
- **mmap 友好**：读取对象时，知道起始地址就能 mmap 该区域
- **崩溃恢复**：从文件末尾向前扫描，读 Footer 的 Length，跳到上一条记录

**[!] 但有一个问题：Head Length 和 Tail Length 重复了**

如果 Tag 紧跟 Length，那 TotalLength 字段其实存了两份。

**[A] 简化方案：Header 只存 Tag，Length 只放 Footer**

```
[Tag:1B][...Body...][Length:4B]
^                    ^
Ptr                  用于回扫 + 计算 Body 范围
```

读取流程：
1. 从 Ptr 读 Tag（1B），知道类型
2. 根据类型决定如何解析 Body（值类型直接读 varint，引用类型读 TotalLength）
3. 回扫时：从文件末尾读 Length（4B），向前跳 Length 字节到上一条记录

**[Q] 这样够用吗？还是 Head 和 Tail 都要 Length？**

---

#### 3. 文件布局与 Meta：[+1] 头部 Superblock

**[+1] Gemini 的 Ping-Pong Superblock 方案**。

理由：
- 位置固定（Offset 0），不需要追踪"尾部在哪里"
- 原子更新：写入 4KB block 是文件系统的原子单位
- 缓存友好：Superblock 常驻 page cache

**[A] Superblock 具体结构**：

```
Offset 0:
  [Magic: 8B = "DHEAP01\0"]  ← 文件格式签名
  [BlockA: 4088B]
  [BlockB: 4088B]
  
Block 结构:
  [Seq: 8B]                   ← 单调递增序列号
  [RootPtr: 8B]               ← 当前 Root Object 的文件偏移
  [DataEnd: 8B]               ← 数据区结束位置（下次写入从这里开始）
  [Checksum: 4B]              ← CRC32
  [Padding...]                ← 对齐到 4088B
  
Data Area starts at Offset 8KB:
  [Record 0][Record 1][Record 2]...
```

**写入流程**：
1. 在 DataEnd 处 append 新对象
2. 更新 DataEnd
3. 写入 RootPtr（指向新的 Root）
4. 计算 Checksum
5. 写入 Seq 较小的那个 Block（Ping-Pong 切换）
6. fsync

**读取流程**：
1. 读 BlockA 和 BlockB
2. 校验 Checksum
3. 取 Seq 较大且 Checksum 正确的 Block
4. 从 RootPtr 开始遍历

---

#### 4. 预分配策略：[+1] 方案 C（稀疏文件）

**[+1] 稀疏文件**，但需要明确 fallback。

**[A] MVP 策略**：

```csharp
public class DurableHeapFile {
    const long VIRTUAL_SIZE = 1L * 1024 * 1024 * 1024;  // 1GB
    
    public static FileStream Create(string path) {
        var fs = new FileStream(path, FileMode.CreateNew, FileAccess.ReadWrite);
        fs.SetLength(VIRTUAL_SIZE);  // 稀疏文件：逻辑 1GB，物理 0KB
        
        // 写入 Superblock（Magic + 初始化 BlockA）
        WriteSuperblock(fs, rootPtr: 0, dataEnd: 8192);
        return fs;
    }
}
```

**验证点**：
- Windows (NTFS)：✅ 默认支持稀疏文件
- Linux (ext4)：✅ 默认支持
- macOS (APFS)：✅ 支持

**[!] 注意：Git 会把稀疏文件展开成实际大小**

如果用户不小心把 DurableHeap 文件放进 Git 仓库，会很惨。

**[A] 建议在 `.gitignore` 模板中默认加 `*.dheap`。**

---

#### 5. Lazy Wrapper API 设计 (Q7)

这是我最想讨论的部分，因为它决定了开发者体验。

**[A] 核心类型提案**：

```csharp
// 惰性包装器 —— 知道"哪里有数据"，但还没读
public readonly struct DurableRef<T> where T : IDurableObject {
    internal readonly long _offset;
    
    public T Resolve(DurableHeap heap) => heap.Read<T>(_offset);
    public bool IsNull => _offset == 0;
}

// 可持久化对象的标记接口
public interface IDurableObject {
    static abstract T Read(DurableHeap heap, long offset);
    static abstract void Write(DurableHeap heap, ref T value);
}

// 具体类型示例
public sealed class DurableString : IDurableObject { ... }
public sealed class DurableArray<T> : IDurableObject { ... }
public sealed class DurableDict<TKey, TValue> : IDurableObject { ... }
```

**使用示例**：

```csharp
// 写入
using var heap = DurableHeap.Open("agent.dheap");
var history = new DurableArray<HistoryEntry>();
history.Add(heap, new HistoryEntry { ... });
heap.SetRoot(history);
heap.Commit();  // 更新 Superblock

// 读取（惰性）
using var heap = DurableHeap.Open("agent.dheap");
var history = heap.GetRoot<DurableArray<HistoryEntry>>();
var entry0 = history[0];  // 这一步才真正读取磁盘
```

**[Q] MVP 需要支持哪些类型？**

监护人说只支持 `int`, `string`, `JObject`, `JArray`。那：
- `DurableInt` = `int`（值类型，直接嵌入）
- `DurableString` = 引用类型
- `DurableDict` = JObject 语义
- `DurableArray` = JArray 语义

**[!] 但 JObject 的 Key 必须是 string，这隐含了 string 是值类型的假设**

如果 Key 用引用（字符串池），那 JObject 的 Key 其实是 `DurableRef<DurableString>`，不是 `string`。

**[A] 建议：MVP 中 Key 直接内联存储，不做字符串池。简化实现，代价是空间。**

---

#### 6. 崩溃恢复 (Q8)

**[A] 恢复流程**：

```
1. 读取 BlockA 和 BlockB
2. 校验两个 Block 的 Checksum
3. 选择 Seq 较大且 Checksum 有效的 Block
4. 如果两个都损坏 → 文件不可恢复（MVP 可接受）
5. 从 RootPtr 开始，验证对象链完整性（可选）
```

**MVP 简化**：不做对象链验证。如果 Superblock 有效，就认为数据有效。

**[!] 风险**：如果 Superblock 写入成功但数据写入不完整，会读到垃圾。

**[A] 缓解措施**：写入顺序必须是 Data → Superblock。只有 Data 完整后才更新 Superblock。

---

#### 7. MVP 边界确认

让我整理一下 MVP 应该包含/排除什么：

| 特性 | MVP 包含? | 理由 |
|------|-----------|------|
| 4 种基础类型 | ✅ | 核心功能 |
| 增量序列化 | ✅ | 核心功能 |
| mmap 读取 | ✅ | 性能基础 |
| Lazy Wrapper | ✅ | API 人体工程学 |
| Superblock Ping-Pong | ✅ | 崩溃安全 |
| 稀疏文件预分配 | ✅ | 实现简单 |
| 字符串池/去重 | ❌ | 复杂度高，收益需验证 |
| 对象销毁/GC | ❌ | 监护人明确排除 |
| 多文件分片 | ❌ | 监护人明确排除 |
| 对象链完整性校验 | ❌ | MVP 可接受风险 |

**[Q] 这个边界大家认同吗？**

---

#### 8. 命名建议

最后一点小建议：

| 当前术语 | 建议 | 理由 |
|----------|------|------|
| Durable-Pointer | `DurableRef` | 对齐 C# 引用语义，避免与 C 指针混淆 |
| Record | `DurableBlock` 或 `Slot` | "Record" 太通用，易与数据库术语混淆 |
| 双 Meta | `Superblock` | 借用文件系统术语，自解释 |

---

**总结**：我认为当前讨论的方向是正确的。核心决策点：
1. ✅ 混合布局（值类型 CBOR + 引用类型固定）
2. ✅ 指针指向头 + Footer 支持回扫
3. ✅ 头部 Superblock Ping-Pong
4. ✅ 稀疏文件预分配

待决：
- Key 内联 vs 引用（我建议 MVP 内联）
- Header Length 是否必要（我建议省略，只留 Footer Length）

期待大家的回应！

---

### Investigator 的想法

你们好！我已经仔细研读了前面的讨论，以及 `ChunkedReservableWriter.cs` 的预留回填机制。作为实现专家，我来从**字节级布局**和**代码实现**角度给出具体方案。

---

#### 1. 关于概念内核的确认

**[+1] Claude 的"磁盘是本尊，内存是投影"概念**

这是 DurableHeap 区别于传统序列化的核心。从实现角度，这意味着：

```
传统序列化：Object → byte[] → Object  （两次拷贝）
DurableHeap：FileOffset → mmap → Span<byte> → 按需解析（零拷贝读取）
```

**[A] MVP 应坚持这个概念内核**，但有一个实现细节需要澄清：

- **写入时**：仍需要序列化（Object → bytes），但只序列化一次，直接写入文件
- **读取时**：通过 `MemoryMappedFile` 直接映射，`DurableRef<T>.Resolve()` 按需解析

这个模型下，`DurableRef<T>` 的 `_offset` 就是**绝对文件偏移量**，类型安全由泛型参数保证。

---

#### 2. 二进制格式：字节级规范

**[+1] 混合风格**（值类型 CBOR 风格 + 引用类型固定布局）

**[A] MVP 完整布局规范**：

```
=== Type Tags (1 byte) ===
0x00 = Null
0x01 = Int (varint encoded)
0x02 = String (UTF-8, length-prefixed)
0x10 = JObject (fixed layout)
0x11 = JArray (fixed layout)

=== Int ===
┌─────────┬────────────────────┐
│ Tag=0x01│ Value (varint 1-5B)│
└─────────┴────────────────────┘
  1B        1-5B

说明：varint 编码（LEB128 风格），小整数 1 字节，最大 5 字节覆盖 int32

=== String ===
┌─────────┬────────────────────┬─────────────────┐
│ Tag=0x02│ Length (varint)    │ UTF-8 Bytes     │
└─────────┴────────────────────┴─────────────────┘
  1B        1-5B                  Length bytes

=== JObject ===
┌─────────┬────────────┬────────────┬─────────────────────┬──────────────────────────┐
│ Tag=0x10│ TotalLen 4B│ FieldCnt 2B│ OffsetTable 4B×N    │ KV Pairs (Key+Value Ptr) │
└─────────┴────────────┴────────────┴─────────────────────┴──────────────────────────┘
  1B        4B           2B           N×4B                   N×8B

- TotalLen: 从 Tag 开始到末尾的总字节数（便于跳过）
- OffsetTable[i]: 第 i 个 KV Pair 相对于 Tag 的偏移（4B，支持 4GB 内对象）
- KV Pair: [KeyPtr:4B][ValuePtr:4B]（绝对文件偏移）

=== JArray ===
┌─────────┬────────────┬────────────┬─────────────────────┬──────────────────────────┐
│ Tag=0x11│ TotalLen 4B│ ElemCnt 4B │ OffsetTable 4B×N    │ Element Ptrs 4B×N        │
└─────────┴────────────┴────────────┴─────────────────────┴──────────────────────────┘
  1B        4B           4B           N×4B                   N×4B

- 使用 4B ElemCnt（而非 2B）因为数组可能很大（如 Agent 历史）
```

**[!] 关于 FieldCount 用 2B 的问题**

JObject 字段数 2B (max 65535) 对 Agent 足够吗？考虑到单个 JObject 代表一个 HistoryEntry 或 ToolDefinition，不太可能超过 65535 个字段。如果担心，可以升到 4B。

**[A] MVP 建议保持 2B**，因为：
1. 符合实际使用场景
2. 省 2 字节 × 每个对象，数据更紧凑
3. 如果未来需要，可以用新 Tag (0x12) 扩展

---

#### 3. Pointer 指向头 + 末尾 Length：[+1] 并补充

**[+1] Claude 的简化方案**（Header 只存 Tag，Length 只放 Footer）

但我有一个**实现层面的修正**：

**[!] 问题：值类型 (Int/String) 没有 Footer**

Int 和 String 是变长的，如果没有 Footer，从尾部回扫时无法知道它们的边界。

**[A] 分层处理**：

1. **顶层 Record 必须有 Footer**（用于崩溃恢复回扫）
2. **嵌套值类型不需要 Footer**（已被父对象的 TotalLen 覆盖）

换句话说：

```
顶层写入：
┌─────────┬───────────────────┬──────────┐
│ Content │ TotalLen (4B)     │ CRC (4B) │
└─────────┴───────────────────┴──────────┘
           ^-- 回扫读这里

嵌套写入（在 JObject/JArray 内部）：
┌─────────┬─────────────────────┐
│ Tag     │ Value (inline)      │
└─────────┴─────────────────────┘
没有 Footer，靠父对象定位
```

**为什么需要 CRC？**

- 监护人布局提案里提到了 CRC
- 每个顶层 Record 末尾 4B CRC32 可以验证写入完整性
- 崩溃恢复时：从尾部读 [Length][CRC]，验证 CRC 后才信任 Length

---

#### 4. Superblock 格式：细化 + mmap 友好

**[+1] Gemini 的 Ping-Pong Superblock**

**[A] 具体结构（对齐 4KB 页）**：

```csharp
// Superblock 物理布局（文件开头 8KB）
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct SuperblockSlot {
    public fixed byte Magic[8];      // "DHEAP01\0"
    public ulong Seq;                // 单调递增序列号
    public long RootPtr;             // Root Object 的文件偏移（0 表示无）
    public long DataEnd;             // 数据区结束位置
    public long ObjectCount;         // 已写入对象数（调试用）
    public uint Checksum;            // CRC32(Magic..ObjectCount)
    public fixed byte Padding[4096 - 8 - 8 - 8 - 8 - 8 - 4]; // 对齐到 4KB
}

// 文件布局
// [0..4095]    = SuperblockSlot A
// [4096..8191] = SuperblockSlot B
// [8192..]     = Data Area (Records)
```

**为什么 RootPtr 用 `long` 而不是 `uint`？**

- 稀疏文件逻辑大小 1GB+ 时，4B 不够
- `.NET long` = 8B，支持 EB 级偏移（实际用不到，但 API 一致性更好）

**[A] mmap 读取示例**：

```csharp
public class DurableHeap : IDisposable {
    private readonly MemoryMappedFile _mmf;
    private readonly MemoryMappedViewAccessor _view;
    private SuperblockSlot _currentSuper;
    
    public static DurableHeap Open(string path) {
        var mmf = MemoryMappedFile.CreateFromFile(path, FileMode.Open);
        var view = mmf.CreateViewAccessor();
        
        // 读取两个 Superblock，选 Seq 大且 CRC 有效的
        view.Read(0, out SuperblockSlot slotA);
        view.Read(4096, out SuperblockSlot slotB);
        
        bool aValid = ValidateCrc(slotA);
        bool bValid = ValidateCrc(slotB);
        
        SuperblockSlot current;
        if (aValid && bValid) { current = slotA.Seq > slotB.Seq ? slotA : slotB; }
        else if (aValid) { current = slotA; }
        else if (bValid) { current = slotB; }
        else { throw new CorruptedFileException("Both superblocks invalid"); }
        
        return new DurableHeap(mmf, view, current);
    }
}
```

---

#### 5. 写入流程：借鉴 ChunkedReservableWriter

阅读了 `ChunkedReservableWriter.cs`，它的核心设计是：

1. **预留空间**：`ReserveSpan(count, out token)` 返回 Span，记录 offset
2. **后续写入**：继续调用 `GetSpan()` + `Advance()` 写其他数据
3. **回填**：在 Span 中写入预留的数据
4. **提交**：`Commit(token)` 标记预留区已填充
5. **Flush**：只 flush 连续已提交的数据前缀

**[A] DurableHeap 写入流程（适配版）**：

```csharp
public class DurableHeapWriter : IDisposable {
    private readonly FileStream _fs;
    private readonly ChunkedReservableWriter _buffer;
    private long _dataEnd;  // 当前写入位置
    
    // 写入 JObject
    public long WriteObject(DurableDict dict) {
        long startOffset = _dataEnd;
        
        // 1. 写 Tag
        WriteByte(0x10);
        
        // 2. 预留 TotalLen（稍后回填）
        var totalLenSpan = _buffer.ReserveSpan(4, out int totalLenToken);
        
        // 3. 写 FieldCount
        WriteUInt16((ushort)dict.Count);
        
        // 4. 预留 OffsetTable
        var offsetTableSpan = _buffer.ReserveSpan(4 * dict.Count, out int offsetToken);
        
        // 5. 写 KV Pairs，并记录各自偏移
        int[] offsets = new int[dict.Count];
        int i = 0;
        foreach (var kv in dict) {
            offsets[i] = (int)(_dataEnd - startOffset);
            WritePtr(kv.Key);    // KeyPtr
            WritePtr(kv.Value);  // ValuePtr
            i++;
        }
        
        // 6. 回填 OffsetTable
        for (int j = 0; j < offsets.Length; j++) {
            BinaryPrimitives.WriteInt32LittleEndian(
                offsetTableSpan.Slice(j * 4, 4), offsets[j]);
        }
        _buffer.Commit(offsetToken);
        
        // 7. 回填 TotalLen
        int totalLen = (int)(_dataEnd - startOffset);
        BinaryPrimitives.WriteInt32LittleEndian(totalLenSpan, totalLen);
        _buffer.Commit(totalLenToken);
        
        // 8. 写 Footer (TotalLen + CRC) —— 仅顶层对象
        if (isTopLevel) {
            WriteInt32(totalLen);
            WriteUInt32(ComputeCrc(...));
        }
        
        return startOffset;  // 返回 DurableRef
    }
}
```

**[!] 注意：ChunkedReservableWriter 的"提交顺序"**

`ChunkedReservableWriter` 要求**按顺序提交**才能 Flush：

> "只有连续已提交的数据前缀才会 Flush"

这意味着如果我们嵌套预留：
```
Reserve TotalLen → Reserve OffsetTable → Write KVs → Commit OffsetTable → Commit TotalLen
```

OffsetTable 必须先 Commit，TotalLen 后 Commit，才能保证 Flush 顺序正确。

**[A] 实现建议**：使用栈结构管理嵌套预留的 token，LIFO 顺序 Commit。

---

#### 6. Lazy Wrapper API：[+1] Claude 方案 + 细化

**[+1] `DurableRef<T>` 设计**

**[A] 完整 API 草案**：

```csharp
/// <summary>
/// 惰性引用：知道"哪里有数据"，但还没读。
/// </summary>
/// <remarks>
/// 本尊在磁盘，这只是一个"寻址指针"。
/// </remarks>
public readonly struct DurableRef<T> where T : IDurable {
    public readonly long Offset;
    
    public DurableRef(long offset) => Offset = offset;
    
    public bool IsNull => Offset == 0;
    
    public T Resolve(DurableHeap heap) {
        if (IsNull) throw new NullReferenceException("DurableRef is null");
        return T.Read(heap, Offset);
    }
    
    // 隐式转换：方便从 Write 返回值创建 Ref
    public static implicit operator DurableRef<T>(long offset) => new(offset);
}

/// <summary>
/// 可持久化类型的静态接口（.NET 7+ static abstract）
/// </summary>
public interface IDurable {
    static abstract T Read<T>(DurableHeap heap, long offset) where T : IDurable;
}

/// <summary>
/// 可持久化字符串
/// </summary>
public sealed class DurableString : IDurable {
    private readonly DurableHeap _heap;
    private readonly long _offset;
    private string? _cached;
    
    internal DurableString(DurableHeap heap, long offset) {
        _heap = heap;
        _offset = offset;
    }
    
    public string Value => _cached ??= ReadFromHeap();
    
    private string ReadFromHeap() {
        var span = _heap.GetSpan(_offset);
        // Skip Tag (0x02)
        int tagLen = 1;
        int strLen = ReadVarint(span.Slice(tagLen), out int varintLen);
        return Encoding.UTF8.GetString(span.Slice(tagLen + varintLen, strLen));
    }
    
    public static implicit operator string(DurableString ds) => ds.Value;
    
    static T IDurable.Read<T>(DurableHeap heap, long offset) {
        return (T)(object)new DurableString(heap, offset);
    }
}

/// <summary>
/// 可持久化字典（对应 JObject）
/// </summary>
public sealed class DurableDict : IDurable, IEnumerable<KeyValuePair<DurableRef<DurableString>, DurableRef>> {
    private readonly DurableHeap _heap;
    private readonly long _offset;
    private int? _cachedCount;
    
    public int Count => _cachedCount ??= ReadCount();
    
    public DurableRef this[int index] => GetValueAt(index);
    
    public DurableRef this[string key] => FindByKey(key);
    
    // Lazy enumeration - 只在枚举时读取
    public IEnumerator<KeyValuePair<DurableRef<DurableString>, DurableRef>> GetEnumerator() {
        for (int i = 0; i < Count; i++) {
            yield return GetPairAt(i);
        }
    }
}

/// <summary>
/// 可持久化数组（对应 JArray）
/// </summary>
public sealed class DurableArray<T> : IDurable, IReadOnlyList<DurableRef<T>> where T : IDurable {
    private readonly DurableHeap _heap;
    private readonly long _offset;
    
    public int Count { get; }
    public DurableRef<T> this[int index] => GetElementAt(index);
}
```

**[Q] 关于 `DurableRef` 的非泛型版本**

JObject 的 Value 可能是任意类型（int/string/object/array），怎么处理？

**[A] 引入"类型擦除"的 `DurableRef`（非泛型）**：

```csharp
public readonly struct DurableRef {
    public readonly long Offset;
    
    public DurableRef<T> As<T>() where T : IDurable => new(Offset);
    
    public DurableType GetType(DurableHeap heap) {
        byte tag = heap.ReadByte(Offset);
        return tag switch {
            0x00 => DurableType.Null,
            0x01 => DurableType.Int,
            0x02 => DurableType.String,
            0x10 => DurableType.Object,
            0x11 => DurableType.Array,
            _ => throw new InvalidDataException($"Unknown tag: {tag}")
        };
    }
}
```

---

#### 7. 关于 Key 内联 vs 引用

**[+1] Claude 的 MVP 建议：Key 直接内联**

理由补充：
1. **实现复杂度**：字符串池需要 Hash 去重 + 额外的池管理逻辑
2. **MVP 优先**：先跑通流程，字符串池是优化，可后加
3. **度量先行**：等有实际数据后，再评估去重收益

**[A] 但布局上为字符串池留好扩展点**：

```
当前 MVP：KeyPtr 指向实际 String 对象
未来优化：KeyPtr 可以指向字符串池表项

// 字符串池（未来）
[Tag=0xF0][EntryCount:4B][Entry0][Entry1]...
Entry: [Hash:4B][StringPtr:4B]
```

只要 Key 仍是 Ptr（而非内联 bytes），未来加字符串池不需要改布局。

---

#### 8. MVP 边界：[+1] 并补充测试策略

**[+1] Claude 的边界定义**

补充一个**测试策略**：

| 测试类别 | 测试点 | 优先级 |
|----------|--------|--------|
| 单元测试 | Varint 编码/解码 | P0 |
| 单元测试 | 各类型序列化/反序列化 | P0 |
| 单元测试 | Superblock Ping-Pong 切换 | P0 |
| 集成测试 | 写入 → 关闭 → 重新打开 → 读取 | P0 |
| 崩溃测试 | 写入中途 Kill → 恢复 | P1 |
| 压力测试 | 1M 对象写入/读取 | P1 |
| 边界测试 | 大字符串 (1MB+)、大数组 (100K+ 元素) | P2 |

**[A] 崩溃测试的实现思路**：

```csharp
[Fact]
public void CrashRecovery_PartialWrite_ShouldRollbackToLastValidRoot() {
    // Arrange: 写入 N 个对象，在第 N+1 个中途模拟崩溃
    using var heap = DurableHeap.Create(path);
    for (int i = 0; i < 10; i++) {
        heap.WriteObject(...);
        heap.Commit();
    }
    
    // 开始写第 11 个但不 Commit
    heap.WriteObject(...);
    // 模拟崩溃：直接 Dispose，不调 Commit
    heap.Dispose();
    
    // Act: 重新打开
    using var recovered = DurableHeap.Open(path);
    
    // Assert: 只能看到 10 个对象
    Assert.Equal(10, recovered.ObjectCount);
}
```

---

#### 9. 实现路线图建议

**[A] MVP 分阶段实现**：

```
Phase 1: 基础设施（1-2 天）
├── DurableHeap.cs           // 文件管理、mmap
├── Superblock.cs            // Ping-Pong 读写
├── Varint.cs                // LEB128 编解码
└── Tests/SuperblockTests.cs

Phase 2: 类型系统（2-3 天）
├── DurableRef.cs            // 惰性引用
├── DurableString.cs
├── DurableDict.cs
├── DurableArray.cs
└── Tests/TypeTests.cs

Phase 3: 写入引擎（2-3 天）
├── DurableHeapWriter.cs     // 集成 ChunkedReservableWriter
├── ObjectWriter.cs          // 各类型序列化
└── Tests/WriterTests.cs

Phase 4: 集成与文档（1 天）
├── README.md
├── Examples/
└── Tests/IntegrationTests.cs
```

---

#### 10. 遗留问题汇总

| # | 问题 | 我的建议 | 待确认 |
|---|------|----------|--------|
| 1 | FieldCount 2B 够用？ | 够用，2B = 65535 | 可以确认 |
| 2 | 是否需要 Version 字段？ | MVP 不需要，Magic 含版本 "DHEAP01" | 可以确认 |
| 3 | CRC 算法选择？ | CRC32-C（有硬件加速） | 待讨论 |
| 4 | 大对象 (>4GB) 支持？ | MVP 不支持，TotalLen 4B 限制 | 可以接受 |

---

**总结**：

从实现角度，我认为当前方案是**可编码**的。核心收敛点：

1. ✅ **布局**：Tag + TotalLen + 类型特定数据 + Footer(顶层)
2. ✅ **Pointer**：指向头（Tag 字节），8B 绝对偏移
3. ✅ **Superblock**：4KB×2 Ping-Pong，CRC32 校验
4. ✅ **写入**：借鉴 ChunkedReservableWriter 的预留回填模式
5. ✅ **API**：`DurableRef<T>` + `IDurable` static abstract interface

**下一步**：如果大家认同，我可以产出一份正式的 **Implementation Brief**，包含完整的字节布局图和代码骨架。

---

## 主席总结：设计共识

经过四位参与者的深入讨论，**所有关键问题（Q1-Q10）均已达成共识**。

### 最终设计方案

#### 1. 文件布局

```
┌─────────────────────────────────────────────────────────────┐
│ Offset 0:        Magic "DHEAP01\0" (8B)                     │
├─────────────────────────────────────────────────────────────┤
│ Offset 8:        Superblock A (4088B)                       │
│                  ├── Seq (8B)                               │
│                  ├── RootPtr (8B)                           │
│                  ├── DataEnd (8B)                           │
│                  ├── ObjectCount (8B)                       │
│                  ├── Checksum (4B, CRC32-C)                 │
│                  └── Padding...                             │
├─────────────────────────────────────────────────────────────┤
│ Offset 4096:     Superblock B (4096B)                       │
│                  └── (同上)                                 │
├─────────────────────────────────────────────────────────────┤
│ Offset 8192:     Data Area                                  │
│                  ├── Record 0                               │
│                  ├── Record 1                               │
│                  └── ...                                    │
└─────────────────────────────────────────────────────────────┘
```

#### 2. 类型二进制格式

```
Type Tags:
  0x00 = Null
  0x01 = Int
  0x02 = String
  0x10 = JObject
  0x11 = JArray

Int:     [0x01][Varint Value]
String:  [0x02][Varint Len][UTF-8 Bytes...]
JObject: [0x10][TotalLen:4B][FieldCnt:2B][OffsetTable:4B×N][KV:8B×N]
JArray:  [0x11][TotalLen:4B][ElemCnt:4B][OffsetTable:4B×N][Ptr:4B×N]

顶层 Record Footer: [TotalLen:4B][CRC32:4B]
```

#### 3. DurableRef 设计

```
DurableRef = 8B 绝对文件偏移，指向 Tag 字节
DurableRef<T> = 类型安全版本
DurableRef (非泛型) = 类型擦除版本，支持 GetType() + As<T>()
```

#### 4. 核心 API

```csharp
// 打开/创建
DurableHeap.Create(path)  // 创建稀疏文件
DurableHeap.Open(path)    // mmap 打开

// 写入
heap.WriteObject(dict)    // 返回 DurableRef
heap.WriteArray(arr)
heap.WriteString(str)
heap.SetRoot(ref)         // 设置根对象
heap.Commit()             // 更新 Superblock

// 读取
heap.GetRoot<T>()         // 获取根对象
ref.Resolve(heap)         // 惰性解析
```

### 实现路线图

| 阶段 | 内容 | 预估时间 |
|------|------|----------|
| Phase 1 | 基础设施：文件管理、Superblock、Varint | 1-2 天 |
| Phase 2 | 类型系统：DurableRef、String/Dict/Array | 2-3 天 |
| Phase 3 | 写入引擎：集成 ChunkedReservableWriter | 2-3 天 |
| Phase 4 | 集成测试与文档 | 1 天 |

### MVP 边界确认

| 包含 ✅ | 排除 ❌ |
|---------|---------|
| 4 种基础类型 | 字符串池/去重 |
| 增量序列化 | 对象销毁/GC |
| mmap 读取 | 多文件分片 |
| Lazy Wrapper | 大对象 (>4GB) |
| Superblock Ping-Pong | 对象链完整性校验 |
| 稀疏文件预分配 | |

### 术语统一

| 旧术语 | 新术语 |
|--------|--------|
| Persist-Pointer | DurableRef |
| Record | DurableBlock |
| 双 Meta | Superblock |

---

**畅谈结束条件检查**：

✅ 所有问题（Q1-Q10）已解决或绕过
✅ 设计文档达到可编码程度
✅ 参与者共识：Gemini [+1]、Claude [+1]、Investigator [+1]

**结论**：畅谈目标达成，可以开始编码。

---

**会议记录完成于 2025-12-16**

*产出：DurableHeap MVP 设计规范*



