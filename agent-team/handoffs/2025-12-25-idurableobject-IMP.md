# T-P2-05 IDurableObject 接口 Implementation Result

## 实现摘要

为 StateJournal 实现了 `IDurableObject` 接口定义，包含对象生命周期管理的核心契约：State/HasChanges 属性和二阶段提交 API。同时提供了 `FakeDurableObject` test double 用于验证接口契约。

## 文件变更

| 文件 | 变更类型 | 描述 |
|------|----------|------|
| `src/StateJournal/Core/IDurableObject.cs` | ✨ 新增 | 接口定义，包含 ObjectId、State、HasChanges 属性及 WritePendingDiff/OnCommitSucceeded/DiscardChanges 方法 |
| `tests/StateJournal.Tests/Core/IDurableObjectTests.cs` | ✨ 新增 | 27 个测试用例 + FakeDurableObject test double |

## 接口定义摘要

```csharp
public interface IDurableObject
{
    // === Properties ===
    ulong ObjectId { get; }
    DurableObjectState State { get; }  // [A-OBJECT-STATE-PROPERTY] O(1), never throws
    bool HasChanges { get; }           // [A-HASCHANGES-O1-COMPLEXITY] O(1)
    
    // === Two-Phase Commit API ===
    void WritePendingDiff(IBufferWriter<byte> writer);  // Prepare: 不改变状态
    void OnCommitSucceeded();                            // Finalize: 追平状态 → Clean
    void DiscardChanges();                               // 丢弃变更，状态转换按规范
}
```

## 规范对齐说明

| 条款 | 实现 | 备注 |
|------|------|------|
| `[A-OBJECT-STATE-PROPERTY]` | ✅ `State` 属性 | O(1) 复杂度，含 Detached 状态不抛异常 |
| `[A-HASCHANGES-O1-COMPLEXITY]` | ✅ `HasChanges` 属性 | O(1) 复杂度 |
| `[S-TRANSIENT-DISCARD-DETACH]` | ✅ `DiscardChanges()` | TransientDirty → Detached |

## 测试结果

- **Targeted**: `dotnet test --filter "IDurableObjectTests"` → **27/27 Passed**
- **Full**: `dotnet test StateJournal.Tests` → **223/223 Passed**

### 测试覆盖

| 类别 | 测试数 | 描述 |
|------|--------|------|
| State 属性 | 4 | 所有状态下读取不抛异常 |
| HasChanges 属性 | 4 | 与状态一致性验证 |
| State-HasChanges 一致性 | 4 | Theory 验证映射关系 |
| DiscardChanges 状态转换 | 4 | PersistentDirty→Clean, TransientDirty→Detached 等 |
| OnCommitSucceeded 状态转换 | 2 | Dirty→Clean |
| 完整生命周期 | 4 | 多步状态转换验证 |
| WritePendingDiff | 2 | 不改变状态，写入数据 |
| ObjectId | 3 | 边界值验证 |

## 设计决策

1. **HasChanges 语义**：`HasChanges == (State is PersistentDirty or TransientDirty)`
   - Detached 状态返回 false（不是"有变更"，而是"无法访问"）

2. **DiscardChanges 对 Clean/Detached**：No-op 设计
   - 幂等操作，避免调用方状态检查负担

3. **WritePendingDiff 前置条件**：行为未定义（而非抛异常）
   - 真实实现由 Workspace 层保证只对 Dirty 对象调用

## 依赖关系

- 依赖 `DurableObjectState` 枚举（T-P2-04 已实现）
- 依赖 `System.Buffers.IBufferWriter<byte>`
- 被后续 `DurableMap<TKey, TValue>` 等具体类型实现引用

## Phase 2 完成状态

| 任务 | 状态 |
|------|------|
| T-P2-00 错误类型定义 | ✅ |
| T-P2-01 Address64/Ptr64 | ✅ |
| T-P2-02 VarInt 编解码 | ✅ |
| T-P2-03 FrameTag 位段编码 | ✅ |
| T-P2-04 DurableObjectState 枚举 | ✅ |
| T-P2-05 IDurableObject 接口 | ✅ |

**Phase 2 完成！** 🎉

## Changefeed Anchor

`#delta-2025-12-25-idurableobject`
