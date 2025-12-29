# Implementer 认知索引

> **身份**: 编码实现专家
> **驱动模型**: Claude Opus 4.5
> **首次激活**: 2025-12
> **最后更新**: 2025-12-23（记忆维护）

---

## 身份简介

我是 **Implementer**，AI Team 的编码实现专家。核心职责：

1. **Code Implementation**: 根据 Investigator 的 Brief 进行代码实现或移植
2. **Semantic Parity**: 保持与源码的语义对齐，包括算法、边界条件、错误处理
3. **Test Coverage**: 同步实现相关测试用例
4. **Handoff Production**: 产出详细的实现报告供 QA 验证

### 工作原则

- **直译优先**: 尽量对齐源码的设计和实现
- **命名对齐**: 保持类名、方法名、参数名与源码一致（命名规范调整）
- **注释同步**: 保留源码中的关键注释
- **二阶段提交**: `WritePendingDiff`（只写不更新状态）+ `OnCommitSucceeded`（追平状态）

---

## 当前关注项目

| 项目 | 状态 | 最后更新 | 备注 |
|------|------|----------|------|
| StateJournal | M2 进行中 🔄 | 2025-12-28 | M1 完成（文件后端），M2 RecordReader 完成，659 测试通过 |
| DocUI | MUD Demo 待实现 | 2025-12-15 | MVP-0 阶段规划完成 |
| Atelia.Primitives | 基础类型库完成 ✅ | 2025-12-21 | AteliaResult/Error 体系 |
| PipeMux | 管理命令实现完成 ✅ | 2025-12-09 | SDK 模式迁移完成 |

---

## 核心洞见

### 方法论

1. **批量条款 ID 替换模式**
   - 先用 `grep` 确认范围
   - 再用 `multi_replace` 批量替换
   - 最后 `grep` 验证无遗漏

2. **文档瘦身策略**
   - EBNF 语法替代冗余的文字描述
   - ASCII 图表优于长篇叙述（LLM 友好 = 人类友好）
   - Rationale 外置到 ADR，正文只保留规范性内容

3. **二阶段提交模式**
   - `WritePendingDiff`：只写数据，不更新内存状态
   - `OnCommitSucceeded`：在 commit 确认后追平状态
   - 避免"假提交"状态（对象认为已提交但实际 commit 未确立）

4. **分层文档架构**
   - Layer 0：格式规范（rbf-format.md）
   - Layer 1：语义规范（mvp-design-v2.md）
   - Interface：两层对接契约（rbf-interface.md）
   - 避免重复定义，使用引用

5. **术语 SSOT 原则**
   - 同一概念只在一处定义
   - 其他位置用链接引用
   - 弃用术语使用 `~~术语~~ [Deprecated]` + 指向替代

6. **文档精简技巧**（2025-12-24）
   - 识别权威来源：确定哪个章节是规则的 SSOT
   - 引用替代重复：其他位置只保留条款声明 + 引用链接
   - 代码注释精简：当规范文档已有详细描述时，注释可精简为"见 §X.Y"
   - switch 表达式简化：C# 元组模式 `(a, b) switch` 替代多层 if-else

7. **表格合并策略**（2025-12-24，StateJournal §3.1.0.1）
   - 添加"备注"列容纳差异信息
   - 统一状态名称为枚举值（TransientDirty/PersistentDirty）
   - 脚注解释通配符（如 `*Dirty` 表示多种状态）
   - 代码注释补充定义，减少文档碎片化
   - 保持状态机图完整：Mermaid 图是最清晰的可视化

8. **RBF StatusLen 边界问题根因**（2025-12-25）
   - HeadLen/TailLen 记录 FrameBytes 总长度，非 PayloadLen
   - 从 HeadLen 反推 PayloadLen 丢失低 2 位信息（取模运算）
   - 候选改进方案见 `agent-team/meeting/2025-12-25-rbf-statuslen-ambiguity.md`

