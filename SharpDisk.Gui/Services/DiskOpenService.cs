using Microsoft.Extensions.Logging;
using SharpDisk.Core;
using SharpDisk.Core.Mbr;
using SharpDisk.Gui.Models;

namespace SharpDisk.Gui.Services;

/// <inheritdoc cref="IDiskOpenService" />
public sealed class DiskOpenService(IBlockDeviceService devices, ILogger<DiskOpenService> logger) : IDiskOpenService
{
    public async Task<OpenedDisk> OpenDeviceAsync(BlkDevice device, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(device);

        logger.LogInformation("Opening device /dev/{DevName}", device.DevName);

        await using var stream = devices.OpenDevice(device, writable: false);
        return await ReadAsync(stream, DiskSource.ForDevice(device), cancellationToken);
    }

    public async Task<OpenedDisk> OpenPathAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        path = path.Trim();

        // A device needs the backend: its size and sector size come from ioctls that a plain file
        // stream has no way to ask for. Matching against the listed devices rather than sniffing
        // the path keeps that decision in one place.
        var device = await FindDeviceAsync(path);
        if (device is not null)
        {
            return await OpenDeviceAsync(device, cancellationToken);
        }

        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Nie ma takiego pliku ani urządzenia: {path}", path);
        }

        logger.LogInformation("Opening file {Path}", path);

        await using var stream = new FileDriveStream(path, writable: false);
        return await ReadAsync(stream, DiskSource.ForFile(path), cancellationToken);
    }

    private async Task<BlkDevice?> FindDeviceAsync(string path)
    {
        if (!devices.IsSupported || !path.StartsWith("/dev/", StringComparison.Ordinal))
        {
            return null;
        }

        try
        {
            var all = await devices.ListDisksAsync();
            return all.FirstOrDefault(d => string.Equals($"/dev/{d.DevName}", path, StringComparison.Ordinal));
        }
        catch (Exception error)
        {
            // Not being able to enumerate is not a reason to refuse the path outright - fall
            // through and let the file route report whatever the real problem is.
            logger.LogWarning(error, "Could not enumerate devices while resolving {Path}", path);
            return null;
        }
    }

    private static async Task<OpenedDisk> ReadAsync(DriveStream stream, DiskSource source, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (stream.LbaSize == 0)
        {
            throw new InvalidOperationException($"{source.Path} zgłasza rozmiar sektora 0, nie da się z tego odczytać tablicy partycji.");
        }

        var mbr = await stream.ReadSectionAsync(0, 512);

        var driveInfo = new SharpDisk.Core.DriveInfo(stream.LbaCount, (uint)stream.LbaSize);
        var manager = MbrManager.CreateFromBytes(mbr, driveInfo);

        return new OpenedDisk
        {
            Source = source,
            DriveInfo = driveInfo,
            RawMbr = mbr,
            Manager = manager,
            Analysis = manager.Analyze(),
        };
    }
}
