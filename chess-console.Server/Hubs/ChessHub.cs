using chess_console.Core.Dto;
using chess_console.Exceptions;
using chess_console.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace chess_console.Server.Hubs;

public sealed class ChessHub : Hub
{
    public const string BoardStateEvent = "ReceiveBoardState";
    public const string ErrorEvent = "ReceiveError";
    public const string OpponentJoinedEvent = "OpponentJoined";
    public const string OpponentLeftEvent = "OpponentLeft";

    private readonly MatchService _matches;

    public ChessHub(MatchService matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        _matches = matches;
    }

    public Task<MatchJoinedDto?> CreateMatch() =>
        GuardAsync(async () =>
        {
            MatchJoinedDto match = await _matches.CreateAsync(Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(match.MatchId));

            return match;
        });

    public Task<MatchJoinedDto?> JoinMatch(Guid matchId) =>
        GuardAsync(async () =>
        {
            MatchJoinedDto match = await _matches.JoinAsync(matchId, Context.ConnectionId);

            // Entra no grupo antes de avisar, para OthersInGroup significar "o adversário".
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(matchId));
            await Clients.OthersInGroup(GroupOf(matchId)).SendAsync(OpponentJoinedEvent, match.Color);

            return match;
        });

    public Task<BoardStateDto?> GetBoardState(Guid matchId) =>
        GuardAsync(() => _matches.GetStateAsync(matchId));

    public Task<PossibleMovesDto?> GetPossibleMoves(Guid matchId, string origin) =>
        GuardAsync(() => _matches.GetPossibleMovesAsync(matchId, origin, Context.ConnectionId));

    public async Task MakeMove(Guid matchId, string origin, string target)
    {
        MoveResultDto? result = await GuardAsync(
            () => _matches.MakeMoveAsync(matchId, origin, target, Context.ConnectionId));

        if (result is null)
            return;

        await Clients.Group(GroupOf(matchId)).SendAsync(BoardStateEvent, result);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Guid? matchId = await _matches.LeaveAsync(Context.ConnectionId);

        // O SignalR tira a conexão dos grupos sozinho no disconnect; aqui só avisamos
        // quem ficou do outro lado do tabuleiro.
        if (matchId is not null)
            await Clients.OthersInGroup(GroupOf(matchId.Value)).SendAsync(OpponentLeftEvent);

        await base.OnDisconnectedAsync(exception);
    }

    // Toda BoardException — do tabuleiro ou das regras de sessão — vira ReceiveError
    // para quem chamou, no lugar do Console.WriteLine do Program.cs.
    private async Task<T?> GuardAsync<T>(Func<Task<T>> operation) where T : class
    {
        try
        {
            return await operation();
        }
        catch (BoardException e)
        {
            await Clients.Caller.SendAsync(ErrorEvent, e.Message);
            return null;
        }
    }

    private static string GroupOf(Guid matchId) => matchId.ToString();
}
