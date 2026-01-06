# Implementer 认知索引

> **身份**: 编码实现专家
> **驱动模型**: Claude Opus 4.5
> **首次激活**: 2025-12
> **最后更新**: 2026-01-03（记忆维护）

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
| DocGraph | v0.2 进行中 🔄 | 2026-01-07 | v0.2: Wish 布局迁移 + IssueAggregator Phase 2 |
| StateJournal | M2 完成 ✅ | 2025-12-28 | 659 测试通过，待 M3 |
| DocUI | 待启动 | 2025-12-15 | MVP-0 规划完成 |
| Atelia.Primitives | 完成 ✅ | 2026-01-06 | 双类型架构（AteliaResult + AteliaAsyncResult） |
| PipeMux | 完成 ✅ | 2025-12-09 | SDK 模式迁移完成 |

---

## 核心洞见

### 方法论

1. **批量条款 ID 替换模式**
   - 先用 `grep` 确认范围 → `multi_replace` 批量替换 → `grep` 验证无遗漏

2. **文档瘦身策略**
   - EBNF 语法替代冗余文字；ASCII 图表优于长篇叙述；Rationale 外置到 ADR

3. **二阶段提交模式**
   - `WritePendingDiff`（只写数据）+ `OnCommitSucceeded`（追平状态）
   - 避免"假提交"状态

4. **分层文档架构**
   - Layer 0（格式）→ Interface（契约）→ Layer 1（语义）
   - 避免重复定义，使用引用

5. **术语 SSOT 原则**
   - 同一概念只在一处定义，其他位置用链接引用
   - 弃用术语使用 `~~术语~~ [Deprecated]` + 指向替代

6. **文档精简技巧**
   - 识别权威来源 → 引用替代重复 → 注释精简为"见 §X.Y"

7. **构成性 vs 规约性规则**（系统提示词设计原则）
   - 构成性规则定义"你是谁"，不抑制涌现
   - 规约性规则规定"该做什么动作"，抑制涌现
   - **应删除**：执行序列、强制输出格式、详细结构规定
   - **应保留**：人格原型、核心关注点、判断标准定义

### RBF 实现洞见

8. **Magic as Record Separator**
   - Magic 与 Record **并列**，不是 Record 的一部分
   - 设计收益：概念简洁、forward/reverse scan 统一、空间效率

9. **StatusLen 边界问题根因**
   - HeadLen/TailLen 记录 FrameBytes 总长度，非 PayloadLen
   - 从 HeadLen 反推 PayloadLen 丢失低 2 位信息

10. **IRbfScanner 逆向扫描**
    - PayloadLen 消歧：枚举 StatusLen=4→1 + CRC 验证
    - `ReadOnlySpan<T>` 无法跨越 `yield return`

11. **VarInt 编码**
    - Canonical 校验：`bytesConsumed == GetVarUIntLength(result)`
    - 第 10 字节只能有 1 bit 有效（0x00/0x01）

12. **ASCII Art 修订规范**（spec-conventions v0.3）
    - 保留教学性 ASCII：加 `(Informative)` 标注
    - 时序图改用 Mermaid sequenceDiagram

### StateJournal 高阶洞见

> 实现细节已归档：[statejournal-impl-details.md](../../archive/members/implementer/2025-12/statejournal-impl-details.md)

13. **`_dirtyKeys` 集合优于 `_isDirty` 布尔**
    - `ComputeDiff` 复杂度从 O(n+m) 降为 O(|dirtyKeys|)
    - 消除"set-then-delete 回到原状态"的语义困惑

14. **条款编号体系**
    - `[F-xxx]`：Format | `[A-xxx]`：API | `[S-xxx]`：Semantics | `[R-xxx]`：Recovery
    - 使用 SCREAMING-KEBAB-CASE 稳定锚点

15. **CRC32C 多项式**
    - Normal: `0x1EDC6F41` / Reflected: `0x82F63B78`
    - .NET `Crc32C` 采用 Reflected I/O 约定

16. **runSubagent 递归分解大任务**
    - 219 编译错误 → 6 个子任务（按文件分组）
    - 大文件进一步分段处理

