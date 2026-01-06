# F-003: 逆向扫描算法遗漏 FrameStatus 校验

**状态**: ✅ 已修复

**位置**: rbf-format.md#§6.1 `[R-REVERSE-SCAN-ALGORITHM]`

## 问题描述

逆向扫描伪代码只校验了 HeadLen/TailLen、CRC、Fence、对齐，但 §5 `[F-FRAMING-FAIL-REJECT]` 明确要求还要校验 FrameStatus 位域合法性和字节一致性。

实现者照抄 §6 伪码会漏掉 FrameStatus 校验。

## 修复

在 §6.1 伪代码中，CRC 校验之后添加 FrameStatus 校验步骤：

1. 读取 FrameStatus 最后一字节，提取 StatusLen
2. 检查 Reserved bits (bit 2-6) MUST 为 0
3. 验证 FrameStatus 所有字节一致

## 变更

- **文件**: `/repos/focus/atelia/docs/Rbf/rbf-format.md`
- **版本**: 0.18 → 0.19
- **变更日志条目**: §6.1 伪代码补充 FrameStatus 校验：Reserved bits 合法性 + 全字节一致性（对齐 §5 `[F-FRAMING-FAIL-REJECT]`）
