using chess_console.Core.Dto;
using chess_console.Exceptions;
using chess_console.Server.Filters;
using chess_console.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace chess_console.Server.Hubs;

/* ChessHub é a camada de transporte (Handshake e conexão websocket). 
 * Responsável por realizar o contrato entre cliente/aplicação, 
 * expor os métodos publico (mantendo uma conexão persistente) e 
 * enviar mensagens ao cliente*/

public sealed class ChessHub : Hub
{
    public const string BoardStateEvent = "ReceiveBoardState";
    public const string ClockStateEvent = "ReceiveClockState";
    public const string ErrorEvent = "ReceiveError";
    public const string OpponentJoinedEvent = "OpponentJoined";
    public const string OpponentLeftEvent = "OpponentLeft";
    public const string MatchAbandonedEvent = "MatchAbandoned";
    public const string SideSwapRequestedEvent = "SideSwapRequested";
    public const string SideSwapDeclinedEvent = "SideSwapDeclined";
    public const string SideSwapAcceptedEvent = "SideSwapAccepted";

    private readonly MatchService _matches;

    public ChessHub(MatchService matches)
    {
        ArgumentNullException.ThrowIfNull(matches);

        _matches = matches;
    }

    // Chama o serviço e adicionam a conexão ao grupo cujo nome é o matchId.
    public Task<MatchJoinedDto?> CreateMatch() =>
        GuardAsync(async () =>
        {
            PlayerSession player = PlayerTokenHubFilter.GetRequired(Context);
            MatchJoinedDto match = await _matches.CreateAsync(
                Context.ConnectionId,
                player.Name);
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(match.MatchId));

            return match;
        });

    // Chama o serviço e adicionam a conexão ao grupo cujo nome é o matchId. (avisa ao primeiro jogador quando o oponente entra)
    public Task<MatchJoinedDto?> JoinMatch(Guid matchId) =>
        GuardAsync(async () =>
        {
            PlayerSession player = PlayerTokenHubFilter.GetRequired(Context);
            MatchJoinedDto match = await _matches.JoinAsync(
                matchId,
                Context.ConnectionId,
                player.Name);

            // Entra no grupo antes de avisar, para OthersInGroup significar "o adversário".
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupOf(matchId));
            await Clients.OthersInGroup(GroupOf(matchId)).SendAsync(OpponentJoinedEvent, match.Color);
            await Clients.OthersInGroup(GroupOf(matchId)).SendAsync(ClockStateEvent, match.State);

            return match;
        });

    public Task<Guid[]> GetWaitingMatches() =>
        Task.FromResult(_matches.GetWaitingMatchIds());

    public async Task<bool> LeaveMatch(Guid matchId)
    {
        (bool left, BoardStateDto? finalState) =
            await _matches.AbandonAsync(matchId, Context.ConnectionId);
        if (!left)
            return false;

        if (finalState is null)
            await Clients.OthersInGroup(GroupOf(matchId)).SendAsync(OpponentLeftEvent);
        else
            await Clients.OthersInGroup(GroupOf(matchId)).SendAsync(MatchAbandonedEvent, finalState);

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupOf(matchId));
        return true;
    }

    public async Task<bool> ExpireWaitingMatch(Guid matchId)
    {
        try
        {
            return await _matches.ExpireWaitingAsync(matchId, Context.ConnectionId);
        }
        catch (BoardException e)
        {
            await Clients.Caller.SendAsync(ErrorEvent, e.Message);
            return false;
        }
    }

    // Retorna dados ao chamador
    public Task<BoardStateDto?> GetBoardState(Guid matchId) =>
        GuardAsync(() => _matches.GetStateAsync(matchId));

    // Retorna dados ao chamador
    public Task<PossibleMovesDto?> GetPossibleMoves(Guid matchId, string origin) =>
        GuardAsync(() => _matches.GetPossibleMovesAsync(matchId, origin, Context.ConnectionId));

    public Task MakeMove(Guid matchId, string origin, string target) =>
        MakeMoveCore(matchId, origin, target, null);

    public Task MakeMoveWithPromotion(Guid matchId, string origin, string target, string promotion) =>
        MakeMoveCore(matchId, origin, target, promotion);

    public async Task OfferSideSwap(Guid matchId)
    {
        string? targetConnectionId = await GuardAsync(
            () => _matches.OfferSideSwapAsync(matchId, Context.ConnectionId));

        if (targetConnectionId is not null)
            await Clients.Client(targetConnectionId).SendAsync(SideSwapRequestedEvent);
    }

    public async Task RespondToSideSwap(Guid matchId, bool accepted)
    {
        SideSwapResult? result = await GuardAsync(
            () => _matches.RespondToSideSwapAsync(matchId, Context.ConnectionId, accepted));

        if (result is null)
            return;

        if (!result.Accepted)
        {
            await Clients.Client(result.RequesterConnectionId).SendAsync(SideSwapDeclinedEvent);
            return;
        }

        await Clients.Client(result.RequesterConnectionId)
            .SendAsync(SideSwapAcceptedEvent, result.RequesterColor);
        await Clients.Caller.SendAsync(SideSwapAcceptedEvent, result.ResponderColor);
    }

    private async Task MakeMoveCore(Guid matchId, string origin, string target, string? promotion)
    {
        MoveResultDto? result = await GuardAsync(
            () => _matches.MakeMoveAsync(
                matchId,
                origin,
                target,
                Context.ConnectionId,
                promotion: promotion));

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
