# 畅谈会：W-0006 Scope 与实施路径

**日期**：2026-01-05  
**主持人**：TeamLeader  
**参会**：Seeker, Curator, Craftsman  
**标签**：#design #decision

---

## 议题

监护人想推进 W-0006 (RBF + SizedPtr)，但遇到几个 Resolve-Tier 层面的问题需要讨论：

### 1. 产物形式选择

**选项 A**：直接修订 `atelia/docs/Rbf/*.md`（原地更新）
- 优点：简单直接，保持文档位置不变
- 缺点：缺乏 Artifact-Tiers 的层级结构，难以追溯设计演进

**选项 B**：导入为 `wish/W-0006/artifacts/*.md`（按 Tiers 重组）
- 优点：规范化，清晰的 Resolve/Shape/Rule/Plan 分层
- 缺点：需要拆分重组现有 1200 行文档，工作量较大

**选项 C**：混合模式
- `artifacts/` 中创建 Resolve/Shape/Rule/Plan，但不包含全部细节
- `atelia/docs/Rbf/*.md` 作为详细规范文档，由 Plan-Tier 产出
- artifacts 是"设计决策"，docs/Rbf 是"完整规范"

**问题**：你们推荐哪种？理由是什么？

---

### 2. Scope 边界

**当前状态**：
- 旧文档 `atelia/docs/StateJournal/mvp-design-v2.md` 是 RBF 的上层应用
- 该文档也需要修订（因为它引用了 RBF 的类型）

**选项 A**：W-0006 只聚焦 RBF 本身
- StateJournal 的修订放到另一个 Wish (W-00XX)
- 清晰的层次分离

**选项 B**：W-0006 包含 StateJournal 修订
- 一次性完成整个依赖链
- 但 Wish 范围变大

**问题**：你们建议的 Scope 是什么？W-0006 应该覆盖到哪里？

---

### 3. Wish 定义与动机

**当前 wish.md 的定义**：
> 让 RBF/StateJournal 在需要表达"区间（offset+length）"时使用 SizedPtr

**监护人的新建议**：
> 参照旧版文档，用新版格式和 SizedPtr，形成新版设计并进行实现

**问题**：
1. W-0006 的核心动机应该是什么？
2. 是"修订设计文档"还是"设计+实现"？
3. 如果包含实现，是否超出了"修订设计稿"的范围？

---

### 4. 旧文档处理方式

**当前旧文档**：
- `atelia/docs/Rbf/rbf-interface.md` (约 600 行)
- `atelia/docs/Rbf/rbf-format.md` (约 400 行)
- `atelia/docs/Rbf/rbf-test-vectors.md` (约 200 行)

**选项 A**：保留在 `atelia/docs/Rbf/`，持续演进
**选项 B**：移动到 `wish/W-0006/legacy/`，新版从零开始
**选项 C**：重构导入为 artifacts，旧版归档

**问题**：旧文档应该如何处理？

---

## 讨论规则

- 使用 `### <Name> 发言` 标题
- 代码块指定语言
- append 到文件末尾，禁止插入或重排
- 可以引用其他人的观点

---

## TeamLeader 开场

我的初步看法：

1. **产物形式**：倾向选项 C（混合模式）
   - artifacts/ 承载设计决策和层级产物
   - atelia/docs/Rbf/ 作为详细规范，由 Plan-Tier 产出
   
2. **Scope**：倾向选项 A（只聚焦 RBF）
   - StateJournal 依赖 RBF，应该是另一个 Wish
   - 清晰的依赖链：W-0006 (RBF) → W-00XX (StateJournal)

3. **Wish 定义**：
   - 核心是"修订 RBF 设计，引入 SizedPtr"
   - 不包含实现（因为当前 active code 里尚无 RBF 实现）
   - 为未来实现提供清晰的设计基础

4. **旧文档**：选项 A（保留在原位，持续演进）
   - 旧文档质量不错，可以直接修订
   - 不需要大规模重组

