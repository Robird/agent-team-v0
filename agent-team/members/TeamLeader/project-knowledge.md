# focus 生态项目知识

> 分离自 lead-metacognition.md (2025-12-21)
> 按需加载：当处理特定项目任务时加载此文件

---

## 项目概览

| 项目 | 定位 | 成熟度 | 测试基线 |
|------|------|--------|----------|
| **PieceTreeSharp** | VS Code 编辑器核心 C# 移植 | Tier 1 生产就绪 | 1158 passed |
| **PipeMux** | 本地进程编排框架（Named Pipe） | Tier 1 核心稳定 | E2E 脚本 |
| **DurableHeap** | 持久化堆协议 | Tier 2 设计稳定 | MVP v2 设计完成 |
| **DocUI** | LLM-Native 纯文本 TUI 框架 | Tier 3 早期探索 | 24 passed |
| **atelia/prototypes** | Agent 技术栈实验场 | Tier 2 可用 | — |
| **atelia-copilot-chat** | Copilot Chat fork，研究参考 | — | — |

---

## 技术栈全景图

```
┌──────────────────────────────────────────────────────────────────────────┐
│                         focus 生态技术栈全景                              │
├──────────────────────────────────────────────────────────────────────────┤
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                     应用层 (Tier 3 早期)                         │    │
│  │   DocUI — LLM-Native TUI 框架                                   │    │
│  │   ├── DocUI.Text (24 tests) ✅                                  │    │
│  │   ├── Widget 系统 🔄 概念                                       │    │
│  │   └── MUD Demo 🔮 规划中                                        │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                    │                                     │
│                                    ▼                                     │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                     持久化层 (Tier 2 设计稳定)                   │    │
│  │   DurableHeap — 持久化堆协议                                    │    │
│  │   ├── MVP v2 设计文档 ✅                                        │    │
│  │   └── 实现 🔮 待开始                                            │    │
│  └─────────────────────────────────────────────────────────────────┘    │
│                                    │                                     │
│                                    ▼                                     │
│  ┌─────────────────────────────────────────────────────────────────┐    │
│  │                     基础设施层 (Tier 1 稳定)                     │    │
│  │   PieceTreeSharp — 编辑器核心 (1158 tests) ✅                   │    │
│  │   PipeMux — 进程编排框架 ✅                                     │    │
│  └─────────────────────────────────────────────────────────────────┘    │
└──────────────────────────────────────────────────────────────────────────┘
```

---

## DurableHeap 项目状态

### MVP v2 设计（2025-12-21 更新）

**文档位置**: `DurableHeap/docs/mvp-design-v2.md`
**Backlog**: `DurableHeap/docs/backlog.md`

**已完成的审阅修订**:
- ✅ 稳定语义锚点（43 条款）
- ✅ State 枚举升级为核心 API（4 条款）
- ✅ Error Affordance 规范化（4 条款）
- ✅ "必须写死"条款化
- ✅ 命名一致性、泛型矛盾修复

**相关会议记录**:
- `agent-team/meeting/2025-12-20-secret-base-durableheap-mvp-v2-final-audit.md`
- `agent-team/meeting/2025-12-21-semantic-anchor-naming-workshop.md`
- `agent-team/meeting/2025-12-21-decision-clinic-state-and-error.md`

---

## DocUI 项目状态

### Key-Notes（规范文档）

**位置**: `DocUI/docs/key-notes/`

| 文档 | 状态 | 内容 |
|------|------|------|
| `glossary.md` | 活跃 | 术语表，所有概念的权威定义 |
| `ui-anchor.md` | Adopted | UI-Anchor 核心机制 |
| `context-projection.md` | Adopted | 上下文投影与 LOD |
| `action-prototype.md` | Adopted | 可执行代码片段 |
| `micro-wizard.md` | Draft | 微流程/向导 |
| `cursor-and-selection.md` | Draft | 光标与选区表示 |

### 关键概念

**LLM-Native TUI** 核心设计理念:
- **用户是 LLM**：不是人类在看屏幕，是 LLM 在消费文本
- **UI-Anchor**：可寻址的交互锚点
- **Context Projection**：LOD（细节层次）机制
- **Action Prototype**：可执行的代码片段
