SELECT i.id AS Id, i.team_id AS TeamId, t.name AS TeamName,
       i.status AS Type, i.responded_at AS CreatedAt
FROM invitations i
JOIN teams t ON t.id = i.team_id
WHERE i.invited_user_id = @UserId
  AND i.status IN ('accepted', 'declined')
  AND i.responded_at > NOW() - INTERVAL '7 days'
ORDER BY i.responded_at DESC;
