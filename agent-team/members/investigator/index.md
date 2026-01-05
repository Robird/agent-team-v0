# Investigator 认知索引

> 最后更新: 2026-01-05
> - 2026-01-05: DocGraph 代码调查（Visitor 扩展机制、produce 验证路径、7 条便签）
> - 2026-01-04: Memory Palace — 处理了 3 条便签（SizedPtr/RBF/Address64 调查锚点）
> - 2026-01-01: workspace_info 机制调查（Copilot Chat Agent Prompt System）
> - 2025-12-27: ObjectLoaderDelegate 重构影响分析
> - 2025-12-27: Workspace/ObjectLoader/RBF 设计意图调查
> - 2025-12-27: Memory Palace — 处理了 1 条便签（_removedFromCommitted 设计洞见）
> - 2025-12-25: Memory Palace — 处理了 1 条便签（历史决策引用分析）
> - 2025-12-24: Memory Palace — 处理了 1 条便签（术语别名调研）
> - 2025-12-24: Memory Palace — 处理了 1 条便签

## 我是谁
源码分析专家，负责分析源码并产出实现 Brief。

## 我关注的项目
- [x] PieceTreeSharp - 已调查 2025-12-09
- [x] DocUI - 已调查 2025-12-09
- [x] PipeMux - 已调查 2025-12-09
- [x] atelia/prototypes - 已调查 2025-12-09
- [ ] atelia-copilot-chat

## Session Log

### 2026-01-05: DocGraph 代码调查
**任务**: Wish-0007 相关的 DocGraph 源码调查，定位 Visitor 扩展点和 produce 验证机制
**关键发现**:

#### 1. Visitor 扩展机制
| 类型 | 位置 | 备注 |
|:-----|:-----|:-----|
| **扩展入口** | `RunCommand.cs#L95-L101` | `GetVisitors()` 硬编码列表 |
| **frontmatter 字段** | `GlossaryVisitor.cs#L93-L103` | `KnownFrontmatterFields` 静态类 |
| **Wish 归属推导** | `DocumentNode.ProducedBy` | 比路径正则更健壮 |

#### 2. produce 验证 → 空文件创建路径
- `DocumentGraphBuilder.cs#L402` — `ValidateProduceRelations()` 检测文件不存在
- `DocumentGraphBuilder.cs#L424` — 添加 `CreateMissingFileAction`
- `CreateMissingFileAction.cs#L89` — `Execute()` 写入模板内容
- **关键问题**: 不区分"手动维护"和"自动生成"的产物文件

#### 3. 多输出 Visitor 实现路径
1. `IDocumentGraphVisitor.cs` — 接口扩展点
2. `RunCommand.cs#L147` — `GetVisitors()` 注册入口
3. `RunCommand.cs#L104-L130` — Visitor 执行循环
- **建议**: 接口扩展 `GenerateMultiple()` 而非拆分 Visitor 类

#### 4. Gotcha 陷阱
| 陷阱 | 后果 | 规避 |
|:-----|:-----|:-----|
| IssueAggregator 已存在 | 重复造轮子 | W-0007 应改为"扩展"而非"新建" |
| produce 声明 vs Visitor 输出路径不一致 | fix 阶段用空模板覆盖手动文件 | produce 只声明 `.gen.md` 路径 |

**置信度**: ✅ 全部验证过

### 2026-01-04: SizedPtr/RBF/Address64 现状调查
**任务**: Wish-0004 SizedPtr 设计调查，定位权威定义和代码现状
**关键发现**:
1. **Address64 权威定义位置**：
   - 位置: `atelia/docs/Rbf/rbf-interface.md#2.3`
   - 条款: `[F-ADDRESS64-DEFINITION]`, `[F-ADDRESS64-ALIGNMENT]`, `[F-ADDRESS64-NULL]`
   - 源码实现已归档: `atelia/archive/2025-12-29-rbf-statejournal-v1/Rbf/Address64.cs`
2. **RBF 层代码状态**：
   - 搜索 `atelia/src/**` 无任何 Address64/Rbf/SizedPtr 匹配
   - RBF 层实现已整体归档到 `atelia/archive/2025-12-29-rbf-statejournal-v1/`
   - **结论**: SizedPtr 需从零开始在 `atelia/src/Data/` 实现
