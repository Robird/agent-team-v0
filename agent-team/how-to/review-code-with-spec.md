# 规范驱动代码审阅配方 (Spec-Driven Code Review Recipe)

> **版本**: 1.0.0  
> **创建**: 2025-12-26  
> **状态**: Draft → 待 Pilot 验证  
> **来源**: [畅谈会记录](../meeting/2025-12-26-code-review-methodology-jam.md)

---

## 概述

### 什么是规范驱动审阅

传统代码审阅是**逆向工程意图**——审阅者从代码推断"作者想做什么"，再判断"做对了吗"。

规范驱动审阅是**验证翻译**——检验 `Spec → Code` 这个映射是否保真：

```
传统审阅：  Code → (逆向推断) → Intent → (判断) → 正确性
规范驱动：  Spec → (正向推导) → Code → (比对) → 符合性
```

**核心定义**：

> **审阅不是"多看代码"，而是"在可判定证据链上做裁决"，并把不可判定的部分强制升级为规范修订的工作项。**

### 适用场景

- 有明确规范文档的代码库
- 规范文档有可引用的条款（如 `[CLAUSE-ID]`）
- 希望通过 SubAgent 并行审阅

### 核心产出

| 产出 | 说明 |
|:-----|:-----|
| **FindingList** | 结构化的审阅发现列表 |
| **U 类清单** | 规范不可判定的点，需要修订 |
| **L3 建议** | 工程性改进建议（可选） |

---

## 三层审阅模型

### 层次定义

| 层次 | 问题 | 类比 | 输入 | 输出 |
|:-----|:-----|:-----|:-----|:-----|
| **L1 符合性** | 代码是否实现了规范？ | 合同履约审 | Spec条款 + Code | V/U/C |
| **L2 完备性** | 规范是否覆盖了所有情况？ | 合同文本修订审 | Code边界 + Spec | 规范盲区 |
| **L3 工程性** | 实现是否"好"？ | 代码品味 | Code + 经验 | I 建议 |

**关键区分**：
- **L1 有客观答案**——条款 X 要求行为 Y，代码是否满足？
- **L2 发现新问题**——代码处理了规范未明确的边界
- **L3 主观判断**——命名、结构、风格，不改变外部语义

### 裁决类型 (VerdictType)

| 类型 | 符号 | 含义 | 允许 severity | 后续流程 |
|:-----|:-----|:-----|:-------------|:---------|
| **Violation** | `V` | 违反条款 | ✅ Critical/Major/Minor | 修复 |
| **Underspecified** | `U` | 规范不可判定 | ❌ | 澄清/修订 |
| **Conform** | `C` | 符合条款 | ❌ | 无 |
| **Improvement** | `I` | 改进建议 | ✅ Nit | 可选 |

**核心规则**：
- `U` 类不是 bug，不进入 bug queue
- `U` 类必须有 `clarifyingQuestions` 和 `specChangeProposal`
- 无法复现的 `V` 必须降级为 `U`

---

## 权威源定义 (Normative Sources)

### 声明原则

**`[CR-01]` MUST** 明确 L1 的权威输入集合：
- 哪些规范文件/条款版本
- 哪些测试向量
- 哪些错误/结果规范

**`[CR-02]` MUST** 声明"禁止实现倒灌"：
- L1 不得因为实现存在某行为就推定规范允许
- 此类必须标为 `U` 类或进入 L2

### 版本锁定

每次审阅必须记录 `specRef`：

```yaml
specRef:
  commit: "abc123"           # Git commit SHA
  tag: "v2.0"                # 或版本标签
  files:
    - path: "docs/mvp-design-v2.md"
      section: "§3-§5"
    - path: "docs/rbf-interface.md"
      section: "全文"
```

---

## 任务分解策略

### 分组原则

**按模块**（推荐）：
- 与代码目录结构对齐
- 上下文边界清晰

**按条款域**：
- 功能性条款一组
- 错误处理条款一组

### 粒度指南

**`[CR-07]` MUST** 每个 SubAgent 任务有明确边界：
- 最多 **N 个条款**（建议 5-10 条）
- 或 **1 个模块**
- 必须提供"定位入口"（文件/符号/测试名）

### 依赖包

**`[CR-03]` SHOULD** 为每个条款维护显式依赖：
- 术语定义
- 前置不变量
- 相关条款

依赖包随任务包自动打包，防止断章取义。

### L0 Gate（可选）

**`[CR-10]` MAY** 引入 L0 前置检查：
- [ ] 编译通过
- [ ] 测试通过
- [ ] 格式化通过

减少 L1 噪音与误判。

---

## Review-to-Closure：审阅驱动的质量闭环（LLM 版）

