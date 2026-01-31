using System.Buffers.Binary;
using Atelia.Data.Hashing;
using Atelia.Rbf.Internal;
using Microsoft.Win32.SafeHandles;
using Xunit;

namespace Atelia.Rbf.Tests.Internal;

/// <summary>RbfWriteSink 单元测试</summary>
/// <remarks>
/// 验收标准（来自 Task 7.1）：
/// - crcSkipBytes=0 时，行为与原 RandomAccessByteSink + 全量 CRC 一致
/// - crcSkipBytes>0 时，前 N 字节不参与 CRC，但仍写入文件
/// - 跨 Push() 调用的 skip 逻辑正确（累积 _skippedSoFar）
/// - GetCrc() 返回正确的 Finalize 后 CRC
/// - TotalWritten 返回总字节数（含 skipped）
/// - CrcCoveredLength 返回 CRC 覆盖的字节数（边界情况正确）
/// - 当 _totalWritten &lt; _crcSkipBytes 时，CrcCoveredLength 返回 0
/// </remarks>
public class RbfWriteSinkTests : IDisposable {
    private readonly List<string> _tempFiles = new();

    private string GetTempFilePath() {
        var path = Path.Combine(Path.GetTempPath(), $"RbfWriteSinkTest_{Guid.NewGuid()}");
        _tempFiles.Add(path);
        return path;
    }

    public void Dispose() {
        foreach (var path in _tempFiles) {
            try {
                if (File.Exists(path)) {
                    File.Delete(path);
                }
            }
            catch {
                // 忽略清理错误
            }
        }
    }

    /// <summary>计算预期 CRC（使用 RollingCrc.CrcForward）</summary>
    private static uint ComputeExpectedCrc(ReadOnlySpan<byte> data) {
        return RollingCrc.CrcForward(data);
    }

