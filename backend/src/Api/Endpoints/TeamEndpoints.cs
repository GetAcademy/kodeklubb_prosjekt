using Persistence.Repositories;
using Core.Commands;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/teams").WithName("Teams").WithOpenApi();

        group.MapGet("/available", GetAvailableTeams).WithName("GetAvailableTeams").WithOpenApi();
        group.MapPost("/{teamId}/request", RequestToJoinTeam).WithName("RequestToJoinTeam").WithOpenApi();
        group.MapGet("/{teamId}/requests", GetTeamRequests).WithName("GetTeamRequests").WithOpenApi();
        group.MapPatch("/{teamId}/requests/{requestId}/approve", ApproveTeamRequest).WithName("ApproveTeamRequest").WithOpenApi();
        group.MapPatch("/{teamId}/requests/{requestId}/decline", DeclineTeamRequest).WithName("DeclineTeamRequest").WithOpenApi();
    }

    private static async Task<IResult> GetAvailableTeams(string? discordId, ITeamRepository teamRepository)
    {
        var teams = await teamRepository.GetAvailableTeamsAsync(discordId);

        var results = teams.Select(team => new TeamListItem(
            team.Id,
            team.Name,
            team.Description,
            team.CreatedBy,
            team.CreatedAt,
            team.TeamTags
                .Where(tag => tag.Tag != null)
                .Select(tag => tag.Tag!.Name)
                .Distinct()
                .ToArray()
        ));

        return Results.Ok(results);
    }

    private static async Task<IResult> JoinTeam(long teamId, JoinTeamCommand command, ITeamRepository teamRepository)
    {
        try
        {
            var joinCommand = new JoinTeamCommand(teamId, command.UserId);
            var result = await teamRepository.JoinTeamAsync(joinCommand);

            if (!result)
            {
                return Results.BadRequest(new { message = "Could not join team" });
            }

            return Results.Ok(new { message = "Successfully joined team" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RequestToJoinTeam(long teamId, HttpContext context, ITeamRepository teamRepository, IUserRepository userRepository)
    {
        try
        {
            var body = await context.Request.ReadFromJsonAsync<TeamJoinRequest>();
            
            if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
            {
                return Results.BadRequest(new { message = "Discord ID is required" });
            }

            // Find user by Discord ID
            var user = await userRepository.GetByDiscordIdAsync(body.DiscordId);
            if (user == null)
            {
                return Results.BadRequest(new { message = "User not found" });
            }

            var joinRequest = await teamRepository.RequestToJoinTeamAsync(new RequestToJoinTeamCommand(teamId, user.Id));
            if (joinRequest == null)
            {
                return Results.BadRequest(new { message = "Could not create request" });
            }

            return Results.Ok(new { message = "Request sent successfully", requestId = joinRequest.Id });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetTeamRequests(long teamId, ITeamRepository teamRepository)
    {
        try
        {
            var requests = await teamRepository.GetTeamRequestsAsync(teamId);
            return Results.Ok(requests);
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> ApproveTeamRequest(long teamId, long requestId, ITeamRepository teamRepository)
    {
        try
        {
            var result = await teamRepository.ApproveTeamRequestAsync(requestId);
            if (!result)
            {
                return Results.BadRequest(new { message = "Could not approve request" });
            }

            return Results.Ok(new { message = "Request approved" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeclineTeamRequest(long teamId, long requestId, ITeamRepository teamRepository)
    {
        try
        {
            var result = await teamRepository.DeclineTeamRequestAsync(requestId);
            if (!result)
            {
                return Results.BadRequest(new { message = "Could not decline request" });
            }

            return Results.Ok(new { message = "Request declined" });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }
}

public record TeamListItem(
    long Id,
    string Name,
    string? Description,
    long CreatedBy,
    DateTime CreatedAt,
    string[] Tags
);
