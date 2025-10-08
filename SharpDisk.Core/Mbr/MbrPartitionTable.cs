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

    public ImmutableArray<byte> BootSignature { get; } = [0x55, 0xAA];

    public byte[] ToBinary()
    {
        var data = new byte[512];
        var partitionsBin = data.AsMemory(446);

        foreach (var partition in Partitions)
        {
            partition.ToBinary(partitionsBin);
            partitionsBin = partitionsBin[16..];
        }
        
        data[510] = BootSignature[0];
        data[511] = BootSignature[1];

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
            _partitions = partitionsBuilder.DrainToImmutable()
        };
    }
}
