# Broker Lazy Start 方案对比分析

> 日期: 2025-12-09
> 状态: 分析中
> 目标: 选择最优的 Broker 自动启动方案

## 问题定义

用户执行 `pmux calculator push 10` 时，如果 Broker 未运行，应该：
1. 自动启动 Broker
2. 等待 Broker 就绪
3. 执行原始命令

## 候选方案

### 方案 A: Shell Wrapper

```bash
#!/bin/bash
# atelia-devkit/bin/pmux

ensure_broker() {
    if ! broker_is_running; then
        start_broker_background
        wait_for_ready
    fi
}

ensure_broker
exec dotnet run --project CLI -- "$@"
```

**优点**：
| 维度 | 评价 |
|------|------|
| 实现复杂度 | ✅ 极低（~50 行 bash） |
| 部署复杂度 | ✅ 低（一个文件） |
| 调试容易度 | ✅ 高（可直接 `bash -x`） |
| 对 CLI 代码侵入 | ✅ 零（完全解耦） |

**缺点**：
| 维度 | 评价 |
|------|------|
| 跨平台 | ❌ 需要 bash/zsh/PowerShell 多版本 |
| 启动延迟 | ⚠️ 每次都要 `dotnet run`（~1-2s） |
| 竞态处理 | ⚠️ 多终端同时启动可能冲突 |
| 错误处理 | ⚠️ bash 错误处理较弱 |

### 方案 B: CLI 内置 Lazy Start

```csharp
// PipeMux.CLI/Program.cs
public static async Task Main(string[] args)
{
    await BrokerLauncher.EnsureRunningAsync();
    await ExecuteCommandAsync(args);
}

// PipeMux.CLI/BrokerLauncher.cs
public static class BrokerLauncher
{
    public static async Task EnsureRunningAsync()
    {
        if (await TryConnectAsync())
            return;
        
        await StartBrokerProcessAsync();
        await WaitForReadyAsync(timeout: 5s);
    }
}
```

**优点**：
| 维度 | 评价 |
|------|------|
| 跨平台 | ✅ 完全一致（.NET 运行时处理） |
| 错误处理 | ✅ 完善的异常体系 |
| 竞态处理 | ✅ 可用 Mutex/文件锁 |
| 类型安全 | ✅ 编译期检查 |

**缺点**：
| 维度 | 评价 |
|------|------|
| 实现复杂度 | ⚠️ 中（需要 Process 管理） |
| 代码侵入 | ⚠️ CLI 需要知道 Broker 位置 |
| 调试 | ⚠️ 需要附加调试器 |
| 启动延迟 | ⚠️ 仍需 `dotnet run`（除非预编译） |

### 方案 C: 预编译 + CLI 内置

```bash
# 发布为单文件可执行
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true

# 直接执行（无需 dotnet 运行时启动）
./pmux calculator push 10
```

```csharp
// CLI 内置 Lazy Start（同方案 B）
// 但 Broker 路径相对于可执行文件
var brokerPath = Path.Combine(
    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
    "pmux-broker"
);
```

**优点**：
| 维度 | 评价 |
|------|------|
| 启动速度 | ✅ 极快（~50ms，无 JIT） |
| 跨平台 | ✅ 完全一致 |
| 部署 | ✅ 单文件，易分发 |
| 用户体验 | ✅ 像原生命令 |

**缺点**：
| 维度 | 评价 |
|------|------|
| 构建复杂度 | ⚠️ 需要多平台构建 |
| 文件大小 | ⚠️ self-contained ~60MB |
| 开发迭代 | ⚠️ 每次改动需重新发布 |

### 方案 D: Systemd/Launchd 服务

```ini
# /etc/systemd/user/pmux-broker.service
[Unit]
Description=PipeMux Broker

[Service]
ExecStart=/usr/local/bin/pmux-broker
Restart=always

[Install]
WantedBy=default.target
```

**优点**：
| 维度 | 评价 |
|------|------|
| 生命周期管理 | ✅ 系统级，自动重启 |
| 日志管理 | ✅ journald 集成 |
| 资源控制 | ✅ cgroups 支持 |

**缺点**：
| 维度 | 评价 |
|------|------|
| 跨平台 | ❌ Linux 特有 |
| 部署复杂度 | ❌ 需要 root/用户服务配置 |
| 开发环境 | ❌ 不适合频繁迭代 |

## 评估矩阵

