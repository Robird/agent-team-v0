---
archived_from: agent-team/members/Advisor-GPT/index.md
archived_date: 2025-12-25
archived_by: Advisor-GPT
reason: 过程性长记录迁出主记忆文件；保留可追溯原文
original_section: "最后更新" 后的长块引用（StateJournal/RBF/DurableDict 等）
---

# StateJournal / RBF / DurableDict — 审计与评审原始记录（归档）

> 说明：本文件保留从主记忆文件迁出的长块引用，供追溯与核对。主文件仅保留提纯后的洞见与索引入口。

## 2025-12-16 to 2025-12-21（原始记录）

> **2025-12-16 StateJournal：varint 与“可跳过性”的边界条件**
>
> - “zero-copy”需要拆开看：对 `string/blob` 的 payload bytes 可以做到 zero-copy；对 `int` 这类标量必然要 decode（CPU 计算），不应把“必须解码”与“复制 payload bytes”混为一谈。
> - `varint` 的关键代价不是“复制”，而是 **失去 O(1) 随机定位能力**：一旦把 offset/length table 做成变长，就会迫使 wrapper 扫描才能定位成员边界，破坏 lazy random access。
> - “先写 data 再写 length（且不回填 header）”会让记录从 Ptr 出发无法确定尾部位置，从而无法局部跳过 record、也无法只读 header 就建立惰性索引；MVP 更稳妥是 **Header+Footer 都有 TotalLen**（写完回填 header），footer 同时用于尾部回扫与损坏检测。

> **2025-12-20 StateJournal Decision Clinic：Detached 对象与“编码名/语义名”分层的必要性（规范审计）**
>
> - 当对象生命周期存在 “Detach（从 Dirty Set 移除强引用）” 分支时，必须把“仍可访问”的行为钉死为 **确定性契约**：要么 `MUST throw ObjectDetachedException`，要么 `MUST reset to Genesis/Empty 并保持可继续使用`；否则会产生实现分叉且难以写出可判定测试。
> - `Ptr64` 作为 wire-format 名称如果同时被读者理解为“指向 record 起点”，会与 `DataTail = EOF` 这类字段冲突；更稳妥的分层是：`Ptr64` = **4B 对齐 file offset 的编码**（`u64 LE`，`0=null`），`Address64`/`ObjectVersionPtr` = **语义子集：必须指向 record 起点**。这样既不引入新术语，也能把校验规则写到正确的层级。

> **2025-12-20 规格共识形成：P0/P1/P2 投票清单是“可审计收敛器”**
>
> - 以“是否会造成实现分叉/一致性风险”为 P0 标准，把争论从偏好（命名洁癖/风格）拉回到可验证的行为契约（失败语义、边界行为、分层定义）。
> - 投票条目应该写成“可落地修订动作”（可直接映射到文档 diff），并要求每票一句理由；这让共识可回溯，也方便后续把条目变成 PR checklist。
> - 对规范类文档，先锁死“提交点/失败不改内存/NotFound 行为/新建对象生命周期”等条款，再做术语与可读性清洁，能显著减少后续重写成本。

> **2025-12-19 DurableDict：术语消歧的“禁用模糊词”经验（QC 记录）**
>
> - 即便在文档中提供了“术语映射”（例如把“内存态”映射到 Working State/ChangeSet），仍建议在规范段中**彻底禁用**这类跨层模糊词：它会在其他章节再次引入歧义，并削弱读者对三层语义边界的信心。
> - “Materialize（过程）→ committed state（结果）”与“Materialized State（名词）”极易发生术语碰撞；更稳妥的写法是把对外语义状态称为 Working/Current State，把 materialize 的结果明确称为 Committed Baseline/Committed State。

> **2025-12-20 StateJournal MVP v2：规范可实现性审计（关键坑位清单）**
>
> - `RBF reverse scan` 的空文件边界判定存在 off-by-4 风险：若定义 `MagicPos = FileLength-4` 且 `RecordEnd = MagicPos`，则“仅 `[Magic]`”时应判定 `RecordEnd == 0` 而非 `4`。
> - `Dirty Set` 若只存 `ObjectId` 不能防 GC，无法满足“防丢改动”的动机；必须强引用对象实例（或引入独立强引用表）。
> - `RecordKind` 在 data/meta 两域都用 `0x01` 时必须声明“域隔离”，否则实现者很容易误用单一枚举表。
> - `ulong` 作为值类型与 `Val_VarInt(ZigZag)` 不匹配：要么新增 `Val_VarUInt`，要么收紧值类型为有符号。

