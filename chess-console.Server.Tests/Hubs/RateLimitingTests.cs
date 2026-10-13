using System.Net;
using chess_console.Server.Hubs;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using chess_console.Server.Services;

namespace chess_console.Server.Tests.Hubs;

public sealed class RateLimitServerFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:SignalR:InvocationPermitLimit"] = "1",
                ["RateLimiting:SignalR:InvocationWindowSeconds"] = "60"
            }));
    }
}

public sealed class NegotiationRateLimitServerFixture : WebApplicationFactory<Program>
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
    public async Task HubInvocationsReturnAnErrorAfterTheClientLimit()
    {
        string token = _server.Services
            .GetRequiredService<PlayerSessionService>()
            .Create("Rate Limit Test")
            .Token;

        await using HubConnection connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chess", options =>
            {
                options.HttpMessageHandlerFactory = _ => _server.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();

        await connection.StartAsync();
        await connection.InvokeAsync<Guid[]>(nameof(ChessHub.GetWaitingMatches));

        await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync<Guid[]>(nameof(ChessHub.GetWaitingMatches)));
    }

    [Fact]
    public async Task HubInvocationsWithoutAPlayerTokenAreRejected()
    {
        await using HubConnection connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chess", options =>
            {
                options.HttpMessageHandlerFactory = _ => _server.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync();

        HubException exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync<Guid[]>(nameof(ChessHub.GetWaitingMatches)));

        Assert.Contains("session is missing or expired", exception.Message);
    }
}

public class NegotiationRateLimitingTests : IClassFixture<NegotiationRateLimitServerFixture>
{
    private readonly NegotiationRateLimitServerFixture _server;

    public NegotiationRateLimitingTests(NegotiationRateLimitServerFixture server)
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
