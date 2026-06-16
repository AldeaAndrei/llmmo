using System.Text.Json;
using llmmo.Api;
using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class ReportEndpoints
{
    public static RouteGroupBuilder MapReportEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/reports", ListReports).RequireAuth();
        group.MapGet("/reports/{reportId:guid}", GetReport).RequireAuth();
        group.MapPost("/reports/{reportId:guid}/read", MarkReportRead).RequireAuth();
        return group;
    }

    private static async Task<IResult> ListReports(
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        var reports = await db.Reports.AsNoTracking()
            .Where(report => report.PlayerId == auth.PlayerId)
            .OrderByDescending(report => report.CreatedAt)
            .ToListAsync(cancellationToken);

        return Results.Ok(reports.Select(ToDto));
    }

    private static async Task<IResult> GetReport(
        Guid reportId,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        var report = await db.Reports.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == reportId && r.PlayerId == auth.PlayerId, cancellationToken);

        if (report is null)
        {
            return Results.NotFound(new { error = "Report not found." });
        }

        return Results.Ok(ToDto(report));
    }

    private static async Task<IResult> MarkReportRead(
        Guid reportId,
        HttpContext httpContext,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        var report = await db.Reports
            .FirstOrDefaultAsync(r => r.Id == reportId && r.PlayerId == auth.PlayerId, cancellationToken);

        if (report is null)
        {
            return Results.NotFound(new { error = "Report not found." });
        }

        if (report.ReadAt is null)
        {
            report.ReadAt = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }

        return Results.Ok(ToDto(report));
    }

    private static ReportDto ToDto(Entities.Report report) => new(
        report.Id,
        report.Type,
        report.AttackId,
        report.SourceCityId,
        report.TargetCityId,
        report.TargetX,
        report.TargetY,
        ReportPayloadHelper.Deserialize(report.Payload),
        report.CreatedAt,
        report.ReadAt);
}
