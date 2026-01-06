# StateJournal 实现细节归档

> **归档日期**: 2026-01-03
> **归档原因**: 实现细节已稳定，从 index.md 迁移以降低行数
> **来源**: implementer/index.md 核心洞见 #16-26

---

## Phase 3: DiffPayload 编解码 (T-P3-01/02/03a/04/05)

### DiffPayload Writer/Reader
- 两阶段 Writer：收集阶段 + 序列化阶段（先写 PairCount）
- ref struct 泛型限制：`AteliaResult<ReadOnlySpan<byte>>` 非法，改用 `out` 参数
- Key delta 唯一性：delta=0 意味着重复 key，Reader 检测并拒绝
- stackalloc 循环警告：CA2014，将 buffer 声明移到循环外

### DurableDict 双字典模型
- Remove 无 tombstone：用 `_removedFromCommitted` 集合追踪删除的 Committed 键
- Set 恢复语义：Remove 后 Set 同键需从 `_removedFromCommitted` 移除
- `_dirtyKeys` 精确追踪：`HasChanges ⟺ _dirtyKeys.Count > 0`
- DiscardChanges 状态机：四种状态四种行为（Clean→Clean, PersistentDirty→Clean, TransientDirty→Detached, Detached→throw）

---

## Phase 4: IdentityMap + DirtySet (T-P4-01~05)

### IdentityMap/DirtySet 实现
- IdentityMap 幂等添加：同一对象重复 Add 时 no-op（`ReferenceEquals` 检查）
- WeakReference GC 测试：`[MethodImpl(NoInlining)]` + 三连 GC + `GC.KeepAlive` 放 Assert 后

### Workspace.CreateObject/LoadObject
- 保留区处理：ObjectId 0-15 保留给 Well-Known 对象，NextObjectId 从 16 开始
- 命名空间冲突：测试文件使用 type alias `using WorkspaceClass = Atelia.StateJournal.Workspace`
- ObjectLoaderDelegate 委托注入：MVP 阶段通过委托注入存储加载逻辑
- AteliaResult nullable 双重解包：`loadResult.Value.IsFailure` / `loadResult.Value.Value`

### LazyRef<T> 延迟加载引用
- struct 内部状态机：`null`/`ulong`/`T` 三态，统一 `object? _storage` 存储
- 两种构造模式：延迟加载（objectId + workspace）/ 立即可用（instance）
- 回填缓存：加载成功后 `_storage = result.Value`，后续访问直接返回

---

## Phase 5: 二阶段提交 (T-P5-01~04)

### PrepareCommit Phase 1
- CommitContext 收集器：EpochSeq / DataTail / VersionIndexPtr / WrittenRecords
- 遍历 DirtySet：跳过 `HasChanges == false` 的对象
- PrevVersionPtr 写入：8 bytes LE 前置于 DiffPayload
- VersionIndex 同步写入：VersionIndex 自身有变更时也需要写入

### VersionIndex 实现
- DurableDict 类型支持扩展：`ulong` 值需 `Val_Ptr64` 编码
- 委托模式：完全委托给 `DurableDict<ulong?>`
- 保留区保护：ObjectId 0-15 保留，`ComputeNextObjectId` 返回 max(16, maxKey+1)

### MetaCommitRecord 实现
- AteliaResult API：使用 `AteliaResult<T>.Success()` / `.Failure()` 静态方法
- VarInt API 返回 tuple：`(Value, BytesConsumed)`，需手动推进 reader
- 序列化格式：3 varuint + 2 定长 u64 LE，最小 19 字节，最大 46 字节

### FinalizeCommit Phase 2
- Two-Phase Commit 完整流程：PrepareCommit → FinalizeCommit（MVP 无实际 I/O）
- ToList() 避免迭代修改：`foreach (var obj in _dirtySet.GetAll().ToList())`
- 状态一致性：FinalizeCommit 后所有脏对象 State → Clean

### Recovery 实现
- 崩溃恢复：`RecoveryInfo` 结构体 + `WorkspaceRecovery.Recover` 后向扫描
- 测试项目命名空间冲突：Workspace 文件夹需用 type alias 解决

