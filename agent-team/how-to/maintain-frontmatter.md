# 维护 DocGraph Frontmatter 字段

> **一句话定位**：指导 AI Team 正确编写和维护 DocGraph 工具所需的 frontmatter 字段
>
> **版本**：1.0
> **状态**：Published
> **维护者**：DocOps

---

## 你在做什么？（情境锚点）

| 情境 | 跳转 |
|:-----|:-----|
| "我正在创建新的 Wish 文档" | → [§1 Wish 文档模板](#1-wish-文档模板) |
| "我正在创建 Wish 的产物文档" | → [§2 产物文档模板](#2-产物文档模板) |
| "我想添加术语定义（defines）" | → [§3 扩展字段](#3-扩展字段可选) |
| "我想添加问题跟踪（issues）" | → [§3 扩展字段](#3-扩展字段可选) |
| "DocGraph 验证报错了" | → [§4 常见错误与修复](#4-常见错误与修复) |
| "我想验证我写的是否正确" | → [§5 验证命令](#5-验证命令) |

---

## 1. Wish 文档模板

**位置**：`wishes/active/wish-XXXX-<slug>.md` 或 `wishes/completed/`

**必填字段**：

```yaml
---
title: "Wish 标题"
produce:
  - "atelia/docs/SomeFeature/api.md"
  - "atelia/docs/SomeFeature/spec.md"
---
```

| 字段 | 类型 | 说明 |
|:-----|:-----|:-----|
| `title` | string | Wish 标题（必填） |
| `produce` | string[] | 产物文档路径列表（必填，workspace 相对路径） |

**隐式推导字段**（无需手动填写）：
- `docId`：从文件名推导（`wish-0002.md` → `W-0002`）
- `status`：从目录推导（`active/` → `"active"`，`completed/` → `"completed"`）

**完整示例**：

```yaml
---
title: "DocGraph 文档关系验证工具"
status: Active
owner: "监护人刘世超"
created: 2025-12-30
produce:
  - "atelia/docs/DocGraph/v0.1/scope.md"
  - "atelia/docs/DocGraph/v0.1/api.md"
  - "atelia/docs/DocGraph/v0.1/spec.md"
tags: [tooling, automation]
---
```

---

## 2. 产物文档模板

**位置**：被 Wish 文档的 `produce` 字段引用的任意位置

**必填字段**：

```yaml
---
docId: "W-0002-L2"
title: "API 设计文档"
produce_by:
  - "wishes/active/wish-0002-doc-graph-tool.md"
---
```

| 字段 | 类型 | 说明 |
|:-----|:-----|:-----|
| `docId` | string | 文档标识（必填，建议格式：`W-XXXX-L{层级}`） |
| `title` | string | 文档标题（必填） |
| `produce_by` | string[] | 产生本文档的 Wish 路径（必填，workspace 相对路径） |

**关键约束**：
- `produce_by` 的值必须与 Wish 文档的实际路径一致
- DocGraph 会验证双向关系：Wish 的 `produce` ↔ 产物的 `produce_by`

---

## 3. 扩展字段（可选）

### 3.1 `defines` — 术语定义

用于声明本文档定义的术语，供 `GlossaryVisitor` 汇总。

```yaml
---
defines:
  - term: "文档图"
    definition: "frontmatter 关系构成的有向图，不是文件系统树"
  - term: "Root Nodes"
    definition: "Wish 文档，是文档图的入口点"
---
```

### 3.2 `issues` — 问题跟踪

用于记录本文档相关的待解决问题，供 `IssueAggregator` 汇总。

```yaml
---
issues:
  - description: "generate 命令未实现"
    status: "open"
    assignee: "Implementer"
  - description: "需要添加 CI 集成示例"
    status: "open"
---
```

---

## 4. 常见错误与修复

### 4.1 `DOCGRAPH_RELATION_DANGLING_LINK`

**含义**：`produce` 指向的文件不存在

**修复**：
1. 检查路径是否拼写正确
2. 确认目标文件是否已创建
3. 如果计划中的产物尚未创建，先创建空文件或移除引用

### 4.2 `DOCGRAPH_RELATION_MISSING_BACKLINK`

**含义**：产物文档缺少 `produce_by` 字段，或其值不包含引用它的 Wish

**修复**：
```yaml
# 在产物文档的 frontmatter 中添加：
produce_by:
  - "wishes/active/wish-XXXX.md"  # 替换为实际的 Wish 路径
```

### 4.3 `DOCGRAPH_FRONTMATTER_REQUIRED_FIELD_MISSING`

**含义**：必填字段缺失

**修复**：根据文档类型添加缺失字段（见 §1 和 §2）

### 4.4 `DOCGRAPH_YAML_PARSE_ERROR`

**含义**：YAML 语法错误

**常见原因及修复**：
- 缩进不一致：统一使用 2 空格缩进
- 冒号后缺空格：`title:value` → `title: value`
- 数组格式错误：`produce: path.md` → `produce: ["path.md"]`

---

## 5. 验证命令

编写完 frontmatter 后，运行以下命令验证：

```bash
# 从 workspace 根目录运行
cd atelia/src/DocGraph
dotnet run -- validate ../../../

# 或者验证特定目录
dotnet run -- validate ../../../wishes/
```

**预期输出**（成功时）：
```
✅ 验证通过：X 个 Wish 文档，Y 个产物文档，Z 条关系
```

**预期输出**（有问题时）：
```
❌ 发现 N 个问题：
...（问题详情）
```

---

## 路径格式约定

| 规则 | 正确示例 | 错误示例 |
|:-----|:---------|:---------|
| 使用 workspace 相对路径 | `atelia/docs/DocGraph/api.md` | `./atelia/docs/DocGraph/api.md` |
| 使用 `/` 分隔符 | `wishes/active/wish-0002.md` | `wishes\active\wish-0002.md` |
| 不以 `/` 开头 | `atelia/docs/file.md` | `/atelia/docs/file.md` |
| 不使用 `../` | `wishes/active/wish.md` | `../wishes/active/wish.md` |

---

## 参考资料

- [USAGE.md](../../../atelia/docs/DocGraph/v0.1/USAGE.md) — CLI 使用指南
- [scope.md](../../../atelia/docs/DocGraph/v0.1/scope.md) — 功能边界文档
- [api.md](../../../atelia/docs/DocGraph/v0.1/api.md) — 数据模型和错误码详情

---

## 更新历史

| 日期 | 变更 |
|:-----|:-----|
| 2026-01-01 | 初版创建，基于 DocGraph v0.1 畅谈会共识 |
