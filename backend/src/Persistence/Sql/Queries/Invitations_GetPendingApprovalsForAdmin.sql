SELECT i.id AS Id, i.team_id AS TeamId, t.name AS TeamName,
       u.username AS FromUsername, i.invited_at AS CreatedAt,
       'join_request' AS Type
FROM invitations i
JOIN teams t ON t.id = i.team_id
JOIN users u ON u.id = i.invited_user_id
WHERE t.team_admin_id = @UserId
  AND i.status = 'pending'
ORDER BY i.invited_at DESC;
