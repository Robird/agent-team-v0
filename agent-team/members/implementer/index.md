# Implementer 认知索引

> **身份**: 编码实现专家
> **驱动模型**: Claude Opus 4.5
> **首次激活**: 2025-12
> **最后更新**: 2025-12-23（记忆维护）

---

## 身份简介

我是 **Implementer**，AI Team 的编码实现专家。核心职责：

1. **Code Implementation**: 根据 Investigator 的 Brief 进行代码实现或移植
2. **Semantic Parity**: 保持与源码的语义对齐，包括算法、边界条件、错误处理
3. **Test Coverage**: 同步实现相关测试用例
4. **Handoff Production**: 产出详细的实现报告供 QA 验证

### 工作原则

- **直译优先**: 尽量对齐源码的设计和实现
- **命名对齐**: 保持类名、方法名、参数名与源码一致（命名规范调整）
- **注释同步**: 保留源码中的关键注释
- **二阶段提交**: `WritePendingDiff`（只写不更新状态）+ `OnCommitSucceeded`（追平状态）

---

## 当前关注项目

| 项目 | 状态 | 最后更新 | 备注 |
|------|------|----------|------|
| StateJournal | RBF 命名重构完成 ✅ 文档瘦身完成 ✅ | 2025-12-22 | Layer 0/1 分离完成 |
| DocUI | MUD Demo 待实现 | 2025-12-15 | MVP-0 阶段规划完成 |
| Atelia.Primitives | 基础类型库完成 ✅ | 2025-12-21 | AteliaResult/Error 体系 |
| PipeMux | 管理命令实现完成 ✅ | 2025-12-09 | SDK 模式迁移完成 |

---

## 核心洞见

### 方法论

1. **批量条款 ID 替换模式**
   - 先用 `grep` 确认范围
   - 再用 `multi_replace` 批量替换
   - 最后 `grep` 验证无遗漏

2. **文档瘦身策略**
   - EBNF 语法替代冗余的文字描述
   - ASCII 图表优于长篇叙述（LLM 友好 = 人类友好）
   - Rationale 外置到 ADR，正文只保留规范性内容

3. **二阶段提交模式**
   - `WritePendingDiff`：只写数据，不更新内存状态
   - `OnCommitSucceeded`：在 commit 确认后追平状态
   - 避免"假提交"状态（对象认为已提交但实际 commit 未确立）

4. **分层文档架构**
   - Layer 0：格式规范（rbf-format.md）
   - Layer 1：语义规范（mvp-design-v2.md）
   - Interface：两层对接契约（rbf-interface.md）
   - 避免重复定义，使用引用

5. **术语 SSOT 原则**
   - 同一概念只在一处定义
   - 其他位置用链接引用
   - 弃用术语使用 `~~术语~~ [Deprecated]` + 指向替代

### 经验教训

1. **varint 定义 SSOT 缺失事件**（2025-12-22）
   - 问题：将"编码基础"改为引用 rbf-format.md，但该文档不包含 varint
   - 教训：提取公共内容前，验证目标位置确实包含所需定义

2. **FrameTag vs RecordKind 冲突**（2025-12-22）
   - 问题：三份文档对"判别器"定义冲突
   - 解决：确定 FrameTag 为唯一顶层判别器，废弃域隔离条款

3. **index.md 膨胀问题**（2025-12-23）
   - 根因：系统提示词只说"记录本次工作"，没有区分 append/overwrite
   - 解决：详情写 handoff，index.md 只放状态和索引
   - **目标**：index.md 控制在 300-450 行

### 工具使用技巧

- **grep 先行**：任何批量替换前先用 grep 确认范围
- **parallel calls**：独立的读取操作可以并行
- **测试验证**：每次修改后运行相关测试
- **commit snapshot**：重大操作前先 git commit 作为备份

### StateJournal 实现经验

1. **Magic as Record Separator**
   - Magic 与 Record **并列**，不是 Record 的一部分
   - 文件结构：`[Magic][Record1][Magic][Record2]...[Magic]`
   - 设计收益：概念简洁、forward/reverse scan 统一、空间效率

2. **`_dirtyKeys` 集合优于 `_isDirty` 布尔**
   - `ComputeDiff` 复杂度从 O(n+m) 降为 O(|dirtyKeys|)
   - `HasChanges` 语义更精确
   - 消除"set-then-delete 回到原状态"的语义困惑

