using SharpDisk.Core.Mbr;

namespace SharpDisk.Gui.Services;

/// <summary>
/// Picks the colour a partition type is drawn in.
/// </summary>
/// <remarks>
/// Families share a hue the way GParted does it, so a disk reads at a glance: FAT is green,
/// NTFS teal, Linux blue, swap purple, EFI amber, extended brown. Anything without an opinion
/// falls back to a fixed palette indexed by the type byte, which at least keeps one type one
/// colour between runs.
/// <para>
/// The shades are Material 300-400 rather than 600, because the window runs a dark theme: a
/// 600-level blue on a #2c2c2c lane is barely distinguishable from the lane itself.
/// </para>
/// </remarks>
public static class PartitionPalette
{
    private static readonly Dictionary<byte, string> Known = new()
    {
        [MbrPartitionTypes.ProtectiveMbr] = "#90a4ae",
        [MbrPartitionTypes.EfiSystemPartition] = "#ffca28",
        [0x01] = "#d4e157",
        [0x04] = "#9ccc65",
        [0x06] = "#9ccc65",
        [0x0E] = "#9ccc65",
        [0x0B] = "#66bb6a",
        [0x0C] = "#66bb6a",
        [0x07] = "#26a69a",
        [0x05] = "#a1887f",
        [0x0F] = "#a1887f",
        [0x85] = "#a1887f",
        [0x82] = "#ab47bc",
        [0x83] = "#42a5f5",
        [0x8E] = "#5c6bc0",
        [0xFD] = "#7e57c2",
        [0x27] = "#ffa726",
        [0x42] = "#26c6da",
        [0xA5] = "#ef5350",
        [0xA6] = "#ec407a",
        [0xA9] = "#f06292",
        [0xAF] = "#bdbdbd",
        [0xBF] = "#ff7043",
        [0xE8] = "#9575cd",
    };

    private static readonly string[] Fallback =
    [
        "#90a4ae", "#bcaaa4", "#b39ddb", "#80cbc4", "#c5e1a5",
        "#ffcc80", "#81d4fa", "#ce93d8", "#d7ccc8", "#b0bec5",
    ];

    public static string ColorFor(byte partitionType)
        => Known.TryGetValue(partitionType, out var color)
            ? color
            : Fallback[partitionType % Fallback.Length];
}
