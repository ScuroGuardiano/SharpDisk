using SharpDisk.Core;
using SharpDisk.Gui.Models;

namespace SharpDisk.Gui.Services;

/// <summary>
/// Turns a device or a path into an <see cref="OpenedDisk"/>: read sector 0, parse it, analyze it.
/// </summary>
public interface IDiskOpenService
{
    Task<OpenedDisk> OpenDeviceAsync(BlkDevice device, CancellationToken cancellationToken = default);

    /// <summary>
    /// Opens a path, working out on its own whether it names a listed block device or a plain file.
    /// </summary>
    /// <remarks>
    /// This is what the "open file or block device" screen calls, so a user can paste
    /// <c>/dev/sda</c> or an ISO path into the same box and get the right handling either way.
    /// </remarks>
    Task<OpenedDisk> OpenPathAsync(string path, CancellationToken cancellationToken = default);
}
