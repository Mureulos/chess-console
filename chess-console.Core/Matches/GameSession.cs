using System.Runtime.CompilerServices;
using board;
using chess;

namespace chess_console.Core.Matches;

// Uma partida isolada: o ChessMatch em si, quem ocupa cada cor e o lock que
// serializa as operações que chegam dos dois jogadores ao mesmo tempo.
public sealed class GameSession
{
    // SemaphoreSlim só precisa de Dispose quando AvailableWaitHandle é usado — aqui
    // não é. Não descartar evita que remover a partida do repositório quebre uma
    // operação que ainda está em voo segurando o lock.
    private readonly SemaphoreSlim _gate = new(1, 1);

    public GameSession() : this(Guid.NewGuid(), new ChessMatch())
    {
    }

    public GameSession(Guid id, ChessMatch match)
    {
        ArgumentNullException.ThrowIfNull(match);

        Id = id;
        Match = match;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; }

    // Só deve ser lido/alterado de dentro de ExecuteAsync.
    public ChessMatch Match { get; }

    public DateTime CreatedAtUtc { get; }

    public string? WhiteConnectionId { get; private set; }

    public string? BlackConnectionId { get; private set; }

    public bool IsEmpty => WhiteConnectionId is null && BlackConnectionId is null;

    public bool IsFull => WhiteConnectionId is not null && BlackConnectionId is not null;

    public bool IsWaitingForOpponent => !IsEmpty && !IsFull;

    public bool IsFinished => Match.completed;

    public Color? ColorOf(string? connectionId)
    {
        if (string.IsNullOrEmpty(connectionId))
            return null;

        if (connectionId == WhiteConnectionId)
            return Color.White;

        if (connectionId == BlackConnectionId)
            return Color.Black;

        return null;
    }

    public bool IsSeated(string? connectionId) => ColorOf(connectionId) is not null;

    public bool IsTurnOf(string? connectionId) => ColorOf(connectionId) == Match.actualPlayerColor;

    // Senta a conexão na primeira cor livre (branco, depois preto). É idempotente:
    // se a conexão já está na partida, devolve a cor que ela já ocupa.
    public bool TryTakeSeat(string connectionId, out Color color)
    {
        EnsureLocked();
        ArgumentException.ThrowIfNullOrEmpty(connectionId);

        Color? seated = ColorOf(connectionId);
        if (seated is not null)
        {
            color = seated.Value;
            return true;
        }

        if (WhiteConnectionId is null)
        {
            WhiteConnectionId = connectionId;
            color = Color.White;
            return true;
        }

        if (BlackConnectionId is null)
        {
            BlackConnectionId = connectionId;
            color = Color.Black;
            return true;
        }

        color = default;
        return false;
    }

    // Libera a cadeira da conexão (desconexão). A partida continua existindo para
    // que o jogador possa reentrar ocupando a cor vaga.
    public bool ReleaseSeat(string? connectionId)
    {
        EnsureLocked();

        Color? seated = ColorOf(connectionId);
        if (seated is null)
            return false;

        if (seated == Color.White)
            WhiteConnectionId = null;
        else
            BlackConnectionId = null;

        return true;
    }

    // Único ponto de entrada para mexer na partida: garante que branco e preto
    // nunca executem em cima do mesmo ChessMatch ao mesmo tempo.
    public async Task<T> ExecuteAsync<T>(Func<GameSession, T> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            return operation(this);
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task ExecuteAsync(Action<GameSession> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return ExecuteAsync<object?>(session =>
        {
            operation(session);
            return null;
        }, cancellationToken);
    }

    // Rede de proteção para quem esquecer do ExecuteAsync. Não é uma garantia forte
    // (só diz que alguém segura o lock, não que é o chamador atual), mas pega o erro
    // mais comum já no primeiro teste.
    private void EnsureLocked([CallerMemberName] string? member = null)
    {
        if (_gate.CurrentCount != 0)
            throw new InvalidOperationException(
                $"{member} must be called inside {nameof(ExecuteAsync)} so the session stays thread-safe.");
    }
}
