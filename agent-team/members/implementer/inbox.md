# Inbox — 待处理便签

> 这是你的便签收集箱。
> 写下洞见、经验、状态变更，MemoryPalaceKeeper 会定期整理归档。
>
> **格式**：每条便签用 `## 便签 YYYY-MM-DD HH:MM` 开头，用 `---` 分隔。

---

<!-- 在下方添加你的便签 -->

## 便签 2025-12-25 10:30

**任务**：按 spec-conventions.md v0.3 修订 mvp-design-v2.md 中的 ASCII art

**经验记录**：
1. **VarInt 图**：保留教学性 ASCII art 时，只需在上方添加 `(Informative / Illustration)` 标注，并确保上方有文字/公式作为 SSOT。这是 `[S-DOC-ASCIIART-SHOULDNOT]` 的合规方式。

2. **FrameTag 位布局 → Visual Table**：将 box-drawing ASCII 框图改为 Markdown 表格时，关键是保留所有语义信息（位范围、字段名、类型、端序）。用 blockquote 补充端序说明和计算公式。

3. **Two-Phase Commit → Mermaid sequenceDiagram**：时序图改造时注意：
   - 用 `participant` 定义所有参与者（Heap、Dict、Index、DataFile、MetaFile）
   - 用 `Note over` 标注阶段分隔
   - 用 `loop` 表示循环（for each dirty object）
   - 箭头语义：`->>` 表示调用，`Note right of` 表示状态说明

4. **语义核对**：每次修改前需对照正文验证图表语义。本次核对结果：
   - VarInt：文字描述 + 公式在 §3.2.0.1，图作为补充说明，语义一致
   - FrameTag：表格与 §3.2.2 术语表条目一致（SubType=高16位=ObjectKind，RecordType=低16位）
   - Two-Phase：流程与 §3.4.4/§3.4.5 描述一致，增加了 `loop for each dirty object` 更精确表达多对象提交

---
