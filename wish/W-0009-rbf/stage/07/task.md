# Stage 07: 复杂写入路径（BeginAppend/EndAppend）

> **目标**：实现流式写入 Builder，支持 payload 内回填和 TailMeta。
> **前置**：Stage 06.5 完成（RbfFrameInfo 成员方法 + TailMeta API）

---

## 设计决策

### Decision 7.A: Builder 类型设计

**结论**：`RbfFrameBuilder` 是 `sealed class`，直接包含所有实现逻辑（无 Impl 套娃）。

**设计理由**：
- 内部组件（`SinkReservableWriter`、`Crc32cByteSink` 等）本就是堆分配
- `ref struct` 外壳无实际收益，只增加复杂度
- `sealed class` 更简单，且支持未来 Reset 复用优化

**演进可能**：
- 由于 @[S-RBF-BUILDER-SINGLE-OPEN] 约束，每个 `IRbfFile` 同时最多 1 个 Builder
- 未来可通过 `Reset()` 复用同一个实例，避免重复分配（相当于 per-file Builder 单例）

**状态机**：
```
[Created] --PayloadAndMeta.Write()--> [Writing] --EndAppend()--> [Committed]
     |                                      |
     +---------- Dispose() ------------+----+---> [Disposed]
                                            (Auto-Abort if not committed)
```

### Decision 7.B: EndAppend 参数设计

**结论**：`EndAppend(uint tag, int tailMetaLength = 0)` 在提交时指定 Tag 和 TailMeta 长度。

**理由**：
- Tag 可能依赖 Payload 内容（如序列化后的类型信息）
- TailMeta 位于 Payload 末尾，长度在 `EndAppend` 时确定

### Decision 7.C: Auto-Abort 语义

**结论**：若 `Dispose()` 时未调用 `EndAppend()`，执行逻辑回滚（帧不可见）。

**物理实现**（参见 @[S-RBF-BUILDER-DISPOSE-ABORTS-UNCOMMITTED-FRAME]）：
- 不落盘场景（`PushedLength == 0`）：Zero I/O，直接丢弃
- 已落盘场景（`PushedLength > 0`）：将帧标记为 Tombstone（IsTombstone = true）

**异常策略（Decision 7.C.1）**：
Tombstone Auto-Abort 路径的 I/O 可能失败（磁盘满等）。采用策略 A：
- 将 I/O 异常视为"不可恢复的不变量破坏"，**允许抛出**
- 理由：Dispose 失败意味着文件状态已损坏，继续写入也无意义
- 调用方可通过 try-finally 或上层恢复机制处理

### Decision 7.D: 单 Builder 约束（升级为硬约束）

**结论**：同一 `IRbfFile` 同时最多 1 个 open `RbfFrameBuilder`（@[S-RBF-BUILDER-SINGLE-OPEN]）。

**实现**：
- `RbfFileImpl` 维护 `_hasActiveBuilder` 标志
- `BeginAppend()` 检查标志，若为 true 抛出 `InvalidOperationException`
- **`Append()` 在 `_hasActiveBuilder == true` 时也抛出 `InvalidOperationException`**（与 BeginAppend 一致）
- Builder `Dispose()` 时清除标志

### Decision 7.E: PayloadCrc 计算方案

**结论**：采用 **SinkReservableWriter.GetCrcSinceReservationEnd()** 方案——在 Data 层添加从 reservation 末尾计算 CRC 的能力。

**设计演进**：
- 原方案（RbfWriteSink 合并方案）存在设计冲突：HeadLen reservation 阻塞 flush 导致 CRC 无法在 Push 时计算
- 新方案：CRC 计算在 EndAppend 时一次性完成，遍历 `SinkReservableWriter` 内部 buffer 计算

**设计要点**：
1. `SinkReservableWriter` 新增 `GetCrcSinceReservationEnd(token)` 方法
2. 从 reservation 末尾（HeadLen 之后）扫描到当前写入末尾
3. CRC 覆盖 Payload + TailMeta + Padding（符合 @[F-PAYLOAD-CRC-COVERAGE]）
4. 前置条件：只能有 1 个 pending reservation（HeadLen）

**关键特性**：
- HeadLen reservation 阻塞 `SinkReservableWriter` 的 flush
- 整帧数据在内存中，支持 **Zero-IO 取消**（无论 Payload 多大）
- CRC 计算不依赖 flush 时机，在 EndAppend 时统一计算

