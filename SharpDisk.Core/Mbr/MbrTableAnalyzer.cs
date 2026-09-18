using System.Numerics;

namespace SharpDisk.Core.Mbr;

/// <summary>
/// Analyzer/Validator for MbrTable. It is not 100% strict tho and won't be.
/// MBR is so convoluted, there is no single standard, it's messy, half of set error flags are just warnings.
/// Hell knows how different OS-es interpret fields here.
/// TODO: Oh and it won't analyze Hybrid MBR correctly as for now.
/// <br/><br/>
/// The only thing I know for sure, partitions can't overlap, can't overflow the drive and table must have signature at the end.
/// <br/>
/// Hmmm or can they overlap? Validating MBR Table is no fun at all
/// </summary>
/// <remarks>
/// Parts of this analyzer were written with the help of Claude.
/// </remarks>
public class MbrTableAnalyzer
{
    public MbrTableAnalyzer(
        ulong driveLbaCount,
        uint sectorSizeInBytes,
        uint headsPerCylinder = 255,
        uint sectorsPerTrack = 63)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sectorSizeInBytes, 512u);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sectorSizeInBytes, 4096u);

        if (!BitOperations.IsPow2(sectorSizeInBytes))
        {
            throw new ArgumentException("Sector size in bytes must be a power of 2.", nameof(sectorSizeInBytes));
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(headsPerCylinder, 1u);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(headsPerCylinder, 256u);
        ArgumentOutOfRangeException.ThrowIfLessThan(sectorsPerTrack, 1u);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(sectorsPerTrack, 63u);

        _driveLbaCount = driveLbaCount;
        _4KLbaAlign = 4096 / sectorSizeInBytes;
        _1MLbaAlign = 1024 * 1024 / sectorSizeInBytes;

        _heads = headsPerCylinder;
        _sectorsPerTrack = sectorsPerTrack;
        _chsLimitLba = CylinderCount * headsPerCylinder * sectorsPerTrack;

        // CHS is defined in terms of 512-byte sectors, on 4K sectors drives the fields are noise.
        _chsMeaningful = sectorSizeInBytes == 512;
    }

    private const int PartitionCount = 4;
    private const ulong CylinderCount = 1024;
    private const uint ProtectiveSizeSaturation = uint.MaxValue;

    private readonly ulong _driveLbaCount;
    private readonly uint _4KLbaAlign;
    private readonly uint _1MLbaAlign;

    private readonly uint _heads;
    private readonly uint _sectorsPerTrack;
    private readonly ulong _chsLimitLba;
    private readonly bool _chsMeaningful;

    /// <summary>
    /// Analizes the MbrPartitionTable to find errors
    /// </summary>
    /// <param name="table"></param>
    /// <returns></returns>
    public MbrAnalyzeResult Analyze(MbrPartitionTable table)
    {
        var result = new MbrAnalyzeResult();


        if (table.Signature[0] != 0x55 || table.Signature[1] != 0xAA)
        {
            result.TableErrors |= MbrErrors.InvalidSignature;
        }

        // Further verification makes no sense if partition table is empty, so let's check it
        if (table.IsEmpty())
        {
            result.IsEmpty = true;
            return result;
        }

        if (table.IsProtective())
        {
            result.IsProtective = true;
            AnalyzeProtective(table, ref result);
            return result;
        }

        AnalyzePartitions(table, ref result);

        return result;
    }

    private static bool IsUnused(in MbrPartition partition)
        => partition.PartitionType == MbrPartitionTypes.Empty;

    /// <summary>
    /// Fills <paramref name="byTableIndex"/> with the table indices of used entries in table order,
    /// and <paramref name="byLba"/> with the same indices sorted by <c>FirstLba</c>.
    /// The sort is stable, so entries sharing a start LBA keep their table order.
    /// </summary>
    /// <returns>Number of used entries.</returns>
    private static int BuildPhysicalOrder(
        MbrPartitionTable table,
        Span<int> byTableIndex,
        Span<int> byLba)
    {
        var count = 0;

        for (var i = 0; i < table.Partitions.Length; i++)
        {
            if (IsUnused(table.Partitions[i]))
            {
                continue;
            }

            byTableIndex[count] = i;
            count++;
        }

        byTableIndex[..count].CopyTo(byLba);

        // Insertion sort: at most 4 elements, stable, no allocation, no comparer delegate.
        for (var i = 1; i < count; i++)
        {
            var key = byLba[i];
            var keyLba = table.Partitions[key].FirstLba;

            var j = i - 1;
            while (j >= 0 && table.Partitions[byLba[j]].FirstLba > keyLba)
            {
                byLba[j + 1] = byLba[j];
                j--;
            }

            byLba[j + 1] = key;
        }

        return count;
    }

    private void AnalyzePartitions(MbrPartitionTable table, ref MbrAnalyzeResult result)
    {
        // Overlaps first.
        for (var i = 0; i < table.Partitions.Length; i++)
        {
            if (IsUnused(table.Partitions[i]))
            {
                continue;
            }

            for (var j = i + 1; j < table.Partitions.Length; j++)
            {
                if (IsUnused(table.Partitions[j]))
                {
                    continue;
                }

                if (OverlapsWith(table.Partitions[i], table.Partitions[j]))
                {
                    result.PartitionErrors[i] |= MbrPartitionErrors.PartitionOverlap;
                    result.PartitionErrors[j] |= MbrPartitionErrors.PartitionOverlap;
                }
            }
        }

        Span<int> byTableIndex = stackalloc int[PartitionCount];
        Span<int> byLba = stackalloc int[PartitionCount];
        var usedCount = BuildPhysicalOrder(table, byTableIndex, byLba);

        // -1 when every entry is unused, so no partition can match it.
        var firstPhysicalIndex = usedCount > 0 ? byLba[0] : -1;

        for (var i = 0; i < table.Partitions.Length; i++)
        {
            result.PartitionErrors[i] |= AnalyzePartition(table.Partitions[i], i == firstPhysicalIndex);
        }

        // Table slot k holds the k-th used entry; it should also be the k-th by start LBA.
        // The error is attributed to the slot that is out of place, not to the entry that
        // should have been there.
        for (var k = 0; k < usedCount; k++)
        {
            if (byTableIndex[k] != byLba[k])
            {
                result.PartitionErrors[byTableIndex[k]] |= MbrPartitionErrors.PartitionNotInOrder;
            }
        }

        result.TableErrors |= result.Partition1Errors != MbrPartitionErrors.None ? MbrErrors.InvalidPartition1 : MbrErrors.None;
        result.TableErrors |= result.Partition2Errors != MbrPartitionErrors.None ? MbrErrors.InvalidPartition2 : MbrErrors.None;
        result.TableErrors |= result.Partition3Errors != MbrPartitionErrors.None ? MbrErrors.InvalidPartition3 : MbrErrors.None;
        result.TableErrors |= result.Partition4Errors != MbrPartitionErrors.None ? MbrErrors.InvalidPartition4 : MbrErrors.None;

        var activeCount = 0;
        foreach (var partition in table.Partitions)
        {
            if (partition.Bootable == MbrBootable.Bootable)
            {
                activeCount++;
            }
        }

        if (activeCount > 1)
        {
            result.TableErrors |= MbrErrors.MultipleActivePartitions;
        }
        else if (activeCount == 0)
        {
            result.TableErrors |= MbrErrors.NoActivePartitions;
        }
    }

    private static bool OverlapsWith(in MbrPartition a, in MbrPartition b)
    {
        ulong aStart = a.FirstLba;
        ulong aEnd = aStart + a.LbaCount;
        ulong bStart = b.FirstLba;
        ulong bEnd = bStart + b.LbaCount;

        return aStart < bEnd && bStart < aEnd;
    }

    private MbrPartitionErrors AnalyzePartition(MbrPartition partition, bool isFirstPartitionPhysically)
    {
        MbrPartitionErrors errors = new MbrPartitionErrors();

        if (partition.Bootable is not MbrBootable.NonBootable and not MbrBootable.Bootable)
        {
            errors |= MbrPartitionErrors.InvalidBootFlag;
        }

        if (partition.PartitionType is MbrPartitionTypes.Empty)
        {
            errors |= AnalyzeEmptyPartition(partition);
        }
        else
        {
            errors |= AnalyzeNonEmptyPartition(partition, isFirstPartitionPhysically);
        }

        return errors;
    }

    private MbrPartitionErrors AnalyzeEmptyPartition(MbrPartition partition)
    {
        MbrPartitionErrors errors = new MbrPartitionErrors();

        // IsEmpty checks if all fields are zeroed.
        if (!partition.IsZeroed)
        {
            errors |= MbrPartitionErrors.EmptyPartitionNotZeroed;
        }

        return errors;
    }

    private MbrPartitionErrors AnalyzeNonEmptyPartition(MbrPartition partition, bool isFirstPartitionPhysically)
    {
        MbrPartitionErrors errors = new MbrPartitionErrors();

        if (partition.LbaCount == 0)
        {
            errors |= MbrPartitionErrors.ZeroLengthPartition;
        }

        if (partition.FirstLba >= _driveLbaCount)
        {
            errors |= MbrPartitionErrors.StartSectorOverflow;
        }

        var lastLba = (ulong)partition.FirstLba + partition.LbaCount;

        if (lastLba > _driveLbaCount)
        {
            errors |= MbrPartitionErrors.PartitionOverflow;
        }

        if (partition.FirstLba == 0)
        {
            errors |= MbrPartitionErrors.InvalidStartSector;
        }

        if (partition.FirstLba % _4KLbaAlign != 0)
        {
            errors |= MbrPartitionErrors.Unaligned4KStart;
        }

        if (partition.FirstLba % _1MLbaAlign != 0)
        {
            errors |= MbrPartitionErrors.Unaligned1MStart;
        }

        if (isFirstPartitionPhysically && partition.FirstLba < _1MLbaAlign)
        {
            errors |= MbrPartitionErrors.FirstPartitionTooClose;
        }

        errors |= AnalyzeChs(partition);

        return errors;
    }


    /// <summary>
    /// Converts an LBA to CHS using the analyzer's geometry.
    /// Only valid for <c>lba &lt; _chsLimitLba</c>; callers must check first.
    /// </summary>
    private CHSAddress LbaToChs(ulong lba)
    {
        var cylinder = lba / (_heads * _sectorsPerTrack);
        var head = lba / _sectorsPerTrack % _heads;
        var sector = lba % _sectorsPerTrack + 1;

        return new CHSAddress((ushort)cylinder, (byte)head, (byte)sector);
    }

    private MbrPartitionErrors CheckChs(
        CHSAddress chs,
        ulong lba)
    {
        // The FF FF FF marker carries head 255, which is out of range for the usual
        // 255-head geometry, so saturation has to be recognised before the range check.
        if (!chs.IsSaturated && (chs.Sector == 0 || chs.Head >= _heads))
        {
            return MbrPartitionErrors.InvalidChsAddress;
        }

        if (lba >= _chsLimitLba)
        {
            return chs.IsSaturated ? MbrPartitionErrors.None : MbrPartitionErrors.ChsLbaMismatch;
        }

        // Below the limit a saturated marker is itself a mismatch: it never equals LbaToChs.
        return chs == LbaToChs(lba) ? MbrPartitionErrors.None : MbrPartitionErrors.ChsLbaMismatch;
    }

    private MbrPartitionErrors AnalyzeChs(in MbrPartition partition)
    {
        if (!_chsMeaningful)
        {
            return MbrPartitionErrors.None;
        }

        var errors = CheckChs(partition.FirstChs, partition.FirstLba);

        // EndingCHS addresses the last sector, inclusive. A zero-length partition has no
        // last sector; ZeroLengthPartition already covers that case.
        if (partition.LbaCount > 0)
        {
            var lastLba = (ulong)partition.FirstLba + partition.LbaCount - 1;

            errors |= CheckChs(partition.LastChs, lastLba);
        }

        return errors;
    }

    // Protective MBR

    /// <summary>
    /// Validates a protective MBR against UEFI Specification section 5.2.2,
    /// table "Protective MBR Partition Record".
    /// </summary>
    /// <remarks>
    /// Assumes the caller already established that a <c>0xEE</c> entry is present.
    /// The signature check stays in <c>Analyze</c>, since it is common to every table.
    /// </remarks>
    private void AnalyzeProtective(in MbrPartitionTable table, ref MbrAnalyzeResult result)
    {
        var protectiveIndex = -1;
        var protectiveCount = 0;
        var foreignCount = 0;

        for (var i = 0; i < table.Partitions.Length; i++)
        {
            if (table.Partitions[i].PartitionType == MbrPartitionTypes.ProtectiveMbr)
            {
                protectiveCount++;

                if (protectiveIndex < 0)
                {
                    protectiveIndex = i;
                }

                continue;
            }

            if (!IsUnused(table.Partitions[i]))
            {
                foreignCount++;
            }
        }

        if (protectiveCount > 1)
        {
            result.TableErrors |= MbrErrors.MultipleProtectiveEntries;
        }

        // A hybrid MBR describes the same sectors twice, in two tables that can drift apart.
        // It is not a protective MBR at all, so the per-entry rules below do not apply to
        // the other records - only the 0xEE one is still expected to be well formed.
        if (foreignCount > 0)
        {
            result.TableErrors |= MbrErrors.HybridMbr;
        }

        if (protectiveIndex < 0)
        {
            return;
        }

        // The specification places the protective record in the first slot.
        if (protectiveIndex != 0)
        {
            result.PartitionErrors[protectiveIndex] |= MbrPartitionErrors.ProtectivePartitionNotFirst;
        }

        result.PartitionErrors[protectiveIndex] |= AnalyzeProtectivePartition(table.Partitions[protectiveIndex]);

        // Every remaining record must be zero-filled. In a hybrid MBR they deliberately are
        // not, and HybridMbr already reports that, so do not pile a second error on top.
        if (foreignCount > 0)
        {
            return;
        }

        for (var i = 0; i < table.Partitions.Length; i++)
        {
            if (i == protectiveIndex || table.Partitions[i].IsZeroed)
            {
                continue;
            }

            result.TableErrors |= MbrErrors.ProtectiveSlotsNotZeroed;
            result.PartitionErrors[i] |= MbrPartitionErrors.EmptyPartitionNotZeroed;
        }
    }

    private MbrPartitionErrors AnalyzeProtectivePartition(in MbrPartition partition)
    {
        var errors = MbrPartitionErrors.None;

        // BootIndicator: 0x00. Not merely "not 0x80" - the field must be zero.
        if (partition.Bootable != MbrBootable.NonBootable)
        {
            errors |= MbrPartitionErrors.ProtectivePartitionBootable;
        }

        // StartingLBA: 1, the location of the GPT header.
        if (partition.FirstLba != 1)
        {
            errors |= MbrPartitionErrors.ProtectiveFirstLbaInvalid;
        }

        // SizeInLBA: the whole disk minus the MBR itself, saturated at 32 bits.
        var expectedSize = _driveLbaCount - 1 > ProtectiveSizeSaturation
            ? ProtectiveSizeSaturation
            : (uint)(_driveLbaCount - 1);

        if (partition.LbaCount != expectedSize)
        {
            // Some tools write the saturation value unconditionally. The disk is still usable,
            // so report it apart from a genuinely wrong size.
            errors |= partition.LbaCount == ProtectiveSizeSaturation
                ? MbrPartitionErrors.ProtectiveSizeSaturated
                : MbrPartitionErrors.ProtectiveSizeInvalid;
        }

        errors |= AnalyzeProtectiveChs(partition);

        return errors;
    }

    private MbrPartitionErrors AnalyzeProtectiveChs(in MbrPartition partition)
    {
        var errors = MbrPartitionErrors.None;

        // StartingCHS: the literal constant 00 02 00, not a value derived from geometry.
        // It holds on 4Kn media too, which is why this is checked outside _chsMeaningful.
        if (partition.FirstChs != CHSAddress.Second)
        {
            errors |= MbrPartitionErrors.ProtectiveFirstChsInvalid;
        }

        // According to a clanker, saturated here is always acceptable.
        // According to spec: Set to the CHS address of the last logical block on the disk. Set to 0xFFFFFF if it is not possible to represent the value in this field.
        // I mean, technically we can say that we can't represent value in this field because CHS is fucking obsolete
        // and doesn't matter anything anymore, since we don't know geometry of the drive.
        if (partition.LastChs == CHSAddress.ProtectiveMbr)
        {
            return errors;
        }

        if (!_chsMeaningful || _driveLbaCount == 0)
        {
            return errors | MbrPartitionErrors.ProtectiveEndChsInvalid;
        }

        var lastLba = _driveLbaCount - 1;

        if (lastLba >= _chsLimitLba || partition.LastChs != LbaToChs(lastLba))
        {
            errors |= MbrPartitionErrors.ProtectiveEndChsInvalid;
        }

        return errors;
    }
}
