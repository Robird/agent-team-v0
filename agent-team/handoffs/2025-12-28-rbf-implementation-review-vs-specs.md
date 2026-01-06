# RBF 实现审阅报告（atelia/src/Rbf）

- 审阅目标：评估 `atelia/src/Rbf` 的实现是否符合
  - 线格式规范：`atelia/docs/StateJournal/rbf-format.md`（v0.16，2025-12-28）
  - 接口契约：`atelia/docs/StateJournal/rbf-interface.md`（v0.17，2025-12-28）
  - 总体设计：`atelia/docs/StateJournal/mvp-design.md`（v3.9，2025-12-28）
- 审阅范围：仅限 Layer 0（RBF framing / CRC / reverse scan），不评估上层 payload 语义。

---

## 0. 结论摘要（核心洞察）

- **对 `rbf-format.md`（v0.15）**：实现整体上**符合 SSOT**（Fence/FrameBytes 布局、StatusLen 计算、CRC32C 覆盖范围、Reverse Scan + Resync 行为）。
  - v0.15 已修订旧版本的内部矛盾（最小 HeadLen、FrameStatus 填充值域），现文档与实现一致。
- **对 `rbf-interface.md`（v0.15）**：接口层已移除 `FrameStatus` 类型，RbfFrame 直接暴露 `bool IsTombstone` 属性。
  - ✅ v0.15 新增 ScanReverse 返回类型规范（`RbfReverseSequence` ref struct + 6 条语义条款）
  - ❌ 实现仍使用 `IEnumerable<RbfFrame>`，与规范存在**类型系统层面的根本差异**（见 §3.4）

**待办事项**：
1. ✅ ~~修订 `rbf-format.md` 的内部矛盾条款~~（已在 v0.15 完成）
2. ✅ ~~同步更新 `rbf-test-vectors.md`~~（已在 v0.9 完成，新增 ScanReverse 测试向量）
3. ✅ ~~更新 `rbf-interface.md` 的 Tombstone 接口~~（v0.12 重构 FrameStatus → v0.13 移除中间类型）
4. ✅ ~~更新 `rbf-interface.md` 的 ScanReverse 返回类型规范~~（v0.15 畅谈会决议）
5. ✅ ~~简化 `RbfFrameBuilder` Payload 接口~~（v0.16：`Payload` + `ReservablePayload` → 单一 `IReservableBufferWriter Payload`，[畅谈会决议](../meeting/2025-12-28-rbf-builder-payload-simplification.md)）
6. ❌ **实现层跟进 Payload 接口简化**（高优先，需删除 `ReservablePayload` 属性，统一为 `IReservableBufferWriter Payload`）
7. ❌ **实现层跟进 ScanReverse 返回类型**（高优先，类型系统层面不符合）
8. ❌ **更新 `RbfFrame` 形态**（F1 剩余部分）
9. ❌ **移除 `FrameTag` struct，RBF 层直接使用 `uint`**（v0.17 规范变更）
10. ❌ **在 StateJournal 层实现 FrameTag packing/unpacking 工具方法**（v0.17 规范变更）
11. ⚠️ **考虑限制 `RbfFramer(writeGenesis: false)` 的可见性**（F3，中优先）

---

## 1. 证据索引（实现入口）

- 写入（Framing）：
  - `atelia/src/Rbf/RbfFramer.cs`（Append、BeginFrame/Commit、CRC 追踪、Fence 写入）
  - `atelia/src/Rbf/RbfFileFramer.cs`（文件后端封装）
  - `atelia/src/Rbf/RbfLayout.cs`（StatusLen/HeadLen 公式与对齐）
- 读取（Scanner）：
  - `atelia/src/Rbf/RbfScanner.cs`（内存版逆向扫描+校验）
  - `atelia/src/Rbf/RbfFileScanner.cs`（file-backed 逆向扫描 + 分块 CRC）
- 基础类型：
  - `atelia/src/Rbf/FrameStatus.cs`（位域 FrameStatus）
  - `atelia/src/Rbf/FrameTag.cs`（**待移除**，v0.17 规范变更）、`atelia/src/Rbf/<deleted-place-holder>.cs`
  - `atelia/src/Rbf/RbfConstants.cs`（Fence bytes）
  - `atelia/src/Rbf/RbfCrc.cs`（CRC32C Castagnoli reflected）
