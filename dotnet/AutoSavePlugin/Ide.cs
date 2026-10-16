using System.Runtime.InteropServices;

namespace AutoSavePlugin;

/// <summary>
/// PL/SQL Developer callbacks. The IDE hands over function addresses through
/// RegisterCallback(index, address); only the ones this plug-in uses are wrapped.
/// All strings cross the boundary as ANSI (system code page), Bool is a 4-byte LongBool.
/// </summary>
internal static unsafe class Ide
{
    private const int GetConnectionInfoIx = 12;
    private const int GetAppHandleIx = 15;
    private const int GetObjectSourceIx = 79;
    private const int GetWindowObjectIx = 110;
    private const int DebugLogIx = 173;
    private const int GetPrefAsStringIx = 212;
    private const int SetPrefAsStringIx = 215;
    private const int GetConnectionInfoExIx = 240;
    private const int GetWindowConnectionIx = 245;

    private static readonly nint[] Callbacks = new nint[512];

    public static int PlugInId { get; set; }

    public static void Register(int index, nint address)
    {
        if ((uint)index < (uint)Callbacks.Length)
            Callbacks[index] = address;
    }

    private static bool Has(int index) => Callbacks[index] != 0;

    public static nint AppHandle() =>
        Has(GetAppHandleIx) ? ((delegate* unmanaged[Cdecl]<int>)Callbacks[GetAppHandleIx])() : 0;

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
        byte* type = null, owner = null, name = null, subObject = null;
        var ok = ((delegate* unmanaged[Cdecl]<byte**, byte**, byte**, byte**, int>)Callbacks[GetWindowObjectIx])(
            &type, &owner, &name, &subObject);
        if (ok == 0)
            return null;
        return (Decode(type), Decode(owner), Decode(name));
    }

    /// <summary>Database of the connection used by the current window.</summary>
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

        ((delegate* unmanaged[Cdecl]<byte**, byte**, byte**, void>)Callbacks[GetConnectionInfoIx])(
            &username, &password, &database);
        return Decode(database);
    }

    /// <summary>Raw source bytes as the IDE returns them (kept in the IDE's ANSI encoding).</summary>
    public static byte[] GetObjectSource(string objectType, string owner, string name)
    {
        using var t = new AnsiString(objectType);
        using var o = new AnsiString(owner);
        using var n = new AnsiString(name);
        var source = ((delegate* unmanaged[Cdecl]<byte*, byte*, byte*, byte*>)Callbacks[GetObjectSourceIx])(
            t.Pointer, o.Pointer, n.Pointer);
        return source == null ? [] : MemoryMarshal.CreateReadOnlySpanFromNullTerminated(source).ToArray();
    }

    public static string GetPref(string name, string defaultValue)
    {
        using var set = new AnsiString("");
        using var key = new AnsiString(name);
        using var def = new AnsiString(defaultValue);
        var value = ((delegate* unmanaged[Cdecl]<int, byte*, byte*, byte*, byte*>)Callbacks[GetPrefAsStringIx])(
            PlugInId, set.Pointer, key.Pointer, def.Pointer);
        return Decode(value);
    }

    public static void SetPref(string name, string value)
    {
        using var set = new AnsiString("");
        using var key = new AnsiString(name);
        using var val = new AnsiString(value);
        ((delegate* unmanaged[Cdecl]<int, byte*, byte*, byte*, int>)Callbacks[SetPrefAsStringIx])(
            PlugInId, set.Pointer, key.Pointer, val.Pointer);
    }

    private static string Decode(byte* value) => Marshal.PtrToStringAnsi((nint)value) ?? "";

    /// <summary>Null-terminated ANSI copy of a string, freed on Dispose.</summary>
    private readonly ref struct AnsiString(string value)
    {
        private readonly nint _handle = Marshal.StringToHGlobalAnsi(value);

        public byte* Pointer => (byte*)_handle;

        public void Dispose() => Marshal.FreeHGlobal(_handle);
    }
}
