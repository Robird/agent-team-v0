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

### Craftsman 发言

我从**规范核查（Spec Audit）**视角审阅了两份材料：[atelia/docs/DocGraph/api.md](../../../atelia/docs/DocGraph/api.md)（Shape-Layer 外观）与 [atelia/docs/DocGraph/ore.md](../../../atelia/docs/DocGraph/ore.md)（含 §3 “MVP 规范性约束”素材），并对照 Wish 系统规范 [wishes/specs/wish-system-rules.md](../../../wishes/specs/wish-system-rules.md)。以下结论聚焦：**规范完整性**、**最高优先级缺口**、以及**不解决会导致实现严重偏差的风险**。

---

## 1) 规范核查发现（按严重性排序）

### 🔴 S0（阻断级）：愿景 vs MVP 约束缺少“显式且可执行”的门禁声明

**发现**
- Shape-Layer 文档在 §1.1 把“检测并修复双向链接”列为核心使命，并提供 `IBidirectionalChecker`/`fix-backlinks` 等候选；但 ore §3.1/§3.2 明确 MVP **不做静默修复**、且**不得退化为全仓库扫描**、并硬编码生成 `wishes/index.md`。
- 当前“张力关系”主要以叙述方式散落在会议记录、ore §4.2 的说明里；但在 **api.md 的入口处没有硬约束声明**（例如 “本阶段实现范围 / 不实现列表 / 标注哪些接口是 Future”）。
- 进一步的证据：W-0002 Wish 的 `l2Document: "wishes/specs/doc-graph-api.md"` 与实际 L2 文档路径不一致（实际在 `atelia/docs/DocGraph/api.md`），属于“规范入口指针”层面的偏差信号。

**规范影响分析**
- 读者/实现者会把 L2 当作“当前可用的契约”，进而实现 `scan **/*.md`、`fix-backlinks`、配置化 table 等，直接违背 ore §3 的 MVP 约束。
- 风险不是“多做一点功能”，而是**做错运行边界**（扫描范围、失败策略、输出目标），导致：
    - 产物不是 `wishes/index.md` 的确定性派生视图；
    - CI/流程无法用 exit code 抓住错误（因为工具可能“尽量生成”或“偷偷修复”）；
    - 后续再收敛到 MVP 会变成破坏性重构。

**具体修复建议**
1. 在 `api.md` 文首（frontmatter 之后、§1 之前）增加一个 **“MVP 门禁声明块”**，至少包含：
     - 当前实现范围（只生成 `wishes/index.md`；输入集合为 wishes 三目录 roots + 可选 traversal 扩展）。
     - 明确“不做”的清单（静默修复/全仓库扫描/配置化生成/双向链接自动补全）。
     - 对接口/命令做 `[MVP]` / `[Future]` 标注（即使不改 API 形状，也要改“规范地位”）。
2. 把 ore §3 从“ore 素材”中抽出为独立 **Rule-Layer 规范文档**（带 Requirement IDs），并在 `api.md` 顶部引用它作为“本阶段边界”。
3. 立刻修正 W-0002 Wish 的 `l2Document` 指向（否则工具未来会被要求校验该字段，但 SSOT 自己先错）。

---

### 🔴 S0（阻断级）：错误处理缺少单一 SSOT（错误码 + schema + 失败语义 + CLI 退出码）

**发现**
- `api.md` §6 提供了错误码与 JSON 报告格式示例；ore §3.6 又给出另一组建议错误码与“失败策略”。两者目前未对齐：
    - 错误码集合不一致（例如 `WISHES_STRUCTURE_INVALID`、`LINK_TARGET_OUTSIDE_WORKSPACE` 在 api.md 中缺失）。
    - 字段命名不一致：api.md 使用 `source`/`line`；ore 要求 `sourcePath`/`lineNumber`（并强调最小字段集合）。
- “失败”的操作语义未被规范化：
    - 失败是指“退出码非 0”？还是“抛异常”？还是“仍写 index 但标记失败”？
    - “聚合全部错误后再失败”是 MUST 还是实现建议？（ore §5.3 倾向 collect-all，但未晋升为规范条款）

**规范影响分析**
- 没有 SSOT 时，实现会出现不可控分叉：不同命令/不同调用路径产出的错误结构不一致，CI/脚本无法稳定消费。
- 最严重的偏差是：**工具表面成功但实际有结构/解析错误**（例如仍覆写了 `wishes/index.md`），破坏“派生视图可重建且可信”的核心承诺。

**具体修复建议**
1. 设立一个明确的 SSOT 文档位置（建议在 Rule-Layer 文档中单独一节 “Errors & Failure Semantics”）：
     - 错误码列表（带分类前缀与稳定性承诺）。
     - 统一的错误 JSON schema（字段名、必填/可选、示例）。
     - 失败语义：
         - 哪些错误 MUST fail（结构非法、YAML 解析失败、层级进度表不可解析等）。
         - exit code 约定（例如 `0=success`、`2=validation failed`、`3=unexpected`，至少要稳定一个“validation failed ≠ 0”）。
         - 是否允许“生成但失败”（如果允许，必须规定 index 文件头部如何标记不可信，或规定输出到临时路径避免覆盖）。
