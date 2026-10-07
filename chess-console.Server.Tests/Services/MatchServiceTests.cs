using chess_console.Core.Dto;
using chess_console.Core.Matches;
using chess_console.Exceptions;
using chess_console.Server.Services;

namespace chess_console.Server.Tests.Services;

public class MatchServiceTests
{
    private const string White = "conn-white";
    private const string Black = "conn-black";

    [Fact]
    public async Task CreateAsync_SeatsTheCallerAsWhiteAndReturnsTheOpeningBoard()
    {
        MatchService service = NewService();

        MatchJoinedDto created = await service.CreateAsync(White);

        Assert.NotEqual(Guid.Empty, created.MatchId);
        Assert.Equal("White", created.Color);
        Assert.Equal(32, created.State.Pieces.Count);
        Assert.Equal("White", created.State.CurrentPlayer);
    }

    [Fact]
    public async Task JoinAsync_SeatsTheSecondCallerAsBlackInTheSameMatch()
    {
        MatchService service = NewService();
        MatchJoinedDto created = await service.CreateAsync(White);

        MatchJoinedDto joined = await service.JoinAsync(created.MatchId, Black);

        Assert.Equal(created.MatchId, joined.MatchId);
        Assert.Equal("Black", joined.Color);
    }

    [Fact]
    public async Task JoinAsync_RefusesAThirdPlayer()
    {
        MatchService service = NewService();
        MatchJoinedDto created = await service.CreateAsync(White);
        await service.JoinAsync(created.MatchId, Black);

        BoardException error = await Assert.ThrowsAsync<BoardException>(
            () => service.JoinAsync(created.MatchId, "conn-spectator"));

        Assert.Equal("This match already has two players", error.Message);
    }

    [Fact]
    public async Task JoinAsync_RejectsAnUnknownMatch()
    {
        MatchService service = NewService();

        BoardException error = await Assert.ThrowsAsync<BoardException>(
            () => service.JoinAsync(Guid.NewGuid(), White));

        Assert.Equal("Match not found", error.Message);
    }

    [Fact]
    public async Task MakeMoveAsync_AppliesTheMoveAndReportsTheNewState()
    {
        MatchService service = NewService();
        Guid matchId = await StartMatchAsync(service);

        MoveResultDto result = await service.MakeMoveAsync(matchId, "e2", "e4", White);

        Assert.Equal("e2", result.Origin);
        Assert.Equal("e4", result.Target);
        Assert.Equal(2, result.State.Turn);
        Assert.Equal("Black", result.State.CurrentPlayer);
        Assert.Contains(result.State.Pieces, piece => piece.Position == "e4" && piece.Type == "Pawn");
    }

    [Fact]
    public async Task MakeMoveAsync_AppliesTheSelectedPromotion()
    {
        MatchService service = NewService();
        Guid matchId = await StartMatchAsync(service);

        await service.MakeMoveAsync(matchId, "a2", "a4", White);
        await service.MakeMoveAsync(matchId, "b7", "b5", Black);
        await service.MakeMoveAsync(matchId, "a4", "b5", White);
        await service.MakeMoveAsync(matchId, "a7", "a6", Black);
        await service.MakeMoveAsync(matchId, "b5", "b6", White);
        await service.MakeMoveAsync(matchId, "a6", "a5", Black);
        await service.MakeMoveAsync(matchId, "b6", "c7", White);
        await service.MakeMoveAsync(matchId, "a5", "a4", Black);
        MoveResultDto result = await service.MakeMoveAsync(
            matchId, "c7", "b8", White, promotion: "knight");

        Assert.Contains(result.State.Pieces, piece => piece.Position == "b8" && piece.Type == "Knight");
    }

    // O ponto central da Fase 4: hoje o ChessMatch só sabe de cor, não de conexão.
    [Fact]
    public async Task MakeMoveAsync_RefusesThePlayerWhoIsNotToMove()
    {
        MatchService service = NewService();
        Guid matchId = await StartMatchAsync(service);

        BoardException error = await Assert.ThrowsAsync<BoardException>(
            () => service.MakeMoveAsync(matchId, "e7", "e5", Black));

        Assert.Equal("It is not your turn", error.Message);
        Assert.Equal(1, (await service.GetStateAsync(matchId)).Turn);
    }

