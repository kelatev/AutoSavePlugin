using System.Runtime.InteropServices;

namespace AutoSavePlugin;

/// <summary>
/// PL/SQL Developer callbacks. The IDE hands over function addresses through
/// RegisterCallback(index, address); only the ones this plug-in uses are wrapped.
/// All strings cross the boundary as ANSI (system code page), Bool is a 4-byte LongBool.
/// </summary>
/// <remarks>
/// Each IDE function is called through an unmanaged function pointer
/// (<c>delegate* unmanaged[Cdecl]&lt;...&gt;</c>) cast from the stored address. The type arguments
/// are the parameter types followed by the return type, and must match the plug-in API exactly:
/// a wrong signature corrupts the stack instead of raising an error.
/// </remarks>
internal static unsafe class Ide
{
    // Callback indexes from the PL/SQL Developer plug-in API.
    // Older IDE versions may not provide the newer ones (240, 245), hence Has() checks.
    private const int SysVersionIx = 1;            // SYS_Version: IDE version, e.g. 1700 for 17.0
    private const int GetConnectionInfoIx = 12;    // IDE_GetConnectionInfo: main connection
    private const int GetAppHandleIx = 15;         // IDE_GetAppHandle: Application handle
    private const int GetWindowHandleIx = 16;      // IDE_GetWindowHandle: main window, parent for dialogs
    private const int GetObjectSourceIx = 79;      // IDE_GetObjectSource: source text from the database
    private const int GetProcEditExtensionIx = 109; // IDE_GetProcEditExtension: file extension per object type
    private const int GetWindowObjectIx = 110;     // IDE_GetWindowObject: object of the current window
    private const int DebugLogIx = 173;            // IDE_DebugLog: line in the IDE debug log
    private const int GetPrefAsStringIx = 212;     // IDE_GetPrefAsString: read plug-in preference
    private const int SetPrefAsStringIx = 215;     // IDE_SetPrefAsString: write plug-in preference
    private const int GetConnectionInfoExIx = 240; // IDE_GetConnectionInfoEx: connection by index
    private const int GetWindowConnectionIx = 245; // IDE_GetWindowConnection: connection index of the window

    // Address of every IDE function by its index; 0 when the IDE did not register it.
    // The size covers all indexes the API currently defines.
    private static readonly nint[] Callbacks = new nint[512];

    /// <summary>ID assigned by the IDE in IdentifyPlugIn, required by the preference functions.</summary>
    public static int PlugInId { get; set; }

    public static void Register(int index, nint address)
    {
        if ((uint)index < (uint)Callbacks.Length)
            Callbacks[index] = address;
    }

    private static bool Has(int index) => Callbacks[index] != 0;

    // Address of a callback the plug-in cannot work without. Calling address 0 would be an
    // access violation that kills the IDE; an exception is caught by Exports.Guard instead.
    private static nint Required(int index) =>
        Callbacks[index] != 0
            ? Callbacks[index]
            : throw new InvalidOperationException($"IDE callback {index} is not available");

    /// <summary>IDE version as major * 100 + minor (1200 = 12.0), or 0 if unknown.</summary>
    public static int SysVersion() =>
        Has(SysVersionIx) ? ((delegate* unmanaged[Cdecl]<int>)Callbacks[SysVersionIx])() : 0;

    /// <summary>Handle of the IDE main window, used as the owner of the plug-in dialogs.</summary>
    /// <remarks>Window handles fit in 32 bits even on x64, so the API returns them as Integer.</remarks>
    public static nint MainWindow()
    {
        if (Has(GetWindowHandleIx))
            return ((delegate* unmanaged[Cdecl]<int>)Callbacks[GetWindowHandleIx])();
        return Has(GetAppHandleIx) ? ((delegate* unmanaged[Cdecl]<int>)Callbacks[GetAppHandleIx])() : 0;
    }

    /// <summary>
    /// File extension configured in the IDE for an object type (without the dot),
    /// or "" if the IDE does not provide it. Accepts the "... SPEC AND BODY" types.
    /// </summary>
    public static string GetProcEditExtension(string objectType)
    {
        if (!Has(GetProcEditExtensionIx))
            return "";
        using var type = new AnsiString(objectType);
        var extension = ((delegate* unmanaged[Cdecl]<byte*, byte*>)Callbacks[GetProcEditExtensionIx])(type.Pointer);
        return Decode(extension).Trim().TrimStart('.');
    }

