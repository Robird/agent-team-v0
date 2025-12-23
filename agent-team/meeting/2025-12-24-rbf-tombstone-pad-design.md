# 畅谈会：RBF 墓碑帧机制重设计 — 尾部 Pad 标记方案

> **日期**：2025-12-24
> **标签**：#design
> **主持人**：刘德智 (Team Leader / StandardsChair)
> **参与者**：Advisor-Claude, Advisor-Gemini, Advisor-GPT
> **状态**：✅ 已完成

---

## 背景

### 问题回顾

在上次畅谈会（2025-12-23-rbf-frametag-design-smell.md）中，我们解决了 FrameTag 的 4B 对齐问题，但留下了一个遗留问题：

> **墓碑帧机制与 FrameTag 混在一起，层级划分不清。**

当前设计（rbf-interface.md `[S-RBF-BUILDER-AUTO-ABORT]`）：
- Padding/墓碑帧使用 `FrameTag = 0x00000000` 标记
- 这将 **格式层的元信息**（帧是否有效）与 **应用层的分类机制**（FrameTag）混为一体
- 位于头部的 FrameTag 需要 **seek 回填**，增加实现复杂度

### 监护人提议的新方案

**核心思想**：利用 TailLen 之前的 Pad 字段，改为 **Pad 1~4 字节**（当前是 0~3），用 Pad 的特殊值标记墓碑帧。

**具体设计**：

| Pad 值 | 含义 |
|--------|------|
| `0x00` | 普通帧（正常数据） |
| `0xFF` | 墓碑帧（Auto-Abort / 逻辑删除） |
| `0x01`~`0xFE` | 保留（未来扩展，如元数据标志） |

**优势**（监护人陈述）：
1. **层级清晰**：墓碑标记是格式层元信息，不污染应用层 FrameTag
2. **无需 seek 回填**：写入时只需在尾部追加墓碑标记，无需回写头部
3. **FrameTag 完全释放**：应用层可以自由使用 `0x00000000`（最自然的"不关心"值）
4. **可扩展性**：尾部 Pad 的其他值区间可承载格式层元数据（如时间戳、过滤标志）

**代价**：
- 平均帧长度增加 1 字节（从 0~3 变为 1~4）

---

## 待评审问题

请参谋组从各自视角评估以下问题：

### Q1: 方案可行性
- 新方案是否能完整替代当前的 `FrameTag=0` 墓碑机制？
- 是否存在无法覆盖的边界情况？

### Q2: 层级划分
- 用 Pad 值承载"帧有效性"是否是正确的层级划分？
- 这是否符合"RBF 是二进制信封"的设计原则？

### Q3: 逆向扫描兼容性
- 当前逆向扫描算法（`[R-REVERSE-SCAN-ALGORITHM]`）需要如何修改？
- 是否需要在扫描时检查 Pad 值以跳过墓碑帧？

### Q4: 可扩展性评估
- "用其他 Pad 值承载格式层元数据"的扩展方向是否合理？
- 如何平衡"最小必要"与"预留扩展"？

### Q5: 命名与术语
- 如果采纳，应如何命名这个 Pad 字段？（`TailPad`? `FrameMeta`? `ValidityMarker`?）
- 相关条款如何重新组织？

---

## 💬 畅谈记录

### 主持人开场 (刘德智)

各位顾问，欢迎来到这次设计畅谈会。

今天我们要评审一个关于 RBF 墓碑帧机制的改进方案。核心思想很简洁：**把墓碑标记从头部的 FrameTag 移到尾部的 Pad 字段**。

这个方案有几个吸引我的点：
1. **概念更干净**：FrameTag 是"帧内容分类"，Pad 变成"帧元数据"，各司其职
2. **实现更简单**：尾部追加比头部回填更自然
3. **未来扩展**：Pad 的值空间可以承载更多格式层元信息

但我也有担忧：
- 是否过度设计？毕竟 MVP 阶段只需要"有效/无效"两态
- 逆向扫描的逻辑是否会变复杂？

请 Advisor-Claude 先从概念架构角度开场，然后 Advisor-Gemini 从 DX（开发者体验）角度展开，最后 Advisor-GPT 做规范审计和收敛。

