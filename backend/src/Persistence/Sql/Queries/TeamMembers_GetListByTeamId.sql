SELECT
    tm.id AS Id,
    tm.team_id AS TeamId,
    tm.user_id AS UserId,
    tm.role AS Role,
    tm.status AS Status,
    tm.joined_at AS JoinedAt,
    u.username AS Username,
    u.discord_id AS DiscordId,
    u.avatar_url AS AvatarUrl
FROM team_members tm
INNER JOIN users u ON u.id = tm.user_id
WHERE tm.team_id = @TeamId
ORDER BY tm.joined_at ASC;