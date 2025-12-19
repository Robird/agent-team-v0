# [DurableHeap MVP v2] Magic as Record Separator 修订 Implementation Result

> **Implementer Handoff**
> **日期**：2025-12-20
> **任务**：基于 2025-12-19 畅谈会第四轮共识，修订 mvp-design-v2.md 中 Magic 结构定义
> **依据**：`agent-team/handoffs/2025-12-20-mvp-design-v2-revision-brief-INV.md` (P0-2)

---

## 实现摘要

根据 2025-12-19 畅谈会第四轮共识（监护人建议采纳），将 Magic 从 Record 的一部分重新定义为 **Record Separator（记录分隔符）**。这次修订统一了 framing 概念，使所有 Magic 具有相同的语义。

---

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 9 处修改

---

## 源码对齐说明

| 位置 | 修改内容 | 备注 |
|------|----------|------|
| Line 393-410 | record framing 重新描述 | Magic 与 Record 并列，Record 不包含 Magic |
| Line 411-419 | Len 精确定义 | HeadLen 减少 4 字节（不含 Magic） |
| Line 420-433 | 反向扫尾算法 | 基于新的分隔符结构，RecordEnd=4 表示空文件 |
| Line 467-479 | 写入顺序步骤 | 步骤数从 9 减为 8，Record 不以 Magic 开头 |
| Line 477-482 | Magic 哨兵段落 | 重命名为"设计收益"，更新说明 |
| Line 496 | Ptr64 约束 | 指向 HeadLen 起始位置而非 Magic 位置 |
| Line 517-518 | Meta 文件章节 | 哨兵→分隔符 |
| Line 530 | DataTail 定义 | 明确 DataTail=EOF，包含尾部分隔符 Magic |
| Line 1084 | 崩溃恢复 | 截断后文件仍以 Magic 分隔符结尾 |

---

## 详细修改内容

### 1. Record Framing 描述 (核心变更)

**原描述**：
```markdown
- data 与 meta 统一采用 `ELOG` framing：
  - `[Magic(4)] [Len(u32 LE)] [Payload bytes] [Pad(0..3)] [Len(u32 LE)] [CRC32C(u32 LE)]`
  - `Len`（也称 `HeadLen`/`TailLen`）表示从 `Magic` 开始到 `CRC32C` 结束的 **整条 record 总字节长度**。
```

**新描述**：
```markdown
**Magic 是 Record Separator（记录分隔符），不属于任何 Record。**

- **文件结构**：
  - 空文件：`[Magic]`（仅分隔符）
  - 含 N 条 Record：`[Magic][Record1][Magic][Record2]...[RecordN][Magic]`
- **Record 格式**（Record 本身不包含 Magic）：
  - `[Len(u32 LE)] [Payload bytes] [Pad(0..3)] [Len(u32 LE)] [CRC32C(u32 LE)]`
```

### 2. Len 精确定义

**原公式**：
```
HeadLen = 4(Magic) + 4(HeadLen) + PayloadLen + PadLen + 4(TailLen) + 4(CRC32C)
```

**新公式**：
```
HeadLen = 4(HeadLen) + PayloadLen + PadLen + 4(TailLen) + 4(CRC32C)
```

**最小长度变化**：
- 原：`HeadLen >= 16`（固定头尾 16 字节）
- 新：`HeadLen >= 12`（固定头尾 12 字节）

### 3. 反向扫尾算法

**原算法**：
- `End == 0` 表示空文件
- `Start = End - TailLen`

**新算法**：
- `RecordEnd == 4` 表示空文件（仅包含一个 Magic 分隔符）
- `RecordStart = RecordEnd - TailLen`
- `PrevMagicPos = RecordStart - 4`（定位前一个分隔符）

### 4. Ptr64 约束

**原**：`Ptr64` 指向 `Magic` 所在 byte offset

**新**：`Ptr64` 指向 Record 的 `HeadLen` 字段起始位置（紧随分隔符 Magic 之后）

---

## 设计收益

1. **概念简洁**：所有 Magic 语义相同（分隔符），无需区分"Record 内的 Magic"和"尾部哨兵 Magic"
2. **代码统一**：forward/reverse scan 使用相同的 Magic 边界检测逻辑
3. **空间效率**：Record 格式减少 4 字节（Magic 移到 Record 外部）

---

## 测试结果

- **语法检查**：文档 Markdown 格式正确
- **一致性验证**：
  - ✅ record framing 描述与 Len 定义一致
  - ✅ 反向扫尾算法与新的文件结构一致
  - ✅ 写入顺序步骤与分隔符模型一致
  - ✅ Ptr64 约束与新的 Record 起始位置一致
  - ✅ DataTail 定义与分隔符模型一致
  - ✅ Meta 文件章节与 Data 文件章节一致

---

## 遗留问题

无。P0-2 修订已完成。

---

## QA 关注点

1. **空文件边界**：文件 `[Magic]` 时，`RecordEnd == 4` 应正确识别为空
2. **Ptr64 兼容性**：实现时需确保 `Ptr64` 指向 `HeadLen` 位置而非 `Magic` 位置
3. **DataTail 语义**：`DataTail = FileLength`，包含尾部 Magic

---

## Changefeed Anchor

`#delta-2025-12-20-magic-separator`

---

*Handoff 由 Implementer 产出，2025-12-20*