期待各位的真知灼见！

---

### Advisor-Claude 发言

感谢主持人邀请。这个提案触及了 RBF 设计的一个核心问题：**"帧有效性"应该在哪个层级表达？**

让我从概念架构的角度展开分析。

---

#### 1. 概念层级分析：这本质上是什么？

当前设计（`FrameTag = 0` 作为墓碑）的问题，本质上是**两个不同关注点的混淆**：

| 关注点 | 本质问题 | 当前承载 |
|:-------|:---------|:---------|
| **帧有效性** | "这个帧是否应该被处理？" | FrameTag |
| **帧分类** | "这个帧属于哪种业务类型？" | FrameTag |

这两个问题的**抽象层级不同**：

- **帧有效性** 是 **格式层（Layer 0）** 的元信息——它关于帧本身的存在状态
- **帧分类** 是 **语义层（Layer 1/上层）** 的业务信息——它关于 payload 的解释

用同一个字段承载两个层级的信息，违反了我们一直强调的原则："RBF 是二进制信封"。信封不应该知道里面装的是什么类型的文件，但信封可以（且应该）知道自己是否"被撕毁"。

> **类比**：这类似于 TCP 层的 FIN/RST 标志——它们标记连接的有效性状态，而不是用 payload 的某个保留值来表示"连接结束"。

---

#### 2. 新方案的层级评估：用 Pad 承载有效性是否正确？

监护人提议的 **Pad 字段承载帧有效性** 是一个精妙的设计选择。让我分析其合理性：

**2.1 Pad 字段的本质**

Pad 的原始职责是**对齐填充**——纯粹的格式层关注点。将其升级为**帧元数据载体**，需要回答：这两个职责是否兼容？

```
原始 Pad：0~3 字节，全 0，纯填充
新 Pad：  1~4 字节，首字节可为 0x00/0xFF，兼职元数据
```

关键洞察：**对齐需求 + 元数据需求可以共存**。

- 新设计保证 1~4 字节，**至少有 1 字节空间**承载元数据
- 剩余字节仍用于对齐填充
- 这是"最小边际成本"的扩展——平均只增加 1 字节

**2.2 层级划分评估**

| 维度 | 当前设计 | 新设计 |
|:-----|:---------|:-------|
| **有效性标记位置** | 头部 FrameTag | 尾部 Pad |
| **需要 seek 回填？** | 是 | 否 |
| **Layer 0/1 边界** | 模糊 | 清晰 |
| **FrameTag 语义** | 混合（有效性 + 分类） | 纯粹（仅分类） |

新设计使得：
- **Layer 0**：只关心帧的物理结构和有效性（通过 Pad 首字节判断）
- **Layer 1**：只关心帧的业务分类（通过 FrameTag）

这正是"信封原则"的体现：**信封知道自己是否被作废，但不知道里面装的是什么**。

---

#### 3. 逆向扫描兼容性分析

当前 `[R-REVERSE-SCAN-ALGORITHM]` 的逻辑：

```
找到 TailLen → 计算 frameStart → 验证 HeadLen/CRC
```

新设计需要在哪里检查 Pad 值？

**关键洞察**：逆向扫描的职责是**定位帧边界**，不是**过滤业务帧**。

因此：

- **Layer 0 Scanner** 应该产出所有帧（包括墓碑帧），这与当前 `[F-FRAMETAG-PADDING-VISIBLE]` 一致
- **Layer 1 Reader** 负责根据 Pad 首字节跳过墓碑帧

这意味着 `[R-REVERSE-SCAN-ALGORITHM]` **不需要修改**——它仍然产出所有物理完整的帧。变化发生在帧的后处理阶段。

但有一个**细微变化**：如果我们希望 Scanner 提供 Pad 值给上层，需要在 `RbfFrame` 结构中增加一个字段。

---

#### 4. 术语命名建议

如果采纳此方案，需要为这个新概念命名。几个候选：

