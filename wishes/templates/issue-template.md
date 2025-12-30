---
# Issue 条目模板
# 使用方法：在 Wish 文档的"关联 Issue"表格中添加行，或在 issues.md 中添加条目
---

## Issue 条目格式

```markdown
| IssueId | 层级 | 状态 | RelatedWish | Owner | 描述 | 阻塞原因 | LastUpdated |
|:--------|:-----|:-----|:------------|:------|:-----|:---------|:------------|
| I-0001  | L3   | Open | W-0001      | @role | ... | —        | YYYY-MM-DD  |
```

## 字段说明

| 字段 | 必填 | 说明 |
|:-----|:-----|:-----|
| IssueId | ✅ | 唯一标识，格式 `I-XXXX` |
| 层级 | ✅ | L1-L5，表示问题所属层级 |
| 状态 | ✅ | Open / InProgress / Blocked / Done / Deferred |
| RelatedWish | ✅ | 关联的 WishId，孤立 Issue 标记为 `Orphan` |
| Owner | ✅ | 负责人/角色 |
| 描述 | ✅ | 问题简述 |
| 阻塞原因 | 条件必填 | 当状态为 Blocked 时必填 |
| LastUpdated | ✅ | 最后更新日期 |

## 状态转换规则

- **Open → InProgress**: 有人开始处理
- **InProgress → Done**: 问题解决并验证
- **InProgress → Blocked**: 遇到阻塞，必须填写阻塞原因
- **Blocked → InProgress**: 阻塞解除
- **Any → Deferred**: 推迟处理，必须说明推迟到哪个 Wish/里程碑

## 关闭 Issue 的要求

- **Done**: 必须写出"如何验证已解决"
- **Deferred**: 必须指向承接对象（Wish/里程碑）
