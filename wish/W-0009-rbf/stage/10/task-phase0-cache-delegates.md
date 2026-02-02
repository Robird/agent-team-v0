# Task: Phase 0 - 缓存 BeginAppend 的 Delegate

> **预估工时**：0.5h
> **执行者**：Implementer
> **依赖**：无
> **收益**：每帧节省 ~48B delegate 分配

---

## 1. 问题背景

`RbfFileImpl.BeginAppend()` 当前每次调用都会创建两个 lambda 闭包：

```csharp
// 当前实现（每次调用都分配）
Action<long> onCommitCallback = (endOffset) => _tailOffset = endOffset;
Action clearBuilderFlag = () => _hasActiveBuilder = false;
```

这两个 lambda 捕获 `this` 实例，每次 BeginAppend 都会产生 ~48B 的堆分配。

## 2. 目标

将这两个 delegate 改为**构造时一次性创建并缓存**，BeginAppend 直接复用缓存的 delegate 实例。

## 3. 修改范围

**文件**：`/repos/focus/atelia/src/Rbf/Internal/RbfFileImpl.cs`

### 3.1 添加字段

在 `RbfFileImpl` 类中添加两个 readonly 字段：

```csharp
private readonly Action<long> _onCommitCallback;
private readonly Action _clearBuilderFlag;
```

### 3.2 构造函数初始化

在构造函数中初始化这两个字段（lambda 在构造时只分配一次）：

```csharp
_onCommitCallback = (endOffset) => _tailOffset = endOffset;
_clearBuilderFlag = () => _hasActiveBuilder = false;
```

### 3.3 修改 BeginAppend

将 BeginAppend 中的局部变量替换为字段引用：

```csharp
// 修改前
Action<long> onCommitCallback = (endOffset) => _tailOffset = endOffset;
Action clearBuilderFlag = () => _hasActiveBuilder = false;
return new RbfFrameBuilder(..., onCommitCallback, clearBuilderFlag);

// 修改后
return new RbfFrameBuilder(..., _onCommitCallback, _clearBuilderFlag);
```

## 4. 验收标准

1. **编译通过**：无编译错误
2. **现有测试通过**：`dotnet test atelia/tests/Rbf.Tests/`
3. **行为不变**：
   - `_tailOffset` 在 commit 后正确更新
   - `_hasActiveBuilder` 在 dispose 后正确清除
4. **代码审阅**：字段命名、可见性符合项目规范

## 5. 风险评估

| 风险 | 概率 | 影响 | 缓解 |
|:-----|:-----|:-----|:-----|
| 闭包捕获语义变化 | 极低 | 低 | lambda 仍捕获 `this`，语义不变 |
| 并发问题 | 无 | — | `_hasActiveBuilder` 已保证单 builder |

## 6. 注意事项

- 不要改变 lambda 的捕获对象（仍应捕获 `this`）
- 字段应为 `readonly`（构造后不变）
- 这是"先手优化"，与后续 Phase 解耦，可独立完成

---

## 执行指令（供 runSubagent 使用）

```
你是 Implementer，请执行以下任务：

1. 阅读 `/repos/focus/atelia/src/Rbf/Internal/RbfFileImpl.cs`，定位：
   - 类的字段区域
   - 构造函数
   - BeginAppend 方法中创建 delegate 的位置

2. 按照上述"修改范围"进行代码修改

3. 运行测试验证：
   ```bash
   cd /repos/focus && dotnet test atelia/tests/Rbf.Tests/ --no-build
   ```
   如果需要先编译：
   ```bash
   cd /repos/focus && dotnet build atelia/src/Rbf/
   ```

4. 完成后报告：
   - 修改的文件和行号
   - 测试结果
   - 任何发现的问题或建议
```
