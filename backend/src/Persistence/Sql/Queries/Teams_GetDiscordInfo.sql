SELECT id, name, discord_link, discord_server_id, discord_channel_id
FROM teams
WHERE id = @TeamId;
