namespace SharpDisk.Gui.Services;

/// <summary>
/// The host window's native file picker.
/// </summary>
/// <remarks>
/// Behind an interface because it is the one service that genuinely cannot run without Photino -
/// pages stay testable, and swapping the host later touches one class.
/// </remarks>
public interface IFileDialogService
{
    /// <summary>
    /// Asks the user for a file. Returns <c>null</c> when the dialog was dismissed.
    /// </summary>
    Task<string?> PickFileAsync(string title);
}
