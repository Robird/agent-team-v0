# 术语指南：三层 SSOT 结构

> **创建日期**: 2025-12-31
> **最后更新**: 2025-12-31
> **目的**: 为团队成员提供清晰的术语资源导航
> **参考**: [2025-12-31 畅谈会记录](../meeting/2025-12-31-ssot-terminology-structure.md)

---

## 🗺️ 快速导航

### 根据你的需求选择资源：

| 你想... | 查阅... | 文件位置 |
|:--------|:--------|:---------|
| **理解术语含义**（是什么） | Layer 1：概念语义 SSOT | `artifact-tiers.md` |
| **确认术语写法**（怎么写） | Layer 2：写法规范 SSOT | `spec-conventions.md` §4 |
| **配置工具规则**（lint/IDE） | Layer 3：机器可读 SSOT | `terminology-registry.yaml` |
| **查看完整指南** | 本文件 | `terminology-guide.md` |

---

## 📚 三层 SSOT 结构详解

### Layer 1：概念语义 SSOT
**文件**: `artifact-tiers.md`
**职责**: 定义术语的**含义**（"是什么"）
**包含**:
- 五层级方法论（Why/Shape/Rule/Plan/Craft）
- 术语演变历史
- 别名映射和向后兼容
- 常见问题解答

**使用场景**:
- 新成员 onboarding
- 理解项目核心概念
- 澄清术语歧义
- 查看术语历史演变

### Layer 2：写法规范 SSOT  
**文件**: `atelia/docs/spec-conventions.md` 第4章
**职责**: 规定术语的**写法**（"怎么写"）
**包含**:
- 分域命名体系（条款锚点域、术语域、文件名域、代码标识符域）
- Title-Kebab 格式规则
- 缩写大小写规则（引用 registry）
- 注册表治理原则
- 迁移策略

**使用场景**:
- 编写新文档时确认术语格式
- 代码审查时检查命名一致性
- 创建新文件时确定文件名格式
- 实施术语迁移时参考策略

### Layer 3：机器可读 SSOT
**文件**: `terminology-registry.yaml`
**职责**: 提供**工具可执行**的规则
**包含**:
- 全大写缩写白名单（`acronyms_uppercase`）
- 层级术语闭集（`layer_terms`）
- 复合术语示例（`compound_terms`）
- 术语变体映射（`term_variants`）
- 管理元数据（版本、流程）

**使用场景**:
- CI/CD lint 检查
- IDE 自动补全和错误提示
- DocGraph 术语规范化
- 脚本自动化处理

---

## 🔗 引用关系图

```
┌─────────────────────────────────────────────────────┐
│               术语指南 (本文件)                      │
│                   ↙       ↘                         │
│     概念语义 SSOT          写法规范 SSOT             │
│     (artifact-tiers.md)      (spec-conventions.md)     │
│           ↓                     ↓                    │
│     机器可读 SSOT ←─────────────┘                    │
│   (terminology-registry.yaml)                       │
└─────────────────────────────────────────────────────┘
```

**引用原则**:
1. **向上引用**：子文档引用父文档
2. **避免循环**：单向引用关系
3. **明确职责**：每个文件有清晰的职责边界

---

## 🛠️ 工具集成

### 1. 一致性检查脚本
**文件**: `scripts/check-terminology-consistency.sh`
**功能**: 检查文档中的术语一致性
**使用**: `./agent-team/scripts/check-terminology-consistency.sh`
**输出**: 通过/警告/错误报告

### 2. Registry 读取示例
```bash
# 查看当前 registry 版本
grep "version:" terminology-registry.yaml

# 查看缩写白名单
yq '.acronyms_uppercase[]' terminology-registry.yaml

# 查看层级术语
yq '.layer_terms[]' terminology-registry.yaml
```

### 3. 常用命令
```bash
# 检查所有规范层文档
./agent-team/scripts/check-terminology-consistency.sh

# 搜索特定术语的使用
grep -r "Resolve-Tier" --include="*.md" agent-team/wiki atelia/docs

# 查看术语表版本
head -10 artifact-tiers.md | grep "版本"
```

---

## 📋 团队协作流程

### 新术语提案流程
1. **概念定义** → 更新 `artifact-tiers.md`（Layer 1）
2. **写法规范** → 更新 `spec-conventions.md`（Layer 2）
3. **工具规则** → 更新 `terminology-registry.yaml`（Layer 3）
4. **验证检查** → 运行一致性检查脚本

### 术语变更流程
1. **评估影响**：确定变更影响的范围
2. **更新 SSOT**：按三层结构同步更新
3. **迁移文档**：逐步更新相关文档
4. **工具适配**：更新 lint 规则和工具

### 引用规范
- **内部引用**：使用相对路径
- **外部引用**：使用完整路径
- **版本引用**：注明版本号
- **条款引用**：使用条款编号（如 `[S-TERM-*]`）

---

## 🎯 验收标准

### 三层结构验收
- [ ] `artifact-tiers.md` 只包含概念语义，无写法规范
- [ ] `spec-conventions.md` §4 明确引用 registry
- [ ] `terminology-registry.yaml` 可被工具正确读取
- [ ] 检查脚本从 registry 动态加载规则

### 引用关系验收  
- [ ] 无循环引用
- [ ] 无悬空链接
- [ ] 引用路径正确
- [ ] 版本信息一致

### 工具集成验收
- [ ] 检查脚本运行通过
- [ ] Registry 格式正确
- [ ] 版本号管理清晰
- [ ] 错误处理完善

---

## 📖 学习资源

### 新成员入门
1. 阅读本指南了解三层结构
2. 查阅 `artifact-tiers.md` 理解核心概念
3. 查看 `spec-conventions.md` §4 学习写法规范
4. 运行检查脚本验证理解

### 文档作者指南
1. 写作前确认术语在 `artifact-tiers.md` 中有定义
2. 遵循 `spec-conventions.md` 的格式规范
3. 使用 registry 中的缩写白名单
4. 完成后运行一致性检查

### 工具开发者指南
1. 以 `terminology-registry.yaml` 为唯一数据源
2. 支持版本检查和向后兼容
3. 提供清晰的错误信息
4. 集成到 CI/CD 流程

---

## 🔄 变更历史

| 版本 | 日期 | 变更说明 |
|:-----|:-----|:---------|
| 1.0.0 | 2025-12-31 | 初始版本，基于三层 SSOT 结构决策 |

**维护者**: DocOps
**审计者**: Craftsman
**UX 设计**: Curator
**概念设计**: Seeker

---

> **提示**: 本文件是术语资源的**导航入口**，不是 SSOT。具体定义请查阅对应的 SSOT 文件。