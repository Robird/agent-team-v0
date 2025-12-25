# Broker+CLI 架构决策探讨

**日期**: 2025-12-06  
**状态**: 讨论稿

## 参与者

- **Planner** (规划者) - 负责整体架构设计和决策协调
- **Architect** (架构师) - 负责技术方案评估
- **Developer** (开发者) - 负责实施可行性分析

---

## 议题 1：独立仓库

### 背景分析

当前 Broker+CLI 组件位于 `/repos/PieceTreeSharp/src/DocUI.*/`，包含：

| 项目 | 职责 | 依赖 |
|------|------|------|
| PipeMux.Broker | 进程管理、Named Pipe 服务器、请求路由 | PipeMux.Shared, Tomlyn |
| PipeMux.CLI | 统一命令行前端 | PipeMux.Shared, System.CommandLine |
| PipeMux.Shared | JSON-RPC 协议定义 | System.Text.Json |
| DocUI.Calculator | 示例/测试应用 | PipeMux.Shared |
| DocUI.TextEditor | 文本编辑器应用 | PipeMux.Shared, **TextBuffer** |

**关键观察**：
1. Broker+CLI+Shared 是**完全独立**的通用组件，不依赖 PieceTreeSharp 的核心功能（TextBuffer）
2. TextEditor 是唯一连接两个领域的项目（依赖 TextBuffer 和 DocUI 基础设施）
3. Calculator 是纯测试用途，可以随 Broker 移动

### 利弊评估

#### ✅ 支持独立的理由

| 优点 | 说明 |
|------|------|
| **职责清晰** | Broker+CLI 是通用 IPC 框架，与 PieceTree（文本缓冲区）无关 |
| **复用性** | 独立后可作为其他项目的依赖，不需要引入整个 PieceTreeSharp |
| **独立版本** | 可独立发布 NuGet 包，版本管理更灵活 |
| **测试隔离** | 框架测试与应用测试分离 |
| **社区贡献** | 对 IPC 框架感兴趣的开发者不需要了解 PieceTree |

#### ❌ 反对独立的理由

| 缺点 | 说明 |
|------|------|
| **维护成本** | 多仓库需要同步更新、CI/CD 配置 |
| **开发体验** | 跨仓库调试、修改协议需要多次提交 |
| **初期复杂度** | 项目刚起步，过早拆分可能带来不必要的复杂性 |
| **TextEditor 归属** | 需要决定 TextEditor 放在哪个仓库 |

### 建议方案

**推荐：阶段性独立**

```
Phase 1: 目录独立（当前阶段）
├── PieceTreeSharp/
│   ├── src/TextBuffer/          # PieceTree 核心
│   └── src/DocUI.TextEditor/    # TextEditor 应用
│
└── appbroker/                   # 新的子目录（git 子模块或独立仓库）
    ├── src/AppBroker.Core/      # 原 PipeMux.Broker
    ├── src/AppBroker.CLI/       # 原 PipeMux.CLI
    ├── src/AppBroker.Shared/    # 原 PipeMux.Shared
    ├── samples/Calculator/      # 原 DocUI.Calculator
    └── tests/

Phase 2: 发布独立包
- 发布 AppBroker.Shared 到 NuGet
- TextEditor 通过 NuGet 引用而非 ProjectReference

Phase 3: 完全独立（可选）
- 如果框架获得社区关注，可完全独立仓库
```

### 迁移计划

#### 步骤 1：创建新目录结构
```bash
mkdir -p /repos/PieceTreeSharp/appbroker/src
mkdir -p /repos/PieceTreeSharp/appbroker/samples
mkdir -p /repos/PieceTreeSharp/appbroker/tests
```

#### 步骤 2：移动和重命名项目
```bash
# 核心项目
mv src/PipeMux.Broker   appbroker/src/AppBroker.Core
mv src/PipeMux.CLI      appbroker/src/AppBroker.CLI
mv src/PipeMux.Shared   appbroker/src/AppBroker.Shared

# 示例项目
mv src/DocUI.Calculator appbroker/samples/Calculator
```

