using System.Threading.RateLimiting;
using Microsoft.AspNetCore.SignalR;

namespace chess_console.Server.Filters;

public sealed class RateLimitingHubFilter : IHubFilter, IAsyncDisposable
{
    private readonly PartitionedRateLimiter<string> _limiter;

    public RateLimitingHubFilter(IConfiguration configuration)
    {
        int permitLimit = GetPositiveSetting(
            configuration,
            "RateLimiting:SignalR:InvocationPermitLimit",
            40);
        int windowSeconds = GetPositiveSetting(
            configuration,
            "RateLimiting:SignalR:InvocationWindowSeconds",
            10);
        int segmentsPerWindow = GetPositiveSetting(
            configuration,
            "RateLimiting:SignalR:InvocationSegmentsPerWindow",
            2);

        _limiter = PartitionedRateLimiter.Create<string, string>(
            partitionKey => RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey,
                _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
                    Window = TimeSpan.FromSeconds(windowSeconds),
                    SegmentsPerWindow = segmentsPerWindow,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                }));
    }

    public async ValueTask<object?> InvokeMethodAsync(
        HubInvocationContext invocationContext,
        Func<HubInvocationContext, ValueTask<object?>> next)
    {
        string clientIp = invocationContext.Context.GetHttpContext()?.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        using RateLimitLease lease = await _limiter.AcquireAsync(clientIp, 1);
        if (!lease.IsAcquired)
            throw new HubException("Rate limit exceeded. Try again shortly.");

        return await next(invocationContext);
    }

    public ValueTask DisposeAsync() => _limiter.DisposeAsync();

    private static int GetPositiveSetting(
        IConfiguration configuration,
        string key,
        int defaultValue)
    {
        int value = configuration.GetValue(key, defaultValue);
        if (value <= 0)
            throw new InvalidOperationException($"Configuration '{key}' must be greater than zero.");

        return value;
    }
}
