# 畅谈会：DocGraph 工具实现（W-0002）

> **日期**：2025-12-30
> **标签**：`#design` `#implementation`
> **主持人**：刘德智 (Team Leader)
> **参与者**：Seeker, Curator, Craftsman, Implementer, DocOps, QA
> **状态**：进行中
> **前情提要文件**：`2025-12-30-layers-of-dev.md`

---

## 背景

本畅谈会是 **W-0002（文档图工具）** 的专门实现会议。基于前情提要文件（1700+行）中的讨论共识，我们在此聚焦具体实现。

### 前情提要摘要

**监护人决策澄清**：
1. **图遍历方案**：引入注册文件（root objects / known docs）作为遍历起点
2. **核心功能**：提取信息并汇总成表格是唯一有实用价值的功能
3. **实施策略**：最粗暴有效的办法，硬编码先跑起来（如生成 `wishes/index.md`）
4. **Fix 操作范围**：悬空引用问题应汇总报错/报告，而不是静默处理
5. **工具命名**：建议 `DocGraph` 等更功能性的名字

**已完成的成果**：
1. ✅ Wish 系统文件结构已创建（`wishes/` 目录）
2. ✅ W-0002 L2 API 设计草案（`doc-graph-api.md`）
3. ✅ 架构评审（Seeker）和体验评审（Curator）已完成

**当前状态**：
- W-0002 L2 状态：🟡 InProgress
- 需要：L3 条款定义 → L4 实现决策 → L5 代码实现

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

欢迎来到 DocGraph 工具实现畅谈会！基于监护人的明确指导，我们的任务是：

**MVP 目标**：实现一个硬编码的、能生成 `wishes/index.md` 的工具
**核心功能**：从 wishes 目录约定（隐式 Registry）出发，遍历文档图，提取 frontmatter 字段，汇总成表格
**技术约束**：最粗暴有效，先跑起来，后续再抽象

**关键设计决策**（监护人已澄清）：
1. **图遍历起点**：`wishes/active` / `wishes/completed` / `wishes/abandoned` 目录下的文档集合（隐式 Registry）
2. **汇总表格**：硬编码生成 `wishes/index.md`，字段固定
3. **错误处理**：悬空引用报错，不自动修复
4. **命名**：工具名采用 `DocGraph`（历史名：`tempsync` / 温度信息同步工具）

> 注：本记录后续将“注册文件（registry.md/yaml）”收敛为 **“wishes 目录约定即隐式 Registry”**（更符合 MVP 的“最粗暴有效”原则）。

让我们从 **Craftsman** 开始，基于监护人指导重新定义 L3 条款。

---

### Craftsman 发言（L3 条款定义）

`#design`

以下为 **W-0002（DocGraph 工具）** 的 L3 条款（Rules）。目标是：用“最粗暴有效”的方式把工具跑起来，首要产出为生成 `wishes/index.md`；其余能力一律让位于“可判定、可复现、可持续维护”。

---

## 0. 术语与范围

- **DocGraph**：从“隐式 Registry（wishes 目录约定）”出发，遍历一组已知文档（可扩展到链接可达集合），提取字段，生成表格与错误报告的工具。
- **Registry（隐式）**：以约定方式声明 DocGraph 的遍历起点与输出目标的 SSOT。本 MVP 中，“约定目录 + 目录下的文档集合”即 Registry。
- **悬空引用（Dangling Reference）**：Markdown 链接解析后的本地目标文件不存在，或在 workspace 外。

### 范围声明（与 L2 草案对齐后的收敛）

- **[DG-SC-001]** MUST：DocGraph 的唯一“有实用价值”的核心能力是：**提取信息并汇总成表格**（首个硬编码目标：生成 `wishes/index.md`）。
- **[DG-SC-002]** MUST：MVP 阶段不做任何“静默修复”（包括但不限于自动补全反向链接、自动改写链接路径、自动迁移文件）。
- **[DG-SC-003]** SHOULD：工具名使用 `DocGraph`（或同等功能性命名），避免隐喻型命名（如 tempsync）。

