using System.Data;
using System.Text.Json;
using Core.DomainEvents;
using Microsoft.Extensions.DependencyInjection;
using Dapper;
using Microsoft.Extensions.Hosting;
using Persistence.Outbox;

namespace Worker;


public class OutboxWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;

    public OutboxWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IDbConnection>();

            var messages = await db.QueryAsync<OutboxMessage>(
                @"SELECT * FROM outbox
                  WHERE processed_at IS NULL
                  ORDER BY created_at
                  LIMIT 20");

            foreach (var msg in messages)
            {
                await HandleMessage(msg, db);
            }

            await Task.Delay(3000, stoppingToken);
        }
    }

    private async Task HandleMessage(OutboxMessage msg, IDbConnection db)
    {
        switch (msg.EventType)
        {
            case "UserInvitedToTeam":
                var evt = JsonSerializer.Deserialize<UserInvitedToTeam>(msg.Payload)!;
                await SendInvite(evt);
                break;
        }

        await db.ExecuteAsync(
            "UPDATE outbox SET processed_at = NOW() WHERE id = @id",
            new { id = msg.Id }
            );
    }

    private Task SendInvite(UserInvitedToTeam evt)
    {
        Console.WriteLine($"Sending invite to {evt.UserId} for team {evt.TeamId}");
        return Task.CompletedTask;
    }
}