3. **Null 语义冲突（Gotcha）**：
   - Wish-0004 非目标写"不定义特殊值"，但 Address64 定义了 `Null=0`
   - **风险**: 若不澄清，上层 RBF 接口迁移时会卡住
   - **建议**: 在 Shape-Tier 明确立场——SizedPtr 保持纯净，Null 由 RBF 层自行包装

### 2026-01-01: workspace_info 机制调查
**任务**: 分析 VS Code Copilot Chat 中 workspace_info 的生成机制
**关键发现**:
1. **组成结构**：`workspace_info` 是 `GlobalAgentContext` 的子组件，包含 Tasks、FoldersHint、WorkspaceStructure 三部分
2. **深度控制**：无显式深度限制，由 `maxSize=2000` 字符预算和 BFS 算法共同决定
3. **生成算法**：`visualFileTree.ts` 实现广度优先展开，空间不足时添加 `...` 截断
4. **排序规则**：文件在前目录在后，同类型按名称排序
5. **过滤机制**：遵循 `.gitignore`、Copilot Ignore、排除点文件（默认）
6. **缓存策略**：首轮渲染后缓存到 Turn Metadata，后续轮次复用
7. **条件渲染**：仅在 `list_dir` 工具可用时渲染目录结构
**实际意义**: 将 recipe 移到根目录可提高其在 workspace_info 中的可见性（更短路径 = 更高优先级）
**交付**: [handoffs/2026-01-01-workspace-info-mechanism-INV.md](../handoffs/2026-01-01-workspace-info-mechanism-INV.md)

### 2025-12-27: Storage Engine M1 风险分析
**任务**: 调查 StateJournal + Rbf 现状，识别 M1 阶段高风险项
**关键发现**:
1. **RbfScanner 全量内存读取**：当前 `RbfScanner(ReadOnlyMemory<byte> data)` 把整个文件读入内存，对 GB 级仓库不可行。M1 必须重构为流式/分块读取
2. **Durable flush 抽象缺失**：`IRbfFramer.Flush()` 按设计只推缓冲到下层，fsync 由上层自理——但当前没有暴露底层句柄的途径
3. **建议方案**：引入 `FileBackedBufferWriter` 和 `FileBackedRbfScanner`，内部持有 `SafeFileHandle`，暴露 `FlushToDisk()` 方法
**深层洞见**: "接口设计正确但实现层缺失"的典型案例——接口预留了扩展点（`IBufferWriter<byte>` 注入），但 MVP 只实现了内存版本

### 2025-12-27: Workspace/ObjectLoader/RBF 设计意图调查
**任务**: 分析 StateJournal 设计文档，提取 Workspace、ObjectLoader、RBF 的设计意图
**关键发现**:
1. **Workspace 定位**：类比 Git working tree，是核心协调器（Identity Map、Dirty Set、HEAD 追踪、Commit 协调）
2. **ObjectLoader 是内部实现**：不是独立组件，LoadObject 流程定义在 Workspace 内部
3. **四阶段读取模型**：Deserialize → Materialize（Shallow）→ LoadObject → ChangeSet
4. **RBF 层级关系**：RBF 是 Layer 0，提供二进制帧封装；StateJournal 是 Layer 1，定义 Record 语义
5. **护照模式**：每个对象 MUST 绑定一个 Owning Workspace，绑定不可变
6. **分层 API 设计**：Layer 1（构造函数）→ Layer 2（工厂）→ Layer 3（可选 Ambient）
**交付**: [handoffs/2025-12-27-workspace-objectloader-rbf-investigation-INV.md](../handoffs/2025-12-27-workspace-objectloader-rbf-investigation-INV.md)
**待确认问题**:
- B-8: LoadObject<T> 是否应拆分为非泛型底层 + 泛型包装？

