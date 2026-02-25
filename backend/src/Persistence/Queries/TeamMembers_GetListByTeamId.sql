SELECT tm.id, tm.team_id, tm.user_id, tm.role, tm.status, tm.joined_at, tm.updated_at, tm.version, u.username, u.discord_id
FROM team_members tm
JOIN users u ON tm.user_id = u.id
WHERE tm.team_id = @TeamId;