# F-003: 逆向扫描算法遗漏 FrameStatus 校验

**位置**: rbf-format.md#§6.1 `[R-REVERSE-SCAN-ALGORITHM]`

**描述**: 
`[R-REVERSE-SCAN-ALGORITHM]` 的伪代码定义了逆向扫描算法，但校验步骤不完整。

算法检查了：
- HeadLen/TailLen 一致性 (`headLen != tailLen`)
- HeadLen 对齐性 (`headLen % 4 != 0`)
- HeadLen 最小值 (`headLen < 16`，注：应为 20)
- Fence 匹配
- CRC 校验

**但遗漏了**：
- `[F-FRAMESTATUS-VALUES]`：FrameStatus 位域合法性（IsMvpValid = `(status & 0x7C) == 0`）
- `[F-FRAMESTATUS-FILL]`：FrameStatus 所有字节一致性

这两项在 `[F-FRAMING-FAIL-REJECT]` 中明确列为 MUST 验证项。

**风险**: 
实现者按照伪代码实现后，会漏掉 FrameStatus 的校验，导致：
1. 接受了 Reserved bits 非零的非法帧
2. 接受了 FrameStatus 字节不一致的损坏帧
3. 与 `[F-FRAMING-FAIL-REJECT]` 的规范要求不一致

**建议**: 
在 `[R-REVERSE-SCAN-ALGORITHM]` 的伪代码中，在 CRC 校验之前增加 FrameStatus 校验步骤：
```
// 验证 FrameStatus
statusLen = 从 headLen 计算（见 [F-STATUSLEN-FROM-HEADLEN]）
statusBytes = bytes[frameStart + 8 + payloadLen .. frameStart + 8 + payloadLen + statusLen]
若 statusBytes 中任意两字节不等:
      fencePos -= 4
      continue
若 (statusBytes[0] & 0x7C) != 0:  // Reserved bits 非零
      fencePos -= 4
      continue
```
