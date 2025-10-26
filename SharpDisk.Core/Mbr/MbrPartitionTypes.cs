namespace SharpDisk.Core.Mbr;

/// <summary>
/// Types copied from wikipedia xD
/// </summary>
public static class MbrPartitionTypes
{
    /// <summary>
    /// Empty partition entry 
    /// </summary>
    public const byte Empty = 0x00;
    
    /// <summary>
    /// FAT12 as primary partition in first physical 32 MB of disk or as logical drive anywhere on disk (else use 06h instead)
    /// </summary>
    public const byte PrimaryFat12 = 0x01;

    /// <summary>
    /// XENIX root
    /// </summary>
    public const byte XenixRoot = 0x02;

    /// <summary>
    /// XENIX usr
    /// </summary>
    public const byte XenixUsr = 0x03;
    
    public const byte ProtectiveMbr = 0xEE;
}
