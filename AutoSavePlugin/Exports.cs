using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AutoSavePlugin;

/// <summary>
/// Functions exported from the DLL and called by PL/SQL Developer.
/// </summary>
/// <remarks>
/// <para>
/// PL/SQL Developer looks up plug-in functions by their exported names, so every
/// <c>EntryPoint</c> below must match the name from the plug-in API exactly. All exports use
/// the <c>cdecl</c> calling convention (it only matters for the 32-bit build; x64 has a single
/// convention).
/// </para>
/// <para>
/// Call order when the IDE starts: <c>IdentifyPlugIn</c> first, then <c>RegisterCallback</c> once
/// for every IDE function, and only after that any of the event functions.
/// </para>
/// <para>
/// No exception may escape an <c>UnmanagedCallersOnly</c> method: the runtime would terminate
/// the whole IDE process. Every function that does real work therefore goes through <see cref="Guard"/>.
/// </para>
/// </remarks>
internal static class Exports
{
    // Strings handed to the IDE must stay valid for as long as the DLL is loaded,
    // so they are allocated once in unmanaged memory and never freed.
    private static readonly nint Description = Marshal.StringToHGlobalAnsi("AutoSavePlugin");
    private static readonly nint AboutText = Marshal.StringToHGlobalAnsi(
        "AutoSavePlugin\r\nSaves sources of program windows before and after compilation.");

    // Plug-In identification, a unique identifier is received and the description is returned.
    // The identifier is needed later to read and write this plug-in's preferences.
    [UnmanagedCallersOnly(EntryPoint = "IdentifyPlugIn", CallConvs = [typeof(CallConvCdecl)])]
    public static nint IdentifyPlugIn(int id)
    {
        Ide.PlugInId = id;
        return Description;
    }

    // Called by the IDE once per IDE function: index identifies the function, address is its
    // entry point. See Ide for the indexes this plug-in uses.
    [UnmanagedCallersOnly(EntryPoint = "RegisterCallback", CallConvs = [typeof(CallConvCdecl)])]
    public static void RegisterCallback(int index, nint address) => Ide.Register(index, address);

    // Text shown by the About button in the Plug-In configuration dialog
    [UnmanagedCallersOnly(EntryPoint = "About", CallConvs = [typeof(CallConvCdecl)])]
    public static nint About() => AboutText;

    // Called by the Configure button in the Plug-In configuration dialog
    [UnmanagedCallersOnly(EntryPoint = "Configure", CallConvs = [typeof(CallConvCdecl)])]
    public static void Configure() => Guard(AutoSaver.Configure);

    // Called before a window is executed (e.g. F8 in a Program Window compiles the object).
    // The result is a Bool: returning false (0) would cancel the execution, and a backup
    // plug-in must never block the user's work, so it is always true (1).
    [UnmanagedCallersOnly(EntryPoint = "BeforeExecuteWindow", CallConvs = [typeof(CallConvCdecl)])]
    public static int BeforeExecuteWindow(int windowType)
    {
        Guard(AutoSaver.BeforeExecute);
        return 1;
    }

    // Called after the window has been executed; result tells how the execution ended
    // (see AutoSaver.AfterExecute).
    [UnmanagedCallersOnly(EntryPoint = "AfterExecuteWindow", CallConvs = [typeof(CallConvCdecl)])]
    public static void AfterExecuteWindow(int windowType, int result) =>
        Guard(() => AutoSaver.AfterExecute(result));

    // Runs the action and turns any exception into a line in the IDE debug log,
    // so a failed save never crashes PL/SQL Developer.
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
