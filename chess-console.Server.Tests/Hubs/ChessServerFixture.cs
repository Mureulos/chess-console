using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;

namespace chess_console.Server.Tests.Hubs;

// Sobe o host em memória. Long polling porque o TestServer serve o handler HTTP
// direto, sem precisar de um socket de verdade — o Hub executa igual.
public sealed class ChessServerFixture : WebApplicationFactory<Program>
{
    public HubConnection CreateConnection() =>
        new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/chess", options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => Server.CreateHandler();
            })
            .Build();
}
