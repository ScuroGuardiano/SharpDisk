using SharpDisk.Core.Attributes;

namespace SharpDisk.Core.Mbr;

[Flags]
public enum MbrPartitionErrors : uint
{
    None = 0,
    
    /// <summary>
    /// Boot flag should be 0x00 or 0x80, everything else makes it invalid
    /// </summary>
    [ErrorDescription("Invalid boot flag", "The boot flag byte is neither 0x00 (inactive) nor 0x80 (active). Any other value makes the entry invalid and its boot state undefined.", Severity.Error)]
    InvalidBootFlag = 1 << 1,
    
    /// <summary>
    /// Partition overlaps with different partition
    /// </summary>
    [ErrorDescription("Overlapping partitions", "This partition shares sectors with another partition. Writing to either one can destroy data in the other, so the overlap must be resolved before the disk is used.", Severity.Error)]
    PartitionOverlap = 1 << 2,
    
    /// <summary>
    /// Partition type is non zero but it's length is 0
    /// </summary>
    [ErrorDescription("Zero length partition", "The entry declares a partition type but its length is 0 sectors. The partition cannot hold anything and most tools will treat the entry as broken.", Severity.Error)]
    ZeroLengthPartition = 1 << 3,
    
    /// <summary>
    /// Sectors starts at 0, where the partition table is located
    /// </summary>
    [ErrorDescription("Invalid start sector", "The partition starts at LBA 0, which is where the partition table itself lives. Using this partition would overwrite the MBR.", Severity.Error)]
    InvalidStartSector = 1 << 4,
    
    /// <summary>
    /// Start sector is larger than drive's sector count
    /// </summary>
    [ErrorDescription("Start sector past end of disk", "The partition starts beyond the last sector of the drive, so it points at storage that does not exist.", Severity.Error)]
    StartSectorOverflow = 1 << 5,
    
    /// <summary>
    /// Partition size overflows the drive
    /// </summary>
    [ErrorDescription("Partition extends past disk end", "The partition starts inside the drive but its size runs past the last sector. Any access near the end of the partition will fail.", Severity.Error)]
    PartitionOverflow = 1 << 6,

    /// <summary>
    /// Partition type is "Empty" but it doesn't have zeroed fields
    /// </summary>
    [ErrorDescription("Empty entry not zeroed", "The entry has the Empty partition type but its remaining fields are not zeroed. It describes no partition, yet leftover values may confuse other tools.", Severity.Warning)]
    EmptyPartitionNotZeroed = 1 << 7,

    /// <summary>
    /// In case of CHS sector equals to 0 or not (1023, 255, 63) in case of ProtectiveMBR
    /// </summary>
    [ErrorDescription("Invalid CHS address", "The CHS address is malformed: its sector field is 0, or for a protective MBR it is not the expected (1023, 255, 63) value. Modern systems use LBA, so this normally only affects legacy BIOS tools.", Severity.Warning)]
    InvalidChsAddress = 1 << 8,
    
    /// <summary>
    /// Reserved for the future, for now I don't care about CHS too much
    /// </summary>
    [ErrorDescription("CHS and LBA mismatch", "The CHS address does not describe the same location as the LBA fields. Modern systems rely on LBA, so this mainly affects legacy BIOS tools. Reserved for future use and not reported yet.", Severity.Warning)]
    ChsLbaMismatch = 1 << 9,
    
    /// <summary>
    /// Start sector is not aligned to 4K. Grok says it's a performance loss xD
    /// </summary>
    [ErrorDescription("Unaligned start (4 KiB)", "The partition does not start on a 4 KiB boundary. It will work, but on drives with 4 KiB physical sectors this costs read and write performance.", Severity.Warning)]
    Unaligned4KStart = 1 << 10,

    /// <summary>
    /// Windows Vista+ convention.
    /// </summary>
    [ErrorDescription("Unaligned start (1 MiB)", "The partition does not start on a 1 MiB boundary, the convention used by Windows Vista and later. It will work, but may perform worse and differ from what other tools expect.", Severity.Warning)]
    Unaligned1MStart = 1 << 11,

    /// <summary>
    /// By convention or something first partition should be started 1MiB from the disk start.
    /// This is to leave some space for additional bootloader code and stuff like that.
    /// </summary>
    [ErrorDescription("First partition too close to start", "By convention the first partition starts 1 MiB into the disk to leave room for extra boot loader code. This one starts earlier, so installing a boot loader may not fit.", Severity.Warning)]
    FirstPartitionTooClose = 1 << 12,

