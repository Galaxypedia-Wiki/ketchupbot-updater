
namespace ketchupbot_updater_tests.ShipUpdater;

public class UpdateAllShipsTests
{
    // Honestly these tests are a mess. I'm not going to fix them now, but I'll get to it later.
    /*
    private static readonly MwClient MwClient = new();

    // Use the publicly available API for testing
    private static readonly ApiManager ApiManager = new("https://api.info.galaxy.casa");

    private readonly ketchupbot_updater.ShipUpdater _shipUpdater = new(MwClient, ApiManager);

    [Fact]
    public async Task UpdateAllShips_WithValidData_UpdatesAllShips()
    {
        var shipData = new Dictionary<string, Dictionary<string, string>>
        {
            { "Ship1", new Dictionary<string, string> { { "Key1", "Value1" } } },
            { "Ship2", new Dictionary<string, string> { { "Key2", "Value2" } } }
        };

        await _shipUpdater.UpdateAllShips(shipData);
    }

    [Fact]
    public async Task UpdateAllShips_WithNullData_FetchesDataFromApi()
    {
        var shipData = new Dictionary<string, Dictionary<string, string>>
        {
            { "Ship1", new Dictionary<string, string> { { "Key1", "Value1" } } },
            { "Ship2", new Dictionary<string, string> { { "Key2", "Value2" } } }
        };

        await _shipUpdater.UpdateAllShips();

        ApiManager.Verify(api => api.GetShipsData(true), Times.Once);
        MwClient.Verify(bot => bot.EditArticle(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(shipData.Count));
    }

    [Fact]
    public async Task UpdateAllShips_WithEmptyData_DoesNotUpdateShips()
    {
        var shipData = new Dictionary<string, Dictionary<string, string>>();

        ApiManager.Setup(api => api.GetShipsData(true)).ReturnsAsync(shipData);

        await _shipUpdater.UpdateAllShips(shipData);

        MwClient.Verify(bot => bot.EditArticle(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAllShips_WithNullApiData_ThrowsException()
    {
        ApiManager.Setup(api => api.GetShipsData(true)).ReturnsAsync((Dictionary<string, Dictionary<string, string>>?)null);

        await Assert.ThrowsAsync<ArgumentNullException>(() => _shipUpdater.UpdateAllShips());
    }
*/
}