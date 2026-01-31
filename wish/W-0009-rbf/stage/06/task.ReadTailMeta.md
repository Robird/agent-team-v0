# Task: ReadTailMeta 接口实现

> **目标**：实现"仅读取 TailMeta"的专用接口，用于大帧预览/筛选场景
> **前置**：Stage 06 Part A/B 完成（v0.40 帧格式 + ScanReverse）
> **设计决策**：参见 [畅谈会记录](../../../agent-team/meeting/rbf/2026-01-26-tailmeta-read-api.md)

---

## 设计决策摘要

| 议题 | 决议 |
|:-----|:-----|
| API 选择 | 新增专用 `ReadTailMeta()`，不扩展现有 `ReadFrame` |
| 返回类型 | 新建 `RbfTailMeta`（ref struct）+ `RbfPooledTailMeta`（class） |
| CRC 策略 | 只做 TrailerCrc（L2 信任），显式声明不做 PayloadCrc |
| `TailMetaLength = 0` | 返回成功 + 空 Span |

### 信任模型

| 层次 | 校验内容 | API |
|:-----|:---------|:----|
| L1: Framing | TrailerCodeword 可解码 | `ScanReverse` |
| L2: Meta | TrailerCrc 通过 | `ReadTailMeta` ← **本任务** |
| L3: Full | PayloadCrc 通过 | `ReadFrame` |

### 设计原则

1. **正交性**：`ReadFrame`/`ReadTailMeta`/`ScanReverse` 各司其职
2. **意图即命名**：方法名自解释，拒绝布尔参数伪装
3. **类型即约束**：用独立类型阻止误用
4. **诚实地贫瘠**：不暴露 `PayloadLength`，避免虚假示能

---

## 实现计划

### Step 1: 定义 RbfTailMeta 类型

**执行者**：Implementer
**目标**：创建 `RbfTailMeta` ref struct 类型

**上下文文件**：
- 参考：`atelia/src/Rbf/RbfFrame.cs`
- 参考：`atelia/src/Rbf/IRbfFrame.cs`
- 参考：`atelia/src/Rbf/RbfFrameInfo.cs`
- 产出：`atelia/src/Rbf/RbfTailMeta.cs`

**类型定义**：
```csharp
/// <summary>TailMeta 预览结果（L2 信任级别：仅保证 TrailerCrc）。</summary>
/// <remarks>
/// 只读引用结构，生命周期受限于产生它的 buffer。
/// 信任声明：本类型只保证 TrailerCrc 校验通过（L2），不保证 PayloadCrc（L3）。
/// 若需完整数据完整性保证，请使用 <see cref="IRbfFile.ReadFrame"/>。
/// </remarks>
public readonly ref struct RbfTailMeta {
    /// <summary>帧位置凭据（支持"预览→完整读取"工作流）。</summary>
    public SizedPtr Ticket { get; init; }
    
    /// <summary>帧类型标识符。</summary>
    public uint Tag { get; init; }
    
    /// <summary>TailMeta 数据（可能为空）。</summary>
    public ReadOnlySpan<byte> TailMeta { get; init; }
    
    /// <summary>是否为墓碑帧。</summary>
    public bool IsTombstone { get; init; }
    
    // 注意：不暴露 PayloadLength（诚实地贫瘠原则）
}
```

**验收标准**：
- [ ] 编译通过
- [ ] XML 文档完整，包含 L2 信任声明
- [ ] 不暴露 `PayloadLength` 或 `PayloadAndMeta`

---

### Step 1R: 审阅 RbfTailMeta

**执行者**：Craftsman
**目标**：审阅类型定义，确保符合设计决策

**审阅要点**：
- [ ] 类型签名与畅谈会决议一致
- [ ] XML 文档清晰表达信任级别
- [ ] 属性集合遵循"诚实地贫瘠"原则
- [ ] 命名规范符合项目风格

---

### Step 2: 定义 RbfPooledTailMeta 类型

**执行者**：Implementer
**目标**：创建 `RbfPooledTailMeta` class 类型（携带 ArrayPool buffer）

**上下文文件**：
- 参考：`atelia/src/Rbf/RbfPooledFrame.cs`
- 产出：`atelia/src/Rbf/RbfPooledTailMeta.cs`

**类型定义**：
```csharp
/// <summary>携带 ArrayPool buffer 的 TailMeta 预览结果（L2 信任）。</summary>
/// <remarks>
/// 调用方 MUST 调用 <see cref="Dispose"/> 归还 buffer。
/// 生命周期警告：Dispose 后 TailMeta 变为 dangling，不可再访问。
/// Buffer 租用：只租 TailMetaLength 大小，不租整帧大小。
/// </remarks>
public sealed class RbfPooledTailMeta : IDisposable {
    public SizedPtr Ticket { get; }
    public uint Tag { get; }
    public ReadOnlySpan<byte> TailMeta { get; }
    public bool IsTombstone { get; }
    public void Dispose();
}
```

**验收标准**：
- [ ] 编译通过
- [ ] `Dispose()` 正确归还 ArrayPool buffer
- [ ] `Dispose()` 幂等（可多次调用）
- [ ] Dispose 后访问 TailMeta 抛出 `ObjectDisposedException`

