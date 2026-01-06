# 畅谈会：RBF 文件交互接口设计

> **日期**：2025-12-29
> **标签**：#design
> **主持人**：刘德智 (StandardsChair)
> **参与者**：Advisor-Claude, Advisor-DeepSeek, Advisor-GPT
> **状态**：进行中

---

## 背景

**问题来源**：Implementer 在现有文档指导下实现 RBF 层时严重偏离设计，监护人发现根本原因是文档层级边界模糊——StateJournal 文档与 RBF 实现细节混在一起，导致注意力溢出。

**已完成的工作**：
- RBF 层文档已分离到 `atelia/docs/Rbf/`
- 旧实现已归档，`atelia/src` 和 `atelia/tests` 现在是干净的
- `rbf-interface.md` 定义了 `IRbfFramer` / `IRbfScanner` 接口

**当前缺失**：
- 文件打开/创建的接口契约（如何从路径获得 Framer/Scanner？）
- Flush / DurableFlush 的语义边界（属于文件级还是 Framer 级？）
- 文件生命周期管理（打开、关闭、异常处理）

## 核心问题

**设计目标**：为 RBF 层增补"文件交互接口"，使上层（StateJournal）能够：
1. 从文件路径打开/创建 RBF 文件
2. 获得 `IRbfFramer` 用于写入帧
3. 获得 `IRbfScanner` 用于读取帧
4. 控制持久化（Flush / DurableFlush）
5. 正确关闭文件

**设计约束**（监护人明确）：
- **排除传入 Backend 方案**：即使内部需要 Backend 抽象，对外也不暴露
- **核心用例**：Agent 重启后，手里只有文件路径，需要重建状态
- **本质功能**：从路径打开文件 → 以 Binary Frame 为单位读写

**待讨论的焦点**：
1. **接口形状**：单一 `IRbfFile` 还是分离的工厂方法？
2. **资源管理**：Framer/Scanner 的生命周期与文件句柄的关系？
3. **并发语义**：允许多个 Framer/Scanner 实例吗？
4. **错误处理**：文件损坏、权限错误的异常策略？

## 💬 畅谈记录

---

## 🎯 结论同步块（动态更新）

### 需求分析结论（基于 StateJournal 真实用例）

| 需求项 | 具体描述 | 接口要求 |
|:-------|:---------|:---------|
| **双文件模式** | Data（对象版本）+ Meta（Commit 日志） | 每个文件独立 `IRbfFile` |
| **打开时读取** | Meta 逆向扫描找 HEAD（含撕裂检测 `[R-META-AHEAD-BACKTRACK]`） | `ScanReverse()` |
| **运行时读取** | 通过 <deleted-place-holder> 随机读取（Lazy Load 触发）| `TryReadAt(<deleted-place-holder>)` |
| **Commit 流程** | 2 次 DurableFlush（Data → Meta 顺序） | `DurableFlush()` |
| **恢复流程** | 按 DataTail 截断 Data 文件 | `Truncate(newLength)` + `LengthBytes` |
| **生命周期** | 长期持有文件句柄（进程级） | `Dispose()` |

### 已形成共识

| 决策点 | 共识 | 支持者 |
|:-------|:-----|:-------|
| **入口点形状** | `IRbfFile` 单一入口点 | Claude, DeepSeek, GPT |
| **创建方式** | `CreateNew` vs `OpenExisting` 显式分离 | Claude, DeepSeek, 监护人 |
| **持久化控制** | `DurableFlush()` 在文件级 | Claude, DeepSeek, GPT |
| **Dispose 幂等性** | 必须幂等 | GPT |
| **D2: Create 对已有文件** | **FailIfExists** | **监护人裁决** |
| **D3: Scanner 实例数** | **单例**（至少 MVP） | **监护人裁决** |

### 关键纠正（GPT 核查）

| 原判断 | 纠正 |
|:-------|:-----|
| "跟随视图不需要" | 仅对 `ScanReverse` 成立；`TryReadAt` **必须**支持文件增长期间读取 |
| "Scanner 是一次性工具" | Scanner 是"长期随机读者"（Lazy Load 热路径） |
| "功能完备" | **缺失** `LengthBytes` 和 `Truncate(newLength)` |

### MVP 接口收敛

```csharp
public interface IRbfFile : IDisposable {
    string Path { get; }
    ulong LengthBytes { get; }              // ← 新增（DataTail 校验）
    
    IRbfFramer CreateFramer();              // 单例（第二次调用抛异常）
    IRbfScanner CreateScanner();            // 单例（MVP）
    
    void DurableFlush();
    void Truncate(ulong newLengthBytes);    // ← 新增（恢复截断）
}

public static class RbfFile {
    public static IRbfFile CreateNew(string path);       // FailIfExists
    public static IRbfFile OpenExisting(string path);    // 验证 Genesis
}
```

### 待规范化的关键契约（GPT 识别）

| # | 契约 | 可判定性要求 |
|:--|:-----|:-------------|
| 1 | `TryReadAt(<deleted-place-holder>)` 在文件增长期间的行为 | 以"调用时刻"文件内容为准；EOF 外返回 `false` |
| 2 | `ScanReverse()` 扫描窗口 | 当前：并发修改未定义；可考虑：固定为调用时 EOF |
| 3 | `DurableFlush()` 边界 | 只保证本文件；不影响其他文件实例 |
| 4 | `Truncate(newLength)` 后 <deleted-place-holder> 失效语义 | 被截断区间的地址后续 `TryReadAt` 必须返回 `false` |
| 5 | `CreateFramer()` 第二次调用 | 抛 `InvalidOperationException` |
| 6 | `LengthBytes` 语义 | OS 视角的当前文件长度（byte） |

### 规范工作量（更新）

- **新增接口成员**：`LengthBytes` + `Truncate(newLength)`
- **新增条款**：12-18 条（含 6 个关键契约）
- **修订条款**：1-3 条（`IRbfFramer.Flush()` remarks）
- **新增示例**：3-5 个（Open→扫描→随机读；Commit 流程；恢复截断）

---

## 第二轮架构共识

### 监护人提议（第二轮）

| 提议 | 描述 |
|:-----|:-----|
| **分层** | A层（格式组件，可测试）+ B层（句柄管理，极少单元测试） |
| **Façade** | 常规路径简化 + 灾难恢复独立 ToolKit |
| **FramePtr** | 升级 <deleted-place-holder>，含偏移量+长度，打包为 ulong |
| **底层 API** | RandomAccess vs Stream？缓存策略？可测试性？ |

### 参谋共识（第二轮）

| 决策点 | 共识 | 支持者 |
|:-------|:-----|:-------|
| **分层合理性** | ✅ 符合依赖倒置，测试性边界清晰 | Claude, DeepSeek |
| **Façade 模式** | ✅ 常规 vs 高级分离正确，需接口隔离防 God Object | Claude, DeepSeek |
| **FramePtr Bit 分配** | ✅ 44:20（64TB / 4MB），满足需求 | Claude, GPT |
| **底层 API** | ✅ RandomAccess（无状态，配合 FramePtr） | Claude, DeepSeek |
| **缓存策略** | ✅ 暂不实现（过早优化） | Claude, DeepSeek |
| **可测试性** | ✅ A层内存测试，B层集成测试 | DeepSeek |

### 关键裁决点（GPT 识别）

| # | 问题 | 影响 |
|:--|:-----|:-----|
| **R1** | <deleted-place-holder> → FramePtr 是完全替换还是并存？ | Breaking change 范围 |
| **R2** | 超过 4MB 的帧如何处理？ | 构造期拒绝 vs 运行时截断 |
| **R3** | 向后兼容策略：软兼容（透明转换）vs 硬迁移（显式版本）？ | 迁移路径 |

---

### 主持人开场 (刘德智)

各位参谋，这是一个相对纯粹的技术架构问题。我会为每位发言者提供以下背景文件：
- `atelia/docs/Rbf/rbf-interface.md` — 当前接口契约（缺文件交互部分）
- `atelia/docs/Rbf/rbf-format.md` — RBF 线格式规范
- `atelia/src/Data/IReservableBufferWriter.cs` — 核心抽象
- `atelia/src/Data/ChunkedReservableWriter.cs` — 实现参考（注意：这是唯一保留的旧代码，因为它是通用组件）

请先不要急于提出完整方案。第一轮发言，我希望听到：
- **你认为这个设计的关键张力在哪里？**（什么因素在相互制约？）
- **有哪些隐含的假设需要显式化？**（例如：文件独占？读写分离？）
- **有哪些类似系统的参考？**（不必是 C#，可以是任何领域的设计）

现在邀请 Advisor-Claude 先发言。

---

### Advisor-Claude 发言（基于需求重新设计）

> **基于**：StateJournal 真实需求调查（§ 需求分析结论、GPT 验证）
> **重点**：接口形状设计与实现衔接

#### 1. 接口形状设计

```csharp
/// <summary>
/// RBF 文件交互入口。持有文件句柄，提供 Framer/Scanner 视图与 durability 控制。
/// </summary>
public interface IRbfFile : IDisposable {
    /// <summary>文件路径（诊断用）</summary>
    string Path { get; }
    
    /// <summary>当前文件长度（OS 视角，用于 DataTail 校验）</summary>
    ulong LengthBytes { get; }
    
    /// <summary>创建写入器（单例：第二次调用抛 InvalidOperationException）</summary>
    IRbfFramer CreateFramer();
    
    /// <summary>创建扫描器（MVP：单例）</summary>
    IRbfScanner CreateScanner();
    
    /// <summary>强制数据落盘（fsync 等价）</summary>
    void DurableFlush();
    
    /// <summary>截断文件到指定长度（恢复流程用）</summary>
    void Truncate(ulong newLengthBytes);
}

/// <summary>工厂方法：显式区分创建与打开</summary>
public static class RbfFile {
    /// <summary>创建新文件（写入 Genesis Fence）。若文件已存在则失败。</summary>
    public static IRbfFile CreateNew(string path);
    
    /// <summary>打开已有文件（验证 Genesis Fence）。若不存在或损坏则失败。</summary>
    public static IRbfFile OpenExisting(string path);
}
```

**与现有接口的关系**：

```
┌─────────────────────────────────────────────────────────┐
│                    IRbfFile (新增)                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────┐  │
│  │ IRbfFramer  │  │ IRbfScanner │  │ DurableFlush()  │  │
│  │   (已有)     │  │   (已有)     │  │ Truncate()     │  │
│  └─────────────┘  └─────────────┘  │ LengthBytes     │  │
│                                     └─────────────────┘  │
└─────────────────────────────────────────────────────────┘
```

- `IRbfFramer` / `IRbfScanner`：**保持不变**，由 `IRbfFile` 创建并管理生命周期
- `IRbfFile`：**新增**，作为文件级资源持有者

#### 2. 关键设计决策

**D1：为什么选择单一入口点？**

| 需求 | 接口如何满足 |
|:-----|:-------------|
| 长期持有句柄 | `IRbfFile` 是进程级单例，`Dispose()` 仅在关闭时调用 |
| 两文件独立管理 | 上层各持有一个 `IRbfFile` 实例（Data + Meta） |
| Commit 顺序控制 | 上层分别调用 `dataFile.DurableFlush()` → `metaFile.DurableFlush()` |

**D2：如何支持 `TryReadAt` 的增长可见性？**

| 契约 | 语义 |
|:-----|:-----|
| `TryReadAt(addr)` | 以**调用时刻**的文件内容为准 |
| `addr` 超出当前 EOF | 返回 `false`（不抛异常） |
| 文件增长后再次调用 | 可能返回 `true`（新写入区域可见） |

**理由**：StateJournal 是"长生命周期持有句柄、持续 append"的系统。Commit 后对新地址的读取是热路径（Lazy Load）。

**D3：如何支持恢复流程？**

```csharp
// 恢复流程（StateJournal Open）
var meta = RbfFile.OpenExisting(metaPath);
var data = RbfFile.OpenExisting(dataPath);

// 1. 逆向扫描 Meta 找 HEAD
var metaScanner = meta.CreateScanner();
MetaCommitRecord? head = null;
foreach (var frame in metaScanner.ScanReverse()) {
    if (TryParseCommitRecord(frame, out var record) 
        && record.DataTail <= data.LengthBytes) {  // ← 使用 LengthBytes
        head = record;
        break;
    }
}

// 2. 按 DataTail 截断 Data 文件（清理垃圾）
if (head != null && head.DataTail < data.LengthBytes) {
    data.Truncate(head.DataTail);  // ← 使用 Truncate
}
```

**D4：为什么 Framer/Scanner 是单例？**

| 考虑 | 决策 |
|:-----|:-----|
| 单读者单写者 | StateJournal 明确同线程操作，不需要多实例 |
| 简化实现 | 避免多实例状态同步复杂性 |
| 可扩展性 | MVP 单例不阻碍未来放宽（但不承诺） |

#### 3. 与现有规范的衔接

**需要新增的章节**（建议 rbf-interface.md §5）：

```markdown
## 5. 文件交互接口

### 5.1 IRbfFile
[A-RBF-FILE-INTERFACE] — 接口定义
[S-RBF-FILE-SINGLE-FRAMER] — CreateFramer() 单例约束
[S-RBF-FILE-DISPOSE-IDEMPOTENT] — Dispose() 幂等
[S-RBF-FILE-DURABLEFLUSH-DISPOSED] — disposed 后 DurableFlush 抛异常
[S-RBF-FILE-TRYREADAT-EOF] — TryReadAt 的 EOF 边界语义
[S-RBF-FILE-TRUNCATE-INVALIDATE] — Truncate 后地址失效语义

### 5.2 RbfFile 工厂
[S-RBF-FILE-CREATE-FAIL-IF-EXISTS] — CreateNew 对已有文件失败
[S-RBF-FILE-OPEN-VERIFY-GENESIS] — OpenExisting 验证 Genesis
```

**需要修订的条款**：

| 现有条款 | 修订内容 |
|:---------|:---------|
| `IRbfFramer.Flush()` remarks | 将"由上层在其持有的底层句柄上执行 durable flush"改为"通过 `IRbfFile.DurableFlush()` 执行" |

