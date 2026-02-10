INSERT INTO event_log (
    event_type, 
    payload, 
    occurred_at
)
VALUES (
    @EventType, 
    @Payload::jsonb, 
    @OccurredAt
);
