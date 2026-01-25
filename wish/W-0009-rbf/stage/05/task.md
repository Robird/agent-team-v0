# Stage 05: ReadFrame 重构与 Buffer 外置

> **目标**：重构 `RbfRawOps.ReadFrame`，实现 Buffer 外置模式，为后续 `ScanReverse` 解耦做准备。
> **前置**：Stage 04 完成（现有 ReadFrame 实现可作为参考）

---

## 设计决策

### Decision 5.A: 移除旧 ReadFrame 签名

**结论**：直接替换 `ReadFrame(file, ptr)` 为 `ReadFrameInto(file, ptr, buffer)`，不保留兼容层。

**理由**：
1. 当前无外部依赖，没有兼容性包袱
2. 轻装上阵便于彻底重构
3. 避免维护两套 API 的复杂性

### Decision 5.B: CRC 校验策略简化

**结论**：不引入 `CrcCheckPolicy` 参数。

- `ReadFrameInto` **始终校验** CRC（随机读取场景，CRC 是唯一完整性保证）
- `ScanReverse` **始终不校验** CRC（结构迭代场景，framing 校验已足够，CRC 留给 ReadFrameInto）

**理由**：
1. 简化 API，减少用户决策负担
2. 职责明确：ScanReverse 管结构，ReadFrameInto 管内容
3. 避免过早优化（如需 CRC 策略可后续添加）

### Decision 5.C: 内部分层架构

**结论**：将 ReadFrame 拆分为三层：

```
公开 API 层：ReadFrameInto(file, ptr, buffer) → AteliaResult<RbfFrame>
            ↓
内部协调层：调用 ReadRaw + ValidateAndParse
            ↓
原子操作层：ReadRaw(file, ptr, buffer) → int
            ValidateAndParse(ptr, frameBytes) → AteliaResult<RbfFrame>
            ValidateAndParseHeader(ptr, headerBytes) → AteliaResult<RbfFrameHeader>
```

**理由**：
1. `ValidateAndParse` 可被 ScanReverse 复用
2. `ValidateAndParseHeader` 为 ScanReverse 轻量迭代提供支持
3. 职责单一，便于单元测试

---

## 新增类型定义

### RbfFrameHeader（待后续细化）

> **备注**：在后续 Stage（ScanReverse）实现时进一步设计。可能命名为 `FrameInfo`，绑定 `RbfFileImpl` 实例，支持惰性读取。

当前 Stage 只需实现 `ValidateAndParseHeader` 的内部版本，返回 `(SizedPtr, Tag, IsTombstone)` 元组或简单结构。

### RbfBufferTooSmallError

```csharp
internal sealed record RbfBufferTooSmallError(
    string Message,
    int RequiredBytes,
    int ProvidedBytes,
    string? RecoveryHint = null
) : AteliaError("Rbf.BufferTooSmall", Message, RecoveryHint);
```

---

## 实现任务

### Task 5.1: 实现 ValidateAndParse（含错误类型定义）

**执行者**：Implementer
**依赖**：无

**任务简报**：
1. 在 `RbfErrors.cs` 中新增 `RbfBufferTooSmallError`
2. 从现有 `ReadFrame.cs` 提取校验逻辑为独立的 `ValidateAndParse` 方法

**产出**：
1. 修改 `atelia/src/Rbf/Internal/RbfErrors.cs`，添加：
   ```csharp
   internal sealed record RbfBufferTooSmallError(
       string Message,
       int RequiredBytes,
       int ProvidedBytes,
       string? RecoveryHint = null
   ) : AteliaError("Rbf.BufferTooSmall", Message, RecoveryHint);
   ```

2. 创建 `atelia/src/Rbf/Internal/RbfRawOps.ValidateAndParse.cs`，实现：
   ```csharp
   /// <summary>校验并解析已读入 buffer 的帧数据。</summary>
   /// <param name="ptr">帧位置凭据（用于构造 RbfFrame.Ptr）。</param>
   /// <param name="frameBytes">已读入的帧字节，长度 MUST >= ptr.LengthBytes（只使用前 LengthBytes 字节）。</param>
   /// <returns>校验通过则返回 RbfFrame（Payload 为 frameBytes 的切片），否则返回错误。</returns>
   internal static AteliaResult<RbfFrame> ValidateAndParse(SizedPtr ptr, ReadOnlySpan<byte> frameBytes);
   ```

**校验清单**（从现有实现提取）：
- [ ] frameBytes.Length >= ptr.LengthBytes（只使用前 LengthBytes 字节）
- [ ] HeadLen == ptr.LengthBytes
- [ ] StatusByte 解码成功（保留位为零）
- [ ] PayloadLen/StatusLen 一致性
- [ ] Status 全字节同值
- [ ] TailLen == HeadLen
- [ ] CRC 校验通过

