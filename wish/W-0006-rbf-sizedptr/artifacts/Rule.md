---
docId: "W-0006-Rule"
title: "W-0006 Rule-Tier"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
---

# W-0006 Rule-Tier

> **一句话**：定义 NullPtr 约定、废弃 <deleted-place-holder>、列出条款变更清单。

---

## 1. NullPtr 约定定义

### 条款 [R-RBF-NULLPTR]

```csharp
using Atelia.Data;

namespace Atelia.Rbf
{
    /// <summary>
    /// RBF Interface 层的公共常量定义。
    /// </summary>
    public static class RbfInterface
    {
        /// <summary>
        /// RBF 层的 Null 约定：表示"无效的 Frame 引用"。
        /// </summary>
        /// <remarks>
        /// <para>语义：Packed == 0 → (OffsetBytes=0, LengthBytes=0)</para>
        /// <para>用途：方法返回值表示"未找到"、初始化默认值、无效状态标记</para>
        /// </remarks>
        public static readonly SizedPtr NullPtr = default;
    }
}
```

### 语义规则

| 属性 | 值 | 含义 |
|:-----|:---|:-----|
| `NullPtr.Packed` | `0` | 64 位零值 |
| `NullPtr.OffsetBytes` | `0` | 无有效偏移 |
| `NullPtr.LengthBytes` | `0` | 无有效长度 |

### 判等规则

```csharp
// 推荐：使用标准值类型判等
if (ptr == default) { /* 无效引用 */ }

// 等价：显式比较 NullPtr
if (ptr == RbfInterface.NullPtr) { /* 无效引用 */ }

// 等价：检查 Packed
if (ptr.Packed == 0) { /* 无效引用 */ }
```

### 设计依据

监护人建议（原话）：
> 把 <deleted-place-holder> 的 Null 相关成员函数改为 RBF 层的静态函数/常量。在 RBF 层定义：`public static SizedPtr NullPtr => default;`，表示"我们 RBF 层是如何定义 SizedPtr 中的特殊值的"。

**关键澄清**：SizedPtr 作为几何类型不自带 Null 语义（`Packed=0` 数学上表示空区间 `(0,0)`）。Null 语义是 **RBF 层的业务约定**，不影响 SizedPtr 的纯净性。

---

## 2. <deleted-place-holder> 完全移除

### 条款 [R-RBF-<deleted-place-holder>-REMOVED]

**规则**：<deleted-place-holder> 类型及其所有成员完全移除，不再出现在 RBF 设计文档和代码中。

**迁移映射**：

| 原成员 | 迁移目标 | 说明 |
|:-------|:---------|:-----|
| <deleted-place-holder> 类型 | `SizedPtr` | 完全替代 |
| `<deleted-place-holder>.Value` | `SizedPtr.OffsetBytes` | 语义等价 |
| `<deleted-place-holder>.Null` | `RbfInterface.NullPtr` | 移至 RBF 层常量 |
| `<deleted-place-holder>.IsNull` | `ptr == default` | 标准值类型判等 |
| `new <deleted-place-holder>(value)` | `SizedPtr.FromOffsetAndLength()` | 需同时提供 length |

### 设计依据

**Investigator 调查结论**（[w0006-<deleted-place-holder>-value-check.md](../../../agent-team/handoffs/w0006-<deleted-place-holder>-value-check.md)）：

> <deleted-place-holder> 可以完全移除。没有发现不可替代的存在价值。

**关键证据**：
- 接口层用途：全部可被 SizedPtr 替代，且 SizedPtr 提供更好的"一次 IO"能力
- 格式层用途：Wire format 本质是 u64 LE，类型名可改为 SizedPtr
- "仅需位置"场景：不存在（DataTail 可用 `SizedPtr.OffsetBytes` 表达）

---

## 3. 条款变更清单

### 3.1 要删除的条款

| 条款 ID | 原位置 | 说明 |
|:--------|:-------|:-----|
| `[F-<deleted-place-holder>-DEFINITION]` | rbf-interface.md §2.3 | 被 SizedPtr 定义替代 |
| `[F-<deleted-place-holder>-ALIGNMENT]` | rbf-interface.md §2.3 | SizedPtr 已有 4B 对齐约束 |
| `[F-<deleted-place-holder>-NULL]` | rbf-interface.md §2.3 | 被 `[R-RBF-NULLPTR]` 替代 |

### 3.2 要新增的条款

| 条款 ID | 目标位置 | 内容 |
|:--------|:---------|:-----|
| `[F-SIZEDPTR-DEFINITION]` | rbf-interface.md §2.3 | 引用 `Atelia.Data.SizedPtr`，说明 38:26 位分配 |
| `[R-RBF-NULLPTR]` | rbf-interface.md §2.3 | `default(SizedPtr)` 表示无效 Frame 引用 |

### 3.3 要更新的条款

| 条款 ID | 变更内容 |
|:--------|:---------|
| `[F-PTR64-WIRE-FORMAT]` | 标题改为"SizedPtr Wire Format"；内容更新为 SizedPtr 的 8 字节 LE 编码 |

### 3.4 要更新的接口签名

| 位置 | 原类型 | 新类型 |
|:-----|:------|:------|
| `IRbfFramer.Append()` 返回值 | <deleted-place-holder> | `SizedPtr` |
| `RbfFrameBuilder.Commit()` 返回值 | <deleted-place-holder> | `SizedPtr` |
| `IRbfScanner.TryReadAt()` 参数 | <deleted-place-holder> | `SizedPtr` |
| `RbfFrame.Address` 属性 | <deleted-place-holder> | `RbfFrame.Ptr`（`SizedPtr`） |

---

## 4. 边界规则

### 4.1 SizedPtr 约束（继承自 Atelia.Data）

| 约束 | 规则 | 违反后果 |
|:-----|:-----|:---------|
| 4B 对齐 | `OffsetBytes % 4 == 0` 且 `LengthBytes % 4 == 0` | 构造时抛出 `ArgumentException` |
| Offset 上限 | `OffsetBytes <= MaxOffset`（~1TB） | 构造时抛出 `ArgumentOutOfRangeException` |
| Length 上限 | `LengthBytes <= MaxLength`（~256MB） | 构造时抛出 `ArgumentOutOfRangeException` |

### 4.2 RBF 层使用规则

| 规则 | 说明 |
|:-----|:-----|
| 返回值语义 | `Append()`/`Commit()` 成功时返回有效 SizedPtr；失败时返回 `NullPtr` |
| 参数校验 | `TryReadAt()` 接收 `NullPtr` 时立即返回 `false` |
| 持久化 | `SizedPtr.Packed` 可直接序列化为 8 字节 LE |

---

## 5. 设计演进链接

- **概念边界**：[Shape.md](Shape.md)
- **决策动机**：[Resolve.md](Resolve.md) §6-7
- **<deleted-place-holder> 调查报告**：[w0006-<deleted-place-holder>-value-check.md](../../../agent-team/handoffs/w0006-<deleted-place-holder>-value-check.md)

