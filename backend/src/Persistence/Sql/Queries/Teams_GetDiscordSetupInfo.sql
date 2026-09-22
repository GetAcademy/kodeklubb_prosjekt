SELECT
    name,
    discord_server_id AS "DiscordServerId",
    discord_team_role_id AS "DiscordTeamRoleId",
    discord_text_channel_id AS "DiscordTextChannelId",
    discord_voice_channel_id AS "DiscordVoiceChannelId"
FROM teams
WHERE id = @TeamId;
