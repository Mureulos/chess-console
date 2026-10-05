using board;
using chess;
using System.Runtime.CompilerServices;

namespace chess_console.Core.Matches;

// GameSession encapsula uma partida, assentos e exclusão mútua (Contém um ChessMatch e um SemaphoreSlim).
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

    /* Representa o estado completo da partida de xadrez.
     * GameSession usa esse objeto para controlar a partida entre duas conexões. */
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

    /* Tenta colocar a conexão em uma das duas cadeiras da partida
     * A primeira conexão recebe as peças brancas e a segunda recebe as peças pretas. 
     * Se a conexão já estiver sentada, o método mantém a cor que ela já possuía. 
     * Retorna false quando as duas cadeiras já estão ocupadas. */
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

    /* Remove a conexão da cadeira que ela ocupa.
     * Isso acontece, por exemplo, quando o jogador fecha o navegador ou perde a conexão.
     * Retorna true quando a conexão realmente estava sentada em uma das cadeiras. */
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

    /* Executa uma operação que precisa acessar ou alterar a sessão e retorna um resultado.
     * O semáforo permite que apenas uma operação por vez altere o estado da partida.
     * Assim, dois jogadores não conseguem executar movimentos simultaneamente sobre o mesmo tabuleiro. */
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

    /* Versão do ExecuteAsync para operações que não precisam retornar um resultado.
     * Ela reutiliza a versão genérica para aplicar a mesma proteção de exclusão mútua.*/
    public Task ExecuteAsync(Action<GameSession> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        return ExecuteAsync<object?>(session =>
        {
            operation(session);
            return null;
        }, cancellationToken);
    }

    /* Verifica se o método atual está sendo executado dentro de ExecuteAsync.
     * Os métodos que alteram a sessão só podem ser chamados com o semáforo adquirido.
     * Caso contrário, lança uma exceção para evitar acesso simultâneo e inseguro ao estado da partida. */
    private void EnsureLocked([CallerMemberName] string? member = null)
    {
        if (_gate.CurrentCount != 0)
            throw new InvalidOperationException(
                $"{member} must be called inside {nameof(ExecuteAsync)} so the session stays thread-safe.");
    }
}
