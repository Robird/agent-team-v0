---
docId: "W-0010-goals"
title: "W-0010 Goals"
produce_by:
  - "wish/W-0010-design-dsl-impl/wish.md"
---

# W-0010 Goals

## 目标层级树

```
G-001: 实现 Atelia.DesignDsl 基础解析器
├── G-001.1: ATX-Node/ATX-Tree 构建
│   ├── G-001.1.1: ATX-Node 数据结构定义
│   ├── G-001.1.2: ATX-Tree 构建算法
│   └── G-001.1.3: 单元测试覆盖
├── G-001.2: Term/Clause 提取
│   ├── G-001.2.1: Term-Node 模式匹配
│   ├── G-001.2.2: Clause-Node 模式匹配（3 种亚型）
│   └── G-001.2.3: 单元测试覆盖
└── G-001.3: DocGraph 集成
    ├── G-001.3.1: API 设计
    ├── G-001.3.2: DocGraph 调用实现
    └── G-001.3.3: 端到端测试
```

## 目标详细定义

### G-001: 实现 Atelia.DesignDsl 基础解析器

**完成判据**:
- [ ] 所有子目标完成
- [ ] 示例文档（AI-Design-DSL.md）能成功解析
- [ ] 提取出术语和条款列表

### G-001.1: ATX-Node/ATX-Tree 构建

**完成判据**:
- [ ] `AxtNode` 类型定义完成
- [ ] `AxtTree` 构建算法实现
- [ ] 嵌套关系正确（深度计算、父子关系）
- [ ] 单元测试通过（覆盖基本场景和边缘情况）

### G-001.2: Term/Clause 提取

**完成判据**:
- [ ] 能识别 `## term \`Term-ID\` 标题` 模式
- [ ] 能识别 `### decision [CLAUSE-ID]` 模式
- [ ] 能识别 `### spec [CLAUSE-ID]` 模式
- [ ] 能识别 `### derived [CLAUSE-ID]` 模式
- [ ] 单元测试覆盖所有模式

### G-001.3: DocGraph 集成

**完成判据**:
- [ ] DocGraph 能调用 DesignDsl API
- [ ] 能解析并输出术语/条款列表
- [ ] 端到端测试通过
