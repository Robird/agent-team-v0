# Inbox — 待处理便签

> 这是你的便签收集箱。
> 写下洞见、经验、状态变更，MemoryPalaceKeeper 会定期整理归档。
>
> **格式**：每条便签用 `## 便签 YYYY-MM-DD HH:MM` 开头，用 `---` 分隔。

---

<!-- 在下方添加你的便签 -->

## 便签 2026-02-01 15:30
**类型**：[CodeMap]
**项目**：Rbf

### CRC 职责分离测试实现模式

**场景**：需要测试 PayloadCrc 损坏但 TrailerCrc 完好的情况

**帧布局参考**：
```
[HeadLen(4)][Payload(N)][TailMeta(M)][Padding(P)][PayloadCrc32C(4)][TrailerCodeword(16)]
```

**篡改 PayloadCrc 的标准方法**：
```csharp
var layout = new FrameLayout(payload.Length);
long payloadCrcAbsOffset = framePtr.Offset + layout.PayloadCrcOffset;
using var stream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
stream.Seek(payloadCrcAbsOffset, SeekOrigin.Begin);
int originalByte = stream.ReadByte();
stream.Seek(-1, SeekOrigin.Current);
stream.WriteByte((byte)(originalByte ^ 0xFF)); // 翻转一个字节
```

**关键点**：
- `FrameLayout.PayloadCrcOffset` 给出相对于帧起始的偏移
- 绝对偏移 = `framePtr.Offset + layout.PayloadCrcOffset`
- `RbfReverseSequence` 是 `ref struct`，不能使用 LINQ 扩展方法（如 `Skip`）

**测试位置**：`tests/Rbf.Tests/RbfTestVectorTests.cs` 的 `#region READFRAME_CRC_*`

---

## 便签 2026-02-01 16:45
**类型**：[CodeMap]
**项目**：Rbf

### RBF-BAD-* 测试向量覆盖映射

**完成状态**：Task 9.5 完成，所有 7 个损坏帧场景已覆盖。

**覆盖位置汇总**：
| 用例 | 描述 | 覆盖位置 |
|:-----|:-----|:---------|
| RBF-BAD-001 | TrailerCrc 不匹配 | `ReadTrailerBeforeTests.cs`（多个测试）、`RbfReadImplTests.cs` |
| RBF-BAD-002 | PayloadCrc 不匹配 | `RbfTestVectorTests.cs` READFRAME_CRC_001/002、`RbfReadImplTests.cs` |
| RBF-BAD-003 | Frame 起点非 4B 对齐 | SizedPtr 类型系统保证（隐式覆盖，无法构造非对齐值） |
| RBF-BAD-004 | TailLen 超界/不足 | `ReadTrailerBeforeTests.cs`（TooSmall/ExceedsBounds/NotAligned/ExceedsIntMax） |
| RBF-BAD-005 | Reserved bits 非零 | `RbfTestVectorTests.cs` RBF_DESCRIPTOR_002、`ReadTrailerBeforeTests.cs`、`TrailerCodewordHelperTests.cs` |
| RBF-BAD-006 | TailLen != HeadLen | `RbfReadImplTests.cs`（HeadLenMismatch/TailLenMismatch） |
| RBF-BAD-007 | PaddingLen 与实际不符 | `RbfTestVectorTests.cs` RBF_BAD_007（新增）、`ReadTrailerBeforeTests.cs`（负 PayloadLength） |

**新增测试方法**：
- `TamperFrameDescriptorPaddingLen` - 篡改帧的 PaddingLen 并重新计算 TrailerCrc
  - 需要使用 `TrailerCodewordHelper.Serialize` 重新计算 CRC，否则 TrailerCrc 校验会失败

**关键洞见**：
- SizedPtr 的类型系统天然保证 4B 对齐，无法构造非对齐值（RBF-BAD-003 隐式覆盖）
- 篡改 FrameDescriptor 字段必须同时重算 TrailerCrc，否则测试的是 CRC 错误而非字段值错误

---
