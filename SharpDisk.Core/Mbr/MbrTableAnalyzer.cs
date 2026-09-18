using System.Diagnostics;
using System.Numerics;

namespace SharpDisk.Core.Mbr;

/// <summary>
/// Analyzer/Validator for MbrTable. It is not 100% strict tho and won't be.
/// MBR is so convoluted, there is no single standard, it's messy, half of set error flags are just warnings.
/// Hell knows how different OS-es interpret fields here.
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

        var protectiveIndex = FindProtective(table, out var protectiveCount, out var foreignCount);
        var isHybrid = protectiveIndex >= 0 && foreignCount > 0;

        result.IsProtective = protectiveIndex >= 0 && foreignCount == 0;
        result.IsHybrid = isHybrid;

        if (protectiveIndex >= 0)
        {
            AnalyzeProtective(table, protectiveIndex, protectiveCount, isHybrid, ref result);

            // A clean protective MBR has nothing else in it; every other slot is required
            // to be zero and AnalyzeProtective already verified that.
            if (!isHybrid)
            {
                return result;
            }
        }

        // Everything that is not the 0xEE partition is a real one and goes through the
        // ordinary flow: bounds, alignment, ordering, overlaps, CHS.
        AnalyzePartitions(table, protectiveIndex, ref result);

        return result;
    }

    private static bool IsUnused(in MbrPartition partition)
        => partition.PartitionType == MbrPartitionTypes.Empty;

    /// <summary>
    /// A partition that takes part in ordinary analysis: used, and not the 0xEE one.
    /// </summary>
    private static bool IsAnalyzable(in MbrPartitionTable table, int index, int protectiveIndex)
        => index != protectiveIndex && !IsUnused(table.Partitions[index]);

    /// <summary>
    /// Locates the protective partition and counts what surrounds it.
    /// </summary>
    /// <param name="protectiveCount">Number of <c>0xEE</c> partitions; more than one is malformed.</param>
    /// <param name="foreignCount">Number of used partitions that are not <c>0xEE</c>; non-zero means hybrid.</param>
    /// <returns>Index of the first <c>0xEE</c> partition, or -1 when there is none.</returns>
    private static int FindProtective(in MbrPartitionTable table, out int protectiveCount, out int foreignCount)
    {
        var index = -1;
        protectiveCount = 0;
        foreignCount = 0;

        for (var i = 0; i < table.Partitions.Length; i++)
        {
            if (table.Partitions[i].PartitionType == MbrPartitionTypes.ProtectiveMbr)
            {
                protectiveCount++;

                if (index < 0)
                {
                    index = i;
                }

                continue;
            }

            if (!IsUnused(table.Partitions[i]))
            {
                foreignCount++;
            }
        }

        return index;
    }

    /// <summary>
    /// Fills <paramref name="byTableIndex"/> with the table indices of analyzable partitions in
    /// table order, and <paramref name="byLba"/> with the same indices sorted by <c>FirstLba</c>.
    /// The sort is stable, so partitions sharing a start LBA keep their table order.
    /// </summary>
    /// <returns>Number of analyzable partitions.</returns>
    private static int BuildPhysicalOrder(
        in MbrPartitionTable table,
        int protectiveIndex,
        Span<int> byTableIndex,
        Span<int> byLba)
    {
        var count = 0;

        for (var i = 0; i < table.Partitions.Length; i++)
        {
            if (!IsAnalyzable(table, i, protectiveIndex))
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

    private void AnalyzePartitions(in MbrPartitionTable table, int protectiveIndex, ref MbrAnalyzeResult result)
    {
        Debug.Assert(table.Partitions.Length == PartitionCount);

        // Overlaps between real partitions. The 0xEE partition is excluded: in a hybrid it is
        // expected to abut or cover the hybridised regions, so pairing it here would report
        // an overlap on every correctly built hybrid MBR.
        for (var i = 0; i < table.Partitions.Length; i++)
        {
            if (!IsAnalyzable(table, i, protectiveIndex))
            {
                continue;
            }

            for (var j = i + 1; j < table.Partitions.Length; j++)
            {
                if (!IsAnalyzable(table, j, protectiveIndex))
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
        var usedCount = BuildPhysicalOrder(table, protectiveIndex, byTableIndex, byLba);

        // -1 when every partition is unused, so none can match it.
        var firstPhysicalIndex = usedCount > 0 ? byLba[0] : -1;

        for (var i = 0; i < table.Partitions.Length; i++)
        {
            // The 0xEE partition has its own rules and was already analyzed.
            if (i == protectiveIndex)
            {
                continue;
            }

            result.PartitionErrors[i] |= AnalyzePartition(table.Partitions[i], i == firstPhysicalIndex);
        }

        // Table slot k holds the k-th used partition; it should also be the k-th by start LBA.
        // The error is attributed to the slot that is out of place, not to the partition that
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
        for (var i = 0; i < table.Partitions.Length; i++)
        {
            if (IsAnalyzable(table, i, protectiveIndex) &&
                table.Partitions[i].Bootable == MbrBootable.Bootable)
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

        // LastChs addresses the last sector, inclusive. A zero-length partition has no
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
    /// Validates the <c>0xEE</c> partition. Against the full UEFI Specification section 5.2.2,
    /// table "Protective MBR Partition Record", when it is the whole story - against the
    /// relaxed hybrid rules when it is not.
    /// </summary>
    /// <remarks>
    /// Assumes the caller already located the <c>0xEE</c> partition.
    /// The signature check stays in <c>Analyze</c>, since it is common to every table.
    /// </remarks>
    private void AnalyzeProtective(
        in MbrPartitionTable table,
        int protectiveIndex,
        int protectiveCount,
        bool isHybrid,
        ref MbrAnalyzeResult result)
    {
        if (protectiveCount > 1)
        {
            result.TableErrors |= MbrErrors.MultipleProtectiveEntries;
        }

        // The specification places the protective partition in the first slot.
        if (protectiveIndex != 0)
        {
            result.PartitionErrors[protectiveIndex] |= MbrPartitionErrors.ProtectivePartitionNotFirst;
        }

        result.PartitionErrors[protectiveIndex] |= isHybrid
            ? AnalyzeHybridProtectivePartition(table.Partitions[protectiveIndex])
            : AnalyzeStrictProtectivePartition(table.Partitions[protectiveIndex]);

        // Only a clean protective MBR requires the remaining slots to be zero-filled.
        // In a hybrid they carry real partitions by design.
        if (isHybrid)
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

    /// <summary>
    /// Full UEFI Specification 5.2.2 rules, applicable only when the 0xEE partition is the whole story.
    /// </summary>
    private MbrPartitionErrors AnalyzeStrictProtectivePartition(in MbrPartition partition)
    {
        var errors = MbrPartitionErrors.None;

        // Bootable: 0x00. Not merely "not 0x80" - the field must be zero.
        if (partition.Bootable != MbrBootable.NonBootable)
        {
            errors |= MbrPartitionErrors.ProtectivePartitionBootable;
        }

        // FirstLba: 1, the location of the GPT header.
        if (partition.FirstLba != 1)
        {
            errors |= MbrPartitionErrors.ProtectiveFirstLbaInvalid;
        }

        // LbaCount: the whole disk minus the MBR itself, saturated at 32 bits.
        // A zero-sized drive has no valid size to expect, so nothing can match.
        var expectedSize = _driveLbaCount == 0 || _driveLbaCount - 1 > ProtectiveSizeSaturation
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

        // FirstChs: the literal constant 00 02 00, not a value derived from geometry.
        // It holds on 4Kn media too, which is why this is checked outside _chsMeaningful.
        if (partition.FirstChs != CHSAddress.Second)
        {
            errors |= MbrPartitionErrors.ProtectiveFirstChsInvalid;
        }

        if (_driveLbaCount == 0)
        {
            // Nothing to address, so only the FF FF FF marker can be right here.
            errors |= partition.LastChs == CHSAddress.ProtectiveMbr
                ? MbrPartitionErrors.None
                : MbrPartitionErrors.ProtectiveEndChsInvalid;

            return errors;
        }

        errors |= CheckProtectiveEndChs(partition, _driveLbaCount - 1);

        return errors;
    }

    /// <summary>
    /// Relaxed rules for the 0xEE partition inside a hybrid MBR. It no longer stands for
    /// the whole disk - gdisk sizes it to cover LBA 1 up to the first hybridised partition -
    /// so its size cannot be predicted. What remains checkable is that it is not bootable,
    /// starts where the GPT header lives, and stays inside the disk.
    /// </summary>
    private MbrPartitionErrors AnalyzeHybridProtectivePartition(in MbrPartition partition)
    {
        var errors = MbrPartitionErrors.None;

        if (partition.Bootable != MbrBootable.NonBootable)
        {
            errors |= MbrPartitionErrors.ProtectivePartitionBootable;
        }

        if (partition.FirstLba != 1)
        {
            errors |= MbrPartitionErrors.ProtectiveFirstLbaInvalid;
        }

        if (partition.LbaCount == 0 ||
            (ulong)partition.FirstLba + partition.LbaCount > _driveLbaCount)
        {
            errors |= MbrPartitionErrors.ProtectiveOutOfBounds;
        }

        if (partition.FirstChs != CHSAddress.Second)
        {
            errors |= MbrPartitionErrors.ProtectiveFirstChsInvalid;
        }

        if (partition.LbaCount > 0)
        {
            errors |= CheckProtectiveEndChs(partition, (ulong)partition.FirstLba + partition.LbaCount - 1);
        }

        return errors;
    }

    /// <summary>
    /// LastChs of a protective partition is the CHS of its last block, or FF FF FF when that
    /// block is not representable.
    /// </summary>
    /// <remarks>
    /// According to a clanker, saturated here is always acceptable.
    /// According to spec: Set to the CHS address of the last logical block on the disk.
    /// Set to 0xFFFFFF if it is not possible to represent the value in this field.
    /// I mean, technically we can say that we can't represent value in this field because CHS is fucking obsolete
    /// and doesn't matter anything anymore, since we don't know geometry of the drive.
    /// </remarks>
    private MbrPartitionErrors CheckProtectiveEndChs(in MbrPartition partition, ulong lastLba)
    {
        if (partition.LastChs == CHSAddress.ProtectiveMbr)
        {
            return MbrPartitionErrors.None;
        }

        if (!_chsMeaningful || lastLba >= _chsLimitLba || partition.LastChs != LbaToChs(lastLba))
        {
            return MbrPartitionErrors.ProtectiveEndChsInvalid;
        }

        return MbrPartitionErrors.None;
    }
}
