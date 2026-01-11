
# Node-Level-Matter：如何为ATX-Node附加可机读的Key-Value信息？

## CodeFenceBlock方案

````markdown
## term `ATX-Matter` 条款装饰元素
用于承载该 Clause 的修饰信息（attributes）。通常是对其他@`Clause-Node`的依赖关系。
是可选信息。
内容是YAML。
*选Matter这个词纯粹是为了拉近与front-matter的关系，让LLM感到熟悉。*

### spec [F-CLAUSE-MATTER-FORMAT] 定义Clause的元信息格式
是Markdown fenced code block, 且info string MUST 是自定义标记`clause-matter`，且 SHOULD 紧跟在@`Clause-Node`的Heading下面。
解析器 SHOULD 容忍在Heading和clause-matter之间出现空行，但为了最佳可读性，建议不要插入其他内容。

### derived [F-CLAUSE-MATTER-EXAMPLE] 条款元信息示例
```clause-matter
note:注意这条Markdown fence紧跟在@[F-CLAUSE-MATTER-EXAMPLE]条款的标题下。
```
````

## EmphasisInline方案

愿景
````markdown
## term `KV-Emphasis` Emphasis键值对
**see-also: @`TODO-Emphasis`**
一种灵活的为Markdown块级元素附加可机读Key-Value信息的机制，虽然初始动机仅是为了服务于@`ATX-Node`，但提供了后续更精细修饰的能力资源。
````

思路：
用特定开头的斜体/加粗，同@[F-TODO-FORMAT]的思路那样，让@`Identifier`+`:`作为语法，`TODO-Emphasis`退化为一个预定义的`KV-Element`。
- 对于K-V信息非常灵活，不仅可以修饰Node，还能修饰Block。
- 直接使用普通markdown inline级别的引用语法而无需搞YAML层级的ID引用语法（选字面量还是选字符串模板问题）
- 低语法噪音。
- 与@`TODO-Emphasis`和@`Identifier`兼容。

---

# Clause-Change-Tracking：工具如何感知条款内容变更并自动分析影响？

## Hash-in-HTML-Comment方案

> **提案日期**：2026-01-11  
> **提案者**：Seeker, Craftsman, TeamLeader  
> **状态**：孵化中  
> **相关研究**：[clause-id-strategy-exploration.md](../../handoffs/clause-id-strategy-exploration.md) · [clause-id-strategy-engineering-review.md](../../handoffs/clause-id-strategy-engineering-review.md)

### 核心问题

在采用"语义锚点"命名策略后（条款ID保持稳定，内容可演进），如何让工具自动检测条款内容的实质性变更，避免静默漂移？

### 提案概述

**在条款 Heading 行尾添加 HTML 注释形式的内容指纹**，作为变更检测的元数据。

**语法示例**：
```markdown
### decision [S-DATA-CONSISTENCY] 数据一致性保证 <!-- fp:sha256-10:a7c3d9e12f -->
```

### 设计要点

#### 1. 指纹放置位置

**推荐**：Heading 行尾 HTML 注释
```markdown
### decision [ID] Title <!-- fp:algorithm-len:hex -->
```

**优势**：
- ✅ 完全兼容 GFM（HTML comment 是合法 Markdown）
- ✅ 不侵占 DocGraph 依赖的 front-matter
- ✅ 对未来 DSL 解析器友好（可选解析层）

#### 2. 指纹格式

**格式**：`fp:sha256-<hexLen>:<hex>`

**示例**：
- `fp:sha256-10:a7c3d9e12f` (40-bit, 推荐)
- `fp:sha256-12:a7c3d9e12f8b` (48-bit, 更安全)

**关键决策**：
- **算法**：SHA-256（跨平台、库成熟、实现简单）
- **长度**：10-12 hex（在 1k 规模内碰撞风险可接受，视觉负担低）
- **显式标记**：便于未来升级算法而不破坏旧文档

#### 3. 哈希输入（Canonical Input）

**目标**：减少格式噪音，同时对规范性变化敏感。

**建议规范化步骤**（按顺序）：
1. 输入是"Clause-Node 的内容块"（Heading 以下直到下一个同级或更高层 Heading 之前）
2. 统一换行到 `\n`；移除尾随空白；去掉文件末尾多余空行
3. 折叠连续空行为单个空行
4. 对 HTML comment：**剔除**（避免指纹自指）
5. 对代码块/表格：**保留**（规范性内容常在此表达）

**可选增强**（复杂但更精确）：
- 双指纹机制：全内容指纹 + 规范性行指纹（仅对包含 MUST/SHOULD/MAY 的行）
- 价值：让"实质性变更"更可控，减少因示例/解释变更造成的噪音
- 代价：规则更复杂，需要教育人和 AI

#### 4. 分层策略

**不同层级对内容指纹的要求不同**：

| 层级 | 内容指纹 | 理由 |
|:-----|:---------|:-----|
| **Decision** | MAY（可选） | 决策变更应通过畅谈会显式确认，指纹可作为辅助检测 |
| **Spec** | SHOULD（推荐） | 技术规格变更应被显式感知，防止静默漂移 |
| **Derived** | MUST NOT（禁止） | 推导性内容允许自由精炼，无需指纹负担 |

