using System.Text.Json.Serialization;
using Persistence;
using Core.Commands;
using Core.Logic;
using Core.Outcomes;
using Api.Endpoints.Handlers;
using Core.State;
using Persistence.DbModels;

namespace Api.Endpoints;

public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this WebApplication app)
         
    {
        var group = app.MapGroup("/api/discover").WithName("Teams");
        group.MapGet("/tags/hierarchy", async () =>
            {
                var jsonPath = Path.Combine("src", "Persistence", "tag_hierarchy.json");
                if (!File.Exists(jsonPath))
                    return Results.NotFound(new { message = "Tag hierarchy not found" });
                var json = await File.ReadAllTextAsync(jsonPath);
                return Results.Content(json, "application/json");
            }).WithName("GetTagHierarchy");
        group.MapPost("/", (HttpContext context, IServiceProvider sp) => CreateTeam(context, sp)).WithName("CreateTeam");
        group.MapGet("/my-teams", GetUserTeams).WithName("GetUserTeams");
        group.MapGet("/available", GetAvailableTeams).WithName("GetAvailableTeams");
        group.MapGet("/{teamId}", GetTeamDetails).WithName("GetTeamDetails");
        group.MapGet("/{teamId}/content", GetTeamContent).WithName("GetTeamContent");
        group.MapPost("/{teamId}/request", (Guid teamId, HttpContext context, IServiceProvider sp) => RequestToJoinTeam(teamId, context, sp)).WithName("RequestToJoinTeam");
        group.MapGet("/{teamId}/requests", GetTeamRequests).WithName("GetTeamRequests");
        group.MapPatch("/{teamId}/requests/{requestId}/approve", (Guid teamId, Guid requestId, HttpContext context, IServiceProvider sp) => ApproveTeamRequest(teamId, requestId, context, sp)).WithName("ApproveTeamRequest");
        group.MapPatch("/{teamId}/requests/{requestId}/decline", DeclineTeamRequest).WithName("DeclineTeamRequest");
        group.MapGet("/{teamId}/members", GetTeamMembers).WithName("GetTeamMembers");
    }

    private static Task<IResult> GetTeamContent(Guid teamId)
    {
        // TODO: Refactor to use Dapper + Core pattern
        return Task.FromResult(Results.StatusCode(501)); // return new Result
    }
    
    private static async Task<IResult> CreateTeam(HttpContext context, IServiceProvider sp)
    {
        var body = await context.Request.ReadFromJsonAsync<CreateTeamRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.Name) || body.AdminUserId == Guid.Empty)
        {
            return Results.BadRequest(new { message = "Team name and admin user ID are required" });
        }

        await using var db = await DbSession.OpenAsync();

        try
        {
            return await CreateTeamCore(body, db, sp);
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> CreateTeamCore(CreateTeamRequest body, DbSession db, IServiceProvider sp)
    {
        var userExists = await db.QueryOneAsync<bool>(TeamSql.CheckUserExists(), new { Id = body.AdminUserId });
        if (!userExists) return await db.RollbackAsync("Admin user not found");

        var state = new TeamState(Guid.NewGuid(), new List<Guid>(), new List<Guid>());
        var command = new CreateTeamCommand(state.TeamId, body.Name, body.Description, body.AdminUserId);
        var result = TeamService.HandleCreateTeam(state, command, DateTime.UtcNow);

        if (result.Outcome.Status == OutcomeStatus.Rejected)
            return await db.RollbackAsync(result.Outcome.Message);

        await TeamEventHandler.HandleAsync(result.Events, db.Conn, db.Tx, sp);
        await db.CommitAsync();
        return Results.Created($"/api/discover/{state.TeamId}", new
        {
            state.TeamId,
            name = body.Name,
            adminUserId = body.AdminUserId,
            message = "Team created successfully"
        });
    }

    private static async Task<IResult> GetAvailableTeams(string? discordId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var teams = await connection.QueryManyAsync<TeamEntity>(TeamSql.GetAvailable, new { DiscordId = discordId });
        var results = teams.Select(team => new TeamListItem(
            team.Id,
            team.Name,
            team.Description,
            team.IsOpenToJoinRequests,
            team.CreatedBy,
            team.CreatedAt,
            new string[0]
        ));
        return Results.Ok(results);
    }

    private static async Task<IResult> GetUserTeams(string? discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return Results.BadRequest(new { message = "Discord ID is required" });
        }
        await using var connection = await AppConfig.OpenConnectionAsync();
        var teams = await connection.QueryManyAsync<TeamEntity>(TeamSql.GetUserTeams, new { DiscordId = discordId });
        var results = teams.Select(team => new TeamListItem(
            team.Id,
            team.Name,
            team.Description,
            team.IsOpenToJoinRequests,
            team.CreatedBy,
            team.CreatedAt,
            new string[0]
        ));
        return Results.Ok(results);
    }

    private static async Task<IResult> RequestToJoinTeam(Guid teamId, HttpContext context, IServiceProvider sp)
    {
        var body = await context.Request.ReadFromJsonAsync<TeamJoinRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
        {
            return Results.BadRequest(new { message = "Discord ID is required" });
        }

        await using var db = await DbSession.OpenAsync();

        try
        {
            return await RequestToJoinTeamCore(teamId, body, db, sp);
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RequestToJoinTeamCore(Guid teamId, TeamJoinRequest body, DbSession db, IServiceProvider sp)
    {
        var user = await db.QueryOneOrDefaultAsync<UserEntity>(TeamSql.GetUserByDiscordId(), new { body.DiscordId });
        if (user == null) {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = "User not found" });
        }
        var team = await db.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById(), new { TeamId = teamId });
        if (team == null) {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = "Team not found" });
        }

        var userIds = await db.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId(), new { TeamId = teamId });
        var teamInvitations = await db.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId(), new { TeamId = teamId });

        var state = new TeamState(teamId, userIds, teamInvitations);
        var command = new RequestToJoinTeamCommand(teamId, user.Id);
        var result = TeamService.HandleRequestToJoinTeam(state, command, DateTime.UtcNow);

        if (result.Outcome.Status == OutcomeStatus.Rejected)
            return await db.RollbackAsync(result.Outcome.Message);

        await TeamEventHandler.HandleAsync(result.Events, db.Conn, db.Tx, sp);
        await db.CommitAsync();

        return Results.Ok(new
        {
            teamId,
            status = "pending",
            message = "Join request submitted successfully"
        });
    }

    private static async Task<IResult> GetTeamRequests(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var requests = await connection.QueryManyAsync(InvitationSql.GetPendingByTeam, new { TeamId = teamId });
        return Results.Ok(requests);
    }

    private static async Task<IResult> ApproveTeamRequest(Guid teamId, Guid requestId, HttpContext context, IServiceProvider sp)
    {
        var body = await context.Request.ReadFromJsonAsync<AdminActionRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
        {
            return Results.BadRequest(new { message = "Discord ID is required" });
        }

        await using var db = await DbSession.OpenAsync();

        try
        {
            return await ApproveTeamRequestCore(teamId, requestId, body, db, sp);
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { ex.Message });
        }
    }

    private static async Task<IResult> ApproveTeamRequestCore(Guid teamId, Guid requestId, AdminActionRequest body, DbSession db, IServiceProvider sp)
    {
        var adminUser = await db.QueryOneOrDefaultAsync<TeamMemberEntity>(TeamSql.GetAdminUserByTeamId(), new { TeamId = teamId });
        if (adminUser == null) return await db.RollbackAsync("Admin user not found");

        var userIds = await db.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId(), new { TeamId = teamId });
        var teamInvitations = await db.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId(), new { TeamId = teamId });

        var request = await db.QueryOneOrDefaultAsync<InvitationEntity>(InvitationSql.GetById(), new { RequestId = requestId, TeamId = teamId });
        if (request == null) return await db.RollbackAsync("Request not found");

        if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
            return await db.RollbackAsync("Request has already been processed");

        var state = new TeamState(teamId, userIds, teamInvitations);
        var command = new ApproveJoinRequestCommand(teamId, request.InvitedUserId, request.Id);
        var result = TeamService.HandleApproveRequest(state, command, DateTime.UtcNow, adminUser.Id);

        if (result.Outcome.Status == OutcomeStatus.Rejected)
            return await db.RollbackAsync(result.Outcome.Message);

        await TeamEventHandler.HandleAsync(result.Events, db.Conn, db.Tx, sp);
        await db.CommitAsync();
        return Results.Ok(new { message = "Request approved successfully" });
    }

    private static async Task<IResult> DeclineTeamRequest(Guid teamId, Guid requestId, HttpContext context, IServiceProvider sp)
    {
        var body = await context.Request.ReadFromJsonAsync<AdminActionRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
        {
            return Results.BadRequest(new { message = "Discord ID is required" });
        }

        await using var db = await DbSession.OpenAsync();

        try
        {
            return await DeclineTeamRequestCore(teamId, requestId, body, db, sp);
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { ex.Message });
        }
    }

    private static async Task<IResult> DeclineTeamRequestCore(Guid teamId, Guid requestId, AdminActionRequest body, DbSession db, IServiceProvider sp)
    {
        var adminUser = await db.QueryOneOrDefaultAsync<TeamMemberEntity>(TeamSql.GetAdminUserByTeamId(), new { TeamId = teamId });
        if (adminUser == null) return await db.RollbackAsync("User not found");

        var userIds = await db.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId(), new { TeamId = teamId });
        var teamInvitations = await db.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId(), new { TeamId = teamId });
        var request = await db.QueryOneOrDefaultAsync<InvitationEntity>(InvitationSql.GetById(), new { RequestId = requestId, TeamId = teamId });
        if (request == null) return await db.RollbackAsync("Request not found");

        if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
            return await db.RollbackAsync("Request has already been processed");

        var state = new TeamState(teamId, userIds, teamInvitations);
        var command = new DeclineJoinRequestCommand(teamId, request.InvitedUserId, request.Id, adminUser.UserId);
        var result = TeamService.HandleDeclineRequest(state, command, DateTime.UtcNow, adminUser.Id);

        if (result.Outcome.Status == OutcomeStatus.Rejected)
            return await db.RollbackAsync(result.Outcome.Message);

        await TeamEventHandler.HandleAsync(result.Events, db.Conn, db.Tx, sp);
        await db.CommitAsync();
        return Results.Ok(new { message = "Request declined successfully" });
    }

    private static async Task<IResult> GetTeamDetails(Guid teamId, string? discordId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var team = await connection.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById, new { TeamId = teamId });
        if (team == null)
        {
            return Results.NotFound(new { message = "Team not found" });
        }
        if (string.IsNullOrWhiteSpace(discordId))
        {
            return Results.Ok(team);
        }
        
        var isMember = await connection.QueryOneAsync<bool>(
            TeamSql.IsUserMemberByDiscordId,
            new { TeamId = teamId, DiscordId = discordId });
        
        return Results.Ok(new { team, isMember });
    }

    private static async Task<IResult> GetTeamMembers(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var members = await connection.QueryManyAsync<TeamMemberEntity>(TeamSql.GetMemberListByTeamId(), new { TeamId = teamId });
        return Results.Ok(members);
    }
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

public record TeamJoinRequest(
    [property: JsonPropertyName("discordId")] string DiscordId
);