**保持不变**：
- `[S-RBF-FRAMER-NO-FSYNC]` — Flush 不含 fsync（与新设计一致）
- `[S-RBF-SCANREVERSE-CONCURRENT-MUTATION]` — 并发修改未定义（ScanReverse 冷路径，无需更严格承诺）
- 所有 `IRbfFramer` / `IRbfScanner` / `RbfFrame` 的现有条款

#### 4. 实现提示（Informative）

**`TryReadAt` 增长可见性的实现**：

```csharp
// 关键：不缓存文件长度，每次读取时检查当前 EOF
public bool TryReadAt(<deleted-place-holder> address, out RbfFrame frame) {
    // 获取当前文件长度（OS 视角）
    var currentLength = _fileStream.Length;
    
    if (address.Value >= (ulong)currentLength) {
        frame = default;
        return false;  // 超出当前 EOF
    }
    
    // 正常读取...
}
```

**`Truncate` 后地址失效的实现**：

```csharp
public void Truncate(ulong newLengthBytes) {
    _fileStream.SetLength((long)newLengthBytes);
    // 无需额外记录——TryReadAt 自然基于新的 EOF 判断
}
```

**Framer/Scanner 单例的实现**：

```csharp
private IRbfFramer? _framer;

public IRbfFramer CreateFramer() {
    if (_framer != null)
        throw new InvalidOperationException("Framer already created");
    _framer = new RbfFramer(/* internal writer */);
    return _framer;
}
```

---

> 本设计基于 StateJournal 的真实需求，去除了通用化假设带来的复杂性。
> 接口形状"刚好够用"——MVP 优先简单直接。

---

### Advisor-DeepSeek 发言（基于真实用例重新设计）

> **基于**：StateJournal 启动恢复、Lazy Load、Commit 流程
> **重点**：真实场景的 DX/UX 评估与优化

#### 1. 真实场景的 API 体验评估

**场景 A：启动时恢复（Meta 逆向扫描 + Data 截断）**

```csharp
// StateJournal.OpenExisting() 内部流程
var metaFile = RbfFile.OpenExisting("state.meta.rbf");
var dataFile = RbfFile.OpenExisting("state.data.rbf");

// 1. 逆向扫描找 HEAD（含撕裂检测）
var metaScanner = metaFile.CreateScanner();
MetaCommitRecord? head = null;
foreach (var frame in metaScanner.ScanReverse()) {
    if (TryDeserialize(frame, out var record)) {
        // ✅ 体验好：LengthBytes 语义清晰（OS 视角文件长度）
        if (record.DataTail <= dataFile.LengthBytes) {
            head = record;
            break;
        }
    }
}

// 2. 恢复截断（清理垃圾）
if (head != null && head.DataTail < dataFile.LengthBytes) {
    // ✅ 体验好：Truncate 语义明确（缩短文件）
    dataFile.Truncate(head.DataTail);
}
```

**体验评估**：
- ✅ **直观性**：`ScanReverse()` 名称清晰传递"从尾到头"语义
- ✅ **正确性辅助**：`LengthBytes` 与 `DataTail` 对比是自然的数值比较
- ✅ **错误友好性**：`Truncate` 的前置条件（≤当前长度）容易理解
- ⚠️ **潜在困惑**：`ScanReverse()` 返回什么类型？能否存储到变量？（见 §2 反模式）

---

**场景 B：运行时 Lazy Load（<deleted-place-holder> 随机读取）**

```csharp
// StateJournal.LoadObject<T>() 内部流程
public T LoadObject<T>(ObjectId id) {
    // 1. 查找 VersionIndex
    var addr = _versionIndex.Lookup(id);
    
    // 2. 随机读取（注意：文件在持续增长）
    var dataScanner = _dataFile.CreateScanner();
    // ✅ 体验好：TryReadAt 是"试探性读取"，EOF 外返回 false
    if (!dataScanner.TryReadAt(addr, out var frame)) {
        throw new ObjectNotFoundException(id);
    }
    
    // 3. Deserialize + Materialize
    return Materialize<T>(frame.Payload);
}
```

**体验评估**：
- ✅ **直观性**：`TryReadAt` 遵循 .NET 的 `TryXxx` 模式（返回 `bool`）
- ✅ **增长可见性**：开发者自然期待"当前时刻能读到什么就读什么"（而非快照）
- ⚠️ **<deleted-place-holder> 失效边界**：如果文件被截断（场景 A），旧地址会失效，但 API 如何传递这个信息？（见 §2 反模式）
- ⚠️ **Scanner 生命周期**：`CreateScanner()` 多次调用是创建新实例还是返回同一个？（见 §3）

---

**场景 C：Commit 流程（二阶段提交）**

```csharp
// StateJournal.Commit() 内部流程
public void Commit() {
    // Phase 1: Data 文件
    var dataFramer = _dataFile.CreateFramer();
    foreach (var obj in dirtyObjects) {
        dataFramer.Append(SerializeObjectVersion(obj));
    }
    dataFramer.Flush();          // ✅ 推送 RBF 缓冲到 OS
    _dataFile.DurableFlush();    // ✅ Data 先落盘
    
    // Phase 2: Meta 文件
    var metaFramer = _metaFile.CreateFramer();
    var metaCommit = new MetaCommitRecord {
        DataTail = _dataFile.LengthBytes,  // ← Commit Point 的"快照"
        // ...
    };
    metaFramer.Append(SerializeMetaCommit(metaCommit));
    metaFramer.Flush();
    _metaFile.DurableFlush();    // ✅ Meta 后落盘 → Commit Point
}
```

**体验评估**：
- ✅ **持久化语义清晰**：三层分离（`Flush` → `DurableFlush` → Commit Point）易于理解
- ✅ **顺序控制明确**：两次 `DurableFlush` 的调用顺序直接映射到"Data 先持久化"需求
- ⚠️ **Framer 多次创建**：每次 Commit 都调用 `CreateFramer()`，是否会失败？（见 §2 反模式）
- ⚠️ **LengthBytes 时机**：`DataTail = _dataFile.LengthBytes` 是在 `DurableFlush()` 之前还是之后读取？影响正确性（见 §4）

---

#### 2. 反模式识别（基于真实用例）

**反模式 1：误认为 ScanReverse() 返回可持久化集合**

```csharp
// ❌ 错误：试图存储枚举结果
var frames = metaScanner.ScanReverse();  // ref struct 不能存储到字段
_cachedFrames = frames;  // 编译错误！

// ✅ 正确：立即消费，需要持久化则拷贝
foreach (var frame in metaScanner.ScanReverse()) {
    if (NeedCache(frame)) {
        _cache.Add(frame.PayloadToArray());  // 显式拷贝
    }
}
```

**根因**：`ref struct` 的"护栏"设计（见 [rbf-interface.md](../../atelia/docs/Rbf/rbf-interface.md) `[A-RBF-REVERSE-SEQUENCE]`）
**改进建议**：在异常信息中明确提示"使用 PayloadToArray() 持久化数据"

---

**反模式 2：忘记 <deleted-place-holder> 失效后的容错**

```csharp
// ❌ 错误：假设 <deleted-place-holder> 永远有效
var addr = versionIndex.Lookup(id);
// 后来文件被 Truncate（恢复流程）
if (!dataScanner.TryReadAt(addr, out var frame)) {
    // 开发者困惑：为什么读不到？明明 VersionIndex 里有这个地址
}

// ✅ 正确：在 Truncate 后重建 VersionIndex
dataFile.Truncate(head.DataTail);
RebuildVersionIndex();  // 清除被截断区域的地址
```

**根因**：`Truncate` 使地址失效，但 VersionIndex 不会自动同步
**改进建议**：`Truncate` 的文档应明确"调用后，所有 ≥ newLength 的 <deleted-place-holder> 失效"

---

**反模式 3：在 DurableFlush 前读取 LengthBytes**

```csharp
// ❌ 错误：顺序错误
dataFramer.Append(data);
dataFramer.Flush();
var dataT tail = dataFile.LengthBytes;  // ← 可能还未包含刚 Append 的数据
dataFile.DurableFlush();

// ✅ 正确：先 DurableFlush，后读取 LengthBytes
dataFramer.Append(data);
dataFramer.Flush();
dataFile.DurableFlush();               // ← 确保落盘
var dataTail = dataFile.LengthBytes;   // ← 此时 LengthBytes 是可信的
```

**根因**：`Flush()` 只推送到 OS，`LengthBytes` 在 OS 视角可能还未更新
**改进建议**：`LengthBytes` 文档应明确"返回调用时刻的 OS 视角长度"

---

**反模式 4：多次调用 CreateFramer() 导致异常**

```csharp
// ❌ 错误：每次 Commit 都创建新 Framer
public void Commit() {
    var framer = _dataFile.CreateFramer();  // 第一次 OK
    // ...
}
public void Commit() {
    var framer = _dataFile.CreateFramer();  // 第二次抛异常！
}

// ✅ 正确：在构造时创建并复用
private readonly IRbfFramer _dataFramer;
public StateJournal(RbfFile dataFile) {
    _dataFramer = dataFile.CreateFramer();  // 仅创建一次
}
```

**根因**：`CreateFramer()` 单例约束（监护人裁决）
**改进建议**：异常信息应明确"Framer already exists, reuse the existing instance"

---

#### 3. 心智模型建议（基于真实用例）

**隐喻 1：RbfFile 像"进程级文件租约"**

| 传统 FileStream | RbfFile (长生命周期) |
|:----------------|:---------------------|
| 打开 → 读写 → 关闭 | 启动时打开 → 进程生命周期持有 → 关闭时释放 |
| 短生命周期，用完即关 | 长生命周期，全局单例 |
| 每次操作都重新打开 | 一次打开，持续使用 |

**关键洞见**：开发者应该把 `RbfFile` 想象成"数据库连接"而非"文件流"——它是稀缺资源，需要长期持有。

---

**隐喻 2：TryReadAt 的增长可见性像"望远镜观测增长中的彗星"**

```
时刻 T1：文件长度 = 100KB
  TryReadAt(addr=80KB) → ✅ 成功
  TryReadAt(addr=120KB) → ❌ 超出 EOF

时刻 T2：Append + Flush 后，文件长度 = 150KB
  TryReadAt(addr=120KB) → ✅ 成功（新数据可见）
```

**关键洞见**：`TryReadAt` 不是快照，而是"实时观测"——文件增长后，新地址变得可读。这符合"持续 Append 的日志文件"直觉。

---

**隐喻 3：Flush 三层次像"快递发货流程"**

| 操作 | 快递类比 | 数据位置 | 持久化 |
|:-----|:---------|:---------|:-------|
| `Append(data)` | 商品打包 | RBF 内部缓冲 | ❌ |
| `Framer.Flush()` | 交给快递站 | OS 缓冲区 | ❌ |
| `File.DurableFlush()` | 快递车发车 | 磁盘物理介质 | ✅ |

**关键洞见**：
- `Flush()` 像"把包裹交给快递站"——包裹离开了你的手，但还没上车
- `DurableFlush()` 像"快递车发车"——包裹真正开始运输，不会因为快递站停电而丢失

---

**隐喻 4：Truncate 像"时光倒流后的记忆抹除"**

```
原时间线：[A, B, C, D, E]  (文件长度 = 5)
撕裂检测：D 和 E 是"未来碎片"（DataTail < 当前长度）
Truncate(3)：[A, B, C]      (文件长度 = 3)

后果：所有指向 D/E 的 <deleted-place-holder> 失效
```

**关键洞见**：`Truncate` 不仅是物理操作（缩短文件），更是逻辑操作（抹除历史）。所有依赖被截断区域的指针（<deleted-place-holder>）都应重建。

---

#### 4. API 优化建议

**优化 1：LengthBytes 的时机保证**

**问题**：开发者不确定 `LengthBytes` 是否包含刚 `Flush()` 的数据
**建议**：在 `DurableFlush()` 的文档中明确：

```csharp
/// <summary>
/// 强制数据落盘（fsync 等价）。
/// </summary>
/// <remarks>
/// <para><b>LengthBytes 保证</b>：DurableFlush() 成功返回后，
/// LengthBytes 保证反映已落盘的数据长度。</para>
/// </remarks>
void DurableFlush();
```

---

**优化 2：Truncate 的副作用提示**

**问题**：开发者不知道 `Truncate` 会使 <deleted-place-holder> 失效
**建议**：在 `Truncate` 的文档中明确：

```csharp
/// <summary>
/// 截断文件到指定长度（恢复流程用）。
/// </summary>
/// <param name="newLengthBytes">新长度（byte），MUST ≤ 当前长度且 4B 对齐</param>
/// <remarks>
/// <para><b><deleted-place-holder> 失效</b>：截断后，所有 <deleted-place-holder> ≥ newLengthBytes 的地址失效。
/// 后续 TryReadAt(失效地址) 将返回 false。</para>
/// <para><b>上层责任</b>：调用方应重建依赖这些地址的索引（如 VersionIndex）。</para>
/// </remarks>
void Truncate(ulong newLengthBytes);
```

---

**优化 3：CreateFramer/CreateScanner 的单例语义文档化**

**问题**：开发者不知道 `CreateFramer()` 只能调用一次
**建议**：在接口文档中明确：

```csharp
/// <summary>
/// 创建写入器（单例：第二次调用抛 InvalidOperationException）。
/// </summary>
/// <remarks>
/// <para><b>生命周期</b>：Framer 生命周期绑定到 RbfFile，应在构造时创建并复用。</para>
/// <para><b>错误信息</b>：第二次调用时，异常包含 "Framer already exists, reuse the existing instance"。</para>
/// </remarks>
IRbfFramer CreateFramer();
```

---

**优化 4：ScanReverse 的"立即消费"提示**

**问题**：开发者试图存储 `ScanReverse()` 的返回值到字段
**建议**：在返回类型文档中明确：

```csharp
/// <summary>
/// 从文件尾部逆向扫描所有帧。
/// </summary>
/// <returns>帧序列（从尾到头）</returns>
/// <remarks>
/// <para><b>ref struct 限制</b>：返回值是 ref struct，不能存储到字段或跨 await 边界。
/// 必须在方法内立即 foreach 消费。</para>
/// <para><b>持久化数据</b>：若需保存 Payload，调用 frame.PayloadToArray()。</para>
/// <para><b>设计理由</b>：ref struct 的编译器限制是"护栏"，防止 use-after-free。</para>
/// </remarks>
RbfReverseSequence ScanReverse();
```