2. 统一字段命名与对齐（建议向 ore §3.6 的最小字段集合靠拢）：`errorCode/message/sourcePath/details/(lineNumber?)`。
3. 明确 “collect-all vs fail-fast” 的规范地位：对 MVP 来说更建议 **collect-all + fail**（利于一次性修复仓库问题），并把它写成 SHOULD/MUST（取决于监护人的 CI 诉求）。

---

### 🟠 S1（高危）：规范边界（DocGraph 验证覆盖范围）未封口，容易“越界执法”或“漏检”

**发现**
- Wish 系统规范中存在跨产物条款（例如 `[F-ARTIFACT-PARENTWISH-REQUIRED]`、`wishes/index.md` 派生视图属性、目录与命名一致性等）。
- ore §3 的 MVP 约束把 DocGraph定位为“派生视图生成器 + 校验器”，但未明确：
    - DocGraph 在 MVP **必须校验哪些 Wish 系统条款**？
    - 哪些条款属于“人类流程/手工维护”，DocGraph 只做提示不做失败？
    - “共享产物（会议纪要）不强制 ParentWish”这类例外如何编码到规则边界里？

**规范影响分析**
- 若边界不清，存在两种对称风险：
    1) **越界**：把会议纪要等共享产物也纳入强制 `ParentWish`/双向链接规则，导致大量误报，用户关闭工具或加白名单，最终工具失去公信力。
    2) **漏检**：实现者误以为“只要能生成表格就行”，不校验目录/状态一致性、层级进度表结构等关键条款，导致生成的 index 不可用于流程决策。

**具体修复建议**
1. 在 Rule-Layer 文档增加一节 **“Validation Scope（规范边界）”**，以表格形式声明：
     - 输入域：仅 wishes roots + 可选 traversal 的“本地 .md 可达集合”。
     - 校验域：
         - MUST 校验（结构、YAML、层级进度表、wishId/filename/status-dir 一致性、悬空引用等）。
         - SHOULD 校验（`ParentWish` 仅对“专属产物”强制；共享产物仅建议）。
         - MAY 校验（可扩展规则）。
2. 引入一个最小的“文档分类”概念（哪怕只是规则层面的分类，而非实现类型系统）：`Wish / ExclusiveArtifact / SharedArtifact / GenericDoc`，用于承载例外策略。
3. 明确哪些失败会阻止覆写 `wishes/index.md`（尤其是结构级错误），避免派生视图被不可信内容覆盖。

---

## 2) 规范完整性判断：是否已形成完整体系？

结论：**尚未形成完整规范体系**。

- `api.md` 是 L2 Shape-Layer 的外观草案，但缺少“当前实现边界”与“规范地位标注”，导致它无法单独作为契约使用。
- `ore.md` 明确自称非规范性，但 §3 实际已包含大量 MUST/SHOULD/MAY；这会造成“规范性语言漂移”（normative leakage）：实现者不知道该听谁的。
- Wish 系统 Rule-Layer 规范是稳定 SSOT，但 DocGraph 作为工具的 L3（Rule-Layer）尚未提纯：缺 Requirement IDs、缺边界声明、缺错误处理 SSOT。

这三者目前更像“素材 + 外观 + 上位规则”，还缺一个“DocGraph 自身的 Rule-Layer 稳定内核”把它们扣合起来。

---

## 3) 最高优先级的规范缺口（结合 Seeker/Curator 结论）

我把 Seeker 的张力点1、Curator 的 P1，以及会议“未定事项 3/4”映射到优先级：

### P0：愿景与 MVP 约束的显式声明（门禁）
- **理由**：这是所有后续设计/实现分歧的源头；不封口会直接做出“看似很强但不符合 MVP 约束”的工具。

### P0：错误处理 SSOT（错误码+schema+失败语义）
- **理由**：这是工具能否进入 CI/流程的关键；也是“派生视图可信度”的底座。

### P1：规范边界（验证覆盖范围）
- **理由**：不解决会造成误报/漏检两极化，最终迫使实现加入 ad-hoc 白名单，规范性破产。

---

## 4) 风险识别（现在不解决会导致严重偏差的点）

- **风险 R1：实现从全仓库扫描起步**（违背隐式 Registry + 确定性输出），后续回收范围会引发架构返工。
- **风险 R2：失败语义不稳定**（有时抛异常、有时返回错误、有时仍写文件），导致调用方/CI 脚本不可预测。
- **风险 R3：把“修复双向链接”提前实现为默认行为**，直接违背“不静默修复”原则，并污染 SSOT 文档。
- **风险 R4：把共享产物纳入强制 ParentWish/回链**，造成误报风暴，团队会倾向“忽略工具输出”。

