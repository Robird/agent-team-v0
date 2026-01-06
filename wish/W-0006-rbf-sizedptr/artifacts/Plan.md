---
docId: "W-0006-Plan"
title: "W-0006 Plan-Tier"
produce_by:
  - "wish/W-0006-rbf-sizedptr/wish.md"
---

# W-0006 Plan-Tier

> **一句话**：分两阶段完成 <deleted-place-holder> → SizedPtr 迁移，本 Wish 完成文档层修订。

---

## 1. 修订策略

### Phase 1：文档层修订（本 Wish 范围）

| 步骤 | 内容 | 验收标准 |
|:-----|:-----|:---------|
| 1.1 | 修订 rbf-interface.md | 移除 <deleted-place-holder>，引入 SizedPtr + NullPtr |
| 1.2 | 修订 rbf-format.md | 更新 Wire Format 章节 |
| 1.3 | 添加 artifacts 导航锚点 | 文档头部增加设计演进链接 |

### Phase 2：代码实现（可选，未来 Wish）

| 步骤 | 内容 | 说明 |
|:-----|:-----|:-----|
| 2.1 | 更新 RBF 实现 | 使用 SizedPtr 替代 <deleted-place-holder> |
| 2.2 | 更新 StateJournal | 另一个 Wish 负责 |

---

## 2. 修订清单（rbf-interface.md）

基于 [<deleted-place-holder> 调查报告](../../../agent-team/handoffs/w0006-<deleted-place-holder>-value-check.md) §6 和源文件行号：

### 2.1 删除操作

| 目标 | 操作 | 说明 |
|:-----|:-----|:-----|
| §2.3 <deleted-place-holder> 整节 | 删除 | 被新 §2.3 SizedPtr 替代 |
| 条款索引 `[F-<deleted-place-holder>-DEFINITION]` | 删除 | 条款已移除 |
| 条款索引 `[F-<deleted-place-holder>-ALIGNMENT]` | 删除 | 约束已由 SizedPtr 定义 |
| 条款索引 `[F-<deleted-place-holder>-NULL]` | 删除 | 被 NullPtr 约定替代 |

### 2.2 新增操作

| 目标 | 新增内容 | 条款 ID |
|:-----|:---------|:--------|
| §2.3（原 <deleted-place-holder> 位置） | SizedPtr 定义 + NullPtr 约定 | `[F-SIZEDPTR-DEFINITION]`, `[F-RBF-NULLPTR]` |

### 2.3 替换操作

| 原内容 | 修改为 | 类型 |
|:-------|:-------|:-----|
| §3 "写入返回 <deleted-place-holder>，读取通过 <deleted-place-holder> 定位" | "写入返回 SizedPtr，读取通过 SizedPtr 定位" | 术语替换 |
| `IRbfFramer.Append` 返回类型 <deleted-place-holder> | `SizedPtr` | 签名替换 |
| `RbfFrameBuilder.Commit` 返回类型 <deleted-place-holder> | `SizedPtr` | 签名替换 |
| `IRbfScanner.TryReadAt(<deleted-place-holder> address, ...)` | `TryReadAt(SizedPtr ptr, ...)` | 签名替换 |
| `RbfFrame.Address` 属性 | `RbfFrame.Ptr`（类型 `SizedPtr`） | 属性替换 |
| 示例代码中所有 <deleted-place-holder> | `SizedPtr` | 示例更新 |

### 2.4 详细修订内容

#### 新 §2.3 内容（替换原 <deleted-place-holder> 定义）

```markdown
### 2.3 SizedPtr

**`[F-SIZEDPTR-DEFINITION]`**

> **SizedPtr** 是 8 字节紧凑表示的 offset+length 区间，作为 RBF Interface 层的核心 Frame 句柄类型。

**来源**：`Atelia.Data.SizedPtr`（38:26 位分配方案）

| 属性 | 位数 | 范围 | 说明 |
|:-----|:-----|:-----|:-----|
| `OffsetBytes` | 38-bit | ~1TB | 指向 Frame 起点（HeadLen 字段位置） |
| `LengthBytes` | 26-bit | ~256MB | Frame 的字节长度（含 HeadLen 到 CRC32C） |

**约束**：
- 有效 SizedPtr MUST 4 字节对齐（`OffsetBytes % 4 == 0` 且 `LengthBytes % 4 == 0`）
- 超出范围的值在构造时抛出 `ArgumentOutOfRangeException`

**`[F-RBF-NULLPTR]`**

> `default(SizedPtr)`（即 `Packed == 0`）在 RBF 层表示"无效的 Frame 引用"。

```csharp
// RBF 层的 Null 约定
public static readonly SizedPtr NullPtr = default;

