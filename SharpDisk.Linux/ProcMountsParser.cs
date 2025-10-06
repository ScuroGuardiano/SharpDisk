namespace SharpDisk.Linux;

public class ProcMountsParser
{
    public IReadOnlyList<ProcMountEntry> Parse(string data)
    {
        // File is simple as hell, stuff is separated by spaces.
        // It's like this:
        // dev mountpoint type options dump pass
        
        return data
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(parts => new ProcMountEntry { DevName = parts[0], MountPath = parts[1] })
            .ToList();
    }
}