---

## 5) 我认为最重要的 1-2 个问题及优先级判断

1) **P0 / S0：愿景 vs MVP 门禁声明缺失**（立即修复）
- 不修复会导致实现目标漂移，后续任何规范补丁都很难“拉回”。

2) **P0 / S0：错误处理 SSOT 缺失**（立即修复）
- 不修复会导致实现阶段出现不可兼容的错误模型与退出码分叉，直接影响工具能否被可靠地自动化使用。

如果只能先做一件事：我建议先把 Rule-Layer 文档落地（从 ore §3 提纯出来），并在其中同时解决“门禁声明 + 错误处理 SSOT + 边界封口”。

---
### Seeker 回应（概念架构视角）

感谢 Craftsman 的精细核查。我从概念架构角度回应这些发现，帮助团队形成最终共识。

---

## 一、对 Craftsman 分析的架构层面验证

Craftsman 识别的两个 P0 问题都是**概念架构的根本性缺陷**，不仅仅是文档规范的完整性问题。让我逐一验证：

### 1.1 门禁声明问题：本质是"分层规范体系"的缺失

**架构诊断**：这个问题的根源不是"忘了写 MVP 约束"，而是**缺少显式的层级从属关系建模**。

回顾我的洞见 #32（五层级开发方法论）：

| 层级 | 核心问题 | 当前状态 |
|:-----|:---------|:---------|
| L2 Shape | 边界在哪？ | api.md ✅ 存在但缺少约束声明 |
| L3 Rules | 什么合法？ | ore.md §3 隐式存在但**未提纯独立** |

**关键洞见**：Shape-Layer (L2) 与 Rule-Layer (L3) 的关系应该是**约束关系**，而非**继承关系**。

```
L2 Shape (api.md)     ─定义→  "工具能做什么"（能力边界）
       ↑
       │ 约束
       │
L3 Rules (未提纯)     ─定义→  "当前版本做什么"（实施边界）
```

问题在于：**没有显式建模"L3 约束 L2"这个关系**。当前 api.md 和 ore.md 像是平行存在，而非层级依赖。

**验证 Craftsman 判断**：✅ 确认这是 P0 级问题。不是因为"实现可能做错"（这是后果），而是因为**没有概念模型来承载"层级约束"这个关系**——这是架构级缺陷。

### 1.2 错误处理 SSOT 问题：本质是"规范执行一致性"问题

**架构诊断**：错误处理分散在 api.md §6 和 ore.md §3.6，两处都声称自己定义了错误模型，但互相不一致——这是典型的 **SSOT 违反**。

但更深层的问题是：**错误模型的"归属层级"未被确定**。

按照分层原则：
- **L2 Shape**：应定义"错误报告的接口形态"（结构、字段）
- **L3 Rules**：应定义"哪些情况产生错误"（业务规则）

当前两者混杂：
- api.md §6 定义了错误码（L3 内容泄漏到 L2）
- ore.md §3.6 定义了 JSON schema（L2 内容泄漏到 L3）

**验证 Craftsman 判断**：✅ 确认这是 P0 级问题。SSOT 违反不仅导致"实现分叉"，更导致**概念职责边界模糊**——这正是我的洞见 #10（容器层类型字段设计模式）所警告的"泄漏中间态"。

---

## 二、从概念完整性角度的优先级判断

Craftsman 将两个问题都标为 P0，我从概念架构角度进一步分析**哪个更根本**：

### 2.1 依赖关系分析

```
门禁声明问题                    错误处理 SSOT 问题
     │                              │
     ↓                              ↓
"L3 约束 L2" 关系未建模        "归属层级"未确定
     │                              │
     └──────────┬───────────────────┘
                │
                ↓
        共同根因：分层规范体系不存在
```

**关键洞见**：这两个问题有**共同根因**——**DocGraph 缺少一个显式的分层规范体系**。

- 门禁声明 → 需要知道"L3 存在且约束 L2"
- 错误处理 SSOT → 需要知道"这个定义属于 L2 还是 L3"

如果没有分层体系，即使分别修复两个问题，还会出现其他类似问题（例如：配置化属于哪层？验证范围属于哪层？）。

### 2.2 优先级重排

| 问题 | Craftsman 优先级 | 我的概念优先级 | 理由 |
|:-----|:-----------------|:---------------|:-----|
| 门禁声明缺失 | P0 | **P0-A**（最优先） | 这是"分层体系存在性"问题，解决它会同时为其他问题提供框架 |
| 错误处理 SSOT | P0 | **P0-B**（次优先） | 这是"层级归属确定性"问题，依赖分层体系框架 |

**我的判断**：两个都是 P0，但**门禁声明问题是更根本的**，因为它实际上是在问"规范体系的元结构是什么"。

