# 畅谈会：命名方法论改进

> **日期**：2025-12-22
> **标签**：#jam
> **主持人**：刘德智 (Team Leader)
> **参与者**：Advisor-Claude, Advisor-Gemini, Advisor-GPT
> **状态**：进行中

---

## 背景

刚创建了 `agent-team/wiki/naming-skill-guide.md`，总结了三种命名方法：

| 方法 | 来源 | 核心思路 |
|------|------|----------|
| **全排列淘汰法** | Atelia | 概念词汇集 → 排列组合 → 筛选淘汰 |
| **Cycle Loss 采样法** | StateJournal | 编码采样 → 解码还原 → 选还原度高的 |
| **畅谈会共识法** | RBF | 多视角讨论 → 中场整理 → 收敛投票 |

## 畅谈目标

1. **分析适用性**：三种方法各自适合什么场景？有没有更清晰的选择标准？
2. **发现盲区**：有没有遗漏的方法或改进空间？
3. **实践验证**：用改进后的方法论处理当前问题——`elog/rbf/rlog` 代码标识符命名

## 当前命名问题

| 决策点 | 当前状态 | 问题 |
|--------|----------|------|
| 格式名 | RBF (Reversible Binary Framing) | ✅ 已决定 |
| Magic | `RBF1` | ✅ 已决定 |
| 扩展名 | `.rbf` | ✅ 已决定 |
| **代码标识符** | `ElogFramer`, `ElogFrameBuilder` | ❓ 待决定 |
| **文档文件名** | `elog-format.md`, `elog-interface.md` | ❓ 待决定 |

**候选思路**：
- `Rbf*`：与格式名一致，但 `RbfFramer` 读起来不顺
- `Rlog*`：`r` = reverse，更口语化，`RlogFramer` 像个名词
- 保持 `Elog*`：减少迁移成本，但与正式名 RBF 不一致

## 💬 畅谈记录

### 主持人开场 (刘德智)

这是一个 #jam 类型的畅谈会，欢迎自由畅想！

**问题 1**：三种方法的适用边界是什么？

我的初步想法：
- **全排列淘汰法**：适合"从零创造"，需要独特性
- **Cycle Loss 法**：适合"有多个候选"，需要量化比较
- **畅谈会法**：适合"有复杂约束"，需要多视角权衡

**问题 2**：当前的代码标识符命名问题，应该用哪种方法？

直觉上这不是"从零创造"（已有候选），也不需要大规模采样（候选很少），可能更适合快速讨论收敛？

请各位自由发表想法！

---

### Advisor-Claude 发言

感谢主持人的开场！这是一个很有价值的元认知话题——我们如何系统化"起名"这件事。

---

#### 一、三种方法的适用边界分析

我尝试用一个二维框架来刻画三种方法的"生态位"：

| 方法 | 候选空间 | 约束复杂度 | 核心机制 |
|------|----------|------------|----------|
| **全排列淘汰法** | 开放（需生成） | 低（主要是唯一性） | 暴力搜索 + 过滤 |
| **Cycle Loss 法** | 中等（需采样） | 中（需要"自明性"） | 信息论压缩 |
| **畅谈会法** | 封闭（已有候选） | 高（多维约束冲突） | 社会性共识 |

**关键洞察**：三种方法对应三种不同的**问题结构**：

```
                    约束复杂度
                        ↑
                        │
           畅谈会法 ●   │
        (多维约束冲突)   │
                        │
                        │   ● Cycle Loss 法
                        │  (需要量化"好坏")
                        │
     全排列淘汰法 ●      │
    (主要是唯一性)       │
                        └────────────────→ 候选空间开放度
```

**选择启发式**：
1. 候选空间是**空的**（需要创造）→ 全排列淘汰法
2. 候选空间**可采样**但评价标准模糊 → Cycle Loss 法（让采样过程定义"好"）
3. 候选空间**已闭合**但约束相互冲突 → 畅谈会法（需要人类/多视角仲裁）

---

#### 二、遗漏的方法？——两个补充

**1. 类比锚定法（Analogy Anchoring）**

> **适用场景**：当概念有明确的"领域前辈"时

**步骤**：
1. 找到最接近的已知概念（如 Git、LSP、React）
2. 分析该概念的命名风格（术语体系、缩写惯例）
3. 在相同风格下构造新名字

**示例**：
- `VersionIndex` 借用 Git 的 "Index" 概念
- `Context-Projection` 借用 Event Sourcing 的 "Projection"
- `Capability-Provider` 借用 LSP 的 Provider 术语体系

**优势**：降低读者认知负担——利用已有心智模型

**缺陷**：可能产生错误期望（如果类比不精确）

---

**2. 反向排斥法（Negative Selection）**

> **适用场景**：当"不要什么"比"要什么"更清晰时

**步骤**：
1. 列出**绝对不能是**的特征（如：已被占用、发音困难、有负面联想）
2. 用排斥条件过滤全部候选
3. 在幸存者中随意选择（因为它们"足够好"）

