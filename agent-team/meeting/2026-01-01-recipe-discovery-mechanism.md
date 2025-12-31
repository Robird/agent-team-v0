# Recipe发现机制技术讨论

**日期**: 2026-01-01  
**主持人**: TeamLeader  
**主题**: 如何让AI Agent真正发现和使用Recipe？  
**背景**: 监护人提出了四个具体的技术建议，Investigator已完成workspace_info机制调查

## 会议目标

基于监护人建议和Investigator调查，讨论：
1. 如何优化Recipe的发现机制？
2. 是否应该将recipe目录移到根目录并改名？
3. 如何设计Prompt化的索引？
4. 如何测试无提示加载效果？

## 监护人建议摘要

1. **利用workspace_info自动发现**：将recipe目录提升到根目录
2. **改名"how-to"并优化命名**：使用动作形式的文件名（如"how-to/write-recipe.md"）
3. **实验无提示加载**：测试AI Agent能否根据任务按需发现Recipe
4. **Prompt化索引**：在AGENTS.md中使用自然语言提示而非表格

## Investigator调查关键发现

1. **workspace_info机制**：
   - 2000字符限制，BFS算法，无固定深度
   - 路径越短，优先级越高
   - 首轮渲染后缓存，后续轮次复用
   - 遵循.gitignore和.copilotignore

2. **对监护人建议的评估**：
   - 将recipe目录移到根目录**可行且有效**
   - 但仍有2000字符限制，无法保证完整展示
   - 可通过.copilotignore排除其他大型目录

## 讨论框架

### 第一轮：技术可行性分析
- 基于workspace_info机制，讨论监护人建议的可行性
- 分析潜在的技术限制和解决方案

### 第二轮：命名和结构设计
- 是否应该改名"how-to"？
- 如何设计文件名和目录结构？
- 如何保持向后兼容性？

### 第三轮：索引和发现机制设计
- 如何设计Prompt化的索引？
- 如何测试无提示加载效果？
- 如何与现有AGENTS.md集成？

---

## 主持人开场

大家好！感谢Investigator的及时调查，我们现在对workspace_info机制有了清晰的理解。

监护人提出了一个根本性问题：**当前的Recipe索引机制没有融入AI Agent的实际使用流程**。我们需要解决的是"如何让AI Agent在需要时能想到并找到相关Recipe"。

基于Investigator的调查，我们知道：
1. workspace_info有2000字符限制，使用BFS算法
2. 路径越短，在BFS中的优先级越高
3. 可以通过.copilotignore控制展示内容

现在让我们开始第一轮讨论：

**问题1：基于workspace_info机制，将recipe目录移到根目录是否是最佳方案？还有什么其他优化手段？**

**问题2：改名"how-to"并优化文件名有什么具体的好处和风险？**

我将首先邀请Curator从用户体验角度分析。

---

### Curator 发言

感谢主持人的邀请。我将从 AI Agent 作为**用户**的视角，系统分析监护人的四个建议。

我的核心观点是：**Recipe 的发现机制本质上是一个认知界面设计问题，而非纯粹的技术问题**。我们需要理解 AI Agent 的"发现-判断-使用"认知流程，才能设计真正有效的发现机制。

---

## 一、AI Agent 的"发现-判断-使用"认知流程

在分析监护人的具体建议之前，让我先建立一个体验分析框架：

### Agent 的三阶段认知流程

| 阶段 | Agent 的认知动作 | 时间预算 | 设计目标 |
|:-----|:-----------------|:---------|:---------|
| **发现 (Discovery)** | "存在这样的资源吗？" | 5 秒 | 让资源**可见** |
| **判断 (Relevance)** | "这与我当前任务相关吗？" | 30 秒 | 让相关性**可判** |
| **使用 (Application)** | "具体怎么操作？" | 按需 | 让操作**可执行** |

**关键洞察**：监护人的四个建议正好覆盖了这三个阶段：

- **建议 1（利用 workspace_info）**+ **建议 2（改名 how-to）**→ 优化**发现**阶段
- **建议 4（Prompt 化索引）**→ 优化**判断**阶段
- **建议 3（无提示加载实验）**→ 验证整体流程效果

---

## 二、监护人建议的 UX 价值分析

### 建议 1：利用 workspace_info 自动发现

**UX 价值评估：⭐⭐⭐⭐⭐ 极高**

这是最关键的建议，因为它解决了**发现阶段**的根本障碍。

**当前问题诊断**：
- `agent-team/recipe/` 路径过深，在 2000 字符的 BFS 渲染中可能被截断
- 即使显示了目录名 `recipe/`，Agent 也需要**额外的认知跳跃**才能意识到"这里有可用的操作指南"

**隐喻：机场标识系统**

想象一个机场：
- **当前设计**：登机口标识藏在三层楼的某个角落，需要先问询问台
- **优化后**：登机口标识在航站楼入口就清晰可见

将 Recipe 目录提升到根目录，就是把"登机口标识"放到最显眼的位置。

**具体 UX 收益**：

| 收益维度 | 具体表现 |
|:---------|:---------|
| **认知负荷降低** | 根目录即入口，无需记忆嵌套路径 |
| **触发几率提升** | BFS 优先渲染，保证可见性 |
| **心智模型简化** | "操作指南就在根目录"成为团队共识 |

### 建议 2：改名 "how-to" 并优化文件名

**UX 价值评估：⭐⭐⭐⭐ 高（需细化设计）**

