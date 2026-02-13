INSERT INTO outbox (
    event_type, 
    event_data, 
    status, 
    created_at
)
VALUES (
    @EventType, 
    '{}'::jsonb, 
    'Pending', 
    NOW()
);