**示例**：RBF 命名过程中：
- ❌ 不能是 "ELOG"（与 elastic log 冲突）
- ❌ 不能暗示 "Log"（这不是日志系统）
- ❌ Magic 不能是非 ASCII

**优势**：快速收敛，避免"完美主义陷阱"

**缺陷**：可能选到"不坏但也不好"的平庸选项

---

#### 三、方法论的元结构

我发现三种方法可以用一个统一的框架描述：

```
命名 = argmin_name Loss(name)

其中 Loss 可以是：
- 全排列淘汰法: Loss = IsOccupied(name) + IsBadPronunciation(name) + ...
- Cycle Loss 法: Loss = |Decode(name) - original_description|
- 畅谈会法: Loss = Σ (1 - Agree(advisor_i, name))
```

**统一视角**：所有方法都是**在某个损失函数下的搜索**，差异在于：
1. **搜索策略**（暴力/采样/社会性协商）
2. **损失函数定义**（规则/信息论/共识）

这个视角暗示了一个**混合策略**：
- 先用全排列/采样生成候选（探索）
- 再用畅谈会精选（利用）

---

#### 四、当前问题分析：`elog/rbf/rlog` 代码标识符

**问题性质判定**：
- 候选空间：已封闭（Rbf*/Rlog*/Elog* 三选一）
- 约束复杂度：中等
  - 与格式名一致性 → 倾向 Rbf*
  - 代码可读性（像名词）→ 倾向 Rlog*
  - 迁移成本 → 倾向 Elog*
- 无需量化采样（候选太少，Cycle Loss 无意义）

**我的判定**：适用**畅谈会法**或其简化版（快速投票）

---

**具体分析**：

| 候选 | 格式一致性 | 可读性 | 迁移成本 | 综合评价 |
|------|------------|--------|----------|----------|
| `Rbf*` | ✅ 完全一致 | ⚠️ `RbfFramer` 三辅音连读 | 中 | 最"正确"但略拗口 |
| `Rlog*` | ❌ 丢失 "Framing" 语义 | ✅ `RlogFramer` 像名词 | 中 | 易读但语义偏移 |
| `Elog*` | ❌ 与正式名完全不一致 | ✅ 已习惯 | ✅ 零成本 | 短期舒适，长期困惑 |

**我的倾向**：`Rbf*`

**理由**：
1. **术语一致性原则**：格式名 RBF → 代码应该用 Rbf，这是最低认知负担的映射
2. **"拗口"是习惯问题**：`RbfFramer` 多读几次就顺了，但名字不一致会持续造成困惑
3. **迁移成本是一次性的**：Rename 之后就结束了，术语不一致的困惑是长期的

**但有一个顾虑**：`RbfFrameBuilder` 会变成 `RbfRbfBuilder` 吗？不会——应该是 `RbfFrameBuilder`，因为 Frame 不等于 RBF。

---

#### 五、文档文件名

| 当前 | 建议 | 理由 |
|------|------|------|
| `elog-format.md` | `rbf-format.md` | 与格式名一致 |
| `elog-interface.md` | `rbf-interface.md` 或 `rbf-api.md` | 同上 |

这是更明确的决策——文档应该用正式格式名。

---

#### 六、疯狂想法：命名决策的"类型系统"

> ⚠️ 以下是 #jam 模式的自由畅想

如果我们把命名方法论再抽象一层，可以设计一个"命名决策的类型系统"：

```typescript
type NamingProblem = {
  candidateSpace: "empty" | "samplable" | "closed";
  constraints: Constraint[];
  evaluationMode: "rule-based" | "information-theoretic" | "consensus";
};

type Constraint =
  | { kind: "uniqueness"; scope: "github" | "project" | "file" }
  | { kind: "phonetic"; requirement: "pronounceable" | "memorable" }
  | { kind: "semantic"; requirement: "self-explanatory" | "domain-aligned" }
  | { kind: "technical"; requirement: "valid-identifier" | "length-limit" };

function selectMethod(problem: NamingProblem): NamingMethod {
  if (problem.candidateSpace === "empty") return "全排列淘汰法";
  if (problem.evaluationMode === "information-theoretic") return "Cycle Loss 法";
  return "畅谈会法";
}
```

这个"类型系统"可以帮助快速判断用哪种方法——填入问题属性，自动推导方法。

---

#### 七、总结

| 议题 | 我的观点 |
|------|----------|
| 三种方法的边界 | 按"候选空间开放度 × 约束复杂度"二维分类 |
| 遗漏的方法 | 类比锚定法、反向排斥法 |
| 统一框架 | 所有方法都是"在损失函数下搜索"，差异在搜索策略和损失定义 |
| 当前问题（代码标识符） | 适用畅谈会法，倾向 `Rbf*` |
| 当前问题（文档文件名） | 应改为 `rbf-*.md` |

期待其他 Advisors 的视角！🎤

---

### Advisor-GPT 发言

从“精确性/规范审计/可执行性”视角，我想把讨论拆成两层：**方法论是否可操作**、以及**本题约束如何落到可判定选择**。

#### 1) 方法论可操作性：现在还缺“可审计产物”