> 目的：把“测试通过”之后的质量改进变成可调度、可终止、可复盘的循环，而不是一次性评论。

### 为什么需要闭环

LLM 参与实现时常见失败模式：
- **漏读/误读规范或设计条款**（上下文有限）
- **实现与文档漂移**（文档里出现“伪实现”，代码里是另一种结构）
- **范围漂移**（审阅者提出并发安全/大量工具函数等超出目标范围的建议）

因此，审阅应该默认是一个循环：

```
Review → Fix → Re-Review → (通过则关闭 / 失败则继续)
```

### Scope Gate：范围闸门（防范围外建议泛滥）

在发起审阅任务前，MUST 写清：

- **In-Scope（允许审阅）**：
  - Spec/Design ↔ Code/Test 一致性（保真度）
  - 正确性/边界/溢出/异常语义
  - API 误用成本（仅限不引入破坏性变更的微调）
  - 可维护性（不改变外部语义的重构/去重）

- **Out-of-Scope（禁止审阅）**（示例，可按项目定制）：
  - 多线程/并发安全改造
  - 新增大量便捷方法/工具函数
  - 扩展到其他模块/消费者接入（应拆分为新 Wish/Issue）
  - 改变已决策的关键参数（例如 bit 分配、格式契约）

**规则**：Out-of-Scope 建议必须进入 Parking Lot（只记录，不执行）。

### 交付物：FixList + Parking Lot

- **FixList**（结构化、可执行，建议分级）：
  - **Sev0**：规范/设计违背、错误实现、潜在数据损坏
  - **Sev1**：高概率误用、关键边界缺失、错误异常语义、名实不符的测试/注释
  - **Sev2**：可读性、一致性、微小去重、防御性改进（不改变语义）

- **Parking Lot**：范围外建议（最多 3 条，避免变成“许愿池”）。

### Stop Condition（何时关闭）

推荐的可判定闭环条件：

> **连续一次 Re-Review 未发现 Sev0/Sev1，即关闭本次 review loop**。

（注：Sev2 可按投入产出决定是否继续。）

---

## 预防“伪实现漂移”的规则：先有 Shape-Tier SSOT，再进入 Craft

> 实验现象：当缺失明确的 Shape-Tier 产物（例如 API 外观/命名/契约 SSOT），团队往往会在设计文档中用“伪实现代码块”来代替 API 定义。
> 这会导致后续 Craft 实现时出现“文档结构 ≠ 真实代码结构”的漂移，并诱发审阅范围外建议（审阅者倾向于要求“补齐伪实现”）。

因此建议在进入 Craft 前补齐最小 Shape-Tier 产物：

- **API 外观 SSOT**：一份简短的 API 定义（属性/方法签名 + 语义要点），不要用大段伪实现来承载 SSOT。
- **SSOT 声明**：明确“实现 SSOT 是哪个源文件/测试文件；文档不内嵌伪实现代码以避免漂移”。

这条规则不要求写长文档，但要求“外观与契约”有落点。

---

## 任务包模板 (Mission Brief Template)

### YAML Schema

```yaml
# Mission Brief for L1 Code Review
# Schema Version: 1.0

meta:
  briefId: "L1-{module}-{date}-{seq}"
  reviewType: "L1"                    # L1 | L2 | L3
  createdBy: "strategic-session"
  createdAt: "2025-12-26T10:00:00Z"
  specRef:
    commit: "abc123"
    tag: "v2.0"

focus:
  module: "Workspace"
  clauses:
    - id: "[WS-IDENTITY-01]"
      title: "IdentityMap 单例保证"
      specFile: "mvp-design-v2.md"
      specSection: "§4.3.1"

codeLocations:
  entryPoints:
    - "src/StateJournal/Workspace/IdentityMap.cs"
  relatedTests:
    - "tests/StateJournal.Tests/Workspace/IdentityMapTests.cs"
  excludePatterns:
    - "**/obj/**"
    - "**/bin/**"

dependencyContext:
  requiredClauses:
    - id: "[CORE-PTR64-01]"
      summary: "Ptr64 是对象地址的 8 字节表示"
  requiredTerms:
    specFile: "mvp-design-v2.md"
    section: "§2 术语表"

instructions:
  persona: "L1-Judge"
  mustDo:
    - "逐条款检查代码是否满足规范语义"
    - "每个 Finding 必须引用条款原文和代码位置"
    - "遇到规范未覆盖的行为，标记为 U 类"
    - "每个 V 类必须提供可复现的验证步骤"
  mustNot:
    - "不要评论代码风格（那是 L3）"
    - "不要假设规范未写的约束"
    - "不要产出无法复现的 Finding"

  # 可选：Review-to-Closure 扩展字段（强烈建议用于 LLM 审阅）
  inScope:
    - "Spec/Design ↔ Code/Test 一致性"
    - "边界/溢出/异常语义"
  outOfScope:
    - "并发安全/多线程改造"
    - "新增大量便捷方法"
  stopCondition:
    - "连续一次 Re-Review 无 Sev0/Sev1"

outputFormat:
  file: "findings/L1-{module}-{date}.md"
  schema: "EVA-v1"
```