> **2025-12-16 StateJournal：纯 C# span-view vs C/FFI 低层的权衡**
>
> - `ReadOnlySpan<byte>` 的“临时构造”本质是（ptr,length）两个字段的轻量视图，通常可被 JIT 内联/消除；真实成本更多来自“映射视图切换/边界检查/分支”，而不是 span 本身。
> - 纯 C# 方案的关键是把 `unsafe` 收口在“AcquirePointer + span 建立”的边界层，其余解析用 `BinaryPrimitives/MemoryMarshal` 走安全代码；可获得接近 C 的吞吐且大幅降低跨平台/构建复杂度。
> - C/FFI 方案更适合当你需要：多语言复用同一套解析、极致性能（减少边界检查/更自由的指针运算）、或需要复用既有 C 生态；代价是 ABI/部署/调试复杂度显著上升，并引入 native 崩溃面。

> **2025-12-17 StateJournal：把“可实现性”写进格式——CRC32C + DataTail + SortedDict 写入算法**
>
> - 校验应明确到“具体变体”：CRC32C（Castagnoli）比泛称 CRC32 更可实现/更一致，且 .NET 有 `System.IO.Hashing.Crc32C` 可直接复用。
> - DataTail 作为必填字段能显著简化恢复：恢复路径从“尾扫推断”降维到“truncate 到 DataTail”，尾扫仅保留为诊断工具。
> - SortedDict 选择“排序写入 + 二分查找”能减少读路径分支；关键是把写入算法说清：header 后先 PadTo4，再写 ObjHeader/预留并回填 EntryTable，ValueData 写入时记录 `ValueOffset32` 并保持 4B 对齐，最后按 TotalLen→CRC32C 的 finalize 顺序。

> **2025-12-19 DurableDict：ChangeSet 语义审计——术语边界与不变式优先**
>
> - “三层语义”一旦写死（Materialized State / ChangeSet / On-Disk Diff），文档中任何一句“内存态用 tombstone”都会造成规范自相矛盾；必须把“tombstone 属于 ChangeSet 的内部表示”与“materialized state 的对外语义=删除即不存在”明确拆开。
> - 方案评估要以规范约束为准：只要 tombstone 出现在可枚举的 working/materialized state，就会把过滤责任扩散到所有读 API，并高概率引入 `Count/枚举/ContainsKey` 不一致。
> - 维护性与可测性优先看不变式集中度：`Upserts+Deleted`（B）把不变式集中在“集合互斥 + 读合并 + commit 压缩”；双字典 diff（C）把语义收敛为“最终状态为真相”，但需要明确 dirty tracking 与 diff 的覆盖面（包含删除）。
> - 建议用 property tests 固化：最后写赢、Count/枚举一致性、删除重放一致性、以及序列化格式不变式（keys 升序/无重复/delta 可还原）。

> **2025-12-19 DurableDict：把 Q1/Q2/Q3 收敛为“可写进规范”的条款（Commit/Dirty/Diff Canonicalization）**
>
> - Q1：Commit 成功后必须满足 `CommittedState == CurrentState`（语义相等）且二者对后续写入**逻辑隔离**；实现上允许深拷贝/不可变结构共享/COW，但禁止“交换引用后把 working 清空”的惊讶语义。
> - Q2：MVP 至少需要 `HasWritesSinceCommit`/`HasChanges` 作为 $O(1)$ fast-path；dirty-keys 仅作为可选性能优化，避免过早复杂化。
> - Q3：要求 **Canonical Diff**：仅当 committed 与 current 在该 key 上语义不同才输出 diff；“新增后删除/设置回原值”等 net-zero 变更必须被消除，避免幽灵 tombstone 污染版本历史。
> - 文档措辞治理：禁止用“内存态”这种跨层词；固定三词：Working/Materialized State、ChangeSet/Write-Tracking、On-Disk Diff，并明确“哨兵对象/tombstone 只能属于 ChangeSet 内部表示”。

> **2025-12-19 StateJournal：FlushToWriter 的两阶段语义（避免“假提交”）**
>
> - 若将 `FlushToWriter` 定义为“对象级：计算 diff 并写入 writer（非提交）”，它就不应在成功写入后立刻更新 `_committed` 或清空 dirty；否则当 heap 级 commit 在后续步骤（例如写 meta commit record / fsync）失败时，内存会出现“看似已提交但磁盘未提交”的假提交状态，违反“Commit 失败不改内存”。
> - 更稳妥的落点是两阶段：`FlushToWriter` 仅产生/写出 DiffPayload（可视为 prepare），heap 级 commit point 成功后再统一回调 `OnCommitSucceeded()`（或批量 `FinalizeCommit()`）来更新 `_committed`、清空 ChangeSet/Dirty Set；失败则不触碰内存状态并允许 retry。

> **2025-12-19 StateJournal：RBF framing 的“Magic 哨兵/分隔符”歧义与 reverse-scan 校验点（规范审计）**
>
> - 发现潜在 P0 规格歧义：`Magic` 既被描述为 record header 字段，又被描述为文件级 separator/尾部哨兵；若不收口会导致 reverse scan 的 `MagicPos`/`End` 语义分叉。
> - 如果采用“record header 的 Magic + 文件末尾孤立 Magic 哨兵”的方案，reverse scan 的下一轮边界应更新为 `MagicPos = Start`（当前 record 的起点也是前一条 record 的 End），而不是 `Start - 4`；否则会落入上一条 record 的 CRC 字段。
> - `DataTail` 必须精确定义是否包含尾部哨兵（更推荐 `DataTail = EOF` 包含哨兵），否则恢复 truncate 会破坏“文件以 Magic 结束”的不变量。

