using Core.DomainEvents;
using Npgsql;
using Persistence;

namespace Api.Endpoints.Handlers;

public static class TeamEventHandler
{
    public static async Task HandleAsync(
        IReadOnlyList<IDomainEvent> events,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        IServiceProvider? serviceProvider = null)
    {
        foreach (var evt in events)
        {
            if (evt is TeamCreated created) await HandleEvent(created, connection, transaction);
            if (evt is UserRequestedToJoinTeam team) await HandleEvent(team, connection, transaction, serviceProvider);
            if (evt is JoinRequestApproved approved) await HandleEvent(approved, connection, transaction, serviceProvider);
            if (evt is JoinRequestDeclined declined) await HandleEvent(declined, connection, transaction);
        }
    }

    private static async Task HandleEvent(JoinRequestDeclined evt, NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await connection.ExecuteCommandAsync(
            InvitationSql.DeclineInvitation(),
            new { RequestId = evt.RequestId, TeamId = evt.TeamId, RespondedAt = evt.OccurredAt },
            transaction);
        await InsertToEventLogAndOutbox(evt, connection, transaction);
    }

    private static async Task HandleEvent(JoinRequestApproved evt, NpgsqlConnection connection,
        NpgsqlTransaction transaction, IServiceProvider? serviceProvider)
    {
        await connection.ExecuteCommandAsync(
            InvitationSql.ApproveInvitation(),
            new { RequestId = evt.RequestId, TeamId = evt.TeamId, RespondedAt = evt.OccurredAt },
            transaction);
        await connection.ExecuteCommandAsync(
            TeamSql.InsertTeamMember,
            new { TeamId = evt.TeamId, UserId = evt.UserId, Role = "member" },
            transaction);
        await InsertToEventLogAndOutbox(evt, connection, transaction);

        if (serviceProvider != null)
        {
            var emailService = serviceProvider.GetRequiredService<Core.Logic.IEmailService>();
            // Get user email
            var user = await connection.QueryOneOrDefaultAsync<Persistence.DbModels.UserEntity>(Persistence.TeamSql.GetUserByUserId(), new { UserId = evt.UserId }, transaction);
            if (user?.Email != null)
            {
                try
                {
                    await emailService.SendEmailAsync(user.Email, "You have been accepted to the team!", $"<h1>Congratulations!</h1><p>Your request to join the team has been approved.</p>");
                }
                catch (Exception ex)
                {
                    // A failed notification email should never undo a successful
                    // approval. Log and move on — the outbox row already recorded
                    // this event for later inspection/retry if needed.
                    Console.WriteLine($"[TeamEventHandler] Failed to send approval email to {user.Email}: {ex.Message}");
                }
            }
        }
    }

    private static async Task HandleEvent(UserRequestedToJoinTeam evt, NpgsqlConnection connection,
        NpgsqlTransaction transaction, IServiceProvider? serviceProvider)
    {
        var invitationId = Guid.NewGuid();
        await connection.ExecuteCommandAsync(
            InvitationSql.SendInvitation,
            new
            {
                Id = invitationId,
                TeamId = evt.TeamId,
                InvitedUserId = evt.UserId,
                InvitedBy = evt.UserId,
                Status = "pending",
                InvitedAt = evt.OccurredAt
            }, transaction);
        await InsertToEventLogAndOutbox(evt, connection, transaction);

        if (serviceProvider != null)
        {
           var emailService = serviceProvider.GetRequiredService<Core.Logic.IEmailService>();            // Get team admin email
            var admin = await connection.QueryOneOrDefaultAsync<Persistence.DbModels.UserEntity>(Persistence.TeamSql.GetAdminUserRaw(), new { TeamId = evt.TeamId }, transaction);
            if (admin?.Email != null)
            {
                try
                {
                    await emailService.SendEmailAsync(admin.Email, "New team join request", $"<h1>New join request</h1><p>A user has requested to join your team.</p>");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[TeamEventHandler] Failed to send join-request notification to {admin.Email}: {ex.Message}");
                }
            }
        }
    }
    private static async Task HandleEvent(TeamCreated evt, NpgsqlConnection connection, NpgsqlTransaction transaction)
    {
        await connection.ExecuteCommandAsync(TeamSql.CreateTeam,
            new
            {
                Id = evt.TeamId,
                Name = evt.Name,
                Description = evt.Description,
                AdminUserId = evt.AdminUserId
            }, transaction);
        await connection.ExecuteCommandAsync(TeamSql.InsertTeamMember,
            new
            {
                TeamId = evt.TeamId,
                UserId = evt.AdminUserId,
                Role = "admin"
            }, transaction);
        await InsertToEventLogAndOutbox(evt, connection, transaction);
    }

    private static async Task InsertToEventLogAndOutbox(
        IDomainEvent evt,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        // evt.GetType().Name gives the real event type (e.g. "UserRequestedToJoinTeam").
        // nameof(evt) would only ever return the literal string "evt" — the parameter's
        // own name, not the runtime type — which was silently recording useless data.
        var eventType = evt.GetType().Name;

        // Outbox_Insert.sql requires @EventData; it was previously never supplied,
        // which is what caused the "column eventdata does not exist" failure.
        var eventDataJson = System.Text.Json.JsonSerializer.Serialize(evt, evt.GetType());

        await connection.ExecuteCommandAsync(TeamSql.InsertEventLog,
            new { EventType = eventType, OccurredAt = evt.OccurredAt },
            transaction);
        await connection.ExecuteCommandAsync(
            TeamSql.InsertOutbox,
            new { EventType = eventType, EventData = eventDataJson, CreatedAt = (DateTime?)null },
            transaction);
    }
}