### Markdown 简化版

见 [Mission Brief 模板示例](#附录-c-mission-brief-示例)

---

## Finding 输出格式 (EVA-v1)

### Schema 定义

```yaml
# Finding Schema EVA-v1
type: object
required: [id, verdictType, clauseId, evidence, action]
properties:
  id:
    type: string
    description: "唯一标识，格式：F-{ClauseId}-{hash}"
  verdictType:
    enum: [V, U, C, I]
  severity:
    enum: [Critical, Major, Minor, Nit]
    description: "仅 V 和 I 允许"
  clauseId:
    type: string
  specRef:
    type: object
    properties:
      file: { type: string }
      section: { type: string }
      quote: { type: string, description: "短引用" }
  evidence:
    type: object
    required: [specQuote, codeLoc, repro]
    properties:
      specQuote: { type: string }
      codeLoc: { type: string, format: "file:line" }
      snippet: { type: string }
      repro:
        type: object
        required: [type]
        properties:
          type: { enum: [existingTest, newTest, manual] }
          ref: { type: string }
          howToVerifyFix: { type: string }
  action:
    type: string
    description: "建议的修复/修订方案"
  dedupeKey:
    type: string
    description: "clauseId + normalizedCodeLoc + verdictType + signature"

# U 类特有字段
  clarifyingQuestions:
    type: array
    items: { type: string, description: "Yes/No 形式优先" }
  specChangeProposal:
    type: string
```

### 输出示例

```markdown
---
id: "F-COMMIT-01-a1b2c3"
verdictType: "V"
severity: "Critical"
clauseId: "[COMMIT-01]"
specRef:
  file: "mvp-design-v2.md"
  section: "§5.2"
dedupeKey: "COMMIT-01|Commit/CommitContext.cs:42|V|no-lock"
---

# 🔴 V: [COMMIT-01] 提交时未检查 DirtySet 锁

## 📝 Evidence

**规范**:
> "在构建 CommitContext 之前，必须获取 DirtySet 的**写锁**" (§5.2)

**代码**: [`src/StateJournal/Commit/CommitContext.cs:42`]
```csharp
public CommitContext Create(Workspace ws) {
    var dirty = ws.DirtySet.GetAll(); // ❌ 无锁
    return new CommitContext(dirty);
}
```

**复现**:
- 类型: `newTest`
- 参考: 需新增 `CommitRaceTests.cs`
- 验证修复: 运行 `dotnet test --filter CommitRace`

## ⚖️ Verdict

**判定**: V (Critical) — 违反显式锁契约，并发环境下会破坏原子性

## 🛠️ Action

包裹写锁：
```csharp
using (ws.Lock.Write()) {
    var dirty = ws.DirtySet.GetAll();
    return new CommitContext(dirty);
}
```
```

---

## 执行流程

### Phase 1: 并行审阅

```
┌─────────────────────────────────────────────────────┐
│  Dispatcher (Team Leader)                           │
│  - 生成 Mission Brief × N                           │
│  - 派发给 SubAgent                                  │
└──────────────────────┬──────────────────────────────┘
                       │
        ┌──────────────┼──────────────┐
        ▼              ▼              ▼
   [SubAgent 1]   [SubAgent 2]   [SubAgent N]
   Module: Core   Module: Objects Module: ...
        │              │              │
        └──────────────┼──────────────┘
                       ▼
              FindingList × N
```

### Phase 2: Triage

1. **合并** FindingLists
2. **去重**（按 `dedupeKey`）
3. **分级**：
   - `V(Critical/Major)` → P0
   - `V(Minor)` → P1
   - `U` → L2 修订队列
   - `I` → P2（可选）

### Phase 3: Fix

```
Issue(P0/P1) → [Assign] → InProgress → [Fix] → NeedsVerify
```

### Phase 4: Verify

- **局部重审**：只针对被修复的条款/模块
- **运行测试**：验证 `repro.howToVerifyFix`
- **状态更新**：NeedsVerify → Closed / Reopened

---

## 闭环机制

### Finding 状态机

```
     ┌─────────────────────────────────────────┐
     │                                         │
New ──→ Triaged ──→ InFix ──→ NeedsVerify ──→ Closed
     │                │              │
     └────────────────┴──────────────┴──→ Reopened
```

### U 类升级流程

```
U Finding
    │
    ├─→ clarifyingQuestions (Yes/No)
    │         │
    │         ▼
    │   Guardian/Lead 决策
    │         │
    │    ┌────┴────┐
    │    ▼         ▼
    │  Accept   Revise Spec
    │    │         │
    │    ▼         ▼
    │  Close    L2 修订 PR
    │              │
    └──────────────┴──→ Decision Log
```

### 即时验证

**`[CR-12]` MUST** 对每个 `V` 类提供可复现验证步骤：

```bash
# 验证单个 Finding
./scripts/verify-fix.sh F-COMMIT-01-a1b2c3

# 执行：
# 1. 解析 Finding 获取 clauseId 和 repro.ref
# 2. 运行相关测试
# 3. 如果通过，更新状态为 Closed
```

---

## 成本控制

### 去抖策略

**`[CR-08]` MUST** 流式触发必须有去抖/合并策略：
- 同一变更集只触发一次 L1
- 重复 Finding 自动合并（按 `dedupeKey`）

### 增量 vs 全量

| 触发条件 | 审阅范围 | 频率 |
|:---------|:---------|:-----|
| 任务完成 | 变更触及的条款/模块 | 每次 |
| 里程碑 | 全量 | Sprint 结束 |
| 手动 | 指定范围 | 按需 |

### 成本记录

每次审阅记录：

```yaml
metrics:
  clauseCount: 8
  subAgentCount: 1
  durationMinutes: 15
  findingCount: 5
  validFindingCount: 4  # SNR = 80%
```

---

## 附录

### 附录 A: CR 条款速查表

| 编号 | 级别 | 内容 |
|:-----|:-----|:-----|
| CR-01 | MUST | 明确权威源（Normative Sources） |
| CR-02 | MUST | 禁止实现倒灌 |
| CR-03 | SHOULD | 条款依赖显式化 |
| CR-04 | MUST | Finding 结构化输出（EVA） |
| CR-05 | MUST | U 类必须有澄清问题和修订案 |
| CR-06 | SHOULD | 条款多实现点必须枚举 |
| CR-07 | MUST | 任务边界明确（N 条款 / 1 模块） |
| CR-08 | MUST | 流式触发去抖 |
| CR-09 | SHOULD | 增量审阅优先 |
| CR-10 | MAY | L0 Gate（编译/测试/格式） |
| CR-11 | MUST | 裁决权分离（L1 vs L2） |
| CR-12 | MUST | V 类必须可复现 |
| CR-13 | SHOULD | 上诉/复核步骤 |
| CR-14 | MUST | L3 不得使用"违反条款"措辞 |
| CR-15 | SHOULD | L3 建议需有测量方式 |

### 附录 B: 常见误判清单

| 误判类型 | 症状 | 防御 |
|:---------|:-----|:-----|
| 定位误判 | grep 漏掉间接实现 | 多点证据：入口+不变量+测试 |
| 形状偏好 | 要求特定代码形状 | 只基于可观察行为判定 |
| 测试=规范 | 测试覆盖≠条款满足 | 证据链三要素 |
| 断章取义 | 忽略条款依赖 | 依赖包打包 |
| Happy path 盲区 | 只看正常路径 | 检查失败路径+状态保持 |
| 实现倒灌 | 实现存在→规范允许 | 强制 U 类 |

### 附录 C: Mission Brief 示例

```markdown
# 审阅任务：Core 模块 L1 符合性审阅

## 🎯 焦点

**模块**：`src/StateJournal/Core/`
**specRef**: `abc123` (mvp-design-v2.md v2.0)

**条款清单**：
| ID | 标题 | 规范位置 |
|:---|:-----|:---------|
| `[F-PTR64-DEFINITION]` | Ptr64 8 字节对齐 | §2.1 |
| `[F-VARINT-ENCODE]` | VarInt 编码规则 | §2.2 |

## 🔍 代码入口

- `src/StateJournal/Core/Ptr64.cs`
- `src/StateJournal/Core/VarInt.cs`
- 相关测试：`tests/.../Core/*Tests.cs`

## 📚 依赖上下文

**前置条款**：无（Core 是基础模块）

## 📋 审阅指令

**角色**：L1 符合性法官

**MUST**：
- 逐条款检查
- 引用条款原文 + 代码位置 + 复现方式
- 规范未覆盖 → U 类

**MUST NOT**：
- 评论风格
- 假设未写约束
- 无法复现的 Finding

## 📤 输出

文件：`findings/L1-Core-2025-12-26.md`
格式：EVA-v1
```

---

## 变更日志

| 版本 | 日期 | 变更 |
|------|------|------|
| 1.0.0 | 2025-12-26 | 初始版本，来自畅谈会共识 |

