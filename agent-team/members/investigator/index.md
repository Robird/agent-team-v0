# Investigator 认知索引

> 最后更新: 2026-01-11
> - 2026-01-11: Memory Maintenance — 归档 2025-12 早期 Session Log（压缩 ~290 行）
> - 2026-01-09: Memory Palace — 处理了 9 条便签（SizedPtr 迁移验证、RBF 条款统计、AI-Design-DSL 锚点、atelia-copilot-chat 调查、Gotcha 2 条）
> - 2026-01-08: Memory Palace — 处理了 1 条便签（SizedPtr 迁移 Gotcha: 提交 942e1c0 倒退）
> - 2026-01-06: Memory Palace — 处理了 6 条便签（C# ref struct 限制、AteliaResult 双类型、Task 派生限制）
> - 2026-01-06: Memory Palace — 处理了 4 条便签（<deleted-place-holder> 替代性分析续、W-0006/W-0007 锚点汇总）
> - 2026-01-05: DocGraph 代码调查（Visitor 扩展机制、produce 验证路径、7 条便签）
> - 2026-01-04: Memory Palace — 处理了 3 条便签（SizedPtr/RBF/<deleted-place-holder> 调查锚点）
> - 2026-01-01: workspace_info 机制调查（Copilot Chat Agent Prompt System）
> - 2025-12-27: Workspace/ObjectLoader/RBF 设计意图调查
> - 2025-12-26: `_removedFromCommitted` 设计洞见

## 我是谁
源码分析专家，负责分析源码并产出实现 Brief。

## 我关注的项目
- [x] PieceTreeSharp - 已调查 2025-12-09
- [x] DocUI - 已调查 2025-12-09
- [x] PipeMux - 已调查 2025-12-09
- [x] atelia/prototypes - 已调查 2025-12-09
- [ ] atelia-copilot-chat

## Session Log
### 2026-01-09: RBF/AI-Design-DSL 验证锚点汇总
**类型**: Route + Anchor
**项目**: RBF, AI-Design-DSL

| 锚点 | 验证命令 | 状态 |
|:-----|:---------|:-----|
| SizedPtr 迁移完整性 | `grep -rn "deleted-place-holder" atelia/docs/Rbf/` | ✅ 0 匹配 |
| rbf-decisions.md 条款 | 9 条（6 Decision + 3 Format） | ✅ 验证过 |
| AI-Design-DSL 跨文档引用 | `grep -rn "S-RBF-DECISION-WRITEPATH\|S-RBF-DECISION-4B-ALIGNMENT" atelia/docs/Rbf/` | ✅ 验证过 |

