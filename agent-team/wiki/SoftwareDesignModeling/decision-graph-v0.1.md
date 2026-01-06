---
docId: "B-0003-Theory"
title: "Decision Graph：用显式方案图赋予 Agent 树状思考与回溯能力"
status: "Draft"
version: "0.1"
created: "2026-01-04"
produce_by:
  - "agent-team/experiment/2026-01-04-sizedptr-wish0004-reviewloop/2026-01-04-wish-0004-sizedptr-simulation.md"
---

# Decision Graph：用显式方案图赋予 Agent 树状思考与回溯能力

> **一句话定位**：把设计/规划问题显式建模为一个“决策点-选项-证据”的有向图（Decision Graph），让 Agent 能并行探索、Look-Ahead、回溯（把 Soft SSOT 降级回 Draft），并以“方案级别”向人类请求裁决，而不是在细枝末节上频繁打断。

---

## 1. 动机：为什么需要 Decision Graph

在真实工程中，很多问题属于**不完全信息下的启发式搜索**：
- 信息不完备：关键事实/约束/风险要靠探索才能获得
- 多目标冲突：性能/复杂度/兼容性/体验/成本往往不可兼得
- 需要 Look-Ahead：用 Spike/PoC/实验来为决策提供证据
- 需要回溯：撞墙后必须显式撤销某些“已钉死结论”（Soft SSOT → Draft）

与“线性收口到 SSOT”的差异在于：
- SSOT 不再被视为永恒真理，而是分级：
  - **Hard SSOT**：不可轻易回滚（已发布 API、已落盘格式、外部承诺）
  - **Soft SSOT**：当前最佳假设（允许回溯降级，但必须记录证据与原因）

---

## 2. 核心概念

### 2.1 决策点（Decision Point）
一个需要选择的分叉点，例如：
- bit 分配方案选 38:26 还是 40:24？
- API 命名选择 A/B/C？
- 技术路线选方案 X/Y？

### 2.2 选项（Option / Branch）
在某个决策点下的候选分支。每个选项应携带：
- 简短描述（能被人类快速读懂）
- 预期收益/成本/风险（可粗略）
- 前置依赖（事实、工具、外部资源）

### 2.3 证据（Evidence）
支持或反驳某条选项的证据：
- PoC/Spike 的测试结果
- 代码可行性验证
- benchmark 数据
- 规范条款冲突/一致性审阅结论

### 2.4 路径（Path / Candidate Plan）
从根节点（当前问题）出发的一条选择序列，形成一份“方案文本”。

Decision Graph 的关键产物不是图本身，而是：
- **聚合后的方案文本**（沿路径汇总成可读的设计说明/实施计划）
- **可比较的候选集**（方案 A/B/C 的 tradeoff）

---

## 3. 与 Artifact-Tiers 的关系

Decision Graph 不替代 Artifact-Tiers，而是为其提供“搜索与回溯”的结构化载体：

- Resolve：记录“是否值得做/优先级”的选择点与裁决理由
- Shape：记录 API 外观/命名/用户承诺的候选
- Rule：记录约束条款的候选版本与冲突化解路径
- Plan：记录实施路线、依赖排序、里程碑划分
- Craft：记录实现策略分支（例如 PoC vs 正式实现路径），以及由测试/审阅得到的证据

> 实践要点：Decision Graph 的节点应标注其主要落点 Tier，避免“把 Craft 的细节分叉当成 Shape 的分叉”。

---

## 4. 最小数据结构（MVP：JSON 图）

### 4.1 Node Schema（示意）

> 目标：足够简单，能在仓库中存储、diff、review；能被工具聚合输出“方案文本”。

```json
{
  "graphId": "DG-W-0004",
  "root": "D-001",
  "nodes": {
    "D-001": {
      "type": "decision",
      "title": "Bit allocation",
      "tier": "Rule",
      "question": "OffsetBits/LengthBits how to split?",
      "status": "open",
      "options": ["O-001A", "O-001B", "O-001C"],
      "notes": ["why this matters", "constraints"],
      "links": {
        "wish": "wishes/active/wish-0004-SizedPtr.md",
        "design": "atelia/docs/Data/Draft/SizedPtr.md"
      }
    },
    "O-001A": {
      "type": "option",
      "label": "38:26",
      "summary": "Balanced: ~1TB offset, ~256MB length",
      "state": "chosen",
      "pros": ["length headroom"],
      "cons": ["offset smaller than 40:24"],
      "requires": [],
      "evidence": ["E-0001"],
      "next": ["D-002"]
    },
    "E-0001": {
      "type": "evidence",
      "kind": "review|test|poc|benchmark",
      "summary": "Test suite passes; bounds computed",
      "refs": ["atelia/tests/Data.Tests/SizedPtrTests.cs"],
      "result": "support|refute|neutral"
    }
  }
}
```

### 4.2 状态机（最小）
- decision.status: `open | frozen | backtracked`
- option.state: `unexplored | exploring | chosen | rejected`

> 回溯：将 `chosen` 的某些路径节点标记为 `backtracked`，并记录触发证据（例如 PoC 失败、复杂度超阈值）。

---

## 5. 图上导航与聚合（工具的价值点）

即便不做复杂 UI，MVP 工具也可以非常有用：

### 5.1 命令（示意）
- `dg list-open`：列出所有 open decision 点
- `dg show D-001`：展示该决策点的候选与证据
- `dg path --from root --to leaf --format markdown`：输出某条路径的“方案文本”
- `dg compare D-001`：按选项输出对比表（tradeoff）
- `dg backtrack --to D-001 --reason "poc failed" --evidence E-0007`：显式回溯

### 5.2 为什么能减少“细粒度问人类”

Decision Graph 允许 Agent：
- 并行探索多个选项（批量 LLM 调用做启发估计/价值评估）
- 把探索结果聚合为 2-3 个可选方案
- 以“方案级别”向人类询问：
  - "A/B/C 三案 tradeoff 如下，我推荐 B，是否裁决？"

相比在实现细节上频繁问人类，这更符合人类协作者的注意力模式。

---

## 6. 与 LLM / Agent 桥接层的接口

Decision Graph 的关键在于“让 LLM 充当启发函数”，而不是让 LLM 直接成为状态机：

- LLM 负责：
  - 生成候选选项
  - 评估某条路径的可行性/风险/信息增益
  - 推荐下一步最小探针（Look-Ahead）

- 规则代码/工具负责：
  - 存储与版本控制（JSON/Markdown）
  - 聚合输出（路径→方案文本）
  - 约束执行（scope gate、stop condition、回溯记录）

---

## 7. 风险与护栏（MVP 需要提前声明）

- **图爆炸**：分支过多会反噬注意力
  - 护栏：限制每个决策点候选数（例如 3±1），并强制写 stop_condition
- **伪精确**：LLM 可能对价值/风险做“幻觉估计”
  - 护栏：证据必须可引用（测试/PoC/审阅记录），没有证据就标注为假设
- **回溯失控**：频繁回溯导致永远不收敛
  - 护栏：回溯必须绑定触发证据；超过阈值需要人类裁决

---

## 8. 下一步（若要做成工具）

MVP 路线建议：
1) 先用 JSON + CLI 工具跑通：创建节点、导航、聚合方案文本
2) 把输出对接到 Wish 目录结构（每个 Wish 一个 Decision Graph 文件）
3) 再考虑 DocGraph 聚合：自动生成“当前 open decisions / 可选方案”索引