---

#### 5. 总结：真实场景驱动的 DX 改进

| 场景 | 核心痛点 | 改进方向 |
|:-----|:---------|:---------|
| **启动恢复** | `LengthBytes` 与 `DataTail` 对比不确定 | 明确 `LengthBytes` 语义（OS 视角） |
| **Lazy Load** | <deleted-place-holder> 失效边界模糊 | `Truncate` 文档明确"地址失效"副作用 |
| **Commit 流程** | `Flush` 与 `DurableFlush` 时机混淆 | 三层次隐喻（打包→快递站→发车） |
| **反复 Commit** | 多次 `CreateFramer()` 异常难理解 | 异常信息提示"reuse existing instance" |

**核心洞见**：
1. **RbfFile 是长生命周期对象**（进程级文件租约），而非短生命周期流
2. **TryReadAt 是实时观测**（文件增长后新地址可读），而非快照
3. **Truncate 是逻辑抹除**（地址失效需要重建索引），而非单纯物理操作
4. **ref struct 的限制是护栏**（防止 use-after-free），需要在文档中解释设计理由

---

### Advisor-GPT 发言

### Advisor-GPT 发言（基于 MVP 重新核查）

#### 1. 必须规范化的契约

基于已收敛的 MVP 接口（含 `LengthBytes` 与 `Truncate`），以下行为必须进入“可判定条款”，否则实现会倒灌设计（尤其是 durability、恢复、以及视图生命周期）。

1) **工厂方法语义（CreateNew/OpenExisting）**
- `CreateNew(path)` 的语义必须是 **FailIfExists**（已裁决），并明确“成功返回前完成 Genesis 写入”。
- `OpenExisting(path)` 必须验证 **Genesis Fence**（`RBF1`），并明确“遇到非 RBF/损坏时的失败类型”。

2) **`IRbfFile` 生命周期与视图失效**
- `Dispose()` 是否幂等必须裁决；Disposed 后对 `IRbfFile`、以及其创建的 `IRbfFramer/IRbfScanner` 的调用行为必须可判定。
- 明确 `IRbfFramer/IRbfScanner` 的生命周期是否由 `IRbfFile` 管理（MVP 倾向：由 file 统一管理；视图本身不可独立 dispose）。

3) **视图创建次数与并发模型（MVP 已裁决：单例）**
- `CreateFramer()` 第二次调用必须是“fail-fast”，并固定异常类型。
- `CreateScanner()` 第二次调用同上（监护人裁决：MVP 单例）。
- `IRbfFile` / `IRbfFramer` / `IRbfScanner` 的线程安全与多进程并发语义必须给出“允许/不支持/未定义”的边界（至少要禁止或 fail-fast 多 writer）。

4) **`DurableFlush()` 的边界与与 `IRbfFramer.Flush()` 的关系**
- 必须明确：`IRbfFramer.Flush()` 仅是“推送到下层 writer/stream”，不承诺 durability（已由 `rbf-interface.md` 的 `[S-RBF-FRAMER-NO-FSYNC]` 建立）。
- 必须补齐：durable 由 `IRbfFile.DurableFlush()` 提供（并据此修订 `rbf-interface.md` 中 `IRbfFramer.Flush()` remarks 里“上层持有底层句柄”的暗契约表述）。
- 必须明确 DurableFlush 的“完成点”（同步返回即 durable 达成），以及失败后的对象状态（是否仍可继续写）。

5) **`LengthBytes` 与增长可见性（直接关系到恢复与撕裂检测）**
- `LengthBytes` 必须定义为“调用时刻 OS 视角的当前文件长度（byte）”。
- 明确与增长相关的最低承诺：同一进程同一 file 句柄持续 append 后，后续 `LengthBytes` 允许增长；不得缓存为打开瞬间快照。

6) **`Truncate(newLengthBytes)` 的前置条件/后置条件**
- 前置条件必须裁决：是否允许扩容（通常不允许）；是否要求 `newLengthBytes <= LengthBytes`；是否要求 4 字节对齐（建议 MUST：与 <deleted-place-holder> 对齐一致）。
- 后置条件必须裁决：截断后，被截断区间内的 <deleted-place-holder>/指针后续 `TryReadAt` 必须失败（返回 false 或抛异常需二选一）。

补位：**缺口（可判定性）**
- 现有 `rbf-interface.md` 的 `[S-RBF-SCANREVERSE-CONCURRENT-MUTATION]` 将“枚举期间修改”定义为未定义行为。MVP 若要允许“Scanner 长期随机读并在文件增长后可读到新帧”，至少需要为 `TryReadAt` 与 `LengthBytes` 明确“增长语义”；对 `ScanReverse` 可以继续维持未定义（冷路径），但必须在条款里说清楚。

#### 2. 异常契约清单

以下异常类型必须在规范中明确（目标：让调用方能写黑盒断言；让实现者无需猜测）：

1) **参数类（Argument）**
- `ArgumentNullException`：`path == null`。
- `ArgumentException`：`path` 为空/全空白/包含非法字符等。
- `ArgumentOutOfRangeException`：`Truncate(newLengthBytes)` 传入值不满足约束（例如大于当前长度、或不满足对齐约束）。

2) **路径/权限/存在性（I/O Surface）**
- `UnauthorizedAccessException`：无权限创建/打开/写入/截断。
- `FileNotFoundException`：`OpenExisting` 路径不存在。
- `DirectoryNotFoundException`：目录不存在。
- `IOException`：其他 I/O 失败（磁盘满、共享冲突、句柄失效等）。

3) **格式/内容损坏（Corruption）**
- 至少要固定一种“非 RBF/Genesis 不匹配/明显损坏”的异常类型：建议 `InvalidDataException`（或项目自定义 `RbfCorruptedException`，二选一并写入 SSOT）。

4) **生命周期/状态机（Lifecycle / State）**
- `ObjectDisposedException`：Disposed 后调用任意成员（包含 `DurableFlush/Truncate/CreateFramer/CreateScanner/LengthBytes`）以及视图对象的方法。
- `InvalidOperationException`：违反单例约束（第二次 `CreateFramer` / `CreateScanner`）或在不允许的状态调用。

5) **不支持的文件类型/平台能力**
- `NotSupportedException`：非 seekable/不支持随机访问的底层对象（若实现仅支持常规文件）。

#### 3. 边界条件定义

1) **文件长度边界**
- 0 字节文件：`OpenExisting` 必须失败（因为 Genesis Fence 需存在）。
- 仅 Genesis Fence（长度=4）：`CreateScanner().ScanReverse()` 必须返回空序列且不抛（与 `[S-RBF-SCANREVERSE-EMPTY-IS-OK]` 对齐）。
- 极大文件：若实现受 `long` 限制，必须在规范中声明最大支持长度（或明确超出时抛 `NotSupportedException/IOException`）。

2) **CreateNew / OpenExisting 的副作用与原子性**
- `CreateNew` 在写 Genesis 失败时：需要裁决“允许遗留无效文件”还是“必须清理回滚”。两者都可行，但必须写明，否则测试不可判定。

3) **`LengthBytes` 与并发外部修改**
- 若文件被外部进程截断/替换：`LengthBytes`/`TryReadAt`/`ScanReverse` 的行为至少要归入“可能抛 `IOException` 或未定义”；不能沉默返回错误数据。

4) **`Truncate` 的合法输入域**
- `newLengthBytes == LengthBytes`：必须定义为 no-op（建议 MUST 成功且不改变语义）。
- `newLengthBytes > LengthBytes`：必须明确允许与否（MVP 建议：禁止并抛 `ArgumentOutOfRangeException`）。
- 对齐：建议 MUST 要求 `newLengthBytes % 4 == 0`，否则抛 `ArgumentOutOfRangeException`（避免制造非对齐 <deleted-place-holder> 空间）。

5) **视图创建与使用顺序**
- 在同一 `IRbfFile` 上，允许先 `CreateScanner` 再 `CreateFramer`，以及相反顺序吗？
  - MVP 最小化建议：两者都允许，但并发语义不扩张；如实现难度高，可明确“允许共存，但线程不安全；行为以调用时刻文件内容为准”。

#### 4. 关键条款草案（3-5 个）

**`[S-RBF-FILE-CREATENEW-FAILIFEXISTS]`**
> `RbfFile.CreateNew(path)` MUST 创建一个全新的 RBF 文件。若目标路径已存在（无论内容是否为 RBF），该方法 MUST 失败，并抛出 `IOException`（或更具体的存在性异常，需固定）。成功返回前 MUST 写入并持久化（至少到 OS 缓冲）Genesis Fence。

**`[S-RBF-FILE-OPENEXISTING-VERIFY-GENESIS]`**
> `RbfFile.OpenExisting(path)` MUST 仅在目标文件存在且其开头 Genesis Fence 有效时成功返回；否则 MUST 失败。Genesis 不匹配或文件明显非 RBF 时 MUST 抛出 `InvalidDataException`（或项目自定义的 corruption 异常）。

**`[S-RBF-FILE-SINGLETON-VIEWS]`**
> 对同一 `IRbfFile` 实例：`CreateFramer()` MUST 成功至多一次；`CreateScanner()` MUST 成功至多一次。第二次调用 MUST 抛出 `InvalidOperationException`。

**`[S-RBF-FILE-DURABLEFLUSH-BOUNDARY]`**
> `IRbfFile.DurableFlush()` MUST 在成功返回前使得该文件上先前已完成 `IRbfFramer.Flush()` 的字节达到 durability（等价 fsync 语义）。在 Disposed 状态下调用 MUST 抛出 `ObjectDisposedException`。

**`[S-RBF-FILE-LENGTH-AND-TRUNCATE]`**
> `LengthBytes` MUST 返回调用时刻的文件长度（byte）。`Truncate(newLengthBytes)` MUST 要求 `newLengthBytes <= LengthBytes` 且 `newLengthBytes % 4 == 0`；否则 MUST 抛出 `ArgumentOutOfRangeException`。截断成功后，位于被截断区域的任意 <deleted-place-holder> 其后续 `TryReadAt` MUST 失败（返回 `false`）。

---

## 监护人--刘世超 第一轮发言

我们要有的放矢，我建议让Claude和GPT分别独立读取和分析一次`atelia/docs/StateJournal/mvp-design-v2.md.original`文件(正在拆分中，这是未改动的大单体版本)，来分析上层的StateJournal对文件访问接口的功能需求，这样有利于设计出对上层使用者来说最佳的文件访问接口。
发言完毕，期待二位呈现的调查与分析结果。

对于具体问题上：
  关于“D1：Scanner 可见性语义”，应以前面对需求的具体调查为准。从我的理解来看，目前的接口设计和需求并不完全匹配。StateJournal的需求有2个：1. 打开文件后找Meta的尾帧。2. 用<deleted-place-holder>随机读取特定的目标二进制帧。 StateJournal对RBF文件的用法是以Meta尾部帧为线索，沿着引用关系重建出RootObject，运行时长期持有文件句柄，单读者单写者(因为有内存对象缓存`Identity Map`，Lazy Load，读取操作都是伴随着写入缓存的副作用的)。StateJournao Commit操作比如需要先Date文件落盘再Meta文件落盘，用的全是RBF的Append Frame操作，我们不需要也不支持Inplace Write操作，Append-Only。
  关于“D2：Create 对已有文件的行为”，我建议选“A. FailIfExists”。
  关于“D3：多 Scanner 实例约束”，我建议选“C. 单例”

---

### Advisor-Claude 需求分析

> **任务**：从 StateJournal 设计文档提取 RBF 文件访问的实际需求，识别当前接口设计与需求的 Gap。
> **taskTag**: #design

#### 1. RBF 文件使用模式

##### 1.1 文件数量与职责

从 [mvp-design-v2.md.original](../../../atelia/docs/StateJournal/mvp-design-v2.md.original) §3.2 提取：

| 文件 | 作用 | 记录类型 |
|:-----|:-----|:---------|
| **Data 文件** | 承载对象版本记录（ObjectVersionRecord）+ VersionIndex 版本 | `FrameTag = 0x00010001` (Dict) 等 |
| **Meta 文件** | Commit 日志，记录每次提交的元数据 | `FrameTag = 0x00000002` (MetaCommitRecord) |

**关键特征**：
- 两个文件都是 **Append-Only**
- Data 文件需要**随机读取**（通过 Ptr64/<deleted-place-holder>）
- Meta 文件需要**逆向扫描**（找最后一个有效 MetaCommitRecord）

##### 1.2 打开/关闭时机

| 时机 | 操作 | 文档依据 |
|:-----|:-----|:---------|
| **启动时** | Open Meta → 逆向扫描找 HEAD → Open Data | §3.3.1 Open |
| **运行时** | 长期持有两个文件句柄 | 监护人确认："运行时长期持有文件句柄" |
| **关闭时** | 进程终止时关闭 | 隐式（进程生命周期） |

**洞见**：文件句柄是**长期持有**的，不是"用完即关"。这影响了 `IRbfFile` 的生命周期设计——它应该是进程级的单例，而非短生命周期对象。

##### 1.3 两文件关系

```
┌─────────────────────────────────────────────────────────────┐
│                     StateJournal Commit                      │
│                                                              │
│  ┌─────────────┐   1. Append   ┌─────────────┐              │
│  │ Dirty Objects│──────────────▶│  Data File  │              │
│  └─────────────┘               └──────┬──────┘              │
│                                       │                      │
│                                       │ 2. Fsync             │
│                                       ▼                      │
│  ┌─────────────┐   3. Append   ┌─────────────┐              │
│  │ MetaCommit   │──────────────▶│  Meta File  │              │
│  │  Record      │               └──────┬──────┘              │
│  └─────────────┘                       │                      │
│                                       │ 4. Fsync             │
│                                       ▼                      │
│                              ← COMMIT POINT →                │
└─────────────────────────────────────────────────────────────┘
```

---

#### 2. 读取操作详细需求

##### 2.1 "找 Meta 尾帧"的具体含义

