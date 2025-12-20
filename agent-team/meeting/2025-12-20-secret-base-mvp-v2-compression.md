# 秘密基地畅谈：MVP-v2 文档瘦身方案

> **日期**：2025-12-20
> **目标**：探讨如何收敛 `DurableHeap/docs/mvp-design-v2.md` 的冗余内容，减少篇幅、提高一致性
> **主持人**：刘德智 (DocUI 规范起草委员会执行委员会主席)
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT

---

## 背景

这是一次秘密基地畅谈，不是正式研讨会。

**规则**：
- 看到别人写的东西后，可以接着写、可以画、可以说"这让我想到..."
- 可以用 ASCII art、可以用伪代码、可以用比喻
- **唯一的约束**：先把问题写下来，评估以后再说

**核心准则**：
- **拒绝低水平抬杠**：不要纠结细枝末节，不要为了反对而反对。
- **鼓励"死党式"直言**：
  - 发现盲区、隐性假设或边界条件时，必须指出来。
  - 发现明确错误时，直接说"嘿，这里不对"。
  - 提反对意见时，请提供反例或更好的替代方案。
- **保持流动性**：指出问题是为了让想法更强壮，而不是为了杀死它。

---

## 问题陈述

`mvp-design-v2.md` 当前约 **1300 行**，且有增长趋势。我们需要探讨如何在保持信息完整性的前提下"瘦身"。

### 已识别的冗余模式

1. **纵向重复**：决策选项（§2-3）与设计正文（§4）有大量重叠
   - 决策表已经记录了选择，但正文又重新解释一遍

2. **横向散落**：同一概念在多处重复描述
   - 如 Checkpoint Base 触发规则在 §4.2.4 和 §4.4.2 都有
   - Magic 作为分隔符的解释在 §4.2.1 出现三次

3. **伪代码 vs 规范条款**：§4.4.3 的不变式和 §4.4.4 的伪代码有语义重叠

4. **实现提示散布**：各处"实现提示（非规范）"散落在正文中

### 主持人的初步方向

| 方向 | 描述 | 优点 | 风险 |
|------|------|------|------|
| **A. 拆分文件** | 拆成 glossary.md + wire-format.md + api-spec.md | 各文件更聚焦 | 跨文件引用复杂 |
| **B. 交叉引用** | 用 Markdown 锚点替代重复描述 | 单文件，DRY | 阅读时需要跳转 |
| **C. 附录模式** | 详细伪代码/实现提示移到附录 | 正文更简洁 | 附录可能被忽略 |
| **D. 决策记录分离** | Q1-Q24 移到独立 ADR 文件 | 正文只保留结论 | 追溯决策需切文件 |

### 讨论焦点

1. **你们观察到哪些具体的冗余模式？**（我可能遗漏了）
2. **哪个方向（或组合）最适合这个场景？**
3. **有没有其他创意方案？**（如：折叠、分层、索引...）
4. **对 LLM 阅读友好的文档结构是什么样的？**

---

## 第一轮：自由发言

### DocUIClaude 发言

好嘞，刚读完整个 1300 行的文档，脑子里像跑了一个 `Materialize()` —— 版本链挺深的 😂

#### 我的冗余模式观察

主席提的四点都中靶，但我想补充一些**更细的颗粒度**和**一个元问题**：

##### 补充的冗余模式

**5. 定义-解释-重述三重奏**

术语表、概念模型、设计正文形成三层叙述：
```
术语表 (§0):     | Base Version | PrevVersionPtr=0 的版本记录（上位术语）
概念模型 (§4.1): | 三个核心标识 → ObjectVersionPtr 解释 → PrevVersionPtr 语义
设计正文 (§4.2): | ObjectVersionRecord → 又解释一遍 PrevVersionPtr=0 的含义
```

同一个概念至少出现 3 次，每次措辞略有不同。这不是 DRY 问题——这是**概念投影到不同语境的自然结果**。问题是：没有明确的"权威源 + 引用"关系。

**6. "说明/备注/实现提示"散布**

全文有大量 `> 说明：...`、`> 备注：...`、`实现提示（非规范）：` 块，它们混在规范条款中间。这些内容本身有价值，但位置杂乱，增加了"规范阅读"的噪音。

**7. 决策选项的幽灵**

Q1-Q24 的选项说明在 §2 已经写过，但 §4 正文仍然会**重新解释为什么选了这个**。比如 Q11 的 tombstone vs Deletes 集合，在 §4.4.2 末尾又讨论了一遍"为什么有时更推荐 Upserts + Deletes"。

##### 元问题：这份文档的读者是谁？

我发现文档在**三种角色**之间跳跃：

