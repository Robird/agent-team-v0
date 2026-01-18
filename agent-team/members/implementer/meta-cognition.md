# Implementer 元认知

## 工作模式偏好

### 实现风格
- **直译优先**：尽量对齐源码的设计和实现
- **测试先行**：复杂功能先写测试用例确认边界
- **批量操作**：用 grep 确认范围，再 multi_replace 批量替换

### 工具使用习惯
- 用 `grep_search` 搜索条款 ID、术语出现位置
- 用 `read_file` 读取完整上下文（优先大块读取）
- 用 `multi_replace_string_in_file` 批量替换，减少工具调用次数

## 经验教训

### 2025-12-23: 记忆积累机制反思

**问题诊断**：
- index.md 膨胀到 1877 行，主要是任务执行细节的 append-only 日志
- 系统提示词只说"记录本次工作"，没有区分 append/overwrite
- meta-cognition.md 几乎为空（因为提示词没提它）

**改进方向**：
- 详情写 handoff，index.md 只放状态和索引
- 明确 OVERWRITE 触发条件：项目状态变更、当前任务更新
- index.md 预算：200-300 行上限
- OnSessionEnd 流程：60 秒内完成

**理想的 index.md 结构**：
1. 我是谁（2-3 句话）
2. 当前关注项目表格（状态 + 最后更新）
3. 近期交付物索引（链接到 handoff）
4. 可复用洞见（≤10 条，外链 wiki）

### 2025-12-21: DurableHeap → StateJournal 更名

**事件**：项目 DurableHeap 更名为 StateJournal 并迁入 atelia repo

**影响**：
- 旧路径：`DurableHeap/docs/` ❌ 已删除
- 新路径：`atelia/docs/StateJournal/` ✅
- 命名空间：`Atelia.StateJournal`
- 未来代码位置：`atelia/src/StateJournal/`

**命名由来**（全票通过）：
- "State" = Agent 状态持久化用例
- "Journal" = 追加写入 + 版本可回溯

**教训**：
- 项目更名时需检查所有认知文件中的引用
- 历史记录中的旧名称可保留（作为事实描述），但路径引用需更新

## 协作模式

### 与 Investigator
- 收到 Brief 后，先验证所有引用路径是否存在
- 对 Brief 中的"待确认"项主动求证

### 与 QA
- Handoff 中明确"需要 QA 关注的点"
- 测试结果包含 targeted + full 两种

## 战术层协作模式总结（2025-12-26）

### 批量任务执行策略

**任务分解原则**：
1. 识别依赖关系，按依赖顺序执行
2. 独立任务可并行委派给 Implementer subagent
3. 复杂任务（>30min 预估）单独委派
4. 简单任务（枚举/错误类型）Team Leader 直接完成

**委派 Prompt 结构**（实战优化版）：
```markdown
# 任务: <任务ID> <任务名>

## 背景
<前序任务完成情况>

## 规范参考
<条款引用、数据结构定义>

## 输出文件
<明确的路径列表>

## 实现要求
<API 签名、关键逻辑伪代码>

## 测试用例
<具体测试场景代码>

## 验收标准
<可检验的完成条件>
```

**效率递增原因分析**：
- Phase 1-2：规范理解 + 项目骨架建立（overhead 高）
- Phase 3-4：模式成熟，复制调整即可
- Phase 5：委派 prompt 高度结构化，subagent 几乎无需澄清

### 规范到实现的映射技巧

1. **条款 ID 作为锚点**：每个测试类/方法标注对应的条款 ID
2. **枚举值速查表 → 常量类**：直接翻译，保持 1:1 对应
3. **ASCII art → 代码注释**：复杂数据结构的二进制布局保留为注释
4. **"MUST"/"SHOULD" → 断言/警告**：MUST 用 throw，SHOULD 用 Debug.Assert

### 效率递增机制分析（2025-12-26 StateJournal MVP 总结）

战略层记录了效率从 3x → 24x 的递增，根本原因：

1. **规范-实现映射的认知负担逐步降低**
   - Phase 1-2：理解 RBF 帧格式、VarInt 编码等"基础设施"
   - Phase 3+：在已有基础上构建，只需理解增量概念
   - 类比：学会字母表后，拼写单词就快了

2. **委派 Prompt 模板迭代成熟**
   - 早期：需要猜测 Implementer 需要什么信息
   - 后期：形成固定模板（背景→规范→文件→实现→测试→验收）
   - 这本身就是一种"元学习"

3. **项目骨架建立后的复利效应**
   - csproj 结构、命名空间、测试框架只需建立一次
   - 后续任务可以直接"模式匹配"已有代码风格

### 战术层自主决策空间

批量任务模式的成功，关键在于战术层有足够的自主决策空间：

- **分解权**：收到"完成 Phase N"后，自行决定拆分粒度
- **调度权**：决定哪些任务自己做、哪些委派
- **变通权**：遇到规范模糊点时，做合理假设并记录