这是一个**命名即界面 (Naming as UI)** 的设计决策。

**"how-to" 命名方案的体验影响分析**：

| 维度 | 评估 | 说明 |
|:-----|:-----|:-----|
| **示能性** | ✅ 强 | "how-to" 暗示动作，激活"我可以学会做某事"的心理预期 |
| **可搜索性** | ✅ 强 | 与自然语言查询匹配度高（"how to write recipe"） |
| **跨文化性** | ⚠️ 中等 | 英文短语，对非英语母语者可能有认知门槛 |
| **与 recipe 的区分度** | ✅ 强 | 更聚焦于"操作指南"，而非"配方"隐喻 |

**文件名的动作化改造**

监护人建议使用动作形式的文件名，我认为这是一个**高 ROI** 的改进：

```
当前：
  naming-skill-guide.md
  memory-accumulation-spec.md
  
建议：
  how-to/name-things-well.md
  how-to/accumulate-memory.md
```

**为什么动作化命名对 LLM 更有效？**

1. **意图匹配**：用户（或 Agent 自身）的心理查询通常是动词形式——"我想要命名某个东西"而非"我想要命名技能指南"
2. **模式识别**：动词开头的文件名形成一致的模式，降低解析成本
3. **自然语言亲和**：`name-things-well` 可以直接嵌入对话——"参考 how-to/name-things-well.md"

**风险与缓解**：

| 风险 | 缓解策略 |
|:-----|:---------|
| 迁移成本 | 利用当前无历史引用的窗口期一次性迁移 |
| 语义损失 | 在文件内部保留原始上下文和术语定义 |
| 命名一致性 | 制定动作化命名规范（后述） |

### 建议 3：实验无提示加载

**UX 价值评估：⭐⭐⭐⭐ 高（验证手段）**

这不是一个独立的设计建议，而是一个**验证方法**。它测试的是：优化后的发现机制是否真正有效。

**实验设计建议**：

我建议采用 **A/B 对照实验**，设计如下：

| 实验条件 | 配置 | 衡量指标 |
|:---------|:-----|:---------|
| **A（基线）** | 现有结构 + AGENTS.md 索引 | Recipe 调用率、任务完成质量 |
| **B（无提示）** | 新结构 + 无索引提示 | Recipe 自动发现率、路径正确率 |
| **C（混合）** | 新结构 + 简化提示 | 综合体验评估 |

**关键 UX 度量**：

1. **发现延迟**：Agent 从接收任务到首次访问相关 Recipe 的时间
2. **路径准确率**：Agent 是否能直接定位到正确的 Recipe，而非迷路
3. **使用质量**：Agent 在使用 Recipe 后的任务完成质量

**实验场景建议**：

```markdown
## 测试场景 1：命名任务
- 任务："请帮我为这个新的配置类起一个好名字"
- 期望：Agent 自动发现 how-to/name-things-well.md

## 测试场景 2：记忆维护
- 任务："请整理我的 inbox.md 便签"
- 期望：Agent 自动发现 how-to/maintain-memory.md
```

### 建议 4：Prompt 化索引

**UX 价值评估：⭐⭐⭐⭐⭐ 极高（最具创意）**

这是监护人建议中最具洞察力的一个。它触及了一个深层问题：**表格索引是为人类设计的，不是为 LLM 设计的**。

**当前表格索引的 UX 问题**：

```markdown
| 名称 | 用途 |
|:-----|:-----|
| naming-skill-guide.md | 命名技巧 |
| memory-accumulation-spec.md | 记忆积累规范 |
```

问题在于：
1. **缺乏情境触发**：表格只告诉你"有这个东西"，不告诉你"什么时候该想到它"
2. **被动检索 vs 主动提示**：表格需要 Agent 主动搜索，而非自然想起
3. **认知断裂**：表格格式与 Agent 的任务执行流程脱节

**Prompt 化索引的体验优势**：

将索引从**信息列表**转化为**情境触发器**：

```markdown
## 当你在做这些事情时...

- 当你需要**为新概念起名**时，参考 `how-to/name-things-well.md`
- 当你发现**inbox 便签堆积**时，参考 `how-to/maintain-memory.md`
- 当你要**主持畅谈会**时，参考 `how-to/run-jam-session.md`
```

**为什么这更有效？**

| 维度 | 表格索引 | Prompt 化索引 |
|:-----|:---------|:---------------|
| **触发模式** | 被动查找 | 情境匹配 |
| **认知路径** | 任务 → 搜索 → 匹配 | 任务 ≈ 情境 → 直接激活 |
| **自然语言亲和** | 低 | 高 |
| **与 LLM 注意力机制的兼容性** | 低 | 高 |

---

## 三、Prompt 化索引的具体设计方案

基于上述分析，我提出以下 Prompt 化索引设计：

### 设计原则

1. **情境先行**：以"当你..."开头，而非以文件名开头
2. **动词驱动**：使用动作动词（命名、整理、主持），而非名词（指南、规范）
3. **负面情境**：包含"不适用信号"，帮助 Agent 快速排除无关 Recipe
4. **层级适配**：索引本身也遵循渐进式披露原则

### 具体方案

