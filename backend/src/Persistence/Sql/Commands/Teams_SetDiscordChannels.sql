UPDATE teams
SET
    discord_team_role_id = @DiscordTeamRoleId,
    discord_text_channel_id = @DiscordTextChannelId,
    discord_voice_channel_id = @DiscordVoiceChannelId,
    updated_at = NOW()
WHERE id = @TeamId;
