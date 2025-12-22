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
