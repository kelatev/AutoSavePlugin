using System.Runtime.InteropServices;

namespace AutoSavePlugin.Tests;

/// <summary>
/// Loads the published native DLL and drives it through its exports with a fake IDE,
/// the same way PL/SQL Developer does. Catches mistakes unit tests cannot see:
/// missing or misnamed exports, wrong callback signatures, crashes in native code.
/// </summary>
/// <remarks>
/// Needs the DLL from <c>dotnet publish -r win-x64 -o publish</c>. Its path is taken from the
/// AUTOSAVE_PLUGIN_DLL environment variable or defaults to <c>publish\AutoSavePlugin.dll</c>
/// in the repository root; without it the tests are skipped (or fail when the variable is set).
/// Every test sets a path preference, because an empty one would open a dialog.
/// </remarks>
public sealed unsafe class NativePluginTests : IDisposable
{
    private const int ProgramWindow = 3;
    private const int Error = 0;
    private const int Success = 2;

    private static readonly Lazy<nint> Dll = new(Load);

    private readonly string _root = Path.Combine(Path.GetTempPath(), "AutoSavePluginTests", Guid.NewGuid().ToString("N"));

    public NativePluginTests()
    {
        FakeIde.Reset();
        FakeIde.PathPref = _root;
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, recursive: true);
    }

    [Theory]
    [InlineData("IdentifyPlugIn")]
    [InlineData("RegisterCallback")]
    [InlineData("About")]
    [InlineData("Configure")]
    [InlineData("CreateMenuItem")]
    [InlineData("OnMenuClick")]
    [InlineData("BeforeExecuteWindow")]
    [InlineData("AfterExecuteWindow")]
    public void ExportsPluginFunction(string name) =>
        Assert.True(NativeLibrary.TryGetExport(GetDll(), name, out _), $"{name} is not exported");

    [Fact]
    public void About_ShowsVersion()
    {
        var about = Marshal.PtrToStringAnsi(((delegate* unmanaged[Cdecl]<nint>)Export("About"))());

        Assert.StartsWith("AutoSavePlugin " + PluginVersion.Current, about);
    }

    [Fact]
    public void CreateMenuItem_UsesRibbonLayoutForNewVersions()
    {
        FakeIde.Version = 1700;
        Assert.Equal("GROUP=AutoSave", MenuItem(Menu.Group));

        FakeIde.Version = 1105;
        Assert.Equal("Tools / -", MenuItem(Menu.Group));
        Assert.Equal("", MenuItem(50));
    }

    [Fact]
    public void SuccessfulCompilation_SavesPackageBeforeAndAfter()
    {
        FakeIde.WindowObject = ("PACKAGE BODY", "HR", "PKG");
        FakeIde.Sources["PACKAGE"] = "create or replace package PKG is\r\nend;\r\n";
        FakeIde.Sources["PACKAGE BODY"] = "create or replace package body PKG is\r\nend;";

        Execute(Success);

        var files = SavedFiles();
        Assert.Equal(2, files.Length);
        Assert.All(files, f => Assert.Equal(
            "create or replace package PKG is\r\nend;\r\n/\r\ncreate or replace package body PKG is\r\nend;",
            File.ReadAllText(f)));

        var timestampFolder = Assert.Single(Directory.GetDirectories(Path.Combine(_root, "host_1521_orcl", "HR", "PACKAGE", "PKG")));
        Assert.True(File.Exists(Path.Combine(timestampFolder, "Before", "PKG.pck")));
        Assert.True(File.Exists(Path.Combine(timestampFolder, "After", "PKG.pck")));
    }

    [Fact]
    public void FailedCompilation_SavesOnlyBefore()
    {
        FakeIde.WindowObject = ("PROCEDURE", "HR", "DO_IT");
        FakeIde.Sources["PROCEDURE"] = "create or replace procedure DO_IT is begin null; end;";

        Execute(Error);

        var file = Assert.Single(SavedFiles());
        Assert.Equal("Before", Path.GetFileName(Path.GetDirectoryName(file)));
        Assert.Equal("DO_IT.prc", Path.GetFileName(file));
    }

    [Fact]
    public void TypeWithoutBody_IsSavedAsSpecOnly()
    {
        FakeIde.WindowObject = ("TYPE", "HR", "T_POINT");
        FakeIde.Sources["TYPE"] = "create or replace type T_POINT as object (x number);";

        Execute(Success);

        Assert.All(SavedFiles(), f =>
        {
            Assert.Equal("T_POINT.typ", Path.GetFileName(f));
            Assert.Equal("create or replace type T_POINT as object (x number);", File.ReadAllText(f));
        });
    }

    [Fact]
    public void FileExtension_ComesFromIdeWhenAvailable()
    {
        FakeIde.Extensions = new() { ["PACKAGE SPEC AND BODY"] = ".pkg" };
        FakeIde.UpdateExtensionCallback();
        FakeIde.WindowObject = ("PACKAGE", "HR", "PKG");
        FakeIde.Sources["PACKAGE"] = "create or replace package PKG is end;";

        Execute(Success);

        Assert.All(SavedFiles(), f => Assert.Equal("PKG.pkg", Path.GetFileName(f)));
    }

    [Fact]
    public void WindowWithoutObject_SavesNothing()
    {
        FakeIde.WindowObject = null;

        Execute(Success);

        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public void ObjectWithoutSource_SavesNothing()
    {
        FakeIde.WindowObject = ("PROCEDURE", "HR", "NEW_PROC");

        Execute(Success);

        Assert.False(Directory.Exists(_root));
    }

    [Fact]
    public void SaveError_IsLoggedAndDoesNotBlockExecution()
    {
        FakeIde.PathPref = Path.Combine(_root, "bad<>path");
        FakeIde.WindowObject = ("PROCEDURE", "HR", "DO_IT");
        FakeIde.Sources["PROCEDURE"] = "create or replace procedure DO_IT is begin null; end;";

        var result = ((delegate* unmanaged[Cdecl]<int, int>)Export("BeforeExecuteWindow"))(ProgramWindow);

        Assert.Equal(1, result);
        Assert.Contains(FakeIde.Log, line => line.StartsWith("AutoSavePlugin: "));
    }

    private void Execute(int result)
    {
        var before = ((delegate* unmanaged[Cdecl]<int, int>)Export("BeforeExecuteWindow"))(ProgramWindow);
        Assert.Equal(1, before);
        ((delegate* unmanaged[Cdecl]<int, int, void>)Export("AfterExecuteWindow"))(ProgramWindow, result);
        Assert.Empty(FakeIde.Log);
    }

    private string[] SavedFiles() =>
        Directory.Exists(_root) ? Directory.GetFiles(_root, "*", SearchOption.AllDirectories) : [];

    private static string MenuItem(int index) =>
        Marshal.PtrToStringAnsi(((delegate* unmanaged[Cdecl]<int, nint>)Export("CreateMenuItem"))(index)) ?? "";

    private static nint Export(string name) => NativeLibrary.GetExport(GetDll(), name);

    private static nint GetDll()
    {
        if (Dll.Value == 0)
            Assert.Skip("Native DLL not found. Run: dotnet publish AutoSavePlugin/AutoSavePlugin.csproj -c Release -r win-x64 -o publish");
        return Dll.Value;
    }

    // Loads the DLL once and plays the IDE start-up sequence: IdentifyPlugIn, then callbacks
    private static nint Load()
    {
        // The test process runs in the bin folder, so relative paths are taken from the repository root
        var root = RepositoryRoot();
        var path = Environment.GetEnvironmentVariable("AUTOSAVE_PLUGIN_DLL");
        if (string.IsNullOrEmpty(path))
        {
            path = root == null ? null : Path.Combine(root, "publish", "AutoSavePlugin.dll");
            if (path == null || !File.Exists(path))
                return 0;
        }
        else if (!Path.IsPathRooted(path) && root != null)
        {
            path = Path.Combine(root, path);
        }

        var dll = NativeLibrary.Load(Path.GetFullPath(path));
        ((delegate* unmanaged[Cdecl]<int, nint>)NativeLibrary.GetExport(dll, "IdentifyPlugIn"))(42);
        FakeIde.Register(dll);
        return dll;
    }

    // Folder with the solution file, found by walking up from the test binaries
    private static string? RepositoryRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "AutoSavePlugin.slnx")))
                return dir.FullName;
        }
        return null;
    }
}
