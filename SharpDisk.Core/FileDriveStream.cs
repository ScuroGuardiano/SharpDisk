namespace SharpDisk.Core;

/// <summary>
/// Stream on raw file, so we can partition even fuckin JPEG XD With small exceptions
/// </summary>
/// <remarks>
/// <b>DO NOT</b> use it on <c>/dev/&lt;drive&gt;</c> on Linux.
/// There's implementation for that for Linux in <c>SharpDisk.Linux</c> project.
/// Gathering some drive data may require using some Linux unique stuff to do,
/// <see cref="FileDriveStream"/> is only designed to be used with regular files!
/// </remarks>
public sealed class FileDriveStream : DriveStream
{
    public FileDriveStream(string path, bool writable, ulong lbaSize = 512)
    {
        if (writable)
        {
            _file = new FileStream(path, FileMode.Open, FileAccess.ReadWrite);
            LbaSize = lbaSize;
            LbaCount = (ulong)Math.Ceiling((double)_file.Length / lbaSize);
        }
        else
        {
            _file = new FileStream(path, FileMode.Open, FileAccess.Read);
        }

        if (_file.Length < (long)lbaSize)
        {
            // To partition file we need at least one sector for MBR xD So if it's smaller then we can't partition this
            throw new InvalidOperationException("File is too small to be partitioned");
        }
    }
    
    public override bool CanRead => _file.CanRead;
    public override bool CanSeek => _file.CanSeek;
    public override bool CanWrite => _file.CanWrite;
    public override long Length => _file.Length;
    public override long Position { get => _file.Position; set => _file.Position = value; }
    public override ulong LbaCount { get; protected set; }
    public override ulong LbaSize { get; protected set; }
    private readonly FileStream _file;
    
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

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _file.Dispose();
        }
    }
}