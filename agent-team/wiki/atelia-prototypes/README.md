# Atelia Prototypes 知识库

> 最后更新: 2025-12-09
> 维护者: 所有 Specialist 共同维护
> 源码核实: Investigator @ 2025-12-09

## 项目概述

**atelia/prototypes** 是自研 Agent 技术栈的实验场，包含 LLM 服务抽象、Agent 引擎、Tool 包装等核心组件。

## 项目结构（源码核实）

```
atelia/prototypes/
├── Completion.Abstractions/   # LLM 抽象层
│   ├── ICompletionClient.cs   # 统一客户端接口
│   ├── IHistoryMessage.cs     # 消息中间表示 (Observation/Action/ToolResults)
│   ├── CompletionRequest.cs   # 请求参数
│   ├── CompletionChunk.cs     # 流式响应块
│   ├── ToolDefinition.cs      # Tool 元数据定义 (record)
│   └── ParsedToolCall.cs      # 解析后的工具调用
│
├── Agent.Core/                # Agent 运行时引擎
│   ├── AgentEngine.cs         # 状态机核心 (事件驱动)
│   ├── AgentPrimitives.cs     # AgentRunState 枚举等基础类型
│   ├── MethodToolWrapper.cs   # 反射包装 C# 方法为 Tool
│   ├── ITool.cs               # 工具接口
│   ├── IApp.cs                # 应用接口 (含 Window 渲染)
│   ├── LevelOfDetailContent.cs # Basic/Detail 双层内容
│   ├── LlmProfile.cs          # LLM 配置
│   ├── History/               # 历史管理
│   │   ├── AgentState.cs      # 状态管理器 (RecentHistory + Notifications)
│   │   ├── HistoryEntry.cs    # 历史条目基类及派生类
│   │   ├── RecapBuilder.cs    # Recap 编辑器
│   │   └── CompletionAccumulator.cs
│   ├── Tool/                  # 工具执行
│   │   ├── ToolExecutor.cs    # 工具调度器
│   │   └── ToolContracts.cs   # 执行结果类型
│   └── App/                   # 应用宿主
│       └── DefaultAppHost.cs
│
├── Completion/                # Provider 实现
│   └── Anthropic/             # ✅ 仅 Anthropic 实现
│       ├── AnthropicClient.cs
│       ├── AnthropicApiModels.cs
│       ├── AnthropicMessageConverter.cs
│       └── AnthropicStreamParser.cs
│
├── Agent/                     # 应用层示例
│   ├── CharacterAgent.cs
│   ├── Apps/                  # 内置 App
│   │   ├── MemoryNotebookApp.cs
│   │   └── RecapBuilderApp.cs
│   ├── SubAgents/
│   │   └── RecapMaintainer.cs
│   └── Text/                  # 文本编辑工具
│       ├── TextEditorWidget.cs
│       ├── TextReplacementEngine.cs
│       └── ...
│
└── LiveContextProto/          # 控制台原型 TUI
    ├── Program.cs
    ├── ConsoleTui.cs
    └── README.md              # 有独立 README
```

## 三层 LLM 调用模型（概念图）

```
┌─────────────────────────────────────────────────────────────────┐
│                      History 层                                  │
│   HistoryEntry.cs — 丰富的历史记录（含元数据、工具调用等）       │
│   AgentState.cs — RecentHistory 管理 + 通知队列                  │
└───────────────────────────────┬─────────────────────────────────┘
                                │ RenderLiveContext()
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    中间表示层                                    │
│   IHistoryMessage.cs — 跨 Model/Provider 的通用消息格式         │
│   HistoryMessageKind: Observation | Action | ToolResults        │
└───────────────────────────────┬─────────────────────────────────┘
                                │ Provider 适配
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Provider 层                                   │
│   ICompletionClient.cs — 统一接口                               │
│   AnthropicClient.cs — Anthropic Messages API 流式实现          │
└─────────────────────────────────────────────────────────────────┘
```

**设计优势**：
- History 层保留完整信息，便于调试和回放
- 中间表示层实现 Provider 无关
- Provider 层处理具体 API 差异

## 核心组件详解

### 1. Completion.Abstractions (✅ 稳定)

LLM 服务的抽象层，采用 RL 术语（Observation/Action）而非聊天术语（User/Assistant）。

| 文件 | 职责 |
|------|------|
| `ICompletionClient.cs` | 统一客户端接口，`StreamCompletionAsync()` 返回 `IAsyncEnumerable<CompletionChunk>` |
| `IHistoryMessage.cs` | 消息中间表示：`IActionMessage`, `ObservationMessage`, `ToolResultsMessage` |
| `ToolDefinition.cs` | Tool 定义 record：`Name`, `Description`, `Parameters` |
| `ToolParamSpec` | 参数规格：类型 (`ToolParamType`)、可空性、默认值 |
| `ParsedToolCall.cs` | 解析后的工具调用信息 |
| `TokenUsage` | Token 使用统计 |