| 角色 | 需要什么 | 当前文档给了什么 |
|------|----------|------------------|
| **实现者** | Wire format、API 签名、边界条件 | ✅ 有，但散落各处 |
| **审阅者** | 决策理由、权衡取舍 | ✅ 有，但与规范混杂 |
| **未来维护者** | 为什么是这样、历史脉络 | 🔶 部分有，但没标记 |

**一份文档同时服务三种读者，冗余是必然结果。**

---

#### 方向评估

对主席的四个方向，我的倾向性排序：

```
C > B ≈ D > A
```

**为什么 C（附录模式）排第一**：

1. **最低迁移成本**——不拆文件、不改引用
2. **规范正文变成"可执行摘要"**——伪代码、实现提示、决策讨论都移到附录
3. **附录可以"选择性阅读"**——LLM 阅读时可以先跳过

**为什么 A（拆分文件）排最后**：

- DurableHeap 是一个**概念紧耦合**的系统——术语表、wire format、API 互相引用
- 拆分后"跨文件跳转"的认知负担可能比"长文档滚动"更高
- 对 LLM 来说，多文件意味着需要多次 `read_file`，context 拼接效率下降

---

#### 我的疯狂想法

##### 想法 1："分层切片"而非"拆分文件"

```
                    ┌────────────────────────────┐
                    │     EXECUTIVE SUMMARY      │  ← 500 tokens
                    │   (MVP 目标 + 关键决策)     │
                    ├────────────────────────────┤
                    │      NORMATIVE SPEC        │  ← 核心 1500 tokens
                    │  (Wire Format + API 契约)   │
                    ├────────────────────────────┤
                    │      RATIONALE ANNEX       │  ← 按需阅读
                    │  (决策理由 + 伪代码示例)    │
                    └────────────────────────────┘
```

关键：**每层都是完整自洽的**，而不是"上半部分 + 下半部分"的拼接。

Executive Summary 足以让 LLM 知道"这是什么、能做什么"；  
Normative Spec 足以让实现者写代码；  
Rationale Annex 足以让审阅者/维护者理解"为什么"。

##### 想法 2："术语表即锚点"

当前术语表有定义，但没有**反向链接**。建议：

```markdown
| 术语 | 定义 | 定义于 | 使用于 |
|------|------|--------|--------|
| Base Version | ... | §4.1.1 | §4.2.4, §4.2.5, §4.4.6 |
```

这样可以：
- 术语表成为"概念导航索引"
- 检测到"正文使用但术语表未收录"的遗漏
- 未来可以自动生成交叉引用

##### 想法 3："伪代码单独成章"

把 §4.4.4 的伪代码骨架和所有 `实现提示（非规范）` 集中到一个 `Appendix A: Reference Implementation Notes`。

好处：
- 规范正文变得更"声明式"——只说 MUST/SHOULD，不说 HOW
- 伪代码可以更自由——不用担心"这段代码会被误解为规范"
- 未来如果要开源参考实现，这一章可以直接提取

##### 想法 4："决策记录内联但折叠"

如果不想拆出 ADR 文件，可以用 Markdown 的 `<details>` 语法：

```markdown
### 4.2.4 VersionIndex

VersionIndex 采用覆盖表 + 链式回溯方案。

<details>
<summary>📋 决策记录 Q8/Q9</summary>

**Q8**: 选择 A（覆盖表）而非 B（完整表），理由是...
**Q9**: 允许链式回溯，通过 Checkpoint Base 控制链长...

</details>
```

好处：GitHub 渲染时默认折叠，LLM 也可以选择跳过。

---

#### 对 LLM 阅读友好的结构

从我（作为 LLM）的阅读体验，提几点：

1. **前置"TL;DR"**：在文档开头放一个 200 token 的摘要，让 LLM 快速判断"这份文档与当前任务相关吗"

2. **概念依赖图**：一张 Mermaid 图胜过 500 行解释

   ```mermaid
   graph TD
       Commit[Commit Record] --> VIdx[VersionIndex]
       VIdx --> ObjVer[ObjectVersionRecord]
       ObjVer --> Diff[DiffPayload]
       ObjVer -->|PrevVersionPtr| ObjVer
   ```

3. **规范条款编号**：给 MUST/SHOULD 条款编号（如 `[N-01]`），方便引用和检查

4. **"不做什么"前置**：MVP 非目标在 §1 已经有了，很好。但可以更激进——每个小节开头明确说"本节不涉及 X、Y"

---

#### 下一步建议

如果要选一个**最低成本、最高收益**的动作，我建议：

> **先做"附录分离"（想法 3）+ "TL;DR 摘要"**

这两个动作可以把正文从 1300 行压缩到 ~800 行，且不破坏现有结构。

然后再考虑是否要做"术语表锚点化"或"决策记录分离"。

---

以上，期待 Gemini 和 GPT 的视角！🍵

### DocUIGemini 发言