### 2025-12-26: `_removedFromCommitted` 集合必要性分析
**任务**: 调查 DurableDict 中 `_removedFromCommitted` 集合是否多余
**关键发现**:
1. **不是 Materialize 的问题**：加载时 `_committed` 是最终状态，`_removedFromCommitted` 初始为空
2. **运行时状态管理的副产品**：双字典策略要求 `_committed` 在 Commit 前只读，Remove 操作无法直接修改，只能用集合记录删除意图
3. **符合规范条款**：`[S-WORKING-STATE-TOMBSTONE-FREE]` 要求 Working State 无 tombstone，当前实现用集合而非 tombstone 值满足约束
4. **替代设计存在**：改为单一 `_current` 合并视图可消除该集合，但需要重构读写路径
**结论**: 设计上可以消除，但当前架构下有其存在理由。保持现有设计，考虑长期重构。
**交付**: [handoffs/2025-12-26-removedFromCommitted-analysis-INV.md](../handoffs/2025-12-26-removedFromCommitted-analysis-INV.md)
**深层洞见** (2025-12-26 补充):
- **设计权衡本质**：双字典策略的核心约束是"_committed 在 Commit 前只读"。带来 Commit 失败时恢复简单的好处，代价是需要 `_removedFromCommitted` 追踪删除意图
- **规范与实现的巧妙契合**：`[S-WORKING-STATE-TOMBSTONE-FREE]` 用集合（而非 tombstone 值）实现——隐晦但有效
- **监护人意见精确定位**：意见针对 Load/Materialize 阶段，但实际问题在运行时状态管理；加载时 `_committed` 确实是最终状态

### 2025-12-24: mvp-design-v2.md 历史决策引用分析
**任务**: 分析 mvp-design-v2.md 中的历史决策引用情况
**关键发现**:
1. Qxx 引用共 15 处，涉及 13 个不同决策（Q3/Q7-Q11/Q13-Q19/Q22-Q23）
2. "方案 X" 引用全部是"方案 C"（双字典），共 8 处
3. 方案 C 出现在术语表、实现描述、伪代码三个层面
**潜在问题**:
- 部分引用没有 "=Y" 后缀（如 Q15、Q17），需要查 decisions 才知道选了什么
- 方案 C 没有对应的 Qxx 编号，是独立的实现方案选择
**交付**: [handoffs/2025-12-24-mvp-design-v2-decision-refs-INV.md](../handoffs/2025-12-24-mvp-design-v2-decision-refs-INV.md)

### 2025-12-24: 术语别名调研任务完成
**任务**: 调研 StateJournal 目录下弃用/别名术语的使用情况
**发现**:
1. 所有出现均在术语表定义或正式术语旁的注释说明中
2. 没有发现需要替换的"误用"情况
3. archived 文件夹中的历史文档包含相同的定义，无需处理
**结论**: 术语使用情况良好，无需修改

### 2025-12-24: RBF v0.12 格式变更对上层文档的影响
**任务**: 完成 mvp-design-v2.md 对 RBF v0.12 变更的适配
**关键发现**:
1. **墓碑机制分离**：旧设计中 `FrameTag=0x00000000 (Padding)` 承担墓碑语义，现在墓碑完全由 `FrameStatus=0xFF (Tombstone)` 承载。职责分离——FrameStatus 管 Layer 0 帧有效性，FrameTag 管 Layer 1 业务分类
2. **StateJournal 处理顺序变更**：上层 Reader 现在 MUST 先检查 `FrameStatus`，再解释 `FrameTag`。比之前"检查 FrameTag=0 就跳过"更清晰
3. **mvp-test-vectors.md 无需更新**：正确地将 Layer 0 测试委托给 rbf-test-vectors.md，Layer 1 测试不涉及帧格式细节
4. **术语演化轨迹**：Magic → Fence (v0.10) → Pad → FrameStatus (v0.12)。每次重命名都反映更精确的语义理解

### 2025-12-23: 记忆积累机制反思畅谈会（第二波）
**任务**: 参与团队记忆机制反思，从 Investigator 视角提供建议
**参与文件**: agent-team/meeting/2025-12-22-memory-accumulation-reflection.md
**贡献要点**:
1. 分析 Investigator 记忆精简（286 行）的原因：系统提示词设计 + handoffs 外置机制
2. 提出"过程产物外置"模式的推广建议：详情在 handoff，index.md 只放指针
3. 评估参谋组框架：四种写入动作适用，但需要区分"调查中"vs"调查完成"状态
4. 提出知识传递反模式：避免 Investigator Brief 和 Implementer 记录双重记录
**发言位置**: agent-team/meeting/2025-12-22-memory-accumulation-reflection.md（Investigator 发言第二波）

