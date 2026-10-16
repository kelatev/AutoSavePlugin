using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AutoSavePlugin;

/// <summary>The few Windows dialogs the plug-in needs.</summary>
internal static unsafe partial class Win32
{
    private const uint MB_YESNO = 0x04;
    private const uint MB_ICONQUESTION = 0x20;
    private const int IDYES = 6;

    private const uint BIF_RETURNONLYFSDIRS = 0x01;
    private const uint BIF_EDITBOX = 0x10;
    private const uint BIF_NEWDIALOGSTYLE = 0x40;
    private const uint BFFM_INITIALIZED = 1;
    private const uint BFFM_SETSELECTIONW = 0x0400 + 103;
    private const uint COINIT_APARTMENTTHREADED = 0x2;
    private const int MAX_PATH = 260;

    public static bool AskYesNo(nint owner, string text, string caption) =>
        MessageBoxW(owner, text, caption, MB_YESNO | MB_ICONQUESTION) == IDYES;

    /// <summary>Shows the folder picker; returns null when cancelled.</summary>
    public static string? PickFolder(nint owner, string title, string initialPath)
    {
        var initialized = CoInitializeEx(0, COINIT_APARTMENTTHREADED) >= 0;
        try
        {
            var displayName = stackalloc char[MAX_PATH + 1];
            fixed (char* titlePtr = title)
            fixed (char* initialPtr = initialPath)
            {
                var info = new BROWSEINFOW
                {
                    hwndOwner = owner,
                    pszDisplayName = displayName,
                    lpszTitle = titlePtr,
                    ulFlags = BIF_RETURNONLYFSDIRS | BIF_EDITBOX | BIF_NEWDIALOGSTYLE,
                    lpfn = &BrowseCallback,
                    lParam = (nint)initialPtr,
                };

                var pidl = SHBrowseForFolderW(&info);
                if (pidl == 0)
                    return null;
                try
                {
                    var path = stackalloc char[MAX_PATH + 1];
                    return SHGetPathFromIDListW(pidl, path) != 0 ? new string(path) : null;
                }
                finally
                {
                    CoTaskMemFree(pidl);
                }
            }
        }
        finally
        {
            if (initialized)
                CoUninitialize();
        }
    }

    // Preselects the current path when the dialog opens
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int BrowseCallback(nint hwnd, uint message, nint lParam, nint data)
    {
        if (message == BFFM_INITIALIZED && data != 0)
            SendMessageW(hwnd, BFFM_SETSELECTIONW, 1, data);
        return 0;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BROWSEINFOW
    {
        public nint hwndOwner;
        public nint pidlRoot;
        public char* pszDisplayName;
        public char* lpszTitle;
        public uint ulFlags;
        public delegate* unmanaged[Stdcall]<nint, uint, nint, nint, int> lpfn;
        public nint lParam;
        public int iImage;
    }

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    private static partial int MessageBoxW(nint hWnd, string text, string caption, uint type);

    [LibraryImport("user32.dll")]
    private static partial nint SendMessageW(nint hWnd, uint msg, nint wParam, nint lParam);

    [LibraryImport("shell32.dll")]
    private static partial nint SHBrowseForFolderW(BROWSEINFOW* info);

    [LibraryImport("shell32.dll")]
    private static partial int SHGetPathFromIDListW(nint pidl, char* path);

    [LibraryImport("ole32.dll")]
    private static partial int CoInitializeEx(nint reserved, uint coInit);

    [LibraryImport("ole32.dll")]
    private static partial void CoUninitialize();

    [LibraryImport("ole32.dll")]
    private static partial void CoTaskMemFree(nint pv);
}
