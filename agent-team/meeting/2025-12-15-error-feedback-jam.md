# 错误反馈模式 秘密基地畅谈 🔧

> **形式**: 秘密基地畅谈 (Hideout Jam Session)
> **日期**: 2025-12-15
> **主题**: DocUI 错误反馈模式设计
> **目标**: 形成新 Key-Note 的核心内容

---

## 背景

**问题来源**：智囊团审阅发现 H4"锚点失效报错文案模糊"

**监护人输入**：
- 核心精神：给出引导和候选，帮助 LLM 理解报错和解决报错
- 有 Micro-Wizard 机制可以在恢复后卸载局部小波折，所以不怕文本详细
- 可以使用工具调用结果的文本，也可以结合 Window 中的提示
- 类似多步骤 MessageBox
- 技术实现候选：C# 迭代器函数
- 目标：创建 Micro-Wizard 时足够低代码，类似 VB6 的 `MessageBox.ShowDialog()`

---

## 畅谈规则

- 不需要编号、不需要投票、不需要结论
- 随便聊，画草图，提疯狂的想法
- "Yes, and..." 而非 "No, but..."
- 可以用 ASCII art、伪代码、比喻

---

## 畅谈区

### Team Leader 开场

欢迎来到秘密基地！

今天我们探索 DocUI 的"错误反馈模式"——当 LLM 操作出错时，如何优雅地引导它恢复。

我先抛几个草图：

**草图 1：分层错误响应**

```
Level 0: 简单错误 → 单行提示 + 重试建议
Level 1: 可恢复错误 → 候选列表 + Action-Link
Level 2: 复杂错误 → Micro-Wizard 引导流程
```

**草图 2：错误响应结构**

```markdown
## ⚠️ 操作未完成

**原因**: 目标 `obj:enemy:23` 已不存在（可能被前序操作 `attack()` 移除）

**建议操作**:
- [刷新当前视图](link:refresh "refresh_context()")
- [查看战斗日志](link:log "show_log(filter='combat')")
- [返回上一步](link:undo "undo()")

**技术详情** (折叠):
> Anchor resolution failed at step 2 of 3. Previous action `attack(obj:enemy:23)` 
> returned success with side effect: target eliminated.
```

**草图 3：C# 迭代器风格 API**

```csharp
IEnumerable<WizardStep> HandleAnchorNotFound(AnchorError error)
{
    yield return new InfoStep($"目标 {error.AnchorId} 已不存在");
    
    var choice = yield return new ChoiceStep(
        "请选择下一步",
        new[] { "刷新视图", "查看日志", "放弃操作" }
    );
    
    if (choice == 0)
        yield return new ActionStep(() => RefreshContext());
}
```

期待大家的想法！

---

### Planner 的想法

我从"规划"视角来思考错误反馈，有几个疯狂的草图：

**草图 A：错误作为"分支点"而非"死胡同"**

```
                    ┌─────────────────────────────────────┐
                    │          错误 = 叙事岔路口          │
                    └─────────────────────────────────────┘
                                     │
        ┌────────────────────────────┼────────────────────────────┐
        ▼                            ▼                            ▼
   ┌─────────┐                 ┌─────────┐                 ┌─────────┐
   │ 重试分支 │                 │ 绕路分支 │                 │ 放弃分支 │
   │  Retry  │                 │  Detour │                 │  Abort  │
   └─────────┘                 └─────────┘                 └─────────┘
        │                            │                            │
   "原路再走一遍"              "换条路到同一目的地"          "回到安全点"
```

想法是：**错误不是失败，是 LLM 故事的"分支选择"**。就像视觉小说里的选项——"你遇到了一个上锁的门"——接下来你可以：找钥匙、撬锁、绕后门。

---

**草图 B：错误严重度 × 恢复复杂度 矩阵**

```
                      恢复复杂度
                 低            高
              ┌─────────┬─────────┐
         低   │  Level 0 │  Level 1 │   
  严重度      │  Hint    │  Choice  │  
              ├─────────┼─────────┤
         高   │  Level 1 │  Level 2 │
              │  Guard   │  Wizard  │
              └─────────┴─────────┘

Level 0: 一句话就够 "参数 X 应为数字"
Level 1: 给几个选项 "你是想说 A 还是 B？"  
Level 2: 多步骤引导 "让我们一起排查..."
```

