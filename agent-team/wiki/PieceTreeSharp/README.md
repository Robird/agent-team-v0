# PieceTreeSharp 知识库

> 最后更新: 2025-12-09
> 维护者: 所有 Specialist 共同维护
> 源码核实: Investigator @ 2025-12-09

## 项目概述

**PieceTreeSharp** 是将 VS Code 的 TypeScript 编辑器核心移植为 C# 类库的项目（.NET 9.0 + xUnit）。

**定位**：DocUI 系统的文本建模基础，为 LLM Agent 打造 LLM-Native 编程 IDE。

## 项目结构

```
PieceTreeSharp/
├── PieceTree.sln              # 解决方案文件
├── AGENTS.md                  # 跨会话记忆（注入上下文）
├── src/
│   ├── README.md              # 项目简介
│   └── TextBuffer/            # 主类库
│       ├── TextBuffer.csproj
│       ├── Core/              # 🟢 核心：Piece Table 实现
│       ├── Cursor/            # 🟡 外围：光标/选区操作
│       ├── Decorations/       # 🟡 外围：装饰系统
│       ├── Diff/              # 🟡 外围：差异比较
│       ├── DocUI/             # 🟡 外围：Find 控制器
│       ├── Rendering/         # 🟡 外围：Markdown 渲染
│       ├── Services/          # 🟡 外围：服务接口
│       ├── Snippet/           # 🟡 外围：代码片段
│       ├── PortingDrafts/     # 📁 移植草稿
│       ├── TextModel.cs       # 🟢 核心：文本模型入口
│       ├── PieceTreeBuffer.cs # 🟢 核心：Buffer 外观
│       └── ...                # 其他顶层文件
├── tests/
│   └── TextBuffer.Tests/      # 1158+ tests
│       ├── Cursor/            # 光标相关测试子目录
│       ├── DocUI/             # DocUI 相关测试子目录
│       ├── Helpers/           # 测试辅助类
│       ├── Snapshots/         # 快照测试数据
│       └── *.cs               # 各模块测试文件（50+）
├── tools/                     # 工具脚本
│   ├── copilotmd_skeleton.py
│   └── patch_copilot_prompt.py
├── docs/                      # 文档目录
│   ├── meetings/
│   ├── plans/                 # 规划文档（含 llm-native-editor-features.md）
│   ├── prompts/
│   ├── reports/
│   ├── sprints/
│   └── tasks/
└── BugRepro/                  # Bug 复现项目（空）
```

## 架构分层：核心 vs 外围

### 🟢 核心层（稳定，对齐 TS 原版）

| 目录/文件 | 文件数 | 说明 |
|-----------|--------|------|
| `Core/` | 20 | Piece Table 数据结构、搜索、快照 |
| `TextModel.cs` | 1 | 文本模型主入口 |
| `PieceTreeBuffer.cs` | 1 | Buffer 外观类 |
| `EditStack.cs` | 1 | 编辑栈 |

**Core/ 详细内容**：
- `PieceTreeModel.cs` + `.Edit.cs` + `.Search.cs` — Piece Tree 核心
- `PieceTreeBuilder.cs` — 构建器
- `PieceTreeSnapshot.cs` — 快照支持
- `PieceTreeSearcher.cs` + `PieceTreeSearchCache.cs` — 搜索实现
- `ChunkBuffer.cs` + `ChunkUtilities.cs` — 分块缓冲
- `LineStarts.cs` + `TextMetadataScanner.cs` — 行索引
- `Range.Extensions.cs` + `Selection.cs` — 范围/选区类型
- `ReplacePattern.cs` + `SearchTypes.cs` — 搜索替换

### 🟡 外围层（功能完整，边界待定义）

| 目录 | 文件数 | 说明 |
|------|--------|------|
| `Cursor/` | 15 | 光标、多光标、词操作、Snippet Session |
| `Decorations/` | 8 | 装饰系统、IntervalTree |
| `Diff/` | 15+ | 差异算法（含 Algorithms/ 子目录） |
| `DocUI/` | 5 | FindModel、FindDecorations、FindReplaceState |
| `Rendering/` | 3 | MarkdownRenderer |
| `Services/` | 2 | 服务接口（Language、UndoRedo） |
| `Snippet/` | 1 | Transform（FormatString + Regex） |

### 边界定义

**核心层特征**：
- 直接移植自 VS Code `pieceTreeTextBuffer`
- 数据结构层，无 UI 依赖
- 稳定，变更需要与 TS 原版对齐

**外围层特征**：
- 移植自 VS Code editor 其他部分
- 功能层，可能需要 LLM-Native 适配
- 未来可能独立于 TS 原版演化

**待决策**：是否将核心/外围拆分为独立 crate/repo

## 测试基线

| 指标 | 数值 |
|------|------|
| Passed | 1158 |
| Skipped | 9 |
| Total | 1167 |
| Duration | ~2 min |

**测试命令**：
```bash
cd /repos/focus/PieceTreeSharp
export PIECETREE_DEBUG=0 && dotnet test tests/TextBuffer.Tests/TextBuffer.Tests.csproj --nologo
```

**测试文件分布**（50+ 文件）：
- PieceTree 相关：14 文件（Base, Buffer, Builder, Deterministic, Fuzz, Search 等）
- Cursor 相关：7 文件（Collection, Core, Word, MultiCursor 等）
- Decoration 相关：4 文件
- Diff 相关：2 文件
- DocUI 相关：9 文件（在 DocUI/ 子目录）
- Snippet 相关：4 文件
- TextModel 相关：6 文件
- 其他：4 文件

## 移植原则

1. **移植优于重写**：尽量对齐 TS 原版的设计、实现、测试
2. **遇到疑惑时对照 TS 原版**：答案往往就在那里
3. **增强保留原则**：C# 实现优于原版时，保留增强
4. **快速胜利优先**：低复杂度高产出的任务优先
5. **LLM-Native 筛选**：非 LLM 场景功能可降级或跳过

## TS 原版参考

| C# 模块 | TS 原版位置 |
|---------|-------------|
| Core/ | `vscode/src/vs/editor/common/model/pieceTreeTextBuffer/` |
| Cursor/ | `vscode/src/vs/editor/common/cursor/` |
| Diff/ | `vscode/src/vs/editor/common/diff/` |
| Decorations/ | `vscode/src/vs/editor/common/model/` |

## 关键文档

| 文档 | 路径 | 说明 |
|------|------|------|
| 跨会话记忆 | `AGENTS.md` | 每次注入上下文 |
| LLM-Native 功能筛选 | `docs/plans/llm-native-editor-features.md` | 功能移植优先级 |
| 迁移日志 | `docs/reports/migration-log.md` | 详细变更记录 |

## 已知问题

| 问题 | 状态 | 说明 |
|------|------|------|
| 外围功能边界模糊 | 🔄 待定义 | 需要进一步挖掘 DocUI 需求 |
| 可能需要拆分 repo | 🔄 待决策 | 核心保持对齐，外围独立演化 |
| 9 个 skipped 测试 | ⚠️ 已知 | 部分功能降级/跳过 |

## 技术栈

- **.NET 9.0**
- **xUnit** — 测试框架
- **Nullable enable** — 空安全
- **InternalsVisibleTo** — 测试访问内部成员
