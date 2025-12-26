# Workspace 绑定机制 Phase 1 实现报告

> **状态**: ✅ 完成
> **日期**: 2025-12-27
> **实现者**: Implementer

---

## 实现摘要

根据 `workspace-binding-spec.md` Phase 1 要求，实现了 Workspace 绑定机制的核心部分。
主要工作包括：创建 `DurableObjectBase` 抽象基类、重构 `DurableDict` 继承体系、调整 Workspace 工厂方法。

---

## 文件变更

### 新增文件

| 文件路径 | 描述 |
|:---------|:-----|
| [src/StateJournal/Objects/DurableObjectBase.cs](../../../atelia/src/StateJournal/Objects/DurableObjectBase.cs) | 持久化对象抽象基类，包含 Workspace 绑定逻辑 |
| [src/StateJournal/Core/ObjectDetachedException.cs](../../../atelia/src/StateJournal/Core/ObjectDetachedException.cs) | 从 DurableDict.cs 提取的异常类 |

### 修改文件

| 文件路径 | 描述 |
|:---------|:-----|
| [src/StateJournal/Objects/DurableDict.cs](../../../atelia/src/StateJournal/Objects/DurableDict.cs) | 继承自 DurableObjectBase，移除冗余字段 |
| [src/StateJournal/Workspace/Workspace.cs](../../../atelia/src/StateJournal/Workspace/Workspace.cs) | 更新 CreateInstance 以支持新的构造函数签名 |
| [tests/StateJournal.Tests/Objects/DurableDictTests.cs](../../../atelia/tests/StateJournal.Tests/Objects/DurableDictTests.cs) | 修复反射测试代码 |

---

## 源码对齐说明

| 规范条款 | 实现 | 备注 |
|:---------|:-----|:-----|
| `[S-WORKSPACE-OWNING-EXACTLY-ONE]` | `DurableObjectBase._owningWorkspace` | readonly 字段 |
| `[S-WORKSPACE-OWNING-IMMUTABLE]` | `readonly Workspace _owningWorkspace` | 构造后不可变 |
| `[S-WORKSPACE-CTOR-REQUIRES-WORKSPACE]` | `protected internal` 构造函数 | 接收 Workspace 参数 |
| `[S-LAZYLOAD-DISPATCH-BY-OWNER]` | `protected T LoadObject<T>()` | 按 Owning Workspace 分派 |

---

## 设计决策

### 1. 双重构造函数策略

为兼容 `VersionIndex` 等内部对象，保留了无 Workspace 参数的构造函数：

```csharp
// 标准用户路径（强制绑定）
internal DurableDict(Workspace workspace, ulong objectId)

// 内部路径（VersionIndex 等 Well-Known 对象）
internal DurableDict(ulong objectId)
```

这是一个 **临时方案**，未来 Phase 2/3 可能统一处理。

### 2. 构造函数可见性

- `protected internal` 用于基类构造函数
- `internal` 用于派生类构造函数
- 禁止用户直接 `new DurableDict()`，强制通过 `Workspace.CreateObject<T>()`

### 3. Activator.CreateInstance 调用

需要显式指定 `BindingFlags.NonPublic` 才能找到 internal 构造函数：

```csharp
Activator.CreateInstance(
    typeof(T),
    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
    binder: null,
    args: [this, objectId],
    culture: null
);
```

---

## 测试结果

```
Passed!  - Failed: 0, Passed: 606, Skipped: 0, Total: 606
```

### 修复的测试问题

`CreateDetachedDict<T>()` 辅助方法使用反射设置 `_state` 字段，需要更新为从基类获取：

```csharp
// 修改前
typeof(DurableDict).GetField("_state", ...)

// 修改后
typeof(DurableObjectBase).GetField("_state", ...)
```

---

## 遗留问题

1. **VersionIndex 无 Workspace 绑定**：使用 `DurableDict(ulong objectId)` 构造函数，无法 Lazy Load。这是可接受的，因为 VersionIndex 是系统级对象。

2. **Phase 2 任务**：Lazy Loading 实现需要调用 `LoadObject<T>()` 方法，该方法已在基类中实现。

3. **Phase 3 任务**：Ambient Context（可选），规范中标记为 MAY。

---

## Changefeed Anchor

`#delta-2025-12-27-workspace-binding-phase1`