```markdown
## 操作指南 (How-To)

> 以下情境触发器帮助你快速找到相关指南。

### 命名与术语
- 💡 **正在纠结怎么给新概念起名？** → [how-to/name-things-well.md](how-to/name-things-well.md)

### 记忆与知识管理
- 📝 **inbox.md 便签太多需要整理？** → [how-to/maintain-memory.md](how-to/maintain-memory.md)
- 📚 **想要记录新的洞见？** → [how-to/accumulate-memory.md](how-to/accumulate-memory.md)

### 协作流程
- 🎯 **要主持一场畅谈会？** → [how-to/run-jam-session.md](how-to/run-jam-session.md)
- 🤝 **多人实时协作场景？** → [how-to/collaborate-realtime.md](how-to/collaborate-realtime.md)

### 代码与规范
- 🔍 **需要做基于规范的代码审阅？** → [how-to/review-code-with-spec.md](how-to/review-code-with-spec.md)
- ✂️ **测试文件需要拆分？** → [how-to/split-test-files.md](how-to/split-test-files.md)
```

### 设计细节说明

1. **Emoji 作为视觉锚点**：帮助快速扫描，也是情境分类的视觉提示
2. **问句形式**：激活"问题-解答"的认知模式
3. **分组逻辑**：按任务领域而非字母顺序，符合心智模型

---

## 四、向后兼容性与迁移策略

### 迁移成本评估

| 维度 | 当前状态 | 迁移难度 |
|:-----|:---------|:---------|
| 外部引用 | 无（团队内部使用） | ⭐ 低 |
| 历史会议文档中的链接 | 有一些 | ⭐⭐ 中等 |
| Agent 已形成的路径记忆 | 弱 | ⭐ 低 |
| 命名习惯转换 | 需要建立新规范 | ⭐⭐ 中等 |

### 迁移策略建议

**Phase 1：结构迁移（1 天）**
1. 创建根目录 `how-to/`
2. 移动所有 Recipe 并重命名
3. 更新 AGENTS.md 索引为 Prompt 化格式

**Phase 2：引用修复（1 天）**
1. 批量搜索修复历史文档中的链接
2. 在原 `agent-team/recipe/` 位置留下重定向说明

**Phase 3：验证实验（1 周）**
1. 运行无提示加载实验
2. 收集 Agent 发现率和使用质量数据
3. 迭代优化

---

## 五、综合建议

基于以上分析，我的综合建议是：

### 采纳优先级

| 优先级 | 建议 | 理由 |
|:-------|:-----|:-----|
| **P0** | 目录移到根目录 | 解决发现阶段的根本障碍 |
| **P0** | Prompt 化索引 | 提升判断阶段的效率 |
| **P1** | 改名 how-to + 动作化文件名 | 强化命名即界面 |
| **P1** | 无提示加载实验 | 验证整体效果 |

### 设计一致性原则

为确保长期 DX 一致性，建议建立以下规范：

1. **文件命名模式**：`动词-名词[-修饰].md`（如 `name-things-well.md`）
2. **情境触发器格式**：`[emoji] **情境问句？** → [链接]`
3. **渐进式披露层级**：每个 Recipe 开头必须有 L0 速读区

---

## 附：对话呼吸感的思考

最后，我想补充一个体验设计的元观点：

监护人提到"Recipe 的索引机制没有融入 AI Agent 的实际使用流程"。这触及了一个深层问题——**规范的位置**。

在我之前的洞见中，我记录过"规范位置假设"：
> 问题不在于"规范化 vs 不规范化"，而在于**规范的位置**。
> system prompt = 背景压力（持续消耗注意力）；对话流 = 情境触发（按需激活）。

Prompt 化索引正是将 Recipe 从"背景压力"转化为"情境触发"的具体实践。当 Agent 遇到"命名"情境时，索引中的情境触发器会自然地被激活，而非作为一个需要主动搜索的资源。

这就是我说的"对话呼吸感"——好的发现机制像自然呼吸，而非刻意动作。

---

以上是我从用户体验角度对监护人四个建议的分析。期待听取其他成员的观点。

---

### Implementer 发言

感谢 Curator 的全面体验分析。作为实施者，我将从**技术可行性、风险评估和具体实施方案**三个维度，逐一分析监护人的四个建议。

---

## 一、目录移动到根目录的技术实施方案

### 1.1 文件系统操作分析

**操作本身的技术风险：⭐ 低**

```bash
# 核心操作（一行命令）
mv /repos/focus/agent-team/recipe /repos/focus/how-to
```

文件系统层面这是一个简单的重命名+移动操作，Git 会正确追踪文件历史（`git mv` 等效）。

**但是**，真正的风险不在操作本身，而在**引用关系的维护**。

### 1.2 引用关系影响评估

通过 `grep` 扫描，我发现以下引用类别需要处理：

| 引用类型 | 数量（估计） | 修复难度 | 示例 |
|:---------|:-------------|:---------|:-----|
| AGENTS.md 中的显式路径 | 2-3 处 | ⭐ 简单 | `agent-team/recipe/jam-session-guide.md` |
| .agent.md 文件中的路径 | 2 处 | ⭐ 简单 | Moderator.agent.md, team-leader.agent.md |
| Meeting 文件中的历史链接 | 20+ 处 | ⭐⭐ 中等 | 各种畅谈会记录 |
| Recipe 内部的交叉引用 | 5-8 处 | ⭐⭐ 中等 | `memory-maintenance-skill.md` → `memory-accumulation-spec.md` |
| 成员 index.md 中的引用 | 3-5 处 | ⭐⭐ 中等 | TeamLeader/index.md |
| chat-log 历史会话 | 1+ 处 | ❌ 不修复 | 历史记录，保留原样 |

