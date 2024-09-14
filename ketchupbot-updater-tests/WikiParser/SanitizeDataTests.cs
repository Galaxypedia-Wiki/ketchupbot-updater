using ketchupbot_framework;

namespace ketchupbot_updater_tests.WikiParser;

public class SanitizeDataTests
{
    // Don't test if SanitizeData actually sanitizes data, since it can change at any time. It's better to test the individual sanitization methods.

    [Fact]
    public void SanitizeData_RemovesTitle1Parameter()
    {
        var data = new Dictionary<string, string> { { "title1", "Some Title" }, { "description", "Some Description" } };
        var oldData = new Dictionary<string, string>();

        Tuple<Dictionary<string, string>, List<string>> result =
            ketchupbot_framework.WikiParser.SanitizeData(data, oldData);

        Assert.DoesNotContain("title1", result.Item1.Keys);
    }

    [Fact]
    public void SanitizeData_AddsThePrefixToTitle()
    {
        var data = new Dictionary<string, string> { { "title", "Some Title" } };
        var oldData = new Dictionary<string, string>();

        Tuple<Dictionary<string, string>, List<string>> result =
            ketchupbot_framework.WikiParser.SanitizeData(data, oldData);

        Assert.Equal("The Some Title", result.Item1["title"]);
    }

    [Fact]
    public void SanitizeData_ReplacesNewlinesInDescription()
    {
        var data = new Dictionary<string, string> { { "description", "Line1\nLine2" } };
        var oldData = new Dictionary<string, string>();

        Tuple<Dictionary<string, string>, List<string>> result =
            ketchupbot_framework.WikiParser.SanitizeData(data, oldData);

        Assert.Equal("Line1 Line2", result.Item1["description"]);
    }

    [Fact]
    public void SanitizeData_ConvertsDoubleWithZeroDecimalToInteger()
    {
        var data = new Dictionary<string, string> { { "someDouble", "1.0" } };
        var oldData = new Dictionary<string, string>();

        Tuple<Dictionary<string, string>, List<string>> result =
            ketchupbot_framework.WikiParser.SanitizeData(data, oldData);

        Assert.Equal("1", result.Item1["someDouble"]);
    }

    [Fact]
    public void SanitizeData_ConvertsDoubleWithNonZeroDecimalToTwoDecimalPlaces()
    {
        var data = new Dictionary<string, string> { { "someDouble", "0.2" } };
        var oldData = new Dictionary<string, string>();

        Tuple<Dictionary<string, string>, List<string>> result =
            ketchupbot_framework.WikiParser.SanitizeData(data, oldData);

        Assert.Equal("0.20", result.Item1["someDouble"]);
    }

    [Fact]
    public void SanitizeData_AddsCommasToIntegerValues()
    {
        var data = new Dictionary<string, string> { { "someNumber", "1000" } };
        var oldData = new Dictionary<string, string>();

        Tuple<Dictionary<string, string>, List<string>> result =
            ketchupbot_framework.WikiParser.SanitizeData(data, oldData);

        Assert.Equal("1,000", result.Item1["someNumber"]);
    }

    [Fact]
    public void SanitizeData_RemovesParametersWithNoValue()
    {
        var data = new Dictionary<string, string> { { "someKey", "no" } };
        var oldData = new Dictionary<string, string> { { "someKey", "no" } };

        Tuple<Dictionary<string, string>, List<string>> result =
            ketchupbot_framework.WikiParser.SanitizeData(data, oldData);

        Assert.DoesNotContain("someKey", result.Item1.Keys);
    }

    [Fact]
    public void SanitizeData_RemovesParametersWithYesValue()
    {
        var data = new Dictionary<string, string> { { "someKey", "yes" } };
        var oldData = new Dictionary<string, string> { { "someKey", "yes" } };

        GlobalConfiguration.ParametersToDeleteIfValueIsYes.Add("someKey");

        Tuple<Dictionary<string, string>, List<string>> result =
            ketchupbot_framework.WikiParser.SanitizeData(data, oldData);

        GlobalConfiguration.ParametersToDeleteIfValueIsYes.Remove("someKey");

        Assert.DoesNotContain("someKey", result.Item1.Keys);
    }
}