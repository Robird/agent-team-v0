# Craftsman — 认知入口

---

### 核心洞见 (Insight)

#### 输出物
- `#review` → **FixList**（问题 + 定位 + 建议）
- `#design` → **候选方案 + Tradeoff 表**（至少 2 案，含失败模式/反例）
- `#decision` → **Decision Log**（状态 + 上下文 + 约束 + 回滚条件）

#### 规范工程
- **SSOT 唯一化**：同一事实多载体表达是冗余之源。
- **可判定性优先**：优先提出能写成测试/检查清单的条款。
- **失败语义**：失败后内存状态是否保持不变？失败路径是否唯一？

#### 术语治理
- **Primary Definition + Index**：每术语唯一权威定义。
- **Rename vs Re-home**：Rename 保留 alias，Re-home 保留 Redirect Stub。

#### 简单套壳类型审阅 (2025-12-28)
> Wrapper type 的审计判据与常见陷阱。

**核心判据**：约束是否进入类型的**可判定面**（构造/校验/行为），还是仅停留在文档注释？

| 模式 | 信号 | 风险 |
|:-----|:-----|:-----|
| **纯 API 摩擦** | wrapper 定义条款仅等价于"这是某原始类型"，且规范声明"不解释语义、无保留值域" | 类型安全收益不可判定（典型：`FrameTag(uint)`） |
| **约束双写漂移** | wrapper 存在硬约束（如对齐/null），但约束 SSOT 分散在 wire 文档与接口文档，类型本身不提供 `IsAligned` / `TryCreate` / `CreateChecked` 等机制 | 约束双写 + 漂移风险（典型：`Address64(ulong)`） |
| **注释型别名** | wrapper 的收益主要来自"防混淆"，但提供隐式转换回原始类型 | 实质退化为别名，强类型形同虚设 |
| **SSOT 分叉** | 术语表将概念映射为原始类型，但实现已引入 wrapper（如 `ObjectId(ulong)`） | 实现者/审阅者无法仅凭规范判断"强类型是否存在/应被依赖" |

**可复用审计问句**：
1. 该 wrapper 的关键约束是否进入了类型的可判定面（构造/校验/方法），还是仅停留在规范条款？
2. 若 wrapper 的收益主要来自"防混淆"，但又提供隐式转换回原始类型，它是否已经退化为"注释型别名"？

#### Clause Registry 与文档拆分验收 (2025-12-29)
> StateJournal 文档拆分的可判定验收策略。

**Clause Registry 优先**：
- 从 `mvp-design-v2.md` 提取全部 `[S-*]`/`[A-*]` 唯一条款（当前观测：S 33 + A 18 = 51）
- 为每个条款指定唯一 **Primary Definition**（其余均为引用）
- 正文拆分前先完成 Registry，可显著降低遗漏与漂移风险

**定位策略**：
- 行号定位在文档插入/重排后会系统性失效（实测 51/51 行号不匹配）
- 建议改用：**锚点/标题 + 文件内搜索** 或 **GitHub permalink**
- 或在 CI 中自动重算行号以保持可判定性

**验收回归检查点**：
1. `glossary.md` 首屏是否残留 "SSOT" 措辞（即使后文已改为引用 `clauses/index.md`）
2. 场景卡片（如 `scenarios/loadobject.md`）是否仍残留形如 "(L449)/(L484)" 的硬编码行号引用

#### INV-3 跨文件引用与 SSOT 漂移风险 (2025-12-29)
> "每个条款/术语只有一个权威定义"的边界案例。

**规范语气复述误伤**：
- `glossary.md` 中以引用块复述带 MUST 的条款句子（如 `[S-STATEJOURNAL-TOMBSTONE-SKIP]`）
- 即使 Primary Definition 已由 `clauses/index.md` 指向 `rbf-interface.md`，读者仍可能把 glossary 的复述当成"第二权威源"
- 建议在 glossary 的此类引用旁统一加注：`*(Informative quote; primary definition at ...)*`

**glossary.md 两类 SSOT 漂移风险**：
1. 首屏 `状态：Layer 0 — 所有其他文档隐式依赖本文件` 属于隐式 SSOT 声明，可能违反"glossary 无 SSOT 声明"的验收项
2. `Well-Known ObjectId（保留区）` 章节存在以 MUST 句式给出的"关键约束（摘要）"，即使注明"权威定义见条款"，仍构成对 `[S-OBJECTID-RESERVED-RANGE]` 的规范语气复述

**中文改写不是豁免**：
- 即使把英文 RFC2119 的 "MUST/MUST NOT" 改写成中文"应/不得"
- 如果以"关键约束/摘要"形式重新表述某个 `[S-*]`/`[A-*]` 的规则，而非**原文引用块 + 指向 Primary Definition**
- 读者仍会把 glossary 当作第二权威源，实质上仍是"条款复述"（INV-3/SSOT 漂移风险）

#### RBF 文件交互接口可判定性审计 (2025-12-29)
> MVP 接口（`IRbfFile` + `RbfFile.CreateNew/OpenExisting`）的关键遗漏点。

