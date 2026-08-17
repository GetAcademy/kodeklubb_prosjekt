UPDATE teams
SET discord_server_id = NULL,
    discord_channel_id = NULL,
    discord_role_id = NULL,
    discord_link = NULL,
    updated_at = NOW()
WHERE id = @TeamId;
