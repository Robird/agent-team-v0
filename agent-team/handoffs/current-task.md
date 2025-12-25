# 任务: 完成 Phase 5 全部任务（Commit & Recovery）🏁

## 元信息
- **任务 ID**: T-20251226-03 (批量任务)
- **Phase**: 5 (Commit & Recovery) — **最后一个阶段！**
- **类型**: 批量实施
- **优先级**: P0
- **预计时长**: 1 小时（基于 Phase 4 的 14.7x 效率）

---

## 背景

Phase 1-4 已完成，650 个测试全部通过！累计效率 6.6x！

现在进入 **Phase 5（最后一个阶段）**，实现提交协议和崩溃恢复。

**完成后 StateJournal MVP 将正式完工！** 🎉

---

## 目标

完成 Phase 5 全部 5 个任务，输出到 `atelia/src/StateJournal/Commit/`。

---

## 任务清单

| 任务 ID | 名称 | 预估 | 条款覆盖 | 验收标准 |
|---------|------|------|----------|----------|
| T-P5-01 | VersionIndex | 3h | `[F-VERSIONINDEX-REUSE-DURABLEDICT]` | VersionIndex 是 DurableDict; Key 为 ulong |
| T-P5-02 | MetaCommitRecord | 2h | 格式定义 + 序列化 | 序列化往返测试通过 |
| T-P5-03a | CommitAll 核心流程 | 2h | `[A-COMMITALL-*]` | Dirty 对象全部写入 data file |
| T-P5-03b | Commit 完成与恢复 | 2h | `[R-COMMIT-FSYNC-ORDER]` | meta 写入; 状态清理 |
| T-P5-04 | 崩溃恢复 | 3h | `[R-META-AHEAD-BACKTRACK]`, `[R-DATATAIL-TRUNCATE-*]` | Recovery 测试通过; 撕裂提交正确回退 |

**总预估**：12h

---

## 规范参考

- `atelia/docs/StateJournal/mvp-design-v2.md` §6 Commit Protocol, §7 Recovery
- `atelia/docs/StateJournal/rbf-format.md` — RBF 格式
- `atelia/docs/StateJournal/implementation-plan.md` Phase 5 详情

---

## 核心概念

### 提交协议（Two-Phase Commit）

```
Phase 1: 写 Data
  1. 遍历 DirtySet
  2. 对每个脏对象调用 WritePendingDiff()
  3. 写入 data file（通过 RbfFramer）
  4. 更新 VersionIndex

Phase 2: 写 Meta
  1. 构造 MetaCommitRecord
  2. 写入 meta file
  3. fsync(meta)
  4. 清理 DirtySet，状态转 Clean
```

### MetaCommitRecord 格式

```
MetaCommitRecord := {
    Version: u64,          // 版本号（单调递增）
    DataTail: Address64,   // data file 有效尾部
    VersionIndexPtr: Ptr64 // VersionIndex 位置
}
```

### 崩溃恢复

```
Recovery:
  1. 读取最新的 MetaCommitRecord
  2. 比较 data file 实际长度与 DataTail
  3. 若 data > DataTail：截断 data file
  4. 加载 VersionIndex
  5. 重建 _nextObjectId（扫描 VersionIndex 最大 key + 1）
```

---

## 输出目录

- 源码：`atelia/src/StateJournal/Commit/`
- 测试：`atelia/tests/StateJournal.Tests/Commit/`

---

## 依赖关系

```
T-P5-01 (VersionIndex)
    ↓
T-P5-02 (MetaCommitRecord)
    ↓
T-P5-03a (CommitAll 核心)
    ↓
T-P5-03b (Commit 完成)
    ↓
T-P5-04 (崩溃恢复)
```

---

## 关键测试场景

| 场景 | 验证点 |
|------|--------|
| 正常提交 | DirtySet 清空; 状态转 Clean |
| 提交失败 | 内存状态不变; 可重试 |
| data file 撕裂 | 截断到 DataTail |
| meta 未写完 | 回退到上一个 meta |
| 首次启动 | VersionIndex 为空; ObjectId 从 16 开始 |

---

## 汇报要求

完成后请汇报：
1. 各任务完成情况
2. 新增测试数（累计目标 > 700）
3. **MVP 完成确认！**

---

## 备注

这是 StateJournal MVP 的**最后一个阶段**！

完成后我们将拥有一个完整的、crash-safe 的持久化系统！

冲刺到终点！🏁🚀
