# Stage 08: DurableFlush 与 Truncate

> **目标**：实现持久化和恢复能力。
> **前置依赖**：Stage 07（BeginAppend/EndAppend）✅

---

## 设计决策

### Decision 8.A: DurableFlush 实现策略

**结论**：直接委托 `RandomAccess.FlushToDisk(SafeFileHandle)`。

**设计理由**：
1. .NET 9 的 `RandomAccess.FlushToDisk` 已提供跨平台的 durable flush 能力（Windows: `FlushFileBuffers`, Unix: `fsync`）
2. RBF 层无需额外缓冲，所有写入通过 `RandomAccess.Write` 直接到达 OS 缓冲区
3. 简单包装即可满足 @[S-RBF-DURABLEFLUSH-DURABILIZE-COMMITTED-ONLY] 的语义

**异常策略**：
- `RandomAccess.FlushToDisk` 可能抛出 `IOException`（底层 I/O 错误）
- RBF 层不捕获，直接传播给调用方

### Decision 8.B: Truncate 实现策略

**结论**：参数校验 + `RandomAccess.SetLength` + 更新 `_tailOffset`。

**设计理由**：
1. .NET 9 的 `RandomAccess.SetLength` 提供跨平台的文件截断能力
2. RBF 层需要额外校验 4B 对齐（@[S-RBF-TRUNCATE-REQUIRES-NONNEGATIVE-4B-ALIGNED]）
3. 截断后需同步更新 `_tailOffset`，保持状态一致

**校验顺序**：
1. 检查 `_disposed`（若已释放，抛 `ObjectDisposedException`）
2. 检查 `newLengthBytes < 0`（抛 `ArgumentOutOfRangeException`）
3. 检查 `newLengthBytes % 4 != 0`（抛 `ArgumentOutOfRangeException`）
4. 执行 `RandomAccess.SetLength(_handle, newLengthBytes)`
5. 更新 `_tailOffset = newLengthBytes`

**异常策略**：
- 校验失败：抛 `ArgumentOutOfRangeException`（message 包含具体原因）
- 已释放：抛 `ObjectDisposedException`
- I/O 错误：`RandomAccess.SetLength` 抛出的异常直接传播

### Decision 8.C: Builder 期间的操作限制

**结论**：`DurableFlush` 和 `Truncate` 在 active builder 期间的行为。

**DurableFlush**：
- 允许在 active builder 期间调用
- 语义：只 flush "已提交写入"，未提交的 Builder 数据不受影响
- 实现：直接调用 `RandomAccess.FlushToDisk`，不检查 `_hasActiveBuilder`

**Truncate**：
- **禁止**在 active builder 期间调用
- 原因：Builder 正在使用 `_tailOffset` 之后的空间，截断会导致数据不一致
- 实现：检查 `_hasActiveBuilder`，若为 true 抛 `InvalidOperationException`

---

## 实现任务

### Part A: 核心实现

#### Task 8.1: ✅ 已完成 — 实现 RbfFileImpl.DurableFlush()

**执行者**：Implementer
**依赖**：无
**状态**：已完成

**任务简报**：
实现 `RbfFileImpl.DurableFlush()` 方法。

**实现逻辑**：
```csharp
public void DurableFlush() {
    ObjectDisposedException.ThrowIf(_disposed, this);
    RandomAccess.FlushToDisk(_handle);
}
```

**验收标准**：
- [ ] 编译通过
- [ ] 正常调用不抛异常
- [ ] Disposed 后调用抛 `ObjectDisposedException`

---

#### Task 8.2: ✅ 已完成 — 实现 RbfFileImpl.Truncate()

**执行者**：Implementer
**依赖**：无
**状态**：已完成

**任务简报**：
实现 `RbfFileImpl.Truncate(long newLengthBytes)` 方法。

**实现逻辑**：
```csharp
public void Truncate(long newLengthBytes) {
    // 1. Disposed 检查
    ObjectDisposedException.ThrowIf(_disposed, this);
    
    // 2. Active builder 检查
    if (_hasActiveBuilder) {
        throw new InvalidOperationException(
            "Cannot truncate while a builder is active. Dispose the builder first.");
    }
    
    // 3. 参数校验
    if (newLengthBytes < 0) {
        throw new ArgumentOutOfRangeException(
            nameof(newLengthBytes), newLengthBytes,
            "newLengthBytes must be non-negative.");
    }
    
    if ((newLengthBytes & 0x3) != 0) {
        throw new ArgumentOutOfRangeException(
            nameof(newLengthBytes), newLengthBytes,
            "newLengthBytes must be 4-byte aligned.");
    }
    
    // 4. 执行截断
    RandomAccess.SetLength(_handle, newLengthBytes);
    
    // 5. 更新 TailOffset
    _tailOffset = newLengthBytes;
}
```

