using System.Net;
using System.Text.Json;
using System.Threading.RateLimiting;
using chess_console.Core.Matches;
using chess_console.Server.Filters;
using chess_console.Server.Hubs;
using chess_console.Server.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.SignalR;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
const string CorsPolicyName = "Frontend";
string[] allowedOrigins =
    builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
string[] knownProxies =
    builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
int rateLimitPermitLimit = GetPositiveSetting(
    builder.Configuration,
    "RateLimiting:SignalR:PermitLimit",
    30);
int rateLimitWindowSeconds = GetPositiveSetting(
    builder.Configuration,
    "RateLimiting:SignalR:WindowSeconds",
    10);
int rateLimitSegments = GetPositiveSetting(
    builder.Configuration,
    "RateLimiting:SignalR:SegmentsPerWindow",
    2);

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicyName, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        }
    });
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;

    foreach (string proxy in knownProxies)
    {
        if (!IPAddress.TryParse(proxy, out IPAddress? address))
            throw new InvalidOperationException(
                $"Configuration 'ForwardedHeaders:KnownProxies' contains invalid IP '{proxy}'.");

        options.KnownProxies.Add(address);
    }
});

builder.Services
    .AddSignalR()
    // O front é JavaScript: o contrato sai em camelCase (state.currentPlayer, e não
    // state.CurrentPlayer). Há teste fixando esse formato.
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
builder.Services.AddSingleton<RateLimitingHubFilter>();
builder.Services.AddSingleton<PlayerSessionService>();
builder.Services.AddSingleton<PlayerTokenHubFilter>();
builder.Services.Configure<HubOptions>(options =>
{
    options.AddFilter<PlayerTokenHubFilter>();
    options.AddFilter<RateLimitingHubFilter>();
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
    {
        bool isProtectedRequest =
            (httpContext.Request.Path.Equals(
                 "/hubs/chess/negotiate",
                 StringComparison.OrdinalIgnoreCase)
             || httpContext.Request.Path.Equals(
                 "/api/player/session",
                 StringComparison.OrdinalIgnoreCase))
            && HttpMethods.IsPost(httpContext.Request.Method);

        if (!isProtectedRequest)
            return RateLimitPartition.GetNoLimiter("unlimited");

        return RateLimitPartition.GetSlidingWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new SlidingWindowRateLimiterOptions
            {
                PermitLimit = rateLimitPermitLimit,
                Window = TimeSpan.FromSeconds(rateLimitWindowSeconds),
                SegmentsPerWindow = rateLimitSegments,
                QueueLimit = 0,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            });
    });
});

// Monolito: as partidas vivem no mesmo processo que serve o Hub.
builder.Services.AddSingleton<IMatchRepository, InMemoryMatchRepository>();
builder.Services.AddSingleton<MatchService>();
builder.Services.AddHostedService<MatchClockService>();

WebApplication app = builder.Build();

// O mesmo processo serve o front de wwwroot e o Hub — nada de host separado.
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
    app.UseHsts();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors(CorsPolicyName);
app.UseRateLimiter();

app.MapPost("/api/player/session", (
    CreatePlayerSessionRequest request,
    PlayerSessionService sessions) =>
{
    try
    {
        PlayerSession session = sessions.Create(request.Name);
        return Results.Ok(session);
    }
    catch (ArgumentException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

app.MapHub<ChessHub>("/hubs/chess");

app.Run();

static int GetPositiveSetting(IConfiguration configuration, string key, int defaultValue)
{
    int value = configuration.GetValue(key, defaultValue);
    if (value <= 0)
        throw new InvalidOperationException($"Configuration '{key}' must be greater than zero.");

    return value;
}

// Necessário para o WebApplicationFactory dos testes de integração enxergar o host.
public partial class Program;