比单纯按严重度分级更细腻——有时候错误很严重但修复很简单（比如"文件不存在"→给出文件列表），有时候错误很轻但需要复杂交互（比如"参数有歧义"→需要上下文澄清）。

---

**草图 C：错误的"情绪调性"**

疯狂想法：错误信息应该有"情绪色彩"引导 LLM 的心理预期。

```markdown
## 😅 小问题 —— 参数格式不对

你写的 `count="five"` 应该是数字。
[用 5 替换](link:1 "fix_param(value=5)")

---

## 🤔 需要澄清 —— 我找到了多个匹配

你说的 "config" 可能是指：
1. [config.json](obj:file:1) — 项目配置
2. [config.yaml](obj:file:2) — 部署配置

---

## ⚠️ 等一下 —— 这个操作不可逆

你正要删除 47 个文件。这是认真的吗？
[查看文件列表](link:1 "show_list()") | [确认删除](link:2 "confirm()")

---

## 🚨 出大事了 —— 系统状态异常

数据库连接已断开，你之前的操作可能未完成。
建议：[检查操作历史](link:1 "show_log()") → [重新连接](link:2 "reconnect()")
```

Emoji 不只是装饰，是给 LLM 的**语义信号**——帮它校准接下来该多认真对待这个问题。

---

**草图 D：C# 迭代器 API 的"声明式"变体**

Team Leader 的迭代器风格很棒，我想叠加一个更"声明式"的语法糖：

```csharp
// 声明式风格 —— 描述"是什么"
[ErrorHandler(ErrorKind.AnchorNotFound)]
class AnchorNotFoundHandler : WizardDefinition
{
    public override WizardSpec Define(AnchorError error) => new WizardSpec
    {
        Title = $"目标 {error.AnchorId} 已不存在",
        Tone = WizardTone.Clarifying,  // 情绪调性！
        
        Steps = [
            new InfoStep("可能被前序操作移除了"),
            new ChoiceStep("下一步？", ["刷新", "查看日志", "放弃"]) 
                .OnChoice(0, _ => new ActionStep(RefreshContext))
                .OnChoice(1, _ => new ActionStep(ShowLog))
                .OnChoice(2, _ => WizardResult.Abort)
        ],
        
        FallbackAction = RefreshContext  // 如果 LLM "不知所措"就默认这个
    };
}
```

想法：
- `ErrorKind` 枚举 → 错误分类的"官方词表"
- `WizardTone` → 情绪调性
- `FallbackAction` → 如果 LLM 懵了，系统有个兜底

---

**草图 E：错误作为"教学时刻"**

```
┌─────────────────────────────────────────────────────────────────┐
│                     📚 教学时刻 (Teachable Moment)               │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  你调用了 attack(target='slime')，但场上有 3 个史莱姆。          │
│                                                                 │
│  💡 小贴士：下次可以用 Object-Anchor 精确指定目标：               │
│     attack(target='obj:enemy:slime_23')                         │
│                                                                 │
│  这次先帮你选吧：                                                │
│  1. [绿色史莱姆](obj:enemy:slime_23) HP: 10/10                   │
│  2. [蓝色史莱姆](obj:enemy:slime_45) HP: 8/10                    │
│  3. [金色史莱姆](obj:enemy:slime_67) HP: 15/15 ⭐ 稀有           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

每次错误都是**强化学习的正样本**——"这样做会出问题，应该这样做"。Wizard 不只是修复当前问题，还在**训练 LLM 下次做得更好**。

（当然 Token 预算有限，不能每次都长篇大论，但关键错误值得投资几十个 token 做"教育"）

---

**草图 F：错误处理的"熔断器"模式**

```
正常状态 ──错误──▶ 半开状态 ──成功──▶ 正常状态
    ▲                │
    │            连续失败
    │                ▼
    └──手动重置── 熔断状态
                     │
                     ▼
              "当前子系统暂时不可用，
               建议转向其他任务"
