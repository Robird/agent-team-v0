# 畅谈会：RBF RandomAccess → IBufferWriter 适配器设计

**日期**：2026-01-11
**标签**：`#design`
**参会**：TeamLeader, Craftsman, Implementer
**上下文**：RBF 写入路径实现

---

## 问题陈述 (TeamLeader)

**背景**：
- `ChunkedReservableWriter` 已实现，接受 `IBufferWriter<byte> _innerWriter` 作为底层写入目标
- RBF 底层使用 `System.IO.RandomAccess` API：`RandomAccess.Write(SafeFileHandle, ReadOnlySpan<byte>, long offset)`
- 需要适配器：`RandomAccess → IBufferWriter<byte>`

**设计约束**（监护人明确）：
1. **轻量级包装**：不做二次写入合并（合并已由 ChunkedReservableWriter 完成）
2. **延迟写入**：依赖 `RbfFrameBuilder.Commit` 回填 HeadLen 并触发最终 flush
3. **无状态优先**：RandomAccess.Write 本身无状态，适配器应尽量保持简单

**关键文件**：
- [ChunkedReservableWriter.cs](../../../atelia/src/Data/ChunkedReservableWriter.cs) — 内部使用 `_innerWriter.GetSpan/Advance`
- [rbf-format.md](../../../atelia/docs/Rbf/rbf-format.md) — HeadLen 回填语义
- [rbf-type-bone.md](../../../atelia/docs/Rbf/rbf-type-bone.md) — RbfFrameBuilder 设计
- [rbf-interface.md](../../../atelia/docs/Rbf/rbf-interface.md) — 接口契约

**待讨论**：
1. 适配器类型命名与职责边界
2. 适配器状态设计（offset/handle 如何维护）
3. GetSpan/GetMemory 与 Advance 的实现策略
4. 与 RbfFrameBuilder 的协作模式（何时触发 Write）
5. 错误处理与 Result-Pattern 传播

---

## 候选方案（等待参会者发言）

*（请 Craftsman 和 Implementer 补充候选方案与 tradeoff 分析）*

---

## 决策记录（TeamLeader 收敛）

**决策时间**：2026-01-11
**状态**：已收敛

### 最终方案

采纳 **方案 A：SequentialRandomAccessBufferWriter**（Craftsman 推荐，Implementer 支持）。

### 决策理由

1. **职责边界清晰**：适配器仅做"buffer provisioning + sequential offset accounting"，与 `ChunkedReservableWriter` 的"合并 + reservation/backfill + flush gating"分工明确
2. **符合监护人三条约束**：
   - 轻量级包装 ✅（~80 行代码，单 buffer 状态）
   - 延迟写入依赖 Commit ✅（通过 reservation gating 隐式保证）
   - 尽量简单 ✅（最小状态机，直白语义）
3. **实现可行性高**：Implementer 详细分析了实现细节与潜在坑点，无阻塞问题
4. **Zero I/O Abort 可达成**：前提是 `RbfFrameBuilder` 在开头立即 reserve HeadLen（此为关键前置条件）

### 核心类型定义（最终版）

```csharp
namespace Atelia.Rbf.Internal;

/// <summary>
/// Sequential IBufferWriter over RandomAccess.Write.
/// </summary>
/// <remarks>
/// <para><b>职责边界</b>：仅做 buffer provisioning + sequential offset accounting。</para>
/// <para><b>延迟写入</b>：由上层 <see cref="ChunkedReservableWriter"/> 通过 reservation gating 保证。</para>
/// <para><b>并发</b>：非线程安全，依赖 <c>[S-RBF-BUILDER-SINGLE-OPEN]</c> 契约。</para>
/// </remarks>
internal sealed class SequentialRandomAccessBufferWriter : IBufferWriter<byte>, IDisposable {
    private readonly SafeFileHandle _file;
    private readonly ArrayPool<byte> _pool;
    private byte[]? _buffer;

    private long _writeOffset;
    private bool _hasOutstanding;
    private int _outstandingLength;
    private bool _disposed;

    public SequentialRandomAccessBufferWriter(
        SafeFileHandle file, 
        long startOffset, 
        ArrayPool<byte>? pool = null);

    public long CurrentOffset { get; }

    // IBufferWriter<byte> 实现
    public Memory<byte> GetMemory(int sizeHint = 0);
    public Span<byte> GetSpan(int sizeHint = 0);
    public void Advance(int count);

    // IDisposable 实现
    public void Dispose();
}
```

