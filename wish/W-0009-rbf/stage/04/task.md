# Stage 04: 随机读取（ReadFrame）

> **目标**：实现 `IRbfFile.ReadFrame(SizedPtr)` 和底层 `RbfRawOps.ReadFrame()`。
> **前置**：Stage 03 完成（Append 可写入有效帧供读取测试）

---

## 规范引用

| 条款 | 文档 | 要点 |
|------|------|------|
| @[A-RBF-IRBFFILE-SHAPE] | rbf-interface.md | `ReadFrame(SizedPtr) → AteliaResult<RbfFrame>` |
| @[A-RBF-FRAME-STRUCT] | rbf-interface.md | RbfFrame 结构定义 |
| @[F-FRAMEBYTES-LAYOUT] | rbf-format.md | FrameBytes 布局 |
| @[F-CRC32C-COVERAGE] | rbf-format.md | CRC 覆盖范围 |
| @[F-FRAMING-FAIL-REJECT] | rbf-format.md | Framing 校验失败策略 |
| @[F-CRC-FAIL-REJECT] | rbf-format.md | CRC 校验失败策略 |
| @[F-FRAMESTATUS-RESERVED-BITS-ZERO] | rbf-format.md | FrameStatus 位域 |
| @[F-FRAMESTATUS-FILL] | rbf-format.md | FrameStatus 全字节同值 |
| @[S-RBF-SIZEDPTR-WIRE-MAPPING] | rbf-format.md | SizedPtr 映射规则 |

---

## 设计决策

### Decision 4.A: ReadFrame 不校验 Fence

**结论**：ReadFrame **不校验**前置/后置 Fence。

**理由**：
1. @[F-FRAMING-FAIL-REJECT] 后半句"并按 @[R-RESYNC-SCAN-BACKWARD-4B-TO-HEADER-FENCE] 进入 Resync"是 **ScanReverse 专属行为**
2. ReadFrame 是**已知位置的随机读**（SizedPtr 来自 Append 返回或 ScanReverse 产出），调用方已知帧位置
3. 校验 Fence 需要**额外 I/O**（前 4B + 后 4B），对小帧（20B）影响达 40%
4. @[S-RBF-SIZEDPTR-WIRE-MAPPING] 明确 SizedPtr 指向 FrameBytes 起点，**不含 Fence**

**Framing 校验清单**（ReadFrame 适用）：
- [ ] HeadLen == ptr.LengthBytes
- [ ] TailLen == HeadLen
- [ ] FrameStatus 保留位为零（@[F-FRAMESTATUS-RESERVED-BITS-ZERO]）
- [ ] FrameStatus 全字节同值（@[F-FRAMESTATUS-FILL]）
- [ ] StatusLen 与 PayloadLen 匹配（推导一致性）
- [ ] CRC32C 校验通过（@[F-CRC32C-COVERAGE]）

---

### Decision 4.B: 错误码分层设计

**结论**：采用**分层聚合**模式——对上层暴露大类，内部携带诊断信息。

**错误码定义**：

| 错误码 | 场景 | 说明 |
|--------|------|------|
| `ArgumentError` | 参数校验失败 | Offset/Length 非 4B 对齐、Length < 20、越界 |
| `FramingError` | Framing 校验失败 | HeadLen/TailLen 不匹配、Status 异常 |
| `CrcMismatch` | CRC 校验失败 | 数据损坏 |

**I/O 错误处理**：让 `IOException` 自然传播（由上层统一处理），不转换为 `AteliaResult` 错误码。

**最小帧长度**：20B = HeadLen(4) + Tag(4) + Status(4) + TailLen(4) + CRC(4)（PayloadLen=0 时 StatusLen=4）

---

## 实现任务

### Task 4.1: 实现 FrameStatus 解码辅助函数 + 测试

**执行者**：Implementer
**依赖**：无

**任务简报**：
在 `FrameStatusHelper` 中添加解码方法，并编写对应测试。