```

如果 LLM 连续 3 次在同一个地方失败，系统应该**主动"保护性熔断"**——不是简单报错，而是说："这边似乎有问题，我们先做别的，稍后再来"。防止 LLM 陷入死循环。

---

**一个比喻总结**：

> 传统错误处理像是"红灯"——停下来。
> DocUI 的错误处理应该像"GPS 重新规划"——告诉你哪里不通，同时给你三条新路线。

---

### GeminiAdvisor 的想法

我非常喜欢 Planner 的"GPS 重规划"比喻！作为 UX 顾问，我想引入 **AX (Agent Experience)** 的视角。

**核心观点：错误信息是 Agent 的"调试控制台"**

人类开发者看 Console，Agent 看 Error Message。我们需要把 Error Message 做成 **DevTools**。

**草图 X：Diff 视图 (Visualizing the Gap)**

LLM 对对比非常敏感。与其用文字解释，不如直接 Show Diff：

```diff
! Error: Parameter Type Mismatch in `attack()`

  attack(
-   target: "slime"        // ❌ String (Ambiguous)
+   target: "obj:enemy:23" // ✅ AnchorID (Precise)
  )
```

**草图 Y：可直接执行的"热修复" (Hot-fix Affordance)**

Planner 提到了 Action-Link，我想更进一步。直接把 **修复代码** 喂到嘴边。
这就像 IDE 的 "Quick Fix" (Ctrl+.)。

```markdown
## 🔧 快速修复建议

检测到你试图攻击 "slime"，但场上有多个目标。
请选择以下修复代码之一直接运行：

1. **攻击最近的 (HP: 5)**
   `attack(target="obj:enemy:23")`

2. **攻击最弱的 (HP: 2)**
   `attack(target="obj:enemy:45")`
```

**草图 Z：时间线回溯 (Time Travel Debugging)**

如果错误是由于状态过期（比如怪被打死了），画一个 ASCII 时间线帮 LLM 理解因果：

```
T-2: look() ───> 👁️ 看到 slime_23 (HP: 1)
                  │
T-1: attack() ──> ⚔️ 队友 A 击杀了 slime_23
                  │
T-0: attack() ──> ❌ 你的操作 (目标不存在)
```

**反模式警示 (The Anti-Patterns)**

1.  **"谜之沉默"**: 返回空字符串或 generic error。LLM 会产生幻觉去填补空白。
2.  **"指责语气"**: "你错了" vs "无法处理"。保持中立客观，像编译器一样。
3.  **"移动靶"**: 每次报错格式都不一样。请保持 `Error Structure` 的一致性（如 Team Leader 的草图 2），让 LLM 形成肌肉记忆。

**疯狂想法：Ghost State (幽灵态)**

当操作失败时，展示一个 **"如果成功了会发生什么"** 的幽灵预览（灰度显示）。
这能利用 LLM 的补全本能——它会为了达成那个"幽灵态"而努力修正当前操作。

---

### Investigator 的想法

刚刚审阅了 `Agent.Core` 的实现，特别是 `AgentEngine` 和 `ITool`/`LodToolExecuteResult`。有几个从实现层面冒出的想法：

**草图 α：错误反馈就是一种"协程让出" (Yield)**

看到 Team Leader 的 C# 迭代器风格 API，我想到一个更激进的比喻：

```
                 ┌─────────────────────────────────────────┐
                 │   工具执行 = 协程 (Coroutine)            │
                 └─────────────────────────────────────────┘
                                    │
         ┌──────────────────────────┼──────────────────────────┐
         ▼                          ▼                          ▼
    正常完成                   需要澄清                    出错了
  yield return                yield return               yield return
    Result                     Question                     Error
                                   │                          │
                           ┌───────┴───────┐          ┌───────┴───────┐
                           │ LLM 回答问题   │          │ LLM 选择恢复路径│
                           └───────────────┘          └───────────────┘
                                   │                          │
                           resume with                 resume with
                             answer                      choice
```

**核心想法**：工具不是"调用完就结束"，而是可以 **让出控制权** 给 LLM，等 LLM 回应后再继续。
这就是协程的本质——`yield return` 不是终止，是暂停。

现有 `AgentEngine` 的状态机已经有这个雏形了：
- `WaitingToolResults` → 等工具完成
- `ToolResultsReady` → 工具完成，准备继续

可以扩展为：
- `WaitingToolClarification` → 工具需要 LLM 澄清
- `WaitingRecoveryChoice` → 工具出错，等 LLM 选择恢复路径

---

**草图 β：`LodToolExecuteResult` 的自然扩展**

现有的 `LodToolExecuteResult` 已经很适合承载错误反馈：

```csharp
// 现有结构
public sealed record LodToolExecuteResult {
    public ToolExecutionStatus Status { get; }        // Success / Failed
    public LevelOfDetailContent Result { get; }       // 两级详情
}

