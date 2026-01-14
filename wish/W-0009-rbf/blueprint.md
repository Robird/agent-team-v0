# RBF 实现蓝图

> 本文档是 RBF 实现的**动态蓝图**——记录阶段性施工方案与演进方向。
> 随着实现推进，已完成的 Stage 会被压缩或移除，新的洞察会补充进来。
> **维护者**：每个 Stage 完成后由执行者更新。

---

## 施工阶段（Stage）

### Stage 01: 项目骨架与类型骨架 ✅
> 已完成（2026-01-14）。详见 `recap.md`。

---

### Stage 02: 常量与 Fence（Genesis） ✅
> 已完成（2026-01-14）。详见 `recap.md`。

---

### Stage 03: 简单写入路径（Append）
**目标**：实现 `IRbfFile.Append(tag, payload)` 完整帧写入。

**设计决策**：独立实现，不依赖复杂路径。通过提取共享辅助函数（如帧序列化工具）实现代码复用。
- 理由：简单路径是"我已准备好"的语义，复杂路径是"延迟决定"的语义——不应让简单的依赖复杂的
- 渐进交付：Stage 03 交付后立即可用于上层测试

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

## 演进方向（Backlog）

> 以下为 MVP 之后的潜在演进方向，优先级可能随需求变化。

- **P2**：异步版本（`RandomAccessByteSinkAsync`）
- **P2**：性能优化（CRC32C 增量计算、Read Window 调优）
- **P3**：错误码体系完善

---

## 变更日志

| 日期 | 变更 |
|------|------|
| 2026-01-14 | 文档重构：`todo.md` → `blueprint.md`，语义从"待办"改为"动态蓝图" |
| 2026-01-14 | Stage 01、02 完成，压缩为引用 |
| 2026-01-14 | 设计决策：Stage 03/05 独立实现 + 共享辅助函数 |
| 2026-01-14 | 初始版本：8 阶段施工方案 |
