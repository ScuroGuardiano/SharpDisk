namespace SharpDisk.Linux;

public class ProcMountEntry
{
    // All params I care about
    public required string DevName { get; set; }
    public required string MountPath { get; set; }
}
