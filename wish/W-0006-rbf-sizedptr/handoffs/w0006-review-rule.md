# W-0006 Rule.md 审阅报告

> **说明**：文档/措辞类问题已提取到 [w0006-review-doc-issues.md](w0006-review-doc-issues.md)，本报告仅保留设计/工程类问题。

## 审阅摘要
- 原发现问题数：9 个
- **设计/工程类问题**：6 个（E8-E13，保留于本报告）
- **文档/措辞类问题**：3 个（D9-D11，已提取至 w0006-review-doc-issues.md）

## 问题清单

### E8: `[R-RBF-NULLPTR]` 条款 ID 与上游/Plan 不一致，导致条款变更清单不可执行
- **类型**：条款变更可执行性
- **严重性**：Sev1
- **位置**：§1 "条款 [R-RBF-NULLPTR]"；§3.2 "要新增的条款"
- **问题描述**：Rule.md 将 NullPtr 条款编号写为 `[R-RBF-NULLPTR]`，但：
  - `rbf-interface.md` 当前对应条款为 **`[F-RBF-NULLPTR]`**（术语/定义类条款）。
  - Plan.md 也使用 **`[F-RBF-NULLPTR]`**（Plan §2.4 新 §2.3 内容）。

  这会导致条款变更清单不可执行（执行者会新增/引用一个仓库中不存在的条款 ID），并造成后续跨文档引用悬挂。
- **证据**：
  - `atelia/docs/Rbf/rbf-interface.md` §2.3：`[F-RBF-NULLPTR]`。
  - `wish/W-0006-rbf-sizedptr/artifacts/Plan.md` §2.4：`[F-RBF-NULLPTR]`。
- **工程问题本质**：条款 ID 不一致导致执行者无法定位/更新正确的条款，属于**变更清单可执行性**问题。
- **建议修复**：
  - 将 Rule.md 中所有 `[R-RBF-NULLPTR]` 统一改为 `[F-RBF-NULLPTR]`（并明确它属于"术语/约定"而非"规则算法"类条款）。
  - 同步检查 Rule.md 是否还引用了其他未落地/已改名的条款 ID。

---

### E9: §2 迁移映射表包含不存在的 API：`SizedPtr.FromOffsetAndLength()`
- **类型**：迁移指南准确性
- **严重性**：Sev1
- **位置**：§2 "迁移映射"表格最后一行
- **问题描述**：Rule.md 写作 `new Address64(value)` → `SizedPtr.FromOffsetAndLength()`，但 `Atelia.Data.SizedPtr` 当前提供的创建 API 为：
  - `SizedPtr.Create(ulong offsetBytes, uint lengthBytes)`（做完整校验）
  - `SizedPtr.TryCreate(...)`
  - `SizedPtr.FromPacked(ulong packed)`（不校验）
  并不存在 `FromOffsetAndLength()`。
- **证据**：`atelia/src/Data/SizedPtr.cs`。
- **工程问题本质**：迁移映射表是实施的直接依据，包含不存在的 API 会导致实现者调用失败或自行"创造"API。这是**迁移指南准确性**问题。
- **建议修复**：将映射改为更可执行且不误导的形式：
  - 若旧代码只有"纯位置"：明确迁移策略（例如"改传 `SizedPtr` 时必须补齐 length；如确实只需要 position，则使用 `ptr.OffsetBytes` 或专门引入 `ulong dataTailOffsetBytes`"）。
  - 若旧代码已有位置+长度：使用 `SizedPtr.Create(offsetBytes, lengthBytes)`。

---

### E10: §4.1 关于"对齐违规抛 `ArgumentException`"与 `SizedPtr.Create` 实际行为不一致
- **类型**：契约准确性
- **严重性**：Sev2
- **位置**：§4.1 "SizedPtr 约束（继承自 Atelia.Data）"表格第一行
- **问题描述**：Rule.md 声称 4B 对齐违规"构造时抛 `ArgumentException`"，但 `SizedPtr.Create` 对齐检查抛出的是 `ArgumentOutOfRangeException`。
  作为 Rule-Tier（实施依据），此类异常类型差异容易导致实现/测试与既有库不一致。
