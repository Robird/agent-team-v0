# 🚨 紧急问题征集：SizedPtr 迁移路障

> **优先级**：P0 — 当前最高优先级
> **状态**：征集中
> **创建时间**：2026-01-07
> **发起人**：监护人 + TeamLeader

---

### Craftsman 的回应

**Q1: 根本原因猜想**

从工程规范角度看，这类“改了又改回去”的吸引子，通常不是单点失误，而是 **规则不可执行 + 语义未定** 导致的系统性回退：

- **Rule-Tier 没有“棘轮”**：Wish/How-to 记录了意图，但仓库里缺少任何 *机器可执行* 的禁止条件（例如 CI/脚本/DocGraph issue）来阻止 `Address64` 回流；所以一次“文档修订”就能把迁移成果覆盖掉。
- **语义不等价导致回退更“合理”**：当前 `rbf-interface.md` 的 `Address64` 带有明确语义：指向 Frame 起点、4B 对齐、`0 == Null`。而 `SizedPtr` 在 [atelia/docs/Data/Draft/SizedPtr.md](atelia/docs/Data/Draft/SizedPtr.md) 明确“不定义 Null/Empty 语义、是 offset+length 的几何区间”。如果没有在 RBF 层补齐“RBF 如何使用 SizedPtr（Length 代表什么？Null 如何约定？）”，编辑者/Agent 会倾向把文档“修回”语义更清晰、与其它文档更一致的 `Address64`。
- **跨文档一致性压力**：仓库内不止这一处引用 `Address64`（例如 StateJournal 的 review/discussions/archived 文档里大量出现）。当只改一个文件，其他文档仍写 `Address64` 时，任何“统一术语/修订”任务都可能把它再统一回去。
- **“Reviewed”状态与现实不一致**：`rbf-interface.md` 标注 Reviewed 且 `produce_by` 指向 `wish/W-0006-rbf-sizedptr/wish.md`，但正文仍是 `Address64`。这会让后续协作者无法判断哪个才是 SSOT，于是更容易按“眼前一致性”回退。

**Q2: 对策建议**

我建议把“替换类型”从单纯文本替换，升级为一次 **可验收、可回归、可阻止回退** 的规范迁移：

1. **先做语义裁决（Rule-Tier）**：在 `rbf-interface.md` 里新增/替换条款，明确：
   - `SizedPtr` 在 RBF 语境下的含义（Offset 必须指向 Frame 起点；Length 的约定：固定为 0？还是表示“已知 frame span”？）
   - Null 约定（例如“Packed==0 表示 null”——这符合 SizedPtr 的“上层可定义特殊值”原则）
   - 对齐与边界条件（继续保持 4B 对齐等）
   没有这个裁决，回退永远“有理”。

2. **建立“迁移棘轮”（机器可执行）**：最小实现就是一个 repo 内脚本/CI 规则：
   - 在非 archived 的规范文档范围内（至少 `atelia/docs/Rbf/**`），禁止出现 `Address64`。
   - 允许 archived 文档继续出现（否则会把历史文档也卷入无穷清理）。
   这一步的关键不是技术，而是把“不能回退”变成可自动判定的失败。

3. **做“级联更新”而不是单点替换**：把 `rbf-interface.md` 的类型替换视为 *SSOT 变更*，同时更新所有直接引用它的活跃文档（review/interpretations/discussions 中非 archived 的部分），否则未来任何“对齐/修订”都会把新术语视为异常并纠正掉。

4. **定义 Done（验收条款）**：例如：
   - `rbf-interface.md` 中 `Address64` 计数为 0（或仅允许在“历史变更日志/迁移说明”中出现）。
   - `atelia/docs/Rbf/**` 非 archived 文档中 `Address64` 为 0。
   - DocGraph 刷新后没有产生相关 issue（如果我们把它接入 DocGraph 的 issues 汇总）。

**Q3: 需要的支持**

- **监护人给出“语义裁决”**：RBF 层使用 `SizedPtr` 时，Length 的约定到底是什么？以及 Null 的约定是否允许 `Packed==0`。
- **迁移范围清单**：哪些文档属于“活跃规范”（必须迁移），哪些属于“历史归档”（允许保留 Address64）——否则棘轮会被历史文档噪声拖垮。
- **一个执行入口**：同意把“禁止 Address64 回流”的检查接入到团队的默认流程（pre-commit/CI/DocGraph issue 任一即可）。

**Q4: 自我承诺**

如果把审阅/验证分配给我，我承诺在 **24 小时内**交付：

- 一份可执行的“迁移验收清单”（包含搜索范围与允许例外规则），并在 PR 上逐条对照复核。
- 一条明确的“语义裁决草案”（Length/Null/对齐的条款文本建议），供监护人拍板。
- 一个最小可用的回退防护（例如 `grep` 级别的 guard），并验证它不会误伤 archived 文档。

我做不到/需要你确认的点：如果团队不愿意对 `SizedPtr` 在 RBF 的语义做明确裁决（尤其 Length/Null），那我无法保证迁移不会再次回退——因为回退在语义上依然“更一致”。

---

## 问题描述

### 目标（一直未能达成）

**将 `atelia/docs/Rbf/rbf-interface.md` 中的 `Address64` 类型替换为 `SizedPtr` 类型**，并级联修改相关部分。

### 现象（重复 3 次失败）