- 测试佐证：
  - `atelia/tests/Rbf.Tests/RbfFramerTests.cs`

---

## 2. 对 `rbf-format.md`（Layer 0 线格式）的符合性审阅

> 以 `rbf-format.md` 声明的 SSOT 为准：
> - §3 的 `[F-FRAME-LAYOUT]`
> - §4 的 `[F-CRC32C-COVERAGE]` / `[F-CRC32C-ALGORITHM]`
> - §6 的 `[R-REVERSE-SCAN-ALGORITHM]` / `[R-RESYNC-BEHAVIOR]`

### 2.1 Fence / Genesis

**规范点**：
- `[F-FENCE-DEFINITION]` Fence 值为 ASCII 字节序列 `52 42 46 31`（`RBF1`），按字节匹配。
- `[F-GENESIS]` 文件偏移 0 MUST 是 Genesis Fence。

**实现核对**：
- `RbfConstants.FenceBytes => [0x52, 0x42, 0x46, 0x31]`，与规范一致。
- `RbfFileFramer`：当 `Backend.Length == 0` 时构造 `RbfFramer(... writeGenesis: true)`，会写入 Genesis Fence。
- `RbfFramer` 公开参数 `writeGenesis` 允许禁用 Genesis（见 `RbfFramer(... writeGenesis: false)` 测试用例）。

**结论**：
- 对“默认路径”（新建文件/空文件）来说：**符合**。
- **潜在偏差**：`RbfFramer` 允许 `writeGenesis: false` 与 `[F-GENESIS]` 的 MUST 冲突。若该构造方式进入生产路径，会导致“不符合格式规范”。

**建议**：
- 若 `writeGenesis: false` 仅用于测试/特殊场景：建议将其降级为 internal 或在 API 文档中标注“非规范模式/仅测试”。

### 2.2 FrameBytes 布局与长度公式

**规范点（SSOT）**：
- `[F-FRAME-LAYOUT]`：HeadLen(4) + FrameTag(4) + Payload(N) + FrameStatus(1-4) + TailLen(4) + CRC32C(4)。
- `[F-STATUSLEN-FORMULA]` / `[F-HEADLEN-FORMULA]`：
  - $StatusLen = 1 + ((4 - ((PayloadLen + 1) \% 4)) \% 4)$
  - $HeadLen = 16 + PayloadLen + StatusLen$

**实现核对**：
- `RbfLayout.CalculateStatusLength(int payloadLength)` 实现与公式一致。
- `RbfLayout.CalculateFrameLength(...)` 使用 `16 + payload + statusLen`。
- `RbfFramer.WriteFrameComplete(...)` 写入顺序与布局一致；`RbfFramer.CommitFrameStreaming(...)` 亦按该布局补齐 FrameStatus/TailLen，并回填 HeadLen。
- `RbfScanner` / `RbfFileScanner`：
  - 校验 `headLen % 4 == 0`
  - 校验 `HeadLen == TailLen`
  - 通过 `payloadPlusStatus = headLen - 16` 与从 FrameStatus 推导出的 `statusLen` 反解出 `payloadLen`
  - 再反向验证 `expectedStatusLen == statusLen`

**结论**：**符合**（并且有测试覆盖：`RbfFramerTests.Append_VariousPayloadLengths_CorrectStatusLen` 等）。

> **已解决**：`rbf-format.md` v0.15 已修订最小 HeadLen 为 20，与 SSOT 公式一致。

### 2.3 FrameStatus 位域 + Fill 校验

**规范点（SSOT）**：
- `[F-FRAMESTATUS-VALUES]`：Bit7 Tombstone，Bit0-1 编码 `StatusLen-1`，Bit2-6 保留位 MVP MUST 为 0。
- `[F-FRAMESTATUS-FILL]`：FrameStatus 多字节填充必须“全字节同值”。

**实现核对**：
- `FrameStatus`（`atelia/src/Rbf/FrameStatus.cs`）按位域实现：
  - `IsTombstone` 取 bit7
  - `StatusLen = (value & 0x03) + 1`
  - `IsMvpValid = (value & 0x7C) == 0`（保留位必须为 0）
