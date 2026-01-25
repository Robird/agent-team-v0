# Atelia.Rbf 测试模式规范

**创建日期**：2026-01-14
**状态**：Active

---

## 概述

本文档定义 Atelia.Rbf 项目的测试分层策略。核心原则：**职责分离，避免冗余验证**。

---

## 测试架构

```
┌─────────────────────────────────────┐
│ RbfFacadeTests                      │ ← Facade 状态管理测试
│ · 验证 TailOffset 状态更新          │   · 不读取文件内容
│ · 验证多帧偏移序列                  │   · 不验证格式细节
│ · 验证返回值正确转发                │
├─────────────────────────────────────┤
│ RbfRawOpsTests                      │ ← RawOps 格式单元测试
│ · 完整格式验证（HeadLen/CRC/Fence） │   · 读取文件内容
│ · 边界值覆盖（空/小/大 payload）    │   · 验证规范约束
│ · ArrayPool 路径验证                │
├─────────────────────────────────────┤
│ Crc32CHelperTests                   │ ← 工具层单元测试
│ FrameStatusHelperTests              │   · 已有，覆盖完备
└─────────────────────────────────────┘
```

---

## 决策依据

### 问题背景

最初 `RbfAppendTests` 通过 `RbfFileImpl.Append()` 进行集成测试，同时验证：
- Facade 状态管理（TailOffset 更新）
- 底层格式正确性（HeadLen/CRC/Fence）

这导致职责混杂和潜在的冗余验证。

### 分析结论

**RbfFileImpl（Facade）的职责**：
```csharp
public SizedPtr Append(uint tag, ReadOnlySpan<byte> payload) {
    var frameOffset = _tailOffset;                              // 1. 读取状态
    var ptr = RbfRawOps._AppendFrame(..., out long nextTailOffset);  // 2. 委托
    _tailOffset = nextTailOffset;                               // 3. 更新状态
    return ptr;                                                 // 4. 转发返回值
}
```

Facade 只做状态管理，**不需要验证其委托的底层实现细节**。

**RbfRawOps 的职责**：
- 帧布局序列化（@[F-FRAMEBYTES-LAYOUT]）
- CRC 计算（@[F-CRC32C-COVERAGE]）
- 对齐保证（@[S-RBF-DECISION-4B-ALIGNMENT-ROOT]）
- ArrayPool 租用逻辑

格式验证应该**集中在 RawOps 测试中**，作为规范的可执行版本。

### 选择路线 B 的理由

| 维度 | 路线 A（共享 TestHelpers） | 路线 B（职责分离）✅ |
|:-----|:-------------------------|:--------------------|
| 冗余 | 两处都读取文件、验证格式 | 格式验证只在 RawOps |
| 抽象开销 | 需要 TestHelpers 层 | 无额外抽象 |
| 职责清晰度 | 中等 | 高 |
| 可扩展性 | 中等 | 高（未来操作同模式） |

---

## 测试编写指南

### RbfRawOpsTests：格式验证

**职责**：验证 RawOps 层输出的字节序列符合规范。

**Assert 方法**（留在此类中）：
- `AssertAlignment(ptr, tailOffset)` — @[S-RBF-DECISION-4B-ALIGNMENT-ROOT]
- `AssertFence(data, offset)` — @[F-FENCE-RBF1-ASCII-4B]
- `AssertHeadTailSymmetry(data, offset, headLen)` — @[F-FRAMEBYTES-LAYOUT]
- `AssertCrc(data, offset, headLen)` — @[F-CRC32C-COVERAGE]

**测试模板**：
```csharp
[Fact]
public void _AppendFrame_SingleFrame_WritesCorrectFormat() {
    // Arrange
    var path = GetTempFilePath();
    using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);
    
    // Act
    var ptr = RbfRawOps._AppendFrame(handle, 0, tag, payload, out long nextTailOffset);
    
    // Assert - 读取文件内容，完整验证格式
    var data = new byte[nextTailOffset];
    RandomAccess.Read(handle, data, 0);
    AssertAlignment(ptr, nextTailOffset);
    AssertHeadTailSymmetry(data, 0, (uint)headLen);
    AssertCrc(data, 0, (uint)headLen);
    AssertFence(data, headLen);
}
```

### RbfFacadeTests：状态管理

**职责**：验证 Facade 正确维护状态、正确转发返回值。

**不做**：
- 不读取文件内容
- 不验证帧格式
- 不重复 RawOps 已覆盖的断言

**测试模板**：
```csharp
[Fact]
public void Append_UpdatesTailOffset() {
    // Arrange
    using var rbf = RbfFile.CreateNew(path);
    
    // Act
    var ptr = rbf.Append(tag, payload);
    
    // Assert - 只验证状态和返回值
    int expectedHeadLen = RbfRawOps.ComputeHeadLen(payload.Length, out _);
    Assert.Equal(4UL, ptr.OffsetBytes);  // HeaderFence 后
    Assert.Equal((uint)expectedHeadLen, ptr.LengthBytes);
    Assert.Equal(4 + expectedHeadLen + 4, rbf.TailOffset);
}
```

---

## 未来操作的统一模式

| 操作 | RawOps 单元测试 | Facade 状态测试 |
|:-----|:----------------|:----------------|
| `Append` | 格式验证（HeadLen/CRC/Fence） | TailOffset 更新、多帧偏移 |
| `ReadFrame` | 解码验证（Payload/Tag/Status） | 返回值转发、错误码传递 |
| `BeginFrame` | Builder 格式验证 | Builder 生命周期、TailOffset 更新时机 |
| `ScanReverse` | 遍历逻辑验证 | Iterator 状态管理 |

---

## 文件清单

| 文件 | 职责 |
|:-----|:-----|
| `RbfRawOpsTests.cs` | RawOps 格式单元测试 |
| `RbfFacadeTests.cs` | Facade 状态管理测试 |
| `RbfFileFactoryTests.cs` | 工厂方法测试（CreateNew/OpenExisting） |
| `Crc32CHelperTests.cs` | CRC32C 工具测试 |
| `FrameStatusHelperTests.cs` | FrameStatus 工具测试 |

---

## 变更历史

| 日期 | 变更 |
|:-----|:-----|
| 2026-01-14 | 初始版本：确立职责分离测试模式 |