| 候选名 | 优点 | 缺点 |
|:-------|:-----|:-----|
| `TailPad` | 与 `TailLen` 对称 | 暗示仅是填充，丢失"元数据"语义 |
| `FrameMeta` | 准确表达"帧元数据" | 过于宽泛，可能暗示承载更多信息 |
| `ValidityMarker` | 精确描述当前用途 | 太长，且限制了未来扩展 |
| `Trailer` | 已有概念（帧尾部） | 可能与 `TailLen/CRC` 混淆 |
| **`FrameStatus`** | 简洁，表达"帧状态" | — |

**我的建议**：**`FrameStatus`**

理由：
- 比 `ValidityMarker` 更简洁
- 比 `TailPad` 更准确表达其职责
- 语义上是"帧的状态标记"，为未来扩展留空间（如 `0x01` = "软删除待 GC"）
- 与 `FrameTag`（帧分类）形成语义对比

如果采纳，字段值的命名建议：

| 值 | 名称 | 语义 |
|----|------|------|
| `0x00` | `FrameStatus.Valid` | 正常帧 |
| `0xFF` | `FrameStatus.Tombstone` | 墓碑帧（Auto-Abort / 逻辑删除） |
| `0x01`~`0xFE` | — | 保留（未来扩展） |

---

#### 5. 可扩展性评估

监护人提到"其他 Pad 值可承载格式层元数据"。这是一个**两难问题**：

**保守路线（YAGNI）**：
- MVP 只定义 `0x00`（Valid）和 `0xFF`（Tombstone）
- 其他值标记为"保留，读取时视为 Valid"（向前兼容）
- 未来扩展时再定义语义

**激进路线（预留架构）**：
- 定义一个小型值空间分区（如 `0x00-0x0F` = 有效类，`0xF0-0xFF` = 无效类）
- 为未来的"软删除"、"待 GC"、"元数据标记"等预留语义空间

**我的建议**：**保守路线 + 最小预留**

具体来说：
- `0x00` = Valid（MUST 处理）
- `0xFF` = Tombstone（MUST 跳过）
- `0x01`~`0xFE` = 未来保留，**当前实现 SHOULD 视为 Valid**（向前兼容）

这避免了过度设计，同时为未来留下扩展空间。

---

#### 6. 总体评估

| 维度 | 评分 | 说明 |
|:-----|:-----|:-----|
| **概念清晰度** | ⭐⭐⭐⭐⭐ | 层级划分更干净，符合"信封原则" |
| **实现简化** | ⭐⭐⭐⭐ | 消除 seek 回填，尾部追加更自然 |
| **空间代价** | ⭐⭐⭐⭐ | 平均 +1 字节，可接受 |
| **兼容性影响** | ⭐⭐⭐ | 需要修改 Pad 定义和 FrameBytes 布局 |
| **扩展潜力** | ⭐⭐⭐⭐ | 为未来元数据预留空间 |

**结论**：**支持采纳此方案**。

它让 RBF 更好地体现了"二进制信封"的设计原则——信封的"作废标记"（Tombstone）与信封的"内容分类"（FrameTag）分离，各归其位。

---

#### 7. 遗留问题（供后续讨论）

1. **CRC 覆盖范围**：新的 `FrameStatus` 字段是否应纳入 CRC 计算？
   - 当前 Pad 不在 CRC 范围内（`[F-CRC32C-COVERAGE]` 覆盖 `FrameTag + Payload + Pad + TailLen`）
   - 等等，当前 Pad **已经**在 CRC 范围内！所以 `FrameStatus` 自然被保护

2. **术语表更新**：如果采纳，需要在 rbf-format.md 中更新：
   - `[F-FRAME-LAYOUT]` 表格：Pad 改为 `FrameStatus + Pad`（或 `FrameStatus` 为首字节 + Pad 为后续字节）
   - 新增 `[F-FRAMESTATUS-VALUES]` 条款

期待 Gemini 从 DX 角度和 GPT 从规范审计角度的补充意见。

---


### Advisor-GPT 发言

以下审计以“条款可引用/可测试/可回滚”为标准；重点检查 **Layer 0（rbf-format.md）** 与 **Layer 1（rbf-interface.md）** 的一致性、边界条件与兼容性语义。

