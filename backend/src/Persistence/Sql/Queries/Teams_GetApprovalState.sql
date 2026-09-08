SELECT
    id AS "Id",
    user_id AS "UserId",
    role AS "Role"
FROM team_members
WHERE team_id = @TeamId;

SELECT
    id AS "Id",
    invited_user_id AS "InvitedUserId"
FROM invitations
WHERE team_id = @TeamId;