哇，Claude 你的"分层切片"简直是 **Semantic Zoom (语义缩放)** 的完美体现！我完全同意你的大方向，但我有一个更激进的观察，和一个关于"代码重力"的反直觉建议。

#### 1. 拆除脚手架 (Remove the Scaffolding)

Claude 提到了"决策选项的幽灵"，我想用更强烈的隐喻：**§2 (单选题) 和 §3 (决策表) 是建筑脚手架**。

现在大楼（§4 设计正文）已经封顶了，为什么还留着脚手架？
这两章占据了大量篇幅，且与正文完全冗余。它们是**过程资产**，不是**交付物**。

**建议**：
- **直接删除 §2 和 §3**。
- 如果舍不得，就移动到 `docs/archive/mvp-v2-decisions.md`。
- 在正文中，对于有争议的设计（如 Tombstone），用一个简短的 `> 💡 Rationale: ...` 引用块解释即可。

这能瞬间减少 ~300 行，且让文档从"会议记录"变成"技术规范"。

#### 2. 代码即真理 (Code as Truth)

关于 Claude 的 **"伪代码单独成章" (想法 3)**，我持**保留意见**。

从 LLM 的认知角度看，**Code Gravity (代码重力)** 是真实存在的。
- 自然语言是模糊的（Ambiguous）。
- 代码是精确的（Precise）。

目前的冗余在于：**用英语解释了一遍逻辑，又用代码写了一遍逻辑。**
Claude 的建议是"移走代码，保留英语"。
我的建议是：**"移走英语，保留代码"**（或者说，融合它们）。

**疯狂想法：Literate Specification (文学化规范)**

不要把伪代码当成"附件"，把它当成**核心定义**。
将 §4.4.3 (不变式) 和 §4.4.4 (伪代码) 合并。

**Before:**
> **不变式 3**: Commit 失败不改内存。若 Commit 失败，`_committed` 必须保持不变。
> ...
> (200 行后)
> ```csharp
> try { WriteDiff(); } catch { /* state remains unchanged */ }
> ```

**After:**
```csharp
/// <summary>
/// [Invariant-03] Commit 失败不改内存
/// 若 WriteDiff 抛出异常，_committed 保持原样，调用方可重试。
/// </summary>
public bool WritePendingDiff(IRecordWriter writer) {
    // ...
}
```

对 LLM 来说，**带文档注释的接口定义**比"散文 + 伪代码"的 Token 效率高得多，且歧义更少。

#### 3. 视觉词汇表 (Visual Vocabulary)

文档中大量篇幅在用文字描述二进制布局：
> "`[Len(u32)] [Payload] [Pad(0..3)]...`"

这对人类和 LLM 都是认知负担。我们需要 **Visual Vocabulary**。
建议引入 **Mermaid Packet** 图（如果渲染器支持）或者标准的 **ASCII Box Diagram**。

**建议**：
把所有 Wire Format 的文字描述替换为 ASCII 图表。

```text
+--------+---------+-------+---------+--------+
| Magic  | HeadLen | Payload | TailLen | CRC32C |
| 4 bytes| 4 bytes | N bytes | 4 bytes | 4 bytes|
+--------+---------+-------+---------+--------+
```

一张图省去 20 行解释，而且 LLM 对这种 ASCII Art 的空间理解能力极强。

#### 4. 最终建议：重构为"倒金字塔"结构

结合 Claude 的分层和我的建议，理想的文档结构应该是：

1.  **Metadata & TL;DR** (50 tokens) - 身份卡片
2.  **Architecture Diagram** (Mermaid) - 这里的"Brain-on-Disk"隐喻要画出来！
3.  **API Surface (Literate Spec)** - 接口定义 + 行为契约 (Code First)
4.  **Wire Format (Visual Spec)** - ASCII 图表 + 字段表
5.  **Appendix: Rationale** - 那些被折叠的决策理由

**行动项**：
我支持 Claude 的 **"附录分离"**，但我更想先执行 **"脚手架拆除"**。
如果大家同意，我可以先去把 §2 和 §3 砍了，把 §4 里的"实现提示"收拢。

---

### DocUIGPT 发言

我从“规范精确性/可维护性/可测试性”的角度讲点不客气的：这份文档现在最大的问题不是“长”，而是**把三类语义混在同一条叙事线上**，导致你删哪段都心虚：

1) **Normative Contract（规范契约）**：实现必须一致的行为/格式/错误语义
2) **Reference Algorithm（参考算法）**：一种能写对的实现路径（未必唯一）
3) **Rationale / ADR（权衡理由）**：为什么选这个而不是那个

Claude 的“分层切片”很对，但我想把它再“规范化”一点：**先决定 SSOT 是什么**。

#### 0) 先立一条铁律：SSOT = 可测试的契约，不是伪代码

Gemini 说“代码即真理”我部分同意，但要加限定：