| 维度 | 权重 | A (Wrapper) | B (内置) | C (预编译+内置) | D (Systemd) |
|------|------|-------------|----------|-----------------|-------------|
| 跨平台一致性 | 25% | 2 | 5 | 5 | 1 |
| 实现复杂度 | 20% | 5 | 3 | 2 | 2 |
| 启动速度 | 20% | 2 | 2 | 5 | 5 |
| 开发迭代效率 | 20% | 5 | 4 | 2 | 1 |
| 错误处理健壮性 | 15% | 2 | 4 | 4 | 5 |
| **加权得分** | | **3.2** | **3.5** | **3.6** | **2.7** |

## 深入分析：为什么我实施时选了 Wrapper？

### 当时的决策过程

1. **时间约束**：需要快速交付可工作的解决方案
2. **风险规避**：CLI 代码改动需要更多测试
3. **MVP 思维**：先验证流程，再优化实现

### 复盘反思

这是**务实但非最优**的选择。对于追求"有依据的选优"，应该选择 **方案 C（预编译 + CLI 内置）**。

## 推荐方案

### 短期（当前）：方案 A (Wrapper) ✅ 已实施

- 立即可用
- 验证了工作流程
- 为长期方案提供参考

### 中期（P2）：方案 B (CLI 内置)

- 在 CLI 中实现 `BrokerLauncher`
- 保持 `dotnet run` 方式
- 解决跨平台一致性

### 长期（P3）：方案 C (预编译 + CLI 内置)

- 发布为单文件可执行
- 极致启动速度
- 像原生命令一样使用

## 实施路径

```
方案 A (当前)
    │
    │ 验证工作流程
    ▼
方案 B (中期)
    │ 实现 BrokerLauncher.cs
    │ 复用 BrokerClient 连接逻辑
    │ 添加 Mutex 防止竞态
    ▼
方案 C (长期)
    │ 配置 PublishSingleFile
    │ 多平台 CI 构建
    │ 版本化发布
    ▼
方案 D (可选)
    │ 生产环境部署
    │ systemd 服务
```

## 关键实现细节（方案 B）

### BrokerLauncher.cs 设计

```csharp
public static class BrokerLauncher
{
    private static readonly string MutexName = "PipeMux-Broker-Launch";
    
    public static async Task EnsureRunningAsync(CancellationToken ct = default)
    {
        // 快速路径：Broker 已运行
        if (await BrokerClient.TryConnectAsync(timeout: 100ms))
            return;
        
        // 慢速路径：需要启动
        using var mutex = new Mutex(false, MutexName);
        if (!mutex.WaitOne(TimeSpan.FromSeconds(10)))
            throw new TimeoutException("Another process is starting broker");
        
        try
        {
            // 再次检查（可能被其他进程启动了）
            if (await BrokerClient.TryConnectAsync(timeout: 100ms))
                return;
            
            await StartBrokerAsync();
            await WaitForReadyAsync(timeout: 5s, ct);
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }
    
    private static async Task StartBrokerAsync()
    {
        var brokerPath = FindBrokerPath();
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{brokerPath}\" -c Release",
            CreateNoWindow = true,
            UseShellExecute = false,
        };
        
        var process = Process.Start(psi);
        await WritePidFileAsync(process.Id);
    }
}
```

### 竞态处理

```
Terminal A                    Terminal B
    │                             │
    ├─ TryConnect() → fail        │
    │                             ├─ TryConnect() → fail
    ├─ Mutex.WaitOne() → got      │
    │                             ├─ Mutex.WaitOne() → waiting...
    ├─ TryConnect() → fail        │
    ├─ StartBroker()              │
    ├─ WaitForReady()             │
    ├─ Mutex.Release()            │
    │                             ├─ ...got mutex
    │                             ├─ TryConnect() → success!
    │                             ├─ Mutex.Release()
    ▼                             ▼
  (both proceed)
```

## 结论

| 阶段 | 方案 | 理由 |
|------|------|------|
| 当前 | A (Wrapper) | 已实施，验证流程 |
| 中期 | B (CLI 内置) | 跨平台一致，值得投入 |
| 长期 | C (预编译) | 极致体验，生产就绪 |

**我承认**：实施时选择 Wrapper 是为了快速交付，而非最优选择。感谢你的追问，这让我更清晰地分析了各方案的权衡。

---

*分析完成: 2025-12-09*
