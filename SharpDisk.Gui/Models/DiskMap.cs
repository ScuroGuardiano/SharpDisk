using SharpDisk.Core;
using SharpDisk.Core.Mbr;

namespace SharpDisk.Gui.Models;

public enum DiskSegmentKind
{
    /// <summary>A partition entry from the table.</summary>
    Partition,

    /// <summary>A run of sectors no partition claims.</summary>
    Free,
}

/// <summary>
/// One block on the disk map: a partition, or a gap between partitions.
/// </summary>
/// <remarks>
/// <see cref="StartFraction"/> and <see cref="WidthFraction"/> are the whole point - they are
/// fractions of the entire disk, ready to become CSS <c>left</c> and <c>width</c> percentages, so
/// the component draws and the builder does the arithmetic.
/// </remarks>
public sealed record DiskSegment
{
    public required DiskSegmentKind Kind { get; init; }

    /// <summary>
    /// Which row of the map this belongs on. Row 0 is the disk itself; further rows exist only
    /// because MBR lets partitions overlap and a protective entry cover everything.
    /// </summary>
    public required int Lane { get; init; }

    /// <summary>Table slot, 1-4. Null for free space.</summary>
    public int? Slot { get; init; }

    public required uint FirstLba { get; init; }

    public required uint LbaCount { get; init; }

    public required ulong SizeInBytes { get; init; }

    /// <summary>Position of the left edge, 0..1 of the whole disk.</summary>
    public required double StartFraction { get; init; }

    /// <summary>Width, 0..1 of the whole disk. Clamped at the end of the disk.</summary>
    public required double WidthFraction { get; init; }

    public byte PartitionType { get; init; }

    /// <summary>Localized type name, e.g. "Linux Filesystem". Empty for free space.</summary>
    public string TypeName { get; init; } = string.Empty;

    public bool Bootable { get; init; }

    /// <summary>The 0xEE entry, drawn as an umbrella over the rest rather than inline.</summary>
    public bool Protective { get; init; }

    /// <summary>The entry runs past the last sector of the disk; the drawn width was clamped.</summary>
    public bool ExtendsBeyondDisk { get; init; }

    /// <summary>CSS colour for the block.</summary>
    public string Color { get; init; } = "#9e9e9e";

    public MbrPartitionErrors Errors { get; init; }

    /// <summary>
    /// Worst severity among <see cref="Errors"/>, or <c>null</c> when the entry is clean.
    /// </summary>
    public Severity? MaxSeverity { get; init; }

    public bool HasErrors => Errors != MbrPartitionErrors.None;
}

/// <summary>
/// Everything needed to draw one disk, produced by <see cref="Services.IDiskMapBuilder"/>.
/// </summary>
public sealed record DiskMap
{
    public required IReadOnlyList<DiskSegment> Segments { get; init; }

    /// <summary>Number of rows the map needs.</summary>
    public required int LaneCount { get; init; }

    /// <summary>
    /// First row holding real partitions. Rows above it belong to protective (0xEE) entries,
    /// which cover the whole disk by design and would otherwise look like an overlap.
    /// </summary>
    public required int FirstDataLane { get; init; }

    public required ulong TotalLba { get; init; }

    public required ulong TotalBytes { get; init; }

    /// <summary>Partitions only, in table-slot order - what the legend and the detail table list.</summary>
    public IEnumerable<DiskSegment> Partitions
        => Segments.Where(s => s.Kind == DiskSegmentKind.Partition).OrderBy(s => s.Slot);

    /// <summary>True when two entries claim the same sectors, which is worth pointing out loudly.</summary>
    public bool HasOverlap => Segments.Any(segment => !segment.Protective && segment.Lane > FirstDataLane);
}