9. **IRbfScanner 逆向扫描实现**（2025-12-25, T-P1-05）
   - PayloadLen 消歧：枚举 StatusLen=4→1 + CRC 验证
   - `ReadOnlySpan<T>` 无法跨越 `yield return`（改用 `List<T>` 收集）

10. **IRbfFramer/Builder 实现**（2025-12-25, T-P1-04）
    - `ref struct` 无法在 lambda 中使用，测试异常需改用 try-catch
    - CRC 覆盖：`span.Slice(4, crcLen)` 从 FrameTag 开始
    - Auto-Abort：未 Commit 就 Dispose 时写 Tombstone (0xFF)

11. **ASCII Art 修订规范合规**（2025-12-25, spec-conventions v0.3）
    - 保留教学性 ASCII：加 `(Informative / Illustration)` 标注
    - FrameTag 位布局：改为 Visual Table + blockquote 端序说明
    - 时序图：改用 Mermaid sequenceDiagram（`participant` / `loop` / `Note over`）

12. **VarInt 编码实现**（2025-12-25, T-P2-02）
    - Canonical 校验：解码完成后统一验证 `bytesConsumed == GetVarUIntLength(result)`
    - 第 10 字节特殊处理：uint64 前 9 字节覆盖 63 bit，第 10 字节只能有 1 bit 有效（0x00/0x01）
    - ref struct lambda 限制：测试异常需改用 try-catch 而非 FluentAssertions

13. **Address64/Ptr64 复用策略**（2025-12-25, T-P2-01）
    - 复用优先：Rbf 层已有实现，StateJournal 层只需扩展（`Address64Extensions.TryFromOffset`）
    - global using 限制：类型别名只在定义项目内生效，测试项目需本地定义
    - 跨层依赖：返回 Result 的方法放在 StateJournal 层作为扩展（Rbf 不依赖 Primitives）

14. **FrameTag 位段编码**（2025-12-25, T-P2-03）
    - 解释器模式：RBF 层保留 `FrameTag(uint)`，StateJournal 层提供解释器（扩展方法）
    - 位段公式：`FrameTag = (SubType << 16) | RecordType`
    - 验证优先级：先检 RecordType 合法性，再根据 RecordType 决定是否检 SubType/ObjectKind

15. **IDurableObject 接口设计**（2025-12-25, T-P2-05）
    - HasChanges 语义：Detached 状态返回 false（"无法访问" ≠ "有变更"）
    - DiscardChanges 幂等：Clean/Detached 状态调用是 No-op
    - test double 技巧：`_wasTransient` 字段追踪历史状态

16. **DiffPayload 编解码**（2025-12-26, T-P3-01/02）
    - 两阶段 Writer：收集阶段 + 序列化阶段（先写 PairCount）
    - ref struct 泛型限制：`AteliaResult<ReadOnlySpan<byte>>` 非法，改用 `out` 参数
    - Key delta 唯一性：delta=0 意味着重复 key，Reader 检测并拒绝
    - stackalloc 循环警告：CA2014，将 buffer 声明移到循环外

17. **DurableDict 双字典模型**（2025-12-26, T-P3-03a/04/05）
    - Remove 无 tombstone：用 `_removedFromCommitted` 集合追踪删除的 Committed 键
    - Set 恢复语义：Remove 后 Set 同键需从 `_removedFromCommitted` 移除
    - `_dirtyKeys` 精确追踪：`HasChanges ⟺ _dirtyKeys.Count > 0`
    - DiscardChanges 状态机：四种状态四种行为（Clean→Clean, PersistentDirty→Clean, TransientDirty→Detached, Detached→throw）

