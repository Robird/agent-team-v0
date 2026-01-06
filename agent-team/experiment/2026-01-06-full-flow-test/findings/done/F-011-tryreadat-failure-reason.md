# F-011: TryReadAt 失败时无法区分原因

**位置**: rbf-interface.md#§4.1 `[A-RBF-SCANNER-INTERFACE]`

**描述**: 
`TryReadAt` 的签名是：
```csharp
bool TryReadAt(<deleted-place-holder> address, out RbfFrame frame);
```

返回 `false` 时，调用者无法区分：
1. 地址超出文件范围（out of bounds）
2. 帧 Framing 校验失败（损坏的 HeadLen/TailLen）
3. 帧 CRC 校验失败（数据损坏）
4. 地址不是 4 字节对齐（无效地址）
5. 地址指向 Fence 而非 Frame

文档在 §9"待实现时确认"中提到：
> **错误处理**：TryReadAt 失败时是否需要 `RbfReadStatus`（P2）

但这是"待确认"状态，实现者不知道当前应该如何处理。

**风险**: 
1. 实现者可能随意决定是否提供详细错误信息，导致不同实现行为不一致
2. 上层调用者无法做差异化处理（如：损坏需要 resync，越界是正常终止条件）
3. 调试困难——不知道读取为什么失败

**建议**: 
要么：
1. 现在就确定需要 `RbfReadStatus` 并定义其值枚举
2. 或者明确声明 MVP 阶段 `TryReadAt` 失败原因不需要区分，统一返回 false

建议至少区分：
```csharp
enum RbfReadStatus { 
    Success, 
    OutOfBounds,     // 地址超出文件范围
    FramingFailed,   // HeadLen/TailLen/FrameStatus 校验失败
    CrcMismatch,     // CRC 校验失败
    InvalidAddress   // 地址非法（非4B对齐、指向Fence等）
}
```
