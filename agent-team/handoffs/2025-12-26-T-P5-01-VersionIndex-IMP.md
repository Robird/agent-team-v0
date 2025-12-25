# [T-P5-01] VersionIndex Implementation Result

## 实现摘要

VersionIndex 是 StateJournal 的"引导扇区"，使用 Well-Known ObjectId `0`。复用 `DurableDict<ulong?>` 实现，支持 ObjectId → ObjectVersionPtr 的映射。为支持 `ulong` 值类型的序列化，扩展了 `DurableDict.WriteValue` 方法以使用 `WritePtr64`。

## 文件变更

| 文件 | 操作 | 描述 |
|------|------|------|
| [src/StateJournal/Commit/VersionIndex.cs](../../atelia/src/StateJournal/Commit/VersionIndex.cs) | 新增 | VersionIndex 实现类 |
| [tests/StateJournal.Tests/Commit/VersionIndexTests.cs](../../atelia/tests/StateJournal.Tests/Commit/VersionIndexTests.cs) | 新增 | 27 个测试用例 |
| [src/StateJournal/Objects/DurableDict.cs](../../atelia/src/StateJournal/Objects/DurableDict.cs) | 修改 | `WriteValue` 方法增加 `ulong` 类型支持 |

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|---------|---------|------|
| `[F-VERSIONINDEX-REUSE-DURABLEDICT]` | `DurableDict<ulong?>` 委托 | 完全对齐 |
| Well-Known ObjectId = 0 | `WellKnownObjectId` 常量 | 完全对齐 |
| value 使用 `Val_Ptr64` 编码 | `WritePtr64` | 完全对齐 |
| ComputeNextObjectId 保护保留区 | 返回 max(16, maxKey+1) | 完全对齐 |

## 测试结果

- **Targeted**: `dotnet test --filter "VersionIndexTests"` → 27/27 ✅
- **Full**: `dotnet test StateJournal.Tests` → 520/520 ✅

## 已知差异

无。实现完全对齐规范。

## 遗留问题

无。

## 实现细节

### VersionIndex 类结构

```csharp
public sealed class VersionIndex : IDurableObject
{
    public const ulong WellKnownObjectId = 0;
    private readonly DurableDict<ulong?> _inner;
    
    // 特有 API
    public bool TryGetObjectVersionPtr(ulong objectId, out ulong versionPtr);
    public void SetObjectVersionPtr(ulong objectId, ulong versionPtr);
    public IEnumerable<ulong> ObjectIds { get; }
    public int Count { get; }
    public ulong ComputeNextObjectId();
    
    // IDurableObject 实现（委托给 _inner）
    public ulong ObjectId => WellKnownObjectId;
    public DurableObjectState State => _inner.State;
    public bool HasChanges => _inner.HasChanges;
    // ... 其他方法
}
```

### DurableDict.WriteValue 扩展

新增 `ulong` 类型支持：

```csharp
case ulong ulongVal:
    // [F-VERSIONINDEX-REUSE-DURABLEDICT]: VersionIndex 使用 Val_Ptr64 编码 ObjectVersionPtr
    writer.WritePtr64(key, ulongVal);
    break;
```

## Changefeed Anchor

`#delta-2025-12-26-versionindex`