- **证据**：`atelia/src/Data/SizedPtr.cs`：对齐失败 `throw new ArgumentOutOfRangeException(...)`。
- **工程问题本质**：异常类型是 API 契约的一部分，Rule 与实现不一致会导致测试用例期望错误的异常类型。这是**契约准确性**问题。
- **建议修复**：
  - 将"违反后果"改为"抛 `ArgumentOutOfRangeException`"；或
  - 若 Rule 希望 RBF 层再包一层并抛 `ArgumentException`，需明确"RBF 层 wrapper 的异常契约"，否则默认应以 `Atelia.Data` 的契约为准。

---

### E11: §4.2 "返回值语义：Append/Commit 失败返回 NullPtr"与 `rbf-interface.md` 的契约风格不对齐
- **类型**：错误处理语义
- **严重性**：Sev2
- **位置**：§4.2 "RBF 层使用规则"表格第一行（返回值语义）
- **问题描述**：Rule.md 将 `Append()`/`Commit()` 描述为"失败时返回 NullPtr"。但 `rbf-interface.md` 的 API 形态是：
  - `Append(...)` / `Commit()`：返回 `SizedPtr`，未宣称"失败返回 NullPtr"的错误通道；
  - `TryReadAt(...)`：通过 `bool` 表示失败。

  在 .NET 语境中，`Append/Commit` 更自然的失败机制是抛异常（I/O 错误、校验失败、非法状态等）。若 Rule 要求"失败返回 NullPtr"，需要在接口契约中明确，否则会造成"实现者不知道该抛异常还是返回 NullPtr"的歧义。
- **证据**：`atelia/docs/Rbf/rbf-interface.md`：`SizedPtr Append(...)`，`public SizedPtr Commit();`。
- **工程问题本质**：错误处理语义是 API 设计的核心决策，Rule 与接口文档不一致会导致实现者做出矛盾的选择。这是**错误处理语义**问题。
- **建议修复**：二选一并写死：
  1) **对齐现接口风格**：删除/改写该行，将 NullPtr 的用途限定为"可选返回/未找到"（例如 future 的 `Try*` API）或"外部存储中的缺失引用"；
  2) **若确需 NullPtr 作为错误通道**：必须同步修订 `rbf-interface.md`，为 `Append/Commit` 增加规范化的失败语义（不建议：会弱化异常的可诊断性，并与现有 `TryReadAt` 的风格混搭）。

---

### E12: §3.3 "要更新的条款"只写"改标题"，但条款 ID 已从 `[F-PTR64-WIRE-FORMAT]` 改为 `[F-SIZEDPTR-WIRE-FORMAT]`
- **类型**：条款变更清单可执行性
- **严重性**：Sev1
- **位置**：§3.3 "要更新的条款"
- **问题描述**：Rule.md 仍以 `[F-PTR64-WIRE-FORMAT]` 作为"要更新"的条款 ID，并只要求"标题改为 SizedPtr Wire Format"。
  但：
  - Plan.md 明确要求条款 ID 替换为 `[F-SIZEDPTR-WIRE-FORMAT]`；
  - `atelia/docs/Rbf/rbf-format.md` 当前也已使用 `[F-SIZEDPTR-WIRE-FORMAT]`。

  若执行者按 Rule.md 操作，会出现"更新一个已不存在的条款 ID"的情况。
- **证据**：
  - `wish/W-0006-rbf-sizedptr/artifacts/Plan.md` §3.1：`[F-PTR64-WIRE-FORMAT]` → `[F-SIZEDPTR-WIRE-FORMAT]`。
  - `atelia/docs/Rbf/rbf-format.md` §7：`[F-SIZEDPTR-WIRE-FORMAT]`。
