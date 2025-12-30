# 畅谈会：DocGraph 设计审阅

> **日期**：2025-12-30
> **标签**：#review
> **主持人**：刘德智 (Team Leader)
> **参与者**：Seeker, Curator, Craftsman
> **状态**：进行中

---

## 背景
基于 W-0002（DocGraph 工具）的当前进展，审阅以下两份草稿：
1. `atelia/docs/DocGraph/api.md` - Shape-Layer API 设计草案
2. `atelia/docs/DocGraph/ore.md` - 设计原矿（汇总材料）

参考文件：
- `wishes/active/wish-0002-doc-graph-tool.md`
- `wishes/specs/wish-system-rules.md`

## 目标
识别最重要的待定问题，形成相应文档。识别问题本身就是项目的进展。

## 💬 畅谈记录

### 主持人开场 (刘德智)
大家好！今天我们审阅 DocGraph 的设计文档。当前有两份关键材料：
1. **Shape-Layer API 草案** - 定义了完整的愿景外观，包括5个核心接口和CLI设计
2. **设计原矿** - 汇总了会议记录、Wish文档和Shape-Layer草案中的信息，特别强调了MVP约束

我注意到一个关键张力：Shape-Layer草案描绘了完整的"文档图工具"愿景，而设计原矿中的Rule-Layer约束将MVP裁剪为"仅生成wishes/index.md的派生视图生成器"。

让我们从理解当前状态开始。首先邀请 **Seeker** 从概念架构角度分析这两份文档的一致性与冲突点。

---

### Seeker 发言

感谢主持人的邀请。我从概念架构角度审阅了这两份文档，以下是我的分析。

---

## 一、已达成共识的关键点

这两份文档在以下方面展现了清晰的共识：

1. **核心使命一致**：两份文档都认同 DocGraph 的核心价值是"提取信息并汇总成表格"。Shape-Layer 的 §1.1 与 ore 的 §0.1 在此点上完全对齐。

2. **设计原则相容**：Shape-Layer 提出的四原则（最小惊讶、渐进复杂度、可测试性、幂等性）与 ore 中强调的"确定性输出"、"失败策略"在理念上互补而非冲突。

3. **术语核心集合基本一致**：Document、Frontmatter、Link 等核心概念的定义在两份文档中语义兼容。

4. **分层架构认知**：两份文档都隐式接受了 Why-Layer→Craft-Layer 的分层框架，ore 明确将 Shape-Layer 定位为"愿景外观"，将 Rule-Layer 定位为"MVP 约束"。

5. **技术选型方向**：Markdig、YamlDotNet、System.IO.Abstractions 的选型建议在两份文档中一致出现。

---

## 二、存在冲突或模糊的关键点

### 2.1 范围定义的根本张力（核心冲突）

| 维度 | Shape-Layer API 草案 | ore Rule-Layer 约束 | 张力分析 |
|:-----|:------------|:------------|:---------|
| **扫描范围** | `IWorkspaceScanner` 默认 `**/*.md` | 仅 `wishes/{active,completed,abandoned}/*.md` | Shape-Layer 是开放式全局扫描，Rule-Layer 是闭合式目录约定 |
| **双向链接** | `IBidirectionalChecker` 作为核心接口 | MVP 不做静默修复，只报告/失败 | Shape-Layer 将"修复"列为使命，Rule-Layer 明确排除 |
| **配置化** | `TableConfig`、YAML 配置文件 | 硬编码生成 `wishes/index.md` | Shape-Layer 追求灵活性，Rule-Layer 追求确定性 |

**我的判断**：这不是错误，而是**层级间的正当张力**。Shape-Layer 描绘的是"工具应该能做什么"（Shape），Rule-Layer 描绘的是"MVP 只做什么"（Rules）。但两份文档需要**显式声明这一关系**，否则读者会困惑。

> **建议**：在 Shape-Layer 文档头部添加"MVP 范围约束"章节，明确引用 ore 中的 Rule-Layer 约束作为当前实现边界。

### 2.2 术语命名漂移（需显式治理）

