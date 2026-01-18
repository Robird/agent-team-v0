### Assistant
~~~md
现在开始第一轮实验——极简提示词： 
🛠️ runSubagent (toolu_013mEQcmh5RdqKW1jrpesAo6) {
  "agentName": "Craftsman.OpenRouter",
  "description": "E1: 极简提示词审阅",
  "prompt": "审阅以下两个文档的一致性：

- `/repos/focus/atelia/docs/Rbf/rbf-interface.md`
- `/repos/focus/atelia/docs/Rbf/rbf-format.md`

列出发现的问题。"
}
~~~

### Tool
~~~md
🛠️ toolu_013mEQcmh5RdqKW1jrpesAo6
下面是对  与  **一致性**的审阅问题清单（按影响程度排序）。

## Sev2（会影响索引/引用/协作的硬问题）

1. **Frontmatter 的 `docId` 冲突且  的 `docId` 明显写错**
   -  的 frontmatter：`docId: "rbf-format"`（它是 interface 文档，却标成 format 的 docId）
   -  的 frontmatter：同样是 `docId: "rbf-format"`
   - 结果：DocGraph/索引系统若以 `docId` 做唯一键，会发生覆盖或引用混乱；也会让“单一事实源(SSOT)”追踪变得不可靠。
   - 直观修复方向： 应该使用独立的 `docId`（例如 `"rbf-interface"`）。

2. ** 的“最近变更”段落 Markdown 被截断（文档结构损坏）**
   - 在  的变更表中，`0.16` 那一行的链接/括号未闭合，后续内容直接断掉（导致渲染、引用、甚至后续自动处理工具都可能异常）。
   - 这会让“接口契约的版本演进”无法可靠对齐  的版本演进。

3. **章节编号不连续（跨文档引用时容易产生“引用失真”）**
   - ：从 “## 5. 使用示例” 直接跳到 “## 7. 最近变更”，中间缺失 §6。
   - ：从 “## 8. DataTail 与截断” 跳到 “## 10. 近期变更”，缺失 §9。
   - 这类问题会让口头/会议纪要/评审意见中使用“§N”引用时产生歧义，尤其你们在文档里已经明确使用了“§3/§4/§6 是 SSOT”的叙述方式。

## Sev3（语义/表述层面的不一致或容易误读点）

4. ** 的文档关系表述有“实现/依赖方向”歧义**
   -  写的是：`rbf-format.md 实现 rbf-interface.md（下层 RBF 线格式实现接口定义）`
   - 但  的自述是“只定义 wire format，不定义接口类型，接口类型见 rbf-interface.md”。
   - 两者放一起读时，会让人困惑到底谁是“规范”、谁是“实现细节”：更自然的关系应该是：
     - ：定义上层可依赖的 API/语义边界；
     - ：定义该 API 所采用/要求的底层线格式约束（作为实现必须遵守的格式规范）。
   - 建议把“实现”改成更不容易误读的词（例如“采用/使用/由…支撑/线格式由…规定”）。

5. **标题/层级标签在两份文档之间不够自洽（容易误导读者定位）**
   - ：`title: "RBF Rule-Tier"`，正文标题是“RBF 二进制格式规范（Layer 0）”——相对一致。
   - ：`title: "RBF Shape-Tier"`，但 `docId` 又写成 `rbf-format`，且正文开头是 “RBF Layer Interface Contract / Layer 0/1 边界”。
   - 读者会产生困惑：它到底是 Shape-Tier 还是 Rule-Tier？是 “Layer 0/1 边界契约” 还是 “format 文档”？

6. **“SizedPtr 在 wire format 上的存在”在两文档中的定位略显混杂**
   - ：把 `SizedPtr` 作为“接口层核心句柄类型”，并给出位分配/范围/对齐与 `NullPtr` 语义。
   - ：专门加了 §7 “SizedPtr（Wire Format）”，并明确 `u64 LE` 紧凑编码、4B 粒度等。
   - 这本身并不矛盾，但目前读感上会像“接口类型细节”与“线格式细节”交叉出现：如果后续要严格分层，建议明确哪部分是**接口语义**（例如 NullPtr 语义、TryReadAt 行为），哪部分是**线格式编码**（u64 LE / bit packing / 粒度），避免读者在两份文档里来回拼图。

