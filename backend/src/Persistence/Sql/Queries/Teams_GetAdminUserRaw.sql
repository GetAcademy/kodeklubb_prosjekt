SELECT u.* FROM users u
JOIN teams t ON u.id = t.team_admin_id
WHERE t.id = @TeamId;