---

### Step 2R: 审阅 RbfPooledTailMeta

**执行者**：Craftsman
**目标**：审阅类型定义，确保资源管理正确

**审阅要点**：
- [ ] IDisposable 实现正确
- [ ] Buffer 租用策略符合"只租 TailMetaLength"原则
- [ ] 与 `RbfPooledFrame` 风格一致

---

### Step 3: 实现 RbfReadImpl.ReadTailMeta 核心算法

**执行者**：Implementer
**目标**：在 `RbfReadImpl` 中实现 TailMeta 读取的核心算法

**上下文文件**：
- 参考：`atelia/src/Rbf/Internal/RbfReadImpl.ReadFrame.cs`
- 参考：`atelia/src/Rbf/Internal/RbfReadImpl.cs`（ReadTrailerBefore）
- 参考：`atelia/src/Rbf/Internal/TrailerCodewordHelper.cs`
- 参考：`atelia/src/Rbf/Internal/FrameLayout.cs`
- 产出：`atelia/src/Rbf/Internal/RbfReadImpl.ReadTailMeta.cs`

**I/O 路径（最小化）**：
1. 从 `RbfFrameInfo` 获取 `Ticket`、`TailMetaLength`
2. 计算 TailMeta 在帧内的偏移：`offset = ticket.EndOffsetExclusive - TrailerCodewordSize - PayloadCrcSize - PaddingLen - TailMetaLength`
3. 只读取 TailMeta 那段数据（不读 Payload）
4. **不做 PayloadCrc 校验**（L2 信任，TrailerCrc 已在 ScanReverse 时校验）

**关键点**：
- 需要从 `RbfFrameInfo` 重新获取 `PaddingLen`（需要读 TrailerCodeword 或从 info 推算）
- 或者：在 `RbfFrameInfo` 中增加 `PaddingLen` 字段（需评估）

**API 签名**：
```csharp
// 从 RbfFrameInfo 读取（热路径，避免重复读 trailer）
public static AteliaResult<RbfTailMeta> ReadTailMeta(
    SafeFileHandle file, 
    in RbfFrameInfo info, 
    Span<byte> buffer);

public static AteliaResult<RbfPooledTailMeta> ReadPooledTailMeta(
    SafeFileHandle file, 
    in RbfFrameInfo info);
```

**验收标准**：
- [ ] 编译通过
- [ ] I/O 只读取 TailMeta 区域（不读 Payload）
- [ ] `TailMetaLength = 0` 时返回成功 + 空 Span
- [ ] 返回类型正确填充所有字段

---

### Step 3R: 审阅 ReadTailMeta 核心算法

**执行者**：Craftsman
**目标**：审阅实现，确保 I/O 最小化和正确性

**审阅要点**：
- [ ] I/O 路径确实最小化（只读 TailMeta）
- [ ] 偏移计算正确
- [ ] 错误处理完整
- [ ] 与 ReadFrame 代码风格一致

---

### Step 4: 扩展 IRbfFile 接口

**执行者**：Implementer
**目标**：在 `IRbfFile` 接口中添加 ReadTailMeta 方法声明

**上下文文件**：
- 修改：`atelia/src/Rbf/IRbfFile.cs`
- 参考：`atelia/docs/Rbf/rbf-interface.md`

**新增方法**：
```csharp
/// <summary>读取帧的 TailMeta（预览模式，L2 信任）。</summary>
/// <param name="info">帧元信息（来自 ScanReverse）。</param>
/// <param name="buffer">调用方提供的 buffer，长度 MUST >= info.TailMetaLength。</param>
/// <returns>成功时返回 RbfTailMeta，失败时返回错误。</returns>
/// <remarks>
/// 信任级别：L2（仅保证 TrailerCrc），不校验 PayloadCrc。
/// 若需完整数据完整性保证，请使用 <see cref="ReadFrame"/>。
/// </remarks>
RbfTailMeta ReadTailMeta(in RbfFrameInfo info, Span<byte> buffer);

/// <summary>读取帧的 TailMeta（预览模式，L2 信任，自动租用 buffer）。</summary>
RbfPooledTailMeta ReadPooledTailMeta(in RbfFrameInfo info);
```

**验收标准**：
- [ ] 编译通过
- [ ] XML 文档包含 L2 信任声明
- [ ] 与现有 ReadFrame 方法风格一致

---

### Step 4R: 审阅 IRbfFile 接口扩展

**执行者**：Craftsman
**目标**：审阅接口设计，确保与现有 API 一致

**审阅要点**：
- [ ] 方法签名合理
- [ ] 文档完整
- [ ] 与 `rbf-interface.md` 规范风格一致

---

### Step 5: 实现 RbfFileImpl.ReadTailMeta

**执行者**：Implementer
**目标**：在 `RbfFileImpl` 中实现 IRbfFile.ReadTailMeta

**上下文文件**：
- 修改：`atelia/src/Rbf/Internal/RbfFileImpl.cs`
- 参考：`atelia/src/Rbf/Internal/RbfReadImpl.ReadTailMeta.cs`

