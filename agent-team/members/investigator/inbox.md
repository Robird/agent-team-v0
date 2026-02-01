# Inbox — 待处理便签

> 这是你的便签收集箱。
> 写下洞见、经验、状态变更，MemoryPalaceKeeper 会定期整理归档。
>
> **格式**：每条便签用 `## 便签 YYYY-MM-DD HH:MM` 开头，用 `---` 分隔。

---

## 便签 2026-02-01 15:30
**类型**：Route
**项目**：RBF

### RBF v0.32→v0.40 差异调查快速路径

**意图**："想了解 RBF 帧格式变更" → 直接读这三个文件

| 目标 | 位置 | 关键信息 |
|:-----|:-----|:---------|
| 新规范 | [rbf-format.md](atelia/docs/Rbf/rbf-format.md) | @[F-FRAMEBYTES-LAYOUT]、@[F-FRAME-DESCRIPTOR-LAYOUT] |
| 旧测试向量 | [rbf-test-vectors.md](atelia/docs/Rbf/rbf-test-vectors.md) | §1.6 FrameStatus 位域已废弃 |
| 实现代码 | [RbfLayout.cs](atelia/src/Rbf/Internal/RbfLayout.cs) | FrameLayout、MinFrameLength=24 |
| 差异报告 | [RBF-v0.40-diff-INV.md](agent-team/handoffs/RBF-v0.40-diff-INV.md) | 完整对照表+修订建议 |

**置信度**: ✅ 验证过

---

## 便签 2026-02-01 15:32
**类型**：Anchor
**项目**：RBF

### TrailerCodeword 解析代码锚点

| 锚点 | 位置 |
|:-----|:-----|
| TrailerCodewordData 结构定义 | [TrailerCodewordHelper.cs#L10-L35](atelia/src/Rbf/Internal/TrailerCodewordHelper.cs#L10-L35) |
| Parse 方法 | [TrailerCodewordHelper.cs#L66-L73](atelia/src/Rbf/Internal/TrailerCodewordHelper.cs#L66-L73) |
| BuildDescriptor 方法 | [TrailerCodewordHelper.cs#L82-L93](atelia/src/Rbf/Internal/TrailerCodewordHelper.cs#L82-L93) |
| CRC 校验 | [TrailerCodewordHelper.cs#L110-L113](atelia/src/Rbf/Internal/TrailerCodewordHelper.cs#L110-L113) |
| ParseAndValidate 入口 | [TrailerCodewordHelper.cs#L121-L140](atelia/src/Rbf/Internal/TrailerCodewordHelper.cs#L121-L140) |

**置信度**: ✅ 验证过

---

## 便签 2026-02-01 15:35
**类型**：Gotcha
**项目**：RBF

### 测试向量文档与规范版本不匹配

- **现象**: `rbf-test-vectors.md` 声明对齐 v0.32 但实际使用 v0.13 旧布局（FrameStatus/单CRC/MinLen=20）
- **后果**: 基于该文档写的测试代码会与当前 v0.40 实现完全不兼容
- **规避**: 看 [RBF-v0.40-diff-INV.md](agent-team/handoffs/RBF-v0.40-diff-INV.md) §7 的修订建议，需要大规模重写 §1-§2

---

<!-- 在下方添加你的便签 -->
