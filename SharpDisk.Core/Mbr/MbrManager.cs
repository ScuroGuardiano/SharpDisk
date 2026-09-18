namespace SharpDisk.Core.Mbr;

public class MbrManager
{
    protected MbrManager(DriveInfo driveInfo)
    {
        DriveInfo = driveInfo;
        Analyzer = new MbrTableAnalyzer(driveInfo.SizeInLba,
            driveInfo.SectorSizeInBytes,
            driveInfo.HeadsPerCylinder,
            driveInfo.SectorsPerTrack);
    }

    // I can expose this publicly, as MbrPartitionTable is completely unmutable.
    public MbrPartitionTable CurrentPartitionTable { get; private set; } = null!;
    public IReadOnlyList<MbrPartitionTable> Changelist => ChangeStack.ToArray();

    protected readonly MbrTableAnalyzer Analyzer;
    protected readonly DriveInfo DriveInfo;
    protected readonly byte[] OriginalMbrData = new byte[512];
    protected readonly Stack<MbrPartitionTable> ChangeStack = new();

    public static MbrManager CreateFromBytes(ReadOnlySpan<byte> mbrData, DriveInfo driveInfo)
    {
        var manager = new MbrManager(driveInfo);
        manager.InitFromBytes(mbrData);
        return manager;
    }

    public static MbrManager CreateNew(DriveInfo driveInfo)
    {
        var manager = new MbrManager(driveInfo);
        manager.InitNewTable();
        return manager;
    }

    private void InitFromBytes(ReadOnlySpan<byte> mbrData)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(mbrData.Length, 512, nameof(mbrData));
        mbrData.CopyTo(OriginalMbrData);
        CurrentPartitionTable = MbrPartitionTable.FromBinary(OriginalMbrData);
        ChangeStack.Push(CurrentPartitionTable);
    }

    private void InitNewTable()
    {
        // Constructor of MbrPartitionTable creates empty partition table but with valid signature.
        CurrentPartitionTable = new MbrPartitionTable();
        CurrentPartitionTable.ToBinary().CopyTo(OriginalMbrData);
        ChangeStack.Push(CurrentPartitionTable);
    }
}
