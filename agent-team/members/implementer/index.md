# Implementer 认知索引

> **身份**: 编码实现专家
> **驱动模型**: Claude Opus 4.5
> **首次激活**: 2025-12
> **最后更新**: 2026-01-11（记忆维护）

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
| Rbf | Stage 06 完成 ✅ | 2026-01-25 | v0.40 TrailerCodeword 布局，171 测试通过 |
| DesignDsl | Parser MVP ✅ | 2026-01-14 | 67 测试通过，Term/Clause 节点解析 |
| Atelia.Data | Phase 3 完成 ✅ | 2026-01-11 | SizedPtr 公开 API 改 long/int，测试架构治理完成 |
| DocGraph | v0.2 进行中 🔄 | 2026-01-07 | v0.2: Wish 布局迁移 + IssueAggregator Phase 2 |
| StateJournal | M2 完成 ✅ | 2025-12-28 | 659 测试通过，待 M3 |
| DocUI | 待启动 | 2025-12-15 | MVP-0 规划完成 |
| Atelia.Primitives | 完成 ✅ | 2026-01-16 | DisposableAteliaResult 21 测试，双类型架构 |
| PipeMux | 完成 ✅ | 2025-12-09 | SDK 模式迁移完成 |

---

## 核心洞见

### 方法论原则

1. **SSOT + 引用优先**
   - 同一概念只在一处定义，其他位置用链接引用
   - 弃用术语：`~~术语~~ [Deprecated]` + 指向替代
   - 批量替换三步：`grep` 确认范围 → `multi_replace` 执行 → `grep` 验证

2. **分层文档架构**
   - Layer 0（格式）→ Interface（契约）→ Layer 1（语义）
   - 避免重复定义；精简为"见 §X.Y"引用

3. **二阶段提交模式**
   - `WritePendingDiff`（只写数据）+ `OnCommitSucceeded`（追平状态）
   - Clean→Dirty 同步：`RegisterDirty()` + `NotifyDirty()` + `TransitionToDirty()`

4. **构成性 vs 规约性规则**（提示词设计）
   - 构成性定义"你是谁"（保留）；规约性规定"做什么动作"（删除）
   - 删除：执行序列、强制格式；保留：人格原型、判断标准

5. **纯引用模式**（2026-01-09）
   - 场景：跨层条款语义重复时
   - 保留锚点 + 依赖图 + 一句话引用 SSOT，不重复书写

### RBF/StateJournal 洞见

> 细节归档：[statejournal-impl-details.md](../../archive/members/implementer/2025-12/statejournal-impl-details.md)

6. **Magic as Record Separator**：Magic 与 Record **并列**，概念简洁

7. **IRbfScanner 逆向扫描**
   - StatusLen 边界：HeadLen 记录 FrameBytes 总长度（非 PayloadLen）
   - 消歧：枚举 StatusLen=4→1 + CRC 验证

8. **VarInt Canonical 校验**
   - `bytesConsumed == GetVarUIntLength(result)`
   - 第 10 字节只能 0x00/0x01

9. **`_dirtyKeys` 集合优于 `_isDirty` 布尔**
   - O(|dirtyKeys|) vs O(n+m)；消除"set-then-delete 回原状态"困惑

10. **CRC 分块策略**（M1 文件后端）
    - 64KB chunk + 增量计算，1GB 文件 ~64KB 内存

### DocGraph 洞见

11. **路径越界检测时机**：**先检查原始路径，再 Normalize**

12. **循环检测**：`visited`（避免重复）+ `inStack`（检测当前路径）

13. **produce 路径语义**：相对于 **workspace root**，非源文件
    - `../docs/api.md` 越界；`subdir/../docs/api.md` 合法（归一化为 `docs/api.md`）

14. **TwoTierAggregator 基类**：子类实现 5 个抽象成员，共享两级输出逻辑

### 通用技巧

15. **ref struct 限制**：测试异常用 try-catch 而非 FluentAssertions

16. **WeakReference GC 测试**：`[MethodImpl(NoInlining)]` + 三连 GC + `GC.KeepAlive` 放 Assert 后

17. **API 分层设计**：Core（非泛型基类）+ Convenience（泛型包装）

18. **runSubagent 分解大任务**：219 错误 → 6 子任务（按文件分组）

### 测试架构治理（2026-01-11）

19. **Theory 化中间状态**：接口测试只验证最终结果；中间状态放实现级 Fact