如果你希望我进一步做“可执行的修复清单（按最小改动修复 docId、章节编号、截断段落）”，我可以给出精确到文件与段落的修改建议。
~~~

### Assistant
~~~md
最后 E6——角色设定： 
🛠️ runSubagent (toolu_019JcmQHhecBP2KkZtDNNKGV) {
  "agentName": "Craftsman.OpenRouter",
  "description": "E6: 角色设定审阅",
  "prompt": "你是一位**准备实现 RBF 库的工程师**，正在阅读以下两份设计文档：

- `/repos/focus/atelia/docs/Rbf/rbf-interface.md`
- `/repos/focus/atelia/docs/Rbf/rbf-format.md`

你需要确保自己理解正确，不会因为文档描述的歧义而写出错误的代码。

请指出文档中**可能让你写错代码的地方**。只关注会导致实现错误的问题，不关注文档风格。"
}
~~~

### Tool
~~~md
🛠️ toolu_019JcmQHhecBP2KkZtDNNKGV
下面这些点会**直接诱发实现歧义/实现偏差**（可能写出“能跑但不符合规范、或在边界条件下会错”的代码）。我只列会影响实现正确性的，不管文档风格。

---

## 1) `SizedPtr` 的“bit 数 vs 字节语义 vs 4B 粒度”容易实现错
**位置**
-  §2.3 `[F-SIZEDPTR-DEFINITION]`：`OffsetBytes 38-bit`、`LengthBytes 26-bit`，且要求 `% 4 == 0`
-  §7.1 `[F-SIZEDPTR-WIRE-FORMAT]`：明确写了“38-bit，4B 粒度”“26-bit，4B 粒度”

**风险**
- 如果按字面把 `OffsetBytes` / `LengthBytes` 当作“**直接用 bit 存 bytes**”，那 **38-bit offset 只能到 256GB**，与文档写的“~1TB”不一致；同理 length 的 “~256MB”也只有在**按 4B 单位编码**时才成立（$2^{26}\cdot 4\text{B}=256\text{MB}$）。
- 最容易写错的地方是：**Packed 内到底存的是 `bytes` 还是 `bytes/4`**？接口属性名叫 `OffsetBytes` 更像是 bytes；格式文档又强调 4B 粒度。

**会导致的错误实现**
- 写入/读取 `SizedPtr` 时 shift 搞反（`<<2` / `>>2`），导致定位到错误地址或长度，扫描/随机读全部失效但表面看像“CRC 失败”。
- Wire format（u64 LE）序列化/反序列化时，把 Packed 当成 host-endian 或把 bytes/units 混用。

**建议需要澄清/落锤的规则**
- 规范最好明确一句：Packed 的 bitfield 存的是 `OffsetQuads = OffsetBytes/4` 与 `LengthQuads = LengthBytes/4`，属性 `OffsetBytes/LengthBytes` 返回 `Quads*4`。

---

## 2) Reverse Scan 算法里对 `HeadLen` 下限的检查与 §3.3/§6.1 自相矛盾
**位置**
-  §3.3 `[F-HEADLEN-FORMULA]`：最小 HeadLen = 20（payload=0 时 statusLen=4）
-  §6.1 `[R-REVERSE-SCAN-ALGORITHM]`：
  - 先用 `recordEnd < HeaderFenceLen + 20` 注释“最小 FrameBytes=20”
  - 但后面又写：`若 headLen != tailLen 或 headLen % 4 != 0 或 headLen < 16: continue`

**风险**
- `headLen < 16` 这个阈值与“最小 20”矛盾，会诱发实现者照抄算法，导致**接受本不合法的帧长度**（尤其是文件中间位置的帧——它们不会被前面的 `recordEnd < HeaderFenceLen+20` 拦住）。

**会导致的错误实现**
- 可能把 `HeadLen==16` 这类非法布局当成候选，随后出现越界读取、CRC 覆盖范围错误、或“偶发通过”造成数据污染。

---

## 3) §6 的 Reverse Scan 算法没有落实 §5 声明的 Framing 校验集合，容易让实现“少验关键项”
**位置**
-  §5 `[F-FRAMING-FAIL-REJECT]`：要求验证 FrameStatus 位域合法、填充一致、HeadLen 公式一致、4B 对齐、Fence 匹配、HeaderFence 等
-  §6.1 算法：实际只做了 fence 匹配、(headLen==tailLen)、对齐、CRC；**没有**解析/验证 FrameStatus 的一致性与 reserved bits、也没有显式验证 `[F-HEADLEN-FORMULA]`（16+payload+status）这一组约束

