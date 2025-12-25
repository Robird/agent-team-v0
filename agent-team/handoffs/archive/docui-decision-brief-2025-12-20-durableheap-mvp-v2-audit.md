# 决策摘要：DurableHeap MVP v2 设计文档自洽性审阅

## 背景（2-3句）

DurableHeap MVP v2 设计文档经过多轮迭代后已相当成熟，但多轮修订可能留下术语不一致、条款冗余等问题。本次组织三位 DocUI Specialist（Claude/Gemini/GPT）进行畅谈式审阅，目标是确保文档自洽、一致、低冗余。

---

## 监护人批示（2025-12-21）

✅ **全部批准**，并补充以下意见：

1. **条款 ID 升级为稳定语义锚点**：一步到位，无需先做数字重编号
2. **项目用户定位**：明确 DurableHeap / DocUI / Agent-OS 的主要用户是 LLM 自己
3. **P2 `LoadObject` 补充设计思路**：`string? TryLoadObject(out DurableObject? ret)` — 返回 string 表示错误原因，成功返回 null

--- 问题清单

| # | 问题 | 严重度 | 建议方案 |
|---|------|--------|----------|
| 1 | **State 枚举仅为"建议 API"** | High | 升级为 `[A-xx]` 规范条款，明确 `Clean/Dirty/TransientDirty/Detached` 枚举 |
| 2 | **条款 ID 乱序**（如 [S-04] 出现在 [S-20] 之后） | High | 按文内首次出现顺序重新编号，附"旧→新"映射表 |
| 3 | **"必须写死"等关键契约无条款 ID** | High | 将刷盘顺序升级为 `[R-xx]`，写入步骤升级为 `[F-WR-xx]` |
| 4 | **文档自我矛盾**（禁用泛型但正文使用） | High | 删除 §3.4.5 中的泛型写法，改用描述性语句 |
| 5 | **Error Affordance 未规范化** | Medium | 新增 `[A-ERR-xx]` 条款，要求结构化错误字段（ErrorCode 为 MUST） |
| 6 | **冗余条款**（[S-08]/[S-15] 重复） | Medium | 采用 SSOT + 内联摘要模式 |
| 7 | **DiscardChanges 后 ObjectId 回收语义不明** | Medium | 在 [S-20] 中明确不回收 |
| 8 | **术语冗余**（Dirty Set/Modified Object Set） | Low | 删除弃用映射，只保留 Dirty Set |
| 9 | **命名不一致**（图中 WriteDiff vs 正文 WritePendingDiff） | Low | 统一为 WritePendingDiff |

## 推荐行动

- [ ] **P0（阻断性，立即修复）**：
  - [ ] State 枚举升级为核心 API
  - [ ] 条款 ID 重新编号
  - [ ] 关键契约升级为条款
  - [ ] 消除泛型写法自我矛盾

- [ ] **P1（本次修订应完成）**：
  - [ ] Error Affordance 规范化（ErrorCode 为 MUST）
  - [ ] 冗余条款处理（SSOT + 内联摘要）
  - [ ] 命名一致性 pass
  - [ ] DiscardChanges 语义明确
  - [ ] 术语表冗余清理

- [ ] **P2（建议但非必须）**：
  - [ ] `LoadObject` 文档标注 TryLoad 语义
  - [ ] `HasChanges` O(1) 查询保证

## 备选方案（如有分歧）

无重大分歧。所有 P0/P1 条目均获全票赞同。

P2 条目中 `LoadObject vs TryLoadObject` 命名有保留意见，最终采纳"保持现状 + 文档澄清"方案。

## 决策选项

- [x] 全部批准 ✅
- [ ] 部分批准（请标注不批准的条目）
- [ ] 需要更多信息
- [ ] 否决

---

## 稳定语义锚点映射表

> 2025-12-21 命名工作坊产出

### Format 类
| 原编号 | 稳定锚点 |
|--------|----------|
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

### API 类
| 原编号 | 稳定锚点 |
|--------|----------|
| `[A-01]` | `[A-DISCARDCHANGES-REVERT-COMMITTED]` |
| `[A-02]` | `[A-COMMITALL-FLUSH-DIRTYSET]` |
| `[A-03]` | `[A-COMMITALL-SET-NEWROOT]` |
| `[A-04]` | `[A-DIRTYSET-OBSERVABILITY]` |

### Semantics 类
| 原编号 | 稳定锚点 |
|--------|----------|
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

### Recovery 类
| 原编号 | 稳定锚点 |
|--------|----------|
| `[R-01]` | `[R-RESYNC-DISTRUST-TAILLEN]` |
| `[R-02]` | `[R-META-AHEAD-BACKTRACK]` |
| `[R-03]` | `[R-DATATAIL-TRUNCATE-GARBAGE]` |
| `[R-04]` | `[R-ALLOCATOR-SEED-FROM-HEAD]` |

---

**会议文件**：
- [2025-12-20 畅谈会](../meeting/2025-12-20-secret-base-durableheap-mvp-v2-final-audit.md)
- [2025-12-21 命名工作坊](../meeting/2025-12-21-semantic-anchor-naming-workshop.md)

**参与者**：DocUIClaude、DocUIGemini、DocUIGPT

**日期**：2025-12-20（初版）/ 2025-12-21（批示 + 锚点设计）

---
监护人批示：✅ 全部批准（2025-12-21）
