using board;
using chess;
using chess_console.Core.Dto;
using chess_console.Core.Matches;
using chess_console.Exceptions;

namespace chess_console.Server.Services;

/* MatchService é a camada de aplicação (Regras de sessão, turno, concorrência e tudo que é decisão). 
 * Ele impede que o Hub conheça detalhes de armazenamento ou regras de assento */

public sealed class MatchService
{
    private readonly IMatchRepository _repository;

    public MatchService(IMatchRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
    }

    // Pede uma nova sessão ao repositório e ocupa a primeira cadeira, sempre branca.
    public Task<MatchJoinedDto> CreateAsync(
        string connectionId,
        string playerName = "Anonymous",
        CancellationToken cancellationToken = default) =>
        TakeSeatAsync(_repository.Create(), connectionId, playerName, cancellationToken);

    // Procura o Guid, ocupa a cadeira livre e devolve a cor e o estado inicial.
    public Task<MatchJoinedDto> JoinAsync(
        Guid matchId,
        string connectionId,
        string playerName = "Anonymous",
        CancellationToken cancellationToken = default) =>
        TakeSeatAsync(Require(matchId), connectionId, playerName, cancellationToken);

    public Guid[] GetWaitingMatchIds() =>
        _repository.GetWaitingForOpponent()
            .OrderBy(session => session.CreatedAtUtc)
            .Select(session => session.Id)
            .ToArray();

    public async Task<bool> ExpireWaitingAsync(
        Guid matchId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        GameSession session = Require(matchId);
        bool expired = await session.ExecuteAsync(current =>
        {
            if (!current.IsWaitingForOpponent || !current.IsSeated(connectionId))
                return false;

            current.ReleaseSeat(connectionId);
            return true;
        }, cancellationToken);

        if (expired)
            _repository.Remove(session.Id);

        return expired;
    }

    // Entra no lock da sessão e transforma o ChessMatch em BoardStateDto.
    public Task<BoardStateDto> GetStateAsync(Guid matchId, CancellationToken cancellationToken = default) =>
        Require(matchId).ExecuteAsync(session => StateOf(session), cancellationToken);

    // Verifica se a conexão é o jogador da vez, converte "e2" (linguagem natural do xadrez) em Position e delega a validação ao Core
    public Task<PossibleMovesDto> GetPossibleMovesAsync(
        Guid matchId,
        string? origin,
        string connectionId,
        CancellationToken cancellationToken = default) =>
        Require(matchId).ExecuteAsync(session =>
        {
            EnsureItIsTheTurnOf(session, connectionId);

            return BoardMapper.ToPossibleMoves(session.Match, ChessPosition.Parse(origin).ToPosition());
        }, cancellationToken);

    // Verifica conexão, partida cheia, partida encerrada e turno; depois executa ValideOriginPosition, ValidadeTargetPosition e MakeMove dentro do lock.
    public Task<MoveResultDto> MakeMoveAsync(
        Guid matchId,
        string? origin,
        string? target,
        string connectionId,
        CancellationToken cancellationToken = default,
        string? promotion = null) =>
        Require(matchId).ExecuteAsync(session =>
        {
            EnsureItIsTheTurnOf(session, connectionId);

            Position from = ChessPosition.Parse(origin).ToPosition();
            Position to = ChessPosition.Parse(target).ToPosition();

            // As mesmas três chamadas do laço do Program.cs, agora atrás do lock da sessão.
            session.Match.ValideOriginPosition(from);
            session.Match.ValidadeTargetPosition(from, to);
            session.Match.MakeMove(from, to, ParsePromotion(promotion));
            session.RestartClock();

            (int whiteSeconds, int blackSeconds, bool running) =
                session.GetClockState(DateTime.UtcNow);

            return BoardMapper.ToMoveResult(
                session.Match,
                from,
                to,
                whiteSeconds,
                blackSeconds,
                running);
        }, cancellationToken);

    public Task<string> OfferSideSwapAsync(
        Guid matchId,
        string connectionId,
        CancellationToken cancellationToken = default) =>
        Require(matchId).ExecuteAsync(session =>
        {
            EnsureCanSwapSides(session, connectionId);

            if (session.HasPendingSideSwap)
                throw new BoardException("A side swap offer is already pending");

            if (!session.TryOfferSideSwap(connectionId, out string targetConnectionId))
                throw new BoardException("Unable to offer a side swap");

            return targetConnectionId;
        }, cancellationToken);