- 写入：`RbfFramer` 写 FrameStatus 时使用 `statusSpan.Fill(statusByte)`，满足“全字节同值”。
- 读取：`RbfScanner` / `RbfFileScanner`：
  - 从 `recordEnd - 9` 读取 statusByte（TailLen 之前的最后一个 FrameStatus 字节）
  - 校验 `status.IsMvpValid`
  - 校验 `ValidateStatusFill(...)`（所有 statusLen 字节 == statusByte）

**结论**：对位域 SSOT：**符合**。

> **已解决**：`rbf-format.md` v0.15 已删除过时的 `0x00/0xFF` 值域枚举，现在 `[F-FRAMESTATUS-FILL]` 只要求“全字节同值”，并引用位域 SSOT 定义合法值。

### 2.4 CRC32C 覆盖范围与算法

**规范点（SSOT）**：
- `[F-CRC32C-COVERAGE]`：$CRC32C = crc32c(FrameTag + Payload + FrameStatus + TailLen)$
- `[F-CRC32C-ALGORITHM]`：CRC32C Castagnoli reflected，init/xorout 均为 `0xFFFFFFFF`。

**实现核对**：
- `RbfCrc`：使用 reflected polynomial `0x82F63B78`，Begin/End 与规范一致。
- `RbfFramer.WriteFrameComplete`：crcStart=4（FrameTag 起点），crcLen = 4 + payloadLen + statusLen + 4（含 TailLen），与覆盖范围一致。
- `RbfFramer.CommitFrameStreaming`：通过 `CrcPositionTrackingBufferWriter` 在写入过程中增量计算 CRC（排除 HeadLen），最终写出 CRC。
- `RbfScanner` / `RbfFileScanner`：均按 `[frameStart+4, recordEnd-4)` 验证 CRC。

**结论**：**符合**（有单元测试 `Append_CrcCoversCorrectRange` 佐证）。

### 2.5 Reverse Scan / Resync 行为

**规范点（SSOT）**：
- `[R-REVERSE-SCAN-ALGORITHM]`：从 file tail alignDown4 扫 fence；验证失败按 4B 步长继续寻找（resync）。
- `[R-RESYNC-BEHAVIOR]`：校验失败 MUST NOT 信任 TailLen 做跳跃；只能 4B 步长 resync。

**实现核对**：
- `RbfScanner.ScanReverseInternal` 与 `RbfFileScanner.ScanReverseInternal`：
  - `fencePos = AlignDown4(fileLength - 4)`
  - 非 fence：`fencePos -= 4`
  - fence 命中后，读取 `tailLen` 计算候选 `frameStart`；若任何校验失败，仍然 `fencePos -= 4`（而不是用 tailLen 跳跃）。
  - 仅在验证成功时，`fencePos = prevFencePos`（按规范算法回到上一个 fence）。

**结论**：**符合**。

---

## 3. 对 `rbf-interface.md`（接口契约 Layer 0/1 边界）的符合性审阅

### 3.1 FrameTag / <deleted-place-holder>

- `FrameTag`：
  - **规范变更（v0.17）**：`[F-FRAMETAG-DEFINITION]` 已移除，FrameTag 在 RBF 层不再定义为独立类型，仅作为线格式的 4 字节 uint（LE）字段
  - **实现现状**：仍存在 `public readonly record struct FrameTag(uint Value)`，**需移除**
  - **迁移计划**：FrameTag 语义解析（packing/unpacking）移至 StateJournal 层
- <deleted-place-holder>：实现为 `public readonly record struct <deleted-place-holder>(ulong Value)`，并提供 `Null/IsNull/IsValid`，与 `[F-<deleted-place-holder>-DEFINITION]` / `[F-<deleted-place-holder>-ALIGNMENT]` / `[F-<deleted-place-holder>-NULL]` 一致。

**结论**：<deleted-place-holder> **符合**；FrameTag **待移除**（v0.17 规范变更）。

### 3.2 Tombstone 接口（已解决 ✅）

**接口文档（v0.13）**：
- 移除 `FrameStatus` 类型，RbfFrame 直接暴露 `bool IsTombstone` 属性
- 接口层不再定义中间类型，简化为单一布尔属性

