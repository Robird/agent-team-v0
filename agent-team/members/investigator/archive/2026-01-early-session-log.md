# Investigator Session Log Archive — 2026-01 早期

> **归档日期**: 2026-01-24
> **覆盖范围**: 2026-01-01 ~ 2026-01-12
> **归档原因**: Session Log 已沉淀为 handoff/wiki，压缩至归档释放 index.md 空间

---

## 2026-01-12: AI-Design-DSL/DesignDsl 导航锚点汇总
**类型**: Route + Anchor + Signal
**项目**: DesignDsl, Atelia

### 1. AI-Design-DSL 规范导航（Route）
| 意图 | 位置 | 关键锚点 |
|:-----|:-----|:---------|
| 术语定义语法 | [AI-Design-DSL.md](agent-team/wiki/SoftwareDesignModeling/AI-Design-DSL.md) | `[F-TERM-DEFINITION-FORMAT]`, `` term `ATX-Tree` `` |
| 条款定义语法 | 同上 | `[F-CLAUSE-DEFINITION-FORMAT]` |
| Identifier 格式 | 同上 | `[S-IDENTIFIER-ALNUM-HYPHENATED]` |

### 2. DocGraph 现有代码位置（Anchor）
| 意图 | 位置 |
|:-----|:-----|
| DocGraph Markdown 处理 | [DocumentGraphBuilder.cs](atelia/src/DocGraph/Core/DocumentGraphBuilder.cs) |

**备注**: DocGraph 当前只使用 FrontmatterParser 处理 YAML front-matter，不解析 Markdown 内容。DesignDsl 将填补这个空白。

### 3. DSL 关键字快速识别（Signal）
| 特征 | 含义 |
|:-----|:-----|
| Heading 以 `term`/`decision`/`spec`/`derived` 开头 | DSL 节点 |
| 反引号包裹 ID | Term-Node |
| 方括号包裹 ID | Clause-Node |

---

## 2026-01-12: RBF 文档版本/条款 ID 导航锚点汇总
**类型**: Route + Signal + Gotcha
**项目**: RBF

### 1. RBF 文档版本信息速查（Route）
| 文档 | 版本号位置 |
|:-----|:-----------|
| interface | `## 6. 最近变更` 表格第一行 |
| format | `## 10. 变更日志` 表格第一行 |
| test-vectors | `## 变更日志` 表格第一行 |
| decisions | 无版本号（AI-Design-DSL 保护的根文档）|

### 2. 条款 ID 引用分析速查（Route）
| 资源 | 位置 |
|:-----|:-----|
| 完整清单 | `agent-team/handoffs/rbf-clause-id-inventory.csv` |
| 影响分析 | `agent-team/handoffs/rbf-rename-impact-analysis.md` |
| 搜索命令 | `grep -rn "CLAUSE-ID" --include="*.md" --include="*.cs" atelia/ agent-team/` |

**高引用 TOP5**: R-REVERSE-SCAN-ALGORITHM(47), F-HEADLEN-FORMULA(47), F-FRAMESTATUS-VALUES(41), F-FRAME-LAYOUT(35), F-STATUSLEN-FORMULA(34)

### 3. Gotcha
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 版本声明与实际版本的"假漂移" | 看起来漂移 4-6 个版本，实际都是格式/表述优化，语义无变更 | 先看变更日志判断语义是否变化 |
| 条款 ID 改名时 archive/ 可跳过 | 若全量替换会修改历史快照 | 改名脚本应排除 `archive/` 目录 |

---

## 2026-01-12: spec-conventions 条款 ID 改名影响分析
**类型**: Route + Signal
**项目**: spec-conventions

| 指标 | 数值 |
|:-----|:-----|
| 涉及条款 | 8 个条款 ID 改名 |
| 核心更新文件 | 4 个（spec-conventions.md + 2 meeting + 1 wiki）|
| 总更新点 | 15 处 |
| 高影响条款 | `S-DOC-FORMAT-MINIMALISM`（6 处非 SSOT 引用）|

**详细报告**: [spec-conventions-rename-impact.md](../handoffs/spec-conventions-rename-impact.md)

---

## 2026-01-11: Atelia.Data 测试架构治理 + 代码去重分析
**类型**: Route + Anchor + Gotcha
**项目**: Atelia.Data

