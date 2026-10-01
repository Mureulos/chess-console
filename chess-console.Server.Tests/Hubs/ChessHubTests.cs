using chess_console.Core.Dto;
using chess_console.Server.Hubs;
using Microsoft.AspNetCore.SignalR.Client;

namespace chess_console.Server.Tests.Hubs;

public class ChessHubTests : IClassFixture<ChessServerFixture>
{
    private static readonly TimeSpan EventTimeout = TimeSpan.FromSeconds(15);

    private readonly ChessServerFixture _server;

    public ChessHubTests(ChessServerFixture server)
    {
        _server = server;
    }

    [Fact]
    public async Task CreateAndJoin_SeatBothPlayersAndWarnTheFirstOne()
    {
        await using HubConnection white = _server.CreateConnection();
        await using HubConnection black = _server.CreateConnection();

        Task<string> opponentJoined = NextEventAsync<string>(white, ChessHub.OpponentJoinedEvent);

        await white.StartAsync();
        await black.StartAsync();

        MatchJoinedDto created = await InvokeAsync<MatchJoinedDto>(white, nameof(ChessHub.CreateMatch));
        MatchJoinedDto joined = await InvokeAsync<MatchJoinedDto>(black, nameof(ChessHub.JoinMatch), created.MatchId);

        Assert.Equal("White", created.Color);
        Assert.Equal("Black", joined.Color);
        Assert.Equal(created.MatchId, joined.MatchId);
        Assert.Equal("Black", await opponentJoined);
    }

    [Fact]
    public async Task MakeMove_BroadcastsTheNewStateToBothPlayers()
    {
        await using HubConnection white = _server.CreateConnection();
        await using HubConnection black = _server.CreateConnection();

        Task<MoveResultDto> seenByWhite = NextEventAsync<MoveResultDto>(white, ChessHub.BoardStateEvent);
        Task<MoveResultDto> seenByBlack = NextEventAsync<MoveResultDto>(black, ChessHub.BoardStateEvent);

        Guid matchId = await StartMatchAsync(white, black);
        await white.InvokeAsync(nameof(ChessHub.MakeMove), matchId, "e2", "e4");

        foreach (MoveResultDto broadcast in new[] { await seenByWhite, await seenByBlack })
        {
            Assert.Equal("e2", broadcast.Origin);
            Assert.Equal("e4", broadcast.Target);
            Assert.Equal(2, broadcast.State.Turn);
            Assert.Equal("Black", broadcast.State.CurrentPlayer);
            Assert.Contains(broadcast.State.Pieces, piece => piece.Position == "e4" && piece.Type == "Pawn");
        }
    }

    // Sem amarrar a conexão à cor, o preto conseguiria jogar na vez do branco.
    [Fact]
    public async Task MakeMove_OutOfTurn_SendsReceiveErrorAndLeavesTheBoardUntouched()
    {
        await using HubConnection white = _server.CreateConnection();
        await using HubConnection black = _server.CreateConnection();

        Task<string> error = NextEventAsync<string>(black, ChessHub.ErrorEvent);

        Guid matchId = await StartMatchAsync(white, black);
        await black.InvokeAsync(nameof(ChessHub.MakeMove), matchId, "e7", "e5");

        Assert.Equal("It is not your turn", await error);

        BoardStateDto state = await InvokeAsync<BoardStateDto>(white, nameof(ChessHub.GetBoardState), matchId);
        Assert.Equal(1, state.Turn);
        Assert.Equal("White", state.CurrentPlayer);
    }

    [Fact]
    public async Task MakeMove_WithAnIllegalMove_SendsTheBoardExceptionMessage()
    {
        await using HubConnection white = _server.CreateConnection();
        await using HubConnection black = _server.CreateConnection();

        Task<string> error = NextEventAsync<string>(white, ChessHub.ErrorEvent);

        Guid matchId = await StartMatchAsync(white, black);
        await white.InvokeAsync(nameof(ChessHub.MakeMove), matchId, "e2", "e5");

        Assert.Equal("Invalid target position", await error);
    }