**实现**：
- `FrameStatus` 为 **位域 struct**（见 `atelia/src/Rbf/FrameStatus.cs`），提供 `IsTombstone` 属性
- 内部实现使用位域编码（Bit 7 = Tombstone），但这是实现细节，不暴露到接口层
- 实现层保留 `FrameStatus` struct 用于位域解析，接口层无需对应类型

**结论**：对 `rbf-interface.md v0.13`：**符合**。

> v0.12→v0.13 演进：v0.12 将 enum 改为 `readonly struct { bool IsTombstone }`，v0.13 进一步移除中间类型，RbfFrame 直接暴露 `bool IsTombstone`。简化接口，消除只有一个属性的中间类型。

### 3.3 RbfFrame 形态（待更新）

**接口文档（v0.13）**：
- `public readonly ref struct RbfFrame { FrameTag Tag; bool IsTombstone; ReadOnlySpan<byte> Payload; <deleted-place-holder> Address; ... }`

**实现**：
- `atelia/src/Rbf/RbfFrame.cs`：
  - `public readonly record struct RbfFrame(long FileOffset, uint FrameTag, long PayloadOffset, int PayloadLength, FrameStatus Status)`
  - 不提供 `ReadOnlySpan<byte> Payload`（以 offset/length 表示，并通过 `IRbfScanner.ReadPayload(in RbfFrame)` 做拷贝读取）
  - `FrameTag` 字段类型为 `uint` 而非 `FrameTag`
  - 未显式暴露 <deleted-place-holder>（可由 `FileOffset` 推导，但不是契约中的形态）

**结论**：对 `rbf-interface.md v0.13`：**不符合**（形态差异，待后续更新）。

### 3.4 IRbfScanner / ScanReverse 返回类型（待实现跟进 ❌）

**接口文档（v0.15）**：
- `bool TryReadAt(<deleted-place-holder> address, out RbfFrame frame);`
- `RbfReverseSequence ScanReverse();`（返回 ref struct，duck-typed 枚举器模式）

**v0.15 新增条款**（[畅谈会决议](../meeting/2025-12-28-scan-reverse-return-type.md)）：
| 条款 ID | 内容摘要 |
|:--------|:---------|
| `[A-RBF-REVERSE-SEQUENCE]` | 返回 duck-typed 可枚举序列，枚举器为 stack-only |
| `[S-RBF-SCANREVERSE-NO-IENUMERABLE]` | 返回类型 MUST NOT 实现 `IEnumerable<RbfFrame>` |
| `[S-RBF-SCANREVERSE-EMPTY-IS-OK]` | 空序列 MUST 产生 0 元素，不得抛异常 |
| `[S-RBF-SCANREVERSE-CURRENT-LIFETIME]` | `Current` 生命周期 ≤ 下次 `MoveNext()` |
| `[S-RBF-SCANREVERSE-CONCURRENT-MUTATION]` | 并发修改行为：未定义 |
| `[S-RBF-SCANREVERSE-MULTI-GETENUM]` | 多次 `GetEnumerator()` 返回独立枚举器 |

**设计理由**：`RbfFrame` 是 `ref struct`，不能作为泛型接口的类型参数。`IEnumerable<RbfFrame>` 在类型系统层面不可行。

**实现**：
- `atelia/src/Rbf/IRbfScanner.cs`：
  - `IEnumerable<RbfFrame> ScanReverse();` ← **直接违反 `[S-RBF-SCANREVERSE-NO-IENUMERABLE]`**
  - 额外添加 `byte[] ReadPayload(in RbfFrame frame);`

**结论**：对 `rbf-interface.md v0.15`：**不符合**（类型系统层面的根本差异）。

**后续行动**：实现层需要重构为 duck-typed 枚举器模式，参考 `Span<T>.GetEnumerator()` 的成熟先例。

### 3.5 IRbfFramer / RbfFrameBuilder 语义

**接口文档要点**：
- `IRbfFramer.Append/BeginFrame/Flush` 语义
- `[S-RBF-BUILDER-SINGLE-OPEN]`：同一时刻最多 1 个 open builder
- `[S-RBF-BUILDER-AUTO-ABORT]`：Dispose 未 Commit 时 logical non-existence；物理实现 SHOULD zero-I/O，否则 MUST tombstone；**v0.16 新增**：明确 Dispose 在 Auto-Abort 分支 MUST NOT throw
- `[S-RBF-BUILDER-FLUSH-NO-LEAK]`（v0.16 新增）：open builder 期间 Flush MUST NOT 外泄未提交字节