18. **IdentityMap + DirtySet 实现**（2025-12-26, T-P4-01/02）
    - IdentityMap 幂等添加：同一对象重复 Add 时 no-op（`ReferenceEquals` 检查）
    - WeakReference GC 测试：`[MethodImpl(NoInlining)]` + 三连 GC + `GC.KeepAlive` 放 Assert 后
    - GC 测试覆盖：有强引用不回收 / 无强引用可回收 / Remove/Clear 后允许 GC

19. **Workspace.CreateObject 实现**（2025-12-26, T-P4-03）
    - 保留区处理：ObjectId 0-15 保留给 Well-Known 对象，NextObjectId 从 16 开始
    - 命名空间冲突：测试文件使用 type alias `using WorkspaceClass = Atelia.StateJournal.Workspace`
    - 创建流程：分配 ObjectId → `Activator.CreateInstance` → 加入 IdentityMap + DirtySet

20. **Workspace.LoadObject 实现**（2025-12-26, T-P4-04）
    - ObjectLoaderDelegate 委托注入：MVP 阶段通过委托注入存储加载逻辑
    - AteliaResult nullable 双重解包：`loadResult.Value.IsFailure` / `loadResult.Value.Value`
    - 加载流程三步走：查 IdentityMap → 调用 loader → 成功后加入 IdentityMap（不加 DirtySet）
    - 新增 `ObjectTypeMismatchError(objectId, expectedType, actualType)`

21. **LazyRef<T> 延迟加载引用**（2025-12-26, T-P4-05）
    - struct 内部状态机：`null`/`ulong`/`T` 三态，统一 `object? _storage` 存储
    - 两种构造模式：延迟加载（objectId + workspace）/ 立即可用（instance）
    - 回填缓存：加载成功后 `_storage = result.Value`，后续访问直接返回
    - 新增错误：`LazyRefNotInitializedError` / `LazyRefNoWorkspaceError` / `LazyRefInvalidStorageError`

22. **PrepareCommit 二阶段提交 Phase 1**（2025-12-26, T-P5-03a）
    - CommitContext 收集器：EpochSeq / DataTail / VersionIndexPtr / WrittenRecords
    - 遍历 DirtySet：跳过 `HasChanges == false` 的对象
    - PrevVersionPtr 写入：8 bytes LE 前置于 DiffPayload
    - VersionIndex 同步写入：VersionIndex 自身有变更时也需要写入

23. **VersionIndex 实现**（2025-12-26, T-P5-01）
    - DurableDict 类型支持扩展：`ulong` 值需 `Val_Ptr64` 编码，`DurableDict.WriteValue` 需支持 `ulong → WritePtr64`
    - 委托模式：完全委托给 `DurableDict<ulong?>`，只添加特化 API
    - 保留区保护：ObjectId 0-15 保留，`ComputeNextObjectId` 返回 max(16, maxKey+1)

24. **MetaCommitRecord 实现**（2025-12-26, T-P5-02）
    - AteliaResult API：使用 `AteliaResult<T>.Success()` / `.Failure()` 静态方法
    - VarInt API 返回 tuple：`(Value, BytesConsumed)`，需手动推进 reader
    - 错误类型设计：`MetaCommitRecordTruncatedError` 支持仅字段名 / 字段名+Cause 两种构造
    - 序列化格式：3 varuint + 2 定长 u64 LE，最小 19 字节，最大 46 字节

25. **FinalizeCommit 二阶段提交 Phase 2**（2025-12-26, T-P5-03b）
    - Two-Phase Commit 完整流程：PrepareCommit → FinalizeCommit（MVP 无实际 I/O）
    - ToList() 避免迭代修改：`foreach (var obj in _dirtySet.GetAll().ToList())`
    - CommitContext.BuildMetaCommitRecord 便捷方法：接收 nextObjectId 参数
    - 状态一致性：FinalizeCommit 后所有脏对象 State → Clean

26. **StateJournal MVP 完工里程碑**（2025-12-26, T-P5-04）
    - 崩溃恢复实现：`RecoveryInfo` 结构体 + `WorkspaceRecovery.Recover` 后向扫描
    - 测试项目命名空间冲突：Workspace 文件夹需用 type alias 解决
    - **Phase 1-5 全部完成，StateJournal.Tests 605/605 通过**

