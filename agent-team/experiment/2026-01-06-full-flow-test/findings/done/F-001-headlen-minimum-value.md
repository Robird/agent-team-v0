# F-001: HeadLen 最小值歧义

**位置**: rbf-format.md#§3.3 `[F-HEADLEN-FORMULA]`

**描述**: 
文档在 `[F-HEADLEN-FORMULA]` 中给出公式：
```
HeadLen = 16 + PayloadLen + StatusLen
```
并注释"当 PayloadLen = 0 时，StatusLen = 4，故最小 HeadLen = 20"。

然而在 §6.1 `[R-REVERSE-SCAN-ALGORITHM]` 的伪代码中，使用了"最小 FrameBytes = 20"的注释：
```
若 recordEnd < HeaderFenceLen + 20:  // 最小 FrameBytes = 20（PayloadLen=0, StatusLen=4）
```

问题是：**Reader 实现需要验证 `headLen >= 16` 还是 `headLen >= 20`？**

- 按公式推导：HeadLen = 16 + PayloadLen + StatusLen，当 PayloadLen=0, StatusLen=4 时，HeadLen=20
- 但公式本身没有显式约束 "HeadLen MUST >= 20"
- `[F-FRAMING-FAIL-REJECT]` 列出的验证项不包含 HeadLen 最小值检查

**风险**: 
实现者可能：
1. 只验证 `headLen >= 16`（公式常数部分），漏掉 PayloadLen=0 时 StatusLen 必须为 4 的隐含约束
2. 不清楚应该在 Framing 阶段还是算法阶段做这个检查

**建议**: 
在 `[F-HEADLEN-FORMULA]` 中显式添加约束条款：
```
HeadLen MUST >= 20（因最小 StatusLen = 1，最小 PayloadLen = 0，但当 PayloadLen=0 时 StatusLen=4）
```
或在 `[F-FRAMING-FAIL-REJECT]` 验证清单中增加 `[F-HEADLEN-MINIMUM]` 条款。