17. **Clean→Dirty DirtySet 同步 Bug 修复模式**
    - `Workspace.RegisterDirty()` + `DurableObjectBase.NotifyDirty()` + `TransitionToDirty()`

18. **M1 文件后端关键突破**
    - CRC 分块策略（64KB chunk + 增量计算），1GB 文件只需 ~64KB 内存
    - `TryValidateFrameFileBacked` 是核心校验原语

### DocGraph 实现洞见

19. **路径越界检测时机**
    - **先 `IsWithinWorkspace()` 检查原始路径，再 `Normalize()`**——否则越界信息丢失

20. **循环检测双集合模式**
    - `visited` + `inStack`（前者避免重复访问，后者检测当前路径循环）

21. **YamlDotNet 命名转换**
    - `UnderscoredNamingConvention` 将 camelCase 转为 snake_case

22. **produce 路径语义**（2026-01-07）
    - 路径相对于 **workspace root**，不是相对于源文件
    - `../docs/api.md` 作为 produce 路径是越界的（从 workspace root 开始计算）
    - `subdir/../docs/api.md` 是合法的（归一化后为 `docs/api.md`）

23. **TwoTierAggregator 抽象基类模式**
    - 子类只需实现：`FieldName`、`ResolvedFieldName`、`GlobalOutputPath`、`WishOutputFileName`、`ExtractItems()`
    - 共享逻辑：两级输出、Wish 归属推导、相对路径计算

### 通用实现技巧

24. **ref struct lambda 限制**
    - 测试异常需改用 try-catch 而非 FluentAssertions

25. **WeakReference GC 测试**
    - `[MethodImpl(NoInlining)]` + 三连 GC + `GC.KeepAlive` 放 Assert 后

26. **Activator.CreateInstance 与 internal 构造函数**
    - 需显式指定 `BindingFlags.NonPublic`

27. **API 分层设计**
    - Core API（非泛型）返回基类，Convenience API（类型化）提供泛型包装

### 协作模式洞见

28. **Recipe 改进实施规划**（2026-01-01）
    - 渐进式路径：Phase 0(基础设施) → Phase 1(结构对齐) → Phase 2(持续改进)
    - 分步执行降低复杂度——大变更分多个 PR，每个可独立回滚

29. **Wish 系统初始化实践**
    - 用系统定义系统本身是检验设计通用性的好方法
    - 条款编号前缀按功能领域分类便于查找

30. **Artifact-Adventures Beacon 写作实践**
    - "隐喻一句 + 工程一句"双句式——防止叙事过度游戏化
    - 词汇护栏（允许词/禁止词）是实用的团队协作工具

### AOS 实现路径规划（2026-01-05）

> 年会畅想后的实现路径思考。成熟度：Exploring。

31. **已有基础设施复用分析**

| 现有组件 | AOS用途 | 复用程度 |
|:---------|:--------|:---------|
| StateJournal | AOS Journal | 需适配层（Frame扩展） |
| DocGraph | 文档关系追踪 | 直接复用 |
| Atelia.Primitives | 错误处理 | 直接复用 |
| PipeMux | 进程间通信 | 可能用于Session隔离 |

32. **Week-1 MVP 路径**
    - 1个 Core Session + 2个 Cortex Session（Observer + Retriever）
    - Context Builder 纯函数 + Journal 适配层
    - 关键：`Observation.Nothing()` 实现自激振荡

33. **Frame 扩展设计**
    - `Provenance`（Craftsman）+ `ExperienceNote`（Curator）+ `DebugHint`
    - `ICortexSession` 接口实现可插拔

34. **验收条款→实现映射**

| 条款 | 实现 |
|:-----|:-----|
| 可启动 | `dotnet run -- aos start --ticks N` |
| 可回放 | `StateJournal.Replay()` |
| 可解释 | `Frame.Provenance` |
| 可控成本 | `Budget` 结构体 |
| 可插拔 | `ICortexSession` + DI |

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

4. **记忆维护核心原则**（2026-01-03）
   - "去重而非删减"——重复内容比冗长内容更损害认知效率
   - 识别重复信号：关键词重叠率>60%、相同主题多处表述

### 工具使用技巧

