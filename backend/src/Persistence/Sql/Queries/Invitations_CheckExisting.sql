SELECT EXISTS (
    SELECT 1 
    FROM invitations
    WHERE team_id = @TeamId
        AND invited_user_id = @UserId
        AND status = 'pending'
);
