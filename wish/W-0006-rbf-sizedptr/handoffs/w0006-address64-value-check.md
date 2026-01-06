# <deleted-place-holder> 存在价值快速调查

> **调查员**: Investigator
> **调查日期**: 2026-01-05
> **任务来源**: Wish W-0006
> **背景**: 监护人澄清 SizedPtr 完全替代 <deleted-place-holder>，需验证 <deleted-place-holder> 是否仍有不可替代的存在价值

---

## 1. <deleted-place-holder> 使用点清单

### 1.1 rbf-interface.md 中的使用点（接口层）

| 位置 | 用途 | 语义 | 是否需要长度？ |
|:-----|:-----|:-----|:--------------|
| [#L82-L97](../../../atelia/docs/Rbf/rbf-interface.md#L82) | §2.3 类型定义 | 8 字节 LE 文件偏移量 | **定义本身** |
| [#L90](../../../atelia/docs/Rbf/rbf-interface.md#L90) | `<deleted-place-holder>.Null` 常量 | `Value=0` 表示无效地址 | N/A |
| [#L106-L107](../../../atelia/docs/Rbf/rbf-interface.md#L106) | §2.4 Frame 术语说明 | 写入返回 <deleted-place-holder>，读取通过 <deleted-place-holder> 定位 | ✅ 需要（读取时需知长度） |
| [#L134](../../../atelia/docs/Rbf/rbf-interface.md#L134) | `IRbfFramer.Append()` 返回值 | 新写入帧的起始地址 | ✅ 需要（否则无法一次读取） |
| [#L187](../../../atelia/docs/Rbf/rbf-interface.md#L187) | `RbfFrameBuilder.Commit()` 返回值 | 新写入帧的起始地址 | ✅ 需要（同上） |
| [#L262](../../../atelia/docs/Rbf/rbf-interface.md#L262) | `IRbfScanner.TryReadAt()` 参数 | 要读取的帧起始地址 | ✅ 需要（定位后需知长度） |
| [#L359](../../../atelia/docs/Rbf/rbf-interface.md#L359) | `RbfFrame.Address` 属性 | 帧的起始地址 | ✅ 需要（配合 payload 范围使用） |
| [#L376](../../../atelia/docs/Rbf/rbf-interface.md#L376) | 示例代码 `WriteFrame()` | 返回类型 | ✅ 需要 |
| [#L383](../../../atelia/docs/Rbf/rbf-interface.md#L383) | 示例代码 `ProcessFrame()` | 参数类型 | ✅ 需要 |

### 1.2 rbf-format.md 中的使用点（格式层）

| 位置 | 用途 | 语义 | 是否需要长度？ |
|:-----|:-----|:-----|:--------------|
| [#L28](../../../atelia/docs/Rbf/rbf-format.md#L28) | §1 范围说明 | 引用接口层定义 | N/A |
| [#L292-L302](../../../atelia/docs/Rbf/rbf-format.md#L292) | §7 Wire Format | 8 字节 u64 LE 文件偏移量 | **编码定义** |
| [#L310](../../../atelia/docs/Rbf/rbf-format.md#L310) | `DataTail` 定义 | 指向有效数据末尾的地址 | ❌ 纯位置（文件截断点） |

### 1.3 条款引用汇总

| 条款 ID | 定义位置 | 内容 |
|:--------|:---------|:-----|
| `[F-ADDRESS64-DEFINITION]` | rbf-interface.md#L84 | 8 字节 LE 文件偏移量定义 |
| `[F-ADDRESS64-ALIGNMENT]` | rbf-interface.md#L96 | 4B 对齐约束 |
| `[F-ADDRESS64-NULL]` | rbf-interface.md#L97 | `Value=0` 表示 null |
| `[F-PTR64-WIRE-FORMAT]` | rbf-format.md#L296 | Wire format 编码规范 |

---

## 2. 可替代性分析

### 2.1 核心问题：是否存在"仅需位置、不需长度"的场景？

通过逐一分析上述使用点：

| 使用场景 | 当前类型 | 实际需求 | SizedPtr 可替代？ |
|:---------|:---------|:---------|:-----------------|
| `Append()` 返回值 | <deleted-place-holder> | 返回 offset+length 能一次读取 | ✅ 完全可替代 |
| `Commit()` 返回值 | <deleted-place-holder> | 同上 | ✅ 完全可替代 |
| `TryReadAt()` 参数 | <deleted-place-holder> | 传入 SizedPtr 可直接定位+校验长度 | ✅ 完全可替代 |
| `RbfFrame.Address` | <deleted-place-holder> | 配合 Payload 表达范围 | ✅ SizedPtr 更直接 |
| `DataTail` | 地址 | 表示 EOF 位置 | ✅ 可用 `SizedPtr(offset, 0)` |

**唯一候选例外**：`DataTail`（文件截断点）

- **当前语义**：纯位置，表示"截断到这里"
- **SizedPtr 替代方案**：`SizedPtr(offset, 0)` 或 `SizedPtr(0, offset)` 表示空区间
- **分析**：SizedPtr 的 `OffsetBytes` 可以完全表达相同语义。`Length=0` 的 SizedPtr 在数学上表示"空区间"，语义上可解读为"位置标记"

### 2.2 关键洞察：<deleted-place-holder> 的所有用途都是为了"定位 Frame"

监护人的分析揭示了核心问题：

> RBF append 数据返回的 <deleted-place-holder> 只包含偏移，导致后续随机读取时需要：先读开头拿长度，再次读取全长，验证。如果用 RandomAccess 类进行 IO，需要至少 2 次独立的 IO。

**结论**：<deleted-place-holder> 的所有接口层用途（`Append` 返回、`TryReadAt` 参数、`RbfFrame.Address`）都指向同一个需求——**定位 Frame 并读取其内容**。SizedPtr 通过携带长度信息，彻底解决了这个问题。

### 2.3 "纯位置"需求的真实情况

**问题**：是否有场景真的只需要位置，不需要长度？

**回答**：在 RBF 的设计中——**没有**。

理由：
1. **写入返回**：返回 <deleted-place-holder> 后，调用方持久化这个地址，将来要读取。读取时需要知道长度才能一次 IO。返回 SizedPtr 是严格更优的。
2. **读取定位**：传入 <deleted-place-holder> 后，Scanner 需要先读 HeadLen 才知道帧边界。传入 SizedPtr 可以直接校验。
3. **DataTail**：虽然语义上是"截断点"，但 SizedPtr 的 `OffsetBytes` 完全等价于 <deleted-place-holder> 的 `Value`。

---

## 3. Null 语义迁移分析

### 3.1 当前 <deleted-place-holder>.Null 的使用方式

```csharp
public readonly record struct <deleted-place-holder>(ulong Value) {
    public static readonly <deleted-place-holder> Null = new(0);
    public bool IsNull => Value == 0;
}
```

**语义**：`Value=0` 表示"无效地址"（文件偏移 0 被 Genesis Fence 占据，不可能是有效 Frame 起点）

### 3.2 迁移方案

监护人已明确建议：

> 把 <deleted-place-holder> 的 Null 相关成员函数改为 RBF 层的静态函数/常量。
> 在 RBF 层定义：`public static SizedPtr NullPtr => default;`

**具体迁移**：

| 原代码 | 迁移后 |
|:-------|:------|
| `<deleted-place-holder>.Null` | `RbfConstants.NullPtr` 或 `SizedPtr.Empty` |
| `address.IsNull` | `ptr == default` 或 `ptr.IsEmpty()` |

### 3.3 迁移障碍评估

**无障碍**。理由：
1. SizedPtr 的 `Packed=0` 状态是天然的"零值"（offset=0, length=0）
2. RBF 层只需定义一个常量 `NullPtr = default(SizedPtr)` 即可
3. 这是 RBF 层的**业务约定**，不影响 SizedPtr 作为几何类型的纯净性

---

## 4. 结论

### 4.1 核心判断

**<deleted-place-holder> 可以完全移除**。没有发现不可替代的存在价值。

### 4.2 证据总结

| 维度 | 结论 |
|:-----|:-----|
| 接口层用途 | 全部可被 SizedPtr 替代，且 SizedPtr 提供更好的"一次 IO"能力 |
| 格式层用途 | Wire format 本质是 u64 LE，类型名可改为 SizedPtr 引用 |
| "仅需位置"场景 | 不存在。DataTail 可用 `SizedPtr.OffsetBytes` 表达 |
| Null 语义 | 由 RBF 层定义 `NullPtr = default`，无障碍 |

### 4.3 迁移策略

**Phase 1: 文档修订（本 Wish 范围）**
1. 移除 §2.3 <deleted-place-holder> 定义
2. 移除条款 `[F-ADDRESS64-DEFINITION]`, `[F-ADDRESS64-ALIGNMENT]`, `[F-ADDRESS64-NULL]`
3. 引入 SizedPtr 定义（引用 Atelia.Data）
4. 新增 RBF 层 Null 约定条款（如 `[F-RBF-NULLPTR]`）
5. 更新所有接口签名：<deleted-place-holder> → `SizedPtr`
6. 更新 rbf-format.md §7：`<deleted-place-holder> / Ptr64` → `SizedPtr（Wire Format）`

**Phase 2: 代码实现（未来 Wish）**
- 删除 `<deleted-place-holder>.cs`（已归档在 archive/）
- 更新 RBF 实现使用 SizedPtr

### 4.4 风险评估

| 风险 | 等级 | 缓解措施 |
|:-----|:-----|:---------|
| 遗漏 <deleted-place-holder> 使用点 | 低 | 本调查已穷举 RBF 文档所有出现 |
| 术语混淆（旧文档引用） | 中 | 在变更日志中记录"<deleted-place-holder> → SizedPtr" |
| StateJournal 文档引用 <deleted-place-holder> | 中 | 另一个 Wish 负责 StateJournal 更新 |

---

## 5. 附录：条款变更清单

### 要删除的条款

| 条款 ID | 位置 | 说明 |
|:--------|:-----|:-----|
| `[F-ADDRESS64-DEFINITION]` | rbf-interface.md#L84 | 被 SizedPtr 定义替代 |
| `[F-ADDRESS64-ALIGNMENT]` | rbf-interface.md#L96 | SizedPtr 已有 4B 对齐约束 |
| `[F-ADDRESS64-NULL]` | rbf-interface.md#L97 | 被 RBF 层 NullPtr 约定替代 |

### 要新增的条款

| 条款 ID（建议） | 位置 | 内容 |
|:---------------|:-----|:-----|
| `[F-SIZEDPTR-DEFINITION]` | rbf-interface.md §2.3 | 引用 Atelia.Data.SizedPtr |
| `[F-RBF-NULLPTR]` | rbf-interface.md §2.3 | `default(SizedPtr)` 表示无效帧引用 |

### 要更新的条款

| 条款 ID | 变更 |
|:--------|:-----|
| `[F-PTR64-WIRE-FORMAT]` | 标题改为 SizedPtr Wire Format |

---

> **调查完成**。结论：<deleted-place-holder> 已无存在价值，建议完全移除并迁移至 SizedPtr。
