# F-008: "每个 Frame 之后 MUST 紧跟 Fence" 与 Writer flush 时机的冲突

**位置**: 
- rbf-format.md#§3.1 `[F-FENCE-SEMANTICS]`
- rbf-interface.md#§3.1 `[S-RBF-FRAMER-NO-FSYNC]`

**描述**: 
`[F-FENCE-SEMANTICS]` 规定：
> 每个 Frame 之后 MUST 紧跟一个 Fence。

同时注释说：
> 注：在崩溃/撕裂写入场景，文件尾部 MAY 不以 Fence 结束

这里存在张力：
1. MUST 是强制约束，但又说 MAY 不满足
2. "崩溃/撕裂写入场景"是什么时候？

对于 Writer 实现者：
- `Commit()` 返回时，Frame 和后续 Fence 都应该写完吗？
- 还是可以延迟写 Fence 到下一次 `BeginFrame()` 或 `Flush()`？

对于 Reader 实现者：
- 扫描到末尾没有 Fence 时，最后一个 Frame 是有效的吗？

**风险**: 
1. Writer 实现者可能延迟写 Fence，导致正常关闭的文件也缺少尾部 Fence
2. Reader 实现者可能过于严格，拒绝没有尾部 Fence 的有效文件
3. "MUST"和"MAY 不满足"的措辞冲突让实现者困惑

**建议**: 
明确约束的适用范围：
```
[F-FENCE-AFTER-FRAME]
Writer 在完成 Frame 写入时 MUST 同时写入后续 Fence（作为原子单位）。
如果写入过程中发生崩溃，文件尾部 MAY 缺少 Fence（这是损坏状态）。

[F-READER-INCOMPLETE-FRAME]
Reader 遇到文件尾部缺少 Fence 的情况时，MUST 将该不完整帧视为损坏，不作为有效数据返回。
```
