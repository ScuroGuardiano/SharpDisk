namespace SharpDisk.Core;

public interface IBlkDeviceProvider
{
    public Task<IReadOnlyList<BlkDevice>> ListDevices();

    /// <summary>
    /// Opens a device returned by <see cref="ListDevices"/> for raw sector access.
    /// </summary>
    /// <param name="device">Device to open.</param>
    /// <param name="writable">
    /// Open for writing as well as reading. Read-only is the safe default; a writable handle to a
    /// disk in use is how partition tables get destroyed.
    /// </param>
    /// <remarks>
    /// The platform backend owns the details - on Linux a device needs ioctls for its size and
    /// sector size, which a plain <see cref="FileDriveStream"/> does not do. Raw access to a disk
    /// normally needs root.
    /// </remarks>
    /// <exception cref="UnauthorizedAccessException">The process may not open the device.</exception>
    /// <exception cref="InvalidOperationException">The path is not a block device of the expected kind.</exception>
    public DriveStream OpenDevice(BlkDevice device, bool writable = false);
}
