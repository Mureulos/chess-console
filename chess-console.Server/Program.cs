using chess_console.Core.Matches;
using chess_console.Server.Hubs;
using chess_console.Server.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

// Monolito: as partidas vivem no mesmo processo que serve o Hub.
builder.Services.AddSingleton<IMatchRepository, InMemoryMatchRepository>();
builder.Services.AddSingleton<MatchService>();

WebApplication app = builder.Build();

app.MapHub<ChessHub>("/hubs/chess");

app.Run();

// Necessário para o WebApplicationFactory dos testes de integração enxergar o host.
public partial class Program;
