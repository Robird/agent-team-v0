# Stage 03: 简单写入路径（Append）

> **目标**：实现 `IRbfFile.Append(tag, payload)` 完整帧写入。
> **验收标准**：单元测试覆盖正常写入、多帧追加、边界条件。

---

## 上下文文件

- **前情提要**：`wish/W-0009-rbf/recap.md`
- **实现蓝图**：`wish/W-0009-rbf/blueprint.md`
- **接口规范**：`atelia/docs/Rbf/rbf-interface.md` - @[A-RBF-IRBFFILE-SHAPE]
- **格式规范**：`atelia/docs/Rbf/rbf-format.md` - §3 Wire Layout、§4 CRC32C
- **实现指南**：`atelia/docs/Rbf/rbf-type-bone.md` - §5 RandomAccessByteSink

---

## 规范要点摘要

### FrameBytes 布局（来自 rbf-format.md @[F-FRAMEBYTES-LAYOUT]）

| 偏移 | 字段 | 类型 | 长度 | 说明 |
|------|------|------|------|------|
| 0 | HeadLen | u32 LE | 4 | FrameBytes 总长度（不含 Fence） |
| 4 | FrameTag | u32 LE | 4 | 帧类型标识符 |
| 8 | Payload | bytes | N | 业务数据 |
| 8+N | FrameStatus | bytes | 1-4 | 帧状态（见下） |
| 8+N+StatusLen | TailLen | u32 LE | 4 | MUST 等于 HeadLen |
| 12+N+StatusLen | CRC32C | u32 LE | 4 | 校验和 |

### StatusLen 计算（来自 @[F-STATUSLEN-ENSURES-4B-ALIGNMENT]）
```
StatusLen = 1 + ((4 - ((PayloadLen + 1) % 4)) % 4)
```
即：确保 Payload + FrameStatus 总长度 4B 对齐。

### FrameStatus 位域（来自 @[F-FRAMESTATUS-RESERVED-BITS-ZERO]）
- Bit 7: Tombstone（0 = Valid）
- Bit 6-2: Reserved（MUST 为 0）
- Bit 1-0: StatusLen 减 1

### CRC32C 覆盖范围（来自 @[F-CRC32C-COVERAGE]）
```
CRC32C = crc32c(FrameTag + Payload + FrameStatus + TailLen)
```
**不覆盖**：HeadLen、CRC32C 本身、Fence。

### 写入后结构（来自 rbf-decisions.md @[F-FENCE-IS-SEPARATOR-NOT-FRAME]）
每帧写入后 MUST 紧跟一个 Fence：
```
[Fence][FrameBytes][Fence][FrameBytes][Fence]...
```

### SizedPtr 返回值（来自 @[S-RBF-SIZEDPTR-WIRE-MAPPING]）
- `OffsetBytes` = FrameBytes 起点（HeadLen 字段位置）
- `LengthBytes` = HeadLen 字段值

---

## Task 列表

### Task 3.1: 实现 CRC32C 工具函数 + 单元测试

**执行者**：Implementer

**任务简报**：
创建 CRC32C 计算工具类（封装 .NET 的 `BitOperations.Crc32C`）并编写单元测试。

**规范引用**：
- `rbf-format.md` @[F-CRC-IS-CRC32C-CASTAGNOLI-REFLECTED]

**产出**：
1. `atelia/src/Rbf/Internal/Crc32CHelper.cs` - 包含：
   - `internal static class Crc32CHelper`
   - `public static uint Compute(ReadOnlySpan<byte> data)` - 计算 CRC32C
     - 初始值：`0xFFFFFFFF`
     - **性能优化**：优先用 `ulong` 重载处理 8 字节块，再用 `uint` 重载，剩余用 `byte` 重载：
       ```csharp
       unsafe {
           fixed (byte* ptr = data) {
               int i = 0;
               while (i + 8 <= data.Length) {
                   crc = BitOperations.Crc32C(crc, *(ulong*)(ptr + i));
                   i += 8;
               }
               if (i + 4 <= data.Length) { // 长度[0-7] 无需while
                   crc = BitOperations.Crc32C(crc, *(uint*)(ptr + i));
                   i += 4;
               }
               while (i < data.Length) {
                   crc = BitOperations.Crc32C(crc, data[i++]);
               }
           }
       }
       ```
     - 最终异或：`0xFFFFFFFF`

2. `atelia/tests/Rbf.Tests/Crc32CHelperTests.cs` - 包含：
   - `Compute_EmptyInput_ReturnsExpected()` - 空输入测试
   - `Compute_KnownVector_ReturnsExpected()` - 已知测试向量（如 "123456789" → 0xE3069283）
   - `Compute_4ByteAligned_Efficient()` - 验证 4 字节对齐输入（RBF 典型场景）

