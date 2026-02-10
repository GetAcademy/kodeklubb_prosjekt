INSERT INTO outbox (
    event_type, 
    payload, 
    status, 
    created_at
)
VALUES (
    @EventType, 
    @Payload::jsonb, 
    'Pending', 
    NOW()
);
