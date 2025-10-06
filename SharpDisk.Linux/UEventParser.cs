using System.Runtime.Versioning;

namespace SharpDisk.Linux;

[SupportedOSPlatform("linux")]
internal class UEventParser
{
    // uevent is just a datastructure of KEY=value
    private static readonly Dictionary<string, Action<string, UEventData>> Parsers = new()
    {
        ["MAJOR"] = (data, output) =>
            output.Major = int.TryParse(data, out var major) ? major : -1,
        
        ["MINOR"] = (data, output) =>
            output.Minor = int.TryParse(data, out var minor) ? minor : -1,
        
        ["DEVNAME"] = (data, output) => output.DevName = data,
        ["DEVTYPE"] = (data, output) => output.DevType = data,
        ["PARTNAME"] = (data, output) => output.PartName = data,
        ["PARTUUID"] =  (data, output) => output.PartUuid = data,
    };
    
    public UEventData Parse(string? data)
    {
        UEventData output = new();
        if (data is null)
        {
            return output;
        }
        
        foreach (var line in data.Split("\n", StringSplitOptions.RemoveEmptyEntries))
        {
            var equalPos = line.IndexOf('=');
            var key = line.Substring(0, equalPos);
            var value = line.Substring(equalPos + 1);

            if (Parsers.TryGetValue(key, out var parser))
            {
                parser(value, output);
            }
        }

        return output;
    }
}