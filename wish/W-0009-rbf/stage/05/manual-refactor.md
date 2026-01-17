# Stage 05 手动重构记录

> **时间范围**: 2026-01-15 ~ 2026-01-17  
> **参与者**: 监护人 + Implementer  
> **涉及提交**: 46aa891, bc5bd8b, 733bafb

---

## 概述

本次重构聚焦于三个核心目标：

1. **ReadFrame 重构与 Buffer 外置** — 实现 zero-copy 读取，引入 `RbfPooledFrame` 自动管理 ArrayPool buffer
2. **SizedPtr 类型简化** — 将 `ulong/uint` 改为 `long/int`，与 .NET API 对齐
3. **DisposableAteliaResult** — 引入带资源所有权的结果类型，简化 Disposable 资源的错误处理

---

## 设计变更

### 1. RBF ReadFrame 重构 (Commit 733bafb)

#### 1.1 字段重命名

| 旧名称 | 新名称 | 理由 |
|:-------|:-------|:-----|
| `RbfFrame.Ptr` | `RbfFrame.Ticket` | "Ticket"语义更明确——表示读取凭据而非裸指针 |

#### 1.2 新增接口 `IRbfFrame`

统一 `RbfFrame`（ref struct）和 `RbfPooledFrame`（class）的公共契约：

```csharp
public interface IRbfFrame {
    SizedPtr Ticket { get; }
    uint Tag { get; }
    ReadOnlySpan<byte> Payload { get; }
    bool IsTombstone { get; }
}
```

#### 1.3 文件变更

| 操作 | 文件路径 |
|:-----|:---------|
| 新增 | `src/Rbf/IRbfFrame.cs` |
| 新增 | `src/Rbf/RbfPooledFrame.cs` |
| 新增 | `src/Rbf/Internal/RbfReadImpl.cs` |
| 删除 | `src/Rbf/Internal/RbfRawOps.ReadFrame.cs` |
| 修改 | `src/Rbf/RbfFrame.cs` |
| 修改 | `src/Rbf/IRbfFile.cs` |
| 修改 | `src/Rbf/Internal/RbfErrors.cs` |

### 2. SizedPtr 类型简化 (Commit bc5bd8b)

#### API 变更对照

| 旧 API | 新 API | 类型变更 |
|:-------|:-------|:---------|
| `OffsetBytes` | `Offset` | `ulong` → `long` |
| `LengthBytes` | `Length` | `uint` → `int` |
| `OffsetBits` | `OffsetPackedBits` | 语义不变，仅改名 |
| `LengthBits` | `LengthPackedBits` | 语义不变，仅改名 |
| `LengthMask` | `LengthPackedMask` | 语义不变，仅改名 |

**设计理由**: .NET API（如 `RandomAccess.Read`、`Span<T>` 索引）普遍使用 `long`/`int`，保持类型一致避免频繁转换。

### 3. DisposableAteliaResult (Commit 46aa891)

#### 文件变更

| 操作 | 文件路径 |
|:-----|:---------|
| 新增 | `src/Primitives/DisposableAteliaResult.cs` |
| 新增 | `src/Primitives/IAteliaResult.cs` |
| 重命名 | `AteliaAsyncResult.cs` → `AsyncAteliaResult.cs` |
| 简化 | `src/Primitives/AteliaResult.cs` |

---

## 新增类型

### RbfPooledFrame

携带 ArrayPool buffer 的 RBF 帧，实现 `IDisposable`：

```csharp
public sealed class RbfPooledFrame : IRbfFrame, IDisposable {
    private byte[]? _buffer;
    private readonly int _payloadOffset;
    private readonly int _payloadLength;

    public SizedPtr Ticket { get; }
    public uint Tag { get; }
    public bool IsTombstone { get; }

    public ReadOnlySpan<byte> Payload => _buffer is not null
        ? _buffer.AsSpan(_payloadOffset, _payloadLength)
        : throw new ObjectDisposedException(nameof(RbfPooledFrame));

    public void Dispose() {
        var buf = _buffer;
        if (buf is not null) {
            _buffer = null;
            ArrayPool<byte>.Shared.Return(buf);
        }
    }
}
```

**生命周期契约**:
- 调用方 **MUST** 调用 `Dispose()` 归还 buffer
- Dispose 后 `Payload` 变为 dangling，访问将抛 `ObjectDisposedException`

### RbfReadImpl

新的读取实现，提供两个入口：

