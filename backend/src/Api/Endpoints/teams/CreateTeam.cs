using Core.Commands;
using Core.DomainEvents;
using Core.Logic;
using Core.Outcomes;
using Core.State;
using Persistence;
using static Api.Endpoints.TeamEndpoints;
namespace Api.Endpoints.Teams;

public static class CreateTeamEndpoint
    {
        public static async Task<IResult> CreateTeam(HttpContext context)
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
    }
