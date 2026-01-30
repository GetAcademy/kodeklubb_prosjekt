using System.Data;
using System.Text.Json;
using Dapper;

namespace Persistence.Outbox;

public class OutboxRepository
{
    private readonly IDbConnection _db;

    public OutboxRepository(IDbConnection db)
    {
        _db = db;
    }

    public async Task AddEvents(IEnumerable<object> events, IDbTransaction tx)
    {
        foreach (var evt in events)
        {
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                EventType = evt.GetType().Name,
                Payload = JsonSerializer.Serialize(evt),
                OccuredOn = DateTime.UtcNow
            };

            await _db.ExecuteAsync(
                @"INSERT INTO outbox (id, event_type, payload, created_at)
                  VALUES (@Id, @EventType, @Payload, @CreatedAt)",
                message, tx
            );
        }
    }
}