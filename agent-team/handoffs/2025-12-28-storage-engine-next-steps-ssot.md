# Workspace 内置存储引擎（移除 ObjectLoaderDelegate）— Next Steps SSOT

> 目的：防止上下文压缩导致“整体航线”丢失。
> 本文件只维护**接下来能确定的小步骤**（每步可独立完成/验证）。
> 
> 更新时间：2025-12-28

---

## 当前位置（已验证）

- RBF 写入路径已对齐既定设计：`ChunkedReservableWriter` + reservation/backfill。
- Auto-Abort 语义已收敛为 **Zero I/O abort**（Dispose 未 Commit 不产生任何帧）。
- File-backed 读写已具备：
  - file-backed writer：`RbfFileBackend` + `FileBackendBufferWriter` + `RbfFileFramer`
  - file-backed scanner：`RbfFileScanner` 支持 `TryReadAt` / `ReadPayload` / `ScanReverse`，且 `ScanReverse` 走 RandomAccess + CRC chunking
- `atelia` 全套测试可通过（若遇到 MSBuild pipe flake，使用 `dotnet test -m:1`）。

---

## Phase M1（RBF: file-backed 基建）剩余小步骤

### T-M1-13（DurableFlush 语义确认：标准库即可）✅
- 结论：.NET `FileStream.Flush(true)` 在 Unix 上走 `fsync`，在 macOS 上走 `F_FULLFSYNC`，可直接作为 `IRbfFileBackend.DurableFlush()`。
- 状态：代码已是 `_stream.Flush(flushToDisk: true)`（见 `atelia/src/Rbf/RbfFileBackend.cs`）。
- 参考：`agent-team/handoffs/2025-12-28-filestream.flush.md`

### T-M1-14（截断/损坏场景的 file-backed parity 维护）
**Goal**：保证 `RbfFileScanner` 在 truncate/corruption/resync 下持续与 `RbfScanner` 行为一致。
- 维护策略：任何涉及 framing/CRC/scan 的变更，都需要跑 parity tests。

**DoD（M1 收口）**
- 不读取整文件：`RbfFileScanner.ScanReverse` 不得 `ReadAllBytes`。
- 不整帧分配：`TryReadAt` 不得 `new byte[frameLen]`。
- `Rbf.Tests` 通过，并至少 1 个大 payload 场景覆盖。

---

## Phase M2（StateJournal Record Reader/Writer）— ✅ 全部完成

> 原则：每步尽量只引入 1 个新类型 + 1 组测试。
> 
> **状态**：全部 5 个任务已完成（2025-12-28）。M2 为 M3 Recovery/Materialize 提供了完整的 Record 读写能力。

### T-M2-01（定义 record/tag 常量与 payload layout）✅
- FrameTag 分配：`atelia/src/StateJournal/Core/FrameTags.cs`
- ObjectVersionRecord payload：`atelia/src/StateJournal/Core/ObjectVersionRecord.cs`
- 测试：`atelia/tests/StateJournal.Tests/Core/ObjectVersionRecordTests.cs`

### T-M2-02（实现 MetaRecordWriter：append meta records）
- 输入：commit metadata（commit id、root ptr、indexes ptr 等按 SSOT）。
- 输出：调用 `IRbfFramer.Append/BeginFrame` 写入 meta frames。
- 测试：写入后用 `RbfScanner` 读回并解析。

### T-M2-03（实现 DataRecordWriter：append object version records）
- 支持 reservation/backfill：先写 prevPtr + diff，再回填长度/校验（如需要）。
- 测试：写入 N 个版本，确认 Prev 链可追溯。

### T-M2-03a（最小 DataRecordWriter：只写 DictVersion）
- 先落一个最小可用 `DataRecordWriter.AppendDictVersion(prevPtr, diffPayload)`。
- 直接用 `IRbfFramer.BeginFrame(FrameTags.DictVersion)` + `ObjectVersionRecord.WriteTo(builder.Payload, ...)`。
- 测试：写 3 条版本记录 → 用 `RbfScanner` 读回 payload → `ObjectVersionRecord.TryParse` 校验 prevPtr/diff。

状态：✅ 已完成（`atelia/src/StateJournal/Core/DataRecordWriter.cs` + `atelia/tests/StateJournal.Tests/Core/DataRecordWriterTests.cs`）。

### T-M2-04（最小 MetaRecordWriter：写入 MetaCommitRecord）
状态：✅ 已完成。
- 写入器：`atelia/src/StateJournal/Core/MetaRecordWriter.cs`
- 复用现有 payload 定义与序列化：`atelia/src/StateJournal/Commit/MetaCommitRecord.cs`
- 测试：`atelia/tests/StateJournal.Tests/Core/MetaRecordWriterTests.cs`

### T-M2-05（RecordReader v0：从 IRbfScanner 扫描并解析 records）
状态：✅ 已完成。
- Meta 读取器：`atelia/src/StateJournal/Core/MetaRecordReader.cs`
  - `ParsedMetaRecord` 结构化输出
  - `ScanReverse()` 逆向扫描
  - `TryReadAt()` 随机读取
- Data 读取器：`atelia/src/StateJournal/Core/DataRecordReader.cs`
  - `ParsedDataRecord` 结构化输出（含 `ObjectKind`、`IsBaseVersion`）
  - `ScanReverse()` 逆向扫描
  - `TryReadAt()` 随机读取
- 测试：`atelia/tests/StateJournal.Tests/Core/MetaRecordReaderTests.cs`（7 用例）
- 测试：`atelia/tests/StateJournal.Tests/Core/DataRecordReaderTests.cs`（7 用例）
- 过滤策略：非目标 FrameTag / 解析失败的帧被 skip；Tombstone 帧产出（Status=Tombstone）。

---

## Phase M3（Workspace.Open + Recovery）建议拆分

### T-M3-01（Workspace.Open: 初始化 meta/data handles + scanners）
- 打通从路径到 `IRbfScanner`/`IRbfFramer` 的对象图。
- 测试：空目录首次打开产生 HeaderFence。

### T-M3-02（Recovery: 按写入序读取 committed state）
- 关键写入序：data durable flush → meta durable flush → FinalizeCommit（由上层 enforce）。
- 测试：模拟中断点（仅内存/文件截断模拟）确认恢复行为。

---

## 运行与验证建议

- Rbf 局部验证：`dotnet test tests/Rbf.Tests/Rbf.Tests.csproj -v q`
- 全量验证（避免 MSBuild node/pipes 偶发）：`dotnet test -m:1 -v q`

---

## 会议记录轮转指针

- 历史记录：`agent-team/meeting/2025-12-27-storage-engine-kickoff.md`（已偏长）
- 继续记录：`agent-team/meeting/2025-12-28-storage-engine-execution-log.md`