### 关键实现约束

1. **HeadLen 必须立即 reserve**（Implementer 强调）：
   ```csharp
   // RbfRawOps._BeginFrame() 必须确保
   var headLenSpan = chunkedWriter.ReserveSpan(4, out int headLenToken);
   // 在写任何 payload 前执行，以避免进入 passthrough mode
   ```

2. **Advance 立即写入**：
   - 每次 `Advance(count)` 调用 `RandomAccess.Write` 并推进 `_writeOffset`
   - 实际写入频率由 `ChunkedReservableWriter` 的 flush gating 控制（非性能问题）

3. **错误处理**：
   - I/O 异常在 `Advance` 时直接抛（符合 Infra Fault 规范）
   - Auto-Abort 路径的 `Dispose()` 不得抛异常

4. **Buffer 来源**：
   - 从 `ArrayPool<byte>` 租用
   - 单 buffer，按需扩容（超过当前容量时换新）
   - `Dispose()` 归还 pool

### 待实现阶段确认的问题（P2）

1. **Fence 写入位置**：Builder 层 vs 适配器层（影响 offset 计算）
2. **CRC32C 计算时机**：Commit 时遍历 vs 增量计算
3. **异步版本**：是否需要 `SequentialRandomAccessBufferWriterAsync`

### 后续行动

1. 将类型定义追加到 [rbf-type-bone.md](../../../atelia/docs/Rbf/rbf-type-bone.md)（TeamLeader 执行）
2. 在 Craft-Tier 实现时验证 HeadLen reservation 时机（Implementer 主导）
3. 编写集成测试验证 Zero I/O Abort 路径（QA 协助）

---

**会议结束** ✅


### Craftsman 发言

我把问题先“拆层”：

- `System.IO.RandomAccess` 是 **(handle, offset) → 写入** 的无状态原语；它不提供可写缓冲区。
- `IBufferWriter<byte>` 是 **“给你一块可写 buffer，然后你 Advance 告诉我写了多少”** 的协议；它天然是“顺序流式”的。
- 现有 `ChunkedReservableWriter` 已经承担了：
  - reservation/backfill（回填）
  - chunk 合并与“只 flush contiguous committed prefix”
  - passthrough 模式（无 reservation 时直通 inner writer）

因此 RandomAccess→IBufferWriter 的适配器职责边界应当极窄：

1) 维护顺序写入游标（offset），把 `Advance(count)` 翻译成 `RandomAccess.Write(handle, span[0..count], offset)`；
2) 提供可写 span/memory（通过 ArrayPool 租一块工作缓冲区）；
3) 不做 reservation，不做二次合并，不做 flush 策略（这些由 `ChunkedReservableWriter`/`RbfFrameBuilder.Commit` 驱动）。

下面给 2 个候选方案（都满足“轻量级包装、不做二次写入合并”的约束，但侧重点不同）。

---

## 方案 A：SequentialRandomAccessBufferWriter（推荐）

### 核心思路

- 适配器是“顺序文件写入器”：只维护 `_writeOffset`。
- `GetSpan/GetMemory`：从 `ArrayPool<byte>` 租（或复用）一个工作缓冲区，返回给上层写。
- `Advance(count)`：把工作缓冲区前 `count` 字节写到文件 `_writeOffset`，成功后 `_writeOffset += count`。
- **延迟写入如何满足？**：让 `RbfFrameBuilder` 在开头先 Reserve `HeadLen`（使 `ChunkedReservableWriter` 进入 buffered mode），这样在 `HeadLen` commit 前不会 flush 到 inner writer；最终在 `Commit()` 填好 HeadLen 并 commit reservation 后触发 flush，从而把整个 frame 一次性（按 chunk）落到 RandomAccess。

### 类型签名草图