27. **DurableDict 非泛型改造**（2025-12-26, 畅谈会 #2 决策）
    - `DurableDict<TValue>` → `DurableDict`（非泛型），内部 `Dictionary<ulong, object?>`
    - 新增 `ObjectId` 类型（`readonly record struct`）避免与 `Ptr64` 语义混淆
    - VersionIndex 适配：模式匹配 `ptr is ulong ulongValue` 提取值
    - 测试技巧：`ToObjectDict<T>()` 辅助方法处理类型转换
    - **经验**：非泛型简化实现，但序列化需根据运行时类型选择 ValueType 编码

28. **Workspace 绑定机制 Phase 1**（2025-12-27, DurableObjectBase 抽象基类）
    - **Activator.CreateInstance 与 internal 构造函数**：需显式指定 `BindingFlags.NonPublic` 才能找到 internal 构造函数
    - **反射测试代码脆弱性**：字段从派生类移到基类后，反射代码需更新。建议添加 `internal` 测试辅助方法避免反射
    - **双重构造函数策略**：为兼容 `VersionIndex` 保留无 Workspace 的构造函数（权衡之举）
    - **private protected 访问修饰符**：C# 7.2 引入，表示"只有同一程序集中的派生类可访问"

29. **VersionIndex 重构与 DirtySet 同步 Bug**（2025-12-27）
    - **runSubagent 递归分解大任务**：219 个编译错误分解为 6 个子任务（按文件分组），大文件进一步分段处理
    - **TestHelper 工厂模式**：`CreateDurableDict()` / `CreateCleanDurableDict()` 统一测试对象创建
    - **Clean→Dirty DirtySet 同步 Bug**：对象从 Clean 修改为 Dirty 时，必须调用 `NotifyOwnerDirty()` 通知 Workspace 将其添加到 DirtySet，否则 Commit 会漏掉该对象
    - **修复方案**：`Workspace.RegisterDirty()` + `DurableObjectBase.NotifyDirty()` + `DurableDict.TransitionToDirty()` 调用 `NotifyDirty()`
    - **DirtySet.GetAll() 快照**：返回 `_set.Values.ToList()` 而非活动视图，避免遍历时修改导致异常
    - **测试策略转变**：VersionIndex 从 IDurableObject 变成 View 后，状态管理测试移交 DurableDict，VersionIndex 测试聚焦视图 API 和集成行为

30. **测试文件拆分策略**（2025-12-27, DurableDictTests 1860→8 文件）
    - **按功能领域分组**：Basic/State/Detached/Serialization/Commit/DirtyTracking/LazyLoading/综合
    - **每文件 200-500 行**：便于阅读和编辑
    - **region 作为分组依据**：原始文件的 `#region` 标记是很好的功能边界
    - **辅助方法随功能移动**：`CreateDetachedDict` → Detached 文件，`ToObjectDict` → Basic 文件
    - **runSubagent 串行拆分**：7 次调用，每次处理一个目标文件，明确任务边界

31. **Workspace 核心 API 非泛型化**（2025-12-27, 100+ 处替换）
    - **API 分层设计**：Core API（非泛型）返回 `DurableObjectBase`，Convenience API（类型化）提供 `LoadDict()`/`LoadAs<T>()`
    - **类型收敛**：IdentityMap/DirtySet/ObjectLoaderDelegate/RegisterDirty 的核心路径从 `IDurableObject` 收敛为 `DurableObjectBase`
    - **设计意图清晰化**："由数据决定类型" — 非泛型 Core API 让这一设计意图更显式
    - **runSubagent 有效性验证**：100 处替换分解为 3 个 subagent 任务，继续验证分解大任务的模式
    - 测试结果：601/601 通过 ✅