**Flush 语义明确化**：
- `IRbfFramer.Flush()` 的 remarks 不能再暗示"上层持有底层句柄做 durable flush"
- 应明确：durable 由 `IRbfFile.DurableFlush()` 提供

**Truncate 约束**：
- `Truncate(newLengthBytes)` 建议强制 `newLengthBytes <= LengthBytes` 且 `newLengthBytes % 4 == 0`
- 与 `Address64` 对齐一致，否则恢复期会制造不可判定的半帧/非对齐空间

**异常分类最小集合**：
- 必须固定：Argument/IO/Corruption/Disposed/InvalidOperation
- 否则实现会倒灌为"随平台变化"的错误语义

**暗契约显式化最小集**：
- 视图创建次数（单 framer？）
- Dispose 幂等性 + disposed 后方法异常类型
- Flush 可见性边界
- 并发读写语义（未定义 / 必须 fail-fast）
- Scanner 的快照/跟随语义

#### Scanner 可见性与 D1 保留必要性 (2025-12-29)
> 对 StateJournal 真实 I/O 用法审计后的结论。

**D1（Scanner 可见性/跟随视图）不能简单删除**：
- meta 的 `ScanReverse()` 确实是启动期一次性尾扫
- 但 data 的 `TryReadAt(Address64)` 必须在"同进程同句柄持续 append"下定义增长语义
- 语义：以调用时刻 EOF 为准，EOF 外返回 false，增长后可变为 true

**恢复流程必需能力**：
- 文件层 `Length` 与 `Truncate(DataTail)` 两项能力
- 否则 `[R-META-AHEAD-BACKTRACK]` 与 `[R-DATATAIL-TRUNCATE-GARBAGE]` 无法写成可判定接口契约

#### FramePtr 失败语义三类行为 (2025-12-29)
> offset+length 打包为 u64 把失败语义推到接口边界的审计点。

**必须固定的三类行为**：

1. **Pack/Unpack 的 valid domain 与 fail-fast 策略**
   - 异常类型 或 TryXxx 模式

2. **TryReadAt 语义**
   - 对 Null/EOF 外必须返回 false
   - 对 length mismatch / CRC fail 需固定为 false 或 InvalidDataException

3. **向后兼容性**
   - 若历史保存了裸 u64 指针，必须有外部版本字段或内部版本位
   - 否则无法机械判别 Address64 vs FramePtr

#### 参谋组提示词统一骨架 (2025-12-29)
> 为三位顾问统一 `.agent.md` 的架构洞见。

**设计原则**："理性/感性差异"降级为同一骨架下的"语言皮肤"。

**三层统一骨架**：
| 层 | 内容 |
|:---|:-----|
| **认知透镜 (Lens)** | 每位顾问独特的观察视角 |
| **内部生成协议** | 思考→输出的内部流程 |
| **护栏** | 反承诺 / 真实性两个插槽 |

**外显验收信号**：
- `补位：<盲区>` — 明确标注盲区
- Preview 禁强断言 — 首屏避免锚定
- 可推翻标注 — 标注假设的可推翻性
- 推翻时的修正说明 — 假设被推翻时如何修正

**实施记录 (2025-12-29 00:24)**：已按畅谈会第二轮 Craftsman §4.1 草案更新系统提示词，在身份段落后加入"认知透镜"（可判定性+证据链），新增"内部生成协议"与"护栏"，保持双模态协议定义不变。

### 参与历史索引 (Advisor)
- **StateJournal (RBF/Durability)**: 格式不变式、恢复/截断边界、两阶段提交。
- **术语治理**: Primary Definition + Index。

---

### 审阅本质
> **"审阅不是'多看代码'，而是'在可判定证据链上做裁决'。"**

### 三层审阅模型
| 层次 | 问题 | 产出 |
|:-----|:-----|:-----|
| **L1 符合性** | 代码是否实现了规范？ | V/U/C |
| **L2 完备性** | 规范是否覆盖所有情况？ | 规范盲区 (U) |
| **L3 工程性** | 实现是否"好"？ | I 建议 |

### 核心规则
1. **U 类不是 bug**：规范不可判定 ≠ 代码有问题，必须升级为规范修订。
2. **禁止实现倒灌**：不得因实现存在某行为就推定规范允许。
3. **无法复现的 V 降级为 U**：每个 V 必须有可复现的验证步骤。

### 参与历史索引 (Reviewer)
- **StateJournal**: L1-Commit 审阅 (100% 符合), L1-Workspace 审阅 (92.3% 符合).
- **PipeMux**: 管理命令 RFC 审阅.
- **DocUI**: Proposal 研讨会发言.

---

## 归档与索引

### 认知文件结构
```
agent-team/members/Craftsman/
├── index.md              ← 认知入口（本文件）
├── key-notes-digest.md   ← 对 Key-Note 的消化理解
├── inbox.md              ← 待处理便签
└── inbox-archive.md      ← 已处理便签的归档
```

### 历史归档
- 原 Advisor-GPT 归档: `agent-team/archive/members/Advisor-GPT/`
- 原 CodexReviewer 归档: `agent-team/archive/members/codex-reviewer/`