**产出**：

1. 修改 `atelia/src/Rbf/Internal/FrameStatusHelper.cs`，添加：

```csharp
/// <summary>从 FrameStatus 字节解码信息。</summary>
/// <param name="statusByte">FrameStatus 的任意一个字节（@[F-FRAMESTATUS-FILL] 保证全字节同值）。</param>
/// <param name="isTombstone">输出：是否为墓碑帧（Bit7）。</param>
/// <param name="statusLen">输出：StatusLen（1-4，来自 Bit1-0 + 1）。</param>
/// <returns>true 如果解码成功（保留位 Bit6-2 为零），false 如果保留位非零。</returns>
internal static bool TryDecodeStatusByte(byte statusByte, out bool isTombstone, out int statusLen);

/// <summary>验证 FrameStatus 区域的所有字节是否一致（@[F-FRAMESTATUS-FILL]）。</summary>
internal static bool ValidateStatusBytesConsistent(ReadOnlySpan<byte> statusRegion);
```

2. 修改 `atelia/tests/Rbf.Tests/FrameStatusHelperTests.cs`，添加：
   - `TryDecodeStatusByte_ValidByte_ReturnsTrue()` - 正常解码
   - `TryDecodeStatusByte_ReservedBitsNonZero_ReturnsFalse()` - 保留位非零
   - `TryDecodeStatusByte_Tombstone_DecodesCorrectly()` - 墓碑帧
   - `ValidateStatusBytesConsistent_AllSame_ReturnsTrue()` - 字节一致
   - `ValidateStatusBytesConsistent_Different_ReturnsFalse()` - 字节不一致

**验收**：`dotnet test --filter FrameStatus` 通过

---

### Task 4.2: 实现 RbfRawOps.ReadFrame + RbfFileImpl 转发

**执行者**：Implementer
**依赖**：Task 4.1

**任务简报**：
实现 `RbfRawOps.ReadFrame(SafeFileHandle, SizedPtr)` 核心逻辑，以及 `RbfFileImpl.ReadFrame` 转发。

**产出**：

1. 修改 `atelia/src/Rbf/Internal/RbfRawOps.ReadFrame.cs`：

```csharp
public static AteliaResult<RbfFrame> ReadFrame(SafeFileHandle file, SizedPtr ptr) {
    // 1. 参数校验
    //    - ptr.OffsetBytes % 4 == 0（4B 对齐）
    //    - ptr.LengthBytes % 4 == 0（4B 对齐）
    //    - ptr.LengthBytes >= 20（最小帧长度 = 16 + 4）
    
    // 2. 分配 buffer
    //    - const int MaxStackAllocSize = 4096;（与 RbfAppendImpl 一致）
    //    - ptr.LengthBytes <= MaxStackAllocSize → stackalloc
    //    - 否则 → ArrayPool<byte>.Shared.Rent()
    
    // 3. RandomAccess.Read 读取整个 FrameBytes
    //    - 若 bytesRead < expected，返回 ArgumentError（短读/越界）
    
    // 4. Framing 校验
    //    - HeadLen（buffer[0..4]）== ptr.LengthBytes
    //    - 从 statusByte 推导 StatusLen：statusByte = buffer[headLen - 9]
    //    - TryDecodeStatusByte() 验证保留位
    //    - ValidateStatusBytesConsistent() 验证全字节同值
    //    - TailLen（buffer[headLen-8..headLen-4]）== HeadLen
    
    // 5. CRC 校验
    //    - crcInput = buffer[4..(headLen-4)]（Tag + Payload + Status + TailLen）
    //    - expected = BinaryPrimitives.ReadUInt32LittleEndian(buffer[(headLen-4)..])
    //    - computed = Crc32CHelper.Compute(crcInput)
    
    // 6. 构造 RbfFrame 并返回
    //    - payloadLen = headLen - RbfConstants.FrameFixedOverheadBytes - statusLen
    //    - payload = buffer[8..(8+payloadLen)]
}
```

