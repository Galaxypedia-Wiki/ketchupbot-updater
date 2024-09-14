namespace ketchupbot_updater_tests.WikiParser;

public class MergeDataTests
{
    private static readonly Dictionary<string, string> SampleDeityPage = new()
    {
        { "acceleration", "12" },
        { "cargo_hold", "4,000" },
        { "creator", "Envoy001 ''(Old)''\n\nStarforce6000 ''(New)''" },
        { "credit", "$56,019" },
        { "damage_res", "65%" },
        {
            "description",
            "The Deity is a maneuverable supercapital warship ship designed by Event Horizon Shipyards and originally sold as a luxury transport vessel as a means to evade permit restrictions, although this was quickly stopped by UNE regulations. Featuring unique thrust and weapon systems, the Deity brings both speed and powerful turreted weaponry to any battlefield."
        },
        { "explosion_radius", "1,100" },
        { "huge_turrets", "4 Olympian Laser" },
        { "hull", "37,500" },
        {
            "image",
            "<gallery>\nDeity-icon.webp|Overview\nDeity-front.png|Front\nDeity-back.png|Back\nDeity-side.png|Side\nDeity-top.png|Top\nDeity-bottom.png|Bottom\nDeity-interior.png|Interior\nDeity-interior2.png|Interior 2\nDeity-interior3.png|Interior 3\nDeity-trails.png|Trails\nDeity-warp.png|Warp\n</gallery>"
        },
        { "large_turrets", "6 Olympian Cannon" },
        { "loyalty_required", "11% + Level 3 Starbase" },
        { "m_class_range", "6,500" },
        { "permit", "1 SC Build Permit" },
        { "r_class_range", "6,500" },
        { "shields", "37,500" },
        { "title", "The Deity" },
        { "top_speed", "60" },
        { "turn_speed", "0.20" },
        { "turret_dps", "328" },
        { "version_added", ".65b" },
        { "warehouse", "28" }
    };

    private static readonly Dictionary<string, string> SampleApiResponse = new()
    {
        { "title", SampleDeityPage["title"] },
        { "shields", SampleDeityPage["shields"] },
        { "hull", SampleDeityPage["hull"] },
        { "top_speed", SampleDeityPage["top_speed"] },
        { "acceleration", SampleDeityPage["acceleration"] },
        { "turn_speed", SampleDeityPage["turn_speed"] },
        { "large_turrets", SampleDeityPage["large_turrets"] },
        { "huge_turrets", SampleDeityPage["huge_turrets"] },
        { "m_class_range", SampleDeityPage["m_class_range"] },
        { "r_class_range", SampleDeityPage["r_class_range"] },
        { "cargo_hold", SampleDeityPage["cargo_hold"] },
        { "damage_res", SampleDeityPage["damage_res"] },
        { "turret_dps", SampleDeityPage["turret_dps"] },
        { "description", SampleDeityPage["description"] },
        { "explosion_radius", SampleDeityPage["explosion_radius"] }
    };

    [Fact]
    public void MergeData_UpdatesExistingValues()
    {
        Dictionary<string, string> newData = new(SampleApiResponse)
        {
            ["description"] = "Merge test"
        };

        Tuple<Dictionary<string, string>, List<string>> mergedData =
            ketchupbot_framework.WikiParser.MergeData(newData, SampleDeityPage);

        foreach (KeyValuePair<string, string> kvp in mergedData.Item1)
            Assert.Equal(kvp.Key == "description" ? "Merge test" : SampleDeityPage[kvp.Key], kvp.Value);
    }

    [Fact]
    public void MergeData_ChangesNothing()
    {
        Tuple<Dictionary<string, string>, List<string>> mergedData =
            ketchupbot_framework.WikiParser.MergeData(SampleApiResponse, SampleDeityPage);

        Assert.Equal(SampleDeityPage, mergedData.Item1);
    }

    [Fact]
    public void MergeData_AddsNewValues()
    {
        Dictionary<string, string> newData = new(SampleApiResponse)
        {
            ["new_key"] = "new_value"
        };

        Tuple<Dictionary<string, string>, List<string>> mergedData =
            ketchupbot_framework.WikiParser.MergeData(newData, SampleDeityPage);

        foreach (KeyValuePair<string, string> kvp in mergedData.Item1)
            Assert.Equal(kvp.Key == "new_key" ? "new_value" : SampleDeityPage[kvp.Key], kvp.Value);
    }

    [Fact]
    public void MergeData_RemovingValueFromApiResponseDoesntRemoveFromPageData()
    {
        Dictionary<string, string> newData = new(SampleApiResponse);
        newData.Remove("description");

        Tuple<Dictionary<string, string>, List<string>> mergedData =
            ketchupbot_framework.WikiParser.MergeData(newData, SampleDeityPage);

        foreach (KeyValuePair<string, string> kvp in mergedData.Item1)
            Assert.Equal(SampleDeityPage[kvp.Key], kvp.Value);
    }
}