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
    t.version
FROM teams t
INNER JOIN team_members tm ON tm.team_id = t.id
INNER JOIN users u ON u.id = tm.user_id
WHERE u.discord_id = @DiscordId
ORDER BY t.created_at DESC;
