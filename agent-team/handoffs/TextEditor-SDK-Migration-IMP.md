# [TextEditor SDK Migration] Implementation Result

## 实现摘要
将 TextEditor 从手动 JSON-RPC 循环迁移到 `PipeMuxApp` + `System.CommandLine` 模式，与 MemoryNotebook 保持架构一致。

## 文件变更
| 文件 | 操作 | 描述 |
|------|------|------|
| `DocUI/demo/TextEditor/DocUI.TextEditor.csproj` | 修改 | 引用 `PipeMux.Sdk` 替代 `PipeMux.Shared` |
| `DocUI/demo/TextEditor/Program.cs` | 重写 | 使用 `PipeMuxApp` 和 `System.CommandLine` 定义命令 |
| `DocUI/demo/TextEditor/EditorSession.cs` | 重构 | 移除 Protocol 依赖，返回纯字符串 |
| `DocUI/demo/TextEditor/TextEditorService.cs` | 删除 | 命令分发逻辑合并到 Program.cs |
| `~/.config/pipemux/broker.toml` | 更新 | 添加 texteditor 应用配置 |

## 源码对齐说明
| 源码元素 | 实现 | 备注 |
|---------|---------|------|
| `TextEditorService` 命令分发 | `System.CommandLine` 命令 | 使用声明式命令定义 |
| `Response` 返回类型 | 纯字符串 + 异常 | SDK 框架处理输出 |
| JSON-RPC 循环 | `PipeMuxApp.RunAsync()` | SDK 内部处理 |
| 有状态会话 | 闭包捕获 `session` 变量 | 同进程内共享状态 |

## 新命令语法
```bash
# 打开文件
pmux texteditor open <path>

# 跳转到指定行
pmux texteditor goto-line <line>

# 选区（尚未实现）
pmux texteditor select <startLine> <startCol> <endLine> <endCol>

# 重新渲染
pmux texteditor render
```

## 测试结果
- Build: `dotnet build -c Release` → ✅ 成功
- Targeted:
  - `pmux texteditor open /path/to/file` → ✅
  - `pmux texteditor goto-line 50` → ✅ (状态保持，Session ID 一致)
  - `pmux texteditor render` → ✅
  - `pmux texteditor select 1 1 5 10` → ✅ (预期错误: NotImplementedException)

## 已知差异
1. **命令解析**: 原实现从 `Request.Args[0]` 解析命令，新实现使用 `System.CommandLine` 子命令模式
2. **错误处理**: 原实现返回 `Response.Fail()`，新实现通过 `Configuration.Error.WriteLine()` 输出错误
3. **Session ID 生成**: 保持原有的 `te-{8字符}` 格式

## 遗留问题
1. `select` 命令尚未实现（抛出 NotImplementedException）
2. 需要手动更新 `~/.config/pipemux/broker.toml` 添加 texteditor 配置

## broker.toml 配置
```toml
[apps.texteditor]
command = "dotnet /repos/focus/DocUI/demo/TextEditor/bin/Release/net9.0/DocUI.TextEditor.dll"
auto_start = false
timeout = 30
```

## Changefeed Anchor
`#delta-2025-12-10-texteditor-sdk-migration`
