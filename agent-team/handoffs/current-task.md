# 任务: 完成 Phase 3 全部任务（DurableDict 实现）

## 元信息
- **任务 ID**: T-20251226-01 (批量任务)
- **Phase**: 3 (DurableDict 实现)
- **类型**: 批量实施
- **优先级**: P0
- **预计时长**: 2-3 小时（基于 Phase 1/2 效率）

---

## 背景

Phase 1 + Phase 2 已完成，380 个测试全部通过！

现在进入 Phase 3，实现 StateJournal 的核心容器类型 DurableDict。

---

## 目标

完成 Phase 3 全部 6 个任务，输出到 `atelia/src/StateJournal/Objects/`。

---

## 任务清单

| 任务 ID | 名称 | 预估 | 条款覆盖 | 依赖 |
|---------|------|------|----------|------|
| T-P3-01 | DiffPayload 格式 | 3h | `[F-KVPAIR-HIGHBITS-RESERVED]`, `[S-DIFF-KEY-SORTED-UNIQUE]` | T-P2-02 |
| T-P3-02 | ValueType 编码 | 2h | `[F-UNKNOWN-VALUETYPE-REJECT]` | T-P3-01 |
| T-P3-03a | DurableDict 基础结构 | 2h | `[A-DURABLEDICT-API-SIGNATURES]`, `[S-DURABLEDICT-KEY-ULONG-ONLY]` | T-P2-05, T-P3-02 |
| T-P3-03b | DurableDict 序列化集成 | 2h | `[S-POSTCOMMIT-WRITE-ISOLATION]` | T-P3-03a |
| T-P3-04 | _dirtyKeys 机制 | 2h | `[S-DIRTYKEYS-TRACKING-EXACT]`, `[S-WORKING-STATE-TOMBSTONE-FREE]` | T-P3-03b |
| T-P3-05 | DiscardChanges | 2h | `[A-DISCARDCHANGES-REVERT-COMMITTED]`, `[S-TRANSIENT-DISCARD-DETACH]` | T-P3-04 |

**总预估**：13h

---

## 规范参考

- `atelia/docs/StateJournal/mvp-design-v2.md` §4 DurableDict
- `atelia/docs/StateJournal/implementation-plan.md` Phase 3 详情

---

## 核心概念

### DurableDict 双字典模型

```
┌─────────────────────────────────────────────┐
│ DurableDict<TValue>                         │
├─────────────────────────────────────────────┤
│ _committed: Dictionary<ulong, TValue?>      │  ← 已提交状态
│ _working: Dictionary<ulong, TValue?>        │  ← 工作副本
│ _dirtyKeys: HashSet<ulong>                  │  ← 脏键追踪
├─────────────────────────────────────────────┤
│ Get(key) → 先查 _working，再查 _committed   │
│ Set(key, value) → 写 _working + 记录脏键    │
│ Remove(key) → 写 null + 记录脏键            │
├─────────────────────────────────────────────┤
│ WritePendingDiff() → 序列化 _dirtyKeys      │
│ OnCommitSucceeded() → 合并到 _committed     │
│ DiscardChanges() → 清空 _working + 脏键     │
└─────────────────────────────────────────────┘
```

### DiffPayload 格式

```
DiffPayload := PairCount (VarInt) + KVPair*

KVPair := Key (VarUInt) + ValueType (1 byte) + Value (变长)

ValueType:
  0x00 = Tombstone (删除标记，无 Value)
  0x01 = VarInt
  0x02 = Ptr64
  0x03 = ObjRef
```

---

## 输出目录

- 源码：`atelia/src/StateJournal/Objects/`
- 测试：`atelia/tests/StateJournal.Tests/Objects/`

---

## 验收标准

| 任务 | 验收标准 |
|------|----------|
| T-P3-01 | DiffPayload 往返序列化测试通过；Key 升序唯一 |
| T-P3-02 | 未知 ValueType reject；所有类型往返测试通过 |
| T-P3-03a | API 签名匹配规范；Set/Get/Remove 基础测试通过 |
| T-P3-03b | WritePendingDiff 生成正确 payload；OnCommitSucceeded 追平状态 |
| T-P3-04 | `HasChanges == (_dirtyKeys.Count > 0)`；Get 不污染脏键 |
| T-P3-05 | Persistent 对象重置测试通过；Transient 对象 Detach 测试通过 |

---

## 执行策略建议

1. **T-P3-01 + T-P3-02** 可以一起做（序列化基础）
2. **T-P3-03a** 是 DurableDict 骨架
3. **T-P3-03b + T-P3-04 + T-P3-05** 是状态管理

可以委派 Implementer 处理复杂的序列化逻辑。

---

## 汇报要求

完成后请汇报：
1. 各任务完成情况和实际用时
2. 新增源文件和测试文件清单
3. 测试统计（累计应 > 400）
4. 遇到的问题或规范模糊点（如有）

---

## 备注

Phase 3 是 MVP 的核心——DurableDict 是所有持久化对象的基础。

保持前两个 Phase 的效率！🚀