**v0.16 Payload 接口简化**（[畅谈会决议](../meeting/2025-12-28-rbf-builder-payload-simplification.md)）：
- 原设计：`IBufferWriter<byte> Payload` + `IReservableBufferWriter? ReservablePayload`
- 新设计：单一 `IReservableBufferWriter Payload`（因 `IReservableBufferWriter : IBufferWriter<byte>` 是严格超集）
- 设计理由：消除分支代码、降低认知负担、承认 `IReservableBufferWriter` 是 RBF 写入的原生协议

**实现核对**：
- 单开：`RbfFramer` 用 `_hasOpenBuilder` 防止同时开启多个 builder，并阻止 builder open 时 Append。
- Auto-abort：streaming builder 使用 `ChunkedReservableWriter`，Dispose 未 Commit 时走 `AbortFrameStreaming` 丢弃缓冲，属于 **Zero I/O** 路径；测试 `BeginFrame_DisposeWithoutCommit_DoesNotWriteFrame_ZeroIoAbort` 覆盖。
- `Flush()`：
  - `RbfFramer.Flush()` 为 no-op（因为 `IBufferWriter` 无 flush 概念）
  - `RbfFileFramer.Flush()` 调用 `FileBackendBufferWriter.Flush()` → `RbfFileBackend.Flush(flushToDisk:false)`，符合 `[S-RBF-FRAMER-NO-FSYNC]`。
- **Payload 接口**：实现层当前仍为双属性设计，**需跟进 v0.16 简化**。

**结论**：语义层面 **基本符合**，但 Payload 接口需跟进 v0.16 简化。

---

## 4. FixList（问题 + 定位 + 建议）

### F1（规范已更新，待实现跟进）：`ScanReverse()` 返回类型不符合规范

- **定位**：
  - 文档：`atelia/docs/StateJournal/rbf-interface.md`（v0.15）
  - 实现：`atelia/src/Rbf/IRbfScanner.cs`
- **规范变更（v0.15）** ✅：
  - 返回类型从 `RbfReverseEnumerable` 改为 `RbfReverseSequence`（ref struct）
  - 明确禁止实现 `IEnumerable<RbfFrame>`（因 RbfFrame 是 ref struct）
  - 新增 6 条语义条款（见 §3.4）
- **实现现状** ❌：
  - 仍返回 `IEnumerable<RbfFrame>`，直接违反 `[S-RBF-SCANREVERSE-NO-IENUMERABLE]`
- **建议**：
  - **高优先**：重构实现为 duck-typed 枚举器模式
  - 参考：`Span<T>.GetEnumerator()` 的成熟先例
  - 测试向量：`rbf-test-vectors.md` v0.9 §4 已提供覆盖用例

### F1.1（待解决）：`RbfFrame` 形态差异

- **定位**：
  - 文档：`atelia/docs/StateJournal/rbf-interface.md`
  - 实现：`atelia/src/Rbf/RbfFrame.cs`
- **问题**：
  - 文档：`ref struct` 提供 `Payload` span
  - 实现：`record struct` 提供 offset/length
- **建议**：后续版本评估是否需要对齐（可能影响性能特性）

### F2（已解决 ✅）：`rbf-format.md` 内部矛盾（最小长度、Status fill 值域）

- **定位**：`atelia/docs/StateJournal/rbf-format.md`
- **问题**（已修复）：
  - 最小 HeadLen/最小帧长度的描述与 SSOT 公式不一致（实现采用 20）
  - FrameStatus fill 的“0x00 或 0xFF”描述与 v0.14 位域 SSOT 不一致
- **解决方案**（v0.15，2025-12-28）：
  - 删除 `[F-FRAMESTATUS-FILL]` 中过时的 `0x00/0xFF` 值域枚举，添加对位域 SSOT 的引用
  - 修正 `[F-HEADLEN-FORMULA]` 注释中的最小 HeadLen 为 20
  - 修正 `[F-FRAMING-FAIL-REJECT]` 中对 FrameStatus 合法值的描述
  - 修正扫描算法中的最小帧长度检查（21 → 20）
  - 同步更新 `rbf-test-vectors.md` v0.8