#### 步骤 3：更新项目引用和命名空间
- 重命名 csproj 文件
- 更新 namespace 从 `DocUI.*` 到 `AppBroker.*`
- 更新 solution 文件

#### 步骤 4：处理 TextEditor
- TextEditor 保留在 PieceTreeSharp
- 暂时使用 ProjectReference 指向 appbroker 子目录
- 未来改为 NuGet PackageReference

---

## 议题 2：命名建议

### 核心特征分析

分析当前系统的技术特征：

| 特征 | 描述 |
|------|------|
| **进程管理** | Broker 管理后台应用进程的生命周期 |
| **IPC 通信** | 使用 Named Pipe（Unix Socket）进行进程间通信 |
| **协议** | JSON-RPC 2.0 标准协议 |
| **架构模式** | 类似微服务的 Sidecar/Service Mesh 模式 |
| **统一入口** | CLI 作为所有应用的统一前端 |
| **常驻进程** | 后台应用以守护进程方式运行 |

**定位描述**：一个轻量级的本地进程编排框架，通过 Named Pipe 和 JSON-RPC 连接 CLI 前端与常驻后台服务。

### 命名候选

#### 候选 1：AppBroker / appbroker
- **优点**: 直白描述核心功能（应用中介）
- **缺点**: 比较平淡，broker 在消息队列领域有特定含义
- **可搜索性**: 中等（会混淆消息 broker）

#### 候选 2：PipeMux / pipemux
- **含义**: Pipe Multiplexer（管道多路复用器）
- **优点**: 准确描述技术特征（Named Pipe 多路复用）
- **缺点**: 技术导向，不易理解用途
- **可搜索性**: 好（独特名称）

#### 候选 3：Conduit
- **含义**: 管道、导管
- **优点**: 优雅、简短、暗示通信管道
- **缺点**: 可能与其他项目重名
- **可搜索性**: 中等

#### 候选 4：CliDaemon / clidaemon
- **含义**: CLI + Daemon
- **优点**: 准确描述 CLI 前端 + 守护进程模式
- **缺点**: 名字较长
- **可搜索性**: 好

#### 候选 5：LocalRpc / localrpc
- **含义**: 本地 RPC
- **优点**: 准确描述技术特征
- **缺点**: 过于技术化
- **可搜索性**: 中等

#### 候选 6：Nexus
- **含义**: 连接点、核心
- **优点**: 简短有力，暗示连接功能
- **缺点**: 已被多个项目使用
- **可搜索性**: 差

#### 候选 7：AppLink / applink
- **含义**: 应用链接
- **优点**: 直观、易记
- **缺点**: 可能与其他项目重名
- **可搜索性**: 中等

#### 候选 8：StdioMesh / stdiomesh
- **含义**: 标准 I/O 网格
- **优点**: 准确描述技术特征（通过 stdin/stdout 通信）
- **缺点**: 略长
- **可搜索性**: 好（独特）

### 推荐名称及理由

**首选推荐：`PipeMux` (pipemux)**

理由：
1. **独特性** - 不太可能与其他项目重名，便于搜索
2. **技术准确** - Named Pipe + Multiplexer 准确描述架构
3. **简短** - 只有 7 个字母，适合命令行输入
4. **可组合** - `pipemux-cli`, `pipemux-broker`, `PipeMux.Shared`

**备选推荐：`AppBroker` (appbroker)**

理由：
1. **直观** - 非技术人员也能理解"应用中介"
2. **通用** - 不绑定具体技术实现
3. **可扩展** - 未来可以扩展为非 Pipe 通信

**命名规范建议**：

```
项目结构示例（以 PipeMux 为例）：

pipemux/
├── src/
│   ├── PipeMux.Core/        # Broker 核心
│   ├── PipeMux.Cli/         # CLI 前端
│   ├── PipeMux.Sdk/         # App 开发 SDK
│   └── PipeMux.Protocol/    # 协议定义
├── samples/
│   └── Calculator/
└── tests/
```