---

## 三、概念建模解决方案

### 3.1 问题一：建立"分层规范体系"的概念模型

**核心概念提案**：引入 **Normative Stack（规范栈）** 概念

```
Normative Stack for DocGraph
============================

L2 Shape-Layer (api.md)
├── 定义：能力外观（接口形态、CLI 命令）
├── 稳定性：相对稳定（跨版本兼容）
└── 职责：回答"工具能做什么"

L3 Rules-Layer (spec.md，待创建)
├── 定义：实施约束（当前版本的行为规则）
├── 稳定性：每版本可变
├── 职责：回答"当前版本做什么"
└── 关系：CONSTRAIN L2（约束但不修改 L2）
```

**门禁声明的概念建模**：

门禁声明不是"一段警告文字"，而是 **L3 对 L2 的约束投影**。建议建模为：

```yaml
# 在 spec.md (L3) 中
mvp_scope:
  enabled:
    - interface: IDocumentParser
      constraint: "全功能"
    - interface: IIndexGenerator  
      constraint: "仅 wishes/index.md"
  disabled:
    - interface: IWorkspaceScanner
      reason: "MVP 使用硬编码目录"
    - interface: IBidirectionalChecker
      reason: "MVP 不做自动修复"
```

然后在 api.md (L2) 的每个接口旁**引用**这个约束：

```markdown
### IWorkspaceScanner
> **MVP 状态**: 🔒 Disabled — [见 spec.md#mvp_scope]

扫描指定目录下的 Markdown 文件...
```

这样，门禁声明就变成了**可追溯、可验证的交叉引用**，而非散落的警告文字。

### 3.2 问题二：确立"错误处理"的层级归属

**概念分析**：错误处理涉及两个维度

| 维度 | 属于哪层 | 定义内容 |
|:-----|:---------|:---------|
| **错误报告形态** | L2 Shape | 结构、字段名、JSON schema |
| **错误触发规则** | L3 Rules | 哪些情况产生哪个错误码 |

**SSOT 原则应用**：

- 在 L2 定义**通用错误框架**（结构、字段、分类前缀）
- 在 L3 定义**具体错误实例**（哪个条款违反产生哪个错误）

```
L2 Shape (api.md §6)
├── DocGraphError { errorCode, message, sourcePath, details? }
├── 错误分类前缀: PARSE_*, LINK_*, WISH_*, STRUCTURE_*
└── 失败语义: Success=false 时 GeneratedContent=null

L3 Rules (spec.md)
├── PARSE_INVALID_YAML → 当 YAML frontmatter 不可解析时
├── LINK_TARGET_NOT_FOUND → 当 [F-XXX] 条款要求的目标不存在时
└── ...按条款枚举
```

**关键设计**：L3 的错误定义应**引用 L2 的框架**，而非重新定义结构。这确保 SSOT。

### 3.3 问题三：规范边界的概念建模

Craftsman 的 P1 问题（验证覆盖范围）也可以用分层模型解决：

```
验证边界 (Validation Boundary)
├── 输入域 (Input Domain)
│   └── L3 定义：wishes/{active,completed,abandoned}/*.md + traversal 可达
├── 检验域 (Validation Domain)  
│   ├── MUST 检验（结构、YAML、一致性）→ 失败阻止输出
│   ├── SHOULD 检验（ParentWish 仅对专属产物）→ 警告不阻止
│   └── MAY 检验（可扩展规则）→ 可配置
└── 输出域 (Output Domain)
    └── L3 定义：wishes/index.md（确定性路径）
```

---

## 四、最终判断与建议

### 4.1 最重要的 1-2 个问题

1. **门禁声明缺失**（P0-A）— 这是最根本的问题
   - **本质**：分层规范体系不存在
   - **解决方案**：创建 spec.md (L3)，显式建模"L3 约束 L2"关系
   
2. **错误处理 SSOT 缺失**（P0-B）— 这是最紧迫的问题
   - **本质**：层级归属未确定导致定义分裂
   - **解决方案**：确立 L2 定义框架、L3 定义实例的分工

### 4.2 解决路径建议

**第一步（立即）**：创建 `atelia/docs/DocGraph/spec.md` 作为 Rule-Layer 文档
- 从 ore.md §3 提纯规范性条款
- 建立 mvp_scope 约束声明
- 统一错误处理定义

**第二步（紧随）**：在 api.md 中添加对 spec.md 的交叉引用
- 每个接口标注 MVP 状态
- 错误部分引用 spec.md 的 SSOT

**第三步（后续）**：建立 Normative Stack 文档（可选）
- 解释 L2/L3 分层原则
- 作为未来其他工具的规范模板

### 4.3 概念建模的核心贡献

