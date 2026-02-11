UPDATE invitations
SET 
    status = 'accepted',
    responded_at = @RespondedAt,
    version = version + 1
WHERE id = @RequestId
  AND team_id = @TeamId
  AND status = 'pending';
