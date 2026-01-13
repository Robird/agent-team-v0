---
docId: "W-0010-issues"
title: "W-0010 Issues"
produce_by:
  - "wish/W-0010-design-dsl-impl/wish.md"
---

# W-0010 Issues

> **当前状态**: 无阻塞

## Active Issues

（暂无）

## Resolved Issues

### ISSUE-002: Setext Heading 处理策略 ✅

**发现时间**: 2026-01-13
**解决时间**: 2026-01-13
**解决方案**: 选项 B（严格 ATX-only）

**问题描述**:
当前 `AtxSectionSplitter` 检查所有 `HeadingBlock`（不区分 ATX 和 Setext），可能处理非预期的文档结构。

**解决方案**:
- 添加 `IsAtxHeading()` 辅助方法，检查 `!heading.IsSetext`
- 只按 ATX Heading 分段，Setext Heading 作为普通 Block 归入 Content
- 新增 Case 9 测试用例验证 Setext Heading 行为
- 符合 DSL 规范设计意图

### ISSUE-001: HeadingTextExtractor 对 CodeInline 处理丢失反引号 ✅

**发现时间**: 2026-01-13
**解决时间**: 2026-01-13
**解决方案**: 方案 A（Span 切片）

**问题描述**:
当前 `HeadingTextExtractor.ExtractText()` 对 `CodeInline` 取 `.Content` 属性，丢失了反引号，导致 Term 模式匹配无法工作。

**解决方案**:
- 使用 Markdig 的 `Span` 属性 + 原始 Markdown 字符串切片
- 修改接口签名，传入 `originalMarkdown` 参数
- 影响文件：HeadingTextExtractor, INodeBuilder, DefaultNodeBuilder, NodeBuilderPipeline, AxtTreeBuilder