**数据流**：
```
SinkReservableWriter
    → RandomAccessByteSink(frameStart)  // 纯文件写入，无 CRC

写入顺序：[HeadLen(reservation)] → [Payload + TailMeta] → [Padding]
                                           ↓
                        EndAppend 时：GetCrcSinceReservationEnd(headLenToken)
                                           ↓
                                      写入 [Tail]
```

**Data 层扩展**：`SinkReservableWriter.GetCrcSinceReservationEnd()` 是通用能力，适用于任何需要"对 reservation 之后的数据计算 CRC"的场景。

### Decision 7.F: 资源上限控制

**结论**：在 EndAppend 中检查 Payload+TailMeta 上限。

**实现**：
- `RbfFrameBuilder.EndAppend()` 中校验 `payloadAndMetaLength > MaxPayloadAndMetaLength`
- 若超过抛出 `InvalidOperationException`
- 校验位置在 `long -> int` 转换之前，防止溢出

### Decision 7.G: TailOffset 推进公式

**结论**：`TailOffset = startOffset + frameLength + FenceSize`

**与 RbfAppendImpl 对齐**：
- `SizedPtr` 返回 `(startOffset, frameLength)` — 不含 Fence
- `TailOffset` 推进到 `startOffset + frameLength + FenceSize` — 含 Fence
- 下一次 Append 从 `TailOffset` 开始写入

---

## 实现任务

### Part A: 核心基础设施

#### Task 7.1: ✅ 已完成 — SinkReservableWriter.GetCrcSinceReservationEnd()

**执行者**：Implementer（由监护人直接调度完成）
**状态**：已完成

**任务简报**：
为 `SinkReservableWriter` 添加 `GetCrcSinceReservationEnd()` 方法，支持从 reservation 末尾计算 CRC。

**实现说明**：
- 新增方法签名：`uint GetCrcSinceReservationEnd(int token, uint initValue = ..., uint finalXor = ...)`
- 从 reservation 末尾遍历到当前写入末尾，计算 CRC
- 前置条件：只能有 1 个 pending reservation
- 文件位置：`atelia/src/Data/SinkReservableWriter.cs`

**验收标准**：
- [x] 编译通过
- [x] 从 reservation 末尾正确计算 CRC
- [x] 前置条件校验（1 个 pending reservation）

---

#### Task 7.2: ✅ 已完成 — RbfFrameBuilder（sealed class）

**执行者**：Implementer（由监护人直接调度完成）
**状态**：已完成

**任务简报**：
实现 `RbfFrameBuilder`，支持流式写入 payload 并提交帧。

**实现说明**：
- `sealed class` 类型，使用 `RandomAccessByteSink` + `SinkReservableWriter`
- 构造时预留 HeadLen（阻塞 flush，支持 Zero-IO 取消）
- `EndAppend()` 使用 `GetCrcSinceReservationEnd()` 计算 PayloadCrc
- `Dispose()` 未 commit 时执行 Auto-Abort（`_writer.Reset()`）

**类结构**（实际实现）：
```csharp
public sealed class RbfFrameBuilder : IDisposable {
    private readonly long _frameStart;
    private readonly RandomAccessByteSink _sink;         // 纯文件写入
    private readonly SinkReservableWriter _writer;
    private readonly int _headLenReservationToken;
    private readonly Action<long> _onCommitCallback;
    private readonly Action _clearBuilderFlag;
    private bool _committed;
    private bool _disposed;
    
    public IReservableBufferWriter PayloadAndMeta => _writer;
    public SizedPtr EndAppend(uint tag, int tailMetaLength = 0);
    public void Dispose();
}
```

**EndAppend 逻辑**（实际实现）：
1. 验证前置条件（disposed、committed、PendingReservationCount == 1）
2. 获取 `payloadAndMetaLength = _writer.WrittenLength - HeadLenSize`
3. 资源上限校验（Decision 7.F）
4. 验证 tailMetaLength 约束
5. 计算 FrameLayout
6. 写入 Padding（0 字节填充）
7. 获取 PayloadCrc：`_writer.GetCrcSinceReservationEnd(_headLenReservationToken)`
8. 构建并写入 Tail buffer（PayloadCrc + Trailer + Fence）
9. 回填 HeadLen，调用 Commit（触发 flush）
10. 调用 `_onCommitCallback(endOffset)`
11. 返回 `SizedPtr(_frameStart, layout.FrameLength)`