### 2. Agent.Core (✅ 稳定)

Agent 运行时引擎，核心是 `AgentEngine` 状态机。

#### AgentEngine 状态机

```
AgentRunState:
├── WaitingInput       ← 等待外部输入
├── PendingInput       ← 有输入待处理
├── PendingToolResults ← 调用模型后有 ToolCalls 待执行
├── WaitingToolResults ← 工具执行中
└── ToolResultsReady   ← 工具结果就绪，待追加到历史
```

**事件钩子**：
- `WaitingInput` — 等待输入时触发
- `BeforeModelCall` / `AfterModelCall` — 模型调用前后
- `BeforeToolExecute` / `AfterToolExecute` — 工具执行前后
- `StateTransition` — 状态转换时

#### MethodToolWrapper

反射 + Attribute 自动包装 C# 函数为 `ITool`：

```csharp
[Tool("search_files", "Search for files matching a pattern")]
public ValueTask<LodToolExecuteResult> SearchFiles(
    [ToolParam("The glob pattern")] string pattern,
    CancellationToken cancellationToken
) { /* ... */ }
```

**规则**：
- 返回类型必须是 `ValueTask<LodToolExecuteResult>`
- 最后一个参数必须是 `CancellationToken`
- 支持类型：`string`, `bool`, `int`, `long`, `float`, `double`, `decimal` 及其可空版本
- 自动推断可空性和默认值

#### History 子系统

| 文件 | 职责 |
|------|------|
| `AgentState.cs` | 管理 RecentHistory（内存）+ 待处理通知队列 |
| `HistoryEntry.cs` | 基类及派生：`ActionEntry`, `ObservationEntry`, `ToolResultsEntry`, `RecapEntry` |
| `RecapBuilder.cs` | 用于 Recap 编辑的快照工具 |
| `CompletionAccumulator.cs` | 聚合流式响应为完整条目 |

### 3. Provider 实现

| Provider | 状态 | 文件 |
|----------|------|------|
| Anthropic | ✅ 可用 | `Completion/Anthropic/AnthropicClient.cs` |
| OpenAI | ❌ 未实现 | — |
| Azure | ❌ 未实现 | — |

**AnthropicClient** 实现 `ICompletionClient`：
- API Version: `2023-06-01`
- ApiSpecId: `messages-v1`
- 流式 SSE 解析

### 4. Agent (应用层)

| 组件 | 职责 |
|------|------|
| `CharacterAgent.cs` | 主 Agent 角色 |
| `Apps/MemoryNotebookApp.cs` | 记忆笔记本 App |
| `Apps/RecapBuilderApp.cs` | Recap 构建 App |
| `SubAgents/RecapMaintainer.cs` | 自动 Recap 维护 |
| `Text/*` | 文本编辑工具集（替换、定位等） |

### 5. LiveContextProto

最小原型控制台 TUI，验证三段式流水线。

**控制台命令**：
- 直接输入文本：触发 LLM 调用
- `/history`：打印当前上下文
- `/reset`：清空历史
- `/notebook view|set|clear`：记忆笔记本操作
- `/exit`：退出

## LevelOfDetail 机制

```csharp
public enum LevelOfDetail { Basic, Detail }
```

- `LevelOfDetailContent` 包含 Basic 和 Detail 两级内容（全量替代，非增量）
- `RenderLiveContext()` 时最新的 Observation 使用 Detail，其余使用 Basic
- 用于渐进式压缩历史

## 已知问题 / 待完善

| 问题 | 代码现状 | 优先级 |
|------|---------|--------|
| 缺乏 OpenAI Provider | 仅 Anthropic 实现 | P2 |
| 缺乏多模态支持 | 仅文本 | P2 |
| 缺乏 thinking/reasoning 支持 | 有 TODO 注释 | P2 |
| 持久化层未实现 | AgentState 仅内存 | P2 |
| HistoryLimitOptions 未实现 | 无自动容量控制 | P3 |

## 与上层的关系

```
LiveContextProto (控制台原型)
       │
       ▼
    Agent (应用层)
       │
       ▼
  Agent.Core (引擎)
       │
       ├─────────────────┐
       ▼                 ▼
Completion.Abstractions  Completion/Anthropic
```

## 技术栈

- **.NET 9.0**
- **System.Text.Json** — JSON 序列化
- **HttpClient** — HTTP 请求
- **IAsyncEnumerable** — 流式响应

## 参考文档

- [LiveContextProto/README.md](../../../atelia/prototypes/LiveContextProto/README.md) — 原型说明
