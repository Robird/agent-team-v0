# 方案 B（草案）：File/Builder 协同状态机

> **目标**：通过显式状态机，消除 `RbfFileImpl` 与 `RbfFrameBuilder` 的隐式布尔逻辑，提升一致性与可测试性。

---

## 1. 核心动机

当前状态靠 `_hasActiveBuilder`、`_disposed`、`_committed` 等多处布尔旗组合，存在以下风险：
- 状态组合难穷尽，易遗漏边界分支
- 失败语义难以测试（同一异常可能来自不同状态）
- 未来扩展（如 `TryEndAppend`、`Abort`）时不可判定性增加

---

## 2. 目标与非目标

### 2.1 目标
- 将 `RbfFileImpl` 与 `RbfFrameBuilder` 生命周期变为显式状态机
- 每个公开方法都能判定“允许/拒绝”与原因
- 测试用例能明确覆盖每个状态转移

### 2.2 非目标
- 不重构写入算法（留给方案 C）
- 不统一错误模型（留给方案 D；若 D 已实施，EndAppend 返回类型应与 D 对齐）

---

## 3. 状态机设计

### 3.1 `RbfFileImpl` 状态

```csharp
internal enum FileState {
    Idle,
    BuilderActive,
    Disposed
}
```

**转移规则**：
- `Idle -> BuilderActive`：`BeginAppend()`
- `BuilderActive -> Idle`：builder `EndAppend()` 或 `Dispose()`（Abort）
- `* -> Disposed`：`Dispose()`

**约束**：
- `Append()` 只允许 `Idle`
- `Truncate()` 只允许 `Idle`
- `BeginAppend()` 只允许 `Idle`
- `ScanReverse()`、`Read*()` 在 `BuilderActive` 状态**默认禁止**（避免读取未提交视图）

#### 3.1.1 `Read*`/`ScanReverse` 裁决

**候选方案**：

| 方案 | 行为 | 优点 | 缺点 |
|---|---|---|---|
| A | `BuilderActive` 时直接抛异常 | 一致、简单、可判定 | 限制使用场景 |
| B | 允许读取，但仅看到旧的 `TailOffset` | 更灵活 | 容易误解为“看到最新” |
| C | 允许读取并包含未提交数据 | 语义完整 | 需要暴露 Builder 缓冲区，复杂 |

**推荐**：方案 A。若未来需要放宽可升级为方案 B。

### 3.2 `RbfFrameBuilder` 状态

```csharp
internal enum BuilderState {
    Active,
    Committed,
    Aborted
}
```

**转移规则**：
- `Active -> Committed`：`EndAppend()`
- `Active -> Aborted`：`Dispose()`（未提交）
- `Committed/Aborted -> Active`：`Reset()`（复用）

> 资源释放（`DisposeInternal()`）属于实现细节，不纳入状态机枚举。

**约束**：
- `PayloadAndMeta` 仅允许 `Active`
- `EndAppend()` 仅允许 `Active`
- `Reset()` 仅允许 `Committed/Aborted`
- `Dispose()` 允许 `Active/Committed/Aborted`（幂等）

### 3.3 迁移策略（从 `_hasActiveBuilder` 到 `FileState`）

1. Phase 1：引入 `FileState`，以语义等价方式替换 `_hasActiveBuilder`：
    - `_hasActiveBuilder == true` → `FileState.BuilderActive`
    - `_hasActiveBuilder == false && !_disposed` → `FileState.Idle`
2. Phase 2：回调仍保留（`_clearBuilderFlag` / `_onCommitCallback`），避免循环引用。
3. Phase 3：若后续减少回调，可在 `RbfFileImpl` 内部封装状态转移方法，避免散落调用。

### 3.4 变体方案：Builder 作为 Facade，FileImpl 统一内聚

**动机**：让 `RbfFrameBuilder` 仅承载 `IDisposable`（Abort）与用户写入入口，
将写入提交与状态机统一收敛到 `RbfFileImpl`，移除回调耦合。

**核心变化**：

- `RbfFileImpl` 统一持有并管理：
    - 状态机（`FileState` + TailOffset 更新）
    - `RbfFrameBuilder` 单例
    - `RandomAccessByteSink` / `SinkReservableWriter` 单例
- `RbfFrameBuilder` 变为薄 Facade：
    - `PayloadAndMeta` 直接转发到 file-owned writer
    - `EndAppend` 仅调用 `RbfFileImpl.CommitFromBuilder(...)`
    - `Dispose` 仅调用 `RbfFileImpl.AbortBuilder()`
- 移除 `_onCommitCallback` / `_clearBuilderFlag`（由 FileImpl 内部方法替代）

**最小 API 形式**（对调用方无感）：

