using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace AutoSavePlugin;

/// <summary>The few Windows dialogs the plug-in needs.</summary>
/// <remarks>
/// NativeAOT has no WinForms, so the dialogs come straight from the Windows API.
/// <c>LibraryImport</c> generates the interop code at compile time, which keeps it AOT-compatible.
/// The wide (<c>W</c>) functions are used so paths with any characters work.
/// </remarks>
internal static unsafe partial class Win32
{
    // MessageBox flags and result
    private const uint MB_YESNO = 0x04;
    private const uint MB_ICONQUESTION = 0x20;
    private const int IDYES = 6;

    // SHBrowseForFolder flags: only real file system folders, a text box to type or paste
    // a path, and the resizable dialog with a "Make New Folder" button
    private const uint BIF_RETURNONLYFSDIRS = 0x01;
    private const uint BIF_EDITBOX = 0x10;
    private const uint BIF_NEWDIALOGSTYLE = 0x40;

    // Folder picker callback messages
    private const uint BFFM_INITIALIZED = 1;
    private const uint BFFM_SETSELECTIONW = 0x0400 + 103; // WM_USER + 103

    private const uint COINIT_APARTMENTTHREADED = 0x2;
    private const int MAX_PATH = 260;

    /// <summary>Modal Yes/No question; true when the user answered Yes.</summary>
    public static bool AskYesNo(nint owner, string text, string caption) =>
        MessageBoxW(owner, text, caption, MB_YESNO | MB_ICONQUESTION) == IDYES;

    /// <summary>Shows the folder picker; returns null when cancelled.</summary>
    /// <param name="owner">Parent window, so the dialog stays on top of the IDE.</param>
    /// <param name="title">Text shown above the folder tree.</param>
    /// <param name="initialPath">Folder selected when the dialog opens.</param>
    public static string? PickFolder(nint owner, string title, string initialPath)
    {
        // BIF_NEWDIALOGSTYLE requires COM on the calling thread. The IDE has normally
        // initialized it already (then the call just adds a reference); it is released
        // only if this call succeeded.
        var initialized = CoInitializeEx(0, COINIT_APARTMENTTHREADED) >= 0;
        try
        {
            var displayName = stackalloc char[MAX_PATH + 1];

            // fixed pins the strings so their addresses stay valid while the dialog is open
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
                    lParam = (nint)initialPtr, // passed back to BrowseCallback as data
                };

                // Returns the selected folder as a PIDL (shell item ID list), or 0 on Cancel
                var pidl = SHBrowseForFolderW(&info);
                if (pidl == 0)
                    return null;
                try
                {
                    // Virtual folders (e.g. "This PC") have no file system path
                    var path = stackalloc char[MAX_PATH + 1];
                    return SHGetPathFromIDListW(pidl, path) != 0 ? new string(path) : null;
                }
                finally
                {
                    // The PIDL is allocated by the shell with the COM allocator
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

    // Preselects the current path when the dialog opens.
    // Called by Windows (stdcall), so it is an unmanaged entry point like the plug-in exports.
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int BrowseCallback(nint hwnd, uint message, nint lParam, nint data)
    {
        // wParam 1 means data is a path string rather than a PIDL
        if (message == BFFM_INITIALIZED && data != 0)
            SendMessageW(hwnd, BFFM_SETSELECTIONW, 1, data);
        return 0;
    }

    // Layout must match the Windows BROWSEINFOW structure field by field
    [StructLayout(LayoutKind.Sequential)]
    private struct BROWSEINFOW
    {
        public nint hwndOwner;
        public nint pidlRoot;             // 0 = start at the desktop
        public char* pszDisplayName;      // receives the display name of the selected folder
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
