# PipeMux 管理命令 QA 结果

> 日期: 2025-12-09
> QA: @qa
> 状态: ✅ 通过

## 验证范围

验证 PipeMux 管理命令的 E2E 功能：
- `:help` - 帮助信息
- `:list` - 已注册应用列表
- `:ps` - 运行中进程列表
- `:stop` - 停止指定应用

## 测试结果

| 测试用例 | 命令 | 预期结果 | 实际结果 | 状态 |
|---------|------|---------|---------|------|
| T1 | `:ps` (初始) | 无运行进程 | `(no running processes)` | ✅ |
| T2 | `calculator push 10` | 启动 calculator | `Stack: [10]` | ✅ |
| T3 | `:ps` (启动后) | 显示 calculator | 显示 PID 和 healthy 状态 | ✅ |
| T4 | `:stop calculator` | 停止成功 | `Stopped: calculator:...` | ✅ |
| T5 | `:ps` (停止后) | 无运行进程 | `(no running processes)` | ✅ |
| T6 | `:stop nonexistent` | 错误提示 | `No running process found` | ✅ |
| T7 | `:unknown` | 未知命令提示 | `Unknown management command` | ✅ |

**汇总**: 7/7 测试通过

## 发现的问题

### 问题 1: 配置文件路径过时 (已修复)
- **现象**: Calculator 启动失败，显示 `Could not execute because the specified command or file was not found`
- **原因**: `~/.config/pipemux/broker.toml` 中的路径指向旧位置 `/repos/PieceTreeSharp/...`
- **修复**: 更新为 `/repos/focus/PipeMux/samples/Calculator/bin/Release/net9.0/Samples.Calculator.dll`

## 基线更新

| 指标 | 前 | 后 |
|-----|----|----|
| E2E 测试 | N/A | 7/7 pass |

## Changefeed Anchor

`#delta-2025-12-09-management-commands`

## 验收确认

- [x] `:help` 显示帮助信息
- [x] `:list` 显示已注册应用
- [x] `:ps` 初始为空，启动后显示进程，停止后为空
- [x] `:stop` 成功停止指定应用
- [x] 错误处理：未知命令、不存在的应用

## 测试环境

- **Broker**: `dotnet run --project src/PipeMux.Broker -c Release`
- **CLI**: `dotnet run --project src/PipeMux.CLI -c Release -- <args>`
- **配置**: `~/.config/pipemux/broker.toml`
