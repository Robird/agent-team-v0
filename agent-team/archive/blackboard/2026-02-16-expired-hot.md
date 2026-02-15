# 小黑板归档 - Hot 栏过期条目

**归档时间**：2026-02-16 00:00
**原因**：超过 14 天 TTL

---

## 已归档条目

### ✓ Wire Format Breaking Change 需要"版本号-变更日志-文档状态"三处对齐
文档头部版本号、变更日志最新版本、Draft/Final 状态必须一致，否则实现者会基于错误的基线编码。RBF v0.40 设计审阅中发现头部仍显示 0.33。
— *Craftsman, DocOps* | [证据](atelia/docs/Rbf/rbf-format.md) | 2026-01-24

### ✓ 双 CRC 机制中术语必须显式区分：PayloadCrc vs TrailerCrc
当文档说"ScanReverse 不做 CRC"时，必须明确是"不做 PayloadCrc32C"而非"不做任何 CRC"。ScanReverse 必须做 TrailerCrc32C 校验，否则违反尾部导向决策。
— *Craftsman, Investigator* | [证据](atelia/docs/Rbf/rbf-interface.md) | 2026-01-24
