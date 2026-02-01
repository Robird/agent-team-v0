# Stage 09: 测试向量与集成验证 ✅

> **目标**：对照 `rbf-test-vectors.md` v0.40 完成集成测试，验证端到端场景。
> **前置依赖**：Stage 08（DurableFlush/Truncate）✅
> **状态**：✅ **已完成**（2026-02-01）

---

## 设计决策

### Decision 9.A: 测试覆盖范围

**结论**：基于 `rbf-test-vectors.md` v0.40 补充缺失的测试用例。

**现有覆盖**（已在其他测试文件中）：
- ScanReverse 行为：`RbfScanReverseTests.cs`
- ReadFrame/Framing 校验：`RbfReadImplTests.cs`
- TrailerCodeword 端序：`TrailerCodewordHelperTests.cs`
- TailMeta 读取：`RbfReadTailMetaTests.cs`
- FrameBuilder 流式写入：`RbfFrameBuilderTests.cs`

**待补充覆盖**（本 Stage 实现）：
- 帧长度计算（RBF-LEN-*）：验证 PaddingLen 公式
- FrameDescriptor 位域（RBF-DESCRIPTOR-*）：验证有效值/无效值枚举
- 损坏帧检测（RBF-BAD-*）：补充未覆盖的损坏场景
- CRC 职责分离（READFRAME-CRC-*）：验证 ScanReverse 只校验 TrailerCrc

### Decision 9.B: 测试文件组织

**结论**：创建 `RbfTestVectorTests.cs` 统一存放测试向量验证。

**理由**：
1. 与 `rbf-test-vectors.md` 形成 1:1 对应，便于追溯
2. 已有测试文件按功能职责划分，新增文件专注于"规范验证"
3. 避免在多个文件中分散添加，降低维护成本

---

## 实现任务

### Part A: 帧格式验证

#### Task 9.1: 帧长度计算测试

**执行者**：Implementer
**依赖**：无

**任务简报**：
在 `tests/Rbf.Tests/` 中创建 `RbfTestVectorTests.cs`，实现帧长度计算测试。

**测试用例**（对应 rbf-test-vectors.md §1.4）：
1. **RBF_LEN_001**：PayloadLen = 0,1,2,3 时，验证 PaddingLen 和 FrameLength
2. **RBF_LEN_002**：验证 PaddingLen 取值 0,3,2,1（与 PayloadLen%4 的关系）
3. **RBF_LEN_003**：PayloadLen=10, TailMetaLen=5 → FrameLength=40

**实现要点**：
- 使用 `FrameLayout` 类计算
- 使用 `[Theory]` 参数化测试

**验收标准**：
- [ ] 所有测试通过
- [ ] 覆盖 PaddingLen 四种取值

---

#### Task 9.2: FrameDescriptor 位域测试

**执行者**：Implementer
**依赖**：Task 9.1

**任务简报**：
在 `RbfTestVectorTests.cs` 中添加 FrameDescriptor 位域测试。

**测试用例**（对应 rbf-test-vectors.md §1.6）：
1. **RBF_DESCRIPTOR_001**：MVP 有效值枚举（8 个值）
2. **RBF_DESCRIPTOR_002**：无效值（Reserved bits 非零）

**实现要点**：
- 使用 `TrailerCodewordHelper.BuildDescriptor()` 构建
- 使用 `TrailerCodewordHelper.ValidateReservedBits()` 验证

**验收标准**：
- [ ] 覆盖所有有效值组合
- [ ] 覆盖 Reserved bits 非零场景

---

### Part B: CRC 职责分离验证

#### Task 9.3: CRC 职责分离测试

**执行者**：Implementer
**依赖**：Task 9.2

**任务简报**：
在 `RbfTestVectorTests.cs` 中添加 CRC 职责分离测试。

**测试用例**（对应 rbf-test-vectors.md §3.3 和 §4.3）：
1. **READFRAME_CRC_001**：TrailerCrc 正确 + PayloadCrc 错误 → ReadFrame 失败
2. **READFRAME_CRC_002**：TrailerCrc 正确 + PayloadCrc 错误 → ScanReverse 仍返回帧

**实现要点**：
- 先正常写入帧
- 手动篡改 PayloadCrc32C 字节
- 验证 ReadFrame 失败、ScanReverse 成功

