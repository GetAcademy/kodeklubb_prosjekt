SELECT id, event_type AS EventType, event_data AS Payload, created_at AS OccuredOn,
       retry_count AS RetryCount, max_retries AS MaxRetries
FROM outbox
WHERE processed_at IS NULL
  AND status = 'pending'
  AND (next_retry_at IS NULL OR next_retry_at <= NOW())
ORDER BY created_at
LIMIT 20;
