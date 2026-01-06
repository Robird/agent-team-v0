# F-002: 从 HeadLen 反推 PayloadLen/StatusLen 的算法缺失

**位置**: rbf-format.md#§3.2 `[F-FRAMESTATUS-VALUES]` 与 §3.3 `[F-STATUSLEN-FORMULA]`

**描述**: 
Reader 在解析 Frame 时需要从 HeadLen 反推出 PayloadLen 和 StatusLen 的边界。

文档给出了两种推导路径，但没有明确说明 Reader 应该使用哪种：

1. **路径 A**（先读 FrameStatus 判定 StatusLen）：
   - 从 `[F-FRAMESTATUS-VALUES]` 可知：`StatusLen = (status & 0x03) + 1`
   - 但 Reader 在不知道 StatusLen 的情况下，**如何定位 FrameStatus 的位置**来读取它？

2. **路径 B**（用 HeadLen 推导 StatusLen）：
   - 从 `[F-STATUSLEN-FORMULA]` 可以推导：`StatusLen = 1 + ((4 - ((PayloadLen + 1) % 4)) % 4)`
   - 但这需要先知道 PayloadLen，形成循环依赖

实际上 Reader 需要的算法是：
```
StatusLen = 4 - ((HeadLen - 16 - 1) % 4)   // 或等价公式
PayloadLen = HeadLen - 16 - StatusLen
```
但文档没有显式给出这个反向公式。

**风险**: 
实现者可能：
1. 误以为需要先猜测位置读取 FrameStatus 字节来得到 StatusLen
2. 自行推导公式时出错（如忘记 -1 偏移或符号错误）
3. 在 StatusLen 编码于 FrameStatus 本身的设计下，困惑于"先有鸡还是先有蛋"

**建议**: 
在 §3.3 新增 `[F-STATUSLEN-FROM-HEADLEN]` 条款，显式给出 Reader 需要的反向公式：
```
对于有效帧：StatusLen = ((3 - ((HeadLen - 17) % 4)) % 4) + 1
          PayloadLen = HeadLen - 16 - StatusLen
```
或给出等价的算法伪代码。