### F3（中优先）：`RbfFramer(writeGenesis: false)` 公开参数可能引入“非规范文件”

- **定位**：`atelia/src/Rbf/RbfFramer.cs`
- **问题**：对外暴露禁用 Genesis Fence 的构造方式，与 `[F-GENESIS]` 冲突。
- **建议**：限制其可见性/用途（internal + tests），或在类型/注释中明确这是“非规范模式”。

---

## 5. 对齐策略与进度

### 已完成：规范层面

- ✅ 修订 `rbf-format.md` v0.14 → v0.15：消除内部矛盾，保证 SSOT 与解释性文字一致
- ✅ 修订 `rbf-interface.md` v0.10 → v0.16：
  - v0.12: FrameStatus 从 `enum { 0x00/0xFF }` 改为 `readonly struct { bool IsTombstone }`
  - v0.13: 移除 `FrameStatus` 类型，RbfFrame 直接暴露 `bool IsTombstone`
  - v0.15: ScanReverse 返回类型重构（`RbfReverseSequence` ref struct + 6 条语义条款）
  - v0.16: Payload 接口简化（`Payload` + `ReservablePayload` → 单一 `IReservableBufferWriter Payload`）+ 新增 `[S-RBF-BUILDER-FLUSH-NO-LEAK]` + `[S-RBF-BUILDER-AUTO-ABORT]` 增强
- ✅ 同步更新 `rbf-test-vectors.md` v0.7 → v0.9：
  - v0.8: 适配 rbf-format.md v0.15
  - v0.9: 新增 §4 ScanReverse 接口行为测试向量

### 待完成：实现层跟进

- ❌ **高优先**：跟进 Payload 接口简化（删除 `ReservablePayload`，`Payload` 类型改为 `IReservableBufferWriter`）
- ❌ **高优先**：重构 `IRbfScanner.ScanReverse()` 为 duck-typed 枚举器模式
- ❌ 评估 `RbfFrame` 形态对齐需求（ref struct vs record struct）

### 对齐策略说明

**当前策略**：规范先行，实现跟进（路线 A 的演进）

v0.15 的 ScanReverse 规范变更是基于[畅谈会分析](../meeting/2025-12-28-scan-reverse-return-type.md)的设计决策，理由：
- `RbfFrame` 是 `ref struct`，类型系统层面不允许作为 `IEnumerable<T>` 的类型参数
- Duck-typed 枚举器模式是 `Span<T>` 生态的成熟先例
- 零堆分配、编译期类型安全符合 RBF 层"高性能、零拷贝"定位

---

## 6. 附：与规范条款的逐项对照（简表）

