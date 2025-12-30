#!/bin/bash
# 术语一致性检查脚本
# 用于验证术语命名规范的迁移进度
# 版本: 1.1.0
# 创建日期: 2025-12-31
# 更新日期: 2025-12-31 - 动态从 registry 读取缩写白名单

set -e

# 颜色定义
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# 项目根目录
ROOT_DIR="/repos/focus"
REGISTRY_FILE="agent-team/wiki/terminology-registry.yaml"
cd "$ROOT_DIR"

echo -e "${BLUE}=== 术语一致性检查脚本 ===${NC}"
echo "开始时间: $(date)"
echo ""

# 从 registry 读取版本号
get_registry_version() {
    python3 -c "
import yaml
with open('$REGISTRY_FILE') as f:
    data = yaml.safe_load(f)
    print(data.get('version', 'unknown'))
" 2>/dev/null || echo "unknown"
}

# 从 registry 读取缩写列表并生成检查模式
# 返回: 首字母大写形式的缩写数组（如 ID -> Id, API -> Api）
load_acronyms_from_registry() {
    python3 -c "
import yaml
with open('$REGISTRY_FILE') as f:
    data = yaml.safe_load(f)
    acronyms = data.get('acronyms_uppercase', [])
    for a in acronyms:
        # 转换为首字母大写形式（用于检测错误用法）
        # 如 ID -> Id, API -> Api, LLM -> Llm
        if len(a) == 2:
            print(a[0].upper() + a[1].lower())
        else:
            print(a[0].upper() + a[1:].lower())
" 2>/dev/null
}

# 1. 检查注册表文件是否存在
echo -e "${BLUE}[1/5] 检查术语注册表文件...${NC}"
if [ -f "$REGISTRY_FILE" ]; then
    echo -e "  ${GREEN}✅ terminology-registry.yaml 存在${NC}"
    
    # 检查文件格式
    if python3 -c "import yaml; yaml.safe_load(open('$REGISTRY_FILE'))" 2>/dev/null; then
        echo -e "  ${GREEN}✅ YAML 格式正确${NC}"
        
        # 输出版本信息
        REGISTRY_VERSION=$(get_registry_version)
        echo -e "  ${GREEN}📋 Registry 版本: ${REGISTRY_VERSION}${NC}"
        
        # 从 registry 动态读取缩写列表
        ACRONYMS_FROM_REGISTRY=($(load_acronyms_from_registry))
        if [ ${#ACRONYMS_FROM_REGISTRY[@]} -eq 0 ]; then
            echo -e "  ${YELLOW}⚠️  未能从 registry 读取缩写列表，使用默认值${NC}"
            USE_REGISTRY_ACRONYMS=false
        else
            echo -e "  ${GREEN}📋 从 registry 加载了 ${#ACRONYMS_FROM_REGISTRY[@]} 个缩写规则${NC}"
            USE_REGISTRY_ACRONYMS=true
        fi
    else
        echo -e "  ${RED}❌ YAML 格式错误${NC}"
        exit 1
    fi
else
    echo -e "  ${RED}❌ terminology-registry.yaml 不存在${NC}"
    exit 1
fi

# 2. 检查规范层文档中的缩写不一致
echo -e "\n${BLUE}[2/5] 检查规范层文档中的缩写不一致...${NC}"

# 定义规范层目录
SPEC_DIRS=(
    "agent-team/wiki"
    "atelia/docs"
    "wishes/specs"
    "wishes/templates"
)

# 动态生成检查模式
# 如果从 registry 读取成功，使用 registry 数据；否则使用硬编码默认值
if [ "$USE_REGISTRY_ACRONYMS" = true ]; then
    PATTERNS=()
    for abbr in "${ACRONYMS_FROM_REGISTRY[@]}"; do
        PATTERNS+=("\\b${abbr}\\b")
    done
    echo -e "  ${GREEN}✅ 使用 registry 动态生成的检查模式${NC}"
else
    # 降级：使用硬编码默认值
    echo -e "  ${YELLOW}⚠️  降级模式：使用硬编码默认值${NC}"
    PATTERNS=(
        "\\bId\\b"
        "\\bUi\\b" 
        "\\bUx\\b"
        "\\bDx\\b"
        "\\bAi\\b"
        "\\bIo\\b"
        "\\bApi\\b"
        "\\bLlm\\b"
        "\\bMvp\\b"
        "\\bSsot\\b"
        "\\bCrc\\b"
        "\\bRbf\\b"
        "\\bCow\\b"
        "\\bOk\\b"
    )
fi

ERROR_COUNT=0
WARNING_COUNT=0

# 函数：获取特定缩写的排除模式
get_exclude_pattern() {
    local abbr="$1"
    case "$abbr" in
        "Id")
            # 排除代码标识符（.Id, `Id`, int Id, ObjectId, -Id 等）
            # 包括：属性访问、内联代码、类型声明、参数名、PowerShell 参数、表格列名、枚举列表
            # 历史讨论文档中的术语引用（Diagnostic Id 在 .NET 中常见）
            echo "\\.Id\\|\`Id\`\\|int Id\\|ObjectId\\|-Id \\|: Id\\|| Id |\\|Id =\\|Id:\\|Id、\\|/ Id /\\|Diagnostic Id"
            ;;
        "Api")
            # 排除代码标识符
            echo "\\.Api\\|\`Api\`"
            ;;
        "Rbf")
            # 排除文件路径、命名空间、项目名
            echo "atelia/docs/Rbf\\|atelia/src/Rbf\\|atelia/tests/Rbf\\|Atelia\\.Rbf\\|Rbf\\.csproj\\|Rbf\\.Tests\\|/Rbf/"
            ;;
        *)
            echo ""
            ;;
    esac
}