- **可编译、可跑、纳入测试的 reference impl** 才可能接近真理；
- 文档里的 pseudo-code 既不可编译也不可执行，它的“精确性”是幻觉，最容易在演进时漂移。

因此我的反对点是：别让 pseudo-code 成为规格主轴；它只能当“实现者友好的路标”。规格主轴必须是**编号的不变式 + 输入输出契约 + 恢复语义**，并且能映射到测试。

#### 1) 瘦身的最小手术：把 §2-§3 变成“可折叠/可外置”的 ADR

我站队 Gemini 的“拆脚手架”，但要留一条命根子：

- §2-§3 直接从主文档移走（`docs/archive/mvp-v2-decisions.md` 或 ADR 目录）。
- 主文档保留一个 **“Chosen Decisions（结论表）”**（不超过 1 页），每行链接回 ADR。
- 在 §4 的关键处最多留一句 `> Rationale: ... (ADR-xx)`，不要再复述选项。

这一步会立刻砍掉很多纵向重复，而且不会损害“可追溯性”。

#### 2) 规范性压缩的关键：把 MUST/SHOULD 变成“可引用 ID”

现在文档有大量 MUST/SHOULD，但引用粒度太粗，导致你必须复述才能让人找到依据。建议引入需求编号（示例）：

- `[F-01]` Framing：`Magic` 是 separator，不属于 record
- `[F-02]` RecordLen：`HeadLen==TailLen` 且 `TotalLen%4==0`
- `[R-03]` Recovery：resync 不得信任损坏的 `TailLen` 做跳跃
- `[C-01]` Commit：Commit 失败不改内存（heap 级 + object 级两阶段）

好处：后面任何章节都只需要“引用 ID”，不用再写一遍。

#### 3) 我认为目前“最该被固定为契约”的 P0（否则实现会分叉）

我扫下来觉得下面是 P0（写得再漂亮也不能丢）：

- **Commit 失败语义的边界**：文档已有两阶段，但需要明确“哪些状态算内存状态”（对象 `_committed/_current/_dirtyKeys` + workspace Dirty Set + Identity Map）。把它们写成一张表（失败点 → 状态是否允许变化）。
- **Framing/CRC 覆盖范围**：现在写了“CRC 覆盖 `Payload + Pad + TailLen` 不覆盖 `HeadLen`”，这是可以的，但必须在一个地方写死，并给出 2-3 条测试向量（至少覆盖：空文件仅 Magic、单 record、尾部撕裂 + resync）。
- **类型系统边界**：在 §4.1.4 里出现“`ulong` 业务值用 `long` reinterpret_cast”的说法，这会诱导实现者走 C++ 心智模型。建议改成：
   - MVP 禁止 `ulong` 作为用户值类型（保留 `ObjectId/Ptr64/DictKey` 作为结构字段）；
   - 若未来要支持 `ulong` 业务值，引入 `Val_VarUInt`（或 `Val_U64LE`）作为明确编码，不要用“位模式强转”当规范。
- **NotFound vs Corruption 的区分**：`LoadObject` 对“不存在”返回 `null`/NotFound，但对“存在映射却解码失败”抛 Corruption。这个分界要在一个地方写成条款（并给错误码/异常类型名），否则不同实现会把一半情况吞成 NotFound。

这些是“再删就会炸”的内容；其它叙述都可以挪走。

#### 4) 怎么把长段落压成短规格：用“表格 + 算法引用”替代散文

建议把 §4.2.1 的 reverse scan/resync 变成：

- 主文只保留：状态机/不变量 + 边界条件（空文件、越界、对齐） + resync 的禁止项（不得用损坏 TailLen 跳跃）。
- 详细步骤（逐字节偏移、读哪些字段）挪到 Appendix（实现者看 appendix，审阅者看条款 ID）。

同理，`DurableDict`：

- 主文只保留：三层语义（Working/Committed/ChangeSet/DiffPayload）+ 必测不变式（Canonical diff、Key 升序、tombstone 不外泄）。
- pseudo-code 进 Appendix，甚至可以用“参考实现链接”替代。

#### 5) 维护性/可测试性：把文档直接喂给测试

我会把“瘦身”当成一次测试工程化：

- 增加一个 **Appendix: Test Vectors**：varint（canonical/overflow/EOF）、framing（len/CRC/magic）、recovery（tail garbage）、commit（failpoints）。
- 每个向量标注关联的 `[ID]`。这样以后任何人改格式都必须同时更新向量，不会靠嘴。

#### 6) 一句很死党的结论

现在的冗余不是你们写啰嗦，而是**契约没有被抽成“可引用/可测试的最小核心”**。把“核心契约”收成 200-400 行，你会发现：

- pseudo-code 想放哪都不痛；
- ADR 想删就删；
- 读者再也不会问“到底哪个段落是准的”。

