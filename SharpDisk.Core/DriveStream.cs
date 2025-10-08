namespace SharpDisk.Core;

public abstract class DriveStream : Stream
{
    public abstract ulong LbaCount { get; protected set; }
    public abstract ulong LbaSize { get; protected set; }

    public void ReadSection(long offset, Memory<byte> destination)
    {
        if (offset > Length || offset < 0 || offset + destination.Length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(destination), "Tried to read beyond the end of the stream.");
        }
        
        var oldPos = Position;
        Seek(offset, SeekOrigin.Begin);
        ReadExactly(destination.Span);
        Seek(oldPos, SeekOrigin.Begin);
    }

    public byte[] ReadSection(long offset, ulong count)
    {
        var buffer = new byte[count];
        ReadSection(offset, buffer);
        return buffer;
    }

    public async Task ReadSectionAsync(long offset, Memory<byte> destination)
    {
        if (offset > Length || offset < 0 || offset + destination.Length > Length)
        {
            throw new ArgumentOutOfRangeException(nameof(destination), "Tried to read beyond the end of the stream.");
        }
        
        var oldPos = Position;
        Seek(offset, SeekOrigin.Begin);
        await ReadExactlyAsync(destination);
        Seek(oldPos, SeekOrigin.Begin);
    }

    public async Task<byte[]> ReadSectionAsync(long offset, ulong count)
    {
        var buffer = new byte[count];
        await ReadSectionAsync(offset, buffer);
        return buffer;
    }
    
    public virtual void ReReadParitionTable() {}

    public virtual Task ReReadParitionTableAsync()
    {
        return Task.CompletedTask;
    }
}
