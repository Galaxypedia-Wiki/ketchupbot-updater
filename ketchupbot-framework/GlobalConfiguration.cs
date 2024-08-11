namespace ketchupbot_framework;

public static class GlobalConfiguration
{
    /// <summary>
    /// For ships with mismatching names: Map the ship name in-game to the page name on the Galaxypedia
    /// </summary>
    public static Dictionary<string, string> ShipNameMap { get; } = new()
    {
        { "2018", "2018 Ship" },
        { "yname", "Yname (ship)" }
    };

    /// <summary>
    /// Exclude certain parameters from being managed by KetchupBot
    /// </summary>
    public static List<string> ParameterExclusions { get; } =
    [
        "damage_res",
        "loyalty",
        "version_added"
    ];

    /// <summary>
    /// Parameters that should NOT be deleted if their value is "no", which is usually done automatically for every parameter
    /// </summary>
    public static List<string> ParametersToNotDeleteIfValueIsNo { get; } =
        [];

    /// <summary>
    /// Parameters that should be deleted if their value is "yes", which would typically be left alone otherwise.
    /// </summary>
    public static List<string> ParametersToDeleteIfValueIsYes { get; } =
    [
        "warp_drive"
    ];
}