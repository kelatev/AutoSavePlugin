using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AutoSavePlugin;

/// <summary>
/// Functions exported from the DLL and called by PL/SQL Developer.
/// No exception may escape an UnmanagedCallersOnly method, it would crash the IDE.
/// </summary>
internal static class Exports
{
    // Returned to the IDE and never freed
    private static readonly nint Description = Marshal.StringToHGlobalAnsi("AutoSavePlugin");
    private static readonly nint AboutText = Marshal.StringToHGlobalAnsi(
        "AutoSavePlugin\r\nSaves sources of program windows before and after compilation.");

    // Plug-In identification, a unique identifier is received and the description is returned
    [UnmanagedCallersOnly(EntryPoint = "IdentifyPlugIn", CallConvs = [typeof(CallConvCdecl)])]
    public static nint IdentifyPlugIn(int id)
    {
        Ide.PlugInId = id;
        return Description;
    }

    [UnmanagedCallersOnly(EntryPoint = "RegisterCallback", CallConvs = [typeof(CallConvCdecl)])]
    public static void RegisterCallback(int index, nint address) => Ide.Register(index, address);

    [UnmanagedCallersOnly(EntryPoint = "About", CallConvs = [typeof(CallConvCdecl)])]
    public static nint About() => AboutText;

    // Called by the Configure button in the Plug-In configuration dialog
    [UnmanagedCallersOnly(EntryPoint = "Configure", CallConvs = [typeof(CallConvCdecl)])]
    public static void Configure() => Guard(AutoSaver.Configure);

    // Returning false would cancel the execution, so always true
    [UnmanagedCallersOnly(EntryPoint = "BeforeExecuteWindow", CallConvs = [typeof(CallConvCdecl)])]
    public static int BeforeExecuteWindow(int windowType)
    {
        Guard(AutoSaver.BeforeExecute);
        return 1;
    }

    [UnmanagedCallersOnly(EntryPoint = "AfterExecuteWindow", CallConvs = [typeof(CallConvCdecl)])]
    public static void AfterExecuteWindow(int windowType, int result) =>
        Guard(() => AutoSaver.AfterExecute(result));

    private static void Guard(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            try
            {
                Ide.DebugLog("AutoSavePlugin: " + e.Message);
            }
            catch
            {
                // Nothing else can be done without crashing the IDE
            }
        }
    }
}
