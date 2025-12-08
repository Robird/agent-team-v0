# Session Report: DocUI Broker 骨架搭建

**日期**: 2025-12-06  
**会话类型**: 新方向启动  
**成果**: PipeMux.Broker + CLI 项目骨架完成

---

## 背景

基于 PieceTreeSharp 移植的成熟度（1158 测试通过），启动新方向探索：为 LLM Agent 打造有状态的交互式编辑器系统。

### 核心痛点

当前 LLM Agent 使用编辑器的典型模式：
- `read_file` → 上下文历史堆积，token 浪费
- `replace_string_in_file` → 多处匹配困难，大段替换成本高
- 无状态交互 → 每次全新开始，无法利用持久化状态

### 核心洞察

**LLM Agent 需要的不是"文件系统"，而是"有状态的交互式编辑器"**

---

## 架构设计

### 三层结构

```
消费者层:  CLI (开发) → Tool Calling (生产)
    ↓
中转层:    PipeMux.Broker (进程管理 + 路由)
    ↓
应用层:    DocUI.TextEditor / Debugger / DiffViewer...
```

### 关键设计

1. **通信协议**: Named Pipes / Unix Socket + JSON-RPC
2. **会话管理**: 自动分配 Session ID，持久化状态
3. **配置驱动**: TOML 配置，自动启动后台应用
4. **Markdown 渲染**: 行号 + 内容 + 光标/选区标记

---

## 本会话成果

### 创建的项目 (4 个)

| 项目 | 类型 | 职责 | 状态 |
|------|------|------|------|
| **PipeMux.Shared** | 类库 | 协议定义 (Request/Response/JsonRpc) | ✅ 完成 |
| **PipeMux.Broker** | 可执行 | 中转服务器 (进程管理 + 路由) | 🚧 骨架 |
| **PipeMux.CLI** | 可执行 | 统一 CLI 前端 | 🚧 骨架 |
| **DocUI.TextEditor** | 可执行 | 文本编辑器后台 (基于 PieceTreeSharp) | 🚧 骨架 |

### 核心文件清单

#### 协议层 (PipeMux.Shared)
- `Protocol/Request.cs` - 统一请求格式
- `Protocol/Response.cs` - 统一响应格式
- `Protocol/JsonRpc.cs` - JSON-RPC 序列化

#### 中转层 (PipeMux.Broker)
- `BrokerConfig.cs` - 配置模型
- `ConfigLoader.cs` - TOML 配置加载
- `ProcessRegistry.cs` - 后台应用进程管理
- `BrokerServer.cs` - 服务器主逻辑 (骨架)

#### 消费者层 (PipeMux.CLI)
- `Program.cs` - CLI 参数解析 (System.CommandLine)
- `BrokerClient.cs` - Broker 客户端 (骨架)

#### 应用层 (DocUI.TextEditor)
- `TextEditorService.cs` - 命令处理服务
- `EditorSession.cs` - 单会话状态管理
- `MarkdownRenderer.cs` - Markdown 渲染器 (基础版)

### 文档清单

- [`docs/plans/docui-broker-architecture.md`](../docs/plans/docui-broker-architecture.md) - 完整架构设计 (5000+ 字)
- [`docs/docui-quickstart.md`](../docs/docui-quickstart.md) - 快速开始指南
- [`docs/examples/broker.toml`](../docs/examples/broker.toml) - 配置示例
- `src/PipeMux.Broker/README.md` - 项目概览

### 构建验证

```bash
✅ PipeMux.Shared 构建成功
✅ PipeMux.Broker 构建成功
✅ PipeMux.CLI 构建成功
✅ DocUI.TextEditor 构建成功
✅ 整体解决方案构建成功 (只有测试项目的风格警告)
```

---

## 已实现功能

### ✅ 协议定义
- Request/Response 数据结构
- JSON-RPC 序列化/反序列化
- 错误处理模型 (Success/Fail)

### ✅ 基础 Markdown 渲染
- 行号格式化 (自动对齐)
- 光标标记 (`█`)
- 统计信息 (行数、光标位置)

### ✅ 配置加载
- TOML 格式支持 (Tomlyn 库)
- 跨平台路径处理
- 默认配置回退

### ✅ 进程管理骨架
- 应用进程注册表
- 启动/获取/关闭接口
- 自动启动支持

---

## 待实现功能 (下一步)

### 🚧 通信层
- Named Pipe 服务器循环 (Broker)
- Named Pipe 客户端 (CLI)
- JSON-RPC 完整循环测试

### 🚧 命令实现
- `open` - 加载文件到 TextModel ✅ (骨架完成)
- `goto-line` - 移动光标 ✅ (骨架完成)
- `select` - 设置选区 (待实现)
- `insert` - 插入文本 (待实现)
- `delete` - 删除文本 (待实现)
- `replace` - 替换文本 (待实现)
- `undo/redo` - 撤销/重做 (待实现)

### 🚧 装饰系统
- 选区渲染 (下划线/高亮)
- 查找结果高亮
- 诊断信息显示

---

## 技术亮点

### 1. 跨平台通信
使用 Named Pipes (Windows + Linux/Mac 都支持)，避免端口占用。

### 2. 职责清晰分离
- **Shared**: 纯协议定义，无依赖
- **Broker**: 进程管理，不关心应用细节
- **CLI**: 简单的转发层
- **TextEditor**: 专注于编辑逻辑

### 3. 基于 PieceTreeSharp
充分利用已移植的高质量文本编辑引擎 (1158 测试)。

### 4. 面向未来的设计
CLI 只是过渡方案，最终通过 tool calling 集成到 Agent 环境。

---

## 文件统计

- **新增文件**: 20+ 个
- **代码行数**: ~1000 行 (含注释)
- **文档字数**: ~6000 字
- **构建时间**: < 5 秒

---

## 下一步计划

### Phase 1.1: 实现通信循环 (预计 2-3 会话)
1. Named Pipe 服务器 (Broker)
2. Named Pipe 客户端 (CLI)
3. 端到端测试 (CLI → Broker → TextEditor → CLI)

### Phase 1.2: 完善 TextEditor 命令 (预计 2-3 会话)
1. Select 命令 (选区渲染)
2. Edit 命令族 (insert/delete/replace)
3. Undo/Redo 栈

### Phase 1.3: 装饰系统集成 (预计 1-2 会话)
1. 选区渲染 (下划线)
2. 查找结果高亮
3. 诊断信息显示

### Phase 2: 自研 Agent 集成
等 Phase 1 完成后评估。

---

## 认知文件更新

✅ 已更新:
- `agent-team/status.md` - 添加 DocUI Broker 项目状态
- `AGENTS.md` - 添加 DocUI Broker 骨架成果

✅ Changefeed 锚点:
- `#delta-2025-12-06-docui-broker-skeleton`

---

## 总结

**战略意义**: 为 LLM Agent 打造真正 LLM-Native 的编辑器交互模式，从根本上解决上下文堆积和无状态困境。

**技术稳健性**: 基于成熟的 PieceTreeSharp 引擎，架构清晰，职责分离。

**迭代路径清晰**: 骨架 → 通信循环 → 命令完善 → 装饰系统 → Agent 集成。

**风险可控**: 每个阶段都有明确的验收标准，可以随时调整方向。

---

*创建时间: 2025-12-06*  
*作者: AI Team (刘德智 / SageWeaver)*
