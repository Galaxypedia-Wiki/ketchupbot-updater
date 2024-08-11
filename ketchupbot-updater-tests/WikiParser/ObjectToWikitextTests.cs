namespace ketchupbot_updater_tests.WikiParser;

public class ObjectToWikitextTests
{
    [Fact]
    public void ObjectToWikitext_ValidDictionary_ReturnsCorrectWikitext()
    {
        var data = new Dictionary<string, string>
        {
            { "name", "Test Ship" },
            { "type", "Destroyer" }
        };

        string result = ketchupbot_updater.WikiParser.ObjectToWikitext(data);

        Assert.Equal("{{Ship Infobox\n|name = Test Ship\n|type = Destroyer\n}}", result);
    }

    [Fact]
    public void ObjectToWikitext_EmptyDictionary_ReturnsEmptyInfobox()
    {
        var data = new Dictionary<string, string>();

        string result = ketchupbot_updater.WikiParser.ObjectToWikitext(data);

        Assert.Equal("{{Ship Infobox\n}}", result);
    }

    [Fact]
    public void ObjectToWikitext_DictionaryWithSpecialCharacters_EscapesCorrectly()
    {
        var data = new Dictionary<string, string>
        {
            { "name", "Test $hip" },
            { "type", "Destroyer" }
        };

        string result = ketchupbot_updater.WikiParser.ObjectToWikitext(data);

        Assert.Equal("{{Ship Infobox\n|name = Test $$hip\n|type = Destroyer\n}}", result);
    }
}