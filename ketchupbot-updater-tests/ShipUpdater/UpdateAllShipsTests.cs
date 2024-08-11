
namespace ketchupbot_updater_tests.ShipUpdater;

public class UpdateAllShipsTests
{
    // In order to begin writing tests for the UpdateAllShips method, I need to make MwClient available without being logged in. This should allow it to be Mock-able
    /*
    private static readonly Mock<MwClient> MockBot = new();
    private static readonly Mock<ApiManager> MockApiManager = new();
    private readonly ketchupbot_updater.ShipUpdater _shipUpdater = new(MockBot.Object, MockApiManager.Object);

    [Fact]
    public async Task UpdateAllShips_WithValidData_UpdatesAllShips()
    {
        var shipData = new Dictionary<string, Dictionary<string, string>>
        {
            { "Ship1", new Dictionary<string, string> { { "Key1", "Value1" } } },
            { "Ship2", new Dictionary<string, string> { { "Key2", "Value2" } } }
        };

        MockApiManager.Setup(api => api.GetShipsData(true)).ReturnsAsync(shipData);

        await _shipUpdater.UpdateAllShips(shipData);

        MockBot.Verify(bot => bot.EditArticle(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(shipData.Count));
    }

    [Fact]
    public async Task UpdateAllShips_WithNullData_FetchesDataFromApi()
    {
        var shipData = new Dictionary<string, Dictionary<string, string>>
        {
            { "Ship1", new Dictionary<string, string> { { "Key1", "Value1" } } },
            { "Ship2", new Dictionary<string, string> { { "Key2", "Value2" } } }
        };

        MockApiManager.Setup(api => api.GetShipsData(true)).ReturnsAsync(shipData);

        await _shipUpdater.UpdateAllShips();

        MockApiManager.Verify(api => api.GetShipsData(true), Times.Once);
        MockBot.Verify(bot => bot.EditArticle(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(shipData.Count));
    }

    [Fact]
    public async Task UpdateAllShips_WithEmptyData_DoesNotUpdateShips()
    {
        var shipData = new Dictionary<string, Dictionary<string, string>>();

        MockApiManager.Setup(api => api.GetShipsData(true)).ReturnsAsync(shipData);

        await _shipUpdater.UpdateAllShips(shipData);

        MockBot.Verify(bot => bot.EditArticle(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAllShips_WithNullApiData_ThrowsException()
    {
        MockApiManager.Setup(api => api.GetShipsData(true)).ReturnsAsync((Dictionary<string, Dictionary<string, string>>?)null);

        await Assert.ThrowsAsync<ArgumentNullException>(() => _shipUpdater.UpdateAllShips());
    }
*/
}