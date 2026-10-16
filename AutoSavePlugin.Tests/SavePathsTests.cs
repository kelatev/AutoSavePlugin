using System.Text;

namespace AutoSavePlugin.Tests;

public class SavePathsTests
{
    [Theory]
    [InlineData("ORCL", "ORCL")]
    [InlineData("host:1521/service", "host_1521_service")]
    [InlineData("  name  ", "name")]
    [InlineData("a*b?c\"d<e>f|g\\h", "a_b_c_d_e_f_g_h")]
    [InlineData("tab\tname", "tab_name")]
    [InlineData("dots...", "dots")]
    [InlineData("", "_")]
    [InlineData(null, "_")]
    public void SafeName_ReplacesCharactersNotAllowedInPaths(string? value, string expected) =>
        Assert.Equal(expected, SavePaths.SafeName(value));

    [Fact]
    public void Timestamp_IsSortableWithPaddedMilliseconds() =>
        Assert.Equal("2026.01.02 03-04-05.007",
                     SavePaths.Timestamp(new DateTime(2026, 1, 2, 3, 4, 5, 7)));

    [Theory]
    [InlineData("PACKAGE", "PACKAGE", "PACKAGE BODY")]
    [InlineData("PACKAGE BODY", "PACKAGE", "PACKAGE BODY")]
    [InlineData("TYPE", "TYPE", "TYPE BODY")]
    [InlineData("TYPE BODY", "TYPE", "TYPE BODY")]
    public void SpecAndBody_ForPackagesAndTypes(string type, string spec, string body) =>
        Assert.Equal((spec, body), SavePaths.SpecAndBody(type));

    [Theory]
    [InlineData("PROCEDURE")]
    [InlineData("FUNCTION")]
    [InlineData("TRIGGER")]
    [InlineData("JAVA SOURCE")]
    public void SpecAndBody_IsNullForSingleUnitObjects(string type) =>
        Assert.Null(SavePaths.SpecAndBody(type));

    [Theory]
    [InlineData("PACKAGE BODY", "PACKAGE", "PACKAGE SPEC AND BODY")]
    [InlineData("TYPE BODY", "TYPE", "TYPE SPEC AND BODY")]
    [InlineData("PROCEDURE", "PROCEDURE", "PROCEDURE")]
    public void FolderTypeAndExtensionType(string type, string folderType, string extensionType)
    {
        Assert.Equal(folderType, SavePaths.FolderType(type));
        Assert.Equal(extensionType, SavePaths.ExtensionType(type));
    }

    [Theory]
    [InlineData("PACKAGE", "pck")]
    [InlineData("PACKAGE BODY", "pck")]
    [InlineData("TYPE BODY", "typ")]
    [InlineData("FUNCTION", "fnc")]
    [InlineData("PROCEDURE", "prc")]
    [InlineData("TRIGGER", "trg")]
    [InlineData("JAVA SOURCE", "jsp")]
    [InlineData("VIEW", "sql")]
    public void DefaultExtension_MatchesPlsqlDeveloperDefaults(string type, string extension) =>
        Assert.Equal(extension, SavePaths.DefaultExtension(type));

    [Fact]
    public void ObjectFolderAndFilePath()
    {
        var folder = SavePaths.ObjectFolder(@"C:\Saves\", "db:1521/orcl", "HR", "PACKAGE BODY", "PKG");
        var file = SavePaths.FilePath(folder, "PKG", ".pck", "2026.01.02 03-04-05.007", before: true);

        Assert.Equal(@"C:\Saves\db_1521_orcl\HR\PACKAGE\PKG", folder);
        Assert.Equal(@"C:\Saves\db_1521_orcl\HR\PACKAGE\PKG\2026.01.02 03-04-05.007\Before\PKG.pck", file);
    }

    [Fact]
    public void FilePath_After() =>
        Assert.Equal(@"C:\Saves\ORCL\HR\PROCEDURE\DO_IT\ts\After\DO_IT.prc",
                     SavePaths.FilePath(@"C:\Saves\ORCL\HR\PROCEDURE\DO_IT", "DO_IT", "prc", "ts", before: false));

    [Fact]
    public void JoinSpecAndBody_PutsSlashOnItsOwnLine()
    {
        var result = SavePaths.JoinSpecAndBody("spec end;  \r\n\r\n"u8, "body end;"u8);

        Assert.Equal("spec end;\r\n/\r\nbody end;", Encoding.ASCII.GetString(result));
    }

    [Fact]
    public void JoinSpecAndBody_WithoutBody_KeepsSpecAsIs()
    {
        var result = SavePaths.JoinSpecAndBody("type spec;\r\n"u8, ""u8);

        Assert.Equal("type spec;\r\n", Encoding.ASCII.GetString(result));
    }
}
