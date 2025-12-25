# 任务: T-P1-01 Fence/常量定义

## 元信息
- **任务 ID**: T-P1-01
- **Phase**: 1 (RBF Layer 0)
- **类型**: 实施
- **优先级**: P0
- **预计时长**: 1 小时

---

## 背景

这是 StateJournal MVP 的**首个编码任务**！

RBF (Robust Binary Format) 是 StateJournal 的底层帧格式。Fence 是 RBF 的魔数，用于帧边界识别和崩溃恢复。

---

## 目标

实现 `RbfConstants.cs`，定义 RBF 格式的核心常量。

---

## 规范依据

- `atelia/docs/StateJournal/rbf-format.md` §2 Fence

**条款覆盖**：
- `[F-FENCE-DEFINITION]`: Fence = 0x31464252 ('RBF1' in ASCII, little-endian)
- `[F-GENESIS]`: 空文件以单个 Fence 开始

---

## 实现要求

### 目标文件
- `atelia/src/Rbf/RbfConstants.cs`

### 代码结构

```csharp
namespace Atelia.Rbf;

/// <summary>
/// RBF (Robust Binary Format) 核心常量定义。
/// </summary>
public static class RbfConstants
{
    /// <summary>
    /// RBF 魔数 "RBF1" 的 little-endian 表示。
    /// 用于帧边界识别和崩溃恢复时的重同步。
    /// </summary>
    /// <remarks>
    /// ASCII: 'R'=0x52, 'B'=0x42, 'F'=0x46, '1'=0x31
    /// Little-endian uint32: 0x31464252
    /// </remarks>
    public const uint Fence = 0x31464252;

    /// <summary>
    /// Fence 的字节序列表示（用于写入和扫描）。
    /// </summary>
    public static ReadOnlySpan<byte> FenceBytes => [0x52, 0x42, 0x46, 0x31];

    /// <summary>
    /// Fence 的字节长度。
    /// </summary>
    public const int FenceLength = 4;
}
```

### 测试文件
- `atelia/tests/Rbf.Tests/RbfConstantsTests.cs`

### 测试用例

```csharp
public class RbfConstantsTests
{
    [Fact]
    public void Fence_HasCorrectValue()
    {
        Assert.Equal(0x31464252u, RbfConstants.Fence);
    }

    [Fact]
    public void FenceBytes_MatchesFenceValue()
    {
        var bytes = RbfConstants.FenceBytes;
        var fromBytes = BitConverter.ToUInt32(bytes);
        Assert.Equal(RbfConstants.Fence, fromBytes);
    }

    [Fact]
    public void FenceBytes_IsRBF1InAscii()
    {
        var bytes = RbfConstants.FenceBytes;
        Assert.Equal((byte)'R', bytes[0]);
        Assert.Equal((byte)'B', bytes[1]);
        Assert.Equal((byte)'F', bytes[2]);
        Assert.Equal((byte)'1', bytes[3]);
    }

    [Fact]
    public void FenceLength_Is4()
    {
        Assert.Equal(4, RbfConstants.FenceLength);
        Assert.Equal(RbfConstants.FenceLength, RbfConstants.FenceBytes.Length);
    }
}
```

---

## 验收标准

- [ ] `RbfConstants.cs` 已创建，包含 Fence、FenceBytes、FenceLength
- [ ] `RbfConstantsTests.cs` 已创建，包含 4 个测试用例
- [ ] `dotnet build` 成功
- [ ] `dotnet test` 全部通过
- [ ] 代码符合项目编码规范（XML 文档注释）

---

## 备注

这是首个编码任务，也是验证 runSubagent 模板在实际编码中效果的机会。

完成后请汇报：
1. 实现是否顺利
2. 模板是否需要调整
3. 发现的任何问题
