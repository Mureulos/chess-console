using board;
using chess;
using chess_console.Core.Matches;

namespace chess_console.Core.Tests.Matches;

public class GameSessionTests
{
    [Fact]
    public async Task TryTakeSeat_GivesWhiteToTheFirstConnectionAndBlackToTheSecond()
    {
        GameSession session = new();

        Assert.Equal<Color?>(Color.White, await TakeSeatAsync(session, "conn-1"));
        Assert.Equal<Color?>(Color.Black, await TakeSeatAsync(session, "conn-2"));
        Assert.True(session.IsFull);
    }

    [Fact]
    public async Task Clock_RemainsStoppedUntilTheFirstMove()
    {
        GameSession session = new();
        await TakeSeatAsync(session, "white");

        (int whiteBefore, int blackBefore, bool waiting) =
            await session.ExecuteAsync(s => s.GetClockState(DateTime.UtcNow));

        await TakeSeatAsync(session, "black");
        (int whiteAfter, int blackAfter, bool running) =
            await session.ExecuteAsync(s => s.GetClockState(DateTime.UtcNow));

        Assert.Equal(ChessMatch.InitialClockSeconds, whiteBefore);
        Assert.Equal(ChessMatch.InitialClockSeconds, blackBefore);
        Assert.False(waiting);
        Assert.Equal(ChessMatch.InitialClockSeconds, whiteAfter);
        Assert.Equal(ChessMatch.InitialClockSeconds, blackAfter);
        Assert.False(running);

        await session.ExecuteAsync(s =>
        {
            s.Match.MakeMove(Square('e', 2), Square('e', 4));
            s.RestartClock();
        });

        (int whiteAfterMove, int blackAfterMove, bool runningAfterMove) =
            await session.ExecuteAsync(s => s.GetClockState(DateTime.UtcNow));

        Assert.Equal(ChessMatch.InitialClockSeconds, whiteAfterMove);
        Assert.Equal(ChessMatch.InitialClockSeconds, blackAfterMove);
        Assert.True(runningAfterMove);
    }

    [Fact]
    public async Task Clock_CompletesTheMatchWhenTheActivePlayerRunsOutOfTime()
    {
        GameSession session = new();
        await TakeSeatAsync(session, "white");
        await TakeSeatAsync(session, "black");

        await session.ExecuteAsync(s =>
        {
            s.Match.MakeMove(Square('e', 2), Square('e', 4));
            s.RestartClock();
        });

        bool changed = await session.ExecuteAsync(s =>
            s.UpdateClock(DateTime.UtcNow.AddMinutes(10).AddSeconds(1)));

        (int whiteSeconds, int blackSeconds, bool running) =
            await session.ExecuteAsync(s => s.GetClockState(DateTime.UtcNow));

        Assert.True(changed);
        Assert.Equal(ChessMatch.InitialClockSeconds, whiteSeconds);
        Assert.Equal(0, blackSeconds);
        Assert.False(running);
        Assert.True(session.Match.completed);
        Assert.True(session.Match.timeout);
        Assert.Equal(Color.White, session.Match.Opponent(session.Match.actualPlayerColor));
    }

    [Fact]
    public async Task TryTakeSeat_IsIdempotentForTheSameConnection()
    {
        GameSession session = new();

        Assert.Equal<Color?>(Color.White, await TakeSeatAsync(session, "conn-1"));
        Assert.Equal<Color?>(Color.White, await TakeSeatAsync(session, "conn-1"));
        Assert.Null(session.BlackConnectionId);
        Assert.True(session.IsWaitingForOpponent);
    }

    [Fact]
    public async Task TryTakeSeat_RefusesAThirdConnection()
    {
        GameSession session = new();
        await TakeSeatAsync(session, "conn-1");
        await TakeSeatAsync(session, "conn-2");

        Assert.Null(await TakeSeatAsync(session, "conn-3"));
        Assert.True(session.IsFull);
    }

    [Fact]
    public void SeatChanges_OutsideExecuteAsync_AreRejected()
    {
        GameSession session = new();

        Assert.Throws<InvalidOperationException>(() => session.TryTakeSeat("conn-1", out _));
        Assert.Throws<InvalidOperationException>(() => session.ReleaseSeat("conn-1"));
    }

    [Fact]
    public async Task ReleaseSeat_FreesTheColorForAnotherConnection()
    {
        GameSession session = new();
        await TakeSeatAsync(session, "white-1");
        await TakeSeatAsync(session, "black-1");

        bool released = await session.ExecuteAsync(s => s.ReleaseSeat("white-1"));

        Assert.True(released);
        Assert.Null(session.WhiteConnectionId);
        Assert.True(session.IsWaitingForOpponent);
        Assert.Equal<Color?>(Color.White, await TakeSeatAsync(session, "white-2"));
    }

    [Fact]
    public async Task ColorOf_ReturnsNullForAConnectionOutsideTheMatch()
    {
        GameSession session = new();
        await TakeSeatAsync(session, "white");

        Assert.Equal<Color?>(Color.White, session.ColorOf("white"));
        Assert.Null(session.ColorOf("black"));
        Assert.Null(session.ColorOf(null));
        Assert.Null(session.ColorOf(string.Empty));
    }

    [Fact]
    public async Task IsTurnOf_FollowsTheColorToMove()
    {
        GameSession session = new();
        await TakeSeatAsync(session, "white");
        await TakeSeatAsync(session, "black");

        Assert.True(session.IsTurnOf("white"));
        Assert.False(session.IsTurnOf("black"));
        Assert.False(session.IsTurnOf("spectator"));

        await session.ExecuteAsync(s => s.Match.MakeMove(Square('e', 2), Square('e', 4)));

        Assert.False(session.IsTurnOf("white"));
        Assert.True(session.IsTurnOf("black"));
    }

    [Fact]
    public async Task ExecuteAsync_SerializesConcurrentOperations()
    {
        GameSession session = new();
        int inFlight = 0;
        int peak = 0;
        int counter = 0;

        Task[] operations = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(async () => await session.ExecuteAsync(_ =>
            {
                int current = Interlocked.Increment(ref inFlight);
                peak = Math.Max(peak, current);

                // Leitura e escrita separadas de propósito: sem o lock da sessão,
                // este contador perde incrementos.
                int read = counter;
                Thread.Sleep(1);
                counter = read + 1;

                Interlocked.Decrement(ref inFlight);
            })))
            .ToArray();

        await Task.WhenAll(operations);

        Assert.Equal(1, peak);
        Assert.Equal(50, counter);
    }

    [Fact]
    public async Task TryTakeSeat_UnderConcurrency_HandsOutAtMostTwoSeats()
    {
        GameSession session = new();

        Color?[] seats = await Task.WhenAll(Enumerable.Range(0, 32)
            .Select(index => Task.Run(() => TakeSeatAsync(session, $"conn-{index}"))));

        Assert.Equal(1, seats.Count(seat => seat == Color.White));
        Assert.Equal(1, seats.Count(seat => seat == Color.Black));
        Assert.Equal(30, seats.Count(seat => seat is null));
    }

    private static Task<Color?> TakeSeatAsync(GameSession session, string connectionId) =>
        session.ExecuteAsync(s => s.TryTakeSeat(connectionId, out Color color) ? color : (Color?)null);

    private static Position Square(char column, int row) => new ChessPosition(column, row).ToPosition();
}
