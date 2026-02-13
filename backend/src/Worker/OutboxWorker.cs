using System.Data;
using System.Text.Json;
using Core.DomainEvents;
using Dapper;
using Microsoft.Extensions.Hosting;
using Persistence.Outbox;
using Npgsql;

namespace Worker;


public class OutboxWorker : BackgroundService
{
    private readonly string _connectionString;

    public OutboxWorker(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await using var db = new NpgsqlConnection(_connectionString);
            await db.OpenAsync(stoppingToken);

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
        Console.WriteLine($"Sending invite to {evt.UserId}.");
        return Task.CompletedTask;
    }
}