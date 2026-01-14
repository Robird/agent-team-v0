# RBF 实现前情提要

> 本文档记录 RBF 实现的当前状态和已完成的交付成果。
> **维护者**：每个 Task 完成后由执行者更新。

---

## 当前状态

**阶段**：Stage 02 - 常量与 Fence（Genesis） ✅ **已完成**

**下一阶段**：Stage 03 - 简单写入路径（Append）

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

---

## 已完成的交付成果

### Stage 02: 常量与 Fence（Genesis）（2026-01-14）

**新增类型**：
- `Internal/RbfConstants.cs` - Fence 常量（`internal` 避免 API surface 污染）
- `Internal/RbfFileImpl.cs` - `IRbfFile` 内部实现类（Facade 模式）

**工厂方法实现**：
- `RbfFile.CreateNew(path)` - 创建新文件，写入 Genesis Fence，TailOffset=4
- `RbfFile.OpenExisting(path)` - 打开已有文件，验证 Genesis + 4B 对齐

**测试覆盖**（8 个测试）：
- `CreateNew_CreatesFileWithGenesisFence` - 验证新文件内容
- `CreateNew_FailsIfFileExists` - 文件已存在时抛 IOException
- `OpenExisting_SucceedsWithValidFile` - 正常打开
- `OpenExisting_FailsWithInvalidGenesis` - Genesis 不匹配
- `OpenExisting_FailsIfFileNotExists` - 文件不存在
- `OpenExisting_FailsWhenFileTooShort` - 文件过短（<4B）
- `OpenExisting_FailsWhenLengthNotAligned` - 长度非 4B 对齐
- `RbfFileTests.Placeholder` - 原有占位测试

**设计亮点**：
- 失败路径确保句柄关闭（try-catch 模式）
- 入口处校验 4B 对齐根不变量（@[S-RBF-DECISION-4B-ALIGNMENT-ROOT]）

### Stage 01: 项目骨架与类型骨架（2026-01-14）

**项目结构**：
- `atelia/src/Rbf/Rbf.csproj` - 主项目（引用 Data + Primitives）
- `atelia/tests/Rbf.Tests/Rbf.Tests.csproj` - xUnit 测试项目
- 两个项目已添加到 `Atelia.sln`

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
| 2026-01-14 | Stage 02 完成：常量与 Fence（Genesis），含 4B 对齐校验 |
| 2026-01-14 | 设计决策：IDisposable 保留 + Append/BeginAppend 独立实现 |
| 2026-01-14 | Code Review 修复：4 个 Major 问题已解决 |
| 2026-01-14 | Stage 01 完成：项目骨架与类型骨架 |
| 2026-01-14 | 初始版本：记录基础条件 |
