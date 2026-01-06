---
docId: "W-0006-Shape"
title: "W-0006 Shape-Tier"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
---

# W-0006 Shape-Tier

> **一句话**：SizedPtr 完全替代 <deleted-place-holder>，成为 RBF Interface 层的核心 Frame 句柄类型。

---

## 1. 核心概念边界

### SizedPtr（新）

**定义**：RBF Interface 层的核心 Frame 句柄类型。8 字节紧凑表示 offset+length 的区间。

**定位**（监护人原话）：
> Interface 层的御用对外 Frame 句柄（可持久化），类似文件偏移空间的 span。

**来源**：`Atelia.Data.SizedPtr`（已在 W-0004 实现并通过测试）

### <deleted-place-holder>（已废弃）

**状态**：完全由 SizedPtr 替代，不再存在。

**迁移依据**（监护人原话）：
> 没啥混用的，接口层对外就是用 SizedPtr 替代 <deleted-place-holder>。是直接增强替代关系。

---

## 2. Glossary Alignment（术语对照表）

| 术语（新） | 术语（旧） | 定义 |
|:----------|:----------|:-----|
| `SizedPtr` | <deleted-place-holder> | 8 字节紧凑 offset+length 区间，完全替代 <deleted-place-holder> |
| `RbfInterface.NullPtr` | `<deleted-place-holder>.Null` | `= default(SizedPtr)`，表示无效引用 |

**术语合同**：不再出现 <deleted-place-holder> 术语

---

## 3. Interface Contract（关键用途）

SizedPtr 在 RBF 中有三个关键用途：

### 3.1 写数据路径

**改进**：写入方法返回 `SizedPtr`，一次性告诉调用方地址+长度。

原 <deleted-place-holder> 只包含偏移，导致后续随机读取需要至少 2 次独立 IO（先读长度，再读全文）。

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
| `IRbfFramer.Append()` | <deleted-place-holder> 返回值 | `SizedPtr` 返回值 |
| `RbfFrameBuilder.Commit()` | <deleted-place-holder> 返回值 | `SizedPtr` 返回值 |
| `IRbfScanner.TryReadAt()` | <deleted-place-holder> 参数 | `SizedPtr` 参数 |
| `RbfFrame.Address` | <deleted-place-holder> 属性 | `SizedPtr Ptr` 属性 |

---

## 6. 设计演进链接

- **决策动机**：[Resolve.md](Resolve.md) §6-7
- **规则条款**：[Rule.md](Rule.md)
- **<deleted-place-holder> 调查报告**：[w0006-address64-value-check.md](../../../agent-team/handoffs/w0006-address64-value-check.md)