| 术语 | 出现位置 | 问题 |
|:-----|:---------|:-----|
| `DocGraph` | Shape-Layer、ore | 当前共识名 ✅ |
| `tempsync` / "温度信息同步工具" | ore §0.2 提及历史名 | 历史遗留，需确保已彻底清理 |
| `Registry` | ore 中作为隐式概念 | Shape-Layer 未提及此术语 |
| `Workspace` vs `Registry` | Shape-Layer 用 Workspace，ore 用 Registry | 语义边界不清 |

**我的判断**：`Workspace` 和 `Registry` 指向不同层次：
- **Workspace** = 物理边界（目录树根）
- **Registry** = 逻辑边界（哪些文件是"已知文档"）

ore 的"隐式 Registry"概念（目录约定 = 文档集合）是一个重要的中间抽象，Shape-Layer 未显式建模。

> **建议**：在 Shape-Layer 术语表中补充 Registry 概念，明确 "Registry ⊆ Workspace"。

### 2.3 双向链接语义的分裂

Shape-Layer §1.1 声明核心使命之一是"检测并修复双向链接"，但 ore §3.1 明确 MVP 不做任何静默修复。

更深层的问题：**什么是"双向链接"的正确语义？**

- wish-system-rules.md 的 `[F-ARTIFACT-PARENTWISH-REQUIRED]` 条款要求层级产物必须有 `ParentWish` 字段
- 但 ore §6.2 提到"会议纪要作为共享产物不应强制 ParentWish"

这暗示存在**两类链接规则**：
1. **专属产物链接**：必须双向（Wish ↔ Shape-Layer/Rule-Layer 文档）
2. **共享产物链接**：可单向（Wish → 会议记录）

Shape-Layer 的 `IBidirectionalChecker` 接口需要能表达这种区分。

> **建议**：在 Shape-Layer 中引入 `LinkPolicy` 或类似概念，区分强制双向 vs 可选双向。

---

## 三、需要进一步澄清的概念问题

### 3.1 Document 模型的完备性问题

Shape-Layer 定义了 Document 模型：
```
Document
├── Path, Frontmatter, Content
├── OutgoingLinks, IncomingLinks
```

但遗漏了关键属性：
- **LayerProgressTable**（ore §3.4 要求的 Wish 专用解析产物）
- **DocumentType**（区分 Wish / Spec / Meeting / Generic）

> **澄清问题**：Document 是通用模型还是需要子类化？如果是通用模型，LayerProgressTable 应该如何挂载？

### 3.2 错误处理的"失败"语义

ore §3.6 定义了"失败策略"，默认行为是"以失败结束"。但未明确：
- **失败 = 进程退出码非零？**
- **失败 = 抛出异常？**
- **失败 = 不生成输出文件？**

Shape-Layer §6 定义了错误类型和报告格式，但未定义"失败"的操作语义。

> **澄清问题**：当 Rule-Layer 说"必须以失败结束"时，API 层面应该返回什么？建议在 Shape-Layer 补充 `RunResult` 类型：
> ```csharp
> public record RunResult(
>     bool Success,
>     IReadOnlyList<DocGraphError> Errors,
>     string? GeneratedContent // null if failed
> );
> ```

### 3.3 "遍历" vs "枚举" 的概念边界

ore §3.3 定义了图遍历：从 Root 出发，解析 Markdown 链接，扩展可达集合。

但 Shape-Layer 的 `IWorkspaceScanner` 只做目录扫描，不做链接遍历。

**概念分裂**：
- `IWorkspaceScanner.ScanAsync()` → 目录枚举
- `ILinkTracker.ExtractLinks()` → 链接发现
- 缺失：**GraphTraverser** 将两者组合

> **澄清问题**：图遍历应该是独立接口，还是 Scanner + Tracker 的组合使用？如果是后者，应在 Shape-Layer 中补充使用模式示例。

### 3.4 "层级进度表格解析"的位置

ore §3.4 定义了 Wish 专用提取逻辑（解析正文中的"层级进度"表格），这是一个相当复杂的解析需求。

但 Shape-Layer 的 `IDocumentParser` 只处理 frontmatter，正文解析能力未显式建模。

