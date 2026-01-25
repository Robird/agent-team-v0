# RBF 实现前情提要

> 本文档记录 RBF 实现的当前状态和已完成的交付成果。
> **维护者**：每个 Task 完成后由执行者更新。

---

## 当前状态

**阶段**：Stage 06 - 帧布局 v0.40 + ScanReverse ✅ **已完成**

**下一阶段**：Stage 07 - 复杂写入路径（BeginAppend/EndAppend）

**测试覆盖**：197 个 RBF 测试 + 60 个 Primitives 测试 + 117 个 Data 测试（全部通过）

**基础条件**：
- 设计文档已就绪：`atelia/docs/Rbf/` 目录下 7 个文档（已同步至 v0.40）
- 核心依赖已存在于 `atelia/src/Data/`：
  - `SizedPtr.cs` - 帧位置凭据（v2: `Offset`/`Length` 使用 `long`/`int`）
  - `IByteSink.cs` - 推式写入接口
  - `IReservableBufferWriter.cs` - 可预留的 BufferWriter 接口
  - `SinkReservableWriter.cs` - 基于 IByteSink 的 IReservableBufferWriter 实现
  - `RollingCrc.cs` - Codeword 保护（SealCodewordBackward/CheckCodewordBackward）
- `Atelia.Primitives` 中的结果类型家族：
  - `AteliaResult<T>` - 标准结果类型（ref struct）
  - `DisposableAteliaResult<T>` - 带资源所有权的结果类型
  - `IAteliaResult<T>` - 公共接口契约

---

## 设计文档速查

| 文档 | 层级 | 要点 |
|------|------|------|
| [rbf-decisions.md](../../atelia/docs/Rbf/rbf-decisions.md) | Decision | 8 个关键决策（不可修改） |
| [rbf-interface.md](../../atelia/docs/Rbf/rbf-interface.md) | Shape | `IRbfFile` 门面 + 公开类型契约（v0.30: 新增 `IRbfFrame`/`RbfPooledFrame`） |
| [rbf-format.md](../../atelia/docs/Rbf/rbf-format.md) | Layer 0 | 二进制线格式（Fence, FrameBytes, CRC） |
| [rbf-type-bone.md](../../atelia/docs/Rbf/rbf-type-bone.md) | Plan | 实现指南（v0.5: `RbfReadImpl`/`RbfWriteImpl` 分离） |
| [testing-pattern.md](testing-pattern.md) | Craft | 测试分层策略（职责分离模式） |

---

## 代码结构速查

### 公开类型 (`src/Rbf/`)

| 文件 | 职责 |
|------|------|
| `IRbfFile.cs` | 文件门面接口（含 ScanReverse） |
| `IRbfFrame.cs` | 帧公共属性契约（`Ticket`, `Tag`, `Payload`, `IsTombstone`） |
| `RbfFrame.cs` | ref struct 帧实现（生命周期受限于 buffer） |
| `RbfPooledFrame.cs` | class 帧实现（持有 ArrayPool buffer，需 Dispose） |
| `RbfFrameInfo.cs` | 帧元信息（ScanReverse 返回的轻量描述符） |
| `RbfReverseEnumerator.cs` | 逆向扫描枚举器 |
| `RbfReverseSequence.cs` | 逆向扫描序列（支持 foreach） |
| `RbfFile.cs` | 工厂方法（CreateNew/OpenExisting） |

### 内部类型 (`Internal/`)

| 文件 | 职责 |
|------|------|
| `RbfLayout.cs` | 布局常量 + FrameLayout 计算 + ResultFromTrailer |
| `RbfFileImpl.cs` | `IRbfFile` Facade 实现（状态管理） |
| `RbfAppendImpl.cs` | Append 核心实现（v0.40: 双 CRC, Tag 在 Trailer） |
| `RbfReadImpl.cs` | ReadFrame + ReadTrailerBefore 核心实现 |
| `TrailerCodewordHelper.cs` | TrailerCodeword 编解码（v0.40 新增） |
| `Crc32CHelper.cs` | CRC32C 计算（Init/Update/Finalize） |
| `RbfErrors.cs` | 错误码定义（含 `RbfBufferTooSmallError`） |
| `RandomAccessByteSink.cs` | `IByteSink` 适配器 |

### 测试分层 (`tests/Rbf.Tests/`)

