# Workspace 内置存储引擎（移除 ObjectLoaderDelegate）— Execution Log (Rotated)

> 本文件用于继续追加 runSubagent 的输出与执行日志。
> 上一份过长的 kickoff 记录见：`agent-team/meeting/2025-12-27-storage-engine-kickoff.md`
>
> 建议阅读策略：只读尾部增量（`tail`/`sed`），避免把整份日志塞进上下文。
>
> 创建时间：2025-12-28

---

## 航线 SSOT

- Next steps（防压缩）：`agent-team/handoffs/2025-12-28-storage-engine-next-steps-ssot.md`

---

## 追加区

（从这里开始 append。每次 subagent 输出请追加在文件末尾。）

## 2025-12-28 Update — DurableFlush 策略收敛

- 新增证据表明：.NET `FileStream.Flush(true)` 在 Linux 等效 `fsync`，在 macOS 等效 `F_FULLFSYNC`。
- 因此 `IRbfFileBackend.DurableFlush()` 可直接用标准库实现，无需自建平台 P/Invoke 封装。
- 参考：`agent-team/handoffs/2025-12-28-filestream.flush.md`
- 航线文件已更新：`agent-team/handoffs/2025-12-28-storage-engine-next-steps-ssot.md`

---

### Implementer 执行 - T-M2-01（定义 FrameTag 分配与 ObjectVersionRecord payload layout）

**执行时间**: 2025-12-28

#### 1. FrameTag 分配表

| 文件类型 | FrameTag 值 | 说明 | 字节序列 (LE) |
|----------|-------------|------|---------------|
| **Meta File** | | | |
| MetaCommit | `0x00000002` | 提交元数据记录 | `02 00 00 00` |
| **Data File** | | | |
| DictVersion | `0x00010001` | DurableDict 版本记录 | `01 00 01 00` |
| ArrayVersion | `0x00020001` | DurableArray 版本记录（Reserved） | `01 00 02 00` |

**位段编码**（参见 `StateJournalFrameTag.cs`）：
- 低 16 位：`RecordType`（0x01=ObjectVersion, 0x02=MetaCommit）
- 高 16 位：`SubType`（当 RecordType=ObjectVersion 时解释为 ObjectKind）

#### 2. 新增类型/API

**新增文件**：
| 文件 | 说明 |
|------|------|
| [FrameTags.cs](atelia/src/StateJournal/Core/FrameTags.cs) | RbfFileKind 枚举 + FrameTags 静态类（分配表 + 验证方法）|
| [ObjectVersionRecord.cs](atelia/src/StateJournal/Core/ObjectVersionRecord.cs) | ObjectVersionRecord payload layout encode/decode |
| [ObjectVersionRecordTests.cs](atelia/tests/StateJournal.Tests/Core/ObjectVersionRecordTests.cs) | 22 个测试用例 |

**ObjectVersionRecord API**：

```csharp
public static class ObjectVersionRecord {
    const int PrevVersionPtrSize = 8;           // u64 LE
    const int MinPayloadLength = 8;             // [F-OBJVER-PAYLOAD-MINLEN]
    const ulong NullPrevVersionPtr = 0;         // Base Version marker

    // Encode
    static void WriteTo(IBufferWriter<byte> writer, ulong prevVersionPtr, ReadOnlySpan<byte> diffPayload);
    static void WriteTo(IBufferWriter<byte> writer, ulong prevVersionPtr, ReadOnlyMemory<byte> diffPayload);
    static int GetPayloadLength(int diffPayloadLength);

    // Decode
    static AteliaResult<bool> TryParse(ReadOnlySpan<byte> payload, out ulong prevVersionPtr, out ReadOnlySpan<byte> diffPayload);
    static AteliaResult<ParsedObjectVersionRecord> TryParse(ReadOnlyMemory<byte> payload);
    static bool IsBaseVersion(ulong prevVersionPtr);
}

public readonly record struct ParsedObjectVersionRecord(ulong PrevVersionPtr, ReadOnlyMemory<byte> DiffPayload) {
    public bool IsBaseVersion => PrevVersionPtr == 0;
}

public record ObjectVersionRecordTruncatedError(int ActualLength, int MinLength) : AteliaError(...);
```

**Payload 布局**：
```
┌────────────────────────────┬─────────────────────────┐
│ PrevVersionPtr (u64 LE)    │ DiffPayload (N bytes)   │
│ 8 bytes                    │ 剩余全部                 │
└────────────────────────────┴─────────────────────────┘
```

#### 3. 测试覆盖