> **澄清问题**：层级进度表格解析应该是：
> - (A) `IDocumentParser` 的扩展方法？
> - (B) 独立的 `IWishParser` 接口？
> - (C) `IIndexGenerator` 的内部实现？

我倾向于 **(B)**：Wish 是一种特殊 Document，有自己的解析需求，值得独立建模。

---

## 四、概念架构建议

基于以上分析，我提出以下架构性建议：

### 4.1 显式分层：愿景 vs MVP

```
Shape-Layer              Rule-Layer (MVP)
─────────────────────   ─────────────────────
IWorkspaceScanner       → 裁剪为 WishRegistry
IDocumentParser         → 保留
ILinkTracker            → 保留，但只用于报告
IBidirectionalChecker   → MVP 不实现
IIndexGenerator         → 裁剪为 WishIndexGenerator
```

### 4.2 引入缺失的中间概念

| 缺失概念 | 用途 | 建议位置 |
|:---------|:-----|:---------|
| **Registry** | 已知文档集合（区别于 Workspace） | Shape-Layer 术语表 |
| **DocumentType** | 区分 Wish / Spec / Meeting | Shape-Layer Document 模型 |
| **LinkPolicy** | 区分强制双向 / 可选双向 | Shape-Layer BidirectionalChecker |
| **GraphTraverser** | 组合 Scanner + Tracker 做遍历 | Shape-Layer 新接口或使用模式 |

### 4.3 ore 文档的定位澄清

ore 当前自称"原矿"，但实际上已经承担了 Rule-Layer 草案的职能（§3 的 MUST/SHOULD 条款）。

> **建议**：将 ore §3 提取为独立的 `spec.md`（Rule-Layer），ore 保留为"设计决策上下文 + 待决事项"的素材库。

---

## 五、审阅总结

| 分类 | 数量 | 代表性问题 |
|:-----|:-----|:---------|
| ✅ 已达成共识 | 5 | 核心使命、设计原则、技术选型 |
| ⚠️ 存在张力 | 3 | 范围定义、术语漂移、双向链接语义 |
| ❓ 需澄清 | 4 | Document 完备性、失败语义、遍历边界、层级进度解析 |

**我的核心判断**：两份文档不是"冲突"，而是**处于不同抽象层级且未显式声明关系**。解决方案不是"统一"，而是"显式分层 + 交叉引用"。

> **类比**：这类似于 TCP 协议规范（RFC 793）与某个具体实现（如 Linux TCP 栈）的关系——后者是前者的裁剪实现，但需要明确说明"我实现了哪些 MUST、推迟了哪些 SHOULD"。

---

**下一步建议**：
1. 在 Shape-Layer 文档头部添加"MVP 约束声明"
2. 统一 Registry/Workspace 术语
3. 将 ore §3 独立为 Rule-Layer 规范草案
4. 补充 Document 模型的类型区分

以上是我的分析，期待 Curator 和 Craftsman 的视角补充。

---

### Curator 发言

感谢 Seeker 的精彩分析。我将从 **UX/DX（用户体验/开发者体验）** 角度对这两份文档进行评估，重点关注"用起来感觉如何"。

---

## 一、UX/DX 优点列表

### 1.1 接口设计的优点

| 优点 | 体现位置 | 为什么这是好体验 |
|:-----|:---------|:----------------|
| **命名意图清晰** | `IDocumentParser`, `ILinkTracker` | 从名字就能猜到用途，符合"最小惊讶"原则 |
| **Record 类型的不可变性** | `ParseResult`, `Link`, `TableConfig` | 数据结构的不可变性减少了"这个对象被谁改了"的调试焦虑 |
| **可选参数有默认值** | `ScanAsync(pattern = "**/*.md")` | 简单场景零配置，降低入门门槛 |
| **错误报告结构化** | §6.2 JSON 错误格式 | 包含 `suggestion` 字段——这是 **Error as Affordance** 的正确实践 |
| **LinkType 枚举显式化** | `Document`, `Anchor`, `External`, `Image` | 用户不需要猜测"这个链接会被怎么处理" |

### 1.2 CLI 设计的优点