---

## 1) 需要修改的现有条款（按文件归类）

### A. Layer 0：`rbf-format.md`

- **`[F-FRAME-LAYOUT]`（必须修改）**
   - Pad 字段：`0-3` → `1-4`。
   - Pad 语义：从“全 0 的对齐填充”升级为“首字节为帧状态，其余为对齐填充”。
   - 建议将字段命名从 `Pad` 明确为 `PadBytes`（或维持 `Pad` 但在说明中声明 `Pad[0]` 的语义），以避免“Pad 必全 0”这类旧约束残留。

- **`[F-FRAMETAG-WIRE-ENCODING]`（必须修改）**
   - 删除/替换：`FrameTag = 0x00000000` 为 RBF 保留值（Padding/墓碑帧）。
   - 改为：FrameTag 仅为“上层分类 discriminator”，RBF 不再保留 `0` 作为墓碑。

- **`[F-PAD-ZERO-FILL]`（必须修改/改写）**
   - 现条款“Pad 字节 MUST 全为 0”将与新方案直接冲突。
   - 改为：`Pad[0]` 为状态字节；`Pad[1..]` MUST 为 `0x00`；并对 `PadLen` 范围作硬约束。

- **`[F-PADLEN-FORMULA]`（必须修改）**
   - 从产生 `0..3` 的公式，改为产生 `1..4` 的公式（见 §3）。

- **`[F-HEADLEN-FORMULA]`（必须修改）**
   - 常数项仍可保持 `16 + PayloadLen + PadLen`，但要更新：PadLen 最小值由 `0` 变为 `1`，因此最小 `HeadLen` 将从 `16` 上升。

- **`[F-FRAMING-FAIL-REJECT]`（必须修改）**
   - 引用项中关于 Pad 的校验逻辑需要替换（不再是“Pad 全 0”，而是“Pad[0] 合法 + Pad[1..] 全 0 + PadLen 合法”）。

- **`[R-REVERSE-SCAN-ALGORITHM]`（建议同步修订，避免注释与最小长度漂移）**
   - 本提案会把“最小 FrameBytes”从 16B 提升到 20B（因为最小 PadLen=4 when PayloadLen=0，见 §3）。
   - 现算法文本中的“最小 FrameBytes”注释应同步更新为 20B，并把相关阈值说明与布局一致。
   - 另外：是否在 reverse scan 阶段过滤墓碑帧属于接口层策略（见 §2.B），Layer 0 的扫描算法建议仍“只负责 framing/CRC 验证，不负责业务过滤”。

### B. Layer 1：`rbf-interface.md`

- **`[F-FRAMETAG-DEFINITION]`（必须修改）**
   - 保留值表中 `0x00000000 = Padding（可丢弃帧）` 将失效。
   - 建议变更为：`FrameTag` 无保留值（或仅保留为“上层可选约定”，但不再承载 Auto-Abort）。

- **`[F-FRAMETAG-PADDING-VISIBLE]` 与 `[S-STATEJOURNAL-PADDING-SKIP]`（必须修改/替换语义对象）**
   - 这两条当前围绕 “Padding frame（FrameTag=0）” 建立职责边界。
   - 在新方案下，应改为围绕 “Tombstone frame（FrameStatus=Tombstone）”。

- **`[S-RBF-BUILDER-AUTO-ABORT]`（必须修改：物理保底路径）**
   - 当前物理保底路径为“覆写 FrameTag 为 Padding(0)”。
   - 在新方案下应改为：写出一个 framing/CRC 完整的帧，但在尾部 Pad 的状态字节写 Tombstone。
   - 同时需要明确：Abort 生成的墓碑帧，其 FrameTag 是否保留原 tag 仅用于诊断（建议 SHOULD 保留，但禁止上层依赖）。

- **`[A-RBF-FRAME-REF-STRUCT]`（建议修改：暴露状态以便上层过滤）**
   - 若上层需要“跳过 tombstone”，接口面必须给到可观测字段。
   - 建议在 `RbfFrame` 增加 `FrameStatus Status { get; }`（或 `byte StatusByte`）。

---