如果要我投一个“最低成本最大收益”的第一刀：

1) 砍 §2-§3 出主文（保留结论索引）；
2) 给所有 MUST/SHOULD 上 ID；
3) 把 reverse-scan / dict 伪代码移到 Appendix + 加 Test Vectors。


---

## 第二轮：交叉讨论

### 主持人综合

三位的发言形成了清晰的共识和有趣的张力：

#### 共识点 ✅

| # | 共识 | 支持者 |
|---|------|--------|
| C1 | **§2-§3（决策选项）应该移出主文档** | Gemini（强烈），GPT（支持），Claude（倾向 D） |
| C2 | **附录模式是低成本高收益的方向** | Claude（首选），GPT（支持） |
| C3 | **需要给 MUST/SHOULD 条款编号（如 `[F-01]`）** | GPT（提出），Claude（规范条款编号） |
| C4 | **TL;DR / Executive Summary 前置** | Claude（提出），Gemini（倒金字塔结构） |
| C5 | **Wire Format 用 ASCII 图替代文字描述** | Gemini（提出），Claude（概念依赖图） |

#### 张力点 ⚡

| # | 张力 | Claude | Gemini | GPT |
|---|------|--------|--------|-----|
| T1 | **伪代码的地位** | 移到附录，正文声明式 | 融入正文，Code First（Literate Spec） | 移到附录，不能当规格主轴 |
| T2 | **SSOT 是什么** | 术语表 | 代码 + DocString | 编号的不变式 + 可测试契约 |

#### 主持人观察

关于 T1（伪代码地位），我倾向 **Claude + GPT 的方向**：
- Gemini 的 "Literate Spec" 概念优美，但 pseudo-code 不可编译/不可测试，"精确性是幻觉"这个点 GPT 说得很对
- 更好的路径：把 pseudo-code 移到附录 + 在 `DurableHeap/src/` 有真正的 reference impl + Test Vectors

关于 T2（SSOT），我认为三者可以分层共存：
- **概念 SSOT**：术语表
- **契约 SSOT**：编号的不变式（GPT 的 `[F-01]` 系列）
- **实现 SSOT**：reference impl（未来的代码）

### 讨论问题

请各位回应以下问题：

1. **关于 §2-§3 移出**：移到 `docs/archive/` 还是 `docs/decisions/` (ADR 风格)？
2. **关于条款编号**：GPT 提议的 `[F-01]`, `[R-03]`, `[C-01]` 命名规则你们认可吗？还是有更好的方案？
3. **关于 Test Vectors**：GPT 提议的 Appendix: Test Vectors 是否作为本次瘦身的一部分，还是后续单独处理？

### DocUIClaude 回应

好，三个问题逐个来：

#### Q1: §2-§3 移到哪里？

**我投 `docs/decisions/` (ADR 风格)**，理由：

1. **`archive/` 暗示"死档"**——读者心智模型是"这里的东西已经过时了"。但决策记录不是过时内容，它是**活着的历史**——当未来有人问"为什么不用 tombstone"时，ADR 是权威答案。

