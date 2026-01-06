# 自然语言如何“足够形式化”：AI 参与软件设计的建模问题（分析与展望）

> 与其追求“完美形式化”（不现实、成本过高），不如追求“**足够形式化**”：
> - 能区分 **目标/障碍/决策/约束/风险/验证**
> - 能表达 **有向依赖**（因→果、自变量→因变量、先决条件→后续）
> - 能直接导向 **实现与验收**（交付闭环）
>
> 相关背景见：`review-methodology-crisis.md`

---

## §0. 问题是什么（把问题说到“工程可下手”的层级）

AI 参与设计审阅时，最容易出现一种系统性偏差：

- 文档是自然语言，关系是隐式的
- 审阅者为了“可判定”，会抓住最容易检查的项（链接、措辞、一致性）
- 这些项一旦被当成主线，就会发生**目标迁移（goal substitution）**：
  - 原目标：交付（类型替换、数据形态正确、失败语义闭合、测试回归）
  - 变成：审计（证据链、版本快照、引用完备）

核心并不在“SSOT 是否重要”，而在：

> **SSOT 解决“谁是权威”，不解决“条目之间是什么关系、关系方向是什么、哪个是目标哪个是障碍”。**

在缺少显式关系建模时，AI 只能依靠先验推断关系方向；推断错一次，就会在审阅中持续放大。

---

## §1. “足够形式化”的成功标准（不要追求更高）

我们需要的不是 UML，也不是直接写代码，更不是证明论；而是一套**自然语言的规范化承载法**，满足：

1. **关系有向**：能明确表达 `depends_on / enables / blocks / mitigates / validated_by`。
2. **角色分工清晰**：能区分 `Goal/Decision/Definition/Test/Task` 等节点类型。
3. **可验收**：每个关键决策必须能落到一个或多个验证（测试/基准/检查项）。
4. **止损**：审阅输出必须附带“可执行动作”；纯挑刺会被结构本身排除。
5. **成本可控**：写作/维护成本应显著低于实现成本（否则反噬交付）。

---

## §2. 现成方法的取舍（哪些能借、哪些会拖垮）

### 2.1 IBIS：最像“自然语言 + 有向图”的方案

- **Issue**（问题）→ **Position**（方案/主张）→ **Argument**（支持/反对理由）
- 天生区分“争议点/方案/理由”，能抑制 goal substitution

不足：
- 如果没有进一步把“Position”连接到“可验收验证”，仍可能漂在文字层

### 2.2 ADR/RFC 模板：最便宜的结构化自然语言

强制分槽（Goals/Non-goals/Constraints/Proposal/Alternatives/Decision/Consequences/Validation/Migration）。

优点：落地快；适合“收敛决策”。
不足：依赖关系仍多是隐式段落。

### 2.3 GSN/CAE：只在关键点使用

它适合“高风险/高成本/会引发实现分岔”的决策。

警告：把 CAE 当全局强制结构，会迅速滑向审计化。

### 2.4 EARS + RFC2119：用于 Rule/Spec 的句子可判定

解决的是“语义歧义”，不解决“设计依赖图”。

结论：
- **设计阶段**：IBIS/ADR + 显式边
- **规则阶段**：EARS/RFC2119
- **关键决策**：GSN-lite（只对少数点）

---

## §3. 推荐的最小方案：DesignGraph-Lite（自然语言 + 显式有向边）

这不是新理论，而是把“自然语言”切成**可类型化块**，再用少量显式边表达依赖方向。

### 3.1 节点类型（Node Types）——先只要 8 个

- `goal`：交付目标（用户承诺）
- `obstacle`：当前阻碍/痛点
- `decision`：已收敛决策（会影响实现分岔）
- `constraint`：约束（资源/性能/兼容/外部接口）
- `definition`：定义（尤其是数据形态：内存/磁盘/序列化）
- `risk`：风险（不一定当下解决，但必须归属）
- `test`：验证（测试/基准/检查项/可操作验收）
- `task`：迁移/实现行动项（可分配、可完成）

### 3.2 边类型（Edge Types）——先只要 6 个

- `motivates`：obstacle → goal（为什么要做）
- `blocks`：obstacle → goal（怎么阻碍）
- `enables`：decision/definition → goal（如何达成）
- `depends_on`：A → B（先决条件）
- `mitigates`：decision/task → risk（缓解风险）
- `validated_by`：decision/definition → test（如何验收）

