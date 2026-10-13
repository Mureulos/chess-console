using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using chess_console.Server.Services;

namespace chess_console.Server.Tests.Hubs;

// Sobe o host em memória. Long polling porque o TestServer serve o handler HTTP
// direto, sem precisar de um socket de verdade — o Hub executa igual.
public sealed class ChessServerFixture : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:SignalR:InvocationPermitLimit"] = "1000"
            }));
    }

    public HubConnection CreateConnection()
    {
        string token = Services
            .GetRequiredService<PlayerSessionService>()
            .Create("Test Player")
            .Token;

        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chess", options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
            })
            .Build();
    }
}
