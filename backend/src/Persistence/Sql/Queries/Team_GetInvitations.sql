SELECT
    id,
    team_id,
    invited_user_id,
    invited_by,
    status,
    invited_at,
    responded_at,
    expires_at,
    version
FROM invitations
WHERE id = @Id;