---

## 议题 3：App 开发框架

### 当前问题分析

当前 Calculator 应用的样板代码问题：

```csharp
// Calculator/Program.cs - 当前实现（约 70 行）
using var reader = new StreamReader(Console.OpenStandardInput());
using var writer = new StreamWriter(Console.OpenStandardOutput());

string? line;
while ((line = Console.ReadLine()) != null)
{
    // 手动 JSON 解析
    var request = JsonSerializer.Deserialize<JsonRpcRequest>(line);
    
    // 手动路由
    var result = request.Method switch
    {
        "add" => HandleAdd(request),
        "subtract" => HandleSubtract(request),
        // ...
    };
    
    // 手动响应构建
    var response = JsonRpcResponse.Success(request.Id, result);
    Console.WriteLine(JsonSerializer.Serialize(response));
}
```

**问题清单**：
1. ❌ 大量样板代码（stdin/stdout 处理、JSON 序列化）
2. ❌ 手动路由（switch 表达式）
3. ❌ 手动参数解析和验证
4. ❌ 错误处理重复代码
5. ❌ 无内置帮助生成
6. ❌ 无参数默认值支持

### 设计目标

1. **极简 API** - 类似 ASP.NET Minimal API / Python FastAPI 的声明式风格
2. **类型安全** - 利用 C# 强类型和 Source Generator
3. **CLI 一致性** - 与 System.CommandLine 概念对齐
4. **零依赖运行时** - App 只需引用轻量 SDK
5. **可测试性** - 命令处理器可单独测试
6. **帮助生成** - 自动生成命令帮助文档

### 框架 API 设计

#### 方案 A：Minimal API 风格（推荐）

```csharp
// Calculator/Program.cs - 期望的最简实现
using PipeMux.Sdk;

var app = new PipeMuxApp("calculator", "A simple calculator service");

app.MapCommand("add", "Add two numbers", 
    (double a, double b) => a + b);

app.MapCommand("subtract", "Subtract b from a", 
    (double a, double b) => a - b);

app.MapCommand("divide", "Divide a by b", 
    (double a, double b) => 
        b == 0 ? Result.Error("Division by zero") : Result.Ok(a / b));

// 支持默认参数
app.MapCommand("power", "Calculate a^b", 
    (double a, double b = 2) => Math.Pow(a, b));

// 支持可选参数
app.MapCommand("format", "Format number with precision", 
    (double value, int? precision) => 
        precision.HasValue 
            ? value.ToString($"F{precision}") 
            : value.ToString());

await app.RunAsync();
```

#### 方案 B：特性标注风格

```csharp
// 使用特性标注方法
using PipeMux.Sdk;

var app = new PipeMuxApp("calculator");
app.RegisterService<CalculatorService>();
await app.RunAsync();

public class CalculatorService
{
    [Command("add", Description = "Add two numbers")]
    public double Add(double a, double b) => a + b;
    
    [Command("divide")]
    public Result<double> Divide(
        [Param(Description = "Dividend")] double a,
        [Param(Description = "Divisor")] double b)
    {
        if (b == 0) return Result.Error("Division by zero");
        return Result.Ok(a / b);
    }
    
    [Command("power")]
    public double Power(double a, [Param(Default = 2)] double b = 2) 
        => Math.Pow(a, b);
}
```

#### 方案 C：混合风格

```csharp
// 结合两种风格
var app = new PipeMuxApp("calculator");

// 简单命令用 Lambda
app.MapCommand("add", (double a, double b) => a + b);

// 复杂命令用类
app.RegisterService<AdvancedMathService>();

await app.RunAsync();
```

### 与 System.CommandLine 集成方案

#### 核心理念

利用 System.CommandLine 的核心抽象，但适配常驻进程场景：

```
System.CommandLine     PipeMux.Sdk
────────────────────   ────────────────────
Command                PipeMuxCommand
Option<T>              Parameter<T>
Argument<T>            Parameter<T>
RootCommand            PipeMuxApp
Handler                CommandHandler
```

