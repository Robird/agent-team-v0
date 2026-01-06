# F-004: CRC 覆盖范围的字节边界不精确

**位置**: rbf-format.md#§4.1 `[F-CRC32C-COVERAGE]` 与 §6.1 伪代码

**描述**: 
文档用两种方式描述 CRC 覆盖范围：

1. **语义描述**（§4.1）：
   ```
   CRC32C = crc32c(FrameTag + Payload + FrameStatus + TailLen)
   ```

2. **伪代码**（§6.1）：
   ```
   // CRC 覆盖范围是 [frameStart+4, recordEnd-4)（含 FrameTag）
   computedCrc = crc32c(bytes[frameStart+4 .. recordEnd-4])
   ```

两者在语义上等价，但存在以下问题：

**问题 A**：伪代码使用半开区间 `[frameStart+4, recordEnd-4)` 表示法，但没有明确说明是"含左不含右"。不同语言对 slice 的约定不同。

**问题 B**：`recordEnd` 的定义为 `fencePos`（即帧后 Fence 的位置），而 `recordEnd - 4` 指向 CRC32C 字段的起始位置。所以实际覆盖的是 `[frameStart+4, frameStart+headLen-4)`，即 `headLen - 8` 字节。

**问题 C**：语义描述中"FrameTag + Payload + FrameStatus + TailLen"是否包含 TailLen？从公式看是包含的，但这与"CRC MUST NOT 覆盖 CRC32C 本身"的表述形成对照时容易混淆。

**风险**: 
实现者可能：
1. 因区间开闭约定不同导致多算或少算一个字节
2. 误算 CRC 覆盖的字节数（应为 `headLen - 8` 字节）

**建议**: 
在 `[F-CRC32C-COVERAGE]` 中增加显式字节边界：
```
CRC 覆盖范围：从 Frame 起点偏移 4 开始，长度为 (HeadLen - 8) 字节
即字节范围 [frameStart+4, frameStart+HeadLen-4)（含左不含右）
```
