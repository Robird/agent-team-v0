# Implementer 认知索引

> **身份**: 编码实现专家
> **驱动模型**: Claude Opus 4.5
> **首次激活**: 2025-12
> **最后更新**: 2026-01-03（记忆维护）

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
| DocGraph | v0.1 完成 ✅ | 2026-01-01 | 93 测试通过 |
| StateJournal | M2 完成 ✅ | 2025-12-28 | 659 测试通过，待 M3 |
| DocUI | 待启动 | 2025-12-15 | MVP-0 规划完成 |
| Atelia.Primitives | 完成 ✅ | 2025-12-21 | AteliaResult/Error 体系 |
| PipeMux | 完成 ✅ | 2025-12-09 | SDK 模式迁移完成 |

---

## 核心洞见

### 方法论

1. **批量条款 ID 替换模式**
   - 先用 `grep` 确认范围 → `multi_replace` 批量替换 → `grep` 验证无遗漏

2. **文档瘦身策略**
   - EBNF 语法替代冗余文字；ASCII 图表优于长篇叙述；Rationale 外置到 ADR

3. **二阶段提交模式**
   - `WritePendingDiff`（只写数据）+ `OnCommitSucceeded`（追平状态）
   - 避免"假提交"状态

4. **分层文档架构**
   - Layer 0（格式）→ Interface（契约）→ Layer 1（语义）
   - 避免重复定义，使用引用

5. **术语 SSOT 原则**
   - 同一概念只在一处定义，其他位置用链接引用
   - 弃用术语使用 `~~术语~~ [Deprecated]` + 指向替代

6. **文档精简技巧**
   - 识别权威来源 → 引用替代重复 → 注释精简为"见 §X.Y"

7. **构成性 vs 规约性规则**（系统提示词设计原则）
   - 构成性规则定义"你是谁"，不抑制涌现
   - 规约性规则规定"该做什么动作"，抑制涌现
   - **应删除**：执行序列、强制输出格式、详细结构规定
   - **应保留**：人格原型、核心关注点、判断标准定义

### RBF 实现洞见

8. **Magic as Record Separator**
   - Magic 与 Record **并列**，不是 Record 的一部分
   - 设计收益：概念简洁、forward/reverse scan 统一、空间效率

9. **StatusLen 边界问题根因**
   - HeadLen/TailLen 记录 FrameBytes 总长度，非 PayloadLen
   - 从 HeadLen 反推 PayloadLen 丢失低 2 位信息

10. **IRbfScanner 逆向扫描**
    - PayloadLen 消歧：枚举 StatusLen=4→1 + CRC 验证
    - `ReadOnlySpan<T>` 无法跨越 `yield return`

11. **VarInt 编码**
    - Canonical 校验：`bytesConsumed == GetVarUIntLength(result)`
    - 第 10 字节只能有 1 bit 有效（0x00/0x01）

12. **ASCII Art 修订规范**（spec-conventions v0.3）
    - 保留教学性 ASCII：加 `(Informative)` 标注
    - 时序图改用 Mermaid sequenceDiagram

### StateJournal 高阶洞见

> 实现细节已归档：[statejournal-impl-details.md](../../archive/members/implementer/2025-12/statejournal-impl-details.md)

13. **`_dirtyKeys` 集合优于 `_isDirty` 布尔**
    - `ComputeDiff` 复杂度从 O(n+m) 降为 O(|dirtyKeys|)
    - 消除"set-then-delete 回到原状态"的语义困惑

14. **条款编号体系**
    - `[F-xxx]`：Format | `[A-xxx]`：API | `[S-xxx]`：Semantics | `[R-xxx]`：Recovery
    - 使用 SCREAMING-KEBAB-CASE 稳定锚点

15. **CRC32C 多项式**
    - Normal: `0x1EDC6F41` / Reflected: `0x82F63B78`
    - .NET `Crc32C` 采用 Reflected I/O 约定

16. **runSubagent 递归分解大任务**
    - 219 编译错误 → 6 个子任务（按文件分组）
    - 大文件进一步分段处理

17. **Clean→Dirty DirtySet 同步 Bug 修复模式**
    - `Workspace.RegisterDirty()` + `DurableObjectBase.NotifyDirty()` + `TransitionToDirty()`

18. **M1 文件后端关键突破**
    - CRC 分块策略（64KB chunk + 增量计算），1GB 文件只需 ~64KB 内存
    - `TryValidateFrameFileBacked` 是核心校验原语

### DocGraph 实现洞见

19. **路径越界检测时机**
    - **先 `IsWithinWorkspace()` 检查原始路径，再 `Normalize()`**——否则越界信息丢失

20. **循环检测双集合模式**
    - `visited` + `inStack`（前者避免重复访问，后者检测当前路径循环）

21. **YamlDotNet 命名转换**
    - `UnderscoredNamingConvention` 将 camelCase 转为 snake_case

### 通用实现技巧

22. **ref struct lambda 限制**
    - 测试异常需改用 try-catch 而非 FluentAssertions

23. **WeakReference GC 测试**
    - `[MethodImpl(NoInlining)]` + 三连 GC + `GC.KeepAlive` 放 Assert 后

24. **Activator.CreateInstance 与 internal 构造函数**
    - 需显式指定 `BindingFlags.NonPublic`

25. **API 分层设计**
    - Core API（非泛型）返回基类，Convenience API（类型化）提供泛型包装

### 协作模式洞见

26. **Recipe 改进实施规划**（2026-01-01）
    - 渐进式路径：Phase 0(基础设施) → Phase 1(结构对齐) → Phase 2(持续改进)
    - 分步执行降低复杂度——大变更分多个 PR，每个可独立回滚

27. **Wish 系统初始化实践**
    - 用系统定义系统本身是检验设计通用性的好方法
    - 条款编号前缀按功能领域分类便于查找

28. **Artifact-Adventures Beacon 写作实践**
    - "隐喻一句 + 工程一句"双句式——防止叙事过度游戏化
    - 词汇护栏（允许词/禁止词）是实用的团队协作工具

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

---

## 交付物索引

> 详细 Handoff 文件位于 `agent-team/handoffs/`，详细实现日志位于 `archive/members/implementer/`

| 时间 | 项目 | 主要交付 |
|------|------|----------|
| 2026-01 | DocGraph v0.1 | 93 测试通过，validate/fix/generate 命令 |
| 2025-12 | StateJournal M2 | 659 测试通过，完整二阶段提交 + Recovery |
| 2025-12 | Atelia.Primitives | AteliaResult/Error 体系，27 测试 |
| 2025-12 | PipeMux | SDK 模式迁移，管理命令 |

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

> 维护日志已压缩。详细历史见 `archive/members/implementer/`

- **2026-01-03**: 记忆维护——去重、压缩历史、简化索引（575→330 行）
- **2026-01-01**: DocGraph v0.1 完成，Recipe 改进规划
- **2025-12-28**: StateJournal M2 完成（659 测试）
- **2025-12-23**: 首次深度记忆维护（1903→350 行）
