using Api.Endpoints.Teams;

namespace Api.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/discover").WithName("Teams");

        group.MapPost("/", CreateTeamEndpoint.CreateTeam).WithName("CreateTeam");
        group.MapGet("/my-teams", (Delegate)Users.Users.GetUserTeams).WithName("GetUserTeams");
        group.MapGet("/available", GetAvailableTeamsEndpoint.GetAvailableTeams).WithName("GetAvailableTeams");
        group.MapGet("/{teamId}", GetTeamDetailsEndpoint.GetTeamDetails).WithName("GetTeamDetails");
        group.MapGet("/{teamId}/content", GetTeamContentEndpoint.GetTeamContent).WithName("GetTeamContent");
        group.MapPost("/{teamId}/request", RequestToJoinTeamEndpoint.RequestToJoinTeam).WithName("RequestToJoinTeam");
        group.MapGet("/{teamId}/requests", GetTeamRequestsEndpoint.GetTeamRequests).WithName("GetTeamRequests");
        group.MapPatch("/{teamId}/requests/{requestId}/approve", ApproveTeamRequestEndpoint.ApproveTeamRequest).WithName("ApproveTeamRequest");
        group.MapPatch("/{teamId}/requests/{requestId}/decline", DeclineTeamRequestEndpoint.DeclineTeamRequest).WithName("DeclineTeamRequest");
    }


    public record TeamListItem(
        Guid Id,
        string Name,
        string? Description,
        bool IsOpenToJoinRequests,
        Guid CreatedBy,
        DateTime CreatedAt,
        string[] Tags
    );

    public record CreateTeamRequest(
        string Name,
        string? Description,
        Guid AdminUserId
    );

    public record AdminActionRequest(
        string DiscordId
    );
}