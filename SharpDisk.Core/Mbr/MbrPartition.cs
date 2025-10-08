using System.Runtime.InteropServices;

namespace SharpDisk.Core.Mbr;

public sealed record MbrPartition
{
    public static MbrPartition CreateProtective(ulong driveSizeInSectors)
    {
        return new MbrPartition
        {
            Bootable = MbrBootable.NonBootable,
            FirstChs = CHSAddress.Second,
            PartitionType = MbrPartitionTypes.ProtectiveMbr,
            LastChs = CHSAddress.ProtectiveMBR,
            FirstLba = 1,
            LbaCount = driveSizeInSectors > uint.MaxValue ? uint.MaxValue : (uint)driveSizeInSectors,
        };
    }
    
    public static MbrPartition Empty { get; } = new MbrPartition();
    
    public MbrBootable Bootable { get; init; }
    public CHSAddress FirstChs { get; init; } = CHSAddress.Zero;
    public byte PartitionType { get; init; }
    public CHSAddress LastChs { get; init; } = CHSAddress.Zero;
    public uint FirstLba { get; init; }
    public uint LbaCount { get; init; }

    public void ToBinary(Memory<byte> target)
    {
        if (target.Length < 16)
        {
            throw new ArgumentException("Target too short, MbrParition requires 16 bytes.", nameof(target));
        }
        
        target.CopyTo(target);
        
        target.Span[0] = (byte)Bootable;
        FirstChs.ToBinary(target[1..4]);
        target.Span[4] = PartitionType;
        LastChs.ToBinary(target[5..8]);
        BitConverter.TryWriteBytes(target[8..12].Span, FirstLba);
        BitConverter.TryWriteBytes(target[12..16].Span, LbaCount);
    }

    public static MbrPartition FromBinary(ReadOnlyMemory<byte> source)
    {
        ReadOnlySpan<byte> sp = source.Span;
        var mbrPartition = new MbrPartition
        {
            Bootable = (MbrBootable)sp[0],
            FirstChs = CHSAddress.FromBinary(source[1..4]),
            PartitionType = sp[4],
            LastChs = CHSAddress.FromBinary(source[5..8]),
            FirstLba = BitConverter.ToUInt32(source[8..12].Span),
            LbaCount = BitConverter.ToUInt32(source[12..16].Span)
        };
        
        return mbrPartition;
    }
}
