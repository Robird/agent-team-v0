# RBF 实现待办事项

> 本文档记录 RBF 实现的阶段性施工方案和后续方向性思路。
> **维护者**：每个 Stage 完成后由执行者更新。

---

## 阶段性施工方案

### Stage 01: 项目骨架与类型骨架
**目标**：创建 dotnet 9.0 项目结构，实现主要类型的骨架代码（函数体留空或抛 `NotImplementedException`）。

**交付物**：
- `atelia/src/Rbf/Rbf.csproj` - 主项目
- `atelia/tests/Rbf.Tests/Rbf.Tests.csproj` - xUnit 测试项目
- 类型骨架：
  - `IRbfFile` 接口
  - `RbfFile` 静态工厂
  - `RbfFrame` ref struct
  - `RbfFrameBuilder` ref struct
  - `RbfReverseSequence` / `RbfReverseEnumerator` ref struct
  - `RbfRawOps` 静态类（internal）
  - `RandomAccessByteSink` 适配器（internal）
- 基础编译通过

**验收标准**：`dotnet build` 成功，类型签名与 `rbf-interface.md` 对齐。

---

### Stage 02: 常量与 Fence（Genesis）
**目标**：实现 Fence 常量、Genesis 验证、`RbfFile.CreateNew/OpenExisting` 工厂方法。

**交付物**：
- Fence 常量 `RBF1` (ASCII 4B)
- `CreateNew` 创建仅含 Genesis Fence 的空文件
- `OpenExisting` 验证 Genesis Fence
- 对应的单元测试

---

### Stage 03: 简单写入路径（Append）
**目标**：实现 `IRbfFile.Append(tag, payload)` 完整帧写入。

**交付物**：
- `Append` 方法完整实现
- FrameBytes 布局正确（HeadLen, Tag, Payload, Status, TailLen, CRC）
- Fence 写入（帧后）
- `TailOffset` 更新
- 对应的单元测试

---

### Stage 04: 随机读取（ReadFrame）
**目标**：实现 `IRbfFile.ReadFrame(SizedPtr)`。

**交付物**：
- `ReadFrame` 方法完整实现
- Framing 校验（HeadLen/TailLen 一致性、对齐等）
- CRC32C 校验
- `AteliaResult<RbfFrame>` 错误码定义
- 对应的单元测试

---

### Stage 05: 复杂写入路径（BeginAppend/EndAppend）
**目标**：实现流式写入 Builder。

**交付物**：
- `BeginAppend` 返回 `RbfFrameBuilder`
- `RbfFrameBuilder.Payload` (IReservableBufferWriter) 集成
- `EndAppend` 提交帧
- Auto-Abort（Dispose 未 EndAppend 时）
- 单 Builder 约束
- 对应的单元测试

---

### Stage 06: 逆向扫描（ScanReverse）
**目标**：实现逆向扫描与 Resync。

**交付物**：
- `ScanReverse(showTombstone)` 实现
- 逆向遍历（从尾到头）
- Tombstone 过滤（默认隐藏）
- Resync 机制（损坏帧跳过）
- 对应的单元测试

---

### Stage 07: DurableFlush 与 Truncate
**目标**：实现持久化和恢复能力。

**交付物**：
- `DurableFlush` 落盘
- `Truncate` 截断（4B 对齐验证）
- 对应的单元测试

---

### Stage 08: 测试向量与集成验证
**目标**：对照 `rbf-test-vectors.md` 完成集成测试。

**交付物**：
- 测试向量用例实现
- 端到端场景覆盖
- 代码 Review

---

## 后续方向性思路

- **P2**：异步版本（`RandomAccessByteSinkAsync`）
- **P2**：性能优化（CRC32C 增量计算、Read Window 调优）
- **P3**：错误码体系完善

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-14 | 初始版本：8 阶段施工方案 |
