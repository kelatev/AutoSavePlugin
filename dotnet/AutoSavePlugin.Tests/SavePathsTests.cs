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

    [Fact]
    public void FilePath_ForProcedure()
    {
        var path = SavePaths.FilePath(@"C:\Saves\", "db:1521/orcl", "HR", "PROCEDURE", "DO_IT",
                                      "2026.01.02 03-04-05.007", before: true);

        Assert.Equal(@"C:\Saves\db_1521_orcl\HR\PROCEDURE\DO_IT\2026.01.02 03-04-05.007\Before\DO_IT.sql", path);
    }

    [Theory]
    [InlineData("PACKAGE")]
    [InlineData("PACKAGE BODY")]
    public void FilePath_ForPackage_UsesPackageFolderAndPckExtension(string type)
    {
        var path = SavePaths.FilePath(@"C:\Saves", "ORCL", "HR", type, "PKG", "ts", before: false);

        Assert.Equal(@"C:\Saves\ORCL\HR\PACKAGE\PKG\ts\After\PKG.pck", path);
    }

    [Fact]
    public void JoinPackage_PutsSlashOnItsOwnLine()
    {
        var result = SavePaths.JoinPackage("spec end;  \r\n\r\n"u8, "body end;"u8);

        Assert.Equal("spec end;\r\n/\r\nbody end;", Encoding.ASCII.GetString(result));
    }
}
