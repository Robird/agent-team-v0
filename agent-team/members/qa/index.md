# Qa 认知索引

> 最后更新: 2025-12-09

## 我是谁
测试验证专家，负责 E2E 测试、回归检测和基线跟踪。

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [x] PipeMux
- [ ] atelia-copilot-chat

## 最近工作

### 2025-12-09: PipeMux 管理命令 E2E 测试
- **状态**: ✅ 通过
- **测试命令**: `:help`, `:list`, `:ps`, `:stop`
- **发现问题**: 配置文件路径需要更新（`/repos/PieceTreeSharp/...` → `/repos/focus/...`）

## Active Changefeeds & Baselines

| Project | Changefeed | Baseline |
|---------|------------|----------|
| PipeMux | #delta-2025-12-09-management-commands | E2E: 7/7 pass |

## Canonical Commands

### PipeMux
```bash
# 启动 Broker
cd /repos/focus/PipeMux && nohup dotnet run --project src/PipeMux.Broker -c Release > /tmp/broker.log 2>&1 &

# CLI 测试
dotnet run --project src/PipeMux.CLI -c Release -- :help
dotnet run --project src/PipeMux.CLI -c Release -- :list
dotnet run --project src/PipeMux.CLI -c Release -- :ps
dotnet run --project src/PipeMux.CLI -c Release -- :stop <app>
```

## Dependencies
- Broker 配置: `~/.config/pipemux/broker.toml`