### 2025-12-21: DurableHeap → StateJournal 更名迁移
**任务**: 响应团队通知，更新认知文件中的 DurableHeap 引用
**变更**:
- 项目已从 `DurableHeap` 正式更名为 `StateJournal`
- 新路径: `atelia/docs/StateJournal/`
- 命名空间: `Atelia.StateJournal`
- 核心文档: `atelia/docs/StateJournal/mvp-design-v2.md`
**更新内容**:
- Key Deliverables: 修订清单描述更新
- Open Investigations: 项目名称更新
- Session Log: 4 处标题/任务描述更新（2025-12-20, 2025-12-17, 2025-12-16）
- 会议文件引用路径更新（durableheap-mvp-review → statejournal-mvp-review）
**命名由来**: "State" = Agent 状态持久化用例，"Journal" = 追加写入 + 版本可回溯

### 2025-12-09: PieceTreeSharp 项目现状核实
**任务**: Team Leader 需要核实 PieceTreeSharp 的实际状态，特别是核心/外围的边界
**发现**:
1. **项目结构与旧 Wiki 有差异**：
   - 旧 Wiki 描述的 `PieceTree/`、`Model/`、`Find/` 目录不存在
   - 实际结构是 `Core/`（核心）+ 多个外围目录
2. **实际目录结构（src/TextBuffer/ 下）**：
   - `Core/` (20 files) — Piece Tree 核心：PieceTreeModel, Builder, Snapshot, Searcher, SearchCache 等
   - `Cursor/` (15 files) — 光标/多光标/词操作/Snippet Session
   - `Decorations/` (8 files) — 装饰系统、IntervalTree
   - `Diff/` (15+ files) — 差异算法（含 Algorithms/ 子目录：Myers, DP）
   - `DocUI/` (5 files) — FindModel, FindDecorations, FindReplaceState
   - `Rendering/` (3 files) — MarkdownRenderer
   - `Services/` (2 files) — ILanguageConfigurationService, IUndoRedoService
   - `Snippet/` (1 file) — Transform.cs
   - 顶层：TextModel.cs, PieceTreeBuffer.cs, EditStack.cs 等
3. **核心 vs 外围边界明确**：
   - 🟢 核心层：Core/ + TextModel.cs + PieceTreeBuffer.cs + EditStack.cs（数据结构层）
   - 🟡 外围层：Cursor, Decorations, Diff, DocUI, Rendering, Services, Snippet（功能层）
4. **测试基线确认**：1158 passed, 9 skipped, 1167 total
5. **文档位置**：docs/（含 plans/, reports/, sprints/ 等子目录）
6. **其他目录**：tools/（Python 脚本）, BugRepro/（空）, PortingDrafts/（移植草稿）

**更新**: [wiki/PieceTreeSharp/README.md](../../wiki/PieceTreeSharp/README.md) - 全面重写，修正目录结构，明确核心/外围边界

### 2025-12-09: atelia/prototypes 项目现状核实
**任务**: Team Leader 需要核实 atelia/prototypes 的实际状态
**发现**:
1. **项目结构与 Wiki 有多处差异**：
   - Wiki 描述 "三层 LLM 调用模型" 概念上正确但映射文件有误
   - 无 OpenAI Provider 实现（Wiki 误报存在）
   - LiveContextProto 位于 prototypes 下，有自己的 README
2. **实际子项目（5 个）**：
   - `Completion.Abstractions/` - ICompletionClient, IHistoryMessage, ToolDefinition 等
   - `Agent.Core/` - AgentEngine (状态机), MethodToolWrapper, History/, Tool/, App/
   - `Completion/` - 仅 Anthropic Provider（含 AnthropicClient, MessageConverter, StreamParser）
   - `Agent/` - 应用层，含 CharacterAgent, Apps/, SubAgents/, Text/
   - `LiveContextProto/` - 控制台原型 TUI
3. **关键修正**：
   - Tool.cs 应改为 ToolDefinition.cs（且不是接口而是 record）
   - AgentEngine 的事件驱动模型详见源码（WaitingInput, BeforeModelCall, AfterToolExecute 等）
   - History 目录含 AgentState.cs, HistoryEntry.cs, RecapBuilder.cs 等