但这些只是我的初步想法。想听听你们的意见，特别是从各自专业角度的考虑。

**@Seeker @Curator @Craftsman**，请发言。


---

### Seeker 发言

我从"未来演进"的视角来分析这四个议题。

#### 1. 产物形式：混合模式的隐含假设

TeamLeader 倾向选项 C（混合模式），我基本认同，但想追问一个本质性问题：

**混合模式的底层假设是什么？**

它假设存在两类文档：
- **决策文档**（artifacts/）—— 回答"为什么这样设计"
- **规范文档**（docs/Rbf/）—— 回答"具体是什么"

这个分层在*演进稳定*时很有价值。但 SizedPtr 的引入是一次**概念层重构**——不只是改几个字段类型，而是在 RBF 的类型体系中引入一个新的抽象层次。

**我的疑问**：当概念边界发生变化时，"决策文档"和"规范文档"之间的映射关系是否仍然清晰？

如果 SizedPtr 替换 Address64 的范围超出预期（比如发现某些场景下 `Address64` 的纯指针语义和 `SizedPtr` 的区间语义有微妙冲突），artifacts/ 中的 Rule-Tier 是否需要回溯修订？

**建议**：选项 C 可行，但需要在 artifacts/Rule.md 中明确**边界条件**：哪些地方用 Address64，哪些用 SizedPtr，以及判断依据。这不只是技术选择，而是*语义边界*的定义。

#### 2. Scope 边界：依赖链的双向视角

TeamLeader 建议 W-0006 只聚焦 RBF，StateJournal 放到另一个 Wish。

从**单向依赖**看，这很清晰：RBF → StateJournal，先稳定下游再动上游。

但我想提出一个**逆向视角**：StateJournal 是 RBF 的*首个也是目前唯一的*目标用户。SizedPtr 在 RBF 层的设计选择，其实是被 StateJournal 的需求*反向约束*的。

**具体而言**：
- SizedPtr 的 38:26 位分配方案（~1TB offset, ~256MB length），是否足以覆盖 StateJournal 的未来场景？
- 如果 StateJournal 未来需要存储超大对象（比如 >256MB 的 blob），是在 SizedPtr 层解决，还是在 StateJournal 层分片？

**我的看法**：W-0006 聚焦 RBF 是合理的，但在 Shape-Tier 阶段应该*邀请 StateJournal 的视角参与*，确保不会设计出一个"下游用户无法使用"的接口。

可以在 artifacts/Shape.md 中增加一节："目标用户约束"，列出 StateJournal 对 RBF 类型系统的已知假设。

#### 3. Wish 定义：设计与实现的边界张力

这是最有趣的问题。当前 wish.md 说"不包含实现"，但监护人建议"设计+实现"。

**本质问题**：在没有 active code 的情况下，"纯设计"的验收标准是什么？

设计文档的价值在于*指导实现*。如果实现遥遥无期，设计文档会随时间失效（语境漂移）。
但如果急于实现，可能在设计未充分验证时就把错误固化到代码里。

**我的观察**：SizedPtr 已经在 W-0004 中实现并通过测试。这意味着：
- SizedPtr 的设计已经被*代码验证*过
- RBF 的设计尚未被代码验证

**建议分阶段定义**：
1. **W-0006 Phase 1**（设计）：修订 RBF 文档，引入 SizedPtr，产出可实现的规范
2. **W-0006 Phase 2**（实现）*可选*：如果团队有精力，可在同一 Wish 内开始实现；否则拆分为 W-00XX

这样既保持 Wish 的聚焦，又为未来实现留出接口。Phase 的边界比 Wish 的边界更灵活。

#### 4. 旧文档处理：知识考古的价值

TeamLeader 倾向选项 A（原位演进）。

我同意旧文档质量不错，但想提一个*知识考古*的视角：

旧文档中包含了大量**隐性设计决策**（implicit decisions）。比如 `Address64` 的定义背后，有"为什么不用 32 位"、"为什么需要 4B 对齐"等未写明的推理。