**风险**
- 文档又把“§6 扫描算法”列为 SSOT（§1），实现者很容易把 §6 当成“完整验收逻辑”，从而漏掉 §5 的强制校验点。

**会导致的错误实现**
- 扫描器可能把 reserved bits 非零或 status bytes 不一致的帧当作有效帧（只要 CRC 碰巧对上或被攻击构造），违反 `[F-FRAMESTATUS-VALUES]` / `[F-FRAMESTATUS-FILL]`。
- 更现实的问题：实现者可能根本不解析 `FrameStatus`，从而无法正确给出 `IsTombstone`。

---

## 4) `FrameStatus` 的解析“从哪儿读到 status 字节”没有明确给出步骤，容易写错边界
**位置**
-  §3.2 `[F-FRAME-LAYOUT]`：FrameStatus 在 Payload 后、TailLen 前，长度 1-4
- §3.2 `[F-FRAMESTATUS-VALUES]`：status 低 2 bit 编 StatusLen，且所有字节相同
- §3.3 `[F-STATUSLEN-FORMULA]`：StatusLen 由 PayloadLen mod 4 推导

**风险**
- 实际实现时会遇到“先有鸡还是先有蛋”：
  - 要知道 PayloadLen 才能算 StatusLen（公式）
  - 但要知道 StatusLen 才能分出 PayloadLen（从 HeadLen 拆出来）
  - status 自身又编码了 StatusLen（低 2 bit）
- 文档目前没有明确告诉你**推荐/规范的解法**：例如“从 `recordEnd-9` 读最后一个 status 字节，先取 StatusLen，再回推 status 起点与 payloadLen，并交叉验证公式”。

**会导致的错误实现**
- 以错误的起点读取 status，导致把 payload 尾部当 status；CRC 仍可能通过（因为 CRC 覆盖 payload+status），从而“看似正确但 Tombstone 判断错/ payload 切分错”。

---

## 5) `SizedPtr.LengthBytes` 是否**必须等于** `HeadLen`（FrameBytes 总长）没被用强规范语句钉死
**位置**
-  §2.3：LengthBytes = “Frame 的字节长度（含 HeadLen 到 CRC32C）”
-  §3.2：HeadLen = FrameBytes 总长度（不含 Fence）

**风险**
- 语义上两者应该相等（`ptr.LengthBytes == HeadLen == TailLen`），但接口文档里没有用 MUST 明确写出这个等式关系。
- 这会影响 `TryReadAt(SizedPtr)`：如果调用方传的 length 与文件里 headLen 不一致，应该怎么处理？

**会导致的错误实现**
- `TryReadAt` 只按 `ptr.LengthBytes` 读，不核对 headLen/tailLen：可能读到半帧/跨帧，出现“偶发 CRC 失败”或更糟的数据混读。
- 或反过来完全忽略 ptr.LengthBytes，只用 headLen：那 ptr.LengthBytes 的存在就变成误导，调用方传错 length 也不被发现。

---

## 6) `TryReadAt` 是否必须做“前后 Fence 校验”没有写清楚，可能导致随机读不安全
**位置**
-  §3.1/§3.2：FrameBytes 位于 `[Fence][FrameBytes][Fence]`
-  §5 `[F-FRAMING-FAIL-REJECT]`：列了 “Fence 匹配” 作为 framing 校验
-  §4.1：`TryReadAt(ptr, out frame)` 只说“是否成功读取”，没写它到底要验证哪些约束

**风险**
- 随机读场景中，如果不验证“前一个 fence / 后一个 fence”，就可能把文件中任意位置的 bytes（碰巧满足 headLen/tailLen/CRC）当成帧。概率不高，但这是日志/恢复系统里典型的“低概率灾难”。

**会导致的错误实现**
- `TryReadAt` 成功返回一个“伪帧”，上层据此做恢复决策时会产生不可逆错误。

---

## 7) `IRbfFramer.Flush()` 与 open `RbfFrameBuilder` 的“不可观测”约束，在实现策略上有歧义
**位置**
-  `[S-RBF-BUILDER-FLUSH-NO-LEAK]`：open builder 时 Flush MUST NOT 使任何未 Commit 的字节对下游/扫描器可观测
- 同时 `RbfFrameBuilder` 的定位又暗示可能做“流式写入 payload”