20. **TheoryData 工厂模式**：`Func<(PublicInterface, Delegate)>` 绕过 CS0059

21. **CollectingWriter 双接口**：`IBufferWriter<byte>` + `IByteSink`，Pull/Push 共存

### DSL 迁移扩展点（2026-01-09）

22. **条款类型选择**
    - `decision`：根决策（AI 不可改）
    - `design`：关键约束
    - `hint`：可推导提示
    - `term`：术语锚点

23. **迁移位置清单**
    | 位置 | 变更 |
    |:-----|:-----|
    | 文件头 | 添加 DSL 声明 |
    | 条款定义 | `**[ID]**` → `### design [ID] 标题` |
    | 条款引用 | `` `[ID]` `` → `@[ID]` |
    | 依赖声明 | 添加 `clause-matter depends:` 块 |

### AOS 实现路径（Exploring）

> 2026-01-05 年会后思考，待推进

24. **基础设施复用**：StateJournal(需适配) / DocGraph(直接) / Primitives(直接)

25. **Week-1 MVP**：1 Core + 2 Cortex（Observer/Retriever）；关键：`Observation.Nothing()` 自激振荡

26. **验收→实现映射**：可启动 / 可回放 / 可解释 / 可控成本 / 可插拔

### DesignDsl Parser 洞见（2026-01-14）

27. **Markdig Setext Heading 陷阱**
    - `text\n---` 被解析为 Setext Heading（Level=2），而非 ThematicBreakBlock
    - 测试 Case 需适应 Markdig 实际解析行为

28. **INodeBuilder 职责链模式**
    - 接口 `TryBuild()` 返回 `Node?`，null 表示不匹配
    - Pipeline 按顺序调用，DefaultNodeBuilder 作兜底
    - 扩展：`InsertBefore()` 注册到 Default 之前