```csharp
internal static class RbfReadImpl {
    // Buffer 外置，zero-copy
    public static AteliaResult<RbfFrame> ReadFrame(
        SafeFileHandle file, SizedPtr ticket, Span<byte> buffer);

    // 自动管理 buffer（从 ArrayPool 借用）
    public static AteliaResult<RbfPooledFrame> ReadPooledFrame(
        SafeFileHandle file, SizedPtr ticket);
}
```

### RbfBufferTooSmallError

新增的专用错误类型：

```csharp
internal sealed record RbfBufferTooSmallError : AteliaError {
    public int RequiredBytes { get; init; }
    public int ProvidedBytes { get; init; }
}
```

### DisposableAteliaResult<T>

带资源所有权的结果类型：

```csharp
public sealed class DisposableAteliaResult<T> : IAteliaResult<T>, IDisposable
    where T : class, IDisposable {
    
    public bool IsSuccess { get; }
    public T? Value { get; }
    public AteliaError? Error { get; }
    
    public void Dispose() {
        // 成功时调用 Value.Dispose()
        // 失败时静默无操作
    }
}
```

**典型使用**:

```csharp
using var result = api.GetResource().ToDisposable();
if (result.IsFailure) { /* handle error */ }
var resource = result.Value;  // 安全使用
// scope 结束自动 Dispose
```

### IAteliaResult<T>

抽取的公共接口，统一 `AteliaResult<T>`、`AsyncAteliaResult<T>`、`DisposableAteliaResult<T>` 的契约：

```csharp
public interface IAteliaResult<T> where T : allows ref struct {
    bool IsSuccess { get; }
    bool IsFailure { get; }
    T? Value { get; }
    AteliaError? Error { get; }
    T? GetValueOrDefault(T? defaultValue = default);
    T GetValueOrThrow();
    bool TryGetError(out AteliaError? error);
    bool TryGetValue(out T? value);
}
```

---

## IRbfFile API 变更

```csharp
public interface IRbfFile : IDisposable {
    // 新增：Buffer 外置读取
    AteliaResult<RbfFrame> ReadFrame(SizedPtr ptr, Span<byte> buffer);

    // 新增：自动管理 buffer 读取
    AteliaResult<RbfPooledFrame> ReadPooledFrame(SizedPtr ptr);
    
    // 其他方法保持不变...
}
```

---

## 测试覆盖

测试文件已同步重构，覆盖以下场景：

| 场景 | 测试位置 |
|:-----|:---------|
| ReadFrame 正常读取 | `RbfFacadeTests.cs` |
| ReadPooledFrame 正常读取 | `RbfFacadeTests.cs` |
| Buffer 过小错误 | `RbfReadImplTests.cs` |
| Ticket 长度不足 | `RbfReadImplTests.cs` |
| CRC 校验失败 | `RbfReadImplTests.cs` |
| DisposableAteliaResult 生命周期 | `DisposableAteliaResultTests.cs` |

---

## 关键设计决策

### D1: 为什么引入 `IRbfFrame` 接口？

**问题**: `RbfFrame` 是 `ref struct`，无法被存储或用于异步场景；`RbfPooledFrame` 是 `class`，可以跨越作用域。两者需要统一契约。

**决策**: 创建 `IRbfFrame` 接口。C# 13 的 `ref struct 实现接口` 特性使这成为可能。

### D2: 为什么 `Ptr` 改名为 `Ticket`？

**问题**: "Ptr"暗示裸指针语义，容易误导使用者直接操作地址。

**决策**: "Ticket"明确表示这是一个"凭据"——用于后续操作（如读取），而非可直接解引用的指针。

### D3: 为什么 SizedPtr 改用 `long`/`int`？

**问题**: 原设计使用 `ulong`/`uint`，但 .NET 的 I/O API（`RandomAccess.Read`、`Stream.Position`）普遍使用有符号类型。

**决策**: 改为 `long`/`int`，消除每次 API 调用时的类型转换。38:26 bit 分配仍支持约 1TB 偏移和 256MB 长度。

### D4: 为什么创建 `DisposableAteliaResult<T>`？

**问题**: `AteliaResult<T>` 是 `ref struct`，无法实现 `IDisposable`；但 `RbfPooledFrame` 需要自动资源管理。

**决策**: 创建专用的 `DisposableAteliaResult<T> : class, IDisposable`，通过 `.ToDisposable()` 扩展方法转换，支持 `using var` 语法。

---

## 遗留事项

- [ ] 考虑为 `ReadPooledFrame` 添加 `DisposableAteliaResult` 包装，简化调用方代码
- [ ] 评估是否需要 `RbfFrame` 和 `RbfPooledFrame` 之间的相互转换方法
