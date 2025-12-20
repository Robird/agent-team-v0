# [DurableHeap] 条款 ID 语义锚点重构 Implementation Result

## 实现摘要

根据命名工作坊共识（2025-12-21-semantic-anchor-naming-workshop.md），将 MVP v2 设计文档中的 43 个数字条款 ID 批量替换为稳定语义锚点（Stable Semantic Anchors），并更新了"条款编号"章节的规则说明。

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 54 处条款 ID 替换 + 规则说明更新

## 源码对齐说明

| 旧格式 | 新格式 | 示例 |
|--------|--------|------|
| `[F-nn]` | `[F-SEMANTIC-NAME]` | `[F-01]` → `[F-RECORDKIND-DOMAIN-ISOLATION]` |
| `[A-nn]` | `[A-SEMANTIC-NAME]` | `[A-02]` → `[A-COMMITALL-FLUSH-DIRTYSET]` |
| `[S-nn]` | `[S-SEMANTIC-NAME]` | `[S-17]` → `[S-OBJECTID-RESERVED-RANGE]` |
| `[R-nn]` | `[R-SEMANTIC-NAME]` | `[R-01]` → `[R-RESYNC-DISTRUST-TAILLEN]` |

### 完整映射表

**Format 类 (13 条)**：
| 旧 ID | 新锚点 |
|-------|--------|
| `[F-01]` | `[F-RECORDKIND-DOMAIN-ISOLATION]` |
| `[F-02]` | `[F-MAGIC-RECORD-SEPARATOR]` |
| `[F-03]` | `[F-HEADLEN-TAILLEN-SYMMETRY]` |
| `[F-04]` | `[F-RECORD-4B-ALIGNMENT]` |
| `[F-05]` | `[F-PTR64-NULL-AND-ALIGNMENT]` |
| `[F-06]` | `[F-CRC32C-PAYLOAD-COVERAGE]` |
| `[F-07]` | `[F-VARINT-CANONICAL-ENCODING]` |
| `[F-08]` | `[F-DECODE-ERROR-FAILFAST]` |
| `[F-09]` | `[F-KVPAIR-HIGHBITS-RESERVED]` |
| `[F-10]` | `[F-OBJECTKIND-STANDARD-RANGE]` |
| `[F-11]` | `[F-OBJECTKIND-VARIANT-RANGE]` |
| `[F-12]` | `[F-UNKNOWN-OBJECTKIND-REJECT]` |
| `[F-13]` | `[F-MAGIC-IS-FENCE]` |

**API 类 (4 条)**：
| 旧 ID | 新锚点 |
|-------|--------|
| `[A-01]` | `[A-DISCARDCHANGES-REVERT-COMMITTED]` |
| `[A-02]` | `[A-COMMITALL-FLUSH-DIRTYSET]` |
| `[A-03]` | `[A-COMMITALL-SET-NEWROOT]` |
| `[A-04]` | `[A-DIRTYSET-OBSERVABILITY]` |

**Semantics 类 (22 条)**：
| 旧 ID | 新锚点 |
|-------|--------|
| `[S-01]` | `[S-DIRTYSET-OBJECT-PINNING]` |
| `[S-02]` | `[S-IDENTITY-MAP-KEY-COHERENCE]` |
| `[S-03]` | `[S-DIRTY-OBJECT-GC-PROHIBIT]` |
| `[S-04]` | `[S-NEW-OBJECT-AUTO-DIRTY]` |
| `[S-05]` | `[S-DEPRECATED-MERGED-TO-S06]` |
| `[S-06]` | `[S-WORKING-STATE-TOMBSTONE-FREE]` |
| `[S-07]` | `[S-DELETE-API-CONSISTENCY]` |
| `[S-08]` | `[S-COMMIT-FAIL-MEMORY-INTACT]` |
| `[S-09]` | `[S-COMMIT-SUCCESS-STATE-SYNC]` |
| `[S-10]` | `[S-POSTCOMMIT-WRITE-ISOLATION]` |
| `[S-11]` | `[S-DIFF-KEY-SORTED-UNIQUE]` |
| `[S-12]` | `[S-DIFF-CANONICAL-NO-NETZERO]` |
| `[S-13]` | `[S-DIFF-REPLAY-DETERMINISM]` |
| `[S-14]` | `[S-DIRTYKEYS-TRACKING-EXACT]` |
| `[S-15]` | `[S-HEAP-COMMIT-FAIL-INTACT]` |
| `[S-16]` | `[S-COMMIT-FAIL-RETRYABLE]` |
| `[S-17]` | `[S-OBJECTID-RESERVED-RANGE]` |
| `[S-18]` | `[S-CHECKPOINT-HISTORY-CUTOFF]` |
| `[S-19]` | `[S-MSB-HACK-REJECTED]` |
| `[S-20]` | `[S-TRANSIENT-DISCARD-DETACH]` |
| `[S-21]` | `[S-OBJECTID-MONOTONIC-BOUNDARY]` |
| `[S-22]` | `[S-CREATEOBJECT-IMMEDIATE-ALLOC]` |

**Recovery 类 (4 条)**：
| 旧 ID | 新锚点 |
|-------|--------|
| `[R-01]` | `[R-RESYNC-DISTRUST-TAILLEN]` |
| `[R-02]` | `[R-META-AHEAD-BACKTRACK]` |
| `[R-03]` | `[R-DATATAIL-TRUNCATE-GARBAGE]` |
| `[R-04]` | `[R-ALLOCATOR-SEED-FROM-HEAD]` |

## 测试结果

- 验证：`grep '\[F-[0-9]\+\]\|\[A-[0-9]\+\]\|\[S-[0-9]\+\]\|\[R-[0-9]\+\]'` → **无遗漏**
- 统计：54 处替换（F:17, A:5, S:27, R:5）
- 去重后：43 个唯一锚点（与映射表一致）

## 已知差异

无。所有 43 个条款均按映射表完成替换。

## 遗留问题

- `mvp-test-vectors.md` 中的条款引用需同步更新（如有）
- 其他引用 MVP v2 文档条款的文件需检查更新

## Changefeed Anchor

`#delta-2025-12-21-semantic-anchor-refactor`
