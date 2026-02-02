# 方案 D（草案）：错误模型统一与 DX 优化

> **目标**：统一 `Append` 与 `EndAppend` 的错误语义，减少调用方心智负担，并降低测试复杂度。

---

## 1. 背景

- `IRbfFile.Append` 以 `AteliaResult<SizedPtr>` 返回可恢复错误
- `RbfFrameBuilder.EndAppend` 当前直接返回 `SizedPtr` 并抛异常
- 同一功能（追加帧）的两条路径错误语义不一致，增加 API 使用成本

---

## 2. 目标与非目标

### 2.0 前置条件
- `RbfStateError` 需要新增到 `/repos/focus/atelia/src/Rbf/Internal/RbfErrors.cs`

### 2.1 目标
- 将 `EndAppend` 的返回类型改为 `AteliaResult<SizedPtr>`
- 让 `EndAppend` 与 `Append` 使用一致的 Result-Pattern
- 让错误类型与 `Append` 对齐（如 `RbfArgumentError`）

### 2.2 非目标
- 不改变 `Append` 的现有签名
- 不引入额外的 `TryEndAppend` 方法
- 不修改 `RbfAppendImpl` 的错误模型

---

## 3. 设计草案

### 3.1 API 变更

```csharp
public AteliaResult<SizedPtr> EndAppend(uint tag, int tailMetaLength = 0);
```

> 调用方如需“抛异常风格”，可自行使用 `AteliaResult.GetValueOrThrow()`，或在上层封装 `EndAppendOrThrow`。

### 3.2 错误类型对齐

`EndAppend` 的失败场景应尽量与 `Append` 一致：
- `RbfArgumentError`：输入不合法（超限、未对齐）
- `RbfStateError`：状态不允许（重复提交、已 Dispose）
- I/O 异常仍直接抛出（Infra Fault）

**建议的错误映射表**：

| 失败场景 | 错误类型 | ErrorCode 示例 |
|---|---|---|
| `tailMetaLength < 0` | `RbfArgumentError` | `Rbf.ArgumentError` |
| `tailMetaLength > payloadAndMetaLength` | `RbfArgumentError` | `Rbf.ArgumentError` |
| `payloadAndMetaLength > MaxPayloadAndMetaLength` | `RbfArgumentError` | `Rbf.ArgumentError` |
| 重复调用 `EndAppend` | `RbfStateError` | `Rbf.StateError` |
| `Dispose` 后调用 | `RbfStateError` | `Rbf.StateError` |
| 存在未提交 reservation | `RbfStateError` | `Rbf.StateError` |
| I/O 异常（磁盘满/权限） | 直接抛出 | — |

### 3.3 I/O 异常边界

`EndAppend` 内部不捕获 `IOException`（直接穿透），仅将可恢复的逻辑错误转为 `AteliaResult.Failure`。

---

## 4. 修改范围

- `/repos/focus/atelia/src/Rbf/RbfFrameBuilder.cs`
- `/repos/focus/atelia/src/Rbf/Internal/RbfErrors.cs`（新增 `RbfStateError`）

---

## 5. 验收标准

1. `EndAppend` 返回错误时不抛异常
2. `EndAppend` 与 `Append` 的错误语义对齐
3. 错误类型与 `Append` 对齐
4. 单测覆盖 `EndAppend` 的失败场景

---

## 6. 风险与缓解

| 风险 | 影响 | 缓解 |
|---|---|---|
| 返回类型变更影响调用方 | 中 | 统一迁移到 Result-Pattern；提供示例或上层 helper |
| 错误类型不统一 | 中 | 明确错误分类并补测试 |
| 前置条件未满足（缺 `RbfStateError`） | 高 | 先补齐错误类型再推进 API |

---

## 7. 审阅关注点

- builder state 失败是否应视为可恢复错误
- 是否需要新增 `Abort()` 作为显式失败分支

**回应（草案）**：
- builder state 违规属于可恢复错误，应返回 `RbfStateError` 而非抛异常。
- `Abort()` 暂不新增，沿用 Dispose 自动中止；后续如需显式取消可在方案 B 中追加状态。
