namespace SharpDisk.Core.Mbr;

[Flags]
public enum MbrPartitionErrors : uint
{
    None = 0,
    
    /// <summary>
    /// Boot flag should be 0x00 or 0x80, everything else makes it invalid
    /// </summary>
    InvalidBootFlag = 1 << 1,
    
    /// <summary>
    /// Partition overlaps with different partition
    /// </summary>
    PartitionOverlap = 1 << 2,
    
    /// <summary>
    /// Partition type is non zero but it's length is 0
    /// </summary>
    ZeroLengthPartition = 1 << 3,
    
    /// <summary>
    /// Sectors starts at 0, where the partition table is located
    /// </summary>
    InvalidStartSector = 1 << 4,
    
    /// <summary>
    /// Start sector is larger than drive's sector count
    /// </summary>
    StartSectorOverflow = 1 << 5,
    
    /// <summary>
    /// Partition size overflows the drive
    /// </summary>
    PartitionOverflow = 1 << 6,

    /// <summary>
    /// Partition type is "Empty" but it doesn't have zeroed fields
    /// </summary>
    EmptyPartitionNotZeroed = 1 << 7,

    /// <summary>
    /// In case of CHS sector equals to 0 or not (1023, 255, 63) in case of ProtectiveMBR
    /// </summary>
    InvalidChsAddress = 1 << 8,
    
    /// <summary>
    /// Reserved for the future, for now I don't care about CHS too much
    /// </summary>
    ChsLbaMismatch = 1 << 9,
    
    /// <summary>
    /// Start sector is not aligned to 4K. Grok says it's a performance loss xD
    /// </summary>
    Unaligned4KStart = 1 << 10,

    /// <summary>
    /// Windows Vista+ convention.
    /// </summary>
    Unaligned1MStart = 1 << 11,

    /// <summary>
    /// By convention or something first partition should be started 1MiB from the disk start.
    /// This is to leave some space for additional bootloader code and stuff like that.
    /// </summary>
    FirstPartitionTooClose = 1 << 12,

    /// <summary>
    /// So, MBR standard doesn't enforce order of partition.
    /// You can have partition 1 physically further away than partition 2 xD
    /// Useful to display as some kind of warning
    /// </summary>
    PartitionNotInOrder = 1 << 13,

    /// <summary>
    /// Protective partition (0xEE) is not first.
    /// </summary>
    ProtectivePartitionNotFirst = 1 << 14,

    /// <summary>
    /// Protective partition (0xEE) is bootable. It shouldn't be.
    /// </summary>
    ProtectivePartitionBootable = 1 << 15,

    /// <summary>
    /// First Lba should be 1, if it's not it's invalid.
    /// Purpose of this partition is to <b>protect</b> GPT and the rest of the drive
    /// So it must start from Lba 1, coz this is where GPT starts
    /// </summary>
    ProtectiveFirstLbaInvalid = 1 << 16,

    /// <summary>
    /// SizeInLBA should cover entire drive or be 0xFFFFFFFF in case of uint overflow.
    /// </summary>
    ProtectiveSizeInvalid = 1 << 17,

    /// <summary>
    /// FirstCHS should be 00 02 00 - <see cref="CHSAddress.Second"/>
    /// </summary>
    ProtectiveFirstChsInvalid = 1 << 18,

    /// <summary>
    /// LastCHS should be equal to the last block of drive or FF FF FF - <see cref="CHSAddress.ProtectiveMbr"/>
    /// </summary>
    ProtectiveEndChsInvalid = 1 << 19,

    /// <summary>
    /// Size of protective partition is equal to <see cref="uint.MaxValue"/> - 1, but drive is smaller than that.
    /// </summary>
    ProtectiveSizeSaturated = 1 << 20,

    /// <summary>
    /// Hybrid MBR only: the area covered by the 0xEE entry runs past the end of the drive,
    /// or the entry has no length at all. In a hybrid its size is not predictable
    /// (gdisk sizes it up to the first hybridised partition), so staying in bounds
    /// is all we can demand of it.
    /// </summary>
    ProtectiveOutOfBounds = 1 << 21,
}
