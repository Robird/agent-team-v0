# Docops 认知索引

> 最后更新: 2025-12-21

## 我是谁
DocOps - 文档与索引管理专家，负责维护团队的集体记忆和认知连续性。

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [x] PipeMux - 2025-12-09 更新管理命令文档
- [ ] atelia-copilot-chat
- [x] StateJournal - 2025-12-21 更名迁移索引更新

## 最近工作

### 2025-12-21 - DurableHeap → StateJournal 更名迁移
- **任务**: 响应项目更名通知，更新所有文档索引中的 DurableHeap 引用
- **变更摘要**:
  - 旧路径：`DurableHeap/docs/` ❌ 已删除
  - 新路径：`atelia/docs/StateJournal/` ✅
  - 命名空间：`Atelia.StateJournal`
- **更新文件**:
  - `agent-team/lead-metacognition.md`: 项目 Backlog 表格路径更新
  - `.github/agents/docui-standards-chair.agent.md`: 任务类型加载文件表格更新
  - `agent-team/members/gemini-advisor/index.md`: 项目列表更新
  - `agent-team/wiki/DurableHeap/` → `agent-team/wiki/StateJournal/`: 目录重命名
  - `agent-team/wiki/StateJournal/concept.md`: 添加更名通知 banner
  - `agent-team/wiki/StateJournal/jam-brief-1.md`: 添加更名通知 banner
- **保留历史记录的文件**（不修改，作为历史事实）:
  - `agent-team/members/implementer/index.md`: 历史工作日志
  - `agent-team/archive/state-journal-mvp-design-v2.before-rename.md`: 更名前快照
- **状态**: ✅ 完成

### 2025-12-09 - PipeMux 管理命令文档更新
- **任务**: 更新 PipeMux 管理命令文档
- **更新文件**:
  - `PipeMux/docs/README.md`: 添加管理命令章节（`:list`, `:ps`, `:stop`, `:help`）
  - `agent-team/wiki/PipeMux/README.md`: 添加管理命令章节，更新已知问题表格标记 P1 任务已完成
- **状态**: ✅ 完成
