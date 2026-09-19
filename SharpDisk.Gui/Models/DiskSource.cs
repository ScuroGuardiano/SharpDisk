using SharpDisk.Core;

namespace SharpDisk.Gui.Models;

/// <summary>
/// Where an opened disk came from.
/// </summary>
public enum DiskSourceKind
{
    /// <summary>A block device listed by the platform backend, e.g. <c>/dev/sda</c>.</summary>
    Device,

    /// <summary>A regular file - a disk image, an ISO, anything with sectors in it.</summary>
    File,
}

/// <summary>
/// Identifies what is open, for display and for reopening later.
/// </summary>
/// <param name="Kind">Device or file.</param>
/// <param name="DisplayName">Short label for the UI.</param>
/// <param name="Path">Full path the source was opened from.</param>
/// <param name="Details">Model name for a device, containing directory for a file. May be empty.</param>
public sealed record DiskSource(DiskSourceKind Kind, string DisplayName, string Path, string? Details)
{
    public static DiskSource ForDevice(BlkDevice device)
        => new(DiskSourceKind.Device, $"/dev/{device.DevName}", $"/dev/{device.DevName}", device.Model?.Trim());

    public static DiskSource ForFile(string path)
        => new(DiskSourceKind.File, System.IO.Path.GetFileName(path), path, System.IO.Path.GetDirectoryName(path));
}