---

## 1. Registry（隐式：wishes 目录约定）条款

### 1.1 固定目录约定与基本约束

- **[DG-RG-001]** MUST：MVP 不引入显式 Registry 文件；以固定目录约定作为 Registry（SSOT）。
- **[DG-RG-002]** MUST：DocGraph 的 Wish 输入集合仅来自以下目录（相对于仓库根目录），不得退化为全仓库扫描：
	- `wishes/active/*.md`
	- `wishes/completed/*.md`
	- `wishes/abandoned/*.md`
- **[DG-RG-003]** MUST：上述目录约定 + 目录下实际存在的文档集合，是唯一权威来源（SSOT）。生成结果（如 `wishes/index.md`）必须被视为可重建派生视图。
- **[DG-RG-004]** MUST：若 `wishes/` 不存在，或上述任一目录缺失，DocGraph 必须失败并给出结构错误（不得静默忽略）。
- **[DG-RG-005]** SHOULD：目录枚举结果必须稳定排序（字典序），以保证生成输出的可复现性。

### 1.2 输出目标（MVP 硬编码）

- **[DG-RG-010]** MUST：MVP 的唯一输出为生成/覆写 `wishes/index.md`。
- **[DG-RG-011]** SHOULD：MVP 暂不支持“通过配置选择输出类型/路径”；扩展点留给未来（届时可重新引入显式 Registry）。

---

## 2. 图遍历（Graph Traversal）条款

### 2.1 遍历起点

- **[DG-TR-001]** MUST：遍历起点仅来自隐式 Registry 的目录枚举结果（known docs）。
- **[DG-TR-002]** SHOULD：对每个 root 文档，DocGraph 应解析其 Markdown 链接，并将“可达的本地 `.md` 文件”纳入遍历集合（形成文档图）。
- **[DG-TR-003]** MAY：提供“只处理 roots、不扩展链接”的运行模式（用于快速生成固定表格）。

### 2.2 链接解析与可达性规则

- **[DG-TR-010]** MUST：仅将以下链接视为“可遍历边（edge）”：
	- Markdown 普通链接：`[text](relative/or/absolute/path.md)`
	- 带锚点的本地文档链接：`[text](path.md#anchor)`（遍历目标文件时忽略 `#anchor`）
- **[DG-TR-011]** MUST：忽略并不遍历：
	- 外部链接（`http://`、`https://` 等）
	- 图片链接（`![](...)`）
	- 非 `.md` 目标
- **[DG-TR-012]** MUST：任何解析到的本地目标路径都必须规范化（消解 `..`/`.`），并以仓库根目录为 workspace 边界。
- **[DG-TR-013]** MUST：若目标路径在 workspace 之外，必须报告为悬空引用（不得静默跳过）。
- **[DG-TR-014]** MUST：遍历必须去重（按规范化后的路径维护 visited 集合），并保证对循环引用可终止。

---

## 3. 信息提取（Extraction）条款

### 3.1 Frontmatter 提取

- **[DG-EX-001]** MUST：对每个被处理的 Markdown 文档，若存在 YAML frontmatter（文件起始 `---` 块），必须尝试解析。
- **[DG-EX-002]** MUST：YAML 解析失败必须报错（记录文件路径与失败原因），不得以空字典替代。
- **[DG-EX-003]** SHOULD：frontmatter 解析后的键名按原样保留；表格生成阶段负责字段映射。

### 3.2 Wish 文档专用提取（硬编码允许）

为生成 `wishes/index.md`，MVP 允许对 Wish 文档采用硬编码解析规则：