```csharp
using System;
using System.Buffers;
using Microsoft.Win32.SafeHandles;
using System.IO;

namespace Atelia.Rbf.Internal;

/// <summary>
/// Minimal adapter: sequential IBufferWriter<byte> over RandomAccess.Write.
/// Responsibility: buffer provisioning + sequential offset accounting.
/// </summary>
internal sealed class SequentialRandomAccessBufferWriter : IBufferWriter<byte>, IDisposable {
	private readonly SafeFileHandle _file;
	private readonly ArrayPool<byte> _pool;
	private byte[]? _buffer;
	private int _bufferLength;

	private long _writeOffset;
	private bool _hasOutstanding;
	private int _lastRequestLength;
	private bool _disposed;

	public SequentialRandomAccessBufferWriter(SafeFileHandle file, long startOffset, ArrayPool<byte>? pool = null) {
		_file = file ?? throw new ArgumentNullException(nameof(file));
		_writeOffset = startOffset;
		_pool = pool ?? ArrayPool<byte>.Shared;
	}

	public long CurrentOffset => _writeOffset;

	public Memory<byte> GetMemory(int sizeHint = 0) {
		EnsureRequest(sizeHint);
		return _buffer!.AsMemory(0, _bufferLength);
	}

	public Span<byte> GetSpan(int sizeHint = 0) {
		EnsureRequest(sizeHint);
		return _buffer!.AsSpan(0, _bufferLength);
	}

	public void Advance(int count) {
		ThrowIfDisposed();
		if (!_hasOutstanding) throw new InvalidOperationException("GetSpan/GetMemory must be called before Advance.");
		if ((uint)count > (uint)_lastRequestLength) throw new ArgumentOutOfRangeException(nameof(count));

		if (count != 0) {
			RandomAccess.Write(_file, _buffer!.AsSpan(0, count), _writeOffset);
			_writeOffset += count;
		}

		_hasOutstanding = false;
		_lastRequestLength = 0;
	}

	public void Dispose() {
		if (_disposed) return;
		if (_buffer is not null) _pool.Return(_buffer);
		_buffer = null;
		_bufferLength = 0;
		_disposed = true;
	}

	private void EnsureRequest(int sizeHint) {
		ThrowIfDisposed();
		if (_hasOutstanding) throw new InvalidOperationException("Previous buffer not advanced.");
		if (sizeHint <= 0) sizeHint = 1;

		if (_buffer is null || _buffer.Length < sizeHint) {
			if (_buffer is not null) _pool.Return(_buffer);
			_buffer = _pool.Rent(sizeHint);
		}

		// IBufferWriter contract: returned length can be >= sizeHint
		_bufferLength = _buffer.Length;
		_hasOutstanding = true;
		_lastRequestLength = _bufferLength;
	}

	private void ThrowIfDisposed() {
		if (_disposed) throw new ObjectDisposedException(nameof(SequentialRandomAccessBufferWriter));
	}
}
```

### Tradeoff

| 维度 | 优势 | 劣势 |
|---|---|---|
| 简单性 | 最小状态：`handle + offset + 1 个工作 buffer`；非常贴合“轻量级包装”约束 | 需要调用方（builder）确保“在 commit 前不触发 flush” |
| 性能 | 写入路径无额外聚合；合并由 `ChunkedReservableWriter` 完成；每次 flush 是直接 RandomAccess.Write | 每次 `Advance` 产生一次 RandomAccess.Write（但 flush 频率由 `ChunkedReservableWriter` 控制，且是按 chunk/contiguous committed prefix） |
| Auto-Abort (Zero I/O) | 只要 builder 在开头先 reserve `HeadLen` 并在 `Commit` 前不 commit 它，就不会有任何 flush，Dispose 可做到“丢 buffer 即零 I/O” | 如果 builder 误用（先写后 reserve），可能在 passthrough 模式下提前落盘，Zero I/O 失败（需要 tombstone fallback） |
| 协作边界 | 适配器不理解 RBF，不理解 reservation；职责纯粹 | 延迟写入语义依赖“上层正确使用 reservation”这一隐含前置条件 |
| 错误处理 | `IBufferWriter` 只能抛异常；与 AteliaResult 规范里“Infra Fault 用异常”一致 | 无法在 IBufferWriter 层返回 `AteliaResult`（只能在上层边界捕获并转换） |

