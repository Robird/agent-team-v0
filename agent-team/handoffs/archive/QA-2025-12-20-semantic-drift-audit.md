# DurableHeap MVP-v2 语义漂移审查报告

> 审查日期: 2025-12-20
> 审查员: QA
> Changefeed: #delta-2025-12-20-doc-compression

---

## 审查结论

✅ **通过 - 无语义漂移**

重构任务 A1/A3/A4/A5/A6/A7/A9 完成后，所有规范性语义完整保留，未发现条款丢失或语义改变。

---

## 审查范围

### 对比文件
| 文件 | 路径 | 行数 |
|------|------|------|
| 原始版本 | `DurableHeap/docs/mvp-design-v2.bak.md` | ~1300 行 |
| 新版本 | `DurableHeap/docs/mvp-design-v2.md` | ~1300 行 |

### 重构任务清单
| ID | 任务 | 状态 |
|----|------|------|
| A1 | §2-§3 移出到 ADR 文件 | ✅ 完成 |
| A3 | 添加条款编号分类定义 | ✅ 完成 |
| A4 | 给 MUST/SHOULD 条款编号 | ✅ 完成 (32 条) |
| A5 | 伪代码移到附录 | ✅ 完成 |
| A6 | Test Vectors 骨架 | ✅ 完成 |
| A7 | 添加 Wire Format ASCII 图 | ✅ 完成 |
| A9 | 合并 Appendix B 到独立文件 | ✅ 完成 |

---

## 1. 规范性条款完整性

### MUST/SHOULD 条款统计
| 类型 | 原文件 | 新文件 | 状态 |
|------|--------|--------|------|
| MUST 条款 | 16 | 16 | ✅ 一致 |

### 条款编号分布（新增）
新文件共 32 条编号条款：

| 前缀 | 数量 | 覆盖范围 |
|------|------|----------|
| `[F-xx]` | 9 | Framing/Format（线格式、对齐、CRC 覆盖） |
| `[A-xx]` | 4 | API（签名、返回值、参数校验） |
| `[S-xx]` | 16 | Semantics（跨 API/格式的语义不变式） |
| `[R-xx]` | 3 | Recovery（崩溃一致性、resync） |

### 关键规范性条款对照

| 条款 | 原文件行号 | 新文件行号 | 编号 | 状态 |
|------|------------|------------|------|------|
| RecordKind 域隔离 | L88 | L103 | [F-01] | ✅ |
| Magic 是 Record Separator | L496 | L431 | [F-02] | ✅ |
| HeadLen == TailLen | L514 | L449 | [F-03] | ✅ |
| HeadLen % 4 == 0 对齐 | L520 | L455 | [F-04] | ✅ |
| Ptr64 null/对齐约束 | 隐含 | L527 | [F-05] | ✅ |
| CRC32C 覆盖范围 | L508 | L443 | [F-06] | ✅ |
| varint canonical 最短编码 | L447 | L338 | [F-07] | ✅ |
| varint 解码错误策略 | L452 | L343 | [F-08] | ✅ |
| ValueType 高 4 bit 预留 | L907 | L842 | [F-09] | ✅ |
| Dirty Set 强引用 | L333 | L224 | [S-01] | ✅ |
| Identity Map key 约束 | L334 | L225 | [S-02] | ✅ |
| Dirty 对象不得被 GC | L335 | L226 | [S-03] | ✅ |
| 新建对象立即加入 Modified Object Set | L354 | L245 | [S-04] | ✅ |
| 术语约束（File Framing vs Record Layout） | L489 | L397 | [S-05] | ✅ |
| Working State 纯净性 | L973 | L908 | [S-06] | ✅ |
| Delete 一致性 | L976 | L911 | [S-07] | ✅ |
| Commit 失败不改内存 | L982 | L917 | [S-08] | ✅ |
| Commit 成功后追平 | L984 | L919 | [S-09] | ✅ |
| 隔离性（深拷贝） | L986 | L921 | [S-10] | ✅ |
| Key 唯一 + 升序 | L989 | L924 | [S-11] | ✅ |
| Canonical Diff（规范化） | L991 | L926 | [S-12] | ✅ |
| 可重放性 | L993 | L928 | [S-13] | ✅ |
| _dirtyKeys 精确性 | L1005 | L940 | [S-14] | ✅ |
| CommitAll 失败不改内存 | L1223 | L1044 | [S-15] | ✅ |
| 可重试 | L1224 | L1045 | [S-16] | ✅ |
| DiscardChanges MUST | L1015 | L950 | [A-01] | ✅ |
| CommitAll() MUST | L1188 | L1009 | [A-02] | ✅ |
| CommitAll(newRoot) SHOULD | L1189 | L1010 | [A-03] | ✅ |
| Dirty Set 可见性 API SHOULD | L1265 | L1086 | [A-04] | ✅ |
| resync 不信任 TailLen | L540 | L475 | [R-01] | ✅ |
| meta 领先 data 继续回扫 | L637 | L572 | [R-02] | ✅ |
| DataTail 截断 | L1281 | L1102 | [R-03] | ✅ |