**验收标准**：
- [ ] 编译通过
- [ ] 正确委托到 `RbfReadImpl.ReadTailMeta`
- [ ] 传递正确的 `_fileHandle`

---

### Step 5R: 审阅 RbfFileImpl 实现

**执行者**：Craftsman
**目标**：审阅门面层实现

**审阅要点**：
- [ ] 委托正确
- [ ] 无冗余逻辑
- [ ] 错误处理一致

---

### Step 6: 编写测试用例

**执行者**：Implementer
**目标**：为 ReadTailMeta 编写完整测试

**上下文文件**：
- 参考：`atelia/tests/Rbf.Tests/RbfReadFrameTests.cs`
- 参考：`atelia/tests/Rbf.Tests/RbfScanReverseTests.cs`
- 产出：`atelia/tests/Rbf.Tests/RbfReadTailMetaTests.cs`

**测试用例**：
1. **正常路径**：读取有 TailMeta 的帧，验证数据正确
2. **空 TailMeta**：`TailMetaLength = 0` 时返回成功 + 空 Span
3. **Pooled 版本**：验证 buffer 租用和释放
4. **Dispose 后访问**：Pooled 版本 Dispose 后访问抛异常
5. **与 ScanReverse 组合**：`ScanReverse` → `ReadTailMeta` 工作流
6. **预览→完整读取**：`ReadTailMeta` → `ReadFrame` 工作流（验证 Ticket 可用）
7. **大帧小 Meta**：验证 I/O 只读 TailMeta（可通过 mock 或计数验证）

**验收标准**：
- [ ] 所有测试通过
- [ ] 覆盖正常路径和边界情况
- [ ] 测试命名清晰

---

### Step 6R: 审阅测试用例

**执行者**：Craftsman
**目标**：审阅测试覆盖度和质量

**审阅要点**：
- [ ] 覆盖所有关键路径
- [ ] 测试命名符合 Given-When-Then 风格
- [ ] 断言完整
- [ ] 无冗余测试

---

### Step 7: 更新规范文档

**执行者**：DocOps
**目标**：在 `rbf-interface.md` 中补充 ReadTailMeta 相关规范

**上下文文件**：
- 修改：`atelia/docs/Rbf/rbf-interface.md`

**新增内容**：
1. 在术语表补充 TailMeta 身份定义（独立元数据域）
2. 补充三层信任模型（L1/L2/L3）
3. 新增 `ReadTailMeta` 接口规范条款
4. 新增 `RbfTailMeta` 类型定义

**验收标准**：
- [ ] 规范与实现一致
- [ ] 条款编号符合规范

---

## runSubagent 调用计划

| 序号 | Agent | 任务 | 上下文文件 |
|:-----|:------|:-----|:-----------|
| 1 | Implementer | Step 1: 定义 RbfTailMeta | RbfFrame.cs, IRbfFrame.cs |
| 1R | Craftsman | 审阅 RbfTailMeta | RbfTailMeta.cs |
| 2 | Implementer | Step 2: 定义 RbfPooledTailMeta | RbfPooledFrame.cs |
| 2R | Craftsman | 审阅 RbfPooledTailMeta | RbfPooledTailMeta.cs |
| 3 | Implementer | Step 3: ReadTailMeta 核心算法 | RbfReadImpl.*.cs, TrailerCodewordHelper.cs |
| 3R | Craftsman | 审阅核心算法 | RbfReadImpl.ReadTailMeta.cs |
| 4 | Implementer | Step 4: 扩展 IRbfFile 接口 | IRbfFile.cs |
| 4R | Craftsman | 审阅接口扩展 | IRbfFile.cs |
| 5 | Implementer | Step 5: 实现 RbfFileImpl | RbfFileImpl.cs |
| 5R | Craftsman | 审阅门面实现 | RbfFileImpl.cs |
| 6 | Implementer | Step 6: 编写测试 | RbfReadFrameTests.cs, RbfScanReverseTests.cs |
| 6R | Craftsman | 审阅测试 | RbfReadTailMetaTests.cs |
| 7 | DocOps | Step 7: 更新规范 | rbf-interface.md |

---

## 规范引用

| 条款 | 文档 | 要点 |
|------|------|------|
| @[A-RBF-FRAME-INFO] | rbf-interface.md | RbfFrameInfo 定义 |
| @[F-FRAME-DESCRIPTOR-LAYOUT] | rbf-format.md | TailMetaLen 存储位置 |
| @[S-RBF-SCANREVERSE-TOMBSTONE-FILTER] | rbf-interface.md | Tombstone 过滤行为 |
| (新增) | rbf-interface.md | ReadTailMeta L2 信任语义 |

---

## 相关文档

| 文档 | 职责 |
|:-----|:-----|
| [畅谈会记录](../../../agent-team/meeting/rbf/2026-01-26-tailmeta-read-api.md) | 设计决策来源 |
| [task.md](task.md) | Stage 06 主任务（前置） |
| [design-draft.md](design-draft.md) | 帧布局参考 |
