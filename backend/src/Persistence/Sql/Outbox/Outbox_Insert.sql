INSERT INTO outbox (
    event_type, 
    event_data, 
    status, 
    created_at
)
VALUES (
    @EventType, 
    @EventData::jsonb, 
    'Pending', 
    COALESCE(@CreatedAt, NOW())
);
