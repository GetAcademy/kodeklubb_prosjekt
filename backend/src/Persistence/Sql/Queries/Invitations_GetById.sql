-- SELECT 
--     i.invited_user_id AS UserId,
--     i.status AS Status
-- FROM invitations i
-- WHERE i.id = @RequestId
--   AND i.team_id = @TeamId;

SELECT
    id AS Id,
    team_id AS TeamId,
    invited_user_id AS InvitedUserId,
    invited_by AS InvitedBy,
    status AS Status,
    invited_at AS InvitedAt,
    responded_at AS RespondedAt,
    expires_at AS ExpiresAt,
    version AS Version
FROM invitations
WHERE
    id = @RequestId
    AND team_id = @TeamId