29. **AI-Design-DSL 正则模式**
    - Term: `` ^\s*term\s+`([^`]+)`(?:\s+(.+?))?\s*$ ``
    - Clause: `` ^\s*(decision|design|hint)\s+\[([^\]]+)\](?:\s+(.+?))?\s*$ ``
    - 使用 `[GeneratedRegex]` 优化性能

### RBF 实现洞见（2026-01-14 ~ 01-24）

30. **Facade 集成测试 vs RawOps 单元测试**
    - 两层都测，各司其职
    - Facade：状态管理（TailOffset/SizedPtr），不读文件内容
    - RawOps：格式正确性（HeadLen/CRC/Fence），验证字节序列
    - 变更影响隔离：状态变更只影响 Facade 测试，格式变更只影响 RawOps 测试

31. **Owner Token 模式解决 ArrayPool 生命周期**
    - `PooledBufferOwner`（内部类）：幂等释放，`Interlocked.Exchange` 保证线程安全
    - `RbfFrame.Owner`（可选字段）：引用类型，struct 复制后共享，消除 double-return
    - API 分化：`ReadFrameInto(buffer)` → Owner=null；`ReadPooledFrame()` → Owner 有效

32. **Check + Unchecked 分离模式**
    - `CheckReadParams` / `ReadFrameIntoUnchecked`：参数校验与逻辑分离
    - Unchecked 约定：仍保留运行时检查（IOException/FramingError），仅移除参数合法性检查

33. **RBF v0.40 格式变更要点**（2026-01-24）[I-IMP-34]

    **新旧布局对比**：
    | 旧版 | 新版 (v0.40) |
    |:-----|:-------------|
    | `HeadLen(4) + Tag(4) + Payload + Status(1-4) + TailLen(4) + CRC(4)` | `HeadLen(4) + Payload + UserMeta + Padding(0-3) + PayloadCrc(4) + TrailerCodeword(16)` |
    | Tag 在头部 | Tag 移到 TrailerCodeword |
    | Status 字节编码 IsTombstone/StatusLen | FrameDescriptor 位字段编码 IsTombstone/PaddingLen/UserMetaLen |
    | 单 CRC（LE） | 双 CRC：PayloadCrc（LE）+ TrailerCrc（**BE**） |

    **TrailerCodeword 布局**（固定 16B）：
    ```
    [0-3]   TrailerCrc32C   (u32 BE)  ← CRC(FrameDescriptor + FrameTag + TailLen)
    [4-7]   FrameDescriptor (u32 LE)  ← bit31:IsTombstone, bit30-29:PaddingLen, bit15-0:UserMetaLen
    [8-11]  FrameTag        (u32 LE)
    [12-15] TailLen         (u32 LE)  ← 等于 HeadLen
    ```

    **RbfLayout.cs 修改要点**：
    1. **删除**：`TagOffset`/`TagSize` 从 Header 区移除
    2. **新增**：`TrailerCodewordSize = 16`，`TrailerCrcSize/FrameDescriptorSize/FrameTagSize/TailLenSize` 各 4
    3. **重定义 PayloadOffset**：`HeadLenSize`（直接跟在 HeadLen 后面）
    4. **新增 FrameDescriptor 编解码**：
       - `EncodeDescriptor(isTombstone, paddingLen, userMetaLen) → uint`
       - `DecodeDescriptor(uint) → (isTombstone, paddingLen, userMetaLen)`
    5. **FillTrailer → FillTrailerCodeword**：写入 4 字段，**TrailerCrc 用 BE**
    6. **ResultFromTrailer → ReadTrailerCodeword**：读 16B，先用 `RollingCrc.CheckCodewordBackward` 校验

    **RollingCrc API 使用**：
    - `SealCodewordBackward(span)`: CRC 写在 span 开头，BE 存储；Payload 在 CRC 之后
    - `CheckCodewordBackward(span)`: 验证 span 开头的 BE CRC 是否匹配后续内容
    - **对齐 TrailerCodeword**：`span[0..16]` 整体作为 codeword 传入

    **ScanReverse 迭代器实现思路**：
    1. 从文件末尾开始，每次读取 16 字节 TrailerCodeword
    2. 调用 `CheckCodewordBackward` 校验 TrailerCrc
    3. 从 TailLen 得知完整 FrameBytes 长度 → 跳到前一帧
    4. `RbfFrameInfo` 返回：`Ticket(SizedPtr), Tag, PayloadLength, UserMetaLen, IsTombstone`
    5. **无需读取 HeadLen 或 PayloadCrc**——逆向扫描只关心元信息

    **关键注意事项**：
    - TrailerCrc 覆盖 **后三个字段**（12 字节），不含自身
    - PayloadCrc 覆盖 `Payload + UserMeta + Padding`（不含 HeadLen）
    - PaddingLen 编码在 bit30-29（2 bit），取值 0-3
    - UserMetaLen 占 bit15-0（16 bit），最大 65535

34. **ScanReverse 实现决策**（2026-01-17）[I-IMP-35]

    | 决策点 | 结论 | 理由 |
    |:-------|:-----|:-----|
    | Window 大小 | 64KB 固定 | 匹配系统 I/O 粒度，覆盖常见帧 |
    | 大帧 buffer | ArrayPool | 可能很大，需复用 |
    | Window buffer | new byte[] | 生命周期与枚举器绑定，简单优先 |
    | CRC 校验 | 不校验 PayloadCrc | ScanReverse 是结构迭代，非内容验证 |
    | 错误报告 | 静默跳过 | MVP 简化，诊断回调后续按需 |
    | Current 类型 | 完整 RbfFrame | 与 ReadFrame 一致，已读数据不丢弃 |

    **ref struct 枚举器资源管理模式**：
    - ref struct 无析构函数，必须显式 `Dispose()` 或在 `MoveNext()` 返回 false 时归还
    - `ArrayPool` buffer 用可空字段 `byte[]?`，`Dispose()` 内 `Interlocked.Exchange` 幂等归还
    - 支持 duck-typed `using`（foreach 自动调用）

35. **RollingCrc BackwardScanner 语义澄清**（2026-01-23）[I-IMP-36]

    | 概念 | 语义 |
    |:-----|:-----|
    | ForwardScanner | 正向扫描流，检测 `[payload][CRC-LE]` 格式的 Forward Codeword |
    | BackwardScanner | 正向扫描流，检测**字节反转后的 Forward Codeword** |

    **关键**：BackwardScanner **不是**"从末尾扫描"，而是"扫描反转数据"。

    **正逆对称性**：
    - `CheckCodewordBackward(reversed(ForwardCodeword))` → ✅
    - `CheckCodewordForward(reversed(BackwardCodeword))` → ✅
    - BackwardScanner 找到的 `match.Codeword` 是**接收顺序**，非原始格式

    **测试陷阱**：不要直接 Seal 一个 Backward Codeword 然后扫描——应该：
    1. Seal Forward Codeword → 2. 反转字节 → 3. 用 BackwardScanner 扫描

36. **Stage 06 审阅要点**（2026-01-24）[I-IMP-37]

    | 要点 | 决策/确认 |
    |:-----|:---------|
    | TrailerCodewordHelper 返回类型 | 用 `TrailerCodewordData` 结构体（含计算属性），减少调用点重复解码 |
    | RollingCrc.CheckCodewordBackward 输入 | 传完整 16B span，TrailerCodeword 布局完美匹配 |
    | PayloadCrc / TrailerCrc 写入顺序 | 先填充所有字段，最后 seal 两个 CRC |
    | 测试修复估时调整 | 2h → 3-4h（单 CRC → 双 CRC 大改，测试帧构造全部重写） |

    **Stage 06 实施进度**：
    - Task 6.1+6.6: RbfLayout v0.40 布局 + RbfFrameInfo 新建
    - Task 6.2: TrailerCodewordHelper（25 测试）
    - Task 6.3: RbfAppendImpl 重写（双 CRC + UserMeta + Tombstone）
    - Task 6.4: RbfReadImpl 重写（双 CRC 校验链）
    - Task 6.5: 既有测试修复（148 测试通过）
    - Task 6.7: ReadTrailerBefore（21 测试，168 总计）
    - Task 6.8: RbfReverseEnumerator + RbfReverseSequence（171 总计）

    **CRC API 重构**（2026-01-24）：
    - 删除 `Crc32CHelper`，统一使用 `RollingCrc`
    - API 映射：`Init()` → `DefaultInitValue`，`Update()` → `CrcForward()`，`Finalize()` → `^DefaultFinalXor`，`Compute()` → `CrcForward(span)`
    - 测试调整：Rbf.Tests 168 通过（删除 Crc32CHelperTests），Data.Tests 173 通过

### LLM Agent 完工标准差距分析（2026-01-17）[I-IMP-33]

> 从人类打磨 RBF Stage 05 代码中提炼的 6 个关键差距

| 维度 | LLM 倾向 | 人类做法 | 经验 |
|:-----|:---------|:---------|:-----|
| **架构** | partial class 聚合 | 职责分离独立类 | 边界清晰时优先独立类型 |
| **内存所有权** | 内部分配 + 隐式复制 | 显式外置 + 双路径 | 所有权决策权交给调用方 |
| **类型契约** | 运行时全量校验 | 类型系统 + Debug.Assert | 入口点校验，内部信任契约 |
| **算法实现** | 自定义辅助方法 | BCL 高效 API | 优先 `MemoryExtensions` 等向量化 API |
| **类型设计** | 复用现有类型 | 为场景创建专用类型 | 接口支持多态，专用错误类型 |
| **命名** | 技术导向 (`Ptr`) | 语义导向 (`Ticket`) | 表达"是什么"而非"怎么存储" |

**核心原则：做减法**——从"这里好像多余了"的直觉出发，寻找等价变换。

### 经验教训

1. **SSOT 缺失/冲突事件**
   - varint 提取事件（2025-12-22）：将编码基础改为引用，但目标文档不含定义
   - FrameTag vs RecordKind 冲突（2025-12-22）：三份文档定义冲突 → 确定 FrameTag 为唯一判别器
   - **教训**：提取公共内容前，验证目标确实包含所需定义

2. **index.md 膨胀问题**（2025-12-23）
   - 根因：系统提示词只说"记录工作"，没有区分 append/overwrite
   - 解决：详情写 handoff/archive，index.md 只放状态和索引
   - **目标**：控制在 300-450 行

3. **记忆维护核心原则**（2026-01-03）
   - "去重而非删减"——重复内容比冗长内容更损害认知效率
   - 重复信号：关键词重叠率>60%、相同主题多处表述

4. **项目知识定位原则**（2026-01-11）
   - index.md 按"深入入口"定位，非"完整内容"
   - 保留一句话定位 + 归档链接；详细表格外迁 archive/

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
| 2026-01 | DesignDsl | Parser MVP（INodeBuilder 框架 + Term/Clause 解析），67 测试 |
| 2026-01 | Atelia.Primitives | 双类型架构重构（AteliaResult ref struct + AsyncAteliaResult），39 测试 |
| 2026-01 | DocGraph v0.1 | 93 测试通过，validate/fix/generate 命令 |
| 2025-12 | StateJournal M2 | 659 测试通过，完整二阶段提交 + Recovery |
| 2025-12 | Atelia.Primitives | AteliaResult/Error 体系，27 测试 |
| 2025-12 | PipeMux | SDK 模式迁移，管理命令 |

---

## 项目知识参考

> 详细扩展点已归档：[project-knowledge-details.md](../../archive/members/implementer/2026-01/project-knowledge-details.md)

### DocGraph

- **文档**：`atelia/docs/DocGraph/v0.1/` | **用法**：`USAGE.md`
- **关键扩展点**：Wish 布局迁移（v0.2）、TwoTierAggregator 基类、OutputPreflight 预检
- **详情**：见归档文件

### StateJournal

- **文档体系**：`mvp-design-v2.md`(语义) / `rbf-format.md`(Layer 0) / `rbf-interface.md`(契约)
- **关键术语**：RBF、FrameTag、VersionIndex、DiffPayload、Working/Committed State
- **条款**：72 条（F/A/S/R 四类）
- **详情**：见归档文件

### DocUI

| 组件 | 状态 |
|:-----|:-----|
| SegmentListBuilder / OverlayBuilder / StructList | ✅ 已实现 |
| UI-Anchor / AnchorTable | 📝 设计完成 |
| run_code_snippet / Micro-Wizard | ❌ 待实现 |

**MVP 分阶段**：MVP-0(静态) → MVP-1(功能) → MVP-2(交互)

### PipeMux

- **架构**：`PipeMuxApp` + `System.CommandLine`
- **管理命令**：`:` 前缀（`:status`, `:reload`）
- **应用**：texteditor, monitor

### Atelia.Data / SizedPtr

- **设计**：38:26 bit Fat Pointer，4B 对齐
- **关键**：`FromPacked()` 不校验；`Create()/TryCreate()` 完整校验
- **测试**：50 个测试（roundtrip/对齐/边界/Contains）
- **详情**：见归档文件

### Atelia.Primitives

```
AteliaError (abstract record)
├── AteliaResult<T>      ← ref struct，同步层
├── AsyncAteliaResult<T> ← readonly struct，异步层
└── AteliaException      ← 异常桥接
```

- **关键**：`IsSuccess` 从 `_error is null` 推导；允许 `Success(null)`
- **测试**：39 个用例

---

## 认知文件结构

```
agent-team/members/implementer/
├── index.md                  ← 主记忆（本文件）
├── inbox.md                  ← 便签收集箱
└── maintenance-log.md        ← 维护日志

