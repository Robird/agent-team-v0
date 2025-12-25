# 任务: 完成 Phase 2 全部任务（核心类型与编码）

## 元信息
- **任务 ID**: T-20251225-06 (批量任务)
- **Phase**: 2 (核心类型与编码)
- **类型**: 批量实施
- **优先级**: P0
- **预计时长**: 2-3 小时（基于 Phase 1 效率）

---

## 背景

Phase 1 (RBF Layer 0) 已完成，157 个测试全部通过！

现在进入 Phase 2，实现 StateJournal 的核心类型和编码。

---

## 目标

完成 Phase 2 全部 6 个任务，输出到 `atelia/src/StateJournal/Core/`。

---

## 任务清单

| 任务 ID | 名称 | 预估 | 条款覆盖 | 验收标准 |
|---------|------|------|----------|----------|
| T-P2-00 | 错误类型定义 | 0.5h | `*-REJECT`, `*-FAILFAST` | `StateJournalError` 继承 `AteliaError` |
| T-P2-01 | Address64/Ptr64 | 1h | `[F-ADDRESS64-*]`, `[F-PTR64-WIRE-FORMAT]` | 对齐测试 `value % 4 == 0` |
| T-P2-02 | VarInt 编解码 | 2h | `[F-VARINT-CANONICAL-ENCODING]` | Canonical 编码测试通过 |
| T-P2-03 | FrameTag 位段编码 | 2h | `[F-FRAMETAG-STATEJOURNAL-BITLAYOUT]` | FRAMETAG-OK-* 通过 |
| T-P2-04 | DurableObjectState 枚举 | 1h | `[A-OBJECT-STATE-*]` | 4 个枚举值 |
| T-P2-05 | IDurableObject 接口 | 2h | `[A-HASCHANGES-O1-COMPLEXITY]` | 存在 test double |

---

## 规范文件

- `atelia/docs/StateJournal/mvp-design-v2.md` — 主规范
- `atelia/docs/StateJournal/implementation-plan.md` — 实施计划（含详细条款映射）

---

## 输出目录

- 源码：`atelia/src/StateJournal/Core/`
- 测试：`atelia/tests/StateJournal.Tests/Core/`

**注意**：需要先创建 `Atelia.StateJournal` 项目骨架（如尚不存在）。

---

## 项目结构建议

```
atelia/src/StateJournal/
├── StateJournal.csproj      ← 新建
├── Core/
│   ├── StateJournalError.cs  ← T-P2-00
│   ├── Address64.cs          ← T-P2-01
│   ├── Ptr64.cs              ← T-P2-01
│   ├── VarInt.cs             ← T-P2-02
│   ├── StateJournalFrameTag.cs ← T-P2-03
│   ├── DurableObjectState.cs ← T-P2-04
│   └── IDurableObject.cs     ← T-P2-05
```

---

## 依赖关系

```
Atelia.Primitives ←── Atelia.Rbf ←── Atelia.StateJournal
                      (Phase 1)      (Phase 2+)
```

---

## 执行策略

你可以：
1. 自行实现简单任务（T-P2-00, T-P2-04）
2. 委派 Implementer 处理复杂任务（T-P2-02, T-P2-03, T-P2-05）
3. 并行执行无依赖任务

**建议执行顺序**：
1. T-P2-00（错误类型）— 被所有其他任务依赖
2. T-P2-01（Address64）— 被 T-P2-03 依赖
3. T-P2-04（枚举）— 被 T-P2-05 依赖
4. T-P2-02（VarInt）— 独立
5. T-P2-03（FrameTag）— 依赖 T-P2-01
6. T-P2-05（IDurableObject）— 依赖 T-P2-04

---

## 验收标准

- [ ] StateJournal 项目骨架创建
- [ ] T-P2-00 ~ T-P2-05 全部完成
- [ ] `dotnet build` 成功
- [ ] `dotnet test` 全部通过
- [ ] Phase 2 质量门禁：所有条款覆盖

---

## 汇报要求

完成后请汇报：
1. 各任务完成情况和实际用时
2. 新增源文件和测试文件清单
3. 测试统计
4. 遇到的问题（如有）

---

## 备注

Phase 1 预估 9-12h，实际 ~3h。Phase 2 预估 8.5h，期待类似效率！

祝顺利！🚀