- **[DG-EX-010]** MUST：Wish 文档必须至少能提取：`wishId`、`title`、`status`、`owner`、`updated`（均来自 frontmatter）。
- **[DG-EX-011]** MUST：Wish 的 L1-L5 状态必须通过解析正文中的 “层级进度 (Layer Progress)” Markdown 表格得到。
	- 识别规则：找到包含“层级进度”标题后的首个 Markdown 表格；按 `| 层级 | 状态 | ... |` 的列含义读取。
	- 映射规则：层级名 `L1`/`L2`/`L3`/`L4`/`L5`（允许带文字后缀如 `L1 Why`）→ 状态符号（⚪/🟡/🟢/🔴/➖）。
- **[DG-EX-012]** MUST：若某 Wish 缺少该表格、或缺少某层级行，必须报告为结构错误（因为会导致索引表格不可判定）。

---

## 4. 表格生成（Table Generation）条款

### 4.1 输出目标与确定性

- **[DG-TB-001]** MUST：MVP 必须支持生成 `wishes/index.md`。
- **[DG-TB-002]** MUST：生成过程必须确定性：同一输入文档集合与隐式 Registry（目录约定），输出字节序列必须一致（忽略运行时时间戳字段时不应引入随机性）。
- **[DG-TB-003]** SHOULD：默认按 `wishId` 升序排序；排序必须是稳定的字符串排序。

### 4.2 wishes/index.md 的内容规则（MVP 硬编码模板）

- **[DG-TB-010]** MUST：`wishes/index.md` 必须被视为派生视图，文件头部必须包含“可重建”声明（与 Wish 系统规范一致）。
- **[DG-TB-011]** MUST：至少生成以下三个分组段落与表格：
	1) `活跃 Wish (Active)` 表格（来源：`wishes/active/*.md`）
	2) `已完成 Wish (Completed)` 段落/表格或空态文案
	3) `已放弃 Wish (Abandoned)` 段落/表格或空态文案
- **[DG-TB-012]** MUST：Active 表格列固定为：`WishId`、`标题`、`Owner`、`L1`、`L2`、`L3`、`L4`、`L5`、`更新日期`。
- **[DG-TB-013]** MUST：`WishId` 单元格必须生成相对链接：`[W-XXXX](active/wish-XXXX-*.md)`（Completed/Abandoned 分组同理指向各自目录）。
- **[DG-TB-014]** SHOULD：Quick Nav（“从这里开始”）在 MVP 中允许为固定空态文本（例如提示用户查看 Active 表格），不得因缺少配置而崩溃。
- **[DG-TB-015]** MAY：Issue 汇总、统计信息等非核心段落可暂时生成固定空态文本；但若生成，必须保持确定性。

---

## 5. 错误处理（Error Handling）条款

### 5.1 错误分类与最小报告字段

- **[DG-ER-001]** MUST：任何悬空引用必须被聚合报告（不得静默忽略）。
- **[DG-ER-002]** MUST：错误报告最小字段集：`errorCode`、`message`、`sourcePath`、`details`。
- **[DG-ER-003]** SHOULD：对链接类错误，报告应包含：`rawTarget`、`resolvedPath`、（若可得）`lineNumber`。

建议的最小错误码集合（MVP）：
- `WISHES_STRUCTURE_INVALID`
- `DOC_NOT_FOUND`
- `PARSE_INVALID_YAML`
- `WISH_LAYER_PROGRESS_MISSING`
- `LINK_TARGET_NOT_FOUND`
- `LINK_TARGET_OUTSIDE_WORKSPACE`

### 5.2 失败策略（不静默）

- **[DG-ER-010]** MUST：出现以下任一情况，DocGraph 必须以失败结束（non-success）：
	- wishes 目录结构非法（缺失 `wishes/active` / `wishes/completed` / `wishes/abandoned` 等）
	- YAML frontmatter 解析失败
	- Wish 层级进度表格缺失/不可解析
- **[DG-ER-011]** SHOULD：出现任意悬空引用（内部 `.md` 链接目标不存在或越界）时，DocGraph 应以失败结束，以便在 CI 或人工流程中被发现。
- **[DG-ER-012]** MAY：提供“允许悬空引用但仍生成表格”的开关；但即便允许，错误报告仍必须输出。

