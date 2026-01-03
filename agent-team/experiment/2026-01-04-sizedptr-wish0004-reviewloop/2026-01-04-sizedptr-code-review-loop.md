---
docId: "M-2026-01-04-SizedPtr-CodeReview-Loop"
title: "实验：SizedPtr 的 Review 驱动质量闭环（防范围外建议）"
created: "2026-01-04"
status: "Draft"
links:
  wish: "wishes/active/wish-0004-SizedPtr.md"  # 已标记 Completed；本文件是实验性质量闭环
  design: "atelia/docs/Data/Draft/SizedPtr.md"
  code: "atelia/src/Data/SizedPtr.cs"
  tests: "atelia/tests/Data.Tests/SizedPtrTests.cs"
  leader_model: "agent-team/beacon/team-leader-mental-model-v0.1.md"
---

# 实验：SizedPtr 的 Review 驱动质量闭环（防范围外建议）

> 目的：在“测试已绿”的前提下，建立一个 Review→Fix→Re-Review 的质量闭环；并采样 LLM 审阅者常见的范围漂移模式，形成可执行的“范围闸门”。

## 0. Review Loop 协议（本实验使用）

### 0.1 输入（Scope）
- **允许审阅范围（In-Scope）**：
  - 设计文档一致性：`atelia/docs/Data/Draft/SizedPtr.md` 与实现/测试是否对齐
  - 正确性：打包/解包、对齐校验、边界值、溢出/checked、Contains 半开区间
  - API 误用成本（仅限于不引入破坏性变更、不扩展功能集的前提下）
  - 代码质量：清晰性、可维护性、小的防御性改进（不改变语义）

- **禁止审阅范围（Out-of-Scope）**：
  - 多线程/并发安全改造
  - 新增大量便捷方法/工具函数
  - 扩展到 RBF 接入/迁移（属于新 Wish）
  - 改变 bit 分配、引入多类型/标记位（违背已定决策）

### 0.2 输出（Deliverable）
- FixList（分级）：
  - **Sev0**：规范/设计违背、错误实现、潜在数据损坏
  - **Sev1**：高概率误用/边界缺失/错误异常语义
  - **Sev2**：可读性/一致性/小的防御性改进
- Parking Lot：收集 Out-of-Scope 建议（仅记录，不执行）

### 0.3 Stop Condition（何时结束 Review Loop）
- 连续一次 Re-Review 未发现 **Sev0/Sev1**，即可结束本次闭环。

---

## 1. Review Iteration 1

### Snapshot
- Stage: Craft（已实现+测试通过）
- Mode: 收敛
- Pressure: 指定（交付物：FixList + Patch + Re-Review 通过）

### FixList
#### Sev0
- （未发现）

#### Sev1
- `atelia/tests/Data.Tests/SizedPtrTests.cs`
  - 问题：测试 `Create_OffsetPlusLengthOverflows_Throws` 名不副实：并未触发异常，只做了“不会溢出”的断言，误导维护者。
  - 修复建议：重命名为体现真实意图，并补充注释说明“在当前 38:26 值域下该溢出不可达”。

- 设计一致性（文档 vs 代码）
  - 问题：文档里的参考实现包含 components ctor/共享校验拆分；当前实现以 `Create/TryCreate` 为主且重复校验逻辑。行为一致，但结构漂移会增加后续误读成本。
  - 修复建议：本轮先不扩展 API（保持范围受控），优先消除重复校验并提高可读性（例如抽取共享校验函数）。

#### Sev2
- `atelia/src/Data/SizedPtr.cs`
  - 问题：`EndOffsetExclusive`、`Contains` 依赖隐式数值提升（uint→ulong），在高敏感位操作代码里不够显式。
  - 修复建议：显式 cast（不改变语义，仅提升可读性/降低误判）。

- `atelia/src/Data/SizedPtr.cs`
  - 问题：`Create` 与 `TryCreate` 校验逻辑重复，未来易漂移。
  - 修复建议：抽取共享逻辑（本轮作为可维护性改进）。