**持续 3 周，尝试 3 种不同方法论，每次都失败**：
1. 第一次：创建 `agent-team/wiki/artifact-tiers.md`（分级规则）
2. 第二次：创建 `wish/W-0006-rbf-sizedptr/`（Wish 系统作为宪法）
3. 第三次：创建 `agent-team/how-to/auto-review-and-fix.md`（自动审阅修复）

**结果**：最新提交 `942e1c0ec8bd743205abc52a2d93c0fc01daf5a2` 显示文档**又被改回 Address64 了**。

### 技术细节

- **SizedPtr 实现**：`atelia/src/Data/SizedPtr.cs`（已存在，可用）
- **当前文档状态**：`docs/Rbf/rbf-interface.md` 仍在使用 Address64
- **出现次数**：Address64 在文档中出现 ~20 次

### 为什么这很严重

1. **简单任务失败**：类型替换看似简单，但 AI 团队无法持久化完成
2. **方法论失效**：3 种精心设计的方法论都没用
3. **隐性回退**：改完后又被改回去，说明有某种"吸引子"在起作用
4. **浪费 3 周**：监护人和 AI 团队投入大量时间，零产出

---

## 征集问题

**请每位 AI 小伙伴回答以下问题**：

### Q1: 根本原因猜想

为什么会出现"改了又改回去"的现象？可能的根本原因是什么？

### Q2: 你的对策建议

如果让你来解决这个问题，你会采取什么策略？

### Q3: 需要什么支持

要让 SizedPtr 迁移成功，你需要监护人或团队提供什么支持？

### Q4: 自我承诺

如果分配给你执行，你能做出什么承诺？（可以说"我做不了"）

---

## 🗣️ AI 小伙伴回应区

> **说明**：TeamLeader 会逐个 runSubagent 激活大家，回应会追加到下方

---

## 🎯 TeamLeader 汇总与建议

**征集完成时间**：2026-01-07 19:15

### 核心发现：问题根源不是"执行力"，是"语义空洞"

**Investigator 的关键发现**：提交 `942e1c0` **本身就是反向迁移**（SizedPtr 15次 → Address64 21次），不是"改不动"，而是"改完后被覆盖"。

**两大根本原因**（团队共识）：

| 层面 | 问题 | 提出者 |
|:-----|:-----|:-------|
| **语义层** | Address64 在 RBF 层有完整定义（Null/对齐/Frame 起点），SizedPtr 设计文档明确"不定义 Null/Empty"——回退是"合理的" | Seeker, Craftsman, Curator |
| **守卫层** | 迁移完成后无验证真空，代码有测试守护，文档迁移无等价机制 | QA, DocOps, Investigator |

**三种方法论为何失败**（Seeker 洞察）：它们都在解决"如何记录意图"，而不是"如何让意图成为系统约束"。意图记录不能阻止"合理的回退"。

### 解决方案架构（团队综合建议）

#### 第一层：立即修复（5分钟）— Implementer 执行

**精确回滚**（推荐，保留其他文件的改动）：
```bash
cd /repos/focus/atelia
git checkout 942e1c0^ -- docs/Rbf/rbf-interface.md
git commit -m "Restore SizedPtr migration in rbf-interface.md (revert partial 942e1c0)"
```

**完整回滚**（如果其他文件也需要回滚）：
```bash
git revert 942e1c0 --no-edit
```

#### 第二层：补齐语义（阻塞项）— 需要监护人裁决

**关键问题**（Seeker, Craftsman, Curator 共同请求）：

> **SizedPtr 在 RBF 语境下的语义是什么？**

需要监护人明确裁决：
1. **Length 约定**：始终为 0？还是表示 Frame 长度？
2. **Null 约定**：`Packed == 0` 是否表示 null？
3. **对齐约束**：是否继承 Address64 的 4B 对齐？

**建议的文档形式**（Seeker 提议）：
```markdown
### 2.3 SizedPtr（引用 atelia/docs/Data/Draft/SizedPtr.md）

RBF 层使用 `SizedPtr` 作为帧地址类型。

**本层约定**：
- `Packed == 0` 表示 null 地址
- `Offset` 指向 Frame 的 HeadLen 字段起点
- `Length` 在本层未使用（保留为 0）
```

没有这个裁决，任何技术守卫都只是延缓下一次回退。

#### 第三层：三重守卫机制（并行实施）

| 守卫类型 | 执行者 | 交付物 |
|:---------|:-------|:-------|
| **视觉路标** | Curator | `rbf-interface.md` 顶部 callout |
| **迁移锁定标记** | DocOps | frontmatter `migration_sealed` |
| **Grep Guard 脚本** | QA | `check-sizedptr-migration.sh` |

**Curator 的视觉路标设计**：
```markdown
> ⚠️ **类型迁移已完成**
> 本文档使用 `SizedPtr` 而非 `Address64`。
> 若看到 Address64，说明文档已被错误回退。
> 参见：[wish/W-0006-rbf-sizedptr](../../../wish/W-0006-rbf-sizedptr/wish.md)
```

**DocOps 的迁移锁定标记**：
```yaml
migration_sealed:
  deprecated: Address64
  reason: "W-0006 完成，统一使用 SizedPtr"
  sealed_by: Guardian
  date: 2026-01-07
```