**更新**: [wiki/atelia-prototypes/README.md](../../wiki/atelia-prototypes/README.md) - 全面重写

### 2025-12-09: PipeMux 项目现状核实
**任务**: Team Leader 需要核实 PipeMux 项目的实际状态
**发现**:
1. 项目结构与 wiki 基本一致，补充了 `samples/TerminalIdTest`, `tools/TerminalIdTest`, `tests/` 目录
2. P1 问题（关闭/列出 app）- `ProcessRegistry` 已实现 `Close()` 和 `ListActive()` 方法，但 Broker 未暴露给 CLI
3. Calculator 示例支持完整 RPN 命令：push, pop, peek, dup, swap, clear, add, sub, mul, div, neg
4. 终端标识检测覆盖全面：env 覆盖、VS Code (IPC UUID/WSL)、Windows Terminal、传统 Windows、Unix TTY/SID

**更新**: [wiki/PipeMux/README.md](../../wiki/PipeMux/README.md) - 增加了核心组件详解、Calculator 命令表、已知问题代码现状列

## Key Deliverables
- **handoffs/2026-01-01-workspace-info-mechanism-INV.md (2026-01-01)** - workspace_info 机制调查，含目录树生成算法、深度控制、缓存机制分析
- **handoffs/2025-12-27-objectloaderdelegate-refactoring-analysis-INV.md (2025-12-27)** - ObjectLoaderDelegate 重构影响分析，含使用点清单、测试 Mock 方式、IRbfScanner 状态评估
- **handoffs/2025-12-27-workspace-objectloader-rbf-investigation-INV.md (2025-12-27)** - Workspace/ObjectLoader/RBF 设计意图调查，含关键条款引用和实现建议
- **handoffs/2025-12-24-mvp-design-v2-decision-refs-INV.md (2025-12-24)** - mvp-design-v2.md 历史决策引用分析，15 处 Qxx 引用 + 8 处方案 C 引用
- wiki/PieceTreeSharp/README.md (2025-12-09) - 源码核实全面重写，修正目录结构
- wiki/atelia-prototypes/README.md (2025-12-09) - 源码核实全面重写
- wiki/PipeMux/README.md (2025-12-09) - 源码核实更新
- wiki/DocUI/README.md (2025-12-09) - 源码核实全面重写，明确实际代码现状
- **handoffs/2025-12-20-mvp-design-v2-revision-brief-INV.md (2025-12-20)** - StateJournal MVP v2 修订清单，7 个 P0 问题的具体修改位置

## Open Investigations
- [x] StateJournal MVP — 修订清单已产出 (2025-12-20)

### 2025-12-20: StateJournal MVP v2 修订清单
**任务**: 基于 2025-12-19 畅谈会第四轮共识，分析 mvp-design-v2.md 需要修改的具体位置
**共识来源**: `agent-team/meeting/2025-12-19-statejournal-mvp-review.md`（第四轮共识）
**发现**:
1. **P0-1 `_isDirty` → `_dirtyKeys`**：9 个修改点，涉及术语表、实现方案描述、伪代码骨架
2. **P0-2 Magic 结构定义**：5 个修改点，将 Magic 从 record header 改为 record separator
3. **P0-3 `DataTail` 定义**：2 个修改点，明确 DataTail = EOF（包含尾部 Magic）
4. **P0-4 Value 类型收敛**：2 个修改点，MVP 仅支持 null/varint/ObjRef/Ptr64
5. **P0-5 Dirty Set 卡住**：由 P0-1 解决，无额外修改点
6. **P0-6 Commit API 命名**：3 个修改点，改为 `CommitAll(newRootId:)`
7. **P0-7 首次 commit 语义**：2 个修改点，明确空仓库 Epoch=0、NextObjectId=1
**关键技术点**:
- `_dirtyKeys` 实现：每次 Set/Delete 时比较当前值与 committed 值，维护 dirty key 集合
- Magic-as-Separator：Record 不包含 Magic，文件结构为 `[Magic][R1][Magic][R2]...[Magic]`
- reverse scan 算法修正：`MagicPos = RecordStart - 4` 定位前一个分隔符

