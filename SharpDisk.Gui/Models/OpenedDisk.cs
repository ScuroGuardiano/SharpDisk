using SharpDisk.Core.Mbr;

namespace SharpDisk.Gui.Models;

/// <summary>
/// What kind of MBR the opened disk turned out to have.
/// </summary>
public enum MbrLayoutKind
{
    /// <summary>Every entry is zeroed - the disk has no partitions.</summary>
    Empty,

    /// <summary>A lone 0xEE entry: the real table is GPT and this only keeps old tools away.</summary>
    Protective,

    /// <summary>A 0xEE entry alongside real ones, the way bootable ISO images are built.</summary>
    Hybrid,

    /// <summary>An ordinary MBR with real partitions and no 0xEE entry.</summary>
    Regular,
}

/// <summary>
/// A snapshot of one disk: where it came from, its geometry, its partition table and what the
/// analyzer made of it.
/// </summary>
/// <remarks>
/// A snapshot, not a handle - the stream is read and closed during opening, so nothing here holds
/// a file descriptor on a device. Writing will need to reopen; see DOCS.MD.
/// </remarks>
public sealed class OpenedDisk
{
    public required DiskSource Source { get; init; }

    public required SharpDisk.Core.DriveInfo DriveInfo { get; init; }

    /// <summary>The whole 512-byte sector 0, exactly as it was read.</summary>
    public required byte[] RawMbr { get; init; }

    public required MbrManager Manager { get; init; }

    public required MbrAnalyzeResult Analysis { get; init; }

    /// <summary>When the disk was opened.</summary>
    public DateTimeOffset OpenedAt { get; } = DateTimeOffset.Now;

    public MbrPartitionTable Table => Manager.CurrentPartitionTable;

    public ulong SizeInBytes => DriveInfo.SizeInLba * DriveInfo.SectorSizeInBytes;

    /// <summary>
    /// Whether the 0x55 0xAA signature at the end of the sector is intact. When it is not, the
    /// partition entries were still parsed but are not worth trusting.
    /// </summary>
    public bool HasValidSignature => !Analysis.TableErrors.HasFlag(MbrErrors.InvalidSignature);

    public MbrLayoutKind LayoutKind => Analysis switch
    {
        { IsEmpty: true } => MbrLayoutKind.Empty,
        { IsProtective: true } => MbrLayoutKind.Protective,
        { IsHybrid: true } => MbrLayoutKind.Hybrid,
        _ => MbrLayoutKind.Regular,
    };

    /// <summary>
    /// Whether it makes sense to draw a partition map: only layouts that actually describe
    /// partitions in the MBR itself do.
    /// </summary>
    public bool HasPartitionMap => LayoutKind is MbrLayoutKind.Regular or MbrLayoutKind.Hybrid;
}
