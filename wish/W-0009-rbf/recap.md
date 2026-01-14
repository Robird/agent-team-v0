# RBF 实现前情提要

> 本文档记录 RBF 实现的当前状态和已完成的交付成果。
> **维护者**：每个 Task 完成后由执行者更新。

---

## 当前状态

**阶段**：Stage 03 - 简单写入路径（Append） ✅ **已完成**

**下一阶段**：Stage 04 - 随机读取（ReadFrame）

**测试覆盖**：84 个测试全部通过

**基础条件**：
- 设计文档已就绪：`atelia/docs/Rbf/` 目录下 7 个文档
- 核心依赖已存在于 `atelia/src/Data/`：
  - `SizedPtr.cs` - 帧位置凭据
  - `IByteSink.cs` - 推式写入接口
  - `IReservableBufferWriter.cs` - 可预留的 BufferWriter 接口
  - `SinkReservableWriter.cs` - 基于 IByteSink 的 IReservableBufferWriter 实现
- `Atelia.Primitives` 中的 `AteliaResult<T>` 可用

---

## 设计文档速查

| 文档 | 层级 | 要点 |
|------|------|------|
| [rbf-decisions.md](../../atelia/docs/Rbf/rbf-decisions.md) | Decision | 8 个关键决策（不可修改） |
| [rbf-interface.md](../../atelia/docs/Rbf/rbf-interface.md) | Shape | `IRbfFile` 门面 + 公开类型契约 |
| [rbf-format.md](../../atelia/docs/Rbf/rbf-format.md) | Layer 0 | 二进制线格式（Fence, FrameBytes, CRC） |
| [rbf-type-bone.md](../../atelia/docs/Rbf/rbf-type-bone.md) | Plan | 实现指南（RbfRawOps, RandomAccessByteSink） |
| [testing-pattern.md](wish/W-0009-rbf/testing-pattern.md) | Craft | 测试模式（RbfRawOpsTests, RbfFacadeTests） |

---

## 已完成的交付成果

### Stage 03: 简单写入路径（Append）（2026-01-14）

**新增内部辅助类**：
- `Internal/Crc32CHelper.cs` - CRC32C 计算（ulong/uint/byte 三级优化，使用 `Unsafe.ReadUnaligned` 保证跨平台安全）
- `Internal/FrameStatusHelper.cs` - FrameStatus 编码（StatusLen 计算 + 位域编码 + 值域校验）

**RbfRawOps 新增方法**：
- `ComputeHeadLen(payloadLen)` - 计算 FrameBytes 总长度
- `SerializeFrame(dest, tag, payload, isTombstone)` - 序列化完整 FrameBytes（不含 Fence）

**RbfFileImpl.Append 实现**：
- Buffer 策略：≤512B stackalloc，否则 ArrayPool
- 写入顺序：FrameBytes → Fence
- SizedPtr 返回正确的 offset/length 映射
- TailOffset 在两次写入成功后更新

**测试覆盖**（新增 76 个测试，共 84 个）：
- CRC32CHelper：23 个测试（含 RFC 3720 向量 + baseline 对比）
- FrameStatusHelper：49 个测试（含值域校验异常）
- RbfAppendTests：4 个集成测试（单帧/多帧/空 payload/大 payload）

**关键不变量验证**：
- 4B 对齐根不变量（@[S-RBF-DECISION-4B-ALIGNMENT-ROOT]）
- HeadLen/TailLen 对称性（@[F-FRAMEBYTES-FIELD-OFFSETS]）
- Fence 字节验证（@[F-FENCE-VALUE-IS-RBF1-ASCII-4B]）
- CRC32C 覆盖范围（@[F-CRC32C-COVERAGE]）

### Stage 02: 常量与 Fence（Genesis）（2026-01-14）

**新增类型**：
- `Internal/RbfConstants.cs` - Fence 常量（`internal`）
- `Internal/RbfFileImpl.cs` - `IRbfFile` 内部实现类

**工厂方法实现**：
- `RbfFile.CreateNew(path)` - 创建新文件，写入 Genesis Fence
- `RbfFile.OpenExisting(path)` - 打开已有文件，验证 Genesis + 4B 对齐

### Stage 01: 项目骨架与类型骨架（2026-01-14）

**项目结构**：
- `atelia/src/Rbf/Rbf.csproj` - 主项目
- `atelia/tests/Rbf.Tests/Rbf.Tests.csproj` - xUnit 测试项目

**公开类型骨架**（方法体 `NotImplementedException` 占位）：
- `IRbfFile.cs` - 接口定义
- `RbfFile.cs` - 静态工厂（`CreateNew`, `OpenExisting`）
- `RbfFrame.cs` - `readonly ref struct`
- `RbfFrameBuilder.cs` - `ref struct : IDisposable`
- `RbfReverseSequence.cs` - `ref struct`
- `RbfReverseEnumerator.cs` - `ref struct`

**内部类型骨架**：
- `Internal/RbfRawOps.cs` - 静态类
- `Internal/RandomAccessByteSink.cs` - `IByteSink` 适配器（**已完整实现**）

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-14 | Stage 03 完成：Append 实现 + 76 个新测试 |
| 2026-01-14 | Stage 02 完成：常量与 Fence（Genesis） |
| 2026-01-14 | Stage 01 完成：项目骨架与类型骨架 |
| 2026-01-14 | 初始版本：记录基础条件 |
