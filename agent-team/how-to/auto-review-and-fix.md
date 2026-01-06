# Recipe: 自动化文档审阅与修复

> **状态**：已验证（实验 2-3 通过）
> **版本**：v0.2
> **创建**：2026-01-06
> **验证**：87.5% 问题可自动处理

---

## 1. 概述

### 1.1 目标

让 AI Team Leader 通过调度 SubAgent，自动化完成设计文档的审阅和修复，只将真正无法处理的问题上报监护人。

### 1.2 核心理念

- **Leader 只管调度**：不读具体问题内容，只根据 SubAgent 汇报决定下一步
- **不预设难度**：直接问"你能修吗"，让 Agent 自己判断
- **不强制修复**：给 Agent 说"不会"的空间，避免硬着头皮上
- **自然沉淀**：复杂问题自然留到最后，集中上报

---

## 2. 流程设计

### 2.1 整体流程

```
┌─────────────────────────────────────────────────────────┐
│              Round 1: 工程师视角审阅                     │
│                                                         │
│   Discovery → Fix Loop → Report                         │
│   （发现实现层面的问题）                                 │
└────────────────────────┬────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│              Round 2: DocOps 视角审阅（可选）            │
│                                                         │
│   Discovery → Fix Loop → Report                         │
│   （发现文档结构层面的问题）                             │
└────────────────────────┬────────────────────────────────┘
                         ↓
┌─────────────────────────────────────────────────────────┐
│              Final: 上报监护人                           │
│                                                         │
│   汇总 blocked/ 目录下的问题                             │
└─────────────────────────────────────────────────────────┘
```

### 2.2 单轮流程伪代码

```python
def auto_review_and_fix(docs: list[str], findings_dir: str, discovery_prompt: str):
    """
    自动审阅并修复文档问题
    
    Args:
        docs: 待审阅的文档路径列表
        findings_dir: 问题文件输出目录
        discovery_prompt: Discovery 阶段的提示词
    """
    
    # Phase 1: Discovery
    # Leader 调用 SubAgent，获取问题清单
    # SubAgent 汇报：发现 N 个问题，已输出到 findings_dir
    
    # Phase 2: Fix Loop
    while has_pending_files(findings_dir):
        finding_file = get_first_pending(findings_dir)
        
        # 用改进的提示词询问
        result = run_subagent(FIX_PROMPT.format(finding=finding_file))
        
        # 根据响应分类
        if is_false_positive(result):
            move_to(finding_file, "false-positive/")
        elif is_fixed(result):
            move_to(finding_file, "done/")
        else:  # blocked or unknown
            move_to(finding_file, "blocked/")
    
    # Phase 3: Report
    return list_blocked_files(findings_dir)
```

---

## 3. 提示词模板（已验证）

### 3.1 Round 1: 工程师视角 Discovery

```markdown
你是一位**准备实现 {系统名称} 的工程师**，正在阅读以下设计文档：
{文件列表}

你需要确保自己理解正确，不会因为文档描述的歧义而写出错误的代码。

请指出文档中**可能让你写错代码的地方**。只关注会导致实现错误的问题，不关注文档风格。

对于每个问题，说明：
1. 位置（文件#章节/条款）
2. 问题描述
3. 风险（如果误解会怎样）
4. 修复建议
```

### 3.2 Round 2: DocOps 视角 Discovery（可选）

```markdown
审阅以下文档的一致性：
{文件列表}

列出发现的问题。
```

### 3.3 Fix 提示词（核心，已验证） ⭐

```markdown
嘿！我好像发现了个问题，你帮我看看能不能修？
能的话帮我给修一下呗。不能的话也没关系，告诉我为什么就行。

**问题描述**：
{问题内容}

**相关文档**：
{文件路径列表}
```

**设计要点**：
1. **表现不确定性**（"我好像发现了"）→ Agent 会验证假阳性
2. **不强制修复**（"不能的话也没关系"）→ Agent 敢说"不会"
3. **一次一条** → 更专注，质量更高

### 3.4 响应解析规则

| 响应特征 | 判断 | 处理 |
|:---------|:-----|:-----|
| "已被修复"、"假阳性"、"问题不存在" | 假阳性 | 移到 `false-positive/` |
| "FIXED"、"修复完成"、"已修"、"DONE" | 已修复 | 移到 `done/` |
| "BLOCKED"、"需要决策"、"无法确定"、"需要确认" | 阻塞 | 移到 `blocked/` |

---

## 4. 目录结构

```
{findings_dir}/
├── F-001-xxx.md          # 待处理
├── F-002-xxx.md          # 待处理
├── done/                 # 已修复
│   └── F-003-xxx.md
├── false-positive/       # 假阳性
│   └── F-004-xxx.md
└── blocked/              # 需人工决策
    └── F-005-xxx.md
```

---
