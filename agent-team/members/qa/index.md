# Qa 认知索引

> 最后更新: 2025-12-19

## 我是谁
测试验证专家，负责 E2E 测试、回归检测和基线跟踪。

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [x] PipeMux
- [x] DurableHeap
- [ ] atelia-copilot-chat

## 最近工作

### 2025-12-19: DurableHeap MVP v2 术语修订验证
- **状态**: ⚠️ 发现问题
- **验证范围**: `DurableHeap/docs/mvp-design-v2.md` 术语修订
- **发现问题**: 
  - `head` 未完全统一为 `HEAD`（第289行）
  - `On-Disk Diff` 在4.4.1语义层次定义中仍作为定义名出现（672/696/709行）
  - `state diff` 在2.4节标题和正文中仍出现（146/652行）
  - 决策表Q3/Q5标题仍使用旧术语（历史决策记录，可接受）
  - 4.4.3节建议仍使用 `Commit()` 而非 `FlushToWriter()`（825行）

### 2025-12-09: PipeMux 管理命令 E2E 测试
- **状态**: ✅ 通过
- **测试命令**: `:help`, `:list`, `:ps`, `:stop`
- **发现问题**: 配置文件路径需要更新（`/repos/PieceTreeSharp/...` → `/repos/focus/...`）

## Active Changefeeds & Baselines

| Project | Changefeed | Baseline |
|---------|------------|----------|
| PipeMux | #delta-2025-12-09-management-commands | E2E: 7/7 pass |
| DurableHeap | #delta-2025-12-19-terminology-review | Doc review: 术语表完整，正文有遗漏 |

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
