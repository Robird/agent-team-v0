# 调查 Brief: mvp-design-v2.md 历史决策引用分析

> **日期**: 2025-12-24
> **调查员**: Investigator
> **任务**: 分析 mvp-design-v2.md 中的历史决策编号引用，为语义化锚点升级做准备

---

## 目标

找出 mvp-design-v2.md 中所有 `Qxx=Y` 和 `方案 X` 格式的历史决策引用，记录其上下文和语义含义。

## 源码位置

- 主文档: `atelia/docs/StateJournal/mvp-design-v2.md`
- 决策记录: `atelia/docs/StateJournal/decisions/mvp-v2-decisions.md`

---

## 引用清单

### Qxx 引用（共 15 处）

| 行号 | 原文片段 | Qxx 含义（来自 decisions） |
|------|----------|---------------------------|
| L388 | `依据 Q18：MVP 只把 \`NextObjectId\` 写在 meta commit record 中。` | Q18: NextObjectId 持久化位置 → A. 只写在 meta 的 commit record 中 |
| L406 | `#### 3.1.2 LoadObject 语义（Q3：workspace + HEAD）的精确定义` | Q3: Resolve 语义 → B. "workspace + HEAD" |
| L415 | `因此，Q3B 的可观察效果是：` | Q3 选项 B 的具体行为描述 |
| L515 | `MVP 固定（Q15）：` | Q15: varint 适用范围 → B. 除硬定长外全部用 varint |
| L520 | `- \`DurableDict\` 的 key（Q23=A）：\`ulong\`，采用 \`varuint\`。` | Q23: Dict key 类型(MVP) → A. 仅 `ulong` key |
| L523 | `#### 3.2.0.1 varint 的精确定义（Q22=A）` | Q22: varint 规范 → A. protobuf 风格 base-128，canonical 最短编码 |
| L605 | `打开（Open）策略（Q17）：` | Q17: meta HEAD 定位 → A. 扫尾（从 meta 文件尾部回扫） |
| L640 | `- \`RootObjectId\`：ObjectId（概念上为 \`uint64\`；序列化时按 Q15 使用 \`varuint\`）` | Q15: varint 适用范围 |
| L654 | `VersionIndex 是一个 durable object（Q7），它自身也以版本链方式存储。` | Q7: VersionIndex 结构 → A. 作为 durable object，本身也版本化 |
| L656 | `落地选择（Q19=B）：` | Q19: VersionIndex 落地 → B. 扩展通用 Dict |
| L661 | `更新方式（Q8/Q9）：` | Q8: VersionIndex 更新方式 → A. 覆盖表+链式回溯; Q9: 允许链式回溯 |
| L679 | `每个对象的版本以链式版本组织（Q10）：` | Q10: VersionRecord 组织 → A. `PrevVersionPtr + DiffPayload`（链式版本） |
| L700 | `单文件 ping-pong superblock 仍是可行备选，但依据 Q16 本文不将其作为 MVP 默认方案。` | Q16: 元数据布局 → A. data + meta 两文件 |
| L774 | `- 每个内存对象具有 ChangeSet 语义（可为显式结构或隐式 diff 算法）（Q13/Q14）：` | Q13: ChangeSet 清理点 → A. Commit 成功后清空; Q14: 是否记录旧值 → A. MVP 不记录 |
| L804 | `#### 3.4.2 Dict 的 DiffPayload（Q11：Upserts + tombstone）` | Q11: Dict DiffPayload 形态 → B. 仅 Upserts + tombstone |
| L806 | `本 MVP 采用单表 \`Upserts(key -> value)\`，删除通过 tombstone value 表达（Q11B）。` | Q11 选项 B 的具体实现 |
| L1243 | `这是 MVP 的简化决策（Q23=A）；未来版本可能引入多 key 类型支持。` | Q23: Dict key 类型(MVP) → A. 仅 `ulong` key |

### 方案引用（共 8 处）