从 §3.3.1 Open 流程提取：

**操作描述**：
1. 从 meta 文件尾部**逆向扫描**
2. 找到**最后一个 CRC32C 有效**的 `MetaCommitRecord`
3. 验证其 `DataTail` 不超过 data 文件长度（撕裂检测 `[R-META-AHEAD-BACKTRACK]`）
4. 成功找到即为 HEAD

**需要的接口能力**：
- ✅ 逆向扫描（`ScanReverse()`）— 当前接口已有
- ✅ 帧内容读取 — 当前接口已有
- ❓ **文件末尾位置获取** — 用于初始化扫描起点

**关键洞见**：这**不是**"读取最后一帧"，而是"从尾部向前扫描，找到第一个有效帧"。因为可能存在撕裂写入（meta 领先 data），需要跳过无效帧。

##### 2.2 "随机读取"的使用场景

| 场景 | 触发时机 | 读取对象 | 频率 |
|:-----|:---------|:---------|:-----|
| **LoadObject** | 首次访问对象 | ObjectVersionRecord（沿版本链） | 中等（冷启动时高，运行时按需） |
| **Version Chain Replay** | Materialize 对象状态 | 多个 ObjectVersionRecord | 每次 LoadObject |
| **VersionIndex 查询** | LoadObject 内部 | VersionIndex 版本记录 | 每次 LoadObject（可能多跳） |

**操作模式**：
```
<deleted-place-holder> (from VersionIndex) → TryReadAt → Deserialize → (PrevVersionPtr) → TryReadAt → ...
```

**需要的接口能力**：
- ✅ 随机读取（`TryReadAt(<deleted-place-holder>)`）— 当前接口已有

##### 2.3 读取频率分析

| 路径 | 热度 | 说明 |
|:-----|:-----|:-----|
| Meta 逆向扫描 | **冷路径** | 仅启动时执行一次 |
| Data 随机读取 | **温路径** | 首次访问对象时触发，有 Identity Map 缓存 |
| 运行时读取 | **冷路径** | 几乎不发生——Lazy Load 后对象常驻内存 |

**洞见**：运行时的读取需求很低。一旦对象被 LoadObject 加载，它就进入 Identity Map 并常驻内存。后续访问直接命中缓存，不再触发磁盘读取。

---

#### 3. 写入操作详细需求

##### 3.1 Commit 流程的精确操作序列

从 §3.4.5 CommitAll 和 §3.4.4 二阶段提交提取：

```
┌─────────────────────────────────────────────────────────────┐
│ Phase 1: Prepare (Data File)                                 │
├─────────────────────────────────────────────────────────────┤
│ 1. for each dirty object:                                    │
│    - framer.Append(ObjectVersionRecord)                      │
│ 2. framer.Append(VersionIndex overlay)                       │
│ 3. framer.Flush()  ← 推送到 OS 缓冲                          │
│ 4. DurableFlush()  ← Data 文件 fsync (MUST)                  │
├─────────────────────────────────────────────────────────────┤
│ Phase 2: Finalize (Meta File)                                │
├─────────────────────────────────────────────────────────────┤
│ 5. metaFramer.Append(MetaCommitRecord)                       │
│ 6. metaFramer.Flush()                                        │
│ 7. DurableFlush()  ← Meta 文件 fsync (MUST)                  │
│    ← COMMIT POINT                                            │
├─────────────────────────────────────────────────────────────┤
│ Phase 3: Memory Update                                       │
├─────────────────────────────────────────────────────────────┤
│ 8. for each dirty object:                                    │
│    - object.OnCommitSucceeded()                              │
└─────────────────────────────────────────────────────────────┘
```

**关键约束**（`[R-COMMIT-FSYNC-ORDER]`）：
- Data fsync **MUST** 在 Meta append 之前完成
- Meta fsync 完成 = Commit Point 确立

##### 3.2 Flush 需求分析

| 操作 | 语义 | 调用次数/Commit |
|:-----|:-----|:----------------|
| `Framer.Flush()` | 推送 RBF 缓冲到 OS | 2 次（Data + Meta 各一次） |
| `DurableFlush()` | fsync，保证数据落盘 | 2 次（Data + Meta 各一次） |

**洞见**：每次 Commit 需要 **2 次 DurableFlush()**——这是事务语义的核心。设计必须支持对两个文件独立控制 DurableFlush。

##### 3.3 Append-Only 确认

从监护人描述和文档中确认：
- ✅ 只有 Append 操作，无 Inplace Write
- ✅ 无 Truncate 需求（恢复时的 Truncate 是特殊情况，见 §3.5）
- ✅ 无 Seek-then-Write 需求

**洞见**：Framer 只需要 `Append` 能力，不需要随机写入。这简化了接口设计。

##### 3.4 Reservation（预留空间）需求

从 §5 实现建议提取：
- `ChunkedReservableWriter` 用于"先写 payload、后回填长度/CRC32C/指针"
- 这是 **Framer 内部实现**，不需要暴露到文件接口层

**结论**：文件接口层**不需要** Reservation API。

---

#### 4. 并发与生命周期确认

##### 4.1 单读者单写者确认

监护人明确：
> "单读者单写者（因为有内存对象缓存 Identity Map，Lazy Load，读取操作都是伴随着写入缓存的副作用的）"

**解读**：
- 不是"无并发读取"，而是"读取会触发写入副作用"（更新 Identity Map）
- 这意味着从文件接口层看，**同一时刻只有一个逻辑操作**
- 无需考虑多线程并发访问同一文件的场景

##### 4.2 文件句柄生命周期

| 特征 | 值 | 影响 |
|:-----|:---|:-----|
| 持有时长 | 进程生命周期 | `IRbfFile` 是长生命周期对象 |
| 打开次数 | 启动时各打开一次 | 无需频繁 Open/Close |
| 关闭时机 | 进程终止 | `Dispose()` 在最后调用 |

##### 4.3 Framer 与 Scanner 的关系

从使用模式分析：

| 阶段 | 需要的能力 | 是否同时 |
|:-----|:-----------|:---------|
| 启动恢复 | Scanner（逆向扫描 Meta） | 否 |
| 启动恢复 | Scanner（随机读 Data） | 否 |
| 运行时 | Framer（Append Data） + Scanner（随机读 Data） | 可能同时 |
| Commit | Framer（Append Data → Append Meta） | 顺序 |

**关键发现**：运行时可能需要 **Framer 和 Scanner 共存**（写入新帧的同时，Lazy Load 可能触发读取旧帧）。但这不是"并发"——是**同一线程的交替操作**。

---

#### 5. Gap 分析：当前设计 vs 实际需求

##### 5.1 过度设计

| 功能 | 当前设计 | 实际需求 | 评估 |
|:-----|:---------|:---------|:-----|
| **"跟随视图"** | D1 讨论了 Scanner 是否看到后续增长 | Meta 文件只需启动时扫描一次；Data 文件只需随机读取 | **过度设计** — 不需要"跟随视图" |
| **多 Scanner 实例** | D3 讨论了数量限制 | 实际只需要一个 Scanner | **过度设计** — 单例即可 |
| **ScanReverse 通用性** | 设计为枚举所有帧 | Meta 只需找最后一个有效帧；Data 不需要扫描 | **可能过度** — 但保留通用性无害 |

##### 5.2 功能缺失

| 缺失功能 | 需求来源 | 影响 |
|:---------|:---------|:-----|
| **两文件独立 DurableFlush** | Commit 流程需要先 Data fsync 再 Meta fsync | **关键缺失** — 当前设计 `RbfFile.DurableFlush()` 是单文件的，但 StateJournal 需要协调两个文件 |
| **获取文件当前长度** | 恢复时验证 `DataTail` 不超过 Data 文件长度 | **可能需要** — 用于撕裂检测 |

##### 5.3 抽象不当

| 问题 | 当前设计 | 实际使用模式 | 建议 |
|:-----|:---------|:-------------|:-----|
| **Scanner 的定位** | 设计为"枚举器"（`ScanReverse()` 产出帧序列） | Data 文件只需 `TryReadAt()`；Meta 文件只需启动时扫描一次 | Scanner 更像是"随机读取器" + "一次性扫描器" |
| **Framer/Scanner 共存** | 大量讨论"是否允许共存" | 实际是同一线程交替使用，不是并发 | 不需要复杂的共存语义，只需确保操作原子性 |
| **文件入口点** | `RbfFile.Open()` 返回单一入口 | StateJournal 需要管理两个独立文件（Data + Meta） | 可能需要上层自己管理两个 `RbfFile` 实例 |

##### 5.4 核心 Gap 总结

```
┌────────────────────────────────────────────────────────────┐
│                      核心 Gap 识别                          │
├────────────────────────────────────────────────────────────┤
│ 1. 当前设计假设"单文件场景"                                  │
│    实际需求是"双文件协调"（Data + Meta）                     │
│                                                            │
│ 2. Scanner 被设计为"持续观察者"                              │
│    实际需求是"启动时一次性扫描" + "运行时随机读取"            │
│                                                            │
│ 3. 大量讨论"并发可见性"                                      │
│    实际是单线程场景，不需要复杂的可见性语义                   │
└────────────────────────────────────────────────────────────┘
```

---

#### 6. 接口调整建议

##### 6.1 简化 Scanner 职责

**当前**：Scanner 是一个"枚举器视图"，可能需要处理并发可见性
**建议**：Scanner 退化为两个独立能力：

```csharp
// 能力 1：随机读取（运行时热路径）
bool TryReadFrameAt(<deleted-place-holder> address, out RbfFrame frame);

// 能力 2：逆向扫描（启动时冷路径）
IEnumerable<RbfFrame> ScanReverse();  // 一次性使用
```

**理由**：
- 随机读取是核心需求，应该是简单的"给地址，返回帧"
- 逆向扫描只在启动时用一次，不需要考虑"跟随视图"

##### 6.2 明确双文件模式

StateJournal 的实际使用是：
```csharp
class StateJournal {
    private RbfFile _dataFile;   // 独立管理
    private RbfFile _metaFile;   // 独立管理
    
    void Commit() {
        // 1. Data 文件操作
        _dataFramer.Append(...);
        _dataFramer.Flush();
        _dataFile.DurableFlush();  // Data 先落盘
        
        // 2. Meta 文件操作
        _metaFramer.Append(...);
        _metaFramer.Flush();
        _metaFile.DurableFlush();  // Meta 后落盘 = Commit Point
    }
}
```

**建议**：接口设计应明确"每个 RbfFile 是独立的单文件抽象"，双文件协调由上层负责。

##### 6.3 简化 Framer/Scanner 生命周期

**当前**：复杂的共存语义讨论
**建议**：

```csharp
// 简化模型：Framer 和 RandomReader 是同一 RbfFile 的两个"视图"
// 它们共享文件句柄，但操作互不影响（因为是单线程）
class RbfFile {
    IRbfFramer Framer { get; }       // 单例，创建时即初始化
    IRbfRandomReader Reader { get; } // 单例，创建时即初始化
    
    // 逆向扫描是一次性操作，返回枚举器
    IEnumerable<RbfFrame> ScanReverse();
    
    void DurableFlush();
    void Dispose();
}
```

**理由**：
- 去掉 `CreateFramer()` / `CreateScanner()` 的"工厂"语义
- Framer 和 Reader 天然单例，避免"第二次调用抛异常"的边界情况
- 符合实际使用模式：打开文件后长期持有，不反复创建视图

##### 6.4 D1/D2/D3 裁决建议

| 决策点 | 监护人倾向 | 我的分析支持 | 建议 |
|:-------|:-----------|:-------------|:-----|
| **D1：Scanner 可见性** | 未明确 | 不需要"跟随视图"——Meta 只扫描一次，Data 只随机读取 | **快照视图（简化实现）** |
| **D2：Create 对已有文件** | A. FailIfExists | 符合安全原则 | **A. FailIfExists** |
| **D3：多 Scanner 约束** | C. 单例 | 实际只需要一个 Reader | **C. 单例** |

---

#### 7. 总结

**核心发现**：

1. **StateJournal 的 RBF 使用模式比预想的简单**
   - 两个文件，各自独立管理
   - 单线程操作，无并发语义需求
   - 写入只有 Append，读取是启动时扫描 + 运行时随机读取

2. **当前讨论的复杂性来自"通用化假设"**
   - 多 Scanner 实例 → 实际只需要一个
   - 跟随视图 vs 快照视图 → 实际不需要动态可见性
   - Framer/Scanner 共存语义 → 实际是同线程交替使用

3. **接口应该"刚好够用"而非"预留扩展"**
   - MVP 阶段，简单直接比灵活抽象更重要
   - 过度设计会增加实现复杂度和出错概率

**建议的下一步**：
1. 确认 D1/D2/D3 裁决
2. 将 Scanner 简化为 `RandomReader` + `ScanReverse()` 一次性枚举
3. 去掉工厂方法，改为属性访问（`file.Framer` / `file.Reader`）
4. 明确"双文件由上层协调"的设计边界

---

<EditAnchor EOF>

### Advisor-GPT 需求验证与规范核查

#### 1. Claude 需求理解的验证结果

**总体结论：大体准确，但有 2 个关键“语义边界”需要纠正/补全。**

1) **文件模式（Data + Meta 双文件）**：准确。
- StateJournal 规格在“磁盘布局”明确将 **data file** 与 **meta file** 分离：data 承载 ObjectVersionRecord/VersionIndex 版本，meta 承载 MetaCommitRecord（commit log）。
- Commit 的 durability/可见点也明确绑定到 meta file 的 durable 完成。

2) **读取需求："找 Meta 尾帧"**：Claude 的“逆向扫描”判断准确，但扫描条件需要更精确。
- StateJournal 的 Open 流程是：从 meta 文件尾部回扫，**找到最后一个 CRC32C 有效**的 MetaCommitRecord；并且必须做“撕裂提交检测”：若该条记录的 `DataTail` 大于当前 data 文件长度或其 `VersionIndexPtr` 无法解引用，则继续回扫上一条（`[R-META-AHEAD-BACKTRACK]`）。
- 这不是“读最后一帧”，而是“从尾到头找最后一条可验证 HEAD”。
- **空仓库边界**也必须算进需求：meta 文件为空或找不到有效 MetaCommitRecord 时，StateJournal 将进入“隐式空状态”（EpochSeq=0 等）。

