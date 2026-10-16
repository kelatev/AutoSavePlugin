namespace AutoSavePlugin;

/// <summary>Saves the source of the program window's object before and after compilation.</summary>
internal static class AutoSaver
{
    private const string Caption = "AutoSavePlugin";
    private const string PathPref = "path";
    private const string DefaultPath = @"C:\AutoSavePlugin";

    // Resolved once in BeforeExecute and reused by AfterExecute of the same execution
    private static string _root = "";
    private static string _timestamp = "";

    public static void BeforeExecute()
    {
        _timestamp = SavePaths.Timestamp(DateTime.Now);
        _root = GetRootPath();
        Save(before: true);
    }

    public static void AfterExecute(int result)
    {
        if (result == 2)
            Save(before: false);
    }

    public static void Configure()
    {
        var current = Ide.GetPref(PathPref, DefaultPath);
        var folder = Win32.PickFolder(Ide.AppHandle(), "Folder for autosaved sources", current);
        if (!string.IsNullOrWhiteSpace(folder))
            Ide.SetPref(PathPref, folder.Trim());
    }

    private static string GetRootPath()
    {
        var path = Ide.GetPref(PathPref, "").Trim();
        if (path.Length == 0 &&
            Win32.AskYesNo(Ide.AppHandle(), "The path to save the files is not specified.\r\nSpecify now?", Caption))
        {
            Configure();
            path = Ide.GetPref(PathPref, "").Trim();
        }
        return path;
    }

    private static void Save(bool before)
    {
        if (_root.Length == 0)
            return;

        if (Ide.GetWindowObject() is not { } windowObject || windowObject.Name.Length == 0)
            return;
        var (type, owner, name) = windowObject;

        var source = SavePaths.IsPackage(type)
            ? SavePaths.JoinPackage(Ide.GetObjectSource("PACKAGE", owner, name),
                                    Ide.GetObjectSource("PACKAGE BODY", owner, name))
            : Ide.GetObjectSource(type, owner, name);
        if (source.Length == 0)
            return;

        var file = SavePaths.FilePath(_root, Ide.GetWindowDatabase(), owner, type, name, _timestamp, before);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllBytes(file, source);
    }
}
