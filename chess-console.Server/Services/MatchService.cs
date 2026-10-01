using board;
using chess;
using chess_console.Core.Dto;
using chess_console.Core.Matches;
using chess_console.Exceptions;

namespace chess_console.Server.Services;

// Regras de sessão, turno e concorrência. O Hub só cuida do transporte: tudo que é
// decisão ("é a sua vez?", "essa partida existe?") mora aqui e é testável sem SignalR.
public sealed class MatchService
{
    private readonly IMatchRepository _repository;

    public MatchService(IMatchRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        _repository = repository;
    }

    public Task<MatchJoinedDto> CreateAsync(string connectionId, CancellationToken cancellationToken = default) =>
        TakeSeatAsync(_repository.Create(), connectionId, cancellationToken);

    public Task<MatchJoinedDto> JoinAsync(Guid matchId, string connectionId, CancellationToken cancellationToken = default) =>
        TakeSeatAsync(Require(matchId), connectionId, cancellationToken);

    public Task<BoardStateDto> GetStateAsync(Guid matchId, CancellationToken cancellationToken = default) =>
        Require(matchId).ExecuteAsync(session => BoardMapper.ToDto(session.Match), cancellationToken);

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

    public Task<MoveResultDto> MakeMoveAsync(
        Guid matchId,
        string? origin,
        string? target,
        string connectionId,
        CancellationToken cancellationToken = default) =>
        Require(matchId).ExecuteAsync(session =>
        {
            EnsureItIsTheTurnOf(session, connectionId);

            Position from = ChessPosition.Parse(origin).ToPosition();
            Position to = ChessPosition.Parse(target).ToPosition();

            // As mesmas três chamadas do laço do Program.cs, agora atrás do lock da sessão.
            session.Match.ValideOriginPosition(from);
            session.Match.ValidadeTargetPosition(from, to);
            session.Match.MakeMove(from, to);

            return BoardMapper.ToMoveResult(session.Match, from, to);
        }, cancellationToken);

    // Devolve o id da partida de onde a conexão saiu, ou null se ela não estava em nenhuma.
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

    private static Task<MatchJoinedDto> TakeSeatAsync(
        GameSession session,
        string connectionId,
        CancellationToken cancellationToken) =>
        session.ExecuteAsync(current =>
        {
            if (!current.TryTakeSeat(connectionId, out Color color))
                throw new BoardException("This match already has two players");

            return new MatchJoinedDto(current.Id, color.ToString(), BoardMapper.ToDto(current.Match));
        }, cancellationToken);

    private GameSession Require(Guid matchId) =>
        _repository.Get(matchId) ?? throw new BoardException("Match not found");

    // Ponto 4 do diagnóstico: ValideOriginPosition só compara cores, não sabe quem está
    // conectado. Sem amarrar a conexão à cor da vez, qualquer client move as duas cores.
    private static void EnsureItIsTheTurnOf(GameSession session, string connectionId)
    {
        if (!session.IsSeated(connectionId))
            throw new BoardException("You are not playing this match");

        if (session.Match.completed)
            throw new BoardException("This match is already over");

        if (!session.IsFull)
            throw new BoardException("Waiting for the opponent to join");

        if (!session.IsTurnOf(connectionId))
            throw new BoardException("It is not your turn");
    }
}
