---
docId: "W-0006-Resolve"
title: "W-0006 Resolve-Tier"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
issues:
  - id: "I-SEMANTIC-CONFLICT"
    description: "Address64 的 Null 语义与 SizedPtr 的几何语义冲突"
    status: "resolved"
  - id: "I-NO-EXPLICIT-RANGE"
    description: "RBF 缺乏显式的区间类型"
    status: "resolved"
  - id: "I-256MB-LIMIT"
    description: "SizedPtr 的 256MB Length 上限对大 Blob 场景的适用性"
    status: "resolved"
  - id: "I-TERM-DRIFT-RISK"
    description: "Address64.Value 与 SizedPtr.OffsetBytes 术语混淆风险"
    status: "resolved"
---

# W-0006 Resolve-Tier

> **一句话判定**：RBF 缺乏显式区间类型，SizedPtr 已实现可复用，值得引入以统一"区间"语义表达。

---

## 1. 动机（Why）

### 当前痛点

RBF 文档没有显式的区间类型。所有"范围"语义通过隐式表达：
- Frame 位置 → `Address64`（纯指针，无长度）
- Payload 范围 → 运行时 `ReadOnlySpan<byte>`（无持久化表达）
- ScanReverse 范围 → 隐式（文件起点到当前位置）

**后果**：设计者和实现者需要在脑中持续进行"位置+长度→范围"的心智计算，增加认知负担和出错风险。

### 机会

SizedPtr 已在 W-0004 中实现并通过测试：
- 38:26 位分配方案（~1TB offset, ~256MB length）
- 严格的 4B 对齐要求
- 完整的校验和溢出保护

RBF 是 SizedPtr 的首个目标用户，引入是自然的扩展。

### 目标

统一"区间"的表达方式：
- **Address64**：指向 Frame 起点的纯位置（point-to 语义）
- **SizedPtr**：表达 offset+length 的紧凑区间（range 语义）

减少重复设计，提升跨层复用。

---

## 2. 现状问题（What's Wrong）

### P1: Address64 与 SizedPtr 的语义冲突