**验收标准**：
- [x] 编译通过
- [x] `sealed class` 类型
- [x] `PayloadAndMeta` 返回 `IReservableBufferWriter`
- [x] `EndAppend` 正确完成帧尾部
- [x] 前置条件校验
- [x] `Dispose` 未 commit 时执行 Auto-Abort
- [ ] 提交的帧通过 `ReadFrame` L3 完整校验（需集成测试）
- [ ] `ScanReverse` 能枚举到该帧（需集成测试）

---

### Part B: 门面集成

#### Task 7.3: ✅ 已完成 — RbfFileImpl.BeginAppend 和 Append 互斥

**执行者**：Implementer
**依赖**：Task 7.2 ✅
**状态**：已完成

**任务简报**：
实现 `RbfFileImpl.BeginAppend()` 并添加 Append/BeginAppend 互斥约束。

**变更要点**：
1. 添加 `_hasActiveBuilder` 字段
2. `BeginAppend` 检查 `_hasActiveBuilder`，若为 true 抛出 `InvalidOperationException`
3. `BeginAppend` 检查 Disposed、TailOffset 4B 对齐、MaxFileOffset
4. 创建 `RbfFrameBuilder` 实例（传入 handle、frameStart、回调等）
5. 设置 `_hasActiveBuilder = true`
6. 返回 builder
7. **修改 `Append()` 方法**：在开头检查 `_hasActiveBuilder`，若为 true 抛出 `InvalidOperationException`

**并发约束（@[S-RBF-BUILDER-SINGLE-OPEN] 升级版）**：
- `BeginAppend()` 在 active builder 存在时抛出异常
- `Append()` 在 active builder 存在时抛出异常
- Builder `Dispose()` 清除标志后，两者均可用

**验收标准**：
- [ ] 编译通过
- [ ] 单 Builder 约束生效（重复 BeginAppend 抛异常）
- [ ] **Builder 期间 Append 抛异常**
- [ ] Builder Dispose 后可再次 BeginAppend/Append

---

#### Task 7.4: ✅ 已完成 — TailOffset 更新机制

**执行者**：Implementer
**依赖**：Task 7.3 ✅
**状态**：已完成（逻辑在 Task 7.2 中实现，测试在 Task 7.7 中覆盖）

**任务简报**：
确保 `TailOffset` 在 `EndAppend` 成功后正确更新。

**变更要点**：
1. `RbfFrameBuilder.EndAppend` 成功后，调用回调 `_onCommitCallback(endOffset)`
2. `RbfFileImpl` 在回调中更新 `_tailOffset = endOffset`
3. Auto-Abort 路径也调用回调（Tombstone 也占用空间）

**TailOffset 公式（@Decision 7.G）**：
```csharp
endOffset = startOffset + frameLength + FenceSize
```

**验收标准（@[S-RBF-TAILOFFSET-UPDATE]）**：
- [ ] `EndAppend` 成功后 `TailOffset == startOffset + frameLength + FenceSize`
- [ ] Auto-Abort (Tombstone) 后 `TailOffset` 也正确推进
- [ ] Auto-Abort (Zero I/O) 后 `TailOffset` 保持不变
- [ ] Builder 生命周期内（commit 前）`TailOffset` 不变

---

### Part C: Auto-Abort 实现

#### Task 7.5: ✅ 已完成 — Zero I/O Auto-Abort 路径

**执行者**：Implementer
**依赖**：Task 7.4 ✅
**状态**：已完成（逻辑在 Task 7.2 中实现，测试在 Task 7.8 中覆盖）

**任务简报**：
由于 HeadLen reservation 阻塞 flush，所有数据都在内存中，`Dispose` 直接丢弃即可实现 Zero I/O。

**判断条件**：
- `_writer.PushedLength == 0`（无数据落盘）
- **在方案 B 下，这始终为 true**（HeadLen reservation 阻塞 flush）

**实现步骤**：
1. 检测 `!_committed && !_disposed`
2. 调用 `_writer.Reset()` 丢弃所有缓冲（包括 HeadLen reservation）
3. 不调用 `_onCommitCallback`（TailOffset 不变）
4. 设置 `_disposed = true`

**验收标准**：
- [ ] 编译通过
- [ ] **任意 Payload 大小**：Dispose 后文件长度不变（Zero I/O）
- [ ] 单元测试：写入 100KB 数据 → Dispose → 验证文件无变化
- [ ] 单元测试：写入后多次 ReserveSpan/Commit → Dispose → 验证文件无变化

---

#### Task 7.6: ✅ 已完成（防御性保留）— Tombstone 路径

**执行者**：Implementer
**依赖**：Task 7.5 ✅
**状态**：已完成（在当前设计下不会触发，作为防御性代码保留）

