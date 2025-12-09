# atelia-copilot-chat 知识库

> 最后更新: 2025-12-09
> 维护者: 所有 Specialist 共同维护

## 项目概述

**atelia-copilot-chat** 是 GitHub Copilot Chat 的 fork，用于研究和定制 AI 编程助手功能。

**定位**：自我增强环境——我参与开发的功能提升我自己的能力。

## 架构参考

详细架构文档参见 `copilot-chat-deepwiki/` 目录：

| 文档 | 内容 |
|------|------|
| `1_Overview.md` | 总览 |
| `2_Extension_Architecture.md` | 扩展架构 |
| `4_Language_Model_Integration.md` | 语言模型集成 |
| `9_Chat_System.md` | 聊天系统 |
| `11_Agent_Prompt_System.md` | Agent 提示系统 |
| `16_Tool_Ecosystem.md` | 工具生态 |
| `22_Inline_Editing_System.md` | 内联编辑 |

## 关键目录

```
src/
├── chat/              # 聊天核心
├── agents/            # Agent 系统
├── tools/             # 工具定义
└── ...
```

## 与 atelia/prototypes 的关系

atelia-copilot-chat 是研究对象，atelia/prototypes 是自研实现：

| 概念 | copilot-chat | atelia/prototypes |
|------|--------------|-------------------|
| LLM 抽象 | 内部封装 | `Completion.Abstractions` |
| Agent 引擎 | 内部封装 | `Agent.Core/AgentEngine.cs` |
| Tool 包装 | 手动定义 | `MethodToolWrapper.cs` (反射自动化) |

## 研究价值

- 理解成熟 AI 编程助手的架构
- 学习半上下文压缩等技术
- 为 LiveContextProto 提供参考

## 已知问题

目前主要作为研究参考，暂无直接开发任务。
