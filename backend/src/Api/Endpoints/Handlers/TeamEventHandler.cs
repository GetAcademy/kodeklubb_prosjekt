using Core.DomainEvents;
using Npgsql;
using Persistence;

namespace Api.Endpoints.Handlers;

public static class TeamEventHandler
{
    public static async Task HandleAsync(
        IReadOnlyList<IDomainEvent> events,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        foreach (var evt in events)
        {
            if (evt is TeamCreated created) await HandleEvent(created, connection, transaction);
            if (evt is UserRequestedToJoinTeam team) await HandleEvent(team, connection, transaction);
            if (evt is JoinRequestApproved approved) await HandleEvent(approved, connection, transaction);
            if (evt is JoinRequestDeclined declined) await HandleEvent(declined, connection, transaction);
        }
    }

    private static async Task HandleEvent(JoinRequestDeclined evt, NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await connection.ExecuteCommandAsync(
            InvitationSql.DeletePendingInvitation, 
            new { RequestId = evt.RequestId, TeamId = evt.TeamId }, 
            transaction);
        await InsertToEventLogAndOutbox(evt, connection, transaction);
    }

    private static async Task HandleEvent(JoinRequestApproved evt, NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        await connection.ExecuteCommandAsync(
            InvitationSql.DeletePendingInvitation, 
            new { RequestId = evt.RequestId, TeamId = evt.TeamId }, 
            transaction);
        await connection.ExecuteCommandAsync(
            TeamSql.InsertTeamMember, 
            new { TeamId = evt.TeamId, UserId = evt.UserId, Role = "member" }, 
            transaction);
        await InsertToEventLogAndOutbox(evt, connection, transaction);
    }

    private static async Task HandleEvent(UserRequestedToJoinTeam evt, NpgsqlConnection connection,
        NpgsqlTransaction transaction)
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
        await connection.ExecuteCommandAsync(TeamSql.InsertEventLog,
            new { EventType = nameof(evt), OccurredAt = evt.OccurredAt },
            transaction);
        await connection.ExecuteCommandAsync(
            TeamSql.InsertOutbox, 
            new { EventType = nameof(evt) }, transaction);
    }
}