**任务简报**：
在方案 B 下，由于 HeadLen reservation 阻塞 flush，**正常情况下不会触发 Tombstone 路径**。
但保留此路径作为防御性实现，处理极端边界情况。

**触发条件**（理论上不应发生）：
- `_writer.PushedLength > 0`（数据已落盘）

**实现步骤**（与之前相同，作为防御性代码）：
1. 获取 `payloadAndMetaLength = _crcSink.GetCrcCoveredLength()`
2. 构建 `FrameLayout(payloadAndMetaLength, 0)`
3. 写入 Padding + PayloadCrc + TrailerCodeword（**IsTombstone = true**）+ Fence
4. 回填 HeadLen（Commit reservation）
5. 调用 `_onCommitCallback(endOffset)`（Tombstone 也占用空间）

**异常处理（@Decision 7.C.1）**：
- I/O 异常允许传播（视为不可恢复）

**关键约束**：
- Tombstone 帧结构完整（可被 ScanReverse 正确解析）
- `ScanReverse(showTombstone=false)` 会跳过此帧

**验收标准**：
- [ ] 编译通过
- [ ] Tombstone 场景：Dispose 后文件包含完整 Tombstone 帧
- [ ] `ScanReverse()` 默认跳过 Tombstone
- [ ] `ScanReverse(showTombstone=true)` 能看到 Tombstone
- [ ] Tombstone 的 `ReadFrame` 校验通过（L3 双 CRC）

---

### Part D: 测试覆盖

#### Task 7.7: ✅ 已完成 — RbfFrameBuilder 基本功能测试

**执行者**：Implementer
**依赖**：Task 7.4 ✅
**状态**：已完成（14 个测试用例）

**任务简报**：
创建 `tests/Rbf.Tests/RbfFrameBuilderTests.cs`。

**测试用例**：
1. **正常路径**：BeginAppend → 写入数据 → EndAppend → ReadFrame 验证帧可读
2. **空 Payload**：BeginAppend → EndAppend(tag, 0) → 验证最小帧
3. **带 TailMeta**：写入 Payload + TailMeta → EndAppend(tag, tailMetaLen) → ReadTailMeta 验证内容
4. **使用 Reservation**：ReserveSpan → 写入数据 → Commit → EndAppend
5. **重复 EndAppend**：应抛出 `InvalidOperationException`
6. **重复 Dispose**：幂等，不抛异常
7. **未提交 Reservation 时 EndAppend**：应抛出 `InvalidOperationException`
8. **TailMetaLength 超过 PayloadAndMetaLength**：应抛出 `InvalidOperationException`

**验收标准**：
- [ ] 所有测试通过
- [ ] 覆盖正常路径和边界条件
- [ ] 提交的帧通过 L3 完整校验

---

#### Task 7.8: ✅ 已完成 — Auto-Abort 测试

**执行者**：Implementer
**依赖**：Task 7.6 ✅
**状态**：已完成（6 个测试用例）

**任务简报**：
在 `RbfFrameBuilderTests.cs` 中添加 Auto-Abort 测试。

**测试用例（方案 B 下 Zero-IO 是默认行为）**：
1. **Zero I/O Abort（小 Payload）**：BeginAppend → 写 100B → Dispose → 验证文件无变化
2. **Zero I/O Abort（大 Payload）**：BeginAppend → 写 100KB → Dispose → 验证文件无变化（HeadLen reservation 阻塞）
3. **Zero I/O Abort（带 Reservation）**：BeginAppend → ReserveSpan → 写数据 → Commit → Dispose → 验证文件无变化
4. **Abort 后可继续 Append**：Dispose 后 Append 成功
5. **Abort 后可继续 BeginAppend**：Dispose 后 BeginAppend 成功
6. **Abort 后 TailOffset 不变**：验证 `file.TailOffset` 与 Abort 前相同

**Tombstone 测试（防御性，理论上不触发）**：
7. **Tombstone 帧结构完整**（如果触发）：ReadFrame 通过 L3 校验
8. **Tombstone 可见性**：ScanReverse(false) 跳过，ScanReverse(true) 包含

**验收标准**：
- [ ] 所有测试通过
- [ ] **重点验证**：任意 Payload 大小都是 Zero-IO Abort
- [ ] 验证 @[S-RBF-BUILDER-DISPOSE-ABORTS-UNCOMMITTED-FRAME] 语义

---

#### Task 7.9: ✅ 已完成 — 单 Builder 约束与 Append 互斥测试