| 文件 | 职责 | 验证内容 |
|------|------|----------|
| `RbfFacadeTests.cs` | Facade 状态测试 | TailOffset 更新、ReadFrame/ReadPooledFrame 集成 |
| `RbfAppendImplTests.cs` | Append 格式验证 | HeadLen/CRC/Fence/对齐 |
| `RbfReadImplTests.cs` | ReadFrame 格式验证 | Framing 校验/CRC/Buffer 错误/边界值 |
| `ReadTrailerBeforeTests.cs` | ReadTrailerBefore 测试 | 正常/Fence损坏/CRC损坏/TailLen越界 |
| `RbfScanReverseTests.cs` | ScanReverse 集成测试 | 逆向遍历/Tombstone过滤/损坏停止 |
| `TrailerCodewordHelperTests.cs` | TrailerCodeword 测试 | 端序/位布局/CRC |
| `RbfPooledFrameTests.cs` | RbfPooledFrame 生命周期 | Dispose 行为/异常后资源释放 |
| `RbfFileFactoryTests.cs` | 工厂方法测试 | CreateNew/OpenExisting |
| `Crc32CHelperTests.cs` | CRC 工具测试 | RFC 向量 + baseline 对比 |

### Benchmark (`benchmarks/Rbf.Benchmarks/`)

性能基准测试项目，用于验证 Append 优化效果。

---

## 已完成的交付成果

### Stage 06: 帧布局 v0.40 + ScanReverse（2026-01-24）

**核心变更**：

1. **帧布局 v0.40**（Breaking Change）：
   - 旧：`[HeadLen][Tag][Payload][Status][TailLen][CRC]`
   - 新：`[HeadLen][Payload][TailMeta][Padding][PayloadCrc][TrailerCodeword(16B)]`
   - TrailerCodeword = `[TrailerCrc(4)][FrameDescriptor(4)][FrameTag(4)][TailLen(4)]`
   - Tag 从头部移至 TrailerCodeword（支持 ScanReverse 单次读取）

2. **双 CRC 机制**：
   - PayloadCrc32C：覆盖 Payload + TailMeta + Padding，LE 存储
   - TrailerCrc32C：覆盖 FrameDescriptor + FrameTag + TailLen，BE 存储
   - 使用 RollingCrc.SealCodewordBackward 实现后向 CRC

3. **ScanReverse 实现**：
   - `ReadTrailerBefore()`：单次 I/O 读取 20B（TrailerCodeword + Fence），只校验 TrailerCrc
   - `RbfReverseEnumerator` / `RbfReverseSequence`：支持 foreach 的逆向迭代
   - `IRbfFile.ScanReverse(showTombstone)`：门面方法
   - 损坏帧硬停止，TerminationError 记录错误

4. **新增类型**：
   - `RbfFrameInfo`：轻量帧元信息（Ticket, Tag, PayloadLength, TailMetaLength, IsTombstone）
   - `TrailerCodewordHelper`：TrailerCodeword 编解码辅助类
   - `TrailerCodewordData`：解析结果结构体

5. **删除/重构**：
   - 删除 `FrameStatusHelper.cs`（v0.40 使用 FrameDescriptor 替代 Status）
   - 删除旧常量（TagOffset, TagSize 等）
   - `RbfLayout.cs` 重写，新增 PayloadCrcSize, TrailerCodewordSize, MinFrameLength=24

**设计决策**：
- **Decision 6.A**：Wire format breaking change，不保留向后兼容
- **Decision 6.B**：TrailerCrc32C 使用 BE 存储（与 PayloadCrc LE 区分）
- **Decision 6.C**：ScanReverse 只校验 TrailerCrc，不校验 PayloadCrc（职责分离）

**测试覆盖**：197 个测试全部通过

**详细记录**：见 [stage/06/task.md](stage/06/task.md)

### Stage 05: ReadFrame 重构与 Buffer 外置（2026-01-17）

**核心变更**：

1. **SizedPtr 类型简化**：
   - `OffsetBytes` → `Offset`（`ulong` → `long`）
   - `LengthBytes` → `Length`（`uint` → `int`）
   - 与 .NET I/O API 类型对齐，消除频繁转换

2. **RbfFrame.Ptr → Ticket 重命名**：
   - "Ticket"语义更明确——表示读取凭据而非裸指针

3. **新增 IRbfFrame 接口**：
   - 统一 `RbfFrame`（ref struct）和 `RbfPooledFrame`（class）的公共契约
   - C# 13 特性：ref struct 可实现接口

4. **新增 RbfPooledFrame 类型**：
   - 封装 ArrayPool buffer 生命周期
   - 实现 `IDisposable`，调用方 MUST Dispose
   - Dispose 后 Payload 变为 dangling

5. **ReadFrame API 重构**：
   - 移除旧签名 `ReadFrame(file, ptr)`
   - 新增 `ReadFrame(file, ticket, buffer)` — Buffer 外置，zero-copy
   - 新增 `ReadPooledFrame(file, ticket)` — 自动管理 buffer

6. **新增 DisposableAteliaResult&lt;T&gt;**：
   - 带资源所有权的结果类型
   - 支持 `using var result = ...` 语法
   - 成功时 Dispose 调用 Value.Dispose()

7. **新增 RbfBufferTooSmallError**：
   - 专用错误类型（RequiredBytes, ProvidedBytes）