#### 集成架构

```csharp
// PipeMux.Sdk 内部实现
public class PipeMuxApp
{
    private readonly Dictionary<string, CommandDescriptor> _commands = new();
    
    public PipeMuxApp MapCommand<TResult>(
        string name, 
        string? description,
        Func<...> handler)
    {
        // 内部转换为 System.CommandLine 的模型
        var command = new Command(name, description);
        
        // 从 Lambda 参数推断 Options
        foreach (var param in handler.Method.GetParameters())
        {
            command.AddOption(CreateOption(param));
        }
        
        _commands[name] = new CommandDescriptor(command, handler);
        return this;
    }
    
    public async Task RunAsync()
    {
        // 主循环：读取 JSON-RPC，路由到命令，返回响应
        await foreach (var request in ReadRequestsAsync())
        {
            var response = await DispatchAsync(request);
            await WriteResponseAsync(response);
        }
    }
}
```

#### 帮助命令支持

```bash
# 自动支持 help 命令
$ docui calculator help
Calculator - A simple calculator service

Commands:
  add       Add two numbers
  subtract  Subtract b from a
  divide    Divide a by b
  power     Calculate a^b (default b=2)

$ docui calculator help add
add - Add two numbers

Parameters:
  a  (required, double)  First operand
  b  (required, double)  Second operand
```

### 示例代码

#### SDK 核心类型定义

```csharp
// PipeMux.Sdk/PipeMuxApp.cs
namespace PipeMux.Sdk;

public class PipeMuxApp
{
    private readonly string _name;
    private readonly string? _description;
    private readonly Dictionary<string, ICommandHandler> _handlers = new();

    public PipeMuxApp(string name, string? description = null)
    {
        _name = name;
        _description = description;
        
        // 内置命令
        MapCommand("help", "Show available commands", 
            (string? command) => GenerateHelp(command));
    }

    // Lambda 重载 - 无参数
    public PipeMuxApp MapCommand(string name, string description, Func<object> handler)
    {
        _handlers[name] = new FuncHandler0(name, description, handler);
        return this;
    }

    // Lambda 重载 - 1-8 个参数（使用 Source Generator 生成）
    public PipeMuxApp MapCommand<T1>(string name, string description, Func<T1, object> handler)
    {
        _handlers[name] = new FuncHandler1<T1>(name, description, handler);
        return this;
    }

    public PipeMuxApp MapCommand<T1, T2>(string name, string description, Func<T1, T2, object> handler)
    {
        _handlers[name] = new FuncHandler2<T1, T2>(name, description, handler);
        return this;
    }

    // ... 更多重载

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(Console.OpenStandardInput());
        using var writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };

        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line == null) break;
            if (string.IsNullOrWhiteSpace(line)) continue;

            var response = ProcessRequest(line);
            await writer.WriteLineAsync(JsonSerializer.Serialize(response));
        }
    }

    private JsonRpcResponse ProcessRequest(string json)
    {
        try
        {
            var request = JsonSerializer.Deserialize<JsonRpcRequest>(json);
            if (request == null)
                return JsonRpcResponse.Error(null, -32600, "Invalid request");

            if (!_handlers.TryGetValue(request.Method, out var handler))
                return JsonRpcResponse.Error(request.Id, -32601, $"Method not found: {request.Method}");

            var result = handler.Invoke(request.Params);
            
            if (result is ErrorResult error)
                return JsonRpcResponse.Error(request.Id, error.Code, error.Message);
            
            return JsonRpcResponse.Success(request.Id, result);
        }
        catch (JsonException ex)
        {
            return JsonRpcResponse.Error(null, -32700, $"Parse error: {ex.Message}");
        }
        catch (Exception ex)
        {
            return JsonRpcResponse.Error(null, -32603, $"Internal error: {ex.Message}");
        }
    }
}
```

#### 结果类型

