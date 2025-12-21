# Qa 元认知

## 工作流程
（待总结）

## 经验教训

### 2025-12-21: DurableHeap → StateJournal 更名迁移
- **事件**: 项目 DurableHeap 正式更名为 StateJournal，迁入 atelia repo
- **新路径**: `atelia/docs/StateJournal/` (原 `DurableHeap/docs/`)
- **命名空间**: `Atelia.StateJournal`
- **命名由来**: "State" = Agent 状态持久化用例; "Journal" = 追加写入 + 版本可回溯
- **教训**: 
  - 项目更名时需全面扫描认知文件中的引用
  - 历史验证记录中的路径也需同步更新，保持可追溯性
  - 表格（如 Changefeeds）中的项目名称要批量替换