| 优点 | 体现位置 | 为什么这是好体验 |
|:-----|:---------|:----------------|
| **动词优先** | `docgraph scan`, `docgraph links` | 符合 Git 式命令心智模型 |
| **--dry-run 选项** | `fix-backlinks --dry-run` | 给用户"安全预览"的控制感 |
| **--output 选项** | `table --output=./wishes/index.md` | 不强制覆盖，让用户决定输出位置 |

### 1.3 配置设计的优点

| 优点 | 体现位置 | 为什么这是好体验 |
|:-----|:---------|:----------------|
| **YAML 层级直观** | `table.columns[].header/field` | 结构映射物理世界的"表格列"隐喻 |
| **format 预设** | `format: "emoji"` | 减少样板代码，常见需求一行搞定 |

---

## 二、UX/DX 问题列表（按严重性排序）

### 🔴 严重问题（阻碍正常使用）

#### P1: 愿景与约束的割裂导致认知失调

**问题描述**：Shape-Layer 声明"检测并修复双向链接"为核心使命（§1.1），但 ore 明确 MVP 不做静默修复。如果开发者先读 Shape-Layer 再读 ore，会产生"这个工具到底做不做修复"的困惑。

**体验后果**：
- 开发者可能写出调用 `fix-backlinks` 的脚本，却发现 MVP 根本不存在这个功能
- 文档阅读体验类似于"被骗"——Shape-Layer 承诺的东西 Rule-Layer 不让做

**改进建议**：
```markdown
# 在 Shape-Layer 文档头部添加
> **MVP 约束**：当前版本仅实现 `docgraph table` 命令，硬编码生成 `wishes/index.md`。
> 以下接口中标记 [MVP] 的部分已实现，标记 [Future] 的部分为愿景规划。
```

这是 **隐喻限定模式** 的应用——告诉读者愿景的边界在哪。

---

#### P2: IWorkspaceScanner 的"虚假示能"

**问题描述**：Shape-Layer 定义 `IWorkspaceScanner.ScanAsync(rootPath, pattern = "**/*.md")`，暗示支持全局扫描。但 ore 明确 MVP 只扫描 `wishes/{active,completed,abandoned}/`。

**体验后果**：
- 接口签名承诺了它做不到的事——这是 **False Affordance**
- 开发者调用 `ScanAsync(".", "**/*.md")` 期望扫描全仓库，实际只得到 wishes 子集

**改进建议**：
- 方案 A（诚实命名）：MVP 阶段接口改名为 `IWishRegistry`，明确职责边界
- 方案 B（参数限制）：`pattern` 参数标记为 `[Future]`，MVP 固定为 wishes 目录

---

#### P3: "失败"语义不明导致调用者困惑

**问题描述**：ore §3.6 反复提到"必须以失败结束"，但 Shape-Layer 未定义"失败"在 API 层面意味着什么。

**体验后果**：调用者不知道该 catch 什么异常、检查什么返回值。

**改进建议**（与 Seeker 建议呼应）：
```csharp
public record RunResult(
    bool Success,
    IReadOnlyList<DocGraphError> Errors,
    string? GeneratedContent
);
```

并在文档中明确："失败 = `Success == false` + `GeneratedContent == null` + 进程退出码非零"。

---

### 🟡 中等问题（增加认知负担）

#### P4: IBidirectionalChecker 接口的"共享产物例外"未建模

**问题描述**：ore §6.2 提到"会议纪要作为共享产物不应强制 ParentWish"，但 Shape-Layer 的 `IBidirectionalChecker` 只有 `CheckAll()` 和 `HasBacklink()`，无法表达"这个链接不需要反向"。

**体验后果**：
- 实现者不知道如何跳过共享产物
- 用户会收到"缺失反向链接"的误报，造成"狼来了"疲劳

**改进建议**：引入链接策略枚举
```csharp
public enum LinkPolicy { MustBidirectional, OptionalBidirectional, Unidirectional }

public record MissingBacklink(
    ...
    LinkPolicy Policy  // 让调用者知道这是"必须修"还是"建议修"
);
```

---

#### P5: 层级进度表格解析的"隐藏复杂度"

**问题描述**：ore §3.4 要求解析正文中的"层级进度"表格，这是 **Wish 专属的硬编码逻辑**。但 Shape-Layer 的 `IDocumentParser` 只处理 frontmatter，正文解析能力被隐藏。