---

## 方案 B：CommitGatedRandomAccessBufferWriter（显式 Gate，延迟 I/O）

### 核心思路

- 适配器仍实现 `IBufferWriter<byte>`，但 `Advance(count)` **不做 I/O**，而是把“已写 segment”（byte[] + count）追加到一个列表。
- 在 `RbfFrameBuilder.Commit()` 时显式调用 `Flush()`，把所有 segment 按顺序写到 RandomAccess（offset 也在 flush 时推进）。
- 这样“延迟写入依赖 Commit”变成显式机制，不再依赖“HeadLen reservation 阻塞 flush”的隐式约束。

注意：这会引入一个额外的接口面（`Flush()`/`Abort()`），以及与 `ChunkedReservableWriter` 的职能重叠：
`ChunkedReservableWriter` 已经能通过 reservation 控制 flush 时机。

### 类型签名草图

```csharp
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace Atelia.Rbf.Internal;

internal interface ICommitGatedWriter {
	void Flush();
	void Abort();
}

internal sealed class CommitGatedRandomAccessBufferWriter : IBufferWriter<byte>, ICommitGatedWriter, IDisposable {
	private readonly SafeFileHandle _file;
	private readonly ArrayPool<byte> _pool;
	private readonly List<(byte[] Buffer, int Count)> _segments = new();
	private byte[]? _current;
	private int _currentLen;
	private bool _hasOutstanding;
	private int _lastRequestLength;
	private long _startOffset;
	private long _writeOffset;
	private bool _disposed;

	public CommitGatedRandomAccessBufferWriter(SafeFileHandle file, long startOffset, ArrayPool<byte>? pool = null) {
		_file = file ?? throw new ArgumentNullException(nameof(file));
		_pool = pool ?? ArrayPool<byte>.Shared;
		_startOffset = startOffset;
		_writeOffset = startOffset;
	}

	public Span<byte> GetSpan(int sizeHint = 0) {
		EnsureRequest(sizeHint);
		return _current!.AsSpan(0, _currentLen);
	}

	public Memory<byte> GetMemory(int sizeHint = 0) {
		EnsureRequest(sizeHint);
		return _current!.AsMemory(0, _currentLen);
	}

	public void Advance(int count) {
		if (!_hasOutstanding) throw new InvalidOperationException();
		if ((uint)count > (uint)_lastRequestLength) throw new ArgumentOutOfRangeException(nameof(count));
		if (count != 0) {
			_segments.Add((_current!, count));
			_current = null;
			_currentLen = 0;
		}
		_hasOutstanding = false;
		_lastRequestLength = 0;
	}

	public void Flush() {
		long offset = _startOffset;
		foreach (var (buf, cnt) in _segments) {
			RandomAccess.Write(_file, buf.AsSpan(0, cnt), offset);
			offset += cnt;
			_pool.Return(buf);
		}
		_segments.Clear();
		_writeOffset = offset;
		_startOffset = offset;
	}

	public void Abort() {
		foreach (var (buf, _) in _segments) _pool.Return(buf);
		_segments.Clear();
		_current = null;
		_currentLen = 0;
		_hasOutstanding = false;
		_lastRequestLength = 0;
		_writeOffset = _startOffset;
	}

	public void Dispose() {
		if (_disposed) return;
		Abort();
		_disposed = true;
	}

	private void EnsureRequest(int sizeHint) {
		if (_hasOutstanding) throw new InvalidOperationException("Previous buffer not advanced.");
		if (sizeHint <= 0) sizeHint = 1;
		_current = _pool.Rent(sizeHint);
		_currentLen = _current.Length;
		_hasOutstanding = true;
		_lastRequestLength = _currentLen;
	}
}
```

### Tradeoff

