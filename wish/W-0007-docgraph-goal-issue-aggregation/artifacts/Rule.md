---
docId: "W-0007-Rule"
title: "W-0007 Rule-Tier"
produce_by:
  - "wish/W-0007-docgraph-goal-issue-aggregation/wish.md"
---

# W-0007 Rule-Tier: 规范条款

## 1. ID 格式约束

（已在 Shape-Tier 定义，此处引用）

- **格式**：`{Tier前缀}-{关键词}`
- **正则**：`^[A-Z]-[A-Z0-9-]+$`
- **唯一性**：Wish 内 active 条目不重复

## 2. produce 关系语义

### [R-PRODUCE-001] 派生文件不作为产物

**条款**：自动生成的派生视图文件（`.gen.md`）**不应**记录在 Wish 的 `produce` 字段中。

**理由**：
1. **可再生性**：派生视图可以随时从源数据重新生成，不是设计/实现本体
2. **语义清晰**：`produce` 应该表达"需要人工维护、承载决策/契约/实现"的产物
3. **减少噪声**：避免 produce 图包含大量自动生成节点，干扰对设计产物的理解
4. **避免副作用**：防止验证逻辑尝试"修复"不存在的派生文件

**适用范围**：
- `.gen.md` 扩展名的所有文件
- 由 DocGraph 或其他工具自动生成的汇总/报告文件
- 存储在 `wish-panels/` 的派生视图

**示例**：

❌ **不合规**：
```yaml
produce:
  - "wish/W-XXXX/project-status/goals.gen.md"  # 派生文件
  - "wish/W-XXXX/project-status/issues.gen.md"  # 派生文件
```

✅ **合规**：
```yaml
produce:
  - "wish/W-XXXX/artifacts/Resolve.md"  # 人工维护的设计产物
  - "wish/W-XXXX/artifacts/Shape.md"    # 人工维护的设计产物
```

**可追溯性**：派生视图可以在 Wish 正文中链接引用，但不使用 `produce` 关系表达。

---

**状态**：🟢 已定义
**更新**：2026-01-05