**体验后果**：
- 开发者看 Shape-Layer API 会以为 frontmatter 就是全部，踩到正文解析的坑
- 这违反了 **渐进复杂度** 原则——简单场景（frontmatter）和复杂场景（表格解析）的边界不清

**改进建议**：
- 显式声明 `IWishParser : IDocumentParser` 作为 Wish 专用解析器
- 或在 `ParseResult` 中添加 `Sections: List<ContentSection>` 来建模正文结构

---

#### P6: 错误码缺乏层级结构

**问题描述**：Shape-Layer §6.1 定义了 5 个错误码（`PARSE_INVALID_YAML`, `LINK_TARGET_NOT_FOUND` 等），ore §3.6 定义了 6 个。两者未对齐，且都是扁平列表。

**体验后果**：
- 开发者想"只处理链接类错误"时，必须硬编码匹配多个字符串
- 新增错误码时容易忘记更新处理逻辑

**改进建议**：引入错误码分类前缀
```
STRUCTURE_*  → 目录结构问题
PARSE_*      → 解析问题
LINK_*       → 链接问题
WISH_*       → Wish 专属问题
```

并提供 `error.Category` 属性，便于分组处理。

---

#### P7: CLI 命令与 API 接口的映射不透明

**问题描述**：Shape-Layer 定义了 5 个 CLI 命令和 5 个核心接口，但未说明"哪个命令调用哪个接口"。

**体验后果**：
- 想要"编程式调用"的开发者不知道从哪个接口入手
- CLI 用户想看源码理解行为时，找不到对应入口

**改进建议**：添加命令→接口映射表
| 命令 | 主要接口 | 状态 |
|:-----|:---------|:-----|
| `scan` | `IWorkspaceScanner` | [Future] |
| `links` | `ILinkTracker` | [Future] |
| `table` | `IIndexGenerator` | [MVP] |
| ... | ... | ... |

---

### 🟢 轻微问题（可优化）

#### P8: 配置文件的错误处理体验未设计

**问题描述**：Shape-Layer §5 定义了 YAML 配置格式，但未说明"配置文件解析失败"时的错误体验。

**体验后果**：用户写错 YAML 时，不知道会看到什么错误信息。

**改进建议**：补充配置错误的报告格式，至少包含行号和建议。

---

#### P9: 表格生成的"预览"能力缺失

**问题描述**：`IIndexGenerator.GenerateTable()` 返回 `string`，但用户可能想在生成前预览结构。

**改进建议**：考虑添加 `TablePreview` 类型，包含行列统计、缺失字段警告等元信息。

---

## 三、张力点的体验设计建议

Seeker 指出了三个关键张力点，我从体验角度补充处理建议：

### 张力 1: Shape-Layer 开放式扫描 vs Rule-Layer 闭合式 Registry

| 方案 | 体验代价 | 体验收益 |
|:-----|:---------|:---------|
| Shape-Layer 接口保持开放，运行时抛异常限制 | 虚假示能，用户被骗 | 代码结构简单 |
| Shape-Layer 接口裁剪为 MVP 版本 | 未来需要改签名 | 诚实 API，用户不困惑 |
| **推荐：双轨设计** | 需维护两套接口 | 清晰的演进路径 |

**双轨设计示意**：
```csharp
// MVP 专用（诚实）
public interface IWishRegistry { ... }

// 愿景接口（标记为 Future）
[Future("v2.0")]
public interface IWorkspaceScanner { ... }
```

---

### 张力 2: Shape-Layer 双向链接修复 vs Rule-Layer 只报告不修复

**体验设计原则**：**Error as Navigation**——错误信息不仅要诊断问题，还要提供恢复路径。

当前 `MissingBacklink` 包含 `SuggestedValue`，这是好的。但需要补充：
- 具体的修复命令（`docgraph fix-backlinks --apply`）何时可用
- 手动修复的步骤指引

