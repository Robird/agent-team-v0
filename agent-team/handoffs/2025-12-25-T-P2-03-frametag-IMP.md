# T-P2-03 FrameTag 位段编码 Implementation Result

## 实现摘要

实现 StateJournal 层对 RBF `FrameTag` 的位段解释器，包括 `RecordType` 和 `ObjectKind` 枚举、预定义常量、位段提取扩展方法以及验证/解析逻辑。

## 文件变更

| 文件 | 变更类型 | 说明 |
|------|----------|------|
| `atelia/src/StateJournal/Core/StateJournalFrameTag.cs` | 新增 | 枚举定义 + 静态解释器类 |
| `atelia/tests/StateJournal.Tests/Core/StateJournalFrameTagTests.cs` | 新增 | 47 个测试用例 |

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|---------|------|------|
| `[F-FRAMETAG-STATEJOURNAL-BITLAYOUT]` | 位段提取方法 | `GetRecordType()` = 低 16 位，`GetSubType()` = 高 16 位 |
| `[F-FRAMETAG-SUBTYPE-ZERO-WHEN-NOT-OBJVER]` | `TryParse()` 验证 | 非 ObjectVersion 时 SubType 必须为 0 |
| 端序（LE） | `Create()` 计算 | `(SubType << 16) \| RecordType` |
| MVP 取值表 | 预定义常量 | `DictVersion = 0x00010001`, `MetaCommit = 0x00000002` |

## API 清单

```csharp
// 枚举
public enum RecordType : ushort { Reserved, ObjectVersion, MetaCommit }
public enum ObjectKind : ushort { Reserved, Dict }

// 静态类
public static class StateJournalFrameTag
{
    // 预定义常量
    public static readonly FrameTag DictVersion;   // 0x00010001
    public static readonly FrameTag MetaCommit;    // 0x00000002
    
    // 位段提取（扩展方法）
    public static RecordType GetRecordType(this FrameTag tag);
    public static ushort GetSubType(this FrameTag tag);
    public static ObjectKind GetObjectKind(this FrameTag tag);
    
    // 构造
    public static FrameTag Create(RecordType recordType, ushort subType = 0);
    public static FrameTag CreateObjectVersion(ObjectKind kind);
    
    // 验证解析
    public static AteliaResult<(RecordType, ObjectKind?)> TryParse(FrameTag tag);
}
```

## 测试结果

- **Targeted**: `dotnet test --filter StateJournalFrameTagTests` → 47/47 ✅
- **Full**: `dotnet test StateJournal.Tests.csproj` → 196/196 ✅

## 验收标准检查

| 验收条件 | 状态 |
|---------|------|
| `StateJournalFrameTag.DictVersion.Value == 0x00010001` | ✅ |
| `StateJournalFrameTag.MetaCommit.Value == 0x00000002` | ✅ |
| `GetRecordType(new FrameTag(0x00010001)) == RecordType.ObjectVersion` | ✅ |
| `GetSubType(new FrameTag(0x00010001)) == 0x0001` | ✅ |
| `GetObjectKind(new FrameTag(0x00010001)) == ObjectKind.Dict` | ✅ |
| `TryParse(new FrameTag(0x00000000))` 返回 Failure（Reserved） | ✅ |
| 所有 FRAMETAG-OK-* 测试通过 | ✅ |

## 已知差异

无。完全按规范实现。

## 遗留问题

无。

## Changefeed Anchor

`#delta-2025-12-25-frametag-bitlayout`
