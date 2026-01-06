# F-010: alignDown4 公式的截断方向未定义负数行为

**位置**: rbf-format.md#§6.1 `[R-REVERSE-SCAN-ALGORITHM]`

**描述**: 
算法定义了辅助函数：
```
alignDown4(x) = x - (x % 4)
```

并在步骤 2 中使用：
```
fencePos = alignDown4(fileLength - FenceLen)
```

**问题**：当 `fileLength < FenceLen`（即 `fileLength < 4`）时：
- `fileLength - FenceLen` 会是负数
- `x % 4` 在不同语言中对负数的行为不同：
  - C#/Java：`-1 % 4 == -1`（truncate towards zero）
  - Python：`-1 % 4 == 3`（floor division）
- 因此 `alignDown4(-1)` 在 C# 中是 `0`，在 Python 中是 `-4`

虽然步骤 1 已经处理了 `fileLength < GenesisLen` 的情况返回空，但如果实现者重排代码逻辑或遗漏前置检查，负数行为可能导致意外。

**风险**: 
跨语言移植或重构时，`alignDown4` 对负数输入的行为差异可能导致计算出错误的 `fencePos`。

**建议**: 
明确 `alignDown4` 的前置条件或定义域：
```
alignDown4(x) = x - (x % 4)
前提：x >= 0。本算法保证在调用 alignDown4 前 x 非负（步骤 1 过滤）。
```
或使用位运算版本（明确无符号语义）：
```
alignDown4(x) = x & ~3    // 仅适用于无符号整数
```
