# Stage 02: 常量与 Fence（Genesis）

> **目标**：实现 Fence 常量、Genesis 验证、`RbfFile.CreateNew/OpenExisting` 工厂方法。
> **验收标准**：单元测试覆盖 CreateNew/OpenExisting 的正常和异常路径。

---

## 上下文文件

- **前情提要**：`wish/W-0009-rbf/recap.md`
- **实现蓝图**：`wish/W-0009-rbf/blueprint.md`
- **格式规范**：`atelia/docs/Rbf/rbf-format.md` - §2 Fence 定义
- **决策文档**：`atelia/docs/Rbf/rbf-decisions.md` - Genesis Fence 决策

---

## 规范要点摘要

### Fence 常量（来自 rbf-format.md @[F-FENCE-VALUE-IS-RBF1-ASCII-4B]）
- **值**：`RBF1`（ASCII: `0x52 0x42 0x46 0x31`）
- **长度**：4 字节
- **编码**：ASCII 字节序列（非 u32 端序）

### Genesis Fence 语义（来自 rbf-decisions.md @[F-FILE-STARTS-WITH-GENESIS-FENCE]）
- 每个 RBF 文件 MUST 以 Fence 开头（偏移 0）
- 新建的 RBF 文件 MUST 仅含 Genesis Fence（长度 = 4 字节）
- 首帧起始地址 MUST 为 `offset=4`

### 工厂方法语义（来自 rbf-interface.md）
- `RbfFile.CreateNew(path)` - 创建新文件（FailIfExists），写入 Genesis Fence
- `RbfFile.OpenExisting(path)` - 打开已有文件，验证 Genesis Fence

---

## Task 列表

### Task 2.1: 定义 Fence 常量

**执行者**：Implementer

**任务简报**：
创建 RBF 常量定义文件，包含 Fence 相关常量。

**产出**：
1. `atelia/src/Rbf/Internal/RbfConstants.cs` - 包含：
   - `internal static class RbfConstants`（internal 避免过早暴露 API surface）
   - `public static ReadOnlySpan<byte> Fence => "RBF1"u8;`（C# 11 UTF-8 字面量）
   - `public const int FenceLength = 4;`
   - `public const int GenesisLength = 4;`（等于 FenceLength，语义别名）

**验收**：编译通过

---

### Task 2.2: 实现 IRbfFile 内部实现类

**执行者**：Implementer
**依赖**：Task 2.1

**任务简报**：
创建 `IRbfFile` 的内部实现类，持有 `SafeFileHandle` 和 `TailOffset` 状态。

**职责边界**（重要）：
- `RbfFileImpl` = **Facade**（资源管理 + TailOffset 状态 + 参数校验/并发约束）
- 具体读写/扫描算法下沉到 `RbfRawOps`（后续阶段实现）

**产出**：
1. `atelia/src/Rbf/Internal/RbfFileImpl.cs` - 包含：
   - `internal sealed class RbfFileImpl : IRbfFile`
   - 构造函数接收 `SafeFileHandle` 和初始 `tailOffset`
   - `TailOffset` 属性实现
   - `Dispose()` 释放 `SafeFileHandle`
   - 其他方法暂时 `throw new NotImplementedException()`

**验收**：编译通过

---

### Task 2.3: 实现 RbfFile.CreateNew

**执行者**：Implementer
**依赖**：Task 2.1, Task 2.2

**任务简报**：
实现创建新 RBF 文件的工厂方法。

**输入**：
- `rbf-decisions.md` @[F-FILE-STARTS-WITH-GENESIS-FENCE]：新文件 MUST 仅含 Genesis Fence

**产出**：
1. 修改 `atelia/src/Rbf/RbfFile.cs`：
   - `CreateNew(path)` 实现：
     - 使用 `File.OpenHandle(path, FileMode.CreateNew, FileAccess.ReadWrite)` 打开文件
     - 写入 Genesis Fence（4 字节）
     - 返回 `RbfFileImpl` 实例（`tailOffset = 4`）

**关键约束**：
- `FileMode.CreateNew` 确保文件不存在时才创建（FailIfExists 语义）
- 写入后 `TailOffset = 4`（Genesis Fence 长度）
- **失败路径句柄释放**：若写入过程抛异常，MUST 确保句柄关闭（使用 try-catch 或所有权转移模式）

