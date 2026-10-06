using System.Text.Json;

namespace KBot.Tests.WikiParser;

public class ImageMigrationTests
{
    [Fact]
    public void GalleryConvertsAndRoundTripsWithoutChangingFilenames()
    {
        var original = Framework.WikiParser.ParseInfobox("{{Ship Infobox\n|title = The Deity\n|image = <gallery>\nDeity-icon.webp|Overview\nDeity-front.png|Front\n</gallery>\n|hull = 42\n}}");
        var migrated = Framework.WikiParser.MigrateImages(original);
        Assert.False(migrated.ContainsKey("image"));
        using var images = JsonDocument.Parse(migrated["images"]);
        Assert.Equal("Deity-front.png", images.RootElement[1].GetProperty("src").GetString());
        Assert.Equal("Front", images.RootElement[1].GetProperty("title").GetString());
        Assert.Equal(2, migrated["images"].Split('\n').Count(line => line.StartsWith('{')));
        Assert.True(original.ContainsKey("image"));
        Assert.False(original.ContainsKey("images"));
        Assert.Equal(migrated, Framework.WikiParser.MigrateImages(migrated));
        string wikitext = Framework.WikiParser.ObjectToWikitext(migrated);
        string page = "Before\n" + wikitext + "\nAfter";
        Assert.Equal(wikitext, Framework.WikiParser.ExtractInfobox(page));
        Assert.Equal(migrated, Framework.WikiParser.ParseInfobox(wikitext));
        Assert.Equal("Before\n" + wikitext + "\nAfter", Framework.WikiParser.ReplaceInfobox(page, wikitext));
        Assert.True(Framework.WikiParser.CheckIfInfoboxesChanged(original, migrated));
    }

    [Fact]
    public void JsonPipesAndBracesDoNotSplitOrTruncateInfobox()
    {
        string json = """[{"title":"A | B }} {{ = C","src":"A.webp"}]""";
        string text = "{{Ship Infobox\n|images = " + json + "\n|hull = 42\n}}";
        Assert.Equal(text, Framework.WikiParser.ExtractInfobox(text));
        Assert.Equal(json, Framework.WikiParser.ParseInfobox(text)["images"]);
    }

    [Theory]
    [InlineData("<gallery>\nA.png|A\n</gallery>", "[]")]
    [InlineData("[[File:A.png|thumb|Caption]]", null)]
    [InlineData("<gallery>\nA.png|thumb|Caption\n</gallery>", null)]
    public void AmbiguousFieldsRemainUnchanged(string image, string? images)
    {
        var input = new Dictionary<string, string> { ["image"] = image };
        if (images != null) input["images"] = images;
        Assert.Equal(input, Framework.WikiParser.MigrateImages(input));
    }

    [Fact]
    public void OrbSingleImageConvertsToOverview()
    {
        const string text = "{{Ship Infobox|title1=Orb|shields=800|hull=800|top_speed=349|acceleration=50|turn_speed=0.82|armed=No|cargo_hold=0|damage_res=92%|turret_dps=N/A|force_cost=92,319|description=N/A|creator=teentitansgohomee|version_added=.72f (?)|image=[[File:Orb.webp]]|warp_drive=Yes|vip_required=No}}";
        var original = Framework.WikiParser.ParseInfobox(text);
        var migrated = Framework.WikiParser.MigrateImages(original);
        Assert.False(migrated.ContainsKey("image"));
        using var images = JsonDocument.Parse(migrated["images"]);
        Assert.Equal(1, images.RootElement.GetArrayLength());
        Assert.Equal("Overview", images.RootElement[0].GetProperty("title").GetString());
        Assert.Equal("Orb.webp", images.RootElement[0].GetProperty("src").GetString());
        foreach (var pair in original.Where(pair => pair.Key != "image"))
            Assert.Equal(pair.Value, migrated[pair.Key]);
        Assert.Equal(migrated, Framework.WikiParser.ParseInfobox(Framework.WikiParser.ObjectToWikitext(migrated)));
    }

    [Fact]
    public void ExistingJsonIsPreserved()
    {
        var input = new Dictionary<string, string> { ["images"] = "[]" };
        Assert.Equal(input, Framework.WikiParser.MigrateImages(input));
    }
}