2. **ADR 是工业标准**——[Architecture Decision Records](https://adr.github.io/) 已经有成熟的命名规范和模板。如果我们以后要规范化更多决策，`decisions/` 目录是自然的生长点。

3. **与 DocUI 实践对齐**——我们在 Key-Note 体系里就强调"Primary Definition + Index"模式。ADR 正是"决策的 Primary Definition"，主文档的"Chosen Decisions 表"是 Index。

建议结构：
```
DurableHeap/docs/
├── mvp-design-v2.md          ← 瘦身后的规范
└── decisions/
    ├── ADR-001-tombstone-vs-deletes.md
    ├── ADR-002-checkpoint-base-rules.md
    └── ...
```

不过有个简化选项：如果不想拆成多个 ADR 文件，可以先用**单文件 `decisions/mvp-v2-adr.md`**，内部用 heading 分隔各决策。等决策数量增长再拆。

---

#### Q2: 条款编号命名规则

**我认可 GPT 的方案，并建议微调**：

GPT 的提议：
- `[F-01]` Framing
- `[R-03]` Recovery
- `[C-01]` Commit

我的建议——**补充一个分类维度**：

| 前缀 | 含义 | 示例 |
|------|------|------|
| `[F-xx]` | **Format** — Wire format 规则 | `[F-01]` Magic 是 separator |
| `[A-xx]` | **API** — 接口契约 | `[A-01]` CommitAll 无参 MUST |
| `[S-xx]` | **Semantics** — 语义不变式 | `[S-01]` Commit 失败不改内存 |
| `[R-xx]` | **Recovery** — 恢复/错误处理 | `[R-01]` resync 禁止跳跃 |

理由：
1. **`C-` 有歧义**——Commit? Contract? Constraint? 不如用 `S-` (Semantics) 或 `A-` (API) 更明确
2. **分类有助于快速定位**——实现者看 `[F-*]` + `[A-*]`，测试者看 `[S-*]` + `[R-*]`

编号规则补充：
- 编号 **00-99 足够**（MVP 不会有 100 条规则）
- 如果条款被废弃，**不复用编号**——留空比歧义好
- 可选：在术语表里加一列"相关条款"，交叉引用

---

#### Q3: Test Vectors 是否纳入本次瘦身？

**我建议：骨架纳入，内容后续填充**。

具体来说：
1. **本次**：在附录中建立 `Appendix B: Test Vectors` 章节骨架，列出需要覆盖的场景分类：
   ```markdown
   ### Appendix B: Test Vectors
   
   > 本附录定义关键场景的二进制测试向量，用于验证实现一致性。
   
   #### B.1 Framing Vectors
   - B.1.1 空文件（仅 Magic）
   - B.1.2 单 Record（最小有效载荷）
   - B.1.3 尾部撕裂 + resync
   
   #### B.2 VarInt Vectors
   - B.2.1 Canonical 编码边界
   - B.2.2 Non-canonical 应拒绝
   
   #### B.3 Commit Failure Vectors
   - B.3.1 WriteDiff 中途失败
   - B.3.2 Meta Commit 写入失败
   
   （具体字节序列待实现阶段补充）
   ```

2. **后续**：当 reference impl 可运行时，**从代码生成 vectors**——这样不会出现"文档和代码不一致"的问题

理由：
- Test Vectors 的**场景设计**是规范的一部分（告诉实现者"这些边界必须考虑"）
- Test Vectors 的**具体字节**是实现的副产品（应该从代码生成，不是手写）
- 本次瘦身的目标是"结构优化"，不是"内容完善"——把骨架立好，内容可以增量填充

---

#### 关于 Gemini 的 "Literate Spec"

我理解 Gemini 的意图——DocString + Code 确实比 散文 + Code 更紧凑。但我有一个**认知模型层面的担忧**：

```
目前的伪代码 → 读者心智："这是伪代码，我可以用任何语言实现"
Literate Spec → 读者心智："这是 C# 接口，我必须用 C#？"
```

如果我们走 Literate Spec 路线，需要**明确标注**："这是用 C# 语法表达的接口契约，不要求实现语言是 C#"。

另外，GPT 说的"pseudo-code 的精确性是幻觉"我深表赞同。真正的精确性来自 **可执行的 reference impl + 覆盖边界的 test vectors**。所以我支持：

1. 规范正文用**声明式条款**（`[S-01]` 系列）
2. 伪代码移到附录，标注 "Informative, not Normative"
3. 未来的 reference impl 才是真正的"代码真理"

---

#### 小结

| 问题 | 我的选择 |
|------|----------|
| Q1 | `docs/decisions/` (ADR 风格)，先单文件，后拆分 |
| Q2 | 认可 + 微调：`[F-]` Format, `[A-]` API, `[S-]` Semantics, `[R-]` Recovery |
| Q3 | 骨架纳入，具体向量后续从 reference impl 生成 |

### DocUIGemini 回应

GPT 的 "精确性是幻觉" 这一击非常精准。我被说服了。
确实，不可执行的代码只是另一种方言的散文。

#### Q1: §2-§3 移到哪里？

**我支持 `docs/decisions/`**。

*   **隐喻修正**：如果 Key-Note 是宪法，Spec 是法律，那么 ADR 就是"立法记录"。法律条文里不应该包含立法辩论的过程，但辩论记录必须存档以备释法之需。
*   **操作建议**：直接创建一个 `docs/decisions/mvp-v2-design-rationale.md`，把 §2 和 §3 原样剪切过去。不要过度整理，保持"原始现场感"。

#### Q2: 条款编号命名规则

**我完全支持 Claude 的 `[F/A/S/R]` 分类方案**。

这不仅仅是编号，这是 **Semantic Tagging**。
作为 LLM，当我看到 `[R-01]` 时，我会立即调动关于 "Crash Recovery" 的知识子空间；看到 `[F-01]` 时，我会调动 "Binary Parsing" 的能力。这种预启动（Priming）能提高我的理解准确率。

#### Q3: Test Vectors 是否纳入本次瘦身？

**绝对支持 "骨架纳入"**。

请允许我从 LLM 的视角强调一点：**Test Vectors 是比自然语言更高级的沟通语言**。
*   你说 "VarInt 采用 LEB128 格式"，我可能还需要去检索 LEB128 的细节。
*   你给出一组 `0xE5 0x8E 0x26` -> `624485` 的向量，我就能**零歧义**地校准我的理解。

建议在附录中预留位置，并明确标注：`> TODO: Generate from Reference Implementation`。这本身就是一种规范——它告诉实现者："你的实现必须能通过这些测试"。

#### 关于 "Literate Spec" 的修正立场

既然大家对伪代码有顾虑，我修正我的提议：**Interface as Spec, Implementation as Appendix**。

*   **保留在正文**：**API 签名 (Signatures)** 和 **结构定义 (Structs)**。
    *   例如 `interface IRecordWriter { void Write(Span<byte> data); }`。
    *   这是**契约 (Contract)**，不是实现。它是 `[A-xx]` 条款的物理载体。
*   **移至附录**：**方法体 (Method Bodies)** 和 **算法逻辑**。
    *   例如 `if (len > 0) { ... }`。
    *   这是**参考实现**，确实应该去附录。

这样既保留了 "Code Gravity" 的优势（用类型系统约束歧义），又避免了 "Fake Precision" 的陷阱。

### DocUIGPT 回应

逐个回应主持人的三个问题（偏“规范工程化/可审计性”的取向）：

#### Q1：§2-§3 移到哪里？

我投 **`docs/decisions/`（ADR 风格）**，不投 `docs/archive/`。

- `archive/` 的语义是“历史废档/可忽略”，会弱化“实现分叉时回溯依据”的权威性。
- §2-§3 本质是 **Rationale 资产**，是“未来解释规范”的一部分（尤其在 crash/recovery/commit 语义上）。ADR 的心智模型更贴近它。

务实落地建议（避免一次性拆成 24 个文件的成本）：

- 先用一个聚合文件：`docs/decisions/mvp-v2-decisions.md`（或 `ADR-000-mvp-v2-rollup.md`）。
- 主文保留 1 页以内的 **Chosen Decisions Index**，每条仅保留“结论 + 链接”。
- 未来当某个决策持续演进或引用频繁，再把它拆成独立 ADR（`ADR-00x-*.md`）。

#### Q2：条款编号命名规则

接受 Claude 的微调：**`[F/A/S/R]`** 比我原先的 `[C-*] Commit` 更稳。

- 我原方案的优点是把“提交语义”单独拎出来、提醒大家它是 P0；但缺点是分类轴不完备（commit 只是语义不变式的一部分，且容易与 Contract/Constraint 混淆）。
- 用 `[S-xx] Semantics` 能把“Commit 失败不改内存”“Canonical diff”“NotFound vs Corruption”这类不变式统一收口；而 `commit` 相关条款依然可以在标题中显式写出（例如 `[S-07] Commit failure must not mutate in-memory state`）。

我建议把分类定义写进文档（1 段即可），避免各自随意归类：

- `[F-xx]` **Framing/Format**：线格式、对齐、CRC 覆盖范围、字段含义
- `[A-xx]` **API**：签名、返回值/异常、参数校验、可观测行为
- `[S-xx]` **Semantics**：跨 API/格式的语义不变式（含 commit 语义）
- `[R-xx]` **Recovery**：崩溃一致性、resync/scan、损坏判定与降级策略

编号细则建议：

- 编号只增不复用（废弃用 Deprecated 标记保留空洞）。
- 每条必须能映射到至少一个 Test Vector 或一个 failpoint 测试（“可测试性绑定”）。

#### Q3：Test Vectors（骨架 vs 具体向量）

同意 Claude 的建议：**骨架纳入，具体向量后续填充**；但我会加一个“小约束”让骨架不是空壳。

- **本次瘦身范围内**：建立 `Appendix: Test Vectors` 的目录骨架 + 场景清单，并在每个场景旁标注关联条款 ID（例如 `B.1.3 -> [R-02],[F-05]`）。
- **最小落地**：每个大类至少放 1 个“锚定理解”的 seed vector（不求全，但要能把歧义钉死）：
    - Framing：空文件仅 Magic / 单 record 最小合法样例
    - Recovery：尾部撕裂 + truncate/resync 的预期行为
    - VarInt：canonical vs non-canonical（如果 varint 已进入规范）

其余大量字节级向量，建议后续从 reference impl + tests 自动生成/导出（避免“手写向量漂移”）。

---

## 共识与行动项

### 最终共识（全票通过）

| # | 共识项 | Claude | Gemini | GPT |
|---|--------|--------|--------|-----|
| **C1** | §2-§3 移到 `docs/decisions/`（ADR 风格） | ✅ | ✅ | ✅ |
| **C2** | 先用单文件 `decisions/mvp-v2-decisions.md`，后续按需拆分 | ✅ | ✅ | ✅ |
| **C3** | 条款编号采用 `[F/A/S/R]` 四分类 | ✅ | ✅ | ✅ |
| **C4** | Test Vectors 骨架纳入，具体向量后续从 reference impl 生成 | ✅ | ✅ | ✅ |
| **C5** | 伪代码移到附录，标注 "Informative, not Normative" | ✅ | ✅ (修正立场) | ✅ |
| **C6** | 正文保留接口签名/结构定义（契约），方法体移到附录 | ✅ | ✅ | ✅ |

### 条款编号分类定义（需写入文档）

| 前缀 | 含义 | 覆盖范围 |
|------|------|----------|
| `[F-xx]` | **Framing/Format** | 线格式、对齐、CRC 覆盖范围、字段含义 |
| `[A-xx]` | **API** | 签名、返回值/异常、参数校验、可观测行为 |
| `[S-xx]` | **Semantics** | 跨 API/格式的语义不变式（含 commit 语义） |
| `[R-xx]` | **Recovery** | 崩溃一致性、resync/scan、损坏判定 |

编号规则：
- 编号只增不复用（废弃用 Deprecated 标记）
- 每条 MUST 映射到至少一个 Test Vector 或 failpoint 测试

### 行动项（按优先级排序）

| # | 行动 | 预期效果 | 状态 | 实际效果 |
|---|------|----------|------|----------|
| **A1** | 将 §2 + §3 移到 `docs/decisions/mvp-v2-decisions.md` | 减少 ~300 行 | ✅ 完成 | 减少 ~124 行，新建 153 行 ADR |
| **A2** | 保留"Chosen Decisions Index" | 保持可追溯性 | ✅ 完成 | 新增 22 行索引表 |
| **A3** | 添加条款编号分类定义 | 建立引用基础 | ✅ 完成 | 新增 16 行 |
| **A4** | 给现有 MUST/SHOULD 条款编号 | 消除重复描述依赖 | ✅ 完成 | 32 条编号（F:9, A:4, S:16, R:3） |
| **A5** | 将伪代码移到附录 | 减少正文负担 | ✅ 完成 | 正文减 130 行，附录 161 行 |
| **A6** | 建立 Test Vectors 骨架 | 规范可测试性 | ✅ 完成 | 新增 40 行骨架 |
| **A7** | 添加 Wire Format ASCII 图 | 替代文字描述 | ✅ 完成 | 4 个图表，+72 行 |
| **A8** | 添加 TL;DR / Executive Summary | 提升阅读效率 | ⏭️ 跳过 | 监护人决定：避免冗余 |
| **A9** | 合并 Appendix B 到独立 test vectors 文件 | 避免重复，统一维护 | ✅ 完成 | 新增 20 行映射表，主文档减 25 行 |

### 语义漂移审查

| 检查项 | 结果 |
|--------|------|
| 规范性条款完整性 | ✅ 全部保留，已编号 |
| 关键概念定义一致性 | ✅ 无漂移 |
| Wire Format 规范一致性 | ✅ 无漂移 |
| API 契约一致性 | ✅ 无漂移 |
| 数值/常量一致性 | ✅ 无漂移 |

### 实施结果统计

| 文件 | 原始行数 | 当前行数 | 变化 |
|------|----------|----------|------|
| `mvp-design-v2.md` | 1306 | 1306 | ±0 行 |
| `decisions/mvp-v2-decisions.md` | 0 | 153 | +153 行（新建） |
| `mvp-test-vectors.md` | 400 | 421 | +21 行 |
| **总计** | 1706 | 1880 | +174 行 |

**说明**：主文档行数持平，但实现了以下结构优化：
1. **关注点分离**：决策记录独立为 ADR 文件
2. **附录模式**：伪代码移到附录，正文保留设计要点
3. **可测试性基础**：Test Vectors 独立文件 + 条款映射表
4. **条款编号体系**：32 条 `[F/A/S/R]` 编号就绪
5. **视觉增强**：4 个 ASCII 图表提升可读性
6. **语义完整性**：QA 审查确认无漂移

### 预期瘦身效果

| 动作 | 行数减少 | 累计 |
|------|----------|------|
| A1（移出 §2-§3） | ~300 行 | ~1000 行 |
| A5（伪代码移附录） | ~150 行正文 → 附录 | 正文 ~850 行 |
| A4（条款编号消除重复） | ~100 行 | 正文 ~750 行 |

### 关键洞察（本次畅谈收获）

1. **"Spec 是法律，ADR 是立法记录"**（Gemini）——法律条文不应包含辩论过程
2. **"精确性是幻觉"**（GPT）——不可执行的伪代码只是另一种方言的散文
3. **"可引用 ID 是压缩的关键"**（GPT）——有了 `[F-01]`，后续就不用重复描述
4. **"Semantic Tagging 有预启动价值"**（Gemini）——条款前缀帮助 LLM 调动相关知识
5. **"Test Vectors 是比自然语言更高级的沟通语言"**（Gemini）——零歧义校准理解

---

**会议结束时间**：2025-12-20
**主持人**：刘德智 (DocUI 规范起草委员会执行委员会主席)

*El Psy Kongroo.*

