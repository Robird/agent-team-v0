# Qa 认知索引

> 最后更新: 2026-01-06 — RBF 规范审阅三项验证洞见归档 [I-QA-008~010]

## 我是谁
测试验证专家，负责 E2E 测试、回归检测和基线跟踪。

## 我关注的项目
- [ ] PieceTreeSharp
- [x] DocGraph
- [ ] DocUI
- [x] PipeMux
- [x] StateJournal (原 DurableHeap，2025-12-21 更名)
- [ ] atelia-copilot-chat

## 最近工作

### 2026-01-01: DocGraph v0.1 Day 3 全面验证
- **状态**: ✅ 验证通过
- **验证范围**: Day 3 全部实现（CLI命令、修复功能、输出格式、端到端测试）
- **测试结果**:
  - 单元测试: 93/93 通过（Day 2后74 + Day 3新增19）
  - CLI命令测试: 19/19 通过
  - 退出码测试: 4/4 通过
  - 回归测试: 无回归
- **CLI命令验证**:
  - `docgraph --help`: 正确显示所有命令
  - `docgraph validate`: 基础/verbose/json/fix模式全部正常
  - `docgraph fix`: dry-run/--yes模式正常，成功创建缺失文件
  - `docgraph stats`: 基础/verbose/json模式正常
- **性能基准**:
  - 小规模: 6文档/4ms
  - 大规模: 2000文档/30ms (远超spec要求53倍)
