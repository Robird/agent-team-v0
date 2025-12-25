# DurableHeap MVP v2 术语修订 Implementation Result

## 实现摘要

根据 2025-12-19 秘密基地畅谈会达成的术语共识，对 `DurableHeap/docs/mvp-design-v2.md` 进行了全面的术语统一修订，涵盖状态术语、版本索引、快照术语、指针术语、加载 API、差分术语、提交术语和对象级方法共 8 大类约 60+ 处替换。

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 术语全面统一

## 术语替换汇总

| 原术语 | 新术语 | 影响范围 |
|--------|--------|---------|
| `Baseline`（单独使用） | `Committed State` | 概念层描述 |
| `Current State`（概念名） | `Working State` | 概念层描述 |
| `EpochMap` | `VersionIndex` | 全文 15+ 处 |
| `EpochMapVersionPtr` | `VersionIndexPtr` | meta payload 字段 |
| `snapshot`（版本链语境） | `Checkpoint Version` | 版本链相关 |
| `Ptr64`（概念层） | `Address64` | 标识与指针 |
| `Resolve`（外部 API） | `LoadObject` | 读路径 API |
| `On-Disk Diff` | `DiffPayload` | 差分相关 |
| `state diff` | `DiffPayload` | 二进制编码 |
| `EpochRecord`（MVP） | `Commit Record` | 逻辑概念 |
| `head`/`Head` | `HEAD` | 全文统一大写 |
| `identity map` | `Identity Map` | Title Case |
| `_dirtySet` | `Dirty Set` | 概念层 |
| `Commit()`（对象级） | `FlushToWriter(writer)` | 伪代码 |

## 新增内容

### 术语表（Glossary）

在文档开头添加了规范化术语表，包含：

1. **状态与差分**：Working State, Committed State, ChangeSet, DiffPayload
2. **版本链**：Version Chain, Checkpoint Version, VersionIndex
3. **标识与指针**：ObjectId, Address64, ObjectVersionPtr
4. **提交与 HEAD**：Commit, HEAD, Commit Record
5. **载入与缓存**：Identity Map, Dirty Set, LoadObject
6. **对象级 API**：FlushToWriter

### Identity Map 正式定义

> **Identity Map**：进程内的 `ObjectId → WeakReference<DurableObject>` 映射，用于保证同一 ObjectId 在存活期间只对应一个内存对象实例。

### Dirty Set 正式定义

> **Dirty Set**：Workspace 级别的 `Set<ObjectId>`（强引用），记录所有具有未提交修改的对象。Commit 时遍历此集合写入新版本。

## 章节标题修订

| 原标题 | 新标题 |
|--------|--------|
| 4.1.2 Resolve 语义 | 4.1.2 LoadObject 语义 |
| 4.2.3 EpochRecord（逻辑概念） | 4.2.3 Commit Record（逻辑概念） |
| 4.2.4 EpochMap（ObjectId -> ObjectVersionPtr 的映射对象） | 4.2.4 VersionIndex（ObjectId -> ObjectVersionPtr 的映射对象） |
| 4.2.5 ObjectVersionRecord（对象版本，增量 state diff） | 4.2.5 ObjectVersionRecord（对象版本，增量 DiffPayload） |
| 4.3.2 Resolve(ObjectId) | 4.3.2 LoadObject(ObjectId) |
| 4.4.2 Dict 的 state diff | 4.4.2 Dict 的 DiffPayload |

## 伪代码修订

4.4.4 节的 `DurableDict.Commit()` 方法修改为 `FlushToWriter(writer)`：

```csharp
public void FlushToWriter(IRecordWriter writer) {
    if (!_isDirty) return;  // Fast path: O(1)
    
    var diff = ComputeDiff(_committed, _current);
    if (diff.Count == 0) {
        _isDirty = false;
        return;
    }
    
    WriteDiffTo(writer, diff);  // 可能抛异常；失败时内存状态不变
    
    _committed = Clone(_current);
    _isDirty = false;
}
```

## 保留的旧术语引用

以下位置有意保留旧术语（用于别名/弃用说明或历史问题描述）：

- 术语表的 "Deprecated" 列
- Q3 问题原文中的 "Resolve 语义"

## 遗留问题

无

## Changefeed Anchor

`#delta-2025-12-19-durableheap-terminology`
