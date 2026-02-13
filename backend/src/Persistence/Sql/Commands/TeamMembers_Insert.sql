INSERT INTO team_members (
    team_id, 
    user_id, 
    role, 
    status, 
    joined_at, 
    updated_at, 
    version
)
VALUES (
    @TeamId, 
    @UserId, 
    @Role, 
    'active', 
    NOW(), 
    NOW(), 
    1
);
