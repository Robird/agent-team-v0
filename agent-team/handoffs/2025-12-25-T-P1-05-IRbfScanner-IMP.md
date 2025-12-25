# [T-P1-05] IRbfScanner/逆向扫描 Implementation Result

## 实现摘要

完成 RBF Layer 0 的帧扫描器 (`IRbfScanner`/`RbfScanner`) 实现。支持从文件尾部逆向扫描帧、按地址随机读取、Resync 恢复等功能。这是 Phase 1 的最后一个任务。

## 文件变更

| 文件 | 操作 | 描述 |
|------|------|------|
| `atelia/src/Rbf/IRbfScanner.cs` | 新增 | 扫描器接口定义 |
| `atelia/src/Rbf/RbfScanner.cs` | 新增 | 扫描器实现 |
| `atelia/tests/Rbf.Tests/RbfScannerTests.cs` | 新增 | 扫描器测试（37 个测试用例） |

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|----------|------|------|
| `[A-RBF-SCANNER-INTERFACE]` | `IRbfScanner` 接口 | 完整实现 |
| `[R-REVERSE-SCAN-ALGORITHM]` | `ScanReverse()` 方法 | 从尾部向前扫描 |
| `[R-RESYNC-BEHAVIOR]` | 验证失败时 `fencePos -= 4` | 4B 步长向前搜索 |
| `[S-RBF-TOMBSTONE-VISIBLE]` | Scanner 产出 Tombstone 帧 | 测试覆盖 |
| `[F-FRAMING-FAIL-REJECT]` | HeadLen/TailLen/对齐/Fence 校验 | 完整校验链 |
| `[F-CRC-FAIL-REJECT]` | CRC32C 校验 | 使用 `RbfCrc.Verify` |
| `[F-FRAMESTATUS-VALUES]` | 值必须为 0x00 或 0xFF | 完整校验 |
| `[F-FRAMESTATUS-FILL]` | 所有字节必须相同 | 完整校验 |

## 测试结果

```
Targeted: dotnet test tests/Rbf.Tests/ → 133/133 passed
Full: dotnet test → 208 passed (包括 Analyzers.Style.Tests)
```

### 测试覆盖

| 用例 ID | 测试方法 | 状态 |
|---------|----------|------|
| RBF-EMPTY-001 | `ScanReverse_EmptyFile_ReturnsNoFrames` | ✅ |
| RBF-SINGLE-001 | `ScanReverse_SingleFrame_ReturnsOneFrame` | ✅ |
| RBF-DOUBLE-001 | `ScanReverse_TwoFrames_ReturnsInReverseOrder` | ✅ |
| RBF-OK-001 | `ScanReverse_EmptyPayloadValidFrame_Succeeds` | ✅ |
| RBF-OK-002 | `ScanReverse_TombstoneFrame_IsVisible` | ✅ |
| RBF-OK-003 | `ScanReverse_VariousPayloadLengths_CorrectStatusLen` | ✅ |
| RBF-BAD-001 | `ScanReverse_HeadLenMismatch_SkipsFrame` | ✅ |
| RBF-BAD-002 | `ScanReverse_CrcMismatch_SkipsFrame` | ✅ |
| RBF-BAD-005 | `ScanReverse_InvalidFrameStatus_SkipsFrame` | ✅ |
| RBF-BAD-006 | `ScanReverse_InconsistentFrameStatus_SkipsFrame` | ✅ |
| RBF-TRUNCATE-001 | `ScanReverse_TruncatedFile_MissingTrailingFence_ResyncSucceeds` | ✅ |
| RBF-TRUNCATE-002 | `ScanReverse_TruncatedInMiddleOfFrame_ResyncFindsEarlierFrame` | ✅ |

## 已知差异

### 接口设计微调

规范定义 `RbfReverseEnumerable ScanReverse()`，实现为 `IEnumerable<RbfFrame> ScanReverse()`。
原因：`RbfReverseEnumerable` 未定义，使用标准接口满足需求。

### PayloadLen/StatusLen 边界确定

**问题**：Wire format 中 PayloadLen 不直接存储，而 `HeadLen = 16 + PayloadLen + StatusLen`。由于取模运算丢失低 2 位信息，给定 HeadLen 存在 4 个数学上合法的 (PayloadLen, StatusLen) 组合。

**当前实现方案**：枚举 + CRC 消歧
1. 从 StatusLen=4 开始枚举（优先匹配空 Payload 帧）
2. 验证 FrameStatus 值（0x00/0xFF）和字节一致性
3. 依赖 CRC32C 验证确定正确边界

**后续**：已提交技术报告 [2025-12-25-rbf-statuslen-ambiguity.md](../meeting/2025-12-25-rbf-statuslen-ambiguity.md) 待畅谈会讨论格式改进方案。

## 遗留问题

1. **Stream 构造函数**：规范提到 `RbfScanner(Stream stream)` 构造函数，暂未实现（MVP 仅需内存模式）
2. **RbfReverseEnumerable**：规范中的专用枚举类型，用 `IEnumerable<RbfFrame>` 替代
3. **StatusLen 边界确定**：待畅谈会决策格式改进方案（见上述技术报告）

## Changefeed Anchor

`#delta-2025-12-25-rbf-scanner`
