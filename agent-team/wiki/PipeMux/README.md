# PipeMux 知识库

> 最后更新: 2025-12-09
> 维护者: 所有 Specialist 共同维护
> 源码核实: Investigator @ 2025-12-09

## 项目概述

**PipeMux** (Pipe Multiplexer) 是本地进程编排框架，通过 Named Pipe 实现 CLI 与后台持久进程的通信。

**定位**：弥补 `run_in_terminal` 对多轮交互的不足，将多轮交互适配为多个单轮交互的外观。

## 架构

```
Terminal A              Terminal B
    │                       │
    └──── CLI ──────────────┘
              │
              ▼ Named Pipe (pipemux-broker)
        ┌─────────────┐
        │   Broker    │  进程管理 + 路由 + 终端隔离
        └──────┬──────┘
               │ StreamJsonRpc (stdin/stdout, NewLineDelimited)
        ┌──────┴──────┐
        │    Apps     │  有状态后台进程 (按 App:TerminalId 隔离)
        └─────────────┘
```

## 项目结构

| 目录 | 职责 | 稳定性 |
|------|------|--------|
| `src/PipeMux.CLI/` | 统一 CLI 前端 (System.CommandLine) | ✅ 稳定 |
| `src/PipeMux.Broker/` | 中转服务器 | ✅ 核心稳定，管理命令待暴露 |
| `src/PipeMux.Sdk/` | App 开发 SDK | ✅ 稳定 |
| `src/PipeMux.Shared/` | 协议定义 + 终端标识 | ✅ 稳定 |
| `samples/Calculator/` | RPN 计算器示例 | ✅ 稳定 |
| `samples/TerminalIdTest/` | 终端标识测试工具 | ✅ 稳定 |
| `tools/TerminalIdTest/` | 详细终端检测工具 | ✅ 稳定 |
| `tests/` | E2E 测试脚本 | ✅ 稳定 |

## 核心组件详解

### CLI (`PipeMux.CLI`)
- 使用 `System.CommandLine` 解析 `<app> <args...>` 格式
- 通过 `BrokerClient` 连接 Named Pipe 发送请求
- 自动检测终端标识符并附加到请求

### Broker (`PipeMux.Broker`)
核心类：
- `BrokerServer`: Named Pipe 服务器，处理并发连接
- `ProcessRegistry`: 管理 App 进程生命周期
- `AppProcess`: 封装进程 + StreamJsonRpc 客户端
- `ConfigLoader`: 加载 TOML 配置

**当前支持的操作**：
- 接收 CLI 请求 → 路由到对应 App
- 按 `App:TerminalId` 键值实现进程隔离
- 自动启动/重启不健康的进程
- 支持 `auto_start` 配置项自动启动 App

**内部已实现但未暴露的功能** (在 `ProcessRegistry` 中):
- `ListActive()`: 列出所有活跃进程
- `Close(appName)`: 关闭指定进程

### SDK (`PipeMux.Sdk`)
- `PipeMuxApp`: 接管 stdin/stdout，启动 StreamJsonRpc 服务
- `InvokeResult`: 统一返回格式 `{ExitCode, Output, Error}`
- 支持 `System.CommandLine` 风格的命令定义

### 终端标识 (`PipeMux.Shared.TerminalIdentifier`)
跨平台检测机制：
| 环境 | 检测方式 | 标识格式 |
|------|----------|----------|
| 环境变量覆盖 | `PIPEMUX_TERMINAL_ID` | `env:{value}` |
| VS Code 终端 | `VSCODE_IPC_HOOK_CLI` 中的 UUID | `vscode-window:{uuid}` |
| VS Code WSL | `WSL_INTEROP` | `vscode-wsl:{pid}` |
| Windows Terminal | `WT_SESSION` | `wt:{guid}` |
| 传统 Windows | `GetConsoleWindow()` | `hwnd:{hex}` |
| Linux/macOS TTY | `/proc/self/fd/0` | `tty:/dev/pts/N` |
| Session ID | `getsid()` | `sid:{id}` |

## Calculator 示例命令

RPN (逆波兰表示法) 栈式计算器：

