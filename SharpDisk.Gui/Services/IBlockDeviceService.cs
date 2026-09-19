using SharpDisk.Core;

namespace SharpDisk.Gui.Services;

/// <summary>
/// The GUI's window onto the platform block-device backend.
/// </summary>
/// <remarks>
/// Exists so pages never touch <c>SharpDisk.Lib</c> directly: the provider is created once, the
/// noise of the raw listing is filtered out here, and the whole thing can be swapped for a fake
/// when there is no real disk to point at.
/// </remarks>
public interface IBlockDeviceService
{
    /// <summary>Whether this OS has a backend at all.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// Whole disks, smallest noise filtered out, ordered by name.
    /// </summary>
    Task<IReadOnlyList<BlkDevice>> ListDisksAsync();

    /// <summary>
    /// Opens a device for raw sector access. Normally needs root.
    /// </summary>
    DriveStream OpenDevice(BlkDevice device, bool writable = false);
}