### 1. 测试泛化陷阱（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 测试断言 `IsPassthrough` 或中间状态 `inner.Data()` | 泛化到 `IReservableBufferWriter` 接口后 `SinkReservableWriter` 测试失败 | 只验证最终结果 |

### 2. 接口契约 vs 实现细节判定锚点
| 问题 | 答案 |
|:----|:----|
| 最终数据顺序 | ✅ 接口契约 |
| Flush 时机 | ❌ 实现细节 |
| 中间状态可观察性 | ❌ 实现细节 |
| Reservation 阻塞语义 | ⚠️ 契约推论 |

**位置**: [IReservableBufferWriter.cs](atelia/src/Data/IReservableBufferWriter.cs) 接口注释

### 3. 关键 Gotcha
- **BitOperations.RoundUpToPowerOf2 溢出**: x > 1GB 时返回负数 — 仅对 `candidate ≤ (1 << 30)` 调用
- **可变 struct + 引用字段**: 复制后值字段独立、引用字段共享 — 改为 `sealed class`

### 4. 详细分析报告索引
| 主题 | 位置 |
|:-----|:-----|
| 测试架构治理 | [2026-01-11-test-architecture-governance-analysis.md](agent-team/handoffs/2026-01-11-test-architecture-governance-analysis.md) |
| 代码去重 | [2026-01-11-deduplication-analysis.md](agent-team/handoffs/2026-01-11-deduplication-analysis.md) |
| Chunk 统一 | [2026-01-11-unified-chunk-analysis.md](agent-team/handoffs/2026-01-11-unified-chunk-analysis.md) |
| 溢出修复 | [2026-01-11-p0-overflow-fix-analysis.md](agent-team/handoffs/2026-01-11-p0-overflow-fix-analysis.md) |
| struct 重构 | [2026-01-11-p1-mutable-struct-analysis.md](agent-team/handoffs/2026-01-11-p1-mutable-struct-analysis.md) |
| 适配器设计 | [2026-01-11-randomaccess-bytesink-design.md](agent-team/handoffs/2026-01-11-randomaccess-bytesink-design.md) |

---

## 2026-01-09: RBF/AI-Design-DSL 验证锚点汇总
**类型**: Route + Anchor
**项目**: RBF, AI-Design-DSL

| 锚点 | 验证命令 | 状态 |
|:-----|:---------|:-----|
| SizedPtr 迁移完整性 | `grep -rn "deleted-place-holder" atelia/docs/Rbf/` | ✅ 0 匹配 |
| rbf-decisions.md 条款 | 9 条（6 Decision + 3 Format） | ✅ 验证过 |
| AI-Design-DSL 跨文档引用 | `grep -rn "S-RBF-DECISION-WRITEPATH\|S-RBF-DECISION-4B-ALIGNMENT" atelia/docs/Rbf/` | ✅ 验证过 |

### Gotcha
- **AI-Design-DSL 迁移 Heading 锚点会变化**: DSL 化后 `#s-rbf-decision-xxx` → `#decision-s-rbf-decision-xxx-中文title`
- **SizedPtr 范围估算陷阱**: 38-bit offset 因 4B 对齐实际为 ~1TB，非 ~256GB

---

## 2026-01-09: atelia-copilot-chat Fork 现状调查
**任务**: 调研 Fork 版本差距、核心修改点、扩展机制

### Fork 版本状态
| 指标 | 数值 |
|:-----|:-----|
| 版本差距 | 6 commits ahead, 64 commits behind |
| 我们的改动 | 8 files, +1083/-5 lines |
| 新增功能 | `summarizedConversationHistory.tsx` (966 lines) — half-context 摘要 |

