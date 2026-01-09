# Design-DSL 自动化条款检查

> **版本**: 0.1  
> **创建**: 2026-01-09  
> **状态**: 技术储备/待条件成熟  
> **灵感来源**: RBF 文档一致性修复中发现的职能重叠/冲突检测需求

---

## 1. 问题背景

在 RBF 文档 DSL 迁移和一致性修复过程中（2026-01-09），发现了两类典型问题：

1. **职能重复**：interface/format 层的条款与 decisions 层语义重叠
2. **依赖冲突**：不同层级的条款约束可能存在隐含冲突

当前解决方案：**人工审阅**（Investigator 调查 + Craftsman 审阅 + QA 验证）

**痛点**：
- 人工审阅依赖专家经验，容易遗漏
- 跨文件的语义重叠不易发现
- 随着条款数量增长，全量检查成本上升

**机会**：AI-Design-DSL 的形式化语法为自动化检查创造了基础。

---

## 2. 技术愿景

**目标**：构建 Design-DSL 自动化条款检查工具，能够：

- ✅ 自动发现职能重叠的条款（候选列表）
- ✅ 识别跨层依赖冲突（如上层 MUST NOT throw，下层 MAY throw）
- ✅ 验证单向依赖原则（decisions ← interface/format，无反向引用）
- ✅ 生成审阅报告（供人工确认）

**非目标**（MVP 阶段）：
- ❌ 完全自动修复（仍需人工判断）
- ❌ 理解自然语言语义（聚焦语法和模式匹配）

---

## 3. 技术路线

### 路线 A：向量数据库 + 语义嵌入模型

**核心思路**：将每个条款 embedding 化，通过向量相似度检测语义重叠。

```
┌─────────────────────────────────────────────────────────────┐
│  DSL Parser                                                  │
│  解析 *.md → 条款列表 (ID, text, layer, dependencies)        │
└────────────────────┬────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  Embedding Model (e.g., text-embedding-3-small)              │
│  text → vector (1536-dim)                                    │
└────────────────────┬────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  Vector Database (e.g., Qdrant, Milvus, in-memory FAISS)    │
│  存储：{clause_id, vector, metadata}                         │
└────────────────────┬────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  Similarity Search                                           │
│  对每个条款，查找 top-K 最相似条款（cosine > threshold）      │
└────────────────────┬────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  Candidate Report                                            │
│  {clause_A, clause_B, similarity_score, layer_info}          │
└─────────────────────────────────────────────────────────────┘
```

**优势**：
- 语义理解能力强（能发现"用词不同但意思相同"的重叠）
- 可扩展性好（数百条款性能仍可接受）
- 可调节阈值（precision/recall trade-off）

**挑战**：
- 需要引入 embedding 模型（API 调用或本地部署）
- 向量数据库选型与集成
- 阈值调优（避免过多假阳性）

**技术栈候选**：
- Embedding: OpenAI `text-embedding-3-small` / Sentence-Transformers
- Vector DB: Qdrant (自托管) / FAISS (轻量) / Milvus (重型)
- Language: Python (生态成熟)

---

### 路线 B：LLM + 决策层常驻上下文 + 批次执行

**核心思路**：以决策层文件作为系统提示词，逐条询问其他条款是否有职能重叠/冲突。

```
┌─────────────────────────────────────────────────────────────┐
│  DSL Parser                                                  │
│  解析 decisions.md → 决策层条款（作为 SSOT 基准）            │
└────────────────────┬────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  Prompt Constructor                                          │
│  System: "你是设计审阅专家，decisions.md 是 SSOT..."         │
│  User: "interface.md 的 @[CLAUSE-X] 是否与 decisions 重叠？" │
└────────────────────┬────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  LLM Batch API (e.g., OpenAI Batch / Anthropic)             │
│  批次提交 N 个条款检查请求                                   │
└────────────────────┬────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  Response Aggregator                                         │
│  解析 LLM 返回 → {clause_id, has_overlap, reason, severity} │
└────────────────────┬────────────────────────────────────────┘
                     ▼
┌─────────────────────────────────────────────────────────────┐
│  Candidate Report                                            │
│  按 severity 排序，生成人工审阅清单                          │
└─────────────────────────────────────────────────────────────┘
```

**优势**：
- 语义理解能力最强（能理解复杂的逻辑关系）
- 无需训练/调优（利用 LLM 预训练能力）
- 可提供解释（why overlap / why conflict）

**挑战**：
- 成本较高（N 条款 × API 调用）
- 需要提示词工程（避免假阳性/假阴性）
- 批次执行的延迟（Batch API 通常有小时级延迟）

**技术栈候选**：
- LLM: Claude Opus 4.5 (batch) / GPT-4 (batch) / 本地 LLM (Llama 3)
- Batch API: Anthropic Message Batches / OpenAI Batch API
- Language: Python (调用 API) / .NET (与 Atelia 技术栈一致)

---

## 4. 混合方案（推荐）

结合两种路线的优势：

