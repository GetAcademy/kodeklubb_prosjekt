using Core.DomainEvents;
using Persistence;

namespace Api.Endpoints.Handlers;

public static class TeamEventHandler
{
    public static async Task HandleAsync(
        IReadOnlyList<IDomainEvent> events,
        DbSession db,
        Core.Logic.IEmailService emailService,
        Core.Logic.IDiscordNotificationService? discordService = null)
    {
        foreach (var evt in events)
        {
            if (evt is TeamCreated created) await HandleEvent(created, db);
            if (evt is UserRequestedToJoinTeam team) await HandleEvent(team, db, emailService);
            if (evt is JoinRequestApproved approved) await HandleEvent(approved, db, emailService, discordService);
            if (evt is JoinRequestDeclined declined) await HandleEvent(declined, db);
        }
    }

    private static async Task HandleEvent(JoinRequestDeclined evt, DbSession db)
    {
        await db.ExecuteAsync(
            InvitationSql.DeclineInvitation(),
            new { RequestId = evt.RequestId, TeamId = evt.TeamId, RespondedAt = evt.OccurredAt });
        await InsertToEventLogAndOutbox(evt, db);
    }

    private static async Task HandleEvent(JoinRequestApproved evt, DbSession db, Core.Logic.IEmailService emailService, Core.Logic.IDiscordNotificationService? discordService)
    {
        await db.ExecuteAsync(
            InvitationSql.ApproveInvitation(),
            new { RequestId = evt.RequestId, TeamId = evt.TeamId, RespondedAt = evt.OccurredAt });
        await db.ExecuteAsync(
            TeamSql.InsertTeamMember(),
            new { TeamId = evt.TeamId, UserId = evt.UserId, Role = "member" });
        await InsertToEventLogAndOutbox(evt, db);

        var user = await db.QueryOneOrDefaultAsync<Persistence.DbModels.UserEntity>(
            Persistence.TeamSql.GetUserByUserId(), new { UserId = evt.UserId });

        if (user?.Email != null)
        {
            try
            {
                await emailService.SendEmailAsync(user.Email, "You have been accepted to the team!",
                    $"<h1>Congratulations!</h1><p>Your request to join the team has been approved.</p>");
            }
            catch (Exception ex)
            {
                // A failed notification email should never undo a successful
                // approval. Log and move on -- the outbox row already recorded
                // this event for later inspection/retry if needed.
                Console.WriteLine($"[TeamEventHandler] Failed to send approval email to {user.Email}: {ex.Message}");
            }
        }

        if (discordService != null && !string.IsNullOrWhiteSpace(user?.DiscordId))
        {
            try
            {
                await EnsureTeamDiscordSetupAndNotify(evt.TeamId, user!.DiscordId, db, discordService);
            }
            catch (Exception ex)
            {
                // Same principle as the email notification above -- Discord
                // setup/notification failures must never undo the approval.
                Console.WriteLine($"[TeamEventHandler] Discord setup/notify failed for team {evt.TeamId}: {ex.Message}");
            }
        }
    }

    private static async Task EnsureTeamDiscordSetupAndNotify(Guid teamId, string newMemberDiscordId, DbSession db, Core.Logic.IDiscordNotificationService discordService)
    {
        var team = await db.QueryOneOrDefaultAsync<Persistence.DbModels.TeamDiscordSetupInfo>(
            Persistence.TeamSql.GetDiscordSetupInfo(), new { TeamId = teamId });

        if (team == null || string.IsNullOrWhiteSpace(team.DiscordServerId))
            return;

        var roleId = team.DiscordTeamRoleId;
        var textChannelId = team.DiscordTextChannelId;
        var voiceChannelId = team.DiscordVoiceChannelId;

        var alreadySetUp = !string.IsNullOrWhiteSpace(roleId)
            && !string.IsNullOrWhiteSpace(textChannelId)
            && !string.IsNullOrWhiteSpace(voiceChannelId);

        if (!alreadySetUp)
        {
            roleId = await discordService.CreateRoleAsync(team.DiscordServerId, team.Name);
            textChannelId = await discordService.CreateRestrictedChannelAsync(team.DiscordServerId, team.Name, Core.Logic.DiscordChannelType.Text, roleId);
            voiceChannelId = await discordService.CreateRestrictedChannelAsync(team.DiscordServerId, team.Name, Core.Logic.DiscordChannelType.Voice, roleId);

            await db.ExecuteAsync(Persistence.TeamSql.SetDiscordChannels(), new
            {
                TeamId = teamId,
                DiscordTeamRoleId = roleId,
                DiscordTextChannelId = textChannelId,
                DiscordVoiceChannelId = voiceChannelId,
            });
        }

        await discordService.AddRoleToMemberAsync(team.DiscordServerId, newMemberDiscordId, roleId!);
        await discordService.SendChannelMessageAsync(textChannelId!, $"<@&{roleId}> -- a new member just joined the team!");
    }

    private static async Task HandleEvent(UserRequestedToJoinTeam evt, DbSession db, Core.Logic.IEmailService emailService)
    {
        var invitationId = Guid.NewGuid();
        await db.ExecuteAsync(
            InvitationSql.SendInvitation(),
            new
            {
                Id = invitationId,
                TeamId = evt.TeamId,
                InvitedUserId = evt.UserId,
                InvitedBy = evt.UserId,
                Status = "pending",
                InvitedAt = evt.OccurredAt
            });
        await InsertToEventLogAndOutbox(evt, db);

        var admin = await db.QueryOneOrDefaultAsync<Persistence.DbModels.UserEntity>(
            Persistence.TeamSql.GetAdminUserRaw(), new { TeamId = evt.TeamId });
        if (admin?.Email != null)
        {
            try
            {
                await emailService.SendEmailAsync(admin.Email, "New team join request",
                    $"<h1>New join request</h1><p>A user has requested to join your team.</p>");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TeamEventHandler] Failed to send join-request notification to {admin.Email}: {ex.Message}");
            }
        }
    }

    private static async Task HandleEvent(TeamCreated evt, DbSession db)
    {
        await db.ExecuteAsync(TeamSql.CreateTeam(),
            new
            {
                Id = evt.TeamId,
                Name = evt.Name,
                Description = evt.Description,
                AdminUserId = evt.AdminUserId
            });
        await db.ExecuteAsync(TeamSql.InsertTeamMember(),
            new
            {
                TeamId = evt.TeamId,
                UserId = evt.AdminUserId,
                Role = "admin"
            });
        await InsertToEventLogAndOutbox(evt, db);
    }

    private static async Task InsertToEventLogAndOutbox(IDomainEvent evt, DbSession db)
    {
        var eventType = evt.GetType().Name;
        var eventDataJson = System.Text.Json.JsonSerializer.Serialize(evt, evt.GetType());

        await db.ExecuteAsync(TeamSql.InsertEventLog(),
            new { EventType = eventType, OccurredAt = evt.OccurredAt });
        await db.ExecuteAsync(
            TeamSql.InsertOutbox(),
            new { EventType = eventType, EventData = eventDataJson, CreatedAt = (DateTime?)null });
    }
}
