DELETE FROM invitations
WHERE id = @RequestId
  AND team_id = @TeamId
  AND status = 'pending';