    // ========== Test 1: Skip0_FullCrc ==========
    /// <summary>skipBytes=0 时，写入数据，验证 CRC 与直接计算一致</summary>
    [Fact]
    public void Skip0_FullCrc() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: 0);

        // Act
        sink.Push(data);

        // Assert
        uint expectedCrc = ComputeExpectedCrc(data);
        Assert.Equal(expectedCrc, sink.GetCrc());
        Assert.Equal(data.Length, sink.TotalWritten);
        Assert.Equal(data.Length, sink.CrcCoveredLength);
        Assert.Equal(data.Length, sink.CurrentOffset);

        // 验证文件内容
        var written = new byte[data.Length];
        RandomAccess.Read(handle, written, 0);
        Assert.Equal(data, written);
    }

    // ========== Test 2: Skip4_HeadLenNotInCrc ==========
    /// <summary>skipBytes=4，写入 8 字节，验证 CRC 只覆盖后 4 字节</summary>
    [Fact]
    public void Skip4_HeadLenNotInCrc() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] data = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: 4);

        // Act
        sink.Push(data);

        // Assert - CRC 只覆盖后 4 字节
        uint expectedCrc = ComputeExpectedCrc(data.AsSpan(4)); // [0x05, 0x06, 0x07, 0x08]
        Assert.Equal(expectedCrc, sink.GetCrc());
        Assert.Equal(8, sink.TotalWritten);
        Assert.Equal(4, sink.CrcCoveredLength); // 只有后 4 字节参与 CRC
        Assert.Equal(8, sink.CurrentOffset);

        // 验证文件内容 - 全部 8 字节都写入
        var written = new byte[data.Length];
        RandomAccess.Read(handle, written, 0);
        Assert.Equal(data, written);
    }

    // ========== Test 3: CrossPushSkip ==========
    /// <summary>skipBytes=4，分两次 Push（2B + 6B），验证 skip 跨调用正确</summary>
    [Fact]
    public void CrossPushSkip() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] chunk1 = [0x01, 0x02];          // 2 bytes - 全部 skipped
        byte[] chunk2 = [0x03, 0x04, 0x05, 0x06, 0x07, 0x08]; // 6 bytes - 前 2B skipped, 后 4B in CRC
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: 4);

        // Act
        sink.Push(chunk1);

        // 中间状态验证
        Assert.Equal(2, sink.TotalWritten);
        Assert.Equal(0, sink.CrcCoveredLength); // 全部被 skip
        Assert.Equal(2, sink.CurrentOffset);

        sink.Push(chunk2);

        // Assert - CRC 只覆盖 [0x05, 0x06, 0x07, 0x08]
        byte[] crcInput = [0x05, 0x06, 0x07, 0x08];
        uint expectedCrc = ComputeExpectedCrc(crcInput);
        Assert.Equal(expectedCrc, sink.GetCrc());
        Assert.Equal(8, sink.TotalWritten);
        Assert.Equal(4, sink.CrcCoveredLength); // 8 - 4(skipped)
        Assert.Equal(8, sink.CurrentOffset);

        // 验证文件内容
        var written = new byte[8];
        RandomAccess.Read(handle, written, 0);
        byte[] expected = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        Assert.Equal(expected, written);
    }

    // ========== Test 4: TotalWrittenLessThanSkip ==========
    /// <summary>skipBytes=10，写入 3B，验证 CrcCoveredLength == 0</summary>
    [Fact]
    public void TotalWrittenLessThanSkip() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] data = [0xAA, 0xBB, 0xCC]; // 3 bytes
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: 10);

        // Act
        sink.Push(data);

        // Assert
        Assert.Equal(3, sink.TotalWritten);
        Assert.Equal(0, sink.CrcCoveredLength); // 3 < 10, so covered = 0

        // 验证文件内容仍然正确写入
        var written = new byte[3];
        RandomAccess.Read(handle, written, 0);
        Assert.Equal(data, written);
    }

    // ========== Test 5: FileContentCorrect ==========
    /// <summary>验证无论 skipBytes 值如何，文件内容都正确写入</summary>
    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    public void FileContentCorrect(int crcSkipBytes) {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] data = new byte[32];
        new Random(42).NextBytes(data);
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: crcSkipBytes);

        // Act
        sink.Push(data);

        // Assert - 文件内容完整写入
        var written = new byte[data.Length];
        RandomAccess.Read(handle, written, 0);
        Assert.Equal(data, written);

        // TotalWritten 始终等于数据长度
        Assert.Equal(data.Length, sink.TotalWritten);
    }

    // ========== Additional Tests ==========

    /// <summary>验证空 Push 不影响状态</summary>
    [Fact]
    public void EmptyPush_NoOp() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: 4);

        // Act
        sink.Push(ReadOnlySpan<byte>.Empty);

        // Assert
        Assert.Equal(0, sink.TotalWritten);
        Assert.Equal(0, sink.CrcCoveredLength);
        Assert.Equal(0, sink.CurrentOffset);
    }

    /// <summary>验证带 startOffset 的写入位置正确</summary>
    [Fact]
    public void StartOffset_WritesAtCorrectPosition() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        // 预填充
        byte[] prefix = [0xFF, 0xFF, 0xFF, 0xFF];
        RandomAccess.Write(handle, prefix, 0);

        byte[] data = [0x01, 0x02, 0x03, 0x04];
        var sink = new RbfWriteSink(handle, startOffset: 4, crcSkipBytes: 0);

        // Act
        sink.Push(data);

        // Assert
        Assert.Equal(4, sink.TotalWritten);
        Assert.Equal(8, sink.CurrentOffset); // 4 + 4

        // 验证文件内容
        var all = new byte[8];
        RandomAccess.Read(handle, all, 0);
        byte[] expected = [0xFF, 0xFF, 0xFF, 0xFF, 0x01, 0x02, 0x03, 0x04];
        Assert.Equal(expected, all);
    }

    /// <summary>验证多次 Push 累积 CRC</summary>
    [Fact]
    public void MultiplePush_CumulativeCrc() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] chunk1 = [0x01, 0x02, 0x03, 0x04];
        byte[] chunk2 = [0x05, 0x06, 0x07, 0x08];
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: 0);

        // Act
        sink.Push(chunk1);
        sink.Push(chunk2);

        // Assert - CRC 覆盖所有 8 字节
        byte[] allData = [0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08];
        uint expectedCrc = ComputeExpectedCrc(allData);
        Assert.Equal(expectedCrc, sink.GetCrc());
        Assert.Equal(8, sink.TotalWritten);
        Assert.Equal(8, sink.CrcCoveredLength);
    }

    /// <summary>验证 crcSkipBytes 参数校验（负数抛出异常）</summary>
    [Fact]
    public void Constructor_NegativeCrcSkipBytes_Throws() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        // Act & Assert
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: -1));
        Assert.Equal("crcSkipBytes", ex.ParamName);
    }

    /// <summary>验证 file 参数校验（null 抛出异常）</summary>
    [Fact]
    public void Constructor_NullFile_Throws() {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new RbfWriteSink(null!, startOffset: 0, crcSkipBytes: 0));
        Assert.Equal("file", ex.ParamName);
    }

    /// <summary>验证 skipBytes 恰好等于数据长度时的边界情况</summary>
    [Fact]
    public void SkipExactlyEqualsDataLength() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] data = [0x01, 0x02, 0x03, 0x04];
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: 4);

        // Act
        sink.Push(data);

        // Assert - 全部被 skip，CrcCoveredLength = 0
        Assert.Equal(4, sink.TotalWritten);
        Assert.Equal(0, sink.CrcCoveredLength);

        // 文件内容仍写入
        var written = new byte[4];
        RandomAccess.Read(handle, written, 0);
        Assert.Equal(data, written);
    }

    /// <summary>验证跨 Push skip 的边界：第一次 Push 正好消耗完 skip 配额</summary>
    [Fact]
    public void CrossPushSkip_ExactBoundary() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] chunk1 = [0x01, 0x02, 0x03, 0x04]; // 4 bytes - 恰好消耗 skip 配额
        byte[] chunk2 = [0x05, 0x06, 0x07, 0x08]; // 4 bytes - 全部参与 CRC
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: 4);

        // Act
        sink.Push(chunk1);
        Assert.Equal(0, sink.CrcCoveredLength); // 全部被 skip

        sink.Push(chunk2);

        // Assert - CRC 只覆盖 chunk2
        uint expectedCrc = ComputeExpectedCrc(chunk2);
        Assert.Equal(expectedCrc, sink.GetCrc());
        Assert.Equal(8, sink.TotalWritten);
        Assert.Equal(4, sink.CrcCoveredLength);
    }

    /// <summary>验证大数据写入的 CRC 正确性</summary>
    [Fact]
    public void LargeData_CrcCorrect() {
        // Arrange
        var path = GetTempFilePath();
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.ReadWrite);

        byte[] data = new byte[64 * 1024]; // 64KB
        new Random(12345).NextBytes(data);
        int skipBytes = 1024;
        var sink = new RbfWriteSink(handle, startOffset: 0, crcSkipBytes: skipBytes);

        // Act
        sink.Push(data);

        // Assert
        uint expectedCrc = ComputeExpectedCrc(data.AsSpan(skipBytes));
        Assert.Equal(expectedCrc, sink.GetCrc());
        Assert.Equal(data.Length, sink.TotalWritten);
        Assert.Equal(data.Length - skipBytes, sink.CrcCoveredLength);

        // 验证文件内容
        var written = new byte[data.Length];
        RandomAccess.Read(handle, written, 0);
        Assert.Equal(data, written);
    }
}
