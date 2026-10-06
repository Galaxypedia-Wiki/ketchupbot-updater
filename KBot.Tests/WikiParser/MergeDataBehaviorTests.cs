using KBot.Framework;

namespace KBot.Tests.WikiParser;

public class MergeDataBehaviorTests
{
    [Fact]
    public void MergeData_PreservesExclusionsSpinalRulesAndUpdateTracking()
    {
        var oldData = new Dictionary<string, string>
        {
            ["damage_res"] = "old", ["title"] = "old title", ["wiki_only"] = "keep",
            ["(f)_spinal"] = "old spinal", ["spinal_3"] = "old extra", ["unchanged"] = "same"
        };
        var newData = new Dictionary<string, string>
        {
            ["damage_res"] = "excluded", ["title"] = "new title", ["spinal_1"] = "first",
            ["spinal_2"] = "second", ["spinal_3"] = "discard", ["spinal_dps"] = "42",
            ["unchanged"] = "same", ["new_key"] = "new", ["flag"] = "NO", ["warp_drive"] = "YES"
        };
        var oldSnapshot = oldData.ToArray();
        var newSnapshot = newData.ToArray();

        var (merged, updated) = KBot.Framework.WikiParser.MergeData(newData, oldData);

        var expected = new Dictionary<string, string>
        {
            ["damage_res"] = "old", ["title"] = "new title", ["wiki_only"] = "keep",
            ["(f)_spinal"] = "first", ["(g)_spinal"] = "second", ["spinal_3"] = "old extra",
            ["spinal_dps"] = "42", ["unchanged"] = "same", ["new_key"] = "new",
            ["flag"] = "NO", ["warp_drive"] = "YES"
        };
        Assert.Equal(expected.OrderBy(pair => pair.Key), merged);
        // Tracking currently uses original API keys, including discarded spinal keys.
        Assert.Equal(new[] { "spinal_1", "spinal_2", "spinal_3", "spinal_dps", "new_key" }, updated);
        Assert.Equal(oldSnapshot, oldData.ToArray());
        Assert.Equal(newSnapshot, newData.ToArray());
    }

    [Fact]
    public void MergeData_SpinalAliasesOverrideExplicitWikiKeys()
    {
        var (merged, updated) = KBot.Framework.WikiParser.MergeData(new()
        {
            ["(f)_spinal"] = "explicit", ["spinal_1"] = "API", ["Spinal_3"] = "case-sensitive"
        }, new());
        Assert.Equal("API", merged["(f)_spinal"]);
        Assert.Equal("case-sensitive", merged["Spinal_3"]);
        Assert.False(merged.ContainsKey("spinal_1"));
        Assert.Equal(new[] { "(f)_spinal", "spinal_1", "Spinal_3" }, updated);
    }

    [Fact]
    public void MergeData_EmptyStringsOverwriteAndExcludedNullsPreserveOldValues()
    {
        var (merged, updated) = KBot.Framework.WikiParser.MergeData(new()
        {
            ["description"] = "", ["damage_res"] = null!
        }, new() { ["description"] = "old", ["damage_res"] = "keep", ["old_null"] = null! });
        Assert.Equal("", merged["description"]);
        Assert.Equal("keep", merged["damage_res"]);
        Assert.Null(merged["old_null"]);
        Assert.Equal(new[] { "description" }, updated);
    }

    [Fact]
    public void MergeData_NonExcludedNullCurrentlyThrows()
    {
        Assert.Throws<NullReferenceException>(() => KBot.Framework.WikiParser.MergeData(
            new() { ["new_key"] = null! }, new()));
    }

    [Fact]
    public void MergeData_EmptyInputPreservesOldDataAndSortsKeys()
    {
        var (merged, updated) = KBot.Framework.WikiParser.MergeData(new(),
            new() { ["z"] = "last", ["a"] = "first" });
        Assert.Equal(new[] { "a", "z" }, merged.Keys);
        Assert.Empty(updated);
    }
}
