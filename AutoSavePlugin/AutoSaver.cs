namespace AutoSavePlugin;

/// <summary>Saves the source of the program window's object before and after compilation.</summary>
/// <remarks>
/// One compilation produces two copies in the same timestamp folder:
/// <c>Before</c> is the source stored in the database before it is replaced,
/// <c>After</c> is the source the database holds once compilation succeeded.
/// Together they let you restore any previous version of an object.
/// </remarks>
internal static class AutoSaver
{
    public const string Caption = "AutoSavePlugin";

    // Name of the plug-in preference that stores the root folder.
    // PL/SQL Developer keeps plug-in preferences per plug-in ID in its own preference files.
    private const string PathPref = "path";

    // Suggested in the folder picker while no path has been chosen yet
    private const string DefaultPath = @"C:\AutoSavePlugin";

    // AfterExecuteWindow result: 0 = finished with error, 1 = finished with the option
    // to continue (e.g. "next page" in a SQL Window), 2 = finished successfully
    private const int ExecutedSuccessfully = 2;

    // BeforeExecuteWindow and AfterExecuteWindow are separate calls for the same execution.
    // The root folder and the timestamp are decided once in BeforeExecute and reused by
    // AfterExecute, so both copies end up next to each other and the user is asked at most once.
    private static string _root = "";
    private static string _timestamp = "";

    public static void BeforeExecute()
    {
        _timestamp = SavePaths.Timestamp(DateTime.Now);
        _root = GetRootPath(askIfMissing: true);
        Save(before: true);
    }

    public static void AfterExecute(int result)
    {
        // On failure the database still has the old source, which is already the Before copy
        if (result == ExecutedSuccessfully)
            Save(before: false);
    }

    /// <summary>Lets the user choose the root folder and stores it in the plug-in preferences.</summary>
    public static void Configure()
    {
        var current = Ide.GetPref(PathPref, DefaultPath);
        var folder = Win32.PickFolder(Ide.MainWindow(), "Folder for autosaved sources", current);

        // Cancel keeps the previous setting
        if (!string.IsNullOrWhiteSpace(folder))
            Ide.SetPref(PathPref, folder.Trim());
    }

    /// <summary>Opens the root folder with all autosaved sources in Explorer.</summary>
    public static void OpenRootFolder()
    {
        var root = GetRootPath(askIfMissing: true);
        if (root.Length == 0)
            return;

        Directory.CreateDirectory(root);
        Win32.OpenFolder(Ide.MainWindow(), root);
    }

    /// <summary>Opens the folder with saved versions of the object in the current window.</summary>
    public static void OpenObjectHistory()
    {
        var root = GetRootPath(askIfMissing: true);
        if (root.Length == 0)
            return;

        if (Ide.GetWindowObject() is not { } windowObject || windowObject.Name.Length == 0)
        {
            Win32.ShowInfo(Ide.MainWindow(),
                "The current window is not a Program Window with a database object.", Caption);
            return;
        }

        var (type, owner, name) = windowObject;
        var folder = SavePaths.ObjectFolder(root, Ide.GetWindowDatabase(), owner, type, name);
        if (!Directory.Exists(folder))
        {
            Win32.ShowInfo(Ide.MainWindow(),
                $"No saved versions of {owner}.{name} yet.\r\nThey appear after the object is compiled.", Caption);
            return;
        }

        Win32.OpenFolder(Ide.MainWindow(), folder);
    }

    // Returns the configured root folder. If none is set and askIfMissing is true, offers to
    // choose one right away; an empty result means the user declined.
    private static string GetRootPath(bool askIfMissing)
    {
        var path = Ide.GetPref(PathPref, "").Trim();
        if (path.Length == 0 && askIfMissing &&
            Win32.AskYesNo(Ide.MainWindow(), "The path to save the files is not specified.\r\nSpecify now?", Caption))
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

        // Only windows bound to a database object (Program Windows) have something to save;
        // SQL, Test and Command windows return no object and are skipped.
        if (Ide.GetWindowObject() is not { } windowObject || windowObject.Name.Length == 0)
            return;
        var (type, owner, name) = windowObject;

        // Source is read from the database, not from the editor: Before captures the version
        // that is about to be overwritten, After captures what was actually compiled.
        // Packages and object types are always saved as a whole (spec + body),
        // whichever part is being compiled.
        var source = SavePaths.SpecAndBody(type) is { } parts
            ? SavePaths.JoinSpecAndBody(Ide.GetObjectSource(parts.Spec, owner, name),
                                        Ide.GetObjectSource(parts.Body, owner, name))
            : Ide.GetObjectSource(type, owner, name);

        // Nothing in the database yet, e.g. the very first compilation of a new object
        if (source.Length == 0)
            return;

        var file = SavePaths.FilePath(
            SavePaths.ObjectFolder(_root, Ide.GetWindowDatabase(), owner, type, name),
            name, GetExtension(type), _timestamp, before);
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);

        // Bytes are written exactly as received, in the IDE's ANSI encoding
        File.WriteAllBytes(file, source);
    }

    // Same extension the IDE uses when saving the object to a file
    // (Preferences > Files > Extensions), so it opens the copy as a Program Window
    private static string GetExtension(string objectType)
    {
        var extension = Ide.GetProcEditExtension(SavePaths.ExtensionType(objectType));
        return extension.Length > 0 ? extension : SavePaths.DefaultExtension(objectType);
    }
}