**交付**: [agent-team/handoffs/2025-12-20-mvp-design-v2-revision-brief-INV.md](../handoffs/2025-12-20-mvp-design-v2-revision-brief-INV.md)

### 2025-12-17: StateJournal 写入路径设计畅谈
**任务**: 调研内存对象→持久化对象的"透明迁移"前人经验
**调研范围**:
1. CLR GC 对象重定位机制（Marking→Relocating→Compacting）
2. 数据库 MemTable→SSTable 刷盘（LevelDB/RocksDB WAL）
3. 内存映射数据库 COW 语义（LMDB/MDBX B+ Tree）
4. ORM 脏追踪与 Identity Map（SQLAlchemy Unit of Work, Hibernate 状态机）
5. 函数式持久化数据结构（Structural Sharing, HAMT, Path Copying）
6. GC 前向指针技术（Cheney 算法, Shenandoah forwarding pointer）
**核心发现**:
- 关键抽象：Handle/Slot 间接层 + 状态机（Transient→Pending→Durable）
- LSM-Tree 的"不可变 + 分层刷盘"思路契合 append-only 设计
- ORM 的 Identity Map 可参考用于保证"同一 Ptr 返回同一 wrapper"
- 前向指针技术启发：commit 时原位更新内部 Slot 指针

**发言位置**: agent-team/meeting/2025-12-17-durable-heap-write-path-jam.md

### 2025-12-16: Tool-As-Command 秘密基地畅谈
**任务**: 参加 Tool-As-Command 设计畅谈，从现有代码实现层面提供技术见解
**参考代码**: `atelia/prototypes/Agent.Core` — AgentEngine.cs, ITool.cs, AgentPrimitives.cs
**贡献要点**:
1. **现有代码锚点识别**：
   - `BeforeToolExecute`/`AfterToolExecute` 事件已是拦截器，可注入 Command 逻辑
   - `_pendingToolResults` 字典可扩展为 Command 状态存储
   - `DetermineState()` 可扩展或复用现有 `WaitingInput` 状态
2. **ITool 最小扩展方案**：不改接口，用返回值变体（`Yielded` 状态）表示 yield
3. **极简序列化格式**：5 字段种子（cmd_id, tool_call_id, node, data, prompt）
4. **三种渐进整合路径**：A(最小侵入/MVP)→B(专用状态)→C(一等公民)
5. **隐喻"工具的影子"**：Command 是 Tool 执行中途的"分身"
6. **YAML DSL 提案**：声明式流程定义，业务逻辑只写叶节点

**发言位置**: agent-team/meeting/2025-12-15-tool-as-command-jam.md（Investigator 的想法）

### 2025-12-15: 错误反馈模式秘密基地畅谈
**任务**: 参加 DocUI 错误反馈模式设计畅谈，从实现层面提供技术见解
**参考代码**: `atelia/prototypes/Agent.Core` — AgentEngine, ITool, LodToolExecuteResult
**贡献要点**:
1. **协程比喻**：错误反馈 = yield return，工具可以"让出控制权"给 LLM 等待澄清/选择
2. **LodToolExecuteResult 扩展**：增加 NeedsClarification/NeedsRecoveryChoice 状态，Wizard 作为返回值变体
3. **声明式 vs 迭代器**：声明式 WizardSpec 可序列化、可预览、可组合；迭代器表达力强但状态难序列化
4. **AfterToolExecute 钩子**：错误增强/恢复引导可外置，实现关注点分离
5. **JSON 序列化格式草案**：含 action_hint（可粘贴代码）、confidence（引导优先级）、context（因果链）
6. **嵌套 Agent 思路**：复杂错误可 spawn 恢复子 Agent，完成后返回主 Agent

**发言位置**: agent-team/meeting/2025-12-15-error-feedback-jam.md（Investigator 的想法）

