# T-P2-02 VarInt 编解码 Implementation Result

## 实现摘要

实现了 protobuf 风格 base-128 VarInt 编解码，包括无符号 varuint、ZigZag 有符号编码、canonical 校验和错误处理。完全符合 `[F-VARINT-CANONICAL-ENCODING]` 和 `[F-DECODE-ERROR-FAILFAST]` 条款要求。

## 文件变更

| 文件 | 类型 | 描述 |
|------|------|------|
| `atelia/src/StateJournal/Core/VarInt.cs` | 新增 | VarInt 编解码静态类 |
| `atelia/tests/StateJournal.Tests/Core/VarIntTests.cs` | 新增 | 87 个测试用例 |

## 源码对齐说明

| 规范元素 | 实现 | 备注 |
|---------|------|------|
| `varuint` 编码 | `WriteVarUInt(Span<byte>, ulong)` | 返回写入字节数 |
| `varuint` 解码 | `TryReadVarUInt(ReadOnlySpan<byte>)` | 返回 `AteliaResult<(ulong, int)>` |
| `GetVarUIntLength` | `GetVarUIntLength(ulong)` | 计算 canonical 长度 |
| ZigZag64 编码 | `ZigZagEncode(long)` | `(n << 1) ^ (n >> 63)` |
| ZigZag64 解码 | `ZigZagDecode(ulong)` | `(zz >>> 1) ^ -(zz & 1)` |
| `varint` 有符号 | `WriteVarInt` / `TryReadVarInt` | ZigZag + varuint 组合 |
| Canonical 校验 | `VarIntNonCanonicalError` | 检测多余 0x00 continuation |
| EOF 检测 | `VarIntDecodeError` | 检测截断的编码 |
| 溢出检测 | `VarIntDecodeError` | 超过 10 字节或第 10 字节 > 0x01 |

## API 设计

```csharp
public static class VarInt
{
    public const int MaxVarUInt64Bytes = 10;

    // === Unsigned ===
    public static int GetVarUIntLength(ulong value);
    public static int WriteVarUInt(Span<byte> destination, ulong value);
    public static AteliaResult<(ulong Value, int BytesConsumed)> TryReadVarUInt(ReadOnlySpan<byte> source);

    // === ZigZag ===
    public static ulong ZigZagEncode(long value);
    public static long ZigZagDecode(ulong encoded);

    // === Signed ===
    public static int WriteVarInt(Span<byte> destination, long value);
    public static AteliaResult<(long Value, int BytesConsumed)> TryReadVarInt(ReadOnlySpan<byte> source);
}
```

## 测试结果

- **Targeted**: `dotnet test --filter VarIntTests` → 87/87 Passed
- **Full**: `dotnet test StateJournal.Tests` → 149/149 Passed

## 测试覆盖

| 分类 | 用例数 | 说明 |
|------|--------|------|
| GetVarUIntLength 边界 | 16 | 0→1B, 127→1B, 128→2B, ... ulong.Max→10B |
| VarUInt 往返 | 10 | 边界值和常见值 |
| 编码字节验证 | 4 | 300→0xAC 0x02 等 |
| Non-canonical 拒绝 | 3 | 0x80 0x00 表示 0, 0xFF 0x00 表示 127 等 |
| EOF 检测 | 3 | 空缓冲区、截断 continuation |
| 溢出检测 | 3 | 11 字节、第 10 字节太大、MaxValue |
| ZigZag 编码 | 10 | 0→0, -1→1, 1→2, long.Max/Min |
| ZigZag 解码 | 10 | 逆映射验证 |
| ZigZag 往返 | 10 | 边界值往返 |
| VarInt 有符号 | 15 | 正负数往返、字节验证 |
| 错误类型 | 2 | ErrorCode 验证 |
| 部分读取 | 2 | 缓冲区有多余数据 |

## 已知差异

无。实现完全遵循规范定义。

## 遗留问题

无。

## QA 关注点

1. **Non-canonical 检测**：确保 `[0x80, 0x00]` 等多余编码被正确拒绝
2. **第 10 字节边界**：uint64 最高位只允许 0x00 或 0x01
3. **ZigZag 极值**：long.MaxValue → ulong.MaxValue - 1, long.MinValue → ulong.MaxValue

## Changefeed Anchor

`#delta-2025-12-25-varint`
