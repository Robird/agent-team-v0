---
docId: "W-0006-Shape"
title: "W-0006 Shape-Tier"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
---

# W-0006 Shape-Tier

> **一句话**：SizedPtr 完全替代 Address64，成为 RBF Interface 层的核心 Frame 句柄类型。

---

## 1. 核心概念边界

### SizedPtr（新）

**定义**：RBF Interface 层的核心 Frame 句柄类型。8 字节紧凑表示 offset+length 的区间。

**定位**（监护人原话）：
> Interface 层的御用对外 Frame 句柄（可持久化），类似文件偏移空间的 span。

**来源**：`Atelia.Data.SizedPtr`（已在 W-0004 实现并通过测试）

### Address64（已废弃）

**状态**：完全由 SizedPtr 替代，不再存在。

**迁移依据**（监护人原话）：
> 没啥混用的，接口层对外就是用 SizedPtr 替代 Address64。是直接增强替代关系。

---

## 2. Glossary Alignment（术语对照表）

| 术语（新） | 术语（旧） | 定义 | 备注 |
|:----------|:----------|:-----|:-----|
| `SizedPtr` | `Address64` | 8 字节紧凑表示的 offset+length 区间 | 完全替代 |
| `SizedPtr.OffsetBytes` | `Address64.Value` | 指向 Frame 起点的 file offset | 4B 对齐，38-bit（~1TB 范围） |
| `SizedPtr.LengthBytes` | （无） | Frame 的字节长度 | 4B 对齐，26-bit（~256MB 范围） |
| `RbfInterface.NullPtr` | `Address64.Null` | RBF 层的 Null 约定 | `= default(SizedPtr)` |
| `ptr == default` | `address.IsNull` | 判断是否为无效引用 | 标准值类型判等 |

**术语合同**：
- 跨文档使用时，`Offset` 一词专指 `SizedPtr.OffsetBytes`
- 不再出现 `Address64` 术语

---

## 3. Interface Contract（关键用途）

基于监护人的核心描述，SizedPtr 在 RBF 中有三个关键用途：

### 3.1 写数据路径

**场景**：`Append()` / `Commit()` 返回值

**改进**（监护人原话）：
> 一次性告诉外界地址+长度。RBF append 数据返回的 Address64 只包含偏移，导致后续随机读取时需要：先读开头拿长度，再次读取全长，验证。如果用 RandomAccess 类进行 IO，需要至少 2 次独立的 IO。

**新行为**：返回 `SizedPtr`，调用方获得完整的 offset+length 信息。

### 3.2 读数据路径

**场景**：`TryReadAt()` 参数、Frame 定位

**改进**（监护人原话）：
> 一次性划分缓存和从磁盘读取。用胖指针，一次性就能完整读取一个 Frame 然后进行校验。

**新行为**：传入 `SizedPtr`，直接定位并校验长度，单次 IO 完成读取。

### 3.3 持久化存储

**场景**：Frame 引用的外部存储

**行为**：`SizedPtr.Packed`（8 字节 `ulong`）可直接序列化到存储介质，恢复后仍可定位 Frame。

---

## 4. 与 StateJournal 的约束

### 4.1 256MB Length 上限

**决策**（监护人确认）：足够用，之前已分析过，最终选定。

**理由**：RBF Frame 的典型大小远小于 256MB，该上限对实际场景无影响。

### 4.2 StateJournal 使用方式

StateJournal 作为 RBF 的首个目标用户：
- 使用 `SizedPtr` 作为 Frame 引用的持久化表示
- 依赖 `OffsetBytes` 定位 Frame 起点
- 依赖 `LengthBytes` 预分配缓冲区、校验读取完整性

### 4.3 边界约束

| 约束 | RBF 承诺 | StateJournal 假设 |
|:-----|:---------|:-----------------|
| 最大 Frame 长度 | ~256MB（26-bit × 4B） | 单 Frame 不超过 256MB |
| 最大文件偏移 | ~1TB（38-bit × 4B） | 单文件不超过 1TB |
| 对齐 | 4 字节对齐 | Frame 边界 4B 对齐 |
| Null 语义 | `default(SizedPtr)` 表示无效 | 检查 `ptr == default` |

---

## 5. 接口签名变更概览

| 接口 | 原签名 | 新签名 |
|:-----|:------|:------|
| `IRbfFramer.Append()` | `Address64` 返回值 | `SizedPtr` 返回值 |
| `RbfFrameBuilder.Commit()` | `Address64` 返回值 | `SizedPtr` 返回值 |
| `IRbfScanner.TryReadAt()` | `Address64` 参数 | `SizedPtr` 参数 |
| `RbfFrame.Address` | `Address64` 属性 | `SizedPtr Ptr` 属性 |

---

## 6. 设计演进链接

- **决策动机**：[Resolve.md](Resolve.md) §6-7
- **规则条款**：[Rule.md](Rule.md)
- **Address64 调查报告**：[w0006-address64-value-check.md](../../../agent-team/handoffs/w0006-address64-value-check.md)

