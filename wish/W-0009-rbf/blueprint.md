# RBF 实现蓝图

> 本文档是 RBF 实现的**动态蓝图**——记录阶段性施工方案与演进方向。
> 随着实现推进，已完成的 Stage 会被压缩或移除，新的洞察会补充进来。
> **维护者**：每个 Stage 完成后由执行者更新。

---

## 施工阶段（Stage）

### Stage 01: 项目骨架与类型骨架 ✅
> 已完成（2026-01-14）。详见 `recap.md`。

---

### Stage 02: 常量与 Fence✅
> 已完成（2026-01-14）。详见 `recap.md`。

---

### Stage 03: 简单写入路径（Append） ✅
> 已完成（2026-01-14）。详见 `recap.md`。
> 关键成果：CRC32CHelper、FrameStatusHelper、RbfRawOps.SerializeFrame、RbfFileImpl.Append
> 测试覆盖：84 个测试全部通过

---

### Stage 04: 随机读取（ReadFrame） ✅
> 已完成（2026-01-15）。详见 `recap.md`。
> 关键成果：RbfRawOps.ReadFrame、RbfReadError、FrameStatus 解码
> 测试覆盖：146 个测试全部通过

---

### Stage 05: ReadFrame 重构与 Buffer 外置 ✅
> 已完成（2026-01-17）。详见 `recap.md` 和 `stage/05/manual-refactor.md`。
> 关键成果：`IRbfFrame` 接口、`RbfPooledFrame` 类型、`RbfReadImpl`（Buffer 外置 + Pooled 两种模式）、`SizedPtr` 类型简化（`long`/`int`）
> 测试覆盖：150 个测试全部通过

---

### Stage 06: 复杂写入路径（BeginAppend/EndAppend）
**目标**：实现流式写入 Builder。

**交付物**：
- `BeginAppend` 返回 `RbfFrameBuilder`
- `RbfFrameBuilder.Payload` (IReservableBufferWriter) 集成
- `EndAppend` 提交帧
- Auto-Abort（Dispose 未 EndAppend 时）
- 单 Builder 约束
- 对应的单元测试

---

### Stage 07: 逆向扫描（ScanReverse）
**目标**：实现逆向扫描与 Resync。

**设计方向**（待细化）：
- `ScanReverse` 返回 `FrameInfo`（或 `RbfFrameHeader`）序列而非 `RbfFrame`
- `FrameInfo` 包含 `SizedPtr` + `Tag` + `IsTombstone`，不含 Payload
- `FrameInfo` 可能绑定 `RbfFileImpl` 实例，支持惰性读取
- ScanReverse 只做 framing 校验，不做 CRC 校验
- 调用方按需使用 `ReadFrameInto` 获取完整 Payload

**交付物**：
- `FrameInfo` 类型定义（具体设计待 Stage 05 完成后细化）
- `ScanReverse(showTombstone)` 实现
- 逆向遍历（从尾到头）
- Tombstone 过滤（默认隐藏）
- Resync 机制（损坏帧跳过）
- 对应的单元测试

---

### Stage 08: DurableFlush 与 Truncate
**目标**：实现持久化和恢复能力。

**交付物**：
- `DurableFlush` 落盘
- `Truncate` 截断（4B 对齐验证）
- 对应的单元测试

---

### Stage 09: 测试向量与集成验证
**目标**：对照 `rbf-test-vectors.md` 完成集成测试。

**交付物**：
- 测试向量用例实现
- 端到端场景覆盖
- 代码 Review

---

## 演进方向（Backlog）

> 以下为 MVP 之后的潜在演进方向，优先级可能随需求变化。

- **P2**：异步版本（`RandomAccessByteSinkAsync`）
- **P2**：性能优化（CRC32C 增量计算、Read Window 调优）
- **P3**：错误码体系完善
- **P3**：`CrcCheckPolicy` 可选参数（如需更灵活的 CRC 控制）

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-16 | 插入 Stage 05（ReadFrame 重构），原 Stage 05-08 顺延为 06-09 |
| 2026-01-16 | 设计决策：移除 ReadFrame 兼容层、简化 CRC 策略、ScanReverse 返回 FrameInfo |
| 2026-01-15 | Stage 04 完成：ReadFrame 实现 + 146 个测试通过 |
| 2026-01-14 | Stage 03 完成：Append 实现 + 84 个测试通过 |
| 2026-01-14 | 文档重构：`todo.md` → `blueprint.md`，语义从"待办"改为"动态蓝图" |
| 2026-01-14 | Stage 01、02 完成，压缩为引用 |
| 2026-01-14 | 设计决策：Stage 03/05 独立实现 + 共享辅助函数 |
| 2026-01-14 | 初始版本：8 阶段施工方案 |