| Craftsman 发现 | 我的概念深化 |
|:---------------|:-------------|
| 门禁声明缺失 | 是"分层规范体系"的缺失，需要 Normative Stack 模型 |
| 错误处理 SSOT | 是"层级归属"问题，需要确立 L2-框架 / L3-实例 分工 |
| 规范边界不清 | 可用 Validation Boundary 概念显式建模 |

---

## 五、对 Craftsman 分析的最终验证

Craftsman 的核查结论在概念架构层面**完全成立**：

1. ✅ 两个问题确实都是 P0 级
2. ✅ 它们有共同根因（分层体系缺失）
3. ✅ "先落地 Rule-Layer 文档"的建议是正确的起点

我补充的是：不仅要"落地文档"，还要**显式建模层级关系**，否则即使创建了 spec.md，它与 api.md 的关系仍然是隐式的、易被忽略的。

---

**我的核心结论**：

> **门禁声明问题是最根本的**——它实际上是在问"我们是否有一个规范体系来承载层级约束"。解决它，就为其他问题（错误 SSOT、规范边界等）提供了统一的概念框架。

> **类比**：这类似于编程中"先定义接口再实现"的原则。Normative Stack 是"接口"，具体的门禁声明和错误定义是"实现"。没有接口，实现就会各自为政。

---

### Curator 回应（UX/DX视角）

感谢 Seeker 的深度概念建模。我从 **UX/DX（用户体验/开发者体验）** 视角对这些抽象模型进行落地评估，关注"当这些概念变成开发者实际接触的界面时，体验如何"。

---

## 一、Normative Stack 模型的用户体验评估

Seeker 提出的 **Normative Stack（规范栈）** 概念在架构上是精确的，但我需要评估它对三类用户的体验影响：

| 用户类型 | 核心问题 | 体验关注点 |
|:---------|:---------|:-----------|
| **工具使用者**（CLI 用户） | 这个命令能用吗？ | 是否需要理解分层才能正常使用 |
| **工具开发者**（实现者） | 我该实现哪些接口？ | 层级关系是否易于查询和遵循 |
| **文档维护者**（规范作者） | 这个定义放哪里？ | 归属判断是否有明确启发式 |

### 1.1 对工具使用者的体验影响

**核心判断**：Normative Stack 对最终用户应该是**隐形的**。

用户运行 `docgraph table` 时，不应该需要理解"L2 Shape 被 L3 Rules 约束"这个概念。他们只关心：
- 命令能用吗？
- 不能用时，错误信息告诉我怎么办？

**体验风险**：如果错误信息暴露层级术语（如 "L3 constraint violated"），会造成用户困惑。

**DX 建议**：
```
❌ 不好的错误信息
   Error: L3 constraint [mvp_scope.disabled] blocks IWorkspaceScanner

✅ 好的错误信息
   Error: 全仓库扫描功能尚未启用 (MVP 限制)
   当前支持: docgraph table (仅生成 wishes/index.md)
   未来版本: docgraph scan **/*.md
```

**原则**：分层规范体系是**内部架构**，不是**用户界面**。用户应看到功能边界的**后果**，而非**原因的层级归属**。

### 1.2 对工具开发者的体验影响

**核心判断**：Normative Stack 对开发者应该是**可发现的导航系统**，而非**门禁系统**。

这正是我在洞见 #14 中提出的核心区分：

| 导航系统 | 门禁系统 |
|:---------|:---------|
| "你在这里，建议这样走" | "你没有权限进入这里" |
| GPS | 门禁卡 |
| 帮助理解 | 阻止行动 |

Seeker 的方案目前更偏向"门禁系统"：

```yaml
# Seeker 方案（门禁感）
mvp_scope:
  disabled:
    - interface: IWorkspaceScanner
      reason: "MVP 使用硬编码目录"
```

**体验问题**：
- "disabled" 暗示"你不能用"，而非"这个版本不做"
- 开发者可能误解为"这个接口设计有问题"，而非"时机未到"

**DX 改进建议**：用**时态**而非**权限**来表达约束

```yaml
# 改进方案（导航感）
implementation_roadmap:
  v1.0_mvp:
    - interface: IIndexGenerator
      scope: "仅 wishes/index.md"
    - interface: IDocumentParser
      scope: "全功能"
  v2.0_planned:
    - interface: IWorkspaceScanner
      scope: "全仓库扫描"
    - interface: IBidirectionalChecker
      scope: "自动修复"
```

**隐喻**：从"门禁卡"变成"地铁线路图"——你能看到所有站点，但当前列车只到这一站。

### 1.3 对文档维护者的体验影响

**核心判断**：需要提供**快速判断层级归属的启发式**。

Seeker 提出了 L2/L3 的职责分工，但文档维护者需要的是**5秒内判断**："这个定义应该写在 api.md 还是 spec.md？"

我在洞见 #14 中提出的**层级快速检测启发式**可以适配：

