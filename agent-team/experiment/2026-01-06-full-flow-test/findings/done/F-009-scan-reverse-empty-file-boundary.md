# F-009: 空文件与仅有 HeaderFence 文件的处理边界

**位置**: 
- rbf-format.md#§6.1 `[R-REVERSE-SCAN-ALGORITHM]`
- rbf-interface.md#§4.1 `[S-RBF-SCANREVERSE-EMPTY-IS-OK]`

**描述**: 
`[S-RBF-SCANREVERSE-EMPTY-IS-OK]` 规定：
> 当扫描范围为空（空文件、仅 HeaderFence、或无有效帧）时，`ScanReverse()` 返回序列 MUST 产生 0 个元素且不得抛出异常。

但 `[R-REVERSE-SCAN-ALGORITHM]` 的伪代码只处理了 `fileLength < HeaderFenceLen` 的情况：
```
1) 若 fileLength < HeaderFenceLen: 返回空
```

**遗漏的边界情况**：
1. `fileLength == 4`（仅 HeaderFence）：按算法，`fencePos = alignDown4(4 - 4) = 0`，然后在步骤 3a 停止，返回空。这是正确的。

2. `fileLength == 0`（空文件）：这是否是合法的 RBF 文件？按 `[F-HEADER-FENCE]` 规定"新建的 RBF 文件 MUST 仅含 HeaderFence"，空文件不合法。但 `[S-RBF-SCANREVERSE-EMPTY-IS-OK]` 说"空文件"返回 0 元素不抛异常。

3. `fileLength` 在 `(0, 4)` 区间：既不是空文件也没有完整 HeaderFence，是损坏状态。

**风险**: 
实现者可能：
1. 对"空文件"是否合法产生困惑
2. 不清楚 `fileLength < 4` 时应返回空序列还是抛异常

**建议**: 
在 `[R-REVERSE-SCAN-ALGORITHM]` 或新增条款中明确：
```
[F-FILE-MINIMUM-LENGTH]
有效 RBF 文件长度 MUST >= 4（至少包含 HeaderFence）。

[R-SCAN-INVALID-FILE]
ScanReverse 遇到 fileLength < 4 时，SHOULD 返回空序列（fail-soft），
但实现 MAY 选择抛出异常或返回特定错误状态。
```