| 维度 | 优势 | 劣势 |
|---|---|---|
| 延迟写入语义 | 延迟 I/O 变成显式 `Flush()`，不依赖 reservation 阻塞 flush 的“隐式正确用法” | 引入额外接口面（`ICommitGatedWriter`），builder 与适配器耦合加深 |
| Zero I/O Abort | 非常容易做到：Commit 前 Dispose/Abort 直接丢 segments | 如果上层同时使用 `ChunkedReservableWriter`，会出现“双缓冲”与职责重叠，理解成本高 |
| 性能 | 可以把 I/O 聚合到一次 Flush（仍不做“合并 copy”，只是顺序写多个 segment） | 内存占用更高；小 segment 数量可能很多；并且这部分 buffering 与 `ChunkedReservableWriter` 重复 |
| 简单性 | 延迟语义更直观 | 代码量与状态机复杂度显著上升，违背“适配器应尽量简单”的约束 |
| 错误处理 | `Flush()` 处集中抛 I/O 异常，commit 边界更清晰 | I/O 异常发生时，已写入部分 segment 的回收/重试语义更棘手 |

---

## 逐问回答（对应会议 5 个问题）

1) **命名与职责边界**：建议直白命名 `SequentialRandomAccessBufferWriter` / `RandomAccessSequentialBufferWriter`。
   - 名字里带 `Sequential` 是关键：它只提供顺序游标语义，不承诺任意 offset 写。
   - 职责只做“顺序 offset + RandomAccess.Write”，不触碰 reservation/backfill/CRC 逻辑。

2) **状态设计（offset/handle）**：
   - 必备状态：`SafeFileHandle _file` + `long _writeOffset`。
   - 可选状态：`ArrayPool<byte> + byte[] workBuffer`；`_hasOutstanding/_lastRequestLength` 用于协议校验。
   - 不要引入 reservation token / chunk 列表（那是 `ChunkedReservableWriter` 的职责）。

3) **GetSpan/GetMemory 与 Advance 策略**：
   - `GetSpan/GetMemory(sizeHint)`：租/复用一个 `>= sizeHint` 的连续 buffer；返回“尽可能大”的 span（符合常见 IBufferWriter 实现习惯）。
   - `Advance(count)`：对方案 A 立刻 RandomAccess.Write；对方案 B 仅记录 segment。
   - 建议实现“禁止未 Advance 的二次 GetSpan”，以尽早暴露调用方 bug（`ChunkedReservableWriter` 本身也做了类似保护）。

4) **与 RbfFrameBuilder 协作（何时触发 Write）**：
   - 推荐协作模式（配合方案 A）：builder 开始时立刻 `ReserveSpan(4, out headLenToken)`（写 placeholder），确保 `ChunkedReservableWriter` 进入 buffered mode 并且 flush 被阻塞；
	 在 `Commit()` 最后填 HeadLen 并 `Commit(headLenToken)`，此时 `ChunkedReservableWriter` flush → inner writer → RandomAccess.Write。
   - 这样“延迟写入依赖 Commit”由 reservation 的可观察行为保障，且适配器保持极简。

5) **错误处理与 Result-Pattern 传播**：
   - `IBufferWriter` 接口层无法表达 `AteliaResult`（`Advance`/`GetSpan` 都无返回值），因此适配器应采用 Exception-pattern 表达 I/O 故障。
   - 这与 AteliaResult 规范对 Infra Fault 的建议一致：I/O 故障属于“基础设施故障”，在当前抽象层通常不应作为“正常分支”处理。
   - 若 TeamLeader 希望在更外层提供 Result-pattern：可以在 `IRbfFile.Append/BeginAppend.Commit` 边界捕获异常并转换为 `AteliaException(AteliaError)` 或新增 `TryAppend/TryCommit` 返回 `AteliaResult<SizedPtr>`（但这属于 API 形态决策，不建议塞进适配器）。

---

## 推荐结论

我推荐 **方案 A（SequentialRandomAccessBufferWriter）**。

- 它与现有分工最一致：`ChunkedReservableWriter` 负责“合并 + reservation/backfill + flush gating”，适配器只负责“顺序 offset 写入”。
- 它更符合监护人三条约束：轻量、延迟写入由 `Commit` 语义驱动（通过 reservation gating 实现）、整体简单。
- 关键风险点是“隐含前置条件”：builder 必须在写任何 payload 前就 reserve `HeadLen`；我建议把这个前置条件写进 `RbfFrameBuilder` 的实现注释/断言里（比如在 builder 开始写之前强制创建 head reservation），以避免误用导致提前落盘。



