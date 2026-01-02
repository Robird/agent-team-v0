# 工具探索实验笔记

> 目标：系统探索工具能力边界，提炼成可用知识

---

## 实验计划

### 困惑点
1. `file_search` vs `grep_search` vs `semantic_search` — 什么时候用哪个？
2. `terminal_selection` / `terminal_last_command` — 干什么用？
3. `test_failure` — 和 `run_in_terminal` 跑测试有什么区别？
4. `get_search_view_results` — 从未用过
5. `list_code_usages` — 能力边界在哪？
6. `fetch_webpage` vs `vscode-websearchforcopilot_webSearch` — 区别？

---

## 实验记录

### 实验 1：搜索工具对比

**查询**：`DocumentGraphBuilder`

| 工具 | 结果 | 特点 |
|:-----|:-----|:-----|
| `file_search` | 0 结果 | 按文件名 glob 匹配，`**/DocumentGraphBuilder*` 找不到 `.cs` 文件 |
| `grep_search` | 5 匹配 | 精确文本搜索，返回行号和上下文，快！ |
| `semantic_search` | ~30 段代码片段 | 语义理解，返回**大量相关代码上下文**，很详细 |

**发现**：
1. `file_search` 是**文件名搜索**，不是内容搜索！`**/DocumentGraphBuilder*` 找的是文件名包含这个的文件
2. `grep_search` 快速精确，适合"我知道要找什么文本"
3. `semantic_search` 返回大量上下文，适合"我想理解这个概念"

**正确用法**：
- 找文件 → `file_search`（用 `**/*.cs` 这样的 glob）
- 找文本位置 → `grep_search`
- 理解代码 → `semantic_search`

---

### 实验 2：file_search 深入测试

尝试了多种 glob 模式，**全部返回空**：
- `**/*Builder*.cs` → 空
- `**/DocGraph/**/*.cs` → 空
- `atelia/src/DocGraph/**/*.cs` → 空
- `*.cs` → 空

但 `list_dir` 确认文件存在！

**结论**：`file_search` 可能有 bug，或者工作区配置有问题。
**实用建议**：用 `list_dir` + `grep_search` 组合代替 `file_search`。

---

### 实验 3：终端相关工具

| 工具 | 结果 | 用途 |
|:-----|:-----|:-----|
| `terminal_last_command` | 返回上一条命令 + 输出 + 退出码 | 检查刚跑完的命令结果 |
| `terminal_selection` | "No text selected" | 用户在终端选中文本时读取 |

**发现**：
- `terminal_last_command` 有用！当 `run_in_terminal` 输出被截断时，可以用它获取完整输出
- `terminal_selection` 是用户交互场景，自动化编码用不上

---

### 实验 4：test_failure 工具

结果："No test failures were found yet, call the tool runTests to run tests"

**发现**：这个工具是**读取 VS Code 测试视图的状态**，不是主动跑测试。
需要先在 VS Code 里跑测试，失败了才能用这个工具获取详情。

**对比**：
- `run_in_terminal` + `dotnet test` → 主动跑测试，获取输出
- `test_failure` → 被动读取 VS Code 测试结果

**实用建议**：我的场景直接用 `run_in_terminal` 更可控。

---

### 实验 5：get_search_view_results

结果：返回空（"The following are the results from the search view:"）

**发现**：这是读取 VS Code **搜索面板**的结果。需要用户先在 VS Code 里搜索，才有内容。
自动化场景用不上。

---

### 实验 6：list_code_usages

测试了 `DeriveStatus` 和 `DocumentGraphBuilder`：
- 带 filePaths 参数 → 报错 "Cannot read properties of undefined"
- 不带参数 → "Symbol not found"

**发现**：这个工具依赖语言服务器的符号索引。C# 项目可能没有正确加载，或者工具本身对 C# 支持有限。

**实用建议**：用 `grep_search` 代替——搜索方法名/类名一样有效。

---

### 实验 7：runSubagent

测试：让 subagent 查询 `DeriveStatus` 方法实现。

**结果**：成功！返回了方法实现和详细解释。

**发现**：
- Subagent 可以访问所有工具
- 适合委托"需要多步骤搜索+分析"的任务
- 返回的是最终总结，中间过程不可见

---

### 实验 8：Web 搜索工具对比

| 工具 | 输入 | 输出 | 用途 |
|:-----|:-----|:-----|:-----|
| `vscode-websearchforcopilot_webSearch` | 查询字符串 | 多个网页摘要片段 | 快速了解话题，获取多个来源观点 |
| `fetch_webpage` | URL + 查询 | 单个网页的详细内容 | 深入阅读特定网页，获取完整信息 |

**对比**：
- `webSearch` = 广度优先（多来源摘要）
- `fetch_webpage` = 深度优先（单来源详情）

**工作流**：先 webSearch 找到相关页面，再 fetch_webpage 深入读取最相关的那个。

---

## 总结：工具选择指南

### 核心工具（高频使用）

