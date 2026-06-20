using llmmo.Api.Dtos;
using llmmo.Auth;
using llmmo.Data;
using Microsoft.EntityFrameworkCore;

namespace llmmo.Api.Endpoints;

public static class DiplomacyEndpoints
{
    public static RouteGroupBuilder MapDiplomacyEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/diplomacy/players", ListPlayers).RequireAuth();
        group.MapGet("/diplomacy/messages", ListMessages).RequireAuth();
        group.MapPost("/diplomacy/messages", SendMessage).RequireAuth();
        group.MapPost("/diplomacy/messages/{messageId:guid}/read", MarkMessageRead).RequireAuth();
        group.MapGet("/diplomacy/relations", ListRelations).RequireAuth();
        group.MapPut("/diplomacy/relations", SetRelation).RequireAuth();
        group.MapDelete("/diplomacy/relations/{toPlayerId:guid}", ClearRelation).RequireAuth();
        group.MapGet("/diplomacy/overview", GetOverview).RequireAuth();
        group.MapGet("/diplomacy/cooldowns", GetCooldowns).RequireAuth();
        return group;
    }

    private static async Task<IResult> ListPlayers(
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var players = await diplomacy.ListPlayersAsync(auth.PlayerId, cancellationToken);
        return Results.Ok(players);
    }

    private static async Task<IResult> ListMessages(
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var messages = await diplomacy.ListMessagesAsync(auth.PlayerId, cancellationToken);
        return Results.Ok(messages);
    }

    private static async Task<IResult> SendMessage(
        SendMessageRequest request,
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        try
        {
            var message = await diplomacy.SendMessageAsync(
                auth.PlayerId,
                request.ToPlayerId,
                request.Subject,
                request.Body,
                cancellationToken);

            return Results.Created($"/api/v1/diplomacy/messages/{message.Id}", message);
        }
        catch (DiplomacyCooldownException ex)
        {
            return CooldownResult(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> MarkMessageRead(
        Guid messageId,
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var message = await diplomacy.MarkMessageReadAsync(auth.PlayerId, messageId, cancellationToken);

        return message is null
            ? Results.NotFound(new { error = "Message not found." })
            : Results.Ok(message);
    }

    private static async Task<IResult> ListRelations(
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var relations = await diplomacy.ListRelationsAsync(auth.PlayerId, cancellationToken);
        return Results.Ok(relations);
    }

    private static async Task<IResult> SetRelation(
        SetRelationRequest request,
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        try
        {
            var relation = await diplomacy.SetRelationAsync(
                auth.PlayerId,
                request.ToPlayerId,
                request.Relation,
                cancellationToken);

            return Results.Ok(relation);
        }
        catch (DiplomacyCooldownException ex)
        {
            return CooldownResult(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> ClearRelation(
        Guid toPlayerId,
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;

        try
        {
            await diplomacy.ClearRelationAsync(auth.PlayerId, toPlayerId, cancellationToken);
            return Results.NoContent();
        }
        catch (DiplomacyCooldownException ex)
        {
            return CooldownResult(ex);
        }
        catch (InvalidOperationException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetOverview(
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var overview = await diplomacy.GetOverviewAsync(auth.PlayerId, cancellationToken);
        return Results.Ok(overview);
    }

    private static async Task<IResult> GetCooldowns(
        HttpContext httpContext,
        DiplomacyService diplomacy,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var cooldowns = await diplomacy.GetCooldownStatusAsync(auth.PlayerId, cancellationToken);
        return Results.Ok(cooldowns);
    }

    private static IResult CooldownResult(DiplomacyCooldownException ex) =>
        Results.Json(
            new
            {
                error = ex.Message,
                kind = ex.Kind,
                remainingTicks = ex.RemainingTicks,
                allowedAtTick = ex.AllowedAtTick,
            },
            statusCode: StatusCodes.Status429TooManyRequests);
}
