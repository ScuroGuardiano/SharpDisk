namespace SharpDisk.Core.Mbr;

[Flags]
public enum MbrErrors : uint
{
    None = 0,
    
    /// <summary>
    /// Signature at the end is not 0x55 0xAA
    /// </summary>
    InvalidSignature = 1 << 1,
    
    /// <summary>
    /// Partition 1 has errors or warnings
    /// </summary>
    InvalidPartition1 = 1 << 2,
    
    /// <summary>
    /// Partition 2 has errors or warnings
    /// </summary>
    InvalidPartition2 = 1 << 3,
    
    /// <summary>
    /// Partition 3 has errors or warnings
    /// </summary>
    InvalidPartition3 = 1 << 4,
    
    /// <summary>
    /// Partition 4 has errors or warnings
    /// </summary>
    InvalidPartition4 = 1 << 5,
    
    /// <summary>
    /// There are many active [bootable] partitions, which is invalid for MBR
    /// </summary>
    MultipleActivePartitions = 1 << 7,
    
    /// <summary>
    /// There is no bootable partition, it's not an error tho
    /// </summary>
    NoActivePartitions = 1 << 8,
    
    /// <summary>
    /// It's a warning, not an error. We may use it to diagnose and fix errors occuring on Windows OS-es
    /// </summary>
    ZeroDiskSignature = 1 << 9,
    
    /// <summary>
    /// It's also a warning and it matters only for Windows I think. Generic MBR doesn't include that.
    /// </summary>
    InvalidReservedFields = 1 << 10,
}