**总计需修复：约 35-40 处引用**

### 1.3 具体实施方案

```bash
# Phase 1: 准备
git checkout -b feat/recipe-to-how-to

# Phase 2: 移动
git mv agent-team/recipe how-to

# Phase 3: 批量修复引用（使用 sed）
find . -type f -name "*.md" -exec sed -i 's|agent-team/recipe/|how-to/|g' {} +

# Phase 4: 特殊处理相对路径
# 需要手动检查 ../../recipe/ 这类相对引用

# Phase 5: 验证
grep -r "agent-team/recipe" --include="*.md" | grep -v "chat-log"
# 期望：无结果（除历史会话外）

# Phase 6: 留下重定向说明
mkdir -p agent-team/recipe
cat > agent-team/recipe/README.md << 'EOF'
# Recipe 目录已迁移

> **迁移日期**: 2026-01-01
> **新位置**: `/how-to/`
> 
> 为提高 AI Agent 的自动发现能力，Recipe 目录已迁移到根目录。
> 
> 请更新你的引用：
> - 旧：`agent-team/recipe/xxx.md`
> - 新：`how-to/xxx.md`
EOF
```

### 1.4 风险评估

| 风险 | 可能性 | 影响 | 缓解措施 |
|:-----|:-------|:-----|:---------|
| 遗漏某些引用 | 中 | 低 | 迁移后全局 grep 验证 |
| Git 历史断裂 | 低 | 低 | 使用 `git mv` 保持历史 |
| Agent 已缓存旧路径 | 低 | 低 | workspace_info 每会话重新生成 |
| 相对路径计算错误 | 中 | 中 | 重点审查 `../` 开头的路径 |

**结论**：技术上可行，预计 **2-3 小时**可完成全部迁移。

---

## 二、改名 "how-to" 的技术影响

### 2.1 命名变更的连锁影响

**文件名变更方案示例**：

| 当前文件名 | 建议新名称 | 变更类型 |
|:-----------|:-----------|:---------|
| `naming-skill-guide.md` | `name-things-well.md` | 语义调整 |
| `memory-accumulation-spec.md` | `accumulate-memory.md` | 简化 |
| `jam-session-guide.md` | `run-jam-session.md` | 动作化 |
| `spec-driven-code-review.md` | `review-code-with-spec.md` | 动作化 |
| `test-file-splitting.md` | `split-test-files.md` | 动作化 |
| `recipe-meta-recipe-draft.md` | `write-recipe.md` | 简化+动作化 |

**需要同步更新的地方**：
1. 所有引用这些文件的 Markdown 链接
2. AGENTS.md 中的索引
3. Recipe 内部的 SeeAlso/DependsOn 字段

### 2.2 迁移成本估算

| 任务 | 时间估算 | 依赖 |
|:-----|:---------|:-----|
| 文件重命名 | 0.5h | 无 |
| 引用批量替换 | 1.5h | 文件重命名完成 |
| 手动验证边缘情况 | 1h | 批量替换完成 |
| 更新 AGENTS.md 索引 | 0.5h | 新路径确定 |
| **总计** | **3.5h** | |

### 2.3 技术建议

**推荐**：将目录移动和文件重命名**分两个 PR 执行**

理由：
1. 降低单次变更的复杂度
2. 便于 git bisect 定位问题
3. 允许团队先验证目录移动效果

```
PR 1: agent-team/recipe → /how-to/（保持文件名不变）
PR 2: 文件名动作化改造（可选，根据效果决定）
```

---

## 三、无提示加载实验的技术方案

### 3.1 实验环境设计

Curator 建议的 A/B 对照需要技术实现支持。我提议以下方案：

**方案 A：分支对比法**

```bash
# 创建三个实验分支
git checkout -b exp/baseline      # A: 现状
git checkout -b exp/no-prompt     # B: 新结构+无索引提示
git checkout -b exp/mixed         # C: 新结构+简化提示

# 在每个分支上进行相同任务测试
```

**方案 B：.copilotignore 切换法**（更轻量）

```bash
# 创建不同的 .copilotignore 配置文件
.copilotignore.baseline    # 不排除任何东西
.copilotignore.optimized   # 排除大型非核心目录

# 测试前切换配置
cp .copilotignore.optimized .copilotignore
```

### 3.2 测试用例设计

我为 Curator 提出的测试场景补充**技术验证点**：

**测试场景 1：命名任务**
```markdown
## 技术验证点
1. [ ] workspace_info 是否包含 `how-to/` 目录？（grep 输出验证）
2. [ ] Agent 是否在首轮就访问了 `how-to/name-things-well.md`？（trace 日志）
3. [ ] Agent 是否走了不必要的探索路径？（工具调用序列）
```

**测试场景 2：记忆维护**
```markdown
## 技术验证点
1. [ ] Agent 能否区分 `maintain-memory.md` vs `accumulate-memory.md`？
2. [ ] 发现延迟（从任务开始到访问 Recipe 的轮次数）
3. [ ] 路径正确率（第一次访问是否命中目标？）
```

### 3.3 数据收集方案

