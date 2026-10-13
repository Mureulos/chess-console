using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace chess_console.Server.Services;

public sealed class PlayerSessionService
{
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromMinutes(25);
    private readonly ConcurrentDictionary<string, PlayerSession> _sessions = new();

    public PlayerSession Create(string name)
    {
        string displayName = NormalizeName(name);
        string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
        PlayerSession session = new(
            token,
            Guid.NewGuid().ToString("N"),
            displayName,
            DateTime.UtcNow.Add(SessionLifetime));

        _sessions[token] = session;
        return session;
    }

    public PlayerSession? Find(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return null;

        if (!_sessions.TryGetValue(token, out PlayerSession? session))
            return null;

        if (session.ExpiresAtUtc > DateTime.UtcNow)
            return session;

        _sessions.TryRemove(token, out _);
        return null;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        string normalized = string.Join(
            ' ',
            name.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

        if (normalized.Length is < 2 or > 24)
            throw new ArgumentException("Player name must contain between 2 and 24 characters.", nameof(name));

        if (normalized.Any(char.IsControl))
            throw new ArgumentException("Player name contains invalid characters.", nameof(name));

        return normalized;
    }
}

public sealed record PlayerSession(
    string Token,
    string PlayerId,
    string Name,
    DateTime ExpiresAtUtc);

public sealed record CreatePlayerSessionRequest(string Name);
