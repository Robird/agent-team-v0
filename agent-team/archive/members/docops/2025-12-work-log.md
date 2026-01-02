# DocOps 工作日志归档 (2025-12)

> 归档日期：2026-01-03
> 来源：`agent-team/members/docops/index.md` 增量维护

---

## 2025-12-31 - 术语 SSOT 三层结构实施

**任务**: 响应术语命名规范畅谈会决策，实施三层 SSOT 结构

**完成项目**:

| 任务 | 变更 | 说明 |
|:-----|:-----|:-----|
| 检查脚本修复 | `check-terminology-consistency.sh` | 移除 `grep -i`，添加反例/代码域过滤 |
| artifact-tiers.md 导航添加 | v1.0.0 → v1.2.0 | 三层 SSOT 导航章节 |
| artifact-tiers.md 写法清理 | v1.2.0 → v1.3.0 | 写法规范内容移至 spec-conventions.md，用 Redirect Stub 替换 |
| wish-system-rules.md 精简 | v0.3.0 → v0.4.0 | 保留 Wish 特有术语，移除全局概念，添加三层引用 |

**验证结果**:
- wiki 目录：0 术语警告 ✅
- `check-terminology-consistency.sh` 脚本通过 ✅
- 所有 Redirect Stub 链接格式正确 ✅

---

## 2025-12-30 - 五层级术语体系迁移（P1 行动项）

**任务**: 响应畅谈会决策，将 Wish 系统相关文档的旧术语（What/How/Build）更新为新术语（Shape/Plan/Craft）

**更新文件清单**:

| 文件 | 版本变更 | 主要修改 |
|:-----|:---------|:---------|
| `wishes/templates/wish-template.md` | — | 层级进度表格术语更新，添加术语表引用 |
| `wishes/README.md` | — | 核心概念表格术语更新，添加术语表引用 |
| `wishes/specs/wish-system-rules.md` | v0.1.0 → v0.2.0 | §1 术语定义更新，新增 §9 术语演变说明 |
| `wishes/active/wish-0001-*.md` | — | 层级进度表格术语更新 |
| `wishes/active/wish-0002-*.md` | — | 层级进度表格术语更新 |

**验证结果**:
- ✅ 旧术语（L2 What, L4 How, L5 Build）：0 处残留
- ✅ 新术语（Shape-Tier, Plan-Tier, Craft-Tier）：9 处正确应用
- ✅ 术语表引用：3 处添加

---

## 2025-12-30 - Wish 系统首次文档一致性核查

**任务**: 核查 Wish 系统文档结构与双向链接规范落地情况

**核心发现**:
1. Wish 系统结构完整，37 条 L3 条款覆盖全面
2. 双向链接规范存在落地差距：畅谈会记录作为 L1 产物被引用，但其自身没有 frontmatter 和 ParentWish 字段
3. 这一差距是**合理的例外**：畅谈会记录是通用性质文档，不应与单一 Wish 绑定

---

## 2025-12-29 - StateJournal 文档重构 Phase 1 完成

**任务**: 执行 StateJournal 文档原子化重构 Phase 1

**产出文件**:
| 任务 | 产出 | 关键指标 |
|:-----|:-----|:---------|
| Task 1.0 条款注册表 | `clauses/index.md` | 51 条款（S:33, A:18） |
| Task 1.1 术语表提取 | `glossary.md` | 266 行，原文减少 171 行（11.5%） |
| Task 1.2 场景卡片 | `scenarios/loadobject.md` + `scenarios/index.md` | 9 条款引用，全部验证通过 |

---

## 2025-12-29 - clauses/index.md 行号漂移紧急修复

**问题**: Task 1.1 术语表提取后，mvp-design-v2.md 行号漂移导致 51/51 条款行号失效

**修复内容**:
- 行号重算：51/51 条款全部重新定位
- 统计修正：`S:35, A:16` → 实际 `S:33, A:18`
- 版本：v1.0 → v1.1

---

## 2025-12-28 - FrameTag Wrapper Type 移除全文档同步

**任务**: 响应 wrapper type 核查决议，同步所有 RBF 相关文档

**变更文件**:
| 文件 | 版本变更 | 主要修改 |
|:-----|:---------|:---------|
| `rbf-interface.md` | v0.16 → v0.17 | 移除 `[F-FRAMETAG-DEFINITION]`，接口参数 `FrameTag` → `uint` |
| `mvp-design-v2.md` | v3.8 → v3.9 | 术语表 FrameTag 定义更新 |
| `rbf-format.md` | v0.15 → v0.16 | 修复过时引用 |
| `rbf-test-vectors.md` | v0.10 → v0.11 | 核查确认无需修改 |

---

## 2025-12-28 - RBF 测试向量文档版本同步检查

**任务**: 检查 `rbf-test-vectors.md` 是否需要更新以反映 `rbf-interface.md` v0.16 变更

**检查结果**: 测试向量专注 Layer 0 线格式，不涉及写入器 API，无需更新。

---

## 2025-12-27 - StateJournal mvp-design-v2.md Workspace 绑定机制增补

**任务**: 添加 §3.1.2.1 Workspace 绑定机制条款（7 个规范条款）

---

## 2025-12-24 - StateJournal mvp-design-v2.md 文档冗余清理

**任务**: 移除冗余内容，精简文档

**效果**: 文档体积从 1416 行减少到 1399 行（-17 行）

---

## 2025-12-23 - 记忆积累机制畅谈会（第三波）

**核心观点**:
- 86 行精简是"设计好"的结果：index.md 只放工作日志，产物外置
- 提出"三层分类"文档规范：索引层、摘要层、详情层
- 建议采用"changefeed anchor"作为记忆文件的索引机制

---

## 2025-12-21 相关工作

- StateJournal mvp-design-v2.md §3.4.8 引用更新
- AteliaResult 全项目规范文档创建
- DurableHeap → StateJournal 更名迁移

---

## 2025-12-09 - PipeMux 管理命令文档更新

**更新文件**:
- `PipeMux/docs/README.md`: 添加管理命令章节
- `agent-team/wiki/PipeMux/README.md`: 添加管理命令章节
