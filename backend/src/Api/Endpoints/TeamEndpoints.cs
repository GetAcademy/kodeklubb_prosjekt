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

        group.MapPost("/", CreateTeam).WithName("CreateTeam");
        group.MapGet("/my-teams", GetUserTeams).WithName("GetUserTeams");
        group.MapGet("/available", GetAvailableTeams).WithName("GetAvailableTeams");
        group.MapGet("/{teamId}", GetTeamDetails).WithName("GetTeamDetails");
        group.MapGet("/{teamId}/content", GetTeamContent).WithName("GetTeamContent");
        group.MapPost("/{teamId}/request", RequestToJoinTeam).WithName("RequestToJoinTeam");
        group.MapGet("/{teamId}/requests", GetTeamRequests).WithName("GetTeamRequests");
        group.MapPatch("/{teamId}/requests/{requestId}/approve", ApproveTeamRequest).WithName("ApproveTeamRequest");
        group.MapPatch("/{teamId}/requests/{requestId}/decline", DeclineTeamRequest).WithName("DeclineTeamRequest");
    }

    private static Task<IResult> GetTeamContent(Guid teamId)
    {
        // TODO: Refactor to use Dapper + Core pattern
        return Task.FromResult(Results.StatusCode(501)); // return new Result
    }

    private static async Task<IResult> CreateTeam(HttpContext context)
    {
        try
        {
            var body = await context.Request.ReadFromJsonAsync<CreateTeamRequest>();
            if (body == null || string.IsNullOrWhiteSpace(body.Name) || body.AdminUserId == Guid.Empty)
            {
                return Results.BadRequest(new { message = "Team name and admin user ID are required" });
            }

            await using var connection = await AppConfig.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var userExists = await connection.QueryOneAsync<bool>(TeamSql.CheckUserExists, new { Id = body.AdminUserId }, transaction);
                if (!userExists)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "Admin user not found" });
                }

                var teamState = new TeamState(
                    Guid.NewGuid(),
                    new List<Guid>(),
                    new List<Guid>()
                    );
                var command = new CreateTeamCommand(teamState.TeamId, body.Name, body.Description, body.AdminUserId);
                var result = TeamService.HandleCreateTeam(teamState, command, DateTime.UtcNow);

                if (result.Outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = result.Outcome.Message });
                }

                await TeamEventHandler.HandleAsync(result.Events, connection, transaction);
                await transaction.CommitAsync();

                return Results.Created($"/api/discover/{teamState.TeamId}", new
                {
                    teamState.TeamId,
                    name = body.Name,
                    adminUserId = body.AdminUserId,
                    message = "Team created successfully"
                });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
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

    private static async Task<IResult> RequestToJoinTeam(Guid teamId, HttpContext context)
    {
        try
        {
            var body = await context.Request.ReadFromJsonAsync<TeamJoinRequest>();
            if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
            {
                return Results.BadRequest(new { message = "Discord ID is required" });
            }

            await using var connection = await AppConfig.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var user = await connection.QueryOneOrDefaultAsync<UserEntity>(TeamSql.GetUserByDiscordId, new { body.DiscordId }, transaction);
                if (user == null)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "User not found" });
                }

                var team = await connection.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById, new { TeamId = teamId }, transaction);
                if (team == null)
                {
                    await transaction.RollbackAsync();
                    return Results.NotFound(new { message = "Team not found" });
                }

                var userIds = await connection.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId, new { TeamId = teamId }, transaction);
                var teamInvitations = await connection.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId, new { TeamId = teamId }, transaction);

                var teamState = new TeamState(teamId, userIds, teamInvitations);
                var cmd = new RequestToJoinTeamCommand(teamId, user.Id);
                var result = TeamService.HandleRequestToJoinTeam(teamState, cmd, DateTime.UtcNow);

                if (result.Outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = result.Outcome.Message });
                }

                await TeamEventHandler.HandleAsync(result.Events, connection, transaction);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            await transaction.CommitAsync();

            return Results.Ok(new
            {
                teamId,
                status = "pending",
                message = "Join request submitted successfully"
            });
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GetTeamRequests(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var requests = await connection.QueryManyAsync(InvitationSql.GetPendingByTeam, new { TeamId = teamId });
        return Results.Ok(requests);
    }

    private static async Task<IResult> ApproveTeamRequest(Guid teamId, Guid requestId, HttpContext context)
    {
        try
        {
            var body = await context.Request.ReadFromJsonAsync<AdminActionRequest>();
            if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
            {
                return Results.BadRequest(new { message = "Discord ID is required" });
            }

            await using var connection = await AppConfig.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var adminUser = await connection.QueryOneOrDefaultAsync<TeamMemberEntity>(TeamSql.GetAdminUserByTeamId, new { TeamId = teamId }, transaction);
                if (adminUser == null)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "User not found" });
                }
                
                var userIds = await connection.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId, new { TeamId = teamId }, transaction);
                var teamInvitations = await connection.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId, new { TeamId = teamId }, transaction);
                
                var request = await connection.QueryOneOrDefaultAsync<InvitationEntity>(InvitationSql.GetById, new { RequestId = requestId, TeamId = teamId }, transaction);
                if (request == null)
                {
                    await transaction.RollbackAsync();
                    return Results.NotFound(new { message = "Request not found" });
                }
                if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "Request has already been processed" });
                }

                var teamState = new TeamState(teamId, userIds, teamInvitations);
                var command = new ApproveJoinRequestCommand(teamId, request.InvitedUserId, request.Id);
                var result = TeamService.HandleApproveRequest(teamState, command, DateTime.UtcNow, adminUser.Id);

                if (result.Outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = result.Outcome.Message });
                }

                await TeamEventHandler.HandleAsync(result.Events, connection, transaction);
                await transaction.CommitAsync();
                return Results.Ok(new { message = "Request approved successfully" });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeclineTeamRequest(Guid teamId, Guid requestId, HttpContext context)
    {
        try
        {
            var body = await context.Request.ReadFromJsonAsync<AdminActionRequest>();
            if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
            {
                return Results.BadRequest(new { message = "Discord ID is required" });
            }

            await using var connection = await AppConfig.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                var adminUser = await connection.QueryOneOrDefaultAsync<TeamMemberEntity>(TeamSql.GetAdminUserByTeamId, new { TeamId = teamId }, transaction);
                if (adminUser == null)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "User not found" });
                }
                
                var userIds = await connection.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId, new { TeamId = teamId }, transaction);
                var teamInvitations = await connection.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId, new { TeamId = teamId }, transaction);
                var request = await connection.QueryOneOrDefaultAsync<InvitationEntity>(InvitationSql.GetById, new { RequestId = requestId, TeamId = teamId }, transaction);
                if (request == null)
                {
                    await transaction.RollbackAsync();
                    return Results.NotFound(new { message = "Request not found" });
                }
                if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "Request has already been processed" });
                }

                var teamState = new TeamState(teamId, userIds, teamInvitations);
                var command = new DeclineJoinRequestCommand(teamId, request.InvitedUserId, request.Id, adminUser.UserId);
                var result = TeamService.HandleDeclineRequest(teamState, command, DateTime.UtcNow, adminUser.Id);

                if (result.Outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = result.Outcome.Message });
                }

                await TeamEventHandler.HandleAsync(result.Events, connection, transaction);
                await transaction.CommitAsync();
                return Results.Ok(new { message = "Request declined successfully" });
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
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