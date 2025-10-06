namespace SharpDisk.Core;

public interface IBlkDeviceProvider
{
    public Task<IReadOnlyList<BlkDevice>> ListDevices();
}