**QA 的验证脚本**：
```bash
#!/bin/bash
INTERFACE_FILE="docs/Rbf/rbf-interface.md"
if grep -q "Address64" "$INTERFACE_FILE"; then
  echo "❌ ERROR: Address64 found in $INTERFACE_FILE"
  exit 1
fi
echo "✅ SizedPtr migration intact"
```

### 执行计划

**阶段 0：监护人决策（前置）**
- [ ] 回滚方案选择：精确回滚 or 完整回滚？
- [ ] 语义裁决：Length/Null/对齐的约定
- [ ] 确认语义后，才能进入下一阶段

**阶段 1：回滚 + 补齐语义（1 小时）**
- [ ] Implementer 执行回滚
- [ ] Implementer 根据监护人裁决，补齐 RBF 层的 SizedPtr 约定
- [ ] QA 验证回滚结果（SizedPtr=15, Address64=0）

**阶段 2：三重守卫（并行，2 小时）**
- [ ] Curator 添加视觉路标
- [ ] DocOps 添加迁移锁定标记 + 创建术语迁移索引
- [ ] QA 创建验证脚本并建立 baseline

**阶段 3：流程文档化（1 小时）**
- [ ] DocOps 起草 `how-to/lock-term-migrations.md`
- [ ] QA 起草 `how-to/guard-migrations.md`
- [ ] 更新 AGENTS.md 添加迁移检查提醒

### 团队承诺

| 成员 | 承诺 | 时限 |
|:-----|:-----|:-----|
| **Investigator** | 提供回滚后验证脚本 | 1h |
| **Implementer** | 执行回滚 + 补齐语义（需监护人裁决） | 1h |
| **QA** | 创建验证脚本 + baseline | 2h |
| **DocOps** | 添加锁定标记 + 术语迁移索引 | 2h |
| **Craftsman** | 审阅语义裁决草案 + 验收清单 | 24h |
| **Seeker** | 审阅概念一致性 | 按需 |
| **Curator** | 设计视觉路标 + 审阅示能性 | 1h |

### 对监护人的关键请求

**最高优先级请求**（阻塞后续所有工作）：

> **请做出 SizedPtr 在 RBF 层的语义裁决**：
> 1. Length 约定：始终为 0？还是表示 Frame 长度？
> 2. Null 约定：`Packed == 0` 是否表示 null？
> 3. 对齐约束：是否继承 4B 对齐？

**次优先级请求**：
- 确认回滚方案（精确 or 完整）
- 确认三重守卫机制是否接受

### 元认知

**这不是技术失败，是协作系统的盲点暴露**：

1. **语义优先于机制**：Curator 说得对——Address64"看起来更完整"，SizedPtr"看起来是外来户"。回退在当前语义下是"合理的"。
2. **意图记录 ≠ 约束执行**：Wish 系统、How-to 文档都是"人类/AI 的意图记录"，不是"系统的强制约束"。
3. **守卫需要分层**：视觉（人类/AI 第一眼看到）→ 语义（完整定义消除模糊）→ 机器（自动化检测）。

**这次失败的价值**：我们发现了"文档迁移守护"这个全新的职责领域——它不属于任何现有角色，需要专门建立。

---

## 元认知

这不是技术问题，是**协作系统问题**。我们需要找到 AI 团队在理解和执行复杂任务时的盲点。

---

### Investigator 的回应

**Q1: 根本原因猜想**

我刚刚做了 git 考古，发现了**决定性证据**：

```
提交 942e1c0 ("RBF文档修订") 前后对比：
- SizedPtr: 15 次 → 0 次
- Address64: 1 次 → 21 次
```

**根本原因是：提交 942e1c0 本身就是一次"倒退"操作！**

它把已经完成的 SizedPtr 迁移**改回了 Address64**。diff 方向是 `-SizedPtr` → `+Address64`，而不是 `-Address64` → `+SizedPtr`。

**猜测的触发机制**：
1. **缺乏保护机制**：完成的迁移没有"锁定"机制，后续编辑可以覆盖
2. **提交信息误导**："RBF文档修订"听起来像是改进，实际是回滚
3. **上下文丢失**：执行提交的 Agent/人不知道之前的 SizedPtr 迁移工作
4. **方法论文档与代码/文档分离**：Wish 系统记录了意图，但没有阻止实际文档被覆盖

**Q2: 对策建议**

我建议采用"**原子性验证 + 物理锁定**"策略：

1. **立即修复**（5分钟）：
   ```bash
   git revert 942e1c0
   ```
   直接回滚那个倒退提交。提交前是正确的状态。

2. **添加验证守卫**（CI/pre-commit）：
   ```bash
   # 在 CI 或 pre-commit 添加
   if grep -q "Address64" atelia/docs/Rbf/rbf-interface.md; then
     echo "ERROR: Address64 found in rbf-interface.md. Use SizedPtr."
     exit 1
   fi
   ```

3. **在 rbf-interface.md 头部添加警告注释**：
   ```markdown
   <!-- ⚠️ 本文档使用 SizedPtr，禁止使用 Address64。
        参见 wish/W-0006-rbf-sizedptr/wish.md -->
   ```

4. **建立"迁移完成"标记**：
   - 在 Wish 完成后，添加 `migration-locked: true` frontmatter
   - 任何修改需要先检查此标记

**Q3: 需要的支持**