### Parking Lot（范围外建议）
1) 多线程/并发安全改造（Out-of-Scope）
2) 新增更多便捷方法（TryGetEndOffsetExclusive/格式化输出等）（Out-of-Scope）
3) 引入静态断言/调试断言作为工程护栏（可考虑后续纳入，但本轮不做）（Out-of-Scope）

### Patch
已应用（范围内）修复：
- `atelia/src/Data/SizedPtr.cs`
  - `EndOffsetExclusive` / overflow guard / `Contains` 的隐式数值提升改为显式 cast（降低误读）
- `atelia/tests/Data.Tests/SizedPtrTests.cs`
  - 重命名并澄清“溢出不可达”测试：`OffsetPlusLength_CannotOverflow_UnderCurrentBitAllocation`

验证：`dotnet test tests/Data.Tests/Data.Tests.csproj -c Release`（total 86, failed 0）。

---

## 2. Review Iteration 2（Re-Review）

### FixList
#### Sev0
- 无

#### Sev1
- 文档与代码存在“结构性漂移”的误读风险：
  - `atelia/docs/Data/Draft/SizedPtr.md` 的示例代码段展示了当前实现不存在的 public API/结构（如 components ctor、`IsValidComponents/CreatePacked*` 结构）。
  - 风险：读者误以为这些 API 存在，或把示例当成 SSOT，造成误用与维护成本。
  - 修复建议：把文档示例收敛为“真实存在的 API 形态”，示例可保留，但必须与代码一致。

#### Sev2
- `atelia/src/Data/SizedPtr.cs`
  - `Create`/`TryCreate` 仍维护两份同构校验条件（对齐/边界/overflow guard），存在长期漂移风险。
  - `lengthBytes & AlignmentMask` 依赖隐式 `uint→ulong` 提升，可读性不足。
  - `EndOffsetExclusive_UsesCheckedArithmetic` 测试名承诺了不可验证的语义（在当前 38:26 值域下溢出不可达），建议改名为“返回 offset+length”。

### Parking Lot
沿用上一轮 Parking Lot，新增：
1) 若未来 bit 分配可配置，需要系统性处理 shift-count 的 64 位边界行为，避免 `<< 64` 的 silent bug（Out-of-Scope）。
2) 若用于长期演进的序列化协议，需要明确 versioning/compat 文档叙述（Out-of-Scope）。

### 结论
本轮未关闭：存在 Sev1（文档示例与代码漂移）与若干 Sev2。
下一步计划：
1) 修订 `atelia/docs/Data/Draft/SizedPtr.md` 示例代码，使其对齐真实 API。
2) 在 `SizedPtr.cs` 内部抽取共享校验逻辑，减少 Create/TryCreate 漂移风险（不扩展 public API）。
3) 调整测试命名，避免承诺不可验证语义。

#### 已执行的修复（本轮）
- ✅ 修订文档：移除/收敛 Plan-Tier 中的大段伪实现代码，并显式声明 `atelia/src/Data/SizedPtr.cs` 为 SSOT（降低文档↔代码漂移风险）。
- ✅ 调整测试命名：`EndOffsetExclusive_UsesCheckedArithmetic` → `EndOffsetExclusive_ReturnsOffsetPlusLength`（避免承诺不可验证语义）。
- ✅ 回归测试：`dotnet test tests/Data.Tests/Data.Tests.csproj -c Release`（total 86, failed 0）。

---

## 3. Review Iteration 3（Final Re-Review / Closure）

### FixList
- Sev0：未发现
- Sev1：未发现（文档已显式声明实现/测试为 SSOT，且不再内嵌伪实现代码，显著降低范围漂移诱因）
- Sev2：
  - `atelia/src/Data/SizedPtr.cs`：Create/TryCreate 仍存在同构校验重复，长期可能漂移；当前未见语义不一致（可后续微调）。

### Parking Lot（范围外建议）
1) 若未来 bit 分配可配置，需要系统性处理 shift-count 的 64 位边界行为并补 compat 叙述（Out-of-Scope）。

### Closure
- 满足 stop_condition：已出现“连续一次 review 未发现 Sev0/Sev1”。本次 review loop 关闭。