3) **读取需求："随机读取" 与 Lazy Load**：Claude 的“TryReadAt(<deleted-place-holder>) + 版本链重放”判断准确。
- 随机读热路径来自 `LoadObject(ObjectId)`：VersionIndex 给出 ObjectVersionPtr，然后沿 `PrevVersionPtr` 链做 Deserialize+Materialize。
- Lazy Load（ObjRef 透明加载）会触发 LoadObject，因此确实是“按需随机读”的触发场景。

4) **写入需求：Commit 是否需要 2 次 DurableFlush，顺序是否 Data→Meta**：Claude 判断准确。
- StateJournal 明确 `[R-COMMIT-FSYNC-ORDER]`：必须先 data 文件 durable，再 meta 文件 durable；meta durable 完成定义 Commit Point（`[R-COMMIT-POINT-META-FSYNC]`）。
- 因此“每次 commit 至少 2 次 durable（data+meta 各一次）”成立。

**需要纠正/补全的关键点（影响 D1 与接口语义）**：
- Claude 说“跟随视图不需要”在 **ScanReverse（尾扫）** 维度基本成立；但在 **TryReadAt（随机读）** 维度必须补全：StateJournal 是长生命周期持有句柄、持续 append 的系统，随机读必须在“文件增长（同进程同句柄）”下可判定。
    - 最小正确语义：`TryReadAt(addr)` 以“调用时刻的当前文件内容/长度”为准；若 `addr` 落在当前 EOF 之外返回 `false`；若随后文件增长，再次调用可变为 `true`。
    - 否则会出现“Reader/Scanner 绑定到打开瞬间快照长度”的错误实现，破坏运行时对新写入 record 的读取能力（尤其 commit 后对新地址的读取）。

---

#### 2. Gap 分析的验证

| Gap 类型 | Claude 的判断 | 我的验证 |
|:--|:--|:--|
| **过度设计** | “跟随视图不需要” | **部分成立**：对 meta 的 `ScanReverse()`（启动时尾扫）而言，上层没有“运行时观察文件增长并持续尾随枚举”的需求；但对 data 的 `TryReadAt()` 而言，必须定义“文件增长期间的随机读行为”（见 §4 关键契约），否则实现会隐式选一种语义。换言之：**不需要“跟随式 ScanReverse”，但需要“增长可见的 TryReadAt”**。 |
| **功能缺失** | “双文件协调由上层负责” | **成立且层级划分合理**：跨文件事务（data→meta 的 durable 顺序）是 StateJournal 的 Layer 1 语义，不应下沉到单文件的 RBF 层。RBF 文件交互接口只需提供“每个文件各自的 DurableFlush/Length/Truncate 等能力”，上层组合即可完成二阶段提交。 |
| **抽象不当** | “Scanner 应是一次性工具” | **不成立（需要拆分语义）**：`ScanReverse()` 是冷路径的一次性工具，但 `TryReadAt()` 是整个生命周期都会用的随机读能力（LoadObject/LazyLoad/版本链回放）。因此 Scanner 作为“长期存在的随机读者 + 提供一次性尾扫能力”的组合是合理的；若要拆分，也应是“接口拆分（RandomReader vs ReverseScanner）”，而不是把 Scanner 整体降格为一次性。 |

补位：**缺口（可判定性）**
- Claude Gap 表里隐含了“我们可以把 D1（可见性）直接删掉”的倾向；但从规范角度看，D1 不能消失，只能被“更小但可判定的承诺”替代：至少要把 `TryReadAt` 在文件增长与 EOF 边界下的行为写成契约。

---

#### 3. 接口调整的规范核查

本节的“兼容性”以现行 RBF SSOT 为准： [atelia/docs/Rbf/rbf-interface.md](atelia/docs/Rbf/rbf-interface.md)。

**(1) 保持一致性（与 rbf-interface.md 条款兼容）**
- ✅ 允许新增“文件交互入口（RbfFile/IRbfFile）”是兼容的：`rbf-interface.md` 目前只定义 `IRbfFramer/IRbfScanner`，并未规定如何从路径获得它们。
- ⚠️ 需要修订/对齐的点：`IRbfFramer.Flush()` remarks 目前写“由上层在其持有的底层句柄上执行 durable flush”。如果新增 `RbfFile.DurableFlush()` 并让上层不再持有句柄，这段文字会变成误导性暗契约，应同步改为“通过文件入口点执行 durable flush”。
- ✅ Claude 的“每个文件独立 DurableFlush，上层协调 data→meta”与 StateJournal `[R-COMMIT-FSYNC-ORDER]` 一致。

**(2) 可判定性（能否写成明确契约）**
- “去掉工厂方法、改属性单例（file.Framer / file.Reader）”本身不自动提升可判定性，反而可能引入新的不可判定面：
    - 需要明确：这些属性是否永远返回同一实例？Dispose 后访问如何？线程安全如何？
    - 若未来要引入“快照 scanner vs 跟随 scanner”，属性单例会把扩展点锁死。
- 更可判定的做法是：保留 `CreateFramer()/CreateScanner(options?)` 作为显式操作，并用条款规定“当前 MVP 的默认策略可以返回单例，但不得承诺永远单例”。

**(3) 前向兼容（未来需求变化是否易扩展）**
- Claude 的“Scanner 拆成 RandomReader + ScanReverse 一次性枚举”在工程上可行，但从规范演进角度：
    - ✅ 可以作为“更细的接口分层”，让上层只依赖 RandomReader（热路径）而把 ScanReverse 留给恢复路径。
    - ⚠️ 但如果直接替换/删改现有 `IRbfScanner`（SSOT 已发布且有测试向量依赖），会造成规范漂移。建议策略是：**保持 `IRbfScanner` 不变**，在文件交互层提供更贴近用例的 facade（例如 `IRbfRandomReader` 仅暴露 TryReadAt），而非反向修改 Layer 0/1 边界契约。

---

#### 4. 关键契约清单（基于真实需求）

以下契约若不显式化，会直接导致实现倒灌/行为不可判定（尤其是 D1 与 durability）：

1) **`TryReadAt(<deleted-place-holder>)` 在文件增长期间的行为**（data file 热路径）
- MUST：以“调用时刻”的文件内容为准。
- MUST：当 `address` 在调用时刻的 EOF 之后，返回 `false`（不得返回半初始化 frame，亦不应静默抛异常作为常态）。
- SHOULD：当 `address` 在 EOF 之内但帧损坏/CRC 失败时，返回 `false` 或抛出明确的“格式损坏异常”——需要在接口层裁决一种主路径（否则调用方无法写测试断言）。
- MUST：`address=0`（Null）处理要可判定（建议返回 `false`）。

2) **`ScanReverse()` 的扫描窗口与并发修改**（meta file 冷路径）
- `rbf-interface.md` 已定义：枚举期间文件被修改行为未定义（`[S-RBF-SCANREVERSE-CONCURRENT-MUTATION]`）。
- 若未来希望允许“写入同时尾扫”，必须新增条款缩小未定义区（例如：扫描窗口固定为调用开始时的 EOF，且仅保证看到调用前已 durable 的帧）。在 MVP 中可以明确禁止或维持未定义，但必须让上层知道“要在稳定快照上调用”。

3) **`DurableFlush()` 的边界与效果**（commit 关键）
- MUST：DurableFlush 只保证“在同一文件上，DurableFlush 调用返回前已完成 flush 的字节”达到 durability（fsync 等价效果）。
- MUST：DurableFlush 与 `IRbfFramer.Flush()` 的关系可判定（Flush≠Durable；Flush 不含 fsync，已由 `[S-RBF-FRAMER-NO-FSYNC]` 定义）。
- MUST：disposed 后 DurableFlush 的行为（建议抛 `ObjectDisposedException`）与异常类型需固定。

4) **双文件 commit 的最小原子性承诺**（StateJournal 真实需求）
- MUST：上层能够分别对 data 与 meta 执行 DurableFlush。
- MUST：上层能够查询 data 文件的当前长度（byte），用于验证 `MetaCommitRecord.DataTail <= data.Length`（撕裂检测）。

5) **恢复期截断（truncate）能力**（StateJournal §3.5）
- MUST：上层能够按 `DataTail` 截断 data 文件尾部垃圾（`[R-DATATAIL-TRUNCATE-GARBAGE]`）。
- MUST：截断后的 <deleted-place-holder>/Ptr64 失效语义要明确（被截断区间内的地址后续 TryReadAt 必须失败）。

6) **多进程/多实例语义（最低限度）**
- MVP 允许维持“未定义/不支持多 writer”，但必须在文件入口层可判定：
    - 例如：CreateFramer 若无法获得写入独占（被其他进程/实例持有 writer），MUST fail-fast（异常类型需固定），而不是静默产生损坏。

---

#### 5. 最小可行接口（MVP）建议

目标：只覆盖 StateJournal 的真实用法（双文件、尾扫、随机读、append-only、两阶段 commit、恢复截断），并保持与 `rbf-interface.md` 的 SSOT 一致。

**保留（必须）**
- 文件入口：`CreateNew(path)` / `OpenExisting(path)`（或同等语义的 `Create`/`Open`，但必须支持 FailIfExists 与“Open 验证 Genesis”）。
- 视图：`IRbfFramer`（append/flush/builder）与 `IRbfScanner`（TryReadAt/ScanReverse）。
- `DurableFlush()`：文件级 durability 控制（供 data→meta 顺序）。
- `LengthBytes`（或 `GetLengthBytes()`）：用于 `DataTail` 校验。
- `Truncate(ulong newLengthBytes)`：用于恢复期按 DataTail 截断 data file。
- 生命周期：`Dispose()` 关闭句柄；Dispose 幂等性与 disposed 后行为必须可判定。

**去掉（MVP 不要承诺）**
- “跟随式 ScanReverse（边写边持续枚举）”语义（可先保持未定义或明确禁止）。
- 多 scanner 实例的强承诺（可以允许创建多个，但不需要承诺；若要简单，可先规定单例或上限，但需写清楚）。
- 暴露 Stream/Backend 注入（监护人已排除）。

**调整（把语义钉死）**
- `TryReadAt` 的 EOF/增长/损坏语义（见 §4-1）。
- `Create`/`Open` 对 0 字节文件、Genesis 缺失、权限错误、损坏等的异常分类（至少要能写黑盒测试）。
- `Flush` 的 durable 边界：`IRbfFramer.Flush()` 仍保持“不含 fsync”，durable 由文件入口提供。

**建议的 MVP 形状（示意，规范应以条款而非示例为 SSOT）**

```csharp
public interface IRbfFile : IDisposable
{
        string Path { get; }

        // 当前 OS 视角的文件长度（byte）。用于 meta.DataTail 校验。
        ulong LengthBytes { get; }

        // 视图
        IRbfFramer CreateFramer();        // MVP 建议：最多一次成功（单 writer）
        IRbfScanner CreateScanner();      // MVP：至少一个；可实现为单例

        // durability
        void DurableFlush();

        // recovery
        void Truncate(ulong newLengthBytes);
}

public static class RbfFile
{
        public static IRbfFile CreateNew(string path);      // FailIfExists
        public static IRbfFile OpenExisting(string path);   // 验证 Genesis
}
```

---

## 监护人--刘世超 第二轮发言
基于以上大家的分析，我有了更清晰的思路，作为对大家已达成共识的补充，请各位同仁分析是否合理可行。
1. 分层。
  A. 可测试的格式与编码组件层，工作在{Stream/ReadonAccess/mmap accessor}上。具体用什么API待进一步分析。
  B. 文件句柄管理层，独占文件、创建/打开/关闭。这部分可以只做极少的单元测试。管理{FileStream/SafeFileHandle/MemoryMappedFile}。

2. façade 模式。
  为常规读写提供傻瓜风格的门面类型。把所有复杂性封装起来，提供一个易用的界面，让最终用户能传入路径就能得到一个门面对象，在门面对象上直接提供从后向前迭代合法帧，以及随机读取一帧(含crc验证)的能力。
  为灾难恢复等高级场景提供另外的ToolKit，设计与实现可延后。

3. 升级<deleted-place-holder>为FramePtr struct。
  内部包含4字节对齐的文件内偏移量和Frame长度，这样读取时一次性就能知道“在哪读，读多少”方便读取路径。为了支持序列化和反序列化，需要能打包成ulong(uint64)和从ulong解包。不支持描述非4B对齐地址，以节约2 bit。我们需要选择bit分配，多少给地址部分，多少给长度部分。我估计单个frame不会需要太长，肯定不需要上GB。

4. 根据读写需求，确定一种常规路径底层API。灾难恢复等高级场景可以有完全独立的工具栈，我们先不管。
  - 要不要缓存？如果要，用标准库自带的缓存还是我们自己写？
  - 配合FramePtr struct，似乎ReadonAccess是最佳选择，但是我们实现代码的单元测试性要怎么做？
  - 用Stream的话，牺牲点性能，是否能大幅简化我们的实现？

不要把我的话当指令，也只是候选技术方案，我们要从软件工程的原理寻找好的设计。
---

### Advisor-Claude 发言（分层与 Façade 架构分析）

> **taskTag**: #design
> **scope**: 架构合理性分析，基于软件工程原理
> **基于**: 监护人第二轮发言中的四点架构提议

---

#### 快速轨道（30 秒扫描）

| 提议 | 评估 | 核心理由 |
|:-----|:-----|:---------|
| **A/B 分层** | ✅ 合理 | 依赖倒置 + 测试性分离 |
| **Façade 模式** | ✅ 合理（需护栏） | 隐藏复杂性，但需防止 God Object |
| **FramePtr struct** | ⚠️ 需权衡 | 读路径收益明确，写路径需调整 |
| **RandomAccess 底层** | ✅ 推荐 | 无状态、线程安全、与 A 层理念匹配 |

---

#### 深度轨道

---

##### 1. 分层合理性分析

**1.1 依赖倒置评估**