| 类别 | 测试用例数 | 说明 |
|------|-----------|------|
| Constants | 1 | 验证常量值正确 |
| Roundtrip | 5 | encode→decode 往返（含 Memory 重载）|
| Boundary | 8 | 空 payload / 截断 / 边界长度 |
| Wire Format | 2 | Little-endian 验证 / 偏移量验证 |
| GetPayloadLength | 4 | Theory 测试多组输入 |
| IsBaseVersion | 4 | Theory 测试多组输入 |
| ParsedRecord | 2 | 结构化解析结果 |
| Large Payload | 1 | 1KB diff payload |

#### 4. 测试命令与结果

```bash
# Targeted test
$ dotnet test tests/StateJournal.Tests/StateJournal.Tests.csproj --filter "FullyQualifiedName~ObjectVersionRecordTests" -v q
# → 22/22 passed ✅

# Full StateJournal tests
$ dotnet test tests/StateJournal.Tests/StateJournal.Tests.csproj -v q
# → All passed ✅

# Full suite
$ dotnet test -m:1 -v q
# → All passed ✅
```

#### 5. 范围限制确认

- ✅ 只新增 Core 层文件（FrameTags.cs, ObjectVersionRecord.cs）
- ✅ 不触碰 Workspace/Commit 等上层
- ✅ 不涉及 commit 顺序/flush 细节

---

### Implementer 执行 - T-M2-03a（最小 DataRecordWriter：AppendDictVersion 写入 + 扫描回读测试）

**执行时间**: 2025-12-28

#### 1. 实现摘要

实现最小 Data 写入路径闭环：`DataRecordWriter.AppendDictVersion` 写入 + `RbfScanner.ScanReverse` + `ObjectVersionRecord.TryParse` 回读验证。

#### 2. 新增文件

| 文件 | 说明 |
|------|------|
| [DataRecordWriter.cs](atelia/src/StateJournal/Core/DataRecordWriter.cs) | 封装 IRbfFramer，提供语义化 Data 写入 API |
| [DataRecordWriterTests.cs](atelia/tests/StateJournal.Tests/Core/DataRecordWriterTests.cs) | 8 个测试用例 |

#### 3. DataRecordWriter API

```csharp
public sealed class DataRecordWriter {
    public DataRecordWriter(IRbfFramer framer);
    
    // 写入 DurableDict 版本记录
    public Address64 AppendDictVersion(ulong prevVersionPtr, ReadOnlySpan<byte> diffPayload);
    public Address64 AppendDictVersion(ulong prevVersionPtr, ReadOnlyMemory<byte> diffPayload);
    
    public void Flush();
}
```

**设计要点**：
- 封装 RBF 层 framing 细节（FrameTag、Frame layout）
- 使用 `BeginFrame` 流式写入 + `ObjectVersionRecord.WriteTo` 写入 payload
- 返回 `Address64` 作为版本指针

#### 4. 测试覆盖

| 类别 | 测试用例 | 说明 |
|------|----------|------|
| 基本往返 | `AppendDictVersion_ThreeRecords_ScanReverseReadsAllInOrder` | 3 条记录，逆向扫描验证顺序和内容 |
| 单条记录 | `AppendDictVersion_SingleRecord_CanBeScannedBack` | 最简场景 |
| 空 payload | `AppendDictVersion_EmptyDiffPayload_Succeeds` | Checkpoint Base 空 dict |
| Memory 重载 | `AppendDictVersion_MemoryOverload_WorksEquivalently` | API 一致性 |
| TryReadAt | `AppendDictVersion_ReturnedAddress_CanBeReadBack` | 返回的地址可直接回读 |
| 大 payload | `AppendDictVersion_LargeDiffPayload_Succeeds` | 4KB payload |
| 版本链 | `AppendDictVersion_VersionChain_CanBeTraversedBackward` | 5 条链式记录回溯 |
| Flush | `Flush_DoesNotThrow` | 基本健壮性 |

#### 5. 验收测试结果

```bash
# Targeted test
$ dotnet test tests/StateJournal.Tests/StateJournal.Tests.csproj --filter "FullyQualifiedName~DataRecordWriterTests" -v n
# → 8/8 passed ✅

# Full StateJournal tests
$ dotnet test tests/StateJournal.Tests/StateJournal.Tests.csproj -v q
# → 636/636 passed ✅ (原 628 + 新增 8)
```

#### 6. 范围限制确认

- ✅ 只新增 Core 层文件（DataRecordWriter.cs）
- ✅ 不引入 file-backed（纯内存 ArrayBufferWriter）
- ✅ 不引入 commit/meta 语义
- ✅ 只验证 record framing + payload layout

---
### Implementer 执行 - T-M2-04（最小 MetaCommitRecord + MetaRecordWriter 内存闭环）

**执行时间**: 2025-12-28

#### 1. 实现摘要

实现最小 Meta 写入路径闭环：`MetaRecordWriter.AppendCommit` 写入 `MetaCommitRecord` + `RbfScanner.ScanReverse` + `MetaCommitRecordSerializer.TryRead` 回读验证。

