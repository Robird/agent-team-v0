# Stage 11 分步骤实施任务计划（按 runSubagent 调用）

> 目标：按可复核的顺序实施 A/B/C/D 四项重构，确保相容自洽。

---

## 0. 兼容性结论（摘要）

- **A + D**：A 的前置校验若采用 D，应返回 `AteliaResult.Failure` 而非抛异常（已在方案 A 中注明）。
- **B 变体 + D**：B 变体已使用 `AteliaResult<SizedPtr>` 返回形式，兼容 D。
- **C 依赖 A/B**：C 的 `ValidateEndOffset` 需要 A 的边界校验统一；建议在 A/B 落地后实施。

无阻碍性冲突，建议顺序：**B 变体 → D → C → QA**。

---

## Task-01（Implementer）：B 变体结构重构 + A 生命周期修复

**目标**：让 `RbfFrameBuilder` 退化为 Facade，提交/状态机收敛到 `RbfFileImpl`；引入 epoch 防旧引用误用；修复 A 中的生命周期问题。

**范围**：
- `RbfFileImpl`：新增 `FileState`，统一状态检查；持有 writer/sink/builder 单例；实现 `CommitFromBuilder` / `AbortBuilder`。
- `RbfFrameBuilder`：保留 `PayloadAndMeta`/`Dispose`/`EndAppend`，内部调用 owner；加入 epoch 校验；移除 `_onCommitCallback` / `_clearBuilderFlag`。
- 生命周期修复：`EndAppend` 成功后清理 active 标记；`PayloadAndMeta` 在 commit/dispose 后拒绝写入；`Append` 添加 Dispose 入口检查。

**runSubagent 提示词**（直接用于调用）：
- Agent: Implementer
- Prompt:
  - 按方案 B 变体实施结构重构：让 `RbfFrameBuilder` 仅作为 Facade，提交逻辑移入 `RbfFileImpl`。
  - 引入 `FileState` / `BuilderState`，并实现 epoch token 防旧 Builder 误用。
  - 落地方案 A 的生命周期修复（commit 后清除 active；commit/dispose 后拒绝写入；`Append` 加 Dispose 检查）。
  - 输出修改文件清单 + 关键差异说明 + 单测建议。

**完成标准**：
- Builder 复用路径无回调；commit/abort 均回到 `Idle`；旧 Builder 误用触发异常。

---

## Task-02（Implementer）：D 结果模型落地

**目标**：将 `EndAppend` 改为 `AteliaResult<SizedPtr>`，新增 `RbfStateError` 并对齐错误分层。

**范围**：
- `RbfFrameBuilder.EndAppend` 返回 `AteliaResult<SizedPtr>`。
- `RbfErrors.cs` 新增 `RbfStateError`。
- 逐一更新调用点与测试（若 Task-01 已引入 `CommitFromBuilder`，需同步返回类型）。

**runSubagent 提示词**：
- Agent: Implementer
- Prompt:
  - 依据方案 D，将 `EndAppend` 的返回类型改为 `AteliaResult<SizedPtr>`。
  - 新增 `RbfStateError`，并将状态违规映射为 Result Failure。
  - I/O 异常保持抛出，不捕获。
  - 更新相关调用点/测试。
  - 输出修改文件清单 + 失败场景映射表。

**完成标准**：
- `EndAppend` 失败返回 `AteliaResult.Failure`，状态违规不抛异常。

---

## Task-03（Implementer）：C 写入核心统一

**目标**：抽取 `RbfFrameWriteCore`，统一尾部写入与 `ValidateEndOffset`，降低两条路径漂移风险。

**范围**：
- 新增 `RbfFrameWriteCore`。
- `RbfAppendImpl` / `RbfFrameBuilder` 使用 `WriteTail`。
- `ValidateEndOffset` 在两条路径统一调用。

**runSubagent 提示词**：
- Agent: Implementer
- Prompt:
  - 依据方案 C 抽取 `RbfFrameWriteCore`。
  - 明确 `WriteTail` 不负责 padding，CRC 需为 finalized。
  - 在 `Append` 与 `EndAppend` 路径均调用 `ValidateEndOffset`。
  - 输出修改文件清单 + 对照测试建议。

**完成标准**：
- 两条路径尾部字节序列一致；`ValidateEndOffset` 在两条路径生效。

---

## Task-04（QA）：状态机与结果模型测试

**目标**：覆盖状态转移矩阵、epoch 防旧引用、Result-Pattern 失败路径。

**runSubagent 提示词**：
- Agent: QA
- Prompt:
  - 基于方案 B/D，补充测试覆盖状态转移矩阵与 epoch 误用。
  - 基于方案 D，覆盖 `EndAppend` 失败返回的 `RbfArgumentError`/`RbfStateError`。
  - 输出新增测试清单与回归范围。

**完成标准**：
- 关键路径测试可复现且稳定通过。

---

## Task-05（DocOps，可选）：文档一致性检查

**目标**：确保接口文档与实现一致（尤其是 `EndAppend` 返回类型与 Result-Pattern）。

**runSubagent 提示词**：
- Agent: DocOps
- Prompt:
  - 检查 `atelia/docs/Rbf/rbf-interface.md` 与当前实现是否一致。
  - 若有漂移，给出修订建议或直接提交补丁。

**完成标准**：
- 文档与实现一致，无关键语义漂移。
