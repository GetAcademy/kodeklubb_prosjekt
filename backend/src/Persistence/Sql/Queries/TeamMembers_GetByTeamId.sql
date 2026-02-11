SELECT
    id,
    team_id,
    user_id,
    role,
    status,
    joined_at,
    updated_at,
    version
FROM team_members
WHERE id = @Id;