    [Fact]
    public async Task MakeMoveAsync_RefusesAConnectionThatIsNotInTheMatch()
    {
        MatchService service = NewService();
        Guid matchId = await StartMatchAsync(service);

        BoardException error = await Assert.ThrowsAsync<BoardException>(
            () => service.MakeMoveAsync(matchId, "e2", "e4", "conn-stranger"));

        Assert.Equal("You are not playing this match", error.Message);
    }

    [Fact]
    public async Task MakeMoveAsync_RefusesWhileTheMatchHasOnlyOnePlayer()
    {
        MatchService service = NewService();
        MatchJoinedDto created = await service.CreateAsync(White);

        BoardException error = await Assert.ThrowsAsync<BoardException>(
            () => service.MakeMoveAsync(created.MatchId, "e2", "e4", White));

        Assert.Equal("Waiting for the opponent to join", error.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("zz")]
    [InlineData("e9")]
    [InlineData("e44")]
    [InlineData("i2")]
    public async Task MakeMoveAsync_RejectsGarbageSquaresWithoutCrashing(string origin)
    {
        MatchService service = NewService();
        Guid matchId = await StartMatchAsync(service);

        BoardException error = await Assert.ThrowsAsync<BoardException>(
            () => service.MakeMoveAsync(matchId, origin, "e4", White));

        Assert.StartsWith("Invalid board square", error.Message);
    }

    [Fact]
    public async Task MakeMoveAsync_RefusesOnceTheMatchIsOver()
    {
        MatchService service = NewService();
        Guid matchId = await StartMatchAsync(service);

        // Fool's mate: 1. f3 e5 2. g4 Qh4#
        await service.MakeMoveAsync(matchId, "f2", "f3", White);
        await service.MakeMoveAsync(matchId, "e7", "e5", Black);
        await service.MakeMoveAsync(matchId, "g2", "g4", White);
        MoveResultDto mate = await service.MakeMoveAsync(matchId, "d8", "h4", Black);

        Assert.True(mate.State.Completed);
        Assert.Equal("Black", mate.State.Winner);

        BoardException error = await Assert.ThrowsAsync<BoardException>(
            () => service.MakeMoveAsync(matchId, "a2", "a3", White));

        Assert.Equal("This match is already over", error.Message);
    }

    [Fact]
    public async Task GetPossibleMovesAsync_ReturnsTheSquaresInChessNotation()
    {
        MatchService service = NewService();
        Guid matchId = await StartMatchAsync(service);

        PossibleMovesDto moves = await service.GetPossibleMovesAsync(matchId, "e2", White);

        Assert.Equal("e2", moves.Origin);
        Assert.Equal(new[] { "e4", "e3" }, moves.Targets);
    }

    [Fact]
    public async Task GetPossibleMovesAsync_RefusesThePlayerWhoIsNotToMove()
    {
        MatchService service = NewService();
        Guid matchId = await StartMatchAsync(service);

        BoardException error = await Assert.ThrowsAsync<BoardException>(
            () => service.GetPossibleMovesAsync(matchId, "e7", Black));

        Assert.Equal("It is not your turn", error.Message);
    }

    [Fact]
    public async Task LeaveAsync_FreesTheSeatAndKeepsTheMatchWhileSomeoneIsStillThere()
    {
        IMatchRepository repository = new InMemoryMatchRepository();
        MatchService service = new(repository);
        Guid matchId = await StartMatchAsync(service);

        Assert.Equal(matchId, await service.LeaveAsync(Black));
        Assert.Equal(1, repository.Count);

        // A cadeira preta ficou livre: outra conexão assume a mesma cor.
        Assert.Equal("Black", (await service.JoinAsync(matchId, "conn-black-2")).Color);
    }

    [Fact]
    public async Task LeaveAsync_DropsTheMatchWhenTheLastPlayerLeaves()
    {
        IMatchRepository repository = new InMemoryMatchRepository();
        MatchService service = new(repository);
        Guid matchId = await StartMatchAsync(service);

        await service.LeaveAsync(White);
        await service.LeaveAsync(Black);

        Assert.Equal(0, repository.Count);
        Assert.Null(repository.Get(matchId));
    }

    [Fact]
    public async Task LeaveAsync_IgnoresAConnectionWithoutAMatch()
    {
        MatchService service = NewService();

        Assert.Null(await service.LeaveAsync("conn-stranger"));
    }

    private static MatchService NewService() => new(new InMemoryMatchRepository());

    private static async Task<Guid> StartMatchAsync(MatchService service)
    {
        MatchJoinedDto created = await service.CreateAsync(White);
        await service.JoinAsync(created.MatchId, Black);

        return created.MatchId;
    }
}
