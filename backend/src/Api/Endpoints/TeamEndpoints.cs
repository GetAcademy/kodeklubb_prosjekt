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
using Core.Models;
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
            await using var connection = await AppConfig.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 2. Read state - check if admin user exists
                var userExists = await connection.QueryOneAsync<bool>(TeamSql.CheckUserExists, new { Id = body.AdminUserId }, transaction);

                if (!userExists)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "Admin user not found" });
                }

                // 3. Call Core with Command
                var teamState = new TeamState(
                    Guid.NewGuid(),
                    new List<Guid>(),
                    new List<Guid>()
                    );
                var command = new CreateTeamCommand(teamState.TeamId, body.Name, body.Description, body.AdminUserId);
                var result = TeamService.HandleCreateTeam(teamState, command, DateTime.UtcNow);

                // 4. Check outcome
                if (result.Outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = result.Outcome.Message });
                }

                // 5. Persist state based on domain events
                foreach (var evt in result.Events)
                {
                    if (evt is TeamCreated tc)
                    {
                        // Insert team
                        await connection.ExecuteCommandAsync(TeamSql.CreateTeam, new { Id = tc.TeamId, Name = tc.Name, Description = tc.Description, AdminUserId = tc.AdminUserId }, transaction);

                        // Insert team member (admin)
                        await connection.ExecuteCommandAsync(TeamSql.InsertTeamMember, new { TeamId = tc.TeamId, UserId = tc.AdminUserId, Role = "admin" }, transaction);

                        // 6. Write to EventLog
                        await connection.ExecuteCommandAsync(TeamSql.InsertEventLog, new { EventType = nameof(TeamCreated), OccurredAt = tc.OccurredAt }, transaction);

                        // 7. Write to Outbox
                        await connection.ExecuteCommandAsync(TeamSql.InsertOutbox, new { EventType = nameof(TeamCreated) }, transaction);
                    }
                }

                // 8. Commit transaction
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
        
        var teams = await connection.QueryManyAsync<Persistence.DbModels.TeamEntity>(TeamSql.GetAvailable, new { DiscordId = discordId });

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
        
        var teams = await connection.QueryManyAsync<Persistence.DbModels.TeamEntity>(TeamSql.GetUserTeams, new { DiscordId = discordId });

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

            // 1. Start connection and transaction
            await using var connection = await AppConfig.OpenConnectionAsync();
            await using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // 2. Read state - get user by discord ID
                var user = await connection.QueryOneOrDefaultAsync<UserEntity>(TeamSql.GetUserByDiscordId, new { body.DiscordId }, transaction);
                
                if (user == null)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "User not found" });
                }

                // Retrieve team
                var team = await connection.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById, new { TeamId = teamId }, transaction);

                if (team == null)
                {
                    await transaction.RollbackAsync();
                    return Results.NotFound(new { message = "Team not found" });
                }

                // Retrieve team members
                var userIds = await connection.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId, new { TeamId = teamId }, transaction);
                
                // Retrieve team invitations
                var teamInvitations = await connection.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId, new { TeamId = teamId }, transaction);

                var teamState = new TeamState(teamId, userIds, teamInvitations);
                var cmd = new RequestToJoinTeamCommand(teamId, user.Id);
                var result = TeamService.HandleRequestToJoinTeam(teamState, cmd, DateTime.UtcNow);

                // 4. Check outcome
                if (result.Outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = result.Outcome.Message });
                }

                // 5. Persist state based on domain events
                foreach (var evt in result.Events)
                {
                    if (evt is not UserRequestedToJoinTeam userRequest) continue;
                    var invitationId = Guid.NewGuid();
                        
                    // Insert invitation (join request stored as self-invitation)
                    await connection.ExecuteCommandAsync(InvitationSql.SendInvitation, new { Id = invitationId, TeamId = userRequest.TeamId, InvitedUserId = userRequest.UserId, InvitedBy = userRequest.UserId, Status = "pending", InvitedAt = userRequest.OccurredAt }, transaction);

                    // 6. Write to EventLog
                    await connection.ExecuteCommandAsync(TeamSql.InsertEventLog, new { EventType = nameof(UserRequestedToJoinTeam), OccurredAt = userRequest.OccurredAt }, transaction);

                    // 7. Write to Outbox
                    await connection.ExecuteCommandAsync(TeamSql.InsertOutbox, new { EventType = nameof(UserRequestedToJoinTeam) }, transaction);
                }
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }

            // 8. Commit transaction
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
                // 1. Get user that is admin
                var adminUser = await connection.QueryOneOrDefaultAsync<TeamMemberEntity>(TeamSql.GetAdminUserByTeamId, new { TeamId = teamId }, transaction);
                Console.WriteLine(adminUser);

                if (adminUser == null)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "User not found" });
                }
                
                // Retrieve team members
                var userIds = await connection.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId, new { TeamId = teamId }, transaction);
                
                // Retrieve team invitations
                var teamInvitations = await connection.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId, new { TeamId = teamId }, transaction);
                

                // 3. Get the invitation/request details
                var request = await connection.QueryOneOrDefaultAsync<InvitationEntity>(InvitationSql.GetById, new { RequestId = requestId, TeamId = teamId }, transaction);

                if (request == null)
                {
                    await transaction.RollbackAsync();
                    return Results.NotFound(new { message = "Request not found" });
                }
                
                //Burde denna flyttes til Core? Er den riktig i det hele tatt?
                if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "Request has already been processed" });
                }

                // 4. Call Core with Command
                var teamState = new TeamState(teamId, userIds, teamInvitations);
                var command = new ApproveJoinRequestCommand(teamId, request.InvitedUserId, request.Id);
                var result = TeamService.HandleApproveRequest(teamState, command, DateTime.UtcNow, adminUser.Id);

                if (result.Outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = result.Outcome.Message });
                }

                // 5. Persist state based on domain events
                foreach (var evt in result.Events)
                {
                    if (evt is JoinRequestApproved jra)
                    {
                        // Remove pending invitation after approval
                        await connection.ExecuteCommandAsync(InvitationSql.DeletePendingInvitation, new { RequestId = requestId, TeamId = teamId }, transaction);

                        // Add user to team
                        await connection.ExecuteCommandAsync(TeamSql.InsertTeamMember, new { TeamId = teamId, UserId = jra.UserId, Role = "member" }, transaction);

                        // Write to EventLog
                        await connection.ExecuteCommandAsync(TeamSql.InsertEventLog, new { EventType = nameof(JoinRequestApproved), OccurredAt = jra.OccurredAt }, transaction);

                        // Write to Outbox
                        await connection.ExecuteCommandAsync(TeamSql.InsertOutbox, new { EventType = nameof(JoinRequestApproved) }, transaction);
                    }
                }

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
                // 1. Get admin user
                var adminUser = await connection.QueryOneOrDefaultAsync<TeamMemberEntity>(TeamSql.GetAdminUserByTeamId, new { TeamId = teamId }, transaction);
                Console.WriteLine(adminUser);

                if (adminUser == null)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "User not found" });
                }
                
                // Retrieve team members
                var userIds = await connection.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId, new { TeamId = teamId }, transaction);
                
                // Retrieve team invitations
                var teamInvitations = await connection.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId, new { TeamId = teamId }, transaction);
                
                // 3. Get the invitation/request details
                var request = await connection.QueryOneOrDefaultAsync<InvitationEntity>(InvitationSql.GetById, new { RequestId = requestId, TeamId = teamId }, transaction);

                if (request == null)
                {
                    await transaction.RollbackAsync();
                    return Results.NotFound(new { message = "Request not found" });
                }
                
                //Burde denna flyttes til Core? Er den riktig i det hele tatt?
                if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = "Request has already been processed" });
                }

                // 4. Call Core with Command
                var teamState = new TeamState(teamId, userIds, teamInvitations);
                var command = new DeclineJoinRequestCommand(teamId, request.InvitedUserId, request.Id, adminUser.UserId);
                var result = TeamService.HandleDeclineRequest(teamState, command, DateTime.UtcNow, adminUser.Id);

                if (result.Outcome.Status == OutcomeStatus.Rejected)
                {
                    await transaction.RollbackAsync();
                    return Results.BadRequest(new { message = result.Outcome.Message });
                }

                // 5. Persist state based on domain events
                foreach (var evt in result.Events)
                {
                    if (evt is JoinRequestDeclined jrd)
                    {
                        // Remove pending invitation
                        await connection.ExecuteCommandAsync(InvitationSql.DeletePendingInvitation, new { RequestId = requestId, TeamId = teamId }, transaction);

                        // Write to EventLog
                        await connection.ExecuteCommandAsync(TeamSql.InsertEventLog, new { EventType = nameof(JoinRequestDeclined), OccurredAt = jrd.OccurredAt }, transaction);

                        // Write to Outbox
                        await connection.ExecuteCommandAsync(TeamSql.InsertOutbox, new { EventType = nameof(JoinRequestDeclined) }, transaction);
                    }
                }

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

        var team = await connection.QueryOneOrDefaultAsync<Persistence.DbModels.TeamEntity>(TeamSql.GetById, new { TeamId = teamId });

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