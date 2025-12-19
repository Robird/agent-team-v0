# Implementer 认知索引

> 最后更新: 2025-12-19

## 我是谁
编码实现专家，负责根据设计进行代码实现、移植和修复。

## 我关注的项目
- [x] DocUI — 创建 demo/SystemMonitor 概念原型，展示动态 LOD
- [ ] DocUI MUD Demo — 验证 UI-Anchor 系统的综合演示
- [ ] PieceTreeSharp
- [x] PipeMux — 实现管理命令 `:list`, `:ps`, `:stop`, `:help`
- [ ] atelia-copilot-chat
- [x] DurableHeap — 设计文档修订（DurableDict ChangeSet 决策 + 术语一致性修正，共 14 轮）

## 当前关注

### DurableHeap 设计文档修订 Round 15 — B-6 新增"类型约束"章节 (2025-12-19)

根据监护人批示（B-6 任务），在 4.1 概念模型章节中新增 4.1.4 类型约束（Type Constraints）子节。

**背景**：
- DurableHeap 不是通用序列化库，应显式声明类型边界
- 这是设计约束，不是用户需要小心的"陷阱"

**修订内容**：
- 在 4.1.3 节末尾（Line 327）之后插入新的 4.1.4 节
- 明确支持的类型（基元值类型 + DurableObject 派生类型）
- 明确不支持的类型（任意 struct、用户自定义值类型、普通 class、泛型集合等）
- 说明运行时行为：赋值不支持类型时抛出明确异常（Fail-fast）

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 新增 4.1.4 节（约 17 行）

---

### DurableHeap 设计文档修订 Round 14 — Q11 移除"（推荐）"标记 (2025-12-19)

根据畅谈会质检（B-5 任务），移除 Q11 选项 A 的"（推荐）"标记，消除与决策表不一致的混淆。

**背景问题**：
- Q11 选项 A 标注"（推荐）"：`Upserts(key->value) + Deletes(keys)`
- 但决策表最终选择了 B：`仅 Upserts，删除通过 tombstone value 表达`
- 这会导致读者困惑

**修订内容**：
- Line 155: 移除选项 A 的"（推荐）"标记

**采用方案一的理由**：
- 决策表已有备注说明选择 B 的理由（"明显这样可以省掉一个集合和一次查找，实现起来更简单"）
- 移除更简洁，避免混淆

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 1 处修改

---

### DurableHeap 设计文档修订 Round 13 — 修复 Markdown 相对链接 (2025-12-19)

根据畅谈会质检（B-4 任务），修复第 6 节中 ChunkedReservableWriter.cs 的相对链接路径错误。

**背景问题**：
- 链接写成 `../atelia/src/Data/ChunkedReservableWriter.cs`
- 文档位于 `DurableHeap/docs/` 下，`../atelia` 会解析到 `DurableHeap/atelia`（不存在）
- 实际目录在仓库根的 `atelia/`

**修订内容**：
- Line 1023: `../atelia/...` → `../../atelia/...`（从 `DurableHeap/docs/` 回到仓库根再进入 `atelia/`）

**验证**：
- 从 `DurableHeap/docs/` 执行 `ls ../../atelia/src/Data/ChunkedReservableWriter.cs` 成功

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 1 处链接修复

---

### DurableHeap 设计文档修订 Round 12 — 统一 RecordKind/MetaKind 命名 (2025-12-19)

根据畅谈会共识（B-3 任务），将 Meta 文件中的 `MetaKind` 统一替换为 `RecordKind`。

**背景问题**：
- Data payload 用 `RecordKind`，Meta payload 用 `MetaKind`，命名不统一
- 同层不同名会导致读者误以为判定规则/扩展策略不同

**修订内容**：
1. Meta payload 最小字段中 `MetaKind` → `RecordKind`，并添加说明"Meta file 的 RecordKind"
2. MetaCommitRecord payload 解析中 `MetaKind == 0x01` → `RecordKind == 0x01`
3. 实现提示中 `MetaKind==0x01` → `RecordKind==0x01`