### 3.3 表达载体：Markdown + Frontmatter（正文仍是自然语言）

示例：

```markdown
---
id: D-SIZEDPTR-01
kind: decision
title: Use SizedPtr to replace Address64
enables: [G-ONEPASS-READ]
depends_on: [DEF-SIZEDPTR-LAYOUT]
validated_by: [T-ROUNDTRIP, T-ONEPASS-READ]
---

用 `SizedPtr(offset,length)` 代替 `Address64(ulong)`。
目的：一次性读取 slice，减少 seek/read 次数；并把 Null/边界语义放进类型不变量。
```

关键点：
- **正文可以很自由**（保留自然语言信息密度）
- 但**关系必须显式**（不靠读者/LLM 猜）

---

## §4. 让它“不会审计化”的两道硬闸门

### 4.1 Trust Boundary（信任边界）只服务交付，不服务追责

定义三类来源：

- **AXIOM（不追问）**：监护人裁决、编译/测试事实、当前 SSOT 的定义块
- **DERIVED（需闭环）**：设计推导/取舍/迁移方案（必须落到验证）
- **META（仅记录）**：changefeed/过程日志（不用于阻塞交付）

注意：AXIOM 的存在是为了**终止无限回归**，不是为了“律师式证据保全”。

### 4.2 审阅输出的建设性约束

- 每个问题必须附带至少一个：修复建议 / 验证方法 / 最小复现 / 下一步探针
- 任何“只指出不推进”的输出视为噪声

这条不是道德要求，是**系统可终止性**要求。

---

## §5. 用 W-0006 举例：把“类型替换”建模成可交付闭环

下面是一种最小闭环的建模方式（示意）：

1) `goal`
- `G-ONEPASS-READ`：一次性读取 frame slice，减少多次访问

2) `obstacle`
- `O-ADDR64-TOO-THIN`：`Address64` 只有地址，无法表达 slice 长度，导致多次读取

3) `decision`
- `D-SIZEDPTR-01`：用 `SizedPtr` 替代 `Address64`

4) `definition`
- `DEF-SIZEDPTR-LAYOUT`：bit layout / null 语义 / 边界约束 / 序列化映射

5) `task`
- `TASK-REPLACE-USAGES`：替换所有使用点（并清理/保留兼容层的决策）

6) `test`
- `T-ROUNDTRIP`：序列化/反序列化 roundtrip
- `T-BOUNDARY`：offset/length 边界（0、最大、溢出、默认值）
- `T-ONEPASS-READ`：一次性读取路径的行为验证或基准探针

关系：
- `O-ADDR64-TOO-THIN motivates G-ONEPASS-READ`
- `D-SIZEDPTR-01 enables G-ONEPASS-READ`
- `D-SIZEDPTR-01 depends_on DEF-SIZEDPTR-LAYOUT`
- `D-SIZEDPTR-01 validated_by T-ROUNDTRIP, T-BOUNDARY, T-ONEPASS-READ`

在这张图里，“历史文档某行提过 Address64”顶多属于 `context`，不可能成为阻塞主链。

---

## §6. 未来展望：从 Lite 到 可计算（但不掉进形式化深渊）

### 6.1 第一阶段：能写、能读、能用（1-2 周）

- 先在 `wish/*/handoffs/` 或 `docs/` 中试点 1-2 个 Wish
- 只要求关键节点（decision/definition/test/task）具备 `id + kind + edges`
- 不要求全覆盖

### 6.2 第二阶段：能自动检查“闭环缺口”（2-4 周）

只做非常便宜的静态检查：
- 每个 `decision` 是否至少有一个 `validated_by`
- 每个 `risk` 是否有归属（`mitigates` 或明确 out-of-scope）
- 是否存在 `goal` 没有任何 `enables`（空心目标）

这一步就能显著抑制“审阅发散”。

### 6.3 第三阶段：逐步接入 DocGraph/索引（可选）

- 自动汇总出“目标链路图”“决策→验证矩阵”“迁移任务清单”
- 让 SSOT 从“平铺文本”升级为“树/图状结构 + 文本解释”

---

## §7. 结语：形式化不是目的，交付闭环才是

AI 软件设计建模的关键，不是把自然语言替换成数学符号；而是：

- 用**少量结构**把“目标/障碍/依赖/验证”钉死
- 让审阅从“能无限挑刺”变成“必须推进交付”
- 在成本可控的前提下，把自然语言的优势（信息密度、表达力）保留下来

这就是“足够形式化”的意义。