```csharp
// PipeMux.Sdk/Result.cs
namespace PipeMux.Sdk;

public static class Result
{
    public static OkResult<T> Ok<T>(T value) => new(value);
    public static ErrorResult Error(string message, int code = -32000) => new(code, message);
}

public record OkResult<T>(T Value);
public record ErrorResult(int Code, string Message);
```

#### 完整 Calculator 示例

```csharp
// samples/Calculator/Program.cs
using PipeMux.Sdk;

var app = new PipeMuxApp("calculator", "A simple calculator service");

app.MapCommand("add", "Add two numbers", 
    (double a, double b) => a + b);

app.MapCommand("subtract", "Subtract b from a", 
    (double a, double b) => a - b);

app.MapCommand("multiply", "Multiply two numbers", 
    (double a, double b) => a * b);

app.MapCommand("divide", "Divide a by b", 
    (double a, double b) => 
        b == 0 ? Result.Error("Division by zero") : Result.Ok(a / b));

app.MapCommand("sqrt", "Calculate square root", 
    (double x) => 
        x < 0 ? Result.Error("Cannot calculate square root of negative number") 
              : Result.Ok(Math.Sqrt(x)));

app.MapCommand("power", "Calculate a^b (default: square)", 
    (double a, double b = 2) => Math.Pow(a, b));

await app.RunAsync();
```

**对比代码行数**：
- 当前实现：约 130 行（Program.cs + CalculatorService.cs）
- 新框架：约 25 行

---

## 总结与行动项

### 决策摘要

| 议题 | 建议 | 优先级 |
|------|------|--------|
| 独立仓库 | 阶段性独立：先目录分离，稳定后考虑 NuGet 包 | P1 |
| 命名 | **PipeMux** 或 **AppBroker** | P1 |
| App 框架 | 实现 Minimal API 风格的 SDK | P0 |

### 行动项

#### P0 - App SDK 框架（本周）

- [ ] 创建 `PipeMux.Sdk` 项目
- [ ] 实现 `PipeMuxApp` 核心类
- [ ] 实现 `MapCommand` 泛型重载（1-8 参数）
- [ ] 实现 `Result.Ok/Error` 结果类型
- [ ] 迁移 Calculator 到新框架
- [ ] 添加单元测试

#### P1 - 重命名和目录重组（下周）

- [ ] 确定最终名称（团队投票）
- [ ] 创建新目录结构
- [ ] 批量重命名项目和命名空间
- [ ] 更新 solution 文件
- [ ] 更新所有 using 语句

#### P2 - 帮助系统和文档（Sprint+1）

- [ ] 实现内置 help 命令
- [ ] 生成命令帮助文档
- [ ] 与 System.CommandLine 深度集成
- [ ] 编写开发者文档

#### P3 - NuGet 发布（未来）

- [ ] 设置 NuGet 包元数据
- [ ] 配置 CI/CD 发布流程
- [ ] 发布 PipeMux.Sdk 和 PipeMux.Protocol

---

## 附录：参考设计

### 参考项目

- **ASP.NET Minimal API**: Lambda 风格路由
- **FastAPI** (Python): 声明式 API 设计
- **System.CommandLine**: CLI 参数绑定
- **Dapper**: 简单高效的数据访问
- **MediatR**: 请求/响应分发模式

### 技术约束

- .NET 9.0 目标框架
- 无第三方依赖（SDK 部分）
- Named Pipe / Unix Socket 跨平台支持
- JSON-RPC 2.0 标准协议

---

## 团队意见征集

**日期**: 2025-12-06

### InvestigatorTS 意见

*从 VS Code/TypeScript 生态角度*

#### 关于命名

我**强烈支持 PipeMux**。理由如下：

1. **与 VS Code 生态的命名风格一致** - VS Code 本身就有很多技术导向的命名：`vscode-languageserver-protocol`、`vscode-jsonrpc`、`vscode-textmate`。PipeMux 符合这种简洁、技术准确的命名传统。