**统一规则**：
- `RecordKind` = 顶层类型判别（data 和 meta 文件都用这个名字）
- `ObjectKind` = 对象级 codec 判别（仅在 ObjectVersionRecord 内）

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 3 处 MetaKind 替换

---

### DurableHeap 设计文档修订 Round 11 — 术语表新增 EpochSeq 条目 (2025-12-19)

在术语表（Glossary）的"标识与指针"分组中新增 `EpochSeq` 条目。

**背景**：
- `EpochSeq` 是三大核心标识之一（与 ObjectId、ObjectVersionPtr 并列）
- 在 4.1.1 节有描述，但术语表中遗漏了

**修订内容**：
- 在"标识与指针"分组的 `ObjectVersionPtr` 之后新增 `EpochSeq` 行
- 定义：Commit 的单调递增序号，用于判定 HEAD 新旧
- 实现映射：`varuint`

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 术语表新增 1 行

---

### DurableHeap 设计文档修订 Round 10 — 术语表新增"编码层"分组 (2025-12-19)

在术语表（Glossary）中新增"编码层"分组，收录 RecordKind、ObjectKind、ValueType 三个编码类型标识术语：

**修订内容**：
- 在"载入与缓存"分组之后、"对象级 API（二阶段提交）"分组之前插入新的"编码层"分组
- 添加 3 个术语定义：
  - **RecordKind**: Record 的顶层类型标识，决定 payload 解码方式（`byte` 枚举）
  - **ObjectKind**: ObjectVersionRecord 内的对象类型标识，决定 diff 解码器（`byte` 枚举）
  - **ValueType**: Dict DiffPayload 中的值类型标识（`byte` 低 4 bit）

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 术语表新增 1 个分组（8 行）

---

### DurableHeap 设计文档修订 Round 9 — 4.4.5 Commit finalize 规范约束 (2025-12-19)

在 4.4.5 Commit(rootId) 章节增加规范约束，强调二阶段 finalize 语义：

**修订内容**：
- 在步骤 4/5 之后增加"规范约束（二阶段 finalize）"段落
- 明确：**对象级写入不得改变 Committed/Dirty 状态；只有 heap 级 commit 成功才能 finalize**
- 说明步骤 2 的 `WritePendingDiff()` 仅写入数据，不更新内存状态
- 说明步骤 5 的 finalize 必须在步骤 4 meta 落盘成功后执行
- 引用 4.4.4 的二阶段设计，保证语义一致性

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 4.4.5 节增加 1 个规范约束段落

---

### DurableHeap 设计文档修订 Round 8 — 4.4.4 二阶段提交拆分 (2025-12-19)

根据 commit point 语义修正需求，将 `FlushToWriter()` 拆分为两阶段 API：

**背景问题**：
- 原 `FlushToWriter()` 在写入成功后立即追平 `_committed = Clone(_current); _isDirty = false`
- 但实际 commit point 在 meta commit record durable，不在对象级写入
- 这会导致"假提交"状态：对象认为已提交但实际 commit 未确立

**修订内容**：

1. **术语表更新**（对象级 API 表格）：
   - 删除 `FlushToWriter` 定义
   - 新增 `WritePendingDiff`：Prepare 阶段，计算 diff 并序列化到 writer；不更新内存状态
   - 新增 `OnCommitSucceeded`：Finalize 阶段，追平内存状态

2. **4.4.4 节标题与说明**：
   - 标题改为"DurableDict 伪代码骨架（二阶段提交）"
   - 新增二阶段设计说明表格

3. **伪代码骨架重构**：
   - `FlushToWriter()` → `WritePendingDiff(writer)` + `OnCommitSucceeded()`
   - `WritePendingDiff` 只写数据，返回 bool 表示是否写入了新版本
   - `OnCommitSucceeded` 只追平内存状态，在 Heap 确认 meta 落盘后调用

