namespace Api.Endpoints.Teams
{
    using Api;
    using Api.Endpoints;
    using Core.Commands;
    using Core.DomainEvents;
    using Core.Logic;
    using Core.Outcomes;
    using Core.State;
    using Persistence;
    using Persistence.DbModels;

    public static class RequestToJoinTeamEndpoint
    {
        public static async Task<IResult> RequestToJoinTeam(Guid teamId, HttpContext context)
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
    }
}