- **工程问题本质**：条款 ID 与上游文档不一致导致变更清单不可执行。这是**变更清单可执行性**问题。
- **建议修复**：将 Rule §3.3 改写为与 Plan 一致的可执行描述：
  - "将 `[F-PTR64-WIRE-FORMAT]` 改为 `[F-SIZEDPTR-WIRE-FORMAT]`，并重写内容为 offset+length 的 8 字节 LE 编码。"

---

### E13: NullPtr 判等规则缺少关键边界澄清：`LengthBytes == 0` 并不等价于 Null
- **类型**：边界条件定义
- **严重性**：Sev2
- **位置**：§1.3 判等规则；§4.2 DataTail/持久化描述附近（间接相关）
- **问题描述**：Rule.md 已明确 NullPtr 为 `Packed == 0`，但同一 wish 的 Plan/Format 引入了 DataTail 表达（`LengthBytes` 可为 0 且"无意义"）。
  在这种设计下，读者很容易误用"`LengthBytes==0` → Null"来做判定，从而把合法的 DataTail 表达（offset!=0,length=0）当成 Null。
- **工程问题本质**：边界条件定义不完整会导致实现者做出错误的 Null 判定逻辑。这是**边界条件定义**问题。
- **建议修复**：在 §1.3 或 §4.2 增加一句强约束：
  - "RBF NullPtr 的判定条件唯一且仅为 `Packed == 0`（或 `ptr == default`）；`LengthBytes == 0` 不能作为 Null 判定条件。"

---

## 优点记录

- **Null 语义边界表达正确**：Rule 明确指出 SizedPtr 作为几何类型不自带 Null 语义，而 NullPtr 是 RBF 层业务约定（与 Resolve §6 D3、`SizedPtr.cs` 的 remarks 一致）。
- **判等建议贴近 .NET 惯用法**：推荐 `ptr == default`，并给出等价的 `Packed == 0` 检查，方向正确。
- **迁移主张与上游结论一致**：整体叙事是"SizedPtr 完全替代 Address64"，与 Resolve §6 D4、Shape §1-2、调查报告结论一致。

---

## 总体建议

1. **先修条款 ID 与变更清单的可执行性（E8, E12）**：统一 `[F-RBF-NULLPTR]` / `[F-SIZEDPTR-WIRE-FORMAT]`，并让 Rule 的清单与 Plan 和现行 `rbf-interface.md`/`rbf-format.md` 一致。
2. **把迁移映射表升级为"不会误导实现"的版本（E9）**：删除不存在的 API，明确应使用 `SizedPtr.Create/TryCreate/FromPacked`，并说明"只需位置"的场景如何表达（例如 DataTail 使用 `OffsetBytes`）。
3. **收敛错误处理语义（E11）**：明确 `Append/Commit` 的失败语义是"抛异常"还是"返回 NullPtr"，避免在实现阶段出现两套互斥约定。
4. **补齐契约准确性与边界定义（E10, E13）**：异常类型与实现对齐；明确 `LengthBytes == 0` 不等价于 Null。

---

## 问题分类总览

| 原编号 | 新编号 | 分类 | 去向 |
|:-------|:-------|:-----|:-----|
| P1 | **E8** | 设计/工程 | 本报告 |
| P2 | D9 | 文档/措辞 | w0006-review-doc-issues.md |
| P3 | **E9** | 设计/工程 | 本报告 |
| P4 | **E10** | 设计/工程 | 本报告 |
| P5 | **E11** | 设计/工程 | 本报告 |
| P6 | **E12** | 设计/工程 | 本报告 |
| P7 | D10 | 文档/措辞 | w0006-review-doc-issues.md |
| P8 | **E13** | 设计/工程 | 本报告 |
| P9 | D11 | 文档/措辞 | w0006-review-doc-issues.md |
