SELECT u.email, u.discord_id AS DiscordId FROM users u
JOIN teams t ON t.team_admin_id = u.id
WHERE t.id = @TeamId;
