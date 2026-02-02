# RBF 写入路径架构审阅（对象生命周期）

> 日期：2026-02-01
> 范围：`RbfFrameBuilder` → `SinkReservableWriter` → `IByteSink`（含 `RbfFileImpl.BeginAppend()` 调用点）

---

## 1. 当前架构概述（简要）

**调用/持有关系（按所有权）**

- `RbfFileImpl`（Facade）持有：`SafeFileHandle _handle`、`long _tailOffset`、`bool _hasActiveBuilder`
- `RbfFileImpl.BeginAppend()` 每次创建：`RbfFrameBuilder(handle, tailOffset, onCommitCallback, clearBuilderFlag)`
- `RbfFrameBuilder` 内部创建并持有：
  - `RandomAccessByteSink`（`SafeFileHandle` + `_writeOffset` 记账，`Push()` 直接 `RandomAccess.Write()`）
  - `SinkReservableWriter`（基于 `ArrayPool<byte>` 的 chunk 缓冲 + reservation 追踪；`Commit()` 触发同步 flush 到 `IByteSink`）

**关键语义点（写入路径为何这样拼）**

- `RbfFrameBuilder` 在构造时先 `ReserveSpan(HeadLenSize)`，让“最早的 pending reservation”永远是 HeadLen。
- `SinkReservableWriter.FlushCommittedData()` 只能 flush 到“最早 pending reservation 的起点”，因此在 HeadLen 未提交前：
  - 任何 `Advance()`/`Commit()` 都不会把数据写入磁盘（flush 被 HeadLen 阻塞）
  - `Dispose()` 的 Auto-Abort 可以通过 `_writer.Reset()` 做到 *逻辑取消 + 归还 ArrayPool*（实现注释称 Zero I/O，成立的前提是从未执行过 headlen 的 `Commit()`）
- `EndAppend()` 时一次性：补 padding → 计算 payload CRC（扫 buffer，不分配）→ 写入尾部（payloadCrc + trailer + fence）→ 回填 HeadLen → `Commit(headLenToken)` 触发一次性 flush

---

## 2. 发现的问题/改进点（按优先级）

### P0（高优先级：ROI 高，且与“每帧分配/生命周期”直接相关）

1) **“每帧写入”的实际堆对象比当前清单更多：BeginAppend 的 lambda/闭包是隐形固定成本**
- 证据：`RbfFileImpl.BeginAppend()` 传入了两段捕获 `_tailOffset`/`_hasActiveBuilder` 的 lambda。
- 影响：即使未来池化 `RbfFrameBuilder/SinkReservableWriter/RandomAccessByteSink`，每次 BeginAppend 仍会发生额外分配（closure 或 delegate），削弱池化收益。
- 评价：这是“生命周期设计”层面最容易被忽视、但对高频 append 影响非常实在的点。

2) **`SinkReservableWriter` 的构造热路径包含“options 分配 + Clone 分配”，与“每帧 new Writer”的模型冲突**
- 证据：`SinkReservableWriter(IByteSink, ArrayPool<byte>?)` 先 `new ChunkedReservableWriterOptions`，进入主构造又 `Clone()` 再 new 一次。
- 影响：在“每帧创建 Writer”的模式下，options 两次对象分配属于纯管理开销。
- 评价：如果目标是降低堆分配，这块是 P0；比 `ChunkSizingStrategy` 的 class→struct 更值。

3) **`SinkReservableWriter` 与 `ChunkedReservableWriter` 存在大量逻辑重复，生命周期策略一旦演进容易出现“分叉漂移”**
- 证据：两者都实现 `IReservableBufferWriter`，都有 chunk 管理 / reservation / flush / recycle 等同构逻辑。
- 影响：未来改动（例如：更复杂的 reservation 规则、诊断、边界修复）需要双线维护；bugfix 漏同步会引入难定位的不一致。
- 评价：这是架构层面的长期维护风险；是否重构取决于后续演进频率，但至少应在报告中标红。

