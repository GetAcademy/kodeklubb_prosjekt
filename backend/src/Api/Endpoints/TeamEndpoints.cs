using Api;
using Persistence;
using Core.Commands;
using Core.Logic;
using Core.Outcomes;
using Core.DomainEvents;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Dapper;
using System.Text.Json;
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

            // 1. Start connection and transaction
            await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 2. Read state - check if admin user exists
                var userExistsSql = SqlLoader.Load("Queries/Users_CheckExists.sql");
                var userExists = await connection.QuerySingleAsync<bool>(
                    userExistsSql,
                    new { Id = body.AdminUserId },
                    transaction);

                if (!userExists)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "Admin user not found" });
                }

                // 3. Call Core with Command
                var teamId = Guid.NewGuid();
                var command = new CreateTeamCommand(teamId, body.Name, body.Description, body.AdminUserId);
                var (outcome, events) = TeamService.Handle(command, DateTime.UtcNow);

                // 4. Check outcome
                if (outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = outcome.Message });
                }

                // 5. Persist state based on domain events
                foreach (var evt in events)
                {
                    if (evt is TeamCreated tc)
                    {
                        // Insert team
                        var createTeamSql = SqlLoader.Load("Commands/Teams_Create.sql");
                        await connection.ExecuteAsync(
                            createTeamSql,
                            new
                            {
                                Id = tc.TeamId,
                                Name = tc.Name,
                                Description = tc.Description,
                                AdminUserId = tc.AdminUserId
                            },
                            transaction);

                        // Insert team member (admin)
                        var insertMemberSql = SqlLoader.Load("Commands/TeamMembers_Insert.sql");
                        await connection.ExecuteAsync(
                            insertMemberSql,
                            new
                            {
                                TeamId = tc.TeamId,
                                UserId = tc.AdminUserId,
                                Role = "admin"
                            },
                            transaction);

                        // 6. Write to EventLog
                        var eventLogSql = SqlLoader.Load("Outbox/EventLog_Insert.sql");
                        await connection.ExecuteAsync(
                            eventLogSql,
                            new
                            {
                                EventType = nameof(TeamCreated),
                                Payload = JsonSerializer.Serialize(tc),
                                OccurredAt = tc.OccurredAt
                            },
                            transaction);

                        // 7. Write to Outbox
                        var outboxSql = SqlLoader.Load("Outbox/Outbox_Insert.sql");
                        await connection.ExecuteAsync(
                            outboxSql,
                            new
                            {
                                EventType = nameof(TeamCreated),
                                Payload = JsonSerializer.Serialize(tc)
                            },
                            transaction);
                    }
                }

                // 8. Commit transaction
                await transaction.CommitAsync();

                return Results.Created($"/api/discover/{teamId}", new
                {
                    teamId,
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
        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();
        
        var sql = SqlLoader.Load("Queries/Teams_GetAvailable.sql");
        var teams = await connection.QueryAsync<Persistence.DbModels.TeamEntity>(
            sql, 
            new { DiscordId = discordId });

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

        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();
        
        var sql = SqlLoader.Load("Queries/Teams_GetUserTeams.sql");
        var teams = await connection.QueryAsync<Persistence.DbModels.TeamEntity>(
            sql,
            new { DiscordId = discordId });

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
        // TODO: Refactor to use Dapper + Core pattern
        /*
         * var result = TeamResult.Handle()
         * if (result.Outcome == OutcomeStatus.Accepted) {
         *      _db.ExecuteAsync();
         * }
         */
        // var body = await context.Request.ReadFromJsonAsync<JoinRequestDto>();
        // if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
        // {
        //     return Results.BadRequest(new { message = "discord id is required" });
        // }
        await using var connection = new NpgsqlConnection(AppConfig.ConnectionString);
        await connection.OpenAsync();
        
        // 1) Load team row
        var teamSql = SqlLoader.Load("Queries/Team_GetById.sql");
        var teamRow = await connection.QuerySingleOrDefaultAsync<TeamEntity>(teamSql, new { Id = teamId });
        if (teamRow == null)
        {
            return Results.NotFound(new { message = "Team not found" });
        }
        // 2) Load members, requests, etc.
        var membersSql = SqlLoader.Load("Queries/TeamMembers_GetByTeamId.sql");
        var members = (await connection.QueryAsync<Guid>(membersSql, new { TeamId = teamId })).ToList();
        var requestsSql = SqlLoader.Load("Queries/Team_GetInvitations.sql");
        var requests = (await connection.QueryAsync<TeamRequestEntity>(requestsSql, new { TeamId = teamId })).ToList();
        var reqInvitations = new List<Guid>();
        foreach (var request in requests)
        {
            reqInvitations.Add(request.userId);
        }
        
        // 3) Map to Core.State.TeamState
        // var teamState = new TeamState
        // {
        //     TeamId = teamRow.Id,
        //     Members = members,
        //     PendingInvitations = reqInvitations
        // };
        // 4) Call core
        // var cmd = new RequestToJoinTeamCommand(body.DiscordId);
        var now = DateTime.Now;
        // var result = TeamService.HandleRequestToJoinTeam(teamState, cmd, now);
        //
        // if (result.Outcome.Status == OutcomeStatus.Rejected)
        // {
        //     return Results.BadRequest(result.Outcome.Message);
        // }
        
        
        // 5) Persist changes in a transaction (write request row + outbox + bump version)
        await using var tx = await connection.BeginTransactionAsync();

        // Results.Ok(new { message = result.Outcome.Message, status = result.Outcome.Status });

        // var sql = SqlLoader.Load("/Queries/Teams_GetAvailable.sql");
        // var teams = await connection.QueryAsync<Persistence.DbModels.TeamEntity>(sql);
        // var selectedTeam = teams.Where(team => team.Id == teamId);
        //
        // var status = TeamService.HandleRequestToJoinTeam(new TeamState());
        return Results.Ok();
    }

    private static Task<IResult> GetTeamRequests(Guid teamId)
    {
        // TODO: Refactor to use Dapper + Core pattern
        return Task.FromResult(Results.StatusCode(501));
    }

    private static Task<IResult> ApproveTeamRequest(Guid teamId, Guid requestId, HttpContext context)
    {
        // TODO: Refactor to use Dapper + Core pattern
        return Task.FromResult(Results.StatusCode(501));
    }

    private static Task<IResult> DeclineTeamRequest(Guid teamId, Guid requestId, HttpContext context)
    {
        // TODO: Refactor to use Dapper + Core pattern
        return Task.FromResult(Results.StatusCode(501));
    }

    private static Task<IResult> GetTeamDetails(Guid teamId)
    {
        // TODO: Refactor to use Dapper + Core pattern
        return Task.FromResult(Results.StatusCode(501));
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

