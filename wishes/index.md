# Wish 索引

> **文档性质**: 派生视图 / 可重建
> **更新时间**: 2025-12-30
> **说明**: 本文件可从 `wishes/active/*.md`、`wishes/completed/*.md`、`wishes/abandoned/*.md` 扫描生成。

---

## 快速导航

### 🎯 从这里开始

| 优先级 | Wish | 一句话动机 | 状态 |
|:-------|:-----|:-----------|:-----|
| 1 | [W-0001 Wish 系统自举](active/wish-0001-wish-system-bootstrap.md) | 建立 Wish 驱动的开发工作流 | 🟡 进行中 |
| 2 | [W-0002 DocGraph](active/wish-0002-doc-graph-tool.md) | 自动维护链接健康和状态汇总 | ⚪ 未开始 |

### 🔴 当前阻塞

> 暂无阻塞的 Wish 或 Issue。

---

## 活跃 Wish (Active)

| WishId | 标题 | Owner | L1 | L2 | L3 | L4 | L5 | 更新日期 |
|:-------|:-----|:------|:---|:---|:---|:---|:---|:---------|
| [W-0001](active/wish-0001-wish-system-bootstrap.md) | Wish 系统自举 | 监护人 | 🟢 | 🟢 | 🟢 | 🟡 | ➖ | 2025-12-30 |
| [W-0002](active/wish-0002-doc-graph-tool.md) | DocGraph | 监护人 | 🟢 | 🟡 | ⚪ | ⚪ | ⚪ | 2025-12-30 |

> **状态符号**: ⚪ 未开始 | 🟡 进行中 | 🟢 完成 | 🔴 阻塞 | ➖ N/A

---

## 已完成 Wish (Completed)

> 暂无已完成的 Wish。

---

## 已放弃 Wish (Abandoned)

> 暂无已放弃的 Wish。

---

## Issue 汇总

### 按状态分组

| 状态 | 数量 |
|:-----|-----:|
| 🔴 Blocked | 0 |
| 🟡 InProgress | 0 |
| ⚪ Open | 0 |
| ✅ Done | 0 |
| ⏸️ Deferred | 0 |

### 详细列表

> 暂无记录的 Issue。

---

## 统计信息

| 指标 | 值 |
|:-----|:---|
| 活跃 Wish | 2 |
| 已完成 Wish | 0 |
| 已放弃 Wish | 0 |
| 阻塞 Issue | 0 |
| 总 Issue | 0 |

---

## 相关资源

- [Wish 系统规范 (L3 条款)](specs/wish-system-rules.md)
- [Wish 文档模板](templates/wish-template.md)
- [Issue 条目模板](templates/issue-template.md)

---

## 维护说明

此文件是**派生视图**，应保持与源文件同步：

1. **新增 Wish**: 在 `active/` 创建文件后，更新本文件的"活跃 Wish"表格
2. **Wish 完成**: 将文件移动到 `completed/`，更新两个表格
3. **Wish 放弃**: 将文件移动到 `abandoned/`，更新两个表格
4. **Issue 变更**: 更新"Issue 汇总"章节

> 💡 未来 W-0002（DocGraph）实现后，本文件可自动生成。
