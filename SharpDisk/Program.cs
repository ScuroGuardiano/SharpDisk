// See https://aka.ms/new-console-template for more information

using SharpDisk.Core;
using SharpDisk.Linux;

IBlkDeviceProvider blkDeviceProvider;

if (OperatingSystem.IsLinux())
{
    blkDeviceProvider = new LinuxBlkDeviceProvider();
}
else
{
    throw new NotSupportedException("Unsupported operating system.");
}

var blkDevs = await blkDeviceProvider.ListDevices();

foreach (var dev in blkDevs)
{
    Console.Write($"{dev.DevName}, Type: {dev.DeviceType}, {dev.Major}:{dev.Minor}");
    
    if (dev.Mountpoints is { Count: > 0 })
    {
        Console.Write($" Mounts: {string.Join(", ", dev.Mountpoints)}");
    }
    
    Console.WriteLine();

    if (dev.Children is not null)
    {
        foreach (var child in dev.Children)
        {
            var boxDrawing = child != dev.Children[^1] ? "\u251C\u2500" : "\u2514\u2500";
            
            Console.Write($"{boxDrawing} {child.DevName}, Type: {child.DeviceType}, {child.Major}:{dev.Minor}");
            
            if (child.Mountpoints is { Count: > 0 })
            {
                Console.Write($" Mounts: {string.Join(", ", child.Mountpoints)}");
            }
            
            Console.WriteLine();
        }
    }
}
