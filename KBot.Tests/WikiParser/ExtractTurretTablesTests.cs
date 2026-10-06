using System.Text.RegularExpressions;

namespace KBot.Tests.WikiParser;

public class ExtractTurretTablesTests
{
    [Fact]
    public void ExtractsTurretTables_ReturnsMatchCollection_WhenTurretTablesExist()
    {
        const string text = "{| class=\"wikitable sortable\" ... |}";

        MatchCollection result = KBot.Framework.WikiParser.ExtractTurretTables(text);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public void ExtractsTurretTables_ThrowsInvalidOperationException_WhenNoTurretTablesFound()
    {
        const string text = "No turret tables here";

        Assert.Throws<InvalidOperationException>(() => KBot.Framework.WikiParser.ExtractTurretTables(text));
    }

    [Fact]
    public void ExtractsTurretTables_ThrowsInvalidOperationException_WhenMoreThanSixTurretTablesFound()
    {
        string text = string.Concat(Enumerable.Repeat("{| class=\"wikitable sortable\" ... |}", 7));

        Assert.Throws<InvalidOperationException>(() => KBot.Framework.WikiParser.ExtractTurretTables(text));
    }
}