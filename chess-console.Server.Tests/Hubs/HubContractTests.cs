using System.Net;
using System.Text;
using chess;
using chess_console.Core.Dto;
using chess_console.Server.Hubs;
using Microsoft.AspNetCore.SignalR.Protocol;
using Microsoft.Extensions.DependencyInjection;

namespace chess_console.Server.Tests.Hubs;

// O front em wwwroot/app.js é JavaScript e não compila junto: estes testes seguram o
// contrato que ele assume, que o compilador não tem como verificar.
public class HubContractTests : IClassFixture<ChessServerFixture>
{
    private readonly ChessServerFixture _server;

    public HubContractTests(ChessServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public void TheBoardStatePayloadGoesOutInCamelCase()
    {
        IHubProtocol protocol = _server.Services.GetRequiredService<IHubProtocol>();
        BoardStateDto state = BoardMapper.ToDto(new ChessMatch());

        string json = Encoding.UTF8.GetString(
            protocol.GetMessageBytes(new InvocationMessage(ChessHub.BoardStateEvent, [state])).ToArray());

        Assert.Contains("\"currentPlayer\"", json);
        Assert.Contains("\"capturedWhitePieces\"", json);
        Assert.Contains("\"pieces\"", json);
        Assert.Contains("\"position\"", json);
        Assert.DoesNotContain("\"CurrentPlayer\"", json);
    }

    [Theory]
    [InlineData("/", "text/html")]
    [InlineData("/app.js", "text/javascript")]
    [InlineData("/styles.css", "text/css")]
    public async Task TheSameProcessServesTheFrontEnd(string path, string contentType)
    {
        using HttpClient client = _server.CreateClient();
        using HttpResponseMessage response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(contentType, response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task TheIndexPageLoadsTheSignalRClientAndTheApp()
    {
        using HttpClient client = _server.CreateClient();

        string html = await client.GetStringAsync("/");

        Assert.Contains("@microsoft/signalr", html);
        Assert.Contains("app.js", html);
    }
}
