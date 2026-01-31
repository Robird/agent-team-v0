# Inbox — 待处理便签

> 这是你的便签收集箱。
> 写下洞见、经验、状态变更，MemoryPalaceKeeper 会定期整理归档。
>
> **格式**：每条便签用 `## 便签 YYYY-MM-DD HH:MM` 开头，用 `---` 分隔。

---

<!-- 在下方添加你的便签 -->

## 便签 2026-01-31 12:30
**类型**：[CodeMap]
**项目**：Rbf

### Task 7.2: RbfFrameBuilder 实现

**实现要点**：
1. `RbfFrameBuilder` 是 `sealed class`，包含完整的帧构建逻辑
2. 构造函数创建 `RbfWriteSink`（crcSkipBytes=4）+ `SinkReservableWriter`，预留 HeadLen
3. `EndAppend` 10 步逻辑完整实现（验证→计算布局→写Padding→获取CRC→写Tail→回填HeadLen→Commit→回调→返回）
4. `Dispose` 支持 Auto-Abort（Zero I/O）通过 `_writer.Reset()`

**需要的 Data 层扩展**：
| 位置 | 新增内容 |
|:-----|:---------|
| `ReservationTracker.cs` | `TryPeek(token, out entry)` 方法（不移除） |
| `SinkReservableWriter.cs` | `TryGetReservationSpan(token, out span)` 方法 |

**扩展点说明**：
- 原因：`ReserveSpan` 返回 `Span<byte>` 是值类型，无法跨方法边界存储
- 解决：通过 token 重新获取 reservation 对应的 span（底层数组在 Commit 前稳定）
- 这是通用能力，符合 "在 Commit 前回填数据" 的常见用例

**测试状态**：
- Data.Tests: 1 通过（SinkReservableWriter 基本测试）
- Rbf.Tests: 204 通过（无回归）
- 需要等待 Task 7.3（RbfFileImpl.BeginAppend）完成后编写集成测试

---

## 便签 2026-02-01 10:30
**类型**：[CodeMap]
**项目**：Data / Rbf

### GetCrcSinceReservationEnd 实现

**背景**：RbfFrameBuilder 在 HeadLen pending 时需要计算 payload CRC，旧方案（Sink 层 CRC）因数据未 push 到 sink 而失效。

**实现扩展点**：
| 位置 | 新增内容 |
|:-----|:---------|
| `SinkReservableWriter.cs` | `GetCrcSinceReservationEnd(token, init, xor)` 方法 |

**方法约束（强约束/早失败）**：
- writer 未 Dispose
- `_hasLastSpan == false`（无借用 span/memory）
- token 有效且 pending
- `PendingReservationCount == 1`

**实现要点**：
1. 通过 `ReservationTracker.TryPeek` 定位 entry（chunk/offset/length）
2. 从 entry 所在 chunk 开始遍历 active chunks
3. 第一个 chunk：start = entry.Offset + entry.Length, end = chunk.DataEnd
4. 后续 chunk：start = chunk.DataBegin, end = chunk.DataEnd
5. 对每段 span 调用 `RollingCrc.CrcForward` 累积
6. 返回 crcRaw ^ finalXor

**RbfFrameBuilder 修改**：
- payloadAndMetaLength：改用 `WrittenLength - HeadLenSize`（不再用 `_sink.CrcCoveredLength`）
- payloadCrc：调用 `_writer.GetCrcSinceReservationEnd(_headLenReservationToken)`

**测试覆盖**（SinkReservableWriterCrcTests.cs）：
- 正确性：单一 pending + payload → CRC 匹配
- 正确性：空 payload → 空 CRC
- 正确性：大 payload 跨 chunk → CRC 正确
- 正确性：自定义 init/xor → CRC 正确
- 失败：GetSpan 后未 Advance → InvalidOperationException
- 失败：2 个 pending → InvalidOperationException
- 失败：已 commit token → InvalidOperationException
- 失败：Reset 后旧 token → InvalidOperationException
- 失败：已 Dispose → ObjectDisposedException
- 状态不变性：调用不修改 WrittenLength/PushedLength/PendingCount

---
