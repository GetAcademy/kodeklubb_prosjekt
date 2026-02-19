using Core.Commands;
using Core.DomainEvents;
using Core.Logic;
using Core.Outcomes;
using Core.State;
using Persistence;
using Persistence.DbModels;
using static Api.Endpoints.TeamEndpoints;

namespace Api.Endpoints.Teams;

    public static class DeclineTeamRequestEndpoint
    {
        public static async Task<IResult> DeclineTeamRequest(Guid teamId, Guid requestId, HttpContext context)
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
    }