**验收标准**：
- [ ] 验证 CRC 职责分离行为
- [ ] 测试覆盖"PayloadCrc 损坏但 TrailerCrc 完好"场景

---

### Part C: 端到端场景

#### Task 9.4: 端到端集成测试

**执行者**：Implementer
**依赖**：Task 9.3

**任务简报**：
在 `RbfTestVectorTests.cs` 中添加端到端集成测试。

**测试用例**：
1. **E2E_WriteReadRoundtrip**：写入多帧 → ScanReverse → ReadFrame → 验证内容
2. **E2E_TailMetaRoundtrip**：写入带 TailMeta 的帧 → ReadTailMeta → 验证内容
3. **E2E_TombstoneFilter**：写入 Valid + Tombstone + Valid → 验证过滤行为
4. **E2E_TruncateRecovery**：写入 3 帧 → Truncate → 验证只剩前 N 帧
5. **E2E_DurableFlushReopen**：写入 → DurableFlush → 重新打开 → 验证内容

**验收标准**：
- [ ] 覆盖完整写入-读取闭环
- [ ] 验证 Truncate 恢复场景
- [ ] 验证 DurableFlush 持久化

---

### Part D: 补充损坏检测测试

#### Task 9.5: 损坏帧检测补充测试

**执行者**：Implementer
**依赖**：Task 9.4

**任务简报**：
检查现有测试是否已覆盖 RBF-BAD-* 测试向量，补充缺失的用例。

**检查清单**：
- [x] RBF-BAD-001（TrailerCrc 不匹配）→ ReadTrailerBeforeTests（多个测试）、RbfReadImplTests
- [x] RBF-BAD-002（PayloadCrc 不匹配）→ RbfTestVectorTests READFRAME_CRC_001、RbfReadImplTests
- [x] RBF-BAD-003（Frame 起点非 4B 对齐）→ SizedPtr 类型系统保证 4B 对齐（隐式覆盖）
- [x] RBF-BAD-004（TailLen 超界/不足）→ ReadTrailerBeforeTests（多个边界测试）
- [x] RBF-BAD-005（Reserved bits 非零）→ RbfTestVectorTests RBF_DESCRIPTOR_002、ReadTrailerBeforeTests、TrailerCodewordHelperTests
- [x] RBF-BAD-006（TailLen != HeadLen）→ RbfReadImplTests（HeadLenMismatch/TailLenMismatch）
- [x] RBF-BAD-007（PaddingLen 与实际不符）→ RbfTestVectorTests RBF_BAD_007（新增）+ ReadTrailerBeforeTests（负 PayloadLength）

**实现要点**：
- 如果已有测试覆盖，标记并跳过
- 如果未覆盖，在 `RbfTestVectorTests.cs` 中补充

**验收标准**：
- [x] 所有 RBF-BAD-* 用例有对应测试
- [x] 无回归（290 测试全部通过）

---

## 规范引用

| 条款 | 文档 | 要点 |
|------|------|------|
| @[F-FRAMEBYTES-LAYOUT] | rbf-format.md | v0.40 帧布局 |
| @[F-FRAME-DESCRIPTOR-LAYOUT] | rbf-format.md | FrameDescriptor 位域 |
| @[F-PADDING-CALCULATION] | rbf-format.md | PaddingLen 公式 |
| @[F-PAYLOAD-CRC-COVERAGE] | rbf-format.md | PayloadCrc 覆盖范围 |
| @[F-TRAILER-CRC-COVERAGE] | rbf-format.md | TrailerCrc 覆盖范围 |
| @[R-REVERSE-SCAN-USES-TRAILER-CRC] | rbf-format.md | ScanReverse 只校验 TrailerCrc |

---

## 相关文档

| 文档 | 职责 |
|:-----|:-----|
| **本文档（task.md）** | 执行蓝图：任务分解、验收标准 |
| [rbf-test-vectors.md](../../atelia/docs/Rbf/rbf-test-vectors.md) | 测试向量定义 |
| [Stage 08](../08/task.md) | 前置 Stage |
| [recap.md](../../recap.md) | 已完成交付成果汇总 |
| [blueprint.md](../../blueprint.md) | 施工阶段总览 |