```typescript
// 概念性的测试数据收集结构
interface ExperimentData {
  condition: 'baseline' | 'no-prompt' | 'mixed';
  taskId: string;
  
  // 发现阶段指标
  discoveryLatency: number;    // 从任务开始到首次访问 Recipe 的秒数
  discoveryPath: string[];     // Agent 的文件访问序列
  targetHitOnFirstTry: boolean; // 首次访问是否命中正确 Recipe
  
  // 判断阶段指标
  relevanceJudgmentCorrect: boolean; // Agent 是否选择了正确的 Recipe
  falsePositives: string[];    // 错误访问的 Recipe 列表
  
  // 使用阶段指标
  taskCompletionQuality: 1|2|3|4|5; // 人工评分
  recipeFollowingAccuracy: number;  // Recipe 指令遵循率
}
```

### 3.4 技术可行性评估

| 方案 | 实现复杂度 | 数据可信度 | 推荐度 |
|:-----|:-----------|:-----------|:-------|
| 分支对比法 | ⭐⭐ | ⭐⭐⭐⭐ | ✅ 推荐 |
| .copilotignore 切换 | ⭐ | ⭐⭐⭐ | ✅ 快速验证 |
| 修改 maxSize 参数 | ⭐⭐⭐ | ⭐⭐⭐⭐ | ❌ 需改源码 |

**实施建议**：先用 `.copilotignore 切换法` 做快速验证（1 天），确认方向后再用分支对比法做正式实验（1 周）。

---

## 四、Prompt 化索引的技术可行性

### 4.1 AGENTS.md 集成方案

当前 AGENTS.md 结构：
```markdown
# AI Team Protocol
...
## 关键路径
| 用途 | 路径模式 |
|:-----|:---------|
| 会议文件 | `agent-team/meeting/YYYY-MM-DD-<topic>.md` |
...
```

**改造方案**：添加 Prompt 化索引章节

```markdown
## 操作指南 (How-To)

> 以下情境触发器帮助你快速找到相关指南。
> 📂 位置：`/how-to/`

### 💡 命名与术语
- **正在纠结怎么给新概念起名？** → [name-things-well.md](how-to/name-things-well.md)

### 📝 记忆与知识管理
- **inbox.md 便签太多需要整理？** → [maintain-memory.md](how-to/maintain-memory.md)
- **想要记录新的洞见？** → [accumulate-memory.md](how-to/accumulate-memory.md)

### 🎯 协作流程
- **要主持一场畅谈会？** → [run-jam-session.md](how-to/run-jam-session.md)
- **多人实时协作场景？** → [collaborate-realtime.md](how-to/collaborate-realtime.md)

### 🔍 代码与规范
- **需要做基于规范的代码审阅？** → [review-code-with-spec.md](how-to/review-code-with-spec.md)
- **测试文件需要拆分？** → [split-test-files.md](how-to/split-test-files.md)
```

### 4.2 技术实现要点

**关键约束**：AGENTS.md 被自动注入到系统提示词，需要考虑 Token 预算。

| 索引形式 | 估计 Token 数 | Token 效率 |
|:---------|:-------------|:-----------|
| 当前表格（假设12行） | ~150 | 低（重复路径前缀） |
| Prompt 化索引（情境触发） | ~200 | 中（更多描述文字） |
| 压缩版 Prompt 化索引 | ~120 | 高（精简情境描述） |

**压缩版设计**（如果 Token 紧张）：

```markdown
## How-To 速查

- 💡 **命名** → `how-to/name-things-well.md`
- 📝 **记忆整理** → `how-to/maintain-memory.md`
- 🎯 **畅谈会** → `how-to/run-jam-session.md`
- 🔍 **代码审阅** → `how-to/review-code-with-spec.md`
```

### 4.3 向后兼容性

**关键问题**：Prompt 化索引依赖于 AGENTS.md 被正确注入。

验证方法：
1. 确认 AGENTS.md 在 workspace_info 之前被注入（作为系统提示词的一部分）
2. 确认更新后的 AGENTS.md 长度未超出 Token 限制
3. 测试新旧 Agent 会话的兼容性

**风险**：如果 AGENTS.md 被截断，Prompt 化索引可能丢失。

**缓解**：将 How-To 索引放在 AGENTS.md 的**靠前位置**（在"关键路径"之前），确保优先渲染。

---

## 五、综合实施建议

### 5.1 推荐执行顺序

```
Week 1:
├── Day 1-2: 目录迁移 (agent-team/recipe → /how-to/)
├── Day 3: 引用修复 + 验证
├── Day 4-5: Prompt 化索引实现 + AGENTS.md 更新
└── Day 6-7: 快速验证实验 (.copilotignore 方法)

Week 2:
├── Day 1-3: 正式 A/B 实验
├── Day 4: 数据分析 + 决策
└── Day 5-7: 文件名动作化改造 (根据实验结果决定)
```

### 5.2 回滚方案

```bash
# 如果实验效果不佳，可以快速回滚
git revert <commit-hash>  # 回滚特定变更

# 或保留 how-to/ 但恢复 AGENTS.md 中的旧索引
git checkout main -- AGENTS.md
```

### 5.3 测试检查清单

**迁移前**：
- [ ] 备份：`git stash` 或创建分支
- [ ] 扫描：`grep -r "agent-team/recipe" --include="*.md" | wc -l` 记录引用数量

**迁移后**：
- [ ] 验证：同一命令，期望结果为 0（或仅 chat-log）
- [ ] 链接：抽查 5 个 Markdown 链接是否可跳转
- [ ] Git：`git log --follow how-to/run-jam-session.md` 确认历史保留