### 2025-12-16: StateJournal MVP 设计畅谈
**任务**: 参加 StateJournal MVP 设计畅谈，从实现层面提供字节级布局和 API 设计
**参考代码**: `atelia/src/Data/ChunkedReservableWriter.cs` — 预留回填机制
**贡献要点**:
1. **字节级布局规范**：完整定义 Tag 编码、Int/String/JObject/JArray 的二进制格式
2. **分层 Footer 策略**：顶层 Record 有 Footer (TotalLen + CRC)，嵌套对象无 Footer
3. **Superblock 结构细化**：4KB×2 Ping-Pong，含 Magic/Seq/RootPtr/DataEnd/Checksum
4. **写入流程**：借鉴 ChunkedReservableWriter 的预留回填模式，LIFO 顺序 Commit
5. **C# API 草案**：`DurableRef<T>` 惰性引用 + `IDurable` static abstract interface
6. **类型擦除方案**：非泛型 `DurableRef` 处理 JObject 的异构 Value
7. **测试策略**：单元/集成/崩溃/压力测试分层
8. **实现路线图**：4 阶段 (基础设施→类型系统→写入引擎→集成)

**发言位置**: agent-team/meeting/2025-12-16-durable-heap-mvp-design.md（Investigator 的想法）

### 2025-12-15: DocUI Key-Notes 规范核查（第一轮）
**任务**: 以规范核查专家视角审阅 DocUI Key-Notes 文档
**审阅范围**: 7 个文档（glossary.md, llm-agent-context.md, doc-as-usr-interface.md, app-for-llm.md, UI-Anchor.md, micro-wizard.md, cursor-and-selection.md）
**审阅维度**: 命名约定、文档格式、代码示例、状态标记
**发现**:
1. **文件名大小写不一致**：UI-Anchor.md 与其他小写文件不同（中）
2. **状态标记与成熟度不符**：UI-Anchor.md 内容详实但标 Draft（中）
3. **代码围栏转义错误**：四反引号嵌套和空格分隔问题（高）
4. **术语连字符风格不一致**：Tool-Call vs tool calling（中）
5. **glossary AnchorTable 锚点指向问题**：指向 anchorid-结构（低）
6. **Mermaid 图转义问题**：doc-as-usr-interface.md（中）
7. **示例代码语言标记不统一**：typescript/csharp 混用（低）
8. **Stable 文档含 TODO**：llm-agent-context.md 有 5 条（低）
9. **引用格式不一致**：相对路径 vs 锚点（低）
10. **Selection-Marker 示例语法错误**：空格导致无效围栏（中）
11. **WizardView 未定义**：micro-wizard.md 使用但无定义（中）
12. **弃用术语节格式不统一**：表格 vs 标题（低）

**交付**: agent-team/meeting/2025-12-15-keynotes-audit-round1.md（DocUIGPT 发现的问题节）

### 2025-12-12: App-For-LLM 进程架构讨论（第二轮回应）
**任务**: 回应 GeminiAdvisor 的挑战——"LLM 作为开发者"约束是否改变我对方案 B 的倾向
**回应要点**:
1. 接受"LLM 作为开发者"是关键约束，修正我的判断
2. 内嵌不可替代的场景极其狭窄（<10ms 延迟、宿主 UI 紧耦合），但这些场景在 LLM Agent 领域几乎不存在
3. 同意转向方案 A+，但建议保留"内嵌 App"作为 Agent 核心团队内部实验通道
**结论**: 从 B 转向 A+，条件是保留内部实验逃生舱

### 2025-12-11: Key-Notes 驱动 Proposals 研讨会发言
**任务**: 参加文档体系研讨会，从技术实现角度验证 Key-Note 术语与业界实践的一致性
**调研范围**:
1. copilot-chat deepwiki: Tool Calling Loop, Language Model Integration, Agent Prompt System
2. 重点验证：History Append-Only、Tool-Call 作为唯一有效输出
**贡献**:
1. 验证 "History 是 Append-Only" 与业界实现一致（copilot-chat 的 toolCallRounds 数组、IBuildPromptContext 累积模式）
2. 验证 "Tool-Call 是唯一有效输出" 的技术可行性，但指出 Thinking 在调试中的价值（类比 console.log）
3. 分析术语精确性：RL 术语体系在 LLM Agent 语境下的适配性
4. 识别实现细节与理论模型的张力：流式 vs 块通讯、Cache Breakpoints 的状态管理
5. 建议 Key-Note 补充 "不可变性的实现边界"（何时/如何允许历史重写如摘要）

