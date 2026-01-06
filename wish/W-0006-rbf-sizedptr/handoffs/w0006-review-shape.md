# W-0006 Shape.md 审阅报告

> **说明**：文档/措辞类问题已提取到 [w0006-review-doc-issues.md](w0006-review-doc-issues.md)，本报告仅保留设计/工程类问题。

## 审阅摘要
- 总体评价：基本可用，但需补齐关键精确定义以降低后续 Plan/实现歧义风险
- 原发现问题数：6 个
- **设计/工程类问题**：3 个（E5-E7，保留于本报告）
- **文档/措辞类问题**：3 个（D6-D8，已提取至 w0006-review-doc-issues.md）

## 问题清单

### E5: Glossary 中对 `LengthBytes` 的边界定义不精确（Fence/FrameBytes 歧义风险）
- **类型**：关键信息遗漏
- **严重性**：Sev2
- **位置**：§2 Glossary Alignment 表格；§4.2 "依赖 LengthBytes …校验读取完整性"
- **问题描述**：Shape 中对 `SizedPtr.LengthBytes` 的定义写作"Frame 的字节长度"，但未明确"是否包含 Fence""从哪里算起到哪里为止"。
  - 当前 `atelia/docs/Rbf/rbf-interface.md` §2.3 已明确：`OffsetBytes` 指向 **Frame 起点（HeadLen 字段位置）**，`LengthBytes` 为 **Frame 的字节长度（含 HeadLen 到 CRC32C）**，且 FrameBytes 不包含 Fence。
  - Shape 作为 Plan 的基础 glossary，如果不把边界写死，后续很容易出现"把 Fence 算进 length"或"把 offset 当成 payload 起点"的实现偏差。
- **证据**：`rbf-interface.md` §2.3 的表格与注释（OffsetBytes 指向 HeadLen；LengthBytes 含 HeadLen..CRC32C）。
- **工程问题本质**：`LengthBytes` 是实现 RBF 读路径的核心参数，边界定义不精确会直接导致实现偏差。这是**设计定义精确性**问题。
- **建议修复**：
  - 在 Shape §2 的 `SizedPtr.OffsetBytes`/`LengthBytes` 两行里补一句"Offset 指向 HeadLen 字段；Length 为 FrameBytes 长度（不含前后 Fence，覆盖 HeadLen..CRC32C）"。
  - 在 §3.2/§4.2 补一句"读路径按 FrameBytes 一次性读取并做 framing/CRC 校验"。

---

### E6: §4.3 "~1TB"表述像产品承诺，混淆"可表示范围"与"系统承诺"
- **类型**：设计边界定义错误
- **严重性**：Sev2
- **位置**：§4.3 边界约束表（"最大文件偏移"行的 RBF 承诺 / StateJournal 假设）
- **问题描述**：此处写作"最大文件偏移 ~1TB…单文件不超过 1TB"，容易被读者理解为"RBF/StateJournal 系统保证文件永远不会超过 1TB"。
  但从 `Atelia.Data.SizedPtr` 的性质看，这是"可表示范围"限制，不必然是"系统必须小于 1TB"的业务承诺。
- **工程问题本质**：Shape-Tier 的边界约束如果被误读为"产品承诺"，会影响上层系统（StateJournal）的容量规划和分卷策略设计。这是**语义边界定义**问题。
- **建议修复**：把表述改成"可表示/可寻址范围"，并明确超出范围时的责任归属：
  - "RBF Interface 通过 SizedPtr 仅能寻址到 ~1TB 的 OffsetBytes；超出范围为 out-of-scope（未来可通过分卷/多段文件或其他方案处理）"。
  - StateJournal 侧则写作"当前实现假设单段文件不会超过 ~1TB"。

---

### E7: §3.3 持久化序列化缺少端序约定（跨平台实现风险）
- **类型**：关键信息遗漏
- **严重性**：Sev3
- **位置**：§3.3 持久化存储
- **问题描述**：Shape 提到 `SizedPtr.Packed` 可序列化，但没有明确"8 字节 LE / 与 wire format 的关系"。Rule/格式层已有"8 字节 LE 编码"的导向（例如 Rule 3.3、rbf-format 的 wire format 语气）。
- **工程问题本质**：端序约定是跨语言/跨进程读取的基础，缺失会导致实现不一致。这是**协议完整性**问题。
- **建议修复**：在 §3.3 补一句："序列化形式为 `Packed` 的 8 字节 little-endian（与 RBF wire format 对齐）"。

---

## 优点记录

- **与 Resolve §6-7 的最终决策一致**：Shape 从开头就采用"SizedPtr 完全替代 Address64"的叙事，与 Resolve D4、Rule §2 的"完全移除 Address64"一致。
- **监护人"写路径/读路径/定位与持久化"要点覆盖完整**：§3 明确记录了三用途，且与 `rbf-interface.md` 当前签名（Append/Commit/TryReadAt/Frame.Ptr）相匹配。
- **与 Rule.md 的 NullPtr 约定一致**：Shape 使用 `RbfInterface.NullPtr = default(SizedPtr)` 与 `ptr == default` 的判等方式，与 Rule 的条款 `[R-RBF-NULLPTR]` 方向一致。

---

## 总体建议

1. **精确化边界定义**（E5）：
   - 把 Glossary 表格升级为"可直接落到 Plan/实现"的精确定义
   - 尤其是 `OffsetBytes`/`LengthBytes` 的边界（HeadLen..CRC32C、是否含 Fence）

2. **区分"可表示范围"与"系统承诺"**（E6）：
   - 将 §4.3 的"~1TB/256MB"从"系统承诺"改写为"可表示范围/寻址范围"
   - 注明超范围的处理归属（out-of-scope 或未来分卷/分片）

3. **补齐协议完整性**（E7）：
   - 在 §3.3 明确序列化端序约定（8 字节 LE）

---

## 问题分类总览

| 原编号 | 新编号 | 分类 | 去向 |
|:-------|:-------|:-----|:-----|
| P1 | **E5** | 设计/工程 | 本报告 |
| P2 | D6 | 文档/措辞 | w0006-review-doc-issues.md |
| P3 | D7 | 文档/措辞 | w0006-review-doc-issues.md |
| P4 | **E6** | 设计/工程 | 本报告 |
| P5 | **E7** | 设计/工程 | 本报告 |
| P6 | D8 | 文档/措辞 | w0006-review-doc-issues.md |
