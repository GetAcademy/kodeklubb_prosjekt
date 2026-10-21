SELECT
    t.name,
    t.discord_server_id AS "DiscordServerId",
    t.discord_team_role_id AS "DiscordTeamRoleId",
    t.discord_text_channel_id AS "DiscordTextChannelId",
    t.discord_voice_channel_id AS "DiscordVoiceChannelId",
    u.discord_id AS "AdminDiscordId"
FROM teams t
LEFT JOIN users u ON u.id = t.team_admin_id
WHERE t.id = @TeamId;