---

## 2. 关键概念定义一致性

### 核心概念对照

| 概念 | 原文件定义 | 新文件定义 | 状态 |
|------|------------|------------|------|
| **Checkpoint Base** | L51：为截断回放成本而写入的全量状态版本（Base Version），Deprecated: snapshot | L66：同上 | ✅ 一致 |
| **VersionIndex** | L54：Deprecated: EpochMap，每个 Epoch 的 ObjectId → ObjectVersionPtr 映射表 | L69：同上 | ✅ 一致 |
| **Magic-as-Separator** | L496：Magic 是 Record Separator，不属于任何 Record | L431：同上（新增 ASCII 图 L403） | ✅ 一致 |
| **二阶段提交流程** | L1029-1032：Prepare (WritePendingDiff) → Finalize (OnCommitSucceeded) | L962-965：同上（新增 ASCII 流程图 L969-993） | ✅ 一致 |
| **Commit 失败语义** | L1211-1225：三阶段失败表 + MUST 保证 | L1032-1046：同上 | ✅ 一致 |
| **_dirtyKeys 不变式** | L1003-1010：精确追踪所有与 _committed 差异的 key | L938-945：同上 | ✅ 一致 |

---

## 3. Wire Format 规范一致性

### Record 格式定义
| 项目 | 原文件 | 新文件 | 状态 |
|------|--------|--------|------|
| Record Layout | L485 | L393（新增 ASCII 图 L418-426） | ✅ 一致 |
| HeadLen 计算公式 | L516 | L451 | ✅ 一致 |
| 最小长度 ≥12 | L519 | L454 | ✅ 一致 |
| PadLen 计算 | L518 | L453 | ✅ 一致 |

### CRC32C 覆盖范围
- 原文件 L508：`Payload + Pad + TailLen`（不覆盖 `HeadLen(u32)`）
- 新文件 L443：同上，新增 ASCII 图 L424 可视化
- **状态**: ✅ 一致

### VarInt Canonical 编码规则
- 原文件 L447-452：protobuf 风格 base-128，要求 canonical 最短编码，非 canonical 视为格式错误
- 新文件 L338-343：同上，新增 ASCII 编码示例 L346-359
- **状态**: ✅ 一致

---

## 4. API 契约一致性

### CommitAll 行为规范
| 项目 | 原文件 | 新文件 | 状态 |
|------|--------|--------|------|
| API 名称 | CommitAll(newRootId) | 同上 | ✅ |
| 重载：CommitAll() MUST | L1188 | L1009 [A-02] | ✅ |
| 重载：CommitAll(IDurableObject) SHOULD | L1189 | L1010 [A-03] | ✅ |
| 失败语义表格 | L1213-1220 | L1034-1041 | ✅ |
| 二阶段 finalize 约束 | L1227-1233 | L1048-1054 | ✅ |

### LoadObject 行为规范
| 项目 | 原文件 | 新文件 | 状态 |
|------|--------|--------|------|
| workspace + HEAD 语义 | L376-388 | L267-279 | ✅ |
| Identity Map hit 返回同实例 | L382 | L273 | ✅ |
| 对象不存在返回 null | L773-776 | L708-711 | ✅ |
| 版本链解析失败抛异常 | L774 | L709 | ✅ |

