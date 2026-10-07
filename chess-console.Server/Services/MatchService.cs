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
    public Task<MatchJoinedDto> CreateAsync(string connectionId, CancellationToken cancellationToken = default) =>
        TakeSeatAsync(_repository.Create(), connectionId, cancellationToken);

    // Procura o Guid, ocupa a cadeira livre e devolve a cor e o estado inicial.
    public Task<MatchJoinedDto> JoinAsync(Guid matchId, string connectionId, CancellationToken cancellationToken = default) =>
        TakeSeatAsync(Require(matchId), connectionId, cancellationToken);

    // Entra no lock da sessão e transforma o ChessMatch em BoardStateDto.
    public Task<BoardStateDto> GetStateAsync(Guid matchId, CancellationToken cancellationToken = default) =>
        Require(matchId).ExecuteAsync(session => BoardMapper.ToDto(session.Match), cancellationToken);

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

            return BoardMapper.ToMoveResult(session.Match, from, to);
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
