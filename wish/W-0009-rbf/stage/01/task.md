# Stage 01: 项目骨架与类型骨架

> **目标**：创建 dotnet 9.0 项目结构，实现主要类型的骨架代码。
> **验收标准**：`dotnet build` 成功，类型签名与 `rbf-interface.md` 对齐。

---

## 上下文文件

- **前情提要**：`wish/W-0009-rbf/recap.md`
- **后续方向**：`wish/W-0009-rbf/todo.md`
- **接口规范**：`atelia/docs/Rbf/rbf-interface.md`
- **实现指南**：`atelia/docs/Rbf/rbf-type-bone.md`
- **现有依赖**：`atelia/src/Data/` 目录

---

## Task 列表

### Task 1.1: 创建项目结构

**执行者**：Implementer

**任务简报**：
创建 RBF 的 dotnet 项目结构，包括主项目和测试项目。

**输入**：
- 参考 `atelia/src/Data/Data.csproj` 的项目配置风格
- 参考 `atelia/tests/` 下现有测试项目的组织方式

**产出**：
1. `atelia/src/Rbf/Rbf.csproj` - 类库项目（net9.0, nullable enable, implicit usings）
2. `atelia/tests/Rbf.Tests/Rbf.Tests.csproj` - xUnit 测试项目
3. 两个项目都添加到 `atelia/Atelia.sln`
4. Rbf 项目引用 `Data.csproj` 和 `Primitives.csproj`

**验收**：`dotnet build atelia/Atelia.sln` 成功

---

### Task 1.2: 实现公开类型骨架

**执行者**：Implementer

**任务简报**：
根据 `rbf-interface.md` 实现所有公开类型的骨架。方法体使用 `throw new NotImplementedException()` 占位。

**输入**：
- `atelia/docs/Rbf/rbf-interface.md` 中的 §3（IRbfFile）、§4（RbfReverseSequence）、§4 末尾（RbfFrame）
- 依赖类型：`Atelia.Data.SizedPtr`、`Atelia.Data.IReservableBufferWriter`、`Atelia.AteliaResult<T>`

**产出**：
1. `atelia/src/Rbf/IRbfFile.cs` - 接口定义
2. `atelia/src/Rbf/RbfFile.cs` - 静态工厂类（`CreateNew`, `OpenExisting`）
3. `atelia/src/Rbf/RbfFrame.cs` - `readonly ref struct`
4. `atelia/src/Rbf/RbfFrameBuilder.cs` - `ref struct`，含 `Payload` 属性和 `EndAppend`/`Dispose` 方法
5. `atelia/src/Rbf/RbfReverseSequence.cs` - `ref struct`，含 `GetEnumerator`
6. `atelia/src/Rbf/RbfReverseEnumerator.cs` - `ref struct`，含 `Current`/`MoveNext`

**关键约束**：
- `RbfFrame` 必须是 `readonly ref struct`
- `RbfFrameBuilder` 必须是 `ref struct`（实现 `IDisposable` 的 Dispose 模式）
- `RbfReverseSequence`/`RbfReverseEnumerator` 必须是 `ref struct`（因为 `RbfFrame` 是 ref struct）

**验收**：编译通过，类型签名与规范文档一致

---

### Task 1.3: 实现内部类型骨架

**执行者**：Implementer

**任务简报**：
根据 `rbf-type-bone.md` 实现内部实现类型的骨架。

**输入**：
- `atelia/docs/Rbf/rbf-type-bone.md` 中的 §2（RbfRawOps）、§5（RandomAccessByteSink）

**产出**：
1. `atelia/src/Rbf/Internal/RbfRawOps.cs` - 静态类，包含 `ReadFrame`、`ScanReverse`、`_BeginFrame` 方法签名
2. `atelia/src/Rbf/Internal/RandomAccessByteSink.cs` - `IByteSink` 适配器

**验收**：编译通过

---

### Task 1.4: 创建基础测试骨架

**执行者**：Implementer

**任务简报**：
创建测试项目的基础结构，包含一个占位测试确保测试框架正常工作。

**产出**：
1. `atelia/tests/Rbf.Tests/RbfFileTests.cs` - 包含一个 `[Fact]` 占位测试

**验收**：`dotnet test atelia/tests/Rbf.Tests/` 成功（1 个测试通过）

---

## 执行记录

*（Task 执行后在此记录状态）*

| Task | 状态 | 执行者 | 备注 |
|------|------|--------|------|
| 1.1 | ⏳ 待执行 | - | - |
| 1.2 | ⏳ 待执行 | - | - |
| 1.3 | ⏳ 待执行 | - | - |
| 1.4 | ⏳ 待执行 | - | - |

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-14 | 初始版本：4 个 Task |