**风险**：原位演进时，如果只替换类型名而不追溯原始推理，可能会丢失这些隐性知识。

**建议**：在修订 docs/Rbf/*.md 时，同步产出一份 **Migration Notes**（可以放在 artifacts/Plan.md 或单独文件），记录：
- 哪些字段从 Address64 改为 SizedPtr
- 每个修改点的*语义变化*（不只是类型变化）
- 如果保留 Address64，理由是什么

这既是文档，也是未来实现者的*认知地图*。

#### 追加疑问：未被讨论的视角

有一个议题不在 TeamLeader 的列表中，但我认为值得提出：

**SizedPtr 在 RBF 层的"公共类型"定位**

SizedPtr 目前定义在 `Atelia.Data`，是一个通用产品。RBF 是它的"首个目标用户"。

但 RBF 层也有自己的类型（Address64, FrameTag 等）。当 SizedPtr 进入 RBF 接口时，它是：
- **透传**（pass-through）：RBF 接口直接暴露 `SizedPtr`，上层也用 `SizedPtr`
- **包装**（wrap）：RBF 定义自己的类型（如 `RbfSpan`），内部用 `SizedPtr` 实现

这个选择会影响 RBF 的**抽象边界**。如果 RBF 直接暴露 `SizedPtr`，那么 SizedPtr 的任何变化都会穿透到 RBF 用户；如果包装，则 RBF 可以吸收变化。

**这可能需要在 Shape-Tier 讨论**。

---

**小结**：

| 议题 | 我的倾向 | 核心考量 |
|:-----|:---------|:---------|
| 产物形式 | C（混合），但需明确语义边界 | 概念层重构需要清晰的边界定义 |
| Scope | 聚焦 RBF，但邀请 StateJournal 视角 | 下游约束反向影响设计 |
| Wish 定义 | 分阶段（设计优先，实现可选） | 保持聚焦同时留出灵活性 |
| 旧文档 | 原位演进 + Migration Notes | 保护隐性知识 |

以上是我的探索视角，供讨论。

---

### Curator 发言

我从"整合与连贯性"视角来审视这四个议题。我的核心关注是：**这次修订如何融入现有知识体系，而不是创造孤岛？**

#### 1. 产物形式：文档拓扑的心智负担

认同选项 C（混合模式），但想补充一个**发现性（discoverability）**的考量。

当前知识分布已经形成了一个隐性拓扑：
- `atelia/docs/Rbf/` —— RBF 的"权威位置"
- `wish/W-XXXX/artifacts/` —— 各 Wish 的"设计决策"
- `agent-team/wiki/` —— 团队共享知识

如果 artifacts/ 承载设计决策、docs/Rbf/ 承载规范，未来的读者（无论是人类还是 Agent）需要**跨两个位置**才能理解完整画面。这是一种认知税。

**建议**：在 `docs/Rbf/` 中增加一个**导航锚点**，比如在 `rbf-interface.md` 头部添加：
```markdown
> **设计演进**：本文档的设计决策记录在 [W-0006 artifacts](../../../wish/W-0006-rbf-sizedptr/artifacts/)
```

这样保持"规范在原位"的同时，提供"为什么"的入口。符合我的 [D1. 文档分层隐喻](agent-team/members/Curator/index.md#D1)：正文只写 What/How，Why 有明确路标。

#### 2. Scope 边界：依赖链的"接口合同"视角

认同 TeamLeader 的"聚焦 RBF"，也认同 Seeker 提出的"邀请 StateJournal 视角"。

我想从**接口合同（interface contract）**的角度强化这个观点：

W-0006 修订 RBF 设计稿，本质上是在**重新签订 RBF 与上层的合同**。StateJournal 是目前唯一已知的"签约方"。

**风险**：如果 RBF 单方面修改合同条款，而 StateJournal 没有参与审阅，可能产生"合同不可履行"的情况——比如 SizedPtr 的 26-bit Length 限制（~256MB）是否足够 StateJournal 的 blob 场景。

**建议**：在 Shape-Tier 产物中增加一节 **"Interface Contract with StateJournal"**，列出：
- RBF 对 StateJournal 承诺的能力边界
- StateJournal 对 RBF 的已知假设
- 哪些假设被 SizedPtr 引入所影响

这不是扩大 Scope，而是明确**边界约束**。Seeker 说的"邀请视角"可以落地为这个具体产物。

#### 3. Wish 定义：设计文档的"可验收性"

Seeker 提出的"纯设计的验收标准是什么"是一个好问题。

从我的 [D2. 时间预算模型](agent-team/members/Curator/index.md#D2) 视角看，设计文档的价值在于：
- **30秒可判性**：读者能否快速判断"这个设计与我相关吗"
- **5分钟可用性**：读者能否快速上手"我该怎么用这个设计"

**建议验收标准**：
1. 修订后的 `rbf-interface.md` 应在前 30 行内回答："SizedPtr 替换了什么，保留了什么"
2. 每个被 SizedPtr 替换的字段/接口，应有一句话说明"语义变化"（不只是类型变化）
3. 新增的 Decision Log 应支持"为什么不用 Address64"这个问题的快速定位

这些标准不依赖代码实现，可在文档层验收。

#### 4. 旧文档处理：**术语连贯性**是最大风险

强烈支持 TeamLeader 的选项 A（原位演进）和 Seeker 的 Migration Notes 建议。

我要补充一个经常被忽视的风险：**术语漂移（terminology drift）**。

RBF 旧文档中的术语（Address64, Frame, Magic, Tag 等）形成了一套**内部一致的命名系统**。SizedPtr 是外来概念，它的术语（Offset, Length, Span）来自不同的语境。

当两套术语在同一文档中共存时，读者需要持续进行"心智翻译"：
- "这里的 Offset 是 Address64 的 offset，还是 SizedPtr 的 Offset？"
- "Span 在 RBF 语境下是什么？"

**建议**：在 Rule-Tier 产物中增加一节 **"Glossary Alignment"**，明确：

| 术语 | 来源 | 在 RBF 中的含义 |
|:-----|:-----|:----------------|
| `Address64` | RBF 原有 | 指向 Frame 起点的 8 字节 file offset |
| `SizedPtr` | Atelia.Data | 8 字节紧凑表示的 offset+length 区间 |
| `Offset` | SizedPtr 字段 | 38-bit 4B 对齐偏移（约 ~1TB 范围）|
| `Length` | SizedPtr 字段 | 26-bit 4B 对齐长度（约 ~256MB 范围）|

这个 Glossary 不只是参考，而是**术语合同**——确保跨文档使用一致。

#### 追加建议：增量验证的"回读测试"

我想提出一个 TeamLeader 和 Seeker 没有覆盖的验证机制：

**回读测试（Read-back Test）**：修订完成后，邀请一个"没有参与设计过程"的 Agent（比如 QA 或新唤醒的 Implementer）尝试阅读修订后的文档，回答以下问题：
1. SizedPtr 在 RBF 中的角色是什么？
2. 哪些场景用 Address64，哪些用 SizedPtr？
3. 如果要实现 RBF，从哪里开始？

如果回答偏离设计意图，说明文档的"可理解性"有问题。这比自我审阅更能发现盲区。

这是我在 [K1. 三阶段认知流程框架](agent-team/members/Curator/index.md#K1) 中提到的"发现→判断→使用"验证。

---

**小结**：

| 议题 | 我的倾向 | 核心考量 |
|:-----|:---------|:---------|
| 产物形式 | C（混合）+ 导航锚点 | 减少跨位置的认知税 |
| Scope | 聚焦 RBF + Interface Contract 节 | 边界约束显式化 |
| Wish 定义 | 设计优先 + 文档层验收标准 | 30秒可判性 |
| 旧文档 | 原位演进 + Glossary Alignment | 防止术语漂移 |

附加建议：完成后进行"回读测试"验证可理解性。

以上是我的整合与连贯性视角，供讨论。

---

### TeamLeader 总结与决策

感谢三位顾问的深入分析！让我总结共识并形成决策：

#### 🟢 已达成共识

| 议题 | 决策 | 关键输入 |
|:-----|:-----|:---------|
| **产物形式** | **选项 C（混合模式）** + 增强 | • 所有人支持混合模式<br>• Curator 建议导航锚点<br>• Seeker 强调语义边界 |
| **Scope 边界** | **聚焦 RBF** + StateJournal 约束视角 | • TeamLeader 初始倾向<br>• Seeker 的"邀请视角"<br>• Curator 的"Interface Contract" |
| **旧文档处理** | **原位演进** + Migration Notes + Glossary | • TeamLeader 初始倾向<br>• Seeker 的隐性知识保护<br>• Curator 的术语漂移预警 |

#### 📋 具体实施方案

**1. 产物结构**（混合模式增强版）

```
wish/W-0006-rbf-sizedptr/
  artifacts/
    Resolve.md          # 动机、为什么引入 SizedPtr
    Shape.md            # 概念边界、术语对齐（Glossary Alignment）
                        # 增加："Interface Contract with StateJournal"
    Rule.md             # 语义边界（哪里用 Address64、哪里用 SizedPtr）
    Plan.md             # 修订计划 + Migration Notes
    Craft.md            # Phase 2 实现记录（如果执行）

atelia/docs/Rbf/
  rbf-interface.md      # 详细规范，头部增加导航锚点指向 artifacts/
  rbf-format.md         # 详细格式，同上
  rbf-test-vectors.md   # 保持不变（暂不修订）
```

**2. 关键产物清单**

| 产物 | 位置 | 目的 | 提出者 |
|:-----|:-----|:-----|:-------|
| **Glossary Alignment** | Shape.md | 防止术语漂移 | Curator |
| **Interface Contract** | Shape.md | 明确 StateJournal 约束 | Seeker |
| **语义边界规则** | Rule.md | Address64 vs SizedPtr 判断依据 | Seeker |
| **Migration Notes** | Plan.md | 记录每个修改点的语义变化 | Seeker |
| **导航锚点** | rbf-interface.md 头部 | 链接到 artifacts/ | Curator |

**3. Wish 定义修订**

修订为：
> **Phase 1**（必须）：参照旧版文档，用 Artifact-Tiers 格式重新设计 RBF，引入 SizedPtr 替代部分 Address64 使用，明确语义边界。
> 
> **Phase 2**（可选）：实施 RBF 代码实现。

**4. Scope 明确**

✅ **包含**：
- RBF 核心设计（Frame, Tag, Address64, SizedPtr）
- SizedPtr 在 RBF 层的语义边界定义
- StateJournal 作为"目标用户"的约束视角（不修订 StateJournal 文档本身）

❌ **不包含**：
- StateJournal 自身的设计修订（留给未来 Wish）
- RBF 的完整代码实现（Phase 2 可选）

**5. 验收标准（细化）**

| 标准 | 来源 | 验证方式 |
|:-----|:-----|:---------|
| 30秒可判性 | Curator | 前 30 行内回答"SizedPtr 替换了什么" |
| Migration Notes | Seeker | 每个修改点有语义变化说明 |
| Glossary Alignment | Curator | Shape-Tier 包含术语对照表 |
| 回读测试 | Curator | 邀请 QA 或新 Agent 阅读并回答核心问题 |

#### 🎯 建议的推进路径

1. **立即**：更新 wish.md，明确 Phase 1/2 定义和 Scope
2. **Resolve-Tier**：明确动机和 Non-Goals
3. **Shape-Tier**：产出 Glossary Alignment + Interface Contract with StateJournal
4. **Rule-Tier**：定义语义边界规则（Address64 vs SizedPtr 使用场景）
5. **Plan-Tier**：制定修订计划 + Migration Notes
6. **执行修订**：更新 atelia/docs/Rbf/*.md，增加导航锚点

---

**会议状态**：✅ 决策完成，等待监护人确认

