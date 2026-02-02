# 方案 C（草案）：Append/Builder 写入核心统一

> **目标**：减少写入路径分叉，让 `Append` 与 `BeginAppend/EndAppend` 共享帧尾部构建逻辑与边界校验，避免语义漂移。

---

## 1. 现状与问题

- `RbfAppendImpl.Append` 与 `RbfFrameBuilder.EndAppend` 各自维护一套尾部构建逻辑（CRC/Trailer/Fence）。
- 两条路径使用不同的校验/异常模型，未来演进容易产生格式差异。
- 共享逻辑不足，导致“写入格式一致性”依赖人为记忆。

---

## 2. 目标与非目标

### 2.1 目标
- 抽取“帧尾部构建与校验”逻辑为共享核心
- 统一 `FrameLayout`/`MaxFileOffset` 的计算规则
- 明确两条路径的“差异仅在 I/O 方式”

### 2.2 非目标
- 不改动 `RbfAppendImpl` 的多段写入策略
- 不改变 `SinkReservableWriter` 的缓存模型
- 不引入异步写入或批量写入

---

## 3. 提议结构

### 3.1 新增内部组件

**候选名**：`RbfFrameWriteCore` 或 `RbfFrameTailWriter`

**职责**：
- 根据 `FrameLayout` 写入 `PayloadCrc`、`TrailerCodeword`、`Fence`
- **不负责** Padding 填充（Padding 仍由调用方在 PayloadCrc 之前写入）
- 提供 `ValidateEndOffset(frameStart, frameLength)` 方法
- 提供 `ComputeEndOffset(frameStart, frameLength)` 方法

### 3.2 两条路径的协作方式

- `RbfAppendImpl`：
  - 继续负责“分段写入”策略
  - 仍可复用现有 Padding 逻辑
  - 使用 `RbfFrameWriteCore.WriteTail` 生成尾部字节序列（PayloadCrc + Trailer + Fence）

- `RbfFrameBuilder.EndAppend`：
  - `payloadCrc` 计算仍依赖 `SinkReservableWriter.GetCrcSinceReservationEnd`
  - 使用 `RbfFrameWriteCore.WriteTail` 写入尾部（PayloadCrc + Trailer + Fence）

> 约束：`WriteTail` 期望传入 **已 finalize** 的 PayloadCrc（包含默认 XOR），避免两条路径计算规则分叉。

---

## 4. 修改范围

### 4.1 新增文件

- `/repos/focus/atelia/src/Rbf/Internal/RbfFrameWriteCore.cs`

### 4.2 修改文件

- `/repos/focus/atelia/src/Rbf/Internal/RbfAppendImpl.cs`
- `/repos/focus/atelia/src/Rbf/RbfFrameBuilder.cs`

---

## 5. 核心 API 草案

```csharp
internal static class RbfFrameWriteCore {
    internal static long ComputeEndOffset(long frameStart, int frameLength);
    internal static void ValidateEndOffset(long frameStart, int frameLength);
    internal static void WriteTail(
        Span<byte> tailBuffer,
        in FrameLayout layout,
        uint tag,
        uint payloadCrc,
        bool isTombstone = false);
}
```

说明：
- `tailBuffer` 长度需 ≥ `PayloadCrc(4) + Trailer(16) + Fence(4)`
- `WriteTail` 仅负责尾部构建，不负责写盘
- `WriteTail` 内部复用 `FrameLayout.FillTrailer` 以避免 Trailer 逻辑双写
- 若未来需要统一常量，可新增 `RbfLayout.TailSize = PayloadCrcSize + TrailerCodewordSize + FenceSize`

---

## 6. 验收标准

1. `Append` 与 `EndAppend` 写出的尾部布局完全一致
2. 关键常量（TailLen/TrailerCrc 相关）不再双写
3. 共享核心被单元测试覆盖（对照 payload/tailMeta 边界值）
4. `ValidateEndOffset` 在两条路径中都被调用（与方案 A 的边界修复对齐）

---

## 7. 风险与缓解

| 风险 | 影响 | 缓解 |
|---|---|---|
| 新增抽象增加复杂度 | 中 | 只抽取最小核心（尾部构建） |
| 共享核心引入性能损耗 | 低 | `WriteTail` 为 span 写入，无额外分配 |
| 修改范围扩大 | 中 | 先完成方案 A/B，再进入方案 C |
| Padding 与 Tail 边界不清 | 中 | 明确 `WriteTail` 不负责 Padding，调用方需先填充 Padding |

---

## 8. 审阅关注点

- `payloadCrc` 的计算来源是否清晰，避免双算
- `WriteTail` 与现有 `FillTrailer` 是否职责冲突
- `ValidateEndOffset` 是否应在 `RbfFrameBuilder` 内部调用
- `WriteTail` 期望 CRC 是否已 finalize（需在文档中锁定）
