using Terminal.Gui.App;
using Terminal.Gui.Views;

namespace SharpDisk.Ui;

public class MainMenu : MenuBarv2
{
    public MainMenu()
    {
        Menus =
        [
            new MenuBarItemv2("_File", [
                new MenuItemv2("_Open", "Open block device or file to partition", () =>
                {
                    MessageBox.Query (50, 6, "Jebać", "Kurwa mać", "Ok");
                }),
                new MenuItemv2("_Quit", "", () => Application.RequestStop())
            ]),
            new MenuBarItemv2("_Drive", [
                _newGptMenuItem,
                _newMbrMenuItem
            ]),
            new MenuBarItemv2("_Help", [
                new MenuItemv2("_About", "About this program", () => {}),
            ])
        ];
    }
    
    private readonly MenuItemv2 _newGptMenuItem =
        new("New _GPT table", "Create new GPT table on drive", () => { })
        {
            Enabled = false
        };

    private readonly MenuItemv2 _newMbrMenuItem =
        new("New _MBR table", "Create new MBR table on drive", () => { })
        {
            Enabled = false
        };
}