### P1（中高优先级：可优化，但需要权衡复杂度/收益）

4) **`ReservationTracker` 对“常见的小 reservation 数量”不友好：首个 reservation 就会初始化 Dictionary + LinkedList**
- 证据：`ReservationTracker.Add()` 内部 `EnsureInitialized()` 直接 new 两个容器；而 Builder 在构造里必然 Reserve HeadLen。
- 影响：即使 frame 很小、reservation 很少，也会付出 2 个堆对象；并且每次 `ReserveSpan()` 还会 new `ReservationEntry`。
- 评价：这是典型的“为最坏情况付费”的通用实现；如果写入路径的常见负载是“1~2 个 reservation”，可用 small-vector 优化。

5) **`RandomAccessByteSink` 的职责单一且合理，但其“每帧 new”的生命周期目前只是实现便利，不是语义必需**
- 证据：它仅持有 `_file` 与 `_writeOffset`。
- 影响：每帧一次小对象分配；不大，但在高频小帧写入下会积少成多。
- 评价：如果走“按 file 实例复用 builder”的方向，`RandomAccessByteSink` 可以天然变成“与 file 绑定”的常驻对象，仅 Reset offset。

### P2（中优先级：更多是接口/抽象成本与 DX 的取舍）

6) **`IReservableBufferWriter` 缺少 “TryGetReservationSpan” 会让 codec 侧难以写出无歧义的回填代码**
- 现状：`SinkReservableWriter` 自己提供了 `TryGetReservationSpan()`，但接口层没暴露。
- 影响：上层如果只拿到 `IReservableBufferWriter`，要么必须保存原始 span（容易被调用顺序限制困扰），要么只能向下转型，破坏抽象。
- 评价：这不是性能点，但属于“接口抽象代价”：抽象如果不覆盖真实用法，最终会以转型/泄漏实现细节的方式付费。

7) **“Builder 模式”当前更像“受控会话对象”，但 API 语义仍以 IDisposable 作为主要终止手段**
- 现状：`Dispose()` = Auto-Abort；`EndAppend()` = Commit。
- 影响：生命周期语义依赖调用者遵守（虽然有 `_hasActiveBuilder` 防止重入）。
- 评价：不是必须改，但如果未来要池化/复用，更推荐把 API 命名/形态显式化成 `Commit/Abort`（Dispose 仅做资源兜底）。

---

## 3. 推荐的优化方案（含权衡分析）

下面方案按“收益/复杂度”从优到次排序；并且刻意把“能在不引入全局池的情况下复用”放在前面（因为 `RbfFileImpl` 已经保证同一时刻只有一个 builder）。

### 方案 A（推荐）：**按 `RbfFileImpl` 复用单个 Builder（不是 ObjectPool）**

**核心思想**：`RbfFileImpl` 本来就禁止并发 builder，因此最简复用策略是“每个 file 常驻一个 builder 实例”，每次 BeginAppend 只 Reset 状态与 offset。

- 做法（架构层）：
  - `RbfFileImpl` 持有一个 `_builder` 字段（初次使用创建）。
  - `BeginAppend()` 不再 new builder，而是 `_builder.Reset(frameStart)` 并返回。
  - `RbfFrameBuilder.Dispose()` 在 committed/aborted 后把内部 writer Reset 回到干净状态（但不 Dispose 掉 writer），使其可再次 Begin。
- 收益：
  - 直接消除 `RbfFrameBuilder`/`RandomAccessByteSink`/`SinkReservableWriter` 的 per-frame 堆分配（以及 options/strategy 等构造链）。
  - 不引入池上限、命中率、跨文件归还等额外复杂度。
- 代价/风险：
  - 必须把 builder/writer/sink 从“只构造一次、readonly 字段”演进成“可 Reset 的会话状态机”。
  - Reset 不完整会引入脏状态泄漏；需要一组专门的 Reset 单测（见下文“验证建议”）。

**权衡结论**：在已存在“单 builder 活跃”约束下，这是最自然、最少魔法、最容易验收的生命周期优化路径。