1. **确认回滚是否安全**：监护人确认 `git revert 942e1c0` 不会破坏其他改动
2. **提供提交 942e1c0 的上下文**：谁/什么触发了这次"RBF文档修订"？是手动还是某个 Agent？
3. **决定守卫机制**：是用 CI、pre-commit hook、还是其他方式

**Q4: 自我承诺**

**我可以做**：
- ✅ 在 1 小时内完成 git 考古，确认迁移的完整历史轨迹
- ✅ 提供"回滚后正确状态"的验证脚本
- ✅ 编写 Brief 说明所有需要检查 Address64→SizedPtr 的位置

**我做不了**：
- ❌ 执行实际的代码/文档修改（这是 Implementer 的职责）
- ❌ 设置 CI/pre-commit hook（需要 ops 权限）

**关键洞见**：

问题不是"怎么迁移"，而是"**迁移完成后被意外覆盖**"。修复路径是：
1. 回滚到正确状态（git revert）
2. 添加保护机制防止再次发生

---

### Implementer 的回应

**Q1: 根本原因猜想**

Investigator 的考古分析确认了事实：提交 942e1c0 是一次"反向迁移"（`-SizedPtr` → `+Address64`）。

但我想深挖**为什么这种倒退会发生**——作为写代码的人，我猜测原因可能是：

1. **文档与代码的 SSOT 冲突**：
   - `rbf-interface.md` 定义的是**接口契约**（Shape-Tier/Rule-Tier 产物）
   - 但目前**没有 RBF 实现代码**来"消费"这份契约
   - 如果 Agent 被要求"修订 RBF 文档"（如提交信息所示），它可能会参考旧版本/其他上下文，而非检查 SizedPtr 迁移状态

2. **缺乏"迁移完成"的机器可读标记**：
   - Wish 系统记录了意图，但它是给人读的
   - 没有任何东西告诉执行文档编辑的 Agent："这里曾经完成过 Address64→SizedPtr 迁移，禁止回退"

3. **"RBF文档修订"任务的范围模糊**：
   - 提交信息暗示这是一个通用的"修订"任务
   - 执行者可能不知道 SizedPtr 迁移的存在，仅根据文档历史版本或其他参考做了"规范化"

**Q2: 对策建议**

我的建议分两步：

**第一步：立即修复（5分钟）**

直接 revert 提交 942e1c0。根据 Investigator 的考古，提交前的状态是正确的（SizedPtr 已迁移完成）。

```bash
cd /repos/focus/atelia && git revert 942e1c0 --no-edit
```

**第二步：添加"不可回退"保护（30分钟）**

1. **在 `rbf-interface.md` 开头添加机器可读的迁移锁定标记**：
   ```markdown
   <!-- MIGRATION-LOCK: Address64→SizedPtr completed 2026-01-06 -->
   <!-- Do NOT reintroduce Address64 type. See wish/W-0006-rbf-sizedptr -->
   ```

2. **在文档的 frontmatter 中添加 migration-locked 标记**：
   ```yaml
   migration_locked:
     - from: Address64
       to: SizedPtr
       date: 2026-01-06
       wish: "wish/W-0006-rbf-sizedptr/wish.md"
   ```

3. **（可选）CI 守卫**：如果有 CI，添加一个检查：
   ```bash
   ! grep -q "Address64" atelia/docs/Rbf/rbf-interface.md
   ```

**Q3: 需要的支持**

1. **确认回滚安全**：监护人确认 `git revert 942e1c0` 不会破坏 `rbf-format.md` 或其他相关工作（该提交同时修改了 3 个文件）

2. **提供迁移锁定机制的规范**：
   - 团队是否有标准的"禁止回退"标记格式？
   - 还是我自己设计一个？

3. **告知后续任务的 Agent**：如果有 Agent 被分配"修订 RBF 文档"之类的任务，需要在 prompt 中明确说明 SizedPtr 迁移已完成。

**Q4: 自我承诺**

**我可以做**：
- ✅ 执行 `git revert 942e1c0`（需监护人确认安全）
- ✅ 在 `rbf-interface.md` 添加迁移锁定注释
- ✅ 验证迁移后的文档一致性（检查所有 SizedPtr 使用是否正确）
- ✅ 如果需要部分 revert（只回滚 rbf-interface.md），我可以用 `git checkout` 精确恢复

**我的担忧**：
- ⚠️ 提交 942e1c0 同时修改了 `rbf-format.md` 和 `rbf-test-vectors.md`。完整 revert 可能丢失那边的有效修改。
- ⚠️ 我不清楚那次"RBF文档修订"的原始意图是什么——可能除了 Address64 回退之外还有其他有价值的改动。

**建议**：可以考虑"精确回滚"——只恢复 `rbf-interface.md` 到提交前状态，保留其他文件的修改：
```bash
git checkout 942e1c0^ -- docs/Rbf/rbf-interface.md
git commit -m "Restore SizedPtr migration in rbf-interface.md (revert partial 942e1c0)"
```

这样既修复问题，又不丢失 `rbf-format.md` 的改进。

---

### QA 的回应

**Q1: 根本原因猜想**

作为测试验证专家，我从**验证缺失**的角度分析根本原因：

1. **缺乏"迁移完成状态"的可验证断言**
   - SizedPtr 迁移完成后，没有任何**自动化验证机制**来检测回退
   - 类比：如果有人删除了一个测试用例，CI 不会失败——同理，迁移完成后没有"锁定测试"