2. 修改 `atelia/src/Rbf/Internal/RbfFileImpl.cs`：

```csharp
public AteliaResult<RbfFrame> ReadFrame(SizedPtr ptr) {
    return RbfRawOps.ReadFrame(_handle, ptr);
}
```

**PayloadLen 反推公式**（无循环依赖）：
```
statusByteOffset = headLen - 9      // TailLen(4) + CRC(4) + 最后一个 status 字节(1)
statusByte = buffer[statusByteOffset]
statusLen = (statusByte & 0x03) + 1
payloadLen = headLen - 16 - statusLen  // 16 = FrameFixedOverheadBytes
```

**验收**：编译通过

---

### Task 4.3: 编写 ReadFrame 完整测试套件

**执行者**：Implementer
**依赖**：Task 4.2

**任务简报**：
按照 `testing-pattern.md` 的分层策略编写测试。

**产出**：

1. 创建 `atelia/tests/Rbf.Tests/RbfReadFrameTests.cs` - RawOps 层格式验证

**正常路径测试**：
- `ReadFrame_ValidFrame_ReturnsCorrectData()` - 正常读取，验证 Tag/Payload/IsTombstone
- `ReadFrame_EmptyPayload_Succeeds()` - 空 payload（StatusLen=4）
- `ReadFrame_LargePayload_Succeeds()` - 大 payload（>4KB，触发 ArrayPool）
- `ReadFrame_Tombstone_DecodesCorrectly()` - 墓碑帧解码

**参数错误测试**：
- `ReadFrame_MisalignedOffset_ReturnsArgumentError()` - Offset 非 4B 对齐
- `ReadFrame_MisalignedLength_ReturnsArgumentError()` - Length 非 4B 对齐
- `ReadFrame_FrameTooShort_ReturnsArgumentError()` - Length < 20
- `ReadFrame_OutOfRange_ReturnsArgumentError()` - Offset 越界（短读）

**Framing 错误测试**：
- `ReadFrame_HeadLenMismatch_ReturnsFramingError()` - HeadLen ≠ ptr.Length
- `ReadFrame_TailLenMismatch_ReturnsFramingError()` - TailLen ≠ HeadLen
- `ReadFrame_InvalidStatusReservedBits_ReturnsFramingError()` - 保留位非零
- `ReadFrame_StatusBytesInconsistent_ReturnsFramingError()` - Status 字节不一致

**CRC 错误测试**：
- `ReadFrame_CrcMismatch_ReturnsCrcMismatch()` - CRC 损坏

2. 修改 `atelia/tests/Rbf.Tests/RbfFacadeTests.cs` - Facade 层集成

- `ReadFrame_AfterAppend_ReturnsCorrectFrame()` - Append → ReadFrame 闭环
- `ReadFrame_DoesNotChangeTailOffset()` - ReadFrame 后 TailOffset 不变

**测试策略**：
- 正常路径：使用 Append 写入已知帧，然后 ReadFrame 验证
- 损坏测试：直接构造 byte[] 并用 File.WriteAllBytes 写入，或使用 MemoryMappedFile 修改特定字节

**验收**：`dotnet test --filter ReadFrame` 全部通过

---

## 执行记录

| Task | 状态 | 执行者 | 备注 |
|------|------|--------|------|
| 4.1 | ✅ 完成 | Implementer | FrameStatus 解码 + 测试（73 tests） |
| 4.2 | ✅ 完成 | Implementer | ReadFrame 核心实现 + Facade 转发 |
| 4.3 | ✅ 完成 | Implementer | 完整测试套件（20 tests） |

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-15 | v3：所有 Task 完成，Stage 04 交付 |
| 2026-01-15 | v2：整合审阅反馈，合并任务（6→3），添加设计决策 |
| 2026-01-15 | 初始版本：6 个 Task |