- **grep 先行**：任何批量替换前先用 grep 确认范围
- **parallel calls**：独立的读取操作可以并行
- **测试验证**：每次修改后运行相关测试
- **commit snapshot**：重大操作前先 git commit 作为备份

---

## 交付物索引

> 详细 Handoff 文件位于 `agent-team/handoffs/`，详细实现日志位于 `archive/members/implementer/`

| 时间 | 项目 | 主要交付 |
|------|------|----------|
| 2026-01 | Atelia.Primitives | 双类型架构重构（AteliaResult ref struct + AteliaAsyncResult），39 测试 |
| 2026-01 | DocGraph v0.1 | 93 测试通过，validate/fix/generate 命令 |
| 2025-12 | StateJournal M2 | 659 测试通过，完整二阶段提交 + Recovery |
| 2025-12 | Atelia.Primitives | AteliaResult/Error 体系，27 测试 |
| 2025-12 | PipeMux | SDK 模式迁移，管理命令 |

---

## 项目知识参考

### DocGraph

**文档体系**：
- 规范文档：`atelia/docs/DocGraph/v0.1/`
- 用法指南：`atelia/docs/DocGraph/v0.1/USAGE.md`

**v0.2 Wish Instance Directory 布局迁移**（2026-01-07）

| 变更 | 说明 |
|:-----|:------|
| DefaultWishDirectories | 从 `["wishes/active", "wishes/biding", "wishes/completed", "wish"]` 变为 `["wish"]` |
| Wish 识别规则 | v0.2 只识别 `wish/**/wish.md`，不再扫描旧布局 |
| Status 字段 | 从目录名推导改为从 frontmatter `status` 字段读取 |
| DocId 字段 | 从文件名推导改为从 frontmatter `wishId` 字段读取 |

**扩展点：创建新 Wish 实例目录**（2026-01-05）

| 位置 | 修改内容 |
|:-----|:---------|
| `wish/W-XXXX-slug/wish.md` | 主 wish 文件，frontmatter 含 wishId/title/status/produce |
| `wish/W-XXXX-slug/project-status/{goals,issues,snapshot}.md` | 状态寄存器，produce_by 指向 wish.md |
| `wish/W-XXXX-slug/artifacts/{Resolve,Shape,Rule,Plan,Craft}.md` | 分层产物，produce_by 指向 wish.md |
| 外部产物文档 | 在 produce_by 数组中追加新 wish.md 路径 |

**OutputPreflight 预检机制**（2026-01-05）

| 校验规则 | 说明 |
|:---------|:-----|
| 路径冲突检测 | 用 `HashSet<string>` 收集规范化后的所有输出路径 |
| 安全校验 | 拒绝绝对路径、`..` 穿越、归一化后不在 workspace 内 |
| 空 Dictionary 语义 | 等价于 null，回退单输出模式 |

**IssueAggregator Phase 2**（2026-01-07）

| 扩展点 | 说明 |
|:-------|:-----|
| Issue 类扩展 | 新增 `Id`, `SourceNode` 字段 |
| 双格式解析 | 字符串 `"X-ID: 描述"` + 对象 `{description, ...}` |
| 两层输出 | 全局 `docs/issues.gen.md` + Wish 级别 `project-status/issues.md` |
| Wish 归属 | 优先 `ProducedBy`，回退路径提取 |

**TwoTierAggregatorBase 基类抽取**（2026-01-05）

| 基类方法 | 说明 |
|:---------|:-----|
| `CollectAllItems()` | 从所有文档收集条目 |
| `GetOwningWishPath()` | 推导条目所属 Wish（ProducedBy 优先） |
| `GenerateGlobalOutput()` | 全局输出（按源文件分组子弹列表） |
| `GenerateWishOutput()` | Wish 级别输出 |

**扩展点：过滤 Abandoned Wish**（2026-01-06）

| 位置 | 修改内容 |
|:-----|:---------|
| `DocumentGraphBuilder.cs` Build 方法 (~L99) | 检查 `node.Status?.Equals("abandoned", ...)` 后跳过 |
| `DocumentGraphBuilderTests.cs` | 新增 `Build_ShouldFilterOutAbandonedWishes` 测试 |