**文件变更**：
- 新增：`IRbfFrame.cs`, `RbfPooledFrame.cs`, `RbfReadImpl.cs`
- 删除：`RbfRawOps.ReadFrame.cs`
- 重命名：`RbfReadFrameTests.cs` → `RbfReadImplTests.cs`

**设计决策**：
- **Decision 5.A**：移除旧 ReadFrame 签名，无兼容层（无外部依赖，轻装上阵）
- **Decision 5.B**：ReadFrameInto 始终校验 CRC，ScanReverse 始终不校验（职责分离）

**测试覆盖**：150 个测试全部通过

**详细记录**：见 [stage/05/manual-refactor.md](stage/05/manual-refactor.md)

### Stage 04: 随机读取（ReadFrame）（2026-01-15）

**核心实现**：
- `RbfRawOps.ReadFrame()` - 随机读取指定位置的帧
  - 完整 Framing 校验（HeadLen/TailLen 对称、FrameStatus 位域+一致性）
  - CRC32C 校验
  - Buffer 策略：≤4KB stackalloc，>4KB ArrayPool
  - Payload 复制到新数组保证生命周期安全

**错误码定义**（`RbfErrors.cs`）：
- `RbfArgumentError` - 参数错误（对齐、长度、越界）
- `RbfFramingError` - Framing 校验失败
- `RbfCrcMismatchError` - CRC 校验失败

**FrameStatus 解码**：
- `FrameStatusHelper.TryDecodeStatusByte()` - 解码 Tombstone/StatusLen，验证保留位

**设计决策**：
- **Decision 4.A**：ReadFrame 不校验 Fence（已知位置的随机读，Fence 校验是冗余 I/O）
- **Decision 4.B**：错误码采用分层聚合（ArgumentError/FramingError/CrcMismatch）

### Stage 03: 简单写入路径（Append）（2026-01-14 ~ 2026-01-15）

**核心实现**：
- `RbfAppendImpl.Append()` - 自适应 1-3 次写入策略
  - 小帧（≤4KB）：单次 `RandomAccess.Write`
  - 中帧（4KB-8KB）：2 次写入（header + tail buffer 复用）
  - 大帧（>8KB）：3 次写入（header + 零拷贝 payload + tail）
  - CRC 流式计算（`Init`/`Update`/`Finalize`），避免二次遍历

**辅助工具**：
- `Crc32CHelper` - 三级优化（ulong/uint/byte）+ `Unsafe.ReadUnaligned` 跨平台安全
- `FrameStatusHelper` - StatusLen 公式 + 位域编码 + 值域校验
- `RbfConstants.ComputeFrameLen()` - 帧长度计算（内聚于常量类）

**关键不变量验证**：
- 4B 对齐根不变量（@[S-RBF-DECISION-4B-ALIGNMENT-ROOT]）
- HeadLen/TailLen 对称性（@[F-FRAMEBYTES-LAYOUT]）
- Fence 字节验证（@[F-FENCE-RBF1-ASCII-4B]）
- CRC32C 覆盖范围（@[F-CRC32C-COVERAGE]）

### Stage 02: 常量与 Fence（2026-01-14）

**工厂方法实现**：
- `RbfFile.CreateNew(path)` - 创建新文件，写入 HeaderFence
- `RbfFile.OpenExisting(path)` - 打开已有文件，验证 HeaderFence + 4B 对齐

### Stage 01: 项目骨架与类型骨架（2026-01-14）

**项目结构**：
- `atelia/src/Rbf/Rbf.csproj` - 主项目
- `atelia/tests/Rbf.Tests/Rbf.Tests.csproj` - 测试项目
- `atelia/benchmarks/Rbf.Benchmarks/Rbf.Benchmarks.csproj` - 基准测试项目

**公开类型骨架**：`IRbfFile`, `RbfFile`, `RbfFrame`, `RbfFrameBuilder`, `RbfReverseSequence`, `RbfReverseEnumerator`

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-24 | Stage 06 完成：帧布局 v0.40 + ScanReverse + 197 个测试通过 |
| 2026-01-17 | Stage 05 完成：ReadFrame 重构 + SizedPtr 简化 + 150 个测试通过 |
| 2026-01-17 | 设计文档同步：rbf-interface.md v0.30, rbf-type-bone.md v0.5, AteliaResult 文档更新, SizedPtr.md 更新 |
| 2026-01-15 | Stage 04 完成：ReadFrame 实现 + 146 个测试通过 |
| 2026-01-15 | 监护人手动重构：Append 自适应写入、测试分层、常量内聚 |
| 2026-01-14 | Stage 03 完成：Append 实现 + 测试 |
| 2026-01-14 | Stage 02 完成：常量与 Fence|
| 2026-01-14 | Stage 01 完成：项目骨架与类型骨架 |
| 2026-01-14 | 初始版本：记录基础条件 |
