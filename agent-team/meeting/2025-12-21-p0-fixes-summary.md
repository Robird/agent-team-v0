# P0 问题修复摘要

> **日期**：2025-12-21
> **执行者**：StandardsChair (刘德智)
> **状态**：✅ 已完成

---

## 背景

基于畅谈会 [2025-12-21-statejournal-impl-readiness-review.md](2025-12-21-statejournal-impl-readiness-review.md) 识别的 9 个 P0 问题，已完成修复。

## 修复清单

| # | 问题 | 修复位置 | 新增条款 |
|---|------|----------|----------|
| 1 | Test Vectors 条款 ID 与规范不一致 | `mvp-test-vectors.md` L346-L475 | 已完成映射表迁移（前序工作坊） |
| 2 | PairCount=0 语义冲突 | `mvp-design-v2.md` L914+ | `[S-PAIRCOUNT-ZERO-LEGALITY]`, `[S-OVERLAY-DIFF-NONEMPTY]`, `[F-UNKNOWN-VALUETYPE-REJECT]` |
| 4 | 首次 Commit 的 VersionIndex | `mvp-design-v2.md` §3.4.6 | `[S-VERSIONINDEX-BOOTSTRAP]` |
| 5 | 读写类型不对称 (Lazy Loading) | `mvp-design-v2.md` §3.1.3 | `[A-OBJREF-TRANSPARENT-LAZY-LOAD]`, `[A-OBJREF-BACKFILL-CURRENT]`，含 `LazyRef<T>` 内部结构代码 |
| 6 | Detached 访问层级未分层 | `mvp-design-v2.md` §3.1.0.1 | `[S-DETACHED-ACCESS-TIERING]` 含访问层级表 |
| 7 | 失败通道不唯一 | `mvp-design-v2.md` §3.3.2 | `[A-LOADOBJECT-RETURN-RESULT]` 含 ErrorCode 表 |
| 9 | TryGetValue API 形态歧义 | `mvp-design-v2.md` §3.4.3 后 | `[A-DURABLEDICT-API-SIGNATURES]` 含完整 C# 签名 |
| - | meta resync 硬约束缺锚点 | `mvp-design-v2.md` §3.2.2 | `[R-META-RESYNC-SAME-AS-DATA]` |
| - | Test Vectors Delete→Remove | `mvp-test-vectors.md` | 全文 `dict.Delete` → `dict.Remove` |

**P0-3 (Transient→Clean 状态转换)**: 经审查，§3.1.0.1 的 mermaid 状态机图已包含 `TransientDirty --> Clean`，文字描述与图一致，无需额外修改。

## 新增条款汇总

### 格式层 [F-*]
- `[F-UNKNOWN-VALUETYPE-REJECT]`: 未知 ValueType 必须 fail-fast

### API 层 [A-*]
- `[A-OBJREF-TRANSPARENT-LAZY-LOAD]`: DurableDict 读 API 透明 Lazy Loading
- `[A-OBJREF-BACKFILL-CURRENT]`: ObjectId 加载后回填 _current
- `[A-LOADOBJECT-RETURN-RESULT]`: LoadObject 返回 AteliaResult
- `[A-DURABLEDICT-API-SIGNATURES]`: DurableDict 完整 API 签名

### 语义层 [S-*]
- `[S-PAIRCOUNT-ZERO-LEGALITY]`: PairCount=0 合法性规则
- `[S-OVERLAY-DIFF-NONEMPTY]`: Overlay diff 非空要求
- `[S-DETACHED-ACCESS-TIERING]`: Detached 访问分层
- `[S-VERSIONINDEX-BOOTSTRAP]`: VersionIndex 引导扇区初始化

### 恢复层 [R-*]
- `[R-META-RESYNC-SAME-AS-DATA]`: meta resync 与 data 相同策略

## 设计意图确认

监护人确认的设计意图：
> 设计意图就是"索引器/TryGetValue 自动检测值类型，若是 `ObjectId` 则触发 `LoadObject` 并**回填**到 `_current`（实现 Transparent Lazy Loading）"
> 
> 建议封装 Lazy Load 逻辑为可复用的类型（因为 Array 等容器也会需要）

已按此意图补充：
1. `LazyRef<T>` 内部结构定义
2. 回填逻辑的明确描述
3. 触发条件（索引器/TryGetValue/Enumerate）

## 下一步

畅谈会状态已更新为"✅ P0 问题已收口，可制定实施计划"

三位顾问建议的实施优先级高度一致：
```
Week 1-2: 格式层基座（RBF Framing, VarInt, Ptr64）
Week 3-4: 存储层语义（DurableDict, DiffPayload, Materialize）
Week 5-6: 协议层集成（Workspace, 二阶段提交, Open/Recovery）
Week 7:   可诊断性（Error Affordance, 边缘情况）
```

---

> El Psy Kongroo.