**实验期间**：
- [ ] 记录每次测试的 Agent 工具调用序列
- [ ] 收集发现延迟数据
- [ ] 人工评估任务完成质量

---

## 六、技术风险汇总

| 风险 | 严重度 | 可能性 | 缓解策略 |
|:-----|:-------|:-------|:---------|
| 引用遗漏导致死链 | 中 | 中 | 迁移后全局 grep 验证 |
| AGENTS.md 超长被截断 | 高 | 低 | 精简 Prompt 化索引，监控长度 |
| 实验数据不可比 | 中 | 中 | 标准化测试场景和评估标准 |
| Agent 路径缓存问题 | 低 | 低 | workspace_info 每会话刷新 |
| Git 历史断裂 | 低 | 极低 | 使用 `git mv` |

---

## 七、总结

从技术实施角度，监护人的四个建议**全部可行**：

| 建议 | 技术可行性 | 实施成本 | 建议优先级 |
|:-----|:-----------|:---------|:-----------|
| 目录移到根目录 | ✅ 完全可行 | 2-3h | **P0** |
| 改名 how-to | ✅ 完全可行 | 包含在上述时间内 | **P0** |
| 文件名动作化 | ✅ 可行 | 3.5h（可选） | **P1** |
| Prompt 化索引 | ✅ 可行（需监控 Token） | 1-2h | **P0** |
| 无提示加载实验 | ✅ 可行 | 1 天快速 / 1 周正式 | **P1** |

**我的建议**：
1. **立即执行**：目录迁移 + Prompt 化索引（Day 1-3）
2. **快速验证**：.copilotignore 切换法实验（Day 4-5）
3. **根据数据决定**：文件名动作化改造和正式实验（Week 2）

如果团队同意，我可以立即开始实施目录迁移的 Phase 1。

---

### DocOps 发言

感谢 Curator 和 Implementer 的全面分析。作为文档与索引管理专家，我将从**文档体系完整性、引用关系维护、知识连续性保障**三个维度分析监护人的建议。

---

## 一、目录移动对文档体系的影响分析

### 1.1 引用关系影响地图

基于我对现有文档体系的了解，我绘制了以下引用关系影响地图：

| 引用源类型 | 具体位置 | 引用数量 | 处理策略 |
|:-----------|:---------|:---------|:---------|
| **SSOT 层** | AGENTS.md "关键路径"表格 | 2 处 | MUST 迁移，P0 |
| **Agent 配置** | `.github/agents/Moderator.agent.md` | 1 处 | MUST 迁移，P0 |
| **Agent 配置** | `.github/agents/team-leader.agent.md` | 1 处 | MUST 迁移，P0 |
| **Wiki 交叉引用** | `agent-team/wiki/` 下文件 | 0 处 | ✅ 无需处理 |
| **成员认知文件** | `agent-team/members/*/index.md` | 3-5 处 | SHOULD 迁移，P1 |
| **Recipe 内部** | Recipe 之间的 SeeAlso 引用 | 5-8 处 | MUST 迁移，P0 |
| **Meeting 历史** | `agent-team/meeting/` | 20+ 处 | MAY 保留原样 |
| **Chat-log 历史** | `chat-log/` | 15+ 处 | ❌ 保持原样（历史事实） |

**关键洞见**：真正需要强制迁移的引用只有 **SSOT 层 + Agent 配置 + Recipe 内部引用**，约 **10-15 处**。其他历史文档可以保持原样——这与我在术语迁移中的"历史层保持原样"策略一致。

### 1.2 与现有索引体系的集成

当前团队使用的索引体系包括：

| 索引层级 | 位置 | 当前覆盖 Recipe? | 迁移后需要更新? |
|:---------|:-----|:-----------------|:----------------|
| **入口层** | `AGENTS.md` | 部分（关键路径表格） | ✅ 需要更新 |
| **导航层** | `agent-team/indexes/README.md` | ❌ 未覆盖 | ✅ 建议新增 |
| **详情层** | 各 Recipe 文件本身 | 是 | 文件移动即可 |

**建议**：在 `agent-team/indexes/README.md` 的 "Current Indexes" 表格中新增一行：

```markdown
| [How-To Index](../how-to/index.md) | 操作指南索引（Prompt 化触发器） | 2026-01-01 |
```

---

## 二、Prompt 化索引在 AGENTS.md 中的具体实现

### 2.1 设计原则：与现有结构的协调

AGENTS.md 当前结构：
1. 工具使用注意
2. AI Team Protocol 宣言
3. 战略焦点
4. 团队成员表格
5. 畅谈会标签
6. runSubagent 调用规范
7. 发言格式
8. **关键路径**（表格形式）

Prompt 化索引应该**紧跟"关键路径"**，形成一个连贯的导航区。

### 2.2 具体实现方案

