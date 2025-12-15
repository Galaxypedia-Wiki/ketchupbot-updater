using System.Text.RegularExpressions;

namespace ketchupbot_updater_tests.WikiParser;

public class ExtractTurretTablesTests
{
    [Fact]
    public void ExtractsTurretTables_ReturnsMatchCollection_WhenTurretTablesExist()
    {
        const string text = "{| class=\"wikitable sortable\" ... |}";

        MatchCollection result = ketchupbot_framework.WikiParser.ExtractTurretTables(text);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public void ExtractsTurretTables_ThrowsInvalidOperationException_WhenNoTurretTablesFound()
    {
        const string text = "No turret tables here";

        Assert.Throws<InvalidOperationException>(() => ketchupbot_framework.WikiParser.ExtractTurretTables(text));
    }

    [Fact]
    public void ExtractsTurretTables_ThrowsInvalidOperationException_WhenMoreThanSevenTurretTablesFound()
    {
        string text = string.Concat(Enumerable.Repeat("{| class=\"wikitable sortable\" ... |}", 8));

        Assert.Throws<InvalidOperationException>(() => ketchupbot_framework.WikiParser.ExtractTurretTables(text));
    }
}