```csharp
// public API 保持不变
public AteliaResult<SizedPtr> EndAppend(uint tag, int tailMetaLength = 0)
        => _owner.CommitFromBuilder(_epoch, tag, tailMetaLength);

public void Dispose()
        => _owner.AbortBuilder(_epoch);
```

**必要保护机制**：

- **Epoch Token**：`BeginAppend()` 时递增 `_epoch`，Builder 保存快照；
    `EndAppend/Dispose/PayloadAndMeta` 校验 `_epoch`，防止旧 Builder 被误用。
- **双提交保护**：`CommitFromBuilder` 内部检查 `BuilderState`。
- （可选）**Writer Wrapper**：如需更强保护，可用 wrapper 在每次调用时验证 `_epoch`。

**收益**：
- 状态机与写入逻辑集中在 `RbfFileImpl`（更易审阅/测试）
- 移除回调，降低跨类型同步复杂度

**代价**：
- `RbfFileImpl` 体积增大
- 测试需重构（Builder 测试减少，FileImpl 测试增加）

---

## 4. 修改范围

### 4.1 `RbfFileImpl`

- 新增 `FileState _state`
- 替换 `_hasActiveBuilder` 分支
- `Append/BeginAppend/Truncate` 统一入口状态检查

### 4.2 `RbfFrameBuilder`

- 新增 `BuilderState _state`
- `EndAppend`、`Dispose`、`PayloadAndMeta` 依据状态判断
- 在 `EndAppend/Dispose` 成功后调用回调清理 `RbfFileImpl` 状态
- `Reset` 依据状态判断（仅 `Committed/Aborted` 可重置）

---

## 5. 验收标准

1. **状态转移全覆盖**：每条允许/拒绝路径都能被单测验证
2. **异常一致性**：相同状态违规应抛一致异常类型
3. **状态可观测**：debug 日志或诊断 API 可确认当前状态（可选）
4. **Reset 不引入脏数据**：连续 Begin→写入→Abort→Reset→Begin→写入→Commit，第二次写入不包含第一次残留数据

---

## 6. 风险与缓解

| 风险 | 影响 | 缓解 |
|---|---|---|
| 状态机过度设计 | 中 | 保持枚举最小化，不引入新功能 |
| Read/Scan 允许状态不清 | 中 | 明确决策并写入注释/测试 |
| Dispose 语义复杂化 | 低 | 明确 Active/Committed/Aborted 的幂等处理 |
| Builder 复用误用（旧引用） | 中 | 引入 Epoch Token 校验 |

---

## 7. 测试建议

**状态转移矩阵（文件级）**：

| 起始状态 | 操作 | 期望结果 |
|---|---|---|
| `Idle` | `BeginAppend()` | 进入 `BuilderActive` |
| `Idle` | `Append()` | 成功，仍为 `Idle` |
| `Idle` | `Read*()` | 成功 |
| `Idle` | `ScanReverse()` | 成功 |
| `Idle` | `Truncate()` | 成功 |
| `Idle` | `Dispose()` | 进入 `Disposed` |
| `BuilderActive` | `BeginAppend()` | 抛异常 |
| `BuilderActive` | `Append()` | 抛异常 |
| `BuilderActive` | `Read*()` | 抛异常（按方案 A 裁决） |
| `BuilderActive` | `ScanReverse()` | 抛异常（按方案 A 裁决） |
| `BuilderActive` | `Truncate()` | 抛异常 |
| `BuilderActive` | Builder.`EndAppend()` | 回到 `Idle` |
| `BuilderActive` | Builder.`Dispose()` | 回到 `Idle` |
| `Disposed` | 任何操作 | 抛 `ObjectDisposedException` |

**状态转移矩阵（Builder 级）**：

| 起始状态 | 操作 | 期望结果 |
|---|---|---|
| `Active` | `PayloadAndMeta.GetSpan()` | 成功 |
| `Active` | `EndAppend()` | 进入 `Committed` |
| `Active` | `Dispose()` | 进入 `Aborted` |
| `Active` | `Reset()` | 抛异常 |
| `Committed` | `PayloadAndMeta.GetSpan()` | 抛异常 |
| `Committed` | `EndAppend()` | 抛异常 |
| `Committed` | `Reset()` | 进入 `Active` |
| `Aborted` | `Reset()` | 进入 `Active` |
| `Any` | 旧 Builder 调用（epoch mismatch） | 抛 `InvalidOperationException` |

---

## 8. 审阅关注点

- `Read*`/`ScanReverse` 在 `BuilderActive` 状态是否允许
- `Dispose` 在 `Committed` 状态是否需要额外动作
- `Append` 与 `BeginAppend` 的互斥关系是否完整