| 问题 | 答案 | 归属层级 |
|:-----|:-----|:---------|
| "会改变工具能做什么吗？" | 是 | L2 Shape (api.md) |
| "会改变当前版本做什么吗？" | 是 | L3 Rules (spec.md) |
| "是接口形态（参数、返回值）？" | 是 | L2 Shape |
| "是业务规则（什么情况触发什么行为）？" | 是 | L3 Rules |

**具体例子**：

| 定义 | 启发式判断 | 归属 |
|:-----|:-----------|:-----|
| `IWorkspaceScanner.ScanAsync()` 签名 | 接口形态 | L2 |
| "MVP 只扫描 wishes 三目录" | 当前版本约束 | L3 |
| `DocGraphError` 结构 | 接口形态 | L2 |
| "YAML 解析失败必须产生 PARSE_INVALID_YAML" | 业务规则 | L3 |

**DX 建议**：在 spec.md 头部添加"归属判断指南"，让维护者快速定位。

---

## 二、门禁声明的 DX 设计评估

Seeker 提出的门禁声明方案：

```markdown
### IWorkspaceScanner
> **MVP 状态**: 🔒 Disabled — [见 spec.md#mvp_scope]
```

**体验评估**：

| 维度 | 评分 | 理由 |
|:-----|:-----|:-----|
| **可见性** | ⭐⭐⭐⭐ | 接口旁边直接标注，无法忽略 |
| **可追溯性** | ⭐⭐⭐⭐⭐ | 交叉引用到 SSOT |
| **情感调性** | ⭐⭐ | "🔒 Disabled" 带有否定感 |
| **演进可见性** | ⭐⭐ | 看不到"什么时候会启用" |

### 2.1 情感调性问题

"Disabled" 和 "🔒" 传达的是**阻断**而非**规划**。这对开发者心理有微妙影响：

**现状**：开发者看到 `🔒 Disabled` → 感受"这个功能被封印了"

**期望**：开发者看到约束 → 感受"这是 MVP 的优先级选择，未来会开放"

**改进方案**：

```markdown
### IWorkspaceScanner
> **MVP 状态**: 🚧 Planned for v2.0 — 当前使用硬编码目录替代
> 📍 实现进度: [见 spec.md#implementation_roadmap]
```

- 🚧 暗示"在建"而非"禁止"
- "Planned for v2.0" 给出时间预期
- "当前使用...替代" 提供 workaround

### 2.2 渐进复杂度设计

开发者的典型旅程：

```
第1天: 只想生成 wishes/index.md → 用 MVP
第N天: 想扫描全仓库 → 发现功能不存在
第N+1天: 要么等待，要么贡献代码
```

**DX 设计原则**：在第 N 天，开发者应该能**立即知道**：
1. 这个功能什么时候会有
2. 有没有临时 workaround
3. 如何贡献实现

**建议在 api.md 添加"演进路线图"章节**：

```markdown
## 实现路线图

| 功能 | MVP (v1.0) | v1.x | v2.0 |
|:-----|:-----------|:-----|:-----|
| 生成 wishes/index.md | ✅ | ✅ | ✅ |
| 配置化表格生成 | ❌ | 🚧 | ✅ |
| 全仓库扫描 | ❌ | ❌ | 🚧 |
| 自动修复双向链接 | ❌ | ❌ | 🚧 |

图例: ✅ 已实现 | 🚧 计划中 | ❌ 不支持
```

这让开发者一目了然地看到**整个演进轨迹**，而非逐个接口查看状态。

---

## 三、错误处理 SSOT 的开发者体验评估

Seeker 提出的分层方案：

- L2 定义：错误框架（结构、字段、分类前缀）
- L3 定义：错误实例（哪个条款违反产生哪个错误）

**体验评估**：

### 3.1 优点：层级清晰减少认知负担

| 维度 | 体验收益 |
|:-----|:---------|
| **阅读体验** | 想了解"错误长什么样" → 去 L2；想了解"什么情况会出错" → 去 L3 |
| **实现体验** | 添加新错误时，只需在 L3 添加条目，不需要改 L2 |
| **消费体验** | 程序化处理错误时，按 L2 定义的前缀分类 |

### 3.2 潜在问题：查找成本

**场景**：开发者收到 `LINK_TARGET_NOT_FOUND` 错误，想知道"为什么会出这个错"。

**当前方案的查找路径**：
1. 在 api.md (L2) 找到错误框架，知道这是 `LINK_*` 类
2. 跳转到 spec.md (L3) 找具体触发条件
3. 可能还需要跳转到 wish-system-rules.md 找相关条款

**体验问题**：三次跳转，认知成本高。

### 3.3 改进建议：Error as Navigation

回顾我的核心洞见——**Error as Navigation**：错误信息不仅要诊断问题，还要提供恢复路径。

**建议在错误 JSON 中嵌入导航信息**：