**验收**：编译通过

---

### Task 2.4: 实现 RbfFile.OpenExisting

**执行者**：Implementer
**依赖**：Task 2.1, Task 2.2

**任务简报**：
实现打开已有 RBF 文件的工厂方法。

**输入**：
- `rbf-decisions.md` @[F-FILE-STARTS-WITH-GENESIS-FENCE]：文件 MUST 以 Genesis Fence 开头

**产出**：
1. 修改 `atelia/src/Rbf/RbfFile.cs`：
   - `OpenExisting(path)` 实现：
     - 使用 `File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite)` 打开文件
     - 读取前 4 字节，验证是否为 `RBF1`
     - **边界条件**：若文件长度 < 4 字节，视为 Genesis 缺失，同样抛出异常
     - 若验证失败，关闭句柄并抛出 `InvalidDataException("Invalid RBF file: Genesis Fence mismatch")`
     - 获取文件长度作为 `tailOffset`
     - 返回 `RbfFileImpl` 实例

**关键约束**：
- **失败路径句柄释放**：验证失败或任何异常时 MUST 确保句柄关闭

**验收**：编译通过

---

### Task 2.5: 编写单元测试

**执行者**：Implementer
**依赖**：Task 2.3, Task 2.4

**任务简报**：
为 CreateNew/OpenExisting 编写单元测试。

**产出**：
1. `atelia/tests/Rbf.Tests/RbfFileFactoryTests.cs` - 包含：
   - `CreateNew_CreatesFileWithGenesisFence()` - 验证新文件长度为 4，内容为 `0x52 0x42 0x46 0x31`（按字节断言，不依赖常量）
   - `CreateNew_FailsIfFileExists()` - 先创建文件，再调用 CreateNew，验证抛出 `IOException`
   - `OpenExisting_SucceedsWithValidFile()` - 先 CreateNew，再 OpenExisting，验证 TailOffset 正确
   - `OpenExisting_FailsWithInvalidGenesis()` - 创建内容非 `RBF1` 的文件，验证抛出 `InvalidDataException`
   - `OpenExisting_FailsIfFileNotExists()` - 验证文件不存在时抛出 `FileNotFoundException`
   - `OpenExisting_FailsWhenFileTooShort()` - 创建小于 4 字节的文件，验证抛出 `InvalidDataException`

**测试工具**：
- **不要使用 `Path.GetTempFileName()`**（会创建文件，导致 CreateNew 失败）
- 使用 `Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())` 生成不存在的文件路径
- 测试结束后在 `finally` 块或 `Dispose` 中清理文件

**验收**：`dotnet test` 全部通过（6 个测试）

---

## 执行记录

| Task | 状态 | 执行者 | 备注 |
|------|------|--------|------|
| 2.1 | ✅ 完成 | Implementer | `RbfConstants.cs` created |
| 2.2 | ✅ 完成 | Implementer | `RbfFileImpl.cs` created |
| 2.3 | ✅ 完成 | Implementer | `CreateNew` implemented |
| 2.4 | ✅ 完成 | Implementer | `OpenExisting` implemented + 4B 对齐校验 |
| 2.5 | ✅ 完成 | Implementer | 8 tests passing |

---

## Code Review 总结

**审阅者**：Craftsman.OpenRouter
**审阅时间**：2026-01-14
**报告位置**：`agent-team/handoffs/2026-01-14-rbf-stage02-review.md`

**发现问题**：
- **Major 1**（已修复）：OpenExisting 需要校验 4B 对齐（@[S-RBF-DECISION-4B-ALIGNMENT-ROOT]）
- Minor 2-4：错误消息区分、unused using 等（可在后续阶段顺手修复）

**修复内容**：
- OpenExisting 增加 `fileLength % 4 != 0` 校验
- 新增测试：`OpenExisting_FailsWhenLengthNotAligned`
- 修改错误消息：`file too short` vs `mismatch` 区分

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-14 | Review 修订：RbfConstants 改 internal、补充文件过短测试、修复临时文件策略、明确句柄释放约束 |
| 2026-01-14 | 初始版本：5 个 Task |
