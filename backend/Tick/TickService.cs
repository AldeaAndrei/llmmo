using llmmo.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace llmmo.Tick;

public class TickService
{
    private const long AdvisoryLockKey = 8675309;

    private readonly AppDbContext _db;
    private readonly ActionCompleter _actionCompleter;
    private readonly ProductionService _productionService;
    private readonly TickOptions _options;
    private readonly ILogger<TickService> _logger;

    public TickService(
        AppDbContext db,
        ActionCompleter actionCompleter,
        ProductionService productionService,
        IOptions<TickOptions> options,
        ILogger<TickService> logger)
    {
        _db = db;
        _actionCompleter = actionCompleter;
        _productionService = productionService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunTickAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = _db.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var lockCommand = connection.CreateCommand();
        lockCommand.CommandText = "SELECT pg_try_advisory_lock(@p0)";
        var lockParameter = lockCommand.CreateParameter();
        lockParameter.ParameterName = "p0";
        lockParameter.Value = AdvisoryLockKey;
        lockCommand.Parameters.Add(lockParameter);

        var lockResult = await lockCommand.ExecuteScalarAsync(cancellationToken);
        if (lockResult is not true and not (long)1)
        {
            _logger.LogDebug("Tick skipped; advisory lock not acquired.");
            return;
        }

        try
        {
            var worldState = await _db.WorldState.FirstOrDefaultAsync(state => state.Id == 1, cancellationToken);
            if (worldState is null)
            {
                _logger.LogWarning("Tick skipped; world state missing.");
                return;
            }

            worldState.CurrentTick += 1;
            await _actionCompleter.CompleteReadyActionsAsync(worldState.CurrentTick, cancellationToken);
            await _productionService.ApplyProductionAsync(cancellationToken);

            var utcNow = DateTime.UtcNow;
            worldState.LastTickAt = utcNow;
            worldState.UpdatedAt = utcNow;

            await _db.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Tick advanced to {Tick}.", worldState.CurrentTick);
        }
        finally
        {
            await using var unlockCommand = connection.CreateCommand();
            unlockCommand.CommandText = "SELECT pg_advisory_unlock(@p0)";
            var unlockParameter = unlockCommand.CreateParameter();
            unlockParameter.ParameterName = "p0";
            unlockParameter.Value = AdvisoryLockKey;
            unlockCommand.Parameters.Add(unlockParameter);
            await unlockCommand.ExecuteScalarAsync(cancellationToken);
        }
    }

    public TimeSpan Interval => TimeSpan.FromSeconds(Math.Max(1, _options.TickIntervalSeconds));
}
