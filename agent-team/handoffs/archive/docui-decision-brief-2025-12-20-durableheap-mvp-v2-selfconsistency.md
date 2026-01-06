# 决策摘要：DurableHeap MVP v2 设计自洽性审阅

> **日期**：2025-12-20
> **会议类型**：秘密基地畅谈 (Hideout Jam Session)
> **参与者**：DocUIClaude、DocUIGemini、DocUIGPT
> **主持人**：刘德智 / SageWeaver
> 
> **✅ 状态：已执行（2025-12-20 监护人批准）**

---

## 背景（2-3句）

DurableHeap MVP v2 设计文档 (`DurableHeap/docs/mvp-design-v2.md`) 是一份约 1000 行的技术规范，尚无实现和数据。三位 Specialist 从概念框架、UX/DX、规范核查三个视角进行了三轮畅谈式审阅，目标是确保文档自洽、术语一致、低冗余。

---

## 问题清单

| # | 问题 | 严重度 | 发现者 | 建议方案 |
|---|------|--------|--------|----------|
| 1 | `[S-05]` 编号跳号未标 Deprecated | P0 | GPT | 显式标注 `[S-05] Deprecated` |
| 2 | <deleted-place-holder> 术语表重复定义 | P0 | GPT | 合并为单条定义 |
| 3 | ObjectId 保留区定义分散 | P0 | Claude+GPT | Glossary 新增 Well-Known ObjectId 条目 |
| 4 | `DurableDict<...>` 泛型写法混用 | P1 | GPT+Claude | 禁用泛型写法，统一描述 |
| 5 | Shallow Materialization 概念隐性 | P1 | Gemini+GPT | 入 Glossary + cross-ref |
| 6 | VersionIndex Bootstrap 叙事断裂 | P1 | Gemini+Claude | §3.2.4 加"引导扇区"说明 |
| 7 | "(MVP 固定)→编号" 元规则未闭环 | P1 | GPT | 放宽元规则：仅规范性约束需编号 |
| 8 | 僵尸对象 UX 风险 | P2 | Gemini | 加警告 + 建议 IsDetached 属性 |
| 9 | 状态转换表缺少脚注 | P2 | Claude | 加脚注说明"Commit 失败=无变化" |
| 10 | 状态机可视化缺失 | P2 | Gemini | 补充 Mermaid 状态图 |

---

## 推荐行动

### 立即执行（P0）
- [x] 在条款表中显式标注 `[S-05] Deprecated`
- [x] 合并术语表中的两条 <deleted-place-holder> 定义
- [x] 术语表新增 **Well-Known ObjectId** 条目，集中定义 `0=VersionIndex`、`0..15` 保留区、`NextObjectId=16`

### 建议执行（P1）
- [x] 正文中禁用 `DurableDict<...>` 泛型写法，统一为描述性语句
- [x] 术语表新增 **Shallow Materialization** 并在 §3.1.0、§3.1.3 交叉引用
- [x] §3.2.4 开头加"Bootstrap 入口"说明，打破概念死锁
- [x] 修改规范语言章节：仅规范性约束的 (MVP 固定) 需要条款编号

### 后续优化（P2）
- [x] §3.1.0.1 加僵尸对象警告 + ~~建议 IsDetached~~ `DurableObjectState` 枚举（监护人建议升级）
- [x] 状态转换表加脚注说明
- [x] 补充 Mermaid 状态机图

---

## 共识达成情况

| 参与者 | 签署状态 | 态度 |
|--------|----------|------|
| DocUIClaude | ✅ 已签署 | 全部同意 |
| DocUIGemini | ✅ 已签署 | 全部同意 |
| DocUIGPT | ✅ 已签署 | 全部同意 + 补充 P1-7 |

**共识率**：100% (3/3)

---

## 决策选项

- [x] **全部批准**：按清单执行 P0 + P1 修订，P2 纳入 backlog ← **监护人选择**
- [ ] ~~仅 P0：仅执行 P0 修订，P1/P2 后续再议~~
- [ ] ~~需要更多信息：请说明需要补充的内容~~
- [ ] ~~否决：请说明理由~~

---

**监护人批示**：✅ 全部批准。采纳 `IsDetached` → `DurableObjectState` enum 建议。

---

## 会议记录链接

完整会议记录：[2025-12-20-secret-base-durableheap-mvp-v2-selfconsistency-jam.md](../meeting/2025-12-20-secret-base-durableheap-mvp-v2-selfconsistency-jam.md)

---

*决策摘要由 DocUI 规范起草委员会执行委员会主席准备*
