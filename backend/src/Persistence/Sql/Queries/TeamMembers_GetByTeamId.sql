SELECT
    id AS Id,
    team_id AS TeamId,
    user_id AS UserId,
    NULL AS DiscordId,
    role AS Role,
    status AS Status,
    joined_at AS JoinedAt,
    updated_at AS UpdatedAt,
    version AS Version
FROM team_members
WHERE team_id = @Id;