**发言位置**: agent-team/meeting/seminar-keynotes-system-2025-12-11.md（发言 3）

### 2025-12-11: DocUI 文档术语修订搜索
**任务**: 搜索需要修订的 DocUI 相关文档，识别三类术语
**搜索范围**: `/repos/focus` 全目录
**发现**:
1. **DDOC- 前缀**：3 个文件，~50+ 处出现
   - DocUI/docs/Proposals/ 目录（3 文件）是主要修订目标
   - agent-team/meeting/ 研讨会记录（2 文件）
   - agent-team/members/ 认知文件（2 文件）
2. **Machine Accessibility / MA**：2 个文件，9 处出现
   - 主要在研讨会记录中定义和引用
3. **[action:...] 锚点格式**：4 个文件，29 处出现
   - 研讨会记录中讨论格式选择
   - 需要决定是否改为 Button/Form 分类

**交付**: 搜索汇报（当前任务）

### 2025-12-10: DocUI Proposal 体系规划研讨会发言
**任务**: 参加第二次 DocUI 研讨会，从技术参考和已有实现角度发言
**调研范围**:
1. copilot-chat deepwiki 文档（Overview, Agent Prompt System, Tool Calling Loop）
2. DocUI wiki 和 rendering-framework.md 设计文档
**贡献**:
1. 验证 Planner 依赖图的技术合理性（基于 copilot-chat 的 IBuildPromptContext 和 IToolCallRound）
2. 提出"冻结内容"机制作为 Context 模型的参考实现
3. 分析锚点格式选项，建议 `[action:cmd]` + 正规文法约束 + 位置约束
4. 识别三个核心技术约束：工具调用轮次累积、Cache Breakpoints 位置、限制与确认机制
5. 建议 Proposal 应包含的技术规范内容（Data Structures, Invariants, Error Handling, Test Vectors）
6. 补充"历史摘要"作为高优先级遗漏主题

**发言位置**: agent-team/meeting/seminar-docui-proposals-2025-12-10.md（发言 2）

### 2025-12-10: DocUI 研讨会发言
**任务**: 参加"LLM Context 作为面向 LLM Agent 的 UI"研讨会，从技术实现角度发言
**贡献**:
1. 引入 copilot-chat Tool Calling Loop 的"轮次累积"模式作为 DocUI 可借鉴的参考实现
2. 分析"Context 累积与冻结"机制（FrozenContent、Cache Breakpoints）
3. 提出"双向契约"视角：Context 既是 UI 也是协议
4. 回应 Planner（锚点格式）和 GeminiAdvisor（屏幕阅读器类比）的观点
5. 识别 PipeMux 集成的技术边界问题

**Handoff**: agent-team/handoffs/seminar-docui-as-llm-ui-2025-12-10.md（发言已追加）

### 2025-12-09: DocUI 项目现状核实
**任务**: Team Leader 需要核实 DocUI 的实际状态
**发现**:
1. **项目阶段：设计 + 早期实现**（比预期更成熟）
2. **实际代码存在且可编译**：
   - `DocUI.Text.Abstractions` (1 files) — 抽象接口（主要是注释掉的 ITextReadOnly 草稿）
   - `DocUI.Text` (6 files) — 核心实现：StructList, SegmentListBuilder, OverlayBuilder
   - `DocUI.Text.Tests` (5 files) — 24 个测试全部通过
3. **demo/TextEditor 不在解决方案中**：
   - 是跨项目演示，依赖 PipeMux.Shared + PieceTreeSharp/TextBuffer
   - 路径配置不正确（`../PipeMux.Shared` 不存在于 DocUI 目录）
   - 实现了 EditorSession, MarkdownRenderer 等，展示了"选区可视化"概念
4. **设计文档丰富**：
   - docs/design/ 包含多个概念/方案文档
   - AGENTS.md 有详细的跨会话记忆和最新记忆日志
5. **技术决策已确定**：
   - 语言：C# + .NET 9.0
   - 格式：GitHub Flavored Markdown
   - 架构：即时模式 + Elm Architecture

**更新**: [wiki/DocUI/README.md](../../wiki/DocUI/README.md) - 源码核实全面重写