    [Fact]
    public async Task JoinMatch_WithAnUnknownId_SendsReceiveErrorAndReturnsNothing()
    {
        await using HubConnection player = _server.CreateConnection();

        Task<string> error = NextEventAsync<string>(player, ChessHub.ErrorEvent);
        await player.StartAsync();

        MatchJoinedDto? refused = await player.InvokeAsync<MatchJoinedDto?>(
            nameof(ChessHub.JoinMatch), Guid.NewGuid());

        Assert.Null(refused);
        Assert.Equal("Match not found", await error);
    }

    [Fact]
    public async Task JoinMatch_WithTheMatchAlreadyFull_IsRefused()
    {
        await using HubConnection white = _server.CreateConnection();
        await using HubConnection black = _server.CreateConnection();
        await using HubConnection spectator = _server.CreateConnection();

        Task<string> error = NextEventAsync<string>(spectator, ChessHub.ErrorEvent);

        Guid matchId = await StartMatchAsync(white, black);
        await spectator.StartAsync();

        MatchJoinedDto? refused = await spectator.InvokeAsync<MatchJoinedDto?>(
            nameof(ChessHub.JoinMatch), matchId);

        Assert.Null(refused);
        Assert.Equal("This match already has two players", await error);
    }

    [Fact]
    public async Task GetPossibleMoves_ReturnsSquaresInsteadOfABooleanMatrix()
    {
        await using HubConnection white = _server.CreateConnection();
        await using HubConnection black = _server.CreateConnection();

        Guid matchId = await StartMatchAsync(white, black);

        PossibleMovesDto moves = await InvokeAsync<PossibleMovesDto>(
            white, nameof(ChessHub.GetPossibleMoves), matchId, "e2");

        Assert.Equal("e2", moves.Origin);
        Assert.Equal(new[] { "e4", "e3" }, moves.Targets);
    }

    [Fact]
    public async Task Disconnecting_NotifiesTheOpponentAndFreesTheSeat()
    {
        await using HubConnection white = _server.CreateConnection();
        HubConnection black = _server.CreateConnection();

        Task opponentLeft = NextEventAsync(white, ChessHub.OpponentLeftEvent);

        Guid matchId = await StartMatchAsync(white, black);
        await black.StopAsync();
        await black.DisposeAsync();

        await opponentLeft;

        // A cadeira preta voltou a ficar livre para outra conexão.
        await using HubConnection replacement = _server.CreateConnection();
        await replacement.StartAsync();

        MatchJoinedDto joined = await InvokeAsync<MatchJoinedDto>(
            replacement, nameof(ChessHub.JoinMatch), matchId);

        Assert.Equal("Black", joined.Color);
    }

    private async Task<Guid> StartMatchAsync(HubConnection white, HubConnection black)
    {
        await white.StartAsync();
        await black.StartAsync();

        MatchJoinedDto created = await InvokeAsync<MatchJoinedDto>(white, nameof(ChessHub.CreateMatch));
        await InvokeAsync<MatchJoinedDto>(black, nameof(ChessHub.JoinMatch), created.MatchId);

        return created.MatchId;
    }

    // Os métodos do Hub devolvem null quando mandam ReceiveError; nos caminhos felizes
    // um null é falha do teste, não resultado esperado.
    private static async Task<T> InvokeAsync<T>(HubConnection connection, string method, params object?[] arguments)
        where T : class
    {
        T? result = await connection.InvokeCoreAsync<T?>(method, arguments);

        Assert.NotNull(result);
        return result;
    }

    private static Task<T> NextEventAsync<T>(HubConnection connection, string eventName)
    {
        TaskCompletionSource<T> received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On<T>(eventName, payload => received.TrySetResult(payload));

        return received.Task.WaitAsync(EventTimeout);
    }

    private static Task NextEventAsync(HubConnection connection, string eventName)
    {
        TaskCompletionSource received = new(TaskCreationOptions.RunContinuationsAsynchronously);
        connection.On(eventName, () => received.TrySetResult());

        return received.Task.WaitAsync(EventTimeout);
    }
}