for dir in "${SPEC_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        echo -e "  检查目录: $dir"
        
        for pattern in "${PATTERNS[@]}"; do
            # 提取缩写（去掉转义字符）
            abbr=$(echo "$pattern" | sed 's/\\b//g')
            
            # 获取特定排除模式
            extra_exclude=$(get_exclude_pattern "$abbr")
            
            # 构建完整排除模式
            # 基本排除：反例说明（包含"不是"、"not"、❌ 标记）和注册表文件
            base_exclude="terminology-registry.yaml\\|不是\\|not \\|❌"
            
            if [ -n "$extra_exclude" ]; then
                full_exclude="$base_exclude\\|$extra_exclude"
            else
                full_exclude="$base_exclude"
            fi
            
            # 搜索匹配项（大小写敏感，只匹配首字母大写形式如 'Api'，而非正确的 'API'）
            matches=$(grep -r "$pattern" --include="*.md" "$dir" 2>/dev/null | grep -v "$full_exclude" | wc -l)
            
            if [ "$matches" -gt 0 ]; then
                echo -e "    ${YELLOW}⚠️  发现 $matches 处 '$abbr' 使用（应为大写）${NC}"
                WARNING_COUNT=$((WARNING_COUNT + matches))
                
                # 显示前3个匹配项（排除反例和合法用法）
                grep -r "$pattern" --include="*.md" "$dir" 2>/dev/null | grep -v "$full_exclude" | head -3 | while read -r line; do
                    echo -e "      - $line"
                done
            fi
        done
    fi
done

# 3. 检查层级术语格式
echo -e "\n${BLUE}[3/5] 检查层级术语格式...${NC}"

LAYER_PATTERNS=(
    "Why Layer"
    "Shape Layer"
    "Rule Layer"
    "Plan Layer"
    "Craft Layer"
    "WhyLayer"
    "ShapeLayer"
    "RuleLayer"
    "PlanLayer"
    "CraftLayer"
)

for dir in "${SPEC_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        for pattern in "${LAYER_PATTERNS[@]}"; do
            # 排除作为反例或代码示例的情况（❌ 标记、代码标识符域示例）
            matches=$(grep -r "$pattern" --include="*.md" "$dir" 2>/dev/null | grep -v "❌\|代码标识符域" | wc -l)
            
            if [ "$matches" -gt 0 ]; then
                echo -e "    ${YELLOW}⚠️  在 $dir 中发现 $matches 处 '$pattern'（应为连字符格式）${NC}"
                WARNING_COUNT=$((WARNING_COUNT + matches))
            fi
        done
    fi
done

# 4. 检查复合术语中的缩写
echo -e "\n${BLUE}[4/5] 检查复合术语中的缩写...${NC}"

# 检查 App-For-Llm 等错误格式（大小写敏感搜索，只匹配真正错误的格式）
# 正确格式：App-For-LLM（缩写全大写）
# 错误格式：App-For-Llm（缩写首字母大写）
# 注意：需要排除作为反例出现的情况（如"不是 App-For-Llm"）
COMPOUND_PATTERNS=(
    "App-For-Llm[^A-Z]"    # 错误格式：缩写首字母大写，且后面不是大写字母（排除 App-For-LLM）
    "App-For-Api[^A-Z]"    # 错误格式：缩写首字母大写，且后面不是大写字母（排除 App-For-API）
    "Context-Proj[^e]"     # 错误格式：不完整的缩写，且后面不是 e（排除 Context-Projection）
)

COMPOUND_NAMES=(
    "App-For-Llm（应为 App-For-LLM）"
    "App-For-Api（应为 App-For-API）"
    "Context-Proj（应为 Context-Projection）"
)

for dir in "${SPEC_DIRS[@]}"; do
    if [ -d "$dir" ]; then
        for i in "${!COMPOUND_PATTERNS[@]}"; do
            pattern="${COMPOUND_PATTERNS[$i]}"
            name="${COMPOUND_NAMES[$i]}"
            # 使用大小写敏感搜索，但需要过滤掉作为反例的情况
            # 先搜索所有匹配，然后过滤掉包含"不是"、"not"、"错误"等词的上下文
            # 排除作为反例出现的情况（"不是" 或 "not"）
            matches=$(grep -r -E "$pattern" --include="*.md" "$dir" 2>/dev/null | grep -v "不是\|not " | wc -l)
            
            if [ "$matches" -gt 0 ]; then
                echo -e "    ${RED}❌ 在 $dir 中发现 $matches 处 $name${NC}"
                ERROR_COUNT=$((ERROR_COUNT + matches))
                # 显示匹配项
                grep -r -E "$pattern" --include="*.md" "$dir" 2>/dev/null | grep -v "不是\|not " | head -3 | while read -r line; do
                    echo -e "      - $line"
                done
            fi
        done
    fi
done

# 5. 总结报告
echo -e "\n${BLUE}[5/5] 检查完成${NC}"
echo "========================================"

if [ "$ERROR_COUNT" -eq 0 ] && [ "$WARNING_COUNT" -eq 0 ]; then
    echo -e "${GREEN}✅ 所有检查通过！${NC}"
    echo "规范层文档术语使用完全符合新规范。"
    exit 0
elif [ "$ERROR_COUNT" -eq 0 ]; then
    echo -e "${YELLOW}⚠️  发现 $WARNING_COUNT 个警告${NC}"
    echo "建议在 Phase 2 迁移中修复这些不一致。"
    exit 0
else
    echo -e "${RED}❌ 发现 $ERROR_COUNT 个错误和 $WARNING_COUNT 个警告${NC}"
    echo "错误需要立即修复（复合术语中的缩写格式错误）。"
    exit 1
fi

echo ""
echo "结束时间: $(date)"