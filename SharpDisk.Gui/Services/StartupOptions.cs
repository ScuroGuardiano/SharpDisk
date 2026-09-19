namespace SharpDisk.Gui.Services;

/// <summary>
/// What the process was asked to do on the command line.
/// </summary>
/// <param name="Path">
/// A file or device to open as soon as the window is up, or <c>null</c> to start on the drive list.
/// </param>
/// <remarks>
/// <c>SharpDisk.Gui /path/to/image.iso</c> or <c>SharpDisk.Gui /dev/sda</c>. Handy when the same
/// image is being looked at over and over, and it is what makes the app scriptable enough to
/// screenshot.
/// </remarks>
public sealed record StartupOptions(string? Path)
{
    public static StartupOptions Parse(string[] args)
        => new(args.FirstOrDefault(arg => !arg.StartsWith('-')));
}
