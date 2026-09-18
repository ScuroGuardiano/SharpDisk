using SharpDisk.Core.Attributes;

namespace SharpDisk.Core.Mbr;

[Flags]
public enum MbrErrors : uint
{
    None = 0,
    
    /// <summary>
    /// Signature at the end is not 0x55 0xAA
    /// </summary>
    [ErrorDescription("Invalid signature", "The last two bytes of the sector are not the 0x55 0xAA boot signature, so this sector is not a valid MBR. Firmware and most tools will refuse to treat the disk as partitioned.", Severity.Error)]
    InvalidSignature = 1 << 1,
    
    /// <summary>
    /// Partition 1 has errors or warnings
    /// </summary>
    [ErrorDescription("Partition 1 has problems", "Partition entry 1 reported one or more errors or warnings of its own. Check that partition for the specific issues.", Severity.Warning)]
    InvalidPartition1 = 1 << 2,
    
    /// <summary>
    /// Partition 2 has errors or warnings
    /// </summary>
    [ErrorDescription("Partition 2 has problems", "Partition entry 2 reported one or more errors or warnings of its own. Check that partition for the specific issues.", Severity.Warning)]
    InvalidPartition2 = 1 << 3,
    
    /// <summary>
    /// Partition 3 has errors or warnings
    /// </summary>
    [ErrorDescription("Partition 3 has problems", "Partition entry 3 reported one or more errors or warnings of its own. Check that partition for the specific issues.", Severity.Warning)]
    InvalidPartition3 = 1 << 4,
    
    /// <summary>
    /// Partition 4 has errors or warnings
    /// </summary>
    [ErrorDescription("Partition 4 has problems", "Partition entry 4 reported one or more errors or warnings of its own. Check that partition for the specific issues.", Severity.Warning)]
    InvalidPartition4 = 1 << 5,
    
    /// <summary>
    /// There are many active [bootable] partitions, which is invalid for MBR
    /// </summary>
    [ErrorDescription("Multiple active partitions", "More than one partition is marked active (bootable). MBR allows at most one active partition, so booting from this disk may fail or pick an unintended partition.", Severity.Error)]
    MultipleActivePartitions = 1 << 7,
    
    /// <summary>
    /// There is no bootable partition, it's not an error tho
    /// </summary>
    [ErrorDescription("No active partition", "No partition is marked active (bootable). This is not an error, but a legacy BIOS boot loader may have nothing to hand control to.", Severity.Info)]
    NoActivePartitions = 1 << 8,
    
    /// <summary>
    /// It's a warning, not an error. We may use it to diagnose and fix errors occuring on Windows OS-es
    /// </summary>
    [ErrorDescription("Zero disk signature", "The 32-bit disk signature is zero. The table still works, but Windows uses this value to identify the disk, so a zero signature can cause drive-letter and boot problems there.", Severity.Warning)]
    ZeroDiskSignature = 1 << 9,
    
    /// <summary>
    /// It's also a warning and it matters only for Windows I think. Generic MBR doesn't include that.
    /// </summary>
    [ErrorDescription("Invalid reserved fields", "The two reserved bytes that follow the disk signature are not zero. A generic MBR does not use them, so this matters mainly for Windows.", Severity.Warning)]
    InvalidReservedFields = 1 << 10,

    /// <summary>
    /// Multiple protective (0xEE) partitions
    /// </summary>
    [ErrorDescription("Multiple protective entries", "The table contains more than one protective (type 0xEE) partition. A protective MBR must have exactly one, so tools may misread how the disk is laid out.", Severity.Error)]
    MultipleProtectiveEntries = 1 << 11,

    /// <summary>
    /// Clean protective MBR: entries other than the 0xEE one are not zeroed
    /// </summary>
    [ErrorDescription("Protective slots not zeroed", "In a clean protective MBR every entry other than the 0xEE one should be zeroed, but some are not. The disk still works, though tools may read it as a hybrid MBR rather than a pure protective one.", Severity.Warning)]
    ProtectiveSlotsNotZeroed = 1 << 12,
}
