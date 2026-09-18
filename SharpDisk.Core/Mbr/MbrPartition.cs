using System.Buffers.Binary;

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
            LastChs = CHSAddress.ProtectiveMbr,
            FirstLba = 1,
            LbaCount = driveSizeInSectors > uint.MaxValue ? uint.MaxValue : (uint)driveSizeInSectors,
        };
    }
    
    public static MbrPartition Zeroed { get; } = new MbrPartition();
    
    public MbrBootable Bootable { get; init; }
    public CHSAddress FirstChs { get; init; } = CHSAddress.Zero;
    public byte PartitionType { get; init; }
    public CHSAddress LastChs { get; init; } = CHSAddress.Zero;
    public uint FirstLba { get; init; }
    public uint LbaCount { get; init; }

    public void ToBinary(Span<byte> target)
    {
        if (target.Length < 16)
        {
            throw new ArgumentException("Target too short, MbrPartition requires 16 bytes.", nameof(target));
        }

        // I don't know what I had in my head when I wrote line below but it's funny, so I must leave it here, just commented XD
        // target.CopyTo(target);

        target[0] = (byte)Bootable;
        FirstChs.ToBinary(target[1..4]);
        target[4] = PartitionType;
        LastChs.ToBinary(target[5..8]);

        // I have been gaslighted by Claude to use proper binary writing with endianess
        // Even though EVERY PLATFORM IT WILL SUPPORT IS ALREADY LITTLE ENDIAN ;-;
        // AND .NET SUPPORTS ONLY X86, X64 OR ARM, WHICH ARE LITTLE ENDIAN.
        // Alright, technically it's possible to run ARM CPU with big endian but AFAIK Linux doesn't do that
        // Anyways, looks more professional and explicit I guess.
        BinaryPrimitives.WriteUInt32LittleEndian(target[8..12], FirstLba);
        BinaryPrimitives.WriteUInt32LittleEndian(target[12..16], LbaCount);
    }

    public static MbrPartition FromBinary(ReadOnlySpan<byte> source)
    {
        var mbrPartition = new MbrPartition
        {
            Bootable = (MbrBootable)source[0],
            FirstChs = CHSAddress.FromBinary(source[1..4]),
            PartitionType = source[4],
            LastChs = CHSAddress.FromBinary(source[5..8]),
            FirstLba = BinaryPrimitives.ReadUInt32LittleEndian(source[8..12]),
            LbaCount = BinaryPrimitives.ReadUInt32LittleEndian(source[12..16])
        };
        
        return mbrPartition;
    }

    public bool IsZeroed => this == Zeroed;
}