#### 5. 工具支持最小闭环

**必需命令**（建议实现为 `atelia dsl` CLI）：

1. **lint**：
   - 校验 Clause-ID 唯一性（case-insensitive）
   - 校验引用存在性：扫描 `@[...]` 引用
   - 校验指纹（若存在/或按策略必须存在）：计算 canonical fp，与注释比对

2. **fix**：
   - 自动插入/更新 `<!-- fp:... -->`
   - 提供 `--check`（只检查不写）供 CI 使用

3. **explain --clause [ID]**：
   - 输出 canonical 文本（或其摘要）+ 计算出的 fp
   - 便于调试与 code review

#### 6. CI 错误分级策略

**Error（阻断 CI）**：
- Clause-ID 重复（case-insensitive）
- 语法不符合（`decision/spec/derived [id]` 结构破坏）
- 指纹策略要求存在但缺失（例如 `spec` 必须有 fp）
- 指纹不匹配且 PR 未运行 `fix`

**Warning（不阻断）**：
- ID 风格不符合推荐大小写
- 指纹存在但格式未知（例如缺少算法字段）

### 核心价值主张

| 维度 | 纯语义锚点 | + 内容指纹 |
|:-----|:-----------|:-----------|
| **引用稳定性** | ✅ 优秀 | ✅ 优秀 |
| **变更可感知性** | ❌ 静默变更 | ✅ 工具检测 |
| **维护成本** | ✅ 最低 | ⚠️ 需工具自动化 |
| **演进友好度** | ✅ 精炼无阻 | ✅ 精炼无阻（指纹自动更新） |
| **审计追溯** | ⚠️ 依赖 Git | ✅ 指纹 + Git |

### 关键工程风险与缓解

#### Sev2 风险 1：Canonicalization 定义不当

**风险**：规范化规则过严或过松，导致误报/漏报。

**缓解**：
- 规则必须可判定、可实现、可回归
- 提供 `explain` 命令显示参与哈希的 canonical 文本
- MVP 先做单指纹（全内容），收集误报后再考虑双指纹

#### Sev2 风险 2：短指纹碰撞

**风险**：4 hex（16-bit）在几百条款规模下碰撞概率显著。

**缓解**：
- 推荐至少 10 hex（40-bit）或 12 hex（48-bit）
- 仍然足够短、肉眼可读

#### Sev2 风险 3：多人协作 merge conflict

**风险**：自动更新指纹导致 Heading 行频繁冲突。

**缓解**：
- 指纹放在行尾，保持固定格式，减少 diff 噪音
- PR 最后统一运行 `fix`，减少冲突面

### 可选增强：带指纹的精确引用

**场景**：安全关键/合规审计场景需要"内容锁定"。

**语法**：利用 Markdown Link 的 URL fragment 携带期望 fp
```markdown
@[S-FOO](spec.md#fp=sha256-10:a7c3d9e12f)
```

**工具行为**：既检查 `[S-FOO]` 存在，也检查目标定义的 fp 是否匹配 fragment。

**代价**：
- 引用解析从"regex 扫描"升级为"解析 link + fragment"
- 实现复杂度上升一档
- fragment 是约定，非标准，需工具链校验

### 迁移路径

**第一阶段（可选采用）**：
- 继续使用纯语义锚点
- 不引入指纹机制

**第二阶段（规模扩大后）**：
- 引入可选内容指纹
- 为 Spec 层关键约束条款添加指纹
- 工具提供 `lint`/`fix` 命令

**第三阶段（工具成熟后）**：
- 工具自动维护指纹
- 支持带指纹的精确引用（可选）

### 与命名风格决策的关系

**命名风格决策**（2026-01-11）明确了：条款ID应采用"具体倾向"命名，ID本身就传达立场。

**内容指纹方案的定位**：
- **不冲突**：指纹是变更检测机制，ID 是语义锚点
- **互补**：具体倾向命名 + 指纹检测 = 既有语义清晰度，又有变更可感知性
- **独立演进**：指纹方案可以在命名风格确立后独立孵化

### 待解决的开放问题

1. **指纹算法选择**：SHA-256 是否为最优？是否需要考虑更快的非加密哈希（如 XXH3）？
2. **指纹覆盖范围**：是否需要双指纹机制（全内容 + 规范性行）？
3. **跨文档引用的指纹**：`@[Lib.S-FOO#fp]` 如何处理版本一致性？
4. **性能优化**：大规模文档（数千条款）时的增量计算策略？

### 参考资料

- **深度探索**：[clause-id-strategy-exploration.md](../../handoffs/clause-id-strategy-exploration.md)（Seeker，2026-01-11）
- **工程评审**：[clause-id-strategy-engineering-review.md](../../handoffs/clause-id-strategy-engineering-review.md)（Craftsman，2026-01-11）
- **命名风格决策**：[2026-01-11-clause-id-naming-style.md](../../meeting/2026-01-11-clause-id-naming-style.md)

---