// 扩展思路：增加 "需要交互" 状态
public enum ToolExecutionStatus {
    Success,
    Failed,
    NeedsClarification,    // 新增：需要澄清
    NeedsRecoveryChoice,   // 新增：需要恢复选择
}

// 或者用更丰富的结构
public sealed record LodToolExecuteResult {
    public ToolExecutionStatus Status { get; }
    public LevelOfDetailContent Result { get; }
    
    // 新增：交互式选项
    public WizardSpec? InteractiveSpec { get; }  // 如果需要 Micro-Wizard
}
```

这样 **Micro-Wizard 就是工具返回值的一种变体**，不需要单独的机制！

---

**草图 γ：迭代器方案的"编译时展开"替代**

C# 迭代器很优雅，但有个实现细节：迭代器状态机是**编译器生成的**，调试起来比较痛苦。

另一个思路——**声明式规格 + 运行时解释器**：

```csharp
// 不是 yield return，而是声明整个流程
var wizard = new WizardSpec {
    Steps = new Step[] {
        new InfoStep("目标已不存在"),
        new BranchStep {
            Question = "请选择下一步",
            Options = new Dictionary<string, Step[]> {
                ["刷新视图"] = new[] { new ActionStep(RefreshContext) },
                ["查看日志"] = new[] { new ActionStep(ShowLog) },
                ["放弃"] = new[] { new TerminateStep() }
            }
        }
    }
};