**验收标准**：
- [ ] 编译通过
- [ ] 负数抛 `ArgumentOutOfRangeException`
- [ ] 非 4B 对齐抛 `ArgumentOutOfRangeException`
- [ ] Disposed 后调用抛 `ObjectDisposedException`
- [ ] Active builder 期间调用抛 `InvalidOperationException`
- [ ] 正常调用后 `TailOffset == newLengthBytes`
- [ ] 正常调用后文件物理长度 == newLengthBytes

---

### Part B: 测试覆盖

#### Task 8.3: ✅ 已完成 — DurableFlush 单元测试

**执行者**：Implementer
**依赖**：Task 8.1 ✅
**状态**：已完成（5 个测试用例）

**任务简报**：
在 `tests/Rbf.Tests/` 中创建 `RbfDurableFlushTests.cs`。

**测试用例**：
1. **正常调用**：CreateNew → Append → DurableFlush → 不抛异常
2. **空文件**：CreateNew → DurableFlush → 不抛异常
3. **多次调用**：DurableFlush → DurableFlush → 幂等，不抛异常
4. **Disposed 后调用**：Dispose → DurableFlush → 抛 `ObjectDisposedException`
5. **Builder 期间调用**：BeginAppend → DurableFlush → 不抛异常（允许）

**验收标准**：
- [ ] 所有测试通过

---

#### Task 8.4: ✅ 已完成 — Truncate 单元测试

**执行者**：Implementer
**依赖**：Task 8.2 ✅
**状态**：已完成（12 个测试用例）

**任务简报**：
在 `tests/Rbf.Tests/` 中创建 `RbfTruncateTests.cs`。

**测试用例**：

**参数校验**：
1. **负数**：Truncate(-1) → 抛 `ArgumentOutOfRangeException`
2. **非 4B 对齐（1B）**：Truncate(5) → 抛 `ArgumentOutOfRangeException`
3. **非 4B 对齐（2B）**：Truncate(6) → 抛 `ArgumentOutOfRangeException`
4. **非 4B 对齐（3B）**：Truncate(7) → 抛 `ArgumentOutOfRangeException`
5. **零长度**：Truncate(0) → 成功（0 是 4B 对齐的）

**状态检查**：
6. **Disposed 后调用**：Dispose → Truncate → 抛 `ObjectDisposedException`
7. **Builder 期间调用**：BeginAppend → Truncate → 抛 `InvalidOperationException`
8. **Builder Dispose 后调用**：BeginAppend → Dispose → Truncate → 成功

**功能验证**：
9. **截断到 HeaderFence**：Append → Truncate(4) → TailOffset == 4 → 文件长度 == 4
10. **截断到中间帧**：Append 3 帧 → Truncate(帧2末尾) → TailOffset 正确 → 文件长度正确
11. **截断后可继续 Append**：Truncate → Append → ScanReverse 只看到新帧
12. **截断后 ScanReverse**：Append 3 帧 → Truncate(帧1末尾) → ScanReverse 只看到帧1

**验收标准**：
- [ ] 所有测试通过
- [ ] 覆盖参数校验边界
- [ ] 验证状态一致性（TailOffset 与文件长度）

---

#### Task 8.5: ✅ 已完成 — 集成测试（恢复场景）

**执行者**：Implementer
**依赖**：Task 8.3 ✅, Task 8.4 ✅
**状态**：已完成（3 个测试用例）

**任务简报**：
在 `RbfTruncateTests.cs` 中添加恢复场景测试。

**测试用例**：
1. **DurableFlush + Truncate 恢复**：
   - Append 3 帧 → DurableFlush → Truncate(帧2末尾) → DurableFlush
   - 重新 OpenExisting → ScanReverse 只看到帧1、帧2
2. **Truncate 到 Fence 位置**：
   - Append 帧 → Truncate(HeaderFence 后) → 验证 TailOffset == 4
3. **Truncate 后 BeginAppend**：
   - Append → Truncate(4) → BeginAppend → EndAppend → ReadFrame 成功

**验收标准**：
- [ ] 所有测试通过
- [ ] 验证恢复语义正确

---

## 规范引用

| 条款 | 文档 | 要点 |
|------|------|------|
| @[S-RBF-DURABLEFLUSH-DURABILIZE-COMMITTED-ONLY] | rbf-interface.md | DurableFlush 语义 |
| @[S-RBF-TRUNCATE-REQUIRES-NONNEGATIVE-4B-ALIGNED] | rbf-interface.md | Truncate 参数约束 |
| @[S-RBF-TAILOFFSET-UPDATE] | rbf-interface.md | TailOffset 更新规则 |

---

## 相关文档

| 文档 | 职责 |
|:-----|:-----|
| **本文档（task.md）** | 执行蓝图：任务分解、验收标准 |
| [Stage 07](../07/task.md) | 前置 Stage（BeginAppend/EndAppend） |
| [recap.md](../../recap.md) | 已完成交付成果汇总 |
| [blueprint.md](../../blueprint.md) | 施工阶段总览 |
