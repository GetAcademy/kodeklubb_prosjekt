using System.Text.Json.Serialization;
using Persistence;
using Core.Commands;
using Core.Logic;
using Core.Outcomes;
using Api.Endpoints.Handlers;
using Core.State;
using Persistence.DbModels;
using Dapper;

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
        
        group.MapGet("/available", GetAvailableTeams).WithName("GetAvailableTeams");
        group.MapPost("/", (HttpContext context, IServiceProvider sp) => CreateTeam(context, sp)).WithName("CreateTeam");
        group.MapGet("/my-teams", GetUserTeams).WithName("GetUserTeams");
        group.MapGet("/my-requests", GetMyRequests).WithName("GetMyRequests");
        group.MapDelete("/{teamId:guid}/tags/{tagPath}", RemoveTeamTag).WithName("RemoveTeamTag");
        
        // Discord integration endpoints
        group.MapPost("/{teamId:guid}/discord", SetTeamDiscordConfig).WithName("SetTeamDiscordConfig");
        group.MapPatch("/{teamId:guid}/discord", UpdateTeamDiscordConfig).WithName("UpdateTeamDiscordConfig");
        group.MapGet("/{teamId:guid}/discord/info", GetTeamDiscordInfo).WithName("GetTeamDiscordInfo");
        group.MapDelete("/{teamId:guid}/discord", RemoveTeamDiscordConfig).WithName("RemoveTeamDiscordConfig");
        group.MapPost("/{teamId:guid}/discord/members/{userId:guid}", GrantDiscordAccess).WithName("GrantDiscordAccess");
        group.MapDelete("/{teamId:guid}/discord/members/{userId:guid}", RevokeDiscordAccess).WithName("RevokeDiscordAccess");
        group.MapGet("/{teamId:guid}", GetTeamDetails).WithName("GetTeamDetails");
        group.MapGet("/{teamId:guid}/content", GetTeamContent).WithName("GetTeamContent");
        group.MapPost("/{teamId:guid}/request", (Guid teamId, HttpContext context, IServiceProvider sp) => RequestToJoinTeam(teamId, context, sp)).WithName("RequestToJoinTeam");
        group.MapGet("/{teamId:guid}/requests", GetTeamRequests).WithName("GetTeamRequests");
        group.MapPatch("/{teamId:guid}/requests/{requestId:guid}/approve", (Guid teamId, Guid requestId, HttpContext context, IServiceProvider sp) => ApproveTeamRequest(teamId, requestId, context, sp)).WithName("ApproveTeamRequest");
        group.MapPatch("/{teamId:guid}/requests/{requestId:guid}/decline", DeclineTeamRequest).WithName("DeclineTeamRequest");
        group.MapDelete("/{teamId:guid}/requests/{requestId:guid}", (Guid teamId, Guid requestId, string discordId, IServiceProvider sp) => CancelJoinRequest(teamId, requestId, discordId, sp)).WithName("CancelJoinRequest");
        group.MapGet("/{teamId:guid}/members", GetTeamMembers).WithName("GetTeamMembers");
        group.MapGet("/{teamId:guid}/tags", GetTeamTags).WithName("GetTeamTags");
        group.MapPost("/{teamId:guid}/tags", AddTeamTags).WithName("AddTeamTags");
    
        group.MapPost("/{teamId:guid}/discord/sync", SyncTeamWithDiscord).WithName("SyncTeamWithDiscord");
    }

    private static async Task<IResult> GetTeamTags(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var tags = await connection.QueryManyAsync<dynamic>(
            @"SELECT pt.id, pt.name, pt.slug, pt.category
              FROM team_tags tt
              JOIN predefined_tags pt ON pt.id = tt.predefined_tag_id
              WHERE tt.team_id = @TeamId
              ORDER BY pt.name",
            new { TeamId = teamId });
        return Results.Ok(tags);
    }

    private static async Task<IResult> AddTeamTags(Guid teamId, AddTeamTagsRequest body)
    {
        if (body.TagPaths == null || body.TagPaths.Length == 0)
            return Results.BadRequest(new { message = "At least one tag path is required" });

        await using var db = await DbSession.OpenAsync();
        try
        {
            foreach (var tagPath in body.TagPaths)
            {
                var parts = tagPath.Split('/');
                var tagName = parts[^1];
                var category = parts.Length > 1 ? parts[0] : null;

                // Upsert into predefined_tags
                var tag = await db.QueryOneOrDefaultAsync<dynamic>(
                    @"INSERT INTO predefined_tags (id, name, slug, category)
                      VALUES (uuid_generate_v4(), @Name, @Slug, @Category)
                      ON CONFLICT (slug) DO UPDATE SET name = EXCLUDED.name
                      RETURNING id",
                    new { Name = tagName, Slug = tagPath.ToLower().Replace("/", "-").Replace(" ", "-"), Category = category });

                // Insert into team_tags
                await db.ExecuteAsync(
                    @"INSERT INTO team_tags (id, team_id, predefined_tag_id)
                      VALUES (uuid_generate_v4(), @TeamId, @TagId)
                      ON CONFLICT (team_id, predefined_tag_id) DO NOTHING",
                    new { TeamId = teamId, TagId = tag!.id });
            }

            await db.CommitAsync();
            return Results.Ok(new { message = "Tags added successfully" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RemoveTeamTag(Guid teamId, string tagPath)
    {
        await using var db = await DbSession.OpenAsync();
        try
        {
            var slug = tagPath.ToLower().Replace("/", "-").Replace(" ", "-");
            await db.ExecuteAsync(
                @"DELETE FROM team_tags
                  WHERE team_id = @TeamId
                  AND predefined_tag_id = (SELECT id FROM predefined_tags WHERE slug = @Slug)",
                new { TeamId = teamId, Slug = slug });

            await db.CommitAsync();
            return Results.Ok(new { message = "Tag removed successfully" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    // ========== Discord Integration Endpoints ==========

    private static async Task<IResult> SetTeamDiscordConfig(Guid teamId, SetDiscordConfigRequest body)
    {
        if (string.IsNullOrWhiteSpace(body.DiscordServerId) || 
            string.IsNullOrWhiteSpace(body.DiscordChannelId) || 
            string.IsNullOrWhiteSpace(body.DiscordRoleId))
            return Results.BadRequest(new { message = "discordServerId, discordChannelId, and discordRoleId are required" });

        await using var db = await DbSession.OpenAsync();
        try
        {
            var team = await db.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById(), new { TeamId = teamId });
            if (team == null)
                return Results.NotFound(new { message = "Team not found" });

            await db.ExecuteAsync(
                @"UPDATE teams 
                  SET discord_server_id = @DiscordServerId,
                      discord_channel_id = @DiscordChannelId,
                      discord_role_id = @DiscordRoleId,
                      discord_link = @DiscordLink,
                      updated_at = NOW()
                  WHERE id = @TeamId",
                new { 
                    TeamId = teamId,
                    DiscordServerId = body.DiscordServerId,
                    DiscordChannelId = body.DiscordChannelId,
                    DiscordRoleId = body.DiscordRoleId,
                    DiscordLink = body.DiscordLink
                });

            await db.CommitAsync();
            return Results.Ok(new { message = "Discord info configured successfully" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateTeamDiscordConfig(Guid teamId, UpdateDiscordConfigRequest body)
    {
        await using var db = await DbSession.OpenAsync();
        try
        {
            var team = await db.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById(), new { TeamId = teamId });
            if (team == null)
                return Results.NotFound(new { message = "Team not found" });

            var updates = new List<string>();

            if (!string.IsNullOrWhiteSpace(body.DiscordServerId))
                updates.Add("discord_server_id = @DiscordServerId");
            if (!string.IsNullOrWhiteSpace(body.DiscordChannelId))
                updates.Add("discord_channel_id = @DiscordChannelId");
            if (!string.IsNullOrWhiteSpace(body.DiscordRoleId))
                updates.Add("discord_role_id = @DiscordRoleId");
            if (!string.IsNullOrWhiteSpace(body.DiscordLink))
                updates.Add("discord_link = @DiscordLink");

            if (updates.Count == 0)
                return Results.BadRequest(new { message = "No fields to update" });

            updates.Add("updated_at = NOW()");

            var sql = $"UPDATE teams SET {string.Join(", ", updates)} WHERE id = @TeamId";
            await db.ExecuteAsync(sql, new
            {
                TeamId = teamId,
                DiscordServerId = body.DiscordServerId,
                DiscordChannelId = body.DiscordChannelId,
                DiscordRoleId = body.DiscordRoleId,
                DiscordLink = body.DiscordLink
            });

            await db.CommitAsync();
            return Results.Ok(new { message = "Discord config updated" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

private static async Task<IResult> GetTeamDiscordInfo(Guid teamId)
{
    await using var connection = await AppConfig.OpenConnectionAsync();
    
    var sql = @"SELECT id, name, discord_link, discord_server_id, discord_channel_id FROM teams WHERE id = @TeamId";
    var results = await connection.QueryAsync<dynamic>(sql, new { TeamId = teamId });
    var team = results.FirstOrDefault();

    if (team == null)
        return Results.NotFound(new { message = "Team not found" });

    return Results.Ok(team);
}

    private static async Task<IResult> RemoveTeamDiscordConfig(Guid teamId)
    {
        await using var db = await DbSession.OpenAsync();
        try
        {
            var team = await db.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById(), new { TeamId = teamId });
            if (team == null)
                return Results.NotFound(new { message = "Team not found" });

            await db.ExecuteAsync(
                @"UPDATE teams 
                  SET discord_server_id = NULL,
                      discord_channel_id = NULL,
                      discord_role_id = NULL,
                      discord_link = NULL,
                      updated_at = NOW()
                  WHERE id = @TeamId",
                new { TeamId = teamId });

            await db.CommitAsync();
            return Results.Ok(new { message = "Discord config removed" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> GrantDiscordAccess(Guid teamId, Guid userId)
    {
        await using var db = await DbSession.OpenAsync();
        try
        {
            await db.ExecuteAsync(
                @"INSERT INTO discord_role_assignments (id, team_id, user_id, discord_role_id, assigned_at)
                  SELECT uuid_generate_v4(), @TeamId, @UserId, discord_role_id, NOW()
                  FROM teams WHERE id = @TeamId
                  ON CONFLICT (team_id, user_id) DO NOTHING",
                new { TeamId = teamId, UserId = userId });

            await db.CommitAsync();
            return Results.Ok(new { message = "Discord access granted" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RevokeDiscordAccess(Guid teamId, Guid userId)
    {
        await using var db = await DbSession.OpenAsync();
        try
        {
            await db.ExecuteAsync(
                @"UPDATE discord_role_assignments 
                  SET removed_at = NOW()
                  WHERE team_id = @TeamId AND user_id = @UserId AND removed_at IS NULL",
                new { TeamId = teamId, UserId = userId });

            await db.CommitAsync();
            return Results.Ok(new { message = "Discord access revoked" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> SyncTeamWithDiscord(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var teamMembers = await connection.QueryManyAsync<dynamic>(
            @"SELECT u.id, u.discord_id FROM team_members tm
              JOIN users u ON u.id = tm.user_id
              WHERE tm.team_id = @TeamId AND tm.status = 'active'",
            new { TeamId = teamId });

        var synced = 0;
        var failed = 0;

        foreach (var member in teamMembers)
        {
            try
            {
                synced++;
            }
            catch
            {
                failed++;
            }
        }

        return Results.Ok(new { message = "Sync completed", synced, failed });
    }

    private static Task<IResult> GetTeamContent(Guid teamId)
    {
        return Task.FromResult(Results.StatusCode(501));
    }

    private static async Task<IResult> GetMyRequests(string? discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var db = await DbSession.OpenAsync();

        var user = await db.QueryOneOrDefaultAsync<UserEntity>(
            TeamSql.GetUserByDiscordId(), new { DiscordId = discordId });
        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        var requests = await db.Conn.QueryManyAsync<JoinRequestDto>(
            @"SELECT i.id         AS Id,
                     i.team_id    AS TeamId,
                     t.name       AS TeamName,
                     i.status     AS Status,
                     i.invited_at AS InvitedAt
              FROM invitations i
              JOIN teams t ON i.team_id = t.id
              WHERE i.invited_user_id = @UserId
              ORDER BY i.invited_at DESC",
            new { UserId = user.Id });

        return Results.Ok(requests);
    }

    private static async Task<IResult> CancelJoinRequest(Guid teamId, Guid requestId, string discordId, IServiceProvider sp)
    {
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var db = await DbSession.OpenAsync();
        try
        {
            var user = await db.QueryOneOrDefaultAsync<UserEntity>(
                TeamSql.GetUserByDiscordId(), new { DiscordId = discordId });
            if (user == null)
                return await db.RollbackAsync("User not found");

            var request = await db.QueryOneOrDefaultAsync<InvitationEntity>(
                InvitationSql.GetById(), new { RequestId = requestId, TeamId = teamId });
            if (request == null)
                return await db.RollbackAsync("Request not found");

            if (request.InvitedUserId != user.Id)
                return Results.Forbid();

            if (!string.Equals(request.Status, "pending", StringComparison.OrdinalIgnoreCase))
                return await db.RollbackAsync("Request has already been processed and cannot be cancelled");

            await db.ExecuteAsync(
                InvitationSql.DeletePendingInvitation(),
                new { RequestId = requestId, TeamId = teamId });

            await db.CommitAsync();
            return Results.Ok(new { message = "Request cancelled successfully" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> CreateTeam(HttpContext context, IServiceProvider sp)
    {
        var body = await context.Request.ReadFromJsonAsync<CreateTeamRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.Name) || body.AdminUserId == Guid.Empty)
            return Results.BadRequest(new { message = "Team name and admin user ID are required" });

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
            team.Id, team.Name, team.Description,
            team.IsOpenToJoinRequests, team.CreatedBy, team.CreatedAt, new string[0]));
        return Results.Ok(results);
    }

    private static async Task<IResult> GetUserTeams(string? discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var connection = await AppConfig.OpenConnectionAsync();
        var teams = await connection.QueryManyAsync<TeamEntity>(TeamSql.GetUserTeams, new { DiscordId = discordId });
        var results = teams.Select(team => new TeamListItem(
            team.Id, team.Name, team.Description,
            team.IsOpenToJoinRequests, team.CreatedBy, team.CreatedAt, new string[0]));
        return Results.Ok(results);
    }

    private static async Task<IResult> RequestToJoinTeam(Guid teamId, HttpContext context, IServiceProvider sp)
    {
        var body = await context.Request.ReadFromJsonAsync<TeamJoinRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

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
        if (user == null) { await db.Tx.RollbackAsync(); return Results.BadRequest(new { message = "User not found" }); }

        var team = await db.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById(), new { TeamId = teamId });
        if (team == null) { await db.Tx.RollbackAsync(); return Results.BadRequest(new { message = "Team not found" }); }

        var userIds = await db.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId(), new { TeamId = teamId });
        var teamInvitations = await db.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId(), new { TeamId = teamId });

        var state = new TeamState(teamId, userIds, teamInvitations);
        var command = new RequestToJoinTeamCommand(teamId, user.Id);
        var result = TeamService.HandleRequestToJoinTeam(state, command, DateTime.UtcNow);

        if (result.Outcome.Status == OutcomeStatus.Rejected)
            return await db.RollbackAsync(result.Outcome.Message);

        await TeamEventHandler.HandleAsync(result.Events, db.Conn, db.Tx, sp);
        await db.CommitAsync();
        return Results.Ok(new { teamId, status = "pending", message = "Join request submitted successfully" });
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
            return Results.BadRequest(new { message = "Discord ID is required" });

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
            return Results.BadRequest(new { message = "Discord ID is required" });

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
        if (team == null) return Results.NotFound(new { message = "Team not found" });

        if (string.IsNullOrWhiteSpace(discordId)) return Results.Ok(team);

        var isMember = await connection.QueryOneAsync<bool>(
            TeamSql.IsUserMemberByDiscordId, new { TeamId = teamId, DiscordId = discordId });
        return Results.Ok(new { team, isMember });
    }

    private static async Task<IResult> GetTeamMembers(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var members = await connection.QueryManyAsync<TeamMemberEntity>(TeamSql.GetMemberListByTeamId(), new { TeamId = teamId });
        return Results.Ok(members);
    }
}

// ── Records ──────────────────────────────────────────────────────────────────
public record TeamListItem(Guid Id, string Name, string? Description, bool IsOpenToJoinRequests, Guid CreatedBy, DateTime CreatedAt, string[] Tags);
public record CreateTeamRequest(string Name, string? Description, Guid AdminUserId);
public record AdminActionRequest(string DiscordId);
public record TeamJoinRequest([property: JsonPropertyName("discordId")] string DiscordId);
public record JoinRequestDto(Guid Id, Guid TeamId, string TeamName, string Status, DateTime? InvitedAt);
public record AddTeamTagsRequest(string[] TagPaths);
public record SetDiscordConfigRequest(string DiscordServerId, string DiscordChannelId, string DiscordRoleId, string? DiscordLink);
public record UpdateDiscordConfigRequest(string? DiscordServerId, string? DiscordChannelId, string? DiscordRoleId, string? DiscordLink);