**风险**
- 如果实现者为了支持超大 payload，选择“builder 期间就把 payload 直接写进 data 文件”，那即使不写尾部 Fence，**这些字节对扫描器是否算“可观测”**？  
  - 从严格字面理解：写进文件就可观测（文件长度变了，mmap 能看见，崩溃恢复时也可能被 resync 扫到）。
  - 但 builder 设计又常见于“边写边算 CRC”，很诱导人直接落盘。

**会导致的错误实现**
- Flush 或进程崩溃后，扫描器可能在 resync 中把未提交的 payload 内部某段误判成 fence 入口（虽然 CRC 把概率压低，但规范这里想要的是“零可见”，不是“概率很低”）。
- 也会让 “Auto-Abort SHOULD Zero I/O” 的优化空间与实现行为变得混乱。

---

## 8) `DataTail` 复用 `SizedPtr.OffsetBytes`：会诱发“把 DataTail 当帧指针验证/对齐”的错误
**位置**
-  §7.2：DataTail 用 `SizedPtr.OffsetBytes` 表达“文件截断点（length 无意义）”
-  §8：DataTail MUST 包含尾部 Fence（有效 EOF）

**风险**
- `SizedPtr` 在接口语义上是“帧句柄”，offset 指向 HeadLen 字段；而 DataTail 是“EOF”，它**不一定**是任何帧的 HeadLen 位置（它应该落在 fence 边界上）。
- 如果实现里复用同一套 `SizedPtr` 校验逻辑（例如“offset 必须指向一个可解析帧”），会把合法的 DataTail 当非法；反之也可能把非法 tail 当合法。

---

### 你如果准备马上写代码，我建议你先对齐 3 个“必须钉死”的裁决
1) `SizedPtr` packed 的单位到底是不是 quads（/4）？属性返回 bytes 还是 units？wire u64 LE 的具体 bit 排布/shift。  
2) `TryReadAt` 的“成功”必须包含哪些 framing 校验（至少：head==tail、status 合法与一致、CRC、前后 fence、对齐、HeaderFence 之后）。  
3) Reverse scan 以 §5 为准还是以 §6 伪码为准？（尤其是 headLen 下限与 status 校验是否必须在扫描中做。）

如果你希望我把这些问题整理成一份**最小修改的 spec patch 清单**（只改会导致实现出错的句子/阈值/伪码），我可以按条款编号给出具体改法。

[copilot_cache_control: { type: 'ephemeral' }]
~~~


### Assistant
~~~md
开始 C1——极简提示词： 
🛠️ runSubagent (toolu_01N9fpfwhwNhzYMTszQWgvWs) {
  "agentName": "Curator",
  "description": "C1: 极简提示词审阅",
  "prompt": "审阅以下两个文档的一致性：

- `/repos/focus/atelia/docs/Rbf/rbf-interface.md`
- `/repos/focus/atelia/docs/Rbf/rbf-format.md`

列出发现的问题。"
}
~~~

### Tool
~~~md
🛠️ toolu_01N9fpfwhwNhzYMTszQWgvWs
现在我已经阅读了两个文档。让我仔细分析它们之间的一致性问题。

---

## 审阅发现：rbf-interface.md 与  一致性问题

### 🔴 问题 1：最小 HeadLen 数值矛盾

**证据**：
-  §6.1 逆向扫描算法中：`recordEnd < HeaderFenceLen + 16` — 暗示最小 FrameBytes = 16
-  §3.3 `[F-HEADLEN-FORMULA]` 注释：「最小 HeadLen = 20」
-  §6.1 逆向扫描注释：「最小 FrameBytes = 20（PayloadLen=0, StatusLen=4）」

**矛盾**：rbf-interface.md 使用 16 作为阈值，rbf-format.md 明确声明最小值是 20。

**建议**：将  中的 `16` 改为 `20`。

---

### 🔴 问题 2：ScanReverse 终止条件缺少 FrameStatus 校验

**证据**：
-  §5 `[F-FRAMING-FAIL-REJECT]` 明确要求校验：
  - `[F-FRAMESTATUS-VALUES]`：FrameStatus 位域合法（IsMvpValid）
  - `[F-FRAMESTATUS-FILL]`：FrameStatus 所有字节一致