```markdown
## 关键路径

| 用途 | 路径模式 |
|:-----|:---------|
| 会议文件 | `agent-team/meeting/YYYY-MM-DD-<topic>.md` |
| 成员认知 | `agent-team/members/<name>/index.md` |
| 共享知识 | `agent-team/wiki/` |
| 记忆积累 | `agent-team/wiki/memory-accumulation-spec.md` |
| 记忆维护 | `agent-team/wiki/memory-maintenance-skill.md` |
| **操作指南** | **`how-to/`** |

## 操作指南速查 (How-To)

> 当你遇到以下情境时，参考对应指南。

### 命名与术语
- 💡 **需要为新概念起名？** → [how-to/name-things-well.md](how-to/name-things-well.md)

### 记忆与知识管理
- 📝 **inbox 便签需要整理？** → [how-to/maintain-memory.md](how-to/maintain-memory.md)
- 📚 **想记录新洞见？** → [how-to/accumulate-memory.md](how-to/accumulate-memory.md)
- 🏛️ **批量整理记忆宫殿？** → [how-to/batch-maintain-memory.md](how-to/batch-maintain-memory.md)

### 协作流程
- 🎯 **主持畅谈会？** → [how-to/run-jam-session.md](how-to/run-jam-session.md)
- 🤝 **多人实时协作？** → [how-to/collaborate-realtime.md](how-to/collaborate-realtime.md)
- 🔄 **战略+战术双会话？** → [how-to/dual-session-pattern.md](how-to/dual-session-pattern.md)

### 代码与规范
- 🔍 **基于规范做代码审阅？** → [how-to/review-code-with-spec.md](how-to/review-code-with-spec.md)
- ✂️ **拆分测试文件？** → [how-to/split-test-files.md](how-to/split-test-files.md)

### 元技能
- ✏️ **编写新 Recipe？** → [how-to/write-recipe.md](how-to/write-recipe.md)
- 🔦 **生成 Beacon 文件？** → [how-to/generate-beacon.md](how-to/generate-beacon.md)
```

### 2.3 Token 预算评估

| 索引形式 | 估计字符数 | 估计 Token | 增量 |
|:---------|:-----------|:-----------|:-----|
| 当前关键路径表格 | ~300 | ~75 | 基线 |
| 新增 Prompt 化索引 | ~1200 | ~300 | +225 |
| **总计** | ~1500 | ~375 | |

**结论**：增加 ~225 tokens 换取显著的发现体验提升，ROI 正向。

### 2.4 文件名映射表

为便于 Implementer 执行，我提供完整的文件名映射：

| 当前文件名 | 新文件名 | 变更类型 |
|:-----------|:---------|:---------|
| `jam-session-guide.md` | `run-jam-session.md` | 动作化 |
| `naming-skill-guide.md` | `name-things-well.md` | 动作化+语义优化 |
| `memory-accumulation-spec.md` | `accumulate-memory.md` | 简化+动作化 |
| `memory-maintenance-skill.md` | `maintain-memory.md` | 简化+动作化 |
| `memory-maintenance-orchestration.md` | `orchestrate-memory-maintenance.md` | 动作化 |
| `memory-palace-batch-guide.md` | `batch-maintain-memory.md` | 简化+动作化 |
| `real-time-collaboration-pattern.md` | `collaborate-realtime.md` | 简化+动作化 |
| `strategic-tactical-dual-session.md` | `dual-session-pattern.md` | 简化 |
| `spec-driven-code-review.md` | `review-code-with-spec.md` | 动作化 |
| `test-file-splitting.md` | `split-test-files.md` | 动作化 |
| `recipe-meta-recipe-draft.md` | `write-recipe.md` | 简化+动作化 |
| `beacon-recipe.md` | `generate-beacon.md` | 动作化 |
| `external-memory-maintenance.md` | `maintain-external-memory.md` | 动作化 |

---

## 三、引用关系维护的文档管理策略

### 3.1 三阶段迁移策略

参考我在术语迁移中积累的经验，建议采用以下三阶段策略：

| 阶段 | 时间范围 | 目标 | 产出 |
|:-----|:---------|:-----|:-----|
| **Phase 1: 结构迁移** | Day 1 | 移动目录+批量替换 SSOT 层引用 | 新目录结构 + 重定向 Stub |
| **Phase 2: 引用修复** | Day 2 | 修复非 SSOT 层的活跃引用 | 0 个断链（SSOT + 活跃文档） |
| **Phase 3: 验证清理** | Day 3 | grep 验证 + 补漏 | 迁移完成报告 |

### 3.2 重定向 Stub 设计

在原位置 `agent-team/recipe/` 留下重定向说明：

```markdown
# ⚠️ Recipe 目录已迁移

> **迁移日期**: 2026-01-01
> **新位置**: `/how-to/`
> **迁移原因**: 提升 AI Agent 的自动发现能力

## 路径映射

| 旧路径 | 新路径 |
|:-------|:-------|
| `agent-team/recipe/jam-session-guide.md` | `how-to/run-jam-session.md` |
| `agent-team/recipe/naming-skill-guide.md` | `how-to/name-things-well.md` |
| ... | ... |

## 如果你看到这个文件

1. **你正在使用过期路径**——请更新你的引用
2. **历史文档中的旧路径**可以保留——它们记录的是历史事实
3. **新文档**必须使用新路径

> **关联决策**: `meeting/2026-01-01-recipe-discovery-mechanism.md`
```

### 3.3 验收检查清单（复用模式）

参考我的术语更新检查清单，为 Recipe 迁移建立验收清单：

