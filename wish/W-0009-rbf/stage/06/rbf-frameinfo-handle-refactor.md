# RbfFrameInfo 句柄化重构方案（readonly struct）

## 背景与目标
当前读取流程中存在重复校验与重复读取逻辑：
- TailMeta 相关偏移/短读判断在多个方法中重复。
- ValidateTailMetaInfo 与 ReadFrameInfo 的校验链路重复。
- 以 SafeFileHandle + RbfFrameInfo 作为参数的函数存在大量薄封装逻辑。

目标是把 `RbfFrameInfo` 升级为“已验证 + 绑定 SafeFileHandle”的只读句柄对象，并将读取操作迁移为成员方法，从根源减少重复校验与重复实现。

## 关键结论
- 选择 `readonly struct` 作为 `RbfFrameInfo` 类型。
- `RbfFrameInfo` 在构造时完成完整验证（Trailer CRC、reserved bits、TailLen 一致性、TailMetaLength 取值等）。
- 读取方法从静态工具函数迁移为 `RbfFrameInfo` 成员方法，避免重复验证。
- `SafeFileHandle` 仅为非拥有引用，生命周期由调用方管理。

## 不变量（Invariants）
`RbfFrameInfo` 一旦构造完成，以下条件视为已验证且不可变：
1. Ticket.Length >= FrameLayout.MinFrameLength
2. TrailerCrc32C 通过
3. Trailer.FrameDescriptor reserved bits 合法
4. Trailer.TailLen == Ticket.Length
5. PayloadLength >= 0
6. TailMetaLength 在 [0, FrameLayout.MaxTailMetaLength] 范围内，且不超出 Ticket 容量

## 结构调整
### 1) `RbfFrameInfo` 成为句柄
- 新增字段：SafeFileHandle File
- 所有读取函数改为实例方法：
  - ReadTailMeta(buffer)
  - ReadPooledTailMeta()
  - ReadFrame(buffer?) / ReadPooledFrame()（如已有对应能力）
- 保持 `readonly struct` + `in` 传参使用习惯

### 2) 构造路径必须唯一
- `RbfFrameInfo` 构造函数/工厂为 `internal`
- `ReadFrameInfo` 或 `ScanReverse` 成为唯一入口，负责完成完整验证与构造
- 保证外部无法绕过验证创建 `RbfFrameInfo`

### 3) 读方法只做 I/O 级校验
实例方法仅做：
- buffer 长度检查
- short read 检查
- 不再重复 Trailer / TailLen / reserved bits 校验

## 代码迁移策略
1. 定位 `RbfFrameInfo` 定义文件，改为 `readonly struct` 并新增 `SafeFileHandle File` 字段。
2. 调整 `ReadFrameInfo`（或 `ScanReverse`）创建 `RbfFrameInfo` 的路径，确保验证完成后注入 File。
3. 将 `RbfReadImpl.ReadTailMeta` / `ReadPooledTailMeta` 迁移到 `RbfFrameInfo` 成员方法：
   - 使用 `this.File` 读取
   - 删除重复 ValidateTailMetaInfo 调用
4. 其它以 (SafeFileHandle, RbfFrameInfo) 作为参数的读取函数改为实例方法。
5. 删除或弱化旧的静态薄封装（保留也可以但需要标记 obsolete/内部使用）。

## 风险与约束
- 句柄生命周期：`RbfFrameInfo` 不拥有 SafeFileHandle，调用方必须确保 File 在使用期间有效。
- `readonly struct` 复制成本：鼓励 `in RbfFrameInfo` 传参。
- 现有方法签名变化需同步更新调用方与测试。

## 验收标准
- `RbfReadImpl.ReadTailMeta.cs` 中重复校验逻辑明显减少。
- `RbfFrameInfo` 成为读操作唯一载体，调用方不再传入 SafeFileHandle。
- 读操作只执行 I/O 级校验，不再重复 Trailer/TailLen/descriptor 校验。
- 现有测试可通过（或相应调整测试以满足新 API）。