**执行者**：Implementer
**依赖**：Task 7.3 ✅
**状态**：已完成（6 个测试用例）

**任务简报**：
在 `RbfFrameBuilderTests.cs` 中添加并发约束测试。

**测试用例**：
1. **重复 BeginAppend**：BeginAppend → BeginAppend → 抛出 `InvalidOperationException`
2. **Dispose 后可 BeginAppend**：BeginAppend → Dispose → BeginAppend → 成功
3. **EndAppend 后可 BeginAppend**：BeginAppend → EndAppend → BeginAppend → 成功
4. **Builder 期间 Append 抛异常**：BeginAppend → Append → 抛出 `InvalidOperationException`
5. **Dispose 后可 Append**：BeginAppend → Dispose → Append → 成功
6. **EndAppend 后可 Append**：BeginAppend → EndAppend → Append → 成功

**验收标准**：
- [ ] 所有测试通过
- [ ] 验证 @[S-RBF-BUILDER-SINGLE-OPEN] 约束（含 Append 互斥）

---

#### Task 7.10: ✅ 已完成 — 与 ScanReverse 集成测试

**执行者**：Implementer
**依赖**：Task 7.8 ✅
**状态**：已完成（6 个测试用例）

**任务简报**：
验证 Builder 写入的帧可被 ScanReverse 正确扫描。

**测试用例**：
1. **混合写入**：Append + BeginAppend/EndAppend 交替 → ScanReverse 返回正确顺序
2. **大帧写入**：Builder 写入 > 4KB payload → ScanReverse + ReadFrame 验证完整性
3. **TailMeta 读取**：Builder 带 TailMeta → ScanReverse → ReadTailMeta 验证内容
4. **多帧序列**：连续 5 次 BeginAppend/EndAppend → ScanReverse 验证逆序正确

**验收标准**：
- [ ] 所有测试通过
- [ ] 端到端集成验证

---

## 规范引用

| 条款 | 文档 | 要点 |
|------|------|------|
| @[A-RBF-FRAME-BUILDER] | rbf-interface.md | RbfFrameBuilder 定义 |
| @[S-RBF-BUILDER-DISPOSE-ABORTS-UNCOMMITTED-FRAME] | rbf-interface.md | Auto-Abort 逻辑语义 |
| @[S-RBF-BUILDER-SINGLE-OPEN] | rbf-interface.md | 单 Builder 约束 |
| @[S-RBF-TAILOFFSET-UPDATE] | rbf-interface.md | TailOffset 更新规则 |
| @[F-FRAMEBYTES-LAYOUT] | rbf-format.md | FrameBytes 布局（v0.40） |
| @[F-FRAME-DESCRIPTOR-LAYOUT] | rbf-format.md | FrameDescriptor 位布局（含 IsTombstone） |
| @[F-PAYLOAD-CRC-COVERAGE] | rbf-format.md | PayloadCrc 覆盖范围 |

---

## 相关文档

| 文档 | 职责 |
|:-----|:-----|
| **本文档（task.md）** | 执行蓝图：任务分解、验收标准、规范引用 |
| [Stage 06](../06/task.md) | 前置 Stage（帧格式 v0.40 + ScanReverse） |
| [rbf-interface.md](../../atelia/docs/Rbf/rbf-interface.md) | 接口规范（@[A-RBF-FRAME-BUILDER]） |
| Stage 08 | 后续 Stage（DurableFlush + Truncate） |

---

## 已决策问题（从待讨论升级）

1. **PayloadCrc 增量计算**：✅ **Decision 7.E**
   - 采用 `Crc32cByteSink` wrapper 方案
   - 不修改 `SinkReservableWriter`，避免 RBF 语义渗透到 Data 层

2. **Builder 期间 Append 行为**：✅ **Decision 7.D 升级**
   - 硬约束：`Append()` 在 active builder 期间抛出 `InvalidOperationException`

3. **TailMeta 长度验证时机**：✅ **Task 7.2 EndAppend 前置条件**
   - `EndAppend` 时验证 `tailMetaLength <= payloadAndMetaLength`
   - `EndAppend` 时验证 `tailMetaLength <= MaxTailMetaLength`

4. **Auto-Abort 异常策略**：✅ **Decision 7.C.1**
   - Tombstone 路径的 I/O 异常允许传播（视为不可恢复）

5. **TailOffset 推进公式**：✅ **Decision 7.G**
   - `TailOffset = startOffset + frameLength + FenceSize`

