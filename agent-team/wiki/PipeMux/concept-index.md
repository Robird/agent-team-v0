# PipeMux 概念索引

> 最后更新: 2026-01-03
> 维护者: Investigator + Implementer 共同维护
> 用途: 快速定位概念→代码/文档位置，减少调查跳数

## 项目定位
Local Process Orchestration via Named Pipes — 面向 LLM Agent 的本地进程编排框架，通过 Named Pipe 实现 CLI 与后台持久进程的有状态通信。

### 核心概念索引

#### 架构层

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| 三层架构 | [pipemux-broker-architecture.md](../../../PipeMux/docs/pipemux-broker-architecture.md#三层结构) | — | CLI → Broker → Backend App 的分层设计 |
| Broker | [README.md](../../../PipeMux/docs/README.md#核心概念) | [PipeMux.Broker/](../../../PipeMux/src/PipeMux.Broker/) | 中转层，进程管理 + Named Pipe 路由 |
| CLI | [README.md](../../../PipeMux/docs/README.md#使用-cli) | [PipeMux.CLI/](../../../PipeMux/src/PipeMux.CLI/) | 前端命令行工具 |
| SDK | [README.md](../../../PipeMux/docs/README.md#使用-pipemuxsdk) | [PipeMux.Sdk/](../../../PipeMux/src/PipeMux.Sdk/) | App 开发框架 |
| Shared/Protocol | — | [PipeMux.Shared/Protocol/](../../../PipeMux/src/PipeMux.Shared/Protocol/) | 共享协议定义 |

#### Broker 核心

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| BrokerServer | — | [BrokerServer.cs](../../../PipeMux/src/PipeMux.Broker/BrokerServer.cs) | Named Pipe 服务器主循环 |
| ProcessRegistry | — | [ProcessRegistry.cs](../../../PipeMux/src/PipeMux.Broker/ProcessRegistry.cs) | 后台进程生命周期管理 |
| AppProcess | — | [ProcessRegistry.cs#L63](../../../PipeMux/src/PipeMux.Broker/ProcessRegistry.cs#L63) | 单个后台进程封装，StreamJsonRpc 通信 |
| BrokerConfig | — | [BrokerConfig.cs](../../../PipeMux/src/PipeMux.Broker/BrokerConfig.cs) | TOML 配置模型 |
| ManagementHandler | — | [ManagementHandler.cs](../../../PipeMux/src/PipeMux.Broker/ManagementHandler.cs) | `:list` / `:ps` / `:stop` 管理命令处理 |

#### 通信协议

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| Request | — | [Request.cs](../../../PipeMux/src/PipeMux.Shared/Protocol/Request.cs) | CLI → Broker 请求格式 |
| Response | — | [Response.cs](../../../PipeMux/src/PipeMux.Shared/Protocol/Response.cs) | Broker → CLI 响应格式 |
| ManagementCommand | — | [ManagementCommand.cs](../../../PipeMux/src/PipeMux.Shared/Protocol/ManagementCommand.cs) | 管理命令枚举 |
| JsonRpc | — | [JsonRpc.cs](../../../PipeMux/src/PipeMux.Shared/Protocol/JsonRpc.cs) | JSON 序列化工具 |
| StreamJsonRpc | [架构文档](../../../PipeMux/docs/pipemux-broker-architecture.md#1-通信协议) | — | Broker↔App 的 JSON-RPC 通信（微软库） |

#### 多终端隔离

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| TerminalIdentifier | [终端标识机制](../../../PipeMux/docs/README.md#终端标识机制) | [TerminalIdentifier.cs](../../../PipeMux/src/PipeMux.Shared/TerminalIdentifier.cs) | 跨平台终端会话检测 |
| VS Code 终端检测 | [terminal-identifier-research.md](../../../PipeMux/docs/terminal-identifier-research.md) | [TerminalIdentifier.cs#L172](../../../PipeMux/src/PipeMux.Shared/TerminalIdentifier.cs#L172) | 从 `VSCODE_IPC_HOOK_CLI` 提取 UUID |
| TerminalIdInfo | — | [TerminalIdentifier.cs#L261](../../../PipeMux/src/PipeMux.Shared/TerminalIdentifier.cs#L261) | 终端标识解析结果 |

#### SDK/App 开发

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| PipeMuxApp | [开发自己的 App](../../../PipeMux/docs/README.md#使用-pipemuxsdk) | [PipeMuxApp.cs](../../../PipeMux/src/PipeMux.Sdk/PipeMuxApp.cs) | SDK 核心类，接管 stdin/stdout |
| InvokeResult | — | [InvokeResult.cs](../../../PipeMux/src/PipeMux.Sdk/InvokeResult.cs) | App 执行结果封装 |
| Calculator 示例 | — | [samples/Calculator/Program.cs](../../../PipeMux/samples/Calculator/Program.cs) | RPN 有状态栈式计算器 |

### 常见调查路径

#### "我想了解整体架构"
1. 读 [AGENTS.md](../../../PipeMux/AGENTS.md) 了解最新进展
2. 读 [pipemux-broker-architecture.md](../../../PipeMux/docs/pipemux-broker-architecture.md) 了解三层架构设计
3. 读 [README.md](../../../PipeMux/docs/README.md) 了解使用方法

#### "我想了解 Broker 如何管理进程"
1. 从 [BrokerServer.cs](../../../PipeMux/src/PipeMux.Broker/BrokerServer.cs) 的 `HandleRequestAsync()` 开始
2. 看 [ProcessRegistry.cs](../../../PipeMux/src/PipeMux.Broker/ProcessRegistry.cs) 的 `Start()` 和 `Get()`
3. 看 `AppProcess` 类了解 StreamJsonRpc 通信

#### "我想了解多终端隔离原理"
1. 读 [terminal-identifier-research.md](../../../PipeMux/docs/terminal-identifier-research.md) 了解调研背景
2. 看 [TerminalIdentifier.cs](../../../PipeMux/src/PipeMux.Shared/TerminalIdentifier.cs) 的跨平台实现
3. 看 `BrokerServer.HandleRequestAsync()` 中 `processKey` 的生成逻辑

#### "我想开发一个 PipeMux App"
1. 读 [README.md#使用-pipemuxsdk](../../../PipeMux/docs/README.md#使用-pipemuxsdk)
2. 看 [samples/Calculator/Program.cs](../../../PipeMux/samples/Calculator/Program.cs) 作为模板
3. 看 [PipeMuxApp.cs](../../../PipeMux/src/PipeMux.Sdk/PipeMuxApp.cs) 了解 SDK 内部机制

#### "我想了解 CLI 如何与 Broker 通信"
1. 从 [PipeMux.CLI/Program.cs](../../../PipeMux/src/PipeMux.CLI/Program.cs) 开始
2. 看 [BrokerClient.cs](../../../PipeMux/src/PipeMux.CLI/BrokerClient.cs) 的 Named Pipe 连接
3. 看 [Request.cs](../../../PipeMux/src/PipeMux.Shared/Protocol/Request.cs) / [Response.cs](../../../PipeMux/src/PipeMux.Shared/Protocol/Response.cs) 了解协议格式

### 技术栈速查

| 技术 | 用途 | 相关代码 |
|:-----|:-----|:---------|
| StreamJsonRpc | Broker↔App JSON-RPC 通信 | `ProcessRegistry.cs`, `PipeMuxApp.cs` |
| Named Pipe | CLI↔Broker 通信 | `BrokerServer.cs`, `BrokerClient.cs` |
| System.CommandLine | CLI 参数解析 + App 命令定义 | `PipeMuxApp.cs`, `samples/Calculator/` |
| Tomlyn | TOML 配置解析 | `ConfigLoader.cs` |
