# 畅谈会：Layer 0 文档对齐复核

> **日期**：2025-12-22
> **标签**：#review
> **主持人**：刘德智 (Team Leader)
> **参与者**：Advisor-Claude, Advisor-GPT
> **状态**：进行中

---

## 背景

从 `mvp-design-v2.md.bak` 切分出 Layer 0 文档：
- `elog-format.md`（二进制格式规范）
- `elog-interface.md`（接口契约）

需要复核这两个文档是否完整覆盖原版中的 Layer 0 内容。

## 审阅目标

1. **完整性**：原版中的 Layer 0 条款是否都被提取？
2. **一致性**：提取后的内容是否与原版一致（无歧义、无矛盾）？
3. **条款映射**：原版 `[F-xxx]` 条款是否都有对应的 `[E-xxx]` 条款？

## 目标文件

- 原版：`atelia/docs/StateJournal/mvp-design-v2.md.bak`
- 新版：`atelia/docs/StateJournal/elog-format.md`
- 新版：`atelia/docs/StateJournal/elog-interface.md`

## 💬 畅谈记录

### 主持人开场 (刘德智)

请各位顾问对照原版，检查 Layer 0 文档的完整性和一致性。

重点关注：
1. 原版的 EBNF 语法、Frame 结构、Magic、CRC32C 是否完整迁移
2. 原版的 `[F-xxx]` 条款是否都有对应
3. Genesis Header 的定义是否完整
4. 逆向扫描、Resync 算法是否完整

发现问题请给出 FixList，会后直接修复。

---

### Advisor-Claude 发言

## 1. 条款映射检查

### 1.1 原版 `[F-xxx]` 条款映射表