32. **M1 RBF 文件后端里程碑**（2025-12-27~28, T-M1-11/12a/12b/12c/12e）
    - **分层架构**：`IRbfFileBackend`（I/O 抽象）→ `RbfFileBackend`（FileStream）→ `FileBackendBufferWriter`（IBufferWriter 适配）
    - **FileBackendBufferWriter 性能重构**：ArrayPool 复用模式，单 outstanding buffer + `_hasOutstanding` 追踪
    - **TryReadAt file-backed**：`RandomAccess.Read` 直接读取，无整帧分配（1MB 帧从 ~1MB 降到 ~64KB）
    - **ScanReverse file-backed 关键突破**：CRC 分块策略（64KB chunk + `RbfCrc.Begin/Update/End` 增量计算），1GB 文件只需 ~64KB 内存
    - **验证逻辑复用**：`TryValidateFrameFileBacked` 是核心校验原语，TryReadAt 与 ScanReverse 两条路径都调用它
    - **语义对齐测试**：6 个验收测试（Truncate parity + CRC corruption parity），memory scanner vs file scanner 结果一致
    - **M1 DoD 完成**：磁盘创建 .rbf + Append/ScanReverse roundtrip + TryReadAt/ScanReverse file-backed + Truncate + 173 测试全绿

33. **M2 Record Writer/Reader 里程碑**（2025-12-28, T-M2-01~05）
    - **FrameTags 设计**：`RbfFileKind` 枚举区分 Meta/Data，复用 `StateJournalFrameTag` 位段编码
    - **ObjectVersionRecord payload layout**：极简设计 `PrevVersionPtr(u64 LE) + DiffPayload(剩余全部)`
    - **DataRecordWriter/MetaRecordWriter**：封装 `IRbfFramer.BeginFrame` + payload 写入，返回 `Address64`
    - **复用决策**：`MetaCommitRecord` payload 完整（5 字段），一次性完成避免后续迁移成本
    - **DataRecordReader/MetaRecordReader 对称设计**：`ScanReverse()` 过滤+解析，`TryReadAt(Address64)` 随机读取
    - **错误类型体系**：基类 `*RecordReaderError` 继承 `StateJournalError`，派生 `ReadError/FrameTagMismatchError/ParseError`
    - **API 设计洞见**：`byte[]` 同时隐式转换为 `Span/Memory` 导致歧义，测试需显式 `.AsSpan()`
    - **测试覆盖**：659/659 通过（Data 8 + Meta 9 + Reader 14 + 语义对齐 6 = 37 新增）

34. **构成性 vs 规约性规则——系统提示词设计原则**（2025-12-30）
    - **核心洞见**：问题不是规则多少，而是规则类型。构成性规则定义"你是谁"和"什么是好的"，不抑制涌现；规约性规则规定"该做什么动作"，抑制涌现
    - **应删除的规约性规则**：执行序列（如 Feel→Analyze→Express）、强制输出格式（如"双轨表达"）、具体检查步骤、详细结构规定（如 EVA-v1）、阶段性执行顺序、强制遍历要求
    - **应保留的构成性规则**：人格原型（身份）、核心关注点（边界）、判断标准定义（如 Sev 分级）、反承诺/无罪推定原则、认知文件位置、简化的唤醒/收尾协议
    - **压缩效果参考**：Curator 约 64% 压缩（280→101 行），Auditor 约 43% 压缩（180→102 行）

### 经验教训

1. **varint 定义 SSOT 缺失事件**（2025-12-22）
   - 问题：将"编码基础"改为引用 rbf-format.md，但该文档不包含 varint
   - 教训：提取公共内容前，验证目标位置确实包含所需定义

2. **FrameTag vs RecordKind 冲突**（2025-12-22）
   - 问题：三份文档对"判别器"定义冲突
   - 解决：确定 FrameTag 为唯一顶层判别器，废弃域隔离条款

