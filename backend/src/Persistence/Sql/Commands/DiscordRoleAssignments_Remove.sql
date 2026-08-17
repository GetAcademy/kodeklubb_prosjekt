UPDATE discord_role_assignments
SET removed_at = NOW()
WHERE team_id = @TeamId AND user_id = @UserId AND removed_at IS NULL;