# 任务: 代码同步 rbf-format.md v0.14（FrameStatus 位域格式）

## 元信息
- **任务 ID**: T-20251225-05
- **类型**: 代码同步
- **优先级**: P0
- **预计时长**: 30 分钟

---

## 背景

Phase 1 实施过程中发现 StatusLen 歧义问题，战略层与监护人共同决策：

**rbf-format.md v0.12 → v0.14**

FrameStatus 采用**位域格式**，Bit 7 = Tombstone，Bit 0-1 = StatusLen-1。

---

## 规范变更

### 位域布局（SSOT）

| Bit | 名称 | 说明 |
|-----|------|------|
| 7 | Tombstone | 0 = Valid，1 = Tombstone |
| 6-2 | Reserved | 保留位，MVP MUST 为 0 |
| 1-0 | StatusLen | 状态字节数减 1：`00`=1, `01`=2, `10`=3, `11`=4 |

### MVP 有效值

| 值 | 二进制 | Tombstone | StatusLen |
|----|--------|-----------|-----------|
| `0x00` | `0b0000_0000` | 0 | 1 |
| `0x01` | `0b0000_0001` | 0 | 2 |
| `0x02` | `0b0000_0010` | 0 | 3 |
| `0x03` | `0b0000_0011` | 0 | 4 |
| `0x80` | `0b1000_0000` | 1 | 1 |
| `0x81` | `0b1000_0001` | 1 | 2 |
| `0x82` | `0b1000_0010` | 1 | 3 |
| `0x83` | `0b1000_0011` | 1 | 4 |

### 判断规则

```csharp
bool IsTombstone(byte status) => (status & 0x80) != 0;
bool IsValid(byte status) => (status & 0x80) == 0;
int GetStatusLen(byte status) => (status & 0x03) + 1;
bool IsMvpValid(byte status) => (status & 0x7C) == 0;  // Reserved bits must be zero
```

---

## 需要修改的文件

### 1. FrameStatus.cs

```csharp
/// <summary>
/// RBF 帧状态标记（位域格式）。
/// </summary>
/// <remarks>
/// 位布局：
/// - Bit 7: Tombstone (0=Valid, 1=Tombstone)
/// - Bit 6-2: Reserved (MUST be 0 for MVP)
/// - Bit 1-0: StatusLen - 1
/// </remarks>
public readonly struct FrameStatus
{
    private readonly byte _value;

    private FrameStatus(byte value) => _value = value;

    public bool IsTombstone => (_value & 0x80) != 0;
    public bool IsValid => (_value & 0x80) == 0;
    public int StatusLen => (_value & 0x03) + 1;
    public byte Value => _value;

    /// <summary>
    /// 检查是否为 MVP 合法值（保留位为 0）。
    /// </summary>
    public bool IsMvpValid => (_value & 0x7C) == 0;

    public static FrameStatus CreateValid(int statusLen)
        => new((byte)(statusLen - 1));

    public static FrameStatus CreateTombstone(int statusLen)
        => new((byte)(0x80 | (statusLen - 1)));

    public static FrameStatus FromByte(byte value) => new(value);
}
```

### 2. RbfLayout.cs

更新 `CalculateFrameStatus` 方法使用新值。

### 3. RbfFramer.cs / RbfScanner.cs

- Writer：根据 PayloadLen 计算 StatusLen，使用 `CreateValid`/`CreateTombstone`
- Reader：直接从 FrameStatus 读取 StatusLen，**删除枚举消歧逻辑**

### 4. 测试文件

更新测试用例以使用新的 FrameStatus 值。

---

## 验收标准

- [ ] FrameStatus.cs 更新为新值定义 + 扩展方法
- [ ] RbfLayout 使用新的 FrameStatus 创建方法
- [ ] RbfScanner 简化：直接从 FrameStatus 读取 StatusLen
- [ ] 所有测试通过（可能需要更新测试数据）
- [ ] `dotnet build` 成功
- [ ] `dotnet test` 全部通过

---

## 收益

1. **简化 Scanner**：删除枚举 + CRC 消歧逻辑
2. **自描述格式**：FrameStatus 直接告知 StatusLen
3. **规范对齐**：代码与 rbf-format.md v0.13 一致

---

## 备注

这是规范驱动开发的典型场景：
1. Implementer 发现问题
2. StandardsChair 做出决策并更新规范
3. 战术层同步代码

完成后 Phase 1 才真正 finalize！
