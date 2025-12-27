# Recipe: 测试文件拆分

> **用途**：将大型单元测试文件（>1000 行）拆分为多个专项测试文件  
> **适用角色**：Implementer  
> **难度**：中等  
> **预计耗时**：30-60 分钟（取决于文件大小）

## 何时使用

当测试文件出现以下情况时，考虑使用本配方：

- ✅ 文件行数超过 1000 行
- ✅ 包含多个功能领域的测试（如基础操作、状态管理、序列化等）
- ✅ 编辑和阅读时需要频繁滚动
- ✅ 有清晰的功能分组（region 或注释）

## 前置条件

- 目标测试文件已存在且可编译
- 所有测试通过
- 了解代码库的测试命名规范

## 配方步骤

### 第 1 步：分析现状

**目标**：理解测试文件的结构和功能分组

**操作**：
```
1. 读取完整的测试文件
2. 识别 region 标记（#region xxx）
3. 统计各 region 的测试数量
4. 记录辅助方法（如 CreateXxx、ToXxx 等）
```

**输出示例**：
```
原始文件：DurableDictTests.cs（1860 行，84 个测试）

功能分组：
- 构造函数测试（2 tests）
- Set/Get 基础测试（7 tests）
- Remove 测试（4 tests）
- Working State 测试（3 tests）
- 状态转换测试（4 tests）
- Detached 状态测试（12 tests）
- WritePendingDiff 测试（10 tests）
- OnCommitSucceeded 测试（8 tests）
- DiscardChanges 测试（9 tests）
- _dirtyKeys 追踪测试（12 tests）
- Lazy Loading 测试（8 tests）
- 复杂场景测试（5 tests）

辅助方法：
- ToObjectDict<T>
- CreateDetachedDict<T>
```

### 第 2 步：规划拆分方案

**目标**：确定目标文件结构和测试分配

**原则**：
1. **功能内聚**：相关测试放在一起
2. **大小适中**：每个文件 200-500 行
3. **独立编译**：每个文件可以单独编译
4. **辅助方法就近**：辅助方法跟随使用它的测试

**拆分策略**：

| 策略 | 说明 | 示例 |
|------|------|------|
| 按功能领域 | 基础操作、状态管理、序列化等 | BasicTests, StateTests |
| 按测试阶段 | 构造、读取、修改、提交等 | ConstructorTests, CommitTests |
| 按异常场景 | 正常流程 vs 异常场景 | HappyPathTests, ErrorTests |
| 按复杂度 | 简单场景 vs 复杂场景 | BasicTests, IntegrationTests |

**输出示例**：
```markdown
## 拆分方案（8 个文件）

1. **DurableDictBasicTests.cs** (16 tests)
   - 构造函数测试（2）
   - Set/Get 基础测试（7）
   - Remove 测试（4）
   - Working State 测试（3）
   - 辅助方法：ToObjectDict

2. **DurableDictStateTests.cs** (4 tests)
   - 状态转换测试
   - Clean ↔ PersistentDirty ↔ TransientDirty

3. **DurableDictDetachedTests.cs** (12 tests)
   - Detached 状态下各种操作的异常测试
   - 辅助方法：CreateDetachedDict

4. **DurableDictSerializationTests.cs** (10 tests)
   - WritePendingDiff 序列化测试

5. **DurableDictCommitTests.cs** (17 tests)
   - OnCommitSucceeded（8）
   - DiscardChanges（9）

6. **DurableDictDirtyTrackingTests.cs** (12 tests)
   - _dirtyKeys 精确追踪测试

7. **DurableDictLazyLoadingTests.cs** (8 tests)
   - Lazy Loading 测试

8. **DurableDictTests.cs** (5 tests，保留原文件名)
   - 复杂场景和综合测试
```

### 第 3 步：执行拆分

**目标**：通过 runSubagent 创建新文件并移动测试

**关键约束**：
- ⚠️ **SubAgent 只返回最后一轮输出**：必须先完成所有工具调用，最后再汇报
- ✅ **工具调用优先原则**：新建文件 + 移动测试 + 删除原代码 → 最后简单汇报问题或建议
- 💡 **鼓励发现问题**：如果遇到异常、不一致或改进建议，应该报告

**操作模板**：

```typescript
// 对于每个目标文件（除了保留的原文件）
runSubagent({
  agentName: "Implementer",
  description: "创建XXX测试文件",
  prompt: `创建新文件 \`path/to/TargetTests.cs\`，从 \`path/to/SourceTests.cs\` 中移动以下测试函数：

**要移动的函数（按原始顺序）：**
1. TestFunction1
2. TestFunction2
3. HelperMethod (辅助方法)
...

**任务要求：**
- 新建文件包含完整的 using 语句和命名空间
- 更新类 summary 为"XXX 功能测试"
- 保留必要的辅助方法（如 HelperMethod）
- 从原文件中删除这些已移动的函数（包括对应的 region）
- 确保新文件可以独立编译

**输出要求：**
- 先完成所有文件操作（读取、创建、编辑、删除）
- 最后简单汇报：如发现问题（如缺少依赖、命名冲突）或有改进建议，请说明；否则一句话确认完成即可。`
});
```

**实际调用示例**：

```javascript
// 第 1 次调用：创建基础功能测试文件
runSubagent({
  agentName: "Implementer",
  description: "创建基础功能测试文件",
  prompt: "创建新文件 `tests/.../DurableDictBasicTests.cs`，从原文件中移动：\n" +
    "- Constructor_New_SetsTransientDirtyState\n" +
    "- Constructor_FromCommitted_SetsCleanState\n" +
    "- Set_ThenGet_ReturnsSameValue\n" +
    "...\n" +
    "先完成所有文件操作，最后如有问题或建议请简单说明，否则一句话确认即可。"
});

