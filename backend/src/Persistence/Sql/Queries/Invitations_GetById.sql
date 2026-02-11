SELECT 
    i.invited_user_id AS UserId,
    i.status AS Status
FROM invitations i
WHERE i.id = @RequestId
  AND i.team_id = @TeamId;
