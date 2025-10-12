using System.Runtime.Versioning;
using SharpDisk.Core;

namespace SharpDisk.Linux;

[SupportedOSPlatform("linux")]
public class LinuxBlkDeviceProvider : IBlkDeviceProvider
{
    private readonly UEventParser _ueventParser = new UEventParser();
    private readonly ProcMountsParser _procMountsParser = new ProcMountsParser();
    
    public async Task<IReadOnlyList<BlkDevice>> ListDevices()
    {
        List<BlkDevice> devices = [];
        
        var dir = Directory.EnumerateDirectories("/sys/class/block");
        
        foreach (var dev in dir)
        {
            var uevent = ReadIfExists(Path.Combine(dev, "uevent"));
            var sizeIn512BlocksRaw = ReadIfExists(Path.Combine(dev, "size"));
            var logicalBlockSizeRaw = ReadIfExists(Path.Combine(dev, "queue", "logical_block_size"));
            var model = ReadIfExists(Path.Combine(dev, "device", "model"));
            var roRaw = ReadIfExists(Path.Combine(dev, "ro"));

            await Task.WhenAll(uevent, sizeIn512BlocksRaw, logicalBlockSizeRaw, model, roRaw).ConfigureAwait(false);

            var ueventData = _ueventParser.Parse(uevent.Result);
            var sizeIn512Blocks = ulong.TryParse(sizeIn512BlocksRaw.Result, out var size) ? size : 0;
            var logicalBlockSize = uint.TryParse(logicalBlockSizeRaw.Result, out var lbs) ? lbs : 0;
            var ro = roRaw.Result == "1";

            var blkDevice = new BlkDevice
            {
                DevName = ueventData.DevName ?? "",
                Major = ueventData.Major,
                Minor = ueventData.Minor,
                BlockSize = logicalBlockSize,
                Size = sizeIn512Blocks * 512,
                Model = model.Result,
                PartitionName = ueventData.PartName,
                PartitionUuid = ueventData.PartUuid,
                ReadOnly = ro,
                DeviceType = ueventData.DevType switch
                {
                    "disk" => BlkDeviceType.Disk,
                    "partition" => BlkDeviceType.Partition,
                    _ => BlkDeviceType.Unknown
                },
            };
            
            devices.Add(blkDevice);
        }
        
        var procMountsRaw = await ReadIfExists("/proc/mounts");
        if (procMountsRaw is not null)
        {
            var procMounts = _procMountsParser.Parse(procMountsRaw);
            AssignMountpoints(devices, procMounts);
        }
        
        return BuildDeviceTree(devices);
    }

    private async Task<string?> ReadIfExists(string path)
    {
        try
        {
            return await File.ReadAllTextAsync(path);
        }
        catch (DirectoryNotFoundException)
        {
            return null;
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (Exception exception)
        {
            await Console.Error.WriteLineAsync($"Error reading file {path}: {exception.Message}");
            return null;
        }
    }

    /// <summary>
    /// Builds device tree from flat list. Basically nests partitions.
    /// </summary>
    /// <param name="flatDeviceList"></param>
    /// <returns></returns>
    private IReadOnlyList<BlkDevice> BuildDeviceTree(IReadOnlyList<BlkDevice> flatDeviceList)
    {
        // This method is O(n^2) but no one has thousands of block devices on their computer, so it's fine.
        foreach (var device in flatDeviceList.Where(d => d.DeviceType != BlkDeviceType.Partition))
        {
            var children = flatDeviceList
                .Where(x => x.DevName.StartsWith(device.DevName) && x.DevName != device.DevName)
                .ToList();

            foreach (var child in children)
            {
                child.Parent = device;
            }
            
            if (children.Count > 0)
            {
                device.Children = children;
            }
        }
        
        return flatDeviceList.Where(d => d.Parent is null).ToList();
    }

    private void AssignMountpoints(IReadOnlyList<BlkDevice> flatDeviceList, IReadOnlyList<ProcMountEntry> mounts)
    {
        foreach (var device in flatDeviceList)
        {
            device.Mountpoints = mounts
                .Where(m => m.DevName == $"/dev/{device.DevName}")
                .Select(m => m.MountPath)
                .ToList();
        }
    }
}
