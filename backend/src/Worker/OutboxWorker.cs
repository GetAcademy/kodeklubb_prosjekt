using System.Data;
using System.Text.Json;
using Core.DomainEvents;
using Core.Logic;
using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

                await using var db = new NpgsqlConnection(_connectionString);
                await db.OpenAsync(stoppingToken);

                // Only pick up rows that are still pending (never re-pick 'failed'
                // rows — per handbook 3.6, those need an admin to look at them,
                // not endless silent retries) and whose next_retry_at has passed.
                var messages = await db.QueryAsync<OutboxMessage>(
                    @"SELECT id, event_type AS EventType, event_data AS Payload, created_at AS OccuredOn,
                             retry_count AS RetryCount, max_retries AS MaxRetries
                      FROM outbox
                      WHERE processed_at IS NULL
                        AND status = 'pending'
                        AND (next_retry_at IS NULL OR next_retry_at <= NOW())
                      ORDER BY created_at
                      LIMIT 20");

                foreach (var msg in messages)
                {
                    await HandleMessage(msg, db, emailService);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Outbox worker error (will retry): {ex.Message}");
            }

            await Task.Delay(3000, stoppingToken);
        }
    }

    private async Task HandleMessage(OutboxMessage msg, IDbConnection db, IEmailService emailService)
    {
        string? errorMessage = null;

        try
        {
            switch (msg.EventType)
            {
                case "UserRequestedToJoinTeam":
                {
                    var evt = JsonSerializer.Deserialize<UserRequestedToJoinTeam>(msg.Payload)!;
                    await SendJoinRequestNotification(evt, db, emailService);
                    break;
                }
                case "JoinRequestApproved":
                {
                    var evt = JsonSerializer.Deserialize<JoinRequestApproved>(msg.Payload)!;
                    await SendApprovalNotification(evt, db, emailService);
                    break;
                }
                case "JoinRequestDeclined":
                {
                    var evt = JsonSerializer.Deserialize<JoinRequestDeclined>(msg.Payload)!;
                    await SendDeclineNotification(evt, db, emailService);
                    break;
                }
                case "UserInvitedToTeam":
                {
                    var evt = JsonSerializer.Deserialize<UserInvitedToTeam>(msg.Payload)!;
                    await SendInviteNotification(evt, db, emailService);
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
                "UPDATE outbox SET status = 'processed', processed_at = NOW() WHERE id = @id",
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

    private Task SendInviteNotification(UserInvitedToTeam evt, IDbConnection db, IEmailService emailService)
    {
        return SendEmailToUser(evt.UserId,
            "You have been invited to a team",
            "<h1>Team invitation</h1><p>You have been invited to join a team.</p>",
            db, emailService);
    }

    private Task SendApprovalNotification(JoinRequestApproved evt, IDbConnection db, IEmailService emailService)
    {
        return SendEmailToUser(evt.UserId,
            "Your team request was approved",
            "<h1>Congratulations!</h1><p>Your request to join the team has been approved.</p>",
            db, emailService);
    }

    private Task SendDeclineNotification(JoinRequestDeclined evt, IDbConnection db, IEmailService emailService)
    {
        return SendEmailToUser(evt.UserId,
            "Your team request was declined",
            "<h1>Request declined</h1><p>Your request to join the team was declined.</p>",
            db, emailService);
    }

    private async Task SendJoinRequestNotification(UserRequestedToJoinTeam evt, IDbConnection db, IEmailService emailService)
    {
        var adminEmail = await db.QuerySingleOrDefaultAsync<string?>(
            @"SELECT u.email FROM users u
              JOIN teams t ON t.team_admin_id = u.id
              WHERE t.id = @TeamId",
            new { TeamId = evt.TeamId });

        if (!string.IsNullOrWhiteSpace(adminEmail))
        {
            // Intentionally NOT caught here — let it propagate up to HandleMessage,
            // which is now the single place responsible for deciding retry vs. failed
            // vs. processed. Swallowing it here (as in the earlier fix) hid failures
            // from the outbox's retry bookkeeping entirely.
            await emailService.SendEmailAsync(
                adminEmail,
                "New team join request",
                "<h1>New join request</h1><p>A user has requested to join your team.</p>");
        }
    }

    private async Task SendEmailToUser(Guid userId, string subject, string htmlBody, IDbConnection db, IEmailService emailService)
    {
        var email = await db.QuerySingleOrDefaultAsync<string?>(
            "SELECT email FROM users WHERE id = @UserId",
            new { UserId = userId });

        if (!string.IsNullOrWhiteSpace(email))
        {
            // Same reasoning as SendJoinRequestNotification above — no try/catch here.
            await emailService.SendEmailAsync(email, subject, htmlBody);
        }
    }
}