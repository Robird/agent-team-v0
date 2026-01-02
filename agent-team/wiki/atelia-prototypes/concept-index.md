# atelia 概念索引

> 最后更新: 2026-01-03
> 维护者: Investigator + Implementer 共同维护
> 用途: 快速定位概念→代码/文档位置，减少调查跳数

## 项目定位

Atelia 是面向 LLM Agent 的自研框架，提供自主持续行动能力的基础设施，包括数据持久化（StateJournal/RBF）、文档图谱（DocGraph）、智能体引擎（Agent.Core）等核心组件。

## 核心概念索引

### 基础设施层（Primitives）

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| **AteliaResult** | `docs/AteliaResult-Specification.md` | `src/Primitives/` | 跨组件统一的成功/失败协议 |
| **AteliaError** | `docs/AteliaResult-Specification.md` | `src/Primitives/AteliaError.cs` | 携带 ErrorCode、RecoveryHint 的结构化错误 |
| **DebugUtil** | — | `src/Diagnostics/DebugUtil.cs` | 调试日志工具，支持类别过滤 |

### 存储层（RBF + StateJournal）

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| **RBF** | `docs/Rbf/rbf-interface.md` | — | Reversible Binary Framing，二进制信封层 |
| **Frame** | `docs/Rbf/rbf-format.md` | — | RBF 基本 I/O 单元 |
| **FrameTag** | `docs/Rbf/rbf-format.md` | — | 4 字节帧类型标识符 |
| **Address64** | `docs/Rbf/rbf-interface.md` | — | 8 字节文件偏移量 |
| **SizedPtr** | `docs/Data/Draft/SizedPtr.md` | — | Packed Fat Pointer，Offset+Length 压缩存储 |
| **IRbfFramer** | `docs/Rbf/rbf-interface.md` | — | RBF 帧写入器接口 |
| **IRbfScanner** | `docs/Rbf/rbf-interface.md` | — | RBF 帧扫描器接口 |
| **StateJournal** | `docs/StateJournal/mvp-design-v2.md` | — | 对象版本持久化层 |
| **ObjectVersionRecord** | `docs/StateJournal/mvp-design-v2.md` | — | 对象版本记录 |
| **MetaCommitRecord** | `docs/StateJournal/mvp-design-v2.md` | — | 元数据提交记录 |
| **VersionIndex** | `docs/StateJournal/mvp-design-v2.md` | — | ObjectId → ObjectVersionPtr 映射表 |

### 数据结构层（Data）

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| **IReservableBufferWriter** | `docs/Atelia.Data-LLM-Guide.md` | `src/Data/IReservableBufferWriter.cs` | 支持预留区段并延后回填的 BufferWriter |
| **ChunkedReservableWriter** | — | `src/Data/ChunkedReservableWriter.cs` | IReservableBufferWriter 的分块实现 |
| **SlidingQueue** | — | `src/Data/SlidingQueue.cs` | 滑动窗口队列 |

### 文档图谱层（DocGraph）

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| **DocGraph** | `docs/DocGraph/v0.1/README.md` | `src/DocGraph/Program.cs` | 文档关系验证器（CLI 入口） |
| **DocumentNode** | `docs/DocGraph/v0.1/scope.md` | `src/DocGraph/Core/DocumentNode.cs` | 文档图中的节点 |
| **DocumentGraph** | `docs/DocGraph/v0.1/scope.md` | `src/DocGraph/Core/DocumentGraph.cs` | 完整的文档关系图 |
| **produce 关系** | `docs/DocGraph/v0.1/scope.md` | — | Wish → 产物文档的链接 |
| **produce_by 关系** | `docs/DocGraph/v0.1/scope.md` | — | 产物文档 → Wish 的反向链接 |

### Agent 框架层（prototypes）

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| **Agent.Core** | — | `prototypes/Agent.Core/` | LLM Agent 执行引擎原型 |
| **AgentEngine** | — | `prototypes/Agent.Core/AgentEngine.cs` | Agent 核心协调器 |
| **ITool** | — | `prototypes/Agent.Core/ITool.cs` | 工具接口 |
| **IApp** | — | `prototypes/Agent.Core/IApp.cs` | 应用接口 |
| **MethodToolWrapper** | `docs/Agent/MethodToolWrapper-spec.md` | `prototypes/Agent.Core/MethodToolWrapper.cs` | 方法→工具包装器 |
| **LiveContextProto** | `prototypes/LiveContextProto/README.md` | `prototypes/LiveContextProto/` | 实时上下文交互原型 |

### 代码质量层（Analyzers）

| 概念 | 入口文档 | 代码位置 | 一句话 |
|:-----|:---------|:---------|:-------|
| **Analyzers.Style** | `src/Analyzers.Style/README.md` | `src/Analyzers.Style/` | 自定义 Roslyn 分析器 |
| **MT0001~MT0009** | `docs/AnalyzerRules/` | `src/Analyzers.Style/` | 多行语句/缩进/括号格式分析器 |

## 常见调查路径

### "我想了解 StateJournal 存储设计"
1. 先读 `docs/Rbf/rbf-interface.md` 了解底层 RBF 二进制信封
2. 再读 `docs/StateJournal/mvp-design-v2.md` §术语表 了解核心概念
3. 查看 `docs/Data/Draft/SizedPtr.md` 了解指针压缩编码

### "我想了解 DocGraph 文档验证"
1. 先读 `docs/DocGraph/v0.1/README.md` 了解用法
2. 读 `docs/DocGraph/v0.1/scope.md` 了解功能边界
3. 查看 `src/DocGraph/Core/` 了解核心数据结构
4. 运行 `cd atelia/src/DocGraph && dotnet run -- validate ../../../`

### "我想了解 Agent 框架"
1. 先读 `prototypes/Agent.Core/AgentEngine.cs` 了解引擎架构
2. 查看 `prototypes/Agent.Core/ITool.cs` 了解工具接口
3. 读 `docs/Agent/MethodToolWrapper-spec.md` 了解自动化

### "我想了解错误处理规范"
1. 读 `docs/AteliaResult-Specification.md` 完整规范
2. 查看 `src/Primitives/` 了解实现
3. 理解 `bool+out` vs `AteliaResult` vs 异常的选择标准

### "我想添加自定义代码分析规则"
1. 查看 `src/Analyzers.Style/README.md`
2. 参考现有分析器 `MT0001~MT0009` 的实现模式
3. 在 `AnalyzerReleases.Unshipped.md` 登记新规则

## 文档结构速查

| 目录 | 内容 |
|:-----|:-----|
| `docs/StateJournal/` | 版本化存储设计 |
| `docs/Rbf/` | 二进制帧格式 |
| `docs/DocGraph/` | 文档图谱验证器 |
| `docs/Agent/` | Agent 相关规范 |
| `docs/Data/` | 数据结构草稿 |
| `prototypes/` | 原型实现 |
| `src/` | 正式代码 |
