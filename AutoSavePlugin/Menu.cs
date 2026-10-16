namespace AutoSavePlugin;

/// <summary>Menu items the plug-in adds to PL/SQL Developer.</summary>
/// <remarks>
/// The IDE calls CreateMenuItem for every index from 1 to 99 and creates an item for each
/// non-empty result; OnMenuClick later receives the index of the clicked item.
/// Version 12 replaced the classic menu with a ribbon, which uses a different text format.
/// </remarks>
public static class Menu
{
    public const int Group = 10;
    public const int ObjectHistory = 11;
    public const int RootFolder = 12;
    public const int Settings = 13;

    // First version with the ribbon menu (12.0)
    private const int RibbonVersion = 1200;

    /// <summary>Text that creates the menu item for an index, or "" for no item.</summary>
    /// <param name="index">Index from 1 to 99.</param>
    /// <param name="ideVersion">SYS_Version of the IDE, e.g. 1700 for 17.0.</param>
    public static string Item(int index, int ideVersion)
    {
        if (ideVersion >= RibbonVersion)
        {
            // Without a TAB= line the group is created in the Plug-Ins tab
            return index switch
            {
                Group => "GROUP=AutoSave",
                ObjectHistory => "ITEM=Object history",
                RootFolder => "ITEM=Autosave folder",
                Settings => "ITEM=Settings...",
                _ => "",
            };
        }

        // Classic menu: path separated by "/", "-" is a separator, "&" marks the access key
        return index switch
        {
            Group => "Tools / -",
            ObjectHistory => "Tools / AutoSave / Object &history",
            RootFolder => "Tools / AutoSave / Autosave &folder",
            Settings => "Tools / AutoSave / &Settings...",
            _ => "",
        };
    }

    /// <summary>Runs the action of a clicked menu item.</summary>
    internal static void Click(int index)
    {
        switch (index)
        {
            case ObjectHistory:
                AutoSaver.OpenObjectHistory();
                break;
            case RootFolder:
                AutoSaver.OpenRootFolder();
                break;
            case Settings:
                AutoSaver.Configure();
                break;
        }
    }
}