2. **跨平台考量** - 虽然叫 "Pipe"，但实际上在 Unix 上是 Unix Socket。这在 VS Code 的 Language Server Protocol 实现中也是类似的抽象（transport layer 可以是 stdio, socket, pipe）。名称不需要精确描述底层实现。

3. **避免与 Message Broker 混淆** - 在前端/Node.js 生态中，"Broker" 立刻让人想到 Redis、RabbitMQ、Kafka。PipeMux 不会有这种误解。

#### 关于 SDK API

我倾向于**方案 A (Minimal API 风格)**，但建议考虑 TypeScript 生态的一些实践：

```csharp
// 建议增加：链式调用 + 分组
var app = new PipeMuxApp("calculator")
    .WithDescription("A simple calculator service")
    .WithGroup("math", group => {
        group.MapCommand("add", (double a, double b) => a + b);
        group.MapCommand("subtract", (double a, double b) => a - b);
    });
```

这种模式在 Express.js、Fastify 等 Node.js 框架中非常流行，可以提供更好的组织结构。

#### 关于代码组织

同意移到子目录。建议使用 `pipemux/` 而非 `appbroker/`，与命名选择保持一致。

**一个额外建议**：未来如果要提供 TypeScript/Node.js 的 SDK，可以考虑在 `pipemux/sdk-ts/` 下提供对应的 npm 包。JSON-RPC 协议本身是语言无关的，这是一个很好的跨生态机会。

---

### PorterCS 意见

*从 C#/.NET 开发者角度*

#### 关于命名

我对两个名称都可以接受，但略微倾向于 **AppBroker**：

1. **对 .NET 开发者更直观** - 在 .NET 生态中，我们有 `ServiceBroker`（SQL Server）、`MessageBroker` 等概念。AppBroker 一看就知道是"应用程序代理/中介"。

2. **但我理解 PipeMux 的优势** - 如果面向的是更广泛的开发者社区（不仅仅是 .NET），PipeMux 确实更独特。

最终我不反对 PipeMux，只要文档中清楚说明它是什么就行。

#### 关于 SDK API

**强烈支持方案 A (Minimal API 风格)**！

作为一个 C# 开发者，我从 ASP.NET Core 6 开始就爱上了 Minimal API。理由：

1. **与现代 .NET 风格一致** - .NET 6+ 的 Minimal API 已经证明了这种风格的成功。开发者学习曲线几乎为零。

2. **AOT 友好** - Lambda 风格相比反射标注方式更容易支持 Native AOT 编译，这对 CLI 工具非常重要（启动速度）。

3. **IDE 支持更好** - 直接的 Lambda 表达式比特性标注更容易获得 IntelliSense 支持。

**但我建议保留方案 B 作为高级用法**：

```csharp
// 简单场景用 Lambda
app.MapCommand("add", (double a, double b) => a + b);

// 复杂场景（需要依赖注入、生命周期管理）用类注册
app.RegisterService<ComplexService>();
```

这与 ASP.NET Core 同时支持 Minimal API 和 Controller 的思路一致。

#### 关于代码组织

完全同意移到子目录。作为曾经负责将 PieceTree TypeScript 移植到 C# 的人，我深知保持代码边界清晰的重要性。

**一个实施建议**：在移动时，使用 `git mv` 而非手动移动，以保留文件历史：

```bash
git mv src/PipeMux.Broker pipemux/src/PipeMux.Core
git mv src/PipeMux.CLI pipemux/src/PipeMux.Cli
# ...
```

---

### GeminiAdvisor 意见

*从用户体验和 API 设计角度*

#### 关于命名

从 **UX 角度**，我需要考虑几类用户：

| 用户类型 | PipeMux | AppBroker |
|---------|---------|-----------|
| 框架开发者 | ✅ 技术准确，一看就懂 | ⚠️ 可能与 MQ Broker 混淆 |
| 应用开发者 | ⚠️ 需要解释含义 | ✅ 直观："应用代理" |
| 最终用户 (CLI) | ➖ 都需要学习 | ➖ 都需要学习 |