---

## 6. 与 L2 草案的关系（裁剪说明）

- 本 L3 将 L2 草案中的“修复双向链接 / 自动补全”能力明确降级为 **非目标**（MVP 不做）。
- L2 中的通用 `TableConfig`/多命令 CLI 作为未来扩展方向保留；但 L3 规定：MVP 以“固定目录约定（隐式 Registry）”+ `wishes/index.md` 单一输出先跑通。
---

### Seeker 发言（L3 评估 & L4 决策）

`#design`

Craftsman 的 L3 条款展现了"最粗暴有效"原则的精准落地。我从架构角度评估这些条款的可实现性，并提出 L4 实现决策建议。

---

#### 一、L3 条款架构评估

**1.1 核心设计决策评估**

| 决策点 | L3 条款 | 架构评估 | 可实现性 |
|:-------|:--------|:---------|:---------|
| **SSOT 模式** | [DG-RG-003] Registry 为唯一权威 | ✅ 清晰的单向数据流 | 高 |
| **遍历起点** | [DG-TR-001] 仅从 roots 出发 | ✅ 消除了"全仓库扫描"的复杂性 | 高 |
| **硬编码提取** | [DG-EX-010~012] Wish 专用规则 | ⚠️ 权宜之计，但 MVP 合理 | 高（短期） |
| **错误策略** | [DG-ER-010~011] 失败优先 | ✅ Fail-fast 符合工具正确性 | 高 |

**核心洞见**：这套条款成功地将"图遍历"问题简化为**"配置驱动的扩展搜索"**——从 roots 出发，沿链接扩展，但边界由 workspace 约束。这是务实的简化。

**1.2 条款间一致性检查**

| 检查项 | 结果 | 分析 |
|:-------|:-----|:-----|
| Registry 格式 vs 遍历条款 | ✅ 一致 | `roots` glob 展开后直接成为遍历起点 |
| 提取条款 vs 表格条款 | ✅ 一致 | [DG-EX-010] 提取的字段正好是 [DG-TB-012] 表格列 |
| 错误处理 vs 遍历条款 | ✅ 一致 | 悬空引用的定义在 [DG-TR-013] 和 [DG-ER-001] 对齐 |
| L2 裁剪说明 vs 范围声明 | ✅ 明确 | Fix 能力显式降级，无歧义 |

**1.3 识别的架构风险**

| 风险 | 来源条款 | 影响 | 缓解建议 |
|:-----|:---------|:-----|:---------|
| **硬编码的可维护性** | [DG-EX-011] 层级进度表格解析 | 中 | 封装为独立解析器，后续可替换 |
| **glob 排序的稳定性** | [DG-RG-013] | 低 | 使用确定性排序算法（如字典序） |
| **循环引用检测** | [DG-TR-014] | 低 | visited 集合机制已声明 |

---

#### 二、注册文件方案的架构合理性

**2.1 Registry 设计模式分析**

Craftsman 提出的 Registry 方案本质上是一种 **Manifest 模式**——声明式地定义"已知实体"和"期望输出"。

**系统类比**：

| 系统 | Manifest | 作用 |
|:-----|:---------|:-----|
| npm | `package.json` | 声明依赖和脚本 |
| Cargo | `Cargo.toml` | 声明 crate 和 features |
| **DocGraph (MVP)** | 目录约定 | 声明 roots 和 outputs |

**架构优势**：
1. **消除歧义**：不存在"从哪里开始扫描"的隐式假设
2. **可复现性**：相同 Registry + 相同文档 → 相同输出（[DG-TB-002] 确定性要求）
3. **最小心智负担**：Wish 作者只需“把文件放进正确目录”，不需要维护额外配置文件

**潜在张力**（非当前阻塞）：

> Registry 列出的是"骨干文档"（roots），但真正的文档图是通过链接扩展得到的。
> 如果某个重要文档既不在 roots 中，也没有被任何 root 链接，它将被遗漏。