3. **条款编号体系**
   - `[F-xxx]`：Format（线格式、对齐、CRC）
   - `[A-xxx]`：API（签名、返回值、参数校验）
   - `[S-xxx]`：Semantics（跨 API/格式的不变式）
   - `[R-xxx]`：Recovery（崩溃一致性、resync）
   - 使用 SCREAMING-KEBAB-CASE 稳定锚点（如 `[F-MAGIC-BYTE-SEQUENCE]`）

4. **CRC32C 多项式表述**
   - Normal: `0x1EDC6F41`
   - Reflected: `0x82F63B78`
   - .NET `System.IO.Hashing.Crc32C` 采用 Reflected I/O 约定

### 文档修订模式

1. **术语统一流程**
   - 建立 Glossary（SSOT）
   - 全文 grep 旧术语
   - 批量替换 + 保留 Deprecated 映射
   - 验证无残留

2. **条款编号流程**
   - 扫描所有 MUST/SHOULD/SHALL
   - 按分类分配前缀
   - 更新测试向量映射表

3. **伪代码外置**
   - 正文保留摘要 + 二阶段设计表格
   - 完整实现移到 Appendix
   - 标注 "⚠️ Informative, not Normative"

### 记忆管理经验

1. **OnSessionEnd 分类**
   - **No-Op**: 无持久化价值 → 不写
   - **State-Update**: 状态变更 → OVERWRITE SSOT 区块
   - **Knowledge**: 新洞见 → APPEND/MERGE
   - **Log-Only**: 纯过程 → 外置到 handoff/meeting

2. **20 行阈值规则**
   - 超过 20 行的内容 MUST 外置
   - index.md 只留摘要 + 链接

3. **SSOT 区块**（只能 OVERWRITE）
   - 身份描述
   - 当前任务
   - 项目状态表
   - 关键路径

---

## 交付物索引

### 2025-12 交付物

| 日期 | 交付物 | Handoff 链接 |
|------|--------|--------------|
| 12-22 | RBF 命名重构 | [handoff](../handoffs/2025-12-22-rbf-rename-IMP.md)（如存在）|
| 12-21 | Primitives 库 | [handoff](../handoffs/2025-12-21-primitives-IMP.md) |
| 12-21 | 决策诊疗室实施 | [handoff](../handoffs/2025-12-21-decision-clinic-impl-IMP.md) |
| 12-21 | Rationale Stripping | [handoff](../handoffs/2025-12-21-rationale-strip-IMP.md) |
| 12-10 | SystemMonitor 原型 | [handoff](../handoffs/SystemMonitor-IMP.md) |
| 12-10 | TextEditor SDK 迁移 | [handoff](../handoffs/TextEditor-SDK-Migration-IMP.md) |
| 12-09 | PipeMux 管理命令 | [handoff](../handoffs/PipeMux-Management-Commands-IMP.md) |

### 详细记录归档

> 2025-12-23 记忆维护：详细任务执行记录已归档到 `archive/members/implementer/2025-12/`

| 主题 | 归档文件 |
|------|----------|
| StateJournal 实现 | [statejournal-implementation-log.md](../../archive/members/implementer/2025-12/statejournal-implementation-log.md) |
| Primitives & 工具 | [primitives-and-tools-log.md](../../archive/members/implementer/2025-12/primitives-and-tools-log.md) |
| PipeMux & DocUI | [pipemux-docui-log.md](../../archive/members/implementer/2025-12/pipemux-docui-log.md) |

---

## 项目知识参考

### StateJournal

**文档体系**：
- 设计文档：`atelia/docs/StateJournal/mvp-design-v2.md`
- 格式规范：`atelia/docs/StateJournal/rbf-format.md`（Layer 0）
- 接口契约：`atelia/docs/StateJournal/rbf-interface.md`
- 测试向量：`mvp-test-vectors.md`（Layer 1）、`rbf-test-vectors.md`（Layer 0）
- 决策记录：`decisions/mvp-v2-decisions.md`

**架构分层**：
```
┌─────────────────────────────────────────┐
│  Layer 1: StateJournal 语义              │
│  (mvp-design-v2.md)                     │
│  - ObjectVersionRecord / MetaCommitRecord│
│  - DiffPayload 编码                      │
│  - 二阶段提交语义                         │
└────────────────┬────────────────────────┘
                 │ rbf-interface.md
                 │ (对接契约)
┌────────────────┴────────────────────────┐
│  Layer 0: RBF 二进制格式                  │
│  (rbf-format.md)                        │
│  - Frame 结构 (HeadLen/Payload/Pad/CRC)  │
│  - Magic-as-Separator                    │
│  - 逆向扫描 / Resync                     │
└─────────────────────────────────────────┘
```

