# PipeMux Management Commands Implementation Result

> 日期: 2025-12-09
> 实现者: Implementer
> 状态: ✅ 已完成

## 实现摘要

为 PipeMux 添加了管理命令支持，使用 `:` 前缀语法。实现了 `:list`、`:ps`、`:stop`、`:help` 四个命令，CLI 在入口点检测管理命令并分流处理，Broker 通过 ManagementHandler 执行实际操作。

## 文件变更

| 文件 | 变更类型 | 说明 |
|------|---------|------|
| `src/PipeMux.Shared/Protocol/ManagementCommand.cs` | 新建 | 管理命令枚举和解析逻辑 |
| `src/PipeMux.Shared/Protocol/Request.cs` | 修改 | 添加 `ManagementCommand` 字段，`App` 改为可空 |
| `src/PipeMux.CLI/Program.cs` | 修改 | 入口点检测 `:` 前缀并分流 |
| `src/PipeMux.CLI/BrokerClient.cs` | 修改 | 添加 `SendManagementCommandAsync` 方法 |
| `src/PipeMux.Broker/ManagementHandler.cs` | 新建 | 管理命令处理器 |
| `src/PipeMux.Broker/BrokerServer.cs` | 修改 | 集成 ManagementHandler |

## 源码对齐说明

| 设计元素 | 实现 | 备注 |
|---------|---------|------|
| `:` 前缀检测 | `ManagementCommand.IsManagementCommand()` | 静态方法，CLI 入口点调用 |
| 命令解析 | `ManagementCommand.Parse()` | 支持 `:list`, `:ps`, `:stop <app>`, `:help` |
| Request 扩展 | `Request.ManagementCommand` 字段 | 可选，管理命令时设置 |
| Broker 路由 | `request.IsManagementRequest` 判断 | 在 HandleClientAsync 中分流 |
| 命令执行 | `ManagementHandler` 类 | 复用 ProcessRegistry 方法 |

## 测试结果

- **Build**: ✅ 成功
  - PipeMux.sln: 5/5 项目
  - DocUI.sln: 6/6 项目（依赖更新兼容）
- **Unit Tests**: N/A（PipeMux 暂无单元测试项目）
- **E2E Tests**: 待手动验证

## 已知差异

1. **`:restart` 未实现**: 按 P2 规划，返回 "not yet implemented" 错误
2. **`:stop` 支持前缀匹配**: `pmux :stop calculator` 会停止所有 `calculator` 和 `calculator:*` 进程

## 遗留问题

1. 需要添加单元测试覆盖 `ManagementCommand.Parse()` 逻辑
2. 需要 QA 进行 E2E 手动验证
3. 文档待更新

## 验收命令

```bash
# 启动 Broker
dotnet run --project /repos/focus/PipeMux/src/PipeMux.Broker &

# 测试管理命令
pmux :help
pmux :list
pmux :ps

# 启动应用后测试
pmux calculator push 10
pmux :ps
pmux :stop calculator
pmux :ps
```

## Changefeed Anchor

`#delta-2025-12-09-pipemux-management-commands`
