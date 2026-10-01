namespace chess_console.Core.Matches;

// Guarda as partidas vivas. Substitui o ChessMatch único e estático do Program.cs:
// cada partida passa a ser endereçada por um Guid.
public interface IMatchRepository
{
    int Count { get; }

    GameSession Create();

    GameSession? Get(Guid matchId);

    // Usado no OnDisconnectedAsync do Hub, para saber de qual partida a conexão saiu.
    GameSession? FindByConnection(string? connectionId);

    IReadOnlyCollection<GameSession> GetWaitingForOpponent();

    bool Remove(Guid matchId);
}
