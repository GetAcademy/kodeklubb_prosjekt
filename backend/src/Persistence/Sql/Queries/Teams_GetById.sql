SELECT
    id,
    name,
    description,
    location,
    discord_link AS DiscordLink,
    discord_server_id AS DiscordServerId,
    discord_channel_id AS DiscordChannelId,
    discord_role_id AS DiscordRoleId,
    meeting_schedule AS MeetingSchedule,
    is_open_to_join_requests AS IsOpenToJoinRequests,
    created_by AS CreatedBy,
    team_admin_id AS TeamAdminId,
    created_at AS CreatedAt,
    updated_at AS UpdatedAt,
    version
FROM teams
WHERE id = @TeamId;