### Implementer 发言

认同 Craftsman 的方案 A（SequentialRandomAccessBufferWriter），下面从实现角度补充具体细节和潜在坑点。

---

## 1. GetSpan/GetMemory 返回的 Buffer 来源

**结论**：从 `ArrayPool<byte>.Shared`（或注入的 pool）租用。

**细节**：

```csharp
// 策略：按需租，超量时换大
private void EnsureRequest(int sizeHint) {
    if (_hasOutstanding) throw new InvalidOperationException("...");
    sizeHint = Math.Max(sizeHint, 1);

    // 只有当 buffer 为空或容量不足时才重新租用
    if (_buffer is null || _buffer.Length < sizeHint) {
        if (_buffer is not null) _pool.Return(_buffer);
        _buffer = _pool.Rent(sizeHint);
    }

    _bufferLength = _buffer.Length; // 返回整个可用长度，符合 IBufferWriter 惯例
    _hasOutstanding = true;
    _lastRequestLength = _bufferLength;
}
```

**坑点**：
- ❌ 不要用栈上 `stackalloc`——生命周期无法跨 `GetSpan` → `Advance` 调用边界（调用方可能持有 span 跨方法）
- ⚠️ 返回的 span 长度应 `>= sizeHint`（而非恰好等于）——这是 `IBufferWriter` 的惯例，减少 roundtrip 次数

---

## 2. Advance 何时触发 RandomAccess.Write

**方案 A（推荐）**：每次 `Advance(count)` **立即**调用 `RandomAccess.Write`。

**关键洞察**：实际不会频繁触发——因为 `ChunkedReservableWriter` 是适配器的**调用者**，而它的 flush 行为受 reservation gating 控制：

```
┌─────────────────────────────────────────────────────────────┐
│ RbfFrameBuilder                                             │
│   ├─ ReserveSpan(4, out headLenToken)   ← HeadLen 占位      │
│   ├─ Payload.GetSpan()/Advance()        ← 进入 buffered mode│
│   └─ Commit()                                               │
│        ├─ backfill HeadLen                                  │
│        └─ Commit(headLenToken)          ← flush 触发!       │
├─────────────────────────────────────────────────────────────┤
│ ChunkedReservableWriter                                     │
│   └─ 只在 contiguous committed prefix 时才调 _innerWriter   │
├─────────────────────────────────────────────────────────────┤
│ SequentialRandomAccessBufferWriter (适配器)                 │
│   └─ Advance() → RandomAccess.Write()   ← 实际只在 flush 时 │
└─────────────────────────────────────────────────────────────┘
```

**结论**：适配器的 `Advance` 虽然"立即写"，但调用频率由上层 flush 策略控制，不影响"延迟写入依赖 Commit"的语义。

---

## 3. 适配器是否需要内部 Buffer

**结论**：需要——但只需要 **1 个工作 buffer**，职责是"提供可写 span"。

**职责边界对比**：

| 职责 | ChunkedReservableWriter | 适配器 |
|------|------------------------|--------|
| Chunk 管理 | ✅ 多 chunk + SlidingQueue | ❌ |
| Reservation/Backfill | ✅ token + commit 机制 | ❌ |
| Flush Gating | ✅ 阻塞直到 reservation commit | ❌ |
| 提供可写 Span | ❌ passthrough 时直接调 inner | ✅ 从 pool 租 |
| 顺序 offset 维护 | ❌ | ✅ `_writeOffset += count` |

**为什么适配器必须有 buffer**：`RandomAccess.Write(handle, ReadOnlySpan, offset)` 是 **pull 模型**（你给它数据），而 `IBufferWriter` 是 **push 模型**（它给你 buffer 填）。适配器必须提供"可写 buffer"来桥接这两种模型。

---

## 4. 并发安全

**分析**：
- `RandomAccess.Write` 本身是线程安全的（OS 层面原子写）
- 但 `SequentialRandomAccessBufferWriter` 内部状态（`_writeOffset`, `_hasOutstanding`, `_buffer`）不是
- **更重要的是**：`IRbfFile` 接口已约束 `[S-RBF-BUILDER-SINGLE-OPEN]`——同一实例同时最多 1 个 open Builder

