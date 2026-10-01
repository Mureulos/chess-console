using System.Text.Json;
using chess_console.Core.Matches;
using chess_console.Server.Hubs;
using chess_console.Server.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddSignalR()
    // O front é JavaScript: o contrato sai em camelCase (state.currentPlayer, e não
    // state.CurrentPlayer). Há teste fixando esse formato.
    .AddJsonProtocol(options =>
        options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);

// Monolito: as partidas vivem no mesmo processo que serve o Hub.
builder.Services.AddSingleton<IMatchRepository, InMemoryMatchRepository>();
builder.Services.AddSingleton<MatchService>();

WebApplication app = builder.Build();

// O mesmo processo serve o front de wwwroot e o Hub — nada de host separado.
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHub<ChessHub>("/hubs/chess");

app.Run();

// Necessário para o WebApplicationFactory dos testes de integração enxergar o host.
public partial class Program;