**验收**：编译通过 + `dotnet test` 测试通过

---

### Task 3.2: 实现 FrameStatus 编码函数 + 单元测试

**执行者**：Implementer

**任务简报**：
创建 FrameStatus 编码工具（计算 StatusLen 并填充字节）并编写单元测试。

**规范引用**：
- `rbf-format.md` @[F-STATUSLEN-ENSURES-4B-ALIGNMENT]
- `rbf-format.md` @[F-FRAMESTATUS-RESERVED-BITS-ZERO]
- `rbf-format.md` @[F-FRAMESTATUS-FILL]

**产出**：
1. `atelia/src/Rbf/Internal/FrameStatusHelper.cs` - 包含：
   - `internal static class FrameStatusHelper`
   - `public static int ComputeStatusLen(int payloadLen)` - 计算 StatusLen
     - 公式：`StatusLen = 1 + ((4 - ((payloadLen + 1) % 4)) % 4)`
   - `public static byte EncodeStatusByte(bool isTombstone, int statusLen)` - 编码单字节
   - `public static void FillStatus(Span<byte> dest, bool isTombstone, int statusLen)` - 填充全部状态字节

2. `atelia/tests/Rbf.Tests/FrameStatusHelperTests.cs` - 包含：
   - `ComputeStatusLen_PayloadLen0_Returns4()` - payload=0, statusLen=4
   - `ComputeStatusLen_PayloadLen1_Returns3()` - payload=1, statusLen=3
   - `ComputeStatusLen_PayloadLen2_Returns2()` - payload=2, statusLen=2
   - `ComputeStatusLen_PayloadLen3_Returns1()` - payload=3, statusLen=1
   - `ComputeStatusLen_PayloadLen4_Returns4()` - payload=4, statusLen=4（循环）
   - `EncodeStatusByte_NotTombstone_CorrectBits()` - 验证位域编码
   - `EncodeStatusByte_Tombstone_CorrectBits()` - 验证 Tombstone 位

**验收**：编译通过 + `dotnet test` 测试通过

---

### Task 3.3: 实现帧序列化辅助函数

**执行者**：Implementer
**依赖**：Task 3.1, Task 3.2

**任务简报**：
在 `RbfRawOps` 中添加帧序列化辅助方法，用于计算 HeadLen 和序列化完整帧。

**规范引用**：
- `rbf-format.md` @[F-FRAMEBYTES-LAYOUT]
- `rbf-format.md` @[F-CRC32C-COVERAGE]

**产出**：
1. 修改 `atelia/src/Rbf/Internal/RbfRawOps.cs`，添加：
   - `public static int ComputeHeadLen(int payloadLen)` - 计算 FrameBytes 总长度
     - 公式：`HeadLen = 4 + 4 + payloadLen + statusLen + 4 + 4`
     - 即：HeadLen(4) + Tag(4) + Payload(N) + Status(1-4) + TailLen(4) + CRC(4)
   - `public static int SerializeFrame(Span<byte> dest, uint tag, ReadOnlySpan<byte> payload, bool isTombstone = false)` - 序列化完整 FrameBytes
     - 返回值：实际写入字节数（等于 HeadLen）
     - 写入顺序：HeadLen → Tag → Payload → Status → TailLen → CRC
     - **重要**：只写入 FrameBytes，**不写入前置 Fence**（Fence 由调用方处理）

**CRC 输入切片公式（@[F-CRC32C-COVERAGE] 可执行化）**：
```csharp
// CRC 覆盖从 Tag 到 TailLen（不含 HeadLen 和 CRC 本身）
var crcInput = dest[4..(headLen-4)];  // Tag(4) + Payload(N) + Status(1-4) + TailLen(4)
var crc = Crc32CHelper.Compute(crcInput);
BinaryPrimitives.WriteUInt32LittleEndian(dest[(headLen-4)..], crc);
```
**注意**：CRC 不覆盖 HeadLen（前 4 字节）、不覆盖 CRC 本身、不覆盖 Fence。

**验收**：编译通过

---

### Task 3.4: 实现 RbfFileImpl.Append

**执行者**：Implementer
**依赖**：Task 3.3

**任务简报**：
实现 `Append(tag, payload)` 方法，写入完整帧并返回 SizedPtr。

**规范引用**：
- `rbf-interface.md` @[A-RBF-IRBFFILE-SHAPE]
- `rbf-decisions.md` @[F-FENCE-IS-SEPARATOR-NOT-FRAME] - 帧后写 Fence
- `rbf-format.md` @[S-RBF-SIZEDPTR-WIRE-MAPPING]
- `rbf-format.md` @[F-FENCE-RBF1-ASCII-4B] - Fence 必须是 ASCII `RBF1`

