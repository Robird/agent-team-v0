# 任务: 完成 Phase 1 剩余任务 (T-P1-02 ~ T-P1-05)

## 元信息
- **任务 ID**: T-20251225-04 (批量任务)
- **Phase**: 1 (RBF Layer 0)
- **类型**: 批量实施
- **优先级**: P0
- **预计时长**: 2-3 小时

---

## 背景

T-P1-01 已完成（10 分钟），Phase 1 剩余 4 个任务。

**监护人建议**：战术层可以一次性执行一系列 runSubagent 调用，以较大粒度调度工作。

---

## 目标

完成 Phase 1 剩余全部任务，使 RBF Layer 0 达到可用状态。

---

## 任务清单

| 任务 ID | 名称 | 预估 | 依赖 | 条款覆盖 |
|---------|------|------|------|----------|
| T-P1-02 | Frame 布局与对齐 | 2h | T-P1-01 | `[F-FRAME-LAYOUT]`, `[F-FRAME-4B-ALIGNMENT]`, `[F-HEADLEN-FORMULA]` |
| T-P1-03 | CRC32C 实现 | 1h | — | `[F-CRC32C-COVERAGE]`, `[F-CRC32C-ALGORITHM]` |
| T-P1-04 | IRbfFramer/Builder | 3h | T-P1-02, T-P1-03 | `[A-RBF-FRAMER-INTERFACE]`, `[A-RBF-FRAME-BUILDER]` |
| T-P1-05 | IRbfScanner/逆向扫描 | 3h | T-P1-04 | `[A-RBF-SCANNER-INTERFACE]`, `[R-REVERSE-SCAN-ALGORITHM]` |

---

## 执行策略

你有两个选择：

### 策略 A：自行实现
直接编写代码，适合简单任务或你有把握的任务。

### 策略 B：委派给 Implementer
通过 `runSubagent` 调用 Implementer，适合复杂任务或需要专注实现的任务。

**建议**：
- T-P1-02、T-P1-03 可并行（无相互依赖）
- T-P1-04、T-P1-05 串行（有依赖链）

---

## 规范文件

- `atelia/docs/StateJournal/rbf-format.md` — RBF 格式规范
- `atelia/docs/StateJournal/rbf-interface.md` — RBF 接口规范
- `atelia/docs/StateJournal/implementation-plan.md` — 实施计划（含详细任务描述）

---

## 输出目录

- 源码：`atelia/src/Rbf/`
- 测试：`atelia/tests/Rbf.Tests/`

---

## 验收标准

- [ ] T-P1-02: Frame 布局类型定义 + 测试
- [ ] T-P1-03: CRC32C 实现 + 测试（使用标准测试向量）
- [ ] T-P1-04: IRbfFramer/Builder 实现 + 测试
- [ ] T-P1-05: IRbfScanner 实现 + 逆向扫描测试
- [ ] `dotnet build` 成功
- [ ] `dotnet test` 全部通过
- [ ] Phase 1 质量门禁：RBF 读写测试 100% 通过

---

## 汇报要求

完成后请汇报：
1. 各任务完成情况和实际用时
2. 遇到的问题和解决方案
3. 对实施计划/模板的改进建议
4. Phase 1 整体测试通过情况

---

## 备注

这是首次**批量任务**派发，验证战术层自主调度能力。

你可以：
- 自己决定执行顺序
- 自己决定是否委派给 Implementer
- 遇到阻塞时可以请求战略层协助

祝顺利！🚀
