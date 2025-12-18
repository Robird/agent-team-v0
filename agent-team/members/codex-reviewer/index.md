# Codex-reviewer 认知索引

- 最后更新: 2025-12-17

## 我是谁
- CodexReviewer，专注代码审查、架构质量风险识别与改进建议。

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [x] PipeMux
- [ ] atelia-copilot-chat

## 最近工作
- 2025-12-09: 审阅 PipeMux 管理命令 RFC，建议优先独立管理 CLI（方案 B），强调共享 Broker 客户端库与管理 RPC 分层，关注保留字冲突与权限隔离风险。

## Session Log
- 2025-12-17: 审阅 DurableHeap 写入路径 jam（2025-12-17），指出拓扑排序需显式环检测、方案 A 回滚会丢失 `_transientData`、并发缺 CAS/锁、需要 DataTail 截断或坏尾标记及上限校验，并建议 API 显式 Draft/Snapshot 以避免静默冻结。
- 2025-12-17: 复核 DurableHeap/docs/mvp-design.md 在 DictInt32 writer 对齐/回填顺序/FieldCount*8 调整后的版本，确认 pad-to-4→ObjHeader/EntryTable、回填 TotalLen→footer→CRC32C 顺序与对齐和值偏移不变量一致，无新增矛盾。
- 2025-12-17: 审阅 DurableHeap/docs/mvp-design.md 的 DictInt32 写入算法新增段，指出需在 header 后插入 4B 对齐 padding/确保 EntryTableOffset 基于对齐位置，强调回填 TotalLen 后再算 CRC32C 并保持 ValueOffset32 相对 Tag；建议维持 EntryTable 大小 FieldCount*8 与布局一致。
- 2025-12-17: 再次审阅 DurableHeap/docs/mvp-design.md（CRC32C/DataTail/Dict 排序+二分/UTF-16 pad 3B 版本），补充 “CRC32C(Castagnoli)” 口径与 UTF-16 字符串末尾补齐对齐说明，未发现其他内在矛盾。
- 2025-12-17: 复核 DurableHeap/docs/mvp-design.md 在“CRC32C、DataTail 必填、Dict entry 有序+二分查找、更新 invariants”改动需求下的规范一致性；指出需将所有 CRC32 术语收敛为 CRC32C（superblock/record/header/footer/Open Questions）、将 DataTail 从“可选”改为必填并与恢复流程一致化，DictInt32 章节与 Open Questions/下一步任务需改成“entries 按 key 升序+二分查找”且从决策清单移除可选线性扫描表述，同时把上述约束加入 MVP 决策清单，去除已决问题的 Open Questions；未新增开放问题。
- 2025-12-16: 最终一致性检查 DurableHeap/docs/mvp-design.md（UTF-16 only、Ptr32 绝对偏移/4B 对齐、RecordPtr32 vs ValueOffset32、4GB/2GB 上限）；明确 Ptr32 起点/0 哨兵与 Null 表示，去重 String 取舍措辞。
- 2025-12-16: 复审 DurableHeap/docs/mvp-design.md（按新约束：UTF-16 only、int32 dict keys、Ptr32(4B units)+4B 对齐、固定宽度 int32、文件 4GB、record 2GB）；发现仍残留 UTF-8/varint/JObject/JArray/int64/“Ptr(8B)”措辞，并指出 Ptr32 基址/可指向范围（record 起始 vs 记录内 inline tag）存在语义不一致，需要在文档中明确并统一。
- 2025-12-16: 审阅 DurableHeap MVP 文档 5.1（.NET mmap/unsafe 实现策略）；指出 AcquirePointer/ReleasePointer 的 PointerOffset 处理、span 长度上限（int）、Dispose/use-after-free 风险、mmap 视图增长与 flush 顺序等关键坑；建议用“先读固定头再取 record span”的两段式读取，并明确 offset table 与 TotalLen 的边界校验。
- 2025-12-16: 复审 DurableHeap/docs/mvp-design.md 更新内容；确认 PointerOffset 与 span 长度上限已写入，并补充 HeaderLen/Endian 约定与更明确的 commit flush 顺序表述；提出指针算术的 nint/范围检查提醒与“只加一次 PointerOffset”的防误用说明。
- 2025-12-16: 最终一致性快审：修正文档中 record 写入步骤与 CRC 计算顺序的潜在矛盾（CRC 必须在 header 回填 TotalLen 后计算），并明确 basePtr 语义为“view 的第一个字节地址”（已加 PointerOffset），避免地址计算重复加 offset。
- 2025-12-12: App-For-LLM 架构决策审阅发言（方案 A 独立进程 vs 方案 B 混合）；强调实现复杂度 A < B、技术债风险主要在 B 的嵌入式 API 兼容面；提出高权限 API 需 capability/租约模型、受审计 IPC 渠道、历史只读+受控写入、禁用进程内 reentrancy；判定 A 更可控，B 需严格 sandbox 与版本控制才能落地。
- 2025-12-11: Key-Notes 驱动 Proposals 研讨会发言 4（严谨性/边界风险）：强调 Key-Note 变更控制与版本化引用、Proposal 合规清单与偏离登记、History/History-View/Observation 分层澄清、Thinking 可观测但无效力的边界说明。
- 2025-12-10: DocUI Proposal 研讨会发言 4（严谨性/风险防范）：补强 0000 版本与安全条款、0001 冻结/失效与最小字段集、0003 命名空间与注册流程；指出 0001↔0010/0011 隐式双向依赖、LOD 定量指标缺失、ToolCallRound 缺位导致同步风险。
- 2025-12-09: 调研 DocUI 折叠与 LOD 支持方案（一次性调研要求未留痕，但记录以保持履历）。
- 2025-12-09: pmux 管理命令前缀偏好调查，选择方案 B（前缀冒号）以避免保留字冲突且保持管理命令与应用命令的视觉分隔。
- 2025-12-09: 审阅 DocUI MemoryNotebook 概念原型，记录数据模型可扩展性与 CLI 交互的改进点。

## Open Investigations
- None