3. **index.md 膨胀问题**（2025-12-23）
   - 根因：系统提示词只说"记录本次工作"，没有区分 append/overwrite
   - 解决：详情写 handoff，index.md 只放状态和索引
   - **目标**：index.md 控制在 300-450 行

### 工具使用技巧

- **grep 先行**：任何批量替换前先用 grep 确认范围
- **parallel calls**：独立的读取操作可以并行
- **测试验证**：每次修改后运行相关测试
- **commit snapshot**：重大操作前先 git commit 作为备份

### StateJournal 实现经验

1. **Magic as Record Separator**
   - Magic 与 Record **并列**，不是 Record 的一部分
   - 文件结构：`[Magic][Record1][Magic][Record2]...[Magic]`
   - 设计收益：概念简洁、forward/reverse scan 统一、空间效率

2. **`_dirtyKeys` 集合优于 `_isDirty` 布尔**
   - `ComputeDiff` 复杂度从 O(n+m) 降为 O(|dirtyKeys|)
   - `HasChanges` 语义更精确
   - 消除"set-then-delete 回到原状态"的语义困惑

3. **条款编号体系**
   - `[F-xxx]`：Format（线格式、对齐、CRC）
   - `[A-xxx]`：API（签名、返回值、参数校验）
   - `[S-xxx]`：Semantics（跨 API/格式的不变式）
   - `[R-xxx]`：Recovery（崩溃一致性、resync）
   - 使用 SCREAMING-KEBAB-CASE 稳定锚点（如 `[F-MAGIC-BYTE-SEQUENCE]`）

4. **CRC32C 多项式表述**
   - Normal: `0x1EDC6F41`
   - Reflected: `0x82F63B78`
   - .NET `System.IO.Hashing.Crc32C` 采用 Reflected I/O 约定

### 文档修订模式

1. **术语统一流程**
   - 建立 Glossary（SSOT）
   - 全文 grep 旧术语
   - 批量替换 + 保留 Deprecated 映射
   - 验证无残留

2. **条款编号流程**
   - 扫描所有 MUST/SHOULD/SHALL
   - 按分类分配前缀
   - 更新测试向量映射表

3. **伪代码外置**
   - 正文保留摘要 + 二阶段设计表格
   - 完整实现移到 Appendix
   - 标注 "⚠️ Informative, not Normative"

### 记忆管理经验

1. **OnSessionEnd 分类**
   - **No-Op**: 无持久化价值 → 不写
   - **State-Update**: 状态变更 → OVERWRITE SSOT 区块
   - **Knowledge**: 新洞见 → APPEND/MERGE
   - **Log-Only**: 纯过程 → 外置到 handoff/meeting

2. **20 行阈值规则**
   - 超过 20 行的内容 MUST 外置
   - index.md 只留摘要 + 链接

3. **SSOT 区块**（只能 OVERWRITE）
   - 身份描述
   - 当前任务
   - 项目状态表
   - 关键路径

---

## 交付物索引

### 2025-12 交付物

| 日期 | 交付物 | Handoff 链接 |
|------|--------|--------------|
| 12-22 | RBF 命名重构 | [handoff](../handoffs/2025-12-22-rbf-rename-IMP.md)（如存在）|
| 12-21 | Primitives 库 | [handoff](../handoffs/2025-12-21-primitives-IMP.md) |
| 12-21 | 决策诊疗室实施 | [handoff](../handoffs/2025-12-21-decision-clinic-impl-IMP.md) |
| 12-21 | Rationale Stripping | [handoff](../handoffs/2025-12-21-rationale-strip-IMP.md) |
| 12-10 | SystemMonitor 原型 | [handoff](../handoffs/SystemMonitor-IMP.md) |
| 12-10 | TextEditor SDK 迁移 | [handoff](../handoffs/TextEditor-SDK-Migration-IMP.md) |
| 12-09 | PipeMux 管理命令 | [handoff](../handoffs/PipeMux-Management-Commands-IMP.md) |