**复用策略**：
- `MetaCommitRecord` 已存在于 `atelia/src/StateJournal/Commit/`，payload 包含 5 个字段：`EpochSeq(varuint)` + `RootObjectId(varuint)` + `VersionIndexPtr(u64 LE)` + `DataTail(u64 LE)` + `NextObjectId(varuint)`
- `MetaCommitRecordSerializer` 已实现 `Write` 和 `TryRead` API
- 新增 `MetaRecordWriter` 封装 framing 逻辑

#### 2. 新增文件

| 文件 | 说明 |
|------|------|
| [MetaRecordWriter.cs](atelia/src/StateJournal/Core/MetaRecordWriter.cs) | 封装 IRbfFramer，提供语义化 Meta 写入 API |
| [MetaRecordWriterTests.cs](atelia/tests/StateJournal.Tests/Core/MetaRecordWriterTests.cs) | 9 个测试用例 |

#### 3. MetaRecordWriter API

```csharp
public sealed class MetaRecordWriter {
    public MetaRecordWriter(IRbfFramer framer);
    
    // 写入 MetaCommitRecord（结构体重载）
    public Address64 AppendCommit(in MetaCommitRecord record);
    
    // 写入 MetaCommitRecord（独立参数重载）
    public Address64 AppendCommit(
        ulong epochSeq,
        ulong rootObjectId,
        ulong versionIndexPtr,
        ulong dataTail,
        ulong nextObjectId);
    
    public void Flush();
}
```

**设计要点**：
- 封装 RBF 层 framing 细节（FrameTag、Frame layout）
- 使用 `BeginFrame(FrameTags.MetaCommit)` + `MetaCommitRecordSerializer.Write` 写入 payload
- 返回 `Address64` 作为 commit 记录地址
- 提供两种重载：结构体（便于传递）和独立参数（便于构造）

#### 4. Payload 布局（复用 MetaCommitRecord）

```
┌────────────────────┬────────────────────┬────────────────────┬────────────────────┬────────────────────┐
│ EpochSeq (varuint) │ RootObjectId       │ VersionIndexPtr    │ DataTail           │ NextObjectId       │
│ 1-10 bytes         │ (varuint)          │ (u64 LE, 8 bytes)  │ (u64 LE, 8 bytes)  │ (varuint)          │
│                    │ 1-10 bytes         │                    │                    │ 1-10 bytes         │
└────────────────────┴────────────────────┴────────────────────┴────────────────────┴────────────────────┘
```

- 最小 payload 大小：19 字节（3 个 varuint 各 1 字节 + 2 个 u64）
- 最大 payload 大小：46 字节（3 个 varuint 各 10 字节 + 2 个 u64）

#### 5. 测试覆盖

| 类别 | 测试用例 | 说明 |
|------|----------|------|
| 基本往返 | `AppendCommit_TwoRecords_ScanReverseReadsAllInOrder` | 2 条记录，逆向扫描验证顺序和内容 |
| 单条记录 | `AppendCommit_SingleRecord_CanBeScannedBack` | 最简场景 |
| 参数重载 | `AppendCommit_IndividualParams_WorksEquivalently` | 独立参数 API 一致性 |
| TryReadAt | `AppendCommit_ReturnedAddress_CanBeReadBack` | 返回的地址可直接回读 |
| 边界值(最小) | `AppendCommit_MinimalValues_Succeeds` | 全零值 |
| 边界值(最大) | `AppendCommit_MaxValues_Succeeds` | ulong.MaxValue |
| 多记录序列 | `AppendCommit_MultipleRecords_MaintainsSequence` | 5 条记录，验证地址递增和逆序 |
| Flush | `Flush_DoesNotThrow` | 基本健壮性 |
| Wire Format | `AppendCommit_FrameTag_IsMetaCommit` | 验证 FrameTag = 0x00000002 |

#### 6. 验收测试结果

```bash
# Targeted test
$ dotnet test tests/StateJournal.Tests/StateJournal.Tests.csproj --filter "FullyQualifiedName~MetaRecordWriterTests" -v n
# → 9/9 passed ✅

# Full StateJournal tests
$ dotnet test tests/StateJournal.Tests/StateJournal.Tests.csproj -v q
# → 645/645 passed ✅ (原 636 + 新增 9)
```

#### 7. 范围限制确认

- ✅ 只新增 Core 层文件（MetaRecordWriter.cs）
- ✅ 复用现有 MetaCommitRecord/Serializer（不重新定义 payload layout）
- ✅ 不引入 file-backed（纯内存 ArrayBufferWriter）
- ✅ 不涉及 Workspace.Open / durable commit 顺序
- ✅ 只验证 record framing + payload roundtrip

---