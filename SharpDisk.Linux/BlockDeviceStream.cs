using System.Runtime.Versioning;
using SharpDisk.Core;

namespace SharpDisk.Linux;

[SupportedOSPlatform("linux")]
public sealed class BlockDeviceStream : DriveStream
{
    public BlockDeviceStream(string path, bool writable, bool allowPartition = false)
    {
        CheckFileType(path, allowPartition);
        _file = new FileStream(path, FileMode.Open, writable ? FileAccess.ReadWrite : FileAccess.Read);
        
        Length = (long)LinuxNative.Ioctl_BLKGETSIZE64(_file);
        LbaSize = LinuxNative.Ioctl_BLKSZGET(_file);
        LbaCount = (ulong)Math.Ceiling((double)Length / LbaSize);
    }

    public override bool CanRead => _file.CanRead;
    public override bool CanSeek => _file.CanSeek;
    public override bool CanWrite => _file.CanWrite;
    public override long Length { get; }
    public override long Position { get => _file.Position; set => _file.Position = value; }
    public override ulong LbaCount { get; protected set; }
    public override ulong LbaSize { get; protected set; }
    
    private readonly FileStream _file;

    private void CheckFileType(string path, bool allowPartition)
    {
        var fileName = Path.GetFileName(path);
        var isBlock = Directory.Exists($"/sys/class/block/{fileName}");
        
        if (!isBlock)
        {
            throw new InvalidOperationException("Provided path is not a block device.");
        }
        
        var isPartition = File.Exists($"/sys/class/block/${fileName}/partition");

        if (isPartition && !allowPartition)
        {
            throw new InvalidOperationException("Provided path is a partition.");
        }
    }
    
    public override void Flush()
    {
        _file.Flush();
    }
    
    public override int Read(byte[] buffer, int offset, int count)
    {
        return _file.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        return _file.Seek(offset, origin);
    }

    public override void SetLength(long value)
    {
        throw new InvalidOperationException("FileDriveStream does not support setting file length");
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        _file.Write(buffer, offset, count);
    }

    public override void ReReadParitionTable()
    {
        Flush();
        LinuxNative.Ioctl_BLKRRPART(_file);
    }

    public override async Task ReReadParitionTableAsync()
    {
        await FlushAsync();
        await Task.Run(() => LinuxNative.Ioctl_BLKRRPART(_file));
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _file.Dispose();
        }
    }
}
