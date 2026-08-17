SELECT u.id, u.discord_id
FROM team_members tm
JOIN users u ON u.id = tm.user_id
WHERE tm.team_id = @TeamId AND tm.status = 'active';
