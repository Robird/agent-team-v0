# [Decision Clinic State & Error] Implementation Result

> **日期**：2025-12-21
> **Implementer**: Implementer
> **任务来源**：`agent-team/meeting/2025-12-21-decision-clinic-state-and-error.md`

## 实现摘要

根据决策诊疗室 Round 2/3 共识，完成 DurableHeap MVP v2 设计文档第二批中复杂度修订。新增 10 条规范性条款，涵盖 State 枚举核心 API、写入顺序规范化、Error Affordance 结构化、DiscardChanges ObjectId 回收语义。

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 6 处修改，约 +70 行

## 源码对齐说明

| 任务编号 | 决策内容 | 实现位置 | 备注 |
|----------|----------|----------|------|
| P0-1 | State 枚举升级 | §3.1.0.1 DurableObjectState enum | `Dirty` → `PersistentDirty` |
| P0-1 | 核心 API 条款 | §3.1.0.1 enum 定义后 | 4 条新条款 |
| P0-3 | 写入顺序条款化 | §3.2.1 写入顺序章节 | 表格化步骤 0-7 |
| P0-3 | 刷盘顺序条款 | §3.2.2 | 已存在，确认保留 |
| P1-5 | Error Affordance | §3.4.8（新章节） | 4 条新条款 + ErrorCode 枚举 |
| P1-8 | ObjectId 回收语义 | [S-TRANSIENT-DISCARD-DETACH] 末尾 | 1 条新条款 |

## 新增条款清单

### P0 优先级（4 条）

| 条款 ID | 内容摘要 |
|---------|----------|
| `[A-OBJECT-STATE-PROPERTY]` | IDurableObject MUST 暴露 State 属性；读取 MUST NOT 抛异常；O(1) |
| `[A-OBJECT-STATE-CLOSED-SET]` | DurableObjectState MUST 仅含 Clean/PersistentDirty/TransientDirty/Detached |
| `[A-HASCHANGES-O1-COMPLEXITY]` | HasChanges 属性 MUST 存在且 O(1) |
| `[S-STATE-TRANSITION-MATRIX]` | 对象状态转换 MUST 遵循状态转换矩阵 |
| `[F-RECORD-WRITE-SEQUENCE]` | 单条 Record 写入 MUST 按步骤 0-7 顺序执行 |

### P1 优先级（5 条）

| 条款 ID | 内容摘要 |
|---------|----------|
| `[A-ERROR-CODE-MUST]` | 所有异常 MUST 包含 ErrorCode 属性（字符串，机器可判定） |
| `[A-ERROR-MESSAGE-MUST]` | 所有异常 MUST 包含 Message 属性 |
| `[A-ERROR-CODE-REGISTRY]` | ErrorCode 值 MUST 在文档中登记 |
| `[A-ERROR-RECOVERY-HINT-SHOULD]` | 异常 SHOULD 包含 RecoveryHint 属性 |
| `[S-TRANSIENT-DISCARD-OBJECTID-QUARANTINE]` | Detached ObjectId 进程内 MUST NOT 重用；重启后 MAY 重用 |

## ErrorCode 枚举（MVP 最小集）

| ErrorCode | 说明 |
|-----------|------|
| `OBJECT_DETACHED` | 对象已分离 |
| `OBJECT_NOT_FOUND` | 对象不存在 |
| `CORRUPTED_RECORD` | 记录损坏 |
| `INVALID_FRAMING` | 帧格式错误 |
| `UNKNOWN_OBJECT_KIND` | 未知对象类型 |
| `COMMIT_DATA_FSYNC_FAILED` | data file fsync 失败 |
| `COMMIT_META_FSYNC_FAILED` | meta file fsync 失败 |

## 验证结果

- ✅ 枚举定义更新：`Dirty` → `PersistentDirty`
- ✅ 状态机可视化更新：增加 PersistentDirty 自环
- ✅ 写入顺序表格化
- ✅ Error Affordance 章节新增
- ✅ ObjectId 回收语义条款追加

## 遗留问题

无

## Changefeed Anchor

`#delta-2025-12-21-decision-clinic-impl`
