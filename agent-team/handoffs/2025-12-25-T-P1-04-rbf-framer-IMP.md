# T-P1-04 IRbfFramer/Builder Implementation Result

## 实现摘要

完成了 RBF Layer 0 帧写入器的实现，包括 `FrameTag`、<deleted-place-holder> 类型定义、`IRbfFramer` 接口及 `RbfFramer` 实现类。实现符合 `[A-RBF-FRAMER-INTERFACE]`、`[A-RBF-FRAME-BUILDER]` 和 `[S-RBF-BUILDER-AUTO-ABORT]` 规范。

## 文件变更

| 文件 | 操作 | 描述 |
|------|------|------|
| `src/Rbf/FrameTag.cs` | 新建 | FrameTag 类型定义（`readonly record struct`），支持 fourCC 风格创建 |
| `src/Rbf/<deleted-place-holder>.cs` | 新建 | <deleted-place-holder> 类型定义，含 `Null`/`IsNull`/`IsValid`/`FromOffset` |
| `src/Rbf/IRbfFramer.cs` | 新建 | 接口定义 + `RbfFrameBuilder` ref struct + `IReservableBufferWriter` 接口 |
| `src/Rbf/RbfFramer.cs` | 新建 | 基于 `IBufferWriter<byte>` 的帧写入器实现 |
| `tests/Rbf.Tests/RbfFramerTests.cs` | 新建 | 16 个测试用例，覆盖帧写入、CRC、Auto-Abort 等 |

## 源码对齐说明

| 规范条款 | 实现位置 | 备注 |
|---------|---------|------|
| `[F-FRAMETAG-DEFINITION]` | `FrameTag.cs` | readonly record struct FrameTag(uint Value) |
| `[F-ADDRESS64-DEFINITION]` | `<deleted-place-holder>.cs` | 含 Null 静态成员和 IsNull/IsValid 属性 |
| `[A-RBF-FRAMER-INTERFACE]` | `IRbfFramer.cs` | Append/BeginFrame/Flush 三方法 |
| `[A-RBF-FRAME-BUILDER]` | `IRbfFramer.cs` | ref struct 实现，Payload/ReservablePayload/Commit/Dispose |
| `[S-RBF-BUILDER-AUTO-ABORT]` | `RbfFrameBuilder.Dispose()` | 未 Commit 时写入 Tombstone 帧 |
| `[S-RBF-BUILDER-SINGLE-OPEN]` | `RbfFramer.BeginFrame()` | _hasOpenBuilder 标志检查 |
| `[F-FRAME-LAYOUT]` | `RbfFramer.WriteFrameComplete()` | 按规范顺序写入各字段 |
| `[F-CRC32C-COVERAGE]` | `RbfFramer.WriteFrameComplete()` | CRC 覆盖 FrameTag+Payload+FrameStatus+TailLen |

## 测试结果

```
Targeted: dotnet test tests/Rbf.Tests/Rbf.Tests.csproj --filter "FullyQualifiedName~RbfFramerTests"
  → 16/16 passed

Full: dotnet test tests/Rbf.Tests/Rbf.Tests.csproj
  → 95/95 passed (含之前的 RbfConstants/Layout/Crc/Frame/FrameStatus 测试)
```

## 测试覆盖

| 测试用例 | 覆盖规范 |
|---------|---------|
| `Append_EmptyPayload_WritesValidFrame` | RBF-OK-001, [F-FRAME-LAYOUT] |
| `Append_WithPayload_WritesValidFrame` | [F-STATUSLEN-FORMULA] |
| `Append_VariousPayloadLengths_CorrectStatusLen` | RBF-OK-003, StatusLen 1-4 覆盖 |
| `Append_CrcCoversCorrectRange` | [F-CRC32C-COVERAGE] |
| `BeginFrame_Commit_WritesValidFrame` | [A-RBF-FRAME-BUILDER] |
| `BeginFrame_DisposeWithoutCommit_WritesTombstone` | [S-RBF-BUILDER-AUTO-ABORT] |
| `BeginFrame_WhileBuilderOpen_ThrowsInvalidOperationException` | [S-RBF-BUILDER-SINGLE-OPEN] |
| `Append_WhileBuilderOpen_ThrowsInvalidOperationException` | [S-RBF-BUILDER-SINGLE-OPEN] |
| `Commit_Twice_ThrowsInvalidOperationException` | Builder 状态管理 |
| `Append_MultipleFrames_CorrectAddresses` | <deleted-place-holder> 正确性 |
| `Append_Returns4ByteAlignedAddress` | [F-ADDRESS64-ALIGNMENT] |
| `BeginFrame_ReservablePayload_ReturnsNull` | MVP 简化（无 Reservation） |
| `Framer_WithoutGenesis_StartsAtPosition0` | 追加写入支持 |

## 已知差异

1. **ReservablePayload 返回 null**：MVP 阶段不实现 Reservation 机制，`ReservablePayload` 始终返回 null。

2. **Genesis Fence 可选**：构造函数添加 `writeGenesis` 参数，支持追加写入场景。

## 遗留问题

1. **IRbfScanner 未实现**：本任务仅实现写入器，读取器（Scanner）待后续任务。

2. **Flush 语义为空操作**：`IBufferWriter<byte>` 无 Flush 概念，当前实现为空操作。如需真正刷盘，上层需持有 Stream 引用并调用 `Stream.Flush()`。

## Changefeed Anchor

`#delta-2025-12-25-rbf-framer-impl`
