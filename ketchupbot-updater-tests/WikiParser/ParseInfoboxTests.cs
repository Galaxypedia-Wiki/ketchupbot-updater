namespace ketchupbot_updater_tests.WikiParser;

public class ParseInfoboxTests
{

    [Fact]
    public void ParseInfobox_ValidInfobox_ReturnsDictionary()
    {
        const string infobox = "{{Ship Infobox|name=Test Ship|type=Destroyer|image=<gallery>image1.jpg</gallery>}}";
        Dictionary<string, string> result = ketchupbot_updater.WikiParser.ParseInfobox(infobox);
        Assert.Equal(3, result.Count);
        Assert.Equal("Test Ship", result["name"]);
        Assert.Equal("Destroyer", result["type"]);
        Assert.Equal("<gallery>image1.jpg</gallery>", result["image"]);
    }

    [Fact]
    public void ParseInfobox_InfoboxWithoutImage_ReturnsDictionary()
    {
        const string infobox = "{{Ship Infobox|name=Test Ship|type=Destroyer}}";
        Dictionary<string, string> result = ketchupbot_updater.WikiParser.ParseInfobox(infobox);
        Assert.Equal(2, result.Count);
        Assert.Equal("Test Ship", result["name"]);
        Assert.Equal("Destroyer", result["type"]);
    }

    [Fact]
    public void ParseInfobox_InfoboxWithMalformedGallery_ThrowsException()
    {
        const string infobox = "{{Ship Infobox|name=Test Ship|type=Destroyer|image=<gallery>image1.jpg}}";
        Assert.Throws<InvalidOperationException>(() => ketchupbot_updater.WikiParser.ParseInfobox(infobox));
    }

    [Fact]
    public void ParseInfobox_EmptyInfobox_ReturnsEmptyDictionary()
    {
        const string infobox = "{{Ship Infobox}}";
        Dictionary<string, string> result = ketchupbot_updater.WikiParser.ParseInfobox(infobox);
        Assert.Empty(result);
    }

    [Fact]
    public void ParseInfobox_InfoboxWithNoEqualsSign_ReturnsEmptyDictionary()
    {
        const string infobox = "{{Ship Infobox|name|type}}";
        Dictionary<string, string> result = ketchupbot_updater.WikiParser.ParseInfobox(infobox);
        Assert.Empty(result);
    }
}