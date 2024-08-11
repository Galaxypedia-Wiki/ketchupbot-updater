namespace ketchupbot_updater_tests.WikiParser;

public class ReplaceInfoboxTests
{
    [Fact]
    public void ReplaceInfobox_ReplacesInfoboxSuccessfully()
    {
        const string pageText = "Some text before {{Ship Infobox|param1=value1|param2=value2}} some text after";
        const string newInfobox = "{{Ship Infobox|param1=newValue1|param2=newValue2}}";

        string result = ketchupbot_framework.WikiParser.ReplaceInfobox(pageText, newInfobox);

        Assert.Equal("Some text before {{Ship Infobox|param1=newValue1|param2=newValue2}} some text after", result);
    }

    [Fact]
    public void ReplaceInfobox_NoInfoboxFound_ReturnsOriginalText()
    {
        const string pageText = "Some text without infobox";
        const string newInfobox = "{{Ship Infobox|param1=newValue1|param2=newValue2}}";

        string result = ketchupbot_framework.WikiParser.ReplaceInfobox(pageText, newInfobox);

        Assert.Equal(pageText, result);
    }

    [Fact]
    public void ReplaceInfobox_MalformedInfobox_ReturnsOriginalText()
    {
        const string pageText = "Some text before {{Ship Infobox|param1=value1|param2=value2 some text after";
        const string newInfobox = "{{Ship Infobox|param1=newValue1|param2=newValue2}}";

        string result = ketchupbot_framework.WikiParser.ReplaceInfobox(pageText, newInfobox);

        Assert.Equal(pageText, result);
    }
}