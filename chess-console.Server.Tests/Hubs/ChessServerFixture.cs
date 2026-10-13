using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

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

    public HubConnection CreateConnection() =>
        new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chess", options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            })
            .Build();
}
