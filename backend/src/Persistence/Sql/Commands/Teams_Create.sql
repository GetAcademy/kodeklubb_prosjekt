INSERT INTO teams (
    id, 
    name, 
    description, 
    created_by, 
    team_admin_id, 
    is_open_to_join_requests,
    created_at, 
    updated_at, 
    version
)
VALUES (
    @Id, 
    @Name, 
    @Description, 
    @AdminUserId, 
    @AdminUserId, 
    true,
    NOW(), 
    NOW(), 
    1
);
