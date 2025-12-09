# PipeMux 管理命令实施计划

> 日期: 2025-12-09
> 状态: 进行中
> 基于: PipeMux-Management-Commands-RFC.md

## 实施目标

为 PipeMux 添加管理命令支持，使用 `:` 前缀语法。

## 任务分解

### Task 1: 协议扩展 (PipeMux.Shared)
**负责**: Implementer

1. 在 `Request` 中添加 `RequestKind` 枚举区分 App 调用和管理命令
2. 定义管理命令类型：`List`, `Ps`, `Stop`, `Restart`, `Register`, `Reload`, `Help`
3. 定义管理命令响应格式

**文件**:
- `src/PipeMux.Shared/Protocol/Request.cs`
- `src/PipeMux.Shared/Protocol/ManagementCommand.cs` (新建)

### Task 2: CLI 解析 (PipeMux.CLI)
**负责**: Implementer

1. 检测第一个参数是否以 `:` 开头
2. 如果是，解析为管理命令
3. 如果不是，保持现有 App 调用逻辑

**命令映射**:
| 输入 | 解析结果 |
|------|----------|
| `pmux :list` | ManagementCommand.List |
| `pmux :ps` | ManagementCommand.Ps |
| `pmux :stop calc` | ManagementCommand.Stop("calc") |
| `pmux :restart calc` | ManagementCommand.Restart("calc") |
| `pmux :help` | ManagementCommand.Help |
| `pmux calc push 10` | AppCall("calc", "push", ["10"]) |

**文件**:
- `src/PipeMux.CLI/Program.cs`

### Task 3: Broker 路由 (PipeMux.Broker)
**负责**: Implementer

1. 在 `BrokerServer.HandleClientAsync` 中添加管理命令分支
2. 实现 `HandleManagementAsync` 方法
3. 复用 `ProcessRegistry` 已有的 `ListActive()` 和 `Close()` 方法

**文件**:
- `src/PipeMux.Broker/BrokerServer.cs`
- `src/PipeMux.Broker/ManagementHandler.cs` (新建)

### Task 4: 测试
**负责**: QA

1. 单元测试：CLI 解析逻辑
2. E2E 测试：管理命令实际执行
3. 更新 `tests/test-e2e.sh`

### Task 5: 文档更新
**负责**: DocOps

1. 更新 `PipeMux/docs/README.md`
2. 更新 `agent-team/wiki/PipeMux/README.md`

## 优先级

P1 (本次实施) — **✅ 全部完成**:
- [x] Task 1: 协议扩展
- [x] Task 2: CLI 解析
- [x] Task 3: Broker 路由 (`:list`, `:ps`, `:stop`, `:help`)
- [x] Task 4: 测试 (7/7 E2E 通过)
- [x] Task 5: 文档更新

P2 (后续):
- `:restart`
- `:register`
- `:reload`

## 验收标准

```bash
# 启动 Broker
dotnet run --project src/PipeMux.Broker -c Release &

# 管理命令测试
pmux :list           # 应显示注册的 App 列表
pmux :ps             # 应显示运行中的实例（初始为空）

pmux calculator push 10  # 启动 calculator
pmux :ps             # 应显示 calculator 实例

pmux :stop calculator    # 停止 calculator
pmux :ps             # 应显示实例已停止
```