agent-team/archive/members/implementer/
├── 2025-12/                  ← 归档详细记录
│   ├── statejournal-impl-details.md
│   ├── primitives-and-tools-log.md
│   └── pipemux-docui-log.md
└── 2026-01/                  ← 本次维护归档
    └── project-knowledge-details.md
```

---

## 最后更新

> 详细历史见 `archive/members/implementer/`

- **2026-01-25**: Stage 06 完成（Task 6.1-6.8），171 测试通过；Stage 06 审阅要点归档 [I-IMP-37]
- **2026-01-24**: RollingCrc BackwardScanner 语义澄清（正逆对称性、测试陷阱）；ScanReverse 实现决策（6 项 + ref struct 资源管理模式）
- **2026-01-24**: RBF v0.40 格式变更认知更新（TrailerCodeword 16B 固定布局、双 CRC、FrameDescriptor 位字段）
- **2026-01-17**: RBF Stage 05 完成（ValidateAndParse/ReadRaw/ReadFrameInto/ReadPooledFrame），156 测试；LLM 完工标准差距分析（6 维度）
- **2026-01-16**: RbfPooledFrame Owner Token 模式实现；DisposableAteliaResult 21 测试；SizedPtr 公开 API 改 long/int
- **2026-01-14**: RBF 测试架构重构（Facade/RawOps 分离）；DesignDsl Parser MVP 完成（INodeBuilder 框架 + Term/Clause 节点），67 测试
- **2026-01-11**: 记忆维护完成（530→262行，-51%）；洞见合并（39→26条）；180+ 行项目表格外迁归档
- **2026-01-11**: Atelia.Data 测试治理（Theory 化 + TheoryData 工厂 + 双接口模式）
- **2026-01-09**: DSL 迁移扩展点 + 纯引用模式
