# DocUI 知识库

> 最后更新: 2025-12-09
> 维护者: 所有 Specialist 共同维护
> 源码核实: Investigator @ 2025-12-09

## 项目概述

**DocUI** 是面向 LLM Agent 的纯文本 TUI 框架，目标是让 LLM 通过 Markdown 进行有状态的多轮交互。

**定位**：LLM 的自主上下文管理系统，"Model-View-Control 一体封装为 Widget"。

**核心机制**（摘自 README）：
- TUI 与 Tool 建立绑定机制
- 多个 Widget 聚合成 App
- 以 App 为单位，3 种信息注入方式：
  1. History（永久注入）：随 User/Tool 消息注入，含 Basic/Detail LOD
  2. Window（最新状态）：每次构建 LLM 上下文时唯一注入
  3. Tool（可见工具）：调用 LLM 前注入当前可用工具

## 项目结构

```
DocUI/
├── DocUI.sln              # 解决方案（含 3 个项目）
├── AGENTS.md              # 跨会话记忆（详细的设计决策和最新记忆）
├── README.md              # 项目愿景
├── src/
│   ├── DocUI.Text.Abstractions/    # 🔄 抽象层（草稿状态）
│   │   ├── DocUI.Text.Abstractions.csproj
│   │   └── ITextReadOnly.cs        # 注释掉的接口草稿
│   └── DocUI.Text/                 # ✅ 核心实现（可编译）
│       ├── DocUI.Text.csproj
│       ├── StructList.cs           # 零分配值类型列表
│       ├── SegmentListBuilder.cs   # 段列表构建器（行/列/偏移）
│       ├── OverlayBuilder.cs       # 渲染期叠加层生成器
│       ├── IKeySelector.cs         # 二分查找零开销抽象
│       └── OverlayImmutable.cs     # 不可变叠加
├── tests/
│   └── DocUI.Text.Tests/           # ✅ 24 tests passed
│       ├── StructListBasicTests.cs
│       ├── StructListAdvancedTests.cs
│       ├── StructListEnumeratorTests.cs
│       └── OverlayBuilderTests.cs
├── demo/
│   └── TextEditor/                 # ⚠️ 不在 sln 中，跨项目演示
│       ├── EditorSession.cs
│       ├── MarkdownRenderer.cs
│       ├── TextEditorService.cs
│       └── Program.cs
├── docs/
│   ├── design/                     # 设计文档
│   │   ├── concept.md
│   │   ├── TEA-style.md
│   │   ├── text-buffer-pipeline.md
│   │   └── samples/
│   └── todo/                       # 待办事项
├── agents/
│   └── SegmentSnapshotDev.md       # Agent 开发笔记
└── reference/                      # 外部参考
    ├── asciidoc-lang/
    └── cmark-gfm/
```

## 代码现状

### ✅ 已实现（可编译，有测试）

| 组件 | 文件 | 说明 |
|------|------|------|
| `StructList<T>` | StructList.cs (~400 LOC) | 零分配值类型列表，ref 返回避免复制，池化支持 |
| `SegmentListBuilder` | SegmentListBuilder.cs (~350 LOC) | 段列表操作器，支持行/列/偏移坐标系 |
| `OverlayBuilder` | OverlayBuilder.cs (~200 LOC) | 声明式叠加层，坐标不漂移 |
| `IKeySelector<T,TKey>` | IKeySelector.cs | static abstract interface 实现零开销二分查找 |

### 🔄 草稿/待设计

| 组件 | 状态 | 说明 |
|------|------|------|
| `ITextReadOnly` | 注释掉 | 核心接口设计草稿 |
| Widget 系统 | 概念 | 尚未开始实现 |
| App 聚合 | 概念 | 尚未开始实现 |
| LOD 管理 | 概念 | 尚未开始实现 |

### ⚠️ demo/TextEditor（不在 sln 中）

这是一个**跨项目演示**，展示了"选区可视化"概念：

```xml
<!-- 依赖外部项目（路径不正确，需要手动调整） -->
<ProjectReference Include="..\PipeMux.Shared\PipeMux.Shared.csproj" />
<ProjectReference Include="..\TextBuffer\TextBuffer.csproj" />
```

实现了：
- `EditorSession` — 持有 TextModel 和光标状态
- `MarkdownRenderer` — 将 TextModel 渲染为 Markdown（含行号、光标标记）
- `TextEditorService` — JSON-RPC 请求分发

## 测试基线

