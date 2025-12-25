# [P0-7] File Framing vs Record Layout 两层定义 — Implementation Result

## 实现摘要

在 `mvp-design-v2.md` §4.2.1 Data 文件部分添加了新的分层定义小节，明确区分 File Framing（文件级框架）与 Record Layout（记录级布局）两个层次，并将原有的 record framing 描述改为对该分层定义的展开。

## 文件变更

- [DurableHeap/docs/mvp-design-v2.md](../../DurableHeap/docs/mvp-design-v2.md) — §4.2.1 新增分层定义小节（Line 472-494）

## 修改详情

### 新增内容位置

在 §4.2.1 "实现约束（避免实现分叉）" 之后、原 "record framing" 描述之前，新增：

```markdown
##### 分层定义：File Framing vs Record Layout（MVP 固定）

为避免实现分叉，本 MVP 明确区分两个层次：

**File Framing（文件级框架）**：
- 定义：Magic-separated log 结构，用于在文件级别识别 Record 边界
- 核心元素：**Magic**（4 bytes，作为 Record 分隔符）
- 文件结构：`[Magic][Record₁][Magic][Record₂]...[Recordₙ][Magic]`
- Magic **不属于任何 Record**，它是 Record 之间的"栅栏"（fencepost）
- 空文件仅包含一个 Magic：`[Magic]`

**Record Layout（记录级布局）**：
- 定义：单条 Record 的内部结构
- 结构：`[HeadLen(u32)] [Payload] [Pad(0..3)] [TailLen(u32)] [CRC32C(u32)]`
- Record 本身不包含 Magic
- `HeadLen == TailLen`，表示 Record 总字节数

**术语约束（MUST）**：
- 规范正文讨论"文件结构/边界识别/reverse scan"时，使用 **File Framing**
- 规范正文讨论"单条记录的字段布局/CRC 覆盖范围"时，使用 **Record Layout**
- 不得将 Magic 描述为"Record 的一部分"
```

### 标题修改

原：
```markdown
record framing（Q20=A；data/meta 统一）：
```

新：
```markdown
**File Framing 详细规范**（基于上述两层定义，Q20=A，data/meta 统一）：
```

## 测试结果

- Markdown 语法验证: ✅ 结构正确
- 行号验证: ✅ 新增内容在 Line 472-494，详细规范在 Line 496

## 设计收益

1. **概念分离**：File Framing 与 Record Layout 现在是正交的概念，便于独立讨论
2. **术语约束明确**：避免实现者将 Magic 误解为 Record 的一部分
3. **层次清晰**：新读者可以快速理解两层定义的边界

## 遗留问题

无

## Changefeed Anchor

`#delta-2025-12-20-P0-7-FileFraming-RecordLayout`
