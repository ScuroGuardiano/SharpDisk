namespace SharpDisk.Core;

public class BlkDevice
{
    public int Major { get; set; }
    public int Minor { get; set; }
    public bool ReadOnly { get; set; }
    public required string DevName { get; set; }
    public BlkDeviceType DeviceType { get; set; }
    public ulong Size { get; set; }
    public uint BlockSize { get; set; }
    public string? Model { get; set; }
    public string? PartitionName { get; set; }
    public string? PartitionUuid { get; set; }
    public IReadOnlyList<string>? Mountpoints { get; set; }

    // Partitions only, I guess
    public IReadOnlyList<BlkDevice>? Children { get; set; }
    
    // Disk only, I guess
    public BlkDevice? Parent { get; set; }
}