## 2) 需要新增的条款（建议条款 ID + 建议内容）

为避免“旧条款改写导致语义漂移不可追溯”，建议新增以下最小条款集：

### A. Layer 0：`rbf-format.md`

- **建议新增：`[F-FRAMESTATUS-BYTE]`**
   - 定义：`Pad` 的首字节为 **FrameStatusByte**。
   - 约束：该字节属于 FrameBytes，位于 Payload 之后、TailLen 之前。

- **建议新增：`[F-FRAMESTATUS-VALUES]`**
   - `0x00` = Normal（普通帧）
   - `0xFF` = Tombstone（墓碑帧）
   - `0x01..0xFE` = Reserved
   - **关键：必须明确 Reserved 的处理策略**（二选一必须钉死，见“修改建议”）。

- **建议新增：`[F-PADLEN-RANGE]`**
   - `PadLen` MUST 在 `1..4`。
   - `PadLen` MUST 满足对齐：`(PayloadLen + PadLen) % 4 == 0`。

- **建议新增：`[F-PAD-ZERO-FILL-EXCEPT-STATUS]`**
   - `Pad[1..PadLen-1]` MUST 全为 `0x00`。
   - 目的：保留“对齐填充可预测”的性质，同时允许状态字节。

### B. Layer 1：`rbf-interface.md`

- **建议新增：`[F-FRAMESTATUS-DEFINITION]`（术语/接口类型）**
   - 定义一个对外类型（或最小暴露为 byte）：
      - `public readonly record struct FrameStatus(byte Value);` 或 `enum FrameStatus : byte { Normal=0x00, Tombstone=0xFF }`
   - 声明：RBF 层仅解释 `Normal/Tombstone`；其余为 Reserved。

- **建议新增：`[S-RBF-TOMBSTONE-VISIBLE]`（替代/对齐旧的 Padding Visible）**
   - `IRbfScanner` MUST 产出所有通过 framing/CRC 校验的帧，包括 Tombstone。
   - 理由：诊断价值 + 职责边界清晰（Scanner 不做业务过滤）。

- **建议新增：`[S-STATEJOURNAL-TOMBSTONE-SKIP]`（替代旧的 Padding Skip）**
   - 上层 Record Reader MUST 忽略 Tombstone 帧，不将其解释为业务记录。
   - 这条必须在接口层钉死，否则“墓碑过滤责任”会在实现中漂移。

---

## 3) 公式需要如何修改（`[F-HEADLEN-FORMULA]` / `[F-PADLEN-FORMULA]`）

### `[F-PADLEN-FORMULA]`（建议替换为 1..4 版本）

目标：PadLen ∈ {1,2,3,4}，且 `(PayloadLen + PadLen) % 4 == 0`。

建议公式：

```
PadLen = 1 + ((4 - ((PayloadLen + 1) % 4)) % 4)
```

解释（规范可简写）：先强制放入 1 字节状态位，再用 0..3 字节补齐到 4B 对齐。

### `[F-HEADLEN-FORMULA]`

若继续把 `PadLen` 作为“状态字节 + 对齐填充”的总长度，则 HeadLen 仍可保持结构不变：

```
HeadLen = 16 + PayloadLen + PadLen
```

但需同步注明：
- `PadLen` 的取值域已变为 `1..4`；因此 `HeadLen >= 20`（当 PayloadLen=0 或 4k+0 的边界）。

---

## 4) CRC 覆盖范围是否需要调整？

结论：**不需要调整覆盖范围，但需要更新术语表述以避免歧义**。

- 现条款 `[F-CRC32C-COVERAGE]` 为 `crc32c(FrameTag + Payload + Pad + TailLen)`。
- 在新方案下，`Pad` 已包含 `FrameStatusByte`，因此 **墓碑标记天然被 CRC 保护**。

建议仅做文字层面的明确：
- 将“Pad”描述为“`FrameStatusByte + PadZeroFill`”，避免读者误以为“Pad 仍全 0”。

---

## 5) 对 Claude 建议的“FrameStatus”术语看法（审计视角）

总体意见：**赞成采用 `FrameStatus`，但需要防止它在 Layer 0/Layer 1 发生“命名-职责漂移”**。

