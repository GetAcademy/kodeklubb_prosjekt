SELECT tm.user_id, u.username, u.email, u.avatar_url, tm.team_id, tm.role
FROM team_members tm
JOIN users u ON tm.user_id = u.id
WHERE tm.team_id = @TeamId;