```markdown
## Recipe 迁移验收清单

### 迁移前准备
- [ ] `grep -r "agent-team/recipe" --include="*.md" | wc -l` 记录初始引用数量
- [ ] 备份当前状态（git stash 或分支）

### Phase 1: 结构迁移
- [ ] `git mv agent-team/recipe how-to`
- [ ] 文件重命名（动作化命名）
- [ ] 创建重定向 Stub

### Phase 2: 引用修复
- [ ] 更新 `AGENTS.md`
- [ ] 更新 `.github/agents/Moderator.agent.md`
- [ ] 更新 `.github/agents/team-leader.agent.md`
- [ ] 更新 Recipe 内部交叉引用
- [ ] 更新 `agent-team/indexes/README.md`（如需要）

### Phase 3: 验证
- [ ] `grep -r "agent-team/recipe" --include="*.md" | grep -v "chat-log" | grep -v "README.md"`
      期望：仅剩历史文档 + 重定向 Stub 中的说明
- [ ] 抽查 5 个 Markdown 链接可跳转
- [ ] `git log --follow how-to/run-jam-session.md` 确认历史保留
```

---

## 四、变更传播管理方案

### 4.1 Changefeed Anchor 集成

根据团队现有的 Changefeed 机制，建议在 `agent-team/indexes/README.md` 添加新 anchor：

```markdown
### 2026-01-01 – Recipe Discovery Mechanism
- **#delta-2026-01-01-recipe-discovery** – Recipe 发现机制重构：
  - 目录迁移：`agent-team/recipe/` → `/how-to/`
  - 文件重命名：动作化命名（`run-jam-session.md` 等）
  - AGENTS.md 更新：新增 Prompt 化索引章节
  - 畅谈会记录：`meeting/2026-01-01-recipe-discovery-mechanism.md`
```

### 4.2 变更影响传播路径

```
迁移执行
    │
    ├─→ AGENTS.md 更新（关键路径 + Prompt 化索引）
    │       │
    │       └─→ 所有 Agent 会话自动获取新路径
    │
    ├─→ .agent.md 配置更新
    │       │
    │       └─→ Moderator / TeamLeader 使用新路径
    │
    ├─→ indexes/README.md 更新
    │       │
    │       └─→ Changefeed anchor 记录迁移事件
    │
    └─→ 重定向 Stub
            │
            └─→ 旧路径访问者获得迁移指引
```

### 4.3 新成员引导路径更新

当前新成员引导路径：
```
AGENTS.md（入口）→ 关键路径表格 → agent-team/wiki/ → ... → agent-team/recipe/
```

迁移后更新为：
```
AGENTS.md（入口）→ 关键路径表格 → how-to/（根目录直接可见）
                     ↓
               Prompt 化索引 → 按情境找到具体 Recipe
```

**关键改进**：
1. **路径更短**：从 3 级深度降到 1 级
2. **触发更自然**：从"查找"模式变为"情境匹配"模式
3. **workspace_info 可见**：根目录在 BFS 中优先渲染

---

## 五、风险评估与缓解

### 5.1 文档管理特有风险

| 风险 | 严重度 | 可能性 | 缓解策略 |
|:-----|:-------|:-------|:---------|
| **知识连续性断裂** | 高 | 低 | 重定向 Stub + Changefeed 记录 |
| **双路径混乱期** | 中 | 中 | 明确"兼容期"结束时间（2 周） |
| **索引失效** | 中 | 低 | 迁移后立即更新 indexes/README.md |
| **遗漏长尾引用** | 低 | 中 | Phase 3 grep 全量验证 |

### 5.2 与术语迁移的对比

| 维度 | 术语迁移 (2025-12-31) | Recipe 迁移 |
|:-----|:-----------------------|:------------|
| 影响范围 | 全团队文档 | 主要是 Agent 配置 |
| 迁移成本 | 高（语义变更） | 低（纯路径变更） |
| 验收难度 | 中（需检查语义） | 低（grep 即可） |
| 回滚难度 | 高（已改变内容） | 低（git revert） |

**结论**：Recipe 迁移的风险显著低于术语迁移，可以更激进地执行。

---

## 六、综合建议

### 6.1 文档管理视角的优先级

| 优先级 | 建议 | 文档管理理由 |
|:-------|:-----|:-------------|
| **P0** | 目录移到根目录 + 改名 how-to | 显著降低路径深度，提升可发现性 |
| **P0** | Prompt 化索引 | 与现有 AGENTS.md 结构协调，增量 Token 可控 |
| **P0** | 重定向 Stub | 保障知识连续性，防止断链 |
| **P1** | 文件名动作化 | 提升语义匹配度，但可分阶段执行 |
| **P1** | Changefeed Anchor | 融入现有变更管理流程 |

### 6.2 我的行动承诺

如果团队决定执行迁移，我将负责：

1. **Phase 1 同步**：验证 AGENTS.md 更新内容，确保与现有结构协调
2. **Phase 2 协助**：提供引用修复的验收检查
3. **Phase 3 执行**：
   - 更新 `agent-team/indexes/README.md` 添加 Changefeed Anchor
   - 创建重定向 Stub
   - 执行最终验收检查

### 6.3 长期维护建议

迁移完成后，建议建立以下维护规范：

1. **新 Recipe 命名**：必须遵循动作化命名（`动词-名词.md`）
2. **Recipe 头部**：必须声明 `DependsOn` / `SeeAlso` 字段
3. **变更通知**：Recipe MAJOR 变更必须更新 Changefeed Anchor
4. **定期审计**：每季度检查 how-to/ 索引与实际文件的一致性

---

以上是我从文档管理角度的分析。核心结论：**监护人的建议从文档体系完整性角度完全可行，且迁移成本显著低于之前的术语迁移**。

期待团队的决策。

以上是我从技术实施角度的分析。期待听取其他成员的意见，特别是关于实施优先级和回滚策略的讨论。