using SharpDisk.Gui.Models;

namespace SharpDisk.Gui.Services;

/// <summary>
/// The one disk the window is currently looking at.
/// </summary>
/// <remarks>
/// A singleton holding shared state, so the drive list and the "open path" screen can both hand a
/// disk over and let the viewer page pick it up after navigation. Components subscribe to
/// <see cref="Changed"/> in <c>OnInitialized</c> and unsubscribe in <c>Dispose</c>; since handlers
/// may run off the UI thread, they re-render through <c>InvokeAsync(StateHasChanged)</c>.
/// </remarks>
public sealed class DiskSession
{
    private OpenedDisk? _current;

    /// <summary>Raised after <see cref="Current"/> changes.</summary>
    public event Action? Changed;

    public OpenedDisk? Current
    {
        get => _current;
        private set
        {
            _current = value;
            Changed?.Invoke();
        }
    }

    public bool HasDisk => _current is not null;

    public void Open(OpenedDisk disk) => Current = disk ?? throw new ArgumentNullException(nameof(disk));

    public void Close() => Current = null;
}
