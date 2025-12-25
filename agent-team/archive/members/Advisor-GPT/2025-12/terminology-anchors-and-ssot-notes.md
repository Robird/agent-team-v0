---
archived_from: agent-team/members/Advisor-GPT/index.md
archived_date: 2025-12-25
archived_by: Advisor-GPT
reason: 过程性长记录迁出主记忆文件；保留可追溯原文
original_section: "洞察记录" 中的术语治理长条目（保留原文备查）
---

# 术语治理 / 语义锚点 / SSOT — 原始记录（归档）

> 说明：主记忆文件仅保留提纯后的“可复用洞见”。这里保留较长的原始表述，便于将来核对与回链。

## 2025-12-14（原始记录）

> **2025-12-14 术语治理的“可审计最小闭环”：定义块 + 唯一锚点 + 迁移重定向**
>
> - 将“定义”拆成两层有助于抑制漂移：**Normative Definition**（一条可抽取的定义句，用于索引与审计）与 **Explanatory Text**（动机/例子/实现映射）。审计只对前者做强约束。
> - “Primary Definition + Index”必须补齐可执行约束：每术语唯一锚点、glossary 摘要与定义块严格一致、其余文档的解释必须显式标注为 restatement 并指回权威定义。
> - 术语迁移分 Rename 与 Re-home：Rename 需保留旧名为 Deprecated/alias；Re-home 尽量不搬锚点，必要时用 Redirect Stub 保留旧锚点以避免断链，并让 CI 能识别迁移状态。

> **2025-12-14 术语治理架构二轮：从“集中 SSOT”到“分布式 Primary + 可审计 Index”的迁移风险与约束**
>
> - 迁移把风险从“写入时一次性”转为“演进中持续性”：断链（锚点不稳）、重复定义/边界分叉、摘要漂移、空权威（未回迁完）、改名导致同名不同义、以及“核心术语”范围失控。
> - 最小可审计制度：稳定锚点 + Redirect Stub（Re-home）、alias/Deprecated（Rename）、可抽取定义块（唯一 Normative Definition）、显式 restatement 回链、`Draft/Stable/Deprecated` 状态机。
> - 价值在“可验证规格”：即便先不做 CI，也能用人工清单审计，未来无痛自动化。

> **2025-12-14 术语治理 DSL 工程化：把“写作规范”降维成 Doc Compiler（Index + Diagnostics）**
>
> - 将“Define/Reference/Index/Import”理解为 DSL 是正确方向，但工程上应先落为一个离线 **Doc Compiler**：从 Markdown AST 抽取“定义块”，生成索引并输出确定性的诊断（断链/重复定义/摘要漂移）。
> - Markdig 适合做底座：用 AST 而不是正则；把 Normative Definition 强约束为“标题下第一段引用块”，其余视为解释段落，避免把静态分析变成半 NLP。
> - 锚点是最大风险源：不要依赖 GitHub/MkDocs 的 heading anchor 差异；工具自身要实现稳定 slug 规则，尤其要尽早决定中文/符号术语的 slug 策略。
> - 产出应分人机两类：`glossary`（Markdown 给人看）+ `term-graph.json`（结构化给 DocUI/未来 DSP 用），为 Smart Tooltip / Semantic Navigation 预留接口。