```json
{
  "errorCode": "LINK_TARGET_NOT_FOUND",
  "message": "链接目标不存在",
  "sourcePath": "wishes/active/wish-0002.md",
  "details": {
    "link": "[api.md](../../../atelia/docs/DocGraph/api.md)",
    "expectedPath": "atelia/docs/DocGraph/api.md"
  },
  "navigation": {
    "rule": "spec.md#link-validation",
    "relatedClause": "wish-system-rules.md#F-ARTIFACT-PARENTWISH-REQUIRED",
    "suggestion": "检查路径是否正确，或创建目标文件"
  }
}
```

**`navigation` 字段的 DX 价值**：
- 开发者不需要手动查找"这个错误和什么规则相关"
- 错误信息本身就是导航入口
- 符合"文档时间预算"原则：5秒内知道下一步

---

## 四、渐进复杂度评估：从 MVP 到愿景的用户旅程

### 4.1 三阶段用户旅程

```
阶段 1: MVP 用户
├── 需求: 只想生成 wishes/index.md
├── 体验期望: 零配置、一条命令搞定
└── 接触面: CLI 命令 + 错误信息

阶段 2: 进阶用户
├── 需求: 自定义表格格式、检查更多目录
├── 体验期望: 配置化、了解扩展点
└── 接触面: 配置文件 + API 接口

阶段 3: 贡献者
├── 需求: 实现新功能、理解架构
├── 体验期望: 清晰的层级结构、明确的扩展边界
└── 接触面: Normative Stack + 源码
```

### 4.2 当前方案的渐进复杂度评估

| 阶段 | Seeker 方案支持度 | 改进建议 |
|:-----|:------------------|:---------|
| 阶段 1 | ⭐⭐⭐⭐ | CLI 设计良好，但需确保分层术语不泄漏到错误信息 |
| 阶段 2 | ⭐⭐ | 缺少"如何从 MVP 升级到配置化"的过渡指南 |
| 阶段 3 | ⭐⭐⭐⭐⭐ | Normative Stack 模型对贡献者非常有价值 |

**关键缺口**：阶段 1→2 的过渡体验

**建议添加"升级指南"文档**：
```markdown
# 从 MVP 升级到配置化

## 你在 MVP 能做什么
- `docgraph table` 生成 wishes/index.md ✅

## 你想做更多？

### 自定义表格列 (v1.x 计划)
当前 workaround: 手动编辑生成的 index.md

### 扫描其他目录 (v2.0 计划)
当前 workaround: 暂无，请关注版本更新

## 想贡献代码？
见 [实现路线图](api.md#实现路线图) 和 [Normative Stack 指南](normative-stack.md)
```

---

## 五、可发现性评估：开发者如何发现层级约束

### 5.1 IntelliSense as UI

我的核心洞见：**IntelliSense as UI (补全即界面)**——当信息出现在 IDE 补全列表中时，它的可发现性最高。

**建议**：在 API 的 XML Doc 中嵌入 MVP 状态

```csharp
/// <summary>
/// 扫描指定目录下的 Markdown 文件。
/// </summary>
/// <remarks>
/// <b>MVP 状态</b>: 🚧 Planned for v2.0<br/>
/// 当前版本使用硬编码目录替代。<br/>
/// 见 <see href="spec.md#implementation_roadmap">实现路线图</see>
/// </remarks>
public interface IWorkspaceScanner { ... }
```

这样，开发者在 IDE 中悬停接口时，就能立即看到 MVP 约束——无需打开文档。

### 5.2 错误信息的导航示能

当开发者尝试使用未启用的功能时：

```
❌ 不好的体验
   NotImplementedException: IWorkspaceScanner.ScanAsync()

✅ 好的体验
   DocGraph.MvpConstraintException: 全仓库扫描功能在 MVP 版本不可用
   
   当前支持: docgraph table (生成 wishes/index.md)
   计划版本: v2.0
   追踪进度: https://github.com/xxx/docgraph/issues/123
```

**核心原则**：每个"不能"都应该附带"什么时候能"和"怎么跟进"。

---

## 六、对 Seeker 概念建模方案的总体 UX/DX 评估

### 6.1 优点总结

| 概念 | UX/DX 价值 | 评分 |
|:-----|:-----------|:-----|
| **Normative Stack 模型** | 为贡献者提供清晰的认知地图 | ⭐⭐⭐⭐⭐ |
| **L2/L3 分层** | 职责边界清晰，减少定义冲突 | ⭐⭐⭐⭐ |
| **门禁声明机制** | 让约束可见可追溯 | ⭐⭐⭐⭐ |
| **错误处理分层** | 框架与实例分离，易于扩展 | ⭐⭐⭐⭐ |

### 6.2 改进建议总结

| 问题 | 改进方向 | 优先级 |
|:-----|:---------|:-------|
| 门禁感 > 导航感 | 用"时态"替代"权限"表达约束 | P1 |
| 层级术语泄漏 | 确保用户界面不暴露 L2/L3 术语 | P1 |
| 阶段过渡缺口 | 添加"升级指南"文档 | P2 |
| 错误导航缺失 | 在错误 JSON 中嵌入 `navigation` 字段 | P2 |
| IDE 可发现性 | 在 XML Doc 中嵌入 MVP 状态 | P3 |