- **用户体验评估**: 5/5 - 命令直观、输出清晰、交互友好
- **整体评分**: ⭐⭐⭐⭐⭐ (5/5)
- **结论**: Day 3验证通过，v0.1 MVP可交付
- **报告**: [meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase3.md](../../meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase3.md#qa-day-3-验证结果)
- **洞见**:
  - CLI 三命令分工（validate/fix/stats）设计优秀，`--dry-run` + `--yes` 组合让修复操作既安全又灵活
  - 修复功能安全性：存在 Error/Fatal 时阻止自动修复，避免在问题环境下制造更多问题
  - v0.1 MVP 达到交付标准：功能完整、质量达标（93测试）、性能优秀、体验良好

### 2026-01-01: DocGraph v0.1 P1/P2 修复验证
- **状态**: ✅ 验证通过
- **验证范围**: Craftsman 核查发现的 P1/P2 问题修复
- **测试结果**:
  - 单元测试: 74/74 通过（原 63 + 新增 11）
  - P1 修复测试: 9/9 通过
  - P2 修复测试: 2/2 通过
  - 回归测试: 无回归
- **P1 问题修复验证**:
  - P1-1: 路径归一化"越界折叠"风险 ✅（先检查后规范化）
  - P1-2: 产物文档核心字段验证 ✅（docId/produce_by 验证）
- **P2 问题修复验证**:
  - P2-1: 条款编号SSOT对齐 ✅
  - P2-2: 视觉标记/动作标签完善 ✅
  - P2-3: 排序规则字段补齐 ✅
- **规范符合性**: 完全符合 spec.md 相关条款
- **性能影响**: 无明显退化
- **整体评分**: ⭐⭐⭐⭐⭐ (5/5)
- **结论**: P1/P2 修复质量优秀，可进入下一阶段
- **报告**: [meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase2-p1p2-fix.md](../../meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase2-p1p2-fix.md#qa-p1p2修复验证结果)
- **洞见**:
  - P1-1 核心教训：路径检查必须在规范化之前执行，否则 `../outside.md` 会被折叠为 `outside.md`，典型的"顺序依赖"安全问题
  - P1-2 占位节点处理智慧：占位节点（文件不存在）不验证 docId/produce_by，避免无意义的噪音警告
  - P2-3 排序规则完善：`ValidationIssue` 新增 `TargetFilePath` 字段，排序从 4 级变为 5 级

### 2026-01-01: DocGraph v0.1 P0 修复验证
- **状态**: ✅ 验证通过
- **验证范围**: Craftsman 核查发现的 4 个 P0 规范偏差修复
- **测试结果**:
  - 单元测试: 63/63 通过（原 57 + 新增 6）
  - P0 修复测试: 7/7 通过
  - 回归测试: 无回归
- **P0 问题修复验证**:
  - P0-1: produce目标frontmatter存在性验证 ✅
  - P0-2: produce_by backlink验证逻辑 ✅
  - P0-3: 闭包确定性边排序 ✅
  - P0-4: 循环引用Info记录 ✅
- **规范符合性**: 完全符合 spec.md [A-DOCGRAPH-003/004/005] 条款
- **性能影响**: 无明显退化
- **整体评分**: ⭐⭐⭐⭐⭐ (5/5)
- **结论**: P0 修复质量优秀，可进入 Day 3
- **报告**: [meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase2.md](../../meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase2.md#qa-p0修复验证结果)
- **洞见**:
  - frontmatter 存在性检查：使用 `Frontmatter.Count > 0 && !Title.StartsWith("[缺失]")` 判断
  - backlink 验证关键：从 produce 边出发检查目标的 produce_by 声明，而非反向（后者会漏报缺失 produce_by）
  - 边排序时机：应在 DocumentGraph 构造时而非 DocumentNode 创建时执行，确保所有边都已建立
  - 循环检测实现：DFS + inStack 集合 + reportedCycles 避免重复报告

### 2026-01-01: DocGraph v0.1 Day 2 实现验证
- **状态**: ✅ 验证通过
- **验证范围**: Day 2 全部实现（DocumentGraphBuilder 集成测试、验证逻辑、错误报告）
- **测试结果**:
  - 单元测试: 57/57 通过（Day 1: 35 + Day 2: 22）
  - 端到端测试: 5/5 场景通过
  - 退出码语义: 100% 符合 spec.md §5.3
  - 性能基准: 300文档/4ms, 1500文档/9ms
- **集成测试覆盖**:
  - 目录扫描: 7 个测试用例（递归、隐藏文件、临时文件、非 md 文件）
  - 关系提取: 6 个测试用例（单/多 produce、双向关系、循环引用、传递闭包）
  - 验证逻辑: 5 个测试用例（必填字段、悬空链接、无效路径）
  - 错误报告: 4 个测试用例（统计信息、三层建议、严重度排序、文件路径）
- **整体评分**: ⭐⭐⭐⭐⭐ (5/5)
- **结论**: 通过验证，可进入 Day 3
- **报告**: [meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase2.md](../../meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase2.md#qa-测试验证结果)
- **洞见**:
  - 性能惊艳：1500 文档验证仅需 9ms，吞吐量达 166,666 docs/s，远超 spec 要求 88 倍
  - 文件过滤规则澄清：`temp~.md` 未被过滤是正确行为——过滤规则是"以 `~` 结尾"而非"包含 `~`"，符合 vim 备份文件命名模式

### 2026-01-01: DocGraph v0.1 Day 1 实现验证
- **状态**: ✅ 验证通过
- **验证范围**: Day 1 全部实现（PathNormalizer, FrontmatterParser, DocumentGraphBuilder）
- **测试结果**:
  - 单元测试: 35/35 通过
  - 端到端测试: 5/5 场景通过
  - 退出码语义: 100% 符合 spec.md
- **发现的测试缺口**:
  - 资源限制测试（64KB、10层嵌套、1024项数组）
  - DocumentGraphBuilder 集成测试（循环引用、多层追踪）
  - 大规模性能基准测试
- **整体评分**: ⭐⭐⭐⭐☆ (4.5/5)
- **结论**: 通过验证，可进入 Day 2
- **报告**: [meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase1.md](../../meeting/DocGraph/2026-01-01-docgraph-v0.1-implementation-phase1.md#qa-测试验证结果)
- **洞见**:
  - Implementer 超额完成：不只是骨架，而是完整实现；XML 注释正确引用 spec.md 条款编号
  - 性能预估乐观：3 文档验证仅需 2-3ms，远超预期
  - 资源限制测试是 Day 2 应优先补充的最大缺口

### 2025-12-27: StateJournal Storage Engine 测试侦察
- **状态**: ✅ 侦察完成
- **发现**:
  - RBF 层测试覆盖率高（157 tests），全部为内存 mock
  - StateJournal 层测试完善（601 tests），同样内存模拟
  - **关键缺失**：无文件 I/O 测试基础设施（TempFileFixture / TempWorkspaceFixture）
- **M1/M7 测试策略要点**:
  1. 必须先建立 `TempFileFixture` 才能开始 M1 实现
  2. "meta 领先 data" 损坏场景可通过截断 data.rbf 或注入垃圾数据构造
  3. Linux fsync 语义需要 P/Invoke 回退（`FileStream.Flush(true)` 可能不够）
- **测试基线**:
  - Rbf.Tests: 157 passed
  - StateJournal.Tests: 601 passed

### 2025-12-21: DurableHeap → StateJournal 项目更名
- **状态**: ✅ 认知文件已更新
- **变更**: 项目正式更名为 StateJournal 并迁入 atelia repo
- **新路径**: `atelia/docs/StateJournal/` (命名空间: `Atelia.StateJournal`)
- **核心文档**: `mvp-design-v2.md`, `mvp-test-vectors.md`, `backlog.md`

### 2025-12-20: StateJournal MVP-v2 文档优化语义完整性验证
- **状态**: ✅ 通过 - 无语义丢失
- **验证范围**: P0 (Rationale Stripping) + P1 (消灭双写) 文档优化后的语义完整性
- **对比文件**: 
  - 原始: `mvp-design-v2.bak.md` (1307 行)
  - 新版: `mvp-design-v2.md` (1159 行, -148 行)
- **验证结果**:
  - **条款完整性**: 32/32 条规范性条款全部保留
    - F 系列: 9/9 条 (Framing/Format)
    - A 系列: 4/4 条 (API)
    - S 系列: 16/16 条 (Semantics)
    - R 系列: 3/3 条 (Recovery)
  - **MUST/SHOULD 语义**: 16 处 MUST/MUST NOT (原 15 处，新增 1 处更明确的标注)
  - **核心概念定义**: 全部保留于术语表
  - **Wire Format**: EBNF 正确表达原有信息
  - **测试可追踪性**: Appendix B 引用独立测试向量文件
- **删除内容验证**: 被删除内容均为 Rationale/解释性内容，无 Contract 丢失
- **报告**: [handoffs/QA-2025-12-20-doc-optimization.md](../../handoffs/QA-2025-12-20-doc-optimization.md)

### 2025-12-20: StateJournal MVP-v2 重构语义漂移审查
- **状态**: ✅ 通过 - 无语义漂移
- **验证范围**: A1/A3/A4/A5/A6/A7/A9 重构任务后的语义对齐验证
- **对比文件**: 
  - 原始: `mvp-design-v2.bak.md`
  - 新版: `mvp-design-v2.md`
- **重构内容**:
  - A1: §2-§3 移至 `decisions/mvp-v2-decisions.md`
  - A3: 添加条款编号分类定义 [F-xx], [A-xx], [S-xx], [R-xx]
  - A4: 给 MUST/SHOULD 条款编号（32 条）
  - A5: 伪代码移到 Appendix A
  - A6: Test Vectors 引用独立文件 `mvp-test-vectors.md`
  - A7: 添加 Wire Format ASCII 图（RBF 文件结构、Record Layout、VarInt 编码）
  - A9: 章节编号从 4.x 调整为 3.x
- **验证结果**: 所有规范性条款完整保留，语义一致
- **报告**: [handoffs/QA-2025-12-20-semantic-drift-audit.md](../../handoffs/QA-2025-12-20-semantic-drift-audit.md)

### 2025-12-20: StateJournal MVP 文档第四轮共识修订验证
- **状态**: ✅ 全部通过（7/7 P0 问题）
- **验证范围**: `mvp-design-v2.md` + `mvp-test-vectors.md` 针对 2025-12-19 会议第四轮共识的修订
- **验证结果**:
  - **P0-1**: `_dirtyKeys` 实现：术语表（L27, L76）、4.4.1（L732-748）、4.4.3 不变式（L888-891）、4.4.4 伪代码（L919-963）✓
  - **P0-2**: Magic-as-Separator：4.2.1（L403, L485）✓
  - **P0-3**: `DataTail = EOF`（含尾部 Magic）：4.2.2（L538）✓
  - **P0-4**: Value 类型收敛到 null/varint/ObjRef/Ptr64：4.1.4（L343, L346）、4.4.2 ValueType（L816-820, L822）✓
  - **P0-5**: Dirty Set 卡住由 #1 解决：通过 `_dirtyKeys` 机制实现 ✓
  - **P0-6**: Commit API 改名为 `CommitAll(newRootId)`：4.4.5 章节标题（L1056, L1060）✓
  - **P0-7**: 首次 commit 空仓库 Epoch=0, NextObjectId=1：4.1.1（L299-302）、4.3.1（L658-659）、4.4.6 新章节（L1100-1118）✓
- **测试向量新增**：DIRTY-001~005、FIRST-COMMIT-001~003、VALUE-OK/BAD、COMMIT-ALL-001~002、RBF-EMPTY/SINGLE/DOUBLE ✓

### 2025-12-19: B-4/B-5/B-6 批量修订验证
- **状态**: ✅ 全部通过
- **验证范围**: `atelia/docs/StateJournal/mvp-design-v2.md` 三项修订
- **验证结果**:
  - **B-4**: §6 ChunkedReservableWriter.cs 链接为 `../../atelia/src/Data/ChunkedReservableWriter.cs`（L1012）✓
  - **B-5**: Q11 选项 A 已移除"（推荐）"标记，现为 `Upserts(key->value) + Deletes(keys)`（L155）✓
  - **B-6**: 4.1.4 节"类型约束"存在（L292），含支持类型表格（L298-302），章节编号连续（4.1.3→4.1.4→4.2）✓

### 2025-12-19: B-3 RecordKind/MetaKind 命名统一验证
- **状态**: ✅ 通过
- **验证范围**: `atelia/docs/StateJournal/mvp-design-v2.md` RecordKind/MetaKind 命名统一
- **验证结果**:
  - `MetaKind` 无残留（grep 返回 0 matches）✓
  - 4.2.2 节 meta payload 使用 `RecordKind`（L509）✓
  - 区分文件域时使用"Meta file 的 RecordKind"表述（L509, L542）✓
  - `ObjectKind` 保持不变（L68, L198, L246, L473, L566, L597, L604, L981）✓

### 2025-12-19: B-2 术语表 EpochSeq 验证
- **状态**: ✅ 通过
- **验证范围**: `atelia/docs/StateJournal/mvp-design-v2.md` 术语表"标识与指针"分组
- **验证结果**:
  - "标识与指针"分组存在 `EpochSeq` 条目（L46）✓
  - 定义为"Commit 的单调递增序号，用于判定 HEAD 新旧" ✓
  - 实现映射为 `varuint` ✓

### 2025-12-19: B-1 术语表"编码层"分组验证
- **状态**: ✅ 通过
- **验证范围**: `atelia/docs/StateJournal/mvp-design-v2.md` 术语表新增"编码层"分组
- **验证结果**:
  - "### 编码层"分组存在（L62）✓
  - 包含 `RecordKind`、`ObjectKind`、`ValueType` 三个术语（L66-68）✓
  - 表格格式与其他分组一致（四列：术语/定义/别名弃用/实现映射）✓
  - 位置在"对象级 API（二阶段提交）"分组之前（L62 < L70）✓

### 2025-12-19: A-2 Commit 流程规范约束验证
- **状态**: ✅ 通过
- **验证范围**: `atelia/docs/StateJournal/mvp-design-v2.md` 中 4.4.5 节 Commit 流程规范约束
- **验证结果**:
  - 4.4.5 节新增"规范约束（二阶段 finalize）"段落（L977-L983）✓
  - 核心约束语句存在："对象级写入不得改变 Committed/Dirty 状态；只有 heap 级 commit 成功才能 finalize"（L979）✓
  - `WritePendingDiff()` 执行时机说明：步骤 2，仅写入不更新状态（L981）✓
  - `OnCommitSucceeded()` 执行时机说明：步骤 5，必须在步骤 4 meta 落盘后（L982）✓
  - 与 4.4.4 二阶段设计语义一致性声明（L983）✓

### 2025-12-19: A-1 二阶段拆分修订验证（FlushToWriter → WritePendingDiff + OnCommitSucceeded）
- **状态**: ✅ 通过
- **验证范围**: `atelia/docs/StateJournal/mvp-design-v2.md` 中 FlushToWriter 二阶段拆分
- **验证结果**: 
  - 术语表"对象级 API（二阶段提交）"新增 `WritePendingDiff` + `OnCommitSucceeded` 两行 ✓
  - `FlushToWriter` 仅作为 Deprecated 标记保留 ✓
  - 4.4.4 伪代码 `WritePendingDiff` 方法不更新 `_committed/_isDirty` ✓
  - 4.4.4 伪代码新增 `OnCommitSucceeded()` 负责追平状态 ✓
  - "关键实现要点"正确描述二阶段崩溃安全语义 ✓
  - 正文无 `FlushToWriter` 残留（仅术语表 Deprecated 标记）✓

### 2025-12-19: A-4 术语替换验证（EpochMap → VersionIndex）
- **状态**: ✅ 通过
- **验证范围**: `atelia/docs/StateJournal/mvp-design-v2.md` 中 EpochMap 术语替换
- **验证结果**: 
  - `EpochMap` 正文无残留，仅术语表 Deprecated 标记保留（第36行）✓
  - `epoch map`（小写）无残留 ✓
  - `VersionIndex` 共出现35处，分布正确：术语表、Q7/Q8/Q9、决策汇总、规格说明等 ✓
  - Q7/Q8/Q9 决策选项区语句通顺 ✓

### 2025-12-19: A-3 术语替换验证（EpochRecord → Commit Record）
- **状态**: ✅ 通过
- **验证范围**: `atelia/docs/StateJournal/mvp-design-v2.md` 中 EpochRecord/EpochRecordPtr 术语替换
- **验证结果**: 
  - `EpochRecord` 正文无残留，仅术语表 Deprecated 标记保留（第52行）✓
  - `EpochRecordPtr` 已全部替换为 `CommitRecordPtr`（仅1处，第121行）✓
  - `epoch record`/`Epoch Record` 等变体形式无残留 ✓
  - 替换后语句通顺（第121行描述完整且语法正确）✓
  - `EpochSeq` 为合法术语（epoch序号），不在替换范围内 ✓

### 2025-12-09: PipeMux 管理命令 E2E 测试
- **状态**: ✅ 通过
- **测试命令**: `:help`, `:list`, `:ps`, `:stop`
- **发现问题**: 配置文件路径需要更新（`/repos/PieceTreeSharp/...` → `/repos/focus/...`）

## 核心洞见

### 方法论

#### [I-QA-001] LLM 全自动 Wish 执行的结构化门槛（2026-01-04）

> 来源：SizedPtr (W-0004) 实验回归分析

**五个关键自动化门槛**：

| # | 门槛 | 问题 | 结构化方案 |
|:--|:-----|:-----|:-----------|
| 1 | Demand Schema | LLM 范围漂移，建议越给越多不收敛 | `action/scope/deliverable/definition_of_done/stop_condition` |
| 2 | Review Findings 分级 | 无法隔离范围外建议 | Sev0/Sev1/Sev2 + Parking Lot |
| 3 | Tier Gate 机读输出 | LLM 无法可靠判定检验问题是否通过 | 检验结果需机器可读（非纯自然语言） |
| 4 | 测试通过 ≠ 完成 | "测试全绿"后仍存在文档-代码一致性等问题 | 显式 Review Loop（文档、API 易用性） |
| 5 | Wish 输入质量 | 占位符未替换、正文重复等 | 前置 lint/validate |

**核心洞见**：**自动化的瓶颈不在执行，而在"何时停止"和"如何判定完成"的语义边界**。

### 验证技术

#### [I-QA-002] 路径检测时序敏感（2026-01-04）

> 来源：DocGraph P1-1 验证

- **现象**：路径安全检查必须在规范化之前执行
- **机制**：`../outside.md` 规范化后变成 `outside.md`，越界信息丢失
- **通用模式**：任何"检测+转换"流程，检测应在转换前执行
- **验证技巧**：构造 `../` 前缀路径，验证系统是否拒绝而非静默折叠

#### [I-QA-003] 占位节点的验证智慧（2026-01-04）

> 来源：DocGraph P1-2 验证

- **现象**：不存在的文件（占位节点）不应验证 docId/produce_by
- **原因**：避免对"未来可能存在"的内容产生噪音警告
- **通用模式**：验证逻辑应区分"存在但不合规"与"不存在"两种状态
- **实现信号**：检查是否有"占位/placeholder"概念与验证逻辑的交互

#### [I-QA-004] 性能数据的惊喜信号（2026-01-04）

> 来源：DocGraph Day 2 验证

- **现象**：性能远超预期（88 倍）
- **可能原因**：算法选择正确 / 规模估计偏保守 / 测试数据太简单
- **验证方法**：对比真实数据规模和测试数据规模
- **警惕信号**：如果测试数据 << 真实数据，性能数据可能虚高

#### [I-QA-005] 循环检测的双集合模式（2026-01-04）

> 来源：DocGraph P0-4 验证

- **标准实现**：`visited`（避免重复访问）+ `inStack`（检测当前路径循环）
- **常见错误**：只用 `visited` 会误报（A→B→C 和 A→D→C 不是循环）
- **测试技巧**：构造钻石依赖图，验证不误报为循环

#### [I-QA-008] 跨文档一致性约束的规范表达（2026-01-06）

> 来源：RBF 规范审阅

- **场景**：两个规范文档描述同一个值（如 SizedPtr.LengthBytes 和 HeadLen），但缺少明确的等价声明
- **洞见**：
  1. 当同一语义在两处表达时，MUST 在至少一处用 MUST 声明等价关系
  2. 实现层应有对应的校验条款，形成"写入承诺 + 读取校验"闭环
  3. 交叉引用（cross-reference）应双向：A 引 B，B 也引 A
- **验证技巧**：审阅规范时主动搜索"同义词"（如 length/size/count），检查是否有隐式等价假设

#### [I-QA-009] 伪代码与规范约束的对齐验证（2026-01-06）

> 来源：RBF 规范审阅

- **场景**：§5 列出 7 项 MUST 校验约束，§6 伪代码只实现了 5 项
- **洞见**：
  1. 伪代码应与"失败策略"条款逐项对照，确保每项约束都有对应校验步骤
  2. "照抄伪代码"的实现者可能漏掉规范其他章节的约束
  3. 伪代码注释应引用相关条款 ID（如 `// 见 [F-FRAMESTATUS-VALUES]`）形成双向追踪
- **验证技巧**：审阅含伪代码的规范时，检查伪代码覆盖的校验项是否与 MUST 列表完全对齐

#### [I-QA-010] 分层规范的一致性约束（2026-01-06）

> 来源：RBF 规范审阅

- **场景**：rbf-format.md（Layer 0 实现层）与 rbf-interface.md（Layer 0/1 边界）对同一场景的异常行为描述不一致
- **洞见**：
  1. 分层文档中，下层实现 MUST 遵守上层接口契约的约束
  2. 当上层说"MUST NOT throw"，下层不能说"MAY throw"——这是逻辑矛盾
  3. 检查模式：搜索 `SHOULD` vs `MUST`、`MAY` vs `MUST NOT` 的措辞对比
- **验证技巧**：审阅分层规范时，优先检查"异常行为"条款的层间一致性

### 协作模式

#### [I-QA-006] 验证报告的关键信息密度（2026-01-04）

> 来源：记忆维护反思

- **问题**：验证报告如果全是"测试通过"，信息密度低
- **改进**：每次验证后提炼一条"可复用模式"或"发现的边界条件"
- **检验标准**：报告中是否有"下次遇到类似场景可以直接应用"的内容
- **反模式**：纯流水账式记录（X 测试通过，Y 测试通过...）

#### [I-QA-007] QA 洞见积累的元方法论（2026-01-04）

> 来源：洞见密度反思（0.3% → 阈值 1%）

**根因**：index.md 主体是"操作日志"（事件流水账），不是"可复用模式"。

**QA 应积累的五类洞见**：

| 类型 | 示例问题 |
|:-----|:---------|
| **测试策略** | 如何设计验证范围？targeted vs full sweep 的选择依据？ |
| **失败模式识别** | 典型 bug 类型、早期信号、预防措施？ |
| **边界条件技巧** | 哪些边界条件容易被遗漏？如何构造极端测试数据？ |
| **性能基准解读** | 性能数据意味着什么？何时需要警惕？ |
| **协作模式** | 如何与 Implementer 高效衔接？QA 报告的最佳实践？ |

**便签写作自检**：
- ❌ "测试通过了" → 流水账，低价值
- ✅ "为什么这个测试策略有效" / "这次验证发现了什么模式" → 可复用洞见

**关键问题**：每次验证后问自己——"下次遇到类似场景，我会怎么做得更好？"

---

## Active Changefeeds & Baselines

| Project | Changefeed | Baseline |
|---------|------------|----------|
| DocGraph | #delta-2026-01-01-docgraph-day3-qa-verification | Unit: 93/93 pass, CLI: 19/19 pass, Perf: 2000 docs/30ms |
| DocGraph | #delta-2026-01-01-docgraph-p1p2-qa-verification | Unit: 74/74 pass, P1: 9/9 pass, P2: 2/2 pass |
| DocGraph | #delta-2026-01-01-docgraph-p0-qa-verification | Unit: 63/63 pass, E2E: 5/5 pass, P0: 7/7 pass |
| DocGraph | #delta-2026-01-01-docgraph-day2 | Unit: 57/57 pass, E2E: 5/5 pass, Perf: 1500 docs/9ms |
| DocGraph | #delta-2026-01-01-docgraph-day1 | Unit: 35/35 pass, E2E: 5/5 pass |
| PipeMux | #delta-2025-12-09-management-commands | E2E: 7/7 pass |
| StateJournal | #delta-2025-12-20-p0-revision | P0 第四轮共识修订: ✅ 7/7 验证通过 |
| StateJournal | #delta-2025-12-19-b456-batch | B-4/B-5/B-6 批量修订: ✅ 验证通过 |
| StateJournal | #delta-2025-12-19-b3-recordkind | RecordKind命名统一: ✅ 验证通过 |
| StateJournal | #delta-2025-12-19-b2-epochseq | EpochSeq术语条目: ✅ 验证通过 |
| StateJournal | #delta-2025-12-19-b1-encoding-layer | 术语表编码层分组: ✅ 验证通过 |
| StateJournal | #delta-2025-12-19-a1-two-phase | WritePendingDiff + OnCommitSucceeded: ✅ 验证通过 |
| StateJournal | #delta-2025-12-19-a2-commit-constraint | Commit流程规范约束: ✅ 验证通过 |
| StateJournal | #delta-2025-12-19-terminology-a3 | EpochRecord替换: ✅ 全部完成 |
| StateJournal | #delta-2025-12-19-terminology-a4 | EpochMap替换: ✅ 全部完成 |

## Canonical Commands

### PipeMux
```bash
# 启动 Broker
cd /repos/focus/PipeMux && nohup dotnet run --project src/PipeMux.Broker -c Release > /tmp/broker.log 2>&1 &

# CLI 测试
dotnet run --project src/PipeMux.CLI -c Release -- :help
dotnet run --project src/PipeMux.CLI -c Release -- :list
dotnet run --project src/PipeMux.CLI -c Release -- :ps
dotnet run --project src/PipeMux.CLI -c Release -- :stop <app>
```

## Dependencies
- Broker 配置: `~/.config/pipemux/broker.toml`
