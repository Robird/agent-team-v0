# RBF StatusLen 歧义问题技术报告

> **状态**：✅ 已决策 (v2)
> **发起人**：Implementer
> **决策人**：刘德智 (StandardsChair) + 监护人
> **日期**：2025-12-25
> **标签**：`#design` `#decision`

---

## 最终决策

**采纳位域格式**（监护人建议改进）

**位域布局（SSOT）**：

| Bit | 名称 | 说明 |
|-----|------|------|
| 7 | Tombstone | 0 = Valid，1 = Tombstone |
| 6-2 | Reserved | 保留位，MVP MUST 为 0 |
| 1-0 | StatusLen | 状态字节数减 1 |

**MVP 有效值**：

| 值 | Tombstone | StatusLen |
|----|-----------|-----------|
| `0x00`-`0x03` | 0 (Valid) | 1-4 |
| `0x80`-`0x83` | 1 (Tombstone) | 1-4 |

**判断规则**：

```csharp
bool IsTombstone(byte s) => (s & 0x80) != 0;
int GetStatusLen(byte s) => (s & 0x03) + 1;
bool IsMvpValid(byte s) => (s & 0x7C) == 0;
```

**规范更新**：`rbf-format.md` v0.12 → v0.14

**后续任务**：战术层需要更新 `FrameStatus.cs` 和相关测试

---

## 1. 问题描述

### 1.1 现象

在实现 `RbfScanner` 逆向扫描时发现：给定 `HeadLen` 值，无法唯一确定 `PayloadLen` 和 `StatusLen` 的边界。

### 1.2 根因分析

根据当前规范 `[F-HEADLEN-FORMULA]`：

```
HeadLen = 16 + PayloadLen + StatusLen
```

而 `[F-STATUSLEN-FORMULA]` 定义：

```
StatusLen = 1 + ((4 - ((PayloadLen + 1) % 4)) % 4)
```

这保证了 `(PayloadLen + StatusLen) % 4 == 0`。

**问题在于**：取模运算丢失了低 2 位信息。对于任意 `HeadLen - 16` 是 4 的倍数的值，存在 **4 个数学上合法的 (PayloadLen, StatusLen) 组合**。

以 `HeadLen = 20` 为例（`PayloadLen + StatusLen = 4`）：

| PayloadLen | StatusLen | 公式验证 |
|------------|-----------|----------|
| 0 | 4 | ✓ |
| 1 | 3 | ✓ |
| 2 | 2 | ✓ |
| 3 | 1 | ✓ |

所有组合都满足 StatusLen 公式！

### 1.3 当前实现的 Workaround

`RbfScanner` 采用枚举 + CRC 消歧策略：

1. 枚举 StatusLen ∈ {4, 3, 2, 1}
2. 验证 FrameStatus 字节值（0x00 或 0xFF）
3. 验证 FrameStatus 所有字节一致
4. **依赖 CRC32C 验证消歧**——错误边界会导致 CRC 不匹配

这个方案可行，但存在以下不足：
- 最坏情况需要 4 次 CRC 计算
- 逻辑复杂度增加
- 格式本身缺乏自描述性

---

## 2. 候选方案

### 方案 A：HeadLen/TailLen 改为记录 PayloadLen

**改动**：
- `HeadLen` / `TailLen` 改为记录 **纯 Payload 长度**（不含 StatusLen）
- 帧总长度 = `16 + PayloadLen + CalculateStatusLen(PayloadLen)`

**优点**：
- PayloadLen 直接可读，无歧义
- 语义明确："长度字段 = 业务数据长度"

**缺点**：
- Breaking change（需重新设计布局表）
- 跳帧/迭代时需额外计算总长度
- HeadLen/TailLen 不再表示"帧字节长度"，可能造成概念混淆
- HeadLen ≠ TailLen 在对齐后的实际帧内，可能需要重命名字段

**布局示意**（修改后）：

| 偏移 | 字段 | 说明 |
|------|------|------|
| 0 | PayloadLen | 纯业务数据长度 |
| 4 | FrameTag | ... |
| 8 | Payload | N 字节 |
| 8+N | FrameStatus | 1-4 字节对齐填充 |
| ... | PayloadLen | 尾部冗余校验 |
| ... | CRC32C | ... |

---

### 方案 B：FrameStatus 值编码 StatusLen

**改动**：
- FrameStatus 的 Valid 状态不再是单一值 `0x00`
- 使用 `0x01` ~ `0x04` 表示 StatusLen（自描述）
- 保留 `0xFF` 为 Tombstone

**值域定义**：

