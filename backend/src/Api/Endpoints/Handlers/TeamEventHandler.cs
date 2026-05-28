using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
            InvitationSql.DeletePendingInvitation,
            new { RequestId = evt.RequestId, TeamId = evt.TeamId },
            transaction);
        await InsertToEventLogAndOutbox(evt, connection, transaction);
    }

    private static async Task HandleEvent(JoinRequestApproved evt, NpgsqlConnection connection,
        NpgsqlTransaction transaction, IServiceProvider? serviceProvider)
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

        if (serviceProvider != null)
        {
            // Guild auto-join: add the user to the team's Discord server
            try
            {
                var botService = (Core.Logic.IDiscordBotService)serviceProvider.GetService(typeof(Core.Logic.IDiscordBotService))!;

                var team = await connection.QueryOneOrDefaultAsync<Persistence.DbModels.TeamEntity>(
                            @"SELECT id AS Id,
                            name AS Name,
                            discord_server_id AS DiscordServerId,
                            discord_link AS DiscordLink
                            FROM teams WHERE id = @TeamId",
                            new { TeamId = evt.TeamId }, transaction);

                var mapping = await connection.QueryOneOrDefaultAsync<DiscordMapping>(
                    @"SELECT discord_user_id   AS DiscordUserId,
                             oauth_access_token  AS OauthAccessToken,
                             oauth_refresh_token AS OauthRefreshToken,
                             token_expires_at    AS TokenExpiresAt
                      FROM discord_user_mappings
                      WHERE user_id = @UserId",
                    new { UserId = evt.UserId }, transaction);

                Console.WriteLine($"[DISCORD] team.DiscordServerId={team?.DiscordServerId}, mapping={mapping != null}, token={mapping?.OauthAccessToken != null}, tokenExpiry={mapping?.TokenExpiresAt}");
                Console.WriteLine($"[DISCORD] Looking up team with ID: {evt.TeamId}");
                if (team?.DiscordServerId != null && mapping != null)
                {
                    var oauthToken = mapping.OauthAccessToken;

                    // Refresh the token if it has expired or is about to expire (within 60s)
                    if (oauthToken == null || mapping.TokenExpiresAt == null || DateTime.UtcNow >= mapping.TokenExpiresAt.Value.AddSeconds(-60))
                    {
                        Console.WriteLine("[DISCORD] Token expired or missing, attempting refresh...");
                        if (mapping.OauthRefreshToken != null)
                        {
                            oauthToken = await RefreshDiscordTokenAsync(
                                mapping.OauthRefreshToken,
                                mapping.DiscordUserId,
                                evt.UserId,
                                connection,
                                transaction);
                        }
                        else
                        {
                            Console.WriteLine("[DISCORD] No refresh token stored — user must log in again.");
                            oauthToken = null;
                        }
                    }

                    if (oauthToken != null)
                    {
                        await botService.AddUserToGuildAsync(
                            team.DiscordServerId,
                            mapping.DiscordUserId,
                            oauthToken);
                        Console.WriteLine($"[DISCORD] Added user {evt.UserId} to guild {team.DiscordServerId}");
                    }
                    else
                    {
                        Console.WriteLine("[DISCORD] Skipping guild join: could not obtain a valid OAuth token.");
                    }
                }
                else
                {
                    Console.WriteLine($"[DISCORD] Skipping guild join: team.DiscordServerId={team?.DiscordServerId ?? "null"}, mapping={mapping != null}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[DISCORD] Guild join failed: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[DISCORD] Inner: {ex.InnerException.Message}");
            }

            // Email notification
            try
            {
                var emailService = (Core.Logic.IEmailService)serviceProvider.GetService(typeof(Core.Logic.IEmailService))!;
                var user = await connection.QueryOneOrDefaultAsync<Persistence.DbModels.UserEntity>(
                    "SELECT * FROM users WHERE id = @UserId",
                    new { UserId = evt.UserId }, transaction);
                if (user?.Email != null)
                {
                    await emailService.SendEmailAsync(
                        user.Email,
                        "You have been accepted to the team!",
                        "<h1>Congratulations!</h1><p>Your request to join the team has been approved. You should now be in the team's Discord server!</p>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL] Failed to send approval email (non-critical): {ex.Message}");
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
            try
            {
                var emailService = (Core.Logic.IEmailService)serviceProvider.GetService(typeof(Core.Logic.IEmailService))!;
                var admin = await connection.QueryOneOrDefaultAsync<Persistence.DbModels.UserEntity>(
                    "SELECT u.* FROM users u JOIN teams t ON u.id = t.team_admin_id WHERE t.id = @TeamId",
                    new { TeamId = evt.TeamId }, transaction);
                if (admin?.Email != null)
                {
                    await emailService.SendEmailAsync(
                        admin.Email,
                        "New team join request",
                        "<h1>New join request</h1><p>A user has requested to join your team.</p>");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL] Failed to send join request email (non-critical): {ex.Message}");
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
        await connection.ExecuteCommandAsync(TeamSql.InsertEventLog,
            new { EventType = nameof(evt), OccurredAt = evt.OccurredAt },
            transaction);
        await connection.ExecuteCommandAsync(
            TeamSql.InsertOutbox,
            new { EventType = nameof(evt) }, transaction);
    }

    /// <summary>
    /// Calls Discord's token refresh endpoint, updates discord_user_mappings, and returns the new access token.
    /// Returns null if the refresh fails.
    /// </summary>
    private static async Task<string?> RefreshDiscordTokenAsync(
        string refreshToken,
        string discordUserId,
        Guid userId,
        NpgsqlConnection connection,
        NpgsqlTransaction transaction)
    {
        try
        {
            var clientId     = AppConfig.Configuration["Discord:ClientId"]!;
            var clientSecret = AppConfig.Configuration["Discord:ClientSecret"]!;

            using var http = new HttpClient();
            var resp = await http.PostAsync(
                "https://discord.com/api/oauth2/token",
                new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"]     = clientId,
                    ["client_secret"] = clientSecret,
                    ["grant_type"]    = "refresh_token",
                    ["refresh_token"] = refreshToken,
                }));

            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync();
                Console.WriteLine($"[DISCORD] Token refresh HTTP {resp.StatusCode}: {err}");
                return null;
            }

            var json = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var newAccessToken  = root.GetProperty("access_token").GetString();
            var newRefreshToken = root.GetProperty("refresh_token").GetString();
            var expiresIn       = root.GetProperty("expires_in").GetInt32();
            var newExpiry       = DateTime.UtcNow.AddSeconds(expiresIn);

            if (newAccessToken == null) return null;

            await connection.ExecuteCommandAsync(
                @"UPDATE discord_user_mappings
                  SET oauth_access_token  = @Token,
                      oauth_refresh_token = @RefreshToken,
                      token_expires_at    = @ExpiresAt
                  WHERE user_id = @UserId AND discord_user_id = @DiscordUserId",
                new
                {
                    Token         = newAccessToken,
                    RefreshToken  = newRefreshToken,
                    ExpiresAt     = newExpiry,
                    UserId        = userId,
                    DiscordUserId = discordUserId
                }, transaction);

            Console.WriteLine("[DISCORD] Token refreshed successfully.");
            return newAccessToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[DISCORD] Token refresh exception: {ex.Message}");
            return null;
        }
    }

    // Typed mapping class — avoids dynamic property access bugs with Dapper
    private class DiscordMapping
    {
        public string DiscordUserId { get; init; } = "";
        public string? OauthAccessToken { get; init; }
        public string? OauthRefreshToken { get; init; }
        public DateTime? TokenExpiresAt { get; init; }
    }
}