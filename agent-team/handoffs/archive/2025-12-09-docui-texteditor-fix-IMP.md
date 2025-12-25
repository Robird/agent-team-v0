# [DocUI demo/TextEditor 项目引用修复] Implementation Result

## 实现摘要
修复了 DocUI demo/TextEditor 项目的跨项目引用问题，更正了 csproj 的 ProjectReference 路径，并将项目添加到解决方案。同时修复了代码与 PipeMux.Shared.Protocol API 的兼容性问题。

## 文件变更
- `DocUI/demo/TextEditor/DocUI.TextEditor.csproj` — 更正三个 ProjectReference 路径
- `DocUI/demo/TextEditor/TextEditorService.cs` — 修复 Request 属性访问（Args[0] 替代 Command）
- `DocUI/DocUI.sln` — 添加 DocUI.TextEditor 项目到 demo 文件夹

## 源码对齐说明
| 原引用 | 修正后 | 备注 |
|--------|--------|------|
| `../PipeMux.Shared/PipeMux.Shared.csproj` | `../../../PipeMux/src/PipeMux.Shared/PipeMux.Shared.csproj` | PipeMux 位于 workspace 根 |
| `../TextBuffer/TextBuffer.csproj` | `../../../PieceTreeSharp/src/TextBuffer/TextBuffer.csproj` | PieceTreeSharp 位于 workspace 根 |
| (无) | `../../src/DocUI.Text/DocUI.Text.csproj` | 新增 DocUI.Text 引用 |

## API 适配
原代码使用了不存在的 `Request.Command` 属性，修改为从 `Request.Args[0]` 获取命令：
```csharp
// 修复前
return request.Command switch { ... }

// 修复后
var command = request.Args.Length > 0 ? request.Args[0] : "";
return command switch { ... }
```

## 测试结果
- Build: `dotnet build DocUI.sln` → ✅ 6/6 成功
- Test: `dotnet test DocUI.sln` → ✅ 24/24 通过

## 已知差异
- 任务要求引用 `PipeMux.Sdk`，但代码实际使用的是 `PipeMux.Shared.Protocol` 中的 `Request`/`Response` 类型，因此引用了 `PipeMux.Shared`

## 遗留问题
无

## Changefeed Anchor
`#delta-2025-12-09-docui-texteditor-fix`