```
┌─────────────────────────────────────────────────────────────┐
│                      监护人提议的分层                         │
├─────────────────────────────────────────────────────────────┤
│                                                              │
│    ┌──────────────────────────────────────────────────┐     │
│    │  A层：格式与编码组件                               │     │
│    │  ──────────────────────────────────                │     │
│    │  • 工作在 Stream/RandomAccess/mmap 上             │     │
│    │  • 可注入 → 可测试                                 │     │
│    │  • 纯格式逻辑，无 I/O 决策                         │     │
│    └──────────────────┬─────────────────────────────────┘     │
│                       │ 依赖抽象                              │
│                       ▼                                      │
│    ┌──────────────────────────────────────────────────┐     │
│    │  B层：文件句柄管理                                 │     │
│    │  ──────────────────────────────────                │     │
│    │  • 管理 FileStream/SafeFileHandle/MemoryMappedFile│     │
│    │  • 独占文件、创建/打开/关闭                        │     │
│    │  • 极少单元测试（真实 I/O）                        │     │
│    └──────────────────────────────────────────────────┘     │
│                                                              │
└─────────────────────────────────────────────────────────────┘
```

**评估**：

| 原则 | 符合度 | 分析 |
|:-----|:-------|:-----|
| **依赖倒置 (DIP)** | ✅ 符合 | A层依赖抽象（Stream/RandomAccess），不依赖具体文件实现 |
| **单一职责 (SRP)** | ✅ 符合 | A层管"格式"，B层管"句柄"——职责正交 |
| **接口隔离 (ISP)** | ✅ 符合 | A层不需要知道 B层的独占/生命周期细节 |
| **测试性** | ✅ 符合 | A层可注入 MemoryStream/byte[] 测试；B层薄到可接受不测 |

**边界清晰度**：

| 问题 | 答案 |
|:-----|:-----|
| CRC32C 计算属于哪层？ | A层（格式组件）——与 I/O 无关 |
| Fence/Genesis 写入属于哪层？ | A层（格式）+ B层（写入）——协作完成 |
| Fsync 属于哪层？ | B层——这是文件句柄的能力，非格式逻辑 |
| 文件长度查询属于哪层？ | B层——这是句柄状态，非格式问题 |

**1.2 更好的分层？**

监护人的分层已经很好。唯一可考虑的改进是**再细分 A 层**：

```
A层细分选项（仅供参考，不建议 MVP 采用）：
  A1: 编码原语（VarInt, CRC32C, 4B 对齐）
  A2: 帧格式（Frame Header/Fence 解析）
  A3: 序列操作（逆向扫描、随机读取）
```

**但**：MVP 阶段不建议过度分层。当前 A/B 两层已足够，等需求驱动再细化。

---

##### 2. Façade 模式评估

**2.1 "常规路径" vs "灾难恢复" 分离**

| 维度 | 常规路径 | 灾难恢复 |
|:-----|:---------|:---------|
| **用户** | StateJournal（上层库） | 管理员/开发者 |
| **假设** | 文件格式正确 | 文件可能损坏 |
| **失败策略** | 快速失败（异常） | 尽力恢复（容错） |
| **性能要求** | 热路径需优化 | 冷路径可慢 |
| **API 风格** | 简单（路径→对象） | 复杂（暴露底层细节） |

**评估**：✅ 分离是正确的。

**类比**：
- Git：`git log` vs `git fsck --full`
- 数据库：正常 CRUD vs `DBCC CHECKDB`
- 文件系统：常规读写 vs `fsck`

**设计原则**：常规路径应该"做正确的事很容易，做错误的事很难"（Pit of Success）；灾难恢复可以"暴露危险能力，但需明确警告"。

**2.2 Façade 应暴露什么接口？**

基于 StateJournal 真实需求（见前文需求分析），Façade 的最小接口是：

```csharp
/// <summary>
/// RBF 文件傻瓜门面。隐藏所有复杂性，只暴露"读帧"和"写帧"能力。
/// </summary>
public interface IRbfFile : IDisposable {
    // === 基本信息 ===
    string Path { get; }
    ulong LengthBytes { get; }  // OS 视角的当前文件长度
    
    // === 读取能力（Façade 只暴露这两个） ===
    /// <summary>逆向迭代帧（从尾到头，含 CRC 验证）</summary>
    IEnumerable<RbfFrame> ScanReverse();
    
    /// <summary>随机读取帧（含 CRC 验证）</summary>
    bool TryReadAt(FramePtr ptr, out RbfFrame frame);
    
    // === 写入能力 ===
    /// <summary>追加帧，返回其指针（写入后才知道位置+长度）</summary>
    FramePtr Append(ReadOnlySpan<byte> payload, uint frameTag);
    
    /// <summary>推送缓冲到 OS</summary>
    void Flush();
    
    // === Durability ===
    void DurableFlush();  // fsync
    
    // === 恢复（保留，但标记为 Advanced）===
    void Truncate(ulong newLengthBytes);
}
```

**对比现有 `IRbfFramer`/`IRbfScanner`**：

| 现有设计 | Façade 设计 | 变化 |
|:---------|:------------|:-----|
| 分离的 Framer + Scanner | 合并为单一 IRbfFile | 简化用户心智模型 |
| `IRbfFramer.AppendFrame()` 返回 <deleted-place-holder> | `Append()` 返回 `FramePtr` | 升级地址类型 |
| `IRbfScanner.TryReadAt(<deleted-place-holder>)` | `TryReadAt(FramePtr)` | 读取时已知长度 |
| 无 CRC 验证选项 | 默认验证 CRC | Pit of Success |

**2.3 如何避免 God Object？**

**风险**：Façade 可能逐渐积累职责，变成"什么都做的大泥球"。

**护栏**：

| 策略 | 实现 |
|:-----|:-----|
| **职责边界条款** | Façade 只做"组装 + 委托"，不实现格式逻辑 |
| **方法数量预算** | MVP 不超过 10 个公开方法 |
| **委托结构** | 内部持有 A 层组件，真正逻辑在 A 层 |

```csharp
// 内部结构示意（非公开 API）
internal class RbfFileImpl : IRbfFile {
    private readonly SafeFileHandle _handle;     // B层：句柄管理
    private readonly RbfFrameCodec _codec;       // A层：格式编解码
    private readonly RbfScannerCore _scanner;    // A层：扫描逻辑
    private readonly RbfFramerCore _framer;      // A层：写入逻辑
    
    // Façade 方法只是委托
    public bool TryReadAt(FramePtr ptr, out RbfFrame frame)
        => _scanner.TryReadAt(_handle, ptr, out frame);
}
```

**类比**：Spring 的 `JdbcTemplate`——它是 Façade，但不实现 SQL 解析或连接管理。

---

##### 3. FramePtr struct 评估

**3.1 升级 <deleted-place-holder> 为 FramePtr 的收益**

| 维度 | <deleted-place-holder>（现有） | FramePtr（提议） |
|:-----|:------------------|:-----------------|
| **内容** | 仅偏移量 | 偏移量 + 长度 |
| **读取效率** | 需两次：先读头（获长度）→ 再读体 | 一次：直接读 offset..offset+len |
| **写入流程** | 写入时知道偏移量 | 写入完成后知道偏移量+长度 |
| **序列化** | 8 字节 | 8 字节（打包） |

**收益明确的场景**：
- ✅ **随机读取**（热路径）：一次系统调用 vs 两次
- ✅ **Lazy Load**：FramePtr 自描述，无需额外查找

**需要调整的场景**：
- ⚠️ **写入时获取 FramePtr**：`Append()` 返回的 FramePtr 必须在帧完全写入后才能确定长度

```csharp
// 现有流程：
addr = framer.AppendFrame(...);  // addr 在开始写入时就知道

// 新流程：
ptr = framer.Append(...);  // ptr 必须等帧写完才能返回（长度在写入过程中累积）
```

这不是问题——因为 `Append` 是同步的，写完自然知道长度。

**3.2 Bit 分配策略**

**约束**：
- 总共 64 bit
- 偏移量需要 4B 对齐 → 可节省 2 bit
- 帧长度也可要求 4B 对齐 → 再节省 2 bit

**候选方案**：

| 方案 | 偏移量 bit | 长度 bit | 最大文件 | 最大帧 |
|:-----|:-----------|:---------|:---------|:-------|
| **A. 均衡** | 44 | 20 | 64 TB（实际 ×4 = 256 TB） | 4 MB |
| **B. 偏文件** | 48 | 16 | 1 PB（实际 ×4 = 4 PB） | 256 KB |
| **C. 偏帧** | 40 | 24 | 4 TB（实际 ×4 = 16 TB） | 64 MB |

**建议**：方案 A（均衡）

**理由**：
- 64 TB 文件上限对 StateJournal 足够（实际日志文件很少超过 TB 级）
- 4 MB 帧上限对对象版本记录足够（单个对象状态很少超过 MB 级）
- 如果需要更大帧，说明应该拆分对象

**编码示意**：

```csharp
[StructLayout(LayoutKind.Sequential)]
public readonly struct FramePtr {
    private readonly ulong _packed;
    
    // 高 44 bit：偏移量（4B 对齐，实际值 = 字段值 × 4）
    // 低 20 bit：长度（4B 对齐，实际值 = 字段值 × 4）
    
    public ulong Offset => (_packed >> 20) << 2;   // 还原 4B 对齐
    public uint Length => (uint)((_packed & 0xFFFFF) << 2);
    
    public static FramePtr FromOffsetAndLength(ulong offset, uint length) {
        Debug.Assert(offset % 4 == 0 && length % 4 == 0);
        return new FramePtr((offset >> 2) << 20 | (length >> 2));
    }
}
```

**3.3 对现有规范的影响**

| 规范位置 | 影响 | 处理建议 |
|:---------|:-----|:---------|
| `rbf-interface.md` | <deleted-place-holder> → `FramePtr` | 新增类型定义章节 |
| `IRbfFramer.AppendFrame()` | 返回类型变化 | 修订签名 |
| `IRbfScanner.TryReadAt()` | 参数类型变化 | 修订签名 |
| StateJournal 的 Ptr64 | 需要映射到 FramePtr | 上层决策 |

**迁移策略**：

```csharp
// 兼容层（如果需要）
public static class <deleted-place-holder>Compat {
    public static FramePtr ToFramePtr(this <deleted-place-holder> addr, uint length)
        => FramePtr.FromOffsetAndLength(addr.Value, length);
    
    public static <deleted-place-holder> To<deleted-place-holder>(this FramePtr ptr)
        => new <deleted-place-holder>(ptr.Offset);
}
```

---

##### 4. 底层 API 建议

**4.1 RandomAccess vs Stream Tradeoff**

| 维度 | RandomAccess | Stream |
|:-----|:-------------|:-------|
| **状态性** | 无状态（每次调用传 offset） | 有状态（Position 属性） |
| **线程安全** | 天然线程安全 | 需要锁或每线程实例 |
| **性能** | 略优（无锁开销） | 略差（Position 更新有开销） |
| **API 熟悉度** | .NET 6+ 新 API | 经典 API，广泛熟悉 |
| **可测试性** | 可包装接口注入 | MemoryStream 直接可用 |
| **抽象层级** | 较低（接近 OS） | 较高（Buffer + Position） |

**建议**：✅ **RandomAccess 作为首选**

**理由**：
1. **与 A 层理念匹配**：A 层是"给我偏移量和长度，我处理格式"——RandomAccess 的无状态调用风格天然匹配
2. **简化并发模型**：单一 `SafeFileHandle` 可被多处同时 `RandomAccess.Read/Write`，无需锁
3. **与 FramePtr 配合**：`FramePtr` 自带偏移+长度，正好是 `RandomAccess.Read` 所需

**可测试性保证**：

```csharp
// 方案 1：抽象接口
public interface IRandomAccessFile {
    int Read(long offset, Span<byte> buffer);
    void Write(long offset, ReadOnlySpan<byte> buffer);
}

// 方案 2：直接用 Span<byte>（内存测试）
public class InMemoryRandomAccess : IRandomAccessFile {
    private byte[] _data;
    public int Read(long offset, Span<byte> buffer) {
        _data.AsSpan((int)offset, buffer.Length).CopyTo(buffer);
        return buffer.Length;
    }
}
```

**4.2 缓存策略**

**问题**：是否需要应用层缓存？

**分析**：

| 场景 | OS 页缓存 | 应用层缓存 |
|:-----|:---------|:-----------|
| 顺序写入 | ✅ 高效 | 无收益 |
| 逆向扫描 | ⚠️ 可能低效（反向读取） | 可能有收益 |
| 随机读取 | ✅ 高效（热页缓存） | 无收益（Identity Map 在上层） |

**建议**：✅ **不引入应用层缓存（MVP）**

**理由**：
1. **StateJournal 已有 Identity Map**：对象级缓存在上层，RBF 层不需要重复
2. **逆向扫描是冷路径**：启动时执行一次，不值得优化
3. **复杂度成本高**：缓存引入一致性问题（写入后需失效）、内存管理问题

**例外**：如果性能剖析发现瓶颈，可在 A 层添加"帧头缓存"（只缓存 Header 用于快速跳过）。

**4.3 可测试性保证策略**

**总体策略**：A 层可完全单元测试，B 层做薄集成测试。

| 层 | 测试方式 | 工具 |
|:---|:---------|:-----|
| **A层（格式组件）** | 单元测试 | `byte[]` + `MemoryStream` + Mock `IRandomAccessFile` |
| **B层（句柄管理）** | 集成测试 | 临时文件 + 真实 `SafeFileHandle` |
| **Façade** | 端到端测试 | 临时文件 + 完整流程 |

**A 层测试示例**：

```csharp
[Fact]
public void RbfFrameCodec_Decode_ValidFrame_ReturnsPayload() {
    // Arrange: 内存中构造一个合法帧
    var frameBytes = BuildValidFrame(payload: "Hello"u8, tag: 0x00010001);
    var reader = new InMemoryRandomAccess(frameBytes);
    var codec = new RbfFrameCodec();
    
    // Act
    var result = codec.TryDecode(reader, offset: 0, out var frame);
    
    // Assert
    Assert.True(result);
    Assert.Equal("Hello"u8.ToArray(), frame.Payload.ToArray());
}
```

---

##### 5. 架构建议总结