4. **关键实现要点更新**：
   - 详细说明二阶段分离的崩溃安全性语义

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 4 处修改

根据术语表 Deprecated 标记（EpochMap → VersionIndex），对 mvp-design-v2.md 正文进行了术语一致性替换：

**修订内容**：
1. Line 111: `epoch map` → `VersionIndex`

**保留位置**：
- Line 36 术语表中的 `Deprecated: EpochMap` 保留，作为术语映射说明

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 1 处替换

---

### DurableHeap 设计文档修订 Round 6 — EpochRecord 术语替换 (2025-12-19)

根据术语表 Deprecated 标记（EpochRecord → Commit Record），对 mvp-design-v2.md 正文（特别是 Q4/Q5 决策选项区）进行了术语一致性替换：

**修订内容**：
1. Line 121: `EpochRecordPtr` → `CommitRecordPtr`
2. Line 121: `epoch record` → `Commit Record`
3. Line 122: `epoch record 可选` → `Commit Record 可选`
4. Line 124: `**Q5. EpochRecord 最少包含哪些信息？**` → `**Q5. Commit Record 最少包含哪些信息？**`

**保留位置**：
- Line 52 术语表中的 `Deprecated: EpochRecord（MVP）` 保留，作为术语映射说明

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 2 处替换（4 个 EpochRecord 相关术语实例）

---

### DurableHeap 设计文档修订 Round 5 — 术语畅谈会共识落地 (2025-12-19)

根据 2025-12-19 秘密基地畅谈会（DurableHeap MVP v2 术语与命名审阅）达成的共识，对 mvp-design-v2.md 进行了全面术语统一修订：

**修订内容**：

1. **添加术语表（Glossary）**：在文档开头（第 1 节之前）添加了规范化术语表 (SSOT)，包含：
   - 状态与差分（Working State / Committed State / ChangeSet / DiffPayload）
   - 版本链（Version Chain / Checkpoint Version / VersionIndex）
   - 标识与指针（ObjectId / Address64 / ObjectVersionPtr）
   - 提交与 HEAD（Commit / HEAD / Commit Record）
   - 载入与缓存（Identity Map / Dirty Set / LoadObject）
   - 对象级 API（FlushToWriter）

2. **状态术语统一**：
   - `Baseline`（单独使用）→ `Committed State`
   - `Current State`（作为概念名）→ `Working State`

3. **版本索引术语**：
   - `EpochMap` → `VersionIndex`（全文 15+ 处）
   - `EpochMapVersionPtr` → `VersionIndexPtr`

4. **快照术语**：
   - `snapshot`（版本链语境）→ `Checkpoint Version`
   - `DictSnapshotEveryNVersions` → `DictCheckpointEveryNVersions`

5. **指针术语**：
   - `Ptr64`（概念层）→ `Address64`

6. **加载 API**：
   - `Resolve`（外部 API 总称）→ `LoadObject`
   - 章节标题 `4.3.2 Resolve(ObjectId)` → `4.3.2 LoadObject(ObjectId)`
   - 章节标题 `4.1.2 Resolve 语义` → `4.1.2 LoadObject 语义`

7. **差分术语**：
   - `On-Disk Diff` → `DiffPayload`
   - 章节标题 `4.4.2 Dict 的 state diff` → `4.4.2 Dict 的 DiffPayload`
   - 章节标题 `4.2.5 ObjectVersionRecord（对象版本，增量 state diff）` → `4.2.5 ObjectVersionRecord（对象版本，增量 DiffPayload）`

8. **提交相关**：
   - `EpochRecord` → `Commit Record`（逻辑概念）
   - 章节标题 `4.2.3 EpochRecord（逻辑概念）` → `4.2.3 Commit Record（逻辑概念）`
   - `head` / `Head` → `HEAD`（全文统一大写）

