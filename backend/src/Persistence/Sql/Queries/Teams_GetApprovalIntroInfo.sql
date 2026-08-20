SELECT t.discord_channel_id AS DiscordChannelId,
       admin_u.discord_id   AS AdminDiscordId,
       admin_u.username     AS AdminUsername,
       new_u.discord_id     AS NewMemberDiscordId,
       new_u.username       AS NewMemberUsername
FROM teams t
JOIN users admin_u ON admin_u.id = t.team_admin_id
JOIN users new_u ON new_u.id = @UserId
WHERE t.id = @TeamId;