    /// <summary>
    /// So, MBR standard doesn't enforce order of partition.
    /// You can have partition 1 physically further away than partition 2 xD
    /// Useful to display as some kind of warning
    /// </summary>
    [ErrorDescription("Partitions not in order", "The partition entries are not in the same order as their positions on the disk. MBR does not require any order, so this is only worth knowing about.", Severity.Info)]
    PartitionNotInOrder = 1 << 13,

    /// <summary>
    /// Protective partition (0xEE) is not first.
    /// </summary>
    [ErrorDescription("Protective partition not first", "The protective (0xEE) partition is not in the first slot of the table. Some tools and firmware only look at the first entry to detect a GPT disk.", Severity.Warning)]
    ProtectivePartitionNotFirst = 1 << 14,

    /// <summary>
    /// Protective partition (0xEE) is bootable. It shouldn't be.
    /// </summary>
    [ErrorDescription("Protective partition is bootable", "The protective (0xEE) partition is marked active. It should never be bootable, and some firmware may behave unexpectedly because of it.", Severity.Warning)]
    ProtectivePartitionBootable = 1 << 15,

    /// <summary>
    /// First Lba should be 1, if it's not it's invalid.
    /// Purpose of this partition is to <b>protect</b> GPT and the rest of the drive
    /// So it must start from Lba 1, coz this is where GPT starts
    /// </summary>
    [ErrorDescription("Protective first LBA invalid", "The protective (0xEE) partition must start at LBA 1, where the GPT header lives, so that it protects the GPT and the rest of the drive. This one starts somewhere else, leaving the GPT unprotected.", Severity.Error)]
    ProtectiveFirstLbaInvalid = 1 << 16,

    /// <summary>
    /// SizeInLBA should cover entire drive or be 0xFFFFFFFF in case of uint overflow.
    /// </summary>
    [ErrorDescription("Protective size invalid", "The protective (0xEE) partition should span the whole drive, or be 0xFFFFFFFF when the sector count does not fit in 32 bits. Its size is neither, so part of the GPT disk is left unprotected.", Severity.Error)]
    ProtectiveSizeInvalid = 1 << 17,

    /// <summary>
    /// FirstCHS should be 00 02 00 - <see cref="CHSAddress.Second"/>
    /// </summary>
    [ErrorDescription("Protective first CHS invalid", "The first CHS address of the protective (0xEE) partition should be 00 02 00, the second sector of the disk. Modern systems use LBA, so this only affects legacy tools.", Severity.Warning)]
    ProtectiveFirstChsInvalid = 1 << 18,

    /// <summary>
    /// LastCHS should be equal to the last block of drive or FF FF FF - <see cref="CHSAddress.ProtectiveMbr"/>
    /// </summary>
    [ErrorDescription("Protective end CHS invalid", "The last CHS address of the protective (0xEE) partition should point at the last block of the drive, or be FF FF FF when it cannot be represented. Modern systems use LBA, so this only affects legacy tools.", Severity.Warning)]
    ProtectiveEndChsInvalid = 1 << 19,

    /// <summary>
    /// Size of protective partition is equal to <see cref="uint.MaxValue"/> - 1, but drive is smaller than that.
    /// </summary>
    [ErrorDescription("Protective size saturated", "The protective (0xEE) partition uses the saturated size 0xFFFFFFFF even though the drive is small enough for its real sector count to fit. It still covers the whole disk, but the value is larger than it should be.", Severity.Warning)]
    ProtectiveSizeSaturated = 1 << 20,

    /// <summary>
    /// Hybrid MBR only: the area covered by the 0xEE entry runs past the end of the drive,
    /// or the entry has no length at all. In a hybrid its size is not predictable
    /// (gdisk sizes it up to the first hybridised partition), so staying in bounds
    /// is all we can demand of it.
    /// </summary>
    [ErrorDescription("Protective entry out of bounds", "Hybrid MBR only: the area covered by the 0xEE entry runs past the end of the drive, or the entry has no length at all. A hybrid entry may be sized freely (gdisk sizes it up to the first hybridised partition), but it must stay within the disk.", Severity.Error)]
    ProtectiveOutOfBounds = 1 << 21,
}
