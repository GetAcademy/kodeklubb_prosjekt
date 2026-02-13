INSERT INTO user_tags (
    user_id,
    tag_id,
    created_at
)
VALUES (
    @UserId,
    @TagId,
    NOW()
)
ON CONFLICT (user_id, tag_id)
DO NOTHING;