---

## 重构里程碑

### DurableDict 非泛型改造 (12-26 畅谈会 #2)
- `DurableDict<TValue>` → `DurableDict`（非泛型），内部 `Dictionary<ulong, object?>`
- 新增 `ObjectId` 类型（`readonly record struct`）避免与 `Ptr64` 语义混淆
- VersionIndex 适配：模式匹配 `ptr is ulong ulongValue` 提取值
- 测试技巧：`ToObjectDict<T>()` 辅助方法处理类型转换
- **经验**：非泛型简化实现，但序列化需根据运行时类型选择 ValueType 编码

### Workspace 绑定机制 Phase 1 (12-27)
- Activator.CreateInstance 与 internal 构造函数：需显式指定 `BindingFlags.NonPublic`
- 双重构造函数策略：为兼容 `VersionIndex` 保留无 Workspace 的构造函数
- private protected 访问修饰符：C# 7.2 引入，表示"只有同一程序集中的派生类可访问"

### VersionIndex 重构与 DirtySet 同步 Bug (12-27)
- runSubagent 递归分解大任务：219 个编译错误分解为 6 个子任务
- TestHelper 工厂模式：`CreateDurableDict()` / `CreateCleanDurableDict()` 统一创建
- Clean→Dirty DirtySet 同步 Bug：修改为 Dirty 时必须调用 `NotifyOwnerDirty()`
- DirtySet.GetAll() 快照：返回 `_set.Values.ToList()` 而非活动视图

### 测试文件拆分策略 (12-27)
- 按功能领域分组：Basic/State/Detached/Serialization/Commit/DirtyTracking/LazyLoading/综合
- 每文件 200-500 行：便于阅读和编辑
- region 作为分组依据：原始文件的 `#region` 标记是很好的功能边界

### Workspace 核心 API 非泛型化 (12-27)
- API 分层设计：Core API（非泛型）返回 `DurableObjectBase`，Convenience API（类型化）
- 类型收敛：IdentityMap/DirtySet/ObjectLoaderDelegate/RegisterDirty 收敛为 `DurableObjectBase`
- runSubagent 有效性验证：100 处替换分解为 3 个 subagent 任务

---

## M1 RBF 文件后端 (12-27~28)

### 分层架构
- `IRbfFileBackend`（I/O 抽象）→ `RbfFileBackend`（FileStream）→ `FileBackendBufferWriter`（IBufferWriter 适配）
- FileBackendBufferWriter 性能重构：ArrayPool 复用模式，单 outstanding buffer + `_hasOutstanding` 追踪

### TryReadAt file-backed
- `RandomAccess.Read` 直接读取，无整帧分配（1MB 帧从 ~1MB 降到 ~64KB）

### ScanReverse file-backed 关键突破
- CRC 分块策略（64KB chunk + `RbfCrc.Begin/Update/End` 增量计算）
- 1GB 文件只需 ~64KB 内存
- 验证逻辑复用：`TryValidateFrameFileBacked` 是核心校验原语

### 语义对齐测试
- 6 个验收测试（Truncate parity + CRC corruption parity）
- memory scanner vs file scanner 结果一致

---

## M2 Record Writer/Reader (12-28)

### FrameTags 设计
- `RbfFileKind` 枚举区分 Meta/Data
- 复用 `StateJournalFrameTag` 位段编码

### ObjectVersionRecord payload layout
- 极简设计 `PrevVersionPtr(u64 LE) + DiffPayload(剩余全部)`

### DataRecordWriter/MetaRecordWriter
- 封装 `IRbfFramer.BeginFrame` + payload 写入，返回 <deleted-place-holder>

### DataRecordReader/MetaRecordReader 对称设计
- `ScanReverse()` 过滤+解析
- `TryReadAt(<deleted-place-holder>)` 随机读取

### 错误类型体系
- 基类 `*RecordReaderError` 继承 `StateJournalError`
- 派生 `ReadError/FrameTagMismatchError/ParseError`

### API 设计洞见
- `byte[]` 同时隐式转换为 `Span/Memory` 导致歧义，测试需显式 `.AsSpan()`
