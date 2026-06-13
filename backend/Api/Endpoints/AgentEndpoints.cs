using llmmo.Api.Dtos;
using llmmo.Auth;

namespace llmmo.Api.Endpoints;

public static class AgentEndpoints
{
    public static RouteGroupBuilder MapAgentEndpoints(this RouteGroupBuilder group)
    {
        var agents = group.MapGroup("/auth/agents");

        agents.MapGet("/", ListAgents).RequireHumanSession();
        agents.MapPost("/", CreateAgent).RequireHumanSession();
        agents.MapGet("/{playerId:guid}", GetAgent).RequireHumanSession();
        agents.MapPost("/{playerId:guid}/keys", ReissueKey).RequireHumanSession();
        agents.MapDelete("/{playerId:guid}/keys", RevokeKey).RequireHumanSession();
        agents.MapDelete("/{playerId:guid}", DeleteAgent).RequireHumanSession();

        return group;
    }

    private static async Task<IResult> ListAgents(
        HttpContext httpContext,
        AgentManagementService agents,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var list = await agents.ListAgentsAsync(auth.UserId, cancellationToken);
        return Results.Ok(list);
    }

    private static async Task<IResult> CreateAgent(
        CreateAgentRequest request,
        HttpContext httpContext,
        AgentManagementService agents,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var result = await agents.CreateAgentAsync(auth.UserId, request, cancellationToken);
        if (result is null)
        {
            return Results.Conflict(new { error = "Could not create agent. Name or tile may be invalid." });
        }

        return Results.Created($"/api/v1/auth/agents/{result.Value.Response.Agent.PlayerId}", result.Value.Response);
    }

    private static async Task<IResult> GetAgent(
        Guid playerId,
        HttpContext httpContext,
        AgentManagementService agents,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var agent = await agents.GetAgentAsync(auth.UserId, playerId, cancellationToken);
        return agent is null ? Results.NotFound(new { error = "Agent not found." }) : Results.Ok(agent);
    }

    private static async Task<IResult> ReissueKey(
        Guid playerId,
        HttpContext httpContext,
        AgentManagementService agents,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var result = await agents.ReissueKeyAsync(auth.UserId, playerId, cancellationToken);
        return result is null ? Results.NotFound(new { error = "Agent not found." }) : Results.Ok(result.Value.Response);
    }

    private static async Task<IResult> RevokeKey(
        Guid playerId,
        HttpContext httpContext,
        AgentManagementService agents,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var revoked = await agents.RevokeKeyAsync(auth.UserId, playerId, cancellationToken);
        return revoked ? Results.NoContent() : Results.NotFound(new { error = "Agent or active key not found." });
    }

    private static async Task<IResult> DeleteAgent(
        Guid playerId,
        HttpContext httpContext,
        AgentManagementService agents,
        CancellationToken cancellationToken)
    {
        var auth = httpContext.GetPlayerAuth()!;
        var deleted = await agents.DeleteAgentAsync(auth.UserId, playerId, cancellationToken);
        return deleted ? Results.NoContent() : Results.NotFound(new { error = "Agent not found." });
    }
}
