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
            switch (evt)
            {
                case TeamCreated tc:
                    await connection.ExecuteCommandAsync(TeamSql.CreateTeam,
                        new
                        {
                            Id = tc.TeamId, 
                            Name = tc.Name, 
                            Description = tc.Description, 
                            AdminUserId = tc.AdminUserId
                        }, transaction);
                    await connection.ExecuteCommandAsync(TeamSql.InsertTeamMember,
                        new
                        {
                            TeamId = tc.TeamId, 
                            UserId = tc.AdminUserId, 
                            Role = "admin"
                        }, transaction);
                    await InsertToEventLogAndOutbox(tc, connection, transaction);
                    break;
                case UserRequestedToJoinTeam ur:
                    var invitationId = Guid.NewGuid();
                    await connection.ExecuteCommandAsync(
                        InvitationSql.SendInvitation, 
                        new
                        {
                            Id = invitationId, 
                            TeamId = ur.TeamId, 
                            InvitedUserId = ur.UserId, 
                            InvitedBy = ur.UserId, 
                            Status = "pending", 
                            InvitedAt = ur.OccurredAt
                        }, transaction);
                    await InsertToEventLogAndOutbox(ur, connection, transaction);
                    break;
                case JoinRequestApproved jra:
                    await connection.ExecuteCommandAsync(
                        InvitationSql.DeletePendingInvitation, 
                        new { RequestId = jra.RequestId, TeamId = jra.TeamId }, 
                        transaction);
                    await connection.ExecuteCommandAsync(
                        TeamSql.InsertTeamMember, 
                        new { TeamId = jra.TeamId, UserId = jra.UserId, Role = "member" }, 
                        transaction);
                    await InsertToEventLogAndOutbox(jra, connection, transaction);
                    break;
                case JoinRequestDeclined jrd:
                    await connection.ExecuteCommandAsync(
                        InvitationSql.DeletePendingInvitation, 
                        new { RequestId = jrd.RequestId, TeamId = jrd.TeamId }, 
                        transaction);
                    await InsertToEventLogAndOutbox(jrd, connection, transaction);
                    break;
            }
        }

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