// 判等方式
if (ptr == default) { /* 无效引用 */ }
```

**语义**：
- `NullPtr.OffsetBytes == 0` 且 `NullPtr.LengthBytes == 0`
- 方法返回 `NullPtr` 表示"未找到"或操作失败
- `TryReadAt()` 接收 `NullPtr` 时立即返回 `false`
```

---

## 3. 修订清单（rbf-format.md）

### 3.1 替换操作

| 原内容 | 修改为 | 类型 |
|:-------|:-------|:-----|
| §1 "<deleted-place-holder> 等接口类型" | "SizedPtr 等接口类型" | 术语替换 |
| §7 标题 "<deleted-place-holder> / Ptr64（编码层）" | "SizedPtr（Wire Format）" | 标题替换 |
| §7 "本规范所称"地址（<deleted-place-holder>/Ptr64）"" | "SizedPtr（8 字节紧凑区间）" | 术语替换 |
| 条款 `[F-PTR64-WIRE-FORMAT]` | `[F-SIZEDPTR-WIRE-FORMAT]` | 条款 ID 替换 |
| §7 Wire Format 描述 | 改为 offset+length 语义 | 内容替换 |
| §7 "接口层的类型化封装见...<deleted-place-holder>" | "接口层定义见...SizedPtr" | 引用替换 |
| DataTail "地址（见 §7）" | "SizedPtr.OffsetBytes（见 §7）" | 术语替换 |
| 条款索引 `[F-PTR64-WIRE-FORMAT]` | `[F-SIZEDPTR-WIRE-FORMAT]` | 条款 ID 替换 |

### 3.2 详细修订内容

#### 新 §7 内容（替换原 <deleted-place-holder> / Ptr64 章节）

```markdown
## 7. SizedPtr（Wire Format）

### 7.1 Wire Format

**`[F-SIZEDPTR-WIRE-FORMAT]`**

- **编码**：SizedPtr 在 wire format 上为 8 字节 u64 LE 紧凑编码，包含 offset 和 length。
- **Offset**：指向 Frame 的 `HeadLen` 字段起始位置（38-bit，4B 粒度）。
- **Length**：Frame 的字节长度（26-bit，4B 粒度）。
- **空值**：`Packed == 0`（即 `OffsetBytes == 0 && LengthBytes == 0`）表示 null（无效引用）。
- **对齐**：非零 SizedPtr MUST 4B 对齐。

> 接口层的类型定义见 [rbf-interface.md](rbf-interface.md) 的 `SizedPtr`（`[F-SIZEDPTR-DEFINITION]`）。

### 7.2 DataTail 表达

`DataTail` 使用 `SizedPtr.OffsetBytes` 表达文件截断点（length 部分无意义，可为 0）。
```

---

## 4. Migration Notes

### 4.1 语义增强清单

| 修改点 | 原语义（<deleted-place-holder>） | 新语义（SizedPtr） | 增强说明 |
|:-------|:-------------------|:------------------|:---------|
| `Append()` 返回值 | 仅返回 offset | 返回 offset+length | **一次 IO**：调用方无需先读 HeadLen 再读全帧 |
| `Commit()` 返回值 | 仅返回 offset | 返回 offset+length | 同上 |
| `TryReadAt()` 参数 | 传入 offset，需运行时读 HeadLen | 传入 offset+length，可直接校验 | **预分配**：可提前分配精确大小的缓冲区 |
| `RbfFrame.Address` → `Ptr` | 仅 offset | offset+length | **自包含**：Frame 视图携带完整范围信息 |
| `<deleted-place-holder>.Null` → `NullPtr` | `Value == 0` | `Packed == 0` | **语义等价**：Null 约定从类型移至 RBF 层常量 |
| `address.IsNull` → `ptr == default` | 成员方法 | 标准值类型判等 | **惯用法**：符合 .NET struct 惯例 |

### 4.2 隐性知识记录

#### K1: SizedPtr 的"御用句柄"定位

监护人原话：
> Interface 层的御用对外 Frame 句柄（可持久化），类似文件偏移空间的 span。

**含义**：SizedPtr 不是"可选增强"，而是 RBF Interface 层的**核心类型**。所有对外暴露的 Frame 引用都应使用 SizedPtr。

#### K2: Null 语义是"业务约定"而非"类型内禀"

SizedPtr 作为几何类型（`Atelia.Data`），`Packed=0` 数学上表示空区间 `(0,0)`，本身不携带 Null 语义。

Null 语义是 **RBF 层的业务约定**：
- "我们 RBF 层是如何定义 SizedPtr 中的特殊值的"
- 不影响 SizedPtr 的纯净性（几何类型不应内置业务语义）

