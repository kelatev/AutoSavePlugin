# PL/SQL Developer AutoSavePlugin
PL/SQL Developer Plugin for autosave sources of program window before and after compilation.

Every time you compile an object in a Program Window, the plug-in saves its source from the database
twice: right before compilation (the version you are about to overwrite) and right after a successful one.
If something goes wrong, any previous version can be restored from these copies.

# Install
1. Download `AutoSavePlugin_64.dll` (or `AutoSavePlugin_32.dll` for 32-bit PL/SQL Developer) from
   [Releases](https://github.com/kelatev/AutoSavePlugin/releases).
2. Copy it to the `PlugIns` folder of PL/SQL Developer, e.g. `C:\Program Files\PLSQL Developer 17\PlugIns`.
3. Restart PL/SQL Developer.
4. Choose the folder for saved sources: Plug-Ins > AutoSave > Settings... (or Configure in the Plug-In
   configuration dialog). If no folder is set, the plug-in asks for one on the first compilation.

# Saved files
```
<folder>\<database>\<owner>\<type>\<name>\<yyyy.mm.dd hh-nn-ss.zzz>\Before\<name>.<ext>
                                                                 \After\<name>.<ext>
```

For example:
```
C:\AutoSavePlugin\ORCL\HR\PACKAGE\EMPLOYEES_API\2026.10.16 09-46-39.534\Before\EMPLOYEES_API.pck
C:\AutoSavePlugin\ORCL\HR\PACKAGE\EMPLOYEES_API\2026.10.16 09-46-39.534\After\EMPLOYEES_API.pck
```

- **Before** is the source stored in the database before compilation. It is saved even if compilation fails.
- **After** is the source in the database after a successful compilation.
- Packages and object types are saved as a whole (spec, `/`, body) whichever part you compile,
  in a format that can be run in SQL*Plus or opened in PL/SQL Developer.
- File extensions follow Preferences > Files > Extensions (`.pck`, `.typ`, `.prc`, `.fnc`, `.trg`, `.jsp` by default).
- Characters not allowed in file names (e.g. in `host:1521/service` connect strings) are replaced with `_`.
- Files are written in the same encoding PL/SQL Developer passes to plug-ins (the Windows ANSI code page).

# Menu
In PL/SQL Developer 12 and later the plug-in adds an **AutoSave** group to the Plug-Ins tab
(older versions: Tools > AutoSave):

| Item | Action |
|---|---|
| Object history | Opens the folder with saved versions of the object in the current Program Window |
| Autosave folder | Opens the root folder with all saved sources |
| Settings... | Chooses the root folder |

# Troubleshooting
- **Nothing is saved.** Check that the folder is set (Settings...) and that you compile from a Program Window;
  SQL, Test and Command windows are not saved. A new object has no Before copy on its first compilation.
- **Errors.** The plug-in never interrupts compilation. If saving fails, the reason is written to the
  PL/SQL Developer debug log with the `AutoSavePlugin:` prefix.
- **Version.** The installed version is shown by the About button in the Plug-In configuration dialog
  and in the DLL file properties.

# Build
The plug-in is written in C# and compiled to a native DLL with .NET NativeAOT.

Requirements: .NET 10 SDK and Visual Studio with the "Desktop development with C++" workload.

```
dotnet publish AutoSavePlugin/AutoSavePlugin.csproj -c Release -r win-x64 -o publish
dotnet test --solution AutoSavePlugin.slnx
```

Besides unit tests, `AutoSavePlugin.Tests` contains integration tests that load `publish\AutoSavePlugin.dll`
with a fake IDE, the same way PL/SQL Developer does. They are skipped until the DLL is published;
another location can be set with the `AUTOSAVE_PLUGIN_DLL` environment variable.

# Release
GitHub Actions builds and tests both DLLs on every push. Pushing a `v*` tag publishes a release;
the version from the tag is shown in About and in the DLL file properties:

```
git tag v2.1.0
git push remote v2.1.0
```