**建议的错误输出**：
```
⚠️ 缺失反向链接
   源: wishes/active/wish-0002.md → atelia/docs/DocGraph/api.md
   期望: api.md 应包含 `ParentWish: W-0002`
   
   手动修复: 编辑 api.md 添加 `parentWish: "W-0002"` 到 frontmatter
   自动修复: [Future] docgraph fix-backlinks --apply
```

---

### 张力 3: Shape-Layer 配置化 vs Rule-Layer 硬编码

**渐进复杂度设计**：

```
         用户旅程
    ─────────────────────────►
    
    零配置        轻配置         全配置
    (MVP)        (v1.x)        (v2.0)
    
    docgraph     docgraph      docgraph
    table        table -c      table -c
                 quick.yaml    advanced.yaml
```

**关键 DX 决策**：硬编码不等于无法扩展。在代码中，硬编码值应该集中在一个 `WishIndexDefaults` 类中，便于未来配置化时替换。

---

## 四、CLI 用户心智模型评估

我用"五秒测试"评估 CLI 设计：用户看到命令列表 5 秒后，能否正确猜测每个命令的作用？

| 命令 | 预期理解 | 实际语义 | 匹配度 |
|:-----|:---------|:---------|:-------|
| `scan <path>` | 扫描目录 | ✅ 扫描并显示统计 | ✅ 高 |
| `links <path>` | 显示链接 | ✅ 检查链接有效性 | ✅ 高 |
| `backlinks <path>` | 显示反向链接？ | 检查双向链接完整性 | 🟡 中 |
| `table <config>` | 生成表格 | ✅ 根据配置生成索引 | ✅ 高 |
| `fix-backlinks` | 修复反向链接 | ✅ 自动补全缺失链接 | ✅ 高 |

**问题点**：`backlinks` 命令名暗示"列出反向链接"，但实际是"检查双向完整性"。

**改进建议**：重命名为 `check-bilinks` 或 `validate-backlinks`，动词更准确。

---

## 五、总结与优先级建议

### 优先修复（阻碍理解）
1. 🔴 P1: 愿景与约束的割裂 → 添加 MVP 约束声明
2. 🔴 P2: IWorkspaceScanner 虚假示能 → MVP 阶段改名或标记
3. 🔴 P3: "失败"语义不明 → 定义 RunResult 类型

### 建议改进（提升体验）
4. 🟡 P4: 链接策略未建模 → 引入 LinkPolicy
5. 🟡 P5: 层级进度解析隐藏 → 显式声明 IWishParser
6. 🟡 P6: 错误码扁平 → 引入分类前缀
7. 🟡 P7: 命令→接口映射不透明 → 添加映射表

### 可选优化（锦上添花）
8. 🟢 P8-P9: 配置错误体验、表格预览

---

#### 结论同步块

**✅ 已定**：
1. **核心问题定位**：Shape-Layer API草案与ore.md不是冲突，而是不同抽象层级（愿景 vs MVP约束）的关系未显式声明
2. **API骨架良好**：接口命名直观、结构合理、错误处理有示能性
3. **关键张力点**：已识别3个核心张力（扫描范围、双向链接处理、配置化程度）
4. **规范缺口**：ore.md §3作为Rule-Layer规范存在完整性缺口，特别是对Wish系统规范的验证覆盖不足

**❓ 未定**：
1. **处理策略**：立即修复文档 vs 先识别问题并规划后续畅谈会
2. **双轨设计**：是否采用MVP专用接口 + 愿景接口的双轨设计
3. **错误处理SSOT**：错误码+schema+失败语义的单一SSOT位置
4. **规范边界**：DocGraph对Wish系统规范的验证覆盖范围

**🔺 待裁决**（需要人类/外部输入）：
1. **优先级决策**：监护人认为哪些问题是最高优先级需要立即解决？
2. **后续畅谈会规划**：是否需要组织专门会议解决特定张力点？

**📋 行动项**（owner / 截止 / 验收）：
1. **Seeker**：将ore.md §3提取为独立Rule-Layer规范草案，补充Requirement IDs
2. **Curator**：为Shape-Layer API草案添加MVP约束声明和[Future]标记
3. **Craftsman**：定义错误处理的单一SSOT（错误码+schema+失败语义）
4. **主持人**：整理待决问题清单，规划后续畅谈会

---
