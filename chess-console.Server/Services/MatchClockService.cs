using chess_console.Core.Dto;
using chess_console.Core.Matches;
using chess_console.Server.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace chess_console.Server.Services;

public sealed class MatchClockService : BackgroundService
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

    private readonly IMatchRepository _repository;
    private readonly IHubContext<ChessHub> _hub;

    public MatchClockService(IMatchRepository repository, IHubContext<ChessHub> hub)
    {
        _repository = repository;
        _hub = hub;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using PeriodicTimer timer = new(Tick);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            DateTime now = DateTime.UtcNow;

            foreach (GameSession session in _repository.GetAll())
            {
                BoardStateDto? state = await session.ExecuteAsync(current =>
                {
                    if (!current.UpdateClock(now))
                        return null;

                    (int whiteSeconds, int blackSeconds, bool running) =
                        current.GetClockState(now);

                    return BoardMapper.ToDto(
                        current.Match,
                        whiteSeconds,
                        blackSeconds,
                        running);
                }, stoppingToken);

                if (state is not null)
                {
                    await _hub.Clients
                        .Group(session.Id.ToString())
                        .SendAsync(ChessHub.ClockStateEvent, state, stoppingToken);
                }
            }
        }
    }
}
