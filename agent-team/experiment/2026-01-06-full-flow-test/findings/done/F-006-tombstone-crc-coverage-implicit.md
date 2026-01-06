# F-006: Tombstone 帧的 CRC 保护范围隐含但不显眼

**位置**: rbf-format.md#§4.1 `[F-CRC32C-COVERAGE]`

**描述**: 
文档在 `[F-CRC32C-COVERAGE]` 末尾有一行注释：
> 注：FrameStatus 在 CRC 覆盖范围内，Tombstone 标记受 CRC 保护。

这是一个重要的安全属性，但仅作为"注"出现，而非正式条款。

同时，`[S-RBF-BUILDER-AUTO-ABORT]` 在 rbf-interface.md 中说明 Auto-Abort 需要写入 Tombstone 帧，且：
> Abort 产生的帧 MUST 通过 framing/CRC 校验

这意味着 Writer 实现必须：
1. 将 Tombstone 位写入 FrameStatus
2. 计算 CRC 时包含这个 Tombstone 状态

但两份文档没有交叉引用，实现者可能在 Writer 实现中：
- 先计算 CRC
- 再设置 Tombstone 标志
- 导致 CRC 不匹配

**风险**: 
Writer 实现者可能在写入 Tombstone 帧时，错误地在 CRC 计算之后才设置 Tombstone 标志，导致产出的 Tombstone 帧 CRC 校验失败。

**建议**: 
在 `[F-CRC32C-COVERAGE]` 中将注释提升为正式约束：
```
FrameStatus（包含 Tombstone 标记）在 CRC 覆盖范围内。
Writer 在计算 CRC 之前 MUST 先确定最终的 FrameStatus 值。
```
