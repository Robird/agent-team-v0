已手动实施的主要重构：
- Task里的“`atelia/src/Rbf/Internal/RbfFileImpl.cs`，实现 `Append` 方法”演变成了[RbfAppendImpl.Append](atelia/src/Rbf/Internal/RbfAppendImpl.cs)。为的是比较优雅的处理长payload，优化了copy量和RandomAccess.Write的调用次数。还引入了`atelia/benchmarks/Rbf.Benchmarks/Rbf.Benchmarks.csproj`
- Task里的`atelia/tests/Rbf.Tests/RbfAppendTests.cs`演变成了2部分：`atelia/tests/Rbf.Tests/RbfFacadeTests.cs`测试门面`RbfFile`和`IRbfFile` + `atelia/tests/Rbf.Tests/RbfAppendImplTests.cs`测试内部具体实现。
- 撰写了`wish/W-0009-rbf/testing-pattern.md`文档，记录后续的测试撰写模式。
- Task里的“在 `RbfRawOps` 中添加帧序列化辅助方法，用于计算 HeadLen 和序列化完整帧”演变成了[RbfConstants.ComputeFrameLen](atelia/src/Rbf/Internal/RbfConstants.cs)。更内聚一些，也方便独立的各种Impl类型复用。
- Task里的“公式：`HeadLen = 4 + 4 + payloadLen + statusLen + 4 + 4`”等魔数字面量，演变成了[`RbfConstants.FenceLength/HeadLenFieldLength`](RbfConstants.cs)等常量。
- Task里的`Crc32CHelper.Compute`中的指针改成了更安全的`System.Runtime.CompilerServices.Unsafe`实现。
