# F-005: Address64 与 Ptr64 命名不一致

**位置**: 
- rbf-interface.md#§2.3 `[F-ADDRESS64-DEFINITION]`
- rbf-format.md#§7 `[F-PTR64-WIRE-FORMAT]`

**描述**: 
两份文档使用了不同的命名：

- **rbf-interface.md** 定义类型为 `Address64`：
  ```csharp
  public readonly record struct Address64(ulong Value)
  ```

- **rbf-format.md** 在 §7 标题使用 "Address64 / Ptr64"，条款 ID 是 `[F-PTR64-WIRE-FORMAT]`

文档中说"本规范所称'地址（Address64/Ptr64）'"，暗示两者等价，但：
1. 没有明确声明 Address64 == Ptr64
2. 条款 ID 是 `Ptr64` 而非 `Address64`
3. 接口文档只用 `Address64`，格式文档混用两个名称

**风险**: 
实现者可能：
1. 误以为 Address64 和 Ptr64 是两种不同的类型
2. 在代码中创建两个不同的类型
3. 在实现 wire format 时查找 `Ptr64` 相关条款，却发现接口层没有这个类型

**建议**: 
统一命名。建议：
1. 将 `[F-PTR64-WIRE-FORMAT]` 改为 `[F-ADDRESS64-WIRE-FORMAT]`
2. 或在 rbf-format.md §7 开头明确声明："本文档中 Ptr64 与 rbf-interface.md 中的 Address64 是同一概念的不同层级名称"