| 指标 | 数值 |
|------|------|
| Passed | 24 |
| Skipped | 0 |
| Total | 24 |
| Duration | ~1.7s |

**测试命令**：
```bash
cd /repos/focus/DocUI
dotnet test tests/DocUI.Text.Tests/DocUI.Text.Tests.csproj --nologo
```

## 技术栈与决策

| 决策 | 选择 | 状态 |
|------|------|------|
| 语言 | C# + .NET 9.0 | ✅ 已确定 |
| 格式 | GitHub Flavored Markdown | ✅ 已确定 |
| 架构 | 即时模式 + Elm Architecture | ✅ 已确定 |
| 基础 | Fork Terminal.Gui v2 去渲染化 | 🔄 计划中 |
| 参考 | Helix (选区), Textual (样式), Bubble Tea (状态) | 📚 |

## 设计方向

### TEA 架构 (Elm Architecture)

```
┌─────────┐    ┌─────────┐    ┌─────────┐
│  Model  │───▶│  View   │───▶│Markdown │
│ (数据)  │    │(渲染器) │    │ (输出)  │
└────┬────┘    └─────────┘    └─────────┘
     │
     │ Update
     │
┌────┴────┐
│ Message │◀─── LLM 输入
└─────────┘
```

### 与 MCP 的定位差异

| 维度 | MCP | DocUI + LiveContext |
|------|-----|---------------------|
| 层级 | 协议层（如何调用工具） | 应用层（有状态多轮交互） |
| 状态 | 无状态工具调用 | **有状态 App** |
| 部署 | 需要服务端 | **本地运行，易集成** |
| 上下文控制 | 被动 | **主动 LOD + 信息折叠** |
| 信息新鲜度 | 堆积历史 | **仅注入 Live 信息** |

## 依赖关系

```
DocUI.Text
└── DocUI.Text.Abstractions

demo/TextEditor (不在 sln 中)
├── PipeMux.Shared (外部 - 需要路径修复)
└── PieceTreeSharp/TextBuffer (外部 - 需要路径修复)
```

**注意**：核心库 `DocUI.Text` 目前**无外部依赖**，与 PieceTreeSharp/PipeMux 是独立的。

## 设计文档索引

| 文档 | 路径 | 说明 |
|------|------|------|
| 跨会话记忆 | `AGENTS.md` | 每次注入上下文，含详细设计决策 |
| TEA 架构 | `docs/design/TEA-style.md` | Elm Architecture 探索 |
| Text Buffer Pipeline | `docs/design/text-buffer-pipeline.md` | 缓冲层设计 |
| 概念设计 | `docs/design/concept.md` | 核心概念 |

## AGENTS.md 关键记忆（摘要）

最新开发进展（来自 AGENTS.md）：
- 2025-11-30：`OverlayBuilder` 重构为接收 `SegmentListBuilder` 参数
- 2025-11-27：`StructList<T>` 引入 `_version` + fail-fast 枚举器
- 2025-11-27：`StructList<T>.BinarySearchBy` 重构为 static abstract interface
- 2025-11-21：`SegmentSnapshot` 引入集中式 `SegmentLineBuilder`

## 已知问题

| 问题 | 状态 | 说明 |
|------|------|------|
| ~~demo/TextEditor 路径不正确~~ | ✅ 已修复 | 已更正引用路径并加入 sln |
| Widget 系统未实现 | 🔄 待设计 | 只有概念，无代码 |
| ITextReadOnly 是草稿 | 🔄 待完善 | 注释掉的接口定义 |
| App 范式未落地 | 🔄 概念阶段 | 需要更多实验 |

## 与其他项目的关系

| 项目 | 关系 | 说明 |
|------|------|------|
| PieceTreeSharp | 概念依赖 | DocUI 未来可能使用 PieceTree 作为持久缓冲 |
| PipeMux | 演示集成 | demo/TextEditor 使用 PipeMux.Shared 协议 |
| atelia/prototypes | 同层 | 都是 LLM-Native 实验项目 |

## 下一步建议

1. **修复 demo/TextEditor**：调整项目引用路径或移入 sln
2. **完善 ITextReadOnly**：取消注释并实现
3. **Widget 原型**：从 demo/TextEditor 抽象出可复用组件
4. **LOD 实验**：实现 Basic/Detail 两级信息注入

## 参考

- TEA/Elm 架构: https://guide.elm-lang.org/architecture/
- PipeMux: 作为 CLI 交互层使用