审计要点：
- `FrameStatus` 这个词本身是“可扩展容器名”，容易诱导未来把更多语义塞进同一个字节，导致上层实现出现分叉。
- 因此必须配套两条硬约束：
   1) **只承诺两态**（Normal/Tombstone），其余值如何处理必须写死（fail-fast or forward-compatible policy）。
   2) 该字节属于 Layer 0 framing 元信息；上层不得把它当业务字段（不能用它替代 FrameTag/RecordKind）。

命名替代案（若团队担心“Status”过宽）：`FrameValidity` / `FrameDisposition`。
但在我看来，`FrameStatus` 在可读性与对齐 `FrameTag` 的对称性上更优，只要条款把边界钉死即可。

---

## 修改建议（收敛为可执行决策点）

### D1. Reserved 状态值处理策略必须二选一（避免实现分叉）

对 `0x01..0xFE`：
- **方案 A（更安全，推荐）**：Reader MUST 将其视为 framing failure（等价损坏帧），进入 Resync。
   - 代价：未来扩展需要升级 Reader。
   - 收益：避免旧 Reader 把“未来语义”当普通帧处理而造成静默错误。

- **方案 B（更前向兼容）**：Reader MUST 接受 Reserved 值（framing 通过），但 MUST 将其暴露给上层，并由上层决定如何处理。
   - 风险：若上层未更新，可能误处理本应特殊对待的帧。

从“无版本字段 + 以安全为先”的前提看，我建议采用 **方案 A**。

### D2. 接口层必须把“跳过 Tombstone”写成条款，而不是实现习惯

否则会出现：Scanner/Reader/StateJournal 各自“以为对方会跳过”的责任漂移。

### D3. Auto-Abort 的物理路径要写成后置条件可测试

建议在 `[S-RBF-BUILDER-AUTO-ABORT]` 中明确可断言的后置条件：
- Abort 产生的帧 MUST 通过 framing/CRC（否则会被 Resync 丢弃，导致“可继续写入”的后置条件在部分实现中变得不可测试）。
- Abort 后 MUST 可继续追加后续帧（已有）。

（以上三点如果不钉死，最容易在实现阶段产生“兼容但不同”的分叉。）

### Advisor-Gemini 发言

我是 Advisor-Gemini，从 **DX (开发者体验)** 和 **交互隐喻** 的角度进行评估。

#### 1. 写入端体验：从 "Undo" 到 "Seal" (From Undo to Seal)

这个方案最大的 DX 胜利在于**修正了 `Abort()` 的心智模型**。

*   **旧方案 (Seek & Erase)**：`Abort()` 感觉像是一个 "Undo" 操作——"哎呀写错了，回去涂掉"。这在 Append-Only 的流式写入（如网络流、压缩流）中是极其痛苦的，甚至是不可能的。
*   **新方案 (Seal as Void)**：`Abort()` 变成了一个 "Commit" 操作——"我写完了，但我决定废弃它"。
    *   这符合 **事务 (Transaction)** 的隐喻：`Commit(Valid)` 和 `Commit(Void)` 都是向流中追加一个终结符。
    *   这对实现者来说是 **"Pit of Success"**：你不需要维护复杂的"头部位置指针"来支持回滚，你只需要在最后决定盖什么章。

#### 2. 读取端体验：纯净的 FrameTag (Pure FrameTag)

我非常赞同 Claude 关于"关注点分离"的观点，这对消费端代码的整洁度有直接贡献。

*   **代码示能性 (Code Affordance)**：
    *   **Before**: `if (frame.Tag == 0) continue; switch (frame.Tag) { ... }` —— 开发者必须时刻警惕 `0` 这个特殊值。
    *   **After**: `if (frame.Status != Valid) continue; switch (frame.Tag) { ... }` —— `FrameTag` 变成了纯粹的业务枚举，不再包含 Layer 0 的噪音。
*   **类型安全**: 在强类型语言中，`FrameTag` 可以是一个纯粹的 `enum`，而不需要包含 `Padding = 0` 这种尴尬的成员。

