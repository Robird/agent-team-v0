# P1 消灭双写 Implementation Result

## 实现摘要

根据畅谈会共识（P1 任务），消灭 `mvp-design-v2.md` 中的双写问题。将 File Framing/Record Layout 的冗余文字描述替换为 EBNF 语法（-66 行，-5.4%）。

## 文件变更

- `DurableHeap/docs/mvp-design-v2.md` — 消灭双写，EBNF 转换

## 双写审计结果

| 图表 | 附近文字 | 是否双写 | 处理 |
|------|----------|----------|------|
| VarInt Encoding | [F-07]/[F-08] 条款 | **否**（互补） | 保留 |
| RBF File Structure | 分层定义文字 | **是** | 替换为 EBNF |
| Record Layout | 分层定义文字 | **是** | 替换为 EBNF |
| Two-Phase Commit Flow | 二阶段提交表格 | **否**（互补） | 保留 |

## 源码对齐说明

| 原内容 | 新内容 | 行数变化 |
|--------|--------|----------|
| 分层定义文字（13行）+ 2 ASCII 图（22行）= 35行 | EBNF（13行）+ 术语约束（1行）= 14行 | -21 |
| F-02/F-03/F-04/F-06 详细描述 | 精简条款定义 | -18 |
| Meta 文件 framing 描述 | 引用 §3.2.1 + Magic 值 | -10 |
| Data 文件 Magic 单独段落 | 已在 EBNF 中定义 | -8 |
| RecordKind/约定分散描述 | 合并为 2 行 | -9 |

## 关键变更：EBNF 替换 ASCII 图表

```ebnf
(* File Framing: Magic-separated log *)
File   := Magic (Record Magic)*
Magic  := 4 bytes ("DHD3" for data, "DHM3" for meta)

(* Record Layout *)
Record := HeadLen Payload Pad TailLen CRC32C
HeadLen := u32 LE        (* == TailLen, record total bytes *)
Payload := N bytes
Pad     := 0..3 bytes    (* align to 4B *)
TailLen := u32 LE        (* == HeadLen *)
CRC32C  := u32 LE        (* covers Payload + Pad + TailLen *)
```

**收益**：
- 信息密度提升：13 行 EBNF 替代 35 行（图表+文字）
- LLM 友好：线性语法比 2D ASCII 图更易解析
- 消除维护风险：不再有图表与文字不一致的可能

## 测试结果

- Targeted: N/A（文档修订，无代码测试）
- Full: N/A
- 文档格式验证：✅ Markdown 语法正确，EBNF 高亮正常

## 已知差异

- 删除了 2 个 ASCII 图表（RBF File Structure、Record Layout）
- 这些图表的信息已完全包含在 EBNF 中，且 EBNF 更精确
- 保留了 VarInt 图（编码示例）和 Two-Phase Commit 图（流程可视化）

## 统计

| 指标 | P0 后 | P1 后 | 变化 |
|------|-------|-------|------|
| 主文档行数 | 1225 | 1159 | **-66 行** |
| 累计减少 | -82 行 | -148 行 | -12.0% (vs 原始 1307) |

## 遗留问题

- P2（补充 EBNF/Format String）已部分完成——File Framing 已转换
- DurableDict diff payload 格式可考虑后续添加 EBNF
- P3（映射表生成化）待测试基础设施建好后执行

## Changefeed Anchor

`#delta-2025-12-21-p1-double-write`