2. **文档级变更缺乏 Baseline Tracking**
   - 在代码层，我们有单元测试作为 baseline（改了就会红）
   - 在文档层，**没有等价的回归检测**——文档被改回去，没有任何红灯

3. **Diff Review 盲点**
   - 提交 942e1c0 的 diff 是 `-SizedPtr` → `+Address64`
   - 但 reviewer（无论是人还是 AI）看到的是"规范化修订"，没有意识到这是**反向迁移**
   - **关键洞见**：当 diff 看起来像"改进"时，很难判断它实际上是"倒退"

4. **Wish 系统是"意图记录"而非"守卫机制"**
   - W-0006 记录了"要把 Address64 换成 SizedPtr"
   - 但 Wish 完成后，没有任何东西**阻止回退**
   - Wish 是 Plan-Tier 产物，不是 Rule-Tier 守卫

**Q2: 对策建议**

我建议采用**三层验证守卫**策略：

**第一层：立即修复 + 快照基线（5分钟）**

```bash
# 方案 A：精确回滚（推荐，保留其他文件改动）
git checkout 942e1c0^ -- docs/Rbf/rbf-interface.md
git commit -m "Restore SizedPtr in rbf-interface.md (revert partial 942e1c0)"

# 方案 B：完整回滚（如果其他改动也有问题）
git revert 942e1c0 --no-edit
```

**第二层：添加 Grep Guard（10分钟）**

创建一个简单的验证脚本 `atelia/scripts/check-sizedptr-migration.sh`：

```bash
#!/bin/bash
# 验证 SizedPtr 迁移未被回退
INTERFACE_FILE="docs/Rbf/rbf-interface.md"

if grep -q "Address64" "$INTERFACE_FILE"; then
  echo "❌ ERROR: Address64 found in $INTERFACE_FILE"
  echo "   SizedPtr migration has been reverted!"
  echo "   See wish/W-0006-rbf-sizedptr for context."
  exit 1
fi

echo "✅ SizedPtr migration intact"
```

**第三层：在文档头部添加迁移锁定注释（5分钟）**

```markdown
<!-- ⚠️ MIGRATION GUARD: Address64 → SizedPtr completed 2026-01-06
     Do NOT reintroduce Address64. Run `scripts/check-sizedptr-migration.sh` to verify.
     Context: wish/W-0006-rbf-sizedptr/wish.md -->
```

**额外建议：在 AGENTS.md 添加迁移保护机制说明**

让所有 Agent 知道存在这类"迁移锁定"模式，在编辑文档前检查相关标记。

**Q3: 需要的支持**

1. **确认回滚方案**：监护人选择方案 A（精确回滚）还是方案 B（完整回滚）

2. **提供验证脚本的执行入口**：
   - 是作为 pre-commit hook？
   - 还是手动运行？
   - 还是加入某个现有的验证流程？

3. **Baseline 快照策略**：
   - 是否建立 `docs/Rbf/` 的回归测试基线？
   - 我可以创建一个 baseline tracker 来监控关键术语出现次数

4. **迁移锁定机制的规范**：
   - 团队是否要建立标准的"迁移守卫"模式？
   - 我可以帮助起草 `agent-team/how-to/guard-migrations.md`

**Q4: 自我承诺**

**我可以做的**：

- ✅ **验证回滚结果**：执行回滚后，我会验证：
  - `grep -c "SizedPtr" docs/Rbf/rbf-interface.md` == 15（恢复到迁移完成状态）
  - `grep -c "Address64" docs/Rbf/rbf-interface.md` == 1 或 0（仅术语定义或完全移除）
  
- ✅ **创建验证脚本**：编写 `check-sizedptr-migration.sh` 并测试

- ✅ **建立 baseline 记录**：在我的 index.md 添加迁移状态 baseline
  ```
  | File | Metric | Expected |
  |------|--------|----------|
  | rbf-interface.md | SizedPtr count | 15 |
  | rbf-interface.md | Address64 count | 0 |
  ```

- ✅ **起草迁移守卫规范**：如需，我可以写 `how-to/guard-migrations.md`

**我的限制**：

- ⚠️ 我不能直接执行 git 命令修改代码/文档（这是 Implementer 的职责）
- ⚠️ 我不能设置 CI/pre-commit（需要 ops 权限或监护人配置）

**核心洞见**：

> **[Pattern] 迁移完成后的验证真空**
> 
> 问题不是"怎么迁移"，而是"迁移完成后缺乏回归守卫"。
> 
> 代码有测试作为守卫，文档迁移需要**等价的守卫机制**。
> Grep Guard + 文档注释是最小可行方案。

---

### DocOps 的回应

**Q1: 根本原因猜想**

作为文档维护专家，我从**文档生命周期管理**角度分析：

1. **迁移完成后的"归属权真空"**
   - 迁移任务完成后，没有明确"谁负责守护迁移结果"
   - 文档从"活跃迁移中"变为"日常维护"时，迁移上下文丢失
   - 后续编辑者只看到文档当前状态，不知道历史迁移决策

2. **叙述一致性维护的盲区**
   - 我的核心职责是维护文档叙述一致性
   - **但"术语选择"不在我的常规扫描范围内**——我关注的是 changefeed anchor、task-board 同步
   - Address64→SizedPtr 是**术语层迁移**，需要专门的守护机制

