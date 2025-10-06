using System.Runtime.Versioning;

namespace SharpDisk.Linux;

[SupportedOSPlatform("linux")]
internal class UEventData
{
    public int Major { get; set; } = -1;
    public int Minor { get; set; } = -1;
    public string? DevName { get; set; }
    public string? DevType { get; set; }
    public string? PartName { get; set; }
    public string? PartUuid { get; set; }
}