| 场景 | 首选工具 | 备注 |
|:-----|:---------|:-----|
| 读代码/文档 | `read_file` | 大块读取 |
| 找文本位置 | `grep_search` | 知道关键词时 |
| 理解概念 | `semantic_search` | 返回大量上下文 |
| 编辑代码 | `replace_string_in_file` / `multi_replace` | 后者更高效 |
| 跑命令 | `run_in_terminal` | |
| 检查结果 | `get_terminal_output` | 输出被截断时 |
| 导航目录 | `list_dir` | |
| 委托任务 | `runSubagent` | 多步骤任务 |

### 边缘工具（特定场景）

| 工具 | 适用场景 | 备注 |
|:-----|:---------|:-----|
| `file_search` | ⚠️ 不稳定，用 `list_dir` 代替 | |
| `list_code_usages` | TypeScript/JavaScript 项目 | C# 支持有限 |
| `terminal_last_command` | 获取完整输出 | 当输出被截断时 |
| `webSearch` | 技术调研 | 广度搜索 |
| `fetch_webpage` | 深入阅读文档 | 深度搜索 |

### 不常用工具

| 工具 | 说明 |
|:-----|:-----|
| `terminal_selection` | 用户手动选中文本时才有用 |
| `test_failure` | 读取 VS Code 测试视图，不如直接跑测试 |
| `get_search_view_results` | 读取 VS Code 搜索面板，自动化用不上 |
| `github_repo` | 工作区外的 GitHub 代码搜索 |

---

## 待探索

- [x] `get_changed_files` 的用法 ✅
- [x] `get_errors` 的最佳实践 ✅
- [ ] `create_file` vs 终端命令创建文件的差异

---

### 实验 9：get_changed_files

**结果**：返回当前 git 仓库中所有未提交变更的**完整 diff**！

**输出内容**：
- 每个变更文件的完整内容（attachment 形式）
- git diff 输出（显示具体变更）
- 可以通过 `sourceControlState` 参数过滤：staged / unstaged / merge-conflicts

**超级有用的场景**：
1. **代码审阅**：一次性看到所有变更
2. **提交前检查**：确认要提交的内容
3. **上下文恢复**：刚接手任务时快速了解当前状态

**与 `run_in_terminal` + `git diff` 对比**：
- `get_changed_files` 返回结构化数据 + 完整文件内容
- `git diff` 只返回差异部分

**实用建议**：任务开始时调用一次，了解当前工作状态。

---

### 实验 10：get_errors

**结果**："No errors found."

**发现**：这是读取 VS Code **问题面板**的内容（编译错误、lint 警告等）。

**使用场景**：
1. 编辑代码后检查是否引入编译错误
2. 不需要跑完整构建就能发现语法问题
3. 比 `dotnet build` 快（增量检查）

**参数**：
- 无参数 → 返回所有文件的错误
- 指定 `filePaths` → 只检查特定文件

**实用建议**：`replace_string_in_file` 后立即调用，快速验证编辑是否引入错误。

---

### 实验 11：list_code_usages 修正

**前置条件**：C# 需要先在 VS Code 中选择打开的 Solution（如 `atelia/Atelia.sln`）并激活 C# 扩展。

**修正后测试**：`DocumentGraphBuilder` → **47 个引用**！

**输出内容**：
- 定义位置（`<definition>` 标签）
- 所有使用位置（文件路径 + 行号 + 列号）

**vs `grep_search` 对比**：

| 维度 | `list_code_usages` | `grep_search` |
|:-----|:-------------------|:--------------|
| 精确度 | **语义级**——只找真正的引用 | 文本级——可能包含注释/字符串 |
| 前置条件 | 需要 LSP 激活 | 无 |
| 跨文件 | ✅ 自动搜索整个 Solution | 需要指定范围 |
| 速度 | 依赖 LSP 索引状态 | 快 |

**实用建议**：
- 重构时找所有调用点 → `list_code_usages`（精确）
- 快速定位 → `grep_search`（无依赖）

---

### 实验 12：create_file vs 终端命令

**测试**：创建包含特殊字符的文件（引号、反斜杠、$变量、反引号、Tab、Unicode）

**发现 1：Tab 字符处理差异**
```
create_file: "6. Tab:	制表符"  (Tab 保留)
heredoc:     "6. Tab:制表符"   (Tab 可能丢失)
```

**发现 2：Shell 变量展开陷阱**
```bash
# 单引号 → 原样保留
echo '$HOME' → $HOME

# 双引号 → 变量展开！
echo "$HOME" → /root
echo "`date`" → Sat Jan 3 04:13:08 CST 2026
```

**结论**：

| 方面 | `create_file` | 终端命令 |
|:-----|:--------------|:---------|
| 转义 | **所见即所得**——内容原样写入 | 需要处理 shell 转义规则 |
| 变量 | 不展开 | 双引号内会展开 `$VAR` 和 `` `cmd` `` |
| 安全性 | **更安全**——无注入风险 | 需小心命令注入 |
| 适用 | 创建配置文件、代码文件 | 简单文本、需要变量展开时 |

**实用建议**：
- 创建代码/配置文件 → `create_file`（避免转义地狱）
- 需要动态内容（如当前日期） → 终端命令

---
