using board;
using chess;
using chess_console.Core.Matches;

namespace chess_console.Core.Tests.Matches;

public class InMemoryMatchRepositoryTests
{
    [Fact]
    public void Create_ReturnsSessionsWithIndependentMatches()
    {
        IMatchRepository repository = new InMemoryMatchRepository();
        GameSession first = repository.Create();
        GameSession second = repository.Create();

        Assert.NotEqual(first.Id, second.Id);

        first.Match.MakeMove(Square('e', 2), Square('e', 4));

        Assert.Equal(2, first.Match.turn);
        Assert.Equal(Color.Black, first.Match.actualPlayerColor);

        Assert.Equal(1, second.Match.turn);
        Assert.Equal(Color.White, second.Match.actualPlayerColor);
        Assert.NotNull(second.Match.board.piece(Square('e', 2)));
        Assert.Null(second.Match.board.piece(Square('e', 4)));
    }

    [Fact]
    public void Get_ReturnsNullForAnUnknownId()
    {
        IMatchRepository repository = new InMemoryMatchRepository();

        Assert.Null(repository.Get(Guid.NewGuid()));
    }

    [Fact]
    public void Remove_TakesTheSessionOutOfTheRepository()
    {
        IMatchRepository repository = new InMemoryMatchRepository();
        GameSession session = repository.Create();

        Assert.True(repository.Remove(session.Id));
        Assert.Null(repository.Get(session.Id));
        Assert.Equal(0, repository.Count);
        Assert.False(repository.Remove(session.Id));
    }

    [Fact]
    public async Task FindByConnection_FindsTheSessionTheConnectionIsSeatedIn()
    {
        IMatchRepository repository = new InMemoryMatchRepository();
        repository.Create();
        GameSession target = repository.Create();

        await target.ExecuteAsync(s => s.TryTakeSeat("conn-white", out _));

        Assert.Same(target, repository.FindByConnection("conn-white"));
        Assert.Null(repository.FindByConnection("conn-unknown"));
        Assert.Null(repository.FindByConnection(null));
    }

    [Fact]
    public async Task GetWaitingForOpponent_OnlyListsSessionsWithASingleSeatTaken()
    {
        IMatchRepository repository = new InMemoryMatchRepository();
        GameSession empty = repository.Create();
        GameSession waiting = repository.Create();
        GameSession full = repository.Create();

        await waiting.ExecuteAsync(s => s.TryTakeSeat("white-1", out _));
        await full.ExecuteAsync(s =>
        {
            s.TryTakeSeat("white-2", out _);
            s.TryTakeSeat("black-2", out _);
        });

        GameSession[] open = repository.GetWaitingForOpponent().ToArray();

        Assert.Single(open);
        Assert.Same(waiting, open[0]);
        Assert.DoesNotContain(empty, open);
        Assert.DoesNotContain(full, open);
    }

    [Fact]
    public async Task Create_UnderConcurrency_RegistersEveryMatch()
    {
        IMatchRepository repository = new InMemoryMatchRepository();

        GameSession[] sessions = await Task.WhenAll(Enumerable.Range(0, 64)
            .Select(_ => Task.Run(() => repository.Create())));

        Assert.Equal(64, repository.Count);
        Assert.Equal(64, sessions.Select(session => session.Id).Distinct().Count());
        Assert.All(sessions, session => Assert.Same(session, repository.Get(session.Id)));
    }

    private static Position Square(char column, int row) => new ChessPosition(column, row).ToPosition();
}
