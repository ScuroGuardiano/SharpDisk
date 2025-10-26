using System.Collections.Immutable;

namespace SharpDisk.Core.Mbr;

public class MbrPartitionTable
{
    // 0-445 Some fucking crap no one cares about [bootloader code]
    // 446-509 Partitions
    private ImmutableArray<MbrPartition> _partitions = [
        MbrPartition.Empty,
        MbrPartition.Empty,
        MbrPartition.Empty,
        MbrPartition.Empty,
    ]; 

    public ImmutableArray<MbrPartition> Partitions
    {
        get => _partitions;
        set
        {
            if (value.Length != 4)
            {
                throw new ArgumentException("Partition table must have 4 partitions, even if they're empty.");
            }
            _partitions = value;
        }
    }

    public ImmutableArray<byte> Signature { get; private init; } = [0x55, 0xAA];

    /// <summary>
    /// Verifies MBR table for correctness
    /// </summary>
    /// <returns></returns>
    public MbrVerificationResult Verify()
    {
        // Verifies MBR Table for correctness.
        // First of all we need to verify the signature. It should be 0x55, 0xAA

        var tableErrors = MbrErrors.None;

        if (Signature[0] != 0x55 && Signature[1] != 0xAA)
        {
            tableErrors |= MbrErrors.InvalidSignature;
        }
        
        // Further verification makes no sense if partition table is empty, so let's check it
        if (IsEmpty())
        {
            return new MbrVerificationResult()
            {
                IsEmpty = true,
                TableErrors = tableErrors
            };
        }
        
        
    }

    /// <summary>
    /// Returns whenever the partition table is empty. Partition table is empty if all of it's partitions are empty
    /// </summary>
    /// <returns></returns>
    public bool IsEmpty()
    {
        // I can use equals here, because MbrPartition is a record with only comparable types inside
        return Partitions.All(p => p == MbrPartition.Empty);
    }

    /// <summary>
    /// Returns whenever it's protective MBR. We will assume that it is protective MBR if one of it's partition
    /// has type of EFI Protective
    /// </summary>
    /// <returns></returns>
    public bool IsProtective()
    {
        return Partitions.Any(p => p.PartitionType == MbrPartitionTypes.ProtectiveMbr);
    }
    
    public byte[] ToBinary()
    {
        var data = new byte[512];
        var partitionsBin = data.AsMemory(446);

        foreach (var partition in Partitions)
        {
            partition.ToBinary(partitionsBin);
            partitionsBin = partitionsBin[16..];
        }
        
        data[510] = Signature[0];
        data[511] = Signature[1];

        return data;
    }

    public static MbrPartitionTable FromBinary(ReadOnlyMemory<byte> data)
    {
        if (data.Length < 512)
        {
            throw new ArgumentOutOfRangeException(nameof(data), "MBR partition table must be at least 512 bytes long.");
        }

        var partitionsRaw = data[446..];
        var partitionsBuilder = ImmutableArray.CreateBuilder<MbrPartition>();

        for (int i = 0; i < 4; i++)
        {
            partitionsBuilder.Add(MbrPartition.FromBinary(partitionsRaw[(i * 16)..]));
        }

        return new MbrPartitionTable
        {
            _partitions = partitionsBuilder.DrainToImmutable(),
            Signature = [data.Span[510], data.Span[511]]
        };
    }
}
