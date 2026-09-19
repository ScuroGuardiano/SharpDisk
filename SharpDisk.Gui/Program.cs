using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using SharpDisk.Gui.Services;

namespace SharpDisk.Gui;

internal static class Program
{
    // No-op on Linux, but WebView2 on Windows wants the STA apartment.
    [STAThread]
    private static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();

        // Everything the pages need, behind interfaces. Singletons across the board: this is a
        // single-window desktop app, there is no request scope to speak of, and DiskSession has to
        // be shared or the drive list and the viewer page would each get their own copy.
        builder.Services.AddSingleton<IBlockDeviceService, BlockDeviceService>();
        builder.Services.AddSingleton<IDiskOpenService, DiskOpenService>();
        builder.Services.AddSingleton<IDiskMapBuilder, DiskMapBuilder>();
        builder.Services.AddSingleton<IAnalysisReportBuilder, AnalysisReportBuilder>();

        // Takes PhotinoWindow, which Photino.Blazor itself registers as a singleton instance.
        builder.Services.AddSingleton<IFileDialogService, PhotinoFileDialogService>();

        builder.Services.AddSingleton<DiskSession>();
        builder.Services.AddSingleton(StartupOptions.Parse(args));

        builder.RootComponents.Add<App>("app");

        var app = builder.Build();

        app.MainWindow
            .SetTitle("SharpDisk")
            .SetUseOsDefaultSize(false)
            .SetWidth(1280)
            .SetHeight(860);

        // Without this a managed exception on a background thread kills the window silently.
        AppDomain.CurrentDomain.UnhandledException += (_, error) =>
            app.MainWindow.ShowMessage("Fatal exception", error.ExceptionObject.ToString() ?? "unknown");

        app.Run();
    }
}