| 命令 | 说明 |
|------|------|
| `push <value>` | 将数值压入栈 |
| `pop` | 弹出栈顶（丢弃） |
| `peek` | 查看栈状态 |
| `dup` | 复制栈顶 |
| `swap` | 交换栈顶两个值 |
| `clear` | 清空栈 |
| `add` | 弹出两值，压入和 |
| `sub` | 弹出 a, b，压入 a - b |
| `mul` | 弹出两值，压入积 |
| `div` | 弹出 a, b，压入 a / b |
| `neg` | 栈顶取反 |

## 管理命令

> 新增于 2025-12-09

PipeMux CLI 支持以 `:` 前缀的管理命令，用于查看和管理 Broker 状态：

| 命令 | 说明 |
|------|------|
| `:list` | 列出所有已注册的 App（从配置文件加载） |
| `:ps` | 列出当前运行中的进程实例 |
| `:stop <app>` | 停止指定 App 的所有实例 |
| `:help` | 显示帮助信息 |

**使用示例**：

```bash
# 日常使用（不变）
pmux calculator push 10

# 管理命令（`:` 前缀）
pmux :list              # 列出注册的 App
pmux :ps                # 列出运行中的实例
pmux :stop calculator   # 停止指定 App
pmux :help              # 显示帮助
```

## 已知问题 / 待完善

| 问题 | 代码现状 | 影响 | 优先级 |
|------|---------|------|--------|
| ~~缺少关闭 app 命令~~ | ✅ 已通过 `:stop` 命令暴露 | - | ~~P1~~ 已完成 |
| ~~缺少列出运行 app 命令~~ | ✅ 已通过 `:list` 和 `:ps` 命令暴露 | - | ~~P1~~ 已完成 |
| 需修改配置+重启才能注册新 app | 配置在启动时一次性加载 | 不便于开发迭代 | P2 |
| Broker 无法热重载配置 | `ConfigLoader.Load()` 仅启动时调用 | 每次改动需重启 | P2 |
| 目标：静默后台运行 | 当前需要前台运行 | 依赖上述功能完善 | 目标 |

## 配置文件

位置: `~/.config/pipemux/broker.toml`

```toml
[broker]
pipe_name = "pipemux-broker"

[apps.calculator]
command = "dotnet run --project /repos/focus/PipeMux/samples/Calculator -c Release"
auto_start = false
timeout = 30
```

配置项说明：
- `broker.pipe_name`: Named Pipe 名称
- `broker.socket_path`: (可选) Unix Socket 路径
- `apps.<name>.command`: 启动命令
- `apps.<name>.auto_start`: 是否随 Broker 自动启动
- `apps.<name>.timeout`: 请求超时（秒，默认 30）

## 快速命令

### 推荐方式（使用 pmux wrapper）

```bash
# 确保环境变量已设置（已添加到 ~/.bashrc）
export ATELIA_HOME="/repos/focus/atelia"
export PATH="$ATELIA_HOME/bin:$PATH"

# 直接使用 pmux（Broker 自动启动）
pmux :help                    # 显示帮助
pmux :list                    # 列出注册的 App
pmux :ps                      # 列出运行中的实例

pmux calculator push 10       # 调用 calculator
pmux calculator push 20
pmux calculator add
pmux calculator peek

pmux :stop calculator         # 停止 calculator
```

### 开发调试方式

```bash
# 启动 Broker (前台，可看日志)
cd /repos/focus/PipeMux && dotnet run --project src/PipeMux.Broker -c Release

# 另一个终端：使用 CLI
dotnet run --project src/PipeMux.CLI -c Release -- calculator push 10
dotnet run --project src/PipeMux.CLI -c Release -- :list

# 运行 E2E 测试
./test-e2e.sh

# 模拟不同终端
PIPEMUX_TERMINAL_ID="session-A" dotnet run --project src/PipeMux.CLI -c Release -- calculator push 10
PIPEMUX_TERMINAL_ID="session-B" dotnet run --project src/PipeMux.CLI -c Release -- calculator push 99
```

## 技术栈

- **.NET 9.0**
- **StreamJsonRpc** (`2.22.x`) - Microsoft 官方 JSON-RPC 库
- **Nerdbank.Streams** (`2.13.x`) - 流处理工具
- **System.CommandLine** - 命令行解析
- **Tomlyn** - TOML 配置解析

## 参考文档

- [PipeMux/docs/README.md](../../../PipeMux/docs/README.md) — 使用说明
- [PipeMux/AGENTS.md](../../../PipeMux/AGENTS.md) — 项目跨会话记忆
