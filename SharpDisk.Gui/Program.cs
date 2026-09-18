using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace SharpDisk.Gui;

internal static class Program
{
    // No-op on Linux, but WebView2 on Windows wants the STA apartment.
    [STAThread]
    private static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);
        builder.Services.AddLogging();

        builder.RootComponents.Add<App>("app");

        var app = builder.Build();

        app.MainWindow
            .SetTitle("SharpDisk")
            .SetUseOsDefaultSize(false)
            .SetWidth(1100)
            .SetHeight(760);

        // Without this a managed exception on a background thread kills the window silently.
        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString() ?? "unknown");

        app.Run();
    }
}