**结论**：适配器不需要做并发保护，依赖上层契约保证单线程写入。

**坑点**：如果未来支持并发读写场景，需要：
1. 为每个 Builder 创建独立的适配器实例
2. 或者用 `Interlocked` 保护 offset 推进（但 buffer 复用会变复杂）

---

## 5. 错误处理

**结论**：I/O 异常在 `Advance` 时直接抛出，上层边界捕获并转换。

**处理层次**：

```csharp
// 适配器层：直接抛（符合"Infra Fault 用异常"规范）
public void Advance(int count) {
    // ...
    RandomAccess.Write(_file, _buffer!.AsSpan(0, count), _writeOffset);
    // ↑ 失败时抛 IOException / UnauthorizedAccessException
    _writeOffset += count;
}

// IRbfFile.Append 层：可选择捕获并包装
public SizedPtr Append(uint tag, ReadOnlySpan<byte> payload) {
    try {
        // ... write logic ...
    }
    catch (IOException ex) {
        throw new AteliaException(AteliaError.IoFailure, "...", ex);
    }
}
```

**坑点**：
- ⚠️ 如果在 `Advance` 中途失败（部分写入），`_writeOffset` 状态会不一致。但由于 `RandomAccess.Write` 通常是原子的（对于合理大小），且失败后 Builder 应走 Auto-Abort 路径，这不是实际问题。
- ⚠️ Auto-Abort 路径的 `Dispose()` MUST NOT 抛异常——如果此时 I/O 失败，只能 swallow（记日志）。

---

## 6. 补充实现细节

### 6.1 HeadLen 回填的 Reservation 时机

**关键洞察**：`HeadLen` 必须在 **Builder 创建时立即 reserve**，而不是等 payload 写完。

原因（对照 [rbf-format.md](../../../atelia/docs/Rbf/rbf-format.md)）：
- `HeadLen` 位于 Frame 开头（offset 0）
- 若先写 payload 再 reserve，`ChunkedReservableWriter` 会进入 passthrough mode 直接 flush
- Reserve 后才写 payload，才能确保 flush 被阻塞

**建议实现模式**：

```csharp
// RbfRawOps._BeginFrame()
internal static RbfFrameBuilder _BeginFrame(SafeFileHandle file, long writeOffset, uint tag) {
    var adapter = new SequentialRandomAccessBufferWriter(file, writeOffset);
    var chunkedWriter = new ChunkedReservableWriter(adapter);
    
    // 关键：立即 reserve HeadLen（4 字节）
    var headLenSpan = chunkedWriter.ReserveSpan(4, out int headLenToken);
    // 写占位值（可以是 0 或 magic，Commit 时回填）
    BinaryPrimitives.WriteUInt32LittleEndian(headLenSpan, 0);
    
    // 写 FrameTag（紧跟 HeadLen）
    var tagSpan = chunkedWriter.GetSpan(4);
    BinaryPrimitives.WriteUInt32LittleEndian(tagSpan, tag);
    chunkedWriter.Advance(4);
    
    return new RbfFrameBuilder {
        _chunkedWriter = chunkedWriter,
        _headLenToken = headLenToken,
        _startOffset = writeOffset,
        // ...
    };
}
```

### 6.2 Dispose 时的 Pool 归还

**坑点**：`Dispose()` 必须归还 buffer，但调用顺序可能是：

1. `RbfFrameBuilder.Dispose()` → Auto-Abort
2. `ChunkedReservableWriter.Dispose()` → 归还其 chunks
3. `SequentialRandomAccessBufferWriter.Dispose()` → 归还其 buffer

需要确保链式 Dispose 正确调用。建议在 Builder 的 `Dispose` 中显式调用内部 writer 的 `Dispose`。

### 6.3 offset 起点问题

**注意**：适配器的 `startOffset` 应从 `IRbfFile.TailOffset` 获取，但要考虑 Fence：