| 维度 | 症状 |
|:-----|:-----|
| **症状** | Address64 是纯指针（`Value=0` 表示 Null），SizedPtr 是区间（`Packed=0` 数学上表示 `(0, 0)` 不是 Null） |
| **证据** | [rbf-interface.md#L120](../../atelia/docs/Rbf/rbf-interface.md#L120) 定义 `[F-ADDRESS64-NULL]`；[SizedPtr.md#L32-L33](../../atelia/docs/Data/Draft/SizedPtr.md#L32-L33) 明确不定义 Null |
| **后果** | 两者不能直接替换——需要明确语义边界，否则 Null 判断逻辑会产生歧义 |

**tier**: Rule-Tier  
**next_probe**: 列举 RBF 文档中所有 Address64 使用点，分类为"仅需位置"vs"需要长度"

---

### P2: RBF 文档中缺乏区间类型

| 维度 | 症状 |
|:-----|:-----|
| **症状** | Frame 的"范围"通过 Address64 + 隐式 HeadLen 表达；Payload 的"范围"只在运行时 Span 中体现，无持久化表达 |
| **证据** | [rbf-interface.md#L125-L135](../../atelia/docs/Rbf/rbf-interface.md#L125-L135) Frame 定义只有 Address64；[rbf-format.md#L64](../../atelia/docs/Rbf/rbf-format.md#L64) `[F-FRAME-LAYOUT]` 布局表无区间字段 |
| **后果** | 无法在接口层显式表达"Frame 区间"，上层（如 StateJournal）需自行计算 |

**tier**: Shape-Tier  
**next_probe**: 评估 RbfFrame 是否需要 `SizedPtr Range` 属性

---

### P3: 256MB Length 上限的适用性

| 维度 | 症状 |
|:-----|:-----|
| **症状** | SizedPtr 的 26-bit Length 最大约 256MB；StateJournal mvp-design-v2.md 未提及 Frame 大小上限 |
| **证据** | [SizedPtr.cs#L19](../../atelia/src/Data/SizedPtr.cs#L19) 定义 `MaxLength`；[mvp-design-v2.md](../../atelia/docs/StateJournal/mvp-design-v2.md) 无 Frame 大小约束 |
| **后果** | 对正常对象版本（KB~MB 级）绰绰有余，但若未来有大 Blob 存储需求，可能需要分块策略 |

**tier**: Rule-Tier  
**next_probe**: 检查 StateJournal 是否有"大对象"存储的设计讨论

---

### P4: 术语一致性风险

| 维度 | 症状 |
|:-----|:-----|
| **症状** | RBF 使用 `Offset`（Address64 的文件偏移）；SizedPtr 使用 `OffsetBytes`/`LengthBytes`；术语混用导致"这里的 Offset 是哪个？" |
| **证据** | [畅谈会讨论](meeting/2026-01-05-scope-and-approach.md#L286) 已识别此风险 |
| **后果** | 跨文档阅读时需持续"心智翻译"，增加理解成本和误用风险 |

**tier**: Shape-Tier  
**next_probe**: 检查现有文档中 `Offset` 一词的使用频率和上下文

---

## 3. 边界与约束（Scope）

### In-Scope

| 范围 | 说明 |
|:-----|:-----|
| **RBF 设计文档修订** | 修订 `rbf-interface.md`、`rbf-format.md`，引入 SizedPtr |
| **语义边界定义** | 明确 Address64 vs SizedPtr 的使用场景和判断依据 |
| **StateJournal 约束视角** | 作为"目标用户"参与设计约束，**不修订 StateJournal 文档本身** |

### Out-of-Scope

| 范围 | 说明 |
|:-----|:-----|
| **RBF 代码实现** | Phase 2 可选，本 Wish 主体是设计文档修订 |
| **StateJournal 文档修订** | 另一个 Wish（W-00XX）负责 |
| **改变 SizedPtr 的设计** | 已在 W-0004 完成，本 Wish 是其消费者 |

---

## 4. 后续探索方向（Next Tiers）

| Tier | 核心问题 | 预期产出 |
|:-----|:---------|:---------|
| **Shape** | 术语如何对齐？RBF 对 StateJournal 的接口承诺是什么？ | Glossary Alignment + Interface Contract with StateJournal |
| **Rule** | Address64 vs SizedPtr 的判断依据是什么？ | 语义边界规则（使用场景清单 + 判断条件） |
| **Plan** | 修订哪些文档？顺序如何？如何记录语义变化？ | 修订计划 + Migration Notes |

---

## 5. 决策依据

本 Resolve 基于以下输入：
- **Investigation Brief**: [w0006-resolve-brief.md](../../agent-team/handoffs/w0006-resolve-brief.md)
- **畅谈会决议**: [2026-01-05-scope-and-approach.md](meeting/2026-01-05-scope-and-approach.md)
- **Wish 定义**: [wish.md](wish.md)

---

## 6. 决策澄清（Decision Clarification）

> **更新**：2026-01-05，监护人提供了关键设计决策，直接解答了 Resolve-Tier 识别的 4 个问题。

### D1: 256MB Length 上限适用性 (I-256MB-LIMIT)

**决策**：✅ 足够用，之前已分析过，最终选定。

**理由**：RBF Frame 的典型大小远小于 256MB，该上限对实际场景无影响。

---

### D2: SizedPtr 在 RBF 中的核心用途 (I-NO-EXPLICIT-RANGE)

**决策**：✅ SizedPtr 就是为了填补"显式区间类型"的缺口而设计的。

**核心用途**（监护人原话）：

> **写数据路径**：一次性告诉外界地址+长度。RBF append 数据返回的 Address64 只包含偏移，导致后续随机读取时需要：
> 1. 先读开头，拿到长度
> 2. 再次读取全长
> 3. 验证
> 
> 如果用 RandomAccess 类进行 IO，需要至少 2 次独立的 IO。
> 
> **读路径**：一次性划分缓存和从磁盘读取。用胖指针，一次性就能完整读取一个 Frame 然后进行校验。
> 
> **定位**：Interface 层的御用对外 Frame 句柄（可持久化），类似文件偏移空间的 span。

**结论**：SizedPtr 不是"可选增强"，而是 RBF Interface 层的**核心类型**。

---

### D3: Null 语义处理 (I-SEMANTIC-CONFLICT)

**决策**：✅ 把 Address64 的 Null 相关成员函数改为 RBF 层的静态函数/常量。

**实施方案**（监护人建议）：
- 在 RBF 层定义：`public static SizedPtr NullPtr => default;` 或类似常量
- 表示"我们 RBF 层是如何定义 SizedPtr 中的特殊值的"

**结论**：这不是"语义冲突"，而是普通的 Null 语义定义问题。SizedPtr 作为几何类型不自带 Null 语义，RBF 层定义自己的约定。

---

### D4: Address64 vs SizedPtr 关系 (I-TERM-DRIFT-RISK)

**决策**：✅ SizedPtr **直接替代** Address64，不是"共存"或"部分替代"。

**监护人分析**（原话）：
> 没啥混用的，接口层对外就是用 SizedPtr 替代 Address64。是直接增强替代关系。我建议研究在 RBF 层本身，是否依然有必要保留 Address64 类型。我估计 Address64 类型已无继续存在的价值。

**结论**：
- Interface 层对外：SizedPtr 完全替代 Address64
- 需要调查：RBF 层内部是否还需要 Address64（可能已无存在价值）

---

## 7. 关键洞察总结

监护人的输入揭示了一个重要认知转变：

| 之前理解（Resolve 初稿） | 实际情况（监护人澄清） |
|:-------------------------|:----------------------|
| SizedPtr 是"可选增强" | SizedPtr 是 Interface 层核心类型 |
| Address64 与 SizedPtr 共存 | SizedPtr 直接替代 Address64 |
| 需要复杂的语义边界规则 | 简单：对外全部用 SizedPtr |
| Null 语义是"冲突" | Null 语义由 RBF 层定义约定 |

**下一步影响**：
- Shape-Tier 大幅简化（不需要复杂的"共存策略"）
- Rule-Tier 聚焦两点：
  1. RBF 层如何定义 NullPtr
  2. 确认 Address64 是否可以完全移除
- Plan-Tier 修订策略清晰：全局替换 Address64 → SizedPtr

