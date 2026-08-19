using System.Data;
using System.Text.Json;
using Core.DomainEvents;
using Core.Logic;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Persistence;
using Persistence.Outbox;
using Npgsql;

namespace Worker;

public class OutboxWorker : BackgroundService
{
    private readonly string _connectionString;
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxWorker(string connectionString, IServiceScopeFactory scopeFactory)
    {
        _connectionString = connectionString;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Outbox worker started and polling outbox table every 3 seconds.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var discordService = scope.ServiceProvider.GetRequiredService<IDiscordNotificationService>();

                await using var db = new NpgsqlConnection(_connectionString);
                await db.OpenAsync(stoppingToken);

                // Only pick up rows that are still pending (never re-pick 'failed'
                // rows — per handbook 3.6, those need an admin to look at them,
                // not endless silent retries) and whose next_retry_at has passed.
                var messages = await db.QueryAsync<OutboxMessage>(OutboxSql.ClaimPending());

                foreach (var msg in messages)
                {
                    await HandleMessage(msg, db, emailService, discordService);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Outbox worker error (will retry): {ex.Message}");
            }

            await Task.Delay(3000, stoppingToken);
        }
    }

    private async Task HandleMessage(OutboxMessage msg, IDbConnection db, IEmailService emailService, IDiscordNotificationService discordService)
    {
        string? errorMessage = null;

        try
        {
            switch (msg.EventType)
            {
                case "UserRequestedToJoinTeam":
                {
                    var evt = JsonSerializer.Deserialize<UserRequestedToJoinTeam>(msg.Payload)!;
                    await SendJoinRequestNotification(evt, db, emailService, discordService);
                    break;
                }
                case "JoinRequestApproved":
                {
                    var evt = JsonSerializer.Deserialize<JoinRequestApproved>(msg.Payload)!;
                    await SendApprovalNotification(evt, db, emailService, discordService);
                    break;
                }
                case "JoinRequestDeclined":
                {
                    var evt = JsonSerializer.Deserialize<JoinRequestDeclined>(msg.Payload)!;
                    await SendDeclineNotification(evt, db, emailService, discordService);
                    break;
                }
                case "UserInvitedToTeam":
                {
                    var evt = JsonSerializer.Deserialize<UserInvitedToTeam>(msg.Payload)!;
                    await SendInviteNotification(evt, db, emailService, discordService);
                    break;
                }
                default:
                    Console.WriteLine($"Unknown event type: {msg.EventType} — skipping.");
                    break;
            }
        }
        catch (Exception ex)
        {
            // Any failure sending the notification (bad address, provider outage, etc.)
            // is recorded here rather than thrown further — per handbook 3.6, a failed
            // notification should never roll back or block, but it must still be
            // tracked via retry_count/error_message so the row isn't silently dropped.
            errorMessage = ex.Message;
        }

        if (errorMessage == null)
        {
            await db.ExecuteAsync(
                OutboxSql.MarkProcessed(),
                new { id = msg.Id });
            return;
        }

        var newRetryCount = msg.RetryCount + 1;
        var exceededThreshold = newRetryCount >= msg.MaxRetries;

        Console.WriteLine(exceededThreshold
            ? $"[OutboxWorker] {msg.EventType} ({msg.Id}) failed permanently after {newRetryCount} attempts: {errorMessage}"
            : $"[OutboxWorker] {msg.EventType} ({msg.Id}) failed (attempt {newRetryCount}/{msg.MaxRetries}), will retry: {errorMessage}");

        await db.ExecuteAsync(
            @"UPDATE outbox
              SET retry_count = @RetryCount,
                  error_message = @ErrorMessage,
                  status = @Status,
                  failed_at = @FailedAt,
                  next_retry_at = @NextRetryAt
              WHERE id = @Id",
            new
            {
                Id = msg.Id,
                RetryCount = newRetryCount,
                ErrorMessage = errorMessage,
                Status = exceededThreshold ? "failed" : "pending",
                FailedAt = exceededThreshold ? DateTime.UtcNow : (DateTime?)null,
                // Simple fixed backoff for MVP — the handbook doesn't mandate a
                // specific backoff curve, just that retries happen "later" rather
                // than immediately hammering the same failing row every 3s.
                NextRetryAt = exceededThreshold ? (DateTime?)null : DateTime.UtcNow.AddSeconds(30)
            });
    }

    private Task SendInviteNotification(UserInvitedToTeam evt, IDbConnection db, IEmailService emailService, IDiscordNotificationService discordService)
    {
        return NotifyUser(evt.UserId,
            "You have been invited to a team",
            "<h1>Team invitation</h1><p>You have been invited to join a team.</p>",
            "You have been invited to join a team on KodeKlubb! 🎉",
            db, emailService, discordService);
    }

    private async Task SendApprovalNotification(JoinRequestApproved evt, IDbConnection db, IEmailService emailService, IDiscordNotificationService discordService)
    {
        await NotifyUser(evt.UserId,
            "Your team request was approved",
            "<h1>Congratulations!</h1><p>Your request to join the team has been approved.</p>",
            "🎉 Your request to join the team has been approved!",
            db, emailService, discordService);

        // Since server membership itself is still added manually (we only
        // have one Discord server), this is the automated part: introduce
        // the new member and the team admin to each other — both via a
        // channel post (if the team has one configured) and a DM to each
        // person naming the other, so they can connect directly.
        await SendTeamIntroNotifications(evt.TeamId, evt.UserId, db, discordService);
    }

    private async Task SendTeamIntroNotifications(Guid teamId, Guid newMemberId, IDbConnection db, IDiscordNotificationService discordService)
    {
        try
        {
            var info = await db.QuerySingleOrDefaultAsync<ApprovalIntroInfo?>(
                OutboxSql.GetApprovalIntroInfo(), new { TeamId = teamId, UserId = newMemberId });

            if (info == null) return;

            // Post in the team's Discord channel, mentioning both people,
            // if one is configured for this team.
            if (!string.IsNullOrWhiteSpace(info.DiscordChannelId)
                && !string.IsNullOrWhiteSpace(info.AdminDiscordId)
                && !string.IsNullOrWhiteSpace(info.NewMemberDiscordId))
            {
                try
                {
                    await discordService.SendChannelMessageAsync(info.DiscordChannelId,
                        $"👋 <@{info.NewMemberDiscordId}> has joined the team! <@{info.AdminDiscordId}>, say hi.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OutboxWorker] Failed to post team-intro channel message: {ex.Message}");
                }
            }

            // Also DM each person individually with the other's name, so the
            // connection happens even for teams with no channel configured.
            if (!string.IsNullOrWhiteSpace(info.AdminDiscordId))
            {
                try
                {
                    await discordService.SendDirectMessageAsync(info.AdminDiscordId,
                        $"👋 {info.NewMemberUsername} just joined your team on Discord \u2014 say hi and get them connected!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OutboxWorker] Failed to send admin intro DM: {ex.Message}");
                }
            }

            if (!string.IsNullOrWhiteSpace(info.NewMemberDiscordId))
            {
                try
                {
                    await discordService.SendDirectMessageAsync(info.NewMemberDiscordId,
                        $"👋 Your team admin is {info.AdminUsername} \u2014 reach out on Discord if you have questions!");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[OutboxWorker] Failed to send new-member intro DM: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            // Best-effort only — never let this affect the approval itself.
            Console.WriteLine($"[OutboxWorker] Team-intro notification step failed: {ex.Message}");
        }
    }

    private Task SendDeclineNotification(JoinRequestDeclined evt, IDbConnection db, IEmailService emailService, IDiscordNotificationService discordService)
    {
        return NotifyUser(evt.UserId,
            "Your team request was declined",
            "<h1>Request declined</h1><p>Your request to join the team was declined.</p>",
            "Your request to join the team was declined.",
            db, emailService, discordService);
    }

    private async Task SendJoinRequestNotification(UserRequestedToJoinTeam evt, IDbConnection db, IEmailService emailService, IDiscordNotificationService discordService)
    {
        var admin = await db.QuerySingleOrDefaultAsync<UserContact?>(
            OutboxSql.GetAdminContactByTeamId(),
            new { TeamId = evt.TeamId });

        if (admin == null) return;

        await SendToBothChannels(
            admin.Email,
            admin.DiscordId,
            "New team join request",
            "<h1>New join request</h1><p>A user has requested to join your team.</p>",
            "📥 A new user has requested to join your team.",
            emailService, discordService);
    }

    private async Task NotifyUser(Guid userId, string subject, string htmlBody, string discordMessage, IDbConnection db, IEmailService emailService, IDiscordNotificationService discordService)
    {
        var contact = await db.QuerySingleOrDefaultAsync<UserContact?>(
            OutboxSql.GetUserContactById(),
            new { UserId = userId });

        if (contact == null) return;

        await SendToBothChannels(contact.Email, contact.DiscordId, subject, htmlBody, discordMessage, emailService, discordService);
    }

    /// <summary>
    /// Tries both notification channels independently. A failure in one
    /// doesn't stop the other from being attempted. Only throws (and thus
    /// triggers the outbox's retry logic) if BOTH channels fail — if at
    /// least one succeeded, the user was actually notified, so the row is
    /// considered processed.
    /// </summary>
    private static async Task SendToBothChannels(
        string? email, string? discordId, string subject, string htmlBody, string discordMessage,
        IEmailService emailService, IDiscordNotificationService discordService)
    {
        Exception? emailError = null;
        Exception? discordError = null;

        if (!string.IsNullOrWhiteSpace(discordId))
        {
            try
            {
                await discordService.SendDirectMessageAsync(discordId, discordMessage);
            }
            catch (Exception ex)
            {
                discordError = ex;
                Console.WriteLine($"[OutboxWorker] Discord DM failed for {discordId}: {ex.Message}");
            }
        }
        else
        {
            discordError = new InvalidOperationException("No Discord ID on file.");
        }

        if (!string.IsNullOrWhiteSpace(email))
        {
            try
            {
                await emailService.SendEmailAsync(email, subject, htmlBody);
            }
            catch (Exception ex)
            {
                emailError = ex;
                Console.WriteLine($"[OutboxWorker] Email failed for {email}: {ex.Message}");
            }
        }
        else
        {
            emailError = new InvalidOperationException("No email on file.");
        }

        if (discordError != null && emailError != null)
        {
            throw new InvalidOperationException(
                $"Both notification channels failed. Discord: {discordError.Message} | Email: {emailError.Message}");
        }
    }
}

public record UserContact(string? Email, string? DiscordId);
public record ApprovalIntroInfo(string? DiscordChannelId, string? AdminDiscordId, string? AdminUsername, string? NewMemberDiscordId, string? NewMemberUsername);