    /// <summary>Writes a line to the IDE debug log.</summary>
    public static void DebugLog(string message)
    {
        if (!Has(DebugLogIx))
            return;
        using var text = new AnsiString(message);
        ((delegate* unmanaged[Cdecl]<byte*, void>)Callbacks[DebugLogIx])(text.Pointer);
    }

    /// <summary>Object of the current window, or null for windows without one (SQL, Test...).</summary>
    public static (string Type, string Owner, string Name)? GetWindowObject()
    {
        // The IDE fills the pointers with strings it owns; they are copied right away
        // and must not be freed. SubObject (e.g. a procedure inside a package) is not needed.
        byte* type = null, owner = null, name = null, subObject = null;
        var ok = ((delegate* unmanaged[Cdecl]<byte**, byte**, byte**, byte**, int>)Required(GetWindowObjectIx))(
            &type, &owner, &name, &subObject);
        if (ok == 0)
            return null;
        return (Decode(type), Decode(owner), Decode(name));
    }

    /// <summary>Database of the connection used by the current window.</summary>
    /// <remarks>
    /// In newer IDE versions a window can use its own connection instead of the main one,
    /// so the window's connection is asked first. Older versions only know the main connection.
    /// Username and password are returned by the same calls but are not used.
    /// </remarks>
    public static string GetWindowDatabase()
    {
        byte* username = null, password = null, database = null, connectAs = null;

        if (Has(GetWindowConnectionIx) && Has(GetConnectionInfoExIx))
        {
            var ix = ((delegate* unmanaged[Cdecl]<int>)Callbacks[GetWindowConnectionIx])();
            var ok = ((delegate* unmanaged[Cdecl]<int, byte**, byte**, byte**, byte**, int>)Callbacks[GetConnectionInfoExIx])(
                ix, &username, &password, &database, &connectAs);
            if (ok != 0)
                return Decode(database);
        }

        ((delegate* unmanaged[Cdecl]<byte**, byte**, byte**, void>)Required(GetConnectionInfoIx))(
            &username, &password, &database);
        return Decode(database);
    }

    /// <summary>Raw source bytes as the IDE returns them (kept in the IDE's ANSI encoding).</summary>
    /// <remarks>
    /// The bytes are copied without decoding, so the saved file is byte-for-byte what the IDE
    /// returned and no characters can be lost in a round trip through .NET strings.
    /// </remarks>
    public static byte[] GetObjectSource(string objectType, string owner, string name)
    {
        using var t = new AnsiString(objectType);
        using var o = new AnsiString(owner);
        using var n = new AnsiString(name);
        var source = ((delegate* unmanaged[Cdecl]<byte*, byte*, byte*, byte*>)Required(GetObjectSourceIx))(
            t.Pointer, o.Pointer, n.Pointer);
        return source == null ? [] : MemoryMarshal.CreateReadOnlySpanFromNullTerminated(source).ToArray();
    }

    /// <summary>Reads a string preference of this plug-in, or defaultValue if it is not set.</summary>
    public static string GetPref(string name, string defaultValue)
    {
        // An empty preference set means the default set; the plug-in ID keeps
        // the names separate from other plug-ins.
        using var set = new AnsiString("");
        using var key = new AnsiString(name);
        using var def = new AnsiString(defaultValue);
        var value = ((delegate* unmanaged[Cdecl]<int, byte*, byte*, byte*, byte*>)Required(GetPrefAsStringIx))(
            PlugInId, set.Pointer, key.Pointer, def.Pointer);
        return Decode(value);
    }

    /// <summary>Stores a string preference of this plug-in.</summary>
    public static void SetPref(string name, string value)
    {
        using var set = new AnsiString("");
        using var key = new AnsiString(name);
        using var val = new AnsiString(value);
        ((delegate* unmanaged[Cdecl]<int, byte*, byte*, byte*, int>)Required(SetPrefAsStringIx))(
            PlugInId, set.Pointer, key.Pointer, val.Pointer);
    }

    // Copies an IDE-owned ANSI string into a .NET string; null becomes "".
    private static string Decode(byte* value) => Marshal.PtrToStringAnsi((nint)value) ?? "";

    /// <summary>Null-terminated ANSI copy of a string, freed on Dispose.</summary>
    /// <remarks>
    /// A ref struct cannot be stored or captured, which guarantees the memory is only used
    /// for the duration of the IDE call it was created for.
    /// </remarks>
    private readonly ref struct AnsiString(string value)
    {
        private readonly nint _handle = Marshal.StringToHGlobalAnsi(value);

        public byte* Pointer => (byte*)_handle;

        public void Dispose() => Marshal.FreeHGlobal(_handle);
    }
}