### 方案 B：**预绑定委托/方法，消除 BeginAppend 的 per-call 闭包分配**

- 做法：
  - 在 `RbfFileImpl` 构造函数中创建并缓存 `_onCommitCallback` / `_clearBuilderFlag`（捕获一次即可），BeginAppend 直接传字段；或改为传 method group（并缓存 delegate）。
- 收益：
  - 立刻减少每次 BeginAppend 的隐藏分配；与是否池化 builder 解耦。
- 代价：
  - 极低；几乎纯收益。

**权衡结论**：这是“先手 P0”。即使不做其他复用，也值得做。

### 方案 C：**引入 `ObjectPool<T>`（但建议限定在 per-file，而非全局共享）**

- 适用条件：
  - 未来放宽“同一时刻只有一个 builder”的限制（例如：同文件并发写多个 frame，或多文件共享 builder 资源）。
- 设计建议：
  - **优先 per-file pool**：因为 `SafeFileHandle` 是 file 实例状态，跨 file 复用会引入更多 Reset/安全约束。
  - 如果确实要全局池，建议池化的是“无 handle 的纯 writer core”，把 sink 作为参数注入（见方案 D）。
- 风险：
  - 池化对象的 use-after-return 更隐蔽；必须建立 owner token / generation id 等防误用机制（尤其在异常路径上）。

**权衡结论**：在当前约束下，ObjectPool 不是第一选择；除非后续要支持并发或多 builder。

### 方案 D：**把 `ReservationTracker` 做 small-vector（≤N 个 reservation 零分配），超出才 fallback 到 Dictionary/LinkedList**

- 适用条件：
  - 常见 codec 只需要少量 reservation（例如 1~4 个）；且你们确实在意 micro-allocation。
- 收益：
  - 消除 headlen 这一“必有 reservation”导致的容器初始化分配；也显著减少每次 `ReserveSpan()` 的 entry/node 分配。
- 代价：
  - 实现复杂度上升，测试矩阵扩大（顺序提交/乱序提交/Reset/Dispose/Token 不复用）。

**权衡结论**：这是性能向的中期优化；建议先做 A/B，把架构的生命周期骨架钉死后再做。

### 方案 E：**抽取共享 core，减少 `SinkReservableWriter` 与 `ChunkedReservableWriter` 代码重复**

- 方向 1（偏性能）：内部用泛型 `where TSink : struct, ISpanSink`，避免 delegate 间接。
- 方向 2（偏工程）：抽一层 internal helper（例如 chunk 管理/flush 边界计算）+ 两个具体 writer 调用。
- 收益：降低长期维护税，减少双修 bug。
- 风险：抽象过度会带来可读性下降，甚至引入新的间接开销。

**权衡结论**：如果后续 writer 逻辑还会快速演进，这是值得的；如果 MVP 后基本稳定，可以接受重复但要明确“同构实现必须同步”的维护规则。

---

## 4. 对 Implementer 分析结果的补充意见

Implementer 的“堆分配清单”总体方向正确，但从“生命周期设计”视角，有几处需要补强：

1) **需要把 `RbfFileImpl.BeginAppend()` 的闭包/委托分配纳入固定成本**
- 这部分是常见盲区：代码上看是“只是传了个回调”，但对 GC 来说是“每帧都在制造短命对象”。

2) **需要把 `SinkReservableWriter` 的 options/Clone 视为 hot-path 分配**
- 如果继续“每帧 new Writer”，那么“options 两连 new”比 `ChunkSizingStrategy class` 更值得优先优化。

3) **“池化 Builder”之外，更贴合现状的其实是“按 file 复用单 builder”**
- 因为 `_hasActiveBuilder` 已把并发写入排除了：这让“池”的意义变弱，“复用单实例”更简单更稳。

