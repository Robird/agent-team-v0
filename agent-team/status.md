# Project Status Snapshot

> Team Leader 认知入口。只记录"现在在哪里"的快照指标。
> 过程记录 → `meeting/` | 待办事项 → `task-board.md` & `todo.md`

---

## 最近更新 (2025-12-26)

**StateJournal MVP 实现完成** 🎉 — 圣诞节两天完成了预估一周以上的工作。5 Phase，27 任务，418 测试，~3,600 行代码。战略-战术双会话协作模式首次大规模验证成功，效率达到 **8x**（预估 53.5h → 实际 6.75h）。

**代码审阅方法论建立** — [畅谈会](meeting/2025-12-26-code-review-methodology-jam.md)产出 [spec-driven-code-review.md](recipe/spec-driven-code-review.md) Recipe。核心概念：L1/L2/L3 三层审阅、EVA 结构化输出、V/U/C/I 裁决类型。

---

## 🎯 当前焦点

| 优先级 | 工作流 | 状态 | 下一步 |
|:-------|:-------|:-----|:-------|
| **P0** | StateJournal 代码审阅 | 🔜 待启动 | 按 Recipe 执行 Core 校准审阅 |
| **P1** | StateJournal 类型扩展 | 🔜 待设计 | 字符串类型、数组类型功能边界探讨 |
| **P2** | DocUI 渲染框架 | ⏸️ 延后 | 待 StateJournal 稳定 |

---

## 📦 项目状态仪表盘

### StateJournal
| 维度 | 状态 |
|:-----|:-----|
| **Tier** | 2 — MVP 完成 ✅ |
| **测试** | 418 passed |
| **模块** | Core / Objects / Workspace / Commit |
| **入口** | [atelia/src/StateJournal/](../atelia/src/StateJournal/) |
| **规范** | [mvp-design-v2.md](../atelia/docs/StateJournal/mvp-design-v2.md) (43 条款) |

> 崩溃安全的持久化堆。圣诞节完成 MVP 实现。

### PipeMux
| 维度 | 状态 |
|:-----|:-----|
| **Tier** | 1 — 生产就绪 ✅ |
| **测试** | 6 E2E (18 子测试点) 全通过 |
| **组件** | Broker / CLI / SDK / Samples |
| **入口** | [PipeMux/](../PipeMux/) |

> LLM Agent 有状态服务的本地进程编排框架。

### DocUI
| 维度 | 状态 |
|:-----|:-----|
| **Tier** | 3 — 早期探索 |
| **Key-Notes** | 10 篇 ([glossary.md](../DocUI/docs/key-notes/glossary.md) 索引 21 术语) |
| **原型** | MemoryNotebook / TextEditor / SystemMonitor |
| **入口** | [DocUI/](../DocUI/) |

> LLM-Native 纯文本 TUI 框架。术语治理体系已稳定。

### PieceTreeSharp
| 维度 | 状态 |
|:-----|:-----|
| **Tier** | 1 — 稳定 |
| **测试** | 964 passed |
| **入口** | [PieceTreeSharp/](../PieceTreeSharp/) |

> 文本建模核心库，VS Code 同款 Piece Table 实现。

### atelia-copilot-chat
| 维度 | 状态 |
|:-----|:-----|
| **分支** | `exp/custom-prompt` |
| **用途** | 系统提示词定制 + 半上下文压缩实验 |
| **入口** | [atelia-copilot-chat/](../atelia-copilot-chat/) |

> VS Code Copilot Chat 的 fork 参考。

---

## 🧠 AI Team 技术状态

### 团队架构

**参谋组 (Advisory Board)** — 设计文档审阅、方案探讨

| Specialist | 模型 | 专长 |
|:-----------|:-----|:-----|
| Advisor-Claude | Claude Opus 4.5 | 概念框架、术语治理 |
| Advisor-Gemini | Gemini 3 Pro | UX/DX、交互设计 |
| Advisor-GPT | GPT-5.2 | 规范审计、精确性验证 |

**前线组 (Field Team)** — 编码实现、测试验证

| Specialist | 模型 | 专长 |
|:-----------|:-----|:-----|
| Investigator | Claude Opus 4.5 | 源码分析、技术调研 |
| Implementer | Claude Opus 4.5 | 编码实现、移植 |
| QA | Claude Opus 4.5 | 测试编写、验证 |
| DocOps | Claude Opus 4.5 | 文档维护、索引管理 |
| CodexReviewer | Claude Opus 4.5 | 规范驱动代码审阅 |
| MemoryPalaceKeeper | Claude Opus 4.5 | 便签整理、记忆归档 |

### 认知目录结构

```
agent-team/
├── members/{specialist}/     # 私有认知 (index.md + inbox.md)
├── wiki/                     # 共享知识库
├── meeting/                  # 会议记录
├── recipe/                   # 可复用配方
├── beacon/                   # 面向未来 AI 的知识传播
└── indexes/                  # 引用索引
```

### 记忆架构

- **二阶段解耦**：Specialist 写便签 → MemoryPalaceKeeper 整理
- **分层加载**：核心认知 → 当前状态 → 按需加载项目知识
- **Memory Accumulation Protocol**：[recipe/memory-accumulation-spec.md](recipe/memory-accumulation-spec.md)

---

## 🏗️ 工作区架构

```
/repos/focus/                    # agent-team repo 根目录
├── agent-team/                  # AI Team 认知文件
├── atelia/                      # Atelia 主项目 (StateJournal, Primitives, ...)
├── PieceTreeSharp/              # 文本建模 (独立 git)
├── DocUI/                       # LLM TUI 框架 (独立 git)
├── PipeMux/                     # 进程编排 (独立 git)
├── atelia-copilot-chat/         # Copilot Chat fork (独立 git)
├── vscode/                      # TS 原版参考 (只读)
└── copilot-chat-deepwiki/       # 架构文档 (只读参考)
```

---

## 🔗 Key References

| 类型 | 链接 |
|:-----|:-----|
| **Task Board** | [task-board.md](task-board.md) |
| **TODO** | [todo.md](todo.md) |
| **Team Leader 认知** | [members/TeamLeader/index.md](members/TeamLeader/index.md) |
| **畅谈会指南** | [recipe/jam-session-guide.md](recipe/jam-session-guide.md) |
| **代码审阅配方** | [recipe/spec-driven-code-review.md](recipe/spec-driven-code-review.md) |
| **外部记忆维护** | [recipe/external-memory-maintenance.md](recipe/external-memory-maintenance.md) |
| **会议索引** | [meeting/](meeting/) |

---

## 📅 里程碑归档

> 完整历史里程碑见 [archive/](archive/)

| 日期 | 里程碑 | 关键产出 |
|:-----|:-------|:---------|
| 2025-12-26 | StateJournal MVP 完成 | 418 测试，8x 效率 |
| 2025-12-26 | 代码审阅方法论 | spec-driven-code-review.md |
| 2025-12-25 | Beacon 机制建立 | 3 篇 Beacon 发布 |
| 2025-12-23 | 记忆架构完善 | Memory Accumulation Protocol |
| 2025-12-21 | AI Team 元认知重构 | AGENTS.md, 畅谈会指南 |
| 2025-12-21 | StateJournal 迁移 | DurableHeap → StateJournal |

---

_Last updated: 2025-12-26_
