using Microsoft.Extensions.Logging;
using SharpDisk.Core;
using SharpDisk.Lib;

namespace SharpDisk.Gui.Services;

/// <inheritdoc cref="IBlockDeviceService" />
public sealed class BlockDeviceService(ILogger<BlockDeviceService> logger) : IBlockDeviceService
{
    // Created on first use rather than in the constructor: on an unsupported OS building the
    // provider throws, and a service that cannot be constructed takes the whole window with it.
    private IBlkDeviceProvider? _provider;

    public bool IsSupported => BlkDeviceProviders.IsSupported;

    public async Task<IReadOnlyList<BlkDevice>> ListDisksAsync()
    {
        var devices = await Provider.ListDevices();

        var disks = devices
            .Where(device => device.DeviceType == BlkDeviceType.Disk)
            // /sys/class/block is full of unbound loop and ram devices that report zero bytes.
            // Nothing can be partitioned there, so they are noise in a disk picker.
            .Where(device => device.Size > 0)
            .OrderBy(device => device.DevName, StringComparer.Ordinal)
            .ToList();

        logger.LogInformation("Listed {DiskCount} disks out of {DeviceCount} block devices", disks.Count, devices.Count);

        return disks;
    }

    public DriveStream OpenDevice(BlkDevice device, bool writable = false)
        => Provider.OpenDevice(device, writable);

    private IBlkDeviceProvider Provider => _provider ??= BlkDeviceProviders.ForCurrentPlatform();
}
