using llmmo.Data;
using llmmo.Entities;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api;

public class IntelService
{
    private readonly AppDbContext _db;

    public IntelService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<bool> HasScoutIntelAsync(
        Guid playerId,
        Guid targetCityId,
        CancellationToken cancellationToken = default)
    {
        return await _db.Reports.AsNoTracking()
            .AnyAsync(
                report => report.PlayerId == playerId
                          && report.Type == "scout"
                          && report.TargetCityId == targetCityId,
                cancellationToken);
    }
}
