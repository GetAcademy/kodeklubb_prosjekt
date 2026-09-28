UPDATE teams
SET discord_server_id = @DiscordServerId, updated_at = NOW()
WHERE id = @TeamId;