**产出**：
1. 修改 `atelia/src/Rbf/Internal/RbfFileImpl.cs`，实现 `Append` 方法：
   - 计算 HeadLen，分配临时 buffer
   - 调用 `RbfRawOps.SerializeFrame` 序列化帧
   - 调用 `RandomAccess.Write` 写入 FrameBytes
   - 写入 Fence（**必须使用 `RbfConstants.Fence`**，即 ASCII `RBF1`）
   - 构造 `SizedPtr`（offset = 当前 TailOffset，length = HeadLen）
   - 更新 `_tailOffset += HeadLen + FenceLength`
   - 返回 `SizedPtr`

**Buffer 策略**：
- **阈值**：`const int MaxStackAllocSize = 512;`
- payload + 固定开销 ≤ 512 → `stackalloc`
- 否则 → `ArrayPool<byte>.Shared.Rent()` + 用完归还

**关键约束**：
- 帧后 MUST 写入 Fence（使用 `RbfConstants.Fence`，不是任意 4 字节）
- SizedPtr.Offset 指向 HeadLen 字段位置
- **TailOffset 更新条件**：FrameBytes 写入成功 **且** Fence 写入成功后才更新；否则保持不变

**验收**：编译通过

---

### Task 3.5: 编写 Append 单元测试

**执行者**：Implementer
**依赖**：Task 3.4

**任务简报**：
为 Append 方法编写单元测试。

**产出**：
1. `atelia/tests/Rbf.Tests/RbfAppendTests.cs` - 包含：
   - `Append_SingleFrame_WritesCorrectFormat()` - 验证单帧写入：
     - TailOffset 正确更新
     - SizedPtr 返回值正确
     - 文件内容符合规范（可读取文件字节验证）
   - `Append_MultipleFrames_AppendSequentially()` - 验证多帧追加：
     - 第二帧 offset = 4 + headLen1 + 4（HeaderFence + 第一帧 + Fence）
     - 所有帧后都有 Fence（验证 Fence 字节 = `0x52 0x42 0x46 0x31`）
   - `Append_EmptyPayload_Succeeds()` - 验证空 payload 场景
   - `Append_LargePayload_Succeeds()` - 验证大 payload（>1KB）

**4B 对齐根不变量断言**（@[S-RBF-DECISION-4B-ALIGNMENT-ROOT]）：
每个测试 MUST 验证：
- `SizedPtr.OffsetBytes % 4 == 0`
- `SizedPtr.LengthBytes % 4 == 0`（即 HeadLen 是 4 的倍数）
- `TailOffset % 4 == 0`

**HeadLen/TailLen 对称性断言**（@[F-FRAMEBYTES-LAYOUT]）：
- 读取文件字节，验证 HeadLen 字段值 == TailLen 字段值 == SizedPtr.LengthBytes

**Fence 验证**（@[F-FENCE-RBF1-ASCII-4B]）：
- 验证 HeaderFence 和每帧后的 Fence 都是 `0x52 0x42 0x46 0x31`

**测试策略**：
- 使用临时文件
- 写入后读取文件字节，验证布局正确
- 验证 CRC 可手动计算对比

**验收**：`dotnet test` 全部通过

---

## 执行记录

| Task | 状态 | 执行者 | 备注 |
|------|------|--------|------|
| 3.1 | ✅ 完成 | Implementer | CRC32C + 23 tests, 审阅后修复非对齐读 |
| 3.2 | ✅ 完成 | Implementer | FrameStatus + 49 tests, 审阅后添加值域校验 |
| 3.3 | ✅ 完成 | Implementer | SerializeFrame, 审阅通过 |
| 3.4 | ✅ 完成 | Implementer | RbfFileImpl.Append, 审阅通过 |
| 3.5 | ✅ 完成 | Implementer | Append 集成测试 4 cases, 审阅通过 |

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-14 | Stage 03 完成：5 个 Task 全部完成，84 个测试通过 |
| 2026-01-14 | Task 3.1 完成：Crc32CHelper.cs + Crc32CHelperTests.cs（12 测试） |
| 2026-01-14 | 性能优化修订：CRC32C 改用 ulong/uint/byte 三级重载；合并简单 helper + 测试为单 Task（3.1/3.2）；明确 SerializeFrame 不写前置 Fence；Task 数量优化为 5 个 |
| 2026-01-14 | Review 修订：CRC 切片公式明确化、Fence 字节值约束、4B 对齐断言、Buffer 阈值 512B、TailOffset 更新条件明确 |
| 2026-01-14 | 初始版本：6 个 Task |