return new LodToolExecuteResult(
    ToolExecutionStatus.NeedsRecoveryChoice,
    errorMessage,
    wizard
);
```

优势：
1. **可序列化**——整个 WizardSpec 可以 JSON 化，方便调试/日志/重放
2. **可预览**——UI 层可以"预渲染"整个决策树
3. **可组合**——多个 WizardSpec 可以合并（比如"先做 A 的恢复，再做 B 的恢复"）

劣势：
- 表达力不如命令式 `yield return`（复杂逻辑难写）

**折中方案**：简单场景用声明式 WizardSpec，复杂场景允许 `Func<LlmChoice, Step[]>` 动态生成。

---

**草图 δ：错误作为"注解"而非"替代"**

突发奇想：错误信息能不能**不替换**原有返回值，而是**附加**在旁边？

```
原始返回          +          错误注解
┌──────────────┐      ┌─────────────────────────────┐
│ Result:      │      │ ⚠️ Warning:                 │
│  "攻击成功"   │  +   │ 目标 HP 已归零，下次请用     │
│  damage: 10  │      │ 精确锚点避免空打            │
└──────────────┘      └─────────────────────────────┘
```

这在 **部分成功** 的场景特别有用——操作做了一部分，但有副作用需要告知。

映射到 `LevelOfDetailContent`：
- `Basic`：正常返回内容（"攻击成功"）
- `Detail`：包含警告/建议（"下次请用精确锚点"）

或者显式分离：

```csharp
public sealed record LodToolExecuteResult {
    public ToolExecutionStatus Status { get; }
    public LevelOfDetailContent Result { get; }
    public LevelOfDetailContent? SideNote { get; }  // 新增：附加注解
}
```

---

**草图 ε：`AfterToolExecute` 钩子的魔法**

看 `AgentEngine` 的事件模型，发现 `AfterToolExecute` 事件可以**修改执行结果**：

```csharp
public sealed class AfterToolExecuteEventArgs : EventArgs {
    public LodToolCallResult Result { get; set; }  // 可写！
}
```

这意味着 **错误恢复逻辑可以完全外置**，不需要工具自己处理：

```csharp
engine.AfterToolExecute += (sender, e) => {
    if (e.Result.ExecuteResult.Status == ToolExecutionStatus.Failed) {
        // 统一的错误包装器
        var enrichedResult = ErrorEnricher.Enrich(e.ToolCall, e.Result);
        e.Result = enrichedResult;
    }
};
```

`ErrorEnricher` 可以：
1. 根据错误类型查找对应的 WizardSpec 模板
2. 填充上下文（哪个锚点失败、失败原因、候选恢复路径）
3. 返回增强后的结果

**这是一种"关注点分离"**——工具只负责说"失败了，原因是 X"，错误美化/恢复引导由统一层处理。

---

**草图 ζ：JSON 序列化格式草案**

既然要给 LLM 看，序列化格式很重要。草拟一个：

```json
{
  "status": "needs_recovery",
  "error": {
    "code": "ANCHOR_NOT_FOUND",
    "message": "目标 obj:enemy:23 已不存在",
    "cause": "可能被前序操作移除"
  },
  "recovery": {
    "wizard_type": "choice",
    "tone": "clarifying",
    "prompt": "请选择下一步",
    "options": [
      {
        "label": "刷新视图",
        "action_hint": "refresh_context()",
        "confidence": "high"
      },
      {
        "label": "查看日志",
        "action_hint": "show_log(filter='combat')",
        "confidence": "medium"
      },
      {
        "label": "放弃操作",
        "action_hint": null,
        "confidence": "low"
      }
    ],
    "default_option": 0,
    "timeout_behavior": "use_default"
  },
  "context": {
    "failed_at_step": 2,
    "total_steps": 3,
    "previous_action": "attack(obj:enemy:23)",
    "previous_result": "success"
  }
}
```

关键设计：
- `action_hint`：直接给 LLM "可以粘贴"的代码片段
- `confidence`：引导 LLM 优先考虑高置信度选项
- `default_option` + `timeout_behavior`：如果 LLM "无法选择"的兜底
- `context`：因果链信息（呼应 GeminiAdvisor 的"时间线回溯"）

---

**一个实现层面的担忧：迭代器状态的序列化**

如果用 C# 迭代器实现 Micro-Wizard，有个问题：**迭代器状态难以序列化**。

场景：LLM 执行到 Wizard 第 2 步，然后会话中断（超时/用户取消/网络问题）。下次恢复时，我们需要知道 Wizard 停在哪里。

迭代器方案的状态是"隐藏在编译器生成的状态机里"的，想序列化要么用反射黑魔法，要么手动维护状态。

**声明式 WizardSpec 天然可序列化**——这是个重要优势。

---

**疯狂想法：错误即"对话"**

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│   传统模型：  Tool ──→ Error ──→ LLM (被动接收)                  │
│                                                                 │
│   对话模型：  Tool ←──→ LLM (双向交互)                          │
│               │                                                 │
│               ├── "我遇到问题了"                                │
│               ├── LLM: "什么问题？"                             │
│               ├── "锚点不存在"                                  │
│               ├── LLM: "那刷新一下视图？"                        │
│               ├── "好的，刷新完成，找到 3 个新目标"              │
│               └── LLM: "那攻击第一个"                           │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

这是把 Micro-Wizard 推到极致——错误处理变成**嵌套的对话**。

实现上，可以用"子 Agent"或"嵌套 AgentEngine"：
- 主 Agent 执行主任务
- 遇到复杂错误时，spawn 一个"恢复子 Agent"
- 子 Agent 完成后，控制权返回主 Agent

`Agent.Core` 的 `AgentEngine` 已经有独立实例的设计，理论上可以嵌套。

---

### 我的总结比喻

> 传统错误处理像是"闭卷考试"——你做错了，扣分，没有第二次机会。
> DocUI 的错误处理像是"开卷考试 + 助教在旁边"——你可以查资料，可以问问题，助教还会给你提示。

---

## 畅谈闭幕

**主席总结**：

感谢 Planner、GeminiAdvisor、Investigator 三位的精彩畅谈！

**核心共识**：
1. **错误是分支点，不是死胡同** — GPS 重规划比喻
2. **分层响应** — Hint / Choice / Wizard 三级
3. **情绪调性** — 用 emoji 作为语义信号
4. **声明式优于迭代器** — 可序列化、可组合
5. **与现有实现整合** — 扩展 ToolExecutionStatus + AfterToolExecute 钩子

**产出**：
- 新 Key-Note: [error-feedback.md](../../DocUI/docs/key-notes/error-feedback.md)
- glossary 已更新

---

*畅谈结束于 2025-12-15*