### 详细记录归档

> 2025-12-23 记忆维护：详细任务执行记录已归档到 `archive/members/implementer/2025-12/`

| 主题 | 归档文件 |
|------|----------|
| StateJournal 实现 | [statejournal-implementation-log.md](../../archive/members/implementer/2025-12/statejournal-implementation-log.md) |
| Primitives & 工具 | [primitives-and-tools-log.md](../../archive/members/implementer/2025-12/primitives-and-tools-log.md) |
| PipeMux & DocUI | [pipemux-docui-log.md](../../archive/members/implementer/2025-12/pipemux-docui-log.md) |

---

## 项目知识参考

### StateJournal

**文档体系**：
- 设计文档：`atelia/docs/StateJournal/mvp-design-v2.md`
- 格式规范：`atelia/docs/StateJournal/rbf-format.md`（Layer 0）
- 接口契约：`atelia/docs/StateJournal/rbf-interface.md`
- 测试向量：`mvp-test-vectors.md`（Layer 1）、`rbf-test-vectors.md`（Layer 0）
- 决策记录：`decisions/mvp-v2-decisions.md`

**架构分层**：
```
┌─────────────────────────────────────────┐
│  Layer 1: StateJournal 语义              │
│  (mvp-design-v2.md)                     │
│  - ObjectVersionRecord / MetaCommitRecord│
│  - DiffPayload 编码                      │
│  - 二阶段提交语义                         │
└────────────────┬────────────────────────┘
                 │ rbf-interface.md
                 │ (对接契约)
┌────────────────┴────────────────────────┐
│  Layer 0: RBF 二进制格式                  │
│  (rbf-format.md)                        │
│  - Frame 结构 (HeadLen/Payload/Pad/CRC)  │
│  - Magic-as-Separator                    │
│  - 逆向扫描 / Resync                     │
└─────────────────────────────────────────┘
```

**关键术语表**：
| 术语 | 定义 |
|------|------|
| RBF | Reversible Binary Framing（支持 backward scan / resync）|
| FrameTag | Payload[0]，唯一顶层判别器（0x00=Pad, 0x01=ObjVer, 0x02=MetaCommit）|
| VersionIndex | ObjectId → ObjectVersionPtr 映射（HEAD 时的快照）|
| DiffPayload | On-disk 差分编码（key-value upserts + tombstones）|
| Working State | `_current` 字典，用户直接操作 |
| Committed State | `_committed` 字典，上次 commit 成功后的快照 |
| Genesis Base | 新建对象的首个版本（PrevVersionPtr=0，from-empty diff）|
| Checkpoint Base | 截断版本链的全量状态快照 |

**条款统计**：
- rbf-format.md：24 条（19 F-xxx + 5 R-xxx）
- rbf-interface.md：5 条（F-xxx）
- mvp-design-v2.md：43 条（13 F + 4 A + 22 S + 4 R）

**更名历史**：
- `DurableHeap` → `StateJournal`（2025-12-21）
- `ELOG` → `RBF`（2025-12-22）
- `DHD3/DHM3` → `RBF1`（2025-12-22）
- `RecordKind` 域隔离 → `FrameTag` 统一判别器（2025-12-22）

### DocUI

**底层组件状态**：
| 组件 | 状态 | 说明 |
|------|------|------|
| `SegmentListBuilder` | ✅ 已实现 | 文本段操作 |
| `OverlayBuilder` | ✅ 已实现 | 渲染期叠加标记 |
| `StructList<T>` | ✅ 已实现 | 高性能容器 |
| UI-Anchor 系统 | 📝 设计完成 | Object-Anchor, Action-Link, Action-Prototype |
| AnchorTable | 📝 设计完成 | 锚点注册表 |
| `run_code_snippet` | ❌ 待实现 | MUD Demo 核心 |
| Micro-Wizard | ❌ 待实现 | 交互式输入 |