```csharp
// 新帧开始前，先写入前导 Fence
long frameStart = tailOffset + 4;  // 跳过 Fence 的 4 字节
// 或者让 Fence 写入也经过适配器，取决于架构选择
```

---

## 7. 建议的最终类型签名

在 Craftsman 的基础上做微调：

```csharp
namespace Atelia.Rbf.Internal;

/// <summary>
/// Sequential IBufferWriter over RandomAccess.Write.
/// </summary>
/// <remarks>
/// <para><b>职责边界</b>：仅做 buffer provisioning + sequential offset accounting。</para>
/// <para><b>延迟写入</b>：由上层 <see cref="ChunkedReservableWriter"/> 通过 reservation gating 保证。</para>
/// <para><b>并发</b>：非线程安全，依赖 <c>[S-RBF-BUILDER-SINGLE-OPEN]</c> 契约。</para>
/// </remarks>
internal sealed class SequentialRandomAccessBufferWriter : IBufferWriter<byte>, IDisposable {
    private readonly SafeFileHandle _file;
    private readonly ArrayPool<byte> _pool;
    private byte[]? _buffer;

    private long _writeOffset;
    private bool _hasOutstanding;
    private int _outstandingLength;
    private bool _disposed;

    public SequentialRandomAccessBufferWriter(
        SafeFileHandle file, 
        long startOffset, 
        ArrayPool<byte>? pool = null)
    {
        _file = file ?? throw new ArgumentNullException(nameof(file));
        _writeOffset = startOffset;
        _pool = pool ?? ArrayPool<byte>.Shared;
    }

    /// <summary>当前写入位置（byte offset）。</summary>
    public long CurrentOffset => _writeOffset;

    public Memory<byte> GetMemory(int sizeHint = 0) => GetSpan(sizeHint).AsMemory();

    public Span<byte> GetSpan(int sizeHint = 0) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_hasOutstanding)
            throw new InvalidOperationException("Previous buffer not advanced.");

        sizeHint = Math.Max(sizeHint, 1);

        if (_buffer is null || _buffer.Length < sizeHint) {
            if (_buffer is not null) _pool.Return(_buffer);
            _buffer = _pool.Rent(sizeHint);
        }

        _hasOutstanding = true;
        _outstandingLength = _buffer.Length;
        return _buffer.AsSpan();
    }

    public void Advance(int count) {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!_hasOutstanding)
            throw new InvalidOperationException("GetSpan/GetMemory must be called first.");
        if ((uint)count > (uint)_outstandingLength)
            throw new ArgumentOutOfRangeException(nameof(count));

        if (count > 0) {
            RandomAccess.Write(_file, _buffer.AsSpan(0, count), _writeOffset);
            _writeOffset += count;
        }

        _hasOutstanding = false;
        _outstandingLength = 0;
    }

    public void Dispose() {
        if (_disposed) return;
        _disposed = true;

        if (_buffer is not null) {
            _pool.Return(_buffer);
            _buffer = null;
        }
    }
}
```

---

## 8. Tradeoff 汇总

| 维度 | 方案 A（推荐） | 风险/缓解措施 |
|------|---------------|---------------|
| **复杂度** | 极简：~80 行代码 | - |
| **Zero I/O Abort** | 依赖 reservation gating | ⚠️ 需要 Builder 一开始就 reserve HeadLen |
| **Buffer 复用** | 单 buffer，按需扩容 | 大帧场景可能多次换 buffer，但实际由 ChunkedReservableWriter chunk 策略控制 |
| **错误恢复** | I/O 失败直接抛异常 | Auto-Abort 路径需要 swallow 异常 |
| **测试性** | 可注入 mock SafeFileHandle | 需要真实文件或 MemoryMappedFile 模拟 |

---

## 9. 待确认问题

1. **Fence 写入位置**：Fence 是由 Builder 层写还是适配器层写？（影响 offset 计算）
2. **CRC32C 计算时机**：是在 `Commit()` 时遍历已写 chunks 计算，还是写入时增量计算？
3. **异步版本**：是否需要 `SequentialRandomAccessBufferWriterAsync` 配合 `RandomAccess.WriteAsync`？

以上问题可在 Craft-Tier 实现阶段确认。