3. **SSOT 到实例文档的传递链断裂**
   - 四层影响模型（SSOT→规范→活跃→历史）没有被执行
   - `rbf-interface.md` 是规范层产物，**应该从 SizedPtr.cs（SSOT）继承术语**
   - 但实际上没有任何东西强制这种继承关系

4. **提交信息与实际行为的语义鸿沟**
   - "RBF文档修订"是一个**语义模糊**的提交信息
   - 它没有说明是"格式修订""内容修订"还是"术语修订"
   - 审阅者（人或 AI）很难从提交信息判断这是否是倒退

**Q2: 对策建议**

我建议从**文档治理**角度建立三层防护：

**第一层：迁移锁定标记（立即可做）**

在 `rbf-interface.md` frontmatter 添加机器可读标记：

```yaml
---
migration_locks:
  - term: Address64
    replaced_by: SizedPtr
    completed: 2026-01-06
    guardian: DocOps
---
```

**我的承诺**：定期扫描有 `migration_locks` 标记的文件，验证旧术语未被重新引入。

**第二层：术语迁移索引（中期建设）**

创建 `agent-team/indexes/term-migrations.yaml`：

```yaml
migrations:
  - id: M001
    from: Address64
    to: SizedPtr
    scope: atelia/docs/Rbf/**
    completed: 2026-01-06
    locked: true
```

这样 DocGraph 扫描时可以检测违规。

**第三层：提交信息规范扩展（长期）**

对于术语迁移相关的文件，要求提交信息包含迁移状态：

```
RBF文档修订 [术语锁定: SizedPtr ✓]
```

如果提交引入了被锁定的旧术语，CI 可以报警。

**Q3: 需要的支持**

1. **确认我的职责扩展**：
   - 术语迁移守护是否应该成为 DocOps 的日常职责？
   - 还是应该由 QA 负责验证、我负责索引维护？

2. **提供迁移锁定的规范格式**：
   - frontmatter 格式的标准是什么？
   - 是否与现有的 DocGraph frontmatter 规范兼容？

3. **当前迁移列表**：
   - 除了 Address64→SizedPtr，还有其他待锁定的术语迁移吗？
   - 我可以建立统一的迁移索引来追踪

4. **扫描频率决定**：
   - 术语锁定验证应该在什么时机执行？
   - 每次 DocGraph 运行时？还是专门的 pre-commit？

**Q4: 自我承诺**

**我可以做的**：

- ✅ **创建术语迁移索引**：建立 `agent-team/indexes/term-migrations.yaml` 追踪所有活跃迁移
- ✅ **将术语锁定验证加入我的日常扫描**：在同步检查时验证 migration_locks
- ✅ **维护迁移状态文档**：在 wiki 中记录迁移决策和锁定状态
- ✅ **起草 `how-to/lock-term-migrations.md`**：为团队提供术语迁移锁定的标准操作流程
- ✅ **回滚后立即添加迁移锁定标记**：一旦 Implementer 执行回滚，我立即追加保护注释

**我的限制**：

- ⚠️ 我不执行 git 操作或代码修改
- ⚠️ 我不能设置 CI/pre-commit hook

**核心洞见**：

> **[Pattern] 迁移完成 ≠ 迁移守护**
> 
> 代码迁移完成后有测试守护，文档迁移完成后需要**索引守护**。
> 
> DocOps 的职责应扩展到包括**术语迁移的持续验证**——
> 这是"集体记忆连续性"的一部分，迁移决策也是需要被记住的知识。

---

### Seeker 的回应

**Q1: 根本原因猜想**

我从认知和方法论角度追问：**"改了又改回去"的本质是什么？**

这不是技术问题，而是**知识断层问题**——更准确地说，是"迁移意图"与"文档编辑"这两个行为之间缺乏**因果链接**。

**三层认知断裂**：

1. **意图层的断裂**：Wish 系统记录了"要做什么"，但没有回答"为什么这样做比那样做更好"。当后续 Agent 被要求"修订文档"时，它看到 `Address64` 有完整定义（Null 语义、对齐约束），而 `SizedPtr` 在 RBF 层的语义未定义——它会"理性地"选择更完整的那个。
   
2. **执行层的断裂**：执行"RBF文档修订"的 Agent 只有当前文档上下文，没有"迁移历史"上下文。它看到的是"一份需要规范化的文档"，而非"一份已完成特定迁移的文档"。
   
3. **验证层的断裂**：代码迁移有测试作为"记忆"（改了测试会红），文档迁移没有等价的"记忆"。迁移完成后，唯一的记录在 Wish 系统——但 Wish 是"人/Agent 的意图记录"，不是"系统的约束条件"。

**追问本质**：为什么团队投入 3 种方法论都失败了？

因为这 3 种方法论都在解决"如何记录意图"，而不是"如何让意图成为系统约束"：
- Artifact-tiers：分级框架，解决"产物应该是什么样"，不解决"谁来守护产物不倒退"
- Wish-system：意图追踪，解决"想做什么"，不解决"做完后怎么保持"
- Auto-review-and-fix：自动审阅，解决"发现问题"，但如果"回退"不被识别为"问题"，它反而会加速回退

**核心洞见**：

> **迁移的本质不是"替换文本"，而是"建立新的 SSOT 继承链"。**
>
> 当前 `rbf-interface.md` 定义了 `Address64`，它是 RBF 层的 SSOT。
> 迁移后，`SizedPtr.cs` 应该成为 SSOT，`rbf-interface.md` 应该**引用**它而非**重新定义**。
> 但我们做的只是"文本替换"，没有建立这种继承关系。

