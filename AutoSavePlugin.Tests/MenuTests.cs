namespace AutoSavePlugin.Tests;

public class MenuTests
{
    [Theory]
    [InlineData(Menu.Group, "GROUP=AutoSave")]
    [InlineData(Menu.ObjectHistory, "ITEM=Object history")]
    [InlineData(Menu.RootFolder, "ITEM=Autosave folder")]
    [InlineData(Menu.Settings, "ITEM=Settings...")]
    [InlineData(1, "")]
    [InlineData(99, "")]
    public void Ribbon_FromVersion12(int index, string expected) =>
        Assert.Equal(expected, Menu.Item(index, 1200));

    [Theory]
    [InlineData(Menu.Group, "Tools / -")]
    [InlineData(Menu.ObjectHistory, "Tools / AutoSave / Object &history")]
    [InlineData(Menu.Settings, "Tools / AutoSave / &Settings...")]
    [InlineData(1, "")]
    public void ClassicMenu_BeforeVersion12(int index, string expected) =>
        Assert.Equal(expected, Menu.Item(index, 1105));
}
