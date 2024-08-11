using System.Text.RegularExpressions;

namespace ketchupbot_updater_tests.WikiParser;

public class ExtractTurretTablesTests
{
    [Fact]
    public void ExtractsTurretTables_ReturnsMatchCollection_WhenTurretTablesExist()
    {
        const string text = "{| class=\"wikitable sortable\" ... |}";

        MatchCollection result = ketchupbot_updater.WikiParser.ExtractTurretTables(text);

        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public void ExtractsTurretTables_ThrowsInvalidOperationException_WhenNoTurretTablesFound()
    {
        const string text = "No turret tables here";

        Assert.Throws<InvalidOperationException>(() => ketchupbot_updater.WikiParser.ExtractTurretTables(text));
    }

    [Fact]
    public void ExtractsTurretTables_ThrowsInvalidOperationException_WhenMoreThanSixTurretTablesFound()
    {
        string text = string.Concat(Enumerable.Repeat("{| class=\"wikitable sortable\" ... |}", 7));

        Assert.Throws<InvalidOperationException>(() => ketchupbot_updater.WikiParser.ExtractTurretTables(text));
    }
}