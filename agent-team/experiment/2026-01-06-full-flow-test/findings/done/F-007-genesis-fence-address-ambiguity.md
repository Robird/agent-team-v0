# F-007: HeaderFence 后首帧地址是 4 而非 0

**位置**: 
- rbf-format.md#§2.2 `[F-HEADER-FENCE]`
- rbf-interface.md#§2.3 `[F-<deleted-place-holder>-NULL]`

**描述**: 
<deleted-place-holder> 的约束规定：
- `Value == 0` 表示 null（无效地址）
- 有效 <deleted-place-holder> MUST 4 字节对齐

HeaderFence 的约束规定：
- 每个 RBF 文件 MUST 以 Fence 开头（偏移 0，长度 4 字节）
- 新建的 RBF 文件 MUST 仅含 HeaderFence

**隐含推论**（文档未显式说明）：
1. 文件偏移 0 是 HeaderFence，不是 Frame
2. 第一个 Frame 的起始地址是 4（HeaderFence 之后）
3. <deleted-place-holder>(0) 是 null，不会指向任何 Frame（因为偏移 0 是 Fence）

这个推论是正确的，但文档没有明确串联这些约束。

**风险**: 
实现者可能：
1. 误以为 <deleted-place-holder>(0) 指向第一个 Frame
2. 在 `TryReadAt(<deleted-place-holder>.Null)` 时尝试读取偏移 0，得到 Fence 字节而非 Frame
3. 混淆"null 地址"和"第一帧地址"

**建议**: 
在 `[F-<deleted-place-holder>-NULL]` 中增加说明：
```
Value == 0 表示 null（无效地址）。
注：偏移 0 被 HeaderFence 占用，因此 <deleted-place-holder>(0) 不会指向任何有效 Frame。
第一个 Frame 的最小有效地址是 4（紧跟 HeaderFence 之后）。
```