#### K3: DataTail 的 SizedPtr 表达

`DataTail` 表示文件截断点（纯位置），使用 `SizedPtr.OffsetBytes` 表达：
- `LengthBytes = 0`（或任意值，忽略）
- 语义上等价于原 <deleted-place-holder> 的 `Value`

这不是"滥用"SizedPtr，而是明确其 `OffsetBytes` 分量的独立可用性。

#### K4: 属性重命名（Address → Ptr）

`RbfFrame.Address` 重命名为 `RbfFrame.Ptr`：
- 反映从"地址"到"胖指针"的语义升级
- 避免新旧代码的隐式兼容（编译器强制更新调用点）

---

## 5. 验收标准

### 5.1 必达标准

| 标准 | 验证方法 |
|:-----|:---------|
| rbf-interface.md / rbf-format.md 中 <deleted-place-holder> 已替换为 `SizedPtr` | 人工复核核心章节 |
| 新增 `SizedPtr` 定义完整 | 复核 §2.3 含 `[F-SIZEDPTR-DEFINITION]`、`[F-RBF-NULLPTR]` |
| 条款索引已同步 | 索引无悬挂引用 |
| frontmatter 含 `produce_by` | 指向本 wish |

### 5.2 变更日志条目

rbf-interface.md 新增条目：
```
| 0.18 | 2026-01-06 | **SizedPtr 替代 <deleted-place-holder>**（W-0006）：移除 <deleted-place-holder> 类型，引入 SizedPtr 作为核心 Frame 句柄；新增 `[F-SIZEDPTR-DEFINITION]`、`[F-RBF-NULLPTR]`；移除 `[F-<deleted-place-holder>-*]` 条款；`RbfFrame.Address` 改为 `RbfFrame.Ptr` |
```

rbf-format.md 新增条目：
```
| 0.17 | 2026-01-06 | **SizedPtr Wire Format**（W-0006）：§7 重写为 SizedPtr 编码；`[F-PTR64-WIRE-FORMAT]` 改为 `[F-SIZEDPTR-WIRE-FORMAT]` |
```

---

## 6. 风险评估

### 6.1 遗漏风险

| 风险 | 等级 | 缓解措施 |
|:-----|:-----|:---------|
| 遗漏 <deleted-place-holder> 使用点 | 低 | 调查报告已穷举 RBF 文档所有出现（9 处） |
| 条款引用悬挂 | 低 | 执行后用 grep 验证 |
| StateJournal 文档引用 <deleted-place-holder> | 中 | 本 Wish 不修订 StateJournal，另一个 Wish 负责 |

### 6.2 破坏性变更

| 变更 | 影响 | 说明 |
|:-----|:-----|:-----|
| `RbfFrame.Address` → `Ptr` | 代码层面的 breaking change | Phase 2 实现时，编译器会报错所有旧调用点 |
| <deleted-place-holder> 类型完全移除 | 代码层面的 breaking change | 同上 |
| 条款 ID 变更 | 跨文档引用可能失效 | 检查 StateJournal 等上层文档是否引用 `[F-<deleted-place-holder>-*]` |

### 6.3 待确认项

| 项 | 说明 | 建议 |
|:---|:-----|:-----|
| StateJournal 对 <deleted-place-holder> 的引用 | 可能在 mvp-design-v2.md 中存在 | Phase 2 或另一个 Wish 处理 |
| 测试向量更新 | rbf-test-vectors.md 可能需要更新 | 检查是否引用 <deleted-place-holder> |

---

## 7. 执行顺序

```
1. rbf-interface.md
   1.1 删除 §2.3 <deleted-place-holder> 定义
   1.2 新增 §2.3 SizedPtr + NullPtr 定义
   1.3 替换接口签名（Append/Commit/TryReadAt/RbfFrame）
   1.4 更新示例代码
   1.5 更新条款索引
   1.6 添加变更日志条目

2. rbf-format.md
   2.1 更新 §1 术语引用
   2.2 重写 §7 为 SizedPtr Wire Format
   2.3 更新 DataTail 描述
   2.4 更新条款索引
   2.5 添加变更日志条目

3. 验收
   3.1 grep 验证 <deleted-place-holder> 清除
   3.2 条款引用完整性检查
   3.3 frontmatter 导航锚点确认
```

---

## 8. 设计演进链接

- **动机与问题**：[Resolve.md](Resolve.md)
- **概念边界**：[Shape.md](Shape.md)
- **规则条款**：[Rule.md](Rule.md)
- **<deleted-place-holder> 调查报告**：[w0006-<deleted-place-holder>-value-check.md](../../../agent-team/handoffs/w0006-<deleted-place-holder>-value-check.md)

