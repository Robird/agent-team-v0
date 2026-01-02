# PieceTreeSharp 概念索引

> 最后更新: 2026-01-03
> 维护者: Investigator + Implementer 共同维护
> 用途: 快速定位概念→代码位置，减少调查跳数

## 核心概念 → 代码锚点

### 数据结构层

| 概念 | 位置 | 一句话 |
|:-----|:-----|:-------|
| **PieceTreeModel** | `src/TextBuffer/Core/PieceTreeModel.cs#L9` | 红黑树文本存储核心，源自 TS 的 `PieceTreeBase` |
| **PieceTreeNode** | `src/TextBuffer/Core/PieceTreeNode.cs` | 红黑树节点，维护长度/行数聚合 |
| **Piece** | `src/TextBuffer/Core/PieceSegment.cs` | 描述 buffer 中一个文本片段（bufferIndex + start + end） |
| **ChunkBuffer** | `src/TextBuffer/Core/ChunkBuffer.cs` | 不可变文本块存储 |
| **BufferCursor** | `src/TextBuffer/Core/PieceTreeModel.cs` | 在 buffer 中的位置（行号+列号） |

### 用户 API 层

| 概念 | 位置 | 一句话 |
|:-----|:-----|:-------|
| **TextModel** | `src/TextBuffer/TextModel.cs#L63` | 高层文本模型，封装 buffer + decorations + undo |
| **PieceTreeBuffer** | `src/TextBuffer/PieceTreeBuffer.cs#L13` | 纯文本 buffer 门面，无装饰 |
| **TextPosition** | `src/TextBuffer/TextPosition.cs` | 行号 + 列号坐标（1-based） |
| **Range** | `src/TextBuffer/Core/Range.Extensions.cs` | 文本范围（起止位置） |
| **Selection** | `src/TextBuffer/Core/Selection.cs` | 选区（锚点 + 活动点 + 方向） |

### 光标与编辑

| 概念 | 位置 | 一句话 |
|:-----|:-----|:-------|
| **Cursor** | `src/TextBuffer/Cursor/Cursor.cs` | 单光标状态 |
| **CursorCollection** | `src/TextBuffer/Cursor/CursorCollection.cs` | 多光标管理 |
| **WordOperations** | `src/TextBuffer/Cursor/WordOperations.cs` | 单词级导航（Ctrl+Left/Right） |
| **EditStack** | `src/TextBuffer/EditStack.cs` | Undo/Redo 栈 |

### 装饰系统

| 概念 | 位置 | 一句话 |
|:-----|:-----|:-------|
| **ModelDecoration** | `src/TextBuffer/Decorations/ModelDecoration.cs` | 文本装饰（高亮、标记） |
| **IntervalTree** | `src/TextBuffer/Decorations/IntervalTree.cs` | 装饰存储的区间树 |
| **DecorationsTree** | `src/TextBuffer/Decorations/DecorationsTree.cs` | 装饰管理器 |

### 差异比较

| 概念 | 位置 | 一句话 |
|:-----|:-----|:-------|
| **MyersDiffAlgorithm** | `src/TextBuffer/Diff/Algorithms/MyersDiffAlgorithm.cs` | O(ND) 差异算法 |
| **DynamicProgrammingDiffing** | `src/TextBuffer/Diff/Algorithms/DynamicProgrammingDiffing.cs` | 小文本精确比对 |
| **RangeMapping** | `src/TextBuffer/Diff/RangeMapping.cs` | 差异行映射 |

### DocUI 集成

| 概念 | 位置 | 一句话 |
|:-----|:-----|:-------|
| **FindModel** | `src/TextBuffer/DocUI/FindModel.cs` | 查找/替换模型 |
| **FindReplaceState** | `src/TextBuffer/DocUI/FindReplaceState.cs` | 查找状态（搜索词、选项） |
| **FindDecorations** | `src/TextBuffer/DocUI/FindDecorations.cs` | 搜索结果高亮 |
| **MarkdownRenderer** | `src/TextBuffer/Rendering/MarkdownRenderer.cs` | 渲染为 Markdown（含光标/选区标记） |

### Snippet 系统

| 概念 | 位置 | 一句话 |
|:-----|:-----|:-------|
| **SnippetSession** | `src/TextBuffer/Cursor/SnippetSession.cs` | 代码片段插入会话 |
| **Transform** | `src/TextBuffer/Snippet/Transform.cs` | 变量变换（大小写、正则） |

## TS → C# 源码对应

| C# 文件 | TS 源码 |
|:--------|:--------|
| `Core/PieceTreeModel.cs` | `vs/editor/common/model/pieceTreeTextBuffer/pieceTreeBase.ts` |
| `PieceTreeBuffer.cs` | `vs/editor/common/model/pieceTreeTextBuffer/pieceTreeTextBuffer.ts` |
| `TextModel.cs` | `vs/editor/common/model/textModel.ts` |
| `Core/Selection.cs` | `vs/editor/common/core/selection.ts` |
| `Decorations/IntervalTree.cs` | `vs/editor/common/model/intervalTree.ts` |
| `Cursor/CursorCollection.cs` | `vs/editor/common/cursor/cursorCollection.ts` |
| `Diff/Algorithms/MyersDiffAlgorithm.cs` | `vs/editor/common/diff/algorithms/myersDiffAlgorithm.ts` |

## 常见调查路径

### "我想了解 PieceTree 的数据结构"
1. 入口：`src/TextBuffer/Core/PieceTreeModel.cs` — 核心实现
2. 节点：`src/TextBuffer/Core/PieceTreeNode.cs` — 红黑树节点
3. 文档：`docs/` 或对照 TS 源码

### "我想改查找功能"
1. 状态：`src/TextBuffer/DocUI/FindReplaceState.cs` — 查找选项
2. 逻辑：`src/TextBuffer/DocUI/FindModel.cs` — 查找执行
3. 渲染：`src/TextBuffer/DocUI/FindDecorations.cs` — 结果高亮
4. 测试：`tests/TextBuffer.Tests/DocUI/FindModelTests.cs`

### "我想了解多光标实现"
1. 集合：`src/TextBuffer/Cursor/CursorCollection.cs` — 多光标管理
2. 单光标：`src/TextBuffer/Cursor/Cursor.cs` — 单光标状态
3. 操作：`src/TextBuffer/Cursor/WordOperations.cs` — 导航逻辑
4. 测试：`tests/TextBuffer.Tests/Cursor/`

### "我想改 Undo/Redo"
1. 栈：`src/TextBuffer/EditStack.cs` — 编辑栈实现
2. 服务接口：`src/TextBuffer/Services/IUndoRedoService.cs`
3. TextModel 集成：`src/TextBuffer/TextModel.cs` 搜索 `EditStack`

### "我想加新的装饰类型"
1. 模型：`src/TextBuffer/Decorations/ModelDecoration.cs` — 装饰定义
2. 存储：`src/TextBuffer/Decorations/IntervalTree.cs` — 区间树
3. 管理：`src/TextBuffer/Decorations/DecorationsTree.cs` — 增删查改