**缓解（已在 L3 中隐含）**：
- 目录约定本身就是“已知入口”，仍然允许通过链接扩展得到可达集合
- 若未来需要跨目录/跨域扩展或多输出，再引入显式 Registry 文件即可

**我的判断**：对 MVP 而言，“目录约定即隐式 Registry”是**更粗暴有效**的决策：把"图发现"问题转化为"约定 + 遍历"问题，减少一个配置层，降低实现与使用成本；代价是短期可扩展性下降，但可在下一阶段通过显式 Registry 补回。

---

#### 三、硬编码解析的可维护性分析

**3.1 [DG-EX-011] 层级进度表格解析**

这是 L3 中最"硬编码"的条款：

> "找到包含'层级进度'标题后的首个 Markdown 表格；按 `| 层级 | 状态 | ... |` 的列含义读取。"

**架构分析**：

| 维度 | 评估 | 说明 |
|:-----|:-----|:-----|
| **耦合度** | 高 | 与 Wish 文档格式强绑定 |
| **脆弱性** | 中 | 标题/列名变化将破坏解析 |
| **可测试性** | 高 | 纯文本输入/结构化输出，易于单元测试 |
| **可替换性** | 取决于实现 | 若封装为独立模块，可后续替换 |

**设计模式建议**：将 Wish 文档解析器实现为 **Strategy 模式**：

```
IDocumentExtractor
├── WishDocumentExtractor (MVP: 硬编码规则)
└── GenericFrontmatterExtractor (未来: 通用提取)
```

这样 MVP 的硬编码规则可作为一个具体策略存在，不阻塞未来的泛化。

**3.2 表格解析的鲁棒性考量**

[DG-EX-011] 要求解析 Markdown 表格。Markdown 表格有多种变体：

| 情况 | 示例 | 条款覆盖 |
|:-----|:-----|:---------|
| 标准对齐 | `\| L1 \| 🟢 \|` | ✅ 隐含支持 |
| 列对齐标记 | `\|:---\|:---:\|` | ⚠️ 未显式说明 |
| 多余空格 | `\|  L1  \|  🟢  \|` | ⚠️ 需规范化 |
| 缺少列 | `\| L1 \|` | ❓ 应视为错误？ |

**建议补充到 L4**：
- 表格解析应容忍"装饰性差异"（空格、对齐标记）
- 列数不足应报错（与 [DG-EX-012] 一致）

---

#### 四、L4 实现决策建议

基于 L3 条款，以下是关键的 L4（How）层面决策点：

**4.1 项目结构决策**

| 选项 | 描述 | 推荐 |
|:-----|:-----|:-----|
| **A. 独立项目** | `atelia/src/DocGraph/` 作为独立程序集 | ✅ 推荐 |
| **B. 现有项目扩展** | 作为 Atelia.Core 的子模块 | ❌ 过早耦合 |
| **C. 脚本工具** | 单文件 .csx 脚本 | ❌ 难以测试 |

**推荐理由**：独立项目便于测试、打包和未来的 CLI 包装。

**项目结构建议**：

```
atelia/src/DocGraph/
├── DocGraph.csproj
├── Registry/
│   ├── WishDirectoryRegistry.cs # 隐式 Registry（目录约定）
│   └── RegistryModel.cs         # Registry 数据模型（未来可扩展为显式）
├── Traversal/
│   ├── LinkExtractor.cs       # 提取 Markdown 链接
│   ├── DocumentGraph.cs       # 文档图遍历
│   └── PathNormalizer.cs      # 路径规范化
├── Extraction/
│   ├── IDocumentExtractor.cs  # 策略接口
│   ├── WishExtractor.cs       # Wish 专用提取器
│   └── LayerProgressParser.cs # 层级进度表格解析
├── Generation/
│   ├── IndexGenerator.cs      # 生成 index.md
│   └── TableRenderer.cs       # Markdown 表格渲染
├── Errors/
│   └── DocGraphError.cs       # 错误模型
└── Program.cs                 # CLI 入口
```

