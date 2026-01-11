# Implementer 项目知识归档（2026-01）

> **归档原因**：从 index.md 外迁的项目细节知识，保持 index.md 的仪表盘功能
> **创建日期**：2026-01-11
> **来源**：index.md 记忆维护

---

## DocGraph 扩展点详情

### v0.2 Wish Instance Directory 布局迁移

| 变更 | 说明 |
|:-----|:------|
| DefaultWishDirectories | 从 `["wishes/active", "wishes/biding", "wishes/completed", "wish"]` 变为 `["wish"]` |
| Wish 识别规则 | v0.2 只识别 `wish/**/wish.md`，不再扫描旧布局 |
| Status 字段 | 从目录名推导改为从 frontmatter `status` 字段读取 |
| DocId 字段 | 从文件名推导改为从 frontmatter `wishId` 字段读取 |

### 创建新 Wish 实例目录

| 位置 | 修改内容 |
|:-----|:---------|
| `wish/W-XXXX-slug/wish.md` | 主 wish 文件，frontmatter 含 wishId/title/status/produce |
| `wish/W-XXXX-slug/project-status/{goals,issues,snapshot}.md` | 状态寄存器，produce_by 指向 wish.md |
| `wish/W-XXXX-slug/artifacts/{Resolve,Shape,Rule,Plan,Craft}.md` | 分层产物，produce_by 指向 wish.md |
| 外部产物文档 | 在 produce_by 数组中追加新 wish.md 路径 |

### OutputPreflight 预检机制

| 校验规则 | 说明 |
|:---------|:-----|
| 路径冲突检测 | 用 `HashSet<string>` 收集规范化后的所有输出路径 |
| 安全校验 | 拒绝绝对路径、`..` 穿越、归一化后不在 workspace 内 |
| 空 Dictionary 语义 | 等价于 null，回退单输出模式 |

### IssueAggregator Phase 2

| 扩展点 | 说明 |
|:-------|:-----|
| Issue 类扩展 | 新增 `Id`, `SourceNode` 字段 |
| 双格式解析 | 字符串 `"X-ID: 描述"` + 对象 `{description, ...}` |
| 两层输出 | 全局 `docs/issues.gen.md` + Wish 级别 `project-status/issues.md` |
| Wish 归属 | 优先 `ProducedBy`，回退路径提取 |

### TwoTierAggregatorBase 基类抽取

| 基类方法 | 说明 |
|:---------|:-----|
| `CollectAllItems()` | 从所有文档收集条目 |
| `GetOwningWishPath()` | 推导条目所属 Wish（ProducedBy 优先） |
| `GenerateGlobalOutput()` | 全局输出（按源文件分组子弹列表） |
| `GenerateWishOutput()` | Wish 级别输出 |

### 过滤 Abandoned Wish

| 位置 | 修改内容 |
|:-----|:---------|
| `DocumentGraphBuilder.cs` Build 方法 (~L99) | 检查 `node.Status?.Equals("abandoned", ...)` 后跳过 |
| `DocumentGraphBuilderTests.cs` | 新增 `Build_ShouldFilterOutAbandonedWishes` 测试 |

- **Build 阶段过滤**：因为 RootNodes 从 allNodes 过滤得出，必须在 Build 阶段排除
- **闭包影响**：Abandoned Wish 的 produce 路径不入 pendingPaths 队列

### 设计决策：输出格式重构

- 表格 → 按源文件分组的子弹列表
- 全局输出标题：`# 问题汇总`，用 `## \`filepath\`` 分组
- ID 必填：字符串格式须匹配 `^([A-Z]-[A-Z0-9-]+):\s*(.+)$`

---

## StateJournal 架构详情

### 架构分层图

```
┌─────────────────────────────────────────┐
│  Layer 1: StateJournal 语义              │
│  (mvp-design-v2.md)                     │
│  - ObjectVersionRecord / MetaCommitRecord│
│  - DiffPayload 编码                      │
│  - 二阶段提交语义                         │
└────────────────┬────────────────────────┘
                 │ rbf-interface.md
                 │ (对接契约)
┌────────────────┴────────────────────────┐
│  Layer 0: RBF 二进制格式                  │
│  (rbf-format.md)                        │
│  - Frame 结构 (HeadLen/Payload/Pad/CRC)  │
│  - Magic-as-Separator                    │
│  - 逆向扫描 / Resync                     │
└─────────────────────────────────────────┘
```

### 关键术语表

| 术语 | 定义 |
|------|------|
| RBF | Reversible Binary Framing（支持 backward scan / resync）|
| FrameTag | Payload[0]，唯一顶层判别器（0x00=Pad, 0x01=ObjVer, 0x02=MetaCommit）|
| VersionIndex | ObjectId → ObjectVersionPtr 映射（HEAD 时的快照）|
| DiffPayload | On-disk 差分编码（key-value upserts + tombstones）|
| Working State | `_current` 字典，用户直接操作 |
| Committed State | `_committed` 字典，上次 commit 成功后的快照 |
| Genesis Base | 新建对象的首个版本（PrevVersionPtr=0，from-empty diff）|
| Checkpoint Base | 截断版本链的全量状态快照 |

### 条款统计

- rbf-format.md：24 条（19 F-xxx + 5 R-xxx）
- rbf-interface.md：5 条（F-xxx）
- mvp-design-v2.md：43 条（13 F + 4 A + 22 S + 4 R）

### 更名历史

- `DurableHeap` → `StateJournal`（2025-12-21）
- `ELOG` → `RBF`（2025-12-22）
- `DHD3/DHM3` → `RBF1`（2025-12-22）
- `RecordKind` 域隔离 → `FrameTag` 统一判别器（2025-12-22）

---

## Atelia.Data / SizedPtr 详情

### 代码位置

| 位置 | 说明 |
|:-----|:-----|
| `src/Data/SizedPtr.cs` | 38:26 bit 分配的 Fat Pointer 实现 |
| `tests/Data.Tests/SizedPtrTests.cs` | 50 个测试：roundtrip、对齐、边界、FromPacked、Contains |
| `tests/Data.Tests/TestHelpers.cs` | `CollectingWriter`（IBufferWriter + IByteSink 双接口） |

### 测试文件命名约定

- 接口级测试：`{InterfaceName}Tests.cs`（如 `ReservableWriterTests.cs`）
- 实现级测试：`{ClassName}Tests.cs`（如 `ChunkedReservableWriterP1Tests.cs`）
- 负面路径测试：`{InterfaceName}NegativeTests.cs`

### W-0006 文档修订

| 文件 | 修改项 |
|:-----|:-------|
| `rbf-interface.md` | §2.3 <deleted-place-holder>→SizedPtr+NullPtr、接口签名×4、示例×2、条款索引×3 |
| `rbf-format.md` | §1术语、§7重写（SizedPtr Wire Format）、§8 DataTail更新、条款索引×1 |

### 条款更名

`[F-<deleted-place-holder>-*]` → `[F-SIZEDPTR-*]` / `[F-RBF-NULLPTR]`；`RbfFrame.Address` → `RbfFrame.Ptr`
