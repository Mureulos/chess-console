using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace chess_console.Server.Tests.Hubs;

public sealed class RateLimitServerFixture : WebApplicationFactory<Program>
{
}

public class RateLimitingTests : IClassFixture<RateLimitServerFixture>
{
    private readonly RateLimitServerFixture _server;

    public RateLimitingTests(RateLimitServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task SignalRNegotiationReturnsTooManyRequestsAfterTheClientLimit()
    {
        using HttpClient client = _server.CreateClient();
        HttpResponseMessage? rejectedResponse = null;

        for (int requestNumber = 0; requestNumber <= 30; requestNumber++)
        {
            HttpResponseMessage response = await client.PostAsync(
                "/hubs/chess/negotiate?negotiateVersion=1",
                content: null);

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                rejectedResponse = response;
                break;
            }

            response.Dispose();
        }

        Assert.NotNull(rejectedResponse);
        Assert.Equal(HttpStatusCode.TooManyRequests, rejectedResponse.StatusCode);
        rejectedResponse.Dispose();
    }
}
