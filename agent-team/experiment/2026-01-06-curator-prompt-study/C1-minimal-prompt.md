# C1: 极简提示词

## 输入提示词

```
审阅以下两个文档的一致性：

- `/repos/focus/atelia/docs/Rbf/rbf-interface.md`
- `/repos/focus/atelia/docs/Rbf/rbf-format.md`

列出发现的问题。
```

**字数**：47 字（与 Craftsman E1 相同）

## 输出反馈

### 问题清单（6 个）

| 严重性 | 问题 | 
|:-------|:-----|
| 🔴 高 | HeadLen 下限矛盾（16 vs 20）|
| 🔴 高 | Reverse Scan 缺少 FrameStatus 校验 |
| 🟡 中 | docId 重复 |
| 🟡 中 | SizedPtr 范围与粒度不一致 |
| 🟡 中 | 章节编号缺 §6 |
| 🟢 低 | TryReadAt 校验语义未明确 |

## 分析

### 有价值问题

| 问题 | 价值判断 | 理由 |
|:-----|:---------|:-----|
| HeadLen 下限矛盾 | ✅ 高价值 | 会导致实现错误 |
| 缺少 FrameStatus 校验 | ✅ 高价值 | 会导致漏检非法帧 |
| docId 重复 | ✅ 有价值 | 会导致索引冲突 |
| SizedPtr 表述不一致 | ✅ 有价值 | 会导致实现歧义 |
| 章节编号 | ⚠️ 中等 | 格式问题 |
| TryReadAt 语义 | ✅ 有价值 | 实现者需要知道 |

### 统计

- 总问题数：6
- 高价值问题：4（HeadLen、FrameStatus、docId、SizedPtr）
- 中等价值：1（章节编号）
- 低价值：1（TryReadAt）
- 伪问题：0
- **价值比**：83%（严格）/ 100%（含中等）

## 对比 Craftsman E1

| 维度 | Craftsman E1 | Curator C1 |
|:-----|:-------------|:-----------|
| 问题数 | 6 | 6 |
| 伪问题 | 2 | 0 |
| 价值比 | 33% | 83% |
| 输出风格 | 分 Sev2/Sev3 | 用 🔴🟡🟢 |
| 有汇总表 | ❌ | ✅ |
| 有修复建议 | 部分 | ✅ 全部 |

## 观察

1. **Curator 默认不产生伪问题**：相同提示词，Craftsman 有 2 个伪问题，Curator 0 个
2. **输出更结构化**：Curator 自动生成汇总表
3. **修复建议更具体**：每个问题都有明确的修改建议
4. **发现相同核心问题**：HeadLen 下限、SizedPtr 粒度、docId 重复
5. **额外发现**：FrameStatus 校验缺失（Craftsman E1 没发现）

## 关键发现

**Curator 的默认行为比 Craftsman 更"工程化"**

即使用极简提示词，Curator 也：
- 不追问"证据链"
- 聚焦技术细节
- 提供具体修复建议
- 自动分类严重性

这可能与 Curator 的 Agent 设定有关。