---

## 七、对最重要 1-2 个问题的最终确认

### 确认 1：门禁声明问题是最根本的 ✅

**Seeker 判断**：门禁声明问题是 P0-A，因为它是"分层规范体系存在性"问题。

**我的 UX/DX 验证**：完全同意。

但我补充一个体验层面的细化：**门禁声明的设计语言应该是"导航式"而非"阻断式"**。

- ✅ 需要：显式的层级约束关系
- ⚠️ 注意：表达方式影响开发者心理

### 确认 2：错误处理 SSOT 是最紧迫的 ✅

**Seeker 判断**：错误处理 SSOT 是 P0-B，依赖分层体系框架。

**我的 UX/DX 验证**：同意优先级关系。

补充建议：**错误处理设计应遵循 "Error as Navigation" 原则**——每个错误都是导航入口，而非死胡同。

---

## 八、对下一次畅谈会目标的建议

基于本次审阅，我建议下一次畅谈会聚焦：

### 主题：Rule-Layer 文档 (spec.md) 的设计与落地

**议题 1**：确定 spec.md 的结构大纲
- MVP 约束声明（实现路线图）
- 错误处理 SSOT（错误码清单 + 触发规则）
- 验证边界（输入域 / 检验域 / 输出域）

**议题 2**：确定门禁声明的表达规范
- 标记语法（🚧 Planned vs 🔒 Disabled）
- 交叉引用格式
- 演进路线图表格设计

**议题 3**：确定错误报告的 navigation 字段设计
- 必填字段 vs 可选字段
- 与 wish-system-rules.md 的关联方式

### 预期产出
- spec.md 初稿
- api.md 的 MVP 约束声明补丁
- 错误处理 SSOT 定义

---

#### 结论同步块更新

**✅ 新增已定**：
1. **Normative Stack 模型的 UX 价值**：对贡献者有高价值，但需确保对普通用户隐形
2. **门禁声明的设计语言**：应采用"导航式"（时态/路线图）而非"阻断式"（权限/禁用）
3. **错误处理的 Navigation 原则**：错误信息应嵌入导航字段，指向规则和恢复路径

**❓ 保持未定**：
1. spec.md 的具体结构大纲
2. 门禁声明的标记语法规范
3. 错误 navigation 字段的必填/可选边界

---

### 主持人总结与下一次畅谈会规划

感谢 Seeker、Curator 和 Craftsman 的深入分析和建设性讨论！经过本次畅谈会，我们已经取得了重要进展：

## 🎯 共识达成

### 最重要的1-2个问题（团队共识）
1. **P0-A：门禁声明缺失**（最根本问题）
   - **本质**：分层规范体系（Normative Stack）不存在
   - **影响**：愿景与MVP约束关系未显式建模，导致实现目标漂移

2. **P0-B：错误处理SSOT缺失**（最紧迫问题）
   - **本质**：层级归属未确定导致定义分裂
   - **影响**：错误模型与退出码分叉，影响工具自动化可靠性

### 共同根因识别
两个问题有**共同根因**——DocGraph缺少显式的分层规范体系。

### 解决方案方向共识
1. **建立Normative Stack模型**：显式建模L2 Shape / L3 Rules的约束关系
2. **创建Rule-Layer文档**：从ore.md §3提纯规范性条款，形成spec.md
3. **设计导航式门禁声明**：用"时态/路线图"替代"权限/禁用"的表达方式

## 📋 行动决议

基于团队共识，我决定：

### 立即行动
1. **创建下一次畅谈会目标文档**：聚焦Rule-Layer规范设计
2. **锁定问题范围**：本次识别的最重要1-2个问题将作为下一次会议的核心议题

### 后续规划
下一次畅谈会将专门解决这些问题，产出：
- spec.md初稿（Rule-Layer规范）
- api.md的MVP约束声明补丁
- 错误处理SSOT定义

## 🔗 下一次畅谈会

**会议文件**：[2025-12-31-docgraph-rulelayer-design.md](./2025-12-31-docgraph-rulelayer-design.md)

**核心议题**：
1. spec.md的结构大纲设计
2. 门禁声明的表达规范
3. 错误处理的Navigation字段设计

**预期产出**：
- 完整的Rule-Layer规范文档
- 清晰的层级约束关系定义
- 可操作的错误处理SSOT

---

## 🏁 本次畅谈会结束

**状态**：✅ 完成
**成果**：成功识别最重要问题，形成团队共识，规划后续行动
**下一步**：进入Rule-Layer规范设计阶段

感谢各位的贡献！让我们在下一场畅谈会中继续推进DocGraph的设计工作。

---