**验收标准**：
- [ ] 编译通过
- [ ] 单元测试覆盖正常路径和各类错误路径
- [ ] `RbfBufferTooSmallError` 包含 `RequiredBytes` 和 `ProvidedBytes` 字段

---

### Task 5.2: 实现 ReadRaw

**执行者**：Implementer
**依赖**：无

**任务简报**：
实现纯 I/O 的 `ReadRaw` 方法，只负责从文件读取数据到 buffer。

**产出**：
1. 创建 `atelia/src/Rbf/Internal/RbfRawOps.ReadRaw.cs`
2. 实现：
   ```csharp
   /// <summary>原始读取：仅执行 I/O，不校验。</summary>
   /// <param name="file">文件句柄。</param>
   /// <param name="ptr">帧位置凭据。</param>
   /// <param name="buffer">目标 buffer，长度 MUST >= ptr.LengthBytes。</param>
   /// <returns>实际读取的字节数。</returns>
   /// <remarks>
   /// <para>短读时返回实际读取字节数（由 ReadFrameInto 检测并报错）。</para>
   /// <para>I/O 异常传播给调用方（不在此层处理）。</para>
   /// </remarks>
   internal static int ReadRaw(SafeFileHandle file, SizedPtr ptr, Span<byte> buffer);
   ```

**验收标准**：
- [ ] 编译通过
- [ ] 单元测试覆盖正常读取和短读场景
- [ ] 短读时返回实际字节数（不抛异常）
- [ ] I/O 异常传播给调用方

---

### Task 5.3: 实现 ReadFrameInto 并更新测试

**执行者**：Implementer
**依赖**：Task 5.1, Task 5.2

**任务简报**：
实现新的 `ReadFrameInto` API，替换现有 `ReadFrame`，并同步更新测试。

**产出**：
1. 修改 `atelia/src/Rbf/Internal/RbfRawOps.ReadFrame.cs`：
   - 移除旧的 `ReadFrame(SafeFileHandle file, SizedPtr ptr)` 实现
   - 添加新的 `ReadFrameInto`：
   ```csharp
   /// <summary>将帧读入调用方提供的 buffer，返回解析后的 RbfFrame。</summary>
   /// <param name="file">文件句柄。</param>
   /// <param name="ptr">帧位置凭据。</param>
   /// <param name="buffer">调用方提供的 buffer，长度 MUST >= ptr.LengthBytes。</param>
   /// <returns>成功时返回 RbfFrame（Payload 指向 buffer 子区间），失败时返回错误。</returns>
   /// <remarks>
   /// <para><b>生命周期警告</b>：返回的 RbfFrame.Payload 直接引用 buffer，
   /// 调用方 MUST 确保 buffer 在使用 Payload 期间有效。</para>
   /// </remarks>
   public static AteliaResult<RbfFrame> ReadFrameInto(
       SafeFileHandle file, 
       SizedPtr ptr, 
       Span<byte> buffer);
   ```

2. 更新 `tests/Rbf.Tests/RbfReadFrameTests.cs`：
   - 所有测试改为调用方提供 buffer（stackalloc 或 new byte[]）
   - 保留 `CreateValidFrameBytes` / `CreateValidFileWithFrame` 辅助方法
   - 新增 Buffer 相关测试

**实现逻辑**：
1. 参数校验（对齐、范围、可表示性）
2. Buffer 长度校验（buffer.Length < ptr.LengthBytes → RbfBufferTooSmallError）
3. 调用 `ReadRaw`
4. 检查短读（bytesRead < ptr.LengthBytes → RbfArgumentError）
5. 调用 `ValidateAndParse`
6. 返回结果

**验收标准**：
- [ ] 编译通过
- [ ] 所有现有测试适配后全部通过
- [ ] 新增 `ReadFrameInto_BufferTooSmall_ReturnsBufferError` 测试
- [ ] 新增 `ReadFrameInto_ValidBuffer_PayloadIsSlice` 测试（验证 zero-copy）
- [ ] `buffer.Length < ptr.LengthBytes` 返回 `RbfBufferTooSmallError`（包含 RequiredBytes 和 ProvidedBytes）

---

## 规范引用

| 条款 | 文档 | 要点 |
|------|------|------|
| @[A-RBF-FRAME-STRUCT] | rbf-interface.md | RbfFrame 结构定义 |
| @[F-FRAMEBYTES-LAYOUT] | rbf-format.md | FrameBytes 布局 |
| @[F-CRC32C-COVERAGE] | rbf-format.md | CRC 覆盖范围 |

---

## 相关文档

- 畅谈记录：[2026-01-15-readframe-buffer-lifetime.md](../04/2026-01-15-readframe-buffer-lifetime.md)
- 畅谈记录：[2026-01-15-readframe-refactor-design.md](./2026-01-15-readframe-refactor-design.md)

---