**我的建议是 PipeMux**，但需要配套：

1. **tagline/副标题** - "PipeMux - Local Process Orchestration via Named Pipes"
2. **Logo/Icon** - 设计一个直观的管道多路复用图标
3. **README 首行** - 一句话解释是什么

#### 关于 SDK API

从 **Developer Experience (DX)** 角度，我评估两个方案：

**方案 A 的 DX 优势**：
- ✅ 学习曲线：近乎零（如果熟悉 ASP.NET Minimal API）
- ✅ 代码量：最少
- ✅ 可读性：一眼看出命令和实现的对应关系
- ✅ 调试体验：直接在 Lambda 中设断点

**方案 B 的 DX 优势**：
- ✅ 大型项目组织：命令可按类分组
- ✅ 测试性：类方法更容易单元测试
- ✅ 依赖注入：可以注入服务

**我的建议**：

1. **默认推荐方案 A** - 用于 README 示例、快速入门
2. **文档中提供方案 B** - 标注为"高级用法"
3. **两种方式共存** - 不要强迫用户只能选一种

#### 关于参数命名

一个细节建议：当前 Lambda 参数推断可能导致参数名丢失（如果不用 Source Generator）。建议提供显式参数名的方式：

```csharp
// 方式 1: Lambda 参数名（需要 Source Generator 保留）
app.MapCommand("add", (double a, double b) => a + b);

// 方式 2: 显式参数定义（更可控）
app.MapCommand("add", 
    new Param<double>("a", "First operand"),
    new Param<double>("b", "Second operand"),
    (a, b) => a + b);
```

#### 关于代码组织

同意子目录方案。**额外建议**：

- 在 `pipemux/` 根目录添加独立的 `README.md`
- 添加 `pipemux/docs/` 存放框架文档
- 添加 `pipemux/samples/` 提供多个示例（不只是 Calculator）

---

### Team Leader 总结

感谢 InvestigatorTS、PorterCS 和 GeminiAdvisor 的宝贵意见。综合各方观点：

#### 命名决策

三位成员中：
- InvestigatorTS：**强烈支持 PipeMux**
- PorterCS：**略倾向 AppBroker，但接受 PipeMux**
- GeminiAdvisor：**推荐 PipeMux（配套解释）**

**结论**：采用 **PipeMux**。需要在文档和 README 中提供清晰的一句话解释。

#### SDK API 决策

三位成员一致：
- **方案 A (Minimal API)** 作为默认/推荐
- **方案 B (特性标注)** 保留为高级用法

**结论**：采用**混合方案**，以方案 A 为主。

#### 代码组织决策

三位成员一致同意子目录方案。

**结论**：创建 `pipemux/` 子目录，按照文档中的目录结构组织。

---

## 最终决策

### 命名
- **选择**: PipeMux
- **理由**: 独特且可搜索，技术准确描述核心功能（Named Pipe 多路复用），与 VS Code 生态命名风格一致，避免与 Message Broker 概念混淆。配套 tagline："Local Process Orchestration via Named Pipes"。

### SDK API
- **选择**: 方案 A (Minimal API 风格) 为主，方案 B 为辅
- **理由**: 与现代 .NET 风格一致（ASP.NET Core Minimal API），学习曲线最低，代码量最少，AOT 友好。同时保留类注册方式支持复杂场景和依赖注入需求。

### 代码组织
- **选择**: 创建 `pipemux/` 子目录
- **理由**: 职责清晰分离，为未来独立发布 NuGet 包做准备，便于跨语言 SDK 扩展（如 TypeScript SDK）。使用 `git mv` 保留文件历史。

---

## 下一步行动

1. **立即执行**：创建 `pipemux/` 目录结构并迁移现有代码
2. **本周完成**：实现 `PipeMux.Sdk` 核心框架，迁移 Calculator 示例
3. **下周完成**：完善文档和帮助系统
4. **Sprint+1**：评估 NuGet 发布时机

**决策生效日期**: 2025-12-06
