using System.Text.Json;
using Govor.Mobile.Utilities;
public class HubContractTests
{
    [Test]
    public void ServerResultIsDeserializedAsCommandAcknowledgement()
    {
        var result = JsonSerializer.Deserialize<HubResult<string>>("{\"status\":200,\"result\":\"saved\"}", new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.That(result!.Status, Is.EqualTo(HubResultStatus.Success));
        Assert.That(result.Value, Is.EqualTo("saved"));
    }
}
