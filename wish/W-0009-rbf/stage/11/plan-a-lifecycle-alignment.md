# 方案 A（草案）：生命周期与边界一致性最小修正

> **目标**：以最小改动修复 `RbfFileImpl` 与 `RbfFrameBuilder` 的生命周期契约不一致、提交后可继续写入、边界校验遗漏等高风险问题。
> **范围定位**：仅修复协同漏洞，不引入状态机或写入核心重构。

---

## 1. 问题摘要

1. `EndAppend` 成功后不会清除 `_hasActiveBuilder`，只有 `Dispose` 才会清除；若调用方未显式 `Dispose`，`BeginAppend` 仍被拒绝。
2. `PayloadAndMeta` 返回 `IReservableBufferWriter`，当前无状态检查；`EndAppend` 后仍可 `GetSpan/ReserveSpan/Advance`，存在“悬挂写入器”风险。
3. `RbfAppendImpl.Append` 路径已做 `MaxFileOffset` 校验，但 `EndAppend` 路径缺少等效校验，最终在 `SizedPtr.Create` 抛异常，失败语义不一致。
4. `RbfFileImpl.Append` 未检查 `Dispose` 状态，行为与 `BeginAppend`/`Truncate` 不一致。

---

## 2. 目标与非目标

### 2.1 目标
- `EndAppend` 结束后文件应立即恢复可写（清除 active builder 标记）。
- 提交后阻止继续写入（明确“已关闭”状态）。
- 在提交前完成 `MaxFileOffset` 校验，失败语义可预测。
- `Append` 对 `Dispose` 状态行为与其他方法一致。

### 2.2 非目标
- 不引入完整状态机（留给方案 B）。
- 不改写 `RbfAppendImpl` 与 `RbfFrameBuilder` 的写入算法。
- 不统一异常/Result 模型（留给方案 D）。

---

## 3. 方案设计

### 3.1 `RbfFrameBuilder` 生命周期调整

新增/调整行为：
1. `EndAppend` 成功后 **立即清除** `RbfFileImpl` 的 active builder 标记。
2. 使用 `_committed` 作为“提交后禁止写入”标识；`_disposed` 仅用于资源释放语义。
3. 公开写入入口（`PayloadAndMeta` 或内部写操作）需要验证状态：`_disposed` / `_committed`。

### 3.2 `MaxFileOffset` 校验补齐

在 `EndAppend` 写入前计算：

```
endOffset = _frameStart + layout.FrameLength + RbfLayout.FenceSize
```

并校验 `endOffset <= SizedPtr.MaxOffset`（即 `frameStart + frameLength + fenceSize` 不越界）。
若已采用方案 D，则应返回 `AteliaResult.Failure`；否则抛出 `InvalidOperationException` 或 `ArgumentOutOfRangeException`（需与现有风格一致）。

### 3.3 `RbfFileImpl.Append` 统一 Dispose 行为

在 `Append` 入口增加：
```
ObjectDisposedException.ThrowIf(_disposed, this)
```

与 `BeginAppend`、`Truncate` 保持一致。

---

## 4. 修改范围

### 4.1 `RbfFrameBuilder`

文件：`/repos/focus/atelia/src/Rbf/RbfFrameBuilder.cs`

**字段策略**：
- 复用 `_committed` 表示“提交后禁止写入”。
- 保留 `_disposed` 表示“资源已释放”。

**EndAppend**：
- 提交成功后：
   - 设置 `_committed = true`
   - 调用 `_clearBuilderFlag?.Invoke()`

**PayloadAndMeta 访问**：
- 若 `_disposed`，抛 `ObjectDisposedException`。
- 若 `_committed`，抛 `InvalidOperationException`。

**Dispose（Abort 路径）**：
- 仅在 `!_committed` 时调用 `_clearBuilderFlag?.Invoke()`，避免提交路径重复清理。

### 4.2 `RbfFileImpl`

文件：`/repos/focus/atelia/src/Rbf/Internal/RbfFileImpl.cs`

**Append**：入口加 `Dispose` 检查。

---

## 5. 验收标准

1. **Commit 后能再次 BeginAppend**：
   - `builder.EndAppend()` 后**不调用 `Dispose`**，再次调用 `BeginAppend()` 不抛异常。
2. **Commit 后禁止继续写入**：
   - `EndAppend` 后继续调用 `PayloadAndMeta.GetSpan()` 抛异常。
3. **越界失败语义明确**：
   - 满足 `frameStart + frameLength + fenceSize > SizedPtr.MaxOffset` 的帧在 `EndAppend` 前被拒绝（若采用方案 D：返回失败结果）。
4. **Dispose 一致性**：
   - `RbfFileImpl.Dispose()` 后 `Append` 抛 `ObjectDisposedException`。
5. **Dispose 后写入保护**：
   - `builder.Dispose()` 后访问 `PayloadAndMeta` 抛 `ObjectDisposedException`。

---

## 6. 风险与缓解

| 风险 | 影响 | 缓解 |
|---|---|---|
| `_committed` 语义扩展导致误用 | 中 | 仅用于“提交后禁止写入”，不改变资源释放语义 |
| 提交后清除 active 标记过早 | 中 | 只在 `EndAppend` 全部成功后清除 |
| 新异常类型与旧逻辑不一致 | 低 | 与现有异常风格保持一致 |

---

## 7. 任务拆分

1. 修改 `RbfFrameBuilder.EndAppend` 与 `PayloadAndMeta` 状态检查
2. 补齐 `MaxFileOffset` 校验
3. 修改 `RbfFileImpl.Append` 入口 `Dispose` 检查
4. 加入单测（Commit 后可再 BeginAppend + Commit 后拒绝写入）

---

## 8. 审阅关注点

- 清除 active builder 标记的时机是否会导致早释放风险
- 提交后写入保护是否足够（是否还需要防止 `Reset` 误用）
- 若采用方案 D，Result-Pattern 的失败分支是否覆盖全部前置校验
