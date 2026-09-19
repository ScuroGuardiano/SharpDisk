using SharpDisk.Gui.Models;

namespace SharpDisk.Gui.Services;

/// <summary>
/// Turns a partition table into the blocks a map component draws.
/// </summary>
public interface IDiskMapBuilder
{
    DiskMap Build(OpenedDisk disk);
}
