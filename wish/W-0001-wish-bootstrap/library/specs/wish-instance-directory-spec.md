---
docId: "wish-instance-directory-spec"
title: "Wish 实例目录规范（v0.2 草案）"
produce_by:
  - "wish/W-0001-wish-bootstrap/wish.md"
  - "wish/W-0005-wish-instance-directory/wish.md"
status: "Draft"
version: "0.2"
created: "2026-01-05"
updated: "2026-01-05"
---

# Wish 实例目录规范（v0.2 草案）

> **目的**：把 Wish 从“单文件意图”升级为“一个自带状态寄存器 + 分层产物的实例目录”，让 LLM/Agent 可以冷启动扫描并持续推进。

## 1. 总体约定

- **[WID-ROOT-001]** 实例根目录 MUST 位于仓库根下 `wish/`。
- **[WID-ROOT-002]** 每个 Wish MUST 对应 `wish/<wishId>-<slug>/` 一个实例目录。
  - `wishId` 使用 `W-0005` 形式。
  - `<slug>` 使用 `kebab-case`，稳定且简短。
- **[WID-ROOT-003]** 实例目录 MUST 包含入口文件 `wish.md`。

## 2. `wish.md`（入口 Wish 文档）

### 2.1 Frontmatter

- **[WID-WISHMD-001]** `wish.md` MUST 有 YAML frontmatter。
- **[WID-WISHMD-002]** `wish.md` frontmatter MUST 包含：
  - `wishId`, `title`, `status`, `owner`, `created`, `updated`, `tags`, `produce`
- **[WID-WISHMD-003]** `status` MUST 为 `Active | Biding | Completed | Abandoned` 之一。

### 2.2 内容结构（保持与旧 Wish 模板一致）

- **[WID-WISHMD-010]** `wish.md` SHOULD 继续使用现有 Wish 文档骨架（动机/目标/验收/层级进度/Issue/变更日志）。
- **[WID-WISHMD-011]** `produce` 字段 SHOULD 指向：
  - 本实例内的状态寄存器文件（见 §3）
  - 本实例内的 Tier 分层产物（见 §4）
  - 需要外部引用的跨目录文档（允许，但应克制）

## 3. `project-status/`（状态寄存器）

> 本目录承载 Team-Leader 心智模型中的 Goal Graph / Issue Graph / Snapshot。

- **[WID-STATUS-001]** 实例目录 MUST 包含 `project-status/`。
- **[WID-STATUS-002]** `project-status/` MUST 包含：
  - `goals.md`
  - `issues.md`
  - `snapshot.md`

## 4. `artifacts/<Tier>/`（分层产物）

- **[WID-ART-001]** 实例目录 MUST 包含 `artifacts/`。
- **[WID-ART-002]** 为避免文件数量爆炸与目录过深，`artifacts/` 下 MUST 使用“每层一个 Markdown 文件”的形式：
  - `Resolve.md`, `Shape.md`, `Rule.md`, `Plan.md`, `Craft.md`

> 注：当某一层内容膨胀到单文件难以维护时，再拆分为子目录（那是后续演进，不属于 v0.2 的默认形态）。

## 5. `meeting/` 与 `experiments/`

- **[WID-MEETING-001]** 实例目录 SHOULD 包含 `meeting/` 用于滚动工作记忆。
- **[WID-EXP-001]** 实例目录 MAY 包含 `experiments/` 用于 spike/bench/临时脚本。

## 6. 与 DocGraph 的交互约定

- **[WID-DOCGRAPH-001]** DocGraph 扫描 `wish/**/wish.md` 时：
  - docId SHOULD 使用 `wishId`（而非文件名推导）。
  - status SHOULD 来自 `wish.md` frontmatter 的 `status`。

### 6.1 `produce_by` 的允许形态（修正）

- **[WID-DOCGRAPH-010]** `produce_by` MAY 为“间接 produce”：产物文档的 `produce_by` 不要求直接指向 `wish.md`。
  - 允许指向某个中间产物文档（例如 `artifacts/Plan.md`），由其再通过 `produce_by` 或 `links.parentWish` 等元信息追溯到 Wish。
  - 若暂时无法一步到位完成追溯链，MUST 在 `artifacts/Plan.md` 中记录迁移计划（见 v0.2 迁移策略）。

> 说明：该条款是为了适应“先落盘、后清理”的渐进迁移节奏，避免迁移中期被过强的 backlink 约束卡死。

## 7. `wish-panels/`（汇总面板目录）

- **[WID-PANELS-001]** 仓库根下 SHOULD 建立 `wish-panels/` 目录，用于集中存放 DocGraph（或其他工具）生成的汇总/面板类产物。
- **[WID-PANELS-002]** `wish/` MUST 仅承载 Wish 实例目录；面板/派生视图 MUST NOT 放入 `wish/`，以避免污染 Root Nodes 扫描语义。
- **[WID-PANELS-003]** 面板类产物 MUST 视为“派生视图”，可随时重建；不应作为长期 SSOT。

---

## 附录 A：示例目录

```text
wish/
  W-0004-sizedptr/
    wish.md
    project-status/
      goals.md
      issues.md
      snapshot.md
    artifacts/
      Resolve.md
      Shape.md
      Rule.md
      Plan.md
      Craft.md
    meeting/
    experiments/
```