// 第 2 次调用：创建状态转换测试文件
runSubagent({
  agentName: "Implementer",
  description: "创建状态转换测试文件",
  prompt: "创建新文件 `tests/.../DurableDictStateTests.cs`，移动状态转换相关测试..."
});

// 依此类推，共 N-1 次调用（N = 目标文件数）
```

**关于汇报的时机**：
- SubAgent 机制只返回**最后一轮**模型输出
- 必须先完成所有工具调用，最后再输出汇报
- 汇报应简洁：仅报告发现的问题、不一致或改进建议
- 正常情况下一句话确认完成即可（如"完成，15 个测试已移动"）

### 第 4 步：更新原文件

**目标**：更新原始文件的注释和元数据

**操作**：
```
1. 更新类的 summary 注释（反映保留的测试内容）
2. 移除已废弃的 remarks（如对应条款列表）
3. 确保文件头部注释准确
```

**示例**：
```csharp
// 修改前
/// <summary>
/// DurableDict 基础结构测试。
/// </summary>
/// <remarks>
/// 对应条款：[A-XXX], [S-YYY], [S-ZZZ]
/// </remarks>

// 修改后
/// <summary>
/// DurableDict 复杂场景和杂项测试。
/// </summary>
/// <remarks>
/// 综合测试混合操作、不同数据类型等场景。
/// 基础功能测试已拆分到其他专项测试文件。
/// </remarks>
```

### 第 5 步：验证结果

**目标**：确保拆分后所有测试仍然通过

**操作**：
```bash
# 1. 列出拆分后的文件
ls -lh tests/.../DurableDict*.cs

# 2. 运行所有相关测试
dotnet test tests/XXX.Tests/ --filter "FullyQualifiedName~DurableDict"

# 3. 检查测试数量
# 预期：Passed = 原始测试总数
```

**验证清单**：
- [ ] 所有新文件可以编译
- [ ] 测试数量不变（Passed = 原始总数）
- [ ] 没有跳过的测试（Skipped = 0）
- [ ] 没有失败的测试（Failed = 0）
- [ ] 原文件已更新注释

### 第 6 步：记录经验

**目标**：将拆分经验写入 inbox，供后续归档

**操作**：
```bash
echo "
## 便签 YYYY-MM-DD HH:MM

**XXXTests.cs 文件拆分完成**

原始文件 N 行，包含 M 个单元测试，拆分为 K 个专项测试文件：

1. **FileA.cs** (X tests) - 功能描述
2. **FileB.cs** (Y tests) - 功能描述
...

验证结果：Passed: M, Failed: 0

经验总结：
- 关键洞察 1
- 关键洞察 2

---
" >> agent-team/members/implementer/inbox.md
```

## 成功案例

### 案例 1：DurableDictTests.cs 拆分

**背景**：
- 原始文件：1860 行，84 个测试
- 包含 11 个功能领域
- 有清晰的 region 标记

**执行**：
- 分析用时：5 分钟
- 规划用时：10 分钟
- 拆分用时：35 分钟（7 次 runSubagent 调用）
- 验证用时：5 分钟

**结果**：
- 拆分为 8 个文件（7 个新文件 + 1 个保留）
- 每个文件 150-400 行
- 所有 84 个测试通过
- 编译无错误

**关键决策**：
1. 将 Detached 测试单独成文件（12 个异常测试）
2. Commit 相关测试合并（OnCommitSucceeded + DiscardChanges）
3. 辅助方法跟随：`CreateDetachedDict` → Detached 文件

## 常见问题

### Q1：如何决定拆分粒度？

**A**：遵循以下优先级：
1. **功能内聚优先**：相关测试放在一起
2. **大小次之**：每个文件 200-500 行
3. **避免过度拆分**：不要为了拆分而拆分

**示例**：
- ❌ 不好：每个测试一个文件（过度拆分）
- ❌ 不好：所有状态测试放在一起（太大）
- ✅ 好：Clean/Dirty/Detached 状态分别成文件

### Q2：辅助方法应该放在哪里？

**A**：遵循"就近原则"：
- 如果只被一个文件使用 → 放在该文件中
- 如果被多个文件使用 → 提取到 TestHelper 或 TestBase

**示例**：
```csharp
// CreateDetachedDict 只被 Detached 测试使用
// → 放在 DurableDictDetachedTests.cs 中

