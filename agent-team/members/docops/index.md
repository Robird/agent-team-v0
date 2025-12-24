# Docops 认知索引

> 最后更新: 2025-12-24

## 我是谁
DocOps - 文档与索引管理专家，负责维护团队的集体记忆和认知连续性。

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [x] PipeMux - 2025-12-09 更新管理命令文档
- [ ] atelia-copilot-chat
- [x] StateJournal - 2025-12-21 更名迁移索引更新

## 最近工作

### 2025-12-24 - StateJournal mvp-design-v2.md 文档冗余清理
- **任务**: 移除冗余内容，精简文档
- **修改文件**: `atelia/docs/StateJournal/mvp-design-v2.md`
- **变更内容**:
  - Section 2（设计决策）：原有摘要表格已移至外部决策记录，简化为单句指针
  - Section 3.1.0 末尾：Identity Map/Dirty Set 定义与术语表重复，已删除
- **效果**: 文档体积从 1416 行减少到 1399 行（-17 行）
- **状态**: ✅ 完成

### 2025-12-24 - StateJournal mvp-design-v2.md 冗余枚举清理
- **任务**: 移除重复的枚举列表，统一引用术语表中的枚举值速查表
- **修改文件**: `atelia/docs/StateJournal/mvp-design-v2.md`
- **变更内容**:
  - §3.2.5 ObjectVersionRecord：将详细的 ObjectKind 范围列表替换为引用，保留规范性标签
  - §3.4.2 Dict 的 DiffPayload：将 ValueType 枚举值列表替换为术语表引用
- **设计理由**: 枚举值已在 Glossary 的"枚举值速查表"中集中定义，避免多处重复维护
- **状态**: ✅ 完成

### 2025-12-23 - 记忆积累机制畅谈会（第三波）
- **任务**: 审阅 DocOps 记忆文件并参与第三波讨论
- **发言文件**: `agent-team/meeting/2025-12-22-memory-accumulation-reflection.md`
- **核心观点**:
  - 86 行精简是"设计好"的结果：index.md 只放工作日志，产物外置
  - 提出"三层分类"文档规范：索引层、摘要层、详情层
  - 建议采用"changefeed anchor"作为记忆文件的索引机制
  - 补充"链接检验"视角：过期链接是记忆腐败的主要症状
- **状态**: ✅ 完成

### 2025-12-21 - StateJournal mvp-design-v2.md §3.4.8 引用更新
- **任务**: 将 Error Affordance 部分改为引用全项目规范
- **修改文件**: `atelia/docs/StateJournal/mvp-design-v2.md`
- **变更内容**:
  - 添加规范提升通知 banner，引用 `AteliaResult-Specification.md`
  - 添加条款映射表，说明原 StateJournal 本地条款与新全项目条款的对应关系
  - 更新 ErrorCode 格式：从 `SCREAMING_SNAKE_CASE` 改为 `StateJournal.{ErrorName}` 格式
  - 保留 StateJournal 特有的 ErrorCode 注册表
  - 保留异常示例（更新 ErrorCode 格式）
- **删除的重复内容**:
  - 结构化错误字段定义（已在全项目规范中定义）
  - 本地条款定义 `[A-ERROR-CODE-MUST]` 等（改为引用）
- **状态**: ✅ 完成

### 2025-12-21 - AteliaResult 全项目规范文档
- **任务**: 创建 `atelia/docs/AteliaResult-Specification.md`
- **来源**: LoadObject 命名与返回值设计畅谈会共识
- **文档内容**:
  - §1 概述：定位、设计目标、核心洞察
  - §2 规范语言（RFC 2119）
  - §3 类型定义：`AteliaResult<T>`、`AteliaError`、`AteliaException`、`IAteliaHasError`
  - §4 规范条款（从 StateJournal 提升为全项目范围）
  - §5 使用规范：Result vs 异常选择、派生类模式、JSON 序列化
  - §6 与 StateJournal 规范的关系
  - §7 代码位置
- **定义的条款**:
  - [ATELIA-ERROR-CODE-MUST]
  - [ATELIA-ERROR-MESSAGE-MUST]
  - [ATELIA-ERROR-RECOVERY-HINT-SHOULD]
  - [ATELIA-ERROR-DETAILS-MAY]
  - [ATELIA-ERROR-CAUSE-MAY]
  - [ATELIA-ERRORCODE-NAMING]
  - [ATELIA-ERRORCODE-REGISTRY]
- **约束**:
  - Cause 链最多 5 层
  - Details 最多 20 个 key
  - ErrorCode 格式：`{Component}.{ErrorName}`
- **状态**: ✅ 完成

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