-  §6.1 `[R-REVERSE-SCAN-ALGORITHM]` 伪代码：
  - 校验了 HeadLen/TailLen、CRC、Fence、对齐
  - **未提及 FrameStatus 位域校验**

**矛盾**：§5 声明 FrameStatus 校验是 MUST，但 §6 的 SSOT 算法中未体现。

**建议**：在 `[R-REVERSE-SCAN-ALGORITHM]` 伪代码中增加 FrameStatus 校验步骤。

---

### 🟡 问题 3：文档关系声明的自指错误

**证据**：
-  文档关系表声明自己是「Layer 0/1 边界」
- 但 frontmatter 中 `docId: "rbf-format"`（两个文件 docId 相同）
-  frontmatter 也是 `docId: "rbf-format"`

**问题**：两个不同文件共用同一个 docId，可能导致 DocGraph 索引冲突。

**建议**：将  的 docId 改为 `"rbf-interface"`。

---

### 🟡 问题 4：SizedPtr 位分配描述细节不一致

**证据**：
-  §2.3 SizedPtr 表格：
  - `OffsetBytes`：38-bit，~1TB
  - `LengthBytes`：26-bit，~256MB
-  §7.1 `[F-SIZEDPTR-WIRE-FORMAT]`：
  - 提到「38-bit，4B 粒度」和「26-bit，4B 粒度」

**潜在矛盾**：
-  说 `OffsetBytes` 范围是 ~1TB（= $2^{38}$ bytes），但如果是 4B 粒度，则应为 ~4TB（= $2^{38} \times 4$ bytes）
-  提到 4B 粒度但未给出范围数值

**建议**：统一表述粒度含义。如果 38-bit 存储的是 4B 对齐的偏移量（实际偏移 = 存储值 × 4），则 ~1TB 描述不准确，应为 ~1TB 存储空间（$2^{40}$ bytes = 1TB，对应 38-bit × 4）。

---

### 🟡 问题 5：rbf-interface.md §6 被截断

**证据**：
-  从 §5 直接跳到 §7
- 缺少 §6

**问题**：章节编号不连续，可能是删除某节后忘记重新编号。

**建议**：将 §7、§8 分别改为 §6、§7。

---

### 🟢 问题 6：TryReadAt 校验要求未明确

**证据**：
-  `TryReadAt` 方法只说「读取指定位置的帧」，返回 `bool`
-  §5 定义了 Framing/CRC 校验规则

**问题**：rbf-interface.md 未声明 `TryReadAt` 是否执行完整的 Framing/CRC 校验。如果 `SizedPtr` 来自上层索引而非扫描，实现者可能困惑是否需要重新校验。

**建议**：在 `TryReadAt` 的文档注释中明确：「返回 false 表示位置无效或 Framing/CRC 校验失败」。

---

## 汇总

| 严重性 | 问题 | 位置 | 修复建议 |
|:-------|:-----|:-----|:---------|
| 🔴 高 | 最小 HeadLen 数值矛盾（16 vs 20） |  §6.1 | 改 16 → 20 |
| 🔴 高 | Reverse Scan 缺少 FrameStatus 校验步骤 |  §6.1 | 补充校验步骤 |
| 🟡 中 | docId 重复（两文件都是 `rbf-format`） |  frontmatter | 改为 `rbf-interface` |
| 🟡 中 | SizedPtr 范围与粒度表述不一致 | 两文件 §2.3/§7.1 | 统一措辞 |
| 🟡 中 | 章节编号缺 §6 |  | 重编章节号 |
| 🟢 低 | TryReadAt 校验语义未明确 |  §4.1 | 补充文档注释 |
~~~

### Assistant
~~~md
继续 C6——角色设定（最关键的对比）： 
🛠️ runSubagent (toolu_01P1wGbERDLUvvYpRRgoc7tP) {
  "agentName": "Curator",
  "description": "C6: 角色设定审阅",
  "prompt": "你是一位**准备实现 RBF 库的工程师**，正在阅读以下两份设计文档：

- `/repos/focus/atelia/docs/Rbf/rbf-interface.md`
- `/repos/focus/atelia/docs/Rbf/rbf-format.md`

你需要确保自己理解正确，不会因为文档描述的歧义而写出错误的代码。

请指出文档中**可能让你写错代码的地方**。只关注会导致实现错误的问题，不关注文档风格。"
}
~~~

