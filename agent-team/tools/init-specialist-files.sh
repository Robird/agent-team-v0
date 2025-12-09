#!/bin/bash
# Specialist 初始文件生成脚本
# 用法: ./init-specialist-files.sh

set -e
cd "$(dirname "$0")/.."

DATE=$(date +%Y-%m-%d)

# Specialist 列表
SPECIALISTS=(planner investigator implementer qa docops codex-reviewer gemini-advisor)

# 项目列表
PROJECTS=(PieceTreeSharp DocUI PipeMux atelia-copilot-chat atelia-prototypes)

# 为每个 Specialist 创建初始文件
for s in "${SPECIALISTS[@]}"; do
  DIR="members/$s"
  mkdir -p "$DIR"
  
  # index.md
  if [ ! -f "$DIR/index.md" ]; then
    cat > "$DIR/index.md" << EOF
# ${s^} 认知索引

> 最后更新: $DATE

## 我是谁
（待填写）

## 我关注的项目
- [ ] PieceTreeSharp
- [ ] DocUI
- [ ] PipeMux
- [ ] atelia-copilot-chat

## 最近工作
（按需填写）
EOF
    echo "Created $DIR/index.md"
  fi
  
  # meta-cognition.md
  if [ ! -f "$DIR/meta-cognition.md" ]; then
    cat > "$DIR/meta-cognition.md" << EOF
# ${s^} 元认知

## 工作流程
（待总结）

## 经验教训
（待积累）
EOF
    echo "Created $DIR/meta-cognition.md"
  fi
done

# 为每个项目创建 wiki README
for p in "${PROJECTS[@]}"; do
  DIR="wiki/$p"
  mkdir -p "$DIR"
  
  if [ ! -f "$DIR/README.md" ]; then
    cat > "$DIR/README.md" << EOF
# $p 知识库

> 最后更新: $DATE
> 维护者: 所有 Specialist 共同维护

## 项目概述
（待补充）

## 关键文件
（待补充）

## 架构要点
（待补充）

## 已知问题
（待补充）
EOF
    echo "Created $DIR/README.md"
  fi
done

echo "Done! All initial files created."
