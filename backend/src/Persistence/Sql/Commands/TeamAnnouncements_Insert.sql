INSERT INTO team_announcements (
    id,
    team_id,
    created_by,
    title,
    body,
    created_at,
    updated_at,
    version
)
VALUES (
    @Id,
    @TeamId,
    @CreatedBy,
    @Title,
    @Body,
    NOW(),
    NOW(),
    1
);