### Tool
~~~md
🛠️ toolu_01P1wGbERDLUvvYpRRgoc7tP
我已阅读了两份设计文档和我的认知入口。现在我以"准备实现 RBF 库的工程师"视角，分析这两份文档中可能让我写错代码的地方。

---

## 审阅结论：可能导致实现错误的 8 个问题

### 1. SizedPtr 4B 粒度 vs 字节粒度歧义

**位置**： `[F-SIZEDPTR-DEFINITION]`

文档说：
> | 属性 | 位数 | 范围 | 说明 |
> | `OffsetBytes` | 38-bit | ~1TB | 指向 Frame 起点 |
> | `LengthBytes` | 26-bit | ~256MB | Frame 的字节长度 |

但  说：
> - **Offset**：指向 Frame 的 `HeadLen` 字段起始位置（38-bit，**4B 粒度**）。
> - **Length**：Frame 的字节长度（26-bit，**4B 粒度**）。

**问题**：属性名叫 `OffsetBytes` / `LengthBytes` 暗示"单位是字节"，但 wire format 说"4B 粒度"。那 `OffsetBytes` 返回的是**实际字节偏移**（内部存储的值 × 4）还是**原始编码值**？

**如果我理解错了会怎样**：我可能把 `OffsetBytes = 100` 当成"偏移 100 字节"去读，实际应该是"偏移 400 字节"，导致读取错位。

**建议**：明确 `OffsetBytes` / `LengthBytes` 是**解码后的字节值**（调用者无需乘 4），还是原始编码值（调用者需乘 4）。

---

### 2. Reverse Scan 最小 HeadLen 不一致

**位置**：
-  §3.3 说：最小 `HeadLen = 20`
-  §6.1 算法 Step 3c 说：`recordEnd < HeaderFenceLen + 20`，但注释说 `// 最小 FrameBytes = 20`

**问题**：算法检查的 `20` 是 `最小 FrameBytes` 还是 `最小 HeadLen`？按公式它们是同一个值，但注释混用了术语，让我不确定边界条件。

**如果我理解错了会怎样**：边界条件检查写错，可能误拒绝合法的最小帧或误接受非法帧。

**建议**：统一术语：要么都叫 `FrameBytes`，要么都叫 `HeadLen`。

---

### 3. Reverse Scan 缺少 FrameStatus 校验

**位置**： `[R-REVERSE-SCAN-ALGORITHM]`

算法只校验了：
- HeadLen == TailLen
- HeadLen % 4 == 0
- HeadLen >= 16
- CRC32C

但 **没有校验**：
- `[F-FRAMESTATUS-VALUES]` 位域合法性（`IsMvpValid`）
- `[F-FRAMESTATUS-FILL]` 所有字节一致

而 §5 `[F-FRAMING-FAIL-REJECT]` 明确要求：
> Reader MUST 验证以下条款...`[F-FRAMESTATUS-VALUES]`、`[F-FRAMESTATUS-FILL]`...

**如果我理解错了会怎样**：我按 §6 的伪代码实现，结果漏掉了 FrameStatus 校验，违反了 §5 的 MUST。

**建议**：在算法伪代码 Step 3c 中显式增加 FrameStatus 校验步骤，与 §5 保持一致。

---

### 4. FrameStatus 解析步骤未在算法中出现

**位置**：

算法伪代码中从未读取或解析 `FrameStatus` 字段。我不知道：
- 什么时候读 FrameStatus？
- 如何从 `HeadLen` 推导出 `PayloadLen` 和 `StatusLen` 的边界？

**问题**：`HeadLen = 16 + PayloadLen + StatusLen`，但给定 `HeadLen`，我无法唯一确定 `PayloadLen`（因为 `StatusLen ∈ {1,2,3,4}`）。

§3.2 说 FrameStatus 采用位域编码，`StatusLen = (status & 0x03) + 1`。这意味着我需要**先读 FrameStatus 的最后一个字节**才能知道 `StatusLen`——但我不知道 FrameStatus 在哪里结束，这是鸡生蛋问题。

**如果我理解错了会怎样**：我可能无法正确解析 Frame 的字段边界。