#### 3. 调试体验：墓碑的视觉识别 (Visualizing the Tombstone)

关于"尾部标记是否难发现"的担忧：

*   **视觉韵律**: RBF 的帧结构是以 `TailLen` (4B) 结尾的。在 Hex Dump 中，`TailLen` 是帧与帧之间的"路标"。
*   **红旗法则**: `Pad` 紧邻 `TailLen`。如果我们在 `Pad[0]` 使用 `0xFF` (Tombstone)，它会在一堆 `00` (Padding) 和 `TailLen` 之间形成一个刺眼的 **Visual Glitch**。
    *   `... Payload ... [FF 00 00] [TailLen] ...`
    *   这就像给帧贴了一个鲜红的 "VOID" 标签。虽然它在尾部，但因为它紧挨着导航路标 (`TailLen`)，所以调试者在跳跃浏览时很容易扫到它。

#### 4. 术语与隐喻：FrameStatus

支持 `FrameStatus`。

*   **API 隐喻**: `frame.Status` 暗示了这是帧的**元属性 (Meta-property)**，而 `frame.Tag` 是帧的**内容属性 (Content-property)**。
*   **扩展性隐喻**: 如果未来我们需要 "Soft Delete" (软删除) 或 "Priority" (优先级)，`Status` 字段天然容纳这些概念。

#### 5. 补充建议：Pad 的命名

虽然字段叫 `Pad`，但在代码实现层面，建议将其拆解，以提升 DX：

```csharp
// 推荐的结构体属性设计
public struct RbfFrame {
    public FrameTag Tag;
    public FrameStatus Status; // 映射自 Pad[0]
    public ReadOnlySpan<byte> Payload;
    // PadBytes 隐藏在底层，不直接暴露给业务层
}
```

**总结**：
这个方案将 "Abort" 从一个**物理回退**动作变成了一个**逻辑标记**动作，极大地降低了 Writer 的实现门槛，同时净化了 Reader 的业务逻辑。**强烈支持**。

---

### 主持人收敛总结 (刘德智)

感谢三位顾问的深入分析！经过这轮畅谈，我们达成了高度共识。让我总结要点：

---

## 共识清单

### ✅ 已达成共识

| # | 共识点 | 来源 |
|---|--------|------|
| 1 | **采纳尾部 Pad 标记方案** | 三方一致支持 |
| 2 | **Pad 长度从 0~3 改为 1~4** | 监护人原案 + GPT 公式确认 |
| 3 | **术语命名为 `FrameStatus`** | Claude 提议，Gemini/GPT 支持 |
| 4 | **值定义**：`0x00=Normal`, `0xFF=Tombstone` | Claude 提议，GPT 确认 |
| 5 | **CRC 覆盖范围不变**（Pad 已在 CRC 内） | Claude 确认，GPT 审计 |
| 6 | **逆向扫描算法核心逻辑不变**，仅更新最小长度注释 | Claude/GPT 一致 |
| 7 | **Scanner 仍产出所有帧（含 Tombstone）**，过滤责任在 Layer 1 | Claude 提议，GPT 条款化 |

### ⚠️ 需要决策点

| # | 问题 | 候选方案 | GPT 推荐 |
|---|------|----------|----------|
| D1 | Reserved 值（`0x01~0xFE`）处理策略 | A) Fail-fast B) Forward-compatible | **方案 A** |
| D2 | Abort 帧的 FrameTag 是否保留原值 | SHOULD 保留（诊断用），但上层禁止依赖 | 待确认 |

---

## 待修改条款清单（GPT 审计结果）

### Layer 0: rbf-format.md

| 条款 | 状态 | 修改内容 |
|------|------|----------|
| `[F-FRAME-LAYOUT]` | 必须修改 | Pad: 0~3 → 1~4；首字节语义 |
| `[F-FRAMETAG-WIRE-ENCODING]` | 必须修改 | 删除 `0x00000000` 保留值 |
| `[F-PAD-ZERO-FILL]` | 替换 | → `[F-PAD-ZERO-FILL-EXCEPT-STATUS]` |
| `[F-PADLEN-FORMULA]` | 必须修改 | 新公式产生 1~4 |
| `[F-HEADLEN-FORMULA]` | 必须修改 | 最小值从 16 → 17 |
| `[F-FRAMING-FAIL-REJECT]` | 必须修改 | Pad 校验逻辑更新 |
| `[R-REVERSE-SCAN-ALGORITHM]` | 同步更新 | 最小 FrameBytes 注释 |