// CreateDurableDict 被所有测试使用
// → 已在 TestHelper.cs 中
```

### Q3：region 应该保留吗？

**A**：视情况而定：
- 拆分后单个文件测试数量少（<10）→ 不需要 region
- 拆分后仍有多个子分组（>10）→ 保留 region

### Q4：如何处理测试间的依赖？

**A**：
- **最佳实践**：测试应该独立，没有依赖
- **如果存在依赖**：
  - 共享的 Setup → 提取到基类
  - 共享的数据 → 提取到 TestData 类
  - 测试顺序依赖 → 考虑重构测试

### Q5：拆分后测试运行变慢了？

**A**：
- 通常不会，xUnit 并行运行测试类
- 如果变慢，检查：
  - 是否有静态状态共享
  - 是否有文件 I/O 冲突
  - 考虑使用 `[Collection]` 特性控制并行度

## 反模式警告

### ❌ 反模式 1：按文件行数机械拆分

**错误做法**：
```
DurableDictTests_Part1.cs (lines 1-500)
DurableDictTests_Part2.cs (lines 501-1000)
DurableDictTests_Part3.cs (lines 1001-1500)
```

**为什么不好**：破坏了功能内聚性，难以维护

**正确做法**：按功能领域拆分（见第 2 步）

### ❌ 反模式 2：要求详细汇报或在汇报后调用工具

**错误做法**：
```javascript
runSubagent({
  prompt: "...完成后输出详细的变更报告，包括每个测试的迁移细节、代码差异对比..."
});
```

**为什么不好**：
- SubAgent 只返回最后一轮输出
- 如果先输出详细汇报，然后发现问题需要调用工具修复，汇报会丢失
- 详细汇报占用 token 但价值有限

**正确做法**：
```javascript
runSubagent({
  prompt: "...先完成所有文件操作，最后如有问题或建议请简单说明，" +
          "否则一句话确认即可（如'完成，15 个测试已移动'）"
});
```

**允许的汇报内容**：
- ✅ 发现的问题：缺少依赖、命名冲突、测试失败
- ✅ 改进建议：测试分组建议、辅助方法提取建议
- ✅ 简单确认：测试数量、文件大小
- ❌ 详细代码差异对比
- ❌ 每个测试的迁移日志

### ❌ 反模式 3：并行调用 runSubagent

**错误做法**：
```javascript
// 同时调用多个 subagent
Promise.all([
  runSubagent({ /* 创建文件 A */ }),
  runSubagent({ /* 创建文件 B */ }),
  runSubagent({ /* 创建文件 C */ })
]);
```

**为什么不好**：
- 多个 subagent 同时修改原文件会冲突
- 删除操作可能互相干扰

**正确做法**：串行调用，等待每个完成后再调用下一个

### ❌ 反模式 4：忘记移动辅助方法

**错误做法**：
- 移动测试函数，但忘记移动 `CreateDetachedDict`
- 导致新文件编译失败

**正确做法**：
- 在分析阶段就识别辅助方法
- 在拆分方案中明确标注辅助方法去向
- 在 subagent prompt 中明确列出辅助方法

## 工具清单

本配方使用的工具：

| 工具 | 用途 | 关键参数 |
|------|------|----------|
| `read_file` | 读取原始测试文件 | filePath |
| `runSubagent` | 创建新文件并移动测试 | agentName="Implementer" |
| `replace_string_in_file` | 更新原文件注释 | filePath, oldString, newString |
| `run_in_terminal` | 运行测试验证 | command="dotnet test ..." |
| `list_dir` | 列出拆分后的文件 | path |

## 检查清单

拆分完成前，确认以下事项：

**分析阶段**：
- [ ] 已读取完整的原始文件
- [ ] 已识别所有 region 和功能分组
- [ ] 已统计测试数量
- [ ] 已识别辅助方法

**规划阶段**：
- [ ] 已确定目标文件数量和命名
- [ ] 每个文件的测试数量合理（5-20 个）
- [ ] 每个文件的预估行数合理（200-500 行）
- [ ] 已分配辅助方法去向

**执行阶段**：
- [ ] 已串行调用 runSubagent（不并行）
- [ ] 每个 prompt 强调"先完成工具调用，最后汇报"
- [ ] 每个 prompt 明确列出要移动的函数
- [ ] 每个 prompt 包含辅助方法（如需要）
- [ ] 每个 prompt 鼓励报告发现的问题或建议

**验证阶段**：
- [ ] 所有新文件已创建
- [ ] 所有新文件可以编译
- [ ] 测试数量不变（Passed = 原始总数）
- [ ] 没有失败或跳过的测试
- [ ] 原文件已更新注释

**记录阶段**：
- [ ] 已将经验写入 inbox
- [ ] 已记录关键决策和洞见

## 参考资料

- [Implementer 实现协议](../members/implementer/index.md)
- [记忆维护技能](../wiki/memory-maintenance-skill.md)
- [SubAgent 调用规范](../../AGENTS.md#runsubagent-调用规范)

## 版本历史

- **v1.0** (2025-12-27): 初始版本，基于 DurableDictTests.cs 拆分经验
