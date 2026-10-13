using chess_console.Server.Services;
using Microsoft.AspNetCore.SignalR;

namespace chess_console.Server.Filters;

public sealed class PlayerTokenHubFilter : IHubFilter
{
    public const string SessionItemKey = "PlayerSession";

    private readonly PlayerSessionService _sessions;

    public PlayerTokenHubFilter(PlayerSessionService sessions)
    {
        _sessions = sessions;
    }

    public static PlayerSession GetRequired(HubCallerContext context) =>
        context.Items.TryGetValue(SessionItemKey, out object? value)
            && value is PlayerSession session
            ? session
            : throw new HubException("Player session is missing or expired.");

    public ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        HttpRequest? request = invocationContext.Context.GetHttpContext()?.Request;
        string? token = request?.Query["access_token"];

        if (string.IsNullOrWhiteSpace(token)
            && request?.Headers.Authorization.ToString() is string authorization
            && authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            token = authorization["Bearer ".Length..].Trim();
        }

        PlayerSession? session = _sessions.Find(token);

        if (session is null)
            throw new HubException("Player session is missing or expired.");

        invocationContext.Context.Items[SessionItemKey] = session;
        return next(invocationContext);
    }
}