    public Task<SideSwapResult> RespondToSideSwapAsync(
        Guid matchId,
        string connectionId,
        bool accepted,
        CancellationToken cancellationToken = default) =>
        Require(matchId).ExecuteAsync(session =>
        {
            EnsureCanSwapSides(session, connectionId);

            if (!session.TryRespondToSideSwap(connectionId, accepted, out string requesterConnectionId))
                throw new BoardException("No side swap offer is pending for you");

            Color requesterColor = session.ColorOf(requesterConnectionId)
                ?? throw new BoardException("Side swap participants are no longer in the match");
            Color responderColor = session.ColorOf(connectionId)
                ?? throw new BoardException("Side swap participants are no longer in the match");

            return new SideSwapResult(
                requesterConnectionId,
                connectionId,
                accepted,
                requesterColor.ToString(),
                responderColor.ToString());
        }, cancellationToken);

    private static PromotionPiece? ParsePromotion(string? promotion)
    {
        if (promotion is null)
            return null;

        return promotion.Trim().ToLowerInvariant() switch
        {
            "q" or "queen" => PromotionPiece.Queen,
            "r" or "rook" => PromotionPiece.Rook,
            "b" or "bishop" => PromotionPiece.Bishop,
            "n" or "knight" => PromotionPiece.Knight,
            _ => throw new BoardException("Invalid promotion piece")
        };
    }

    // Encontra a sessão pela conexão, libera a cadeira e remove a partida somente quando os dois jogadores saíram.
    public async Task<Guid?> LeaveAsync(string connectionId, CancellationToken cancellationToken = default)
    {
        GameSession? session = _repository.FindByConnection(connectionId);

        if (session is null)
            return null;

        bool abandoned = await session.ExecuteAsync(current =>
        {
            current.ReleaseSeat(connectionId);
            return current.IsEmpty;
        }, cancellationToken);

        if (abandoned)
            _repository.Remove(session.Id);

        return session.Id;
    }

    public async Task<bool> LeaveAsync(
        Guid matchId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        GameSession? session = _repository.FindByConnection(connectionId);

        if (session?.Id != matchId)
            return false;

        return await LeaveAsync(connectionId, cancellationToken) == matchId;
    }

    private static Task<MatchJoinedDto> TakeSeatAsync(
        GameSession session,
        string connectionId,
        string playerName,
        CancellationToken cancellationToken) =>
        session.ExecuteAsync(current =>
        {
            if (!current.TryTakeSeat(connectionId, out Color color))
                throw new BoardException("This match already has two players");

            return new MatchJoinedDto(current.Id, color.ToString(), StateOf(current), playerName);
        }, cancellationToken);

    private static BoardStateDto StateOf(GameSession session)
    {
        (int whiteSeconds, int blackSeconds, bool running) = session.GetClockState(DateTime.UtcNow);
        return BoardMapper.ToDto(session.Match, whiteSeconds, blackSeconds, running);
    }

    private GameSession Require(Guid matchId) =>
        _repository.Get(matchId) ?? throw new BoardException("Match not found");

    private static void EnsureItIsTheTurnOf(GameSession session, string connectionId)
    {
        session.UpdateClock(DateTime.UtcNow);

        if (!session.IsSeated(connectionId))
            throw new BoardException("You are not playing this match");

        if (session.Match.completed)
            throw new BoardException("This match is already over");

        if (!session.IsFull)
            throw new BoardException("Waiting for the opponent to join");

        if (!session.IsTurnOf(connectionId))
            throw new BoardException("It is not your turn");
    }

    private static void EnsureCanSwapSides(GameSession session, string connectionId)
    {
        session.UpdateClock(DateTime.UtcNow);

        if (!session.IsSeated(connectionId))
            throw new BoardException("You are not playing this match");

        if (!session.IsFull)
            throw new BoardException("Waiting for the opponent to join");

        if (session.Match.completed)
            throw new BoardException("This match is already over");

        if (session.Match.MoveHistory.Count > 0)
            throw new BoardException("Side swapping is only available before the first move");
    }
}

public sealed record SideSwapResult(
    string RequesterConnectionId,
    string ResponderConnectionId,
    bool Accepted,
    string RequesterColor,
    string ResponderColor);