**反模式**：如果战略层事无巨细地规定每个 subagent 调用，反而会降低效率。

### 便签机制的价值

便签（inbox.md）在 StateJournal MVP 实践中起到了意想不到的作用：

1. **即时记录**：执行中遇到的陷阱、决策点、API 细节
2. **知识复用**：后续任务可以参考前序便签避坑
3. **团队学习**：MemoryPalaceKeeper 归档后成为团队知识

**建议**：每个 subagent 任务结束时都应该产出 1-2 条便签。

### subagent 反馈质量评估

**高质量反馈特征**：
- 明确报告新增/修改的文件列表
- 包含 targeted + full 测试结果
- 指出需要 QA 关注的边界情况
- 主动记录便签（洞见、陷阱、经验）

**需要重试的信号**：
- 测试失败但未说明原因
- 文件路径不存在
- 遗漏验收标准中的某项

## RBF 实现经验详情（2025-12-25）

### StatusLen 边界确定——根因澄清

**问题本质**：HeadLen/TailLen 记录的是 FrameBytes 总长度，而非 PayloadLen。由于 StatusLen 公式基于取模运算，给定 `(PayloadLen + StatusLen)` 的和，无法反推出具体的 PayloadLen——**丢失了低 2 位信息**。

**这不是规范 bug**，而是 wire format 设计的选择：
- 从 PayloadLen → StatusLen 是确定性映射（无歧义）
- 但从 HeadLen 反推 PayloadLen 需要额外信息

**候选改进方案**（已提交技术报告）：
- 方案 A：HeadLen/TailLen 改为记录纯 PayloadLen
- 方案 B：FrameStatus 值编码 StatusLen（0x01~0x04 = Valid + StatusLen）— **推荐**
- 方案 C：StatusLen 固定为 4
- 方案 D：保持现状（CRC 消歧）

技术报告：`agent-team/meeting/2025-12-25-rbf-statuslen-ambiguity.md`

### T-P1-05 IRbfScanner/逆向扫描 实现要点

1. **PayloadLen 边界确定**：当前采用枚举 + CRC 消歧策略。从 StatusLen=4 开始尝试，验证 FrameStatus 值和字节一致性，最终依赖 CRC 验证。

2. **测试设计要点**：
   - 使用非零 Payload 内容避免歧义（如 `0x10, 0x11, ...`）
   - 测试 FrameStatus 非法值时，确保所有可能的 StatusLen 解释都失败（如全填 0x77）

3. **Span 与 yield 不兼容**：`ReadOnlySpan<T>` 无法跨越 `yield return` 边界（编译器限制）。解决方案是使用 `List<T>` 收集结果后返回。

### T-P1-04 IRbfFramer/Builder 实现要点

1. **ref struct + lambda 约束**：`RbfFrameBuilder` 作为 `ref struct` 无法在 lambda/匿名方法中使用。测试异常抛出时需改用 try-catch 或 `using var` + `Assert.Throws(() => otherMethod())`。

2. **CRC 覆盖范围精确实现**：`[F-CRC32C-COVERAGE]` 要求 CRC 覆盖 FrameTag + Payload + FrameStatus + TailLen。在写入时需要先写完 TailLen，再计算 span 切片的 CRC。实现中用 `span.Slice(4, crcLen)` 准确覆盖从 FrameTag 开始的范围。

3. **Payload 缓冲设计**：`RbfFrameBuilder` 需要支持流式写入 Payload。使用内部 `PayloadBufferWriter` 类累积 Payload 数据，在 Commit 时一次性写入完整帧。

4. **Auto-Abort 语义**：未 Commit 就 Dispose 时写入 Tombstone (0xFF)。测试验证了 Tombstone 帧仍通过 CRC 校验，符合 `[S-RBF-BUILDER-AUTO-ABORT]` 规范。

5. **HeaderFence 可选**：构造函数添加 `writeHeaderFence` 参数，支持追加写入场景（不重复写 HeaderFence）。

### ASCII Art 修订规范合规（spec-conventions v0.3）

1. **VarInt 图**：保留教学性 ASCII art 时，只需在上方添加 `(Informative / Illustration)` 标注，并确保上方有文字/公式作为 SSOT。这是 `[S-DOC-ASCIIART-SHOULDNOT]` 的合规方式。

2. **FrameTag 位布局 → Visual Table**：将 box-drawing ASCII 框图改为 Markdown 表格时，关键是保留所有语义信息（位范围、字段名、类型、端序）。用 blockquote 补充端序说明和计算公式。

3. **Two-Phase Commit → Mermaid sequenceDiagram**：时序图改造时注意：
   - 用 `participant` 定义所有参与者（Heap、Dict、Index、DataFile、MetaFile）
   - 用 `Note over` 标注阶段分隔
   - 用 `loop` 表示循环（for each dirty object）
   - 箭头语义：`->>` 表示调用，`Note right of` 表示状态说明

4. **语义核对**：每次修改前需对照正文验证图表语义。