| FrameStatus 值 | 含义 |
|----------------|------|
| `0x01` | Valid, StatusLen = 1 |
| `0x02` | Valid, StatusLen = 2 |
| `0x03` | Valid, StatusLen = 3 |
| `0x04` | Valid, StatusLen = 4 |
| `0xFF` | Tombstone |
| 其他 | 保留（视为损坏） |

**优点**：
- HeadLen/TailLen 语义不变
- 最小改动：仅修改 FrameStatus 值定义
- 解析时直接读取 StatusLen，无需枚举
- 隐式冗余：多字节 FrameStatus 仍要求所有字节相同

**缺点**：
- FrameStatus 语义从"状态标记"变为"状态+长度"混合
- Valid 不再是单一值，判断逻辑 `status != Tombstone` 变为范围检查

**备选变体 B'**：使用 `0x00` ~ `0x03` 表示 StatusLen - 1

| FrameStatus 值 | 含义 |
|----------------|------|
| `0x00` | Valid, StatusLen = 1 |
| `0x01` | Valid, StatusLen = 2 |
| `0x02` | Valid, StatusLen = 3 |
| `0x03` | Valid, StatusLen = 4 |
| `0xFF` | Tombstone |

优点：保持 `0x00` 作为"最常见的 Valid"（当 PayloadLen % 4 == 3 时）

---

### 方案 C：StatusLen 固定为 4

**改动**：
- 废弃变长 StatusLen，固定为 4 字节
- Payload 后始终补 4 字节 FrameStatus

**优点**：
- 彻底消除歧义
- 简化所有长度计算：`HeadLen = 20 + PayloadLen`
- FrameStatus 字段固定位置，易于定位

**缺点**：
- 每帧最多浪费 3 字节（对于大 Payload 可忽略，对于小帧有影响）
- Breaking change

---

### 方案 D：保持现状（CRC 消歧）

**不改动格式**，依赖实现层的枚举 + CRC 验证。

**优点**：
- 无需修改规范
- 现有测试已验证可行

**缺点**：
- 解析性能略差（最坏 4 次 CRC）
- 格式缺乏自描述性
- 实现复杂度转嫁给所有 Reader

---

## 3. 方案对比

| 维度 | A (改 HeadLen) | B (Status 编码) | C (固定 4B) | D (现状) |
|------|----------------|-----------------|-------------|----------|
| 格式自描述 | ✓ | ✓ | ✓ | ✗ |
| 改动范围 | 大 | 小 | 中 | 无 |
| 空间效率 | 最优 | 最优 | 略差 | 最优 |
| 解析复杂度 | 低 | 低 | 最低 | 高 |
| 语义清晰度 | 中 | 中 | 高 | 低 |
| Breaking | Yes | Yes | Yes | No |

---

## 4. Implementer 倾向

**推荐方案 B**（FrameStatus 值编码 StatusLen）

理由：
1. **最小侵入**：只修改 `[F-FRAMESTATUS-VALUES]` 条款
2. **向后兼容分析**：现有实现中 FrameStatus 全填 `0x00`，在新定义下对应 `StatusLen=1`。虽然语义变了，但对于 `PayloadLen % 4 == 3` 的帧，StatusLen 确实是 1，所以部分旧数据可能恰好兼容
3. **实现简化**：Reader 读取 FrameStatus 后直接知道边界，无需枚举
4. **保持 HeadLen 语义**：跳帧逻辑不变

**备选**：若团队认为混合语义不妥，可考虑方案 C（固定 4B），牺牲少量空间换取最大简洁性。

---

## 5. 待讨论问题

1. **向后兼容性**：是否需要支持读取旧格式文件？还是接受 breaking change？
2. **Tombstone 处理**：方案 B 中 Tombstone 帧的 StatusLen 如何确定？
   - 选项 a：Tombstone 帧的 StatusLen 也编码在值中（需要定义 `0xFD`~`0xFF` 区间）
   - 选项 b：Tombstone 保持 `0xFF`，StatusLen 按公式计算（接受解析时的枚举）
3. **命名**：若采用方案 B，`FrameStatus` 是否需要重命名为 `FrameMarker` 或类似名称？

---

## 6. 下一步

请 Advisor 团队审阅此报告，在畅谈会上讨论并决策。

决策后 Implementer 将：
1. 更新 `rbf-format.md` 规范
2. 修改 `RbfWriter` / `RbfScanner` 实现
3. 更新测试向量

---

## 附录：旧版（0.13版） FrameStatus 定义（供参考）

```markdown
**`[F-FRAMESTATUS-VALUES]`**

| 值 | 名称 | 语义 |
|----|------|------|
| `0x00` | Valid | 正常帧（业务数据） |
| `0xFF` | Tombstone | 墓碑帧（Auto-Abort / 逻辑删除） |
| `0x01`-`0xFE` | — | 保留；Reader MUST 视为 Framing 失败 |
```