| 原版条款 ID | 原版位置 | 新版条款 ID | 新版位置 | 状态 |
|-------------|----------|-------------|----------|------|
| `[F-MAGIC-IS-FENCE]` | §3.2.1 | `[E-MAGIC-IS-FENCE]` | elog-format.md §5.1 | ✅ 已迁移 |
| `[F-MAGIC-RECORD-SEPARATOR]` | §3.2.1 | `[E-MAGIC-FRAME-SEPARATOR]` | elog-format.md §5.2 | ✅ 已迁移（术语更新 Record→Frame）|
| `[F-HEADLEN-TAILLEN-SYMMETRY]` | §3.2.1 | `[E-HEADLEN-TAILLEN-SYMMETRY]` | elog-format.md §3.2 | ✅ 已迁移 |
| `[F-RECORD-4B-ALIGNMENT]` | §3.2.1 | `[E-FRAME-4B-ALIGNMENT]` | elog-format.md §3.2 | ✅ 已迁移（术语更新 Record→Frame）|
| `[F-CRC32C-PAYLOAD-COVERAGE]` | §3.2.1 | `[E-CRC32C-COVERAGE]` | elog-format.md §4.1 | ✅ 已迁移 |
| `[F-RECORD-WRITE-SEQUENCE]` | §3.2.1 | `[E-FRAME-WRITE-SEQUENCE]` | elog-format.md §6 | ✅ 已迁移（术语更新 Record→Frame）|
| `[F-PTR64-NULL-AND-ALIGNMENT]` | §3.2.1 | `[E-PTR64-NULL-AND-ALIGNMENT]` | elog-format.md §9.2 | ✅ 已迁移 |
| `[F-VARINT-CANONICAL-ENCODING]` | §3.2.0.1 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[F-DECODE-ERROR-FAILFAST]` | §3.2.0.1 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[F-RECORDKIND-DOMAIN-ISOLATION]` | §3.2.1 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[F-OBJECTKIND-STANDARD-RANGE]` | §3.2.5 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[F-OBJECTKIND-VARIANT-RANGE]` | §3.2.5 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[F-UNKNOWN-OBJECTKIND-REJECT]` | §3.2.5 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[F-KVPAIR-HIGHBITS-RESERVED]` | §3.4.2 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[F-UNKNOWN-VALUETYPE-REJECT]` | §3.4.2 | — | — | ⚪ 未迁移（属于 Layer 1）|

### 1.2 原版 `[R-xxx]` 条款映射表（Recovery）

| 原版条款 ID | 原版位置 | 新版条款 ID | 新版位置 | 状态 |
|-------------|----------|-------------|----------|------|
| `[R-RESYNC-DISTRUST-TAILLEN]` | §3.2.1 | `[E-RESYNC-DISTRUST-TAILLEN]` | elog-format.md §8.2 | ✅ 已迁移 |
| `[R-META-AHEAD-BACKTRACK]` | §3.2.2 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[R-META-RESYNC-SAME-AS-DATA]` | §3.2.2 | 隐含覆盖 | elog-format.md §8 统一定义 | ✅ 隐含覆盖 |
| `[R-COMMIT-FSYNC-ORDER]` | §3.2.2 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[R-COMMIT-POINT-META-FSYNC]` | §3.2.2 | — | — | ⚪ 未迁移（属于 Layer 1）|
| `[R-DATATAIL-TRUNCATE-GARBAGE]` | §3.5 | `[E-DATATAIL-TRUNCATE]` | elog-format.md §10.2 | ✅ 已迁移 |
| `[R-ALLOCATOR-SEED-FROM-HEAD]` | §3.5 | — | — | ⚪ 未迁移（属于 Layer 1）|

### 1.3 评估结论

**Layer 0 相关条款迁移完整性：✅ 完整**

原版中属于 Layer 0（ELOG Framing）的条款已全部迁移：
- Frame 结构（HeadLen/TailLen/CRC32C/Magic/Pad）
- 写入顺序
- Ptr64 编码与对齐
- 逆向扫描与 Resync
- DataTail 截断

**未迁移的条款均属于 Layer 1（StateJournal Record 层）**：
- varint 编码
- RecordKind/ObjectKind/ValueType 枚举
- Commit Point 与 fsync 顺序
- Allocator 初始化

---

## 2. 内容完整性检查

### 2.1 EBNF 语法

| 元素 | 原版位置 | 新版位置 | 状态 |
|------|----------|----------|------|
| File 产生式 | §3.2.1 | elog-format.md §2.2 | ✅ 已迁移（改进：明确 Genesis 概念）|
| Record/Frame 产生式 | §3.2.1 | elog-format.md §2.2 | ✅ 已迁移（术语 Record→Frame）|

**评估**：新版 EBNF 与原版一致，且更清晰地区分了 Genesis Header 与 Frame。

### 2.2 Frame 结构

| 字段 | 原版 | 新版 | 状态 |
|------|------|------|------|
| HeadLen (u32 LE) | ✅ | ✅ | 一致 |
| Payload | ✅ | ✅ | 一致 |
| Pad (0-3B) | ✅ | ✅ | 一致 |
| TailLen (u32 LE) | ✅ | ✅ | 一致 |
| CRC32C (u32 LE) | ✅ | ✅ | 一致 |
| Magic (4B) | ✅ | ✅ | 一致 |

**评估**：✅ 完全一致

### 2.3 Genesis Header

| 内容 | 原版 | 新版 | 状态 |
|------|------|------|------|
| Magic 作为文件头 | §3.2.1 步骤 0 隐含 | elog-format.md §2.1 显式定义 | ✅ 新版更清晰 |
| 空文件语义 | 隐含（FileLength==4） | `[E-GENESIS-EMPTY-FILE]` 显式定义 | ✅ 新版更清晰 |
| Magic 值定义 | "DHD3"/"DHM3" | 相同 | ✅ 一致 |

**评估**：✅ 新版改进——Genesis Header 作为独立概念显式定义

### 2.4 Magic 语义

| 内容 | 原版 | 新版 | 状态 |
|------|------|------|------|
| Magic 是 Fence | `[F-MAGIC-IS-FENCE]` | `[E-MAGIC-IS-FENCE]` | ✅ 一致 |
| Magic 是 Separator | `[F-MAGIC-RECORD-SEPARATOR]` | `[E-MAGIC-FRAME-SEPARATOR]` | ✅ 一致 |

### 2.5 CRC32C

| 内容 | 原版 | 新版 | 状态 |
|------|------|------|------|
| 覆盖范围 | Payload + Pad + TailLen | 相同 | ✅ |
| 不覆盖 HeadLen | ✅ | ✅ | ✅ |
| 算法 | CRC32C (Castagnoli) | 相同 + 明确多项式 | ✅ 新版更详细 |

### 2.6 逆向扫描（Reverse Scan）

| 内容 | 原版 | 新版 | 状态 |
|------|------|------|------|
| 扫描算法描述 | §3.2.1 散落 | elog-format.md §7 集中 | ✅ 新版更清晰 |
| 地址计算公式 | 散落 | §7.3 集中 | ✅ 新版更清晰 |
| 图示 | 无 | §7.3 有 | ✅ 新版改进 |

### 2.7 Resync 策略

| 内容 | 原版 | 新版 | 状态 |
|------|------|------|------|
| 不信任 TailLen | `[R-RESYNC-DISTRUST-TAILLEN]` | `[E-RESYNC-DISTRUST-TAILLEN]` | ✅ |
| 4B 对齐向前扫描 | ✅ | `[E-RESYNC-SCAN-MAGIC]` | ✅ |
| 测试场景 | 无 | §12.2 有 | ✅ 新版改进 |

---

## 3. 一致性检查

### 3.1 术语变更一致性

| 原版术语 | 新版术语 | 变更合理性 |
|----------|----------|------------|
| Record | Frame | ✅ 合理——Layer 0 只知道 Frame，不知道 Record 语义 |
| Ptr64 | Ptr64 + Address64 | ✅ 合理——Address64 是类型化封装，语义更清晰 |

### 3.2 数值一致性

| 数值 | 原版 | 新版 | 状态 |
|------|------|------|------|
| 最小 HeadLen | >= 12 | >= 12 | ✅ |
| Pad 范围 | 0-3B | 0-3B | ✅ |
| Magic "DHD3" | 0x44484433（隐含） | 0x44484433（显式） | ✅ |

### 3.3 ⚠️ 发现的不一致

**问题 1：Pad 长度计算公式表述差异**

- **原版**（§3.2.1 EBNF 注释）：`Pad := 0..3 bytes (* align to 4B *)`
- **新版**（elog-format.md §3.2）：
  - `PadLen = (4 - ((PayloadLen + 4) % 4)) % 4`
  - 或等价地：`PadLen = (4 - (PayloadLen % 4)) % 4`

**分析**：新版给出了两个"等价"公式，但表述可能造成困惑。

验证后公式等价，但 **公式 1 的 `+4` 来源不清晰**（可能是考虑 HeadLen 占位？）。

**建议**：保留公式 2（更简洁），删除公式 1 或添加推导说明。

---

**问题 2：elog-interface.md 的 FrameTag 0x01/0x02 与原版 RecordKind 的映射**

- **原版**：`RecordKind = 0x01` 在 data file 表示 `ObjectVersionRecord`，在 meta file 表示 `MetaCommitRecord`（域隔离）
- **新版 elog-interface.md §5.1**：`FrameTag 0x01 = DataRecord.ObjectVersion`，`FrameTag 0x02 = MetaRecord.Commit`

**分析**：新版使用不同 FrameTag 值区分，而非域隔离。这是 **设计变更** 而非迁移问题。

**建议**：需在 mvp-design-v2.md 中同步更新，或在 elog-interface.md 注明"待对齐"。

---

## 4. FixList

### P0（Must Fix）

| # | 问题 | 位置 | 建议修复 |
|---|------|------|----------|
| — | *无 P0 级问题* | — | — |

### P1（Should Fix）

| # | 问题 | 位置 | 建议修复 |
|---|------|------|----------|
| 1 | Pad 长度公式有两个版本，表述冗余 | elog-format.md §3.2 `[E-HEADLEN-FORMULA]` | 删除公式 1 或添加推导说明 |
| 2 | FrameTag 值与原版 RecordKind 域隔离设计不一致 | elog-interface.md §5.1 | 添加注释说明设计变更，或与 mvp-design-v2.md 对齐 |

### P2（Nice to Have）

| # | 问题 | 位置 | 建议修复 |
|---|------|------|----------|
| 3 | 原版的 CRC32C 校验失败处理未迁移 | elog-format.md | 添加 `[E-CRC-FAIL-REJECT]` 条款 |
| 4 | 逆向扫描终止条件（遇到损坏数据时的策略）未明确 | elog-format.md §7 / elog-interface.md §9 | 在 §7 添加"扫描终止"子节 |
| 5 | 测试向量 §12 缺少 CRC32C 实际值 | elog-format.md §12.1 | 补充实际 CRC32C 计算结果 |

---

## 5. 总体评估

### 5.1 评分

| 维度 | 评分 | 说明 |
|------|------|------|
| 条款覆盖完整性 | **A** | Layer 0 相关条款 100% 迁移 |
| 内容完整性 | **A** | EBNF、Frame 结构、扫描算法完整 |
| 一致性 | **B+** | 有 2 个小问题需要对齐 |
| 清晰度改进 | **A** | 新版在 Genesis Header、扫描算法、图示方面显著改进 |

### 5.2 结论

**Layer 0 文档迁移质量：✅ 合格**

- 所有 Layer 0 核心内容已完整迁移
- 新版在结构清晰度和图示方面有改进
- 未迁移的条款均属于 Layer 1，符合分层设计预期
- 2 个 P1 级问题建议修复

### 5.3 术语映射表（供后续 Layer 1 迁移参考）

| 原版条款前缀 | 归属层 | 迁移目标 |
|--------------|--------|----------|
| `[F-MAGIC-*]` | Layer 0 | elog-format.md ✅ |
| `[F-HEADLEN-*]` | Layer 0 | elog-format.md ✅ |
| `[F-RECORD-*]` | Layer 0 | elog-format.md ✅ |
| `[F-CRC32C-*]` | Layer 0 | elog-format.md ✅ |
| `[F-PTR64-*]` | Layer 0 | elog-format.md ✅ |
| `[F-VARINT-*]` | Layer 1 | 待迁移到 record-format.md |
| `[F-RECORDKIND-*]` | Layer 1 | 待迁移到 record-format.md |
| `[F-OBJECTKIND-*]` | Layer 1 | 待迁移到 record-format.md |
| `[F-KVPAIR-*]` | Layer 1 | 待迁移到 record-format.md |
| `[R-RESYNC-*]` | Layer 0 | elog-format.md ✅ |
| `[R-DATATAIL-*]` | Layer 0 | elog-format.md ✅ |
| `[R-COMMIT-*]` | Layer 1 | 待迁移 |
| `[R-META-*]` | Layer 1 | 待迁移 |
| `[R-ALLOCATOR-*]` | Layer 1 | 待迁移 |

---

*复核完成 — Advisor-Claude*

### Advisor-GPT 发言

以下复核聚焦：**精确性（数值/边界条件/字节序表述）**与**可测试性（条款可判定、可写测试向量）**，并对 Claude 的 FixList 做确认与补充。

## 1) 对 Claude FixList 的确认/修正

### 1.1 确认项（Claude 判断正确）

1) **Pad 公式表述冗余**（P1）
- 现状：`elog-format.md` 在 `[E-HEADLEN-FORMULA]` 同时给出 `PadLen = (4 - ((PayloadLen + 4) % 4)) % 4` 与 `PadLen = (4 - (PayloadLen % 4)) % 4` 并声称等价。
- 结论：两式数学上等价（`+4` 对 mod 4 无影响），但“+4 来源”不自证，容易误导实现者。
- 建议：保留更直观的 `PadLen = (4 - (PayloadLen % 4)) % 4`，删除另一个或补一行推导。

2) **FrameTag 映射与原版 RecordKind 域隔离不一致**（P1）
- 结论：这确实是“设计变更 / 分层重构”而非“迁移遗漏”。
- 关键风险：若 `mvp-design-v2.md`（或后续 Layer 1 record-format 文档）仍假定 payload 首字节是 `RecordKind` 且 data/meta 通过“域隔离”复用数值 `0x01`，则与 `elog-interface.md` 里“通过 FrameTag 取值区分 data/meta”会产生双轨实现。

### 1.2 需要修正 Claude 结论的点

Claude 的 FixList 中把“逆向扫描终止条件不明确”归为 P2。基于精确性复核，我认为这里存在一个**明确的边界 bug（P0）**，见下文 2.2。

---

## 2) 精确性（Magic 值、CRC 多项式、对齐/边界）审计

### 2.1 CRC32C 多项式表述错误（P0）

`elog-format.md` 的 `[E-CRC32C-ALGORITHM]` 写道：
- “多项式：`0x1EDC6F41`（反射形式）”

问题：`0x1EDC6F41` 是 CRC32C（Castagnoli）的**常规（normal / non-reflected）**多项式表示；常见的**反射（reflected）**表示是 `0x82F63B78`。

这会直接导致跨语言实现的 CRC 不一致（尤其是实现者按“反射形式”去写 bitwise routine）。

建议修复（择一，推荐 A）：
- A（推荐）：明确写成“使用 .NET `System.IO.Hashing.Crc32C` 等价的 CRC32C（Castagnoli）”，并同时列出 normal 与 reflected 两种多项式表示，避免实现者混淆。
- B：若只想列一个数值，则把括注改正确：`0x1EDC6F41 (normal)` 或 `0x82F63B78 (reflected)`，并补一句“本规范采用 reflected input/output 的常见实现约定”。

### 2.2 Reverse Scan 算法首帧边界条件错误（P0）

`elog-format.md` 的 `[E-REVERSE-SCAN-ALGORITHM]` 步骤 f：
- “验证 `RecordStart >= 8` 且 `RecordStart % 4 == 0`”

这与本格式的文件布局冲突：Genesis Magic 长度为 4B，因此**第一条 Frame 的 `RecordStart` 合法值应为 4**（紧随 Genesis）。

可构造最小反例（可测试）：
- 文件：`[Genesis Magic][空 Payload Frame][Trailing Magic]`
- 该帧 `TailLen=12`，`RecordEnd=MagicPos=16`，所以 `RecordStart=4`。
- 现有条件 `RecordStart>=8` 会错误拒绝该帧，导致“只有一条 frame 的合法文件无法被 reverse scan 识别”。

建议修复：把条件改为 `RecordStart >= 4`（或更一般地：`RecordStart >= GenesisLen`），并在算法中显式引用 GenesisLen=4 的常量。

### 2.3 Magic 的 Hex 表示存在端序歧义（P1）

`elog-format.md` 在 §2.1 的 Magic 表里写了：
- `DHD3` → `0x44484433`

若把该数值理解为“u32 LE 写入文件”，写出的字节序会变成 `33 44 48 44`（非 ASCII）。

建议修复：
- 把“Hex”改为**字节序列**形式：`44 48 44 33` / `44 48 4D 33`；
- 或明确声明：Magic 在文件中以 ASCII 字节序列写入（逐字节），不是以 u32 的某个端序写入。

---

## 3) 可测试性审计（条款是否可判定）

### 3.1 `elog-format.md` 未定义 FrameTag 的 wire encoding（P0）

`elog-interface.md` 把 Frame 抽象为 `(FrameTag, Payload)`，且 `IElogScanner` 必须读出 `Tag`。

但 `elog-format.md` 的 Frame 布局没有出现 `Tag` 字段：仅有 `HeadLen/Payload/Pad/TailLen/CRC32C/Magic`。

没有 Tag 的 wire 定义，将导致：
- 无法写“黑盒可判定测试”：scanner 如何从文件解码出 `Tag`？
- interface 与 format 无法对齐：实现者会自发选择“Tag=Payload[0]”或“Tag 在 header”等，造成兼容性分叉。

建议：在 `elog-format.md` 加一个 P0 条款，明确 Tag 的编码位置（推荐：**Tag 作为 Payload 的第 1 个字节**，ELOG 负责读写但不解释语义；这与“ELOG 是信封”并不冲突）。并同步更新：
- CRC 覆盖范围仍然正确（Tag 属于 Payload，自然被覆盖）。
- `HeadLen`/`PayloadLen` 的定义是否将 Tag 计入 PayloadLen（应计入）。

### 3.2 CRC mismatch / framing mismatch 的“失败策略”需要条款化（P2→P1）

Claude 建议添加 `[E-CRC-FAIL-REJECT]`（P2）。从可测试性角度我建议提升为 **P1**：
- 不规定“CRC/HeadLen/TailLen/Magic 不匹配时的处理”，测试只能验证“某实现行为”，无法验证“规范行为”。

建议：增加最小失败策略条款集合：
- `[E-CRC-FAIL-REJECT]`：CRC 不匹配 MUST 视为损坏（进入 resync/或 TryReadAt 返回 false）。
- `[E-FRAMING-FAIL-REJECT]`：HeadLen!=TailLen / 非 4B 对齐 / 越界 / Magic 不匹配等 MUST 视为损坏。

并在 interface 层给出可测试的返回通道（例如 `TryReadAt` 返回 false；`ScanReverse` 跳过损坏并继续 resync）。

### 3.3 “ScanReverse 遇损坏后的终止/继续”建议明确（P2）

这里我同意 Claude 的 P2 定性：属于行为细化，不改主格式。
但建议至少写一条“不得无限循环、必须单调推进”的可判定约束，以避免实现陷入死循环。

---

## 4) 最终统一 FixList（汇总）

### P0（Must Fix：会导致实现分叉或规范自相矛盾）

1) **CRC32C 多项式“反射形式”表述错误**
- 位置：`elog-format.md` `[E-CRC32C-ALGORITHM]`
- 修复：纠正为 normal/reflected 的正确数值与约定（或直接引用 `.NET System.IO.Hashing.Crc32C` 语义）。

2) **Reverse Scan 算法首帧边界条件错误（RecordStart>=8）**
- 位置：`elog-format.md` `[E-REVERSE-SCAN-ALGORITHM]` 步骤 f
- 修复：改为 `RecordStart >= 4`（或 `>= GenesisLen`），并用最小单帧文件作为回归向量。

3) **FrameTag 的 wire encoding 缺失**
- 位置：`elog-format.md`（整体：Frame 布局未包含 Tag）与 `elog-interface.md`（Scanner 需要 Tag）之间
- 修复：在 `elog-format.md` 明确 Tag 的编码位置（推荐 `Tag = Payload[0]`），并把 PayloadLen/测试向量一并对齐。

### P1（Should Fix：歧义/误导，会降低可实现一致性）

4) **Magic 的 Hex 表达端序歧义**
- 位置：`elog-format.md` §2.1 Magic 表
- 修复：改为字节序列表示（`44 48 44 33`），或明确“ASCII bytes 写入，不是 u32 端序写入”。

5) **PadLen 公式冗余且 `+4` 来源不清**
- 位置：`elog-format.md` `[E-HEADLEN-FORMULA]`
- 修复：保留简式、删冗余或补推导。

6) **FrameTag 取值与原版 RecordKind 域隔离的对齐说明不足**
- 位置：`elog-interface.md` §5.1
- 修复：要么同步更新上层（`mvp-design-v2.md` / Layer1 record-format），要么在 interface 中明确“这是新设计，原版 RecordKind 仅存在于 payload、且未来将被 FrameTag 取代”的迁移注记。

7) **CRC/Framing 失败策略条款化**（把 Claude 的 P2#3 提升为 P1）
- 位置：`elog-format.md`
- 修复：增加 `[E-CRC-FAIL-REJECT]` / `[E-FRAMING-FAIL-REJECT]` 最小条款，保证测试可判定。

### P2（Nice to Have：质量/可维护性提升）

8) **ScanReverse 损坏数据时的终止条件与“单调推进”约束**
- 位置：`elog-format.md` §7 / `elog-interface.md` §9
- 建议：写明“resync 每步 MagicPos -= 4，必须终止”。

9) **测试向量补齐 CRC32C 具体数值**
- 位置：`elog-format.md` §12
- 建议：给 1-2 个固定 payload 的 CRC32C 常量，便于跨语言互验。

（以上为本轮 #review 的补充结论。）


---

## 📋 主持人总结 (刘德智)

### 发现的问题汇总

| 优先级 | 数量 | 关键问题 |
|--------|------|----------|
| **P0** | 3 | CRC32C 多项式表述、Reverse Scan 边界、FrameTag wire encoding 缺失 |
| **P1** | 4 | Magic 端序歧义、Pad 公式冗余、FrameTag/RecordKind 对齐、失败策略 |
| **P2** | 2 | ScanReverse 终止、测试向量补 CRC |

### 立即执行修复

状态：🔧 执行中

---


### P1 问题修复完成 (Implementer)

**执行时间**：2025-12-22

| P1 ID | 问题 | 修复 | 状态 |
|-------|------|------|------|
| P1-1 | Magic 端序歧义 | 改为字节序列表示 `44 48 44 33`，新增 `[E-MAGIC-BYTE-SEQUENCE]` 条款 | ✅ |
| P1-2 | PadLen 公式冗余 | 简化为单一公式，添加推导说明，新增 `[E-PADLEN-FORMULA]` 条款 | ✅ |
| P1-3 | CRC/Framing 失败策略 | 新增 `[E-CRC-FAIL-REJECT]` 和 `[E-FRAMING-FAIL-REJECT]` 条款 | ✅ |
| P1-4 | FrameTag/RecordKind 对齐 | 在 elog-interface.md §5.1 添加设计变更说明 | ✅ |

**文件变更**：
- `atelia/docs/StateJournal/elog-format.md` — 版本 0.2 → 0.3
- `atelia/docs/StateJournal/elog-interface.md` — §5.1 更新

**新增条款**：4 条（E-MAGIC-BYTE-SEQUENCE, E-PADLEN-FORMULA, E-CRC-FAIL-REJECT, E-FRAMING-FAIL-REJECT）

---



**状态**：✅ Layer 0 + Layer 1 对齐复核全部完成

---