**关键术语表**：
| 术语 | 定义 |
|------|------|
| RBF | Reversible Binary Framing（支持 backward scan / resync）|
| FrameTag | Payload[0]，唯一顶层判别器（0x00=Pad, 0x01=ObjVer, 0x02=MetaCommit）|
| VersionIndex | ObjectId → ObjectVersionPtr 映射（HEAD 时的快照）|
| DiffPayload | On-disk 差分编码（key-value upserts + tombstones）|
| Working State | `_current` 字典，用户直接操作 |
| Committed State | `_committed` 字典，上次 commit 成功后的快照 |
| Genesis Base | 新建对象的首个版本（PrevVersionPtr=0，from-empty diff）|
| Checkpoint Base | 截断版本链的全量状态快照 |

**条款统计**：
- rbf-format.md：24 条（19 F-xxx + 5 R-xxx）
- rbf-interface.md：5 条（F-xxx）
- mvp-design-v2.md：43 条（13 F + 4 A + 22 S + 4 R）

**更名历史**：
- `DurableHeap` → `StateJournal`（2025-12-21）
- `ELOG` → `RBF`（2025-12-22）
- `DHD3/DHM3` → `RBF1`（2025-12-22）
- `RecordKind` 域隔离 → `FrameTag` 统一判别器（2025-12-22）

### DocUI

**底层组件状态**：
| 组件 | 状态 | 说明 |
|------|------|------|
| `SegmentListBuilder` | ✅ 已实现 | 文本段操作 |
| `OverlayBuilder` | ✅ 已实现 | 渲染期叠加标记 |
| `StructList<T>` | ✅ 已实现 | 高性能容器 |
| UI-Anchor 系统 | 📝 设计完成 | Object-Anchor, Action-Link, Action-Prototype |
| AnchorTable | 📝 设计完成 | 锚点注册表 |
| `run_code_snippet` | ❌ 待实现 | MUD Demo 核心 |
| Micro-Wizard | ❌ 待实现 | 交互式输入 |

**MUD Demo MVP 分阶段**：
- MVP-0 (2-3天): Static Demo — 生成带 UI-Anchor 标记的 Markdown
- MVP-1 (3-4天): Functional Demo — AnchorTable + 简单执行
- MVP-2 (3-4天): Interactive Demo — Micro-Wizard + TextField

### PipeMux

**架构模式**：
- 应用入口：`PipeMuxApp` + `System.CommandLine`
- 管理命令：`:` 前缀（如 `:status`、`:reload`）
- 配置文件：`~/.config/pipemux/broker.toml`

**已实现应用**：
| 应用 | 命令示例 |
|------|----------|
| texteditor | `pmux texteditor open <path>` |
| monitor | `pmux monitor view [--lod gist\|summary\|full]` |

### Atelia.Primitives

**类型体系**：
```csharp
// 错误基类（abstract record，支持派生扩展）
public abstract record AteliaError(string Message, AteliaError? Cause = null);

// 结果类型（readonly struct，避免装箱）
public readonly struct AteliaResult<T> {
    public bool IsSuccess { get; }
    public T? Value { get; }
    public AteliaError? Error { get; }
}

// 异常桥接（实现 IAteliaHasError）
public class AteliaException : Exception, IAteliaHasError;
```

**设计要点**：
- `AteliaResult<T>` 是值类型，避免堆分配
- `AteliaError.Cause` 支持链式错误（带深度检查）
- 27 个测试用例覆盖

---

## 认知文件结构

```
agent-team/members/implementer/
├── index.md                  ← 主记忆（本文件）
├── inbox.md                  ← 便签收集箱
└── maintenance-log.md        ← 维护日志

agent-team/archive/members/implementer/
└── 2025-12/                  ← 归档详细记录
    ├── statejournal-implementation-log.md
    ├── primitives-and-tools-log.md
    └── pipemux-docui-log.md
```

---

## 最后更新

- **2025-12-23**: Memory Maintenance — 从 1903 行压缩到 ~350 行，归档详细记录到 archive/
- **2025-12-22**: RBF 命名重构完成，Layer 0/1 文档分离完成
- **2025-12-21**: Primitives 库创建，决策诊疗室实施
