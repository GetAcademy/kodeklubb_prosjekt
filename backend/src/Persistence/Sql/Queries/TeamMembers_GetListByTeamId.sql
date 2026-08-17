SELECT tm.id AS Id,
       tm.team_id AS TeamId,
       tm.user_id AS UserId,
       u.discord_id AS DiscordId,
       u.username AS Username,
       u.email AS Email,
       u.avatar_url AS AvatarUrl,
       tm.role AS Role,
       tm.status AS Status,
       tm.joined_at AS JoinedAt,
       tm.updated_at AS UpdatedAt,
       tm.version AS Version
FROM team_members tm
JOIN users u ON tm.user_id = u.id
WHERE tm.team_id = @TeamId;