`agent-team/wiki/naming-skill-guide.md` 里方法 C（畅谈会共识法）流程顺，但缺少能让团队“落地后不反复争论”的最小产物定义。我建议把方法 C 补成“4 个可复制粘贴的表/字段”，这样每次命名都能机械执行：

- **Name Surface Matrix（命名表面清单）**：本次涉及哪些“名字表面”（格式名 / magic / 扩展名 / 文档名 / 代码标识符 / public API / 内部实现）？哪些已经 SSOT 锁定？
- **Constraint Table（约束表）**：每条约束标注 `MUST/SHOULD/MAY`，并写清“违反后果”（会导致实现/文档分叉？迁移不可逆？只是读起来怪？）
- **Candidate Scorecard（候选打分卡）**：同一组维度对比（例如：SSOT 一致性、可搜索性、语义漂移风险、发音/识别、迁移成本），避免讨论跑题。
- **Decision Record（最小决策记录）**：一句话结论 + 2-3 条理由 + 1 条“保留/兼容策略”（alias / obsolete / redirect），保证未来可追溯。

这四样产物能把“畅谈会法”从讨论形式变成工程化流程（也更符合你们在术语治理里追求的 SSOT/可审计闭环）。

#### 2) 当前问题约束分析：这是“SSOT 一致性优先”的典型

本题的关键约束不是“哪个更顺口”，而是：**格式名 SSOT 已经决定为 RBF**，并且 magic 与扩展名也锁定为 `RBF1` / `.rbf`。因此代码标识符/文档文件名如果继续用 `Elog*`，就会产生长期二义：

- **`Elog*` 的风险（长期）**：与 SSOT（RBF）不一致，后续读者会反复问“ELOG 和 RBF 什么关系？”；同时 GitHub/文档搜索会被割裂（搜 rbf 找不到代码入口，搜 elog 找不到规范入口）。
- **`Rlog*` 的风险（语义漂移）**：引入新词根 `Rlog`，既不在 SSOT 里也不直指 Framing，还容易让人误读为“reverse log / logging 系统”；从规范审计角度看，这是额外新增概念，不是映射。
- **`Rbf*` 的代价（一次性）**：主要是迁移成本与轻微拗口（`RbfFramer`）；但这是一次性成本，且不会引入第二套概念系统。

#### 3) 推荐

- **推荐选择：`Rbf*` 作为 canonical 前缀**（代码标识符与文档文件名都跟随 SSOT）。
- **兼容策略（如果担心迁移）**：保留 `Elog*` 作为短期 alias（例如类型转发/obsolete），但明确它是 deprecated 名称，不进入新文档与新 API。
- **读音/名词性问题的工程化解法**：如果 `RbfFramer` 读起来别扭，可以把“Framer”这层名词再精确化为 `RbfReader/RbfWriter` 或 `RbfEncoder/RbfDecoder`（仍保持 `Rbf*` 前缀不变），用更熟悉的名词替代“口感”争议。

我的投票顺序：`Rbf*` >>> `Elog*`（仅作为 deprecated alias）> `Rlog*`。

---

### 主持人总结 (刘德智)

#### 方法论改进共识

| 改进点 | 来源 | 建议 |
|--------|------|------|
| **适用边界框架** | Claude | 二维坐标：候选空间开放度 × 约束复杂度 |
| **补充方法** | Claude | 类比锚定法、反向排斥法 |
| **可审计产物** | GPT | 4 个模板：Name Surface Matrix / Constraint Table / Candidate Scorecard / Decision Record |
| **统一视角** | Claude | 所有方法都是"在损失函数下搜索" |

#### 代码标识符命名决定

**共识**：`Rbf*` 作为 canonical 前缀

| 顾问 | 立场 | 理由 |
|------|------|------|
| Claude | ✅ `Rbf*` | 术语一致性优先，"拗口"是习惯问题 |
| GPT | ✅ `Rbf*` | SSOT 一致性优先，避免长期二义 |
| 主持人 | ✅ `Rbf*` | 同意上述理由 |

**具体映射**：

| 当前 | 新名 | 类型 |
|------|------|------|
| `elog-format.md` | `rbf-format.md` | 文档 |
| `elog-interface.md` | `rbf-interface.md` | 文档 |
| `elog-test-vectors.md` | `rbf-test-vectors.md` | 文档 |
| `IElogFramer` | `IRbfFramer` | 接口 |
| `ElogFrameBuilder` | `RbfFrameBuilder` | 类型 |
| `IElogScanner` | `IRbfScanner` | 接口 |

**GPT 的补充建议**（可选）：
- 如果 `RbfFramer` 读起来别扭，可用 `RbfReader/RbfWriter` 替代
- 保留 `Elog*` 作为 deprecated alias（短期兼容）

---

## 📋 行动项

1. [ ] 更新 `naming-skill-guide.md`（融入 Claude 的框架 + GPT 的 4 个产物模板）
2. [ ] 执行文档重命名（`elog-*.md` → `rbf-*.md`）
3. [ ] 更新文档内的类型名引用

**状态**：待监护人批准执行

---

