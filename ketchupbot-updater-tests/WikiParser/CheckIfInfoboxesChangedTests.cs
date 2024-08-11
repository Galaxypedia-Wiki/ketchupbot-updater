namespace ketchupbot_updater_tests.WikiParser;

public class CheckIfInfoboxesChangedTests
{
    [Fact]
    public void CheckIfInfoboxesChanged_ReturnsTrue_WhenDictionariesAreDifferent()
    {
        var oldData = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };
        var newData = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "differentValue" } };

        bool result = ketchupbot_updater.WikiParser.CheckIfInfoboxesChanged(oldData, newData);

        Assert.True(result);
    }

    [Fact]
    public void CheckIfInfoboxesChanged_ReturnsFalse_WhenDictionariesAreIdentical()
    {
        var oldData = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };
        var newData = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

        bool result = ketchupbot_updater.WikiParser.CheckIfInfoboxesChanged(oldData, newData);

        Assert.False(result);
    }

    [Fact]
    public void CheckIfInfoboxesChanged_ReturnsTrue_WhenNewDictionaryHasAdditionalKeys()
    {
        var oldData = new Dictionary<string, string> { { "key1", "value1" } };
        var newData = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };

        bool result = ketchupbot_updater.WikiParser.CheckIfInfoboxesChanged(oldData, newData);

        Assert.True(result);
    }

    [Fact]
    public void CheckIfInfoboxesChanged_ReturnsTrue_WhenOldDictionaryHasAdditionalKeys()
    {
        var oldData = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } };
        var newData = new Dictionary<string, string> { { "key1", "value1" } };

        bool result = ketchupbot_updater.WikiParser.CheckIfInfoboxesChanged(oldData, newData);

        Assert.True(result);
    }

    [Fact]
    public void CheckIfInfoboxesChanged_ReturnsFalse_WhenBothDictionariesAreEmpty()
    {
        var oldData = new Dictionary<string, string>();
        var newData = new Dictionary<string, string>();

        bool result = ketchupbot_updater.WikiParser.CheckIfInfoboxesChanged(oldData, newData);

        Assert.False(result);
    }
}