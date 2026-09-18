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

    // =====================================================================
    // Everything below is a stub. Signatures and intent only - the bodies
    // are yours to write.
    //
    // Shape these all share: a mutation builds a brand new MbrPartitionTable,
    // pushes it onto ChangeStack and repoints CurrentPartitionTable at it.
    // Nothing edits a table in place, which is what makes Undo a pop.
    //
    // Suggested split of responsibility, so this class does not turn into a
    // second analyzer: throw only for things that are structurally impossible
    // (slot out of range, zero length, no free slot). Everything debatable -
    // overlaps, misalignment, partitions out of physical order - is not an
    // exception, it is what Analyze() is for. The GUI shows those as warnings
    // and lets the user decide, the same way cfdisk does.
    // =====================================================================

    #region Analysis

    /// <summary>
    /// Runs <see cref="Analyzer"/> over <see cref="CurrentPartitionTable"/>.
    /// </summary>
    /// <remarks>
    /// The Analyzer field has been sitting here unused since the class was scaffolded;
    /// this is what it is for. Cheap enough to call after every mutation.
    /// </remarks>
    public MbrAnalyzeResult Analyze() => throw new NotImplementedException();

    #endregion

    #region Whole table

    /// <summary>
    /// Replaces the current table with an empty one (four zeroed partitions, valid 55 AA signature).
    /// </summary>
    /// <remarks>
    /// Does not touch boot code or the disk signature - see <see cref="ToBinary"/> on why those
    /// live outside the partition table.
    /// </remarks>
    public void CreateNewTable() => throw new NotImplementedException();

    /// <summary>
    /// Replaces the current table with a protective MBR for the whole drive.
    /// <see cref="MbrPartition.CreateProtective"/> already builds the 0xEE partition.
    /// </summary>
    public void CreateProtectiveTable() => throw new NotImplementedException();

    /// <summary>
    /// Zeroes every partition but keeps the table itself.
    /// </summary>
    public void ClearAllPartitions() => throw new NotImplementedException();

    #endregion

    #region Partitions

    /// <summary>
    /// Writes a partition into the given slot.
    /// </summary>
    /// <param name="slot">Table slot, 0-3.</param>
    /// <remarks>
    /// Decide whether writing over a slot that is already in use is allowed or should throw.
    /// CHS fields: derive them from the LBAs, saturating to <see cref="CHSAddress.TooLarge"/>
    /// past the addressable limit - the analyzer will flag a mismatch otherwise.
    /// </remarks>
    public void AddPartition(
        int slot,
        uint firstLba,
        uint lbaCount,
        byte partitionType,
        MbrBootable bootable = MbrBootable.NonBootable)
        => throw new NotImplementedException();

    /// <summary>
    /// Same as <see cref="AddPartition(int,uint,uint,byte,MbrBootable)"/> but picks the first free slot.
    /// </summary>
    /// <returns>The slot used.</returns>
    /// <exception cref="InvalidOperationException">All four slots are taken.</exception>
    public int AddPartition(
        uint firstLba,
        uint lbaCount,
        byte partitionType,
        MbrBootable bootable = MbrBootable.NonBootable)
        => throw new NotImplementedException();

    /// <summary>
    /// Zeroes the given slot.
    /// </summary>
    public void RemovePartition(int slot) => throw new NotImplementedException();

    /// <summary>
    /// Changes a partition's length, keeping FirstLba where it is.
    /// </summary>
    /// <remarks>
    /// Growing into a neighbour is not rejected here - <see cref="Analyze"/> reports the overlap.
    /// Remember to recompute LastChs.
    /// </remarks>
    public void ResizePartition(int slot, uint newLbaCount) => throw new NotImplementedException();

    /// <summary>
    /// Moves a partition to a new start sector, keeping its length.
    /// </summary>
    /// <remarks>
    /// This only rewrites the table. It does <b>not</b> relocate a single byte of partition
    /// content - whatever filesystem lives there will be left pointing at the wrong place.
    /// Worth a confirmation prompt in the UI.
    /// </remarks>
    public void MovePartition(int slot, uint newFirstLba) => throw new NotImplementedException();

    /// <summary>
    /// Changes the type byte, leaving geometry alone. See <see cref="MbrPartitionTypes"/>.
    /// </summary>
    public void SetPartitionType(int slot, byte partitionType) => throw new NotImplementedException();

    /// <summary>
    /// Marks a partition bootable, or clears the flag.
    /// </summary>
    /// <remarks>
    /// MBR allows only one active partition, so setting one should clear the other three -
    /// otherwise <see cref="MbrErrors.MultipleActivePartitions"/> fires. Pass <c>null</c> to
    /// make the whole table non-bootable.
    /// </remarks>
    public void SetActivePartition(int? slot) => throw new NotImplementedException();

    /// <summary>
    /// Swaps two table slots, leaving the partitions themselves untouched.
    /// </summary>
    /// <remarks>
    /// The fix for <see cref="MbrPartitionErrors.PartitionNotInOrder"/>: MBR does not require
    /// table order to match physical order, but tools and users expect it to.
    /// </remarks>
    public void SwapPartitions(int slotA, int slotB) => throw new NotImplementedException();

    #endregion

    #region Free space

    /// <summary>
    /// Unallocated runs on the drive, in ascending LBA order.
    /// </summary>
    /// <remarks>
    /// What the GUI needs to offer "create partition here". Sector 0 is the MBR itself and is
    /// never free; by convention the first partition starts 1 MiB in, so consider reporting the
    /// gap from the aligned start rather than from LBA 1.
    /// </remarks>
    public IReadOnlyList<MbrFreeRegion> GetFreeRegions() => throw new NotImplementedException();

    /// <summary>
    /// First free run that can hold <paramref name="minimumLbaCount"/> sectors, or null.
    /// </summary>
    public MbrFreeRegion? FindFreeRegion(uint minimumLbaCount) => throw new NotImplementedException();

    /// <summary>
    /// Rounds an LBA up to the 1 MiB boundary, the alignment Windows Vista onward and every
    /// modern Linux tool use. Derive the sector count from <see cref="DriveInfo"/>.
    /// </summary>
    public uint AlignUp(uint lba) => throw new NotImplementedException();

    /// <summary>
    /// Rounds an LBA down to the 1 MiB boundary.
    /// </summary>
    public uint AlignDown(uint lba) => throw new NotImplementedException();

    #endregion

    #region Disk signature and boot code

    /// <summary>
    /// The 4-byte disk signature at offset 440, which Windows uses to identify the drive.
    /// Zero trips <see cref="MbrErrors.ZeroDiskSignature"/>.
    /// </summary>
    /// <remarks>
    /// Lives in <see cref="OriginalMbrData"/>, not in <see cref="MbrPartitionTable"/> - the table
    /// type only models bytes 446-511.
    /// </remarks>
    public uint DiskSignature
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    /// <summary>
    /// Puts a random non-zero value in <see cref="DiskSignature"/>.
    /// </summary>
    public void GenerateDiskSignature() => throw new NotImplementedException();

    /// <summary>
    /// Bytes 0-439: the bootstrap code. Read only, since nothing in this program generates it.
    /// </summary>
    public ReadOnlySpan<byte> BootCode => throw new NotImplementedException();

    /// <summary>
    /// Zeroes the bootstrap code, leaving the partition table and disk signature intact.
    /// </summary>
    public void ClearBootCode() => throw new NotImplementedException();

    #endregion

    #region History

    public bool CanUndo => throw new NotImplementedException();

    public bool CanRedo => throw new NotImplementedException();

    /// <summary>
    /// Steps back one change.
    /// </summary>
    /// <remarks>
    /// <see cref="ChangeStack"/> alone cannot do redo - a popped table is gone. Either add a
    /// second stack that Undo pushes onto and any fresh mutation clears, or drop redo and say so.
    /// Also note the stack is seeded with the initial table in Init*, so "nothing left to undo"
    /// is Count == 1, not Count == 0.
    /// </remarks>
    public void Undo() => throw new NotImplementedException();

    /// <summary>
    /// Steps forward one change.
    /// </summary>
    public void Redo() => throw new NotImplementedException();

    /// <summary>
    /// Throws away every change and goes back to the table as it was read from the drive.
    /// </summary>
    public void Revert() => throw new NotImplementedException();

    #endregion

    #region Persistence

    /// <summary>
    /// Whether <see cref="CurrentPartitionTable"/> still matches what is on the drive.
    /// </summary>
    public bool HasUnsavedChanges => throw new NotImplementedException();

    /// <summary>
    /// The full 512-byte sector, ready to be written back.
    /// </summary>
    /// <remarks>
    /// <b>Do not just return <c>CurrentPartitionTable.ToBinary()</c>.</b> That method allocates a
    /// fresh 512-byte array and only fills 446-511, so bytes 0-445 come back zeroed - writing it
    /// to a real drive wipes the bootloader and the disk signature and leaves an unbootable
    /// machine. Start from a copy of <see cref="OriginalMbrData"/> and overlay the partition
    /// table onto it. Keeping <see cref="OriginalMbrData"/> around is the whole reason this class
    /// holds it.
    /// </remarks>
    public byte[] ToBinary() => throw new NotImplementedException();

    /// <summary>
    /// Writes <see cref="ToBinary"/> to sector 0 and marks the current state as saved.
    /// </summary>
    /// <remarks>
    /// After this the kernel still has the old table cached - call
    /// <see cref="DriveStream.ReReadParitionTable"/> so /dev nodes catch up. That ioctl fails
    /// while any partition on the drive is mounted, which is worth surfacing rather than swallowing.
    /// </remarks>
    public void Write(DriveStream stream) => throw new NotImplementedException();

    /// <inheritdoc cref="Write"/>
    public Task WriteAsync(DriveStream stream, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();

    #endregion
}
