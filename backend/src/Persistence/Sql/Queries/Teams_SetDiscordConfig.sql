UPDATE teams
SET discord_server_id = @DiscordServerId,
    discord_channel_id = @DiscordChannelId,
    discord_role_id = @DiscordRoleId,
    discord_link = @DiscordLink,
    updated_at = NOW()
WHERE id = @TeamId;
