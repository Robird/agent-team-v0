# Implementer 元认知

## 工作流程
（待总结）

## 经验教训

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