**5.1 推荐架构**

```
┌─────────────────────────────────────────────────────────────┐
│                      StateJournal (上层)                     │
│                        使用 IRbfFile                         │
└───────────────────────────┬─────────────────────────────────┘
                            │
┌───────────────────────────▼─────────────────────────────────┐
│                      Façade Layer                            │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  IRbfFile (公开接口)                                    │ │
│  │  ─────────────────────────                              │ │
│  │  • CreateNew(path) / OpenExisting(path)                 │ │
│  │  • ScanReverse() / TryReadAt(FramePtr)                  │ │
│  │  • Append(payload, tag) → FramePtr                      │ │
│  │  • Flush() / DurableFlush() / Truncate()                │ │
│  └────────────────────────────────────────────────────────┘ │
└───────────────────────────┬─────────────────────────────────┘
                            │ 委托
┌───────────────────────────▼─────────────────────────────────┐
│                      A 层：格式组件                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────┐   │
│  │ RbfFrameCodec│  │RbfScannerCore│  │ RbfFramerCore    │   │
│  │  (编解码)     │  │ (扫描逻辑)   │  │  (写入逻辑)       │   │
│  └──────────────┘  └──────────────┘  └──────────────────┘   │
│                            │ 依赖                            │
│                            ▼                                │
│  ┌─────────────────────────────────────────────────────────┐│
│  │  IRandomAccessFile (抽象接口)                           ││
│  │  • Read(offset, buffer)                                  ││
│  │  • Write(offset, buffer)                                 ││
│  └─────────────────────────────────────────────────────────┘│
└───────────────────────────┬─────────────────────────────────┘
                            │ 实现
┌───────────────────────────▼─────────────────────────────────┐
│                      B 层：句柄管理                          │
│  ┌─────────────────────────────────────────────────────────┐│
│  │  RbfFileHandle                                          ││
│  │  • 持有 SafeFileHandle                                   ││
│  │  • 实现 IRandomAccessFile（委托到 RandomAccess API）      ││
│  │  • 提供 DurableFlush (FlushFileBuffers)                  ││
│  │  • 管理生命周期 (Dispose)                                 ││
│  └─────────────────────────────────────────────────────────┘│
└─────────────────────────────────────────────────────────────┘
```

**5.2 关键决策清单**

| 决策 | 结论 | 理由 |
|:-----|:-----|:-----|
| 分层方式 | A（格式）+ B（句柄） | 依赖倒置 + 测试性 |
| Façade 形状 | 单一 `IRbfFile` | 用户心智模型简单 |
| 常规 vs 恢复分离 | ✅ 分离 | 职责不同、用户不同 |
| 底层 API | RandomAccess | 无状态、与 FramePtr 配合 |
| 缓存 | 不引入（MVP） | 上层已有 Identity Map |
| FramePtr bit 分配 | 44:20（偏移:长度） | 均衡，64TB/4MB 上限 |

**5.3 规范影响预估**

| 文档 | 需要的工作 |
|:-----|:-----------|
| `rbf-interface.md` | 新增 §5 文件交互接口；新增 FramePtr 类型定义 |
| `rbf-format.md` | 无需修改（线格式不变） |
| 新增文档 | 可选：`rbf-facade.md`（Façade 使用指南） |

**5.4 待进一步裁决**

| 问题 | 候选 | 需要裁决 |
|:-----|:-----|:---------|
| FramePtr 的 Null 表示 | `0` vs 专用字段 | 监护人 |
| Truncate 是否放在 Façade | Façade vs 独立 RecoveryKit | 监护人 |
| IRandomAccessFile 命名 | `IRbfStorage` vs `IRandomAccessFile` | 团队 |

---

> **核心洞见**：监护人的架构方向从软件工程原理角度是合理的。A/B 分层实现依赖倒置、Façade 模式隐藏复杂性、FramePtr 优化读取路径、RandomAccess 匹配无状态设计——这些选择相互协调，形成一个自洽的架构。
>
> 关键风险点是"Façade 膨胀"——需要用职责边界条款约束其增长。

---

### Advisor-GPT 发言（FramePtr 规范核查）

#### 1. 与现有规范的冲突检查

**SSOT 现状**（以 atelia/docs/Rbf/rbf-interface.md v0.17 为准）：
- 当前规范定义的指针类型是 `<deleted-place-holder>(ulong Value)`，其语义为“指向 Frame 起始位置（HeadLen 字段起点）的文件偏移量”。相关条款为 `[F-ADDRESS64-DEFINITION]` / `[F-ADDRESS64-ALIGNMENT]` / `[F-ADDRESS64-NULL]`。
- `IRbfScanner.TryReadAt(<deleted-place-holder>)` 的签名在 `[A-RBF-SCANNER-INTERFACE]` 明确。
- `IRbfFramer.Append(...)` 与 `RbfFrameBuilder.Commit()` 的返回类型也都是 <deleted-place-holder>（`[A-RBF-FRAMER-INTERFACE]` / `[A-RBF-FRAME-BUILDER]`）。

**冲突点（必须裁决）**：
1) **类型替换的 Breaking 面**
- 若“升级 <deleted-place-holder> 为 FramePtr struct”意味着 **完全替换**，则 rbf-interface.md 中上述 3 处 API 签名均为 breaking，且 rbf-test-vectors.md 中“<deleted-place-holder>/Ptr64（u64 LE 文件偏移，4B 对齐）”的叙述也需同步更名/重写。
- 若希望最小化破坏，另一个可行路线是：保留 <deleted-place-holder>（offset-only）不变，新增 `FramePtr`，并在 `IRbfScanner` 上新增重载 `TryReadAt(FramePtr)` 作为 fast-path；但这与“升级”一词的语义不一致，需要在规范里明确“<deleted-place-holder> 退为兼容类型/别名”。

2) **条款 ID 漂移风险**
- 任务描述提到的 `[A-RBF-ADDRESS64-OFFSET]` 在当前 SSOT 中不存在（现为 `[F-ADDRESS64-*]` 系列）。若该 ID 来自旧草案，必须在 Clause Registry 或文档内做“旧 ID → 新 ID”的显式映射，否则核查与实现无法对齐。

3) **FramePtr.length 的“长度到底指什么”必须钉死**
- 现行 <deleted-place-holder> 只定义 offset，因此读路径会先读 HeadLen 再知道 frame 总长度。
- FramePtr 的目标是“一次知道在哪读，读多少”，因此 `length` 字段必须明确是：
    - A) `HeadLen/TailLen`（整个 FrameBytes 的总长度，不含 Fence）——最契合“一次读完整帧”；或
    - B) 仅 payload 长度——会导致仍需额外读取 header/status/tail/crc（与目标冲突）。
    建议：选 A，并在规范中把 length 命名为 `FrameBytesLength`（或等价名）以减少歧义。

4) **对齐约束的一致性**
- 现行规范已要求 <deleted-place-holder> 4B 对齐（`[F-ADDRESS64-ALIGNMENT]`）。FramePtr 提议“不支持非 4B 对齐地址”与之不冲突，属于“把既有约束内化到类型编码”。
- 但 FramePtr 的 `length` 是否也要求 4B 对齐，需要裁决；若 `length` 定义为 FrameBytes 总长度，则按 RBF wire format 计算，FrameBytes 总长度天然 4B 对齐（见 rbf-test-vectors.md 对 StatusLen 的对齐公式），因此可将其作为 MUST（可判定）。

#### 2. 必须规范化的契约

FramePtr 引入的“新契约面”至少包括：

1) **打包/解包可逆性（域内可逆）**
- 需要一个可判定的域定义：
    - `offsetBytes % 4 == 0` 且 `offsetBytes != 0`（除非允许 Null）
    - `frameBytesLength % 4 == 0` 且 `frameBytesLength >= MinFrameBytes`（最小值建议绑定 wire format 的最小 frame 长度，例如 20 bytes）
    - `offsetBytes + frameBytesLength <= fileLengthBytes` 的“可解引用性”属于运行时条件（TryReadAt 的返回语义），不属于构造期条件。
- 契约必须写成：对任意满足域约束的 `(offsetBytes, frameBytesLength)`，`Pack` 与 `Unpack` MUST 互为逆运算。

2) **4B 对齐约束的强制性进入可判定面**
- 不能只在文字里说“4B 对齐”，需要规定：
    - `FramePtr.CreateChecked/FromOffsetAndLength` 对非对齐输入 MUST 抛 `ArgumentOutOfRangeException`（或提供 `TryCreate` 返回 false，二选一并固定）。
    - `FromPacked(ulong)` 解包时若发现不满足对齐/保留模式，MUST fail-fast（抛异常或 TryParse 返回 false）。

3) **长度字段的有效范围（最小/最大）**
- 需要明确 `FramePtr.MaxFrameBytesLength`（由 bit 分配决定）与 `FramePtr.MinFrameBytesLength`（由 wire format 决定）。
- 需要明确 `length == 0` 的语义：建议保留为 Null（与 <deleted-place-holder>.Null 一致），并规定非 Null 时 length MUST > 0。

4) **超出范围的错误处理（构造期 vs 读写期）**
- 构造期（pack/unpack）：越界/不合法 MUST fail-fast（异常类型或 TryXxx 语义固定）。
- 读期（TryReadAt）：
    - `ptr.IsNull` 或 `ptr.OffsetBytes >= EOF`：建议返回 `false`（保持 TryXxx 语义），而非异常。
    - `ptr.FrameBytesLength` 与实际 headlen 不一致：这是“指针与文件内容不一致”，建议归入“格式损坏/索引损坏”并明确行为（返回 false vs 抛 InvalidDataException，需裁决）。

补位：失败语义缺口
- 目前 rbf-interface.md 对 `TryReadAt` 的失败分类仍标为“待实现时确认”（§9 错误处理）。FramePtr 把“长度一致性检查”拉到了接口边界，导致失败分类更不能再悬空，否则实现会倒灌。

#### 3. Bit 分配的可判定性

**44:20（offset:length，以 4B 为单位）可判定结论**：
- 偏移 44 bit：最大文件寻址 $2^{44}\times 4 = 2^{46}$ bytes = 64 TiB。
- 长度 20 bit：最大帧长 $2^{20}\times 4 = 2^{22}$ bytes = 4 MiB。

**是否满足 StateJournal 实际需求（以当前公开需求为证据的裁决）**：
- 文件上限 64 TiB：对 StateJournal 的 append-only 日志而言现实上充足；如果未来需要更大文件，通常更倾向“分片/分文件（epoch/segment）”而非单文件无限增长。
- 单帧上限 4 MiB：在“对象版本记录 + VersionIndex overlay”的建模下通常足够，但这属于 Layer 1 负载大小的隐含约束，必须上升为可判定条款，否则未来出现 5 MiB payload 时行为不可判定。

**超出范围时必须明确的行为**：
- 写入侧：当上层请求写入的 frame（wire-format 总长度）超过 `FramePtr.MaxFrameBytesLength` 时，`IRbfFramer.Append/Commit` MUST 失败（建议 `ArgumentOutOfRangeException`），不得截断，不得 silent wrap。
- 读取侧：当解包得到的 FramePtr 长度超出上限或小于最小值时：
    - `FramePtr.FromPacked` MUST fail-fast（建议 `ArgumentOutOfRangeException` 或 `FormatException`）；
    - `TryReadAt` 收到“非法 ptr”（若 API 允许）时的行为也必须固定（建议返回 false，或抛参数异常，二选一）。

**是否需要预留扩展位（扩展性核查）**：
- 44:20 用满 64 bit，本身没有“显式版本位”。
- 若要支持向后兼容与未来扩展，建议考虑预留 1 个“编码版本/种类 bit”（例如 MSB）：
    - `0` = legacy <deleted-place-holder>-only packed（length 隐含未知，读时需要二次读取 header），
    - `1` = FramePtr v1 packed（offset+length）。
    这会减少可用 offset bit（例如 43:20 或 43:19 等），但可换来“无需外部版本字段也能判别”的可判定性。

补位：可判定性缺口
- “是否需要预留扩展位”不是偏好问题，而是“未来兼容是否可机械判别”的问题：若没有版本位，则必须依赖外部版本字段（见 §4），否则单凭一个 `ulong` 无法判别它是 <deleted-place-holder> 还是 FramePtr。

#### 4. 向后兼容性

**先明确边界**：
- <deleted-place-holder>/FramePtr 是接口层类型；“已序列化的 <deleted-place-holder> 数据”通常位于 Layer 1（StateJournal 的 VersionIndex/Meta 记录等 payload 中）。因此向后兼容责任原则上在 StateJournal 的 wire format/记录版本上，而不是 RBF Layer 0。

**必须补齐的兼容契约问题**：
1) **旧数据的 `ulong` 到底表示什么？**
- 若旧记录存的是 `<deleted-place-holder>.Value`（offset-only），新实现要么：
    - A) 在读取时允许“length 未知”并做二次读取（先读 header 得 headlen），从而兼容旧数据；或
    - B) 直接拒绝打开旧格式（breaking），要求离线迁移。
    这必须二选一。

2) **是否需要版本标识？**
- 若选择 A（兼容旧数据），建议引入版本标识的最小方式之一：
    - 在 Layer 1（StateJournal）记录中加入显式版本号/字段标记（推荐）；或
    - 在 64-bit packed 值内部预留版本位（见 §3）；
    - 不建议依赖“经验式判别”（例如根据 length 是否为 0 推断），除非该判别规则被写成 MUST 并覆盖全部历史值域。

3) **迁移路径（可执行）**
- 路线 A（软兼容）：新版本仍能读取旧 <deleted-place-holder>；当遇到旧指针时按 offset 去读 header 获取 length，然后在内存中升级为 FramePtr（但写回仍是旧格式或在下一次 compact/rewriter 时升级）。
- 路线 B（硬迁移）：引入 StateJournal 文件格式版本 V2，要求首次打开时离线迁移（重写 VersionIndex/Meta 指针字段为 FramePtr packed），旧版本仅通过迁移工具支持。
- 路线 C（双写一段时间）：新写入使用 FramePtr，旧字段仍保留 <deleted-place-holder>（冗余）以支持旧 reader；该路线复杂且容易产生漂移，除非有明确的跨版本共存需求，否则不建议。

