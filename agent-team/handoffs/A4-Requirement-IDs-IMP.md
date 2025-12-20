# [A4] Implementation Result — 给现有 MUST/SHOULD 条款编号

## 实现摘要

根据畅谈会共识（2025-12-20-secret-base-mvp-v2-compression.md）行动项 A4，为 `mvp-design-v2.md` 中的规范性条款添加编号，并同步更新 `mvp-test-vectors.md` 的条款映射表。

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 添加 32 条规范性条款编号
- `DurableHeap/docs/mvp-test-vectors.md` — 更新条款编号映射表（7 条 → 32 条）

## 条款编号统计

| 分类 | 前缀 | 数量 | 覆盖范围 |
|------|------|------|----------|
| **Framing/Format** | `[F-xx]` | 9 条 | 线格式、对齐、CRC 覆盖范围、字段含义 |
| **API** | `[A-xx]` | 4 条 | 签名、返回值/异常、参数校验、可观测行为 |
| **Semantics** | `[S-xx]` | 16 条 | 跨 API/格式的语义不变式（含 commit 语义） |
| **Recovery** | `[R-xx]` | 3 条 | 崩溃一致性、resync/scan、损坏判定 |
| **总计** | — | **32 条** | — |

## 条款编号清单

### Framing/Format [F-01 ~ F-09]

| 编号 | 条款内容 | 位置 |
|------|----------|------|
| F-01 | RecordKind 域隔离（data/meta 各有独立枚举空间） | 术语表 |
| F-02 | Magic 是 Record Separator，不属于任何 Record | §3.2.1 |
| F-03 | HeadLen == TailLen，否则视为损坏 | §3.2.1 |
| F-04 | HeadLen % 4 == 0 且 Record 起点 4B 对齐 | §3.2.1 |
| F-05 | Ptr64 == 0 表示 null；否则 Ptr64 % 4 == 0 | §3.2.1 |
| F-06 | CRC32C 覆盖范围：Payload + Pad + TailLen | §3.2.1 |
| F-07 | VarInt canonical 最短编码 | §3.2.0.1 |
| F-08 | VarInt 解码错误策略（EOF/溢出/非 canonical 失败） | §3.2.0.1 |
| F-09 | ValueType 高 4 bit 必须写 0 | §3.4.2 |

### API [A-01 ~ A-04]

| 编号 | 条款内容 | 位置 |
|------|----------|------|
| A-01 | DiscardChanges MUST | §3.4.3 |
| A-02 | CommitAll() 无参重载 MUST | §3.4.5 |
| A-03 | CommitAll(IDurableObject) SHOULD | §3.4.5 |
| A-04 | Dirty Set 可见性 API SHOULD | §3.4.7 |

### Semantics [S-01 ~ S-16]

| 编号 | 条款内容 | 位置 |
|------|----------|------|
| S-01 | Dirty Set MUST 持有对象强引用 | §3.1.0.1 |
| S-02 | Identity Map 与 Dirty Set 的 key 必须等于 ObjectId | §3.1.0.1 |
| S-03 | Dirty 对象不得被 GC 回收 | §3.1.0.1 |
| S-04 | 新建对象 MUST 立即加入 Modified Object Set | §3.1.0.1 |
| S-05 | 术语约束：File Framing vs Record Layout | §3.2.1 |
| S-06 | Working State 纯净性（tombstone 不得作为值出现） | §3.4.3 |
| S-07 | Delete 一致性（ContainsKey/TryGetValue/Enumerate 一致） | §3.4.3 |
| S-08 | Commit 失败不改内存 | §3.4.3 |
| S-09 | Commit 成功后追平 | §3.4.3 |
| S-10 | 隔离性（Commit 后写入不影响 _committed） | §3.4.3 |
| S-11 | Key 唯一 + 升序 | §3.4.3 |
| S-12 | Canonical Diff（规范化：不含 net-zero 变更） | §3.4.3 |
| S-13 | 可重放性（Apply(S, D) == CurrentState） | §3.4.3 |
| S-14 | _dirtyKeys 精确性 | §3.4.3 |
| S-15 | CommitAll 失败不改内存 | §3.4.5 |
| S-16 | CommitAll 可重试 | §3.4.5 |

### Recovery [R-01 ~ R-03]

| 编号 | 条款内容 | 位置 |
|------|----------|------|
| R-01 | Resync 不得信任损坏 TailLen 并做跳跃 | §3.2.1 |
| R-02 | Meta 领先 Data 按撕裂提交处理 | §3.2.2 |
| R-03 | 崩溃恢复截断后文件仍以 Magic 分隔符结尾 | §3.5 |

## Test Vectors 映射表更新

`mvp-test-vectors.md` 的条款编号映射表已从 7 条扩展为 32 条，覆盖所有新增编号。

每条映射包含：
- 条款 ID
- 规范条款描述
- 对应测试用例（如有）

## 测试结果

- N/A（文档重构任务，无需运行测试）

## 已知差异

- 无

## 遗留问题

- 部分条款尚未关联具体测试用例（标记为 `—`），待 reference implementation 完成后补充

## Changefeed Anchor

`#delta-2025-12-20-a4-requirement-ids`
