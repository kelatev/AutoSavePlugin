# PL/SQL Developer AutoSavePlugin
PL/SQL Developer Plugin for autosave sources of program window before and after compilation.

Sources are saved to `<path>\<database>\<owner>\<type>\<name>\<yyyy.mm.dd hh-nn-ss.zzz>\Before|After\<name>.sql`
(packages: spec and body together in `<name>.pck`). The path is set with the Configure button in the
Plug-In configuration dialog.

# Install
Download `AutoSavePlugin_64.dll` (or `AutoSavePlugin_32.dll` for 32-bit PL/SQL Developer) from
[Releases](https://github.com/kelatev/AutoSavePlugin/releases) and copy it to the `PlugIns` folder of PL/SQL Developer.

# Build
The plug-in is written in C# and compiled to a native DLL with .NET NativeAOT.

Requirements: .NET 10 SDK and Visual Studio with the "Desktop development with C++" workload.

```
dotnet test AutoSavePlugin.slnx
dotnet publish AutoSavePlugin/AutoSavePlugin.csproj -c Release -r win-x64 -o publish
```

GitHub Actions builds both DLLs on every push; pushing a `v*` tag creates a release with them.
