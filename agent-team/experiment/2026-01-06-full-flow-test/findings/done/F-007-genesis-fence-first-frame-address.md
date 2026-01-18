---
id: F-007
status: done
severity: P2
---
# F-007: HeaderFence 后首帧地址是 4 而非 0

## 问题
文档说首帧紧跟 HeaderFence，但没有明确说首帧的起始地址是 offset=4（HeaderFence 长度 4B）。
实现者可能误认为首帧从 offset=0 开始。

## 修复
在 `[F-HEADER-FENCE]` 条款中增加第三条：
> 首帧（如果存在）的起始地址 MUST 为 `offset=4`（紧跟 HeaderFence 之后）。

## 变更
- 文档版本：0.22 → 0.23
- 修改位置：§2.2 `[F-HEADER-FENCE]`
