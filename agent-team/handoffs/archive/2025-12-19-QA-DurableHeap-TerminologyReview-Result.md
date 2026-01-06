# DurableHeap MVP v2 术语修订 QA 验证报告

> **日期**: 2025-12-19
> **验证者**: QA
> **Changefeed Anchor**: `#delta-2025-12-19-terminology-review`

## 验证范围

验证 `/repos/focus/DurableHeap/docs/mvp-design-v2.md` 是否符合 `2025-12-19-secret-base-durableheap-terminology-review.md` 畅谈会共识。

## 检查结果

### 1. 术语表检查 ✅

| 检查项 | 结果 | 说明 |
|--------|------|------|
| 文档开头有 Glossary | ✅ PASS | 第16-73行有完整的"术语表（Glossary）"章节 |
| 术语表包含核心术语 | ✅ PASS | 包含状态与差分、版本链、标识与指针、提交与HEAD、载入与缓存、对象级API共6大类 |

### 2. 术语替换检查

| 旧术语 | 新术语 | 结果 | 说明 |
|--------|--------|------|------|
| Baseline（单独使用）| Committed State | ✅ PASS | 术语表第26行明确标注 Deprecated |
| EpochMap | VersionIndex | ✅ PASS | 术语表第36行明确标注 Deprecated |
| snapshot（版本链语境）| Checkpoint Version | ✅ PASS | 术语表第35行明确标注 Deprecated |
| Ptr64（概念层）| <deleted-place-holder> | ✅ PASS | 术语表第43行声明概念层用 <deleted-place-holder>，编码层保留 Ptr64 |
| Resolve（外部 API）| LoadObject | ✅ PASS | 术语表第60行明确标注 Deprecated，允许内部使用 |
| On-Disk Diff | DiffPayload | ⚠️ PARTIAL | 术语表已标注，但正文4.4.1仍有3处使用 |
| EpochRecord（MVP）| Commit Record | ✅ PASS | 术语表第52行明确标注 Deprecated |
| head/Head | HEAD | ⚠️ PARTIAL | 第289行仍有小写 `head` |

### 3. 代码修订检查

| 检查项 | 结果 | 说明 |
|--------|------|------|
| 4.4.4 伪代码 `Commit()` → `FlushToWriter()` | ✅ PASS | 第865行方法名已改为 `FlushToWriter(IRecordWriter writer)` |
| 4.4.3 实现建议中的 `Commit()` | ⚠️ INFO | 第825行提到 `Commit()`，但这是指 Heap 级 API，非对象级，属于正确用法 |

### 4. 定义补充检查

| 检查项 | 结果 | 说明 |
|--------|------|------|
| Identity Map 正式定义 | ✅ PASS | 术语表第58行 + 正文第273行有完整定义 |
| Dirty Set 正式定义 | ✅ PASS | 术语表第59行 + 正文第275行有完整定义 |

### 5. 一致性检查

| 检查项 | 结果 | 说明 |
|--------|------|------|
| 遗漏的旧术语 | ⚠️ PARTIAL | 见下方详情 |
| 新术语使用一致性 | ✅ PASS | HEAD/VersionIndex/Checkpoint Version/LoadObject 等新术语在正文中一致使用 |

## 发现的问题

### P1: 正文遗漏术语替换

1. **第289行**: `head` 未统一为 `HEAD`
   ```markdown
   > 备注：在 meta file 方案下，head 由最后一条 `MetaCommitRecord` 给出
   ```
   **修复建议**: 改为 `HEAD`

2. **第146行**: 节标题 `### 2.4 对象版本与增量（state diff）`
   **修复建议**: 改为 `### 2.4 对象版本与增量（DiffPayload）`

3. **第652行**: `对 Dict：ChangeSet 采用"state diff 形态"。`
   **修复建议**: 改为 `对 Dict：ChangeSet 采用"DiffPayload 形态"。`

4. **第672/696/709行**: 4.4.1 语义层次定义中仍使用 `On-Disk Diff` 作为第4层定义名
   **评估**: 此处作为"概念名 → 实现映射"的说明，保留有合理性；但根据共识应改为 `DiffPayload（序列化差分）`

### P2: 历史决策记录（可接受）

以下位置保留旧术语属于"历史决策记录"，不做修改：
- 第114/116行: Q3 标题 `Resolve 语义`
- 第121/124-126行: Q4/Q5 标题涉及 `EpochRecordPtr`/`EpochRecord`

## 检查结论

**结果: ⚠️ CONDITIONAL PASS**

术语表完整且规范，大部分正文已按共识修订，但仍有 4 处正文遗漏需要修复。

### 必须修复（P1）

| 行号 | 问题 | 修复方案 |
|------|------|---------|
| 289 | `head` → `HEAD` | 替换为大写 |
| 146 | `state diff` 节标题 | 改为 `DiffPayload` |
| 652 | `state diff` 描述 | 改为 `DiffPayload` |
| 672/696/709 | `On-Disk Diff` 定义名 | 改为 `DiffPayload（序列化差分）` |

### 建议评审

第672-709行的"4.4.1 语义层次定义"区块完整性需要 Implementer 确认：该区块是否应随术语表一起更新，还是保留作为历史"概念 → 实现"的对照说明。

---

> **QA 签章**: 验证完成，条件通过待修复
