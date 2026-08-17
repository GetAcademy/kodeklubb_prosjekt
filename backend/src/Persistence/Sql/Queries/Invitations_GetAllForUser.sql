SELECT i.id         AS Id,
       i.team_id    AS TeamId,
       t.name       AS TeamName,
       i.status     AS Status,
       i.invited_at AS InvitedAt
FROM invitations i
JOIN teams t ON i.team_id = t.id
WHERE i.invited_user_id = @UserId
ORDER BY i.invited_at DESC;