4) **对 `RandomAccessByteSink` 的评价应更偏正面：它不是纯传话筒，而是很干净的层间适配器**
- 从架构边界看，它把 RBF 的 RandomAccess I/O 细节隔离在 `Rbf.Internal`，让 `Atelia.Data` 只面对 `IByteSink`。
- 如果要优化，建议优先考虑“复用/Reset”，而不是合并层级。

---

## 附：生命周期矩阵（建议作为后续重构验收点）

| 对象 | 推荐生命周期 | 是否可全局单例 | 备注 |
|---|---:|---:|---|
| `ArrayPool<byte>.Shared` | 全局 | ✅ | 已是最佳实践 |
| `ChunkedReservableWriterOptions` | “配置”应与 file/系统绑定 | ✅/⚠️ | 作为 immutable 共享值可行；当前 class+Clone 更像“每实例私有” |
| `ReservationTracker` | 与 writer 实例绑定 | ❌ | 有状态，且不线程安全 |
| `SinkReservableWriter` | 与 builder（或 file）绑定并可 Reset | ❌ | 强状态机；适合复用，不适合全局 |
| `RandomAccessByteSink` | 与 file 绑定并可 Reset(offset) | ❌ | handle 与 file 一致；offset 每帧变化 |
| `RbfFrameBuilder` | 与 file 绑定并可 Reset(frameStart) | ❌ | 当前每帧 new；更推荐按 file 复用 |

---

## 验证建议（为池化/复用保驾护航）

- **Reset 完整性单测**：连续 Begin→写入→Abort（Dispose）→Begin→写入→Commit；验证第二次写入不包含第一次残留、reservation/token 不可误提交。
- **异常路径单测**：在 `IByteSink.Push()` 注入抛异常，验证：builder Dispose 后 facade 处于可继续写状态（是否需要截断/标记损坏需明确）。
- **基准**：至少 2 组：
  - 小帧高频（例如 64B payload × 1M 次）评估分配/吞吐
  - 中等帧（例如 4KB~64KB）评估 chunk 复用与 CRC 扫描成本

---

## Craftsman 补充审阅（2026-02-01）

> 以下内容补充 Investigator 的初始审阅，聚焦"架构视角的取舍判断"。

### 补充 1：对"方案 A vs ObjectPool"的进一步收敛

原报告的方案 A（按 `RbfFileImpl` 复用单个 Builder）与 Implementer 提议的 ObjectPool 方案，本质区别在于：

| 维度 | 方案 A（per-file 复用） | ObjectPool |
|:-----|:------------------------|:-----------|
| **语义自然度** | ✅ 与 `_hasActiveBuilder` 约束天然匹配 | ⚠️ 需要显式 Get/Return 仪式 |
| **跨 file 共享** | ❌ 不支持 | ✅ 支持（但当前无此需求） |
| **Reset 复杂度** | 中（需 Reset sink offset） | 高（需 Reset + 重注入 handle） |
| **实现成本** | ~4h | ~6h |

**收敛判断**：鉴于 `[S-RBF-BUILDER-SINGLE-OPEN]` 已限定单 builder 活跃，**方案 A 是当前约束下的最优解**。只有当需要"多 file 共享 builder 池"或"放宽单 builder 约束"时，才值得引入 ObjectPool。

### 补充 2：对"闭包分配"问题的验证与修复建议

原报告 P0-1 提到 `BeginAppend()` 的 lambda 闭包分配。我通过源码确认：

```csharp
// RbfFileImpl.BeginAppend() 中的两处 lambda
Action<long> onCommitCallback = (endOffset) => _tailOffset = endOffset;
Action clearBuilderFlag = () => _hasActiveBuilder = false;
```

这两个 lambda **每次 BeginAppend 都会分配**（捕获 `this` 实例）。修复方案：

```csharp
// 在 RbfFileImpl 构造函数中缓存
private readonly Action<long> _onCommitCallback;
private readonly Action _clearBuilderFlag;

public RbfFileImpl(...) {
    _onCommitCallback = (endOffset) => _tailOffset = endOffset;
    _clearBuilderFlag = () => _hasActiveBuilder = false;
}
```

