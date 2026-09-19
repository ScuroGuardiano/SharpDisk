using SharpDisk.Core.Mbr;
using SharpDisk.Gui.Models;

namespace SharpDisk.Gui.Services;

/// <summary>
/// Lays a partition table out along the disk, the way GParted or Windows Disk Management draw it.
/// </summary>
/// <remarks>
/// Two things stop this from being a simple left-to-right walk.
/// <para>
/// First, MBR lets entries overlap - nothing in the format forbids it, and a damaged table
/// routinely does. So partitions are packed into <i>lanes</i>: each one goes on the first row
/// where it does not collide with what is already there. One row means a healthy disk; a second
/// row is an overlap the user can see rather than read about.
/// </para>
/// <para>
/// Second, a hybrid MBR's 0xEE entry covers the entire disk by design, so laying it out inline
/// would push every real partition onto its own row. Protective entries therefore get their own
/// rows above the map, drawn as an umbrella rather than as a block.
/// </para>
/// Free space is computed only on the first real lane; on the rows above it, gaps are just the
/// absence of an overlap and mean nothing.
/// </remarks>
public sealed class DiskMapBuilder : IDiskMapBuilder
{
    public DiskMap Build(OpenedDisk disk)
    {
        ArgumentNullException.ThrowIfNull(disk);

        // A zero-sector disk would turn every fraction into a division by zero. It cannot happen
        // for anything openable, but the map must not be the thing that crashes if it does.
        var total = Math.Max(disk.DriveInfo.SizeInLba, 1UL);
        var sectorSize = disk.DriveInfo.SectorSizeInBytes;
        var analysis = disk.Analysis;
        var errors = new[]
        {
            analysis.Partition1Errors,
            analysis.Partition2Errors,
            analysis.Partition3Errors,
            analysis.Partition4Errors,
        };

        var entries = disk.Table.Partitions
            .Select((partition, index) => (Partition: partition, Slot: index + 1))
            .Where(entry => !entry.Partition.IsZeroed)
            .ToList();

        var segments = new List<DiskSegment>();
        var lane = 0;

        foreach (var entry in entries.Where(e => e.Partition.PartitionType == MbrPartitionTypes.ProtectiveMbr))
        {
            segments.Add(Describe(entry.Partition, entry.Slot, lane++, total, sectorSize, errors));
        }

        var firstRealLane = lane;
        var laneEnds = new List<ulong>();

        foreach (var entry in entries
                     .Where(e => e.Partition.PartitionType != MbrPartitionTypes.ProtectiveMbr)
                     .OrderBy(e => e.Partition.FirstLba)
                     .ThenBy(e => e.Slot))
        {
            ulong start = entry.Partition.FirstLba;
            ulong end = start + entry.Partition.LbaCount;

            var index = laneEnds.FindIndex(laneEnd => laneEnd <= start);
            if (index < 0)
            {
                laneEnds.Add(end);
                index = laneEnds.Count - 1;
            }
            else
            {
                laneEnds[index] = end;
            }

            segments.Add(Describe(entry.Partition, entry.Slot, firstRealLane + index, total, sectorSize, errors));
        }

        segments.AddRange(FreeSpace(segments, firstRealLane, total, sectorSize));

        return new DiskMap
        {
            Segments = segments,
            LaneCount = Math.Max(firstRealLane + Math.Max(laneEnds.Count, 1), 1),
            FirstDataLane = firstRealLane,
            TotalLba = total,
            TotalBytes = total * sectorSize,
        };
    }

    private static IEnumerable<DiskSegment> FreeSpace(
        IReadOnlyCollection<DiskSegment> segments,
        int lane,
        ulong total,
        uint sectorSize)
    {
        var occupied = segments
            .Where(segment => segment.Lane == lane)
            .OrderBy(segment => segment.FirstLba)
            .ToList();

        var cursor = 0UL;

        foreach (var segment in occupied)
        {
            if (segment.FirstLba > cursor)
            {
                yield return Free(cursor, segment.FirstLba - cursor, lane, total, sectorSize);
            }

            cursor = Math.Max(cursor, (ulong)segment.FirstLba + segment.LbaCount);
        }

        if (cursor < total)
        {
            yield return Free(cursor, total - cursor, lane, total, sectorSize);
        }
    }

    private static DiskSegment Free(ulong firstLba, ulong lbaCount, int lane, ulong total, uint sectorSize) => new()
    {
        Kind = DiskSegmentKind.Free,
        Lane = lane,
        FirstLba = (uint)Math.Min(firstLba, uint.MaxValue),
        LbaCount = (uint)Math.Min(lbaCount, uint.MaxValue),
        SizeInBytes = lbaCount * sectorSize,
        StartFraction = (double)firstLba / total,
        WidthFraction = (double)lbaCount / total,
        Color = "transparent",
    };

    private static DiskSegment Describe(
        MbrPartition partition,
        int slot,
        int lane,
        ulong total,
        uint sectorSize,
        IReadOnlyList<MbrPartitionErrors> errors)
    {
        ulong start = partition.FirstLba;
        ulong length = partition.LbaCount;

        // An entry may start or end past the last sector - that is one of the things the analyzer
        // reports. Draw what fits and flag the rest, rather than letting it run off the component.
        var visibleStart = Math.Min(start, total);
        var visibleLength = Math.Min(length, total - visibleStart);
        var beyond = start + length > total;

        var typeInfo = MbrPartitionTypeDescriptions.GetPrimary(partition.PartitionType);

        return new DiskSegment
        {
            Kind = DiskSegmentKind.Partition,
            Lane = lane,
            Slot = slot,
            FirstLba = partition.FirstLba,
            LbaCount = partition.LbaCount,
            SizeInBytes = length * sectorSize,
            StartFraction = (double)visibleStart / total,
            WidthFraction = (double)visibleLength / total,
            PartitionType = partition.PartitionType,
            TypeName = typeInfo?.Type ?? $"Nieznany typ 0x{partition.PartitionType:X2}",
            Bootable = partition.Bootable == MbrBootable.Bootable,
            Protective = partition.PartitionType == MbrPartitionTypes.ProtectiveMbr,
            ExtendsBeyondDisk = beyond,
            Color = PartitionPalette.ColorFor(partition.PartitionType),
            Errors = errors[slot - 1],
            MaxSeverity = MbrPartitionErrorDescriptions.MaxSeverity(errors[slot - 1]),
        };
    }
}