### CreateObject/Delete 行为规范
| 项目 | 原文件 | 新文件 | 状态 |
|------|--------|--------|------|
| 新建对象立即加入 Modified Object Set | L354 | L245 [S-04] | ✅ |
| Transient Dirty 定义 | L351 | L242 | ✅ |
| 首次 Commit 创建 Epoch=1 | L1254-1256 | L1075-1077 | ✅ |

---

## 5. 数值/常量一致性

| 常量 | 原文件 | 新文件 | 状态 |
|------|--------|--------|------|
| Magic (data) | DHD3 | DHD3 | ✅ |
| Magic (meta) | DHM3 | DHM3 | ✅ |
| 对齐要求 | 4B | 4B | ✅ |
| Ptr64 null | 0 | 0 | ✅ |
| 空仓库 Epoch | 0 | 0 | ✅ |
| 空仓库 NextObjectId | 1 | 1 | ✅ |
| ObjectId 类型 | uint64 | uint64 | ✅ |
| RecordKind (ObjectVersionRecord) | 0x01 | 0x01 | ✅ |
| RecordKind (MetaCommitRecord) | 0x01 | 0x01 | ✅ |
| ObjectKind (Dict) | 1 | 1 | ✅ |
| Val_Null | 0x0 | 0x0 | ✅ |
| Val_Tombstone | 0x1 | 0x1 | ✅ |
| Val_ObjRef | 0x2 | 0x2 | ✅ |
| Val_VarInt | 0x3 | 0x3 | ✅ |
| Val_Ptr64 | 0x4 | 0x4 | ✅ |
| DictCheckpointEveryNVersions 建议 | 64 | 64 | ✅ |

---

## 6. 结构变更确认

### 章节编号映射
| 原文件 | 新文件 | 内容 |
|--------|--------|------|
| §2 单选题 | → decisions/mvp-v2-decisions.md | 决策选项移出 |
| §3 决策表 | → decisions/mvp-v2-decisions.md | 决策表移出 |
| §4 设计正文 | §3 设计正文 | 章节编号调整 |
| §5 Open Questions | §4 Open Questions | 章节编号调整 |
| §6 实现建议 | §5 实现建议 | 章节编号调整 |
| — | §2 设计决策（索引） | 新增摘要索引 |
| — | Appendix A | 伪代码移入 |
| — | Appendix B | Test Vectors 引用 |

### 新增内容
1. **条款编号系统**：32 条编号条款（[F-xx], [A-xx], [S-xx], [R-xx]）
2. **ASCII 图**：
   - RBF File Structure (L403-412)
   - Record Layout (L418-426)
   - VarInt Encoding (L346-359)
   - Two-Phase Commit Flow (L969-993)
3. **附录结构**：
   - Appendix A: Reference Implementation Notes (L1130+)
   - Appendix B: Test Vectors 引用 (L1294+)

---

## 7. 确认保持一致的关键项

✅ 所有 16 条 MUST 规范性条款完整保留并编号  
✅ Magic 值（DHD3/DHM3）一致  
✅ 4B 对齐要求一致  
✅ CRC32C 覆盖范围（`Payload + Pad + TailLen`）一致  
✅ VarInt canonical 最短编码规则一致  
✅ 二阶段提交语义（WritePendingDiff → OnCommitSucceeded）一致  
✅ Commit 失败语义（不改内存、可重试）一致  
✅ _dirtyKeys 精确性不变式一致  
✅ LoadObject/CommitAll API 契约一致  
✅ ValueType 枚举值（0x0-0x4）一致  
✅ 空仓库初始状态（Epoch=0, NextObjectId=1）一致  

---

## 8. 建议

1. **保持 bak 文件**：建议在实现完成前保留 `mvp-design-v2.bak.md` 作为参照
2. **条款编号文档**：建议在 wiki 中维护条款编号 → 测试用例的映射表
3. **ADR 文件索引**：建议在主文档的"设计决策"索引中添加到 ADR 文件的直接链接

---

## 审查方法

1. **全文搜索对比**：`grep -n` 提取关键术语和条款
2. **MUST/SHOULD 计数**：确认规范性条款数量一致
3. **概念定义对照**：逐项检查核心概念的定义文本
4. **数值常量验证**：枚举所有数值常量并对比
5. **章节结构映射**：确认内容移动后的可追溯性

---

**Changefeed Anchor**: `#delta-2025-12-20-doc-compression`
