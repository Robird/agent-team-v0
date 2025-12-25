---
archived_from: agent-team/members/Advisor-GPT/index.md
archived_date: 2025-12-25
archived_by: Advisor-GPT
reason: 过程性长记录迁出主记忆文件；保留可追溯原文
original_section: "最后更新" 后的长块引用（AI Team 协作协议/命名/条款可寻址性等）
---

# AI Team 协作协议 / 命名审计 — 原始记录（归档）

> 说明：本文件保留从主记忆文件迁出的长块引用，供追溯与核对。主文件仅保留提纯后的洞见与索引入口。

## 2025-12-21（原始记录）

> **2025-12-21 条款可寻址性：稳定语义锚点（Semantic Anchor）命名纪律**
>
> - 位置型编号（如 `[S-17]`）适合作为“阅读索引”，但不适合作为跨文档/测试/代码的稳定引用键；推荐用稳定语义锚点（如 `[S-OBJECTID-RESERVED-RANGE]`）作为机器可读 key。
> - 锚点名优先选择“可测试的语义维度”（字段名/不变式/错误策略），避免把 MUST/SHOULD 写进锚点名（规范级别会变，但语义锚点应保持稳定）。
> - 命名长度以 2-4 个词（不含 `F-/A-/S-/R-` 前缀）更利于 grep 与测试名映射；必要时用术语化缩写（如 `CRC32C`、`PTR64`），但避免引入第二套缩写词表。

> **2025-12-21 StateJournal 命名与归属：品牌名/技术名分层 + NuGet/namespace 的“不可逆成本”**
>
> - “是否改名”建议分两层：**品牌名（package/repo）**可相对稳定以降低外部迁移成本；**技术名（public API 类型/组件名）**必须对齐真实语义，避免 `Heap` 暗示随机访问/allocator 语义。
> - NuGet `PackageId` 与 public `namespace` 属于高不可逆资产：不建议用 `*V2/*2` 这类临时后缀固化在包名；更稳妥用 **SemVer major bump** 表达语义断裂，并把格式版本放到 `FormatVersion`（与 API version 解耦）。
> - Repo 归属优先按依赖方向与发布节奏裁决：底层存储不应依赖 UI（避免层级反转）；若主要服务 Agent runtime/历史存储，放进 atelia 往往更自然；独立 repo 仅在明确存在跨生态消费者与“协议化”目标时才值得。

> **2025-12-21 命名投票经验：优先“稳定语义/用例名”，把实现术语留在内部**
>
> - 当底层实现仍在演进（diff 编码、对象图布局、GC/compaction 策略）时，repo/package 的名字更应锚定“对外不变的语义与用例”（如 State + Journal/Append-only），避免把短期实现细节（如 DeltaGraph）固化为外部心智模型。
> - 实现导向名可以作为内部组件/子模块名（codec、index、graph 等），但对外优先选“使用者第一眼就能预测行为边界”的命名。

> **2025-12-21 命名投票复盘：把“稳定语义”写成可迁移的共识**
>
> - 论证策略：优先用“对外不变的语义边界”取代“实现机制名”。例如 `StateJournal` 直接承诺“状态 + 追加写 + 版本链/可回放”，比 `Heap`/`Store` 更不容易随实现演进而过时。
> - 迁移动作：同步明确旧文档目录已删除、新路径 `atelia/docs/StateJournal/` 生效，并把 namespace 钉死为 `Atelia.StateJournal`，避免后续出现“文档已迁/代码仍旧名”的二次漂移。
> - 团队协作经验：投票理由要求一句话且能映射到后续 PR checklist（链接替换、命名空间、入口文档与 backlog），可把“偏好争论”收敛成可执行的迁移清单。

> **2025-12-21 AI Team 元认知：Subagent 命名 grammar 与 runSubagent 的可审计字段**
>
> - 命名优先级：先保证“可机械解析”，再考虑好听；推荐 grammar：`<Role>-<Model>`，Role 取自受控词表（Advisor/Reviewer/Tester/Facilitator），Model 取自 {Claude/Gemini/GPT}（或更具体版本）。避免把“项目归属/品牌前缀”混进同一层命名，降低歧义与迁移成本。
> - 研讨会形式合并要避免“只有语气没有契约”：建议 `#review/#design/#decision` 三类标签绑定最低产物（FixList / 候选方案+tradeoff / ADR+回滚条件），否则讨论难以复用与审计。
> - `runSubagent` 的邀请信息应补齐可审计字段：`chatroomFile`、`targetFiles`、`appendHeading`、`scope`、`outputForm`、`language`（MUST）；`existingConsensus/openQuestions/timebox/verificationStep`（SHOULD）。其中 `verificationStep`（调用方与子代理都声明“已追加到文件末尾”）能显著减少“插入中间/写错文件”的返工。

> **2025-12-21 畅谈会 Round 2：AGENTS.md 协议收口的“唯一规范”原则（标题与调用卡片）**
>
> - 协议里最忌“同时鼓励两套写法”：例如 `### <Name> 发言` 与 `### <Name> 第 N 轮`。必须二选一收口（另一种只能作为可选元信息），否则索引/检索/工具化会立刻分叉。
> - `runSubagent` 的字段命名应定义一套 **canonical keys** 作为 SSOT，避免 `chatroomFile/聊天室文件`、`appendHeading/发言标题` 等同义词漂移；同理，`targetFiles` 的类型（list vs single）与示例写法必须一致。
> - 调用格式建议以“结构化字段”作为 SSOT（fenced YAML 或逐行 Key: Value）；Markdown 表格可作为人类友好渲染，但不宜承载协议参数（对齐/换行导致解析与复制粘贴不稳）。
> - Role 表示长期稳定身份（如 `Advisor-*`），具体任务用 `taskTag` 表达；不要把 `-Design` 这类语境性后缀烧进名字里，避免边界不清与过度设计。