**4.2 核心库选择**

| 依赖 | 用途 | 版本建议 | 理由 |
|:-----|:-----|:---------|:-----|
| **Markdig** | Markdown 解析 | 最新稳定版 | .NET 生态标准选择 |
| **YamlDotNet** | YAML frontmatter | 最新稳定版 | 成熟可靠 |
| **System.IO.Abstractions** | 文件系统抽象 | 最新稳定版 | 便于测试 |

**技术决策**：

| 决策点 | 选项 | 推荐 | 理由 |
|:-------|:-----|:-----|:-----|
| **Markdown AST vs 正则** | Markdig AST | ✅ AST | 结构化解析更可靠，避免正则脆弱性 |
| **Frontmatter 分离** | Markdig 扩展 vs 手工分割 | 手工分割 | Markdig 的 YAML 扩展不如 YamlDotNet 灵活 |
| **Glob 实现** | Microsoft.Extensions.FileSystemGlobbing | ✅ | 标准库，与 .NET 生态对齐 |

**4.3 遍历算法决策**

[DG-TR-002] 要求"将可达的本地 .md 文件纳入遍历集合"。这是一个图搜索问题。

| 算法 | 特点 | 推荐 |
|:-----|:-----|:-----|
| **BFS (广度优先)** | 层级清晰，内存占用可控 | ✅ 推荐 |
| **DFS (深度优先)** | 实现简单，但可能栈溢出 | 可选 |
| **并行遍历** | 性能好，但复杂度高 | ❌ MVP 不需要 |

**伪代码框架**：

```csharp
IEnumerable<Document> TraverseFromRoots(IEnumerable<string> roots)
{
    var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    var queue = new Queue<string>(roots);
    
    while (queue.Count > 0)
    {
        var path = queue.Dequeue();
        if (!visited.Add(NormalizePath(path))) continue;
        
        var doc = ParseDocument(path);
        yield return doc;
        
        foreach (var link in doc.Links.Where(IsLocalMarkdown))
        {
            var target = ResolvePath(path, link);
            if (!visited.Contains(target) && IsWithinWorkspace(target))
            {
                queue.Enqueue(target);
            }
        }
    }
}
```

**4.4 错误聚合决策**

[DG-ER-001] 要求"悬空引用必须被聚合报告"。这意味着遍历不应在第一个错误时中止。

| 策略 | 描述 | 推荐 |
|:-----|:-----|:-----|
| **Fail-fast** | 遇到第一个错误即停止 | ❌ 与条款不符 |
| **Collect-all** | 收集所有错误后统一报告 | ✅ 推荐 |
| **Threshold** | 错误达到阈值后停止 | 可选（未来） |

**实现模式**：使用 Result<T, Error> 模式或错误列表收集。

---

#### 五、与现有 L2 设计的整合建议

**5.1 L2 API 草案的命运**

[doc-graph-api.md](../../../wishes/specs/doc-graph-api.md) 定义了通用的五接口架构。L3 条款将其大幅收敛。

| L2 接口 | L3 MVP 状态 | 说明 |
|:--------|:------------|:-----|
| `IDocumentParser` | ✅ 保留核心 | frontmatter 解析必需 |
| `ILinkTracker` | ✅ 保留核心 | 链接提取必需 |
| `IBidirectionalChecker` | ❌ 降级 | MVP 不做双向检查修复 |
| `IIndexGenerator` | ✅ 保留但简化 | 硬编码 wishes/index.md |
| `IWorkspaceScanner` | ✅ 保留但简化 | glob 展开即可 |

**建议**：
- L2 文档保留为"愿景文档"
- L3 作为 MVP 的规范约束
- L4/L5 实现 L3，但**代码结构预留 L2 扩展点**

**5.2 代码结构的扩展预留**

即使 MVP 硬编码，代码结构也应为未来扩展预留接缝：

