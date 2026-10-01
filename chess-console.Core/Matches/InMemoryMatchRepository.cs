using System.Collections.Concurrent;

namespace chess_console.Core.Matches;

// Implementação em memória: serve para um único processo (o monolito do plano).
// Trocar por Redis/banco depois é só reimplementar esta interface.
public sealed class InMemoryMatchRepository : IMatchRepository
{
    private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();

    public int Count => _sessions.Count;

    public GameSession Create()
    {
        GameSession session = new();

        // Guid.NewGuid() não colide na prática; o retry só garante que uma partida
        // nova nunca sobrescreva uma partida viva.
        while (!_sessions.TryAdd(session.Id, session))
            session = new GameSession();

        return session;
    }

    public GameSession? Get(Guid matchId) =>
        _sessions.TryGetValue(matchId, out GameSession? session) ? session : null;

    public GameSession? FindByConnection(string? connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            return null;

        foreach (GameSession session in _sessions.Values)
        {
            if (session.IsSeated(connectionId))
                return session;
        }

        return null;
    }

    public IReadOnlyCollection<GameSession> GetWaitingForOpponent() =>
        _sessions.Values
            .Where(session => session.IsWaitingForOpponent && !session.IsFinished)
            .ToArray();

    public bool Remove(Guid matchId) => _sessions.TryRemove(matchId, out _);
}
