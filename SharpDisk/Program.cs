// See https://aka.ms/new-console-template for more information

using SharpDisk.Core;
using SharpDisk.Core.Mbr;
using SharpDisk.Linux;

var blkPath = args[0];

var blkStream = new BlockDeviceStream(blkPath, false);
var mbrRaw = blkStream.ReadSection(0, blkStream.LbaSize);

var mbr = MbrPartitionTable.FromBinary(mbrRaw);

Console.WriteLine($"Partitions of {blkPath}:");

// Nagłówek tabeli
Console.WriteLine(new string('-', 80)); // Linia oddzielająca
Console.WriteLine($"{"FIRST LBA SECTOR",-20} {"LBA SECTORS COUNT",-20} {"BOOTABLE",-15} {"PARTITION TYPE",-30}");
Console.WriteLine(new string('-', 80)); // Linia oddzielająca

// Wypisanie danych partycji
foreach (var mbrPartition in mbr.Partitions)
{
    Console.WriteLine(
        $"{mbrPartition.FirstLba,-20} " +
        $"{mbrPartition.LbaCount,-20} " +
        $"{mbrPartition.Bootable,-15} " +
        $"{mbrPartition.PartitionType,-30}"
    );
}
Console.WriteLine(new string('-', 80)); // Linia zamykająca

// IBlkDeviceProvider blkDeviceProvider;
//
// if (OperatingSystem.IsLinux())
// {
//     blkDeviceProvider = new LinuxBlkDeviceProvider();
// }
// else
// {
//     throw new NotSupportedException("Unsupported operating system.");
// }
//
// var blkDevs = await blkDeviceProvider.ListDevices();

// foreach (var dev in blkDevs)
// {
//     Console.Write($"{dev.DevName}, Type: {dev.DeviceType}, {dev.Major}:{dev.Minor}");
//     
//     if (dev.Mountpoints is { Count: > 0 })
//     {
//         Console.Write($" Mounts: {string.Join(", ", dev.Mountpoints)}");
//     }
//     
//     Console.WriteLine();
//
//     if (dev.Children is not null)
//     {
//         foreach (var child in dev.Children)
//         {
//             var boxDrawing = child != dev.Children[^1] ? "\u251C\u2500" : "\u2514\u2500";
//             
//             Console.Write($"{boxDrawing} {child.DevName}, Type: {child.DeviceType}, {child.Major}:{dev.Minor}");
//             
//             if (child.Mountpoints is { Count: > 0 })
//             {
//                 Console.Write($" Mounts: {string.Join(", ", child.Mountpoints)}");
//             }
//             
//             Console.WriteLine();
//         }
//     }
// }
