# W-0006 Resolve-Tier 事实清单（Investigation Brief）

> **Investigator**: 2026-01-05
> **任务**: 为 W-0006 (RBF + SizedPtr) 的 Resolve-Tier 产出事实清单，支持动机分析和设计决策

---

## 1. 现状事实清单

### 1.1 RBF 文档成熟度

| 文档 | 版本 | 状态 | 成熟度评估 |
|:-----|:-----|:-----|:-----------|
| [rbf-interface.md](../atelia/docs/Rbf/rbf-interface.md) | 0.17 | **Reviewed** | ✅ 高 — 经过复核会议，条款完整，可作为接口契约 |
| [rbf-format.md](../atelia/docs/Rbf/rbf-format.md) | 0.16 | Draft | ◐ 中 — 有 SSOT 定义，但标记为 Draft |
| [rbf-test-vectors.md](../atelia/docs/Rbf/rbf-test-vectors.md) | 0.11 | Draft | ◐ 中 — 覆盖范围完整，与规范对齐 |

### 1.2 RBF 核心类型定义

#### <deleted-place-holder>（当前定义）

**位置**: [rbf-interface.md#L111-L122](atelia/docs/Rbf/rbf-interface.md#L111-L122)

```csharp
public readonly record struct <deleted-place-holder>(ulong Value) {
    public static readonly <deleted-place-holder> Null = new(0);
    public bool IsNull => Value == 0;
}
```

**条款**:
- `[F-ADDRESS64-DEFINITION]` — 8 字节 LE 文件偏移量，指向 Frame 的 HeadLen 字段起点
- `[F-ADDRESS64-ALIGNMENT]` — 有效 <deleted-place-holder> MUST 4B 对齐（`Value % 4 == 0`）
- `[F-ADDRESS64-NULL]` — `Value == 0` 表示 null

**语义**：纯指针（point-to 语义），**不含长度信息**。

#### FrameTag

**位置**: [rbf-interface.md#L34-L51](atelia/docs/Rbf/rbf-interface.md#L34-L51)

- 4 字节（`uint`）帧类型标识符
- 三层视角：存储层（4B LE）→ 接口层（`uint`）→ 应用层（enum 等）
- RBF 层不保留任何 FrameTag 值，全部值域由上层定义

#### Tombstone

**位置**: [rbf-interface.md#L57-L78](atelia/docs/Rbf/rbf-interface.md#L57-L78)

- 帧有效性标记（Layer 0 元信息）
- 通过 `RbfFrame.IsTombstone` bool 属性暴露
- 条款 `[S-RBF-TOMBSTONE-VISIBLE]`、`[S-STATEJOURNAL-TOMBSTONE-SKIP]`

### 1.3 RBF 文档中的"区间/范围"语义表达

| 概念 | 位置 | 当前表达方式 | 评估 |
|:-----|:-----|:-------------|:-----|
| **Frame 位置** | <deleted-place-holder> | 纯指针，无长度 | ❌ 无区间语义 |
| **Payload** | RbfFrame.Payload | `ReadOnlySpan<byte>`（运行时长度） | ⚠️ 隐式长度 |
| **DataTail** | rbf-format.md#L243 | Ptr64（文件末尾偏移） | ❌ 纯位置，无长度 |
| **ScanReverse 范围** | IRbfScanner | 隐式（文件起点到当前位置） | ❌ 无显式区间表达 |

**关键发现**：RBF 文档中**没有显式的区间类型**。所有"范围"语义都是隐式的（通过指针+运行时长度）。

### 1.4 SizedPtr 能力边界

**位置**: [atelia/src/Data/SizedPtr.cs](atelia/src/Data/SizedPtr.cs)

#### Bit 分配方案（38:26）

| 字段 | Bit 数 | 最大值 | 换算 |
|:-----|:-------|:-------|:-----|
| **OffsetBits** | 38 | `((1UL << 38) - 1UL) << 2` | **~1 TB** |
| **LengthBits** | 26 | `((1UL << 26) - 1UL) << 2` | **~256 MB** |

#### 对齐要求
- Offset **MUST** 4B 对齐
- Length **MUST** 4B 对齐
- 通过位移 2 bit 实现压缩存储

#### 特殊值语义

**位置**: [atelia/docs/Data/Draft/SizedPtr.md#L30-L35](atelia/docs/Data/Draft/SizedPtr.md#L30-L35)

> `SizedPtr` **不定义**任何特殊值语义（例如 Null/Empty）。
> - `Packed == 0` 在数学上仅意味着 `(OffsetBytes=0, LengthBytes=0)`。

**关键约束**：SizedPtr 是纯粹的几何类型，Null 语义由上层定义。

#### 代码实现验证

**已实现**（[SizedPtr.cs](atelia/src/Data/SizedPtr.cs)）:
- `FromPacked(ulong)` — 无校验解包
- `Create(ulong, uint)` / `TryCreate(...)` — 完整校验（对齐、范围、溢出）
- `Contains(ulong)` — 半开区间判断，使用差值比较避免溢出
- `EndOffsetExclusive` — checked 算术
- `Deconstruct` — 解构为 `(offset, length)`

**未实现**（SizedPtr.md 设计文档未要求）:
- 无 `IsNull` / `IsEmpty` 属性
- 无与 <deleted-place-holder> 的转换方法

### 1.5 代码实现状态

| 组件 | 状态 | 位置 |
|:-----|:-----|:-----|
| **SizedPtr** | ✅ 已实现 | `atelia/src/Data/SizedPtr.cs` |
| **<deleted-place-holder>** | ❌ 无实现 | 仅文档定义 |
| **RBF 层** | ⚠️ 已归档 | `atelia/archive/2025-12-29-rbf-statejournal-v1/Rbf/` |
| **StateJournal** | ⚠️ 已归档 | `atelia/archive/2025-12-29-rbf-statejournal-v1/StateJournal/` |

**关键发现**：`atelia/src/` 下无 RBF/<deleted-place-holder> 实现，历史代码已归档。SizedPtr 需要从零集成。

---

## 2. 已识别的问题

### P1: <deleted-place-holder> 与 SizedPtr 的语义冲突

**Symptom**: 
- <deleted-place-holder> 是**纯指针**（point-to），`Value=0` 表示 Null
- SizedPtr 是**区间**（range），`Packed=0` 数学上表示 `(0, 0)` 不是 Null

**Tier**: Rule-Tier

**证据来源**:
- [rbf-interface.md#L120](atelia/docs/Rbf/rbf-interface.md#L120) — `[F-ADDRESS64-NULL]`
- [SizedPtr.md#L32-L33](atelia/docs/Data/Draft/SizedPtr.md#L32-L33) — 不定义 Null

**初步观察**:
- 两者不能直接替换——语义模型不同
- 需要明确哪些场景用 <deleted-place-holder>（纯位置），哪些用 SizedPtr（区间）

**next_probe**: 列举 RBF 文档中所有 <deleted-place-holder> 使用点，分类为"仅需位置"vs"需要长度"

---

### P2: RBF 文档中缺乏区间类型

**Symptom**: 
- Frame 的"范围"通过 <deleted-place-holder> + 隐式 HeadLen 表达
- Payload 的"范围"只在运行时 Span 中体现，无持久化表达

**Tier**: Shape-Tier

**证据来源**:
- [rbf-interface.md#L125-L135](atelia/docs/Rbf/rbf-interface.md#L125-L135) — Frame 定义只有 <deleted-place-holder>
- [rbf-format.md#L64](atelia/docs/Rbf/rbf-format.md#L64) — `[F-FRAME-LAYOUT]` 布局表无区间字段

**初步观察**:
- 引入 SizedPtr 可以让"Frame 区间"在接口层显式表达
- 可能涉及 `RbfFrame` 结构扩展（增加 `SizedPtr Range` 属性？）

**next_probe**: 评估 StateJournal 是否需要"Frame 区间"作为持久化字段

---

### P3: 256MB Length 上限的适用性

**Symptom**: 
- SizedPtr.MaxLength = ~256MB
- StateJournal mvp-design-v2.md 未提及 Frame 大小上限

**Tier**: Rule-Tier

**证据来源**:
- [SizedPtr.cs#L19](atelia/src/Data/SizedPtr.cs#L19) — `MaxLength` 常量
- [mvp-design-v2.md](atelia/docs/StateJournal/mvp-design-v2.md) — 无 Frame 大小约束

**初步观察**:
- 256MB 对单个 Frame 绰绰有余（正常对象版本不会达到 MB 级）
- 但若未来有"大 Blob 存储"需求，可能需要分块策略

**next_probe**: 检查 StateJournal 是否有 "大对象" 存储的设计讨论

---

### P4: 术语一致性风险

**Symptom**:
- RBF 使用 `Offset`（如 <deleted-place-holder> 的文件偏移）
- SizedPtr 使用 `OffsetBytes`、`LengthBytes`
- 可能产生混淆："这里的 Offset 是哪个？"

**Tier**: Shape-Tier

**证据来源**:
- [wish/W-0006 meeting 讨论](wish/W-0006-rbf-sizedptr/meeting/2026-01-05-scope-and-approach.md#L286)
- 畅谈会已识别此风险并建议术语对齐表

**初步观察**:
- 需要 Glossary Alignment 表明确区分
- SizedPtr 的 `OffsetBytes` 可能需要上下文前缀（如 `RangeOffset`）

**next_probe**: 检查现有文档中 `Offset` 一词的使用频率和上下文

---

## 3. 待澄清的疑问（Known-Unknown）

### Q1: SizedPtr 是否应该成为 RBF 接口层类型？

**描述**: SizedPtr 目前定义为 Atelia.Data 的通用类型，RBF 接口使用 <deleted-place-holder>。是否需要在 RBF 层引入 SizedPtr？

**证据来源**: 
- [rbf-interface.md 依赖关系](atelia/docs/Rbf/rbf-interface.md#L6-L8)
- [SizedPtr.md 首个目标用户](atelia/docs/Data/Draft/SizedPtr.md#L7)

**next_probe**: 评估 RBF 接口的稳定性承诺——引入新类型是否破坏现有契约

---

### Q2: <deleted-place-holder> 是否应该被 SizedPtr 完全替换？

**描述**: 文档提到"用 SizedPtr 替代部分 <deleted-place-holder> 使用"，但边界不清晰。

**证据来源**:
- [W-0006 畅谈会决议](wish/W-0006-rbf-sizedptr/meeting/2026-01-05-scope-and-approach.md#L375)

**next_probe**: 列举 <deleted-place-holder> 使用场景，判断哪些可替换、哪些需保留

---

### Q3: StateJournal 对 SizedPtr 的依赖范围

**描述**: StateJournal 使用 RBF 作为 Layer 0。如果 RBF 引入 SizedPtr，StateJournal 哪些地方需要适配？

**证据来源**:
- [mvp-design-v2.md 文档层次](atelia/docs/StateJournal/mvp-design-v2.md#L6-L15)

**next_probe**: 检查 StateJournal 中 `ObjectVersionPtr`、`VersionIndexPtr`、`DataTail` 的使用方式

---

### Q4: 新旧文档的演进策略

**描述**: 现有 RBF 三份文档（interface/format/test-vectors）如何演进？是原位修订还是创建新版本？

**证据来源**:
- [W-0006 畅谈会 artifacts/ vs docs/ 讨论](wish/W-0006-rbf-sizedptr/meeting/2026-01-05-scope-and-approach.md#L100-L130)

**next_probe**: 确认监护人对文档演进策略的偏好

---

## 4. 对 Resolve-Tier 的建议

### 4.1 值得做的理由（Why）

1. **显式区间语义**：SizedPtr 可以让 RBF 的"范围"概念在类型系统中显式表达，减少隐式长度计算的错误风险
2. **复用已验证代码**：SizedPtr 已在 W-0004 完成实现和测试，引入 RBF 是自然的扩展
3. **为 StateJournal 铺路**：StateJournal M2 可能需要"对象区间"的持久化表达

### 4.2 风险点

1. **语义冲突**：<deleted-place-holder> (Null=0) vs SizedPtr (无 Null) 需要明确分层策略
2. **文档演进成本**：三份 RBF 文档需要协调更新
3. **向后兼容**：若 RBF 实现已存在使用者，接口变更可能破坏

### 4.3 建议的 next_probe

| 优先级 | 动作 | 预期产出 |
|:-------|:-----|:---------|
| P0 | 确认 <deleted-place-holder> 使用点分类 | 替换边界清单 |
| P0 | 确认文档演进策略（监护人） | 决策记录 |
| P1 | 评估 StateJournal 依赖 | 适配范围估算 |
| P2 | 检查术语冲突点 | Glossary Alignment 草案 |

---

## 5. 调查方法论说明

本 Brief 基于以下文件的完整阅读：
- `atelia/docs/Rbf/rbf-interface.md` (v0.17)
- `atelia/docs/Rbf/rbf-format.md` (v0.16)
- `atelia/docs/Rbf/rbf-test-vectors.md` (v0.11)
- `atelia/docs/Data/Draft/SizedPtr.md`
- `atelia/src/Data/SizedPtr.cs`
- `atelia/docs/StateJournal/mvp-design-v2.md` (v3.9, 浏览)

代码搜索确认：`atelia/src/` 下无 <deleted-place-holder>/RBF 实现。