9. **对象级方法**：
   - 伪代码 `Commit()` → `FlushToWriter(writer)`
   - 关键实现要点从 `Commit 中途失败` → `FlushToWriter 中途失败`

10. **缓存术语**：
    - `identity map` → `Identity Map`（Title Case）
    - `_dirtySet` → `Dirty Set`（概念层）
    - `epoch map lookups` → `VersionIndex lookups`

11. **补充定义**：
    - 在 4.1.0 新增 Identity Map 正式定义块
    - 在 4.1.0 新增 Dirty Set 正式定义块

**文件变更**：
- `DurableHeap/docs/mvp-design-v2.md` — 60+ 处术语替换

根据 DocUIGPT 第三轮质检反馈，对 mvp-design-v2.md 进行了 3 处术语一致性最终修订：

**修订内容**：
1. **4.4.1 节标题修正**：
   - "##### 三层语义术语定义（MVP 固定）" → "##### 语义层次定义（MVP 固定）"
   - 原因：内容实际定义了四层（Working State / Committed State / ChangeSet / On-Disk Diff），标题"三层"与内容不符

2. **小写 "committed state" 统一**（共 6 处）：
   - Line 200: `committed state（例如一个内存 dict）` → `Committed State（Baseline）（例如一个内存 dict）`
   - Line 208: `committed state（materialize 的结果）` → `Committed State（Baseline）（materialize 的结果）`
   - Line 248: `materialize 的 committed state（`_committed`）` → `materialize 的 Committed State（Baseline）（`_committed`）`
   - Line 250: `该缓存不属于 committed state` → `该缓存不属于 Committed State（Baseline）`
   - Line 559: `该对象的 committed state（其中对象引用` → `该对象的 Committed State（Baseline）（其中对象引用`
   - Line 755: `对任意 committed state $S$` → `对任意 Committed State（Baseline） $S$`

3. **"state diff" 术语说明**（开头摘要）：
   - Line 13: `**state diff**（为了查询快、实现简单）` → `**state diff**（即 On-Disk Diff；为了查询快、实现简单）`
   - 在首次出现时明确 state diff 等价于 On-Disk Diff

根据 DocUIGPT 第二轮质检反馈，对 mvp-design-v2.md 进行了 3 处术语一致性修正：

**修订内容**：
1. **文档开头"当前已达成共识"列表**：
   - "反序列化后的 committed state" → "反序列化后的 Committed State（Baseline）"
   - 与正文术语定义保持一致

2. **4.1.0 的 Materialize 定义**：
   - "合成为当前可读的 committed state" → "合成为当前可读的 **Committed State（Baseline）**"
   - 术语加粗、格式一致

3. **4.4.1 术语定义的引导句**：
   - "将**内存中的状态**明确区分为以下四层" → "将**状态与差分表达**明确区分为以下四层"
   - 因为第 4 条 On-Disk Diff 不属于"内存中的状态"，改为更准确的表述

### DurableHeap 设计文档修订 Round 2 (2025-12-19)

根据 DocUIGPT 质检反馈，对 mvp-design-v2.md 进行了第二轮修订，修正了 4 个术语精确性问题：

**修订内容**：
1. **"内存态"全局替换**：将文档中除术语映射以外的"内存态"替换为精确术语
   - Line 11: "反序列化后的内存态" → "反序列化后的 committed state"
   - Line 237: "不从磁盘覆盖内存态" → "不从磁盘覆盖 Working State（`_current`）"
   - Line 248: "内存态表示" → "进程内对象实例表示"
   - Line 641: "内存态：使用哨兵对象" → "ChangeSet 内部使用哨兵对象"

2. **术语定义修正**（4.4.1）：
   - 移除"Materialized State"括注，改为"Current State"
   - 新增独立的"Committed State（已提交状态 / Baseline）"定义
   - 明确 Materialize 输出为 Committed State
   - 术语映射改为更精确的对应

