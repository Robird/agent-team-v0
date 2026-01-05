# Inbox — 待处理便签

> 这是你的便签收集箱。
> 写下洞见、经验、状态变更，MemoryPalaceKeeper 会定期整理归档。
>
> **格式**：每条便签用 `## 便签 YYYY-MM-DD HH:MM` 开头，用 `---` 分隔。

---

<!-- 在下方添加你的便签 -->

## 便签 2026-01-05 07:15
**类型**：[CodeMap]
**项目**：Wish Instance Directory Migration

### 迁移扩展点：创建新 Wish 实例目录
| 位置 | 修改内容 |
|:-----|:---------|
| `wish/W-XXXX-slug/wish.md` | 主 wish 文件，frontmatter 包含 wishId/title/status/produce |
| `wish/W-XXXX-slug/project-status/{goals,issues,snapshot}.md` | 状态寄存器，produce_by 指向 wish.md |
| `wish/W-XXXX-slug/artifacts/{Resolve,Shape,Rule,Plan,Craft}.md` | 分层产物，produce_by 指向 wish.md |
| 外部产物文档 | 在 produce_by 数组中追加新 wish.md 路径 |

### 注意事项
- 创建文件时**不要**使用 markdown 代码块包裹内容，直接写 frontmatter
- produce_by 需要同时保留新旧路径（过渡期），最终清理时再移除旧路径
- DocGraph 扫描 `wish/**/wish.md` 作为 Root Nodes，其他文件通过 produce 闭包发现

---

## 便签 2026-01-07 02:30
**类型**：[CodeMap]
**项目**：DocGraph v0.2 — Wish Instance Directory 布局迁移

### 收口迁移完成
| 变更 | 说明 |
|:-----|:------|
| DefaultWishDirectories | 从 `["wishes/active", "wishes/biding", "wishes/completed", "wish"]` 变为 `["wish"]` |
| Wish 识别规则 | v0.2 只识别 `wish/**/wish.md`，不再扫描旧布局 |
| Status 字段 | 从目录名推导改为从 frontmatter `status` 字段读取 |
| DocId 字段 | 从文件名推导改为从 frontmatter `wishId` 字段读取 |
| 测试文件 | DocumentGraphBuilderTests.cs 和 CommandTests.cs 完全重写为 v0.2 布局 |

### 测试布局转换规则
| 旧布局 | 新布局 |
|:-------|:-------|
| `wishes/active/wish-0001.md` | `wish/W-0001-test/wish.md` + frontmatter: `wishId: "W-0001"` |
| 从目录推导 status | frontmatter: `status: Active` |
| 从文件名推导 DocId | frontmatter: `wishId: "W-0001"` |

### 注意事项
- `produce` 路径是相对于 **workspace root** 的，不是相对于源文件的
- `../docs/api.md` 作为 produce 路径是越界的（因为从 workspace root 开始）
- `subdir/../docs/api.md` 是合法的（归一化后为 `docs/api.md`）

---
