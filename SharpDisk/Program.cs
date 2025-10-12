// See https://aka.ms/new-console-template for more information

using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using SharpDisk.Core.Mbr;
using SharpDisk.Linux;
using Humanizer.Bytes;
using SharpDisk.Core;
using SharpDisk.Ui;
using Terminal.Gui;
using Terminal.Gui.App;
using Terminal.Gui.ViewBase;
using Terminal.Gui.Views;



Application.Init();
// Application.QuitKey = Key.Q;

if (!OperatingSystem.IsLinux())
{
    MessageBox.Query(50, 7, "No support", "Only Linex is supported for now", "Switch to NoxOS");
    Application.Shutdown();
    Environment.Exit(1);
    throw new UnreachableException("Jebać kompilator");
}

try
{
    IBlkDeviceProvider blkProvider = new LinuxBlkDeviceProvider();
    var deviceList = Task.Run(async () => await blkProvider.ListDevices()).Result;
    
    var dataTable = new DataTable();

    dataTable.Columns.Add("Device");
    dataTable.Columns.Add("MAJ:MIN");
    dataTable.Columns.Add("Size");
    dataTable.Columns.Add("Model");


    foreach (var device in deviceList)
    {
        dataTable.Rows.Add([
            device.DevName,
            $"{device.Major}:{device.Minor}",
            new ByteSize(device.Size).ToString(),
            device.Model
        ]);
    }

    var menu = new MainMenu();

    var mainTileView = new TileView(2)
    {
        Orientation = Orientation.Horizontal,
        Height = Dim.Fill(),
        Width = Dim.Fill(),
        X = 0,
        Y = Pos.Bottom(menu) + 1,
    };
    
    var drivesFrameView = new FrameView
    {
        Title = "Drives",
        X = 0,
        Y = 0,
        Height = Dim.Fill(),
        Width = Dim.Fill(),
    };

    var partitionsFrameView = new FrameView()
    {
        Title = "Partitions",
        X = 0,
        Y = 0,
        Height = Dim.Fill(),
        Width = Dim.Fill(),
    };
    
    var drivesTableView = new TableView()
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = 60,
        FullRowSelect = true,
        Table = new DataTableSource(dataTable)
    };
    drivesTableView.Style.ShowHorizontalBottomline = true;

    var partitionsTileView = new TileView(2)
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        Orientation = Orientation.Vertical
    };
    
    var partitionsDataTable = new DataTable();
    partitionsDataTable.Columns.Add("Partition");
    partitionsDataTable.Columns.Add("MAJ:MIN");
    partitionsDataTable.Columns.Add("Size");

    var mountpoints = new ObservableCollection<string>();
    
    if (deviceList.Count > 0)
    {
        LoadPartitionsTable(partitionsDataTable, mountpoints, deviceList[0]);
    }
    
    var partitionsTableView = new TableView
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
        FullRowSelect = true,
        Table = new DataTableSource(partitionsDataTable),
        Style = { ShowHorizontalBottomline = true }
    };

    partitionsTableView.SelectedCellChanged += (s, e) =>
    {
        var dev = deviceList[drivesTableView.SelectedRow];

        if (dev.Children is null || dev.Children.Count == 0)
        {
            return;
        }
        
        // xpp
        var part = dev.Children[e.NewRow];
        LoadMountpoints(mountpoints, part);
    };
    
    partitionsFrameView.Add(partitionsTableView);
    drivesFrameView.Add(drivesTableView);

    var mountPointsFrameView = new FrameView
    {
        Title = "Mountpoints",
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };

    var mountPointsListView = new ListView()
    {
        X = 0,
        Y = 0,
        Width = Dim.Fill(),
        Height = Dim.Fill(),
    };

    mountPointsListView.SetSource(mountpoints);
    mountPointsFrameView.Add(mountPointsListView);

    partitionsTileView.Tiles.ToArray()[0].ContentView.Add(partitionsFrameView);
    partitionsTileView.Tiles.ToArray()[1].ContentView.Add(mountPointsFrameView);
    partitionsTileView.Tiles.ToArray()[1].ContentView.CanFocus = false;

    var drivesTile = mainTileView.Tiles.ToArray()[0];
    var partitionsTile = mainTileView.Tiles.ToArray()[1];
    
    drivesTile.ContentView.Add(drivesFrameView);
    partitionsTile.ContentView.Add(partitionsTileView);

    drivesTableView.SelectedCellChanged += (s, e) =>
    {
        LoadPartitionsTable(partitionsDataTable, mountpoints, deviceList[e.NewRow]);
    };
    
    var app = new Toplevel();
    app.Add(menu, mainTileView);
    
    Application.Run(app);

    app.Dispose();
}
catch (Exception ex)
{
    MessageBox.ErrorQuery(ex.GetType().Name, ex.Message, "Ok");
}

Application.Shutdown();
return;

void LoadPartitionsTable(DataTable dataTable, ObservableCollection<string> mountpoints, BlkDevice drive)
{
    dataTable.Clear();
    mountpoints.Clear();
    if (drive.Children is null || drive.Children.Count == 0)
    {
        return;
    }
    
    foreach (var part in drive.Children)
    {
        dataTable.Rows.Add(part.DevName, $"{part.Major}:{part.Minor}", new ByteSize(part.Size).ToString());
    }
    
    LoadMountpoints(mountpoints, drive.Children[0]);
}

void LoadMountpoints(ObservableCollection<string> mountPointsList, BlkDevice drive)
{
    mountPointsList.Clear();
    if (drive.Mountpoints is null || drive.Mountpoints.Count == 0)
    {
        return;
    }

    foreach (var mount in drive.Mountpoints)
    {
        mountPointsList.Add(mount);
    }
}

// if (!OperatingSystem.IsLinux())
// {
//     throw new NotSupportedException("Linux is not supported");
// }
//
// var blkPath = args[0];
//
// var blkStream = new BlockDeviceStream(blkPath, false);
// var mbrRaw = blkStream.ReadSection(0, blkStream.LbaSize);
//
// var mbr = MbrPartitionTable.FromBinary(mbrRaw);
//
//     
// Console.WriteLine($"{"Drive:", -16} {blkPath}");
// Console.WriteLine($"{"LBA Size:", -16} {new ByteSize(blkStream.LbaSize).ToString()}");
// Console.WriteLine($"{"LBA Count", -16} {blkStream.LbaCount}");
// Console.WriteLine($"{"Size in bytes:", -16} {new ByteSize(blkStream.Length).ToString()}");
// Console.WriteLine();
//
// Console.WriteLine("Partitions:");
//
// // Nagłówek tabeli
// Console.WriteLine(new string('-', 80)); // Linia oddzielająca
// Console.WriteLine($"{"FIRST LBA",-16} {"LBA COUNT",-16} {"SIZE",-10} {"BOOTABLE",-16} {"PARTITION TYPE",-22}");
// Console.WriteLine(new string('-', 80)); // Linia oddzielająca
//
// // Wypisanie danych partycji
// foreach (var mbrPartition in mbr.Partitions)
// {
//     Console.WriteLine(
//         $"{mbrPartition.FirstLba,-16} " +
//         $"{mbrPartition.LbaCount,-16} " +
//         $"{new ByteSize(mbrPartition.LbaCount * blkStream.LbaSize).ToString(), -10} " +
//         $"{mbrPartition.Bootable,-16} " +
//         $"{mbrPartition.PartitionType,-22}"
//     );
// }
// Console.WriteLine(new string('-', 80)); // Linia zamykająca

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