3. **ChangeSet 描述修正**（4.4.1）：
   - "每个内存对象维护一个 ChangeSet" → "每个内存对象具有 ChangeSet 语义（可为显式结构或隐式 diff 算法）"
   - 与方案 C 的实现口径一致

4. **NoChange 编码说明**（4.4.2）：
   - 补充说明"`NoChange` 通过 diff 中缺失该 key 表达，不在 payload 中编码"
   - 与 Canonical Diff 约束一致

### DurableHeap 设计文档修订 (2025-12-19)

根据畅谈会决策（2025-12-19-durabledict-changeset-jam.md），修订了 mvp-design-v2.md 文档：

**修订内容**：
1. 修改 4.4.1 节：新增三层语义术语定义（Working State / ChangeSet / On-Disk Diff）
2. 修改 4.4.2 节：
   - 添加方案 C（双字典）实现说明
   - 用三层术语替换歧义的"内存态"措辞
   - 澄清 tombstone 仅在 diff 计算与序列化阶段出现
3. 新增 4.4.3 节：DurableDict 不变式与实现规范
   - 8 条核心不变式（MUST）
   - 4 条实现建议（SHOULD）
4. 新增 4.4.4 节：DurableDict 伪代码骨架
   - 完整的读/写/生命周期 API
   - ComputeDiff 和 Clone 内部方法
5. 原 4.4.3 Commit(rootId) 重命名为 4.4.5

**关键决策落地**：
- 方案选择：方案 C（双字典）
- Q1: _committed 更新时机 → Clone（深拷贝）
- Q2: dirty tracking → _isDirty flag + HasChanges 属性
- Q3: 新增后删除 → 不写记录（Canonical Diff）

### DocUI MUD Demo 技术评估 (2025-12-15)

参与了 MUD Demo 秘密基地畅谈，对 DocUI 技术状态进行了评估：

**已实现的底层组件**：
- `SegmentListBuilder` — 文本段操作
- `OverlayBuilder` — 渲染期叠加标记
- `StructList<T>` — 高性能容器

**设计完成但未实现**：
- UI-Anchor 系统 (Object-Anchor, Action-Link, Action-Prototype)
- AnchorTable（锚点注册表）
- `run_code_snippet` tool
- Micro-Wizard

**MVP 建议分阶段**：
- MVP-0 (2-3天): Static Demo — 能生成带 UI-Anchor 标记的 Markdown
- MVP-1 (3-4天): Functional Demo — AnchorTable + 简单执行
- MVP-2 (3-4天): Interactive Demo — Micro-Wizard + TextField

**技术风险**：
1. Roslyn 解析复杂性 → 建议 MVP 用正则手写解析
2. 状态同步混乱 → 建议简单 GameState 类
3. 过度设计 → 先人玩，再 Agent 玩

## 最近工作

### 2025-12-10: SystemMonitor 概念原型

**任务**：创建展示动态内容 LOD 的概念原型

**交付物**：
1. `DocUI/demo/SystemMonitor/SystemMonitor.csproj` — 项目文件
2. `DocUI/demo/SystemMonitor/Program.cs` — PipeMux.SDK 入口 + 命令定义
3. `DocUI/demo/SystemMonitor/Model/` — 数据模型 (LodLevel, SystemStatus, ResourceMetrics)
4. `DocUI/demo/SystemMonitor/Collectors/MetricsCollector.cs` — 模拟数据收集器
5. `DocUI/demo/SystemMonitor/Rendering/MonitorRenderer.cs` — 按 LOD 级别渲染

**LOD 设计**：
- `[GIST]` — 一行关键指标：`System ✓ OK | CPU 23% | Mem 4.2/16GB | Disk 45%`
- `[SUMMARY]` — 表格摘要 + Top 3 进程
- `[FULL]` — 完整详情（CPU/Memory/Disk/进程表）