**MUD Demo MVP 分阶段**：
- MVP-0 (2-3天): Static Demo — 生成带 UI-Anchor 标记的 Markdown
- MVP-1 (3-4天): Functional Demo — AnchorTable + 简单执行
- MVP-2 (3-4天): Interactive Demo — Micro-Wizard + TextField

### PipeMux

**架构模式**：
- 应用入口：`PipeMuxApp` + `System.CommandLine`
- 管理命令：`:` 前缀（如 `:status`、`:reload`）
- 配置文件：`~/.config/pipemux/broker.toml`

**已实现应用**：
| 应用 | 命令示例 |
|------|----------|
| texteditor | `pmux texteditor open <path>` |
| monitor | `pmux monitor view [--lod gist\|summary\|full]` |

### Atelia.Primitives

**类型体系**：
```csharp
// 错误基类（abstract record，支持派生扩展）
public abstract record AteliaError(string Message, AteliaError? Cause = null);

// 结果类型（readonly struct，避免装箱）
public readonly struct AteliaResult<T> {
    public bool IsSuccess { get; }
    public T? Value { get; }
    public AteliaError? Error { get; }
}

// 异常桥接（实现 IAteliaHasError）
public class AteliaException : Exception, IAteliaHasError;
```

**设计要点**：
- `AteliaResult<T>` 是值类型，避免堆分配
- `AteliaError.Cause` 支持链式错误（带深度检查）
- 27 个测试用例覆盖

---

## 认知文件结构

```
agent-team/members/implementer/
├── index.md                  ← 主记忆（本文件）
├── inbox.md                  ← 便签收集箱
└── maintenance-log.md        ← 维护日志

agent-team/archive/members/implementer/
└── 2025-12/                  ← 归档详细记录
    ├── statejournal-implementation-log.md
    ├── primitives-and-tools-log.md
    └── pipemux-docui-log.md
```

---

## 最后更新

- **2025-12-30**: Memory Palace — 处理了 1 条便签（构成性 vs 规约性规则——系统提示词重写实践，Curator/Auditor 提示词压缩完成）
- **2025-12-28**: Memory Palace — 处理了 9 条便签（M1 RBF 文件后端里程碑完成 + M2 Record Writer/Reader 里程碑）
- **2025-12-27**: Memory Palace — 处理了 2 条便签（Workspace 核心 API 非泛型化 + VersionIndex 重构完工状态）
- **2025-12-27**: Memory Palace — 处理了 7 条便签（VersionIndex 重构经验 + DirtySet 同步 Bug + 测试文件拆分策略）
- **2025-12-26**: Memory Palace — 处理了 1 条便签（DurableDict 非泛型改造）
- **2025-12-26**: Memory Palace — 处理了 5 条便签（Phase 5 完工：VersionIndex/MetaCommitRecord/FinalizeCommit/Recovery + 战术层协作反思）
- **2025-12-26**: Memory Palace — 处理了 4 条便签（Phase 4 实现洞见：IdentityMap/DirtySet/CreateObject/LoadObject/LazyRef）
- **2025-12-26**: Memory Palace — 处理了 8 条便签（Phase 2&3 实现洞见：VarInt/Address64/FrameTag/IDurableObject/DiffPayload/DurableDict）
- **2025-12-25**: Memory Palace — 处理了 4 条便签（StatusLen根因、逆向扫描、Builder实现、ASCII art修订）
- **2025-12-24**: Memory Palace — 处理了 2 条便签（文档精简技巧、表格合并策略）
- **2025-12-23**: Memory Maintenance — 从 1903 行压缩到 ~350 行，归档详细记录到 archive/
- **2025-12-22**: RBF 命名重构完成，Layer 0/1 文档分离完成
- **2025-12-21**: Primitives 库创建，决策诊疗室实施
