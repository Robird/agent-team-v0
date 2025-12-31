# ⚠️ Recipe 目录已迁移

> **迁移日期**: 2026-01-01  
> **新位置**: `agent-team/how-to/`  
> **迁移原因**: 提升 AI Agent 的自动发现能力，优化 Token 使用效率

## 路径映射

| 旧路径 | 新路径 |
|:-------|:-------|
| `agent-team/recipe/jam-session-guide.md` | `agent-team/how-to/run-jam-session.md` |
| `agent-team/recipe/naming-skill-guide.md` | `agent-team/how-to/name-things-well.md` |
| `agent-team/recipe/memory-accumulation-spec.md` | `agent-team/how-to/accumulate-memory.md` |
| `agent-team/recipe/memory-maintenance-skill.md` | `agent-team/how-to/maintain-memory.md` |
| `agent-team/recipe/memory-maintenance-orchestration.md` | `agent-team/how-to/organize-deep-maintenance.md` |
| `agent-team/recipe/memory-palace-batch-guide.md` | `agent-team/how-to/batch-process-inbox.md` |
| `agent-team/recipe/real-time-collaboration-pattern.md` | `agent-team/how-to/collaborate-realtime.md` |
| `agent-team/recipe/strategic-tactical-dual-session.md` | `agent-team/how-to/dual-session-pattern.md` |
| `agent-team/recipe/spec-driven-code-review.md` | `agent-team/how-to/review-code-with-spec.md` |
| `agent-team/recipe/test-file-splitting.md` | `agent-team/how-to/split-test-files.md` |
| `agent-team/recipe/recipe-meta-recipe-draft.md` | `agent-team/how-to/write-recipe.md` |
| `agent-team/recipe/beacon-recipe.md` | `agent-team/how-to/generate-beacon.md` |
| `agent-team/recipe/external-memory-maintenance.md` | `agent-team/how-to/maintain-external-memory.md` |

## 如果你看到这个文件

1. **你正在使用过期路径**——请更新你的引用
2. **历史文档中的旧路径**可以保留——它们记录的是历史事实
3. **新文档**必须使用新路径

## 索引位置

所有操作指南的索引现在位于：
- **AGENTS.md** → "操作指南速查"章节（极简Prompt化索引）
- **workspace_info** → 自动显示 `agent-team/how-to/` 目录

## 关联决策

> **决策记录**: `meeting/2026-01-01-recipe-discovery-decision.md`  
> **技术讨论**: `meeting/2026-01-01-recipe-discovery-mechanism.md`  
> **元Recipe**: `how-to/write-recipe.md`

## 稳健路线说明

本次迁移采用**稳健路线**：
- ✅ 目录改名但不上移（保持 `agent-team/` 前缀）
- ✅ 零引用修复成本（历史文档保持原样）
- ✅ 极简索引格式（优化 Token 使用）
- ✅ 可迭代优化（根据效果决定是否上移到根目录）

如果发现效果不足，可考虑 Phase 2：将 `agent-team/how-to/` 上移到根目录 `/how-to/`。