**命令语法**：
- `pmux monitor view [--lod gist|summary|full]` — 查看系统状态
- `pmux monitor cpu [--lod ...]` — 只看 CPU
- `pmux monitor memory [--lod ...]` — 只看内存
- `pmux monitor disk [--lod ...]` — 只看磁盘
- `pmux monitor processes [--top N] [--lod ...]` — 查看进程
- `pmux monitor set-lod <level>` — 设置默认 LOD

**测试结果**：
- Build: ✅ `dotnet build -c Release` 成功
- `pmux monitor view`: ✅ SUMMARY 级别正常
- `pmux monitor view --lod gist`: ✅ 一行摘要正常
- `pmux monitor view --lod full`: ✅ 完整详情正常
- 子命令 (cpu/memory/disk/processes): ✅ 全部正常

**Handoff**: `agent-team/handoffs/SystemMonitor-IMP.md`

### 2025-12-10: TextEditor 迁移到 PipeMux.SDK

**任务**：将 TextEditor 从手动 JSON-RPC 循环迁移到 `PipeMuxApp` + `System.CommandLine` 模式

**交付物**：
1. 修改 `DocUI.TextEditor.csproj` — 引用 `PipeMux.Sdk` 替代 `PipeMux.Shared`
2. 重写 `Program.cs` — 使用 `PipeMuxApp` 和 `System.CommandLine`
3. 重构 `EditorSession.cs` — 移除 Protocol 依赖，返回纯字符串
4. 删除 `TextEditorService.cs` — 命令逻辑合并到 Program.cs
5. 更新 `~/.config/pipemux/broker.toml` — 添加 texteditor 应用配置

**命令语法**：
- `pmux texteditor open <path>` — 打开文件
- `pmux texteditor goto-line <line>` — 跳转到指定行
- `pmux texteditor select <startLine> <startCol> <endLine> <endCol>` — 选区（未实现）
- `pmux texteditor render` — 重新渲染

**测试结果**：
- Build: ✅ `dotnet build -c Release` 成功
- pmux open: ✅ 正常工作
- pmux goto-line: ✅ 状态保持正常（Session ID 一致）
- pmux render: ✅ 正常工作
- pmux select: ✅ 预期错误（NotImplementedException）

**Handoff**: `agent-team/handoffs/TextEditor-SDK-Migration-IMP.md`

### 2025-12-09: PipeMux 管理命令实现

**任务**：为 PipeMux 添加管理命令支持

**交付物**：
1. `ManagementCommand.cs` — 命令类型枚举和解析逻辑
2. `Request.cs` — 添加 ManagementCommand 字段
3. `Program.cs` — CLI 入口点检测 `:` 前缀
4. `BrokerClient.cs` — 添加 SendManagementCommandAsync
5. `ManagementHandler.cs` — Broker 端命令处理器
6. `BrokerServer.cs` — 集成 ManagementHandler

**测试结果**：
- Build: ✅ PipeMux.sln 成功
- Build: ✅ DocUI.sln 成功（兼容性验证）

**Handoff**: `agent-team/handoffs/PipeMux-Management-Commands-IMP.md`

### 2025-12-09: 修复 DocUI demo/TextEditor 项目引用

**任务**：修复跨项目演示的引用路径问题

**交付物**：
1. 修复 `DocUI.TextEditor.csproj` 的项目引用
   - 添加 `DocUI.Text` 引用 (同 repo)
   - 更正 `TextBuffer` 引用路径 (PieceTreeSharp)
   - 更正 `PipeMux.Shared` 引用路径 (PipeMux)
2. 将项目添加到 `DocUI.sln` 的 demo 文件夹
3. 修复代码兼容性问题 (`Request.Command` → `Request.Args[0]`)

**测试结果**：
- Build: ✅ 成功 (6 个项目)
- Test: ✅ 24/24 通过

**注意**：原任务要求引用 `PipeMux.Sdk`，但代码使用的是 `PipeMux.Shared.Protocol` API，因此引用了 `PipeMux.Shared`。
