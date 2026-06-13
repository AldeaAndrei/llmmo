using llmmo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace llmmo.Tick;

public class TickBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TickBackgroundService> _logger;

    public TickBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<TickBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunStartupTickIfNeededAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var tickService = scope.ServiceProvider.GetRequiredService<TickService>();

                var worldState = await db.WorldState.AsNoTracking()
                    .FirstOrDefaultAsync(state => state.Id == 1, stoppingToken);

                if (worldState is null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var nextTickAt = worldState.LastTickAt.Add(tickService.Interval);
                var delay = nextTickAt - DateTime.UtcNow;
                if (delay > TimeSpan.Zero)
                {
                    await Task.Delay(delay, stoppingToken);
                }

                await tickService.RunTickAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tick loop error.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }

    private async Task RunStartupTickIfNeededAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var tickService = scope.ServiceProvider.GetRequiredService<TickService>();

        var worldState = await db.WorldState.AsNoTracking()
            .FirstOrDefaultAsync(state => state.Id == 1, stoppingToken);

        if (worldState?.CurrentTick == 0)
        {
            _logger.LogInformation("Running startup tick 0→1.");
            await tickService.RunTickAsync(stoppingToken);
        }
    }
}
