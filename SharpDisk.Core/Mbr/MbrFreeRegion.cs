namespace SharpDisk.Core.Mbr;

/// <summary>
/// A run of unallocated sectors on the drive, as reported by <see cref="MbrManager.GetFreeRegions"/>.
/// </summary>
/// <param name="FirstLba">First free sector.</param>
/// <param name="LbaCount">How many sectors the run covers.</param>
public readonly record struct MbrFreeRegion(uint FirstLba, uint LbaCount)
{
    /// <summary>
    /// Last sector of the run, inclusive. Only meaningful when <see cref="LbaCount"/> is non zero.
    /// </summary>
    public uint LastLba => FirstLba + LbaCount - 1;
}