**建议**：补充"如何从 HeadLen 推导出 Payload/FrameStatus 边界"的解析步骤（提示：先读 `TailLen - 4` 处的 CRC 前一个字节作为 status，再推导）。

---

### 5. `LengthBytes == HeadLen` 的等式未钉死

**位置**：
- ：`LengthBytes` 是 "Frame 的字节长度（含 HeadLen 到 CRC32C）"
- ：`HeadLen = 16 + PayloadLen + StatusLen`

**问题**：`LengthBytes` 描述的 "Frame 字节长度" 是否**等于 HeadLen**？如果是，为什么要两个概念？如果不是，差异在哪里？

**如果我理解错了会怎样**：我可能在计算 Frame 边界时出错。

**建议**：显式声明 `SizedPtr.LengthBytes == Frame 的 HeadLen`（或不等于，说明差异）。

---

### 6. `TryReadAt` 需要哪些校验？

**位置**： `IRbfScanner.TryReadAt`

接口说：
> 读取指定位置的帧。返回是否成功读取。

**问题**：`TryReadAt(ptr, out frame)` 应该做哪些校验？
- 只做 CRC 校验？
- 还是做完整的 `[F-FRAMING-FAIL-REJECT]` 全套校验？
- `ptr.LengthBytes` 与实际读到的 `HeadLen` 不一致时怎么办？

**如果我理解错了会怎样**：我可能校验不足（安全漏洞）或校验过度（拒绝本应接受的帧）。

**建议**：在 `TryReadAt` 的 remarks 中明确返回 `false` 的条件（建议引用 `[F-FRAMING-FAIL-REJECT]` + `[F-CRC-FAIL-REJECT]`）。

---

### 7. `Flush` 语义与 Buffer 边界

**位置**： `IRbfFramer.Flush()`

文档说：
> 将 RBF 缓冲数据推送到底层 Writer/Stream。不承诺 Durability。

**问题**：`Flush()` 后，刚写入的帧是否立即对 `IRbfScanner` 可见？
- 如果 `Framer` 和 `Scanner` 操作同一文件，`Flush()` 是否保证 `Scanner` 能读到？
- 还是说"可见性"取决于底层实现（例如 mmap 刷新时机）？

**如果我理解错了会怎样**：我可能在 `Flush()` 后立即用 Scanner 读，结果读不到，误以为写入失败。

**建议**：说明 `Flush()` 对同一文件的 Scanner 的可见性保证（或显式声明"无保证"）。

---

### 8. DataTail 复用 `SizedPtr.OffsetBytes` 的陷阱

**位置**： §7.2

> `DataTail` 使用 `SizedPtr.OffsetBytes` 表达文件截断点（length 部分无意义，可为 0）。

**问题**：`SizedPtr` 的设计初衷是 `(offset, length)` 对，但 DataTail 只用 offset。这意味着：
- 我能用 `SizedPtr.LengthBytes = 0` 来表达 DataTail 吗？
- 还是应该定义一个专门的 `DataTail` 类型？
- 如果我用 `default(SizedPtr)` 表示"无 DataTail"，但 `[F-RBF-NULLPTR]` 说 `default` 是"无效引用"——这两个语义冲突吗？

**如果我理解错了会怎样**：我可能混淆"无效 Frame 引用"和"空 DataTail"的语义。

**建议**：要么定义专用的 DataTail 类型，要么在 §7.2 明确 `SizedPtr` 作为 DataTail 时的合法值域和 null 表示。

---

## 总结

| # | 问题 | 严重程度 | 建议 |
|---|------|----------|------|
| 1 | SizedPtr 4B 粒度 vs 字节粒度歧义 | **高** | 明确属性返回值单位 |
| 2 | 最小 HeadLen 术语不一致 | 中 | 统一术语 |
| 3 | Reverse Scan 缺少 FrameStatus 校验 | **高** | 补充伪代码步骤 |
| 4 | FrameStatus 解析步骤缺失 | **高** | 补充边界推导逻辑 |
| 5 | LengthBytes == HeadLen 未钉死 | 中 | 显式声明等式 |
| 6 | TryReadAt 校验要求不明 | 中 | 补充失败条件 |
| 7 | Flush 可见性语义模糊 | 低 | 补充可见性保证 |
| 8 | DataTail 复用 SizedPtr 有陷阱 | 中 | 明确合法值域 |

[copilot_cache_control: { type: 'ephemeral' }]
~~~