#### 5. 关键条款草案（3-5 个）

**`[F-FRAMEPTR-DEFINITION]`（术语）**
> FramePtr 是一个可打包为 `ulong` 的帧指针，包含 `OffsetBytes` 与 `FrameBytesLength`。`OffsetBytes` 指向该帧的 `HeadLen` 字段起点；`FrameBytesLength` 表示该帧的 FrameBytes 总长度（不含 Fence）。

**`[F-FRAMEPTR-ALIGNMENT]`（格式）**
> 对任意非 Null FramePtr：`OffsetBytes % 4 == 0` 且 `FrameBytesLength % 4 == 0` MUST 成立。`OffsetBytes == 0` 或 `FrameBytesLength == 0` MUST 表示 Null。

**`[S-FRAMEPTR-PACK-REVERSIBLE]`（语义）**
> 对任意满足 `[F-FRAMEPTR-ALIGNMENT]` 且值域在 bit 分配上限内的 `(OffsetBytes, FrameBytesLength)`，`Pack()` 与 `Unpack()` MUST 互为逆运算。对不满足值域/对齐/保留模式的输入，`Unpack` MUST fail-fast（错误类型需固定）。

**`[S-RBF-SCANNER-TRYREADAT-FRAMEPTR]`（语义）**
> `IRbfScanner.TryReadAt(FramePtr ptr, out RbfFrame frame)` MUST 在 `ptr` 可解引用且 framing/CRC 校验通过时返回 `true` 并产出该帧；当 `ptr` 为 Null 或落在调用时刻 EOF 之外时 MUST 返回 `false` 且不得抛异常。若 `ptr.FrameBytesLength` 与文件中该帧的 headlen 不一致，行为 MUST 归类为“格式/索引损坏”并按规范固定处理（返回 false 或抛 `InvalidDataException`，二选一）。

**`[S-RBF-FRAMEPTR-LENGTH-LIMIT]`（语义）**
> 当待写入或待读取的帧（FrameBytes 总长度）超过 `FramePtr.MaxFrameBytesLength` 时，写入侧 MUST 失败（建议 `ArgumentOutOfRangeException`）；读取侧若遇到无法表示的帧长度 MUST 失败并归类为“损坏或不支持”（错误类型需固定）。


<EditAnchor 监护人第二轮发言结束>
---

### Advisor-DeepSeek 发言（可测试性与 DX 分析）

> **基于**：监护人第二轮发言的四点架构提议（A/B 分层、Façade、FramePtr、底层 API）
> **重点**：从开发者体验与可测试性角度评估技术选择

---

#### 快速轨道（Preview）

| 维度 | 核心发现 | 推荐 |
|:-----|:---------|:-----|
| **可测试性** | A/B 分层天然支持测试隔离 | ✅ 推荐分层 |
| **DX** | RandomAccess 直观但需要"心智模型转换" | ✅ 推荐,需提供指南 |
| **缓存** | MVP 不需要,避免测试复杂度 | ✅ 推荐不引入 |
| **FramePtr** | 读路径优秀,需防止"过度打包"心智负担 | ✅ 推荐,需文档 |

---

(实际发言内容较长，已通过编辑追加到会议文件)


### Advisor-GPT 发言（FramePtr 规范核查）

#### 1. 与现有规范的冲突检查

**SSOT 现状（`atelia/docs/Rbf/rbf-interface.md` v0.17）**：
- 指针类型是 `<deleted-place-holder>(ulong Value)`，条款为：`[F-ADDRESS64-DEFINITION]` / `[F-ADDRESS64-ALIGNMENT]` / `[F-ADDRESS64-NULL]`。
- `IRbfScanner.TryReadAt(<deleted-place-holder> address, out RbfFrame frame)` 在 `[A-RBF-SCANNER-INTERFACE]` 中固化。
- 写入侧 `IRbfFramer.Append(...)` / `RbfFrameBuilder.Commit()` 的返回类型都是 <deleted-place-holder>（`[A-RBF-FRAMER-INTERFACE]` / `[A-RBF-FRAME-BUILDER]`）。

**冲突/改动面（必须裁决“升级”的含义）**：
1) **若 FramePtr = 完全替换 <deleted-place-holder>（Breaking）**：
     - 需要同步修改：`IRbfScanner.TryReadAt(...)`、`IRbfFramer.Append(...)`、`RbfFrameBuilder.Commit()`、`RbfFrame.Address` 类型。
     - 需要处理条款迁移：`[F-ADDRESS64-*]` 是否废弃？是否保留为兼容术语？（否则 Clause Registry 会出现“同一概念多权威定义”的风险）
2) **若 FramePtr = 新增（与 <deleted-place-holder> 并存）**：
     - `rbf-interface.md` 的“指针概念”会分裂为 offset-only 与 offset+length 两种，需要明确：何时必须用 FramePtr，何时允许 <deleted-place-holder>。
     - `TryReadAt` 是否要提供双签名（`TryReadAt(<deleted-place-holder>)` + `TryReadAt(FramePtr)`）？若不提供，FramePtr 的价值会被限制在上层私有索引结构，而不是接口契约。

**签名是否必须改**（针对你问的 `IRbfScanner.TryReadAt(<deleted-place-holder>)`）：
- 如果目标是“读路径一次知道在哪读、读多少（减少一次 header 读取）”，那么**仅靠 <deleted-place-holder> 无法达成**，因此需要：
    - A) 直接把签名改成 `TryReadAt(FramePtr ptr, ...)`（Breaking），或
    - B) 增加一个 `TryReadAt(FramePtr ptr, ...)` fast-path，同时保留 <deleted-place-holder> 兼容（更平滑，但规范更复杂）。

**现有条款影响**：
- `[F-ADDRESS64-ALIGNMENT]` 的“4B 对齐”与 FramePtr 的“offset 不支持非 4B 对齐”不冲突；但 FramePtr 还引入“length 的对齐/范围”这一组新增约束。
- 任务描述里的 `[A-RBF-ADDRESS64-OFFSET]` 在 v0.17 SSOT 中不存在：这属于**条款 ID 漂移/引用失效**风险，必须在规范侧补一个“旧 ID → 新 ID”的映射或更正引用来源。

#### 2. 必须规范化的契约

FramePtr 会把“原本可在实现细节里偷懒的行为”推到接口边界，以下契约必须规范化为可判定条款（否则实现会倒灌）：

1) **Pack/Unpack 可逆性（域内可逆）**
- 必须定义“可逆域”（valid domain）：
    - `OffsetBytes % 4 == 0`（非 Null 时）；
    - `FrameBytesLength % 4 == 0` 且 `FrameBytesLength > 0`（非 Null 时）；
    - `OffsetBytes` 与 `FrameBytesLength` 需在 bit 分配可表示范围内。
- 规范必须要求：对任意落在 valid domain 的值对，`Pack(Unpack(x)) == x` 且 `Unpack(Pack(y)) == y`。

2) **4B 对齐约束的强制性**
- 不能只写“要求对齐”，必须明确：
    - 构造/转换 API（如 `FramePtr.FromOffsetAndLength` 或 `TryCreate`）对非对齐输入如何处理（抛异常 vs 返回 false，二选一并固定）。
    - 从 `ulong` 解包遇到“非法对齐/保留模式”的处理（fail-fast 异常类型或 TryParse 语义，二选一并固定）。

3) **长度字段语义必须唯一**
- 必须钉死 length 表示的单位与范围：建议命名为 `FrameBytesLength` 并定义为“FrameBytes 总长度（不含 Genesis/Fence）”。
- 必须明确 Null 判定：建议 `packed == 0` 或 `(OffsetBytes==0 && FrameBytesLength==0)` 为唯一 Null；其他“半空值”（offset=0,length>0 或 offset>0,length=0）建议归为 invalid（Unpack fail-fast）。

4) **越界/超范围的错误处理分层**
- 构造/打包层（纯值语义）：越界/不合法 MUST fail-fast（异常类型需固定或 TryXxx 返回 false）。
- I/O 层（TryReadAt）：
    - Null 或 EOF 外：MUST 返回 `false` 且不得抛异常（保持 TryXxx 语义的一致性）。
    - “ptr 可解引用但与文件内容不一致”（length mismatch / CRC fail / framing invalid）：必须归类为“损坏/不一致”并固定处理策略（返回 false vs 抛 `InvalidDataException`，需裁决；不要留在“待实现时确认”，否则不可判定）。

#### 3. Bit 分配的可判定性

**Claude 建议 44:20（offset:length，且均以 4B 单位编码）是可机械判定的**：
- 最大可表示文件偏移：$2^{44}\times 4$ bytes = $2^{46}$ bytes = 64 TiB。
- 最大可表示帧长：$2^{20}\times 4$ bytes = $2^{22}$ bytes = 4 MiB。

**是否满足 StateJournal 需求（以当前会议材料的“需求分析结论”为证据）**：
- 64 TiB 单文件上限：对“append-only + 长期句柄”的 StateJournal 通常足够；若未来超出，更可能采用“分段/多文件”而非无限单文件。
- 4 MiB 单帧上限：大概率够用，但它属于 Layer 1 负载大小的硬上限，必须上升为条款（否则出现 5 MiB 对象版本时，行为不可判定：是拒绝写入？拆分？截断？）。

**超出范围时的行为必须明确（可判定点）**：
- **写入侧**：当待写入帧（FrameBytes 总长度）> `FramePtr.MaxFrameBytesLength` 时，`Append/Commit` MUST 失败（建议 `ArgumentOutOfRangeException`），不得 silent wrap/截断。
- **读取侧**：
    - 若 `Unpack` 得到的 length 超出 Max 或小于 Min（由 wire format 最小帧长度决定），`Unpack` MUST fail-fast；
    - `TryReadAt` 收到非法 FramePtr 时的行为也必须固定（参数异常 vs 返回 false，二选一）。

**是否需要预留扩展位（版本位）**：
- 44:20 用满 64 bit，单凭一个 `ulong` 将无法区分“它是旧 <deleted-place-holder> packed 还是新 FramePtr packed”，除非依赖外部版本字段。
- 若系统存在“历史数据里存了 u64 指针值”的场景（StateJournal 的索引/记录），建议至少满足一个判别条件：
    - A) 在 Layer 1（StateJournal 记录格式）引入显式版本号/字段标记（推荐）；或
    - B) 在 packed 值里预留 1 bit 作为 encoding version/kind（会牺牲寻址上限但换来自描述可判定性）。

#### 4. 向后兼容性

**先定责任边界**：
- RBF Layer 0 的 `FramePtr/<deleted-place-holder>` 是“接口层类型”；“已序列化的 <deleted-place-holder> 数据”通常位于 Layer 1（StateJournal 的 VersionIndex / CommitRecord payload 等）。因此兼容策略必须在 StateJournal 的 wire format 侧可判定，RBF 侧只需要提供足够的读取能力。

**必须裁决的两条路线（不要隐式）**：
1) **软兼容（推荐默认）**：
     - StateJournal 旧数据里的 `ulong` 仍按 <deleted-place-holder> offset-only 解读；读取时需要“先读 header 获取 headlen，再读完整帧”的慢路径。
     - 新数据写入可使用 FramePtr；当加载旧指针时，可在内存中升级为 FramePtr（但写回是否升级由 StateJournal 的版本策略决定）。
2) **硬迁移（可接受但必须提供工具）**：
     - 引入 StateJournal 文件格式新版本（例如 V2），旧格式不再支持直接打开；提供迁移工具把旧 offset-only 指针重写为 FramePtr packed。

**是否需要版本标识**：
- 如果历史上任何地方把指针保存为“裸 u64”，那么必须有版本标识（外部字段或内部版本位），否则 reader 无法机械判别该 u64 的语义。

#### 5. 关键条款草案（3-5 个）

**`[F-FRAMEPTR-DEFINITION]`（术语）**
> FramePtr 是一个可打包为 `ulong` 的帧指针，包含 `OffsetBytes` 与 `FrameBytesLength`。`OffsetBytes` 指向该帧的 `HeadLen` 字段起点；`FrameBytesLength` 表示该帧的 FrameBytes 总长度（不含 Genesis/Fence）。

**`[F-FRAMEPTR-ALIGNMENT]`（格式）**
> 对任意非 Null FramePtr：`OffsetBytes % 4 == 0` 且 `FrameBytesLength % 4 == 0` MUST 成立。`packed == 0`（等价于 `OffsetBytes == 0 && FrameBytesLength == 0`）MUST 表示 Null；除 Null 外任何 `OffsetBytes == 0` 或 `FrameBytesLength == 0` MUST 视为 invalid。

**`[S-FRAMEPTR-PACK-REVERSIBLE]`（语义）**
> 对任意满足 `[F-FRAMEPTR-ALIGNMENT]` 且值域在 bit 分配上限内的 `(OffsetBytes, FrameBytesLength)`，`Pack()` 与 `Unpack()` MUST 互为逆运算。对不满足对齐/值域/保留模式的 packed 值，`Unpack` MUST fail-fast（异常类型需固定，或提供 `TryUnpack` 返回 `false`，二选一）。

**`[S-RBF-SCANNER-TRYREADAT-FRAMEPTR]`（语义）**
> `IRbfScanner.TryReadAt(FramePtr ptr, out RbfFrame frame)` MUST 在 `ptr` 可解引用且 framing/CRC 校验通过时返回 `true` 并产出该帧；当 `ptr` 为 Null 或落在调用时刻 EOF 之外时 MUST 返回 `false` 且不得抛异常。若 `ptr.FrameBytesLength` 与文件中该帧的 `HeadLen` 不一致，行为 MUST 归类为“索引/格式不一致”并按规范固定处理（返回 `false` 或抛 `InvalidDataException`，二选一）。

**`[S-RBF-FRAMEPTR-LENGTH-LIMIT]`（语义）**
> 当待写入或待读取的帧（FrameBytes 总长度）超过 `FramePtr.MaxFrameBytesLength` 时，写入侧 MUST 失败（建议 `ArgumentOutOfRangeException`）；读取侧若遇到无法表示的帧长度 MUST 失败并归类为“损坏或不支持”（错误类型需固定）。