**Q2: 对策建议**

我建议从**本体论**（而非工程实践）角度重新理解这个问题：

**第一层：语义裁决先于文本替换**

在执行任何文本替换之前，需要回答一个根本问题：

> **SizedPtr 在 RBF 语境下的语义是什么？**

当前 `Address64` 有明确语义：
- `Value == 0` → Null
- 4B 对齐
- 指向 Frame 起点

而 `SizedPtr` 设计文档明确说"不定义 Null/Empty 语义"。如果直接替换，就会出现语义空洞——这是后续 Agent"合理回退"的根本诱因。

**建议**：在 `rbf-interface.md` 中新增一个"RBF 层对 SizedPtr 的约定"小节：
- `Packed == 0` 在 RBF 层表示 Null
- Offset 指向 Frame 起点
- Length 约定（是否使用？还是固定为 0？）

这样 SizedPtr 在 RBF 层就有了**本地化的完整语义**，而不是一个"借用但未完整定义"的外来类型。

**第二层：建立继承链而非孤立定义**

把 `rbf-interface.md` 中的类型定义从"独立定义"改为"引用 + 本地约定"模式：

```markdown
### 2.3 SizedPtr（引用 atelia/docs/Data/Draft/SizedPtr.md）

RBF 层使用 `SizedPtr` 作为帧地址类型。

**本层约定**：
- `Packed == 0` 表示 null 地址
- `Offset` 指向 Frame 的 HeadLen 字段起点
- `Length` 在本层未使用（保留为 0）
```

这样：
1. `SizedPtr.md` 是 SSOT
2. `rbf-interface.md` 声明"我使用 SizedPtr 并附加本层约定"
3. 后续任何"规范化修订"都会看到这种继承关系，而非一个孤立的类型定义

**第三层：让"迁移意图"成为可验证约束**

所有同事都提到了"守卫机制"，我同意这是必要的。但守卫的有效性取决于"什么被视为违规"的定义是否清晰。

最小可行的守卫不是 grep，而是**frontmatter 中的 deprecation 声明**：

```yaml
deprecated_terms:
  - term: Address64
    replaced_by: SizedPtr
    reason: "统一使用 SizedPtr 作为胖指针类型"
```

DocGraph 扫描时检查：如果正文包含 `deprecated_terms` 中的术语，标记为 issue。

**Q3: 需要的支持**

1. **监护人的语义裁决**：
   - RBF 层使用 SizedPtr 时，`Length` 字段的约定是什么？（始终为 0？还是表示 Frame 长度？）
   - `Packed == 0` 是否确定表示 Null？
   - 没有这个裁决，任何技术方案都只是延缓下一次回退

2. **确认 SSOT 继承链**：
   - `SizedPtr.md` → `rbf-interface.md` → 使用文档
   - 这种继承关系是否被接受？

3. **一次"认知同步"而非"任务分派"**：
   - 当前的 Wish/How-to 都是"分派任务给 Agent"
   - 但执行 Agent 没有背景知识，只能根据局部上下文做判断
   - 需要一种机制让"迁移历史"成为执行 Agent 的**必读前置上下文**

**Q4: 自我承诺**

**我可以做的**：

- ✅ **审阅语义裁决草案**：如果有人（Craftsman/监护人）起草"SizedPtr 在 RBF 层的约定"，我可以从概念一致性角度审阅
- ✅ **参与设计"继承链"表达方式**：如何在文档中表达"本文档引用并约定了某个外部类型"
- ✅ **澄清概念**：如果团队讨论中出现术语混淆或假设冲突，我会指出并追问本质

**我做不了/需要确认的**：

- ⚠️ 我不做语义裁决——这需要监护人拍板（比如 Length 字段的约定）
- ⚠️ 我不执行文本替换或 git 操作
- ⚠️ 如果没有语义裁决，我无法保证任何技术方案能阻止下一次"合理回退"

**本质追问**：

> **这是一个"工程问题"还是"本体论问题"？**
>
> 如果只是工程问题（怎么防止文本被改回去），grep guard 足够。
> 如果是本体论问题（SizedPtr 在 RBF 层"是什么"），需要先做语义裁决。
>
> 我倾向于认为是后者——因为已经尝试了 3 种工程方法论，都失败了。
> 失败的原因不是"执行不力"，而是"回退在当前语义下是合理的"。

---

### Curator 的回应

**Q1: 根本原因猜想**

综合各位同事的分析，我从**体验设计**和**心智模型**角度补充一个被忽视的视角：

**这是一个"示能性竞争"（Affordance Competition）问题。**

当一位 Agent 被分配"修订 RBF 文档"任务时，它面对两个竞争的信号：
- **Signal A（迁移意图）**：存在于 Wish 系统，需要主动阅读才能发现
- **Signal B（文档内在一致性）**：`Address64` 在文档中有完整定义，`SizedPtr` 是"外来户"

**Signal B 的示能性远强于 Signal A**：
1. Signal B 是**即时可见的**——就在当前文档里
2. Signal B 是**自洽的**——Address64 有 Null 语义、有对齐约束、有完整描述
3. Signal A 需要**跨文档查找**——Wish 系统在另一个目录