| 行号 | 原文片段 | 方案含义 |
|------|----------|----------|
| L50 | `\| **ChangeSet** \| 自上次 Commit 以来的累积变更（逻辑概念，可为隐式） \| 方案 C: \`ComputeDiff()\` + \`_dirtyKeys\` \|` | 方案 C = 双字典实现方案（`_committed` + `_current` + `_dirtyKeys`） |
| L264 | `- 说明：ChangeSet 可为隐式（如方案 C 的 Write-Tracking 机制），不要求显式的数据结构。` | 方案 C 的 Write-Tracking 机制 |
| L787 | `- 在方案 C（双字典）中，体现为 \`_current\` 字典的内容。` | 方案 C = 双字典实现 |
| L791 | `- 在方案 C（双字典）中，体现为 \`_committed\` 字典的内容。` | 方案 C = 双字典实现 |
| L795 | `- 在方案 C 中，ChangeSet 退化为"由 \`_committed\` 与 \`_current\` 两个状态做差得到"的隐式 diff 算法，不需要显式的数据结构。` | 方案 C 的 ChangeSet 语义 |
| L808 | `##### DurableDict 实现方案：双字典（方案 C）` | 章节标题，定义方案 C |
| L810 | `依据畅谈会决策（2025-12-19），MVP 采用 **方案 C（双字典）**：` | 方案 C 的正式采用声明 |
| L1234 | `以下伪代码展示方案 C（双字典）的推荐实现结构。` | 方案 C 伪代码参考 |

---

## 语义化锚点建议

### Qxx → 锚点映射建议

| 原引用 | 建议锚点 | 语义说明 |
|--------|----------|----------|
| Q3/Q3B | `[S-LOADOBJECT-WORKSPACE-HEAD]` | LoadObject 的 workspace + HEAD 语义 |
| Q7 | `[S-VERSIONINDEX-AS-DURABLE-OBJECT]` | VersionIndex 作为 durable object |
| Q8/Q9 | `[S-VERSIONINDEX-OVERLAY-CHAIN]` | VersionIndex 覆盖表+链式回溯 |
| Q10 | `[S-OBJVER-CHAIN-STRUCTURE]` | 对象版本链式组织 |
| Q11/Q11B | `[S-DICT-DIFF-TOMBSTONE]` | Dict DiffPayload 使用 tombstone 表示删除 |
| Q13/Q14 | `[S-CHANGESET-COMMIT-CLEAR]` | ChangeSet 在 Commit 成功后清空 |
| Q15 | `[F-VARINT-SCOPE]` | varint 适用范围 |
| Q16 | `[S-DUAL-FILE-LAYOUT]` | data + meta 两文件布局 |
| Q17 | `[S-META-TAIL-SCAN]` | meta HEAD 扫尾定位 |
| Q18 | `[S-NEXTOBJECTID-META-ONLY]` | NextObjectId 只存于 meta |
| Q19/Q19=B | `[S-VERSIONINDEX-REUSE-DICT]` | VersionIndex 复用 Dict 实现 |
| Q22/Q22=A | `[F-VARINT-CANONICAL]` | varint canonical 最短编码 |
| Q23/Q23=A | `[S-DICT-KEY-ULONG]` | Dict key 固定为 ulong |

### 方案 C → 锚点映射建议

| 原引用 | 建议锚点 | 语义说明 |
|--------|----------|----------|
| 方案 C / 方案 C（双字典） | `[I-DICT-DUAL-DICT-IMPL]` | DurableDict 双字典实现方案 |

---

## 实施建议

1. **批量替换策略**:
   - 先定义所有语义化锚点（在文档开头或专门章节）
   - 再将 Qxx/方案 X 引用替换为对应锚点

2. **保留决策追溯**:
   - 在 `decisions/mvp-v2-decisions.md` 中为每个 Q 添加对应的 `[S-XXX]` 锚点
   - 形成 Qxx ↔ 锚点 的双向映射

3. **渐进式迁移**:
   - 优先处理高频引用（Q15, Q23, 方案 C）
   - 低频引用可保留 Qxx 形式并添加注释

---

## 交付确认

- [x] 找出所有 Qxx=Y 引用（15 处，涉及 Q3/Q7/Q8/Q9/Q10/Q11/Q13/Q14/Q15/Q16/Q17/Q18/Q19/Q22/Q23）
- [x] 找出所有"方案 X"引用（8 处，全部为方案 C）
- [x] 提供语义化锚点映射建议