| 条款/主题 | 规范来源 | 结论 | 证据 |
|---|---|---|---|
| Fence bytes = `RBF1` | `[F-FENCE-DEFINITION]` | 符合 | `RbfConstants.FenceBytes` |
| Genesis Fence | `[F-GENESIS]` | 默认路径符合；存在可选偏差 | `RbfFileFramer` / `RbfFramer(writeGenesis)` |
| FrameBytes 布局 | `[F-FRAME-LAYOUT]` | 符合 | `RbfFramer.WriteFrameComplete` / `CommitFrameStreaming` |
| StatusLen 公式 | `[F-STATUSLEN-FORMULA]` | 符合 | `RbfLayout.CalculateStatusLength` |
| HeadLen 公式 | `[F-HEADLEN-FORMULA]` | 符合 | `RbfLayout.CalculateFrameLength` |
| FrameStatus 位域 + reserved=0 | `[F-FRAMESTATUS-VALUES]` | 符合 | `FrameStatus.IsMvpValid` + scanner 校验 |
| FrameStatus fill all-bytes-same | `[F-FRAMESTATUS-FILL]` | 符合（按位域 SSOT） | `Fill(statusByte)` + `ValidateStatusFill` |
| CRC 覆盖范围 | `[F-CRC32C-COVERAGE]` | 符合 | `crcStart=4` / scanners `[+4, end-4)` |
| CRC32C 算法约定 | `[F-CRC32C-ALGORITHM]` | 符合 | `RbfCrc` |
| Reverse scan + resync | `[R-REVERSE-SCAN-ALGORITHM]` / `[R-RESYNC-BEHAVIOR]` | 符合 | `RbfScanner` / `RbfFileScanner` |
| IRbfFramer API | `[A-RBF-FRAMER-INTERFACE]` | 基本符合 | `IRbfFramer.cs`, `RbfFramer`, `RbfFileFramer` |
| Auto-Abort | `[S-RBF-BUILDER-AUTO-ABORT]` | 符合（走 Zero I/O） | `RbfFrameBuilder.Dispose` + tests |
| Flush 不外泄未提交 | `[S-RBF-BUILDER-FLUSH-NO-LEAK]` | 待验证（v0.16 新增） | 需检查 open builder 期间 Flush 行为 |
| Payload 接口 | `[A-RBF-FRAME-BUILDER]` (v0.16) | **不符合**（实现仍为双属性） | 需删除 `ReservablePayload`，统一为 `IReservableBufferWriter Payload` |
| Tombstone 接口 | `[F-TOMBSTONE-DEFINITION]` | **符合**（v0.13 移除中间类型） | `RbfFrame.IsTombstone` |
| IRbfScanner API | `[A-RBF-SCANNER-INTERFACE]` | **不符合**（返回类型根本差异） | `IRbfScanner.cs` vs doc |
| RbfReverseSequence | `[A-RBF-REVERSE-SEQUENCE]` | **不符合**（实现为 IEnumerable） | `IRbfScanner.cs` vs doc |
| ScanReverse 不实现 IEnumerable | `[S-RBF-SCANREVERSE-NO-IENUMERABLE]` | **不符合**（直接违反） | `IRbfScanner.cs` |
| 空序列合法 | `[S-RBF-SCANREVERSE-EMPTY-IS-OK]` | 符合（实现行为正确） | `RbfScanner.ScanReverse` |
| Current 生命周期 | `[S-RBF-SCANREVERSE-CURRENT-LIFETIME]` | 待验证（需实现重构后评估） | — |
| 并发修改行为 | `[S-RBF-SCANREVERSE-CONCURRENT-MUTATION]` | 符合（规范为 UB） | — |
| 多次 GetEnumerator | `[S-RBF-SCANREVERSE-MULTI-GETENUM]` | 符合（IEnumerable 语义） | `RbfScanner.cs` |
| RbfFrame 形态 | `[A-RBF-FRAME-REF-STRUCT]` | 不符合（struct 形态差异） | `RbfFrame.cs` vs doc |
| FrameTag 类型 | `[F-FRAMETAG-DEFINITION]` | **已移除**（v0.17 移除条款，RBF 层用 uint） | `FrameTag.cs` 待删除 |

---

> 初版生成：2025-12-28
>
> **修订历史**：
> - 2025-12-28 更新 #6：反映 `rbf-interface.md` v0.17 + `rbf-format.md` v0.16（FrameTag wrapper type 移除，[畅谈会决议](../meeting/2025-12-28-wrapper-type-audit.md)）；新增实现待办：移除 FrameTag struct、StateJournal 层实现 packing/unpacking 工具方法
> - 2025-12-28 更新 #5：反映 `rbf-interface.md` v0.16（Payload 接口简化，[畅谈会决议](../meeting/2025-12-28-rbf-builder-payload-simplification.md)）；新增 `[S-RBF-BUILDER-FLUSH-NO-LEAK]` 条款；`[S-RBF-BUILDER-AUTO-ABORT]` 增强（Dispose MUST NOT throw）；识别实现层需跟进 Payload 简化
> - 2025-12-28 更新 #4：反映 `rbf-interface.md` v0.15（ScanReverse 返回类型重构 + 6 条新条款，[畅谈会决议](../meeting/2025-12-28-scan-reverse-return-type.md)）；更新 `rbf-test-vectors.md` v0.9；识别实现层与新规范的**类型系统层面根本差异**
> - 2025-12-28 更新 #3：反映 `rbf-interface.md` v0.13（移除 FrameStatus 类型，RbfFrame 直接暴露 `bool IsTombstone`）
> - 2025-12-28 更新 #2：反映 FrameStatus 接口已对齐（`rbf-interface.md` v0.12）
> - 2025-12-28 更新 #1：反映 F2 已解决（`rbf-format.md` v0.15、`rbf-test-vectors.md` v0.8）；收束对齐策略为已采取的路线 A
