using System.IO;
using Microsoft.Win32.SafeHandles;
using Atelia.Data;
using Atelia.Data.Hashing;

namespace Atelia.Rbf.Internal;

/// <summary>合并文件写入和 CRC 计算的 IByteSink 实现</summary>
/// <remarks>
/// 职责边界：Push → RandomAccess.Write + CRC 累积（跳过前 N 字节）。
/// 设计背景（参见 @[Decision 7.E]）：
/// - 将 <c>RandomAccessByteSink</c> 和 CRC 计算合并为单一类型
/// - 在 RBF Builder 场景，"写入文件 + 计算 CRC"是内聚的单一职责
/// - 支持 <c>crcSkipBytes</c> 参数：前 N 字节不参与 CRC 累积，但仍写入文件
/// 用法（HeadLen reservation）：
/// - <c>RbfWriteSink(handle, frameStart, crcSkipBytes: 4)</c>
/// - HeadLen 放在帧开头，CRC 只覆盖 Payload + TailMeta + Padding
/// 并发：非线程安全，依赖 <c>[S-RBF-BUILDER-SINGLE-OPEN]</c> 契约
/// （同一时刻只有一个活跃 Builder）。
/// </remarks>
internal sealed class RbfWriteSink : IByteSink {
    private readonly SafeFileHandle _file;
    private readonly int _crcSkipBytes;    // 跳过 CRC 的前 N 字节
    private long _writeOffset;
    private int _skippedSoFar;             // 已跳过的字节数
    private uint _crc = RollingCrc.DefaultInitValue;
    private long _totalWritten;

    /// <summary>创建合并写入 + CRC 计算的 Sink</summary>
    /// <param name="file">文件句柄（需具备 Write 权限）</param>
    /// <param name="startOffset">起始写入位置（byte offset）</param>
    /// <param name="crcSkipBytes">跳过 CRC 累积的前 N 字节（默认 0，全量 CRC）</param>
    /// <exception cref="ArgumentNullException"><paramref name="file"/> 为 null</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="crcSkipBytes"/> 为负数</exception>
    public RbfWriteSink(SafeFileHandle file, long startOffset, int crcSkipBytes = 0) {
        _file = file ?? throw new ArgumentNullException(nameof(file));
        if (crcSkipBytes < 0) { throw new ArgumentOutOfRangeException(nameof(crcSkipBytes), crcSkipBytes, "crcSkipBytes must be >= 0"); }
        _writeOffset = startOffset;
        _crcSkipBytes = crcSkipBytes;
    }

    /// <summary>当前写入位置（byte offset）</summary>
    /// <remarks>
    /// Builder 层可用于计算已写入字节数（CurrentOffset - StartOffset）
    /// 以及最终 HeadLen 回填。
    /// </remarks>
    public long CurrentOffset => _writeOffset;

    /// <summary>累计写入字节数（含 skipped 部分）</summary>
    public long TotalWritten => _totalWritten;

    /// <summary>参与 CRC 计算的字节数（不含 skipped 部分）</summary>
    /// <remarks>
    /// 使用 <c>_skippedSoFar</c> 而非 <c>_crcSkipBytes</c>，
    /// 以处理 <c>_totalWritten &lt; _crcSkipBytes</c> 的边界情况。
    /// </remarks>
    public long CrcCoveredLength => _totalWritten - _skippedSoFar;

    /// <summary>推送数据到文件并累积 CRC</summary>
    /// <remarks>
    /// 1. 调用 <see cref="RandomAccess.Write(SafeFileHandle, ReadOnlySpan{byte}, long)"/> 写入数据
    /// 2. 跳过前 <c>crcSkipBytes</c> 字节后累积 CRC
        /// 错误处理：I/O 异常直接抛出（符合 Infra Fault 策略）。
    /// </remarks>
    public void Push(ReadOnlySpan<byte> data) {
        if (data.IsEmpty) { return; }

        // 1. 写入文件
        RandomAccess.Write(_file, data, _writeOffset);
        _writeOffset += data.Length;
        _totalWritten += data.Length;

        // 2. CRC 累积（跳过前 crcSkipBytes 字节）
        if (_skippedSoFar < _crcSkipBytes) {
            int toSkip = Math.Min(data.Length, _crcSkipBytes - _skippedSoFar);
            _skippedSoFar += toSkip;
            data = data.Slice(toSkip);
        }

        if (!data.IsEmpty) {
            _crc = RollingCrc.CrcForward(_crc, data);
        }
    }

    /// <summary>获取当前累积的 CRC（已 Finalize）</summary>
    public uint GetCrc() => _crc ^ RollingCrc.DefaultFinalXor;
}
