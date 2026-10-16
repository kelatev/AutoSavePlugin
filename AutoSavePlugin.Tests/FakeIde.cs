using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AutoSavePlugin.Tests;

/// <summary>
/// Stand-in for PL/SQL Developer: implements the IDE callbacks the plug-in uses and
/// registers them in the native DLL the same way the IDE does.
/// </summary>
/// <remarks>
/// The native DLL keeps its own static state, so it is loaded once per test run and
/// the tests change the fake IDE state between calls instead.
/// Strings returned to the plug-in are never freed, like strings owned by the IDE.
/// </remarks>
internal static unsafe class FakeIde
{
    public static int Version = 1700;
    public static string PathPref = "";
    public static string Database = "host:1521/orcl";
    public static (string Type, string Owner, string Name)? WindowObject;
    public static Dictionary<string, string> Sources = [];
    public static Dictionary<string, string>? Extensions;
    public static readonly List<string> Log = [];

    private static nint _registerCallback;

    /// <summary>Registers all callbacks; extensions only when <see cref="Extensions"/> is set.</summary>
    public static void Register(nint dll)
    {
        _registerCallback = NativeLibrary.GetExport(dll, "RegisterCallback");
        Set(1, (nint)(delegate* unmanaged[Cdecl]<int>)&SysVersion);
        Set(12, (nint)(delegate* unmanaged[Cdecl]<byte**, byte**, byte**, void>)&GetConnectionInfo);
        Set(16, (nint)(delegate* unmanaged[Cdecl]<int>)&GetWindowHandle);
        Set(79, (nint)(delegate* unmanaged[Cdecl]<byte*, byte*, byte*, byte*>)&GetObjectSource);
        Set(110, (nint)(delegate* unmanaged[Cdecl]<byte**, byte**, byte**, byte**, int>)&GetWindowObject);
        Set(173, (nint)(delegate* unmanaged[Cdecl]<byte*, void>)&DebugLog);
        Set(212, (nint)(delegate* unmanaged[Cdecl]<int, byte*, byte*, byte*, byte*>)&GetPrefAsString);
        UpdateExtensionCallback();
    }

    /// <summary>Registers or removes IDE_GetProcEditExtension to match <see cref="Extensions"/>.</summary>
    public static void UpdateExtensionCallback() =>
        Set(109, Extensions == null ? 0 : (nint)(delegate* unmanaged[Cdecl]<byte*, byte*>)&GetProcEditExtension);

    public static void Reset()
    {
        Version = 1700;
        PathPref = "";
        Database = "host:1521/orcl";
        WindowObject = null;
        Sources = [];
        Extensions = null;
        Log.Clear();
        UpdateExtensionCallback();
    }

    // Does nothing until the DLL is loaded; Register applies the current state then
    private static void Set(int index, nint address)
    {
        if (_registerCallback != 0)
            ((delegate* unmanaged[Cdecl]<int, nint, void>)_registerCallback)(index, address);
    }

    private static byte* Ansi(string value) => (byte*)Marshal.StringToHGlobalAnsi(value);

    private static string Read(byte* value) => Marshal.PtrToStringAnsi((nint)value) ?? "";

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int SysVersion() => Version;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int GetWindowHandle() => 0;

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void GetConnectionInfo(byte** username, byte** password, byte** database)
    {
        *username = Ansi("hr");
        *password = Ansi("secret");
        *database = Ansi(Database);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static int GetWindowObject(byte** type, byte** owner, byte** name, byte** subObject)
    {
        if (WindowObject is not { } windowObject)
            return 0;
        *type = Ansi(windowObject.Type);
        *owner = Ansi(windowObject.Owner);
        *name = Ansi(windowObject.Name);
        *subObject = Ansi("");
        return 1;
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte* GetObjectSource(byte* type, byte* owner, byte* name) =>
        Sources.TryGetValue(Read(type), out var source) ? Ansi(source) : Ansi("");

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte* GetProcEditExtension(byte* type) =>
        Extensions != null && Extensions.TryGetValue(Read(type), out var extension) ? Ansi(extension) : Ansi("");

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static void DebugLog(byte* message) => Log.Add(Read(message));

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
    private static byte* GetPrefAsString(int plugInId, byte* set, byte* name, byte* defaultValue) =>
        Read(name) == "path" && PathPref.Length > 0 ? Ansi(PathPref) : Ansi(Read(defaultValue));
}
