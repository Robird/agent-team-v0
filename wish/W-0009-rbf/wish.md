---
wishId: "W-0009"
title: "设计并实现RBF（Reversible Binary Framing）"
status: Active
owner: "监护人刘世超"
created: 2026-01-06
updated: 2026-01-06
tags: [rbf, design]
produce:
  # 外部产物文档
  - "atelia/docs/Rbf/rbf-interface.md"
  - "atelia/docs/Rbf/rbf-format.md"
  - "atelia/docs/Rbf/rbf-test-vectors.md"
  - "atelia/docs/Rbf/rbf-decisions.md"
  - "atelia/docs/Rbf/rbf-type-bone.md"
  - "atelia/docs/Rbf/README.md"
  - "wish/W-0009-rbf/recap.md"
  - "wish/W-0009-rbf/todo.md"
---

# Wish: 设计并实现RBF（Reversible Binary Framing）

按照`atelia/docs/Rbf/`中的设计文档，完成软件细节设计，并完成编码实现。包含标的测试和代码Review。

## 目标与边界

**目标 (Goals)**:
- `atelia/docs/Rbf/`中的设计文档的所有decision和spec条款都有对应实现代码或单元测试。
- 尝试新的详细设计与实现过程文档组织形式（来自于W-0010的实践经验）：
  - 人工（含AI）维护两个关键的状态文档：
    - `wish/W-0009-rbf/recap.md`记录当前最新的“前情提要”，可以全一些，累积之前的交付成果。用于让AI会话了解当前的基础条件。
    - `wish/W-0009-rbf/todo.md`记录后续的待做事项，方向性思路。
  - 将实现划分成若干个Stage：
    - 以`wish/W-0009-rbf/stage/`中的数字序号命名的目录作为载体，内部记录每个Stage的所有实现过程。
    - 每个Stage应该是高度内聚的，对外以修改前述`recap.md`和`todo.md`文件作为信息交换媒介。开发过程被建模为:
      1. 在AI辅助下，人工创建`stage/<sequence>/task.md`文件，内部规划几个可由单次`runSubagent`工具调用完成的Task，以“任务简报”的形式呈现。制定Task是从`todo.md`切下当前行动和进一步制定后续规划的过程。
      2. 迭代实施与修订各Task，直至完成所有制定的Task。每次提供`recap.md`（过去）、`stage/<sequence>/task.md`（现在）、`todo.md`（未来）三个关键上下文文件。执行完一个Task都修订`recap.md`使得后续会话得知之前的进展。
      3. 完成一个Stage的所有Task后，修订`recap.md`以压缩太过具体的细节，保留关键的后效性信息。

**非目标 (Non-Goals)**:
- 上层使用者修订和实现，如StateJournal。
