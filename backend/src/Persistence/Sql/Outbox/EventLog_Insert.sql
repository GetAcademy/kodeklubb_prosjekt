INSERT INTO event_log (
    event_type, 
    event_data, 
    aggregate_id,
    aggregate_type,
    occurred_at
)
VALUES (
    @EventType, 
    '{}'::jsonb, 
    '',
    '',
    @OccurredAt
);
