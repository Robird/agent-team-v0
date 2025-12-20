# Qa 认知索引

> 最后更新: 2025-12-20

## 我是谁
测试验证专家，负责 E2E 测试、回归检测和基线跟踪。

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [x] PipeMux
- [x] DurableHeap
- [ ] atelia-copilot-chat

## 最近工作

### 2025-12-20: DurableHeap MVP-v2 重构语义漂移审查
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
  - A7: 添加 Wire Format ASCII 图（ELOG 文件结构、Record Layout、VarInt 编码）
  - A9: 章节编号从 4.x 调整为 3.x
- **验证结果**: 所有规范性条款完整保留，语义一致
- **报告**: [handoffs/QA-2025-12-20-semantic-drift-audit.md](../../handoffs/QA-2025-12-20-semantic-drift-audit.md)

### 2025-12-20: DurableHeap MVP 文档第四轮共识修订验证
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
- **测试向量新增**：DIRTY-001~005、FIRST-COMMIT-001~003、VALUE-OK/BAD、COMMIT-ALL-001~002、ELOG-EMPTY/SINGLE/DOUBLE ✓

### 2025-12-19: B-4/B-5/B-6 批量修订验证
- **状态**: ✅ 全部通过
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` 三项修订
- **验证结果**:
  - **B-4**: §6 ChunkedReservableWriter.cs 链接为 `../../atelia/src/Data/ChunkedReservableWriter.cs`（L1012）✓
  - **B-5**: Q11 选项 A 已移除"（推荐）"标记，现为 `Upserts(key->value) + Deletes(keys)`（L155）✓
  - **B-6**: 4.1.4 节"类型约束"存在（L292），含支持类型表格（L298-302），章节编号连续（4.1.3→4.1.4→4.2）✓

### 2025-12-19: B-3 RecordKind/MetaKind 命名统一验证
- **状态**: ✅ 通过
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` RecordKind/MetaKind 命名统一
- **验证结果**:
  - `MetaKind` 无残留（grep 返回 0 matches）✓
  - 4.2.2 节 meta payload 使用 `RecordKind`（L509）✓
  - 区分文件域时使用"Meta file 的 RecordKind"表述（L509, L542）✓
  - `ObjectKind` 保持不变（L68, L198, L246, L473, L566, L597, L604, L981）✓

### 2025-12-19: B-2 术语表 EpochSeq 验证
- **状态**: ✅ 通过
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` 术语表"标识与指针"分组
- **验证结果**:
  - "标识与指针"分组存在 `EpochSeq` 条目（L46）✓
  - 定义为"Commit 的单调递增序号，用于判定 HEAD 新旧" ✓
  - 实现映射为 `varuint` ✓

### 2025-12-19: B-1 术语表"编码层"分组验证
- **状态**: ✅ 通过
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` 术语表新增"编码层"分组
- **验证结果**:
  - "### 编码层"分组存在（L62）✓
  - 包含 `RecordKind`、`ObjectKind`、`ValueType` 三个术语（L66-68）✓
  - 表格格式与其他分组一致（四列：术语/定义/别名弃用/实现映射）✓
  - 位置在"对象级 API（二阶段提交）"分组之前（L62 < L70）✓

### 2025-12-19: A-2 Commit 流程规范约束验证
- **状态**: ✅ 通过
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` 中 4.4.5 节 Commit 流程规范约束
- **验证结果**:
  - 4.4.5 节新增"规范约束（二阶段 finalize）"段落（L977-L983）✓
  - 核心约束语句存在："对象级写入不得改变 Committed/Dirty 状态；只有 heap 级 commit 成功才能 finalize"（L979）✓
  - `WritePendingDiff()` 执行时机说明：步骤 2，仅写入不更新状态（L981）✓
  - `OnCommitSucceeded()` 执行时机说明：步骤 5，必须在步骤 4 meta 落盘后（L982）✓
  - 与 4.4.4 二阶段设计语义一致性声明（L983）✓

### 2025-12-19: A-1 二阶段拆分修订验证（FlushToWriter → WritePendingDiff + OnCommitSucceeded）
- **状态**: ✅ 通过
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` 中 FlushToWriter 二阶段拆分
- **验证结果**: 
  - 术语表"对象级 API（二阶段提交）"新增 `WritePendingDiff` + `OnCommitSucceeded` 两行 ✓
  - `FlushToWriter` 仅作为 Deprecated 标记保留 ✓
  - 4.4.4 伪代码 `WritePendingDiff` 方法不更新 `_committed/_isDirty` ✓
  - 4.4.4 伪代码新增 `OnCommitSucceeded()` 负责追平状态 ✓
  - "关键实现要点"正确描述二阶段崩溃安全语义 ✓
  - 正文无 `FlushToWriter` 残留（仅术语表 Deprecated 标记）✓

### 2025-12-19: A-4 术语替换验证（EpochMap → VersionIndex）
- **状态**: ✅ 通过
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` 中 EpochMap 术语替换
- **验证结果**: 
  - `EpochMap` 正文无残留，仅术语表 Deprecated 标记保留（第36行）✓
  - `epoch map`（小写）无残留 ✓
  - `VersionIndex` 共出现35处，分布正确：术语表、Q7/Q8/Q9、决策汇总、规格说明等 ✓
  - Q7/Q8/Q9 决策选项区语句通顺 ✓

### 2025-12-19: A-3 术语替换验证（EpochRecord → Commit Record）
- **状态**: ✅ 通过
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` 中 EpochRecord/EpochRecordPtr 术语替换
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

## Active Changefeeds & Baselines

| Project | Changefeed | Baseline |
|---------|------------|----------|
| PipeMux | #delta-2025-12-09-management-commands | E2E: 7/7 pass |
| DurableHeap | #delta-2025-12-20-p0-revision | P0 第四轮共识修订: ✅ 7/7 验证通过 |
| DurableHeap | #delta-2025-12-19-b456-batch | B-4/B-5/B-6 批量修订: ✅ 验证通过 |
| DurableHeap | #delta-2025-12-19-b3-recordkind | RecordKind命名统一: ✅ 验证通过 |
| DurableHeap | #delta-2025-12-19-b2-epochseq | EpochSeq术语条目: ✅ 验证通过 |
| DurableHeap | #delta-2025-12-19-b1-encoding-layer | 术语表编码层分组: ✅ 验证通过 |
| DurableHeap | #delta-2025-12-19-a1-two-phase | WritePendingDiff + OnCommitSucceeded: ✅ 验证通过 |
| DurableHeap | #delta-2025-12-19-a2-commit-constraint | Commit流程规范约束: ✅ 验证通过 |
| DurableHeap | #delta-2025-12-19-terminology-a3 | EpochRecord替换: ✅ 全部完成 |
| DurableHeap | #delta-2025-12-19-terminology-a4 | EpochMap替换: ✅ 全部完成 |

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