> **2025-12-19 StateJournal：规范审计结论——命名/链接/示例一致性是“可开工”门槛**
>
> - 文档若宣称“概念层/编码层/实现层分离”，则必须把实现标识符（如 `_current/_committed`）收口到 Implementation Mapping；否则规范会被 reference implementation 绑死，后续演进难以审计。
> - 格式规范（Markdown）里“相对链接可达性”属于硬质量门槛：例如从 `atelia/docs/StateJournal/` 指向仓库根 `atelia/` 的链接若写错，会让读者无法验证关键实现提示，降低规范可信度。
> - 伪代码必须与 commit point 一致：只要 meta commit record 是对外可见点，对象级 `FlushToWriter` 就必须是 prepare-only；否则会出现“假提交”，并在失败重试时丢失 dirty 信息。

> **2025-12-20 “瘦身悖论”复盘：行数不是目标，SSOT + 可测试闭环才是**
>
> - “瘦身”若以行数/字数衡量，会与规范工程的核心目标冲突；真正可度量的目标应是：冗余度（同一事实/约束是否双写）、可判定性（无需作者意图即可判真伪）、可追踪性（条款 ID ↔ 测试向量/失败注入）。
> - 元数据基础设施（条款编号、索引、映射）通常是必要增量；“新冗余”来自“双写”：结构化表述与自然语言复述并存、Index 与 ADR 边界不清、图表与线性定义重复。
> - 若要真的减少行数，应优先删 Informative 层（rationale/权衡解释/重复示意），并把机械映射表转为生成物（文档只引用生成摘要/链接），从根源减少漂移风险。

> **2025-12-20 Rationale Stripping 边界：以“可判定性/实现不变性”做硬裁判**
>
> - 建议把 Rationale 的边界写成可执行规则：删除某段后，陌生实现者是否还能正确实现？测试者是否还能写出可判定测试？若两者都不受影响，则该段属于 Rationale/认知脚手架，应迁 ADR 或删除。
> - “说明/备注/Note”是高风险容器：若包含约束，应升级为 MUST/SHOULD 条款并删除 Note；若只是解释，应剥离。
> - 例外：会改变错误恢复与交互闭环的“UX 提示”属于接口契约的一部分，不应被误删为 Rationale。

> **2025-12-20 ASCII vs 线性定义：禁止双写，SSOT 选更可测试的表示**
>
> - 对 LLM-first 规范正文，ASCII 图通常是低带宽/高 token 的噪音；更稳的 SSOT 是 EBNF/format-string/字段表等线性可解析表示。
> - ASCII/mermaid 若保留，应作为人类 DX 的衍生物，并删除等价的叙述性段落以避免漂移。

> **2025-12-21 StateJournal Hideout Round 2：AteliaResult/AteliaError 作为跨项目失败协议（规范审计）**
>
> - “机制级别”应上提到 **Atelia 项目基础机制**：`AteliaResult<T>` + `AteliaError`，避免 StateJournal/DocUI/PipeMux 各自发明 Result/Error 类型导致适配器扩散。
> - 现有条款（如 `[A-ERROR-CODE-MUST]`）当前措辞限定为“异常”，但 MVP 设计已转向 Try 方法返回结构化结果；建议把条款适用面扩展为“对外可见失败载体（异常 + Result.Error）”，否则字段与恢复策略会出现双轨漂移。
> - 强类型与通用性的折中：协议面稳定键押注 `ErrorCode: string`（可登记/可测试/可跨语言），强类型派生类仅用于库内部 DX；序列化/对外投影必须可降级为基础字段。
> - DocUI 的 Error Affordance 应是“渲染与交互层”（如何引导恢复），而不是错误对象的 SSOT；错误对象 SSOT 放在 Atelia 基座更利于一致性与复用。

> **2025-12-21 交叉讨论收口：错误即文档索引（AteliaError / ErrorCode / Details）**
>
> - `Message` 应默认面向 Agent（LLM-friendly），不再拆 `AgentMessage` 以避免双文案漂移；如需人类友好，交由 DocUI Presenter 做摘要/裁剪。
> - `Details` 约束为 `IReadOnlyDictionary<string, string>` 是务实且安全的跨边界选择；复杂结构允许 JSON-in-string。
> - `ErrorCode` 除“机器分派键”外应被视为“静态文档索引”：需要配套 ErrorCode Registry（SOP/示例/恢复路径），并提供 `help(code)` 类入口把运行时错误闭环到文档与处置流程。
