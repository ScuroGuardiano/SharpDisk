namespace SharpDisk.Gui.Services;

/// <summary>
/// Byte counts as people read them.
/// </summary>
public static class ByteSize
{
    private static readonly string[] Units = ["B", "KiB", "MiB", "GiB", "TiB", "PiB", "EiB"];

    /// <summary>
    /// Formats a byte count with binary units, e.g. <c>1,86 TiB</c>.
    /// </summary>
    /// <remarks>
    /// Binary, not decimal: partition tables count sectors, and a disk laid out in powers of two
    /// reads as round numbers only in KiB/MiB/GiB. The separator follows the current culture, so
    /// never feed the result into CSS or a file format.
    /// </remarks>
    public static string Format(ulong bytes)
    {
        double value = bytes;
        var unit = 0;

        while (value >= 1024 && unit < Units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return unit == 0
            ? $"{bytes} {Units[unit]}"
            : $"{value.ToString(value >= 100 ? "0" : "0.##")} {Units[unit]}";
    }

    /// <summary>
    /// Formats a sector count as a byte count.
    /// </summary>
    public static string FormatSectors(ulong sectors, uint sectorSize) => Format(sectors * sectorSize);
}
