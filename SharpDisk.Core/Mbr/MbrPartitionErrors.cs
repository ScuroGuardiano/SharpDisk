namespace SharpDisk.Core.Mbr;

[Flags]
public enum MbrPartitionErrors : ushort
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
    /// Unknown partition type, not necessary an error
    /// </summary>
    InvalidPartitionType = 1 << 7,
    
    /// <summary>
    /// In case of CHS sector equals to 0 or not (1023, 255, 63) in case of ProtectiveMBR
    /// </summary>
    InvalidChsAddress = 1 << 8,
    
    /// <summary>
    /// Reserved for the future, for now I don't care about CHS too much
    /// </summary>
    ChsLbaMismatch = 1 << 9,
    
    /// <summary>
    /// Start sector is not aligned to 4K/1M (usually 2048 sector). Grok says it's a performance loss xD
    /// </summary>
    UnalignedStart = 1 << 10,
}
