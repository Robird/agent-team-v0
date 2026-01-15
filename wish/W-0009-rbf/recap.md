# RBF 实现前情提要

> 本文档记录 RBF 实现的当前状态和已完成的交付成果。
> **维护者**：每个 Task 完成后由执行者更新。

---

## 当前状态

**阶段**：Stage 03 - 简单写入路径（Append） ✅ **已完成**（含监护人手动重构）

**下一阶段**：Stage 04 - 随机读取（ReadFrame）

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
| [testing-pattern.md](testing-pattern.md) | Craft | 测试分层策略（职责分离模式） |

---

## 代码结构速查

### 内部类型 (`Internal/`)

| 文件 | 职责 |
|------|------|
| `RbfConstants.cs` | 常量定义 + `ComputeFrameLen()` |
| `RbfFileImpl.cs` | `IRbfFile` Facade 实现（状态管理） |
| `RbfAppendImpl.cs` | Append 核心实现（自适应 1-3 次写入） |
| `Crc32CHelper.cs` | CRC32C 计算（Init/Update/Finalize） |
| `FrameStatusHelper.cs` | StatusLen 计算 + 位域编码 |
| `RbfRawOps.cs` | 分文件组织：`_BeginFrame.cs`, `ReadFrame.cs`, `ScanReverse.cs` |
| `RandomAccessByteSink.cs` | `IByteSink` 适配器 |

### 测试分层 (`tests/Rbf.Tests/`)

| 文件 | 职责 | 验证内容 |
|------|------|----------|
| `RbfFacadeTests.cs` | Facade 状态测试 | TailOffset 更新、返回值转发 |
| `RbfAppendImplTests.cs` | Append 格式验证 | HeadLen/CRC/Fence/对齐 |
| `RbfFileFactoryTests.cs` | 工厂方法测试 | CreateNew/OpenExisting |
| `Crc32CHelperTests.cs` | CRC 工具测试 | RFC 向量 + baseline 对比 |
| `FrameStatusHelperTests.cs` | Status 工具测试 | 公式 + 值域校验 |

### Benchmark (`benchmarks/Rbf.Benchmarks/`)

新增性能基准测试项目，用于验证 Append 优化效果。

---

## 已完成的交付成果

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
- `RbfConstants` 字段常量 - 消除魔数（`HeadLenFieldLength`, `TagFieldLength` 等）

**测试分层重构**（监护人手动）：
- `RbfAppendTests.cs` → `RbfFacadeTests.cs` + `RbfAppendImplTests.cs`
- 建立职责分离测试模式：Facade 测状态，RawOps 测格式
- 撰写 `testing-pattern.md` 文档记录测试策略

**关键不变量验证**：
- 4B 对齐根不变量（@[S-RBF-DECISION-4B-ALIGNMENT-ROOT]）
- HeadLen/TailLen 对称性（@[F-FRAMEBYTES-FIELD-OFFSETS]）
- Fence 字节验证（@[F-FENCE-VALUE-IS-RBF1-ASCII-4B]）
- CRC32C 覆盖范围（@[F-CRC32C-COVERAGE]）

### Stage 02: 常量与 Fence（Genesis）（2026-01-14）

**工厂方法实现**：
- `RbfFile.CreateNew(path)` - 创建新文件，写入 Genesis Fence
- `RbfFile.OpenExisting(path)` - 打开已有文件，验证 Genesis + 4B 对齐

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
| 2026-01-15 | 监护人手动重构：Append 自适应写入、测试分层、常量内聚 |
| 2026-01-14 | Stage 03 完成：Append 实现 + 测试 |
| 2026-01-14 | Stage 02 完成：常量与 Fence（Genesis） |
| 2026-01-14 | Stage 01 完成：项目骨架与类型骨架 |
| 2026-01-14 | 初始版本：记录基础条件 |
