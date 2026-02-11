SELECT 
    i.id,
    i.team_id,
    i.invited_user_id,
    i.invited_by,
    i.status,
    i.invited_at,
    i.responded_at,
    i.expires_at,
    i.version,
    u.username,
    u.discord_id,
    u.avatar_url
FROM invitations i
INNER JOIN users u ON i.invited_user_id = u.id
WHERE i.team_id = @TeamId
    AND i.status = 'pending'
    AND i.invited_by = i.invited_user_id  -- Self-requested (join requests)
ORDER BY i.invited_at DESC;
