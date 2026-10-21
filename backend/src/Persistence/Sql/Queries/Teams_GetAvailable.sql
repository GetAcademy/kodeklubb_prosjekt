SELECT DISTINCT 
    t.id, 
    t.name, 
    t.description, 
    t.location, 
    t.discord_link AS DiscordLink, 
    t.meeting_schedule AS MeetingSchedule, 
    t.is_open_to_join_requests AS IsOpenToJoinRequests,
    t.created_by AS CreatedBy, 
    t.team_admin_id AS TeamAdminId, 
    t.created_at AS CreatedAt, 
    t.updated_at AS UpdatedAt, 
    t.version,
    pending.invited_at AS PendingRequestSentAt
FROM teams t
LEFT JOIN LATERAL (
    SELECT i.invited_at
    FROM invitations i
    JOIN users u2 ON u2.id = i.invited_user_id
    WHERE i.team_id = t.id AND u2.discord_id = @DiscordId AND i.status = 'pending'
    ORDER BY i.invited_at DESC
    LIMIT 1
) pending ON true
WHERE t.is_open_to_join_requests = true
  AND (@DiscordId IS NULL OR NOT EXISTS (
    SELECT 1 FROM team_members tm
    INNER JOIN users u ON u.id = tm.user_id
    WHERE tm.team_id = t.id AND u.discord_id = @DiscordId
))
ORDER BY t.created_at DESC;