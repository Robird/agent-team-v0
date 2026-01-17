# atelia-copilot-chat：Upstream 追 tag 的两条线工作流（含 rebase --onto）

## 目的

把仓库维护成两条长期稳定的“线”，避免在同一分支里不断 merge upstream 导致历史发散、冲突面放大：

- **Upstream 线（只跟随 upstream tag）**：始终保持“纯净”，不带任何 Atelia 改动。
- **Atelia 补丁线（只放 Atelia 改动）**：把我们的低侵入修改做成一组可重放的 commits（patch series），每次升级到新 tag 就把这组 commits 重新 replay 到新 tag 上。

这样做的直接收益：

- 每次追新 tag 时，你只需要处理“补丁线 rebase 时的冲突”，而不是在一个已经混入很多上游非 release 提交/历史 merge 的分支里做大合并。
- 可以做到“逐 tag 追”，也可以做到“逐提交追”，并且都能保持变动很小。

## 本仓库的约定（建议固定下来）

### 分支

- `line/upstream`
  - **只**指向 upstream 的某个 release/tag（例如 `v0.36.0`）。
  - **禁止**在该分支上提交任何 Atelia 改动。

- `line/atelia`
  - 基于某个 upstream tag（例如 `v0.36.0`），叠加 Atelia 改动（patch series）。
  - 允许 rebase（甚至 force-push），因为它本质是“补丁重放线”。

### tag

- upstream 原始 tag：例如 `v0.36.0`、`v0.36.1`（来自 upstream）。
- Atelia 对齐点 tag（推荐新增一类）：
  - 例如 `atelia-v0.36.0-patches`：表示“在 upstream `v0.36.0` 基础上重放 Atelia patch series 后的头”。

> 注意：不要覆盖 upstream 同名 tag。Atelia 自己的对齐点建议用 `atelia-...` 前缀。

## 一次性初始化（只做一次）

1. 确保有 upstream remote

```bash
git remote -v
# 应该能看到 upstream -> https://github.com/microsoft/vscode-copilot-chat.git
```

2. 拉取 upstream tags

```bash
git fetch upstream --tags
```

3. 建立两条线（以 v0.36.0 为例）

```bash
# upstream 线：纯 tag
git checkout -B line/upstream v0.36.0

# atelia 线：在 tag 上应用补丁（两种方式，二选一）
# A) 如果你已经有一个“对齐点”提交（比如旧分支/旧 tag），用 squash 抽出差异
git checkout -B line/atelia v0.36.0
git merge --squash <你的对齐点提交或tag>
git commit -m "Atelia patches on v0.36.0"

# B) 如果你已经把 Atelia 改动整理成多个 commits（推荐），就直接 cherry-pick 那些 commits
# git cherry-pick <c1> <c2> ...
```

4. 推送（首次创建分支）

```bash
git push -u origin line/upstream line/atelia
```

## 标准流程：追到“下一个 upstream tag”（推荐）

假设：
- 现在 upstream 对齐点是 `v0.36.0`
- 要追到 `v0.36.1`

### Step 0：获取新 tag

```bash
git fetch upstream --tags
```

### Step 1：更新 upstream 线

```bash
git checkout line/upstream
# 方式 A：用 reset 保证绝对纯净（推荐）
git reset --hard v0.36.1

# 推送（如果你允许 upstream 线被重置，则需要 force）
# 更保守的做法：line/upstream 只在本地用；或每个 tag 单独建 branch。
# 这里给出“允许重置”的方案：
git push --force-with-lease origin line/upstream
```

> 如果你不想 force-push：可以改为“每个 tag 建一个只读分支”，例如 `upstream/v0.36.1`。

### Step 2：把 Atelia 补丁线 rebase 到新 tag（核心）

```bash
git checkout line/atelia

# 关键命令：把 (oldTag, line/atelia] 这段 commits 重放到 newTag 上
# 等价理解：把 Atelia 的 patch series 从旧底座搬到新底座

git rebase --onto v0.36.1 v0.36.0

# 如果有冲突：
# - 修冲突
# - git add -A
# - git rebase --continue
```

完成后，你的 `line/atelia` 就变成：

- 基座：`v0.36.1`
- 顶部：Atelia patch series（内容不变，commit id 变化）

### Step 3：最小验证

在这个仓库里经常会遇到安装环境差异，建议走“最小可信验证”而不是追求 100% e2e。

```bash
# 依赖安装：某些环境下 Playwright 下载会失败，可先跳过
PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1 npm ci

npm run compile
npm run typecheck

# 可选：跑 unit/extension tests
npm test
```

### Step 4：打 Atelia 对齐点 tag + 推送

```bash
# 约定：打在 line/atelia 的 HEAD 上
git tag -a atelia-v0.36.1-patches -m "Atelia patch-line head based on upstream v0.36.1"

git push origin line/atelia
git push origin atelia-v0.36.1-patches
```

## 进阶：逐提交追到下一个 tag（更繁琐，但每步更小）

如果你真的希望“每次冲突都极小”，可以把 upstream 的变更拆成每个提交一步步推进。

### 思路

- 不要直接从 `v0.36.0` 跳到 `v0.36.1`。
- 把 `v0.36.0..v0.36.1` 区间的 commits 按顺序逐个应用到一个临时分支上。
- 每推进一小步就跑一轮最小检查（比如 typecheck）。

### 操作示例

```bash
# 1) 准备一个临时分支，从旧 tag 起步
git checkout -B tmp/upstream-step v0.36.0

# 2) 列出到新 tag 的提交（按时间正序）
git log --reverse --oneline v0.36.0..v0.36.1

# 3) 逐个 cherry-pick（示例：挑一个 commit）
git cherry-pick <commit1>
# 如有冲突，修复后继续

# 4) 当 tmp/upstream-step 最终到达 v0.36.1
#    你可以把 line/upstream 重置到它，或直接 reset 到 tag（看你策略）
```

> 一般来说：Atelia patch 很小的话，逐提交追 upstream 的收益不大；
> 真正该逐提交处理的是 `line/atelia` 的 rebase（它本来就是逐提交 replay）。

## Agent 执行时的注意事项（减少“意外编辑”）

- 永远不要在 `line/upstream` 上提交。
- 任何冲突处理都发生在 `line/atelia` 的 rebase 流程里。
- 若遇到大冲突：优先把 Atelia patch commit 拆小（例如每个文件一个 commit），下次 replay 更容易定位问题。
- 用 `git range-diff` 检查 patch 系列在新旧底座上的变化：

```bash
# 对比新旧 patch 重放后的差异（示例）
git range-diff v0.36.0..atelia-v0.36.0-patches v0.36.1..atelia-v0.36.1-patches
```

## 故障恢复

- rebase 过程中想放弃：

```bash
git rebase --abort
```

- 把分支直接重置回某个对齐点：

```bash
git reset --hard atelia-v0.36.0-patches
```