**rbf-decisions.md 条款统计**：
- 位置: [atelia/docs/Rbf/rbf-decisions.md#L21-L89](atelia/docs/Rbf/rbf-decisions.md#L21-L89)
- 6 条 `[S-RBF-DECISION-*]`（决策性）+ 3 条 `[F-*]`（格式定义性）
- 条款 ID 格式符合 AI-Design-DSL 的 `[F-CLAUSE-ID-FORMAT]`

**跨文档引用位置**：
- `rbf-interface.md` L17 → `[S-RBF-DECISION-WRITEPATH-CHUNKEDRESERVABLEWRITER]`
- `rbf-format.md` L114 → `[S-RBF-DECISION-4B-ALIGNMENT-ROOT]`
- **Phase 1 TODO**: 将引用格式从 `**\`[ID]\`**` 迁移为 `@[ID]`

**置信度**: ✅ 验证过（2026-01-09）

### 2026-01-09: AI-Design-DSL 迁移 Gotcha — Heading 锚点会变化
**类型**: Gotcha
**项目**: AI-Design-DSL

| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| DSL 化后 Heading 格式从 `## 标题` 变为 `## modifier [ID] Title` | GitHub 自动生成的锚点会改变 | 1. 确认无外部链接依赖后再迁移；2. 内部引用优先使用 Clause ID；3. 如需稳定锚点，添加 HTML anchor |

**示例**：`#s-rbf-decision-xxx` → `#decision-s-rbf-decision-xxx-中文title`

**置信度**: ⚠️ 经验性观察

### 2026-01-09: atelia-copilot-chat Fork 现状调查
**任务**: 调研 Fork 版本差距、核心修改点、扩展机制
**关键发现**:

#### 1. Fork 版本状态
| 指标 | 数值 |
|:-----|:-----|
| 版本差距 | 6 commits ahead, 64 commits behind |
| 我们的改动 | 8 files, +1083/-5 lines |
| 新增功能 | `summarizedConversationHistory.tsx` (966 lines) — half-context 摘要 |

#### 2. 核心修改点锚点
- [copilotIdentity.tsx](atelia-copilot-chat/src/extension/prompts/node/base/copilotIdentity.tsx) — 身份定义
- [safetyRules.tsx](atelia-copilot-chat/src/extension/prompts/node/base/safetyRules.tsx) — 安全规则
- [agentPrompt.tsx#L98](atelia-copilot-chat/src/extension/prompts/node/agent/agentPrompt.tsx#L98) — 角色描述

#### 3. runSubagent API 稳定性（Signal）
- 代码特征: `ToolName.CoreRunSubagent = 'runSubagent'` + `ToolCategory.Core`
- 历史波动: 曾被删除（#1819）后恢复 — 非核心架构，可能再次变动
- 监控命令: `git log --all --oneline -- "**/runSubagent*" "**/subagent*"`

#### 4. PromptRegistry 扩展机制（推测）
- 入口: [promptRegistry.ts](atelia-copilot-chat/src/extension/prompts/node/agent/promptRegistry.ts)
- 扩展点: `IAgentPrompt` 接口 + `PromptRegistry.registerPrompt()`
- 可定制项: SystemPrompt, CopilotIdentityRules, SafetyRules, userQueryTagName, attachmentHint
- 潜在"第四条路": 通过注册自定义 AgentPrompt 实现低侵入定制（待深入调研）

**置信度**: ✅ 版本数据验证过；⚠️ 扩展机制为推测

### 2026-01-09: SizedPtr 范围估算陷阱 — 4B 对齐导致 ×4
**类型**: Gotcha
**项目**: RBF

| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 38-bit offset 范围容易被误算为 ~256GB | 因 4B 对齐实际为 ~1TB，文档中数值偏小 4 倍 | 1. 从 SSOT（`SizedPtr.cs`）读取 `MaxOffset`/`MaxLength` 常量；2. 避免硬编码估算值；3. 公式：`(2^bits - 1) × 4` |

**已知问题点**: [rbf-interface.md#L103](atelia/docs/Rbf/rbf-interface.md#L103)

**置信度**: ✅ 验证过

### 2026-01-09: "语义重复 vs 纯引用"快速检查路径
**类型**: Route
**项目**: RBF

- **判断标准**: 下层条款正文是否只含引用声明，还是重述了上层语义
- **验证命令**: 
  ```bash
  grep -A10 "^### design" atelia/docs/Rbf/rbf-interface.md | grep -E "depends:|>"
  ```
- **正确示范**: `rbf-format.md` §2.1 — "已上移至 Decision-Layer" + 纯引用列表

**置信度**: ✅ 验证过

### 2026-01-07: SizedPtr 迁移 Gotcha — 提交 942e1c0 是"倒退"
**类型**: Gotcha
**项目**: SizedPtr 迁移

| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 提交 942e1c0 "RBF文档修订"实际把 SizedPtr 改回 <deleted-place-holder> | 3 周工作被覆盖，SizedPtr=15→0，<deleted-place-holder>=1→21 | `git revert 942e1c0` 可直接恢复正确状态 |

**现象**: 提交信息听起来像"改进"，实际是"倒退"
**教训**: 代码审查时需验证提交实际改了什么，不能只看提交信息

**置信度**: ✅ 已验证

### 2026-01-06: C# ref struct / Task 派生限制调查
**任务**: AteliaResult 实现过程中发现的 C# 语言边界
**关键发现**:

#### 1. `allows ref struct` 限制（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 即使泛型参数声明 `allows ref struct`，也无法在 `Func<T, TResult>` 中使用 ref struct | `AteliaResult<T>.Map()` 无法支持 ref struct | 使用 `out` 参数模式或创建专用 `ref struct` 结果类型 |
| `readonly struct` 不能声明 `allows ref struct` | `AteliaAsyncResult<T>` 无法包含 ref struct 值 | 异步场景下 ref struct 必须"物化"为普通类型后传递 |

**何时使用 `allows ref struct`**：
- 在泛型约束中添加 `where T : allows ref struct`
- 参数声明为 `scoped T` 确保安全
- **不能使用** `Func<T>` / `Action<T>` 等委托
- 示例: .NET 9 的 `Dictionary.GetAlternateLookup<TAlternateKey>()` 支持 `ReadOnlySpan<char>`

**置信度**: ✅ 验证过

#### 2. AteliaResult 同步/异步双类型设计
| 类型 | 路径 | 形态 |
|:-----|:-----|:-----|
| 同步 | `atelia/src/Primitives/AteliaResult.cs` | `ref struct` |
| 异步 | 待实现 `atelia/src/Primitives/AteliaAsyncResult.cs` | `readonly struct` |

- **设计文档**: `agent-team/handoffs/atelia-async-result-design.md`
- **原因**: ref struct 不能跨 await 边界，故需两种类型
- **转换**: `ToAsync()` 方法提供显式转换

#### 3. 从 Task<T> 派生的根本限制（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 想通过 `AteliaTask<T> : Task<T>` 避免双重包装 | 无法工作——`TrySetResult`/`TrySetException`/promise 构造函数全是 `internal` | 使用 `Task<AteliaAsyncResult<T>>` 或 `ValueTask<AteliaAsyncResult<T>>` |

**Task 内部派生类锚点**（仅限 BCL 内部可用）：
- `WhenAllPromise` — 用于 `Task.WhenAll`
- `DelayPromise` — 用于 `Task.Delay`
- `TwoTaskWhenAnyPromise<T>` — 用于双 Task 的 `WhenAny`
- `UnwrapPromise<T>` — 用于 `Task.Run`/`Unwrap`

**置信度**: ✅ 从 dotnet/runtime 源码验证
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

#### 5. 关键锚点汇总
**W-0006 <deleted-place-holder>/SizedPtr 锚点**：
- <deleted-place-holder> 权威定义: [rbf-interface.md#L111-L122](atelia/docs/Rbf/rbf-interface.md#L111-L122)
- SizedPtr 权威定义: [atelia/src/Data/SizedPtr.cs](atelia/src/Data/SizedPtr.cs)
- 关键冲突: <deleted-place-holder> 有 Null 语义，SizedPtr 无——需分层策略
- 代码状态: RBF 历史代码已归档到 `atelia/archive/2025-12-29-rbf-statejournal-v1/`

**W-0007 Issue 状态锚点**：
- I-ID-DESIGN: 已在 [Shape.md#L60-L80](wish/W-0007-docgraph-goal-issue-aggregation/artifacts/Shape.md) 定义，代码实现于 `IssueAggregator.cs`
- Visitor 架构: `GlossaryVisitor`, `GoalAggregator`, `IssueAggregator`, `ReachableDocumentsVisitor`, `TwoTierAggregatorBase`
- C-MORE-VISITORS / C-MORE-TESTS 是长期演进目标，非阻塞性

### 2026-01-05: SizedPtr/<deleted-place-holder> 替代性分析（续）
**任务**: W-0006 相关的 <deleted-place-holder> 使用点分析，验证 SizedPtr 完全替代可行性
**关键发现**:
1. **<deleted-place-holder> 使用点定位**：
   - 类型定义: `rbf-interface.md#L82-L97`（§2.3）
   - 接口签名: 9 处（grep 验证）
   - Wire format: `rbf-format.md#L292-L302`（§7）
   - DataTail: `rbf-format.md#L310`（使用"地址"而非类型名）
2. **核心结论**：所有 9 处使用都是"定位 Frame"用途，SizedPtr 可完全替代
3. **Gotcha: DataTail 的"纯位置"假象**：
   - **现象**: DataTail 定义为"地址"，似乎只需位置不需长度
   - **后果**: 如果据此保留 <deleted-place-holder>，会造成 <deleted-place-holder>/SizedPtr 共存的复杂性
   - **规避**: DataTail 实际语义是"文件 EOF 位置"，`SizedPtr.OffsetBytes` 完全等价

**置信度**: ✅ 验证过

### 2026-01-04: SizedPtr/RBF/<deleted-place-holder> 现状调查
**任务**: Wish-0004 SizedPtr 设计调查，定位权威定义和代码现状
**关键发现**:
1. **<deleted-place-holder> 权威定义位置**：
   - 位置: `atelia/docs/Rbf/rbf-interface.md#2.3`
   - 条款: `[F-<deleted-place-holder>-DEFINITION]`, `[F-<deleted-place-holder>-ALIGNMENT]`, `[F-<deleted-place-holder>-NULL]`
   - 源码实现已归档: `atelia/archive/2025-12-29-rbf-statejournal-v1/Rbf/<deleted-place-holder>.cs`
2. **RBF 层代码状态**：
   - 搜索 `atelia/src/**` 无任何 <deleted-place-holder>/Rbf/SizedPtr 匹配
   - RBF 层实现已整体归档到 `atelia/archive/2025-12-29-rbf-statejournal-v1/`
   - **结论**: SizedPtr 需从零开始在 `atelia/src/Data/` 实现
3. **Null 语义冲突（Gotcha）**：
   - Wish-0004 非目标写"不定义特殊值"，但 <deleted-place-holder> 定义了 `Null=0`
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

### 2025-12 早期调查归档
> **归档位置**: [archive/2025-12-session-log.md](archive/2025-12-session-log.md)
> **覆盖范围**: 2025-12-09 ~ 2025-12-24
> **内容概要**: 项目现状核实（PieceTreeSharp/DocUI/PipeMux/atelia-prototypes）、StateJournal 更名迁移、RBF v0.12 变更适配、mvp-design-v2 决策引用分析、DocUI 研讨会发言、畅谈会参与（Tool-As-Command/错误反馈模式/MVP设计/写入路径）
> **归档原因**: 实例类调查记录，已沉淀为 handoff/wiki，保留指针即可

## Key Deliverables