| 扩展点 | MVP 实现 | 预留方式 |
|:-------|:---------|:---------|
| 多输出类型 | 只有 `wishes/index.md` | 以生成器策略/分支预留（未来再引入显式配置） |
| 通用字段提取 | 只有 Wish 专用 | `IDocumentExtractor` 接口 |
| 配置化表格列 | 固定列 | `TableConfig` 概念保留（注释中）|

---

#### 六、实施路线图建议

基于五层级方法论，以下是推荐的实施顺序：

**Phase 1：基础设施（预计 2-3h）**

| 任务 | 产出 | 依赖 |
|:-----|:-----|:-----|
| 创建项目结构 | `DocGraph.csproj` + 目录 | 无 |
| 添加依赖 | Markdig, YamlDotNet, Glob | 项目创建 |
| 实现 PathNormalizer | 路径规范化工具 | 无 |

**Phase 2：Registry & 遍历（预计 3-4h）**

| 任务 | 产出 | 依赖 |
|:-----|:-----|:-----|
| RegistryModel | Registry 数据结构 | 无 |
| WishDirectoryRegistry | 加载 + 验证 wishes 目录结构 | RegistryModel |
| LinkExtractor | 从 Markdown 提取链接 | Markdig |
| DocumentGraph | BFS 遍历 + visited 去重 | LinkExtractor, PathNormalizer |

**Phase 3：提取 & 生成（预计 4-5h）**

| 任务 | 产出 | 依赖 |
|:-----|:-----|:-----|
| FrontmatterExtractor | YAML frontmatter 解析 | YamlDotNet |
| LayerProgressParser | Wish 层级进度表格解析 | Markdig |
| WishExtractor | 组合 frontmatter + layer progress | 上述两者 |
| IndexGenerator | 生成 wishes/index.md | WishExtractor, DocumentGraph |

**Phase 4：CLI & 错误处理（预计 2-3h）**

| 任务 | 产出 | 依赖 |
|:-----|:-----|:-----|
| DocGraphError | 错误模型 + 错误码 | 无 |
| 错误聚合 | Collect-all 模式实现 | DocGraphError |
| Program.cs | CLI 入口 | 所有核心模块 |

**Phase 5：测试 & 文档（预计 2-3h）**

| 任务 | 产出 | 依赖 |
|:-----|:-----|:-----|
| 单元测试 | xUnit 测试项目 | 核心模块 |
| 集成测试 | 端到端测试（使用 wishes/ 目录） | CLI |
| README | 使用说明 | CLI |

**总预计时间**：13-18 小时（不含评审和迭代）

---

#### 七、总结

**7.1 L3 条款架构评估结论**

| 维度 | 评分 | 说明 |
|:-----|:-----|:-----|
| **条款完备性** | 90% | 覆盖了 MVP 所需的所有关键路径 |
| **条款一致性** | 95% | 各条款间逻辑自洽 |
| **可实现性** | 95% | 技术难度低，依赖成熟 |
| **可测试性** | 90% | 大部分条款可转化为自动化测试 |
| **可扩展性** | 75% | 硬编码较多，但有预留空间 |

**7.2 关键 L4 决策推荐**

1. **项目结构**：独立项目 `atelia/src/DocGraph/`
2. **核心依赖**：Markdig + YamlDotNet + System.IO.Abstractions
3. **遍历算法**：BFS + visited 集合
4. **错误策略**：Collect-all 聚合报告
5. **扩展预留**：`IDocumentExtractor` 接口 + “未来多输出类型”的分支预留

**7.3 建议的下一步**

1. **确认 L4 决策**：邀请 Implementer 和 QA 确认技术选择
2. **准备 wishes 测试数据**：在 `wishes/active` / `wishes/completed` / `wishes/abandoned` 放入最小样例文档
3. **启动 Phase 1**：创建项目结构和基础设施

---

发言完毕。L3 条款在架构层面是**可行且合理**的，关键在于 L4/L5 阶段保持"先跑通，后优化"的节奏。
