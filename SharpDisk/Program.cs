// See https://aka.ms/new-console-template for more information

using SharpDisk.Core.Mbr;
using SharpDisk.Linux;
using Humanizer.Bytes;

if (!OperatingSystem.IsLinux())
{
    throw new NotSupportedException("Linux is not supported");
}

var blkPath = args[0];

var blkStream = new BlockDeviceStream(blkPath, false);
var mbrRaw = blkStream.ReadSection(0, blkStream.LbaSize);

var mbr = MbrPartitionTable.FromBinary(mbrRaw);

    
Console.WriteLine($"{"Drive:", -16} {blkPath}");
Console.WriteLine($"{"LBA Size:", -16} {new ByteSize(blkStream.LbaSize).ToString()}");
Console.WriteLine($"{"LBA Count", -16} {blkStream.LbaCount}");
Console.WriteLine($"{"Size in bytes:", -16} {new ByteSize(blkStream.Length).ToString()}");
Console.WriteLine();

Console.WriteLine("Partitions:");

// Nagłówek tabeli
Console.WriteLine(new string('-', 80)); // Linia oddzielająca
Console.WriteLine($"{"FIRST LBA",-16} {"LBA COUNT",-16} {"SIZE",-10} {"BOOTABLE",-16} {"PARTITION TYPE",-22}");
Console.WriteLine(new string('-', 80)); // Linia oddzielająca

// Wypisanie danych partycji
foreach (var mbrPartition in mbr.Partitions)
{
    Console.WriteLine(
        $"{mbrPartition.FirstLba,-16} " +
        $"{mbrPartition.LbaCount,-16} " +
        $"{new ByteSize(mbrPartition.LbaCount * blkStream.LbaSize).ToString(), -10} " +
        $"{mbrPartition.Bootable,-16} " +
        $"{mbrPartition.PartitionType,-22}"
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
