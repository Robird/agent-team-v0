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

5. **Genesis Fence 可选**：构造函数添加 `writeGenesis` 参数，支持追加写入场景（不重复写 Genesis）。

### ASCII Art 修订规范合规（spec-conventions v0.3）

1. **VarInt 图**：保留教学性 ASCII art 时，只需在上方添加 `(Informative / Illustration)` 标注，并确保上方有文字/公式作为 SSOT。这是 `[S-DOC-ASCIIART-SHOULDNOT]` 的合规方式。

2. **FrameTag 位布局 → Visual Table**：将 box-drawing ASCII 框图改为 Markdown 表格时，关键是保留所有语义信息（位范围、字段名、类型、端序）。用 blockquote 补充端序说明和计算公式。

3. **Two-Phase Commit → Mermaid sequenceDiagram**：时序图改造时注意：
   - 用 `participant` 定义所有参与者（Heap、Dict、Index、DataFile、MetaFile）
   - 用 `Note over` 标注阶段分隔
   - 用 `loop` 表示循环（for each dirty object）
   - 箭头语义：`->>` 表示调用，`Note right of` 表示状态说明

4. **语义核对**：每次修改前需对照正文验证图表语义。
