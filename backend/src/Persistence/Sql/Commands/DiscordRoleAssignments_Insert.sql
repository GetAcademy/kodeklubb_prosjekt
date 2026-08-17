INSERT INTO discord_role_assignments (id, team_id, user_id, discord_role_id, assigned_at)
SELECT uuid_generate_v4(), @TeamId, @UserId, discord_role_id, NOW()
FROM teams WHERE id = @TeamId
ON CONFLICT (team_id, user_id) DO NOTHING;