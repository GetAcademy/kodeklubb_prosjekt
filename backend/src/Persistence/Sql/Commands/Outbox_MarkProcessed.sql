UPDATE outbox SET status = 'processed', processed_at = NOW() WHERE id = @id;
