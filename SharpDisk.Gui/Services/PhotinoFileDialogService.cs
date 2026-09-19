using Photino.NET;

namespace SharpDisk.Gui.Services;

/// <inheritdoc cref="IFileDialogService" />
internal sealed class PhotinoFileDialogService(PhotinoWindow window) : IFileDialogService
{
    public async Task<string?> PickFileAsync(string title)
    {
        var picked = await window.ShowOpenFileAsync(
            title,
            multiSelect: false,
            filters:
            [
                ("Obrazy dysków", ["iso", "img", "bin", "raw", "dd"]),
                ("Wszystkie pliki", ["*"]),
            ]);

        return picked?.FirstOrDefault();
    }
}