**收益**：每帧节省 2 次 delegate 分配（约 ~48B），低成本高收益。

### 补充 3：对 ReservationEntry struct 化的风险补充

Implementer 建议将 `ReservationEntry` 改为 struct。我同意方向，但需补充风险点：

| 风险 | 说明 | 缓解 |
|:-----|:-----|:-----|
| **LinkedListNode 仍然分配** | 即使 Entry 是 struct，`LinkedList<T>.AddLast()` 仍会 new node | 若要彻底消除，需改用数组 + 索引 |
| **Chunk 引用语义不变** | Entry 的 `Chunk` 字段是引用类型，struct 化不影响 Chunk 生命周期 | 无需额外处理 |
| **Tag 字段的 string 分配** | `Tag` 是 `string?`，调试时传入字符串常量无分配，但若动态构造会分配 | 生产环境建议 `Tag = null` |

**结论**：Entry struct 化是**低风险中收益**，推荐实施。若要进一步消除 node 分配，可改用"固定槽位 + overflow list"模式，但复杂度上升，建议作为二期优化。

### 补充 4：IByteSink 接口的设计亮点

原报告对 `IByteSink` 的评价偏中性。从架构视角，这是**推式接口的典型正确用法**：

- **职责清晰**：调用方持有数据所有权，sink 只负责消费
- **无缓冲要求**：sink 不需要持有 buffer，简化了生命周期
- **同步语义**：`Push()` 返回即表示数据已写入，无需回调

这种设计使得 `RandomAccessByteSink` 只有 ~50 行代码，且无需实现 `IDisposable`。**这是"接口抽象代价最小化"的范例**。

### 补充 5：实施顺序建议（修订）

综合原报告与 Implementer 分析，建议的实施顺序：

| 阶段 | 任务 | 预估工时 | 依赖 |
|:-----|:-----|:---------|:-----|
| **Phase 0** | 缓存 BeginAppend 的 delegate（补充 2） | 0.5h | 无 |
| **Phase 1a** | `ChunkSizingStrategy` struct 化 | 1h | 无 |
| **Phase 1b** | `ReservationEntry` struct 化 | 2h | 无 |
| **Phase 2** | `SinkReservableWriter.Reset(IByteSink)` + `RandomAccessByteSink.Reset(offset)` | 3h | Phase 1 |
| **Phase 3** | `RbfFrameBuilder.Reset()` + `RbfFileImpl` per-file 复用 | 4h | Phase 2 |

**总工时**：~10.5h，可分 2-3 个 Sprint 渐进式实施。

### 补充 6：对"SinkReservableWriter 与 ChunkedReservableWriter 重复"的处理建议

原报告 P0-3 指出两者存在大量重复逻辑。我的建议：

- **短期**：保持现状，在两者共同变更时同步修改
- **中期**：若演进频率 >2 次/月，抽取 `ReservableWriterCore<TSink>` 泛型内部类
- **长期**：若 `ChunkedReservableWriter` 使用场景消失，直接删除

**判断依据**：当前 `ChunkedReservableWriter` 主要用于测试和非文件场景，与 `SinkReservableWriter` 的分化点在于 sink 类型。除非 RBF 之外还有高频使用场景，否则不急于抽象。

---

## 最终结论

| 维度 | 评估 |
|:-----|:-----|
| **职责边界** | ✅ 清晰，IByteSink 推式接口是亮点 |
| **生命周期** | ⚠️ 三元组绑定但未复用，每帧 ~150B + ~48B delegate 分配 |
| **接口抽象代价** | ✅ IByteSink 是轻量适配，IReservableBufferWriter 可补充 TryGetReservationSpan |
| **池化/复用可行性** | ✅ per-file 复用是最优解，不需要全局 ObjectPool |

**核心洞见**：当前架构"职责清晰，生命周期未复用"，演进方向是 **per-file 单 builder 复用 + delegate 缓存**，而非引入全局池。这是约束驱动的自然结论，风险可控。