```
Phase 1: 快速过滤（向量相似度）
  → 从 N 条款中找出 top-20 可疑候选（高相似度）

Phase 2: 深度分析（LLM）
  → 对 20 个候选进行批次 LLM 检查
  → 生成详细原因和修复建议

Phase 3: 人工确认
  → Craftsman 审阅 LLM 报告
  → 决策是否修复
```

**收益**：
- 降低 LLM 调用成本（只检查高相似度候选）
- 保持高召回率（向量搜索不易遗漏）
- 保持高精确度（LLM 深度分析避免假阳性）

---

## 5. 实施路线图

### Phase 0: 基础设施（约 2-3 天）

- [ ] DSL Parser v1（解析 decision/design/hint/term）
- [ ] 条款数据结构定义（Clause, Layer, Dependency）
- [ ] 文件扫描器（递归读取 `atelia/docs/**/*.md`）

### Phase 1: MVP - 向量相似度检测（约 3-5 天）

- [ ] 集成 embedding 模型（优先 OpenAI API）
- [ ] 实现简单的 in-memory 向量检索（FAISS 或纯 NumPy）
- [ ] 生成候选报告（CSV 或 Markdown）
- [ ] 在 RBF 文档上验证（复现已知的 4 个问题）

### Phase 2: 增强 - LLM 深度分析（约 5-7 天）

- [ ] 设计 Batch API 提示词模板
- [ ] 实现批次提交/轮询/聚合流程
- [ ] 将 LLM 输出结构化（JSON schema）
- [ ] 生成增强版审阅报告（包含解释和建议）

### Phase 3: 产品化（约 7-10 天）

- [ ] CLI 工具封装（`dsl-check --target atelia/docs/Rbf`）
- [ ] 集成到 CI/CD（pre-commit hook / GitHub Action）
- [ ] 配置文件支持（阈值、模型选择、忽略规则）
- [ ] 用户文档（README + 使用示例）

**总计**：约 17-25 天（取决于优先级和资源）

---

## 6. 成功指标

### 技术指标

- **召回率** ≥ 90%（已知问题能发现）
- **精确度** ≥ 70%（报告的问题多数有效）
- **性能** < 30s（100 条款全量检查）

### 业务指标

- 减少人工审阅时间 50%+
- 在新文档迁移中提前发现问题（左移质量保障）
- 形成可复用的 Recipe（`how-to/run-dsl-check.md`）

---

## 7. 风险与缓解

| 风险 | 影响 | 概率 | 缓解措施 |
|:-----|:-----|:-----|:---------|
| Embedding 模型理解不准确 | 假阴性（漏检） | 中 | Phase 1 验证时用已知问题回归测试 |
| LLM 产生假阳性 | 浪费人工审阅时间 | 中 | 提示词工程 + 阈值调优 |
| 成本超出预算 | 项目搁置 | 低 | 优先用开源模型（Sentence-Transformers） |
| 集成复杂度高 | 延期交付 | 中 | Phase 1 先做独立 CLI，不强求集成 |

---

## 8. 依赖与前置条件

### 当前已具备

- ✅ AI-Design-DSL 规范 v0.1（`agent-team/wiki/SoftwareDesignModeling/AI-Design-DSL.md`）
- ✅ RBF 文档试点（55 条条款，已验证 DSL 可用性）
- ✅ 已知问题集（4 个真实案例作为测试集）

### 待具备

- [ ] DSL Parser 实现（需 Implementer 完成）
- [ ] 向量数据库选型决策（需监护人 + Craftsman 评估）
- [ ] LLM Batch API 账号与配额（需监护人提供）

---

## 9. 备选方案

如果向量/LLM 方案不可行，可降级为：

**方案 C：静态规则检查**
- 同名条款检测（跨文件的 `[CLAUSE-ID]` 重复）
- depends 依赖图验证（无环、单向）
- 层级约束检查（interface/format 不能定义 decision）

**收益**：简单可靠，0 外部依赖  
**局限**：无法发现"语义重叠但 ID 不同"的问题

---

## 10. 相关资源

### 参考实现

- [OpenAI Embeddings Guide](https://platform.openai.com/docs/guides/embeddings)
- [Qdrant Vector Database](https://qdrant.tech/)
- [FAISS (Facebook AI Similarity Search)](https://github.com/facebookresearch/faiss)
- [Sentence-Transformers](https://www.sbert.net/)

### 团队经验

- **Investigator**：擅长技术调研和方案评估
- **Implementer**：有 .NET 和 Python 双语言能力
- **Craftsman**：擅长工程视角的方案审阅

---

## 11. 下一步行动

**当前状态**：技术储备阶段，等待条件成熟

**触发条件**（满足任一即可启动）：
1. DSL 推广到 3+ 项目（有足够规模的条款库）
2. 人工审阅成本成为瓶颈（每次审阅 > 1 小时）
3. 监护人明确要求（战略优先级提升）

**启动前准备**：
- [ ] 监护人确认技术路线（A / B / 混合）
- [ ] 评估预算（API 成本 + 开发时间）
- [ ] 创建 Wish 实例（`wish/W-00XX-dsl-automated-checking/`）

---

> **备注**：本文档记录技术愿景和路线，不代表立即启动。待时机成熟时，可作为 Wish Resolve-Tier 的输入。