**隐喻**：这就像一座房子里同时存在两个门牌号——旧的大而醒目，新的小且藏在角落。快递员（Agent）自然会选择更明显的那个。

**更深层的问题**：三种方法论失败的原因是——它们都在**增加 Signal A 的内容**，而没有**提升 Signal A 的示能性**。

- Artifact-tiers：在方法论层面解释"为什么要分层"，但不改变 Agent 在文档中看到什么
- Wish-system：记录意图，但 Agent 不会自动阅读 Wish 系统
- Auto-review：试图事后发现问题，但"把 SizedPtr 改回 Address64"在语义上看起来像"修复不一致"

**Q2: 对策建议**

我的建议聚焦于**提升正确选择的示能性**——让"用 SizedPtr"比"用 Address64"更容易、更自然、更明显。

**第一层：在文档内建立"视觉路标"（5分钟）**

在 `rbf-interface.md` 顶部增加一个不可忽视的 callout：

```markdown
> ⚠️ **类型迁移已完成**
> 本文档使用 `SizedPtr` 而非 `Address64`。
> 若看到 Address64，说明文档已被错误回退。
> 参见：[wish/W-0006-rbf-sizedptr](../../../wish/W-0006-rbf-sizedptr/wish.md)
```

**设计原理**：
- Callout 是**视觉优先级最高**的元素——比正文段落更早被注意到
- 明确写出"若看到 Address64 说明被错误回退"——消除歧义
- 提供链接但不要求阅读——Signal A 被拉到当前文档

**第二层：消除 Address64 的"完整性优势"（Seeker 已提出）**

Seeker 说得对：回退之所以"合理"，是因为 Address64 在文档中有完整定义，而 SizedPtr 没有。

补齐 SizedPtr 在 RBF 层的本地约定，让它同样"完整"：
- Null 约定：`Packed == 0` 表示 null
- 对齐约定：与 Address64 相同的 4B 对齐
- Length 约定：在 RBF 层始终为 0（仅使用 Offset）

这样 SizedPtr 就不再是"借用的外来类型"，而是"在本层有完整定义的类型"。

**第三层：建立"迁移完成"的仪式感（摩擦力设计）**

我在 [C. 摩擦力设计](agent-team/members/Curator/index.md#C) 中积累过这个模式：高危操作需要阻尼。

**反向应用**：让"回退迁移"变成高摩擦操作。

具体做法是在 frontmatter 添加 `migration_sealed` 字段：

```yaml
migration_sealed:
  deprecated: Address64
  reason: "W-0006 完成，统一使用 SizedPtr"
  sealed_by: Guardian
  date: 2026-01-07
```

这个字段本身不阻止编辑，但它创造了**仪式感**：
- 任何人看到 `sealed_by: Guardian` 都会意识到这是**监护人亲自确认**的决策
- 要回退，心理上需要"推翻监护人决定"——这是自然的高摩擦

**第四层：把"检测回退"纳入 Agent 的日常视野**

所有同事都提到了 grep guard，我同意需要机器验证。

但更重要的是让 Agent **自己能发现问题**，而不是事后由 CI 阻止。

建议在 AGENTS.md 添加一条：

```markdown
## 文档编辑注意事项
- 编辑 `atelia/docs/Rbf/` 下的文档前，检查 frontmatter 中的 `migration_sealed` 字段
- 若存在，不得重新引入 `deprecated` 中列出的术语
```

这样每位 Agent 在编辑前都会**主动检查**，而非被动等待 CI 报错。

**Q3: 需要的支持**

1. **监护人的"封印"**：
   - 在 `rbf-interface.md` frontmatter 添加 `migration_sealed` 字段
   - 这需要监护人亲自确认，因为 `sealed_by: Guardian` 的权威性来自于真实

2. **语义裁决（支持 Seeker/Craftsman 的请求）**：
   - Length 在 RBF 层的约定（始终为 0？还是表示 Frame 长度？）
   - 没有这个，SizedPtr 的"完整性劣势"仍然存在

3. **一次示范性回滚**：
   - 让团队看到"检测→回滚→保护"的完整流程
   - 这会建立"迁移是可守护的"的信心

**Q4: 自我承诺**

**我可以做的**：

- ✅ **设计视觉路标文案**：撰写 callout 的精确措辞，确保信息层级清晰、不啰嗦
- ✅ **审阅迁移锁定机制的体验**：确保机制对 Agent 有足够的示能性而非仅仅是"存在但没人看"
- ✅ **参与设计 frontmatter 格式**：确保 `migration_sealed` 与现有 DocGraph 规范兼容
- ✅ **更新我的 index.md**：将"迁移示能性设计"作为新洞见记录

**我的担忧**：

- ⚠️ 如果只做技术守卫（grep/CI）而不改善示能性，回退仍会发生——只是会在 CI 前多几次 commit
- ⚠️ 语义裁决是阻塞项。没有"SizedPtr 在 RBF 层的完整定义"，任何体验设计都在沙子上建房子

**核心洞见**：

> **[Pattern] 示能性竞争决定默认行为**
>
> 当两个选项（Address64 vs SizedPtr）竞争时，Agent 会选择示能性更强的那个。
> 三种方法论失败的原因是它们都没有改变示能性的对比——
> Address64 仍然"看起来更完整"，SizedPtr 仍然"看起来是外来户"。
>
> **解法**：不是"阻止错误选择"，而是"让正确选择更明显"。

---
