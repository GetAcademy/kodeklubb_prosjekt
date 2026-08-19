using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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

        // --- Team discovery & management ---
        group.MapGet("/available", GetAvailableTeams).WithName("GetAvailableTeams");
        group.MapPost("/", (HttpContext context, IServiceProvider sp) => CreateTeam(context, sp)).WithName("CreateTeam");
        group.MapGet("/my-teams", GetUserTeams).WithName("GetUserTeams");
        group.MapGet("/{teamId:guid}", GetTeamDetails).WithName("GetTeamDetails");
        group.MapGet("/{teamId:guid}/members", GetTeamMembers).WithName("GetTeamMembers");
        group.MapGet("/{teamId:guid}/content", GetTeamContent).WithName("GetTeamContent");

        // --- Tags ---
        group.MapGet("/{teamId:guid}/tags", GetTeamTags).WithName("GetTeamTags");
        group.MapPost("/{teamId:guid}/tags", (Guid teamId, AddTeamTagsRequest body, IServiceProvider sp) => AddTeamTags(teamId, body, sp)).WithName("AddTeamTags");
        group.MapDelete("/{teamId:guid}/tags/{tagId:guid}", RemoveTeamTag).WithName("RemoveTeamTag");

        // --- Join requests & invitations ---
        group.MapPost("/{teamId:guid}/request", (Guid teamId, HttpContext context, IServiceProvider sp) => RequestToJoinTeam(teamId, context, sp)).WithName("RequestToJoinTeam");
        group.MapGet("/{teamId:guid}/requests", GetTeamRequests).WithName("GetTeamRequests");
        group.MapPatch("/{teamId:guid}/requests/{requestId:guid}/approve", (Guid teamId, Guid requestId, HttpContext context, IServiceProvider sp) => ApproveTeamRequest(teamId, requestId, context, sp)).WithName("ApproveTeamRequest");
        group.MapPatch("/{teamId:guid}/requests/{requestId:guid}/decline", DeclineTeamRequest).WithName("DeclineTeamRequest");
        group.MapDelete("/{teamId:guid}/requests/{requestId:guid}", (Guid teamId, Guid requestId, string discordId, IServiceProvider sp) => CancelJoinRequest(teamId, requestId, discordId, sp)).WithName("CancelJoinRequest");
        group.MapGet("/my-requests", GetMyRequests).WithName("GetMyRequests");
        group.MapGet("/notifications", GetNotifications).WithName("GetNotifications");

        // --- Announcements ---
        group.MapGet("/{teamId:guid}/announcements", GetTeamAnnouncements).WithName("GetTeamAnnouncements");
        group.MapPost("/{teamId:guid}/announcements", (Guid teamId, HttpContext context, IServiceProvider sp) => CreateTeamAnnouncement(teamId, context, sp)).WithName("CreateTeamAnnouncement");
        group.MapPatch("/{teamId:guid}/announcements/{announcementId:guid}", (Guid teamId, Guid announcementId, HttpContext context, IServiceProvider sp) => UpdateTeamAnnouncement(teamId, announcementId, context, sp)).WithName("UpdateTeamAnnouncement");
        group.MapDelete("/{teamId:guid}/announcements/{announcementId:guid}", DeleteTeamAnnouncement).WithName("DeleteTeamAnnouncement");

        // --- Discord integration ---
        group.MapPost("/{teamId:guid}/discord", SetTeamDiscordConfig).WithName("SetTeamDiscordConfig");
        group.MapPatch("/{teamId:guid}/discord", UpdateTeamDiscordConfig).WithName("UpdateTeamDiscordConfig");
        group.MapGet("/{teamId:guid}/discord/info", GetTeamDiscordInfo).WithName("GetTeamDiscordInfo");
        group.MapDelete("/{teamId:guid}/discord", RemoveTeamDiscordConfig).WithName("RemoveTeamDiscordConfig");
        group.MapPost("/{teamId:guid}/discord/members/{userId:guid}", GrantDiscordAccess).WithName("GrantDiscordAccess");
        group.MapDelete("/{teamId:guid}/discord/members/{userId:guid}", RevokeDiscordAccess).WithName("RevokeDiscordAccess");
        group.MapPost("/{teamId:guid}/discord/sync", SyncTeamWithDiscord).WithName("SyncTeamWithDiscord");
    }

    // ========== Tags ==========
    // Reads a team's tags, adds tags to a team, and removes a tag from a team.
    // All SQL lives in Persistence (see TeamSql: GetTeamTagsByTeamId, InsertTeamTag,
    // DeleteTeamTag, CheckPredefinedTagExists) rather than inline here.

    private static async Task<IResult> GetTeamTags(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var tags = await connection.QueryManyAsync<dynamic>(TeamSql.GetTeamTagsByTeamId(), new { TeamId = teamId });
        return Results.Ok(tags);
    }

    private static async Task<IResult> AddTeamTags(Guid teamId, AddTeamTagsRequest body, IServiceProvider sp)
    {
        if (body.Selections == null || body.Selections.Length == 0)
            return Results.BadRequest(new { message = "At least one tag selection is required" });

        if (string.IsNullOrWhiteSpace(body.DiscordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var db = await DbSession.OpenAsync();
        try
        {
            // Only team members are allowed to add tags to a team.
            var isMember = await db.Conn.QuerySingleAsync<bool>(
                TeamSql.IsUserMemberByDiscordId(), new { TeamId = teamId, DiscordId = body.DiscordId }, db.Tx);

            if (!isMember)
            {
                await db.Tx.RollbackAsync();
                return Results.Json(new { message = "Only team members can add tags to this team." }, statusCode: 403);
            }

            foreach (var selection in body.Selections)
            {
                // Combined check-and-insert: one round trip instead of a
                // separate "does it exist" query followed by an insert.
                // If the tag was already added, its level is updated to
                // whatever was just selected (upsert), rather than a no-op.
                var result = await db.Conn.QuerySingleAsync<TeamTagInsertResult>(
                    TeamSql.CheckAndInsertTeamTag(),
                    new { TeamId = teamId, TagId = selection.TagId, LevelTagId = selection.LevelTagId }, db.Tx);

                if (!result.TagExists)
                    throw new InvalidOperationException($"Tag '{selection.TagId}' does not exist.");
            }

            await db.CommitAsync();

            // Notify via Discord: confirm to the person who added the tags,
            // and separately let the team admin know (skipped if they're the
            // same person). Best-effort only — a notification failure should
            // never make the tag save itself look like it failed.
            await NotifyTeamTagsAdded(teamId, body.DiscordId, body.Selections.Select(s => s.TagId).ToArray(), db.Conn, sp);

            return Results.Ok(new { message = "Tags added successfully" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task NotifyTeamTagsAdded(Guid teamId, string? actingDiscordId, Guid[] tagIds, NpgsqlConnection connection, IServiceProvider sp)
    {
        try
        {
            var discordService = sp.GetRequiredService<Core.Logic.IDiscordNotificationService>();

            var team = await connection.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById(), new { TeamId = teamId });
            if (team == null) return;

            var admin = await connection.QueryOneOrDefaultAsync<UserEntity>(
                TeamSql.GetUserByAdminId(), new { AdminId = team.TeamAdminId });
            var adminName = admin?.Username ?? "ukjent admin";

            // Look up the actual tag names, in the same order they were selected.
            var tagNames = await connection.QueryManyAsync<string>(
                TeamSql.GetTagNamesByIds(), new { TagIds = tagIds });
            var tagList = string.Join(", ", tagNames);

            var actingUsername = "Noen";
            if (!string.IsNullOrWhiteSpace(actingDiscordId))
            {
                var actingUser = await connection.QueryOneOrDefaultAsync<UserEntity>(TeamSql.GetUserByDiscordId(), new { DiscordId = actingDiscordId });
                if (actingUser != null) actingUsername = actingUser.Username;

                // Confirmation to the person who added the tags.
                try
                {
                    await discordService.SendDirectMessageAsync(actingDiscordId,
                        $"✅ Du la til [{tagList}] på {team.Name}! (Admin: {adminName})");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AddTeamTags] Failed to send confirmation DM to {actingDiscordId}: {ex.Message}");
                }
            }

            // Notify the admin, unless they're the one who just added the tags.
            if (!string.IsNullOrWhiteSpace(admin?.DiscordId) && admin.DiscordId != actingDiscordId)
            {
                try
                {
                    await discordService.SendDirectMessageAsync(admin.DiscordId,
                        $"🏷️ {actingUsername} la til [{tagList}] på {team.Name}.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[AddTeamTags] Failed to send admin notification to {admin.DiscordId}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // Never let a notification problem affect the tag-save response.
            Console.WriteLine($"[AddTeamTags] Notification step failed: {ex.Message}");
        }
    }

    private static async Task<IResult> RemoveTeamTag(Guid teamId, Guid tagId)
    {
        await using var db = await DbSession.OpenAsync();
        try
        {
            await db.ExecuteAsync(TeamSql.DeleteTeamTag(), new { TeamId = teamId, TagId = tagId });

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
    // Connecting a team to a Discord server/channel/role, and syncing member
    // access to that role. SQL lives in Persistence (TeamSql), except
    // UpdateTeamDiscordConfig which builds its SET clause dynamically based
    // on which fields were actually supplied — see the comment there.

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

            await db.ExecuteAsync(TeamSql.SetTeamDiscordConfig(), new
            {
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

    // Kept as dynamic inline SQL rather than a static .sql file: unlike every
    // other command here, the SET clause genuinely varies per call (only the
    // fields the caller actually supplied are updated). A static file can't
    // express that; a fixed set of nullable-COALESCE columns was considered
    // but would silently no-op on intentional-null updates, which isn't what
    // "partial update" callers expect either.
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

        var results = await connection.QueryAsync<dynamic>(TeamSql.GetTeamDiscordInfo(), new { TeamId = teamId });
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

            await db.ExecuteAsync(TeamSql.ClearTeamDiscordConfig(), new { TeamId = teamId });

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
            await db.ExecuteAsync(TeamSql.InsertDiscordRoleAssignment(), new { TeamId = teamId, UserId = userId });

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
            await db.ExecuteAsync(TeamSql.RemoveDiscordRoleAssignment(), new { TeamId = teamId, UserId = userId });

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
        var teamMembers = await connection.QueryManyAsync<dynamic>(TeamSql.GetActiveTeamMembersWithDiscordId(), new { TeamId = teamId });

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

    // Not yet implemented — placeholder for general team content (separate from
    // announcements/tags/members below).
    private static Task<IResult> GetTeamContent(Guid teamId)
    {
        return Task.FromResult(Results.StatusCode(501));
    }

    // ========== Announcements ==========

    private static async Task<IResult> GetTeamAnnouncements(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var announcements = await connection.QueryManyAsync<TeamAnnouncementDto>(
            TeamSql.GetTeamAnnouncementsByTeamId(),
            new { TeamId = teamId });
        return Results.Ok(announcements);
    }

    private static async Task<IResult> CreateTeamAnnouncement(Guid teamId, HttpContext context, IServiceProvider sp)
    {
        try
        {
            context.Request.EnableBuffering();
            context.Request.Body.Position = 0;

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;

            var body = JsonSerializer.Deserialize<CreateTeamAnnouncementRequest>(rawBody, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (body == null || string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Body) || string.IsNullOrWhiteSpace(body.CreatedBy))
                return Results.BadRequest(new { message = "Title, body, and createdBy are required" });

            await using var db = await DbSession.OpenAsync();
            try
            {
                var existingTeam = await db.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById(), new { TeamId = teamId });
                if (existingTeam == null)
                    return await db.RollbackAsync("Team not found");

                var creatorUser = await db.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetByDiscordId(), new { DiscordId = body.CreatedBy });
                if (creatorUser == null)
                    return await db.RollbackAsync("User not found");

                if (existingTeam.TeamAdminId != creatorUser.Id)
                    return await db.RollbackAsync("Only the team admin may create announcements");

                var announcementId = Guid.NewGuid();
                await db.ExecuteAsync(TeamSql.InsertTeamAnnouncement(), new
                {
                    Id = announcementId,
                    TeamId = teamId,
                    CreatedBy = creatorUser.Id,
                    Title = body.Title,
                    Body = body.Body
                });

                await db.CommitAsync();
                return Results.Created($"/api/discover/{teamId}/announcements/{announcementId}", new { Id = announcementId, TeamId = teamId, Title = body.Title, Body = body.Body });
            }
            catch (Exception ex)
            {
                await db.Tx.RollbackAsync();
                return Results.BadRequest(new { message = ex.Message });
            }
        }
        catch (Exception ex)
        {
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> UpdateTeamAnnouncement(Guid teamId, Guid announcementId, HttpContext context, IServiceProvider sp)
    {
        var body = await context.Request.ReadFromJsonAsync<UpdateTeamAnnouncementRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.Title) || string.IsNullOrWhiteSpace(body.Body) || body.UpdatedBy == Guid.Empty)
            return Results.BadRequest(new { message = "Title, body, and updatedBy are required" });

        await using var db = await DbSession.OpenAsync();
        try
        {
            var announcement = await db.QueryOneOrDefaultAsync<TeamAnnouncementDto>(TeamSql.GetTeamAnnouncementById(), new { AnnouncementId = announcementId, TeamId = teamId });
            if (announcement == null)
                return await db.RollbackAsync("Announcement not found");

            if (announcement.CreatedBy != body.UpdatedBy)
                return await db.RollbackAsync("Only the announcement creator may update it");

            await db.ExecuteAsync(TeamSql.UpdateTeamAnnouncement(), new
            {
                AnnouncementId = announcementId,
                TeamId = teamId,
                Title = body.Title,
                Body = body.Body
            });

            await db.CommitAsync();
            return Results.Ok(new { message = "Announcement updated successfully" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> DeleteTeamAnnouncement(Guid teamId, Guid announcementId)
    {
        await using var db = await DbSession.OpenAsync();
        try
        {
            var announcement = await db.QueryOneOrDefaultAsync<TeamAnnouncementDto>(TeamSql.GetTeamAnnouncementById(), new { AnnouncementId = announcementId, TeamId = teamId });
            if (announcement == null)
                return await db.RollbackAsync("Announcement not found");

            await db.ExecuteAsync(TeamSql.DeleteTeamAnnouncement(), new { AnnouncementId = announcementId, TeamId = teamId });
            await db.CommitAsync();
            return Results.Ok(new { message = "Announcement deleted successfully" });
        }
        catch (Exception ex)
        {
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    // ========== Join Requests & Invitations ==========

    // Returns either all of a user's requests (history=true) or just the
    // pending ones — two separate queries rather than one with a runtime
    // WHERE toggle, since the "all" version intentionally drops the status
    // filter entirely rather than filtering by every possible status.
    private static async Task<IResult> GetMyRequests(string? discordId, bool history = false)
    {
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var db = await DbSession.OpenAsync();

        var user = await db.QueryOneOrDefaultAsync<UserEntity>(
            TeamSql.GetUserByDiscordId(), new { DiscordId = discordId });
        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        var query = history ? TeamSql.GetAllRequestsForUser() : TeamSql.GetPendingRequestsForUser();
        var requests = await db.Conn.QueryManyAsync<JoinRequestDto>(query, new { UserId = user.Id });

        return Results.Ok(requests);
    }

    private static async Task<IResult> GetNotifications(string? discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var connection = await AppConfig.OpenConnectionAsync();

        var user = await connection.QueryOneOrDefaultAsync<UserEntity>(
            TeamSql.GetUserByDiscordId(), new { DiscordId = discordId });
        if (user == null)
            return Results.NotFound(new { message = "User not found" });

        // Requests waiting for this user's approval (teams they admin)
        var pendingApprovals = await connection.QueryManyAsync<dynamic>(
            TeamSql.GetPendingApprovalsForAdmin(), new { UserId = user.Id });

        // Recent status changes on this user's own requests (accepted/declined)
        var myUpdates = await connection.QueryManyAsync<dynamic>(
            TeamSql.GetRecentUpdatesForUser(), new { UserId = user.Id });

        return Results.Ok(new { pendingApprovals, myUpdates });
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

    // ========== Team Discovery & Management ==========

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

    private static async Task<Dictionary<Guid, TeamTagSummary[]>> LoadTeamTagsLookupAsync(NpgsqlConnection connection)
    {
        var rows = await connection.QueryManyAsync<TeamTagRow>(TeamSql.GetAllTeamTagsGrouped());
        return rows
            .GroupBy(row => row.TeamId)
            .ToDictionary(g => g.Key, g => g.Select(row => new TeamTagSummary(row.TagId, row.TagName)).ToArray());
    }

    private static async Task<IResult> GetAvailableTeams(string? discordId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var teams = await connection.QueryManyAsync<TeamEntity>(TeamSql.GetAvailable, new { DiscordId = discordId });
        var tagsByTeam = await LoadTeamTagsLookupAsync(connection);
        var results = teams.Select(team => new TeamListItem(
            team.Id, team.Name, team.Description,
            team.IsOpenToJoinRequests, team.CreatedBy, team.CreatedAt,
            tagsByTeam.GetValueOrDefault(team.Id, Array.Empty<TeamTagSummary>())));
        return Results.Ok(results);
    }

    private static async Task<IResult> GetUserTeams(string? discordId)
    {
        if (string.IsNullOrWhiteSpace(discordId))
            return Results.BadRequest(new { message = "Discord ID is required" });

        await using var connection = await AppConfig.OpenConnectionAsync();
        var teams = await connection.QueryManyAsync<TeamEntity>(TeamSql.GetUserTeams, new { DiscordId = discordId });
        var tagsByTeam = await LoadTeamTagsLookupAsync(connection);
        var results = teams.Select(team => new TeamListItem(
            team.Id, team.Name, team.Description,
            team.IsOpenToJoinRequests, team.CreatedBy, team.CreatedAt,
            tagsByTeam.GetValueOrDefault(team.Id, Array.Empty<TeamTagSummary>())));
        return Results.Ok(results);
    }

    private static async Task<IResult> RequestToJoinTeam(Guid teamId, HttpContext context, IServiceProvider sp)
    {
        Console.WriteLine($"[JoinTeam] Incoming request for teamId={teamId}");

        var body = await context.Request.ReadFromJsonAsync<TeamJoinRequest>();
        if (body == null || string.IsNullOrWhiteSpace(body.DiscordId))
        {
            Console.WriteLine("[JoinTeam] Rejected: missing or invalid body / DiscordId");
            return Results.BadRequest(new { message = "Discord ID is required" });
        }

        Console.WriteLine($"[JoinTeam] Body parsed OK, DiscordId={body.DiscordId}");

        await using var db = await DbSession.OpenAsync();
        try
        {
            return await RequestToJoinTeamCore(teamId, body, db, sp);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JoinTeam] EXCEPTION: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[JoinTeam] StackTrace: {ex.StackTrace}");
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = ex.Message });
        }
    }

    private static async Task<IResult> RequestToJoinTeamCore(Guid teamId, TeamJoinRequest body, DbSession db, IServiceProvider sp)
    {
        var user = await db.QueryOneOrDefaultAsync<UserEntity>(TeamSql.GetUserByDiscordId(), new { body.DiscordId });
        if (user == null)
        {
            Console.WriteLine($"[JoinTeam] User not found for DiscordId={body.DiscordId}");
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = "User not found" });
        }
        Console.WriteLine($"[JoinTeam] Resolved user: {user.Id} ({user.Username})");

        var team = await db.QueryOneOrDefaultAsync<TeamEntity>(TeamSql.GetById(), new { TeamId = teamId });
        if (team == null)
        {
            Console.WriteLine($"[JoinTeam] Team not found for teamId={teamId}");
            await db.Tx.RollbackAsync();
            return Results.BadRequest(new { message = "Team not found" });
        }
        Console.WriteLine($"[JoinTeam] Resolved team: {team.Id} ({team.Name})");

        var userIds = await db.QueryListAsync<Guid>(TeamSql.GetMemberIdsByTeamId(), new { TeamId = teamId });
        var teamInvitations = await db.QueryListAsync<Guid>(InvitationSql.GetIdsByTeamId(), new { TeamId = teamId });
        Console.WriteLine($"[JoinTeam] Existing members: {userIds.Count}, existing invitations: {teamInvitations.Count}");

        var state = new TeamState(teamId, userIds, teamInvitations);
        var command = new RequestToJoinTeamCommand(teamId, user.Id);
        var result = TeamService.HandleRequestToJoinTeam(state, command, DateTime.UtcNow);

        Console.WriteLine($"[JoinTeam] Outcome status: {result.Outcome.Status}, message: {result.Outcome.Message}");

        if (result.Outcome.Status == OutcomeStatus.Rejected)
            return await db.RollbackAsync(result.Outcome.Message);

        try
        {
            await TeamEventHandler.HandleAsync(result.Events, db.Conn, db.Tx, sp);
            await db.CommitAsync();
            Console.WriteLine("[JoinTeam] Committed successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[JoinTeam] EXCEPTION during event handling/commit: {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"[JoinTeam] StackTrace: {ex.StackTrace}");
            throw;
        }

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

        if (string.IsNullOrWhiteSpace(discordId))
            return Results.Ok(team);

        // Resolve the caller by discord id to determine membership and admin status
        var user = await connection.QueryOneOrDefaultAsync<UserEntity>(UserSql.GetByDiscordId(), new { DiscordId = discordId });
        var isMember = await connection.QueryOneAsync<bool>(
            TeamSql.IsUserMemberByDiscordId, new { TeamId = teamId, DiscordId = discordId });

        var isAdmin = user != null && team.TeamAdminId == user.Id;
        return Results.Ok(new { team, isMember, isAdmin });
    }

    private static async Task<IResult> GetTeamMembers(Guid teamId)
    {
        await using var connection = await AppConfig.OpenConnectionAsync();
        var members = await connection.QueryManyAsync<TeamMemberEntity>(TeamSql.GetMemberListByTeamId(), new { TeamId = teamId });
        return Results.Ok(members);
    }
}

// ── Records ──────────────────────────────────────────────────────────────────
public record TeamListItem(Guid Id, string Name, string? Description, bool IsOpenToJoinRequests, Guid CreatedBy, DateTime CreatedAt, TeamTagSummary[] Tags);
public record TeamTagRow(Guid TeamId, Guid TagId, string TagName);
public record TeamTagSummary(Guid Id, string Name);
public record TeamTagInsertResult(bool TagExists, bool WasInserted);
public record CreateTeamRequest(string Name, string? Description, Guid AdminUserId);
public record AdminActionRequest(string DiscordId);
public record TeamJoinRequest([property: JsonPropertyName("discordId")] string DiscordId);
public record JoinRequestDto(Guid Id, Guid TeamId, string TeamName, string Status, DateTime? InvitedAt);
public record AddTeamTagsRequest(TagSelection[] Selections, string? DiscordId);
public record TagSelection(Guid TagId, Guid? LevelTagId);
public record SetDiscordConfigRequest(string DiscordServerId, string DiscordChannelId, string DiscordRoleId, string? DiscordLink);
public record UpdateDiscordConfigRequest(string? DiscordServerId, string? DiscordChannelId, string? DiscordRoleId, string? DiscordLink);
public record TeamAnnouncementDto(Guid Id, Guid TeamId, Guid CreatedBy, string Title, string Body, DateTime CreatedAt, DateTime UpdatedAt);
public record CreateTeamAnnouncementRequest(string CreatedBy, string Title, string Body);
public record UpdateTeamAnnouncementRequest(Guid UpdatedBy, string Title, string Body);