**新增条款**：
- `[F-FRAMESTATUS-BYTE]` — 定义 Pad[0] 为 FrameStatus
- `[F-FRAMESTATUS-VALUES]` — 值定义（Normal/Tombstone/Reserved）
- `[F-PADLEN-RANGE]` — PadLen ∈ 1..4

### Layer 1: rbf-interface.md

| 条款 | 状态 | 修改内容 |
|------|------|----------|
| `[F-FRAMETAG-DEFINITION]` | 必须修改 | 删除 Padding 保留值 |
| `[F-FRAMETAG-PADDING-VISIBLE]` | 替换 | → `[S-RBF-TOMBSTONE-VISIBLE]` |
| `[S-STATEJOURNAL-PADDING-SKIP]` | 替换 | → `[S-STATEJOURNAL-TOMBSTONE-SKIP]` |
| `[S-RBF-BUILDER-AUTO-ABORT]` | 必须修改 | 物理路径改为写 Tombstone 状态 |
| `[A-RBF-FRAME-REF-STRUCT]` | 建议修改 | 增加 `Status` 属性 |

**新增条款**：
- `[F-FRAMESTATUS-DEFINITION]` — 接口类型定义

---

## 新 PadLen 公式（GPT 提供）

```
PadLen = 1 + ((4 - ((PayloadLen + 1) % 4)) % 4)
```

**验证表**：

| PayloadLen | PayloadLen % 4 | PadLen | Total (Payload+Pad) % 4 |
|------------|----------------|--------|-------------------------|
| 0 | 0 | 4 | 0 ✓ |
| 1 | 1 | 3 | 0 ✓ |
| 2 | 2 | 2 | 0 ✓ |
| 3 | 3 | 1 | 0 ✓ |
| 4 | 0 | 4 | 0 ✓ |

---

## 后续行动

1. **监护人决策**：确认 D1 (Reserved 处理策略) 和 D2 (Abort 帧 FrameTag)
2. **规范更新**：根据上述清单修改 rbf-format.md 和 rbf-interface.md
3. **测试向量**：更新 rbf-test-vectors.md 中的示例

---

## 会议状态

**状态**：✅ 已批准

---

## 监护人批示 (2025-12-24)

### D1: Reserved 值处理策略
**批准方案 A (Fail-fast)**

> 依据：省事先延后。虽然 Unknown 状态对用户更友好（类比：磁盘分区软件遇到不认识的分区显示 Unknown 比崩溃体验好），但定义起来麻烦，拖一拖先延后处理。

### D2: Abort 帧的 FrameTag
**批准 SHOULD 保留**

> 依据：能不耦合的问题就尽量不耦合。

### D3: 字节布局方案
**批准方案 A（3:0 投票通过）**

| 投票人 | 选择 | 理由 |
|--------|------|------|
| 监护人 | A | 实现最简，计算长度和选择值是两个独立过程 |
| Team Leader | A | 一次 memset，一次 peek，还有隐式冗余校验 |
| Advisor-Claude | A | "全息标记"概念最纯粹 + 隐式冗余校验是免费赠品 |

**最终布局**：

```
[Payload][FrameStatus 1-4B 全同值][TailLen 4B][CRC 4B]
```

- **命名**：`FrameStatus`（不再称为 Pad）
- **值**：`0x00` = Valid，`0xFF` = Tombstone
- **长度**：1-4 字节，保证 `(PayloadLen + FrameStatusLen) % 4 == 0`
- **填充规则**：全部字节填相同值

---

## 后续行动

- [x] 监护人批示完成
- [x] 更新 rbf-format.md（v0.12）
- [x] 更新 rbf-interface.md（v0.5）
- [x] 更新 rbf-test-vectors.md（v0.6）

---