- **Build 阶段过滤**：因为 RootNodes 从 allNodes 过滤得出，必须在 Build 阶段排除
- **闭包影响**：Abandoned Wish 的 produce 路径不入 pendingPaths 队列

**设计决策：输出格式重构**（2026-01-07）
- 表格 → 按源文件分组的子弹列表
- 全局输出标题：`# 问题汇总`，用 `## \`filepath\`` 分组
- ID 必填：字符串格式须匹配 `^([A-Z]-[A-Z0-9-]+):\s*(.+)$`

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

### Atelia.Data / SizedPtr（2026-01-06）

**代码位置**：
| 位置 | 说明 |
|:-----|:-----|
| `src/Data/SizedPtr.cs` | 38:26 bit 分配的 Fat Pointer 实现 |
| `tests/Data.Tests/SizedPtrTests.cs` | 50 个测试：roundtrip、对齐、边界、FromPacked、Contains |

**关键设计**：
- `FromPacked()` 不校验，任意 ulong 可解包
- `Create()/TryCreate()` 做完整校验（对齐+范围+溢出）
- `Contains()` 用差值比较避免溢出：`(position - offset) < length`
- `EndOffsetExclusive` 用 `checked` 算术

**测试注意**：所有 offset/length 参数必须 4B 对齐（0, 4, 8, ...），否则抛异常

**W-0006 文档修订**（2026-01-06）：
| 文件 | 修改项 |
|:-----|:-------|
| `rbf-interface.md` | §2.3 <deleted-place-holder>→SizedPtr+NullPtr、接口签名×4、示例×2、条款索引×3 |
| `rbf-format.md` | §1术语、§7重写（SizedPtr Wire Format）、§8 DataTail更新、条款索引×1 |

**条款更名**：`[F-<deleted-place-holder>-*]` → `[F-SIZEDPTR-*]` / `[F-RBF-NULLPTR]`；`RbfFrame.Address` → `RbfFrame.Ptr`

### Atelia.Primitives

**类型体系**：
```csharp
// 错误基类（abstract record，支持派生扩展）
public abstract record AteliaError(string Message, AteliaError? Cause = null);

// 同步层结果类型（ref struct，支持 ref struct 值）
public ref struct AteliaResult<T> where T : allows ref struct {
    public bool IsSuccess => _error is null;  // 从 _error 推导
    public T? Value { get; }
    public AteliaError? Error { get; }
}

// 异步层结果类型（readonly struct，可用于 Task/ValueTask）
public readonly struct AteliaAsyncResult<T> {
    public bool IsSuccess => _error is null;  // 从 _error 推导
    public T? Value { get; }
    public AteliaError? Error { get; }
}

// 异常桥接（实现 IAteliaHasError）
public class AteliaException : Exception, IAteliaHasError;
```

**设计要点**：
- 双类型架构：`AteliaResult<T>`（同步，支持 ref struct）+ `AteliaAsyncResult<T>`（异步）
- `IsSuccess` 从 `_error is null` 推导，不存储 bool
- 允许 `Success(null)`：区分"空结果"与"失败"
- `ToAsync()` 作为扩展方法，当 T 为 ref struct 时编译失败（期望行为）
- 删除 `Map`/`FlatMap`/`Match`：ref struct 不能用于委托
- 39 个测试用例覆盖

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

> 维护日志已压缩。详细历史见 `archive/members/implementer/`

- **2026-01-06**: Atelia.Primitives 双类型架构重构——`AteliaResult<T>` 改为 ref struct + 新增 `AteliaAsyncResult<T>`
- **2026-01-06**: W-0006 RBF/SizedPtr 文档修订——<deleted-place-holder>→SizedPtr 术语迁移、条款重命名、接口签名更新
- **2026-01-07**: DocGraph v0.2 实施——Wish 布局迁移 + IssueAggregator Phase 2 + TwoTierAggregatorBase 基类抽取
- **2026-01-03**: 记忆维护——去重、压缩历史、简化索引（575→330 行）
- **2026-01-01**: DocGraph v0.1 完成，Recipe 改进规划
- **2025-12-28**: StateJournal M2 完成（659 测试）