### 核心修改点锚点
- [copilotIdentity.tsx](atelia-copilot-chat/src/extension/prompts/node/base/copilotIdentity.tsx) — 身份定义
- [safetyRules.tsx](atelia-copilot-chat/src/extension/prompts/node/base/safetyRules.tsx) — 安全规则
- [agentPrompt.tsx#L98](atelia-copilot-chat/src/extension/prompts/node/agent/agentPrompt.tsx#L98) — 角色描述

### runSubagent API 稳定性（Signal）
- 代码特征: `ToolName.CoreRunSubagent = 'runSubagent'` + `ToolCategory.Core`
- 历史波动: 曾被删除（#1819）后恢复 — 非核心架构，可能再次变动

---

## 2026-01-07: SizedPtr 迁移 Gotcha — 提交 942e1c0 是"倒退"
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 提交 942e1c0 "RBF文档修订"实际把 SizedPtr 改回 <deleted-place-holder> | 3 周工作被覆盖 | `git revert 942e1c0` |

---

## 2026-01-06: C# ref struct / Task 派生限制调查
### `allows ref struct` 限制（Gotcha）
| 问题 | 后果 | 规避 |
|:-----|:-----|:-----|
| 即使泛型参数声明 `allows ref struct`，也无法在 `Func<T, TResult>` 中使用 ref struct | `AteliaResult<T>.Map()` 无法支持 ref struct | 使用 `out` 参数模式 |
| `readonly struct` 不能声明 `allows ref struct` | `AsyncAteliaResult<T>` 无法包含 ref struct 值 | 异步场景下 ref struct 必须"物化" |

### 从 Task<T> 派生的根本限制
想通过 `AteliaTask<T> : Task<T>` 避免双重包装 → 无法工作——`TrySetResult`/`TrySetException`/promise 构造函数全是 `internal`

---

## 2026-01-05: DocGraph 代码调查
**任务**: Wish-0007 相关的 DocGraph 源码调查

### Visitor 扩展机制
| 类型 | 位置 | 备注 |
|:-----|:-----|:-----|
| **扩展入口** | `RunCommand.cs#L95-L101` | `GetVisitors()` 硬编码列表 |
| **frontmatter 字段** | `GlossaryVisitor.cs#L93-L103` | `KnownFrontmatterFields` 静态类 |
| **Wish 归属推导** | `DocumentNode.ProducedBy` | 比路径正则更健壮 |

### Gotcha 陷阱
| 陷阱 | 后果 | 规避 |
|:-----|:-----|:-----|
| IssueAggregator 已存在 | 重复造轮子 | W-0007 应改为"扩展"而非"新建" |
| produce 声明 vs Visitor 输出路径不一致 | fix 阶段用空模板覆盖手动文件 | produce 只声明 `.gen.md` 路径 |

---

## 2026-01-05: SizedPtr/<deleted-place-holder> 替代性分析（续）
**核心结论**：所有 9 处使用都是"定位 Frame"用途，SizedPtr 可完全替代

---

## 2026-01-04: SizedPtr/RBF/<deleted-place-holder> 现状调查
### <deleted-place-holder> 权威定义位置
- 位置: `atelia/docs/Rbf/rbf-interface.md#2.3`
- 条款: `[F-<deleted-place-holder>-DEFINITION]`, `[F-<deleted-place-holder>-ALIGNMENT]`, `[F-<deleted-place-holder>-NULL]`
- 源码实现已归档: `atelia/archive/2025-12-29-rbf-statejournal-v1/Rbf/<deleted-place-holder>.cs`

### Null 语义冲突（Gotcha）
Wish-0004 非目标写"不定义特殊值"，但 <deleted-place-holder> 定义了 `Null=0` → SizedPtr 保持纯净，Null 由 RBF 层自行包装

---

## 2026-01-01: workspace_info 机制调查
**任务**: 分析 VS Code Copilot Chat 中 workspace_info 的生成机制

### 关键发现
1. **组成结构**：`workspace_info` 是 `GlobalAgentContext` 的子组件，包含 Tasks、FoldersHint、WorkspaceStructure 三部分
2. **深度控制**：无显式深度限制，由 `maxSize=2000` 字符预算和 BFS 算法共同决定
3. **生成算法**：`visualFileTree.ts` 实现广度优先展开，空间不足时添加 `...` 截断
4. **过滤机制**：遵循 `.gitignore`、Copilot Ignore、排除点文件（默认）
5. **缓存策略**：首轮渲染后缓存到 Turn Metadata，后续轮次复用

**交付**: [handoffs/2026-01-01-workspace-info-mechanism-INV.md](../handoffs/2026-01-01-workspace-info-mechanism-INV.md)
