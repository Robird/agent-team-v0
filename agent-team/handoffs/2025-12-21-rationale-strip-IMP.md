# [P0 Rationale Stripping] Implementation Result

## 实现摘要

完成对 `mvp-design-v2.md` 的 Rationale Stripping 任务：从 1307 行减少到 1225 行（-82 行，-6.3%），同时将有价值的设计理由迁移到 ADR 文件 `mvp-v2-decisions.md`。

## 文件变更

| 文件 | 变更 | 行数变化 |
|------|------|----------|
| `DurableHeap/docs/mvp-design-v2.md` | 删除/精简 Rationale | 1307 → 1225 (-82) |
| `DurableHeap/docs/decisions/mvp-v2-decisions.md` | 新增 §4 Rationale Archive | 153 → 212 (+59) |

## 删除内容清单

### §3.2 磁盘布局
1. ✓ mmap/zero-copy 动机解释（"v2 不再以 mmap/zero-copy 为目标：v2 选择 varint + 全量 materialize..."）
2. ✓ varint 重要澄清段落（"引入 varint 并不影响... 代价是..."）
3. ✓ varint mmap 备注（"> 备注：这等价于承诺'对象字段一次性 materialize'..."）
4. ✓ varint/framing 注意事项（"> 注意：即使 payload 采用 varint，也不改变 framing header..."）
5. ✓ fast-path/resync 统一说明
6. ✓ Magic 加速 resync 实现提示
7. ✓ 写入目标说明（"- 目标：writer 可以'先顺序写 payload，最后回填头部 HeadLen'"）
8. ✓ Magic 设计收益段落
9. ✓ All Magic 备注
10. ✓ RecordKind payload 备注

### §3.2.3 Meta 文件
11. ✓ Meta 文件章节开头精简（删除 Q16/Q17 引用和 mmap 约束说明）
12. ✓ meta 回扫实现提示
13. ✓ Q16 引用删除

### §3.2.4 VersionIndex
14. ✓ Checkpoint Base 说明和实现提示
15. ✓ 历史遍历说明

### §3.3 读路径
16. ✓ LoadObject 返回 null 设计说明
17. ✓ 缓存建议段落
18. ✓ WeakReference/Dirty Set 说明精简

### §3.4 写路径
19. ✓ 方案 A/B 历史说明（tombstone vs 哨兵对象）
20. ✓ tombstone 权衡讨论

### §3.4.5 CommitAll
21. ✓ CommitAll 步骤 1 精简（删除目的/语义详解/代价分析）
22. ✓ commit point 注意事项
23. ✓ Dirty Set API 用途注意事项
24. ✓ 刷盘顺序崩溃说明

## ADR 迁移内容

新增 `mvp-v2-decisions.md` 第 4 节：

- §4.1 varint 与 mmap 的取舍
- §4.2 Magic 作为 Record Separator 的设计收益
- §4.3 tombstone vs Upserts + Deletes(keys) 的权衡
- §4.4 LoadObject 返回 null 的设计理由
- §4.5 Dirty Set 存在的必要性
- §4.6 CommitAll 提交全部 Dirty 对象的理由
- §4.7 刷盘顺序的崩溃安全性

## 判断规则应用

| 类别 | 判断 | 处理 |
|------|------|------|
| "为什么选择 varint + materialize" | 删除后实现者仍可正确实现 | 删除，迁移到 ADR |
| "mmap 收益下降" | 纯解释性，不影响实现 | 删除，迁移到 ADR |
| "tombstone vs Deletes" 权衡 | 实现者只需知道选了什么，不需知道为什么 | 删除，迁移到 ADR |
| Dirty Set 规则 | 契约性条款，影响正确性 | **保留** |
| CommitAll 步骤定义 | 操作规程，必须保留 | **保留** |
| 32 个规范子句 [F-01~R-03] | MUST/SHOULD 条款 | **全部保留** |

## 边界案例

1. **"实现提示（非规范）"**：删除，因为标注"非规范"表明不影响正确性
2. **"说明：xxx"**：删除，除非内容是错误处理语义
3. **"注意：xxx"**：删除，除非内容是边界条件约束
4. **"目的：xxx"**：删除，因为是动机解释
5. **"代价与接受理由"**：删除，因为是权衡取舍解释

## 统计

- 原始行数：1307 行
- 最终行数：1225 行
- 删除行数：82 行（6.3%）
- ADR 新增：59 行
- 净内容转移：约 60% 的删除内容有价值，迁移到 ADR

## 验证

```bash
# 检查规范子句完整性
grep -c '\[F-\|A-\|S-\|R-' mvp-design-v2.md
# 预期: 32 个子句全部保留
```

## Changefeed Anchor

`#delta-2025-12-21-rationale-strip`
